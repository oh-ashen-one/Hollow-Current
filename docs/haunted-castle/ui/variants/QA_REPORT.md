# UI variants — verification record

Completed on the verified Mac Studio (Apple M3 Ultra), using CPU-only Pillow/NumPy rendering. The task uses the current Codex session; no Unity, Blender, game instance or other terminal session was operated.

## Mechanical evidence

`python3 verify.py` passes **5,776 checks** with zero errors; full machine-readable results are in `verification.json`.

- All five directions contain all eight required PNGs: **40 images at exactly 1920 × 1080**.
- All required ledger, photograph, journal, HUD, menu, settings and ending strings are present. Paragraph line breaks preserve the exact words and punctuation.
- All rendered characters are covered by the bundled fonts. All 17 font/licence files match the pinned Google Fonts SHA-256 manifest; every family includes its original OFL 1.1 text.
- All body-style text is at least 30 px, with document prose at 36–46 px. Document body lines are at most 70 characters. Text rectangles are inside the canvas and have no pairwise intersections.
- Minimum text contrast: Casefile **5.747:1**, Occult folio **5.367:1**, Last light **7.575:1**, Dead channel **8.269:1**, Two moons **9.456:1**. Every measured run exceeds 4.5:1. This measures foreground against the actual solid backing specified by the renderer, not antialiased edge pixels.
- All 30 texture images fit within 2048 px. Each family-photo placeholder has a 4:3 aspect ratio.
- Running the five individual `render.py` entry points and `package.py` reproduced **81 PNG/audit files byte-for-byte** by SHA-256, including the final comparison and five review sheets. No network is used for regeneration.

## Visual inspection and corrections

All 40 screens were inspected in the five labelled review sheets; the castle documents, casefile journal, VHS HUD and gothic photograph were also inspected at full size. The ten-panel comparison was inspected for direction separation, dark-scene legibility and consistent presentation.

Corrections made during that review:

1. Moved the casefile dread label/trace to clear the final recovered-document row.
2. Gave every text run a quiet backing so ornament, grain and scanlines cannot cross letters.
3. Corrected tracked-text baseline alignment, preserving natural ascenders/descenders.
4. Inset the occult conclusion arrows to clear its printed book border.
5. Made selected journal row numbers follow the inverse selection colours.
6. Differentiated the HUD objective framing: evidence slip, occult seal, restrained left rule, camcorder focus/timecode, and twin moons.

The material/typography distinctions remain coherent across all eight screens per direction. No mockup was accepted solely because the script completed. The recommendation order and tradeoffs are recorded in `README.md`.

## Scope and limits

These are static design proposals for owner selection, not Unity implementation. No C#, scene, audio, existing CODEX_HANDOFF.md or wall-art file was changed. The two scene plates are deliberately simplified procedural stand-ins, and the family image is an illustrated placeholder. Animation, interaction, imported Unity font rendering and runtime performance were not tested or claimed. The final artist/user preference remains the owner's decision; this is not a separate-model or user-test verdict.
