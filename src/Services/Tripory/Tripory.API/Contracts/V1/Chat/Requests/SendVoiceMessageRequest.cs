namespace Tripory.API.Contracts.V1.Chat.Requests
{
    public record SendVoiceMessageRequest(string VoiceUrl, int VoiceDuration);
}