using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusCRM.Api.Extensions;
using NexusCRM.Application.Search;

namespace NexusCRM.Api.Controllers.V1;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/search")]
public sealed class SearchController : ControllerBase
{
    private readonly ISender _sender;

    public SearchController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] int takePerType = 8,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GlobalSearchQuery(q, takePerType), cancellationToken);
        return result.ToActionResult();
    }
}
