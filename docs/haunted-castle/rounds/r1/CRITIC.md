# Haunted Castle — round 1 blind critique

Critic: opus-critic, blind. I read only DESIGN.md, `refs/*.png` and `rounds/r1/captures/*.png`. I did not read HANDOFF, NOTES, capture.json or git history.
Method: captures at 2560×1440, refs at 2688×1520 and 2688×1152. Crops were brightened ×2–×6 for inspection. Luma stats are per-pixel (0–255). Magenta pixels are R>200, B>200, G<80. All coordinates are full-resolution capture pixels (x, y).

## Global measurements

| Capture | mean luma | % px luma<8 | notes |
|---|---|---|---|
| S1 great hall | 20.8 | 36.1% | |
| S2 chapel | 10.9 | 50.9% | slab at x1207–1352, y470–715 is exactly RGB 0,0,0 |
| S3 crypt | 4.4 | **81.3%** | |
| S4 exterior | 15.5 | **76.3%** | rows y330–1170: 97.9% of px luma<6 (black wall) |
| S5 perch | 11.2 | 59.8% | |
| S6 sheet | 3.8 | **86.6%** | |
| S7 title | 54.7 | 8.9% | **magenta 1,285,394 px (34.9%)**, bbox x43–1228, y165–1319 |
| S8 ledger | 78.3 | 15.9% | **magenta 2,615,356 px (70.9%)**, bbox x99–2460, y198–1325 |
| H05–H10 | 3.4–3.5 | 87.7–87.9% | **H07, H08, H09 and H10 are byte-identical (md5 ebbf439f…)**: the hunt froze for 4 s |

Hunter screen height, head top to feet, as a % of 1440: S5 ≈16% (y≈625–850, feet hidden by rail). S6 ≈43% (y≈392–1015). H01 ≈15%. H02 ≈21% (y≈630–940). H03 ≈40% (y≈530–1110). H04 is >100% (chest fills the frame, head out of frame). In H05–H10 the camera is inside or against his collar, so height cannot be measured.

## Score table (0–10)

| Shot | Comp/layout | Silhouette/proportion | Materials | Lighting | Surreal dressing | Colour/atmos | Creature readability | Pose/anim | Scale vs 1.8 m |
|---|---|---|---|---|---|---|---|---|---|
| S1 great hall | 4 | 3 | 2 | 2 | 1 | 2 | — | — | — |
| S2 chapel altar | 4 | 3 | 2 | 2 | 0 | 2 | — | — | — |
| S3 crypt | 2 | 1 | 1 | 1 | 1 | 2 | — | — | — |
| S4 exterior sky | 1 | 1 | 1 | 1 | 3 | 3 | — | — | — |
| S5 lord perch | 3 | 3 | 3 | 2 | 2 | 2 | 3 | 1 | 4 |
| S6 lord sheet | 5 | 5 | 5 | 3 | — | 4 | 6 | 2 | 4 |
| H01–H10 hunt | 3 | 3 | 3 | 2 | 1 | 2 | 3 | 1 | 2 |
| S7 title | legibility 4 | castle identity 2 | | | | | | | |
| S8 journal | legibility 5 | castle identity 2 | | | | | | | |

Surreal element counts (capture vs reference):

| Shot | Capture | Reference |
|---|---|---|
| S1 | 0 floating books, 2 floating candelabras (≈10 candles), 0 skeleton, rune ring as 2 plain blue lines with no legible glyphs, 0 visible moons, 0 fog | ≈8 books, ≈8 candelabras, skeleton, glyph ring, 2 moons, ground fog |
| S2 | 0 floating candles | 40+ floating candles |
| S3 | hand-roots off-frame (only their shadows on the floor at x2200–2560, y940–1080), 1 flat red channel, 0 fog | dozens of hand-roots, glowing runes, fog, reflective blood |
| S4 | 2 moons, aurora, big stars; 0 bats, 0 moat, 0 castle silhouette, 0 drawbridge | all of these present |
| S5 | 3 floating candelabras with flames vertical (no bend), 1 flat purple bat decal (≈x400–500, y280–330) | ≈10 candelabras with flames streaming toward the Count |

## Per shot

