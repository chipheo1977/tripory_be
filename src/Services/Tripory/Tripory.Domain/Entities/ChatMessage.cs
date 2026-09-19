using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.Domains.Abstractions;
using Tripory.Domain.Enums;
using Tripory.Domain.ValueObjects;

namespace Tripory.Domain.Entities;

public class ChatMessage : EntityBase<Guid>
{
    public const int MaxTextLength = 2000;
    public const int MinVoiceDurationSeconds = 1;
    public const int MaxVoiceDurationSeconds = 120;

    public Guid ConversationId { get; private set; }
    public Guid SenderId { get; private set; }
    public MessageType Type { get; private set; }
    public string? Content { get; private set; }
    public string? VoiceUrl { get; private set; }
    public int? VoiceDuration { get; private set; }
    public CallLogData? CallLogData { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ChatMessage() { }

    // Factory method 1: Tạo tin nhắn văn bản
    public static Result<ChatMessage> CreateText(Guid conversationId, Guid senderId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Result.Failure<ChatMessage>(new Error("ChatMessage.EmptyContent", "Nội dung tin nhắn không được để trống."));

        var trimmedContent = content.Trim();
        if (trimmedContent.Length > MaxTextLength)
            return Result.Failure<ChatMessage>(new Error("ChatMessage.ContentTooLong", $"Tin nhắn không được vượt quá {MaxTextLength} ký tự."));

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            Type = MessageType.Text,
            Content = trimmedContent,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        return Result.Success(message);
    }

    // Factory method 2: Tạo tin nhắn thoại
    public static Result<ChatMessage> CreateVoice(Guid conversationId, Guid senderId, string voiceUrl, int voiceDuration)
    {
        if (string.IsNullOrWhiteSpace(voiceUrl))
            return Result.Failure<ChatMessage>(new Error("ChatMessage.InvalidVoiceUrl", "Đường dẫn file ghi âm không hợp lệ."));

        if (voiceDuration < MinVoiceDurationSeconds || voiceDuration > MaxVoiceDurationSeconds)
            return Result.Failure<ChatMessage>(
                new Error("ChatMessage.InvalidVoiceDuration", $"Thời lượng ghi âm phải từ {MinVoiceDurationSeconds} đến {MaxVoiceDurationSeconds} giây.")
            );

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            Type = MessageType.Voice,
            VoiceUrl = voiceUrl.Trim(),
            VoiceDuration = voiceDuration,
            Content = "🎙️ Tin nhắn thoại",
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return Result.Success(message);
    }

    // Factory method 3: Tạo bản ghi nhận cuộc gọi
    public static Result<ChatMessage> CreateCallLog(Guid conversationId, Guid senderId, CallLogData callLogData)
    {
        if (callLogData is null)
            return Result.Failure<ChatMessage>(new Error("ChatMessage.NullCallLogData", "Dữ liệu cuộc gọi không được để trống."));

        var summaryText = callLogData.Status switch
        {
            CallStatus.Completed => $"📞 Cuộc gọi thoại ({callLogData.DurationSeconds}s)",
            CallStatus.Missed => "📞 Cuộc gọi nhỡ",
            CallStatus.Declined => "📞 Cuộc gọi bị từ chối",
            _ => "📞 Cuộc gọi thoại"
        };

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            Type = MessageType.CallLog,
            CallLogData = callLogData,
            Content = summaryText,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return Result.Success(message);
    }

    public void MarkRead()
    {
        IsRead = true;
    }
}