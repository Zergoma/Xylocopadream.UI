using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Xylocopadream.UI.Avalonia.Controls;

/// <summary>
/// Displays an image (or video frame) that can be zoomed around the cursor with the wheel, panned by dragging,
/// fitted to the view or shown at 100 %. Exposes the visible part of the image for a minimap.
/// </summary>
public sealed class ZoomPanViewer : Control
{
    public const double MaxZoom = 32;
    private const double ZoomStep = 1.25;

    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<ZoomPanViewer, IImage?>(nameof(Source));

    /// <summary>When fitting, also enlarge images smaller than the view (useful for videos).</summary>
    public static readonly StyledProperty<bool> UpscaleOnFitProperty =
        AvaloniaProperty.Register<ZoomPanViewer, bool>(nameof(UpscaleOnFit));

    public static readonly DirectProperty<ZoomPanViewer, double> ZoomProperty =
        AvaloniaProperty.RegisterDirect<ZoomPanViewer, double>(nameof(Zoom), v => v.Zoom);

    public static readonly DirectProperty<ZoomPanViewer, Rect> VisibleRectProperty =
        AvaloniaProperty.RegisterDirect<ZoomPanViewer, Rect>(nameof(VisibleRect), v => v.VisibleRect);

    public static readonly DirectProperty<ZoomPanViewer, bool> IsZoomedInProperty =
        AvaloniaProperty.RegisterDirect<ZoomPanViewer, bool>(nameof(IsZoomedIn), v => v.IsZoomedIn);

    private double _zoom = 1;
    private Rect _visibleRect;
    private bool _isZoomedIn;
    private Vector _offset;
    private bool _fitMode = true;
    private Size _lastSourceSize;
    private Point? _dragStart;
    private Vector _dragStartOffset;

    static ZoomPanViewer()
    {
        AffectsRender<ZoomPanViewer>(SourceProperty);
        FocusableProperty.OverrideDefaultValue<ZoomPanViewer>(true);
        ClipToBoundsProperty.OverrideDefaultValue<ZoomPanViewer>(true);
    }

    public IImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public bool UpscaleOnFit
    {
        get => GetValue(UpscaleOnFitProperty);
        set => SetValue(UpscaleOnFitProperty, value);
    }

    /// <summary>Current scale: 1 = one image pixel per device-independent pixel.</summary>
    public double Zoom
    {
        get => _zoom;
        private set => SetAndRaise(ZoomProperty, ref _zoom, value);
    }

    /// <summary>Part of the image currently visible, in image coordinates.</summary>
    public Rect VisibleRect
    {
        get => _visibleRect;
        private set => SetAndRaise(VisibleRectProperty, ref _visibleRect, value);
    }

    /// <summary>True when the image is larger than the view (part of it is hidden).</summary>
    public bool IsZoomedIn
    {
        get => _isZoomedIn;
        private set => SetAndRaise(IsZoomedInProperty, ref _isZoomedIn, value);
    }

    private Size SourceSize => Source?.Size ?? default;

    public void ZoomIn() => ZoomAt(Zoom * ZoomStep, ViewCenter);

    public void ZoomOut() => ZoomAt(Zoom / ZoomStep, ViewCenter);

    public void ActualSize() => ZoomAt(1, ViewCenter);

    public void Fit()
    {
        _fitMode = true;
        ApplyFit();
    }

    /// <summary>Scrolls (keeping the zoom) so that <paramref name="imagePoint"/> is at the center of the view.</summary>
    public void CenterOn(Point imagePoint)
    {
        if (Source is null)
        {
            return;
        }

        _offset = new Vector(ViewCenter.X - (imagePoint.X * Zoom), ViewCenter.Y - (imagePoint.Y * Zoom));
        _fitMode = false;
        ClampAndUpdate();
    }

    /// <summary>Redraws after the pixels of <see cref="Source"/> changed in place (video frames).</summary>
    public void Refresh() => InvalidateVisual();

