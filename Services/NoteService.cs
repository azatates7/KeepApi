using KeepApi.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace KeepApi.Services;

/// <summary>
/// Persists notes to a JSON file on disk instead of a database.
/// All reads/writes go through a semaphore so concurrent requests
/// don't corrupt the file (classic JSON-as-DB race condition).
/// </summary>
public class NoteService
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<NoteService> _logger;
    private readonly IDatabase _redis;
    private const string NotesCacheKey = "notes:all";

    public NoteService(
        IWebHostEnvironment env,
        ILogger<NoteService> logger,
        IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis.GetDatabase();
        var dataDir = Path.Combine(env.ContentRootPath, "Data");

        Directory.CreateDirectory(dataDir);

        _filePath = Path.Combine(dataDir, "notes.json");

        if (!File.Exists(_filePath))
        {
            File.WriteAllText(_filePath, "[]");
        }
    }

    public async Task<List<Note>> GetAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var results = await ReadFileAsync();
            return results
                   .Where(x => !x.IsDeleted && x.Status == 1)
                   .ToList();
        }
        finally
        {
            _logger.LogInformation("Notes have been read.");
            _lock.Release();
        }
    }

    public async Task<List<Note>> GetAllsync()
    {
        await _lock.WaitAsync();
        try
        {
            var results = await ReadFileAsync();
            return results
                .ToList();
        }
        finally
        {
            _logger.LogInformation("All notes have been read.");
            _lock.Release();
        }
    }
    
    /// <summary>Çöp kutusundaki (IsDeleted == true) notları döner.</summary>
    public async Task<List<Note>> GetDeletedAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var results = await ReadFileAsync();
            Console.WriteLine($"Toplam kayıt : {results.Count}");

            Console.WriteLine($"Silinen : {results.Count(x => x.IsDeleted)}");

            return results
                   .Where(x => x.IsDeleted && x.Status == 1)
                   .ToList();
        }
        finally
        {
            _logger.LogInformation("Trash notes have been read.");
            _lock.Release();
        }
    }
    
    public async Task<bool> RestoreAsync(string id)
    {
        await _lock.WaitAsync();

        try
        {
            var notes = await ReadFileAsync();

            var existing = notes.FirstOrDefault(x => x.Id == id);

            if (existing == null)
                return false;

            existing.IsDeleted = false;
            existing.Status = 1;
            existing.UpdatedAt = DateTime.UtcNow;

            await WriteFileAsync(notes);

            return true;
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public async Task<bool> DeleteForeverAsync(string id)
    {
        await _lock.WaitAsync();

        try
        {
            var notes = await ReadFileAsync();

            var existing = notes.FirstOrDefault(x => x.Id == id);

            if (existing == null)
                return false;

            existing.Status = 0;
            existing.IsDeleted = true;
            existing.UpdatedAt = DateTime.UtcNow;

            await WriteFileAsync(notes);

            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Note?> GetByIdAsync(string id)
    {
        var notes = await GetAllsync();
        return notes.FirstOrDefault(n => n.Id == id);
    }

    public async Task<Note> CreateAsync(Note note)
    {
        await _lock.WaitAsync();
        try
        {
            var notes = await ReadFileAsync();

            note.Id = Guid.NewGuid().ToString("N");
            note.CreatedAt = DateTime.UtcNow;
            note.UpdatedAt = DateTime.UtcNow;
            note.IsDeleted = false;
            note.Status = 1;

            notes.Insert(0, note);
            await WriteFileAsync(notes);
            _logger.LogInformation("A Note has been created.");
            return note;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An error occurred while creating a Note.");
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Note?> UpdateAsync(string id, Note updated)
    {
        await _lock.WaitAsync();
        try
        {
            var notes = await ReadFileAsync();
            var existing = notes.FirstOrDefault(n => n.Id == id);
            if (existing is null) return null;

            existing.Title = updated.Title;
            existing.Content = updated.Content;
            existing.Color = updated.Color;
            existing.Pinned = updated.Pinned;
            existing.Archived = updated.Archived;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.ReminderAt = updated.ReminderAt;
            existing.IsDeleted = updated.IsDeleted;
            existing.Status = updated.Status;
            
            await WriteFileAsync(notes);
            return existing;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        await _lock.WaitAsync();
        try
        {
            var notes = await ReadFileAsync();
            var existing = notes.FirstOrDefault(n => n.Id == id);
            if (existing is null) return false;

            existing.IsDeleted = true;
            existing.Status = 1;
            existing.UpdatedAt = DateTime.UtcNow;
            
            await WriteFileAsync(notes);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<Note>> ReadFileAsync()
    {
        await using var stream = File.OpenRead(_filePath);
        var notes = await JsonSerializer.DeserializeAsync<List<Note>>(stream, _jsonOptions);
        return notes ?? [];
    }

    private async Task WriteFileAsync(List<Note> notes)
    {
        var tempPath = _filePath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, notes, _jsonOptions);
        }
        File.Move(tempPath, _filePath, overwrite: true);
    }
}
