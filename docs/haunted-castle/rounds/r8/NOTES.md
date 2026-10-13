# Round 8 captures: Haunted Castle

Facts only. Judging these is the critic's job.

- **Captures:** `/Users/midir/hollow-current-haunted-castle/docs/haunted-castle/rounds/r8/captures/` (gitignored), with `capture.json`.
- **Build:** `/Users/midir/hollow-current-haunted-castle/Builds/macOS-Castle/Hollow Current Castle.app`, made by `BuildPreparedMacCastle` from commit `b817437`.
- **Merged since round 7:** hc/look8 `8d7c65e`, hc/arch8 `9f6f73e` (its CaptureMode.cs additions were taken), hc/count8 `a6d2cfd`.
- **Count import:** 54 bones, 8 clips, head top 2.5 m, grab reach 1.385 m, no grab-reach warning. The castle log reports `HC_COUNT_GORE applied`.
- **Castle bake:** 1,312 static renderers, 5 lightmaps, 196 probes, 17 reflections. The bake log reports `HC_HALL_AXIS ... PASS`.
- **Bellwether bake:** 4 / 124 / 9. The combined build `/Users/midir/hollow-current-haunted-castle/Builds/macOS/Hollow Current.app` succeeded.
- **Gate:** `ValidateCastleGeometry` passes: 9 routes, the hunter loads (54 bones), 23 of 23 doors are clear.
- **Command:** `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-capture -- ".../Hollow Current Castle.app/Contents/MacOS/Hollow Current" -hc-capture /Users/midir/hollow-current-haunted-castle/docs/haunted-castle/rounds/r8/captures`. Slot held 48.4 s, exit 0.
- **Machine:** Mac Studio M3 Ultra, Unity 6000.3.20f1 Mono player.
- **Resolution:** full-screen window at 2560x1440 on a 3440x1440 display. All 18 PNGs are 2560x1440.
- **Quality:**
  - "Ultra" quality level, MSAA 4x, shadows Medium at 16 m, target 90 fps, vSync 0.
  - Camera FOV 72°; S4 uses 95° and S5 uses 50°.
- **GPU state:** this round the slot read 44–56% `util_before` on most runs; the capture read `util_before=47`, `util_after=19`. External load was present at about half the level of round 6.
- **Superseded first pass:** an earlier r8 pass logged `HC_S5_CANDLES 0 (enabled 0, with a flame in frame 0)`.
  - Cause: `Presence.HideFloatersOnViewLine`, called by `RoseLedgeFraming`, deactivated all 11 floaters on the view lines, including the six `neverHide` ledge candelabras. It does not check `Floater.neverHide`.
  - Fix: since `71208f3`, capture re-activates the `neverHide` floaters after framing (log `S5 neverHide floaters re-shown 6`).
  - The PNGs from that pass were replaced.

## Shot list

| Shot | Eye (x, y, z) | Look-at (x, y, z) | Staging |
|---|---|---|---|
| S1 great hall | 0, 1.64, 14.70 | 0, 2.60, 27.00 | inside the hall great doors |
| S2 chapel altar | 7.00, 1.64, 6.50 | 10° down toward +x | nave aisle |
| S3 crypt | 0, -1.56, 15.40 | 0, -2.30, 24.00 | eye from arch7 |
| S4 exterior sky | 0, 2.50, -14.00 | pitch 27°, FOV 95° | eye/pitch/FOV from the framing search; drawbridge snapped lowered |
| S5 lord perch | -0.01, 6.33, 24.23 | -0.02, 6.52, 26.92 | `StageOnRoseLedge(true)`, then capture's flat framing: pitch 4°, FOV 50°, distance 2.70 m. Framed height 1.46 m = max(head-to-feet 0.94, 0.6 x pose 2.43). Ledge candelabras re-shown |
| S6 lord sheet | 0.40, 1.30, 5.60 | 0.40, 1.25, 2.20 | gatehouse. Count feet (-0.2, 0, 2.2), Mode 2. 1.80 m `ScaleReference` at (1.05, 0, 2.2) |
| S7 title | n/a | n/a | castle selected |
| S8 journal | n/a | n/a | "Ledger: the steward" |
| H01 to H10 | 0, 1.64, 15.60 | tracks the Count | staged hunt: 3 s wait, then 6.0 s more for the interference to end, then `BeginCaptureChoreography(1)`; shot k taken at `CaptureClock` = k |

