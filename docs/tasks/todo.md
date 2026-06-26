# Backlog

Durable, prioritized task list. Active work goes in `tasks/<task-name>.md`, not here.

## High priority

<!-- TODO -->

## Medium priority

- Loudness matching in the WPF App: CLI `export --target-lufs` auto-applies the
  recommended gain per audio track; the desktop App does not yet expose this (it would
  call `ILoudnessService.MatchToTarget` and set `AppliedGainDb` like the CLI does).
- Loudness matching for multi-part sources: `export --target-lufs` currently warns and
  skips when `--part` is used (per-part measurement / concat analysis not wired).
- WPF App test coverage: `ReMedia.App` ViewModels have no automated tests (e.g. the
  `MainWindowViewModel` clipping recalc with a half-populated `LoudnessAnalysisResult`).
- CI: `.github/workflows/ci.yml` builds only on `windows-latest`; the solution now builds
  on Linux (`EnableWindowsTargeting`), so a fast Linux build leg could speed PR feedback.

## Low priority / someday

- Native probing: extend `ContainerFormatDetector` toward native stream/chapter parsing so
  ffprobe can be replaced without changing workflows.
- Interactive chapter editing (retiming exists; editing UI does not).
