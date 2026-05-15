namespace PaperService.DTOs
{
    public class PaperFilterDto
    {
        public string? Keyword { get; set; }
        public short? Year { get; set; }
        public Guid? JournalId { get; set; }
        public Guid? AuthorId { get; set; }
        public string? Source { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
