# Haunted Castle: owner playtest

## Launch

- **Build:** `/Users/midir/hollow-current-haunted-castle/Builds/macOS/Hollow Current.app` (Finder: double-click). It contains both maps. Build commit: see "Build" at the bottom.
- **Choose the map:** on the title screen pick **Haunted Castle** (click it, press **2**, or select it with the D-pad and press Cross), then **BEGIN INVESTIGATION**. The choice is remembered.
- **Two copies of the project:** if two copies of the project are open in Unity, close them first. The build itself needs nothing else.

## Controls

| Action | Keyboard / mouse | DualSense |
|---|---|---|
| Move / look | WASD / mouse | left / right stick |
| Interact, hold to keep acting | E | Cross |
| Use the instrument (also the sword strike) | left mouse | R2 |
| Switch instrument (compass, lantern, ear-trumpet) | 1 / 2 / 3 | L1 / R1 |
| Torch | F | Square |
| Journal | J | Triangle |
| Crouch / sprint | Ctrl or C / Shift | Circle / L3 |
| Radio (asks dispatch for a hint) | R | L2 |
| Pause | Esc | Options |

## Two ways to win

Both start the same way: take the instruments from the lantern cart in the gatehouse and read the gate keeper's letter.

**Escape**
1. **Gather evidence:** find three kinds of evidence with the three instruments.
   - The compass spins where he lingers.
   - The grave-ash lantern shows claw marks on stone.
   - The ear-trumpet catches a whisper if you ask once and stay still.
2. **Read the ledger:** find and read the three Blood Ledger pages.
3. **Ring the chapel bell:** the bargain hears you. Hide and survive the hunt.
4. **Solve it in the journal (J):** what holds the castle, what repeats, and what to do.
5. **Seal it:** relight the chapel's fire, then hold the bell until the last stroke fades.
6. **Escape:** the drawbridge comes down. Walk out across it.

**Kill**
1. **Read the ledger:** the ledger pages also hint where three relics were hidden.
   - One is sealed in a way only the lantern can open.
   - One sits where a planet is missing.
   - One is behind a face.
2. **Forge:** bring all three to the altar and hold until the sword is forged.
3. **Fight:** ring the bell to call him. Strike him three times with the instrument button. After each clean hit he falls back to a perch, then returns lower and faster.

## What to look for (please note anything that feels off)

- **Dread pacing:** the quiet stretches, the small scares, and how fast the castle turns hostile.
- **Hunt readability:** can you tell when he is hunting, and where he is?
- **Count Claude:** is he frightening? Look at his silhouette, face, wings, and the perch, glide, dive and grab.
- **The candle tell:** floating flames lean toward him before you see him.
- **Doors:** do they swing cleanly? Can he follow through the narrow service doors (he crawls under low lintels)?
- **Forge and fight:** does the 9 s hold feel earned, are the hits readable, does his retreat make sense?
- **Drawbridge escape:** the moment it lowers and the walk out.
- **Look and performance:** stone, fog, moonlight, the two moons, and any hitch.

## Known issues

- **The Count is too passive:** in the automated runs he stayed on his balcony perch for whole hunts and did not grab an investigator standing next to him. The hunt may feel toothless; this is being fixed.
- **No sword model:** after forging you still hold the current instrument; the strike works but shows no blade.
- **Rough edges:** ledger and UI art are generated placeholders, and some textures are projected and mirrored on the Count.
- **Escape:** walking out over the bridge ends the case; the winch at the gate also ends it once the bell is sealed.
- **Bellwether:** the Bellwether map is unchanged; pick it on the title screen to compare.

## Where to write notes

Write in `docs/haunted-castle/playtest/OWNER_NOTES.md` (create it), or tell the session directly. Timestamps or screenshots help (Cmd+Shift+4).

## Build

- **Commit:** `080b3e1` on branch `haunted-castle` (combined player, both maps, built 2026-10-13).
- **Since the first automated run:**
  - The Count's materials are now mapped to the right parts of the model: membrane wings, body albedo and flesh (count9).
  - Candle flames are new and stay rigid on their wicks (flames9).
- **Automated proof:** both endings complete in this exact build (escape 82.8 s, kill 87.5 s, 0 blockers). See `docs/haunted-castle/playtest/AUTO_RUN.md`.
