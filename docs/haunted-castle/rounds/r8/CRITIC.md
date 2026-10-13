# Haunted Castle: round 8 blind critique

**Critic:** opus-critic, working blind.

**What I read:** DESIGN.md (including "Count Claude — redesign"), `rounds/r7/CRITIC.md`, `refs/{great_hall,chapel_altar,crypt,exterior_sky,lord_perch,count_demon_sheet}.png`, `rounds/r7/captures/*.png` and `rounds/r8/captures/*.png`.

**What I did not read:** HANDOFF, NOTES, capture.json, the handoffs or git history.

**Method:** same as r7.
- Coordinates are full-resolution pixels (x, y) at 2560×1440.
- Luma is 0.299R + 0.587G + 0.114B.
- "Warm" means R > G+20, G > B and luma > 40.
- "Orange blob" means ≥20 px with R > 200 and B < 140.
- **Flame core:** blobs ≥150 px with R > 230, G > 120, B < 150; the axis is the principal axis, measured from screen vertical.
- **Streak:** an orange blob (R>170, G>90, B<110, R>G+40) with major axis ≥250 px and aspect >5, plus a visual check.
- **Demon colour:** the S6 body rectangle x 1350–1580, y 345–1120, with blue stone and floor px (B ≥ R−3) dropped. This rectangle still holds some wood-door px, so treat it as a lower bound on red. "Red-dominant" means G < 0.5R. "Grey-white" means R−B < 45 and luma > 90. "Pale", a lighting-independent measure, means G ≥ 0.75R and B ≥ 0.65R on px with luma > 20. The sheet was measured on its front view with the flat grey background removed.
- Every r7 number in the tables below was re-measured with the same script, so the deltas are like-for-like.

**S7 and S8 are unchanged.** Against r7 the mean abs diff is 0.000.

## Global measurements

| Capture | mean luma r7 → r8 | % px luma<8 r7 → r8 | warm % r7 → r8 | orange blobs r7 → r8 | change vs r7 (mean abs /255) | changed px (>30) |
|---|---|---|---|---|---|---|
| S1 great hall | 39.2 → 41.2 | 11.6 → 11.7 | 9.0 → **11.0** | 73 → 77 | 0.014 | 1.7% |
| S2 chapel | 27.3 → 27.1 | 20.6 → 20.8 | 13.0 → 12.9 | 37 → 38 | **0.003** | 0.3% |
| S3 crypt | 23.5 → 25.4 | 23.1 → 22.1 | 9.5 → 9.8 | 23 → 16 | 0.018 | 4.3% |
| S4 exterior | 30.2 → 30.8 | 6.2 → 5.0 | 0.4 → 0.4 | 1 → 2 | 0.006 | 0.7% |
| S5 perch | 27.0 → 25.8 | 15.1 → 14.2 | 2.9 → 7.7 | 10 → 11 | 0.058 | 19.5% |
| S6 sheet | 15.9 → 16.8 | 34.2 → **32.9** | 1.7 → 3.9 | 0 → 1 | 0.008 | 1.4% |
| H01–H10 | 25.6–34.2 → 26.8–35.4 | 14.4–20.5 → 14.9–21.4 | 8.9–14.6 → 10.1–18.6 | 42–61 → 50–66 | 0.018–0.069 | |

- **Hunt frame-pair diffs:** 4.3, 2.9, 14.4, 18.8, 25.5, 18.6, 12.4, 28.3 and 21.7. **H01–H03 are one frame for the fifth round running.**
- **Most of this round touched very few pixels.** S2 is effectively identical to r7, S4 changed 0.7% and S6 changed 1.4%. The only large change is S5, at 19.5%, and that is the candelabras plus the wing recolour.

## 1. Round-7 checks and top-5 items

### Top-5 item 1: the grab (H09/H10)

