namespace Tripory.Application.UseCases.V1.Chat.Responses;

public record ParticipantDto(
    Guid Id,
    string FullName,
    string Handle,
    string AvatarUrl
);