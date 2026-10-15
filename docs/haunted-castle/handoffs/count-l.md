# Piece count-l (wave L): the r12 blind playtest findings on the Count

Branch `hc/count-l` from `haunted-castle` `af96343` (contains count18 `2566d85` and the HUD-hide `004443e`).
Worktree `/Users/midir/hollow-current-haunted-castle/.wt/count-l`. Model: Opus 5.5. Date 2026-10-14. This is code only: nothing was launched.

Static checks, all passing:
- `python3 tools/check_source.py` (with `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll`): PASS 1071 rules assertions; runtime and editor C# compile.
- `python3 tools/check_castle_scares.py`: PASS.
- `python3 tools/bellwether_fuzz.py`: PASS, identical to the c4b24c1 reference over 4000 seeds × 300 steps.

Files changed: `Assets/HollowCurrent/Runtime/Presence.cs` (castle branch only) and `Assets/HollowCurrent/Runtime/Castle/CountClaudeProfile.cs`.
Not touched: Game.cs, Interface.cs, PlaytestDriver, CastleRun.cs.

## 1. Unwatched hides fail sometimes
- **Open chance by Dread.** The near-miss open chance now follows Dread: 0 below 40, 5 % at 40, 25 % at 80, 40 % at 100, linear between those points.
  - The floor is 10 % once she has survived a hunt, and 25 % in Long Night.
  - HuntRules has no such field yet. If F1 adds one, swap `OpenChanceForDread` for it.
- **Within-match learning.** The hide he last found her in or near becomes his learned hide. "Found" means any of:
  - he made a near-miss visit to it;
  - he watched her go in;
  - he grabbed her in it.

  When she is hidden in that hide in a later hunt, he checks it first: there is no attempt roll, from 2.5 s into the hunt. Its open chance is doubled (capped at 80 %).
  - At Dread 50–70 that is 2 × 10–20 % = 20–40 % per hunt in the learned hide, so 6 hunts give about 74–95 % for at least one open.
- **Logs:**
  - `HC_COUNT_NEARMISS attempt ... chance=1.00 reason=learned hide <name>`
  - `HC_COUNT_NEARMISS begin ... open= openRoll= chance= curve= learnedHide=True|False eligible= dread=`
  - `HC_COUNT_LEARNED hide=<name> reason=found her in it|watched her go in|grabbed her in it`
  - `HC_COUNT_NEARMISS open=True|False`

## 2. No kill pop
The r12 numbers were eyeToSkull 0.52 → 3.32 m on huntcam and 0.47 → 1.76 m on death. The grab now commits where the presentation needs him, so nothing moves him at the kill.
- **Open-floor grabs** commit at the chosen clip's stand-off from her (its reach + 0.6 m: 2.22 m for the drop, 2.33 m for the crawl), not at 1.05 m.
  - The Dead presentation no longer re-places a committed grab; it only turns him to face her.
  - `DiveEndsIn`, `DiveLastSecond` and `HC_COUNT_DIVE close_s` now measure to that reach.
- **Hides:**
  - A known hide is approached to its grab's stand-off point, and the grab commits within 0.5 m of it.
  - An opening near-miss or witnessed visit rears back from the slats to that point (`<tag> rear-back to=`) before it commits, so `CommitGrab` has nothing left to move.
