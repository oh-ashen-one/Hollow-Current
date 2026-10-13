# Haunted Castle: round 5 blind critique

**Critic:** opus-critic, working blind.

**What I read:** DESIGN.md, `rounds/r4/CRITIC.md`, `refs/{great_hall,chapel_altar,crypt,exterior_sky,lord_perch,lord_sheet}.png`, and `rounds/r4/captures/*.png` and `rounds/r5/captures/*.png`.

**What I did not read:** HANDOFF, NOTES, capture.json, the handoffs or git history.

**Owner direction applied:**
- The Count is scored only on animation, behaviour and readability. His costume and face are not compared with lord_sheet.
- The skeleton's placement is not discussed.

**Method:** same as r4, so the numbers compare directly.
- All coordinates are full-resolution pixels (x, y) at 2560×1440.
- Luma is 0.299R + 0.587G + 0.114B.
- "Warm" means R > G+20, G > B and luma > 40.
- "Orange flame" means a blob of ≥20 px with R > 200 and B < 140.
- Tile period comes from the autocorrelation of high-passed luma along wall strips.
- Crops were inspected at native resolution or 2–3× nearest-neighbour, brightened ×1.5–2.5.

**S7 and S8 are unchanged.** Against r4 the mean abs diff is 0.0003 and 0.00003, so every r4 UI note still stands.

## Global measurements

| Capture | mean luma r4 → r5 | % px luma<8 r4 → r5 | warm % r4 → r5 | orange blobs r4 → r5 |
|---|---|---|---|---|
| S1 great hall | 43.2 → 42.8 | 11.7 → **7.3** | 15.3 → **11.2** | 47 → 62 |
| S2 chapel | 22.8 → 23.2 | 23.4 → **35.3** | 7.9 → 11.0 | 15 → **51** |
| S3 crypt | 35.7 → 31.4 | 11.1 → **4.2** | 5.6 → **16.2** | 8 → 36 |
| S4 exterior | 29.5 → 29.8 | 8.8 → 10.3 | 0.6 → 0.5 | 0 → 1 |
| S5 perch | 17.5 → 15.5 | 30.1 → **36.1** | 1.2 → **0.0** | 7 → **1** |
| S6 sheet | 16.9 → 16.8 | 32.0 → 33.5 | 0.1 → 0.1 | 0 → 0 |
| H01–H10 | 23.7–40.4 → 25.1–36.2 | 14.0–32.8 → **8.2–14.9** | 8.8–11.9 → 8.0–9.8 | 35–48 → 48–59 |

- **Hunt frame-pair diffs:** 3.3, 2.2, 15.9, 16.7, 20.7, 21.3, 16.1, 21.4 and 18.5. H01–H03 are still one frame.
- **White blobs:** 0 pure-white blobs ≥400 px anywhere except the S4 moon.

## 1. Round-4 checks and top-5 items

