using FluentValidation;
using NexusCRM.Application.Abstractions.Messaging;
using NexusCRM.Application.Abstractions.Persistence;
using NexusCRM.Application.Abstractions.Tenancy;
using NexusCRM.Contracts.Customers;
using NexusCRM.Shared.Results;

namespace NexusCRM.Application.Customers.Commands;

public sealed record UpdateCustomerCommand(Guid Id, string DisplayName, string? Email, string? Phone)
    : ICommand<CustomerDto>;

public sealed class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public sealed class UpdateCustomerCommandHandler : MediatR.IRequestHandler<UpdateCustomerCommand, Result<CustomerDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateCustomerCommandHandler(
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CustomerDto>> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.Id, cancellationToken);
        if (customer is null)
        {
            return Result.Failure<CustomerDto>(Error.NotFound("Customer", request.Id));
        }

        customer.UpdateProfile(request.DisplayName, request.Email, request.Phone, _currentUser.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(customer.ToDto());
    }
}

public sealed record AddCustomerContactCommand(
    Guid CustomerId,
    string Name,
    string? Email,
    string? Phone,
    bool IsPrimary) : ICommand<CustomerContactDto>;

public sealed class AddCustomerContactCommandValidator : AbstractValidator<AddCustomerContactCommand>
{
    public AddCustomerContactCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class AddCustomerContactCommandHandler
    : MediatR.IRequestHandler<AddCustomerContactCommand, Result<CustomerContactDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public AddCustomerContactCommandHandler(
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CustomerContactDto>> Handle(
        AddCustomerContactCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Failure<CustomerContactDto>(Error.NotFound("Customer", request.CustomerId));
        }

        var contact = customer.AddContact(
            request.Name,
            request.Email,
            request.Phone,
            request.IsPrimary,
            _currentUser.UserId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new CustomerContactDto(contact.Id, contact.Name, contact.Email, contact.Phone, contact.IsPrimary));
    }
}

public sealed record AddCustomerNoteCommand(Guid CustomerId, string Body) : ICommand<CustomerNoteDto>;

public sealed class AddCustomerNoteCommandValidator : AbstractValidator<AddCustomerNoteCommand>
{
    public AddCustomerNoteCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
    }
}

public sealed class AddCustomerNoteCommandHandler : MediatR.IRequestHandler<AddCustomerNoteCommand, Result<CustomerNoteDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public AddCustomerNoteCommandHandler(
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CustomerNoteDto>> Handle(AddCustomerNoteCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Failure<CustomerNoteDto>(Error.NotFound("Customer", request.CustomerId));
        }

        var note = customer.AddNote(request.Body, _currentUser.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new CustomerNoteDto(note.Id, note.Body, note.AuthorUserId, note.CreatedAtUtc));
    }
}

public sealed record AddCustomerTagCommand(Guid CustomerId, string Tag) : ICommand<CustomerDto>;

public sealed class AddCustomerTagCommandValidator : AbstractValidator<AddCustomerTagCommand>
{
    public AddCustomerTagCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Tag).NotEmpty().MaximumLength(50);
    }
}

public sealed class AddCustomerTagCommandHandler : MediatR.IRequestHandler<AddCustomerTagCommand, Result<CustomerDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public AddCustomerTagCommandHandler(
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CustomerDto>> Handle(AddCustomerTagCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Failure<CustomerDto>(Error.NotFound("Customer", request.CustomerId));
        }

        customer.AddTag(request.Tag, _currentUser.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(customer.ToDto());
    }
}
