using Tripory.Domain.Enums;

namespace Tripory.Domain.ValueObjects;

public record CallLogData(
    CallStatus Status,
    int DurationSeconds,
    CallDirection Direction
);