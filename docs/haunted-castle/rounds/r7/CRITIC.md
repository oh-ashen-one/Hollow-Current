# Haunted Castle: round 7 blind critique

**Critic:** opus-critic, working blind.

**What I read:** DESIGN.md (including "Count Claude — redesign"), `rounds/r6/CRITIC.md`, `refs/{great_hall,chapel_altar,crypt,exterior_sky,lord_perch,count_demon_sheet}.png`, `rounds/r6/captures/*.png` and `rounds/r7/captures/*.png`.

**What I did not read:** HANDOFF, NOTES, capture.json, the handoffs or git history.

**Scope:**
- This is the first round with the demon mesh. The Count's model is now scored against `count_demon_sheet.png` (section 6), and his creature-read scores are reset.
- Performance is not scored.

**Method:** same as r6.
- Coordinates are full-resolution pixels (x, y) at 2560×1440.
- Luma is 0.299R + 0.587G + 0.114B.
- "Warm" means R > G+20, G > B and luma > 40.
- "Orange blob" means ≥20 px with R > 200 and B < 140.
- **Flame axis:** principal axis of blobs ≥150 px with R > 230, G > 120, B < 150. Angles are measured from screen vertical, and every one was checked against a 1–2.5× crop.
- **Local std:** median over 60 px windows.
- **Demon mask:** R > 20, G < 0.62R, B < 0.62R inside his bbox.

**S7 and S8 are unchanged.** Against r6 the mean abs diff is 0.000.

## Global measurements

| Capture | mean luma r6 → r7 | % px luma<8 r6 → r7 | warm % r6 → r7 | orange blobs r6 → r7 | change vs r6 (mean abs RGB /255) |
|---|---|---|---|---|---|
| S1 great hall | 35.0 → 39.2 | 14.9 → **11.6** | 6.3 → **9.0** | 70 → 73 | 0.024 |
| S2 chapel | 27.5 → 27.3 | 22.8 → 20.6 | 15.1 → 13.0 | 54 → 37 | 0.088 |
| S3 crypt | 12.3 → 23.5 | 41.9 → **23.1** | 1.0 → **9.5** | 14 → 23 | 0.069 |
| S4 exterior | 34.1 → 30.2 | 3.1 → 6.2 | 0.4 → 0.4 | 1 → 1 | 0.054 |
| S5 perch | 23.0 → 27.0 | 23.5 → 15.1 | 1.1 → 2.9 | 8 → 10 | 0.077 |
| S6 sheet | 14.4 → 15.9 | 44.8 → **34.2** | 2.0 → 1.7 | 0 → 0 | 0.028 |
| H01–H10 | 21.6–31.3 → 25.6–34.2 | 17.9–23.6 → 14.4–20.5 | 4.7–7.7 → 8.9–14.6 | 62–76 → 42–61 | 0.043–0.061 |

- **Hunt frame-pair diffs:** 3.7, 2.6, 13.0, 18.4, 24.6, 17.3, 12.3, 27.2 and 18.4. H01–H03 are one frame for the fourth round running.
- **S1 barely moved** (0.024). The only changes are the pier boxes, sticks under the gallery flames and a ring chandelier.

## 1. Round-6 checks and top-3 items

