namespace ReMedia.App.Avalonia.ViewModels;

using ReMedia.Core.Models;

/// <summary>Display row for one media stream in the tracks DataGrid, with an export toggle.</summary>
public sealed class TrackRowViewModel : ViewModelBase
{
    private bool _selected;

    public TrackRowViewModel(MediaStreamInfo stream)
    {
        Stream = stream;
        // Default to exporting audio/subtitle tracks; video is not exported in this app.
        _selected = stream.AssetType is not MediaAssetType.Video;
    }

    public MediaStreamInfo Stream { get; }

    public int StreamIndex => Stream.Index;

    public string Kind => Stream.AssetType.ToString();

    public string Codec => Stream.CodecName ?? "?";

    public string Language => Stream.Language ?? "und";

    public string Title => Stream.Title ?? string.Empty;

    public string Channels => Stream.Channels?.ToString() ?? string.Empty;

    public string SampleRate => Stream.SampleRate is int sr ? $"{sr} Hz" : string.Empty;

    public bool IsExportable => Stream.AssetType is not MediaAssetType.Video;

    public bool Selected
    {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }
}
