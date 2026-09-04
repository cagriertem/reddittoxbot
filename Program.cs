using Microsoft.Extensions.Options;
using RedditToXBot;
using RedditToXBot.Models;
using RedditToXBot.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<BotOptions>(builder.Configuration.GetSection("Bot"));
builder.Services.AddSingleton(builder.Configuration.GetSection("Reddit").Get<RedditOptions>() ?? new());
builder.Services.AddSingleton(builder.Configuration.GetSection("X").Get<XOptions>() ?? new());
builder.Services.AddSingleton(builder.Configuration.GetSection("OpenAI").Get<OpenAiOptions>() ?? new());

builder.Services.AddHttpClient();
builder.Services.AddSingleton<RedditClient>();
builder.Services.AddSingleton<XClient>();
builder.Services.AddSingleton<AiThreadWriter>();
builder.Services.AddSingleton<ThreadSplitter>();
builder.Services.AddSingleton<StateStore>();
builder.Services.AddHostedService<Worker>();

var botOptions = builder.Configuration.GetSection("Bot").Get<BotOptions>() ?? new();
var openAiOptions = builder.Configuration.GetSection("OpenAI").Get<OpenAiOptions>() ?? new();
if (botOptions.UseAi && !string.IsNullOrWhiteSpace(openAiOptions.ApiKey))
    builder.Services.AddSingleton<AiThreadWriter>();

builder.Services.AddHostedService<Worker>();
await builder.Build().RunAsync();
