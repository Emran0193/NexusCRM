namespace NexusCRM.Api.Tests.Infrastructure;

public abstract class ApiTestBase
{
    protected readonly NexusApiFactory Factory;
    private HttpClient? _client;

    protected ApiTestBase(NexusApiFactory factory) => Factory = factory;

    protected HttpClient Client
    {
        get
        {
            EnsureAvailable();
            return _client ??= Factory.CreateClient();
        }
    }

    protected void EnsureAvailable() => Skip.If(!Factory.IsAvailable, Factory.SkipReason);
}
