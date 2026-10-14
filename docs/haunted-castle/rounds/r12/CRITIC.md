# Round 12 blind playtest critic: the running build

Critic: Opus 5.5 high, blind. Build `Builds/macOS/Hollow Current.app` at commit e57a00d (binary 18:41:36, commit 18:41:25), Mac Studio (display 3440×1440).
Before scoring I read only `FEAR_PLAN.md`, `DESIGN.md`, `docs/research/phasmophobia-horror/README.md`, `refs/*`, `rounds/r10/title_mock.png`, `Resources/UI/CastleBackdrop.png`, and the driver/capture sources (for flags and log keys). I did not read HANDOFF, PLAYTEST, AUTO_RUN, the round handoffs or the integrator's r12 captures. I read `rounds/r11/CRITIC.md` only after fixing the score.

I made 14 launches, one at a time, each through `gpu_slot.sh capture --label hc-critic -- open -g -W -n -a …`. All were scripted (`HC_INPUT scripted=true devices_ignored=keyboard,mouse,gamepad`). There were 0 crashes, 0 timeouts and 0 SIGTERM/SIGKILL. No instance is left running and the slot table shows 0/2 (`critic/runs.txt`).

| Run | Flags | Secs | Result line |
|---|---|---|---|
| natural1 | `-hc-playtest natural` | 542 | won=True blockedDeaths=1649 blockers=0 |
| huntcam1 | `-hc-playtest huntcam` (natural dice, mortal) | 85 | dead=True |
| fear | `-hc-playtest fear` | 331 | won=True blockers=0 |
| natural2 | `-hc-playtest natural` | 527 | won=True blockedDeaths=986 blockers=0 |
| death | `-hc-playtest death` | 40 | deaths=1 penalty=True dread=58.4 |
| escape | `-hc-playtest escape` | 111 | won=True blockers=0 |
| kill | `-hc-playtest kill` | 110 | won=True slain=True |
| siege | `-hc-playtest siege` | 737 | hunts=6 nearMissBegin=2 nearMissOpen=0 recoveryCuts=1 |
| longnight | `-hc-playtest escape -hc-long-night` | 101 | won=True, `HC_NAIL_HIDES nailed=2` |
| verify | `-hc-verify` | 426 | `HC_NATIVE_ROUTE_PASS` |
| cap_rooms | `-hc-capture` | 61 | S1–S8, H01–H10 |
| cap_ext | `-hc-capture -hc-capture-art exterior` | 30 | 9 views |
| cap_props | `-hc-capture -hc-capture-art props` | 35 | 4 views + crypt frame times |
| cap_title | `-hc-capture -hc-capture-title` | 15 | castle + Bellwether title |

Outputs: `docs/haunted-castle/rounds/r12/critic/<run>/` (`player.log`, PNG frames, `timeline.txt` for the natural runs). The 590 MB of PNGs are kept out of git by `critic/.gitignore`; the committed JPEG sheets are listed at the end. The capture window is 2416×1292, not 2560×1440: the 3440×1440 display cannot hold a 2560×1440 window plus its title bar (`cap_rooms/player.log`: `requested 2560x1440 window 2416x1359 (largest window that fits)`). Playtest frames are 1920×1080. Pixel bars below are scaled (200 px at 2560 = 150 px at 1920).

**Score: 5.0 / 10** (bar 8 = a Phasmophobia-grade hunt loop in a hand-authored gothic castle).

| Axis | Score | One-line reason |
|---|---|---|
| Fear loop, natural play | 5.5 | Hunt share is down to 23–24 % and quiet gaps reach 168–183 s. But hunts ride the apparition clock: 6 of 7 non-Return hunts began within 5 s of an apparition, and apparitions arrive every 101–141 s. |
| Hunt readability and kill | 5.5 | The kill frames read: skull centred and claws ≥ 200 px in 5 of 7 +0.3 s frames. But he pops 0.5 m → 1.8–3.3 m at the kill, 8 of 9 grabs are the same low crawl, and the HUD stays up through the grab commit. |
| Hiding | 3.5 | 9 of 9 unwatched hidden hunts were survived, 6 of them at Dread 82–100. The near-miss never opened (0 of 2). Door-held never fired in any run. |
| Environment vs refs | 3 | It reads as a lit blockout: tiled brick boxes, hexagonal-prism candles, a toy exterior with clip-art moons. The hand-roots and the Count are the exceptions. |
| Title / journal / end cards | 7.5 | The title matches the mock. Dispatch text overflows its card. |
| Audio | 6.5 | Clean assets, no music in play, the heartbeat follows felt Dread. Not listened to by a human. |
| Stability / perf / Bellwether | 8 | 0 exceptions, 0 blockers across 14 runs. Great-hall p99 11.58 ms. Bellwether verify passes with no castle log keys. |

