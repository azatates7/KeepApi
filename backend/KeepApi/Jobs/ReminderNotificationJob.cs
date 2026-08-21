using KeepApi.Data.Context;
using KeepApi.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace KeepApi.Jobs
{
    /// <summary>
    /// ReminderAt zamanı geçmiş ama henüz bildirilmemiş (ReminderNotifiedAt == null) notlar için
    /// sahibine e-posta gönderir. DailySummaryJob'ın aksine bir JobDefinition/JobHistory kaydına
    /// bağlı değil — sık aralıklarla (bkz. Program.cs tetikleyicisi) çalışan, kendi başına yeten
    /// basit bir job. Bir kullanıcıya e-posta gönderimi başarısız olursa o notun
    /// ReminderNotifiedAt'i set edilmez, böylece bir sonraki çalışmada tekrar denenir.
    /// </summary>
    public class ReminderNotificationJob : IJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReminderNotificationJob> _logger;

        public ReminderNotificationJob(
            IServiceScopeFactory scopeFactory,
            ILogger<ReminderNotificationJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<KeepDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            // NoteService/DailySummaryJob'daki diğer zaman alanlarıyla tutarlı olması için
            // DateTime.Now kullanılıyor (ReminderAt de frontend'den yerel saatle yazılıyor).
            var now = DateTime.Now;

            var dueNotes = await db.Notes
                .Include(n => n.User)
                .Where(n =>
                    !n.IsDeleted &&
                    n.Status == 1 &&
                    n.ReminderAt != null &&
                    n.ReminderAt <= now &&
                    n.ReminderNotifiedAt == null)
                .ToListAsync(context.CancellationToken);

            if (dueNotes.Count == 0)
            {
                return;
            }

            var sentCount = 0;

            foreach (var note in dueNotes)
            {
                if (string.IsNullOrWhiteSpace(note.User?.Email))
                {
                    _logger.LogWarning(
                        "Not {NoteId} için hatırlatma e-postası atlandı: kullanıcının e-postası yok.",
                        note.Id);

                    // Sahibinin e-postası hiç yoksa bu not her job çalışmasında tekrar denenmesin.
                    note.ReminderNotifiedAt = now;
                    continue;
                }

                try
                {
                    var title = string.IsNullOrWhiteSpace(note.Title) ? "Notunuz" : note.Title;

                    await emailService.SendAsync(
                        note.User.Email,
                        $"Hatırlatma: {title}",
                        $"Merhaba {note.User.FirstName},\n\n" +
                        $"\"{title}\" için ayarladığınız hatırlatma zamanı geldi.\n\n" +
                        $"{note.Content}",
                        context.CancellationToken);

                    note.ReminderNotifiedAt = now;
                    sentCount++;
                }
                catch (Exception ex)
                {
                    // Bu notu bilerek atla (ReminderNotifiedAt set edilmedi) — bir sonraki
                    // job çalışmasında tekrar denenecek. Tüm job'ı burada durdurmuyoruz ki
                    // tek bir SMTP hatası diğer kullanıcıların bildirimini de engellemesin.
                    _logger.LogError(ex, "Not {NoteId} için hatırlatma e-postası gönderilemedi.", note.Id);
                }
            }

            await db.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "ReminderNotificationJob tamamlandı. Uygun not: {DueCount}, gönderilen: {SentCount}.",
                dueNotes.Count,
                sentCount);
        }
    }
}