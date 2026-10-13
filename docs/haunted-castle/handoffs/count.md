# HANDOFF: piece 4a, Count Claude model (branch `hc/count`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/count`)

Model: Sonnet 5.5. Unity was NOT launched; everything below is verified by Blender renders, FBX re-import, and `tools/check_source.py` (PASS, editor C# compiled).

## Done
- Mesh: Higgsfield `multi_image_to_3d`, job `0599a54a-d8de-42fe-a9cb-b3a3ec94d655` (30 credits). Rig: `3d_rigging`, job `325ca2e8-31f1-4706-9063-ae103a0b714d` (5 credits). **Total 35 credits, 2 jobs.** Details in `docs/haunted-castle/ASSET_LOG_count.md`.
- Silhouette check: wings, hooked wing tips, high collar, coat, clawed bare feet all present (Blender renders from front/side/back/3-4). Contact sheets: `tools/count_source/preview_front.jpg`, `preview_side.jpg` (rest, Perch, Glide, Search, Dive, Stagger, Death).
- `tools/count_prepare.py` (headless Blender, idempotent): re-skins wings onto 4 new bones `WingL/WingTipL/WingR/WingTipR` (the auto-rig weighted ~10k wing vertices to the leg bones), scales to head top 2.15 m, soles at y=0, hips over the x/y origin, faces +Z in Unity (same FBX axes as SplitGrin), authors six clips, exports FBX. Re-run: `/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/count_prepare.py` (add `-- --preview DIR` for Workbench renders).
- Final numbers: **30,696 triangles** (no decimation needed, under 40k), 41,753 vertices, 28 bones, max 4 influences per vertex, head top **2.15 m**, wing hooks 2.18 m, feet at y=0 (verified by FBX re-import in Blender). Largest file: `Maps/Albedo.png` 6.7 MB (2048 px); `CountClaudeBody.fbx` 2.2 MB. All under 25 MB.
- Files: `Assets/HollowCurrent/Resources/Characters/CountClaude/{Native/CountClaudeBody.fbx, Native/CountClaude{Perch,Glide,Search,Dive,Stagger,Death}.fbx, Native/CountClaude.json (receipt), Maps/Albedo.png}`. Sources: `tools/count_source/CountClaude_{mesh,rigged}.glb`.
- `Assets/HollowCurrent/Editor/CountClaudeImport.cs` mirrors `SplitGrinImport.cs`. Entry: `HollowCurrent.Editor.CountClaudeImport.Prepare` (batch: `-executeMethod`). It will write, on first run: `CountClaude.controller`, `CountClaude.mat`, `CountClaudeAvatar.asset`, six `.anim` files into `.../CountClaude/`, and the prefab at `Assets/HollowCurrent/Resources/Characters/CountClaude.prefab` (beside the folder, so `Resources.Load<GameObject>("Characters/CountClaude")` resolves). These Unity-generated assets are not committed: nobody has run Unity.
- Animator: Int `Mode`, Float `LocomotionRate` (scales the Glide wing-beat). Any-State transitions, same layout as SplitGrin.
- `Assets/HollowCurrent/Runtime/Castle/CountClaudeProfile.cs`: plain public fields (HunterProfile does not exist on `haunted-castle` yet, so no shape to match): sight 14 m / 80 deg half-angle, glide 4.6 / 3.0 m/s, grab 1.05 m, stagger retreat 20 s, 3 hits, `ModelPath = "Characters/CountClaude"`, `Mode*` int constants, `GlideSpeedFor(width)`. Compiled and run with mono. Fields marked PROPOSED are not in DESIGN.md.

## Clips (all six are authored, none are reused placeholders)
| Mode | State | Length | Loop | Content |
|---|---|---|---|---|
| 0 | Perch | 3.2 s | yes | breathing, slow look, wings folded |
| 1 | Glide | 1.5 s | yes | forward lean, trailing legs, hover bob, wing beats; Animator rate follows LocomotionRate (4.6 m/s gives 2.2x) |
| 2 | Search | 2.6 s | yes | slower glide, head sweeps +-62 deg, wings half open |
| 3 | Dive | 1.1 s | no (holds) | stoop, wings swept back, arms forward, then wings snap forward (wing-wrap grab) |
| 4 | Stagger | 1.7 s | no (holds) | recoil, wing flare, recovering hunch |
| 5 | Death | 2.4 s | no (holds) | knees buckle, topples forward, wings droop |
Motion is procedural keyframing (hand-tuned angles), judged only from stills. Expect polish rounds.

## Requests for integrator (not my files)
1. `Runtime/HearthboundActor.cs:10`: `ModeFrames=new int[4]` and `LateUpdate` does `ModeFrames[mode]++`, so `Pose(4)` or `Pose(5)` throws IndexOutOfRange. Make it `new int[6]` (or `CountClaudeProfile.ModeCount`).
2. `Presence.Initialize` loads `Characters/SplitGrin` and requires `BoneCount>=20` (Count has 28, fine). Wire `CountClaudeProfile.ModelPath`, `EyeHeight` (Presence.cs:93 uses 1.75), sight (`:94`), grab (`:159`), agent speed.
3. `tools/check_source.py` globs `Runtime/*.cs` non-recursively, so `Runtime/Castle/*.cs` is not compiled by it. Add the glob (I compiled `CountClaudeProfile.cs` standalone against the 4.8 API).
4. Run `CountClaudeImport.Prepare` once under Unity (`gpu_slot.sh`, `-batchmode -quit`), commit the generated assets. Merge `ASSET_LOG_count.md` rows into `ASSET_LOG.md` (kept separate to avoid an add/add conflict with `hc/audio`).

## Open issues
- Not seen in Unity. Possible failures: FBX unit scale (import script normalises by ratio against `CountClaude.json`, errors loudly), animation binding paths (same correction as SplitGrin), Standard shader on the wings (mesh has thickness, back-face culling rendered fine in Blender).
- Lower wing tatters are long thin spikes and stretch into streamers when the wing bones swing past about 40 degrees. Angles were reduced; remaining stretch is visible in Glide and Stagger stills.
- Coat skirt is skinned to the leg bones, so leg swings flare it. Fine for glide, would look odd for a walk cycle (no Walk clip exists; he would reuse Glide when landed).
- Texture is base colour only (no normal/PBR maps; ember eyes are baked into the albedo, no emissive).
- Feet hover about 7 cm in Glide by design.
