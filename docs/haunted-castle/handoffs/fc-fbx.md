# fc-fbx: door leaf thickness and the old castle kit's FBX axis (wave FC)

Branch `hc/fc-fbx` (from `haunted-castle` `0bd9db5`), worktree `.wt/fc-fbx`. Model: Opus 5.5. Date 2026-10-18. No Unity launch (both GPU slots held by other sessions; LOOP rule 3): everything below is static evidence (FBX vertices and normals read in Python, the baked `Scenes/Castle.unity` at `0bd9db5`, the wave-FB bake log `.castle-bake2.log`, the r13 `fb2` captures). The integrator must re-prepare, re-bake and capture (recipe at the end). Follow-up of fb-doors (items 3-5 of `rounds/r13/OWNER_FEEDBACK.md`, its two open risks).

## 1. Door leaf thickness

Facts (FBX vertices, `tools/castle_architecture.py` `DOOR_LEAF_SPECS`): the oak slab is 0.12 m (great), 0.09 (standard), 0.08 (service), 0.10 (portrait); straps stand 1.5 cm and ring plates and rings 3-6 cm proud of each face, so the imported leaf is 0.240 / 0.194 / 0.184 / 0.204 m overall (the 0.19-0.24 in `HC_DOOR_LEAF bounds=`). The collision panel was 0.09 m for every kind, so it was thinner than the great/portrait slab (camera could reach the planks) and thicker than the service slab (invisible skin).

Decision: the mesh is too thick, the panel follows the mesh. A real oak great-door leaf is 0.08-0.12 m, a service door 0.05-0.07 m. `CastleKitAxis.FitDoorLeaf` (called from `CastleWorld.BuildDoor`) scales the leaf art along its thickness axis (hinge Z) so the oak slab is **0.10 m great, 0.07 m standard, 0.06 m service, 0.07 m portrait**, and the panel gets exactly that thickness: collider = visible oak, hardware 1-4 cm proud like real ironwork, the slab inside the swing probe (`Door.ProbeExtents.z` 0.055 half), so a leaf parked against a wall no longer pushes planks through the wall face. Leaf overall after fitting: 0.200 / 0.151 / 0.138 / 0.143 m. A leaf whose overall thickness does not match the authored FBX (a future re-export) is left unscaled with the old 0.09 panel. Log: `HC_DOOR_LEAF_THICKNESS name= art= meshZ= slabAuthored= slab= scaleZ= overall= collider= fitted=True`. Swing direction, `Door.cs` (player-collision ignore while moving, `ChooseSwing`) and the "Open/Close <name>" prompt are untouched; the swing probe never used the panel (it ignores the hinge's own children).

Portrait door canvas (found by this audit): the canvas is a single-sided quad (Standard shader, back-face culled) whose visible side is its local +Z (FBX normal (0,0,1)), but the FBX puts it on the -Z face of the slab (`door_leaf`: canvas at y = t/2 + 0.012 with a -Y winding). It faced into its own leaf and showed from neither side, before and after fb-doors' turn. `CastleKitAxis.FaceCanvasOut` turns it 180 degrees about its own centre when it faces the slab, so the painting faces the portrait hall (the side it sits on after BuildDoor's turn), not mirrored. Log: `HC_PORTRAIT_CANVAS ... flipped=True now=+z world=(-1.00, 0.00, 0.00)` expected.

## 2. Old-kit axis audit

Cause (unchanged from art-exterior2 section 4 / fb-doors): `tools/castle_lib.py export_asset` without `castle_gothic.turn_to_unity` (manifests architecture, props, furniture, props_r12) imports turned 180 degrees about Y: Blender (x, y, z) -> Unity (-x, z, -y). Import settings are identical for all 108 kit FBX (`bakeAxisConversion: 0`, `globalScale 1`, `useFileScale 1`); the round-13 hall kit (`generateSecondaryUV: 1`: Altar, Candelabra, Candlestick, ChapelSunburst, the four windows, Hall*, LongTable, Pew, PointedArch) and the exterior (`Ext*`) bake the turn and are not affected. **The FBX and their .meta were not touched.**

