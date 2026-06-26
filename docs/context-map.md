# ReMedia Sync context map

Use this file to decide which memory files to read for a task. Do not read every
file by default.

## Always read at session start

- [[index]]
- [[current-status]]
- [[environment]]
- [[instructions/agent-rules]]
- [[tasks/lessons]]

## Domain / timing model

Read when touching FPS handling, PAL/NTSC, timing analysis, export planning, or
anything in ReMedia.Core.

- [[domain-rules]]
- [[architecture]]

## Architecture / layering

Read when touching project boundaries, tool integration, or the Core/Tooling/Cli/App split.

- [[architecture]]
- [[tooling]]

## Media handling (audio / subtitles / chapters)

Read when implementing track export, conversion, retiming, or loudness work.

- [[media-handling]]
- [[domain-rules]]

## Tooling / ffprobe / ffmpeg

Read when touching the external-tool wrappers, command builders, or process execution.

- [[tooling]]
- [[architecture]]

## Coding / testing (always relevant when writing code)

- [[instructions/coding-and-testing]]
- [[instructions/cli-tooling]]

## Phases / roadmap

Read for scope and what belongs in the current phase.

- [[phases]]
