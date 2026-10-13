# Round 6 captures: Haunted Castle

Facts only. Judging these is the critic's job.

- **Captures:** `/Users/midir/hollow-current-haunted-castle/docs/haunted-castle/rounds/r6/captures/` (gitignored), with `capture.json`.
- **Player:** `/Users/midir/hollow-current-haunted-castle/Builds/macOS-Castle/Hollow Current Castle.app`, built by `BuildPreparedMacCastle` from commit `eff4866`.
- **Merged since round 5:** hc/hall6 `2c42157`, hc/look6 `e4d29e2`, hc/arch6 `b7e9892`, hc/count6 `849f284`. The Count mesh is unchanged from round 5 (2.15 m, 56 bones): the demon 3D jobs were refused by moderation.
- **Count import:** re-run. 8 clips, Crawl included. Grab reach 1.704 m.
- **Castle bake:** 1,247 static renderers, 7 lightmaps, 196 probes, 17 reflections.
- **Bake log:** `HC_HALL_AXIS table centre x=0.000 rune ring centre x=0.000 skeleton max x analytic=-4.38 measured=-4.39 (limit -3.0) PASS`.
- **Bellwether:** re-baked (4 / 124 / 9). The combined build `/Users/midir/hollow-current-haunted-castle/Builds/macOS/Hollow Current.app` succeeded.
- **Gate:** `ValidateCastleGeometry` passes: 9 routes, the hunter loads, 23 of 23 doors clear.
- **Command:** `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-capture -- ".../Hollow Current Castle.app/Contents/MacOS/Hollow Current" -hc-capture /Users/midir/hollow-current-haunted-castle/docs/haunted-castle/rounds/r6/captures`. Slot hold 46.7 s, exit 0.
- **Machine:** Mac Studio M3 Ultra, Unity 6000.3.20f1 Mono player.
- **Window:** full-screen window at 2560x1440 on a 3440x1440 display. All 18 PNGs are 2560x1440.
- **Quality:**
  - Settings: "Ultra", MSAA 4x, shadows Medium at 16 m, target 90 fps, vSync 0.
  - Camera FOV 72°. S4 uses 95°; S5 uses 50°.
- **Active components:** `CastleAtmosphere` and `CastleLightTuning`. Its fill lights are ForcePixel since look6.

## GPU state during this round

The GPU lock reported the GPU busy from another session for every run this round. My Unity was the only instance in this project. `util_before` and `util_after` are the slot's own readings:

| Run | util_before | util_after |
|---|---|---|
| Count import | 98% | 99% |
| Route check | 100% | 99% |
| Castle bake | 100% | 100% |
| Bellwether bake | 100% | 100% |
| Castle build | 97% | 99% |
| Combined build | 100% | 98% |
| **Capture** | **100%** | **99%** |

The frame times below are therefore contaminated by external GPU load and are not comparable with rounds 1 to 5. Because of this, no light was switched to "Important" this round.

## Shot list

| Shot | Eye (x, y, z) | Look-at (x, y, z) | Staging |
|---|---|---|---|
| S1 great hall | 0, 1.64, 14.70 | 0, 2.60, 27.00 | inside the hall great doors |
| S2 chapel altar | 7.00, 1.64, 6.50 | 13.00, 1.75, 6.50 | nave aisle |
| S3 crypt | -5.20, -1.56, 25.00 | 0, -2.60, 22.50 | crypt side of the service-stair door |
| S4 exterior sky | 0, 3.50, -14.00 | pitch 22.5° up | FOV 95°, chosen by the search (below). Drawbridge snapped lowered |
| S5 lord perch | 0, 6.17, 22.48 | 0, 6.47, 26.82 | `StageOnRoseLedge(true)` + `RoseLedgeFraming(0.58)`, then flattened by capture to pitch 4°, FOV 50°, distance 4.34 m. Pose height 2.35 m, fill 0.58 of that height. 2 moon sprites in frame |
| S6 lord sheet | 0.40, 1.15, 5.60 | 0.40, 1.10, 2.20 | gatehouse, facing the gate. Count feet (-0.2, 0, 2.2), Mode 2. 1.80 m `ScaleReference` at (1.05, 0, 2.2) |
| S7 title | n/a | n/a | castle selected |
| S8 journal | n/a | n/a | "Ledger: the steward" |
| H01 to H10 | 0, 1.64, 15.60 | tracks the Count | staged hunt. After a 3 s wait, a further 6.0 s for the interference to end, then `BeginCaptureChoreography(1)`. Shot k at `CaptureClock` = k |

## Log lines

From `/Users/midir/hollow-current-haunted-castle/.capture.log`.

Light, flame and S4/S5 framing:

```
[CastleLightTuning] tuned 61 lights
HC_FLAME_CHECK scene material: shader=HollowCurrent/CastleFlame tex=Surreal_Flame 64x128 RGBA32 readable=False | runtime material: shader=HollowCurrent/CastleFlame tex=Surreal_Flame 64x128 RGBA32 readable=False | queue=3001
HC_CAPTURE S4 search eye=0.00,3.50,-14.00 pitch=22.5 fov=95
HC_CAPTURE S4 pale_moon inFrame=True occluded=False viewport=(0.39,0.85) red_moon inFrame=True occluded=False viewport=(0.67,0.74)
HC_CAPTURE_FLOATERS_HIDDEN 8 eye=(0.00, 5.80, 24.23) target=(0.00, 6.47, 26.82)
HC_S5_FRAMING eye=(0.00, 5.80, 24.23) target=(0.00, 6.47, 26.82) pitch=14.5 distance=2.68 moons=2 rise=0.36
HC_CAPTURE S5 rose ledge, RoseLedgeFraming(0.58) -> flattened: pitch 4 deg, FOV 50, distance 4.34 m, pose height 2.35 m, floaters hidden 0 eye=0.00,6.17,22.48 target=0.00,6.47,26.82 moonSpritesInFrame=2
```

