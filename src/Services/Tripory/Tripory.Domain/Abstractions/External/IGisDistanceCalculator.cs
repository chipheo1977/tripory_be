using Tripory.Domain.ValueObjects;

namespace Tripory.Domain.Abstractions.External;

public interface IGisDistanceCalculator
{
    double CalculateDistanceKm(Wgs84Coordinate from, Wgs84Coordinate to);
    double CalculateRouteDistanceKm(Wgs84Coordinate from, Wgs84Coordinate to);
}