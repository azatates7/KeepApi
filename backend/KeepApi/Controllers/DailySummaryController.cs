using KeepApi.Application.Interfaces;
using KeepApi.Data.Context;
using KeepApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace KeepApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DailySummaryController : ControllerBase
    {
        private readonly DailySummaryService _summaryService;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ICurrentUserService _currentUser;

        public DailySummaryController(
            DailySummaryService summaryService,
            ISchedulerFactory schedulerFactory,
            ICurrentUserService currentUser)
        {
            _summaryService = summaryService;
            _schedulerFactory = schedulerFactory;
            _currentUser = currentUser;
        }

        // Kullanıcı kendi özetini istediği an yeniden üretir (senkron, sonucu bekler)
        [HttpPost("me/run")]
        public async Task<IActionResult> RunForMe(CancellationToken cancellationToken)
        {
            await _summaryService.GenerateForUserAsync(_currentUser.UserId, cancellationToken);
            return Ok(new { message = "Özet güncellendi." });
        }

        [HttpGet("me/history")]
        public async Task<IActionResult> GetMyHistory(
        [FromServices] KeepDbContext context,
        int take = 30,
        CancellationToken cancellationToken = default)
        {
            var history = await context.JobHistories
                .Where(h =>
                    h.Username == _currentUser.Username &&
                    h.JobTypeId == 1 &&
                    !h.IsDeleted)
                .OrderByDescending(h => h.StartedAt)
                .Take(take)
                .Select(h => new
                {
                    h.Id,
                    h.TransactionId,
                    h.JobTypeId,
                    h.Username,
                    h.StartedAt,
                    h.CompletedAt,
                    h.Status,
                    h.ErrorMessage
                })
                .ToListAsync(cancellationToken);

            return Ok(history);
        }

        // Tüm kullanıcılar için Quartz job'ını tetikler (asenkron, arka planda çalışır)
        [HttpPost("run-all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RunAll()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            await scheduler.TriggerJob(new JobKey("DailySummaryJob"));
            return Accepted(new { message = "Job tetiklendi, arka planda çalışıyor." });
        }
    }
}