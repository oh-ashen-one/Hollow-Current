# Automated castle playthroughs: waves G and H (windowed checklist, 2026-10-14)

These are facts from the real combined player. The critic and the owner judge them.

## How the runs were made

- **Launch:** every run went through `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-f0 -- …`, one instance at a time, in a 1920x1080 window (never full screen).
  - Rounds 2 and 3 used `open -g -W -n -a "Hollow Current.app" --args …` so the window opened behind. During the first such launch the front app stayed "Google Chrome" (`lsappinfo front`), and the run logged `HC_CURSOR free`.
- **Driver:** `Assets/HollowCurrent/Runtime/Castle/PlaytestDriver.cs` sends input through `Investigator.TestInput` and teleports between stops. Unless a mode says otherwise, the investigator is invulnerable to the driver only: `blockedDeaths` counts grab frames that would have killed.
- **Forcing:**
  - `fear` forces dice queues (`ForceDice`, hc/rules3) and Dread, and logs each as `HC_PLAY_FORCED`.
  - `siege` holds Dread at 82 or higher.
  - `lowdread` holds Dread below the run's boundary.
  - `natural` forces nothing.
- **Logs (gitignored):** `/Users/midir/hollow-current-haunted-castle/.playtest-<name>.log` for the latest run, plus `.playtest-r1-*.log` and `.playtest-r2-*.log`.
- **Screenshots:** `docs/haunted-castle/playtest/auto/<name>/`, with `r1_*` and `r2_*` for the earlier rounds.

| Round | Build (bake commit) | Source | What changed |
|---|---|---|---|
| r1 | `3173015` | `40ec63d` | wave G merged, proof tooling |
| r2 | `99a09c3` | `e1e360a` | wave H merged; N1, N4, N5, N9; rerun fixes; cursor rule |
| r3 | `a412df7` | `430a266` | driver watched-hide timing; `HC_SCARE_SKIP` logs |

**Throwables check:** `python3 tools/check_throwables_scene.py` exits 0 on all three bakes (0 of 38 throwables batched).

## Proof table

| Proof | r1 | r2 / r3 (latest) | Evidence (latest) |
|---|---|---|---|
| **Escape** route | FAIL (5 blockers: recording suspended by through-wall interference, N4) | **WON 87 s, 0 blockers** (r2) | `.playtest-escape.log` |
| **Kill** route | WON, slain (1 blocker, same N4) | not rerun | `.playtest-r1-kill.log` |
| **Throw + poltergeist** (fear) | PASS: plate, poltergeist 10 | **PASS**: plate, poltergeist 11 (r3) | `HC_THROW prop=Throwable / Plate`, `flung=11` |
| **Lies, silence and duck** (fear) | PASS: HC_LIE 6, HC_DUCK 3 | **PASS**: HC_LIE 10, HC_DUCK 7 (r3) | |
| **False warning** (fear, forced dice) | PASS | **PASS** (r3) | `HC_WARNING false=True`, then `HC_FALSE_WARNING ended` |
| **No-tell hunt, first 6 s** (fear, forced dice) | Partial (wing +0.6, 1 voice line) | **Partial** (r3) | See below |
| **Watched hide, torch off, from a perch** | PASS (he was chasing) | **PASS** (r3; r2 missed it, driver timing) | See below |
| **Near-miss with open** (siege) | **PASS** attempt 1 | not rerun | `HC_COUNT_NEARMISS begin … open=True openRoll=0.22 dread=87`, then `HC_COUNT_NEARMISS open=True` |
| **Recovery cut short** (siege) | **PASS** attempt 1 | not rerun | `HC_PLAY_RECOVERY_CUT to=Warning` |
| **Hostile return: swoop legs** | FAIL (`leg2clear=False`) | **PASS** (r2, r3) | `leg1=clear leg2=detour via (0.00, 8.65, 4.95)` |
| **Hostile return: bridge is lethal** | FAIL (grab chosen 20×, no death) | **PASS** (r2: 11, r3: 31 blocked grabs on the bridge) | `grabsOnBridge=31` |
| **Low-Dread hunt** | not run | **PASS** attempt 1 (r2) | A hunt 72 s into the low-Dread watch, at Dread 21.5 against `RunDreadMin` 43.7 |
| **Death, then checkpoint retry** | **PASS** | not rerun | Deaths 1, penalty true, Dread 59.4, relic 0 moved (`HC_DEATH_PENALTY`, `HC_RELIC_MOVE`) |
| **Long Night** | route FAIL (N4) | **WON** (r2) | `HC_NAIL_HIDES nailed=2`, no-tell hunt on the bell. The one blocker is the driver's expectation of a Warning, which Long Night does not have. |
| **Bellwether `-hc-verify`** | FAIL (Raycast focus Hall coat cupboard) | **PASS** (r2) | `HC_NATIVE_ROUTE_PASS`, after the deliberate Bellwether ray fix in `e1e360a` |
| **Natural run, no forcing** | not won (N4) | **WON** (r2) | r3 (build `a412df7`) was **aborted by the owner** at about 6.4 min (SIGTERM, clean exit, not a crash, not retried). Before that it had 4 hunts and the `HC_SCARE_SKIP` lines below. |
| **Missing audio** | 0 | 0 | `HC_MISSING_ART=0` in fear and natural |

