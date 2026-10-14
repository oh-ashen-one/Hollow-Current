# HANDOFF: scares3 (wave H, hc/scares3)

Base `haunted-castle` 5e86a3f. Model: Sonnet 5.5. 2026-10-14. Code only: no Unity, no game. Verified: `INPUT_SYSTEM_REFERENCE=... python3 tools/check_source.py` (939 assertions PASS, compiles) and `python3 tools/check_castle_scares.py` PASS. The new lint was checked by mutation (removing `tell-safe:` makes it exit 1). Files: `Assets/HollowCurrent/Runtime/Castle/CastleScares.cs`, `tools/check_castle_scares.py`.

## Changes
- **N11a silence keyed on FeltDread.** `SilenceChance = 0.15 + 0.25 * clamp01((FeltDread - 40) / 60)`: a flat 15 % at any felt level (so silence happens when the player feels calm), 40 % at felt 100. Real Dread is no longer read there (the lint fails if it is). The silence log carries both: `HC_SCARE_CASTLE silence seconds=.. duck=wired|unwired felt=.. dread=..`.
- **N11b nothing stops on a phase change.**
  - `Footsteps` no longer checks any phase or hunt state; the five steps always finish (only `Ready`, i.e. the run existing, ends them).
  - `BalustradeShape` used to be destroyed the frame a warning or hunt began (`!Calm`); it now ends only on `Present` (run exists, not paused), the 40 s timeout, leaving the hall floor, or being looked at.
  - Phantom listen is one `Speak` call (cannot be cut). Knife flight, bell swing, candle gutter, bat and window silhouette never read the phase. `FalseCompass` runs its 3 s timer regardless.
- **Audit and substitute.** The old `Calm` gate (no beat in Warning, Hunt or Recovery) is gone from `CastleScares`; there is a single `Active` gate (run present, not hidden, not Briefing, not Dead/Won) and every Warning/Hunt/Recovery/Containment/Return beat is allowed. Because `ScareDirector.Update` itself returns in those phases, `CastleScares.TickAmbient` fires an **ambient substitute** there: first after 8-30 s of the quiet phase, then every 22-38 s (the director's own cadence), picking footsteps behind (when Lies), window silhouette, gallery shape, then book drop, knife, bat or candle gutter. Not the bell, silence, false candle tell or false compass (they would collide with the real candle tell/compass in a hunt). Log: `HC_AMBIENT phase=<phase> beat=<last beat|none>`.
- **Lies are no longer all-or-nothing.** `FootstepsBehind(decoy)`: when not `Lies`, 12 % of dread ticks still play the steps behind (`HC_LIE kind=footsteps_behind decoy=True`), so the steps are not proof of Dread >= 70. Phantom listen stays Lies-only (once per run, shared with Soundscape's rare line). Lies and lie decoys now ignore hunts entirely.
- **Lint.** `tools/check_castle_scares.py` fails if any code line in `CastleScares.cs` tests `HuntActive`, `Phase.`, `.Phase`, `NoTellHunt` or `FalseWarning` without a `// tell-safe:` comment on the line or within 8 lines above (log lines are exempt); if `SilenceChance` does not use FeltDread or touches real Dread; if the ambient substitute is missing; or if the Footsteps coroutine tests a phase.

## Audit result (everything in the castle scare layer keyed on hunt/phase/Dread)
| Item | Before | Now |
|---|---|---|
| `Calm` (Warning, Hunt, Recovery excluded) | every beat vanished ~80 s around each hunt | removed; `Active` excludes Briefing/Dead/Won only (`// tell-safe:`); ambient substitute covers the director's quiet phases |
| `ScareDirector.Update` early return in Warning/Hunt/Recovery/Containment/Return | scare layer silent | `TickAmbient` (above). The 30 s `next` after Recovery remains in the director (diff below) |
| `Hidden` | beats deferred | unchanged and tell-safe: same rule in calm and hunt phases, and a player action |
| Footsteps `LieAllowed` (no hunt) | stopped on the hunt frame | never stops |
| Shape killed by `!Calm` | vanished on hunt start | persists |
| Silence chance on real Dread >= 60 | statistical gauge | FeltDread, flat base 15 % |
| Lies only at Dread >= 70 | presence = Dread >= 70 | footsteps decoy 12 % below; residual below |
| `FalseCandleTell`, `FalseCompassReading` | only at Lies | unchanged, **residual**: their presence still implies Dread >= 70 (they are lies; a decoy would need a second lie channel). Fix if wanted: let the director roll them at 5 % below the threshold. |
| Poltergeist, Throwable, KnifeOrbit, Floater | no phase gate | no change |

## Optional ScareDirector.cs diff for the integrator (not applied)
The long gap after a hunt is the director's: `next=30` in Recovery, with `rhythm.Rest(90/65)` for apparitions. `TickAmbient` removes the scare-layer silence during the hunt itself; this one line shortens the gap that remains after it, randomised so its length is not a clock:
```diff
-                if(previousPhase==Phase.Recovery){rhythm.Rest(65);next=30;}
+                if(previousPhase==Phase.Recovery){rhythm.Rest(65);next=IsCastle?Random.Range(6f,30f):30f;}
```

## What a windowed run must prove (exact log keys)
- `HC_AMBIENT phase=Hunt beat=...` (and `phase=Warning|Recovery|Return`) appears in a natural session; across 10 hunts at least 6 have an `HC_AMBIENT phase=Hunt` or `phase=Recovery` line with a beat other than `none`.
- A footsteps sequence started within 3 s before a forced hunt still plays all five steps: `HC_LIE kind=footsteps_behind` followed by five `claw_steps_` cues after `HC_HUNT_START`.
- With `HC_SCARE_CASTLE balustrade_shape` logged and then a hunt started, no `HC_SCARE_CASTLE balustrade_shape_gone` on the hunt frame (it only appears when looked at, after 40 s, or on leaving the hall).
- `HC_SCARE_CASTLE silence ... felt=<f>` frequency over a 10 minute session: about 15 % of dread ticks while felt < 40 and clearly above 15 % only as felt rises; correlate against `felt=`, not `dread=`. Silence lines must exist at felt < 40.
- `HC_LIE kind=footsteps_behind decoy=True` occurs at Dread < 70 (about 1 in 8 dread ticks) and `decoy=False` only at Dread >= 70.
- `HC_LIE kind=phantom_listen` at most once per run, never together with `HC_RARE_LINE`.
- No `HC_SCARE_CASTLE` / `HC_AMBIENT` line rate change at hunt start: compare beats per minute in the 60 s before and 60 s after `HC_HUNT_START` over 10 hunts; they should not differ by more than the noise of the cadence.
