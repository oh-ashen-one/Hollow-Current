# HANDOFF: F5 grabs (branch `hc/grabs`, Sonnet 5.5, 2026-10-13)

Two new Count Claude clips in the existing Blender pipeline. Mesh and the eight older clips are unchanged (a full re-run reproduces every old clip metric exactly; old FBX were restored byte-for-byte from git, since the FBX writer stamps a timestamp). Unity not launched. No paid generation.

## Regenerate (idempotent, headless, about 90 s)
    /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/count_prepare.py -- --only-new --proof docs/haunted-castle/rounds/r10
- `--only-new`: computes and exports only Mode 8 `GrabDrop` and Mode 9 `GrabCrawl` FBX, merges the receipt, leaves the mesh and older FBX alone.
- No flags: full regeneration of all ten clips + mesh (then `git checkout` any FBX you did not mean to change).
- `--proof DIR`: renders the proof images and `grab_proof_measurements.json`.

## Clips (tools/count_prepare.py, tables `DROP` and `CG`)
- `GrabDrop` (Mode 8, 1.6 s, holds last pose): starts with the lowest point 2.2 m above the origin, wings spread; falls in 0.5 s (constant acceleration), lands in a crouch, then lunges: hips pitched forward, chest pitched 61.3 deg toward the lens, jaw open (-36), right claw reaching to the lower centre of the lens, left claw out beside the face, wings spread forward (membrane area ratio >= 1.52 throughout). Grounded: lowest mesh point is z = 0 at the final pose (Mode 6 floats 0.10 m).
- `GrabCrawl` (Mode 9, 1.4 s, holds last pose): from the Mode 7 crawl pose; coils, lunges 1.2 m forward low to the ground head first with the jaw open (frame 0.52), then rears up (chest 50.5 deg at the end, 76 deg max during the lunge) and the claws close on the lens. Grounded (lowest point z = 0 every frame). Wing area ratio min 0.84.

## Receipt (`Art/Characters/CountClaude/Native/CountClaude.json`)
`grab_drop_reach_m` 1.758, `grab_crawl_reach_m` 1.818 (furthest hand-weighted vertex, forward of origin, final pose; whole-mesh reach is the same, so nothing is nearer the lens than the claws), `modes` [0..9], `mode_names`, per-clip `claw_reach_m_last`, `chest_pitch_deg_last/max`. The same keys are copied to the shipped `Resources/Characters/CountClaude/CountClaudeModes.json` (new; Unity makes its `.meta`) so the player can detect Mode 8/9 with `Resources.Load<TextAsset>("Characters/CountClaude/CountClaudeModes")`; the receipt itself is not in Resources.

## Proof renders (`docs/haunted-castle/rounds/r10/`)
Camera 1.99 m in front of the origin (GrabReach 1.39 + 0.6), eye 1.64 m, vFOV 72 (Investigator camera), aimed at the face centroid of the final pose, 2560x1440 Workbench: `grab_drop_pose.png`, `grab_crawl_pose.png`, `*_frame0.png` (same camera), `*_side.png` (orthographic side contact sheet, 5 frames), `grab_proof_measurements.json`. Measured by projecting the posed hand vertices (geometry, not pixels): GrabDrop right claw bbox 796 px wide, centre 477 px from frame centre (bbox includes the long fingers; the palm sits nearer); GrabCrawl right claw 913 px wide, centre 462 px. The far-side claw sits near the lower frame edge in both. Nearest hand point to the lens 0.36 m (drop) and 0.25 m (crawl). Frame 0 of GrabDrop is nearly empty from this camera: he starts 2.2 m up, above the view.

## What the integrator must do
1. Run `CountClaudeImport.Prepare` (gpu_slot, nographics). Expect `HC_COUNTCLAUDE_GRABS drop=1.60s reach 1.758 crawl=1.40s reach 1.818 receipt modes=0,...,9` and `HC_COUNTCLAUDE_PASS ... 10 independent motions`. Commit the generated `CountClaudeGrabDrop.anim/.crawl.anim`, the controller change and the new `.fbx.meta`/`CountClaudeModes.json.meta`. If an FBX is missing, the import logs `HC_CHARACTER_CLIP_SKIPPED` and adds no state.
2. Presence/profile (not my files): add `ModeGrabDrop=8`, `ModeGrabCrawl=9` (and `ModeCount=10`) to `CountClaudeProfile`; the camera stand-off for these must be the clip's own reach + clearance (`grab_drop_reach_m` + 0.6 = 2.36 m, `grab_crawl_reach_m` + 0.6 = 2.42 m), not the Mode 6 `GrabReach` 1.39 m; my proofs use 1.99 m, so at 2.36/2.42 m the claws are farther from the lens than in the proofs and read smaller. Use hover 0 for Modes 8/9 (grounded clips). GrabDrop's first frames are 2.2 m up with a 4.5 m top: ceilings clip him but he is out of view.
3. `HearthboundActor.ModeFrames` is `new int[8]` (guarded, no throw); size it 10 to count Modes 8/9.
4. Animator is unscaled time in the presentation already (Pose(mode,0,true)); both clips hold the last pose.
