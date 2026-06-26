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
- CI: verify the Windows leg actually runs tests. `dotnet test` does not discover the
  xunit.v3 / Microsoft.Testing.Platform projects locally (reports "No test is available");
  the Windows job still uses `dotnet test ReMedia.sln`, so it may be a false pass. The new
  Linux leg runs the built test executables directly instead. Consider switching Windows
  to the same approach (or enabling MTP `dotnet test` support).

## Low priority / someday

- Native probing: extend `ContainerFormatDetector` toward native stream/chapter parsing so
  ffprobe can be replaced without changing workflows.
- Interactive chapter editing (retiming exists; editing UI does not).
