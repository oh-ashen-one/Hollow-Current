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

## Build wave F (owner decision 2026-10-13: build all 19)

Rules from `LOOP.md` apply (worktrees under `.wt/<piece>`, branch `hc/<piece>`, `python3 tools/check_source.py`
before every commit, no Unity except the integrator, report under 300 words, rewrite `HANDOFF.md`). Shared files
stay with the integrator. Bellwether must play exactly as before: every new behaviour is gated by `HuntRules` or
`Game.Map==MapId.Castle`.

| Piece | Branch | Model | Owns | Items |
|---|---|---|---|---|
| F0 integrator | main clone `haunted-castle` | opus-builder | `Game.cs`, `Interface.cs`, `Soundscape.cs`, `Investigator.cs`, `ScareDirector.cs`, `Editor/*`, merges, build, playtest driver | 6 (UI), 12 (mixer), 13 (5 % footsteps), 16 (torch fail), 19 (mic), pulse line in journal (1), door held (5) |
| F1 rules | `hc/dread` | opus-builder | `Core/Investigation.cs`, new `Core/HuntRules.cs`, `Core/CaseBook.cs` (Rules field only), `Tests/RulesTests.cs` (append) | 1, 2, 3, 4 (rules), 5 (thresholds), 6 (rules), 7 (rules) |
| F2 count | `hc/count11` | opus-builder | `Runtime/Presence.cs` (castle branch only), `Runtime/Castle/CountClaudeProfile.cs`, `Runtime/Castle/CastleRun.cs` | 4 (runtime), 9 (perch AI), 10, 11, 13 (voice triggers), 14, 16 (rim light), 17 (play the new clips), 18 |
| F3 scares | `hc/scares` | sonnet-builder | new `Runtime/Castle/CastleScares.cs`, `Runtime/Castle/Floater.cs`, `Runtime/Castle/CastleWorld.cs`, `Runtime/Castle/WindowMoon.cs`, `Runtime/Castle/SetPiece.cs`, `Runtime/Castle/KnifeOrbit.cs` | 5 (false candle tell, false compass hook), 7 (alternate relic places), 8, 9 (window silhouette, balustrade shape), 15 |
| F4 audio | `hc/audio2` | sonnet-builder | `Resources/CastleAudio/**`, `tools/voice-lines.json` (append), `tools/audio_*.py`, `docs/haunted-castle/AUDIO.md`, `ASSET_LOG.md` (append) | 12 (new loops), 13 (missing voices), clips for 10, 11, 15, 17 |
| F5 grabs | `hc/grabs` | sonnet-builder | `tools/count_*.py`, `Resources/Characters/CountClaude/**`, `Editor/CountClaudeImport.cs` | 17 (two new grab clips) |
| F6 critic | reads only | opus-critic | `docs/haunted-castle/rounds/r10/**` | blind verdict after integration |

### Contracts (agreed now so pieces build in parallel)

**HuntRules (F1, `Core/HuntRules.cs`)**: a plain data class, `HuntRules.Bellwether` holds today's constants exactly
(warning 9 s, hunt 38 s, recovery 32 s, timed hunt at 95 s with two evidence, no dread, no lies, no hostile return),
`HuntRules.Castle` the new values, `HuntRules.LongNight` the dial. `CaseBook.Rules` points at the map's rules;
`Investigation(CaseBook)` reads them. Fields at least: `TimedHuntSeconds` (0 = dice only), `DiceIntervalMin/Max`,
`DiceChanceAt40/60/80`, `WarningSeconds`, `HuntSeconds`, `RecoverySeconds`, `NoTellChance`, `FalseWarningChance`,
`EventEscalationChance`, `HostileReturn`, `DreadDarkRate`, `DreadInsideRate`, `DreadSafeRecovery`, `DreadStart`,
`LieThreshold` (70), `DoorHeldThreshold` (90), `DeathPenalty`, `NailedHides` (0 or 2).

