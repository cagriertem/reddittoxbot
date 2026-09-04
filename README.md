# RedditToXBot

A .NET 8 bot that checks public posts from a specified Reddit user, optionally turns them into an X thread with OpenAI, and publishes the thread to X.

## Current Reddit method

The bot currently reads the public Reddit JSON endpoint:

`https://www.reddit.com/user/{username}/submitted.json`

It does not use a Reddit OAuth access token. Public JSON access can be rate-limited or restricted by Reddit, so treat this as a test/initial implementation rather than a guarantee of permanent API access.

## GitHub Actions

The repository includes `.github/workflows/reddit-to-x.yml`.

It runs approximately every 5 minutes and starts the bot in `RunOnce` mode. After the run, it commits `data/processed.json` so a new runner does not forget which Reddit posts were already processed.

### Required GitHub Secrets

Repository → Settings → Secrets and variables → Actions:

- `REDDIT_USERNAME` — `boslukstrikesagain`
- `OPENAI_API_KEY` — your OpenAI API key
- `X_USER_ACCESS_TOKEN` — your X user access token

### First test

Keep this line in the workflow:

`Bot__DryRun: "true"`

The bot will generate/log the thread but will not publish to X.

When everything looks correct, change it to:

`Bot__DryRun: "false"`

### Local run

Set the values in `appsettings.json` or environment variables, then:

```powershell
dotnet restore
dotnet run
```

For a single local run:

```powershell
$env:Bot__RunOnce = "true"
dotnet run
```

## Important state behavior

The bot stores processed Reddit IDs in `data/processed.json`. GitHub Actions commits this file after each run. If you test with `DryRun=true`, those posts are still marked as processed. To test the same post again, remove its ID from `data/processed.json` and commit the change.
