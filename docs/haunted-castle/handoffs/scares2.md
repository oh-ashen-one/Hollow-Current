# HANDOFF: scares2 (wave G static fix, hc/scares2)

Base `haunted-castle` c4b24c1. Model: Sonnet 5.5. 2026-10-14. Static only: no Unity, no window, no build, no bake.
Verified: `INPUT_SYSTEM_REFERENCE=... python3 tools/check_source.py` (487 assertions PASS, runtime and editor C# compile) with the ScareDirector diff below applied to a scratch copy (it compiles; the file was restored, so the branch does not contain it), and `python3 tools/check_castle_scares.py` PASS (it fails when a beat's log line is removed; checked once by mutation).

## What changed (all in `Assets/HollowCurrent/Runtime/Castle/CastleScares.cs`, plus the new check tool)
- **#27 escalation.** `CastleScares.ApparitionPoint(out point)` is the castle's `VisiblePoint`: same tests, distances 3.4 m first, 4.6 m instead of 4.8, then 6 m. `CastleScares.EscalateRange = 5.5` replaces the director's `near < 4`. Director lines are in the diff.
- **#32 lies.** `PhantomListen()`: `cc_listen` 2 m behind and 1.5 m above the player (ceiling-clamped), only when `Lies`, once per run (`ListenPlayed`). `FootstepsBehind()`: a point 3-5 m behind on the same nav floor with a clear line, five `claw_steps_N` cues 0.5-0.7 s apart each 0.6 m farther away, volume 0.55, 60 s cooldown, aborted by any hunt, warning, return or menu. Both need `Lies` and no hunt (`Calm`, not `HuntActive`, not Return/Containment). Both log `HC_LIE kind=phantom_listen` / `kind=footsteps_behind`; the two existing lies now also log `HC_LIE kind=false_candle` / `kind=false_compass`. `Lie()` picks between the two.
- **#38 silence.** `Silence()` ducks the bed through the hook `CastleScares.DuckBed` (`Action<float,float>(seconds,depth)`; 6-10 s, depth 0.1), sets `Silent` for that long, logs `HC_SCARE_CASTLE silence seconds=.. duck=wired|unwired dread=..`. `Soundscape.DuckBed` is not in the tree yet, so I did not call it directly (it would not compile); wire the hook in Game once it lands (below). Chance is `SilenceChance`: 1/6 below Dread 60, 0.30 at 60 rising to 0.45 at 100, so silence clusters in the pre-hunt zone without being absent elsewhere. `Beat(allowSilence=true)` rolls it; the last-resort fallthrough is also `Silence()` now.
- **`DreadTick()`** is the whole of `ScareDirector.CastleBeat`: silence roll, then lies (candle tell 1/3, compass, then `Lie()` half the time), then window silhouette 20 %, balustrade 15 %, then `Beat(false)`. This is what lets the director diff stay small.
- **Throw gates re-read.** Nothing in `Throwable`/`Poltergeist`/`ThrowNearbyProp` is a never-true gate in the castle any more: layer mask `~((1<<2)|(1<<30))` matches no castle collider layer; the camera is `Player.View`; limits are 6 m, 65 degrees, 0.4 m (0.9 m in the director for Throwables); the table proxy is handled by `SightBlocked`; batching by `StaticBatched`. Remaining gates are rhythm gates, not code bugs: throws only happen in `Trigger` on every second apparition (`Events%2==0`) and `ScareRhythm` spaces apparitions 105-165 s after a 90 s/65 s rest, plus the `approachingHunt` gate (#26, below). The one reject path without a log was the Poltergeist early return (`!Ready||Modal`): now `HC_THROW_SKIP reason=not_ready source=poltergeist`. Every other reject path logs through `Throwable.LogSkip` (checked by the tool). Props added after `ScareDirector.Initialize` (stuck kitchen knives) are not in the director's list; `Poltergeist` sees them through `Throwable.All`.
- **`tools/check_castle_scares.py`** (pure python): asserts the 11 beat methods exist and log, `TryBeat` registers five beats, `DreadTick` reaches silence/lies/peripherals/Beat, both lies carry `HC_LIE`, every Poltergeist and `ThrowNearbyProp` reject path logs, `MakeThrowable` call sites >= 20 (30 today; the build logs `HC_THROWABLES placed=<n>`, 38 loop-expanded), the director is castle-gated. Exit 1 on any failure.

## Exact ScareDirector.cs diff for the integrator (not applied here)
The `approachingHunt` lines are critic #26 (not in my list; included because that gate starves every apparition-driven throw in the castle). Drop those two hunks if #26 belongs to someone else.
```diff
--- a/Assets/HollowCurrent/Runtime/ScareDirector.cs	2026-10-14 12:11:34
+++ b/ScareDirector.cs	2026-10-14 12:14:18
@@ -65,7 +65,7 @@
             if(game.Player.Hidden||game.Case.Phase==Phase.Hunt||game.Case.Phase==Phase.Warning||game.Case.Phase==Phase.Recovery||game.Case.Phase==Phase.Containment)return;
             // Reserve the approach to a natural hunt for dread, not a competing
             // loud apparition. The nine-second hunt warning remains intact.
-            bool approachingHunt=game.Case.Phase==Phase.Disturbance&&game.Case.Evidence.Count>=2&&game.Case.PhaseTime>=55;
+            bool approachingHunt=!IsCastle&&game.Case.Phase==Phase.Disturbance&&game.Case.Evidence.Count>=2&&game.Case.PhaseTime>=55;
             if(!basement&&game.Player.transform.position.y< -2.5f&&!approachingHunt&&rhythm.CanSurprise)
             {
                 if(Trigger(true)){basement=true;BasementFlashes++;}
@@ -91,29 +91,14 @@
                 // The last beat is deliberately unresolved silence.
             }
         }
-        // Castle dread beats: lies above the Lies threshold (false candle tell, false compass), otherwise the
-        // castle's own beats (Beat picks a book drop, knife, bat, bell; the window silhouette; the balustrade shape).
-        bool CastleBeat()
-        {
-            if(!Castle)return false;
-            // Lies (Dread >= 70): one candle tell in three points at an empty doorway; the compass lies while raised.
-            if(game.Case.Lies)
-            {
-                if(Random.value<1f/3&&Castle.FalseCandleTell())return true;
-                if(game.Player.Tool==1&&game.Case.Equipped&&Random.value<.5f){Castle.FalseCompassReading();return true;}
-            }
-            // Peripheral beats need the player's view and return false cheaply when nothing qualifies.
-            float r=Random.value;
-            if(r<.2f&&Castle.WindowSilhouette())return true;
-            if(r<.35f&&Castle.BalustradeShape())return true;
-            Castle.Beat();return true; // Beat logs its own HC_SCARE_CASTLE line
-        }
+        // Castle dread beats: the whole tick (silence roll, lies, peripheral sightings, Beat) lives in CastleScares.DreadTick.
+        bool CastleBeat()=>Castle&&Castle.DreadTick();
         public bool Trigger(bool firstBasement=false)
         {
             if(game.Modal||game.Player.Hidden||game.Case.Phase==Phase.Hunt||game.Case.Phase==Phase.Warning||game.Case.Phase==Phase.Recovery||game.Case.Phase==Phase.Containment||game.Case.Phase==Phase.Return||!rhythm.CanSurprise)return false;
             if(!firstBasement&&!rhythm.AppearanceDue)return false;
-            if(game.Case.Phase==Phase.Disturbance&&game.Case.Evidence.Count>=2&&game.Case.PhaseTime>=55)return false;
-            if(!VisiblePoint(out var point)){if(!firstBasement)Dread();return false;}
+            if(!IsCastle&&game.Case.Phase==Phase.Disturbance&&game.Case.Evidence.Count>=2&&game.Case.PhaseTime>=55)return false;
+            if(!(IsCastle&&Castle&&Castle.ApparitionPoint(out var point))&&!VisiblePoint(out point)){if(!firstBasement)Dread();return false;}
             bool rush=!firstBasement&&Events%3==2;
             if(!game.Creature.ManifestAt(point,0,firstBasement?.32f:rush?1.6f:.55f,true))return false;
             game.Creature.FacePlayer();if(rush)game.Creature.BeginFakeRush();
@@ -124,7 +109,7 @@
                 game.Case.AddDread(game.Case.Rules.DreadApparition);
                 // An apparition that lands close can become a hunt with a short tell (FEAR_PLAN item 3).
                 float near=Vector3.Distance(point,game.Player.transform.position);
-                if(near<4){bool escalated=game.Case.TryEscalate();Debug.Log("HC_ESCALATE near="+near.ToString("F1")+" escalated="+escalated);}
+                if(near<CastleScares.EscalateRange){bool escalated=game.Case.TryEscalate();Debug.Log("HC_ESCALATE near="+near.ToString("F1")+" escalated="+escalated);}
             }
             if(!firstBasement&&Events%2==0)
             {
```

## Other lines the integrator must add
- `Game.cs`, castle branch after `CastleDread.Initialize(this)`: `CastleDread.DuckBed=Sound.DuckBed;` (needs `public void DuckBed(float seconds,float depth)` on `Soundscape`: ducks wind, room tone and heartbeat to `depth` over 1 s, holds, restores over 3 s). Until then the silence beat logs `duck=unwired`.
- `Soundscape.RareLine`: skip when `game.CastleDread&&game.CastleDread.ListenPlayed`, and call `game.CastleDread.MarkListenPlayed()` when it plays, so `cc_listen` stays at most once per run across both paths (critic #30 also notes the R-key `cc_listen` at `Game.cs:304`, not mine).
- Optional: `Soundscape.ClawSteps` and other quiet bed sounds can skip while `CastleDread.Silent`.

## What a windowed run must prove
- HC_ESCALATE: `near` between 3 and 5.5 in natural play and `escalated=True` at least once in 10 apparitions (needs `EventEscalationChance` > 0 in the castle rules).
- HC_THROW (single) and `HC_THROW poltergeist` > 0 in natural play with 2+ evidence and 60+ s in Disturbance (only possible with the #26 hunks). Any rejection prints `HC_THROW_SKIP reason=` with prop, distance and angle.
- `HC_LIE kind=phantom_listen` exactly once per run, only after Dread reached 70, and not together with `HC_RARE_LINE` (needs the Soundscape lines above). `HC_LIE kind=footsteps_behind` only at Dread >= 70, heard from behind, steps receding, nothing visible; no `HC_LIE` while a hunt, warning or return is active.
- `HC_SCARE_CASTLE silence seconds=` shows `duck=wired`; the wind, room tone and heartbeat are audibly lower for 6-10 s and recover without a click; silence lines at Dread >= 60 are about twice as frequent as below 40 over a 10 minute session, and still appear below 40.
- A silence is never followed within 6 s by a hunt-state change (HUD, music, radio), otherwise silence becomes a tell.
