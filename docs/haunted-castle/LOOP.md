# Haunted Castle build loop — rules and ownership

Every agent reads this file first, then `DESIGN.md`. Director: the main session (Fable 5.1). Builders: Opus 5.5 / Sonnet 5.5. Scouts: Haiku 5.5. Critics: Opus 5.5.

## Rules (each one is a past incident)

1. **Branch and worktree.** Integration branch is `haunted-castle` in `/Users/midir/hollow-current-haunted-castle`. Every builder works in its own worktree under `/Users/midir/hollow-current-haunted-castle/.wt/<piece>/` on branch `hc/<piece>`, created with `git worktree add`. Only the integrator (piece 0) edits the main clone. Never touch `/Users/midir/Hollow-Current` or `/Users/midir/hollow-current` (other sessions' checkouts). Never merge to `main`; never force-push; never rebase a branch you did not create.
2. **Ownership.** Edit only the files in your row of the table below. If you need a change elsewhere, write it in your `HANDOFF.md` under "requests for integrator" and stop. Shared files (`Game.cs`, `Interface.cs`, `Investigation.cs`, `HouseLighting.cs`, `NativeBuild.cs`, `Presence.cs`, `Investigator.cs`, `Soundscape.cs`) belong to piece 0 only.
3. **Engines.** Another session is running Unity for a different project on this Mac. Hard cap applies across all sessions. Builders do **not** launch Unity. Only the integrator may, and only via `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-<piece> -- <cmd>`, always `-batchmode -quit`, `-nographics` wherever the step allows, one instance, SIGTERM then wait 60 s to stop, never relaunch after 2 crashes, never a tool that opens a port or an OS dialog. Blender runs headless: `/Applications/Blender.app/Contents/MacOS/Blender -b -P script.py`. Never kill a process you did not start; never use a broad `pkill`.
4. **Compile check without Unity.** `python3 tools/check_source.py` compiles the C# against the installed Unity assemblies and runs the rules tests. Run it before every commit that touches `.cs`.
5. **Assets.** No downloaded packs. Meshes come from committed Blender scripts (`tools/castle_*.py`, idempotent, re-runnable) or from Higgsfield 3D jobs via the `higgsfield` CLI (`higgsfield generate create multi_image_to_3d --image a.png --image b.png --should_texture true --wait --json`). Paid generations: **one job at a time, look at the result, then the next**. Record every paid job (model, cost, job id, output path) in `docs/haunted-castle/ASSET_LOG.md`. Do not commit files over 25 MB; keep the generator script instead and note how to regenerate.
6. **Licensing.** Public MIT repo. Only commit what we may redistribute: our scripts, our meshes, Higgsfield outputs (covered by the existing grant in `ASSET_LICENSES.md`; add a line per asset). Never the reference images of any other game; never a real game's name in a prompt.
7. **Commits.** Small, descriptive, pushed to your `hc/<piece>` branch before you stop. Trailer: `Co-Authored-By: Claude <model> <noreply@anthropic.com>` with your real model. Rewrite `HANDOFF.md` in your worktree root at the end: what is done (verified how), what is not, requests for the integrator, absolute paths.
8. **No self-assessment in captures.** When captures exist, notes record camera, inputs and resolution only. The critic decides quality.
9. **Report under 300 words** plus absolute paths. Conclusions, not dumps.
10. **Never edit** `AGENTS.md`, `CLAUDE.md`, memory, other repos or global settings. Owner messages are handled by the director only.

## Contracts between pieces (so pieces can build in parallel)

- `MapId { Bellwether, Castle }` lives in `Core/MapId.cs` (piece 0). Scene names: `Bellwether.unity`, `Castle.unity`. Bake receipts: `Art/LayoutData/<Map>Bake.json`.
- `CastleWorld.Build(Transform root, CastleWorld.Points out)` mirrors `World.Build`: it creates all geometry under `root` and fills a `Points` struct (Spawn, Gatehouse safe bounds, Anchor, Compass, Voice, Print, HunterSpawn, PerchPoints[], AltarPosition, RelicPositions[3], DrawbridgeTrigger). Floors y = −3.2 / 0 / 3.2 / 6.4. Footprint x ±14, z 0–28. Doors 1.5 × 2.0 m; great doors 3.0 × 4.5 m; service doors 0.9 × 2.0 m.
- Castle meshes load from `Resources/Castle/<Name>` (prefab or mesh+material); `CastleArt.Place(string name, Transform parent, Vector3 pos, Quaternion rot)` is the only loading entry point (piece 2). Missing asset = visible magenta primitive placeholder, never a throw.
- Surreal prop scripts live in `Runtime/Castle/` and are plain MonoBehaviours with no references to Game/Investigation (piece 3): `Floater` (slow bob + drift, candle flame bend toward a Transform target set by the hunter), `RuneRing` (glow on proximity), `PortraitEyes`, `StairLoop` (teleport volume, fires once per investigation via a public `bool Armed`), `KnifeOrbit`, `GiantBat` flock.
- The hunter: `Presence` gets a `HunterProfile` ScriptableObject-free data class (piece 0 splits the constants out; piece 4 supplies `CountClaudeProfile` values + the model under `Resources/Characters/CountClaude` with an Animator using Mode ints 0 Perch, 1 Glide, 2 SearchGlide, 3 Dive, 4 Stagger, 5 Death).
- Audio: clips in `Resources/CastleAudio/` keyed by filename, voices as `voice_<id>.wav` generated by `tools/generate_open_voice.py` from entries added to `tools/voice-lines.json` with ids prefixed `cc_` (piece 6).
- Rules: `Core/Forge.cs` (piece 5) is pure C#: relics found → `Forge.CanCraft`, `Forge.Craft()` → `HasSword`; `Fight` state: hits 0–3, stagger timers. Tests in `Tests/RulesTests.cs`.

## Ownership

| Piece | Branch / worktree | Model | Owns |
|---|---|---|---|
| 0 integrator: map plumbing, picker, checkpoint keyed by map, bake/build per map, Presence profile split, Investigation castle rules wiring | main clone, `haunted-castle` | opus-builder | shared files listed in rule 2, `Core/MapId.cs`, `Scenes/Castle.unity`, `Editor/*` |
| 1 castle layout | `.wt/layout`, `hc/layout` | sonnet-builder | `Runtime/Castle/CastleWorld.cs`, `Runtime/Castle/CastleLayout.cs` |
| 2 castle art | `.wt/art`, `hc/art` | sonnet-builder | `tools/castle_*.py`, `Resources/Castle/**`, `Runtime/Castle/CastleArt.cs`, skybox material |
| 3 surreal props | `.wt/props`, `hc/props` | sonnet-builder | `Runtime/Castle/{Floater,RuneRing,PortraitEyes,StairLoop,KnifeOrbit,GiantBat}.cs` |
| 4 Count Claude model + animation | `.wt/count`, `hc/count` | sonnet-builder (asset) then opus-builder (behaviour, after piece 0) | `Resources/Characters/CountClaude/**`, `Editor/CountClaudeImport.cs`, `Runtime/Castle/CountClaudeProfile.cs`, `tools/count_*.py` |
| 5 forge/fight rules | `.wt/forge`, `hc/forge` | sonnet-builder | `Core/Forge.cs`, tests in `Tests/RulesTests.cs` (append only) |
| 6 audio | `.wt/audio`, `hc/audio` | sonnet-builder | `Resources/CastleAudio/**`, `tools/voice-lines.json` (append only), `docs/haunted-castle/ASSET_LOG.md` (append) |
| 7 critic rounds | reads only | opus-critic | `docs/haunted-castle/rounds/**` |

Integrator merges `hc/*` into `haunted-castle` after `check_source.py` is green. PR to `main` only when the owner says so.
