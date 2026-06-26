namespace ReMedia.App.ViewModels;

using ReMedia.Core.Models;
using ReMedia.Core.Services;

public sealed class TrackRowViewModel : ViewModelBase
{
    private bool _selected;
    private string _outputCodec = "copy";
    private string _outputContainer = ".mka";
    private string _loudnessDisplay = string.Empty;
    private string _clippingDisplay = string.Empty;
    private decimal? _measuredLufs;
    private decimal? _gainDb;
    private LoudnessAnalysisResult? _lastLoudness;
    private IReadOnlyList<string>? _availableCodecs;

    public int StreamIndex { get; init; }
    public MediaAssetType AssetType { get; init; }
    public string? CodecName { get; init; }
    public string? Language { get; init; }
    public string? Title { get; init; }
    public bool IsDefault { get; init; }
    public bool IsForced { get; init; }
    public int? Channels { get; init; }
    public int? SampleRate { get; init; }

    public IReadOnlyList<string> AvailableCodecs => _availableCodecs ??= BuildCodecList();

    public string DetailDisplay
    {
        get
        {
            return AssetType switch
            {
                MediaAssetType.Audio => FormatAudioDetail(),
                MediaAssetType.Video => FormatVideoDetail(),
                _ => string.Empty
            };
        }
    }

    public bool Selected
    {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }

    public string OutputCodec
    {
        get => _outputCodec;
        set
        {
            if (SetProperty(ref _outputCodec, value))
            {
                string resolvedCodec = string.Equals(value, "copy", StringComparison.OrdinalIgnoreCase)
                    ? CodecName ?? value
                    : value;

                OutputContainer = ContainerDefaults.GetDefaultContainer(AssetType, resolvedCodec);
            }
        }
    }

    public string OutputContainer
    {
        get => _outputContainer;
        set => SetProperty(ref _outputContainer, value);
    }

    public string LoudnessDisplay
    {
        get => _loudnessDisplay;
        set => SetProperty(ref _loudnessDisplay, value);
    }

    public string ClippingDisplay
    {
        get => _clippingDisplay;
        set => SetProperty(ref _clippingDisplay, value);
    }

    public decimal? MeasuredLufs
    {
        get => _measuredLufs;
        set => SetProperty(ref _measuredLufs, value);
    }

    public decimal? GainDb
    {
        get => _gainDb;
        set => SetProperty(ref _gainDb, value);
    }

    public LoudnessAnalysisResult? LastLoudness
    {
        get => _lastLoudness;
        set => SetProperty(ref _lastLoudness, value);
    }

    public string FlagsDisplay
    {
        get
        {
            List<string> flags = [];
            if (IsDefault) flags.Add("default");
            if (IsForced) flags.Add("forced");
            return flags.Count > 0 ? string.Join(", ", flags) : string.Empty;
        }
    }

    private IReadOnlyList<string> BuildCodecList()
    {
        IReadOnlyList<CodecOption> catalog = AssetType switch
        {
            MediaAssetType.Audio => CodecCatalog.Audio,
            MediaAssetType.Subtitle => CodecCatalog.Subtitle,
            _ => []
        };

        List<string> codecs = ["copy"];
        foreach (CodecOption option in catalog)
        {
            codecs.Add(option.Id);
        }

        return codecs;
    }

    private string FormatAudioDetail()
    {
        List<string> parts = [];
        if (Channels.HasValue) parts.Add($"{Channels}ch");
        if (SampleRate.HasValue) parts.Add($"{SampleRate}Hz");
        return string.Join(" ", parts);
    }

    private string FormatVideoDetail()
    {
        return string.Empty;
    }
}
