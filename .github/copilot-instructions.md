# ReMedia Sync repository instructions for GitHub Copilot

## Product identity
- Internal codename: **ReMedia**
- Public product name: **ReMedia Sync**
- Public positioning: extract, retime, and sync audio, subtitles, and chapters across PAL/NTSC and other media timings.

## Mission
ReMedia Sync is a purpose-built timing-aware media helper, not a generic ffmpeg wrapper. Its job is to:
- probe media files cleanly
- export tracks as individual files
- calculate timing differences between source and target frame-rate families
- convert audio and later subtitles/chapters to match alternate masters
- help users merge retimed assets into destination releases

## Current implementation strategy
For now, the backend uses:
- `ffprobe`
- `ffmpeg`
- other CLI tools when clearly useful

A future phase may replace ffprobe with a native probe/parser layer.

## Architectural rules
- Keep all external tool integration behind abstractions in `ReMedia.Core`.
- Concrete ffmpeg/ffprobe implementations live in `ReMedia.Tooling`.
- The rest of the solution must not depend on raw ffprobe JSON or tool-specific DTOs.
- Normalize all tool output into domain models before exposing it elsewhere.
- CLI and desktop app should share the same services and domain logic.
- Prefer deterministic command generation and verbose logs.

## Scope by phase

### Phase 0
Probe one file and support same-format export:
- show container and duration
- show all streams and chapters
- allow selecting tracks to export
- export individual tracks without timing changes
- keep logs and generated commands visible

### Phase 1
Add timing and conversion:
- detect reference/source FPS
- allow destination FPS selection
- show original duration and calculated destination duration
- show stretch factor and audio tempo factor
- allow audio codec/container selection
- support audio conversion
- prepare subtitle/chapter retiming workflows

### Planned later features
- loudness match to destination audio
- clipping prediction/warnings
- subtitle retiming and cleanup
- chapter retiming and editing
- one-click mux into destination master
- optional segment-based timing
- optional native probing layer

## Domain guidance
- Model PAL/NTSC as exact FPS values, not as vague labels only.
- Core timing formula:
  - `destinationDuration = originalDuration * sourceFps / targetFps`
  - `stretchFactor = sourceFps / targetFps`
  - `audioTempoFactor = targetFps / sourceFps`
- Always allow manual override of detected FPS.
- Multiple video streams must allow choosing a reference stream.
- Files with no video stream must still work with manual FPS input.

## Audio guidance
- Audio conversion must support explicit codec/container selection.
- Build for later loudness workflows:
  - measure source loudness
  - measure destination loudness
  - apply gain or normalization
- Keep space in the model for clipping prediction.
- Show warnings when loudness or gain changes would likely cause excessive clipping.

## Subtitle and chapter guidance
- Keep text subtitles and bitmap subtitles distinct in the model.
- Do not assume all subtitles can be copied, converted, or retimed the same way.
- Chapters should be treated as first-class timing assets, not an afterthought.
- Preserve stream metadata like language, title, default, and forced flags where possible.

## Coding conventions
- Use file-scoped namespaces.
- Prefer small focused services over large god classes.
- Avoid static state unless it is clearly immutable catalog/configuration data.
- Use immutable records for DTO-like domain models where it improves clarity.
- Use explicit names. Example: `TimingAnalysisResult`, `TrackExportOptions`, `ClippingPredictionResult`.
- Validate user input and tool results early and return structured errors.
- Avoid leaking process execution details into UI code.

## Testing expectations
- Add unit tests for timing math, codec/container selection, and command generation.
- Add tests for weird but realistic edge cases:
  - missing fps
  - multiple video streams
  - no chapters
  - forced/default subtitle flags
  - destination duration mismatch
- When changing command builders, add tests that verify generated arguments.

## UI expectations
- Desktop app should be thin and bind to view models.
- No media-processing logic in code-behind.
- Always show:
  - source duration
  - source FPS
  - target FPS
  - calculated destination duration
  - generated commands or log output
- Prefer workflows that match the real use case:
  - source assets file
  - target master file
  - selected tracks
  - timing analysis
  - export/convert/mux

## What to avoid
- Do not scatter ffmpeg command strings across unrelated classes.
- Do not parse ffprobe JSON directly in the UI.
- Do not hardcode PAL/NTSC assumptions without exact math.
- Do not force video transcoding into early phases.
- Do not hide warnings about clipping or suspicious timing mismatches.

## Build and repo expectations
- Target `.NET 10`.
- Keep the solution organized under `src/` and `tests/`.
- Use repository-wide instructions in this file and keep them updated as the roadmap evolves.
