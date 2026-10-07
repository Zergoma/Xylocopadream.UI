using System.Collections;

using Microsoft.Maui.Controls.Shapes;

namespace Xylocopadream.UI.Maui.Controls;

/// <summary>
/// A choice among a few options shown side by side (wrapping when there is not enough room).
/// The accent-colored pill slides to the selected option. Each option shows its <c>ToString()</c>.
/// </summary>
public class SegmentedControl : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(SegmentedControl),
        propertyChanged: (bindable, _, _) => ((SegmentedControl)bindable).BuildSegments());

    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(
        nameof(SelectedItem), typeof(object), typeof(SegmentedControl), defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: (bindable, _, _) => ((SegmentedControl)bindable).OnSelectionChanged(animated: true));

    private const string MoveAnimation = "MoveIndicator";
    private const uint MoveDuration = 220;

    private readonly WrapPanel _segments;
    private readonly Border _indicator;
    // each option has two texts, normal and on the accent color when selected, and a hover layer (see Layers)
    private readonly List<(object Item, Grid Segment, BoxView Hover, Label Text, Label SelectedText)> _items = [];

    public SegmentedControl()
    {
        XdTheme.EnsureApplied();

        _indicator = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 15 },
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            InputTransparent = true,
            Opacity = 0,
        };
        _indicator.SetDynamicResource(BackgroundColorProperty, XdKeys.Accent);

        _segments = new WrapPanel();
        _segments.SizeChanged += (_, _) => OnSelectionChanged(animated: false);

        Border frame = new()
        {
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            StrokeThickness = 1,
            Padding = 3,
            HorizontalOptions = LayoutOptions.Start,
            Content = new Grid { Children = { _indicator, _segments } },
        };
        frame.SetDynamicResource(Border.StrokeProperty, XdKeys.Border);

        Content = frame;
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    private void BuildSegments()
    {
        _segments.Children.Clear();
        _items.Clear();

        if (ItemsSource is null)
            return;

        foreach (object item in ItemsSource)
        {
            Label text = Layers.Text(XdKeys.Text, isVisible: true);
            text.Text = item.ToString();

            Label selectedText = Layers.Text(XdKeys.AccentForeground);
            selectedText.Text = item.ToString();

            // the hover layer is a rounded box behind the padded texts (a flex layout measured texts with margins badly)
            BoxView hover = Layers.Fill(XdKeys.Hover);
            hover.CornerRadius = 15;

            Grid segment = new()
            {
                Children =
                {
                    hover,
                    new Border
                    {
                        StrokeThickness = 0,
                        Padding = new Thickness(16, 6),
                        BackgroundColor = Colors.Transparent,
                        Content = new Grid { Children = { text, selectedText } },
                    },
                },
            };

            TapGestureRecognizer tap = new();
            tap.Tapped += (_, _) => SelectedItem = item;
            segment.GestureRecognizers.Add(tap);

            // hover: a light shade on the options that are not selected
            PointerGestureRecognizer pointer = new();
            pointer.PointerEntered += (_, _) => hover.IsVisible = !Equals(item, SelectedItem);
            pointer.PointerExited += (_, _) => hover.IsVisible = false;
            segment.GestureRecognizers.Add(pointer);

            segment.SizeChanged += (_, _) => OnSelectionChanged(animated: false);

            _segments.Children.Add(segment);
            _items.Add((item, segment, hover, text, selectedText));
        }

        OnSelectionChanged(animated: false);
    }

    private void OnSelectionChanged(bool animated)
    {
        foreach (var (item, _, hover, text, selectedText) in _items)
        {
            bool isSelected = Equals(item, SelectedItem);
            text.IsVisible = !isSelected;
            selectedText.IsVisible = isSelected;
            if (isSelected)
                hover.IsVisible = false;
        }

        var selected = _items.FirstOrDefault(i => Equals(i.Item, SelectedItem));
        if (selected.Segment is null || selected.Segment.Width <= 0)
        {
            _indicator.Opacity = 0;
            return;
        }

        Rect target = selected.Segment.Frame;
        _indicator.AbortAnimation(MoveAnimation);

        // first placement (or a new layout): no slide
        if (!animated || _indicator.Opacity == 0)
        {
            Place(target.X, target.Y, target.Width, target.Height);
            _indicator.Opacity = 1;
            return;
        }

        double fromX = _indicator.TranslationX, fromY = _indicator.TranslationY;
        double fromWidth = _indicator.WidthRequest, fromHeight = _indicator.HeightRequest;

        new Animation(progress => Place(
                fromX + (target.X - fromX) * progress,
                fromY + (target.Y - fromY) * progress,
                fromWidth + (target.Width - fromWidth) * progress,
                fromHeight + (target.Height - fromHeight) * progress))
            .Commit(_indicator, MoveAnimation, length: MoveDuration, easing: Easing.CubicOut);
    }

    private void Place(double x, double y, double width, double height)
    {
        _indicator.TranslationX = x;
        _indicator.TranslationY = y;
        _indicator.WidthRequest = width;
        _indicator.HeightRequest = height;
    }
}
