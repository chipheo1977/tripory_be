using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Tripory.Infrastructure.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Người dùng {UserId} đã kết nối WebSocket qua ConnectionId: {ConnectionId}", userId, Context.ConnectionId);

        // Tham gia nhóm cá nhân theo UserId để dễ dàng gửi notification đích danh
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Người dùng {UserId} đã ngắt kết nối WebSocket: {ConnectionId}", userId, Context.ConnectionId);

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    #region 1. Room / Conversation Groups

    /// <summary>
    /// Tham gia phòng chat của hội thoại cụ thể (khi người dùng mở cửa sổ chat đó)
    /// </summary>
    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conv_{conversationId}");
        _logger.LogDebug("User {UserId} tham gia phòng chat hội thoại: {ConversationId}", GetCurrentUserId(), conversationId);
    }

    /// <summary>
    /// Rời phòng chat của hội thoại (khi người dùng đóng modal chat hoặc chuyển sang cuộc trò chuyện khác)
    /// </summary>
    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conv_{conversationId}");
        _logger.LogDebug("User {UserId} rời phòng chat hội thoại: {ConversationId}", GetCurrentUserId(), conversationId);
    }

    #endregion

    #region 2. WebRTC Audio Call Signaling (PT-03 §6)

    /// <summary>
    /// Bắt đầu gọi cho một người dùng khác (chuyển tiếp SDP Offer)
    /// </summary>
    public async Task CallUser(string targetUserId, object offer)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            _logger.LogWarning("CallUser bị hủy: targetUserId là null hoặc rỗng.");
            return;
        }

        var callerId = GetCurrentUserId();
        _logger.LogInformation("Cuộc gọi đi từ {CallerId} tới {TargetUserId}", callerId, targetUserId);

        await Clients.User(targetUserId).SendAsync("IncomingCall", new
        {
            CallerId = callerId,
            Offer = offer,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Chấp nhận cuộc gọi (chuyển tiếp SDP Answer từ người nhận về người gọi)
    /// </summary>
    public async Task AcceptCall(string callerUserId, object answer)
    {
        if (string.IsNullOrWhiteSpace(callerUserId))
        {
            _logger.LogWarning("AcceptCall bị hủy: callerUserId là null hoặc rỗng.");
            return;
        }

        var responderId = GetCurrentUserId();
        _logger.LogInformation("Cuộc gọi được chấp nhận bởi {ResponderId} từ người gọi {CallerUserId}", responderId, callerUserId);

        await Clients.User(callerUserId).SendAsync("CallAccepted", new
        {
            ResponderId = responderId,
            Answer = answer,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Từ chối cuộc gọi
    /// </summary>
    public async Task RejectCall(string callerUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(callerUserId))
        {
            _logger.LogWarning("RejectCall bị hủy: callerUserId là null hoặc rỗng.");
            return;
        }

        var responderId = GetCurrentUserId();
        _logger.LogInformation("Cuộc gọi bị từ chối bởi {ResponderId}. Lý do: {Reason}", responderId, reason);

        await Clients.User(callerUserId).SendAsync("CallRejected", new
        {
            ResponderId = responderId,
            Reason = string.IsNullOrWhiteSpace(reason) ? "Người dùng bận" : reason
        });
    }

    /// <summary>
    /// Kết thúc cuộc gọi đàm thoại
    /// </summary>
    public async Task EndCall(string partnerUserId)
    {
        if (string.IsNullOrWhiteSpace(partnerUserId))
        {
            _logger.LogWarning("EndCall bị hủy: partnerUserId là null hoặc rỗng.");
            return;
        }

        var userId = GetCurrentUserId();
        _logger.LogInformation("Cuộc gọi kết thúc bởi {UserId}", userId);

        await Clients.User(partnerUserId).SendAsync("CallEnded", new
        {
            EndedBy = userId
        });
    }

    /// <summary>
    /// Chuyển tiếp ICE Candidate để thiết lập kết nối âm thanh P2P trực tiếp
    /// </summary>
    public async Task SendIceCandidate(string targetUserId, object candidate)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            _logger.LogWarning("SendIceCandidate bị hủy: targetUserId là null hoặc rỗng.");
            return;
        }

        var senderId = GetCurrentUserId();
        await Clients.User(targetUserId).SendAsync("ReceiveIceCandidate", new
        {
            SenderId = senderId,
            Candidate = candidate
        });
    }

    #endregion

    private string? GetCurrentUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.UserIdentifier;
    }
}
