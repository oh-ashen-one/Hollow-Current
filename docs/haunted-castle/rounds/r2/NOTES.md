# Round 2 captures: Haunted Castle

Facts only. Judging them is the critic's job.

- **Captures:** `/Users/midir/hollow-current-haunted-castle/docs/haunted-castle/rounds/r2/captures/` (gitignored). `capture.json` there holds the cameras, notes, settings and frame times written by the player.
- **Build:** `/Users/midir/hollow-current-haunted-castle/Builds/macOS-Castle/Hollow Current Castle.app` from commit `36393a2`, built with `HollowCurrent.Editor.NativeBuild.BuildPreparedMacCastle`. Castle receipt: 955 static renderers, 4 lightmaps, 196 probes, 17 reflections. Merged into it: hc/look `6fbd8cf`, hc/dressing `52bd472`, hc/look2 `10a9a79`, hc/count2 `851a769`. `CountClaudeImport.Prepare` was re-run (46 bones, 7 clips).
- **Command:** `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-capture -- ".../Hollow Current Castle.app/Contents/MacOS/Hollow Current" -hc-capture /Users/midir/hollow-current-haunted-castle/docs/haunted-castle/rounds/r2/captures`. Slot hold 36.7 s, exit 0.
- **Machine:** Mac Studio M3 Ultra, Unity 6000.3.20f1 Mono player, built-in render pipeline.
- **Window:** full-screen window at 2560x1440 on a 3440x1440 display. All 18 PNGs are 2560x1440.
- **Quality:** level "Ultra", MSAA 4x, shadow resolution Medium, shadow distance 16 m, `Application.targetFrameRate` 90, vSync 0. The frame-time floor is therefore 11.1 ms. Camera FOV 72°.
- **Atmosphere:** `CastleAtmosphere` (hc/look) is active.
- **Capture setup:**
  - 3 s warm-up after load.
  - HUD off for S1 to S6 and the H frames. Investigator input disabled.
  - Field kit not collected for S1 to S4 (flashlight off).
  - Flashlight on at its in-game intensity for S5, S6 and the hunt.

## Shot list

| Shot | Reference | Eye (x, y, z) | Look-at (x, y, z) | Staging |
|---|---|---|---|---|
| S1 great hall | refs/great_hall.png | 0, 1.64, 14.70 | 0, 2.60, 27.00 | inside the hall great doors |
| S2 chapel altar | refs/chapel_altar.png | 7.00, 1.64, 6.50 | 13.00, 1.75, 6.50 | nave aisle |
| S3 crypt | refs/crypt.png | -5.20, -1.56, 25.00 | 0, -2.60, 22.50 | crypt side of the service-stair door |
| S4 exterior sky | refs/exterior_sky.png | 0, 1.64, -9.50 | 3.48, 4.26, 40.31 | drawbridge snapped lowered. Eye chosen by search: first eye walking back from the bank edge with both moons in frame, no wall hit on the moon rays, gate and bridge end in the lower half. Result z = -9.5, pitch 3° |
| S5 lord perch | refs/lord_perch.png | -1.43, 3.79, 19.83 | -3.26, 4.93, 20.59 | `Presence.StageForCapture(0)` returned true. Feet at (-3.22, 4.25, 20.51), i.e. the west balustrade, perch 0 point (-4.40, 3.20, 21.00). Camera from `Presence.CaptureFraming(0.5, 30°)`. Floating flames targeted at the Count |
| S6 lord sheet | refs/lord_sheet.png | -2.40, 1.15, 7.40 | -2.40, 1.10, 11.00 | Count feet (-3, 0, 11), Mode 2. `Presence.ScaleReference` 1.80 m figure at (-1.75, 0, 11) |
| S7 title | (title) | n/a | n/a | castle selected; Bellwether entry disabled (scene not in this castle-only build) |
| S8 journal | (journal) | n/a | n/a | note view on "Ledger: the steward" |
| H01 to H10 | (hunt) | 0, 1.64, 15.60 | 1.50, 3.00, 24.00 | staged hunt: evidence x2, warning skipped with `Case.Tick(9.05)`, hunt origin (4.6, 0, 26.8); one frame per second |

## Hunt sequence

Hunter positions at each frame, from capture.json. Distance is to the investigator's feet.

| Frame | Hunter (x, y, z) | Distance |
|---|---|---|
| H01 | 3.27, 4.25, 20.47 | 7.24 m |
| H02 | 5.12, 4.35, 17.63 | 7.01 m |
| H03 | 7.21, 3.95, 17.47 | 8.43 m |
| H04 | 8.50, 3.95, 21.17 | 10.91 m |
| H05 | 7.76, 3.95, 24.36 | 12.35 m |
| H06 | 7.30, 2.48, 21.23 | 9.55 m |
| H07 | 6.92, 1.24, 18.32 | 7.54 m |
| H08 | 6.92, 0.05, 18.32 | 7.44 m |
| H09 | 6.92, 0.05, 18.32 | 7.44 m |
| H10 | 7.29, 1.90, 21.16 | 9.36 m |

- Phase was Hunt in all ten frames.
- No two H frames share an md5.

Player log (`/Users/midir/hollow-current-haunted-castle/.capture.log`):
- Count lines present:
  - `HC_COUNT_STAGED perch=0 point=(-4.40, 3.20, 21.00) feet=(-3.22, 4.25, 20.51) yaw=113`
  - `HC_HUNT_SPAWN position=(3.22, 4.25, 20.51) ... flight=Perched perch=1 mode=0`
  - `HC_COUNT_DIVE position=(3.22, 4.25, 20.51) ... seen=True memory=True flight=Chase perch=1 mode=0`
- Not present: `HC_COUNT_FEINT` and `HC_CAPTURE_CAMERA_PUSHED`. The closest recorded approach is 7.01 m; the configured capture hold distance is 2.5 m.
- No exceptions.
- No `[CastleArt] missing` lines; the CastleTriplanar / CastleLiquid / CastleLit shaders were supported on the Metal device.

## Frame times

Unscaled `Time.deltaTime`, measured in the player.

| Window | Frames | Mean | p95 | Max |
|---|---|---|---|---|
| S1 great hall, 3 s held view | 270 | 11.15 ms | 11.35 ms | 14.08 ms |
| Hunt, 10 s (includes 10 PNG encodes and writes on the main thread) | 906 | 12.87 ms | 11.37 ms | 200.14 ms |

## Pixel checks

Downscaled to 640x360. Magenta = R>200, B>200, G<80; dark = luma < 8.

| Capture | Magenta px | Dark % | Mean luma |
|---|---|---|---|
| S1 | 0 | 0.0 | 52.3 |
| S2 | 0 | 0.0 | 44.2 |
| S3 | 0 | 0.0 | 31.0 |
| S4 | 0 | 0.0 | 40.9 |
| S5 | 0 | 0.0 | 35.4 |
| S6 | 0 | 0.0 | 33.3 |
| S7 | 0 | 20.1 | 67.5 |
| S8 | 0 | 5.5 | 103.5 |
| H01 to H10 | 0 | 0.0 | 33.6 to 38.4 |