### S1 great hall ↔ great_hall
**Biggest gap:** the hall has no giant skeleton, no floating books and no fog, so it reads as an empty grey box. Place the skeleton mesh draped behind the table along the hall axis, rising above the gallery rail line. Add at least 6 bobbing books between the columns at 2–5 m. Add a ground-fog layer below 0.6 m. **Verify:** in the next S1 the skeleton spans at least 35% of frame width at y≈500–900, at least 6 books are countable, and fog reads over the floor at y>1000.
- The floor and walls are untextured flat albedo with no stone, no wetness and no reflection. The reference floor is wet flagstone with rune reflections.
- The rune ring is two thin blue circles. It needs carved glyphs that glow; none are legible at y1000–1440.
- The near-left column (x≈287–447, y≈450–1150) is split into two misaligned shells, with a dark inner face showing between them. The same fault is on the right column at x≈2140–2300.
- The rose window is a square quad with a dark tracery decal (x≈1170–1390, y≈310–490). No moon shows through it, and its square edge is visible against the black wall.

### S2 chapel ↔ chapel_altar
**Biggest gap:** the chapel has none of the floating candles; the reference has 40+. Add a field of at least 30 instanced candles bobbing at 1.5–6 m between altar and pews, with lit flames. **Verify:** at least 30 lit candles are countable in S2, and the 1280 px downscale shows candle flames in the upper half of the frame.
- The rectangle above the altar (x1207–1352, y470–715) is exactly RGB 0,0,0. It is an unlit or missing material (stained glass or relic recess?). Give it a real material or remove it.
- The altar is a stack of black boxes with no relic recesses or engraving. The reference has 3 visible sockets: blade, hilt and moonstone.
- The walls are flat plaster. There is no stained glass, no banners, no cobwebs or debris, and no cold moon shafts.
- The cold/warm split is absent: 50.9% of pixels are near-black and the rest is uniform grey-brown.

### S3 crypt ↔ crypt
**Biggest gap:** the shot is crushed to black (mean luma 4.4, 81.3% of px <8), and the hand-roots are outside the frame. Move the hand-root cluster into the top third of the camera view, add purple rune emission on the sarcophagus and lift ambient or fog light. **Verify:** S3 mean luma ≥14, px<8 ≤45%, and at least 8 hand-roots visible at y<500.
- The blood channel is an unlit flat red plane (mean RGB 12,2,3, std 3.7 over x720–1000, y820–1080), with no specular and no reflection. It needs a glossy liquid material.
- The sarcophagus is untextured stacked boxes. It needs a lid, carved panels and the angel relief.
- There are no coffin niches, no bridge and no receding vault, so the space reads as a room with a trough.
- Radial banding is visible on the floor and ceiling after brightening, from the light falloff (8-bit banding). Add dithering or raise the HDR precision.

### S4 exterior ↔ exterior_sky
**Biggest gap:** the camera faces a black curtain wall. Rows y330–1170 are 97.9% luma<6, so the drawbridge, red moat and castle silhouette are invisible. Reposition the S4 camera to stand on the drawbridge looking at the castle across the moat, and add a rim/moon light on the masonry. **Verify:** the next S4 shows drawbridge planks in the bottom 25%, red moat reflection across at least 40% of the width, and spires breaking the skyline between the moons. Px<8 must be ≤40%.
- The moons are cut off by the wall top (y≈320–370). Both moons should sit fully in the sky.
- There are no bats, no chains, no dead trees and no mountains.
- The ground is a flat purple-grey plane (y>1170) with no moat.
- The gate interior is visible through the door, which is fine. The flanking obelisks (x≈190–430 and x≈2130–2370) are untextured.

### S5 lord perch ↔ lord_perch
**Biggest gap:** the Count stands upright in rest pose, small (≈16% of frame), with wings folded behind him. He is not crouched over the rail with wings spread. Implement the Perch-Idle pose (crouched, claws on the rail, wings half-open), and frame S5 from below so he fills at least 45% of the frame height. **Verify:** in S5 the wing span is at least 2× his shoulder width, and his hands visibly touch the rail top.
- The floating candelabras are floor-stand meshes with feet and flanges hanging in the air (x≈240–470, y≈770–1000; x≈2180–2420, y≈900–1300). They read as misplaced furniture, not floating candles. The flames do not bend toward him, which breaks the design's candle rule.
- The bat is a flat unlit purple decal stuck on the wall.
- A bright white unlit sliver behind his right side (x≈1350, y≈720–780) is a light leak through a wall seam.
- There is no rose window behind him to frame his silhouette. The rose window is off to the right (x≈1750–2070).

### S6 lord sheet ↔ lord_sheet
**Biggest gap:** the wing skinning is broken. The lower wing fingers bend horizontally outward like snapped spider legs (x≈1000–1180 and x≈1380–1580, y≈700–800). Fix the wing bone weights and rest orientation so the fingers run down and out as on the sheet. **Verify:** in S6 the wing fingers are continuous, straight-segmented and trailing toward the floor, with no segment crossing horizontally at waist height.
- He is in rest A-pose with clenched fists; no idle animation is playing.
- A brown low-poly tree trunk with procedural wood grain (x≈920–1240, y≈330–1030) stands inside the castle behind him. It is out of style, and if it is a hand-root it does not read as one.
- The likeness is the strongest piece this round: face, collar, buckles and clawed feet match the sheet. The coat hem is short of the sheet's floor-length drape.
- The frame is lit only by the flashlight disc (86.6% px<8), so his silhouette and wings vanish at the edges.

