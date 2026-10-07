using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Xylocopadream.UI.Avalonia;

/// <summary>
/// A color per name, always the same for the same name (case-insensitive): tags, project badges, avatars...
/// The palette is readable with white text on the dark theme.
/// </summary>
public static class NamedColors
{
    public static IReadOnlyList<IBrush> Palette { get; } =
    [
        Brush("#4F7DF3"), // blue
        Brush("#D9578A"), // pink
        Brush("#2F9E6E"), // green
        Brush("#D9822B"), // orange
        Brush("#8460D6"), // violet
        Brush("#1F9BB0"), // teal
        Brush("#D0503C"), // red
        Brush("#6B7C8F"), // slate
    ];

    public static IBrush For(string name) => Palette[StableHash(name) % Palette.Count];

    private static IBrush Brush(string color) => new ImmutableSolidColorBrush(Color.Parse(color));

    /// <summary>FNV-1a on the lower-cased name: stable across runs, unlike <see cref="string.GetHashCode()"/>.</summary>
    private static int StableHash(string text)
    {
        var hash = 2166136261u;
        foreach (var c in text.ToLowerInvariant())
        {
            hash = (hash ^ c) * 16777619u;
        }

        return (int)(hash & int.MaxValue);
    }
}