| # | Check | r7 | Measurement |
|---|---|---|---|
| 1 | 0 white blobs ≥400 px | **PASS** | The only one is the S4 moon disc. |
| 2 | S2 ≥30 orange flames | **PASS** | 37 (r6: 54) |
| 3 | Doubled columns gone | **PASS** (=) | |
| 4 | **Pier courses continuous** (S1) | **PASS** | Both piers are now single boxes: left x≈0–470, y≈380–1250; right x≈2050–2400. There is no inner box and no course offset. |
| 5 | 0 untextured faces in S4 and S5 | **PASS** (=) | |
| 6 | Seams: none longer than one module in S2–S5 | **FAIL** | **S2:** PASS. **S5:** PASS (the splice under the ledge is now covered by the Count). **S4:** weaker but still there, a 3.0 luma column step at x≈978 (y 850–1150). **S3:** the black and purple blotch grunge is still on the flat ceiling, x≈0–1300, y≈0–330. |
| 7 | **S6 swatch panel removed** | **FAIL** (=) | The lighter green-mortar panel is still at x≈615–1062, y≈220–930. |
| 8 | **Lean ≥25° at S5** | **FAIL** | S5 now has **0 flames**. Both corner sconces were removed. |
| 9 | **Lean at S1 / H06–H10** | **HALF** | **S1 FAIL:** all 22 flames are within ±5° of vertical. The one at −12° (136,80) is perspective, coaxial with its new stick. **H06–H10:** flames now tilt 45–55° (H06 −51°, −47°, +50°; H10 −56°, −49°, −48°, +55°), but they are **torn off their wicks**, see section 3. |
| 10 | **S5 fill ≥55%** | **PASS** | Demon bbox y 437 → 1439 = **69.6%** (r6 34.7%). The lower limbs are cut by the bottom edge. |
| 11 | S5 both moons within ±300 px of his head | **HALF** | Skull centre ≈(1290,870). **Red moon:** centroid (1477,583), dx 187, dy 287, PASS. **White moon:** centroid (1024,485), dx 266, **dy 385**, FAIL. |
| 12 | S5 candles (≥2 candelabras) | **FAIL**, regression | 0 (r6: 2) |
| 13 | S5 warm ≥2% | **PASS** (hollow) | 2.9%. It comes from the Count's red skin, not from candlelight. |
| 14 | S5 px<8 ≤30% | **PASS** | 15.1% |
| 15 | **Exposure 12–30%** (S1, S2, S3, S5, S6, H06–H10) | **FAIL** (8/10) | Pass: S2 20.6, **S3 23.1** (fixed), S5 15.1, H06 16.8, H07 14.9, H08 14.4, H09 17.1, H10 17.4. Fail: **S1 11.6** (new) and **S6 34.2**. |
| 16 | S1 warm 3–8%; S3 warm ≤8% | **FAIL** | 9.0% and 9.5%. Both overshot. |
| 17 | S4 wall luma ≥18 | **FAIL**, regression | 15.1 (r6 28.6) over x520–2060, y780–1160 |
| 18 | S4 ≥3 lit windows | **FAIL** (=) | 2: x≈1690–1745, y≈935–1035 and x≈1840–1920, y≈930–1100 |
| 19 | S4 white moon clear of row 0 | **PASS** | The disc now spans y≈73–465. Row 0 has 15 px >150, all stars (r6: 217). Craters are now visible. |
| 20 | Planks in S4 rows y≥1080 | **FAIL** (=) | Moat to y≈1340, then dark tile (rows 1290–1440: luma 20.6, std 12.6). No deck. |
| 21 | Chains anchored | **HALF** (=) | They still run across the moat to the near bank at y≈1340. |
| 22 | **Grab: wing tips at the frame edges** | **FAIL** | Left tip x≈365 (y≈505); right x≈2554 (y≈320) |
| 23 | **Grab: torso pitched ≥25°, claw ≥200 px within 300 px of centre** | **FAIL** (=) | Same plumb body and arms-up pose as r6. The claws sit at shoulder height, left x≈1040–1140, y≈550–650 (≈100 px wide) and right x≈1480–1640, y≈530–630. Nothing comes toward the lens. |
| 24 | Grab: face visible | **PASS** | Skull at x≈1318–1377, y≈510–607 |
| 25 | Grab: feet in frame, height 45–70% | **PASS** | y≈506 → 1385 = 61% |
| 26 | H05 face visible, soles hidden | **PASS** | Upright and front-facing. Skull at x≈1215–1260, y≈555–620. No soles shown, though the feet are hidden behind a book at y≈900–1080. |
| 27 | H01→H03 diffs ≥8 | **FAIL** (=) | 3.7 and 2.6 |
| 28 | Closest two frames show his face | **PASS** | H09 and H10 |
| 29 | **Altar sockets readable, front std ≥8** | **HALF** | The camera was raised and the altar top is visible. **Sockets:** they are **raised beige plates seen at a grazing angle**, not recesses: ring x≈1140–1205, y≈563–580; slot plate x≈1335–1450, y≈565–580. The cross is hidden behind a floating candle stick at x≈1255–1290. The order is mirrored against the ref. **Front:** local std **1.8** (r6 2.4), still a black box. |
| 30 | S2 font blood ≥40,000 px with a specular | **HALF** | 151,932 px at x≈103–910, y≈1151–1439, but max luma 18.3 and std 2.2: matte, no specular. |
| 31 | **Crypt: ≥6 niches + bridge** | **FAIL** | **Niches:** 0. **Bridge:** a flat pink-lit brick ramp with side kerbs at x≈1120–1450, y≈930–1020, with no arch and no channel visible under it. |
| 32 | S3 vault with ≥3 arch rings | **FAIL** (=) | Flat tiled ceiling |
| 33 | S1 skull | N/A | |
| 34 | S1 vault ribs | **HALF** (=) | Local std 4.1 (r6 4.0, same method). The geometry is unchanged. |
| 35 | S1 fog veil std ≤12 | **FAIL** | 21.6 |
| 36 | S6 wing tip ≥y920 | **RETIRED → PASS** | This check was written for the old mesh. The new wing tips sit at 61% and 57% of body height, against 52% on the sheet. The proportion matches. |
| 37 | S5 banner row-striping gone | **FAIL**, worse | Left banner x 20–420, y 960–1250: median row diff **4.3**, p90 **6.9** (r6 2.5 and 5.4). Right banner: 3.6 and 5.7. |
| 38 | S4 ≥3 spires | **PASS** (=) | |
| r6 | **0 chip decals** | **PASS** | They are gone from the S2 altar, the S4 wall at x≈720–770 and the S6 left wall at x≈255–335. |
| r6 | **S1 focal subject** ≥25% of width, centred | **FAIL** | The only addition is a ring chandelier at x≈1184–1376, y≈480–580. That is 192 px, **7.5%** of the width. |
| r6 | **Solid wing membranes in H09 and H10** | **FAIL** | They are still strands. Column x=600 in H10 holds 12 membrane px across a 98 px span, x=800 holds 66 px in 12 segments, and gaps run up to 87 px (target ≤20). |

