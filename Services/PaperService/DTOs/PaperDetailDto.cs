namespace PaperService.DTOs
{
    public class PaperDetailDto
    {
        public Guid Id { get; set; }
        public string ExternalId { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Abstract { get; set; }
        public short? PublicationYear { get; set; }
        public string? Doi { get; set; }
        public string? Url { get; set; }
        public int CitationCount { get; set; }
        public int ReferenceCount { get; set; }
        public List<string>? FieldsOfStudy { get; set; }
        public string? PdfUrl { get; set; }
        public bool HasFullText { get; set; }
        
        public JournalDto? Journal { get; set; }
        public IEnumerable<AuthorDto> Authors { get; set; } = new List<AuthorDto>();
        public IEnumerable<KeywordDto> Keywords { get; set; } = new List<KeywordDto>();
    }

    public class JournalDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class AuthorDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Affiliation { get; set; }
        public short AuthorOrder { get; set; }
    }

    public class KeywordDto
    {
        public Guid Id { get; set; }
        public string Term { get; set; } = string.Empty;
        public double? RelevanceScore { get; set; }
    }
}
