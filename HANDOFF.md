# HANDOFF: piece F1 "rules" (hc/dread), wave F

Branch `hc/dread`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/dread`, based on `haunted-castle` `fbd5807`. Model: Opus 5.5. Date 2026-10-13.
FEAR_PLAN items delivered on the rules side: 1 (Dread), 2 (dice hunts), 3 (no-tell, false warning, escalation), 4 (hostile Return), 5 (Lies / DoorHeld thresholds), 6 (`HuntRules.LongNight`), 7 (Deaths, death penalty).

## Files (only these were touched)
- `/Users/midir/hollow-current-haunted-castle/.wt/dread/Assets/HollowCurrent/Core/HuntRules.cs` (+ `.meta`, fresh guid `5a69f4233c5840fba39887605eacdc47`): new.
- `/Users/midir/hollow-current-haunted-castle/.wt/dread/Assets/HollowCurrent/Core/Investigation.cs`
- `/Users/midir/hollow-current-haunted-castle/.wt/dread/Assets/HollowCurrent/Core/CaseBook.cs`: `Rules` field only (Bellwether book = `HuntRules.Bellwether`, Castle book = `HuntRules.Castle`).
- `/Users/midir/hollow-current-haunted-castle/.wt/dread/Tests/RulesTests.cs`: appended `DreadRulesTests()` + one call line in `Main`; no existing line changed.

## Verified (how)
- `python3 tools/check_source.py` (with `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll`): PASS 483 assertions (396 existing, unchanged and passing, + 87 new); runtime and editor C# compile against player and editor assemblies. Unity not launched.
- Mutation check: seven deliberate bugs (no-tell chance 0, Die ignoring hostile Return, Record blocked in false warning, floor clock not pausing below 40, penalty not applied, Bellwether timer gated by dread, MysteryReady ignoring false warning) each fail a named new assertion.
- Bellwether byte-for-byte: differential fuzz, old `fbd5807` Core vs new Core, 4,000 seeds x 300 random operations (ticks, records, notes, conclude, power, seal, BeginWarning, Die, Invulnerable, FinishAtVan, WinByKill, save/restore same and fresh instance), state + messages + phase events logged each step: 1.2 M lines, identical SHA-1 `c70c9479...`. Harness: `/private/tmp/claude-501/-Users-midir/7e6a5e4e-3e3a-4250-94de-140e73836416/scratchpad/diff/Fuzz.cs` (scratch, not committed).

## Final public API
```csharp
// HuntRules (Core/HuntRules.cs): plain data class, public fields
public sealed class HuntRules {
  public string Name;
  public float TimedHuntSeconds; public int TimedHuntEvidence; public float HuntDreadMin;
  public float FirstHuntSeconds, FirstHuntJitter; public int FirstHuntEvidence;
  public float DiceIntervalMin, DiceIntervalMax, DiceChanceAt40, DiceChanceAt60, DiceChanceAt80, DiceEvidenceBonus;
  public float WarningSeconds, HuntSeconds, RecoverySeconds, EscalationWarningSeconds;
  public float NoTellChance, FalseWarningChance, EventEscalationChance;
  public bool HostileReturn;
  public bool UsesDread; public float DreadDarkRate, DreadInsideRate, DreadSafeRecovery, DreadStart;
  public float DreadApparition, DreadThrow, DreadSeen, DreadGrabSurvived;   // suggested AddDread amounts: 12, 8, 15, 20
  public float LieThreshold, DoorHeldThreshold;                              // 0 = never
  public bool DeathPenalty; public float DeathDread;
  public int NailedHides; public float PerchMinSeconds, PerchMaxSeconds;     // 0 = profile default
  public float DiceChance(float dread, int evidence);
  public HuntRules Clone();
  public static HuntRules For(MapId map, bool longNight = false);            // Long Night only for the castle
  public static readonly HuntRules Bellwether, Castle, LongNight;
}
// CaseBook
public HuntRules Rules;
// Investigation (Core/Investigation.cs), additions
public Investigation(CaseBook book, Func<float> random = null, HuntRules rules = null); // rules null = book.Rules
public HuntRules Rules { get; }
public Func<float> Dice { get; set; }            // [0,1); null = deterministic 0.5
public float Dread { get; }                      // 0-100, never in Goal
public void AddDread(float amount);              // clamped; ignored when !Rules.UsesDread
public void Tick(float dt, bool inside);         // = Tick(dt, inside, litNearby: true, inSafe: false)
public void Tick(float dt, bool inside, bool litNearby, bool inSafe);
public bool Lies { get; }                        // Dread >= LieThreshold (70)
public bool DoorHeld { get; }                    // Dread >= DoorHeldThreshold (90)
public bool FalseWarning { get; }                // this Warning ends back in Disturbance
public bool NoTellHunt { get; }                  // this hunt had no Warning (set before Changed fires)
public bool HuntActive { get; }                  // Phase Hunt, or Phase Return with HostileReturn
public float WarningLength { get; }              // 9, or 1.5 after TryEscalate
public bool TryEscalate();                       // call when an apparition lands within 4 m
public int Deaths { get; }
public bool DeathPenaltyApplied { get; }         // last restore applied the penalty: move the next relic/page
public void RestoreCheckpoint(Checkpoint saved); // = RestoreCheckpoint(saved, afterDeath: true)
public void RestoreCheckpoint(Checkpoint saved, bool afterDeath);
// Checkpoint gains: public float Dread; public int Deaths;
// Die(): now allowed whenever HuntActive (and increments Deaths).
```

## Rules as built (castle)
- Dread: inside dark +0.12/s, inside lit +0.05/s, `inSafe` -0.5/s (inSafe wins), outside unchanged. Starts 0 (Long Night 50).
- Dice: only in Disturbance with Dread >= 40; next roll every 2-6 s (interval drawn from Dice); chance 1/12, 1/6, 1/4 by band [40,60), [60,80), [80,100], times (1 + 0.5 x records).
- Floor: 240 s of Disturbance at Dread >= 40 since the last hunt forces one (clock pauses below 40; false warnings do not reset it).
- First-hunt guarantee (added, see below): before any hunt, 95 s x (0.75..1.25 by roll) of Disturbance with >= 2 records forces one. Deterministic dice give exactly 95 s.
- Spacing: dice and floor only run in Disturbance, so the next hunt is always >= 32 s (Recovery) after the last (tested over 600 s of certain dice).
- Rolled or forced hunt: 25 % no Warning (`NoTellHunt`); dice hunts only: else 25 % false alarm (`FalseWarning`, 9 s, says WarningMessage, back to Disturbance). Bell (`BeginWarning`) always comes true. Long Night: no Warning ever, including the bell and escalation.
- Escalation: `TryEscalate()` in Disturbance, 20 %: 1.5 s Warning with no dispatch line, then a hunt.
- Hostile Return: completing the seal increments Hunts, sets NoTellHunt, `HuntActive` until `FinishAtVan`. No timeout.
- Death penalty: a restore after death sets Dread 60, `Deaths` = max(current, saved + 1), `DeathPenaltyApplied`.

## Deviation to review
The brief's castle values alone would break the existing castle test in `MapTests` (2 records, `Tick(95)`, expects a survived hunt) and would soft-lock the mystery (needs a hunt survived) when a player keeps Dread under 40. Added `FirstHuntSeconds/FirstHuntJitter/FirstHuntEvidence` (castle 95 / 0.25 / 2): it fires only before the first hunt and is jittered by the real dice. Set `FirstHuntSeconds=0` to drop it (the old castle test will then fail).

## Requests for integrator (F0)
1. `Game.cs`: `Case=new Investigation(CaseBook.For(Map), ()=>UnityEngine.Random.value, HuntRules.For(Map, longNightSetting));` and call `Case.Tick(dt, Player.Inside, litCandleWithin6m, inGatehouseOrAtChapelFire)`. Until this lands, a merged castle runs with deterministic 0.5 dice (first hunt at 95 s, dice only at Dread 80+ with 3 records) and Dread rising at the lit rate.
2. Call `Case.AddDread(Case.Rules.DreadApparition / DreadThrow / DreadSeen / DreadGrabSurvived)` on those events; `Case.TryEscalate()` when an apparition lands within 4 m.
3. Replace `Phase==Phase.Hunt` with `Case.HuntActive` where kills/visibility are decided (Presence castle branch is F2's).
4. On `Changed(Phase.Warning)`: if `FalseWarning`, strobe lights + radio static but no hunter; if `WarningLength < 9` (escalation) there is no WarningMessage. On `Changed(Phase.Hunt)` with `NoTellHunt`: skip the warning presentation. `CastleRun` bell line says "Nine seconds"; in Long Night the bell starts the hunt at once.
5. `DeathPenaltyApplied` after `RestoreCheckpoint`: move the next relic or ledger page (F3's `RelicAltPositions`).
6. `Lies` / `DoorHeld` drive compass spin, phantom `cc_listen`, false candle tell, held hide door; `NailedHides`, `PerchMin/MaxSeconds` are data for Game / Presence.
7. Interface "HUNTS SURVIVED" uses `Hunts`; the hostile Return now counts as a hunt.

## Not done
No runtime (Game, Presence, Interface, ScareDirector) wiring, no captures, no playtest: those belong to F0/F2/F3. Nothing rendered.
