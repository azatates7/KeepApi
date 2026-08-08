namespace KeepApi.Models.Request.Note
{
    public class CreateNoteRequest
    {
        public string Title { get; set; } = "Yeni Not";
        public string Content { get; set; } = string.Empty;
        public string Color { get; set; } = "default";
        public bool Checklist { get; set; }
        public bool ImageAdded { get; set; }
        public string? ImageUrl { get; set; }
    }
}
