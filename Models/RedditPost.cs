namespace RedditToXBot.Models;

public sealed record RedditPost(
    string Id,
    string Title,
    string Body,
    string Permalink,
    string Subreddit,
    string Author,
    DateTimeOffset CreatedUtc,
    string? Url,
    bool IsSelf);