| Check | r8 | Measurement |
|---|---|---|
| Torso pitched ≥25° toward the lens | **FAIL** (=) | H10 head centre ≈(1325,430), pelvis ≈(1335,710): 2° from plumb. There is no foreshortening. Neck→pelvis is 200 px against 520 px for the legs, the same ratio as r7's frontal hang. The arms-up puppet pose is unchanged. |
| Membranes continuous, max gap ≤20 px | **HALF** | **Inner halves are fixed:** x≈850–1200 (left) and x≈1450–1900 (right) are now solid tattered panels, with holes ≤20 px. **Outer halves are still strands:** at x=650 the bands sit at y≈640–720 and y≈800–840, an **80 px gap**. At x=2100 the strands sit at y≈600–650, 700–760 and ≈880, with gaps of **50 and 120 px**. The left outer wing ends in a rope ("noodle") that curls down to (240,1000). |
| Tips at x<80 and x>2480 | **HALF** | Right: a strand reaches x=2560 at y≈930, PASS. Left: the tip is at **x≈235**, FAIL (r7 365). |
| One claw ≥200 px wide within 300 px of centre | **FAIL** (=) | Left hand x 1070–1215 (145 px), y 345–540. Right hand x 1430–1585 (155 px), y 340–530. Both are at shoulder height, about 310 px from centre. Nothing comes toward the lens. |
| Face visible | **PASS** | The H10 skull is at x≈1290–1360, y≈385–500. |
| H01→H03 diffs ≥8 | **FAIL** (=) | 4.3 and 2.9 |

### Top-5 item 2: the candle rule

| Check | r8 | Measurement |
|---|---|---|
| Flames attached to wicks (centroid within 1.5 flame-lengths) | **FAIL** (=) | The orange "petals" still float free of every candle. **H10:** petals at x≈711–1055, y≈1078–1122. The nearest wicks are the left floor candelabra at x≈22–245, y≥1320, **≥450 px away**. **H07 and H08:** petal pairs at x≈360–500, y≈1120–1160, while their candelabra wicks are at x≈60–180, y≈1040–1080 (≈300 px). There is a second cluster at x≈1510–1620, y≈1110–1150, against its candelabra at x≈1720–1840 (≈200 px). The chandelier flames are still a petal cloud around the Count (H05, H07–H10). |
| 0 orange streaks ≥250 px | **FAIL** (=) | **H05:** x 900–1265, y 982–1004 (365 px, detected). **H07:** a bar at x≈920–1550, y≈575–595 (visual). **H10:** x≈1455–1620, y≈900–940 (visual). |
| S5 ≥2 lit candelabras | **HALF** | Two candelabras are back: left x≈0–640, y≈830–1440 and right x≈1950–2560. **But 0 flame cores pass the core threshold.** What sits above the candles is defocused orange halos about 50 px across (x≈97–329, y≈1030–1064 and x≈2006–2275), not flames on wicks. Most candle tops (e.g. x≈300–330, y≈1110) have no flame at all. |
| Lean ≥25° toward him in S5 | **FAIL** | The only elongated flame blobs are vertical: (545,1060) at −1°, (47,938) at 4° and (126,906) at 3°. The rest are round halos with aspect ≈1. |
| Lean ≥25° in H05 pixels | **HALF** | 6 cores, median \|angle\| 42°, all ≥25°. But they are the detached petals, not flames on wicks, so the lean reads as debris, not as a tell. |
| Lean at S1 | **FAIL** (=) | 0 of 23 cores ≥25°, median 3°. The Count is not in S1, so the hall never shows the tell in a calm frame. |

### Top-5 item 3: demon material

