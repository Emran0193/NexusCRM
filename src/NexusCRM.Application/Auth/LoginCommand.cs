using FluentValidation;
using Microsoft.Extensions.Options;
using NexusCRM.Application.Abstractions.Identity;
using NexusCRM.Application.Abstractions.Messaging;
using NexusCRM.Application.Abstractions.Persistence;
using NexusCRM.Contracts.Auth;
using NexusCRM.Domain.Identity;
using NexusCRM.Shared.Results;

namespace NexusCRM.Application.Auth;

public sealed record LoginCommand(
    string Email,
    string Password,
    Guid? TenantId,
    string? MfaCode,
    string? IpAddress,
    string? UserAgent,
    string? DeviceInfo,
    string? CorrelationId) : ICommand<AuthTokenResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        // Login accepts existing passwords; PasswordPolicy applies when setting/changing passwords.
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.MfaCode).MaximumLength(12).When(x => !string.IsNullOrWhiteSpace(x.MfaCode));
    }
}

public sealed class LoginCommandHandler : MediatR.IRequestHandler<LoginCommand, Result<AuthTokenResponse>>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ITotpService _totpService;
    private readonly IAuthAuditService _audit;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuthSecurityOptions _security;

    public LoginCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ITotpService totpService,
        IAuthAuditService audit,
        IUnitOfWork unitOfWork,
        IOptions<AuthSecurityOptions> security)
    {
        _users = users;
        _roles = roles;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _totpService = totpService;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _security = security.Value;
    }

    public async Task<Result<AuthTokenResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            _passwordHasher.PerformDummyVerification(request.Password);

            await _audit.WriteAsync(
                AuthAuditActions.LoginFailed,
                false,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                details: "Unknown email",
                correlationId: request.CorrelationId,
                cancellationToken: cancellationToken);

            return Result.Failure<AuthTokenResponse>(Error.Unauthorized("Invalid email or password."));
        }

        if (user.IsLockedOut)
        {
            await _audit.WriteAsync(
                AuthAuditActions.LoginLockedOut,
                false,
                userId: user.Id,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                correlationId: request.CorrelationId,
                cancellationToken: cancellationToken);

            return Result.Failure<AuthTokenResponse>(
                Error.Forbidden($"Account is locked until {user.LockoutEndUtc:u}."));
        }

        if (user.Status != UserStatus.Active || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(_security.MaxFailedLoginAttempts, _security.LockoutMinutes);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _audit.WriteAsync(
                AuthAuditActions.LoginFailed,
                false,
                userId: user.Id,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                details: "Invalid credentials",
                correlationId: request.CorrelationId,
                cancellationToken: cancellationToken);

            return Result.Failure<AuthTokenResponse>(Error.Unauthorized("Invalid email or password."));
        }

        if (user.MfaEnabled)
        {
            if (string.IsNullOrWhiteSpace(request.MfaCode) ||
                string.IsNullOrWhiteSpace(user.MfaSecret) ||
                !_totpService.VerifyCode(user.MfaSecret, request.MfaCode))
            {
                await _audit.WriteAsync(
                    AuthAuditActions.MfaChallenge,
                    false,
                    userId: user.Id,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    correlationId: request.CorrelationId,
                    cancellationToken: cancellationToken);

                return Result.Failure<AuthTokenResponse>(
                    new Error("MfaRequired", "A valid MFA code is required."));
            }
        }

        var membership = ResolveMembership(user, request.TenantId);
        if (membership is null)
        {
            return Result.Failure<AuthTokenResponse>(
                Error.Forbidden("User is not a member of the requested tenant."));
        }

        var role = await _roles.GetByIdAsync(membership.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure<AuthTokenResponse>(Error.Forbidden("User role could not be resolved."));
        }

        var access = _tokenService.CreateAccessToken(user, membership.TenantId, role.Name, role.Permissions);
        var refreshRaw = _tokenService.CreateRefreshToken();
        var refreshHash = _tokenService.HashToken(refreshRaw);
        user.RecordSuccessfulLogin();
        var refresh = user.IssueRefreshToken(
            refreshHash,
            request.DeviceInfo,
            request.IpAddress,
            TimeSpan.FromDays(_security.RefreshTokenDays));
        // Persist first so new owned tokens stay Added; then cap older sessions.
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        user.EnforceRefreshTokenLimits(_security.MaxActiveRefreshTokens);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(
            AuthAuditActions.LoginSucceeded,
            true,
            membership.TenantId,
            user.Id,
            request.IpAddress,
            request.UserAgent,
            correlationId: request.CorrelationId,
            cancellationToken: cancellationToken);

        return Result.Success(new AuthTokenResponse(
            access.Token,
            refreshRaw,
            access.ExpiresAtUtc,
            refresh.ExpiresAtUtc,
            user.Id,
            membership.TenantId,
            user.Email,
            user.DisplayName,
            [role.Name],
            role.Permissions.ToList(),
            RequiresMfa: false));
    }

    private static UserTenantMembership? ResolveMembership(Domain.Identity.User user, Guid? tenantId)
    {
        if (tenantId.HasValue)
        {
            return user.GetMembership(tenantId.Value);
        }

        return user.Memberships.FirstOrDefault(m => m.IsDefault) ?? user.Memberships.FirstOrDefault();
    }
}
