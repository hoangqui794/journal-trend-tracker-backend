namespace PaperService.DTOs
{
    public class AuthorTopDto
    {
        public Guid AuthorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Affiliation { get; set; }
        public int PaperCount { get; set; }
    }
}