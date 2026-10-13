# Haunted Castle: round 3 blind critique

**Critic:** opus-critic, working blind.

**What I read:** DESIGN.md, `rounds/r2/CRITIC.md`, `refs/{great_hall,chapel_altar,crypt,exterior_sky,lord_perch,lord_sheet}.png`, and the `rounds/r2/captures/*.png` and `rounds/r3/captures/*.png` frames.

**What I did not read:** HANDOFF, NOTES, capture.json, the handoffs or git history.

**Method:**
- All coordinates are full-resolution capture pixels (x, y) at 2560×1440.
- Luma is 0.299R + 0.587G + 0.114B.
- "Pure white" means all channels > 235. "Blob" means an 8-connected component. "Fill" is blob px ÷ bbox area.
- "Warm" means R > G+20, G > B and luma > 40. This is a different formula from r2's, so I recomputed the refs with it.
- "Orange flame" means R > 200 and B < 140.
- I inspected crops at native resolution, brightened ×1.6–×2.5 where noted.

## Global measurements

| Capture | mean luma r2 → r3 | % px luma<8 r2 → r3 | pure-white px r2 → r3 | white blobs ≥400 px (fill>0.5) | biggest white blob | warm % |
|---|---|---|---|---|---|---|
| S1 great hall | 52.3 → 43.7 | 0.0 → **14.2** | 29,715 → **196,161** | 22 (19) | **74,142** at x300–674, y0–338 | **20.8** |
| S2 chapel | 44.3 → 38.6 | 0.0 → **25.5** | 80,476 → **311,792** | 20 (17) | 57,183 at x137–399, y148–475 | 6.8 |
| S3 crypt | 31.0 → 12.9 | 0.0 → **46.0** | 0 → 0 | 0 | — | 1.2 |
| S4 exterior | 40.9 → 15.6 | 0.0 → **41.3** | 1,133 → 658 | 0 | 394 | 0.3 |
| S5 perch | 35.4 → 23.2 | 0.0 → 28.5 | 38,225 → **106,329** | 9 (6) | 49,837 at x1477–1804, y915–1158 | 5.0 |
| S6 sheet | 33.3 → 18.7 | 0.0 → 24.1 | 1,202 → 1,300 | 1 | 464 (a doorway leak) | 1.6 |
| H01–H10 | 38.4–33.6 → 15.9–24.0 | 0.0 → **44.9–61.4** | ≈18.6k → **98k–158k** | 19–30 per frame | 82,560 (H10) | **0.0–0.1** |
| refs (hall/chapel/crypt/ext/perch) | 33.4 / 25.7 / 17.6 / 37.7 / 30.1 | 16.4 / 22.1 / 29.7 / 16.6 / 18.9 | 1,600 / 728 / 359 / 121 / 3,537 | **0 in every ref** | ≤280 | 1.6 / 0.7 / 1.6 / 0.2 / 2.9 |

The milky-black problem is solved: the min luma is 0 and p1 is 0–2 in every 3D capture. Three new global faults came with it:

1. **The flame fix made the white cards 3–7× larger by area.** In S1, S2, S5 and every H frame the "flames" are opaque white quads. Each is now a large outer card plus an inner card, still RGB 255,255,255 with no orange, no alpha falloff and no lean. See the native crops: the S1 candelabra at x1780–2260, y0–420 and the S2 candle at x100–560, y120–640. Not one ref has a white blob ≥400 px.
2. **The black point overshot in S3, S4 and the hunt.** The share of pixels below 8 is 41–61%, against 16–30% in the refs. The S4 castle face (y300–1200) has a mean luma of 7.8, with 54% of it below 8. In the S1 ceiling (x800–1760, y0–240), 81% is below 8: the vault that r2 showed is now a black void.
3. **The same hall is lit two different ways.** S1 is 20.8% warm, an orange cast 13× the ref's 1.6%. Yet the H frames of the same hall are 0.0–0.1% warm and blue-black. One of them is in the wrong lighting state.

## 1. Round-2 pass checks and top-5 items

