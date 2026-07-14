using System;

namespace TrendService.Models;

public partial class TopicTrendSnapshot
{
    public Guid Id { get; set; }
    public Guid TopicId { get; set; }
    public string TopicName { get; set; } = null!;
    public short Year { get; set; }
    public int PaperCount { get; set; }
    public int CitationSum { get; set; }
    public double? GrowthRate { get; set; }
    public DateTime RecordedAt { get; set; }
}
