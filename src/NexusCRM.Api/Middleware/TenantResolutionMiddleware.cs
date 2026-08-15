using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using NexusCRM.Infrastructure.Identity;
using NexusCRM.Infrastructure.Tenancy;

namespace NexusCRM.Api.Middleware;

/// <summary>
/// Resolves tenant and current user from JWT claims first, then falls back to headers (dev/tools).
/// </summary>
public sealed class TenantResolutionMiddleware
{
    public const string TenantIdHeader = "X-Tenant-Id";
    public const string TenantSlugHeader = "X-Tenant-Slug";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext, CurrentUser currentUser)
    {
        var principal = context.User;
        if (principal.Identity?.IsAuthenticated == true)
        {
            var userIdValue = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email)
                ?? principal.FindFirstValue(ClaimTypes.Email);
            var tenantClaim = principal.FindFirstValue(NexusClaimTypes.TenantId);
            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
            var permissions = principal.FindAll(NexusClaimTypes.Permission).Select(c => c.Value).ToArray();

            if (Guid.TryParse(userIdValue, out var userId))
            {
                Guid? tenantId = Guid.TryParse(tenantClaim, out var parsedTenant) ? parsedTenant : null;
                currentUser.Set(userId, email, tenantId, roles, permissions);
                if (tenantId.HasValue)
                {
                    tenantContext.Set(tenantId.Value);
                }
            }
        }

        // Header fallback for local tooling when unauthenticated, or explicit tenant switch header.
        if (!tenantContext.IsResolved
            && context.Request.Headers.TryGetValue(TenantIdHeader, out var tenantIdValue)
            && Guid.TryParse(tenantIdValue, out var headerTenantId))
        {
            var slug = context.Request.Headers.TryGetValue(TenantSlugHeader, out var slugValue)
                ? slugValue.ToString()
                : null;
            tenantContext.Set(headerTenantId, slug);
        }

        await _next(context);
    }
}
