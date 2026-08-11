using NexusCRM.Application.Abstractions.Identity;
using NexusCRM.Application.Abstractions.Messaging;
using NexusCRM.Application.Abstractions.Tenancy;
using NexusCRM.Contracts.Auth;
using NexusCRM.Shared.Results;

namespace NexusCRM.Application.Auth;

public sealed record GetMeQuery : IQuery<MeResponse>;

public sealed class GetMeQueryHandler : MediatR.IRequestHandler<GetMeQuery, Result<MeResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;

    public GetMeQueryHandler(ICurrentUser currentUser, IUserRepository users, IRoleRepository roles)
    {
        _currentUser = currentUser;
        _users = users;
        _roles = roles;
    }

    public async Task<Result<MeResponse>> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null || _currentUser.TenantId is null)
        {
            return Result.Failure<MeResponse>(Error.Unauthorized());
        }

        var user = await _users.GetByIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure<MeResponse>(Error.Unauthorized());
        }

        var membership = user.GetMembership(_currentUser.TenantId.Value);
        var role = membership is null ? null : await _roles.GetByIdAsync(membership.RoleId, cancellationToken);

        return Result.Success(new MeResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            _currentUser.TenantId.Value,
            role is null ? [] : [role.Name],
            role?.Permissions.ToList() ?? [],
            user.MfaEnabled));
    }
}
