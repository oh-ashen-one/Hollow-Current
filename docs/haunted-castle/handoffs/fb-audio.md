# fb-audio: castle footsteps and the Count's voice (owner feedback r13 items 1, 2, 11)

Branch `hc/fb-audio` (worktree `.wt/audio`). Cost 0. Nothing here touches scares/scheduling, doors, lighting, layout or UI.

## What changed and why

| Owner item | Change | Files |
|---|---|---|
| 1 "little dot dinging" every step | The ding is the 335-390 Hz tone in the shared `step_tile_*` clips (measured: 20-23 dB over its spectral neighbours, band flatness 0.07). The castle no longer plays them: `World.StepSurface` returns `"stone"` for indoor/safe castle positions (was `"tile"`). New `step_stone_0..3` in `Resources/CastleAudio` are built from filtered noise only (heel thud, sole scuff, cloth swish, toe settle, short hall tail): worst 150-600 Hz bin over its neighbours is 2.0-3.1 dB, flatness 0.92-0.97. The shared `step_gravel_*` (outdoors) has no tone (7-11 dB, flat 0.54-0.75) but peaks at -2.6 dBFS, so the castle gets its own quiet `step_gravel_0..3` that override it by file name (Soundscape layers CastleAudio over Audio; Bellwether is unchanged). | `Assets/HollowCurrent/Runtime/World.cs`, `Resources/CastleAudio/step_stone_*.wav(.meta)`, `step_gravel_*.wav(.meta)`, `tools/audio_castle_steps.py` |
| 2 shuffle "way too loud" | Clips peak -14 dBFS (shared ones: -7 tile, -2.6 gravel). `Investigator` cue volume in the castle only: 0.11 walking (was 0.22), 0.15 running, 0.06 crouched; Bellwether keeps 0.22. Net walking level is about 13 dB below the old tile step. `Creature.Hear` radii are unchanged. | `Assets/HollowCurrent/Runtime/Investigator.cs` (line ~260) |
| 11 voice "really bad" | All 22 `cc_*` lines re-rendered with Kokoro-82M (Apache-2.0), voice `bm_george`, speed 0.84, then a new dark chain: 5 % pitch/tempo drop, voice-gated breath layer (-27 dB), low shelf +4 dB at 220 Hz, gentle compression, stone hall (early reflections + 2.2 s dark tail), 48 kHz mono PCM16, -24.2 LUFS, peak -9 to -12 dBFS (cap -3). Same file names, same `.meta`. Line texts lightly rewritten to be shorter/colder in `tools/voice-lines.json` (ids and meanings unchanged; clue content of the three ledger lines preserved; the lines are spoken only, no on-screen copy depends on them). Bellwether's eSpeak lines untouched. | `Resources/CastleAudio/voice_cc_*.wav`, `tools/generate_castle_voice.py`, `tools/voice-lines.json`, `tools/generate_open_voice.py` (now skips `cc_*`) |

Also: `tools/check_audio_keys.py` (surface `stone`, found in CastleAudio), `tools/check_audio_quality.py` (new tonality rule on every `step_*` clip), `docs/haunted-castle/AUDIO.md`, `ASSET_LOG.md`, `ASSET_LICENSES.md`.

Loudness note: the brief asked for about -18 LUFS, but `check_audio_quality.py` fails any `voice_*` more than 1.5 dB from Bellwether's median (-24.2 LUFS) and that gate is strict for CastleAudio, so the voices match -24.2 like the eSpeak set did (relative level in the mix is unchanged; Speak still plays at 0.70 spatial). If the owner finds the Count too quiet, raise `Soundscape.Speak`'s 0.70 or the gate target together, not the clips alone.

## Voice choice

