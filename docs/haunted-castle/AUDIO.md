# Haunted Castle audio wiring (for the integrator)

All clips are in `Assets/HollowCurrent/Resources/CastleAudio/`. `Soundscape.Initialize` only loads `Audio` and `HiggsfieldAudio`, so add one line there, guarded by map:
`foreach(var clip in Resources.LoadAll<AudioClip>("CastleAudio"))clips[clip.name]=clip;`
Clip names are the dictionary keys (filename without `.wav`). Do not add the castle ids to the required-asset throws for Bellwether.

Format: one-shots/loops are 48 kHz mono 16-bit WAV at about -30 LUFS integrated (peaks -7 to -16 dBFS), matching `HiggsfieldAudio`. Voices are 22.05 kHz mono at -24.2 LUFS, the median of Bellwether's voices (see Level and tail rules below). Total folder about 13 MB; every file under 1 MB. The synthesised fear-wave clips are capped at -8 dBFS peak (loudness -30 LUFS; the 0.3-0.6 s transients land at -31 to -34 LUFS because the peak cap wins).

## One-shots (`Soundscape.Cue(key, position, volume)`; pitch jitter is applied by `Cue`)

| Key | Length | Trigger | Volume | Notes | Human listen |
|---|---|---|---|---|---|
| `dive_whoosh` | 3.3 s | Presence enters Reach/Dive (Mode 3) | 0.8 at hunter position | audible about 25 m; played at the hunter, not the player | not yet |
| `wing_wrap_grab` | 1.4 s | Grab fires (1.05 m) | 0.9 at the player | pair with the existing capture sting `capture` | not yet |
| `stagger_screech` | 3.9 s | Fight hit lands (Mode 4 Stagger) | 0.9 at hunter | one per hit, three hits | not yet |
| `death_wail_sunrise` | 8.9 s | Fight hit 3 / Mode 5 Death | 0.8 non-spatial (use `UI`-style 2D source or `Cue` at the player) | wail then a major-chord swell; start the sunrise light fade at about 4 s | not yet |
| `chapel_bell_toll` | 6.9 s | Player rings the chapel bell (music-box replacement) | 1.0 at the bell | long tail; do not stack, ignore re-rings for 7 s | not yet |
| `drawbridge_chains` | 7.9 s | Drawbridge lowered (trigger or win) | 0.9 at the winch | ends on a landing thud at about 7.2 s | not yet |
| `orrery_tick` | 0.28 s | Orrery brass arm ticks, every 1.0 s while player is within 6 m in the Observatory | 0.5 at the orrery | pitch jitter from `Cue` is fine | not yet |
| `door_rattle` | 2.5 s | Cupboard near-miss (FEAR_PLAN 10): he rattles the hide-spot door for 3 s; play once at the cupboard when the rattle starts (re-trigger at 2.5 s if the rattle runs longer) | 0.8 at the cupboard door | audible about 12 m; wood on wood, small-room tail | not yet |
| `book_drop` | 0.6 s | Castle dread beat (15): a book falls from a floater and hits the stone floor | 0.7 at the book's landing point | audible about 18 m; pitch jitter from `Cue` is fine | not yet |
| `knife_stick` | 0.5 s | Castle dread beat (15): one orbit knife leaves `KnifeOrbit` and sticks in the table (play on impact, not on release) | 0.7 at the table | audible about 14 m; steel twang rings out about 0.4 s | not yet |
| `claw_steps_0` .. `claw_steps_3` | 0.3 s each | Count's claw-on-stone footfalls (13): pick a random index, never the same twice in a row, one per step while he glides low or walks; cadence about 0.45 s | 0.05 outside hunts (the 5 % rule), 0.5 inside hunts, at the hunter | audible about 10 m at hunt volume; four distinct strikes (toe count, stone ring, scrape differ) | not yet |
| `bat_pass` | 1.2 s | Castle dread beat (15): a bat crosses the torch beam; also usable for wingbeats overhead with no model (9) | 0.6 at the start of the bat's path (move the source along it if convenient) | audible about 16 m; loudest at 0.55 s | not yet |
| `bell_single` | 2.9 s | Chapel bell tolls once by itself (15, a false tell). One strike of the existing `chapel_bell_toll`, tail cut to 3 s. Ringing the real bell still uses `chapel_bell_toll` | 0.9 at the bell | audible across most of the castle (about 40 m); do not stack with `chapel_bell_toll` | not yet |

## Loops (new looping `AudioSource`s; `Soundscape.Loop` is 2D, so create spatial ones)

All loop files are seam-crossfaded (0.3 s) and are safe with `loop=true`. Set `spatialBlend=1`, `rolloffMode=Logarithmic`, `dopplerLevel=0`.

