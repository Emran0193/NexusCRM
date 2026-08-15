using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusCRM.Application.Abstractions.Identity;
using NexusCRM.Domain.Customers;
using NexusCRM.Domain.Deals;
using NexusCRM.Domain.Identity;
using NexusCRM.Domain.Leads;
using NexusCRM.Domain.Pipelines;
using NexusCRM.Domain.Tenancy;
using NexusCRM.Domain.Workflows;

namespace NexusCRM.Infrastructure.Persistence;

public static class DevelopmentDataSeeder
{
    public static readonly Guid DemoTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid DemoUserId = Guid.Parse("00000000-0000-0000-0000-0000000000A1");
    public static readonly Guid LeadPipelineId = Guid.Parse("00000000-0000-0000-0000-0000000000B1");
    public static readonly Guid DealPipelineId = Guid.Parse("00000000-0000-0000-0000-0000000000B2");
    public static readonly Guid QualifiedLeadWorkflowId = Guid.Parse("00000000-0000-0000-0000-0000000000E1");
    public static readonly Guid WonDealWorkflowId = Guid.Parse("00000000-0000-0000-0000-0000000000E2");

    public const string DemoEmail = "admin@nexuscrm.local";
    public const string DemoPassword = "ChangeMe!12345";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NexusDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevelopmentDataSeeder");

        foreach (var (code, name, module) in SystemPermissions.All)
        {
            if (!await db.Permissions.AnyAsync(p => p.Code == code, cancellationToken))
            {
                await db.Permissions.AddAsync(Permission.Create(code, name, module), cancellationToken);
            }
        }

        if (!await db.Tenants.AnyAsync(t => t.Id == DemoTenantId, cancellationToken))
        {
            await db.Tenants.AddAsync(Tenant.Create("Nexus Demo", "nexus-demo", "standard", DemoTenantId), cancellationToken);
        }

