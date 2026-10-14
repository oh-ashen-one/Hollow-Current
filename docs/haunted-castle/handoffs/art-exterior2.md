# HANDOFF: art-exterior2 (`hc/art-exterior2`, worktree `.wt/art-exterior2`, from `haunted-castle` 3d48761)

Polish pass on the merged exterior kit (d904e77), code only: no Unity launched. Proofs: `docs/haunted-castle/rounds/r12/art/exterior2_proof_*.png` and `exterior2_proof_report.json` (Blender Workbench over the real sky, engine window maths; silhouette, window texture, moons and clearance, not final lighting). Regenerate: `Blender -b -P tools/castle_exterior.py -- --proof docs/haunted-castle/rounds/r12/art` then `python3 tools/castle_exterior_composite.py` (add `--moons-only` for just the ray test, `--debug-hits` to print where a ray lands).

## 1. Windows: texture, not a flat fill
- `tools/castle_exterior_atlas.py` (new, numpy only) draws one 1024x1024 sRGB atlas, `Resources/Castle/Textures/Castle_ExtWindows.png` (+meta): four arrow-slit variants (different brightness and cross-bar position), a leaded lancet (diamond leading, mullion, transoms, frame, radial falloff, low-frequency glass mottling) and the gate tympanum. Glass runs `#d9933a` at the core to `#5a2e0a` at the edges, leading `#1a0c04`; no yellow.
- The gate tympanum is now a dim lit gatehouse seen through the hood: coursed stone, two pointed vault ribs, two torch pools and a deep amber passage glow low in the middle, falling to near black at the top. The portcullis bars silhouette against it.
- New shader `Resources/Castle/Shaders/CastleWindow.shader` (+meta): unlit, opaque, texel times `_Intensity` (1.5, so the hottest linear channel is about 1.0 and nothing can exceed 2.0). Material `Castle exterior windows` replaces the flat `Castle_ExtGlow` fill. Each window is one UV-mapped, one-sided polygon proud of the wall by 3 cm (`window()` in `castle_exterior.py`).

## 2. Stars (now in scope)
The star discs came from the skybox itself: `tools/castle_sky.py` -> `Resources/Castle/Sky/Sky_*.png`. `make_stars`/`paint_stars` were rewritten: same seed and draw order (positions and tints unchanged), disc radius 0.03 to 0.08 deg (never thinner than half a texel; dimmed by the lost area instead), soft 1 px anti-aliased edge, no halo, and the 20 brightest at 0.12 deg with a faint 1 px cross glint. Moons, aurora, clouds, bats (not part of the sky), the colour gradient and the dither are untouched: inside the pale-moon disc the regenerated Sky_Up differs from the old face by 0 in every channel. Median star blob area on the Up face went from 108 px to 5 px. Faces, `SkyCastle.mat` and `art/sky_preview.jpg` regenerated and committed (`python3 tools/castle_sky.py`, 45 s). Proof: `exterior2_proof_sky_before_after.png` (1920x1080 window of the Up face around the pale moon, old left, new right; the window is clamped to the face because the moon sits 430 px from its edge). The exterior2 proofs were re-rendered over the new sky.

## 3. Moon clearance (proved offline)
`castle_exterior_proof.py` casts 127 rays through each moon disc (pale az -34 el 55, radius 11.5 deg incl. 0.5 margin, measured from the sky texture; red az 42 el 42, radius 4.8 deg) from five eyes, against the existing massing alone and against massing plus kit. Percent of the disc occluded:

| eye | pale existing / with kit | red existing / with kit |
|---|---|---|
| S4 (0,2.5,-14) | 0 / 0 | 0 / 0 |
| bridge end (0,1.7,-6.1) (bridge_keep capture) | 0 / 0 | 5.5 / 5.5 |
| courtyard capture eye (0,1.64,7) (courtyard_keep capture) | 3.1 / 3.1 | 20.5 / 20.5 |
| bridge midpoint (0,1.7,-3.45) | 63 / 63 | 100 / 100 |
| courtyard centre (0,1.64,10) | 69 / 69 | 100 / 100 |

The kit now adds 0.0 percent at all five eyes (it added 22 to 86 percent before). Everything left in the table is the existing shell: the gate wall (y 7.5 at z 0) hides most of both moons from the bridge midpoint, and the hall front wall (y 9.2 at z 14) hides them from z 10 in the courtyard, so full clearance from those two exact points is not reachable with a kit-only change. S4, the bridge end and the courtyard capture eye are clear apart from the existing 3 to 20 percent.
What moved: the keep is now 5.6 m square and off-axis, centre (2.3, 20.4), so its silhouette sits between the moons from the courtyard; bartizans and spires are slimmer; drum towers moved out to x +-8.6 (z -2.2, radius 1.7, top 9.4, no flag poles, shallower crown); the gate wall lost its merlons, corbel table and projecting banner rods (anything above or in front of the 7.5 m wall line cuts into the rising moon rays; the hood is lower, apex 6.4, and a flush crown course remains); the observatory drum moved to (11.4, 25.4) radius 2.4; the hall-front merlons and the outer pilasters went. `HC_EXTERIOR_CLEAR` also passes: 0 vertices in any room, in the standing-spot box (x 3.0 to 4.2) or in the bridge corridor.

