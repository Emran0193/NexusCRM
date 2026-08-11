using NexusCRM.Contracts.Customers;
using NexusCRM.Domain.Customers;

namespace NexusCRM.Application.Customers;

internal static class CustomerMappings
{
    public static CustomerDto ToDto(this Customer customer) =>
        new(
            customer.Id,
            customer.TenantId,
            customer.Type.ToString(),
            customer.DisplayName,
            customer.Email,
            customer.Phone,
            customer.Status.ToString(),
            customer.Tags.ToList(),
            customer.CreatedAtUtc);

    public static CustomerDetailDto ToDetailDto(this Customer customer) =>
        new(
            customer.Id,
            customer.TenantId,
            customer.Type.ToString(),
            customer.DisplayName,
            customer.Email,
            customer.Phone,
            customer.Status.ToString(),
            customer.Tags.ToList(),
            customer.Contacts.Select(c => new CustomerContactDto(c.Id, c.Name, c.Email, c.Phone, c.IsPrimary)).ToList(),
            customer.Notes
                .OrderByDescending(n => n.CreatedAtUtc)
                .Select(n => new CustomerNoteDto(n.Id, n.Body, n.AuthorUserId, n.CreatedAtUtc))
                .ToList(),
            customer.Timeline
                .OrderByDescending(t => t.OccurredAtUtc)
                .Select(t => new CustomerTimelineEntryDto(t.Id, t.EventType, t.Summary, t.ActorUserId, t.OccurredAtUtc))
                .ToList(),
            customer.CreatedAtUtc);
}
