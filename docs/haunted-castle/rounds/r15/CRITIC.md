# Round 15: blind playtest critic, Haunted Castle

[Opus 5.5 high]. Blind critic for round 15.

**Build under test**
- Code commit **9269090** (`ff-integ tuning pass`).
- Bakes from b8662ce. The `haunted-castle` tip b46865c adds only docs.
- Binary `Builds/macOS/Hollow Current.app`, `Assembly-CSharp.dll` dated 2026-10-19 14:03:44.
- The brief's header says 6229407. The director's launch message and the brief's own last line both say 9269090, and the binary matches 9269090 plus the b8662ce re-bake (built 13 s after that commit). Every finding below is on that binary.

**How it was run**
- Mac Studio (`hostname` Mac), display 3440×1440. Playtests ran in a 1920×1080 window.
- Every launch went through `gpu_slot.sh capture --label hc-critic -- open -g -W -n -a … --args -logFile …`, one instance at a time, in the background. There was no user input, no timeouts, no SIGTERM, no crashes, and no relaunches.
- The slot table was 0/2 at the end.
- **Window caveat:** the capture sets ask for 2560×1440. The display cannot hold a 1359-px-tall window plus its title bar, so the PNGs are **2416×1292** (`HC_CAPTURE requested 2560x1440 window 2416x1359`, then resized to 1292 by the OS). That aspect ratio is 1.87, not 16:9.

**Evidence**
- Root: `/Users/midir/hollow-current-haunted-castle/docs/haunted-castle/rounds/r15/critic/` (1.2 GB, untracked). Each run has its own folder with `player.log`, numbered frames and `grabcam.txt`.
- Launch order and timings: `runner.log`.
- I wrote my score before reading `rounds/r14/CRITIC.md`. I did not read HANDOFF.md, the handoffs, PLAYTEST.md, AUTO_RUN.md, or the r13/r14 integrator captures.

**Runs (24 launches, 0 errors, 0 crashes)**

| folder | mode | result |
|---|---|---|
| natural1/2/3 | `natural` | won ×3, 587 / 540 / 522 s case time |
| siegeheld | `siege -hc-hold-dread 60` | 6 hidden hunts, 872 s |
| siege | `siege` | near-miss opened plus a recovery cut, 212 s |
| huntcam | `huntcam` | natural dive in the hall, dead |
| fear | `fear` | won |
| escape | `escape` | won |
| kill | `kill` | won, slain |
| longnight | `escape -hc-long-night` | won |
| death | `death` | penalty applied |
| cold | `cold` | `pass=True` |
| doorproof | `doorproof` | PASS |
| settings | `settings` | 3 pages |
| cap_rooms, cap_rooms_lantern | `-hc-capture`, lantern off / on | S1–S8, H01–H10 |
| art_* | `-hc-capture-art exterior, doors, kit, layout (off + on), instruments (off + on), props` | |
| title | `-hc-capture-title -hc-capture-size 1920x1080` | |
| verify | Bellwether `-hc-verify` | |

## Score: 6.0 / 10

| Axis | Score | Reason |
|---|---|---|
| Fear loop, natural | 7.5 | Every numeric pass test passes. Hunts are no-tell or told, irregular, and never right after an apparition. |
| Hunt readability and the kill | 5.5 | Hall drop and hide-open grabs read well at +0.16–0.6 s. But the dive→commit gap is 0.57–0.65 m in **9/9** dive commits. Crawls step 0.7–0.9 m per sample in 4/4, and one chapel crawl drops its skull 1.34 m in 0.3 s. The new side snatch is canned: identical skull pixels in 3 grabs, dark, and the claws are behind the held instrument. The hide-grab commit frames are near-black. Drop-spin played 0 times. A ledger page blocked three dives. |
| Hiding | 7.5 | 10/13 hidden hunts survived (77 %). The near-miss opened. Door held 1 time in 7 tries. The hide was learned. The watched hide opened. |
| Visuals | 5.0 | S1 5.5/10, S4 5/10 (pass is 6). Title 9/10. UI cards 8/10. The moat has a seam and glitter. Props and the skeleton read as toys. |
| Audio | 7.0 | Clean inventory. 32/32 Count lines came from his head. No music in the castle mix. Hidden beats are 2.6× more frequent per second during hunts than in calm. |
| Onboarding / UI | 7.0 | Cold pass. First cue at 10.9 s. 0 layout overlaps. "Flashlight" and "radio" leak into the castle UI. A no-evidence cold player is hunted at 156 s with 6 s of warning. |
| Stability / perf | 7.5 | 0 errors or exceptions in 24 launches. Castle p99 ≤ 16.7 ms everywhere measured. Bellwether `HC_PERF pass=False`. |

