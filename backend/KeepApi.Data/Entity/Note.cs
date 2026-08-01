using KeepApi.Data.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

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
        public DateTime? ArchievedAt { get; set; }

        public DateTime? ReminderAt { get; set; }

        public Guid UserId { get; set; }

        public ApplicationUser User { get; set; } = null!;
    }
}
