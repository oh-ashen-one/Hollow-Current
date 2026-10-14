# HANDOFF: piece "rules5" (hc/rules5), wave J

Branch `hc/rules5`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/rules5`, based on `haunted-castle` `7c7156f`. Model: Opus 5.5. Date 2026-10-14.
Scope: runtime evidence on build 0b08422 (8.7-min natural run, invulnerable driver standing in the open, Dread pinned at 100, a new hunt as each grace ended, HuntShare 0.362 against ≤ 0.30). Code only: no Unity, no game. Ran `tools/check_source.py` and `tools/bellwether_fuzz.py` (Unity's bundled csc + mono, no engine).

## Files touched
- `Assets/HollowCurrent/Core/HuntRules.cs`, `Assets/HollowCurrent/Core/Investigation.cs`
- `Tests/RulesTests.cs`: `WaveJTests()`; `Session()` gains an `immortal` mode; `RecoveryMech()` (the wave I model check) also turns the governor off.
- `docs/haunted-castle/FEAR_PLAN.md`: "Investigation API, wave J additions".
- `docs/haunted-castle/handoffs/rules5-integrator.diff`: `governor=` in `HC_RECOVERY` and `HC_RULES` (not applied).

## What changed (castle only; Bellwether `GovernorGain` 0)
- `GovernorShare` = threat time (Warning, Hunt, hostile Return) over the time since the first threat began, at most 600 s and at least 300 s. The 10-minute `HuntShare` would count the quiet minutes before the first hunt and miss a dense run after it (tested: that variant fails the immortal bound).
- Share above 0.25: multiplier `1 + 12 × (share − 0.25)`, at most 3. The post-hunt grace (30–90 s draw) is multiplied by it, and the banded high-Dread dice odds are divided by it. The low-Dread, quiet-phase and Recovery rolls are untouched.
- If the share had stayed below 0.12 for 180 s when a hunt began, that hunt's grace is × 0.8, so quiet runs do not go dead.
- Never a hard cap: nothing is disarmed beyond the (scaled, still random) grace, and certain dice still give a hunt at multiplier 3 (tested). The grace stays a random draw, so no fixed length can be read as a timer.
- The suggested gain of 4 was too weak for the immortal case (10-minute HuntShare p50 0.333, p90 0.373). Gain 8 gave p90 0.340; gain 12 meets ≤ 0.33.

## API additions (exact)
```csharp
// Investigation
public float Governor { get; }             // live multiplier (1 neutral, 0.8 quiet, up to 3)
public float GovernorAtLastGrace { get; }  // multiplier applied to the last grace (log as HC_RECOVERY governor=)
public float GovernorShare { get; }        // the share the governor reads
// HuntRules (Castle / Bellwether)
float GovernorStart, GovernorGain, GovernorMax, GovernorMinWindow;               // 0.25, 12, 3, 300 / 0
float GovernorQuietShare, GovernorQuietSeconds, GovernorQuietScale;             // 0.12, 180, 0.8 / 0
```
No new dice rolls (driver queues unchanged).

## Results
- `python3 tools/check_source.py`: **PASS 1036** (1018 on `7c7156f`).
- `python3 tools/bellwether_fuzz.py`: **PASS**, 4,000 seeds × 300 steps, sha256 `ee3b74dc…fd68f2f`.
- Mutations: no grace scaling, no odds division, no quiet scale, window from run start each fail a named assertion.
- **Immortal, standing in the open, Dread pinned at 100** (2,000 seeds × 10 min): HuntShare mean **0.285**, p10 0.244, p50 0.285, **p90 0.325**, max 0.370; 2 runs of 2,000 had fewer than 3 hunts, minimum 2. Same model without the governor: p50 0.400, p90 0.448 (reproduces the runtime 0.362).
- **Mortal model** (wave I `Session`, 2,000 seeds): hunt + warning after the first hunt mean **0.250**, p90 **0.327** (wave I: 0.273 / 0.379); 10-minute HuntShare p50 0.183, p90 0.238; quiet gaps p10 47 / p50 119 / p90 221 / max 439 s; 2.5 hunts per run (wave I 2.7). 25 % dark: mean 0.258, p90 0.333.
- **Gap CV:** per-seed median 0.56 (governor off 0.41: higher, as asserted). Pooled CV 0.56 (wave I 0.61: slightly lower, still ≥ 0.4).

## Requests for integrator (F0): `docs/haunted-castle/handoffs/rules5-integrator.diff`
Applies with `patch -p1` on `7c7156f`; compiles (check_source PASS 1036 with it applied, scratch copy).
- `HC_RECOVERY`: `governor=` (the multiplier applied to this grace) and `governorShare=`.
- `HC_RULES` (every 30 s): `governor=` (live).

## What a windowed run must prove
- [ ] The r11 natural run (invulnerable driver in the open): `HC_RULES huntShare` ≤ 0.33 at 10 minutes, still with 3+ hunts.
- [ ] `HC_RECOVERY governor=` rises above 1 after dense hunts and `grace=` grows with it; after a quiet stretch it reads 0.80.
- [ ] Bellwether: `tools/bellwether_fuzz.py` PASS on the merge commit.
