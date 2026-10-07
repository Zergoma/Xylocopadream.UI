# Xylocopadream.UI

Reusable UI components shared across the Xylocopadream applications, one package per framework:

| Package | Framework | Status |
|---|---|---|
| `Xylocopadream.UI.Avalonia` | Avalonia 12, .NET 10 | available |
| `Xylocopadream.UI.Maui` | .NET MAUI 11 (preview), Windows | preview |

The look follows JetBrains Rider (New UI): dense layouts, tool windows, an accent color, line or filled icons.
An app uses only the package of its framework.

**Rule for every app:** use these components first; when a reusable piece is missing, add it here
rather than in the app.

## Install

```
dotnet add package Xylocopadream.UI.Avalonia
dotnet add package Xylocopadream.UI.Maui --prerelease
```

The MAUI package is a preview (`0.x.y-preview`) as long as it is built on a preview of MAUI.

## Publish a new version

```
.\publish.ps1 -Bump              # local feed only
.\publish.ps1 -Bump -NuGetOrg    # local feed, then nuget.org
```

Increments the patch number of `<Version>` in `Directory.Build.props`, then packs every project of `src/` into the
local feed `%UserProfile%\nuget_local` (source `Local` of the user `NuGet.Config`). NuGet caches packages by version
and a version on nuget.org can never be replaced, so the script refuses a version already published.
`-NuGetOrg` then pushes the packages to nuget.org with the API key of the `NUGET_API_KEY` environment variable
(never stored in the repository). Apps then update their `PackageReference` version.

## Xylocopadream.UI.Avalonia

### Setup

```xml
<Application xmlns:xd="https://github.com/Zergoma/Xylocopadream.UI"
             RequestedThemeVariant="Dark">
    <Application.Styles>
        <xd:XylocopadreamTheme />   <!-- replaces <FluentTheme />, which it contains -->
    </Application.Styles>
</Application>
```

Add `.WithInterFont()` (package `Avalonia.Fonts.Inter`) in `Program.BuildAvaloniaApp` for the intended typography.
All types are in the `xd:` XAML namespace.

### Colors (`Themes/Colors.axaml`)

Brushes for app layouts, used with `{DynamicResource ...}`:

| Key | Use |
|---|---|
| `Xd.EditorBackground` | main content area (window background) |
| `Xd.PanelBackground` | toolbars, tool windows, status bar |
| `Xd.Separator`, `Xd.Border`, `Xd.InputBorder` | 1 px lines between panels, borders, input outlines |
| `Xd.Hover`, `Xd.Pressed` | flat button states |
| `Xd.Selected`, `Xd.SelectedInactive` | selection with / without focus |
| `Xd.Accent`, `Xd.AccentHover`, `Xd.Danger`, `Xd.DropTarget` | accent blue, errors, drag and drop highlight |
| `Xd.Text`, `Xd.TextSecondary`, `Xd.Icon` | text, muted text, icons |

The same file overrides Fluent's resources (accent color, buttons, text boxes, tree rows of 24 px, menus,
tooltips, sliders), so standard controls already look right.

### Icons (`Themes/Icons.axaml`)

16 × 16 stroke geometries: `<Path Classes="icon" Data="{StaticResource Xd.Icon.Folder}" />`
(add class `folder` for the blue folder color). Scale with a `Viewbox` for large icons.

Archive, Audio, Back, ChevronRight, Close, Delete, Document, Export, File, Folder, FolderNew, Forward, Hide, Image,
Import, ImportFolder, Lock, Move, Refresh, Rename, Search, Shield, Tag, Thumbnails, Up, Video.

`XdIcons.All` lists them (name, key, geometry). New icons: draw strokes on half pixels inside the 16 px grid,
in the same style; the tests check the grid.

### Style classes (`Themes/Controls.axaml`)

