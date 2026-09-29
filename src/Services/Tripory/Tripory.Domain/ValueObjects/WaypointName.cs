using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.Domains.Abstractions.DDD;

namespace Tripory.Domain.ValueObjects;

public sealed class WaypointName : ValueObject
{
    public const int MaxLength = 200;
    public const int MinLength = 1;

    public string Value { get; }

    private WaypointName(string value)
    {
        Value = value;
    }

    public static Result<WaypointName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<WaypointName>(new Error(
                "WaypointName.Empty",
                "Tên điểm dừng chân không được để trống."));

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
            return Result.Failure<WaypointName>(new Error(
                "WaypointName.TooLong",
                $"Tên điểm dừng chân không được vượt quá {MaxLength} ký tự."));

        return Result.Success(new WaypointName(trimmed));
    }

    public override string ToString() => Value;

    public static implicit operator string(WaypointName name) => name.Value;
}
