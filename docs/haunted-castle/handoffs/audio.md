# HANDOFF — piece 6 audio (`hc/audio`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/audio`)

## Done (verified by ffprobe/ebur128, no Unity launched)
- 15 Count Claude voice lines (`cc_*`) appended to `tools/voice-lines.json` (append-only diff checked); rendered to `Assets/HollowCurrent/Resources/CastleAudio/voice_cc_*.wav` by `python3 tools/generate_open_voice.py --castle` (deterministic: md5 identical across two runs). Existing Bellwether voices untouched (the `--castle` flag skips them).
- 10 Seed Audio SFX + 1 synthesised tick in `CastleAudio/`, all 48 kHz mono, about -30 LUFS, each under 1 MB (folder 12 MB): wing_beats_loop, dive_whoosh, wing_wrap_grab, stagger_screech, death_wail_sunrise, chapel_bell_toll, forge_hold_hum, drawbridge_chains, candle_whisper_bed, crypt_blood_trickle, orrery_tick (tick is local, `tools/audio_orrery_tick.py`, 0 credits).
- Credits: 17.9 for 10 Seed Audio jobs. Job ids/prompts in `docs/haunted-castle/ASSET_LOG.md`; licences in `ASSET_LICENSES.md`.
- Wiring spec for the integrator: `docs/haunted-castle/AUDIO.md` (keys, triggers, loop flags, distances, volumes).

## Not done / open issues
- Nobody has listened to the clips; checks were duration, loudness, peak and envelope only. The critic or owner should audition: stagger_screech and death_wail_sunrise (prompt asked no words; Seed Audio could add vocal-like artefacts), candle_whisper_bed (may contain murmurs).
- Voices are 22.05 kHz eSpeak NG (robotic by nature); Count lines run 4 to 10 s, ledger readings about 18 s because of the slow rate and echo tail. If too long, raise `rate` in the cc_ entries and re-run the generator.
- Loop seams are 0.3 s crossfaded, unverified by ear.
- Seed Audio cost estimate (`generate cost`) says 0.1 but real spend is 1.6-2.0 per job.

## Requests for integrator
1. In `Soundscape.Initialize`, load `Resources.LoadAll<AudioClip>("CastleAudio")` into `clips` for the Castle map (do not add to the Bellwether required-asset throws).
2. Add spatial looping sources for `wing_beats_loop`, `forge_hold_hum`, `candle_whisper_bed`, `crypt_blood_trickle` (spec in AUDIO.md).
3. Call `Speak("cc_...")` / `Cue("dive_whoosh", ...)` etc. at the triggers listed in AUDIO.md.
