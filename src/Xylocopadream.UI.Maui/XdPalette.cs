namespace Xylocopadream.UI.Maui;

/// <summary>
/// Colors of one theme (light or dark). The last four are the surfaces the accent color is blended with
/// (see <see cref="XdAccentPalette"/>). Replace <see cref="XdTheme.Light"/> or <see cref="XdTheme.Dark"/>
/// (e.g. with <c>with { ... }</c>) to adapt them to an app.
/// </summary>
public sealed record XdPalette(
    Color PanelBackground,
    Color PopupBackground,
    Color Border,
    Color Hover,
    Color Text,
    Color TextSecondary,
    Color Danger,
    Color NeutralButton,
    Color NeutralButtonHover,
    Color NeutralButtonPressed,
    Color CardBackground,
    Color PageBackground,
    Color TileBackground,
    Color TileStroke)
{
    public static XdPalette DefaultLight { get; } = new(
        PanelBackground: Color.FromArgb("#FFFFFF"),
        PopupBackground: Color.FromArgb("#FFFFFF"),
        Border: Color.FromArgb("#D8DEE8"),
        Hover: Color.FromArgb("#240F172A"),
        Text: Color.FromArgb("#111827"),
        TextSecondary: Color.FromArgb("#6B7280"),
        Danger: Color.FromArgb("#DC2626"),
        NeutralButton: Color.FromArgb("#334155"),
        NeutralButtonHover: Color.FromArgb("#475569"),
        NeutralButtonPressed: Color.FromArgb("#64748B"),
        CardBackground: Color.FromArgb("#FFFFFF"),
        PageBackground: Color.FromArgb("#F4F5F7"),
        TileBackground: Color.FromArgb("#F6F7F9"),
        TileStroke: Color.FromArgb("#DADDE3"));

    public static XdPalette DefaultDark { get; } = new(
        PanelBackground: Color.FromArgb("#1E2229"),
        PopupBackground: Color.FromArgb("#2A3140"),
        Border: Color.FromArgb("#2E3440"),
        Hover: Color.FromArgb("#33FFFFFF"),
        Text: Color.FromArgb("#F3F4F6"),
        TextSecondary: Color.FromArgb("#9AA3B2"),
        Danger: Color.FromArgb("#F87171"),
        NeutralButton: Color.FromArgb("#343B47"),
        NeutralButtonHover: Color.FromArgb("#434B59"),
        NeutralButtonPressed: Color.FromArgb("#525B6B"),
        CardBackground: Color.FromArgb("#1E2229"),
        PageBackground: Color.FromArgb("#131417"),
        TileBackground: Color.FromArgb("#24262B"),
        TileStroke: Color.FromArgb("#33363D"));

    /// <summary>
    /// The resources of the palette, by <see cref="XdKeys"/>.
    /// </summary>
    public IEnumerable<KeyValuePair<string, Color>> ToResources()
    {
        yield return new(XdKeys.PanelBackground, PanelBackground);
        yield return new(XdKeys.PopupBackground, PopupBackground);
        yield return new(XdKeys.Border, Border);
        yield return new(XdKeys.Hover, Hover);
        yield return new(XdKeys.Text, Text);
        yield return new(XdKeys.TextSecondary, TextSecondary);
        yield return new(XdKeys.Danger, Danger);
        yield return new(XdKeys.NeutralButton, NeutralButton);
        yield return new(XdKeys.NeutralButtonHover, NeutralButtonHover);
        yield return new(XdKeys.NeutralButtonPressed, NeutralButtonPressed);
    }
}