| # | Check | Result | Measurement |
|---|---|---|---|
| 1 | Hunt: the Count is visible in ≥7/10 frames | **PASS** | 10/10. H01–H03 show him perched behind a book at ≈x1260–1320, y≈700–850. H04–H10 follow him in flight. |
| 2 | Hunt: ≥3 distinct poses | **PASS** | 4 poses: perched (H01–H03), gliding with wings spread (H04–H06), reaching with claws forward (H07–H09), and seen from behind with soles to camera (H10). |
| 3 | Hunt: one airborne frame with wings ≥2× shoulder width | **PASS** | In H06 the wing tips span ≈x995–1540 (≈545 px) against shoulders of ≈x1240–1340 (≈100 px), ≈5.4×. The wings read as thin streaks, not membranes. |
| 4 | Hunt: screen height rises to ≥40% and never exceeds 70% | **PASS** | It rises from ≈7% (H01) to ≈46% (H09): head y≈699, claws y≈1364. The visible body ends at the waist, at y≈1055 (H09 column profile x1180–1320). |
| 5 | Hunt: ≥5 frame pairs with mean abs diff ≥2 | **PASS** | 9/9 pairs: 5.2, 2.8, 15.8, 21.1, 26.7, 18.1, 15.8, 13.0, 28.4. |
| 6 | The H01 fog billboard is gone | **PASS** | No blob at x827–1660, y747–1439. A moon-shaft cone (≈x1100–1500) replaces it. |
| 7 | No white card ≥400 px with fill >0.5 in S1, S2 or S5 | **FAIL, worse** | S1 has 19, S2 has 17, S5 has 6. The area is 3–7× r2's. |
| 8 | S2 has ≥30 lit candles (orange blobs) | **FAIL** | **1** orange blob of ≥20 px. ≈19 floating candles carry a white card. Total candles ≈22. |
| 9 | S5 flames lean toward the Count | **FAIL** | 0°. The card edges stay parallel to each candle axis. No smoke. |
| 10 | px<8 is 8–30% in S1, S2, S3 and S5 | **FAIL** (3/4) | S1 14.2 ✓, S2 25.5 ✓, S5 28.5 ✓, **S3 46.0** ✗ |
| 11 | Min luma ≤4 | **PASS** | 0 in all |
| 12 | Warm share ≥5% in S1, S2, S3 and S5 | **FAIL** (3/4) | S1 20.8 (overcooked), S2 6.8, S5 5.0, **S3 1.2** |
| 13 | No pure-white blob >5,000 px | **FAIL, worse** | S1 has 7, S2 has 13, S5 has 4. |
| 14 | S1 shows a fog veil in rows y1000–1300 (floor std ≤60% of r2) | **FAIL** | The std is 24.3 against r2's 20.0, so it rose. The row means are a flat 37–42, with no veil and no gradient. |
| 15a | S3 channel mean R ≥ 2×B | **PASS** | RGB 43,11,17 over x700–960, y800–1080 (R/B = 2.6). |
| 15b | S3 channel shows a specular highlight ≥200 | **FAIL** | The maximum luma on red pixels is 115. The surface is flat pink-red with faint ripples. |
| 16a | The S2 white altar slab is replaced | **PASS** | It is now a dark arched panel at ≈x1220–1360, y450–700. |
| 16b | The S2 lancet white quads are removed | **FAIL** | They are still at x≈790–843 and x≈1627–1772, y≈467–557. |
| 17 | S4 shows planks and a chain in the bottom 25% | **PASS** | Planks at x≈860–1700, y1160–1440. Chains on both sides. |
| 18 | S4 has ≥3 spires breaking the skyline between the moons | **FAIL** | There are 2 tower bodies and the frame top cuts off both roofs. 0 spire tips are in the sky. No keep. |
| 19 | S4 shows both moons unclipped | **FAIL** | The white moon's bbox is x719–1091, y**0**–185, with 328 moon px in row 0. The right tower hides ≈half the red moon (visible at x1718–1833, y46–239). |
| 20 | S4 has no white leak at x≈2015 | **PASS** | There is no white blob ≥400 px anywhere. |
| 21 | S1 skull faces the camera at front-left, clear of the columns | **FAIL** | The skull is now at the left end (x≈765–830, y≈835–950). It is a low-poly box seen side-on and half behind the column at x≈850–920. It does not read as a skull. |
| 22 | S1 skeleton spans ≥35% of the width | **FAIL** | x≈765–1510 = 745 px = **29%** (r2 was 33%) |
| 23 | S1 has ≥6 books | **PASS** | 7, at ≈(300–690, 300–450), (840–930, 280–350), (1020–1250, 120–550), (1270–1410, 320–450), (1490–1540, 410–490), (1610–1850, 280–430) and (1640–1750, 400–600). |
| 24 | No bones pass through the S1 table | **FAIL** | Rib tips pass through the table top and reappear beneath it at x≈1220–1260, y≈940–970. |
| 25 | S6 wing tips reach the knee line | **FAIL** (improved) | The lowest tip is at y≈850 and the knee at y≈890. Wing top to lowest tip is ≈66% of body height (r2 55%, sheet ≈83%). |
| 26 | S6 tree trunk removed | **PASS** | It is gone. A brown wooden crate now stands in the same spot (x≈830–1010, y≈780–990). |
| 27 | S1 floor shows reflections | **PASS** | Wet specular patches across y≈1150–1440 |
| 28 | S7 unselected tab contrast ≥4.5:1 | **FAIL, worse** | The tab (x≈140–620, y≈890–950) now has **no label at all**: a blank grey bar (luma std 15.5). |
| 29 | S8 rule sits outside the title bbox | **FAIL** | The rule at y≈378 still runs through the ascenders of "Ledger: the steward". |

