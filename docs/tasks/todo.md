# Backlog

Durable, prioritized task list. Active work goes in `tasks/<task-name>.md`, not here.

## High priority

- Loudness *matching*: wire an auto gain-to-target workflow on top of existing EBU R128
  measurement + clipping prediction (currently measure-only). See [[phases]] "Later".

## Medium priority

- WPF App test coverage: `ReMedia.App` ViewModels have no automated tests (e.g. the
  `MainWindowViewModel` clipping recalc with a half-populated `LoudnessAnalysisResult`).
- CI: `.github/workflows/ci.yml` builds only on `windows-latest`; the solution now builds
  on Linux (`EnableWindowsTargeting`), so a fast Linux build leg could speed PR feedback.

## Low priority / someday

- Native probing: extend `ContainerFormatDetector` toward native stream/chapter parsing so
  ffprobe can be replaced without changing workflows.
- Interactive chapter editing (retiming exists; editing UI does not).
