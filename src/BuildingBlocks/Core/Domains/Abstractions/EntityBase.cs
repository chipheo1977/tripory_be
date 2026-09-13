namespace BuildingBlocks.Core.Domains.Abstractions;

public abstract class EntityBase<TKey>
{
    public TKey Id { get; set; } = default!; 
}