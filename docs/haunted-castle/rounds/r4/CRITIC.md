# Haunted Castle: round 4 blind critique

**Critic:** opus-critic, working blind.

**What I read:** DESIGN.md, `rounds/r3/CRITIC.md`, `refs/{great_hall,chapel_altar,crypt,exterior_sky,lord_perch,lord_sheet}.png`, and the `rounds/r3/captures/*.png` and `rounds/r4/captures/*.png` frames.

**What I did not read:** HANDOFF, NOTES, capture.json, the handoffs or git history.

**Method:** same as r3, so the numbers compare directly.
- All coordinates are full-resolution capture pixels (x, y) at 2560×1440.
- Luma is 0.299R + 0.587G + 0.114B.
- "Pure white" means all channels > 235. A "blob" is an 8-connected component.
- "Warm" means R > G+20, G > B and luma > 40.
- "Orange flame" means R > 200 and B < 140, as a blob of ≥20 px.
- Flame lean is measured against the candle's own axis, not the image vertical, because the low cameras tilt every vertical.
- I inspected crops at native resolution, brightened ×1.5–×2.2 where noted.

## Global measurements

| Capture | mean luma r3 → r4 | % px luma<8 r3 → r4 | pure-white px r3 → r4 | white blobs ≥400 px | warm % r3 → r4 | orange blobs |
|---|---|---|---|---|---|---|
| S1 great hall | 43.7 → 43.2 | 14.2 → **11.7** | 196,161 → **4** | **0** | 20.8 → **15.3** | 47 |
| S2 chapel | 38.6 → 22.8 | 25.5 → 23.4 | 311,792 → **96** | **0** | 6.8 → 7.9 | **15** |
| S3 crypt | 12.9 → 35.7 | 46.0 → **11.1** | 0 → 21 | 0 | 1.2 → 5.6 | 8 |
| S4 exterior | 15.6 → 29.5 | 41.3 → **8.8** | 658 → 620 (moon, stars) | 0 | 0.3 → 0.6 | 0 |
| S5 perch | 23.2 → 17.5 | 28.5 → **30.1** | 106,329 → **0** | **0** | 5.0 → 1.2 | 7 |
| S6 sheet | 18.7 → 16.9 | 24.1 → **32.0** | 1,300 → 0 | 0 | 1.6 → **0.1** | 0 |
| H01–H10 | 15.9–24.0 → 23.7–40.4 | 44.9–61.4 → 14.0–**32.8** | 98k–158k → **0** | **0** | 0.0–0.1 → 8.8–11.9 | 35–48 |

The hunt frames by number:
- px<8 per frame is 30.0, 30.0, 30.0, **32.8**, 26.8, 16.1, 14.0, 15.0, 19.3 and 18.4.
- The frame-pair mean abs diffs are 3.2, **1.8**, 16.0, 18.3, 24.3, 16.9, 15.2, 22.1 and 15.3.

**The headline:** the white flame cards are gone from every capture. That was r3's worst fault, and it was fixed completely. The flames are now orange teardrops with a soft halo. Two new faults came in:
- The flame meshes on standing candelabras render as **black teardrops**. They have no emissive material.
- S3 swapped its crush for a blue haze over the whole frame (frame mean RGB 40, 33, 39), and the haze bleaches the red channel to mauve.

## 1. Round-3 pass checks and top-5 items

