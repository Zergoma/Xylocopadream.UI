using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Xylocopadream.UI.Avalonia.Dialogs;

/// <summary>Common dialogs, for view models to use without knowing about windows.</summary>
public interface IDialogService
{
    /// <returns>The entered text, or null when cancelled.</returns>
    Task<string?> PromptAsync(string title, string message, string initialValue = "", string okText = "OK");

    Task<bool> ConfirmAsync(string title, string message, string okText = "OK", bool isDestructive = false);

    Task ShowErrorAsync(string title, string message);

    /// <returns>Local path of the chosen folder, or null.</returns>
    Task<string?> PickFolderAsync(string title);

    /// <returns>Local paths of the chosen files (empty when cancelled).</returns>
    /// <param name="filters">File types to choose from, the first one selected; null for any file.</param>
    Task<IReadOnlyList<string>> PickFilesAsync(string title, bool allowMultiple = true, IReadOnlyList<FileFilter>? filters = null);

    /// <returns>Local path to write to, or null.</returns>
    /// <param name="filters">File types to save as; the first one gives the default extension.</param>
    Task<string?> SaveFileAsync(string title, string suggestedName, IReadOnlyList<FileFilter>? filters = null);
}

/// <summary>
/// Dialogs shown over the window returned by <paramref name="owner"/> (looked up at each call, so it can be
/// created after the service). Derive from it to add application-specific dialogs with <see cref="ShowDialogAsync{T}"/>.
/// </summary>
public class DialogService(Func<Window?> owner) : IDialogService
{
    public Task<string?> PromptAsync(string title, string message, string initialValue = "", string okText = "OK") =>
        ShowDialogAsync<string?>(new PromptWindow(title, message, okText, input: initialValue));

    public async Task<bool> ConfirmAsync(string title, string message, string okText = "OK", bool isDestructive = false) =>
        await ShowDialogAsync<string?>(new PromptWindow(title, message, okText, isDestructive: isDestructive)) is not null;

    public Task ShowErrorAsync(string title, string message) =>
        ShowDialogAsync<string?>(new PromptWindow(title, message, "OK", showCancel: false, isDestructive: true));

    public async Task<string?> PickFolderAsync(string title)
    {
        if (Storage() is not { } storage)
        {
            return null;
        }

        var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = title, AllowMultiple = false });
        return folders.FirstOrDefault()?.TryGetLocalPath();
    }

    public async Task<IReadOnlyList<string>> PickFilesAsync(string title, bool allowMultiple = true, IReadOnlyList<FileFilter>? filters = null)
    {
        if (Storage() is not { } storage)
        {
            return [];
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = allowMultiple,
            FileTypeFilter = filters?.Select(f => f.ToFileType()).ToList(),
        });
        return files.Select(f => f.TryGetLocalPath()).OfType<string>().ToList();
    }

    public async Task<string?> SaveFileAsync(string title, string suggestedName, IReadOnlyList<FileFilter>? filters = null)
    {
        if (Storage() is not { } storage)
        {
            return null;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            ShowOverwritePrompt = true,
            FileTypeChoices = filters?.Select(f => f.ToFileType()).ToList(),
            DefaultExtension = filters?.Select(f => f.DefaultExtension).FirstOrDefault(e => e is not null),
        });
        return file?.TryGetLocalPath();
    }

    /// <summary>Shows <paramref name="dialog"/> modally over the owner; default when there is no owner yet.</summary>
    protected async Task<T?> ShowDialogAsync<T>(Window dialog)
    {
        if (owner() is not { } parent)
        {
            return default;
        }

        return await dialog.ShowDialog<T?>(parent);
    }

    private IStorageProvider? Storage() => owner()?.StorageProvider;
}
