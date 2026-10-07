namespace Tripory.API.Contracts.V1.Itineraries.Requests;

public record ReorderWaypointsRequest(IReadOnlyList<Guid> OrderedWaypointIds);
