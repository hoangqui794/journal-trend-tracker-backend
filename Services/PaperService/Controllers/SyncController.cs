using Microsoft.AspNetCore.Mvc;
using PaperService.Services;
using System.Threading.Tasks;
using System.Threading;

namespace PaperService.Controllers
{
    [ApiController]
    [Route("api/papers/[controller]")]
    public class SyncController : ControllerBase
    {
        private readonly ISyncJobService _syncJobService;

        public SyncController(ISyncJobService syncJobService)
        {
            _syncJobService = syncJobService;
        }

        [HttpPost("trigger")]
        public async Task<IActionResult> TriggerSync(CancellationToken cancellationToken)
        {
            // Run async job in background, do not await it so API returns immediately
            _ = Task.Run(() => _syncJobService.DoSyncWorkAsync(CancellationToken.None));
            return Accepted(new { message = "Sync job has been triggered and is running in the background." });
        }

        [HttpDelete("wipe")]
        public async Task<IActionResult> WipeMockData(CancellationToken cancellationToken)
        {
            await _syncJobService.WipeMockDataAsync(cancellationToken);
            return Ok(new { message = "All mock data wiped successfully. Sync cursors have been reset." });
        }
    }
}
