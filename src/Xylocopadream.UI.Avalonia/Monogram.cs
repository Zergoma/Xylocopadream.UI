namespace Xylocopadream.UI.Avalonia;

/// <summary>Two-letter badges, as on Rider's project icons.</summary>
public static class Monogram
{
    /// <summary>"Mon Coffre" → "MC", "MonCoffre" → "MC", "photos" → "PH", "" → "?".</summary>
    public static string Of(string name)
    {
        var words = name.Split([' ', '-', '_', '.'], StringSplitOptions.RemoveEmptyEntries);
        var initials = words.Length switch
        {
            0 => "?",
            1 => words[0][..Math.Min(2, words[0].Length)],
            _ => string.Concat(words.Take(2).Select(w => w[0])),
        };

        // A single camel-cased word gives its capitals.
        if (words.Length == 1 && words[0].Count(char.IsUpper) >= 2)
        {
            initials = string.Concat(words[0].Where(char.IsUpper).Take(2));
        }

        return initials.ToUpperInvariant();
    }

    /// <summary>Last segment of a path ("C:\Vaults\Photos" → "Photos"); the text itself when it is not a path.</summary>
    public static string NameOfPath(string path) =>
        Path.GetFileName(path.TrimEnd('\\', '/')) is { Length: > 0 } name ? name : path;
}
