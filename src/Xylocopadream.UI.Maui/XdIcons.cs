using Microsoft.Maui.Controls.Shapes;

namespace Xylocopadream.UI.Maui;

/// <summary>
/// Vector icons for <see cref="Controls.IconButton"/>, <see cref="Controls.NavRailButton"/> or a <c>Path</c>:
/// filled shapes on a 24 x 24 grid (<c>Aspect="Uniform"</c> scales them).
/// Paths from Google Material Icons (Apache License 2.0).
/// </summary>
public static class XdIcons
{
    private static readonly PathGeometryConverter s_converter = new();

    /// <summary>
    /// "skip_previous": bar then triangle pointing left.
    /// </summary>
    public static Geometry Previous { get; } = Parse("M6 6h2v12H6zm3.5 6l8.5 6V6z");

    /// <summary>
    /// "skip_next": triangle pointing right then bar.
    /// </summary>
    public static Geometry Next { get; } = Parse("M6 18l8.5-6L6 6v12zM16 6v12h2V6h-2z");

    /// <summary>
    /// "block": circle crossed by a diagonal, forbidden / never again.
    /// </summary>
    public static Geometry Block { get; } = Parse(
        "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zM4 12c0-4.42 3.58-8 8-8 1.85 0 3.55.63 4.9 1.69L5.69 16.9C4.63 15.55 4 13.85 4 12zm8 8c-1.85 0-3.55-.63-4.9-1.69L18.31 7.1C19.37 8.45 20 10.15 20 12c0 4.42-3.58 8-8 8z");

    /// <summary>
    /// "shuffle": crossing arrows, "random".
    /// </summary>
    public static Geometry Shuffle { get; } = Parse(
        "M10.59 9.17L5.41 4 4 5.41l5.17 5.17 1.42-1.41zM14.5 4l2.04 2.04L4 18.59 5.41 20 17.96 7.46 20 9.5V4h-5.5zm.33 9.41l-1.41 1.41 3.13 3.13L14.5 20H20v-5.5l-2.04 2.04-3.13-3.13z");

    /// <summary>
    /// "light_mode": sun, light theme.
    /// </summary>
    public static Geometry Sun { get; } = Parse(
        "M12 7c-2.76 0-5 2.24-5 5s2.24 5 5 5 5-2.24 5-5-2.24-5-5-5zM2 13h2c.55 0 1-.45 1-1s-.45-1-1-1H2c-.55 0-1 .45-1 1s.45 1 1 1zm18 0h2c.55 0 1-.45 1-1s-.45-1-1-1h-2c-.55 0-1 .45-1 1s.45 1 1 1zM11 2v2c0 .55.45 1 1 1s1-.45 1-1V2c0-.55-.45-1-1-1s-1 .45-1 1zm0 18v2c0 .55.45 1 1 1s1-.45 1-1v-2c0-.55-.45-1-1-1s-1 .45-1 1zM5.99 4.58c-.39-.39-1.03-.39-1.41 0-.39.39-.39 1.03 0 1.41l1.06 1.06c.39.39 1.03.39 1.41 0s.39-1.03 0-1.41L5.99 4.58zm12.37 12.37c-.39-.39-1.03-.39-1.41 0-.39.39-.39 1.03 0 1.41l1.06 1.06c.39.39 1.03.39 1.41 0 .39-.39.39-1.03 0-1.41l-1.06-1.06zm1.06-10.96c.39-.39.39-1.03 0-1.41-.39-.39-1.03-.39-1.41 0l-1.06 1.06c-.39.39-.39 1.03 0 1.41s1.03.39 1.41 0l1.06-1.06zM7.05 18.36c.39-.39.39-1.03 0-1.41-.39-.39-1.03-.39-1.41 0l-1.06 1.06c-.39.39-.39 1.03 0 1.41s1.03.39 1.41 0l1.06-1.06z");

    /// <summary>
    /// "dark_mode": crescent moon, dark theme.
    /// </summary>
    public static Geometry Moon { get; } = Parse(
        "M12 3c-4.97 0-9 4.03-9 9s4.03 9 9 9 9-4.03 9-9c0-.46-.04-.92-.1-1.36-.98 1.37-2.58 2.26-4.4 2.26-2.98 0-5.4-2.42-5.4-5.4 0-1.81.89-3.42 2.26-4.4-.44-.06-.9-.1-1.36-.1z");

    /// <summary>
    /// "desktop_windows": monitor, theme following the system.
    /// </summary>
    public static Geometry Monitor { get; } = Parse(
        "M20 2H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h7v2H8v2h8v-2h-3v-2h7c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm0 14H4V4h16v12z");

    /// <summary>
    /// "play_arrow": start.
    /// </summary>
    public static Geometry Play { get; } = Parse("M8 5v14l11-7z");

    /// <summary>
    /// "home": the home page.
    /// </summary>
    public static Geometry Home { get; } = Parse("M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z");

    /// <summary>
    /// "format_list_bulleted": a list.
    /// </summary>
    public static Geometry List { get; } = Parse(
        "M4 10.5c-.83 0-1.5.67-1.5 1.5s.67 1.5 1.5 1.5 1.5-.67 1.5-1.5-.67-1.5-1.5-1.5zm0-6c-.83 0-1.5.67-1.5 1.5S3.17 7.5 4 7.5 5.5 6.83 5.5 6 4.83 4.5 4 4.5zm0 12c-.83 0-1.5.68-1.5 1.5s.68 1.5 1.5 1.5 1.5-.68 1.5-1.5-.67-1.5-1.5-1.5zM7 19h14v-2H7v2zm0-6h14v-2H7v2zm0-8v2h14V5H7z");

