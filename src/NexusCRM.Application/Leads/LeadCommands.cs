using FluentValidation;
using NexusCRM.Application.Abstractions.Messaging;
using NexusCRM.Application.Abstractions.Persistence;
using NexusCRM.Application.Abstractions.Plugins;
using NexusCRM.Application.Abstractions.Realtime;
using NexusCRM.Application.Abstractions.Tenancy;
using NexusCRM.Application.Abstractions.Workflows;
using NexusCRM.Contracts.Leads;
using NexusCRM.Domain.Leads;
using NexusCRM.Domain.Pipelines;
using NexusCRM.Domain.Workflows;
using NexusCRM.Shared.Results;

namespace NexusCRM.Application.Leads;

internal static class LeadMappings
{
    public static LeadDto ToDto(this Lead lead) =>
        new(
            lead.Id,
            lead.TenantId,
            lead.PipelineId,
            lead.StageId,
            lead.Title,
            lead.Source,
            lead.Email,
            lead.Phone,
            lead.CompanyName,
            lead.Score,
            lead.Status.ToString(),
            lead.OwnerUserId,
            lead.CustomerId,
            lead.Notes,
            lead.CreatedAtUtc,
            lead.Tags.ToList());
}

public sealed record CreateLeadCommand(
    string Title,
    string? Source,
    string? Email,
    string? Phone,
    string? CompanyName,
    Guid? CustomerId,
    Guid? StageId) : ICommand<LeadDto>;

