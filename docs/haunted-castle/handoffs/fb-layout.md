# HANDOFF: piece fb-layout (`hc/fb-layout`, worktree `.wt/layout`)

Owner feedback 2026-10-15 items 8 (foyer skeleton), 9 (chapel crowded) and the "too well-lit" half of 12. Castle only; Bellwether behaviour untouched. Unity was not launched (no player build of these changes exists; everything below is verified statically, with the recipe for the integrator at the end). Doors, hides, scares, audio and UI were not touched.

## 1. Foyer skeleton (item 8)

**Cause.** The r12 carcass (3.0 m ribcage, 12.1 m long) lay along the west colonnade with its spine at x -4.58. The west bay between the pier faces (x -4.35) and the wall relief (x -5.83) is 1.5 m wide, so the ribs reached x -3.04 straight over the piers at z 18.6, 21.6 and 24.4: "stuck between three poles". No scale from 0.75 to 0.85 fits a 1.5 m bay, so the "keep the west bay" option is geometrically impossible; the bay is not an option at any scale.

**Fix (centre it).** `CastleLayout.SkeletonPos` = (0, 0.05, 18.4), `SkeletonScaleVec` = (0.6, 0.6, 0.5): fully centred on the hall axis, head toward the great doors, inside the rune ring, between the doors and the banquet table. It is 6.06 m long, ribcage 1.8 m across, 1.7 m high. Plan (see `rounds/r13/art/fb_layout_hall_plan.png`, drawn from the numbers, not a render): x -0.88..0.93, z 15.35..21.41.

| Clearance | Value |
|---|---|
| nearest pier face (x +-3.45) | 2.52 m (needs >= 0.6) |
| table front (z 22.05) | 0.65 m |
| doors (inner face z 14.3) to the skull tip | 1.05 m |

Table, rune ring, throne and rose window stay on x = 0 (round 6 rule kept); the skeleton joins them on the axis. The uniform scale is not uniform on purpose (length 0.5, width/height 0.6) so the ribcage does not read as a barrel at the shorter length. The previous "small rib fragment" critique was about size at scale 0.3 x 0.82; this is 2x-3x that in plan but the owner asked for the size change.

