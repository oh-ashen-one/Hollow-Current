# Haunted Castle: round 6 blind critique

**Critic:** opus-critic, working blind.

**What I read:** DESIGN.md, `rounds/r5/CRITIC.md`, `refs/{great_hall,chapel_altar,crypt,exterior_sky,lord_perch,count_demon_sheet}.png`, `rounds/r5/captures/*.png` and `rounds/r6/captures/*.png`.

**What I did not read:** HANDOFF, NOTES, capture.json, the handoffs or git history.

**Owner direction applied:**
- The Count is scored only on animation, behaviour and readability. His model is compared with `count_demon_sheet.png` in its own paragraph and is not scored.
- Performance is not scored, because the frame times are contaminated.

**Method:** same as r5.
- Coordinates are full-resolution pixels (x, y) at 2560×1440.
- Luma is 0.299R + 0.587G + 0.114B.
- "Warm" means R > G+20, G > B and luma > 40.
- "Orange blob" means ≥20 px with R > 200 and B < 140.
- **Flame lean:** principal axis of each flame blob ≥150 px, compared against its candle stick at 2× crops.
- **Local std:** median over 60 px windows.

**S7 and S8 are unchanged.** Against r5 the mean abs diff is 0.0000, so every r4 and r5 UI note still stands.

## Global measurements

| Capture | mean luma r5 → r6 | % px luma<8 r5 → r6 | warm % r5 → r6 | orange blobs r5 → r6 |
|---|---|---|---|---|
| S1 great hall | 42.8 → 35.0 | 7.3 → **14.9** | 11.2 → **6.3** | 63 → 70 |
| S2 chapel | 23.2 → 27.5 | 35.3 → **22.8** | 11.0 → 15.1 | 51 → 54 |
| S3 crypt | 31.4 → 12.3 | 4.2 → **41.9** | 16.2 → **1.0** | 31 → 14 |
| S4 exterior | 29.8 → 34.1 | 10.3 → **3.1** | 0.5 → 0.4 | 1 → 1 |
| S5 perch | 15.5 → 23.0 | 36.1 → **23.5** | 0.0 → 1.1 | 1 → 8 (4 of them are the red moon) |
| S6 sheet | 16.8 → 14.4 | 33.5 → **44.8** | 0.1 → 2.0 | 0 → 0 |
| H01–H10 | 25.1–36.2 → 21.6–31.3 | 8.2–14.9 → **17.9–23.6** | 8.0–9.8 → 4.7–7.7 | 48–59 → 62–76 |

- **Hunt frame-pair diffs:** 2.9, 2.8, 12.8, 15.7, 21.7, 17.4, 13.0, 26.2 and 15.8. H01–H03 are still one frame, for the third round running.
- **White blobs:** 0 pure-white blobs ≥400 px anywhere.
- **Per-shot change vs r5 (mean abs RGB diff):** S1 0.055, S2 0.034, S3 0.071, S4 0.059, S5 0.062, S6 0.022.

## 1. Round-5 checks and top-5 items

