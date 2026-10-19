# Handoff: fe-count (wave FE): hunt/apparition coupling, Late-band share, grab variety, first frame, hunt_dive frame time

Branch `hc/fe-count`, worktree `.wt/fe-count`, base `haunted-castle` `408f406`. Model: Opus 5.5. Date 2026-10-19.

Nothing was built or run in Unity. The worktree has no `Library/`, and another piece owns the engine. Evidence is static only: compiles, the pure rules model and tests, the Bellwether fuzz, the lints, and the FD logs `.playtest-fd-*.log` in the main clone. All runtime claims below are design claims that the integrator's runs must prove.

All of this falls under owner feedback item 12 (fear: "no pressure, no being watched") and the r13 critic pass tests in `rounds/r13/CRITIC_BRIEF.md` items 1-2.

## Causes found in the FD logs

### 1. Hunts within 10 s after a visible apparition (3/6)

There were two causes:

- **Escalations, 2 cases.** `natural1` apparition `t=340 rush=True escalate=true` was followed by the hunt at 341. `natural3` had the same pattern at 334 and 336. An escalation started its 1.5 s tell at the apparition itself. fd-rules' apparition floor raised the number of apparitions, which raised the number of escalation chances.
- **The floor apparition before a planned first hunt, 1 case.** `natural1` had its first apparition at 70 s (`reason=first_apparition`). The Early-band first-hunt deadline then fired a no-tell hunt at 77 s.

### 2. natural2 share 0.089

The Late band's first hunt came at 195 s. A lull began at 339 s, 1 s before the post-first-hunt long quiet stretch (120 s) completed. It held the dice 120 s more, to 459 s, so every apparition logged `reason=held`. At Dread 22, below the run's boundary of 30.6, the 1/15 low roll then had about 60 s left before the Return.

### 3a. `openHeadroom=0`

The flag was written, but it measured the ceiling **over her head**. That is the wrong point: the drop clips fall at his stand-off ring.

- In the hall the natural driver stands under the gallery. There, `ceiling=2.92` against the 4.82 m the clip needs, while the vault a step away is open.
- PlanVariant and PickDiveStyle used the same wrong test, so drop and drop-spin were n/a in every hall dive.
- The only "headroom" commits were Return grabs, and those are not counted as open.

### 3b. huntcam

- The drop-spin start point (`locked ... bearing=0`) was planned 80° beside her. She was staring at him, as the huntcam driver does.
- The only route there was a 20-waypoint NavMesh detour along the gallery (`route=0/20`). He lost sight of her and flew 12 m away, and the driver quit mid-dive.
- Nothing logged a miss.

### 4. First frame and seat

- **natural2 Return drop.** The commit frame's eye-to-skull line was blocked (`lineClear=False`).
- **Seat gaps of 0.74-0.90 m.** These were crawls after a planned drop or side fell back. The seat decays the whole gap over 0.35-0.45 s, which is a visible 1 m slide.
- **snapDeg 135-138.** These were Return drops from behind her. The clamp jumped the view 138° to the edge of the frame, then turned.

### 5. hunt_dive capture frames of 110-122 ms

- These are a capture artefact.
- `CaptureMode` takes ten synchronous screenshots during the dive (`WaitForEndOfFrame`, then `CaptureScreenshotAsTexture` + `EncodeToPNG` + `WriteAllBytes`, about 100 ms each). The frame after each shot carried that stall into the sample, giving about 9 samples over 100 ms.
- fc3 ran with `-hc-capture-shots S1,S2,S3` and took no H shots:
  - `.playtest-fc3-r1-rooms_on.log:225` `H01_hunt_dive skipped (shot filter)`;
  - `:267` `max=17.66ms`.
- The FD full sets did take them: `.playtest-fd-rooms_on.log:274` `p99=110.41ms max=122.47ms`.
- The mean rose 1.07 ms over 882 frames, about 9 × 105 ms.
- The dive's choreography clock steps at most 1/30 s per frame, so gameplay is not affected. Fear's great-hall p99 was 11.53 ms.

## What changed

