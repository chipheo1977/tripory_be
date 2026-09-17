using System.Text.RegularExpressions;
using BuildingBlocks.Core.Domains.Abstractions.DDD;
using BuildingBlocks.Core.Abstractions.Shared;

namespace Tripory.Domain.ValueObjects;

public sealed class Handle : ValueObject
{
    private static readonly Regex HandleRegex = new(
        @"^@[a-z0-9_.]{3,30}$",
        RegexOptions.Compiled
    );

    public String Value { get; }

    private Handle(string value)
    {
        Value = value;
    }

    public static Result<Handle> Create(string? handle)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return Result.Failure<Handle>(new Error("Handle.Empty", "Handle không được để trống."));

        var formatted = handle.Trim().ToLowerInvariant();
        if (!formatted.StartsWith('@'))
            formatted = $"@{formatted}";

        if (!HandleRegex.IsMatch(formatted))
            return Result.Failure<Handle>(new Error("Handle.InvalidFormat", "Handle phải từ 3-30 ký tự, chỉ gồm chữ thường, số, dấu gạch dưới và dấu chấm."));

        return Result.Success(new Handle(formatted));
    }

    public static Handle GenerateDefault(string emailPrefix)
    {
        var cleaned = Regex.Replace(emailPrefix.ToLowerInvariant(), @"[^a-z0-9]", "");
        if (cleaned.Length > 15) cleaned = cleaned[..15];
        if (cleaned.Length < 3) cleaned = "traveler";

        var suffix = Random.Shared.Next(1000, 9999);
        return new Handle($"@{cleaned}_{suffix}");
    }

    public override string ToString() => Value;
}