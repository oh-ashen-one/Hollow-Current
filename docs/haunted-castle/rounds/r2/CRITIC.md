# Haunted Castle — round 2 blind critique

Critic: opus-critic, blind. I read only DESIGN.md, `rounds/r1/CRITIC.md`, `refs/*.png`, `rounds/r1/captures/*.png` and `rounds/r2/captures/*.png`. I did not read HANDOFF, NOTES, capture.json, handoffs or git history.
Method: all coordinates are full-resolution capture pixels (x, y) at 2560×1440. Luma is 0.299R+0.587G+0.114B. "Magenta" means R>200, B>200, G<80. "Pure white" means all channels >235. Crops were inspected at native resolution and brightened ×2–×3 where noted. Hunter distance assumes the 60° vertical FOV that S6 confirms: the 1.8 m figure's foot line at y1013 and the horizon at y≈582 give d≈4.7 m from both the size and the ground-plane calculations.

## Global measurements

| Capture | mean luma r1 → r2 | % px luma<8 r1 → r2 | min luma r2 | notes |
|---|---|---|---|---|
| S1 great hall | 20.9 → 52.3 | 36.7 → **0.0** | 17 | ref: mean 33.3, 15.6% <8 |
| S2 chapel | 10.9 → 44.3 | 52.8 → **0.0** | 17 | 80,428 pure-white px; one blob of 60,322 px over the altar |
| S3 crypt | 4.5 → 31.0 | 82.8 → **0.0** | 17 | ref: mean 17.6, 27.6% <8 |
| S4 exterior | 15.5 → 40.9 | 76.3 → **0.0** | — | |
| S5 perch | 11.2 → 35.4 | 61.2 → **0.0** | — | 37,634 pure-white px (flame cards) |
| S6 sheet | 3.9 → 33.3 | 86.7 → **0.0** | 17 | |
| S7 title | magenta 1,285,394 → **0** | | | |
| S8 ledger | magenta 2,615,356 → **0** | | | |
| H01–H10 | 14.9–3.4 → 38.4–33.6 | 0.0 in all | 17 | H03–H10 mean abs diff between frames 0.00–0.15; the only changing pixels are inside x0–85, y0–252 (a bobbing prop) |

**New global fault: lifted black point.** Every 3D capture has a hard luma floor of 17 (p1 = 17–18), and 0.0% of pixels fall below 8. The references have 15–28% of pixels below 8 and p1 = 0. Round 2 cleared the crush checks with a uniform lift, not with lighting. Every shot now reads milky, flat and evenly lit. Mean luma overshoots the refs by +19 (S1), +19 (S2) and +13 (S3). Warm-pixel share is 3.1% in S1 (ref 4.8%), 0.4% in S3 (ref 9.2%) and 2.7% in S5 (ref 9.1%). The whole castle is one blue-grey key, with no warm/cold split.

## 1. Round-1 pass checks

