using NexusCRM.Domain.Common;

namespace NexusCRM.Domain.Identity;

public sealed class Permission : Entity
{
    private Permission(Guid id, string code, string name, string module)
        : base(id)
    {
        Code = code;
        Name = name;
        Module = module;
    }

    private Permission()
    {
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Module { get; private set; } = string.Empty;

    public static Permission Create(string code, string name, string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(module);

        return new Permission(Guid.NewGuid(), code.Trim().ToLowerInvariant(), name.Trim(), module.Trim());
    }
}

public static class SystemPermissions
{
    public const string CustomersRead = "customers.read";
    public const string CustomersWrite = "customers.write";
    public const string CustomersDelete = "customers.delete";
    public const string LeadsRead = "leads.read";
    public const string LeadsWrite = "leads.write";
    public const string DealsRead = "deals.read";
    public const string DealsWrite = "deals.write";
    public const string UsersManage = "users.manage";
    public const string RolesManage = "roles.manage";
    public const string TenantsManage = "tenants.manage";
    public const string AuditRead = "audit.read";
    public const string ApiKeysManage = "apikeys.manage";
    public const string WorkflowsRead = "workflows.read";
    public const string WorkflowsManage = "workflows.manage";
    public const string ReportsRead = "reports.read";
    public const string PluginsRead = "plugins.read";
    public const string PluginsManage = "plugins.manage";

    public static IReadOnlyList<(string Code, string Name, string Module)> All { get; } =
    [
        (CustomersRead, "Read customers", "customers"),
        (CustomersWrite, "Write customers", "customers"),
        (CustomersDelete, "Delete customers", "customers"),
        (LeadsRead, "Read leads", "leads"),
        (LeadsWrite, "Write leads", "leads"),
        (DealsRead, "Read deals", "deals"),
        (DealsWrite, "Write deals", "deals"),
        (UsersManage, "Manage users", "identity"),
        (RolesManage, "Manage roles", "identity"),
        (TenantsManage, "Manage tenants", "tenancy"),
        (AuditRead, "Read audit trail", "audit"),
        (ApiKeysManage, "Manage API keys", "identity"),
        (WorkflowsRead, "Read workflows", "workflows"),
        (WorkflowsManage, "Manage workflows", "workflows"),
        (ReportsRead, "Read reports", "reports"),
        (PluginsRead, "Read plugins", "plugins"),
        (PluginsManage, "Manage plugins", "plugins")
    ];
}
