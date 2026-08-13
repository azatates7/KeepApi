using KeepApi.Infrastructure.Llm;

namespace KeepApi.Services
{
    public record AttachmentSummaryResult(string Title, string Content);

    /// <summary>
    /// Yüklenen bir görsel/belgeyi LLM'e göndererek özetler ve not olarak
    /// kaydedilebilecek bir başlık + içerik üretir. Dosyanın kendisi
    /// hiçbir yerde saklanmaz; sadece LLM isteğinde kullanılır.
    /// </summary>
    public class AttachmentSummaryService
    {
        // İstemci tarafındaki (Composer.jsx) limitlerle tutarlı.
        public static readonly long MaxAttachmentBytes = 8 * 1024 * 1024; // 8MB

        public static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp", "image/gif", "image/heic", "image/heif",
            "application/pdf",
            "text/plain"
        };

        private readonly ILlmClient _llm;
        private readonly ILogger<AttachmentSummaryService> _logger;

        public AttachmentSummaryService(ILlmClient llm, ILogger<AttachmentSummaryService> logger)
        {
            _llm = llm;
            _logger = logger;
        }

        public async Task<AttachmentSummaryResult> SummarizeAsync(
            byte[] fileBytes,
            string mimeType,
            string fileName,
            CancellationToken ct)
        {
            const string prompt =
                "Ekteki görseli veya belgeyi incele ve bunu bir 'not uygulaması' kaydına dönüştür. " +
                "Yanıtını kesinlikle şu iki satır formatında ver, başka hiçbir açıklama ekleme:\n" +
                "BAŞLIK: <en fazla 10 kelimelik kısa bir başlık>\n" +
                "İÇERİK: <içeriğin veya görselin önemli noktalarının Türkçe, kısa ve maddeler halinde özeti>";

            var raw = await _llm.SummarizeAttachmentAsync(fileBytes, mimeType, prompt, ct);

            var (title, content) = ParseResponse(raw, fileName);

            _logger.LogInformation("Attachment {FileName} ({MimeType}, {Size} bytes) summarized into note.",
                fileName, mimeType, fileBytes.Length);

            return new AttachmentSummaryResult(title, content);
        }

        private static (string Title, string Content) ParseResponse(string raw, string fileName)
        {
            var titleLine = raw
                .Split('\n')
                .FirstOrDefault(l => l.TrimStart().StartsWith("BAŞLIK:", StringComparison.OrdinalIgnoreCase));

            var contentIndex = raw.IndexOf("İÇERİK:", StringComparison.OrdinalIgnoreCase);

            var title = titleLine?.Split(':', 2).ElementAtOrDefault(1)?.Trim();
            var content = contentIndex >= 0
                ? raw[(contentIndex + "İÇERİK:".Length)..].Trim()
                : raw.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                title = $"Özet: {fileName}";
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                content = "Bu dosya için bir özet oluşturulamadı.";
            }

            return (title, content);
        }
    }
}