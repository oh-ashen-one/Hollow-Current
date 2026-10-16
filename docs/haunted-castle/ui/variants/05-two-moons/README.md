# Under two moons

A black illuminated manuscript belongs to the castle beneath its two moons.
UnifrakturMaguntia is reserved for short display phrases; IM Fell English carries readable prose.
Gold tracery repeats the narrow lancets, reliquary and paired moons of the setting.
The ink-dark ground, restrained rubric and inset folio numbering replace generic RPG panels.
The closing cards read as memorial inscriptions; the gameplay HUD retains only the quiet type and rules.

## Palette

| Role | sRGB hex |
| --- | --- |
| bg | `#121116` |
| paper | `#121116` |
| ink | `#ece1c3` |
| accent | `#dfbb73` |
| muted | `#c4b797` |
| dark | `#121116` |
| light | `#ece1c3` |

## Type at 1920 × 1080

| Style | Bundled font | Sizes used |
| --- | --- | --- |
| display | `unifrakturmaguntia/UnifrakturMaguntia-Book.ttf` | 41, 100, 112, 118, 123 px |
| body | `imfellenglish/IMFeENrm28P.ttf` | 32, 34, 35, 36, 37, 38, 40, 43, 44, 45, 46 px |
| mono | `imfellenglish/IMFeENrm28P.ttf` | 25, 26, 27, 28, 29, 30, 32, 35, 36 px |

Tracking is 0 px for all body text. Last light uses 2–4 px on small capitals; Two moons uses 2 px on its document kicker; headings in the shared journal/pause templates use 2 px kicker tracking. Casefile and Dead channel keep most labels untracked. The title tagline and map-choice label use 1 px. Body copy wraps by measured font width and a 70-character ceiling, with 1.25–1.36 line spacing. The full pixel rectangles, colours and sizes are in `layout_audit.json`.

Minimum measured text contrast: **9.46:1**. Body copy is 30 px or larger (document prose is 36–46 px across the set); smaller 25–29 px text is supplementary instrumentation or metadata. Every text run has an opaque, quiet backing, so no stain, scanline or ornament crosses letters. Body text stays readable against a near-black scene.

## Frame and material recipe

Black ground with narrow paired lancets, thin gold boundary rules, small seal roundels and two moons. The ledger woodcut stays in the left margin. Every text run has a clean ink-dark field, interrupting tracery where necessary.

Motifs: Twin moons, pointed lancets, gold roundels, reliquary, blackletter chapter title, memorial inscription.

## Unity IMGUI translation

- Pure code: flat backing rectangles, selection strips, text, slider tracks/knobs, status meters, settings values and focus borders. One dynamic Font per GUIStyle; use the bundled regular font for prose, the display font for titles and the mono style for labels. No fallback to system fonts.
- Textures: `surface.png` (512², quiet centre), `frame.png` (512² RGBA, nine-slice border 48 px), and `icon_atlas.png` (512² RGBA). The atlas contains seal (0,0,170,170), reliquary (185,10,130,170), doorway (345,10,130,170), and pulse (0,210,512,130). Keep corner motifs fixed-size when nine-slicing.
- `photo_placeholder.png` is 720 × 540, a deliberately illustrated 4:3 placeholder. `castle_plate.png` and `bellwether_plate.png` are 1920 × 1080 procedural review plates. They are not screenshots of the shipped game, and do not imply engine integration.
- Scale a 1920 × 1080 reference canvas uniformly and letterbox as needed. Preserve at least 72 px safe margins for HUD information. The hunt state is represented together with ordinary objective/prompt information to stress-test hierarchy.
- Use GUI.DrawTexture for paper, wax/ornament crops and plates; GUI.Label for actual strings; GUI.DrawTexture with a 1 px solid texture for rules. Prebake any grain/scanline detail. No UI shader, real-time blur, masking system or post-process is required.
- The supplied frame and atlas are reusable samples; the complete material/layout recipe is `../render_core.py`. Export the exact screen-specific ornament layer from that code when implementing a selected direction. No C# or scene has been changed in this proposal.

## Regenerate

From this directory: `python3 render.py`. From the variants root: `python3 render_core.py 05-two-moons`. The adjacent shared renderer is required; all fonts and source textures regenerate offline. Dependencies are pinned in `../requirements.txt`. The renderer generates all eight named PNGs, the six texture files and the measurement log. Run `python3 ../package.py` to refresh comparison sheets after a change.

## Screens

`document_castle.png`, `document_bellwether.png`, `journal.png`, `hud.png`, `title.png`, `settings.png`, `ending_closed.png`, `ending_lost.png` — all 1920 × 1080. `REVIEW_SHEET.png` is the labelled overview. “At dawn, the bell kept its silence.” and “The house has learned another name.” are the two original one-line ending epitaphs.
