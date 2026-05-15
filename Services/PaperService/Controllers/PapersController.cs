using Microsoft.AspNetCore.Mvc;
using PaperService.DTOs;
using PaperService.Services;

namespace PaperService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PapersController : ControllerBase
    {
        private readonly IPaperService _paperService;

        public PapersController(IPaperService paperService)
        {
            _paperService = paperService;
        }

        /// <summary>
        /// Tìm kiếm và lọc danh sách bài báo
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<PaperSummaryDto>>> SearchPapers([FromQuery] PaperFilterDto filter)
        {
            var result = await _paperService.SearchPapersAsync(filter);
            
            // TODO: Gọi logic lưu lịch sử tìm kiếm (search-history) ở đây
            // Có thể publish event qua RabbitMQ hoặc gọi thẳng hàm nội bộ để ghi nhận lịch sử

            return Ok(result);
        }
    }
}
