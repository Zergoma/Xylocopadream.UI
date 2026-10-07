using System;
using System.Globalization;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Transformation;

namespace Xylocopadream.UI.Avalonia.Controls;

/// <summary>
/// Overview of the whole image with a frame showing the visible part. Sits in a bottom corner of its parent;
/// a double click slides it (animated) to the opposite corner, and dragging inside it moves the view.
/// </summary>
public sealed class Minimap : Control
{
    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<Minimap, IImage?>(nameof(Source));

    /// <summary>Visible part of the image, in image coordinates.</summary>
    public static readonly StyledProperty<Rect> ViewportProperty =
        AvaloniaProperty.Register<Minimap, Rect>(nameof(Viewport));

    private const double Padding = 4;
    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromArgb(200, 20, 20, 20));
    private static readonly IBrush FrameFill = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
    private static readonly IBrush OutsideShade = new SolidColorBrush(Color.FromArgb(110, 0, 0, 0));
    private static readonly IPen BorderPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), 1);
    private static readonly IPen FramePen = new Pen(new SolidColorBrush(Color.FromRgb(255, 196, 0)), 2);

    private bool _onLeft;
    private Control? _host;
    private Point? _dragStart;
    private bool _dragging;

    static Minimap()
    {
        AffectsRender<Minimap>(SourceProperty, ViewportProperty);
    }

    public Minimap()
    {
        HorizontalAlignment = HorizontalAlignment.Right;
        VerticalAlignment = VerticalAlignment.Bottom;
        Transitions =
        [
            new TransformOperationsTransition
            {
                Property = RenderTransformProperty,
                Duration = TimeSpan.FromMilliseconds(380),
                Easing = new CubicEaseInOut(),
            },
        ];
        RenderTransform = TransformOperations.Parse("translateX(0px)");
    }

    public IImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public Rect Viewport
    {
        get => GetValue(ViewportProperty);
        set => SetValue(ViewportProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.DrawRectangle(BackgroundBrush, BorderPen, bounds, 6, 6);
        if (Source is not { } source || ImageArea() is not { } image)
        {
            return;
        }

        var scale = image.Width / source.Size.Width;
        using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.MediumQuality }))
        {
            context.DrawImage(source, new Rect(source.Size), image);
        }

        var frame = new Rect(
            image.X + (Viewport.X * scale),
            image.Y + (Viewport.Y * scale),
            Viewport.Width * scale,
            Viewport.Height * scale).Intersect(image);

        // Shade what is outside the visible part, then outline it.
        using (context.PushGeometryClip(new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(image), new RectangleGeometry(frame))))
        {
            context.FillRectangle(OutsideShade, image);
        }

        context.DrawRectangle(FrameFill, FramePen, frame);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _host = Parent as Control;
        if (_host is not null)
        {
            _host.PropertyChanged += OnHostPropertyChanged;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_host is not null)
        {
            _host.PropertyChanged -= OnHostPropertyChanged;
            _host = null;
        }
    }

    /// <summary>Raised while the user drags inside the minimap: the image point to center the view on.</summary>
    public event EventHandler<Point>? NavigateRequested;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        e.Handled = true;
        if (e.ClickCount == 2)
        {
            // Double click: move to the opposite corner.
            _dragStart = null;
            _onLeft = !_onLeft;
            UpdatePosition();
            return;
        }

        _dragStart = e.GetPosition(this);
        e.Pointer.Capture(this);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragStart is not { } start)
        {
            return;
        }

        // Navigation only once the pointer really moved, so a double click never jumps the view.
        var position = e.GetPosition(this);
        if (!_dragging && Math.Abs(position.X - start.X) + Math.Abs(position.Y - start.Y) < 3)
        {
            return;
        }

        _dragging = true;
        if (ToImagePoint(position) is { } imagePoint)
        {
            NavigateRequested?.Invoke(this, imagePoint);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _dragStart = null;
        _dragging = false;
        e.Pointer.Capture(null);
    }

    /// <summary>Converts a point of the minimap to image coordinates (null without image).</summary>
    private Point? ToImagePoint(Point point)
    {
        if (ImageArea() is not { } area || Source is not { } source)
        {
            return null;
        }

        var x = Math.Clamp((point.X - area.X) / area.Width, 0, 1) * source.Size.Width;
        var y = Math.Clamp((point.Y - area.Y) / area.Height, 0, 1) * source.Size.Height;
        return new Point(x, y);
    }

    /// <summary>Where the whole image is drawn inside the minimap (uniform fit).</summary>
    private Rect? ImageArea()
    {
        if (Source is not { } source || source.Size.Width <= 0 || source.Size.Height <= 0)
        {
            return null;
        }

        var area = new Rect(Bounds.Size).Deflate(Padding);
        var scale = Math.Min(area.Width / source.Size.Width, area.Height / source.Size.Height);
        var size = source.Size * scale;
        return new Rect(area.X + ((area.Width - size.Width) / 2), area.Y + ((area.Height - size.Height) / 2), size.Width, size.Height);
    }

    private void OnHostPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty && _onLeft)
        {
            UpdatePosition();
        }
    }

    /// <summary>Right-aligned by layout; the left position is a translation by the free width of the host.</summary>
    private void UpdatePosition()
    {
        var distance = 0.0;
        if (_onLeft && _host is not null)
        {
            distance = _host.Bounds.Width - Bounds.Width - Margin.Left - Margin.Right;
        }

        // Invariant culture: "12,5px" (French formatting) would not parse.
        RenderTransform = TransformOperations.Parse(
            string.Create(CultureInfo.InvariantCulture, $"translateX({-Math.Max(0, distance):0.##}px)"));
    }
}
