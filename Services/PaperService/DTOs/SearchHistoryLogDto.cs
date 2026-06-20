namespace PaperService.DTOs
{
    public class SearchHistoryLogDto
    {
        public string? UserId { get; set; }
        public string Query { get; set; } = string.Empty;
        public string SearchType { get; set; } = "keyword";
        public int ResultCount { get; set; }
    }
}
