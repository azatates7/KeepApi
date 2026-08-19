using KeepApi.Common.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeepApi.Data.Entity
{
    [Table("JobDefinitions")]
    public class JobDefinition : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public int JobTypeId { get; set; }

        public string JobName { get; set; } = null!;

        public string? Description { get; set; }

        public string CronExpression { get; set; } = null!;

        public bool IsActive { get; set; }

        public ICollection<JobHistory> JobHistories { get; set; }
            = new List<JobHistory>();
    }
}
