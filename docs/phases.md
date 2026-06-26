# Phase plan

Status markers: `[done]` implemented in code, `[partial]` partly implemented,
`[todo]` not started. Last reconciled with source 2026-06-27. See [[current-status]].

## Phase 0
- [done] Probe one input file
- [done] Display format, duration, streams, chapters
- [done] Export selected non-video tracks individually
- [done] Export chapter metadata
- [done] Keep same format/timing where possible

## Phase 1
- [done] Detect source/reference FPS
- [done] Choose target FPS
- [done] Show destination duration, stretch factor, tempo factor
- [done] Audio codec/container conversion
- [done] Audio retime/export
- [done] Begin subtitle/chapter retime groundwork

## Later
- [partial] Destination loudness matching — measurement (EBU R128) done; auto gain-to-target
  workflow not wired
- [done] Clipping warnings and safety thresholds
- [done] Subtitle cleanup and normalization
- [todo] Chapter editing (retiming done; interactive editing not started)
- [done] Muxing into target releases
- [done] Segment-based timing
- [partial] Native probing — container-format detection done; native stream/chapter parsing
  still via ffprobe
