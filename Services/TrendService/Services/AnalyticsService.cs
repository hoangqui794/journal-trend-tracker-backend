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

        var stats = data.Select(d => new YearlyStatDto
        {
            Year = d.Year,
            PaperCount = d.PaperCount,
            CitationCount = d.CitationSum,
            GrowthRate = d.GrowthRate
        }).ToList();
        
        for (int i = 0; i < stats.Count; i++) {
            stats[i].ForecastPaperCount = CalculateForecast(stats, i);
        }

        var result = new KeywordTrendDto
        {
            KeywordId = id,
            KeywordTerm = data[0].KeywordTerm,
            Stats = stats
        };

        return result;
    }
    
    private int? CalculateForecast(List<YearlyStatDto> stats, int i)
    {
        if (i == 0) return null;
        var diff = stats[i].PaperCount - stats[i-1].PaperCount;
        var forecast = stats[i].PaperCount + diff;
        return forecast > 0 ? forecast : 0;
    }

    public async Task<JournalTrendDto?> GetJournalTrendAsync(Guid id)
    {
        var data = await _repo.GetJournalTrendAsync(id);
        if (data == null || !data.Any()) return null;

        var stats = data.Select(d => new YearlyStatDto
        {
            Year = d.Year,
            PaperCount = d.PaperCount,
            CitationCount = d.CitationSum,
            GrowthRate = d.GrowthRate
        }).ToList();
        
        for (int i = 0; i < stats.Count; i++) {
            stats[i].ForecastPaperCount = CalculateForecast(stats, i);
        }

        return new JournalTrendDto
        {
            JournalId = id,
            JournalName = data[0].JournalName,
            Stats = stats
        };
    }

    public async Task<TopicTrendDto?> GetTopicTrendAsync(Guid id)
    {
        var data = await _repo.GetTopicTrendAsync(id);
        if (data == null || !data.Any()) return null;

        var stats = data.Select(d => new YearlyStatDto
        {
            Year = d.Year,
            PaperCount = d.PaperCount,
            CitationCount = d.CitationSum,
            GrowthRate = d.GrowthRate
        }).ToList();
        
        for (int i = 0; i < stats.Count; i++) {
            stats[i].ForecastPaperCount = CalculateForecast(stats, i);
        }

        return new TopicTrendDto
        {
            TopicId = id,
            TopicName = data[0].TopicName,
            Stats = stats
        };
    }

    public async Task<AuthorTrendDto?> GetAuthorTrendAsync(Guid id)
    {
        var data = await _repo.GetAuthorTrendAsync(id);
        if (data == null || !data.Any()) return null;

        var stats = data.Select(d => new YearlyStatDto
        {
            Year = d.Year,
            PaperCount = d.PaperCount,
            CitationCount = d.CitationSum,
            GrowthRate = d.GrowthRate
        }).ToList();
        
        for (int i = 0; i < stats.Count; i++) {
            stats[i].ForecastPaperCount = CalculateForecast(stats, i);
        }

        return new AuthorTrendDto
        {
            AuthorId = id,
            AuthorName = data[0].AuthorName,
            Stats = stats
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

    public async Task<List<TopTopicDto>> GetHotTopicsAsync(int top)
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

    public async Task RecalculateJournalSnapshotAsync(RecalculateJournalSnapshotDto dto)
    {
        var snapshot = new JournalTrendSnapshot
        {
            Id = Guid.NewGuid(),
            JournalId = dto.JournalId,
            JournalName = dto.JournalName,
            Year = (short)dto.Year,
            PaperCount = dto.PaperCount,
            CitationSum = dto.CitationSum,
            RecordedAt = DateTime.UtcNow
        };
        await _repo.UpsertJournalSnapshotAsync(snapshot);
    }

    public async Task RecalculateTopicSnapshotAsync(RecalculateTopicSnapshotDto dto)
    {
        var snapshot = new TopicTrendSnapshot
        {
            Id = Guid.NewGuid(),
            TopicId = dto.TopicId,
            TopicName = dto.TopicName,
            Year = (short)dto.Year,
            PaperCount = dto.PaperCount,
            CitationSum = dto.CitationSum,
            RecordedAt = DateTime.UtcNow
        };
        await _repo.UpsertTopicSnapshotAsync(snapshot);
    }

    public async Task RecalculateAuthorSnapshotAsync(RecalculateAuthorSnapshotDto dto)
    {
        var snapshot = new AuthorTrendSnapshot
        {
            Id = Guid.NewGuid(),
            AuthorId = dto.AuthorId,
            AuthorName = dto.AuthorName,
            Year = (short)dto.Year,
            PaperCount = dto.PaperCount,
            CitationSum = dto.CitationSum,
            RecordedAt = DateTime.UtcNow
        };
        await _repo.UpsertAuthorSnapshotAsync(snapshot);
    }

}
