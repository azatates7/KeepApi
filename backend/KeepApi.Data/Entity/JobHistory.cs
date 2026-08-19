using KeepApi.Common.Models;

namespace KeepApi.Data.Entity
{
    public class JobHistory : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid JobDefinitionId { get; set; }

        public JobDefinition JobDefinition { get; set; } = null!;

        public int JobTypeId { get; set; }

        public Guid TransactionId { get; set; } = Guid.NewGuid();

        public Guid? UserId { get; set; }

        public string? Username { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public string? ErrorMessage { get; set; }
    }
}