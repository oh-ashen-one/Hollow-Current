# Handoff: fd-art (wave FD): exterior and great hall to 6/10

Branch `hc/fd-art` (from `haunted-castle` `0c2796e`), worktree `.wt/fd-art`. Builder: Opus 5.5, 2026-10-18. Nothing running. No Unity
launch and no GPU render: every proof below is CPU-only (Blender headless BVH ray casts plus numpy). Cost 0: no CustomUse or Higgsfield job.

Subject: the r13 critic's change #3 (`rounds/r13/CRITIC.md` section 4 and section 8 item 3). S1 and S4 scored 4/10, and the pass is 6.

## What changed, per critic item

| Critic item (r13) | Before (f00e343) | After (this branch) | Files |
|---|---|---|---|
| Moat "a grey fog plane, not red, not reflective" | Unlit water reflecting a blue horizon. Six stacked grey-blue fog quads over every moat strip. Exterior fog .05 | Blood mirror: reflected sky luminance times a blood tint, plus a reflected skyline band, rock band and castle band. Fog is applied at 45 %. One sparse dark veil over the lake; the side and back moats keep 2 thin layers (was 6). Exterior fog .034 in a darker blue. The gate passage (z < 1.6) counts as outside | `CastleWater.shader`, `CastleExterior.FogBands`, `CastleAtmosphere` |
| "no moon reflection" | Only a true mirror of a moon at 55° elevation, which falls outside every eye-level frame | An art-directed moon glade sits at the pale moon's azimuth with an effective elevation of 13°. When the moon is behind the camera (gate_out looks south), the glade sits 21° right of the view axis, on the moon's side. **This is a documented cheat.** The far-bank braziers and the causeway lamps add warm glints | `CastleWater.shader` |
| "no far bank", "2/10 view out" | Flat earth box right behind a 5 m moat, under fog | Blood lake, z -0.45..-16 and x ±24, with the water lowered to y -1.6. A timber causeway on stone piers runs to a paved far landing. The landing has two iron braziers (realtime point lights, range 11, intensity 3, no shadows), a ruined gateway, a stepped broken wall, dead trees, shore rocks and outcrops, with shore mist and jagged ridges behind. The leaf's solid parapets are now open timber railings (the collision walls stay), so the water shows from the gate | `CastleWorld.Exterior`, `CastleExterior.Approach`, `CastleWorld.BuildGate` |
| Towers "squat drums with cone caps" | Gate drums 9.4 m with flat tops. Corner towers 11-13 m with a cone straight on the shaft | Gate drums 13.6 m with pepperpot roofs. Corner towers rise 13.8-19.5 m, with a parapet, a turret stage (r 1.2) with slits, and a slender spire. Keep and observatory spires +3 m. All tips stay clear of both moons from the new S4 eye (BVH: 0.0 % / 0.0 %) | `tools/castle_exterior.py` → `ExtTowers/ExtGate/ExtKeep.fbx` |
| "The rock base is a few lumps" | Plateau 2.5-4 m deep. Cliff 2.4-3.8 m out | Basalt outcrop: a ridged plateau up to 5.5 m, a scarp, a broken terrace and the main cliff 3.8-7.1 m out (narrower on the gate face), spurs every ~4.5 m, cliffs down to the water at -1.6. Four new crags at the lake corners. Window caps extended to the whole apron, with new caps for the first-floor lancets and the chapel sunburst | `tools/castle_exterior.py` → `ExtRock.fbx` |
| Moons stretched at FOV 95 | S4 search found 14 m / 2.5 m / FOV 95 | The search now starts 22 m out at eye height 1.64 (back 22, 20, 18, 16, 14...; heights 1.64, 2.5, 3, 3.5). Simulated hit: eye (0, 1.64, -22), pitch 28.5, **FOV 90**, both moons inset, gate base at viewport y 0.18 | `CaptureMode.cs` |
| S1: "Lancet windows: none; two small square windows" | North tall lancets at x ±4.4 were cut by the galleries' floor at 3.2 | Four north-wall lancets. `HallLancetWindow` 1.0 x 2.8 at x ±2.6 flanks the rose. `GalleryLancetWindow` 1.0 x 4.4 at x ±4.85 sits above the galleries. The string course skips them. The old two window spots are dropped (no new lights) | `CastleLayout.Windows`, `CastleHallKit`, `tools/castle_hall_kit.py` |
| S1: "the piers are square blocks" | The `PointedArch` legs (0.45 x 0.5, floor to 2.0) hid the cluster columns | The arch springs from the columns' impost rings at 2.0, and its legs are removed | `tools/castle_hall_kit.py` → `PointedArch.fbx` |
| S1: "banquet table out of shot", "tan museum quadruped" | Eye (0, 1.64, 14.7) behind a 1.7 m ribcage | Skeleton height scale .6 → .5 (plan unchanged). A torn, two-sided oxblood shroud in three ragged panels over the ribcage, with holes and a jagged hem. Darker bone instance (x .55/.51/.47). The S1 eye is on the threshold at (0, 2.3, 14.55) looking level | `tools/castle_props_r12.py` → `GiantSkeleton.fbx`, `CastleLayout.SkeletonScaleVec`, `CastleWorld.DarkenBone`, `CaptureMode` |
| Skull from the east "a dark low-poly lump" | 12-sided loft, 9 sections | 22-sided loft, 16 sections, ridged horns sweeping back, brow ridges | `tools/castle_props_r12.py` |
| S1: "no floor sheen and no mist" | Mist at .5 x .12 | `CastleFloorSheen`: one additive quad with analytic wet-stone reflections of the 4 candle stands, the chandelier, the rose window and the north lancets, in puddle patches with fresnel. It adds light only along the streaks. Mist alpha .16 x brightness .375 keeps the same mean lift (.06) with more contrast | `CastleFloorSheen.shader`, `CastleWorld.FloorSheen`, `CastleAtmosphere` |
| Chapel glass "a coarse mosaic of flat squares" | 4 x 5 flat emissive squares per light | Each light has a red/amber border of small squares around leaded diamond quarries (0.13 x 0.20, 14 mm cames) in pale, blue, green, amber, red and ink. About a fifth are broken out and some sag. Rosette sectors are leaded 2 x 2. New `Castle_GlassGreen/Pale` | `tools/castle_hall_kit.py` → `ChapelSunburst.fbx` |
| Dining-hall beam "stretched, smeared" | Underside of the library floor slab in world-space Castle_Oak | A plank liner (0.3 m boards) and oak joists every 1.6 m (headroom 2.71 m, none over the fireplace hood). Render only | `CastleWorld.DiningCeiling` |
| Count's wings red in the dark, purple under the lantern | `CastleGore` translucency was unlit emission | Translucency is now a lighting term (back-light plus lit rim), scaled by each light's **luminance**. The wings are black in the dark like the body and glow their own red under any light | `tools/castle_textures.py` → `CastleGore.shader` |

