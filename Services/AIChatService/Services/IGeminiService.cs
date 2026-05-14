namespace AIChatService.Services
{
    public interface IGeminiService
    {
        Task<string> GenerateResponseAsync(string prompt, string context = "");
    }
}
