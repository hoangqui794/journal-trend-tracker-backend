using PaperService.DTOs;

namespace PaperService.Services
{
    public interface IPaperService
    {
        Task<PagedResultDto<PaperSummaryDto>> SearchPapersAsync(PaperFilterDto filter);
        Task<PaperDetailDto?> GetPaperByIdAsync(Guid id);
        Task<IEnumerable<KeywordSuggestionDto>> GetKeywordSuggestionsAsync(string? query, int limit = 20);
    }
}
