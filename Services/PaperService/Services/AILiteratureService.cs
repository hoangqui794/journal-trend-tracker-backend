using System.Text;
using System.Text.Json;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Types;
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

        public async Task<string?> ExtractTextFromPdfFileAsync(string filePath, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Extracting text from local PDF file: {Path}", filePath);
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("File not found: {Path}", filePath);
                    return null;
                }

                var pdfBytes = await System.IO.File.ReadAllBytesAsync(filePath, ct);
                
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
                    _logger.LogWarning("PDF read but no text extracted (might be scanned/image PDF)");
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
                _logger.LogError(ex, "Error extracting text from PDF file: {Path}", filePath);
                return null;
            }
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

            _logger.LogInformation("Gemini API Key in use: {Prefix}...{Suffix} (Length: {Length})", 
                apiKey.Length > 4 ? apiKey.Substring(0, 4) : apiKey,
                apiKey.Length > 4 ? apiKey.Substring(apiKey.Length - 4) : string.Empty,
                apiKey.Length);

            var parts = new List<IPart>();

            // Xây dựng prompt text chỉ dẫn AI
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("You are an expert academic researcher specializing in literature review and research gap analysis.");
            promptBuilder.AppendLine($"I will provide you with the content of {papers.Count} research papers and my proposed research idea.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"**My Research Idea:** {userIdea}");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Below are the papers. If a paper has an attached PDF document, please read the PDF directly. If it fails to load or is not attached, its abstract or linear text content is provided below.");
            promptBuilder.AppendLine();

            for (int i = 0; i < papers.Count; i++)
            {
                var p = papers[i];
                promptBuilder.AppendLine($"--- Paper [{i + 1}]: \"{p.Title}\" ---");

                byte[]? pdfBytes = null;
                if (!string.IsNullOrWhiteSpace(p.PdfUrl))
                {
                    try
                    {
                        _logger.LogInformation("Downloading PDF for Gemini analysis: {Url}", p.PdfUrl);
                        var client = _httpClientFactory.CreateClient();
                        client.Timeout = TimeSpan.FromSeconds(30);
                        var response = await client.GetAsync(p.PdfUrl, ct);
                        if (response.IsSuccessStatusCode)
                        {
                            pdfBytes = await response.Content.ReadAsByteArrayAsync(ct);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to download PDF for analysis from {Url}. Status: {Status}", p.PdfUrl, response.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to download PDF from {Url} for Gemini analysis, falling back to abstract/text.", p.PdfUrl);
                    }
                }

                if (pdfBytes != null)
                {
                    promptBuilder.AppendLine($"[Attached PDF file: Paper_{i + 1}.pdf]");
                    parts.Add(new InlineData
                    {
                        MimeType = "application/pdf",
                        Data = Convert.ToBase64String(pdfBytes),
                        DisplayName = $"Paper_{i + 1}.pdf"
                    });
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(p.FullText))
                    {
                        var text = p.FullText.Length > 8000 ? p.FullText.Substring(0, 8000) : p.FullText;
                        promptBuilder.AppendLine(text);
                    }
                    else if (!string.IsNullOrWhiteSpace(p.Abstract))
                    {
                        promptBuilder.AppendLine($"Abstract: {p.Abstract}");
                    }
                    else
                    {
                        promptBuilder.AppendLine("(No abstract or PDF file available)");
                    }
                }
                promptBuilder.AppendLine();
            }

            promptBuilder.AppendLine(@"**Your Task:**
1. Identify 5-8 core methodologies, features, or research dimensions (called ""Core"") that are discussed across these papers (using the attached PDFs and text).
2. Create a comparison matrix showing which paper addresses which Core dimension.
3. Identify research gaps - dimensions that NO paper fully addresses.
4. Evaluate my proposed idea against these dimensions.

**IMPORTANT: Return ONLY valid JSON in this exact format, no markdown, no explanation:**
{
  ""cores"": [""Core 1 description"", ""Core 2 description"", ""Core 3 description""],
  ""matrix"": [
    {""paper"": ""Short title of Paper [1]"", ""ticks"": [true, false, true]},
    {""paper"": ""Short title of Paper [2]"", ""ticks"": [false, true, false]},
    {""paper"": ""My Proposed Idea"", ""ticks"": [true, true, true]}
  ],
  ""summary"": ""A brief 2-3 sentence summary of the key research gaps identified and how the proposed idea addresses them.""
}");

            parts.Add(new Part(promptBuilder.ToString()));

            try
            {
                _logger.LogInformation("Sending request to Gemini AI for research gap analysis...");
                
                var googleAi = new GoogleAI(apiKey);
                var model = googleAi.GenerativeModel(model: "gemini-flash-latest");
                
                var response = await model.GenerateContent(parts);
                var responseText = response?.Text ?? string.Empty;
                
                _logger.LogInformation("Gemini response received. Length: {Length}", responseText.Length);

                // Parse JSON response từ AI
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
        public async Task<DTOs.DeepAnalysisResultDto> DeepAnalyzePaperAsync(string fullText, CancellationToken ct = default)
        {
            var apiKey = _config["GeminiApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("API key is not configured.");
                throw new InvalidOperationException("AI Service is not configured properly.");
            }

            var prompt = $@"You are an expert academic researcher. Please perform a deep analysis of the following research paper text.
Extract and summarize the information into 4 distinct sections. Return your analysis STRICTLY in valid JSON format using the following structure:
{{
  ""summary"": ""A high-level summary of the paper's core objective and main contribution."",
  ""methodology"": ""A detailed description of the methods, models, datasets, or experiments used in the paper."",
  ""findings"": ""The core results, findings, and conclusions of the paper."",
  ""limitations"": ""Any limitations, future work, or gaps identified by the authors.""
}}

Ensure that the output is ONLY JSON and contains no markdown formatting outside of the JSON structure.
Here is the text of the paper:
---------------------------
{fullText}";

            try
            {
                _logger.LogInformation("Sending request to Gemini AI for deep paper analysis...");
                
                var googleAi = new GoogleAI(apiKey);
                var model = googleAi.GenerativeModel(model: "gemini-flash-latest");
                
                var response = await model.GenerateContent(prompt);
                var responseText = response?.Text ?? string.Empty;
                
                _logger.LogInformation("Gemini deep analysis response received.");

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

                var result = JsonSerializer.Deserialize<DTOs.DeepAnalysisResultDto>(responseText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new DTOs.DeepAnalysisResultDto 
                { 
                    Summary = "AI returned an empty response. Please try again." 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini AI for deep analysis");
                return new DTOs.DeepAnalysisResultDto
                {
                    Summary = $"AI analysis failed: {ex.Message}"
                };
            }
        }
    }
}
