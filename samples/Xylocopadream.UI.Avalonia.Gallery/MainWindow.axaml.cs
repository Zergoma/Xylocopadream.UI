using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Xylocopadream.UI.Avalonia.Dialogs;

namespace Xylocopadream.UI.Avalonia.Gallery;

public sealed record Swatch(string Key, IBrush Brush);

/// <summary>Shows every component of the library; it is its own view model (a gallery has no logic).</summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private static readonly string[] SwatchKeys =
    [
        "Xd.EditorBackground", "Xd.PanelBackground", "Xd.Border", "Xd.InputBorder", "Xd.Hover", "Xd.Pressed",
        "Xd.Selected", "Xd.SelectedInactive", "Xd.Accent", "Xd.Danger", "Xd.Text", "Xd.TextSecondary", "Xd.Icon",
    ];

    private readonly IDialogService _dialogs;
    private string _dialogResult = "Résultat du dernier dialogue.";
    private string _filter = "tr";

    public MainWindow()
    {
        InitializeComponent();
        _dialogs = new DialogService(() => this);
        Swatches = SwatchKeys
            .Select(key => this.TryFindResource(key, ActualThemeVariant, out var value) && value is IBrush brush ? new Swatch(key, brush) : null)
            .OfType<Swatch>()
            .ToList();
        Map.NavigateRequested += (_, point) => Viewer.CenterOn(point);
        DataContext = this;
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<Swatch> Swatches { get; }

    public IReadOnlyList<XdIcon> Icons => XdIcons.All;

    public IReadOnlyList<string> Tags { get; } = ["vacances", "famille", "2026", "montagne"];

    /// <summary>Reordered by the grips of the "Filtre et ordre" section.</summary>
    public System.Collections.ObjectModel.ObservableCollection<string> Words { get; } = ["attrise", "triste", "pomme", "strate", "poire"];

    public string Filter
    {
        get => _filter;
        set
        {
            _filter = value;
            OnPropertyChanged();
        }
    }

    public Bitmap SampleImage { get; } = CreateSampleImage(1600, 1000);

    public string StatusText => $"{XdIcons.All.Count} icônes · thème sombre Rider";

    public string DialogResult
    {
        get => _dialogResult;
        private set
        {
            _dialogResult = value;
            OnPropertyChanged();
        }
    }

    private void OnSectionSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (SectionList.SelectedItem is ListBoxItem { Tag: string name } && this.FindControl<Control>(name) is { } section)
        {
            section.BringIntoView();
        }
    }

    private async void OnPrompt(object? sender, RoutedEventArgs e)
    {
        var text = await _dialogs.PromptAsync("Nouveau dossier", "Nom du dossier :", "Nouveau dossier", "Créer");
        DialogResult = text is null ? "Saisie annulée." : $"Saisi : « {text} »";
    }

    private async void OnConfirm(object? sender, RoutedEventArgs e) =>
        DialogResult = await _dialogs.ConfirmAsync("Confirmation", "Continuer ?", "Continuer") ? "Confirmé." : "Annulé.";

    private async void OnConfirmDestructive(object? sender, RoutedEventArgs e) =>
        DialogResult = await _dialogs.ConfirmAsync("Supprimer", "Supprimer définitivement « photo.jpg » ?", "Supprimer", isDestructive: true)
            ? "Supprimé."
            : "Annulé.";

    private async void OnError(object? sender, RoutedEventArgs e)
    {
        await _dialogs.ShowErrorAsync("Erreur", "Accès refusé à cet emplacement.");
        DialogResult = "Erreur affichée.";
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>A gradient with a grid, so zoom and panning are visible.</summary>
    private static WriteableBitmap CreateSampleImage(int width, int height)
    {
        var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
        using var buffer = bitmap.Lock();
        var row = new byte[width * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var onGrid = x % 100 < 2 || y % 100 < 2;
                var t = (x + y) / (double)(width + height);
                var i = x * 4;
                row[i] = onGrid ? (byte)40 : (byte)(240 - (120 * t));          // blue
                row[i + 1] = onGrid ? (byte)40 : (byte)(116 + (100 * t));      // green
                row[i + 2] = onGrid ? (byte)40 : (byte)(53 + (190 * t));       // red
                row[i + 3] = 255;
            }

            Marshal.Copy(row, 0, buffer.Address + (y * buffer.RowBytes), row.Length);
        }

        return bitmap;
    }
}
