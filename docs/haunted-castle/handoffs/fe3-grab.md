# Handoff: fe3-grab (wave FE3): left side snatch starvation, side/spin seat slide

Branch `hc/fe3-grab`, worktree `.wt/fe3-grab`, base `haunted-castle` `952fd5c` (code `6229407`). Model: Opus 5.5. Date 2026-10-19.

Nothing was built or run in Unity. A blind critic owns the engine. Evidence is static only:
- the FE2 logs `.playtest-fe2-r{1,2,3}-*.log` in the main clone;
- the pure rules (new `Core/GrabPlanRules.cs`) and their tests;
- compiles against the player and editor assemblies;
- the Bellwether fuzz.

Every runtime claim below is a design claim. The integrator's runs must prove it.

These are the two open items from fe2-count (HANDOFF top section, items 1 and 2). Both fall under owner feedback item 12 (grab variety and presentation) and the r13 critic's grab tests.

## Root causes (log evidence)

### 1. m10 (left side snatch) 0 of 24 open grabs on the FE2 final build

There were three causes. None of them was a sign error or a mirrored clip.

**a. The deck starved left, then the logs hid why.**
- The deck carried left-side commits across rounds (r1 `natural3` 2×, r2 `natural1`/`natural3` 3× = `deck=[... 10:5 11:1 ...]`).
- `PlanVariant` tried the less-used side first by deck count alone. So through r3 natural1-5 the right flank was always tried first, and left was planned only where the right was shut.
- After the deck reset (r3 natural6 `deck=[8:3 9:1 10:0 11:0 12:0]`), left was tried first but failed. Examples: r3 natural6 bearing 153; r3 natural8 bearings 3 and -43, where he was on her left and the right side was still chosen.
- Only the chosen side was logged, so the left reason never appeared.

**b. The commit gate was asymmetric in practice: view bearing, not the planned frame.**
- r3 `natural9` (`.playtest-fe2-r3-natural9.log:738`) logged: `plan=side-left mode=10 ... bearing=86 frameYaw=-15`, then `locked=side-left point=(12.56, 0.25, 2.99) bearing=86`, then `dropped=side-left reason=at the start point, came in low (1.5 m)`, then `HC_COUNT_GRAB mode=9 ... position=(12.22, 0.39, 3.97)`.
- The only open left frame was turned -15° (away from his side), which puts the start point at 39° of view bearing.
- He was on her right (+86°), flew across her front, and entered the 0.7 m commit radius at about 34-35° of view bearing.
- `PredictGrab` gated *his bearing from her view* at 35-110° and took the side from that bearing's sign, so it refused the left commit and fell to the crawl.
- In the frame the clip is actually placed in (`SideFrameForward(planSide)`), the same point sits at 49°. The test `FE3 natural9` reproduces this: start view bearing about 39, FE2 gate under 35, frame gate 49.

**c. A live side flip kept the old mode.**
- r3 natural9 logged `plan=side-right mode=11`, then `dropped=side-left ... detour 8 legs`.
- The live update flipped `planSide` when the right flank closed, but `plannedMode` stayed 11.

**Torch hand: not a blocker.**
- The hand lantern (`Investigator` localPosition (-0.215, -0.19, 0.45), in her left hand) and every held item are on layer 30. They have no colliders, and `FlightMask` excludes layer 30. Her own capsule is on layer 2, also excluded. So the torch hand never blocks a flank line.
- On screen, at +0.3 s the left snatch's skull was at x 0.27-0.40, y 0.48-0.51 (`HC_COUNT_GRABCAM t=0.30`, r1/r2 m10). The right snatch's was at x 0.62-0.68, which is the mirror image.
- My estimate of the lantern's viewport box is x 0.25-0.38, y 0.67-0.90 from the top, so it does not cover the skull.
- The asymmetric t=0.16 samples (m10 at x 0.18 and 0.10) were the seat slide (item 2), not the hand.
- A guard is added anyway (see changes, 1d).

The clip starts are exactly mirrored (`HC_GRAB_SIDE_MAP sideL_start=(2.88, 1.54, 0.11) sideR_start=(-2.88, 1.54, 0.11)`). The stand-offs are 2.20 and 2.21.

### 2. Side and spin first-frame slide