| # | Check | r6 | Measurement |
|---|---|---|---|
| 1 | 0 white blobs ≥400 px | **PASS** | 0 |
| 2 | S2 ≥30 orange flames | **PASS** | 54 |
| 3 | **Doubled columns gone** (S1, H) | **PASS** | No cylinders remain inside piers in S1 or H01–H10. Every arcade support is now a square coursed pier. |
| 4 | Mortar continuous across each pier | **FAIL** | Two pier boxes overlap with offset course grids. The S1 right pier has a lighter inner box showing through at x≈2220–2340, y≈450–1000. The S1 left pier has the same at x≈255–320, y≈420–840. |
| 5 | 0 untextured faces in S4 and S5 | **PASS** | The S4 black panel and the S5 dark band are gone. |
| 6 | **Seams gone**: no straight material seam longer than one module in S2, S3, S4, S5 | **FAIL** | **S2:** PASS. The charcoal/beige split at y≈497 is gone and it is one brick family. **S4:** panels with different brick size and value, with vertical seams at x≈990, ≈1570 and ≈1665 (y≈790–1260) and a horizontal seam at y≈985 (x≈1375–1595). **S5:** a three-material splice directly under the Count, white ashlar x≈780–1100, navy x≈1100–1460 and moss-tinted x≈1460–1760, y≈1195–1440. **S3:** the black/purple glossy blotch grunge is still on the flat ceiling tiles. |
| 7 | **Gatehouse repetition**: S6 autocorrelation ≤0.30 | **PASS (number) / FAIL (intent)** | Peak r = 0.13 at 205 px, against 0.20 with the same method on r5. My method does not reproduce r5's 0.46 at 388 px, so the trend is reliable and the absolute value is not. The room was **not rebuilt**. Instead, one lighter brick panel with green mortar was dropped in at x≈620–1065, y≈350–930 with hard vertical seams, so repetition became a swatch. Black-outlined "chip" decals float on the left wall at x≈255–335, y≈460–660. |
| 8 | **Lean ≥25° at S5** | **FAIL** | There are 4 real flames: (9,1204) 2.8°, (196,1145) 7.1°, (2380,1168) 4.2°, (2477,1136) 4.7°. All are coaxial with their sticks, within 8°. |
| 9 | **Lean ≥25° at S1 / ≥20° in H06–H10** | **FAIL** | The S1 floor candelabras (x≈1840–1960, y≈780–900 and x≈2240–2370, y≈780–880) burn straight up. In H10 the sticks themselves tilt 17–37° from camera perspective (looking up), but every flame continues its stick's axis with 0° offset: left group x≈210–370, y≈1300–1360; right group x≈1900–2030, y≈1310–1340. **The candle rule has now failed four rounds.** |
| 10 | **S5 fill ≥55%** | **FAIL, regression** | Wing-claw top y≈650 to finger tips y≈1150 is 500 px = **34.7%** (r5 39.2%). The camera pulled back further. |
| 11 | **S5 both moons** within ±300 px of his head | **PASS** | White ≈(1080,560), red ≈(1460,590), head ≈(1300,790). They are flat glowing discs with no surface detail, half-hidden behind spokes. |
| 12 | **S5 candles** (≥2 candelabras) | **PASS** | Two sconces, at the frame corners x≈0–220 and x≈2360–2500, y≈1080–1260. |
| 13 | S5 warm ≥2% | **FAIL** (close) | 1.1% |
| 14 | S5 px<8 ≤30% | **PASS** | 23.5% |
| 15 | **Exposure 12–30%** for S1, S2, S3, S5, S6, H06–H10 | **FAIL** (8/10) | Pass: S1 14.9, S2 22.8, S5 23.5, H06 20.5, H07 18.6, H08 17.9, H09 21.5, H10 22.3. Fail: **S3 41.9** (overshot from 4.2) and **S6 44.8** (was 33.5). S4 is 3.1, though S4 is a night sky. On all 16 castle captures the band holds 13/16, against 5/16 in r5. |
| 16 | S1 warm 3–8%; S3 warm ≤8% | **PASS** | 6.3% and 1.0% |
| 17 | S4 wall luma ≥18 | **PASS** | 28.6 over x520–2060, y780–1160 (r5 12.6) |
| 18 | S4 ≥3 lit windows | **FAIL** | 2, both on the right: x≈1680–1740, y≈930–1030 and x≈1840–1920, y≈920–1110. |
| 19 | S4 white moon clear of row 0 | **FAIL** | 217 px of row 0 have luma >150 (r5 243). |
| 20 | **Planks** readable in S4 rows y≥1080 | **FAIL** | Rows 1080–1380 are flat red moat. Below that is a dark strip with tile lines (rows 1290–1440: luma 24.3, std 11.3). There is no bridge deck. |
| 21 | **Chains** anchored | **HALF** | They now run from the raised bridge top (x≈1200 and ≈1365, y≈950) across the moat to the near bank at y≈1415. They are anchored to something now, but a raised drawbridge's chains should run into the gatehouse wall, not across the water to the viewer. |
| 22 | **Grab: wing tips at the frame edges** (x<80 and x>2480) | **FAIL** | The right tip reaches x≈2555 (y≈395). The left tip stops at x≈290 (y≈530). |
| 23 | **Grab: claws past the lens / torso pitched toward the camera** | **FAIL** | Same plumb vertical body as r5. The arms are raised with the claws at shoulder height (x≈1180 and ≈1500, y≈610), each ≈60 px wide, and nothing reaches toward the camera. |
| 24 | **Grab: face visible** | **PASS** | Face and both eyes at x≈1310–1370, y≈580–680. |
| 25 | **Grab: feet in frame**, height 45–70% | **PASS** | Claw tops y≈614 to toes y≈1293 = 47%. The feet are unclipped. |
| 26 | H05 face visible and soles hidden | **FAIL** (=) | Head tipped back with the chin up, face hidden, and the bare soles toward the camera, x≈1100–1350, y≈570–900. This is the same underside view as r5. |
| 27 | H01→H03 diffs ≥8 | **FAIL** (=) | 2.9 and 2.8 |
| 28 | Hunt: the closest two frames show his face | **PASS** | H09 and H10 |
| 29 | **Altar sockets** (slot, cross, round) and carved front, local std ≥8 | **FAIL** | No sockets are visible. The camera sees the altar top edge-on, with only a red cloth strip at y≈770. The front is a veined black box (local std **2.4**) with one white plaque and seven grey "chip" decals at x≈1360–1500, y≈820–930 that read as holes. |
| 30 | S2 font blood | **HALF** | A flat red plane is just visible at x≈600–950, y≈1400–1440, cut by the frame bottom. It has no gloss. |
| 31 | **Crypt: ≥6 niches + bridge** | **FAIL** | 0 niches and 0 bridge. The small plank piece r5 had at x≈1540–1720, y≈720–770 is gone. The only addition is two red candles on a block at x≈1580–1610, y≈640–720. |
| 32 | S3 vanishing vault with ≥3 arch rings | **FAIL** | Flat tiled ceiling |
| 33 | S1 skull ≥45°, no ribs under the table | **N/A** | The skeleton is no longer in the S1 frame. Only a rib cage at x≈490–615, y≈610–1020 remains, behind the left pier. |
| 34 | S1 vault ribs (local std ≥6) | **HALF** | The number passes at 6.3 (r5 4.4), but the geometry is unchanged: a smooth barrel with two plain transverse arches, and no ribs or bosses. |
| 35 | S1 fog veil std ≤12 (rows 1000–1300) | **FAIL** | 23.0 |
| 36 | S6 wing tip ≥ y920 | **FAIL** (=) | ≈895 (left) and ≈870 (right) |
| 37 | S5 banner row-striping gone | **FAIL** (=) | Left banner x≈340–720, y≈900–1440: median row-to-row R diff 2.5 and p90 5.4 on a mean of 22.8. Right banner: p90 5.3. |
| 38 | S4 ≥3 spires; castle ≤45% of the frame | **PASS** | Unchanged silhouette, 5 short cones on one slab, and still no keep. |

