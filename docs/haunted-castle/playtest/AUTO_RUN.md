# Automated castle playthroughs: wave F build

These are facts from the real combined player. The critic and the owner judge them.

**Build:** `/Users/midir/hollow-current-haunted-castle/Builds/macOS/Hollow Current.app`, from `haunted-castle` source at `b5da27e`, baked and committed as `34a0de9`.
- All of wave F is merged: hc/dread, hc/count11, hc/scares, hc/audio2, hc/grabs (`fef50da`), hc/title and hc/wallart (runtime patch applied).
- The window is 1920x1080, not full screen.
- Each run went through `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-f0 -- "<app>/Contents/MacOS/Hollow Current" -logFile <log> -hc-playtest <dir> escape|kill|fear`.
- Runs, all 2026-10-14:

| Run | Finished | Slot held |
|---|---|---|
| escape | 09:46 | 92.2 s |
| kill | 09:48 | 101.7 s |
| fear | 09:53 | 272.5 s |

**Logs** are gitignored:
- `/Users/midir/hollow-current-haunted-castle/.playtest-escape.log`
- `/Users/midir/hollow-current-haunted-castle/.playtest-kill.log`
- `/Users/midir/hollow-current-haunted-castle/.playtest-fear.log`

**Screenshots** are 1920x1080 and also gitignored: `docs/haunted-castle/playtest/auto/{escape,kill,fear}/`.

**Driver:** `Assets/HollowCurrent/Runtime/Castle/PlaytestDriver.cs`.
- It sends input through `Investigator.TestInput` and teleports between stops.
- The investigator is invulnerable to the driver only: a grab is counted as `blockedDeaths` instead of ending the run.
- **Fear mode is not an organic playthrough:**
  - It feeds queued values into the real rules engine through `Investigation.Dice` and raises Dread with `AddDread`. Each forced step is logged as `HC_PLAY_FORCED`.
  - It calls the director's real `ThrowNearbyProp`, `CastleScares.Poltergeist` and `ScareDirector.Dread` methods.
  - When the queue is empty, the dice return 0.99, so no unplanned hunt fires.

## Results

| Run | Result | Blockers | Driver-blocked grabs |
|---|---|---|---|
| escape | **WON**, 85.8 s | 0 | 0 |
| kill | **WON, slain**, 95.6 s | 0 | 0 |
| fear | **WON (escape)**, 266.1 s | 0 | 856 frames, from one watched-hide grab |

- **Escape:**
  - The bell starts a 9 s told warning, then the hunt (`HC_HUNT_START noTell=False`).
  - The investigator hid in the Count's wardrobe with the torch off and was not grabbed.
  - The near-miss rolled 0.47 against 0.33, so it did not trigger.
  - The seal starts the hostile return (`HC_HUNT_START phase=Return noTell=True`). The bridge was down at 81.2 s and the run won at 85.8 s.
- **Kill:**
  - The sword was forged at 27.3 s; the hits landed at 40.3, 65.9 and 91.4 s.
  - The torch failed for a second on two dives (`HC_TORCH_FAIL dive`).
  - At the third hit `HC_COUNT_GRAB mode=8 why=airborne drop` was chosen, but no death was blocked (`blockedDeaths=0`).

## Wave F proofs (fear run unless noted)

