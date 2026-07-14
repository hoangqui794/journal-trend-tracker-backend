namespace TrendService.DTOs;

public class TrendOverviewDto
{
    public int TotalPapers { get; set; }
    public int TotalKeywords { get; set; }
    public int TotalJournals { get; set; }
    public int TotalAuthors { get; set; }
}

public class KeywordTrendDto
{
    public Guid KeywordId { get; set; }
    public string KeywordTerm { get; set; } = string.Empty;
    public List<YearlyStatDto> Stats { get; set; } = new();
}

public class JournalTrendDto
{
    public Guid JournalId { get; set; }
    public string JournalName { get; set; } = string.Empty;
    public List<YearlyStatDto> Stats { get; set; } = new();
}

public class TopicTrendDto
{
    public Guid TopicId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public List<YearlyStatDto> Stats { get; set; } = new();
}

public class AuthorTrendDto
{
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public List<YearlyStatDto> Stats { get; set; } = new();
}

public class YearlyStatDto
{
    public int Year { get; set; }
    public int PaperCount { get; set; }
    public int CitationCount { get; set; }
    public double? GrowthRate { get; set; }
}

public class TopKeywordDto
{
    public Guid KeywordId { get; set; }
    public string KeywordTerm { get; set; } = string.Empty;
    public int PaperCount { get; set; }
    public double? GrowthRate { get; set; }
}

public class JournalTrendSummaryDto
{
    public Guid JournalId { get; set; }
    public string JournalName { get; set; } = string.Empty;
    public int PaperCount { get; set; }
    public int Year { get; set; }
}

public class TopTopicDto
{
    public Guid TopicId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public int PaperCount { get; set; }
    public double? GrowthRate { get; set; }
}

public class HotTopicDto
{
    public string Query { get; set; } = string.Empty;
    public int SearchCount { get; set; }
}

public class TopAuthorDto
{
    public Guid AuthorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Affiliation { get; set; }
    public int PaperCount { get; set; }
}

public class SearchHistoryLogDto
{
    public Guid? UserId { get; set; }
    public string Query { get; set; } = string.Empty;
    public string? SearchType { get; set; }
    public int ResultCount { get; set; }
}

public class RecalculateSnapshotDto
{
    public Guid KeywordId { get; set; }
    public string KeywordTerm { get; set; } = string.Empty;
    public int Year { get; set; }
    public int PaperCount { get; set; }
    public int CitationSum { get; set; }
}

public class RecalculateJournalSnapshotDto
{
    public Guid JournalId { get; set; }
    public string JournalName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int PaperCount { get; set; }
    public int CitationSum { get; set; }
}

public class RecalculateTopicSnapshotDto
{
    public Guid TopicId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int PaperCount { get; set; }
    public int CitationSum { get; set; }
}

public class RecalculateAuthorSnapshotDto
{
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int PaperCount { get; set; }
    public int CitationSum { get; set; }
}