**Tally: 13 PASS, 4 HALF, 20 FAIL, 1 N/A** (38 rows).
- **Wins:** the cylinders are gone, untextured faces are gone, the exposure band holds on 13/16 captures, S4 wall luma is fixed, both S5 moons show, and S5 has candles.
- **Fails again:** every signature item. The candle lean is 0° for the fourth round, the grab is still a puppet pose, S5 fill got worse, and there is still no altar sockets, no crypt niches and no planks.

## 2. Scores (0–10), delta vs r5

| Shot | Comp | Silhouette | Materials | Lighting | Surreal | Colour/atmos | Creature read | Anim/behaviour | Scale |
|---|---|---|---|---|---|---|---|---|---|
| S1 great hall | **4 (−1)** | 5 (0) | 5 (0) | 6 (0) | **4 (−1)** | 6 (+1) | — | — | — |
| S2 chapel | 4 (0) | 4 (0) | 5 (+1) | 5 (0) | 5 (0) | 6 (+1) | — | — | — |
| S3 crypt | 4 (0) | 3 (0) | 3 (0) | **4 (−1)** | 4 (0) | **4 (−1)** | — | — | — |
| S4 exterior | 5 (0) | 4 (0) | 4 (+2) | 4 (+1) | 4 (0) | 4 (0) | — | — | — |
| S5 perch | **4 (−1)** | 5 (0) | 5 (+1) | 5 (+2) | 4 (+2) | 5 (+2) | **5 (−1)** | 5 (0) | 4 (0) |
| S6 sheet | 5 (0) | 7 (0) | **5 (−1)** | **3 (−1)** | — | 4 (0) | 7 (0) | 6 (0) | 7 (0) |
| H01–H10 | 5 (0) | **4 (−1)** | 5 (0) | 6 (+1) | 4 (0) | 5 (0) | **6 (−1)** | **4 (−1)** | 6 (0) |
| S7 / S8 | unchanged | | | | | | | | |