**Investigation API (F1)**: `float Dread` 0–100; `void AddDread(float)`; `void Tick(float dt,bool inside)` keeps
working for the tests, plus `Tick(float dt,bool inside,bool litNearby,bool inSafe)` used by Game; `bool Lies`
(Dread ≥ LieThreshold); `bool DoorHeld` (Dread ≥ DoorHeldThreshold); `bool FalseWarning` (this Warning will not
become a hunt); `bool HuntActive` (Phase Hunt, or Phase Return with HostileReturn and the bridge not yet crossed);
`Die()` allowed whenever `HuntActive`; `int Deaths`; `bool NoTellHunt` (this hunt skipped Warning). Dice rolls use an
injected `Func<float> random` (default `null` = deterministic 0.5 so tests stay deterministic); Game passes
`UnityEngine.Random.value`. `Investigation.Checkpoint` gains `Dread` and `Deaths`. `MysteryReady` and `Record`
treat FalseWarning like Disturbance.

**Investigation API, wave G additions (hc/rules2, critic r10 static #1-#3, #10, #12, #15)**:
- `float FeltDread` (0–100, read-only): the only Dread reading a player-facing channel may use. It is `Dread` lagged
  by a per-run time constant `FeltLagSeconds` (20–30 s, exponential), plus slow value noise of ±12 (one knot every
  6 s, a pure hash seeded once per run, so it never consumes dice), plus lie episodes (wave H, below; the wave G
  fade-to-calm while `Lies` is gone). It never reads the run's boundary, so no heartbeat or pulse level
  marks where the dice arm. Bellwether: always 0. The heartbeat (`Soundscape`) and the journal pulse (`Interface`)
  must read `FeltDread`, never `Dread`, and must not add a threshold of their own (critic #2, #3).
- `float RunDreadMin`: this run's boundary for the banded dice and the 240 s floor, drawn 30–50 at construction
  (`HuntDreadMin` ± `HuntDreadMinSpread`) and kept through checkpoints. Diagnostics/logs only.
- After the first hunt the dice roll at every Dread level: below `RunDreadMin` at a flat `LowDreadChance` (1/40) per
  roll (critic #1).
- `float CurrentHuntSeconds`, `float CurrentRecoverySeconds`: drawn per hunt (25–50 s, median 38) and per Recovery
  (10–40 s, median 32). At Dread ≥ 80 the dice also roll in Recovery at 1/8 (`RecoveryDiceChance`). The castle no
  longer says `RecoveryMessage` (`HuntRules.AnnounceRecovery` false). UI and dispatch text must read these values
  or say nothing; never print 38, 32 or "nine seconds" for the castle (critic #10).
- No-tell starts (dice, gamble `BeginNoTellHunt`, Long Night bell/escalation) leave `BreakerOn` unchanged; only a
  told Warning cuts it (critic #12).
- The first-hunt guarantee counts from the record that reaches two pieces of evidence, not from Disturbance entry
  (critic #15).
- Construction draws, castle only, in order: boundary, felt lag, noise seed (Bellwether draws nothing). Per hunt:
  hunt length right after the tell rolls; recovery length when the hunt ends. Crossing `RunDreadMin` restarts the
  dice interval, so a queued sequence starting with an interval roll still lines up after forcing Dread upward.

**Investigation API, wave H additions (hc/rules3, critic r10b N6, N7, N10, N11, N13)**:
- **FeltDread lie episodes (N6).** Scheduled from the run's noise seed (never from dice): one starts every 200–600 s
  (start to start, so 1–3 per 10 minutes) and lasts 20–40 s. At Dread ≥ 40 an episode can drift FeltDread toward
  calm (12); at Dread < 60 it can drift it up to a false spike (65–85); between 40 and 60 the kind is a coin flip.
  The drift uses the same 20–30 s lag, then FeltDread returns to Dread. `Lies` (Dread ≥ 70) no longer changes
  FeltDread at all, so a fading heartbeat is not a "Dread ≥ 70" readout, and a rising one is not proof of danger.
  Diagnostics: `string FeltEpisode` ("none", "calm", "spike"), `int FeltEpisodes`. A checkpoint restore reschedules
  the next episode from the restored time. Silence frequency in `CastleScares` must key on `Case.FeltDread` (N11).
- **No refuge phase (N7).** After the first hunt, Survey and Containment roll flat dice at `QuietPhaseChance` 1/80 per
  roll; Recovery below Dread 80 rolls at `RecoveryLowChance` 1/16 (1/8 at ≥ 80 as before). A threat from Containment
  returns to Containment (a told one cuts the brazier again). `Phase ThreatFrom` names the phase a threat started
  from (Disturbance, Recovery, Survey, Containment; Containment for the hostile Return); log it in `HC_HUNT_START`.
- **Drawn warnings (N10).** Every told or false Warning draws `CurrentWarningSeconds` 5–14 s (median 9;
  `WarningSecondsMin/Max`), so length never identifies a false alarm (tested: same distribution). Escalation keeps
  its 1.5 s tell and sets `bool EscalatedWarning` before `Changed` fires; never infer escalation from the length.
- **Driver determinism (N13).** `void ForceDread(float target)` (automated driver only) sets Dread exactly and restarts
  the dice schedule, so a queued sequence always reads: interval, hunt roll, tell roll, false-alarm roll (dice only),
  warning length (told or false), hunt length (at hunt start); the recovery length is drawn when the hunt ends.
- **Bellwether proof (N12).** `python3 tools/bellwether_fuzz.py` compiles the checked-in c4b24c1 Core
  (`Tests/BellwetherReference/`) and the current Core against `Tests/BellwetherFuzz.cs`, runs 4,000 seeds x 300
  random operations with live random dice, and exits 1 on any difference.

**Investigation API, wave I additions (hc/rules4, blind critic r10 P3: runaway Dread, hunts 52–63 % of play)**:
- **Two kinds of Dread.** What `AddDread` adds (apparitions, throws, seen, grab survived, gamble) is event Dread
  (`DreadEvent`) and decays: 0.4/s in Recovery, 0.15/s in Survey/Disturbance/Containment, none in Warning/Hunt/Return,
  times `DreadRelief` (Game sets 3 in the gatehouse, 2 at the chapel fire). After the first hunt the decay stops at a
  total of 20. Dread from darkness and time (and `DreadStart`, `ForceDread`) does not decay; only the safe zone
  lowers it (0.5/s). Checkpoints keep the event part; the death penalty's Dread above the saved ambient part decays.
- **Per-hunt cap.** During a hunt or the hostile Return, `AddDread` adds at most `HuntDreadCap` (25) in total,
  whoever calls it (`HuntDreadAdded`).
- **Post-hunt grace.** When a hunt ends the rules draw the recovery length, then a grace of 30–90 s
  (`GraceSeconds`, `GraceRemaining`). Until it runs out the banded, low-Dread, quiet-phase and Recovery dice and
  `TryEscalate` are disarmed; the bell and the gamble still work. `ForceDread` (driver only) clears it.
- **`float HuntShare`**: share of the last 10 minutes (or the run so far) spent in Warning, Hunt or the hostile
  Return. Observation only; logged in `HC_RULES` every 30 s for proof runs.
- **Session model** (`RulesTests.Session`, 2,000 seeds of 10 minutes): hunt + warning time after the first hunt
  27 % on average (p90 38 %), 10-minute `HuntShare` p50 20 % / p90 28 %, quiet gaps 42 / 101 / 212 s (p10 / p50 /
  p90), pooled CV 0.61; the same model without wave I gives 62 % and Dread ≥ 95 for 177 s per run.

**Investigation API, wave J additions (hc/rules5, runtime r11: immortal driver in the open, HuntShare 0.362)**:
- **Pacing governor.** `GovernorShare` = threat time (Warning, Hunt, hostile Return) over the time since the first
  threat began, at most 10 minutes and at least 300 s. Above 0.25 the multiplier is 1 + 12 × (share − 0.25), at most 3:
  each post-hunt grace is drawn 30–90 s and multiplied by it, and the banded (high-Dread) dice odds are divided by it.
  When the share stayed below 0.12 for 3 minutes before a hunt began, that hunt's grace is × 0.8. It shifts odds and
  graces; it never disarms anything (with certain dice a hunt still follows the grace at the top multiplier), and the
  grace stays a random draw, so nothing reads as a timer. `Governor` (live), `GovernorAtLastGrace` (logged in
  `HC_RECOVERY governor=`), `GovernorShare`.
- **Session models, 2,000 seeds each:** mortal player, hunt + warning after the first hunt 25.0 % mean, p90 32.7 %;
  immortal driver in the open at Dread 100, 10-minute `HuntShare` 28.5 % mean, p90 32.5 % (40.0 % p50 without the
  governor).

**Investigation API, wave K additions (hc/rules6, r11 blind critic: sawtooth, no-tell only at high Dread, door-held never,
lost bridge kill)**:
- **Dread:** passive rise 0.03/s lit, 0.08/s dark (was 0.05/0.12); calm decay 0.3/s (was 0.15); above a total of 60
  the decay also takes ambient Dread (`DreadDecayAllAbove`), so a careful player brings a high Dread down anywhere.
- **No-tell:** a flat 0.35 roll on every rolled or forced hunt, independent of Dread (Long Night: every hunt).
- **First hunt:** `Draw(60, 95, 240)` s after the second record (`FirstHuntMin/Max`), still guaranteed.
- **Door held:** `TryDoorHeld()` per hide attempt rolls 1/6 at Dread ≥ 60 (injected dice); `DoorHeld` no longer
  blankets every hide. Log `HC_DOOR_HELD`.
- **Death first:** a `Die()` in the same tick as `FinishAtVan()` or `WinByKill()` (castle) replaces the win
  (`DeathOverrodeWin`; `Changed` fires Won then Dead). With `FrameSource` (Game: `Time.frameCount`) "same tick" means
  the same frame whatever the script order.
- **Escalation:** 0.35 in Disturbance (was 0.2), 0.15 in Recovery once the grace has run out, never during the grace.
- **Session models (2,000 seeds):** mortal share 0.163 mean / 0.224 p90, a quiet stretch ≥ 120 s after the first hunt in
  93.6 % of runs, Dread at 480 s p90 44.6, a no-tell hunt below Dread 70 in 56.5 % of runs, first hunt 65–215 s after
  the second record (p10–p90); the r11 driver (immortal, open, torch on) share 0.196 / 0.253, Dread at 480 s p50 59.7,
  p90 87.6.

**Investigation API, wave L additions (hc/rules-l, r12 critic: hunts chained to a metronomic apparition clock)**:
- **Apparition clock:** `ScareRhythm.CastleInterval(u1, u2)` (pure): 40 % short 45–70 s, else 70 s + a truncated exponential
  (mean 140 s, cut at 260 s); mean ≈ 110 s, CV ≈ 0.54. `rhythm.UseCastleClock(random)` makes every castle appearance draw
  the next (spacing 45 s) and draws the first as 0.6 × an interval (27–156 s). Bellwether keeps its fixed clock.
- **Escalation policy:** `EscalationChanceNow(dark)`: lit 0.15 / dark 0.25 in Disturbance and in Recovery after the grace;
  0 on the run's first apparition, 0 when either of the last two hunts was escalation-born (at most 1 in 3 consecutive),
  0 in the grace. `TryEscalate(dark)` rolls it; `EscalationBlock(dark)` names the reason; `LastHuntFromEscalation`.
- **Apparitions:** `NoteApparition()` counts the apparition and adds its Dread; the dice read `DiceDread`, which leaves that
  bump out for 20 s, so an apparition cannot arm or raise the dice by itself. Low-Dread roll 1/30 (was 1/40).
- **Door held:** a bag of 5 eligible attempts (Dread ≥ 60), one held at a random position (`DoorAttempts`, `DoorsHeld`).
- **Near-miss open (for Presence):** `Rules.NearMissOpenChance(dread)`: 2 % at 0, 5 % at 40, 25 % at 80, 40 % at 100.
- **Models (2,000 seeds):** hunts starting ≤ 10 s after an apparition 9 % (1 in 3 apparitions close) / 18 % (all close);
  apparition CV 0.52; 3-run first-hunt sets spanning ≥ 45 s 96 % / 93 %; share 0.159 / 0.165 mean (p90 0.22); a ≥ 120 s
  gap in 93 % / 91 % of runs.

**Presence castle branch (F2)**: reads `Game.Case.HuntActive` instead of `Phase==Hunt` everywhere in the castle branch;
between hunts the Count is active and visible on a perch outside the view cone, re-perches only when unobserved;
exposes `public bool Diving` (for the integrator's torch fail), `public event Action<string> Beat` with ids
`"near-miss"`, `"gatehouse"`, `"learned"` for logs; plays the F5 clips as Mode 8 `GrabDrop` and Mode 9 `GrabCrawl`
when `count.ModeGrabDrop/ModeGrabCrawl` ≥ 0 and the receipt lists them, otherwise the existing Mode 6 wing-wrap.
Hunt voices: `cc_hunt_hungry` on entering Search, `cc_hunt_run` on BeginChase, `cc_hunt_found` on LOS regained
within 6 m (each at most once per 8 s). Gatehouse event uses `Game.World.Castle.GatehouseBattlement` (F3 adds it to
`CastleWorld.Points`; until then, 3.5 m above the cart).

**CastleScares (F3, `Runtime/Castle/CastleScares.cs`)**: a MonoBehaviour with `Initialize(Game)`; the integrator adds
it in `Game` when the map is the castle and calls `Scares.Castle.Beat()` from `ScareDirector.Dread()` instead of the
generic set (so `ScareDirector.cs` changes by three lines). Throwables: CastleWorld tags small props with
`Interactable`-free marker component `Throwable` (new, in F3's file) that `ScareDirector.ThrowNearbyProp` also
accepts (integrator wires: `props` list gains every `Throwable`). `CastleWorld.Points` gains `RelicAltPositions[3]`
and `GatehouseBattlement`. `Floater.FalseTarget(Vector3 point,float seconds)` bends every flame toward a point
for a while (false candle tell). `CastleScares.FalseCompass` bool for the integrator's compass needle.

**Audio keys (F4)**: loops `heartbeat_loop`, `wind_battlements_loop`; one-shots `door_rattle`, `book_drop`,
`knife_stick`, `claw_steps_0..3`, `bat_pass`, `bell_single`; voices `voice_cc_kit`, `voice_cc_ending`. No paid
generation without the director's go-ahead; synthesise with `tools/audio_*.py` (numpy) like `audio_orrery_tick.py`,
voices with `generate_open_voice.py --castle`. Levels per `AUDIO.md`.

**Grab clips (F5)**: `Count_GrabDrop` (he falls from above onto the camera, 1.6 s) and `Count_GrabCrawl` (four-limbed
lunge from a door, 1.4 s) built the same way as the existing Grab in `tools/count_prepare.py`; receipt lists
`grab_drop_reach_m`, `grab_crawl_reach_m`; `CountClaudeImport` maps them to Animator Mode 8 and 9. Chest pitched
≥ 25° toward the lens and one claw filling ≥ 200 px at 2560 wide (round 8 critic bar).
