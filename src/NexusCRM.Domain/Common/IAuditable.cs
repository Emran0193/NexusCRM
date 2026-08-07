namespace NexusCRM.Domain.Common;

public interface IAuditable
{
    DateTimeOffset CreatedAtUtc { get; }

    Guid? CreatedBy { get; }

    DateTimeOffset? ModifiedAtUtc { get; }

    Guid? ModifiedBy { get; }
}

public interface ISoftDeletable
{
    bool IsDeleted { get; }

    DateTimeOffset? DeletedAtUtc { get; }

    Guid? DeletedBy { get; }
}
