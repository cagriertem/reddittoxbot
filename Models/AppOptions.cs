namespace RedditToXBot.Models;

public sealed class BotOptions
{
    public int PollIntervalSeconds { get; set; } = 30;
    public bool RunOnce { get; set; } = false;
    public bool DryRun { get; set; } = true;
    public int MaxPostsPerPoll { get; set; } = 5;
    public bool IncludeRedditLink { get; set; } = true;
    public bool UseAi { get; set; } = true;
    public string OpenAiModel { get; set; } = "gpt-5.2";
}

public sealed class RedditOptions
{
    public string Username { get; set; } = "";
}

public sealed class XOptions
{
    public string UserAccessToken { get; set; } = "";
}

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = "";
}
