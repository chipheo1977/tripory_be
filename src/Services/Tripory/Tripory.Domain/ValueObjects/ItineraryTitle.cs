using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.Domains.Abstractions.DDD;

namespace Tripory.Domain.ValueObjects;

public sealed class ItineraryTitle : ValueObject
{
    public const int MaxLength = 100;
    public const int MinLength = 1;

    public string Value { get; }

    private ItineraryTitle(string value)
    {
        Value = value;
    }

    public static Result<ItineraryTitle> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<ItineraryTitle>(new Error("ItineraryTitle.Empty", "Tiêu đề hành trình không được để trống."));

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
            return Result.Failure<ItineraryTitle>(new Error("ItineraryTitle.TooLong", $"Tiêu đề hành trình không được vượt quá {MaxLength} ký tự."));
        
        return Result.Success(new ItineraryTitle(trimmed));
    }

    public override string ToString() => Value;
};