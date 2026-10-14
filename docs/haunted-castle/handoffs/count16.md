# Piece count16 (wave K): the r11 blind playtest findings on the Count

Branch `hc/count16` from `haunted-castle` `b428a8e`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/count16`. Model: Opus 5.5. Date 2026-10-14.
This is code only: the engine was reserved, so nothing was launched, built or rendered.
`python3 tools/check_source.py` (with `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll`) passes: 1036 rules assertions, and runtime and editor C# compile.

Files changed (castle branch only; Bellwether is identical):
- `Assets/HollowCurrent/Runtime/Presence.cs`
- `Assets/HollowCurrent/Runtime/Castle/CountClaudeProfile.cs`

Source: `docs/haunted-castle/rounds/r11/CRITIC.md` §1-3, `critic/crop_death_grab_claws.jpg`, `crop_gatehouse_visit_autocontrast.jpg`.

## 1. Kill framing (grab camera)
### When the camera engages
- A grab commits on the first touching frame in a dive (`HC_COUNT_GRAB`), at a hide entrance, or in the witnessed/near-miss open (stage 4).
- At that moment Presence's `LateUpdate` turns the player's view to his skull over `GrabCamTurnSeconds` (0.15 s, smoothstep slerp, no cut) and holds it there through the Dead presentation.
- The aim point is 30 % of the way from the skull toward the claws, so the claws fill the lower frame.
- Logs: `HC_COUNT_GRABCAM begin kind=chase|hide|hide-open` and `HC_COUNT_GRABCAM end reason=released|stagger|blocked|phase X`.

### Kill presentation
- He is eased along the line of sight so the nearer claw sits 0.8 m from the lens. He never comes closer than `CameraClearance` (0.6 m) for any joint.
- **GrabDrop comes down from above the lens.** His feet start `GrabDropAboveEye` (0.35 m) above her eye and he falls to his stand-off in 0.35 s with an ease-in, while the camera follows his skull down.
  - Log: `HC_COUNT_GRAB from=above dy=<m> fall=0.35 s`.

### Proof in the log
- At the commit frame, +0.3 s and +1.0 s: `HC_COUNT_GRABCAM t= kind= mode= skull_px=(x,y) skull_in_centre60=True|False claw_px= claw_ok=True|False eyeToSkull= headAboveEye=`.
  - All values come from the camera projection, scaled to a 2560 × 1440 frame.
  - `claw_px` is the projected width of a 0.3 m claw (`ClawWidth`) at each hand bone, taking the larger hand.
- The kill frame adds one more `HC_COUNT_GRABCAM` line.

### Hide kills
- The door is torn off: that wardrobe's door, vent and louvre panels are hidden until the next phase change, so the kill is not behind a door.
- Log: `HC_COUNT_GRAB door=torn hide= panels=`.

## 2. Sword hits
- On a hit he recoils 1.2 m away from her over 0.25 s. The recoil is collision-checked and stops at walls.
- Until 1 s after the stagger, no joint (wing end markers included) comes within 0.6 m + 0.15 m skin of the camera; he is pushed away if one would.
- Log: `HC_COUNT_RECOIL closest= pushed=`.

## 3. Stalls (hostile Return hanging on wall tops)
- **Door blocking the step:** he waits at most 1 s (`DoorWaitSeconds`, was 2 s).
- **Wall blocking the step (or a longer door wait):** he re-routes at once.
  - If the new route is blocked too, he lands and crawls the NavMesh to the same goal for 4 s (Mode 7, hover 0, 2.4 m/s).
  - If even that has no route, he lands and prowls.
  - The old 3 s "stop" is gone.
- **Airborne leg with no progress:** less than 0.1 m moved in 0.5 s counts as blocked, with the same fix order.
- Logs: `HC_COUNT_LEG blocked=door|wall fix=wait|replan|crawl|land` and `HC_COUNT_LEG stall=0.5 moved= fix=replan|crawl|land state= pos=`.

## 4. Gatehouse visit is visible
The landing spot is chosen by line of sight from her eye.
- **Candidates:** eight points along the gate-wall top around F3's `GatehouseBattlement` (±1.5 to 6 m), three just inside the gate arch, one just outside it, and the courtyard doorway at the gatehouse's inner wall.
- **Scoring:** each candidate scores 0-3 for the body points at 0.4, 1.3 and 2.2 m that her eye can see. The best wins, with the battlement first on ties. If none is visible, he lands just inside the gate arch.
- Ground spots use the search pose (Mode 2) and are approached by a glide from beyond; battlement spots use the perch pose and are approached from above. He faces her throughout (unchanged).
- Log: `HC_COUNT_GATEHOUSE visible=N/3 kind=battlement|arch-inside|arch-outside|courtyard-door spot= eye= candidates=[...]`.

## 5. Near-miss frequency
- The once-per-hunt near-miss roll is 0.5 (was 0.33) when he lost sight of her within the last 10 s and within 6 m of the hide she is now in. Otherwise it stays at 0.33.
- Log of every attempt: `HC_COUNT_NEARMISS attempt roll= chance= reason=lost her near the hide X m, Y s ago|base go=`.
- The unwitnessed open odds below Dread 80 (1 in 10 after the first hunt) are unchanged.

## Hooks for the integrator
1. **Optional:** in `Game.LateUpdate`, skip the Dead-phase look-at slerp when `Creature.GrabCamActive`. Both aim at the skull, so the two do not fight, but the hook makes Presence's framing exact.
2. **Bridge grab lost** (`natural2:658 → :664`, not in this piece): a grab logged in the same frame as `FinishAtVan` should win. The order is in `Investigation`/`Game`/`CastleRun`'s far-bank trigger. One option is for CastleRun to skip `FinishAtVan` while `Creature.GrabCamActive` or while `contact > 0` is exposed. Tell me if you want CastleRun to do it.
3. `GatehouseFallbackHeight`/`GatehouseHeadroom` are no longer used (the visible-spot search replaced the cart fallback).

## Risks
- `GoOnFoot` to an air goal (a perch) ends on the ground below the perch. `SetPerch` then puts him on the ledge in one step, which is a visible jump on that rare fallback.
- The drop start can pass through a low ceiling (cupboard or low room) for its first 0.1-0.2 s.

## What a windowed run must prove (exact keys)
1. **huntcam and death runs, for each grab type (8, 9, hide-open):**
   - `HC_COUNT_GRABCAM t=0.00`, `t≥0.30` and the kill-frame line all show `skull_in_centre60=True` and `claw_ok=True` (`claw_px` ≥ 200).
   - Mode 8 kills show `HC_COUNT_GRAB from=above dy=` > 0 and a `headAboveEye` that starts positive.
   - The screenshots at the grab frame and +0.3 s show the skull and claws.
2. **kill run sword hits:** after each `HC_COUNT_STAGGER`, either no `HC_COUNT_RECOIL` or one with `pushed=` > 0. The hit screenshots show no membrane over the lens.
3. **Return and natural runs:** no `HC_COUNT_LEG blocked=... fix=stop` (the key no longer exists). Every `stall=` line is followed by movement. The hostile Return closes to ≤ 3 m in at least 3 of 4 runs.
4. **gatehouse visit (fear run):** `HC_COUNT_GATEHOUSE visible=` ≥ 1/3, and the capture shows him.
5. **natural runs:** at least one `HC_COUNT_NEARMISS attempt` per hunt where she hides with the torch off, with `chance=0.50 reason=lost her near the hide` when she hid right after being seen.
