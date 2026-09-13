namespace BuildingBlocks.Core.Domains.Abstractions;

public abstract class EntityAuditBase<TKey> : EntityBase<TKey>
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}