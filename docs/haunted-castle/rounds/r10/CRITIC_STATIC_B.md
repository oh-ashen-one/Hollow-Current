# Round 10b: static re-critique of the wave G fix range

Critic: opus-critic (Opus 5.5 high), 2026-10-14. Static only: no window, no Unity, no build, no run. I did not read HANDOFF.md or handoffs/*.
Judged commit: **6036196** (range `c4b24c1..6036196`). Line numbers below are at 6036196.

- HEAD moved to **40ec63d** while this review ran. That commit is proof tooling only (PlaytestDriver modes, CaptureMode, `Interface.HudGoal` made public, read-only getters). Core is untouched.
- The working tree also has 49 uncommitted castle mesh, material and `Castle.unity` changes from another session. I did not touch them.
- Tools run (compiled the working tree; Core is identical to 6036196):
  - `check_source.py`: PASS, 939 assertions.
  - `check_castle_scares.py`: PASS.
  - `check_audio_keys.py`: OK, 65 uses and 18 required ids.

## Verdict

**Fear score: 5 / 10** (bar 8; r10a gave 4).

Wave G removed most of the *ambient* hunt lamps: the HUD goal, music, the heartbeat floor, `cc_hunt_smell`, the rim light, the brazier/room tone, card styling and the countdown. It also broke the Dread < 40 guarantee.

What it did not remove:
- **On-demand hunt detectors.** The player can ask for them, and several are free or exact:
  - the radio (5/6 per press);
  - the drawbridge winch note (exact, in the safe zone);
  - the journal "ready" line (exact, free because the journal pauses);
  - the ear-trumpet reply (exact);
  - evidence records (exact).
- **Inference channels that are deterministic by construction.** Between hunts every unobserved move is a silent teleport, so sustained wings from an unseen Count mean a hunt. The 12 m interference rule is now a through-wall radar.
- **Guaranteed-safe states:**
  - an unwitnessed torch-off hide below Dread 80;
  - Survey and Containment;
  - Recovery below Dread 80, whose start is observable.

Nothing in the range has runtime evidence. Every runtime claim below is unproven until the windowed checklist runs.

**Single biggest gap (testable):** in the castle, no player action may return a different result during a no-tell hunt than outside one, unless the result comes from the Count's own position and line of sight. To verify, log every player query 40 times in Disturbance and 40 times in no-tell hunts at the same position, and require identical response distributions (chi-square p > 0.2). The queries are:
- radio press (`Game.cs:310-325`);
- winch (`Interactable.cs:30`);
- journal open (`Interface.cs:443`);
- ear-trumpet at the voice spot (`Investigator.cs:345`);
- capture-to-record (`Investigation.cs:140`);
- brazier (`Investigation.cs:167`);
- bell with the sword (`CastleRun.cs:99`).

## Status of the 44 r10a findings

Fixed 28 · Partly 9 · Not fixed 7 · Regressed 0. One sub-case of #16 regressed; it is listed as N2.

| # | Sev | Status | Evidence at 6036196 |
|---|---|---|---|
| 1 | S1 | Fixed | Boundary drawn 30-50 per run (`Investigation.cs:37`). After the first hunt a flat 1/40 roll applies below it (`:249`). RulesTests: 96.5 % of 10-minute low-Dread sessions hunt, mean wait 161 s. Survey is still dice-free (see N7). |
| 2 | S1 | Fixed | Curve starts at 20 with a ±8 per-run offset (`Soundscape.cs:70,88-92`). Driven by FeltDread (20-30 s lag, ±12 noise), not Dread (`:166`). Silence no longer marks the boundary. The lie behaviour opens a new leak (N6). |
| 3 | S1 | Fixed | Pulse = FeltDread ± 12 Perlin (`Interface.cs:366`), lagged and lying. N6 applies here too. |
| 4 | S1 | Partly | The HUD holds the calm goal through Hunt and Recovery (`Interface.cs:351-358`). But raw `Case.Goal`, which is GoalHunt in any hunt, still reaches the player through the drawbridge winch note (`Interactable.cs:30`, N1) and the diagnosed radio (`Game.cs:323`). The frozen `goalSurvived` also flips at the end of Recovery when Evidence == 3 before the first survived hunt (`Interface.cs:355-357`), which marks the end of Recovery. |
| 5 | S1 | Partly | `cc_hunt_smell` is gone from hunt start (`CastleRun.cs:225`). But every non-swoop hunt sits 1-2 s on its start perch (`HuntStartPerchMin/Max`), then `BeginSearch` says `cc_hunt_hungry` (`Presence.cs:544`). That is the same class of hunt-only line at t ≈ 1-2 s. |
| 6 | S1 | Fixed | The loop follows measured flight speed in both states (`CastleRun.cs:239`, `Presence.cs:1084`). A new inference leak replaces it (N3). |
| 7 | S1 | Fixed | No heartbeat floor (`Soundscape.cs:166-170`). `music_hunt` is 0 in the castle (`:224`). |
| 8 | S1 | **Not fixed** | Outside danger the radio gives the *deterministic* hint (first missing evidence, `Game.cs:324`). In a hunt it gives static at p 0.5, otherwise a *uniform random* hint (`:315-316`). P(in-hunt answer looks like the calm answer) = 1/6, so each press detects a hunt with 5/6 probability. The 8 s cooldown allows two presses in 9 s (97 %). Pressed from the gatehouse it carries no risk (`Hear` radius 11 m). |
| 9 | S2 | Fixed | The phase lamp is gone. Interference is now driven by proximity (`Investigator.cs:300-306`). This creates N4. |
| 10 | S2 | Partly | Lengths are randomised: hunts 25-50 s (`Investigation.cs:289`), recoveries 10-40 s (`:196`). The line is silenced (`:198`). Recovery dice only run at Dread ≥ 80 (`:209`), so below 80 Recovery is still a guaranteed window. Its start is observable: he flies off, and the wings stop abruptly when he is unseen (N3). |
| 11 | S2 | Partly | One rim value, 0.35, in both states (`CountClaudeProfile.cs:89`). Observed re-perch fires 1 time in 5, only at perch-timer expiry, and always to a perch ≥ 8 m from her (`Presence.cs:1179-1186`). A hunt flight leaves within 1-2 s of perching and heads toward her or her last-known spot. Timing and direction still separate the two. |
| 12 | S2 | Fixed | No-tell starts leave `BreakerOn` (`Investigation.cs:270-272`, `BeginNoTellHunt`, `BeginWarning`, `TryEscalate`). |
| 13 | S2 | Fixed | `safe=false` for a started castle (`Soundscape.cs:220`). |
| 14 | S3 | Fixed | Castle card styling only for Warning or interference (`Interface.cs:482`). Non-numeric label (`:511`). Torch failure moved to `DiveLastSecond` (`Game.cs:133`, `Presence.cs:743-748`). Residual: N10. |
| 15 | S2 | Fixed | `firstHuntClock` counts from the second record (`Investigation.cs:243-246`). |
| 16 | S1 | Partly | A witnessed hide now opens from perch, travel or land (`Presence.cs:110-116, 620-624, 1373-1470`). An **unwitnessed** torch-off hide is still a 100 % save below Dread 80 outside Long Night. `CountSees` returns false (`:902`). Hidden guesses are now ≥ 3 m (#20), and near-miss opens only at ≥ 80 (#17). The dive sub-case regressed (N2). |
| 17 | S1 | Partly | Opens only at Dread ≥ 80 or in Long Night (`Presence.cs:1405-1407`). Chance: 0.33 near-miss × 0.25 open ≈ 8 % per hunt. Below 80 the chance is 0. |
| 18 | S2 | Not fixed | `DoorHeld` still blocks every hide and is an exact "Dread ≥ 90" readout (`Investigator.cs:361`, `Investigation.cs:60`). |
| 19 | S2 | Partly | Only the ≥ 80 near-miss open reaches the hidden player. The mic still only seeds Memory (`Investigator.cs:289`). Escalation, bell and gamble add nothing. The hostile return excludes near-miss. |
| 20 | S2 | Fixed | Snapped guesses under the minimum are rejected (8 bearings), with an unsnapped fallback at the minimum (`Presence.cs:470-490`). |
| 21 | S2 | Not fixed | Unchanged. The gatehouse visit happens once and is harmless. |
| 22 | S2 | Not fixed (accepted) | The title is still replaced for Bellwether. #41 adds another Bellwether title change (tagline pitch). |
| 23 | S2 | Not fixed (doc) | `EnsureWindowed` still applies to every map (`Game.cs:100`). No release note in range. |
| 24 | S3 | Fixed | `CastleScreen?"settings-page":null` (`Interface.cs:303`). |
| 25 | S3 | Fixed | `CastleSession` gate (`Game.cs:424-434`). |
| 26 | S1 | Fixed (unproven) | `approachingHunt` is Bellwether-only in both places (`ScareDirector.cs:70,102`). |
| 27 | S2 | Fixed (unproven) | `ApparitionPoint` tries 3.4 m first (`CastleScares.cs:191-209`). Escalates under 5.5 m (`ScareDirector.cs:114`). |
| 28 | S2 | Fixed | Witnessed path no longer needs Chase (`Presence.cs:620-624`). |
| 29 | S2 | Fixed | Heard positions update `lastSeen` (`Presence.cs:107`). Learned also runs after the swoop (`:446`). New problem: it is never cleared (N8). |
| 30 | S2 | Fixed | The castle radio no longer speaks `whisper`/`cc_listen` (`Game.cs:312-317`). One `cc_listen` per run is shared with `PhantomListen` (`Soundscape.cs:182-183`, `CastleScares.cs:143-152`). |
| 31 | S3 | Fixed | Four landings plus the nearest NavMesh point, each miss logged; `abort=route` is logged (`Presence.cs:1390-1403`). |
| 32 | S3 | Fixed | `PhantomListen` and `FootstepsBehind` (`CastleScares.cs:133-185`). |
| 33 | S2 | Fixed | The taught rule is enforced from any state. It is slow; see N2. |
| 34 | S2 | Fixed | `SealedMessage`, `GoalReturn`, `cc_dispatch` and the swoop all announce it. The swoop card is never visible (N9). |
| 35 | S3 | Fixed | Torch on in the open gives sight ×1.5 (`Presence.cs:910`). |
| 36 | S3 | Not fixed | Unchanged (gatehouse visit is harmless, and neither visit nor door-held has a natural-run proof). |
| 37 | S1 | Fixed | No castle music in play. The title keeps the safety theme. |
| 38 | S2 | Partly | Silence ducks wind, room tone and heartbeat (`Soundscape.cs:96-111`, `CastleScares.cs:103-111`). Nothing dips before a hunt specifically. Silence frequency is keyed to real Dread (N11). |
| 39 | S1 | Fixed | Heartbeat follows felt Dread only (`Soundscape.cs:166-170`). |
| 40 | S1 | Fixed (static) | `voice_cc_hint_*`, `voice_cc_dispatch` and `voice_cc_contain` are on disk. `check_audio_keys` OK. |
| 41 | S3 | Fixed | Fixed 23.5 px pitch (`Interface.cs:243-244`). |
| 42 | S3 | Fixed | `HC_TITLE_FONT` (`TitleScreen.cs:35`). |
| 43 | S3 | Not fixed | Ken Burns and crossfade unreviewed; the ENTER cap is still hidden without focus. No change in range. |
| 44 | — | Partly | Re-scored 5/10 (below). |

## New findings (introduced or exposed by the wave; missed by r10a)

| ID | Sev | Finding | Evidence | Testable fix |
|---|---|---|---|---|
| N1 | **S1** | **Drawbridge winch note prints raw `Case.Goal`.** Pressing E at the winch before Return shows "Break sight and get under cover..." during *any* hunt, including no-tell hunts. It is exact and on demand, from the safe zone. | `Interactable.cs:30`, `Investigation.cs` Goal `case Phase.Hunt` | Castle: use the same held goal as `HudGoal()`. Probe log `HC_PROBE winch` must be identical in and out of hunts. |
| N2 | S2 regression | **A witnessed hide pre-empts an in-progress dive.** `pendingWitnessed` is processed in every flight state except Stagger and NearMiss, including Chase. It converts a dive that was ~0.3 s from the entrance grab into fly → land → crawl 2.2 m at 0.94 m/s → 1.6 s pause → grab, about 4.5-8 s. The old `atHideEntrance` grab in `CountChase` is now unreachable for witnessed hides. If no landing routes, `abort=route` → `BeginLand()` cancels the dive outright. A no-door hide uses its transform as the "door" and still plays `door_rattle` (S3). | `Presence.cs:620-624, 767-783, 1390-1403` | Skip `BeginHideVisit` when `flight==Chase`. In a non-Chase state, keep the route-fail fallback lethal by diving to `ExitPoint` instead of `BeginLand`. |
| N3 | S2 | **Wings and claws leak hunt state by inference.** Between hunts every unobserved movement is a silent `TeleportPerch` after 0.5 s (Perched, Travel and Land all teleport). In hunts he flies unobserved for seconds. So a sustained `wing_beats_loop` (spatial, 24 m) from a Count you cannot see means a hunt. An abrupt stop while unseen means the hunt has ended (Recovery start). Claw steps are 0.05 vs 0.8 keyed on `hunting`. | `Presence.cs:1195,1205,1208`, `CastleRun.cs:77,239`, `Soundscape.cs:205` | Between hunts, about 1 in 3 unobserved re-perches flies a real audible route. In hunts, some legs are silent glides. Claw volume follows gait, not phase. |
| N4 | S2 | **12 m interference is a through-wall proximity radar in every phase.** It uses pure 3D distance, with no line of sight and no randomness, and works through floors. Between hunts, perches are only ≥ 8 m away, so 8-12 m perches suspend recording and zero `Capture` repeatedly. The "BREAK LINE OF SIGHT" text is wrong when there is none. | `Investigator.cs:300-316`, `CountClaudeProfile.cs:111` | Require line of sight, or a flicker probability that falls off with distance (p 1 at 3 m, 0.2 at 12 m). Never zero `Capture` between hunts. |
| N5 | S2 | **Core action gates leak the Hunt phase on demand:** <ul><li>`Record` refuses in Hunt. RulesTests even assert "no-tell hunt still blocks records".</li><li>Ear-trumpet answers NoReply at the voice spot.</li><li>The journal switches JournalReady → NotReady; this is free because the journal pauses.</li><li>`RestorePower` refuses.</li><li>The bell with the sword answers `AnchorNotReady`.</li></ul> | `Investigation.cs:93,140,167`, `Investigator.cs:345`, `Interface.cs:443`, `CastleRun.cs:99-101`, `RulesTests` "no-tell hunt still blocks records" | Castle: let these succeed in a no-tell hunt. A failure must have a cause that also occurs outside hunts. |
| N6 | S2 | **The FeltDread "lie" is a monotone inversion.** While `Lies` (Dread ≥ 70), felt converges to 12, and Dread only falls in the gatehouse or at the fire. A heartbeat or pulse fading to silence over 20-30 s away from those places therefore reads "Dread ≥ 70": max bands, recovery dice from 80, door-held at 90. Noise of ±12 cannot fake a 70 → 12 fall. | `Investigation.cs:184`, `HuntRules.cs` `FeltCalm=12`, `LieThreshold=70` | While Lies, random-walk felt over 0-100 (or keep it on Dread with a random sign per lie window), never converge to a constant. |
| N7 | S2 | **Guaranteed no-hunt phases.** Survey and Containment have no dice, floor, escalation or summon. Recovery below 80 has no dice. Diagnosis therefore buys a risk-free walk to the brazier and bell. | `Investigation.cs:187` (dice only in Disturbance), `:209`, `:303`, `TryEscalate` (Disturbance only), `CastleRun.cs:99` | Low-rate dice in Survey (after the first record), Containment and Recovery at any Dread (e.g. 1/60 per roll). |
| N8 | S2 | **`lastSeen` is never cleared or aged.** It is assigned at `:107,115,603` and never reset, and it now includes heard positions. After the first contact, every hunt opens with `GoLearned` at a possibly minutes-old spot instead of `HearPlayerRoughly`. A player can steer this by pressing the radio or running far away late in a hunt. The hostile return goes there right after the swoop, away from the bridge, until the 3 s re-hear. | `Presence.cs:107,115,415,446,603` | Clear `lastSeen` at Recovery end, or use it only when younger than 60 s and roll it at 50 %. |
| N9 | S3 | **The swoop card is never seen.** `CountBeginHunt` runs synchronously inside `Seal`'s `Set(Return)`. `Say(CastleReturnSwoop)` is then replaced in the same frame by `Say(SealedMessage)`, because `Game.Say` overwrites `Subtitle`. Swoop legs are logged `clear=` but used even when blocked, so he can pass through walls. The swoop also removes about 5-6 s of the hostile return's threat. | `Game.cs:295,356`, `Investigation.cs:333-335`, `Presence.cs:428-446` | Say the swoop card on a 1 s delay or append it. Reject or re-route blocked legs. |
| N10 | S3 | **The Warning is still exactly 9 s,** and the "SIGNAL COLLAPSING" label disappears on the frame the hunt starts (or the false alarm ends). The countdown was removed from the text only. | `HuntRules.cs` Castle `WarningSeconds=9`, `Interface.cs:511` | Draw the warning length 5-12 s, and keep the label for a random 0-3 s into the hunt. |
| N11 | S3 | **Silence and lies are keyed to real Dread.** Silence chance jumps from 1/6 to 0.30-0.45 at real Dread ≥ 60. This is a statistical Dread gauge that bypasses FeltDread. The footsteps-behind lie also aborts mid-sequence the moment a hunt starts, which is a rare exact hunt-start tell. | `CastleScares.cs:73-80,179` | Key silence to FeltDread. Let a running lie finish. |
| N12 | S3 | **The "4,000-seed byte-identical Bellwether logs" claim is not backed by code in the repo.** Bellwether identity is `Trace()` under 3 dice sources (`null`, 0, 0.99), plus spot asserts. The only 4,000-iteration loop is the castle boundary distribution. | `RulesTests.cs:184-206, 338` | Add the fuzz, or stop citing it. |
| N13 | S3 | **The driver is brittle for low boundaries.** <ul><li>Step 5 Blocks if Dread ≥ `RunDreadMin` before the forced dice. With a boundary of 30-35, apparitions (+12 each) can trip it.</li><li>Step 7's queue `[0,.9,.9]` has no interval slot. If Dread was below the boundary at Disturbance entry, `ForceDread` flips `diceHigh`, the 0 is consumed as an interval and the 0.9 as the hunt roll, so there is no hunt and a Blocker fires.</li></ul> | `PlaytestDriver.cs` ~254, ~280, `Investigation.cs:237` | Prefix the step 7 queue with an interval value when `Dread < RunDreadMin`, or seed the boundary in automated runs. |

Null and ordering checks that came out clean:
- **`DiveLastSecond`:** it is subscribed before `Scares` is assigned, but it is only invoked inside hunts (`Game.cs:133-136`).
- **`DuckBed`:** it is bound after `Sound` exists (`Game.cs:123,140`).
- **FeltDread at run start:** it equals Dread, plus noise of at most 12, plus an offset of at most 8, so felt ≤ 20 and the heartbeat is silent. Briefing does not advance it. A restore snaps it to the restored Dread.
- **`RunDreadMin`:** it is diagnostics-only in Runtime: logs and the driver.

Soft-lock checks:
- A hunt, Recovery and the swoop always end. Phase timers are in Investigation, and `Fly` is kinematic with no NavMesh.
- A witnessed or near-miss visit that outlives the hunt is reset by `CountEndHunt`.
- No hard locks were found.

## Bellwether isolation: every hunk that executes when `Map != Castle`

| File:line (6036196) | Hunk | Changes Bellwether behaviour? |
|---|---|---|
| `HuntRules.cs:102-108` | Bellwether preset gains spread 0, low chance 0, Min=Max 38/32, recovery dice 0, `AnnounceRecovery` true, felt 0 | No (all neutral; RulesTests assert) |
| `Investigation.cs:30-41` | ctor: `RunDreadMin=0`, `Current*=38/32`; draws gated on `UsesDread` | No (0 draws, asserted) |
| `Investigation.cs:184-205` | felt update gated on `UsesDread`; FalseWarning → `Diagnosed?Containment`; hunt end uses `CurrentHuntSeconds` (38); Recovery uses `Current` (32) + `TickRecoveryDice` (returns, chance 0); `AnnounceRecovery` true | No (FalseWarning never true in Bellwether) |
| `Investigation.cs:234-262` | `TickDisturbance`: `dreadful` 0 ≥ 0; `diceHigh` flip resets an unused timer; first-hunt/low paths off | No |
| `Investigation.cs:270-272,297,305,314` | `BreakerOn=false` removed from no-tell branches | No (Bellwether never takes them) |
| `Investigation.cs:107-125,356` | checkpoint `RunDreadMin`; restore resets felt/first-hunt/current lengths; `Set` resets `diceTimer` on Recovery, no `firstHuntAt` reset | No |
| `Game.cs:84,118,130-135,140,340,351-357,383-384` | field removal, castle logs, castle subscriptions | No (castle-gated) |
| `Game.cs:310-325` | RadioHint split; Bellwether `else` branch is the old code | No |
| `Game.cs:395-397` | Return speaks `return` unless castle+hostile | No |
| `Game.cs:423-434` | `CastleSession` gates castle prefs | **Yes, intended (#25):** Bellwether sessions no longer compare/write `hc.long-night`, `hc.microphone` and `hc.mic-threshold`. Values were loaded from prefs, so only missing-key creation differs. |
| `Interface.cs:243-244` | Tagline drawn per line at 23.5 px | **Yes, visible:** the Bellwether title tagline's second line moves 1-4 px (#41). |
| `Interface.cs:303` | Settings toggle id null for Bellwether | **Yes, intended:** restores the pre-castle label-derived ids (#24). |
| `Interface.cs:351-358,493` | `HudGoal` returns `c.Goal` for Bellwether | No |
| `Interface.cs:482,511` | danger styling / countdown: Bellwether expressions equal the old ones | No |
| `Investigator.cs:300-316` | `Interfered()`: Bellwether branch is the old condition | No |
| `Presence.cs:104-108,110-116` | `Hear` adds `lastSeen` if `count!=null`; `WitnessHide` adds `else return` | No (Split Grin path identical) |
| `ScareDirector.cs:70,102-103,114` | `!IsCastle&&` gates; point selection falls through to `VisiblePoint`; escalate range inside `if(IsCastle)` | No |
| `Soundscape.cs:215-237` | music/breath/room: Bellwether huntMusic 0.38, breath while hidden in hunt, room unchanged | No (`tellHeld` was castle-only) |
| `TitleScreen.cs:35` | `HC_TITLE_FONT` log | Log only |
| `CaseBook.cs:163-218` | castle book strings only | No |

**What the rules evidence does not cover** (it covers only Core `Investigation`/`HuntRules`/`CaseBook`, and with 3 dice sources, not 4,000 seeds):
- **UI:** the title (tagline pitch change is real), settings focus ids, HUD, dialogue styling.
- **Audio:** `Soundscape.Update` mix, `Speak` key mapping.
- **Presence:** `Hear`, `WitnessHide`.
- **ScareDirector:** Trigger, point selection.
- **Game:** `RadioHint`, `SavePreferences` (behaviour changed), `PhaseChanged`.
- **Investigator:** interference.
- **The title screen as a whole.**

By reading, I found no unintended Bellwether gameplay change. Two visible or intended deltas remain: the tagline pitch and prefs.

## Score

**5 / 10.** It is up one point because the ambient tell flood is mostly gone, the Dread floor is broken, lengths are randomised, the witnessed rule is enforced and the lies exist.

It is capped by:
- an exact on-demand lamp (N1) and a 5/6 radio detector (#8);
- five gated actions (N5);
- two deterministic inference channels (N3, N4);
- guaranteed safety in an unwitnessed hide below 80, Survey, Containment and Recovery below 80 (#16, #17, N7);
- a readable inverted Dread gauge (N6);
- zero runtime proof of any of it.

## Three highest-leverage remaining changes

1. **Make every player query phase-blind** (#8, N1, N5). The radio's calm and hunt answers must share one distribution. The winch shows the held goal. Record, ear-trumpet, journal, brazier and bell behave the same in no-tell hunts.
2. **Break the remaining guarantees** (#16, #17, N7, #10). Near-miss open should scale from Dread 0, for example 10 % at 0 rising to 35 % at 100. A witnessed hide during Chase keeps the fast entrance grab (N2). Low-rate dice should run in Survey, Containment and Recovery at any Dread.
3. **Remove the inference channels** (N3, N4, N6, N8):
   - audible unobserved re-perches between hunts and silent glides in hunts;
   - line-of-sight or probabilistic interference;
   - a random-walk lie instead of a constant calm;
   - `lastSeen` ages out.

## Proof checklist for the windowed run (ordered; exact keys and conditions)

1. `HC_RULES Castle dread=0 runDreadMin=<x>`, with 30 ≤ x < 50 and `felt=` ≤ 12. Across 10 launches the x values differ.
2. `HC_TITLE_FONT cinzel=True`. Zero `HC_MISSING_ART` lines in the whole run, including radio presses before diagnosis.
3. No-tell channel proof (driver `fear`, 20 forced no-tell hunts): each `HC_HUNT_START phase=Hunt noTell=True ... breaker=True` is followed by `HC_PLAY_CHANNELS ... noTell=True changedFrames=0 heartbeatMaxDelta<0.010 voiceLines=0 smellLines=0 rimLines=0`.
   - `wingMaxDelta` must be 0 whenever the Count is not in view.
   - **Predicted fail on `voiceLines`** (`cc_hunt_hungry` at about 1-2 s, #5).
4. Phase-blind queries (needs a NEW `HC_PROBE <query> phase=<p> result=<r>` line per query; queries as in the biggest gap). Over 40 calm and 40 no-tell samples per query, the result distributions are equal. Today the predicted results are:
   - radio: 5/6 differ;
   - winch, journal, ear-trumpet, record: 100 % differ.
5. `HC_PLAY_LOWDREAD hunt after=<s> dread=<d> runDreadMin=<m>`, with d < m and s ≤ 600 (mode `lowdread`, after the first hunt).
6. Across one run, `HC_HUNT_START ... huntSeconds=` values span at least 30-45 and `HC_RECOVERY seconds=` values span at least 15-35. `HC_PLAY_RECOVERY_CUT to=Warning|Hunt` ≥ 1 in `siege`.
7. Watched hide, torch off, Count perched or travelling, in this order:
   1. `HC_COUNT_WITNESSED hide at` (DiagnosticState not Chase)
   2. `HC_COUNT_WITNESSED begin`
   3. `HC_COUNT_WITNESSED hold ... opens=True`
   4. `HC_COUNT_GRAB ... hideEntrance=True witnessed=True torch=False`
   5. Then Phase Dead, or `grab blocked reason=driver invulnerability`.

   No `HC_COUNT_WITNESSED abort=route` in 10 tries.
8. Watched hide **during a dive**: `HC_COUNT_DIVE`, then `HC_COUNT_WITNESSED hide at`, then `HC_COUNT_GRAB hideEntrance=True` within 1.5 s. **Predicted fail** (N2).
9. Near-miss open (`siege`), in this order:
   1. `HC_COUNT_NEARMISS roll=... go=True`
   2. `HC_COUNT_NEARMISS begin ... open=True eligible=True dread>=80`
   3. `HC_COUNT_NEARMISS open=True`
   4. `HC_COUNT_GRAB ... hideEntrance=True witnessed=False`

   After fix 2: also with dread < 80.
10. Dive torch failure:
    - every `HC_TORCH_FAIL dive last second` is preceded in the same frame by `HC_COUNT_DIVE_LAST eta<=1.00`;
    - no `HC_TORCH_FAIL` line appears without one;
    - when a grab follows, `HC_COUNT_GRAB` comes ≤ 1.3 s after `HC_COUNT_DIVE_LAST`.
11. Hostile return:
    1. `HC_HUNT_START phase=Return`
    2. `HC_COUNT_RETURN_SWOOP begin ... leg1clear=True leg2clear=True`
    3. `HC_COUNT_RETURN_SWOOP end` within 8 s

    The subtitle shows the swoop card for ≥ 2 s (needs `HC_UI_STATE subtitle=`). **Predicted fail** (N9).
12. Natural mode (no forcing, 2+ evidence, ≥ 60 s Disturbance): `HC_ESCALATE near<5.5 escalated=True` ≥ 1; `HC_THROW` ≥ 1 and `HC_THROW poltergeist=` ≥ 1.
13. count(`HC_RARE_LINE`) + count(`HC_LIE kind=phantom_listen`) ≤ 1 per run. Every `HC_COUNT_HEARD ... hidden=True` has `offset` ≥ 3.0.
14. `HC_DUCK seconds=` lines appear, and each pairs with an `HC_SCARE_CASTLE silence` line. `HC_COUNT_PERCH flight observed=True` ≥ 1 in natural mode.
15. Bellwether:
    - `RulesTests` PASS;
    - a Bellwether run logs no `HC_RULES`, `HC_DUCK`, `HC_COUNT_*` or `HC_RECOVERY` lines;
    - its `HC_WARNING`-free cycle is 95/9/38/32 s by timestamps;
    - the title capture matches `title_mock_bellwether.png` within 2 px at 2560×1440.