**Tally: 14 PASS / 15 FAIL** (r2: 15/12, on a different list).

- The hunt cleared all six of its checks. That is the round's real win.
- The flame work made the single worst visual fault larger, not smaller.

## 2. Scores (0–10) with deltas vs round 2

| Shot | Comp/layout | Silhouette | Materials | Lighting | Surreal | Colour/atmos | Creature read | Pose/anim | Scale |
|---|---|---|---|---|---|---|---|---|---|
| S1 great hall | 5 (0) | 4 (0) | 5 (+1) | 3 (0) | 4 (0) | 3 (0) | — | — | — |
| S2 chapel | 3 (−1) | 3 (0) | 4 (0) | 3 (0) | 2 (0) | 3 (+1) | — | — | — |
| S3 crypt | 4 (0) | 3 (0) | 3 (0) | 3 (0) | 4 (0) | 4 (+2) | — | — | — |
| S4 exterior | 3 (+1) | 3 (+1) | 3 (0) | 2 (−1) | 4 (0) | 4 (0) | — | — | — |
| S5 perch | **1 (−5)** | **1 (−5)** | 4 (−1) | 3 (−1) | 1 (−1) | 3 (−1) | **0 (−6)** | 1 (−4) | — (unmeasurable) |
| S6 sheet | 5 (0) | 7 (+1) | 5 (0) | 5 (+1) | — | 5 (+1) | 7 (0) | 3 (0) | 7 (−1) |
| H01–H10 | 5 (+2) | 5 (+2) | 4 (0) | 3 (0) | 3 (0) | 3 (0) | 6 (+4) | 5 (+4) | 5 (+3) |
| S7 title | legibility 5 (−1) | identity 6 (0) | | | | | | | |
| S8 journal | legibility 7 (0) | identity 6 (0) | | | | | | | |

The S6 scale is ≈1.13×, against a 1.19× target. The Count spans y415–1075 (660 px) and the 1.8 m capsule spans y515–1038 (523 px). I depth-corrected with the horizon at y720, which assumes the camera is level because the verticals are plumb.

### Progress pairs (r3 vs r2)

