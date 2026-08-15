using Microsoft.EntityFrameworkCore;
using NexusCRM.Application.Abstractions.Analytics;
using NexusCRM.Contracts.Reports;
using NexusCRM.Contracts.Search;
using NexusCRM.Domain.Deals;
using NexusCRM.Domain.Leads;
using NexusCRM.Domain.Pipelines;
using NexusCRM.Infrastructure.Persistence;

namespace NexusCRM.Infrastructure.Analytics;

internal sealed class CrmAnalyticsReader : ICrmAnalyticsReader
{
    private readonly NexusDbContext _db;

    public CrmAnalyticsReader(NexusDbContext db) => _db = db;

    public async Task<CrmSummaryReportDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var customerCount = await _db.Customers.AsNoTracking().CountAsync(cancellationToken);

        var leads = await _db.Leads.AsNoTracking()
            .Select(l => new { l.Status, l.StageId })
            .ToListAsync(cancellationToken);

        var deals = await _db.Deals.AsNoTracking()
            .Select(d => new { d.Status, d.Amount, d.Currency })
            .ToListAsync(cancellationToken);

        var leadPipeline = await _db.Pipelines.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Type == PipelineType.Lead && p.IsDefault, cancellationToken);

        var qualifiedStageIds = leadPipeline?.Stages
            .Where(s => s.Name.Contains("Qualified", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Id)
            .ToHashSet() ?? [];

        var openLeads = leads.Count(l => l.Status == LeadStatus.Open);
        var qualifiedLeads = leads.Count(l => qualifiedStageIds.Contains(l.StageId));

        var openDeals = deals.Where(d => d.Status == DealStatus.Open).ToList();
        var wonDeals = deals.Where(d => d.Status == DealStatus.Won).ToList();
        var lostDeals = deals.Where(d => d.Status == DealStatus.Lost).ToList();

        var currency = openDeals.FirstOrDefault()?.Currency
            ?? wonDeals.FirstOrDefault()?.Currency
            ?? "INR";

        return new CrmSummaryReportDto(
            customerCount,
            openLeads,
            qualifiedLeads,
            openDeals.Count,
            wonDeals.Count,
            lostDeals.Count,
            openDeals.Sum(d => d.Amount),
            wonDeals.Sum(d => d.Amount),
            currency,
            DateTimeOffset.UtcNow);
    }

    public async Task<IReadOnlyList<PipelineFunnelReportDto>> GetPipelineFunnelsAsync(
        CancellationToken cancellationToken = default)
    {
        var pipelines = await _db.Pipelines.AsNoTracking()
            .Where(p => p.IsDefault)
            .OrderBy(p => p.Type)
            .ToListAsync(cancellationToken);

        var leadCounts = await _db.Leads.AsNoTracking()
            .GroupBy(l => l.StageId)
            .Select(g => new { StageId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StageId, x => x.Count, cancellationToken);

        var dealStats = await _db.Deals.AsNoTracking()
            .GroupBy(d => d.StageId)
            .Select(g => new { StageId = g.Key, Count = g.Count(), Amount = g.Sum(d => d.Amount) })
            .ToDictionaryAsync(x => x.StageId, x => (x.Count, x.Amount), cancellationToken);

        var result = new List<PipelineFunnelReportDto>();
        foreach (var pipeline in pipelines)
        {
            var stages = pipeline.Stages
                .OrderBy(s => s.SortOrder)
                .Select(stage =>
                {
                    if (pipeline.Type == PipelineType.Lead)
                    {
                        leadCounts.TryGetValue(stage.Id, out var count);
                        return new FunnelStageDto(
                            stage.Id,
                            stage.Name,
                            stage.SortOrder,
                            stage.IsWon,
                            stage.IsLost,
                            count,
                            0);
                    }

                    dealStats.TryGetValue(stage.Id, out var stats);
                    return new FunnelStageDto(
                        stage.Id,
                        stage.Name,
                        stage.SortOrder,
                        stage.IsWon,
                        stage.IsLost,
                        stats.Count,
                        stats.Amount);
                })
                .ToList();

            result.Add(new PipelineFunnelReportDto(
                pipeline.Id,
                pipeline.Name,
                pipeline.Type.ToString(),
                stages));
        }

        return result;
    }

    public async Task<IReadOnlyList<SearchHitDto>> SearchAsync(
        string query,
        bool includeCustomers,
        bool includeLeads,
        bool includeDeals,
        int takePerType = 8,
        CancellationToken cancellationToken = default)
    {
        var term = query.Trim().ToLowerInvariant();
        var hits = new List<SearchHitDto>();

        if (includeCustomers)
        {
            var customers = await _db.Customers.AsNoTracking()
                .Where(c =>
                    c.DisplayName.ToLower().Contains(term) ||
                    (c.Email != null && c.Email.ToLower().Contains(term)) ||
                    (c.Phone != null && c.Phone.Contains(term)))
                .OrderByDescending(c => c.CreatedAtUtc)
                .Take(takePerType)
                .Select(c => new { c.Id, c.DisplayName, c.Email, c.Type })
                .ToListAsync(cancellationToken);

            hits.AddRange(customers.Select(c => new SearchHitDto(
                "Customer",
                c.Id,
                c.DisplayName,
                c.Email ?? c.Type.ToString(),
                $"/customers/{c.Id}")));
        }

        if (includeLeads)
        {
            var leads = await _db.Leads.AsNoTracking()
                .Where(l =>
                    l.Title.ToLower().Contains(term) ||
                    (l.CompanyName != null && l.CompanyName.ToLower().Contains(term)) ||
                    (l.Email != null && l.Email.ToLower().Contains(term)))
                .OrderByDescending(l => l.CreatedAtUtc)
                .Take(takePerType)
                .Select(l => new { l.Id, l.Title, l.CompanyName, l.Status })
                .ToListAsync(cancellationToken);

            hits.AddRange(leads.Select(l => new SearchHitDto(
                "Lead",
                l.Id,
                l.Title,
                l.CompanyName ?? l.Status.ToString(),
                "/leads")));
        }

        if (includeDeals)
        {
            var deals = await _db.Deals.AsNoTracking()
                .Where(d => d.Title.ToLower().Contains(term))
                .OrderByDescending(d => d.CreatedAtUtc)
                .Take(takePerType)
                .Select(d => new { d.Id, d.Title, d.Amount, d.Currency, d.Status })
                .ToListAsync(cancellationToken);

            hits.AddRange(deals.Select(d => new SearchHitDto(
                "Deal",
                d.Id,
                d.Title,
                $"{d.Amount:0.##} {d.Currency} · {d.Status}",
                "/deals")));
        }

        return hits
            .OrderBy(h => h.EntityType)
            .ThenBy(h => h.Title)
            .ToList();
    }
}
