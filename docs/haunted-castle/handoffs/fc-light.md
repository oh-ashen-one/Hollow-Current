# HANDOFF: piece fc-light (`hc/fc-light`, worktree `.wt/fc-light`)

HANDOFF.md open item 1: the hall is too bright with the lantern off, and the chapel (and crypt) are too dark with it on. This is the lighting half of owner item 12 ("too well lit"; the castle should be dark and lantern-led). The piece is castle only: Bellwether's torch, bake and receipt are untouched.

Unity was not launched and nothing was baked. The changes rest on an analysis of the integrator's fb2 captures (`docs/haunted-castle/rounds/r13/captures/fb2/{rooms,layout}_{off,on}`, local to the clone). Every number below can be reproduced with `tools/hall_light_model.py`.

## What the captures show (why the earlier realtime cuts barely moved the hall)

1. **The hall floor is lit by the ground mist, not by lights.** In S1 with the lantern off, the walls are already black: rows 20-60 % of the frame are 45-54 % near-black. The floor half of the frame is only 1-18 % near-black (median luma 18-23).
   - The hall mist (`CastleAtmosphere` ground fog area 0) is 8 unlit, alpha-blended planes, 0.08-1.1 m over x -6..6, z 14..28.
   - Their colour (.12,.15,.21) alone is luma ~37. The measured noise mean is .748, and the stacked coverage reaches about 0.4-0.6 in the hall centre.
   - Ray-tracing that coverage from each capture's eye and target predicts more floor brightness than was observed: observed/predicted median 0.55-0.93. So the floor under the mist is close to black, and the mist is the hall's lantern-off brightness.
   - The mist ignores candles, the bake and ambient. That is why candles -25 % gave only +2 points.
   - Neither the chapel nor the crypt can see this mist. The crypt has its own fog area 1.
2. **The lantern did not light most surfaces.** I diffed on against off per pixel: `tools/hall_light_model.py reach <off> <on> --angle 80`.
   - The torch lit the hall vault, candelabras and far wall, but added exactly 0 to the hall floor, the near walls, the skeleton, the chapel floor, the pews and the altar.
   - In-cone reach was S1 33 %, S2 15 %, skeleton views 4-16 %, chapel views 11-37 %.
   - Cause: `Investigator.Torch` uses the Auto render mode. Ultra's forward path gives each object 4 pixel lights. The big floor, wall and pew objects already carry the ForcePixel fills plus nearer candles, so the torch drops to per-vertex/SH lighting on them, which shows as nothing.
   - The surfaces the torch did reach gained +40 to +180 luma, out to the 40 deg cone edge.
3. **The chapel is dark outside the cone.** Even a fully working 80 deg lantern leaves 18-35 % of the chapel frames near-black outside its cone (S2 24 %, guard door 35 %). Those frames need 12-25 % with the lantern on.

## What changed (all runtime; the castle re-bake is only needed because the receipt hashes every `.cs`)

| File | Change | Why |
|---|---|---|
| `Runtime/Castle/CastleAtmosphere.cs` | New `HallMistBrightness = .5` scales the hall mist colour (area 0 only) in linear light. Hall `FogAreaAlpha` goes .15 -> .12. The crypt mist is unchanged. | Lantern-off hall (point 1). The lower alpha also lets more lantern-lit floor show through the veil. |
| `Runtime/Castle/CastleLightTuning.cs` | New `TorchForcePixel = true`: `ApplyTorch` sets `Torch.renderMode = ForcePixel` (castle only). | Lantern-on chapel and crypt (point 2). |
| same | `TorchSpotAngle` 80 -> 100. | Chapel near-black outside the cone (point 3). |
| same | New `HallWindowMoonlight = .48` covers the non-rose window spots inside the hall: x within ±6.8, z 13.5-29, y 0-9.5, castle-local. The default equals `WindowMoonlightFloor`, so nothing changes this round. | A hall-only baked fallback knob. Chapel, courtyard and crypt windows are not affected. |
| `Runtime/Castle/CaptureMode.cs` | Both `HC_CAPTURE lantern` log lines now print `range`, `angle` and `mode`. | Lets the integrator confirm ForcePixel and 100 deg from the log. |
| `tools/hall_light_model.py` (new) | `reach` diagnostic and `predict` model. | Evidence and calibration for the next round. |

