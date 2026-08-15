using Microsoft.EntityFrameworkCore;
using NexusCRM.Application.Abstractions.Persistence;
using NexusCRM.Domain.Customers;
using NexusCRM.Domain.Deals;
using NexusCRM.Domain.Leads;
using NexusCRM.Domain.Pipelines;

namespace NexusCRM.Infrastructure.Persistence;

internal sealed class CustomerRepository : ICustomerRepository
{
    private readonly NexusDbContext _db;

    public CustomerRepository(NexusDbContext db) => _db = db;

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default) =>
        await _db.Customers.AddAsync(customer, cancellationToken);

    public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> SearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(c =>
                c.DisplayName.ToLower().Contains(term) ||
                (c.Email != null && c.Email.ToLower().Contains(term)) ||
                (c.Phone != null && c.Phone.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}

internal sealed class PipelineRepository : IPipelineRepository
{
    private readonly NexusDbContext _db;

    public PipelineRepository(NexusDbContext db) => _db = db;

    public Task<Pipeline?> GetDefaultAsync(PipelineType type, CancellationToken cancellationToken = default) =>
        _db.Pipelines.FirstOrDefaultAsync(p => p.Type == type && p.IsDefault, cancellationToken);

    public Task<Pipeline?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Pipelines.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task AddAsync(Pipeline pipeline, CancellationToken cancellationToken = default) =>
        await _db.Pipelines.AddAsync(pipeline, cancellationToken);
}

internal sealed class LeadRepository : ILeadRepository
{
    private readonly NexusDbContext _db;

    public LeadRepository(NexusDbContext db) => _db = db;

    public Task<Lead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Leads.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task AddAsync(Lead lead, CancellationToken cancellationToken = default) =>
        await _db.Leads.AddAsync(lead, cancellationToken);

    public async Task<IReadOnlyList<Lead>> ListByPipelineAsync(
        Guid pipelineId,
        CancellationToken cancellationToken = default) =>
        await _db.Leads.AsNoTracking()
            .Where(l => l.PipelineId == pipelineId)
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Lead> Items, int TotalCount)> SearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Leads.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(l =>
                l.Title.ToLower().Contains(term) ||
                (l.CompanyName != null && l.CompanyName.ToLower().Contains(term)) ||
                (l.Email != null && l.Email.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}

internal sealed class DealRepository : IDealRepository
{
    private readonly NexusDbContext _db;

    public DealRepository(NexusDbContext db) => _db = db;

    public Task<Deal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Deals.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task AddAsync(Deal deal, CancellationToken cancellationToken = default) =>
        await _db.Deals.AddAsync(deal, cancellationToken);

    public async Task<IReadOnlyList<Deal>> ListByPipelineAsync(
        Guid pipelineId,
        CancellationToken cancellationToken = default) =>
        await _db.Deals.AsNoTracking()
            .Where(d => d.PipelineId == pipelineId)
            .OrderByDescending(d => d.CreatedAtUtc)
            .ToListAsync(cancellationToken);
}
