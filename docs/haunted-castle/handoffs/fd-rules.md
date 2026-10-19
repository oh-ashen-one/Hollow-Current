# Handoff: fd-rules (r13 critic: hide-open predictability, cold first-hunt tell, perch sightings and apparitions, door swing proof)

Branch `hc/fd-rules`, worktree `.wt/fd-rules`, base `haunted-castle` `0c2796e`. Model: Opus 5.5. Date 2026-10-18.

Nothing here was built or run in Unity. Builders have no `Library/` and do not bake. All evidence is static: the C# compile, the pure rules tests, the Bellwether fuzz, the scene lints and a static swing-arc model. The integrator's runs below are the proof.

## What changed and why

| # | Critic finding | Change | Files |
|---|---|---|---|
| 1 | The near-miss open is a bag of three: `bag=1/3 openAt=1 open=True`, then two guaranteed rattles. | The bag is replaced by `NearMissDice`, a pure per-visit roll:<br>- base chance 0.24 per visit;<br>- 0.45 on the visit right after an open, so an open never buys a safe window and back-to-back opens happen;<br>- a soft pity floor only after 4 closed visits (+0.25 per further closed visit).<br>It applies at the same place as the bag did (Dread ≥ `BagDread` 50, at the hide he keeps finding her in). It resets when he switches hides. Witnessed hides (1.00) and the learned-hide attempt chance 1.00 are untouched. Long-run open rate is 0.37 (0.32 over a 6-visit siege from a fresh start), and the longest closed run is 7. | `Core/NearMissDice.cs` (new, with `.meta`), `Runtime/Presence.cs` (`BeginHideVisit`), `CountClaudeProfile.cs` (comment only; `BagSize` is no longer read) |
| 2 | The cold player's first hunt was no-tell at 87 s, in Survey, with 0 evidence. | `RunHistory.Next` with no history sets `RunPlan.Cold`, and the band stays Early. For a cold run's first hunt:<br>- (a) no dice roll, timed hunt or first-hunt deadline starts a threat until a document is read (`Investigation.Read`: the letter or a ledger page) or until `HuntRules.ColdFirstHuntCap` = 150 s of case time, whichever comes first;<br>- (b) it is always a told Warning of 5-14 s, with no no-tell or false-alarm roll.<br>Long Night (no Warning phase) and Bellwether: cap 0, off. | `Core/Investigation.cs`, `Core/HuntRules.cs`, `Core/ScareRhythm.cs` (`RunPlan.Cold`), `Runtime/Game.cs` (logs) |
| 3a | `HC_COUNT_PERCH flight observed=True` was 0 in every run. | Showcase relocation, once per run. From 20 s of case time, in Survey, Disturbance and Recovery, while he is perched and she is not hidden or in a modal, every 0.25 s the code tests:<br>- the departure: `ObservedByPlayer`;<br>- every arrival perch that keeps the distance rules: head or chest point inside viewport 0.08-0.92, z under 40 m, clear line of sight.<br>He flies at the first chance (`observed=True showcase=True view=departure|arrival`). If no chance comes within 90 s of perched calm time, he flies anyway (`showcase=timeout`) and the wait re-arms until one observed flight has happened. On landing it logs `HC_COUNT_PERCH showcase landed seen=`. | `Runtime/Presence.cs` (`ShowcaseTick`, `VisibleArrivalPerch`, `PointInView`), `CountClaudeProfile.cs` (`ShowcaseAfterSeconds/WaitSeconds/CheckSeconds`) |
| 3b | Visible apparitions were only 2-3 per 8.5 minutes. | The castle clock gets an apparition floor:<br>- the n-th apparition is due by n × 90 s of rhythm age; a longer drawn gap is cut to that deadline, or to a 15-30 s catch-up if a block already pushed past it;<br>- bursts are 30-50 s (were 40-55); lulls stay 150-280 s;<br>- the post-hunt grace is open to apparitions except its last 20 s (the grace disarms dice and escalation, so an apparition there cannot start a hunt);<br>- Recovery stays blocked once its grace is out.<br>No `ScareDirector` change was needed. The `RulesTests` session model now fires apparitions in Recovery too, as the director does. | `Core/ScareRhythm.cs`, `Tests/RulesTests.cs` |
| 4 | Door swing was unproven. `HC_DOOR_STATE` logged only under verify. The capture set `Open=true`, so the swing was chosen from the player's position, not the camera's. | See the next row for the `Door.cs` changes.<br>`CaptureMode` doors now call `OpenFrom(eye)`, and its `doors` log line gains `viewerSide= leafSide= swing=`.<br>New driver mode `doorproof`, described below.<br>`tools/check_castle_doors.py --swing` adds a static swing-arc estimate. It is informational and never fails. | `Runtime/Door.cs`, `Runtime/Castle/CaptureMode.cs`, `Runtime/Castle/PlaytestDriver.cs`, `tools/check_castle_doors.py` |
| 4 (`Door.cs`) | Root cause found statically: castle hinges sit on the jamb corner. The clearance probe's near-hinge corner meets the jamb at about 55-60° on one side (`--swing`: 55 on one side, 155 on the other, for all four proof leaves). The old rule needed 85° away, otherwise it took the widest arc, so a viewer on the 55° side got a leaf swinging toward them. | Castle rule: open away when the away arc is ≥ 85°, or when it is ≥ 45° and leaves at least 0.62 m of the opening free (leaf width × (1 − cos arc)). A 1.5 m leaf at 55° leaves 0.64 m for a 0.54 m player. Otherwise the widest arc, as before. Bellwether keeps the old rule.<br>New: `OpenFrom(viewer, who)`, `SideOf`, `LeafSideNow`, `LastSwingAway`, `LastOpener`.<br>`HC_DOOR_STATE name= side= swing=away\|toward angle= by=player\|auto\|capture positive= negative=` now logs on every castle opening, as well as under verify. | `Runtime/Door.cs` |
| 5 | The natural driver teleported during a grab. | In `Natural()`, before each 20 s teleport: `while(game.Creature&&game.Creature.GrabCamActive)yield return null;`. It uses the same property name as fd-grab, so a merge with fd-grab's guard is trivial; keep one copy. | `Runtime/Castle/PlaytestDriver.cs` |

