# HANDOFF: piece "rules2" (hc/rules2), wave G

Branch `hc/rules2`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/rules2`, based on `haunted-castle` `c4b24c1`. Model: Opus 5.5. Date 2026-10-14.
Scope: critic r10 static (`docs/haunted-castle/rounds/r10/CRITIC_STATIC.md`, untracked in the main clone) findings #1, #10, #12, #15 and the FeltDread contract for #2/#3. Stream-safe: no window, no Unity, no build, no game run. Only the standalone rules harness ran (Unity's bundled csc + mono, no engine).

## Files touched
- `Assets/HollowCurrent/Core/HuntRules.cs`: new fields, `Draw`, `DiceChance(dread, evidence, boundary)`.
- `Assets/HollowCurrent/Core/Investigation.cs`: rules below. Also the integrator's `BeginNoTellHunt` (now keeps `BreakerOn`).
- `Tests/RulesTests.cs`: `WaveGTests()` appended and called from `Main`. Seven F1-era castle assertions were edited because their premise is the behaviour this wave removes (listed below). No Bellwether or pre-wave-F assertion was touched.
- `docs/haunted-castle/FEAR_PLAN.md`: "Investigation API, wave G additions" under Contracts.

## What changed (castle rules; Bellwether rules have every new knob off)
- **#1** `RunDreadMin` drawn per run 30–50 (`HuntDreadMin` 40 ± `HuntDreadMinSpread` 10), kept through checkpoints. After the first hunt the dice also roll below it at a flat `LowDreadChance` 1/40 per roll (interval still 2–6 s). Crossing the boundary restarts the interval.
- **#10** Hunt length drawn per hunt 25–50 s (median 38), Recovery per Recovery 10–40 s (median 32), piecewise linear so a 0.5 roll gives exactly the median. At Dread ≥ 80 the dice roll in Recovery at 1/8 (false alarms from Recovery end in Disturbance). The castle no longer says `RecoveryMessage` (`AnnounceRecovery=false`). Long Night hunts 40–60 s (median 50).
- **#12** No-tell starts (dice no-tell, `BeginNoTellHunt`, Long Night bell and escalation) leave `BreakerOn` unchanged. A told Warning still cuts it.
- **#15** The first-hunt guarantee counts Disturbance time from the record that reaches two pieces of evidence (and restarts after a checkpoint restore).
- **FeltDread** for #2/#3: lagged, noisy, lying, boundary-blind reading for the heartbeat and journal pulse.

## API additions (exact)
```csharp
// Investigation
public float FeltDread { get; }              // 0-100; castle: Dread lagged by FeltLagSeconds, +-12 slow noise, -> FeltCalm while Lies; Bellwether 0
public float FeltLagSeconds { get; }         // per run, 20-30 s
public float RunDreadMin { get; }            // per run, 30-50 (Bellwether 0); diagnostics only
public float CurrentHuntSeconds { get; }     // this hunt's length (Bellwether 38)
public float CurrentRecoverySeconds { get; } // this Recovery's length (Bellwether 32)
// Investigation.Checkpoint
public float RunDreadMin = -1;
// HuntRules (public fields; Castle / Bellwether values)
float HuntDreadMinSpread;                    // 10 / 0
float LowDreadChance;                        // 1/40 / 0
float HuntSecondsMin, HuntSecondsMax;        // 25,50 / 38,38   (HuntSeconds = median)
float RecoverySecondsMin, RecoverySecondsMax;// 10,40 / 32,32   (RecoverySeconds = median)
float RecoveryDiceChance, RecoveryDiceDread; // 1/8, 80 / 0, 0
bool AnnounceRecovery;                       // false / true
float FeltLagMin, FeltLagMax, FeltNoise, FeltNoisePeriod, FeltCalm; // 20,30,12,6,12 / 0
public float DiceChance(float dread, int evidence, float boundary);
public static float Draw(float min, float median, float max, float roll);
```
Roll order (matters for `PlaytestDriver` queues): construction, castle only: boundary, felt lag, noise seed (before the driver swaps `Dice`). Per threat: interval, hunt roll, tell roll, false-alarm roll (dice only), then the hunt length when the hunt starts; the recovery length when the hunt ends. FeltDread never consumes a roll.

## Test results
- `python3 tools/check_source.py`: PASS 939 assertions (487 before this wave: 483 F1 + 4 integrator). The distribution loops assert per session, hence the jump. Runtime and editor C# compile against player and editor assemblies.
- Measured by the harness (seeded, printed as `WAVE_G`): Dread 0 for 10 min after the first hunt → 96.5 % of 400 sessions hunt, mean wait 161 s; hunt lengths 25.0 / median 38.0 / 50.0; recovery 10.1 / 32.0 / 40.0; Recovery at Dread 90 interrupted in 53 % of 600; FeltDread at steady Dread 50 ranges 38–62, max change 4.9 per second.
- Mutation check: nine deliberate bugs (brazier cut on no-tell, first hunt from Disturbance entry, no lie calm, no lag, no low roll, no recovery dice, felt reacting to the boundary, fixed hunt length, no interval restart at the boundary) each fail a named assertion.
- Bellwether identical: differential fuzz of `c4b24c1` Core against this Core, 4,000 seeds x 300 random operations (adds `AddDread`, the 4-arg Tick, `TryEscalate`, `BeginNoTellHunt`, `RestoreCheckpoint(saved, afterDeath)` and a live random `Dice` to the wave F op set), state + messages + phase events every step: 1.2 M lines, both SHA-1 `2746285b94ce2208acf58db596402217666822ff`. Harness was in the session scratchpad (not committed).
- F1 assertions edited (premise removed by this wave): "below 40 Dread … certain roll" now uses the run's boundary; the 60–80 band no-tell hunt now expects `BreakerOn` kept; floor and floor-pause tests sit 5 above the run's boundary (3 assertions); the spacing test runs at Dread 79 and checks against each drawn recovery; the false-warning setup waits out drawn lengths instead of fixed 38/32 ticks.

## Requests for integrator (F0)
1. Heartbeat (`Soundscape.cs:84` curve, `:142` floor) and journal pulse (`Interface.cs:351`) read `Case.FeltDread`, not `Dread`, with no threshold of their own.
2. `Interface.cs:494` "SIGNAL COLLAPSE n" and any length text: read `WarningLength` / `CurrentHuntSeconds` / `CurrentRecoverySeconds` or show nothing in the castle. `CastleRun` `BellRung` and castle `WarningMessage` still say "Nine seconds" (true for told warnings, which stay 9 s).
3. `PlaytestDriver.cs:263` waits 45 s for a hunt to end and `:303` 80 s for Disturbance; with the fallback 0.99 roll a hunt now lasts about 50 s and a recovery about 40 s. Raise to 55 s / 95 s or enqueue 0.5 for the length draws. The false-warning queue `[0, 0, .9, 0]` still lines up (tested), provided Dread is forced from below `RunDreadMin` (the driver's blocker checks a literal 40; use `c.RunDreadMin`).
4. Log `RunDreadMin`, `CurrentHuntSeconds`, `CurrentRecoverySeconds`, `FeltDread` in `HC_RULES` / `HC_HUNT_START` for the critic.

## What a windowed run must prove
- [ ] At Dread below `RunDreadMin` for 10+ minutes after the first hunt, at least one hunt starts (expect about 96 %).
- [ ] `HC_HUNT_START` lengths vary across hunts (no two runs of 38 s / 32 s), and a Recovery at Dread ≥ 80 is cut short at least once in a long session.
- [ ] A no-tell hunt leaves room tone and the chapel brazier unchanged (`BreakerOn` stays true in the log).
- [ ] Recording the second evidence late (Disturbance > 120 s) is followed by a quiet 71–119 s, not an instant threat.
- [ ] Heartbeat and journal pulse follow `FeltDread`: no audible/visible step at `RunDreadMin`, a 20–30 s lag after a Dread jump, calm readings while `Lies`.
- [ ] Bellwether: the unchanged 95 / 9 / 38 / 32 cycle (RulesTests green covers the rules; the build must too).
