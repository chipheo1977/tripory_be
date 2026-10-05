using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Chat.Commands;
using Tripory.Application.UseCases.V1.Chat.Responses;
using Tripory.Domain.Entities;
using Tripory.Domain.Errors;

namespace Tripory.Application.UseCases.V1.Chat.Handlers;

public class GetOrCreateConversationCommandHandler : ICommandHandler<GetOrCreateConversationCommand, ConversationDto>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetOrCreateConversationCommandHandler(
        IConversationRepository conversationRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService
    )
    {
        _conversationRepository = conversationRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }
    
    public async Task<Result<ConversationDto>> Handle(GetOrCreateConversationCommand request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure<ConversationDto>(DomainErrors.Auth.Unauthorized);
    
        var currentUserId = _currentUserService.UserId.Value;

        if (currentUserId == request.PartnerId)
            return Result.Failure<ConversationDto>(DomainErrors.Chat.SelfChat);
    
        var partner = await _userRepository.GetByIdAsync(request.PartnerId, ct);
        if (partner is null)
            return Result.Failure<ConversationDto>(DomainErrors.Auth.UserNotFound);

        // Tìm hội thoại hiện có
        var conversation = await _conversationRepository.GetByUsersAsync(currentUserId, request.PartnerId, ct);

        if (conversation is null)
        {
            var createResult = Conversation.Create(currentUserId, request.PartnerId);
            if (createResult.IsFailure)
                return Result.Failure<ConversationDto>(createResult.Error);

            conversation = createResult.Value;
            await _conversationRepository.AddAsync(conversation, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        var pertnerDto = new ParticipantDto(
            partner.Id,
            partner.FullName,
            partner.Handle.Value,
            partner.AvatarUrl
        );

        return Result.Success(new ConversationDto(
            conversation.Id,
            pertnerDto,
            conversation.LastMessageContent,
            conversation.LastMessageType,
            conversation.LastMessageAt,
            conversation.GetUnreadCount(currentUserId),
            conversation.UpdatedAt
        ));

    }
}