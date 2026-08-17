using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using UglyToad.PdfPig;

namespace KeepApi.Infrastructure.Llm
{
    /// <summary>
    /// Ollama'nın OpenAI uyumlu /v1/chat/completions endpoint'i üzerinden
    /// yerel LLM ile özet üretir. GeminiLlmClient/OpenAiLlmClient ile aynı
    /// ILlmClient sözleşmesini uygular; Program.cs'teki DI kaydı
    /// Llm:Provider ayarına göre bu üçünden birini seçer.
    ///
    /// gemma4 gibi doğal çok-modlu (multimodal) modeller görselleri
    /// doğrudan işleyebilir, bu yüzden ayrı bir vision modeli şart değil.
    /// PDF için ise yerel modeller ham binary'yi işleyemediğinden PdfPig
    /// ile metin çıkarılıp text modeline gönderilir.
    /// </summary>
    public class OllamaLlmClient : ILlmClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public OllamaLlmClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        private string BaseUrl => (_configuration["Llm:Ollama:BaseUrl"] ?? "http://localhost:11434/v1").TrimEnd('/');
        private string TextModel => _configuration["Llm:Ollama:Model"] ?? "gemma4:e4b";
        private string VisionModel => _configuration["Llm:Ollama:VisionModel"] ?? TextModel;

        public async Task<string> SummarizeAsync(string prompt, CancellationToken ct)
        {
            return await SendChatAsync(TextModel, new object[]
            {
                new { role = "user", content = prompt }
            }, ct);
        }

        public async Task<string> SummarizeAttachmentAsync(byte[] fileBytes, string mimeType, string prompt, CancellationToken ct)
        {
            if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                var base64Data = Convert.ToBase64String(fileBytes);
                var content = new object[]
                {
                    new { type = "text", text = prompt },
                    new { type = "image_url", image_url = new { url = $"data:{mimeType};base64,{base64Data}" } }
                };

                return await SendChatAsync(VisionModel, new object[]
                {
                    new { role = "user", content }
                }, ct);
            }

            if (mimeType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                var extractedText = ExtractPdfText(fileBytes);
                var combinedPrompt = $"{prompt}\n\n--- BELGE İÇERİĞİ ---\n{extractedText}";
                return await SendChatAsync(TextModel, new object[]
                {
                    new { role = "user", content = combinedPrompt }
                }, ct);
            }

            // text/plain vb.
            var textContent = Encoding.UTF8.GetString(fileBytes);
            var combined = $"{prompt}\n\n--- DOSYA İÇERİĞİ ---\n{textContent}";
            return await SendChatAsync(TextModel, new object[]
            {
                new { role = "user", content = combined }
            }, ct);
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
            // Çok uzun belgelerde context window taşmasını önlemek için kısalt.
            const int maxChars = 20_000;
            return text.Length > maxChars ? text[..maxChars] + "\n[...kırpıldı...]" : text;
        }

        private async Task<string> SendChatAsync(string model, object[] messages, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "ollama"); // Ollama auth istemez, dummy değer yeterli
            request.Content = JsonContent.Create(new
            {
                model,
                messages,
                stream = false
                //options = new
                //{
                //    num_predict = 500,   // maksimum üretilecek token — özet için fazlasıyla yeterli
                //    num_ctx = 4096,        // context penceresini gereksiz büyütme, işlem hızını artırır
                //    maxChars = 6_000,
                //    think = false
                //}
            });

            try
            {
                using var response = await _httpClient.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct);
                    throw new HttpRequestException($"Ollama API hatası ({(int)response.StatusCode}): {errorBody}");
                }

                var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

                if (json.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var message = choices[0].GetProperty("message");
                    if (message.TryGetProperty("content", out var contentProp))
                    {
                        return contentProp.GetString() ?? string.Empty;
                    }
                }
            }
            catch(Exception exception)
            {
                return exception?.StackTrace?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }
    }
}