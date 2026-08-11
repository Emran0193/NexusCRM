using FluentValidation;
using NexusCRM.Application.Abstractions.Messaging;
using NexusCRM.Application.Abstractions.Persistence;
using NexusCRM.Contracts.Customers;
using NexusCRM.Shared.Results;

namespace NexusCRM.Application.Customers.Queries;

public sealed record GetCustomerByIdQuery(Guid Id) : IQuery<CustomerDetailDto>;

public sealed class GetCustomerByIdQueryValidator : AbstractValidator<GetCustomerByIdQuery>
{
    public GetCustomerByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetCustomerByIdQueryHandler : MediatR.IRequestHandler<GetCustomerByIdQuery, Result<CustomerDetailDto>>
{
    private readonly ICustomerRepository _customers;

    public GetCustomerByIdQueryHandler(ICustomerRepository customers)
    {
        _customers = customers;
    }

    public async Task<Result<CustomerDetailDto>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.Id, cancellationToken);
        if (customer is null)
        {
            return Result.Failure<CustomerDetailDto>(Error.NotFound("Customer", request.Id));
        }

        return Result.Success(customer.ToDetailDto());
    }
}
