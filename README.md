# ReMedia Sync

**Extract, retime, and sync audio, subtitles, and chapters across PAL/NTSC and other media timings.**

Internal codename: **ReMedia**  
Public product name: **ReMedia Sync**

## Overview

ReMedia Sync is a .NET 10 solution for probing media files, exporting individual tracks, and later converting timing-sensitive assets between different masters and frame-rate families.

The initial implementation is intentionally split into phases:

- **Phase 0**: probing + same-format export
- **Phase 1**: FPS-aware conversion + codec conversion
- **Later**: loudness matching, clipping prediction, chapter/subtitle retiming, muxing into target masters, and optional native probing instead of ffprobe

## Current backend strategy

For now, ReMedia Sync uses external CLI tooling:

- `ffprobe` for probing
- `ffmpeg` for export and conversion
- room for other helper CLI tools later

The rest of the application does **not** depend directly on raw ffprobe JSON. Tool-specific DTOs and command building stay inside `ReMedia.Tooling`.

## Solution layout

```text
ReMedia.sln
├─ src/
│  ├─ ReMedia.Core/      # Domain models, abstractions, timing logic, codec catalogs
│  ├─ ReMedia.Tooling/   # ffprobe/ffmpeg runners, DTOs, command builders, adapters
│  ├─ ReMedia.Cli/       # CLI host for probing/export/testing workflows
│  └─ ReMedia.App/       # Desktop shell (WPF)
├─ tests/
│  ├─ ReMedia.Core.Tests/
│  └─ ReMedia.Tooling.Tests/
├─ docs/
│  ├─ architecture.md
│  ├─ phases.md
│  └─ tooling.md
├─ scripts/
│  ├─ setup-tools.ps1
│  └─ example-commands.ps1
└─ .github/
   ├─ copilot-instructions.md
   └─ workflows/ci.yml
```

## Phase roadmap

### Phase 0 — Probe + Export
- Probe one input file
- Display:
  - format/container
  - duration
  - all streams
  - chapters
  - language/title/default/forced flags where available
- Export selected non-video tracks as individual files
- Default to same-format/same-timing export
- Export chapter metadata separately
- Keep generated commands and raw tool output in logs

### Phase 1 — Timing + Convert
- Detect source/reference FPS from video stream
- Allow choosing destination FPS
- Print:
  - source FPS
  - target FPS
  - original duration
  - destination duration
  - stretch factor
  - audio tempo factor
- Convert/export audio with selectable codec/container
- Prepare subtitle/chapter retiming services
- Add loudness matching to destination and clipping-risk warnings

## Key architectural rules

- Keep ffprobe/ffmpeg integration behind interfaces.
- Do not leak raw ffprobe JSON DTOs outside `ReMedia.Tooling`.
- Domain logic belongs in `ReMedia.Core`.
- The CLI and desktop app both consume the same core abstractions.
- Prefer deterministic command generation and verbose logs.
- Design for a future native probe implementation without changing app workflows.

## Build

This repository targets **.NET 10**.

Typical commands:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project .\src\ReMedia.Cli\ReMedia.Cli.csproj -- probe "C:\media\movie.mkv"
```

## Desktop shell status

`ReMedia.App` is intentionally a thin WPF shell with a starter layout:
- input file picker area
- timing analysis area
- track list
- export/log sections

The real logic lives in the Core + Tooling + CLI-friendly services.

## Useful next tasks

1. Wire `FfprobeMediaProbeService` to real ffprobe JSON.
2. Expand `FfmpegTrackExportService` for chapter and subtitle export.
3. Finish `ReMedia.App` bindings and per-track export options.
4. Add loudness analysis and clipping prediction services.
5. Add mux-into-target workflows after Phase 1 stabilizes.
