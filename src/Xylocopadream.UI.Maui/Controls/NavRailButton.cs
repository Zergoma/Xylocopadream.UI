using Microsoft.Maui.Controls.Shapes;

using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace Xylocopadream.UI.Maui.Controls;

/// <summary>
/// A button of a tool window bar (see <see cref="NavRail"/>): a flat 20 px icon, muted, brighter and shaded on hover,
/// on the accent color when <see cref="IsActive"/>. Add a tooltip with <see cref="InstantToolTip"/>.
/// </summary>
public class NavRailButton : ContentView
{
    public const double Size = 40;

    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(Geometry), typeof(NavRailButton),
        propertyChanged: (bindable, _, value) =>
        {
            NavRailButton button = (NavRailButton)bindable;
            foreach (Path path in new[] { button._path, button._hoverPath, button._activePath })
                path.Data = (Geometry?)value;
        });

    public static readonly BindableProperty IsActiveProperty = BindableProperty.Create(
        nameof(IsActive), typeof(bool), typeof(NavRailButton), false,
        propertyChanged: (bindable, _, _) => ((NavRailButton)bindable).ApplyState());

    /// <summary>
    /// False when a group draws the selection itself (a sliding pill, see <see cref="RailChoiceGroup"/>):
    /// the active button only changes its icon color.
    /// </summary>
    public static readonly BindableProperty UsesActiveBackgroundProperty = BindableProperty.Create(
        nameof(UsesActiveBackground), typeof(bool), typeof(NavRailButton), true,
        propertyChanged: (bindable, _, _) => ((NavRailButton)bindable).ApplyState());

    public event EventHandler? Clicked;

    private const double IconSize = 20;

    private readonly Path _path;
    private readonly Path _hoverPath;
    private readonly Path _activePath;
    private readonly BoxView _hoverLayer;
    private readonly BoxView _activeLayer;
    private bool _isPointerOver;

    public NavRailButton()
    {
        XdTheme.EnsureApplied();

        _path = Layers.Icon(IconSize, XdKeys.TextSecondary, isVisible: true);
        _hoverPath = Layers.Icon(IconSize, XdKeys.Text);
        _activePath = Layers.Icon(IconSize, XdKeys.AccentForeground);
        _hoverLayer = Layers.Fill(XdKeys.Hover);
        _activeLayer = Layers.Fill(XdKeys.Accent);

        Border frame = new()
        {
            WidthRequest = Size,
            HeightRequest = Size,
            StrokeThickness = 0,
            BackgroundColor = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Content = new Grid { Children = { _hoverLayer, _activeLayer, _path, _hoverPath, _activePath } },
        };

        TapGestureRecognizer tap = new();
        tap.Tapped += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);
        frame.GestureRecognizers.Add(tap);

        PointerGestureRecognizer pointer = new();
        pointer.PointerEntered += (_, _) => { _isPointerOver = true; ApplyState(); };
        pointer.PointerExited += (_, _) => { _isPointerOver = false; ApplyState(); };
        frame.GestureRecognizers.Add(pointer);

        Content = frame;
        ApplyState();
    }

    public Geometry? Icon
    {
        get => (Geometry?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public bool UsesActiveBackground
    {
        get => (bool)GetValue(UsesActiveBackgroundProperty);
        set => SetValue(UsesActiveBackgroundProperty, value);
    }

    private void ApplyState()
    {
        _activeLayer.IsVisible = IsActive && UsesActiveBackground;
        _hoverLayer.IsVisible = _isPointerOver && !IsActive;
        _activePath.IsVisible = IsActive;
        _hoverPath.IsVisible = !IsActive && _isPointerOver;
        _path.IsVisible = !IsActive && !_isPointerOver;
    }
}
