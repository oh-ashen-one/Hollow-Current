# HANDOFF: hc/layout (piece 1, castle layout)

Branch `hc/layout`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/layout`. Built by Sonnet 5.5. No Unity or Blender launched.

## What is built
- `Assets/HollowCurrent/Runtime/Castle/CastleWorld.cs`: `CastleWorld.Build(Transform root, out CastleWorld.Points points)` plus `CastleDrawbridge` (MonoBehaviour, starts raised; `Lower()` / `Raise()` / `Snap(bool)`; hinge rotates 90 deg about X, 30 deg/s) and `CastleWorld.Dispose(ref Points)`.
- `Assets/HollowCurrent/Runtime/Castle/CastleLayout.cs`: constants, `ArtNames`, `ArtSizes`, `Rooms`, `Doors` (with Standard/Great/Service/Gate kind), `Windows`, `Candles`, `RoomAt(Vector3)`, `IsServiceDoor(Door)`.
- `Assets/HollowCurrent/Runtime/Castle/CastleArt.cs`: `// STUB: replaced by hc/art`. Magenta cube sized from `CastleLayout.ArtSize(name)`, base-centre origin. Delete it when merging hc/art.
- All geometry is built imperatively under `root` in castle-local coordinates (root must have unit scale); `Points` are returned in world space. Structure (walls, slabs, stairs, curbs, rails) is Unity primitives with flat Standard materials created in code (names in `Points.Materials`: stone, wall, darkstone, cobble, wood, darkwood, carpet, brass, paper, metal, black, earth, cloth, glass, blood). Every architectural/furniture element also goes through `CastleArt.Place` (names below) with an invisible `BoxCollider` proxy so physics and the NavMesh never depend on art colliders.
- Navigation: baked at the end of `Build` exactly as `World.BuildNavigation` (enabled non-trigger BoxColliders, Door hierarchies skipped, agent r .28 / h 1.8 / climb .25 / slope 45, voxel .1). Result is in `Points.NavData/NavInstance`. Stairs are 16 steps of 0.2 x 0.3 m. The courtyard has no roof and no walkway over it.

## Points (castle-local; Build returns them transformed by root)
| Field | Value |
|---|---|
| Spawn | (0, .1, 3.2), faces +z (gatehouse, by the lantern cart) |
| GatehouseSafeBounds | centre (0, 2.5, 3), size (11.4, 5, 5.4) |
| Anchor (chapel bell, `InteractKind.Anchor` interactable) | (12.4, 1.0, 11.2) |
| Compass | (-10, 1.1, 16) over the dining table |
| Voice | (9.0, 4.4, 16.5) portrait hall |
| Print | (-13.52, 4.65, 2.0) bedchamber west wall, quad faces +x (yaw -90 like World) |
| HunterSpawn | (0, -3.2, 21.4) crypt island by the sarcophagus |
| PerchPoints[6] | balcony W (-4.4, 3.2, 21); balcony E (4.4, 3.2, 21); rose-window ledge (0, 5.45, 27.0); library gallery (-10, 5.6, 16); observatory telescope (11.4, 8.6, 26.4); hall rafter (0, 8.1, 19.5) |
| AltarPosition | (13.0, 1.45, 6.5); `Points.AltarAnchor` Transform there |
| RelicPositions[3] | 0 silver blade (13, -2.55, 21) reliquary box; 1 hallowed hilt (10, 8.1, 24) orrery socket; 2 moonstone (12.3, 4.3, 14.4) behind the portrait door |
| DrawbridgeTrigger | Bounds centre (0, 1.2, -6.1), size (4, 2.4, 1.4): far bank end of the lowered bridge |
Extras: `DrawbridgeWinch` (-2.6, 1, 1.4, `InteractKind.Van` interactable named "Drawbridge winch"), `Drawbridge` component, `RuneRingCenter` (0, .02, 19.5) radius 3 + `RuneRingAnchor`, `KnifeOrbitAnchor` (-9.5, 1.4, 5), `StairLoop` (trigger BoxCollider, centre (-9.45, -1.4, 24.1)) + `StairLoopReturn` (-7, -3.1, 25.4), `Moon`, `Lamps`, `WindowLights`, `Doors`, `ServiceDoors`, `SearchPoints` (32), `HidingPlaces` (8 cupboards, same construction as World.Hide), `Materials`.

