using Microsoft.Maui.Controls.Shapes;

namespace Xylocopadream.UI.Maui.Controls;

/// <summary>
/// A floating panel over the page, closed by a click outside: put it over the whole root grid,
/// place the panel with <see cref="PanelHorizontalOptions"/>, <see cref="PanelVerticalOptions"/> and <see cref="PanelMargin"/>,
/// and open it with <see cref="IsOpen"/>:
/// <code>
/// &lt;xd:Popover x:Name="Picker" Grid.ColumnSpan="3" PanelHorizontalOptions="End" PanelMargin="0,150,68,0" PanelWidth="264"&gt;
///     &lt;xd:ColorPicker ... /&gt;
/// &lt;/xd:Popover&gt;
/// </code>
/// </summary>
[ContentProperty(nameof(Body))]
public class Popover : ContentView
{
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(
        nameof(Body), typeof(View), typeof(Popover),
        propertyChanged: (bindable, _, value) => ((Popover)bindable)._panel.Content = (View?)value);

    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(
        nameof(IsOpen), typeof(bool), typeof(Popover), false, BindingMode.TwoWay,
        propertyChanged: (bindable, _, value) => ((Popover)bindable).IsVisible = (bool)value);

    public static readonly BindableProperty PanelHorizontalOptionsProperty = BindableProperty.Create(
        nameof(PanelHorizontalOptions), typeof(LayoutOptions), typeof(Popover), LayoutOptions.Center,
        propertyChanged: (bindable, _, value) => ((Popover)bindable)._panel.HorizontalOptions = (LayoutOptions)value);

    public static readonly BindableProperty PanelVerticalOptionsProperty = BindableProperty.Create(
        nameof(PanelVerticalOptions), typeof(LayoutOptions), typeof(Popover), LayoutOptions.Start,
        propertyChanged: (bindable, _, value) => ((Popover)bindable)._panel.VerticalOptions = (LayoutOptions)value);

    public static readonly BindableProperty PanelMarginProperty = BindableProperty.Create(
        nameof(PanelMargin), typeof(Thickness), typeof(Popover), default(Thickness),
        propertyChanged: (bindable, _, value) => ((Popover)bindable)._panel.Margin = (Thickness)value);

    public static readonly BindableProperty PanelWidthProperty = BindableProperty.Create(
        nameof(PanelWidth), typeof(double), typeof(Popover), -1d,
        propertyChanged: (bindable, _, value) => ((Popover)bindable)._panel.WidthRequest = (double)value);

    private readonly Border _panel;

    public Popover()
    {
        XdTheme.EnsureApplied();

        IsVisible = false;
        ZIndex = 900;

        _panel = new Border
        {
            HorizontalOptions = PanelHorizontalOptions,
            VerticalOptions = PanelVerticalOptions,
            Padding = new Thickness(16, 14),
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            StrokeThickness = 1,
            Shadow = new Shadow { Brush = Brush.Black, Offset = new Point(0, 6), Radius = 18, Opacity = 0.25f },
        };
        _panel.SetDynamicResource(Border.StrokeProperty, XdKeys.Border);
        _panel.SetDynamicResource(BackgroundColorProperty, XdKeys.PopupBackground);

        // clicks inside the panel must not close it
        _panel.GestureRecognizers.Add(new TapGestureRecognizer());

        // the transparent layer catches the clicks outside
        Grid layer = new() { BackgroundColor = Colors.Transparent, Children = { _panel } };
        TapGestureRecognizer outside = new();
        outside.Tapped += (_, e) =>
        {
            if (e.GetPosition(_panel) is Point p && p.X >= 0 && p.Y >= 0 && p.X <= _panel.Width && p.Y <= _panel.Height)
                return;

            IsOpen = false;
        };
        layer.GestureRecognizers.Add(outside);

        Content = layer;
    }

    public View? Body
    {
        get => (View?)GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public LayoutOptions PanelHorizontalOptions
    {
        get => (LayoutOptions)GetValue(PanelHorizontalOptionsProperty);
        set => SetValue(PanelHorizontalOptionsProperty, value);
    }

    public LayoutOptions PanelVerticalOptions
    {
        get => (LayoutOptions)GetValue(PanelVerticalOptionsProperty);
        set => SetValue(PanelVerticalOptionsProperty, value);
    }

    public Thickness PanelMargin
    {
        get => (Thickness)GetValue(PanelMarginProperty);
        set => SetValue(PanelMarginProperty, value);
    }

    public double PanelWidth
    {
        get => (double)GetValue(PanelWidthProperty);
        set => SetValue(PanelWidthProperty, value);
    }
}