public sealed class CreateLeadCommandValidator : AbstractValidator<CreateLeadCommand>
{
    public CreateLeadCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public sealed class CreateLeadCommandHandler : MediatR.IRequestHandler<CreateLeadCommand, Result<LeadDto>>
{
    private readonly ILeadRepository _leads;
    private readonly IPipelineRepository _pipelines;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IBoardRealtimePublisher _realtime;
    private readonly IPluginRuntime _plugins;

    public CreateLeadCommandHandler(
        ILeadRepository leads,
        IPipelineRepository pipelines,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IBoardRealtimePublisher realtime,
        IPluginRuntime plugins)
    {
        _leads = leads;
        _pipelines = pipelines;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _realtime = realtime;
        _plugins = plugins;
    }

    public async Task<Result<LeadDto>> Handle(CreateLeadCommand request, CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsResolved || _tenantContext.TenantId is null)
        {
            return Result.Failure<LeadDto>(Error.Forbidden("Tenant context is required."));
        }

        var pipeline = await _pipelines.GetDefaultAsync(PipelineType.Lead, cancellationToken);
        if (pipeline is null)
        {
            return Result.Failure<LeadDto>(Error.NotFound("Pipeline", "Lead"));
        }

        var stage = request.StageId.HasValue
            ? pipeline.GetStage(request.StageId.Value)
            : pipeline.GetInitialStage();

        if (stage is null)
        {
            return Result.Failure<LeadDto>(Error.Validation("Invalid pipeline stage."));
        }

        var lead = Lead.Create(
            _tenantContext.TenantId.Value,
            pipeline.Id,
            stage.Id,
            request.Title,
            request.Source,
            request.Email,
            request.Phone,
            request.CompanyName,
            _currentUser.UserId,
            request.CustomerId,
            _currentUser.UserId);

        await _leads.AddAsync(lead, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _plugins.ApplyLeadEnrichmentAsync(lead, stage.Name, _currentUser.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = lead.ToDto();
        await _realtime.PublishLeadChangedAsync(
            lead.TenantId,
            BoardChangeTypes.Created,
            dto,
            actorUserId: _currentUser.UserId,
            cancellationToken: cancellationToken);

        return Result.Success(dto);
    }
}

public sealed record MoveLeadStageCommand(Guid LeadId, Guid StageId) : ICommand<LeadDto>;

public sealed class MoveLeadStageCommandValidator : AbstractValidator<MoveLeadStageCommand>
{
    public MoveLeadStageCommandValidator()
    {
        RuleFor(x => x.LeadId).NotEmpty();
        RuleFor(x => x.StageId).NotEmpty();
    }
}

public sealed class MoveLeadStageCommandHandler : MediatR.IRequestHandler<MoveLeadStageCommand, Result<LeadDto>>
{
    private readonly ILeadRepository _leads;
    private readonly IPipelineRepository _pipelines;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkflowDispatcher _workflows;
    private readonly IBoardRealtimePublisher _realtime;
    private readonly IPluginRuntime _plugins;

    public MoveLeadStageCommandHandler(
        ILeadRepository leads,
        IPipelineRepository pipelines,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IWorkflowDispatcher workflows,
        IBoardRealtimePublisher realtime,
        IPluginRuntime plugins)
    {
        _leads = leads;
        _pipelines = pipelines;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _workflows = workflows;
        _realtime = realtime;
        _plugins = plugins;
    }

    public async Task<Result<LeadDto>> Handle(MoveLeadStageCommand request, CancellationToken cancellationToken)
    {
        var lead = await _leads.GetByIdAsync(request.LeadId, cancellationToken);
        if (lead is null)
        {
            return Result.Failure<LeadDto>(Error.NotFound("Lead", request.LeadId));
        }

        var pipeline = await _pipelines.GetByIdAsync(lead.PipelineId, cancellationToken);
        var stage = pipeline?.GetStage(request.StageId);
        if (stage is null)
        {
            return Result.Failure<LeadDto>(Error.Validation("Stage does not belong to this lead pipeline."));
        }

        var fromStageId = lead.StageId;
        lead.MoveToStage(stage.Id, stage.IsWon, stage.IsLost, _currentUser.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _plugins.ApplyLeadEnrichmentAsync(lead, stage.Name, _currentUser.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = lead.ToDto();
        await _realtime.PublishLeadChangedAsync(
            lead.TenantId,
            BoardChangeTypes.Moved,
            dto,
            fromStageId,
            _currentUser.UserId,
            cancellationToken);

        await _workflows.DispatchAsync(
            WorkflowTriggerType.LeadStageChanged,
            "Lead",
            lead.Id,
            lead.TenantId,
            new Dictionary<string, string?>
            {
                ["FromStageId"] = fromStageId.ToString(),
                ["ToStageId"] = stage.Id.ToString(),
                ["ToStageName"] = stage.Name,
                ["Score"] = lead.Score.ToString(),
                ["Status"] = lead.Status.ToString(),
                ["IsWon"] = stage.IsWon.ToString(),
                ["IsLost"] = stage.IsLost.ToString()
            },
            cancellationToken);

        return Result.Success(dto);
    }
}

public sealed record SetLeadScoreCommand(Guid LeadId, int Score) : ICommand<LeadDto>;

public sealed class SetLeadScoreCommandValidator : AbstractValidator<SetLeadScoreCommand>
{
    public SetLeadScoreCommandValidator()
    {
        RuleFor(x => x.LeadId).NotEmpty();
        RuleFor(x => x.Score).InclusiveBetween(0, 100);
    }
}

public sealed class SetLeadScoreCommandHandler : MediatR.IRequestHandler<SetLeadScoreCommand, Result<LeadDto>>
{
    private readonly ILeadRepository _leads;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkflowDispatcher _workflows;

    public SetLeadScoreCommandHandler(
        ILeadRepository leads,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IWorkflowDispatcher workflows)
    {
        _leads = leads;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _workflows = workflows;
    }

    public async Task<Result<LeadDto>> Handle(SetLeadScoreCommand request, CancellationToken cancellationToken)
    {
        var lead = await _leads.GetByIdAsync(request.LeadId, cancellationToken);
        if (lead is null)
        {
            return Result.Failure<LeadDto>(Error.NotFound("Lead", request.LeadId));
        }

        lead.SetScore(request.Score, _currentUser.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _workflows.DispatchAsync(
            WorkflowTriggerType.LeadScoreChanged,
            "Lead",
            lead.Id,
            lead.TenantId,
            new Dictionary<string, string?>
            {
                ["Score"] = lead.Score.ToString(),
                ["Status"] = lead.Status.ToString(),
                ["StageId"] = lead.StageId.ToString()
            },
            cancellationToken);

        return Result.Success(lead.ToDto());
    }
}

public sealed record GetLeadBoardQuery : IQuery<LeadBoardDto>;

public sealed class GetLeadBoardQueryHandler : MediatR.IRequestHandler<GetLeadBoardQuery, Result<LeadBoardDto>>
{
    private readonly IPipelineRepository _pipelines;
    private readonly ILeadRepository _leads;

    public GetLeadBoardQueryHandler(IPipelineRepository pipelines, ILeadRepository leads)
    {
        _pipelines = pipelines;
        _leads = leads;
    }

    public async Task<Result<LeadBoardDto>> Handle(GetLeadBoardQuery request, CancellationToken cancellationToken)
    {
        var pipeline = await _pipelines.GetDefaultAsync(PipelineType.Lead, cancellationToken);
        if (pipeline is null)
        {
            return Result.Failure<LeadBoardDto>(Error.NotFound("Pipeline", "Lead"));
        }

        var leads = await _leads.ListByPipelineAsync(pipeline.Id, cancellationToken);
        var columns = pipeline.Stages
            .OrderBy(s => s.SortOrder)
            .Select(stage => new PipelineColumnDto(
                stage.Id,
                stage.Name,
                stage.SortOrder,
                stage.IsWon,
                stage.IsLost,
                leads.Where(l => l.StageId == stage.Id).Select(l => l.ToDto()).ToList()))
            .ToList();

        return Result.Success(new LeadBoardDto(pipeline.Id, pipeline.Name, columns));
    }
}
