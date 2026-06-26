# Coding and testing

## Coding conventions

- Target `.NET 10`.
- File-scoped namespaces.
- Prefer small focused services over large god classes.
- Prefer immutable records for DTO-like models when it improves clarity.
- Use explicit names (`TimingAnalysisResult`, `TrackExportPlan`,
  `AudioConversionOptions`, `ClippingPredictionResult`).
- Validate early and return structured failures.
- Avoid static mutable state.
- Keep UI logic out of process/tool runners; keep domain logic out of WPF code-behind.

## Testing expectations

Add tests for: timing math, FPS edge cases, multiple video streams, missing FPS,
no chapters, stream-metadata mapping, codec/container selection, and generated
ffmpeg/ffprobe command arguments.

When command builders change, tests must verify the exact generated arguments.

## CLI and logging

The CLI is part of the product architecture. Commands should be deterministic;
generated external-tool commands should be visible in logs; raw stdout/stderr should
be capturable; parsed/normalized results stay separate from raw tool output; tool
versions should be easy to log for diagnostics.
