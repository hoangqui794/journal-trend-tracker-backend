using PaperService.DTOs;

namespace PaperService.Clients
{
    public interface ITrendServiceClient
    {
        Task LogSearchHistoryAsync(SearchHistoryLogDto dto);
    }
}
