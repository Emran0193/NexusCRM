using FluentValidation;
using NexusCRM.Application.Abstractions.Messaging;
using NexusCRM.Application.Abstractions.Plugins;
using NexusCRM.Application.Abstractions.Tenancy;
using NexusCRM.Contracts.Plugins;
using NexusCRM.Shared.Results;

namespace NexusCRM.Application.Plugins;

public sealed record ListPluginsQuery : IQuery<IReadOnlyList<PluginDescriptorDto>>;

public sealed class ListPluginsQueryHandler : MediatR.IRequestHandler<ListPluginsQuery, Result<IReadOnlyList<PluginDescriptorDto>>>
{
    private readonly IPluginCatalog _catalog;
    private readonly ITenantContext _tenantContext;

    public ListPluginsQueryHandler(IPluginCatalog catalog, ITenantContext tenantContext)
    {
        _catalog = catalog;
        _tenantContext = tenantContext;
    }

    public async Task<Result<IReadOnlyList<PluginDescriptorDto>>> Handle(
        ListPluginsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsResolved || _tenantContext.TenantId is null)
        {
            return Result.Failure<IReadOnlyList<PluginDescriptorDto>>(Error.Forbidden("Tenant context is required."));
        }

        var items = await _catalog.ListForTenantAsync(_tenantContext.TenantId.Value, cancellationToken);
        return Result.Success(items);
    }
}

public sealed record TogglePluginCommand(string PluginId, bool IsEnabled) : ICommand;

public sealed class TogglePluginCommandValidator : AbstractValidator<TogglePluginCommand>
{
    public TogglePluginCommandValidator()
    {
        RuleFor(x => x.PluginId).NotEmpty().MaximumLength(200);
    }
}

public sealed class TogglePluginCommandHandler : MediatR.IRequestHandler<TogglePluginCommand, Result>
{
    private readonly IPluginCatalog _catalog;
    private readonly ITenantContext _tenantContext;

    public TogglePluginCommandHandler(IPluginCatalog catalog, ITenantContext tenantContext)
    {
        _catalog = catalog;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(TogglePluginCommand request, CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsResolved || _tenantContext.TenantId is null)
        {
            return Result.Failure(Error.Forbidden("Tenant context is required."));
        }

        var installed = _catalog.GetInstalled().Any(p =>
            string.Equals(p.Id, request.PluginId, StringComparison.OrdinalIgnoreCase));
        if (!installed)
        {
            return Result.Failure(Error.NotFound("Plugin", request.PluginId));
        }

        await _catalog.SetEnabledAsync(
            _tenantContext.TenantId.Value,
            request.PluginId,
            request.IsEnabled,
            cancellationToken);

        return Result.Success();
    }
}
