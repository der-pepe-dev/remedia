namespace ReMedia.App.Avalonia.ViewModels;

using System.Collections.ObjectModel;
using System.Text;
using ReMedia.App.Avalonia.Infrastructure;
using ReMedia.App.Avalonia.Support;
using ReMedia.Core.Interfaces;
using ReMedia.Core.Models;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IMediaProbeService _probeService;
    private readonly StringBuilder _logBuilder = new();
    private string _inputPath = string.Empty;
    private string _log = string.Empty;

    public MainWindowViewModel()
        : this(new AppServiceFactory().ProbeService, filePicker: null)
    {
    }

    public MainWindowViewModel(IMediaProbeService probeService, IFilePicker? filePicker)
    {
        _probeService = probeService;
        FilePicker = filePicker;

        ProbeCommand = new AsyncRelayCommand(ProbeAsync, () => !string.IsNullOrWhiteSpace(InputPath));
        BrowseInputCommand = new AsyncRelayCommand(_ => BrowseInputAsync());
    }

    /// <summary>Set by the view once a top-level window (and its StorageProvider) exists.</summary>
    public IFilePicker? FilePicker { get; set; }

    public ObservableCollection<TrackRowViewModel> Tracks { get; } = [];

    public AsyncRelayCommand ProbeCommand { get; }

    public AsyncRelayCommand BrowseInputCommand { get; }

    public string InputPath
    {
        get => _inputPath;
        set
        {
            if (SetProperty(ref _inputPath, value))
            {
                ProbeCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Log
    {
        get => _log;
        private set => SetProperty(ref _log, value);
    }

    private async Task ProbeAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputPath))
        {
            return;
        }

        Tracks.Clear();
        AppendLog($"Probing {InputPath}...");

        try
        {
            MediaProbeResult result = await _probeService.ProbeAsync(InputPath, cancellationToken);

            foreach (MediaStreamInfo stream in result.Streams)
            {
                Tracks.Add(new TrackRowViewModel(stream));
            }

            AppendLog($"Found {result.Streams.Count} stream(s), {result.Chapters.Count} chapter(s). Duration: {result.Duration?.ToString() ?? "unknown"}.");
        }
        catch (Exception ex)
        {
            AppendLog($"Probe failed: {ex.Message}");
        }
    }

    private async Task BrowseInputAsync()
    {
        if (FilePicker is null)
        {
            return;
        }

        string? path = await FilePicker.PickInputFileAsync();
        if (!string.IsNullOrWhiteSpace(path))
        {
            InputPath = path;
        }
    }

    private void AppendLog(string message)
    {
        _logBuilder.AppendLine(message);
        Log = _logBuilder.ToString();
    }
}
