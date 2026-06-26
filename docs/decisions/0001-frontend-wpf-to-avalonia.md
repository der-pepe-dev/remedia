# 0001 — Frontend: migrate from WPF to Avalonia

Status: accepted (2026-06-27)

## Context

The desktop frontend (`ReMedia.App`) is WPF (`net10.0-windows`, `UseWPF`). WPF is
Windows-only at runtime — it builds on Linux/WSL only via `EnableWindowsTargeting` and
cannot run there. The maintainer intends to move the frontend to Avalonia soon for
cross-platform (Linux/WSL/macOS) runtime support.

Per `architecture.md`, the App is a thin shell: domain logic, workflows, and tool
integration live in `ReMedia.Core` / `ReMedia.Tooling`, and the `ReMedia.Cli` host
already exercises the full pipeline. So the frontend swap is low-friction and contained
to the App layer.

## Decision

- The frontend will move from WPF to Avalonia.
- Until then, **do not invest further in the WPF App** beyond keeping it compiling.
  Avoid new WPF-only features and WPF ViewModel test suites that won't survive the move.
- Keep all real logic in `ReMedia.Core` / `ReMedia.Tooling` so it is reused unchanged by
  the new frontend (e.g. loudness matching via `ILoudnessService`, already wired in CLI).

## Consequences

- Backlog items scoped to the WPF App (App loudness surface, WPF ViewModel tests) are
  deferred/deprioritized in favour of the Avalonia migration. See `tasks/todo.md`.
- New UI-facing capabilities should be proven in `ReMedia.Cli` first; the GUI consumes
  the same Core services.
- `EnableWindowsTargeting` on `ReMedia.App` stays until the WPF project is replaced, so
  the solution keeps building on Linux CI in the meantime.
