using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaperService.Entities
{
    [Table("api_sync_jobs")]
    public class ApiSyncJob
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("source_name")]
        [MaxLength(100)]
        public string SourceName { get; set; } = string.Empty;

        [Required]
        [Column("source_base_url")]
        public string SourceBaseUrl { get; set; } = string.Empty;

        [Column("query_params", TypeName = "jsonb")]
        public string? QueryParams { get; set; }

        [Column("scheduled_at")]
        public DateTime? ScheduledAt { get; set; }

        [Column("started_at")]
        public DateTime? StartedAt { get; set; }

        [Column("finished_at")]
        public DateTime? FinishedAt { get; set; }

        [Required]
        [Column("status")]
        public SyncStatus Status { get; set; } = SyncStatus.Running;

        [Column("papers_fetched")]
        public int PapersFetched { get; set; } = 0;

        [Column("papers_inserted")]
        public int PapersInserted { get; set; } = 0;

        [Column("papers_updated")]
        public int PapersUpdated { get; set; } = 0;

        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<SyncError> SyncErrors { get; set; } = new List<SyncError>();
    }
}
