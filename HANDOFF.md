# HANDOFF: hc/art (piece 2, castle art)

Branch `hc/art`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/art`. Built by Sonnet 5.5. No Unity launched; Blender ran headless only.

## What is done (verified how)
- 68 FBX assets in `Assets/HollowCurrent/Resources/Castle/` (whole Castle folder 29 MB incl. 18 MB of sky PNGs, textures and metas; largest FBX 159 KB). Verified with `Blender -b -P tools/castle_verify.py`: re-imports every FBX, prints size and tris, compares with the generator manifests and with `CastleLayout.ArtSizes` (tolerance max(0.2 m, 20%)), checks walls < 200 tris, props < 3k, skeleton < 15k, door leaves 1.5x2.0 / 3.0x4.5 / 0.9x2.0. Last run: `HC_VERIFY_PASS assets=68 fails=0`.
- `CastleArt.cs` compiled with Unity's Roslyn against both the macOS player and editor `UnityEngine*.dll` sets (C# 9, warnings as errors), plus a compile probe of the exact call shapes `CastleWorld.cs` on `hc/layout` uses. Not run in engine.
- Procedural tileable textures + `.mat` files (Standard shader) for stone, black stone, oak, brass, oxblood, silver, iron, bone; portrait canvas texture. Textures have hand-written `.meta` (normal maps flagged as normal maps, deterministic guids), so the `.mat` references resolve without an import round-trip.
- Two-moon skybox: `Resources/Castle/SkyCastle.mat` (Skybox/6 Sided) + `Resources/Castle/Sky/Sky_{Front,Back,Left,Right,Up,Down}.png` (2048 px, clamp, no mips). Preview: `docs/haunted-castle/art/sky_preview.jpg` (equirect). Pale moon az -34/el 27 (r 11 deg), blood moon az +42/el 24 (r 4.2 deg), green aurora between, big stars; azimuth from +Z towards +X (the entry view). `CastleArt.PaleMoonDirection/RedMoonDirection` give the directions for the moonlight rig (light travels the opposite way).

## Names (read this, piece 0/1)
`hc/layout` has its `CastleLayout.ArtNames` (commit 136b002); every one of its 49 names exists as an FBX with that exact name and origin = base centre, front = +Z at yaw 0, leaf origin = hinge edge at floor level extending +X, Drawbridge origin = hinge line on the top surface extending -Z (see table, sizes in W x H x D, Unity axes). Extras the layout does not place yet: `Wall_2m/4m(_Window)`, `PointedArch`, `VaultRib`, `FloorTile`, `StairFlight` (3.2 m rise), `GreatDoor/ServiceDoor/Door` (frame + hinged leaves in one FBX), `BanquetTable` (8 m with 14 chairs), `StoneFont`, `CoffinNiche`, `StoneBridge`, `GiantSkeleton` (11.96 m, 4.2k tris, one mesh), `FloatingBook`, `Banner`, `DrawbridgeChains`.
`CastleArt.Place(name, parent, localPos, rot)` returns `CastleArt.Placed`, which converts implicitly to `Transform` (what `CastleWorld.ArtAt` needs) and to `GameObject` (what the brief asked for). Missing asset: magenta cube + one `[CastleArt] missing <name>` warning, never a throw. `CastleArt.PlaceSolid` also adds MeshColliders. `CastleArt.ApplySkybox()` sets `RenderSettings.skybox`.
Parts the surreal-props piece can find by name: `Candelabra` has children `Flame_0..Flame_4` (pivot at flame base, `Castle_Flame` emissive; the 'Candle light' child is still the layout's); `Orrery` has `Sun`; `Portrait` and `PortraitDoorLeaf` have `Canvas` (0..1 UVs, `Castle_Canvas`/`Portrait.png`). Orrery planet socket (scaled x2.3, orrery-local, Blender axes x,y,z = Unity x,z,y): see `manifest_props.json` `Orrery.notes.socket`.

## Asset table (size W x H x D in Unity axes, tris, layout nominal)
| asset | size | tris | layout nominal |
|---|---|---|---|
| Altar | 2.40 x 1.11 x 1.11 | 264 | 2.4 x 1.1 x 1.1 |
| Balustrade | 2.00 x 1.12 x 0.20 | 552 | 2.0 x 1.1 x 0.2 |
| Banner | 1.39 x 2.98 x 0.09 | 304 | - |
| BanquetTable | 9.61 x 1.30 x 2.41 | 2756 | - |
| Battlement | 4.00 x 0.97 x 0.88 | 72 | 4.0 x 1.0 x 0.8 |
| Bookshelf | 2.00 x 3.00 x 0.50 | 1512 | 2.0 x 3.0 x 0.5 |
| ButcherBlock | 1.40 x 0.94 x 1.00 | 96 | 1.5 x 0.9 x 1.0 |
| Buttress | 0.90 x 6.40 x 1.20 | 72 | 0.9 x 6.4 x 1.2 |
| Candelabra | 0.42 x 1.68 x 0.42 | 782 | 0.5 x 1.7 x 0.5 |
| CanopyBed | 2.40 x 2.80 x 2.60 | 588 | 2.4 x 2.8 x 2.6 |
| Cauldron | 1.04 x 1.03 x 0.99 | 284 | 1.0 x 0.9 x 1.0 |
| ChapelBell | 1.26 x 1.92 x 0.86 | 400 | 1.2 x 2.0 x 0.9 |
| ClerestoryWindow | 1.20 x 1.80 x 0.35 | 272 | 1.2 x 1.8 x 0.35 |
| CoffinNiche | 2.20 x 1.45 x 2.58 | 136 | - |
| Column | 0.70 x 3.00 x 0.70 | 296 | 0.7 x 3.0 x 0.7 |
| ColumnGreat | 0.90 x 8.80 x 0.90 | 392 | 0.9 x 8.8 x 0.9 |
| CryptBridge | 1.40 x 0.31 x 1.20 | 48 | 1.4 x 0.3 x 1.2 |
| CryptRib | 8.40 x 0.60 x 0.60 | 76 | 8.4 x 0.6 x 0.6 |
| DeadTree | 2.58 x 4.67 x 2.20 | 2002 | 2.4 x 4.5 x 2.4 |
| DiningTable | 5.60 x 1.09 x 1.50 | 670 | 5.6 x 0.9 x 1.6 |
| Door | 1.80 x 2.15 x 0.40 | 164 | - |
| DoorFrame | 1.96 x 2.60 x 0.84 | 68 | 1.9 x 2.6 x 0.8 |
| DoorLeaf | 1.50 x 2.00 x 0.19 | 220 | 1.5 x 2.0 x 0.09 |
| Drawbridge | 3.24 x 0.27 x 6.00 | 336 | 3.2 x 0.25 x 6.0 |
| DrawbridgeChains | 3.15 x 4.38 x 5.54 | 1872 | - |
| Fireplace | 2.68 x 2.62 x 1.05 | 220 | 2.6 x 2.6 x 0.9 |
| FloatingBook | 0.40 x 0.10 x 0.28 | 60 | - |
| FloorTile | 2.00 x 0.26 x 2.00 | 24 | - |
| Fountain | 2.56 x 1.78 x 2.43 | 280 | 2.6 x 1.8 x 2.6 |
| GateArch | 3.90 x 5.60 x 1.00 | 128 | 3.8 x 5.6 x 1.0 |
| GiantSkeleton | 4.11 x 3.39 x 11.96 | 4222 | - |
| GreatDoor | 3.60 x 4.80 x 0.50 | 340 | - |
| GreatDoorFrame | 3.66 x 5.20 x 0.94 | 68 | 3.6 x 5.2 x 0.9 |
| GreatDoorLeaf | 1.50 x 4.50 x 0.24 | 244 | 1.5 x 4.5 x 0.12 |
| HighBackChair | 0.51 x 1.30 x 0.51 | 112 | 0.6 x 1.3 x 0.6 |
| KitchenHearth | 3.00 x 2.40 x 1.04 | 182 | 2.8 x 2.4 x 1.0 |
| LancetWindow | 1.20 x 1.60 x 0.35 | 272 | 1.2 x 1.6 x 0.35 |
| LanternCart | 1.74 x 1.32 x 1.10 | 628 | 1.8 x 1.3 x 1.0 |
| Lectern | 0.68 x 1.13 x 0.50 | 148 | 0.7 x 1.2 x 0.6 |
| LongTable | 6.60 x 1.10 x 1.30 | 1188 | 6.6 x 0.95 x 1.4 |
| ObservatoryWindow | 1.60 x 2.70 x 0.35 | 284 | 1.6 x 2.7 x 0.35 |
| Orrery | 2.79 x 2.90 x 2.74 | 1236 | 3.0 x 2.8 x 3.0 |
| Pedestal | 0.60 x 0.99 x 0.60 | 160 | 0.6 x 1.0 x 0.6 |
| Pew | 2.40 x 1.00 x 0.70 | 120 | 2.4 x 1.0 x 0.7 |
| PointedArch | 3.90 x 5.11 x 0.50 | 116 | - |
| Portrait | 0.90 x 1.30 x 0.10 | 98 | 0.9 x 1.3 x 0.1 |
| PortraitDoorLeaf | 0.90 x 2.00 x 0.20 | 222 | 0.9 x 2.0 x 0.1 |
| RafterBeam | 11.50 x 0.56 x 0.56 | 84 | 11.4 x 0.5 x 0.5 |
| ReadingTable | 2.00 x 0.96 x 1.00 | 410 | 2.0 x 0.85 x 1.0 |
| ReliquaryBox | 1.00 x 0.98 x 0.70 | 92 | 1.0 x 1.0 x 0.7 |
| RoseWindow | 3.00 x 3.00 x 0.50 | 864 | 3.0 x 3.0 x 0.5 |
| Sarcophagus | 2.49 x 1.21 x 1.18 | 120 | 2.2 x 1.2 x 1.0 |
| ServiceDoor | 1.14 x 2.12 x 0.35 | 164 | - |
| ServiceDoorFrame | 1.26 x 2.30 x 0.84 | 68 | 1.2 x 2.3 x 0.8 |
| ServiceDoorLeaf | 0.90 x 2.00 x 0.18 | 220 | 0.9 x 2.0 x 0.08 |
| Sideboard | 2.60 x 1.10 x 0.60 | 144 | 2.6 x 1.1 x 0.6 |
| StairFlight | 2.60 x 4.15 x 5.04 | 172 | - |
| StoneBridge | 2.68 x 4.30 x 8.06 | 128 | - |
| StoneFont | 1.04 x 1.08 x 1.04 | 200 | - |
| TallLancetWindow | 1.40 x 3.50 x 0.35 | 284 | 1.4 x 3.5 x 0.35 |
| Telescope | 0.86 x 2.35 x 2.58 | 396 | 1.0 x 2.2 x 2.4 |
| Throne | 1.00 x 1.90 x 0.90 | 208 | 1.0 x 1.9 x 0.9 |
| VaultRib | 4.60 x 2.84 x 0.40 | 180 | - |
| Wall_2m | 2.00 x 3.20 x 0.66 | 36 | - |
| Wall_2m_Window | 2.00 x 3.20 x 0.66 | 128 | - |
| Wall_4m | 4.00 x 3.20 x 0.66 | 36 | - |
| Wall_4m_Window | 4.00 x 3.20 x 0.66 | 140 | - |
| Winch | 1.12 x 1.59 x 0.80 | 342 | 1.0 x 1.6 x 0.8 |

## Regenerate
`tools/castle_build_all.sh` (textures, sky, architecture, props, furniture, verify). Individually: `python3 tools/castle_textures.py`, `python3 tools/castle_sky.py 2048`, `Blender -b -P tools/castle_architecture.py [-- Name ...]`, `castle_props.py`, `castle_furniture.py`, `castle_verify.py`. Contact-sheet render for eyeballing: `Blender -b -P tools/castle_preview.py -- out.png [Name ...]` (workbench, `HC_AZ`/`HC_EL` env pick the view). All PNG/mat/meta output is byte-deterministic; FBX bytes embed a creation timestamp, so re-running shows every FBX as modified although the geometry (see manifests) is identical. The `Traceback ... blender_mcp.py unregister` printed when Blender exits is the user's MCP add-on, unrelated.

## Open issues / not verified in engine
1. **FBX axis/handedness is the main risk.** Authored with Blender front = +Y, exported `axis_forward=-Z, axis_up=Y, bake_space_transform`. I could not confirm in Unity that front lands on +Z and +X stays +X. If the first import shows assets facing -Z (leaves swinging wrong way, drawbridge going +Z), set `CastleArt.ImportYaw = 180`; that rotates about each asset's own origin and fixes front, hinge side and bridge direction together.
2. `SkyCastle.mat` uses `m_Shader: fileID 103` (Skybox/6 Sided) from memory of Unity's built-in shader ids. `ApplySkybox` rebuilds the material from the six faces if the shader name does not match. Face orientation follows Unity's 6-sided convention (Front +Z, Left +X, Right -X); seams are continuous (checked in the equirect preview) but Up/Down rotation is unconfirmed.
3. Materials: `CastleArt` swaps FBX materials by name (`Castle_*`) for the `.mat` files, falling back to code-made Standard materials; exact look is unreviewed. Textures are flat-lit noise, not hand-painted; no critic pass yet.
4. Poly-count budgets are met, but nothing was measured in a build (60 fps at 1440p is unproven). Biggest single meshes: `BanquetTable` 2.8k, `DeadTree` 2.0k, `DrawbridgeChains` 1.9k, `GiantSkeleton` 4.2k.
5. Buttress is symmetric on purpose (layout rotates it inconsistently relative to the wall). Windows are open tracery with no glass. `DeadTree` is 2.6 x 4.7 x 2.3 vs nominal 2.4 x 4.5 x 2.4. `Drawbridge` chains are a separate static asset and do not follow the leaf when raised.
6. `tools/check_source.py` only globs `Runtime/*.cs`; `Runtime/Castle/` is not compiled by it (layout piece noted the same).

## Requests for integrator
- Take `hc/art` `Runtime/Castle/CastleArt.cs` over the layout stub (they conflict by design). No `.cs.meta`/folder `.meta` committed for `Runtime/Castle` to avoid guid clashes with `hc/layout`.
- Add one line per art family to `ASSET_LICENSES.md` (all original, produced by `tools/castle_*.py`; no downloads, no Higgsfield jobs, nothing to log in ASSET_LOG.md). I did not edit shared files.
- Call `CastleArt.ApplySkybox()` from the castle scene setup and drive the directional moon lights with `PaleMoonDirection`/`RedMoonDirection`.
- First Unity import: check one door (leaf swing), the drawbridge and the altar niches for orientation (open issue 1).

## Absolute paths
Worktree `/Users/midir/hollow-current-haunted-castle/.wt/art`; assets `.../Assets/HollowCurrent/Resources/Castle/`; scripts `.../tools/castle_*.py`; loader `.../Assets/HollowCurrent/Runtime/Castle/CastleArt.cs`; manifests and previews `.../docs/haunted-castle/art/`.