**Tally: 15 PASS, 6 HALF, 18 FAIL, 1 N/A, 1 retired** (41 rows).
- **Wins:**
  - Pier overlap is gone.
  - Chip decals are gone.
  - The S3 crush is fixed.
  - The S4 moon is clear of row 0.
  - The S5 Count fill is 69.6%.
  - H05 shows his face.
  - The flames finally move.
- **Unfixed for the fifth round:** the grab pose, H01–H03, planks, niches, the vault, the S1 subject and the wing membranes.

## 2. Scores (0–10), delta vs r6

Creature-read is **reset** for the new mesh. Its delta is shown, but it is not like-for-like.

| Shot | Comp | Silhouette | Materials | Lighting | Surreal | Colour/atmos | Creature read | Anim/behaviour | Scale |
|---|---|---|---|---|---|---|---|---|---|
| S1 great hall | 4 (0) | 5 (0) | 5 (0) | 6 (0) | 4 (0) | **5 (−1)** | — | — | — |
| S2 chapel | **5 (+1)** | 4 (0) | 5 (0) | 5 (0) | **4 (−1)** | 6 (0) | — | — | — |
| S3 crypt | 4 (0) | 3 (0) | 3 (0) | **5 (+1)** | 4 (0) | **5 (+1)** | — | — | — |
| S4 exterior | 5 (0) | 4 (0) | 4 (0) | **3 (−1)** | 4 (0) | 4 (0) | — | — | — |
| S5 perch | **6 (+2)** | **6 (+1)** | 5 (0) | 5 (0) | **3 (−1)** | 5 (0) | **6 (+1)** | 5 (0) | **6 (+2)** |
| S6 sheet | 5 (0) | 7 (0) | 5 (0) | **4 (+1)** | — | 4 (0) | 5 (reset) | 6 (0) | 7 (0) |
| H01–H10 | 5 (0) | **5 (+1)** | 5 (0) | 6 (0) | **3 (−1)** | 5 (0) | 6 (0) | 4 (0) | 6 (0) |
| S7 / S8 | unchanged | | | | | | | | |

### Progress lines

- **S1: same.**
  - Gains: the piers are clean.
  - Losses: warm and dark both drifted out of band, and the "hero" is a 7.5%-wide chandelier.
