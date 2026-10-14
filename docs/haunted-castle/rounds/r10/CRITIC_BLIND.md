# Round 10 blind critic: captures and logs

Critic: Opus 5.5 high, blind. I used only the raw evidence: every PNG in `rounds/r10/captures/`, the `grab_*` proofs, the two title mocks, `refs/*.png`, `DESIGN.md`, `FEAR_PLAN.md`, and the `.playtest-*.log` files in the repo root. I did not read HANDOFF, PLAYTEST, AUTO_RUN or the round handoffs. I ran nothing that renders. My only measurements were pixel crops and statistics with PIL.

**Score: 4.5 / 10** (bar 8 = a Phasmophobia-grade hunt loop in a hand-authored gothic castle).

| Axis | Score | One-line reason |
|---|---|---|
| Castle environment vs refs | 3 | Graybox-grade geometry with a tiling brick texture. Hero props are boxes. Every ref's centrepiece is missing. |
| The Count (model) | 6.5 | The demon sheet is translated faithfully (S6). It reads well when it is close and lit. |
| The hunt as a hunt (H01–H10) | 3.5 | He hangs upright in Glide and in Dive. The "grab" happens 2.8 m away with his arms raised, using Mode 6 instead of the new clips. |
| Fear loop from the logs | 4.5 | The systems fire without forcing. But there is an audio tell at the start of every hunt, the voice lines run on a metronome, hunts take about 50 % of play time, and throws never fire on their own. |
| Title and journal UI | 7.5 | Close to the mock, with no seam (measured). Small deviations remain. |
| Stability | 6 | No exceptions in the current logs. Three driver blockers remain, and the Count crosses walls. |

---

## 1. Visual findings

Severity: S1 = breaks the bar, S2 = clearly visible, S3 = polish.

| # | Sev | Capture | Finding |
|---|---|---|---|
| V1 | S1 | `S3_crypt.png` vs `refs/crypt.png` | The ceiling hand-roots are tan **cubes with stick fingers**. Mean colour (125,81,46) is flat; crop 40–200 × 600–780. They read as Minecraft hands, not horror. The left one is about 200 px across, out of scale. The ref's coffin-niche walls and carved sarcophagus are missing: the sarcophagus is a brick block, and the blood channels are flat pink planes with no reflection. There is no fog. |
| V2 | S1 | `S1_great_hall.png` vs `refs/great_hall.png` | The hall's centrepiece, the draped giant skeleton, is reduced to a small rib fragment at the left (x 380–460). There are no banners, no lancet windows with the two moons (one small rose window instead), no carved columns, no fireplace and no rug. The table is barely visible. The floating books are untextured slabs with a red edge. The rune ring and light shafts are the only elements that match. |
| V3 | S1 | `S4_exterior_sky.png` vs `refs/exterior_sky.png` | The castle is a **trapezoid box with five cones**. There is no rock, no spire cluster, no mountains, no trees and no bats. The horizon is a black void. The moat is a flat red plane. Stars are bloated discs, some with visible square halos (around 230,490 and 1500,70). The pale moon is stretched: 257 × 285 px, from the FOV 95 pitch-up. |
| V4 | S2 | `S2_chapel_altar.png` vs `refs/chapel_altar.png` | The palette is inverted: warm amber where the ref is cold blue. The floating candles are 3–5× ref scale, with flames up to about 100 px. The broken stained-glass window, the visual hook of the ref, is missing; there is an empty dark niche instead. The altar is a plain box topped by white cylinders. The font is a flat red octagon (fill mean (31,10,12), std about 7–11), not water. The two left pews overlap at x 0–770 / y 520–700. |
| V5 | S2 | `S5_lord_perch.png` | The Count on the rose-window ledge works as a silhouette. But the rose window is crude (thick spokes, octagon rings), and the foreground candle flames lean about 45–60° toward him. The log confirms this is the lean tell: `HC_LEAN_CHECK ... tilted 51/57/53 deg` (`.playtest-natural.log`). At that angle the flames read as flying teardrops, not as fire bending in a draught. His legs run below the ledge lip to the bottom of the frame, so the "perched" read is lost. |
| V6 | S2 | `S6_lord_sheet.png` | The Count matches `count_demon_sheet.png`: skull face, split jaw, flayed chest, skin wings, coat remnants. But a **white debug capsule** (the 1.80 m ScaleReference, x 770–860) is in the shot, and nothing anchors his feet to the floor (no contact shadow). The gatehouse is a bare brick box with one cart and one winch. |
| V7 | S2 | `H09`, `H10` | One wing membrane tip is crumpled into a zig-zag with a red rim at x 180–400 / y 720–890. This looks like a skinning or weight artefact on the extended wing. Wall sconces at the left (x 0–350 / y 200–460) show candles and flames lying horizontal. |
| V8 | S2 | `grab_drop_pose.png`, `grab_drop_side.png` | GrabDrop is a strong frame. But a long rod-like limb extends off the right edge at x 1500–2000 / y 300–450, and `grab_proof_measurements.json` puts the left claw at x 1857–3084 in a 2560-wide frame, which is off-canvas. In the side strip, frames 3–5 are identical and the wing collapses to an edge-on stick with a spike trailing down-left. Possibly a stretched bone. |
| V9 | S3 | `S7_title.png` | This file is mislabelled: it shows gameplay HUD in the gatehouse, not the title. The dispatch card text overflows: "the gate keeper's letter" is clipped at the bottom edge of the parchment. |
| V10 | S3 | `S8_journal_ledger.png` | The left ornament frame line crosses the "L" of "Ledger: the steward" and the "B" of "BLOOD LEDGER" (x about 150). The small "BLOOD LEDGER" kicker at the top left is covered by the flourish and a blood stain. A feather sprig runs into "Light the ash". |
| V11 | S3 | `S1`, `H01`–`H10` | Floor candelabras carrying books float next to the balcony at mid-height (right of `H01`, x 1050–1200). Whether they stand on anything cannot be read. Intent (floaters) or error cannot be decided from stills. |
| V12 | — | all | Wall frames are blank. This is known and not scored. |

