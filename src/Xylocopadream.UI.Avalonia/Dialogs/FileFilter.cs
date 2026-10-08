using Avalonia.Platform.Storage;

namespace Xylocopadream.UI.Avalonia.Dialogs;

/// <summary>A file type offered by the file pickers, e.g. <c>new FileFilter("Coffres", "*.xvault")</c>.</summary>
/// <param name="Patterns">Glob patterns such as "*.png"; "*" for any file.</param>
public sealed record FileFilter(string Name, params string[] Patterns)
{
    /// <summary>Any file, to add after specific filters so the user can still pick something else.</summary>
    public static FileFilter AllFiles { get; } = new("Tous les fichiers", "*");

    internal FilePickerFileType ToFileType() => new(Name) { Patterns = Patterns };

    /// <summary>Extension of the first specific pattern ("*.xvault" → "xvault"), or null.</summary>
    internal string? DefaultExtension =>
        Patterns.FirstOrDefault(p => p.StartsWith("*.", StringComparison.Ordinal) && p.Length > 2)?[2..];
}
