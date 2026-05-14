using AIChatService.Models;

namespace AIChatService.Repositories
{
    public interface IChatRepository
    {
        // Session methods
        Task<ChatSession?> GetSessionByIdAsync(Guid id);
        Task<IEnumerable<ChatSession>> GetSessionsByUserIdAsync(Guid userId);
        Task CreateSessionAsync(ChatSession session);
        Task UpdateSessionAsync(ChatSession session);
        Task DeleteSessionAsync(ChatSession session);

        // Message methods
        Task<IEnumerable<ChatMessage>> GetMessagesBySessionIdAsync(Guid sessionId);
        Task AddMessageAsync(ChatMessage message);
        
        Task SaveChangesAsync();
    }
}
