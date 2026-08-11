using NexusCRM.Domain.Customers;
using NexusCRM.Domain.Deals;
using NexusCRM.Domain.Leads;
using NexusCRM.Domain.Pipelines;

namespace NexusCRM.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Customer> Items, int TotalCount)> SearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public interface IPipelineRepository
{
    Task<Pipeline?> GetDefaultAsync(PipelineType type, CancellationToken cancellationToken = default);

    Task<Pipeline?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Pipeline pipeline, CancellationToken cancellationToken = default);
}

public interface ILeadRepository
{
    Task<Lead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Lead lead, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Lead>> ListByPipelineAsync(Guid pipelineId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Lead> Items, int TotalCount)> SearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public interface IDealRepository
{
    Task<Deal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Deal deal, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Deal>> ListByPipelineAsync(Guid pipelineId, CancellationToken cancellationToken = default);
}