| # | Check | r5 | Measurement |
|---|---|---|---|
| 1 | 0 white blobs ≥400 px (S1, S2, S5, H) | **PASS** | 0 |
| 2 | S2 ≥30 orange flames | **PASS** (was FAIL) | 51 blobs. No unlit candle top is visible inside the frame. |
| 3 | Flames lean ≥20° toward the Count (S5, H06–H10) | **FAIL** | S5 now has **0 candles in frame** (1 orange blob). The H flames are still coaxial with their sticks, for example the H10 candelabra behind his head at x≈1415–1530, y≈440–560. The candle rule is still invisible. |
| 4 | S5 full face visible | **PASS** | Eyes, nose and mouth show at x≈1270–1340, y≈760–860. |
| 5 | S5 Count bbox ≥40% (r4) / ≥55% (r4 top-5) | **FAIL** | Wing-claw top y≈625 to finger tips y≈1190 is 565 px = **39.2%** (r4 35.8%). |
| 6 | S5 nothing over head or torso | **PASS** | Clear. |
| 7 | S5 rose window and both moons behind his head | **HALF** | A rose window now sits directly behind him (x≈900–1660, y≈350–1030), which is a real gain. It holds no moons: there is a teal disc and a red sliver about 20 px wide at ≈(1340,740), plus a cartwheel of 8 spokes with no glass. |
| 8 | px<8 is 12–30% in every castle capture | **FAIL** (5/16, was 9/16) | Pass: H01 14.1, H02 14.0, H03 14.1, H04 14.9, H05 13.9. Fail: **S1 7.3, S2 35.3, S3 4.2, S4 10.3, S5 36.1, S6 33.5, H06 9.5, H07 8.2, H08 8.2, H09 10.5, H10 11.9**. |
| 9 | S1 ceiling (x800–1760, y0–240) has ≤40% px<8 | **PASS** (was 83.6%) | **5.7%**, mean luma 6.1 → 16.9. One pointed transverse arch reads. The median local std over 60 px windows is 4.5, against ≥6 asked, so the vault surface itself is still nearly flat. |
| 10a | S4 wall mean luma ≥18 | **FAIL, regression** | 12.6 over x520–2060, y780–1160 (r4 21.5) |
| 10b | S4 ≥3 lit windows | **FAIL** (close) | 2 windows, at x≈1681–1724, y≈888–973 and x≈1845–1852, y≈959–977. |
| 11 | S1 warm 3–8% | **FAIL** (improved) | 11.2% |
| 12 | H warm within ±50% of S1 | **PASS** | 8.0–9.8% against a 5.6–16.8 window |
| 13 | S3 channel specular ≥200 | **PASS** | max 219, 47 px ≥200 |
| 14 | S1 fog veil, rows y1000–1300 std ≤12 | **FAIL** | 21.5 |
| 15 | S4 ≥3 spire tips in open sky | **PASS** | 5 tips: (940,427), (1133,540), (1280,480), (1420,533) and (1450,573). There is still no tall keep. All five are short cones on a single-storey slab. |
| 16 | No moon touches row 0 or geometry | **FAIL** | The white moon's bbox is x802–1154, y**0**–329, with **233 px in row 0** (r4 275). The red moon is clear. |
| 17 | S4 wall ≤45% of the frame | **PASS** (was ≈70%) | The castle bbox is x460–2100, y430–1190 = 33.8%. The camera finally pulled back. |
| 18 | S4 planks and chains in the bottom 25% | **FAIL, regression** | The chains hang at x≈1130 and ≈1415 and end in the moat, attached to nothing. Rows y1290–1440 are a featureless plane (luma 9.3, std 2.9), with no readable planks. |
| 19 | Hunt: the closest two frames show his face | **PASS** (was FAIL) | H09 and H10 both show his face with both red eyes, and nothing covers it (H10 face at x≈1250–1330, y≈520–620). |
| 20 | Hunt: no back or underside | **FAIL** | H05 (x≈1180–1450, y≈600–920) is shot from below: chin tipped up, face hidden, feet toward the camera. This is milder than the r4 belly-up tumble, but it is still an underside view. |
| 21 | No waist cut ≥25% | **PASS** (with note) | H07 and H08 now show the full coat. H08 has no legs or feet under the hem (x≈1150–1330, y≈1100–1250, where wall and skeleton bones show through). |
| 22 | Front-facing wing-wrap grab at the end | **FAIL** | H09 and H10 are an "arms up, claws high" pose. The body is plumb vertical and the legs hang limp with toes down, so he reads as hoisted on a wire. The wings are flat and swept back, with tips at x≈340 and ≈2260; neither enters from a frame edge. There is no wrap and no reach toward the camera. |
| 23 | S1 skull ≥45° toward the camera | **FAIL** (=) | Unchanged. It is still a profile facing screen-left at x≈430–620, y≈770–1000, and a candelabra crosses it at x≈595–705. |
| 24 | S1 skeleton ≥35% of width | **PASS** | unchanged |
| 25 | S1 no ribs below the table top | **FAIL** (=) | Rib tips go through the table top at y≈905 and end at x≈1140–1280, y≈920–995. |
| 26 | S6 lowest wing tip ≥ y920 | **FAIL** (=) | y≈885–890. S6 is the same image as r4 (mean diff 1.7). |
| 27 | S2 lancet white quads | **PASS** | |
| 28 | S3 channel R ≥ 2×B | **PASS** (was 1.21) | Mean RGB 79.5, 21.6, 23.4 gives **R/B 3.39**. |
| 29 | S3 warm ≥3% | **PASS** | 16.2%. That is now too warm (see S3). |
| 30–32 | UI checks | **PASS** | Unchanged images |
| 33 | Hunt ≥5 moving pairs | **PASS** | 9/9 ≥2, but H01→H03 are 3.3 and 2.2, so they are still duplicates. |
| 34 | Hunt height peaks ≥40% and never exceeds 70% | **FAIL** (marginal) | H09 is 52% (claw tips y≈520 to toes y≈1270). In H10, claw tips at y≈410 to the feet clipped at row 1439 make **≥71.5%**: he overshoots and is cut off at the feet. |
| r4-top5 | 0 black flame meshes in S1, S3, H | **PASS** | The S1 floor candelabras (x≈595–705 and x≈1860–1960), both S3 candelabras and the H08/H10 floor candelabras all burn orange. |
| r4-top5 | S3 fog only below y≈900 | **PASS** | The blue haze is gone. Rows 0–700 average RGB 37.8, 24.7, 20.5. |
| r4-top5 | S6 warm ≥2% | **FAIL** | 0.1% |
| r4-top5 | S5 bat card gone | **PASS** | |

