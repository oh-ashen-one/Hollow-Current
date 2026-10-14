# HANDOFF: piece "rules4" (hc/rules4), wave I

Branch `hc/rules4`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/rules4`, based on `haunted-castle` `0fb5aad`. Model: Opus 5.5. Date 2026-10-14.
Scope: blind critic `docs/haunted-castle/rounds/r10/CRITIC_BLIND.md` P3 (Dread pinned at 100, hunt + warning 52–63 % of play, 23–46 s gaps). Code only: no Unity, no game. Ran `tools/check_source.py` and `tools/bellwether_fuzz.py` (Unity's bundled csc + mono, no engine).

## Files touched
- `Assets/HollowCurrent/Core/HuntRules.cs`, `Assets/HollowCurrent/Core/Investigation.cs`
- `Tests/RulesTests.cs`: `WaveITests()` + the session model appended; earlier tests adapted (below).
- `docs/haunted-castle/FEAR_PLAN.md`: "Investigation API, wave I additions".
- `docs/haunted-castle/handoffs/rules4-integrator.diff`: integrator-owned changes (not applied).

## What changed (castle; Bellwether knobs are 0)
1. **Decay.** Event Dread (whatever `AddDread` adds) decays 0.4/s in Recovery and 0.15/s in Survey, Disturbance and Containment, times `DreadRelief`. After the first hunt it stops at a total of 20. Dread from darkness and time does not decay (only the safe zone lowers it at 0.5/s), so a player who stays in the dark still builds Dread (the critic's ">60 % of the time in the dark" exception).
2. **Per-hunt cap.** `AddDread` adds at most 25 per hunt or hostile Return, whoever calls it.
3. **Post-hunt grace.** A 30–90 s grace is drawn at hunt end, after the recovery length. It disarms all dice and escalation; low-Dread rolls resume after it. `ForceDread` clears it (driver).
4. **Session model** (2,000 seeds, see results).
5. **`HuntShare`**: rolling 10-minute share of Warning, Hunt or hostile Return.

## API additions (exact)
```csharp
// Investigation
public float DreadEvent { get; }       // decaying part of Dread (diagnostics)
public float DreadRelief { get; set; } // decay multiplier, default 1 (Game: gatehouse 3, chapel fire 2)
public float HuntDreadAdded { get; }   // AddDread total this hunt (cap 25)
public float GraceSeconds { get; }     // last post-hunt grace drawn (30-90 s)
public float GraceRemaining { get; }   // counts down; dice and escalation disarmed while > 0
public float HuntShare { get; }        // 0-1, last 10 minutes
// Investigation.Checkpoint
public float DreadEvent;
// HuntRules (Castle / Bellwether)
float DreadDecayRecovery, DreadDecayCalm, DreadDecayFloor; // 0.4, 0.15, 20 / 0
float HuntDreadCap;                                        // 25 / 0
float GraceMin, GraceMax;                                  // 30, 90 / 0
```
`ForceDread(target)` now also zeroes `DreadEvent` and `GraceRemaining`. Roll order at hunt end: recovery length, then grace. Driver queues are unchanged (ForceDread restarts the schedule and clears the grace).

## Test, fuzz and simulation results
- `python3 tools/check_source.py`: **PASS 1018** (970 before the wave I tests; 970 on `0fb5aad`). One run hit a transient Roslyn `MissingMethodException` in the runtime compile step after the rules tests had passed; an immediate rerun passed (toolchain files unchanged since July).
- `python3 tools/bellwether_fuzz.py`: **PASS**, 4,000 seeds × 300 steps, 1,200,000 lines, sha256 `ee3b74dc…fd68f2f` (reference and current).
- Mutations: eight deliberate bugs (no cap, no grace on Disturbance dice, no decay, no floor, no relief, escalation in grace, Recovery dice in grace, zero grace) each fail a named assertion.
- **Session model** (`RulesTests.Session`). Player: lit inside (+0.05/s); gatehouse 20–40 s every 150–300 s of calm play (relief 3); evidence at 10–40 / 40–120 / 120–240 s; apparitions every 60–110 s (+12, every second +8, one in three close → `TryEscalate`); in hunts seen in 60 % (DreadSeen once, +0.3/s up to 8 s); the hide fails in 30 % (grab survived +20). 2,000 seeds × 600 s:

| variant | hunt+warning after 1st hunt mean / p10 / p50 / p90 | 10-min HuntShare p50 / p90 | quiet gaps p10 / p50 / p90 / max (s) | gap CV pooled / per-seed median | hunts per run | starts below boundary | Dread ≥ 95 s/run |
|---|---|---|---|---|---|---|---|
| wave I damping off (model check) | 0.619 / 0.517 / 0.625 / 0.709 | 0.455 / 0.529 | 5 / 21 / 46 / 285 | 0.87 / 0.65 | 6.1 | 1860 / 14846 | 176.6 |
| **castle, lit** | **0.273** / 0.164 / 0.278 / 0.379 | **0.200 / 0.278** | **42 / 101 / 212 / 439** | **0.61** / 0.41 | 2.7 | 4001 / 6223 | 0.2 |
| castle, 25 % dark | 0.284 / 0.175 / 0.290 / 0.388 | 0.208 / 0.286 | 39 / 97 / 200 / 439 | 0.62 / 0.42 | 2.8 | 3738 / 6522 | 0.7 |

  Asserted: damping off reproduces the runaway (> 50 %, > 60 s at ≥ 95); castle mean ≤ 30 %, HuntShare p50 ≤ 25 % and p90 ≤ 30 %, pooled CV ≥ 0.4, gaps p10 ≥ 30 s and p90 ≥ 150 s, Dread ≥ 95 under 5 s per run, more than a quarter of hunts start below the boundary; the dark variant ≤ 32 %.
- Earlier tests adapted (premise changed): tests that set a Dread level with `AddDread` now use `ForceDread` (event Dread decays); `AfterFirstHunt` also waits out the grace; the Recovery-dice mechanism tests (wave G/H: 1/8 at ≥ 80, 1/16 below, spacing) run on `RecoveryMech()`, the castle with the wave I damping off, because the grace now covers most Recoveries (a wave I test asserts that interplay).

## Requests for integrator (F0): `docs/haunted-castle/handoffs/rules4-integrator.diff`
Applies with `patch -p1` on `0fb5aad`; compiles (check_source PASS 1018 in a scratch copy with it applied).
1. `Game.cs` `CastleTick`: `Case.DreadRelief = Player.InVan ? 3 : atFire ? 2 : 1;` before `Case.Tick`; a periodic `HC_RULES t= phase= dread= event= felt= runDreadMin= grace= relief= huntShare= hunts=` line every 30 s.
2. `Game.cs` `HC_RECOVERY`: add `grace=`, `huntDreadAdded=`, `huntShare=`.
3. `PlaytestDriver.cs` siege: hold Dread with `c.ForceDread(86)` (ambient; the old `AddDread` hold now decays), and once clear the grace at a Recovery start so the Recovery dice can still be proven (logged `HC_PLAY_FORCED grace`).
4. Not in the diff (Presence is F2's): seen/grab-survived once per hunt is F2's; the rules cap applies regardless.

## What a windowed run must prove
- [ ] Natural 10-minute run: `HC_RULES huntShare` ≤ 0.30 at t=600 and quiet gaps between threats from about 40 s up to several minutes.
- [ ] Dread never sits at ≥ 95 for more than 60 s unless the player was in the dark; `event=` falls after each hunt; `grace=` 30–90 in every `HC_RECOVERY`.
- [ ] No `HC_HUNT_START` (dice) or `HC_ESCALATE escalated=True` while the logged grace is still running.
- [ ] Siege mode with the diff still logs a near-miss open and one recovery cut.
- [ ] Bellwether: `tools/bellwether_fuzz.py` PASS on the merge commit.
