# ReMedia — current status

_Update only when durable project status changes: major feature completed, known
limitation discovered, milestone changed, or durable architectural direction changed.
Prefer appending dated notes over rewriting._

## Status

Phase 0 and Phase 1 are substantially implemented, and several "Later" features already
exist in code. The backend shells out to ffprobe/ffmpeg (`ReMedia.Tooling`). See [[phases]].

Implemented today (verified in source / CLI commands):

- Probe via ffprobe + a native header-magic `ContainerFormatDetector` (CLI: `probe`, `detect`).
- FPS-aware timing analysis — source/target FPS, stretch/tempo factors
  (`TimingAnalysisService`, CLI: `analyze`).
- Track export (per-stream), chapter export to ffmetadata, and muxing to MKV
  (`FfmpegTrackExportService`, `FfmpegChapterExportService`, `FfmpegMuxService`,
  orchestrated by `ExportWorkflowService`; CLI: `export`).
- Audio codec/container conversion, gain, and sync-offset; multi-part concat source.
- Loudness measurement (EBU R128) + clipping prediction
  (`FfmpegLoudnessService`, `Ebur128OutputParser`; CLI: `loudness`).
- Subtitle parse/write (SRT + VTT), retiming, cleanup, and segment-based retiming
  (`SrtParser`/`VttParser`/writers, `SubtitleRetimingService`, `SubtitleCleanupService`,
  `SegmentedRetimingService`; CLI: `cleanup`). Chapter retiming via `ChapterRetimingService`.
- Codec/container reference data (`CodecCatalog`, `ContainerDefaults`, `FpsPresets`;
  CLI: `list-codecs`).

## Known limitations / not done

- WPF App (`ReMedia.App`) is a thin shell; no automated tests over its ViewModels.
- Native probe backend covers container-format detection only; full native stream/chapter
  parsing still delegates to ffprobe.
- Loudness *matching* (auto gain to a target) beyond measurement + clipping prediction
  is not wired as an automated workflow.

## Recent notes

<!-- Append dated notes here, newest first: -->
- 2026-06-27: Started the WPF -> Avalonia migration ([[avalonia-migration]],
  [[decisions/0001-frontend-wpf-to-avalonia]]). New cross-platform `ReMedia.App.Avalonia`
  (net10.0) with a runnable shell: input picker -> Probe -> tracks DataGrid + log. Builds
  and launches on Linux/WSLg. WPF App kept until parity. Export/loudness/timing come next.
- 2026-06-27: Loudness matching now auto-applies on export — `export --target-lufs <v>
  [--ceiling <dBTP>]` measures each audio track, computes the ceiling-limited gain, and
  re-encodes (copy falls back to flac) to hit the target. Remaining: WPF App surface and
  multi-part sources (skipped with a warning today).
- 2026-06-27: Added loudness matching — `ILoudnessService.MatchToTarget` recommends a
  gain to reach a target integrated LUFS, limited by a true-peak ceiling (default -1 dBTP)
  and clipping-checked. Exposed via CLI `loudness --target-lufs <v> [--ceiling <dBTP>]`.
- 2026-06-27: Synced status/phases docs with actual code — Phase 0/1 and several "Later"
  features (loudness+clipping, mux, subtitle/chapter/segment retiming, subtitle cleanup,
  native format detection) are implemented. Docs previously listed these as not started.
- 2026-06-27: Full `dotnet build ReMedia.sln` now works on WSL/Linux
  (`EnableWindowsTargeting` on the WPF App); whole solution builds with 0 warnings.
<!-- - YYYY-MM-DD: ... -->
