# Round 11 blind playtest critic: the running build

Critic: Opus 5.5 high, blind. Build `Builds/macOS/Hollow Current.app` (source 3bff23e, bake 0b08422), Mac Studio M3 Ultra.
Before scoring I read only `FEAR_PLAN.md`, `DESIGN.md`, `docs/research/phasmophobia-horror/README.md`, `refs/*` and the driver/capture sources (for flag names and log keys). I did not read HANDOFF, PLAYTEST, AUTO_RUN or the round handoffs. I read the r10 critic reports only after fixing the score.

I made 12 launches, one at a time, each through `gpu_slot.sh capture --label hc-critic -- open -g -W -n -a …`. All were scripted. Every castle log shows `HC_INPUT scripted=true devices_ignored=keyboard,mouse,gamepad` and `HC_CURSOR free` (for example `natural1/player.log:30,33`). There were 0 crashes, 0 timeouts and 0 SIGTERM/SIGKILL, and no instance is left running (`critic/runs.txt`).

| Run | Flags | Secs | Result line |
|---|---|---|---|
| verify | `-hc-verify` | 427 | `HC_NATIVE_ROUTE_PASS` |
| escape | `-hc-playtest escape` | 95 | won=True blockers=0 |
| natural1 | `-hc-playtest natural` | 547 | won=True blockedDeaths=2566 blockers=0 |
| huntcam | `-hc-playtest huntcam` (natural dice, no invulnerability) | 146 | dead=True |
| fear | `-hc-playtest fear` | 331 | won=True blockers=0 |
| siege | `-hc-playtest siege` | 171 | nearMissOpen=1 recoveryCuts=1 |
| kill | `-hc-playtest kill` | 105 | won=True slain=True |
| longnight | `-hc-playtest escape -hc-long-night` | 91 | won=True |
| natural2 | `-hc-playtest natural` | 562 | won=True blockedDeaths=5644 |
| capture | `-hc-capture` | 50 | 20 entries |
| title | `-hc-capture -hc-capture-title -hc-capture-size 1920x1080` | 15 | TITLE DONE |
| death | `-hc-playtest death` | 36 | deaths=1 penalty=True movedRelic=0 |

The driver has no seed flag. Dice use `UnityEngine.Random`, which is unseeded, so the two natural runs drew different values: `runDreadMin` was 37.3 in natural1 and 33.0 in natural2.

Outputs are under `docs/haunted-castle/rounds/r11/critic/<run>/`: `player.log`, the PNG frames, and `timeline.txt` for the natural runs. The frames total 510 MB and are kept out of git by `critic/.gitignore`. The committed JPEG contact sheets and crops are listed at the end.

**Score: 4.5 / 10** (bar 8 = a Phasmophobia-grade hunt loop in a hand-authored gothic castle).

