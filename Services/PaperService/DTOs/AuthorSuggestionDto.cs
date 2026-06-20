namespace PaperService.DTOs
{
    public class AuthorSuggestionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Affiliation { get; set; }
    }
}