- fe-count's reseat (gap > 0.45 m is kept in the root) exempted modes 10/11/12.
- FE2 seat gaps:
  - m11: 0.83, 1.01, 0.55, 0.88;
  - m10: 1.10, 0.94, 2.91;
  - m12: 0.60, 0.61, 0.60.
- These slid over 0.35-0.9 s (`HC_GRAB_SEAT ... seconds=0.41-0.90 note=none`).
- r2 natural3's 2.91 m slide took the skull off screen at t 0.16-0.46.

## Changes

| # | Change | Files |
|---|---|---|
| 1a | **GrabDeck.**<br>- Commits by mode, plus a planned-but-not-committed streak per mode.<br>- `StarveAfter`=2 uncommitted plans rebalance the mode: a side drops below its pair so it is tried first; any other mode loses one count. Logged as `HC_GRAB_DECK rebalance`.<br>- The saved deck is normalised: a side leads its pair by at most 1, and no scored group sits more than 2 counts over its target share. A saved deck therefore cannot shut a mode out.<br>- The format is `c0..c12;s0..s12`. The old format still parses.<br>- `DeckText` adds `streak 10: 11: 12:`. | `Core/GrabPlanRules.cs` (new), `Runtime/Presence.cs` |
| 1a | **Side choice.**<br>- Both sides are evaluated on every plan. For each frame yaw: the flank is open, then the start point is reachable.<br>- The first side is the one committed less often. A tie goes to his side; an exact tie goes by deck parity (`SideGrabRules.FirstSide/Choose`).<br>- Every plan logs `HC_GRAB_FLANK side=L\|R stage=plan feasible= [yaw=] reason= bearing= chosen= first=`. A live flank loss logs `stage=live`; a commit-gate refusal logs `stage=commit`. | `Presence.cs` |
| 1b | **Commit gate in the planned frame, on the planned side** (`SideGrabRules.CommitOk`): the frame bearing is 35-110° on `planSide`'s side, plus the flank check. A refusal says why (`side gate: frame bearing ...`). | `Presence.cs` (`PredictGrab`) |
| 1c | A live side flip now updates `plannedMode`. `SideClearFrame` uses each side's own stand-off and `Blocked` (his and her colliders are ignored). | `Presence.cs` |
| 1d | **Hand guard.**<br>- At a side commit, the +0.3 s skull is predicted from the clip's skull track and the side camera's aim.<br>- If it falls inside a held item's viewport box or outside the central 60 %, the side camera aim turns by up to 15° to move it beside the item, toward the centre. The turn fades out by 1.2 s.<br>- Log: `HC_GRAB_SIDEFRAME mode= t=0.30 x= y= central= covered= bias=`. Expected: `covered=none bias=0.0`. | `Presence.cs` (`SideHandFrame`) |
| 2 | **Seat rule for 10/11/12.**<br>- `PredictVariantSkull` gives the clip's first skull for the placement `CommitGrab` will make: the side frame, or the spin's outward stand-off and matched fall frame. It uses new per-clip local skull tracks sampled in `PrimeClipCurves`.<br>- If that skull is more than `SeatMaxGap` (0.45 m) from his skull now, the commit is held while he flies onto it, at up to `max(DiveSpeed, 4)` m/s for at most `VariantSeatHoldSeconds` (0.3 s), and only along a clear path. Logs: `HC_GRAB_SEAT_HOLD begin/end/blocked`.<br>- After the hold, a measured frame-0 gap still over 0.45 is a **hard cut**: no seat offset, and the view cuts onto his skull. Log: `HC_GRAB_CUT mode= gap= held= predicted=`. `HC_GRAB_FIRSTFRAME` shows `cut=True seatCut=True`.<br>- No side or spin grab slides more than 0.45 m. | `Presence.cs`, `CountClaudeProfile.cs` (`VariantSeatHoldSeconds`, `VariantSeatHoldSpeed`) |
| 2 | **Driver.**<br>- `HC_PLAY_GRAB_CONTINUITY` accepts a commit gap over 0.5 only with a logged cut (`Presence.GrabSeatCut`).<br>- It adds `seat_cut= central03= x03= y03= variant=`.<br>- `HC_GRAB_SUMMARY` adds `variantContinuity=a/b variantCuts= variantCentral03=a/b`. | `Runtime/Castle/PlaytestDriver.cs` |
| fix | `CommitGrab` resets `clipStartT`. Side commits after a drop logged `clip_start_t=0.28`, which ended the side clip 0.28 s early. | `Presence.cs` |

