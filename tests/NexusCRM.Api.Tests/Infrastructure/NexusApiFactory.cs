using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusCRM.Infrastructure.Workflows;
using Npgsql;
using Testcontainers.PostgreSql;

namespace NexusCRM.Api.Tests.Infrastructure;

public sealed class NexusApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private string _connectionString = string.Empty;

    public string? SkipReason { get; private set; }

    public bool IsAvailable => string.IsNullOrEmpty(SkipReason);

    async Task IAsyncLifetime.InitializeAsync()
    {
        try
        {
            _connectionString = await ResolveConnectionStringAsync();
        }
        catch (Exception ex)
        {
            SkipReason =
                "API integration tests need PostgreSQL. Start `docker compose -f deploy/docker-compose.yml up -d postgres`, " +
                "set NEXUSCRM_TEST_CONNECTION, or set NEXUSCRM_USE_TESTCONTAINERS=1 with Docker running. " +
                $"Details: {ex.Message}";
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _connectionString,
                ["ConnectionStrings:Redis"] = "",
                ["OpenTelemetry:Enabled"] = "false",
                ["Jwt:Issuer"] = "NexusCRM",
                ["Jwt:Audience"] = "NexusCRM.Web",
                ["Jwt:SigningKey"] = "NexusCRM-Integration-Test-Signing-Key-32chars!",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "14",
                ["Cors:Origins:0"] = "http://localhost:4200",
                ["Security:ExposeMetricsEndpoint"] = "false"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            var hosted = services
                .Where(d => d.ServiceType == typeof(IHostedService) &&
                            d.ImplementationType == typeof(WorkflowBackgroundProcessor))
                .ToList();

            foreach (var descriptor in hosted)
            {
                services.Remove(descriptor);
            }
        });
    }

    private async Task<string> ResolveConnectionStringAsync()
    {
        var external = Environment.GetEnvironmentVariable("NEXUSCRM_TEST_CONNECTION");
        if (!string.IsNullOrWhiteSpace(external))
        {
            await using var probe = new NpgsqlConnection(external);
            await probe.OpenAsync();
            return external;
        }

        if (string.Equals(Environment.GetEnvironmentVariable("NEXUSCRM_USE_TESTCONTAINERS"), "1", StringComparison.OrdinalIgnoreCase))
        {
            _postgres = new PostgreSqlBuilder("postgres:17-alpine")
                .WithDatabase("nexuscrm_test")
                .WithUsername("nexus")
                .WithPassword("nexus")
                .Build();
            await _postgres.StartAsync();
            return _postgres.GetConnectionString();
        }

        return await EnsureLocalComposeDatabaseAsync();
    }

    private static async Task<string> EnsureLocalComposeDatabaseAsync()
    {
        const string admin = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";
        const string database = "nexuscrm_it";
        var app = $"Host=localhost;Port=5432;Database={database};Username=postgres;Password=postgres";

        await using (var conn = new NpgsqlConnection(admin))
        {
            await conn.OpenAsync();
            await using var exists = new NpgsqlCommand($"SELECT 1 FROM pg_database WHERE datname = '{database}'", conn);
            var found = await exists.ExecuteScalarAsync();
            if (found is null)
            {
                await using var create = new NpgsqlCommand($"CREATE DATABASE {database}", conn);
                await create.ExecuteNonQueryAsync();
            }
        }

        await using var probe = new NpgsqlConnection(app);
        await probe.OpenAsync();
        return app;
    }
}

[CollectionDefinition(Name)]
public sealed class NexusApiCollection : ICollectionFixture<NexusApiFactory>
{
    public const string Name = "NexusApi";
}