| Check | r8 | Measurement |
|---|---|---|
| Red-dominant ≤40% (sheet ≈25%) | **FAIL**, improved | S6 body rect **54.3%** (r7 62.0%). S6 torso 53.7% (r7 64.7%). H08 torso 63.5% (r7 70.8%). H10 torso 59.5% (r7 67.1%). On the sheet's front view the figure is 25.0%. |
| Grey-white ≥10–12% | **FAIL** (=) | S6 **0.7%** (r7 0.3%); sheet 15.6%. The lighting-independent "pale" measure gives S6 2.6% against **61.4%** on the sheet. |
| What changed | | The **wings** went from red-brown to brown-black at S6 ((34,28,21), (51,43,41)) and tan in the hunt ((106,74,52), (134,90,62)). The **body** is still rimmed in saturated red, a fresnel-like red outline along every silhouette edge at S5, S6 and H08–H10. The sheet's pale-skin / raw-red / black-coat contrast is still absent. |
| Ember in each socket at S6 (R>180 cluster) | **FAIL** (=) | 0 clusters in the S6, H09 and H10 skull boxes. In H08 the only hits, at (1198,588), lie outside the skull. |
| Teeth resolvable at S6 | **FAIL** (=) | The skull is ≈70×130 px at x≈1405–1475, y≈350–480. A dark vertical gash reads, but there are no teeth and the sockets are brown smudges. |
| Jaw/teeth/embers at H08–H10 | **HALF** | The skull dome and a dark vertical mouth gash read at ≈110 px skull height (H08 x≈1215–1285, y≈530–640; H10 x≈1290–1360, y≈385–500). There are no teeth and no embers. **Artefact:** a flat, unlit tan triangle sits at the throat in S6 (1455,453), H08 (1247,660) and H10 (1330,502). It was already in r7 S6, and it reads as a mesh hole. |

### Top-5 item 4: set pieces

| Check | r8 | Measurement |
|---|---|---|
| S1 hero ≥25% width, centred | **FAIL** | The ring chandelier is now ≈x 1120–1470: **≈14%** of width (r7 7.5%). There is still no skeleton or laid table as the subject. |
| S2 recessed sockets + carved front std ≥8 | **FAIL** (=) | S2 changed 0.3% of px. The raised plates and the black box front are both unchanged. |
| S3 ≥6 niches | **FAIL** (=) | 0 |
| S3 arched bridge | **FAIL** (=) | Unchanged flat ramp |
| S3 ≥3 vault rings | **HALF** | One new brick-faced rib spans the full width at y≈120–230 (≥4% of frame height, PASS on size). A faint second rib is at y≈340–360. Both are 3-segment polylines, not round arches. 2 rings < 3. |
| S4 keep ≥2× wall height | **HALF** | A crenellated square keep replaced the centre spire: body x≈1193–1366, y≈585–795, with its spire to y≈495. That is 210 px (14.6% of frame height, PASS on ≥4%). But the wall is ≈470 px tall (795 → 1265), so even with its base hidden the keep is ≈1.45× the wall. |
| S4 plank deck in rows ≥1080 | **FAIL** (=) | Rows 1290–1440 have luma 20.8 and std 12.4, the same dark tile as r7. |
| S4 ≥3 lit windows | **FAIL** (=) | 2 |
| S4 wall luma ≥18 | **PASS** | 19.5 over the r7 box. Excluding the door it is **18.2** (r7 15.0). The door itself rose 15.1 → 30.9 and its planks now read. |

### Top-5 item 5: grade and cleanup

| Check | r8 | Measurement |
|---|---|---|
| Exposure px<8 in 12–30% | **FAIL** (8/10) | Pass: S2 20.8, S3 22.1, S5 14.2, H06 17.4, H07 15.2, H08 14.9, H09 17.1, H10 17.8. Fail: **S1 11.7** and **S6 32.9**. |
| S1 warm 3–8% and S3 warm ≤8% | **FAIL**, worse | S1 **11.0** (r7 9.0); S3 9.8 (r7 9.5) |
| S6 swatch panel gone | **FAIL** (=) | The lighter mortar panel is still at x≈615–1062, y≈220–930. |
| S5 banner striping p90 ≤3 | **N/A** | The banners are now hidden behind the candelabras. |
| S1 fog std ≤12 | **FAIL** (=) | 21.9 (r7 22.2, same box x 600–1960, y 900–1400) |

### Carry-over r7 checks

