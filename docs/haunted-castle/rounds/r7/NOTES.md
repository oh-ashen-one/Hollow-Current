# Round 7 captures: Haunted Castle

Facts only. Judging these is the critic's job.

- **Captures:** `/Users/midir/hollow-current-haunted-castle/docs/haunted-castle/rounds/r7/captures/` (gitignored), with `capture.json`.
- **Build:** `/Users/midir/hollow-current-haunted-castle/Builds/macOS-Castle/Hollow Current Castle.app`, built by `BuildPreparedMacCastle` from commit `84d1754`.
- **Merged since round 6:** hc/look7 `1a8596d`, hc/arch7 `19431cc`, hc/count7 `7a0419a` (the new demon mesh).
- **Count import:** 54 bones, 8 clips at 550 bindings each, head top 2.5 m, 46,306 triangles, grab reach 1.562 m.
- **Castle log:** `HC_COUNT_GORE applied`.
- **Castle bake:** 1,247 static renderers, 7 lightmaps, 196 probes, 17 reflections. `HC_HALL_AXIS ... PASS`.
- **Bellwether bake:** 4 / 124 / 9. The combined build `/Users/midir/hollow-current-haunted-castle/Builds/macOS/Hollow Current.app` succeeded.
- **Gate:** `ValidateCastleGeometry` passes: 9 routes, the hunter loads (54 bones), and 23 of 23 doors are clear.
- **Command:** `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-capture -- ".../Hollow Current Castle.app/Contents/MacOS/Hollow Current" -hc-capture /Users/midir/hollow-current-haunted-castle/docs/haunted-castle/rounds/r7/captures`. The slot was held 48.2 s, exit 0.
- **Machine:** Mac Studio M3 Ultra, Unity 6000.3.20f1 Mono player.
- **Resolution:** full-screen window at 2560x1440 on a 3440x1440 display; all 18 PNGs are 2560x1440.
- **Quality:**
  - "Ultra" level, MSAA 4x, shadows Medium at 16 m, target 90 fps, vSync 0.
  - Camera FOV 72°, except S4 at 95° and S5 at 50°.
- **GPU state:** the slot read `util_before=0`, `util_after=27` for the capture, and 0–14% for the other runs this round. The external GPU load reported in round 6 was not present.
- **First pass superseded:** an earlier r7 pass was replaced after two capture fixes (`872d175`, see S5 and S6 below), and its PNGs were replaced.

## Shot list

| Shot | Eye (x, y, z) | Look-at (x, y, z) | Staging |
|---|---|---|---|
| S1 great hall | 0, 1.64, 14.70 | 0, 2.60, 27.00 | inside the hall great doors |
| S2 chapel altar | 7.00, 1.64, 6.50 | pitched 10° down toward +x | nave aisle, aimed down at the altar top |
| S3 crypt | 0, -1.56, 15.40 | 0, -2.30, 24.00 | eye from hc/arch7's handoff |
| S4 exterior sky | 0, 2.50, -14.00 | pitch 27°, FOV 95° | eye/pitch/FOV from the search (moon margin = disc + 4%). Drawbridge snapped lowered |
| S5 lord perch | -0.01, 6.33, 24.20 | -0.02, 6.52, 26.90 | `StageOnRoseLedge(true)`, then capture's flat framing: pitch 4°, FOV 50°, distance 2.71 m. Framed height 1.47 m = max(head-to-feet 0.94, 0.6 x pose 2.45), fill 0.58. 2 moon sprites in frame |
| S6 lord sheet | 0.40, 1.30, 5.60 | 0.40, 1.25, 2.20 | gatehouse, facing the gate. Count feet at (-0.2, 0, 2.2), Mode 2. 1.80 m `ScaleReference` at (1.05, 0, 2.2) |
| S7 title | n/a | n/a | castle selected |
| S8 journal | n/a | n/a | "Ledger: the steward" |
| H01 to H10 | 0, 1.64, 15.60 | tracks the Count | staged hunt: 3 s wait, then 6.0 s more for the interference to end, then `BeginCaptureChoreography(1)`; shot k at `CaptureClock` = k |

S6 viewport log, taken after the pose settled: `countHeadTop=(0.57,0.75) countFeet=(0.57,0.25) figureTop=(0.43,0.61) figureFeet=(0.43,0.25) headTopY=2.43`. All four points are inside the frame.

## Lean read-backs

`HollowCurrent/CastleFlame` is listed without the "(NOT SUPPORTED)" suffix the report adds for unsupported shaders. Every property block below is non-empty.

