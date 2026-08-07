namespace NexusCRM.Contracts.Customers;

public sealed record CustomerDto(
    Guid Id,
    Guid TenantId,
    string Type,
    string DisplayName,
    string? Email,
    string? Phone,
    string Status,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAtUtc);

public sealed record CustomerDetailDto(
    Guid Id,
    Guid TenantId,
    string Type,
    string DisplayName,
    string? Email,
    string? Phone,
    string Status,
    IReadOnlyList<string> Tags,
    IReadOnlyList<CustomerContactDto> Contacts,
    IReadOnlyList<CustomerNoteDto> Notes,
    IReadOnlyList<CustomerTimelineEntryDto> Timeline,
    DateTimeOffset CreatedAtUtc);

public sealed record CustomerContactDto(Guid Id, string Name, string? Email, string? Phone, bool IsPrimary);

public sealed record CustomerNoteDto(Guid Id, string Body, Guid? AuthorUserId, DateTimeOffset CreatedAtUtc);

public sealed record CustomerTimelineEntryDto(
    Guid Id,
    string EventType,
    string Summary,
    Guid? ActorUserId,
    DateTimeOffset OccurredAtUtc);

public sealed record CreateCustomerRequest(
    string Type,
    string DisplayName,
    string? Email,
    string? Phone);

public sealed record UpdateCustomerRequest(
    string DisplayName,
    string? Email,
    string? Phone);

public sealed record AddCustomerContactRequest(
    string Name,
    string? Email,
    string? Phone,
    bool IsPrimary);

public sealed record AddCustomerNoteRequest(string Body);

public sealed record AddCustomerTagRequest(string Tag);