| Class | On | Look |
|---|---|---|
| `accent`, `danger` | `Button` | primary blue / destructive red |
| `tool` | `Button` | flat toolbar button (icon, or icon + text) |
| `link` | `Button` | text link |
| `crumb` | `Button` (with `tool`) | small button, e.g. a breadcrumb in a status bar |
| `chip` | `Button` | colored pill (tags); set `Background`, e.g. with `XdConverters.NameColor` |
| `chip-more` | `Border` > `TextBlock` | grey "+2" pill |
| `stripe` | `ToggleButton` | tool window stripe button, highlighted when checked |
| `overlay` | `Button` | translucent button over images and videos |
| `panel` | `Border` | toolbar / tool window / status bar surface (set `BorderThickness` for the separator side) |
| `toolbar-separator` | `Border` | vertical separator between toolbar groups |
| `thin` | `GridSplitter` | 1 px splitter between panels |
| `error` | `Border` > `TextBlock` | error message box in a form |
| `tiles` | `ListBox` | tile grid (with a `WrapPanel`), Rider selection colors |
| `drop-target` | `TreeViewItem`, `ListBoxItem` | drag and drop highlight, toggled from code |
| `secondary`, `small`, `title` | `TextBlock` | muted, 12 px, semibold |
| `icon` (+ `folder`) | `Path` | line icon |

### Controls

