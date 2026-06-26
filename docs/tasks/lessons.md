# Lessons

Correction patterns and recurring mistakes. Append a dated entry after any user
correction or hard-won lesson. Newest first.

<!-- - YYYY-MM-DD: <what went wrong> -> <the rule to follow next time> -->

- 2026-06-26: Subtitle parsers parsed the fractional-seconds field directly as a
  millisecond integer (`.5` -> 5ms instead of 500ms). -> A decimal fraction must be
  scaled by digit count (1 digit = tenths, 2 = hundredths, 3 = ms). Shared helper:
  `SubtitleTimeParsing.TryParseFractionalMs`. Applies to any `mm:ss.fff`-style field.
- 2026-06-26: An audit agent claimed the ffmpeg concat demuxer mis-tokenizes Windows
  backslash paths. -> Wrong: inside the concat demuxer's single quotes, backslash is
  literal; only `'` is special. Verify escaping claims against the tool's actual quoting
  rules (and existing tests that encode intended behavior) before "fixing".
- 2026-06-26: `dotnet test` reports "No test is available" for xunit.v3 projects. -> They
  use Microsoft.Testing.Platform; run the built test executable directly
  (`dotnet <proj>/bin/Debug/net10.0/<Proj>.Tests.dll`) instead.
- 2026-06-26: The WPF `ReMedia.App` project can't build on WSL/Linux (NETSDK1100,
  Windows-targeted). -> Build/test the non-App projects individually; don't `dotnet build
  ReMedia.sln` on Linux and treat the App failure as a regression.
  UPDATE 2026-06-27: Fixed by adding `<EnableWindowsTargeting>true</EnableWindowsTargeting>`
  to `ReMedia.App.csproj`. The full solution now builds on WSL. Caveat: it still cannot
  *run* on Linux (WPF runtime is Windows-only) — build verification only.
