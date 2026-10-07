namespace Xylocopadream.UI.Maui.Gallery;

public class App : Microsoft.Maui.Controls.Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        // the colors of the controls, before the page is drawn
        XdTheme.Apply(this);

        return new Window(new GalleryPage()) { Width = 1100, Height = 760, Title = "Xylocopadream.UI MAUI Gallery" };
    }
}
