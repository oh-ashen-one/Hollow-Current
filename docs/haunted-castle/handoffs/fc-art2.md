# HANDOFF: piece fc-art2 (`hc/fc-art2`, worktree `.wt/fc-art2`)

Wave FC art leftovers, from `haunted-castle` f00e343. Model: Sonnet 5.5. Date 2026-10-18. Code only. **Unity was not launched** (the engine was owned by a critic run): everything is verified by static checks, offline geometry runs and a Python mirror of the prepare checks, with the recipe for the integrator at the end. Four commits, one per item, plus one for capture views.

| Commit | Item |
|---|---|
| `dd149d4` | A compass dial colour |
| `ac2eb00` | B field radio |
| `8fce81f` | C telescope aim |
| `2692927` | D kitchen hearth |
| `28b9fc4` | `kit` capture set gains `kit_telescope`, `kit_hearth` |

Static checks (run before every commit, all PASS): `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py` (1209 assertions, runtime + editor compile; only the known CS0649/CS0162 warnings), `tools/check_castle_scares.py`, `tools/check_audio_keys.py`, `tools/check_throwables_scene.py` (rc 0), `tools/check_castle_kit.py --simulate-fix` (OK, `problems=0`, runs on the unchanged baked scene).

## A. Compass dial colour (`Runtime/Castle/CastleInstrumentArt.cs`)

- **Evidence.** `rounds/r13/captures/fc/fc3/r3/instruments_on/art_instruments_held_compass_{idle,reading5}.png`: the dial is a flat neutral grey (pixels about 106,105,104), the needle is grey-blue, the lit ember pips are salmon rather than orange (compare fc2, where the same pips are pure orange). A bluish-white translucent veil lies over the whole face, so the parchment colour never reached the screen whatever the tint. The 12 o'clock mark also sits at the bottom of the face.
- **Findings.**
  1. The only layer between the camera and the dial was the glass dome (second `CastleSprite` submesh with a glare texture): prime suspect. It is **removed**; the glass is now a thin opaque rim band just inside the case lip (`Lathe(2, ...)` with the same 5 profile points, all joints smooth). The compass body mesh is 1135 vertices before and after (measured offline with the real `Mb` code), so `compass=1277` in `HC_INSTRUMENTS` is unchanged. The rim material is the unlit `CastleEmissive` shader at a dim pale grey-blue.
  2. The planar dial uv (`Mb.Lathe`, axis +z) runs u toward mesh -x and v toward mesh -y: the texture lands turned 180 degrees (that is why the red mark was at the bottom). The texture is now laid out in the face's own frame (`fx = -tx, fy = -ty`), so the mark is at the top and numerals read upright.
