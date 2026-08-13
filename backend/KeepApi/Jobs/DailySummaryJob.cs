using KeepApi.Data.Context;
using KeepApi.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace KeepApi.Jobs
{
    public class DailySummaryJob : IJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DailySummaryJob> _logger;

        public DailySummaryJob(IServiceScopeFactory scopeFactory, ILogger<DailySummaryJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KeepDbContext>();
            var summaryService = scope.ServiceProvider.GetRequiredService<DailySummaryService>();

            var userIds = await db.Notes
                .Where(n => !n.IsDeleted)
                .Select(n => n.UserId)
                .Distinct()
                .ToListAsync(context.CancellationToken);

            foreach (var userId in userIds)
            {
                try
                {
                    await summaryService.GenerateForUserAsync(userId, context.CancellationToken);
                }
                catch (Exception ex)
                { // tek kullanıcının hatası diğerlerini durdurmasın diye devam ediyoruz, hatayı logluyoruz
                    _logger.LogError(ex, "Kullanıcı {UserId} için günlük özet job'ı başarısız oldu.", userId);
                }
            }
        }
    }
}