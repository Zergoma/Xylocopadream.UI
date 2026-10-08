using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Xylocopadream.UI.Avalonia.Controls;

/// <summary>A color proposed by a <see cref="ColorPicker"/>.</summary>
public sealed record ColorPreset(string Name, Color Color);

/// <summary>
/// Choice of a color: among <see cref="Presets"/>, on the shade square (saturation from left to right, brightness
/// from bottom to top) and the hue bar, or by its hex code; a button goes back to <see cref="DefaultColor"/>.
/// <see cref="SelectedColor"/> changes at once (two-way). Same control as in Xylocopadream.UI.Maui; usually shown in
/// a flyout: <c>&lt;xd:ColorPicker Title="Couleur" SelectedColor="{Binding Accent}" /&gt;</c>.
/// </summary>
public class ColorPicker : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ColorPicker, string?>(nameof(Title));

    public static readonly StyledProperty<IEnumerable<ColorPreset>?> PresetsProperty =
        AvaloniaProperty.Register<ColorPicker, IEnumerable<ColorPreset>?>(nameof(Presets));

    public static readonly StyledProperty<Color> SelectedColorProperty =
        AvaloniaProperty.Register<ColorPicker, Color>(nameof(SelectedColor), DefaultAccent, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<Color> DefaultColorProperty =
        AvaloniaProperty.Register<ColorPicker, Color>(nameof(DefaultColor), DefaultAccent);

    public static readonly StyledProperty<string> ResetTextProperty =
        AvaloniaProperty.Register<ColorPicker, string>(nameof(ResetText), "Par défaut");

    private static readonly Color DefaultAccent = Color.Parse("#3574F0");
    private const double SwatchSize = 22;

    private readonly TextBlock _title = new() { FontWeight = FontWeight.SemiBold, FontSize = 14, IsVisible = false };
    private readonly WrapPanel _presets = new() { IsVisible = false };
    private readonly Border _hueColor = new() { CornerRadius = new CornerRadius(6) };
    private readonly Ellipse _shadeMarker = new()
    {
        Width = 14,
        Height = 14,
        Stroke = Brushes.White,
        StrokeThickness = 2,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
        IsHitTestVisible = false,
    };

    private readonly Grid _shadeSquare;
    private readonly Border _hueMarker = new()
    {
        Width = 8,
        Height = 18,
        Margin = new Thickness(0, -2),
        HorizontalAlignment = HorizontalAlignment.Left,
        CornerRadius = new CornerRadius(3),
        BorderThickness = new Thickness(2),
        BorderBrush = Brushes.White,
        IsHitTestVisible = false,
    };

    private readonly Grid _hueBar;
    private readonly Border _swatch = new() { Width = SwatchSize + 6, Height = SwatchSize + 6, CornerRadius = new CornerRadius(6) };
    private readonly TextBox _hexBox = new() { MaxLength = 7, FontFamily = new FontFamily("Cascadia Mono, Consolas, monospace") };
    private readonly Button _reset = new() { VerticalAlignment = VerticalAlignment.Center };

    // Position in the picker, kept apart from the color: a grey has no hue but the bar keeps one.
    private double _hue;
    private double _saturation;
    private double _value;
    private bool _isUpdating;

    public ColorPicker()
    {
        _shadeSquare = new Grid
        {
            Height = 130,
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Cross),
            Children =
            {
                _hueColor,
                Gradient(new RelativePoint(0, 0, RelativeUnit.Relative), new RelativePoint(1, 0, RelativeUnit.Relative), Colors.White, Color.Parse("#00FFFFFF")),
                Gradient(new RelativePoint(0, 0, RelativeUnit.Relative), new RelativePoint(0, 1, RelativeUnit.Relative), Color.Parse("#00000000"), Colors.Black),
                _shadeMarker,
            },
        };
        AddPointer(_shadeSquare, PickShade);

        _hueBar = new Grid { Height = 14, Background = Brushes.Transparent, Cursor = new Cursor(StandardCursorType.Hand), Children = { HueGradient(), _hueMarker } };
        AddPointer(_hueBar, PickHue);

        _hexBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
            {
                OnHexTyped(_hexBox.Text);
            }
        };
        _reset.Content = ResetText;
        _reset.Click += (_, _) => SelectedColor = DefaultColor;

        var code = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 8 };
        code.Children.Add(_swatch);
        Grid.SetColumn(_hexBox, 1);
        code.Children.Add(_hexBox);
        Grid.SetColumn(_reset, 2);
        code.Children.Add(_reset);

        Content = new StackPanel { Spacing = 10, Width = 240, Children = { _title, _presets, _shadeSquare, _hueBar, code } };

        _shadeSquare.SizeChanged += (_, _) => PlaceMarkers();
        _hueBar.SizeChanged += (_, _) => PlaceMarkers();
        OnSelectedColorChanged(SelectedColor);
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IEnumerable<ColorPreset>? Presets
    {
        get => GetValue(PresetsProperty);
        set => SetValue(PresetsProperty, value);
    }

    public Color SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    public Color DefaultColor
    {
        get => GetValue(DefaultColorProperty);
        set => SetValue(DefaultColorProperty, value);
    }

    /// <summary>Text of the button going back to <see cref="DefaultColor"/>.</summary>
    public string ResetText
    {
        get => GetValue(ResetTextProperty);
        set => SetValue(ResetTextProperty, value);
    }

    /// <summary>The hex code box, for tests and keyboard focus.</summary>
    public TextBox HexBox => _hexBox;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TitleProperty)
        {
            _title.Text = Title;
            _title.IsVisible = !string.IsNullOrEmpty(Title);
        }
        else if (change.Property == PresetsProperty)
        {
            BuildPresets();
        }
        else if (change.Property == SelectedColorProperty)
        {
            OnSelectedColorChanged(SelectedColor);
        }
        else if (change.Property == ResetTextProperty)
        {
            _reset.Content = ResetText;
        }
    }

    // ------------------------------------------------------------------ building

    private static Border Gradient(RelativePoint start, RelativePoint end, Color from, Color to) => new()
    {
        CornerRadius = new CornerRadius(6),
        IsHitTestVisible = false,
        Background = new LinearGradientBrush
        {
            StartPoint = start,
            EndPoint = end,
            GradientStops = { new GradientStop(from, 0), new GradientStop(to, 1) },
        },
    };

    private static Border HueGradient()
    {
        string[] hues = ["#FF0000", "#FFFF00", "#00FF00", "#00FFFF", "#0000FF", "#FF00FF", "#FF0000"];
        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        };
        for (var i = 0; i < hues.Length; i++)
        {
            brush.GradientStops.Add(new GradientStop(Color.Parse(hues[i]), (double)i / (hues.Length - 1)));
        }

        return new Border { CornerRadius = new CornerRadius(7), IsHitTestVisible = false, Background = brush };
    }

    private void BuildPresets()
    {
        _presets.Children.Clear();
        var presets = Presets?.ToList() ?? [];
        _presets.IsVisible = presets.Count > 0;
        foreach (var preset in presets)
        {
            var swatch = new Button
            {
                Width = SwatchSize,
                Height = SwatchSize,
                MinWidth = 0,
                MinHeight = 0,
                Padding = default,
                Margin = new Thickness(0, 0, 6, 6),
                CornerRadius = new CornerRadius(SwatchSize / 2),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                Background = new SolidColorBrush(preset.Color),
                Focusable = false,
                Tag = preset,
            };
            ToolTip.SetTip(swatch, preset.Name);
            swatch.Classes.Add("xd-preset");
            swatch.Click += (_, _) => SelectedColor = preset.Color;
            _presets.Children.Add(swatch);
        }
    }

    private void AddPointer(Control area, Action<Point> pick)
    {
        area.PointerPressed += (_, e) =>
        {
            e.Pointer.Capture(area);
            pick(e.GetPosition(area));
            e.Handled = true;
        };
        area.PointerMoved += (_, e) =>
        {
            if (e.Pointer.Captured == area)
            {
                pick(e.GetPosition(area));
            }
        };
        area.PointerReleased += (_, e) => e.Pointer.Capture(null);
    }

    // ------------------------------------------------------------------- picking

    private void PickShade(Point p)
    {
        if (_shadeSquare.Bounds.Width <= 0)
        {
            return;
        }

        SetHsv(_hue, Math.Clamp(p.X / _shadeSquare.Bounds.Width, 0, 1), Math.Clamp(1 - (p.Y / _shadeSquare.Bounds.Height), 0, 1));
    }

    private void PickHue(Point p)
    {
        if (_hueBar.Bounds.Width <= 0)
        {
            return;
        }

        SetHsv(Math.Clamp(p.X / _hueBar.Bounds.Width * 360, 0, 359.99), _saturation, _value);
    }

    private void SetHsv(double hue, double saturation, double value)
    {
        (_hue, _saturation, _value) = (hue, saturation, value);
        var hex = XdColorMath.HsvToHex(hue, saturation, value);
        _isUpdating = true;
        SelectedColor = Color.Parse(hex);
        _isUpdating = false;
        ShowColor(hex, updateText: true);
    }

    private void OnHexTyped(string? text)
    {
        if (_isUpdating || XdColorMath.NormalizeHex(text) is not { } hex)
        {
            return;
        }

        FromHex(hex);
        _isUpdating = true;
        SelectedColor = Color.Parse(hex);
        _isUpdating = false;

        // The text stays as typed (e.g. lower case).
        ShowColor(hex, updateText: false);
    }

    private void OnSelectedColorChanged(Color color)
    {
        if (_isUpdating)
        {
            return;
        }

        var hex = XdColorMath.ToHex(color);
        FromHex(hex);
        ShowColor(hex, updateText: true);
    }

    private void FromHex(string hex)
    {
        var (hue, saturation, value) = XdColorMath.HexToHsv(hex);

        // A grey has no hue: keep the one of the bar.
        if (saturation > 0)
        {
            _hue = hue;
        }

        _saturation = saturation;
        _value = value;
    }

    private void ShowColor(string hex, bool updateText)
    {
        _swatch.Background = new SolidColorBrush(Color.Parse(hex));
        _hueColor.Background = new SolidColorBrush(Color.Parse(XdColorMath.HsvToHex(_hue, 1, 1)));
        if (updateText)
        {
            _isUpdating = true;
            _hexBox.Text = hex;
            _isUpdating = false;
        }

        PlaceMarkers();
    }

    /// <summary>Markers on the shade square and the hue bar, where the current color is.</summary>
    private void PlaceMarkers()
    {
        var shade = _shadeSquare.Bounds;
        var bar = _hueBar.Bounds;
        if (shade.Width <= 0 || bar.Width <= 0)
        {
            return;
        }

        _shadeMarker.RenderTransform = new TranslateTransform(
            (_saturation * shade.Width) - (_shadeMarker.Width / 2),
            ((1 - _value) * shade.Height) - (_shadeMarker.Height / 2));
        _hueMarker.RenderTransform = new TranslateTransform((_hue / 360 * bar.Width) - (_hueMarker.Width / 2), 0);
    }
}
