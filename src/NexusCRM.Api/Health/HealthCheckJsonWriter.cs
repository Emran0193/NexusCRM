using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NexusCRM.Api.Health;

public static class HealthCheckJsonWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    durationMs = e.Value.Duration.TotalMilliseconds,
                    error = e.Value.Exception?.Message
                })
        };

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }),
            Encoding.UTF8);
    }
}