Not changed: the bake ambient (it is global, so cutting it darkens the chapel, which the brief forbids), the window moonlight values, candles, fills and the vault.
- The vault is only 7 % of S1 and only S1 sees it. A x0.4 vault gains S1 about +2 points.
- The moonlight is only a fallback, because the walls are already black and the floor's light is the mist.

## Expected effect (near-black %, `tools/hall_light_model.py predict docs/haunted-castle/rounds/r13/captures/fb2`)

The lantern-off range covers the mist calibration (kappa x0.7..x1). The lantern-on range is for p .85..p .6, where p is the probability that a newly reached in-cone surface ends at luma >= 8; on surfaces the torch already reached, 93-100 % did.

| View | off now | off predicted (35-55) | on now | on predicted (12-25) |
|---|---|---|---|---|
| S1_great_hall | 26.8 | 37.6-41.5 | 19.4 | 10.6-17.0 |
| hall_from_doors | 25.6 | 40.0-44.5 | 19.3 | 12.6-19.6 |
| hall_pier_gap | 23.5 | 45.1-51.0 | 17.6 | 14.7-22.9 |
| hall_skeleton_head | 21.5 | 36.4-40.1 | 10.5 | 10.4-15.3 |
| hall_skeleton_side_east | 21.2 | 30.4-42.8 (borderline) | 19.9 | 10.9-17.7 |
| hall_skeleton_side_west | 31.7 | 56.8-63.7 (too dark) | 21.5 | 14.3-24.8 |
| hall_skeleton_tail | 31.8 | 47.2-53.6 | 27.4 | 15.8-24.6 |
| S2_chapel_altar | 41.3 | 41.3 (unchanged) | 40.0 | 14.7-22.1 |
| S3_crypt | 35.1 | 35.1 (unchanged) | 28.9 | 9.6-15.3 |
| chapel_aisle | 40.7 | unchanged | 38.7 | 15.1-22.0 |
| chapel_altar_approach | 25.4 | unchanged (already too bright) | 25.5 | 13.3-16.9 |
| chapel_confessional | 62.9 | unchanged (already too dark) | 55.4 | 21.0-31.1 |
| chapel_font_corner | 49.4 | unchanged | 39.9 | 11.4-19.8 |
| chapel_from_great_doors | 48.2 | unchanged | 46.3 | 18.1-26.4 |
| chapel_from_guard_door | 59.2 | unchanged (already too dark) | 55.3 | 23.4-32.8 |

How to read it:
- **Lantern off:** 6 of 7 hall views are predicted in band. side_west goes darker than the band: the two skeleton side views differ by about 20 points under any symmetric hall change, and a darker castle with the lantern off follows the owner's direction.
- **Lantern on:** every view's range overlaps 12-25. The crypt and the skeleton head sit near the bright edge. The confessional and guard door sit at the dark edge.

Limits of the model:
- Candles, the bake and the reflection probes are taken as captured.
- Objects standing in the mist (skeleton, pier bases) shorten the ray, which is partly corrected.
- p is unknown. 100 deg is the angle whose p .6-.85 bracket centres on the band; 90 leaves the chapel at 24-40 and 115 pushes the hall and crypt under 12.

## Integrator recipe

1. Merge `hc/fc-light`. Only castle files and `tools/` changed: re-bake the **castle only**. Bellwether's receipt excludes `Runtime/Castle/`.
2. Bake through the slot with the graphics device (no `-nographics`): `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label fc-light -- <Unity> -batchmode -quit -projectPath <clone> -executeMethod HollowCurrent.Editor.HouseLighting.PrepareAndBakeCastle -logFile <log>`. Expect `HC_CASTLE_LIGHT_TUNING`, then `HC_LIGHTING_BAKED map=Castle`; the receipt's map and probe counts should be as before (7 / 197 / 17).
3. Build: `NativeBuild.BuildPreparedMacAll` (`-nographics`), as for fb2.
4. Run captures with the fb2 invocations (same build, `open -g`, 1920x1080), into new folders, e.g. `rounds/r13/captures/fc/{rooms_off,rooms_on,layout_off,layout_on}`.
   - Use `-hc-capture <dir> -hc-capture-shots S1,S2,S3 -hc-capture-lantern off|on`, and `-hc-capture <dir> -hc-capture-art layout -hc-capture-lantern off|on`.
   - The lantern-on logs must read `HC_CAPTURE lantern on torchScale=2.40 intensity=2.04 range=24.0 angle=100 mode=ForcePixel`.
