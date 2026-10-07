using Microsoft.Maui.Controls.Shapes;

using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace Xylocopadream.UI.Maui.Controls;

/// <summary>
/// States of the controls are drawn with stacked layers, each with its color set once as a dynamic resource,
/// then only shown or hidden. On MAUI a dynamic resource removed and set again (or set by a trigger) was not
/// applied any more, and a local value hides it: layers avoid both.
/// </summary>
internal static class Layers
{
    public static BoxView Fill(string colorKey, bool isVisible = false)
    {
        BoxView layer = new() { IsVisible = isVisible, InputTransparent = true };
        layer.SetDynamicResource(BoxView.ColorProperty, colorKey);
        return layer;
    }

    public static Path Icon(double size, string? colorKey, bool isVisible = false)
    {
        Path path = new()
        {
            Aspect = Stretch.Uniform,
            WidthRequest = size,
            HeightRequest = size,
            StrokeThickness = 0,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            IsVisible = isVisible,
            InputTransparent = true,
        };
        if (colorKey is not null)
            path.SetDynamicResource(Shape.FillProperty, colorKey);
        return path;
    }

    public static Label Text(string colorKey, bool isVisible = false)
    {
        Label label = new()
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            IsVisible = isVisible,
            InputTransparent = true,
        };
        label.SetDynamicResource(Label.TextColorProperty, colorKey);
        return label;
    }
}
