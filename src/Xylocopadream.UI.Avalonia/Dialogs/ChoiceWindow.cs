using Avalonia.Controls;
using Avalonia.Layout;

namespace Xylocopadream.UI.Avalonia.Dialogs;

/// <summary>A message with several answers (e.g. "Enregistrer", "Ne pas enregistrer", "Annuler"). Closes with the index
/// of the chosen answer, or -1 (Escape, or the window closed).</summary>
public class ChoiceWindow : Window
{
    private readonly List<Button> _buttons = [];

    public ChoiceWindow()
        : this(string.Empty, string.Empty, ["OK"])
    {
    }

    /// <param name="defaultIndex">The answer of Enter, in the accent color.</param>
    /// <param name="destructiveIndex">An answer shown in red (losing changes, deleting...), if any.</param>
    /// <param name="cancelIndex">The answer of Escape, if any; -1 is returned otherwise.</param>
    public ChoiceWindow(string title, string message, IReadOnlyList<string> choices, int defaultIndex = 0, int? destructiveIndex = null, int? cancelIndex = null)
    {
        Title = title;
        Width = 480;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        for (var i = 0; i < choices.Count; i++)
        {
            var index = i;
            var button = new Button
            {
                Content = choices[i],
                MinWidth = 90,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                IsDefault = i == defaultIndex,
                IsCancel = i == cancelIndex,
            };
            if (i == destructiveIndex)
            {
                button.Classes.Add("danger");
            }
            else if (i == defaultIndex)
            {
                button.Classes.Add("accent");
            }

            button.Click += (_, _) => Close(index);
            _buttons.Add(button);
            buttons.Children.Add(button);
        }

        Content = new StackPanel
        {
            Margin = new global::Avalonia.Thickness(20),
            Spacing = 14,
            Children = { new TextBlock { Text = message, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap }, buttons },
        };

        Opened += (_, _) =>
        {
            if (defaultIndex >= 0 && defaultIndex < _buttons.Count)
            {
                _buttons[defaultIndex].Focus();
            }
        };
    }

    /// <summary>Same as clicking the answer (for tests and keyboard use).</summary>
    public void Choose(int index) => Close(index);
}