Hunt choreography:

```
HC_CAPTURE hunt interference waited 6.0 s
HC_COUNT_CAPTURE begin perch=1 feet=(3.22, 4.25, 20.51) glideEnd=(0.97, 2.20, 21.01) hold=(0.49, 1.25, 18.36) holdDistance=2.80 eye=(0.00, 1.64, 15.60)
HC_COUNT_CAPTURE phase=Perch clock=0.01 mode=0 distance=6.89
HC_COUNT_CAPTURE phase=Glide clock=3.51 mode=1 distance=6.89
HC_COUNT_CAPTURE phase=Dive  clock=6.50 mode=3 distance=5.72
HC_COUNT_CAPTURE phase=Grab  clock=8.01 mode=6 distance=2.87
HC_CAPTURE_CAMERA_PUSHED by 0.10 (x3, during Grab; player z 15.60 -> 15.30)
HC_COUNT_CAPTURE end clock=10.01
```

Per-candle lean lines (`CandleFlame.LogLeanReport`; the "auto" lines are logged by the prop script itself before S1):

```
auto candle 1 at 4.8 m: quad tilted 49 deg (quad 49, candle 0 from screen vertical; applied lean 3)
auto candle 2 at 4.9 m: quad tilted 41 deg (quad 41, candle 0; applied lean 3)
auto candle 3 at 4.9 m: quad tilted 44 deg (quad -44, candle 0; applied lean 3)
auto candle 1..3 at 8.2-8.5 m: quad tilted 20 deg (quad 163, candle -177; applied lean 17)   [logged twice]
S1  candle 1 at 8.4 m: 20 deg (quad 163, candle -177; applied 17)
S1  candle 2 at 8.2 m: 20 deg (quad 163, candle -177; applied 17)
S1  candle 3 at 8.5 m: 20 deg (quad 163, candle -177; applied 17)
S5  candle 1 at 2.0 m: 28 deg (quad 151, candle 180; applied 29)
S5  candle 2 at 1.6 m: 29 deg (quad -150, candle -179; applied 29)
S5  candle 3 at 2.1 m: 29 deg (quad -151, candle -179; applied 29)
H05 candle 1 at 2.6 m: 9 deg (quad -147, candle -138; applied 35)
H05 candle 2 at 2.8 m: 14 deg (quad -136, candle -121; applied 35)
H05 candle 3 at 2.6 m: 30 deg (quad -127, candle -97; applied 35)
```

Absent from the capture log: `[CastleArt] missing` lines and exceptions. No two H frames share an md5.

Headless boots (`-batchmode -nographics`, Null device):
- **Castle:** `[CastleArt] missing CastleEmissive / CastleLit / CastleTriplanar` (shader not supported, Standard fallback), then `HC_STARTUP_READY`.
- **Bellwether:** `HC_STARTUP_READY`, 49 clips.

## Hunt sequence

From capture.json. Distance is from the Count's bounds centre to the eye.

| Frame | Phase | Mode | Distance | Span | Height |
|---|---|---|---|---|---|
| H01 | Perch | 0 | 6.92 m | 3.46 m | 2.33 m |
| H02 | Perch | 0 | 6.97 m | 3.41 m | 2.36 m |
| H03 | Perch | 0 | 6.91 m | 3.47 m | 2.33 m |
| H04 | Glide | 1 | 7.36 m | 3.84 m | 3.08 m |
| H05 | Glide | 1 | 6.92 m | 3.10 m | 2.77 m |
| H06 | Glide | 1 | 6.53 m | 4.15 m | 3.08 m |
| H07 | Dive | 3 | 6.13 m | 2.79 m | 2.80 m |
| H08 | Grab | 6 | 3.54 m | 2.82 m | 2.81 m |
| H09 | Grab | 6 | 2.34 m | 3.73 m | 3.40 m |
| H10 | Grab | 6 | 2.39 m | 3.74 m | 3.40 m |

## Frame times (contaminated: GPU at 97–100% from another session)

Unscaled `Time.deltaTime`, measured in the player.

| Window | Frames | Mean | p95 | Max |
|---|---|---|---|---|
| S1 great hall, 3 s held view | 177 | 17.14 ms | 55.31 ms | 111.18 ms |
| Hunt, clock 0 to 10 s (includes 10 PNG encodes and writes on the main thread) | 671 | 19.18 ms | 49.91 ms | 316.48 ms |

## Pixel checks

Downscaled to 640x360. Magenta = R>200, B>200, G<80. Dark = luma < 8. Warm = R > 1.25 G, G > 1.1 B and R > 40.

| Capture | Magenta px | Dark % | Mean luma | Warm % |
|---|---|---|---|---|
| S1 | 0 | 14.0 | 35.0 | 14.5 |
| S2 | 0 | 21.8 | 27.5 | 24.9 |
| S3 | 0 | 40.6 | 12.4 | 2.6 |
| S4 | 0 | 2.6 | 34.1 | 0.4 |
| S5 | 0 | 23.1 | 23.0 | 1.6 |
| S6 | 0 | 44.0 | 14.4 | 3.3 |
| S7 | 0 | 20.0 | 68.3 | 22.4 |
| S8 | 0 | 7.1 | 98.2 | 41.6 |
| H01 to H10 | 0 | 16.7 to 22.4 | 21.6 to 31.3 | 14.1 to 17.4 |