Audition set (same chain, line `cc_hunt_hungry`): `docs/haunted-castle/audition/voice_<voice>_cc_hunt_hungry.wav` for `bm_george`, `bm_lewis`, `am_michael`, `am_adam`, `bm_fable`. Crude dry pitch (autocorrelation): lewis 100 Hz, michael 132, adam 129, fable 144, george 154. Shipped: **`bm_george`**, a British narrator voice. Reason: it is the best-rated of the low British Kokoro voices (lewis is deeper but rated lower and shows more synthesis artefacts; adam is the weakest American voice) and keeps the established "English count" character; the chain adds depth. **I cannot listen to audio in this environment; the choice rests on metrics and Kokoro's published voice grades. Human listen: owner pending.** If the owner prefers another candidate: `python tools/generate_castle_voice.py --voice bm_lewis` (or `am_michael`; add `--speed 0.9` for slower) re-renders all 22 in place.

## How verified (no engine launched)

- `python3 tools/check_audio_keys.py`: OK (65 uses, `step_stone_*` resolved in CastleAudio).
- `python3 tools/check_audio_quality.py` (not strict): OK, 106 clips; Bellwether step_tile/step_wood show as WARN only (expected: they are the dings).
- `INPUT_SYSTEM_REFERENCE=<main clone>/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py`: PASS (the known CS0649 warnings only). `tools/check_castle_scares.py`: PASS.
- Step tonality (150-600 Hz, Welch ~47 Hz bins): stone 2.0-3.1 dB / flat 0.92-0.97, gravel 1.4-7.0 dB / flat 0.77-0.97 (thresholds 12 dB / 0.35). Step peaks -14.0 dBFS, 0.58-0.67 s (stone), 0.43-0.48 s (gravel), tails under 0.3 s.
- Voices: all 22 at -24.2 LUFS, trailing silence at most 0.15 s, every peak under -3 dBFS. Durations are 0.5-2 s longer than the eSpeak set because the hall tail is kept (spatial speech reserves a pooled voice for the clip length; the pool is 12, acceptable).

## Integrator: run / bake / capture

- No bake needed (runtime C# plus audio assets; Unity imports the new `.wav.meta` files, copied from the existing CastleAudio importer settings with fresh guids). Rebuild the player.
- Listen check in the castle: walk indoors (no ding, a faint cloth-and-leather shuffle), crouch (almost silent), sprint (a little louder), cross onto the gravel outside the safe zone (soft crunch). Then trigger a hunt line (`cc_hunt_hungry`) and the three ledgers.
- `HC_RARE_LINE`/Speak logs are unchanged. If a step ever sounds missing, `MissingArt.Report("audio","step_stone_N")` would log it (it should not).

## Re-rendering voices

```
python3.12 -m venv <dir>/kokoro-venv        # Python 3.12 (3.14 cannot build spaCy yet)
<dir>/kokoro-venv/bin/pip install kokoro soundfile scipy numpy
# ffmpeg and espeak-ng on PATH; Kokoro weights (hexgrad/Kokoro-82M) and spaCy en_core_web_sm download on first run
<dir>/kokoro-venv/bin/python tools/generate_castle_voice.py                 # all 22 cc_* lines, CPU
<dir>/kokoro-venv/bin/python tools/generate_castle_voice.py cc_listen       # one line
<dir>/kokoro-venv/bin/python tools/generate_castle_voice.py --audition      # candidate voices on cc_hunt_hungry
```
Edit line texts in `tools/voice-lines.json` (`cc_*` entries), then re-render only those ids. The session venv used here was `/private/tmp/claude-501/-Users-midir/7e6a5e4e-3e3a-4250-94de-140e73836416/scratchpad/kokoro-venv` (disposable). Footsteps: `python3 tools/audio_castle_steps.py` (safe bare run). Never run `tools/audio_castle_sfx.py` with no arguments.

## Risks

- The voice is judged by metrics only; owner listen pending. Kokoro prosody can be flat on long ledger lines; if so, split them at sentence breaks (the script already pauses 0.3-0.45 s between sentences) or raise speed to 0.9.
- Footsteps at 0.11 are quiet by design; if the owner wants a little more, change only the three constants in `Investigator.cs` (clips are fixed at -14 dBFS).
- `ScareDirector` line ~106 also plays `Cue("step",...,.15)` for scares; in the castle it now plays the stone clips (no ding), unchanged otherwise.
- Voice set is 17.5 MB (three ledger clips 1.5-1.7 MB, over the older 1 MB habit).
