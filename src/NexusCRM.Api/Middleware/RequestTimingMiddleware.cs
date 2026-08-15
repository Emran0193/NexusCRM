using System.Diagnostics;

namespace NexusCRM.Api.Middleware;

public sealed class RequestTimingMiddleware
{
    public const string HeaderName = "X-Response-Time-Ms";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;
    private readonly long _slowRequestThresholdMs;

    public RequestTimingMiddleware(
        RequestDelegate next,
        ILogger<RequestTimingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _slowRequestThresholdMs = configuration.GetValue("Performance:SlowRequestThresholdMs", 500);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = stopwatch.ElapsedMilliseconds.ToString();
            return Task.CompletedTask;
        });

        await _next(context);
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds >= _slowRequestThresholdMs &&
            !context.Request.Path.StartsWithSegments("/health") &&
            !context.Request.Path.StartsWithSegments("/metrics"))
        {
            _logger.LogWarning(
                "Slow request {Method} {Path} completed in {ElapsedMs}ms with {StatusCode}",
                context.Request.Method,
                context.Request.Path.Value,
                stopwatch.ElapsedMilliseconds,
                context.Response.StatusCode);
        }
    }
}
