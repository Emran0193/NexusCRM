using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusCRM.Api.Extensions;
using NexusCRM.Application.Workflows;
using NexusCRM.Contracts.Workflows;
using NexusCRM.Domain.Identity;
using NexusCRM.Infrastructure.Authorization;

namespace NexusCRM.Api.Controllers.V1;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/workflows")]
public sealed class WorkflowsController : ControllerBase
{
    private readonly ISender _sender;

    public WorkflowsController(ISender sender) => _sender = sender;

    [HttpGet]
    [RequirePermission(SystemPermissions.WorkflowsRead)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListWorkflowsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("runs")]
    [RequirePermission(SystemPermissions.WorkflowsRead)]
    public async Task<IActionResult> Runs([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListWorkflowRunsQuery(take), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/toggle")]
    [RequirePermission(SystemPermissions.WorkflowsManage)]
    public async Task<IActionResult> Toggle(
        Guid id,
        [FromBody] ToggleWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ToggleWorkflowCommand(id, request.IsEnabled), cancellationToken);
        return result.ToActionResult();
    }
}
