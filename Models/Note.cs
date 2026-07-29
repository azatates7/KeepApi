namespace KeepApi.Models;

public class Note
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N"); // without -
    public string Title { get; set; }
    public string Content { get; set; }

    // default | sage | sky | sand | blush | lilac
    public string Color { get; set; } = "default";

    public bool Pinned { get; set; } = false;
    public DateTime? PinnedAt { get; set; }

    public bool Archived { get; set; } = false;
    public DateTime? ArchievedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public DateTime? ReminderAt { get; set; } = null;
    public int Status { get; set; } = 1;
    public bool IsDeleted { get; set; } = false;
}
