namespace ReMedia.App.ViewModels;

using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.IO;
using ReMedia.App.Support;
using ReMedia.App.Windows;
using ReMedia.Core.Interfaces;
using ReMedia.Core.Models;
using ReMedia.Core.Services;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IMediaProbeService _probeService;
    private readonly ITimingAnalysisService _timingService;
    private readonly ILoudnessService _loudnessService;
    private readonly IAudioSyncService _audioSyncService;
    private readonly ExportWorkflowService _exportWorkflow;

    private string _inputPath = string.Empty;
    private string _sourceFpsText = "25";
    private FpsPreset? _selectedSourceFpsPreset;
    private string _targetFpsText = "23.976023976";
    private FpsPreset? _selectedTargetFpsPreset;
    private string _originalDurationText = "00:00:00";
    private string _destinationDurationText = "00:00:00";
    private string _stretchFactorText = string.Empty;
    private string _audioTempoText = string.Empty;
    private string _probeSummary = "No file probed yet.";
    private string _outputFolder = Environment.CurrentDirectory;
    private string _logText = "Ready.";
    private readonly StringBuilder _logBuilder = new("Ready.");
    private bool _exportChapters = true;
    private bool _applyTiming;
    private bool _muxToMkv;
    private string _destinationMasterPath = string.Empty;
    private bool _normalizeLoudness;
    private string _targetLufsText = "-24";
    private bool _isBusy;
    private bool _isProgressIndeterminate;
    private double _progressValue;
    private double _progressMax = 1;
    private string _statusText = string.Empty;
    private MediaProbeResult? _lastProbeResult;
    private MediaProbeResult? _lastDestinationProbeResult;
    private TimingAnalysisResult? _lastTimingResult;
    private double? _detectedSyncOffsetMs;
    private string _syncOffsetText = "Not measured";

    public MainWindowViewModel()
    {
        CallbackToolLogger logger = new(AppendLog);
        AppServiceFactory factory = new(logger);

        _probeService = factory.ProbeService;
        _timingService = factory.TimingAnalysisService;
        _loudnessService = factory.LoudnessService;
        _audioSyncService = factory.AudioSyncService;
        _exportWorkflow = factory.ExportWorkflowService;

        Tracks = [];
        InputParts = [];

        ProbeCommand = new AsyncRelayCommand(ProbeAsync);
        CalculateTimingCommand = new RelayCommand(CalculateTiming);
        ExportCommand = new AsyncRelayCommand(ExportAsync);
        CancelCommand = new RelayCommand(Cancel);
        BrowseInputCommand = new RelayCommand(BrowseInput);
        BrowseOutputCommand = new RelayCommand(BrowseOutput);
        SelectAllTracksCommand = new RelayCommand(SelectAllTracks);
        DeselectAllTracksCommand = new RelayCommand(DeselectAllTracks);
        ClearLogCommand = new RelayCommand(ClearLog);
        MeasureLoudnessCommand = new AsyncRelayCommand(MeasureLoudnessAsync);
        BrowseDestinationCommand = new RelayCommand(BrowseDestination);
        ProbeDestinationCommand = new AsyncRelayCommand(ProbeDestinationAsync);
        MeasureDestinationLoudnessCommand = new AsyncRelayCommand(MeasureDestinationLoudnessAsync);
        AnalyzeSyncCommand = new AsyncRelayCommand(AnalyzeSyncAsync);
        AddPartCommand = new RelayCommand(AddPart);
        RemovePartCommand = new RelayCommand(RemovePart);

        _selectedTargetFpsPreset = TargetFpsPresets.FirstOrDefault(p => p.Value == 24000m / 1001m);
        _selectedSourceFpsPreset = SourceFpsPresets.FirstOrDefault(p => p.Value == 25m);
    }

    public ObservableCollection<TrackRowViewModel> Tracks { get; }
    public ObservableCollection<string> InputParts { get; }

    public AsyncRelayCommand ProbeCommand { get; }
    public RelayCommand CalculateTimingCommand { get; }
    public AsyncRelayCommand ExportCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand BrowseInputCommand { get; }
    public RelayCommand BrowseOutputCommand { get; }
    public RelayCommand SelectAllTracksCommand { get; }
    public RelayCommand DeselectAllTracksCommand { get; }
    public RelayCommand ClearLogCommand { get; }
    public AsyncRelayCommand MeasureLoudnessCommand { get; }
    public RelayCommand BrowseDestinationCommand { get; }
    public AsyncRelayCommand ProbeDestinationCommand { get; }
    public AsyncRelayCommand MeasureDestinationLoudnessCommand { get; }
    public AsyncRelayCommand AnalyzeSyncCommand { get; }
    public RelayCommand AddPartCommand { get; }
    public RelayCommand RemovePartCommand { get; }

    public string InputPath
    {
        get => _inputPath;
        set => SetProperty(ref _inputPath, value);
    }

    public string SourceFpsText
    {
        get => _sourceFpsText;
        set => SetProperty(ref _sourceFpsText, value);
    }

    public IReadOnlyList<FpsPreset> SourceFpsPresets { get; } = FpsPresets.All;

    public FpsPreset? SelectedSourceFpsPreset
    {
        get => _selectedSourceFpsPreset;
        set
        {
            if (SetProperty(ref _selectedSourceFpsPreset, value) && value is not null)
            {
                SourceFpsText = value.ValueText;
            }
        }
    }

    public string TargetFpsText
    {
        get => _targetFpsText;
        set => SetProperty(ref _targetFpsText, value);
    }

    public IReadOnlyList<FpsPreset> TargetFpsPresets { get; } = FpsPresets.All;

    public FpsPreset? SelectedTargetFpsPreset
    {
        get => _selectedTargetFpsPreset;
        set
        {
            if (SetProperty(ref _selectedTargetFpsPreset, value) && value is not null)
            {
                TargetFpsText = value.ValueText;
            }
        }
    }

    public string OriginalDurationText
    {
        get => _originalDurationText;
        set => SetProperty(ref _originalDurationText, value);
    }

    public string DestinationDurationText
    {
        get => _destinationDurationText;
        set => SetProperty(ref _destinationDurationText, value);
    }

    public string StretchFactorText
    {
        get => _stretchFactorText;
        set => SetProperty(ref _stretchFactorText, value);
    }

    public string AudioTempoText
    {
        get => _audioTempoText;
        set => SetProperty(ref _audioTempoText, value);
    }

    public string ProbeSummary
    {
        get => _probeSummary;
        set => SetProperty(ref _probeSummary, value);
    }

    public string OutputFolder
    {
        get => _outputFolder;
        set => SetProperty(ref _outputFolder, value);
    }

    public string LogText
    {
        get => _logText;
        set => SetProperty(ref _logText, value);
    }

    public bool ExportChapters
    {
        get => _exportChapters;
        set => SetProperty(ref _exportChapters, value);
    }

    public bool ApplyTiming
    {
        get => _applyTiming;
        set => SetProperty(ref _applyTiming, value);
    }

    public bool MuxToMkv
    {
        get => _muxToMkv;
        set => SetProperty(ref _muxToMkv, value);
    }

    public string DestinationMasterPath
    {
        get => _destinationMasterPath;
        set
        {
            if (SetProperty(ref _destinationMasterPath, value))
            {
                _lastDestinationProbeResult = null;
            }
        }
    }

    public bool NormalizeLoudness
    {
        get => _normalizeLoudness;
        set
        {
            if (SetProperty(ref _normalizeLoudness, value))
            {
                RecalculateGainAndClipping();
            }
        }
    }

    public string TargetLufsText
    {
        get => _targetLufsText;
        set
        {
            if (SetProperty(ref _targetLufsText, value))
            {
                RecalculateGainAndClipping();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool IsProgressIndeterminate
    {
        get => _isProgressIndeterminate;
        private set => SetProperty(ref _isProgressIndeterminate, value);
    }

    public double ProgressValue
    {
        get => _progressValue;
        private set => SetProperty(ref _progressValue, value);
    }

    public double ProgressMax
    {
        get => _progressMax;
        private set => SetProperty(ref _progressMax, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string SyncOffsetText
    {
        get => _syncOffsetText;
        private set => SetProperty(ref _syncOffsetText, value);
    }

    private async Task ProbeAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputPath))
        {
            AppendLog("[probe] No input path specified.");
            return;
        }

        AppendLog($"[probe] {InputPath}");
        IsBusy = true;
        ResetProgress();
        IsProgressIndeterminate = true;
        StatusText = "Probing...";

        try
        {
            MediaProbeResult result;

            if (InputParts.Count > 0)
            {
                AppendLog($"[probe] Multi-part source: {InputParts.Count + 1} file(s) total.");
                List<string> allPaths = [InputPath, .. InputParts];
                MultiPartProbeResult multiResult = await MultiPartProbeHelper.ProbeAllAsync(_probeService, allPaths, cancellationToken);
                result = multiResult.Combined;

                for (int i = 0; i < multiResult.PartDurations.Count; i++)
                {
                    string name = i == 0 ? Path.GetFileName(InputPath) : Path.GetFileName(InputParts[i - 1]);
                    AppendLog($"[probe]   Part {i + 1}: {name} ({multiResult.PartDurations[i]})");
                }

                AppendLog($"[probe]   Combined: {result.Duration}, {result.Chapters.Count} chapter(s)");
            }
            else
            {
                result = await _probeService.ProbeAsync(InputPath, cancellationToken);
            }

            _lastProbeResult = result;

            ProbeSummary = BuildProbeSummary(result);

            OriginalDurationText = result.Duration?.ToString() ?? "00:00:00";

            MediaStreamInfo? videoStream = result.GetPrimaryVideoStream();
            if (videoStream is not null && videoStream.AverageFramesPerSecond > 0)
            {
                FpsPreset? matchedPreset = SourceFpsPresets.FirstOrDefault(p => p.Value == videoStream.AverageFramesPerSecond);
                if (matchedPreset is not null)
                {
                    SelectedSourceFpsPreset = matchedPreset;
                }
                else
                {
                    SourceFpsText = videoStream.AverageFramesPerSecond.ToString(CultureInfo.InvariantCulture);
                }
            }

            Tracks.Clear();
            foreach (MediaStreamInfo stream in result.Streams)
            {
                string defaultContainer = ContainerDefaults.GetDefaultContainer(stream.AssetType, stream.CodecName);
                Tracks.Add(new TrackRowViewModel
                {
                    StreamIndex = stream.Index,
                    AssetType = stream.AssetType,
                    CodecName = stream.CodecName,
                    Language = stream.Language,
                    Title = stream.Title,
                    IsDefault = stream.Default,
                    IsForced = stream.Forced,
                    Channels = stream.Channels,
                    SampleRate = stream.SampleRate,
                    Selected = stream.AssetType is MediaAssetType.Audio or MediaAssetType.Subtitle,
                    OutputCodec = "copy",
                    OutputContainer = defaultContainer
                });
            }

            AppendLog($"[probe] Found {result.Streams.Count} stream(s), {result.Chapters.Count} chapter(s).");
            StatusText = "Probe complete";
            CalculateTiming();
        }
        catch (OperationCanceledException)
        {
            AppendLog("[probe] Cancelled.");
        }
        catch (Exception ex)
        {
            ProbeSummary = $"Probe failed: {ex.Message}";
            AppendLog($"[probe] Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CalculateTiming()
    {
        if (!decimal.TryParse(SourceFpsText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal sourceFps))
        {
            AppendLog("[timing] Invalid source FPS.");
            return;
        }

        if (!decimal.TryParse(TargetFpsText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal targetFps))
        {
            AppendLog("[timing] Invalid target FPS.");
            return;
        }

        TimeSpan duration = TimeSpan.TryParse(OriginalDurationText, CultureInfo.InvariantCulture, out TimeSpan parsed)
            ? parsed
            : TimeSpan.Zero;

        TimingAnalysisResult result = _timingService.Analyze(new TimingAnalysisRequest(duration, sourceFps, targetFps));
        _lastTimingResult = result;

        DestinationDurationText = result.DestinationDuration.ToString();
        StretchFactorText = result.StretchFactor.ToString("0.################", CultureInfo.InvariantCulture);
        AudioTempoText = result.AudioTempoFactor.ToString("0.################", CultureInfo.InvariantCulture);
        AppendLog($"[timing] stretch={StretchFactorText} tempo={AudioTempoText}");
    }

    private async Task ExportAsync(CancellationToken cancellationToken)
    {
        List<TrackRowViewModel> selectedTracks = Tracks.Where(t => t.Selected).ToList();

        if (selectedTracks.Count == 0 && !ExportChapters)
        {
            AppendLog("[export] No tracks selected and chapter export is disabled.");
            return;
        }

        if (string.IsNullOrWhiteSpace(InputPath))
        {
            AppendLog("[export] No input path specified.");
            return;
        }

        if (ApplyTiming && _lastTimingResult is null)
        {
            AppendLog("[export] Please calculate timing before exporting with timing conversion.");
            return;
        }

        List<ExportTrackSelection> selections = selectedTracks
            .Where(t => t.AssetType is not MediaAssetType.Video)
            .Select(t =>
            {
                bool copyStream = string.Equals(t.OutputCodec, "copy", StringComparison.OrdinalIgnoreCase);

                if (ApplyTiming && t.AssetType == MediaAssetType.Audio && copyStream)
                {
                    AppendLog($"[export] Warning: stream {t.StreamIndex} uses codec copy but timing conversion requires re-encoding. Falling back to flac.");
                    return new ExportTrackSelection(
                        t.StreamIndex, t.AssetType, t.CodecName,
                        "flac", ".flac", CopyStream: false,
                        AppliedGainDb: NormalizeLoudness ? t.GainDb : null,
                        AudioSyncOffsetMs: (_detectedSyncOffsetMs.HasValue && t.AssetType == MediaAssetType.Audio)
                            ? (decimal)_detectedSyncOffsetMs.Value
                            : null);
                }

                bool needsReEncode = NormalizeLoudness && t.GainDb.HasValue && t.GainDb.Value != 0m && t.AssetType == MediaAssetType.Audio;
                if (needsReEncode && copyStream)
                {
                    AppendLog($"[export] Warning: stream {t.StreamIndex} uses codec copy but loudness normalization requires re-encoding. Falling back to flac.");
                    return new ExportTrackSelection(
                        t.StreamIndex, t.AssetType, t.CodecName,
                        "flac", ".flac", CopyStream: false,
                        AppliedGainDb: t.GainDb,
                        AudioSyncOffsetMs: (_detectedSyncOffsetMs.HasValue && t.AssetType == MediaAssetType.Audio)
                            ? (decimal)_detectedSyncOffsetMs.Value
                            : null);
                }

                return new ExportTrackSelection(
                    t.StreamIndex, t.AssetType, t.CodecName,
                    t.OutputCodec, t.OutputContainer, copyStream,
                    AppliedGainDb: NormalizeLoudness ? t.GainDb : null,
                    AudioSyncOffsetMs: (_detectedSyncOffsetMs.HasValue && t.AssetType == MediaAssetType.Audio)
                        ? (decimal)_detectedSyncOffsetMs.Value
                        : null);
            })
            .ToList();

        ExportWorkflowRequest request = new(
            InputPath,
            OutputFolder,
            selections,
            ExportChapters,
            _lastProbeResult?.Chapters ?? [],
            ApplyTiming ? _lastTimingResult?.SourceFps : null,
            ApplyTiming ? _lastTimingResult?.TargetFps : null,
            MuxToMkv: MuxToMkv,
            DestinationMasterPath: string.IsNullOrWhiteSpace(DestinationMasterPath) ? null : DestinationMasterPath,
            AdditionalInputPaths: InputParts.Count > 0 ? [.. InputParts] : null);

        IsBusy = true;
        ResetProgress();
        try
        {
            Progress<WorkflowProgress> progress = new(OnWorkflowProgress);
            ExportWorkflowResult result = await _exportWorkflow.ExecuteAsync(request, cancellationToken, progress);

            foreach (ToolOperationResult trackResult in result.TrackResults)
            {
                LogOperationResult(trackResult);
            }

            if (result.ChapterResult is not null)
            {
                LogOperationResult(result.ChapterResult);
            }

            if (result.MuxResult is not null)
            {
                LogOperationResult(result.MuxResult);
            }

            AppendLog(result.HasFailures ? "[export] Completed with errors." : "[export] Done.");
            StatusText = result.HasFailures ? "Completed with errors" : "Done";
        }
        catch (OperationCanceledException)
        {
            AppendLog("[export] Cancelled.");
        }
        catch (Exception ex)
        {
            AppendLog($"[export] Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BrowseInput()
    {
        Microsoft.Win32.OpenFileDialog dialog = new()
        {
            Title = "Select media file",
            Filter = "Media files|*.mkv;*.mp4;*.avi;*.ts;*.m2ts;*.flac;*.wav;*.ac3;*.dts;*.mka|All files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            InputPath = dialog.FileName;
        }
    }

    private void BrowseOutput()
    {
        Microsoft.Win32.OpenFolderDialog dialog = new()
        {
            Title = "Select output folder"
        };

        if (dialog.ShowDialog() == true)
        {
            OutputFolder = dialog.FolderName;
        }
    }

    private void BrowseDestination()
    {
        Microsoft.Win32.OpenFileDialog dialog = new()
        {
            Title = "Select destination master file",
            Filter = "MKV files|*.mkv|MP4 files|*.mp4|All files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            DestinationMasterPath = dialog.FileName;
        }
    }

    private async Task ProbeDestinationAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(DestinationMasterPath))
        {
            AppendLog("[dest probe] No destination path specified.");
            return;
        }

        AppendLog($"[dest probe] {DestinationMasterPath}");
        IsBusy = true;
        IsProgressIndeterminate = true;
        StatusText = "Probing destination...";

        try
        {
            MediaProbeResult result = await _probeService.ProbeAsync(DestinationMasterPath, cancellationToken);
            _lastDestinationProbeResult = result;

            if (result.Duration.HasValue)
            {
                AppendLog($"[dest probe] Duration: {result.Duration}");
            }

            MediaStreamInfo? videoStream = result.GetPrimaryVideoStream();
            if (videoStream is not null && videoStream.AverageFramesPerSecond > 0)
            {
                FpsPreset? matchedPreset = TargetFpsPresets.FirstOrDefault(p => p.Value == videoStream.AverageFramesPerSecond);
                if (matchedPreset is not null)
                {
                    SelectedTargetFpsPreset = matchedPreset;
                }
                else
                {
                    TargetFpsText = videoStream.AverageFramesPerSecond.ToString(CultureInfo.InvariantCulture);
                }

                AppendLog($"[dest probe] Target FPS set to {TargetFpsText}");
                CalculateTiming();
            }
            else
            {
                AppendLog("[dest probe] No video stream found; target FPS unchanged.");
            }

            StatusText = "Destination probe complete";
        }
        catch (OperationCanceledException)
        {
            AppendLog("[dest probe] Cancelled.");
        }
        catch (Exception ex)
        {
            AppendLog($"[dest probe] Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            IsProgressIndeterminate = false;
        }
    }

    private async Task MeasureDestinationLoudnessAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(DestinationMasterPath))
        {
            AppendLog("[dest loudness] No destination path specified.");
            return;
        }

        IsBusy = true;
        IsProgressIndeterminate = true;
        StatusText = "Analyzing destination loudness...";

        try
        {
            if (_lastDestinationProbeResult is null)
            {
                AppendLog($"[dest loudness] Probing {DestinationMasterPath}");
                _lastDestinationProbeResult = await _probeService.ProbeAsync(DestinationMasterPath, cancellationToken);
            }

            MediaStreamInfo? audioStream = _lastDestinationProbeResult.Streams
                .FirstOrDefault(s => s.AssetType == MediaAssetType.Audio);

            if (audioStream is null)
            {
                AppendLog("[dest loudness] No audio stream found in destination.");
                return;
            }

            AppendLog($"[dest loudness] Analyzing stream {audioStream.Index} ({audioStream.CodecName}, {audioStream.Language ?? "und"})...");
            LoudnessAnalysisResult loudness = await _loudnessService.AnalyzeAsync(DestinationMasterPath, audioStream.Index, cancellationToken);

            if (!loudness.IntegratedLufs.HasValue)
            {
                AppendLog("[dest loudness] Could not measure integrated loudness.");
                return;
            }

            string lufsStr = loudness.IntegratedLufs.Value.ToString("0.#", CultureInfo.InvariantCulture);
            AppendLog($"[dest loudness] Integrated: {lufsStr} LUFS");

            TargetLufsText = lufsStr;
            NormalizeLoudness = true;

            StatusText = "Destination loudness measured";
        }
        catch (OperationCanceledException)
        {
            AppendLog("[dest loudness] Cancelled.");
        }
        catch (Exception ex)
        {
            AppendLog($"[dest loudness] Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            IsProgressIndeterminate = false;
        }
    }

    private async Task AnalyzeSyncAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputPath))
        {
            AppendLog("[sync] No source path specified.");
            return;
        }

        if (string.IsNullOrWhiteSpace(DestinationMasterPath))
        {
            AppendLog("[sync] No destination path specified.");
            return;
        }

        MediaStreamInfo? sourceAudio = _lastProbeResult?.Streams
            .FirstOrDefault(s => s.AssetType == MediaAssetType.Audio);

        if (sourceAudio is null)
        {
            AppendLog("[sync] No audio stream found in source. Probe source first.");
            return;
        }

        if (_lastDestinationProbeResult is null)
        {
            AppendLog($"[sync] Probing destination...");
            _lastDestinationProbeResult = await _probeService.ProbeAsync(DestinationMasterPath, cancellationToken);
        }

        MediaStreamInfo? destAudio = _lastDestinationProbeResult.Streams
            .FirstOrDefault(s => s.AssetType == MediaAssetType.Audio);

        if (destAudio is null)
        {
            AppendLog("[sync] No audio stream found in destination.");
            return;
        }

        decimal stretchFactor = _lastTimingResult?.StretchFactor ?? 1m;

        AppendLog($"[sync] Extracting peaks from source stream {sourceAudio.Index} and dest stream {destAudio.Index}...");
        AppendLog($"[sync] Stretch factor: {stretchFactor:0.################}");

        IsBusy = true;
        IsProgressIndeterminate = true;
        StatusText = "Analyzing audio sync...";

        try
        {
            AudioSyncAnalysisResult result = await _audioSyncService.AnalyzeSyncAsync(
                InputPath, sourceAudio.Index,
                DestinationMasterPath, destAudio.Index,
                stretchFactor,
                cancellationToken);

            _detectedSyncOffsetMs = result.DetectedOffset.TotalMilliseconds;

            string sign = _detectedSyncOffsetMs >= 0 ? "+" : string.Empty;
            string confidence = $"{result.Confidence * 100:0}%";
            SyncOffsetText = $"{sign}{_detectedSyncOffsetMs:0} ms (confidence: {confidence}, {result.SourcePeaks.Count} src peaks, {result.DestinationPeaks.Count} dst peaks)";

            AppendLog($"[sync] Detected offset: {sign}{_detectedSyncOffsetMs:0} ms | confidence: {confidence}");
            AppendLog($"[sync] Source peaks: {result.SourcePeaks.Count}, dest peaks: {result.DestinationPeaks.Count}");

            StatusText = "Sync analysis complete";
        }
        catch (OperationCanceledException)
        {
            AppendLog("[sync] Cancelled.");
        }
        catch (Exception ex)
        {
            AppendLog($"[sync] Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            IsProgressIndeterminate = false;
        }
    }

    private void AddPart()
    {
        Microsoft.Win32.OpenFileDialog dialog = new()
        {
            Title = "Add source part",
            Filter = "Media files|*.mkv;*.mp4;*.avi;*.ts;*.m2ts|All files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            InputParts.Add(dialog.FileName);
        }
    }

    private void RemovePart()
    {
        if (InputParts.Count > 0)
        {
            InputParts.RemoveAt(InputParts.Count - 1);
        }
    }

    private void SelectAllTracks()
    {
        foreach (TrackRowViewModel track in Tracks)
        {
            if (track.AssetType is MediaAssetType.Audio or MediaAssetType.Subtitle)
            {
                track.Selected = true;
            }
        }
    }

    private void DeselectAllTracks()
    {
        foreach (TrackRowViewModel track in Tracks)
        {
            track.Selected = false;
        }
    }

    private async Task MeasureLoudnessAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputPath))
        {
            AppendLog("[loudness] No input path specified.");
            return;
        }

        List<TrackRowViewModel> audioTracks = Tracks
            .Where(t => t.Selected && t.AssetType == MediaAssetType.Audio)
            .ToList();

        if (audioTracks.Count == 0)
        {
            AppendLog("[loudness] No audio tracks selected.");
            return;
        }

        IsBusy = true;
        ResetProgress();
        ProgressMax = audioTracks.Count;
        try
        {
            for (int idx = 0; idx < audioTracks.Count; idx++)
            {
                TrackRowViewModel track = audioTracks[idx];
                cancellationToken.ThrowIfCancellationRequested();

                ProgressValue = idx;
                StatusText = $"Analyzing stream {track.StreamIndex} ({idx + 1}/{audioTracks.Count})";
                AppendLog($"[loudness] Analyzing stream {track.StreamIndex} ({track.CodecName}, {track.Language ?? "und"})...");
                LoudnessAnalysisResult loudness = await _loudnessService.AnalyzeAsync(InputPath, track.StreamIndex, cancellationToken);

                string integrated = loudness.IntegratedLufs.HasValue
                    ? $"{loudness.IntegratedLufs:0.#} LUFS" : "N/A";
                string truePeak = loudness.TruePeakDbtp.HasValue
                    ? $"{loudness.TruePeakDbtp:0.#} dBTP" : "N/A";
                string samplePeak = loudness.SamplePeakDbfs.HasValue
                    ? $"{loudness.SamplePeakDbfs:0.#} dBFS" : "N/A";
                string lra = loudness.LoudnessRange.HasValue
                    ? $"{loudness.LoudnessRange:0.#} LU" : "N/A";

                AppendLog($"[loudness]   Integrated: {integrated}  LRA: {lra}");
                AppendLog($"[loudness]   True peak: {truePeak}  Sample peak: {samplePeak}");

                track.LoudnessDisplay = integrated;
                track.MeasuredLufs = loudness.IntegratedLufs;
                track.LastLoudness = loudness;

                UpdateTrackGainAndClipping(track, loudness);
            }

            ProgressValue = audioTracks.Count;
            StatusText = "Loudness analysis complete";
        }
        catch (OperationCanceledException)
        {
            AppendLog("[loudness] Cancelled.");
        }
        catch (Exception ex)
        {
            AppendLog($"[loudness] Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RecalculateGainAndClipping()
    {
        if (!decimal.TryParse(TargetLufsText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal targetLufs))
        {
            return;
        }

        foreach (TrackRowViewModel track in Tracks)
        {
            if (track.AssetType != MediaAssetType.Audio || !track.MeasuredLufs.HasValue)
            {
                continue;
            }

            if (NormalizeLoudness)
            {
                decimal gain = targetLufs - track.MeasuredLufs.Value;
                track.GainDb = gain;

                LoudnessAnalysisResult loudness = track.LastLoudness
                    ?? new LoudnessAnalysisResult(track.MeasuredLufs, null, null, null);
                ClippingPredictionResult clip = _loudnessService.PredictClipping(loudness, gain);
                track.ClippingDisplay = clip.Danger ? "⚠ CLIP" : clip.Warning ? "⚠ Near" : $"{gain:+0.#;-0.#;0}dB";
            }
            else
            {
                track.GainDb = null;
                track.ClippingDisplay = string.Empty;
            }
        }
    }

    private void UpdateTrackGainAndClipping(TrackRowViewModel track, LoudnessAnalysisResult loudness)
    {
        if (!NormalizeLoudness || !loudness.IntegratedLufs.HasValue)
        {
            track.GainDb = null;
            track.ClippingDisplay = string.Empty;
            return;
        }

        if (!decimal.TryParse(TargetLufsText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal targetLufs))
        {
            return;
        }

        decimal gain = targetLufs - loudness.IntegratedLufs.Value;
        track.GainDb = gain;

        ClippingPredictionResult clip = _loudnessService.PredictClipping(loudness, gain);
        track.ClippingDisplay = clip.Danger ? "⚠ CLIP" : clip.Warning ? "⚠ Near" : $"{gain:+0.#;-0.#;0}dB";

        if (clip.Danger)
        {
            AppendLog($"[loudness]   ⚠ CLIPPING: {clip.Message}");
        }
        else if (clip.Warning)
        {
            AppendLog($"[loudness]   ⚠ Near clipping: {clip.Message}");
        }
    }

    private void Cancel()
    {
        if (ProbeCommand.IsExecuting)
        {
            ProbeCommand.Cancel();
        }

        if (ProbeDestinationCommand.IsExecuting)
        {
            ProbeDestinationCommand.Cancel();
        }

        if (MeasureDestinationLoudnessCommand.IsExecuting)
        {
            MeasureDestinationLoudnessCommand.Cancel();
        }

        if (ExportCommand.IsExecuting)
        {
            ExportCommand.Cancel();
        }

        if (MeasureLoudnessCommand.IsExecuting)
        {
            MeasureLoudnessCommand.Cancel();
        }

        if (AnalyzeSyncCommand.IsExecuting)
        {
            AnalyzeSyncCommand.Cancel();
        }
    }

    private void LogOperationResult(ToolOperationResult opResult)
    {
        string status = opResult.Succeeded ? "OK" : $"FAILED (exit {opResult.ExitCode})";
        string duration = opResult.Duration > TimeSpan.Zero ? $" ({opResult.Duration.TotalSeconds:0.##}s)" : string.Empty;

        AppendLog($"[export] [{opResult.OperationName}] {status}{duration} -> {opResult.OutputPath}");
        AppendLog($"[export]   cmd: {opResult.GeneratedCommand}");

        if (!opResult.Succeeded && opResult.ErrorDetail is not null)
        {
            AppendLog($"[export]   error: {opResult.ErrorDetail}");
        }
    }

    private void AppendLog(string message)
    {
        _logBuilder.AppendLine();
        _logBuilder.Append(message);
        LogText = _logBuilder.ToString();
    }

    private void ResetProgress()
    {
        ProgressValue = 0;
        ProgressMax = 1;
        StatusText = string.Empty;
        IsProgressIndeterminate = false;
    }

    private void OnWorkflowProgress(WorkflowProgress p)
    {
        ProgressValue = p.CurrentStep;
        ProgressMax = p.TotalSteps;
        StatusText = $"{p.StepName} ({p.CurrentStep}/{p.TotalSteps})";
    }

    private void ClearLog()
    {
        _logBuilder.Clear();
        _logBuilder.Append("Ready.");
        LogText = _logBuilder.ToString();
    }

    private static string BuildProbeSummary(MediaProbeResult result)
    {
        StringBuilder sb = new();
        sb.AppendLine($"Format: {result.FormatName} ({result.FormatLongName})");
        sb.AppendLine($"Duration: {result.Duration}");

        if (result.SizeBytes.HasValue)
        {
            sb.AppendLine($"Size: {FormatSize(result.SizeBytes.Value)}");
        }

        int videoCount = result.Streams.Count(s => s.AssetType == MediaAssetType.Video);
        int audioCount = result.Streams.Count(s => s.AssetType == MediaAssetType.Audio);
        int subCount = result.Streams.Count(s => s.AssetType == MediaAssetType.Subtitle);

        sb.Append($"Streams: {result.Streams.Count} total");
        if (videoCount > 0) sb.Append($" · {videoCount} video");
        if (audioCount > 0) sb.Append($" · {audioCount} audio");
        if (subCount > 0) sb.Append($" · {subCount} subtitle");
        sb.AppendLine();

        sb.Append($"Chapters: {(result.Chapters.Count > 0 ? result.Chapters.Count.ToString() : "none")}");

        if (result.NativeFormat is not null && result.NativeFormat.Format != ContainerFormat.Unknown)
        {
            sb.AppendLine();
            sb.Append($"Detected: {result.NativeFormat.Description}");
        }

        return sb.ToString();
    }

    private static string FormatSize(long bytes)
    {
        return bytes switch
        {
            >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:0.##} GB",
            >= 1_048_576 => $"{bytes / 1_048_576.0:0.##} MB",
            >= 1024 => $"{bytes / 1024.0:0.##} KB",
            _ => $"{bytes} bytes"
        };
    }
}
