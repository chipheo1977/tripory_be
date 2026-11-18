using Tripory.Domain.ValueObjects;

namespace Tripory.Application.Abstractions.Data;

public sealed record UserSummary(
    Guid Id,
    string FullName,
    Handle Handle,
    string? AvatarUrl
);
