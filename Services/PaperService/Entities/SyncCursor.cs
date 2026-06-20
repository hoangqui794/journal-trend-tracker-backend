using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaperService.Entities
{
    [Table("sync_cursors")]
    public class SyncCursor
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("source_name")]
        [MaxLength(100)]
        public string SourceName { get; set; } = string.Empty;

        [Column("last_cursor")]
        public string? LastCursor { get; set; }

        [Column("last_synced_at")]
        public DateTime? LastSyncedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
