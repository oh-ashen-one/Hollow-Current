# Round 10a: static critique (code, plan, log only)

Critic: opus-critic (Opus 5.5 high), 2026-10-14. No window, no Unity, no build. I did not read any HANDOFF, NOTES or handoffs/* files.
Judged commit: `haunted-castle` at **b516d81**. Note: HEAD moved to **c4b24c1** while this review ran (merge of hc/throws plus `voice_cc_contain`).
Those commits change only `ScareDirector.cs`, `CastleScares.cs`, `Throwable.cs`, `PlaytestDriver.cs` (one line), `NativeBuild.cs` and audio.
Line numbers for `ScareDirector.cs` and `CastleScares.cs` are at b516d81. Every other file is identical at both commits.
Evidence log: `.playtest-fear.log` (09:53, build from 34a0de9). It is one scripted run, and the driver forced Dread to 55 twice.

## Verdict

**Fear score: 4 / 10** (8 = a Phasmophobia-grade hunt loop).

All 19 systems exist on paper. But the castle still gives off about fifteen exact "hunt on / hunt off / you are safe" signals. Hiding with the torch off is still a guaranteed save in every state except a dive already under way. Dread below 40 is a guaranteed no-hunt zone, and two separate channels report it to the player. Several fear beats can almost never fire, because a Bellwether-era gate in `ScareDirector` suppresses them. The research's number-one fear killer (a single reliable danger tell) has been swapped for many reliable tells.

**Single biggest gap (testable):** in the castle, a no-tell hunt must cause no player-observable change in its first 6 s apart from sound and light coming from the Count's own position. The HUD OBJECTIVE text, the radio reply, the instrument line, `music_hunt`, the heartbeat volume, the wing loop volume, `cc_hunt_smell`, room tone, the rim light and dialogue-card styling must all stay as they were. To verify, log each channel every frame across 20 forced no-tell hunts and require zero deltas before t = 6 s.

## Findings

Severity: S1 = breaks the fear contract or is an audio bug (README Part 3 #6), S2 = materially weakens fear, S3 = minor.

### Q1 Predictability: deterministic tells that remain

1. **S1. Dread < 40 is a guaranteed no-hunt zone.** `Investigation.cs:173-191`: the floor clock and dice both need `Dread >= HuntDreadMin` (40, `HuntRules.cs:96`). The only hunts below 40 are the one-off first-hunt guarantee, the bell, the sarcophagus gamble and a rare escalation (see #14). Natural Dread rises 0.05/s when lit (log: 1.8 at t=38 s), so a careful player spends most of a run in a provably safe state. No "Demon" rule exists (README Part 1 #2). *Fix:* after the first hunt, roll at every Dread level (e.g. 1/40 per roll below 40), and draw each run's HuntDreadMin from 30 to 50.
2. **S1. The heartbeat reports "Dread > 40" exactly.** `Soundscape.cs:84` returns 0 at Dread <= 40, so silence means the dice are disarmed and any heartbeat means they are armed. It is the most precise tell in the game. *Fix:* start the curve at 20, add ±8 slow random offset per run, and let it lie above LieThreshold.
3. **S1. The journal pulse is a readable Dread gauge.** `Interface.cs:351-352`: noise is ±2 on a 0-100 scale, and rate = 0.9 + 0.022 x Dread beats/s. Opening the journal pauses the game, so checking is free. 40 vs 38 is distinguishable. *Fix:* ±12 noise, a 20-30 s lag, and a pulse that lies (shows calm) when `Lies` is set.
4. **S1. The HUD OBJECTIVE flips to the hunt goal the instant any hunt starts, including no-tell hunts, and flips back at Recovery.** `Interface.cs:477` draws `Case.Goal`, and `Investigation.cs:301` returns `GoalHunt`. This alone cancels the no-tell hunt. *Fix:* in the castle, keep the pre-hunt goal text through Hunt and Recovery.
5. **S1. `cc_hunt_smell` plays at the Count's head at every hunt start**, including no-tell, gamble and hostile-return hunts. `CastleRun.cs:219`. *Fix:* remove it here and keep it only for the near-miss (FEAR_PLAN item 10).
6. **S1. The wing loop runs at 0.7 for the whole hunt (even while he is perched) and at 0 between hunts.** `CastleRun.cs:233`. This is a hunt-state lamp that also gives his position. *Fix:* drive it from his actual flight (Travel/Chase only, in hunts and out), not from `HuntActive`.
7. **S1. The heartbeat has a 0.5 floor and `music_hunt` plays at 0.15 throughout every hunt and Warning**, and from second 4 of a no-tell hunt. `Soundscape.cs:142` and `:195`, with `hunting` defined at `:186`. Both turn off exactly when the hunt ends. *Fix:* in the castle, the heartbeat follows Dread only, and `music_hunt` is 0.
8. **S1. The radio is a free hunt detector.** `Game.cs:305`: R during Warning or Hunt always answers `RadioHunt` ("Get under cover"). *Fix:* in the castle during hunts, return static or a random hint with p = 0.5. During a hunt the radio should mostly fail (README Part 4, choppy transmissions).
9. **S2. The instrument line shows "INTERFERENCE · RECORDING SUSPENDED" and false readings for the whole Warning and Hunt.** `Investigator.cs:302-307`. *Fix:* in the castle, show interference only when the Count is within about 12 m, so it gives proximity, not phase.
10. **S2. The 32 s Recovery is guaranteed and announced.** `Investigation.cs:166-169`: no dice and no escalation run in Recovery. `CaseBook.cs:163` adds "He has gone back to his perch. Breathe. You have a quiet window." Hunt length is a fixed 38 s (`HuntRules.cs:99`). *Fix:* randomise hunt length (25-50 s) and recovery length (10-40 s), allow a 1-in-8 dice hunt in Recovery when Dread >= 80, and drop the "quiet window" line in the castle.
11. **S2. The rim light goes from 0.5 between hunts to 0 in hunts** (`Presence.cs:383`, `:419`, `:546`). If you can see him perched, the moment the hunt starts is visible. **He also never moves while observed between hunts** (`Presence.cs:1092-1111`), so any movement you see means a hunt (apart from the fly-off after a hunt). *Fix:* use one rim value in both states. Between hunts, allow a rare observed re-perch flight (1 in 5) that is not a hunt.
12. **S2. Hunt starts kill room tone and the chapel brazier, even for no-tell hunts.** `Investigation.cs:203` sets `BreakerOn=false`, and `Soundscape.cs:206` gates room tone on it. *Fix:* in the castle, do not change `BreakerOn` on no-tell starts.
13. **S2. In the gatehouse, `music_safety` plays at 0.32 unless hunting** (`Soundscape.cs:189-192`). Music cutting out means a hunt has started. Also see Q6.
14. **S3. Other exact signals.** Dialogue cards switch to danger styling during HuntActive (`Interface.cs:466`). The Warning shows a numeric countdown, "SIGNAL COLLAPSE n" (`Interface.cs:494`). The torch blackout (`Game.cs:319`) fires at dive *start*, which signals "diving now"; FEAR_PLAN item 16 says the last second of a dive.
15. **S2. A slow player triggers the first hunt the moment they record the second evidence.** `Investigation.cs:178-181` compares `PhaseTime` from Disturbance entry, not time since the second record. If PhaseTime is already past 95 ± 25 % (71-119 s), recording the second piece of evidence starts the threat on the next tick.

### Q2 Safe-zone: hidden with the torch off

16. **S1. Hidden with the torch off is still a guaranteed save in every flight state except an active dive.** `CountSees` returns false (`Presence.cs:826`). The only lethal hide check (`atHideEntrance`) exists only in `CountChase` (`:689-695`). Chase can only be entered with `CanSeePlayer` (`:586`, `:597`, `:614`).
    - **Watched-hide branch:** `WitnessHide` (`:108`) is called before `Hidden=true` (`Investigator.cs:353-354`), so it does fire. But when the Count was perched, travelling or landed at that moment, he only searches the hide point, lands (`:614-617`), and leaves for a perch after 2.6 s. The arrival note teaches "Never hide while he is watching you" (`CaseBook.cs:153`), so that rule is a bluff.
    - **The log does not prove the watched-hide kill.** `Witnessed()` in the driver hides with the **torch on** (driver line 338). The grab at hunt 2 (`HC_COUNT_GRAB ... witnessed=True`, preceded by `seen=True` in Land) came from torch-on visibility, not the witnessed path.
    - *Fix:* when `witnessedHide` is set in any non-retreating state, route to `knownHide`'s exit and run the hide-entrance grab from there.
17. **S1. The near-miss never opens.** `NearMissTick` (`Presence.cs:1279-1318`) only breathes, rattles and leaves, and it costs +20 Dread at most. It fired 0 times in the log (roll 0.97). *Fix:* at Dread >= 80 or in Long Night, 1 in 4 near-misses open the door.
18. **S2. The door-held branch denies entry; it does not violate a hide you are already in.** `Investigator.cs:351` at Dread >= 90 blocks every hide (not "a hide spot", as the plan says). It never fired in the log.
19. **S2. Escalation, hostile Return, the bell, the gamble and the mic all use the same `CountSees` rule, so none of them can reach a hidden, torch-off player.** The microphone (`Investigator.cs:288-289`) only seeds `Memory`, which leads to a search and a landing, never a grab.
20. **S2. `HearPlayerRoughly` breaks its own "3-5 m when hidden" rule.** The NavMesh snap at `Presence.cs:435-436` produced offsets of 1.0, 0.5 and **0.3 m** while hidden (log, hunt 1). That is under the 0.35 m `knowsThisHide` threshold, so it is a latent kill if he is in Chase. *Fix:* reject snapped guesses closer than `HeardOffsetHiddenMin`.
21. **S2. The gatehouse stays safe in practice.** Item 11 fires once per run, deterministically, after 40 s of standing still (`Presence.cs:1202-1206`), and cannot harm the player. The arrival note promises "Outside the walls you are safe".

### Q3 Bellwether isolation

The rules core holds. `Investigation` with `HuntRules.Bellwether` is equivalent to the 2c9a29b code. The dread gate passes because 0 >= 0. `huntClock` resets match the old `PhaseTime` resets. Warning, Hunt and Recovery lengths are 9 / 38 / 32 s. `Die` behaves as before. `ScareDirector`, `Soundscape` and `Presence` (Split Grin path) differences are all castle-gated or log-only. I could not break the rules. These **are** Bellwether behaviour changes, though, so the claim "identical" is false:

22. **S2. The title screen is fully replaced for Bellwether** (`Interface.cs` `Title()`, `TitleScreen.cs`): new layout, Cinzel type, Ken Burns. This is intended by T1, but it is not identical.
23. **S2. The shipped Bellwether build now opens windowed.** `Game.cs:24-33` `EnsureWindowed` applies to every map, and project settings turn off `defaultIsNativeResolution`. This was an owner rule on 2026-10-14; the release notes should say so.
24. **S3. In Bellwether, the settings-page toggle's controller id changed** from the label-derived `hc-ui/DIALOGUE DISPLAY` / `hc-ui/CONTROLS & AUDIO` to `hc-ui/settings-page` (`Interface.cs:300`). No tool references the old ids. Focus behaviour after the toggle differs.
25. **S3. `SavePreferences` now writes `hc.long-night`, `hc.microphone` and `hc.mic-threshold` from Bellwether sessions** (`Game.cs:397`, `:401`). This is harmless but not identical.

### Q4 Dead gates (siblings of zero throws)

26. **S1. A Bellwether gate (`approachingHunt`) starves castle apparitions, throws, poltergeists and escalations.** `ScareDirector.cs:68`, `:75` and `:115` (b516d81) block `Trigger()` once Disturbance has run 55 s with two or more evidence. That rule existed for Bellwether's 95 s timer. The castle uses dice, so Disturbance runs indefinitely and every apparition-driven beat stops. Combined with `ScareRhythm` (Rest 90 / Rest 65, spacing of 105-165 s), the only window is about 33-55 s after each Recovery, and only when an appearance is due. Throws need every second apparition, so they are close to impossible even after the throw fix. *Fix:* skip `approachingHunt` when `IsCastle`.
27. **S2. Escalation almost never qualifies.** `TryEscalate` needs `near < 4` (`ScareDirector.cs:127`), but `VisiblePoint` tries 4.8 m first for every angle (`:144`). In open castle halls the result is about 4.8 m, so the 3.4 m candidate is used only in cramped rooms. HC_ESCALATE was 0 in the log. *Fix:* use < 5.5 m, or try 3.4 m first in the castle.
28. **S2. The witnessed-hide open is gated on Chase only** (see #16): it is a dead gate for the common case.
29. **S2. Item 14 "learned" is unproven** (HC_COUNT_LEARNED = 0). `lastSeen` is set only while he can see you, and the hostile return excludes it (`Presence.cs:405`).
30. **S2. The `cc_listen` rare line is no longer rare.** `Game.cs:304` with `Soundscape.cs:94`: pressing R during any interference plays `cc_listen` non-spatially, every time. The once-per-run, one-run-in-three, behind-and-above line (`Soundscape.cs:152-164`) is cheapened. HC_RARE_LINE was 0 in the run (`rareLine=False`).
31. **S3. The near-miss can abort silently** when `PlanRoute(landing)` fails (`Presence.cs:1275`). It is unproven in any run.
32. **S3. Not implemented: the item 5 lies "phantom ear-trumpet `cc_listen`" and "footsteps behind you".** `Lies` is read only in `ScareDirector.cs:100`.

### Q5 Rules the hunter breaks

33. **S2. Opening a hide only happens mid-dive** (#16), so the taught rule "never hide while he is watching" mostly fails in the player's favour.
34. **S2. The hostile Return violates "Outside the walls you are safe"** (`CaseBook.cs:153`). The bridge is a kill zone, but the only telegraph is `cc_stay` (via `Speak("return")`), `cc_hunt_smell` and chains. `SealedMessage` and `GoalReturn` say nothing about him coming. A death there will read as a bug. *Fix:* add a dispatch line ("He is free. Run.") and a visible first swoop over the courtyard.
35. **S3. "Lantern off" is taught (Warning, Goal and radio text), but the torch has no effect in the open.** `CountSees` ignores the torch unless you are hidden. Either make torch-on extend his sight range in the open, or stop teaching it.
36. **S3. The gatehouse violation (item 11) is well telegraphed** (cc_stay, chains, landing) but harmless and fires once. **The door-held rule** is telegraphed with a notice and a rattle (good). Neither was seen in a natural run.

### Q6 Audio

37. **S1. Music is not off in the castle.** `music_safety` plays at 0.32 in the gatehouse and on the title (`Soundscape.cs:189-192`). `music_hunt` plays at 0.15 through every Warning and Hunt, including hostile Return (`:186`, `:195`). Only `music_house` is 0.
38. **S2. Silence is not used as an active beat.** `CastleScares.Beat` "silence" (`:66`, b516d81) only logs. Nothing dips the bed before a hunt. The no-tell 4 s `tellHeld` silence is undone by `cc_hunt_smell` and the wing loop at t = 0 (#5, #6).
39. **S1. The heartbeat is tied to hunt phase** (0.5 floor, `Soundscape.cs:142`), not to Dread alone.
40. **S1. Missing audio keys.**
    - At b516d81: `voice_cc_contain` (log `HC_MISSING_ART speech voice_cc_contain`). It was added in b6c21b2 and is not in any build yet.
    - At both commits: `voice_cc_hint_signal`, `voice_cc_hint_trace` and `voice_cc_hint_voice`. `Game.cs:307-308` maps them through `Soundscape.cs:94-95`, so every pre-diagnosis radio press in the castle is silent and logs MISSING_ART.
    - All F4 contract keys are present on disk: `heartbeat_loop`, `wind_battlements_loop`, `door_rattle`, `book_drop`, `knife_stick`, `claw_steps_0..3`, `bat_pass`, `bell_single`, `voice_cc_kit`, `voice_cc_ending`.

### Q7 Title screen vs the approved mocks

I viewed both PNGs (2560x1440) and the generator, `title_mock.py`. The coordinates match the mocks: brand at 44/62 (Georgia 12, tracking 3), HOLLOW/CURRENT at 78/168 (96, tracking 10), the copper rule at y 280, the name at 296 (34, tracking 2.5), the tagline at 350 (Georgia italic 20), cards at y 440 (330x185, pitch 350, brightness 1.0/0.6, crop rects), BEGIN at 80,668,680x60, links at y 766 (16, tracking 2.2, alpha 0.78), the footer rule at 843, footer text at 863 and the right label at 1260. Remaining differences visible from code:

41. **S3. The tagline line pitch is Unity's Georgia `lineHeight`**, not the mock's fixed 23.5 design px (`Interface.cs` `Label(80,350,640,64,…)`). The second line may sit 1-4 px off.
42. **S3. The Cinzel load is never logged.** `TitleScreen.Prepare` silently falls back to Georgia (`TitleScreen.cs:34-35`), and `HC_UI_READY` reports only `font=Georgia` (the body serif). Add an `HC_TITLE_FONT cinzel=<bool>` log line.
43. **S3. The mock is static.** Ken Burns (40 s, 6 %, pivot 0.62, 4 s overlap), the 0.6 s map crossfade and fog drift (4 px/s) are unreviewed. With a gamepad, the ENTER cap is hidden unless BEGIN has focus (`Interface.cs` `TitleBegin`). The S and Q shortcuts still work but are no longer shown.

### Q8 Fear-worthiness

44. **Score 4/10.** Positives: real dice with false alarms and no-tell hunts in the rules core. The SCP-173 perch rule. The gatehouse visit. Hostile Return over a 7.2 s bridge (the log shows the Count within 1.55 m on the bridge). Grab variants wired. Bellwether rules isolated. What caps it: the tell flood (#1-#14), the unbreakable hide (#16-#17), dead apparition-driven beats (#26-#27), and audio holes (#37-#40). In the only run, near-miss, escalation, learned, door-held, rare line and throw all logged 0.

**The three highest-leverage changes:**
1. **Remove every non-diegetic hunt-state channel in the castle** (#4-#9, #11-#13, #37, #39). This makes the no-tell hunt and the false warning real.
2. **Break the two guarantees**: Dread < 40 and the 32 s announced Recovery (#1-#3, #10). Add a low-Dread "Demon" roll, randomised lengths and a lying pulse.
3. **Make hiding fallible and turn the starved beats back on**: watched hides opened from any state, near-miss opening at high Dread, no hidden-guess snap under 2 m, and `approachingHunt` off in the castle (#16, #17, #20, #26, #27).

## What the windowed run must prove

- [ ] A no-tell hunt shows no change in HUD goal, radio, instrument, music, heartbeat, wing loop or voice for 6 s (per-frame channel log, 20 forced hunts).
- [ ] A Dread 0-39 session of at least 10 minutes after the first hunt contains at least one hunt (only valid once a low-Dread roll exists). Today it must contain zero; record that as a fail.
- [ ] A watched hide with the **torch off** while he is perched or travelling results in the hide being opened (HC_COUNT_GRAB hideEntrance=True, torch=False). Today the predicted result is no grab.
- [ ] A near-miss runs end to end (HC_COUNT_NEARMISS begin, then hold, then leave), with a door rattle and `cc_hunt_smell` heard at the slats.
- [ ] HC_THROW > 0 and HC_THROW poltergeist > 0 **in natural play with 2 or more evidence and 60 s or more in Disturbance** (checks #26, not just the driver's direct call).
- [ ] HC_ESCALATE escalated=True at least once in natural play.
- [ ] No HC_MISSING_ART lines across a full run that includes radio presses before diagnosis.
- [ ] `cc_listen` is heard at most once per run (count HC_COUNT_VOICE plus Speak calls).
- [ ] The heartbeat is silent during a hunt at Dread 30 (it must follow Dread, not phase).
- [ ] Bellwether: an unmodified 95 s, 9 s, 38 s, 32 s cycle (`RulesTests` green), and the title matches `title_mock_bellwether.png` within a 2 px offset at 2560x1440. An `HC_TITLE_FONT cinzel=True` line is logged.
- [ ] Hostile Return: a death on the bridge is preceded by an on-screen or voiced telegraph the player can attribute.