### Progress lines

- **S1: worse on composition, better on grade.**
  - Gains: the doubled cylinders are gone, the warm share is 6.3% (in band), and px<8 is 14.9% (in band).
  - Losses:
    - The skeleton, the hall's one focal subject, left the frame. The centre is now an empty fog lane (x≈1100–1500, y≈900–1440) running to a single candelabra.
    - The piers now read as stacked brick boxes, two of them interpenetrating.
- **S2: better (small).**
  - Gains: one stone family, exposure in band, and a hint of font blood.
  - Losses: the altar is still a box, now with hole-like chip decals.
- **S3: worse.**
  - Losses: it overshot from too flat to crushed (41.9% px<8).
  - The blood plane is a saturated emissive pink (RGB ≈80,17,23 averaged, but it reads neon at the ×2 crop), unlike the ref's dark wine reflection.
  - The ceiling grunge remains, it lost the plank piece, and there are still no niches, bridge or vault.
- **S4: better on surface.**
  - Gains: one blue brick family, the black panel gone, and wall luma 28.6.
  - Losses: panel seams, the moon still touching row 0, no planks and only 2 windows.
- **S5: same overall.** The moons and candles arrived, but the Count shrank to 34.7%, the splice moved under him, and the striping remains.
- **S6: worse.** It is darker (44.8%) and the panel swap is new. The wing tip and the room are unchanged.
- **Hunt: worse on the Count.**
  - Gains: exposure is in band on all ten frames.
  - Losses:
    - In H09 and H10 **the wing membranes have collapsed into needle-thin spikes**: H10 x≈290–1000, y≈450–650 and x≈1760–2555, y≈350–650, and H09 x≈1500–2300, y≈420–650. That is a mesh or skinning regression, not a pose.
    - The grab pose is otherwise identical to r5.

## 3. Regressions with coordinates

1. **Wing membrane collapse** (H09, H10). The wings are needle spikes instead of membrane; see the coordinates above. This is the worst new artefact, and it lands on the climax frame.
2. **S5 Count fill** 39.2% → 34.7%.
3. **S3 crush:** px<8 4.2 → 41.9%. Neon-pink blood plane at x≈330–1100, y≈800–1200.
4. **S6 crush** (33.5 → 44.8%) and the new light panel at x≈620–1065, y≈350–930.
5. **S1 lost its focal subject.** The skeleton is out of frame and the centre lane is empty.
6. **Pier-in-pier overlap** with offset courses: S1 at x≈2220–2340, y≈450–1000 and x≈255–320, y≈420–840.
7. **The black "chip" decal** is reused across rooms and reads as holes: S2 altar x≈1360–1500, y≈820–930; S4 wall x≈720–770, y≈960–1020 and x≈1965–2020, y≈1110–1170; S6 x≈255–335, y≈460–660.
8. **S3 plank piece removed** (it was at x≈1540–1720, y≈720–770 in r5).

## 4. Per shot

### S1 great hall
**Biggest gap:** the frame has no subject. Restore a hero element on the central axis: the skeleton (if the owner allows), or the dining table with chairs and candles lit above the fog.

**Pass:** a single object ≥25% of frame width centred within x≈900–1660, with ≥15% of its pixels at luma >60.

