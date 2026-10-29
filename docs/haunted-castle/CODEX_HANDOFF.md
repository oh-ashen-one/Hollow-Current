# Codex handoff: wall art for the Haunted Castle (and fallback prompts)

Author: piece T2 `hc/wallart` (Sonnet 5.5), 2026-10-14, measured from `origin/haunted-castle` `97c02ce`. Nothing in this file was generated; it is prompts, specs and a checker. Read `docs/haunted-castle/LOOP.md` first (own branch/worktree, never `main`, no Unity launches, report under 300 words).

## 0. What is wrong, in one paragraph

All seven painted canvases in the castle (six wall portraits plus the portrait-door leaf) use ONE material, `Castle_Canvas`, whose only texture is `Assets/HollowCurrent/Resources/Castle/Textures/Portrait.png`: a 512x704 procedural flat-colour figure with a blank grey face (written by `tools/castle_textures.py::portrait`). That is the "empty frame" the owner saw: seven identical placeholders. The three Blood Ledger pages (note screen) have no image at all, and the castle's latent claw-scores decal falls back to a human handprint. Bellwether's frames are fine (real photograph, see 1C). The six portraits' animated pupils (`PortraitEyes`) are defined but **never attached** in the castle (`SurrealProps.MakePortraitEyes` has no caller), so even a good painting would not follow the player. Distinct paintings per frame therefore need one small C# change; it is written, compile-checked and waiting as `docs/haunted-castle/wallart_runtime.patch` (section 2). Image files can be produced and committed independently of it.

## 1. Inventory

World positions are CastleWorld root-local metres (`Prop(Art.Portrait, pos, yaw)` places the frame origin = bottom centre of the frame; the canvas centre is 0.65 m above it). Yaw: 0 faces +Z, 90 faces +X, -90 faces -X. Floor of the Portrait hall is y 3.2, so frame origin y = 4.2 and canvas centre y = 4.85.

### 1A. Castle wall pictures (7 canvases, all currently the shared placeholder)

| id | room / wall | canvas centre (x, y, z) and yaw | mesh / slot | canvas size (m) and aspect | UV rectangle | current texture | file to supply (picked up by `Paint()` after the patch) |
|---|---|---|---|---|---|---|---|
| margrave | Portrait hall, south wall (z 13) | (7.60, 4.85, 13.35), yaw 0 | `Portrait.fbx` child `Canvas`, material `Castle_Canvas` (slot 1) | 0.66 x 1.06, 0.6226 | u 0..1, v 0..1 (whole quad; the frame is a separate `Castle_Brass` slot) | shared `Textures/Portrait.png` | `Resources/Castle/Portraits/Portrait_margrave.png` |
| margravine | same wall | (9.60, 4.85, 13.35), yaw 0 | same | same | same | same | `.../Portrait_margravine.png` |
| steward | west wall (x 6), flanks the balcony door (z 16) | (6.35, 4.85, 14.30), yaw 90 | same | same | same | same | `.../Portrait_steward.png` |
| astronomer | west wall | (6.35, 4.85, 18.20), yaw 90 | same | same | same | same | `.../Portrait_astronomer.png` |
| painter | east wall (x 14) | (13.50, 4.85, 16.40), yaw -90 | same | same | same | same | `.../Portrait_painter.png` |
| cellarer | east wall | (13.50, 4.85, 20.20), yaw -90 | same | same | same | same | `.../Portrait_cellarer.png` |
| child (the door) | Portrait hall to Portrait closet, wall x 11, door centred z 14.40 | hinge (11.0, 3.2, 13.95) yaw -90; canvas centre (11.0, 4.30, 14.40), faces -X (hall side; derived from the hinge maths, not seen in engine) | `PortraitDoorLeaf.fbx` child `Canvas`, `Castle_Canvas` (slot 3) | 0.63 x 1.24, 0.508 | u 0..1, v 0..1 | shared `Textures/Portrait.png` | `.../Portrait_child.png` |