**Walkable footprint.** Before there were no colliders. Now `CastleWorld.SkeletonCollision()` builds 11 BoxColliders (1.8 m high) from `CastleLayout.SkeletonFootprint`: the 0.5 x 0.5 art-unit cells where bone stands between the step offset (0.26 m) and head clearance (1.85 m), generated from the FBX triangles by `tools/castle_skeleton_footprint.py` (`blender -b -P tools/castle_skeleton_footprint.py`, prints the C# initialiser; run it again if scale or art change). The open floor between the tail, limbs and head and all the floor beside the ribcage has no collider. Loose floor bones below 0.26 m are steppable. The runtime nav-mesh builds from these boxes, so the Count walks round the ribcage.

**Prepare-time log** (`CastleWorld.CheckHallAxis`, logs at castle build; an error is logged on failure):
`HC_SKELETON bounds=x[..] z[..] y_max=.. source=renderers centre_x=.. columns_hit=[] min_pier_gap=.. min_bone_to_pier=.. table_gap=.. footprint_boxes=11 (clearance 0.6) PASS`
`HC_HALL_AXIS table centre x=0.000 rune ring centre x=0.000 skeleton centre x=0.02 (limit +-0.15) PASS`
Bounds are the union of the placed renderers' bounds (hall-local); `columns_hit` lists every pier box (and the table) closer than `SkeletonClearance` (0.6 m); the old `SkeletonHalfEast`/`SkeletonMaxX` rule (skeleton west of x -3) is gone on purpose and replaced by "centred within 0.15 m, no pier within 0.6 m". Expected numbers from the layout constants: centre_x 0.02, min_pier_gap 2.52, table_gap 0.64..0.65 (the renderer bounds may add a centimetre or two; the limit is 0.6, so a FAIL means the art grew).

Other edits that follow from the move: search point (0, 17.5) (now inside the ribcage) became (2.4, 17.5); the `-hc-capture-art props` views that aimed at the old skull position were re-aimed at the new one (same ids). `HallPierZ`, `HallPierX`, `HallPierHalf` now live in `CastleLayout` and `GreatHall()`/`Pier()` read them.

## 2. Chapel declutter (item 9)

Real room: x 6.3..13.55, z 0.45..12.7 (wall thicknesses). Before: 3 rows x 2 pews per side (4.9 m of bench per side, 0.5 m outer passages), 4 toppled pews, the blood font standing IN the aisle at (7.55, 6.95) directly between the great doors/the S2 eye and the altar, 56 floating candles from 1.5 m up, and the pew rows started 1.7 m inside the great doors (z 7.5-10.5), so the doors opened onto a bench.

Now (`CastleLayout.ChapelPewRows/ChapelPewZ/ChapelFallenPews/FontPos`, `CastleWorld.Chapel()`):
- Two pew rows (x 9.1 and 10.4), one 2.4 m pew per side per row: 2 standing + 2 fallen (<= 2 required). Both fallen pews lean toward the altar, away from the aisle and the door paths (yaw 88/93, roll 76/70).
- Centre aisle 2.36 m (z 5.32-7.67; need >= 2.0), side passages 2.43 m (south) and 2.57 m (north) (need >= 1.2). The 2.45 m between the doors and the first pew (x 6.3-8.75) is open floor.
- The blood font moved to the south-east corner against the south wall at (12.4, 0, 1.05) (footprint x 11.88-12.92, z 0.53-1.57), lit by the south window at x 11.5, clear of both door paths, the aisle and the altar steps (z 4-9). Its collision moved with it (`Prop` proxy). The floating-candle exclusion round the font follows `FontPos`.
- Floating candles: 46 instead of 56 (37 in the S2 cone instead of 41), none below 2.0 m (they hung at eye height and read as clutter in the face). S2 still holds well over 30 flames. No loose books or floor tapers exist in the chapel in this build (checked: no throwables placed in x 6..14, z 0..13); the only floor stand is the candelabrum at (8.0, 0, 1.2) by the south wall, which has no collider.
- Altar approach, relic sockets (SetPiece `altar_sockets` S2, eye (7, 1.64, 6.5) down the aisle) and the confessional (7.3, 0, 12.7; front at z 11.9, reached through the open north passage) are untouched and clear. Search points (9.5, 6.5) and (7.2, 6.5) are in the aisle (the latter used to be inside the font).
- Prepare-time log: `HC_CHAPEL aisle=2.36 (min 2.0) passage_south=2.43 passage_north=2.57 (min 1.2) pews_standing=2 pews_fallen=2 font=(12.40,1.05) font_outside_aisle=True PASS` (computed from the same data the pews are built from; an error is logged on failure).
- Observation, NOT changed (gameplay anchors): the chapel bell (AnchorLocal 12.4, 1.0, 11.2) and the brazier (BrazierPos 12.6, 0, 11.2) overlap each other in the north-east corner (their solid proxies share about 0.6 m in x). Both are interactables the proofs drive via `Find(InteractKind...)`. If the owner still feels cramped there, move the brazier; the position is only read by CastleWorld and `Game` takes the interactable's transform.

**NavMesh.** The castle nav-mesh is built at runtime from the BoxColliders (`CastleWorld.BuildNavigation` / `World.BuildNavigation`); there is no baked nav asset. What changes for the Count: the hall (skeleton footprint), the chapel (pew grid, font) and search points. The integrator needs a normal Prepare so the scene holds the new colliders; no separate navmesh bake file exists. Hall: the Count can path round the ribcage (2.5 m clear each side); chapel: the 2.36 m aisle and both outer passages are open.

## 3. Darker castle (item 12, lighting half)

The castle is now dark and lantern-led like Bellwether. All values are fields (`CastleLightTuning`, `CastleAtmosphere`); old values are in the comments.

| What | Was | Now |
|---|---|---|
| Fog (hall, gatehouse, courtyard) | colour (.30,.37,.52), exp2 density .05 | colour (.04,.05,.08), density .03 |
| Fog (outside the gate, z < 0.5; new `ExteriorFogProfile`) | same as hall | keeps the old mist (.30,.37,.52 at .05) so the bridge/facade views are not blacked out |
| Fog (crypt) | (.20,.18,.22) at .03 | unchanged |
| Runtime ambient trilight sky / equator / ground | (.16,.20,.30) / (.095,.12,.185) / (.04,.04,.06), x1 | (.10,.125,.19) / (.055,.07,.11) / (.025,.027,.04), x0.75 |
| Bake ambient (`HouseLighting`, Castle) | (.050,.065,.100) / (.022,.028,.040) / (.010,.012,.016) | x0.68 of those |
| Shader fill sky / ground (linear) | (.010,.014,.024) / (.008,.010,.018) | (.005,.007,.012) / (.004,.005,.009) |
| Hall ground-fog layers (colour, alpha) | (.34,.42,.58), .26 | (.12,.15,.21), .15 (crypt unchanged) |
| Vignette | .22 | .30 |
| `CandleIntensityScale` | .82 | .53 (-35 %) |
| Window moonlight floor / rose / crypt | .80 / 1.10 / .43 | .48 / .66 / .34 (-40 / -40 / -20 %) |
| Moon directional / second moon | .16 / .05 | .10 / .035 |
| Hall vault fill | .32 | .10 (+ vault emission (.035,.045,.075) -> (.014,.018,.032)) |
| Courtyard fills / balcony fills | 2.6 / 1.4 | 1.3 / .7 |
| Chapel candle fills | 1.0 | .6 (chapel sun-window moon .95 vs 1.1: still the landmark) |
| Crypt candle pool / cold fill | .85 / .55 | .6 / .42 (rune glow .9 and blood glow 1.2 kept: crypt landmarks) |
| Lantern (Investigator torch) | .85 x Brightness, range 20, 52 deg | x `TorchIntensityScale` 1.35 in the castle (1.15 absolute; stored as `Investigator.TorchScale`, Bellwether stays 1.0); `TorchRange` 0 = keep 20 |

**Cannot be verified here.** No capture exists for these values (no Unity run). From the r12 captures (S1 12 % near-black at mean luma 41, S2 22 %, S3 22 %) a pure linear scale to 35-55 % in S1 needs about 0.12-0.18 of the light, S2 about 0.35-0.5 and S3 about 0.35-0.5, which is why S1 (the brightest view) took the larger cuts (moon, vault, ambient, fog colour and floor mist together). The numbers above are an estimate; the first capture will say how far off. Tune in this order if S1 is off: `CandleIntensityScale`, `AmbientIntensity`/`FogDensity`, `WindowMoonlight*`, `VaultFillIntensity`; if the lantern-on state is too dark or too bright: `TorchIntensityScale`, then `TorchRange`.

**Metric (existing tool, extended).** `tools/exposure_report.py` already measures near-black = luma < 8 of 255 per PNG. New: `python3 tools/exposure_report.py <dir> --lantern off|on` judges S1/S2/S3/`art_*` against 35-55 % (aim 45) for off and 12-25 % (aim 18) for on; other views print `n/a`. The capture flag `-hc-capture-lantern off|on` (`CaptureMode.LanternOn`, default off = the old dark-room behaviour) lights the grave-ash lantern (with the castle's `TorchScale`) for every world shot and the art views; the log line `HC_CAPTURE lantern on torchScale=1.35 intensity=1.15 range=20.0` records it. S5/S6/hunt frames always had the lantern on.

## How verified

- `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py`: PASS (editor and player assemblies compile; the one known harmless CS0649 batch).
- `python3 tools/check_castle_scares.py`: PASS. `python3 tools/check_throwables_scene.py`: exit 0 (roots 38, no batching). `python3 tools/check_audio_keys.py`: OK.
- Skeleton clearances recomputed by hand from the Blender-measured art extents (x -1.469..1.543, y -6.010..6.103, z up to 2.827) and checked against the pier boxes, table and chairs; the footprint was drawn into a plan (`rounds/r13/art/fb_layout_hall_plan.png`).
- Chapel clearances recomputed from the same data the log reads (aisle 2.36, passages 2.43 / 2.57).
- No engine launch, no bake, no GPU use.

## What the integrator must run

1. **Prepare + bake** the castle (`HouseLighting.PrepareAndBakeCastle`, i.e. `NativeBuild.PrepareCastle` then bake): the skeleton and chapel are scene content, and the window spots, both moons and the ambient are baked, so lighting, light probes and the reflection probes (17) must all be re-baked; the receipts' source hash changes anyway. Nav-mesh: built at runtime from the colliders, nothing to bake separately. `check_throwables_scene.py` after Prepare (batching of the Throwable roots).
2. Read the Prepare log: `HC_SKELETON ... columns_hit=[] ... PASS`, `HC_HALL_AXIS ... PASS`, `HC_CHAPEL ... PASS`, `HC_CASTLE_LIGHT_TUNING`.
3. Captures (GPU slot, windowed; each run closes itself):
   - **before**: the r12 captures (`rounds/r12/critic/cap_rooms`, lantern off) are the baseline for S1-S3 (12.3 / 21.7 / 22.1 % near-black); the old build `e80fa28` has no `-hc-capture-art layout` set, so the layout views have no before.
   - **after**, two runs of the new build: `-hc-capture <dirOff> -hc-capture-shots S1,S2,S3 -hc-capture-lantern off` and `... <dirOn> ... -hc-capture-lantern on`; then `-hc-capture <dirArtOff> -hc-capture-art layout -hc-capture-lantern off` (and `on`). `python3 tools/exposure_report.py <dir> --lantern off|on` per folder. Target: S1/S2/S3 + art views 35-55 % off, 12-25 % on; the lantern must read the room 6-8 m ahead (S1 `hall_from_doors` on) and barely read it off.
   - Look at: `art_layout_hall_from_doors`, `..._hall_skeleton_side_west/east` (no column touching the ribs), `..._hall_skeleton_head` (skull 1 m inside the doors), `..._hall_pier_gap`, `..._chapel_from_great_doors` (open floor, then the aisle), `..._chapel_from_guard_door`, `..._chapel_aisle` (no font in front of the S2 eye), `..._chapel_font_corner`, `..._chapel_confessional`, `..._chapel_altar_approach`, and S2 (moon window still the landmark). The old `-hc-capture-art props` views `hall_skull_*` were re-aimed at the new skull.
4. Proofs that touch the changed rooms: the playtest driver's `Use(Find(Breaker))`/altar/bell steps (unchanged positions) and a `verify` run; check no stuck-path warnings in the hall (skeleton footprint) and chapel.

## Risks

- **Lighting numbers are an estimate** (see above): expect one or two tuning iterations after the first capture; every knob is a public field.
- The baked ambient and window/moon values only change after the re-bake; until then the realtime candles/fills/fog/ambient are darker over the old lightmaps.
- A 0.5-scale skeleton in front of the doors is the first thing seen from the doorway (S1 eye 0.65 m from the skull tip); the S1 composition and the r12 "small rib fragment" critique may be re-raised, but the owner explicitly asked for the size change.
- The darker castle lowers every light-dependent reading (the "lit" candle check keys on intensity > 0.05, unchanged); the lantern-off dread and torch-failure scares become more severe. Dispatch/UI are unchanged.
- The bell/brazier overlap in the north-east chapel corner remains (see section 2).