    /// <summary>
    /// "spellcheck": words, spelling.
    /// </summary>
    public static Geometry Spellcheck { get; } = Parse(
        "M12.45 16h2.09L9.43 3H7.57L2.46 16h2.09l1.12-3h5.64l1.14 3zm-6.02-5L8.5 5.48 10.57 11H6.43zm15.16.59l-8.09 8.09L9.83 16l-1.41 1.41 5.09 5.09L23 13l-1.41-1.41z");

    /// <summary>
    /// "file_upload": import, upload.
    /// </summary>
    public static Geometry Upload { get; } = Parse("M5 20h14v-2H5v2zm0-10h4v6h6v-6h4l-7-7-7 7z");

    /// <summary>
    /// "keyboard": typing.
    /// </summary>
    public static Geometry Keyboard { get; } = Parse(
        "M20 5H4c-1.1 0-1.99.9-1.99 2L2 17c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V7c0-1.1-.9-2-2-2zm-9 3h2v2h-2V8zm0 3h2v2h-2v-2zM8 8h2v2H8V8zm0 3h2v2H8v-2zm-1 2H5v-2h2v2zm0-3H5V8h2v2zm9 7H8v-2h8v2zm0-4h-2v-2h2v2zm0-3h-2V8h2v2zm3 3h-2v-2h2v2zm0-3h-2V8h2v2z");

    /// <summary>
    /// "bar_chart": results, statistics.
    /// </summary>
    public static Geometry BarChart { get; } = Parse("M5 9.2h3V19H5zM10.6 5h2.8v14h-2.8zm5.6 8H19v6h-2.8z");

    /// <summary>
    /// "palette": colors, the accent color.
    /// </summary>
    public static Geometry Palette { get; } = Parse(
        "M12 3c-4.97 0-9 4.03-9 9s4.03 9 9 9c.83 0 1.5-.67 1.5-1.5 0-.39-.15-.74-.39-1.01-.23-.26-.38-.61-.38-.99 0-.83.67-1.5 1.5-1.5H16c2.76 0 5-2.24 5-5 0-4.42-4.03-8-9-8zm-5.5 9c-.83 0-1.5-.67-1.5-1.5S5.67 9 6.5 9 8 9.67 8 10.5 7.33 12 6.5 12zm3-4C8.67 8 8 7.33 8 6.5S8.67 5 9.5 5s1.5.67 1.5 1.5S10.33 8 9.5 8zm5 0c-.83 0-1.5-.67-1.5-1.5S13.67 5 14.5 5s1.5.67 1.5 1.5S15.33 8 14.5 8zm3 4c-.83 0-1.5-.67-1.5-1.5S16.67 9 17.5 9s1.5.67 1.5 1.5-.67 1.5-1.5 1.5z");

    /// <summary>
    /// "person": head and shoulders, a user.
    /// </summary>
    public static Geometry Person { get; } = Parse(
        "M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z");

    /// <summary>
    /// "group": two people, the users.
    /// </summary>
    public static Geometry Group { get; } = Parse(
        "M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z");

    /// <summary>
    /// "check": tick, done or chosen.
    /// </summary>
    public static Geometry Check { get; } = Parse("M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z");

    /// <summary>
    /// "edit": pencil, rename or edit.
    /// </summary>
    public static Geometry Rename { get; } = Parse(
        "M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z");

    /// <summary>
    /// "delete": bin.
    /// </summary>
    public static Geometry Delete { get; } = Parse("M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z");

    /// <summary>
    /// "add": plus.
    /// </summary>
    public static Geometry Add { get; } = Parse("M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z");

    /// <summary>
    /// "arrow_drop_up": small triangle pointing up, move up.
    /// </summary>
    public static Geometry Up { get; } = Parse("M7 14l5-5 5 5z");

    /// <summary>
    /// "arrow_drop_down": small triangle pointing down, move down.
    /// </summary>
    public static Geometry Down { get; } = Parse("M7 10l5 5 5-5z");

    /// <summary>
    /// "emoji_events": cup, a record.
    /// </summary>
    public static Geometry Trophy { get; } = Parse(
        "M19 5h-2V3H7v2H5c-1.1 0-2 .9-2 2v1c0 2.55 1.92 4.63 4.39 4.94.63 1.5 1.98 2.63 3.61 2.96V19H7v2h10v-2h-4v-3.1c1.63-.33 2.98-1.46 3.61-2.96C19.08 12.63 21 10.55 21 8V7c0-1.1-.9-2-2-2zM5 8V7h2v3.82C5.84 10.4 5 9.3 5 8zm14 0c0 1.3-.84 2.4-2 2.82V7h2v1z");

    /// <summary>
    /// Every icon with its name (after the others: static fields are initialized in order).
    /// </summary>
    public static IReadOnlyList<(string Name, Geometry Geometry)> All { get; } =
    [
        (nameof(Previous), Previous),
        (nameof(Next), Next),
        (nameof(Block), Block),
        (nameof(Shuffle), Shuffle),
        (nameof(Sun), Sun),
        (nameof(Moon), Moon),
        (nameof(Monitor), Monitor),
        (nameof(Play), Play),
        (nameof(Home), Home),
        (nameof(List), List),
        (nameof(Spellcheck), Spellcheck),
        (nameof(Upload), Upload),
        (nameof(Keyboard), Keyboard),
        (nameof(BarChart), BarChart),
        (nameof(Palette), Palette),
        (nameof(Person), Person),
        (nameof(Group), Group),
        (nameof(Check), Check),
        (nameof(Rename), Rename),
        (nameof(Delete), Delete),
        (nameof(Add), Add),
        (nameof(Up), Up),
        (nameof(Down), Down),
        (nameof(Trophy), Trophy),
    ];

    private static Geometry Parse(string data)
        => (Geometry)s_converter.ConvertFromInvariantString(data)!;
}