**Tally: 21 PASS, 1 HALF, 18 FAIL.** (r4: 14 PASS, 21 FAIL)
- **Wins:** the flame binding, the S2 candle count, the crypt grade, the S1 ceiling exposure, the S4 pull-back and the hunt face read.
- **Still failing:** the candle rule, the grab, the S5 hero framing, the exposure band, and every S1 prop note.

## 2. Scores (0–10), delta vs r4

| Shot | Comp | Silhouette | Materials | Lighting | Surreal | Colour/atmos | Creature read | Anim/behaviour | Scale |
|---|---|---|---|---|---|---|---|---|---|
| S1 great hall | 5 (0) | 5 (0) | 5 (0) | 6 (+1) | 5 (0) | 5 (+1) | — | — | — |
| S2 chapel | 4 (0) | 4 (+1) | 4 (0) | 5 (0) | 5 (+1) | 5 (0) | — | — | — |
| S3 crypt | 4 (0) | 3 (0) | 3 (0) | 5 (+1) | 4 (0) | 5 (+2) | — | — | — |
| S4 exterior | 5 (+2) | 4 (+1) | **2 (−1)** | **3 (−1)** | 4 (0) | 4 (0) | — | — | — |
| S5 perch | 5 (0) | 5 (0) | **4 (−1)** | **3 (−1)** | **2 (−1)** | **3 (−1)** | 6 (0) | 5 (0) | 4 (0) |
| S6 sheet | 5 (0) | 7 (0) | 6 (0) | 4 (0) | — | 4 (0) | 7 (0) | 6 (0) | 7 (0) |
| H01–H10 | 5 (0) | 5 (0) | 5 (0) | 5 (0) | 4 (0) | 5 (0) | 7 (+1) | 5 (+1) | 6 (+1) |
| S7 / S8 | unchanged | | | | | | | | |

### Progress lines

- **S1: better, narrowly.**
  - Gains: the ceiling exists now (83.6% → 5.7% black), the candelabras are lit, and the warm share fell to 11.2%.
  - New square coursed piers give the arcade mass.
  - Losses: the old round columns still stand inside those piers (see section 3). The fog veil, skull and table ribs are untouched.
- **S2: better.**
  - Gains: 51 flames against 15. The lancets now have coursed arched surrounds.
  - Losses: the upper wall swapped to a near-black stone, which pushes px<8 to 35%.
  - Unchanged: the altar is still a black box with no sockets, there is no font blood, and there are no banners or cobwebs.
