using AIChatService.DTOs;

namespace AIChatService.Services
{
    public interface IChatService
    {
        Task<SessionResponse> CreateSessionAsync(Guid userId, CreateSessionRequest request);
        Task<IEnumerable<SessionResponse>> GetUserSessionsAsync(Guid userId);
        Task<IEnumerable<MessageResponse>> GetSessionMessagesAsync(Guid sessionId);
        Task<ChatResponse> AskAiAsync(Guid userId, ChatRequest request);
        Task<bool> DeleteSessionAsync(Guid userId, Guid sessionId);
    }
}
