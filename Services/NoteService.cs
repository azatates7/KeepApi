using KeepApi.Data.Context;
using KeepApi.Models;
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
    private readonly string _filePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

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

        var dataDir = Path.Combine(env.ContentRootPath, "Data");

        Directory.CreateDirectory(dataDir);

        _filePath = Path.Combine(dataDir, "notes.json");

        if (!File.Exists(_filePath))
        {
            File.WriteAllText(_filePath, "[]");
        }
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
        await _fileLock.WaitAsync(cancellationToken);

        try
        {
            if (string.IsNullOrWhiteSpace(note?.Title) || string.IsNullOrWhiteSpace(note?.Content))
            {
                throw new Exception("Not title veya içerik boş olamaz.");
            }

            var notes = await ReadFileAsync(cancellationToken);

            note.Id = Guid.NewGuid().ToString("N");
            note.CreatedAt = DateTime.UtcNow;
            note.UpdatedAt = DateTime.UtcNow;
            note.IsDeleted = false;
            note.Status = 1;

            notes.Insert(0, note);

            await WriteFileAsync(notes, cancellationToken);

            _logger.LogInformation("Note created. Id: {Id}", note.Id);

            return note;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating note.");
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<Note?> UpdateAsync(string id, Note updated, CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken);

        try
        {
            var notes = await ReadFileAsync(cancellationToken);

            var existing = notes.FirstOrDefault(x => x.Id == id);
            if (existing is null)
                return null;

            existing.Title = updated.Title;
            existing.Content = updated.Content;
            existing.Color = updated.Color;
            existing.Pinned = updated.Pinned;
            existing.PinnedAt = updated.PinnedAt;
            existing.Archived = updated.Archived;
            existing.ArchievedAt = updated.ArchievedAt;
            existing.ReminderAt = updated.ReminderAt;
            existing.IsDeleted = updated.IsDeleted;
            existing.Status = updated.Status;
            existing.UpdatedAt = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(existing?.Title) || string.IsNullOrWhiteSpace(existing?.Content))
            {
                throw new Exception("Not title veya içerik boş olamaz.");
            }

            await WriteFileAsync(notes, cancellationToken);

            _logger.LogInformation("Note updated. Id: {Id}", id);

            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating note.");
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken);

        try
        {
            var notes = await ReadFileAsync(cancellationToken);

            var existing = notes.FirstOrDefault(x => x.Id == id);
            if (existing is null)
                return false;

            existing.IsDeleted = true;
            existing.Status = 1;
            existing.UpdatedAt = DateTime.UtcNow;

            await WriteFileAsync(notes, cancellationToken);

            _logger.LogInformation("Note moved to trash. Id: {Id}", id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting note.");
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<bool> RestoreAsync(string id, CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken);

        try
        {
            var notes = await ReadFileAsync(cancellationToken);

            var existing = notes.FirstOrDefault(x => x.Id == id);
            if (existing is null)
                return false;

            existing.IsDeleted = false;
            existing.Status = 1;
            existing.UpdatedAt = DateTime.UtcNow;

            await WriteFileAsync(notes, cancellationToken);

            _logger.LogInformation("Note restored. Id: {Id}", id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while restoring note.");
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<bool> DeleteForeverAsync(string id, CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken);

        try
        {
            var notes = await ReadFileAsync(cancellationToken);

            var existing = notes.FirstOrDefault(x => x.Id == id);

            if (existing is null)
                return false;

            existing.Status = 0;
            existing.IsDeleted = true;
            existing.UpdatedAt = DateTime.UtcNow;

            await WriteFileAsync(notes, cancellationToken);

            _logger.LogInformation("Note permanently deleted. Id: {Id}", id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while permanently deleting note.");
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<List<Note>> ReadFileAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_filePath))
                return [];

            await using var stream = File.OpenRead(_filePath);

            return await JsonSerializer.DeserializeAsync<List<Note>>(stream, _jsonOptions, cancellationToken) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read notes file.");
            throw;
        }
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

            // Cache miss -> only one thread should populate Redis.
            await _fileLock.WaitAsync(cancellationToken);

            try
            {
                // Another thread may already have populated Redis.
                cachedNotes = await _redis.StringGetAsync(NotesCacheKey);

                if (cachedNotes.HasValue)
                {
                    return JsonSerializer.Deserialize<List<Note>>((string)cachedNotes!, _jsonOptions) ?? [];
                }

                var notes = await ReadFileAsync(cancellationToken);

                var serialized = JsonSerializer.Serialize(notes, _jsonOptions);

                await _redis.StringSetAsync(NotesCacheKey, serialized, CacheExpiration);

                _logger.LogInformation("Notes loaded from disk and cached in Redis.");

                return notes;
            }
            finally
            {
                _fileLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load notes from Redis. Falling back to disk.");

            return await ReadFileAsync(cancellationToken);
        }
    }

    private async Task WriteFileAsync(List<Note> notes, CancellationToken cancellationToken)
    {
        await using (var stream = File.Create(_filePath))
        {
            await JsonSerializer.SerializeAsync(stream, notes, _jsonOptions, cancellationToken);
            await stream.FlushAsync();
        }

        try
        {
            var serializedNotes = JsonSerializer.Serialize(notes, _jsonOptions);

            await _redis.StringSetAsync(NotesCacheKey, serializedNotes, CacheExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Redis cache after writing notes.");
        }
    }
}