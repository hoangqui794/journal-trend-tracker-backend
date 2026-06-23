using System.Threading;
using System.Threading.Tasks;

namespace PaperService.Services
{
    public interface ISyncJobService
    {
        Task DoSyncWorkAsync(CancellationToken stoppingToken);
        Task WipeMockDataAsync(CancellationToken stoppingToken);
    }
}