- **S1: same.** Gains: the black point is restored, there are 7 books and the floor has wet specular. Losses: the flame cards grew 6.6× in white area, the ceiling is a black void, the orange cast runs to 20.8% warm, and the skeleton shrank to 29% and still pierces the table.
- **S2: worse.** The white altar slab is gone. In its place, 13 white cards larger than 5,000 px now cover the room, and there is 1 orange flame against the 30 required.
- **S3: better, narrowly.** The channel is red again. But the room fell to 46% of pixels below 8, there is no specular and no candle pools, and the fog is a flat card with a hard top edge at y≈620 across x≈380–1120.
- **S4: better, narrowly.** The drawbridge and chains are in frame. But the camera is pressed against the wall: a near-black slab (mean luma 7.8) fills ≈60% of the frame, one moon is cut by the frame top and the other by a tower.
- **S5: much worse.** A giant red book covers **16% of the frame** (red-dominant px in x0–1500, y0–1100), and with it the Count. His face shows in **0 px**; only wing scraps and an ear fragment show at x≈1380–1600, y≈640–1000. Three candelabras of white cards surround the gap.
- **S6: better.** The wings are longer (66%, up from 55%), the tree is gone and the warm/cool lighting is better. He still holds an A-pose with spread claws, and a crate now sits behind him.
- **Hunt: much better.** It is a real perch, glide, then reach sequence, with a tracking camera and 9/9 moving frame pairs. He is lit and readable at 46% of frame height. The faults:
  - The hall is crushed to 45–61% of pixels below 8, with white cards everywhere.
  - In H07–H09 he reads as a floating bust cut at the waist (y≈1055), with no coat skirt or trailing legs.
  - The sequence ends in H10 with his back and soles to camera (soles at x≈1060–1400, y≈900–1060). This is a facing snap, not a wing-wrap grab.

## 3. New regressions with coordinates

1. White flame cards grew: S1 x300–674, y0–338 (74k px), x0–279, y0–229 and x1813–2073, y147–306; S2 x137–399, y148–475, x870–1084, y336–515 and x2362–2492, y291–603; S5 x1477–1804, y915–1158; H10 82k px.
2. The S5 hero is occluded by a red book spanning ≈x0–1460, y0–1040.
3. The S1 ceiling is crushed: x800–1760, y0–240 has 81% of pixels below 8, a black void where the vault was.
4. The S4 wall is crushed: y300–1200 has a mean luma of 7.8. No lit windows, no footing.
5. The hunt hall's lighting state differs from S1: warm share is 0.0–0.1% against 20.8%.
6. S7 lost the unselected tab label entirely.
7. The S6 doorway shows a white-card leak from the hall at ≈x1590–1620, y≈450–540.
8. The S3 fog is a flat translucent card with a hard horizontal top edge (≈y620, x380–1120).

## 4. Per shot

### S1 great hall
**Biggest gap:** the flames are opaque white cards up to 74k px each. Fix the flame material: bind the flame texture and alpha, clamp HDR so the core is ≈255,200,120 and fades to transparent, and size each flame to ≤1.5× the candle diameter.
**Pass:** 0 pure-white blobs ≥400 px with fill >0.5, and ≥15 orange blobs (R>200, B<140, ≥20 px).

Secondary issues:
- Raise the ceiling from black void to dark vault: in x800–1760, y0–240, px<8 ≤40%.
- Pull the warm share down to 3–8%, with blue moonlight dominant as in the ref.
- Make the skull a real skull facing the camera, clear of the column. Bring the skeleton to ≥35% of the width and keep the ribs out of the table (x≈1220–1260).
- Add a ground fog veil: rows y1000–1300 std ≤12. Move the 230×430 px book off the rose window.

### S2 chapel altar
**Biggest gap:** 17 white cards and 1 orange flame. Use the same flame fix as S1, and raise the floating-candle count to ≥30, all lit.
**Pass:** ≥30 orange blobs and 0 white blobs ≥400 px.

Secondary issues:
- Remove or glaze the lancet white quads (x≈790–843 and 1627–1772, y≈467–557).
- Put the 3 relic sockets on the altar top.
- Add red liquid in the foreground font rim (x≈400–1240, y≈1290–1440).
- Add banners, cobwebs and moon shafts.

