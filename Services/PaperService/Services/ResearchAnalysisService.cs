using Microsoft.EntityFrameworkCore;
using PaperService.Data;
using PaperService.DTOs;
using PaperService.Entities;
using System.Text.Json;

namespace PaperService.Services
{
    public interface IResearchAnalysisService
    {
        /// <summary>
        /// Luồng Hybrid: Tìm bài báo trong DB -> Nếu thiếu thì lấy từ OpenAlex -> Gọi AI phân tích
        /// </summary>
        Task<GapMatrixResponseDto> AnalyzeResearchIdeaAsync(
            string userIdea,
            List<Guid>? specificPaperIds,
            CancellationToken ct = default);
    }

    public class ResearchAnalysisService : IResearchAnalysisService
    {
        private readonly PaperDbContext _context;
        private readonly IAILiteratureService _aiService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<ResearchAnalysisService> _logger;

        private const int MIN_PAPERS_NEEDED = 3;
        private const int MAX_PAPERS_FOR_AI = 8;

        public ResearchAnalysisService(
            PaperDbContext context,
            IAILiteratureService aiService,
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<ResearchAnalysisService> logger)
        {
            _context = context;
            _aiService = aiService;
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        public async Task<GapMatrixResponseDto> AnalyzeResearchIdeaAsync(
            string userIdea,
            List<Guid>? specificPaperIds,
            CancellationToken ct = default)
        {
            List<Paper> papers;

            // --- Nhánh A: User tự chọn bài báo cụ thể (ID) ---
            if (specificPaperIds != null && specificPaperIds.Count >= MIN_PAPERS_NEEDED)
            {
                _logger.LogInformation("Using {Count} user-specified papers for analysis", specificPaperIds.Count);
                papers = await _context.Papers
                    .Where(p => specificPaperIds.Contains(p.Id))
                    .Take(MAX_PAPERS_FOR_AI)
                    .ToListAsync(ct);
            }
            else
            {
                // --- Nhánh B: Hybrid Search ---
                papers = await HybridSearchPapersAsync(userIdea, ct);
            }

            if (papers.Count == 0)
            {
                throw new InvalidOperationException(
                    "Không tìm được bài báo liên quan. Hãy thử lại với ý tưởng khác hoặc đồng bộ thêm dữ liệu.");
            }

            _logger.LogInformation("Sending {Count} papers to Gemini AI for analysis", papers.Count);

            // --- Gọi AI phân tích ---
            var aiResult = await _aiService.GenerateResearchGapMatrixAsync(papers, userIdea, ct);

            // --- Lưu kết quả vào DB ---
            var matrixEntity = new ResearchMatrix
            {
                Id = Guid.NewGuid(),
                UserIdeaPrompt = userIdea,
                MatrixDataJson = JsonSerializer.Serialize(aiResult),
                PaperIdsJson = JsonSerializer.Serialize(papers.Select(p => p.Id)),
                CreatedAt = DateTime.UtcNow
            };
            _context.ResearchMatrices.Add(matrixEntity);
            await _context.SaveChangesAsync(ct);

            // --- Trả về kết quả ---
            return new GapMatrixResponseDto
            {
                MatrixId = matrixEntity.Id,
                Cores = aiResult.Cores,
                Matrix = aiResult.Matrix.Select(m => new GapMatrixRowDto
                {
                    Paper = m.Paper,
                    Ticks = m.Ticks
                }).ToList(),
                Summary = aiResult.Summary,
                PapersAnalyzed = papers.Select(p => new PaperUsedDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Source = p.Source.ToString(),
                    HasFullText = !string.IsNullOrWhiteSpace(p.FullText),
                    PdfUrl = p.PdfUrl
                }).ToList(),
                CreatedAt = matrixEntity.CreatedAt
            };
        }

