using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Xylocopadream.UI.Avalonia.Controls;

namespace Xylocopadream.UI.Avalonia;

/// <summary>
/// The main (accent) color of the app: selections, focused fields, links, toggles, the theme switcher's pill...
/// <see cref="Apply"/> writes its variants for both themes in the application resources (looked up before the
/// Xylocopadream theme), at once; the app saves <see cref="Current"/> to apply it again at the next start.
/// Same idea as <c>XdTheme.SetAccent</c> of Xylocopadream.UI.Maui.
/// </summary>
public static class XdAccent
{
    /// <summary>Rider's blue, the color of the theme.</summary>
    public static Color Default { get; } = Color.Parse("#3574F0");

    /// <summary>Colors proposed first (the same as XyloType, after Rider's blue).</summary>
    public static IReadOnlyList<ColorPreset> Presets { get; } =
    [
        new("Bleu Rider", Default),
        new("Bleu", Color.Parse("#2F6FEB")),
        new("Indigo", Color.Parse("#4F46E5")),
        new("Violet", Color.Parse("#7C3AED")),
        new("Pourpre", Color.Parse("#A21CAF")),
        new("Rose", Color.Parse("#DB2777")),
        new("Rouge", Color.Parse("#DC2626")),
        new("Orange", Color.Parse("#EA580C")),
        new("Ambre", Color.Parse("#D97706")),
        new("Vert", Color.Parse("#16A34A")),
        new("Émeraude", Color.Parse("#059669")),
        new("Sarcelle", Color.Parse("#0D9488")),
        new("Cyan", Color.Parse("#0891B2")),
    ];

    public static Color Current { get; private set; } = Default;

    /// <summary>Raised after <see cref="Apply"/>, e.g. to save the color.</summary>
    public static event EventHandler? Changed;

    /// <summary>Uses <paramref name="accent"/> everywhere in the app, for the dark and the light theme.</summary>
    public static void Apply(Color accent)
    {
        if (global::Avalonia.Application.Current is not { } app)
        {
            return;
        }

        accent = Color.FromRgb(accent.R, accent.G, accent.B);
        var resources = app.Resources;
        resources["SystemAccentColor"] = accent;
        resources["SystemAccentColorDark1"] = Mix(accent, Colors.Black, 0.12);
        resources["SystemAccentColorDark2"] = Mix(accent, Colors.Black, 0.24);
        resources["SystemAccentColorLight1"] = Mix(accent, Colors.White, 0.12);
        resources["SystemAccentColorLight2"] = Mix(accent, Colors.White, 0.24);
        resources.ThemeDictionaries[ThemeVariant.Dark] = Variants(accent, dark: true);
        resources.ThemeDictionaries[ThemeVariant.Light] = Variants(accent, dark: false);
        Current = accent;
        Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>Back to <see cref="Default"/>.</summary>
    public static void Reset() => Apply(Default);

    private static ResourceDictionary Variants(Color accent, bool dark)
    {
        // Lighter on the dark theme, so it stands out as much as on the light one.
        var main = dark ? Mix(accent, Colors.White, 0.06) : accent;
        var selected = dark ? Mix(accent, Color.Parse("#2B2D30"), 0.62) : Mix(accent, Colors.White, 0.8);
        var brushes = new Dictionary<string, Color>
        {
            ["Xd.Accent"] = main,
            ["Xd.AccentHover"] = dark ? Mix(main, Colors.White, 0.1) : Mix(main, Colors.Black, 0.1),
            ["Xd.Selected"] = selected,
            ["Xd.DropTarget"] = Color.FromArgb(dark ? (byte)0x59 : (byte)0x40, accent.R, accent.G, accent.B),
            ["Xd.Link"] = dark ? Mix(accent, Colors.White, 0.22) : Mix(accent, Colors.Black, 0.1),
            ["Xd.LinkHover"] = dark ? Mix(accent, Colors.White, 0.32) : accent,
            ["Xd.IconOnSelected"] = dark ? Colors.White : accent,
            ["TextControlBorderBrushFocused"] = main,
            ["TreeViewItemBackgroundSelected"] = selected,
            ["TreeViewItemBackgroundSelectedPointerOver"] = selected,
            ["TreeViewItemBackgroundSelectedPressed"] = selected,
            ["MenuFlyoutItemBackgroundPointerOver"] = selected,
            ["MenuFlyoutItemBackgroundPressed"] = selected,
        };

        var dictionary = new ResourceDictionary();
        foreach (var (key, color) in brushes)
        {
            dictionary[key] = new SolidColorBrush(color);
        }

        return dictionary;
    }

    /// <summary><paramref name="color"/> moved by <paramref name="amount"/> (0 to 1) toward <paramref name="other"/>.</summary>
    private static Color Mix(Color color, Color other, double amount) => Color.FromRgb(
        (byte)Math.Round(color.R + ((other.R - color.R) * amount)),
        (byte)Math.Round(color.G + ((other.G - color.G) * amount)),
        (byte)Math.Round(color.B + ((other.B - color.B) * amount)));
}
