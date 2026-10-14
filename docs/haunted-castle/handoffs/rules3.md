# HANDOFF: piece "rules3" (hc/rules3), wave H

Branch `hc/rules3`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/rules3`, based on `haunted-castle` `5e86a3f`. Model: Opus 5.5. Date 2026-10-14.
Scope: r10b static critic (`docs/haunted-castle/rounds/r10/CRITIC_STATIC_B.md`) N6, N7, N10, N11 (rules side), N12, N13. No Unity, no game, no window: only `tools/check_source.py` and `tools/bellwether_fuzz.py` (Unity's bundled csc + mono, no engine).

## Files touched
- `Assets/HollowCurrent/Core/HuntRules.cs`, `Assets/HollowCurrent/Core/Investigation.cs`
- `Tests/RulesTests.cs`: `WaveHTests()` appended; F1/G assertions whose premise wave H removes were updated (below).
- New: `tools/bellwether_fuzz.py`, `Tests/BellwetherFuzz.cs`, `Tests/BellwetherReference/*.cs` (verbatim `c4b24c1` Core + README). Outside `Assets/`, so Unity never compiles them; `check_source.py` compiles only `Tests/RulesTests.cs`.
- `docs/haunted-castle/FEAR_PLAN.md`: "Investigation API, wave H additions"; FeltDread wording updated.
- `docs/haunted-castle/handoffs/rules3-integrator.diff`: the required changes in integrator-owned files (not applied).

## What changed (castle; every Bellwether knob is off)
- **N7** After the first hunt: Survey and Containment roll 1/80 per roll (2–6 s interval); Recovery below Dread 80 rolls 1/16 (1/8 at ≥ 80 unchanged). A Containment threat returns to Containment. `ThreatFrom` names the phase a threat started from.
- **N6** FeltDread no longer fades to calm at `Lies`. Lie episodes from the run's noise seed (no dice): every 200–600 s, 20–40 s long; calm drift (to 12) at Dread ≥ 40, false spike (65–85) at Dread < 60, coin flip in 40–60; same 20–30 s lag both ways. `FeltEpisode`, `FeltEpisodes` for logs. A restore reschedules from the restored time.
- **N10** Told and false Warnings draw 5–14 s (median 9). `EscalatedWarning` marks the 1.5 s escalation tell (set before `Changed`).
- **N11** `Case.FeltDread` is public and includes the episodes; CastleScares (F3) keys silence on it. Nothing else is needed from the rules.
- **N13** `ForceDread(float)` (driver only) sets Dread and restarts the dice schedule, so a queue always starts with the interval roll.
- **N12** Bellwether fuzz committed (see results).

## API additions (exact)
```csharp
// Investigation
public float CurrentWarningSeconds { get; }  // = WarningLength; castle told/false 5-14 s, escalation 1.5, Bellwether 9
public bool EscalatedWarning { get; }         // this Warning came from TryEscalate
public Phase ThreatFrom { get; }              // phase the current threat started from
public string FeltEpisode { get; }            // "none" | "calm" | "spike" (diagnostics)
public int FeltEpisodes { get; }              // episodes started this run (diagnostics)
public void ForceDread(float target);         // automated driver only
// HuntRules (Castle / Bellwether)
float WarningSecondsMin, WarningSecondsMax;   // 5,14 / 9,9  (WarningSeconds = median)
float RecoveryLowChance;                      // 1/16 / 0
float QuietPhaseChance;                       // 1/80 / 0
float FeltEpisodeGapMin, FeltEpisodeGapMax, FeltEpisodeMin, FeltEpisodeMax;  // 200,600,20,40 / 0
float FeltCalmFrom, FeltSpikeBelow, FeltSpikeMin, FeltSpikeMax;              // 40,60,65,85 / 0
```
Roll order per threat (driver queues): interval, hunt roll, tell roll, false-alarm roll (dice only), warning length (told or false), hunt length (at hunt start); recovery length when the hunt ends. Felt episodes never consume dice.

## Test and fuzz results
- `python3 tools/check_source.py`: **PASS 968** assertions (965 before `EscalatedWarning`; 938 after the wave H rule change alone, from 939 on `5e86a3f`). Runtime and editor C# compile against player and editor assemblies.
- Measured (`WAVE_H` lines): Recovery at Dread 79 cut short in 29 % of 400; Containment 10-minute walks hunted in 75.7 % of 300 (mean wait 225 s); warnings 5.0 / median 9.0 / 14.0; dice warnings: false n=450 median 8.72 s, told n=1,405 median 9.12 s; felt episodes 1.5 per 10 minutes.
- Mutations: seven deliberate bugs (no quiet-phase dice, fixed 9 s false warnings, Lies fade restored, ForceDread not restarting the schedule, no low recovery dice, restore not rescheduling episodes, calm-only episodes) each fail a named assertion.
- **Bellwether fuzz:** `python3 tools/bellwether_fuzz.py` → PASS, 4,000 seeds x 300 steps, 1,200,000 lines, reference and current sha256 `ee3b74dce8e03dcb66bf5b7f0f72394217e92df4fcf13343ffa9da256fd68f2f`, about 9 s. Negative check: a one-second Bellwether recovery change in a scratch copy → FAIL with the first differing line, exit 1.
- Assertions edited because wave H removes their premise: floor test warning length (`==9` → 5–14); the spacing test now expects hunts out of Recovery below 80; the false-warning test waits out the drawn length; the wave G "below Dread 80 Recovery is never interrupted" is now a 1/16 distribution; "while Lies the pulse shows calm" and its follow-up are now "Lies no longer fades the pulse"; the wave G noise test runs with episodes off; helpers wait out drawn warnings instead of `Tick(9)`.

## Requests for integrator (F0): `docs/haunted-castle/handoffs/rules3-integrator.diff`
Applies with `patch -p1` on `5e86a3f`; verified to compile (check_source PASS 968 with it applied, in a scratch copy).
1. `PlaytestDriver.cs` (N13): new `ForceDice(why, queue...)` = `c.ForceDread(max(55, RunDreadMin+5))` + enqueue. Step 5 drops the boundary Blocker; queue `[0,0,.9,0,.5]` (false warning, 9 s). Step 6 waits for Disturbance, then `[0,0,0,.5]` (no-tell, 38 s hunt). Step 7 drops `Frames(3)`; queue `[0,0,.9,.9,.5,.5]` (told, 9 s warning, 38 s hunt). All three are tested for boundaries 30–50 and Dread 0/20/32/60 at Disturbance entry.
2. `Game.cs:393` infers escalation from `WarningLength < 9`; with drawn warnings a 5–8 s told Warning would lose its "warning" voice. Use `Case.EscalatedWarning`; pulse length `Case.CurrentWarningSeconds`. Add `threatFrom=` to `HC_HUNT_START` and `escalated=`/`threatFrom=` to `HC_WARNING`.
3. `CaptureMode.cs:287` ticks 9.05 s to reach the hunt; use `CurrentWarningSeconds + 0.05`.
4. Not in the diff: `CastleScares` silence/lie chance should read `Case.FeltDread` (N11, F3's file).

## What a windowed run must prove
- [ ] `HC_HUNT_START threatFrom=Containment` (and `=Recovery` below Dread 80) at least once in a long natural run.
- [ ] `HC_WARNING` lengths vary 5–14 s for both `false=True` and `false=False`.
- [ ] The heartbeat fades during a calm episode at high Dread and rises during a spike at low Dread (log `FeltEpisode`), and does not fade merely because Dread passed 70.
- [ ] The fear driver with the diff passes steps 5–7 on a run whose `RunDreadMin` is 30–35.
- [ ] Bellwether: `tools/bellwether_fuzz.py` PASS on the merge commit; the 95 / 9 / 38 / 32 cycle in the build.
