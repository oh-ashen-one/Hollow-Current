# HANDOFF: piece "rules-l2" (hc/rules-l2), wave L follow-up

Branch `hc/rules-l2`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/rules-l2`, based on `haunted-castle` `61cfbb7`. Model: Opus 5.5. Date 2026-10-14.
Scope: the wave L proof failed one pass test in the build (apparition inter-arrival CV 0.43 over ~12 samples in three natural runs, target ≥ 0.5). Core only; no Unity, no game; no Runtime file touched (the integrator's `DueIn` and wiring are unchanged).

## What changed (castle only)
- `ScareRhythm.CastleInterval(u1, u2)`: two modes, 60 % a short cluster 45–55 s, else 130 s + a truncated exponential (mean 400 s, cut at 300 s). 10,000 samples: mean 114.1 s, CV 0.738, min 45.0, p10 47, p50 53, p90 252, max 299.9. `CastleMaxInterval` 300 (was 260); the first appearance is 0.6 × a draw (27–180 s). Same call, no integrator change.
- Why the design CV had to rise: groups of 12 samples from the old 0.54 design fall under 0.5 often, and a 10-minute run cuts off the long intervals (they do not complete), which lowers the measured CV further. With the new mix, 12 pure samples give CV ≥ 0.5 in 97.8 % of groups (p10 0.58), and three pooled simulated runs (≈ 12 intervals) in 95 %.
- `HuntRules.Castle.LowDreadChance` 1/24 (was 1/30) and `FirstHuntMax` 180 s (was 240; median still 95) to lift the per-run share p10 to ≥ 0.13 while keeping the ≥ 120 s gap. 1/20 or 1/17 alone raised p10 but pushed the slower-investigator gap measure under 90 %; the first-hunt window was the lever for runs whose first hunt came late.

## Tests and models
- `python3 tools/check_source.py`: **PASS 1103** (1100 at `61cfbb7`); `python3 tools/check_castle_scares.py`: **PASS**; `python3 tools/bellwether_fuzz.py`: **PASS** (4,000 seeds, sha256 `ee3b74dc…fd68f2f`).
- New: 12-sample CV test on the pure draw (10,000 groups of 12, P ≥ 0.9); the session report gains "3-run pooled sets with CV ≥ 0.5" (asserted ≥ 0.9) and a critic-route evidence variant (records by ~15 s, as the natural route).
- Updated (premise changed): interval bounds and CV (≥ 0.7, max 300), first-apparition bound 27–181 s, low roll 1/24, first-hunt window 60–180 s (deadline 179 s for roll 0.99).
- Mutations: the old clock and the old low roll each fail an assertion.

| 2,000 seeds × 600 s | hunts ≤ 10 s after an apparition | apparition CV (pooled) | 3-run sets with CV ≥ 0.5 | first hunt p10 / p50 / p90; 3-run sets spanning ≥ 45 s | share mean / p10 / p90 | runs with a ≥ 120 s gap |
|---|---|---|---|---|---|---|
| target | ≤ 35 % | ≥ 0.5 | ≥ 90 % | ≥ 45 s | 15–30 %, p10 ≥ 0.13 | ≥ 90 % |
| critic route, 1 in 3 close | **9.2 %** | 0.71 | **95.0 %** | 82 / 112 / 179 s; 99.1 % | **0.191 / 0.133 / 0.246** | **97.8 %** |
| critic route, all close | **14.6 %** | 0.71 | **95.2 %** | 82 / 110 / 178 s; 93.7 % | **0.193 / 0.137 / 0.245** | **97.2 %** |
| slower investigator, 1 in 3 close | 8.2 % | 0.72 | 93.7 % | 137 / 187 / 250 s; 94.3 % | 0.173 / **0.119** / 0.229 | 92.4 % |
| slower investigator, all close | 14.7 % | 0.72 | 93.5 % | 132 / 184 / 246 s; 87.2 % | 0.177 / **0.127** / 0.232 | 92.5 % |

  All four targets hold on the critic's route (how the build is measured). On a slower investigator (second record at 40–120 s) the share p10 is 0.119–0.127, under 0.13: those runs start their first hunt late. Wave K driver model: share 0.190 mean, Dread at 480 s p90 83.2.

## What a windowed run must prove
- [ ] Three natural runs: apparition inter-arrival CV ≥ 0.5 (`HC_SCARE_CLOCK next_in=` shows the two modes: about 45–55 s and 130–300 s).
- [ ] Share 15–30 % per run, longest gap ≥ 120 s, ≤ 35 % of hunts within 10 s of an apparition, first hunts spanning ≥ 45 s.
- [ ] Bellwether: `tools/bellwether_fuzz.py` PASS on the merge commit.