- **S2: better framing, worse candles.**
  - Gains: the altar top and the font are now in frame.
  - Losses:
    - The flames are splayed outward, −29° to +25°, radial from the centre.
    - Several flames float clear of their wicks, for example (1818,247) and (2473,340).
    - One flame is a flattened 11,828 px horizontal blob at (406,65).
- **S3: better grade, worse foreground.**
  - Gains: the exposure is in band and the blood is now dark wine (R/B 3.47, luma 51.9, PASS).
  - Losses:
    - The camera now puts cube-palmed, stick-fingered hands at the near plane (x≈25–330, y≈780–1100 and x≈2190–2560, y≈580–870). They read as low-poly toys.
    - Still no niches and no vault.
- **S4: moon fixed, wall darker.**
  - Gains: the moon has craters and clears row 0.
  - Losses: the wall luma halved, and there is still no keep, no deck and only 2 windows.
- **S5: big composition gain.**
  - Gains: a low camera and the demon filling 69.6% of the height.
  - Losses:
    - All candles are gone, so the tell cannot show.
    - The white moon sits 385 px above his skull.
    - The lower limbs make a hoop that exits the bottom edge (x≈1000–1620, y≈1220–1440), reading as dangling in front of the ledge, not crouched on it.
    - The banner striping got worse.
- **S6: slightly better.**
  - Gains: px<8 is 34.2 (from 44.8) and the chips are gone.
  - Losses: the swatch panel and the room are unchanged.
- **Hunt: new creature, same choreography, new flame artefacts.**

## 3. Regressions with coordinates

1. **Flames torn off their wicks.** This is new and affects every hunt frame and S2. The flame meshes are translated as well as rotated:
   - **H10:** the left floor-candelabra wicks sit at x≈190–370, y≈1290–1340 with only a blurry glow left. Their four flames float at **x≈600–760, y≈1280–1350**, 300–400 px away.
   - **H10:** the chandelier flames scatter as a cloud of orange petals at x≈950–1260, y≈700–940. The same happens in H09 at x≈570–730, y≈1230–1290.
   - It reads as embers or leaves blowing, not candles bending.
2. **Orange "laser" streaks.** A flame stretched into a beam:
   - **H05:** x≈538–1376, y≈777–800, passing behind the Count.
   - **H07:** x≈790–1530, y≈450–475.
3. **S5 candelabras removed:** 0 flames (r6 had 4).
4. **S4 wall luma** 28.6 → 15.1.
5. **S1** px<8 14.9 → 11.6 and warm 6.3 → 9.0. **S3** warm 1.0 → 9.5.
6. **S5 banner striping:** p90 row diff 5.4 → 6.9.
7. **S2 altar front** std 2.4 → 1.8. A floating candle now blocks the cross socket at x≈1255–1290.
8. **S3 foreground cube-hands:** see S3 above.

## 4. Per shot

### S1 great hall
**Biggest gap:** still no subject.

**Pass:** one object ≥25% of frame width, centred in x≈900–1660, ≥15% of its px at luma >60. The skeleton or the laid dining table with chairs and lit candelabras above the fog would both do.

Secondary issues:
- px<8 11.6 → 12–30, and warm 9.0 → ≤8.
- Fog std 21.6 (≤12).
- No vault ribs or bosses.
- Flames 0° (target ≥25° when he is in the hall).

### S2 chapel altar
**Biggest gap:** the altar is a black box with flat plates on top.

**Pass:**
- Three **recessed** sockets (slot, cross, round, in that order) cut into the top, readable at a ≥30° view angle in x≈1060–1500, y≈540–620.
- Nothing occluding them.
- Carved front with local std ≥8.

Secondary issues:
- Reattach the flames to their wicks.
- The font blood has no specular (max luma 18.3).
- No banners or cobwebs.
- The window niche is empty.

### S3 crypt
**Biggest gap:** 0 niches, no arched bridge and no vault, for the fifth round.

**Pass:**
- ≥6 coffin niches in the side walls.
- An arched bridge with the channel visible under it.
- ≥3 receding arch rings.

Secondary issues:
- Warm 9.5 → ≤8.
- Replace the cube-palm hands with tapered root-fed hands (≥8 faces across the palm).
- Remove the ceiling blotches.

### S4 exterior
**Biggest gap:** one slab, no keep and no plank deck.

