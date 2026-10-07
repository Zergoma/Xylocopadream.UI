using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Xylocopadream.UI.Avalonia;

/// <summary>
/// Dark theme inspired by JetBrains Rider, built on Fluent. Use it instead of <c>FluentTheme</c>
/// and set <c>RequestedThemeVariant="Dark"</c> on the application.
/// </summary>
public class XylocopadreamTheme : Styles
{
    public XylocopadreamTheme()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
