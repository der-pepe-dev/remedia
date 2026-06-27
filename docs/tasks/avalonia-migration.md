# Task: WPF -> Avalonia frontend migration

Per [[0001-frontend-wpf-to-avalonia]]. Incremental; WPF App stays until parity, then removed.

## Increment 1 — runnable shell + probe (DONE 2026-06-27)
- [x] New `src/ReMedia.App.Avalonia` (net10.0, cross-platform). Avalonia 11.2.1 + DataGrid.
- [x] Reused portable pieces (ViewModelBase, Relay/AsyncRelayCommand, AppServiceFactory,
      CallbackToolLogger) and `IFilePicker` (StorageProvider) replacing WPF OpenFileDialog.
- [x] MainWindow: input + Browse + Probe -> tracks DataGrid + live log.
- [x] `tests/ReMedia.App.Avalonia.Tests` — VM probe tests (fake service, no ffprobe).
- [x] Both CI legs run the new test exe.
- [x] Verified: solution builds 0 warnings; app launches on WSLg (no crash); VM tests green.
- Note: end-to-end Probe needs ffprobe (not on this host) — covered by VM test with a fake.

## Next increments
- [ ] Export workflow (track selection, codec/container, output folder, mux, progress).
- [ ] Loudness (measure + `--target-lufs`-style match), timing analysis, audio sync.
- [ ] Multi-part sources; drag-drop input.
- [ ] Reach parity, then remove `ReMedia.App` (WPF) from the solution + CI (separate PR).

## Notes
- VM stays UI-framework-agnostic; file dialogs go through `IFilePicker`.
- `Tmds.DBus.Protocol` pinned to 0.21.3 (patched; Avalonia's transitive 0.20.0 had
  advisory GHSA-xrw6-gwf8-vvr9).
