using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Chat.Responses;

namespace Tripory.Application.UseCases.V1.Chat.Commands;

public record SendVoiceMessageCommand(
    Guid ConversationId,
    string VoiceUrl,
    int VoiceDuration
) : ICommand<ChatMessageDto>;
