using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeepApi.Infrastructure.Authentication.External
{
    public sealed class ExternalOAuthClient : IExternalOAuthClient
    {
        private readonly HttpClient _httpClient;
        private readonly ExternalProvidersSettings _settings;
        private readonly ILogger<ExternalOAuthClient> _logger;

        public ExternalOAuthClient(
            HttpClient httpClient,
            IOptions<ExternalProvidersSettings> settings,
            ILogger<ExternalOAuthClient> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public Task<ExternalUserInfo> ExchangeAndGetUserInfoAsync(string provider, string code, string redirectUri)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ExternalAuthException("Sağlayıcıdan bir yetkilendirme kodu alınamadı.");
            }

            return provider.ToLowerInvariant() switch
            {
                "google" => HandleGoogleAsync(code, redirectUri),
                "microsoft" => HandleMicrosoftAsync(code, redirectUri),
                "github" => HandleGitHubAsync(code, redirectUri),
                _ => throw new ExternalAuthException($"Desteklenmeyen giriş sağlayıcısı: {provider}")
            };
        }

        // ---------------- Google ----------------

        private async Task<ExternalUserInfo> HandleGoogleAsync(string code, string redirectUri)
        {
            var client = _httpClient;

            var tokenResponse = await PostFormAsync(client, "https://oauth2.googleapis.com/token", new Dictionary<string, string>
            {
                ["client_id"] = _settings.Google.ClientId,
                ["client_secret"] = _settings.Google.ClientSecret,
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            }, "Google");

            var accessToken = RequireField(tokenResponse, "access_token", "Google");

            using var req = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var userInfo = await SendAndParseAsync(client, req, "Google");

            return new ExternalUserInfo(
                ProviderKey: RequireField(userInfo, "sub", "Google"),
                Email: RequireField(userInfo, "email", "Google"),
                EmailVerified: GetBool(userInfo, "email_verified"),
                FirstName: GetString(userInfo, "given_name"),
                LastName: GetString(userInfo, "family_name"));
        }

        // ---------------- Microsoft (Entra ID / Microsoft Graph) ----------------

        private async Task<ExternalUserInfo> HandleMicrosoftAsync(string code, string redirectUri)
        {
            var client = _httpClient;

            var tokenResponse = await PostFormAsync(client, "https://login.microsoftonline.com/common/oauth2/v2.0/token", new Dictionary<string, string>
            {
                ["client_id"] = _settings.Microsoft.ClientId,
                ["client_secret"] = _settings.Microsoft.ClientSecret,
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code",
                ["scope"] = "openid profile email User.Read"
            }, "Microsoft");

            var accessToken = RequireField(tokenResponse, "access_token", "Microsoft");

            using var req = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var profile = await SendAndParseAsync(client, req, "Microsoft");

            var email = GetString(profile, "mail");
            if (string.IsNullOrWhiteSpace(email))
            {
                email = GetString(profile, "userPrincipalName");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ExternalAuthException("Microsoft hesabınızda bir e-posta adresi bulunamadı.");
            }

            return new ExternalUserInfo(
                ProviderKey: RequireField(profile, "id", "Microsoft"),
                Email: email,
                EmailVerified: true, // Microsoft/Entra hesap e-postaları doğrulanmış kabul edilir.
                FirstName: GetString(profile, "givenName"),
                LastName: GetString(profile, "surname"));
        }

        // ---------------- GitHub ----------------

        private async Task<ExternalUserInfo> HandleGitHubAsync(string code, string redirectUri)
        {
            var client = _httpClient;

            using var tokenReq = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token");
            tokenReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            tokenReq.Headers.UserAgent.ParseAdd("KeepApi");
            tokenReq.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _settings.GitHub.ClientId,
                ["client_secret"] = _settings.GitHub.ClientSecret,
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            });
            var tokenResponse = await SendAndParseAsync(client, tokenReq, "GitHub");
            var accessToken = RequireField(tokenResponse, "access_token", "GitHub");

            using var userReq = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
            userReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            userReq.Headers.UserAgent.ParseAdd("KeepApi");
            var profile = await SendAndParseAsync(client, userReq, "GitHub");

            var email = GetString(profile, "email");

            if (string.IsNullOrWhiteSpace(email))
            {
                // GitHub profildeki e-postayı gizli tutabilir; doğrulanmış birincil e-postayı ayrıca çekiyoruz.
                using var emailsReq = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
                emailsReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                emailsReq.Headers.UserAgent.ParseAdd("KeepApi");

                var emailsRes = await client.SendAsync(emailsReq);
                if (emailsRes.IsSuccessStatusCode)
                {
                    var json = await emailsRes.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        var primary = item.TryGetProperty("primary", out var p) && p.GetBoolean();
                        var verified = item.TryGetProperty("verified", out var v) && v.GetBoolean();
                        if (primary && verified && item.TryGetProperty("email", out var e))
                        {
                            email = e.GetString();
                            break;
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ExternalAuthException(
                    "GitHub hesabınızda doğrulanmış herkese açık bir e-posta bulunamadı. " +
                    "GitHub ayarlarınızdan e-postanızı doğrulayıp tekrar deneyin.");
            }

            var fullName = GetString(profile, "name");
            var (firstName, lastName) = SplitName(fullName);

            return new ExternalUserInfo(
                ProviderKey: RequireField(profile, "id", "GitHub"),
                Email: email,
                EmailVerified: true,
                FirstName: firstName,
                LastName: lastName);
        }

        // ---------------- helpers ----------------

        private async Task<JsonElement> PostFormAsync(HttpClient client, string url, Dictionary<string, string> form, string providerName)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(form)
            };
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return await SendAndParseAsync(client, req, providerName);
        }

        private async Task<JsonElement> SendAndParseAsync(HttpClient client, HttpRequestMessage request, string providerName)
        {
            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("{Provider} OAuth isteği başarısız. Status: {Status} Body: {Body}", providerName, response.StatusCode, body);
                throw new ExternalAuthException($"{providerName} ile giriş doğrulanamadı. Lütfen tekrar deneyin.");
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                return doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                _logger.LogWarning("{Provider} yanıtı JSON olarak ayrıştırılamadı: {Body}", providerName, body);
                throw new ExternalAuthException($"{providerName} yanıtı işlenemedi.");
            }
        }

        private static string RequireField(JsonElement element, string fieldName, string providerName)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(fieldName, out var value) &&
                value.ValueKind is JsonValueKind.String or JsonValueKind.Number)
            {
                var str = value.ValueKind == JsonValueKind.Number
                    ? value.GetRawText()
                    : value.GetString();

                if (!string.IsNullOrWhiteSpace(str))
                {
                    return str!;
                }
            }

            throw new ExternalAuthException($"{providerName} yanıtında beklenen '{fieldName}' alanı bulunamadı.");
        }

        private static string GetString(JsonElement element, string fieldName)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(fieldName, out var value) &&
                value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? string.Empty;
            }

            return string.Empty;
        }

        private static bool GetBool(JsonElement element, string fieldName)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(fieldName, out var value))
            {
                return false;
            }

            return value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => bool.TryParse(value.GetString(), out var b) && b,
                _ => false
            };
        }

        private static (string FirstName, string LastName) SplitName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return (string.Empty, string.Empty);
            }

            var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => (string.Empty, string.Empty),
                1 => (parts[0], string.Empty),
                _ => (parts[0], parts[1])
            };
        }
    }
}