**Title vs mock** (`S7_title_castle_*`, `S7_title_bellwether_*` vs `title_mock*.png`): layout, Cinzel typography, card positions, the rule line and the footer all match. I scanned for straight-edge discontinuities and found no seam. On the right-hand art region at both resolutions, the strongest column and row discontinuities are UI rules, the card border at x 911 and the footer at y 1012, or features painted into the art, such as the house eave. Deviations:

- The unselected card is far darker than the mock. Mean luminance is 3.3 vs 13.0 for the Bellwether card on the castle screen, and 4.3 vs 15.6 for the castle card on the Bellwether screen, so the house and castle are almost unreadable.
- The ENTER key-chip has lost its visible box.
- The fog in the castle version is heavier than the mock and washes out the drawbridge.

The fog is static in a still, so its motion is not judged.

## 2. Fear loop from the natural runs

The driver in the "natural" runs **teleports** between six fixed waypoints. The six positions account for 233 of 244 `HC_COUNT_AI` samples in `.playtest-r2-natural.log`. The driver is also **invulnerable**: `blockedDeaths=1864`. So nothing below proves lethality or real-player feel.

`.playtest-r2-natural.log` is the only complete wave-H natural run. `.playtest-natural.log` (r3) stops mid-hunt 4 at line 448 with no `HC_PLAY_RESULT`. `.playtest-r1-natural.log` is a pre-wave-H build: every Warning there is 9.0 s, and FeltDread sits at 5–30 while Dread is 100.

### Hunt timeline: r2 natural (`.playtest-r2-natural.log`)

| # | t (s) | line | phase / tell | threatFrom | Dread | Felt | warn (s) | hunt (s) | recovery (s) | quiet gap before (s) |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 104 | 126 | Hunt, **no tell** | Disturbance | 18.8 | 22.3 | — | 43.8 | 17.3 | (first; evidence complete at t 8) |
| 2 | 194 | 217 | Hunt, **no tell** | Disturbance | 28.7 | 18.8 | — | 26.5 | 24.5 | 46 |
| 3 | 247 | 277 | told | Recovery | 30.5 | 22.1 | 12.7 | 25.6 | 25.2 | 26.5 |
| — | ≈284 | 328 | **false Warning** | Recovery | 67.4 | — | 6.6 | — | — | — |
| 4 | 296 | 333 | Hunt, **no tell** (9.6 s silence first) | Disturbance | 68.3 | 47.7 | — | 47.8 | 37.8 | 23 |
| 5 | 386 | 427 | told | Recovery | 74.1 | 76.0 | 8.9 | 31.4 | 29.4 | 42 |
| 6 | 454 | 495 | told (6.3 s silence first) | Recovery | 100 | 94.3 | 10.7 | 33.1 | 35.8 | 37 |
| 7 | 530 | 570 | told | Recovery | 100 | 99.2 | 9.7 | 27.5 | 16.2 | 43 |
| 8 | 586 | 629 | **hostile Return**, no tell | Containment | 93.9 | 74.4 | — | until bridge (crossed 598.5) | — | 28.5 |

