# Piece count13 (wave H): the Count's fixes for the r10b static critique

Branch `hc/count13` from `haunted-castle` `5e86a3f`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/count13`. Model: Opus 5.5. Date 2026-10-14.
The integrator had the engine, so this is code only: nothing was launched, built or rendered.
`python3 tools/check_source.py` (with `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll`) passes: 939 rules assertions, and runtime and editor C# compile.
Source: `docs/haunted-castle/rounds/r10/CRITIC_STATIC_B.md` (N2, N3, N8, N9, the hide guarantee in #16/#17).

Files changed:
- `Assets/HollowCurrent/Runtime/Presence.cs`: castle branch only.
- `Assets/HollowCurrent/Runtime/Castle/CastleRun.cs`
- `Assets/HollowCurrent/Runtime/Castle/CountClaudeProfile.cs`

Bellwether paths are unchanged: every new line sits behind `count != null` or in castle-only classes.

## Changes
### N2: a hide in front of a diving Count is punished fast
- A hide he witnesses while in Chase no longer starts the fly/land/crawl/pause sequence.
  - He keeps diving: `CountChase` goes for `knownHide`'s entrance (the ~0.3 s entrance grab).
  - Log: `HC_COUNT_WITNESSED chase=True reason=already diving, entrance grab`, then `HC_COUNT_GRAB ... hideEntrance=True`.
- From a perch, in flight or on foot, the telegraphed sequence still runs (`HC_COUNT_WITNESSED begin`).
- No cancel without a fallback:
  - A watched hide with no reachable landing: `abort=route reason=... diving at the entrance instead`, then a dive.
  - A watched hide not matched to a cupboard: `abort=spot reason=... diving at the hide point`, then a dive.
  - A near-miss with no route: `abort=route reason=... prowling where he is`.
- A dive with no NavMesh route for three plans in a row (~1 s) lands and prowls instead of hovering: `HC_COUNT_ROUTE abort=dive reason=no route`.
- A watched hide outside `World.HidingPlaces` uses the player's own `HideExit`. Three seconds at the goal without a grab ends with `HC_COUNT_WITNESSED abort=stuck reason=` and a prowl, so there is no soft-lock.
- A hide with no door panel (no `<name> / lower door`) gets no `door_rattle`; the sequence still runs.
  - Log: `HC_COUNT_WITNESSED door=none` or `HC_COUNT_NEARMISS door=none reason=`.
  - `Presence.NearMissHasDoor` is read by CastleRun.

### Hides are never a guarantee
- Near-miss open odds:
  - Dread >= 80 or Long Night: 1 in 4 (unchanged).
  - Below 80, after she has survived a hunt: 1 in 10 (`NearMissOpenChanceLow`).
  - First hunt below 80: no open. The log says `eligible=False reason=first hunt, below 80`.
- The begin line logs `open= openRoll= chance= eligible= dread= longNight=`.
- A watched hide opens at any Dread (confirmed). Its begin line says `open=True reason=watched (any Dread)`.

### N3: no hunt state in the wings or claws
- Between hunts, a due perch move made while unseen is a real flight one time in three (`AudibleReperchChance`). The flight goes to a perch out of her sight.
  - Log: `HC_COUNT_PERCH flight unseen=True audible=True`.
  - The other moves stay silent. If no hidden perch or route exists: `flight unseen rejected reason=..., moving silently`.
- Between-hunt flights are always flown to the end (no mid-flight teleport). This includes the fly-off after a hunt, so wings fade with his real landing.
- Every Travel leg draws its loudness the same way in hunts and between them: a silent glide, soft (0.5) or full flap, one third each (`HC_COUNT_WINGS leg= loudness= hunt=`). A dive is always full.
  - CastleRun: wing loop = 0.6 x `Presence.WingLoudness`, rising over 0.4 s and fading over 1.5 s.
- `Presence.ClawStepVolume` is one distance curve in both states: 0.7 at 2 m down to 0.12 at 16 m. It needs the integrator hook below.

### N8: memory ages out
- `lastSeen` keeps a timestamp (`Case.Elapsed`) and a kind. Heard places expire after 60 s, seen ones (including a watched hide) after 120 s, and Long Night multiplies both by 1.5.
- At hunt start and at the end of the return swoop he logs `HC_COUNT_MEMORY stale=True|False|none age= limit= kind= when=`.
  - Stale: the memory is dropped (`reason=too old, opening from the perch`) and the hunt opens from the perch with `HearPlayerRoughly`.
  - Fresh: `HC_COUNT_LEARNED`.

### N9: no swoop through walls
- A blocked swoop leg is re-routed through the nearest clear detour. Candidates are the midpoint raised 1-4 m, then 1.5-4.5 m to either side.
  - Log: `HC_COUNT_RETURN_SWOOP begin ... legN=clear|detour via (x,y,z)`.
- If there is no clear detour, or the start is inside geometry, the swoop is skipped (`HC_COUNT_RETURN_SWOOP skipped reason=leg N ... blocked, no clear detour`). He then starts from the drawbridge-side perch.
- `cc_dispatch` and Beat `return-swoop` now fire before the flight check, so the hostile return is announced either way.

## Hooks for the integrator
1. **Claws (N3):** in `Soundscape.cs`, change the claw cue `Cue("claw_steps_"+clawIndex,at,hunting?.8f:.05f)` to `Cue("claw_steps_"+clawIndex,at,game.Creature.ClawStepVolume)`.
2. **Swoop card (N9, Game-side half):** `Say(SealedMessage)` overwrites the `return-swoop` card in the same frame. Show the card on a ~1 s delay or append it (`Game.cs`).
3. **Driver:** `CastleRun.WingVolume` still reports the loop volume for the channel log; it now varies per leg (0 / 0.3 / 0.6).

## What a windowed run must prove (exact keys)
1. **Watched hide during a dive:**
   - `HC_COUNT_DIVE`, then `HC_COUNT_WITNESSED hide at`, then `HC_COUNT_WITNESSED chase=True`, then `HC_COUNT_GRAB ... hideEntrance=True witnessed=True` within 1.5 s of `hide at`.
   - No `HC_COUNT_WITNESSED begin` in between.
2. **Watched hide from a perch with the torch off:** `HC_COUNT_WITNESSED begin ... open=True reason=watched`, then `hold`, then the rattle (`HC_COUNT_BEAT hide-open`), then `HC_COUNT_GRAB hideEntrance=True torch=False`.
3. **Unwitnessed hide below Dread 80 after a survived hunt:** `HC_COUNT_NEARMISS begin ... chance=0.10 eligible=True`. Over about 20 near-misses, at least one `HC_COUNT_NEARMISS open=True`. The first hunt logs `eligible=False reason=first hunt, below 80`.
4. **Between hunts:**
   - About a third of `HC_COUNT_PERCH` moves made unseen are `flight unseen=True audible=True`, each with an `HC_COUNT_WINGS leg=perch hunt=False` line.
   - The `HC_COUNT_WINGS ... loudness` values have the same mix with `hunt=True` and `hunt=False`.
   - The channel log's wing volume never drops from above 0.3 to 0 in under 1 s.
5. **Claw steps (after hook 1):** volumes depend on distance only; at equal distance a step in a hunt and between hunts has equal volume.
6. **Memory:** a hunt that starts more than 60 s after the last heard or 120 s after the last seen position logs `HC_COUNT_MEMORY stale=True ... reason=too old`, then `HC_COUNT_HEARD guess=`. A younger one logs `stale=False`, then `HC_COUNT_LEARNED`.
7. **Swoop:**
   - Every `HC_COUNT_RETURN_SWOOP begin` line shows each leg as `clear` or `detour via`, and he never visibly passes through a wall.
   - Or the run shows `HC_COUNT_RETURN_SWOOP skipped reason=` with `cc_dispatch` still heard.
8. **No soft-lock:** no hunt ends with him stationary at a hide (any `abort=stuck` must be followed by `HC_COUNT_AI state=Land`).
9. **Bellwether:** unchanged (RulesTests green, no `HC_COUNT_` lines).