| # | Check | Result | Measurement |
|---|---|---|---|
| 1 | S7 magenta px = 0 | **PASS** | 0 |
| 2 | S8 magenta px = 0 | **PASS** | 0 |
| 3 | H frames show ≥3 distinct Count poses | **FAIL** | The Count is visible in **1 of 10** frames (H01, a black silhouette at x≈1460–1780, y≈180–420). H02–H10 contain no Count. Distinct poses = 1. |
| 4 | No identical consecutive md5s | PASS (by the letter only) | All 10 md5s differ, but H03–H10 differ by a mean abs diff of 0.00–0.15. Only 637–6,888 px change per frame, all inside x0–85, y0–252 (a floating prop bobbing at the frame edge). The camera and the hunt are frozen for 8 s. |
| 5 | Camera never inside the mesh | PASS | Closest approach: his screen height is ≈240 px (17%) in H01, which implies ≈11 m away. |
| 6 | One airborne frame with wings spread ≥2× shoulder width | **FAIL** | In H01 one wing trails behind, swept back, and his body intersects the gallery balustrade. He is not airborne and the wings are not spread. |
| 7 | He never fills >70% of frame height | PASS | Max 17%. |
| 8 | S1 skeleton spans ≥35% of frame width at y≈500–900 | **FAIL** | x≈912–1762 = 850 px = **33.2%**, y≈670–960. |
| 9 | S1 has ≥6 countable books | **FAIL** | **3** books, at ≈(975,425), (1350,525) and (1690,600). |
| 10 | S1 fog reads over the floor at y>1000 | **FAIL** | None. Floor rows y900–1440 have luma std 17–20 with no gradient or veil. |
| 11 | S1 floor shows texture | PASS | Tiled flagstone texture. |
| 12 | S1 floor shows reflections | **FAIL** | No specular, no rune reflection. |
| 13 | S2 has ≥30 lit floating candles | **FAIL** | ≈21 floating candles counted (±2), and **0 lit**. They are dark hexagonal prisms with no flame. The only white sliver is at x1562, y494–543. |
| 14 | S2 candle flames show in the upper half at 1280 px | **FAIL** | No flames. |
| 15 | S3 mean luma ≥14 | PASS (via the lift) | 31.0 |
| 16 | S3 px<8 ≤45% | PASS (via the lift) | 0.0% |
| 17 | S3 has ≥8 hand-roots at y<500 | PASS | ≈18 root strands. Stems run y≈180–500 and hands hang at y≈500–690. |
| 18 | S5 flames lean toward the Count | **FAIL** | The flames are pure-white rectangles (RGB 254,254,254, std ≈13) aligned to each candle's axis. No bend. |
| 19 | S5 wing span ≥2× shoulder width | PASS | Wing span ≈1010 px / shoulders ≈270 px = **3.7×**. |
| 20 | S5 hands touch the rail top | PASS | The right hand is draped over the coping at x≈1290–1350, y≈920–1060. The left claws pass *through* the rail (see S5). |
| 21 | S6 px<8 ≤60% | PASS (via the lift) | 0.0% |
| 22 | S6 wing fingers are continuous, run down and out, and none cross horizontally at the waist | PASS | The fingers run diagonally down and out at ≈40°. But the wings stop at the hip (see S6). |
| 23 | S4 px<8 ≤40% | PASS | 0.0% |
| 24 | S4 moat spans ≥40% of the width | PASS | It spans ≈100% of the width, in a band y≈1010–1215. Colour ≈RGB 50,25,42, a desaturated maroon. |
| 25 | S4 shows drawbridge planks in the bottom 25% | **FAIL** | The bottom 25% (y1080–1440) is grey flagstone quay. No planks, no chains. |
| 26 | S4 has spires breaking the skyline between the moons | **FAIL** | The skyline is a flat crenellated wall at y≈130–250. 0 spires. |
| 27 | S4 shows both moons unclipped (r1 secondary) | **FAIL** | The white moon's bbox is x145–796, y**0**–192: the frame top cuts it (115 moon px in row 0) and the wall cuts it below. The red moon (x1935–2150, y122–246) is cut by the battlements. |

**Tally: 15 PASS / 12 FAIL.** Four of the passes (15, 16, 21, 23) came from the black-floor lift, which damages the look; see "New global fault" above.

## 2. Scores (0–10) and deltas vs round 1

| Shot | Comp/layout | Silhouette/proportion | Materials | Lighting | Surreal dressing | Colour/atmos | Creature readability | Pose/anim | Scale vs 1.8 m |
|---|---|---|---|---|---|---|---|---|---|
| S1 great hall | 5 (+1) | 4 (+1) | 4 (+2) | 3 (+1) | 4 (+3) | 3 (+1) | — | — | — |
| S2 chapel altar | 4 (0) | 3 (0) | 4 (+2) | 3 (+1) | 2 (+2) | 2 (0) | — | — | — |
| S3 crypt | 4 (+2) | 3 (+2) | 3 (+2) | 3 (+2) | 4 (+3) | 2 (0) | — | — | — |
| S4 exterior sky | 2 (+1) | 2 (+1) | 3 (+2) | 3 (+2) | 4 (+1) | 4 (+1) | — | — | — |
| S5 lord perch | 6 (+3) | 6 (+3) | 5 (+2) | 4 (+2) | 2 (0) | 4 (+2) | 6 (+3) | 5 (+4) | 6 (+2) |
| S6 lord sheet | 5 (0) | 6 (+1) | 5 (0) | 4 (+1) | — | 4 (0) | 7 (+1) | 3 (+1) | 8 (+4) |
| H01–H10 hunt | 3 (0) | 3 (0) | 4 (+1) | 3 (+1) | 3 (+2) | 3 (+1) | 2 (−1) | 1 (0) | 2 (0) |
| S7 title | legibility 6 (+2) | castle identity 6 (+4) | | | | | | | |
| S8 journal | legibility 7 (+2) | castle identity 6 (+4) | | | | | | | |

