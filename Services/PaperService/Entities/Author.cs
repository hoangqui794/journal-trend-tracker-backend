using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaperService.Entities
{
    [Table("authors")]
    public class Author
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("external_id")]
        [MaxLength(255)]
        public string? ExternalId { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Column("affiliation")]
        [MaxLength(500)]
        public string? Affiliation { get; set; }

        [Column("orcid")]
        [MaxLength(50)]
        public string? Orcid { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PaperAuthor> PaperAuthors { get; set; } = new List<PaperAuthor>();
    }
}