## How it was verified (static and offline only)

- `INPUT_SYSTEM_REFERENCE=…/Unity.InputSystem.dll python3 tools/check_source.py`: PASS. `check_castle_scares` PASS, `check_audio_keys` OK. `check_castle_kit.py` (scene and `--simulate-fix`): problems=0. `check_castle_doors.py` OK. `check_throwables_scene.py`: 0 batched. **These scene checks read the old baked `Castle.unity` and only show nothing old broke. Re-run them on the new bake.**
- `Blender -b -P tools/castle_exterior.py -- --proof <dir> --moons-only` (BVH, CPU):
  - `HC_EXTERIOR_CLEAR PASS`; 0 kit vertices inside rooms, the standing spot or the bridge corridor.
  - Moons from the new S4 eye: pale 0.0 %, red 0.0 %.
  - The old 14 m eye (fallback only) would now lose 55 % of the red moon to the right gate drum's roof.
- Exterior proxy proof, `tools/castle_fd_raycast.py` + `tools/castle_fd_shade.py`. It casts one ray per pixel at 2416x1292, then applies flat palette, moonlight, braziers, exponential fog and a numpy twin of the new `CastleWater`. Metrics (`proof/metrics.json`):
  - gate_out_S6: moat R/G **2.04**, glade 6050 px², far bank lit 89532 px²
  - S4 (new eye): R/G **2.02**, glade 13651 px²
  - battlement_out: R/G **2.37**, glade 2617 px², far bank lit 164799 px²

  The proof ignores lightmaps, fog quads and textures.
- Hall proxy proof, `tools/castle_fd_hall_raycast.py`. It imports the hall kit FBX, the skeleton at the new scale, the table, chairs and throne, then ray-casts the S1 eye.
  - The table and chairs are visible over the shroud: 18.6k px² (old eye 3.5k).
  - Lancets visible 172 / 172 / 168 / 168 px tall. The rose window is 200 px.
  - The columns are clustered shafts, with no square piers.
- Local evidence (gitignored): `.wt/fd-art/docs/haunted-castle/rounds/r13/captures/fd/fd-art/proof/` holds `S4_exterior_sky.png`, `art_exterior_gate_out_S6.png`, `art_exterior_battlement_out.png`, `hall_S1_final.png`, `chapel_sunburst_glass.png`, `metrics.json` and `hall_metrics.json`.
- **Not verified:** the in-engine look, the exposure bands, the bake and frame time. These need the recipe below.

## Integrator recipe

