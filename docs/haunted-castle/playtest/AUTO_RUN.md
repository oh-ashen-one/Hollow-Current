# Automated castle playthroughs: waves G-K (windowed checklist, 2026-10-14)

## Round 7b (latest): hc/count17 + hc/art-exterior2, build `e57a00d`

Merges: hc/count17 `b61a396` (`360094f`), hc/art-exterior2 `ac99131` (`9ce9b8c`). Castle and Bellwether re-baked (Presence.cs is shared); the art capture now lowers the drawbridge for `gate_out_S6`. All checks pass; batch of 7 runs, 0 crashes. Captures in `rounds/r12/captures/r7b/` (local).

| Proof | Result | Evidence |
|---|---|---|
| `HC_EXTERIOR_AXIS` | **ok**, no correction | `HC_EXTERIOR_AXIS ok keep centre local (2.3, 29.9, 20.4)`; kit 6 pieces, 38 renderers |
| Missing art | Only the 7 portraits (bake time) | Runtime `HC_MISSING_ART=0` in the fear summary |
| Watched hide grab (fear) | **claw_ok=True at t 0.30 with blend=done**, path label is `dive` (`hide=True`), not `hideopen` | `HC_COUNT_GRAB from=door reason=headroom ceiling=3.00 needs=4.82 mode=8->9`, `placed=door standOff=2.33`. t 0.01 turning (skull off-centre, 179 px); t 0.16 done, centred, 175 px (False); t 0.30 done, centred, 238 px (True); t 1.00 done, logged centred but 162 px (False). In `fear/20_grabcam_grab_t1.00.png` he is not visible (view on dark floor tiles, skull 0.28 m below the eye): possibly occluded or under the floor. The HUD prompt "[E] Leave hiding place" stays on screen during the grab |
| Fear | **WON**, 0 blockers | `wallCrossings=0` both hunts; no-tell `changedFrames=0`; battlement `withClearLine=90/90`; gatehouse `visible=3/3 arch-inside` |
| Great hall frame time | mean 11.15 ms, p99 11.59 ms, worst 17.43 ms, 3 over 16.7 ms | `HC_PLAY_FRAMES` |
| Crypt 118.89 ms frame | **Explained: capture artefact, not the crypt** | Two runs: the only frame over 33 ms is frame 1 right after the screenshot (122.05 / 122.25 ms), with `texDelta=-8294400` (the 1920x1080x4 screenshot texture freed), 0 GC, 0 nav builds, 0 audio loads, no streaming loads. Sampled again after 1 s: max 22.10 / 16.58 ms, 0 frames over 33 ms, p99 11.76 / 11.74 ms. Release-player profiler markers were valid but reported nothing |
| Bellwether `-hc-verify` | **PASS** | First Warning 229.25 s: 21.6 / 21.7 ms, no spike. Slowest 33.4 ms (Dead, 424.6 s); 143 frames over 16 ms, 1 over 33 ms; `meets_60_fps_floor` False |
| Exterior brightness (mean of 255) | battlement_out 27.9 (was 12.3), courtyard_keep 38.2, gate_out_S6 10.8 | The keep in `courtyard_keep` shows stone courses and lit windows; both moons clear of the keep in S4 and courtyard_keep. In `gate_out_S6` the closed gate doors fill the view, so the lowered leaf is not visible |
| Captures | exterior ×9, S1-S8, H01-H10, titles (castle, bellwether) at 1920x1080 | `r7b/exterior`, `r7b/full`, `r7b/S7_title_*`, `r7b/fear` |

## Round 7: art pass, build `ac43efa`

The build is source `ac43efa`: hc/art-exterior (`d904e77`) and hc/art-props (`415eb9d`) merged, the `-hc-capture-art exterior|props` capture mode, and the capture window now resized back to the requested size before every shot. Castle bake: 7 lightmaps, 196 probes, 17 reflections. All runs used `gpu_slot`, `open -g`, a 1920x1080 window and `HC_INPUT scripted=true`. The owner's display changed during the round (3840x2160, then 3440x1440), and an outside resize spoiled the first exterior set; it was recaptured at 1920x1080.

