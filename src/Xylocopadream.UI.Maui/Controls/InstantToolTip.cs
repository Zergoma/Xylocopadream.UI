namespace Xylocopadream.UI.Maui.Controls;

/// <summary>
/// Side of the element its tooltip goes to.
/// </summary>
public enum ToolTipPlacement
{
    Right,
    Left,
    Below,
    Above,
}

/// <summary>
/// A tooltip shown at once (no delay, unlike the native one), beside the element, level with it:
/// <c>&lt;xd:NavRailButton xd:InstantToolTip.Text="Home" xd:InstantToolTip.Placement="Right" /&gt;</c>.
/// It is drawn by the <see cref="InstantToolTipHost"/> found in a layout around the element
/// (put one in the root grid of the page). The text can change while shown later (read on each hover).
/// </summary>
public static class InstantToolTip
{
    public static readonly BindableProperty TextProperty = BindableProperty.CreateAttached(
        "Text", typeof(string), typeof(InstantToolTip), null,
        propertyChanged: (bindable, _, _) => Attach((View)bindable));

    public static readonly BindableProperty PlacementProperty = BindableProperty.CreateAttached(
        "Placement", typeof(ToolTipPlacement), typeof(InstantToolTip), ToolTipPlacement.Right);

    // the pointer recognizer is added once per element
    private static readonly BindableProperty IsAttachedProperty = BindableProperty.CreateAttached(
        "IsAttached", typeof(bool), typeof(InstantToolTip), false);

    public static string? GetText(BindableObject view) => (string?)view.GetValue(TextProperty);

    public static void SetText(BindableObject view, string? value) => view.SetValue(TextProperty, value);

    public static ToolTipPlacement GetPlacement(BindableObject view) => (ToolTipPlacement)view.GetValue(PlacementProperty);

    public static void SetPlacement(BindableObject view, ToolTipPlacement value) => view.SetValue(PlacementProperty, value);

    private static void Attach(View view)
    {
        if ((bool)view.GetValue(IsAttachedProperty))
            return;

        view.SetValue(IsAttachedProperty, true);

        PointerGestureRecognizer pointer = new();
        pointer.PointerEntered += (_, _) =>
        {
            if (GetText(view) is { Length: > 0 } text)
                FindHost(view)?.Show(text, view, GetPlacement(view));
        };
        pointer.PointerExited += (_, _) => FindHost(view)?.Hide(view);
        view.GestureRecognizers.Add(pointer);
    }

    private static InstantToolTipHost? FindHost(Element view)
    {
        for (Element? parent = view.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is Layout layout && layout.Children.OfType<InstantToolTipHost>().FirstOrDefault() is { } host)
                return host;
        }

        return null;
    }
}
