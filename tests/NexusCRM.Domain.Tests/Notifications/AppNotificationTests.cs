using FluentAssertions;
using NexusCRM.Domain.Notifications;

namespace NexusCRM.Domain.Tests.Notifications;

public sealed class AppNotificationTests
{
    [Fact]
    public void Create_And_MarkRead_Works()
    {
        var tenantId = Guid.NewGuid();
        var notification = AppNotification.Create(
            tenantId,
            "Workflow",
            "Lead qualified — sales follow-up required",
            NotificationCategories.Workflow,
            entityType: "Lead",
            entityId: Guid.NewGuid(),
            href: "/leads");

        notification.IsRead.Should().BeFalse();
        notification.Category.Should().Be("workflow");
        notification.TenantId.Should().Be(tenantId);

        notification.MarkRead();
        notification.IsRead.Should().BeTrue();
        notification.ReadAtUtc.Should().NotBeNull();

        notification.MarkRead();
        notification.IsRead.Should().BeTrue();
    }
}
