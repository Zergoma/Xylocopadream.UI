# Xylocopadream.UI

Reusable UI components shared across the Xylocopadream applications, one package per framework:

| Package | Status |
|---|---|
| `Xylocopadream.UI.Avalonia` | available |
| `Xylocopadream.UI.Maui` | planned |

The look follows JetBrains Rider (New UI, dark): dense layouts, tool windows, 13 px text, 16 px line icons.

**Rule for every app:** use these components first; when a reusable piece is missing, add it here
rather than in the app. Extract a component once a second app needs it, not before.

## Install

Packages are published to the local NuGet feed `%UserProfile%\nuget_local`, declared once in the user
`NuGet.Config` (source `Local`), so every project on this machine can use it:

```
dotnet add package Xylocopadream.UI.Avalonia
```

## Publish a new version

```
.\publish.ps1 -Bump
```

Increments the patch number of `<Version>` in `Directory.Build.props`, then packs every project of `src/` into the
feed. NuGet caches packages by version, so the script refuses to overwrite a version already published.
Apps then update their `PackageReference` version.

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

## Repository

- `src/` — the packages.
- `samples/Xylocopadream.UI.Avalonia.Gallery` — every component on one page: run it to browse them.
- `tests/` — headless tests; they render the gallery to `bin/Debug/net10.0/screens/`.
