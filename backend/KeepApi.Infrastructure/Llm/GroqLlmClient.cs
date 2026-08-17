using Microsoft.Extensions.Configuration;
using Polly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;

namespace KeepApi.Infrastructure.Llm
{
    /// <summary>
    /// Groq'un OpenAI uyumlu /v1/chat/completions endpoint'i üzerinden
    /// ücretsiz katmanla (kredi kartı gerekmez) hızlı bulut LLM erişimi
    /// sağlar. GeminiLlmClient/OpenAiLlmClient/OllamaLlmClient ile aynı
    /// ILlmClient sözleşmesini uygular; Program.cs'teki DI kaydı
    /// Llm:Provider ayarına göre bunlardan birini seçer.
    ///
    /// Groq, OpenAI'ın Responses API'sindeki gibi native PDF girişini
    /// desteklemiyor (sadece text + image_url) — bu yüzden PDF'ler
    /// OllamaLlmClient'takiyle aynı yöntemle PdfPig ile metne çevrilip
    /// gönderiliyor. Ücretsiz katmanın rate limit'leri Ollama'ya göre
    /// çok daha sıkı olduğundan 429 için OpenAiLlmClient'taki gibi
    /// exponential backoff retry uygulanıyor.
    /// </summary>
    public class GroqLlmClient : ILlmClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GroqLlmClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        private string ApiKey => _configuration["Llm:Groq:ApiKey"]
            ?? throw new InvalidOperationException("Llm:Groq:ApiKey yok.");

        private string BaseUrl => (_configuration["Llm:Groq:BaseUrl"] ?? "https://api.groq.com/openai/v1").TrimEnd('/');
        private string TextModel => _configuration["Llm:Groq:Model"] ?? "openai/gpt-oss-20b";
        private string VisionModel => _configuration["Llm:Groq:VisionModel"] ?? "qwen/qwen3.6-27b";

        public async Task<string> SummarizeAsync(string prompt, CancellationToken cancellationToken)
        {
            return await SendChatAsync(TextModel, new object[]
            {
                new { role = "user", content = prompt }
            }, cancellationToken);
        }

        public async Task<string> SummarizeAttachmentAsync(byte[] fileBytes, string mimeType, string prompt, CancellationToken cancellationToken)
        {
            if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                var base64Data = Convert.ToBase64String(fileBytes);
                var content = new object[]
                {
                    new { type = "text", text = prompt },
                    new { type = "image_url", image_url = new { url = $"data:{mimeType};base64,{base64Data}" } }
                };

                // Not: qwen/qwen3.6-27b şu an Groq'ta "preview" statüsünde —
                // Groq'un modeli üretimden kaldırması/değiştirmesi ihtimaline karşı
                // console.groq.com/docs/models'tan güncel vision modelini kontrol et.
                return await SendChatAsync(VisionModel, new object[]
                {
                    new { role = "user", content }
                }, cancellationToken);
            }

            if (mimeType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                var extractedText = ExtractPdfText(fileBytes);
                var combinedPrompt = $"{prompt}\n\n--- BELGE İÇERİĞİ ---\n{extractedText}";
                return await SendChatAsync(TextModel, new object[]
                {
                    new { role = "user", content = combinedPrompt }
                }, cancellationToken);
            }

            // text/plain vb.
            var textContent = Encoding.UTF8.GetString(fileBytes);
            var combined = $"{prompt}\n\n--- DOSYA İÇERİĞİ ---\n{textContent}";
            return await SendChatAsync(TextModel, new object[]
            {
                new { role = "user", content = combined }
            }, cancellationToken);
        }

        private static string ExtractPdfText(byte[] fileBytes)
        {
            using var pdf = PdfDocument.Open(fileBytes);
            var sb = new StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                sb.AppendLine(page.Text);
            }

            var text = sb.ToString();
            const int maxChars = 20_000; // bulut modeli olduğu için Ollama'dakinden daha yüksek tutulabilir
            return text.Length > maxChars ? text[..maxChars] + "\n[...kırpıldı...]" : text;
        }

        private async Task<string> SendChatAsync(string model, object[] messages, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            request.Content = JsonContent.Create(new
            {
                model,
                messages,
                max_completion_tokens = 800,
                stream = false
            });

            // Ücretsiz katmanın RPM/TPM limitleri düşük — 429 dönüşü alındığında exponential backoff ile 3 deneme yapılır (OpenAiLlmClient ile aynı desen).
            var retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            using var response = await retryPolicy.ExecuteAsync(
                () => _httpClient.SendAsync(request, cancellationToken));

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Groq API hatası ({(int)response.StatusCode}): {errorBody}");
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

            if (json.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var message = choices[0].GetProperty("message");
                if (message.TryGetProperty("content", out var contentProp))
                {
                    return contentProp.GetString() ?? string.Empty;
                }
            }

            return string.Empty;
        }
    }
}