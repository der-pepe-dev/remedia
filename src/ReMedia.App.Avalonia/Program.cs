namespace ReMedia.App.Avalonia;

using global::Avalonia;

internal static class Program
{
    // Avalonia configuration; the entry point must not use any Avalonia/UI types before
    // AppBuilder is created (initialization order matters).
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