### Hunt timeline: r3 natural, aborted (`.playtest-natural.log`)

| # | t | line | tell | threatFrom | Dread | Felt | warn | hunt | recovery | gap before |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 80 | 120 | no tell | Disturbance | 17.5 | 0.0 | — | 40.2 | 15.9 | first |
| 2 | 193 | 210 | told | Disturbance | 48.7 | 43.0 | 13.9 | 36.3 | 37.1 | 73 |
| 3 | 279 | 336 | told | Disturbance | 88.2 | 79.2 | 6.6 | 27.1 | 37.6 | 50 |
| 4 | 358 | 410 | told | Disturbance | 93.7 | 88.3 | 11.9 | 37.1 (log ends) | — | 52 |

### What is good

- Warning length is drawn per warning: 12.7, 6.6, 8.9, 10.7 and 9.7 s in r2, and 13.9, 6.6 and 11.9 s in r3.
- No-tell hunts happen without forcing: 3 of 7 regular hunts in r2, 1 of 4 in r3.
- A false Warning happened without forcing, and in r2 it rolled straight into a no-tell hunt about 12 s later (r2:328→333). That is a good bait-and-switch.
- First-hunt timing varies (t 80, 104; 13 in `.playtest-lowdread1.log`).
- FeltDread now tracks Dread with lie episodes (r2 hunt 7: felt 40.9 at Dread 100).
- The hostile Return happened unforced: `HC_HUNT_SPAWN hostileReturn=True` (r2:633), return swoop, closest approach 1.75 m on the bridge (r2:681).
- Lies fired unforced: 4 in r2 (false candle, footsteps behind, phantom listen).

### What lets a player predict hunts (each is in the raw log)

- **P1 (S1): an audio cue starts every hunt, including no-tell hunts.** `HC_COUNT_BEAT learned` plus `HC_COUNT_BEAT_AUDIO learned` fire 1–4 lines after every `HC_HUNT_START` from hunt 2 on, 7 of 7 in r2 (lines 221, 281, 337, 431, 499, 574, 647), including the no-tell hunts 2 and 4. In r1 the same happens on 7 of 7. A no-tell hunt that plays a signature sound on frame 1 is a told hunt.
- **P2 (S2): the voice lines run on a metronome.** `cc_hunt_hungry` plays at hunt-time **t=10.0** in 5 of 5 r2 hunts where it played (lines 146, 240, 357, 518, 594) and again at **t=20.0** (257, 371). A fixed 10 s cadence is learnable.
- **P3 (S2): hunts take half the play time, and Dread runs away.** Each hunt adds +15 (seen) and +20 ("grab survived"), so Dread climbs 18.8 → 100 by hunt 6 and pins there. In r2 the Count hunts 236 s of the 453 s between the first hunt start and the end of hunt 7: **52 % of the time**, or 63 % counting warnings. The quiet gaps are 23–46 s (median 37 s). A hunt every 40 s or so is a rhythm, not a surprise, and it starves the investigation. Caveat: the driver never rests in the gatehouse, the only Dread sink, so real-player duty may be lower. Even so, the game rewards a hunt with more hunts.
- **P4 (S3): silence comes before a threat.** Both r2 silences (`HC_SCARE_CASTLE silence`, lines 332 and 488) fired 0–1 lines before a hunt or warning. That is two of two, a small sample, but currently a 100 % precursor.
- **P5 (S3): torch failure means a grab.** `HC_TORCH_FAIL eta≈1.0` precedes every dive grab, 10 of 10 in r1. That is the intended last-second tell, but it is perfectly reliable.
- **P6 (S3): the Count's moves are deterministic.** He opens from one of four perches: (-3.22,4.25,20.51) in 4 of 7 r2 hunts, and (0,5.45,27) in 2. His dive endpoints repeat to the centimetre: (11.25–11.36, 1.18–1.54, 10.29–10.31) in 2 r2 grabs and 4 r1 grabs. For the same player spot he always arrives by the same route.

### Scare layer between hunts (r2)

There are 23 `HC_SCARE_CASTLE` beats in about 600 s, roughly one every 26 s. But 5 of them fire *during* hunts, where they are masked.

