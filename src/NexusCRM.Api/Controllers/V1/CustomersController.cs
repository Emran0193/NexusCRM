using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusCRM.Api.Extensions;
using NexusCRM.Application.Customers.Commands;
using NexusCRM.Application.Customers.Queries;
using NexusCRM.Contracts.Customers;
using NexusCRM.Domain.Identity;
using NexusCRM.Infrastructure.Authorization;

namespace NexusCRM.Api.Controllers.V1;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly ISender _sender;

    public CustomersController(ISender sender) => _sender = sender;

    [HttpGet]
    [RequirePermission(SystemPermissions.CustomersRead)]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new SearchCustomersQuery(search, page, pageSize), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(SystemPermissions.CustomersRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCustomerByIdQuery(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [RequirePermission(SystemPermissions.CustomersWrite)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateCustomerCommand(request.Type, request.DisplayName, request.Email, request.Phone),
            cancellationToken);

        return result.IsFailure
            ? result.ToActionResult()
            : result.ToCreatedResult($"/api/v1/customers/{result.Value.Id}");
    }

    [HttpPatch("{id:guid}")]
    [RequirePermission(SystemPermissions.CustomersWrite)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateCustomerCommand(id, request.DisplayName, request.Email, request.Phone),
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/contacts")]
    [RequirePermission(SystemPermissions.CustomersWrite)]
    public async Task<IActionResult> AddContact(
        Guid id,
        [FromBody] AddCustomerContactRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new AddCustomerContactCommand(id, request.Name, request.Email, request.Phone, request.IsPrimary),
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/notes")]
    [RequirePermission(SystemPermissions.CustomersWrite)]
    public async Task<IActionResult> AddNote(
        Guid id,
        [FromBody] AddCustomerNoteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AddCustomerNoteCommand(id, request.Body), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/tags")]
    [RequirePermission(SystemPermissions.CustomersWrite)]
    public async Task<IActionResult> AddTag(
        Guid id,
        [FromBody] AddCustomerTagRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AddCustomerTagCommand(id, request.Tag), cancellationToken);
        return result.ToActionResult();
    }
}
