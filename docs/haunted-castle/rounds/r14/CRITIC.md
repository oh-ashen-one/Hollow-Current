# Round 14: blind playtest critic, Haunted Castle

**Build under test:** `Builds/macOS/Hollow Current.app` (binary dated 2026-10-19 08:31). It was built from `haunted-castle` at **6229407**. The brief commit `952fd5c` only adds `rounds/r14/CRITIC_BRIEF.md`.

**Setup:**
- Machine: Mac15,14 (Studio). The display is 3440×1440 (plus a 1080×1920 portrait screen).
- Window: every playtest ran in a 1920×1080 window. Art captures used `-hc-capture-size 1920x1080`. The title capture ran at 2416×1292, the largest window that fits for its default 2560×1440.
- Launches: each one went through `gpu_slot.sh capture --label hc-critic -- open -g -W -n -a ...`, one instance at a time.
- Crashes: 0 of 28 launches. The verify run was launched twice because my queue's quit check does not apply to `-hc-verify`. Both runs completed.

**Blindness:** before scoring I read only these:
- `FEAR_PLAN.md`, `DESIGN.md` and `docs/research/phasmophobia-horror/README.md`
- `refs/great_hall.png`, `refs/exterior_sky.png`, `rounds/r10/title_mock.png` and `CastleBackdrop.png`
- The driver and capture sources, for flags and log keys

I read `rounds/r13/CRITIC.md` only after the score was written (last section).

**Evidence:**
- Location: `docs/haunted-castle/rounds/r14/critic/<run>/`.
- Committed: `player.log` for every run, `grabcam.txt`, `runs.txt` and `audio_audit.txt`.
- Not committed: the frames (1.0 GB). They stay on the Studio checkout at the same paths.
- Audio and pixel measures used my own scripts (numpy and PIL), not the game's scalars.

| run | flags | result |
|---|---|---|
| natural1-3 | `-hc-playtest <d> natural` | won ×3. Plans Late / Mid / Early. 521 / 530 / 518 s |
| siegeheld | `siege -hc-hold-dread 60` | 6 hidden hunts, 916 s |
| siege | `siege` | 3 hunts. Near-miss open, then a grab |
| huntcam | `huntcam` | natural dive, then death. Lost card |
| cold | `cold` | pass, 5/5 hides answered |
| fear | `fear` | won. Every scripted beat fired |
| escape, kill, longnight, death | as named | won / won_kill (3 hits) / won / death plus checkpoint retry with penalty |
| doorproof, settings | as named | PASS (toward=0, 8 toggles) / 3 pages |
| verify (×2) | `-hc-verify` | 64 PASS, 0 FAIL, `HC_NATIVE_ROUTE_PASS`. `HC_PERF pass=False` |
| cap_rooms, cap_rooms_lantern, cap_title | `-hc-capture` (plus lantern on, plus title) | S1-S8, H01-H10, title |
| art_props / exterior / doors / kit / layout (off, on) / instruments (on, off) | `-hc-capture-art` | all views written |

Totals across all logs: 0 exceptions, 0 `HC_PLAY_BLOCKER`, 0 `HC_PLAY_TELEPORT during_grab`.

## Score: 6.0 / 10

| Axis | Score | Reason |
|---|---|---|
| Fear loop, natural | 7.5 | Every numeric pass test passes (below). The new structure flaw is that apparitions now lead hunts by 26–57 s in 6 of 6 cases. |
| Hunt readability and the kill | 5.5 | The commit frame is visible and the HUD hidden in 17/17 grabs, and the driver's per-frame continuity passes 17/17. But 59 % of grabs are the same canned crawl. Side snatch played 0/17. The "drop" skips its own fall. Hide grabs are near-black (mean luma 5/255). |
| Hiding | 7.5 | Hidden survival 8/11 = 73 %. The near-miss opened, the door held, the hide was learned. |
| Visuals | 5.0 | S1 5/10, S4 5/10 (pass is 6). Title 9/10. The moat is now red but reads as glitter. |
| Audio | 7.0 | Clean inventory. Every Count line comes from his head (27/27). But `cc_hunt_found` is 10 of the 27. |
| Onboarding / UI | 7.0 | Cold pass. 0 layout overlaps on every document screen. Settings has one misaligned toggle. |
| Stability / perf | 7.0 | 0 errors in 28 launches. S4 p99 was 17.56 ms in one of two captures. Bellwether `performance_pass=False`. |

