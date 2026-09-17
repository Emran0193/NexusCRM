using FluentAssertions;
using NetArchTest.Rules;

namespace NexusCRM.Architecture.Tests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_Should_Not_Depend_On_Application_Or_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Domain.Common.Entity).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "NexusCRM.Application",
                "NexusCRM.Infrastructure",
                "NexusCRM.Api",
                "NexusCRM.Worker")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(typeof(Application.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "NexusCRM.Infrastructure",
                "NexusCRM.Api",
                "NexusCRM.Worker")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }
}
