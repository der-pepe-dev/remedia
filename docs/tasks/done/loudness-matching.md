# Task: Loudness matching (auto gain-to-target)

Build on existing EBU R128 measurement + clipping prediction to recommend a gain that
brings a stream to a target integrated LUFS, limited by a true-peak ceiling.

## Checklist
- [x] `LoudnessMatchResult` model (Core/Models)
- [x] `ILoudnessService.MatchToTarget(current, targetLufs, ceilingDbtp)` + impl in
      `FfmpegLoudnessService` (pure; reuses `PredictClipping`)
- [x] CLI `loudness --target-lufs <v> [--ceiling <dBTP>]` wires it; help text
- [x] `CliPrinter.PrintLoudnessMatch`
- [x] Tests (Tooling.Tests, same instantiation pattern as ClippingPredictionTests)

## Design notes
- rawGain = target - measuredLufs (null measured → no recommendation, clear message).
- ceilingGain = ceiling - truePeak (when truePeak known); recommended = min(raw, ceilingGain).
- limited = recommended < raw; achieved = measured + recommended; shortfall when limited.
- ceiling default -1.0 dBTP. Gains rounded to 0.1 dB. Clipping predicted at recommended.
- Scope: recommendation workflow + CLI surface. Auto-applying into ExportWorkflow per
  track is a follow-up (export already supports AppliedGainDb).

## Verification
- `dotnet build` (Core, Tooling, Cli) 0 warnings; Tooling tests green.

## Result (2026-06-27)
Done. Core 164 + Tooling 53 tests pass (6 new), 0 warnings.
CLI `loudness --target-lufs`/`--ceiling` verified. Export auto-apply deferred (see todo.md).