## 1. Fear loop in natural play (3 runs, no forced dice)

All values come from my parser (`HC_HUNT_START`, `HC_WARNING`, `HC_SCARE visible=`) and are cross-checked against `HC_PLAY_STATS`.

| run (`HC_RUN_PLAN`) | hunts: t (kind, warning s, hunt s) | apparitions (t) | share | longest quiet gap | first hunt |
|---|---|---|---|---|---|
| natural1 (Late, history L,E,L) | 168 (told 6.5, 29.9), 352 (no-tell, 38.3), 512 Return | 90, 135, 270, 319, 466 | 0.161 | 162 s | 168 |
| natural2 (Mid, burst start) | 146 (no-tell, 30.7), 327 (no-tell, 48.6), 455 (told 11.4, 29.1), 517 Return | 56, 105, 270, 400 | 0.251 | 150 s | 146 |
| natural3 (Early) | 63 (no-tell, 49.3), false warning ~295 (5.7 s), 353 (told 5.9, 33.3), 507 Return | 134, 195, 270, 307, 456 | 0.203 | 182 s | 63 |

- **Hunt share 15–30 %:** 16.1 / 25.1 / 20.3 %. **Pass.**
- **Longest quiet gap ≥ 120 s:** 162 / 150 / 182 s. **Pass.** Each run also has a second gap of at least 121 s after its first hunt.
- **Non-Return hunts starting within 10 s of an apparition:** 0 of 7 (**0 %**). **Pass.**
- **Pooled apparition CV:** **0.50** over **11 gaps** (mean 94.7 s, sd 47.7 s). **Pass, at the threshold.** The per-run CVs are 0.51 / 0.42 / 0.53.
- **First-hunt spread:** 63–168 s = **105 s**. **Pass.**
- **New pattern (a learnable clock):** every non-Return threat that had an earlier apparition began **26.5, 33, 41, 57, 43.6 and 40.1 s** after it.
  - That is 6 of 6 inside 26–57 s. Only natural3's first hunt (t 63, no apparition yet) falls outside the window.
  - Cause: the apparition gate (`HC_APPARITION_GATE deferred=hunt ... opensIn=7.7`) and the 20 s `DiceDread` exclusion. Together they make "apparition, then about 30 s of safety, then danger" the castle's rhythm.
  - The 0 % "within 10 s" result passes because hunts never come that early, not because they are unpredictable.
- **Channels:**
  - The no-tell hunt in the fear run logs `HC_PLAY_CHANNELS no_tell changedFrames=0 heartbeatMaxDelta=0.000 wingMaxDelta=0.000 voiceLines=0`. No HUD, music, heartbeat or wing channel moves at a no-tell start.
  - Told hunts have a deliberate tell: candles, the "SIGNAL COLLAPSING" HUD, a dispatch card ("Wings in the rafters…") and the objective line "BREAK LINE OF SIGHT".
  - Torch: `HC_TORCH_FAIL source=dive` fired on **7 of 13** dives, 0.0–1.0 s before contact, against 4 random `source=hunt` failures. The lantern drop-out still predicts a grab about half the time.
- **Scare layer between hunts:**
  - Per natural run: 20–22 `HC_SCARE_CASTLE` beats (bat, knife, book drop, candle gutter, window silhouette, single bell), 2–3 `HC_LIE footsteps_behind`, 2–4 silences, 1–2 throws and 7–9 `HC_WATCHED` cues. The layer is rich and varied.
  - Natural2 logged 15 `HC_THROW_SKIP` (poltergeist props out of range or behind her).
- **While hidden:**
  - Apparitions stop completely: 0 in the 916 s Dread-held siege (by design, `CastleBlocked`).
  - The only layer left is the 4 `HC_HIDE` clips (16 beats in 916 s) and the near-miss with `cc_hunt_smell`.
- **Hostile Return:**
  - All 3 natural runs logged `HC_HUNT_SPAWN hostileReturn=True` and a grab on the Return, and the driver says "a mortal player died on the bridge" in all three.
  - In natural2 and natural3 the `HC_RETURN_KILL would_die=true` line is logged with `phase=Won huntActive=False` at 5.8–6.6 m, after `bridge_crossed`. Either the kill is credited after the win, or the line is late. Either way it is a defect to settle (see secondary issues).

## 2. Hunt readability and the kill

**17 grabs across all runs** (`HC_GRAB_COMMIT`):

