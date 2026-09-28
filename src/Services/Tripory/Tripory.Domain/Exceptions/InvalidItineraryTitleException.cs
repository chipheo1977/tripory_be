namespace Tripory.Domain.Exceptions;

public sealed class InvalidItineraryTitleException : ItineraryDomainException
{
    public InvalidItineraryTitleException(string title) 
        : base("Invalid Itinerary Title", $"Tiêu đề hành trình '{title}' không hợp lệ.")
    {
    }
}