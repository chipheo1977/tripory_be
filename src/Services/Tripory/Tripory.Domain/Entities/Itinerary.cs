
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.Domains.Abstractions;
using BuildingBlocks.Core.Domains.Abstractions.DDD;
using Tripory.Domain.Abstractions.External;
using Tripory.Domain.ValueObjects;

namespace Tripory.Domain.Entities;

// @TODO: Cần refactor tách valication & bussiness
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

    private Itinerary() { }
    private Itinerary(
        Guid id,
        Guid userId,
        ItineraryTitle title,
        bool isPublic,
        double totalDistanceKm,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        UserId = userId;
        Title = title;
        IsPublic = isPublic;
        TotalDistanceKm = totalDistanceKm;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    private readonly List<ItineraryDay> _days = new();
    public IReadOnlyCollection<ItineraryDay> Days => _days.AsReadOnly();

    private readonly List<Waypoint> _waypoints = new();
    public IReadOnlyCollection<Waypoint> Waypoints => _waypoints.AsReadOnly();

    public static Result<Itinerary> CreateQuickDraft(Guid userId, ItineraryTitle title)
    {
        if (userId == Guid.Empty)
            return Result.Failure<Itinerary>(new Error("Itinerary.InvalidUserId", "UserId không được trống."));
    
        var itinerary = new Itinerary(
            Guid.NewGuid(),
            userId,
            title,
            false,
            0.0,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        );

        itinerary.GetOrCreateDay(1);
        return Result.Success(itinerary);
    }

    public Result UpdateMetadata(
        ItineraryTitle title,
        string? description,
        DateOnly? startDate,
        string? coverImageUrl
    )
    {
        if (description != null && description.Length > MaxDescriptionLength)
            return Result.Failure(new Error(
                "Itinerary.DescriptionTooLong",
                $"Mô tả không được vượt quá {MaxDescriptionLength} ký tự."
            ));

        Title = title;
        Description = description;
        StartDate = startDate;
        CoverImageUrl = coverImageUrl;
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public Result Publish()
    {
        if (_waypoints.Count == 0)
            return Result.Failure(new Error(
                "Itinerary.CannotPublishEmpty",
                "Không thể xuất bản hành trình khi chưa có điểm dừng chân nào."
            ));

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
        string name,
        string? address,
        Wgs84Coordinate coordinate,
        string? notes)
    {
        if (dayNumber < 1)
            return Result.Failure<Waypoint>(new Error(
                "Itinerary.InvalidDayNumber",
                "Số thứ tự ngày phải lớn hơn hoặc bằng 1."
            ));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Waypoint>(new Error(
                "Itinerary.WaypointNameEmpty",
                "Tên điểm dừng chân không được để trống."
            ));

        // Tự động sinh mốc ngày nếu chưa có
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

    public Result RemoveWaypoint(Guid waypointId)
    {
        var waypoint = _waypoints.FirstOrDefault(w => w.Id == waypointId);
        if (waypoint is null)
            return Result.Failure(new Error(
                "Waypoint.NotFound",
                $"Không tìm thấy điểm dừng chân có định danh '{waypointId}'."
            ));

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
            return Result.Failure(new Error(
                "Itinerary.InvalidReorderList",
                "Danh sách điểm sắp xếp không khớp với dữ liệu hiện tại."
            ));
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

    private ItineraryDay GetOrCreateDay(int dayNumber, string? subtitle = null)
    {
        var existingDay = _days.FirstOrDefault(d => d.DayNumber == dayNumber);
        if (existingDay is not null)
            return existingDay;

        var newDay = new ItineraryDay(Guid.NewGuid(), Id, dayNumber, subtitle);
        _days.Add(newDay);
        return newDay;
    }

}