**No-tell hunt, first 6 s, detail (r3):**
- Over 532 frames, nothing changed in the HUD goal, instrument line, house/hunt/safety music, breath, room tone, brazier state or card styling (`changedFrames=0`). There was no rim line and no smell line.
- The only hunt voice line was `cc_hunt_hungry`, and it was held (`HC_COUNT_VOICE held … t=1.9 (first 8 s, not seen)`).
- Heartbeat drift was 0.04: felt Dread following the driver's forced Dread.
- The wing loop rose 0.60 because his first leg was `HC_COUNT_WINGS leg=search loudness=1.0` in both no-tell samples (r2 and r3). A silent first leg did occur, in the r3 told hunt (`leg=search loudness=0.0`), so it is possible but was not observed on a no-tell start.

**Watched hide, torch off, from a perch, detail (r3):**
- `HC_COUNT_WITNESSED hide at` with `flight=Perched`, torch off.
- `begin … open=True reason=watched (any Dread)`, then `landed crawl`, then `hold doorDistance=0.50 opens=True`.
- Then `HC_COUNT_GRAB mode=8 why=hide opened (GrabDrop) hideEntrance=True witnessed=True torch=False` and `grab blocked reason=driver invulnerability`.

## Natural run (r2, build 99a09c3): hunt timeline

| t (s) | Phase | From | Threat from | Dread | Felt | Notes |
|---|---|---|---|---|---|---|
| 104 | Hunt | Disturbance | Disturbance | 18.8 | 22.3 | No-tell. The first-hunt guarantee: below the boundary, before the first hunt |
| 194 | Hunt | Disturbance | Disturbance | 28.7 | 18.8 | No-tell, below the boundary: the low-Dread roll |
| 247 | Hunt | Warning | Recovery | 30.5 | 22.1 | Recovery roll |
| 296 | Hunt | Disturbance | Disturbance | 68.3 | 47.7 | No-tell, dice |
| 386 | Hunt | Warning | Recovery | 74.1 | 76.0 | |
| 454 | Hunt | Warning | Recovery | 100.0 | 94.3 | |
| 530 | Hunt | Warning | Recovery | 100.0 | 99.2 | |
| 586 | Return | Containment | Containment | 93.9 | 74.4 | Hostile return |

- `escalated=False` on every hunt. The run's boundary was 49.9.
- **Why Dread climbed:** the driver stood in the open, invulnerable. Every hunt he saw her (+15) and dove and missed (+20 "grab survived"). A real player would have died early. Dread ≥ 80 then allows Recovery rolls, so the late hunts come one Recovery after another. The guarantee fired once, at t=104; the rest is dice.
- **Scare layer:** 16 `HC_AMBIENT` beats during Hunt, Recovery, Warning and Containment, and 21 `HC_SCARE_CASTLE` beats (window silhouette ×6, candle gutter ×6, book ×3, knife ×2, bat ×2, silence ×2, false candle ×1, balustrade ×1). Also 4 `HC_LIE` and 2 `HC_DUCK`.
- **Apparition starvation, cause (natural r3, aborted):**
  - In about 6 minutes the director checked for an apparition 6 times. Five checks hit `HC_SCARE_SKIP reason=rhythm canSurprise=False` and one hit `canSurprise=True` but no appearance was due.
  - Every Warning or hunt, and every Recovery, pushed the rhythm's quiet window out again, while hunts came about 90 s apart.
  - Fixed in code after this run, not yet built: the castle no longer rests the rhythm after a Warning or hunt, rests 20-40 s after Recovery, and checks every 12-25 s.
- **Apparitions are still rare (r2):** 1 apparition in 10 minutes, so 0 natural throws or poltergeist, and 1 escalation try (`near=3.4 escalated=False`). The cause was unknown. `430a266` adds `HC_SCARE_SKIP reason=` lines and natural run r3 reads them.

## Frame time, great hall

The camera stands 3.5 m in front of the rune ring looking down the hall, for 10 s at a 1920x1080 window with the 90 fps cap.

| Round | Mean | p99 | Worst | Over 16.7 ms | Slot GPU busy (before / after run) |
|---|---|---|---|---|---|
| r1 | 11.12 ms | 11.34 ms | 17.58 ms | 1 | 0 / 28 % |
| r2 | 11.12 ms | 11.32 ms | 18.09 ms | 1 | 25 / 0 % |
| r3 | 11.15 ms | 11.49 ms | 18.07 ms | 3 | 0 / 26 % |

## Captures for the blind critic

In `docs/haunted-castle/rounds/r10/captures/`:
- **Title (r2 build):**
  - `S7_title_castle_1920x1080.png` and `S7_title_bellwether_1920x1080.png`.
  - The 2560x1440 request does not fit a window on this 3440x1440 display. The largest window that fits is 2416x1359, giving screenshots of 2416x1292: `S7_title_castle_2416x1292.png` and `S7_title_bellwether_2416x1292.png`.
  - `HC_TITLE_FONT cinzel=True` was logged.
  - The fog is now one masked band, so the two strip seams the coordinator saw are gone.
- **Full 1920x1080 set (r2 build):**
  - `S1_great_hall.png` (the props on the tables).
  - `S5_lord_perch.png` (the Count perched).
  - `S6_lord_sheet.png` (the gatehouse with the Count).
  - `H01-H10_hunt_dive.png` (a hunt).
  - `S2_chapel_altar.png`, `S3_crypt.png`, `S4_exterior_sky.png` and `S8_journal_ledger.png`.