- **Change.** Dial texture: warm ivory (.88,.79,.57) with stain noise, edge darkening, rust-brown foxing (patches plus specks), umber ink (.16,.095,.055) for the ring, 24 ticks and new numerals 3 / 6 / 9 (3x5 bitmap glyphs), oxblood 12 o'clock mark. Tick ranges shortened so the numerals sit between the inner ring and the ticks. Unlit tint `DialLevel` .46 -> .68 (`tint = (L, .97L, .93L)`): a Python port of the texture shows an on-screen dial of about 137,119,82 (dim warm ivory). Needle: unlit dark oxblood tip + umber tail (`CastleEmissive`, same mesh, so independent of lighting). Removed `GlareTexture`.
- **How verified statically.** Compile/checks above; offline mesh run (vertex count identical); `scratchpad` Python port rendered `dial_preview.png` (numerals upright, foxing moderate, ring and ticks read).
- **NOT verified:** that the veil really came from the dome. No engine run was possible. The new `HC_COMPASS_DIAL dial_shader=<name> rim_shader=<name> dial_level=0.68 glass_dome=False` line (printed when the castle's instruments are built) shows which shader the dial material actually got: it must read `HollowCurrent/CastleSprite` (if it reads `Standard`, `Resources.Load` of the sprite shader is failing in the player and the dial is lit and dim, not unlit). If the compass capture still shows a grey face, the next suspects are that shader fallback and a layer-30 post effect; the tint maths cannot produce grey from this texture.

## B. Field radio, castle only (`CastleInstrumentArt.cs`, `Investigator.cs`, `CaptureMode.cs`)

- **Decision: replaced, not hidden.** The castle does use the radio: R (`input.Radio`) raises it, plays the `radio` cue and calls `Game.RadioHint`, which in the castle gives text hints (`CastleHint`), or static (`Book.RadioInterference`) during interference or half the time in Warning/hunt; it also calls `Creature.Hear`. The dispatch *voice* is gone (`BellwetherSpeak` returns in the castle), the call and the UI hint ("R radio") are not.
- **Change.** New `CastleInstrumentArt.CreateRadio` / `RadioGeometry`: a "resonance box": walnut body with a runtime fibre texture, two brass straps, an inset dark lid panel, a round brass-rimmed grille with five brass slats on the front, a glowing tuning pip, a brass lid knob and a flared brass horn (quadratic sweep, dark mouth) that rises from the lid. 1105 vertices (measured offline). `Mb` gained `Quad` and `Box` (hard-edged, 24 vertices per box). `Investigator.BuildInstruments` builds it only when `period` (castle); otherwise the identical `FieldKitArt.Create(camera,"FieldRadio",...)` call. The glove attaches as before (lower right corner, same offsets as the compass). `HC_INSTRUMENTS` moved below the radio creation and now ends `... lantern=N radio=1105 (max 3000)`; it still errors if any count is 0 or over budget.
- **Bellwether untouched.** `HeldAssetName` still returns `FieldRadio` when `radioPose>0` in both maps; the Bellwether branch is the unchanged `FieldKitArt.Create` call, so `NativeVerification.cs:391` ("FieldRadio", more than 1000 vertices) is on the same mesh. `Investigator.cs` is a shared file, so its source hash changes: **both maps need a re-bake** (see integrator list).
- **Capture.** `-hc-capture-art instruments` gains `art_instruments_held_radio` (`Investigator.CaptureHeld(4, ...)` raises the radio pose: expect `HC_CAPTURE_HELD tool=4 asset=FieldRadio verts=1105`).
- **Offline check.** Built the geometry with the real `Mb` code under Unity's Mono/Roslyn and rendered the OBJ in headless Blender: reads as a box radio with a gramophone horn, no inside-out faces (the builder derives winding from normals).

## C. Telescope aim (`CastleLayout.cs`, `CastleWorld.cs`, `tools/check_castle_kit.py`)

- **Geometry facts** (from `tools/castle_furniture.py telescope()` and the `CastleKitAxis` bounds): the tube rises at **30 degrees** (`Rx(60)` of the tube), pivot 1.15 m above the base, eyepiece 0.62 m behind the origin, objective rim 1.96 m ahead; AsImported, so at yaw 0 it runs toward +Z. The old pose (11.4, 27.0, yaw 180) pointed it into the room with the eyepiece end in the north wall.
- **New pose.** `CastleLayout.TelescopePos` (11.25, 6.4, 24.8), `TelescopeYaw` 8. It stands in the 1.6 m strip between the orrery (x <= 10.5) and the stair opening (x >= 12.1, `hole2`, whose rail runs along x 12.1), 2.75 m short of the north wall, turned 8 degrees toward the north window at x 12 (opening x 11.3..12.7, sill .9, head 3.6, wall faces z 27.55 / 28.45). Sight line: x 11.64 at 2.75 m height at the wall's inner face, x 11.76 at 3.28 m at the outer face, so it looks out through the opening at 30 degrees, clear of the posts and the lintel (margin .15). I checked the east windows too: the stair opening blocks z 22.3..26.8 and the exit from the stair would be blocked at z 21.
- **Collision.** The telescope is placed with `solid:false` and a custom proxy on the tripod and eyepiece end only (0.92 x 2.2 x 1.3 m, centred 0.1 m ahead of the pivot): the old 1.0 x 2.4 box was centred on the origin (not the mesh) and would have overlapped the rail. The tube is overhead (1.7 m and more above the floor beyond the pivot).
- **Prepare check (`CastleWorld.LogTelescope`).** Measures the placed art's mesh bounds (falls back to the kit table), maps the footprint into castle space with its yaw, and checks: wall-face clearance >= .12 (observatory interior from `CastleLayout.Room(Observatory)` inset by `SpineWallHalf`/`ExteriorWallHalf`, which mirror `Walls()`), clearance >= .08 to the stair opening with its rail and to the orrery footprint, and that the nearest tower-floor window is crossed by the sight line inside its opening (inner and outer face, margins .15). Output, expected:
  `HC_TELESCOPE aim=window clear=True pos=(11.25, 6.40, 24.80) yaw=8 heading=(0.14,0.99) elev=30.0 window=ObservatoryWindow@z=28.0/12.0 aim_inner=(11.64,2.75) aim_outer=(11.76,3.28) wall_gap=0.75 stair_gap=0.15 orrery_gap=0.24 footprint=[x 10.74..11.95 z 24.13..26.80] bounds=mesh`
  (numbers from a Python mirror of the same formulas using the kit bounds; the player log is authoritative). `clear=False` logs an error and **throws, failing the prepare** (as `HC_CANDLES` does). `tools/check_castle_kit.py <prepare log>` now also requires `HC_TELESCOPE aim=window clear=True` (`--allow-no-art2` for older logs).
- **Elevation** is the generator's constant (30 degrees, inside the requested 20-35), not measured at runtime.

## D. Kitchen hearth (`CastleLayout.cs`, `CastleWorld.cs`, `tools/check_castle_kit.py`)

- **Plan.** `DESIGN.md` only calls the kitchen "surreal: knives orbiting the block"; no wall plan constrains the hearth.
- **Cause.** The hearth (2.8 m nominal, 3.0 m mesh, along z at yaw 90) stood at (-13.0, 5.0) exactly over the west lancet (opening z 4.4..5.6, sill 1.0, head 2.6).
- **Fix: moved along the same wall to a solid stretch** (no window removed). The west wall is solid at z 0.3..4.4 (cauldron at z 1.6) and z 5.6..9.7; `CastleLayout.KitchenHearthPos` = (-13.0, 0, 7.5), `KitchenHearthYaw` 90: footprint z 6.0..9.0, 0.4 m clear of the window, 0.7 m from the partition face. Its fire light (`CastleLayout.Candles`, (-12.2,.2,5.0) -> (-12.2,.2,7.5)) and the candle stand beside it ((-12.4,0,8.6) -> (-12.0,0,9.3), which would otherwise touch the hearth) follow. x is unchanged (wall-backed as before, so `CastleKitAxis` `WallBacked` still holds).
- **Prepare check (`CastleWorld.LogHearth`).** Footprint from the placed art's mesh bounds; tests every ground-floor window and door opening within 1.5 m of the footprint (along-wall distance to the opening span, sill/head against the hearth height) and that the hearth stands inside the kitchen within 5 cm. Expected:
  `HC_HEARTH window_overlap=False pos=(-13.00, 0.00, 7.50) yaw=90 footprint=[x -13.52..-12.48 z 6.00..9.00 h 2.40] openings_checked=5 nearest=LancetWindow@x=-14.0/5.0 window_gap=0.40 (min 0.3) room_gap=0.03 bounds=mesh PASS`
  A Python mirror of the same maths over the layout tables gives overlap=True gap -2.10 for the old (-13, 5) and overlap=False gap 0.40 for the new position (so the check would have caught the original bug). Overlap, a gap under `HearthWindowMinGap` .3, or a hearth outside the room logs an error and fails the prepare. `check_castle_kit.py` now also requires `HC_HEARTH window_overlap=False`.

## What the integrator must run

1. **Merge `hc/fc-art2` into the integration branch.** `Investigator.cs` (shared with Bellwether) changed, so **re-bake both maps**: castle (`HouseLighting.PrepareAndBakeCastle`) and Bellwether. The telescope, hearth, fire light and candle also move baked lighting, so the castle bake is needed regardless.
2. **Castle prepare log**: grep and confirm
   - `HC_TELESCOPE aim=window clear=True ...` (a failure aborts the prepare),
   - `HC_HEARTH window_overlap=False ... PASS` (same),
   - `HC_INSTRUMENTS map=castle compass=1277 trumpet=1026 period=True lantern=1150 radio=1105 (max 3000)` (the last number is measured offline; compass/trumpet/lantern unchanged),
   - `HC_COMPASS_DIAL dial_shader=HollowCurrent/CastleSprite rim_shader=HollowCurrent/CastleEmissive ...`,
   - plus the existing lines (`HC_KIT_AXIS_SUMMARY problems=0`, `HC_CANDLES ... PASS`, `HC_CHAPEL_PROPS`, `HC_DOOR_LEAF_SUMMARY`).
   Then `python3 tools/check_castle_kit.py <prepare log>` (now needs HC_CANDLES, HC_TELESCOPE and HC_HEARTH) and `tools/check_throwables_scene.py`.
3. **Captures** (through the GPU slot, windowed, `open -g`, owner at the machine):
   - `-hc-capture <dir> -hc-capture-art instruments -hc-capture-lantern on` (and `off`): check `art_instruments_held_compass_idle/reading5` (parchment dial, umber numerals and needle, upright, ember pips orange, thin rim, nothing grey over the face), `art_instruments_held_radio` (`HC_CAPTURE_HELD tool=4 asset=FieldRadio verts=1105`, glove on the lower right corner), plus the lantern and trumpet as before.
   - `-hc-capture-art kit`: `kit_telescope` (tube heading to the north window past the orrery) and `kit_hearth` (hearth beside, not over, the west lancet), plus the existing three kit views.
4. **Bellwether** `-hc-verify`: must still pass ("Radio call raises original radio mesh", `HeldAssetName == "FieldRadio"`, more than 1000 vertices; "flashlight meshes mounted"); `HC_NATIVE_ROUTE_PASS`.
5. Castle playtest proofs as usual (`cold`, `escape`); the kitchen route and the observatory ascent (hilt at the orrery socket) are the places the moves could matter.

## Risks

- **A is a diagnosis, not a proven fix** (see A, NOT verified). Read `HC_COMPASS_DIAL` first. The glove sits on the same grip as before; the rim band is only about 3 px wide at 1080p.
- **B** glove fit (palm at the box's lower right corner; fingers curl over the front) and scale (13 cm box, 10 cm horn) are unseen in the engine; nudge `RadioGeometry` box centre or horn path if fingers clip. The radio root must not be scaled (the glove is its child).
- **C** the telescope now sits in a 1.6 m strip: 0.15 m to the stair rail, 0.24 m to the orrery footprint (nominal 3 x 3 m; the mesh is smaller), so the way from the stair head to the north end of the observatory narrows to about 0.4 m each side of the tripod (the west strip beside the orrery and the room's south part stay open). `PerchLocal[4]` (the Count's hover point, (11.4, 8.6, 26.4)) is now about 0.3 m from the objective rim: harmless for a flying silhouette but untouched; move it west (for example x 10.0) if the captures show the Count merged into the tube.
- **D** the hearth's moonlight no longer comes through it, but the west lancet's `Window moonlight` spot still shines from the same window; the kitchen's first-frame look from the dining door changes (hearth now toward the north end). The old `kit_window_inside`-style captures are unaffected.
