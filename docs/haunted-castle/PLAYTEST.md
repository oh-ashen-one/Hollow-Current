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

- **Bellwether bug fix for you to accept:** in both maps, the interaction ray now also finds an interactable that sits up to 12 cm behind the first thing it hits.
  - Before this, the Bellwether hall coat cupboard could not be focused from some angles. Its hide trigger sits in the same plane as its solid lower door (added in `89dac7e` on `main`), and Bellwether's own automated route (`-hc-verify`) failed there. It passes now.
  - Nothing else in Bellwether changed. The rules fuzz stays identical.
- **Apparitions are rare in natural play:** one in a 10-minute run, so props rarely fly by themselves. Being looked into.
- **Wings at the start of a no-tell hunt:** when a no-tell hunt starts, his wings can be heard as he takes off. That is the only audible change in its first seconds.
- **Long Night:** the bell starts the hunt at once, by design.
- **Sword:** the left hand does not hold the hilt yet.
- **Rough edges:** the ledger and UI art are generated placeholders, and the castle portraits await Codex's paintings.

## Where to write notes

Write in `docs/haunted-castle/playtest/OWNER_NOTES.md` (create it), or tell the session directly. Timestamps or screenshots help (Cmd+Shift+4).

## Build

- **Commit:** `0b08422` on `haunted-castle` (source `3bff23e`). It is the combined player with both maps.
  - It opens as a 1920x1080 window, never full screen.
  - Automated test runs no longer take your mouse.
- **Since the last build (wave G and wave H fear fixes):**
  - **No hunt "lamp":**
    - The HUD objective, the music, the heartbeat and the instrument no longer flip when a hunt starts.
    - The radio is unreliable during a hunt, and the countdown no longer shows a number.
    - Recording evidence, the ear-trumpet, the journal, the brazier and the bell work the same in and out of a hunt.
    - The instruments are disturbed only when he is close and in sight.
  - **The dice and the timing:**
    - Hunts can come at any Dread, even low.
    - Hunt and recovery lengths vary, and a recovery can be cut short at high Dread.
    - The heartbeat and the journal pulse follow a lagging, wobbling, sometimes lying "felt" Dread.
  - **Hiding is fallible:**
    - If he sees you hide, he comes and opens it, even with your torch off.
    - At high Dread a near-miss can turn into an opening.
  - **The escape:**
    - Sealing the bell sets him loose. He swoops over the courtyard (the swoop now detours around the wall).
    - The bridge is lethal.
    - A dispatch card and voice announce it.
  - **The scare layer:**
    - It keeps going through hunts and recoveries (ambient beats).
    - Silence is an active beat that ducks the wind, room tone and heartbeat.
    - Lies include footsteps behind you and a phantom whisper.
  - **The title screen:** the fog seam is fixed.
- **Automated proof:** see `docs/haunted-castle/playtest/AUTO_RUN.md`.
  - Escape, Long Night and a natural (unforced) run all win.
  - The forced-dice fear run proves: throws and the poltergeist, lies and silence, a false warning, a watched hide opened with the torch off, the swoop and the lethal bridge.
  - Also proven: the low-Dread hunt, the near-miss with an open, the recovery cut, and death with a checkpoint retry.
  - Bellwether's own route passes.
  - Great hall: 11.15 ms mean frame time at 1920x1080.
