using System;

namespace PaperService.DTOs
{
    public class ApiSyncJobDto
    {
        public Guid Id { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string SourceBaseUrl { get; set; } = string.Empty;
        public string? QueryParams { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int PapersFetched { get; set; }
        public int PapersInserted { get; set; }
        public int PapersUpdated { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
