# Tooling notes

## Current backend
- ffprobe
- ffmpeg
- future optional helper CLI tools

## Logging expectations
Whenever a tool is invoked, capture:
- executable path
- exact arguments
- exit code
- stdout
- stderr
- elapsed time

## Important rule
Only `ReMedia.Tooling` should know about raw ffprobe JSON shapes.
The rest of the solution must consume normalized models.

## Native backend (future)

A later phase may replace ffprobe-based probing with a native implementation. The
solution is structured so this swap does not change app workflows — only the
`ReMedia.Tooling` implementation behind the Core abstractions changes.