---

## 1. Fear loop in natural play

The natural route plays the opening (kit, letter, three records, four ledger pages by t≈13 s), then makes 8 minutes of 20 s stops at six spots with the torch on. It never hides and is invulnerable. Then it closes the ledger, seals the bell and crosses. The driver teleports between stops, so this is not human pathing. The times are case `Elapsed` from `HC_HUNT_START`/`HC_RULES`, interpolated by line for other events (`natural*/timeline.txt`).

**Hunt timeline**

| Run | Hunt t (s) | Preceded by | Tell | Dread / felt | Hunt s | Recovery / grace / governor | Grab |
|---|---|---|---|---|---|---|---|
| n1 | 70 (`:154`) | apparition 1 s earlier (`:153`), escalated | 1.5 s | 15.0 / 0.0 | 30.9 | 38.1 / 50.4 / 1.00 | none |
| n1 | 202 (`:285`) | apparition at 186 (`:275`), not escalated; warning at 190 | told 5.9 s | 37.6 / 23.8 | 48.8 | 12.5 / 132.2 / 1.49 | none |
| n1 | ~434 | apparition at 429 (`:465`) | **false warning** 9.1 s | 37.5 | – | – | – |
| n1 | 452 (`:479`) | false warning 18 s earlier | **no-tell** | 36.1 / 29.5 | 29.8 | 25.0 / 137.5 / 1.66 | mode 9 |
| n1 | 519 (`:562`) | seal | Return, no-tell | 29.4 / 58.1 | to the bridge | – | mode 9 on the bridge, `HC_DEATH beat_win=true … blocked=True` (`:615`): a mortal player dies |
| n2 | 74 (`:140`) | apparition (`:139`), escalated | 1.5 s | 15.3 / 8.2 | 36.6 | 11.5 / 69.4 / 1.00 | mode 9 |
| n2 | 195 (`:266`) | apparition (`:265`), escalated | 1.5 s | 32.1 / 18.3 | 33.4 | 36.4 / 53.0 / 1.00 | mode 9 |
| n2 | 296 (`:375`) | apparition (`:374`), escalated | 1.5 s | 48.9 / 32.5 | 39.7 | 36.1 / 186.0 / 2.57 | mode 9 |
| n2 | 504 (`:545`) | seal | Return, no-tell | 37.8 / 50.7 | to the bridge | – | dive at the far end (`TORCH_FAIL eta=1.00`), crossed |

**Hunt share** (Warning + Hunt + false warning + hostile Return over the run): natural1 126 / 519 s = **24.3 %** (28.0 % after the first threat). natural2 119 / 509 s = **23.4 %** (27.3 %). The game's own `huntShare` ends at 0.26 and 0.34.

**Longest quiet gap** (no Warning/Hunt/Return): natural1 **183 s** (251→434), natural2 **168 s** (336→504). Other gaps after the first hunt: n1 95, 183; n2 83, 68, 168 s.

**Dread no longer runs away.** It peaked at 52 (n1) and 76 (n2) in natural play, against r11's pin at 100.

