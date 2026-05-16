using TrendService.DTOs;

namespace TrendService.Services;

// Interface
public interface IPaperServiceClient
{
    Task<int> GetTotalAuthorsAsync();
    Task<List<TopAuthorDto>> GetTopAuthorsAsync(int top);
}
