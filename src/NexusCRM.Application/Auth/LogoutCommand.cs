using FluentValidation;
using NexusCRM.Application.Abstractions.Identity;
using NexusCRM.Application.Abstractions.Messaging;
using NexusCRM.Application.Abstractions.Persistence;
using NexusCRM.Domain.Identity;
using NexusCRM.Shared.Results;

namespace NexusCRM.Application.Auth;

public sealed record LogoutCommand(
    string RefreshToken,
    string? IpAddress,
    string? UserAgent,
    string? CorrelationId) : ICommand;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class LogoutCommandHandler : MediatR.IRequestHandler<LogoutCommand, Result>
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokenService;
    private readonly IAuthAuditService _audit;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutCommandHandler(
        IUserRepository users,
        ITokenService tokenService,
        IAuthAuditService audit,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _tokenService = tokenService;
        _audit = audit;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var hash = _tokenService.HashToken(request.RefreshToken);
        var user = await _users.GetByRefreshTokenHashAsync(hash, cancellationToken);
        if (user is not null)
        {
            user.RevokeRefreshToken(hash, "Logout");
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _audit.WriteAsync(
                AuthAuditActions.Logout,
                true,
                userId: user.Id,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                correlationId: request.CorrelationId,
                cancellationToken: cancellationToken);
        }

        return Result.Success();
    }
}
