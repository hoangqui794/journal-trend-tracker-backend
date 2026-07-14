using PaperService.DTOs;

namespace PaperService.Clients
{
    public interface IAdminServiceClient
    {
        Task<IEnumerable<ApiSourceDto>> GetApiSourcesAsync();
    }
}
