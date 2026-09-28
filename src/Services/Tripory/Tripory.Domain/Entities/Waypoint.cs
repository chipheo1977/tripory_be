using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.Domains.Abstractions;
using Tripory.Domain.ValueObjects;

namespace Tripory.Domain.Entities;

public class Waypoint : EntityAuditBase<Guid>
{
    public Guid ItineraryId { get; private set; }
    public int DayNumber { get; private set; }
    public int OrderIndex { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public Wgs84Coordinate Coordinate { get; private set; } = null!;
    public string? Notes { get; private set; }

    private Waypoint() { }

    internal Waypoint(
        Guid id,
        Guid itineraryId,
        int dayNumber,
        int orderIndex,
        string name,
        string? address,
        Wgs84Coordinate coordinate,
        string? notes
    )
    {
        Id = id;
        ItineraryId = itineraryId;
        DayNumber = dayNumber;
        OrderIndex = orderIndex;
        Name = name.Trim();
        Address = address?.Trim();
        Coordinate = coordinate;
        Notes = notes?.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    internal Result UpdateInfo(
        string name,
        string? address,
        Wgs84Coordinate coordinate,
        string? notes
    )
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(new Error("Waypoint.InvalidName", "Tên waypoint không được để trống."));

        Name = name.Trim();
        Address = address?.Trim();
        Coordinate = coordinate;
        Notes = notes?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    internal void UpdateOrderIndex(int orderIndex)
    {
        OrderIndex = orderIndex;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    internal void MoveToDay(int newDayNumber, int newOrderIndex)
    {
        DayNumber = newDayNumber;
        OrderIndex = newOrderIndex;
    }
}