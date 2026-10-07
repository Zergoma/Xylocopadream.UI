using Microsoft.Maui.Graphics;

namespace Xylocopadream.UI.Maui.Tests;

public sealed class LibraryTests
{
    [Theory]
    [InlineData("#2f6feb", "#2F6FEB")]
    [InlineData("2F6FEB", "#2F6FEB")]
    [InlineData(" #abc ", "#AABBCC")]
    [InlineData("#12345", null)]
    [InlineData("#GGGGGG", null)]
    [InlineData("", null)]
    public void NormalizeHex_accepts_the_usual_writings(string text, string? expected)
        => Assert.Equal(expected, XdColorMath.NormalizeHex(text));

    [Theory]
    [InlineData("#FF0000")]
    [InlineData("#2F6FEB")]
    [InlineData("#7C3AED")]
    [InlineData("#16A34A")]
    [InlineData("#808080")]
    public void Hex_and_hsv_go_both_ways(string hex)
    {
        var (hue, saturation, value) = XdColorMath.HexToHsv(hex);

        Assert.Equal(hex, XdColorMath.HsvToHex(hue, saturation, value));
        Assert.Equal(hex, XdColorMath.ToHex(Color.FromArgb(hex)));
    }

    [Fact]
    public void Pure_hue_and_full_shade_give_the_pure_color()
        => Assert.Equal("#FF0000", XdColorMath.HsvToHex(0, 1, 1));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Theme_gives_every_key_for_both_themes(bool dark)
    {
        IReadOnlyDictionary<string, Color> resources = XdTheme.Resources(dark, XdTheme.DefaultAccent);

        string[] keys = typeof(XdKeys).GetFields()
            .Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();

        Assert.NotEmpty(keys);
        Assert.All(keys, key => Assert.True(resources.ContainsKey(key), key));
        Assert.All(keys, key => Assert.StartsWith("Xd.", key));
    }

    [Fact]
    public void Light_theme_uses_the_accent_as_is()
    {
        Color accent = Color.FromArgb("#7C3AED");

        Assert.Equal(accent, XdTheme.Resources(dark: false, accent)[XdKeys.Accent]);
        Assert.NotEqual(accent, XdTheme.Resources(dark: true, accent)[XdKeys.Accent]);
    }

    [Theory]
    [InlineData("#2F6FEB", "#FFFFFF")]  // blue: white text
    [InlineData("#FACC15", "#1F2937")]  // yellow: dark text
    public void Text_on_the_accent_stays_readable(string accent, string expected)
        => Assert.Equal(expected, XdColorMath.ToHex(XdTheme.Resources(dark: false, Color.FromArgb(accent))[XdKeys.AccentForeground]));

    [Theory]
    [InlineData("#2F6FEB")]  // blue: violet and teal
    [InlineData("#FACC15")]  // yellow: the brightest hues around
    [InlineData("#16A34A")]
    [InlineData("#DC2626")]
    [InlineData("#808080")]  // grey: still colored alternates
    public void Alternates_turn_the_hue_and_keep_a_white_text_readable(string accent)
    {
        foreach (bool dark in new[] { false, true })
        {
            IReadOnlyDictionary<string, Color> resources = XdTheme.Resources(dark, Color.FromArgb(accent));

            foreach (string key in new[] { XdKeys.AccentAlternate1, XdKeys.AccentAlternate2 })
            {
                // WCAG contrast of white on the color: 3 at least, the minimum for bold text
                double contrast = 1.05 / (XdAccentPalette.RelativeLuminance(resources[key]) + 0.05);
                Assert.True(contrast >= 3, $"{accent} {key} dark={dark}: {contrast:0.0}");
            }

            Assert.NotEqual(resources[XdKeys.AccentAlternate1], resources[XdKeys.AccentAlternate2]);
        }
    }

    [Fact]
    public void Default_accent_gives_violet_and_teal_alternates()
    {
        IReadOnlyDictionary<string, Color> resources = XdTheme.Resources(dark: false, XdTheme.DefaultAccent);

        Assert.InRange(resources[XdKeys.AccentAlternate1].GetHue() * 360, 255, 275);
        Assert.InRange(resources[XdKeys.AccentAlternate2].GetHue() * 360, 165, 185);
    }

    [Fact]
    public void Every_icon_is_listed_and_parsed()
    {
        int properties = typeof(XdIcons).GetProperties().Count(p => p.PropertyType == typeof(Microsoft.Maui.Controls.Shapes.Geometry));

        Assert.Equal(properties, XdIcons.All.Count);
        Assert.All(XdIcons.All, icon => Assert.NotNull(icon.Geometry));
        Assert.Equal(XdIcons.All.Count, XdIcons.All.Select(i => i.Name).Distinct().Count());
    }
}