| # | Check | Result | Measurement |
|---|---|---|---|
| 1 | 0 pure-white blobs ≥400 px with fill >0.5 (S1, S2, S5, H) | **PASS** | 0 blobs in all 18 captures. The largest white count in a 3D capture is S4's 620 px, which is the moon highlights and stars. |
| 2 | S2 has ≥30 orange flames | **FAIL** (was 1) | **15** blobs. About 14 floating candles are lit. About 8 floating candles are unlit, for example at (160,460), (200,20), (810,690), (1270,490), (1320,430) and (1710,380). All 6 candelabra candles at x≈940–1020 and x≈1540–1620, y≈690–760 are unlit. |
| 3 | S5 and H flames lean ≥20° toward the Count | **FAIL** | S5, left candelabra (x≈300–560, y≈340–520): the flame axes run 25–32° off the image vertical, but their candle sticks run 28–35°. The lean relative to the candle is about −9° to −2°. S5, right candelabra (x≈2100–2410, y≈440–600): the lean relative to the candle is 0–13°. In H06–H10 every visible flame is parallel to its candle, for example the H09 candelabra at x≈1110–1240, y≈790–830. The candle rule is not visible anywhere. |
| 4 | S5 full face visible | **PASS** | Both eyes, nose and mouth are clear at x≈1235–1305, y≈690–775. He is lit and front-facing. |
| 5 | S5 Count bbox ≥40% of frame height | **FAIL** | From the wing tops (y≈575) to the claw over the rail (y≈1090) is 515 px = **35.8%**. His head-to-rail height is 400 px (27.8%). His face is 80 px (5.6%). In the ref he fills ≈85% of the frame. |
| 6 | S5 no prop overlaps his head or torso | **PASS** | The red book is at x≈1960–2195, y≈690–850, clear of him. |
| 7 | S5 rose window with both moons behind his head | **FAIL** | A plain lancet arch is behind his head. The only moon is a sliver behind a column at x≈500–560, y≈455–495. |
| 8 | px<8 is 12–30% in every castle capture | **FAIL** (9/16) | Passing: S2 23.4, H02 29.98, H03 30.0, H05 26.8, H06 16.1, H07 14.0, H08 15.0, H09 19.3, H10 18.4. Failing: **S1 11.7, S3 11.1, S4 8.8, S5 30.1, S6 32.0, H01 30.03, H04 32.8**. |
| 9 | S1 ceiling (x800–1760, y0–240) has ≤40% px<8 | **FAIL, worse** | **83.6%** (r3: 80.9%). The mean luma is 6.1. It is still a black void. |
| 10a | S4 wall mean luma ≥18 | **PASS** | 21.5 over y360–1380 (r3: 7.8) |
| 10b | S4 has ≥3 lit windows | **FAIL** | 0 warm blobs ≥30 px on the wall |
| 11 | S1 warm share 3–8% | **FAIL** (improved) | 15.3% (r3: 20.8%) |
| 12 | H-frame warm share within ±50% of S1's | **PASS** | 8.8–11.9%, inside the 7.7–23.0 window. One grade now covers S1 and the hunt. |
| 13 | S3 channel has a ≥200-luma specular | **FAIL** (close) | Max luma in the channel is **189**, at (810,858), RGB 255,171,111. 0 px are ≥200. |
| 14 | S1 fog veil, rows y1000–1300 std ≤12 | **FAIL** | std 22.0. The row means are a flat 60–68, with no ground veil. Three god-ray shafts were added instead (x≈1200–1900). |
| 15 | S4 has ≥3 spire tips in open sky (y≥40) | **FAIL** | The left cone tip is at y≈10 against the frame top. The tip at (1048,222) sits in front of the white moon. The tip at (1530,220) is in open sky. The tip at (1280,330) is a ≈20 px nub. That makes 2 tips in open sky. There is no keep. |
| 16 | Neither moon touches row 0 or geometry | **FAIL** | The white moon's bbox is x759–1142, y**0**–294, with **275 px in row 0**. The spire at x≈1000–1065 cuts its lower edge. The red moon passes (x1687–1858, y174–340, unclipped). |
| 17 | S4 wall covers ≤45% of the frame | **FAIL** | The wall runs full-width from y≈370 to y≈1380, which is **≈70%** (r3 ≈60–75%). The camera is still jammed against the gatehouse. |
| 18 | S4 has planks and chains in the bottom 25% | **PASS** | Planks at x≈920–1640, y≈1280–1440. Chains on both sides. |
| 19 | Hunt: the closest two frames show his face | **FAIL** | H09 (≈51% of frame height) has both hands clasped over his face. The claws pass *through* the face mesh at x≈1260–1340, y≈560–640, and the eyes are hidden. H10 (≈34%) shows his face, but he has receded. |
| 20 | Hunt: no frame shows his back or underside | **FAIL** | In H05 (x≈1120–1480, y≈640–900) he tumbles belly-up with his soles toward the camera, and a book hides his head. The r3 "soles to camera" snap moved from H10 to H05. |
| 21 | Hunt: no waist cut in any frame where he is ≥25% of the frame height | **FAIL** | In H08 (≈31%) the torso ends at y≈1040 (x≈1140–1330) with no coat skirt or legs, and the skeleton shows where his legs should be. H07 has the same cut at y≈900. H09 and H10 do show legs and feet (≥55% of his height below the waist). |
| 22 | Hunt ends in a front-facing wing-wrap grab | **FAIL** | There is no grab frame. His height goes ≈18% (H06), ≈15% (H07), ≈31% (H08), **≈51% (H09)**, then **≈34% (H10)**. The climax backs away. |
| 23 | S1 skull visible and front-facing, clear of the columns | **FAIL** (improved) | It now reads as a skull, with an eye socket and teeth (x≈380–620, y≈765–1010). But it is a crocodilian profile facing screen-left, its left end is cut by the pillar at x≈380–400, and the candelabra at x≈595–705 crosses its snout. |
| 24 | S1 skeleton spans ≥35% of the width | **PASS** | x≈390–1524 = 1,134 px = **44.3%** (r3: 29%) |
| 25 | S1 no ribs below the table top | **FAIL** | The ribs cross the table top and continue beneath it at x≈1170–1300, y≈890–990. The same happens in H09 at x≈600–1200, y≈1280–1400. |
| 26 | S6 lowest wing tip at or below the knee | **FAIL** (close) | The lowest tip is at y≈880 and the knee at ≈887 (head 415, feet 1070). The wings span 72% of body height (r3: 66%, sheet ≈83%). |
| 27 | S2 lancet white quads removed | **PASS** | Both lancets show a starfield. No white blob. |
| 28 | S3 channel mean R ≥ 2×B (passed in r3) | **FAIL, regression** | The pink channel pixels average RGB 81, 49, 67, so **R/B = 1.21** (r3: 2.6). The blue haze turned the blood mauve. |
| 29 | S3 warm share ≥3% | **PASS** | 5.6% (r3: 1.2%) |
| 30 | S7 both tab labels at ≥4.5:1 | **PASS** | "The Bellwether House" 8.5:1, "not in this build" 6.1:1, and "Haunted Castle" (white on oxblood) ≈10:1 |
| 31 | S7 EXIT/Q gap ≥12 px | **PASS** | The text ends at x≈887 and the keycap starts at x≈942, a 55 px gap. |
| 32 | S8 rule outside the title bbox | **PASS** | The rule is at y≈480. The descenders of "Ledger: the steward" end at y≈430. |
| 33 | Hunt: ≥5 moving frame pairs (diff ≥2) | **PASS** | 8/9. H02→H03 is 1.8, and H01–H03 are effectively one frame. |
| 34 | Hunt: height rises to ≥40% and never exceeds 70% | **PASS** | It peaks at ≈51% in H09 (head y≈540, feet y≈1270). |

