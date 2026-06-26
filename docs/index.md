# ReMedia

Extract, retime, and sync audio, subtitles, and chapters across PAL/NTSC and other media timings

Repository: `https://github.com/der-pepe/remedia-sync`

## Main goals

- Extract, retime, and sync audio, subtitles, and chapters across PAL/NTSC and other
  media timings (public name: ReMedia Sync).
- Timing-aware workflow tool, not a generic ffmpeg GUI.
- Clean domain/tooling separation so a native probe backend can replace ffprobe later.
- Phased delivery: probe/export first, then FPS-aware conversion, then loudness/retiming.

## How agents should use this memory

- Start with this file, [[current-status]], [[instructions/agent-rules]], and [[tasks/lessons]].
- Use [[context-map]] to pick only the relevant docs for the task.
- Check [[environment]] before suggesting shell commands.
- Create one file per active task under `tasks/` (parallel tasks supported).
- Use [[tasks/todo]] as the durable backlog only.

## Instructions

- [[instructions/agent-rules]]
- [[instructions/cli-tooling]]
- [[context-map]]

## Task tracking

- [[tasks/todo]] — durable backlog by priority
- `tasks/<task-name>.md` — one file per active task
- `tasks/done/` — completed task files
- [[tasks/lessons]] — correction patterns and recurring mistakes
- [[tasks/task-template]] — reusable task note template

## Main documents

- [[current-status]]
- [[environment]]
- [[sketchpad]] — scratch capture for raw ideas (NOT durable; do not act on without promotion)

- [[domain-rules]] — PAL/NTSC, timing math, reference streams
- [[architecture]] — Core/Tooling/Cli/App layering and hard rules
- [[media-handling]] — audio, subtitle, chapter rules
- [[tooling]] — ffprobe/ffmpeg wrappers, logging, native-backend note
- [[phases]] — phase plan
- [[instructions/coding-and-testing]] — conventions + test expectations
