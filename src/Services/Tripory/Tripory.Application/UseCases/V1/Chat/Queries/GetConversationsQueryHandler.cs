using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Chat.Queries;
using Tripory.Application.UseCases.V1.Chat.Responses;

namespace Tripory.Application.UseCases.V1.Chat.Queries;

public class GetConversationsQueryHandler : IQueryHandler<GetConversationsQuery, IReadOnlyList<ConversationDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetConversationsQueryHandler(
        IConversationRepository conversationRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService
    )
    {
        _conversationRepository = conversationRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IReadOnlyList<ConversationDto>>> Handle(GetConversationsQuery request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure<IReadOnlyList<ConversationDto>>(new Error("Auth.Unauthorized", "Yêu cầu đăng nhập."));

        var currentUserId = _currentUserService.UserId.Value;
        var conversations = await _conversationRepository.GetUserConversationsAsync(currentUserId, ct);

        var dtos = new List<ConversationDto>();

        foreach (var conv in conversations)
        {
            var partnerId = conv.GetPartnerId(currentUserId);
            var partner = await _userRepository.GetByIdAsync(partnerId, ct);

            var partnerDto = new ParticipantDto(
                partnerId,
                partner?.FullName ?? "Người dùng ẩn danh",
                partner?.Handle.Value ?? "@user",
                partner?.AvatarUrl ?? "https://api.dicebear.com/7.x/identicon/svg?seed=tripory"
            );

            dtos.Add(new ConversationDto(
                conv.Id,
                partnerDto,
                conv.LastMessageContent,
                conv.LastMessageType,
                conv.LastMessageAt,
                conv.GetUnreadCount(currentUserId),
                conv.UpdatedAt
            ));
        }

        return Result.Success<IReadOnlyList<ConversationDto>>(dtos);
    }
}