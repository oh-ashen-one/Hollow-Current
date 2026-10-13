# Round 3 captures: Haunted Castle

Facts only. Judging them is the critic's job.

- Captures: `/Users/midir/hollow-current-haunted-castle/docs/haunted-castle/rounds/r3/captures/` (gitignored), with `capture.json`.
- Player: `/Users/midir/hollow-current-haunted-castle/Builds/macOS-Castle/Hollow Current Castle.app`, built with `BuildPreparedMacCastle` from commit `9f7b2d7`.
  - Merged since round 2: hc/look3 `629148b`, hc/dressing3 `d52c641`, hc/count3 `6050ee0`.
  - Count import re-run: 56 bones, 7 clips, 36,405 triangles.
  - Castle bake: 966 static renderers, 6 lightmaps, 196 probes, 17 reflections, with look3's ambient values.
  - `CastleLightTuning.Tune` ran on 61 lights before the bake. Candle values were restored after it, so the player tunes candles once at runtime.
- Command: `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-capture -- ".../Hollow Current Castle.app/Contents/MacOS/Hollow Current" -hc-capture /Users/midir/hollow-current-haunted-castle/docs/haunted-castle/rounds/r3/captures`. Slot hold 38.4 s, exit 0.
- Machine and settings: Mac Studio M3 Ultra, Unity 6000.3.20f1 Mono player.
  - Full-screen window at 2560x1440 on a 3440x1440 display; all 18 PNGs are 2560x1440.
  - Quality "Ultra", MSAA 4x, shadows Medium at 16 m, target 90 fps, vSync 0.
  - Camera FOV 72°, except S4 at 90°.
  - `CastleAtmosphere` and `CastleLightTuning` are active.
- Capture setup:
  - 3 s warm-up after load.
  - HUD off for S1 to S6 and the H frames; investigator input disabled.
  - Flashlight off for S1 to S4; on for S5, S6 and the hunt.

## Shot list

| Shot | Eye (x, y, z) | Look-at (x, y, z) | Staging |
|---|---|---|---|
| S1 great hall | 0, 1.64, 14.70 | 0, 2.60, 27.00 | inside the hall great doors |
| S2 chapel altar | 7.00, 1.64, 6.50 | 13.00, 1.75, 6.50 | nave aisle |
| S3 crypt | -5.20, -1.56, 25.00 | 0, -2.60, 22.50 | crypt side of the service-stair door |
| S4 exterior sky | 0, 2.00, -6.00 | 0, 6.16, 13.56 | on the lowered drawbridge (snapped lowered), pitch 12°, vertical FOV 90° |
| S5 lord perch | -0.41, 3.62, 19.37 | -3.24, 5.38, 20.54 | `StageForCapture(0)` = true. Feet at (-3.22, 4.25, 20.51). Camera from `CaptureFraming(0.5, 30°)` |
| S6 lord sheet | -0.30, 1.15, 1.00 | -0.30, 1.10, 4.40 | in the gatehouse. Count feet at (-0.9, 0, 4.4), Mode 2. 1.80 m `ScaleReference` at (0.35, 0, 4.4) |
| S7 title | n/a | n/a | castle selected |
| S8 journal | n/a | n/a | "Ledger: the steward" |
| H01 to H10 | 0, 1.64, 15.60 (H10: -0.02, 1.64, 15.30) | tracks the Count (per-frame look-at in capture.json) | staged hunt, then a 3 s wait for the warning mist, then `BeginCaptureChoreography(1)`. Shot k taken at `CaptureClock` = k |

## S4 moons

Log line: `HC_CAPTURE S4 pale_moon inFrame=False occluded=False red_moon inFrame=True occluded=False`.

- inFrame tests the direction of the moon's centre against the frame.
- occluded is a physics ray from the eye along the moon's direction.
- Result: the pale moon's centre direction falls outside the frame (above the top edge) but is not occluded. The red moon is in frame and not occluded.

## Hunt choreography

Log lines `HC_COUNT_CAPTURE` from `/Users/midir/hollow-current-haunted-castle/.capture.log`:

```
begin perch=1 feet=(3.22, 4.25, 20.51) glideEnd=(0.97, 1.50, 21.01) hold=(0.44, 0.80, 18.06) holdDistance=2.50 eye=(0.00, 1.64, 15.60)
phase=Perch clock=0.01 mode=0 distance=6.89
phase=Glide clock=3.50 mode=1 distance=6.89
phase=Dive  clock=6.50 mode=3 distance=5.57
phase=Hold  clock=8.01 mode=3 distance=2.51
phase=Feint clock=9.51 mode=1 distance=2.51
end clock=10.01
```

All five phases logged.

`HC_CAPTURE_CAMERA_PUSHED by 0.10` was logged 3 times during Feint; the player moved back from z 15.60 to 15.30. No two H frames share an md5.

| Frame | Phase | Mode | Distance | Span | Height |
|---|---|---|---|---|---|
| H01 | Perch | 0 | 6.99 m | 3.07 m | 2.56 m |
| H02 | Perch | 0 | 7.09 m | 3.01 m | 2.57 m |
| H03 | Perch | 0 | 7.03 m | 3.05 m | 2.57 m |
| H04 | Glide | 1 | 7.10 m | 1.85 m | 3.20 m |
| H05 | Glide | 1 | 6.60 m | 3.81 m | 2.52 m |
| H06 | Glide | 1 | 6.50 m | 4.30 m | 3.22 m |
| H07 | Dive | 3 | 5.62 m | 2.65 m | 2.79 m |
| H08 | Hold | 3 | 2.89 m | 2.50 m | 2.76 m |
| H09 | Hold | 3 | 2.88 m | 2.43 m | 2.76 m |
| H10 | Feint | 1 | 2.99 m | 4.27 m | 3.20 m |

Distance is from the Count's bounds centre to the eye. Span and height are his bounds, from capture.json.

## Frame times

Unscaled `Time.deltaTime`, measured in the player.

| Window | Frames | Mean | p95 | Max |
|---|---|---|---|---|
| S1 great hall, 3 s held view | 270 | 11.15 ms | 11.30 ms | 13.82 ms |
| Hunt, clock 0 to 10 s (includes 10 PNG encodes and writes on the main thread) | 882 | 12.79 ms | 11.14 ms | 187.96 ms |

## Logs

Capture run:
- `[CastleLightTuning] tuned 61 lights`.
- No `[CastleArt] missing` lines and no exceptions.

Headless boot (`-batchmode -nographics`, Null device) of the castle player:
- `[CastleArt] missing CastleLit` and `CastleTriplanar` ("shader not supported", Standard fallback). These do not appear in the graphics capture run.
- `HC_STARTUP_READY`.

Bellwether player headless boot: `HC_STARTUP_READY`, 49 clips, no exceptions.

## Pixel checks

Downscaled to 640x360. Magenta = R>200, B>200, G<80; dark = luma < 8.

| Capture | Magenta px | Dark % | Mean luma |
|---|---|---|---|
| S1 | 0 | 13.0 | 43.7 |
| S2 | 0 | 24.2 | 38.6 |
| S3 | 0 | 44.6 | 12.9 |
| S4 | 0 | 41.3 | 15.6 |
| S5 | 0 | 27.8 | 23.2 |
| S6 | 0 | 22.5 | 18.7 |
| S7 | 0 | 20.1 | 67.5 |
| S8 | 0 | 5.5 | 103.5 |
| H01 to H04 | 0 | 59.9 to 61.1 | 15.9 |
| H05 to H10 | 0 | 44.3 to 57.9 | 18.0 to 24.0 |
