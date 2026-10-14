# HANDOFF: piece T2 `hc/wallart` (wall art handoff for Codex)

Branch `hc/wallart` from `origin/haunted-castle` `97c02ce`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/wallart`. Model: Sonnet 5.5, 2026-10-14. No paid generation, no Unity.

## Done (how verified)
- `docs/haunted-castle/CODEX_HANDOFF.md`: inventory (measured with headless Blender: `Portrait.fbx` canvas 0.66 x 1.06 m, `PortraitDoorLeaf.fbx` canvas 0.63 x 1.24 m, both UV 0..1, material `Castle_Canvas`), 11 prompts (all under 120 words, counted), specs, fallback prompts for every other castle image (title key art taken from `origin/hc/title`), licence lines.
- `tools/wallart_prepare.py` (`list`, `make`, `check`): tested on synthetic images (good, wrong eye height, off-centre face, aspect guard, alpha claw, ledger); destinations/metas were tested in a scratch dir inside the worktree and removed.
- `docs/haunted-castle/wallart_runtime.patch`: `git apply --check` clean; applied temporarily and `python3 tools/check_source.py` (with `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll`) PASS (runtime and editor C# compile), then reverted. Not run in an engine.

## Findings (for the integrator)
- All 7 castle canvases share `Castle_Canvas.mat` -> `Resources/Castle/Textures/Portrait.png` (procedural 512x704 placeholder). Distinct paintings need the patch.
- `PortraitEyes` / `SurrealProps.MakePortraitEyes` have no caller in the castle: portraits never followed the player. The patch adds `SurrealProps.AttachPupils` and attaches it to the six frames.
- Ledger pages (`Castle/LedgerSteward|Astronomer|Painter`) and `Castle/ClawScores` do not exist (text-only note, handprint decal).
- Bellwether frames both show `Surfaces/FamilyPhoto.png`; nothing blank.
- `tools/castle_textures.py` and `tools/ui_blood_ledger.py` overwrite `Portrait.png` / `CastleBackdrop.png`; do not re-run them over finished art.

## Requests for integrator
1. Merge `hc/wallart`, apply `docs/haunted-castle/wallart_runtime.patch` (touches `CastleWorld.cs`, `SurrealProps.cs`), `check_source.py`, re-bake the castle (receipt hashes every `.cs`).
2. After Codex's `hc/wallart-art` lands, run one castle capture of the Portrait hall and the portrait door to check pupil depth, door-canvas facing and that per-frame materials serialise.

## Remote branch
`origin/hc/wallart`