| Check | r8 | Measurement |
|---|---|---|
| S5 both moons within ±300 px of the skull | **FAIL** (=) | Skull ≈(1290,870). White moon (1054,477): dy 393. Red moon (1488,547): dy 323. The camera is unchanged. |
| S5 fill ≥55% | **PASS** (=) | ≈68% |
| S2 ≥30 orange flames | **PASS** | 38 |
| S2 font specular | **FAIL** (=) | Unchanged |
| S3 cube-palm hands replaced | **FAIL** (=) | Unchanged at x≈25–330, y≈780–1100 and x≈2190–2560, y≈580–870 |
| S3 ceiling blotches removed | **HALF** | The new rib covers part of the blotch band. The blotches remain at x≈0–1300, y≈0–110. |
| S4 seam x≈978 | **FAIL** (=) | Unchanged |

**Tally of round-8 targets: 2 PASS, 10 HALF, 20 FAIL, 1 N/A.**
- **Real wins:**
  - The inner wing membranes are solid in the grab.
  - The wings are no longer red.
  - The S4 keep exists.
  - The S4 wall luma is in band.
  - One S3 rib.
  - The S5 candelabras are back as objects.
  - A ≈8-point drop in red-dominant.
- **Every other target missed:**
  - Grab pitch and claws.
  - Flames on wicks.
  - Streaks.
  - S5 lean.
  - Embers and teeth.
  - Pale skin.
  - Niches.
  - S2.
  - S1 hero.
  - Deck.
  - Windows.
  - S1/S6 exposure.
  - The swatch.
  - H01–H03.

## 2. Scores (0–10), delta vs r7

| Shot | Comp | Silhouette | Materials | Lighting | Surreal | Colour/atmos | Creature read | Anim/behaviour | Scale |
|---|---|---|---|---|---|---|---|---|---|
| S1 great hall | 4 (0) | 5 (0) | 5 (0) | 6 (0) | 4 (0) | **4 (−1)** | — | — | — |
| S2 chapel | 5 (0) | 4 (0) | 5 (0) | 5 (0) | 4 (0) | 6 (0) | — | — | — |
| S3 crypt | 4 (0) | **4 (+1)** | 3 (0) | 5 (0) | 4 (0) | 5 (0) | — | — | — |
| S4 exterior | 5 (0) | **5 (+1)** | 4 (0) | **4 (+1)** | 4 (0) | 4 (0) | — | — | — |
| S5 perch | 6 (0) | 6 (0) | 5 (0) | 5 (0) | **4 (+1)** | 5 (0) | 6 (0) | 5 (0) | 6 (0) |
| S6 sheet | 5 (0) | 7 (0) | **6 (+1)** | 4 (0) | — | 4 (0) | **6 (+1)** | 6 (0) | 7 (0) |
| H01–H10 | 5 (0) | **6 (+1)** | 5 (0) | 6 (0) | 3 (0) | 5 (0) | 6 (0) | 4 (0) | 6 (0) |
| S7 / S8 | unchanged | | | | | | | | |

## 3. Regressions with coordinates

1. **S1 warm** 9.0 → 11.0%, further out of the 3–8 band. The only S1 edits are on the left gallery candelabra (x≈350–650, y≈220–640) and the chandelier (x≈1348–1450, y≈390–506).
2. **S5 flame cores are gone.** r7 had no candles at all, so this is a new defect, not a lost feature: the candelabras came back with blurred halos and no flame cores (section 1, item 2). At hero-shot size this reads as a render bug.
3. **Hunt warm** rose to 17.9 / 18.6% in H09 and H10 (r7 14.3 / 14.6). It is driven by more petal and halo px around the Count.
4. **Left grab wing tip** ends as a curled rope at (240,1000). The r7 strands were thinner but straighter. The rope is a new shape artefact.

## 4. Per shot: biggest gap and secondary issues

### S1 great hall
**Biggest gap:** still no subject. The chandelier is ≈14% wide.

**Pass:** one object ≥25% of width, centred in x≈900–1660, with ≥15% of its px at luma >60.

