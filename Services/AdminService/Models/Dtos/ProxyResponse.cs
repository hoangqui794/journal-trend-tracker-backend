using System.Net;

namespace AdminService.Models.Dtos;

public sealed record ProxyResponse(HttpStatusCode StatusCode, string Content, string ContentType = "application/json")
{
    public static async Task<ProxyResponse> FromHttpResponseAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? "application/json";
        return new ProxyResponse(response.StatusCode, content, mediaType);
    }
}