    public override void Render(DrawingContext context)
    {
        // Transparent fill so the whole area receives pointer input.
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));
        if (Source is not { } source)
        {
            return;
        }

        var destination = new Rect(new Point(_offset.X, _offset.Y), SourceSize * Zoom);
        var interpolation = Zoom >= 3 ? BitmapInterpolationMode.None : BitmapInterpolationMode.HighQuality;
        using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = interpolation }))
        {
            context.DrawImage(source, new Rect(SourceSize), destination);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SourceProperty)
        {
            // Same-size replacements (next video frame buffer) keep the current view.
            if (SourceSize != _lastSourceSize)
            {
                _lastSourceSize = SourceSize;
                Fit();
            }
        }
        else if (change.Property == BoundsProperty)
        {
            if (_fitMode)
            {
                ApplyFit();
            }
            else
            {
                ClampAndUpdate();
            }
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (Source is null)
        {
            return;
        }

        ZoomAt(Zoom * Math.Pow(1.2, e.Delta.Y), e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        if (Source is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            // Double click: 100 % on the clicked point, or back to fit.
            if (_fitMode && Math.Abs(Zoom - 1) > 0.01)
            {
                ZoomAt(1, e.GetPosition(this));
            }
            else
            {
                Fit();
            }

            e.Handled = true;
            return;
        }

        if (IsZoomedIn)
        {
            _dragStart = e.GetPosition(this);
            _dragStartOffset = _offset;
            e.Pointer.Capture(this);
            Cursor = new Cursor(StandardCursorType.SizeAll);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragStart is { } start)
        {
            _offset = _dragStartOffset + (e.GetPosition(this) - start);
            ClampAndUpdate();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        EndDrag(e.Pointer);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        EndDrag(null);
    }

    private Point ViewCenter => new(Bounds.Width / 2, Bounds.Height / 2);

    private double FitZoom
    {
        get
        {
            var size = SourceSize;
            if (size.Width <= 0 || size.Height <= 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
            {
                return 1;
            }

            var fit = Math.Min(Bounds.Width / size.Width, Bounds.Height / size.Height);
            return UpscaleOnFit ? fit : Math.Min(1, fit);
        }
    }

    private void EndDrag(IPointer? pointer)
    {
        if (_dragStart is null)
        {
            return;
        }

        _dragStart = null;
        pointer?.Capture(null);
        Cursor = null;
    }

    private void ApplyFit()
    {
        Zoom = FitZoom;
        ClampAndUpdate();
    }

    /// <summary>Zooms keeping the image point under <paramref name="anchor"/> still.</summary>
    private void ZoomAt(double requestedZoom, Point anchor)
    {
        if (Source is null)
        {
            return;
        }

        var minZoom = Math.Min(FitZoom, 1) / 2;
        var zoom = Math.Clamp(requestedZoom, minZoom, MaxZoom);
        var anchorVector = new Vector(anchor.X, anchor.Y);
        _offset = anchorVector - ((anchorVector - _offset) * (zoom / Zoom));
        Zoom = zoom;
        _fitMode = false;
        ClampAndUpdate();
    }

    /// <summary>Centers the image on axes where it is smaller than the view, otherwise keeps the view filled.</summary>
    private void ClampAndUpdate()
    {
        var content = SourceSize * Zoom;
        _offset = new Vector(ClampAxis(_offset.X, content.Width, Bounds.Width), ClampAxis(_offset.Y, content.Height, Bounds.Height));

        IsZoomedIn = content.Width > Bounds.Width + 0.5 || content.Height > Bounds.Height + 0.5;
        VisibleRect = Zoom <= 0
            ? default
            : new Rect(-_offset.X / Zoom, -_offset.Y / Zoom, Bounds.Width / Zoom, Bounds.Height / Zoom)
                .Intersect(new Rect(SourceSize));
        InvalidateVisual();

        static double ClampAxis(double offset, double content, double view) =>
            content <= view ? (view - content) / 2 : Math.Clamp(offset, view - content, 0);
    }
}
