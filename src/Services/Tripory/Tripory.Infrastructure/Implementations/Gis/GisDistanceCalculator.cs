using Tripory.Domain.Abstractions.External;
using Tripory.Domain.ValueObjects;

namespace Tripory.Infrastructure.Implementations.Gis;

public class GisDistanceCalculator : IGisDistanceCalculator
{
    private const double EarthRadiusKm = 6371.0;

    public double CalculateDistanceKm(Wgs84Coordinate from, Wgs84Coordinate to)
    {
        if (from == null || to == null)
        {
            return 0.0;
        }

        var dLat = DegreesToRadians(to.Latitude - from.Latitude);
        var dLon = DegreesToRadians(to.Longitude - from.Longitude);

        var lat1 = DegreesToRadians(from.Latitude);
        var lat2 = DegreesToRadians(to.Latitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return Math.Round(EarthRadiusKm * c, 2);
    }

    public double CalculateRouteDistanceKm(IReadOnlyList<Wgs84Coordinate> coordinates)
    {
        if (coordinates == null || coordinates.Count < 2)
        {
            return 0.0;
        }

        double totalDistance = 0.0;
        for (int i = 0; i < coordinates.Count - 1; i++)
        {
            totalDistance += CalculateDistanceKm(coordinates[i], coordinates[i + 1]);
        }

        return Math.Round(totalDistance, 2);
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}