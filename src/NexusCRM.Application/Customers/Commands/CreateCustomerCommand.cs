using FluentValidation;
using NexusCRM.Application.Abstractions.Messaging;
using NexusCRM.Application.Abstractions.Persistence;
using NexusCRM.Application.Abstractions.Tenancy;
using NexusCRM.Contracts.Customers;
using NexusCRM.Domain.Customers;
using NexusCRM.Shared.Results;

namespace NexusCRM.Application.Customers.Commands;

public sealed record CreateCustomerCommand(
    string Type,
    string DisplayName,
    string? Email,
    string? Phone) : ICommand<CustomerDto>;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).NotEmpty().Must(t =>
            t.Equals("Individual", StringComparison.OrdinalIgnoreCase) ||
            t.Equals("Organization", StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}

public sealed class CreateCustomerCommandHandler : MediatR.IRequestHandler<CreateCustomerCommand, Result<CustomerDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;

    public CreateCustomerCommandHandler(
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ICurrentUser currentUser)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<Result<CustomerDto>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsResolved || _tenantContext.TenantId is null)
        {
            return Result.Failure<CustomerDto>(Error.Forbidden("Tenant context is required."));
        }

        var tenantId = _tenantContext.TenantId.Value;
        var isOrg = request.Type.Equals("Organization", StringComparison.OrdinalIgnoreCase);

        var customer = isOrg
            ? Customer.CreateOrganization(tenantId, request.DisplayName, request.Email, request.Phone, _currentUser.UserId)
            : Customer.CreateIndividual(tenantId, request.DisplayName, request.Email, request.Phone, _currentUser.UserId);

        await _customers.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(customer.ToDto());
    }
}