## 1. Fear loop in natural play (3 runs, no forced dice)

`HC_RUN_PLAN` bands were Late (history `L,E,L`), Mid (`M,L,B`) and Early (`E,M,L`).

| run | hunts: t (kind) | apparitions `HC_SCARE visible=` t | share | longest quiet gap | first hunt |
|---|---|---|---|---|---|
| natural1 | 165 (no-tell), 344 (Warning 13.0 s), 498 (no-tell), 576 Return | 90, 209, 258, 402, 468, 553 | 19.0 % | 164 s | 165 |
| natural2 | 128 (Warning 6.0 s), 301 (Warning 9.3 s), 400 (no-tell), 515 Return | 56, 91, 270, 382, 466 | 26.8 % | 130 s | 128 |
| natural3 | 77 (Warning 5.8 s), false Warning 8.0 s at ~357, 365 (no-tell), 511 Return | 146, 180, 230, 423, 454, 493 | 18.6 % | 228 s | 77 |

The share is the driver's per-frame phase integral. My own interval rebuild from `HC_HUNT_START`, warnings and recovery gives 20.9 / 26.9 / 14.7 %. `HC_RECOVERY seconds=` is not the hunt length (natural3 is still in `phase=Hunt` at `HC_RULES t=390`, 25 s after a 14.9 s "recovery"), so I use the driver's figure.

- **Hunt share 15–30 %:** 19.0 / 26.8 / 18.6 %. **Pass.**
- **Longest quiet gap ≥ 120 s:** 164 / 130 / 228 s. **Pass.**
- **Hunts starting ≤ 10 s after an `HC_SCARE visible=` line:** 0 of 8 non-Return hunts (0 %). **Pass (≤ 35 %).**
- **Apparition inter-arrival CV, pooled:** **0.60 over 14 gaps** (mean 87.1 s, population sd 52.7 s; 0.63 with the sample sd). Per run: 0.37 / 0.51 / 0.90. **Pass (≥ 0.5).**
- **First-hunt spread:** 77–165 s = **88 s**. **Pass (≥ 45 s).**
- **Apparition-to-threat lead** (the r14 clock problem): leads of 74.9, 72.6, 29.2 / 31.3, 21.2, 17.8 / 119.1, 134.9 s. Only 2/8 fall in 25–60 s, and 1 falls in 10–20 s. The clock is broken.
- **Does any channel predict a hunt?**
  - **HUD:** the Warning objective ("BREAK LINE OF SIGHT …") shows only for told hunts. 4 of 8 hunts were no-tell (`HC_HUNT_TELL kind=none`). There is no HUD tell before a no-tell hunt.
  - **Heartbeat:** follows felt Dread only. Felt was 0.0 at the first no-tell hunt in natural1 and the first told hunt in natural3. It is not a phase lamp.
  - **Music:** none in the castle mixer (`HC_CASTLE_MIX heartbeat=True wind=True`, no music source logged).
  - **Torch flicker:** still a partial grab tell. `HC_TORCH_FAIL source=dive` fired on 5 of 14 dives (36 %), and 4 of 13 torch failures came ≤ 1.5 s before a contact (31 %).
  - **Lamps:** no per-hunt lamp log exists. Not judged beyond the Warning strobe, which is by design.
- **Scare layer:** 20–22 `HC_SCARE_CASTLE` beats per 9-minute run, plus 5–6 apparitions. Lies (`HC_LIE`) 0–4 per run, silences 2–4, one thrown prop per run. The kinds vary: door_slam, ear_breath, ear_whisper, footsteps_behind, portrait_eyes, lantern_flicker.
- **Hostile Return:** a no-tell Return hunt in 3/3 runs (`HC_HUNT_SPAWN hostileReturn=True`). In fear, `HC_RETURN_KILL would_die=true phase=Return dist=3.06`, so the bridge can kill. There is no `would_die` after `phase=Won`; the r14 post-win kill is fixed. The Return dive in natural1 and natural2 was abandoned when the hunt ended at the bridge.

## 2. Hunt readability and the kill

**Grab counts.** 17 commits across all runs (`HC_GRAB_COMMIT`):

| mode | count |
|---|---|
| 8 drop | 5 (huntcam, cold, kill, death, fear Return) |
| 9 crawl | 7 (3 open dives, 1 headroom dive, 2 near-miss, 1 hide-open) |
| 10/11 side snatch | 4 |
| 6 | 1 (cold, second grab in the same hunt under invulnerability) |
| **12 drop-spin** | **0** |