Method per piece: the imported mesh bounds (FBX vertices with part transforms, Unity mapping) and, for single-sided or furniture pieces, where the back/front geometry sits (high-back verts, back panels, firebox backs, canvas normals), compared with the placement code's intent and with the "Wall" boxes of the baked scene (back gap to the wall behind the origin at three heights, clear room in front, share of the bounds inside walls). `python3 tools/check_castle_kit.py` on the baked scene (`0bd9db5`) reports **problems=45** (all Turn rows below); `--simulate-fix` reports **problems=0**.

| Piece (instances) | As imported (baked `0bd9db5`) | Decision |
|---|---|---|
| Bookshelf (6, library) | open shelves face the wall 0 m away, plain back panel to the room | **Turn** |
| Fireplace (1, dining) | firebox and mantel face the west wall, 14 % of its bounds inside it | **Turn** |
| KitchenHearth (1) | cooking opening faces the west wall | **Turn** |
| Sideboard (1, dining) | doors face the north wall, 3 cm inside it (4 %) | **Turn** |
| CanopyBed (1) | headboard in the room, foot against the wall | **Turn** |
| TapestryPanel (6, hall) | emblem and braid face the wall; cloth hangs on the wall side of its anchor (S1 shows plain red backs) | **Turn** |
| Throne (1, dais) | seat faces the north wall 0.5 m away, high back to the hall (back verts at import +Z 0.17-0.34) | **Turn** |
| ReliquaryBox (1) | hasp face to the wall | **Turn** |
| Winch (1, gatehouse) | crank/ratchet face the gate wall | **Turn** |
| HighBackChair (10, dining and hall) | seats face away from their tables (also visible in `fb2/cold1/12_cold_hidden_Dining_armoire.png`) | **Turn** |
| Lectern (1) | reading side faces the east wall, not the room | **Turn** |
| Orrery (1) | planet socket mirrored: `World.OrrerySocket` is in contract coordinates, so the hilt relic floats 1.7 m from the real socket ((8.17, 8.95, 24.20) vs (9.83, 8.95, 23.80)); after the turn the socket is where the hilt is | **Turn** |
| CoffinNiche (14, crypt) | recess block stands 0.6 m into the crypt, the coffin points out of the castle; the wall cut, candle and collision proxy all expect the contract | **Turn** |
| Banner, StringCourse, Corbel, GargoyleSpout, StairFlight (not placed) | would project into the wall / climb -Z if placed by the contract | **Turn** if ever placed |
| Portrait (6) | canvas single-sided, visible on import +Z, which every call site's yaw points into the room; pupils (`AttachPupils`) sit on that side | **As imported** (correct) |
| GiantSkeleton | head toward the doors; FB layout and bone collisions are measured on the import | as imported |
| DeadTree (2) | organic; the turn would push the east crown into the chapel wall (overlap 0.02 -> 0.20) | as imported |
| GateArch | its two small corbels face the moat (the approach) | as imported (correct by accident) |
| Telescope | tube points into the observatory; the contract pose drives the tube through the north wall (22 % of its bounds in wall boxes) | as imported; re-anchor with an in-engine check (see risks) |
| LanternCart, ButcherBlock, ReadingTable, Sarcophagus | free-standing, no wall or target relation, in approved shots | as imported |
| RelicTablet | pocket order mirrored vs the fallback sockets (cosmetic; S2 approved) | as imported |
| Books, tomes, relics, sword, hand roots, root crowns | near-symmetric or tuned rotations / organic | as imported |
| DoorLeaf (11), GreatDoorLeaf (6), ServiceDoorLeaf (5), PortraitDoorLeaf (1) | fb-doors turns a leaf found on -X (23 x `turned=True`) | hinged (BuildDoor), now also thinned |
| Drawbridge, DrawbridgeChains | BuildGate turns deck and chains toward the moat | adaptive (unchanged) |
| Wall_2m (226), FloorTile (96), Battlement (28), Balustrade (15), Buttress (10), Door/GreatDoor/ServiceDoorFrame (19), RoseWindow, Column (4), CryptRib, VaultRib, RafterBeam, CryptBridge, ChapelBell, DiningTable, StoneFont, Pedestal | voxel-identical under a 180 degree turn | symmetric |
| Cauldron, Fountain | three legs / round basin, no front | symmetric |

