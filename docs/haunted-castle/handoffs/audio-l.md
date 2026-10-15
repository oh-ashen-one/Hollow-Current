# HANDOFF: audio-l (wave L, F4 audio, r12 critic)

Branch `hc/audio-l` (worktree `/Users/midir/hollow-current-haunted-castle/.wt/audio-l`), base `haunted-castle` af96343. Sonnet 5.5, 2026-10-14. Cost 0. CPU-only; no Unity, window, build or paid API. **Nothing here has been listened to** (loudness, spectrogram and numbers only); ASSET_LOG keeps "Human listen: not yet".

## New keys for F3 (hidden-player beats), 48 kHz mono 16-bit
| key | file | dur s | LUFS | peak dBFS | size |
|---|---|---|---|---|---|
| `castle_hide_breath_0` | `Assets/HollowCurrent/Resources/CastleAudio/castle_hide_breath_0.wav` | 4.32 | -28.0 | -13.1 | 414342 B |
| `castle_hide_breath_1` | `Assets/HollowCurrent/Resources/CastleAudio/castle_hide_breath_1.wav` | 3.50 | -28.0 | -12.8 | 336044 B |
| `castle_hide_clawstep` | `Assets/HollowCurrent/Resources/CastleAudio/castle_hide_clawstep.wav` | 1.68 | -28.0 | -8.9 | 160856 B |
| `castle_hide_handle` | `Assets/HollowCurrent/Resources/CastleAudio/castle_hide_handle.wav` | 1.00 | -29.1 | -5.0 | 96044 B |
| `castle_hide_whisper` | `Assets/HollowCurrent/Resources/CastleAudio/castle_hide_whisper.wav` | 2.90 | -28.0 | -13.7 | 278444 B |

Wiring for F3 (shared files are the integrator's; not edited here):
- Breath is a 2-variant set following the `claw_steps_` convention: `Sound.Cue("castle_hide_breath_"+Random.Range(0,2), pos, .6f)`; there is no bare `castle_hide_breath` file. If F3 wants the bare key, say so and I add it.
- Others by exact key: `castle_hide_clawstep`, `castle_hide_handle`, `castle_hide_whisper`. Suggested volumes and distances are in `docs/haunted-castle/AUDIO.md` (one-shots table, rows after `bell_single`).
- Spec met: -26..-30 LUFS (gated; -28.0, handle -29.1), peaks all < 0.6 full scale (handle -5.0 dBFS = 0.56, others -8.9 to -13.7), tail <= 0.15 s, breath 4.3 s and 3.5 s, clawstep 1.7 s, handle 1.0 s, whisper 2.9 s. Whisper is pure shaped noise with random formants, no words.

## Voices at 48 kHz: left at 22.05 kHz, on purpose
eSpeak NG outputs 22.05 kHz only, and the Count chain low-passes at 3 kHz (after the pitch drop). Measured on the voice files: content above 5 kHz is 33 to 38 dB below the total, above 8 kHz about 60 dB below, so a 48 kHz container would carry the same signal. It would be 2.2x larger (voice set 7.8 MB to 17 MB; each ledger reading 1.6 MB, over the 1 MB per-file rule). Unity resamples on import. If the owner still wants 48 kHz, it is one `resample_poly(x,320,147)` call in `tools/audio_castle_fix.py`; timbre would not change.

## Checks
- `python3 tools/check_audio_keys.py`: OK (65 uses, 18 required ids, 58 files). The five new keys are in its CONTRACT list; the `castle_hide_breath_` prefix needs 2 variants (others need 4).
- `python3 tools/check_audio_quality.py`: OK on all clips; new rule for every `castle_hide_*` (LUFS in -30..-26, peak < 0.6, tail <= 0.15 s). Same 12 Bellwether WARNs as before (shipped files, not edited).
- `python3 tools/check_source.py` (with `INPUT_SYSTEM_REFERENCE`): PASS 1071 assertions, C# compiles. No .cs changed.
- Regeneration order: `audio_castle_sfx.py <names>` -> `audio_castle_fix.py` -> `check_audio_quality.py`. Do not run `audio_castle_sfx.py` with no arguments after the wave K repair: it would regenerate `bell_single` and others unfixed; run the fix pass after.

## Files
`tools/audio_castle_sfx.py`, `tools/audio_castle_fix.py`, `tools/check_audio_keys.py`, `tools/check_audio_quality.py`, `docs/haunted-castle/AUDIO.md`, `docs/haunted-castle/ASSET_LOG.md`, this file, and the five WAVs with `.meta`.