| Key | Length | Where | Volume | min / max distance | Human listen |
|---|---|---|---|---|---|
| `wing_beats_loop` | 5.6 s | Child of the hunter while Mode is 1 or 2 (Glide/Search-Glide); fade in/out over 0.4 s; stop when perched | 0.7 | 2 / 24 | not yet |
| `forge_hold_hum` | 5.8 s | At the chapel altar while the forge hold is active (hold-to-craft), fade out on release or completion | 0.6 | 1.5 / 12 | not yet |
| `candle_whisper_bed` | 7.2 s | One source per floating-candle cluster (or one at the great hall centre); raise volume when the hunter is within 12 m (the candle tell) | 0.25 base, 0.6 with hunter near | 3 / 18 | not yet |
| `crypt_blood_trickle` | 7.2 s | At the crypt blood channel (y -3.2) | 0.5 | 1.5 / 14 | not yet |
| `heartbeat_loop` | 6.9 s | Non-spatial (`spatialBlend=0`, a 2D `AudioSource` on the listener, i.e. `Soundscape.Loop`). 52 bpm double-thump, rate is fixed. Start it when Dread is above 40 or a hunt is active (12) | volume follows Dread: 0 at Dread 40, 0.15 at 60, 0.35 at 80, 0.55 at 100; during a hunt hold at least 0.4. Ramp over 1 s | n/a (2D). Content is below about 400 Hz, so it carries on speakers only at the louder settings | not yet |
| `wind_battlements_loop` | 10.0 s | Castle outdoor/battlement bed (12): at the gatehouse battlement and the roof walks, plus a quiet bed (0.1) on upper-floor windows. One source per wind zone, or one 2D source with volume by distance to the open air | 0.35 | 3 / 26 | not yet |

## Voices (`Soundscape.Speak(id, location)`, prefix added automatically: file `voice_<id>.wav`)

Spatial speech max length is the clip length; the pool reserves a voice for it. Count voices are lower, slower and echoing, with a different treatment from Bellwether's `evp`/`whisper`.

| id | Length | Use | Human listen |
|---|---|---|---|
| `cc_bargain` | 7.5 s | ear-trumpet capture whisper 1 | not yet |
| `cc_night` | 6.4 s | whisper 2 | not yet |
| `cc_steward` | 7.8 s | whisper 3 ("the steward hid them") | not yet |
| `cc_relic_blade` | 7.4 s | whisper 4, hints the silver blade (reliquary) | not yet |
| `cc_relic_hilt` | 8.3 s | whisper 5, hints the hilt (orrery) | not yet |
| `cc_relic_moon` | 6.9 s | whisper 6, hints the moonstone (portrait) | not yet |
| `cc_listen` | 5.6 s | whisper 7 (use near the player; scare line) | not yet |
| `cc_stay` | 7.6 s | whisper 8 (also good at the drawbridge) | not yet |
| `cc_ledger_steward` | 16.9 s | read-aloud of Blood Ledger entry 1 (blade, chapel crypt) | not yet |
| `cc_ledger_orrery` | 16.7 s | entry 2 (hilt, orrery) | not yet |
| `cc_ledger_portrait` | 16.0 s | entry 3 (moonstone, portrait door) | not yet |
| `cc_hunt_smell` | 3.6 s | hunt start, spatial at the hunter | not yet |
| `cc_hunt_run` | 4.7 s | during chase, spatial | not yet |
| `cc_hunt_found` | 2.2 s | on losing line of sight then regaining it / just before grab | not yet |
| `cc_hunt_hungry` | 4.9 s | search phase, spatial | not yet |
| `cc_kit` | 8.1 s | Said when the player takes the instruments (`Book.KitCue`), non-spatial or spatial at the kit; text "Take them. The steward took them too. They did not help him." | not yet |
| `cc_ending` | 8.8 s | `Phase.Won` (kill sunrise or the drawbridge escape); non-spatial, start about 1.5 s after the ending screen appears; text "Dawn. I had forgotten its colour. Go, and leave the door open." | not yet |
| `cc_contain` | 7.5 s | `Speak("contain")`: entering `Phase.Containment` (the bell-rope binding) and the radio hint once the case is diagnosed (`Game.RadioHint`); non-spatial; text "Ring it, then. Better hands than yours have put me to bed." | not yet |
| `cc_hint_signal` | 8.4 s | `Game.RadioHint` before diagnosis when the compass/signal evidence is missing (`Speak("hint_signal")`); non-spatial; text "The needle leans toward whatever I loved. Follow it, by all means." | not yet |
| `cc_hint_trace` | 8.0 s | same, trace evidence missing (`Speak("hint_trace")`); text "Hold your ash lamp to my floors. The stone keeps my signature." | not yet |
| `cc_hint_voice` | 7.5 s | same, ear-trumpet evidence missing (`Speak("hint_voice")`); text "Ask the old trumpet in a still room. I never refuse a guest." | not yet |
| `cc_dispatch` | 6.6 s | Hostile-Return telegraph (wave G): the Count announces he is coming while the player crosses to the drawbridge. Call `Speak("dispatch")` or `Speak("cc_dispatch")` once when the Return hunt begins; non-spatial (or at the hunter). Not yet referenced by C# (the integrator wires it); `tools/check_audio_keys.py` keeps it on disk meanwhile. Text "The gate is open. So am I. Do hurry." | not yet |

