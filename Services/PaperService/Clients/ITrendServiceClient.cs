using PaperService.DTOs;
using System.Threading.Tasks;

namespace PaperService.Clients
{
    public interface ITrendServiceClient
    {
        Task LogSearchHistoryAsync(SearchHistoryLogDto dto);
        Task RecalculateSnapshotAsync(RecalculateSnapshotDto dto);
    }
}
