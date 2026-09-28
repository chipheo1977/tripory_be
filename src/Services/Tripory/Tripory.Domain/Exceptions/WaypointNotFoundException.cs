namespace Tripory.Domain.Exceptions;

public sealed class WaypointNotFoundException : ItineraryDomainException
{
    public WaypointNotFoundException(Guid waypointId)
        : base("Waypoint Not Found", $"Không tìm thấy điểm dừng chân có định danh '{waypointId}'.")
    {
    }
}