Secondary issues:
- Warm 11.0 → ≤8.
- px<8 11.7 → ≥12.
- Fog std 21.9 → ≤12.
- No vault ribs or bosses.

### S2 chapel altar
**Biggest gap:** untouched this round. The altar is a black box with raised plates.

**Pass:**
- Three recessed sockets (slot, cross, round) at x≈1060–1500, y≈540–620, with nothing occluding them.
- Front local std ≥8.

Secondary issues:
- Flames splayed radially, not toward the Count.
- No specular on the font blood.
- Empty window niche.

### S3 crypt
**Biggest gap:** 0 niches and no arched bridge, for the sixth round.

**Pass:**
- ≥6 coffin niches.
- A round arched bridge with the channel visible beneath it.
- ≥3 rings.

Secondary issues:
- Cube-palm hands.
- Polyline rib.
- Warm 9.8 → ≤8.
- Ceiling blotches at y≈0–110.

### S4 exterior
**Biggest gap:** no plank deck. The foreground rows 1290–1440 are flat dark tile.

**Pass:** a visible plank deck with board seams in rows ≥1080, leading to the door.

Secondary issues:
- Keep 1.45× → ≥2× wall height.
- 2 → ≥3 lit windows.
- Seam at x≈978.
- Chains run to the bank.

### S5 lord perch
**Biggest gap:** the candles have no flames on their wicks and no lean, so the tell is still absent from the signature shot.

**Pass:** ≥6 flame cores (R>230 cluster ≥150 px) seated on candle tops, each leaning ≥20° toward the skull.

Secondary issues:
- Moons dy 393 / 323 → ≤300.
- The limb hoop exits the bottom edge.
- The red rim outline on the body.

### S6 lord sheet
**Biggest gap:** the body is still a red mass (54% red-dominant, 0.7% grey-white).

**Pass:**
- Grey-white ≥10%.
- Red-dominant ≤40%.
- An ember in each socket.

Secondary issues:
- px<8 32.9 → ≤30.
- Swatch panel.
- Throat chip at (1455,453).
- No teeth.

### H01–H10 hunt
**Biggest gap:** the grab is still a plumb arms-up hang, and the outer wings are strands.

**Pass:**
- H10 torso pitched ≥25°, with the head and shoulders foreshortened toward the lens.
- Outer-wing gaps ≤20 px.
- Left tip x<80.
- A claw ≥200 px wide.

Secondary issues:
- H01–H03 duplicates.
- Petals and streaks (H05, H07, H10).
- Skull unreadable under 60 px (H01–H07).
- Throat chip.

## 5. Verdicts per piece

| Piece | r7 | r8 | Why |
|---|---|---|---|
| 1 Layout | APPROACHES | **APPROACHES** (=) | Keep and rib added. Niches, bridge, deck and S1 subject are still missing. |
| 2 Art / architecture | APPROACHES | **APPROACHES** (=) | The S4 wall is in band. S1 warm got worse, and S6 exposure and the swatch remain. |
| 3 Surreal props | FAILS | **FAILS** (=) | The flame fix did not land: petals are still detached, streaks persist and S5 has halos without cores. |
| 4 Count | FAILS | **FAILS** (↑ wings) | Inner membranes and wing colour improved. Pose, claws, red body, eyes and teeth are unchanged. |
| 0 UI | APPROACHES | **APPROACHES** (=) | Untouched |

## 6. The demon vs count_demon_sheet.png

