namespace Xylocopadream.UI.Maui;

/// <summary>
/// Keys of the colors <see cref="XdTheme"/> puts in the application resources, for
/// <c>{DynamicResource Xd.Accent}</c> in XAML or <c>SetDynamicResource(..., XdKeys.Accent)</c> in code.
/// They follow the light / dark theme and the accent color.
/// </summary>
public static class XdKeys
{
    // surfaces and text (XdPalette)
    public const string PanelBackground = "Xd.PanelBackground";
    public const string PopupBackground = "Xd.PopupBackground";
    public const string Border = "Xd.Border";
    public const string Hover = "Xd.Hover";
    public const string Text = "Xd.Text";
    public const string TextSecondary = "Xd.TextSecondary";
    public const string Danger = "Xd.Danger";
    public const string NeutralButton = "Xd.NeutralButton";
    public const string NeutralButtonHover = "Xd.NeutralButtonHover";
    public const string NeutralButtonPressed = "Xd.NeutralButtonPressed";

    // accent color and its variants (XdAccentPalette)
    public const string Accent = "Xd.Accent";
    public const string AccentHover = "Xd.AccentHover";
    public const string AccentPressed = "Xd.AccentPressed";
    public const string AccentForeground = "Xd.AccentForeground";
    public const string AccentSoft = "Xd.AccentSoft";
    public const string AccentSoftHover = "Xd.AccentSoftHover";
    public const string AccentText = "Xd.AccentText";
    public const string AccentGradientEnd = "Xd.AccentGradientEnd";
    public const string AccentSurface = "Xd.AccentSurface";
    public const string AccentOnSurface = "Xd.AccentOnSurface";
    public const string AccentPageBackground = "Xd.AccentPageBackground";
    public const string AccentTile = "Xd.AccentTile";
    public const string AccentTileHover = "Xd.AccentTileHover";
    public const string AccentTilePressed = "Xd.AccentTilePressed";
    public const string AccentTileStroke = "Xd.AccentTileStroke";

    // two colors going with the accent, its hue turned by +45° and -45° (badges, tags, categories),
    // dark enough for a white text, and the text to put on them
    public const string AccentAlternate1 = "Xd.AccentAlternate1";
    public const string AccentAlternate1Foreground = "Xd.AccentAlternate1Foreground";
    public const string AccentAlternate2 = "Xd.AccentAlternate2";
    public const string AccentAlternate2Foreground = "Xd.AccentAlternate2Foreground";
}
