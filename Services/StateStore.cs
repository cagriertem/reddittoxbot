using System.Text.Json;

namespace RedditToXBot.Services;

public sealed class StateStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private HashSet<string>? _processed;

    public StateStore()
    {
        _path = Path.Combine(AppContext.BaseDirectory, "data", "processed.json");
    }

    public async Task<bool> HasProcessedAsync(string redditId)
    {
        await LoadAsync();
        await _gate.WaitAsync();
        try { return _processed!.Contains(redditId); }
        finally { _gate.Release(); }
    }

    public async Task MarkProcessedAsync(string redditId)
    {
        await LoadAsync();
        await _gate.WaitAsync();
        try
        {
            _processed!.Add(redditId);
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            await File.WriteAllTextAsync(_path, JsonSerializer.Serialize(_processed));
        }
        finally { _gate.Release(); }
    }

    private async Task LoadAsync()
    {
        if (_processed != null) return;
        await _gate.WaitAsync();
        try
        {
            if (_processed != null) return;
            if (File.Exists(_path))
            {
                var json = await File.ReadAllTextAsync(_path);
                _processed = JsonSerializer.Deserialize<HashSet<string>>(json) ?? new();
            }
            else _processed = new();
        }
        finally { _gate.Release(); }
    }
}
