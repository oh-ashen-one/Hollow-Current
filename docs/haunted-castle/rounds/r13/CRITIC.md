# Round 13: blind playtest critic, Haunted Castle

Build under test: **f00e343** (`haunted-castle` HEAD), binary `Builds/macOS/Hollow Current.app` dated 2026-10-18 20:01:10, both maps. Every run was launched through `gpu_slot.sh capture --label hc-critic -- open -g -W -n -a`, one instance at a time, windowed. Playtests ran at 1920×1080. Captures asked for 2560×1440 and got the largest window that fits the 3440×1440 display, which is 2416×1292 (`HC_CAPTURE requested 2560x1440 window 2416x1359`, screen 2416x1292). Nothing crashed. Every mode quit on its own, so no watchdog fired and nothing was killed. The slot was released after each run, and no Hollow Current process is left running.

Blindness: before scoring I read only `FEAR_PLAN.md`, `DESIGN.md`, `docs/research/phasmophobia-horror/README.md`, `refs/*`, `rounds/r10/title_mock.png` and the driver, capture, Door and Soundscape sources (for flags, log keys and code paths). I did not read HANDOFF, PLAYTEST, AUTO_RUN, any handoff, `rounds/r13/OWNER_FEEDBACK.md` or the integrator's r13 images. I read `rounds/r12/CRITIC.md` after fixing the score, and only for the last section.

Evidence root: `docs/haunted-castle/rounds/r13/critic/<run>/` holds `player.log` and the frames. It is about 1 GB and is **not committed**. My analysis scripts are `nat.py` (fear-loop stats from raw log lines, independent of `HC_PLAY_STATS`), `voice.py` (voice source classifier) and `audio.py` (BS.1770 loudness, clipping and loop seams). They live in my scratchpad and are not part of the repo.

| run | flags | result |
|---|---|---|
| escape | `-hc-playtest <d> escape` | won, 0 blockers |
| natural1-3 | `natural` | won ×3, 0 blockers, 513-533 s each |
| cold | `cold` | `HC_PLAY_COLD pass=True` |
| siege_held | `siege -hc-hold-dread 60` | 6 hidden hunts in 1018 s |
| siege | `siege` | near-miss open plus Recovery cut, 208 s |
| huntcam | `huntcam` | natural no-tell dive at 78 s, no grab inside the 3 s frame window |
| fear | `fear` | won, 0 blockers |
| kill | `kill` | slain in 3 hits, 0 blockers |
| longnight | `escape -hc-long-night` | won |
| death | `death` | died at 18.5 s, retry penalty=True, Dread 58.6 |
| verify | `-hc-verify -hc-evidence <d>` (Bellwether) | passed=True |
| cap_rooms, cap_lantern | `-hc-capture` (plus `-hc-capture-lantern on`) | S1-S8, H01-H10 |
| cap_title | `-hc-capture-title -hc-capture-size 1920x1080` | castle and Bellwether cards |
| cap_art_* | `-hc-capture-art exterior/props/doors/kit/layout/instruments` | all written |
| cap_artL_* | doors, kit and layout with `-hc-capture-lantern on` | all written |

## Score: 5.5 / 10

The bar is 8: a Phasmophobia-grade hunt loop in a hand-authored gothic castle.

| Axis | Score | One-line reason |
|---|---|---|
| Fear loop, natural play | 7.0 | Every numeric pass test now passes: share 17-20 %, gaps 136-232 s, 0/6 hunts within 10 s of an apparition, CV 0.57, first-hunt spread 108 s. Apparitions are thin, though: 2-3 per 8.5 min, and the first came at 230 s in natural2. |
| Hunt readability and kill | 3.5 | The game's own continuity check passed on **0 of 6** natural non-truncated grabs. The Count is missing from the commit frame in 3 of 8. The dispatch card stays up through a Return kill. A hide-open grab puts the camera 0.31 m behind his cranium. Side snatch and drop-spin never appeared (0 of 12 grabs). |
| Hiding | 7.0 | Hidden survival 9/13 = 69 %. 2 near-miss opens. The hide was learned and the door held. But the 1-in-3 open "bag" means the two hunts after an open are safe. |
| Visuals | 4.0 | S1 4/10, S4 4/10 (pass is 6). The moat is grey and dead. `gate_out_S6` and `battlement_out` are a fog plane under low-poly hills. The title is 8/10. |
| Audio | 7.0 | Clean inventory (no clipping, seams ≤ 0.0012). No music in castle play. Hidden beats are about equally likely in Calm and Hunt. But 8 of 21 Count voice lines came from somewhere other than the Count. |
| Onboarding (cold) | 6.0 | Clear objective and nudges, first watched cue at 8.7 s, 3 cues before the first hunt. But no sighting, and the first hunt is no-tell at 87 s while the case is still in Survey with zero evidence. |
| Stability and perf | 8.0 | 0 exceptions, 0 blockers in 24 launches. Great hall p99 11.5 ms, crypt p99 11.8-12.0 ms. Bellwether verify passes but reports `performance_pass=False`. |

