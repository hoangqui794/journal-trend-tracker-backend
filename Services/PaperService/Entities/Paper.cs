using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaperService.Entities
{
    [Table("papers")]
    public class Paper
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("external_id")]
        [MaxLength(255)]
        public string ExternalId { get; set; } = string.Empty;

        [Required]
        [Column("source")]
        public PaperSource Source { get; set; }

        [Required]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("abstract")]
        public string? Abstract { get; set; }

        [Column("publication_year")]
        public short? PublicationYear { get; set; }

        [Column("doi")]
        [MaxLength(255)]
        public string? Doi { get; set; }

        [Column("url")]
        public string? Url { get; set; }

        [Column("citation_count")]
        public int CitationCount { get; set; } = 0;

        [Column("reference_count")]
        public int ReferenceCount { get; set; } = 0;

        [Column("fields_of_study")]
        public List<string>? FieldsOfStudy { get; set; }

        [Column("journal_id")]
        public Guid? JournalId { get; set; }
        
        [ForeignKey("JournalId")]
        public Journal? Journal { get; set; }

        [Column("raw_data", TypeName = "jsonb")]
        public string? RawData { get; set; }

        [Column("synced_at")]
        public DateTime SyncedAt { get; set; } = DateTime.UtcNow;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<PaperAuthor> PaperAuthors { get; set; } = new List<PaperAuthor>();
        public ICollection<PaperKeyword> PaperKeywords { get; set; } = new List<PaperKeyword>();
    }
}
