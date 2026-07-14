using System;

namespace TrendService.Models;

public partial class AuthorTrendSnapshot
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = null!;
    public short Year { get; set; }
    public int PaperCount { get; set; }
    public int CitationSum { get; set; }
    public double? GrowthRate { get; set; }
    public DateTime RecordedAt { get; set; }
}