- **Drop share where the ceiling allows:** 4/6 open headroom grabs (cold 1/2, huntcam 1/1, kill 1/1, death 1/1, natural1 0/1). **Pass on paper.** In natural play alone it is 0/1.
- **Drop-spin present: fail.** 0 in every run.

**Commit continuity.** I measured this from the samples, not from the driver's `commit_gap=0.00`.
- `HC_COUNT_GRAB root=… fromLastDiveSample=` reads **0.57, 0.57, 0.58, 0.59, 0.60, 0.60, 0.62, 0.63, 0.65 m**. That is all 9 dive commits that log it, over the 0.5 m limit in **9/9**. The driver's per-frame `commit_gap` is 0.00 in the same grabs. The ~0.6 m is a systematic offset between the last logged dive sample and the commit skull. Whether it is a real pop or a stale sample, the brief's test as written fails.
- **natural3 grab 3 (side snatch):** `HC_GRAB_FIRSTFRAME cut=True cutWhy="seat gap 0.93 > 0.45"` and `commit_gap=0.93`. A 0.93 m re-seat is hidden under a hard cut.
- **Crawl (mode 9), ≤ 0.5 m per sample. Fails 4/4 open crawls.** Per-sample skull steps, from the `skull=` field of `grabcam.txt` (samples 0.14–0.16 s apart):
  - natural1 #1: 0.22, 0.41, **0.89**, 0.16, 0.36
  - natural1 #2: **0.84, 0.69**, 0.05, …
  - natural2 #1: 0.10, 0.11, 0.49, **0.71**, …
  - natural2 #2: 0.13, 0.06, 0.34, **0.84**, …

  The 0.7–0.9 m step is the crawl's own forward lunge between +0.30 and +0.60 s, horizontal and in the clip's direction. It is the same lunge in every crawl, so it is not a teleport, but it breaks the ≤ 0.5 m limit. The two near-miss crawls pass (0.07 m).
  - **natural1 grab 2 (chapel, `natural1/16-23`) is the worst case.** The skull goes (11.97, 2.90, 6.51) → (12.10, 2.25, 7.03) → (12.13, 1.56, 7.10), steps of **0.84 m and 0.70 m**. The crawl clip was seated 1.33 m above the floor (`originY=1.58 floor=0.25`, "came in low (1.4 m)") and then fell (`root=landed y=0.25 t=0.36`). `root_slide=1.80`, `root_ok=False`. The commit frame (`16`) is a ceiling view of his torso with the skull off-centre (`skull_in_central60=False`).
- **Drop (mode 8), all 5:** the skull only descends in every sample (`dy` all ≤ 0; huntcam 4.78 → 4.49 → 3.78 → 2.64 → 1.89 → 1.84 m). Per-sample steps peak at 1.07–1.16 m in 0.16 s inside the falling clip (`fall_ok=True`). That is within clip velocity × dt + 0.3 m for a fall. **Pass.** The fear Return drop commits at `clip_start_t=0.27` (the others 0.00–0.07), so part of its fall is cut.
- **Side snatches (modes 10 and 11)**, judged from `pos=` and the frames:
  - The root teleports 2.3–3.5 m to a flank stand-off at commit (`root_jump=3.45 / 2.28 / 2.64 / 2.54`; `placed=side … standOff=2.20`).
  - The skull then lunges about 1.0 m per 0.15 s toward her, in a consistent direction.
  - It never comes closer than 1.54 m (`min_eye_skull=1.54`).
  - Per-sample skull steps are 1.03–1.08, 0.95–1.00, 0.67–0.68, 0.24–0.25 m in **all four** side snatches, which is one clip.
  - It is canned: natural2 #1, natural3 #2 and natural3 #3 log the same `skull_px` (1136,644) → (1251,608) → (1257,590) → (1267,604), the same `claw_px_proj` 301/348/337/384 and the same `eyeToSkull` 1.78/1.64/1.58/1.54 from t=0.46 on. That is one fixed cinematic. It repeats the "identical crawl" anti-pattern r14 found, now for the side snatch.

**At +0.3 s.** Skull in the central 60 % in every +0.3 frame; claws measured from pixels, wrist to fingertip, ×1.333 to 2560:

