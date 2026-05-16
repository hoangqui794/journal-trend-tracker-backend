using System;
using System.Collections.Generic;

namespace TrendService.Models;

public partial class ReportCache
{
    public Guid Id { get; set; }

    public string ReportType { get; set; } = null!;

    public string ParamsHash { get; set; } = null!;

    public string ResultJson { get; set; } = null!;

    public DateTime GeneratedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}
