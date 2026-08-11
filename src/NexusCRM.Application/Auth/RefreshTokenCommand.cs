using FluentValidation;
using Microsoft.Extensions.Options;
using NexusCRM.Application.Abstractions.Identity;
using NexusCRM.Application.Abstractions.Messaging;
using NexusCRM.Application.Abstractions.Persistence;
using NexusCRM.Contracts.Auth;
using NexusCRM.Domain.Identity;
using NexusCRM.Shared.Results;

namespace NexusCRM.Application.Auth;

public sealed record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress,
    string? UserAgent,
    string? DeviceInfo,
    string? CorrelationId) : ICommand<AuthTokenResponse>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(512);
    }
}

public sealed class RefreshTokenCommandHandler : MediatR.IRequestHandler<RefreshTokenCommand, Result<AuthTokenResponse>>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly ITokenService _tokenService;
    private readonly IAuthAuditService _audit;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuthSecurityOptions _security;

    public RefreshTokenCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        ITokenService tokenService,
        IAuthAuditService audit,
        IUnitOfWork unitOfWork,
        IOptions<AuthSecurityOptions> security)
    {
        _users = users;
        _roles = roles;
        _tokenService = tokenService;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _security = security.Value;
    }

    public async Task<Result<AuthTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var presentedHash = _tokenService.HashToken(request.RefreshToken);
        var user = await _users.GetByRefreshTokenHashAsync(presentedHash, cancellationToken);

        if (user is null)
        {
            await _audit.WriteAsync(
                AuthAuditActions.RefreshFailed,
                false,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                details: "Unknown refresh token",
                correlationId: request.CorrelationId,
                cancellationToken: cancellationToken);

            return Result.Failure<AuthTokenResponse>(Error.Unauthorized("Invalid refresh token."));
        }

        if (user.IsLockedOut)
        {
            return Result.Failure<AuthTokenResponse>(Error.Forbidden("Account is locked."));
        }

        var existing = user.RefreshTokens.FirstOrDefault(t => t.TokenHash == presentedHash);
        if (existing is null)
        {
            return Result.Failure<AuthTokenResponse>(Error.Unauthorized("Invalid refresh token."));
        }

        // Refresh-token reuse detection: if a revoked token is presented, revoke the whole family.
        if (existing.IsRevoked)
        {
            user.RevokeAllRefreshTokens("Refresh token reuse detected");
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _audit.WriteAsync(
                AuthAuditActions.RefreshReuseDetected,
                false,
                userId: user.Id,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                correlationId: request.CorrelationId,
                cancellationToken: cancellationToken);

            return Result.Failure<AuthTokenResponse>(Error.Unauthorized("Refresh token reuse detected."));
        }

        if (existing.IsExpired || user.Status != UserStatus.Active)
        {
            existing.Revoke("Expired or inactive");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<AuthTokenResponse>(Error.Unauthorized("Refresh token expired."));
        }

        var membership = user.Memberships.FirstOrDefault(m => m.IsDefault) ?? user.Memberships.FirstOrDefault();
        if (membership is null)
        {
            return Result.Failure<AuthTokenResponse>(Error.Forbidden("No tenant membership."));
        }

        var role = await _roles.GetByIdAsync(membership.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure<AuthTokenResponse>(Error.Forbidden("Role not found."));
        }

        var newRefreshRaw = _tokenService.CreateRefreshToken();
        var newRefreshHash = _tokenService.HashToken(newRefreshRaw);
        var rotated = user.IssueRefreshToken(
            newRefreshHash,
            request.DeviceInfo,
            request.IpAddress,
            TimeSpan.FromDays(_security.RefreshTokenDays));

        existing.Revoke("Rotated", newRefreshHash);
        user.EnforceRefreshTokenLimits(_security.MaxActiveRefreshTokens);

        var access = _tokenService.CreateAccessToken(user, membership.TenantId, role.Name, role.Permissions);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(
            AuthAuditActions.RefreshSucceeded,
            true,
            membership.TenantId,
            user.Id,
            request.IpAddress,
            request.UserAgent,
            correlationId: request.CorrelationId,
            cancellationToken: cancellationToken);

        return Result.Success(new AuthTokenResponse(
            access.Token,
            newRefreshRaw,
            access.ExpiresAtUtc,
            rotated.ExpiresAtUtc,
            user.Id,
            membership.TenantId,
            user.Email,
            user.DisplayName,
            [role.Name],
            role.Permissions.ToList()));
    }
}