1. Merge `hc/fd-art`. Expected conflict: `CaptureMode.cs` (S1 eye, S4 search loop, `Record` p99, two `Measure` calls). Keep both sides.
2. Castle re-bake, through the slot with the graphics device:
   ```
   ~/.cache/gpu-slot/bin/gpu_slot.sh capture --label fd-integ -- <Unity> -batchmode -quit -projectPath <clone> -executeMethod HollowCurrent.Editor.HouseLighting.PrepareAndBakeCastle -logFile <clone>/.castle-bake-fd.log
   ```
   Expect in the prepare log:
   - `HC_EXTERIOR_KIT`
   - `HC_EXTERIOR_APPROACH renderers=… lake z[-16,-0.45] causeway z[-16,-6.3] braziers=2 lamps=4`
   - `HC_EXTERIOR_AXIS ok`
   - `HC_SKELETON … PASS` (centre_x 0.00, y_max ≈ 1.5, min_pier_gap ≥ 2.4, table_gap 0.63)
   - `HC_HALL_AXIS … PASS`, `HC_CHAPEL PASS`
   - `HC_CANDLES … floating=0 PASS`, `HC_KIT_AXIS_SUMMARY problems=0`, `HC_DOOR_LEAF_SUMMARY problems=0`

   Bellwether needs no re-bake: only castle-only paths changed.
3. Static checks on the new bake:
   ```
   python3 tools/check_castle_kit.py .castle-bake-fd.log; python3 tools/check_castle_kit.py; python3 tools/check_castle_doors.py; python3 tools/check_throwables_scene.py
   ```
4. Build (`NativeBuild.BuildPreparedMacAll`), then captures through the slot with `open -g -W -n … --args`, one instance at a time:
   - `-hc-capture <d>/rooms_off -hc-capture-lantern off` and `… rooms_on … on` (S1-S8). The log must show `HC_CAPTURE S4 search eye=0.00,1.64,-22.00 pitch=28.5 fov=90` and `FRAMETIME S1_great_hall …` and `FRAMETIME S4_exterior_sky … p99=`.
   - `-hc-capture <d>/ext -hc-capture-art exterior`. Gives `FRAMETIME art_exterior_gate_out_S6` / `battlement_out` with p99.
   - `-hc-capture-art props`, `-hc-capture-art kit`, and `-hc-capture-art layout -hc-capture-lantern off|on`.
   - `-hc-capture-lantern` sets: the instruments set is optional; the kill/huntcam frames show the wings.
5. Exposure: `python3 tools/exposure_report.py <d>/rooms_off --lantern off` (and `on`, and `layout_off/on`). Bands:
   - Lantern off, hall 35-55 % near-black. The north window spots were removed and the skeleton is darker, so expect higher than 30.4.
   - Lantern on, 12-25 %.
6. Moat test (critic's numbers) on `art_exterior_gate_out_S6.png` and `S4_exterior_sky.png`:
   ```
   python3 -c "import sys;from PIL import Image;import numpy as np;F,M=sys.argv[1:3];a=np.asarray(Image.open(F).convert('RGB')).astype(float);m=np.asarray(Image.open(M).convert('L').resize((a.shape[1],a.shape[0])))>127;w=a[m];l=w@[.2126,.7152,.0722];s=a.shape[0]*a.shape[1]/(2416*1292);print('R/G %.2f (pass>=1.6)'%(w[:,0].mean()/w[:,1].mean()),'bright_px2',int((l>=150).sum()/s),'(pass>=400)')" <d>/ext/art_exterior_gate_out_S6.png tools/fd_art_masks/art_exterior_gate_out_S6_water.png
   ```
   For S4 use `tools/fd_art_masks/S4_exterior_sky_water.png`. That mask assumes the S4 search line above.
7. Frame-time pass: S1 / S4 / gate_out / battlement_out p99 ≤ 16.7 ms.
8. A blind critic scores S1 and S4.

## Risks and open points

- **Unrendered:** none of this has been seen in the engine. The likeliest surprises:
  - The `CastleGore` custom lighting function (a surface shader with `LightingStandardTrans` / `_GI`) or `CastleFloorSheen` fails to compile. Watch the player log for shader errors: a pink Count, or no sheen with the warning `CastleFloorSheen unavailable`.
  - The lake is too bright or too pink. The knobs are `_BloodTint`, `_Reflect`, `_Glade` and `_FogKeep` on `Castle exterior moat water`.
- **Exposure:** the S1 framing changed (eye 2.3 m). The hall lost its two north window spots and gained the sheen streaks. Re-measure all hall views, and if lantern-off goes past 55 % restore one spot (`Light: true` on the gallery lancets).
- **Fallback S4 eye:** if the search does not take the 22 m eye, the 14 m fallback frames the red moon behind the right gate drum's roof.
- **Moon glade in gate_out is a cheat:** the glade sits 21° off axis on the moon's side while the moon is in the north sky (header of `CastleWater.shader`). A critic who checks the geometry may object.
- **Far clip and the backdrop:** the north ridges were pulled in (`RZ_NORTH`) so the 75 m far clip from (0, 1.64, -22) keeps them. They now sit 5-6 m closer behind the hall and the courtyard.
- **Wing colour:** the wings are now dark in unlit grabs. That is the request, but it may make the r13 "fully lit Count" complaint feel different. Fight frames under the cold rim light should read dark red, not purple.
- **Not done:**
  - Floating hall candelabras still have no holders (design floaters).
  - The S5 ledge candelabras float in front of the new ±2.6 lancets.
  - Cobwebs were not added; the shroud carries the "draped corpse" read.
