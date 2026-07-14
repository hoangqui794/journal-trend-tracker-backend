using Microsoft.EntityFrameworkCore;
using TrendService.DBContext;
using TrendService.DTOs;
using TrendService.Models;

namespace TrendService.Repositories;

public class TrendRepository : ITrendRepository
{
    private readonly TrendDbContext _context;

    public TrendRepository(TrendDbContext context)
    {
        _context = context;
    }

    public async Task<List<TrendSnapshot>> GetKeywordTrendAsync(Guid keywordId)
    {
        return await _context.TrendSnapshots
            .Where(t => t.KeywordId == keywordId)
            .OrderBy(t => t.Year)
            .ToListAsync();
    }

    public async Task<List<JournalTrendSnapshot>> GetJournalTrendAsync(Guid journalId)
    {
        return await _context.JournalTrendSnapshots
            .Where(j => j.JournalId == journalId)
            .OrderBy(j => j.Year)
            .ToListAsync();
    }

    public async Task<List<TopicTrendSnapshot>> GetTopicTrendAsync(Guid topicId)
    {
        return await _context.TopicTrendSnapshots
            .Where(t => t.TopicId == topicId)
            .OrderBy(t => t.Year)
            .ToListAsync();
    }

    public async Task<List<AuthorTrendSnapshot>> GetAuthorTrendAsync(Guid authorId)
    {
        return await _context.AuthorTrendSnapshots
            .Where(a => a.AuthorId == authorId)
            .OrderBy(a => a.Year)
            .ToListAsync();
    }

    public async Task<List<JournalTrendSnapshot>> GetTopJournalsAsync(int top)
    {
        return await _context.JournalTrendSnapshots
            .GroupBy(j => new { j.JournalId, j.JournalName })
            .Select(g => new JournalTrendSnapshot
            {
                JournalId = g.Key.JournalId,
                JournalName = g.Key.JournalName,
                PaperCount = g.Sum(x => x.PaperCount),
                CitationSum = g.Sum(x => x.CitationSum),
                Year = g.Max(x => x.Year)
            })
            .OrderByDescending(j => j.PaperCount)
            .Take(top)
            .ToListAsync();
    }

    public async Task<List<TopKeywordDto>> GetTopKeywordsAsync(int top)
    {
        return await _context.TrendSnapshots
            .GroupBy(t => new { t.KeywordId, t.KeywordTerm })
            .Select(g => new TopKeywordDto
            {
                KeywordId = g.Key.KeywordId,
                KeywordTerm = g.Key.KeywordTerm,
                PaperCount = g.Sum(x => x.PaperCount),
                GrowthRate = g.OrderByDescending(x => x.Year)
                               .Select(x => x.GrowthRate)
                               .FirstOrDefault()
            })
            .OrderByDescending(t => t.PaperCount)
            .Take(top)
            .ToListAsync();
    }

    public async Task<List<TopTopicDto>> GetHotTopicsAsync(int top)
    {
        var currentYear = (short)DateTime.UtcNow.Year;
        return await _context.TopicTrendSnapshots
            .Where(t => t.Year == currentYear || t.Year == currentYear - 1)
            .GroupBy(t => new { t.TopicId, t.TopicName })
            .Select(g => new TopTopicDto
            {
                TopicId = g.Key.TopicId,
                TopicName = g.Key.TopicName,
                PaperCount = g.Sum(x => x.PaperCount),
                GrowthRate = g.OrderByDescending(x => x.Year).Select(x => x.GrowthRate).FirstOrDefault()
            })
            .OrderByDescending(t => t.GrowthRate)
            .ThenByDescending(t => t.PaperCount)
            .Take(top)
            .ToListAsync();
    }

    public async Task<TrendOverviewDto> GetOverviewAsync()
    {
        var maxPapersPerKeyword = await _context.TrendSnapshots
            .GroupBy(s => s.KeywordId)
            .Select(g => g.Max(s => s.PaperCount))
            .ToListAsync();
        var totalPapers = maxPapersPerKeyword.Sum();

        var totalKeywords = await _context.TrendSnapshots
            .Select(s => s.KeywordId)
            .Distinct()
            .CountAsync();

        var totalJournals = await _context.JournalTrendSnapshots
            .Select(s => s.JournalId)
            .Distinct()
            .CountAsync();

        return new TrendOverviewDto
        {
            TotalPapers = totalPapers,
            TotalKeywords = totalKeywords,
            TotalJournals = totalJournals,
            TotalAuthors = 0 
        };
    }