**Tally: 14 PASS / 21 FAIL.** r3 was 14/15 on a shorter list.
- The single most important item, the white cards, passed outright.
- So did every UI check.
- Every environment-composition check and every hunt-climax check failed.

## 2. Scores (0–10) with deltas vs round 3

| Shot | Comp/layout | Silhouette | Materials | Lighting | Surreal | Colour/atmos | Creature read | Pose/anim | Scale |
|---|---|---|---|---|---|---|---|---|---|
| S1 great hall | 5 (0) | 5 (+1) | 5 (0) | 5 (+2) | 5 (+1) | 4 (+1) | — | — | — |
| S2 chapel | 4 (+1) | 3 (0) | 4 (0) | 5 (+2) | 4 (+2) | 5 (+2) | — | — | — |
| S3 crypt | 4 (0) | 3 (0) | 3 (0) | 4 (+1) | 4 (0) | 3 (−1) | — | — | — |
| S4 exterior | 3 (0) | 3 (0) | 3 (0) | 4 (+2) | 4 (0) | 4 (0) | — | — | — |
| S5 perch | 5 (+4) | 5 (+4) | 5 (+1) | 4 (+1) | 3 (+2) | 4 (+1) | 6 (+6) | 5 (+4) | 4 (new) |
| S6 sheet | 5 (0) | 7 (0) | 6 (+1) | 4 (−1) | — | 4 (−1) | 7 (0) | 6 (+3) | 7 (0) |
| H01–H10 | 5 (0) | 5 (0) | 5 (+1) | 5 (+2) | 4 (+1) | 5 (+2) | 6 (0) | 4 (−1) | 5 (0) |
| S7 title | legibility 7 (+2) | identity 6 (0) | | | | | | | |
| S8 journal | legibility 7 (0) | identity 6 (0) | | | | | | | |

