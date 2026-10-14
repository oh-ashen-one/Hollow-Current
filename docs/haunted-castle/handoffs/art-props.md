# HANDOFF: piece "art-props" (`hc/art-props`, worktree `.wt/art-props`)

Remote branch: `origin/hc/art-props` (branched from `haunted-castle` b428a8e). Castle only; Bellwether, `~/Hollow-Current` and the exterior sibling (`hc/art-exterior`) untouched. Unity was not launched.

Findings addressed (r11 blind critic §visual, r10 `CRITIC_BLIND.md`): hand-roots "glove cards on sticks", ceiling hands "tan cubes with stick fingers", floating books "boxes", relic sockets "untextured primitives", giant skeleton "a small rib fragment".

## What changed

| Family | Art (`Resources/Castle/*.fbx`, built by `tools/castle_props_r12.py`) | Code | Tris each |
|---|---|---|---|
| Hand-roots | `HandRoot0..5` (six variants, strand lengths 1.0, 1.3, 1.6, 1.9, 2.2, 1.45 m = `SurrealProps.HandRootLengths`), `RootCrown` (1 m collar, scaled to the ceiling distance) | `SurrealProps.MakeHandRoot` places `HandRoot<seed mod 6>` and `RootCrown`; the old procedural mesh is the fallback when the FBX is a placeholder. Placement points, the Floater sway and `HandRoots()` untouched | 3.79k to 3.94k each (budget 4k), crown 672 |
| Floating books | `FloatingBookSmall` (0.30 m), `FloatingBook` (0.42 m), `FloatingBookLarge` (0.55 m); closed `BookClosedSmall/BookClosed/BookClosedLarge` (0.16/0.22/0.34 m) | `CastleWorld.HallDressing` picks size `i % 3`, swaps the cover (`SwapCover`, three covers), scale 1.9, the same tilt and `Floater` animation. `MakeThrowable(Small.Book)` places a closed book under the same `Throwable` root (cover and size cycle); the four-box book is the fallback | open 310 to 342, closed 246 to 270 |
| Relic sockets | `RelicSocketBlade/Hilt/Moon` (carved stone niche: foot, shaft, cornice lip, sloped pocket in the relic's outline, worn gilt lip and fillet, `Rune` part) | `CastleWorld.AltarInlay` places them on the same slab, same centres, same tilt, `SetPiece` tag and ember flames; the primitives are the fallback. New `Runtime/SocketRune.cs` (+ `.meta`) lights the `Rune` part once the relic is found and the player is at the altar (or the sword is forged); `RelicAltPositions` and interaction points untouched | 370, 596, 1592 |
| Giant skeleton | `GiantSkeleton` rebuilt: 12.1 m, ribcage 3.0 m across (x -1.47 to +1.54 about the spine), 2.83 m high, 17 rib pairs, 48 vertebrae with neural spines, scapulae, pelvis, four limbs, skull yawed 40 deg with jaw open and sunk 0.16 m below the floor, loose bone | `CastleLayout`: `SkeletonPos` x -4.58, scale 1 (was 0.30 x 0.82 x 0.90), new `SkeletonHalfEast` 1.55; `CastleWorld.CheckHallAxis` uses it. East rib tips end at x -3.04 (limit -3.0), spine clears the pier faces (x -4.35), top at y 2.88 under the 3.0 m gallery slab, no colliders (walkable paths and the hall tables untouched) | 12,404 (budget 40k) |

New materials (9, budget 10), all `Castle_*` so `CastleArt.ApplyMaterials` swaps them like the other props, textures by `tools/castle_r12_textures.py` (albedo, normal, mask, 1024 px): `Castle_BookLeatherA` (oxblood, gilt frame and moon), `B` (near-black, blind-tooled rune ring), `C` (bottle green, cartouche and cross), `Castle_BookPages` (foxed script, fore-edge band), `Castle_GiltWorn`, `Castle_RootBark`, `Castle_RootFlesh`, `Castle_BoneGrime`, `Castle_NicheStone`. `Castle_RuneGlow` (existing unlit CastleEmissive) is the rune. Declared in `castle_common.MATERIALS` and `CastleArt.Specs` (Standard fallback). The generated textures are new files; no existing texture was regenerated.

Retired: `GiantSkeleton` and `FloatingBook` builders were removed from `castle_props.py` BUILDERS (functions kept for history) and their entries from `manifest_props.json`; `castle_build_all.sh` now also runs the two new scripts. `castle_verify.py` allows manifest budgets above 3000 tris and checks the skeleton (length, 2.9 to 3.1 m span, 40k tris).

## CustomUse and cost

One paid job, one at a time: Tripo v3.1 single-image mesh of a hand-root crop from `refs/crypt.png` (workflow `a895a933-7b8e-454e-864e-6bffd8dba22a`, node `7c1895e2-bafb-4aa6-8b51-0f1c755352da`). Balance 4486 to 4386: **100 credits**. Reviewed in an offline render: fingers fused into a paddle, detached thumb, flat-cut wrist, no separable fingers to decimate. Rejected, not committed, no retry (the brief allows procedural sculpting). Everything shipped is procedural. Details and licence note in `ASSET_LOG.md` and `ASSET_LICENSES.md`. No Higgsfield or other spend.

## Budget used

Gross new triangles when placed: hand-roots 30 x about 3.87k = 116k, collars 30 x 672 = 20k, skeleton 12.4k, sockets 2.6k, hall books at most 10 x 330 = 3.3k, throwable books 6 x 250 = 1.5k: about 155k. Removed: old procedural hand-roots about 16k, old skeleton 4.9k, old books about 1k. Net about **+133k** (limit 200k). Materials +9. Draw calls: hand-roots and collars cast no shadows (`ShadowCastingMode.Off`, as the old ones); each strand is one mesh with two materials.

## Proof (offline, no engine)

`docs/haunted-castle/rounds/r12/art/props_proof_{handroots,books,sockets,skeleton}.png`: Blender Cycles on CPU through `tools/props_proof.py` (warm key, cold fill, rim, neutral grey backdrop, the same textures the player gets; the skeleton shot carries a 1.8 m scale figure). `tools/props_bounds.py` prints FBX extremes. `python3 tools/castle_r12_textures.py` then `Blender -b -P tools/castle_props_r12.py` rebuild everything; `Blender -b -P tools/castle_verify.py` passes (91 assets, 0 fails); `INPUT_SYSTEM_REFERENCE=<main clone>/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py` passes.

## Integrator: bake and capture

1. Let Unity import the new FBX (`.fbx.meta` files are committed, cloned from `Pew.fbx.meta`; commit any import changes), the nine materials and textures, and `SocketRune.cs` (meta committed). Re-bake the castle scene: `HandRoots`, `AltarInlay`, `HallDressing` and `SmallProps` output changed.
2. Capture S1 (books, skeleton), S2 (sockets; they stand on the 8 degree tilted slab, so check the lip and the gilt read at 6 m), S3 (hands).
3. Log checks: `HC_HALL_AXIS` must print `skeleton max x analytic=-3.03 measured<=-3.00` with no error; no `[CastleArt] missing`; no `HC_THROWABLE_BOUNDS_INVALID` (closed books are at most 0.34 x 0.21 x 0.06 m); `HC_MISSING_ART` 0.
4. Light the runes in a playtest: pick up a relic, stand at the altar (`Forge.AtAltar`), the matching socket rune should glow; it stays lit until the relics reset.
5. Perf: great-hall frame time (previous p99 11.5 ms) and the crypt (about 136k tris of hand-roots in view at S3).

## Risks and not done

- **S1 will show only part of the skeleton.** The capture eye (0, 1.64, 14.7) is at x = 0 and the owner's r6 rule keeps the skeleton at x <= -3 behind the pier line, so it is seen between piers at the left edge: ribs and the spine, not the skull (the skull is at z about 16.8, 65 degrees off axis). It is now full size and 2.8 m tall, but a "skeleton at 30 % of frame width" read needs an owner decision, for example moving it across the far end (the ref) or changing the S1 camera. I did not move it out of the west strip.
- Hand-roots are still 30 x about 3.9k tris; if the crypt frame time is poor, cut the dead rootlets (about 400 tris per hand-root) or place fewer strands, before touching the hands.
- The hands read as knuckled long fingers merging into rope roots, but they are cleaner than the ref's gnarled trunks; a second pass could add a thicker, branching trunk at the ceiling (the collar is a bundle of seven roots).
- Rune glyphs are angular futhark-like strokes, not copied from the existing `RuneRing` atlas.
- Floating books share a ribbon and corner caps in the leather of the FBX's default cover; the `SwapCover` pass changes every `Castle_BookLeather*` slot together.
- The skull's eye sockets are flat black discs inside the bone rings (no boolean cavity), fine at hall distance.
- Not verified in a player: FBX import orientation, material remaps, SocketRune timing, Floater sway with the new meshes.