### S3 crypt
**Biggest gap:** the crypt is now too dark (46% of pixels below 8), with no candle pools (1.2% warm). Add ≥6 floor and niche candles with warm point lights and lift the fill.
**Pass:** px<8 is 18–32%, warm share ≥3%, and the channel has a specular highlight ≥200 luma.

Secondary issues:
- Replace the flat fog card's hard edge at y≈620 with a soft-falloff volume.
- Replace the 12-square purple grid with vertical rune strips.
- Add coffin niches, a bridge and a receding vault.
- Give the hands volume and give the roots taper.

### S4 exterior sky
**Biggest gap:** the camera is jammed against a black wall. Pull back ≥25 m, or widen and aim up, so that a keep with spires and both moons sit in open sky.
**Pass:** ≥3 spire tips are inside the sky at y≥40. Neither moon bbox touches row 0 or any geometry. The wall covers ≤45% of the frame. The wall's mean luma is ≥18, with ≥3 lit windows.

Secondary issues:
- Add a waterline footing and reflections in the moat.
- Add bats, dead trees and mountains.
- The moat is flat red with no reflections.

### S5 lord perch
**Biggest gap:** the Count is invisible behind a red book covering 16% of the frame. Remove or relocate the book and re-frame.
**Pass:** the full face, with both eyes, is visible. His bbox is ≥40% of frame height. No prop overlaps his head or torso bbox. The rose window, with both moons, sits behind his head.

Secondary issues:
- Flames are white cards with 0° lean. They need a ≥20° lean toward him.
- Crouch him on the coping with one knee up.
- Re-check the left claws against the rail, which can't be judged this round.
- Keep a second book from cutting across his head (x≈1450–1640, y≈700–940).

### S6 lord sheet
**Biggest gap:** the wings stop at mid-thigh (lowest tip y≈850, knee y≈890). Lengthen the membrane to ≈83% of body height.
**Pass:** lowest tip y ≥ knee line (≈890 at this framing).

Secondary issues:
- Replace the A-pose with an arms-at-sides idle.
- Remove the crate behind his right hand (x≈830–1010, y≈780–990).
- Fix the white-card leak through the doorway.
- Scale is ≈1.13×, against 1.19×. Verify it with a level camera.

### H01–H10 hunt dive
**Biggest gap:** the climax is unreadable. From H07 to H09 he is a bust cut at y≈1055, and in H10 he snaps to face away with his soles to camera. Finish the dive with a front-facing wing-wrap grab, with the coat skirt and legs visible trailing behind.
**Pass:** in the closest two frames he faces the camera (face visible). The silhouette extends ≥15% of his height below the waist. No frame shows his back while he is between the player and the hall.

Secondary issues:
- The hall is crushed to 45–61% of pixels below 8, and its lighting doesn't match S1. Use one hall lighting state, with px<8 at 12–30%.
- The white cards appear in every frame.
- The wings read as streaks edge-on. Bank the glide so the membrane faces the camera.
- No flame leans toward him: the candle rule is invisible.

### S7 title
**Biggest gap:** the unselected map tab has no label. Render "Bellwether" (or the other map) in dark ink on a parchment tab.
**Pass:** the label is present, with contrast ≥4.5:1.

Secondary issues:
- "EXIT TO DESKTOP" still touches its Q keycap. Add ≥12 px of gap.
- "FIELD INVESTIGATION OFFICE" is still grey on the moon.
- The moat has scanline banding at y≈1240–1360.

### S8 journal
**Biggest gap:** the rule still strikes the title's ascenders at y≈378. Move it ≥16 px below the baseline.
**Pass:** no rule pixel lies inside the title bbox.

Secondary issues:
- The corner filigree overlaps the "BLOOD LEDGER" kicker.
- The heading is duplicated.
- The entries are black print where blood ink is called for.
- The bottom ≈40% of the page is empty.

## Verdicts per piece

