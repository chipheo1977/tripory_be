using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.Domains.Abstractions;
using BuildingBlocks.Core.Domains.Abstractions.DDD;
using Tripory.Domain.Abstractions.External;
using Tripory.Domain.Errors;
using Tripory.Domain.ValueObjects;

namespace Tripory.Domain.Entities;

public class Itinerary : EntityFullAuditBase<Guid>, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public ItineraryTitle Title { get; private set; } = null!;
    public ItineraryDescription? Description { get; private set; }
    public string? CoverImageUrl { get; private set; }
    public string[] CoverImageUrls { get; private set; } = [];
    public DateOnly? StartDate { get; private set; }
    public bool IsPublic { get; private set; }
    public double TotalDistanceKm { get; private set; }
    public string? DestinationCode { get; private set; }
    public string[] Interests { get; private set; } = [];

    public int? SelectedCoverIndex => FindSelectedCoverIndex();

    private Itinerary() { }

    private Itinerary(
        Guid id,
        Guid userId,
        ItineraryTitle title,
        ItineraryDescription? description,
        bool isPublic,
        double totalDistanceKm,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        DateOnly? startDate,
        string? destinationCode,
        string[]? interests
    )
    {
        Id = id;
        UserId = userId;
        Title = title;
        Description = description;
        IsPublic = isPublic;
        TotalDistanceKm = totalDistanceKm;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        StartDate = startDate;
        DestinationCode = destinationCode;
        Interests = interests ?? [];
    }

    private readonly List<ItineraryDay> _days = new();
    public IReadOnlyCollection<ItineraryDay> Days => _days.AsReadOnly();

    private readonly List<Waypoint> _waypoints = new();
    public IReadOnlyCollection<Waypoint> Waypoints => _waypoints.AsReadOnly();

    public static Result<Itinerary> CreateQuickDraft(
        Guid userId,
        ItineraryTitle title,
        int? dayCount,
        string? destinationCode,
        DateOnly? startDate,
        string[]? interests,
        string[]? coverImageUrls
    )
    {
        if (userId == Guid.Empty)
            return Result.Failure<Itinerary>(DomainErrors.Itinerary.InvalidUserId);

        var itinerary = new Itinerary(
            Guid.NewGuid(),
            userId,
            title,
            null,
            false,
            0.0,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            startDate,
            destinationCode,
            interests
        );

        itinerary.CreateInitialDays(dayCount);

        var coverResult = itinerary.SetCoverImages(coverImageUrls ?? [], 0);
        if (coverResult.IsFailure)
            return Result.Failure<Itinerary>(coverResult.Error);

        return Result.Success(itinerary);
    }

    public Result SetCoverImages(string[] coverImageUrls, int selectedIndex)
    {
        if (coverImageUrls.Length == 0)
        {
            CoverImageUrls = [];
            CoverImageUrl = null;
            return Result.Success();
        }

        if (selectedIndex < 0 || selectedIndex >= coverImageUrls.Length)
            return Result.Failure(DomainErrors.Itinerary.CoverSelectionInvalid);

        CoverImageUrls = coverImageUrls;
        CoverImageUrl = coverImageUrls[selectedIndex];
        return Result.Success();
    }

    public Result UpdateMetadata(
        ItineraryTitle title,
        ItineraryDescription? description,
        DateOnly? startDate,
        string? destinationCode,
        string[]? interests,
        string[]? coverImageUrls,
        int? selectedCoverIndex
    )
    {
        if (coverImageUrls is not null)
        {
            var coverResult = SetCoverImages(coverImageUrls, selectedCoverIndex ?? -1);
            if (coverResult.IsFailure)
                return coverResult;
        }

        Title = title;
        Description = description;
        StartDate = startDate;
        DestinationCode = destinationCode ?? DestinationCode;
        Interests = interests ?? Interests;
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public Result Publish()
    {
        if (_waypoints.Count == 0)
            return Result.Failure(DomainErrors.Itinerary.CannotPublishEmpty);

        IsPublic = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public void Unpublish()
    {
        IsPublic = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Result<Waypoint> AddWaypoint(
        int dayNumber,
        WaypointName name,
        string? address,
        Wgs84Coordinate coordinate,
        string? notes)
    {
        if (dayNumber < 1)
            return Result.Failure<Waypoint>(DomainErrors.Itinerary.InvalidDayNumber);

        GetOrCreateDay(dayNumber);

        var nextOrder = _waypoints.Count(w => w.DayNumber == dayNumber);

        var waypoint = new Waypoint(
            Guid.NewGuid(),
            Id,
            dayNumber,
            nextOrder,
            name,
            address,
            coordinate,
            notes
        );

        _waypoints.Add(waypoint);
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success(waypoint);
    }

    public Result UpdateWaypoint(
        Guid waypointId,
        WaypointName name,
        string? address,
        Wgs84Coordinate coordinate,
        string? notes)
    {
        var waypoint = _waypoints.FirstOrDefault(w => w.Id == waypointId);
        if (waypoint is null)
            return Result.Failure(DomainErrors.Waypoint.NotFoundWithId(waypointId));

        waypoint.UpdateInfo(name, address, coordinate, notes);
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public Result RemoveWaypoint(Guid waypointId)
    {
        var waypoint = _waypoints.FirstOrDefault(w => w.Id == waypointId);
        if (waypoint is null)
            return Result.Failure(DomainErrors.Waypoint.NotFoundWithId(waypointId));

        var dayNumber = waypoint.DayNumber;
        _waypoints.Remove(waypoint);

        // Bảo vệ Invariant chuẩn hóa thứ tự liên tục (0, 1, 2, ..., N-1) theo BR_04
        var remainingWaypoints = _waypoints
            .Where(w => w.DayNumber == dayNumber)
            .OrderBy(w => w.OrderIndex)
            .ToList();

        for (var i = 0; i < remainingWaypoints.Count; i++)
        {
            remainingWaypoints[i].UpdateOrderIndex(i);
        }

        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public Result ReorderWaypointsInDay(int dayNumber, IReadOnlyList<Guid> orderedWaypointIds)
    {
        var dayWaypoints = _waypoints.Where(w => w.DayNumber == dayNumber).ToList();

        // Kiểm tra tính toàn vẹn: số lượng và tập hợp ID phải khớp hoàn toàn
        if (orderedWaypointIds.Count != dayWaypoints.Count ||
            orderedWaypointIds.Any(id => dayWaypoints.All(w => w.Id != id)))
        {
            return Result.Failure(DomainErrors.Itinerary.InvalidReorderList);
        }

        var waypointMap = dayWaypoints.ToDictionary(w => w.Id);

        for (var index = 0; index < orderedWaypointIds.Count; index++)
        {
            var id = orderedWaypointIds[index];
            waypointMap[id].UpdateOrderIndex(index);
        }

        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public Result SetDaySubtitle(int dayNumber, string? subtitle)
    {
        if (dayNumber < 1)
            return Result.Failure(DomainErrors.Itinerary.InvalidDayNumber);

        var day = GetOrCreateDay(dayNumber);
        day.SetSubtitle(subtitle);
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public void RecalculateDistances(IGisDistanceCalculator calculator)
    {
        double totalItineraryKm = 0.0;

        foreach (var day in _days)
        {
            var dayWaypoints = _waypoints
                .Where(w => w.DayNumber == day.DayNumber)
                .OrderBy(w => w.OrderIndex)
                .ToList();

            var coords = dayWaypoints.Select(w => w.Coordinate).ToList();
            var dayKm = calculator.CalculateRouteDistanceKm(coords);

            day.SetDistance(dayKm);

            // Quy tắc ngắt quãng qua đêm (BR_01 trong PT-05): Cộng dồn cự ly từng ngày,
            // không tính khoảng cách nối giữa điểm cuối ngày N và điểm đầu ngày N+1.
            totalItineraryKm += dayKm;
        }

        TotalDistanceKm = totalItineraryKm;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private const int DefaultInitialDayCount = 1;

    private void CreateInitialDays(int? dayCount)
    {
        var totalDays = dayCount ?? DefaultInitialDayCount;
        for (var dayNumber = 1; dayNumber <= totalDays; dayNumber++)
            GetOrCreateDay(dayNumber);
    }

    private ItineraryDay GetOrCreateDay(int dayNumber, string? subtitle = null)
    {
        var existingDay = _days.FirstOrDefault(d => d.DayNumber == dayNumber);
        if (existingDay is not null)
            return existingDay;

        var newDay = new ItineraryDay(Guid.NewGuid(), Id, dayNumber, subtitle);
        _days.Add(newDay);
        return newDay;
    }

    private int? FindSelectedCoverIndex()
    {
        if (CoverImageUrl is null)
            return null;

        var index = Array.IndexOf(CoverImageUrls, CoverImageUrl);
        return index >= 0 ? index : null;
    }
}
