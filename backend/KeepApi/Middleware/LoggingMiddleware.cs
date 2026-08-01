using System.Text;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace KeepApi.Middleware;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;

    public LoggingMiddleware(RequestDelegate next)
    {
        _next = next;
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

        using var responseBody = new MemoryStream();

        context.Response.Body = responseBody;

        var watch = System.Diagnostics.Stopwatch.StartNew();

        await _next(context);

        watch.Stop();

        responseBody.Position = 0;

        var response = await new StreamReader(responseBody).ReadToEndAsync();

        responseBody.Position = 0;

        await responseBody.CopyToAsync(originalBody);

        Log.Information(
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
            requestBody,
            response);
    }
}