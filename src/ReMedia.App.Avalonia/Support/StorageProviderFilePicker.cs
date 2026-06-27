namespace ReMedia.App.Avalonia.Support;

using global::Avalonia.Controls;
using global::Avalonia.Platform.Storage;

/// <summary>
/// <see cref="IFilePicker"/> backed by the Avalonia <see cref="IStorageProvider"/> of a
/// top-level window. Replaces the WPF <c>Microsoft.Win32.OpenFileDialog</c>.
/// </summary>
public sealed class StorageProviderFilePicker : IFilePicker
{
    private readonly TopLevel _topLevel;

    public StorageProviderFilePicker(TopLevel topLevel)
    {
        _topLevel = topLevel;
    }

    public async Task<string?> PickInputFileAsync()
    {
        IReadOnlyList<IStorageFile> files = await _topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Select media file",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Media files")
                    {
                        Patterns = ["*.mkv", "*.mp4", "*.avi", "*.ts", "*.m2ts", "*.flac", "*.wav", "*.ac3", "*.dts", "*.mka"],
                    },
                    FilePickerFileTypes.All,
                ],
            });

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }
}
