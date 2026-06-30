namespace ReMedia.App.Avalonia.ViewModels;

using System.Collections.ObjectModel;
using System.Text;
using ReMedia.App.Avalonia.Infrastructure;
using ReMedia.App.Avalonia.Support;
using ReMedia.Core.Interfaces;
using ReMedia.Core.Models;
using ReMedia.Core.Services;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IMediaProbeService _probeService;
    private readonly ExportWorkflowService _exportWorkflow;
    private readonly StringBuilder _logBuilder = new();
    private string _inputPath = string.Empty;
    private string _outputFolder = string.Empty;
    private bool _muxToMkv;
    private bool _isBusy;
    private double _progress;
    private string _status = string.Empty;
    private string _log = string.Empty;

    public MainWindowViewModel()
    {
        // Route the workflow's step messages into the log pane.
        AppServiceFactory factory = new(new CallbackToolLogger(AppendLog));
        _probeService = factory.ProbeService;
        _exportWorkflow = factory.ExportWorkflowService;
        (ProbeCommand, ExportCommand, BrowseInputCommand, BrowseOutputCommand) = CreateCommands();
    }

    public MainWindowViewModel(IMediaProbeService probeService, ExportWorkflowService exportWorkflow, IFilePicker? filePicker = null)
    {
        _probeService = probeService;
        _exportWorkflow = exportWorkflow;
        FilePicker = filePicker;
        (ProbeCommand, ExportCommand, BrowseInputCommand, BrowseOutputCommand) = CreateCommands();
    }

    private (AsyncRelayCommand Probe, AsyncRelayCommand Export, AsyncRelayCommand BrowseIn, AsyncRelayCommand BrowseOut) CreateCommands()
    {
        return (
            new AsyncRelayCommand(ProbeAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(InputPath)),
            new AsyncRelayCommand(ExportAsync, CanExport),
            new AsyncRelayCommand(_ => BrowseInputAsync()),
            new AsyncRelayCommand(_ => BrowseOutputAsync()));
    }

    /// <summary>Set by the view once a top-level window (and its StorageProvider) exists.</summary>
    public IFilePicker? FilePicker { get; set; }

    public ObservableCollection<TrackRowViewModel> Tracks { get; } = [];

    public AsyncRelayCommand ProbeCommand { get; }

    public AsyncRelayCommand ExportCommand { get; }

    public AsyncRelayCommand BrowseInputCommand { get; }

    public AsyncRelayCommand BrowseOutputCommand { get; }

    public string InputPath
    {
        get => _inputPath;
        set
        {
            if (SetProperty(ref _inputPath, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public string OutputFolder
    {
        get => _outputFolder;
        set
        {
            if (SetProperty(ref _outputFolder, value))
            {
                ExportCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool MuxToMkv
    {
        get => _muxToMkv;
        set => SetProperty(ref _muxToMkv, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public string Log
    {
        get => _log;
        private set => SetProperty(ref _log, value);
    }

    private bool CanExport()
    {
        return !IsBusy
            && !string.IsNullOrWhiteSpace(InputPath)
            && !string.IsNullOrWhiteSpace(OutputFolder)
            && Tracks.Any(t => t is { Selected: true, IsExportable: true });
    }

    private async Task ProbeAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputPath))
        {
            return;
        }

        IsBusy = true;
        try
        {
            Tracks.Clear();
            AppendLog($"Probing {InputPath}...");

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
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportAsync(CancellationToken cancellationToken)
    {
        if (!CanExport())
        {
            return;
        }

        IsBusy = true;
        Progress = 0;
        try
        {
            List<ExportTrackSelection> selections = Tracks
                .Where(t => t is { Selected: true, IsExportable: true })
                .Select(t => BuildSelection(t.Stream))
                .ToList();

            ExportWorkflowRequest request = new(
                InputPath,
                OutputFolder,
                selections,
                ExportChapters: false,
                Chapters: [],
                MuxToMkv: MuxToMkv);

            AppendLog($"Exporting {selections.Count} track(s) to {OutputFolder}{(MuxToMkv ? " (mux)" : string.Empty)}...");

            Progress<WorkflowProgress> progress = new(p =>
            {
                Progress = p.Percentage;
                Status = $"{p.StepName} ({p.CurrentStep}/{p.TotalSteps})";
            });

            ExportWorkflowResult result = await _exportWorkflow.ExecuteAsync(request, cancellationToken, progress);

            foreach (ToolOperationResult track in result.TrackResults)
            {
                AppendLog($"  {(track.Succeeded ? "OK" : "FAILED")}  {track.OutputPath}");
            }

            if (result.MuxResult is not null)
            {
                AppendLog($"  Mux: {(result.MuxResult.Succeeded ? "OK" : "FAILED")}  {result.MuxResult.OutputPath}");
            }

            Status = result.HasFailures ? "Export completed with errors." : "Export completed.";
            AppendLog(Status);
        }
        catch (OperationCanceledException)
        {
            AppendLog("Export canceled.");
            Status = "Export canceled.";
        }
        catch (Exception ex)
        {
            AppendLog($"Export failed: {ex.Message}");
            Status = "Export failed.";
        }
        finally
        {
            IsBusy = false;
            Progress = 0;
        }
    }

    private static ExportTrackSelection BuildSelection(MediaStreamInfo stream)
    {
        string container = ContainerDefaults.GetDefaultContainer(stream.AssetType, stream.CodecName);
        return new ExportTrackSelection(
            stream.Index,
            stream.AssetType,
            stream.CodecName,
            OutputCodec: "copy",
            OutputContainer: container,
            CopyStream: true);
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

    private async Task BrowseOutputAsync()
    {
        if (FilePicker is null)
        {
            return;
        }

        string? path = await FilePicker.PickFolderAsync();
        if (!string.IsNullOrWhiteSpace(path))
        {
            OutputFolder = path;
        }
    }

    private void RaiseCommandStates()
    {
        ProbeCommand.RaiseCanExecuteChanged();
        ExportCommand.RaiseCanExecuteChanged();
    }

    private void AppendLog(string message)
    {
        _logBuilder.AppendLine(message);
        Log = _logBuilder.ToString();
    }
}