## 1. Fear loop in natural play (3 runs, no forced dice)

Hunt timelines, from `HC_WARNING length=`, `HC_HUNT_START t= huntSeconds=` and `HC_SCARE visible= t=`. Warnings have no `t=`, so I placed them at the nearest preceding timed line. The `HC_PLAY_STATS` numbers agree to within 3 s.

| run | plan (`HC_RUN_PLAN`) | threat intervals (s) | apparitions (t) | share | longest gap | first hunt |
|---|---|---|---|---|---|---|
| natural1 | Mid 110-150, burst | warn 124-134, hunt 149-177, warn 303-314, hunt 324-370, Return 506→end | 63, 104, 303 | **20.4 %** | **136 s** | 149 |
| natural2 | Early 60-100, lull | **no-tell** 64-94, warn 326-339, hunt 342-380, Return 506→end | 230, 326 | **17.0 %** | **232 s** | 64 |
| natural3 | Late 160-200, burst | warn 158-164, hunt 172-206, **false warning** 300-310, warn 428-438, hunt 458-487, Return 522→end | 59, 113, 293 | **18.6 %** | **158 s** | 172 |

- **Share 15-30 %:** pass in all three.
- **Longest quiet gap ≥ 120 s:** pass in all three.
- **Non-Return hunts starting within 10 s after an `HC_SCARE visible=`:** **0 of 6 (0 %)**. Pass. The closest was natural1's 303 s apparition followed by the warning at 303 and the hunt at 324, which is 21 s.
- **Apparition inter-arrival CV, pooled:** **0.57 over 5 gaps** (41, 199, 96, 54, 180). Pass, but the sample is small.
- **First-hunt spread:** 64 / 149 / 172 s gives **108 s**. Pass.
- **Channel prediction:**
  - Hunts: no-tell hunts exist (natural2, cold, huntcam) and false warnings exist (natural3), so the candle-strobe warning is not a reliable contract.
  - `HC_PLAY_CHANNELS no_tell` in fear: `changedFrames=0` over 534 frames for HUD goal, instrument, music (`huntMusic/safeMusic/houseMusic=False`), breaker and danger. Nothing on screen or in music flips at a no-tell hunt start.
  - Heartbeat rises by at most 0.091 in the first 6 s, which is after the start.
  - Grabs: `HC_TORCH_FAIL source=dive eta≈1.0` fired on 5 of 8 natural dives (3 `HC_TORCH_SKIP`). A lantern dropout about 1 s before contact is a 60 % grab tell. That is acceptable as dread, but it is a pattern.
- **Scare layer between hunts:** 22-25 `HC_SCARE_CASTLE` beats per run plus 6-9 `HC_WATCHED` cues (door slam, ear breath, ear whisper, lantern flicker, footsteps behind), 1-4 `HC_LIE`, and 32-45 unseen perch moves.
  - **`HC_COUNT_PERCH flight observed=True` = 0 in every run.** He is never seen relocating between hunts.
  - Visible apparitions are thin: 2-3 per 8.5-minute run.
- **Hostile Return:**
  - natural2: Count at 1.75 m, `HC_RETURN_KILL would_die=true` at the inner gate (`natural2/player.log:662`).
  - natural3: `HC_RETURN_KILL would_die=true` at 3.16 m on the bridge.
  - natural1: closest 5.4 m.
  - The Return is genuinely hostile.

## 2. Hunt readability and the kill

Grabs across all runs (`HC_COUNT_GRAB mode=` and `HC_PLAY_GRAB_CONTINUITY`):

