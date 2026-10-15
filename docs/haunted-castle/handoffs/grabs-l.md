# Handoff: grabs-l (F5 wave L, branch `hc/grabs-l`, Sonnet 5.5, 2026-10-14)

Round 12 critic (5.0/10): "7 of 9 grabs are the same Mode 9 low crawl lunge". Three new Count grab clips in `tools/count_prepare.py`, Blender only (CPU, headless; no Unity, no `.anim`/`.meta` committed). New FBX are committed because they are the import sources.

## Modes F2 should use (existing modes unchanged)
| Mode | Animator state / FBX | Duration | Claw reach (final) | Use |
|---|---|---|---|---|
| 8 | GrabDrop (existing) | 1.6 s | 1.615 m | ceiling drop onto her while she faces him (keep >= 30 % of grabs) |
| 9 | GrabCrawl (existing) | 1.4 s | 1.729 m | low door / crouched / crawl lunge |
| 10 | **GrabSideL** | 1.2 s | 1.596 m | side snatch from the player's LEFT flank |
| 11 | **GrabSideR** | 1.2 s | 1.607 m | side snatch from the player's RIGHT flank (mirror image of 10) |
| 12 | **GrabDropSpin** | 1.9 s | 1.614 m | ceiling drop that lands BEHIND her; camera spun into his face |

Stand-off is `reach + CameraClearance (0.6)` per clip, same rule as 8/9 (`CountClaudeModes.json` carries `grab_variants[]` with name, mode, seconds, claw_reach_m, mesh_reach_m, plus `grab_side_reach_m` 1.607, `grab_spin_reach_m` 1.614). Names follow the existing GrabDrop / GrabCrawl pattern (`Count_GrabSideL` etc.). Mode count is now 13: `HearthboundActor.ModeFrames` (`new int[10]`, guarded) should be sized 13, `CountClaudeProfile` needs `ModeGrabSideL=10, ModeGrabSideR=11, ModeGrabDropSpin=12` and `ModeCount=13` (not my files).

