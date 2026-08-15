using KeepApi.Data.Context;
using KeepApi.Data.Entity;
using KeepApi.Infrastructure.Llm;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace KeepApi.Services
{
    public class DailySummaryService
    {
        private readonly KeepDbContext _context;
        private readonly ILlmClient _llm;
        private readonly IDatabase _redis;
        private readonly ILogger<DailySummaryService> _logger;

        public DailySummaryService(
            KeepDbContext context,
            ILlmClient llm,
            IConnectionMultiplexer redis,
            ILogger<DailySummaryService> logger)
        {
            _context = context;
            _llm = llm;
            _redis = redis.GetDatabase();
            _logger = logger;
        }

        public async Task GenerateForUserAsync(Guid userId, CancellationToken ct)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.PreferredLanguage })
                    .FirstOrDefaultAsync(ct);

                var lang = user?.PreferredLanguage == "en" ? "en" : "tr";

                var since = DateTime.UtcNow.AddHours(-24);

                //var raw = await _context.Notes
                //.Where(n => n.UserId == userId)
                //.Select(n => new { n.Id, n.IsDeleted, n.IsDailySummary, n.Pinned, n.Archived, n.Checklist, n.ImageAdded })
                //.ToListAsync(ct);

                var recentNotes = await _context.Notes
                .Where(n => n.UserId == userId)
                .Where(n => n.IsDeleted == false)
                .Where(n => n.IsDailySummary == false)
                .Where(n => (n.UpdatedAt ?? n.CreatedAt) >= since)
                .OrderBy(n => n.CreatedAt)
                .ToListAsync(ct);

                var existingSummaryNote = await _context.Notes
                    .FirstOrDefaultAsync(n => n.UserId == userId && n.IsDailySummary, ct);

                // Sadece "yeni not yok VE özet notu zaten var" durumunda atla.
                // Özet notu hiç yoksa (ilk çalıştırma), not olmasa bile oluştur.
                if (recentNotes.Count == 0 && existingSummaryNote is not null)
                {
                    _logger.LogInformation("Kullanıcı {UserId} için yeni not yok, mevcut özet korunuyor.", userId);
                    return;
                }

                string summaryText;
                if (recentNotes.Count == 0)
                {
                    summaryText = lang == "en"
                        ? "No new notes were found to summarize today."
                        : "Bugün için özetlenecek yeni not bulunamadı.";
                }
                else
                {
                    var notesText = lang == "en"
                        ? string.Join("\n---\n", recentNotes.Select(n => $"Title: {n.Title}\nContent: {n.Content}"))
                        : string.Join("\n---\n", recentNotes.Select(n => $"Başlık: {n.Title}\nİçerik: {n.Content}"));

                    var prompt = lang == "en"
                        ? $"Summarize the following notes into a single short daily summary paragraph, in English:\n\n{notesText}"
                        : $"Aşağıdaki notları tek bir günlük özet paragrafı halinde, Türkçe ve kısa şekilde özetle:\n\n{notesText}";

                    summaryText = await _llm.SummarizeAsync(prompt, ct);
                }

                if (existingSummaryNote is null)
                {
                    existingSummaryNote = new Note
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        UserId = userId,
                        CreatedById = userId,
                        IsDailySummary = true,
                        Color = "default"
                    };
                    _context.Notes.Add(existingSummaryNote);
                }
                else
                {
                    existingSummaryNote.UpdatedById = userId;
                }

                existingSummaryNote.Title = lang == "en"
                    ? $"Daily Summary - {DateTime.Now:dd.MM.yyyy}"
                    : $"Günlük Özet - {DateTime.Now:dd.MM.yyyy}";
                existingSummaryNote.Content = summaryText;

                _context.DailySummaryHistories.Add(new DailySummaryHistory
                {
                    UserId = userId,
                    Content = summaryText,
                    GeneratedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync(ct);
                await _redis.KeyDeleteAsync($"notes:user:{userId}");

                _logger.LogInformation("Kullanıcı {UserId} için özet güncellendi, geçmişe eklendi.", userId);
            }
            catch (Exception exception)
            {
                _logger.LogError($"Çalışma sırasında hata alındı. Hata => {exception.Message} Detay => {exception.StackTrace}");
            }
        }
    }
}