- **Easing.** `FrameClaws` moves at most 1.2 m/s, so at most 0.18 m between the 0.15 s samples.
- **Commit-frame visibility.** If he is not visible in the commit frame (count18's frustum and occlusion test), the view cuts to him at once instead of the 0.15 s turn.
  - Log: `HC_COUNT_GRABCAM snap=True reason=not visible in the commit frame`.
  - This is the only cut. A grab that is already on screen still turns over 0.15 s.

## 3. Grab variety
- **Dive style.** Each dive picks a style when it starts.
  - Where the ceiling over her clears the GrabDrop clip's 4.72 m fall, half the dives come in high (hover up to 2.6 m) and end in the drop grab (mode 8).
  - The rest come in low and end in the crawl grab (mode 9).
  - Log: `HC_COUNT_DIVE style=high|low reason=roll x share 0.50|no headroom (ceiling y) ceiling=`.
- **Hides** use the drop grab where its stand-off has headroom, else the crawl grab.
- Mode 6 is only used where neither clip is bound. No new F5 clip variant exists in the controller yet. When F5 lands the side-snatch / second drop, they need a Mode number and a `ChooseGrab` branch; tell me the names.

## 4. Claw measure that matches pixels
- At init every vertex weighted at least 0.5 to `LeftHand` or `RightHand` is recorded (`HC_COUNT_HANDS vertices= groups=`).
- At each grab-cam sample the skinned mesh is baked in its current pose and those vertices are projected.
  - Only points inside the frame count (frustum-clipped).
  - A hand whose sampled line-of-sight points are all blocked counts 0.
  - The width is the projected x-extent of each hand in a 2560-wide frame, and the larger hand wins.
- Log fields in `HC_COUNT_GRABCAM`: `claw_px_proj= claw_ok=(claw_px_proj>=200) hands=[L=px(Nin,clear/tested) R=...] skull_in_central60=`.
  - `skull_in_central60` also requires a clear line to the skull.
- The old `claw_px` and `skull_in_centre60` keys are gone.

## 5. No purple at the kill
- While the grab camera runs, his renderers move to layer 29 (`GrabLightLayer`) and the grave-ash lantern (`Investigator.Ultraviolet`) drops that layer from its culling mask.
- Both are restored when the grab camera ends.
- The main view's culling mask includes 29; only layer 30 is excluded.
- Log: `HC_COUNT_GRAB light=lantern-excluded layer=29 renderers= uvLight=True`.

## 6. Pre-dive readability
- In a dive only (Chase, during a hunt), 2.5–9 m from her, the cold shoulder rim rises from 0.35 to 1.1 so he reads against the window light.
- It never changes outside a dive, and a dive only happens once he has seen her, so it is not a hunt-start tell.
- Log: `HC_COUNT_RIM scale=1.10` (and back to 0.35), twice per dive.

## 7. HC_TORCH_FAIL (integrator)
- The Count side already exposes what an occasional or decoy cue needs:
  - `Presence.DiveLastSecond` (event, once per dive, now 1 s before the stand-off commit);
  - `Presence.DiveEndsIn` (seconds, +∞ when not diving);
  - `Presence.Diving`;
  - `Presence.GrabCamActive`.
- For a cue that fires on only some dives and also at random hunt moments, Game.cs can roll on `DiveLastSecond` (for example 1 in 3) and fire `Scares.Blackout(1)` on its own timer while `Case.HuntActive`.
- Nothing more is needed from Presence. Tell me if you want the roll inside Presence instead (for example a `DiveCue` event that already includes decoys).

## Hooks for the integrator
- None required.
- Optional: `HC_TORCH_FAIL` rework in `Game.cs` (section 7).
- Note that the grab range is now the clip stand-off (2.2–2.3 m) rather than 1.05 m, so hostile-Return and bridge grabs happen earlier. The driver's invulnerability counting is unchanged.

## What a windowed run must prove (exact keys)
1. **Siege-like run held at Dread 50–70, ≥ 6 hidden hunts in one hide:**
   - `HC_COUNT_LEARNED hide=` after the first visit.
   - `attempt ... reason=learned hide` in later hunts.
   - At least one `HC_COUNT_NEARMISS open=True` or `HC_COUNT_GRAB ... hideEntrance=True`.
2. **Huntcam, death and natural runs**, every grab:
   - Consecutive `HC_COUNT_GRABCAM` lines differ in `eyeToSkull` by ≤ 0.5.
   - The first line is `visible=True`, or is preceded by `snap=True`.
   - At t≥0.30: `skull_in_central60=True` and `claw_px_proj` ≥ 200, agreeing with the frame pixels.
3. **Grab modes:** the `HC_COUNT_DIVE style=` and `HC_COUNT_GRAB mode=` counts show mode 8 on at least 30 % of grabs whose dive logged headroom.
4. **Death / huntcam frames:** no purple cast on him at or after the commit (`light=lantern-excluded` logged).
5. **huntcam frames at 4–7 m:** he reads as a figure (`HC_COUNT_RIM scale=1.10` during the dive).