**Pass:**
- A keep ≥2× the wall height.
- A plank deck in rows ≥1080.
- ≥3 lit windows.
- Wall luma ≥18.

Secondary issues:
- Seam at x≈978.
- Chains to the gatehouse, not to the bank.
- Flat moat with no reflections.
- No horizon landscape.

### S5 lord perch
**Biggest gap:** the hero frame has no candles, so the map's signature tell is absent from its signature shot.

**Pass:**
- ≥2 lit candelabras in frame, with flames leaning ≥20° toward him and attached to their wicks.
- Both moons within ±300 px (per axis) of the skull.

Secondary issues:
- Pose: crouched on top of the ledge with the feet visible, not a hoop of limbs below it.
- Banner striping p90 ≤3.
- The moons are flat discs behind the spokes.

### S6 lord sheet
**Biggest gap:** the demon reads as one red mass (section 6).

**Pass:**
- ≥12% of his px grey-white (R−B<45, luma>90).
- ≤40% red-dominant.
- px<8 ≤30%.
- Swatch panel gone.

### H01–H10 hunt
**Biggest gap:** the grab is unchanged and the wings are still strands.

**Pass:**
- H10 torso pitched ≥25° toward the camera.
- Membrane gaps ≤20 px between spars in H09 and H10.
- Tips at x<80 and x>2480.
- One claw ≥200 px wide within 300 px of centre.

Secondary issues:
- H01–H03 duplicates.
- Detached flames and the H05 and H07 streaks.
- The skull is unreadable below ~60 px head height (H01–H07).

## 5. Verdicts per piece

| Piece | r6 | r7 | Why |
|---|---|---|---|
| 1 Layout | APPROACHES | **APPROACHES** (=) | S5 framing landed. Against that, S3 niches, bridge and vault, the S4 keep and deck, and the S1 subject are all still missing. |
| 2 Art / architecture | APPROACHES | **APPROACHES** (=) | Piers, chips and the S3 crush are fixed. The S4 wall darkened, S1 and S3 grades drifted out of band, the S6 swatch remains and the banner striping got worse. |
| 3 Surreal props | FAILS | **FAILS** (=) | The flames move for the first time, but they detach from the wicks, stretch into beams and vanish from S5. That is a visible bug, not a tell. |
| 4 Count | FAILS | **FAILS** (↑ model, = anim) | The new mesh is clearly scarier, but the grab, the strand wings, H01–H03 and the red cast keep it below APPROACHES. |
| 0 UI | APPROACHES | **APPROACHES** (=) | Untouched |

## 6. The demon vs count_demon_sheet.png (scored)

| Axis | Score | Evidence |
|---|---|---|
| Silhouette and proportions | **7** | See "Silhouette" below. |
| Skull and split jaw at S6 | **5** | See "Skull and split jaw" below. |
| Skull at hunt distances | **3** | See "Skull and split jaw" below. |
| Flayed chest, blood and texture fidelity | **4** | See "Flayed chest, blood and texture" below. |
| Wing membrane, idle and folded | **6** | See "Wing membranes" below. |
| Wing membrane, grab | **2** | See "Wing membranes" below. |
| Scale vs the 1.8 m figure (S6) | **7** | 2.41 m estimated, against 2.5 m (target −4%). It is 12% under "nine feet" (2.74 m). |
| Scarier than the aristocrat (r6 vs r7, H08–H10) | **6** | See "Scarier than the aristocrat?" below. |

**Silhouette:**
- **Height (S6):** the demon is 792 px (y 353–1145). The 1.8 m capsule is 523 px (y 560–1083).
- **Depth correction:** his feet are 62 px lower, so he stands nearer the camera. The base-to-horizon ratio is 538/476 = 1.13×, which gives **≈2.41 m**.
- **Wingspan/height:** 0.63, against 0.65 on the sheet.
- **Fingertips:** at 66% of height, against 73% on the sheet. They stop just above the knee, not below it.
- Limbs are long and skeletal, the legs are digitigrade and the claws are hooked.