| Proof | Result | Evidence |
|---|---|---|
| Fear run on the art build | **WON**, 0 blockers | `HC_PLAY_RESULT mode=fear won=True blockedDeaths=1 blockers=0` |
| Fear: no-tell first leg silent | **PASS** | `HC_COUNT_LEG first=silent reason=notell`; `changedFrames=0 wingMaxDelta=0.000 rimLines=0 smellLines=0` |
| Fear: watched-hide grab camera | **Changed from r6, not proven** | This run took `kind=hide-open` (mode 7, then 8): at t 0.01 the skull is behind the camera (`eyeToSkull=0.40`, off-screen); at 0.30 it is centred, `claw_px=139 claw_ok=False`; then `end reason=blocked` by the driver's invulnerability |
| Battlement line of sight | **PASS**, same as r11 | `HC_PLAY_BATTLEMENT outdoorPoints=90 withClearLine=90` |
| Great hall frame time with the skeleton | mean 11.14 ms, p99 11.50 ms, worst 17.29 ms, 2 frames over 16.7 ms | `HC_PLAY_FRAMES great_hall frames=898` |
| Crypt frame time with the hand-roots | mean 11.57 ms, p99 15.24 ms, **max 118.89 ms** | `HC_CAPTURE FRAMETIME crypt frames=529` (one hitch, cause not found) |
| Exterior kit | 6 pieces, 30 renderers, render only | Bake log `HC_EXTERIOR_KIT`. Also `HC_EXTERIOR_AXIS keep centre local z -19.5 (expected +19.5): group turned 180 degrees` |
| Missing art | 7 portrait textures | `HC_MISSING_ART texture Castle/Portraits/Portrait_*` (Codex wall-art job, not delivered yet) |

**Full proof batch on `ac43efa`** (one window at a time, 0 crashes; grab frames and the fear gatehouse visit copied to `rounds/r12/captures/r7/`):

| Proof | Result | Evidence |
|---|---|---|
| Natural 1 (8.7 min) | **PASS** | Share 0.211 at 479 s (final 0.207); gap 203 → 395 s (192 s); Dread 36.2 at 479 s; no-tell hunts at Dread 20.0 and 20.0; 17 `governor=` lines, 1.00-1.44; 0 escalations; 1 unforced throw |
| Natural 2 (8.7 min) | **FAIL** on share and gap | Share 0.229 at 479 s (final 0.224 > 0.22); first hunt at 220 s, then gaps 84 s and 81 s (none ≥ 120 s between hunts); Dread 49.5 at 479 s; no-tell at Dread 18.8; 17 `governor=` lines, 0.80-2.56; 2 escalations (`escalated=True`); 2 unforced throws |
| Escalation across both | **PASS** | natural2 2 of 4 attempts |
| `HC_DOOR_HELD` | **FAIL (corrected in wave L)** | Never fired. The earlier count matched the `HC_DOOR_HELD=0` field of `HC_PLAY_SUMMARY`, not a real `HC_DOOR_HELD` line. Cause: the roll only runs on a hide entry at Dread ≥ 60, and no scripted run re-entered a hide at that Dread |
| Siege | **Partial** | Near-miss begin, `open=False` (roll 0.54 vs 0.25), Recovery cut 1; 5 hunts, Dread 85.5 at the end |
| Escape | **WON** | 0 `fix=stop`; return swoop leg1 clear, leg2 detour, `end` reached |
| Kill | **WON, slain** | 10 `HC_COUNT_RECOIL` lines, closest joint to the lens 0.42 m before the push (never inside the wing) |
| Death + retry | **PASS** | `HC_DEATH_PENALTY deaths=1`, Dread 58.4 after retry, relic 0 moved |
| Bellwether `-hc-verify` | **PASS** | `HC_NATIVE_ROUTE_PASS`. First Warning at 226.98 s: slow frames there 21.6 / 21.6 / 23.7 ms, no spike. Slowest frame 33.0 ms (1.4 s, Briefing). 117 frames over 16 ms, 0 over 33 ms, 1 % low 59.9 fps, so `meets_60_fps_floor` is still **False**. Report `rounds/r12/captures/r7/verify-native-report.json` |
| Drop grab from a dive (huntcam ×2) | mode 8, from above 3.1 / 3.6 m | t 0.31: skull centred, `claw_px=97 claw_ok=False` both runs. t 0.01: skull off-centre in huntcam1 (y 1420) |
| Chase grab mode 9 (natural, kill) | t 0.31 centred with claws in all 4 | t 0.01: skull off-screen in 3 of 4 (natural1, natural2 ×2); kill fine |
| Hide-open grab (fear) | **Not proven** | See above: t 0.01 behind the camera, t 0.30 `claw_ok=False`, then blocked |
| Gatehouse visit from inside | **PASS** | `HC_COUNT_GATEHOUSE visible=3/3 kind=arch-inside`, `r7/fear_ac43efa/22_gatehouse_visit.png` |