Ledger entries should be played non-spatial (`Speak(id)` with no location) when the player opens the entry; whispers spatial at the ear-trumpet capture position. Text of every line is in `tools/voice-lines.json`.

Every row carries a **Human listen** column. `not yet` means no person has listened to that clip in the game; the critic or the owner replaces it with a name and date once cleared. As of 2026-10-14 nothing in this folder has been human-listened (the synthesised and eSpeak clips were checked by loudness and spectrogram only).

## Level and tail rules (wave K, `hc/audio4`)

Castle voices (`voice_cc_*`) are set to the median integrated loudness of Bellwether's `Resources/Audio/voice_*` clips (-24.2 LUFS, BS.1770; `voice_evp` and `voice_whisper` are Bellwether's deliberately quiet ghost voices and are left out of that median), peaks at -5.5 to -9.5 dBFS. Trailing silence is cut to at most 0.15 s with a 60 ms fade (it was 1.5-1.9 s, so voice lengths in the tables above dropped by 1.4-1.9 s). This matters for pooling: spatial speech reserves a voice for the clip length. The three loops that clicked at the wrap (`crypt_blood_trickle` 0.033, `forge_hold_hum` 0.012, `candle_whisper_bed` 0.008 full scale) had their tail crossfaded into the head (0.3 s equal power, so each is 0.3 s shorter; wrap step now below 0.001). `bell_single` tail trimmed to 0.15 s. Pipeline order after any regeneration: `generate_open_voice.py --castle`, `audio_castle_sfx.py`, `audio_castle_fix.py`, then `check_audio_quality.py`.

## Key check

`python3 tools/check_audio_keys.py` (add `-v` to list every key) scans `Runtime/` and `Core/` for every `Speak`/`Cue`/`Loop`/`SpatialLoop` key, `.Key=` assignment, `"cc_*"` literal and required-id array, applies the `Soundscape.Speak` castle mapping, and exits 1 if any file is missing from `Resources/Audio`, `HiggsfieldAudio` or `CastleAudio`. It also holds a contract list of keys promised here even when no C# calls them yet. Run it before every commit that touches audio or a `Speak`/`Cue` call.

`python3 tools/check_audio_quality.py [-v] [--strict]` fails on a peak above -1 dBFS, a loop seam step above 0.01, a voice more than 1.5 dB from the Bellwether median, or more than 0.3 s of trailing silence, over all 93 clips. The shipped Bellwether clips (`Resources/Audio`, `HiggsfieldAudio`) that break the voice or tail rule are reported as WARN, not edited (11 Bellwether voices carry 0.37-0.91 s of tail, `ui_paper` 0.51 s).

## Regeneration

`python3 tools/generate_open_voice.py --castle` (voices only; needs `espeak-ng`, `ffmpeg`; name entries to render only those, e.g. `--castle cc_contain`). `python3 tools/audio_orrery_tick.py` (tick). `python3 tools/audio_castle_sfx.py [name ...]` regenerates the fear-wave clips (`heartbeat_loop`, `wind_battlements_loop`, `door_rattle`, `book_drop`, `knife_stick`, `claw_steps_0..3`, `bat_pass`, `bell_single`; `bell_single` reads `chapel_bell_toll.wav`, so regenerate that one only if the toll is unchanged); `python3 tools/audio_castle_sfx.py --check` prints rate, channels, duration, size, peak, LUFS and loop-seam steps for every file it owns plus the Count voices (`voice_cc_kit`, `voice_cc_ending`, `voice_cc_contain`, `voice_cc_hint_signal`, `voice_cc_hint_trace`, `voice_cc_hint_voice`, `voice_cc_dispatch`). Paid SFX are not reproducible from scripts: raw jobs and prompts are in `ASSET_LOG.md`; the committed post-processing is `tools/audio_castle_post.py`.
