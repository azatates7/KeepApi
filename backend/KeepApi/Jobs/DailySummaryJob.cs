using KeepApi.Common.Enums;
using KeepApi.Data.Context;
using KeepApi.Data.Entity;
using KeepApi.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace KeepApi.Jobs
{
    public class DailySummaryJob : IJob
    {
        private const int JobTypeId = 1;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DailySummaryJob> _logger;

        public DailySummaryJob(
            IServiceScopeFactory scopeFactory,
            ILogger<DailySummaryJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<KeepDbContext>();

            var summaryService = scope.ServiceProvider
                .GetRequiredService<DailySummaryService>();

            var jobDefinition = await db.JobDefinitions
                .FirstOrDefaultAsync(
                    x =>
                        x.JobTypeId == JobTypeId &&
                        x.IsActive &&
                        !x.IsDeleted,
                    context.CancellationToken);

            if (jobDefinition is null)
            {
                _logger.LogWarning(
                    "DailySummary JobDefinition bulunamadı. JobTypeId: {JobTypeId}",
                    JobTypeId);

                return;
            }

            var transactionId = Guid.NewGuid();

            var jobHistory = new JobHistory
            {
                Id = Guid.NewGuid(),

                JobDefinitionId = jobDefinition.Id,
                JobTypeId = jobDefinition.JobTypeId,

                TransactionId = transactionId,

                Username = "SYSTEM",

                StartedAt = DateTime.UtcNow,

                Status = (int)JobStatus.Pending,

                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            db.JobHistories.Add(jobHistory);

            await db.SaveChangesAsync(context.CancellationToken);

            try
            {
                var userIds = await db.Notes
                    .Where(n => !n.IsDeleted)
                    .Select(n => n.UserId)
                    .Distinct()
                    .ToListAsync(context.CancellationToken);

                foreach (var userId in userIds)
                {
                    try
                    {
                        await summaryService.GenerateForUserAsync(
                            userId,
                            context.CancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Kullanıcı {UserId} için günlük özet job'ı başarısız oldu.",
                            userId);
                    }
                }

                jobHistory.Status = (int)JobStatus.Success;
                jobHistory.CompletedAt = DateTime.Now;
                jobHistory.UpdatedAt = DateTime.Now;

                await db.SaveChangesAsync(context.CancellationToken);

                _logger.LogInformation(
                    "DailySummaryJob başarıyla tamamlandı. TransactionId: {TransactionId}",
                    transactionId);
            }
            catch (Exception ex)
            {
                jobHistory.Status = (int)JobStatus.Failed;
                jobHistory.ErrorMessage = ex.ToString();
                jobHistory.CompletedAt = DateTime.Now;
                jobHistory.UpdatedAt = DateTime.Now;

                await db.SaveChangesAsync(CancellationToken.None);

                _logger.LogError(
                    ex,
                    "DailySummaryJob başarısız oldu. TransactionId: {TransactionId}",
                    transactionId);

                throw;
            }
        }
    }
}