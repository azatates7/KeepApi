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
    //private readonly string _filePath;
    //private readonly SemaphoreSlim _fileLock = new(1, 1);

    private static readonly TimeSpan CacheExpiration =
        TimeSpan.FromMinutes(30);

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<NoteService> _logger;
    private readonly IDatabase _redis;
    private readonly KeepDbContext _context;

    private const string NotesCacheKey = "notes:all";

    public NoteService(IWebHostEnvironment env, ILogger<NoteService> logger, IConnectionMultiplexer redis, KeepDbContext context)
    {
        _logger = logger;
        _redis = redis.GetDatabase();
        _context = context;
    }

    public async Task<List<Note>> GetAsync(CancellationToken cancellationToken)
    {
        var results = await GetNotesWithRedisControl(cancellationToken);

        _logger.LogInformation("Notes have been read.");

        return results.Where(x => !x.IsDeleted && x.Status == 1).ToList();
    }

    public async Task<List<Note>> GetAllAsync(CancellationToken cancellationToken)
    {
        var results = await GetNotesWithRedisControl(cancellationToken);

        _logger.LogInformation("All notes have been read.");

        return results;
    }

    public async Task<List<Note>> GetDeletedAsync(CancellationToken cancellationToken)
    {
        var results = await GetNotesWithRedisControl(cancellationToken);

        _logger.LogInformation("Trash notes loaded. Total: {TotalCount}, Deleted: {DeletedCount}", results.Count, results.Count(x => x.IsDeleted));

        return results
            .Where(x => x.IsDeleted && x.Status == 1)
            .ToList();
    }

    public async Task<Note?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var notes = await GetNotesWithRedisControl(cancellationToken);
        return notes.FirstOrDefault(x => x.Id == id);
    }

    public async Task<Note> CreateAsync(Note note, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(note?.Title) || string.IsNullOrWhiteSpace(note?.Content))
            {
                throw new Exception("Not title veya içerik boş olamaz.");
            }

            note.Id = Guid.NewGuid().ToString("N");
            note.CreatedAt = DateTime.UtcNow;
            note.UpdatedAt = DateTime.UtcNow;
            note.IsDeleted = false;
            note.Status = 1;

            await _context.Notes.AddAsync(note, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await ClearCacheAsync();

            _logger.LogInformation("Note created. Id: {Id}", note.Id);

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
            await ClearCacheAsync();

            var existing = await _context.Notes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (existing is null)
                return null;

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
            existing.UpdatedAt = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(existing?.Title) || string.IsNullOrWhiteSpace(existing?.Content))
            {
                throw new Exception("Not title veya içerik boş olamaz.");
            }

            var result = await _context.SaveChangesAsync(cancellationToken);

            await ClearCacheAsync();

            _logger.LogInformation("Note updated. Id: {Id}", id);

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
            await ClearCacheAsync();

            var existing = await _context.Notes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (existing is null)
                return false;

            existing.IsDeleted = true;
            existing.Status = 1;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await ClearCacheAsync();

            _logger.LogInformation("Note moved to trash. Id: {Id}", id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting note.");
            throw;
        }
    }

    public async Task<bool> RestoreAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            await ClearCacheAsync();

            var existing = await _context.Notes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (existing is null)
                return false;

            existing.IsDeleted = false;
            existing.Status = 1;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);

            await ClearCacheAsync();

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
            await ClearCacheAsync();

            var existing = await _context.Notes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (existing is null)
                return false;

            existing.Status = 0;
            existing.IsDeleted = true;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);

            await ClearCacheAsync();

            _logger.LogInformation("Note permanently deleted. Id: {Id}", id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while permanently deleting note.");
            throw;
        }
    }

    private async Task<List<Note>> GetDatabaseRecords(CancellationToken cancellationToken)
    {
        try
        {
            return await _context.Notes
                        .AsNoTracking()
                        .OrderByDescending(x => x.CreatedAt)
                        .ToListAsync(cancellationToken) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notes from database.");
            throw;
        }
    }

    private async Task ClearCacheAsync()
    {
        await _redis.KeyDeleteAsync(NotesCacheKey);
    }

    private async Task<List<Note>> GetNotesWithRedisControl(CancellationToken cancellationToken)
    {
        try
        {
            // First attempt: read from Redis without locking.
            var cachedNotes = await _redis.StringGetAsync(NotesCacheKey);

            if (cachedNotes.HasValue)
            {
                return JsonSerializer.Deserialize<List<Note>>((string)cachedNotes!, _jsonOptions) ?? [];
            }
            
            try
            {
                // Another thread may already have populated Redis.
                cachedNotes = await _redis.StringGetAsync(NotesCacheKey);

                if (cachedNotes.HasValue)
                {
                    return JsonSerializer.Deserialize<List<Note>>((string)cachedNotes!, _jsonOptions) ?? [];
                }

                var notes = await GetDatabaseRecords(cancellationToken);

                var serialized = JsonSerializer.Serialize(notes, _jsonOptions);

                await _redis.StringSetAsync(NotesCacheKey, serialized, CacheExpiration);

                _logger.LogInformation("Notes loaded from disk and cached in Redis.");

                return notes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load notes from Redis. Falling back to disk.");

                return await GetDatabaseRecords(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load notes from Redis. Falling back to disk.");

            return await GetDatabaseRecords(cancellationToken);
        }
    }
}