| Item | Change | Files |
|---|---|---|
| 1 | **Apparition gap.** No hunt starts within `ApparitionHuntGap` = 11.5 s after a visible apparition. 11.5 s, so two whole-second log stamps still read more than 10 s.<br>- The dice, timed hunt and first-hunt deadline wait. The dice redraw their interval.<br>- An escalation is **armed**, not started: its silent 1.5 s tell starts so that the hunt comes 11.5-13.5 s after the apparition. A threat, Containment or the end of the run cancels it.<br>- A bell Warning that would end sooner is lengthened at its start, so the strobe and the timer agree.<br>- The sarcophagus gamble (the player's choice) and the hostile Return are exempt.<br>Logs: `HC_APPARITION_GATE deferred=hunt source=dice\|first\|timed\|share_floor\|warning reason=apparition_recent since= opensIn=`, `deferred=escalation ... tellIn= huntIn=`, `escalation=fired\|cancelled`. | `Core/HuntRules.cs`, `Core/Investigation.cs` |
| 1 | **Apparition side.** `Investigation.PlannedThreatIn` gives the seconds to a known threat: the first-hunt deadline (after the cold gate), the timed hunt, a share-floor pull or an armed escalation. `ScareRhythm.CastleBlocked(..., plannedThreatIn)` blocks an apparition while that is 11.5 s or less. The blocked due time is redrawn after the hunt by the existing block model. Log: `HC_APPARITION_GATE deferred=apparition reason=planned_hunt huntIn=`. | `Core/ScareRhythm.cs`, `Runtime/ScareDirector.cs`, `Runtime/Game.cs` (`Case.Diagnostic` -> log) |
| 2 | **Share floor.**<br>- From 240 s, after the first hunt and once the 120 s long quiet stretch has happened, a `HuntShare` under 0.15 arms one pull per run.<br>- The pull is drawn 20-150 s later. If the share is still low and no grace or apparition gap is running, it starts the next threat: the usual no-tell roll, no false alarm.<br>- Logs: `HC_SHARE_FLOOR armed share= pullIn=`, `pulled=1 share= phase= t=`, `cancelled reason=`. | `Core/HuntRules.cs`, `Core/Investigation.cs` |
| 2 | **Late cadence.** In a Late-band run, after the long quiet stretch, the low-Dread roll is 3 × 1/15. A lull hold after the first hunt now ends with the long quiet stretch (the natural2 chain). Early and Mid are unchanged. | `Core/Investigation.cs` (`LowChanceNow`, `HoldDiceForLull`) |
| 3 | **Headroom on the ring.** `DropRoomRing` checks for headroom over her or at any floored stand-off on the drop ring, his bearing first. It is used by PlanVariant, PickDiveStyle and `HC_GRAB_COMMIT headroom=`. The commit line adds `headroomAt=her\|ring_<deg>\|none ceilingMax=`. | `Runtime/Presence.cs` |
| 3 | **Feasibility before commitment.** PlanVariant plans only feasible grabs:<br>- drop: ring headroom and an open ring bearing (`OpenRing`);<br>- drop-spin: an open spin point on either side that he can fly to directly (`DirectTo`: straight or after the ledge lift, never the NavMesh detour);<br>- side: the same for its flank start.<br>Side and spin are n/a while she is watching him within 35°. Scores log `n/a(reason)`, plus `ring= roomAt= bearing= facing=`. | `Runtime/Presence.cs` |
| 3 | **Mid-dive fallback.** `DropPlan` picks the next feasible grab: the other side's snatch, else the drop on a high dive with ring headroom, else the plain grab. It logs `next=`. A variant whose route needs a NavMesh detour is dropped (`reason=detour: N waypoints`). | `Runtime/Presence.cs` |
| 3 | **Every dive is resolved.**<br>- A dive whose distance to her grows 3 m past its start (out of her sight, or while chasing a variant point) is a readable `DiveMiss` (`DiveAwayMargin`).<br>- Every dive ends in `HC_COUNT_DIVE end=commit` or `HC_COUNT_DIVE miss=True`. Unplanned endings log `reason=abandoned: <why>`: lost sight, no route, a new dive, the hunt ended, a sword stagger, or the run quit mid-dive.<br>- The driver's `HC_GRAB_SUMMARY` adds `dives= diveCommits= diveMisses= diveUnresolved=` and closes an open dive first. | `Runtime/Presence.cs`, `Runtime/Castle/CountClaudeProfile.cs`, `Runtime/Castle/PlaytestDriver.cs` |
| 4 | **Eye line before the commit.** An open-floor commit waits up to 0.4 s (`EyeHoldSeconds`) while her eye line to his skull is blocked. Logs: `HC_GRAB_PATH held=True reason=eye_line blocker=`, then `held=False reason=eye_line_timeout`. | `Runtime/Presence.cs` |
| 4 | **Seat.**<br>- An open-floor drop or crawl keeps the horizontal part of the gap beyond 0.45 m (`SeatMaxGap`) in its root instead of sliding it. The root is shifted at most that much nearer where his skull already was.<br>- If the carried skull's line is blocked, the seat starts 25-100 % of the way to the clip pose, at the first clear fraction.<br>- Side, spin and hide grabs keep their placement.<br>Log: `HC_GRAB_SEAT ... note=reseated X (raw gap Y)` / `relocated X to a clear line`. | `Runtime/Presence.cs` |
| 4 | **Cuts.** A first frame that must turn more than 90° (`FirstFrameCutDegrees`) is a hard cut on frame 0 straight to the grab framing. Log: `HC_GRAB_FIRSTFRAME ... cut=True cutWhy="turn 138 > 90"`. Every first-frame line now says `cut=True\|False`. | `Runtime/Presence.cs` |
| 5 | **hunt_dive frame time.** `hunt_dive` leaves out the screenshot frame and the frame after it. Logs: `FRAMETIME hunt_dive capture_frames_excluded=N of M`; `hunt_dive_raw` keeps every frame. | `Runtime/Castle/CaptureMode.cs` |

Tests (`Tests/RulesTests.cs`):

- New `WaveFETests`:
  - certain dice wait out the 11.5 s gap, and it is logged;
  - a first-hunt deadline 3 s after an apparition moves to the gap;
  - `PlannedThreatIn` and the new `CastleBlocked` overload;
  - escalation armed and fired, with the hunt 11.5-13.5 s after the apparition, never armed twice, cancelled by a threat;
  - Long Night;
  - the bell Warning is lengthened, the gamble is exempt;
  - the lull hold is capped;
  - the Late ×3 cadence;
  - one share-floor pull after 240 s;
  - the 2,000-run critic-route model.
- The session model now applies the planned-threat block.
- Tests that isolate the escalation odds or the dice run with `NoGap` / `NoFloor` rule clones. Their behaviour is unchanged.
- The identical 2,000-run L3 model is cached (it was built 3 times; the rules run went from 21 s to 13.7 s against check_source's 30 s limit).

## Verification (static, final tree)

- `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py`: **PASS**, 1295 assertions (FD: 1260). Only the known CS0649 / CS0162 warnings.
- `bellwether_fuzz.py`: **PASS**, identical, sha256 `ee3b74dc…fd68f2f`.
- `check_castle_scares.py` PASS; `check_audio_keys.py` OK. No scene changes.

Session model, critic route, 2,000 consecutive runs with plans. FD figures in brackets.

| Measure | FE | FD |
|---|---|---|
| Hunts ≤ 10 s after an apparition | **0.000** (both close shares) | 0.105 / 0.164 |
| Runs with share under 0.15, all | **4.5 %** | 20 % |
| Share under 0.15, Early | 3.1 % | 12.6 % |
| Share under 0.15, Mid | 8.7 % | 17.1 % |
| Share under 0.15, Late | **1.6 %** | 30.9 % |
| Share mean / p10 | 0.201 / 0.160 | 0.182 / 0.130 |
| 3-run pooled apparition CV ≥ 0.5 | 97.3 % of sets | |
| First-hunt spread ≥ 45 s | 100 % | |
| ≥ 120 s gap | 100 % (min 123 s) | |
| ≥ 4 apparitions in 480 s | 96.9 % | 98.8 % |
| Wave I gap CV (slow investigator) | 0.42 (still ≥ 0.4) | 0.53 |

The wave I gap CV falls because the floor cuts long gaps. The one pull per run and the 20-150 s draw are what keep it at or above 0.4.

## Integrator: merge, bake, build, then run

Merge, re-bake both maps (C# changed) and build as LOOP.md says. There are no scene changes. Keep PlayerPrefs between runs (`hc.castle.runs`, `hc.castle.grabdeck`) and restore the owner's afterwards as usual.

Launch every run windowed through the slot:
`~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-fe -- open -g -W -n -a "<app>" --args -logFile <dir>/player.log -hc-playtest <dir> <mode>`

Runs: natural ×3 (in a row), siege_held ×2 (`-hc-hold-dread 60 ... siege`), fear, huntcam, cold ×1, plus a full capture set (rooms_on) for item 5.

**1. Apparition gap** (naturals and sieges):

- `HC_PLAY_STATS pctWithinScare10s=` ≤ 35 per run. Expected 0, except a sarcophagus gamble.
- Every `HC_HUNT_START` that follows an `HC_SCARE visible= t=` within 11 s needs an `HC_APPARITION_GATE` line explaining it. There should be none.
- `escalate=true` lines are followed by `deferred=escalation ... huntIn=11.5-13.5`, then `escalation=fired`, then `HC_WARNING escalated=True length=1.5`, then `HC_HUNT_START`.
- `HC_SCARE visible=` count with `t` < 480 is ≥ 4 per natural.

**2. Share** (naturals): `huntShare` 0.15-0.30 in all three. A Late run should show `HC_SHARE_FLOOR armed` and `pulled=1`, or a hunt from the 3/15 low roll after the long quiet stretch.

**3. Pooled r12 tests stay as they are:**

- `apparitionCV` ≥ 0.5 (pooled);
- first-hunt spread ≥ 45 s;
- `longestGap` ≥ 120 in every run;
- hidden survival ≤ 85 % (sieges);
- cold: `HC_HUNT_TELL kind=warning ... first=True cold=True`;
- siege: an `open=True` within 2 visits of a previous one in at least one run.

**4. Grab variety** (natural ×3 + fear + siege_held ×2 + huntcam, pooled from `HC_GRAB_COMMIT mode=` and `HC_GRAB_MODE_HIST final`):

- m10 ≥ 1, m11 ≥ 1, m12 ≥ 1;
- `openHeadroom` > 0;
- `openHeadroomDrops/openHeadroom` ≥ 30 %;
- `HC_GRAB_SUMMARY ... diveUnresolved=0` in every run.
- huntcam ends in `HC_COUNT_DIVE end=commit` (expected: a drop, `facing=True` makes side and spin n/a) or `miss=True`.

If m10 or m12 is still 0, the `HC_GRAB_VARIANT ... scores=[... n/a(<reason>)]` lines name the blocker. If fewer than 12 open grabs occur, add naturals rather than force dice: siege grabs are all hide grabs, and Return grabs are not "open".

**5. First frame** (`HC_GRAB_FIRSTFRAME`):

- `visible=True` on every line;
- `seatGap` ≤ 0.5 (`HC_GRAB_SEAT` shows `reseated`/`relocated` where it acted);
- `snapDeg` ≤ 90 unless `cut=True`;
- check the `t0.01` and `t0.16` grab frames for no slide.

**6. hunt_dive**: in the full capture set, `FRAMETIME hunt_dive` p99 is about fc3's (≤ 20 ms), `hunt_dive_raw` still shows the ~110 ms shot frames, and `capture_frames_excluded=` is about 20.

## Risks

- **Unproven in the engine.** The ring headroom, DirectTo, the facing rule, the eye hold, the reseat and the cut path have never run.
- **Possible over-correction.** A wrong `DropRoomRing` could plan drops where CommitGrab still finds no headroom. CommitGrab switches those to the crawl (`HC_COUNT_GRAB from=door reason=headroom`), as before.
- **Escalation feel.** The close apparition no longer turns into a hunt at once. It vanishes, and 10-12 s later the silent 1.5 s tell and the hunt come. The escalation odds are unchanged.
- **Floor and cadence.**
  - The share floor caps one long gap per run, so the gaps are a little more regular (wave I gap CV 0.42).
  - The Late ×3 low roll makes Late runs busier after their long quiet.
  - Mid is now the band most often under 0.15 (8.7 % in the model).
- **Variants may be rarer.**
  - DirectTo and the facing rule can make side and spin rarer in cluttered rooms and in staring drivers.
  - The deck pushes for them wherever they are feasible.
  - In huntcam, death and kill they are n/a by design, because she watches him.
- **Reseat.** It can put a drop or crawl stand-off up to 0.45 m nearer his skull's last position. KeepSkullOut still holds 0.65 m from her lens.
- **Cuts.** A > 90° first frame is now a hard cut with no turn. Return grabs from behind will show `cut=True`.
- **The moving-away miss** applies only out of her sight or while chasing a variant point. Hostile-Return chases in view are unaffected.