Surreal counts (r2 capture vs reference):

| Shot | Capture | Reference |
|---|---|---|
| S1 | 1 skeleton, skull not visible; 3 books; ≈8 floating candelabras with white-card flames; rune ring with glyphs; 0 moons; 0 fog | skeleton with the skull to camera, ≈8 books, ≈8 candelabras, 2 moons, fog |
| S2 | ≈21 floating candles, 0 lit | 40+ lit |
| S3 | ≈18 hand-roots; sarcophagus with a 12-square pink emissive grid; channel is blue-grey | dozens of roots, rune strips, red glossy blood, fog |
| S4 | 2 moons (both clipped), aurora, stars, red moat; 0 spires, 0 bats, 0 drawbridge, 0 chains, 0 trees | all of these |
| S5 | 1 candelabra in the foreground and ≈4 behind, white cards, no bend; 2 books | ≈10 candelabras streaming toward him |

## 3. Progress pairs (r2 vs r1)

- **S1: better.** The skeleton, books, readable glyph ring, textured stone and sound columns are now present. It still has no fog, no warmth, white-card flames, and the skeleton is clipped through the table with its skull hidden.
- **S2: better, narrowly.** The candle field and textures now exist. But the candles are unlit, and the RGB 0 slab became a 60,322 px pure-white slab, so the missing material has swapped colour.
- **S3: better.** The roots are in frame and the room is readable. But the blood channel lost its red (mean RGB 19,23,33, was 12,2,3): a regression on the room's signature.
- **S5: much better.** He is large, perched on the rail with wings half-open, and the likeness reads. The candles around him are white rectangles and break the candle rule.
- **S6: better.** He is lit and at a measured scale of 1.20×, with the coat at floor length and the wings no longer snapped. The tree trunk is still behind him and the wings are short.

## 4. Specific measurements

- **Candle flames: solid white rectangles.** At native zoom, S1 flame cards are quads of 34–65 × 60–98 px (fill 0.71–0.91) at RGB 238,238,239. S5 foreground cards are 61–145 × 112–167 px at RGB 254,254,254. There is no orange core, no alpha falloff, no glow and no flicker shape. In S1 the largest is at x103–168, y34–132 and x470–517, y91–180. They read as paper cards on sticks, not flames.
- **Giant skeleton head: it does not face the hall.** The skull end is a flat wedge at the right end (x≈1740–1790, y≈870–910), pointing toward the +x side wall. The column at x≈1650–1720 hides most of it. In the reference the skull sits front-left and looks down the table toward the viewer. A leg bone also passes through the table top and a chair (x≈1080–1170, y≈800–960).
- **Floating books: they read as books.** In S1 they are open, pages up and tilted, with red covers. In S5 the book at x≈2240–2480, y≈700–1040 is a closed slab that reads as a block more than a book. There are only 3 in S1.
- **S4 drawbridge: not visible.** The bottom of the frame is flagstone quay. **Both moons are clipped** (see check 27).
- **Wing silhouette.** S5 is readable: both wings are half-raised with claw tips at y≈335. S6 is readable but short: wing top to lowest tip is ≈55% of body height, against ≈83% on the sheet. In H the wings are unreadable: H01 shows one wing as an unlit black smear (mean RGB 16,19,30) with no rim light, and he is absent from H02–H10.
- **Hunter height vs the 1.8 m figure (S6).** The figure spans y540–1013 = 473 px. The Count spans y≈432–1055 = 623 px, a raw ratio of 1.317. He stands nearer: his feet are at y1055 against 1013, with the horizon at y≈582. Depth-corrected ratio = 1.317 × (431/473) = **1.20** (≈2.16 m). Target 1.19. **PASS.**
- **Z-fighting, missing materials, leaks and seams:**
  - The S2 altar backdrop is an unlit pure-white slab, x1140–1420, y264–739 (60,322 px).
  - The S2 lancet windows hold 4 white quads at x790–933 and x1627–1770, y420–563. They are unlit and not glass.
  - The S4 window at x≈2015–2030, y≈585–645 has a pure-white light-leak sliver.
  - In S5 the left-hand claws clip through the balustrade at x≈1170–1210, y≈1000–1060.
  - In H01 the Count's body intersects the gallery rail.
  - In S1 the skeleton intersects the table.
  - I found no z-fighting flicker; this was checked on stills only.
