using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusCRM.Api.Extensions;
using NexusCRM.Application.Notifications;

namespace NexusCRM.Api.Controllers.V1;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int take = 40, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListNotificationsQuery(take), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new MarkNotificationReadCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new MarkAllNotificationsReadCommand(), cancellationToken);
        return result.ToActionResult();
    }
}
