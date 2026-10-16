# Owner playtest feedback — 2026-10-15 (build e80fa28, haunted-castle 10d43da)

Hari played the castle ~5 min himself. Verbatim-ish items, all to be fixed in this wave.

## Bugs / interaction
1. **Walking ding**: a dot/ding pops every step while walking. Remove it entirely; the shuffle footstep is enough.
2. **Shuffle too loud**: the trouser-shuffle footstep is far too loud. Lower it substantially.
3. **Great Hall doors invisible** until E is pressed. Player hits an invisible wall in the doorcase, presses E, then the door appears.
4. **Door opens into the wall**, not outward toward the player. Doors should swing away from the player / open outward.
5. **Door pushes the player**: when a door opens it moves the player. The door must pass through the player (no player collision while swinging).
6. **Hall armoire E does nothing.**
7. **Hiding broken**: every hide spot (armoire, etc.) does nothing on E. You never go inside, you cannot hide.
8. **Foyer skeleton** is stuck between three columns. Move it into a clear section or resize it so it is the centrepiece you walk into, clipping nothing.
9. **Chapel too crowded**: cannot walk around, keep bumping into pews and the blood chalice. Thin it out hard.

## Presentation
10. **Document / ledger UI is ugly** ("this letter shit"): the parchment overlay with the big serif title, "BLOOD LEDGER / THE STEWARD" header, FILE & CLOSE button. Needs a proper UI redesign for the whole document/journal presentation.
11. **Voice is really bad** (eSpeak): the spoken lines sound like "some weirdo talking". Replace with a much scarier, much more human-sounding voice.

## Design / fear (the big one)
12. After 5 min: no idea what to do or where to go. Nothing scary happened. **Too well lit.** Feels like exploring, not being hunted. No pressure, no "being watched", no sense something bad is about to happen. The first map (Bellwether) had that; the castle does not. Only random voice lines.
    - Needs: clear early objective/direction, darker lighting, early stalking pressure (watched/followed cues) well before the first hunt, the first-map dread layer ported.

## Added later (2026-10-15)
13. **No speaker voice in the castle.** Verbatim: "Whatever voice is saying 'I am so very hungry', we need to fix that and get rid of it. There's no need for that. Nothing should be talked about as if it's a f***ing speaker throughout the entire house. That ruins the scary effect of the game."
    - Delete `cc_hunt_hungry`; no 2D speech in the castle at all (kit/arrival/hints/contain/ending/dispatch/ledger readings gone, cards stay); the Count only speaks diegetically from his head, near her, rarely. Piece `fb-voice2` (`hc/fb-voice2`).
