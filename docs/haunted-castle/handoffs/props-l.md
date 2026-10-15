# HANDOFF: wave L "props-l" (`hc/props-l`, worktree `.wt/props-l`, from haunted-castle af96343)

Remote branch: `origin/hc/props-l`. No Unity launched; nothing here is verified in a player. No Presence.cs change is needed (nothing edited there).
Checks run: `INPUT_SYSTEM_REFERENCE=<main clone>/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py` PASS; `Blender -b -P tools/castle_verify.py` PASS (101 assets, 0 fails); `python3 tools/check_throwables_scene.py` runs on the current baked scene (38 roots, 0 batching-static); it must be re-run after the integrator's bake, expecting the same 38 (no throwable was added or removed; the three new open books on the hall table are not throwables).

## 1. Dark triangular holes on the Count's forearms and wings

Cause (found by marking texels, `rounds/r13/art/count_underside_texels_marked.jpg`): `tools/count_project_refs.py` filled every UV texel that no reference view saw and whose face points down (normal z < -0.35) with a near-black shadow colour (0.07, 0.035, 0.03). That was written for the throat and jaw, but the raised forearms, the hands, the wing undersides, the coat hem and the feet also face down, so each unseen down-facing UV triangle became a hard-edged, stair-stepped black patch. Looking up at him in the grab (the player's view) shows exactly those faces. 46,480 texels (2.2 %) were affected.
Fix: the black fill is now only for the head and torso; the other regions take the mean of the lit (75th to 97th luma percentile) seen texels of their own region at 72 % to 108 % through a soft noise (script edit, `# Round 13` block). Re-running the projection under Blender 5.2 does not reproduce the committed PNG's colour curve, so I patched the committed texture instead: regenerated, then only the 46,480 changed texels were mapped back into the committed colour space with a per-channel LUT fitted on the unchanged texels (mean at those texels 15 to 77 of 255). `tools/count_source/CountClaude_demon_albedo.png` and `Art/Characters/CountClaude/Maps/Albedo.png` (identical copies, as before) are updated. The FBX and the receipt are untouched, so `count_prepare.py` does not need a rerun.
Integrator: let `CountClaudeImport` re-import the albedo (the material is built from the PNG), then re-capture a grab (`natural1/13`, `huntcam1/14` views). If black patches remain on the forearm flesh slot (submesh 2, `CastleArt.Gore` Flesh, wet 0.6, smoothness 0.85) that would be the second suspect, the glossy wet red on the 1,139 flesh faces; lowering `CountClaudeProfile.GoreFleshWet` is the next lever, I did not touch it.

## 2. `hall_skull_from_east`

The eye (2, 1.64, 12) was in the courtyard (the hall starts at z 14), looking through the door at the dead tree. The skull is at about (-4.55, 0.35, 16.8), in the bay between the piers at z 15.6 and 18.6. New views in `CaptureMode.ArtRun` (props set), both checked against the pier and rib footprints (the sight line crosses x = -3.44 at z 17.7, inside the open bay z 16.05 to 18.15):
- `hall_skull_from_east`: eye (1.6, 1.5, 21.6) -> target (-4.55, 0.4, 16.8)
- `hall_skull_close` (new): eye (-1.2, 1.3, 19.6) -> target (-4.55, 0.4, 16.8)
`hall_west_end_skull` and `crypt_ceiling` are unchanged. The skeleton did not move.

## 3. Books

- Hall table: three open books (`FloatingBook` at 0.9 scale, 0.38 m spread, foxed script pages, three covers) lie on the runner at z -1.0, 0 and +1.0 about the table centre, pages up, not throwable (`SmallProps`). The two closed throwable books and the plates are as before. Camera `hall_table_books` moved closer: eye (1.1, 1.35, 21.0) -> target (0, 0.78, 23.7).
- Floating books in the hall are now closed tomes: new `TomeSmall`, `Tome`, `TomeLarge` (0.19 x 0.22 x 0.06 m, 0.26 x 0.30 x 0.08, 0.36 x 0.40 x 0.11 at asset scale; placed at 2.0, so up to 0.72 x 0.80 x 0.22 m): thick boards, embossed gilt cover, page block with fore-edge lines, banded spine, worn gilt corners, a clasp on the large one; 246 to 270 tris. Same tilt logic (cover turned toward the entrance), same Floater animation, cover mix by `SwapCover`. Proof: `rounds/r13/art/props_proof_tomes.png`.

## 4. Chapel altar sockets

The three tall free-standing niches (each 0.31 m high, the round one a cylinder) read as tins. They are replaced by one `RelicTablet.fbx` (0.88 x 2.04 x 0.12 m, 2,589 tris): a moulded stone slab with a beaded gilt border, four corner roundels, and three low carved bosses (5 cm proud) whose pockets follow the blade, the cross-hilt and the moonstone, with the glyph strokes lying in the pockets. Pocket centres are unchanged (tablet z -0.65, 0, +0.65), the slab tilt, the `SetPiece` tag and the embers stay (embers lowered into the pockets, 0.09 m). `SocketRune` stays: three components on the tablet, one per relic, each with a new `runePrefix` (`RuneBlade`, `RuneHilt`, `RuneMoon`); its renderer lookup moved from Awake to Start so a prefix set after AddComponent is honoured. Fallbacks, in order: the round 12 niches, then the primitives. Proof: `rounds/r13/art/props_proof_tablet.png`. Check S2 for the lip and gilt reading at 6 m on the 8 degree slab.

## 5. Chapel floating candles

`SurrealAssets.CandleMesh` was a 40 cm candle 10 cm thick (the old "taper"); it is now a real taper, 30 cm long, 2.7 cm across at the foot, wick 2 cm. The flame sprite went from 0.22 m (hero 0.34 m) to 0.06 m (hero 0.09 m) because the flame is a child of the scaled candle and its quad is 1.8 flame heights tall, so the old flames were bigger than the candles. The hero candle scale 1.5 -> 1.2; the others keep 0.9 to 1.3. The flame is about twice a real taper flame so it still reads at 6 m; the S2 flame count (56 candles) is unchanged. Not verified against the "well over 30 flames" S2 frame.

## Budget and files

New tris: tablet 2,589 and tomes 3 x about 250 (about +3.3k); the three RelicSocket niches (2.5k) are no longer placed. Materials unchanged (nine r12 ones). New: `RelicTablet`, `TomeSmall`, `Tome`, `TomeLarge` FBX + metas (path-derived guids); the existing FBX were not regenerated (byte-different rebuilds reverted). Builders: `tools/castle_props_r12.py` (`relic_tablet`, `niche_on`, `book_closed(dims=)`). Names added to `CastleArt.Names/All`.

## Risks

- The Count fix is a texture patch; the underside colour is a region average, so the forearm undersides are plain mid-grey skin, not the sheet's detail.
- Per-component SocketRune lookups now happen in Start; a tablet baked into the scene serialises the prefix, so both paths work.
- The tomes are 4 x thicker than the old floating slabs; at scale 2.0 and with the cover turned to the aisle they may need a scale trim after the S1 capture.
