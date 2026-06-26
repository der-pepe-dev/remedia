namespace ReMedia.Tooling.Ffprobe;

using System.Text.Json.Serialization;

internal sealed class FfprobeRootDto
{
    [JsonPropertyName("format")]
    public FfprobeFormatDto? Format { get; set; }

    [JsonPropertyName("streams")]
    public List<FfprobeStreamDto>? Streams { get; set; }

    [JsonPropertyName("chapters")]
    public List<FfprobeChapterDto>? Chapters { get; set; }
}

internal sealed class FfprobeFormatDto
{
    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("format_name")]
    public string? FormatName { get; set; }

    [JsonPropertyName("format_long_name")]
    public string? FormatLongName { get; set; }

    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

    [JsonPropertyName("size")]
    public string? Size { get; set; }
}

internal sealed class FfprobeStreamDto
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("codec_type")]
    public string? CodecType { get; set; }

    [JsonPropertyName("codec_name")]
    public string? CodecName { get; set; }

    [JsonPropertyName("codec_long_name")]
    public string? CodecLongName { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("channels")]
    public int? Channels { get; set; }

    [JsonPropertyName("sample_rate")]
    public string? SampleRate { get; set; }

    [JsonPropertyName("avg_frame_rate")]
    public string? AvgFrameRate { get; set; }

    [JsonPropertyName("r_frame_rate")]
    public string? RealFrameRate { get; set; }

    [JsonPropertyName("field_order")]
    public string? FieldOrder { get; set; }

    [JsonPropertyName("disposition")]
    public FfprobeDispositionDto? Disposition { get; set; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; set; }
}

internal sealed class FfprobeDispositionDto
{
    [JsonPropertyName("default")]
    public int Default { get; set; }

    [JsonPropertyName("forced")]
    public int Forced { get; set; }
}

internal sealed class FfprobeChapterDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; set; }
}
