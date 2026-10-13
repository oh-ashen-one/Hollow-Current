# HANDOFF: piece 5, forge/fight rules (branch `hc/forge`)

Worktree: /Users/midir/hollow-current-haunted-castle/.wt/forge. Pure C#, no UnityEngine references.

## Files
- `Assets/HollowCurrent/Core/Forge.cs` (+ `.meta`): `Relic`, `LedgerHints`, `Forge`, `Fight`.
- `Tests/RulesTests.cs`: appended `ForgeAndFightTests()` (called once just before the final PASS line in `Main`; no existing test moved).

## API
- `enum Relic { SilverBlade, HallowedHilt, Moonstone }`
- `LedgerHints.EntryByRelic` / `EntryFor(Relic)` / `TryRelicFor(string id, out Relic)`: SilverBlade=`cc_ledger_steward`, HallowedHilt=`cc_ledger_orrery`, Moonstone=`cc_ledger_portrait`.
- `Forge`: `Found(Relic)` (true on first find), `HasRelic`, `RelicCount`, `AtAltar` (settable; caller sets each frame), `CanCraft` (3 relics && AtAltar && !HasSword), `Craft()` (instant commit; false unless CanCraft), `Tick(dt)` (9 s hold, `CraftSeconds`; any frame where CanCraft is false resets `CraftProgress01` to 0; completing calls `Craft()`), `HasSword`, `CraftProgress01`, `Crafting`, events `RelicFound(Relic)` and `Crafted`, `ReadLedger(entryId)` / `HintKnown(Relic)` (journal hint unlock), `Reset()`.
- `Fight`: `Begin()`, `RegisterHit()` (bool: counted), `Tick(dt)`, `PlayerGrabbed()`, `Hits`, `Dead`, `Lost`, `Over`, `Active`, `Staggered` (2.5 s, `StaggerSeconds`), `Retreating` (20 s, `RetreatSeconds`), `Tier` (= completed retreats, 0..2), `SpeedMultiplier` (1.0 / 1.15 / 1.3), `CanBeHit` (Active, not over, not staggered, not retreating), events `HitLanded`, `Killed`, `PlayerLost`.
- Decision: hits during the 2.5 s stagger also do not count (only the first strike of a stagger/retreat cycle counts). Tier rises when the retreat ends, so the retreat itself still runs at the previous tier's speed; the AI should poll `Retreating`/`Staggered` for the perch behaviour and `SpeedMultiplier` for glide/dive speed once he returns.

## Verification
`python3 tools/check_source.py` in this worktree: rules compile and run, `PASS 174 deterministic gameplay assertions`. The second half needs the Input System assembly, which a fresh worktree lacks, so it was re-run with the main clone's assembly:

`cd /Users/midir/hollow-current-haunted-castle/.wt/forge && INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py`

Result: exit 0, `PASS runtime C# against installed macOS Mono player and editor assemblies; editor C# also compiled` (only pre-existing CS0649 warnings in ControllerReleaseBuild.cs). Unity was not launched. Not verified in a live scene.

## Requests for integrator (piece 0)
Investigation.cs has no outcome enum to extend (`Phase { ..., Won, Dead }` is shared logic and was not touched), so wire the kill ending yourself:
1. Own one `Forge` and one `Fight` per castle run (reset both with the investigation checkpoint restore; `Forge.Reset()`, `Fight.Begin()` on re-entry).
2. Each frame: `forge.AtAltar = playerInChapelAltarVolume && holdingInteract; forge.Tick(dt); fight.Tick(dt);` and drive the HUD ring from `forge.CraftProgress01`. Relic pickups call `forge.Found(relic)`; ledger entries call `forge.ReadLedger(entryId)` to unlock hints.
3. Begin the fight (`fight.Begin()`) when `forge.HasSword` and the Count engages in the great hall. A sword swing that connects while the Count is in front of the player calls `fight.RegisterHit()` (only if `fight.CanBeHit`); on true play Animator Mode 4 (Stagger). When `fight.Retreating` send him to a perch. Multiply his glide/dive speed by `fight.SpeedMultiplier`.
4. Grab: while `fight.Active`, a Count grab calls `fight.PlayerGrabbed()` and then the existing `Investigation.Die()` path (Phase must be Hunt; if the fight happens outside Phase.Hunt, force a Hunt-equivalent state or add a castle death transition in Investigation.cs).
5. Kill ending: on `fight.Killed` (or `fight.Dead`) play Mode 5 (Death), the sunrise through the rose window, then end the run as a win. Investigation has no kill outcome; either add `public void WinByKill(){ if(!Terminal) Set(Phase.Won); }` in Investigation.cs (piece 0 owns it) or call `FinishAtVan()` after forcing `Phase.Return`. Goal text for Won reads "CASE CLOSED"; a castle variant needs its own text.
6. Escape via the drawbridge remains the unchanged Investigation win path.
