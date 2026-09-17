using FluentAssertions;
using NexusCRM.Domain.Customers;

namespace NexusCRM.Domain.Tests.Customers;

public sealed class CustomerTests
{
    [Fact]
    public void CreateIndividual_RaisesDomainEvent_AndNormalizesFields()
    {
        var tenantId = Guid.NewGuid();

        var customer = Customer.CreateIndividual(
            tenantId,
            "  Rahul Sharma  ",
            "  rahul@example.com ",
            " 9999999999 ");

        customer.TenantId.Should().Be(tenantId);
        customer.DisplayName.Should().Be("Rahul Sharma");
        customer.Email.Should().Be("rahul@example.com");
        customer.Phone.Should().Be("9999999999");
        customer.Type.Should().Be(CustomerType.Individual);
        customer.DomainEvents.Should().ContainSingle(e => e is CustomerCreatedDomainEvent);
    }

    [Fact]
    public void SoftDelete_IsIdempotent()
    {
        var customer = Customer.CreateOrganization(Guid.NewGuid(), "Acme Corp");

        customer.SoftDelete();
        customer.SoftDelete();

        customer.IsDeleted.Should().BeTrue();
        customer.Status.Should().Be(CustomerStatus.Archived);
        customer.DomainEvents.OfType<CustomerDeletedDomainEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void Create_WithoutTenant_Throws()
    {
        var act = () => Customer.CreateIndividual(Guid.Empty, "Test");
        act.Should().Throw<ArgumentException>();
    }
}
