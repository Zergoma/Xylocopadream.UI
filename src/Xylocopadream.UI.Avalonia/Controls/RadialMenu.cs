using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;

namespace Xylocopadream.UI.Avalonia.Controls;

/// <summary>An action of a <see cref="RadialMenu"/>.</summary>
public sealed class RadialMenuItem
{
    public required string Header { get; init; }

    public Geometry? Icon { get; init; }

    /// <summary>Run when the item is clicked (before <see cref="Command"/>).</summary>
    public Action? Action { get; init; }

    public ICommand? Command { get; init; }

    public object? CommandParameter { get; init; }

    /// <summary>Its own background, e.g. a translucent red for a dangerous action.</summary>
    public IBrush? Background { get; init; }

    public bool IsEnabled { get; init; } = true;
}

/// <summary>Actions shown together in one bubble of a <see cref="RadialMenu"/>, under an optional title.</summary>
public sealed class RadialMenuGroup
{
    public string? Title { get; init; }

    /// <summary>Background of the bubble (transparency allowed); the popup background by default.</summary>
    public IBrush? Background { get; init; }

    public IList<RadialMenuItem> Items { get; init; } = [];
}

/// <summary>Placement and behavior of a <see cref="RadialMenu"/>.</summary>
public sealed class RadialMenuOptions
{
    /// <summary>Smallest radius of the circle; it grows until the bubbles do not overlap.</summary>
    public double MinRadius { get; init; } = 80;

    /// <summary>Space between bubbles.</summary>
    public double Gap { get; init; } = 8;

    /// <summary>Space kept between the click (or <see cref="Avoid"/>) and the bubbles.</summary>
    public double Margin { get; init; } = 16;

    /// <summary>An area to keep visible (the selection), in the coordinates of the control the menu is shown for:
    /// the circle goes beside it, not over it.</summary>
    public Rect? Avoid { get; init; }

    /// <summary>Highlights the bubble the pointer points to, seen from the click.</summary>
    public bool HighlightByAngle { get; init; } = true;

    /// <summary>With <see cref="HighlightByAngle"/>: a translucent shape joining the click (or <see cref="Avoid"/>) to
    /// the highlighted bubble.</summary>
    public bool ShowConnector { get; init; } = true;

    /// <summary>Fill of the connector; the accent color by default.</summary>
    public IBrush? ConnectorBrush { get; init; }

    public double ConnectorOpacity { get; init; } = 0.15;

    public TimeSpan AnimationDuration { get; init; } = TimeSpan.FromMilliseconds(160);
}

/// <summary>
/// A context menu whose groups of actions are bubbles laid on a circle beside the click, appearing from it.
/// Shown above the window (its overlay layer); a click outside or Escape closes it:
/// <code>RadialMenu.Show(noteBox, e.GetPosition(noteBox), [new RadialMenuGroup { Title = "Édition", Items = [...] }]);</code>
/// </summary>
public sealed class RadialMenu
{
    private readonly Canvas _layer = new() { Background = Brushes.Transparent };
    private readonly Polygon _connector = new() { IsHitTestVisible = false, IsVisible = false };
    private readonly List<(Border Bubble, Rect Bounds, double Angle)> _bubbles = [];
    private readonly RadialMenuOptions _options;
    private readonly Panel _overlay;
    private readonly TopLevel? _topLevel;
    private Point _origin;
    private Rect _avoid;
    private Border? _highlighted;
    private bool _closing;

    private RadialMenu(Panel overlay, RadialMenuOptions options)
    {
        _overlay = overlay;
        _options = options;
        _topLevel = TopLevel.GetTopLevel(overlay);
    }

    /// <summary>Raised once the menu is closed (after an action, a click outside or Escape).</summary>
    public event EventHandler? Closed;

    public bool IsOpen => !_closing && _layer.Parent is not null;

    /// <summary>Bubbles shown, one per group, with their place in the overlay (for tests).</summary>
    public IReadOnlyList<(Border Bubble, Rect Bounds, double Angle)> Bubbles => _bubbles;

    /// <summary>The highlighted bubble, if any.</summary>
    public Border? Highlighted => _highlighted;

    /// <param name="relativeTo">The control clicked: <paramref name="at"/> and <see cref="RadialMenuOptions.Avoid"/>
    /// are in its coordinates.</param>
    /// <returns>The open menu, or null when the control is not in a window.</returns>
    public static RadialMenu? Show(Visual relativeTo, Point at, IEnumerable<RadialMenuGroup> groups, RadialMenuOptions? options = null)
    {
        if (OverlayLayer.GetOverlayLayer(relativeTo) is not { } overlay)
        {
            return null;
        }

        var menu = new RadialMenu(overlay, options ?? new RadialMenuOptions());
        menu.Open(relativeTo, at, groups.Where(g => g.Items.Count > 0).ToList());
        return menu;
    }

