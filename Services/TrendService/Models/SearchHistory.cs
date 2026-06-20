using System;
using System.Collections.Generic;

namespace TrendService.Models;

public partial class SearchHistory
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string Query { get; set; } = null!;

    public string? SearchType { get; set; }

    public int? ResultCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
