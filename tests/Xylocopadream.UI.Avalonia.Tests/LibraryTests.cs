using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
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
