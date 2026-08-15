using System.Diagnostics;
using NexusCRM.Application.Abstractions.Tenancy;

namespace NexusCRM.Api.Middleware;

/// <summary>
/// Pushes tenant/user into the current Activity and logging scope after auth/tenant resolution.
/// </summary>
public sealed class ObservabilityEnrichmentMiddleware
{
    private readonly RequestDelegate _next;

    public ObservabilityEnrichmentMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        ICurrentUser currentUser)
    {
        var activity = Activity.Current;
        var scope = new Dictionary<string, object?>();

        if (tenantContext.TenantId is { } tenantId)
        {
            activity?.SetTag("tenant.id", tenantId.ToString());
            scope["TenantId"] = tenantId;
        }

        if (currentUser.UserId is { } userId)
        {
            activity?.SetTag("user.id", userId.ToString());
            scope["UserId"] = userId;
        }

        if (scope.Count == 0)
        {
            await _next(context);
            return;
        }

        using (context.RequestServices
                   .GetRequiredService<ILoggerFactory>()
                   .CreateLogger("Observability")
                   .BeginScope(scope!))
        {
            await _next(context);
        }
    }
}