| Axis | Score | One-line reason |
|---|---|---|
| Fear loop, natural play | 5 | Every system fires unforced. But hunts take 32–37 % of play, Dread reaches 100 by minute 8, and every hunt below Dread 87 had a warning. |
| Hunt readability (player's eye) | 4 | He closes fast (7 m → grab in ~1.0 s) and reads well at 3.7 m. At the grab he is absent from frame, and the kill shot shows the ceiling. |
| Hiding | 5 | The witnessed hide works. An unwitnessed hide below Dread 80, or on the first hunt, is a guaranteed save. |
| Environment vs refs | 2.5 | Greybox geometry, box books, glove-card hand-roots, a slab castle, untextured altar sockets. |
| Title / journal / end cards | 7.5 | The title is almost pixel-for-pixel the mock. The ledger and end cards are the best-looking things in the build. |
| Audio assets | 6 | No clipping. One loop clicks at its seam, the castle voices are about 5 dB quieter than Bellwether's, and there is no music in the castle (good). |
| Stability / perf | 7.5 | 0 exceptions, 0 HC_MISSING_ART, 0 blockers. Great-hall p99 is 11.5 ms. The Count stalls mid-air on walls 2–6× per run. |

---

## 1. Fear loop in natural play

The natural route plays the opening, then makes 8 minutes of 20 s stops at six spots with the torch on, never hiding and invulnerable, then rings the bell, seals and crosses. The driver teleports between stops, so this is not human pathing.

**Hunt timeline.** Warning start ≈ hunt start − warning length. Grab = `HC_COUNT_GRAB` that the driver's invulnerability absorbed.

| Run | Hunt t (s) | From | Dread / felt | Tell | Warning s | Hunt s | Recovery s / grace s | Grab |
|---|---|---|---|---|---|---|---|---|
| n1 | 114 (`:166`) | Disturbance | 14.3 / 20.4 | told | 7.0 | 29.4 | 40.0 / 52.7 (`:231`) | mode 9, 7.35 m in 1.31 s |
| n1 | 211 (`:270`) | Disturbance | 38.6 / 33.6 | told | 9.2 | 29.5 | 28.8 / 57.9 | mode 9, 13.37 m in 4.69 s |
| n1 | 362 (`:389`) | Disturbance | 51.4 / 40.6 | told | 13.5 | 39.5 | 23.8 / 60.0 | mode 9, 6.89 m in 1.31 s |
| n1 | 466 (`:500`) | Disturbance | 87.4 / 78.6 | **no-tell** | – | 29.6 | 13.3 / 84.9 | mode 9, 10.06 m in 2.03 s |
| n1 | 521 (`:579`) | Containment (Return) | 85.7 / 93.8 | no-tell | – | to the bridge | – | closest 1.06 m, no grab (`:653`) |
| n2 | 91 (`:148`) | Disturbance | 18.1 / 0.0 | told | 6.3 | 36.2 | 25.7 / 58.8 | mode 9, 12.75 m in 4.49 s |
| n2 | 214 (`:266`) | Disturbance | 42.6 / 33.7 | told | 7.5 | 39.8 | 32.8 / 58.7 | mode 9, 12.75 m in 3.82 s |
| n2 | ~323 (`:372`) | Disturbance | 67.4 | **false warning** | 6.7 | – | – | – |
| n2 | 345 (`:379`) | Disturbance | 68.1 / 66.5 | told | 6.3 | 47.7 | 31.1 / 51.5 | 2 dives, mode 9 |
| n2 | ~442 (`:498`) | Disturbance | 97.9 | **false warning** | 10.0 | – | – | – |
| n2 | 462 (`:504`) | Disturbance | 98.2 / 100 | **no-tell** | – | 28.6 | 32.9 / 59.8 | mode 9, 10.06 m in 2.03 s |
| n2 | 536 (`:587`) | Containment (Return) | 78.8 / 93.5 | no-tell | – | to the bridge | – | **grab logged in the same frame as Won** (`:658` → `:664`) |

**Hunt share** (Warning + Hunt + hostile Return over run length): natural1 172.7 / 536 s = **32 %**, natural2 204.1 / 551 s = **37 %**. The game's own `huntShare` ends at 0.31–0.32 and 0.36–0.39. That is better than r10's 52–63 %. It is still above FEAR_PLAN's own session model (p50 20 %, p90 28 %) and far above Phasmophobia, where a solo player is hunted perhaps 10–15 % of a contract. Quiet gaps between a hunt's end and the next threat were 58, 108, 64 s (n1) and 79, ~69, ~49 s (n2). No gap reached 120 s.

**Dread runaway.** Dread climbs monotonically in both runs and pins: n1 89.1 at t=449 (`:494`), 100.0 at t=493 (`:557`); n2 95.3 at t=389, 100.0 at t=493. Event Dread alone is 60–70 at the end. Lies and door-held therefore become the steady state after minute 7, not an excursion.

**Can an attentive player predict a hunt?**
- **First hunt:** 91 s, 114 s and 131 s (n2, n1, huntcam) after the second evidence record (taken at about 8 s). All three came at Dread 10–18, below the run boundary of 33–37. This is a learnable "first hunt about 1.5–2 min after the second record" timer.
- **Told hunts:** 6/8 natural hunts were told. Every told warning changes the HUD objective to "BREAK LINE OF SIGHT – hide [E] – lantern off [F] – stay quiet", adds "SIGNAL COLLAPSING", sets the instrument to "SIGNAL UNSTABLE", shows a dispatch card and strobes the candles (`fear/10_warning.png`, `escape/12_bell_rung.png`). False warnings look the same (`fear/10_warning.png` is a false one), so a warning is not proof. That is the intent, and it works.
- **No-tell hunts happened only at Dread ≥ 87** (n1 87.4, n2 98.2). Below about 85, the sample says "no warning = safe". That is the single reliable tell the research names as fear-killer #1, merely moved from "lights blink" to "no HUD swap".
- **The no-tell channel is clean.** `fear/player.log:162 HC_PLAY_CHANNELS no_tell … changedFrames=0 heartbeatMaxDelta=0.022 wingMaxDelta=0.000`: no HUD, music or wing change in the first 6 s.
- **In-hunt cues are near-metronomic.** `cc_hunt_hungry` played at t = 8.8–14.4 s into the hunt (9 hunts; 8 of 9 within 8.8–11.7 s). `HC_TORCH_FAIL` fires in the last second of every dive (5/5 n1, 6/6 n2): a 100 % reliable "grab in 1 s" cue.
- **Count behaviour:** he is perched between hunts and re-perches only unobserved (`HC_COUNT_PERCH moved … occluded=True`; `flight observed=True` 0 times). Nothing he does between hunts foreshadows one.

**Scare layer between hunts** (n1 + n2, ~18 min, all unforced):
- 8 apparitions (`HC_SCARE visible=`), 4 throws (`HC_THROW castle=near_apparition`; r10 had 0) plus 4 logged skips.
- 6 lies (false candle, phantom listen, footsteps behind), 5 silences with `HC_DUCK depth=0.10`.
- Castle beats: window_silhouette 10, **bell_single 9**, candle_gutter 5, bat_pass 5, knife_stick 4, book_drop 2.
- Problems:
  - `bell_single` is meant as a rare false tell, but fires about every 2 min, so it becomes wallpaper.
  - `window_silhouette` fires inside hunts (n1 `HC_AMBIENT phase=Hunt beat=window_silhouette` ×3).
  - Escalation never converted: `HC_ESCALATE … escalated=False` 8/8 (n1 `:143,252,361,478`; n2 `:141,248,352,486`).
  - The rare line did not fire (`HC_CASTLE_MIX … rareLine=False`, both runs `:41`).

**Hostile Return.** It is real in 2 of 5 runs (n1 closest 1.06 m, n2 a grab at the trigger). It is toothless in the others because the Count stalls on walls:
- escape: `HC_COUNT_LEG blocked=wall fix=stop … Slab / wall at (8.28,6.40,4.19)` ×3, then `fix=land for=3.0` (`escape:222–231`), stationary mid-air for about 3 s.
- fear: the same at `(-1.89,7.50,0.23)` (`fear:439–455`). Closest approach was 7.4 m while waiting and 6.27 m on the bridge (`fear:450,468`).
- natural2: the bridge grab at 1.03 m (`:658`) is logged in the same frame as `HUNT_END` and Won (`:664`). The grab counted neither as a death nor as a blocked death, so a kill on the last metre of the bridge is lost.

## 2. Hunt readability from the player's eye (huntcam, natural dice)

- **Setup:** `huntcam/player.log:160–187`. Told hunt at t=131 (Dread 10.3). He dives from balcony perch 2 at 12.70 m. The player stands on the rune ring with the torch on.
- **Closing:** frames every 0.31 s at 12.7 → 11.8 → 10.2 → 8.6 → **7.0 (1.35 s)** → 5.3 → 3.7 → 2.1 → 1.19 m. `HC_COUNT_DIVE close_s=2.37 from=12.70`. From 7 m to grab is ≈1.0 s at 5.2 m/s. That meets r10's ≤2 s, and it is almost too fast to read.
- **Pitch:** `HC_COUNT_POSE` is 38–65° in every airborne leg (n1: 28 of 44 samples at exactly the 38° floor).
- **Frames 1–4 (12.7–8.6 m):** he is a small dark shape lost inside the lit chandelier ring. He does not read as a figure.
- **Frame 7 (3.7 m)** (`crop_huntcam_07_d3.7m.jpg`): the best frame in the build. Skull face, ember eyes, both six-fingered claws hanging forward, chest pitched toward the lens.
- **Frame 8 (2.1 m):** he fills about 60 % of the frame height.
- **Frame 9, the grab (1.19 m, Animator mode 8):** **he is not in frame at all**. `14_hunt_natural_09_d1.2m_Hunt.png` shows an empty, fully lit hall. `HC_COUNT_GRAB_PRESENTATION mode=8 standOff=2.22` teleports him to a stand-off point.
- **Frame 10 (Dead):** the camera is pitched up at the gallery ceiling, with one wing edge at the top-right corner. No claws. At bottom-left a gap at the wall top shows night sky and stars (`15_hunt_natural_10_d2.2m_Dead.png`).
- **death run, `06_dead.png`, also mode 8:** again the camera looks at the ceiling. The skull and both claws sit at the bottom edge, about 130 px across at 1920 (`crop_death_grab_claws.jpg`). That is about 173 px at 2560, below the 200 px bar. GrabDrop is meant to be "falls from above onto the camera", yet he appears *below* the lens.
- **Grab modes:** 9 on every natural open-floor grab (9/9), 8 on the balcony dives and hide opens. Mode 6 was never used.
- **Clipping and stalls:**
  - kill `17_hit_1.png` and `18_hit_2.png`: the camera is inside his wing mesh, a purple membrane over half the screen.
  - TrackHunt: `wallCrossings=0` in both fear hunts (`fear:222,326`).
  - Mid-air "fix=stop" stalls per run: escape 3, fear 5, kill 5, longnight 3, n1 6, n2 5.

## 3. Hiding

- **Guaranteed save, measured.**
  - escape, first hunt at Dread 2, hidden with the torch off and unwitnessed: `NEARMISS begin … open=False chance=0.00 eligible=False reason=first hunt, below 80` (`escape:131`). He rattles the wardrobe for 3 s and leaves.
  - fear, no-tell hunt at Dread 55: identical, `eligible=False` (`fear:158`).
  - When the near-miss roll fails, he never visits the hide at all (siege hunt 1: `roll=0.94 go=False`, `siege:123`).
  - So **below Dread 80, an unwitnessed hide with the torch off is a 100 % save**.
- **At Dread ≥ 80:** an open needs near-miss 0.33 × open 0.25 ≈ **8 % per hunt**. Siege needed 3 hunts at Dread 86–100 to get one open (`siege:293,307,308`, mode 8 at the hide).
- **Long Night:** eligible at Dread 51 (`longnight:131`, chance 0.25). The roll failed and the hide held.
- **Watched hide:** works. He saw her and she hid with the torch off: `HC_COUNT_WITNESSED chase=True … entrance grab` → `HC_COUNT_GRAB mode=8 … hideEntrance=True witnessed=True` (`fear:299–305`).
- **Near-miss open:** proven once, forced (siege).
- **Door held:** 0 in every run. Not proven.

## 4. Visual quality against the refs

See `sheet_rooms_vs_refs.jpg` and `sheet_rooms_vs_refs2.jpg`.

- **S1 great hall:**
  - Tiling brick boxes.
  - The skeleton is a small rib fragment at the left edge.
  - No lancet windows, banners, fireplace or carved columns.
  - Floating books are untextured slabs with a red edge.
  - Candelabras are crossed cylinders.
  - Only the rune ring and the torch shaft echo the ref.
- **S2 chapel:**
  - Warm amber where the ref is cold blue.
  - The stained-glass window is replaced by a small dark arch.
  - The font is a flat red fill.
  - The altar is a box.
  - `kill/15_sword_forged.png` shows the three relic sockets as **untextured white and lavender primitives** (cylinder and boxes).
- **S3 crypt:**
  - The hand-roots are beige glove cards on sticks.
  - No coffin niches.
  - A pink-lit block stands in for the sarcophagus.
- **S4 exterior:**
  - The castle is a trapezoid slab with three cones on a flat red plane.
  - No rock, mountains or bats.
  - Bloated star discs.
- **S5 / S6 (the Count):** the Count reads as the demon sheet (skull, flayed chest, skin wings). The wing edges carry a red rim line (`S6_lord_sheet.png`, crop x 0.38–0.5). The 1.80 m scale figure logged at `figureTop=(0.43,0.61)` is not visible in the frame.
- **Gameplay frames:**
  - The "Hollow Compass" is Bellwether's modern EMF meter (`escape/05_evidence_Induction.png`), not the brass needle of the design.
  - Plates on the dining table read as floating blue discs.
- **Gatehouse visit (the safe-zone violation):** from inside the gatehouse the camera sees only the gatehouse ceiling. Mean luma is 8.4/255, and the autocontrast crop shows no Count (`crop_gatehouse_visit_autocontrast.jpg`). The beat is invisible to the player it targets.
- **Title:** `title/S7_title_castle_1920x1080.png` matches `rounds/r10/title_mock.png` in layout, type, art and cards (`sheet_title.jpg`). It adds animated fog over the moat. The Bellwether card thumbnail is darker than the mock.
- **Journal and end cards:** strong (`fear/26_journal_pulse.png`, `kill/20_won_kill.png`).
- The known blank wall frames were not scored.

## 5. Audio

Inventory: `CastleAudio` 44 clips, `Audio` 39, `HiggsfieldAudio` 10 (including `music_house/hunt/safety`). Full table and loop analysis in `critic/audio_scan.txt` (python, numpy).

- **Clipping:** none. 0 samples at |x| ≥ 0.999 in all 93 files.
- **Loudness outliers:**
  - The castle voices (22.05 kHz) sit at RMS −29 to −34 dBFS. Bellwether's `voice_*` sit at −24.6 to −25.6, so the castle cast is about 5 dB quieter.
  - Every castle voice carries **1.5–1.85 s of trailing silence**.
  - `step_gravel_*` peaks at −2.6 dBFS (RMS −18), the hottest asset.
- **Loop seams:** `crypt_blood_trickle` jumps 0.033 at the wrap (44× its median sample step), an audible click every 7.5 s. `forge_hold_hum` 0.012 (19×) and `candle_whisper_bed` 0.008 (10×) are marginal. `heartbeat_loop` is a seamless lub-dub at 52 bpm (onsets every 0.87/0.29 s, wrap consistent). Wind and wing loops are clean.
- **No music in the castle:** no music key or volume appears in any castle log. ChannelWatch shows `huntMusic=False|safeMusic=False|houseMusic=False` (`fear:162`). Bellwether still has music (`verify` `music_frames=37265`), as it should.
- **Heartbeat is not a hunt lamp:** FeltDread at hunt starts was 0.0, 33.7, 66.5, 100 (n2) and 20.4, 33.6, 40.6, 78.6 (n1). Between hunts it reached 96 (n2 t=509), and it changed by only 0.022 at the no-tell start.
- I did not *listen* to the in-game mix.

## 6. Bellwether (`-hc-verify`)

- **Result:** `HC_NATIVE_ROUTE_PASS`, scope "full investigation and retry". 37,491 frames at 1920×1080: p50 11.11, p95 11.48, p99 12.02 ms. 186 frames over 16 ms, 1 over 33 ms (35.9 ms). `meets_60_fps_floor=False`, `performance_pass=False` (`verify/evidence/native-report.json`, untracked).
- **Castle leakage:** none visible. The verify log contains no `HC_RULES`, `HC_COUNT_*`, `HC_SCARE_CASTLE`, `HC_LIE` or `HC_DUCK`. It has only the Bellwether keys (`HC_HUNT_SPAWN` ×3, `HC_SCARE` ×1).

## 7. Errors and performance

- **Errors:** 0 `Exception` and 0 `HC_MISSING_ART` lines in all 12 logs. 0 `HC_PLAY_BLOCKER`. 0 crashes. No soft-lock: every run reached its result line.
- **Great hall:** `HC_PLAY_FRAMES great_hall frames=899 mean 11.14 p50 11.11 p95 11.17 **p99 11.53** worst 16.70 over_16.7=0` (`fear:107`). Passes the ≤16.7 ms p99 target. The capture S1 measurement agrees: p95 11.47, max 15.77.
- **Bugs found in play:**
  - Mid-air wall stalls (§1, §2).
  - A grab lost on the bridge (`natural2:658`).
  - Kill shot framing (§2).
  - The camera inside the wing on sword hits.

## 8. Score, top three, not judged

**4.5 / 10.** The rules engine is now a real Phasmophobia-shaped machine. It has dice hunts, false warnings, no-tell hunts, lies, silence, unforced throws and apparitions, a witnessed-hide punish, a death penalty that moves a relic (`death:140–149`), a working sword fight and Long Night. It runs clean and fast.

What a player would actually experience is different:
- A told hunt every ~2 minutes, about a third of the session spent hunted, and Dread pinned at 100 from minute 8.
- No hunt without warning until Dread is near 90, and an unwitnessed hide that never fails below 80.
- A kill shot that shows the ceiling.
- A greybox castle that looks nothing like its references.

### Top three, highest leverage (each testable)

1. **Restore the sawtooth (single biggest gap).** Hunt pacing and Dread must leave room for dread. Pass, in two unforced `natural` runs:
   - Warning + Hunt + Return ≤ 22 % of run time.
   - At least one hunt-free gap ≥ 120 s after the first hunt.
   - Dread < 85 at t=480 for the torch-on route.
   - At least one no-tell hunt below Dread 70.
   - The first hunt's delay after the second record has a spread ≥ 60 s across three runs (today 91/114/131 s).
2. **Frame the kill.** For every grab mode (8, 9, hide-open), the screenshots at the grab frame and at +0.3 s must have:
   - The Count's skull inside the central 60 % of the frame.
   - At least one claw ≥ 200 px wide at 2560 px.
   - GrabDrop arriving from above the lens.
   - Re-run `huntcam` and `death`. Today the grab frame is empty and the Dead frame shows the ceiling.
   - Also fix the camera-inside-wing view on sword hits (`kill/17,18`).
3. **Art pass on the four hero rooms plus the exterior.**
   - Draped skeleton ≥ 30 % of frame width, banners, lancet windows with both moons.
   - Cold chapel, stained glass, real font, textured relic sockets.
   - Organic hand-roots and coffin niches.
   - Castle on a rock with a spire cluster.
   - Pass: a blind A/B of S1–S4 against `refs/*.png`, each pair rated ≥ 6/10.

### Secondary (ordered)
- **Hide contract:** let an unwitnessed hide fail sometimes below Dread 80 (for example near-miss open ≥ 10 % from the second hunt on). Prove door-held at Dread ≥ 90 in a run.
- **Wall stalls:** fix `fix=stop` (0 in five runs) and the hostile-Return stalls. Return must close to ≤ 3 m in ≥ 3 of 4 runs.
- **Bridge grab:** a grab ≤ 1.05 m before the trigger must count as a death (or a blocked death under the driver).
- **Beat rates:** `bell_single` ≤ 1 per run; no `window_silhouette` during Hunt; at least 1 escalation per 3 natural runs.
- **Gatehouse visit:** make it visible from inside the gatehouse (a window or doorway sightline), or move the landing spot.
- **Audio:** fix the `crypt_blood_trickle` loop seam. Trim the 1.5–1.8 s voice tails. Bring castle voices up about 5 dB to match Bellwether.
- **Compass model:** replace the EMF meter with the brass Hollow Compass. Remove the floating-plate look on the dining table.

### Not judged
- Real mixed audio, positional audio and voice intelligibility (assets measured only).
- Motion quality between stills: wing flaps, blend trees, flame flicker, fog.
- Human pathing, chase escapes around columns, crouching under galleries. The natural driver teleports and is invulnerable.
- Frame-time distribution across a whole natural run (only the 10 s great-hall sample and the capture S1 sample).
- Microphone (never enabled in automation).
- Wall art (known blank).
- `bellwether_fuzz.py` (not run).
- Throw and poltergeist motion (stills only).

## Compared with r10 (read after scoring)

The r10 blind critic also scored 4.5.

**Fixed since r10, measured here:**
- Hunt share is 32–37 %, down from 52–63 %.
- Unforced throws went from 0 to 4 in two runs, and apparitions from about 1 to 4 per run.
- Grabs now use modes 8/9 instead of 6.
- 7 m → grab now takes about 1.0 s (r10 asked ≤ 2 s).
- `wallCrossings=0`.
- No hunt-start beat audio at no-tell starts (the ChannelWatch is clean).
- `cc_hunt_hungry` now spreads 8.8–14.4 s, which narrowly passes r10's ≥ 5 s test, although 8 of 9 cluster within 3 s.

**Not fixed:**
- The art. r10's V1–V4 still describe S1–S4 almost word for word.
- The Dread runaway (still 100 by minute 8).
- Hunt share is still above r10's 30 % cap.

**New in r11:**
- Mode 8 kill framing (empty grab frame, ceiling Dead frame).
- Hostile-Return wall stalls.
- The lost bridge grab.
- The invisible gatehouse visit.
- The guaranteed-save hide measured directly.

The score holds at 4.5. The loop improved, but the new visible failures and the unchanged environment keep it there.

### Committed evidence (JPEG, small)
`critic/sheet_escape.jpg`, `sheet_huntcam.jpg`, `sheet_fear.jpg`, `sheet_kill.jpg`, `sheet_rooms_vs_refs.jpg`, `sheet_rooms_vs_refs2.jpg`, `sheet_title.jpg`, `crop_huntcam_07_d3.7m.jpg`, `crop_death_grab_claws.jpg`, `crop_gatehouse_visit_autocontrast.jpg`, every `*/player.log`, `natural1/timeline.txt`, `natural2/timeline.txt`, `capture/capture.json`, `audio_scan.txt`, `runs.txt`. The full-resolution PNGs remain at the paths cited above (untracked).
