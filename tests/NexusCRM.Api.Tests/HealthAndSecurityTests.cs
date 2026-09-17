using System.Net;
using FluentAssertions;
using NexusCRM.Api.Tests.Infrastructure;

namespace NexusCRM.Api.Tests;

[Collection(NexusApiCollection.Name)]
public sealed class HealthAndSecurityTests : ApiTestBase
{
    public HealthAndSecurityTests(NexusApiFactory factory) : base(factory)
    {
    }

    [SkippableFact]
    public async Task Health_live_is_anonymous_and_healthy()
    {
        EnsureAvailable();
        var response = await Client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Healthy");
    }

    [SkippableFact]
    public async Task Health_ready_is_healthy_with_database()
    {
        EnsureAvailable();
        var response = await Client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("database");
        body.Should().Contain("Healthy");
    }

    [SkippableFact]
    public async Task Responses_include_security_headers()
    {
        EnsureAvailable();
        var response = await Client.GetAsync("/health/live");

        response.Headers.Contains("X-Content-Type-Options").Should().BeTrue();
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        response.Headers.Contains("X-Frame-Options").Should().BeTrue();
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
        response.Headers.Contains("Referrer-Policy").Should().BeTrue();
    }

    [SkippableFact]
    public async Task Protected_endpoint_requires_authentication()
    {
        EnsureAvailable();
        var response = await Client.GetAsync("/api/v1/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
