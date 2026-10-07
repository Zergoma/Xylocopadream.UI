using System.Windows.Input;

using Microsoft.Maui.Controls.Shapes;

using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace Xylocopadream.UI.Maui.Controls;

/// <summary>
/// A small round button showing a vector icon (see <see cref="XdIcons"/>), on the neutral button color, shaded on hover:
/// <c>&lt;xd:IconButton Icon="{x:Static xd:XdIcons.Shuffle}" Command="{Binding ShuffleCommand}" /&gt;</c>.
/// <see cref="IsActive"/> makes it a toggle, on the accent color when on. Disabled, it fades.
/// </summary>
public class IconButton : ContentView
{
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(Geometry), typeof(IconButton),
        propertyChanged: (bindable, _, value) =>
        {
            IconButton button = (IconButton)bindable;
            button._path.Data = (Geometry?)value;
            button._activePath.Data = (Geometry?)value;
        });

    /// <summary>
    /// Toggle buttons: highlighted with the accent color when on.
    /// </summary>
    public static readonly BindableProperty IsActiveProperty = BindableProperty.Create(
        nameof(IsActive), typeof(bool), typeof(IconButton), false,
        propertyChanged: (bindable, _, _) => ((IconButton)bindable).ApplyState());

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(IconButton));

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(IconButton));

    public event EventHandler? Clicked;

    private const double Size = 32;
    private const double IconSize = 14;

    private readonly Path _path;
    private readonly Path _activePath;
    private readonly BoxView _normalLayer;
    private readonly BoxView _hoverLayer;
    private readonly BoxView _activeLayer;
    private readonly BoxView _activeHoverLayer;
    private bool _isPointerOver;

    public IconButton()
    {
        XdTheme.EnsureApplied();

        // the icon is white on the neutral color in both themes, like the text of the neutral buttons
        _path = Layers.Icon(IconSize, colorKey: null, isVisible: true);
        _path.Fill = Colors.White;
        _activePath = Layers.Icon(IconSize, XdKeys.AccentForeground);

        _normalLayer = Layers.Fill(XdKeys.NeutralButton, isVisible: true);
        _hoverLayer = Layers.Fill(XdKeys.NeutralButtonHover);
        _activeLayer = Layers.Fill(XdKeys.Accent);
        _activeHoverLayer = Layers.Fill(XdKeys.AccentHover);

        Border circle = new()
        {
            WidthRequest = Size,
            HeightRequest = Size,
            StrokeThickness = 0,
            BackgroundColor = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = Size / 2 },
            Content = new Grid { Children = { _normalLayer, _hoverLayer, _activeLayer, _activeHoverLayer, _path, _activePath } },
        };

        TapGestureRecognizer tap = new();
        tap.Tapped += (_, _) =>
        {
            if (!IsEnabled)
                return;

            Clicked?.Invoke(this, EventArgs.Empty);
            if (Command?.CanExecute(CommandParameter) == true)
                Command.Execute(CommandParameter);
        };
        circle.GestureRecognizers.Add(tap);

        PointerGestureRecognizer pointer = new();
        pointer.PointerEntered += (_, _) => { _isPointerOver = true; ApplyState(); };
        pointer.PointerExited += (_, _) => { _isPointerOver = false; ApplyState(); };
        circle.GestureRecognizers.Add(pointer);

        Content = circle;
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

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        // disabled: faded and no hover
        if (propertyName == nameof(IsEnabled))
        {
            Opacity = IsEnabled ? 1 : 0.35;
            ApplyState();
        }
    }

    private void ApplyState()
    {
        bool hover = _isPointerOver && IsEnabled;

        _normalLayer.IsVisible = !IsActive && !hover;
        _hoverLayer.IsVisible = !IsActive && hover;
        _activeLayer.IsVisible = IsActive && !hover;
        _activeHoverLayer.IsVisible = IsActive && hover;
        _activePath.IsVisible = IsActive;
        _path.IsVisible = !IsActive;
    }
}
