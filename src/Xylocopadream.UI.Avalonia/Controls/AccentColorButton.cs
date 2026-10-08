using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace Xylocopadream.UI.Avalonia.Controls;

/// <summary>
/// A toolbar button showing the main color; it opens a <see cref="ColorPicker"/> (presets, shade, hex code, back to
/// the default) whose choice applies at once to the whole app through <see cref="XdAccent"/>, as in XyloType.
/// </summary>
public class AccentColorButton : Button
{
    private readonly Ellipse _swatch = new() { Width = 12, Height = 12, VerticalAlignment = VerticalAlignment.Center };

    public AccentColorButton()
    {
        var icon = new global::Avalonia.Controls.Shapes.Path();
        icon.Classes.Add("icon");
        icon.Bind(global::Avalonia.Controls.Shapes.Path.DataProperty, icon.GetResourceObservable("Xd.Icon.Palette"));
        Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Children = { icon, _swatch } };
        Classes.Add("tool");
        ToolTip.SetTip(this, "Couleur principale");
        Click += (_, _) => Open();
        ShowColor();
        XdAccent.Changed += (_, _) => ShowColor();
    }

    /// <summary>The picker open in the flyout, if any (for tests).</summary>
    public ColorPicker? OpenPicker { get; private set; }

    protected override Type StyleKeyOverride => typeof(Button);

    private void Open()
    {
        var picker = new ColorPicker
        {
            Title = "Couleur principale",
            Presets = XdAccent.Presets,
            DefaultColor = XdAccent.Default,
            ResetText = "Par défaut",
            SelectedColor = XdAccent.Current,
        };

        // Subscribed after the initial color: opening the picker changes nothing.
        picker.PropertyChanged += (_, e) =>
        {
            if (e.Property == ColorPicker.SelectedColorProperty)
            {
                XdAccent.Apply(picker.SelectedColor);
            }
        };

        var flyout = new Flyout { Content = picker, Placement = PlacementMode.BottomEdgeAlignedRight };
        flyout.Closed += (_, _) => OpenPicker = null;
        OpenPicker = picker;
        flyout.ShowAt(this);
    }

    private void ShowColor() => _swatch.Fill = new SolidColorBrush(XdAccent.Current);
}
