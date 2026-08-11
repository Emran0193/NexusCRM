using FluentValidation;
using NexusCRM.Application.Abstractions.Messaging;
using NexusCRM.Application.Abstractions.Persistence;
using NexusCRM.Contracts.Common;
using NexusCRM.Contracts.Customers;
using NexusCRM.Shared.Results;

namespace NexusCRM.Application.Customers.Queries;

public sealed record SearchCustomersQuery(string? Search, int Page = 1, int PageSize = 20)
    : IQuery<PagedResponse<CustomerDto>>;

public sealed class SearchCustomersQueryValidator : AbstractValidator<SearchCustomersQuery>
{
    public SearchCustomersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class SearchCustomersQueryHandler
    : MediatR.IRequestHandler<SearchCustomersQuery, Result<PagedResponse<CustomerDto>>>
{
    private readonly ICustomerRepository _customers;

    public SearchCustomersQueryHandler(ICustomerRepository customers)
    {
        _customers = customers;
    }

    public async Task<Result<PagedResponse<CustomerDto>>> Handle(
        SearchCustomersQuery request,
        CancellationToken cancellationToken)
    {
        var (items, total) = await _customers.SearchAsync(
            request.Search,
            request.Page,
            request.PageSize,
            cancellationToken);

        var response = new PagedResponse<CustomerDto>(
            items.Select(c => c.ToDto()).ToList(),
            request.Page,
            request.PageSize,
            total);

        return Result.Success(response);
    }
}
