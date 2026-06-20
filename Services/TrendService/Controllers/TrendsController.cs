using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TrendService.DTOs;
using TrendService.Services;

namespace TrendService.Controllers;

[ApiController]
[Route("api/trends")]
public class TrendsController : ControllerBase
{
    private readonly IAnalyticsService _service;
    private readonly IExportService _exportService;
    private readonly ILogger<TrendsController> _logger;
    private readonly IConfiguration _configuration;

    public TrendsController(
        IAnalyticsService service,
        IExportService exportService,
        ILogger<TrendsController> logger,
        IConfiguration configuration)
    {
        _service = service;
        _exportService = exportService;
        _logger = logger;
        _configuration = configuration;
    }

    private bool IsInternalRequestValid()
    {
        var expectedSecret = _configuration["InternalSecret"] ?? "default_internal_secret_key_123";
        if (Request.Headers.TryGetValue("X-Internal-Secret", out var providedSecret))
        {
            return providedSecret == expectedSecret;
        }
        return false;
    }

    // GET /api/trends/overview
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var result = await _service.GetOverviewAsync();
        return Ok(result);
    }

    // GET /api/trends/keywords/{id}
    [HttpGet("keywords/{id:guid}")]
    public async Task<IActionResult> GetKeywordTrend(Guid id)
    {
        var result = await _service.GetKeywordTrendAsync(id);
        if (result == null)
            return NotFound("Không tìm thấy dữ liệu xu hướng cho từ khóa này.");
        return Ok(result);
    }

    // GET /api/trends/journals/{id}
    [HttpGet("journals/{id:guid}")]
    public async Task<IActionResult> GetJournalTrend(Guid id)
    {
        var result = await _service.GetJournalTrendAsync(id);
        if (result == null)
            return NotFound("Không tìm thấy dữ liệu xu hướng cho tạp chí này.");
        return Ok(result);
    }

    // GET /api/trends/top-journals?top=10
    [HttpGet("top-journals")]
    public async Task<IActionResult> GetTopJournals([FromQuery] int top = 10)
    {
        if (top <= 0 || top > 100)
            return BadRequest("top phải từ 1 đến 100.");
        var result = await _service.GetTopJournalsAsync(top);
        return Ok(result);
    }

    // GET /api/trends/top-keywords?top=10
    [HttpGet("top-keywords")]
    public async Task<IActionResult> GetTopKeywords([FromQuery] int top = 10)
    {
        if (top <= 0 || top > 100)
            return BadRequest("top phải từ 1 đến 100.");
        var result = await _service.GetTopKeywordsAsync(top);
        return Ok(result);
    }

    // GET /api/trends/top-authors?top=10
    [HttpGet("top-authors")]
    public async Task<IActionResult> GetTopAuthors([FromQuery] int top = 10)
    {
        if (top <= 0 || top > 100)
            return BadRequest("top phải từ 1 đến 100.");
        var result = await _service.GetTopAuthorsAsync(top);
        return Ok(result);
    }

    // GET /api/trends/hot-topics?top=5
    [HttpGet("hot-topics")]
    public async Task<IActionResult> GetHotTopics([FromQuery] int top = 5)
    {
        if (top <= 0 || top > 50)
            return BadRequest("top phải từ 1 đến 50.");
        var result = await _service.GetHotTopicsAsync(top);
        return Ok(result);
    }

    // GET /api/trends/reports/export?keywordId=xxx
    [HttpGet("reports/export")]
    [Authorize]
    public async Task<IActionResult> ExportExcel([FromQuery] Guid keywordId)
    {
        if (keywordId == Guid.Empty)
            return BadRequest("keywordId không hợp lệ.");
        try
        {
            var fileContent = await _exportService.ExportKeywordTrendToExcelAsync(keywordId);
            string fileName = $"BaoCaoXuHuong_{DateTime.Now:yyyyMMddHHmm}.xlsx";
            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(fileContent, contentType, fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    // ── Internal endpoints (gọi từ PaperService) ─────────────

    // POST /api/trends/search-history
    // PaperService gọi sau mỗi lần user search
    [HttpPost("search-history")]
    public async Task<IActionResult> LogSearchHistory([FromBody] SearchHistoryLogDto dto)
    {
        if (!IsInternalRequestValid())
            return StatusCode(403, "Forbidden: Invalid Internal Secret");

        if (string.IsNullOrWhiteSpace(dto.Query))
            return BadRequest("Query không được để trống.");
        await _service.LogSearchHistoryAsync(dto);
        return Ok();
    }

    // POST /api/trends/snapshots/recalculate
    // PaperService gọi sau khi sync bài mới về
    [HttpPost("snapshots/recalculate")]
    public async Task<IActionResult> RecalculateSnapshot([FromBody] RecalculateSnapshotDto dto)
    {
        if (!IsInternalRequestValid())
            return StatusCode(403, "Forbidden: Invalid Internal Secret");

        if (dto.KeywordId == Guid.Empty)
            return BadRequest("KeywordId không hợp lệ.");
        await _service.RecalculateSnapshotAsync(dto);
        return Ok();
    }
}