**Captures** in `rounds/r12/captures/art/` (gitignored, local only):
- `full/`: S1-S8 and H01-H10 (hunt dive).
- `exterior/`: 9 views from the art-exterior handoff, all 1920x1080, plus `capture_art_exterior.json`.
- `props/`: `hall_skull_from_east`, `hall_west_end_skull`, `hall_table_books`, `crypt_ceiling`, plus `capture_art_props.json`.

Factual notes for the critic (no verdict): exterior stone reads flat dark blue under moonlight and warm tan under courtyard lights; window glows and the gate arch are flat yellow fills; the star discs are large and soft; the white moon is partly behind the keep in `courtyard_keep` and clipped by a tower in `bridge_keep`; a green band sits low in the sky in S4; `battlement_out` and `gate_out_S6` average 12 and 11 of 255 brightness. In S1 the skeleton ribs show between the left piers; the skull is not clear from the west end; books float in S1 but are hard to see in `hall_table_books`. The chapel floating candles are still large in S2.

## Round 6: wave K, build `253372f`

The build is source `8a953e9`: hc/rules6, count16, scares4 and audio4 merged, plus F0's death-before-win. Batch 5 ran on `0236fa4` (source `782c856`); verify and fear were rerun on `253372f`. All runs went through `gpu_slot` with `open -g`, a 1920x1080 window and `HC_INPUT scripted=true`.

