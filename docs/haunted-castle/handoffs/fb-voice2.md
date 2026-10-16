# Piece fb-voice2: no speaker voice in the castle (owner feedback item 13)

Branch `hc/fb-voice2` from `hc/owner-feedback-1` (`d0903fa`). Model: Sonnet 5.5. Date 2026-10-15. Code and docs only: no wav touched, no engine launched.
Static checks: `INPUT_SYSTEM_REFERENCE=<main clone>/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py` PASS (only the known CS0649 warnings), `tools/check_castle_scares.py` PASS (new speech lint), `tools/check_audio_keys.py` OK (new retired-line check).

## Owner item 13
"Whatever voice is saying 'I am so very hungry' ... Nothing should be talked about as if it's a speaker throughout the entire house."

## What changed
- `Runtime/Soundscape.cs`: `Speak` now returns bool. In the castle a line is dropped (logged once per id `HC_VOICE_DROPPED id= reason=`) when it has no location, is not a `cc_`/`evp`/`whisper` id, is more than 14 m from her eyes, or has no clip. The 2D `dispatch` source is unreachable from the castle. Played castle speech uses the voice pool at .70 with min 1.2 m, logarithmic, max 14 m (`Play(..., castleSpeech: true)`; other cues keep 1.5/22), plus a play-time `Linecast` low-pass at 900 Hz when occluded, kept up by the existing 0.12 s occlusion loop (which now uses 900 Hz for castle speech slots). Logs `HC_COUNT_VOICE id= dist= occluded=`. Counters `SpeechRequests/SpeechSpatial/SpeechDropped`; `LogVoiceSummary()` prints `HC_VOICE_SUMMARY lines= spatial= dropped=` (called when the run goes terminal, Dead or Won, and on application quit). Bellwether's `Speak` path is unchanged.
- `Runtime/Game.cs`: every `Sound.Speak` is now `BellwetherSpeak(id)` (no-op in the castle). Removes castle arrival, kit, hints, contain, return, warning, ending (and the Won coroutine's ending). Cards (`Say`) stay.
- `Runtime/Presence.cs`: new `CountSpeak(id)` (all Count speech goes through it): needs his silhouette in the world; <= 14 m from her ears; the four hunt lines (`cc_hunt_found/smell/run`, `cc_stay`) only within 6 m; at most one line per hunt (`huntLines`, reset in `CountBeginHunt`); outside hunts at most one per 180 s; the first-8-s-unseen hold stays (except found/smell). Logs `HC_VOICE_GATE held|spoke`. Removed: `cc_hunt_hungry` queue (`QueueVoice`/`TickVoices`/`pendingVoice`), the `cc_dispatch` speech at the return swoop (the `return-swoop` beat and card remain), the voiceAt/cooldown bookkeeping.
- `Runtime/Castle/CastleRun.cs`: `NoteRead` no longer reads ledger pages aloud; the near-miss line goes through `count.CountSpeak("cc_hunt_smell")` so it obeys the caps.
- `Runtime/Castle/CountClaudeProfile.cs`: `VoiceMaxDistance=14`, `VoiceCloseDistance=6`, `VoiceOffHuntGap=180` (old Voice* fields kept so serialized profiles load).
- `tools/check_castle_scares.py`: lint 6 (every `Speak` outside `Game.cs` must have a location argument; `Game.cs` may only call `Speak` inside `BellwetherSpeak`; no `cc_hunt_hungry` in C#; Soundscape must carry the drop and summary logs). `tools/check_audio_keys.py`: Bellwether-only `BellwetherSpeak` keys, castle mapping reduced to evp/whisper, `RETIRED` list fails on any C# or `voice-lines.json` reference, CONTRACT no longer promises the retired clips.
- `tools/voice-lines.json`: `cc_hunt_hungry` removed.
- Docs: `docs/haunted-castle/AUDIO.md` voice table rewritten (policy, which lines exist, where spoken, caps, retired list); item 13 added to `docs/haunted-castle/rounds/r13/OWNER_FEEDBACK.md`.

## Still spoken in the castle (all spatial)
`cc_hunt_found`, `cc_hunt_smell`, `cc_hunt_run`, `cc_stay` (his head, <= 6 m, one per hunt); `cc_listen` (behind her head, once per run); `cc_bargain` (ear-trumpet reply at the trumpet spot); `cc_ledger_*` (sarcophagus gamble, 1.2 m above it). `cc_steward`, `cc_relic_*` have no C# caller (unchanged).

## Integrator: delete after merging (on top of hc/fb-audio)
Wavs and metas in `Assets/HollowCurrent/Resources/CastleAudio/`: `voice_cc_hunt_hungry`, `voice_cc_dispatch`, `voice_cc_night`, `voice_cc_kit`, `voice_cc_ending`, `voice_cc_contain`, `voice_cc_hint_signal`, `voice_cc_hint_trace`, `voice_cc_hint_voice` (each `.wav` and `.wav.meta`). Also remove the matching `cc_night`, `cc_kit`, `cc_ending`, `cc_contain`, `cc_hint_signal`, `cc_hint_trace`, `cc_hint_voice`, `cc_dispatch` entries from `tools/voice-lines.json` (I removed only `cc_hunt_hungry` to avoid conflicting with fb-audio's voice edits) and from `ASSET_LOG.md` rows if desired. `tools/audio_castle_sfx.py --check` reports them MISSING after deletion (harmless; its VOICES list can be trimmed). After the merge run `check_audio_keys.py`: it passes whether or not the wavs are still on disk.

## Proof recipe (needs a player build; I did not build)
1. Bake/build as usual, run a natural castle run and the kill/escape runs (AUTO_RUN.md).
2. In the player log: `grep -c HC_COUNT_VOICE` lines each show `dist<=14` and, for hunt ids, `dist<=6`; there must be no `HC_COUNT_VOICE` line outside a located source and at most one `HC_VOICE_GATE spoke` per hunt (`huntLines=1`).
3. `HC_VOICE_SUMMARY lines=N spatial=M dropped=K` at run end must satisfy `lines == spatial + dropped` and every dropped line has a `HC_VOICE_DROPPED id=` entry. A natural run should show `dropped=0` (nothing in the castle still asks for 2D); any dropped id names a leftover caller. `cc_hunt_hungry` must never appear.
4. No `dispatch` source exists on the castle path: `Speak` returns before touching it. `PlayedSpeech` counts only spatial lines.

## Risks
- The 6 m gate plus one-per-hunt may leave some hunts silent (intended). If the owner wants a bit more, raise `VoiceCloseDistance`/relax the per-hunt cap in `CountClaudeProfile`/`Presence.CountSpeak`.
- Unity's logarithmic rolloff does not reach zero at max distance; the hard 14 m play gate handles the "dies by 14 m" requirement, but a line already playing keeps going quietly if she runs away.
- `HC_COUNT_VOICE` is now emitted by Soundscape with the `id= dist= occluded=` format; PlaytestDriver's prefix counter still counts played lines only (gate decisions use `HC_VOICE_GATE`).
- `Presence.cs` near-miss: if the hunt already used its one line, `cc_hunt_smell` is silent; the door rattle still plays.
