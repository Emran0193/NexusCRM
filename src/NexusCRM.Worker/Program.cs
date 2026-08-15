using NexusCRM.Application;
using NexusCRM.Infrastructure.DependencyInjection;
using NexusCRM.Infrastructure.Observability;
using NexusCRM.Worker;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.AddServiceDefaults();

    builder.Services.AddSerilog((services, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "NexusCRM.Worker")
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .WriteTo.Console());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddNexusObservability(builder.Configuration, "NexusCRM.Worker", includeAspNetCore: false);
    builder.Services.AddHostedService<Worker>();

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "NexusCRM.Worker terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
