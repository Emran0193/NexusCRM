using FluentValidation;
using NexusCRM.Application.Abstractions.Analytics;
using NexusCRM.Application.Abstractions.Messaging;
using NexusCRM.Application.Abstractions.Tenancy;
using NexusCRM.Contracts.Search;
using NexusCRM.Domain.Identity;
using NexusCRM.Shared.Results;

namespace NexusCRM.Application.Search;

public sealed record GlobalSearchQuery(string? Q, int TakePerType = 8) : IQuery<GlobalSearchResponse>;

public sealed class GlobalSearchQueryValidator : AbstractValidator<GlobalSearchQuery>
{
    public GlobalSearchQueryValidator()
    {
        RuleFor(x => x.TakePerType).InclusiveBetween(1, 25);
    }
}

public sealed class GlobalSearchQueryHandler : MediatR.IRequestHandler<GlobalSearchQuery, Result<GlobalSearchResponse>>
{
    private readonly ICrmAnalyticsReader _analytics;
    private readonly ICurrentUser _currentUser;

    public GlobalSearchQueryHandler(ICrmAnalyticsReader analytics, ICurrentUser currentUser)
    {
        _analytics = analytics;
        _currentUser = currentUser;
    }

    public async Task<Result<GlobalSearchResponse>> Handle(
        GlobalSearchQuery request,
        CancellationToken cancellationToken)
    {
        var query = request.Q?.Trim() ?? string.Empty;
        if (query.Length < 2)
        {
            return Result.Success(new GlobalSearchResponse(query, []));
        }

        var items = await _analytics.SearchAsync(
            query,
            includeCustomers: _currentUser.HasPermission(SystemPermissions.CustomersRead),
            includeLeads: _currentUser.HasPermission(SystemPermissions.LeadsRead),
            includeDeals: _currentUser.HasPermission(SystemPermissions.DealsRead),
            takePerType: request.TakePerType,
            cancellationToken);

        return Result.Success(new GlobalSearchResponse(query, items));
    }
}