### Suggested selection (F2)
- Headroom for the 4.74 m fall (`top_m_max` 4.735 for 12, 4.722 for 8) and the Count is above her: Mode 8 at >= 30 % (the critic's variety floor) and Mode 12 for the rest, but Mode 12 only when the angle between her view direction and the direction to the Count is <= 120 deg (she is facing away from him; the spin never exceeds 120 deg). No headroom: skip both.
- Count arriving on foot with a lateral approach (his bearing from her view direction between ~35 and ~110 deg): Mode 10 if he is on her left, 11 if on her right. If a visual check shows the claw entering from the wrong side, swap the two names; they are exact mirrors.
- Crouched / under a low lintel / crawl route: Mode 9. Mode 6 only when no clip exists.

### GrabDropSpin camera contract (runtime owns the camera)
Count origin at the stand-off behind her, facing her; the clip starts 2.2 m up and lands at t = 0.5 s (all-fours-ish crouch, head down). Spin her view toward him from 120 deg off to 0 deg between t = 0.50 s and t = 1.15 s (`grab_spin_yaw_deg` 120, `grab_spin_start_s` 0.5, `grab_spin_end_s` 1.15 in the receipt); he rears up and the skull reaches the middle of the frame by t = 1.30 s. Hold the last pose (t = 1.9 s). Side clips: camera fixed at the stand-off for the whole clip; he starts 3.4 m to the side (outside the horizontal FOV), so he enters the frame.

## Regenerate
    /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/count_prepare.py -- --only GrabSideL,GrabSideR,GrabDropSpin --proof docs/haunted-castle/rounds/r13
Idempotent; the receipt (`Native/CountClaude.json`) is merged (all eight older clips and Modes 8/9 are untouched and byte-identical in the receipt), `CountClaudeModes.json` is rewritten. `-- --diag Crawl,GrabSideL` prints per-bone shard edges and joint angles without exporting.

## Verified (editor-less, Blender + `tools/check_source.py`)
Constraints: wing stretch max 1.6 on all three (receipt `wing_stretch_max`); claw reach <= 1.615 (GrabDrop's); shard edges (edges over 3x rest length and over 15 cm) 420 / 428 / 565 against Mode 7's 522, budget 600; `check_source.py` PASS (1071 assertions, C# compiles including `CountClaudeImport`). Grab camera for the checks: 2.215 m out (1.615 + 0.6), eye 1.64 m, vFOV 72, 2560x1440, aimed at the final face; "central 60 %" = the skull centroid inside x 512-2048 and y 288-1152; claw width = projected width of the hand-weighted vertices (same measure as the earlier proofs).

| Clip | claw >= 200 px first at | skull in central 60 % first at | +0.3 s (skull px, claw px) | final (claw px) | wing stretch | shard edges | limb max deg UpLeg/Leg/Arm/ForeArm |
|---|---|---|---|---|---|---|---|
| GrabSideL | 0.03 s | 0.23 s | (804, 784) in, 298 | 474 | 1.6 | 420 | 112/98/110/56 |
| GrabSideR | 0.03 s | 0.23 s | (1762, 788) in, 307 | 485 | 1.6 | 428 | 112/98/110/55 |
| GrabDropSpin | - | 0.43 s | clip +0.3 s: skull above the frame (he is still falling and her view is turned away); camera lock (1.15 s) + 0.3 s = 1.467 s: skull (1280, 693) in, 439 | 442 | 1.6 | 565 | 95/87/98/49 |

The claw crosses the frame first (0.03 s) and the skull swings into the central 60 % at 0.23 s, so the side snatch has the requested order and both conditions hold at +0.3 s. For GrabDropSpin the +0.3 s check only holds from the camera lock, because the player is not looking at him before that (documented above); if the critic measures +0.3 s from clip start it fails by design. The side clips' lead arm reaches Arm 110 deg in mid-run (above Mode 7's 93; the shard count stays below Mode 7's, and the skinning is fine in the renders) and the final poses are Arm 96.

## Proofs (`docs/haunted-castle/rounds/r13/`, each PNG < 1 MB)
`grab_side_l_*`, `grab_side_r_*`, `grab_drop_spin_*`: `_pose` (final), `_frame0`, `_t030` (+0.3 s), `_lock_t030` (spin only), `_sheet` (six moments from the player camera), and `grab_variants_measurements.json` (every frame: skull centre, in-central-60 flag, claw px, claw / mesh reach).

## Frame-by-frame forward reach of the furthest claw from the origin (m, 30 fps, frame 0 first; `clips.<name>.claw_reach_by_frame_m` in the receipt, mesh reach and lateral claw reach are in the same place)
**GrabSideL** (37 frames, 1.20 s), claw forward reach:

    0.62 0.66 0.69 0.73 0.76 0.81 0.85 0.90 0.95 1.00 1.06 1.10 1.14 1.17 1.20 1.24 1.29 1.34 1.39 1.43 1.47 1.49 1.51 1.52 1.52 1.52 1.52 1.51 1.51 1.52 1.53 1.54 1.55 1.56 1.57 1.58 1.60

whole-mesh forward reach:

    0.67 0.68 0.70 0.75 0.77 0.81 0.85 0.90 0.95 1.00 1.06 1.10 1.14 1.17 1.20 1.24 1.29 1.34 1.39 1.43 1.47 1.49 1.51 1.52 1.52 1.52 1.52 1.51 1.51 1.52 1.53 1.54 1.55 1.56 1.57 1.58 1.60

max |x| of the claws:

    3.19 2.89 2.59 2.29 1.99 1.71 1.42 1.17 0.95 0.76 0.60 0.46 0.34 0.24 0.30 0.37 0.38 0.37 0.35 0.34 0.32 0.29 0.27 0.27 0.27 0.28 0.28 0.28 0.28 0.29 0.29 0.29 0.29 0.30 0.30 0.30 0.31

**GrabSideR** (37 frames, 1.20 s), claw forward reach:

    0.61 0.65 0.68 0.72 0.75 0.79 0.84 0.89 0.94 0.99 1.04 1.09 1.13 1.16 1.19 1.23 1.28 1.33 1.38 1.42 1.46 1.48 1.50 1.50 1.50 1.50 1.50 1.51 1.52 1.53 1.54 1.55 1.56 1.57 1.58 1.60 1.61

whole-mesh forward reach:

    0.92 0.93 0.91 0.91 0.94 0.98 1.02 1.10 1.11 1.00 1.04 1.09 1.13 1.16 1.19 1.23 1.28 1.33 1.38 1.42 1.46 1.48 1.50 1.50 1.50 1.50 1.50 1.51 1.52 1.53 1.54 1.55 1.56 1.57 1.58 1.60 1.61

max |x| of the claws:

    3.18 2.88 2.57 2.27 1.97 1.69 1.42 1.18 0.97 0.78 0.62 0.48 0.35 0.25 0.31 0.38 0.39 0.38 0.36 0.35 0.33 0.30 0.27 0.27 0.28 0.28 0.28 0.28 0.29 0.29 0.29 0.29 0.30 0.30 0.30 0.31 0.31

**GrabDropSpin** (58 frames, 1.90 s), claw forward reach:

    1.13 1.17 1.20 1.23 1.25 1.26 1.26 1.27 1.27 1.26 1.26 1.27 1.27 1.26 1.23 1.17 1.03 0.88 0.87 0.87 0.87 0.87 0.87 0.87 0.87 0.87 0.88 0.89 0.91 0.94 0.99 1.04 1.09 1.14 1.20 1.25 1.30 1.35 1.42 1.46 1.51 1.55 1.57 1.57 1.58 1.58 1.58 1.59 1.59 1.59 1.60 1.60 1.60 1.60 1.61 1.61 1.61 1.61

whole-mesh forward reach:

    1.13 1.17 1.20 1.23 1.25 1.26 1.26 1.27 1.27 1.26 1.26 1.27 1.27 1.26 1.23 1.17 1.03 0.88 0.87 0.87 0.87 0.87 0.87 0.87 0.87 0.87 0.88 0.89 0.91 0.94 0.99 1.04 1.09 1.14 1.20 1.25 1.30 1.35 1.42 1.46 1.51 1.55 1.57 1.57 1.58 1.58 1.58 1.59 1.59 1.59 1.60 1.60 1.60 1.60 1.61 1.61 1.61 1.61

max |x| of the claws:

    0.48 0.47 0.47 0.46 0.44 0.43 0.43 0.43 0.42 0.42 0.42 0.42 0.43 0.43 0.47 0.47 0.41 0.35 0.35 0.35 0.35 0.35 0.35 0.35 0.35 0.35 0.35 0.35 0.36 0.37 0.38 0.40 0.42 0.44 0.46 0.47 0.47 0.47 0.45 0.41 0.39 0.37 0.36 0.36 0.35 0.35 0.35 0.34 0.34 0.34 0.33 0.33 0.33 0.32 0.32 0.32 0.31 0.31

## Integrator (when a window is allowed)
1. `CountClaudeImport.Prepare` (gpu_slot, nographics): expect `HC_COUNTCLAUDE_VARIANTS GrabSideL=1.20s GrabSideR=1.20s GrabDropSpin=1.90s` and `HC_COUNTCLAUDE_PASS ... 13 independent motions`. Commit the generated `.anim`, the controller and the three new `.fbx.meta`. A missing FBX logs `HC_CHARACTER_CLIP_SKIPPED` and adds no state.
2. F2: wire Modes 10-12 as above; read `CountClaudeModes.json` `grab_variants` for stand-off.
