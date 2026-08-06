namespace KeepApi.Models.Request.Note
{
    public class CreateNoteRequest
    {
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;
        public string Color { get; set; } = "default";
    }
}
