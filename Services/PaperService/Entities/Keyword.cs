using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaperService.Entities
{
    [Table("keywords")]
    public class Keyword
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("term")]
        [MaxLength(255)]
        public string Term { get; set; } = string.Empty;

        [Required]
        [Column("normalized_term")]
        [MaxLength(255)]
        public string NormalizedTerm { get; set; } = string.Empty;

        [Required]
        [Column("source")]
        public KeywordSource Source { get; set; } = KeywordSource.Api;

        [Column("usage_count")]
        public int UsageCount { get; set; } = 0;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PaperKeyword> PaperKeywords { get; set; } = new List<PaperKeyword>();
    }
}