| Piece | r2 | r3 | Why |
|---|---|---|---|
| 1 Layout | APPROACHES | **APPROACHES** (=) | The drawbridge approach now exists. But S4 is crammed against the wall with no keep, the crypt still has no niches, bridge or vault, and the S5 framing is blocked. |
| 2 Art | APPROACHES | **FAILS** (↓) | The black-point fix overshot: 41–61% of pixels below 8 in S3, S4 and H, a void ceiling and an orange-cast S1. Pure white rose 3–7× in area. The lancet quads remain. |
| 3 Surreal props | FAILS | **FAILS** (=) | Books (7) and a red channel are in. But there are 0 real flames, 1 orange blob in S2, a 0° candle lean, a box skull at 29% of the width, and books that occlude the hero shot. |
| 4 Count Claude | FAILS | **APPROACHES** (↑) | The hunt passes all its numeric checks, the model is strong and the S6 wings improved. But the S5 hero shot shows 0 face px, and the hunt climax is a waist-cut bust followed by a back-facing snap. |
| 0 UI | APPROACHES | **APPROACHES** (=) | The identity holds. The unselected tab lost its label, and the S8 rule and Exit collision are unchanged. |

## Ranked top 5 for round 4

1. **Flame material (pieces 2 and 3).** The cards render opaque white. Fix the texture/alpha binding and HDR clamp, give the flames a warm core with falloff, size them to ≤1.5× the candle diameter, and lean them ≥20° toward the Count.
   **Pass:**
   - 0 pure-white blobs ≥400 px with fill >0.5 in S1, S2, S5 and H01–H10.
   - ≥30 orange blobs in S2.
   - Every S5 and H flame tip offset points at him.
2. **S5 hero shot (piece 4).** Clear the occluding book and re-frame on the rail, with the rose window and both moons behind him.
   **Pass:**
   - Full face visible.
   - His bbox is ≥40% of frame height.
   - No prop overlaps his head or torso.
   - Both moons are inside the window and unclipped.
3. **Exposure and one lighting state (piece 2).**
   **Pass:**
   - px<8 is 12–30% in every 3D capture, including H01–H10.
   - The S1 ceiling (x800–1760, y0–240) has ≤40% of pixels below 8.
   - The S4 wall mean luma is ≥18.
   - The S1 warm share is 3–8%, and the H-frame warm share is within ±50% of S1's.
   - The S3 channel has a ≥200-luma specular.
   - S1 rows y1000–1300 show a fog veil (std ≤12).
4. **S4 composition (pieces 1 and 2).** Pull back or widen to show a keep.
   **Pass:**
   - ≥3 spire tips in open sky.
   - Neither moon touches row 0 or any geometry.
   - The wall covers ≤45% of the frame.
   - Planks and chains still show in the bottom 25%.
5. **Hunt climax and hall polish (pieces 3 and 4).** End the dive in a front-facing wing-wrap with legs and coat visible. Make the S1 skull a real skull facing the camera, with the skeleton at ≥35% of the width and no ribs through the table. Bring the S6 wing tips to the knee, and remove the S2 lancet quads.
   **Pass:**
   - The closest two H frames show his face, and no H frame shows his back.
   - The S1 skull is visible and front-facing, the skeleton spans ≥35% of the width, and no ribs show below the table top.
   - The lowest S6 wing tip is at y≥890.
   - The S2 lancet quads are gone.

## Distance from shippable

**Overall:** about 3.5/10 against an 8/10 bar. That is no net gain on r2: the hunt and the Count gained, and the environments lost.

**What is close:** the Count model is the only asset near ship quality. The hunt now has the right structure: perch, glide, reach and a tracking camera.

**What is not:**
- Every environment shot still fails the first test a player would apply: the light sources are flat white rectangles. This round made them larger, not smaller.
- The tone curve swung from milky to crushed. The great hall and the hunt hall are not even lit consistently with each other.
- The surreal dressing is unconvincing: a box skull, books big enough to block the hero shot, and candles with no flame and no lean.
- The exterior still has no castle silhouette.

**Path to 8/10:** it needs at least two more rounds. They must fix the flame material, exposure and S4 framing first. Only then can composition and prop polish move the scores. Until the flame and exposure fixes land, every environment score is capped at 3–5.
