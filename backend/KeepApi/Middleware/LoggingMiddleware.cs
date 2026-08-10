using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;

namespace KeepApi.Middleware;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    // Varsayılan System.Text.Json encoder'ı ASCII-dışı karakterleri (ş, ı, ğ, ö, ü, ç vb.) \uXXXX olarak escape eder. Loglarda okunabilir Türkçe metin için bunu gevşetiyoruz.
    private static readonly JsonSerializerOptions RedactSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    // Bu alan adları (büyük/küçük harf duyarsız), request/response JSON gövdesinde hangi derinlikte olurlarsa olsunlar "***" ile maskelenir.
    private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "oldPassword",
        "newPassword",
        "confirmPassword",
        "confirmNewPassword",
        "currentPassword",
        "token",
        "accessToken",
        "refreshToken",
        "code",
        "otp",
        "tckn",
        "Hesabınızı doğrulamak için kodunuz"
    };

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var request = context.Request;

        request.EnableBuffering();

        string requestBody = "";

        if (request.ContentLength > 0)
        {
            using var reader = new StreamReader(
                request.Body,
                Encoding.UTF8,
                leaveOpen: true);

            requestBody = await reader.ReadToEndAsync();
            request.Body.Position = 0;
        }

        var originalBody = context.Response.Body;

        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var watch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await _next(context);

            watch.Stop();

            responseBody.Position = 0;

            var response = await new StreamReader(responseBody).ReadToEndAsync();

            responseBody.Position = 0;
            await responseBody.CopyToAsync(originalBody);

            _logger.LogInformation(
                """
            HTTP Request
            Method: {Method}
            Path: {Path}
            StatusCode: {StatusCode}
            Duration: {Duration}
            Request: {Request}
            Response: {Response}
            """,
                request.Method,
                request.Path,
                context.Response.StatusCode,
                watch.ElapsedMilliseconds,
                ReformatJson(requestBody),
                ReformatJson(response));
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    /// <summary>
    /// Verilen JSON metnini parse edip SensitiveFields içindeki alan adlarını (iç içe objeler/diziler dahil, her derinlikte) "***" ile değiştirir. JSON parse edilemezse (boş body, multipart form, vb.) metni olduğu gibi döner.
    /// </summary>
    private static string ReformatJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        try
        {
            var node = JsonNode.Parse(json);
            ReformatNode(node);
            return node?.ToJsonString(RedactSerializerOptions) ?? json; ;
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private static void ReformatNode(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(kv => kv.Key).ToList())
                {
                    if (SensitiveFields.Contains(key))
                    {
                        obj[key] = "***";
                    }
                    else
                    {
                        ReformatNode(obj[key]);
                    }
                }
                break;

            case JsonArray arr:
                foreach (var item in arr)
                {
                    ReformatNode(item);
                }
                break;
        }
    }
}