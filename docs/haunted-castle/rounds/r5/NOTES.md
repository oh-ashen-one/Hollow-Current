# Round 5 captures: Haunted Castle

Facts only. Judging them is the critic's job.

- **Captures:** `/Users/midir/hollow-current-haunted-castle/docs/haunted-castle/rounds/r5/captures/` (gitignored), with `capture.json`.
- **Player build:** `/Users/midir/hollow-current-haunted-castle/Builds/macOS-Castle/Hollow Current Castle.app`, made by `BuildPreparedMacCastle` from commit `c9191f5`.
  - **Merged since round 4:** hc/count5 `81c47cb`, hc/look5 `b14eef7`, hc/arch5 `79c7c1d`.
  - **Count import:** re-run. 56 bones, grab reach 0.882 m.
  - **Castle bake:** 986 static renderers, 6 lightmaps, 197 probes, 17 reflections.
  - **Bellwether:** re-baked (4 / 124 / 9). The combined build `/Users/midir/hollow-current-haunted-castle/Builds/macOS/Hollow Current.app` succeeded.
- **Gate after arch5:** `ValidateCastleGeometry` passes: 9 routes, and the hunter loads. All 23 doors swing 75–175°, the same distribution as round 4.
- **Capture command:** `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-capture -- ".../Hollow Current Castle.app/Contents/MacOS/Hollow Current" -hc-capture /Users/midir/hollow-current-haunted-castle/docs/haunted-castle/rounds/r5/captures`. Slot hold 44.3 s, exit 0.
- **Machine:** Mac Studio M3 Ultra, Unity 6000.3.20f1 Mono player.
- **Display:** full-screen window at 2560x1440 on a 3440x1440 display. All 18 PNGs are 2560x1440.
- **Quality:**
  - "Ultra" quality level, MSAA 4x, shadows Medium at 16 m, target 90 fps, vSync 0.
  - Camera FOV 72°, except S4 at 95°.
- **Active components:** `CastleAtmosphere`, `CastleLightTuning`.
- **Capture setup:**
  - 3 s warm-up.
  - HUD off for S1 to S6 and the H frames; investigator input disabled.
  - Flashlight off for S1 to S4; on for S5, S6 and the hunt.

## Shot list

| Shot | Eye (x, y, z) | Look-at (x, y, z) | Staging |
|---|---|---|---|
| S1 great hall | 0, 1.64, 14.70 | 0, 2.60, 27.00 | inside the hall great doors |
| S2 chapel altar | 7.00, 1.64, 6.50 | 13.00, 1.75, 6.50 | nave aisle |
| S3 crypt | -5.20, -1.56, 25.00 | 0, -2.60, 22.50 | crypt side of the service-stair door |
| S4 exterior sky | 0, 3.50, -14.00 | pitch 18°, vertical FOV 95° | drawbridge snapped lowered. Framing search found no pitch/FOV meeting all constraints, so the fallback pitch 18° / FOV 95° was used |
| S5 lord perch | 0, 5.50, 24.23 | 0, 6.47, 26.82 | `StageOnRoseLedge(true)`, then `RoseLedgeFraming(0.58)` |
| S6 lord sheet | 0.40, 1.15, 5.60 | 0.40, 1.10, 2.20 | gatehouse, facing the gate. Count feet (-0.2, 0, 2.2), Mode 2. 1.80 m `ScaleReference` at (1.05, 0, 2.2) |
| S7 title | n/a | n/a | castle selected |
| S8 journal | n/a | n/a | "Ledger: the steward" |
| H01 to H10 | 0, 1.64, 15.60 | tracks the Count | staged hunt; waits 3 s, then 6.0 s for interference to end; then `BeginCaptureChoreography(1)`, with shot k taken at `CaptureClock` = k |

## Log lines

From the player log (`/Users/midir/hollow-current-haunted-castle/.capture.log`):

