using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NexusCRM.Api.Extensions;
using NexusCRM.Application.Auth;
using NexusCRM.Contracts.Auth;

namespace NexusCRM.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new LoginCommand(
                request.Email,
                request.Password,
                request.TenantId,
                request.MfaCode,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                Request.Headers.UserAgent.ToString(),
                HttpContext.TraceIdentifier),
            cancellationToken);

        return result.ToActionResult();
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RefreshTokenCommand(
                request.RefreshToken,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                Request.Headers.UserAgent.ToString(),
                HttpContext.TraceIdentifier),
            cancellationToken);

        return result.ToActionResult();
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new LogoutCommand(
                request.RefreshToken,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                HttpContext.TraceIdentifier),
            cancellationToken);

        return result.ToActionResult();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMeQuery(), cancellationToken);
        return result.ToActionResult();
    }
}
