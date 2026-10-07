using System.Collections.ObjectModel;

using Microsoft.Maui.Controls.Shapes;

namespace Xylocopadream.UI.Maui.Controls;

/// <summary>
/// A choice of a <see cref="RailChoiceGroup"/>: its icon and tooltip.
/// </summary>
public class RailChoice
{
    public Geometry? Icon { get; set; }

    public string? ToolTip { get; set; }
}

/// <summary>
/// A few linked choices stacked in a rail (e.g. the theme: light, dark, system), the accent-colored pill
/// sliding to the selected one:
/// <code>
/// &lt;xd:RailChoiceGroup SelectedIndex="{Binding ThemeIndex}" ToolTipPlacement="Left"&gt;
///     &lt;xd:RailChoice Icon="{x:Static xd:XdIcons.Sun}" ToolTip="Light" /&gt;
///     ...
/// &lt;/xd:RailChoiceGroup&gt;
/// </code>
/// </summary>
[ContentProperty(nameof(Choices))]
public class RailChoiceGroup : ContentView
{
    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(
        nameof(SelectedIndex), typeof(int), typeof(RailChoiceGroup), -1, BindingMode.TwoWay,
        propertyChanged: (bindable, oldValue, _) => ((RailChoiceGroup)bindable).OnSelectedIndexChanged((int)oldValue));

    public static readonly BindableProperty ToolTipPlacementProperty = BindableProperty.Create(
        nameof(ToolTipPlacement), typeof(ToolTipPlacement), typeof(RailChoiceGroup), ToolTipPlacement.Left,
        propertyChanged: (bindable, _, _) => ((RailChoiceGroup)bindable).Rebuild());

    private const string PillAnimation = "RailChoicePill";
    private const double Spacing = 2;

    private readonly Border _pill;
    private readonly VerticalStackLayout _buttons;

    public RailChoiceGroup()
    {
        XdTheme.EnsureApplied();

        _pill = new Border
        {
            WidthRequest = NavRailButton.Size,
            HeightRequest = NavRailButton.Size,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Start,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            StrokeThickness = 0,
            InputTransparent = true,
            IsVisible = false,
        };
        _pill.SetDynamicResource(BackgroundColorProperty, XdKeys.Accent);

        _buttons = new VerticalStackLayout { Spacing = Spacing, HorizontalOptions = LayoutOptions.Center };

        // the rounded corners fit: group radius = pill radius (10) + gap (2) + stroke (1).
        // The outline is drawn alone, behind the buttons: as the content of a Border, they were shifted
        // to the right and cut, over its right edge
        Border outline = new()
        {
            StrokeShape = new RoundRectangle { CornerRadius = 13 },
            StrokeThickness = 1,
            BackgroundColor = Colors.Transparent,
            InputTransparent = true,
        };
        outline.SetDynamicResource(Border.StrokeProperty, XdKeys.Border);

        Content = new Grid
        {
            WidthRequest = NavRailButton.Size + 2 * (2 + 1),
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                outline,
                new Grid { Margin = 3, Children = { _pill, _buttons } },
            },
        };

        Choices.CollectionChanged += (_, _) => Rebuild();
    }

    public ObservableCollection<RailChoice> Choices { get; } = [];

    /// <summary>
    /// Index of the selected choice, -1 for none.
    /// </summary>
    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public ToolTipPlacement ToolTipPlacement
    {
        get => (ToolTipPlacement)GetValue(ToolTipPlacementProperty);
        set => SetValue(ToolTipPlacementProperty, value);
    }

    /// <summary>
    /// The user picked another choice (also raised when <see cref="SelectedIndex"/> is set by code).
    /// </summary>
    public event EventHandler<int>? SelectedIndexChanged;

    private void Rebuild()
    {
        _buttons.Children.Clear();

        for (int i = 0; i < Choices.Count; i++)
        {
            int index = i;
            NavRailButton button = new() { Icon = Choices[i].Icon, UsesActiveBackground = false };
            InstantToolTip.SetText(button, Choices[i].ToolTip);
            InstantToolTip.SetPlacement(button, ToolTipPlacement);
            button.Clicked += (_, _) => SelectedIndex = index;
            _buttons.Children.Add(button);
        }

        ApplySelection(animated: false);
    }

    private void OnSelectedIndexChanged(int oldIndex)
    {
        ApplySelection(animated: oldIndex >= 0);
        SelectedIndexChanged?.Invoke(this, SelectedIndex);
    }

    /// <summary>
    /// The pill goes under the selected button, sliding; its place comes from the index of the button,
    /// known before the buttons are laid out (at start).
    /// </summary>
    private void ApplySelection(bool animated)
    {
        for (int i = 0; i < _buttons.Children.Count; i++)
            ((NavRailButton)_buttons.Children[i]).IsActive = i == SelectedIndex;

        _pill.AbortAnimation(PillAnimation);
        if (SelectedIndex < 0 || SelectedIndex >= _buttons.Children.Count)
        {
            _pill.IsVisible = false;
            return;
        }

        double target = SelectedIndex * (NavRailButton.Size + Spacing);
        if (!animated || !_pill.IsVisible)
        {
            _pill.TranslationY = target;
            _pill.IsVisible = true;
            return;
        }

        double from = _pill.TranslationY;
        new Animation(progress => _pill.TranslationY = from + (target - from) * progress)
            .Commit(_pill, PillAnimation, length: 220, easing: Easing.CubicOut);
    }
}
