# HANDOFF: audio3 (wave G, static fix wave off round-10 finding #40)

Branch `hc/audio3` (worktree `/Users/midir/hollow-current-haunted-castle/.wt/audio3`), base `haunted-castle` c4b24c1. Model: Sonnet 5.5. 2026-10-14. Cost 0. CPU-only (eSpeak NG, ffmpeg, numpy); no window, Unity, build or game run.

## New keys (all in `Assets/HollowCurrent/Resources/CastleAudio/`, with `.wav.meta`)
Same recipe as `voice_cc_kit` (`generate_open_voice.py --castle`, `CASTLE_FX`); entries appended to `tools/voice-lines.json`. Regenerating all `cc_` voices left every existing file byte-identical.
| voice_cc_hint_signal | 22050 | 1 | 10.03 | 442608 | -11.1 | -28.6 |  |
| voice_cc_hint_trace | 22050 | 1 | 9.54 | 420718 | -12.0 | -28.8 |  |
| voice_cc_hint_voice | 22050 | 1 | 9.24 | 407782 | -11.0 | -28.5 |  |
| voice_cc_dispatch | 22050 | 1 | 8.24 | 363390 | -12.0 | -28.6 |  |

| key | line | wired by |
|---|---|---|
| `voice_cc_hint_signal` | "The needle leans toward whatever I loved. Follow it, by all means." | `Game.RadioHint` (`Speak("hint_signal")`), already in C# |
| `voice_cc_hint_trace` | "Hold your ash lamp to my floors. The stone keeps my signature." | same, `hint_trace` |
| `voice_cc_hint_voice` | "Ask the old trumpet in a still room. I never refuse a guest." | same, `hint_voice` |
| `voice_cc_dispatch` | "The gate is open. So am I. Do hurry." | NOT yet called by C#: integrator calls `Sound.Speak("dispatch")` (or `"cc_dispatch"`) once when the hostile Return begins |

`voice_cc_contain` (other builder) and `cc_hunt_smell` untouched. The other pre-diagnosis radio keys (`whisper`, `warning`, `return`, `contain`) already resolve.

## Check
`python3 tools/check_audio_keys.py [-v]` (new): scans Runtime/ and Core/ (Speak/Cue/Loop/CastleLoop/SpatialLoop/UI/Has/ContainsKey, `.Key=`, `"cc_*"` literals, required-id arrays, `claw_steps_` prefix, ternaries, Speak variables) against Audio, HiggsfieldAudio and CastleAudio, plus a CONTRACT list of keys promised in AUDIO.md. Result: **OK, 63 distinct uses, 18 required ids, 56 files, exit 0**. Negative test: removing `voice_cc_hint_trace.wav` (and `voice_cc_dispatch.wav`, via CONTRACT) makes it exit 1 with the key and the call site. Before this branch it would have failed on `voice_cc_hint_signal`, `voice_cc_hint_trace`, `voice_cc_hint_voice`. Limits: ids built from non-literals are printed as `dynamic` notes; they are covered only through the `cc_` literal rule.
`python3 tools/check_source.py` with `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll`: PASS 487 assertions, runtime and editor C# compile (no .cs changed here).

## Docs
`docs/haunted-castle/AUDIO.md`: rows for the four voices; a **Human listen** column (`not yet`) on every table; a Key check section. `ASSET_LOG.md`: inventory of all 44 CastleAudio clips with origin, licence and **Human listen = not yet**. No clip has been listened to by a person (loudness/spectrogram only); critic or owner clears each cell.

## Requests for the integrator
- Wire `Sound.Speak("dispatch")` for the hostile-Return telegraph (CastleRun/Game, shared files).
- `ASSET_LICENSES.md` (not my file) has no line for the synthesised castle SFX (`audio_castle_sfx.py` clips, project CC BY 4.0) nor for `bell_single` (derivative of the Higgsfield bell, same grant). Wording is in `ASSET_LOG.md`.
- Run `python3 tools/check_audio_keys.py` in the integration pass.
