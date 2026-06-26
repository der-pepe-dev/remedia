namespace ReMedia.Tooling.Ffmpeg;

using System.Globalization;
using ReMedia.Core.Models;

public static class FfmpegArgumentBuilder
{
    public static string BuildTrackExportArguments(string inputPath, TrackExportOptions options)
    {
        return BuildTrackExportArguments(inputPath, options, concatDemuxer: false);
    }

    public static string BuildTrackExportArguments(string inputPath, TrackExportOptions options, bool concatDemuxer)
    {
        if (options.CopyStream && (options.ApplyTimingConversion || options.AppliedGainDb.HasValue || (options.AudioSyncOffsetMs.HasValue && options.AudioSyncOffsetMs.Value != 0m)))
        {
            throw new ArgumentException(
                "Cannot use stream copy with timing conversion, gain adjustment, or sync offset. These require re-encoding.",
                nameof(options));
        }

        List<string> args = ["-y"];

        if (concatDemuxer)
        {
            args.Add("-f");
            args.Add("concat");
            args.Add("-safe");
            args.Add("0");
        }

        args.Add("-i");
        args.Add(Quote(inputPath));
        args.Add("-map");
        args.Add($"0:{options.StreamIndex}");

        List<string> audioFilters = [];

        if (options.ApplyTimingConversion && options.SourceFps.HasValue && options.TargetFps.HasValue && options.AssetType == MediaAssetType.Audio)
        {
            decimal audioTempoFactor = options.TargetFps.Value / options.SourceFps.Value;
            audioFilters.Add($"atempo={audioTempoFactor.ToString("0.################", CultureInfo.InvariantCulture)}");
        }

        if (options.AppliedGainDb.HasValue && options.AssetType == MediaAssetType.Audio)
        {
            audioFilters.Add($"volume={options.AppliedGainDb.Value.ToString("0.################", CultureInfo.InvariantCulture)}dB");
        }

        if (options.AudioSyncOffsetMs.HasValue && options.AudioSyncOffsetMs.Value != 0m && options.AssetType == MediaAssetType.Audio)
        {
            double offsetMs = (double)options.AudioSyncOffsetMs.Value;
            if (offsetMs > 0)
            {
                audioFilters.Add($"adelay={offsetMs.ToString("0.###", CultureInfo.InvariantCulture)}:all=1");
            }
            else
            {
                double trimSec = -offsetMs / 1000.0;
                audioFilters.Add($"atrim=start={trimSec.ToString("0.######", CultureInfo.InvariantCulture)}");
                audioFilters.Add("asetpts=PTS-STARTPTS");
            }
        }

        if (audioFilters.Count > 0)
        {
            args.Add("-filter:a");
            args.Add(Quote(string.Join(",", audioFilters)));
        }

        if (options.CopyStream)
        {
            args.Add("-c");
            args.Add("copy");
        }
        else
        {
            string streamSpecifier = options.AssetType switch
            {
                MediaAssetType.Audio => "-c:a",
                MediaAssetType.Subtitle => "-c:s",
                _ => "-c"
            };

            args.Add(streamSpecifier);
            args.Add(options.OutputCodec);
        }

        args.Add(Quote(options.OutputPath));

        return string.Join(' ', args);
    }

    public static string BuildChapterExportArguments(string inputPath, string outputPath)
    {
        return $"-y -i {Quote(inputPath)} -f ffmetadata {Quote(outputPath)}";
    }

    public static string BuildLoudnessAnalysisArguments(string inputPath, int streamIndex)
    {
        return $"-i {Quote(inputPath)} -map 0:{streamIndex} -af ebur128=peak=true -f null -";
    }

    /// <summary>
    /// Builds arguments to mux multiple input files into a single output container.
    /// If a destination master is provided, its streams are mapped first.
    /// Exported asset files are added as additional inputs and mapped after.
    /// Chapters are attached via -i on the ffmetadata file and -map_chapters.
    /// </summary>
    public static string BuildMuxArguments(MuxRequest request)
    {
        List<string> args = ["-y"];
        List<string> maps = [];
        int inputIdx = 0;

        if (request.DestinationMasterPath is not null)
        {
            args.Add("-i");
            args.Add(Quote(request.DestinationMasterPath));
            maps.Add($"-map {inputIdx}");
            inputIdx++;
        }

        int chaptersInputIdx = -1;
        if (request.ChaptersFilePath is not null)
        {
            args.Add("-i");
            args.Add(Quote(request.ChaptersFilePath));
            chaptersInputIdx = inputIdx;
            inputIdx++;
        }

        Dictionary<int, MuxInputAsset> assetInputIndexes = [];
        foreach (MuxInputAsset asset in request.Assets)
        {
            args.Add("-i");
            args.Add(Quote(asset.FilePath));
            maps.Add($"-map {inputIdx}:{asset.StreamIndex}");
            assetInputIndexes[inputIdx] = asset;
            inputIdx++;
        }

        args.AddRange(maps);
        args.Add("-c");
        args.Add("copy");

        if (chaptersInputIdx >= 0)
        {
            args.Add("-map_chapters");
            args.Add(chaptersInputIdx.ToString(CultureInfo.InvariantCulture));
        }
        else if (request.DestinationMasterPath is null)
        {
            args.Add("-map_chapters");
            args.Add("-1");
        }

        int metadataStreamIdx = 0;
        if (request.DestinationMasterPath is not null)
        {
            metadataStreamIdx = -1;
        }

        foreach ((int assetInput, MuxInputAsset asset) in assetInputIndexes)
        {
            metadataStreamIdx++;
            int sIdx = request.DestinationMasterPath is not null ? metadataStreamIdx : metadataStreamIdx - 1;

            if (asset.Language is not null)
            {
                args.Add($"-metadata:s:{sIdx}");
                args.Add($"language={asset.Language}");
            }

            if (asset.Title is not null)
            {
                args.Add($"-metadata:s:{sIdx}");
                args.Add($"title={Quote(asset.Title)}");
            }

            if (asset.IsDefault)
            {
                args.Add($"-disposition:s:{sIdx}");
                args.Add("default");
            }
        }

        args.Add(Quote(request.OutputPath));
        return string.Join(' ', args);
    }

    private static string Quote(string value)
    {
        // Escape any embedded double quote so values like a stream title containing a
        // quote can't terminate the argument early. (File paths can't contain '"' on
        // Windows, but metadata titles can.)
        return $"\"{value.Replace("\"", "\\\"")}\"";
    }
}