| Axis | r7 | r8 | Evidence |
|---|---|---|---|
| Silhouette and proportions | 7 | 7 | Unchanged mesh |
| Skull and split jaw at S6 | 5 | 5 | The gash reads. There are no teeth and no embers. |
| Skull at hunt distances | 3 | 3 | Only a dome reads below ≈60 px. |
| Flayed chest, blood and texture | 4 | **4.5** | Red-dominant 62 → 54% (sheet 25). Pale 2.6% (sheet 61). Red rim outline. |
| Wing membrane, idle and folded | 6 | **6.5** | No longer red, but dark brown/tan, not the sheet's pale stained skin. |
| Wing membrane, grab | 2 | **4** | Solid inner panels; strand and rope outer halves. |
| Scale vs the 1.8 m figure | 7 | 7 | Unchanged, ≈2.41 m |
| Scarier than the aristocrat (H08–H10) | 6 | 6 | H08 is good. H09 and H10 are still the hanging effigy. |

**Overall model fidelity: ≈50–55% of the sheet** (r7: 45–50%).

**Still missing:**
- Pale grey-white skin (the largest single gap).
- Ember eyes.
- Needle teeth and a visibly split jaw.
- Pale wing membranes.
- Black coat remnants (collar and buckles are lost in the red).
- Fingertips below the knee (66% against 73%).

## Ranked top 5 for round 9

1. **Flames: a rebuild, not a tweak (piece 3).** Parent every flame to its wick transform and rotate it about the wick base, with zero translation. Delete the petal mesh path. Give the S5 candelabras real flame cores and drop the DoF on them, or move them into the focus plane.
   - **Pass:**
     - Every flame centroid within 1.5 flame-lengths of its wick in H05–H10 and S5.
     - 0 streaks ≥250 px.
     - S5 ≥6 cores leaning ≥20° toward the skull.
     - S1 warm ≤8.
2. **Body material (piece 4).** Remove the red rim/fresnel term, and set base albedo so the skin regions are pale.
   - **Pass (S6):**
     - Grey-white ≥10% and pale ≥30%.
     - Red-dominant ≤40%.
     - An ember cluster in each socket.
     - Teeth resolvable.
     - Throat chip gone.
3. **Grab pose (piece 4).** Pitch the torso, bring one claw at the lens, and pin the outer membrane to the finger bones.
   - **Pass:**
     - H10 torso ≥25°.
     - Claw ≥200 px.
     - Outer gaps ≤20 px.
     - Left tip x<80.
     - H01→H03 diffs ≥8.
4. **Set pieces (pieces 1 and 3).**
   - **Pass:**
     - S3: ≥6 niches and a round arched bridge.
     - S4: a plank deck and ≥3 windows.
     - S2: recessed sockets.
     - S1: a ≥25%-wide hero.
5. **Grade.**
   - **Pass:**
     - S1 px<8 ≥12.
     - S6 px<8 ≤30.
     - S6 swatch gone.
     - S1 fog std ≤12.

## Distance from shippable

**Overall: 5.4/10** against the 8/10 bar (r7 5.3).

Round 8 targeted five items and moved one materially (the inner grab membranes). It moved two partially (wing colour, keep/rib). It missed the rest, including the flame fix that was ranked cheapest. S2 did not change at all.

## Hands-on playtest now, or more visual rounds?

**Recommend the owner playtest now, in parallel with round 9, not after it.**

**Visual rounds have hit diminishing returns.**
- Rounds 6→7→8 moved the score 5.0 → 5.3 → 5.4.
- This round changed under 2% of pixels in four of the six still shots.
- The remaining 2.6 points are known, enumerated and testable. None of them depends on what a human would learn by playing.

**The gameplay loop is the larger unmeasured risk, and no capture can retire it.** It has never been played by a human:
- Evidence tools.
- Blood Ledger.
- Chapel bell hunt.
- Relic hunt.
- Sword fight with three hits.
- Drawbridge escape.

A broken step there, such as an unreachable relic, an unreadable hunt or a fight that does not land, would invalidate visual work rather than the other way round.

**Two caveats:**
- **The candle tell is a gameplay signal.** The design calls it "the tell before he is visible". In its current detached-petal form the player cannot read the Count's direction from it. The playtest should explicitly note whether players notice it.
- **These captures do not prove a build runs or that either win path completes.** A scripted smoke run of both endings (escape and kill) should precede the owner's session, so his time is not spent finding a blocker a script could find.
