using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Xylocopadream.UI.Avalonia.Dialogs;

/// <summary>Message, confirmation or single-line input dialog. Closes with the text (or "" without input), or null.</summary>
public partial class PromptWindow : Window
{
    private readonly bool _hasInput;

    public PromptWindow()
        : this(string.Empty, string.Empty, "OK")
    {
    }

    /// <param name="input">Initial text of the input box; null for a dialog without input.</param>
    /// <param name="isDestructive">Red confirm button (deletion, errors) instead of the accent one.</param>
    public PromptWindow(string title, string message, string okText, string? input = null, bool showCancel = true, bool isDestructive = false)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        OkButton.Content = okText;
        CancelButton.IsVisible = showCancel;
        OkButton.Classes.Add(isDestructive ? "danger" : "accent");

        _hasInput = input is not null;
        if (_hasInput)
        {
            InputBox.IsVisible = true;
            InputBox.Text = input;
        }

        Opened += (_, _) =>
        {
            if (_hasInput)
            {
                InputBox.Focus();
                InputBox.SelectAll();
            }
            else
            {
                OkButton.Focus();
            }
        };
    }

    /// <summary>Text typed so far (for tests).</summary>
    public string? InputText
    {
        get => InputBox.Text;
        set => InputBox.Text = value;
    }

    /// <summary>Same as clicking the confirm button.</summary>
    public void Accept() => Close(_hasInput ? InputBox.Text ?? string.Empty : string.Empty);

    private void OnOk(object? sender, RoutedEventArgs e) => Accept();

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(null);
}
