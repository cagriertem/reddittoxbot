using Microsoft.Extensions.Options;
using RedditToXBot.Models;
using RedditToXBot.Services;

namespace RedditToXBot;

public sealed class Worker : BackgroundService
{
    private readonly RedditClient _reddit;
    private readonly XClient _x;
    private readonly AiThreadWriter? _ai;
    private readonly ThreadSplitter _splitter;
    private readonly StateStore _state;
    private readonly BotOptions _options;
    private readonly ILogger<Worker> _logger;

    public Worker(RedditClient reddit, XClient x, AiThreadWriter? ai, ThreadSplitter splitter,
        StateStore state, IOptions<BotOptions> options, ILogger<Worker> logger)
    {
        _reddit = reddit; _x = x; _ai = ai; _splitter = splitter; _state = state;
        _options = options.Value; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "RedditToXBot started. DryRun={DryRun}, RunOnce={RunOnce}",
            _options.DryRun, _options.RunOnce);

        if (_options.RunOnce)
        {
            try { await PollAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { _logger.LogError(ex, "Run failed."); }
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, _options.PollIntervalSeconds)));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await PollAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { _logger.LogError(ex, "Polling cycle failed."); }
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        var posts = await _reddit.GetLatestPostsAsync(_options.MaxPostsPerPoll, ct);
        foreach (var post in posts.OrderBy(p => p.CreatedUtc))
        {
            if (await _state.HasProcessedAsync(post.Id)) continue;
            if (string.IsNullOrWhiteSpace(post.Body) && string.IsNullOrWhiteSpace(post.Title))
            {
                await _state.MarkProcessedAsync(post.Id);
                continue;
            }

            _logger.LogInformation("New Reddit post detected: {Id} r/{Subreddit} {Title}", post.Id, post.Subreddit, post.Title);
            var tweets = await BuildThreadAsync(post, ct);
            if (tweets.Count == 0) { _logger.LogWarning("No tweets generated for {Id}", post.Id); continue; }

            if (_options.DryRun)
            {
                _logger.LogInformation("DRY RUN: would publish {Count} posts for Reddit {Id}:\n{Thread}", tweets.Count, string.Join("\n---\n", tweets));
                await _state.MarkProcessedAsync(post.Id);
                continue;
            }

            var ids = new List<string>();
            string? previous = null;
            for (var i = 0; i < tweets.Count; i++)
            {
                var numbered = tweets.Count > 1 ? $"{i + 1}/{tweets.Count} {tweets[i]}" : tweets[i];
                var id = await _x.CreatePostAsync(numbered, previous, ct);
                ids.Add(id);
                previous = id;
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }

            await _state.MarkProcessedAsync(post.Id);
            _logger.LogInformation("Published Reddit {RedditId} as X thread: {Ids}", post.Id, string.Join(",", ids));
        }
    }

    private async Task<IReadOnlyList<string>> BuildThreadAsync(RedditPost post, CancellationToken ct)
    {
        if (_options.UseAi && _ai != null)
        {
            var aiTweets = await _ai.CreateThreadAsync(post, ct);
            if (aiTweets.Count > 0) return aiTweets;
        }

        var source = string.IsNullOrWhiteSpace(post.Body) ? post.Title : $"{post.Title}\n\n{post.Body}";
        var chunks = _splitter.Split(source).ToList();
        if (_options.IncludeRedditLink && !string.IsNullOrWhiteSpace(post.Permalink) && chunks.Count > 0)
            chunks[^1] = $"{chunks[^1]}\n\nhttps://www.reddit.com{post.Permalink}";
        return chunks;
    }
}