Secondary issues:
- Pier overlap at x≈2220–2340 and x≈255–320: make the courses continuous.
- Fog veil std 23 (≤12).
- No vault ribs or bosses.
- The rose window is a cartwheel in a square box.

### S2 chapel altar
**Biggest gap:** the altar is a black box with no visible sockets. The camera is too low to see its top.

**Pass:**
- Raise the camera or tilt the altar top so three recesses (slot, cross, round) read in x≈1060–1500, y≈700–800.
- Front local std ≥8.
- Remove the chip decals.

Secondary issues:
- The font blood is cropped. Show ≥40,000 px of it with a specular.
- No banners or cobwebs.
- Oversized boxy candles.
- The window niche is an empty dark arch in a flat frame.

### S3 crypt
**Biggest gap:** still a lit box around a basin, with 0 niches, no bridge and no vault, for the fourth round.

**Pass:** ≥6 coffin niches, a bridge over the channel, and ≥3 receding arch rings.

Secondary issues:
- px<8 41.9% → 12–30%.
- Change the blood from emissive pink to dark wine, R/B ≥3 and luma ≤60 except speculars.
- Remove the ceiling blotches.
- The hands still lack volume.

### S4 exterior
**Biggest gap:** still one trapezoid slab with no keep, and the drawbridge deck is missing from the foreground.

**Pass:**
- A keep ≥2× the wall height.
- Plank deck visible in rows y≥1080.
- Moon clear of row 0.
- ≥3 lit windows.

Secondary issues:
- Panel seams at x≈990, 1570 and 1665, and at y≈985.
- Chip decals.
- The moat is flat with no reflections.
- No horizon landscape.

### S5 lord perch
**Biggest gap:** the Count is 34.7% of a frame that is mostly wall.

