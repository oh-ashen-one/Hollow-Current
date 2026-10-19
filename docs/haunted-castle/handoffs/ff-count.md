# Handoff: ff-count (wave FF): r14 critic items 2 and 3, plus the Count secondaries

Branch `hc/ff-count`, worktree `.wt/ff-count`. Base: `haunted-castle` `3e84389`, with `hc/fe3-grab` `82e9e2a` merged first (the merge was clean). Model: Opus 5.5. Date: 2026-10-19.

Nothing was built, baked or launched. Every runtime claim below is a design claim, and the integrator's runs must prove it. The evidence is static:
- compiles against the player and editor assemblies;
- the pure rules and their models (`Tests/RulesTests.cs` `WaveFFTests`);
- the Bellwether fuzz;
- the lints;
- the r14 run logs and frames (`rounds/r14/critic/`).

These changes answer the r14 blind critic (`rounds/r14/CRITIC.md`): top change 2 (grab variety and hide-grab visibility), top change 3 (the apparition clock and the lantern tell), and the secondaries on the post-win kill, the cold first hunt and the voice repertoire. They all fall under owner feedback item 12 (fear and the kill).

## What changed

| # | r14 finding | Change | Files |
|---|---|---|---|
| 1 | The crawl (mode 9) was 10 of 17 grabs. The side snatch committed 0 times: both plans were dropped "at the start point, came in low". | **Crawl cap.** `PlanVariant` plans the crawl only while its deck share, counting this commit, stays at or under `CrawlMaxShare` 0.30. The exception is a dive where nothing else is feasible (the hall gallery: 2.92 m ceiling, flanks shut). The n/a reason is logged as `crawl share X over 0.30`.<br>**Side re-frame.** A planned side that reaches its start point after she turned is re-framed from where he is (`AdaptSideFrame`, pure `GrabMixRules.AdaptFrame`). The frame is turned so that his bearing in it is the clip's start bearing (about 52°), up to `SideAdaptMaxTurn` 60° from her view, and only if that frame's flank lines are open. Log: `HC_GRAB_FLANK stage=adapt`. If the re-frame fails, the plan is unlocked and re-tracks her view; the old code dropped it to the crawl. Only the timeout (`not committed in time`) still drops a side.<br>**Side owed.** A dropped side is owed: the next dive of the same hunt plans a side if one is feasible (`forced=True reason=side owed`). A side commit clears the debt. | `Runtime/Presence.cs`, `Core/GrabPlanRules.cs` (`GrabMixRules`), `Runtime/Castle/CountClaudeProfile.cs` |
| 1 | The crawl replays identical camera-relative frames. | Each crawl, open floor and hide, draws a variant: a clip start of 0-0.24 s, a speed of 0.86-1.18 (the presentation clock and the commit length follow it), and a grab-camera aim offset of 3.5-8° yaw (its side alternates per crawl) and ±3° pitch. Log: `HC_GRAB_CRAWL_VARIANT`.<br>No mirrored clip: a negatively scaled skinned rig with the per-bone arm aiming (`ReachArms`) is untested here, and the Animator mirror needs a controller change and a bake. | `Presence.cs` |
| 2 | Drops start 0.22-0.50 s into their clip. Planned drops fell to the crawl with 12 m of headroom. | A drop commits only where its fall clears the ceiling at his own bearing's stand-off. Otherwise he keeps flying to the open ring point (`HC_GRAB_DROP held=True reason=spot_ceiling`), which removes the `from=door ... 8->9` crawls.<br>He must also have his skull at least at the clip's skull at `DropStartMaxT` 0.1 s over her floor. Lower, he climbs first: the rise hold (`HC_GRAB_DROP rise=begin/end`), at 4.5 m/s, at most 1.2 s, never into a ceiling (a 0.25 m sphere cast over his skull).<br>`MatchClipStart` then starts the clip by about 0.1 s, so the skull at t=0.01 is about 2.6 m over her eye on the measured clip (4.38 m at t=0).<br>A rise that times out commits from where it got to (logged `result=timeout`, start possibly > 0.1). A rise blocked under the clip's landed skull ends the dive as a readable miss, never the crawl. | `Presence.cs`, `GrabPlanRules.cs` |
| 3 | Hide grabs (`path=nearmiss`, `hideopen`) average about 5/255 luma. | When he tears the hide open, her lantern swings up (`Investigator.LanternRaise`). From the commit frame (no pop between t=0.01 and t=0.16) the hand lantern's beam is lit even if she had put it out. A warm point glow (`HideLanternGlow` 1.6, `HideLanternRange` 5.5 m, colour (1, 0.74, 0.46), no shadows) sits at the lantern in her left hand and spills over the hide's opening and him. Both go off at the grab's end.<br>This needs the kit (no kit, no lantern). It is not a brightness boost: the beam and the glow are the in-world lantern. Log: `HC_GRAB_LIGHT ... hideLantern=raised glow=1.60 range=5.5`.<br>New `tools/grab_luma.py` runs the critic's test (mean ≥ 18, ≥ 5 % of the box over 60) on a run's t=0.16 hide frames. It uses the new `countRect=`. On the r14 frames it reproduces the critic: siegeheld 5.0, siege 5.0, fear 9.6, all FAIL; natural1's torch-lit dives 37.2 / 27.0 PASS. | `Runtime/Investigator.cs`, `Presence.cs`, `tools/grab_luma.py` |
| 4 | `HC_COUNT_GRABCAM` has no skull position, and the driver reports 0.00 m while the root jumped 0.45-0.97 m. | Every sample adds `skull=(x,y,z)`, `rootReseat=` (his root's distance from the commit `begin` root), `seatOffset=` (the seat offset still applied), `frameCut=` and `countRect=(x0,y0,x1,y1)` at the 2560 × 1440 scale of `skull_px`. `begin` adds `root=`. `grabcam.txt` carries these fields.<br>`HC_PLAY_GRAB_CONTINUITY` adds `root_jump=` (the frame before the commit to the first grab frame), `root_slide=` (the furthest horizontal root travel from frame 0 in the measured 1.2 s), `frame_cut=` and `root_ok=` (slide ≤ 0.5). `HC_GRAB_SUMMARY` adds `rootSlideOk=a/b`. `pass=` is unchanged. | `Presence.cs`, `Runtime/Castle/PlaytestDriver.cs` |
| 5 | Six of six threats with a prior apparition started 26-57 s after it. | Each visible apparition after the first hunt draws its lead (`HC_APPARITION_LEAD`):<br>- **follow** (0.35): a dice-style threat 12-19.5 s after it, always past the 11.5 s gap;<br>- **rest** (0.30): the dice are held 40-90 s after it;<br>- **open**: unchanged.<br>A follow is cancelled by grace, a dice hold (the long quiet stretch), a threat or Containment, and it blocks an escalation.<br>The first hunt (2 of the r14 6) draws once, with the run plan:<br>- follow (0.4): the director asks for an apparition when the deadline is its lead away (`HC_SCARE_WANTED`), and the deadline is tied to it inside the band;<br>- rest (0.4): apparitions are held 62 s, not 11.5 s, before the deadline (`PlannedThreatInForApparition`);<br>- else open. | `Core/Investigation.cs`, `Core/HuntRules.cs`, `Runtime/ScareDirector.cs` |
| 6 | `HC_TORCH_FAIL source=dive` came on 7 of 13 dives. | The dive chance drops from 0.5 to 0.2. Each hunt plans 0 / 1 / 2 random failures at 20 / 45 / 35 %, at least 6 s apart, never on a dive (`HC_TORCH_PLAN hunt n= at=[..]`).<br>Expected per natural run: about 0.8 dive failures and about 3.4 hunt failures, so roughly a fifth of failures come right before a contact. The driver's `HC_PLAY_STATS` adds `torchFails= torchDive= torchHunt= dives= torchBeforeContact15=`. | `Runtime/Game.cs`, `PlaytestDriver.cs` |
| 7 | `HC_RETURN_KILL would_die=true phase=Won` after `bridge_crossed`. | The driver records the kill at the blocked death (phase Return, hunt active) and stops walking for the 1.3 s presentation. The invulnerable driver used to cross the bridge and win inside the grab; that also fixes the Return-grab framing the critic could not judge. The line adds `phaseAfterPresentation=`. A win inside the presentation is a blocker. | `PlaytestDriver.cs` |
| 8 | The arrival letter (read at 4 s) opened the cold gate, so a new player was hunted at 105 s with no evidence. | With `ColdGateNeedsLedger`, the gate opens on the first ledger page or evidence record (`FirstGateDocAt`, `FirstGateDocKind` = page or record), else at the 150 s cap. `HC_HUNT_TELL ... firstDocAt=` is now that time, followed by `firstDocKind= letterAt=`.<br>The cold driver reads only the letter and never records evidence, so it should now log `gate=cap firstDocAt=-1`, with the first hunt about 155-164 s in (still inside its 180 s). | `Investigation.cs`, `HuntRules.cs`, `Game.cs` |
| 9 | `cc_hunt_found` was 10 of 27 lines. | There are 4 new lines, rendered with `tools/generate_castle_voice.py --voice bm_george` (Kokoro-82M, Apache-2.0) at −24.2 LUFS, with peaks of −9.7 to −10.9 dBFS:<br>- `cc_hunt_found_b` "Ah. Found you.";<br>- `cc_hunt_found_c` "Hello again, little lamp.";<br>- `cc_hunt_smell_b` "You breathe so loudly in there.";<br>- `cc_hunt_run_b` "Yes. Run. I like it when they run."<br>`CountSpeak` plays pools: found × 3, smell × 2, run × 2. It picks the variant played least this run (`GrabMixRules.PickVariant`: ties go to the dice, never the variant just played).<br>Every variant is still spoken from his head only. The fd-voice lint passes, and `check_audio_keys` resolves the new literals. They are logged in `ASSET_LOG.md`, `AUDIO.md` and `voice-lines.json`. | `Presence.cs`, `Resources/CastleAudio/voice_cc_hunt_*_b/c.wav(.meta)`, `tools/voice-lines.json`, docs |

## Verification (static, final tree)

- `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py`: **PASS**, 1455 assertions (fe3-grab: 1407). The runtime player, runtime editor and editor assemblies compile; only the known CS0649 / CS0162 / CS0219 warnings appear. The rules run takes 15.8 s against the 30 s limit.
- `check_castle_scares.py`: PASS (including the fd-voice head-only lint).
- `check_audio_keys.py`: OK (69 uses).
- `check_audio_quality.py`: OK.
- `bellwether_fuzz.py`: **identical**, sha256 `ee3b74dc…fd68f2f`.
- No scene, prefab or animation asset changes.

`WaveFFTests` covers these:
- **Follow, rest and first-lead mechanics:**
  - a follow starts its threat 12-19.5 s after the apparition, never inside the gap;
  - a rest holds certain dice for at least 40 s;
  - there is no draw before the first hunt;
  - a follow that comes due in the grace is cancelled;
  - the first-hunt rest holds apparitions 62 s before the deadline;
  - the first-hunt follow asks for an apparition and its hunt follows it by 12-19.5 s inside the Mid band.
- **The critic's clock test as a model** (2,000 consecutive planned critic-route runs, 3-run sets):
  - 25-60 s: 0.288 pooled;
  - 10-20 s: 0.337;
  - ≤ 10 s: 0.004;
  - **sets passing with ≥ 7 leads: 0.919**;
  - the low-Dread regime of the r14 runs (low rolls only): 0.918.
  - Before this change the same model (3,000 runs) gave 0.479 / 0.166 / 0.007 and **0.521**.
- **Unchanged r13 bands (planned model):**
  - share mean 0.201 (p10 0.158);
  - every run has a gap of at least 120 s;
  - first-hunt 3-run spread ≥ 45 s: 1.000;
  - 3-run pooled apparition CV ≥ 0.5 in 0.952 of sets;
  - ≥ 4 apparitions in 480 s in 0.999 of runs;
  - hunts ≤ 10 s after an apparition: 0.000;
  - runs under share 0.15: 6.0 % (Late 3.9 %).
  - The siege model is unchanged: hidden survival 0.726.
- **Cold gate:**
  - the letter alone holds to the 150 s cap (`gate=cap`);
  - a ledger page at 20 s gives the Early band with `firstDocAt` 20;
  - a record at 30 s opens it (`record`);
  - the FD cold tests are updated to this rule.
- **Grab mix:**
  - the crawl cap, the drop start skull and the rise decision;
  - the side re-frame is mirror-symmetric and passes the commit gate;
  - the crawl variant varies.
  - A 5-run model (natural × 3 + cold + huntcam, about 15 dives, r14 feasibility rates) gives a crawl share mean of 0.332. **0.834 of sets** have crawl ≤ 40 % and at least 1 side commit. In the rest, the crawl is over 40 % in 0.137 and there is no side in 0.046.
- **Voice:** over natural × 3 + siege_held at the r14 call counts, no clip exceeds 25 % (worst 0.185).
- **Isolation of older mechanics:** the tests that isolate older mechanics now run without the lead draws: `NoGap` includes `NoLead`, plus the governor reproduction, the 20 s shadow test and the first-hunt gap test.

## Integrator recipe

1. Merge `hc/ff-count` (it already contains `hc/fe3-grab` `82e9e2a`). **Re-bake both maps:** Core, Presence and ScareDirector changed. Build as LOOP.md says. Unity imports the 4 new wavs (guids in the committed `.meta` files).
2. Delete `hc.castle.grabdeck` once (a fresh-player deck), then keep PlayerPrefs between runs. Restore the owner's values afterwards: `hc.castle.runs` `L,E,L` / `E,M,B`, grab deck deleted.
3. Launch every run windowed through the slot:
   `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-ff -- open -g -W -n -a "<app>" --args -logFile <dir>/player.log -hc-playtest <dir> <mode>`
4. Runs: natural × 3 (in a row), cold × 2, huntcam, fear, siege_held × 2 (`-hc-hold-dread 60 ... siege`).

**Bands to check:**
- **Grab mix (natural × 3 + cold + huntcam):**
  - `HC_GRAB_COMMIT mode=9` ≤ 40 % of commits;
  - `mode=10|11` ≥ 1;
  - no commit with `planned=drop mode=9` where `ceilingMax ≥ 4`;
  - every `HC_GRAB_VARIANT dropped=side-*` is followed in the same hunt by a side `HC_GRAB_COMMIT`. Look for `forced=True reason=side owed`.
  - Report the `HC_GRAB_FLANK stage=adapt` count, the `HC_GRAB_CRAWL_VARIANT` lines and the `crawl share ... over 0.30` n/a reasons.
- **Drops:** every `HC_GRAB_COMMIT mode=8` has `clip_start_t ≤ 0.10`, and its t=0.01 `HC_COUNT_GRABCAM` has `headAboveEye ≥ 1.0`. `HC_GRAB_DROP rise=end ... result=commit` should be the norm; report any `timeout` and any `held=True reason=spot_ceiling`.
- **Hide grabs (siege_held × 2, fear):** `HC_GRAB_LIGHT ... hideLantern=raised` on every hide grab, then `python3 tools/grab_luma.py <run dirs>`: PASS on every t=0.16 nearmiss / hideopen frame (mean ≥ 18, ≥ 5 % of `countRect` over 60). Look at one frame: the beam and the warm glow should read as her lantern, with no flat lift.
- **Pop logging:** the `HC_COUNT_GRABCAM` samples carry `skull= rootReseat= seatOffset= frameCut= countRect=`. `HC_PLAY_GRAB_CONTINUITY` carries `root_jump= root_slide= frame_cut=`, and `HC_GRAB_SUMMARY` carries `rootSlideOk=`. Report any grab with `root_slide > 0.5`.
- **Clock (natural × 3):** `HC_PLAY_STATS threatLeads=[..] lead25_60=a/n lead10_20=b/n lead10=c/n`. Pooled: a ≤ 50 % of n, b ≥ 1, c ≤ 35 %. `HC_APPARITION_LEAD` lines show each draw (`first=`, `kind=follow|rest|open|none|first_follow`, `follow=fired|cancelled`). If fewer than 7 leads with a prior apparition arise (the model expects at least 7 in 61 % of sets), add a natural run rather than force dice.
- **Kept (r13):**
  - share 15-30 %;
  - longest gap ≥ 120 s;
  - `pctWithinScare10s` ≤ 35;
  - pooled apparition CV ≥ 0.5;
  - first-hunt spread ≥ 45 s;
  - hidden survival ≤ 85 %;
  - ≥ 4 apparitions per 480 s;
  - `diveUnresolved=0`.
- **Torch:** pooled `torchDive/dives` ≤ 30 %, or `torchBeforeContact15` < 40 % of `torchFails`.
- **Return:** there is no `HC_RETURN_KILL would_die=true phase=Won`. Expect `phase=Return huntActive=True phaseAfterPresentation=Return`.
- **Cold × 2:** `HC_HUNT_TELL ... first=True cold=True gate=cap firstDocAt=-1 firstDocKind=none letterAt=4`, with the first hunt at about 155-164 s, told, and `HC_PLAY_COLD pass=True`. If a cold run ever reads a ledger page, expect `gate=page firstDocAt=<ledger time>`.
- **Voice (natural × 3 + siege_held):** no `HC_COUNT_VOICE id=` value above 25 % of the lines. Run `python3 tools/check_voice_logs.py --require-summary <dirs>`: `from_other=0`.

## Risks

- **Unproven in the engine:** the rise hold, the spot gate, the side re-frame, the crawl speed under the presentation clock, the lantern raise and the director's wanted apparition. Watch these:
  - the rise hold should not lift him through a low arch (the sphere cast checks only his skull's column);
  - a re-framed side turns the grab camera by up to 60° (FirstFrameMargin keeps his skull in frame);
  - a raised lantern inside a narrow hide may over-light the near door frame. Lower `HideLanternGlow` if the frame mean goes far above 18.
- **Apparition CV:** the follow makes the clock a little more regular. In planned runs (the runtime), 3-run sets with CV ≥ 0.5 go from 0.968 to 0.952. In the older plan-less model they go from 0.92 to 0.89-0.91, and that bar (`WaveLTests`) is now 0.88 (commented). r14 measured 0.50, right at the threshold.
- **Grab mix by chance:** about 1 dive in 4 has no alternative to the crawl (the gallery). A 15-dive set is still over 40 % about 1 time in 7, and about 1 in 20 sets has no side, in the model. Side feasibility in the real castle is the unknown: r14 had 2 of 14 plans.
- **Fewer hunts after rests:** the rest holds lower share slightly. Runs under share 0.15 go from 4.5 % (FE model) to 6.0 %, Late 3.9 %.
- **Voice:** `cc_hunt_found_c` has a 0.18 s tail (gate 0.3 s). The new lines are judged by metrics only; owner listen pending, as for every Kokoro line. `cc_hunt_run_b` is 6.7 s with its hall tail, so it holds a pooled voice that long.
- **No mirrored crawl:** see item 1.
