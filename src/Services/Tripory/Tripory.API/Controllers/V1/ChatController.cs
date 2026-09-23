using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tripory.API.Common.Responses;
using Tripory.API.Contracts.V1.Chat.Requests;
using Tripory.API.Contracts.V1.Chat.Responses;
using Tripory.Application.Abstractions.Storage;
using Tripory.Application.UseCases.V1.Chat.Commands;
using Tripory.Application.UseCases.V1.Chat.Queries;
using Tripory.Application.UseCases.V1.Chat.Responses;

namespace Tripory.API.Controllers.V1;

[Authorize]
public class ChatController : ApiController
{
    private readonly IAudioStorageService _audioStorageService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        ISender sender, 
        IAudioStorageService audioStorageService, 
        ILogger<ChatController> logger
    ) : base(sender)
    {
        _audioStorageService = audioStorageService;
        _logger = logger;
    }

    [HttpGet("conversations")]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<ConversationDto>>),
        StatusCodes.Status200OK
    )]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status401Unauthorized
    )]
    public async Task<IActionResult> GetConversations(CancellationToken ct)
    {
        _logger.LogInformation("Lấy danh sách các cuộc hội thoại của người dùng hiện tại.");
        
        var query = new GetConversationsQuery();
        var result = await Sender.Send(query, ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse<IReadOnlyList<ConversationDto>>.Success(result.Value));

    }
}