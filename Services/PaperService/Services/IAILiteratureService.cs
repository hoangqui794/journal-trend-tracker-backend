using PaperService.Entities;

namespace PaperService.Services
{
    public interface IAILiteratureService
    {
        /// <summary>
        /// Trích xuất text từ file PDF tại URL cho trước
        /// </summary>
        Task<string?> ExtractTextFromPdfUrlAsync(string pdfUrl, CancellationToken ct = default);
        
        /// <summary>
        /// Trích xuất text từ file PDF từ đường dẫn vật lý
        /// </summary>
        Task<string?> ExtractTextFromPdfFileAsync(string filePath, CancellationToken ct = default);

        /// <summary>
        /// Tạo ma trận Research Gap từ danh sách bài báo và ý tưởng người dùng
        /// </summary>
        Task<ResearchGapResultDto> GenerateResearchGapMatrixAsync(
            List<Paper> papers,
            string userIdea,
            CancellationToken ct = default);

        /// <summary>
        /// Phân tích chuyên sâu một bài báo từ nội dung text của nó
        /// </summary>
        Task<DTOs.DeepAnalysisResultDto> DeepAnalyzePaperAsync(string fullText, CancellationToken ct = default);
    }

    public class ResearchGapResultDto
    {
        public List<string> Cores { get; set; } = new();
        public List<MatrixRowDto> Matrix { get; set; } = new();
        public string? Summary { get; set; }
    }

    public class MatrixRowDto
    {
        public string Paper { get; set; } = string.Empty;
        public List<bool> Ticks { get; set; } = new();
    }
}
