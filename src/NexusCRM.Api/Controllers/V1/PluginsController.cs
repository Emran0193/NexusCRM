using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusCRM.Api.Extensions;
using NexusCRM.Application.Plugins;
using NexusCRM.Contracts.Plugins;
using NexusCRM.Domain.Identity;
using NexusCRM.Infrastructure.Authorization;

namespace NexusCRM.Api.Controllers.V1;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/plugins")]
public sealed class PluginsController : ControllerBase
{
    private readonly ISender _sender;

    public PluginsController(ISender sender) => _sender = sender;

    [HttpGet]
    [RequirePermission(SystemPermissions.PluginsRead)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListPluginsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{pluginId}/toggle")]
    [RequirePermission(SystemPermissions.PluginsManage)]
    public async Task<IActionResult> Toggle(
        string pluginId,
        [FromBody] TogglePluginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new TogglePluginCommand(pluginId, request.IsEnabled), cancellationToken);
        return result.ToActionResult();
    }
}
