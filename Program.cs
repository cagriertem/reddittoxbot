using Microsoft.Extensions.Options;
using RedditToXBot;
using RedditToXBot.Models;
using RedditToXBot.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<BotOptions>(
    builder.Configuration.GetSection("Bot"));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<BotOptions>>().Value);

builder.Services.AddSingleton(
    builder.Configuration.GetSection("Reddit").Get<RedditOptions>() ?? new RedditOptions());

builder.Services.AddSingleton(
    builder.Configuration.GetSection("X").Get<XOptions>() ?? new XOptions());

builder.Services.AddSingleton(
    builder.Configuration.GetSection("OpenAI").Get<OpenAiOptions>() ?? new OpenAiOptions());

builder.Services.AddHttpClient();

builder.Services.AddSingleton<RedditClient>();
builder.Services.AddSingleton<XClient>();
builder.Services.AddSingleton<AiThreadWriter>();
builder.Services.AddSingleton<ThreadSplitter>();
builder.Services.AddSingleton<StateStore>();

builder.Services.AddHostedService<Worker>();

await builder.Build().RunAsync();