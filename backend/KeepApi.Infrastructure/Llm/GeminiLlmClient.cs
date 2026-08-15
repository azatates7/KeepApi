using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace KeepApi.Infrastructure.Llm
{
    public class GeminiLlmClient : ILlmClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GeminiLlmClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> SummarizeAsync(string prompt, CancellationToken ct)
        {
            var apiKey = _configuration["Llm:ApiKey"] ?? throw new InvalidOperationException("Llm:ApiKey yok.");

            var model = //_configuration["Llm:Model"] ??
                        "gemini-3.6-flash";

            var baseUrl = //_configuration["Llm:BaseUrl"] ??
                          "https://generativelanguage.googleapis.com/v1beta";

            var url = $"{baseUrl}/models/{model}:generateContent?key={apiKey}";

            var response = await _httpClient.PostAsJsonAsync(url, new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            }, ct);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            return json.GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? string.Empty;
        }

        public async Task<string> SummarizeAttachmentAsync(byte[] fileBytes, string mimeType, string prompt, CancellationToken ct)
        {
            var apiKey = _configuration["Llm:ApiKey"] ?? throw new InvalidOperationException("Llm:ApiKey yok.");

            var model = //_configuration["Llm:Model"] ?? 
                "gemini-3.6-flash";

            var baseUrl = //_configuration["Llm:BaseUrl"] ?? 
                "https://generativelanguage.googleapis.com/v1beta";

            var url = $"{baseUrl}/models/{model}:generateContent?key={apiKey}";

            var response = await _httpClient.PostAsJsonAsync(url, new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt },
                            new
                            {
                                inlineData = new
                                { 
                                    mimeType,
                                    data = Convert.ToBase64String(fileBytes)
                                }
                            }
                        }
                    }
                }
            }, ct);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            return json.GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? string.Empty;
        }
    }
}