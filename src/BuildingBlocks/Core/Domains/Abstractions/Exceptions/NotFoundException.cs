namespace BuildingBlocks.Core.Domains.Abstractions.Exceptions;

public abstract class NotFoundException : DomainException
{
    protected NotFoundException(string message)
        : base("Not Found", message)
    {
    }
}