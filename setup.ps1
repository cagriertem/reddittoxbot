# Local setup helper for RedditToXBot.
# Fill in your values before running this script.

$env:Reddit__Username = "boslukstrikesagain"
$env:X__UserAccessToken = "YOUR_X_USER_ACCESS_TOKEN"
$env:OpenAI__ApiKey = "YOUR_OPENAI_API_KEY"
$env:Bot__RunOnce = "true"
$env:Bot__DryRun = "true"

dotnet restore
dotnet run
