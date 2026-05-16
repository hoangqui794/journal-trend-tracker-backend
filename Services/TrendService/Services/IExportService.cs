namespace TrendService.Services;

public interface IExportService
{
    Task<byte[]> ExportKeywordTrendToExcelAsync(Guid keywordId);
}
