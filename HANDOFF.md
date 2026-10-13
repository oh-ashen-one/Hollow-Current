# HANDOFF — piece 4b, Count Claude behaviour + animation (round 2)

Branch `hc/count2`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/count2`, based on `haunted-castle` @ 140b47a.
Model: Claude Opus 5.5. Unity was not launched (builder rule). Blender ran headless only.

## Root cause of the "A-pose in every frame" (critic r1, S5/S6/H01–H10)

The Animator was playing. Evidence: the S6 capture (Mode 1) shows the lower wing segments kicked out sideways
at waist height, which is exactly frame 0 of the old Glide clip rendered in Blender, and is not the rest pose
(rest wings hang straight). The controller, `Mode` parameter, prefab assignment and culling were all correct.
The faults were in the authored data (`tools/count_prepare.py`, round 1):

1. **The clips were near-rest standing poses.** Perch was rest + 2–5 deg; Glide/Search only swung limbs a few
   degrees back (invisible from the front) with a 7 cm hip lift, and Presence kept him on the NavMesh floor. Every
   mode therefore read as a standing A-pose.
2. **Wing hinge at the waist.** `WingTip` pivoted at z 0.95 m; any wing roll swung the lower membrane horizontally
   ("snapped spider legs").
3. **Auto-rig weights.** The wing roots and the collar were weighted to `LeftArm`/`RightArm` (0.8), so any arm
   motion tore the wing root.
4. Dive held its last frame (the wing-wrap) for the whole chase, and H04–H10 froze because the grab killed the
   investigator at ~4 s and the death presentation put him 0.58 m from the camera.

## What changed (2 commits + this file)

- `9e03435` `tools/count_prepare.py`, `Art/Characters/CountClaude/Native/*` (regenerated), `Editor/CountClaudeImport.cs`
  - Each wing = root at the shoulder blade + five-bone fan (`WingFanL0..4`, `R`) on one pivot, membrane weighted by
    angle around it; zero-weight `WingFan*End` markers. Wings fold behind (Perch) and spread as one sheet (Glide).
  - Arm weights behind the back → spine; lowest finger tips included in the wing.
  - Seven clips (Mode = index): 0 Perch (gargoyle crouch, two-bone IK plants feet and claws), 1 Glide (pitched,
    spread, beating), 2 Search (landed prowl, head scanning), 3 Dive (looping stoop, claws forward), 4 Stagger,
    5 Death (topples backward), **6 Grab (wing-wrap, new)**. Airborne clips are shifted so their lowest point is
    0.10 m above the origin (hover 0 = skimming / walking a service door).
  - Receipt `Native/CountClaude.json` now records per-clip span/top/lowest/forward reach (grab reach 0.79 m,
    Glide span 3.24 m vs shoulder width 0.43 m, Perch span 2.34 m, Perch top 1.37 m, head top 2.15 m).
  - Verified: FBX re-import in Blender reproduces the authored world joint positions exactly for every clip.
- `2c0f614` `Runtime/Presence.cs`, `Runtime/HearthboundActor.cs`, `Runtime/Castle/CountClaudeProfile.cs`
  - Presence has a Count-only branch (`count != null`); Split Grin's lines are unchanged (all edits there are
    additive and inert when the profile is `SplitGrinProfile`).
  - Behaviour per DESIGN.md: hunt starts crouched on the perch nearest the hunt origin (on the balustrade's front
    edge when one is found by raycast); sight 14 m / 80 deg with line of sight; 0.9 s tell, then Dive chase
    (4.6 m/s straights, 3.0 m/s when open width < 4 m); lands and walks service doors (`CastleLayout.IsServiceDoor`)
    in Mode 1 at 2.4 m/s; heard / last-seen spots searched on foot (Mode 2); idle perch → next perch in Glide
    (Mode 1). Grab at 1.05 m plays Mode 6. `Fight.Staggered` → Mode 4 on the ground; `Fight.Retreating` → nearest
    perch; tiers fly lower × faster; `Fight.Dead` → Mode 5, AI stops, body stays.
  - He flies kinematically (no NavMeshAgent): straight where a sphere cast is clear, else NavMesh corners lifted
    to a hover under the ceiling.
  - Capture hooks: `Presence.StageForCapture(perchIndex)`, `Presence.CaptureFraming(fill, belowDeg, out eye, out target)`,
    `Presence.ScaleReference(feet, yaw)` (1.8 m mannequin). With `-hc-capture` he dives to `CaptureHoldDistance` 2.5 m,
    holds 1.6 s in Dive, feints back to a perch and repeats (never grabs); the capture camera is pushed out of his
    posed bounds + 0.6 m (`HC_CAPTURE_CAMERA_PUSHED`). Death presentation stands him GrabReach + 0.6 m from the camera.
- `python3 tools/check_source.py` with `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll`:
  PASS (392 rules assertions; runtime vs player + editor assemblies; editor compiled).

## Requests for the integrator (exact steps)

1. Merge `hc/count2`. Through the slot: `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-count -- <Unity> -batchmode -nographics -quit -projectPath <clone> -executeMethod HollowCurrent.Editor.CountClaudeImport.Prepare -logFile <log>`.
   Expect `HC_CHARACTER_RIG bones=46 ...`, seven `HC_CHARACTER_CLIP` lines (Perch, Glide, Search, Dive, Stagger, Death, Grab)
   and `HC_COUNTCLAUDE_PASS ... 7 independent motions`; no `HC_COUNTCLAUDE_GRAB_REACH` warning. Commit the regenerated
   `Resources/Characters/CountClaude.prefab` and `Resources/Characters/CountClaude/*` (new `CountClaudeGrab.anim`).
   Until this runs, the committed prefab/anims are round-1 and do not match the new FBX bones.
2. `CaptureMode.cs` (yours):
   - **S5**: replace `ManifestAt(...)` + `Pose(...)` with `game.Creature.StageForCapture(0);` wait 1.5 s, then
     `game.Creature.CaptureFraming(.5f,30,out var e,out var t); Place(e,t);` wait 0.5 s, shoot. Record `e`/`t` in the note.
   - **S6** (scale + sheet): `game.Creature.ManifestAt(new Vector3(-3,0,11),180,9999,true);` (Mode 2 Search, standing on
     the floor; drop the `Pose(ModeMove,3)` override), `var man=Presence.ScaleReference(new Vector3(-1.75f,0,11),180);`
     eye `(-2.4,1.15,7.4)`, target `(-2.4,1.1,11)`; `Destroy(man)` after the shot. Expected: his head ≈ 2.15 m vs the
     mannequin's 1.80 m.
   - **H01–H10**: no code change needed (hold/feint is automatic under `-hc-capture`); suggest target `(1.5,3.0,24)` so
     the east-balcony perch at ≈ y 4.3 stays in frame.
3. Route check: run your castle validate/route check; then grep the capture log for `HC_HUNT_SPAWN ... flight=Perched perch=1`,
   `HC_COUNT_DIVE`, `HC_COUNT_FEINT`, and `HC_COUNT_STAGED perch=0 ... feet=` (feet y ≈ rail top 4.3 means the balustrade was
   found; y 3.2 means no rail collider in front and he crouches on the deck).

## Not done / known limits

- Nothing verified in Unity: hunt timing, the balustrade raycasts, NavMesh-corner flight and capture framing are untested in engine.
- Death ends reclined on his wings, not fully flat; the Perch coat hem dips ~0.15 m below the rail top.
- Mode 6 Grab extends the 0–5 contract; Split Grin never sets it.

## Evidence (scratch, not committed)

`/private/tmp/claude-501/-Users-midir/7e6a5e4e-3e3a-4250-94de-140e73836416/scratchpad/` — `sheet0a.png` (round-1 clips; Glide
frame 0 matches S6), `sheet7a.png`, `sheet8a.png`, `sheet11.png`, `close3.png` (new clips), `prev11/` (all previews + `count.blend`).
