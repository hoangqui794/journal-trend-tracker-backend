using Microsoft.EntityFrameworkCore;
using PaperService.Data;
using PaperService.DTOs;

namespace PaperService.Services
{
    public class PaperServiceImpl : IPaperService
    {
        private readonly PaperDbContext _context;

        public PaperServiceImpl(PaperDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResultDto<PaperSummaryDto>> SearchPapersAsync(PaperFilterDto filter)
        {
            var query = _context.Papers
                .Include(p => p.Journal)
                .Include(p => p.PaperAuthors).ThenInclude(pa => pa.Author)
                .Include(p => p.PaperKeywords).ThenInclude(pk => pk.Keyword)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.ToLower();
                query = query.Where(p => 
                    p.Title.ToLower().Contains(kw) || 
                    (p.Abstract != null && p.Abstract.ToLower().Contains(kw)) ||
                    p.PaperKeywords.Any(pk => pk.Keyword != null && pk.Keyword.Term.ToLower().Contains(kw))
                );
            }

            if (filter.Year.HasValue)
            {
                query = query.Where(p => p.PublicationYear == filter.Year.Value);
            }

            if (filter.JournalId.HasValue)
            {
                query = query.Where(p => p.JournalId == filter.JournalId.Value);
            }

            if (filter.AuthorId.HasValue)
            {
                query = query.Where(p => p.PaperAuthors.Any(pa => pa.AuthorId == filter.AuthorId.Value));
            }

            if (!string.IsNullOrWhiteSpace(filter.Source))
            {
                if (Enum.TryParse<Entities.PaperSource>(filter.Source, true, out var sourceEnum))
                {
                    query = query.Where(p => p.Source == sourceEnum);
                }
            }

            var totalCount = await query.CountAsync();

            var papers = await query
                .OrderByDescending(p => p.PublicationYear)
                .ThenByDescending(p => p.CitationCount)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var items = papers.Select(p => new PaperSummaryDto
            {
                Id = p.Id,
                Title = p.Title,
                Abstract = p.Abstract,
                PublicationYear = p.PublicationYear,
                JournalName = p.Journal?.Name,
                Source = p.Source.ToString(),
                CitationCount = p.CitationCount,
                Authors = p.PaperAuthors.OrderBy(pa => pa.AuthorOrder).Select(pa => pa.Author?.Name ?? string.Empty).ToList(),
                Keywords = p.PaperKeywords.Select(pk => pk.Keyword?.Term ?? string.Empty).ToList()
            });

            return new PagedResultDto<PaperSummaryDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<PaperDetailDto?> GetPaperByIdAsync(Guid id)
        {
            var paper = await _context.Papers
                .Include(p => p.Journal)
                .Include(p => p.PaperAuthors).ThenInclude(pa => pa.Author)
                .Include(p => p.PaperKeywords).ThenInclude(pk => pk.Keyword)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (paper == null) return null;

            return new PaperDetailDto
            {
                Id = paper.Id,
                ExternalId = paper.ExternalId,
                Source = paper.Source.ToString(),
                Title = paper.Title,
                Abstract = paper.Abstract,
                PublicationYear = paper.PublicationYear,
                Doi = paper.Doi,
                Url = paper.Url,
                CitationCount = paper.CitationCount,
                ReferenceCount = paper.ReferenceCount,
                FieldsOfStudy = paper.FieldsOfStudy,
                Journal = paper.Journal != null ? new JournalDto
                {
                    Id = paper.Journal.Id,
                    Name = paper.Journal.Name
                } : null,
                Authors = paper.PaperAuthors.OrderBy(pa => pa.AuthorOrder).Select(pa => new AuthorDto
                {
                    Id = pa.AuthorId,
                    Name = pa.Author?.Name ?? string.Empty,
                    Affiliation = pa.Author?.Affiliation,
                    AuthorOrder = pa.AuthorOrder
                }).ToList(),
                Keywords = paper.PaperKeywords.Select(pk => new KeywordDto
                {
                    Id = pk.KeywordId,
                    Term = pk.Keyword?.Term ?? string.Empty,
                    RelevanceScore = pk.RelevanceScore
                }).ToList()
            };
        }

        public async Task<IEnumerable<KeywordSuggestionDto>> GetKeywordSuggestionsAsync(string? query, int limit = 20)
        {
            var q = _context.Keywords.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var kw = query.ToLower();
                q = q.Where(k => k.Term.ToLower().Contains(kw));
            }

            return await q.OrderByDescending(k => k.UsageCount)
                .Take(limit)
                .Select(k => new KeywordSuggestionDto
                {
                    Id = k.Id,
                    Term = k.Term
                })
                .ToListAsync();
        }
    }
}
