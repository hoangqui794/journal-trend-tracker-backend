namespace PaperService.DTOs
{
    public class RecalculateJournalSnapshotDto
    {
        public Guid JournalId { get; set; }
        public string JournalName { get; set; } = string.Empty;
        public short Year { get; set; }
        public int PaperCount { get; set; }
        public int CitationSum { get; set; }
    }

    public class RecalculateTopicSnapshotDto
    {
        public string TopicId { get; set; } = string.Empty; // using string name as ID for topic since we use string for field of study
        public string TopicName { get; set; } = string.Empty;
        public short Year { get; set; }
        public int PaperCount { get; set; }
        public int CitationSum { get; set; }
    }

    public class RecalculateAuthorSnapshotDto
    {
        public Guid AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public short Year { get; set; }
        public int PaperCount { get; set; }
        public int CitationSum { get; set; }
    }
}