| Proof | Result | Evidence |
|---|---|---|
| Bellwether `-hc-verify` | **PASS** (rerun) | `HC_NATIVE_ROUTE_PASS`. The first try on `0236fa4` failed: the 9.1 s loading-screen shader warm-up delayed loading. The warm-up is now reverted |
| Verify hitch at the first Warning | **Not fixed, not reproduced** | This run: slowest frame 103.6 ms at 84 s (Survey, upstairs) and 42.6 ms at 407 s (basement). There was no spike at the first Warning. Spikes move from run to run with the window in the background on a shared machine. 206 frames over 16 ms, so `meets_60_fps_floor` stays False |
| Fear: no-tell first leg silent, first 6 s | **PASS** | `HC_COUNT_LEG first=silent reason=notell`; `changedFrames=0 wingMaxDelta=0.000 rimLines=0 smellLines=0` |
| Fear: watched hide, grab camera | **PASS** | `kind=hide mode=8`. Skull in the centre 60 % at t 0.01, 0.31 and 1.01; `claw_ok` True at 0.01 and 1.01 (234 / 314 px), False at 0.31 (137 px) |
| Chase grab camera (huntcam ×2, natural) | **PASS**, drop from above | `HC_COUNT_GRAB mode=8 reason=from above (3.6-3.7 m)`, close in 1.41 / 1.56 s. Skull in the centre at every sample; `claw_ok` True at 0.01 and 1.01, False at 0.31 (121 px, he is 2.6 m above her eye mid-drop) |
| Bridge chase grab camera (fear return) | **Partial** | `kind=chase mode=9`: at t 0.01 the skull is off-screen (`skull_in_centre60=False claw_px=0`) on two samples; at 0.31 and 1.01 it is centred (claws 300 / 193 px) |
| Fear: `wallCrossings` | **PASS** | 0 in both hunts |
| Fear: swoop | **PASS** | `HC_COUNT_LEG first=silent reason=return`, leg2 detour |
| Death over win (bridge) | **PASS (logged)** | `HC_DEATH frame=… beat_win=true died=False blocked=True` on each frame. The driver's invulnerability blocked the kill, so the grab camera kept the win from firing until the driver teleported her (one blocker: "bridge walk did not reach the far bank") |
| Gatehouse visit from inside | **PASS** | `HC_COUNT_GATEHOUSE visible=3/3 kind=arch-inside`, landed. Capture `rounds/r12/captures/fear/23_gatehouse_visit.png` |
| Door held | **FAIL (corrected in wave L)** | The match was the `HC_DOOR_HELD=0` summary field; no real `HC_DOOR_HELD` line fired |
| Escape | **WON**, 0 blockers | 0 `fix=stop` stall lines; return swoop leg2 detour |
| Kill | **WON, slain** | `HC_COUNT_RECOIL closest=0.58/0.49/0.29 pushed=0.19/0.28/0.48` |
| Death + retry | **PASS** | Penalty applied, Dread 58.4, relic 0 moved |
| Siege | **PASS** attempt 1 | Near-miss `open=True` (roll 0.21) and Recovery cut to Hunt |
| Natural 1 (8.7 min) | **PASS** on the critic's test | Final share 0.199; gap 95 → 364 s (269 s); Dread 20 at 479 s; no-tell hunt at Dread 12.1; governor 1.68 |
| Natural 2 (8.7 min) | **FAIL** on share | Final share 0.245 (> 0.22); gap 203 → 387 s (184 s); Dread 60.3 at 479 s; governor up to 2.20 |
| Escalation across both naturals | **PASS** | natural2: `HC_ESCALATE escalated=True reason=ok`. Other attempts failed on `reason=roll` or `reason=grace`, and one became a `substitute=bat_pass` |
| Unforced throws | natural1 0, natural2 2 | |

**Frame time, great hall at 1920x1080** (fear rerun):

| Mean | p99 | Worst | Over 16.7 ms | Slot GPU busy before / after |
|---|---|---|---|---|
| 11.12 ms | 11.35 ms | 17.00 ms | 1 | 0 % / 14 % |

**Captures** in `rounds/r12/captures/`:
- `S7_title_{castle,bellwether}_1920x1080.png`.
- `huntcam1/` and `huntcam2/`: natural chase, drop from above; `grabcam_chase_t0.01/0.31/1.01`.
- `fear/`: watched-hide grab `grabcam_hide_t*`, bridge chase grab `grabcam_chase_t*` (mode 9) and the gatehouse visit from inside.

## Round 5: build `0b08422`

**What changed:** every real device is ignored in scripted runs (`HC_INPUT scripted=true devices_ignored=keyboard,mouse,gamepad`); scripted runs open their own 1920x1080 window and restore the owner's size on quit; castle throws no longer need her to see the prop; flight pitch has a 38 deg floor. There was no `HC_GAMEPAD` line in any run.

