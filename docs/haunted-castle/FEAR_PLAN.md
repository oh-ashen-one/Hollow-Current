# Haunted Castle: fear plan from the ghost-horror research pack

Source: `docs/research/phasmophobia-horror/` (PR #2, merged to `main` 2026-10-13). Each item below cites the
research point it comes from and names the code it touches. Written 2026-10-13 against branch `haunted-castle`
at `2c9a29b`. Owner decides the order; waves are a suggestion.

## What the castle does today (the gap)

- **The hunt is fully predictable.** `Investigation.Tick`: 95 s in Disturbance with two pieces of evidence, then a
  9 s Warning (every candle strobes, dispatch says "Get under cover"), then a 38 s hunt, then 32 s of guaranteed
  quiet. You can only die inside those 38 s. The research names a single reliable danger tell as the number one
  fear killer (README Part 3 #1, notes/04 §2.1).
- **There is no sanity.** Nothing the player does between hunts changes the odds (README Part 2 A.2, notes/05 §9).
- **Hidden with the torch off is a guaranteed save.** He searches 0.7 m away and leaves. The research's scariest
  ghosts are the ones that break the hiding contract (notes/01 §5, notes/04 §1.5).
- **Between hunts the Count does not exist.** `CountPhaseChanged` disables him outside Hunt/Dead; only the generic
  `ScareDirector` apparitions (every 105–165 s) show him. The design said "seen perched". Players ask for
  peripheral, consequence-free sightings most of all (README Part 4).
- **Prop throws never fire in the castle.** `ScareDirector.Initialize` collects only `Authored / Mug`; the castle has
  none, so every second apparition silently skips its throw.
- **Music plays the whole time** (`music_house` at 0.28 inside, `music_hunt` in hunts). The research's consensus is
  no music, silence as the soundtrack, rare one-off audio (notes/05 §5, notes/04 §1.3).
- **The escape is the quietest phase.** In Return the director stops and the Count is parked. Every comparable
  game's "better than Phasmo" claim is a hostile endgame after the mystery is solved (notes/02 cross-game #3, #9).
- **Hunt voices are half wired.** `cc_hunt_run`, `cc_hunt_found`, `cc_hunt_hungry` exist on disk and are never
  played; `voice_cc_kit` and `voice_cc_ending` are missing (audio bugs are severity 1: README Part 3 #6).

Constraint for every item: `Investigation.cs` and `ScareDirector.cs` are shared with Bellwether, which is Hari's
shipped game. New behaviour goes behind per-map rules (a `HuntRules` data object on `CaseBook`/`HunterProfile`
whose Bellwether values equal today's constants) so the existing `RulesTests` keep passing unchanged.

## Wave A: rules (pure C#, testable without Unity)

1. **Dread meter (sanity).** `Investigation.Dread` 0–100. Rises 0.12/s with the torch as the only light (no lit
   candle within 6 m), 0.05/s otherwise inside; +12 per apparition, +8 per thrown prop, +15 when he is seen, +20 per
   grab survived; falls only in the gatehouse (0.5/s) and at the chapel fire. Never shown as a number: the journal
   shows a hand-drawn pulse line with ±2 noise. (README A.2; notes/03 #1 numbers.)
2. **Dice-roll hunts instead of the 95 s timer.** Every 2–6 s while Dread ≥ 40: roll; chance 1-in-12 at 40–60,
   1-in-6 at 60–80, 1-in-4 above 80. Evidence count multiplies the roll (progress escalates danger: notes/01 §12.1,
   Devour in notes/02). Minimum spacing between hunts stays (ScareRhythm). Bell summons unchanged.
3. **Break the Warning contract.** One hunt in four has no Warning (the "Demon" rule); one Warning in four is a false
   alarm (candles strobe, radio static, nothing comes). An apparition that lands within 4 m sometimes converts into
   a hunt with a 1.5 s tell (notes/04 §1.6, §3.2).
4. **Hostile Return.** When the seal completes, the drawbridge takes the full 7.9 s chain clip to lower and the
   Count hunts at straight-glide speed, no tell, until you cross; dying on the bridge is a kill. (Forewarned's exit
   sprint, notes/02 §3.)
5. **Low Dread lies.** Above 70: the compass spins at nothing (reuse `ScareDirector.FalseReading`), the ear-trumpet
   returns a phantom `cc_listen`, one in three candle tells points at an empty doorway, footsteps behind you. Above
   90: a hide spot refuses to open ("the door is held"). (README A.2, notes/04 §3.4.)
6. **"Long Night" dial** on Field Settings: no Warning phase, start Dread 50, hunts 50 s, perch 2–5 s, two hide spots
   nailed shut. Players build this themselves otherwise (notes/04 §3.11).
7. **Death costs something.** On a checkpoint restore the Count has moved the next relic or ledger page to another
   hiding place and Dread restarts at 60. (notes/01 §9.)

## Wave B: castle runtime (Sonnet builders, Opus for Presence)

8. **Castle throwables.** Tag goblets, candlesticks, books and plates as throwable (the `Authored / Mug` path);
   a one-in-six "poltergeist" variant throws everything on the table at once. (notes/03 power table.)
9. **Perch sightings between hunts.** He is active and perched between hunts but only on perches outside the
   player's view cone; he changes perch only while unobserved (SCP-173 rule, notes/03 #3). Consequence-free beats:
   his silhouette crossing a moon window, a shape on the gallery balustrade that is gone when you look, wingbeats
   passing overhead with no model, every candle in the hall bending toward one door then relaxing.
10. **The cupboard near-miss.** When you hide and he has not seen you, one hunt in three he lands, crawls to the
    cupboard, breathes at the slats (`cc_hunt_smell`, 0.5 m), rattles the door for 3 s and leaves. If he witnessed
    you hide (existing `witnessedHide`) he opens it. The gate keeper's letter already warns of this. (notes/01 §5.)
11. **Safe-zone violation, once per run.** After the first survived hunt, if you stay in the gatehouse longer than
    40 s he lands on the battlement above the cart, says `cc_stay`, the chains rattle, and he leaves. No kill.
    (notes/02 cross-game #4; truck crucifix story in notes/04 §1.6.)
12. **Silence instead of music.** Castle inside bed = `candle_whisper_bed` + wind + stone room tone; `music_house`
    volume 0 in the castle; hunts get heartbeat + wingbeats, hunt music at most 0.15. One rare line (`cc_listen`)
    plays at most once per run, in one run of three, positioned behind and above the player. (notes/05 §5.)
13. **Wire the hunt voices.** `cc_hunt_hungry` on search, `cc_hunt_run` on chase start, `cc_hunt_found` on regaining
    line of sight within 6 m. Generate `voice_cc_kit` and `voice_cc_ending`. Count footsteps/claws at 5 % volume
    outside hunts, full inside (notes/03 #1).
14. **Within-match learning.** After a survived hunt his first glide goes to where he last saw you, and his perch
    choice favours the floor you spent the most time on. (ScreenRant/RPS beta behaviour, notes/01 §10.)
15. **Castle dread beats** replacing the generic step/knock/radio set: a book drops from a floater, one knife leaves
    the orbit and sticks in the table, a bat crosses the torch beam, the chapel bell tolls once by itself (a false
    tell, because ringing it is a summons).

## Wave C: polish

16. **Hunt visibility.** Drop the permanent rim light during hunts; he is lit only by candles, moon and torch, so he
    flickers in and out of the dark; the torch fails (`TorchFailed`) for the last second of a dive. (notes/01 §4,
    "visible 31 % of the hunt".)
17. **Grab variants.** Two more kill presentations: a drop from the vault above, and a crawl grab through a service
    door. Identical kill animations are a repeated negative review (README Part 3 #4).
18. **Opt-in gamble (cursed possession).** Reading a ledger page aloud at the sarcophagus gives the relic's exact
    room, costs 25 Dread and has a one-in-three chance of an immediate tell-less hunt. (notes/01 §8.)
19. **Microphone, opt-in only.** Unity `Microphone` RMS above a threshold during a hunt calls `Creature.Hear` at
    the player with a 9 m radius. Default off, a Field Settings toggle, because the first use raises the macOS
    microphone permission dialog; automated playtests never enable it. Single-player value is lower than in co-op,
    so this is last. (notes/01 §6.)

## Not taken from the research

Co-op, proximity voice, dead-player spectating, a ghost roster: the castle has one hunter and one player. Equipment
tiers and grind: the research marks them as disliked.
