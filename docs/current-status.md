# ReMedia — current status

_Update only when durable project status changes: major feature completed, known
limitation discovered, milestone changed, or durable architectural direction changed.
Prefer appending dated notes over rewriting._

## Status

Early phased development. Phase 0 (probe + same-format track/chapter export) is the
active scope; Phase 1 (FPS-aware conversion, audio codec/container conversion,
retimed export) is next. Backend currently shells out to ffprobe/ffmpeg; a native
probe backend is a later option. See [[phases]].

## Known limitations

- No FPS-aware timing conversion yet (Phase 1).
- Loudness matching, clipping prediction, subtitle/chapter retiming, and muxing are
  later phases.
- Native probe backend not implemented (ffprobe-based today).

## Recent notes

<!-- Append dated notes here, newest first: -->
<!-- - YYYY-MM-DD: ... -->