- **S3: better.**
  - Gains: the haze is gone, the blood is red (R/B 3.39), and the channel has a specular.
  - Losses: the room is now evenly lit and orange (4.2% px<8, 16.2% warm), with no dark depth. A crypt with no shadow is not a crypt.
  - Still missing: niches, bridge and vault.
- **S4: better on framing, worse on surface.**
  - Gains: the castle is finally a silhouette with 5 spires.
  - Losses:
    - It reads as one trapezoid slab dressed with mismatched texture swatches (section 3).
    - The wall luma dropped to 12.6.
    - The chains dangle into a flat, reflectionless moat (x0–2560, y≈1160–1290, RGB ≈77,9,19, std 6.2).
    - The white moon is still clipped by the frame top.
- **S5: same.**
  - Gain: the rose window behind him is the right idea.
  - Losses:
    - He is still 39% of frame height.
    - The frame lost every candle (warm 0.0%) and both moons.
    - 60% of the frame is a cold, blank wall of oversized blocks.
    - The right pier is a three-material splice.
- **S6: same.** It is effectively the identical image, and the architecture wave did not reach this room. The back wall still repeats every **388 px** (autocorrelation r=0.46).
- **Hunt: better.**
  - Gains:
    - The H09 claw-through-face clip is fixed.
    - H07 and H08 are full front views with the face lit and the claws forward. H08 is the best Count frame the build has produced.
    - The climax now grows (52% → ≥71%) instead of backing away.
  - Losses:
    - The final beat is a static arms-up puppet pose, not a grab.
    - H05 is an underside view.
    - H01–H03 are still duplicates.

## 3. The architecture wave

**Verdict: half the walls read as stone construction, the other half are now visibly patchwork. The wave added relief but did not integrate it.**

What landed:
- **Coursed piers with relief.** The S1, H and S3 piers and arcades have bevelled blocks, staggered courses and crack decals (for example the S1 pier at x≈2245–2520, y≈430–1190). They no longer read as one texture on a cube. Up close this is the best surface in the build.
- **Arches.**
  - The S2 lancets have voussoir surrounds (x≈720–930 and x≈1650–1830, y≈280–770).
  - The S3 arcade has a segmental arch springing from a coursed column (x≈0–660, y≈150–760).
  - The hall has a barrel vault with transverse arches, lit cool blue and readable in H03, H09 and H10 (x≈300–2300, y≈0–500).
- **Vault lighting:** **PASS.** S1's ceiling went from void to 5.7% px<8, and the H frames show the coursed vault surface.

What did not land, worst first:
1. **Doubled geometry: the old round columns were left inside the new square piers.**
   - S1 left: a cylinder at x≈260–400 pokes out of the square pier at x≈50–330, y≈440–1140.
   - S1 right: a cylinder at x≈2120–2290 pokes out of the pier at x≈2245–2520, y≈430–1180.
   - H03: two more lower-right, at ≈(1200–1330, 1090–1440) and ≈(1550–1700, 980–1440).
   - The two course grids do not align: the cylinder courses run ≈60 px apart, the pier courses ≈63 px, and the joints drift.
   - This is the clearest "module laid over the old box" tell in the build.
2. **Stone variants are whole-panel swaps with hard straight seams.** They do not read as variation within one wall.
   - **S2:** charcoal brick sits on beige ashlar along one straight line at y≈497 (x≈930–1630). The beige courses step ≈7 px across the niche, and a doubled mortar line runs at y≈525–528.
   - **S3:** a vertical seam at x≈810, y≈400–700 runs from beige ashlar to a dark wall with black glossy blotches. The same blotch grunge covers the ceiling tiles.
   - **S5 right pier:** green stone (x≈1700–1960), then an **untextured dark band** (x≈1960–2260, y≈240–880), then white ashlar starting on a horizontal seam at y≈880.
   - **S4:**
     - a glossy dark-blue panel (x≈850–1185, y≈930–1160)
     - a grey limestone block (x≈1360–1610)
     - a green-black block (x≈1665–1975)
     - a **flat black untextured panel** (x≈1880–2025, y≈785–955)
     - Together these read as texture-test swatches on a slab.
   - **Green "moss" stone appears as flat-tinted rectangles** in S1 (x≈1580–1700, y≈250–450), S2 (x≈1640–1850, y≈770–880 and x≈1630–1800, y≈40–260), S3 (x≈210–330, y≈270–570) and S5 (x≈2380–2560). It has no edge transition, so it reads as a material-ID error, not age.