        var adminRole = await db.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.TenantId == DemoTenantId && r.Name == "TenantAdmin", cancellationToken);

        if (adminRole is null)
        {
            adminRole = Role.Create(DemoTenantId, "TenantAdmin", "Full tenant administrator", isSystem: true);
            await db.Roles.AddAsync(adminRole, cancellationToken);
        }

        foreach (var (code, _, _) in SystemPermissions.All)
        {
            adminRole.Grant(code);
        }

        if (!await db.Users.AnyAsync(u => u.Email == DemoEmail, cancellationToken))
        {
            var user = User.Create(DemoEmail, "Nexus Admin", passwordHasher.Hash(DemoPassword), DemoUserId);
            user.ConfirmEmail();
            user.JoinTenant(DemoTenantId, adminRole.Id, isDefault: true);
            await db.Users.AddAsync(user, cancellationToken);
        }

        await SeedPipelinesAsync(db, cancellationToken);
        await SeedWorkflowsAsync(db, cancellationToken);
        await SeedDemoCrmAsync(db, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Development seed ready. Login: {Email} / {Password} (tenant {TenantId})",
            DemoEmail,
            DemoPassword,
            DemoTenantId);
    }

    private static async Task SeedPipelinesAsync(NexusDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.Pipelines.IgnoreQueryFilters().AnyAsync(p => p.Id == LeadPipelineId, cancellationToken))
        {
            var leadPipeline = Pipeline.Create(DemoTenantId, "Default Lead Pipeline", PipelineType.Lead, LeadPipelineId);
            leadPipeline.AddStage("New", 10, id: Guid.Parse("00000000-0000-0000-0000-0000000000C1"));
            leadPipeline.AddStage("Contacted", 20, id: Guid.Parse("00000000-0000-0000-0000-0000000000C2"));
            leadPipeline.AddStage("Qualified", 30, id: Guid.Parse("00000000-0000-0000-0000-0000000000C3"));
            leadPipeline.AddStage("Proposal", 40, id: Guid.Parse("00000000-0000-0000-0000-0000000000C4"));
            leadPipeline.AddStage("Negotiation", 50, id: Guid.Parse("00000000-0000-0000-0000-0000000000C5"));
            leadPipeline.AddStage("Won", 60, isWon: true, id: Guid.Parse("00000000-0000-0000-0000-0000000000C6"));
            leadPipeline.AddStage("Lost", 70, isLost: true, id: Guid.Parse("00000000-0000-0000-0000-0000000000C7"));
            await db.Pipelines.AddAsync(leadPipeline, cancellationToken);
        }

        if (!await db.Pipelines.IgnoreQueryFilters().AnyAsync(p => p.Id == DealPipelineId, cancellationToken))
        {
            var dealPipeline = Pipeline.Create(DemoTenantId, "Default Sales Pipeline", PipelineType.Deal, DealPipelineId);
            dealPipeline.AddStage("New", 10, winProbability: 10, id: Guid.Parse("00000000-0000-0000-0000-0000000000D1"));
            dealPipeline.AddStage("Qualified", 20, winProbability: 30, id: Guid.Parse("00000000-0000-0000-0000-0000000000D2"));
            dealPipeline.AddStage("Negotiation", 30, winProbability: 60, id: Guid.Parse("00000000-0000-0000-0000-0000000000D3"));
            dealPipeline.AddStage("Won", 40, isWon: true, winProbability: 100, id: Guid.Parse("00000000-0000-0000-0000-0000000000D4"));
            dealPipeline.AddStage("Lost", 50, isLost: true, winProbability: 0, id: Guid.Parse("00000000-0000-0000-0000-0000000000D5"));
            await db.Pipelines.AddAsync(dealPipeline, cancellationToken);
        }
    }

    private static async Task SeedWorkflowsAsync(NexusDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.WorkflowDefinitions.IgnoreQueryFilters()
                .AnyAsync(w => w.Id == QualifiedLeadWorkflowId, cancellationToken))
        {
            var workflow = WorkflowDefinition.Create(
                DemoTenantId,
                "Qualified lead follow-up",
                "When a lead moves to Qualified, create a follow-up task and notify the team.",
                WorkflowTriggerType.LeadStageChanged,
                QualifiedLeadWorkflowId);

            workflow.AddCondition("ToStageName", WorkflowOperator.Equals, "Qualified");
            workflow.AddAction(WorkflowActionType.CreateFollowUpTask, value: "Call customer within 24 hours");
            workflow.AddAction(WorkflowActionType.LogNotification, value: "Lead qualified — sales follow-up required");
            await db.WorkflowDefinitions.AddAsync(workflow, cancellationToken);
        }

        if (!await db.WorkflowDefinitions.IgnoreQueryFilters()
                .AnyAsync(w => w.Id == WonDealWorkflowId, cancellationToken))
        {
            var workflow = WorkflowDefinition.Create(
                DemoTenantId,
                "Won deal celebration",
                "When a deal is marked Won, emit a notification.",
                WorkflowTriggerType.DealStageChanged,
                WonDealWorkflowId);

            workflow.AddCondition("IsWon", WorkflowOperator.Equals, "True");
            workflow.AddAction(WorkflowActionType.LogNotification, value: "Deal won — kick off onboarding");
            await db.WorkflowDefinitions.AddAsync(workflow, cancellationToken);
        }
    }

    private static async Task SeedDemoCrmAsync(NexusDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Customers.IgnoreQueryFilters().AnyAsync(c => c.TenantId == DemoTenantId, cancellationToken))
        {
            return;
        }

        var acme = Customer.CreateOrganization(
            DemoTenantId,
            "Acme Robotics",
            "hello@acmerobotics.example",
            createdBy: DemoUserId);
        var northwind = Customer.CreateOrganization(
            DemoTenantId,
            "Northwind Traders",
            "ops@northwind.example",
            createdBy: DemoUserId);
        var priya = Customer.CreateIndividual(
            DemoTenantId,
            "Priya Sharma",
            "priya.sharma@example.com",
            createdBy: DemoUserId);

        await db.Customers.AddRangeAsync([acme, northwind, priya], cancellationToken);

        var newLead = Guid.Parse("00000000-0000-0000-0000-0000000000C1");
        var contacted = Guid.Parse("00000000-0000-0000-0000-0000000000C2");
        var qualified = Guid.Parse("00000000-0000-0000-0000-0000000000C3");
        var proposal = Guid.Parse("00000000-0000-0000-0000-0000000000C4");

        var leads = new[]
        {
            Lead.Create(DemoTenantId, LeadPipelineId, newLead, "Acme plant IoT rollout", "Web", "hello@acmerobotics.example", companyName: "Acme Robotics", ownerUserId: DemoUserId, customerId: acme.Id, createdBy: DemoUserId),
            Lead.Create(DemoTenantId, LeadPipelineId, contacted, "Northwind warehouse upgrade", "Referral", "ops@northwind.example", companyName: "Northwind Traders", ownerUserId: DemoUserId, customerId: northwind.Id, createdBy: DemoUserId),
            Lead.Create(DemoTenantId, LeadPipelineId, qualified, "Priya consulting package", "Event", "priya.sharma@example.com", companyName: "Independent", ownerUserId: DemoUserId, customerId: priya.Id, createdBy: DemoUserId),
            Lead.Create(DemoTenantId, LeadPipelineId, proposal, "Acme support renewal", "Outbound", companyName: "Acme Robotics", ownerUserId: DemoUserId, customerId: acme.Id, createdBy: DemoUserId),
        };

        leads[2].SetScore(72, DemoUserId);
        await db.Leads.AddRangeAsync(leads, cancellationToken);

        var dealNew = Guid.Parse("00000000-0000-0000-0000-0000000000D1");
        var dealQualified = Guid.Parse("00000000-0000-0000-0000-0000000000D2");
        var dealNegotiation = Guid.Parse("00000000-0000-0000-0000-0000000000D3");
        var dealWon = Guid.Parse("00000000-0000-0000-0000-0000000000D4");

        var openDeal = Deal.Create(DemoTenantId, DealPipelineId, dealNew, "Acme sensors — Phase 1", 450000m, "INR", DemoUserId, acme.Id, leads[0].Id, createdBy: DemoUserId);
        var midDeal = Deal.Create(DemoTenantId, DealPipelineId, dealQualified, "Northwind WMS", 820000m, "INR", DemoUserId, northwind.Id, leads[1].Id, createdBy: DemoUserId);
        var lateDeal = Deal.Create(DemoTenantId, DealPipelineId, dealNegotiation, "Priya retainer", 180000m, "INR", DemoUserId, priya.Id, leads[2].Id, createdBy: DemoUserId);
        var wonDeal = Deal.Create(DemoTenantId, DealPipelineId, dealNew, "Acme pilot (closed)", 125000m, "INR", DemoUserId, acme.Id, createdBy: DemoUserId);
        wonDeal.MoveToStage(dealWon, isWon: true, isLost: false, DemoUserId);

        await db.Deals.AddRangeAsync([openDeal, midDeal, lateDeal, wonDeal], cancellationToken);
    }
}
