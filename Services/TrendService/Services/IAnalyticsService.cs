using TrendService.DTOs;

namespace TrendService.Services;

public interface IAnalyticsService
{
    Task<TrendOverviewDto> GetOverviewAsync();
    Task<KeywordTrendDto?> GetKeywordTrendAsync(Guid id);
    Task<JournalTrendDto?> GetJournalTrendAsync(Guid id);
    Task<TopicTrendDto?> GetTopicTrendAsync(Guid id);
    Task<AuthorTrendDto?> GetAuthorTrendAsync(Guid id);
    Task<List<JournalTrendSummaryDto>> GetTopJournalsAsync(int top);
    Task<List<TopKeywordDto>> GetTopKeywordsAsync(int top);
    Task<List<TopTopicDto>> GetHotTopicsAsync(int top);
    Task<List<TopAuthorDto>> GetTopAuthorsAsync(int top);
    Task LogSearchHistoryAsync(SearchHistoryLogDto dto);
    Task RecalculateSnapshotAsync(RecalculateSnapshotDto dto);
    Task RecalculateJournalSnapshotAsync(RecalculateJournalSnapshotDto dto);
    Task RecalculateTopicSnapshotAsync(RecalculateTopicSnapshotDto dto);
    Task RecalculateAuthorSnapshotAsync(RecalculateAuthorSnapshotDto dto);
}

