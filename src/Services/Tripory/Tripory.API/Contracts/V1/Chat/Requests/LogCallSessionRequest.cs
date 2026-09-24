using Tripory.Domain.Enums;
namespace Tripory.API.Contracts.V1.Chat.Requests;

public record LogCallSessionRequest(
    CallStatus Status,
    int DurationSeconds,
    CallDirection Direction
);