| Proof | Result | Evidence |
|---|---|---|
| Bellwether `-hc-verify` | **PASS** | `HC_NATIVE_ROUTE_PASS` |
| Fear: no-tell first leg silent | **PASS** | `HC_COUNT_LEG first=silent reason=notell leg=search` |
| Fear: no-tell first 6 s | **PASS** | `HC_PLAY_CHANNELS … changedFrames=0 wingMaxDelta=0.000 rimLines=0 smellLines=0` over 534 frames. The one voice line is `HC_COUNT_VOICE queued cc_hunt_hungry` (played at t=10.0, outside the window). Heartbeat drift was 0.14: felt Dread following the driver's forced Dread 55 |
| Fear: watched hide, torch off | **PASS** | `HC_COUNT_WITNESSED begin … open=True reason=watched`, then hold, then `HC_COUNT_GRAB mode=8 … hideEntrance=True witnessed=True torch=False` |
| Fear: `wallCrossings` | **PASS** | 0 in both hunts (944 and 573 ground moves) |
| Fear: grab mode | **PASS** | Mode 8 (watched hide). Earlier the natural huntcam (r4) showed mode 9 |
| Fear: flight pitch | **PASS** | Every `HC_COUNT_POSE` is ≥ 38 (38-65) |
| Fear: throw and poltergeist | **PASS** | Plate thrown, poltergeist flung 10 |
| Fear: lies and silence | **PASS** | HC_LIE 7, silence and duck 6 |
| Fear: swoop legs | **PASS** | `leg1=clear leg2=detour` |
| Fear: bridge | not reached this run | Closest 4.83 m, no grab. r2 had 11 and r3 had 31 blocked bridge grabs |
| `HC_COUNT_WING_CLAMP` | not seen | No line |
| Natural (no forcing, 8.7 min): won | **WON**, 0 blockers | |
| Natural: unforced throws | **PASS** | 2: candlestick and goblet, `castle=near_apparition` (2.8 m and 2.6 m from her) |
| Natural: apparitions | **PASS** | 4 |
| Natural: escalation | **PASS** | `escalated=True` once |
| Natural: hunt share | **FAIL** | 0.362 at the end (0.32-0.40 through the run) against ≤ 0.30. See below |
| Natural: Recovery grace | **PASS** | 47.4 / 75.8 / 45.1 / 39.7 s |

**Why the hunt share failed:** the natural driver stands exposed and invulnerable. Every hunt added the per-hunt cap (`huntDreadAdded=25.0`), and Dread rose to 100. At Dread ≥ 80 the dice roll every 2-6 s, so hunts came back as soon as each grace ended. That gave 5 hunts in 8.7 min, including the hostile return.

**Frame time, great hall at 1920x1080** (fear run):

| Mean | p99 | Worst | Over 16.7 ms | Slot GPU busy before / after |
|---|---|---|---|---|
| 11.16 ms | 11.61 ms | 22.37 ms | 3 | 0 % / 0 % |

**Captures:** the r11 set stays as it is. This round changed only the slow-travel pitch from 35 to 38 deg, which the blind critic can see in the round-4 `hunt_natural` frames within 3 deg.

## Round 4: build `fff0de7`

**How it was run:**
- Every run went through `gpu_slot` with `open -g -W -n`, in a 1920x1080 window, with `HC_CURSOR free` logged.
- Logs: `/Users/midir/hollow-current-haunted-castle/.playtest-<name>.log`. Earlier rounds are in `.playtest-r1-*`, `-r2-*` and `-r3-*`.
- Screenshots: `docs/haunted-castle/playtest/auto/<name>/`.

**Build checks:**
- `check_throwables_scene.py` exits 0.
- `HC_COUNT_MODES bound=` lists `8:GrabDrop=1 9:GrabCrawl=1`.
- `HC_COUNTCLAUDE_GRABS`: drop reach 1.615, crawl reach 1.729.