The S6 scale is unchanged at ≈1.13× against a 1.19× target. The Count spans 655 px and the capsule 525 px, depth-corrected with the horizon at y720.

### Progress pairs (r4 vs r3)

- **S1: better.** The flames are real and number 47. The skeleton grew to 44% and now reads as a creature, and the god rays add depth. But the ceiling is still a void (83.6%), the frame is still orange-heavy (15.3% warm), the ribs still pierce the table, the skull is in profile, and the floor candelabras have black flames.
- **S2: much better.** The white cards are gone and the floating candles glow with halos. But only about half the candles are lit (15 against 30). There are no relic sockets on the altar, no blood in the font, and no banners or cobwebs.
- **S3: same.** The crush is gone (46% → 11% below 8), and the purple box grid became rune glyphs. But a blue haze now covers the whole frame, the blood reads mauve (R/B 1.21), both candelabras carry black unlit flames, and there are still no niches, bridge or vault.
- **S4: better, narrowly.** The wall is now readable (luma 21.5). The framing is unchanged: a brick slab covers ≈70% of the frame, the white moon is cut by the frame top and a spire, and no keep or lit window shows.
- **S5: much better.** The Count is visible, front-lit and crouched behind the rail with one claw over it. This is the round's biggest single gain. But he fills only 36% of the frame, no rose window or moons sit behind him, there is no candle lean, and a flat lavender bat card sits at the top-left.
- **S6: same.** The arms-at-sides idle and the clean set are gains, and the wings are longer (72%). But the lighting went cold and flat (warm 0.1%, 32% below 8).
- **Hunt: same.**
  - Gains: the lighting matches the hall, the white cards are gone, he is bigger, and H06/H10 show full-body front views.
  - Losses: the climax is still not a grab. H05 is an unreadable belly-up tumble, H07 and H08 are waist-cut busts, H09 has his claws through his own face, and H10 backs away.

## 3. New regressions with coordinates

1. **Black flame meshes** on every standing candelabra, which shows the emissive material is unbound on that prefab:
   - S1 at x≈595–705, y≈795–835 and x≈1860–1960, y≈820–880
   - S3 at x≈200–460, y≈540–600 and x≈2350–2450, y≈555–600
   - H08 at x≈1695–1810, y≈970–1050
   - H10 at x≈1780–1910, y≈1100–1160
2. **The S3 blood channel turned mauve** under the global blue haze: R/B fell from 2.6 to 1.21 over x660–1100, y760–1130.
3. **H09 clipping:** his hands and claws pass through his face at x≈1260–1340, y≈560–640.
4. **The H05 belly-up tumble:** his soles face the camera and a book hides his head (x≈1120–1480, y≈640–900). This is the r3 back-snap, moved earlier in the sequence.
5. **The H10 retreat:** he shrinks from 51% to 34% of the frame on the last beat.
6. **The S5 bat card:** a flat lavender cut-out with stair-stepped edges at x0–300, y0–280.
7. **S6 went cold:** the warm share fell from 1.6% to 0.1% and px<8 rose to 32%.
8. **H01–H03 are near-duplicates** (diffs 3.2 and 1.8), which wastes three of the ten frames on the same perch.

## 4. Per shot

### S1 great hall
**Biggest gap:** the vault is a black void, so the hall has no top.

Fix it with moonlight through the rose window, or a cool fill on the ribs and vault.

**Pass:** in x800–1760, y0–240, px<8 is ≤40% and the ribs or arches are distinguishable (local std ≥6).

Secondary issues:
- Fix the black candelabra flames at x≈595–705 and x≈1860–1960.
- Pull the warm share from 15.3% to 3–8%.
- Rotate the skull ≥45° toward the camera and clear of the candelabra, and lift the ribs out of the table (x≈1170–1300).
- Add a ground fog veil (rows y1000–1300 std ≤12), and move the book off the rose window (x≈1175–1275, y≈270–350).

### S2 chapel altar
**Biggest gap:** only 15 lit flames. Light every floating and candelabra candle, and add more to reach ≥30.

**Pass:** ≥30 orange blobs ≥20 px, and 0 candle tops without a flame.