| Control | Purpose |
|---|---|
| `ZoomPanViewer` | image or video frame: wheel zoom around the cursor, drag to pan, fit / 100 %, `VisibleRect`, `IsZoomedIn` |
| `Minimap` | overview of a `ZoomPanViewer` (`Viewport="{Binding #Viewer.VisibleRect}"`); drag inside to navigate (`NavigateRequested` → `Viewer.CenterOn`), double click to switch corner |
| `InitialsBadge` | Rider-like project badge: initials of `Text` (or of a path's last segment) on its stable color, `Size` |

### Dialogs

`IDialogService` / `DialogService(() => ownerWindow)`: `PromptAsync`, `ConfirmAsync` (optionally destructive),
`ShowErrorAsync`, `PickFolderAsync`, `PickFilesAsync`, `SaveFileAsync`. Derive from `DialogService` to add app
dialogs with `ShowDialogAsync<T>(window)`. `PromptWindow` is the underlying window.

### Helpers

- `XdConverters`: `Initials`, `NameColor`, `PathName`, `BoldIfTrue`.
- `Monogram.Of(name)` ("Mon Coffre" → "MC"), `Monogram.NameOfPath(path)`.
- `NamedColors.For(name)`: stable color per name, same palette as chips and badges.

## Xylocopadream.UI.Maui

Controls written in C# (no XAML to load), for Windows (`net11.0-windows10.0.19041.0`); the `net11.0` target only
serves the tests.

### Setup

```csharp
public partial class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        XdTheme.Apply(this, Color.FromArgb("#2F6FEB"));   // colors of the controls, before the first page
        return new Window(new MainPage());
    }
}
```

```xml
<ContentPage xmlns:xd="https://github.com/Zergoma/Xylocopadream.UI" ...>
```

All types are in the `xd:` XAML namespace. `XdTheme.SetAccent(color)` changes the accent color of the whole app at once.

### Colors (`XdTheme`, `XdKeys`)

`XdTheme` writes the colors in the application resources and writes them again at each switch between light and dark:
use them with `{DynamicResource Xd.Accent}` or `SetDynamicResource(..., XdKeys.Accent)`.

| Key | Use |
|---|---|
| `Xd.PanelBackground`, `Xd.PopupBackground` | rails, status bar; popups and tooltips |
| `Xd.Border`, `Xd.Hover` | 1 px outlines; hover shade |
| `Xd.Text`, `Xd.TextSecondary`, `Xd.Danger` | text, muted text, errors |
| `Xd.NeutralButton`, `Xd.NeutralButtonHover`, `Xd.NeutralButtonPressed` | neutral buttons (white text) |
| `Xd.Accent`, `Xd.AccentHover`, `Xd.AccentPressed`, `Xd.AccentForeground` | accent color, its states, the text on it |
| `Xd.AccentSoft`, `Xd.AccentSoftHover`, `Xd.AccentText` | soft selection, accent-colored text |
| `Xd.AccentSurface`, `Xd.AccentOnSurface`, `Xd.AccentGradientEnd` | tinted boxes and their text, end of an accent gradient |
| `Xd.AccentPageBackground`, `Xd.AccentTile`, `Xd.AccentTileHover`, `Xd.AccentTilePressed`, `Xd.AccentTileStroke` | lightly tinted page and tiles |
| `Xd.AccentAlternate1`, `Xd.AccentAlternate2` (+ `Foreground`) | the accent hue turned by +45° / -45°, readable with white text: badges, tags |

The accent variants are lighter on the dark theme. `XdTheme.Light` and `XdTheme.Dark` (`XdPalette` records) can be
replaced to adapt the surfaces to an app.

> On MAUI a dynamic resource set by a trigger is not taken back, and a local value hides it. The controls draw their
> states with stacked layers, each colored once, only shown or hidden: do the same in app templates.

### Icons (`XdIcons`)

Filled 24 × 24 geometries (Google Material Icons, Apache 2.0): `<Path Data="{x:Static xd:XdIcons.Home}" Aspect="Uniform" />`.

Previous, Next, Block, Shuffle, Sun, Moon, Monitor, Play, Home, List, Spellcheck, Upload, Keyboard, BarChart, Palette.
`XdIcons.All` lists them with their names.

### Controls

| Control | Purpose |
|---|---|
| `NavRail` + `NavRailItem` | tool window bar on an edge of the window: `TopItems`, `BottomItems`, `SelectedKey` on the accent color, `ItemClicked`; items can change icon, tooltip and visibility while shown |
| `NavRailButton` | flat 40 px rail button, muted icon, shaded on hover, on the accent color when `IsActive` |
| `RailChoiceGroup` + `RailChoice` | a few linked choices stacked in a rail (e.g. light / dark / system theme), the accent pill sliding to `SelectedIndex` |
| `IconButton` | small round button with an icon, `Command`, toggle with `IsActive`, faded when disabled |
| `SegmentedControl` | options side by side (`ItemsSource`, two-way `SelectedItem`), the accent pill sliding to the selected one; wraps when narrow |
| `InstantToolTip` + `InstantToolTipHost` | tooltip shown at once beside an element: `xd:InstantToolTip.Text="Home" xd:InstantToolTip.Placement="Right"`; put one host over the root grid of the page |
| `Popover` | floating panel closed by a click outside: `IsOpen`, `PanelHorizontalOptions`, `PanelMargin`, `PanelWidth` |
| `ColorPicker` + `ColorPreset` | presets, shade square, hue bar and hex code, reset to `DefaultColor`; two-way `SelectedColor` |
| `WrapPanel` | children from left to right, wrapping (a wrapping `FlexLayout` sometimes squeezed its rows) |

### Helpers

- `XdColorMath`: `NormalizeHex`, `ToHex`, `HexToHsv`, `HsvToHex`.
- `XdAccentPalette`: the accent variants of a theme, `IsLight`, `RelativeLuminance` (WCAG).

## Repository

- `src/` — the packages.
- `samples/Xylocopadream.UI.Avalonia.Gallery` — every Avalonia component on one page: run it to browse them.
- `samples/Xylocopadream.UI.Maui.Gallery` — the same for MAUI (Windows), with light / dark and accent switches.
- `tests/` — Avalonia headless tests, which render the gallery to `bin/Debug/net10.0/screens/`;
  MAUI tests of the colors, icons and color math.

The SDK is pinned by `global.json` (.NET 11 preview, for MAUI 11); the Avalonia projects still target .NET 10.