| run | mode | Count in commit frame | continuity (game measure, checked against frames) |
|---|---|---|---|
| natural1 #1 | 8 drop (hall gallery) | **no**: `t=0.01 skull_px=(-630,-1378)`, camera still turning (`natural1/10_grabcam_dive_t0.01.png`) | **fail**, step 0.64 > 0.37 at t=0.12 |
| natural1 #2 | 9 crawl (upper hall door) | yes (`18`) | **fail**, 0.70 at t=0.43. Contaminated: the natural driver's 20 s teleport moved the player mid-grab (eyeToSkull 2.06 → 10.99, frames 21-25 show another room). Driver defect. |
| natural2 #1 | 9 crawl (hall) | **no**: doorframe only (`natural2/10`) | **fail**, 0.45 at t=0.57. Visible lunge between `13` (t 0.46) and `14` (t 0.61). |
| natural2 #2 | 8 drop (Return, inner gate) | **no**: bridge view (`natural2/21`) | **fail**, 0.44 at t=0.23. **Dispatch card "He is out. He is over the courtyard..." stays on screen through every grab frame (`21`-`29`).** |
| natural3 #1 | 9 crawl (hall) | yes, dark silhouette (`natural3/10`) | **fail**, 0.52 at t=0.58 (crawl limit 0.5 m). Lighting pops from silhouette at t=0.01 to fully lit skin at t=0.16 (`10`→`11`). |
| natural3 #2 | 8 drop (Return) | yes (`22`) | 4 frames only (driver release), not judged |
| fear witnessed | 9 hide-entrance crawl | yes, far (`fear/17`) | **fail**, commit gap 0.55 > 0.5, step 0.38. From t=0.60 the camera is **0.31-0.46 m behind his cranium** (`fear/21`-`24`): a dark skull-back fills the frame, unreadable. |
| siege_held ×2, siege ×1 | 9 hide-open | yes, but a floor candelabrum stands between camera and skull (`siege_held/06`-`11`) | pass (9-10 frames before the driver block) |
| death | 8 → 9 ("skull below landed skull") | yes (`death/06`) | **fail**, 0.87 at t=0.65. The Count crawls **through the giant skeleton's ribs and spine** (`death/09`-`15`). |

