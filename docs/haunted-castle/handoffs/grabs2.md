# Handoff: grabs2 (F5 follow-up, branch `hc/grabs2`, Sonnet 5.5, 2026-10-14)

Scope: wing stretch cap and the GrabDrop rod limb (round-10 blind critic V7/V8). Blender only, CPU; no Unity, no `.anim`/`.meta` committed.

## Regenerate
    /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/count_prepare.py -- --only Grab,GrabDrop,GrabCrawl --proof docs/haunted-castle/rounds/r10
`--only A,B` re-does any named clips (Crawl is measured, never exported, as the baseline); `--only-new` = GrabDrop,GrabCrawl. Diagnose without export: `-- --diag Crawl,GrabDrop [--preview DIR]` (prints `HC_STRETCH` shard edges per bone and `HC_ANGLES`).

## What changed (tools/count_prepare.py)
- `WING_STRETCH_CAP = 1.6`. Grab (6) used 2.2, GrabDrop (8) up to 2.1; now both 1.6 (receipt `wing_stretch_max` per clip, measured from the stretch actually passed to the pose). GrabCrawl was already at 1.6 at its last frame.
- GrabDrop was re-posed. The rod limb in the old proof was the left forearm folded to 113 deg with the wrist 0.41 m from the shoulder (Arm 118, Leg 127 deg, claw 1857-3084 px = off canvas). Now: the same elbow guard as GrabCrawl (wrist never closer than 0.76 m to the shoulder), a shallower landing crouch (hips z -0.32 instead of -0.52 to -0.78), body held back (hips y -0.10 instead of -0.48), claws up at shoulder height. Chest pitch 55 deg (was 61), claw reach 1.615 m (was 1.758).
- Grab (6) pose is unchanged apart from the stretch; hip lift changed (-0.184 instead of +0.303) because the lowest point moved with the shorter wing fingers; lowest point is still 0.10 m.
- GrabCrawl pose is unchanged (FBX and its two PNGs restored byte-identical from the previous commit); only measurement fields were added.

## Numbers (`grab_proof_measurements.json`, receipt `Native/CountClaude.json`)
Shard edges = edges stretched over 3x their rest length and longer than 15 cm posed (max over frames). Mode 7 Crawl baseline 522, budget +15% = 600.

| Clip | wing stretch max | shard edges max | limb max deg (UpLeg/Leg/Arm/ForeArm) | claw reach m | chest pitch |
|---|---|---|---|---|---|
| Grab 6 | 1.6 (was 2.2) | 249 | 18/30/72/10 | 1.385 | 28.4 |
| GrabDrop 8 | 1.6 (was 2.1) | 545 (was 853) | 87/82/96/52 (was 115/127/130/55 at the first fix, 118 Arm originally) | 1.615 (was 1.758) | 55.3 |
| GrabCrawl 9 | 1.6 | 586 | 117/116/98/53 | 1.729 | 46.0 |
| Crawl 7 (baseline) | 0.82 | 522 | 119/117/93/67 | 1.076 | - |

GrabDrop Arm 96 and GrabCrawl Arm 98 are 3 to 5 deg above Mode 7's 93; every other joint is inside the Mode 7 range. The GrabDrop peak Arm (95 to 96) is already reached at frame 0 (arms raised in the fall).

## Proofs (`docs/haunted-castle/rounds/r10/`)
`grab_pose.png`, `grab_drop_pose.png`, `grab_crawl_pose.png` (camera 1.99 m out, eye 1.64, 2560x1440, looking at the face), `*_frame0.png`, `*_side.png`, `grab_proof_measurements.json`.
- GrabDrop: the rod is gone. No thin black limb leaves the frame; both six-fingered claws are inside the frame (left claw 1254-1891 px, was 1857-3084) and both reach toward the lens at the lower centre; the wing outer fingers end in hooks, no zig-zag. Frame 0 shows only his feet at the top: he enters from above, as designed.
- Grab (Mode 6): not rod-free. The outer wing finger on each side is a long dark spar from the shoulder to a hooked tip, running to the left frame edge at the bottom. That is the leading finger of the wing mesh at yaw 66 (1.3x was tried: the spar is the same length, the membrane just shrinks), so I kept 1.6. If the critic still reads it as a rod, the lever is the Mode 6 wing yaw/spread, not the stretch.
- The H09/H10 zig-zag wing tip in the hunt frames comes from the 2.2x Mode 6 pose, so it should go with the 1.6 clip (the runtime clamp `HC_COUNT_WING_CLAMP` becomes a no-op); I did not see a new hunt capture.

## Integrator (when a window is allowed)
1. `CountClaudeImport.Prepare`; the changed FBX are `CountClaudeGrab.fbx` and `CountClaudeGrabDrop.fbx` (Mode 9's FBX is unchanged). Commit the regenerated `.anim`.
2. Camera stand-off for Mode 8 is now `grab_drop_reach_m` 1.615 + 0.6 = 2.22 m (was 2.36). Mode 6 `GrabReach` 1.385 unchanged. `CountClaudeModes.json` carries the new values.
