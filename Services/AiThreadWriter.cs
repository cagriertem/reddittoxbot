#pragma warning disable OPENAI001
using System.Text.Json;
using OpenAI.Responses;
using RedditToXBot.Models;

namespace RedditToXBot.Services;

public sealed class AiThreadWriter
{
    private readonly ResponsesClient _client;
    private readonly string _model;
    private readonly ILogger<AiThreadWriter> _logger;

    public AiThreadWriter(OpenAiOptions options, BotOptions bot, ILogger<AiThreadWriter> logger)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey)) throw new InvalidOperationException("OpenAI:ApiKey is missing.");
        _client = new ResponsesClient(options.ApiKey);
        _model = bot.OpenAiModel;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> CreateThreadAsync(RedditPost post, CancellationToken ct)
    {
        var prompt = $"""
You convert a Reddit post into a concise X/Twitter thread.
Rules:
- Preserve the original meaning and factual claims. Do not invent facts.
- Keep the original language of the post.
- Make the thread readable and natural, not a mechanical word split.
- Return ONLY valid JSON: {"tweets":["...","..."]}
- Each tweet must be <= 220 characters before numbering.
- The first tweet should hook the reader and include the post title when useful.
- Do not add hashtags unless they already appear in the source.
- Do not add commentary about being an AI.

TITLE:
{post.Title}

BODY:
{post.Body}
""";

        var result = await _client.CreateResponseAsync(_model, prompt, cancellationToken: ct);
        var raw = result.Value.GetOutputText().Trim();
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var arr = doc.RootElement.GetProperty("tweets");
            var tweets = arr.EnumerateArray().Select(x => x.GetString() ?? "")
                .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            if (tweets.Count == 0) throw new InvalidOperationException("AI returned no tweets.");
            if (tweets.Any(x => x.Length > 240)) throw new InvalidOperationException("AI returned an oversized tweet.");
            return tweets;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "AI output was invalid; falling back to deterministic splitting.");
            return Array.Empty<string>();
        }
    }
}
