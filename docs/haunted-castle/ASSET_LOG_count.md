# Haunted Castle asset log: Count Claude (piece 4a)

Merge these rows into `docs/haunted-castle/ASSET_LOG.md` when integrating (kept separate so `hc/count` and `hc/audio` do not both add that file). Paid generations, one job at a time, 2026-10-13, Higgsfield CLI 1.1.23. Credits are the `spend` rows from `higgsfield account transactions` (balance deltas are polluted by the audio session spending in parallel, and `generate cost` is not supported for these two models).

| Step | Model | Credits | Job id | Inputs | Output |
|---|---|---|---|---|---|
| Mesh + texture | `multi_image_to_3d` (Multi-Image to 3D) | 30 | `0599a54a-d8de-42fe-a9cb-b3a3ec94d655` | `docs/haunted-castle/refs/count_front.png`, `count_side.png` (trimmed to 765 px wide to drop the back-view sliver), `count_back.png`; `--should_texture true --enable_rigging false --enable_animation false` (the CLI rejects the call unless those two are passed explicitly) | `tools/count_source/CountClaude_mesh.glb` (4.9 MB, 30,696 tris, 2048 px base-colour texture). Record: `refs/count_mesh.job.json` |
| Rig | `3d_rigging` (3D Rigging) | 5 | `325ca2e8-31f1-4706-9063-ae103a0b714d` | the mesh job's GLB URL, `--enable_animation false` | `tools/count_source/CountClaude_rigged.glb` (6.3 MB, 24-bone Meshy-style humanoid, no wing bones). Record: `refs/count_rig.job.json` |

Total: 35 credits, 2 jobs (second mesh job not needed: wings, hooked tips, coat and silhouette all present).

Animation clips: the Higgsfield animation library (678 humanoid actions, `animation_action_id`) was **not** used. It is biped-only (the wings would not move), one action per paid job, and none of the six modes exist in it. All six clips are authored locally by `tools/count_prepare.py` (free, regenerable).

Local processing: `tools/count_prepare.py` re-skins the wings onto four new bones (`WingL`, `WingTipL`, `WingR`, `WingTipR`; the auto-rig had weighted 10k wing vertices to the leg bones), rescales to a 2.15 m head, grounds the feet, and writes `Assets/HollowCurrent/Resources/Characters/CountClaude/Native/*.fbx`, `Maps/Albedo.png` and `Native/CountClaude.json`.
