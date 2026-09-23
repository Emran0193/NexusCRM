var builder = DistributedApplication.CreateBuilder(args);

// Infra — host port 5433 avoids clashing with a local Windows PostgreSQL on 5432.
var postgres = builder.AddPostgres("postgres")
    .WithHostPort(5433)
    .WithDataVolume("nexuscrm-aspire-pg");

var nexuscrmDb = postgres.AddDatabase("nexuscrm");

var redis = builder.AddRedis("redis")
    .WithDataVolume("nexuscrm-aspire-redis");

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithDataVolume("nexuscrm-aspire-rabbit");

var api = builder.AddProject<Projects.NexusCRM_Api>("api")
    .WithReference(nexuscrmDb, connectionName: "Default")
    .WithReference(redis, connectionName: "Redis")
    .WithReference(rabbitmq)
    .WaitFor(nexuscrmDb)
    .WaitFor(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    // Aspire dashboard owns OTLP; keep Nexus custom OTEL off to avoid double exporters.
    .WithEnvironment("OpenTelemetry__Enabled", "false")
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.NexusCRM_Worker>("worker")
    .WithReference(nexuscrmDb, connectionName: "Default")
    .WithReference(redis, connectionName: "Redis")
    .WithReference(rabbitmq)
    .WaitFor(nexuscrmDb)
    .WaitFor(api)
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("OpenTelemetry__Enabled", "false");

// Angular remains `npm start` in src/NexusCRM.Web (see docs/deployment.md).
// Wire api HTTPS URL from the Aspire dashboard into environment.ts / proxy if needed.

builder.Build().Run();
