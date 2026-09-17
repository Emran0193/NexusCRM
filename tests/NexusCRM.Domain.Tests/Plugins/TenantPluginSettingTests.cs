using FluentAssertions;
using NexusCRM.Domain.Plugins;

namespace NexusCRM.Domain.Tests.Plugins;

public sealed class TenantPluginSettingTests
{
    [Fact]
    public void Create_normalizes_id_and_toggles()
    {
        var tenantId = Guid.NewGuid();
        var setting = TenantPluginSetting.Create(tenantId, " Samples.Demo ", isEnabled: true);

        setting.PluginId.Should().Be("samples.demo");
        setting.IsEnabled.Should().BeTrue();

        setting.SetEnabled(false);
        setting.IsEnabled.Should().BeFalse();
    }
}
