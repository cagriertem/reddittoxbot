using System.Text.Json;
using RedditToXBot.Models;

namespace RedditToXBot.Services;

/// <summary>
/// Reads public Reddit posts through Reddit's public JSON endpoint.
/// No Reddit OAuth token is used by this client.
/// </summary>
public sealed class RedditClient
{
    private readonly HttpClient _http;
    private readonly RedditOptions _options;
    private readonly ILogger<RedditClient> _logger;

    public RedditClient(HttpClient http, RedditOptions options, ILogger<RedditClient> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
        _http.BaseAddress = new Uri("https://www.reddit.com/");
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("RedditToXBot/1.0 (public-post-reader)");
    }

    public async Task<IReadOnlyList<RedditPost>> GetLatestPostsAsync(int limit, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.Username))
            throw new InvalidOperationException("Reddit:Username is required.");

        var url = $"user/{Uri.EscapeDataString(_options.Username)}/submitted.json?raw_json=1&limit={Math.Clamp(limit, 1, 100)}";
        _logger.LogInformation("Reading public Reddit JSON for u/{Username}", _options.Username);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await _http.SendAsync(request, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Reddit public JSON {(int)response.StatusCode}: {text}");

        using var doc = JsonDocument.Parse(text);
        var children = doc.RootElement.GetProperty("data").GetProperty("children");
        var posts = new List<RedditPost>();

        foreach (var child in children.EnumerateArray())
        {
            var d = child.GetProperty("data");
            var id = d.GetProperty("id").GetString() ?? "";
            var title = d.GetProperty("title").GetString() ?? "";
            var body = d.TryGetProperty("selftext", out var selftext) ? selftext.GetString() ?? "" : "";
            var permalink = d.GetProperty("permalink").GetString() ?? "";
            var subreddit = d.GetProperty("subreddit").GetString() ?? "";
            var author = d.TryGetProperty("author", out var a) ? a.GetString() ?? "[deleted]" : "[deleted]";
            var created = d.GetProperty("created_utc").GetDouble();
            var urlValue = d.TryGetProperty("url", out var u) ? u.GetString() : null;
            var isSelf = d.TryGetProperty("is_self", out var s) && s.GetBoolean();

            posts.Add(new RedditPost(
                id, title, body, permalink, subreddit, author,
                DateTimeOffset.FromUnixTimeSeconds((long)created),
                urlValue, isSelf));
        }

        _logger.LogInformation("Reddit returned {Count} public posts for u/{Username}", posts.Count, _options.Username);
        return posts;
    }
}
