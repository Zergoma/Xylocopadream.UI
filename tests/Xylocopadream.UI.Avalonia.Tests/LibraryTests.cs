using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xylocopadream.UI.Avalonia.Controls;
using Xylocopadream.UI.Avalonia.Dialogs;
using Xylocopadream.UI.Avalonia.Gallery;

[assembly: AvaloniaTestApplication(typeof(Xylocopadream.UI.Avalonia.Tests.TestAppBuilder))]

namespace Xylocopadream.UI.Avalonia.Tests;

public static class TestAppBuilder
{
    // The gallery app: it only declares <xd:XylocopadreamTheme />, like any app using the package.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseSkia()
            .WithInterFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

public sealed class LibraryTests
{
    private static readonly string Screens = Path.Combine(AppContext.BaseDirectory, "screens");

    [AvaloniaFact]
    public void Theme_overrides_fluent_and_exposes_its_brushes()
    {
        var app = global::Avalonia.Application.Current!;

        Assert.True(app.TryGetResource("SystemAccentColor", app.ActualThemeVariant, out var accent));
        Assert.Equal(Color.Parse("#3574F0"), accent);
        Assert.True(app.TryGetResource("TreeViewItemMinHeight", app.ActualThemeVariant, out var rowHeight));
        Assert.Equal(24d, rowHeight);
        Assert.True(app.TryGetResource("Xd.EditorBackground", app.ActualThemeVariant, out var background));
        Assert.Equal(Color.Parse("#1E1F22"), ((ISolidColorBrush)background!).Color);
    }

    [AvaloniaFact]
    public void Every_icon_is_a_resource_with_a_visible_shape()
    {
        var app = global::Avalonia.Application.Current!;

        Assert.True(XdIcons.All.Count >= 25);
        Assert.Equal(XdIcons.All.Count, XdIcons.All.Select(i => i.Name).Distinct().Count());
        foreach (var icon in XdIcons.All)
        {
            Assert.True(app.TryGetResource(icon.Key, app.ActualThemeVariant, out var geometry), icon.Key);
            var bounds = ((Geometry)geometry!).Bounds;
            Assert.True(bounds.Width > 0 || bounds.Height > 0, icon.Key);
            Assert.True(bounds.Right <= 16 && bounds.Bottom <= 16, $"{icon.Key} leaves the 16 px grid: {bounds}");
        }
    }

    [Theory]
    [InlineData("Mon Coffre", "MC")]
    [InlineData("MonCoffre", "MC")]
    [InlineData("photos", "PH")]
    [InlineData("x", "X")]
    [InlineData("", "?")]
    [InlineData("my-great_app", "MG")]
    public void Monogram_takes_two_initials(string name, string expected) => Assert.Equal(expected, Monogram.Of(name));

