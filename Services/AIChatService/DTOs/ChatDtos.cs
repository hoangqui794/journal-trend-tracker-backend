namespace AIChatService.DTOs
{
    public class CreateSessionRequest
    {
        public Guid? DocumentId { get; set; }
        public string Title { get; set; } = "New Chat";
    }

    public class ChatRequest
    {
        public Guid SessionId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Context { get; set; } // Dữ liệu bổ sung gửi kèm để AI đọc
    }

    public class ChatResponse
    {
        public string Answer { get; set; } = string.Empty;
        public Guid MessageId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SessionResponse
    {
        public Guid Id { get; set; }
        public Guid? DocumentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class MessageResponse
    {
        public Guid Id { get; set; }
        public string SenderType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
