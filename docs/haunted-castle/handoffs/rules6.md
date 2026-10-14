# HANDOFF: piece "rules6" (hc/rules6), wave K (+ escalation addendum)

Branch `hc/rules6`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/rules6`, based on `haunted-castle` `b428a8e` (includes rules5). Model: Opus 5.5. Date 2026-10-14.
Scope: r11 blind playtest critic (`docs/haunted-castle/rounds/r11/CRITIC.md`, fear loop and hiding; numbers from build 3bff23e, before the governor), plus the coordinator's escalation addendum. Code only: no Unity, no game. Ran `tools/check_source.py` and `tools/bellwether_fuzz.py` (Unity's bundled csc + mono, no engine).

## Files touched
- `Assets/HollowCurrent/Core/HuntRules.cs`, `Assets/HollowCurrent/Core/Investigation.cs`
- `Tests/RulesTests.cs`: `WaveKTests()`, `WaveKReport()`, the session model gains a `driver` mode (r11 route) and counts no-tell, escalation, Dread at 480 s, the longest quiet stretch and the first-hunt delay; apparitions now also land in Recovery (as F3 reports for the runtime).
- `docs/haunted-castle/FEAR_PLAN.md`: "Investigation API, wave K additions".
- `docs/haunted-castle/handoffs/rules6-integrator.diff` (not applied).

## What changed (castle only; Bellwether fuzz identical)
1. **Dread growth:** passive rise 0.03/s lit, 0.08/s dark (was 0.05/0.12). Calm decay 0.3/s (was 0.15; Recovery stays 0.4). Above a total of 60 the decay also takes ambient Dread, not only event Dread. 0.15 and 0.25 left the r11 driver at Dread p90 99 / 87 at 8 min; 0.3 gave 78 before the escalation addendum.
2. **No-tell:** flat 0.35 on every rolled or forced hunt, independent of Dread (it was already a flat 0.25; the critic's "no-tell only at Dread ≥ 87" was 2 no-tell hunts in 8). Long Night stays 100 % no-tell. The bell and escalations are always told.
3. **First hunt:** `Draw(60, 95, 240)` s after the second record (median kept at 95 so deterministic tests keep their timing); still guaranteed.
4. **Gaps/share:** met by 1 + the governor; no further change needed (see table).
5. **Door held:** `TryDoorHeld()` rolls 1/6 per hide attempt at Dread ≥ 60 with the injected dice; below 60 it never rolls. `DoorHeld` now stays false in the castle (roll mode). Bellwether unchanged.
6. **Death first:** a `Die()` resolved in the same tick as `FinishAtVan()` (hostile Return) or `WinByKill()` replaces the win: Dead, `Deaths` + 1, `HunterSlain` false, `DeathOverrodeWin` true (`Changed` fires Won, then Dead). Invulnerable → a blocked death. With `FrameSource` set, "same tick" means the same frame, so a `Case.Tick` between the crossing and the grab in one frame does not save the win. A tick (or frame) later the win stands. Bellwether's win stays final.
7. **Addendum, escalation:** 0.35 in Disturbance (was 0.2); 0.15 in Recovery once the post-hunt grace has run out; never during the grace (asserted over 3,000 hunts).

## API additions (exact)
```csharp
// Investigation
public bool TryDoorHeld();                  // one hide attempt (castle roll); false and no roll below the threshold
public int DoorsHeld { get; }
public bool DeathOverrodeWin { get; }
public Func<int> FrameSource { get; set; }  // Game: () => Time.frameCount (castle)
// HuntRules (Castle / Bellwether)
float FirstHuntMin, FirstHuntMax;           // 60, 240 / 0   (FirstHuntSeconds 95 = median)
float DreadDecayAllAbove;                   // 60 / 0
float DoorHeldChance;                       // 1/6 / 0       (DoorHeldThreshold 60, was 90)
float EscalationRecoveryChance;             // 0.15 / 0      (EventEscalationChance 0.35, was 0.2)
// changed values: DreadInsideRate 0.03, DreadDarkRate 0.08, DreadDecayCalm 0.3, NoTellChance 0.35
```
Roll order is unchanged except that `TryDoorHeld` and `TryEscalate` in Recovery each consume one roll when they roll.

## Results
- `python3 tools/check_source.py`: **PASS 1071** (1036 on `b428a8e`).
- `python3 tools/bellwether_fuzz.py`: **PASS**, 4,000 seeds × 300 steps, sha256 `ee3b74dc…fd68f2f`.
- Mutations: no death-first, no decay above 60, door-held never rolling, no Recovery escalation, escalation during the grace, old first-hunt window each fail a named assertion.
- **Session models, 2,000 seeds × 600 s** (critic's measures; share = Warning + Hunt + hostile Return over the run):

| model | share mean / p50 / p90 | runs with a quiet stretch ≥ 120 s after the 1st hunt | Dread at 480 s p50 / p90 / max | no-tell hunts; runs with one below Dread 70 | escalations per run | 1st hunt after 2nd record p10 / p50 / p90 |
|---|---|---|---|---|---|---|
| mortal player | **0.163 / 0.162 / 0.224** | **93.6 %** | 20.8 / **44.6** / 77.5 | 1498 / 4602; **56.5 %** | 0.45 | 65 / 99 / 215 s |
| r11 driver (immortal, open, torch on) | **0.196 / 0.206 / 0.253** | 78.5 % | 59.7 / **87.6** / 100 | 1955 / 5526; 64.7 % | 0.42 | 64 / 98 / 210 s |

  Targets: mortal ≤ 22 % mean, p90 ≤ 28 %, ≥ 90 % runs with a 120 s gap, Dread < 85 at 8 min: met. Driver ≤ 0.30 and Dread < 95 at 8 min (p90): met. The critic's own bar (Dread < 85 at t=480 on the torch-on route) is met by the driver's median, not by its p90 (87.6; it was 78.3 before the escalation addendum added hunts). The escalation addendum did not push any share above target, so nothing was traded against the dice odds. Wave I/J models, same run: mortal after-first-hunt 0.235 mean / 0.320 p90; pinned-100 immortal HuntShare 0.281 / 0.323.
- Earlier assertions updated (premise changed): passive rates (0.12/0.05 → 0.08/0.03) and their derived values; decay 0.15 → 0.3 values; the "door held from 90" state check (now a roll); first-hunt deadlines 72/119 → 60/238 s; "no death after the win" now ticks once before the grab (same-tick deaths are WaveKTests); the FeltDread tests that hold Dread at 85 run on `NoAllDecay()` (the castle without decay above 60).

## Requests for integrator (F0): `docs/haunted-castle/handoffs/rules6-integrator.diff`
Applies with `patch -p1` on `b428a8e`; compiles (check_source PASS 1071 with it applied, scratch copy).
1. `Game.cs`: castle `Case.FrameSource = () => Time.frameCount` (death first in any script order).
2. `Game.cs` `PhaseChanged`: in the castle the Won audio (`ending`, `evidence_filed`, focus) waits one frame and plays only if the phase is still Won; log `HC_DEATH_OVER_WIN` when Dead replaces a win. Interface already reads `Case.Phase` every frame, so the results screen follows the final phase. Bellwether path unchanged.
3. `Investigator.cs` `Hide`: `Game.Case.TryDoorHeld()` instead of `DoorHeld`, log `HC_DOOR_HELD hide= dread= held=`.
4. Every grab must call `Case.Die()` even if the phase already reads Won in that frame; the rules then decide (death first). Not verified here whether Presence's grab paths (`Presence.cs:200, 849, 1789`) can skip `Die()` once the phase is Won; F2/F0 should check that against the r11 natural2 log (`:658` grab, `:664` Won).

## What a windowed run must prove
- [ ] Two unforced natural runs: Warning + Hunt + Return ≤ 22 %; one hunt-free gap ≥ 120 s after the first hunt; Dread < 85 at t=480; one no-tell hunt below Dread 70; first-hunt delay spread ≥ 60 s over three runs.
- [ ] `HC_DOOR_HELD` at least once in a run that spends time at Dread ≥ 60.
- [ ] A bridge grab in the crossing frame ends Dead (`HC_DEATH_OVER_WIN`), not Won.
- [ ] `HC_ESCALATE escalated=True` at least once in natural play; never while the logged grace is running.
- [ ] Bellwether: `tools/bellwether_fuzz.py` PASS on the merge commit.
