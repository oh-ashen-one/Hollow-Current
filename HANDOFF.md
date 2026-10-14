# HANDOFF: piece F2 "count" (hc/count11), wave F

Branch `hc/count11`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/count11`. Model: Opus 5.5. Date 2026-10-13.
Contains `origin/haunted-castle` at `21dffb5` (hc/dread, hc/audio2, hc/scares, F0 wiring) merged in, no rebase.
Files changed by F2 (only these):
- `/Users/midir/hollow-current-haunted-castle/.wt/count11/Assets/HollowCurrent/Runtime/Presence.cs` (castle branch; the shared lines only add `count!=null` guards, Split Grin paths are identical)
- `/Users/midir/hollow-current-haunted-castle/.wt/count11/Assets/HollowCurrent/Runtime/Castle/CountClaudeProfile.cs`
- `/Users/midir/hollow-current-haunted-castle/.wt/count11/Assets/HollowCurrent/Runtime/Castle/CastleRun.cs`

## Verified (how)
- `python3 tools/check_source.py` (with `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll`): PASS 483 rules assertions; runtime and editor C# compile against the player and editor assemblies.
- Unity not launched. Nothing rendered, nothing played. Every behaviour below is compiled only; the `HC_COUNT_*` lines are there so the integrator's playthrough logs can confirm it.
- F1 shim: written, then deleted after hc/dread landed. The code calls `Investigation.HuntActive / NoTellHunt / Dread / AddDread / Rules` directly. No shim remains.

## What was built (FEAR_PLAN items) and the log line that shows it
- **4 hostile return (runtime).** Castle branch hunts while `Case.HuntActive` (synced on every phase change and every frame). Includes `Hear`, `WitnessHide`, the grab (`Die` after `huntClock > GrabGraceSeconds`), strike, flames, wings. A return hunt starts on the perch nearest the drawbridge with 0.4 s perch time and no perch tell. He flies at `GlideStraightSpeed` everywhere he can fly and hears her again every 3 s. On the bridge: `GroundUnder` treats drawbridge colliders as ground, and `PlanRoute` reaches off-mesh goals through the nearest NavMesh point. Log: `HC_HUNT_SPAWN hostileReturn=True`.
- **9 perch AI between hunts.** He is active in Survey through Return, but not in critic captures. He stays visible on a perch at least 8 m from her and never on her floor within 6 m. The perch is chosen outside her view cone (more than 80 deg off the camera forward, or occluded). He moves only after the camera has not seen him for 0.5 s, every 6-14 s (Long Night rules 2-5 s), or when she gets too close. Nothing he does between hunts can kill. Flames keep Floater's automatic target. Logs: `HC_COUNT_PERCH moved/landed/leaving hunt`, `HC_COUNT_PERCH_CHOICE weights=[...]`.
- **10 cupboard near-miss.** Once per hunt, 4 s or more into the hunt, if she is hidden with the torch off and he did not see her hide, he rolls 0.33. On a hit: he flies to a point 2.2 m in front of the door, crawls in Mode 7 to 0.5 m, holds 3 s (Beat `near-miss`: CastleRun plays `door_rattle` at the door and `cc_hunt_smell` at his head), then leaves for the perch farthest from the door. The hold has no kill path, and a failed roll is round 10 behaviour, so the scripted escape run survives either way. If he witnessed the hide, the old grab runs. Logs: `HC_COUNT_NEARMISS roll/begin/landed/hold doorDistance=/leave`.
- **11 gatehouse visit.** Once per run, after `Case.Survived`, when she has been 40 s continuously in `InVan`. He drops onto `World.Castle.GatehouseBattlement` (falls back to 3.5 m over the cart, kept under the ceiling, hovering). He says `cc_stay`; CastleRun cues `drawbridge_chains` at 0.4. He holds 4 s, longer while she stays inside, until she has looked at him for 1.2 s, with a 14 s cap. Then he leaves. Logs: `HC_COUNT_GATEHOUSE begin/landed/leaving/gone`.
- **13 voices** at his head: `cc_hunt_hungry` on BeginSearch, `cc_hunt_run` on BeginChase, `cc_hunt_found` when sight is regained within 6 m. Each line plays at most once per 8 s, with at least 2 s between lines. Log: `HC_COUNT_VOICE`.
- **14 learning.** `lastSeen` persists across hunts. The next hunt's first glide goes there (Beat `learned`, `HC_COUNT_LEARNED`). CastleRun counts her seconds per floor (`HC_COUNT_FLOORS` every 30 s). A perch that looks down on her favourite floor gets weight 1 + 2.5 x share.
- **16 rim light.** Off in hunts, half between hunts, full when staged or in the capture choreography (so captures are unchanged). `public bool Diving` (flight Chase) is the name FearLink looks up. Log: `HC_COUNT_RIM`.
- **17 grab variants.** Profile `ModeGrabDrop=8`, `ModeGrabCrawl=9`. GrabDrop when his decaying height above her is at least 2 m. GrabCrawl when he is on foot (Mode 7). Either one is used only when the controller has a `GrabDrop` / `GrabCrawl` clip or state; otherwise Mode 6. Stand-off is `StandOffFor(mode)` (reach + CameraClearance). Logs: `HC_COUNT_GRAB mode= why=`, `HC_COUNT_GRAB_STATE`, `HC_COUNT_GRAB_PRESENTATION`.
- **18 gamble.** Public `CastleRun.Sarcophagus()`: the first ledger page that has been read but not yet read aloud is spoken over the sarcophagus. A `Game.Say` card names its room, the call adds 25 Dread, and in Survey/Disturbance/Recovery a 1-in-3 roll calls `Case.BeginWarning()`. It is called today from `NoteRead("Sarcophagus")` (the F3 interactable shows its note), and has a 1 s cooldown so a double route is harmless. Log: `HC_COUNT_GAMBLE`.
- **Integrator extras.** Dread +`Rules.DreadSeen` the first time in a hunt she sees him within SightRange (`HC_COUNT_DREAD seen`). +`Rules.DreadGrabSurvived` when the near-miss ends or a dive that came within 1.6 m ends without a kill (`HC_COUNT_DREAD grab survived`). The bell card takes its seconds from `Rules.WarningSeconds`; Long Night gets "He is already coming." `TakeRelic` skips the blade reveal when `Game.MovedRelic==0`. Public `CastleRun.MoveRelicToAlternate(int)` uses `World.Castle.RelicAltPositions`. Presence exposes `event Action<string> Beat`, `NearMissDoor`, `HeadPosition` and `Airborne`.

## Not done / risks
- No Unity run. Things to check in the playthrough logs:
  - whether drawbridge colliders are children of `CastleDrawbridge`; if not, the bridge ground falls back to the nearest NavMesh point;
  - whether the near-miss landing point snaps through a wall in tight corridors;
  - whether the gate-wall battlement can be seen from the courtyard.
- F5 clip convention assumed: the GrabDrop/GrabCrawl origin stands at the stand-off on her floor and the clip carries the fall or lunge. `GrabDropReach` and `GrabCrawlReach` default to 1.39 m; set them from the receipt (`grab_drop_reach_m`, `grab_crawl_reach_m`).
- `HearthboundActor.ModeFrames` has 8 slots, so modes 8 and 9 are not counted in frame stats (harmless; integrator file).

## Requests
- Integrator (F0):
  1. `Interactable.cs` `case InteractKind.Sarcophagus:` should call `if(game.Castle)game.Castle.Sarcophagus();` (keeping the note is fine).
  2. Add a public immediate no-tell trigger to Investigation (for example `BeginNoTellHunt()` calling the private `StartHunt(true)`). The gamble uses `BeginWarning()` until it exists (Long Night already starts the hunt at once).
  3. The death penalty can call `Castle.MoveRelicToAlternate(i)` instead of its own copy.
  4. Merge, rebuild, run both scripted playthroughs and grep `HC_COUNT_`.
- F3: nothing blocking. A battlement point visible from the courtyard (over the gatehouse roof) would let the gatehouse beat be seen.
- F5: map clips to Animator Mode 8/9 with names containing `GrabDrop` / `GrabCrawl`, and send the two reach values.
