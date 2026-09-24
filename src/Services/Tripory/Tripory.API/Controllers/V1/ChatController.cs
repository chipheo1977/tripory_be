using BuildingBlocks.Core.Abstractions.Shared;
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
    private static readonly HashSet<string> AllowedAudioMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "audio/webm",
        "audio/wav",
        "audio/wave",
        "audio/x-wav",
        "audio/ogg",
        "audio/mp3",
        "audio/mpeg"
    };
    private const long MaxAudioFileSizeBytes = 10 * 1024 * 1024; // 10MB

    private static Result ValidateAudioFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return Result.Failure(new Error("File.Empty", "File âm thanh không được để trống."));
        if (file.Length > MaxAudioFileSizeBytes)
            return Result.Failure(new Error("File.TooLarge", "Dung lượng file ghi âm không được vượt quá 10MB."));
        if (!AllowedAudioMimeTypes.Contains(file.ContentType))
            return Result.Failure(new Error("File.InvalidFormat", "Định dạng file âm thanh không được hỗ trợ. Chỉ chấp nhận webm, wav, ogg, mp3."));
        return Result.Success();
    }

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

    [HttpGet("conversations/{id:guid}/messages")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ChatMessageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(
        [FromRoute] Guid id,
        CancellationToken ct,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30
        
    )
    {
        _logger.LogInformation("Lấy danh sách tin nhắn từ cuộc hội thoại với ID: {ConversationId}, page: {Page}, pageSize: {PageSize}", id, page, pageSize);
        
        var query = new GetMessagesQuery(id, page, pageSize);
        var result = await Sender.Send(query, ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse<IReadOnlyList<ChatMessageDto>>.Success(result.Value));
    }

    [HttpPost("conversations/{partnerId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ConversationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateConversation(
        [FromRoute] Guid partnerId,
        CancellationToken ct
    )
    {
        _logger.LogInformation("Tạo cuộc hội thoại mới với người dùng có ID: {PartnerId}", partnerId);

        var command = new GetOrCreateConversationCommand(partnerId);
        var result = await Sender.Send(command, ct);
        if (result.IsFailure)
            return HandlerFailure(result);
        
        return Ok(ApiResponse<ConversationDto>.Success(result.Value));
    }

    [HttpPost("conversations/{id:guid}/messages/text")]
    [ProducesResponseType(typeof(ApiResponse<ChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendTextMessage(
        [FromRoute] Guid id,
        [FromBody] SendTextMessageRequest request,
        CancellationToken ct
    )
    {
        _logger.LogInformation("Gửi tin nhắn văn bản từ cuộc hội thoại với ID: {ConversationId}", id);

        var command = new SendTextMessageCommand(id, request.Content);
        var result = await Sender.Send(command, ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse<ChatMessageDto>.Success(result.Value, "Gửi tin nhắn thành công."));
    }

    [HttpPost("conversations/{id:guid}/messages/voice")]
    [ProducesResponseType(typeof(ApiResponse<ChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendVoiceMessage(
        [FromRoute] Guid id,
        [FromBody] SendVoiceMessageRequest request,
        CancellationToken ct
    )
    {
        _logger.LogInformation("Gửi tin nhắn thoại từ cuộc hội thoại với ID: {ConversationId}, Thời lượng: {VoiceDuration}", id, request.VoiceDuration);

        var command = new SendVoiceMessageCommand(id, request.VoiceUrl, request.VoiceDuration);
        var result = await Sender.Send(command, ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse<ChatMessageDto>.Success(result.Value, "Gửi tin nhắn thoại thành công."));
    }

    [HttpPost("voice")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<UploadAudioResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]

    public async Task<IActionResult> UploadVoiceMessage(
        IFormFile file,
        CancellationToken ct
    )
    {
        var validationResult = ValidateAudioFile(file);
        if (validationResult.IsFailure)
            return BadRequest(ApiResponse.Failure(validationResult.Error.Code, validationResult.Error.Message));

        await using var stream = file.OpenReadStream();
        var voiceUrl = await _audioStorageService.SaveAudioAsync(stream, file.FileName, file.ContentType, ct);

        return Ok(ApiResponse<UploadAudioResponse>.Success(new UploadAudioResponse(voiceUrl), "Tải lên file âm thanh thành công."));
    }

    [HttpPost("conversations/{id:guid}/call-log")]
    [ProducesResponseType(typeof(ApiResponse<ChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CallLog(
        [FromRoute] Guid id,
        [FromBody] LogCallSessionRequest request,
        CancellationToken ct
    )
    {
        _logger.LogInformation("Ghi nhật cuộc gọi (Status, Duration) với ID: {ConversationId}", id);
        
        var command = new LogCallSessionCommand(id, request.Status, request.DurationSeconds, request.Direction);
        var result = await Sender.Send(command, ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse<ChatMessageDto>.Success(result.Value, "Ghi nhận nhật ký cuộc gọi thành công."));
    }

    [HttpPut("conversations/{id:guid}/read")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(
    [FromRoute] Guid id,
    CancellationToken ct)
    {
        _logger.LogInformation("Đánh dấu đã xem toàn bộ tin nhắn trong hội thoại với ID: {ConversationId}", id);
        var command = new MarkConversationAsReadCommand(id);
        var result = await Sender.Send(command, ct);
        if (result.IsFailure)
            return HandlerFailure(result);
        return Ok(ApiResponse.Success("Đã đánh dấu đã đọc cuộc hội thoại."));
    }

}