3. **Untextured faces are probably collider or box faces showing.** These are the S5 dark band and the S4 black panel above, which have no mortar and no normal detail. I found no classic coplanar z-fight comb on the stone. However, **both S5 banners carry 1–2 px horizontal row striping**: x≈300–700 and x≈1900–2300, y≈640–1440, with row-to-row R swinging 5.3→8.4 on a mean of ≈7. That is either banner/wall z-fighting or an un-mipmapped weave, and it will crawl in motion.
4. **Floating or odd capitals.** Irregular white slabs sit on the S1 pier tops at x≈180–280, y≈432–470 and x≈2280–2380, y≈432–470. They read as melted snow, not as capitals.
5. **Repetition.**
   - The rebuilt rooms do not show a dominant tile period. The autocorrelation peak is ≤0.31 in S1, S2, S3 and S4, down from 0.38–0.52 in r4.
   - The un-rebuilt S6 gatehouse still repeats at 388 px (r=0.46).
   - The S5 wall blocks are about 150×60 px at this distance. Their scale is ≈2× the hall's, so the perch reads as a toy.
6. **Still boxes where it matters.**
   - **Vault:** no ribs cross the vault and there are no bosses, corbels, mouldings, tracery or string courses.
   - **Rose windows (S1, S5):** each is a cartwheel in a square box recess.
   - **S2 altar:** a plain black box.
   - **S3 ceiling:** flat tiles.
   - **S4 walls:** flat slabs with splayed sides.
   - Against the refs (pier clusters, tracery, ribbed vaults, carved altar and sarcophagus), the geometry gap closed perhaps 20%.

## 4. Regressions with coordinates

1. **S2 crush:** px<8 went from 23.4 to 35.3. The new charcoal upper wall is at x≈930–1630, y≈0–497, plus both side walls.
2. **S5 lighting and props:** 0 candles, warm 0.0%, px<8 36.1%, and no moons.
3. **S4 wall luma** fell from 21.5 to 12.6. The bridge planks are gone from the bottom band, and the chains end in the moat at x≈1130 and ≈1415, y≈1160–1290.
4. **S3 lost all shadow** (px<8 4.2%) and went orange (warm 16.2%).
5. **Doubled columns and pier interpenetration:** S1 x≈260–400 and x≈2120–2290; H03 lower right.
6. **Untextured panels:** S5 x≈1960–2260, y≈240–880; S4 x≈1880–2025, y≈785–955.
7. **Banner row-striping** in S5 (see section 3, item 3).
8. **H10 overshoot:** the feet are clipped at row 1439 and the frame height is ≥71.5%.
9. **H06 claw into the floating book** at x≈1350–1420, y≈770–830.

## 5. Per shot

### S1 great hall
**Biggest gap:** the legacy round columns stand inside the new square piers.

Delete the cylinders, or make them engaged shafts on the pier faces with matched coursing.

**Pass:** no cylinder intersects a pier in S1 or H01–H10, and mortar joints are continuous across each pier.

Secondary issues:
- The skull is still in profile. Rotate it ≥45°.
- The ribs pierce the table at x≈1140–1280, y≈920–995.
- Warm share is 11.2%, against 3–8%.
- Replace the snow-blob capitals, and add ribs or bosses to the vault (60 px local std ≥6).

### S2 chapel altar
**Biggest gap:** the altar is still a black box with no relic sockets. It is the forge and the room's focal point.

