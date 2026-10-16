# Investigator's casefile

An investigator sorts the impossible into an ordinary evidence folder.
A punched sheet, binder rings and a manila tab establish an object, not a web panel.
Courier Prime makes field notes, recovered transcripts and case conclusions feel related.
Oxide stamps and the negative selection strip mark decisions without ornate buttons.
The HUD borrows a small evidence slip; the final screen becomes a disposition stamp.

## Palette

| Role | sRGB hex |
| --- | --- |
| bg | `#181c1a` |
| paper | `#e6dfc9` |
| ink | `#252a26` |
| accent | `#813b2e` |
| muted | `#515548` |
| dark | `#111713` |
| light | `#eee9d8` |

## Type at 1920 × 1080

| Style | Bundled font | Sizes used |
| --- | --- | --- |
| display | `courierprime/CourierPrime-Bold.ttf` | 31, 41, 70, 76, 80, 82, 118 px |
| body | `courierprime/CourierPrime-Regular.ttf` | 34, 35, 36, 37, 38, 40, 43 px |
| mono | `courierprime/CourierPrime-Regular.ttf` | 25, 26, 27, 28, 29, 30, 32, 35, 36 px |

Tracking is 0 px for all body text. Last light uses 2–4 px on small capitals; Two moons uses 2 px on its document kicker; headings in the shared journal/pause templates use 2 px kicker tracking. Casefile and Dead channel keep most labels untracked. The title tagline and map-choice label use 1 px. Body copy wraps by measured font width and a 70-character ceiling, with 1.25–1.36 line spacing. The full pixel rectangles, colours and sizes are in `layout_audit.json`.

Minimum measured text contrast: **5.75:1**. Body copy is 30 px or larger (document prose is 36–46 px across the set); smaller 25–29 px text is supplementary instrumentation or metadata. Every text run has an opaque, quiet backing, so no stain, scanline or ornament crosses letters. Body text stays readable against a near-black scene.

## Frame and material recipe

Flat olive folder with a raised manila tab; a 512 px paper texture keeps grain in the outer 48 px; binder rings and punched holes occupy the left margin. Oxide red is for retrieval and disposition only.

Motifs: Binder rings, evidence numbers, manila tabs, recovery stamp, carbon-copy selection, hand-drawn pulse.

## Unity IMGUI translation

- Pure code: flat backing rectangles, selection strips, text, slider tracks/knobs, status meters, settings values and focus borders. One dynamic Font per GUIStyle; use the bundled regular font for prose, the display font for titles and the mono style for labels. No fallback to system fonts.
- Textures: `surface.png` (512², quiet centre), `frame.png` (512² RGBA, nine-slice border 48 px), and `icon_atlas.png` (512² RGBA). The atlas contains seal (0,0,170,170), reliquary (185,10,130,170), doorway (345,10,130,170), and pulse (0,210,512,130). Keep corner motifs fixed-size when nine-slicing.
- `photo_placeholder.png` is 720 × 540, a deliberately illustrated 4:3 placeholder. `castle_plate.png` and `bellwether_plate.png` are 1920 × 1080 procedural review plates. They are not screenshots of the shipped game, and do not imply engine integration.
- Scale a 1920 × 1080 reference canvas uniformly and letterbox as needed. Preserve at least 72 px safe margins for HUD information. The hunt state is represented together with ordinary objective/prompt information to stress-test hierarchy.
- Use GUI.DrawTexture for paper, wax/ornament crops and plates; GUI.Label for actual strings; GUI.DrawTexture with a 1 px solid texture for rules. Prebake any grain/scanline detail. No UI shader, real-time blur, masking system or post-process is required.
- The supplied frame and atlas are reusable samples; the complete material/layout recipe is `../render_core.py`. Export the exact screen-specific ornament layer from that code when implementing a selected direction. No C# or scene has been changed in this proposal.

## Regenerate

From this directory: `python3 render.py`. From the variants root: `python3 render_core.py 01-casefile`. The adjacent shared renderer is required; all fonts and source textures regenerate offline. Dependencies are pinned in `../requirements.txt`. The renderer generates all eight named PNGs, the six texture files and the measurement log. Run `python3 ../package.py` to refresh comparison sheets after a change.

## Screens

`document_castle.png`, `document_bellwether.png`, `journal.png`, `hud.png`, `title.png`, `settings.png`, `ending_closed.png`, `ending_lost.png` — all 1920 × 1080. `REVIEW_SHEET.png` is the labelled overview. “At dawn, the bell kept its silence.” and “The house has learned another name.” are the two original one-line ending epitaphs.