    /// <summary>Closes with the disappearing animation.</summary>
    public void Close()
    {
        if (_closing)
        {
            return;
        }

        _closing = true;
        _topLevel?.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
        _connector.IsVisible = false;
        foreach (var (bubble, bounds, _) in _bubbles)
        {
            bubble.Opacity = 0;
            bubble.RenderTransform = FromOrigin(bounds);
        }

        DispatcherTimer.RunOnce(
            () =>
            {
                _overlay.Children.Remove(_layer);
                Closed?.Invoke(this, EventArgs.Empty);
            },
            _options.AnimationDuration);
    }

    /// <summary>Runs an item as a click does (for keyboard use and tests).</summary>
    public void Invoke(RadialMenuItem item)
    {
        if (!item.IsEnabled)
        {
            return;
        }

        Close();
        item.Action?.Invoke();
        if (item.Command is { } command && command.CanExecute(item.CommandParameter))
        {
            command.Execute(item.CommandParameter);
        }
    }

    /// <summary>Updates the highlight as a pointer at <paramref name="point"/> (overlay coordinates) does.</summary>
    public void PointTo(Point point)
    {
        if (!_options.HighlightByAngle || _bubbles.Count == 0)
        {
            return;
        }

        var vector = point - _origin;
        Border? target = null;
        if (Math.Sqrt((vector.X * vector.X) + (vector.Y * vector.Y)) > 12)
        {
            var angle = Math.Atan2(vector.Y, vector.X);
            target = _bubbles.MinBy(b => AngleBetween(b.Angle, angle)).Bubble;
        }

        if (target == _highlighted)
        {
            return;
        }

        _highlighted?.Classes.Remove("xd-radial-highlight");
        _highlighted = target;
        _highlighted?.Classes.Add("xd-radial-highlight");
        UpdateConnector();
    }

    // ------------------------------------------------------------------ opening

    private void Open(Visual relativeTo, Point at, IReadOnlyList<RadialMenuGroup> groups)
    {
        var size = _topLevel?.ClientSize ?? _overlay.Bounds.Size;
        _layer.Width = size.Width;
        _layer.Height = size.Height;
        _origin = relativeTo.TranslatePoint(at, _overlay) ?? at;
        _avoid = _options.Avoid is { } avoid && relativeTo.TranslatePoint(avoid.TopLeft, _overlay) is { } topLeft
            ? new Rect(topLeft, avoid.Size).Union(new Rect(_origin, new Size(1, 1)))
            : new Rect(_origin, new Size(1, 1));

        _connector.Fill = _options.ConnectorBrush ?? (_overlay.TryFindResource("Xd.Accent", out var accent) ? accent as IBrush : Brushes.SteelBlue);
        _connector.Opacity = _options.ConnectorOpacity;
        _layer.Children.Add(_connector);

        // Measured once in the window, with their styles and templates.
        _overlay.Children.Add(_layer);
        var bubbles = groups.Select(Bubble).ToList();
        foreach (var bubble in bubbles)
        {
            _layer.Children.Add(bubble);
            bubble.ApplyStyling();
            bubble.Measure(Size.Infinity);
        }

        Place(bubbles);
        _layer.PointerPressed += (_, e) =>
        {
            if (e.Source == _layer)
            {
                Close();
            }
        };
        _layer.PointerMoved += (_, e) => PointTo(e.GetPosition(_layer));
        _topLevel?.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

        // Appearing: from the click to their place.
        Dispatcher.UIThread.Post(
            () =>
            {
                foreach (var (bubble, _, _) in _bubbles)
                {
                    bubble.Opacity = 1;
                    bubble.RenderTransform = TransformOperations.Identity;
                }
            },
            DispatcherPriority.Render);
    }

    private Border Bubble(RadialMenuGroup group)
    {
        var content = new StackPanel { Spacing = 1 };
        if (!string.IsNullOrEmpty(group.Title))
        {
            var title = new TextBlock { Text = group.Title, Margin = new Thickness(6, 2, 6, 3) };
            title.Classes.Add("secondary");
            title.Classes.Add("small");
            content.Children.Add(title);
        }

        foreach (var item in group.Items)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            if (item.Icon is { } icon)
            {
                var path = new global::Avalonia.Controls.Shapes.Path { Data = icon };
                path.Classes.Add("icon");
                row.Children.Add(path);
            }

            row.Children.Add(new TextBlock { Text = item.Header, VerticalAlignment = VerticalAlignment.Center });
            var button = new Button
            {
                Content = row,
                IsEnabled = item.IsEnabled,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Tag = item,
            };
            button.Classes.Add("tool");
            button.Classes.Add("xd-radial-item");
            if (item.Background is { } background)
            {
                button.Background = background;
            }

            button.Click += (_, _) => Invoke(item);
            content.Children.Add(button);
        }

        var bubble = new Border
        {
            Child = content,
            Padding = new Thickness(4),
            CornerRadius = new CornerRadius(8),
            BoxShadow = BoxShadows.Parse("0 4 14 0 #50000000"),
            Opacity = 0,
            Tag = group,
        };
        bubble.Classes.Add("xd-radial-bubble");
        // Default background and border come from the theme styles (Border.xd-radial-bubble), so the highlight style applies.
        if (group.Background is { } groupBackground)
        {
            bubble.Background = groupBackground;
        }