| mode | count | where |
|---|---|---|
| 9 crawl | **10 (59 %)** | every hide grab (4/4), plus 6 open |
| 8 drop | 6 | |
| 12 drop-spin | 1 | natural1, Return |
| 10/11 side snatch | **0** | |

- **Side snatch never plays.** It was planned twice (`HC_GRAB_VARIANT plan=side-right`, `plan=side-left`), and both were dropped: `dropped=side-left reason=at the start point, came in low (0.1 m)`.
- **Planned drops degrade to crawls:** 3 times with headroom up to 12 m (natural2 ×2, natural3 Return, cold).
- **Drop share where the ceiling allows:** 7 of 12 headroom grabs are 8 or 12 (58 %). That passes on paper.
- **The drop does not show the drop:**
  - Mode 8 commits with `clip_start_t=0.22–0.50`, so the fall is cut.
  - In natural1 #2 and cold #2 he is standing on the floor 2–3 m away at t=0.01 (`headAboveEye 0.58`). He then squats (`natural1/18-25`, `cold/16`).
  - Only huntcam (`headAboveEye 1.41` at commit, `huntcam/11`) and the drop-spin (`2.19`, `natural1/29`) read as coming from above.
- **The crawl is canned.** Every mode-9 grab replays the same camera-relative frames: skull at (1280, 614) → (1280, 617) → (1281, 601) → (1281, 556) → (1280, 429) at 2560 scale, with `claw_px_proj` 158 → 160 → 165 → 209 → 266. This holds in natural1, natural2 ×3 and natural3 (`grabcam.txt`). That is the "identical kill animation" anti-pattern.

**Continuity (pop test):**
- Driver per-frame skull trace (`HC_PLAY_GRAB_CONTINUITY`): **17/17 pass**. `commit_gap=0.00` every time, the drop modes only descend, max step 0.34 m, crawl steps ≤ 0.21 m.
- From the `HC_COUNT_GRABCAM` samples, the root `pos=` jumps between the `begin` line and the t=0.01 sample:
  - 0.52 m (natural1 drop)
  - **0.96 m** (natural1 drop-spin, `HC_GRAB_SEAT gap=0.97`)
  - 7 grabs at the clamped `gap=0.45`
  - Each one then slides back over 0.35–0.48 s, for example drop-spin x −1.10 → −2.04 m by t=0.46.
- Those jumps happen in the same frame as a **hard camera cut** (`cut=True cutWhy="turn 140 > 90"`, 6 of 17 grabs). The cut hides the re-seat; it does not remove it.
- The GRABCAM lines do not log the skull's world position, so the brief's sample test cannot be run on them independently. Only the driver's scalar sees the skull, and it reports exactly 0.00 even when the root moved 0.96 m.
- Modes 10–11: no samples, never played.

**At +0.3 s:**
- The skull is in the central 60 % in every grab sample except the huntcam commit frame (`skull_px=(1428,259)`, top 18 %, camera still turning).
- Claws measured from pixels at 1920 (wrist to fingertip, ×1.333 to 2560):

| grab | pixels (at 2560) | game's `claw_px_proj` | verdict |
|---|---|---|---|
| natural1 crawl t0.30 | ~195 px (≈ 260) | 160 | pass |
| huntcam drop t0.30 | ~240 px (≈ 320) | 325 | pass |
| natural1 drop-spin "t0.30" | ≤ 150 px (≈ 200), the hands are cut by the frame bottom | 150 | borderline |

  The game's measure is conservative on the crawl. The drop-spin frame was taken late: the shot queue interleaved `31_drawbridge_lowered.png`.
- **HUD hidden from commit:** `HC_GRAB_HUD pixels=0 draws=0` on every grab, and no HUD in any grab frame. **Pass.**
- **Hide-grab readability fails.** `siegeheld/06-07`, `siege` and `fear/17` (hide-open): mean luma **5.7 / 5.2 / 255**, with 79–83 % of pixels below 10. Only the ember eyes and the red wings show (`fear/17`: eyes only, bottom right). "Visible" per `HC_GRAB_FIRSTFRAME`, effectively invisible on a real monitor.
- **Pre-dive readability (huntcam, hall):** good. Perched, then a wide-wing glide closing from 7.0 m to 2.3 m over 1.3 s (`huntcam/06-10`). The HUD shows in pre-dive frames as it should.
- **Lantern recolour:** none seen. The lantern stays warm amber through every grab.
- **Mesh:** no holes at grab distance. There are **stretched planar shards at the coat hem and thighs** in the drop frames (`huntcam/15`, `natural1/32`).
- **Harness caveat:** on the Return the invulnerable driver keeps walking during the grab presentation (eyeToSkull 2.7 → 5.0 m, `natural2/38-44`). Return-grab framing is not judged.

