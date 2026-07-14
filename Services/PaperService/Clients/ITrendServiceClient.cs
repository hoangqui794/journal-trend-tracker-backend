using PaperService.DTOs;
using System.Threading.Tasks;

namespace PaperService.Clients
{
    public interface ITrendServiceClient
    {
        Task LogSearchHistoryAsync(SearchHistoryLogDto dto);
        Task RecalculateSnapshotAsync(RecalculateSnapshotDto dto);
        Task RecalculateJournalSnapshotAsync(RecalculateJournalSnapshotDto dto);
        Task RecalculateTopicSnapshotAsync(RecalculateTopicSnapshotDto dto);
        Task RecalculateAuthorSnapshotAsync(RecalculateAuthorSnapshotDto dto);
    }
}
