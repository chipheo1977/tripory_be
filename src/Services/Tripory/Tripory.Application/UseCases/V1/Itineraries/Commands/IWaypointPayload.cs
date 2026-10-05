namespace Tripory.Application.UseCases.V1.Itineraries.Commands;

public interface IWaypointPayload
{
    string Name { get; }
    double Longitude { get; }
    double Latitude { get; }
}
