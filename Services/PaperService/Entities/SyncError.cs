using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaperService.Entities
{
    [Table("sync_errors")]
    public class SyncError
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("job_id")]
        public Guid JobId { get; set; }
        
        [ForeignKey("JobId")]
        public ApiSyncJob? Job { get; set; }

        [Column("external_id")]
        [MaxLength(255)]
        public string? ExternalId { get; set; }

        [Column("error_type")]
        [MaxLength(100)]
        public string? ErrorType { get; set; }

        [Column("error_detail")]
        public string? ErrorDetail { get; set; }

        [Column("occurred_at")]
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