**Pass:** three readable recesses (slot, cross, round) on the altar top, and a carved front with local std ≥8 over x≈1060–1500, y≈780–960.

Secondary issues:
- px<8 is 35.3%. Light or lighten the charcoal upper wall, and blend the y≈497 seam over ≥1 course.
- Add blood in the font at x≈400–1240, y≈1290–1440.
- Add banners and cobwebs.
- The floating candles are oversized boxy sticks, up to ≈60 px wide.

### S3 crypt
**Biggest gap:** still no niches, bridge or receding vault, the same as three rounds ago. The room is a lit box around a basin.

**Pass:**
- ≥6 coffin niches on the side walls.
- A bridge over the channel.
- A vanishing vault with ≥3 visible arch rings.

Secondary issues:
- Restore shadow: px<8 12–30%, warm ≤8%.
- Remove the black-blotch grunge on the ceiling and the x≈810 seam.
- The hands still lack volume.

### S4 exterior
**Biggest gap:** the castle is a single trapezoid slab of mismatched swatches, with an untextured black panel at x≈1880–2025.

**Pass:**
- One stone family with per-block variation.
- 0 untextured faces.
- ≥2 height tiers, with a keep at least twice the wall height.
- Wall luma ≥18.

Secondary issues:
- The white moon still touches row 0 (233 px).
- There are only 2 lit windows.
- The moat is flat with no reflections.
- The chains are unanchored, with no planks in rows y≥1290.
- There is no horizon landscape: the frame ends in a flat infinite plane at y≈990.

### S5 lord perch
**Biggest gap:** the Count fills 39% of the frame, against a cold, empty wall with no candles and no moons.

**Pass:**
- Count bbox ≥55%.
- Both moons visible through the rose window within ±300 px of his head.
- ≥2 candelabras in frame.
- Warm share ≥2%, px<8 ≤30%.

Secondary issues:
- Fix the three-material pier splice and the untextured band at x≈1960–2260.
- Fix the banner row-striping.
- The rose window is a cartwheel with no glass. Add tracery and tinted glass.
- The block scale is ≈2× the hall's.

### S6 lord sheet
**Biggest gap:** unchanged. The wing tip is at y≈890, against ≥920 asked.

**Pass:** the lowest tip is at ≥920.

Secondary issues:
- Warm share is 0.1%.
- The room was not rebuilt and repeats at 388 px.

### H01–H10 hunt
**Biggest gap:** the climax is a static arms-up pose on a wire, not a grab.

Author a forward-leaning dive that ends in a wing-wrap: torso pitched ≥25° toward the camera, claws reaching toward the lens, wings forward.

**Pass:**
- In H10 both wing tips enter from the left and right frame edges (x<80 and x>2480).
- Both eyes are visible.
- His feet are not clipped, or he is cropped above the knees deliberately.
- Height is 45–70%.

Secondary issues:
- In H05 his face is hidden in an underside view. Bank him so the membrane and face read.
- H01–H03 are duplicates (diffs 3.3 and 2.2). Use them for the approach.
- The candle rule is still invisible (≥20° lean, H06–H10).
- His legs are missing under the coat in H08, and his claw goes into the book in H06.

### S7 and S8
Unchanged from r4. The ornament overlaps on the keycap and the kicker, the duplicate heading and the black ink all remain.

## Verdicts per piece

| Piece | r4 | r5 | Why |
|---|---|---|---|
| 1 Layout | APPROACHES | **APPROACHES** (=) | S4 is finally framed. The crypt is still missing niches, bridge and vault, and S5 is under-framed. |
| 2 Art / architecture | APPROACHES | **APPROACHES** (=, mixed) | The coursed piers, arches and lit vault are real gains. Against them: doubled columns, swatch-style variants with hard seams, untextured panels, S6 not rebuilt, and an exposure band that regressed (5/16). |
| 3 Surreal props | FAILS | **FAILS** (=) | Flames are fixed everywhere, which is the necessary step. But the candle rule, the map's signature tell, is still nowhere, and S5 lost its candles entirely. |
| 4 Count (anim, behaviour, readability) | APPROACHES | **APPROACHES** (↑) | The face reads in the closest frames, the waist cuts and the face clip are gone, and H08 is good. There is still no grab, H05 shows the underside, and H10 is a puppet pose. |
| 0 UI | APPROACHES | **APPROACHES** (=) | Untouched this round. |

