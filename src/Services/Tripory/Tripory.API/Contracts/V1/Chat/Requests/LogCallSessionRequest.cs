using Tripory.Domain.Enums;
namespace tripori.api.Contracts.V1.Chat.Requests;

public record LogCallSessionRequest(
    CallStatus Status,
    int DurationSeconds,
    CallDirection Direction
);