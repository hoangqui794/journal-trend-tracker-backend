using ClosedXML.Excel;
using TrendService.Repositories;

namespace TrendService.Services;

public class ExportService : IExportService
{
    private readonly ITrendRepository _repo;

    public ExportService(ITrendRepository repo)
    {
        _repo = repo;
    }

    public async Task<byte[]> ExportKeywordTrendToExcelAsync(Guid keywordId)
    {
        var data = await _repo.GetKeywordTrendAsync(keywordId);

        if (data == null || !data.Any())
            throw new KeyNotFoundException("Không tìm thấy dữ liệu xu hướng cho từ khóa này.");

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Báo cáo xu hướng");

        // Header
        ws.Cell(1, 1).Value = "Năm xuất bản";
        ws.Cell(1, 2).Value = "Số lượng bài báo";
        ws.Cell(1, 3).Value = "Tổng số trích dẫn";
        ws.Cell(1, 4).Value = "Tỷ lệ tăng trưởng (%)";

        var headerRow = ws.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
        headerRow.Style.Font.FontColor = XLColor.White;
        headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Data rows
        for (int i = 0; i < data.Count; i++)
        {
            int row = i + 2;
            ws.Cell(row, 1).Value = data[i].Year;
            ws.Cell(row, 2).Value = data[i].PaperCount;
            ws.Cell(row, 3).Value = data[i].CitationSum;
            ws.Cell(row, 4).Value = data[i].GrowthRate.HasValue
                ? Math.Round(data[i].GrowthRate!.Value, 2)
                : 0;

            // Zebra striping
            if (i % 2 == 0)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF4FB");
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
