using KeepApi.Common.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeepApi.Data.Entity
{
    [Table("Notes")]
    public class Note : BaseEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string? Title { get; set; }

        public string? Content { get; set; }

        public string Color { get; set; } = "default";

        [Column(TypeName = "NUMBER(1)")]
        public bool Pinned { get; set; }
        public DateTime? PinnedAt { get; set; }

        [Column(TypeName = "NUMBER(1)")]
        public bool Archived { get; set; }

        [Column(TypeName = "NUMBER(1)")]
        public bool IsDailySummary { get; set; }

        public DateTime? ArchievedAt { get; set; }

        [Column(TypeName = "NUMBER(1)")]
        public bool Checklist { get; set; }

        [Column(TypeName = "NUMBER(1)")]
        public bool ImageAdded { get; set; }
        public string? ImageUrl { get; set; }

        public DateTime? ReminderAt { get; set; }

        /// <summary>Hatırlatma e-postası gönderildiği an (null ise henüz gönderilmedi). ReminderNotificationJob tarafından yazılır.</summary>
        public DateTime? ReminderNotifiedAt { get; set; }

        public Guid UserId { get; set; }

        public ApplicationUser User { get; set; } = null!;

        public Guid? CreatedById { get; set; }
        public ApplicationUser CreatedBy { get; set; } = null!;

        public Guid? UpdatedById { get; set; }
        public ApplicationUser? UpdatedBy { get; set; }

        public Guid? DeletedById { get; set; }
        public ApplicationUser? DeletedBy { get; set; }
    }
}