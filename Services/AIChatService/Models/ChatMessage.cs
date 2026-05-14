using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIChatService.Models
{
    [Table("chat_messages")]
    public class ChatMessage
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("session_id")]
        public Guid SessionId { get; set; }

        [ForeignKey("SessionId")]
        public virtual ChatSession Session { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        [Column("sender_type")]
        public string SenderType { get; set; } = "User"; // User, AI

        [Required]
        [Column("content")]
        public string Content { get; set; } = string.Empty;

        [Column("prompt_tokens")]
        public int PromptTokens { get; set; } = 0;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
