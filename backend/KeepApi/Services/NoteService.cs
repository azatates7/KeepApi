using KeepApi.Application.Interfaces;
using KeepApi.Common.Extensions;
using KeepApi.Data.Context;
using KeepApi.Data.Entity;
using KeepApi.Models.Request.Note;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace KeepApi.Services;

/// <summary>
/// Persists notes to a JSON file on disk instead of a database.
/// Redis is used as a cache layer.
/// JSON file remains the source of truth.
/// </summary>
public class NoteService
{
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<NoteService> _logger;
    private readonly IDatabase _redis;
    private readonly KeepDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public NoteService(
        IWebHostEnvironment env,
        ILogger<NoteService> logger,
        IConnectionMultiplexer redis,
        KeepDbContext context,
        ICurrentUserService currentUser)
    {
        _logger = logger;
        _redis = redis.GetDatabase();
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<Note>> GetAsync(CancellationToken cancellationToken)
    {
        var results = await GetNotesWithRedisControl(cancellationToken);
        var currentUserId = GetCurrentUserId();

        _logger.LogInformation("Notes have been read.");

        return results.Where(x => !x.IsDeleted && x.Status == 1).ToList();
    }

    public async Task<List<Note>> GetAllAsync(CancellationToken cancellationToken)
    {
        var results = await _context.Notes
                            .AsNoTracking()
                            .OrderByDescending(x => x.CreatedAt)
                            .ToListAsync(cancellationToken);

        return results;
    }

    /// <summary>
    /// Search ekranı için kullanıcının tüm görünür kayıtlarını döndürür.
    /// Aktif, sabitlenmiş, arşivlenmiş ve çöp kutusundaki kayıtlar dahildir.
    /// Kalıcı olarak silinen kayıtlar (Status = 0) hariç tutulur.
    /// </summary>
    public async Task<List<Note>> GetSearchableAsync(CancellationToken cancellationToken)
    {
        var results = await GetNotesWithRedisControl(cancellationToken);

        _logger.LogInformation(
            "Search records loaded. Total: {TotalCount}",
            results.Count(x => x.Status == 1));

        return results
            .Where(x => x.Status == 1)
            .ToList();
    }

    public async Task<List<Note>> GetDeletedAsync(CancellationToken cancellationToken)
    {
        var results = await GetNotesWithRedisControl(cancellationToken);
        var currentUserId = GetCurrentUserId();

        _logger.LogInformation("Trash notes loaded. Total: {TotalCount}, Deleted: {DeletedCount}", results.Count, results.Count(x => x.IsDeleted));

        return results
            .Where(x => x.IsDeleted && x.Status == 1)
            .ToList();
    }

    public async Task<Note?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var notes = await GetNotesWithRedisControl(cancellationToken);
        var currentUserId = GetCurrentUserId();

        return notes.FirstOrDefault(x => x.Id == id);
    }

    public async Task<Note> CreateAsync(CreateNoteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!request.ImageAdded &&
                !request.Checklist &&
                string.IsNullOrWhiteSpace(request?.Content))
            {
                throw new Exception("Not title veya içerik boş olamaz.");
            }

            var currentUserId = GetCurrentUserId();

            var note = new Note
            {
                Title = request.Title,
                Content = request.Content,
                Color = request.Color,
                Checklist = request.Checklist,
                ImageAdded = request.ImageAdded,
                ImageUrl = request.ImageAdded ? request.ImageUrl.Truncate(200) : null,
                UserId = currentUserId,
                CreatedById = currentUserId,
            };