- **Fog: absent** from S1–S6. The only "fog" is the H01/H02 billboard blob. It is ≈830×690 px in H01 (x827–1660, y747–1439) and ≈560×570 px in H02. This is the same smeared-billboard fault as r1.
- **Title and journal.** Magenta is gone. Castle identity is now real: moons, aurora, spires, red moat, torn parchment and a wax bat seal. Legibility faults remain:
  - S7: the unselected map tab is grey text on a grey smear, unreadable (x≈140–620, y≈890–950).
  - S7: "EXIT TO DESKTOP" collides with its Q keycap.
  - S7: "FIELD INVESTIGATION OFFICE" is grey on the moon (x≈130–540, y≈130–150).
  - S8: a red rule strikes through the title's ascenders at y≈377.
  - S8: a blood blot sits under the "BLOOD LEDGER" kicker, and the corner filigree overlaps the "B".

## 5. Per shot

### S1 great hall
**Biggest gap:** the hall has no atmosphere. It is evenly lit blue-grey with its black point lifted to 17, there is no fog, and its "flames" are white cards. Do three things. Restore the black point. Add a ground fog volume below 0.8 m. Replace the flame cards with a warm additive flame (core ≈255,210,140 fading to transparent) plus a small warm point light on each floating candelabra. **Pass:** ≥10% of px have luma <8, and min luma ≤4. Warm-pixel share is ≥5%. No pure-white blob ≥400 px with fill >0.5. Rows y1000–1300 show a visible veil, with floor texture contrast (std) ≤60% of the r2 value.
- Turn the skeleton so the skull sits front-left, facing the camera, clear of the columns. Scale it to ≥35% of the width, and stop its bones passing through the table and chairs at x≈1080–1170.
- Raise the book count from 3 to ≥6, between the columns at 2–5 m.
- The floor needs wet specular: the rune ring and candles should reflect.
- The rose window is blown pure white. Show a moon through it, with coloured glass.

### S2 chapel altar
**Biggest gap:** none of the ≈21 floating candles is lit, and the altar backdrop is a 60,322 px unlit white slab. Give every floating candle the same warm flame and glow as S1, raise the count to ≥30, and replace the slab (x1140–1420, y264–739) with a stained-glass material. **Pass:** ≥30 orange flame blobs (R>200, B<140) are countable. No pure-white blob >5,000 px.
- The lancets hold 4 unlit white quads (x790–933 and x1627–1770, y420–563). Remove them or make them glass.
- The altar top has no relic sockets. The reference shows 3: blade, cross-hilt and round moonstone.
- The foreground font rim (x≈400–1240, y≈1290–1440) is empty. Fill it with red liquid as in the reference, or move the camera.
- There are no banners, cobwebs, debris or moon shafts.

