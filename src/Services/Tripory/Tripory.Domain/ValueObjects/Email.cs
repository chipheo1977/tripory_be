using System.Text.RegularExpressions;
using BuildingBlocks.Core.Domains.Abstractions.DDD;
using BuildingBlocks.Core.Abstractions.Shared;

namespace Tripory.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    public const int MaxLength = 256;
    private static readonly Regex EmailRegex = new(
        @"^[^@s]+@[^@s]+.[^@s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<Email>(new Error("Email.Empty", "Email không được để trống."));

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (normalizedEmail.Length > MaxLength)
            return Result.Failure<Email>(new Error("Email.TooLong", $"Email không được vượt quá {MaxLength} ký tự."));

        if (!EmailRegex.IsMatch(normalizedEmail))
            return Result.Failure<Email>(new Error("Email.InvalidFormat", "Email không đúng định dạng."));

        return Result.Success(new Email(normalizedEmail));
    }

    public override string ToString() => Value;
}