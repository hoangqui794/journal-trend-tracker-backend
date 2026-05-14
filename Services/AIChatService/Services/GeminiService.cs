using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIChatService.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _model;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _apiKey = _configuration["Gemini:ApiKey"] ?? string.Empty;
            _model = _configuration["Gemini:Model"] ?? "gemini-1.5-flash";
        }

        public async Task<string> GenerateResponseAsync(string prompt, string context = "")
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey.Contains("YOUR_"))
            {
                return "AI: Xin lỗi, API Key chưa được cấu hình. Vui lòng sử dụng 'dotnet user-secrets set \"Gemini:ApiKey\" \"YOUR_KEY\"' để cài đặt locally.";
            }

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            var fullPrompt = prompt;
            if (!string.IsNullOrEmpty(context))
            {
                fullPrompt = $"Dựa trên nội dung tài liệu sau đây:\n{context}\n\nHãy trả lời câu hỏi của người dùng: {prompt}";
            }

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = fullPrompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            
            // Extract text from Gemini response structure
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "Không nhận được phản hồi từ AI.";
        }
    }
}
