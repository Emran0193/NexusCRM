using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusCRM.Api.Extensions;
using NexusCRM.Application.Leads;
using NexusCRM.Contracts.Leads;
using NexusCRM.Domain.Identity;
using NexusCRM.Infrastructure.Authorization;

namespace NexusCRM.Api.Controllers.V1;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/leads")]
public sealed class LeadsController : ControllerBase
{
    private readonly ISender _sender;

    public LeadsController(ISender sender) => _sender = sender;

    [HttpGet("board")]
    [RequirePermission(SystemPermissions.LeadsRead)]
    public async Task<IActionResult> Board(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLeadBoardQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [RequirePermission(SystemPermissions.LeadsWrite)]
    public async Task<IActionResult> Create([FromBody] CreateLeadRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateLeadCommand(
                request.Title,
                request.Source,
                request.Email,
                request.Phone,
                request.CompanyName,
                request.CustomerId,
                request.StageId),
            cancellationToken);

        return result.IsFailure
            ? result.ToActionResult()
            : result.ToCreatedResult($"/api/v1/leads/{result.Value.Id}");
    }

    [HttpPost("{id:guid}/move")]
    [RequirePermission(SystemPermissions.LeadsWrite)]
    public async Task<IActionResult> Move(
        Guid id,
        [FromBody] MoveLeadStageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new MoveLeadStageCommand(id, request.StageId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/score")]
    [RequirePermission(SystemPermissions.LeadsWrite)]
    public async Task<IActionResult> SetScore(
        Guid id,
        [FromBody] SetLeadScoreRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetLeadScoreCommand(id, request.Score), cancellationToken);
        return result.ToActionResult();
    }
}
