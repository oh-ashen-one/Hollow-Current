# Round 4 captures: Haunted Castle

Facts only. Judging them is the critic's job.

- **Captures:** `/Users/midir/hollow-current-haunted-castle/docs/haunted-castle/rounds/r4/captures/` (gitignored), plus `capture.json`.
- **Build:** `/Users/midir/hollow-current-haunted-castle/Builds/macOS-Castle/Hollow Current Castle.app`, built by `BuildPreparedMacCastle` from commit `1805388`.
  - **Merged since round 3:** hc/flames `9ac544e`, hc/dressing4 `d295e68`, hc/count4 `4921449`, hc/look4 `ff6b93c`.
  - **Count import:** re-run; 56 bones, 7 clips.
  - **Castle bake:** 965 static renderers, 7 lightmaps, 197 probes, 17 reflections.
  - **Bellwether:** re-baked (4 / 124 / 9); the combined build `/Users/midir/hollow-current-haunted-castle/Builds/macOS/Hollow Current.app` also succeeded.
- **Command:** `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-capture -- ".../Hollow Current Castle.app/Contents/MacOS/Hollow Current" -hc-capture /Users/midir/hollow-current-haunted-castle/docs/haunted-castle/rounds/r4/captures`. Slot hold 44.5 s, exit 0.
- **Machine:** Mac Studio M3 Ultra, Unity 6000.3.20f1 Mono player.
- **Display:** full-screen window at 2560x1440 on a 3440x1440 display; all 18 PNGs are 2560x1440.
- **Quality:**
  - "Ultra" quality level, MSAA 4x, shadows Medium at 16 m, target 90 fps, vSync 0.
  - Camera FOV 72°; S4 uses 90°.
- **Active:** `CastleAtmosphere` and `CastleLightTuning`.
- **Capture setup:**
  - 3 s warm-up after load.
  - HUD off for S1 to S6 and the H frames; investigator input disabled.
  - Flashlight off for S1 to S4; on for S5, S6 and the hunt.

## Shot list

| Shot | Eye (x, y, z) | Look-at (x, y, z) | Staging |
|---|---|---|---|
| S1 great hall | 0, 1.64, 14.70 | 0, 2.60, 27.00 | inside the hall great doors |
| S2 chapel altar | 7.00, 1.64, 6.50 | 13.00, 1.75, 6.50 | nave aisle |
| S3 crypt | -5.20, -1.56, 25.00 | 0, -2.60, 22.50 | crypt side of the service-stair door |
| S4 exterior sky | 0, 2.00, -6.00 | 0, 8.18, 13.02 | on the lowered drawbridge (snapped lowered), pitch 18°, vertical FOV 90° |
| S5 lord perch | -0.53, 3.66, 19.48 | -3.12, 5.27, 20.55 | `StageForCapture(0, true)` returned true; feet (-3.22, 4.25, 20.51); camera from `CaptureFraming(0.5, 30°)`; `HC_CAPTURE_FLOATERS_HIDDEN 4` |
| S6 lord sheet | 0.40, 1.15, 5.60 | 0.40, 1.10, 2.20 | gatehouse, facing the gate; Count feet (-0.2, 0, 2.2), Mode 2; 1.80 m `ScaleReference` at (1.05, 0, 2.2) |
| S7 title | n/a | n/a | castle selected; Bellwether shown as a parchment tab "not in this build" |
| S8 journal | n/a | n/a | "Ledger: the steward" |
| H01 to H10 | 0, 1.64, 15.60 | tracks the Count (look-at per frame in capture.json) | staged hunt, 3 s wait, then a further 6.0 s wait for the warning's interference to end, then `BeginCaptureChoreography(1)`; shot k at `CaptureClock` = k |

## Log lines

From `/Users/midir/hollow-current-haunted-castle/.capture.log`.

