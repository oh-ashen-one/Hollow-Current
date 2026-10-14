# HANDOFF: audio4 (wave K, r11 critic audio section)

Branch `hc/audio4` (worktree `/Users/midir/hollow-current-haunted-castle/.wt/audio4`), base `haunted-castle` b428a8e. Sonnet 5.5, 2026-10-14. Cost 0. CPU-only; no Unity, window, build or paid API. Nothing was listened to: all results are measurements.

## What changed
Files edited in place in `Assets/HollowCurrent/Resources/CastleAudio/` (26 WAVs; `.meta` untouched, so guids and importer settings are unchanged): the loops `crypt_blood_trickle`, `forge_hold_hum`, `candle_whisper_bed`, `bell_single` and all 22 `voice_cc_*`.
1. **Loop seams.** Tail crossfaded into head (0.3 s, equal power). Concatenating each clip with itself, the join step is 0.0006 (trickle, was 0.0327), 0.0008 (forge, was 0.0124), 0.0000 (candle, was 0.0081); limit 0.01. Each loop is 0.3 s shorter (7.24 / 5.82 / 7.23 s); loudness unchanged.
2. **Voice level.** Castle voices measured -28.1 to -30.3 LUFS (median -29.0); the 9 Bellwether voices (`voice_evp` and `voice_whisper` excluded, see below) have median -24.20. Every `voice_cc_*` is now -24.2 LUFS (+3.9 to +6.1 dB), peaks -5.5 to -9.5 dBFS, no clipping.
3. **Tails.** Trailing silence (< -50 dBFS) was 1.54-1.92 s; now at most 0.15 s with a 60 ms fade. Voices are 1.4-1.9 s shorter. `bell_single` tail 0.30 -> 0.15 s (2.85 s).
4. **Gate.** `tools/check_audio_quality.py [-v] [--strict]` over all 93 clips: fails on peak > -1 dBFS, loop seam > 0.01, voice > 1.5 dB from the Bellwether median, non-loop tail > 0.3 s. It fails on the pre-fix files (all 22 voices on level and tail, the trickle and forge seams) and passes now. `tools/audio_castle_fix.py` is the idempotent repair pass (second run changes nothing). `tools/check_audio_keys.py`: OK (65 uses, 18 required, 56 files). `tools/check_source.py`: PASS 1036 assertions, C# compiles.
Order after any regeneration: `generate_open_voice.py --castle` -> `audio_castle_sfx.py` -> `audio_castle_fix.py` -> `check_audio_quality.py`.

