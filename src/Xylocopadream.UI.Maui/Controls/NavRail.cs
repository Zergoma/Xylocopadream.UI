using System.Collections.ObjectModel;
using System.Collections.Specialized;

using Microsoft.Maui.Controls.Shapes;

namespace Xylocopadream.UI.Maui.Controls;

/// <summary>
/// An entry of a <see cref="NavRail"/>. Its properties can change while shown (e.g. an entry that only appears
/// while a document is open, or whose icon follows a state).
/// </summary>
public class NavRailItem : BindableObject
{
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(Geometry), typeof(NavRailItem));

    public static readonly BindableProperty ToolTipProperty = BindableProperty.Create(
        nameof(ToolTip), typeof(string), typeof(NavRailItem));

    public static readonly BindableProperty IsVisibleProperty = BindableProperty.Create(
        nameof(IsVisible), typeof(bool), typeof(NavRailItem), true);

    public Geometry? Icon
    {
        get => (Geometry?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? ToolTip
    {
        get => (string?)GetValue(ToolTipProperty);
        set => SetValue(ToolTipProperty, value);
    }

    public bool IsVisible
    {
        get => (bool)GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }

    /// <summary>
    /// What the entry stands for in the app (a section, a page...): compared with <see cref="NavRail.SelectedKey"/>.
    /// </summary>
    public object? Key { get; set; }
}

/// <summary>
/// Navigation bar on an edge of the window, like the tool window bars of the JetBrains IDEs:
/// <see cref="TopItems"/> at the top, <see cref="BottomItems"/> at the bottom, the entry whose key is
/// <see cref="SelectedKey"/> on the accent color. A click raises <see cref="ItemClicked"/>; the app decides what to show.
/// The tooltips need an <see cref="InstantToolTipHost"/> in the page.
/// </summary>
[ContentProperty(nameof(BottomItems))]
public class NavRail : ContentView
{
    public const double RailWidth = 60;

    public static readonly BindableProperty SelectedKeyProperty = BindableProperty.Create(
        nameof(SelectedKey), typeof(object), typeof(NavRail),
        propertyChanged: (bindable, _, _) => ((NavRail)bindable).ApplySelection());

    public static readonly BindableProperty ToolTipPlacementProperty = BindableProperty.Create(
        nameof(ToolTipPlacement), typeof(ToolTipPlacement), typeof(NavRail), ToolTipPlacement.Right,
        propertyChanged: (bindable, _, _) => ((NavRail)bindable).Rebuild());

    private readonly VerticalStackLayout _top;
    private readonly VerticalStackLayout _bottom;
    private readonly List<(NavRailItem Item, NavRailButton Button)> _buttons = [];

    public NavRail()
    {
        XdTheme.EnsureApplied();

        _top = new VerticalStackLayout
        {
            Spacing = 6,
            VerticalOptions = LayoutOptions.Start,
            Padding = new Thickness(10, 14, 10, 0),
        };

        _bottom = new VerticalStackLayout
        {
            Spacing = 6,
            VerticalOptions = LayoutOptions.End,
            Padding = new Thickness(10, 0, 10, 14),
        };

        Grid rail = new() { WidthRequest = RailWidth, Children = { _top, _bottom } };
        rail.SetDynamicResource(BackgroundColorProperty, XdKeys.PanelBackground);
        Content = rail;

        TopItems.CollectionChanged += OnItemsChanged;
        BottomItems.CollectionChanged += OnItemsChanged;
    }

    public ObservableCollection<NavRailItem> TopItems { get; } = [];

    public ObservableCollection<NavRailItem> BottomItems { get; } = [];

    public object? SelectedKey
    {
        get => GetValue(SelectedKeyProperty);
        set => SetValue(SelectedKeyProperty, value);
    }

    /// <summary>
    /// Right for a rail on the left edge, Left for one on the right edge.
    /// </summary>
    public ToolTipPlacement ToolTipPlacement
    {
        get => (ToolTipPlacement)GetValue(ToolTipPlacementProperty);
        set => SetValue(ToolTipPlacementProperty, value);
    }

    public event EventHandler<NavRailItem>? ItemClicked;

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => Rebuild();

    private void Rebuild()
    {
        foreach (var (item, _) in _buttons)
            item.PropertyChanged -= OnItemPropertyChanged;

        _buttons.Clear();
        _top.Children.Clear();
        _bottom.Children.Clear();

        foreach (NavRailItem item in TopItems)
            _top.Children.Add(CreateButton(item));
        foreach (NavRailItem item in BottomItems)
            _bottom.Children.Add(CreateButton(item));

        ApplySelection();
    }

    private NavRailButton CreateButton(NavRailItem item)
    {
        NavRailButton button = new();
        InstantToolTip.SetPlacement(button, ToolTipPlacement);
        button.Clicked += (_, _) => ItemClicked?.Invoke(this, item);
        item.PropertyChanged += OnItemPropertyChanged;

        _buttons.Add((item, button));
        Copy(item, button);
        return button;
    }

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        foreach (var (item, button) in _buttons)
        {
            if (item == sender)
                Copy(item, button);
        }
    }

    private static void Copy(NavRailItem item, NavRailButton button)
    {
        button.Icon = item.Icon;
        button.IsVisible = item.IsVisible;
        InstantToolTip.SetText(button, item.ToolTip);
    }

    private void ApplySelection()
    {
        foreach (var (item, button) in _buttons)
            button.IsActive = item.Key is not null && Equals(item.Key, SelectedKey);
    }
}
