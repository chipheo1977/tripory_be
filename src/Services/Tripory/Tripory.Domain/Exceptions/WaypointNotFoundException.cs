using BuildingBlocks.Core.Domains.Abstractions.Exceptions;

namespace Tripory.Domain.Exceptions;

public sealed class WaypointNotFoundException : NotFoundException
{
    public WaypointNotFoundException(Guid waypointId)
        : base($"Không tìm thấy điểm dừng chân có định danh '{waypointId}'.")
    {
    }
}
