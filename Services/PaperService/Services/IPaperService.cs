using PaperService.DTOs;

namespace PaperService.Services
{
    public interface IPaperService
    {
        Task<PagedResultDto<PaperSummaryDto>> SearchPapersAsync(PaperFilterDto filter);
    }
}
