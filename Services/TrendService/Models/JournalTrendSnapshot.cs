using System;
using System.Collections.Generic;

namespace TrendService.Models;

public partial class JournalTrendSnapshot
{
    public Guid Id { get; set; }

    public Guid JournalId { get; set; }

    public string JournalName { get; set; } = null!;

    public short Year { get; set; }

    public int PaperCount { get; set; }

    public int CitationSum { get; set; }

    public double? GrowthRate { get; set; }

    public DateTime RecordedAt { get; set; }
}