```
[CastleLightTuning] tuned 61 lights
HC_FLAME_CHECK scene material: shader=HollowCurrent/CastleFlame tex=Surreal_Flame 64x128 RGBA32 readable=False | runtime material: shader=HollowCurrent/CastleFlame tex=Surreal_Flame 64x128 RGBA32 readable=False | queue=3001
HC_CAPTURE S4 pale_moon inFrame=True occluded=False red_moon inFrame=True occluded=False
HC_LEAN_CHECK flame leans 26 deg toward the target at 5.4 m (floater bend 0.47)
HC_CAPTURE_FLOATERS_HIDDEN 4 eye=(-0.53, 3.66, 19.48) target=(-3.12, 5.27, 20.55)
HC_CAPTURE hunt interference waited 6.0 s
HC_COUNT_CAPTURE begin perch=1 feet=(3.22, 4.25, 20.51) glideEnd=(0.97, 1.50, 21.01) hold=(0.49, 1.20, 18.36) holdDistance=2.80 eye=(0.00, 1.64, 15.60)
HC_COUNT_CAPTURE phase=Perch clock=0.01 mode=0 distance=6.89
HC_COUNT_CAPTURE phase=Glide clock=3.50 mode=1 distance=6.89
HC_COUNT_CAPTURE phase=Dive  clock=6.50 mode=3 distance=5.57
HC_COUNT_CAPTURE phase=Grab  clock=8.00 mode=6 distance=2.86
HC_COUNT_CAPTURE phase=Feint clock=9.50 mode=1 distance=2.86
HC_COUNT_CAPTURE end clock=10.00
```

- **Not logged:** `HC_CAPTURE_CAMERA_PUSHED`, `[CastleArt] missing` lines, exceptions.
- **H frames:** no two share an md5.

## Headless boots and an earlier pass

- **Castle headless boot** (`-batchmode -nographics`, Null device):
  - `[CastleArt] missing CastleLit` / `CastleTriplanar` (shader not supported, Standard fallback).
  - `HC_FLAME_CHECK ... runtime material ... tex=<null> | queue=2000`.
  - `HC_STARTUP_READY`, no exceptions.
- **Bellwether headless boot:** `HC_STARTUP_READY`, 49 clips.
- **Earlier r4 pass, superseded:** it was taken before two fixes, and its PNGs are replaced by this set.
  - **S7:** BEGIN INVESTIGATION, the castle tab, FIELD SETTINGS and EXIT had no labels. Cause: a commented-out `GUI.Label` in `Interface.Action`, fixed in `f9a1467`.
  - **Hunt frames:** H01, H02 and H05 had 1.8% warm pixels, against about 23% in the other H frames. Cause: the warning's 9 s interference was strobing the candles; fixed in the capture code by waiting it out.

## Hunt sequence

From capture.json. Distance is from the Count's bounds centre to the eye; span and height are his bounds.

| Frame | Phase | Mode | Distance | Span | Height |
|---|---|---|---|---|---|
| H01 | Perch | 0 | 6.91 m | 3.47 m | 2.33 m |
| H02 | Perch | 0 | 6.97 m | 3.42 m | 2.36 m |
| H03 | Perch | 0 | 6.93 m | 3.45 m | 2.34 m |
| H04 | Glide | 1 | 7.10 m | 1.86 m | 3.20 m |
| H05 | Glide | 1 | 6.55 m | 3.96 m | 2.52 m |
| H06 | Glide | 1 | 6.48 m | 4.67 m | 3.21 m |
| H07 | Dive | 3 | 5.77 m | 2.84 m | 2.84 m |
| H08 | Grab | 6 | 3.35 m | 2.63 m | 2.78 m |
| H09 | Grab | 6 | 2.75 m | 3.76 m | 3.40 m |
| H10 | Feint | 1 | 3.59 m | 4.73 m | 3.20 m |

## Frame times

Unscaled `Time.deltaTime`, measured in the player.

| Window | Frames | Mean | p95 | Max |
|---|---|---|---|---|
| S1 great hall, 3 s held view | 270 | 11.11 ms | 11.13 ms | 11.16 ms |
| Hunt, clock 0 to 10 s (includes 10 PNG encodes and writes on the main thread) | 882 | 13.06 ms | 11.14 ms | 222.05 ms |

## Pixel checks

Downscaled to 640x360. Magenta = R>200, B>200, G<80. Dark = luma < 8. Warm = R > 1.25 G, G > 1.1 B and R > 40.

| Capture | Magenta px | Dark % | Mean luma | Warm % |
|---|---|---|---|---|
| S1 | 0 | 10.9 | 43.2 | 27.7 |
| S2 | 0 | 21.4 | 22.8 | 17.5 |
| S3 | 0 | 10.2 | 35.7 | 12.5 |
| S4 | 0 | 5.1 | 29.5 | 0.6 |
| S5 | 0 | 29.2 | 17.5 | 4.4 |
| S6 | 0 | 30.8 | 16.9 | 2.0 |
| S7 | 0 | 20.0 | 68.3 | 22.4 |
| S8 | 0 | 7.1 | 98.2 | 41.6 |
| H01 to H10 | 0 | 13.0 to 31.5 | 23.7 to 40.4 | 22.6 to 24.4 |
