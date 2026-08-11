using NexusCRM.Contracts.Reports;
using NexusCRM.Contracts.Search;

namespace NexusCRM.Application.Abstractions.Analytics;

public interface ICrmAnalyticsReader
{
    Task<CrmSummaryReportDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PipelineFunnelReportDto>> GetPipelineFunnelsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SearchHitDto>> SearchAsync(
        string query,
        bool includeCustomers,
        bool includeLeads,
        bool includeDeals,
        int takePerType = 8,
        CancellationToken cancellationToken = default);
}