### S3 crypt
**Biggest gap:** the blood channel is now blue-grey (mean RGB 19,23,33 over x700–960, y800–1080). The crypt's signature red is gone. Give the channel a deep-red glossy liquid with specular from the candles. **Pass:** channel mean R ≥ 2× B, with a specular highlight ≥200 luma somewhere on the surface.
- The pink emissive grid on the sarcophagus (12 squares) reads as lit windows, not runes. Use carved vertical glyph strips as in the reference.
- There are no coffin niches, no bridge and no receding vault. The space still reads as a box with a trench.
- The hands are flat tan cutouts on straight dark sticks. Give the roots taper and twist, and give the hands volume.
- There is no fog, and warm-pixel share is 0.4% against 9.2% in the ref. Add candle pools.

### S4 exterior sky
**Biggest gap:** the camera still faces a flat crenellated wall from the quay. There is no drawbridge, there are no spires and both moons are clipped. Move the camera back onto the lowered drawbridge, ≥25 m from the curtain wall and aimed up ≈8°, and add a keep with ≥3 spires above the wall. **Pass:** planks and at least one chain show in the bottom 25%. ≥3 spires break the skyline between the moons. Neither moon touches the frame edge or any geometry, with each moon's bbox fully inside the sky.
- There is a white leak sliver at x≈2015–2030, y≈585–645.
- The wall is one repeating brick tile with no footing at the waterline.
- There are no bats, dead trees or mountains.
- The moat is a desaturated maroon (≈50,25,42) and should be a saturated, reflective red.

### S5 lord perch
**Biggest gap:** the candles around him break the candle rule: white rectangles with no bend. Use the warm flame shader, and bend each flame's tip toward the Count's head by ≥20°, with a smoke trail as in the reference. **Pass:** zero pure-white cards ≥400 px. Every flame within 8 m visibly leans toward him (the tip offset points at him in screen space).
- Put the rose window, with both moons, directly behind his head. Right now a column and a dark slot sit behind him.
- The left-hand claws clip through the balustrade at x≈1170–1210, y≈1000–1060.
- The pose is a man leaning on a balcony. The reference has him crouched *on* the rail with one knee up. Lift him onto the coping.
- The book at x≈2240–2480 is a closed block. Open it or shrink it.

### S6 lord sheet
**Biggest gap:** the wings are too short. Wing top to lowest tip is ≈55% of body height, against ≈83% on the sheet. The tips stop at the hip and should reach mid-shin. Lengthen the membrane and fingers. **Pass:** in S6 the lowest wing tip is at or below his knee line (y≥≈900 at this framing).
- The brown tree trunk is still behind him (x≈820–1020, y≈380–940). It was called out in r1. Remove it or replace it with a stone root.
- The arms are held out in an A-pose with spread claws, and no idle is evident. Capture him mid-idle with his arms at his sides as on the sheet.
- The likeness, coat length and scale are good. Scale is measured at 1.20× the 1.8 m figure, against a 1.19× target.

### H01–H10 hunt dive
**Biggest gap:** there is no hunt on camera. The Count appears in 1 of 10 frames, ≈11 m away as an unlit silhouette, and H03–H10 are a frozen camera (frame diff ≤0.15). Drive the capture from a real hunt. He leaves a perch, glides with both wings spread, and dives at the player. The camera tracks him. **Pass:** the Count is visible in ≥7 of 10 frames, in ≥3 distinct poses. ≥1 frame is airborne with both wings spread ≥2× shoulder width. His screen height rises across the sequence to ≥40% of the frame (≤3 m) without exceeding 70%. ≥5 consecutive frame pairs have mean abs diff ≥2.
- The H01/H02 fog-billboard blob is still there (≈830×690 px at x827–1660, y747–1439). Delete it or replace it with volumetric fog.
- He renders as an unlit black shape (mean RGB 16,19,30). Add a cold rim light from the moon or rose window.
- His body intersects the gallery balustrade in H01.
- The hall in H frames carries all the S1 faults.

### S7 title (no ref)
**Biggest gap:** the unselected map tab is unreadable: grey text on a grey smear at x≈140–620, y≈890–950. Render it as a muted parchment tab with dark ink, the same size and font as the selected one. **Pass:** the unselected tab label has a WCAG contrast ratio ≥4.5:1 against its own background.
- "EXIT TO DESKTOP" collides with its Q keycap. Add ≥12 px of gap.
- "FIELD INVESTIGATION OFFICE" is grey on the moon. Move it off the moon or add a scrim.
- The key art is flat vector silhouettes with scanline banding in the moat (y≈1240–1360). Use real castle key art, for example a still of the S4 view once it is fixed.
- The selected state is clear now. Good.

