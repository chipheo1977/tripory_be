using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.Domains.Abstractions;

namespace Tripory.Domain.Entities;

public class ItineraryDay : EntityBase<Guid>
{
    public Guid ItineraryId { get; private set; }
    public int DayNumber { get; private set; }
    public string? Subtitle { get; private set; }
    public double DayDistanceKm { get; private set; }

    private ItineraryDay() { }
    internal ItineraryDay(Guid id, Guid itineraryId, int dayNumber, string? subtitle = null)
    {
        Id = id;
        ItineraryId = itineraryId;
        DayNumber = dayNumber;
        Subtitle = subtitle;
    }

    internal Result SetSubtitle(string? subtitle)
    {    
        Subtitle = subtitle?.Trim();
        return Result.Success();
    }

    internal void SetDistance(double distanceKm)
    {
        DayDistanceKm = Math.Max(0, distanceKm);
    }
}