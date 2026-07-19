using System.Text;
using System.Text.Json;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Mscc.GenerativeAI;
using PaperService.Entities;

namespace PaperService.Services
{
    public class AILiteratureService : IAILiteratureService
    {
        private readonly ILogger<AILiteratureService> _logger;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public AILiteratureService(
            ILogger<AILiteratureService> logger,
            IConfiguration config,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Tải file PDF từ URL, trích xuất toàn bộ text bên trong bằng iText7
        /// </summary>
        public async Task<string?> ExtractTextFromPdfUrlAsync(string pdfUrl, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Downloading PDF from: {Url}", pdfUrl);
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                
                var response = await client.GetAsync(pdfUrl, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to download PDF. Status: {Status}", response.StatusCode);
                    return null;
                }

                var pdfBytes = await response.Content.ReadAsByteArrayAsync(ct);
                
                // Dùng iText7 để đọc text từ PDF bytes
                using var memoryStream = new MemoryStream(pdfBytes);
                using var pdfReader = new PdfReader(memoryStream);
                using var pdfDocument = new PdfDocument(pdfReader);
                
                var sb = new StringBuilder();
                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                {
                    var page = pdfDocument.GetPage(i);
                    var strategy = new SimpleTextExtractionStrategy();
                    var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                    sb.AppendLine(pageText);
                }

                var fullText = sb.ToString().Trim();
                
                if (string.IsNullOrWhiteSpace(fullText))
                {
                    _logger.LogWarning("PDF downloaded but no text extracted (might be scanned/image PDF)");
                    return null;
                }

                // Giới hạn text để tránh quá lớn (max ~50,000 ký tự = ~12,500 tokens)
                if (fullText.Length > 50000)
                {
                    fullText = fullText.Substring(0, 50000);
                }

                _logger.LogInformation("Successfully extracted {Length} characters from PDF", fullText.Length);
                return fullText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from PDF: {Url}", pdfUrl);
                return null;
            }
        }

        /// <summary>
        /// Gọi Google Gemini AI để phân tích Research Gaps từ nội dung các bài báo
        /// </summary>
        public async Task<ResearchGapResultDto> GenerateResearchGapMatrixAsync(
            List<Paper> papers,
            string userIdea,
            CancellationToken ct = default)
        {
            var apiKey = _config["GeminiApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("GeminiApiKey is not configured. Add it to .env or appsettings.json");
            }

            // Xây dựng nội dung các bài báo để gửi cho AI
            var papersContent = new StringBuilder();
            for (int i = 0; i < papers.Count; i++)
            {
                var p = papers[i];
                papersContent.AppendLine($"--- Paper [{i + 1}]: \"{p.Title}\" ---");
                
                if (!string.IsNullOrWhiteSpace(p.FullText))
                {
                    // Giới hạn mỗi bài tối đa 8000 ký tự để không vượt quá context window
                    var text = p.FullText.Length > 8000 ? p.FullText.Substring(0, 8000) : p.FullText;
                    papersContent.AppendLine(text);
                }
                else if (!string.IsNullOrWhiteSpace(p.Abstract))
                {
                    papersContent.AppendLine($"Abstract: {p.Abstract}");
                }
                else
                {
                    papersContent.AppendLine("(No content available)");
                }
                papersContent.AppendLine();
            }

            // Prompt Engineering - Câu lệnh gửi cho AI
            var prompt = $@"You are an expert academic researcher specializing in literature review and research gap analysis.

I will provide you with the content of {papers.Count} research papers and my proposed research idea.

**My Research Idea:** {userIdea}

**Papers Content:**
{papersContent}

**Your Task:**
1. Identify 5-8 core methodologies, features, or research dimensions (called ""Core"") that are discussed across these papers.
2. Create a comparison matrix showing which paper addresses which Core dimension.
3. Identify research gaps - dimensions that NO paper fully addresses.
4. Evaluate my proposed idea against these dimensions.

**IMPORTANT: Return ONLY valid JSON in this exact format, no markdown, no explanation:**
{{
  ""cores"": [""Core 1 description"", ""Core 2 description"", ""Core 3 description""],
  ""matrix"": [
    {{""paper"": ""Short title of Paper [1]"", ""ticks"": [true, false, true]}},
    {{""paper"": ""Short title of Paper [2]"", ""ticks"": [false, true, false]}},
    {{""paper"": ""My Proposed Idea"", ""ticks"": [true, true, true]}}
  ],
  ""summary"": ""A brief 2-3 sentence summary of the key research gaps identified and how the proposed idea addresses them.""
}}";

            try
            {
                _logger.LogInformation("Sending request to Gemini AI for research gap analysis...");
                
                var googleAi = new GoogleAI(apiKey);
                var model = googleAi.GenerativeModel(model: "gemini-flash-latest");
                
                var response = await model.GenerateContent(prompt);
                var responseText = response?.Text ?? string.Empty;
                
                _logger.LogInformation("Gemini response received. Length: {Length}", responseText.Length);

                // Parse JSON response từ AI
                // Loại bỏ markdown code block nếu AI trả về có bọc ```json ... ```
                responseText = responseText.Trim();
                if (responseText.StartsWith("```json"))
                {
                    responseText = responseText.Substring(7);
                }
                if (responseText.StartsWith("```"))
                {
                    responseText = responseText.Substring(3);
                }
                if (responseText.EndsWith("```"))
                {
                    responseText = responseText.Substring(0, responseText.Length - 3);
                }
                responseText = responseText.Trim();

                var result = JsonSerializer.Deserialize<ResearchGapResultDto>(responseText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new ResearchGapResultDto 
                { 
                    Summary = "AI returned an empty response. Please try again." 
                };
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Failed to parse AI response as JSON");
                return new ResearchGapResultDto
                {
                    Summary = $"AI response could not be parsed. Raw response was logged for debugging."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini AI");
                throw;
            }
        }
    }
}
