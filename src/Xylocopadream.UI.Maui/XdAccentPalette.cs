namespace Xylocopadream.UI.Maui;

/// <summary>
/// The variants of an accent color (hover, pressed, soft selection, tinted surfaces, text on it...),
/// adapted to the theme: lighter colors on the dark theme.
/// </summary>
public static class XdAccentPalette
{
    private static readonly Color s_darkForeground = Color.FromArgb("#1F2937");

    /// <summary>
    /// Every accent resource, by <see cref="XdKeys"/>, for the theme of the palette.
    /// </summary>
    public static IReadOnlyDictionary<string, Color> Compute(Color accent, XdPalette palette, bool dark)
    {
        // text and icons on the accent color: white, or dark on a light color (yellow...)
        Color main = dark ? WithLuminosity(accent, Math.Min(accent.GetLuminosity() + 0.12, 0.8)) : accent;

        Dictionary<string, Color> resources = new()
        {
            [XdKeys.AccentForeground] = IsLight(main) ? s_darkForeground : Colors.White,
        };

        if (dark)
        {
            resources[XdKeys.Accent] = main;
            resources[XdKeys.AccentHover] = WithLuminosity(main, Math.Min(main.GetLuminosity() + 0.08, 0.88));
            resources[XdKeys.AccentPressed] = accent;
            resources[XdKeys.AccentSoft] = Blend(accent, palette.CardBackground, 0.28);
            resources[XdKeys.AccentSoftHover] = Blend(accent, palette.CardBackground, 0.36);
            resources[XdKeys.AccentText] = WithLuminosity(main, Math.Min(main.GetLuminosity() + 0.06, 0.85));
            resources[XdKeys.AccentGradientEnd] = WithLuminosity(ShiftHue(accent, 40), Math.Min(main.GetLuminosity(), 0.75));
            resources[XdKeys.AccentSurface] = Blend(accent, palette.CardBackground, 0.12);
            resources[XdKeys.AccentOnSurface] = WithLuminosity(accent, 0.86);
            resources[XdKeys.AccentPageBackground] = Blend(accent, palette.PageBackground, 0.04);
            resources[XdKeys.AccentTile] = Blend(accent, palette.TileBackground, 0.08);
            resources[XdKeys.AccentTileHover] = Blend(accent, palette.TileBackground, 0.14);
            resources[XdKeys.AccentTilePressed] = Blend(accent, palette.TileBackground, 0.2);
            resources[XdKeys.AccentTileStroke] = Blend(accent, palette.TileStroke, 0.1);
        }
        else
        {
            resources[XdKeys.Accent] = accent;
            resources[XdKeys.AccentHover] = WithLuminosity(accent, Math.Max(accent.GetLuminosity() - 0.07, 0.1));
            resources[XdKeys.AccentPressed] = WithLuminosity(accent, Math.Max(accent.GetLuminosity() - 0.13, 0.08));
            resources[XdKeys.AccentSoft] = Blend(accent, palette.CardBackground, 0.13);
            resources[XdKeys.AccentSoftHover] = Blend(accent, palette.CardBackground, 0.2);
            resources[XdKeys.AccentText] = accent;
            resources[XdKeys.AccentGradientEnd] = ShiftHue(accent, 40);
            resources[XdKeys.AccentSurface] = Blend(accent, palette.CardBackground, 0.09);
            resources[XdKeys.AccentOnSurface] = WithLuminosity(accent, 0.3);
            resources[XdKeys.AccentPageBackground] = Blend(accent, palette.PageBackground, 0.045);
            resources[XdKeys.AccentTile] = Blend(accent, palette.TileBackground, 0.05);
            resources[XdKeys.AccentTileHover] = Blend(accent, palette.TileBackground, 0.1);
            resources[XdKeys.AccentTilePressed] = Blend(accent, palette.TileBackground, 0.15);
            resources[XdKeys.AccentTileStroke] = Blend(accent, palette.TileStroke, 0.08);
        }

        foreach (var (key, foregroundKey, degrees) in s_alternates)
        {
            Color alternate = Alternate(accent, degrees, dark);
            resources[key] = alternate;
            resources[foregroundKey] = IsLight(alternate) ? s_darkForeground : Colors.White;
        }

        return resources;
    }

    private static readonly (string Key, string ForegroundKey, double Degrees)[] s_alternates =
    [
        (XdKeys.AccentAlternate1, XdKeys.AccentAlternate1Foreground, 45),
        (XdKeys.AccentAlternate2, XdKeys.AccentAlternate2Foreground, -45),
    ];

    /// <summary>
    /// The accent with its hue turned, well saturated, then darkened until a white text reads on it
    /// (a yellow or a cyan is much brighter than a blue at the same luminosity); a bit lighter on the dark theme.
    /// </summary>
    private static Color Alternate(Color accent, double degrees, bool dark)
    {
        double saturation = Math.Max(accent.GetSaturation(), 0.55);
        double luminosity = dark ? 0.6 : 0.47;
        double maxLuminance = dark ? 0.25 : 0.2;

        Color color = Color.FromHsla(TurnHue(accent.GetHue(), degrees), saturation, luminosity);
        while (RelativeLuminance(color) > maxLuminance && luminosity > 0.2)
        {
            luminosity -= 0.02;
            color = Color.FromHsla(TurnHue(accent.GetHue(), degrees), saturation, luminosity);
        }

        return color;
    }

    /// <summary>
    /// Perceived brightness (WCAG relative luminance): dark text reads better above 0.45.
    /// </summary>
    public static bool IsLight(Color color)
        => RelativeLuminance(color) > 0.45;

    /// <summary>
    /// WCAG relative luminance, from 0 (black) to 1 (white).
    /// </summary>
    public static double RelativeLuminance(Color color)
    {
        static double Linear(float channel)
            => channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);

        return 0.2126 * Linear(color.Red) + 0.7152 * Linear(color.Green) + 0.0722 * Linear(color.Blue);
    }

    // hue of a Maui color goes from 0 to 1, turned by degrees (either way)
    private static double TurnHue(double hue, double degrees)
        => ((hue + degrees / 360) % 1 + 1) % 1;

    private static Color WithLuminosity(Color color, double luminosity)
        => Color.FromHsla(color.GetHue(), color.GetSaturation(), Math.Clamp(luminosity, 0, 1));

    private static Color ShiftHue(Color color, double degrees)
        => Color.FromHsla(TurnHue(color.GetHue(), degrees), color.GetSaturation(), color.GetLuminosity());

    /// <summary>
    /// The color laid over the surface with the given opacity.
    /// </summary>
    private static Color Blend(Color color, Color surface, double amount)
        => new(
            (float)(surface.Red + (color.Red - surface.Red) * amount),
            (float)(surface.Green + (color.Green - surface.Green) * amount),
            (float)(surface.Blue + (color.Blue - surface.Blue) * amount));
}