### S8 journal ledger (no ref)
**Biggest gap:** a red horizontal rule runs through the title's ascenders at y≈377, so the title reads struck out. Move the rule ≥16 px below the title's baseline. **Pass:** no rule pixel lies inside the title's text bbox.
- The "BLOOD LEDGER" kicker sits on a blood blot, and the corner filigree overlaps its "B". Move the kicker or the ornament.
- The heading is duplicated ("Ledger: the steward" and "BLOOD LEDGER / THE STEWARD"). Keep one.
- The body text is black print. The fiction says blood ink: use a dark oxblood ink with a handwritten face for the entries.
- The bottom ≈40% of the parchment is empty.

## Verdicts per piece

| Piece | r1 | r2 | Why |
|---|---|---|---|
| 1 Layout | APPROACHES | **APPROACHES** (=) | The hall reads as a hall and the crypt roots are in frame. S4 still has no approach or keep, and the crypt has no niches, bridge or vault. |
| 2 Art | FAILS | **APPROACHES** (↑) | Every surface is textured and nothing is crushed. But the black point is lifted to 17 everywhere, with 0 fog, no warm/cold split, two unlit white slabs or quads and the blood channel's red lost. |
| 3 Surreal props | FAILS | **FAILS** (=) | Skeleton, books and roots now exist. But 0 lit chapel candles, flames are white cards, no candle bend, 3 books against 6, and the skull is hidden and facing away. |
| 4 Count Claude | FAILS | **FAILS** (model + perch ↑ to APPROACHES; hunt still FAILS) | Scale (1.20×) and the perch pose pass, and the likeness is strong. The wings are short and the tree is still behind him. The hunt shows him in 1/10 frames with no dive. |
| 0 UI | FAILS | **APPROACHES** (↑) | Magenta is gone, and the Blood Ledger parchment and seal give the castle an identity. The unselected tab is illegible, the rule strikes through the title, and the key art is flat vector. |

## Ranked top 5 for round 3

1. **Count hunt on camera (piece 4).** He must perch, then glide, then dive, with the capture camera tracking him and no frozen frames. Pass: visible in ≥7/10 H frames, ≥3 poses, one airborne frame with both wings spread ≥2× shoulder width, screen height rising to ≥40% (≤3 m) and never >70%, and ≥5 frame pairs with mean abs diff ≥2. The H01 fog billboard must be gone.
2. **Real candle flames and the candle rule (pieces 2/3).** Use a warm additive flame material and light all chapel candles, ≥30 of them. Flames within 8 m bend toward the Count. Pass: 0 pure-white blobs ≥400 px with fill >0.5 in S1, S2 and S5. ≥30 orange flame blobs in S2. Every S5 flame tip offset points at him.
3. **Lighting and atmosphere (piece 2).** Restore the black point. Add ground fog and warm candle pools against cold moonlight. Replace the S2 white slab and lancet quads with glass. Pass, in S1, S2, S3 and S5: px<8 between 8% and 30%, min luma ≤4, warm-pixel share ≥5%, and no pure-white blob >5,000 px. In S1 rows y1000–1300 show a fog veil. In S3 the channel has mean R ≥2× B with a specular highlight.
4. **S4 approach (pieces 1/2).** Put the camera on the lowered drawbridge, with a keep and ≥3 spires between unclipped moons. Pass: planks and a chain in the bottom 25%, ≥3 spires breaking the skyline, both moon bboxes fully in open sky, and no white leak at x≈2015.
5. **Hall dressing and Count polish (pieces 3/4).** In S1 the skull faces the camera at front-left, the skeleton spans ≥35% of the width, ≥6 books are countable, and no bones intersect the table. In S6 the wing tips reach the knee line and the tree trunk is removed. Pass: all four measurable in the next S1 and S6.
