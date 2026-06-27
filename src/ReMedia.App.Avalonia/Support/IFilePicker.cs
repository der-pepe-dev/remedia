namespace ReMedia.App.Avalonia.Support;

/// <summary>
/// UI-framework-agnostic file picker so ViewModels stay testable and free of Avalonia
/// types. The Avalonia implementation wraps the window's StorageProvider.
/// </summary>
public interface IFilePicker
{
    Task<string?> PickInputFileAsync();
}
