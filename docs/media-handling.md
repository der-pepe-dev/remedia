# Media handling

Per-track-type rules for audio, subtitles, and chapters. Read when implementing
export, conversion, retiming, or loudness work.

## Audio

Audio is the first conversion target. Design for: same-format export (Phase 0),
codec/container selection (Phase 1), later loudness matching against destination
audio, later clipping prediction and safety warnings.

Model concepts to leave room for: measured loudness, target loudness, applied gain
(dB), predicted sample peak, predicted true peak, clipping warning state.

When loudness matching is added, the workflow is conceptually: extract → retime →
analyze loudness → apply gain/normalization → warn if clipping becomes excessive →
encode.

## Subtitles

Subtitles are not one thing. Keep separate handling paths for:
- text subtitles (`srt`, `ass`, `ssa`, `vtt`)
- bitmap subtitles (`vobsub`, `pgs`, etc.)

Do not assume all subtitle formats can be copied, converted, or retimed the same way.

## Chapters

Chapters should be probeable and exportable in Phase 0, and later retimeable and
editable. Preserve metadata where possible: language, title, default, forced,
hearing-impaired.
