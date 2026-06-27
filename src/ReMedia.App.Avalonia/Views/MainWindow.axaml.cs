namespace ReMedia.App.Avalonia.Views;

using global::Avalonia.Controls;
using ReMedia.App.Avalonia.Support;
using ReMedia.App.Avalonia.ViewModels;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // The StorageProvider is only available once a top-level window exists, so wire the
        // file picker into the ViewModel here.
        if (DataContext is MainWindowViewModel vm)
        {
            vm.FilePicker = new StorageProviderFilePicker(this);
        }
    }
}
