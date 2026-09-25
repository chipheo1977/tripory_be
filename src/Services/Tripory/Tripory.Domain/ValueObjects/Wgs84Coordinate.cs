using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.Domains.Abstractions.DDD;

namespace Tripory.Domain.ValueObjects;

public sealed class Wgs84Coordinate : ValueObject
{
    public const double MinLongitude = -180.0;
    public const double MaxLongitude = 180.0;
    public const double MinLatitude = -90.0;
    public const double MaxLatitude = 90.0;
    public double Longitude { get; }
    public double Latitude { get; }

    private Wgs84Coordinate(double longitude, double latitude)
    {
        Longitude = longitude;
        Latitude = latitude;
    }

    public static Result<Wgs84Coordinate> Create(double longitude, double latitude)
    {
        if (longitude < MinLongitude || longitude > MaxLongitude)
            return Result.Failure<Wgs84Coordinate>(new Error("Coordinate.InvalidLongitude", $"Kinh độ phải nằm trong khoảng từ {MinLongitude} đến {MaxLongitude}."));
    
        if (latitude < MinLatitude || latitude > MaxLatitude)
            return Result.Failure<Wgs84Coordinate>(new Error("Coordinate.InvalidLatitude", $"Vĩ độ phải nằm trong khoảng từ {MinLatitude} đến {MaxLatitude}."));
    
        return Result.Success(new Wgs84Coordinate(longitude, latitude));
    }

    public (double Lng, double Lat) ToTuple() => (Longitude, Latitude);

    public override string ToString() => $"({Longitude}, {Latitude})";
}