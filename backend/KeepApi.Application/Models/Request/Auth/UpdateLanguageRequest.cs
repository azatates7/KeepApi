namespace KeepApi.Application.Models.Request.Auth
{
    public class UpdateLanguageRequest
    {
        /// <summary>"tr" | "en"</summary>
        public string Language { get; set; } = string.Empty;
    }
}