        bubble.Transitions =
        [
            new DoubleTransition { Property = Visual.OpacityProperty, Duration = _options.AnimationDuration },
            new TransformOperationsTransition { Property = Visual.RenderTransformProperty, Duration = _options.AnimationDuration, Easing = new CubicEaseOut() },
        ];
        return bubble;
    }

    /// <summary>
    /// Bubbles in turn on an arc of the circle, the arc facing the click; the circle beside the click and
    /// <see cref="RadialMenuOptions.Avoid"/> (to their right, or to their left when there is no room).
    /// </summary>
    private void Place(IReadOnlyList<Border> bubbles)
    {
        var sizes = bubbles.Select(b => b.DesiredSize).ToList();
        var reach = sizes.Select(s => Math.Sqrt((s.Width * s.Width) + (s.Height * s.Height)) / 2).ToList();
        var maxHalfWidth = sizes.Count == 0 ? 0 : sizes.Max(s => s.Width) / 2;

        // Grow the circle until the bubbles fit in 300° without overlapping.
        var radius = Math.Max(_options.MinRadius, reach.DefaultIfEmpty(0).Max() + _options.Gap);
        double[] steps;
        while (true)
        {
            steps = Enumerable.Range(1, Math.Max(0, bubbles.Count - 1))
                .Select(i => 2 * Math.Asin(Math.Min(1, (reach[i - 1] + reach[i] + _options.Gap) / (2 * radius))))
                .ToArray();
            if (steps.Sum() <= Math.PI * 300 / 180 || radius > 2000)
            {
                break;
            }

            radius *= 1.2;
        }

        var span = _options.Margin + (2 * radius) + (2 * maxHalfWidth);
        var toRight = _avoid.Right + span <= _layer.Width || _avoid.Left - span < 0;
        var center = toRight
            ? new Point(_avoid.Right + _options.Margin + radius + maxHalfWidth, _origin.Y)
            : new Point(_avoid.Left - _options.Margin - radius - maxHalfWidth, _origin.Y);
        var facing = toRight ? Math.PI : 0;
        var angle = facing - (steps.Sum() / 2);

        for (var i = 0; i < bubbles.Count; i++)
        {
            if (i > 0)
            {
                angle += steps[i - 1];
            }

            var size = sizes[i];
            var x = Math.Clamp(center.X + (radius * Math.Cos(angle)) - (size.Width / 2), 4, Math.Max(4, _layer.Width - size.Width - 4));
            var y = Math.Clamp(center.Y + (radius * Math.Sin(angle)) - (size.Height / 2), 4, Math.Max(4, _layer.Height - size.Height - 4));
            var bounds = new Rect(new Point(x, y), size);
            Canvas.SetLeft(bubbles[i], x);
            Canvas.SetTop(bubbles[i], y);
            bubbles[i].RenderTransform = FromOrigin(bounds);
            _bubbles.Add((bubbles[i], bounds, Math.Atan2(bounds.Center.Y - _origin.Y, bounds.Center.X - _origin.X)));
        }
    }

    /// <summary>Drawn at the click, small: the start of the appearing animation and the end of the closing one.</summary>
    private ITransform FromOrigin(Rect bounds)
    {
        var offset = _origin - bounds.Center;
        return TransformOperations.Parse(FormattableString.Invariant($"translate({offset.X}px, {offset.Y}px) scale(0.3)"));
    }

    private void UpdateConnector()
    {
        if (!_options.ShowConnector || _highlighted is null || _bubbles.FirstOrDefault(b => b.Bubble == _highlighted) is not { Bubble: not null } entry)
        {
            _connector.IsVisible = false;
            return;
        }

        _connector.Points = ConvexHull(Corners(_avoid).Concat(Corners(entry.Bounds)).ToList());
        _connector.IsVisible = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private static IEnumerable<Point> Corners(Rect rect) => [rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft];

    private static double AngleBetween(double a, double b)
    {
        var difference = Math.Abs(a - b) % (2 * Math.PI);
        return difference > Math.PI ? (2 * Math.PI) - difference : difference;
    }

    /// <summary>Andrew's monotone chain: the smallest convex polygon around the points.</summary>
    private static List<Point> ConvexHull(List<Point> points)
    {
        var sorted = points.Distinct().OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
        if (sorted.Count < 3)
        {
            return sorted;
        }

        var hull = new List<Point>();
        foreach (var pass in new[] { sorted, Enumerable.Reverse(sorted).ToList() })
        {
            var start = hull.Count;
            foreach (var p in pass)
            {
                while (hull.Count >= start + 2 && Cross(hull[^2], hull[^1], p) <= 0)
                {
                    hull.RemoveAt(hull.Count - 1);
                }

                hull.Add(p);
            }

            hull.RemoveAt(hull.Count - 1);
        }

        return hull;

        static double Cross(Point o, Point a, Point b) => ((a.X - o.X) * (b.Y - o.Y)) - ((a.Y - o.Y) * (b.X - o.X));
    }
}
