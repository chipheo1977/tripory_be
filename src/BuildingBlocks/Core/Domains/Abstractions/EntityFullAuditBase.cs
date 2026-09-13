namespace BuildingBlocks.Core.Domains.Abstractions;

public abstract class EntityFullAuditBase<TKey> : EntityAuditBase<TKey>
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public void MarkAsDeleted(Guid? deleteBy = null)
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deleteBy;
    }

    public void Undo() 
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}