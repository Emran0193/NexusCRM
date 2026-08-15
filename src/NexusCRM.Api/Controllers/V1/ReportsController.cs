using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusCRM.Api.Extensions;
using NexusCRM.Application.Reports;
using NexusCRM.Domain.Identity;
using NexusCRM.Infrastructure.Authorization;

namespace NexusCRM.Api.Controllers.V1;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportsController(ISender sender) => _sender = sender;

    [HttpGet("summary")]
    [RequirePermission(SystemPermissions.ReportsRead)]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCrmSummaryReportQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("pipelines")]
    [RequirePermission(SystemPermissions.ReportsRead)]
    public async Task<IActionResult> Pipelines(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPipelineFunnelsQuery(), cancellationToken);
        return result.ToActionResult();
    }
}