            note.Id = Guid.NewGuid().ToString("N");
            await _context.Notes.AddAsync(note, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await ClearCacheAsync(currentUserId);

            _logger.LogInformation(
            "User {UserId} created note {NoteId}",
            currentUserId,
            note.Id);

            return note;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating note.");
            throw;
        }
    }

    public async Task<Note?> UpdateAsync(string id, [FromBody] UpdateNoteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!request.ImageAdded && string.IsNullOrWhiteSpace(request?.Content))
            {
                throw new Exception("Not title veya içerik boş olamaz.");
            }

            var currentUserId = GetCurrentUserId();

            var existing = await _context.Notes.FirstOrDefaultAsync(x => x.UserId == currentUserId && x.Id == id, cancellationToken);
            if (existing is null)
            {
                return null;
            }

            existing.Title = request.Title;
            existing.Content = request.Content;
            existing.Color = request.Color;
            existing.Pinned = request.Pinned;
            existing.PinnedAt = request.PinnedAt;
            existing.Archived = request.Archived;
            existing.ArchievedAt = request.ArchievedAt;
            existing.ReminderAt = request.ReminderAt;
            existing.IsDeleted = request.IsDeleted;
            existing.Status = request.Status;
            existing.UpdatedById = currentUserId;

            existing.Checklist = request.Checklist;
            existing.ImageAdded = request.ImageAdded;
            existing.ImageUrl = request.ImageUrl;

            if (request.ImageAdded)
            {
                existing.ImageAdded = request.ImageAdded;
                existing.ImageUrl = request.ImageAdded ? (request.ImageUrl ?? existing.ImageUrl) : null;
            }
            else if (request.ImageUrl != null)
            {
                existing.ImageUrl = request.ImageUrl;
                existing.ImageAdded = true;
            }

            var result = await _context.SaveChangesAsync(cancellationToken);

            await ClearCacheAsync(currentUserId);

            _logger.LogInformation($"User {currentUserId} updated note {id}");

            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating note.");
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            var existing = await _context.Notes.FirstOrDefaultAsync(x => x.UserId == currentUserId && x.Id == id, cancellationToken);
            if (existing is null)
                return false;

            existing.IsDeleted = true;
            existing.Status = 1;
            existing.DeletedById = currentUserId;

            await _context.SaveChangesAsync(cancellationToken);

            await ClearCacheAsync(currentUserId);

            _logger.LogInformation("Note moved to trash. Id: {Id}", id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting note.");
            throw;
        }
    }

    // Ayrı, açık bir "görseli sil" endpoint'i de eklenebilir — frontend'de tek çağrıyla net semantik sağlar.
    [HttpDelete("{id}/image")]
    public async Task<bool> DeleteNoteImage(string id)
    {
        var currentUserId = GetCurrentUserId();

        var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == currentUserId);
        if (note == null) return false;

        note.ImageAdded = false;
        note.ImageUrl = null;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            var existing = await _context.Notes.FirstOrDefaultAsync(x => x.UserId == currentUserId && x.Id == id, cancellationToken);
            if (existing is null)
                return false;

            existing.IsDeleted = false;
            existing.Status = 1;
            existing.UpdatedById = currentUserId;

            await _context.SaveChangesAsync(cancellationToken);

            await ClearCacheAsync(currentUserId);

            _logger.LogInformation("Note restored. Id: {Id}", id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while restoring note.");
            throw;
        }
    }

    public async Task<bool> DeleteForeverAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            var existing = await _context.Notes.FirstOrDefaultAsync(x => x.UserId == currentUserId && x.Id == id, cancellationToken);

            if (existing is null)
                return false;

            existing.Status = 0;
            existing.IsDeleted = true;
            existing.DeletedById = currentUserId;

            await _context.SaveChangesAsync(cancellationToken);

            await ClearCacheAsync(currentUserId);

            _logger.LogInformation("Note permanently deleted. Id: {Id}", id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while permanently deleting note.");
            throw;
        }
    }

    private async Task<List<Note>> GetDatabaseRecords(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            return await _context.Notes
                        .AsNoTracking()
                        .Where(x => x.UserId == userId)
                        .OrderByDescending(x => x.CreatedAt)
                        .ToListAsync(cancellationToken) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notes from database.");
            throw;
        }
    }

    private string GetCacheKey(Guid userId)
    {
        return $"notes:user:{userId}";
    }

    private async Task ClearCacheAsync(Guid userId)
    {
        await _redis.KeyDeleteAsync(GetCacheKey(userId));
    }

    private async Task<List<Note>> GetNotesWithRedisControl(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        try
        {
            var cacheKey = GetCacheKey(currentUserId);

            // First attempt: read from Redis without locking.
            var cachedNotes = await _redis.StringGetAsync(cacheKey);

            if (cachedNotes.HasValue)
            {
                return JsonSerializer.Deserialize<List<Note>>((string)cachedNotes!, _jsonOptions) ?? [];
            }
            
            try
            {
                // Another thread may already have populated Redis.
                cachedNotes = await _redis.StringGetAsync(cacheKey);

                if (cachedNotes.HasValue)
                {
                    return JsonSerializer.Deserialize<List<Note>>((string)cachedNotes!, _jsonOptions) ?? [];
                }

                var notes = await GetDatabaseRecords(currentUserId, cancellationToken);

                var serialized = JsonSerializer.Serialize(notes, _jsonOptions);

                await _redis.StringSetAsync(cacheKey, serialized, CacheExpiration);

                _logger.LogInformation("Notes loaded from disk and cached in Redis.");

                return notes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load notes from Redis. Falling back to disk.");

                return await GetDatabaseRecords(currentUserId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load notes from Redis. Falling back to disk.");

            return await GetDatabaseRecords(currentUserId, cancellationToken);
        }
    }

    private Guid GetCurrentUserId()
    {
        var currentUserId = _currentUser.UserId;
        if (currentUserId == Guid.Empty)
            throw new UnauthorizedAccessException("Unable to determine current user.");

        return currentUserId;
    }
}