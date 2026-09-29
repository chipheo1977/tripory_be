using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.Domains.Abstractions.DDD;

namespace Tripory.Domain.ValueObjects;

public sealed class ItineraryDescription : ValueObject
{
    public const int MaxLength = 1000;

    public string Value { get; }

    private ItineraryDescription(string value)
    {
        Value = value;
    }

    public static Result<ItineraryDescription?> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Success<ItineraryDescription?>(null);

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
            return Result.Failure<ItineraryDescription?>(new Error(
                "ItineraryDescription.TooLong",
                $"Mô tả hành trình không được vượt quá {MaxLength} ký tự."));

        return Result.Success<ItineraryDescription?>(new ItineraryDescription(trimmed));
    }

    public override string ToString() => Value;

    public static implicit operator string?(ItineraryDescription? description) => description?.Value;
}
