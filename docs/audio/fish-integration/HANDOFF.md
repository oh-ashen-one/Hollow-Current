# Hollow Current — Fish Audio integration handoff

Status: implementation and bounded automated verification complete; owner listening/playtest acceptance is pending by design. Branch: `fish-audio-integration`. Do not merge to main without separate owner approval.

## Playable deliverables

- App: `/Users/midir/Documents/Codex/2026-10-22/hey-buddy-pull-up-the-hollow/outputs/Hollow Current.app`
- ZIP: `/Users/midir/Documents/Codex/2026-10-22/hey-buddy-pull-up-the-hollow/outputs/Hollow-Current-Fish-Audio-macOS.zip`
- Both Bellwether and Haunted Castle are in the title-screen map picker. The app is closed, ready for the owner to launch.
- Built with Unity 6000.3.20f1 using `NativeBuild.BuildPreparedMacAll`; both lighting receipts validated. The app copy is byte-identical to the tested build. The ZIP integrity test passed.

## Changes and voice direction

All 28 scripted dialogue lines now use offline Fish Audio Drama 3: 17 Count lines, nine dispatch lines, and two Bellwether ghost lines. Eight discrete vocal-effect targets plus the legacy breath alias bring the total to 37 resource WAVs. Original resource IDs and existing Unity GUIDs are preserved; Castle has a new presence-breath override so the house and Count retain separate identities.

The Count uses an original low British voice, controlled for stalking/lore and more forceful during pursuit. Bellwether uses a protective female dispatch operator and a fragile, grief-stricken house presence. The investigator has a separate frightened breathing identity. Existing hunt-line and hiding-breath variants remain available. These are provisional creative selections: no audio-listening-capable tool was available, so do not claim an ear review or owner approval. Scripts, independent ASR, acoustic checks, and native output evidence support technical acceptance only.

The new presence breaths initially overlapped the existing 3.6-second cadence. Live playback exposed that issue. Final house presence is 2.358 seconds and Count presence is 2.9 seconds, with release fades and a duration guard allowing for pitch variation. The player loop remains about six seconds and is seam-crossfaded. No new gameplay voice API calls, subscriptions, accounts, or backend were added.

Ambience, music beds, footsteps, doors and nonvocal scares are unchanged. The dying vocal is replaced; the original sunrise music after 4.45 seconds is sample-identical, with a short transition before it. Candle whisper bed remains the explicitly excluded ambient layer. Castle EVP, phantom and hiding textures stay wordless. Count speech placement, 14 m range, wall filtering, per-hunt/off-hunt gates, and Bellwether radio/spatial behavior are preserved.

## Verification

| Check | Result |
| --- | --- |
| Runtime/player/editor C# compilation | PASS |
| Deterministic gameplay assertions | 1,615 PASS |
| Fish coverage, provenance, content, format, GUID and cadence checks | 259 PASS |
| Audio keys / audio quality / static voice routing / prepared scene checks | PASS |
| Bellwether full investigation, natural hunt, EVP, ending, quiet hiding, death and retry | PASS, exit 0 |
| Castle escape | PASS, won, 0 blockers, exit 0 |
| Castle fear/hiding stress route | PASS, won, 0 blockers, exit 0 |
| Castle fight ending | PASS, won and Count slain, 0 blockers, exit 0 |
| Offline operation | All four final runs used process-level network denial |
| Decoded vocal metadata | Every observed clip matches the installed WAV's sample count, rate and channels |
| Castle speech routing | Five observed Count lines, all from his head; one exercised wall occlusion at 13.8 m |
| Castle EVP | Three trumpet textures observed, all wordless |
| Listener output | Nonzero in all runs; zero clipped samples; peaks 0.1599 / 0.1597 / 0.1511 / 0.1038 |
| Credential scan | 153 app files checked for UTF-8 and UTF-16 key values; none found |
| User preferences | Both preference domains restored exactly to their pre-test values |

The first native attempt uncovered a verifier startup race: a fixed one-second delay could call StartCase before loading finished. NativeVerification now waits on the real loading state with a bounded deadline. Both final builds and the complete rerun passed. No gameplay rule was relaxed to pass this test.

The passive `-hc-audio-proof <folder>` instrumentation is inactive during normal play. Its isPlaying observations include muted loop sources; they are not claims that every observed source was audible. Bellwether's route separately verified breathing and speech ducking. Castle's stress driver deliberately forces scenarios and uses invulnerability; it recorded 41 blocked-death frames in the fear run. This is behavioral/audio coverage, not a natural-survival or difficulty verdict. All logs and native receipts are in `validation/`.

## Known limitations

- Final Bellwether's strict performance gate was false: eight engine-cost frames exceeded 16.7 ms, max 32.5 ms, while engine-cost p99 was 11.2 ms. Runs used shared capture slots under other GPU work; there is no isolated 60 fps certification. Prior project handoffs also reported this strict gate failing.
- Existing Bellwether tile/wood tonal-footstep warnings and the ui_paper trailing-silence warning remain out of scope.
- Actual headphone/speaker listening, scariness, and owner gameplay acceptance remain human judgments. Digital listener output does not verify the owner's physical audio-device mix.
- Scripted test teardown can interrupt ending audio as it exits/retries; normal gameplay does not use that teardown. The complete final files and preserved music tail were checked offline.

## Cost, provenance and continuation

The last recorded account delta for NEW goal requests was about $0.05635; the conservative per-request reservation total is $0.64155, below the $5 cap. Account deltas may post late or include other activity; `usage.json` provides the request ledger. No additional generation was needed during native testing.

Original references are retained in `docs/audio/fish-audition-2026-10-22/references/`. Current prompts, raw masters, model parameters, request hashes, ASR checks and installed hashes are under this folder. `tools/fish_audio_integrate.py` reprocesses cached takes without paid generation; `--generate` is required for new requests. The private key is outside the repository at `~/.claude/secrets/fish-audio.key` and is never included in the app. Legacy eSpeak/Kokoro tools must not overwrite this Fish set.

The unrelated `hc/tripo-fixes` branch was not merged. Its removal of automatic arrival/kit radio events is a separate event-policy change; retain that policy if integrating that branch later without throwing away these audio assets.

The owner cleared the initial desktop-dialog concern. Live rendered work used `~/.cache/gpu-slot`, which the active Roblox/Godot sessions were using; historical coordinator paths were not used to bypass their reservations. All task-owned engines ended normally and slots were released. The temporary task checkout's regenerable caches can be removed after delivery; all required source/artifacts are pushed and the standalone app is independent of them.

## Five-minute owner check

1. Listen to the included Count, ghost EVP and breathing previews at normal volume; decide whether their identities and restraint are frightening enough.
2. Launch the delivered app, choose Bellwether, collect the kit and call the radio. Check speech clarity and level against the room.
3. Enter a hiding place and listen through two breathing loops; check clicks, repetition and whether breathing masks outside cues.
4. Switch to Castle and listen through the first encounter. The Count should have a location; speech should become muffled behind walls rather than sound like a narrator.
5. Note any take that sounds theatrical, insufficiently threatening, too loud/quiet, or cut off. The automated fight-ending route passed; a full manual fight can follow this quick listen.
