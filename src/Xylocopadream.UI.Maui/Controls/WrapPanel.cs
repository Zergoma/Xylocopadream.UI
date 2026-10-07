using Microsoft.Maui.Layouts;

namespace Xylocopadream.UI.Maui.Controls;

/// <summary>
/// Lays its children out from left to right, going to the next row when there is no more room.
/// Sizes are rounded up: a wrapping FlexLayout, arranged with its measured width rounded to the pixel,
/// sometimes wrapped its last child and squeezed both rows into the height of one.
/// </summary>
public class WrapPanel : Layout
{
    public static readonly BindableProperty SpacingProperty = BindableProperty.Create(
        nameof(Spacing), typeof(double), typeof(WrapPanel), 0d,
        propertyChanged: (bindable, _, _) => ((WrapPanel)bindable).InvalidateMeasure());

    /// <summary>
    /// Space between two children, in a row and between rows.
    /// </summary>
    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    protected override ILayoutManager CreateLayoutManager() => new WrapLayoutManager(this);

    private sealed class WrapLayoutManager(WrapPanel panel) : LayoutManager(panel)
    {
        public override Size Measure(double widthConstraint, double heightConstraint)
        {
            Thickness padding = panel.Padding;
            double available = widthConstraint - padding.HorizontalThickness;

            foreach (IView child in panel)
            {
                if (child.Visibility != Visibility.Collapsed)
                    child.Measure(double.PositiveInfinity, double.PositiveInfinity);
            }

            Size content = Place(available, arrange: false, 0, 0);
            return new Size(content.Width + padding.HorizontalThickness, content.Height + padding.VerticalThickness);
        }

        public override Size ArrangeChildren(Rect bounds)
        {
            Thickness padding = panel.Padding;
            Place(bounds.Width - padding.HorizontalThickness, arrange: true, bounds.X + padding.Left, bounds.Y + padding.Top);
            return bounds.Size;
        }

        private Size Place(double available, bool arrange, double left, double top)
        {
            double x = 0, y = 0, rowHeight = 0, width = 0;

            foreach (IView child in panel)
            {
                if (child.Visibility == Visibility.Collapsed)
                    continue;

                double childWidth = Math.Ceiling(child.DesiredSize.Width);
                double childHeight = Math.Ceiling(child.DesiredSize.Height);

                // next row when this child does not fit (a child alone on its row always goes there)
                if (x > 0 && x + childWidth > available + 0.5)
                {
                    x = 0;
                    y += rowHeight + panel.Spacing;
                    rowHeight = 0;
                }

                if (arrange)
                    child.Arrange(new Rect(left + x, top + y, childWidth, childHeight));

                width = Math.Max(width, x + childWidth);
                x += childWidth + panel.Spacing;
                rowHeight = Math.Max(rowHeight, childHeight);
            }

            return new Size(width, y + rowHeight);
        }
    }
}