- **Grab variety:** modes seen were 8 ×4 and 9 ×9 (one of those 9s was death's 8→9 switch). **Side snatch (10/11) and drop-spin (12): 0 of 12.**
  - Drop share outside hides and the Return: 1 of 5 = **20 %** (pass is 30 %).
- **At +0.3 s:** the skull is in the central 60 % in every visible grab. Claw size measured from pixels (scaled ×1.333 to 2560):
  - natural1 #1 t=0.31: ~320-333 px.
  - natural1 #2 t=0.30: ~285 px.
  - Both pass. The game's `claw_px_proj` under-reads (218 and 159).
- **HUD from the commit frame:** hidden in hall grabs. **Not hidden** in the natural2 Return grab (dispatch card).
- **Pre-dive readability (huntcam):** good. A winged silhouette leaves the rose-window gallery 12.7 m away and closes over 2 s (`huntcam/06`-`12`). But the dive ended at 4.9 m above her and no grab followed (`HC_COUNT_DIVE close_s=2.01 ... dive ended`).
- **Lantern recolour:** the grave-ash light still turns his wings purple in the fight frames (`kill/17_hit_1`, `18_hit_2`).
- **Fight:** the hit frames show a pillar and a wing edge (`kill/17`, `18`). The three strikes are not a readable confrontation.

## 3. Hiding

- **Dread-held siege (60 ± 10), 6 hidden hunts in one hide (Count's wardrobe):**
  - Near-miss on 6 of 6 hunts. After the first open, `HC_COUNT_LEARNED hide=Count's wardrobe`, and every later attempt is `chance=1.00 reason=learned hide`.
  - **2 opens** (`HC_COUNT_NEARMISS open=True`), both ending in a hide-open grab (driver-blocked).
  - Survived 4 of 6.
  - `HC_DOOR_TRY` ×7 and **`HC_DOOR_HELD` ×1** (attempt 4). Pass.
- **Classic siege (Dread 82-100):** opens on bag 2 of 3, 1 grab, Recovery cut once.
- **Fear:**
  - The no-tell hunt hidden in the far hide was survived (closest 3.38 m).
  - Witnessed hide: `HC_COUNT_WITNESSED`, then an entrance grab. The watched hide is punished. Pass.
- **Hidden survival, all hidden hunts:** escape 1/1, longnight 1/1, fear 1/2, siege 2/3 (the third hunt ended in an open), siege_held 4/6. Total **9/13 = 69 %**. Pass (≤ 85 %).
- **No commit-and-release loop:** every opened grab is a single commit followed by `grab blocked reason=driver invulnerability` and a leave. No re-commit was seen.
- **Weakness: the open is a bag of three with one open.** Logs show `bag=1/3 openAt=1 open=True`, then 2/3 and 3/3 closed, then a new bag opening at 1 again. After he opens your door, the next two near-misses in that bag are guaranteed rattles. A player who notices learns a safe window.
- **Hidden beats by phase** (`HC_HIDE kind= phase=`, all runs): Disturbance 11, Hunt 7, Warning 1.
  - In siege_held: Hunt 6 beats over about 283 s (2.1 per 100 s) against calm 12 over about 735 s (1.6 per 100 s).
  - About equal. Pass.
- **Hide spots answer E:** cold run `hidesAnswered=5/5 hidden=5`. All 8 audited spots focusable (`HC_HIDE_SPOTS_SUMMARY audited=8 focusable=8 failed=[]`).

## 4. Visuals

- **Great hall S1 vs `refs/great_hall.png`: 4/10** (`cap_rooms/S1_great_hall.png`, `cap_lantern/S1_great_hall.png`).
  - What reads: a rib-vaulted ceiling, galleries with balustrades, a rose window with both moons, many candelabra, a rune ring.
  - Clustered columns: the piers are square blocks.
  - Lancet windows: none in the hall. Two small square windows sit at the far end.
  - The banquet table is out of shot. The giant skeleton is a tan quadruped that blocks the centre axis and reads like a museum dinosaur, not a draped corpse.
  - There is no floor sheen and no mist.
- **Exterior S4 vs `refs/exterior_sky.png`: 4/10** (`cap_rooms/S4_exterior_sky.png`).
  - What reads: textured moons (white and red), aurora, stars, a lit keep, banners, a portcullis arch.
  - Towers are squat drums with cone caps. The moons are stretched ellipses at the 95° capture FOV.
  - The "rock base" is a few lumps. **The moat is a grey fog plane, not red and not reflective.**
  - `art_exterior_gate_out_S6` and `battlement_out` are a fog plane, a grey bridge and low-poly hill silhouettes. There is no far bank and no reflection. The view out is the emptiest in the game: 2/10.
- **Chapel:** lancets with tracery and a candle-lit altar, good. The stained glass is a coarse pixel mosaic of flat blue, red and yellow squares (`cap_artL_layout/art_layout_chapel_aisle.png`).
- **Crypt:** the hand-roots are the best set dressing in the game (`cap_lantern/S3_crypt.png`).
- **Props:**
  - Table, books and plates read (`art_props_hall_table_books.png`).
  - The giant skull from the east is a dark low-poly lump (`art_props_hall_skull_close.png`, `hall_skull_from_east.png`).
  - The dining-hall ceiling beam is a stretched, smeared texture (`cap_artL_kit/art_kit_kit_window_inside.png`, top third).
- **Title vs mock: 8/10.** A near match, with fog added. The Bellwether card thumbnail is almost black (`cap_title/S7_title_castle_1920x1080.png`).
- **Darkness and lantern:** the castle is lantern-led in rooms. Measured radial luminance, centre to edge (0-255):
  - S2 chapel, lantern on: 64 → 9. Lantern off: 80 % of pixels below 20.
  - Crypt, lantern on: 64 → 15.
  - Hand-held: `natural1/05_opened.png` 118 → 12 (hard circular pool).
  - **Flat-lit exceptions:**
    - The courtyard face of the hall great doors is as bright with the lantern off as on (`cap_art_doors/art_doors_hall_doors_closed.png` against the `cap_artL_` version).
    - The gatehouse door at spawn is evenly lit (`escape/01`).
    - The hall-entrance corridor in the cold run is only 17 % near-black, edge luminance 32 (`cold/06_cold_walk_t64.png`).
    - The Count is fully and evenly lit in hall grabs (`natural1/12`), and his wings stay saturated red in the dark while his body is black (`siege_held/08`).
  - Flame quads near the camera look fine. No artifacts beyond those listed.
- **Doors:**
  - Visible when closed, from both sides, lantern on or off. Pass (`cap_art*_doors/*closed*`).
  - **Opening away from the player: unproven, so it fails.**
    - `Door.ChooseSwing` exists in code, but no castle run toggled a door, and the castle never logs `HC_DOOR_STATE`, which only logs under verify.
    - The capture sets `Open=true` directly. Its `hall_doors_open_inside` shows the near leaf swinging toward a hall-side viewer (`leafRot=205`, `cap_artL_doors/art_doors_hall_doors_open_inside.png`).
- **Document and ledger screen: readable.**
  - Ledger body text against the parchment measures **9.3:1** contrast (`escape/08`). The journal record text is 8.2:1.
  - The journal layout has defects (`escape/16_journal.png`):
    - The corner filigree covers the first letters of "INSTRUMENT RECORDS", "ANCHOR" and "PULSE".
    - The wax seal sits on "read again".
    - The PULSE strip runs off the page bottom.
    - Blood blots overlap the "Compare…" line.
    - The end card's "EVIDENCE" label sits under a blot (`kill/20_won_kill.png`).

## 5. Audio

- **Inventory** (48 files in `Resources/CastleAudio`, my own BS.1770 implementation):
  - 0 clipped samples. Peaks -5.0 to -16.3 dBFS.
  - SFX -28 to -35 LUFS. Voices all at -24.3 LUFS.
  - Loop wrap steps ≤ 0.0012 (wind 0.0012, wing 0.0010, forge 0.0008).
  - The new `castle_hide_*` clips are clean: breath ×2, whisper, clawstep at -28.3 LUFS; handle at -29.5 LUFS with a -5.0 peak; seams ≤ 0.0007.
  - `heartbeat_loop` carries 0.62 s of trailing silence (rhythmic, acceptable).
- **No music in castle play:** `HC_PLAY_CHANNELS` shows `huntMusic=False safeMusic=False houseMusic=False`.
- **Heartbeat is not a phase lamp:** no step at a no-tell start; max delta 0.091 over 6 s after the start.
- **The Count's voice, all 21 lines across 12 castle runs:**

| line | count | where it came from | verdict |
|---|---|---|---|
| `cc_hunt_found` | 4 (n1, n2 ×2, n3) | his head (`CountSpeak` → `Speak(id, head)`). For example n1: Count at (5.17, 4.35, 18.45), player at (9.0, 3.28, 16.5), logged dist 3.8 occluded. | correct |
| `cc_hunt_smell` | 8 (siege_held ×6, siege ×2) | his head at the wardrobe slats, dist 0.4 | correct |
| `cc_stay` | 1 (fear) | gatehouse landing, dist 2.0 | correct |
| **`cc_bargain`** | **7** (every run that records the ear-trumpet) | **the ear-trumpet evidence spot, 2.1 m from her** (`Investigator.cs:404 Cue("voice", World.Voice)` → `Speak("evp", pos)`), while the Count is perched 15-25 m away | **disembodied** |
| **`cc_listen`** | **1** (fear, `HC_LIE kind=phantom_listen at=(0.9,3.2,13.2)`) | 2 m behind and 1.5 m above the player (`CastleScares.PhantomListen`). The same placement exists in `Soundscape.RareLine`. | **disembodied** |

  - The hold rule is aggressive. `cc_hunt_run` was held every time (`beyond 6 m for a hunt line`, `one line per hunt`), so natural runs carry only 1-2 Count lines each.
  - `HC_WATCHED kind=ear_whisper` (castle_hide_whisper, side left or right of the head) fired 6 times in natural and cold runs. If that whisper is meant to be him, it is also disembodied.
- **Footsteps:** step clips are clean (no clipping, -32 to -35 LUFS). `HC_WATCHED kind=footsteps_behind` and `HC_LIE footsteps_behind` fire 1-2 per run.

## 6. Bellwether `-hc-verify` and leakage

- `passed=True`, scope "full investigation and retry".
- 37 193 frames at 1920×1080: p50 11.11, p95 11.50, p99 11.96 ms; 0 frames over 33 ms; 89 over 16 ms.
- `performance_pass=False`, reason `engine_cost_over_16ms=87 (max 30.5 ms)`.
- No `cc_*`, `HC_COUNT_*` or castle keys in the Bellwether log. No castle leakage.

## 7. Errors, soft-locks, frame time

- 0 exceptions and 0 `HC_PLAY_BLOCKER` across 24 launches. Every run reached `HC_PLAY_RESULT` or `ART DONE`.
- Frame times:
  - Great hall (fear, 1920×1080): p99 11.52 ms, worst 18.59 ms, 3 frames over 16.7.
  - S1 at 2416×1292: max 11.32 ms.
  - Crypt settled: p99 11.78 ms. After the shot: p99 12.02 ms (the post-screenshot frame is ignored).
  - Hunt dive: p95 11.15-11.37 ms.
- The chapel and exterior views were not separately timed.
- **Driver defect, not a game bug:** `natural` teleports every 20 s even during a grab presentation (natural1 grab #2).

## 8. Top three changes, each with a test

1. **Make the kill presentation hold together (single biggest gap).** In 3 natural runs, the Dread-held siege, fear and death, every grab must meet all of the following:
   - (a) The skull is on screen in the commit frame (`skull_px` inside 0..2560 × 0..1440 at t=0.01).
   - (b) `HC_PLAY_GRAB_CONTINUITY pass=True` on at least 80 % of grabs with 20 or more frames. Crawl steps stay at or below 0.5 m.
   - (c) `eyeToSkull ≥ 0.6` at every sample.
   - (d) No dispatch card or HUD pixels in any grabcam frame, Return grabs included.
   - (e) No world mesh between camera and skull: a linecast from eye to skull is clear at every sample, so no candelabrum and no giant skeleton.
   - (f) Across 12 or more grabs, modes 10, 11 and 12 each appear at least once, and drops are at least 30 % where the ceiling allows.
   - Also stop the natural driver from teleporting while `GrabCamActive`.
2. **The Count speaks only from his body.**
   - Route the ear-trumpet reply and the phantom or rare `cc_listen` through his head position, or cut them.
   - Log `from=` (head, or another source) on every `HC_COUNT_VOICE`.
   - Test: over natural ×3 plus fear plus siege_held, 100 % of `HC_COUNT_VOICE` lines are `from=head`, and each has a matching `HC_VOICE_GATE spoke`.
3. **Exterior and great hall to 6/10.**
   - Test for `gate_out_S6` and S4:
     - The moat pixels below the horizon average R/G ≥ 1.6 (red).
     - The white moon's reflection shows as a bright blob of 400 px² or more in the water.
     - A far bank or ridge silhouette is lit by at least one light source.
   - Test for S1: at least 3 pointed-arch windows 150 px or taller, clustered (non-square) pier sections, and the banquet table in frame.
   - A blind critic must score both S1 and S4 at 6/10 or higher.

Secondary:
- Raise visible apparitions to at least 4 per 8 minutes without re-chaining hunts, and let at least one perch relocation be observed per run (`observed=True` is 0 in every run).
- Randomise or unbag the near-miss open, so a 1-in-3 chance is independent per hunt.
- For a cold player, no no-tell hunt before the first record or page. The cold first hunt came at 87 s in Survey with 0 evidence.
- Fix the journal page layout overlaps.
- Light the courtyard face of the hall doors from the lantern rather than ambient light.
- Coarse stained glass, the dining-beam texture smear, the Bellwether title thumbnail.

## Not judged

- Player-initiated door swing direction in the castle: no run toggled a castle door.
- Chapel and exterior frame time (not separately measured).
- Mic toggle.
- Long Night beyond a won escape (no hidden-hunt stress).
- The gamble at the sarcophagus.
- Throws in natural play: `HC_THROW_SKIP none_in_range` once per run, so only the fear run threw (Plate, poltergeist 11).
- Audio by ear: the analysis is numeric only, and I cannot hear the whisper clip to tell whether it is meant to be his voice.

## What changed since r12 (read after scoring)

r12 scored 5.0 on e57a00d. Its single biggest gap was "unchain hunts from the apparition clock".

- **That gap is closed.**
  - Hunts within 10 s of an apparition went from 6 of 7 (r12) to 0 of 6.
  - The apparition metronome (r12 gaps 101-141 s) now has CV 0.57.
  - The fixed first hunt (69-74 s) now spreads 64-172 s.
  - Share and gap stay in band.
- **Hiding went from a guaranteed save to a real risk.**
  - r12: 9/9 survived, the near-miss never opened, door-held never fired.
  - r13: 69 % survival, 2 opens, door held, hide learned.
- **Art moved only from 3 and 3 to 4 and 4.** It still fails r12's own S1/S4 test (pointed arches, clustered columns, reflective moat).
- **The kill moved sideways.**
  - r12 asked for continuity and readability. The game now measures continuity itself, and the measure fails on every natural grab.
  - The purple recolour r12 flagged is still in the fight frames.
- **New this round:**
  - Onboarding: objective, nudges, watched cues.
  - Audio is clean and music-free.
  - The voice rule is broken by the EVP and phantom-listen paths.

The net gain is half a point.