**Tests (`Tests/RulesTests.cs`, `Fe3GrabTests`):**
- legacy deck parse and round trip;
- the starvation rebalance turns the first side;
- 2,000 random saved decks normalise with side lead <= 1 and no group shut out;
- the commit gate and the first side are mirror-symmetric at every bearing;
- natural9 is reproduced;
- the seat decision covers commit, hold and cut;
- **100 side plans from 50 random rooms and their mirror images.** These use Presence's frame yaws, flank lines, body room and direct flight; he flies to the start and commits at the 0.7 m radius.
  - Left feasibility equals the mirror's right in every room (95/95).
  - m10 50 / m11 48 selected (|diff| <= 2 asserted).
  - 0 commit-gate failures.

## Verification (static, final tree)

- `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py`: **PASS**, 1395 assertions (base 1308). Only the known CS0649 / CS0162 / CS0219 warnings. One run printed a Roslyn internal stack trace but still exited 0; three reruns were clean.
- `tools/check_castle_scares.py`: PASS.
- `tools/check_audio_keys.py`: OK.
- `tools/bellwether_fuzz.py`: **identical**, sha256 `ee3b74dc…fd68f2f`.
- No scene, prefab or asset changes.

## Integrator recipe

1. Merge `hc/fe3-grab`.
2. **Re-bake both maps.** `Presence.cs` changed, and a new Core file was added. Then build as LOOP.md says.
3. Delete `hc.castle.grabdeck` once before the first run (a fresh-player deck), then keep PlayerPrefs between runs. Restore the owner's afterwards: `hc.castle.runs` `L,E,L` / `E,M,B`, grab deck deleted.
4. Launch every run windowed through the slot:
   `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-fe3 -- open -g -W -n -a "<app>" --args -logFile <dir>/player.log -hc-playtest <dir> <mode>`
5. Runs:
   - natural ×3, in a row;
   - fear;
   - siege_held ×2 (`-hc-hold-dread 60 ... siege`);
   - huntcam.

**Item 1 pass:**
- pooled `HC_GRAB_COMMIT mode=10` >= 1 and `mode=11` >= 1;
- every `HC_GRAB_VARIANT plan=` line is followed by two `HC_GRAB_FLANK side=L|R stage=plan feasible= reason=` lines;
- report the per-side feasible counts and the `reason=` histogram;
- `stage=commit feasible=False` lines name any commit refusal;
- `HC_GRAB_DECK rebalance` appears only after 2 uncommitted plans of one mode.

If m10 is still 0, the `side=L` reasons say which gate shut it, by room.

**Item 2 pass**, on every `HC_PLAY_GRAB_CONTINUITY ... variant=True`:
- `pass=True` (100 %);
- `commit_gap` <= 0.45, or `seat_cut=True` with an `HC_GRAB_CUT` line for that grab;
- `central03=True`.

`HC_GRAB_SEAT` for mode 10/11/12 must show `gap` <= 0.45 or `note=cut on frame 0`. The summary should read `variantContinuity=n/n variantCentral03=n/n`.

Look at the `t0.01` / `t0.16` frames of one side grab and one spin grab. Check for no slide, and that a cut reads as a cut: the skull is centred on frame 0.

Also check:
- `HC_GRAB_SIDEFRAME` lines (expected `covered=none bias=0.0`);
- `diveUnresolved=0`;
- open grabs and drop share as in fe2-count.

## Risks

- **Unproven in the engine.**
  - The skull-track prediction assumes the silhouette is upright at the commit (the same assumption `sideStartLocal` already made). If it is off, the hold converges to the wrong point; the measured frame-0 gap then decides, and that case cuts.
  - A hold can delay a side or spin commit by up to 0.3 s. The dive clock and the miss rules still run.
- **Cuts.** Frequent cuts would replace slides with visible edits. Expect gaps of 0.55-1.1 m to close during the hold (he flies about 1.2-1.5 m in 0.3 s). Report the `variantCuts` count.
- **Deck tuning.** The normalisation and the starvation rebalance change which variants come first. Drops still target 0.45, and the side group stays capped at its share + 2.
- **Planning cost.** Both sides × up to 4 yaws now run `Reachable` (NavMesh path) at dive start: up to 8 path queries per dive.
- **Hand guard.** The held-item boxes are world AABBs projected through the view, so they overestimate. The bias is capped at 15° and logged.
