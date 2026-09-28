namespace Tripory.Domain.Exceptions;

public sealed class InvalidCoordinateException : ItineraryDomainException
{
    public InvalidCoordinateException(double longitude, double latitude) 
        : base("Invalid Coordinate", $"Tọa độ [{longitude}, {latitude}] không nằm trong hệ quy chiếu WGS84 hợp lệ.")
    {
    }
}