## 3. Hiding

- **Dread-held siege (band 50–70):**
  - 6 hidden hunts in one hide (Count's wardrobe), 7 hide attempts.
  - `HC_DOOR_TRY` ×7 with `HC_DOOR_HELD` ×1 at attempt 5. **Pass.**
  - `HC_COUNT_NEARMISS begin` ×4, `open=True` ×1 on hunt 6, which became a grab. **Pass.**
  - `HC_COUNT_LEARNED hide=Count's wardrobe reason=found her in it` on hunt 3. **Pass.**
- **Re-commit loop:** none in the hidden runs. 1 commit per open, and the end reason is `blocked` (driver invulnerability).
  - In open play I saw `HC_COUNT_DIVE miss=True reason=recommit blocked: released 1.4 s ago` (natural2, cold). The guard works.
- **Hidden survival, all hidden hunts:** siegeheld 5/6, siege 2/3, fear 1/2 (the witnessed hide is driver-forced). Total **8/11 = 73 %**, or 7/9 = 78 % without the fear run. **Pass (≤ 85 %).**
- **Watched hide:** fear `witnessed_hide seenBeforeHiding=True`, then `path=hideopen` mode 9 grab. Correct, but unreadable (luma 5).
- **Hidden beats by phase (`HC_HIDE`, siegeheld):** Disturbance 9, Recovery 3, Warning 1, Hunt 3.
  - About 215 s hidden in hunts and about 700 s calm: **1.4 per 100 s in hunts vs 1.7 per 100 s calm.** Equal enough. Not a phase lamp.

## 4. Visuals

**Great hall S1 vs `refs/great_hall.png`: 5/10** (`cap_rooms/S1_great_hall.png`, lantern on: `cap_rooms_lantern/`)
- Better than a 4: a ribbed vault is now visible, plus candle chandeliers, floating books, gallery balustrades, the rose window with both moons, the rune ring and the shrouded skeleton.
- Against the reference:
  - **No lancet windows** in the S1 frame. The reference is defined by three tall moonlit lancets.
  - The piers are squat block-clusters, not soaring clustered columns.
  - The banquet table is a small dark shape at the far end. It is legible only with the lantern on.
  - The floor is matte. The reference's wet reflective floor is missing in the hall (the chapel has it).
  - The bricks are a clean, uniform, toy-like grid everywhere (portrait hall, gatehouse).

**Exterior S4 vs `refs/exterior_sky.png`: 5/10** (`cap_rooms/S4_exterior_sky.png`)
- Good: textured pale and red moons, an aurora, lit windows, conical spires, the bridge with lamps.
- Against the reference:
  - The castle is a symmetric ring of fat round towers, closer to theme-park than gothic. It does not match the jagged silhouette the title backdrop promises.
  - The rock base is dark mounds plus a low-poly boulder in the foreground.
  - **The moat is red now** (R/G 3.45 in S4, 1.84 in `gate_out_S6`), **but it is not reflective.** It is a high-frequency glitter (luma sd 29–33) with no mirrored moon or castle.
  - In `art_exterior_battlement_out` and `bridge_left`, a second, **striped red moat material meets the glitter one at a hard seam.**

**Other views:**
- **Chapel S2:** 5.5. Lancets with tracery, stained rose, pews, a wet reflective floor, floating candles.
- **Crypt S3 / crypt_ceiling:** 5. The hanging hand-roots are the best surreal piece. The sarcophagus is a red brick box.
- **Perch S5:** 6. A strong silhouette in the rose window.
- **Props:** 4.
  - `hall_skull_from_east` and `hall_skull_close`: the skull reads as a crude beak.
  - `hall_table_books`: no books are discernible at lantern-off exposure.
- **Capture views:** `gate_out_S6` and `battlement_out` are 4. Flat far bank, sparse dead trees, cut-out mountains.
- **Title vs mock:** **9/10.** Nearly identical (`cap_title/S7_title_castle_2416x1292.png`).
- **Dispatch card / ledger / journal / end and lost cards:** good, parchment layout, readable. `HC_DOC_LAYOUT overlaps=0` on ending ×7, ending_lost ×2, journal ×3 and settings ×1.
- **Flame quads near the camera:** fine in S1, S5 and the layout views.
- **Artifacts:** the moat seam, the hem shards, and the purple grave-ash pool on a flat brick wall with a white *human* handprint labelled "claw-scores" (`natural3/04`).

## 5. Audio

My audit is `critic/audio_audit.txt` (48 kHz mono, peak, clipped samples, BS.1770 LUFS, DC, loop seams and edge samples).
- **Inventory:** 0 clipped samples in any file. Peaks −5.0 to −16.3 dBFS.
- **Loudness:** voices sit at −24.2 LUFS; effects and beds at −28 to −34.
- **The 5 new `castle_hide_*` clips are clean:** peak −5.0 to −13.1, edges ≤ 0.0007, about −28 LUFS.
- **Loops:** the seam jump is at or below the clip's own p99 sample step (wind 0.0012 vs 0.012, heartbeat 0.0002 vs 0.0017). No seams.
- `crypt_blood_trickle` starts and ends at 0.037. It is seamless if looped and a small click if not.
- **No music in play:** the castle mixer targets 0 for house and hunt music (`Soundscape.cs:276-278`). No castle run logs a music measure, so this is **source-only and not proven at runtime.**
- **Heartbeat:** at the no-tell start `heartbeatMaxDelta=0.000`, and it follows `FeltDread`. Not a phase lamp.

## 6. Bellwether `-hc-verify` and leakage

- Both runs: **64 PASS, 0 FAIL**, `HC_NATIVE_ROUTE_PASS`, "all three music states executed", audio below −3 dBFS.
- **`HC_PERF pass=False`** in both: engine cost over 16 ms on 85 frames (max 43.4 ms) and on 98 frames (max 33.0 ms). p99 is 11.9 ms over about 37,190 frames.
- **No castle leakage:** no castle, Count or moat strings in the verify logs.

## 7. Errors, soft-locks, frame time (1920×1080 window)

- 0 exceptions and 0 soft-locks in 28 launches. Every playtest reached `HC_PLAY_RESULT` with `blockers=0`.

| view | p99 | max | notes |
|---|---|---|---|
| Great hall S1 | 11.90 / 11.11 ms | | |
| **Exterior S4** | **17.56 ms** (capture 1), 11.45 ms (capture 2) | 19.1 ms | 4 main-thread frames over 14 ms with no GC, logs or texture loads |
| `gate_out_S6` | 15.93 ms | | |
| `battlement_out` | 15.18 ms | | |
| Crypt (settled) | 11.76 ms | 16.9 ms | after the shot: 121 ms, the screenshot frame |
| Hunt dive | 11.30–11.62 ms | | with screenshot frames excluded |
| Chapel | not measured | | no capture measures it |

- **Pass ≤ 16.7 ms p99: S4 fails 1 of 2.**

## 8. Onboarding, doors, instruments, voice (r14 additions)

**(a) Cold onboarding** (`cold/`, `history=-,-,-`, `cold=True`):
- The letter is read at 4.4 s. The first watched cue comes at 7.8 s (door slam, occluded, 9.9 m).
- 4 cues before the first hunt: door slam, footsteps behind, a whisper on the right, a breath on the left.
- Nudge at 64 s: "A ledger page is still waiting in the dining hall (west wing)."
- The first hunt is a **told** warning of 10.3 s at t 105, with the objective line "BREAK LINE OF SIGHT · hide [E] · lantern off [F] · stay quiet".
- Verdict: good direction and a clear tell.
- Weakness: the cold gate opens on the arrival letter (`gate=page firstDocAt=4`). A new player is hunted at 105 s, before reaching a single ledger page, with 0 evidence.
- The driver only covers 180 s, not the full first five minutes.

**(b) Darkness and lantern:**
- Lantern-led: yes. With it off, 55–89 % of layout pixels are near-black (< 16). With it on, 28–48 %.
- But the lantern is a spot of 88°, range 15 m, intensity 1.53 (`HC_CAPTURE lantern on`). It behaves like a wide flashlight: hall pillars 12–15 m away brighten with it on (`art_layout_on/hall_from_doors`), not a lantern pool with falloff.
- Flat-lit spots: the gatehouse cart wall saturates orange under the lantern (`fear/22`). The evidence walls are a flat lavender disc (`natural3/04`).

**(c) Voice:** 27 `HC_COUNT_VOICE` lines, **27 `from=head`**. `HC_VOICE_SUMMARY from_other=0 refused_other=0` in all 22 summaries. **Pass.**

| line | count | logged distances |
|---|---|---|
| `cc_hunt_found` | 10 | 2.6–5.7 m |
| `cc_hunt_smell` | 7 | 0.4 m (near-miss) |
| `cc_bargain` | 5 | from his head at 13.3–13.5 m, occluded |
| `cc_hunt_run` | 4 | |
| `cc_stay` | 1 | 2.0 m |

- The repertoire is thin: one line is 37 % of all speech.

**(d) Footsteps and hides answering E:**
- Hides answered E 5/5 in cold (`HC_PLAY_COLD_HIDE ... hidden=True`) and 8/8 hide spots registered. **Pass.**
- Player footsteps have no log key. **Not proven.**

**(e) Documents:**
- Journal, ledger, letter, end, lost and settings cards are readable, and `overlaps=0` everywhere.
- Settings page 0: the "Detailed field help ON" toggle sits about 65 px left of the other toggles' column (`settings/02_settings_p0.png`).
- The left menu button names the *next* page, not the current one (p1 page "Blood Ledger" with "CASTLE NIGHT" highlighted).

**(f) Doors:** `HC_DOORPROOF toward=0 toggles=8 unfocused=0 leaves=4 PASS`. Both sides of all 4 leaves swing away. The closed doors are visible from both sides (`art_doors/*closed*`). The 0.5 s frames show the leaves mid-swing away (`doorproof/02-09`). **Pass.**

**(g) Grab variety:** see section 2. Mode 9 is 59 %, side snatch 0, drop-spin 1, drop 6 with the fall cut. **Fail on side snatch.** The first frame of a side or spin grab is not judgeable (no side grab ever played). The one spin first frame (`natural1/29`) reads well: him above, arms raised, camera pitched up.

**(h) Period instruments:** `HC_INSTRUMENTS period=True`. Brass compass (reading 5 lights the lamps), grave-ash lantern (violet), ear-trumpet, resonance box with horn (`art_instruments_on/*`). They read as period and hold up with the lantern off. The black blocky glove hands are the weak part. **Pass.**

## 9. Top three changes, each with a test

**1. Bring S1 and S4 to the reference (single biggest gap: visuals have been stuck at or below 5 for two rounds).**
- S1 test, in `S1_great_hall` at 1920×1080:
  - At least 3 pointed lancet windows 150 px or taller, with a moon visible in at least 1 of them.
  - Piers at least 2× taller than wide in frame.
  - The banquet table at least 15k px with the lantern off.
  - A reflective floor, measured as the rose window's mirror: a bright cluster below the horizon within ±60 px of the window's x.
- S4 test:
  - One moat material: no luma step over 25 along any horizontal line crossing the moat in `battlement_out` and `bridge_left`.
  - Moat luma sd ≤ 12 in the patch under the bridge.
  - Both moons mirrored: a red cluster with R > 120 in the moat below the red moon's x ± 80 px.
  - Blind critic S1 ≥ 6 and S4 ≥ 6.

**2. Make grabs actually vary, and make hide grabs visible.**
- Over natural ×3, cold and huntcam:
  - Mode 9 ≤ 40 % of all `HC_GRAB_COMMIT`.
  - At least 1 committed mode 10 or 11.
  - Every `HC_GRAB_VARIANT dropped=side-*` followed by a side commit within the same hunt, or that line not appearing at all.
  - No planned drop committing as 9 when `ceilingMax ≥ 4`.
  - Mode 8 `clip_start_t ≤ 0.1`, with the t=0.01 sample at `headAboveEye ≥ 1.0`.
- Hide grabs (`path=nearmiss` and `hideopen`): t=0.16 frame mean luma ≥ 18, and ≥ 5 % of pixels > 60 inside the Count's screen bounds.
- Add the skull world position to `HC_COUNT_GRABCAM` so the critic can run the pop test against the 0.45–0.97 m root re-seat under the camera cut.

**3. Break the apparition-to-hunt clock.**
- Over 3 natural runs (at least 7 non-Return threats with a prior apparition):
  - At most 50 % start 25–60 s after the last apparition.
  - At least 1 starts 10–20 s after one.
  - Keep the brief's ≤ 35 % within 10 s.
- Also reduce `HC_TORCH_FAIL source=dive` to ≤ 30 % of dives, or add enough random `source=hunt` failures that a failure precedes a contact ≤ 1.5 s later in under 40 % of failures.

## Secondary issues

- **Post-win Return kill:** `HC_RETURN_KILL would_die=true phase=Won huntActive=False dist=5.75–6.59` after `bridge_crossed` (natural2:937, natural3:729). Test: no `would_die=true` with `phase=Won` in 3 natural runs.
- **Exterior p99:** S4 p99 of 17.56 ms from 4 main-thread frames with no GC or logs. Test: 3 S4 captures with p99 ≤ 16.7 ms.
- **Voice repertoire:** add at least 2 more hunt lines. Test: no line above 25 % of `HC_COUNT_VOICE` across 3 natural runs plus siegeheld.
- **Cold first hunt:** gate it on the first ledger page or evidence record, not the arrival letter. Test: cold `HC_HUNT_TELL firstDocAt` ≥ first ledger page time.
- **Hidden scare layer:** only 4 hide clips, and no apparitions while hidden. Add a consequence-free sighting through the slats.
- **Settings page 0:** fix the misaligned toggle.
- **Grave-ash mark:** the handprint glyph contradicts "claw-scores".

## Not judged

- Real-human input and feel.
- Spatial audio by ear (no audio capture). Player footsteps (no log key).
- Music at runtime in the castle (no log key).
- Return-grab framing (the driver walks during the presentation).
- Side-snatch first frame (never played).
- Chapel frame time (no capture measures it).
- The first five minutes beyond the cold driver's 180 s.
- Long Night hidden behaviour: the escape-route longnight only proved `HC_NAIL_HIDES nailed=2` and a survived bell hunt.

## r13 pass-test numbers (this build)

| test | r14 | pass |
|---|---|---|
| hunt share | 16.1 / 25.1 / 20.3 % | yes |
| longest quiet gap | 162 / 150 / 182 s | yes |
| % non-Return hunts ≤ 10 s after an apparition | 0/7 = 0 % | yes |
| apparition CV (pooled, 11 gaps) | 0.50 | yes (at threshold) |
| first-hunt spread | 105 s (63–168) | yes |
| hidden survival | 8/11 = 73 % | yes |
| siege-held near-miss open / door held (≥ 5 tries) / learned | 1 / 1 of 7 / yes | yes |
| grab continuity (driver, per frame) | 17/17 | yes, but blind to the root re-seat |
| drop share where the ceiling allows | 7/12 = 58 % | yes (the fall is cut) |
| side snatch / drop-spin present | 0 / 1 | **no** |
| claw ≥ 200 px at +0.3 s (pixels) | crawl ≈ 260, drop ≈ 320, spin ≤ 200 | 2 of 3 |
| HUD hidden from commit | 17/17 | yes |
| S1 / S4 | 5 / 5 | **no** (≥ 6) |
| frame time ≤ 16.7 ms p99 | S1, crypt, hunt yes; S4 17.56 in 1 of 2 | **no** |

## What changed since r13 (read after scoring)

- **Score:** r13 5.5 on f00e343, r14 6.0.
- **Kill presentation:** fixed in its r13 sense.
  - Continuity went from 0/6 to 17/17 on the driver's per-frame measure, and the driver no longer teleports mid-grab.
  - The dispatch card no longer survives into the Return kill (HUD 0 px on 17/17).
- **Voice:** `cc_bargain` and `cc_listen` were disembodied in r13. Now 27/27 lines come from his head.
- **Apparitions:** thicker. 4–5 per run against r13's 2–3, with the CV holding at 0.50 against 0.57 over 5 gaps.
- **Drop-spin:** went from 0 to 1.
- **Side snatch:** still 0. r13 already flagged "10/11 and 12: 0 of 12".
- **Crawl dominance got worse:** 59 % now.
- **Hiding:** stays healthy (69 % then 73 %).
- **Visuals:** moved from 4/4 to 5/5. The moat went from grey to red, but it is still not reflective, and S1 still has no lancets or readable table. r13's S1 test (3 lancets of 150 px or more, table in frame) still fails.
- **Bellwether verify:** still `performance_pass=False`.
- **New since r13:** the apparition-to-hunt clock (26–57 s, 6/6), the post-win `RETURN_KILL` line, and the hide-grab darkness.
