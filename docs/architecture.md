# Architecture

Clean separation between domain logic and tool integration.

## Layers / project roles

### ReMedia.Core
Domain models, service abstractions, timing analysis, export planning,
codec/container catalogs, validation, and future loudness/clipping abstractions.
Depends on abstractions, not process-execution details.

### ReMedia.Tooling
Concrete wrappers for external tools (ffprobe, ffmpeg):
- command construction (centralized, testable)
- process execution + raw stdout/stderr capture
- JSON DTO parsing
- adapters mapping raw tool output to Core models

### ReMedia.Cli
Thin command host for probing, timing analysis, exporting, and future batch
workflows. Part of the product architecture — not throwaway code. Keep it useful
for testing and automation.

### ReMedia.App
A WPF desktop shell (`net10.0-windows`, `UseWPF`). Stay as thin as possible and reuse
Core abstractions. No media-processing logic in code-behind.

### tests/*
Unit tests for timing math, planning, codec selection, and command generation.

## Hard architectural rules

- Do **not** leak raw ffprobe DTOs outside `ReMedia.Tooling`.
- Do **not** parse ffprobe JSON in UI code.
- Do **not** scatter ffmpeg command strings across the solution.
- Keep command construction centralized and testable.
- Core depends on abstractions, not process-execution details.
- Design so a future native probe backend can replace ffprobe without changing app
  workflows.

## Design principles

- Keep command generation deterministic.
- Keep process output accessible for diagnostics.
- Keep UI free of media-processing logic.
- Prepare now for future native probing.

## Implementation order

When implementing a feature: define/update the Core domain model → define the service
abstraction in Core → implement the ffmpeg/ffprobe-backed version in Tooling → expose
via CLI → wire the desktop UI. This keeps the system testable and prevents UI-first
shortcuts from becoming architecture.