## S5 candles, set pieces, S4, S6

```
HC_CAPTURE S5 neverHide floaters re-shown 6
HC_S5_CANDLES 6 (enabled 6, with a flame in frame 6)
HC_SETPIECE S1 chandelier 45.3% of frame height (viewport y 0.55..1.00, centre x 0.50)
HC_SETPIECE S1 dais+throne 17.5% of frame height (viewport y 0.34..0.51, centre x 0.50)
HC_SETPIECE S2 altar_sockets 9.2% of frame height (viewport y 0.58..0.68, centre x 0.50)
HC_SETPIECE S3 crypt_vault_arches 41.5% of frame height (viewport y 0.51..0.92, centre x 0.50)
HC_SETPIECE S3 crypt_niches_north 10.6% of frame height (viewport y 0.46..0.57, centre x 0.50)
HC_SETPIECE S4 keep_block 21.6% of frame height (viewport y 0.36..0.58, centre x 0.50)
HC_SETPIECE S4 plank_deck 14.3% of frame height (viewport y 0.01..0.15, centre x 0.50)
HC_CAPTURE S4 search eye=0.00,2.50,-14.00 pitch=27 fov=95
HC_CAPTURE S4 pale_moon inFrame=True occluded=False viewport=(0.40,0.80) red_moon inFrame=True occluded=False viewport=(0.66,0.70)
HC_CAPTURE S6 framing countHeadTop=(0.57,0.75) countFeet=(0.57,0.25) figureTop=(0.43,0.61) figureFeet=(0.43,0.25) headTopY=2.43
```

## Lean read-backs with the wick-position check

- Every line names `HollowCurrent/CastleFlame` without "(NOT SUPPORTED)" and has a non-empty property block.
- "moved" is the wick's screen displacement between the unleaned and leaned flame.
- Screen coordinates are in 2560x1440 pixels; values outside that range are off-screen.

```
S1  candle 1 at 8.2 m: wick (1914.3,673.5) moved 0.00 px; tip tilted 20 deg
S1  candle 2 at 8.4 m: wick (1960.8,673.5) moved 0.00 px; tip tilted 20 deg
S1  candle 3 at 8.5 m: wick (1923.3,656.7) moved 0.00 px; tip tilted 20 deg
S5  candle 1 at 2.7 m: wick (2059.8,347.4) moved 0.00 px; tip tilted 44 deg, tip-to-wick 155 px (unleaned 144); _LeanAmount=45.0 _LeanDir=(-0.78,0.48,0.40)
S5  candle 2 at 2.6 m: wick (343.3,175.1)  moved 0.00 px; tip tilted 43 deg, 211 px (unleaned 190); _LeanAmount=45.0 _LeanDir=(0.72,0.51,0.47)
S5  candle 3 at 2.9 m: wick (2592.1,330.9) moved 0.00 px; tip tilted 47 deg, 215 px (unleaned 184); _LeanAmount=45.0 _LeanDir=(-0.86,0.34,0.37)
H05 candle 1 at 2.7 m: wick (-185.1,2595.9)  moved 0.00 px; tip tilted 30 deg (off-screen)
H05 candle 2 at 2.8 m: wick (-1131.3,1866.1) moved 0.00 px; tip tilted 27 deg (off-screen)
H05 candle 3 at 2.6 m: wick (-1305.1,5357.0) moved 0.00 px; tip tilted 9 deg (off-screen)
```

## Hunt choreography

```
HC_COUNT_CAPTURE begin perch=1 feet=(3.22, 4.25, 20.51) glideEnd=(0.97, 2.20, 21.01) hold=(0.49, 1.25, 18.36) holdDistance=2.80 eye=(0.00, 1.64, 15.60)
HC_COUNT_CAPTURE phase=Perch clock=0.01 mode=0 distance=6.89
HC_COUNT_CAPTURE phase=Glide clock=3.51 mode=1 distance=6.89
HC_COUNT_CAPTURE phase=Dive  clock=6.51 mode=3 distance=5.72
HC_COUNT_CAPTURE phase=Grab  clock=8.01 mode=6 distance=2.87
HC_COUNT_CAPTURE end clock=10.01
```