5. Measure:
   - `python3 tools/exposure_report.py <dir>_off --lantern off --prev <fb2 dir>_off`
   - `... <dir>_on --lantern on --prev <fb2 dir>_on`
   - `python3 tools/hall_light_model.py reach <dir>_off <dir>_on` (default angle 100): in-cone reach should be **> 60 %** on S1, S2 and the chapel views (fb2: 11-37 %).
6. Pass bands (exposure_report near-black %):
   - **off 35-55** for S1, S2, S3 and every `art_layout_hall_*` view (the primary goal is the S1 and hall views).
   - **on 12-25** for S1, S2, S3 and every `art_` view.
   - Chapel lantern-off was already 25-63 % before this piece and is not changed by it.
7. Frame time: compare `capture.json` `frametime` (S1, hunt_dive) and a Bellwether `-hc-verify` against fb2 (S1 11.15 ms mean, hunt_dive max 21.8). ForcePixel adds one additive pass per lit object.

## One-round fallback (edit one constant, re-bake the castle, rebuild, recapture)

Apply the first row that matches, at most two rows in a round:

| Observed after this round | Edit | Expected |
|---|---|---|
| `reach` < 60 % on S2 and the chapel views, or the log does not say `mode=ForcePixel` | Do not tune; report it. The pixel-light diagnosis was wrong or did not apply. | n/a |
| S1 off 31-35 % | `CastleAtmosphere.HallMistBrightness` .5 -> .25 | S1 +2..3 |
| S1 off < 31 % (the floor is lit by the bake, not the mist) | `CastleLightTuning.HallWindowMoonlight` .48 -> .30 (baked) | Hall floor and far wall darker; chapel and crypt unaffected |
| 3 or more hall views off > 55 % | `CastleAtmosphere.HallMistBrightness` .5 -> .7 | S1 -3.5, side_west -7 |
| 2 or more chapel views on > 25 % | `CastleLightTuning.TorchSpotAngle` 100 -> 115 | Chapel on -5..-12, hall on -3..-7; check the hall stays >= 12 |
| 3 or more hall views on < 12 % | `CastleLightTuning.TorchRange` 24 -> 18 | Far hall falls back to dark; the chapel (12 m deep) barely changes |
| S3 on < 12 % | `CastleLightTuning.CryptColdIntensity` .32 -> .22 | Crypt on and off both darker; S3 off has room (35 -> about 40) |

## Verification done here

- `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py`: PASS (only the known CS0649 warnings).
- `python3 tools/check_castle_scares.py`: PASS. `python3 tools/check_audio_keys.py`: OK.
- `tools/hall_light_model.py predict` and `reach` run against the fb2 captures; the outputs are the tables above.
- No engine launch, no bake, no GPU use.

## Risks

- **The lantern diagnosis is inferred from pixels, not from the engine.** The `reach` check in step 5 settles it in the first capture.
- **Performance.** The ForcePixel torch with hard shadows adds a forward-add pass on every object in its 100 deg, 24 m cone, and it can push one Auto candle per object to vertex lighting while it is on. Watch the frame times in step 7.
- **Feel.** The lantern now lights everything in front of the player, including the floor, so the castle with the lantern on reads brighter near the player than in fb2. That is what the 12-25 % band asks for; the lantern-off castle is darker. The owner should judge it in the next playtest.
- **The hall mist is dimmer**, at about half its linear brightness and 80 % of its alpha. It still reads where the lantern hits the floor beneath it, but no longer glows by itself. If the owner misses it, the fallback is `HallMistBrightness` .7, not more alpha.
- **The chapel lantern-off views are untouched:** altar_approach is too bright, the confessional and guard door are too dark. They were outside this piece's goal.
- **side_west lantern-off is predicted darker than the band.** side_east may land just under 35.
