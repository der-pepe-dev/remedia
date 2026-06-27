namespace ReMedia.App.Avalonia.ViewModels;

using ReMedia.Core.Models;

/// <summary>Read-only display row for one media stream in the tracks DataGrid.</summary>
public sealed class TrackRowViewModel
{
    public TrackRowViewModel(MediaStreamInfo stream)
    {
        StreamIndex = stream.Index;
        Kind = stream.AssetType.ToString();
        Codec = stream.CodecName ?? "?";
        Language = stream.Language ?? "und";
        Title = stream.Title ?? string.Empty;
        Channels = stream.Channels?.ToString() ?? string.Empty;
        SampleRate = stream.SampleRate is int sr ? $"{sr} Hz" : string.Empty;
    }

    public int StreamIndex { get; }

    public string Kind { get; }

    public string Codec { get; }

    public string Language { get; }

    public string Title { get; }

    public string Channels { get; }

    public string SampleRate { get; }
}
