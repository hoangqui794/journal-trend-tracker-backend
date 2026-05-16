using TrendService.DTOs;
using TrendService.Models;

namespace TrendService.Repositories;

public interface ITrendRepository
{
    Task<List<TrendSnapshot>> GetKeywordTrendAsync(Guid keywordId);
    Task<List<JournalTrendSnapshot>> GetJournalTrendAsync(Guid journalId);
    Task<List<JournalTrendSnapshot>> GetTopJournalsAsync(int top);
    Task<List<TopKeywordDto>> GetTopKeywordsAsync(int top);
    Task<List<HotTopicDto>> GetHotTopicsAsync(int top);
    Task<TrendOverviewDto> GetOverviewAsync();
    Task LogSearchHistoryAsync(SearchHistory history);
    Task UpsertSnapshotAsync(TrendSnapshot snapshot);
}