        // ─────────────────────────────────────────────────────────
        // HYBRID SEARCH: DB trước → thiếu thì gọi OpenAlex
        // ─────────────────────────────────────────────────────────
        private async Task<List<Paper>> HybridSearchPapersAsync(string userIdea, CancellationToken ct)
        {
            // Bước 1: Tách keyword từ ý tưởng của user
            var keywords = ExtractKeywords(userIdea);
            _logger.LogInformation("Extracted keywords: {Keywords}", string.Join(", ", keywords));

            // Bước 2: Tìm trong DB
            var dbPapers = await SearchInDatabaseAsync(keywords, ct);
            _logger.LogInformation("Found {Count} papers in DB", dbPapers.Count);

            if (dbPapers.Count >= MAX_PAPERS_FOR_AI)
            {
                // Nếu DB quá nhiều bài xuất sắc thì lấy 4 bài tốt nhất để dành chỗ cho OpenAlex
                dbPapers = dbPapers.Take(4).ToList();
            }

            // Bước 3: DB không đủ → gọi OpenAlex real-time
            _logger.LogInformation("Not enough papers in DB. Fetching from OpenAlex with query: {Query}", userIdea);
            var openAlexPapers = await FetchFromOpenAlexAsync(userIdea, ct);

            // Lưu bài mới vào DB (để lần sau dùng lại nhanh)
            var newPapers = new List<Paper>();
            foreach (var p in openAlexPapers)
            {
                var exists = await _context.Papers
                    .AnyAsync(x => x.ExternalId == p.ExternalId && x.Source == p.Source, ct);
                if (!exists)
                {
                    _context.Papers.Add(p);
                    newPapers.Add(p);
                }
            }
            if (newPapers.Count > 0)
            {
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Saved {Count} new papers from OpenAlex to DB", newPapers.Count);
            }

            // Gộp: bài trong DB + bài mới tải về
            var allPapers = dbPapers.Concat(newPapers).ToList();
            return allPapers.Take(MAX_PAPERS_FOR_AI).ToList();
        }

        // ─────────────────────────────────────────────────────────
        // TÌM KIẾM TRONG DATABASE
        // ─────────────────────────────────────────────────────────
        private async Task<List<Paper>> SearchInDatabaseAsync(List<string> keywords, CancellationToken ct)
        {
            if (keywords.Count == 0)
            {
                // Fallback: lấy bài báo mới nhất
                return await _context.Papers
                    .OrderByDescending(p => p.PublicationYear)
                    .Take(MAX_PAPERS_FOR_AI)
                    .ToListAsync(ct);
            }

            // Tìm bài nào có keyword trong Title hoặc Abstract
            var query = _context.Papers.AsQueryable();

            // EF Core: OR search qua các keywords
            // (tìm bài có chứa BẤT KỲ keyword nào)
            var matchingPapers = await _context.Papers
                .Where(p =>
                    keywords.Any(kw =>
                        p.Title.ToLower().Contains(kw) ||
                        (p.Abstract != null && p.Abstract.ToLower().Contains(kw))
                    )
                )
                .ToListAsync(ct);

            // Chấm điểm bài báo: Bài nào chứa nhiều keyword hơn thì điểm cao hơn
            var rankedPapers = matchingPapers
                .OrderByDescending(p => keywords.Count(kw => 
                    p.Title.ToLower().Contains(kw) || 
                    (p.Abstract != null && p.Abstract.ToLower().Contains(kw))))
                .ThenByDescending(p => p.CitationCount)
                .Take(MAX_PAPERS_FOR_AI)
                .ToList();

            return rankedPapers;
        }

