using System.Text;
using System.Text.RegularExpressions;

namespace RedditToXBot.Services;

public sealed class ThreadSplitter
{
    // Conservative limit. X's character counting is more nuanced than simple .Length;
    // keeping chunks <= 230 leaves room for numbering and avoids edge cases.
    private const int MaxChunk = 230;

    public IReadOnlyList<string> Split(string text)
    {
        text = Regex.Replace(text.Replace("\r\n", "\n"), @"\n{3,}", "\n\n").Trim();
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();

        var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var chunks = new List<string>();
        var current = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (paragraph.Length <= MaxChunk)
            {
                if (current.Length == 0) current.Append(paragraph);
                else if (current.Length + 2 + paragraph.Length <= MaxChunk) current.Append("\n\n").Append(paragraph);
                else { chunks.Add(current.ToString()); current.Clear(); current.Append(paragraph); }
                continue;
            }

            if (current.Length > 0) { chunks.Add(current.ToString()); current.Clear(); }
            foreach (var piece in SplitLongParagraph(paragraph)) chunks.Add(piece);
        }
        if (current.Length > 0) chunks.Add(current.ToString());
        return chunks;
    }

    private static IEnumerable<string> SplitLongParagraph(string paragraph)
    {
        var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();
        foreach (var word in words)
        {
            if (word.Length > MaxChunk)
            {
                if (current.Length > 0) { yield return current.ToString(); current.Clear(); }
                for (var i = 0; i < word.Length; i += MaxChunk)
                    yield return word.Substring(i, Math.Min(MaxChunk, word.Length - i));
                continue;
            }
            if (current.Length == 0) current.Append(word);
            else if (current.Length + 1 + word.Length <= MaxChunk) current.Append(' ').Append(word);
            else { yield return current.ToString(); current.Clear(); current.Append(word); }
        }
        if (current.Length > 0) yield return current.ToString();
    }
}