## 4. FBX axis (the `HC_EXTERIOR_AXIS ... z -19.5` bake log)
Cause: Blender's FBX export (`axis_forward=-Z, axis_up=Y`) plus Unity's importer map Blender (x, y, z) to Unity (-x, z, -y), a 180 degree turn about the vertical axis. The kit authored Blender (x, z, y), so the pieces landed turned by 180 degrees and `AxisCheck` rotated the group back. Fix in the export: `U()` in `castle_exterior.py` now writes Blender (-x, -z, y) and the poly/size helpers follow, so pieces land exactly where they are authored. Re-import check in Blender: ExtKeep centre (-2.3, -20.4) which is Unity (2.3, 20.4); ExtGate centre z -2.2. `CastleExterior.AxisCheck` is kept (expects keep centre z > 0; it now logs `HC_EXTERIOR_AXIS ok keep centre local (2.3, ~21, 20.4)`).
Note for everyone else: every other `Resources/Castle/*.fbx` is exported with the old assumption, so their authored "front +Y" lands on Unity -Z at yaw 0 (the drawbridge leaf fix in `BuildGate` is the same effect). I did not touch them.

## 5. Stone
The pieces already used the veneer's own world-space stone (same surfaces and tints), so the difference in the courtyard capture is lighting: the courtyard walls sit in the realtime warm "Courtyard candle fill" points, the keep only gets the dim baked moon (0.16) and the cool hemispheric fill (0.010 to 0.024). Fix without new lights: the keep, towers, gate and rock now use two exterior stone instances (`Castle exterior stone`, `Castle exterior stone dark`: veneer tints, `_FillAmount` 3.0 on CastleTriplanar, which multiplies albedo times the cool fill); expect a dim cool blue-grey, not tan. The scene already has the directional moon and a second moon, so no directional fill was requested; I did not add one because a baked directional would also reach the room interiors through the window openings. If you still want more rim, raise `StoneFill` in `CastleExterior.cs` (3.0 now) or `MoonDirectional` in `CastleLightTuning` and re-bake; a baked directional is the next step and needs an interior recheck.

## 6. Brightness
- Backdrop values are display colours (the project is Linear, `SetColor` takes them as typed): the first pass at .04 to .09 rendered 10 to 15/255. Now far ridge low (.20,.25,.38) to crest (.085,.105,.19), near ridge (.10,.13,.21) to (.04,.05,.095), mist band (.20,.26,.40) alpha .40.
- Fog: existing bands raised (alpha .34 / .24) and eight far-bank and flank mist quads added (two thin layers each over the far bank, both flanks and the rear bank).
- Estimate for `battlement_out`: the proof (same sky, new ridge, no fog, a stand-in bank colour) goes 22.6 to 30.7 mean from the old to the new backdrop (+8); the engine capture was 12.3, so the ridge alone gives about 20, and the far-bank mist (about 0.26 effective alpha of (.34,.42,.58) over roughly a third of the frame) adds a further 6 to 8: expect 26 to 30 in the capture. Please verify; if it is under 25 raise `FogMaterial` alphas or the ridge lows.
- `gate_out_S6` (10.9) cannot move with exterior values: in the engine capture the raised drawbridge leaf closes the gate opening, so the frame is the gatehouse interior and no exterior pixel is visible. My proof for that view uses the lowered bridge (mean 55, exterior visible). To judge the exterior from that eye, capture it with the leaf lowered (`spots.Drawbridge.Snap(true)` as the S4 shot does).

## Budget
Triangles 20,776 in FBX (was 22,464; windows are single polygons now) + 32 fog quads x 2 = about 20.9k of 150k. New materials 6: windows, backdrop, fog low, fog high, exterior stone, exterior stone dark (the old flat glow is gone). Colliders 0, lights 0. Backdrop and fog stay Transparent-queue, outside the bake. 

## Integrator
1. Merge, let Unity import `Castle_ExtWindows.png`, `CastleWindow.shader` and the re-exported `Ext*.fbx` (metas committed; the six FBX metas are unchanged), regenerate the castle scene, re-bake.
2. Log: `HC_EXTERIOR_AXIS ok keep centre local (2.3, ..., 20.4)` with no 180 degree warning; `HC_EXTERIOR_KIT ... renderers=...` (six FBX parts + 32 fog quads = 38 renderers); no `HC_MISSING_ART`; no `[CastleExterior] ... unavailable` warnings.
3. Recapture the art exterior set plus S4. Look for: window texture and tympanum falloff (`bridge_gate`), both moons clear (`courtyard_keep`, `bridge_keep`, S4), keep stone readable (`courtyard_keep`), mean brightness of `battlement_out`.
4. Risks not seen in Unity: `CastleWindow` compile (trivial), the `_FillAmount` boost on stone also lifting the rock a lot (rock uses the dark instance), far-bank mist alpha, overdraw of 32 transparent quads (the far ones are large; if frame time moves, drop the four far-bank pairs).
