using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusCRM.Api.Extensions;
using NexusCRM.Application.Deals;
using NexusCRM.Contracts.Deals;
using NexusCRM.Domain.Identity;
using NexusCRM.Infrastructure.Authorization;

namespace NexusCRM.Api.Controllers.V1;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/deals")]
public sealed class DealsController : ControllerBase
{
    private readonly ISender _sender;

    public DealsController(ISender sender) => _sender = sender;

    [HttpGet("board")]
    [RequirePermission(SystemPermissions.DealsRead)]
    public async Task<IActionResult> Board(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDealBoardQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [RequirePermission(SystemPermissions.DealsWrite)]
    public async Task<IActionResult> Create([FromBody] CreateDealRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateDealCommand(
                request.Title,
                request.Amount,
                request.Currency,
                request.CustomerId,
                request.LeadId,
                request.ExpectedCloseDate,
                request.StageId),
            cancellationToken);

        return result.IsFailure
            ? result.ToActionResult()
            : result.ToCreatedResult($"/api/v1/deals/{result.Value.Id}");
    }

    [HttpPost("{id:guid}/move")]
    [RequirePermission(SystemPermissions.DealsWrite)]
    public async Task<IActionResult> Move(
        Guid id,
        [FromBody] MoveDealStageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new MoveDealStageCommand(id, request.StageId, request.RowVersion),
            cancellationToken);
        return result.ToActionResult();
    }
}
