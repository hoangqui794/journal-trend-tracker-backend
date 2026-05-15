namespace PaperService.DTOs
{
    public class PaperSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Abstract { get; set; }
        public short? PublicationYear { get; set; }
        public string? JournalName { get; set; }
        public string Source { get; set; } = string.Empty;
        public int CitationCount { get; set; }
        public IEnumerable<string> Authors { get; set; } = new List<string>();
        public IEnumerable<string> Keywords { get; set; } = new List<string>();
    }
}