### H01–H10 hunt dive
**Biggest gap:** there is no dive. Across H01–H03 he walks on the floor in the same rest pose, H04–H06 clip the camera into his chest, and H07–H10 are byte-identical frozen frames. Drive Animator modes 1 (Glide) and 3 (Dive) from a perch with spread wings, set the grab camera to stop ≥0.6 m from the mesh, and keep simulating during the grab. **Verify:** H-frames show at least 3 distinct poses, at least one frame has wings spread ≥2× shoulder width while airborne, and no two consecutive frames share an md5. He never fills more than 70% of frame height except in a deliberate wing-wrap.
- H01–H02 have an opaque grey-blue blurry blob ≈1100×800 px in the frame centre (x≈720–1840, y≈600–1400). It is a smeared billboard, not fog, and it hides the arena.
- In H05 the collar texture is visibly low-res and stretched when close. A teal sky sliver shows through a geometry gap (x≈760–800, y≈570–610).
- The dining hall and great hall still show the S1 faults: no skeleton, no books, no fog.
- Scale: he is ≈2.0 m against the gallery rail in S5, which is plausible, but no frame shows him next to a 1.8 m reference, so scale is unproven.

### S7 title (no ref)
**Biggest gap:** the menu panel renders as a solid magenta missing-shader fill: 1,285,394 px, bbox x43–1228, y165–1319. Assign a valid UI material or shader to the panel. **Verify:** magenta px = 0 in S7.
- The background is the Bellwether house photo. When Haunted Castle is selected, swap it to castle key art (moons and moat).
- The white text on magenta (BEGIN INVESTIGATION, the "Haunted Castle" tab) has low contrast, though the black serif text is legible.
- The selected-map state is only a thin underline. Make it unmistakable.

### S8 journal ledger (no ref)
**Biggest gap:** the same magenta fill covers 70.9% of the frame. Fix it with the S7 shader fix. **Verify:** magenta px = 0 in S8.
- There is no Blood Ledger art (vellum, blood ink, seal); it is the Bellwether journal layout with new strings.
- The background is again the Bellwether house.
- The copy itself is good and legible.

## Verdicts per piece

| Piece | Verdict | Why |
|---|---|---|
| 0 UI / map plumbing | **FAILS** | Magenta panels on both UI screens. The castle identity exists only in text. |
| 1 Layout | **APPROACHES** | The hall has its aisle, galleries, rose window and table. The chapel has pews, altar and lancets, and the crypt and gatehouse exist. But S4 framing shows no approach, and the crypt has no niches or vault. |
| 2 Art | **FAILS** | Flat untextured surfaces, crushed blacks (S3, S4, S6 at 76–87% near-black), no fog, a black slab, split columns. |
| 3 Surreal props | **FAILS** | 0 skeleton, 0 books, 0 chapel candles. Flames do not bend. The hand-roots are off-frame and the bat is a decal. |
| 4 Count Claude | **FAILS** (model approaches, animation fails) | The likeness is good, but the wing skinning is broken, rest pose shows in every frame, there is no perch, glide or dive, the camera clips into him and the hunt froze. |

## Round 2 fix list (ranked)

1. **UI magenta shader (S7, S8).** Fix the panel material. Pass when magenta px = 0 in both shots.
2. **Count animation and rig.** Fix the wing weights. Play Perch-Idle, Glide and Dive with spread wings. Stop the grab camera clipping into him and the hunt freezing. Pass when H-frames have ≥3 distinct poses, no identical consecutive md5s and no camera inside the mesh.
3. **Surreal set-dressing.** Add the giant skeleton plus ≥6 books in S1, ≥30 floating candles in S2 and ≥8 hand-roots in frame in S3. Flames must lean toward the Count in S5. Pass when those counts hold in the next captures.
4. **Lighting and materials.** Add fog and a cold-moon/warm-candle split. Give the floors, walls and altar stone textures, make the blood channel glossy, remove the RGB 0 slab and fix the split columns. Pass when S3 has px<8 ≤45% and S6 ≤60%, and the S1 floor shows texture and reflections.
5. **S4 exterior composition.** Put the camera on the drawbridge facing the castle across the red moat with spires between the moons. Pass when S4 px<8 ≤40% and the moat spans ≥40% of the width.
