using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Xylocopadream.UI.Avalonia.Converters;

/// <summary>Converters for bindings: <c>Converter={x:Static xd:XdConverters.Initials}</c>.</summary>
public static class XdConverters
{
    /// <summary>Two-letter badge of a name or of the last segment of a path (see <see cref="Monogram.Of"/>).</summary>
    public static readonly IValueConverter Initials =
        new FuncValueConverter<string, string>(text => Monogram.Of(Monogram.NameOfPath(text ?? string.Empty)));

    /// <summary>Stable color of a name (see <see cref="NamedColors"/>).</summary>
    public static readonly IValueConverter NameColor =
        new FuncValueConverter<string, IBrush>(text => NamedColors.For(text ?? string.Empty));

    /// <summary>Last segment of a path: "C:\Vaults\Photos" → "Photos".</summary>
    public static readonly IValueConverter PathName =
        new FuncValueConverter<string, string>(path => Monogram.NameOfPath(path ?? string.Empty));

    public static readonly IValueConverter BoldIfTrue =
        new FuncValueConverter<bool, FontWeight>(value => value ? FontWeight.SemiBold : FontWeight.Normal);
}
