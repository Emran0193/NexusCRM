using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NexusCRM.Api.Tests.Infrastructure;
using NexusCRM.Contracts.Auth;
using NexusCRM.Contracts.Customers;
using NexusCRM.Contracts.Search;
using NexusCRM.Infrastructure.Persistence;

namespace NexusCRM.Api.Tests;

[Collection(NexusApiCollection.Name)]
public sealed class AuthAndCrmFlowTests : ApiTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AuthAndCrmFlowTests(NexusApiFactory factory) : base(factory)
    {
    }

    [SkippableFact]
    public async Task Login_search_and_customer_flow_works()
    {
        EnsureAvailable();
        var login = await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(
            DevelopmentDataSeeder.DemoEmail,
            DevelopmentDataSeeder.DemoPassword,
            DevelopmentDataSeeder.DemoTenantId,
            null));

        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await login.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions);
        tokens.Should().NotBeNull();
        tokens!.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.Permissions.Should().Contain("reports.read");

        using var authed = CreateAuthedClient(tokens.AccessToken);

        var search = await authed.GetAsync("/api/v1/search?q=Acme");
        search.StatusCode.Should().Be(HttpStatusCode.OK);
        var searchBody = await search.Content.ReadFromJsonAsync<GlobalSearchResponse>(JsonOptions);
        searchBody.Should().NotBeNull();
        searchBody!.Items.Should().NotBeEmpty();
        searchBody.Items.Should().Contain(i => i.Title.Contains("Acme", StringComparison.OrdinalIgnoreCase));

        var reports = await authed.GetAsync("/api/v1/reports/summary");
        reports.StatusCode.Should().Be(HttpStatusCode.OK);

        var create = await authed.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest(
            "Individual",
            $"Load Tester {Guid.NewGuid():N}"[..28],
            "loadtest@nexuscrm.local",
            null));

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = await create.Content.ReadFromJsonAsync<CustomerDto>(JsonOptions);
        customer.Should().NotBeNull();
        customer!.Id.Should().NotBeEmpty();

        var get = await authed.GetAsync($"/api/v1/customers/{customer.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task Login_with_invalid_password_fails()
    {
        EnsureAvailable();
        var login = await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(
            DevelopmentDataSeeder.DemoEmail,
            "WrongPassword!999",
            DevelopmentDataSeeder.DemoTenantId,
            null));

        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task Refresh_rotates_token()
    {
        EnsureAvailable();
        var login = await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(
            DevelopmentDataSeeder.DemoEmail,
            DevelopmentDataSeeder.DemoPassword,
            DevelopmentDataSeeder.DemoTenantId,
            null));

        var tokens = await login.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions);
        tokens.Should().NotBeNull();

        var refresh = await Client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(tokens!.RefreshToken));

        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = await refresh.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions);
        rotated.Should().NotBeNull();
        rotated!.RefreshToken.Should().NotBe(tokens.RefreshToken);
        rotated.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    private HttpClient CreateAuthedClient(string accessToken)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
