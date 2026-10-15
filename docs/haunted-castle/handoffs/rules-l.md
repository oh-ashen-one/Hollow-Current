# HANDOFF: piece "rules-l" (hc/rules-l), wave L

Branch `hc/rules-l`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/rules-l`, based on `haunted-castle` `af96343`. Model: Opus 5.5. Date 2026-10-14.
Scope: r12 blind playtest critic (`docs/haunted-castle/rounds/r12/CRITIC.md` §1, §3, §8), top finding "unchain hunts from the apparition clock", plus door-held and the near-miss open gate. Code only: no Unity, no game. Ran `tools/check_source.py`, `tools/check_castle_scares.py`, `tools/bellwether_fuzz.py` (Unity's bundled csc + mono and python, no engine). ScareDirector, Investigator, Game, Presence and CastleScares untouched (changes are in the integrator diff).

## Files touched
- `Assets/HollowCurrent/Core/ScareRhythm.cs`: castle clock (pure `CastleInterval`, `UseCastleClock`, `LastInterval`). Bellwether path unchanged.
- `Assets/HollowCurrent/Core/HuntRules.cs`, `Assets/HollowCurrent/Core/Investigation.cs`
- `Tests/RulesTests.cs`: `WaveLTests()`, `WaveLReport()`; the session model now draws apparitions from the castle clock, calls `NoteApparition` and `TryEscalate(dark)`, and records apparition and hunt-start times.
- `docs/haunted-castle/FEAR_PLAN.md`: "Investigation API, wave L additions".
- `docs/haunted-castle/handoffs/rules-l-integrator.diff` (not applied).

## What changed (castle only)
1. **Apparition clock** (`ScareRhythm.CastleInterval(u1,u2)`, pure): 40 % short (45–70 s uniform), else 70 s + truncated exponential (mean 140 s, cut at 260 s). 10,000 samples: mean 110.2 s, CV 0.540, min 45.0, p10 51, p50 90, p90 207, max 259.8. `UseCastleClock(random)` sets spacing 45 s, draws each next appearance in `RecordAppearance`, and draws the first as 0.6 × an interval (27–156 s; it was 65 s every run).
2. **Escalation policy** (`Investigation.EscalationChanceNow(dark)`, rolled by `TryEscalate(dark)`): lit 0.15 / dark 0.25 in Disturbance and in Recovery after the grace (was 0.35 / 0.15); 0 on the run's first apparition (`Apparitions` counted by `NoteApparition`); 0 when either of the last two hunts was escalation-born, so at most 1 escalation-born hunt in any 3 consecutive (`LastHuntFromEscalation`); 0 during the grace. `EscalationBlock(dark)` returns `ok | grace | phase | first_apparition | escalation_cap | terminal | rules_off`.
3. **Dice independent of apparitions:** `NoteApparition()` adds the apparition's Dread, and the dice (banded chance, boundary, floor clock, Recovery threshold) read `DiceDread`, which leaves that bump out for 20 s. With the first apparition never escalating, the first hunt comes from the 60–240 s dice window (or a dice roll). To keep the share above 15 % without the escalation hunts, the low-Dread roll went from 1/40 to 1/30 (1/25 dropped the ≥ 120 s-gap measure under 90 %).
4. **Door held:** the rule did return true (wave K tests), but per attempt at 1/6 a run with 5 eligible hides misses it 40 % of the time, and the critic runs never had 5 hides at Dread ≥ 60 (siege hid once for 12 minutes). Now a bag: of every 5 eligible attempts (Dread ≥ 60) exactly one is held at a random position, so any 5 eligible hides from the start of a run include one (tested over 1,000 seeds). `DoorAttempts` counts eligible attempts.
5. **Near-miss open:** HuntRules did not gate it; the "below 80 → open=False" gate is in `Presence.cs:1947` with `CountClaudeProfile` values (F2). Exposed `HuntRules.NearMissOpenChance(dread)`: 2 % at 0, 5 % at 40, 15 % at 60, 25 % at 80, 40 % at 100 (Bellwether 0); the Presence change is in the diff.

## API for the integrator (exact)
```csharp
// ScareRhythm
public static float CastleInterval(float u1, float u2);   // pure, 45-260 s
public void UseCastleClock(Func<float> random);           // castle: call once instead of Spacing/AppearanceBase/Spread
public float LastInterval { get; }                         // the last drawn interval (log HC_SCARE_CLOCK)
// Investigation
public void NoteApparition();                 // every visible castle apparition, in place of AddDread(Rules.DreadApparition), before TryEscalate
public int Apparitions { get; }
public float DiceDread { get; }               // Dread the dice read
public float EscalationChanceNow(bool dark);  // 0 = blocked
public string EscalationBlock(bool dark);
public bool TryEscalate(bool dark);           // TryEscalate() = TryEscalate(false)
public bool LastHuntFromEscalation { get; }
public int DoorAttempts { get; }
// HuntRules (Castle / Bellwether)
float EscalationLitChance, EscalationDarkChance; // 0.15, 0.25 / 0
float ApparitionDiceShadowSeconds;               // 20 / 0
int DoorHeldBag;                                 // 5 / 0
float NearMissOpen0, NearMissOpen40, NearMissOpen80, NearMissOpen100; // 0.02, 0.05, 0.25, 0.40 / 0
public float NearMissOpenChance(float dread);
// changed value: LowDreadChance 1/30 (was 1/40)
```
The exact calls (in `rules-l-integrator.diff`, applies with `patch -p1` on `af96343`, compiles, check_source 1100 and check_castle_scares PASS with it applied):
- `ScareDirector.cs:33`: `if(game.Map==MapId.Castle)rhythm.UseCastleClock(()=>Random.value);` (replaces the Spacing/Base/Spread line); log `HC_SCARE_CLOCK next_in=` after each `RecordAppearance`.
- `ScareDirector.Trigger` castle branch: `game.Case.NoteApparition();` replaces `game.Case.AddDread(game.Case.Rules.DreadApparition);` (keep it before `Castle.Escalate`).
- `CastleScares.Escalate` (F3): `bool dark=!game.LitNearby; float chance=c.EscalationChanceNow(dark); … escalated=c.TryEscalate(dark); … reason=c.EscalationBlock(dark);` and log `dark= apparitions= lastHuntEscalated=`.
- `Game.cs`: `public bool LitNearby=>litNearby;`.
- `Investigator.Hide` (door-held proof line): `HC_DOOR_TRY hide= held= attempt= dread=` on every eligible attempt, before the existing `HC_DOOR_HELD`.
- `Presence.cs:1947` (F2): unwatched near-miss chance = `Game.Case.Rules.NearMissOpenChance(dread)` (Long Night keeps its flat 0.25, watched stays 1).

## Test results
- `python3 tools/check_source.py` (INPUT_SYSTEM_REFERENCE = main clone's `Library/ScriptAssemblies/Unity.InputSystem.dll`): **PASS 1100** (1071 on `af96343`).
- `python3 tools/check_castle_scares.py`: **PASS** (11 beats, 5 in TryBeat, 2 lies, 30 throwable sites).
- `python3 tools/bellwether_fuzz.py`: **PASS**, 4,000 seeds × 300 steps, sha256 `ee3b74dc…fd68f2f`.
- Mutations: escalation on the first apparition, a 1-in-2 instead of 1-in-3 cap, no dice shadow, no door bag, a narrow interval draw, a fixed first appearance each fail a named assertion.
- Deterministic wave L tests: inter-arrival 10k samples (mean 110.2, CV 0.540, bounds); first castle apparition 27–156 s over 2,000 draws; spacing 45 s; never on the first apparition (with a certain roll); the 1-in-3 cap (scripted sequence, and 500 half-hour runs with an apparition every 20 s: no 3 consecutive hunts with 2 escalation-born); grace blocks; dice shadow (20 s, and 0 banded hunts from a bump over 400 runs); door bag (exactly 1 in the first 5 eligible attempts in 1,000 seeds; attempts below 60 do not count); near-miss curve values.
- **The r12 pass tests as 2,000-seed models** (mortal player; one in three apparitions close / every apparition close):

| measure | target | 1 in 3 close | all close |
|---|---|---|---|
| hunts starting ≤ 10 s after an apparition | ≤ 35 % | **9.1 %** (403/4417) | **17.5 %** (812/4629) |
| apparition inter-arrival CV | ≥ 0.5 | **0.52** (mean 112 s) | **0.52** |
| 3-run sets whose first hunts span ≥ 45 s | (3 runs ≥ 45 s) | **95.9 %** (first hunt p10/p50/p90 137/193/293 s) | **92.6 %** |
| share (Warning + Hunt + Return / 600 s) | 15–30 % | mean **0.159**, p10 0.092, p90 0.222 | mean **0.165**, p10 0.105, p90 0.223 |
| runs with a ≥ 120 s gap after the first hunt | ≥ 120 s | **92.6 %** | **91.4 %** |
| escalations per run | – | 0.12 | 0.32 |

  The mean share meets 15–30 %, but about a third of single runs fall under 15 % (p10 0.09–0.11). In the model the second record comes at 40–120 s; the critic's natural route records at ~8 s, which moves the first hunt earlier and raises the runtime share. Wave K driver model: share 0.177 mean, Dread at 480 s p90 76.3.
- Earlier assertions updated (premise changed): escalation-mechanism tests now note two apparitions first; wave K escalation rates (35 %/15 %) → 15 % lit / 25 % dark; door-held "about 1 in 6" → exactly 1 in 5 (bag); low-Dread roll constant 1/40 → 1/30.

## What a windowed run must prove (3 unforced `natural` runs)
- [ ] ≤ 35 % of non-Return `HC_HUNT_START` within 10 s after an `HC_SCARE visible=` line.
- [ ] `HC_SCARE_CLOCK next_in=` / apparition inter-arrival CV ≥ 0.5; the first apparition not at ~65 s every run.
- [ ] First hunt times span ≥ 45 s across the 3 runs; no `HC_ESCALATE escalated=True` with `apparitions=1`.
- [ ] Share 15–30 %, longest gap ≥ 120 s.
- [ ] In any run with ≥ 5 `HC_DOOR_TRY` lines (Dread ≥ 60), at least one `HC_DOOR_HELD`.
- [ ] Bellwether: `tools/bellwether_fuzz.py` PASS on the merge commit.
