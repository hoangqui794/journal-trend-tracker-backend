using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaperService.Entities
{
    [Table("journals")]
    public class Journal
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("external_id")]
        [MaxLength(255)]
        public string? ExternalId { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(500)]
        public string Name { get; set; } = string.Empty;

        [Column("issn")]
        [MaxLength(20)]
        public string? Issn { get; set; }

        [Column("e_issn")]
        [MaxLength(20)]
        public string? EIssn { get; set; }

        [Column("publisher")]
        [MaxLength(255)]
        public string? Publisher { get; set; }

        [Column("field")]
        [MaxLength(255)]
        public string? Field { get; set; }

        [Column("homepage_url")]
        public string? HomepageUrl { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Paper> Papers { get; set; } = new List<Paper>();
    }
}