    [Fact]
    public void Named_colors_are_stable_and_case_insensitive()
    {
        Assert.Same(NamedColors.For("famille"), NamedColors.For("FAMILLE"));
        Assert.Contains(NamedColors.For("anything"), NamedColors.Palette);
        Assert.Equal("Photos", Monogram.NameOfPath(@"C:\Vaults\Photos\"));
    }

    [AvaloniaFact]
    public void Initials_badge_uses_the_last_path_segment()
    {
        var badge = new InitialsBadge { Text = @"C:\Vaults\Mon Coffre", Size = 40 };

        Assert.Equal("MC", badge.Initials);
        Assert.Same(NamedColors.For("Mon Coffre"), badge.Background);
        Assert.Equal(40, badge.Width);
    }

    [AvaloniaFact]
    public async Task Prompt_window_returns_the_typed_text()
    {
        var owner = new Window();
        owner.Show();

        var prompt = new PromptWindow("Renommer", "Nouveau nom :", "Renommer", input: "ancien");
        var result = prompt.ShowDialog<string?>(owner);
        Assert.Equal("ancien", prompt.InputText);
        prompt.InputText = "nouveau";
        prompt.Accept();

        Assert.Equal("nouveau", await result);
        owner.Close();
    }

    [AvaloniaFact]
    public async Task Gallery_renders_every_section_and_the_viewer_zooms()
    {
        Directory.CreateDirectory(Screens);
        var window = new MainWindow { Width = 1280, Height = 860 };
        window.Show();
        Render();
        Save(window, "1-gallery.png");

        var viewer = window.GetVisualDescendants().OfType<ZoomPanViewer>().Single();
        var minimap = window.GetVisualDescendants().OfType<Minimap>().Single();
        viewer.BringIntoView();
        Render();
        Assert.False(viewer.IsZoomedIn);

        viewer.ZoomIn();
        viewer.ZoomIn();
        viewer.ZoomIn();
        Render();
        Assert.True(viewer.IsZoomedIn);
        Assert.True(minimap.IsVisible);
        Save(window, "2-gallery-viewer.png");

        await Task.CompletedTask;
        window.Close();
    }

    [Theory]
    [InlineData("attrise", "tr", "at|[tr]|ise")]
    [InlineData("triste", "TR", "[tr]|iste")]
    [InlineData("trtr", "tr", "[tr]|[tr]")]
    [InlineData("pomme", "tr", "pomme")]
    [InlineData("pomme", "", "pomme")]
    public void Highlight_splits_every_match_ignoring_case(string text, string highlight, string expected) =>
        Assert.Equal(
            expected,
            string.Join("|", HighlightedTextBlock.Split(text, highlight).Select(p => p.IsMatch ? $"[{p.Text}]" : p.Text)));

    [AvaloniaFact]
    public void Highlighted_text_colors_its_matches()
    {
        var block = new HighlightedTextBlock { SourceText = "attrise", Highlight = "tr" };
        var window = new Window { Content = block };
        window.Show();

        var runs = block.Inlines!.OfType<global::Avalonia.Controls.Documents.Run>().ToList();
        Assert.Equal(["at", "tr", "ise"], runs.Select(r => r.Text));
        Assert.NotNull(runs[1].Background);
        Assert.Null(runs[0].Background);

        block.Highlight = null;
        Assert.Equal("attrise", block.Text);
        window.Close();
    }

    [Fact]
    public void File_filters_give_the_default_extension()
    {
        Assert.Equal("xvault", new FileFilter("Coffres", "*.xvault").DefaultExtension);
        Assert.Null(FileFilter.AllFiles.DefaultExtension);
    }

    [AvaloniaFact]
    public void Dragging_a_grip_reorders_the_items_and_animates_them()
    {
        Directory.CreateDirectory(Screens);
        var window = new MainWindow { Width = 1280, Height = 860 };
        window.Show();
        var list = window.FindControl<ItemsControl>("ReorderList")!;
        list.BringIntoView();
        Render();

        var rows = list.GetRealizedContainers().ToList();
        var grip = rows[0].GetVisualDescendants().OfType<Border>().First(b => Reorder.GetIsHandle(b));
        var start = grip.TranslatePoint(new Point(grip.Bounds.Width / 2, grip.Bounds.Height / 2), window)!.Value;
        var third = rows[2].TranslatePoint(new Point(10, rows[2].Bounds.Height / 2), window)!.Value;

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(new Point(start.X, (start.Y + third.Y) / 2));
        window.MouseMove(new Point(start.X, third.Y));
        Render();

        Assert.Equal(["triste", "pomme", "attrise", "strate", "poire"], window.Words);
        Assert.Contains(list.GetRealizedContainers(), c => c.RenderTransform is TranslateTransform);
        Save(window, "3-gallery-reorder.png");

        window.MouseUp(new Point(start.X, third.Y), MouseButton.Left);
        Render();
        Assert.DoesNotContain(list.GetRealizedContainers(), c => c.Classes.Contains("xd-dragging"));
        window.Close();
    }

    [AvaloniaFact]
    public void Dragging_into_a_list_of_the_same_group_moves_the_item_there()
    {
        var window = new MainWindow { Width = 1280, Height = 860 };
        window.Show();
        var list = window.FindControl<ItemsControl>("ReorderList")!;
        var other = window.FindControl<ItemsControl>("OtherList")!;
        list.BringIntoView();
        Render();

        var grip = list.GetRealizedContainers().First().GetVisualDescendants().OfType<Border>().First(b => Reorder.GetIsHandle(b));
        var start = grip.TranslatePoint(new Point(grip.Bounds.Width / 2, grip.Bounds.Height / 2), window)!.Value;
        var second = other.GetRealizedContainers().ElementAt(1);
        var target = second.TranslatePoint(new Point(20, second.Bounds.Height / 2), window)!.Value;

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(target);
        Render();
        window.MouseUp(target, MouseButton.Left);
        Render();

        Assert.Equal(["triste", "pomme", "strate", "poire"], window.Words);
        Assert.Equal(["trèfle", "attrise", "cerise"], window.OtherWords);
        Save(window, "4-gallery-reorder-between-lists.png");
        window.Close();
    }

    [AvaloniaFact]
    public void Color_picker_takes_presets_hex_codes_shades_and_reset()
    {
        var window = new MainWindow { Width = 1280, Height = 860 };
        window.Show();
        var picker = window.FindControl<ColorPicker>("Picker")!;
        picker.BringIntoView();
        Render();
        Assert.Equal("#56A8F5", picker.HexBox.Text);

        // A preset
        var red = picker.GetVisualDescendants().OfType<Button>().First(b => b.Tag is ColorPreset { Name: "Rouge" });
        red.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(Color.Parse("#F75464"), picker.SelectedColor);
        Assert.Equal("#F75464", picker.HexBox.Text);

        // A typed code (short form too)
        picker.HexBox.Text = "#0f0";
        Assert.Equal(Colors.Lime, picker.SelectedColor);

        // A click on the shade square: top right is the pure hue
        var square = picker.GetVisualDescendants().OfType<Grid>().First(g => g.Height == 130);
        var topRight = square.TranslatePoint(new Point(square.Bounds.Width - 1, 1), window)!.Value;
        window.MouseDown(topRight, MouseButton.Left);
        window.MouseUp(topRight, MouseButton.Left);
        var (hue, saturation, value) = XdColorMath.HexToHsv(XdColorMath.ToHex(picker.SelectedColor));
        Assert.InRange(hue, 115, 125);
        Assert.True(saturation > 0.95 && value > 0.95);
        Render();
        Save(window, "5-gallery-color-picker.png");

        // Back to the default
        picker.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Normale"))
            .RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(Color.Parse("#DFE1E5"), picker.SelectedColor);
        window.Close();
    }

    [AvaloniaFact]
    public void Theme_switcher_changes_the_whole_palette()
    {
        var app = global::Avalonia.Application.Current!;
        var window = new MainWindow { Width = 1280, Height = 860 };
        window.Show();
        var switcher = window.FindControl<ThemeSwitcher>("ThemeChoice")!;
        Assert.Equal(global::Avalonia.Styling.ThemeVariant.Dark, switcher.Variant); // the gallery starts dark

        try
        {
            switcher.SelectedIndex = 0; // light
            Render();
            Assert.Equal(global::Avalonia.Styling.ThemeVariant.Light, app.RequestedThemeVariant);
            Assert.True(window.TryFindResource("Xd.EditorBackground", window.ActualThemeVariant, out var light));
            Assert.Equal(Colors.White, ((ISolidColorBrush)light!).Color);
            Save(window, "6-gallery-light.png");

            switcher.Variant = global::Avalonia.Styling.ThemeVariant.Default;
            Assert.Equal(2, switcher.SelectedIndex);
        }
        finally
        {
            switcher.Variant = global::Avalonia.Styling.ThemeVariant.Dark;
            window.Close();
        }

        Assert.Equal(global::Avalonia.Styling.ThemeVariant.Dark, app.RequestedThemeVariant);
    }

    [AvaloniaFact]
    public void Breadcrumb_runs_its_command_with_the_clicked_segment()
    {
        var window = new MainWindow { Width = 1280, Height = 860 };
        window.Show();
        var crumbs = window.FindControl<Breadcrumb>("Crumbs")!;
        crumbs.BringIntoView();
        Render();

        var segments = crumbs.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("crumb")).ToList();
        Assert.Equal(4, segments.Count);
        Assert.Equal(FontWeight.SemiBold, segments[^1].FontWeight);
        Assert.NotEqual(FontWeight.SemiBold, segments[0].FontWeight);
        segments[1].RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.Equal("Segment cliqué : Photos", window.CrumbResult);
        window.Close();
    }

    [AvaloniaFact]
    public void Radial_menu_lays_its_groups_beside_the_click_and_runs_an_action()
    {
        var window = new MainWindow { Width = 1280, Height = 860 };
        window.Show();
        var target = window.FindControl<Border>("RadialTarget")!;
        target.BringIntoView();
        Render();

        var click = new Point(120, 60);
        window.ShowRadialMenu(click);
        var menu = window.OpenRadialMenu!;
        Render();
        var origin = target.TranslatePoint(click, window)!.Value;

        // Three bubbles, right of the click and of the area to avoid, not overlapping.
        Assert.Equal(3, menu.Bubbles.Count);
        Assert.All(menu.Bubbles, b => Assert.True(b.Bounds.Left > origin.X + 40, $"{b.Bounds} not right of {origin}"));
        for (var i = 0; i < menu.Bubbles.Count; i++)
        {
            for (var j = i + 1; j < menu.Bubbles.Count; j++)
            {
                Assert.False(menu.Bubbles[i].Bounds.Intersects(menu.Bubbles[j].Bounds), "bubbles overlap");
            }
        }

        // Equally spaced from 0° (right of the circle's center), clockwise: the first one is the rightmost, at the click's height.
        Assert.Equal(menu.Bubbles.Max(b => b.Bounds.Right), menu.Bubbles[0].Bounds.Right);
        Assert.InRange(menu.Bubbles[0].Bounds.Center.Y - origin.Y, -1, 1);
        Assert.True(menu.Bubbles[1].Bounds.Center.Y > origin.Y, "clockwise: the second one is below");

        // The pointer in a bubble highlights that bubble.
        menu.PointTo(menu.Bubbles[1].Bounds.Center);
        Assert.Same(menu.Bubbles[1].Bubble, menu.Highlighted);

        // Pointing at a bubble highlights it.
        var last = menu.Bubbles[^1];
        menu.PointTo(origin + ((last.Bounds.Center - origin) * 0.5));
        Assert.Same(last.Bubble, menu.Highlighted);
        DispatcherTimer.RunOnce(() => { }, TimeSpan.Zero);
        Thread.Sleep(250); // let the appearing animation end
        Render();
        Save(window, "7-gallery-radial-menu.png");

        // An item runs its action and closes the menu.
        var copy = menu.Bubbles[0].Bubble.GetVisualDescendants().OfType<Button>().First();
        copy.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.Equal("Menu radial : copier", window.CrumbResult);
        Assert.False(menu.IsOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void Dragging_over_a_taller_item_swaps_once_past_its_middle()
    {
        var items = new System.Collections.ObjectModel.ObservableCollection<string>(["court", "très grand", "fin"]);
        var list = new ItemsControl
        {
            ItemsSource = items,
            ItemTemplate = new global::Avalonia.Controls.Templates.FuncDataTemplate<string>((text, _) => new Border
            {
                Height = text == "très grand" ? 90 : 30,
                Background = Brushes.DimGray,
                Child = new Border { Width = 20, Background = Brushes.Transparent, HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left, [Reorder.IsHandleProperty] = true },
            }),
            [Reorder.AnimateProperty] = true,
        };
        var window = new Window { Width = 300, Height = 300, Content = list };
        window.Show();
        Render();

        var tall = list.ContainerFromIndex(1)!;
        var top = list.TranslatePoint(new Point(10, 15), window)!.Value;
        window.MouseDown(top, MouseButton.Left);

        // In the upper half of the tall item: no swap yet.
        window.MouseMove(list.TranslatePoint(new Point(10, tall.Bounds.Top + 20), window)!.Value);
        Render();
        Assert.Equal(["court", "très grand", "fin"], items);

        // Past its middle: one swap, and moving a little more there does not swap back.
        var below = list.TranslatePoint(new Point(10, tall.Bounds.Top + 60), window)!.Value;
        window.MouseMove(below);
        Render();
        Assert.Equal(["très grand", "court", "fin"], items);
        window.MouseMove(below + new Vector(0, 2));
        Render();
        window.MouseMove(below + new Vector(0, 4));
        Render();
        Assert.Equal(["très grand", "court", "fin"], items);

        window.MouseUp(below, MouseButton.Left);
        window.Close();
    }

    private static void Render()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

#pragma warning disable CS0618 // PNG defaults are fine for test screenshots
    private static void Save(Window window, string name) =>
        window.CaptureRenderedFrame()?.Save(Path.Combine(Screens, name));
#pragma warning restore CS0618
}