| grab | my pixels (at 2560) | game `claw_px_proj` | verdict |
|---|---|---|---|
| natural1 crawl #1 `12` | ≈ 170 px (≈ 227) | 165 | pass, marginal |
| huntcam drop `14` | ≈ 115–120 px (≈ 155–160), hands raised over his head 2.75 m away | 170 | **fail** |
| natural3 side-right `12` | right claw hidden behind the held ear-trumpet; left claw ≈ 100 px visible (≈ 135) | **299** | **fail** (the projection ignores the first-person instrument) |
| natural2 side-left `12` | near-black: mean luma 7.4, 2.3 % of the box > 60 | 295 | **fail** |
| fear Return drop `34` | `eyeToSkull 2.95`, hands small | 141 | **fail** |

- The claws reach 200 px only from +0.46–0.6 s.
- In 3 of 4 side snatches the held lantern or trumpet sits over his body or claws at +0.3 s (`natural2/12`, `natural3/12`, `natural3/26`).

**`grab_luma.py` (integrator's checker), my runs:**

| frames | result |
|---|---|
| hide grabs at t=0.16 | **2/2 pass**: siegeheld mean 29.6 / 16.6 % bright; fear hide-open 46.4 / 25.1 % |
| hide grabs at t=0.01 | **0/3 pass**: mean 5.5 / 5.5 / 8.6; 0.0 / 0.0 / 1.5 % bright |
| dive grabs at t=0.01 | 4/8 pass |
| dive grabs at t=0.30 | 5/8 pass. Side snatches in the dark corridor fail at 7.4–9.7 mean |

- **HUD hidden from the commit frame:** `HC_GRAB_HUD pixels=0` on 16/16 logged grabs, and no HUD in any grab frame. **Pass.**
- **First frame visible:** `HC_GRAB_FIRSTFRAME visible=True` on every logged grab. By pixels, the natural2 side snatch commit is a black mass with ember eyes, and the near-miss commits are near-black.
- **Pre-dive readability in the hall (`huntcam/06-11`):**
  - At 8.6 m and 7.5 m he is a few-pixel dark speck against a lit ribbed vault.
  - He is readable from 6.0 m (wings, red membrane).
  - The pitch-up camera (vault, balconies) reads well.
- **Lantern recolour:** none. The grave-ash lantern stays purple in hand and he stays unrecoloured (`HC_COUNT_GRAB light=lantern-excluded`). **Pass.**
- **Mesh holes:** none at grab distance in `huntcam/16-23` or `natural1/12-15`.
- **New credibility break: a paper page blocks the Count.** In natural1 hunt 3 (t 498–537) he hovered 2.2 m from her for the whole hunt:
  - three dives logged `HC_COUNT_DIVE miss=True reason=path blocked by Fallback / Ledger: the painter`
  - `HC_GRAB_PATH held=True … blocker=Fallback / Ledger: the painter`
  - the last dive was `abandoned: the hunt ended`.

  The ledger page's collider at the chapel stop is treated as a wall.

## 3. Hiding

| test | result | verdict |
|---|---|---|
| Dread-held siege, 50–70 band | 6 hidden hunts in the same hide (Count's wardrobe). Dread stayed 60.0–61.1. 4 `HC_COUNT_NEARMISS begin`, **1 `open=True` → grab** (hunt 5, `path=nearmiss`, `blockedDeaths=1`, released after the single blocked kill, no re-commit) | **Pass** |
| Hidden survival, all hidden hunts | siegeheld 5/6, siege 2/3, fear 1/2 (the watched hide is forced p=1.00), escape 1/1, longnight 1/1. Total **10/13 = 77 %** | **Pass (≤ 85 %)** |
| Re-commit loop | No hidden grab re-committed. Open-floor grabs under invulnerability re-commit after `recommit_blocked` expires (natural3 hunt 1 had 3 grabs). Crawl and drop grabs never release while invulnerable (natural1 #2 held 2,024 frames, cold 1,206). This is driver-only, but see the cold caveat in section 8. | — |
| `HC_DOOR_HELD` with ≥ 5 `HC_DOOR_TRY` | 1 held in 7 tries (attempt 4, Dread 60.2) | **Pass** |
| The Count learns the last hide | `HC_COUNT_LEARNED hide=Count's wardrobe reason=found her in it` in hunt 3. Afterwards `learnedHide=True`, `chance=0.31` (up from 0.15) | **Pass** |
| Watched hide (fear) | `HC_COUNT_WITNESSED` → landed, crawl, `hold doorDistance=0.50 seconds=1.6 opens=True`, rear-back, grab | **Pass** |

## 4. Visuals

**Great hall S1 vs `refs/great_hall.png`: 5.5/10** (`cap_rooms/S1_great_hall.png`; lantern on in `cap_rooms_lantern/`)
- **Better than a 5:**
  - Lancets are now in frame, two of them with moon discs, plus the rose window.
  - Piers read as 2.7:1.
  - Balustraded galleries, many lit candelabra, and a rune ring that reads as the reference's ring.
- **Still short of a 6:**
  - The lancets are small: about 190 rows of 1292, against a third of the frame height in the reference. The moons are about 40 px discs.
  - There is no ground mist.
  - There are no banners, fireplace or statues.
  - The giant skeleton in the foreground reads as a beige plastic model with red feathers. From `art_props/hall_skull_from_east` and `hall_skull_close` the **skull is not legible as a skull**.
  - The floating books are flat-shaded boxes.
  - The ribbed vault is almost lost in the dark.
- Exposure, lantern off: mean luma 18.2, near-black 30.6 % (`check_art_views` WARN, band 35–55). Lantern on: 14.4 % (pass).

**Exterior S4 vs `refs/exterior_sky.png`: 5/10** (`cap_rooms/S4_exterior_sky.png`)
- **Present:** two moons (the pale one textured), an aurora, a lit gate and towers.
- **Missing or wrong:**
  - The towers are cartoon cones.
  - The "rock base" is two dark ramps.
  - The far mountains are untextured silhouette cards (`art_exterior/battlement_out`, `gate_out_S6`).
  - The trees are flat orange sticks.
  - The moat is a flat red sheet with a pale smear at the bridge's left. The castle and the pale moon are not mirrored.
  - The 95° capture FOV stretches the pale moon into an ellipse.
- **`check_art_views.py` on my captures:**
  - S4 glitter right **FAIL** (sd 12.4 against a limit of 12; r14 was 30.7) and glitter left **FAIL** (16.5).
  - `battlement_out` seam **FAIL** (step 47 at (1697, 1166)); `bridge_left` seam PASS (4.3).
  - Red moon mirrored PASS (7,831 px); pale glade PASS (467 px).
  - S1 lancets, moon, piers, table (16,007 px) and floor mirror (405 px) PASS.
  - Overall 5 FAIL and 1 WARN out of 23.
  - The masks were made for the integrator's camera at 16:9. My frames are 2416×1292, so pixel positions may be offset.

**Other views**
- **Chapel:** dark (near-black 43.4 %) but readable: altar, stained glass, wet floor, pews.
- **Crypt:** the hand-roots and the glowing sarcophagus runes are among the best views in the game.
- **Lord perch (S5):** the Count framed in the rose window is a strong image.
- **Props:** the book table (`hall_table_books`) is good. The kit portrait (`art_kit_portrait`) is **96.8 % near-black**. The kit hearth's fire is a single glowing orange bar.
- **Doors:** visible closed from outside and inside (`art_doors/*`); the inside view is dark but legible.
- **Title** (`title/S7_title_castle_1920x1080.png`) vs `rounds/r10/title_mock.png`: near-identical layout plus a mist pass. **9/10.**
- **Cards:** dispatch card, ledger pages, journal, settings and end cards are legible and well composed. `HC_DOC_LAYOUT overlaps=0` on journal, settings and ending. **8/10.**
- **Lantern feel:**
  - The lantern-led darkness works in the hall and chapel.
  - Flat-lit spots appear when the lantern meets near walls: `art_layout_lantern/chapel_confessional` (mean 40.1, a hard disc of flat grey brick), the courtyard walls in `cold/05-07`, and the portrait-hall walls in `natural3/10-26`.
  - The tiling brick with uniform mortar reads as a kit.
- **Flame quads near the camera:** fine. **Artifacts:** the moat seam and smear above; no others found.

## 5. Audio

**Inventory** (python, 53 castle WAVs, all 48 kHz mono):
- 0 clipped samples. Peaks −5.0 to −16.3 dBFS. DC ≈ 0.
- Every loop's first and last samples match to within 0.001: wind, wing beats, crypt trickle, forge hum, candle bed and heartbeat.
- Loudness is consistent:
  - Count lines (including the four new `_b`/`_c` variants): −24.4 to −25.9 LUFS-ish.
  - `castle_hide_*`: −30.1 to −32.3.
  - Steps: −34 to −38.
- `castle_hide_handle` peaks hottest at −5.0 dBFS; no clipping.
- `book_drop` starts on a 0.40 transient. That is an impact, so it is fine.

**From the logs**
- No music in play.
- The heartbeat follows felt Dread with a per-run offset; it is not a phase lamp (felt 0.0 at two hunt starts).
- **Hidden beats by phase** (siegeheld plus siege, `HC_HIDE kind= phase=`):
  - Hunt 7, Calm 10.
  - Per hidden second: about 3.6 per 100 s in hunts against about 1.4 per 100 s in calm, from about 165 s of hidden hunt time against about 640 s of hidden calm time in siegeheld.
  - **Not equally likely: 2.6× more frequent in hunts.**
  - `hide_handle` came 5/5 in calm in siegeheld. The kind choice is weighted, not phase-keyed (`CastleScares.HiddenBeat`), so treat that as sample noise.
- **Count voice:** 32 lines, all `from=head`; `HC_VOICE_SUMMARY from_other=0 refused_other=0` in 21/21 summaries. Lines heard:
  - cc_bargain (×4, including two EVP answers `HC_EVP from=head verbal=True`)
  - cc_hunt_found / _b / _c (2 / 4 / 2)
  - cc_hunt_run / _b (3 / 2)
  - cc_hunt_smell / _b (5 / 2; every one at `dist=0.4` through a hide door)
  - cc_stay (1)

  `cc_hunt_found` is 8 of 32 (it was 10/27 in r14 terms).

## 6. Bellwether `-hc-verify` and castle leakage

- `HC_NATIVE_ROUTE_PASS`, 64 `HC_VERIFY PASS`, 0 FAIL. Audio stays below the −3 dBFS peak.
- **`HC_PERF pass=False`:** 82 frames have engine cost over 16 ms (max 33.3 ms), with p99 11.9 ms over 37,218 frames.
- No castle strings or assets in the Bellwether log.
- **Leakage the other way:** the castle UI says "Flashlight level" (settings p0), "F flashlight" and "R radio" in the castle HUD and footer hints. The castle tools are a lantern and a resonance box.

## 7. Errors, soft-locks, frame time

- 0 exceptions, errors or `HC_PLAY_BLOCKER` lines in 24 launches. No soft-lock.
- **Frame time** (capture window 2416×1292; the shot frame and the frame after it excluded):

| view | p99 | max |
|---|---|---|
| S1 great hall | 11.62 ms | 16.65 |
| S4 exterior | 11.11 ms (both captures) | 11.12 |
| `gate_out_S6` | 11.77 ms | 15.90 |
| `battlement_out` | 15.82 ms | 17.39, 3 frames over 14 ms, no GC |
| crypt (settled) | 11.80 ms | 16.39 |
| hunt dive | 11.16 ms | |
| chapel | not sampled by any capture set | |

  **Pass ≤ 16.7 ms p99 on every measured view.** `battlement_out` has the least margin.

## 8. Onboarding, doors, instruments (r14 additions)

**(a) Cold onboarding** (`cold/`, `HC_PLAY_COLD … pass=True`)
- The objective changes at 4.4 s to a precise one: "dining hall (west wing), portrait hall (upstairs, east wing), chapel".
- **Being watched:** 4 `HC_WATCHED` cues in 180 s, the first at 10.9 s (footsteps_behind, ear_whisper, door_slam, ear_breath). A nudge at 64.6 s.
- **First-hunt tell:** the cold gate deferred the Early band to the 150 s cap. Then a 6.1 s Warning with the "BREAK LINE OF SIGHT · hide [E] · lantern off [F]" objective and the dispatch card "Wings in the rafters. Get under cover…". He grabbed her at about 157 s (drop, `cold/12`) with **0 evidence and 0 ledger pages**.

  A first-time player dies 2.5 minutes in, having found nothing, after a 6-s warning. That is legal, but it is the harshest first impression in the loop.
- **Caveat:** the cold walk's frames at t34/64/94 are the same courtyard framing (`unstick=1`), so the route coverage is thin. The 5 hide checks ran **during a held grab** (`HC_PLAY_TELEPORT during_grab=True` ×5, `blockedDeaths=1206`). `hidesAnswered=5/5` is therefore contaminated and not proof.

**(b) Darkness and lantern:** see section 4. Lantern-led in the hall, chapel and crypt; flat-lit discs on near walls; the kit portrait is unreadable.

**(c) Voice from his body:** **pass**. 32/32 lines `from=head`; every line is listed in section 5.

**(d) Footsteps and hides answering E**
- Hides: all 8 audited focusable (`HC_HIDE_SPOTS_SUMMARY audited=8 focusable=8 failed=[]`).
- No log line covers player footsteps (only `groundSteps=509/1650` counts in fear), and I cannot listen, so footsteps are not judged.

**(e) Documents:** `HC_DOC_LAYOUT overlaps=0` on journal, settings and ending (3 screens); the frames confirm it. The settings page-1 dispatch preview card sits correctly.

**(f) Doors** (`doorproof`, `HC_DOORPROOF toward=0 toggles=8 unfocused=0 PASS`)
- Every leaf, from both sides, swings away (55° or 155°). Closed doors are visible from both sides.
- **But 8/8 closes needed `close=direct`:** after a 155° swing the leaf is flat to the wall and E no longer focuses it. A player cannot close these doors with E without walking to the folded leaf.

**(g) Grab variety:** side snatch present (4) but canned and dark or occluded; drop present (5); **drop-spin 0**. First-frame readability of the side snatch:
- right side (natural3 `10`): readable, crouched at 3.4 m
- left side, corridor (natural2 `10`): black
- natural3 `17`: dark, 2.9 % of the box > 60

**(h) Instruments** (`art_instruments*/`): compass (dial readable, needle, glowing tick), vellum ear-trumpet, grave-ash lantern (purple glass) and resonance box are period-plausible and in hand. The trumpet and resonance box are near-black silhouettes without the lantern on.

## 9. Top three changes, each with a test

**1. Bring S1 and S4 to the reference. This is the single biggest gap: three rounds at or below 5–5.5.**

S1 test (`S1_great_hall`, 1920×1080 capture):
- At least 2 lancets each ≥ 25 % of the frame height, with a moon disc ≥ 60 px wide.
- Ground mist: luma sd ≤ 6 and mean ≥ 20 in the floor band beyond the ring.
- At least 2 hanging banners ≥ 120 px tall.
- In `art_props/hall_skull_from_east`, the skull occupies ≥ 8 % of the frame with eye sockets distinguishable (≥ 2 dark blobs of luma < 15 inside a skull region of mean ≥ 40).

S4 test:
- `check_art_views.py` all PASS on a critic capture (seam ≤ 25 in `battlement_out`, glitter sd ≤ 12 both sides).
- A castle reflection: a cluster of ≥ 2,000 px of luma ≥ 25 in the moat below the keep's x ± 150 px.
- Ridge cards textured: luma sd ≥ 6 inside the mountain band of `battlement_out`.

Pass: blind critic S1 ≥ 6 and S4 ≥ 6.

**2. Make every grab readable at +0.3 s as a player sees it, and stop canning the side snatch.**

Over natural ×3, cold and huntcam:
- In ≥ 80 % of grabs, the claw measured from pixels (wrist to fingertip) is ≥ 200 px at 2560 in the +0.3 s frame.
- The held instrument covers ≤ 10 % of `countRect` from commit to +0.6 s: lower or drop the instrument at commit, as the HUD already is.
- Hide-grab commit frames (t=0.01) at mean ≥ 18 and ≥ 5 % > 60 in the box. They are 5.5 / 5.5 / 8.6 now.
- No two side snatches share `skull_px` within 20 px at t ≥ 0.46. Randomise the stand-off, height and lunge.
- `min_eye_skull ≤ 1.0 m` for side snatches (1.54 now).
- At least 1 drop-spin (mode 12) commit.
- `claw_px_proj` must account for first-person occluders, or be replaced by a pixel mask.

**3. Fix the commit seat and the blocked dives.**
- In every dive commit, `fromLastDiveSample ≤ 0.5 m`. It is 0.57–0.65 in 9/9. Either seat the clip on the last dive skull, or log the dive skull on the frame before the commit so the test measures the real gap.
- No `HC_GRAB_FIRSTFRAME seatCut=True` (natural3: 0.93 m under a cut).
- No crawl (mode 9) committed with `originY − floor > 0.5 m`; the chapel crawl fell 1.33 m and `root_slide=1.80`. Mode-9 skull steps ≤ 0.5 m per sample: slow the crawl lunge or sample it so no 0.15 s step exceeds 0.5 m. It is 0.71–0.89 now in 4/4.
- No `HC_COUNT_DIVE miss=True reason=path blocked by Fallback / Ledger:*`: document pages must not block his path or grab ring. Test: 3 natural runs with 0 such lines, and no hunt where he stays ≤ 2.5 m from a visible, unhidden player for over 10 s without committing.

## Secondary issues

Each has a test.

- **Torch dive tell:** `source=dive` on 36 % of dives (target ≤ 30 %), and 31 % of failures precede a contact within 1.5 s. Test: ≤ 30 % of dives over 3 naturals.
- **Hidden beats during hunts:** 2.6× the calm rate. Test: siegeheld per-hidden-second beat rate within ±30 % between Hunt and calm.
- **Door close by E:** 8/8 needed `close=direct`. Test: `doorproof` with 0 `close=direct`.
- **Bellwether leakage into the castle UI:** "Flashlight", "radio". Test: the castle settings and HUD strings contain neither word.
- **Cold first hunt:** at 156 s with 0 evidence and a 6.1 s Warning. Test: a cold run's first hunt comes after the first record, or its Warning is ≥ 9 s.
- **Kit portrait 96.8 % near-black; the hearth fire is a bar.** Test: portrait view mean ≥ 15, and a hearth fire with ≥ 3 flame shapes.
- **Bellwether `HC_PERF pass=False`:** 82 engine-cost frames over 16 ms. Test: `pass=True`.
- **Harness:** invulnerable crawl and drop grabs hold through the whole hunt, which contaminated the cold hide test (5 teleports during a grab). Test: the driver releases the player after the first blocked kill, as the side snatch does, and `teleportsDuringGrab=0` in cold.
- **Capture window:** the art sets run at 2416×1292 on this display. Test: a capture-size flag or crop that yields 16:9 frames, so the checker masks line up.

## Not judged

- Audio by ear: mix balance, positional voice, footsteps.
- Per-hunt lamp or candle behaviour: no log line.
- Chapel frame time: not sampled.
- Real death presentations in natural play: the driver is invulnerable.
- The Return grab framing beyond `fear/32-38`.
- Microphone hunting.
- Long Night beyond one hunt.
- Human input feel and mouse look.
- Bellwether gameplay beyond `-hc-verify`.

## r14 pass-test numbers (this build)

| test | value | pass |
|---|---|---|
| hunt share | 19.0 / 26.8 / 18.6 % | yes |
| longest quiet gap | 164 / 130 / 228 s | yes |
| % non-Return hunts ≤ 10 s after an apparition | 0/8 = 0 % | yes |
| apparition CV (pooled, 14 gaps) | 0.60 | yes |
| first-hunt spread | 88 s (77–165) | yes |
| hidden survival | 10/13 = 77 % | yes |
| near-miss open / door held / learned / watched | 1 / 1 in 7 / yes / yes | yes |
| commit ≤ 0.5 m from the last dive sample | 0/9 (0.57–0.65) | **no** |
| crawl ≤ 0.5 m per sample | 0/4 open crawls (0.71–0.89 lunge; chapel 0.84 + 0.69 fall); near-miss 2/2 | **no** |
| claw ≥ 200 px at +0.3 s (pixels) | 1/5 measured | **no** |
| HUD hidden from commit | 16/16 | yes |
| drop share where the ceiling allows | 4/6 = 67 % | yes |
| side snatch / drop-spin present | 4 / 0 | **no** (drop-spin) |
| S1 / S4 | 5.5 / 5 | **no** (≥ 6) |
| frame time ≤ 16.7 ms p99 | all measured views | yes |

## What changed since r14 (read after scoring)

- **Score:** r14 6.0 on the r14 build, r15 6.0 on 9269090. The gains and the new failures cancel out.
- **Fixed:**
  - The r14 apparition-to-hunt clock is broken: 2/8 leads in 25–60 s, against 6/6 before.
  - Mode 9 is down from 59 % to 36 % of the natural, cold and huntcam commits.
  - The side snatch now commits (4 against 0).
  - Mode 8 starts at `clip_start_t` 0.00–0.07 (4 of 5; the Return drop at 0.27), and the huntcam drop really falls (`headAboveEye 3.09` at commit).
  - Hide grabs at t=0.16 are visible (mean 29.6 / 46.4, against 5.7 in r14).
  - The skull world position is now logged, which made the sample pop test possible.
  - The S4 p99 regression is gone (11.11 ms ×2, against 17.56).
  - Lancets and a floor mirror are in S1.
  - The post-win Return kill is gone.
- **New or still failing:**
  - The sample-based commit test fails 9/9 at about 0.6 m. r14 could not run it.
  - The side snatch is the new canned kill.
  - Drop-spin disappeared (1 in r14, 0 now).
  - Claws behind the held instruments at +0.3 s.
  - The near-miss commit frame is still near-black.
  - The ledger page blocks dives.
  - The S4 moat is calmer (sd 12.4 against 30.7) but still fails the checker, and S1 and S4 stay below 6.
