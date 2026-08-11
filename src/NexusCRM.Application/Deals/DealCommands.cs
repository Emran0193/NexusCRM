using FluentValidation;
using NexusCRM.Application.Abstractions.Messaging;
using NexusCRM.Application.Abstractions.Persistence;
using NexusCRM.Application.Abstractions.Realtime;
using NexusCRM.Application.Abstractions.Tenancy;
using NexusCRM.Application.Abstractions.Workflows;
using NexusCRM.Contracts.Deals;
using NexusCRM.Domain.Deals;
using NexusCRM.Domain.Pipelines;
using NexusCRM.Domain.Workflows;
using NexusCRM.Shared.Results;

namespace NexusCRM.Application.Deals;

internal static class DealMappings
{
    public static DealDto ToDto(this Deal deal) =>
        new(
            deal.Id,
            deal.TenantId,
            deal.PipelineId,
            deal.StageId,
            deal.Title,
            deal.Amount,
            deal.Currency,
            deal.Status.ToString(),
            deal.OwnerUserId,
            deal.CustomerId,
            deal.LeadId,
            deal.ExpectedCloseDate,
            Convert.ToBase64String(deal.RowVersion),
            deal.CreatedAtUtc);
}

public sealed record CreateDealCommand(
    string Title,
    decimal Amount,
    string? Currency,
    Guid? CustomerId,
    Guid? LeadId,
    DateOnly? ExpectedCloseDate,
    Guid? StageId) : ICommand<DealDto>;

public sealed class CreateDealCommandValidator : AbstractValidator<CreateDealCommand>
{
    public CreateDealCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateDealCommandHandler : MediatR.IRequestHandler<CreateDealCommand, Result<DealDto>>
{
    private readonly IDealRepository _deals;
    private readonly IPipelineRepository _pipelines;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IBoardRealtimePublisher _realtime;

    public CreateDealCommandHandler(
        IDealRepository deals,
        IPipelineRepository pipelines,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IBoardRealtimePublisher realtime)
    {
        _deals = deals;
        _pipelines = pipelines;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _realtime = realtime;
    }

    public async Task<Result<DealDto>> Handle(CreateDealCommand request, CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsResolved || _tenantContext.TenantId is null)
        {
            return Result.Failure<DealDto>(Error.Forbidden("Tenant context is required."));
        }

        var pipeline = await _pipelines.GetDefaultAsync(PipelineType.Deal, cancellationToken);
        if (pipeline is null)
        {
            return Result.Failure<DealDto>(Error.NotFound("Pipeline", "Deal"));
        }

        var stage = request.StageId.HasValue
            ? pipeline.GetStage(request.StageId.Value)
            : pipeline.GetInitialStage();

        if (stage is null)
        {
            return Result.Failure<DealDto>(Error.Validation("Invalid pipeline stage."));
        }

        var deal = Deal.Create(
            _tenantContext.TenantId.Value,
            pipeline.Id,
            stage.Id,
            request.Title,
            request.Amount,
            request.Currency ?? "INR",
            _currentUser.UserId,
            request.CustomerId,
            request.LeadId,
            request.ExpectedCloseDate,
            _currentUser.UserId);

        await _deals.AddAsync(deal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = deal.ToDto();
        await _realtime.PublishDealChangedAsync(
            deal.TenantId,
            BoardChangeTypes.Created,
            dto,
            actorUserId: _currentUser.UserId,
            cancellationToken: cancellationToken);

        return Result.Success(dto);
    }
}

public sealed record MoveDealStageCommand(Guid DealId, Guid StageId, string? RowVersion) : ICommand<DealDto>;

public sealed class MoveDealStageCommandValidator : AbstractValidator<MoveDealStageCommand>
{
    public MoveDealStageCommandValidator()
    {
        RuleFor(x => x.DealId).NotEmpty();
        RuleFor(x => x.StageId).NotEmpty();
    }
}

public sealed class MoveDealStageCommandHandler : MediatR.IRequestHandler<MoveDealStageCommand, Result<DealDto>>
{
    private readonly IDealRepository _deals;
    private readonly IPipelineRepository _pipelines;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkflowDispatcher _workflows;
    private readonly IBoardRealtimePublisher _realtime;