The fix (`Runtime/Castle/CastleKitAxis.cs`, hook in `CastleArt.Place`): for **Turn** assets whose model bounds still match the old import (within 3 cm), `Place` returns a new empty root `Castle / <name>` (the contract frame, so callers' positions, yaws, scales and local offsets such as `OrrerySocket` keep their meaning) holding the model, renamed `FBX / <name>`, turned 180 degrees about the asset origin. That is exactly the old placement turned about its origin; no position, yaw or collision proxy changes. A re-exported asset that already faces +Z, or an unknown mesh, is left alone (`HC_KIT_AXIS_IMPORT` warning). `CastleArt.ImportYaw` stays 0 (the correction is per asset).

Prepare-time log (`CastleKitAxis.Log`, end of `CastleWorld.Construct`): one `HC_KIT_AXIS name=<art>#<n> art= policy= check= turned= pos= front= bounds=<centre>/<size> [backGap= frontClear= wallOverlap=] expected= ok=` per placed piece (Turn, AsImported-front, Hinged, Adaptive), one aggregated `HC_KIT_AXIS name=<art> ... instances=N expected=symmetric_under_180 ok=True` per symmetric/small asset, and `HC_KIT_AXIS_SUMMARY instances= aggregated= turned= wallBacked= problems=`. Wall-backed pass: back gap <= 0.6 m, front clear >= 0.3 m, overlap <= 5 %. In a player only failures and the summary are logged (per-instance lines in the editor and under `-hc-verify`).

## Files

| File | Change |
|---|---|
| `Runtime/Castle/CastleKitAxis.cs` (+ .meta) | new: policy table (per asset, measured bounds, reason), `ImportFix`, `FitDoorLeaf`, `FaceCanvasOut`, `Log` |
| `Runtime/Castle/CastleArt.cs` | `Place`: `item = CastleKitAxis.ImportFix(name, item)` after `ApplyMaterials`; ImportYaw comment |
| `Runtime/Castle/CastleWorld.cs` | `BuildDoor`: panel thickness from `FitDoorLeaf`, `FaceCanvasOut` before `Paint` (3 lines); `Construct`: `CastleKitAxis.Log(root)` (1 line) |
| `Runtime/Castle/CaptureMode.cs` | `-hc-capture-art kit` (3 views) and `KitLog` (`kit <view> <door> leafScaleZ= panelZ= leafBounds= canvasFacing=`) |
| `tools/check_castle_kit.py` | new: log mode and scene mode (`--simulate-fix`, `-v`) |
| `tools/check_castle_doors.py` | adds: panel thickness = oak slab (DOOR_LEAF_SPECS t x leaf Z scale) within 5 mm, slab 0.08-0.12 great / 0.05-0.08 others; `--simulate-fix` applies the fitting |

## Verified (static, this branch)

- `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py`: PASS (1191 assertions; known CS0649 / CS0162 warnings only).
- `check_castle_scares.py` PASS, `check_throwables_scene.py` OK (0 / 0), `check_audio_keys.py` OK.
- `check_castle_kit.py` on the baked scene: FAIL problems=45 (as expected: no fix baked); `--simulate-fix`: OK problems=0, 45 turned, all 25 wall-backed pieces back gap 0.00-0.55 m, overlap 0.00. Log mode checked on a synthetic log (OK) and on `.castle-bake2.log` (FAIL: no summary).
- `check_castle_doors.py` on the baked scene: FAIL 23 (panel 0.09 vs slab 0.12/0.08/0.10, standard slab 0.09 over 0.08); `--simulate-fix`: OK.

## Integrator recipe

1. Merge `hc/fc-fbx`. Conflicts likely only in `CastleWorld.cs` / `CaptureMode.cs` / `CastleArt.cs`; my edits are the lines listed above.
2. Through the slot, one instance, background: `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-fc-fbx -- /Applications/Unity/Hub/Editor/6000.3.20f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/midir/hollow-current-haunted-castle -executeMethod HollowCurrent.Editor.HouseLighting.PrepareAndBakeCastle -logFile /Users/midir/hollow-current-haunted-castle/.castle-bake-fc.log` (then `PrepareAndBake` for Bellwether: any `.cs` change stales its receipt).
3. Both must exit 0: `python3 tools/check_castle_doors.py` (re-baked scene) and `python3 tools/check_castle_kit.py /Users/midir/hollow-current-haunted-castle/.castle-bake-fc.log`; also `python3 tools/check_castle_kit.py` (scene mode, no flag) must print OK. In the log expect `HC_KIT_AXIS_SUMMARY ... turned=45 ... problems=0`, 23 `HC_DOOR_LEAF_THICKNESS ... fitted=True`, one `HC_PORTRAIT_CANVAS ... flipped=True now=+z`, no `HC_KIT_AXIS_IMPORT`, `HC_DOOR_LEAF_SUMMARY problems=0` (both stages), unchanged `HC_CASTLE_DOOR` / `HC_DOOR_CLEARANCE` angles. `check_throwables_scene.py` exit 0.
4. Build (`NativeBuild.BuildPreparedMacAll`), then captures, windowed, `open -g`: `gpu_slot.sh capture --label hc-fc-fbx -- open -g -W -n -a "/Users/midir/hollow-current-haunted-castle/Builds/macOS/Hollow Current.app" --args -hc-capture <dir> -hc-capture-art kit -hc-capture-size 1920x1080 -logFile <dir>/kit.log`, and the same with `-hc-capture-art doors`. Expected: `art_kit_kit_window_inside` fireplace opening and mantel toward the camera between the two lancet windows, chairs facing the table; `art_kit_kit_portrait` the two south-wall paintings and a painting on the portrait door (none before); `art_kit_kit_door_edge` the right hall leaf seen almost edge-on: oak slab inside the frame reveal, rings a few cm proud, no plank face beyond the frame; `kit.log` `leafScaleZ=0.833 panelZ=0.100` for the hall leaves, `leafScaleZ=0.700 panelZ=0.070` and `canvasFacing=(-1.00, 0.00, 0.00)` for the portrait door. Doors set: unchanged behaviour (swing away, pass-through).
5. Recapture S1-S3 for the critic: S1 throne now faces the hall and the four visible tapestries show their emblem; S3 niches read as recesses with coffins drawn out; S2 unchanged.

## Risks and notes

- Approved shots change: S1 (throne, tapestries, hall chairs), S3 (14 niches), S7 gatehouse (winch). The pieces keep their positions and proxies; only the art turns.
- The portrait-door canvas fix relies on the FBX normal (+Z) and back-face culling; if the frame shows the painting on the closet side instead, the leaf's turn and the canvas logic disagree: read `HC_PORTRAIT_CANVAS`.
- Telescope left as imported (tube into the room). Aiming it through the north observatory window (x 12, sill 7.3) needs a re-anchor plus an in-engine look (the 2-light tracery has a centre mullion and saddle bars); fc-props' "period instrument meshes" is the natural owner. If any old-kit FBX is replaced by an axis-baked one, update or delete its row in `CastleKitAxis.Kit` (otherwise `HC_KIT_AXIS_IMPORT` warns and the piece is left alone, which is then correct, and the log flags it only if its new bounds match neither orientation).
- `CastleLayout`'s GiantSkeleton comment ("head +Z") is stale: the head is at -Z, toward the doors, which the FB layout wants; left as is.
- Kitchen: the hearth (3 m wide, 2.4 m tall) stands in front of the west lancet window at z 5 (pre-existing layout overlap, not changed).
- The leaf scaling also scales ring/strap depth (rings stay round in the face plane, only their 4 cm tube depth shrinks to ~3 cm).
- `CastleArt.Place` now returns an extra parent for 45 instances (one empty GameObject each). Code that looks up `Castle / <name>` (World.FindArt for Orrery and ReliquaryBox) gets the contract-frame root, which is what its offsets assume.
