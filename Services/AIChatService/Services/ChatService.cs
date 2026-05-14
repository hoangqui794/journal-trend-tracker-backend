using AIChatService.DTOs;
using AIChatService.Models;
using AIChatService.Repositories;

namespace AIChatService.Services
{
    public class ChatService : IChatService
    {
        private readonly IChatRepository _repository;
        private readonly IGeminiService _geminiService;

        public ChatService(IChatRepository repository, IGeminiService geminiService)
        {
            _repository = repository;
            _geminiService = geminiService;
        }

        public async Task<SessionResponse> CreateSessionAsync(Guid userId, CreateSessionRequest request)
        {
            var session = new ChatSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DocumentId = request.DocumentId,
                Title = request.Title,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.CreateSessionAsync(session);
            await _repository.SaveChangesAsync();

            return new SessionResponse
            {
                Id = session.Id,
                DocumentId = session.DocumentId,
                Title = session.Title,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt
            };
        }

        public async Task<IEnumerable<SessionResponse>> GetUserSessionsAsync(Guid userId)
        {
            var sessions = await _repository.GetSessionsByUserIdAsync(userId);
            return sessions.Select(s => new SessionResponse
            {
                Id = s.Id,
                DocumentId = s.DocumentId,
                Title = s.Title,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            });
        }

        public async Task<IEnumerable<MessageResponse>> GetSessionMessagesAsync(Guid sessionId)
        {
            var messages = await _repository.GetMessagesBySessionIdAsync(sessionId);
            return messages.Select(m => new MessageResponse
            {
                Id = m.Id,
                SenderType = m.SenderType,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            });
        }

        public async Task<ChatResponse> AskAiAsync(Guid userId, ChatRequest request)
        {
            var session = await _repository.GetSessionByIdAsync(request.SessionId);
            if (session == null || session.UserId != userId)
            {
                throw new KeyNotFoundException("Phiên chat không tồn tại hoặc không thuộc về bạn.");
            }

            // 1. Lưu tin nhắn của User
            var userMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                SenderType = "User",
                Content = request.Message,
                CreatedAt = DateTime.UtcNow
            };
            await _repository.AddMessageAsync(userMessage);

            // 2. Lấy context (RAG)
            string contextData = request.Context ?? ""; // Ưu tiên context gửi trực tiếp từ API test
            
            if (string.IsNullOrEmpty(contextData) && session.DocumentId.HasValue)
            {
                // TODO: Gọi DocumentService khi có DocumentId mà không có context gửi kèm
                contextData = "[Hệ thống: Đang sử dụng dữ liệu giả lập cho tài liệu ID " + session.DocumentId + "]";
            }

            // 3. Gọi AI
            var aiAnswer = await _geminiService.GenerateResponseAsync(request.Message, contextData);

            // 4. Lưu tin nhắn AI
            var aiMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                SenderType = "AI",
                Content = aiAnswer,
                CreatedAt = DateTime.UtcNow
            };
            await _repository.AddMessageAsync(aiMessage);

            // 5. Cập nhật session
            session.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateSessionAsync(session);

            await _repository.SaveChangesAsync();

            return new ChatResponse
            {
                Answer = aiAnswer,
                MessageId = aiMessage.Id,
                CreatedAt = aiMessage.CreatedAt
            };
        }

        public async Task<bool> DeleteSessionAsync(Guid userId, Guid sessionId)
        {
            var session = await _repository.GetSessionByIdAsync(sessionId);
            if (session == null || session.UserId != userId) return false;

            await _repository.DeleteSessionAsync(session);
            await _repository.SaveChangesAsync();
            return true;
        }
    }
}
