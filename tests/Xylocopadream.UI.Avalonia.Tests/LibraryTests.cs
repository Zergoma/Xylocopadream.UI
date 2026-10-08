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