    public MoveDealStageCommandHandler(
        IDealRepository deals,
        IPipelineRepository pipelines,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IWorkflowDispatcher workflows,
        IBoardRealtimePublisher realtime)
    {
        _deals = deals;
        _pipelines = pipelines;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _workflows = workflows;
        _realtime = realtime;
    }

    public async Task<Result<DealDto>> Handle(MoveDealStageCommand request, CancellationToken cancellationToken)
    {
        var deal = await _deals.GetByIdAsync(request.DealId, cancellationToken);
        if (deal is null)
        {
            return Result.Failure<DealDto>(Error.NotFound("Deal", request.DealId));
        }

        if (!string.IsNullOrWhiteSpace(request.RowVersion))
        {
            var presented = Convert.FromBase64String(request.RowVersion);
            if (!deal.RowVersion.SequenceEqual(presented))
            {
                return Result.Failure<DealDto>(Error.Conflict("Deal was modified by another user. Refresh and retry."));
            }
        }

        var pipeline = await _pipelines.GetByIdAsync(deal.PipelineId, cancellationToken);
        var stage = pipeline?.GetStage(request.StageId);
        if (stage is null)
        {
            return Result.Failure<DealDto>(Error.Validation("Stage does not belong to this deal pipeline."));
        }

        var fromStageId = deal.StageId;
        deal.MoveToStage(stage.Id, stage.IsWon, stage.IsLost, _currentUser.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = deal.ToDto();
        await _realtime.PublishDealChangedAsync(
            deal.TenantId,
            BoardChangeTypes.Moved,
            dto,
            fromStageId,
            _currentUser.UserId,
            cancellationToken);

        await _workflows.DispatchAsync(
            WorkflowTriggerType.DealStageChanged,
            "Deal",
            deal.Id,
            deal.TenantId,
            new Dictionary<string, string?>
            {
                ["FromStageId"] = fromStageId.ToString(),
                ["ToStageId"] = stage.Id.ToString(),
                ["ToStageName"] = stage.Name,
                ["Amount"] = deal.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["Status"] = deal.Status.ToString(),
                ["IsWon"] = stage.IsWon.ToString(),
                ["IsLost"] = stage.IsLost.ToString()
            },
            cancellationToken);

        return Result.Success(dto);
    }
}

public sealed record GetDealBoardQuery : IQuery<DealBoardDto>;

public sealed class GetDealBoardQueryHandler : MediatR.IRequestHandler<GetDealBoardQuery, Result<DealBoardDto>>
{
    private readonly IPipelineRepository _pipelines;
    private readonly IDealRepository _deals;

    public GetDealBoardQueryHandler(IPipelineRepository pipelines, IDealRepository deals)
    {
        _pipelines = pipelines;
        _deals = deals;
    }

    public async Task<Result<DealBoardDto>> Handle(GetDealBoardQuery request, CancellationToken cancellationToken)
    {
        var pipeline = await _pipelines.GetDefaultAsync(PipelineType.Deal, cancellationToken);
        if (pipeline is null)
        {
            return Result.Failure<DealBoardDto>(Error.NotFound("Pipeline", "Deal"));
        }

        var deals = await _deals.ListByPipelineAsync(pipeline.Id, cancellationToken);
        var columns = pipeline.Stages
            .OrderBy(s => s.SortOrder)
            .Select(stage =>
            {
                var items = deals.Where(d => d.StageId == stage.Id).Select(d => d.ToDto()).ToList();
                return new DealColumnDto(
                    stage.Id,
                    stage.Name,
                    stage.SortOrder,
                    stage.IsWon,
                    stage.IsLost,
                    items.Sum(i => i.Amount),
                    items);
            })
            .ToList();

        var openValue = deals.Where(d => d.Status == DealStatus.Open).Sum(d => d.Amount);
        return Result.Success(new DealBoardDto(pipeline.Id, pipeline.Name, openValue, columns));
    }
}