Contract notes are in `FEAR_PLAN.md`, under "Wave FD additions".

### `doorproof` mode

The mode covers three doors:
- the Hall great doors (both leaves);
- the Dining hall door;
- the Vestibule hall door.

That is 4 leaves, each from both sides (8 toggles). Before starting, the driver sets the Late first-hunt band (logged `HC_DOORPROOF plan=Late (driver)`) so that the Count does not hunt or open these doors during the ~70 s proof.

For each leaf and side:
1. Teleport 3.2 m out on that side (NavMesh, side-checked).
2. Walk on foot through TestInput to 1.5 m from the door.
3. Look at the leaf and press E through the interaction ray. If the ray misses, it logs `focus_fail`, calls `Toggle`, and the proof counts the toggle as unfocused (FAIL).
4. `Door` logs `HC_DOOR_STATE ... by=player`.
5. At +0.5 s:
   - measure the leaf's real side (its probe centre in the closed hinge frame) against hers;
   - log `HC_DOORPROOF name= side= swing=away|toward angle= chosen= leafSide= playerSide= focus= by= leafRot=`;
   - save the frame `NN_doorproof_<leaf>_<mz|pz>_open05.png`.
6. Close the door with E (or `Toggle`, logged as `close=direct`).

The run ends with `HC_DOORPROOF toward=<n> toggles=<m> unfocused=<k> leaves=4 PASS|FAIL`, then the usual `HC_PLAY_RESULT`.

## Verification done (static)

- `INPUT_SYSTEM_REFERENCE=<main clone>/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py`: **PASS**, 1249 assertions (1209 at base). The runtime player, runtime editor and editor assemblies compile. Only the known CS0649 warnings and the existing CS0162 in `CastleScares.cs:216` appear.
- `python3 tools/bellwether_fuzz.py`: **PASS**, identical over 4000 seeds (sha256 `ee3b74dc…fd68f2f`, unchanged).
- `check_castle_scares.py`: PASS. `check_audio_keys.py`: OK. `check_throwables_scene.py`: exit 0 (scene untouched). `check_castle_doors.py`: OK.
- New `WaveFDTests` cover:
  - **Open dice:**
    - long-run rate 0.373, longest closed run 7;
    - after an open, the chance is 0.45 and back-to-back opens happen;
    - no pity in the first 4 closed visits, then 0.49 and 0.74;
    - over 5,000 sets of 3 runs × 6 visits, **86.8 %** of sets show an open within 2 visits of a previous open.
  - **Cold first hunt:**
    - a passive cold player gets a told Warning at exactly 150 s, then the hunt after 5-14 s (`gate=cap`);
    - with the letter read at 4 s, a told Warning in the Early band 60-100 s (`gate=page`);
    - Dread forced to 100 with certain dice still gives no threat before 150 s;
    - a warm run keeps the no-tell roll;
    - Long Night: off.
  - **Apparition floor** (critic route, 2,000 runs, consecutive plans): **≥ 4 apparitions in the first 480 s in 98.8 % / 98.6 % of runs**.
  - **Unchanged L3 bands:**
    - pooled CV ≥ 0.5 in 97.6 % / 97.9 % of 3-run sets (was 91 %);
    - every run has a ≥ 120 s gap;
    - first-hunt 3-run spread ≥ 45 s: 100 %;
    - share mean 0.182 (p10 0.130, p90 0.234);
    - hunts ≤ 10 s after an apparition: 10.5 % / 16.4 % (limit 35 %).
- `python3 tools/check_castle_doors.py --swing` (Wall boxes only, so the real arcs can only be smaller): all four proof leaves are `full` on one side and `partial` (55°, passable) on the other, so the new rule opens away from both sides. Service doors (0.9 m leaves) cannot open passably on their 55° side. Crypt stair, Gatehouse kitchen passage, Library stair and Portrait door therefore still swing toward a player on that side. This is a known exception, and those doors are not part of `doorproof`.

