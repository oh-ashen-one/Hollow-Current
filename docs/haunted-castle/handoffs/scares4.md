# HANDOFF: scares4 (wave K, hc/scares4)

Base `haunted-castle` b428a8e. Model: Sonnet 5.5. 2026-10-14. Code only, no Unity. Verified: `check_source.py` (1036 assertions PASS, compiles; also compiled with the ScareDirector diff below applied to the file, then restored) and `tools/check_castle_scares.py` PASS. Files: `Assets/HollowCurrent/Runtime/Castle/CastleScares.cs`, `tools/check_castle_scares.py`.

## 1. Repetition: budgets, cooldowns, weighted picks
- `BudgetTable` (key => max per run, gap seconds): bell_single 2/240; book_drop 6/90; knife_stick 4/90; bat_pass 6/90; candle_gutter 6/90; silence 8/90; poltergeist 3/150; window_silhouette 8/90; balustrade_shape 3/180; false_candle_tell 5/90; false_compass 4/120; footsteps_behind 6/90; phantom_listen 1 (once per run).
- `Can(key)` refuses a beat over its max, inside its gap, or equal to the last beat used (no beat twice in a row, across beats, lies and ambient). Every beat method, both lies and `Silence` call it first and return false when refused, so the caller falls through to the next candidate.
- `Beat()` and `AmbientBeat()` draw by `Weight`: never used 1.25; else the time since its last use over 240 s (floor 0.12), shrinking by up to half as its budget is used up; refused beats weigh 0. If every candidate is refused, `Beat` falls back to silence (if allowed), else returns false and the director's generic cues play.
- Log suffix on every beat: `HC_SCARE_CASTLE bell_single pick=weighted w=0.42/1.31 rank=2 budget=1/2 gap=240 since=312`. `pick=` is `weighted ..`, `tick`, `ambient ..`, `silence_roll chance=..`, `escalation_payoff`, `fallback` or `direct`; `since=` is seconds since the previous use (`-` for the first). Lies carry the same fields: `HC_LIE kind=footsteps_behind ... pick=direct budget=2/6 gap=90 since=..`. A refused poltergeist logs `HC_THROW_SKIP reason=budget source=poltergeist ...`.
- `ResetRun()` clears budgets (called by `Initialize`); call it on a fresh run if the component outlives one.
- Also (critic r11): the ambient substitute no longer fires `window_silhouette` in Hunt itself (`// tell-safe:` comment; every other ambient beat still fires there).

## 2. Escalation 0 of 8: root cause
The r11 `HC_ESCALATE` lines carry no reason, so I read the surrounding logs (`rounds/r11/critic/natural1|2/player.log`). `Investigation.TryEscalate` returns false unless `Phase == Disturbance`, `GraceRemaining == 0` and a 20 % roll succeeds. In both runs the first attempt (before any hunt) was an ordinary roll failure (p 0.8), and **all six others are right after an `HC_RECOVERY`** (n1 :231->252, :332->361, :459->478; n2 :217->248, :329->352, :469->486), each followed by the next dice Warning. The director now lands apparitions 20-40 s after Recovery starts (hc/count apparition starvation fix) and the post-hunt grace is 51-60 s in those runs (30-90 s times the governor), so the apparition falls inside Recovery or inside grace, where escalation is disarmed by design (`grace=52.7`, `58.8`, `51.5` in the logs). Even with every gate open, 8 failures at 20 % happen 17 % of the time. Distance was never the problem (`near` 3.2-4.6, all under the 5.5 m range).
- Fix (by-design block kept): `CastleScares.Escalate(near)` replaces the director's inline call. It logs `HC_ESCALATE near=.. escalated=.. reason=ok|roll|grace|phase_<p>|range|rules_off|terminal phase=.. grace=.. chance=.. substitute=..`. When blocked by grace or by phase (Recovery, Survey) it spends the attempt on a payoff with probability 0.5: a poltergeist (budget permitting), else a candle gutter, else a bat pass (`substitute=poltergeist|candle_gutter|bat_pass|none`), so the apparition is never bare. It does not change the rules (`EventEscalationChance`, grace): if conversions are wanted, that is a rules decision (F1): raising the chance or letting an apparition during grace arm a deferred escalation for the end of grace.

