using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using NexusCRM.Application.Abstractions.Analytics;
using NexusCRM.Application.Abstractions.Messaging;
using NexusCRM.Application.Abstractions.Tenancy;
using NexusCRM.Contracts.Reports;
using NexusCRM.Shared.Results;

namespace NexusCRM.Application.Reports;

public sealed record GetCrmSummaryReportQuery : IQuery<CrmSummaryReportDto>;

public sealed class GetCrmSummaryReportQueryHandler
    : MediatR.IRequestHandler<GetCrmSummaryReportQuery, Result<CrmSummaryReportDto>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ICrmAnalyticsReader _analytics;
    private readonly IDistributedCache _cache;
    private readonly ITenantContext _tenantContext;

    public GetCrmSummaryReportQueryHandler(
        ICrmAnalyticsReader analytics,
        IDistributedCache cache,
        ITenantContext tenantContext)
    {
        _analytics = analytics;
        _cache = cache;
        _tenantContext = tenantContext;
    }

    public async Task<Result<CrmSummaryReportDto>> Handle(
        GetCrmSummaryReportQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"reports:summary:{_tenantContext.TenantId}";
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
        {
            var fromCache = JsonSerializer.Deserialize<CrmSummaryReportDto>(cached, JsonOptions);
            if (fromCache is not null)
            {
                return Result.Success(fromCache);
            }
        }

        var summary = await _analytics.GetSummaryAsync(cancellationToken);
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(summary, JsonOptions),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
            },
            cancellationToken);

        return Result.Success(summary);
    }
}

public sealed record GetPipelineFunnelsQuery : IQuery<PipelineFunnelsResponse>;

public sealed class GetPipelineFunnelsQueryHandler
    : MediatR.IRequestHandler<GetPipelineFunnelsQuery, Result<PipelineFunnelsResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ICrmAnalyticsReader _analytics;
    private readonly IDistributedCache _cache;
    private readonly ITenantContext _tenantContext;

    public GetPipelineFunnelsQueryHandler(
        ICrmAnalyticsReader analytics,
        IDistributedCache cache,
        ITenantContext tenantContext)
    {
        _analytics = analytics;
        _cache = cache;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PipelineFunnelsResponse>> Handle(
        GetPipelineFunnelsQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"reports:pipelines:{_tenantContext.TenantId}";
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
        {
            var fromCache = JsonSerializer.Deserialize<PipelineFunnelsResponse>(cached, JsonOptions);
            if (fromCache is not null)
            {
                return Result.Success(fromCache);
            }
        }

        var funnels = await _analytics.GetPipelineFunnelsAsync(cancellationToken);
        var response = new PipelineFunnelsResponse(funnels);
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(response, JsonOptions),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
            },
            cancellationToken);

        return Result.Success(response);
    }
}