    public async Task LogSearchHistoryAsync(SearchHistory history)
    {
        await _context.SearchHistories.AddAsync(history);
        await _context.SaveChangesAsync();
    }

    public async Task UpsertSnapshotAsync(TrendSnapshot snapshot)
    {
        var existing = await _context.TrendSnapshots
            .FirstOrDefaultAsync(t => t.KeywordId == snapshot.KeywordId
                                   && t.Year == snapshot.Year);
        if (existing == null)
        {
            await _context.TrendSnapshots.AddAsync(snapshot);
        }
        else
        {
            var prevYear = await _context.TrendSnapshots
                .FirstOrDefaultAsync(t => t.KeywordId == snapshot.KeywordId
                                       && t.Year == snapshot.Year - 1);

            existing.PaperCount = snapshot.PaperCount;
            existing.CitationSum = snapshot.CitationSum;
            existing.RecordedAt = DateTime.UtcNow;
            existing.GrowthRate = prevYear != null && prevYear.PaperCount > 0
                ? Math.Round((double)(snapshot.PaperCount - prevYear.PaperCount)
                             / prevYear.PaperCount * 100, 2)
                : null;
        }
        await _context.SaveChangesAsync();
    }

    public async Task UpsertJournalSnapshotAsync(JournalTrendSnapshot snapshot)
    {
        var existing = await _context.JournalTrendSnapshots
            .FirstOrDefaultAsync(t => t.JournalId == snapshot.JournalId
                                   && t.Year == snapshot.Year);
        if (existing == null)
        {
            await _context.JournalTrendSnapshots.AddAsync(snapshot);
        }
        else
        {
            var prevYear = await _context.JournalTrendSnapshots
                .FirstOrDefaultAsync(t => t.JournalId == snapshot.JournalId
                                       && t.Year == snapshot.Year - 1);

            existing.PaperCount = snapshot.PaperCount;
            existing.CitationSum = snapshot.CitationSum;
            existing.RecordedAt = DateTime.UtcNow;
            existing.GrowthRate = prevYear != null && prevYear.PaperCount > 0
                ? Math.Round((double)(snapshot.PaperCount - prevYear.PaperCount)
                             / prevYear.PaperCount * 100, 2)
                : null;
        }
        await _context.SaveChangesAsync();
    }

    public async Task UpsertTopicSnapshotAsync(TopicTrendSnapshot snapshot)
    {
        var existing = await _context.TopicTrendSnapshots
            .FirstOrDefaultAsync(t => t.TopicId == snapshot.TopicId
                                   && t.Year == snapshot.Year);
        if (existing == null)
        {
            await _context.TopicTrendSnapshots.AddAsync(snapshot);
        }
        else
        {
            var prevYear = await _context.TopicTrendSnapshots
                .FirstOrDefaultAsync(t => t.TopicId == snapshot.TopicId
                                       && t.Year == snapshot.Year - 1);

            existing.PaperCount = snapshot.PaperCount;
            existing.CitationSum = snapshot.CitationSum;
            existing.RecordedAt = DateTime.UtcNow;
            existing.GrowthRate = prevYear != null && prevYear.PaperCount > 0
                ? Math.Round((double)(snapshot.PaperCount - prevYear.PaperCount)
                             / prevYear.PaperCount * 100, 2)
                : null;
        }
        await _context.SaveChangesAsync();
    }

    public async Task UpsertAuthorSnapshotAsync(AuthorTrendSnapshot snapshot)
    {
        var existing = await _context.AuthorTrendSnapshots
            .FirstOrDefaultAsync(t => t.AuthorId == snapshot.AuthorId
                                   && t.Year == snapshot.Year);
        if (existing == null)
        {
            await _context.AuthorTrendSnapshots.AddAsync(snapshot);
        }
        else
        {
            var prevYear = await _context.AuthorTrendSnapshots
                .FirstOrDefaultAsync(t => t.AuthorId == snapshot.AuthorId
                                       && t.Year == snapshot.Year - 1);

            existing.PaperCount = snapshot.PaperCount;
            existing.CitationSum = snapshot.CitationSum;
            existing.RecordedAt = DateTime.UtcNow;
            existing.GrowthRate = prevYear != null && prevYear.PaperCount > 0
                ? Math.Round((double)(snapshot.PaperCount - prevYear.PaperCount)
                             / prevYear.PaperCount * 100, 2)
                : null;
        }
        await _context.SaveChangesAsync();
    }

}
