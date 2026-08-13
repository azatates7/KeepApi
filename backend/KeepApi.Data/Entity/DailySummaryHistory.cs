using System.ComponentModel.DataAnnotations.Schema;

namespace KeepApi.Data.Entity
{
    [Table("DailySummaryHistories")]
    public class DailySummaryHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public string Content { get; set; } = null!;

        public DateTime GeneratedAt { get; set; }
    }
}