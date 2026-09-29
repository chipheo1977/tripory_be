using Tripory.Application.UseCases.V1.Itineraries.Responses;
using Tripory.Domain.Entities;

namespace Tripory.Application.UseCases.V1.Itineraries.Extensions;

public static class ItineraryExtensions
{
    public static WaypointDto ToDto(this Waypoint waypoint)
    {
        return new WaypointDto(
            waypoint.Id,
            waypoint.DayNumber,
            waypoint.OrderIndex,
            waypoint.Name,
            waypoint.Address,
            waypoint.Coordinate.Longitude,
            waypoint.Coordinate.Latitude,
            waypoint.Notes
        );
    }

    public static ItineraryDayDto ToDto(this ItineraryDay day)
    {
        return new ItineraryDayDto(
            day.Id,
            day.DayNumber,
            day.Subtitle,
            day.DayDistanceKm
        );
    }

    public static ItinerarySummaryDto ToSummaryDto(this Itinerary itinerary)
    {
        return new ItinerarySummaryDto(
            itinerary.Id,
            itinerary.UserId,
            itinerary.Title.Value,
            itinerary.Description?.Value,
            itinerary.CoverImageUrl,
            itinerary.StartDate,
            itinerary.IsPublic,
            itinerary.TotalDistanceKm,
            itinerary.Days.Count,
            itinerary.Waypoints.Count,
            itinerary.CreatedAt
        );
    }

    public static ItineraryDetailDto ToDetailDto(this Itinerary itinerary)
    {
        return new ItineraryDetailDto(
            itinerary.Id,
            itinerary.UserId,
            itinerary.Title.Value,
            itinerary.Description?.Value,
            itinerary.CoverImageUrl,
            itinerary.StartDate,
            itinerary.IsPublic,
            itinerary.TotalDistanceKm,
            itinerary.CreatedAt,
            itinerary.UpdatedAt,
            itinerary.Days.Select(d => d.ToDto()).ToList(),
            itinerary.Waypoints.Select(w => w.ToDto()).ToList()
        );
    }
}