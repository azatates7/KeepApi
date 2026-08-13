using KeepApi.Common.Models;

namespace KeepApi.Models.Request.Note
{
    public class UpdateNoteRequest : BaseEntity
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string Color { get; set; } = "default";
        public bool Checklist { get; set; }
        public bool ImageAdded { get; set; }
        public string? ImageUrl { get; set; }
        public bool Pinned { get; set; }
        public DateTime? PinnedAt { get; set; }
        public bool Archived { get; set; }
        public DateTime? ArchievedAt { get; set; }
        public DateTime? ReminderAt { get; set; }
    }
}
