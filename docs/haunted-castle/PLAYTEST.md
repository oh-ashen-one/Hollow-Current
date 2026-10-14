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

- **Throwables:** in the automated run neither the single throw nor the poltergeist moved a prop, so `HC_THROW=0`. Props may not fly in your run. The cause is being probed.
- **Escape:** walking out over the bridge ends the case, and so does the winch at the gate once the bell is sealed. In the hostile return the bridge takes about 7 s to come down while he hunts.
- **Missing voice line:** "contain" has no voice (`voice_cc_contain`). The card text still shows.
- **Not checked automatically yet:** Long Night, the microphone, the rare whisper behind you, a held hiding-place door, and a relic moving after a death. The automated run cannot reach these.
- **Sword:** the left hand does not hold the hilt yet.
- **Rough edges:**
  - The ledger and UI art are generated placeholders.
  - Some textures are projected and mirrored on the Count.
  - The castle portraits await Codex's paintings (see `docs/haunted-castle/CODEX_HANDOFF.md`).
- **Bellwether:** the Bellwether map should play exactly as before. Its automated route has not been re-run on this build.

## Where to write notes

Write in `docs/haunted-castle/playtest/OWNER_NOTES.md` (create it), or tell the session directly. Timestamps or screenshots help (Cmd+Shift+4).

## Build

- **Commit:** `34a0de9` on branch `haunted-castle`. It is the combined player with both maps, built 2026-10-14 from `b5da27e` source.
- **Window:** it opens as a 1920x1080 window and never full screen. If an earlier run left it full screen, the first launch switches it back.
- **Since the last build:**
  - **Dread:** an unseen meter rises in the dark and falls in the gatehouse and at the chapel fire.
    - The journal shows it as a hand-drawn pulse line, never a number.
    - A heartbeat follows it.
  - **Hunts come on the dice once Dread is high, not on a timer.**
    - One hunt in four has no warning at all.
    - A dice warning can be a false alarm: candles strobe, the radio crackles, and nothing comes.
    - A close apparition can turn into a hunt after a 1.5 s tell.
  - **Lies above Dread 70:** the compass needle spins at nothing, and candles lean toward an empty doorway. Above Dread 90 a hiding-place door is held shut.
  - **Count Claude between hunts:**
    - He sits on perches you are not looking at, and moves only when you are not watching.
    - Once per run he lands on the gate wall above the cart if you shelter too long.
    - He learns where you like to be.
  - **In hunts:**
    - One time in three he crawls to your cupboard, breathes at the slats and leaves.
    - If he saw you hide, he opens it.
    - He speaks while he hunts. The torch dies for a second when he dives.
    - Two new grabs: one dropping from above, one crawling from a door.
  - **Hostile escape:** sealing the bell does not end the danger. He hunts you while the bridge comes down.
  - **Castle scares:**
    - Books fall, a knife leaves the orbit and sticks in the table, a bat crosses your torch, and the bell tolls once by itself.
    - A silhouette crosses a moon window; a shape on the gallery is gone when you look.
  - **Sound:** no house music in the castle; silence, wind and the heartbeat instead. A rare whisper may come from behind you once.
  - **CASTLE NIGHT page in Field Settings:**
    - **Long Night:** no warnings, darker start, two hiding places nailed shut.
    - **Microphone:** opt-in, off by default. macOS asks for permission the first time.
  - **The sarcophagus:** reading a ledger page aloud there names the relic's room, but it can wake him at once.
  - **After a death:** the next relic may have moved.
  - **New title screen** (castle key art, Cinzel type).
- **Automated proof:** see `docs/haunted-castle/playtest/AUTO_RUN.md`.
  - Escape, kill and a forced-dice fear run all complete in this build:
    - Escape: 85.8 s.
    - Kill: 95.6 s.
    - Fear run: 266.1 s. A false warning, a no-tell hunt, a watched-hide grab, the gatehouse visit and the hostile return all occurred.
  - Great hall: 11.15 ms mean frame time at 1920x1080.