**Can an attentive player predict a hunt?** Yes, from the apparitions.
- **Apparition → hunt.** Every unforced hunt except the Returns started from an apparition. In n1 #1 and n2 #1–#3 the apparition escalated into a 1.5 s warning and a hunt. n1 #2 came 4 s after a non-escalated apparition. n1 #3 came after the apparition at 429 → a false warning at 434 → a no-tell hunt at 452.
  - Counting each start: 5 of 7 non-Return hunts (huntcam's included) began ≤ 5 s after an apparition, and 7 of 7 within 25 s.
  - Of 8 natural apparitions, 6 were followed by a threat within 5 s. The 2 that were not fell inside a post-hunt grace (`HC_ESCALATE … reason=grace`).
- **Apparitions are a metronome.** n1: 69, 186, 312, 429 (gaps 117, 126, 117). n2: 73, 193, 294, 435 (gaps 120, 101, 141). So "he appears about every two minutes, and when he appears he usually hunts" is learnable in one session.
- **The first hunt is fixed.** It started at 70, 74 and 69 s in n1, n2 and huntcam. That is 61–66 s after the second record, each time by escalating the first apparition. r11's 91–131 s spread is gone.
- **The no-tell channel is clean.** `fear/player.log:165 HC_PLAY_CHANNELS no_tell … changedFrames=0 heartbeatMaxDelta=0.052 wingMaxDelta=0.000 voiceLines=1`. Nothing on the HUD, mixer or lamps changes in the first 6 s. The one voice line is the Count speaking after the hunt has begun. That is diegetic and acceptable.
- **No-tell now happens at low Dread.** n1 #3 at Dread 36 is a real fix: "no warning = safe" no longer holds.
- **`HC_TORCH_FAIL` still fires in the last second of every dive** (n1 2/2, n2 4/4, huntcam 1/1, death). It is a 100 % reliable "grab in 1 s" cue. It comes too late to save you, so it is minor.

**Scare layer between hunts** (unforced, n1 + n2 ≈ 17 min):
- Apparitions 8. Throws 3 (`HC_THROW castle=near_apparition`).
- Lies 4: footsteps behind ×2, false candle ×2. Rare line `cc_listen` once (`natural2:243`). Silences 8.
- Castle beats: book_drop 8, candle_gutter 7, knife_stick 5, bat_pass 5, bell_single 4 (budget 2 per run), balustrade_shape 1, window_silhouette 1, false_candle_tell 1.
- That is roughly one beat every 25–30 s. It is a proper two-tier layer, and `bell_single` is now rare.
- Caveat (siege): **12 minutes hidden in one wardrobe produced 1 scare event**. The layer goes silent when the player hides.

**Hostile Return.** It is real:
- n1: he grabbed her on the bridge, and the death-first rule logged the kill (`:615`).
- n2: a dive at the far end.
- fear: closest 5.4 m.
- escape and longnight: crossed clean.

**Gatehouse violation** (fear, forced Dread): he lands inside the gate arch and is fully visible (`fear:399`, `fear/24_gatehouse_visit.png`). It reads well.

## 2. Hunt readability and the kill from the player's eye

I captured a naturally triggered hunt and kill (`huntcam1`, `sheet_huntcam.jpg`): first hunt at t=69 from an escalated apparition, a dive from the great-hall rafters at 7.3 m, `HC_COUNT_DIVE close_s=1.41 from=7.29 to=1.01`, then a mode 8 drop grab.

Every frame was measured on the 1920 frames by eye, with the central-60 % box drawn (`sheet_natural2_grabs.jpg`, `sheet_death.jpg`).

| Grab | Commit frame (t≈0.01) | +0.3 s: skull in central 60 % | +0.3 s: claw ≥ 150 px @1920 |
|---|---|---|---|
| huntcam mode 8 (drop) | point-blank face, reads (`11`) | yes (`14_t0.36`) | yes (~300 px per hand) |
| death mode 9 (kill) | back of his cranium only (`06`) | yes (`10_t0.37`) | yes (~160 / 190 px) |
| n1 #3 mode 9 | **Count absent: head above frame** (`10`) | yes (`12`) | yes |
| n2 #1 mode 9 | **Count absent** (`10`) | **no**: the eyes sit in the top 4 % (`12_t0.31`) | **no**: no hand in frame |
| n2 #2 mode 9 | a red wing only (`14`) | yes (`16`) | borderline (~160 px) |
| n2 #3 mode 9 | **Count absent** (`18`) | yes, barely (y≈22 %) (`20`) | **no**: claws cut by the bottom edge |
| fear, witnessed hide (mode 8) | reads | yes, upside down above (`19`) | yes |

Pass rate at +0.3 s: 5 of 7. The engine's own `HC_COUNT_GRABCAM … claw_ok=True` is wrong for n2 #1 (it says `claw_px=629`; no hand is visible). Do not trust it as a gate.

- **The kill pops.** At the kill the presentation teleports him back to the stand-off distance:
  - huntcam: eyeToSkull 0.52 m at t0.01 → **3.32 m** at t0.16 (`HC_COUNT_GRAB_PRESENTATION … standOff=2.22`, `huntcam1:163`).
  - death: 0.47 m → **1.76 m** (`death:129`).
  - On screen, a point-blank face becomes a small figure hanging in the rafters (`huntcam1/11` → `13`).
- **Drop from above: 2 of 9 grabs** (huntcam, and the fear witnessed hide). The other 7 are the same mode 9 "came in low" crawl lunge. That is the "identical kill animation" anti-pattern the research names.
- **HUD during the grab.** Objective, control strip and the `[E] Leave hiding place` prompt stay on screen through the commit and +0.16 s frames (`huntcam1/13`, `fear/18`, `fear/19`). On a real death the HUD clears from the kill frame onward (`death/08_dead.png`, `huntcam1/14`). So it is not left on during the death itself.
- **Before the dive he is hard to read.** At 7.3 → 3.8 m in the great hall, he is a small dark-red shape among dozens of candle flames and floating books (`huntcam1/06–09`).
- No hovering, stuck or wall-clipping was seen in these runs (TrackHunt `wallCrossings=0`).
- One mesh artifact: dark triangular holes on his forearms and wings (`huntcam1/14`, `natural1/13`).

## 3. Hiding

- **Guaranteed save, in practice.** These were all hidden with the torch off and unwatched, and all were survived (9 hunts, 0 grabs):
  - escape: 1 hunt.
  - longnight: 1 hunt, with 2 hides nailed.
  - fear: 1 no-tell hunt.
  - siege: 6 hunts at Dread 82–100.
- **The near-miss never opened.** It fired 3 times (fear 1, siege 2), with 0 opens. At Dread < 80 the open chance is 0 (`fear:… open=False … reason=first hunt, below 80`). At ≥ 80 it is 0.25 after a 0.33 attempt roll, so about 8 % per hunt. Below Dread 80, an unwatched hide is 100 % safe.
- **Watched hide works.** In fear he saw her hide, did not open the door politely, and took her at the entrance (`HC_COUNT_WITNESSED … entrance grab`, mode 8). It reads (`fear/18–19`).
- **Door held:** `HC_DOOR_HELD` did not appear in any of the 14 runs. It is unproven.

## 4. Visuals against the refs and the bar

Evidence: `sheet_rooms_vs_refs.jpg`, `sheet_exterior.jpg`, `sheet_props.jpg`.

- **Great hall (S1)** vs `great_hall.png`:
  - It has the right ingredients: rune ring, fog, chandelier, the moons in the rose window, a balcony, the skeleton.
  - But it is built from square brick-tiled boxes. There are no ribbed vaults, clustered columns or lancet windows.
  - The candlesticks are faceted low-poly. The table is a dark box, and the skeleton is a row of tan ribs.
  - Against the ref: 3/10.
- **Chapel (S2):**
  - It is a brick box. The pews are slab boxes. The candles are hexagonal prisms with flat flame quads.
  - The altar is a black block with three cans for relic sockets. There is no broken stained-glass sunburst.
  - 2.5/10.
- **Crypt (S3):**
  - The hand-roots are now sculpted multi-finger hands on twisted roots and read at a glance. This is the best set-dressing in the build.
  - The sarcophagus is a red brick box with glyphs, and the blood channel is flat red strips.
  - 3.5/10.
- **Exterior (S4 and art views):** aurora, stars, two moons, a keep with lit windows, banners, a portcullis arch.
  - The moons are flat discs with ringed clip-art craters. The tower bases splay outward like skirts.
  - The moat is a flat red strip and there is no rock outcrop.
  - The whole thing reads as a toy castle, not the ref's gothic silhouette: 3/10.
  - `battlement_out` is an almost empty dark frame. `gate_out_S6` was meant to look out through the gate at the lowered leaf; it shows the inside of a closed wooden door.
- **Props:**
  - `hall_skull_from_east` frames a bare dead tree; no skull is visible.
  - `hall_table_books` shows plates and goblets but no readable books.
  - The floating books elsewhere are flat card-like slabs.
- **The Count (S5, S6):** the sheet's language reads: skull face, ember eyes, skin wings, flayed chest, coat remnants. At distance he is the strongest asset. He is lit purple under the grave-ash lantern, which hides the grey-white and blood palette in two of the kill sequences.
- **Title vs mock:** a near match in layout, type and key art (`cap_title/S7_title_castle_2416x1292.png`). The Bellwether thumbnail is very dark. 8/10.
- **Artifacts:**
  - **Dispatch text overflows the parchment card.** The third line runs over the torn edge (`crop_dispatch_overflow.jpg`, `natural1/16`, `kill/15`).
  - The ledger title overlaps its "BLOOD LEDGER" label and the frame ornament (`S8`).
  - Near the camera, flame quads become large flat orange teardrops (`natural1/12`, `S5`).
  - The instruments are modern props under gothic names: the "Hollow Compass" is an LED EMF meter and the "Vellum Ear-Trumpet" is a cassette recorder.
  - The known-blank wall frames were noted and not scored.

## 5. Audio

Inventory (`critic/audio_scan.txt`, numpy/soundfile over 44 `CastleAudio/*.wav`):
- No clipped sample. The highest peak is 0.530 (`voice_cc_kit`). DC offset is ≤ 0.0007.
- Loops: the wrap jump is ≤ 0.0012 on heartbeat, wing, wind, candle bed, crypt trickle and forge hum. That is below each file's p99.9 step, so there are no seams.
- Loudness (gated RMS): voices −24.7 to −26.1; SFX/beds −26.3 to −36.3.
- The voices are 22.05 kHz mono (an 11 kHz ceiling), against 48 kHz for everything else.
- `book_drop` starts at 0.398 on its first sample, which is a hard onset. Harmless for a drop.

From the logs:
- No music in castle play (`huntMusic=False|safeMusic=False|houseMusic=False` before and during the no-tell hunt).
- The heartbeat follows `FeltDread` (`Soundscape.CastleMix`), with `heartbeatMaxDelta=0.052` over the first 6 s of a no-tell hunt. It is not a phase lamp.

No human listening pass was done; see "Not judged".

## 6. Bellwether

`verify/player.log`: `HC_NATIVE_ROUTE_PASS` and "all three music states executed" (Bellwether keeps its music, as designed). There are 0 castle keys (`HC_RULES`, `HC_COUNT_*`, `HC_SCARE_CASTLE`, `HC_LIE`, `HC_CASTLE_MIX`) in the log, so I saw no leakage.

## 7. Errors, soft-locks, frame time

- 0 exceptions, 0 errors, 0 `HC_PLAY_BLOCKER` and 0 `HC_MISSING_ART` across 14 logs. No soft-lock.
- Great hall: `fear:108 HC_PLAY_FRAMES great_hall frames=897 … p99_ms=11.58 worst_ms=17.78 over_16.7=2` (1920×1080). Capture S1 at 2416×1292: max 11.36 ms.
- Crypt: `crypt_settled … p99=11.75ms max=16.29ms`. The 166 ms frame in `crypt_after_shot` is the frame after the screenshot (ignored).
- Both rooms pass ≤ 16.7 ms p99, but at 2416×1292, not 2560×1440 (display limit).

## 8. Score, top three, not judged

**5.0 / 10.** The fear-wave pacing works:
- 23–24 % threat share.
- Real 3-minute lulls and no Dread runaway.
- No-tell hunts at low Dread.
- False warnings, a lethal hostile Return, and a steady scare layer.
- A clean audio mix with no music.

Three things hold it at 5:
- The hunt start is chained to a metronomic apparition, so the scare that should be consequence-free has become the tell.
- An unwatched hide is a certain save below Dread 80.
- The castle still looks like a lit blockout next to its refs.

### Top three, highest leverage (each testable)

1. **Unchain hunts from the apparition clock (single biggest gap).** Apparitions must stay mostly consequence-free and arrive irregularly; hunts must start from the dice as often as from escalation. Pass, across 3 unforced `natural` runs:
   - ≤ 35 % of non-Return hunts start within 10 s after an `HC_SCARE visible=` line.
   - The coefficient of variation of apparition inter-arrival times is ≥ 0.5 (today about 0.12).
   - The first-hunt time spans ≥ 45 s across the 3 runs (today 69–74 s).
   - Hunt share stays 15–30 % and the longest gap stays ≥ 120 s.
2. **Break the hiding contract below Dread 80.** An unwatched hide must sometimes fail without the player having done anything visible wrong. Examples: he checks the hide he last found you near (within-match learning), the open chance scales from Dread 40, or door-held rolls actually fire. Pass:
   - In a siege-like run held at Dread 50–70 with ≥ 6 hidden hunts in one hide, at least 1 `HC_COUNT_NEARMISS open=True` or a grab.
   - Across all hidden hunts in a full critic set, survival ≤ 85 %.
   - `HC_DOOR_HELD` appears at least once in any run that hides ≥ 5 times at Dread ≥ 60.
3. **Fix the kill presentation, then spend the art budget on the great hall and the exterior silhouette.**
   - Kill pass, on every grab in a natural run (mode 8, 9, hide entrance):
     - The Count is visible in the commit frame.
     - eyeToSkull changes by ≤ 0.5 m between consecutive grab-cam samples (no stand-off pop).
     - At +0.3 s the skull is in the central 60 % and a claw is ≥ 200 px at 2560, measured from pixels, not from `claw_ok`.
     - HUD text is hidden from the commit frame.
     - Mode 8 is ≥ 30 % of grabs where the ceiling allows it.
   - Art pass:
     - S1 shows pointed-arch windows and clustered columns instead of tiled box walls.
     - S4 has vertical tower walls, textured (not clip-art) moons, a rock base and a reflective moat.
     - A blind critic rates S1 and S4 ≥ 6/10 against `refs/great_hall.png` and `refs/exterior_sky.png`.

### Secondary (ordered)

1. Dispatch card text overflows the parchment (3-line messages). Size the card to the text or cap it at 2 lines.
2. The hide layer goes silent: 1 scare event in 12 minutes hidden (siege). Give the hidden player beats too (breathing at the slats, a passing claw step, a lie).
3. `HC_TORCH_FAIL` precedes 100 % of dives. Make it occasional, or fire it at random hunt moments too.
4. The pre-dive Count is unreadable in the candle-dense great hall at 4–7 m. A silhouette or rim against the window light would help.
5. Purple grave-ash light recolours the Count during kills (`death/10`, `huntcam1/14`).
6. Forearm and wing texture holes (dark triangles).
7. Near-camera flame quads blow up into flat orange shapes.
8. Capture views: `gate_out_S6` still shows a closed door, `hall_skull_from_east` frames a tree, and `battlement_out` is blank.
9. The instrument meshes (EMF meter, cassette recorder) contradict their gothic names.
10. The ledger title overlaps its label and ornament.

### Not judged

- How it feels with real input and pathing: every run teleports and is invulnerable except death and huntcam.
- Human-ear audio: spatialisation, voice intelligibility, mix balance in headphones. Only file analysis and mixer logs were used.
- Motion quality between screenshots (no video), and the kill-run fight feel.
- 2560×1440 rendering and frame time (display-limited to 2416×1292).
- Long Night beyond one escape run, and the death penalty's moved relic beyond the log line.
- Multiple seeds: dice are unseeded, so 2 natural runs plus 1 huntcam is a small sample for rates.
- The blank wall frames (known).

## What changed since r11 (read after scoring)

r11 scored 4.5 with "restore the sawtooth", "frame the kill" and "art pass" as its top three.

- **Pacing: largely fixed.**
  - Hunt share fell from 32–37 % to 23–24 %.
  - The longest quiet gap went from < 120 s to 168–183 s.
  - Dread no longer pins at 100 (peaks 52 and 76).
  - No-tell hunts now occur at Dread 36; in r11 they occurred only at ≥ 87.
  - `bell_single` dropped from 9 in two runs to 4.
- **New regression in predictability.**
  - The first hunt moved from a 91–131 s spread to a fixed 69–74 s.
  - Most hunts now begin by escalating the roughly two-minute apparition, so the tell has moved rather than vanished.
- **Kill framing: improved.** He is now in frame at +0.3 s in 5 of 7 grabs (r11: absent at the grab, ceiling at the kill). The stand-off pop and the mode 9 monotony remain.
- **Hiding: unchanged.** Below Dread 80 it is still a guaranteed save, and door-held is still never seen.
- **Art: modest gains.**
  - The glove-card hand-roots became sculpted hands.
  - The exterior gained towers, aurora and lit windows.
  - The rooms are still blockout-grade.
  - The r11 seam click in the audio loops is gone.

### Committed evidence (JPEG, small)

`critic/sheet_rooms_vs_refs.jpg`, `sheet_exterior.jpg`, `sheet_props.jpg`, `sheet_huntcam.jpg`, `sheet_death.jpg`, `sheet_natural1_grab.jpg`, `sheet_natural1_phases.jpg`, `sheet_natural2_grabs.jpg`, `sheet_natural2_plus03.jpg`, `sheet_fear_a.jpg`, `sheet_fear_b.jpg`, `sheet_kill.jpg`, `crop_dispatch_overflow.jpg`, `audio_scan.txt`, `runs.txt`, `natural1/timeline.txt`, `natural2/timeline.txt`, every `*/player.log` and the capture JSONs.
