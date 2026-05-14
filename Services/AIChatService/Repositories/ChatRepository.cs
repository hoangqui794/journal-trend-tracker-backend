using AIChatService.Data;
using AIChatService.Models;
using Microsoft.EntityFrameworkCore;

namespace AIChatService.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly AIChatDbContext _context;

        public ChatRepository(AIChatDbContext context)
        {
            _context = context;
        }

        public async Task<ChatSession?> GetSessionByIdAsync(Guid id)
        {
            return await _context.ChatSessions.FindAsync(id);
        }

        public async Task<IEnumerable<ChatSession>> GetSessionsByUserIdAsync(Guid userId)
        {
            return await _context.ChatSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.UpdatedAt)
                .ToListAsync();
        }

        public async Task CreateSessionAsync(ChatSession session)
        {
            await _context.ChatSessions.AddAsync(session);
        }

        public async Task UpdateSessionAsync(ChatSession session)
        {
            _context.ChatSessions.Update(session);
            await Task.CompletedTask;
        }

        public async Task DeleteSessionAsync(ChatSession session)
        {
            _context.ChatSessions.Remove(session);
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<ChatMessage>> GetMessagesBySessionIdAsync(Guid sessionId)
        {
            return await _context.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task AddMessageAsync(ChatMessage message)
        {
            await _context.ChatMessages.AddAsync(message);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
