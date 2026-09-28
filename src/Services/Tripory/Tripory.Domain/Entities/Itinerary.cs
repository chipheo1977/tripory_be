
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.Domains.Abstractions;
using BuildingBlocks.Core.Domains.Abstractions.DDD;
using Tripory.Domain.ValueObjects;

namespace Tripory.Domain.Entities;

public class Itinerary : EntityAuditBase<Guid>, IAggregateRoot
{
    public const int MaxDescriptionLength = 1000;

    public Guid UserId { get; private set; }
    public ItineraryTitle Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? CoverImageUrl { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public bool IsPublic { get; private set; }
    public double TotalDistanceKm { get; private set; }

    private readonly List<ItineraryDay> _days = new();
    public IReadOnlyCollection<ItineraryDay> Days => _days.AsReadOnly();

    private readonly List<Waypoint> _waypoints = new();
    public IReadOnlyCollection<Waypoint> Waypoints => _waypoints.AsReadOnly();

    public static Result<Itinerary> CreateQuickDraft(Guid userId, ItineraryTitle title)
    {
        if (userId == Guid.Empty)
            return Result.Failure<Itinerary>(new Error("Itinerary.InvalidUserId", "UserId không được trống."));
    }

}