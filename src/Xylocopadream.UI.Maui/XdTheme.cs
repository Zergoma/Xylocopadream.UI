namespace Xylocopadream.UI.Maui;

/// <summary>
/// Puts the colors of the controls (<see cref="XdKeys"/>) in the application resources, for the current theme
/// and accent color, and writes them again at each switch between light and dark.
/// The controls use them as dynamic resources: call <see cref="Apply"/> before the first page is shown
/// (the controls also apply it themselves when the app did not).
/// </summary>
public static class XdTheme
{
    private static Microsoft.Maui.Controls.Application? s_app;

    // RequestedThemeChanged holds its handlers weakly: kept here, or the garbage collector removes it
    private static EventHandler<AppThemeChangedEventArgs>? s_onThemeChanged;

    public static Color DefaultAccent { get; } = Color.FromArgb("#2F6FEB");

    /// <summary>
    /// The accent color: selections, toggles, primary buttons.
    /// </summary>
    public static Color Accent { get; private set; } = DefaultAccent;

    public static XdPalette Light { get; set; } = XdPalette.DefaultLight;

    public static XdPalette Dark { get; set; } = XdPalette.DefaultDark;

    /// <summary>
    /// Writes the colors in the resources of the app, then again at each change of theme.
    /// </summary>
    public static void Apply(Microsoft.Maui.Controls.Application app, Color? accent = null)
    {
        if (accent is not null)
            Accent = accent;

        if (s_app != app)
        {
            if (s_app is not null && s_onThemeChanged is not null)
                s_app.RequestedThemeChanged -= s_onThemeChanged;

            s_app = app;
            s_onThemeChanged = (_, _) => Write(app);
            app.RequestedThemeChanged += s_onThemeChanged;
        }

        Write(app);
    }

    /// <summary>
    /// Changes the accent color of the whole app at once.
    /// </summary>
    public static void SetAccent(Color accent)
    {
        Accent = accent;
        if ((s_app ?? Microsoft.Maui.Controls.Application.Current) is { } app)
            Apply(app);
    }

    /// <summary>
    /// Every resource for a theme: the palette, then the accent variants.
    /// </summary>
    public static IReadOnlyDictionary<string, Color> Resources(bool dark, Color accent)
    {
        XdPalette palette = dark ? Dark : Light;
        Dictionary<string, Color> resources = new(palette.ToResources());
        foreach (var (key, color) in XdAccentPalette.Compute(accent, palette, dark))
            resources[key] = color;

        return resources;
    }

    internal static void EnsureApplied()
    {
        if (s_app is null && Microsoft.Maui.Controls.Application.Current is { } app)
            Apply(app);
    }

    private static void Write(Microsoft.Maui.Controls.Application app)
    {
        foreach (var (key, color) in Resources(app.RequestedTheme == AppTheme.Dark, Accent))
            app.Resources[key] = color;
    }
}