- **Apparitions: 1 per run** (`apparitions=1`, r2:558; r1:476). In r3, 5 of 6 apparition checks were blocked (`HC_SCARE_SKIP reason=rhythm canSurprise=False`, lines 98, 192, 323, 391, 402). The commit that fixes this (3134424) says it is not built.
- **Prop throws: 0** in all three natural runs (`HC_THROW=0` in the r1:699 and r2:683 summaries; `thrown=0`), even though `HC_THROWABLES director=38`.
- **Perch sightings:** 12 of 13 between-hunt perch moves were occluded (`HC_COUNT_PERCH moved ... occluded=True`), and `flight_observed=0`. The "seen perched" design was effectively invisible to this driver.
- **Watched-hide kill: 0** unforced (`HC_COUNT_WITNESSED=0`, r2 summary). Hostile Return: 1 (r2), no grab.

### What the forced "fear" runs had to supply

`.playtest-fear.log` and `.playtest-r2-fear.log` (`HC_PLAY_FORCED` lines 114, 122, 210, 296) forced:

- the first prop throw and the poltergeist burst (t 25.5 / 26.8);
- Dread jumps from 9.8 to 55 to arm the dice;
- a scripted queue for the false warning, the no-tell hunt and the told hunt (every forced hunt is exactly 38.0 s with a 9.0 s warning);
- Dread raised to 78 to trigger the lie beats (10 lies follow);
- the driver hiding in the Count's view, to get the only `HC_COUNT_WITNESSED` open (fear:222–240, then blocked by invulnerability);
- a 40 s gatehouse wait, to get the only safe-zone violation (`HC_COUNT_GATEHOUSE landed`, fear:338).

In r2-fear the witnessed hide did **not** fire (`HC_COUNT_WITNESSED=0`) despite the same forcing.

In short, throws, the poltergeist, the witnessed-hide open and the gatehouse violation were only seen when forced. The forced bridge run shows the hostile Return is lethal when timed: `grabsOnBridge=31`, closest 0.81 m (fear:400). The natural run never got that close.

## 3. Count behaviour (H01–H10, logs)

- **H01–H03 (Perch, clock 1–3):** identical frames. He is small (about 100 × 150 px at 7.1 m), dark, and half-hidden by a floating book and a candelabra. That is acceptable for a perch.
- **H04–H06 (Glide):** he stands upright on the rail, then **hangs vertically in mid-air** with drooping wings and arms down (H05, H06). There is no glide pitch, no wingbeat silhouette and no forward lean. It reads as a puppet on a wire.
- **H07 (Dive, mode 3):** still vertical, still about 6 m away, wings half up. There is no dive.
- **H08–H10 (Grab, mode 6):** he hovers 3.0–3.9 m up at a **2.80–3.48 m** distance, with his arms raised overhead and his wings spread wide. The design grab range is 1.05 m (`DESIGN.md`). The new GrabDrop/GrabCrawl clips (modes 8/9) exist and are logged in play (`HC_COUNT_GRAB mode=8`), but this capture shows the old Mode 6 wing-wrap, which never reaches the camera. From 7.08 m to 2.80 m over 10 s is about 0.43 m/s, against the 4.6 m/s design dive. As a hunt this reads as "a monster floats nearer, then poses", not approach, search, dive and grab.
- **Logs:** the Count crossed walls during hunts, `wallCrossings=2` (`.playtest-fear.log:199`) and `wallCrossings=1` (`.playtest-r1-fear.log`). That is clipping. With deaths blocked he parks 0.4 m from the player for 19 s and keeps "chasing" (`.playtest-r2-natural.log:298–316`). This is a driver artefact, but a "stuck at arm's length" state exists. Frame time: the hunt-dive capture has a 144 ms hitch (`captures/capture.json` line 21: mean 12.41, p95 11.15, max 144.14 ms).

## 4. Missing art, errors, exceptions

- `HC_MISSING_ART=0` in every summary. There are no `Exception`, `NullReference` or `Error` lines in any current log. `HC_TITLE_FONT cinzel=True`. `HC_CURSOR free` is logged only for scripted runs. Windowed 1920×1080 on a 3440×1440 display (`.playtest-verify.log`, `HC_NATIVE_ROUTE_PASS`, 64 PASS).
- `.playtest-r1-verify.log:73`: `HC_VERIFY FAIL System.Exception: Raycast focus Hall coat cupboard` (Bellwether). This is superseded by the passing `.playtest-verify.log`, but it was flaky once.
- Driver blockers in current logs:
  - `.playtest-kill.log:87` and `.playtest-r1-natural.log:85`: the compass cannot record ("INTERFERENCE · RECORDING SUSPENDED · BREAK LINE OF SIGHT").
  - `.playtest-longnight.log:110`: the bell did not start the warning (phase Hunt).
  - `.playtest-siege1.log:87`: the hiding place "Count's wardrobe" is **not focusable by the interaction ray**. The later fear run hid there, so it is intermittent or depends on the route.
