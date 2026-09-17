using FluentAssertions;
using NexusCRM.Domain.Customers;

namespace NexusCRM.Domain.Tests.Customers;

public sealed class CustomerNotesTests
{
    [Fact]
    public void AddNoteAndContact_AppendTimeline()
    {
        var customer = Customer.CreateIndividual(Guid.NewGuid(), "Priya");
        customer.AddContact("Priya Ops", "priya@example.com", null, true);
        customer.AddNote("Called about renewal");
        customer.AddTag("vip");

        customer.Contacts.Should().ContainSingle();
        customer.Notes.Should().ContainSingle();
        customer.Tags.Should().Contain("vip");
        customer.Timeline.Count.Should().BeGreaterThanOrEqualTo(4);
    }
}
