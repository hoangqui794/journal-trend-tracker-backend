using TrendService.DTOs;
using TrendService.Models;

namespace TrendService.Repositories;

public interface ITrendRepository
{
    Task<List<TrendSnapshot>> GetKeywordTrendAsync(Guid keywordId);
    Task<List<JournalTrendSnapshot>> GetJournalTrendAsync(Guid journalId);
    Task<List<TopicTrendSnapshot>> GetTopicTrendAsync(Guid topicId);
    Task<List<AuthorTrendSnapshot>> GetAuthorTrendAsync(Guid authorId);
    Task<List<JournalTrendSnapshot>> GetTopJournalsAsync(int top);
    Task<List<TopKeywordDto>> GetTopKeywordsAsync(int top);
    Task<List<TopTopicDto>> GetHotTopicsAsync(int top);
    Task<TrendOverviewDto> GetOverviewAsync();
    Task LogSearchHistoryAsync(SearchHistory history);
    Task UpsertSnapshotAsync(TrendSnapshot snapshot);
    Task UpsertJournalSnapshotAsync(JournalTrendSnapshot snapshot);
    Task UpsertTopicSnapshotAsync(TopicTrendSnapshot snapshot);
    Task UpsertAuthorSnapshotAsync(AuthorTrendSnapshot snapshot);
}