Per-frame values from capture.json (distance runs from the Count's bounds centre to the eye):

| Frame | Phase | Mode | Distance | Span | Height |
|---|---|---|---|---|---|
| H01 | Perch | 0 | 7.08 m | 3.29 m | 2.42 m |
| H02 | Perch | 0 | 7.12 m | 3.23 m | 2.44 m |
| H03 | Perch | 0 | 7.10 m | 3.27 m | 2.42 m |
| H04 | Glide | 1 | 7.72 m | 3.64 m | 3.18 m |
| H05 | Glide | 1 | 7.44 m | 2.95 m | 3.15 m |
| H06 | Glide | 1 | 6.89 m | 3.92 m | 3.18 m |
| H07 | Dive | 3 | 6.08 m | 2.58 m | 2.99 m |
| H08 | Grab | 6 | 3.48 m | 2.30 m | 2.99 m |
| H09 | Grab | 6 | 3.03 m | 4.94 m | 3.90 m |
| H10 | Grab | 6 | 2.80 m | 4.94 m | 3.90 m |

- No `HC_CAPTURE_CAMERA_PUSHED` lines this round. No two H frames share an md5.
- Capture run: no `[CastleArt] missing` lines and no exceptions.
- Headless boots (Null device):
  - **Castle:** `[CastleArt] missing CastleEmissive / CastleGore / CastleLit / CastleTriplanar` (Standard fallback), `HC_COUNT_GORE applied`, `HC_STARTUP_READY`.
  - **Bellwether:** `HC_STARTUP_READY`, 49 clips.

## Frame times

External GPU load was at about 50% during this round (see above).

| Window | Frames | Mean | p95 | Max |
|---|---|---|---|---|
| S1 great hall, 3 s held view | 271 | 11.11 ms | 11.11 ms | 11.12 ms |
| Hunt, clock 0 to 10 s (includes 10 PNG encodes and writes on the main thread) | 878 | 13.21 ms | 11.16 ms | 221.77 ms |

## Exposure report

`python3 tools/exposure_report.py docs/haunted-castle/rounds/r8/captures --prev docs/haunted-castle/rounds/r7/captures`

```
capture                   mean  dark<8 %  warm %     p1  verdict    next-round step (dark %)   prev
H01_hunt_dive             27.3      19.5    11.1    1.1  ok            18.8 (+0.7)
H02_hunt_dive             27.3      19.4    11.1    1.1  ok            18.8 (+0.7)
H03_hunt_dive             27.3      19.4    11.1    1.1  ok            18.8 (+0.6)
H04_hunt_dive             27.1      20.0    11.0    1.1  ok            19.3 (+0.7)
H05_hunt_dive             26.8      21.4    12.1    1.1  ok            20.5 (+0.9)
H06_hunt_dive             31.1      17.4    10.1    1.1  ok            16.8 (+0.6)
H07_hunt_dive             34.5      15.2    11.0    1.1  ok            14.9 (+0.4)
H08_hunt_dive             35.4      14.9    11.7    1.1  ok            14.4 (+0.4)
H09_hunt_dive             33.4      17.1    17.9    1.1  ok            17.1 (-0.1)
H10_hunt_dive             33.1      17.8    18.6    1.1  ok            17.4 (+0.5)
S1_great_hall             41.2      11.7    11.0    1.3  TOO BRIGHT +4.2 -> aim 15.8   11.6 (+0.1)
S2_chapel_altar           27.1      20.8    12.9    1.6  ok            20.6 (+0.3)
S3_crypt                  25.4      22.1     9.8    1.9  ok            23.1 (-1.0)
S4_exterior_sky           30.8       5.0     0.4    2.8  TOO BRIGHT +7.5 -> aim 12.5   6.2 (-1.2)
S5_lord_perch             25.8      14.2     7.7    1.1  ok            15.1 (-0.9)
S6_lord_sheet             16.8      32.9     3.9    1.4  TOO DARK   -6.4 -> aim 26.4   34.2 (-1.3)
S7_title                  68.3      20.7    28.3    5.8  ui            20.7 (-0.0)
S8_journal_ledger         98.2       7.1    56.3    2.6  ui            7.1 (+0.0)
in band: 13 / 16 judged
```