```
HC_FLAME_CHECK ... runtime material: shader=HollowCurrent/CastleFlame tex=Surreal_Flame 64x128 RGBA32 readable=False | queue=3001 | lean shader property _LeanAmount present
S1  candle 1 at 8.4 m: tip tilted 20 deg on screen; shader HollowCurrent/CastleFlame, _LeanAmount=16.7 _LeanDir=(0.52,-0.02,-0.85)
S1  candle 2 at 8.2 m: 20 deg; _LeanAmount=16.7 _LeanDir=(0.50,-0.02,-0.87)
S1  candle 3 at 8.5 m: 20 deg; _LeanAmount=16.7 _LeanDir=(0.52,-0.03,-0.86)
S5  candle 1 at 3.3 m: 29 deg (tip -150, candle -179); _LeanAmount=28.9 _LeanDir=(0.08,0.05,1.00)
S5  candle 2 at 3.3 m: 61 deg (tip 113, candle 174);   _LeanAmount=34.8 _LeanDir=(-0.45,0.31,0.84)
S5  candle 3 at 3.1 m: 39 deg (tip 42, candle 3);      _LeanAmount=51.1 _LeanDir=(-0.57,0.75,0.32)
H05 candle 1 at 2.8 m: 27 deg (tip -105, candle -78);  _LeanAmount=37.0 _LeanDir=(0.65,0.42,0.63)
H05 candle 2 at 2.7 m: 30 deg (tip -128, candle -99);  _LeanAmount=34.7 _LeanDir=(0.56,0.30,0.77)
H05 candle 3 at 2.6 m: 9 deg (tip -148, candle -140);  _LeanAmount=34.7 _LeanDir=(0.51,0.28,0.81)
```

## S4 search

```
HC_CAPTURE S4 search eye=0.00,2.50,-14.00 pitch=27 fov=95
HC_CAPTURE S4 pale_moon inFrame=True occluded=False viewport=(0.40,0.80) red_moon inFrame=True occluded=False viewport=(0.66,0.70)
```

## Hunt choreography

Log lines:

```
HC_COUNT_CAPTURE begin perch=1 feet=(3.22, 4.25, 20.51) glideEnd=(0.97, 2.20, 21.01) hold=(0.49, 1.25, 18.36) holdDistance=2.80 eye=(0.00, 1.64, 15.60)
HC_COUNT_CAPTURE phase=Perch clock=0.01 mode=0 distance=6.89
HC_COUNT_CAPTURE phase=Glide clock=3.50 mode=1 distance=6.89
HC_COUNT_CAPTURE phase=Dive  clock=6.50 mode=3 distance=5.72
HC_COUNT_CAPTURE phase=Grab  clock=8.00 mode=6 distance=2.87
HC_CAPTURE_CAMERA_PUSHED by 0.10 (x2 during Grab; player z 15.60 -> 15.40)
HC_COUNT_CAPTURE end clock=10.01
```

- No two H frames share an md5.
- The capture run logged no `[CastleArt] missing` lines and no exceptions.

Headless boots (`-batchmode -nographics`, Null device):
- **Castle:** `[CastleArt] missing CastleEmissive / CastleGore / CastleLit / CastleTriplanar` (shader not supported, Standard fallback), `HC_COUNT_GORE applied`, `HC_STARTUP_READY`.
- **Bellwether:** `HC_STARTUP_READY`, 49 clips.

## Frame times

Unscaled `Time.deltaTime`, measured in the player.

| Window | Frames | Mean | p95 | Max |
|---|---|---|---|---|
| S1 great hall, 3 s held view | 271 | 11.11 ms | 11.11 ms | 11.12 ms |
| Hunt, clock 0 to 10 s (includes 10 PNG encodes and writes on the main thread) | 882 | 13.10 ms | 11.13 ms | 233.42 ms |

## Exposure report

`python3 tools/exposure_report.py docs/haunted-castle/rounds/r7/captures --prev docs/haunted-castle/rounds/r6/captures`

```
capture                   mean  dark<8 %  warm %     p1  verdict    next-round step (dark %)   prev
H01_hunt_dive             25.6      18.8     9.6    1.1  ok            23.0 (-4.2)
H02_hunt_dive             25.7      18.8     9.6    1.1  ok            23.1 (-4.4)
H03_hunt_dive             25.6      18.8     9.7    1.1  ok            23.0 (-4.2)
H04_hunt_dive             25.6      19.3     9.6    1.1  ok            23.2 (-3.9)
H05_hunt_dive             26.0      20.5    10.9    1.1  ok            23.6 (-3.1)
H06_hunt_dive             30.3      16.8     8.9    1.1  ok            20.5 (-3.7)
H07_hunt_dive             33.4      14.9    10.0    1.1  ok            18.6 (-3.7)
H08_hunt_dive             34.2      14.4    10.6    1.1  ok            17.9 (-3.4)
H09_hunt_dive             30.3      17.1    14.3    1.1  ok            21.5 (-4.4)
H10_hunt_dive             29.9      17.4    14.6    1.1  ok            22.3 (-4.9)
S1_great_hall             39.2      11.6     9.0    1.4  TOO BRIGHT +4.2 -> aim 15.8   14.9 (-3.3)
S2_chapel_altar           27.3      20.6    13.0    1.7  ok            22.8 (-2.2)
S3_crypt                  23.5      23.1     9.5    1.9  ok            41.9 (-18.8)
S4_exterior_sky           30.2       6.2     0.4    2.5  TOO BRIGHT +6.9 -> aim 13.1   3.1 (+3.1)
S5_lord_perch             27.0      15.1     2.9    2.0  ok            23.5 (-8.4)
S6_lord_sheet             15.9      34.2     1.7    1.4  TOO DARK   -7.1 -> aim 27.1   44.8 (-10.6)
S7_title                  68.3      20.7    28.3    5.8  ui            20.7 (+0.0)
S8_journal_ledger         98.2       7.1    56.3    2.6  ui            7.1 (+0.0)
in band: 13 / 16 judged
```
