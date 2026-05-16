using TrendService.DTOs;
using TrendService.Models;
using TrendService.Repositories;

namespace TrendService.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ITrendRepository _repo;
    private readonly IPaperServiceClient _paperClient;

    public AnalyticsService(ITrendRepository repo, IPaperServiceClient paperClient)
    {
        _repo = repo;
        _paperClient = paperClient;
    }

    public async Task<TrendOverviewDto> GetOverviewAsync()
    {
        var overview = await _repo.GetOverviewAsync();

        // Lấy tổng authors từ PaperService
        overview.TotalAuthors = await _paperClient.GetTotalAuthorsAsync();

        return overview;
    }

    public async Task<KeywordTrendDto?> GetKeywordTrendAsync(Guid id)
    {
        var data = await _repo.GetKeywordTrendAsync(id);
        if (data == null || !data.Any()) return null;

        return new KeywordTrendDto
        {
            KeywordId = id,
            KeywordTerm = data[0].KeywordTerm,
            Stats = data.Select(d => new YearlyStatDto
            {
                Year = d.Year,
                PaperCount = d.PaperCount,
                CitationCount = d.CitationSum,
                GrowthRate = d.GrowthRate
            }).ToList()
        };
    }

    public async Task<JournalTrendDto?> GetJournalTrendAsync(Guid id)
    {
        var data = await _repo.GetJournalTrendAsync(id);
        if (data == null || !data.Any()) return null;

        return new JournalTrendDto
        {
            JournalId = id,
            JournalName = data[0].JournalName,
            Stats = data.Select(d => new YearlyStatDto
            {
                Year = d.Year,
                PaperCount = d.PaperCount,
                CitationCount = d.CitationSum,
                GrowthRate = d.GrowthRate
            }).ToList()
        };
    }

    public async Task<List<JournalTrendSummaryDto>> GetTopJournalsAsync(int top)
    {
        var journals = await _repo.GetTopJournalsAsync(top);
        return journals.Select(d => new JournalTrendSummaryDto
        {
            JournalId = d.JournalId,
            JournalName = d.JournalName,
            PaperCount = d.PaperCount,
            Year = d.Year
        }).ToList();
    }

    public async Task<List<TopKeywordDto>> GetTopKeywordsAsync(int top)
    {
        return await _repo.GetTopKeywordsAsync(top);
    }

    public async Task<List<HotTopicDto>> GetHotTopicsAsync(int top)
    {
        return await _repo.GetHotTopicsAsync(top);
    }

    public async Task<List<TopAuthorDto>> GetTopAuthorsAsync(int top)
    {
        // Gọi sang PaperService để lấy top authors
        return await _paperClient.GetTopAuthorsAsync(top);
    }

    public async Task LogSearchHistoryAsync(SearchHistoryLogDto dto)
    {
        var history = new SearchHistory
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            Query = dto.Query,
            SearchType = dto.SearchType,
            ResultCount = dto.ResultCount,
            CreatedAt = DateTime.UtcNow
        };
        await _repo.LogSearchHistoryAsync(history);
    }

    public async Task RecalculateSnapshotAsync(RecalculateSnapshotDto dto)
    {
        var snapshot = new TrendSnapshot
        {
            Id = Guid.NewGuid(),
            KeywordId = dto.KeywordId,
            KeywordTerm = dto.KeywordTerm,
            Year = (short)dto.Year,
            PaperCount = dto.PaperCount,
            CitationSum = dto.CitationSum,
            RecordedAt = DateTime.UtcNow
        };
        await _repo.UpsertSnapshotAsync(snapshot);
    }
}