**Skull and split jaw:**
- **At S6:** the skull is 68×128 px at x≈1407–1475, y≈352–480. A hairless elongated skull with a long dark open jaw and drips reads.
- **Missing at S6:** the vertical split, the needle teeth and the ember eyes are not resolvable. The sockets are brown smudges.
- **H10:** the mouth is a ≈17 px "O" at x≈1340–1357, y≈573–600.
- **Hunt distances:** below ~60 px of head height (H01–H07), only a pale dome reads.

**Flayed chest, blood and texture:**
- **Global red cast:** **80%** of his px are red-dominant (G < 0.5R), against **26%** on the sheet. Grey-white skin is **0.5%** of his px, against **16.5%** on the sheet. His mean RGB is 54/19/15.
- **The room is not the cause:** the capsule beside him renders beige.
- **The result:** the sheet's three-way contrast of pale skin, raw red flesh and black coat collapses into one red, wet "gummy" mass.
- **Texture faults:**
  - No mirroring: the high-pass left/right correlation on the torso is 0.09.
  - Smearing: there are vertical stretch streaks along the hem strips and along every wing strand.
  - The face texture is soft, with no crisp teeth.

**Wing membranes:**
- **Idle and folded (S5, S6, H05, H08):** solid tattered panels with holes. This is close to the sheet, but red-brown, not pale skin.
- **Grab:** collapsed strands, as in r6 (see the r6 membrane row in section 1).

**Scarier than the aristocrat?**
- **H08:** yes, clearly. The skull, the claws and the flayed torso beat the human face.
- **H09 and H10:** the identical arms-up puppet pose with strand wings reads as a hanging red wax effigy, so the gain is halved.

**Overall model fidelity: ≈45–50% of the sheet** (r6: 15–20%). The silhouette is right. The surface colour and the grab are what remain.

## Ranked top 5 for round 8

1. **The grab (piece 4).** Fix the membrane skinning in the wing-wrap clip (the membrane must follow the finger bones, not stretch past them), then pitch the torso.
   - **Pass:**
     - H09 and H10 membrane gaps ≤20 px.
     - Tips at x<80 and x>2480.
     - H10 torso pitched ≥25°.
     - One claw ≥200 px wide within 300 px of centre.
     - H01→H03 diffs ≥8.
2. **The candle rule (piece 3).** Rotate each flame about its wick. Do not translate it.
   - **Pass:**
     - Every flame centroid within 1.5 flame-lengths of its wick.
     - Axis ≥20° toward him.
     - 0 orange streaks ≥250 px.
     - ≥2 lit candelabras back in S5.
     - Lean visible in S1 when he is present.
3. **Demon material (piece 4).** Remove the red cast and restore the pale skin.
   - **Pass (S6):**
     - Grey-white ≥12% of his px.
     - Red-dominant ≤40%.
     - An ember (R>180 cluster) in each socket.
     - Teeth resolvable at S6.
4. **Room focal dressing (pieces 1 and 3).**
   - **Pass:**
     - **S2:** three recessed sockets and a carved front with std ≥8.
     - **S3:** ≥6 niches, an arched bridge and ≥3 vault rings.
     - **S4:** a keep, a plank deck, ≥3 windows and wall luma ≥18.
     - **S1:** a ≥25%-wide centred hero object.
5. **Grade and cleanup (piece 2).**
   - **Pass:**
     - S1 px<8 12–30 and warm 3–8.
     - S3 warm ≤8.
     - S6 px<8 ≤30 and the swatch panel gone.
     - Banner p90 ≤3.
     - S1 fog std ≤12.

## Distance from shippable

**Overall: 5.3/10** against the 8/10 bar (r6 5.0). The new demon and the S5 framing are real gains. Against them are the new flame bugs, the removed S5 candles, the S4 darkening and five-round-old misses on the grab, the planks, the niches and the S1 subject.

**What the remaining 2.7 points consist of:**
- **Signature mechanics (~30%).** The grab is still a puppet pose with strand wings. The candle tell now exists, but it renders as a bug.
- **Architectural density (~35%).** No keep, deck, niches, vault, carved altar or hall hero. The kit is clean but bare.
- **Creature surface (~15%).** The red cast, the unreadable split jaw and the missing ember eyes.
- **Grade and composition (~20%).** S1, S3 and S6 are out of band, there is no layered floor mist, and the banner striping persists.

Items 1–3 are cheap and testable and should take round 8 before any new stone.
