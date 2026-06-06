using System;

namespace PaperService.DTOs
{
    public class RecalculateSnapshotDto
    {
        public Guid KeywordId { get; set; }
        public string KeywordTerm { get; set; } = string.Empty;
        public int Year { get; set; }
        public int PaperCount { get; set; }
        public int CitationSum { get; set; }
    }
}
