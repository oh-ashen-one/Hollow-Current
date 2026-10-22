# Codex task: Hollow Current UI overhaul — generate variations (2026-10-15)

Owner (Hari) wants a full UI rehaul of **Hollow Current** (Unity 6, first-person ghost-hunting horror; two maps: "14 Bellwether Lane", a 1990s suburban haunted house, and "Haunted Castle", a surreal gothic castle with two moons and a winged vampire lord, Count Claude). The current UI looks like a web page: a torn beige parchment card on a dark backdrop, grey kickers, plain serif titles, boxed buttons ("FILE & CLOSE  ESC"), footers. He called the document screen "this letter shit is too ugly". Produce **distinct UI direction variations** he can pick from before anyone writes Unity code.

## What you are producing
Static, reproducible mock-ups plus a style sheet per variation. No C# changes, no scene changes. Work on branch `codex/ui-variants`, output under `docs/haunted-castle/ui/variants/<variant-slug>/`.

Make **5 variations**, each a complete, coherent direction (not colour swaps). Suggested axes, pick five clearly different ones:
1. **Investigator's casefile**: clipboard, manila folder, carbon-copy forms, typewriter, evidence tags, polaroids, red stamp; the UI is the field office's paperwork.
2. **Diegetic occult**: everything drawn in the world's own materials: iron-gall ink on vellum, wax, blood, candle-lit; thick black borders, woodcut ornaments, no screen chrome at all.
3. **Minimal cinematic horror**: near-black, one accent (dried-blood red or bone white), thin hairline rules, small caps letterspaced, lots of negative space, text fades in; think modern AAA horror menus.
4. **Found-footage / VHS instrument**: the player's own gear: night-vision green/amber readouts, scanlines, timecode, tape-counter, EMF-style bars; documents shown as photographed pages under a torch.
5. **Gothic illuminated**: blackletter display + humanist text, gold leaf on black, red rubrication, borders from the castle's stonework, moons and bats as motifs; chapter-page feel.
(Replace any axis with your own if it is stronger. Never a generic "fantasy RPG" frame.)

## Screens to mock per variation (1920x1080, PNG, real text)
Use the REAL strings below so the owner judges real screens.
- **Document (castle ledger page)** — kicker "BLOOD LEDGER", title "The steward", body:
  "Every midnight the chapel bell rings with no hand on the rope, and every midnight one of us is written out of the book.\n\nI hid the silver blade where he cannot bear to look: in the reliquary box below, under the marks his own claws left. Light the ash and you will see it." Close affordance: ESC.
- **Document (Bellwether note with photo)** — title "Family photograph", a 4:3 photo placeholder, transcript column: "Four of them on the porch, summer. The youngest is holding the music box. Someone has scratched out the father's face."
- **Journal** — list of recovered documents (castle: "Gate keeper's letter", "Ledger: the steward", "Ledger: the portrait", "Ledger: the orrery", "Altar inscription"), a dread pulse trace (hand-drawn heartbeat line), three conclusion pickers (Anchor / Pattern / Closure) and a "Close the case" action.
- **In-play HUD** — minimal: objective line ("Find the Blood Ledger: steward · portrait · orrery — rooms left: chapel, dining hall"), interaction prompt ("Open Great door [E]"), a subtitle/notice ("The door is held."), stamina/lantern state, hunt warning state ("BREAK LINE OF SIGHT · hide [E] · lantern off [F] · stay quiet"). Show it over a dark screenshot-like plate (paint a dark gradient; do not need the real game frame).
- **Title + pause/settings** — game title "HOLLOW CURRENT", map cards "14 Bellwether Lane" / "Haunted Castle", menu items Continue / New case / Settings / Quit; settings rows: Look sensitivity slider, Volume, Windowed, Invert Y.
- **Ending card** — "CASE CLOSED" and "SIGNAL LOST" variants with a one-line epitaph.

## Hard constraints
- Everything must be redistributable: fonts **SIL OFL only** (Google Fonts GitHub: e.g. IM Fell English, Cinzel, UnifrakturMaguntia, Special Elite, Courier Prime, Caveat, La Belle Aurore, Cormorant, EB Garamond, Share Tech Mono, VT323) with licence files copied; textures/ornaments procedural (Python/PIL, SVG, or HTML/CSS rendered to PNG) or CC0. No stock images, no Apple system fonts, no copyrighted game UI assets. Log every asset + licence in the variant's `ASSETS.md`.
- Readability: body text ≥ 30 px at 1080p, ≥ 4.5:1 contrast on its backing, max ~70 characters per line, no text over ornaments or stains. The game is dark; the UI must stay legible on a near-black scene.
- Reproducible: each variant has `render.py` (or `index.html` + a render script) that regenerates all its PNGs from the text above; no hand-painted one-offs.
- The engine draws UI with Unity IMGUI (`Assets/HollowCurrent/Runtime/Interface.cs`, `TitleScreen.cs`): flat rects, textures, one dynamic font per style, no shaders in UI. Keep each direction implementable with textures + fonts + rects (9-slice frames, paper sheets, icon atlas). Note in the style sheet what would need a texture vs. what is pure text.

## Per-variant deliverables
```
docs/haunted-castle/ui/variants/<slug>/
  README.md        direction in 5 lines; palette (hex) ; type (display/body/mono, sizes, tracking); frame/paper recipe; motif list; what is texture vs. code
  ASSETS.md        every font/texture with source URL + licence
  render.py        regenerates the PNGs
  document_castle.png  document_bellwether.png  journal.png  hud.png  title.png  settings.png  ending_closed.png  ending_lost.png
  fonts/           the OFL .ttf files + OFL.txt
  textures/        papers, frames, icons (PNG, ≤ 2048 px)
docs/haunted-castle/ui/variants/CONTACT_SHEET.png   all variants' document + HUD side by side, labelled
docs/haunted-castle/ui/variants/README.md           index + your own ranking with one sentence each
```

## References in the repo
- Current (disliked) document screen: see the description above; the castle title key art is `Assets/HollowCurrent/Resources/UI/CastleBackdrop.png`, Bellwether's `MenuBackdrop.png`; castle concept refs in `docs/haunted-castle/refs/`; a first "vellum" replacement already built by another agent is in `docs/haunted-castle/rounds/r13/docui_mock_*.png` (branch hc/fb-docui) — treat it as variant 0 and do better or different.
- Game copy, map names and goals: `Assets/HollowCurrent/Core/CaseBook.cs`.

Do not touch C#, scenes, audio or the other Codex task (`CODEX_HANDOFF.md`, wall art). Commit and push `codex/ui-variants`; put the final ranking and the contact sheet path in your last message.