Secondary issues:
- Put the three relic sockets on the altar top.
- Add red liquid in the foreground font (x≈400–1240, y≈1290–1440).
- Add banners, cobwebs and moon shafts.
- The altar is a featureless black box. Carve its front.

### S3 crypt
**Biggest gap:** a global blue haze washes the room and turns the blood mauve.

Confine the fog to the floor band, and give the channel a red, glossy surface.

**Pass:**
- Channel R ≥ 2×B.
- Specular ≥200.
- Fog only below y≈900 (rows y0–700 have no uniform veil, with wall local contrast ≥r3's).
- px<8 is 12–30%.

Secondary issues:
- Fix both candelabras' black flames.
- Add coffin niches, a bridge and a receding vault (still absent after three rounds).
- Give the hands volume and give the roots taper.
- Remove the purple smear on the ceiling at x≈1180–1340, y≈0–60.

### S4 exterior sky
**Biggest gap:** unchanged since r3. The camera is still pressed against the gatehouse wall, so ≈70% of the frame is brick. Pull back ≥25 m across the moat.

**Pass:**
- Wall ≤45% of the frame.
- A keep with ≥3 spire tips in open sky at y≥40.
- Neither moon touches row 0 or any geometry.
- ≥3 lit windows.
- Planks and chains still in the bottom 25%.

Secondary issues:
- Add a moat waterline with reflections.
- Add bats, dead trees and mountains.
- The white moon's crater decals read as concentric rings.

### S5 lord perch
**Biggest gap:** he is small in frame (36%) against a plain wall. Move the camera lower and closer so he dominates, with the rose window and both moons behind his head.

**Pass:** his bbox is ≥55% of frame height, and both moons are visible and unclipped within ±300 px of his head.

Secondary issues:
- The flames don't lean (0–13° relative to their candles, against ≥20° toward him).
- Remove the lavender bat card at the top-left.
- The wing roots sprout from behind the window rather than from his shoulders.
- Light is cold, at 1.2% warm. Add a warm rim from the candelabras.

### S6 lord sheet
**Biggest gap:** the wings stop at y≈880, against a knee at ≈887 and the sheet's 83%. Lengthen the membrane.

**Pass:** the lowest tip is ≥30 px below the knee line (y ≥ ≈920 at this framing), and wing span is ≥80% of body height.

Secondary issues:
- The lighting is flat and cold (0.1% warm, 32% below 8). Add a warm key or a doorway glow.
- Scale is still 1.13×, against 1.19×.

### H01–H10 hunt dive
**Biggest gap:** there is still no climax. The sequence peaks at H09, where his claws are through his own face, then backs away in H10.

Author a final grab: he closes to ≥45% of the frame, faces the camera with both eyes visible, and wraps his wings forward around the camera.

**Pass:**
- H09 and H10 each have Count height ≥45%, with both eyes visible and no hand or claw over the face.
- In H10 the wing tips enter from both frame edges.
- No frame shows soles or back.
- No waist cut in any frame ≥20% height.

Secondary issues:
- Replace the H05 tumble with a banked glide that shows the membrane faces.
- H01–H03 are one frame. Use them for an approach.
- Make the candle rule visible: flames lean ≥20° toward him in H06–H10.
- Fix the ribs through the table in H09.

### S7 title
**Biggest gap:** none critical. The blood splotches sit under "EXIT TO DESKTOP" and its Q keycap, and the filigree arrow overlaps the keycap's bottom-right corner (x≈990–1020, y≈1170–1180).

**Pass:** no ornament pixel inside any keycap bbox.

Secondary issue: the filigree crosses the "ORIGINAL INVESTIGATION" kicker at x≈150–180, y≈285–310.

### S8 journal
**Biggest gap:** the corner filigree overlaps the "BLOOD LEDGER" kicker and the "L" of the title (x≈190–260, y≈300–380).

**Pass:** no filigree pixel inside the kicker or title bbox.

Secondary issues:
- The heading is still duplicated ("Ledger: the steward" plus "BLOOD LEDGER / THE STEWARD").
- The body is black print, where blood ink is called for.
- The bottom ≈40% of the page is empty.

## Verdicts per piece

| Piece | r3 | r4 | Why |
|---|---|---|---|
| 1 Layout | APPROACHES | **APPROACHES** (=) | The S4 framing is unchanged at ≈70% wall with no keep. The crypt still has no niches, bridge or vault. S5 is now readable but under-framed. |
| 2 Art | FAILS | **APPROACHES** (↑) | The white cards are gone, there is one grade for the hall and the hunt, and the S3 and S4 crush is lifted. Still open: a void ceiling, an S3 haze, an orange S1, a cold S6, and blocky tiled-brick geometry far below the refs. |
| 3 Surreal props | FAILS | **FAILS** (=) | Real orange flames, a skeleton that reads, and rune glyphs are in. But the candle-bend rule, the map's signature tell, is not visible anywhere. Floor candelabras have black flames, half of S2 is unlit, the ribs pierce the table, and a flat bat card was added. |
| 4 Count Claude | APPROACHES | **APPROACHES** (=) | S5 now shows his face and S6 has a proper idle. The hunt climax is still broken: there is no grab, the H07 and H08 waist cuts remain, his claws pass through his face in H09, and he tumbles belly-up in H05. |
| 0 UI | APPROACHES | **APPROACHES** (↑, near MEETS) | All three r3 UI checks pass. Only ornament overlaps and the duplicate heading remain. |

## Ranked top 5 for round 5

1. **Hunt climax (piece 4).** Author a front-facing wing-wrap grab for H09–H10. Fix the hand-through-face clip, the H07–H08 waist cut and the H05 belly-up tumble.
   **Pass:**
   - H09 and H10 each have Count height ≥45%, with both eyes visible and nothing over the face.
   - The H10 wing tips enter from both frame edges.
   - No frame shows soles or back.
   - No waist cut in any frame ≥20% height.
2. **Flame completeness and the candle rule (piece 3).** Bind the emissive flame material on standing candelabras, light every S2 candle, and bend the flames toward the Count.
   **Pass:**
   - 0 black flame meshes in S1, S3 and H.
   - ≥30 orange blobs in S2.
   - In S5 and H06–H10, every flame axis is ≥20° off its candle axis, toward him.
3. **Exposure and atmosphere (piece 2).**
   **Pass:**
   - The S1 ceiling has ≤40% px<8.
   - The S1 warm share is 3–8%.
   - The S3 channel has R ≥ 2×B and a specular ≥200, with fog confined below y≈900.
   - px<8 is 12–30% in S1, S3, S4, S5, S6, H01 and H04.
   - S6 warm share is ≥2%.
4. **S4 composition (pieces 1 and 2).** Pull back across the moat to show a keep.
   **Pass:**
   - Wall ≤45% of the frame.
   - ≥3 spire tips in open sky.
   - Neither moon touches row 0 or any geometry.
   - ≥3 lit windows.
   - Planks and chains still visible.
5. **The S5 hero frame and hall polish (pieces 3 and 4).**
   **Pass:**
   - The S5 Count bbox is ≥55% of the frame, with both moons in a rose window behind his head.
   - The bat card is gone.
   - The S1 skull is turned ≥45° toward the camera, and no ribs show below the table top.
   - The S6 wing tip is at y≥920.

## Distance from shippable

**Overall:** about **4.5/10** against the 8/10 bar, up from 3.5. The gain is real: the worst visual fault in the build is gone, S5 finally shows the villain, and the hall and the hunt share one grade.

**What the remaining 3.5 points mostly consist of:**
- **Art fidelity (~50%).** Every environment is box geometry with one tiled brick texture. There are no gothic vaults, tracery, banners, cobwebs, coffin niches, ornament or debris. The refs are dense, worn and layered, and these rooms read as a greybox with good light. Until geometry and material density rise, no environment can score above 5–6.
- **Lighting and atmosphere (~25%).** Still open: the void ceilings, the S3 haze, the orange S1 and the cold S6. There is no localised fog or volumetric depth like the refs' floor mist.
- **Animation (~15%).** The hunt has the right beats, but it has no climax, and the waist cuts and the face clip are visible.
- **Composition (~10%).** S4 and S5 are framed wrong for their refs.

**Path to 8/10:** at least two or three more rounds.
- Round 5 must land the grab, the candle rule and the S4 reframe; those are cheap and testable.
- After that, the remaining distance is an art-density pass on the hall, chapel and crypt: modular gothic kit pieces, trim and decals, and set dressing. More tuning of numbers will not close it.
