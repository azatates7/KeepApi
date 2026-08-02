using KeepApi.Data.Common;

namespace KeepApi.Models
{
    public class UpdateNoteRequest : BaseEntity
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string Color { get; set; } = "default";
        public bool Pinned { get; set; }
        public DateTime? PinnedAt { get; set; }
        public bool Archived { get; set; }
        public DateTime? ArchievedAt { get; set; }
        public DateTime? ReminderAt { get; set; }
    }
}
