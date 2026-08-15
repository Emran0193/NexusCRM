using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NexusCRM.Infrastructure.Identity;

namespace NexusCRM.Api.Realtime;

[Authorize]
public sealed class BoardHub : Hub
{
    public static string TenantGroup(Guid tenantId) => $"tenant:{tenantId}:boards";

    public override async Task OnConnectedAsync()
    {
        var tenantId = ResolveTenantId(Context.User);
        if (tenantId is null)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroup(tenantId.Value));
        await base.OnConnectedAsync();
    }

    private static Guid? ResolveTenantId(ClaimsPrincipal? user)
    {
        var value = user?.FindFirstValue(NexusClaimTypes.TenantId)
            ?? user?.FindFirstValue("tenant_id");
        return Guid.TryParse(value, out var tenantId) ? tenantId : null;
    }
}
