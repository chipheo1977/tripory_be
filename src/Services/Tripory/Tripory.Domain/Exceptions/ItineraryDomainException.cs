
using BuildingBlocks.Core.Domains.Abstractions.Exceptions;

namespace Tripory.Domain.Exceptions;

public class ItineraryDomainException : DomainException
{
    public ItineraryDomainException(string title, string message) : base(title, message)
    {
    }
}