using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Xylocopadream.UI.Avalonia.Controls;

/// <summary>
/// Text whose parts matching <see cref="Highlight"/> are shown in color, as in Rider's search results: with
/// "tr", "attrise" and "triste" show their "tr" highlighted. Matching ignores case.
/// </summary>
public class HighlightedTextBlock : TextBlock
{
    public static readonly StyledProperty<string?> SourceTextProperty =
        AvaloniaProperty.Register<HighlightedTextBlock, string?>(nameof(SourceText));

    public static readonly StyledProperty<string?> HighlightProperty =
        AvaloniaProperty.Register<HighlightedTextBlock, string?>(nameof(Highlight));

    public static readonly StyledProperty<IBrush?> HighlightBackgroundProperty =
        AvaloniaProperty.Register<HighlightedTextBlock, IBrush?>(nameof(HighlightBackground));

    public static readonly StyledProperty<IBrush?> HighlightForegroundProperty =
        AvaloniaProperty.Register<HighlightedTextBlock, IBrush?>(nameof(HighlightForeground));

    /// <summary>The full text (<see cref="TextBlock.Text"/> is rebuilt from it).</summary>
    public string? SourceText
    {
        get => GetValue(SourceTextProperty);
        set => SetValue(SourceTextProperty, value);
    }

    /// <summary>Text to highlight; null or empty highlights nothing.</summary>
    public string? Highlight
    {
        get => GetValue(HighlightProperty);
        set => SetValue(HighlightProperty, value);
    }

    /// <summary>Background of matches; defaults to the theme's <c>Xd.Highlight</c> brush.</summary>
    public IBrush? HighlightBackground
    {
        get => GetValue(HighlightBackgroundProperty);
        set => SetValue(HighlightBackgroundProperty, value);
    }

    /// <summary>Foreground of matches; defaults to the theme's <c>Xd.HighlightText</c> brush.</summary>
    public IBrush? HighlightForeground
    {
        get => GetValue(HighlightForegroundProperty);
        set => SetValue(HighlightForegroundProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(TextBlock);

    /// <summary>Cuts <paramref name="text"/> into plain and matching parts, in order (every match, case ignored).</summary>
    public static IReadOnlyList<(string Text, bool IsMatch)> Split(string? text, string? highlight)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        if (string.IsNullOrEmpty(highlight))
        {
            return [(text, false)];
        }

        var parts = new List<(string, bool)>();
        var start = 0;
        int index;
        while ((index = text.IndexOf(highlight, start, StringComparison.CurrentCultureIgnoreCase)) >= 0)
        {
            if (index > start)
            {
                parts.Add((text[start..index], false));
            }

            parts.Add((text.Substring(index, highlight.Length), true));
            start = index + highlight.Length;
        }

        if (start < text.Length)
        {
            parts.Add((text[start..], false));
        }

        return parts;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SourceTextProperty
            || change.Property == HighlightProperty
            || change.Property == HighlightBackgroundProperty
            || change.Property == HighlightForegroundProperty)
        {
            Rebuild();
        }
    }

    protected override void OnAttachedToLogicalTree(global::Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        Rebuild(); // theme brushes are only found once attached
    }

    private void Rebuild()
    {
        var parts = Split(SourceText, Highlight);
        if (!parts.Any(p => p.IsMatch))
        {
            Inlines?.Clear();
            Text = SourceText;
            return;
        }

        var background = HighlightBackground ?? FindBrush("Xd.Highlight", Brushes.DarkGoldenrod);
        var foreground = HighlightForeground ?? FindBrush("Xd.HighlightText", Brushes.White);
        var inlines = new InlineCollection();
        foreach (var (text, isMatch) in parts)
        {
            inlines.Add(isMatch ? new Run(text) { Background = background, Foreground = foreground } : new Run(text));
        }

        Inlines = inlines;
    }

    private IBrush FindBrush(string key, IBrush fallback) =>
        this.TryFindResource(key, ActualThemeVariant, out var value) && value is IBrush brush ? brush : fallback;
}