        // ─────────────────────────────────────────────────────────
        // LẤY BÀI TỪ OPENALEX (Real-time)
        // ─────────────────────────────────────────────────────────
        private async Task<List<Paper>> FetchFromOpenAlexAsync(string userIdea, CancellationToken ct)
        {
            var papers = new List<Paper>();
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(20);

                // Dùng chính ý tưởng của user làm search query
                var encodedQuery = Uri.EscapeDataString(userIdea);
                var mailto = _config["OpenAlexMailto"] ?? "sonngocson25@gmail.com";
                var url = $"https://api.openalex.org/works?search={encodedQuery}&per_page=10&mailto={mailto}&filter=has_abstract:true";

                _logger.LogInformation("Calling OpenAlex: {Url}", url);
                var response = await client.GetAsync(url, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenAlex returned {Status}", response.StatusCode);
                    return papers;
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                    return papers;

                foreach (var work in results.EnumerateArray())
                {
                    try
                    {
                        var paper = ParseOpenAlexWork(work);
                        if (paper != null) papers.Add(paper);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse OpenAlex work");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching from OpenAlex");
            }

            return papers;
        }

        // ─────────────────────────────────────────────────────────
        // PARSE 1 BÀI BÁO TỪ OPENALEX JSON
        // ─────────────────────────────────────────────────────────
        private Paper? ParseOpenAlexWork(JsonElement work)
        {
            var externalId = work.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(externalId)) return null;

            var title = work.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "" : "";
            if (string.IsNullOrWhiteSpace(title)) return null;

            // Reconstruct abstract từ inverted index
            string? paperAbstract = null;
            if (work.TryGetProperty("abstract_inverted_index", out var abstractProp)
                && abstractProp.ValueKind == JsonValueKind.Object)
            {
                paperAbstract = ReconstructAbstract(abstractProp);
            }

            short? publicationYear = null;
            if (work.TryGetProperty("publication_year", out var pyProp) && pyProp.TryGetInt16(out short py))
                publicationYear = py;

            var doi = work.TryGetProperty("doi", out var doiProp) ? doiProp.GetString() : null;

            // Open Access PDF URL
            string? pdfUrl = null;
            if (work.TryGetProperty("open_access", out var oaProp) && oaProp.ValueKind == JsonValueKind.Object)
            {
                if (oaProp.TryGetProperty("is_oa", out var isOa) && isOa.GetBoolean())
                {
                    if (oaProp.TryGetProperty("oa_url", out var oaUrl))
                        pdfUrl = oaUrl.GetString();
                }
            }

            int citationCount = 0;
            if (work.TryGetProperty("cited_by_count", out var citeProp) && citeProp.TryGetInt32(out int cite))
                citationCount = cite;

            return new Paper
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Source = PaperSource.OpenAlex,
                Title = title,
                Abstract = paperAbstract,
                PublicationYear = publicationYear,
                Doi = doi,
                PdfUrl = pdfUrl,
                CitationCount = citationCount,
                RawData = JsonSerializer.Serialize(work),
                SyncedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        // ─────────────────────────────────────────────────────────
        // TRÍCH KEYWORD TỪ Ý TƯỞNG
        // ─────────────────────────────────────────────────────────
        private static List<string> ExtractKeywords(string userIdea)
        {
            // Danh sách từ bỏ qua (stopwords tiếng Việt + tiếng Anh phổ biến)
            var stopwords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "và", "hoặc", "của", "cho", "với", "trong", "trên", "về", "là", "có",
                "the", "a", "an", "and", "or", "in", "on", "of", "for", "to", "with",
                "that", "this", "is", "are", "was", "were", "be", "been", "being",
                "ứng", "dụng", "vào", "tôi", "muốn", "nghiên", "cứu", "hệ", "thống"
            };

            var keywords = userIdea
                .Split(new[] { ' ', ',', '.', ';', ':', '!', '?', '\n', '\t' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 3 && !stopwords.Contains(w))
                .Select(w => w.ToLowerInvariant())
                .Distinct()
                .Take(5) // Chỉ lấy 5 từ khóa quan trọng nhất
                .ToList();

            return keywords;
        }

        // ─────────────────────────────────────────────────────────
        // RECONSTRUCT ABSTRACT TỪ INVERTED INDEX (OpenAlex)
        // ─────────────────────────────────────────────────────────
        private static string ReconstructAbstract(JsonElement invertedIndex)
        {
            var dict = new SortedDictionary<int, string>();
            foreach (var property in invertedIndex.EnumerateObject())
            {
                var word = property.Name;
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var indexElement in property.Value.EnumerateArray())
                    {
                        if (indexElement.TryGetInt32(out int pos))
                            dict[pos] = word;
                    }
                }
            }
            return string.Join(" ", dict.Values);
        }
    }
}