**Pass:** Count bbox ≥55%, and his head within ±300 px of both moons.
- Move the camera to ≈2.5 m below and in front of the ledge, looking up (the ref's framing).
- Or raise the FOV crop.

Secondary issues:
- Candle lean 0°.
- The splice under the ledge at x≈1100 and ≈1460.
- Banner striping.
- Warm 1.1% (≥2).
- The moons are flat discs. Give them surface detail.

### S6 lord sheet
**Biggest gap:** the wing tip is at ≈895 against ≥920. The room is unchanged and now darker, with a swatch panel.

**Pass:**
- Tip ≥920.
- px<8 ≤30%.
- No panel seam longer than one module.

### H01–H10 hunt
**Biggest gap:** there is still no grab, and the wings broke into spikes.

**Pass:**
- In H10 the torso is pitched ≥25° toward the camera.
- Both wing membranes are solid (no gaps wider than 20 px between spars) and enter from x<80 and x>2480.
- At least one claw is ≥200 px wide and within 300 px of frame centre.
- Eyes visible, feet unclipped or cropped above the knee.

Secondary issues:
- H05 shows the underside.
- H01–H03 are duplicates.
- Flames show 0° lean.

### S7 and S8
Unchanged from r4.

## 5. Verdicts per piece

| Piece | r5 | r6 | Why |
|---|---|---|---|
| 1 Layout | APPROACHES | **APPROACHES** (=) | S3 niches, bridge and vault, the S4 keep and planks, and the S5 framing are all still missing. The S1 centre is emptied. |
| 2 Art / architecture | APPROACHES | **APPROACHES** (↑ slightly) | Cylinders and untextured faces are gone, there is one stone family per room, and the exposure band holds 13/16. Against that: panel seams in S4, S5 and S6, pier overlap, chip decals, and S3 and S6 crushed. |
| 3 Surreal props | FAILS | **FAILS** (=) | The candle lean is 0° everywhere for the fourth round. |
| 4 Count (anim, behaviour, readability) | APPROACHES | **FAILS** (↓) | The grab is unchanged, H05 is unchanged, H01–H03 are duplicates, and the wing membranes now collapse into spikes on the climax frames. |
| 0 UI | APPROACHES | **APPROACHES** (=) | Untouched. |

## 6. Count model vs count_demon_sheet.png (baseline, not scored)

The in-game model is roughly **15–20% of the way to the sheet**, and nearly all of the match is silhouette, not surface.

**What already matches:**
- An upright winged biped with a high collar.
- Buckled straps across the chest.
- A tattered hem.
- Bare clawed feet.
- Hooked thumb-claws on the wing tops.

**Everything that defines the sheet is absent:**
- **Head:** the model has a full human face with intact skin, slicked black hair and pointed ears (S6 x≈1430–1500, y≈410–520). The sheet has a hairless skinless skull, a vertically split jaw with needle teeth, black eye sockets and blood running from the mouth.
- **Proportions:**
  - The model is human-proportioned. The S6 fingertips sit at mid-thigh (y≈760 on a body from y≈410 to ≈1050).
  - The sheet's arms are ~1.3× longer, with six-finger claws reaching below the knee, and the limbs are emaciated with visible joints.
- **Torso:** the model is covered by a closed long coat. The sheet has a flayed, bloody rib cage and a skeletal spine, with sinew on the forearms and shins.
- **Wings:** the model's wings are dark oxblood leather. The sheet's wings are pale, stretched, blood-streaked skin.
- **Coat:** the model wears a full ankle-length coat. On the sheet only the collar, buckles and a shredded hem remain, and the legs are bare.
- **Colour:** the model is ≈85% dark red/black. The sheet is ≈60% grey-white skin with red accents. The redesign is effectively a new mesh, new textures and new wing materials; only the rig skeleton and the clips can carry over.

## Ranked top 5 for round 7

1. **The grab and the wing mesh (piece 4).**
   - Fix the membrane collapse first, then author a forward-pitched wing-wrap.
   - **Pass:** the H10 criteria above, solid membranes in H06–H10, H05 face visible with the soles hidden, and H01→H03 diffs ≥8.
2. **The candle rule (piece 3).** Bend the flame mesh, not the candle.
   - **Pass:** in S5, S1 and H06–H10, every flame within 8 m of the Count has its axis ≥20° off its stick toward him, measured on a 2× crop.
3. **The S5 hero shot (pieces 1 and 4).**
   - **Pass:**
     - Count ≥55% of frame height.
     - Low camera looking up.
     - Moons within ±300 px of his head.
     - Warm ≥2%.
     - No splice or striping inside the frame.
4. **Room focal dressing (pieces 1 and 3).**
   - **Pass:**
     - **S2:** three altar sockets visible, front std ≥8.
     - **S3:** ≥6 niches, a bridge and ≥3 vault rings, with the blood dark wine.
     - **S4:** a plank deck in rows ≥1080, a keep, moon clear of row 0, and ≥3 windows.
     - **S1:** a centred hero object as specified.
5. **Seams and crush cleanup (piece 2).**
   - **Pass:**
     - px<8 12–30% in S3 and S6.
     - No straight panel seam longer than one module in S4, S5 or S6.
     - 0 chip decals.
     - S1 pier courses continuous.
     - S6 wing tip ≥920.

## Distance from shippable

**Overall: 5.0/10** against the 8/10 bar, the same as r5. The cleanup gains, which are the integration of one stone family, the cylinders and the exposure band, are cancelled by the S1 subject loss, the S3 and S6 crush, the smaller S5 Count, and the broken wings on the climax frame.

**What the remaining 3 points consist of:**
- **The signature mechanics on screen (~30%).** The candle tell is 0° after four rounds, and the grab is a puppet pose, now with broken wings. Until these two land, the map has no identity, whatever the stone looks like.
- **Architectural density (~35%).** The refs' ribbed vaults, tracery, carved altar and sarcophagus, coffin niches, keep and bridge deck are still absent. The kit is now consistent but still bare boxes.
- **Hero composition (~20%).** S5 is under-framed, S1 has no subject, and S2 cannot see its own altar top.
- **Grade (~15%).** S3 and S6 are crushed, the crypt blood reads neon, and no room has the refs' layered floor mist with a cool/warm split.

**Path to 8/10:** at least three more rounds, plus the Count redesign, which will reset the creature-read scores. Round 7 must spend its budget on items 1–3 above, which are cheap, testable and four rounds overdue, before adding any more stone.
