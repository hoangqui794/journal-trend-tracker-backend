using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaperService.Clients;
using PaperService.Data;
using PaperService.DTOs;
using PaperService.Entities;
using PaperService.Services;
using System.Text.Json;

namespace PaperService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PapersController : ControllerBase
    {
        private readonly IPaperService _paperService;
        private readonly ITrendServiceClient _trendServiceClient;
        private readonly IResearchAnalysisService _researchAnalysisService;
        private readonly PaperDbContext _context;

        public PapersController(
            IPaperService paperService, 
            ITrendServiceClient trendServiceClient,
            IResearchAnalysisService researchAnalysisService,
            PaperDbContext context)
        {
            _paperService = paperService;
            _trendServiceClient = trendServiceClient;
            _researchAnalysisService = researchAnalysisService;
            _context = context;
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
                _ = Task.Run(async () =>
                {
                    await _trendServiceClient.LogSearchHistoryAsync(new SearchHistoryLogDto
                    {
                        UserId = null,
                        Query = filter.Keyword,
                        SearchType = "keyword",
                        ResultCount = result.TotalCount
                    });
                });
            }
            else if (filter.AuthorId.HasValue)
            {
                _ = Task.Run(async () =>
                {
                    await _trendServiceClient.LogSearchHistoryAsync(new SearchHistoryLogDto
                    {
                        UserId = null,
                        Query = filter.AuthorId.Value.ToString(),
                        SearchType = "author",
                        ResultCount = result.TotalCount
                    });
                });
            }
            else if (filter.JournalId.HasValue)
            {
                _ = Task.Run(async () =>
                {
                    await _trendServiceClient.LogSearchHistoryAsync(new SearchHistoryLogDto
                    {
                        UserId = null,
                        Query = filter.JournalId.Value.ToString(),
                        SearchType = "journal",
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

        /// <summary>
        /// [HYBRID] Phân tích Research Gap: Chỉ cần truyền ý tưởng lên.
        /// Hệ thống tự tìm bài báo (DB → OpenAlex) → trích xuất nội dung → gọi AI phân tích.
        /// </summary>
        [HttpPost("generate-gap-matrix")]
        public async Task<ActionResult<GapMatrixResponseDto>> GenerateGapMatrix(
            [FromBody] GenerateGapMatrixRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.UserIdea))
                return BadRequest(new { message = "Vui lòng nhập ý tưởng nghiên cứu của bạn." });

            if (request.UserIdea.Length < 10)
                return BadRequest(new { message = "Ý tưởng quá ngắn, hãy mô tả chi tiết hơn (ít nhất 10 ký tự)." });

            try
            {
                var result = await _researchAnalysisService.AnalyzeResearchIdeaAsync(
                    request.UserIdea,
                    request.PaperIds);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi khi phân tích: {ex.Message}" });
            }
        }

        /// <summary>
        /// Upload file PDF cho một bài báo (dành cho bài báo bị paywall không tải tự động được)
        /// </summary>
        [HttpPost("{id}/upload-pdf")]
        public async Task<IActionResult> UploadPdf(Guid id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded." });
            }

            if (!file.ContentType.Contains("pdf"))
            {
                return BadRequest(new { message = "Only PDF files are accepted." });
            }

            var paper = await _context.Papers.FindAsync(id);
            if (paper == null)
            {
                return NotFound(new { message = $"Paper with id {id} not found." });
            }

            // Lưu file vào wwwroot/pdfs
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdfs");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{id}_{Guid.NewGuid():N}.pdf";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Trích xuất text từ file đã lưu
            var pdfUrl = $"{Request.Scheme}://{Request.Host}/pdfs/{fileName}";
            var aiService = HttpContext.RequestServices.GetRequiredService<IAILiteratureService>();
            var fullText = await aiService.ExtractTextFromPdfUrlAsync(pdfUrl);

            paper.PdfUrl = pdfUrl;
            paper.FullText = fullText;
            paper.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                message = "PDF uploaded and text extracted successfully.",
                hasFullText = !string.IsNullOrWhiteSpace(fullText),
                textLength = fullText?.Length ?? 0
            });
        }
        /// <summary>
        /// Upload file PDF và phân tích chuyên sâu nội dung bằng AI
        /// </summary>
        [HttpPost("{id}/deep-analyze")]
        public async Task<IActionResult> DeepAnalyzePdf(Guid id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded." });
            }

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) && !file.ContentType.Contains("pdf"))
            {
                return BadRequest(new { message = "Only PDF files are accepted." });
            }

            var paper = await _context.Papers.FindAsync(id);
            if (paper == null)
            {
                return NotFound(new { message = $"Paper with id {id} not found." });
            }

            // Lưu file vào wwwroot/pdfs
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdfs");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{id}_{Guid.NewGuid():N}.pdf";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Trích xuất text từ file đã lưu
            var pdfUrl = $"{Request.Scheme}://{Request.Host}/pdfs/{fileName}";
            var aiService = HttpContext.RequestServices.GetRequiredService<IAILiteratureService>();
            var fullText = await aiService.ExtractTextFromPdfFileAsync(filePath);

            // Cập nhật Database
            paper.PdfUrl = pdfUrl;
            paper.FullText = fullText;
            paper.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (string.IsNullOrWhiteSpace(fullText))
            {
                return BadRequest(new { message = "Could not extract text from PDF." });
            }

            // Gọi AI để phân tích chuyên sâu
            var analysisResult = await aiService.DeepAnalyzePaperAsync(fullText);

            return Ok(analysisResult);
        }
    }
}