## Before / after
| clip | dur s | LUFS | peak dBFS | tail s | wrap step |
|---|---|---|---|---|---|
| crypt_blood_trickle | 7.54 -> 7.24 | -30.0 -> -30.0 | -12.5 -> -12.5 | 0.00 -> 0.00 | 0.0327 -> 0.0006 |
| forge_hold_hum | 6.12 -> 5.82 | -29.9 -> -29.9 | -13.2 -> -13.2 | 0.00 -> 0.00 | 0.0124 -> 0.0008 |
| candle_whisper_bed | 7.53 -> 7.23 | -30.0 -> -29.9 | -12.2 -> -12.2 | 0.00 -> 0.00 | 0.0081 -> 0.0000 |
| bell_single | 3.00 -> 2.85 | -30.0 -> -30.0 | -14.9 -> -14.9 | 0.30 -> 0.15 | 0.0000 -> 0.0002 |
| voice_cc_bargain | 9.14 -> 7.54 | -29.0 -> -24.2 | -12.3 -> -7.5 | 1.75 -> 0.06 | 0.0008 -> 0.0014 |
| voice_cc_contain | 9.06 -> 7.46 | -29.4 -> -24.2 | -12.4 -> -7.2 | 1.75 -> 0.10 | 0.0003 -> 0.0005 |
| voice_cc_dispatch | 8.24 -> 6.64 | -28.6 -> -24.2 | -12.0 -> -7.6 | 1.75 -> 0.15 | 0.0000 -> 0.0000 |
| voice_cc_ending | 10.42 -> 8.84 | -29.6 -> -24.2 | -11.4 -> -6.0 | 1.73 -> 0.15 | 0.0000 -> 0.0000 |
| voice_cc_hint_signal | 10.03 -> 8.44 | -28.6 -> -24.2 | -11.1 -> -6.8 | 1.74 -> 0.13 | 0.0000 -> 0.0000 |
| voice_cc_hint_trace | 9.54 -> 7.97 | -28.8 -> -24.2 | -12.0 -> -7.5 | 1.71 -> 0.15 | 0.0000 -> 0.0000 |
| voice_cc_hint_voice | 9.24 -> 7.48 | -28.5 -> -24.2 | -11.0 -> -6.7 | 1.92 -> 0.06 | 0.0005 -> 0.0009 |
| voice_cc_hunt_found | 3.76 -> 2.22 | -30.3 -> -24.2 | -13.5 -> -7.4 | 1.69 -> 0.15 | 0.0000 -> 0.0000 |
| voice_cc_hunt_hungry | 6.44 -> 4.89 | -28.9 -> -24.2 | -12.8 -> -8.1 | 1.69 -> 0.15 | 0.0000 -> 0.0000 |
| voice_cc_hunt_run | 6.27 -> 4.72 | -29.1 -> -24.2 | -12.6 -> -7.9 | 1.69 -> 0.15 | 0.0002 -> 0.0004 |
| voice_cc_hunt_smell | 5.14 -> 3.60 | -28.8 -> -24.2 | -13.7 -> -9.2 | 1.69 -> 0.14 | 0.0009 -> 0.0014 |
| voice_cc_kit | 9.63 -> 8.08 | -30.3 -> -24.2 | -11.6 -> -5.5 | 1.69 -> 0.15 | 0.0000 -> 0.0000 |
| voice_cc_ledger_orrery | 18.10 -> 16.71 | -29.2 -> -24.2 | -11.8 -> -6.7 | 1.54 -> 0.15 | 0.0000 -> 0.0000 |
| voice_cc_ledger_portrait | 17.42 -> 16.03 | -29.1 -> -24.2 | -12.5 -> -7.7 | 1.54 -> 0.15 | 0.0000 -> 0.0000 |
| voice_cc_ledger_steward | 18.35 -> 16.91 | -29.0 -> -24.2 | -12.4 -> -7.6 | 1.59 -> 0.11 | 0.0003 -> 0.0005 |
| voice_cc_listen | 7.17 -> 5.57 | -28.6 -> -24.2 | -12.3 -> -7.8 | 1.75 -> 0.06 | 0.0008 -> 0.0013 |
| voice_cc_night | 7.88 -> 6.43 | -28.1 -> -24.2 | -13.4 -> -9.5 | 1.60 -> 0.15 | 0.0000 -> 0.0000 |
| voice_cc_relic_blade | 8.87 -> 7.40 | -29.4 -> -24.2 | -12.6 -> -7.4 | 1.63 -> 0.13 | 0.0000 -> 0.0000 |
| voice_cc_relic_hilt | 9.77 -> 8.32 | -29.6 -> -24.2 | -13.3 -> -7.9 | 1.60 -> 0.15 | 0.0002 -> 0.0003 |
| voice_cc_relic_moon | 8.39 -> 6.89 | -28.2 -> -24.2 | -12.8 -> -8.8 | 1.64 -> 0.13 | 0.0002 -> 0.0002 |
| voice_cc_stay | 9.00 -> 7.55 | -28.6 -> -24.2 | -12.0 -> -7.5 | 1.60 -> 0.15 | 0.0000 -> 0.0000 |
| voice_cc_steward | 9.29 -> 7.84 | -29.4 -> -24.2 | -13.2 -> -8.0 | 1.60 -> 0.15 | 0.0000 -> 0.0000 |

## Left alone, and why (WARN in the gate, not failures)
- Bellwether shipped files are not mine and were not edited. `voice_evp` (-29.4) and `voice_whisper` (-29.2) are deliberately quiet ghost voices (exempt from the loudness rule). 10 Bellwether voices have 0.37-0.91 s of tail and `HiggsfieldAudio/ui_paper` 0.51 s. `--strict` makes the gate fail on these. Say if you want them trimmed.
- `step_gravel_*` peak -2.6 dBFS: under the -1 dBFS limit, so not flagged (hottest clip; critic noted it).
- Not measured: how the +5 dB voices sit in the real mix against the heartbeat, wind and music-less bed; positional attenuation (`Speak` plays spatial voices at volume .70). Needs a human or captured mix.
- Loop wrap uses a crossfade, so the trickle may double a drip near the join for 0.3 s; listen once.

## Requests for the integrator
- Run `python3 tools/check_audio_quality.py` and `tools/check_audio_keys.py` in the integration pass.
- Spatial speech reserves a voice for the clip length (`Soundscape.Speak`); the shorter clips only help.