Brief said six portraits with the child as the sixth; the code has six frames plus the door leaf, so there are seven pictures. The sixth frame is the cellarer (the Blood Ledger's first night, `CastleWorld.Bedchamber` note). Frame-to-person order is the code order in `PortraitHall()`.

Runtime today: `CastleArt.ApplyMaterials` swaps the FBX's `Castle_Canvas` slot for `Resources/Castle/Materials/Castle_Canvas.mat` (Standard shader, `_MainTex` = `Textures/Portrait.png` guid `1430e3d44feb8091a22ed0a3d5fd04f8`, canvas-weave normal map). Overwriting `Portrait.png` would change all seven at once (zero code, one face seven times: not what we want). `tools/castle_textures.py` rewrites `Portrait.png` every run; do not run it (or `tools/castle_build_all.sh`) after this work.

The six `TapestryPanel` hangings (Great hall, `CastleLayout.HallBanners`) are oxblood cloth with a brass bat-and-moon emblem modelled in geometry: no image slot, nothing to supply. `Banner` is not placed.

### 1B. Castle images that are not on a frame (4, all missing)

| id | where it shows | measured size / aspect | current | exact file (zero code changes) |
|---|---|---|---|---|
| ledger_steward | note screen for "Ledger: the steward" (`Interface.Note`, `CaseBook.Castle.NoteArt`) | any aspect, fitted into 580x364 px (1.59:1) | none: text-only fallback | `Resources/Castle/LedgerSteward.png` (`Resources.Load("Castle/LedgerSteward")`) |
| ledger_astronomer | "Ledger: the astronomer" | same | none | `Resources/Castle/LedgerAstronomer.png` |
| ledger_painter | "Ledger: the painter" | same | none | `Resources/Castle/LedgerPainter.png` |
| claw | latent claw-scores quad under the grave-ash lantern (`World.CastleTrace`, shader `UVTrace`, additive: `rgb * alpha * 0.85`) | quad 0.45 x 0.40 m, 1.125:1, UV 0..1 | falls back to `Surfaces/UVHandprint.png` (a handprint, wrong for a castle) | `Resources/Castle/ClawScores.png` (RGBA) |

All paths are under `Assets/HollowCurrent/`.

### 1C. Bellwether (checked, nothing blank)

| id | where | position and yaw | slot | current texture | status |
|---|---|---|---|---|---|
| family photo, wall frame | Dining room | `World.Frame(-7.83, 1.9, 11.6)` = `Photograph(.., -90, 1.25)` | `PictureFrame` prefab + child quad "Harrow family photograph", 0.45 x 0.33 x scale 1.25 = 0.56 x 0.41 m (1.364), UV 0..1 | `Resources/Surfaces/FamilyPhoto.png` 1448x1086 (the mother's face rubbed away) | present |
| family photo, sideboard | Dining room | `Photograph(-5.65, 1.34, 13.06, 0, .95)` | same, 0.43 x 0.31 m | same file | present (same picture twice) |
| note art | note screen | n/a | `CaseBook.Bellwether.NoteArt` | `Surfaces/FamilyPhoto`, `ChildDrawing`, `FurnaceDiary` | present |

Frame geometry checked from the prefab bounds (photo quad z -0.033, in front of the mat at -0.027 when seen from -Z): the photo is not hidden. Bellwether needs nothing for the owner's complaint. An OPTIONAL second snapshot for the sideboard frame is at the very end (section 7).

Totals: 9 wall canvases (7 castle, 2 Bellwether), 7 of them the shared placeholder and 0 blank; plus 4 missing castle images off the wall. 11 images to generate (7 + 3 + 1), 1 optional.

## 2. How the pictures reach the game

1. Codex: generate, `make`, `check`, append licence lines, commit images and `.meta` on its own branch (section 6). Codex does **not** edit C# (`CastleWorld.cs` and `SurrealProps.cs` are integrator files, `LOOP.md` rule 2).
2. Integrator (or the owner's session): `git apply docs/haunted-castle/wallart_runtime.patch`, run `python3 tools/check_source.py` (passed on this patch: 487 assertions, runtime and editor C# compile), then re-bake because the bake receipt hashes every `.cs`: `HollowCurrent.Editor.HouseLighting.PrepareAndBakeCastle` (graphics session) then `NativeBuild.BuildPreparedMacCastle`, one Unity at a time through `~/.cache/gpu-slot/bin/gpu_slot.sh`.
3. The patch (82 lines, `git apply --check` clean on `97c02ce`):
   - `CastleWorld.PortraitHall()` now calls `Portrait(id, x, z, yaw)` for the six frames with ids `margrave, margravine, steward, astronomer, painter, cellarer` (same positions as before).
   - `Paint(art, id)` loads `Resources.Load<Texture2D>("Castle/Portraits/Portrait_" + id)` and gives each `Castle_Canvas` slot under that art a material copy with that texture. A missing file leaves the shared placeholder and logs `HC_MISSING_ART texture ...` once.
   - `BuildDoor` calls `Paint(leaf, "child")` for the portrait door.
   - `SurrealProps.AttachPupils(...)` (new, 20 lines) adds the two animated pupils from `MakePortraitEyes` to a frame that already exists, at canvas UV (0.5 +- 0.10, 0.64), 4.5 % of canvas width wide, travel +-1.8 % of width. Remove the one `SurrealProps.AttachPupils` line in `Portrait()` to ship paintings without moving pupils. The door gets no pupils.
   - Not verified in a running engine (no Unity here): pupil depth against the recessed canvas (+4 mm), and that the scene serialises the per-frame materials; the first castle capture decides.
4. No-patch fallback: the ledger pages and `ClawScores.png` work with no code at all (exact load names above). Portraits then need the patch.

## 3. Output specs and the tool

`python3 tools/wallart_prepare.py list` prints this table; `make` crops, resizes, writes the PNG and a Unity `.meta` with a deterministic guid; `check` verifies.

| id | generate at (imagegen native) | crop (source px) | final file | colour | destination |
|---|---|---|---|---|---|
| margrave, margravine, steward, astronomer, painter, cellarer | 1024x1536 | 956x1536, window centred, `--shift` up to +-34 | 1024x2048 PNG, RGB, sRGB, no alpha | sRGB | `Assets/HollowCurrent/Resources/Castle/Portraits/Portrait_<id>.png` |
| child | 1024x1536 | 780x1536 centred | 1024x2048 PNG RGB | sRGB | `.../Portraits/Portrait_child.png` |
| ledger_steward / astronomer / painter | 1536x1024 | none | 1536x1024 PNG RGB | sRGB | `.../Resources/Castle/Ledger{Steward,Astronomer,Painter}.png` |
| claw | 1024x1024 | 1024x910 (height) | 1024x1024 PNG RGBA | colour on black becomes alpha | `.../Resources/Castle/ClawScores.png` |

Why not "pure POT at the generated aspect": the canvas is 0.6226:1 (5:8), the generator offers 2:3. Cropping the 2:3 image to the canvas aspect (956 of 1024 px wide) and then resizing to 1024x2048 is correct because the texture's UV 0..1 always fills the whole canvas, so only the content's aspect before the resize matters. Never stretch the raw image to fit.

Eye position (the portrait spec that matters): in canvas UV (u right 0..1, v **up** 0..1, origin bottom-left) the two eye centres are **(0.40, 0.64) and (0.60, 0.64)** (`SurrealAssets.PortraitEyeU = 0.10`, `PortraitEyeV = 0.64`). In image pixels of the final 1024x2048: (410, 737) and (614, 737), i.e. 36 % down from the top edge. Face centred at u 0.5. Paint large pale irises about 8 % of canvas width (~80 px) with **no dark pupils**: the runtime pupils are 4.5 % wide dark discs drawn over the irises. (Without the patch the pale blank eyes still read as watching.)

One-line verification per portrait (replace the id; passes when the size is right and the eye boxes are brighter than the flanks of the same rows):

```
python3 -c "from PIL import Image,ImageStat as S;p='Assets/HollowCurrent/Resources/Castle/Portraits/Portrait_margrave.png';im=Image.open(p);assert im.size==(1024,2048) and im.mode=='RGB',(im.size,im.mode);g=im.convert('L');w,h=g.size;m=lambda c:S.Stat(c).mean[0];e=(m(g.crop((int(.33*w),int(.325*h),int(.47*w),int(.395*h))))+m(g.crop((int(.53*w),int(.325*h),int(.67*w),int(.395*h)))))/2;f=(m(g.crop((0,int(.325*h),int(.15*w),int(.395*h))))+m(g.crop((int(.85*w),int(.325*h),w,int(.395*h)))))/2;print('eyes',round(e),'flank',round(f));assert e>1.25*f+6"
```

Fuller check with a written overlay (green boxes at the required eye spots, yellow centre line) that you must look at once per portrait: `python3 tools/wallart_prepare.py check --overlay /tmp/wallart_overlay` (writes `eyes_<id>.png`). If the face is slightly off-centre, `make <id> <raw.png> --shift N` (N in source px, positive moves the crop window right, max +-34). If the eyes are far from 36 % down, regenerate; do not warp the image.

Ledger and claw checks are in the same `check` command (size, mode, alpha coverage 3 to 60 %).

## 4. Prompts (one image at a time; review, then the next)

Generate with your `imagegen` skill at the "generate at" size from the table, save the raw output anywhere outside the repo (e.g. `/tmp/wallart/raw/<id>.png`), then `python3 tools/wallart_prepare.py make <id> /tmp/wallart/raw/<id>.png`. Fiction: Count Claude was the margrave; he bargained for a night that never ends and the household was written out of the Blood Ledger one by one (`DESIGN.md`). Every portrait is a fictional person, none depicts a real one. Shared look: GPT Image 2.5 refs style (`docs/haunted-castle/refs/*.prompt.txt`): "realistic painterly horror style, no text, no watermark".

If the first result misses the eye spec, change only the numbers in the sentence "Eyes 36% down..." and try once more; at most two tries per image, then keep the better one and note it.

### P1 margrave (the margrave before the bargain, same face the Count will lose)

```
Oil portrait, 17th-century Northern European court style, vertical 2:3, cracked varnish and amber patina, candlelit from lower left, muted oxblood, umber and bone palette, dark background. Bust-length, face front-on and level, centred, top of head 15% from top edge. Eyes 36% down from top, centres at 40% and 60% across, large, wide-set, pale irises with no dark pupils, gazing straight at the viewer. Painting fills the whole image: no frame, no border, no signature, no text, no watermark. The margrave before the bargain: a living man of about forty, black hair to the collar, pale skin with a flush, sharp cheekbones, proud and tired, high-collared oxblood velvet coat, one tarnished silver buckle.
```

### P2 margravine

```
Oil portrait, 17th-century Northern European court style, vertical 2:3, cracked varnish and amber patina, candlelit from lower left, muted oxblood, umber and bone palette, dark background. Bust-length, face front-on and level, centred, top of head 15% from top edge. Eyes 36% down from top, centres at 40% and 60% across, large, wide-set, pale irises with no dark pupils, gazing straight at the viewer. Painting fills the whole image: no frame, no border, no signature, no text, no watermark. The margrave's wife: a woman of about thirty-five, hair pinned under a dark linen cap, bone-white lace collar, a garnet pendant, composed and sorrowful, a faint smile that does not reach her eyes.
```

### P3 steward (hid the three relics)

```
Oil portrait, 17th-century Northern European court style, vertical 2:3, cracked varnish and amber patina, candlelit from lower left, muted oxblood, umber and bone palette, dark background. Bust-length, face front-on and level, centred, top of head 15% from top edge. Eyes 36% down from top, centres at 40% and 60% across, large, wide-set, pale irises with no dark pupils, gazing straight at the viewer. Painting fills the whole image: no frame, no border, no signature, no text, no watermark. The steward: a lean grey-bearded man of about fifty-five in a plain umber doublet, a heavy ring of iron keys held against his chest, one hand bandaged, wary and resolute.
```

### P4 astronomer

```
Oil portrait, 17th-century Northern European court style, vertical 2:3, cracked varnish and amber patina, candlelit from lower left, muted oxblood, umber and bone palette, dark background. Bust-length, face front-on and level, centred, top of head 15% from top edge. Eyes 36% down from top, centres at 40% and 60% across, large, wide-set, pale irises with no dark pupils, gazing straight at the viewer. Painting fills the whole image: no frame, no border, no signature, no text, no watermark. The astronomer: a thin man of about sixty, bald with white hair at the sides, dark scholar's gown, a small brass astrolabe held low in frame, ink-stained fingers, absent-minded wonder.
```

### P5 painter (self-portrait)

```
Oil portrait, 17th-century Northern European court style, vertical 2:3, cracked varnish and amber patina, candlelit from lower left, muted oxblood, umber and bone palette, dark background. Bust-length, face front-on and level, centred, top of head 15% from top edge. Eyes 36% down from top, centres at 40% and 60% across, large, wide-set, pale irises with no dark pupils, gazing straight at the viewer. Painting fills the whole image: no frame, no border, no signature, no text, no watermark. The painter, a self-portrait: a woman of about forty in a paint-stained umber smock, sleeves rolled, a brush in one hand, a smear of oxblood pigment on her cheek, steady and exhausted, looking out as if into a mirror.
```

### P6 cellarer (the first of the household taken)

```
Oil portrait, 17th-century Northern European court style, vertical 2:3, cracked varnish and amber patina, candlelit from lower left, muted oxblood, umber and bone palette, dark background. Bust-length, face front-on and level, centred, top of head 15% from top edge. Eyes 36% down from top, centres at 40% and 60% across, large, wide-set, pale irises with no dark pupils, gazing straight at the viewer. Painting fills the whole image: no frame, no border, no signature, no text, no watermark. The cellarer: a stocky man of about fifty in a leather apron, a pewter cup in one hand, a candle at his shoulder, round ruddy face, an uneasy half-smile.
```

### P7 child (the portrait that is the door; moonstone is hidden behind it)

Full length because the canvas is door-shaped (0.51:1). No eye-band check applies (no pupils on the door); keep the head in the upper fifth and the figure in the central 55 % of the width, because the crop removes 24 % of the width.

```
Oil portrait, 17th-century Northern European court style, tall vertical, cracked varnish and amber patina, candlelit from lower left, muted oxblood, umber and bone palette, very dark archway behind. Full-length, figure centred and occupying the middle half of the width, feet near the bottom edge. A solemn child of about eight in a pale bone-white linen dress and small oxblood shoes, hair tied back, standing very still, holding one unlit candle in both hands. Head in the upper fifth, eyes 18% down from top, large, wide-set, pale irises with no dark pupils, gazing straight at the viewer. Painting fills the whole image: no frame, no border, no signature, no text, no watermark.
```

If the generator refuses P7: replace the sentence "A solemn child ... both hands." with "A porcelain-faced child's doll in a pale bone-white linen dress and small oxblood shoes, standing very still, holding one unlit candle in both hands." and note it in the commit.

### L1 ledger_steward (note art; transcript text appears beside it in game, so the page must not contain readable words)

```
Flat scan of one aged vellum ledger page filling the whole image edge to edge, viewed straight on, no perspective, no table, no cast shadow, landscape 3:2. Faded brown iron-gall ink, a column of dense cramped handwriting that is not legible, ruled margins, ash smudges, a few dark red wax drips. Centre: a hurried ink plan of a vaulted crypt with a narrow channel, one coffin niche marked by three parallel claw scratches, and a small drawing of a plain silver sword blade beside it. Muted umber, bone and oxblood. No readable words, no logo, no frame.
```

### L2 ledger_astronomer

```
Flat scan of one aged vellum ledger page filling the whole image edge to edge, viewed straight on, no perspective, no table, no cast shadow, landscape 3:2. Faded brown iron-gall ink, cramped handwriting that is not legible, ruled margins, ash smudges. Centre: a careful ink diagram of a brass orrery with concentric orbits, two moons drawn as a large and a small circle, one orbit with an empty circle where a planet should be and a tiny drawing of a sword hilt in the margin beside it. Star charts at the edges. Muted umber, bone and oxblood. No readable words, no logo, no frame.
```

### L3 ledger_painter

```
Flat scan of one aged vellum ledger page filling the whole image edge to edge, viewed straight on, no perspective, no table, no cast shadow, landscape 3:2. Faded brown iron-gall ink, cramped handwriting that is not legible, ruled margins, oxblood pigment smears and brush marks. Centre: a quick ink sketch of an upright portrait frame whose lower half is a narrow door with a latch, the painted face above it with both eyes struck out, and a small drawing of a milky round stone in the corner. Muted umber, bone and oxblood. No readable words, no logo, no frame.
```

### C1 claw (RGBA decal; the script turns brightness into alpha)

```
Flat texture on a pure black background, viewed straight on, square. Four deep parallel claw gouges slanting across the image with splintered edges, a short second set crossing them, and a long faint wing-drag smear curving beside them. Drawn only as glowing pale blue-white ash lines, like grave-ash revealed by ultraviolet light, soft glow falling off at the edges. Everything else pure black, no stone texture, no background detail, no text, no watermark, no frame.
```

Word counts (checked): every prompt above is under 120 words (the longest portrait prompt is 117).

## 5. Fallback: "If Higgsfield credits run out" (the project's other images, for Codex `imagegen`)

Nothing below is needed for the wall art. These are the prompts for every other image the castle work generated, so they can be remade without Higgsfield. Job records are in `docs/haunted-castle/refs/*.job.json`; `docs/haunted-castle/gen.sh <name> <aspect>` is the Higgsfield call (GPT Image 2.5, quality high, 2k). Pixel size does not matter for the concept refs (they feed critics and 3D input, never the build): use the largest size of that aspect your generator offers; keep the aspect. `docs/haunted-castle/refs/*.png` is git-ignored, only `*.preview.webp` (1280 px wide) is committed: save the raw PNG there anyway, and write a 1280-wide `<name>.preview.webp`. Higgsfield originals were 2688x1520 (16:9), `exterior_sky` 2688x1152 (21:9).

Not Higgsfield, no prompt needed: `Resources/UI/BloodLedger/{Panel,Dialogue,Warning}.png` come from `python3 tools/ui_blood_ledger.py` (deterministic, from the Mourning Note cards). **Do not re-run that script on a tree that has the title key art:** it also rewrites `Resources/UI/CastleBackdrop.png`. The audio (Seed Audio) and the Customuse/Tripo meshes are out of scope here.

| name | aspect | destination | prompt source |
|---|---|---|---|
| great_hall | 16:9 | `docs/haunted-castle/refs/great_hall.png` | below |
| crypt | 16:9 | `.../crypt.png` | below |
| chapel_altar | 16:9 | `.../chapel_altar.png` | below |
| exterior_sky | 21:9 | `.../exterior_sky.png` | below |
| lord_sheet | 16:9 | `.../lord_sheet.png` (superseded by the demon sheet; kept for the critic history) | below |
| count_demon_sheet | 16:9 | `.../count_demon_sheet.png`; the three views are cropped to `demon_front.png`, `demon_side.png`, `demon_back.png` | below |
| lord_perch | 16:9 | `.../lord_perch.png` (round-5 capture target, `CaptureMode` shot S5) | below |
| relics | 16:9 | `.../relics.png` | below |
| weapon | 16:9 | `.../weapon.png` | below |
| title_1 | 16:9 | key art; on branch `hc/title` as `refs/title_1.prompt.txt`; shipped as `Assets/HollowCurrent/Resources/UI/CastleBackdrop.png`, centre-cropped to 16:9 and Lanczos to 2560x1440 (importer max size 4096 on that branch) | below |

Each prompt is the file's text verbatim (`docs/haunted-castle/refs/<name>.prompt.txt`; read the file, do not retype). For convenience:

**great_hall** Interior concept art of the great hall of a vampire lord's gothic castle, wide establishing shot from the entrance doors at eye level. Soaring vaulted ceiling, two rows of stone columns, a long banquet table with the feast rotted to black. Surreal details: candelabras and open books float motionless in the air, pale runes glow faintly on the flagstones in a ring, and the complete skeleton of something enormous and many-ribbed lies draped over the far end of the hall as if it died there. Tall windows show two moons, one pale, one blood-red. Cold blue moonlight through the windows, warm embers in a dead fireplace, long shadows, mist on the floor. Realistic painterly horror style, no people, no creatures, no text, no watermark.

**crypt** Interior concept art of a crypt beneath a vampire lord's gothic castle. A central stone sarcophagus with its lid pushed half open, carved with a winged figure. Around it, dozens of wall niches with coffins, some open. A narrow channel of dark red liquid runs through the floor like a river, reflecting faint light, with a small stone bridge over it. Surreal details: roots growing downward from the ceiling that end in tiny pale hands, and runes glowing dim violet on the sarcophagus. Only light sources are a few guttering candles and the glow of the runes. Deep darkness, fog along the floor, oppressive and claustrophobic. Realistic painterly horror style, no people, no creatures, no text, no watermark.

**chapel_altar** Interior concept art of a desecrated castle chapel, centered on a black stone altar. The altar top has three empty carved recesses shaped for a long blade, a sword hilt, and a round gem, each outlined with faint silver inlay. Above it a cracked stained-glass window of a sun that has been painted over with black. Pews are overturned, a stone font is filled with still dark red liquid, and dozens of candles float in the air around the altar at different heights, all unlit except one. Dust, cobwebs, cold blue moonlight leaking around the blackened window, warm glow from the single flame. Realistic painterly horror style, no people, no creatures, no text, no watermark.

**exterior_sky** Wide exterior concept art of a vampire lord's gothic castle on a black crag at night, seen from the far end of a lowered wooden drawbridge. The moat below is a slow river of dark red. Two moons dominate the sky, one huge and pale, one smaller and blood-red, with a faint green aurora between them and stars that are slightly too large. Jagged towers, one broken, bats circling, a single warm light in a high window. Bare dead trees, mist, cold blue moonlight. Realistic painterly horror style, no people, no text, no watermark.

**lord_sheet** Character reference sheet of a winged vampire lord, three full-body views side by side on one plain dark grey background: front, side profile, and back. Neutral standing pose, arms slightly away from the body, wings half-folded so the silhouette is readable. Gaunt 7-foot aristocrat, ashen grey skin, sunken eyes glowing ember red, slicked black hair, long clawed fingers. Huge tattered bat wings with a membrane that looks like torn funeral silk, wing bones ending in hooks. Floor-length black and oxblood coat with a high collar, tarnished silver buckles, bare clawed feet. Bioluminescent veins faintly visible under the skin at the throat and hands. Consistent proportions across all three views, even flat studio lighting, no background scenery, no text, no watermark. Realistic painterly horror concept art.

**count_demon_sheet** (canonical Count; Higgsfield's 3D moderation refused meshes from it as `nsfw`, so 3D came from Customuse. For 2D, moderation may still trim the gore words: the shipped sheet was accepted as written.) Character reference sheet of a grotesque demonic vampire, three full-body views side by side on one plain dark grey background: front, side profile, and back. Neutral standing pose, arms slightly away from the body, wings half-folded so the silhouette is readable. Nine feet tall, emaciated, limbs too long with extra joints, spine visible through split skin on the back. Face skinless and distorted: a jaw split vertically into two halves hanging open, rows of needle teeth, no lips, eye sockets that are black holes with faint red embers deep inside, ears torn off. Skin grey-white and translucent with black veins, flayed raw red patches on the chest and forearms with exposed sinew, fresh blood running from the mouth and claws, dried blood crusted on the neck. Hands are elongated claws with six fingers. Wings are membranes of stretched human skin on bone, torn, with hooks, dripping. Only a remnant of a once-noble floor-length black and oxblood coat, shredded and soaked, high collar still standing, tarnished silver buckles, bare clawed feet. Consistent proportions across all three views, even flat studio lighting, no background scenery, no text, no watermark. Realistic painterly horror concept art, body horror, disturbing.

**lord_perch** Cinematic horror concept art. A winged vampire lord crouches on a stone balcony railing inside a vast gothic great hall, wings spread wide, head tilted unnaturally, ember-red eyes fixed on the viewer far below. Gaunt grey aristocrat in a long black and oxblood coat with a high collar, tattered bat wings like torn funeral silk with hooked wing bones, bare clawed feet gripping the railing. Below him, candelabras float in the air at different heights, their flames bending toward him. Two moons, one pale and one blood-red, shine through a shattered rose window behind him. Cold blue moonlight against warm candlelight, deep shadows, dust in the air, extreme low angle from the floor. Realistic painterly style, no text, no watermark.

**relics** Item reference sheet on a plain dark grey background, three objects spaced evenly in a row, each shown clearly from a three-quarter angle with even studio lighting and a soft shadow beneath. Left: a slender silver sword blade without a handle, slightly tarnished, engraved with a thin line of runes, the tip blackened. Middle: an ornate sword hilt and crossguard of dark iron wrapped in faded red leather, with an empty round socket in the pommel. Right: a fist-sized uncut moonstone, milky white with a faint internal blue glow. Realistic painterly style, museum-catalog clarity, no text, no watermark.

**weapon** Weapon reference on a plain dark grey background, a single longsword shown in two views: the full sword horizontal across the upper half, and a large close-up of the hilt in the lower half. The slender silver blade is engraved with a thin line of runes that glow faint blue, the dark iron crossguard is wrapped in faded red leather, and a milky uncut moonstone glowing softly sits in the pommel socket. Slight wisps of pale mist drift off the blade. Even studio lighting, realistic painterly style, no hands, no text, no watermark.

**title_1** (from `git show origin/hc/title:docs/haunted-castle/refs/title_1.prompt.txt`) Painted cinematic key art, oil-on-canvas texture, night. A gothic castle on a black crag above a still moat of dark red water that reflects two moons: one large bone-white moon low on the left, one small blood-red moon higher on the right. Bruised violet and deep teal sky with thin high cloud, a few hard stars. The castle is mostly in shadow; one gatehouse window glows warm candlelight, and a lowered drawbridge chain catches moonlight. On the tallest tower a gaunt winged figure crouches in silhouette against the white moon. Low mist on the water. Heavy blacks, muted palette, no saturation spikes, no aurora, no bokeh. No text, no letters, no logo, no watermark, no frame. Composition: castle on the right two thirds, the left third dark sky and water for text. 16:9. Accept it when the left 30 % is dark (mean luma under 60 at 1920 wide), there are two moons (bone-white left, blood-red right) and no text.

Bellwether and UI art that was generated outside Higgsfield, with the prompts already in the repo (read them there; regenerate only if the owner asks): `Assets/HollowCurrent/Art/References/UI/prompts.json` (two UI concepts and the `background_prompt` that produced `Resources/UI/MenuBackdrop.png`; the background step needs the chosen concept attached), `Art/References/Quality/{family-photo-prompt.txt, story-pages.json (ChildDrawing, FurnaceDiary), rug-prompt.txt, ground-textures.json, hearthbound-prompt.txt, concept-prompt.txt}`, `Art/References/Vehicle/prompt.txt`, `Art/References/DetailWave1|2/prompts.json`. Their outputs are `Resources/Surfaces/*.png`.

## 6. Workflow, licence, commit (Codex)

```
cd /Users/midir/hollow-current-haunted-castle
git fetch origin
git worktree add .wt/wallart-art -b hc/wallart-art origin/hc/wallart      # has tools/wallart_prepare.py; never main, never rebase
cd .wt/wallart-art
# per image: generate -> make -> eyeball the overlay -> next
python3 tools/wallart_prepare.py make margrave /tmp/wallart/raw/margrave.png
python3 tools/wallart_prepare.py check --overlay /tmp/wallart/overlay
```

Licence: outputs go in under the repo's CC BY 4.0 media grant (`ASSET_LICENSES.md`, "Project asset grant"; credit "Hollow Current by Ashen"). Images made with Higgsfield are covered by the Higgsfield permission lines, but **the wall art is generated by Codex's imagegen, so name that**. Append one line per asset to `ASSET_LICENSES.md` under a new heading `## Haunted Castle wall art (added <date>)`:

```
- `Resources/Castle/Portraits/Portrait_margrave.png` — original project prompt (`docs/haunted-castle/CODEX_HANDOFF.md`, P1) through Codex `imagegen` (OpenAI image generation; model name as reported by the skill), cropped and resized by `tools/wallart_prepare.py`. Fictional person; CC BY 4.0 under the project grant; OpenAI terms assign output rights to the user.
```

(one such line each for `Portrait_margravine/steward/astronomer/painter/cellarer/child`, `LedgerSteward/Astronomer/Painter`, `ClawScores`). If you instead remake a section 5 image with Higgsfield, name Higgsfield and the job id as `docs/haunted-castle/ASSET_LOG.md` does. Log any rejected or re-run generations in that file's table style (id, prompt id P1..C1, tries, result). No reference images from any other game; no real game names in prompts.

Commit images, their `.meta` files, `ASSET_LICENSES.md` and the `ASSET_LOG.md` rows, with trailer `Co-Authored-By: Codex <noreply@openai.com>`; `git push -u origin hc/wallart-art`; write `HANDOFF.md` for that worktree listing which of the 11 images passed `check`, the eye-overlay verdicts and anything refused. Then ask the integrator to merge both `hc/wallart` (tool, patch, this file) and `hc/wallart-art` and apply the patch (section 2). Remove your worktree after the push (`git worktree remove .wt/wallart-art`).

### 6A. Codex delivery receipt — 2026-10-14

**11/11 actual PNGs and their Unity `.meta` files are saved and pass `python3 tools/wallart_prepare.py check`.** Generated with the built-in OpenAI `image_gen.imagegen` tool, not Higgsfield or placeholders. The tool does not expose its model version or cost; neither is guessed. The introductory inventory above records the pre-generation state.

**Delivery worktree:** `/Users/midir/hollow-current-haunted-castle/.wt/wallart-art`, branch `hc/wallart-art`. All file paths in this table are relative to that worktree. From the integration checkout, prefix them with `.wt/wallart-art/` to access the files immediately. Assets have not been copied over concurrent integrator files. No push, publication, game/Unity launch or Ghostty interaction occurred.

| Asset | Delivered relative path | Built-in calls including edits |
|---|---|---|
| margrave | `Assets/HollowCurrent/Resources/Castle/Portraits/Portrait_margrave.png` | 4 |
| margravine | `Assets/HollowCurrent/Resources/Castle/Portraits/Portrait_margravine.png` | 3 |
| steward | `Assets/HollowCurrent/Resources/Castle/Portraits/Portrait_steward.png` | 4 |
| astronomer | `Assets/HollowCurrent/Resources/Castle/Portraits/Portrait_astronomer.png` | 2 |
| painter | `Assets/HollowCurrent/Resources/Castle/Portraits/Portrait_painter.png` | 3 |
| cellarer | `Assets/HollowCurrent/Resources/Castle/Portraits/Portrait_cellarer.png` | 2 |
| child | `Assets/HollowCurrent/Resources/Castle/Portraits/Portrait_child.png` | 1 |
| ledger_steward | `Assets/HollowCurrent/Resources/Castle/LedgerSteward.png` | 2 |
| ledger_astronomer | `Assets/HollowCurrent/Resources/Castle/LedgerAstronomer.png` | 1 |
| ledger_painter | `Assets/HollowCurrent/Resources/Castle/LedgerPainter.png` | 1 |
| claw | `Assets/HollowCurrent/Resources/Castle/ClawScores.png` | 1 |

Seven portraits are 1024x2048 RGB; ledgers are 1536x1024 RGB; claw is 1024x1024 RGBA with 19% alpha coverage. Prepared using the existing `tools/wallart_prepare.py`; no modifications to that script or C#.

Evidence in delivery worktree: `docs/haunted-castle/wallart-delivery/contact-sheet.jpg`, `check.txt`, `manifest.json` (SHA-256, source output, crop shifts, attempts and caveats), `original-prompts.json` (all 11 source prompts), and `eyes_<id>.png` for the six adult portraits. All original/rejected tool outputs remain untouched in `/Users/midir/Documents/Codex/2026-10-14/task/generated_images/`.

**Visual checks and remaining gaps:** All six adult portraits now have pale eyes without distinct dark pupils after bounded built-in edits. All six overlays were inspected. The automated brightness test is a heuristic and can pass cheek/beard regions; it is NOT proof of exact eye alignment. The fixed target is `(410,737)` / `(614,737)` in final pixels (UV `(0.40,0.64)` / `(0.60,0.64)`). The margrave and painter still have material vertical offsets; the other four have smaller position/spacing differences. Eyes are also smaller than the requested ~80-pixel iris diameter in several paintings. **Do not enable the unmodified fixed-position pupil overlay and call these aligned.** Use the static paintings first, or have the integrator fit per-portrait pupil centers/diameters and verify in-engine. Approximate visually measured centers below are starting points, not exact calibration (about +/-5 source pixels):

| Portrait | Approximate final image pixel centers | Approximate canvas UV (v up) |
|---|---|---|
| margrave | `[[412, 793], [637, 793]]` | `[[0.403, 0.613], [0.622, 0.613]]` |
| margravine | `[[411, 713], [612, 713]]` | `[[0.402, 0.652], [0.597, 0.652]]` |
| steward | `[[426, 751], [591, 751]]` | `[[0.416, 0.633], [0.577, 0.633]]` |
| astronomer | `[[439, 720], [624, 720]]` | `[[0.429, 0.648], [0.61, 0.648]]` |
| painter | `[[431, 693], [590, 693]]` | `[[0.421, 0.661], [0.576, 0.661]]` |
| cellarer | `[[426, 728], [600, 731]]` | `[[0.416, 0.645], [0.586, 0.643]]` |

Child: complete figure/candle/shoes survive the crop; dress is more ornate than the plain-linen prompt and eyes sit around 14% rather than 18% down; no animated pupils needed. Ledger clue motifs were inspected; handwriting has no clearly readable words, and the steward's initial complete sword was edited to a bare blade/tang. Astronomer moon studies are more elaborate than requested. Claw alpha/background checks pass. No image-generation refusals.

**Integrator action:** take the asset/provenance files from `hc/wallart-art` (or copy the exact files above with their `.meta` files). As checked during delivery, integration `CastleWorld.cs` already contains `Paint(art,id)` and `SurrealProps.AttachPupils`; do not blindly reapply the historical patch in section 2. Resolve the pupil alignment issue before a castle re-bake/render. The existing runtime wiring, current bake receipts, rendering and gameplay were not changed or tested here. Section 5 fallback images and optional section 7 photo were not requested/generated. Keep this worktree until its local assets have been integrated; it was not pushed per the user's instruction.

## 7. Optional: second Bellwether photograph (not needed for the owner's complaint)

Both Bellwether frames show the same picture. If a second one is wanted, "one detail wrong" snapshot for the wall frame, generate at 1536x1024 with `Resources/Surfaces/FamilyPhoto.png` attached as identity reference, then `python3 tools/wallart_prepare.py make photo2 raw.png` (crops to 4:3, writes 1448x1086 `Resources/Surfaces/FamilyPhoto2.png`):

```
A single old family snapshot photographed straight on, edge to edge, landscape 4:3: the same four fictional family members from the attached photograph, mother, father, teenage son and young daughter, now standing on the back lawn in daylight, ordinary awkward candid poses, mother's face intact. Faded 1980s consumer colour film, subdued warm colours, mild print wear, narrow off-white border. One thing is wrong: a fifth long shadow lies across the grass, cast by nobody in the picture. No gore, no text, no frame, no perspective skew, not a photo of real people.
```

It needs a 3-line change in `Runtime/World.cs` (integrator; the Bellwether receipt goes stale): `Palette()`: add `Mat("familyphoto2",Color.white);Materials["familyphoto2"].mainTexture=Resources.Load<Texture2D>("Surfaces/FamilyPhoto2")??Resources.Load<Texture2D>("Surfaces/FamilyPhoto");`; `Photograph(Vector3 center,float yaw,float scale,string material="familyphoto")` using `Materials[material]`; and `void Frame(Vector3 p)=>Photograph(p,-90,1.25f,"familyphoto2");` (the wall frame). Not compile-checked.