| Proof | Status | Evidence |
|---|---|---|
| False warning | **PASS** (forced dice) | `HC_WARNING false=True length=9.0`, then `HC_FALSE_WARNING ended phase=Disturbance`. Hunts stayed 0 to 0. Screenshot `fear/10_warning.png`. |
| No-tell hunt | **PASS** (forced dice) | `HC_HUNT_START phase=Hunt noTell=True from=Disturbance`, with no Warning phase. Hidden, it was survived; closest approach 0.91 m. |
| Near-miss | **NOT SEEN on this build** | The escape roll was 0.47 against 0.33. In the fear no-tell hunt `nearMissBegin=0`. An earlier build in this session (a full-screen run, log since overwritten) did log `HC_COUNT_NEARMISS begin/landed/hold doorDistance=0.50/leave` at the Count's wardrobe. |
| Watched hide, door opened | **PASS** | `witnessed_hide seenBeforeHiding=True` at the ossuary niche. `HC_COUNT_GRAB mode=8 why=airborne drop ... witnessed=True`. The driver blocked 856 grab frames, so a real player dies here. |
| Dread moves | **PASS** | It rose naturally from 0.17 to 0.58 in 8 s in a lit room. It reached 73.0 after a hunt with seen +15 and grab survived +20 (`HC_COUNT_DREAD`), fell in the gatehouse from 73.0 to 51.6, and fell again during the return. Forced jumps are logged separately. |
| Hostile return | **PASS (approach), no kill** | `HC_HUNT_SPAWN hostileReturn=True`. The bridge now takes 7.2 s to lower. He came within 7.9 m while she waited at the gate, then dove on the crossing (`HC_TORCH_FAIL dive`); closest on the bridge 1.55 m. `grabsOnBridge=0`. |
| Bridge ground (F2 risk) | **PASS** | The drawbridge has 3 child colliders. A ray down at z -3 hits `Drawbridge leaf` at y 0.00. |
| Near-miss crawl through walls (F2 risk) | **Partial** | Over 176 + 14 ground moves in the two fear hunts, the wall linecast between consecutive positions hit 0 times. No near-miss crawl happened on this build, so that exact path is unchecked. |
| Battlement visible from the courtyard (F2 risk) | **PASS (line of sight)** | `HC_PLAY_BATTLEMENT`: 90 of 90 outdoor NavMesh sample points have a clear line to the battlement head at (3.6, 8.9, 0.0). The farthest is 24.9 m. Not judged visually. |
| Gatehouse visit | **PASS** | `HC_COUNT_GATEHOUSE landed` at (3.60, 7.50, 0.00) with `cc_stay`, after about 40 s in the gatehouse. Screenshot `fear/19_gatehouse_visit.png`. |
| Throwables fly | **FAIL** | `ThrowNearbyProp` returned false and `Poltergeist()` threw 0, so the summary shows `HC_THROW=0`. The director listed all 38 throwables (`HC_THROWABLES director=38`). Cause unknown. `8485c6a` adds a per-candidate probe (`HC_PLAY_THROW_PROBE`: distance, angle, what the sight line hits) that is not built yet. |
| Castle dread beats | **PASS** | `HC_SCARE_CASTLE` ×7: balustrade_shape, bat_pass, window_silhouette ×2, book_drop, bell_single, candle_gutter. |
| Perch sightings between hunts | **PASS** | `HC_COUNT_PERCH moved` ×18 in the fear run, ×3 in escape and ×5 in kill. Every move was outside the view cone (viewAngle 130-145) or occluded. |
| Hunt voices | **PASS** | `HC_COUNT_VOICE` ×10 in fear: hunt_hungry, hunt_run, hunt_found and stay. |
| GrabDrop clip (F5) | **Selected** | `HC_COUNT_GRAB_STATE mode=8 GrabDrop=True`. The death presentation itself was not run, because the driver blocks deaths. |
| Rare line `cc_listen`, Long Night, microphone, door held, death penalty | **NOT RUN** | The rare line is a one-in-three roll. Long Night needs `-hc-long-night`. The microphone is never enabled in automated runs, by design. Door held needs Dread 90 at a hide. The death penalty needs a death and a checkpoint retry. |
| Bellwether `-hc-verify` route | **NOT RUN on this build** | Needs a window. |
| Title captures at 2560x1440 and 1920x1080 against the mock | **NOT RUN** | Needs a window. The 2560x1440 one does not fit in a window on the 3440x1440 display, so `CaptureMode` falls back to full-screen-window for that run only. |

**Frame time, great hall:** the camera stands 3.5 m in front of the rune ring looking down the hall, for 10 s at a 1920x1080 window with the 90 fps cap.

| Frames | Mean | p50 | p95 | p99 | Worst | Over 16.7 ms |
|---|---|---|---|---|---|---|
| 897 | 11.15 ms | 11.11 ms | 11.16 ms | 11.44 ms | 22.03 ms | 3 |

**Windowed launch (owner rule):** a plain launch of this build logged `HC_WINDOWED switched=True mode=Windowed fullScreen=False size=1920x1080 display=3440x1440`. The Unity prefs then read `Screenmanager Fullscreen mode = 3`, which is Windowed. Log: `/Users/midir/hollow-current-haunted-castle/.owner-launch.log`.

## Other findings

- `HC_MISSING_ART speech voice_cc_contain`: `Speak("contain")` has no clip. This is F4's area.
- **Headless batchmode cannot run the driver:** it stalls at the first `WaitForEndOfFrame` screenshot, so every scripted run needs a window.
- **Full-screen runs were closed from outside:** the earlier full-screen runs in this session quit within 3-11 s (`HC_APP_QUIT` with no `Quit()` call). Scripted runs now open a 1920x1080 window, and stray quit, retry and Escape keys are ignored in scripted runs.
