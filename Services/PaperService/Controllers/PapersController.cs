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
        /// Lưu lịch sử tìm kiếm của user
        /// </summary>
        [HttpPost("search-history")]
        public async Task<IActionResult> LogSearchHistory([FromBody] SearchHistoryLogDto dto)
        {
            await _trendServiceClient.LogSearchHistoryAsync(dto);
            return Ok(new { message = "Search history logged successfully." });
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

        /// <summary>
        /// Lấy danh sách tạp chí gợi ý
        /// </summary>
        [HttpGet("journals")]
        public async Task<ActionResult<IEnumerable<JournalSuggestionDto>>> GetJournals([FromQuery] string? query, [FromQuery] int limit = 20)
        {
            var journals = await _paperService.GetJournalSuggestionsAsync(query, limit);
            return Ok(journals);
        }

        /// <summary>
        /// Lấy danh sách tác giả gợi ý
        /// </summary>
        [HttpGet("authors")]
        public async Task<ActionResult<IEnumerable<AuthorSuggestionDto>>> GetAuthors([FromQuery] string? query, [FromQuery] int limit = 20)
        {
            var authors = await _paperService.GetAuthorSuggestionsAsync(query, limit);
            return Ok(authors);
        }

        /// <summary>
        /// Lấy danh sách lịch sử đồng bộ chạy ngầm (Dành cho AdminService gọi)
        /// </summary>
        [HttpGet("sync-jobs")]
        public async Task<ActionResult<IEnumerable<ApiSyncJobDto>>> GetSyncJobs([FromQuery] int limit = 50)
        {
            var jobs = await _paperService.GetSyncJobsAsync(limit);
            return Ok(jobs);
        }

        [HttpGet("authors/count")]
        public async Task<ActionResult<AuthorCountDto>> GetAuthorsCount()
        {
            var total = await _paperService.GetAuthorsCountAsync();
            return Ok(new AuthorCountDto { Total = total });
        }

        [HttpGet("authors/top")]
        public async Task<ActionResult<IEnumerable<AuthorTopDto>>> GetTopAuthors([FromQuery] int top = 10)
        {
            var authors = await _paperService.GetTopAuthorsAsync(top);
            return Ok(authors);
        }
    }
}