- `.playtest-r1-natural.log:698`: the natural run ends `not_won`, blocked by the compass.

## 5. Score and top three

**4.5 / 10.** UI and the Count model are near the bar. The castle itself is not: it is a blockout with placeholder hero props. The hunt in pictures does not read as a hunt. The fear systems now fire on their own, but they leak the very tells they were built to remove, and they keep the player in a hunt about half the time.

### Top three, highest leverage (each is testable)

1. **Make the hunt read in pixels.** Recapture H01–H10 from the player's eye during a dice-started hunt.
   - In Glide and Dive, his body must pitch at least 35° toward the travel direction.
   - He must close from 7 m to ≤1.05 m in ≤2 s during Dive.
   - The final frame must use GrabDrop or GrabCrawl (mode 8/9), with one claw ≥200 px wide.
   - No frame may show him hanging vertical with his arms down while in Glide or Dive.
2. **Remove the hunt-start tells and the runaway.**
   - `HC_COUNT_BEAT_AUDIO` must not fire within 10 s of any `noTell=True` hunt start (today 7/7).
   - `cc_hunt_hungry` hunt-times must have a spread of at least 5 s over at least 5 hunts (today 10.0/20.0 every time).
   - In a 10-minute natural run, hunt plus warning time after the first hunt must be ≤30 % (today 52–63 %), and Dread must not stay at 100 for more than 60 s unless the player has spent ≥60 % of that time in the dark.
3. **An art pass on the four hero rooms against their refs.**
   - Crypt: organic hand-roots, coffin niches, a carved sarcophagus.
   - Great hall: the draped skeleton at ≥30 % of frame width, banners, tall windows with both moons.
   - Chapel: cold palette, stained glass, true-scale candles, a water-filled font.
   - Exterior: castle on a rock with a spire cluster, mountains and trees, no black void.
   - Pass condition: a blind A/B of S1–S4 against `refs/*.png`, where each room is rated ≥6/10 by a critic who sees only the pair.

### Secondary (ordered)

- Unforced throws are 0 in three natural runs. Make one throw per 3–5 minutes happen without forcing.
- Apparitions are starved (1 per run). The fix (3134424) is unbuilt, so build it and rerun the natural route to completion.
- Remove the S6 debug capsule from the capture set, fix the H09/H10 wing-tip crumple and the GrabDrop off-canvas rod limb, and add contact shadows under the Count.
- Bring the candle lean down to ≤25° on screen (today 51–57°), or the tell reads as a rendering bug.
- Fix `wallCrossings>0` during hunts. Fix the dispatch-card text overflow (`S7_title.png`) and the journal ornament overlaps (S8).
- Raise the unselected title card to about the mock's brightness (luminance ≈13).
- Fix the intermittent wardrobe focus, the compass interference blocker and the Long Night bell-in-Hunt blocker.

## 6. What I could not judge

- **Audio:** the mix, voice intelligibility, the heartbeat, the silence ducks, wing sounds and spatialisation. Only log lines exist.
- **Motion:** animation blending, wing flaps, glide arcs, flame flicker, fog motion and the title fog animation. I had stills only, at 1 s spacing.
- **Real player behaviour:** the natural driver teleports between six waypoints and is invulnerable. Hunt lethality, fairness, chase escapes around columns, hiding under galleries and crouch-walking are all unproven.
- **First-time pacing and onboarding** with human input, and feel at the controller or mouse.
- **Frame pacing during a full natural run.** I only saw capture-scene frame times (the 144 ms hitch).
- **Whether flame lean, perch sightings and silhouettes are noticeable** from a real player's view cone.
- **The unbuilt commit 3134424** (apparition starvation fix). The latest natural run (r3) was aborted mid-hunt 4.
- **The sword fight's feel** (the kill route only logs `slain=True`).
- **Bellwether regression in play.** Only `.playtest-verify.log` PASS was available; I did not run `bellwether_fuzz.py`.
- **Wall art** (frames are blank; excluded by instruction).
- **Windowed and cursor behaviour with real input** (logs only show the scripted `HC_CURSOR free`).
