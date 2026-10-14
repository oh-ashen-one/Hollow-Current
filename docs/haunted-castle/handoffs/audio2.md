# HANDOFF: piece F4 (audio), Haunted Castle fear wave

Branch `hc/audio2` (worktree `/Users/midir/hollow-current-haunted-castle/.wt/audio2`), from `haunted-castle` @ fbd5807. Model: Sonnet 5.5. Date 2026-10-13. Cost: 0 (no paid generation).

## Done (verified how)
All 13 files exist in `Assets/HollowCurrent/Resources/CastleAudio/` with `.wav.meta` (importer settings copied from the other clips, fresh guids). Table is the output of `python3 -B tools/audio_castle_sfx.py --check`
(own BS.1770 K-weighted loudness, cross-checked against ffmpeg `ebur128`: heartbeat/wind/door -30.0 both, chapel_bell_toll -29.88 vs -29.9). pyloudnorm is not installed.

| file | rate | ch | dur s | bytes | peak dBFS | LUFS | seam |
|---|---|---|---|---|---|---|---|
| heartbeat_loop | 48000 | 1 | 6.92 | 664660 | -11.4 | -30.0 | |x[0]-x[-1]| 6 LSB vs p99 step 57 LSB |
| wind_battlements_loop | 48000 | 1 | 10.00 | 960044 | -12.0 | -30.0 | |x[0]-x[-1]| 38 LSB vs p99 step 392 LSB |
| door_rattle | 48000 | 1 | 2.50 | 240044 | -8.9 | -30.0 |  |
| book_drop | 48000 | 1 | 0.60 | 57644 | -8.0 | -31.7 |  |
| knife_stick | 48000 | 1 | 0.50 | 48044 | -8.0 | -31.2 |  |
| bat_pass | 48000 | 1 | 1.20 | 115244 | -9.4 | -30.0 |  |
| bell_single | 48000 | 1 | 3.00 | 288044 | -14.9 | -30.0 |  |
| claw_steps_0 | 48000 | 1 | 0.30 | 28844 | -8.0 | -32.6 |  |
| claw_steps_1 | 48000 | 1 | 0.30 | 28844 | -8.0 | -33.8 |  |
| claw_steps_2 | 48000 | 1 | 0.30 | 28844 | -8.0 | -30.4 |  |
| claw_steps_3 | 48000 | 1 | 0.30 | 28844 | -8.0 | -31.1 |  |
| voice_cc_kit | 22050 | 1 | 9.63 | 424686 | -11.6 | -30.3 |  |
| voice_cc_ending | 22050 | 1 | 10.42 | 459668 | -11.4 | -29.6 |  |

Notes: transients under 0.6 s are peak-capped at -8 dBFS, so they sit 1.5 to 3.8 LU under -30 LUFS (measured padded to 1 s). Loops are periodic by construction (circular FFT filtering, integer-cycle modulators, tail wrapped onto the start), so there is no seam to crossfade; the seam step is below the 99th-percentile sample step. Not listened to (no audio device in this session): checked by numbers and spectrograms only. The critic should listen.
`wind_battlements_loop` is 10.0 s, not 11-12 s, to stay under 1 MB (960 KB).
Voice lines (in `tools/voice-lines.json`, appended): `cc_kit` "Take them. The steward took them too. They did not help him." and `cc_ending` "Dawn. I had forgotten its colour. Go, and leave the door open." (works for both the kill sunrise and the drawbridge escape; `Game.cs` plays `Speak("ending")` on `Phase.Won`). Same `CASTLE_FX` chain as every other `voice_cc_*`; regenerating all `cc_` voices leaves the existing files byte-identical.

## Files
- `tools/audio_castle_sfx.py` (generator + `--check`), `tools/voice-lines.json`, `docs/haunted-castle/AUDIO.md` (row per key: trigger, volume, distances), `docs/haunted-castle/ASSET_LOG.md` (appended, cost 0).

## Requests for the integrator
- `Soundscape.Speak("kit")` and `Speak("ending")` already map to `cc_kit` / `cc_ending`; nothing to change there. The new SFX are keyed by filename like the rest (`Cue("door_rattle",pos,.8f)` etc.), values in AUDIO.md.
- `heartbeat_loop` is meant for a 2D source whose volume you ramp with Dread (AUDIO.md gives a curve); `claw_steps_0..3`: choose a random index, never the same twice running.

## Not done / open
- No paid clip is needed. If the critic finds the synthetic bat or claws too thin, a Higgsfield audio job would be the upgrade (not requested; needs the director's go-ahead).
