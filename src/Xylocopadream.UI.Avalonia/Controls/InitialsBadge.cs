using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Xylocopadream.UI.Avalonia.Controls;

/// <summary>
/// Rounded square with the initials of a name on its stable color, like Rider's project icons:
/// <c>&lt;xd:InitialsBadge Text="{Binding ProjectName}" Size="28" /&gt;</c>. A path shows its last segment.
/// </summary>
public class InitialsBadge : Border
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<InitialsBadge, string?>(nameof(Text));

    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<InitialsBadge, double>(nameof(Size), 22);

    private readonly TextBlock _initials = new()
    {
        Foreground = Brushes.White,
        FontWeight = FontWeight.Bold,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };

    public InitialsBadge()
    {
        Child = _initials;
        Update();
        ApplySize();
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Width and height in pixels; corner radius and font follow.</summary>
    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Initials currently shown (for tests and tooltips).</summary>
    public string Initials => _initials.Text ?? string.Empty;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty)
        {
            Update();
        }
        else if (change.Property == SizeProperty)
        {
            ApplySize();
        }
    }

    private void Update()
    {
        var name = Monogram.NameOfPath(Text ?? string.Empty);
        _initials.Text = Monogram.Of(name);
        Background = NamedColors.For(name);
    }

    private void ApplySize()
    {
        Width = Size;
        Height = Size;
        CornerRadius = new CornerRadius(Math.Round(Size * 0.23));
        _initials.FontSize = Math.Max(8, Math.Round(Size * 0.42));
    }
}