## Ranked top 5 for round 6

1. **Integrate the architecture (pieces 1 and 2).** Remove the legacy columns inside the piers, texture or remove every untextured face, and replace whole-panel stone swaps with per-block variation inside one stone family. Rebuild S6 and the S4 front with the kit.
   **Pass:**
   - 0 pier/column interpenetrations in S1 and H.
   - 0 faces with no mortar or normal detail (local std ≥3) in S4 and S5.
   - No straight material seam longer than one module edge in S2, S3, S4 or S5.
   - S6 wall autocorrelation ≤0.30.
2. **The hunt grab (piece 4).** Forward-leaning wing-wrap in H09–H10, and a readable H05.
   **Pass:**
   - H10 wing tips at x<80 and x>2480.
   - Torso pitched toward the camera.
   - Eyes visible.
   - Height 45–70% with no feet clipped by the frame bottom.
   - In H05 the face is visible and the soles are not.
   - H01→H03 diffs ≥8.
3. **The candle rule and the S5 hero (pieces 3 and 4).**
   **Pass:**
   - S5 has ≥2 candelabras with every flame ≥20° off its candle axis toward him.
   - The same lean shows in H06–H10.
   - S5 Count ≥55%, with both moons in the rose window.
   - S5 warm ≥2%.
4. **The exposure band and the S4 surface (piece 2).**
   **Pass:**
   - px<8 is 12–30% in S1, S2, S3, S5, S6 and H06–H10.
   - S3 warm ≤8%; S1 warm 3–8%.
   - S4 wall luma ≥18, with ≥3 lit windows.
   - The white moon is clear of row 0.
   - Planks are readable in S4 rows y≥1080.
5. **Set dressing for the hall, chapel and crypt (pieces 1 and 3).**
   **Pass:**
   - **S2:** altar sockets and carving, plus font blood.
   - **S3:** ≥6 niches and a bridge.
   - **S1:** skull ≥45° toward the camera, no ribs below the table, and the vault has ribs (local std ≥6).
   - **S6:** wing tip ≥920.

## Distance from shippable

**Overall: 5.0/10** against the 8/10 bar, up from 4.5.

**Gains this round:**
- The hall has a top.
- Flames work everywhere.
- The crypt grade is fixed.
- S4 is framed as a castle.
- The Count's face reads at the climax.

**What holds it down:**
- The architecture wave introduced its own artefacts: doubled columns, swatch seams and untextured panels.
- Two shots regressed: S5 and the S4 surface.

**What the remaining 3 points consist of:**
- **Architectural density and integration (~45%).** The kit exists now, but it is applied as panels over the old boxes. The ribbed vaults, tracery, carved altar and sarcophagus, coffin niches, keep massing and per-block weathering the refs are built from are still absent. Until seams vanish and ornament appears, environments cap at 5–6.
- **Lighting and grade (~25%).** One room is too black (S2, S5, S6) and another too flat and orange (S1, S3). No room yet has the refs' layered floor mist and cool/warm split.
- **The signature mechanics on screen (~20%).** The candle-lean tell and the wing-wrap grab have both now failed three rounds. They are what make this map this map.
- **Composition (~10%).** S5 is under-framed, and S4 still has its moon clip and no planks.

**Path to 8/10:** at least three more rounds.
- Round 6 should be a cleanup-and-integrate pass on the kit, plus the grab and the candle lean. All of that is cheap and testable.
- After that, the remaining distance is dressing density: ornament, banners, cobwebs, debris and niches. More parameter tuning will not close it.
