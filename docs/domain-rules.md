# Domain rules

The timing-model rules at the heart of ReMedia Sync. Read when touching FPS handling,
timing analysis, export planning, or anything in `ReMedia.Core`.

## PAL/NTSC handling

Do not model PAL/NTSC as vague string labels only. Always use exact FPS values
internally — e.g. `25.000`, `24000/1001`, `24.000`, `30000/1001`. The UI may present
friendly labels (PAL 25, NTSC Film 23.976, NTSC Video 29.97), but the math always uses
exact values.

## Timing math

Core formulas — keep centralized in `ReMedia.Core` and covered by tests:

- `destinationDuration = originalDuration * sourceFps / targetFps`
- `stretchFactor = sourceFps / targetFps`
- `audioTempoFactor = targetFps / sourceFps`

## Reference stream behavior

- If multiple video streams exist, allow selecting a reference stream.
- If no video stream exists, allow manual source FPS input.
- Always allow manual override of detected FPS.
- Treat chapters as first-class timing assets.
- Keep text and bitmap subtitles distinct.

## Mission boundary

ReMedia Sync is a timing-aware media workflow tool, not a generic ffmpeg GUI. Its job
is to probe cleanly, export selected tracks, compare source/destination timing models,
convert and retime audio first (then subtitles and chapters), and help merge converted
assets into alternate masters.

Avoid: turning it into a generic ffmpeg front-end, hardcoding PAL/NTSC assumptions
without exact math, or hiding clipping/timing-mismatch warnings.
