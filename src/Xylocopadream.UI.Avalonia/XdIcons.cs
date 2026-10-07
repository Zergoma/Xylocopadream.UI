using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Xylocopadream.UI.Avalonia;

/// <summary>A line icon of Themes/Icons.axaml: use it as <c>{StaticResource Xd.Icon.&lt;Name&gt;}</c>.</summary>
public sealed record XdIcon(string Name, string Key, Geometry Geometry);

/// <summary>Every icon of the library, for galleries, pickers and tests.</summary>
public static class XdIcons
{
    public const string KeyPrefix = "Xd.Icon.";

    private static readonly Lazy<IReadOnlyList<XdIcon>> AllIcons = new(Load);

    public static IReadOnlyList<XdIcon> All => AllIcons.Value;

    private static IReadOnlyList<XdIcon> Load()
    {
        var icons = (ResourceDictionary)AvaloniaXamlLoader.Load(new Uri("avares://Xylocopadream.UI.Avalonia/Themes/Icons.axaml"));

        // Compiled dictionaries hold deferred values: TryGetResource builds them, enumerating does not.
        return icons.Keys
            .OfType<string>()
            .Where(key => key.StartsWith(KeyPrefix, StringComparison.Ordinal))
            .Select(key => icons.TryGetResource(key, null, out var value) && value is Geometry geometry
                ? new XdIcon(key[KeyPrefix.Length..], key, geometry)
                : null)
            .OfType<XdIcon>()
            .OrderBy(icon => icon.Name, StringComparer.Ordinal)
            .ToList();
    }
}
