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

/// <summary>A segment of the breadcrumb demo.</summary>
public sealed record Crumb(string Name);

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
    private string _crumbResult = "Cliquez un segment du fil d’Ariane.";

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

    public IReadOnlyList<Controls.ColorPreset> ColorPresets { get; } =
    [
        new("Rouge", Color.Parse("#F75464")), new("Orange", Color.Parse("#E5A55A")), new("Jaune", Color.Parse("#E5C07B")),
        new("Vert", Color.Parse("#6AAB73")), new("Bleu", Color.Parse("#56A8F5")), new("Violet", Color.Parse("#C77DBB")),
    ];

    /// <summary>Second list of the same reorder group: lines move between both.</summary>
    public System.Collections.ObjectModel.ObservableCollection<string> OtherWords { get; } = ["trèfle", "cerise"];

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

    public IReadOnlyList<Crumb> CrumbPath { get; } = [new("Coffre"), new("Photos"), new("2026"), new("Vacances")];

    /// <summary>Clicked breadcrumb segment, shown under it.</summary>
    public System.Windows.Input.ICommand CrumbCommand => new GalleryCommand(item => CrumbResult = $"Segment cliqué : {(item as Crumb)?.Name}");

    public string CrumbResult
    {
        get => _crumbResult;
        private set
        {
            _crumbResult = value;
            OnPropertyChanged();
        }
    }

    /// <summary>The radial menu open over the demo area, if any (for tests).</summary>
    public Controls.RadialMenu? OpenRadialMenu { get; private set; }

    private void OnRadialTargetPressed(object? sender, global::Avalonia.Input.PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(RadialTarget).Properties.IsRightButtonPressed)
        {
            return;
        }

        e.Handled = true;
        ShowRadialMenu(e.GetPosition(RadialTarget));
    }

    /// <summary>Opens the demo radial menu at <paramref name="at"/> (in the demo area).</summary>
    public void ShowRadialMenu(Point at)
    {
        Geometry? Icon(string key) => this.TryFindResource(key, ActualThemeVariant, out var value) ? value as Geometry : null;
        void Done(string what) => CrumbResult = $"Menu radial : {what}";

        OpenRadialMenu = Controls.RadialMenu.Show(
            RadialTarget,
            at,
            [
                new Controls.RadialMenuGroup
                {
                    Title = "Édition",
                    Items =
                    [
                        new Controls.RadialMenuItem { Header = "Copier", Icon = Icon("Xd.Icon.Copy"), Action = () => Done("copier") },
                        new Controls.RadialMenuItem { Header = "Couper", Action = () => Done("couper") },
                        new Controls.RadialMenuItem { Header = "Coller", Action = () => Done("coller") },
                    ],
                },
                new Controls.RadialMenuGroup
                {
                    Title = "Couleur",
                    Background = new SolidColorBrush(Color.Parse("#3323324A")),
                    Items =
                    [
                        new Controls.RadialMenuItem { Header = "Texte…", Action = () => Done("texte") },
                        new Controls.RadialMenuItem { Header = "Fond…", Action = () => Done("fond") },
                    ],
                },
                new Controls.RadialMenuGroup
                {
                    Items =
                    [
                        new Controls.RadialMenuItem
                        {
                            Header = "Supprimer",
                            Icon = Icon("Xd.Icon.Delete"),
                            Background = new SolidColorBrush(Color.Parse("#99C94F4F")),
                            Action = () => Done("supprimer"),
                        },
                    ],
                },
            ],
            new Controls.RadialMenuOptions { Avoid = new Rect(at.X - 40, at.Y - 10, 80, 20) });
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

    private async void OnChoose(object? sender, RoutedEventArgs e)
    {
        string[] choices = ["Enregistrer", "Ne pas enregistrer", "Annuler"];
        var index = await _dialogs.ChooseAsync("Fermer", "« notes.txt » a des modifications non enregistrées.", choices, destructiveIndex: 1, cancelIndex: 2);
        DialogResult = index >= 0 ? $"Choix : {choices[index]}" : "Fermé sans choisir.";
    }

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

/// <summary>A command running a delegate (the gallery has no MVVM library).</summary>
internal sealed class GalleryCommand(Action<object?> execute) : System.Windows.Input.ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => execute(parameter);
}
