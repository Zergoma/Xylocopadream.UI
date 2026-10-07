using Microsoft.Maui.Controls.Shapes;

namespace Xylocopadream.UI.Maui.Controls;

/// <summary>
/// Draws the <see cref="InstantToolTip"/> of the elements inside the same layout: put it last in the root grid of the page,
/// over every row and column: <c>&lt;xd:InstantToolTipHost Grid.RowSpan="2" Grid.ColumnSpan="3" /&gt;</c>.
/// It never takes the pointer (the element under it would lose its hover).
/// </summary>
public class InstantToolTipHost : Border
{
    public const double Gap = 6;
    private const double ToolTipHeight = 30;

    private readonly Label _text;
    private View? _target;

    public InstantToolTipHost()
    {
        XdTheme.EnsureApplied();

        IsVisible = false;
        InputTransparent = true;
        ZIndex = 1000;
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Start;
        HeightRequest = ToolTipHeight;
        Padding = new Thickness(10, 0);
        StrokeShape = new RoundRectangle { CornerRadius = 6 };
        StrokeThickness = 1;
        Shadow = new Shadow { Brush = Brush.Black, Offset = new Point(0, 2), Radius = 8, Opacity = 0.2f };
        SetDynamicResource(StrokeProperty, XdKeys.Border);
        SetDynamicResource(BackgroundColorProperty, XdKeys.PopupBackground);

        _text = new Label { FontSize = 13, VerticalOptions = LayoutOptions.Center };
        _text.SetDynamicResource(Label.TextColorProperty, XdKeys.Text);
        Content = _text;
    }

    /// <summary>
    /// Shows the text beside the target: centered on its side for the left and right,
    /// aligned on its left edge above and below.
    /// </summary>
    public void Show(string text, View target, ToolTipPlacement placement)
    {
        if (Parent is not VisualElement area)
            return;

        _target = target;
        _text.Text = text;

        Point origin = PositionInPage(area);
        Point position = PositionInPage(target);
        double x = position.X - origin.X;
        double y = position.Y - origin.Y;
        double centeredY = y + (target.Height - ToolTipHeight) / 2;

        // on the left and above, the tooltip is aligned on the far edge of the area: its size is not needed
        switch (placement)
        {
            case ToolTipPlacement.Left:
                Place(LayoutOptions.End, LayoutOptions.Start, -(area.Width - x + Gap), centeredY);
                break;
            case ToolTipPlacement.Below:
                Place(LayoutOptions.Start, LayoutOptions.Start, x, y + target.Height + Gap);
                break;
            case ToolTipPlacement.Above:
                Place(LayoutOptions.Start, LayoutOptions.End, x, -(area.Height - y + Gap));
                break;
            default:
                Place(LayoutOptions.Start, LayoutOptions.Start, x + target.Width + Gap, centeredY);
                break;
        }

        IsVisible = true;
    }

    /// <summary>
    /// Hides the tooltip, if it is still the one of this target.
    /// </summary>
    public void Hide(View target)
    {
        if (_target != target)
            return;

        _target = null;
        IsVisible = false;
    }

    private void Place(LayoutOptions horizontal, LayoutOptions vertical, double x, double y)
    {
        HorizontalOptions = horizontal;
        VerticalOptions = vertical;
        TranslationX = x;
        TranslationY = y;
    }

    /// <summary>
    /// Position of an element in the page (scrolling is not taken into account).
    /// </summary>
    private static Point PositionInPage(VisualElement element)
    {
        double x = 0, y = 0;
        for (Element? current = element; current is VisualElement visual && current is not Page; current = current.Parent)
        {
            x += visual.X;
            y += visual.Y;
        }

        return new Point(x, y);
    }
}
