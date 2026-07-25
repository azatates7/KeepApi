namespace KeepApi.Models;

public class Note
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N"); // without -
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    // default | sage | sky | sand | blush | lilac
    public string Color { get; set; } = "default";

    public bool Pinned { get; set; } = false;
    public bool Archived { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public DateTime? ReminderAt { get; set; } = null;
    public int Status { get; set; } = 1;
    public bool IsDeleted { get; set; } = false;
}
