using Microsoft.Extensions.Configuration;
using Polly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace KeepApi.Infrastructure.Llm
{
    /// <summary>
    /// OpenAI (ChatGPT) Chat Completions API üzerinden özet üretir. GeminiLlmClient ile aynı ILlmClient sözleşmesini uygular; Program.cs'teki DI kaydı Llm:Provider ayarına göre bu ikisinden birini seçer.
    /// </summary>
    public class OpenAiLlmClient : ILlmClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public OpenAiLlmClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> SummarizeAsync(string prompt, CancellationToken cancellationToken)
        {
            var apiKey = _configuration["Llm:OpenAI:ApiKey"] ?? throw new InvalidOperationException("Llm:OpenAI:ApiKey yok.");

            var model = _configuration["Llm:OpenAI:Model"] ?? "gpt-4o-mini";

            var baseUrl = _configuration["Llm:OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = JsonContent.Create(new
            {
                model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            });

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            return json.GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString() ?? string.Empty;
        }

        public async Task<string> SummarizeAttachmentAsync(byte[] fileBytes, string mimeType, string prompt, CancellationToken cancellationToken)
        {
            var apiKey = _configuration["Llm:OpenAI:ApiKey"]
                ?? throw new InvalidOperationException("Llm:OpenAI:ApiKey yok.");

            var model = _configuration["Llm:OpenAI:Model"]
                ?? "gpt-4o-mini";

            var baseUrl = _configuration["Llm:OpenAI:BaseUrl"] ??
                          "https://api.openai.com/v1";

            var base64Data = Convert.ToBase64String(fileBytes);

            object attachmentContent;

            if (mimeType.StartsWith(
                    "image/",
                    StringComparison.OrdinalIgnoreCase))
            {
                attachmentContent = new
                {
                    type = "input_image",
                    image_url = $"data:{mimeType};base64,{base64Data}"
                };
            }
            else if (mimeType.Equals(
                         "application/pdf",
                         StringComparison.OrdinalIgnoreCase))
            {
                attachmentContent = new
                {
                    type = "input_file",
                    file_data = $"data:application/pdf;base64,{base64Data}"
                };
            }
            else
            {
                var textContent = Encoding.UTF8.GetString(fileBytes);

                attachmentContent = new
                {
                    type = "input_text",
                    text = textContent
                };
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{baseUrl.TrimEnd('/')}/responses");

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    apiKey);

            request.Content = JsonContent.Create(new
            {
                model,
                input = new object[]
                {
            new
            {
                role = "user",
                content = new object[]
                {
                    new
                    {
                        type = "input_text",
                        text = prompt
                    },
                    attachmentContent
                }
            }
                }
            });

            var retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(
                    r => r.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(
                        Math.Pow(2, retryAttempt)));

            using var response = await retryPolicy.ExecuteAsync(
                () => _httpClient.SendAsync(request, cancellationToken));

            //response.EnsureSuccessStatusCode();
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                // logla: status code + errorBody
                throw new HttpRequestException($"OpenAI API hatası ({(int)response.StatusCode}): {errorBody}");
            }

            var json = await response.Content
                .ReadFromJsonAsync<JsonElement>(
                       cancellationToken: cancellationToken);

            // Responses API output yapısından text'i al
            if (json.TryGetProperty("output_text", out var outputText))
            {
                return outputText.GetString() ?? string.Empty;
            }

            if (json.TryGetProperty("output", out var output))
            {
                foreach (var item in output.EnumerateArray())
                {
                    if (!item.TryGetProperty("content", out var content))
                        continue;

                    foreach (var contentItem in content.EnumerateArray())
                    {
                        if (!contentItem.TryGetProperty("text", out var text))
                            continue;

                        return text.GetString() ?? string.Empty;
                    }
                }
            }

            return string.Empty;
        }
    }
}