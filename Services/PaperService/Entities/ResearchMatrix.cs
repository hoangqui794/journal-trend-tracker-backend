using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaperService.Entities
{
    [Table("research_matrices")]
    public class ResearchMatrix
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("user_id")]
        public Guid? UserId { get; set; }

        [Required]
        [Column("user_idea_prompt")]
        public string UserIdeaPrompt { get; set; } = string.Empty;

        [Required]
        [Column("matrix_data", TypeName = "jsonb")]
        public string MatrixDataJson { get; set; } = string.Empty;

        [Column("paper_ids", TypeName = "jsonb")]
        public string PaperIdsJson { get; set; } = "[]";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
