# Backlog

Durable, prioritized task list. Active work goes in `tasks/<task-name>.md`, not here.

## High priority

- Migrate the frontend from WPF to Avalonia (cross-platform runtime). See
  [[0001-frontend-wpf-to-avalonia]]. Keep all logic in Core/Tooling; the App is a thin
  shell. Until done, only keep the WPF App compiling — no new WPF-only investment.

## Medium priority

- Loudness matching for multi-part sources: `export --target-lufs` currently warns and
  skips when `--part` is used (per-part measurement / concat analysis not wired).
  Frontend-agnostic (lives in CLI/Core).
- CI: verify the Windows leg actually runs tests. `dotnet test` does not discover the
  xunit.v3 / Microsoft.Testing.Platform projects locally (reports "No test is available");
  the Windows job still uses `dotnet test ReMedia.sln`, so it may be a false pass. The new
  Linux leg runs the built test executables directly instead. Consider switching Windows
  to the same approach (or enabling MTP `dotnet test` support).

## Low priority / someday

- Native probing: extend `ContainerFormatDetector` toward native stream/chapter parsing so
  ffprobe can be replaced without changing workflows.
- Interactive chapter editing (retiming exists; editing UI does not).

## Deferred — superseded by the Avalonia migration ([[0001-frontend-wpf-to-avalonia]])

- Loudness matching surfaced in the desktop GUI: do it in the new Avalonia frontend, not
  WPF. Core already exposes `ILoudnessService.MatchToTarget` + `AppliedGainDb`.
- WPF App ViewModel tests: not worth adding to a frontend being replaced. (The flagged
  `MainWindowViewModel` half-populated `LoudnessAnalysisResult` nullable edge should be
  re-checked in the Avalonia port.)