## 3. Lies, ambient and silence budgets
All of them have table entries above (silence 8/90, footsteps 6/90 on top of its 60 s internal cooldown, phantom once, false candle 5/90, false compass 4/120, window/shape/bell/book/knife/bat/gutter). The ambient substitute and the escalation payoff go through the same `Can`, so no beat can exceed its cap or repeat inside its gap from any entry point.

## 4. Check
`tools/check_castle_scares.py` now parses `BudgetTable` and fails if any beat or lie lacks an entry, has max < 1 or gap < 90 s, if the bell is not max 2 / gap >= 240, if a beat method does not call `Can("<key>")`, if a lie never spends, if `Log()` does not spend and print `pick= budget=`, or if `Escalate` does not log `reason=` and `substitute=`. It prints the table.

## ScareDirector.cs diff for the integrator (not applied)
```diff
--- a/Assets/HollowCurrent/Runtime/ScareDirector.cs
+++ b/Assets/HollowCurrent/Runtime/ScareDirector.cs	2026-10-14 16:33:28
@@ -109,7 +109,7 @@
             if(!game.Case.Lies&&Random.value<.05f)
             {
                 if(Random.value<.5f&&Castle.FalseCandleTell()){Debug.Log("HC_LIE kind=false_candle decoy=True dread="+game.Case.Dread.ToString("F0"));return true;}
-                if(game.Player.Tool==1&&game.Case.Equipped){Castle.FalseCompassReading();Debug.Log("HC_LIE kind=false_compass decoy=True dread="+game.Case.Dread.ToString("F0"));return true;}
+                if(game.Player.Tool==1&&game.Case.Equipped&&Castle.FalseCompassReading()){Debug.Log("HC_LIE kind=false_compass decoy=True dread="+game.Case.Dread.ToString("F0"));return true;}
             }
             return Castle.DreadTick();
         }
@@ -129,7 +129,7 @@
                 game.Case.AddDread(game.Case.Rules.DreadApparition);
                 // An apparition that lands close can become a hunt with a short tell (FEAR_PLAN item 3).
                 float near=Vector3.Distance(point,game.Player.transform.position);
-                if(near<CastleScares.EscalateRange){bool escalated=game.Case.TryEscalate();Debug.Log("HC_ESCALATE near="+near.ToString("F1")+" escalated="+escalated);}
+                if(Castle)Castle.Escalate(near); // logs HC_ESCALATE with reason= and substitute= (hc/scares4)
             }
             if(!firstBasement&&Events%2==0)
             {
```
(`FalseCompassReading()` now returns bool and refuses over budget, so the decoy log must follow its result. `Escalate` is called for every apparition: it logs `reason=range` when too far, where the old code stayed silent.)

## What a windowed run must prove (log keys)
- Over a 20 minute natural session: `HC_SCARE_CASTLE bell_single` appears at most 2 times, at least 240 s apart (compare `since=`), each with `budget=n/2`.
- No two consecutive `HC_SCARE_CASTLE`/`HC_LIE` beats share a key; every repeat has `since>=90` (bell 240).
- Every castle beat line ends with `pick= budget=`; most are `pick=weighted ..` or `pick=ambient ..`; the beat mix is flatter than r11's (window 10, bell 9, gutter 5, bat 5, knife 4, book 2): no key more than 8.
- `HC_ESCALATE ... reason=` is present on every apparition; after a Recovery the reason is `grace` or `phase_Recovery` with `substitute=` not `none` about half the time; at least one `escalated=True reason=ok` across 3 natural runs if the rules allow it (otherwise the proportion of `reason=roll` over `reason=ok` should be about 4:1 with no grace).
- No `HC_AMBIENT phase=Hunt beat=window_silhouette`.
