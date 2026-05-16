using System;
using System.Collections.Generic;

namespace TrendService.Models;

public partial class TrendSnapshot
{
    public Guid Id { get; set; }

    public Guid KeywordId { get; set; }

    public string KeywordTerm { get; set; } = null!;

    public short Year { get; set; }

    public int PaperCount { get; set; }

    public int CitationSum { get; set; }

    public double? GrowthRate { get; set; }

    public DateTime RecordedAt { get; set; }
}