```
[CastleLightTuning] tuned 61 lights
HC_FLAME_CHECK scene material: shader=HollowCurrent/CastleFlame tex=Surreal_Flame 64x128 RGBA32 readable=False | runtime material: shader=HollowCurrent/CastleFlame tex=Surreal_Flame 64x128 RGBA32 readable=False | queue=3001
HC_CAPTURE S4 search fallback
HC_CAPTURE S4 pale_moon inFrame=True occluded=False viewport=(0.38,0.91) red_moon inFrame=True occluded=False viewport=(0.68,0.79)
HC_LEAN_CHECK flame leans 24 deg off its candle toward the target at 6.1 m (floater bend 0.44, screen lean 24 deg)
HC_CAPTURE_FLOATERS_HIDDEN 4 eye=(0.00, 5.50, 24.23) target=(0.00, 6.47, 26.82)
HC_CAPTURE_FLOATERS_HIDDEN 0 (both eye-to-moon lines)
HC_S5_FRAMING eye=(0.00, 5.50, 24.23) target=(0.00, 6.47, 26.82) pitch=20.7 distance=2.77 moons=2 rise=0.06
HC_CAPTURE hunt interference waited 6.0 s
HC_COUNT_CAPTURE begin perch=1 feet=(3.22, 4.25, 20.51) glideEnd=(0.97, 2.20, 21.01) hold=(0.49, 1.60, 18.36) holdDistance=2.80 eye=(0.00, 1.64, 15.60)
HC_COUNT_CAPTURE phase=Perch clock=0.01 mode=0 distance=6.89
HC_COUNT_CAPTURE phase=Glide clock=3.50 mode=1 distance=6.89
HC_COUNT_CAPTURE phase=Dive  clock=6.51 mode=3 distance=5.72
HC_COUNT_CAPTURE phase=Grab  clock=8.01 mode=6 distance=2.96
HC_COUNT_CAPTURE end clock=10.01
```

From the castle bake log (`/Users/midir/hollow-current-haunted-castle/.castle-bake.log`; the layout is built in the editor, not at runtime):

```
HC_SKELETON_TABLE bone samples over the table footprint=68 min bone y=1.53 table top=0.95 clearance=0.58 m, lift applied=0.00
HC_ARCH_KIT wall and floor tiles placed=263 (stone variants, art-backed veneer; colliders unchanged)
```

Not logged in the capture run:
- `HC_CAPTURE_CAMERA_PUSHED`
- `[CastleArt] missing` lines
- exceptions
- an `HC_COUNT_CAPTURE` phase after Grab; H10 is logged as phase=Grab.

No two H frames share an md5.

Castle headless boot (`-batchmode -nographics`, Null device):
- `[CastleArt] missing CastleEmissive`, `CastleLit` and `CastleTriplanar` ("shader not supported", Standard fallback).
- `HC_STARTUP_READY`.

Bellwether headless boot: `HC_STARTUP_READY`, 49 clips.

## Hunt sequence

From capture.json. Distance runs from the Count's bounds centre to the eye.

| Frame | Phase | Mode | Distance | Span | Height |
|---|---|---|---|---|---|
| H01 | Perch | 0 | 6.91 m | 3.47 m | 2.33 m |
| H02 | Perch | 0 | 6.97 m | 3.42 m | 2.36 m |
| H03 | Perch | 0 | 6.93 m | 3.45 m | 2.34 m |
| H04 | Glide | 1 | 7.26 m | 3.36 m | 3.13 m |
| H05 | Glide | 1 | 6.72 m | 1.92 m | 2.75 m |
| H06 | Glide | 1 | 6.30 m | 3.03 m | 3.10 m |
| H07 | Dive | 3 | 6.12 m | 2.84 m | 2.81 m |
| H08 | Grab | 6 | 3.65 m | 2.61 m | 2.79 m |
| H09 | Grab | 6 | 3.20 m | 4.26 m | 3.01 m |
| H10 | Grab | 6 | 2.52 m | 4.26 m | 3.01 m |

## Frame times

Unscaled `Time.deltaTime`, measured in the player.

| Window | Frames | Mean | p95 | Max |
|---|---|---|---|---|
| S1 great hall, 3 s held view | 270 | 11.15 ms | 11.26 ms | 15.27 ms |
| Hunt, clock 0 to 10 s (includes 10 PNG encodes and writes on the main thread) | 882 | 13.27 ms | 11.15 ms | 233.06 ms |

The S1 mean is under the 14 ms limit. One S1 frame (the max) was above it.

## Vault check (look5 request)

- In S1's top 30% (downscaled to 640x360), 13.7% of pixels have luma < 8.
- The threshold for raising the vault fill was 60%, so `VaultFillIntensity` was left at 0.60.

## Pixel checks

Downscaled to 640x360. Magenta = R>200, B>200, G<80. Dark = luma < 8. Warm = R > 1.25 G, G > 1.1 B and R > 40.

| Capture | Magenta px | Dark % | Mean luma | Warm % |
|---|---|---|---|---|
| S1 | 0 | 6.3 | 42.8 | 21.9 |
| S2 | 0 | 34.2 | 23.2 | 23.5 |
| S3 | 0 | 3.1 | 31.4 | 38.1 |
| S4 | 0 | 9.5 | 29.9 | 0.5 |
| S5 | 0 | 36.0 | 15.5 | 0.2 |
| S6 | 0 | 32.2 | 16.8 | 2.0 |
| S7 | 0 | 20.0 | 68.3 | 22.4 |
| S8 | 0 | 7.1 | 98.2 | 41.6 |
| H01 to H10 | 0 | 7.0 to 13.6 | 25.1 to 36.2 | 17.0 to 21.9 |
