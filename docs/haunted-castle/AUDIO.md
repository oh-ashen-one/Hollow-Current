# Haunted Castle audio wiring (for the integrator)

All clips are in `Assets/HollowCurrent/Resources/CastleAudio/`. `Soundscape.Initialize` only loads `Audio` and `HiggsfieldAudio`, so add one line there, guarded by map:
`foreach(var clip in Resources.LoadAll<AudioClip>("CastleAudio"))clips[clip.name]=clip;`
Clip names are the dictionary keys (filename without `.wav`). Do not add the castle ids to the required-asset throws for Bellwether.

Format: one-shots/loops are 48 kHz mono 16-bit WAV at about -30 LUFS integrated (peaks -7 to -16 dBFS), matching `HiggsfieldAudio`. Voices are 22.05 kHz mono at about -29 LUFS (house voices). Total folder about 12 MB; every file under 1 MB.

## One-shots (`Soundscape.Cue(key, position, volume)`; pitch jitter is applied by `Cue`)

| Key | Length | Trigger | Volume | Notes |
|---|---|---|---|---|
| `dive_whoosh` | 3.3 s | Presence enters Reach/Dive (Mode 3) | 0.8 at hunter position | audible about 25 m; played at the hunter, not the player |
| `wing_wrap_grab` | 1.4 s | Grab fires (1.05 m) | 0.9 at the player | pair with the existing capture sting `capture` |
| `stagger_screech` | 3.9 s | Fight hit lands (Mode 4 Stagger) | 0.9 at hunter | one per hit, three hits |
| `death_wail_sunrise` | 8.9 s | Fight hit 3 / Mode 5 Death | 0.8 non-spatial (use `UI`-style 2D source or `Cue` at the player) | wail then a major-chord swell; start the sunrise light fade at about 4 s |
| `chapel_bell_toll` | 6.9 s | Player rings the chapel bell (music-box replacement) | 1.0 at the bell | long tail; do not stack, ignore re-rings for 7 s |
| `drawbridge_chains` | 7.9 s | Drawbridge lowered (trigger or win) | 0.9 at the winch | ends on a landing thud at about 7.2 s |
| `orrery_tick` | 0.28 s | Orrery brass arm ticks, every 1.0 s while player is within 6 m in the Observatory | 0.5 at the orrery | pitch jitter from `Cue` is fine |

## Loops (new looping `AudioSource`s; `Soundscape.Loop` is 2D, so create spatial ones)

All loop files are seam-crossfaded (0.3 s) and are safe with `loop=true`. Set `spatialBlend=1`, `rolloffMode=Logarithmic`, `dopplerLevel=0`.

| Key | Length | Where | Volume | min / max distance |
|---|---|---|---|---|
| `wing_beats_loop` | 5.6 s | Child of the hunter while Mode is 1 or 2 (Glide/Search-Glide); fade in/out over 0.4 s; stop when perched | 0.7 | 2 / 24 |
| `forge_hold_hum` | 6.1 s | At the chapel altar while the forge hold is active (hold-to-craft), fade out on release or completion | 0.6 | 1.5 / 12 |
| `candle_whisper_bed` | 7.5 s | One source per floating-candle cluster (or one at the great hall centre); raise volume when the hunter is within 12 m (the candle tell) | 0.25 base, 0.6 with hunter near | 3 / 18 |
| `crypt_blood_trickle` | 7.5 s | At the crypt blood channel (y -3.2) | 0.5 | 1.5 / 14 |

## Voices (`Soundscape.Speak(id, location)`, prefix added automatically: file `voice_<id>.wav`)

Spatial speech max length is the clip length; the pool reserves a voice for it. Count voices are lower, slower and echoing, with a different treatment from Bellwether's `evp`/`whisper`.

| id | Length | Use |
|---|---|---|
| `cc_bargain` | 9.1 s | ear-trumpet capture whisper 1 |
| `cc_night` | 7.9 s | whisper 2 |
| `cc_steward` | 9.3 s | whisper 3 ("the steward hid them") |
| `cc_relic_blade` | 8.9 s | whisper 4, hints the silver blade (reliquary) |
| `cc_relic_hilt` | 9.8 s | whisper 5, hints the hilt (orrery) |
| `cc_relic_moon` | 8.4 s | whisper 6, hints the moonstone (portrait) |
| `cc_listen` | 7.2 s | whisper 7 (use near the player; scare line) |
| `cc_stay` | 9.0 s | whisper 8 (also good at the drawbridge) |
| `cc_ledger_steward` | 18.4 s | read-aloud of Blood Ledger entry 1 (blade, chapel crypt) |
| `cc_ledger_orrery` | 18.1 s | entry 2 (hilt, orrery) |
| `cc_ledger_portrait` | 17.4 s | entry 3 (moonstone, portrait door) |
| `cc_hunt_smell` | 5.1 s | hunt start, spatial at the hunter |
| `cc_hunt_run` | 6.3 s | during chase, spatial |
| `cc_hunt_found` | 3.8 s | on losing line of sight then regaining it / just before grab |
| `cc_hunt_hungry` | 6.4 s | search phase, spatial |

Ledger entries should be played non-spatial (`Speak(id)` with no location) when the player opens the entry; whispers spatial at the ear-trumpet capture position. Text of every line is in `tools/voice-lines.json`.

## Regeneration

`python3 tools/generate_open_voice.py --castle` (voices only; needs `espeak-ng`, `ffmpeg`). `python3 tools/audio_orrery_tick.py` (tick). Paid SFX are not reproducible from scripts: raw jobs and prompts are in `ASSET_LOG.md`; the committed post-processing is `tools/audio_castle_post.py`.
