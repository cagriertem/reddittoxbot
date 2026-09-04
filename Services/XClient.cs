using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RedditToXBot.Services;

public sealed class XClient
{
    private readonly HttpClient _http;
    private readonly string _token;

    public XClient(HttpClient http, XOptions options)
    {
        _http = http;
        _token = options.UserAccessToken;
        _http.BaseAddress = new Uri("https://api.x.com/");
    }

    public async Task<string> CreatePostAsync(string text, string? replyToId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_token)) throw new InvalidOperationException("X:UserAccessToken is missing.");
        var payload = new Dictionary<string, object?> { ["text"] = text };
        if (!string.IsNullOrWhiteSpace(replyToId))
            payload["reply"] = new Dictionary<string, string> { ["in_reply_to_tweet_id"] = replyToId };

        using var request = new HttpRequestMessage(HttpMethod.Post, "2/tweets");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"X API {(int)response.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetString()!;
    }
}
