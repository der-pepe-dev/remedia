namespace ReMedia.Tooling.Ffmpeg;

using System.Globalization;
using ReMedia.Core.Diagnostics;
using ReMedia.Core.Interfaces;
using ReMedia.Core.Models;
using ReMedia.Tooling.Configuration;

public sealed class FfmpegAudioSyncService : IAudioSyncService
{
    private const int SampleRate = 8000;
    private const int WindowSamples = 800; // 100ms at 8kHz
    private const double SearchRangeSec = 60.0;
    private const double SearchStepSec = 0.05;
    private const double MatchToleranceSec = 0.2;
    private const double PeakThresholdSigmas = 1.5;
    private const int MinPeakSpacingWindows = 5; // 500ms minimum between peaks

    private readonly ExternalToolPaths _toolPaths;
    private readonly IProcessRunner _processRunner;

    public FfmpegAudioSyncService(ExternalToolPaths toolPaths, IProcessRunner processRunner)
    {
        _toolPaths = toolPaths;
        _processRunner = processRunner;
    }

    public async Task<AudioSyncAnalysisResult> AnalyzeSyncAsync(
        string sourcePath,
        int sourceStreamIndex,
        string destinationPath,
        int destinationStreamIndex,
        decimal stretchFactor = 1m,
        CancellationToken cancellationToken = default)
    {
        float[] sourceSamples = await ExtractMonoPcmAsync(sourcePath, sourceStreamIndex, cancellationToken);
        float[] destSamples = await ExtractMonoPcmAsync(destinationPath, destinationStreamIndex, cancellationToken);

        IReadOnlyList<AudioPeak> rawSourcePeaks = DetectPeaks(sourceSamples);
        IReadOnlyList<AudioPeak> destPeaks = DetectPeaks(destSamples);

        // Scale source peak timestamps to simulate post-retime position
        double sf = (double)stretchFactor;
        IReadOnlyList<AudioPeak> sourcePeaks = rawSourcePeaks
            .Select(p => p with { Timestamp = TimeSpan.FromSeconds(p.Timestamp.TotalSeconds * sf) })
            .ToList();

        (TimeSpan offset, float confidence) = FindBestOffset(sourcePeaks, destPeaks);

        return new AudioSyncAnalysisResult(offset, confidence, sourcePeaks, destPeaks);
    }

    private async Task<float[]> ExtractMonoPcmAsync(string path, int streamIndex, CancellationToken cancellationToken)
    {
        string tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".pcm");
        try
        {
            string args = string.Format(
                CultureInfo.InvariantCulture,
                "-y -i \"{0}\" -map 0:{1} -af \"aresample={2},aformat=sample_fmts=s16\" -f s16le -ac 1 \"{3}\"",
                path, streamIndex, SampleRate, tempFile);

            await _processRunner.RunAsync(_toolPaths.FfmpegPath, args, cancellationToken);

            byte[] bytes = await File.ReadAllBytesAsync(tempFile, cancellationToken);
            float[] samples = new float[bytes.Length / 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short s = BitConverter.ToInt16(bytes, i * 2);
                samples[i] = s / 32768.0f;
            }

            return samples;
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    private static IReadOnlyList<AudioPeak> DetectPeaks(float[] samples)
    {
        if (samples.Length < WindowSamples * 2)
        {
            return [];
        }

        int numWindows = samples.Length / WindowSamples;
        float[] envelope = new float[numWindows];

        for (int w = 0; w < numWindows; w++)
        {
            float sumSq = 0f;
            int offset = w * WindowSamples;
            for (int i = 0; i < WindowSamples; i++)
            {
                float s = samples[offset + i];
                sumSq += s * s;
            }
            envelope[w] = MathF.Sqrt(sumSq / WindowSamples);
        }

        float mean = 0f;
        for (int w = 0; w < numWindows; w++) mean += envelope[w];
        mean /= numWindows;

        float variance = 0f;
        for (int w = 0; w < numWindows; w++)
        {
            float diff = envelope[w] - mean;
            variance += diff * diff;
        }
        variance /= numWindows;
        float stdDev = MathF.Sqrt(variance);
        float threshold = mean + (float)PeakThresholdSigmas * stdDev;

        List<AudioPeak> peaks = [];
        int lastPeakWindow = -MinPeakSpacingWindows;

        for (int w = 1; w < numWindows - 1; w++)
        {
            if (envelope[w] < threshold) continue;
            if (envelope[w] < envelope[w - 1] || envelope[w] < envelope[w + 1]) continue;
            if (w - lastPeakWindow < MinPeakSpacingWindows) continue;

            TimeSpan timestamp = TimeSpan.FromSeconds((double)(w * WindowSamples) / SampleRate);
            peaks.Add(new AudioPeak(timestamp, envelope[w]));
            lastPeakWindow = w;
        }

        return peaks;
    }

    private static (TimeSpan offset, float confidence) FindBestOffset(
        IReadOnlyList<AudioPeak> source,
        IReadOnlyList<AudioPeak> dest)
    {
        if (source.Count == 0 || dest.Count == 0)
        {
            return (TimeSpan.Zero, 0f);
        }

        // Pre-sort dest peaks for faster search
        double[] destTimes = dest.Select(p => p.Timestamp.TotalSeconds).OrderBy(t => t).ToArray();

        int bestScore = 0;
        double bestOffsetSec = 0.0;
        int steps = (int)(SearchRangeSec * 2 / SearchStepSec);

        for (int step = -steps / 2; step <= steps / 2; step++)
        {
            double offsetSec = step * SearchStepSec;
            int score = 0;

            foreach (AudioPeak sp in source)
            {
                double adjusted = sp.Timestamp.TotalSeconds + offsetSec;

                // Binary search for nearest dest peak
                int lo = 0, hi = destTimes.Length - 1;
                while (lo < hi)
                {
                    int mid = (lo + hi) / 2;
                    if (destTimes[mid] < adjusted) lo = mid + 1;
                    else hi = mid;
                }

                if (Math.Abs(destTimes[lo] - adjusted) <= MatchToleranceSec)
                {
                    score++;
                }
                else if (lo > 0 && Math.Abs(destTimes[lo - 1] - adjusted) <= MatchToleranceSec)
                {
                    score++;
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestOffsetSec = offsetSec;
            }
        }

        float confidence = (float)bestScore / Math.Max(source.Count, dest.Count);
        return (TimeSpan.FromSeconds(bestOffsetSec), confidence);
    }
}
