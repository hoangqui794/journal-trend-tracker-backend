using Microsoft.AspNetCore.Mvc;
using PaperService.Clients;
using PaperService.DTOs;
using PaperService.Services;

namespace PaperService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PapersController : ControllerBase
    {
        private readonly IPaperService _paperService;
        private readonly ITrendServiceClient _trendServiceClient;

        public PapersController(IPaperService paperService, ITrendServiceClient trendServiceClient)
        {
            _paperService = paperService;
            _trendServiceClient = trendServiceClient;
        }

        /// <summary>
        /// Tìm kiếm và lọc danh sách bài báo
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<PaperSummaryDto>>> SearchPapers([FromQuery] PaperFilterDto filter)
        {
            var result = await _paperService.SearchPapersAsync(filter);
            
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                // Fire and forget call to TrendService
                _ = Task.Run(async () =>
                {
                    await _trendServiceClient.LogSearchHistoryAsync(new SearchHistoryLogDto
                    {
                        UserId = null, // TODO: Extract from HttpContext.User when Authentication is configured
                        Query = filter.Keyword,
                        SearchType = "keyword",
                        ResultCount = result.TotalCount
                    });
                });
            }

            return Ok(result);
        }

        /// <summary>
        /// Xem chi tiết một bài báo
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PaperDetailDto>> GetPaper(Guid id)
        {
            var paper = await _paperService.GetPaperByIdAsync(id);
            if (paper == null)
            {
                return NotFound(new { message = $"Paper with id {id} not found." });
            }

            return Ok(paper);
        }

        /// <summary>
        /// Lấy danh sách từ khóa gợi ý
        /// </summary>
        [HttpGet("keywords")]
        public async Task<ActionResult<IEnumerable<KeywordSuggestionDto>>> GetKeywords([FromQuery] string? query, [FromQuery] int limit = 20)
        {
            var keywords = await _paperService.GetKeywordSuggestionsAsync(query, limit);
            return Ok(keywords);
        }
    }
}
