using Microsoft.Maui.Controls.Shapes;

namespace Xylocopadream.UI.Maui.Controls;

/// <summary>
/// A color proposed by a <see cref="ColorPicker"/>.
/// </summary>
public record ColorPreset(string Name, Color Color);

/// <summary>
/// Choice of a color: among <see cref="Presets"/>, on the shade square (saturation from left to right,
/// brightness from bottom to top) and the hue bar, or by its hex code; a button goes back to <see cref="DefaultColor"/>.
/// <see cref="SelectedColor"/> changes at once (two-way). Usually shown in a <see cref="Popover"/>:
/// <c>&lt;xd:ColorPicker Title="Accent color" SelectedColor="{Binding Accent}" /&gt;</c>.
/// </summary>
public class ColorPicker : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(ColorPicker), null,
        propertyChanged: (bindable, _, value) =>
        {
            ColorPicker picker = (ColorPicker)bindable;
            picker._title.Text = (string?)value;
            picker._title.IsVisible = !string.IsNullOrEmpty((string?)value);
        });

    public static readonly BindableProperty PresetsProperty = BindableProperty.Create(
        nameof(Presets), typeof(IEnumerable<ColorPreset>), typeof(ColorPicker), null,
        propertyChanged: (bindable, _, _) => ((ColorPicker)bindable).BuildPresets());

    public static readonly BindableProperty SelectedColorProperty = BindableProperty.Create(
        nameof(SelectedColor), typeof(Color), typeof(ColorPicker), XdTheme.DefaultAccent, BindingMode.TwoWay,
        propertyChanged: (bindable, _, value) => ((ColorPicker)bindable).OnSelectedColorChanged((Color)value));

    public static readonly BindableProperty DefaultColorProperty = BindableProperty.Create(
        nameof(DefaultColor), typeof(Color), typeof(ColorPicker), XdTheme.DefaultAccent);

    public static readonly BindableProperty ResetTextProperty = BindableProperty.Create(
        nameof(ResetText), typeof(string), typeof(ColorPicker), "Default",
        propertyChanged: (bindable, _, value) => ((ColorPicker)bindable)._resetText.Text = (string?)value);

    private const double SwatchSize = 28;

    private readonly Label _title;
    private readonly WrapPanel _presets;
    private readonly Grid _shadeSquare;
    private readonly BoxView _hueColor;
    private readonly Ellipse _shadeMarker;
    private readonly Grid _hueBar;
    private readonly Border _hueMarker;
    private readonly Border _swatch;
    private readonly Entry _hexEntry;
    private readonly Label _resetText;

    // position in the picker: kept apart from the color, a grey has no hue but the bar keeps one
    private double _hue;
    private double _saturation;
    private double _value;
    private bool _isUpdating;
    private bool _isPicking;

    public ColorPicker()
    {
        XdTheme.EnsureApplied();

        _title = new Label { FontAttributes = FontAttributes.Bold, FontSize = 15, IsVisible = false };
        _title.SetDynamicResource(Label.TextColorProperty, XdKeys.Text);

        _presets = new WrapPanel { Spacing = 10, IsVisible = false };

        // shade: the pure hue, whitened to the left, darkened to the bottom
        _hueColor = new BoxView { CornerRadius = 6 };
        _shadeMarker = new Ellipse
        {
            WidthRequest = 14,
            HeightRequest = 14,
            Stroke = Colors.White,
            StrokeThickness = 2,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            InputTransparent = true,
        };
        _shadeSquare = new Grid
        {
            HeightRequest = 130,
            Children =
            {
                _hueColor,
                Gradient(new Point(0, 0), new Point(1, 0), Colors.White, Color.FromArgb("#00FFFFFF"), 6),
                Gradient(new Point(0, 0), new Point(0, 1), Color.FromArgb("#00000000"), Colors.Black, 6),
                _shadeMarker,
            },
        };
        AddPointer(_shadeSquare, PickShade);

        _hueMarker = new Border
        {
            WidthRequest = 8,
            HeightRequest = 18,
            Margin = new Thickness(0, -2),
            HorizontalOptions = LayoutOptions.Start,
            StrokeShape = new RoundRectangle { CornerRadius = 3 },
            StrokeThickness = 2,
            Stroke = Colors.White,
            BackgroundColor = Colors.Transparent,
            InputTransparent = true,
        };
        _hueBar = new Grid { HeightRequest = 14, Children = { HueGradient(), _hueMarker } };
        AddPointer(_hueBar, PickHue);

        // exact color, by its code
        _swatch = new Border
        {
            WidthRequest = SwatchSize,
            HeightRequest = SwatchSize,
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            StrokeThickness = 0,
        };
        _hexEntry = new Entry
        {
            MaxLength = 7,
            FontSize = 14,
            IsSpellCheckEnabled = false,
            IsTextPredictionEnabled = false,
        };
        _hexEntry.TextChanged += (_, e) => OnHexTyped(e.NewTextValue);

        _resetText = new Label
        {
            Text = ResetText,
            TextColor = Colors.White,
            FontSize = 12,
            Margin = new Thickness(10, 0),
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
        };

        Grid code = new()
        {
            ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
            ColumnSpacing = 10,
        };
        code.Add(_swatch, 0);
        code.Add(_hexEntry, 1);
        code.Add(ResetButton(), 2);

        Content = new VerticalStackLayout
        {
            Spacing = 12,
            Children = { _title, _presets, _shadeSquare, _hueBar, code },
        };

        _shadeSquare.SizeChanged += (_, _) => PlaceMarkers();
        _hueBar.SizeChanged += (_, _) => PlaceMarkers();
        OnSelectedColorChanged(SelectedColor);
    }

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IEnumerable<ColorPreset>? Presets
    {
        get => (IEnumerable<ColorPreset>?)GetValue(PresetsProperty);
        set => SetValue(PresetsProperty, value);
    }

    public Color SelectedColor
    {
        get => (Color)GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    public Color DefaultColor
    {
        get => (Color)GetValue(DefaultColorProperty);
        set => SetValue(DefaultColorProperty, value);
    }

    /// <summary>
    /// Text of the button going back to <see cref="DefaultColor"/>.
    /// </summary>
    public string ResetText
    {
        get => (string)GetValue(ResetTextProperty);
        set => SetValue(ResetTextProperty, value);
    }

    #region Building

    private static BoxView Gradient(Point start, Point end, Color from, Color to, double cornerRadius)
        => new()
        {
            CornerRadius = cornerRadius,
            InputTransparent = true,
            Background = new LinearGradientBrush(
                [new GradientStop(from, 0), new GradientStop(to, 1)], start, end),
        };

    private static BoxView HueGradient()
    {
        GradientStopCollection stops = [];
        string[] hues = ["#FF0000", "#FFFF00", "#00FF00", "#00FFFF", "#0000FF", "#FF00FF", "#FF0000"];
        for (int i = 0; i < hues.Length; i++)
            stops.Add(new GradientStop(Color.FromArgb(hues[i]), (float)i / (hues.Length - 1)));

        return new BoxView
        {
            CornerRadius = 7,
            InputTransparent = true,
            Background = new LinearGradientBrush(stops, new Point(0, 0), new Point(1, 0)),
        };
    }

    /// <summary>
    /// A neutral pill button, white text, shaded on hover.
    /// </summary>
    private Border ResetButton()
    {
        BoxView normal = Layers.Fill(XdKeys.NeutralButton, isVisible: true);
        BoxView hover = Layers.Fill(XdKeys.NeutralButtonHover);

        Border button = new()
        {
            HeightRequest = 30,
            VerticalOptions = LayoutOptions.Center,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 15 },
            Content = new Grid { Children = { normal, hover, _resetText } },
        };

        TapGestureRecognizer tap = new();
        tap.Tapped += (_, _) => SelectedColor = DefaultColor;
        button.GestureRecognizers.Add(tap);

        PointerGestureRecognizer pointer = new();
        pointer.PointerEntered += (_, _) => { hover.IsVisible = true; normal.IsVisible = false; };
        pointer.PointerExited += (_, _) => { hover.IsVisible = false; normal.IsVisible = true; };
        button.GestureRecognizers.Add(pointer);

        return button;
    }

    private void BuildPresets()
    {
        _presets.Children.Clear();
        List<ColorPreset> presets = Presets?.ToList() ?? [];
        _presets.IsVisible = presets.Count > 0;

        foreach (ColorPreset preset in presets)
        {
            Border swatch = new()
            {
                WidthRequest = SwatchSize,
                HeightRequest = SwatchSize,
                StrokeShape = new RoundRectangle { CornerRadius = SwatchSize / 2 },
                StrokeThickness = 0,
                BackgroundColor = preset.Color,
            };
            ToolTipProperties.SetText(swatch, preset.Name);

            TapGestureRecognizer tap = new();
            tap.Tapped += (_, _) => SelectedColor = preset.Color;
            swatch.GestureRecognizers.Add(tap);

            _presets.Children.Add(swatch);
        }
    }

    private void AddPointer(View area, Action<Point> pick)
    {
        PointerGestureRecognizer pointer = new();
        pointer.PointerPressed += (_, e) =>
        {
            _isPicking = true;
            if (e.GetPosition(area) is Point p)
                pick(p);
        };
        pointer.PointerMoved += (_, e) =>
        {
            if (_isPicking && e.GetPosition(area) is Point p)
                pick(p);
        };
        pointer.PointerReleased += (_, _) => _isPicking = false;
        pointer.PointerExited += (_, _) => _isPicking = false;
        area.GestureRecognizers.Add(pointer);
    }

    #endregion

    #region Picking

    private void PickShade(Point p)
    {
        if (_shadeSquare.Width <= 0)
            return;

        SetHsv(_hue, Math.Clamp(p.X / _shadeSquare.Width, 0, 1), Math.Clamp(1 - p.Y / _shadeSquare.Height, 0, 1));
    }

    private void PickHue(Point p)
    {
        if (_hueBar.Width <= 0)
            return;

        SetHsv(Math.Clamp(p.X / _hueBar.Width * 360, 0, 359.99), _saturation, _value);
    }

    private void SetHsv(double hue, double saturation, double value)
    {
        (_hue, _saturation, _value) = (hue, saturation, value);
        string hex = XdColorMath.HsvToHex(hue, saturation, value);

        _isUpdating = true;
        SelectedColor = Color.FromArgb(hex);
        _isUpdating = false;

        ShowColor(hex, updateText: true);
    }

    private void OnHexTyped(string? text)
    {
        if (_isUpdating || XdColorMath.NormalizeHex(text) is not string hex)
            return;

        FromHex(hex);
        _isUpdating = true;
        SelectedColor = Color.FromArgb(hex);
        _isUpdating = false;

        // the text stays as typed (e.g. lower case)
        ShowColor(hex, updateText: false);
    }

    private void OnSelectedColorChanged(Color color)
    {
        if (_isUpdating)
            return;

        string hex = XdColorMath.ToHex(color);
        FromHex(hex);
        ShowColor(hex, updateText: true);
    }

    private void FromHex(string hex)
    {
        var (hue, saturation, value) = XdColorMath.HexToHsv(hex);

        // a grey has no hue: keep the one of the bar
        if (saturation > 0)
            _hue = hue;
        _saturation = saturation;
        _value = value;
    }

    private void ShowColor(string hex, bool updateText)
    {
        _swatch.BackgroundColor = Color.FromArgb(hex);
        _hueColor.Color = Color.FromArgb(XdColorMath.HsvToHex(_hue, 1, 1));

        if (updateText)
        {
            _isUpdating = true;
            _hexEntry.Text = hex;
            _isUpdating = false;
        }

        PlaceMarkers();
    }

    /// <summary>
    /// Markers on the shade square and the hue bar, where the current color is.
    /// </summary>
    private void PlaceMarkers()
    {
        if (_shadeSquare.Width <= 0 || _hueBar.Width <= 0)
            return;

        _shadeMarker.TranslationX = _saturation * _shadeSquare.Width - _shadeMarker.WidthRequest / 2;
        _shadeMarker.TranslationY = (1 - _value) * _shadeSquare.Height - _shadeMarker.HeightRequest / 2;
        _hueMarker.TranslationX = _hue / 360 * _hueBar.Width - _hueMarker.WidthRequest / 2;
    }

    #endregion
}