## Integrator: build, then run

Merge, bake and build as usual (any `.cs` change stales the receipts; no scene or asset changes here). Run every launch through the slot, windowed and in the background:

```
~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-fd -- open -g -W -n -a "<app>" --args -logFile <dir>/player.log -hc-playtest <dir> <mode> [extra]
```

### 1. Cold ×2 (`cold`)

Expect `HC_RUN_PLAN band=Early ... cold=True coldCap=150`. The driver reads the letter at about 4 s (`HC_DOC_OPEN` for the letter appears before any hunt).

Pass per run:
- the first `HC_HUNT_START` is preceded by `HC_WARNING false=False` and is followed by `HC_HUNT_TELL kind=warning lead_s=<5..15> ... first=True cold=True gate=page firstDocAt=<4-ish>`;
- the first hunt `t` is ≥ the letter's read time and inside 60-114 s (60-100 Warning + ≤ 14 s);
- no `noTell=True` on the first hunt;
- the existing cold checks still hold (`HC_PLAY_COLD ... pass=True`, `blockers=0`).

(The 150 s cap path is proven in `RulesTests`. A live passive run would need a driver variant that never reads the letter, which is not built.)

### 2. Natural ×3 (`natural`, in a row so the history rotates)

Per run:
- `grep -c "HC_COUNT_PERCH flight observed=True"` ≥ 1, as `showcase=True view=departure|arrival`, ideally followed by `HC_COUNT_PERCH showcase landed ... seen=True`;
- `HC_SCARE visible=` lines with `t` < 480: **≥ 4**;
- `HC_PLAY_STATS`:
  - `pctWithinScare10s` ≤ 35;
  - `huntShare` 0.15-0.30;
  - `longestGap` ≥ 120.

Pooled over the three runs:
- `apparitionCV` ≥ 0.5;
- the first-hunt spread is ≥ 45 s;
- 0 teleports during a grab: no `eyeToSkull` jump over 5 m inside one grab (critic natural1 #2).

`HC_RUN_PLAN ... cold=False` in all three.

### 3. siege_held ×3 (`siege -hc-hold-dread 60`)

Up to 6 hidden hunts each. Every learned-hide visit logs:
- `HC_COUNT_NEARMISS openroll p=<0.24|0.45|0.49|0.74|0.99|1.00> roll= open= streak= afterOpen=`;
- `HC_COUNT_NEARMISS begin ... open= p=`.

No `bag=` line may remain.

Pass:
- across the 3 runs, at least one `open=True` within 2 near-miss visits of a previous `open=True` in the same run (model: 87 % of 3-run sets, so a miss is possible by chance; then run a 4th);
- hidden survival over all hidden hunts stays ≤ 85 %;
- `HC_DOOR_HELD` ≥ 1.

### 4. doorproof ×1 (`doorproof`)

Pass:
- `HC_DOORPROOF toward=0 toggles=8 unfocused=0 leaves=4 PASS`;
- 8 `HC_DOOR_STATE ... swing=away ... by=player` lines;
- the 8 `*_doorproof_*_open05.png` frames show each leaf swinging away from the camera.

If a leaf swings toward the camera, its `HC_DOOR_STATE positive= negative=` shows the real arcs. An away arc under 45°, or one that leaves under 0.62 m free, means the hinge needs to move to the wall's mid-plane. That is the `World.BuildCastle` hinge-to-wall-face loop and needs a re-bake; it is not a rules change.

### 5. Doors capture (`-hc-capture <dir> -hc-capture-art doors -hc-capture-size 1920x1080`)

Pass:
- the `doors hall_doors_open_inside ...` lines read `viewerSide=+z ... swing=away` for both leaves;
- `art_doors_hall_doors_open_inside.png` shows the leaves swung into the courtyard, away from the hall camera.

### 6. Regression

`fear`, `escape` and `kill` (`blockers=0`). Bellwether `-hc-verify` `passed=True` (fuzz already identical).

## Risks and notes

- **Partial-away doors:** the castle doors now open about 55° away on their jamb side instead of about 155° toward the player. The opening stays passable (0.64 m), but it is narrower. If playtesters snag, the real fix is hinge placement at the wall's mid-plane (art/layout), not the rule.
- **Apparitions in the grace:** apparitions can now come 15-50 s after a hunt ends, in Recovery. This is consequence-free (no dice, no escalation in the grace), but a critic may read it as "he never leaves". The dials are `CastleGraceMargin` (raise it to block more of the grace) and `CastleQuotaSeconds` (raise it for fewer apparitions).
- **Showcase timeout flights:** every 90 s of perched calm time without a view, he takes one extra visible-capable flight until the first observed one. Expect 0-3 `showcase=timeout` lines per run.
- **Merge:** shared-file hunks are small:
  - `Game.cs`: two log lines;
  - `CaptureMode.cs`: the doors block;
  - `PlaytestDriver.cs`: dispatch line, `Natural()` guard, new `DoorProof()` block.

  fd-grab also edits the `Natural()` guard. Keep one `GrabCamActive` wait before the teleport.
