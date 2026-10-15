# HANDOFF: piece "rules-l3" (hc/rules-l3), wave L follow-up 2

Branch `hc/rules-l3`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/rules-l3`, based on `haunted-castle` `f432fe5`. Model: Opus 5.5. Date 2026-10-15.
Scope: the L2 build's three failing pass tests (pooled apparition CV 0.44, first hunts 89/104/119 s, natural2 longest gap 111 s). Core + tests only; the Runtime changes are in `rules-l3-integrator.diff` (not applied). No Unity, no game.

## Diagnosis
The realised gaps 108, 110, 95, 69, 69, 62 s were neither mode: the runtime blocks apparitions in Warning, Hunt, Recovery and the grace, so a short draw that expired inside a block fired at the block's end and became a mid-length gap. Two 10-minute runs only fit 2–4 apparitions, so the long mode rarely completed.

## What changed (castle only)
1. **Block model:** `ScareRhythm.Tick(dt, blocked)`. A due time that expires inside a block is not fired at the block's end. When the block lifts, a fresh 40–55 s gap is drawn (`Rescheduled` counts these). `AppearanceDue` is false while the last tick was blocked. `ScareRhythm.CastleBlocked(phase, graceRemaining, hidden, modal)` defines the block: Warning, Hunt, Recovery, grace, hidden, modal, Briefing, Containment, Return.
2. **Bursts and lulls:** a burst is 2–3 apparitions 40–55 s apart; its last apparition opens a lull of 150–280 s (`TakeLullStart()` reports it once). A run starts with a burst (first apparition 40–55 s in) or a lull. `CastleInterval` stays as a pure function, but the clock no longer uses it.
3. **Cross-run anti-repeat** (pure `RunHistory` / `RunPlan`): the first-hunt band (Early 60–100, Mid 110–150, Late 160–200 s after the second record) is one used in neither of the last two runs, so any three consecutive runs use all three bands and spread ≥ 60 s. The burst/lull start alternates. `Serialize()`/`Parse()` give a short string ("E,M,B") for PlayerPrefs. `Investigation.UseRunPlan(plan)` replaces the first-hunt window.
4. **Every run gets a ≥ 120 s gap:** the first hunt's end holds every dice roll, the floor and escalation for 120 s of non-threat time unless Dread ≥ 60 (`HoldDice`; the countdown stops in Warning and Hunt). A lull calls `HoldDiceForLull()`, which holds only until the run has had its long quiet stretch (`LongQuietDone`). With unconditional 120 s lull holds stacked on the first-hunt hold, the share p10 fell to 0.07–0.09 even at a 1/14 low roll. Escalation is also blocked before the first hunt, so the first hunt comes from its band. Low-Dread roll 1/15 (was 1/24) restores share p10 ≥ 0.13.
   `ForceDread` (driver only) also clears the dice hold.

## API (exact)
```csharp
// ScareRhythm
public void Tick(float dt, bool blocked);                     // Tick(dt) = Tick(dt, false)
public static bool CastleBlocked(Phase phase, float graceRemaining, bool hidden, bool modal);
public void UseCastleClock(Func<float> random, bool startWithBurst); // UseCastleClock(random) picks the start by a roll
public bool TakeLullStart();   public bool StartedWithBurst { get; }   public float LastLull { get; }   public int Rescheduled { get; }
public static float CastleBurstGap(float u), CastleLull(float u);  public static int CastleBurstSize(float u);
// pure planner (ScareRhythm.cs)
public enum FirstHuntBand { Early, Mid, Late }
public sealed class RunPlan { FirstHuntBand Band; bool BurstStart; float FirstHuntMin, FirstHuntMax; }
public sealed class RunHistory { RunPlan Next(float u1, float u2); void Push(RunPlan); string Serialize(); static RunHistory Parse(string); }
// Investigation
public void UseRunPlan(RunPlan plan);  public RunPlan Plan { get; }
public void HoldDice(float seconds);   public void HoldDiceForLull();   public float DiceHoldRemaining { get; }   public bool DiceHeld { get; }   public bool LongQuietDone { get; }
// HuntRules (Castle / Bellwether): EscalationNeedsFirstHunt true/false, LullDiceDread 60/0, FirstHuntAfterHold 120/0, LullHoldSeconds 120/0; LowDreadChance 1/15
```

## Integrator calls (`docs/haunted-castle/handoffs/rules-l3-integrator.diff`)
The diff applies with `patch -p1` on `f432fe5`. With it applied, check_source passes 1132 and check_castle_scares passes (run against the real Resources).
- `Game.cs` (persistence hook), after `Case` is built in the castle: `var history=RunHistory.Parse(PlayerPrefs.GetString("hc.castle.runs","")); RunPlan=history.Next(Random.value,Random.value); Case.UseRunPlan(RunPlan);`. Unless `restoreCheckpoint`: `history.Push(RunPlan); PlayerPrefs.SetString("hc.castle.runs",history.Serialize()); PlayerPrefs.Save();`. Log `HC_RUN_PLAN band= firstHunt= burstStart= history=`. Expose `public RunPlan RunPlan`.
- `ScareDirector.Initialize`: `rhythm.UseCastleClock(()=>Random.value, game.RunPlan!=null?game.RunPlan.BurstStart:Random.value<.5f); if(rhythm.TakeLullStart())game.Case.HoldDiceForLull();` and log `HC_SCARE_CLOCK start=`.
- `ScareDirector.Update`: `rhythm.Tick(dt, IsCastle&&ScareRhythm.CastleBlocked(game.Case.Phase,game.Case.GraceRemaining,game.Player.Hidden,game.Modal));`.
- Remove the castle `Rest(20–40)` after Recovery; the block model replaces it. Bellwether keeps `Rest(65)`.
- After `RecordAppearance` in `Trigger`: `if(rhythm.TakeLullStart())game.Case.HoldDiceForLull();` and log `HC_SCARE_CLOCK next_in= lull= rescheduled= diceHold=`.

## Results
- `python3 tools/check_source.py`: **PASS 1132** (1103 at `f432fe5`). `python3 tools/check_castle_scares.py`: **PASS**. `python3 tools/bellwether_fuzz.py`: **PASS** (4,000 seeds, sha256 `ee3b74dc…fd68f2f`).
- Mutations caught (each fails an assertion): firing at the block's end, no anti-repeat, no hold after the first hunt, and escalation before the first hunt.
- **Simulation:** critic route (records by ~15 s), block model, burst/lull clock, 2,000 consecutive runs sharing one RunHistory.

| measure | target | 1 in 3 close | all close |
|---|---|---|---|
| 3-run pooled apparition CV ≥ 0.5 | (build 0.44) | **96.4 %** of sets (CV p10 0.56, p50 0.70; 2.9 apparitions/run) | **96.4 %** (p10 0.55, p50 0.70) |
| 3 consecutive runs' first hunts span ≥ 45 s | ≥ 45 s | **100 %** (first hunt p10/p50/p90 88/146/204 s) | **100 %** |
| runs with a ≥ 120 s gap after the first hunt | every run | **100 %** (shortest longest gap 123 s) | **100 %** (121 s) |
| share mean / p10 / p90 | 15–30 %, p10 ≥ 0.13 | **0.184 / 0.134 / 0.235** | **0.186 / 0.137 / 0.235** |
| hunts ≤ 10 s after an apparition | ≤ 35 % | **6.6 %** | **10.0 %** |

  The L2 report (critic route, without plans) under the same rules: share p10 0.141 / 0.145, and every run has a ≥ 120 s gap. The slower investigator has share p10 0.114 / 0.121 (reported, not asserted). Wave K driver model: share 0.168 mean, Dread at 480 s p90 57.8.
- Earlier assertions updated (premise changed): the first-apparition and spacing tests now describe bursts and lulls (40–55 s or 150–280 s first; spacing 40 s). Escalation-mechanism tests run on `EscMech()` (no first-hunt gate, no holds). Wave I–K mechanism tests run on `NoHold()`, or wait out the hold (`AfterFirstHunt`). The low-roll constant is 1/15 and the wave G mean wait is about 70 s (was 160 s at 1/40). The dice-shadow test roll is 0.09 (above the 1/15 low roll).

## What a windowed run must prove (3 unforced natural runs, played in a row so the history rotates)
- [ ] `HC_RUN_PLAN` bands differ across the three runs; first hunts span ≥ 45 s.
- [ ] Pooled apparition gaps (`HC_SCARE visible=` times) CV ≥ 0.5. `HC_SCARE_CLOCK` shows bursts (40–55 s), lulls (150–280 s) and `rescheduled=` after hunts.
- [ ] Every run has a ≥ 120 s threat-free stretch after its first hunt. Share 15–30 %, and ≤ 35 % of hunts within 10 s of an apparition.
- [ ] Bellwether: `tools/bellwether_fuzz.py` PASS on the merge commit.