| Proof | Result | Evidence |
|---|---|---|
| Escape | **WON**, 0 blockers | |
| Kill | **WON, slain**, 0 blockers | |
| Long Night | **WON**, 0 blockers | The driver now expects the bell to start the hunt |
| Death + checkpoint retry | **PASS** | Deaths 1, penalty applied, Dread 58.9, relic 0 moved |
| Siege: near-miss with an open | **PASS**, attempt 1 | `open=True openRoll=0.05 chance=0.25 dread=100` |
| Siege: Recovery cut | **PASS**, attempt 1 | `HC_PLAY_RECOVERY_CUT to=Warning`, after the logged driver grace clear |
| Natural (no forcing, 8.7 min): won | **WON**, 0 blockers, 0 blocked deaths | |
| Natural: hunt share | **PASS** | Final `huntShare=0.273`. Note: 0.41 right after the first hunt |
| Natural: Recovery grace | **PASS** | `HC_RECOVERY grace=` 32.7 / 86.6 / 84.4 |
| Natural: event Dread decays | **PASS** | `event=` 29.7 → 23.5 → 19.0 between hunts 2 and 3 |
| Natural: apparitions | **PASS** | 4 (`HC_SCARE visible=4`) |
| Natural: escalation | **PASS** | `HC_ESCALATE near=3.1 escalated=True` once |
| Natural: unforced throws | **FAIL** | 0. The two throw attempts logged `HC_THROW_SKIP summary candidates=38 far=38`: every throwable was more than 6 m from her when the apparition came |
| Natural huntcam: dive close | **PASS** | `HC_COUNT_DIVE close_s=1.38 from=7.92 to=1.00 speed=5.2` |
| Natural huntcam: grab mode | **PASS** | `HC_COUNT_GRAB mode=9 height=0.24 reason=came in low`, then death |
| Natural huntcam: flight pitch | Partial | Logged pitch 35-62 deg (`HC_COUNT_POSE`). Slow travel sits at 35 (target 38) |
| Fear run (forced dice) | **INVALID** | A real gamepad disconnect (`HC_GAMEPAD_DISCONNECTED`, ×3) paused the game mid-run. The no-tell / told / watched-hide / lies steps then failed, so no-tell first-leg silent, the 6 s channel log and `wallCrossings` were **not proven on this build**. Throw (plate) and poltergeist (11) ran before the pause and passed |
| `HC_COUNT_WING_CLAMP` | not seen | No line in any run |
| Bellwether `-hc-verify` | **FAIL** | `Native 1920x1080 output`: the window opened at 1022x579. Scripted verify keeps the last saved window size; it does not set 1920x1080 |
| Candle lean ≤ 22 deg on screen | Partial | S1: 2-4 deg. S5: 23-25 deg on screen (world lean capped at 22, perspective adds 1-3) |
| No scale figure in S6 | **PASS** | `HC_SCALE_REFERENCE hidden` |
| Title, Cinzel | **PASS** | `HC_TITLE_FONT cinzel=True`, both sizes |

**Captures:** `docs/haunted-castle/rounds/r11/captures/`.
- Titles: `S7_title_{castle,bellwether}_{1920x1080,2416x1292}.png`. The 2416x1292 pair is the largest window that fits the 3440x1440 display.
- Scenes: `S1_great_hall`, `S5_lord_perch`, `S6_lord_sheet` (no figure), `H01-H10`, `S2-S4`, `S7_gatehouse_hud_case_start` and `S8`.
- Natural hunt from her eye: `hunt_natural/06-15_hunt_natural_*`. Ten frames through a naturally triggered dive (7.9 m to 1.0 m in 1.38 s) and the GrabCrawl kill.

**Frame time, great hall at 1920x1080** (fear run, measured before the pause):

| Mean | p99 | Worst | Over 16.7 ms | Slot GPU busy before / after |
|---|---|---|---|---|
| 11.14 ms | 11.53 ms | 17.45 ms | 2 | 100 % / 14 %, another GPU load was present at launch |

## Earlier rounds (r1-r3)

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
