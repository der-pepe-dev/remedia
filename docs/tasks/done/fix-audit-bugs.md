# Task: Fix bugs & issues from code audit

Source plan: audit of Core/Tooling/Cli/App. User chose "everything flagged".

## Checklist

- [x] A1 VttParser fractional seconds (`.5` → 500ms)
- [x] A2 SrtParser fractional seconds (shared `SubtitleTimeParsing.TryParseFractionalMs`)
- [x] A3 ContainerFormatDetector EBML scan off-by-one
- [x] A4 ExportWorkflowService mux asset-type alignment
- [~] A5 ConcatFileWriter — investigated, NO CHANGE: backslash is literal inside the
      concat demuxer's single quotes; only `'` is special (already handled). Agent claim
      was wrong; existing tests encode correct behavior.
- [x] B6 CLI error on present-but-invalid flag value (no silent default)
- [x] B7 Subtitle parsers: surface dropped-cue warning (new `Parse(content, out warnings)`)
- [x] C8 FfmetadataWriter round instead of truncate
- [x] C9 FfmpegArgumentBuilder.Quote escape inner `"`
- [x] C10 Rational strictness (zero denom / malformed → Zero, test updated)
- [x] C11 Ebur128OutputParser tighten regex

## Verification (2026-06-26)
- App (WPF) can't build on WSL (NETSDK1100, Windows-targeted) — built non-App projects.
- `ReMedia.Core.Tests`: Total 164, Failed 0.
- `ReMedia.Tooling.Tests`: Total 47, Failed 0.
  (xunit.v3 / Microsoft.Testing.Platform — run the test `.dll` directly, not `dotnet test`.)
- `ReMedia.Cli` build succeeded; ran binary to confirm B6 error paths:
  - `analyze movie.mkv --target-fps abc` → "Invalid or missing value for --target-fps." exit 1
  - `analyze movie.mkv --target-fps` (missing value) → same, exit 1
  - `export movie.mkv --stream xyz` → "Invalid or missing value for --stream..." exit 1

## Files changed
- src/ReMedia.Core/Services/SubtitleTimeParsing.cs (new)
- src/ReMedia.Core/Services/{VttParser,SrtParser,ContainerFormatDetector,
  FfmetadataWriter,ExportWorkflowService}.cs
- src/ReMedia.Core/Models/Rational.cs
- src/ReMedia.Tooling/Ffmpeg/{FfmpegArgumentBuilder,Ebur128OutputParser}.cs
- src/ReMedia.Cli/Support/CliArguments.cs, src/ReMedia.Cli/Program.cs
- tests: VttParserTests, SrtParserTests, ContainerFormatDetectorTests, RationalTests,
  FfmetadataWriterTests, ExportWorkflowServiceTests, Ebur128OutputParserTests,
  FfmpegMuxArgumentTests

## Out of scope (confirmed non-issues)
AsyncRelayCommand async void (standard ICommand), MainWindow DataContext ordering,
ProcessRunner (no deadlock), ffprobe DTO leakage (internal).
