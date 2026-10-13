# Bellwether House — complete

Status: **complete for the agreed single-map implementation scope** (2026-10-13).
The owner approved merging this completed work into `main` on 2026-10-13. Development and validation were completed on `fix/cabinet-dread-immersion`.

The owner's final playtest feedback is implemented:

- All three hiding cupboards have real narrow slats, solid door panels, a fixed eye height and an outward-facing entry pose. Looking around is limited; the field kit is lowered. A small, dim local visibility fill lets the player read nearby movement without lighting the whole room or changing the ghost's sight/sound rules.
- Ordinary full apparitions are spaced 105–165 seconds apart, with a shared minimum 90-second gap protecting scripted basement flashes too. The old first-evidence jump scare is a subtle unresolved cue. Hidden players, recovery and the approach to a hunt are protected from competing apparition spam.
- Quiet intervals use low-volume footsteps, taps, radio noise, breathing, a brief light tremor, or silence. The nine-second hunt warning and escape/hiding counterplay remain.
- Environmental play-by-play cards are suppressed. Actual radio/dispatch text, brief evidence confirmations and actionable errors remain. Evidence cards last 2.2 seconds; other allowed cards last five seconds, with a smaller card footprint.

Validation completed:

- 123 deterministic assertions passed, including a 15-minute stress simulation of the shared scare budget and narration-card filtering.
- Runtime code compiled against installed macOS Mono player and editor assemblies; editor code compiled.
- A new four-map, 124-probe, nine-reflection lighting bake passed its receipt/dependency validation.
- Unity build-time physics checks passed for all three clear eye-height viewing slits, solid lower doors, navigable hiding exits and twelve navigation routes.
- Native universal macOS build succeeded using the Null graphics device for the build step.
- Updated app installed and its strict code signature verified, without launching the game.

The game was deliberately not reopened, as requested. A new rendered playthrough and subjective scare-pacing review were therefore not performed after these final changes. Earlier runtime evidence does not substitute for that check. Human visual acceptance can happen whenever the owner elects to play again; it does not authorize an automatic relaunch.

The public release replaces the private prototype's restricted Apple System Voice recordings with reproducible eSpeak NG dialogue. The original private work and recordings remain preserved separately.
