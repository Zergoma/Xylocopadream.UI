using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;

using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Xylocopadream.UI.Avalonia.Controls;

/// <summary>A choice of a <see cref="RailChoiceGroup"/>: its icon and tooltip.</summary>
public class RailChoice
{
    public Geometry? Icon { get; set; }

    public string? ToolTip { get; set; }
}

/// <summary>
/// A few linked choices stacked in a tool window stripe (e.g. the theme: light, dark, system), the accent-colored
/// pill sliding to the selected one. Same control as in Xylocopadream.UI.Maui:
/// <code>
/// &lt;xd:RailChoiceGroup SelectedIndex="{Binding ThemeIndex}"&gt;
///     &lt;xd:RailChoice Icon="{StaticResource Xd.Icon.Sun}" ToolTip="Clair" /&gt;
///     ...
/// &lt;/xd:RailChoiceGroup&gt;
/// </code>
/// </summary>
public class RailChoiceGroup : UserControl
{
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<RailChoiceGroup, int>(nameof(SelectedIndex), -1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<RailChoiceGroup, Orientation>(nameof(Orientation), Orientation.Vertical);

    /// <summary>Side of the buttons, as the tool window buttons of a stripe.</summary>
    public const double ButtonSize = 28;

    private const double Spacing = 2;

    private readonly Border _pill = new()
    {
        Width = ButtonSize,
        Height = ButtonSize,
        CornerRadius = new CornerRadius(6),
        VerticalAlignment = VerticalAlignment.Top,
        IsHitTestVisible = false,
        IsVisible = false,
    };

    private readonly StackPanel _buttons = new() { Spacing = Spacing };

    public RailChoiceGroup()
    {
        _pill.Bind(Border.BackgroundProperty, _pill.GetResourceObservable("Xd.Accent"));
        var outline = new Border
        {
            CornerRadius = new CornerRadius(9),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2),
            Child = new Grid { Children = { _pill, _buttons } },
        };
        outline.Bind(Border.BorderBrushProperty, outline.GetResourceObservable("Xd.Border"));
        Content = outline;
        Choices.CollectionChanged += (_, _) => Rebuild();
    }

    [Content]
    public ObservableCollection<RailChoice> Choices { get; } = [];

    /// <summary>Index of the selected choice, -1 for none.</summary>
    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>Vertical in a tool window stripe (the default), horizontal in a toolbar.</summary>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>The user picked another choice (also raised when <see cref="SelectedIndex"/> is set by code).</summary>
    public event EventHandler<int>? SelectedIndexChanged;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == OrientationProperty)
        {
            _buttons.Orientation = Orientation;
            _pill.HorizontalAlignment = Orientation == Orientation.Horizontal ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;
            ApplySelection(animated: false);
        }
        else         if (change.Property == SelectedIndexProperty)
        {
            ApplySelection(animated: change.GetOldValue<int>() >= 0);
            SelectedIndexChanged?.Invoke(this, SelectedIndex);
        }
    }

    private void Rebuild()
    {
        _buttons.Children.Clear();
        for (var i = 0; i < Choices.Count; i++)
        {
            var index = i;
            var icon = new global::Avalonia.Controls.Shapes.Path { Data = Choices[i].Icon };
            icon.Classes.Add("icon");
            var button = new Button
            {
                Width = ButtonSize,
                Height = ButtonSize,
                MinWidth = 0,
                MinHeight = 0,
                Padding = default,
                BorderThickness = default,
                Background = Brushes.Transparent,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Content = icon,
            };
            button.Classes.Add("rail-choice");
            ToolTip.SetTip(button, Choices[i].ToolTip);
            button.Click += (_, _) => SelectedIndex = index;
            _buttons.Children.Add(button);
        }

        ApplySelection(animated: false);
    }

    /// <summary>The pill goes under the selected button, sliding; its place comes from the index of the button, known
    /// before the buttons are laid out.</summary>
    private void ApplySelection(bool animated)
    {
        for (var i = 0; i < _buttons.Children.Count; i++)
        {
            _buttons.Children[i].Classes.Set("selected", i == SelectedIndex);
        }

        if (SelectedIndex < 0 || SelectedIndex >= _buttons.Children.Count)
        {
            _pill.IsVisible = false;
            return;
        }

        var target = SelectedIndex * (ButtonSize + Spacing);
        if (_pill.RenderTransform is not TranslateTransform transform)
        {
            transform = new TranslateTransform();
            _pill.RenderTransform = transform;
        }

        var property = Orientation == Orientation.Horizontal ? TranslateTransform.XProperty : TranslateTransform.YProperty;
        transform.Transitions = animated && _pill.IsVisible
            ? [new DoubleTransition { Property = property, Duration = TimeSpan.FromMilliseconds(220), Easing = new CubicEaseOut() }]
            : null;
        transform.X = Orientation == Orientation.Horizontal ? target : 0;
        transform.Y = Orientation == Orientation.Horizontal ? 0 : target;
        _pill.IsVisible = true;
    }
}

/// <summary>
/// The theme choice of the app: light, dark or following the system, as a <see cref="RailChoiceGroup"/> for the
/// bottom of a tool window stripe. Picking a choice sets <c>Application.RequestedThemeVariant</c> at once (the
/// Xylocopadream theme holds both palettes); save <see cref="Variant"/> to restore it at the next start.
/// </summary>
public class ThemeSwitcher : RailChoiceGroup
{
    public static readonly StyledProperty<ThemeVariant> VariantProperty =
        AvaloniaProperty.Register<ThemeSwitcher, ThemeVariant>(nameof(Variant), ThemeVariant.Default, defaultBindingMode: BindingMode.TwoWay);

    private static readonly ThemeVariant[] Variants = [ThemeVariant.Light, ThemeVariant.Dark, ThemeVariant.Default];

    public ThemeSwitcher()
    {
        Choices.Add(new RailChoice { Icon = Find("Xd.Icon.Sun"), ToolTip = "Thème clair" });
        Choices.Add(new RailChoice { Icon = Find("Xd.Icon.Moon"), ToolTip = "Thème sombre" });
        Choices.Add(new RailChoice { Icon = Find("Xd.Icon.Monitor"), ToolTip = "Thème du système" });
        // Shows the theme the app already uses.
        Variant = global::Avalonia.Application.Current?.RequestedThemeVariant ?? ThemeVariant.Default;
        SelectedIndex = Math.Max(0, Array.IndexOf(Variants, Variant));
    }

    /// <summary>The chosen theme: Light, Dark, or Default to follow the system.</summary>
    public ThemeVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedIndexProperty && SelectedIndex >= 0)
        {
            Variant = Variants[SelectedIndex];
        }
        else if (change.Property == VariantProperty)
        {
            SelectedIndex = Math.Max(0, Array.IndexOf(Variants, Variant));
            if (global::Avalonia.Application.Current is { } app)
            {
                app.RequestedThemeVariant = Variant;
            }
        }
    }

    private static Geometry? Find(string key) =>
        global::Avalonia.Application.Current?.TryGetResource(key, null, out var value) == true ? value as Geometry : null;
}