## Rooms (floor y, x range, z range)
Gatehouse 0, x -6..6, z 0..6 (ceiling 5); Courtyard 0, x -6..6, z 6..14 (open sky); Great hall 0, x -6..6, z 14..28 (ceiling 9; columns x +-3.9; ring centre (0, 19.5) r 3 clear; long table z 25.9; balcony); Dining hall 0, x -14..-6, z 10..22; Kitchen 0, x -14..-6, z 0..10; Chapel 0, x 6..14, z 0..13 (clear height 6.2; altar on two 0.16 steps at x 11.6..13.55); Vestibule/main stair 0, x 6..14, z 13..28; Library gallery 3.2, x -14..-6, z 10..22; Bedchamber 3.2, x -14..-6, z 0..10; Portrait hall 3.2, x 6..14, z 13..28 (portrait closet x 11..13.55, z 13.3..15.5 behind "Portrait door"); Balcony 3.2, U-shape x +-(3..5.7) z 14.3..27.7 and x +-3, z 26..27.7; Observatory 6.4, x 6..14, z 19.4..28 (ceiling 11.4); Crypt -3.2, x -6..6, z 14..28 (island x +-1.8, z 20.7..24.3; blood channel 1.2 m, curbs, bridge x +-0.7 at z 19.5..20.7); Reliquary -3.2, x 6..14, z 16..26; Service stair room -3.2..6.4, x -14..-6, z 22..28. Stairs: service A (crypt to ground, foot x -7.2 climbing -x), service B (ground to upper, foot x -12.9 climbing +x), main 1 (ground to upper, foot z 19.6 climbing +z), main 2 (upper to tower, foot z 26.8 climbing -z). Gate: drawbridge hinge (0, 0, -0.45), leaf 3.2 x 6.0 m, moat z -5.6..-0.45, far bank z < -5.6.

## Doors (21 incl. the gate; service doors 0.9 x 2.0, names start `Service door / `)
Service: Gatehouse kitchen passage, Dining stair door, Hall stair door, Library stair door, Portrait door (secret, to the closet), Crypt stair door. Great (3.0 x 4.5, two 1.5 leaves): Gatehouse great doors (z 6), Hall great doors (z 14), Chapel great doors (x 6, z 9). Rest standard 1.5 x 2.0. `CastleLayout.IsServiceDoor(door)` and `Points.ServiceDoors` are for the hunter AI.

## Art names used (all in `CastleLayout.ArtNames`, sizes in `ArtSizes`; origin = base centre, front +Z at yaw 0)
GateArch, Drawbridge (origin at hinge line, extends -Z), Winch, LanternCart, Buttress, Battlement, DoorFrame, GreatDoorFrame, ServiceDoorFrame, DoorLeaf / GreatDoorLeaf / ServiceDoorLeaf / PortraitDoorLeaf (origin at hinge end, leaf extends +X), LancetWindow, TallLancetWindow, ClerestoryWindow, ObservatoryWindow, RoseWindow (base at sill), ColumnGreat, Column, CryptRib, RafterBeam, Balustrade, Fountain, DeadTree, Candelabra, Pew, Altar, ChapelBell, LongTable, DiningTable, HighBackChair, Throne, Fireplace, Sideboard, KitchenHearth, ButcherBlock, Cauldron, Bookshelf, ReadingTable, Lectern, Portrait, CanopyBed, Orrery, Telescope, Sarcophagus, CryptBridge, ReliquaryBox, Pedestal. Door leaf boxes and the drawbridge leaf have their renderer disabled; the art is the visible leaf.

## Verification
- `python3 tools/check_source.py`: rules tests PASS (123 assertions), then stops: "Resolve/import the pinned Unity Input System package" (fresh worktree has no `Library/`). Also note it only globs `Assets/HollowCurrent/Runtime/*.cs` (not recursive), so it would not compile `Runtime/Castle/` even with packages present.
- Substitute: compiled the three Castle files with Unity's Roslyn against both the macOS player and editor `UnityEngine*.dll` sets (C# 9), with a 6-line shim for `Door`/`Interactable`/`InteractKind` (the real ones need `Game`). Zero errors, zero warnings, both surfaces. Script: scratchpad `cc/build.sh` (not committed).
- NOT verified in engine: nav connectivity, stair headroom, door swing clearance, light cost. No captures taken.

## Requests for the integrator
1. `tools/check_source.py`: use `rglob("*.cs")` (or add `Runtime/Castle`) for the Runtime sources.
2. Merge: `CastleArt.cs` conflicts with hc/art; take theirs.
3. `InteractKind` has no Forge/Bell kind: chapel bell uses `Anchor`, winch uses `Van`, altar is only a position/anchor Transform. Notes use labels "Gate keeper's letter", "Kitchen tally", "Chapel inscription", "Steward's last entry", "Blood Ledger page" (`Case.Read` keys, check they are harmless or replace).
4. Attach `StairLoop` (piece 3) to `Points.StairLoop`; `RuneRing`/`KnifeOrbit` to the anchors; `Floater` to the "Candelabra" objects (each holds its light as child "Candle light").
5. Drawbridge: call `Points.Drawbridge.Lower()` when the win condition is met; walk-through collider is the leaf, parapets move with it. Nav is baked with the bridge raised.
6. Piece 0 may want to use `Points.HunterSpawn` (crypt island) or `PerchPoints[2]`; Presence must accept spawn y of -3.2.
7. `Door.cs` measures swing arc with a 1.5 m box; the 3.0 m great doors are split into two 1.5 m leaves and 0.9 m service leaves get a larger probe than their width. Check `Door.Measure` behaviour in a build.
8. Lights: 45 candelabra points (4 shadowed) + 14 window spots (3 shadowed) + 2 directional. Measure; reduce `Candles`/`Windows` flags if needed.
9. Open issues: no collision walls beyond the far bank (player can wander the bank); outer buttress/battlement art placements are decorative and unverified against real art; Voice/Compass/Print spots are guesses pending the tool reskin.
