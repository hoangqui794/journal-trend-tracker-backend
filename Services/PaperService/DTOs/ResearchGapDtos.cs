namespace PaperService.DTOs
{
    /// <summary>
    /// Request body để tạo ma trận Research Gap - chỉ cần truyền ý tưởng lên
    /// </summary>
    public class GenerateGapMatrixRequestDto
    {
        /// <summary>
        /// Ý tưởng nghiên cứu của người dùng (bắt buộc)
        /// Hệ thống sẽ tự động tìm bài báo liên quan trong DB hoặc từ OpenAlex
        /// </summary>
        public string UserIdea { get; set; } = string.Empty;

        /// <summary>
        /// (Tuỳ chọn) Nếu muốn chỉ định cụ thể bài báo nào cần phân tích
        /// Nếu để trống, hệ thống sẽ tự động tìm
        /// </summary>
        public List<Guid>? PaperIds { get; set; }
    }

    /// <summary>
    /// Response trả về cho Frontend chứa ma trận so sánh
    /// </summary>
    public class GapMatrixResponseDto
    {
        public Guid MatrixId { get; set; }
        public List<string> Cores { get; set; } = new();
        public List<GapMatrixRowDto> Matrix { get; set; } = new();
        public string? Summary { get; set; }
        public List<PaperUsedDto> PapersAnalyzed { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class GapMatrixRowDto
    {
        public string Paper { get; set; } = string.Empty;
        public List<bool> Ticks { get; set; } = new();
    }

    /// <summary>
    /// Thông tin bài báo đã được dùng để phân tích (để Flutter hiển thị cho user biết)
    /// </summary>
    public class PaperUsedDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public bool HasFullText { get; set; }
        public string? PdfUrl { get; set; }
    }
}
