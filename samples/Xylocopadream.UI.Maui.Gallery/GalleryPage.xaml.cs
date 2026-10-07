using Microsoft.Maui.Controls.Shapes;

using Xylocopadream.UI.Maui.Controls;

namespace Xylocopadream.UI.Maui.Gallery;

/// <summary>
/// Every component of the library on one page.
/// </summary>
public partial class GalleryPage : ContentPage
{
    private readonly NavRailItem _documentItem = new() { Icon = XdIcons.Keyboard, ToolTip = "Open document", IsVisible = false, Key = "Document" };

    public GalleryPage()
    {
        InitializeComponent();

        // navigation rail: an entry at the top that comes and goes, the sections at the bottom
        Rail.TopItems.Add(_documentItem);
        Rail.BottomItems.Add(new NavRailItem { Icon = XdIcons.Home, ToolTip = "Home", Key = "Home" });
        Rail.BottomItems.Add(new NavRailItem { Icon = XdIcons.List, ToolTip = "List", Key = "List" });
        Rail.BottomItems.Add(new NavRailItem { Icon = XdIcons.Spellcheck, ToolTip = "Words", Key = "Words" });
        Rail.BottomItems.Add(new NavRailItem { Icon = XdIcons.Upload, ToolTip = "Import", Key = "Import" });
        Select("Home");

        Segments.ItemsSource = new[] { "General", "Sounds", "Music" };
        Segments.SelectedItem = "General";
        Segments.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SegmentedControl.SelectedItem))
                SegmentChoice.Text = $"Selected: {Segments.SelectedItem}";
        };
        SegmentChoice.Text = "Selected: General";

        foreach (var (name, geometry) in XdIcons.All)
            IconList.Children.Add(IconTile(name, geometry));

        foreach (var (key, _) in XdTheme.Resources(dark: false, XdTheme.Accent))
            ColorList.Children.Add(ColorTile(key));

        ThemeGroup.SelectedIndex = Microsoft.Maui.Controls.Application.Current?.UserAppTheme switch
        {
            AppTheme.Light => 0,
            AppTheme.Dark => 1,
            _ => 2,
        };

        AccentPicker.Presets =
        [
            new("Blue", Color.FromArgb("#2F6FEB")),
            new("Indigo", Color.FromArgb("#4F46E5")),
            new("Violet", Color.FromArgb("#7C3AED")),
            new("Pink", Color.FromArgb("#DB2777")),
            new("Orange", Color.FromArgb("#EA580C")),
            new("Green", Color.FromArgb("#16A34A")),
        ];
        AccentPicker.SelectedColor = XdTheme.Accent;
        AccentPicker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ColorPicker.SelectedColor))
                XdTheme.SetAccent(AccentPicker.SelectedColor);
        };
    }

    private void Select(string key)
    {
        Rail.SelectedKey = key;
        SectionTitle.Text = key;
        Status.Text = $"Section: {key}";
    }

    private void Rail_ItemClicked(object? sender, NavRailItem item)
        => Select((string)item.Key!);

    // the toggle also shows or hides the entry at the top of the rail
    private void ShuffleButton_Clicked(object? sender, EventArgs e)
    {
        ShuffleButton.IsActive = !ShuffleButton.IsActive;
        _documentItem.IsVisible = !ShuffleButton.IsActive;
    }

    private void ThemeGroup_SelectedIndexChanged(object? sender, int index)
    {
        if (Microsoft.Maui.Controls.Application.Current is { } app)
            app.UserAppTheme = index switch { 0 => AppTheme.Light, 1 => AppTheme.Dark, _ => AppTheme.Unspecified };
    }

    private void AccentButton_Clicked(object? sender, EventArgs e)
        => AccentPopover.IsOpen = !AccentPopover.IsOpen;

    private static View IconTile(string name, Geometry geometry)
    {
        Microsoft.Maui.Controls.Shapes.Path path = new() { Data = geometry, Aspect = Stretch.Uniform, WidthRequest = 22, HeightRequest = 22 };
        path.SetDynamicResource(Shape.FillProperty, XdKeys.Text);

        Label label = new() { Text = name, FontSize = 11, HorizontalOptions = LayoutOptions.Center };
        label.SetDynamicResource(Label.TextColorProperty, XdKeys.TextSecondary);

        return new VerticalStackLayout { WidthRequest = 86, Margin = new Thickness(0, 0, 0, 12), Spacing = 6, Children = { path, label } };
    }

    private static View ColorTile(string key)
    {
        Border swatch = new()
        {
            WidthRequest = 22,
            HeightRequest = 22,
            StrokeShape = new RoundRectangle { CornerRadius = 5 },
            StrokeThickness = 1,
        };
        swatch.SetDynamicResource(BackgroundColorProperty, key);
        swatch.SetDynamicResource(Border.StrokeProperty, XdKeys.Border);

        Label label = new() { Text = key, FontSize = 12, VerticalOptions = LayoutOptions.Center };
        label.SetDynamicResource(Label.TextColorProperty, XdKeys.Text);

        return new HorizontalStackLayout { WidthRequest = 250, Margin = new Thickness(0, 0, 0, 8), Spacing = 8, Children = { swatch, label } };
    }
}
