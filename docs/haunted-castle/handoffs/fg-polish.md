# Handoff: fg-polish (wave FG): the four FF leftovers

Branch `hc/fg-polish`, worktree `.wt/fg-polish`, base `haunted-castle` `b46865c` (code `9269090`). Builder: Opus 5.5, 2026-10-19.

Nothing was built, baked or launched. A blind critic owns the engine. Every runtime claim below is a design claim that the integrator's runs must prove. The evidence is static:
- the FF logs `.playtest-ff-*.log` and frames `rounds/r14/captures/ff/` in the main clone;
- the pure rules and their models (`Tests/RulesTests.cs` `WaveFGTests`);
- compiles against the player and editor assemblies, the Bellwether fuzz, the lints;
- for item 4, CPU-only offline projection (Blender headless ray casts, numpy shader twins).

Subject: HANDOFF top section, open items 1-4 (wave FF integration), all under owner feedback item 12 (fear and the kill) and the r14 critic's S1/S4 art tests.

## Item 1. Follow-up hunts after an apparition were cancelled

**Cause (log evidence).** Over natural1-5, 9 follows were drawn, 1 fired, 7 `HC_APPARITION_LEAD follow=cancelled reason=grace`, 1 `reason=held`.
- After a hunt the grace runs 30-90 s x the governor (natural1 hunt 2: 148.6 s; natural5: 167.2 s), and the first hunt also starts the 120 s long-quiet dice hold.
- The rhythm blocks apparitions only in the grace's last 20 s (`ScareRhythm.CastleBlocked`). So an apparition in the grace always had more than 20 s of it left, and its follow, due 12-19.5 s later, always still met the grace: grace left at the apparition 53, 36, 55, 39, 55, 44, 113 s (`HC_ESCALATE ... grace=`).
- natural5's `held` follow fell in the long-quiet hold after the first hunt.
- natural4's first-hunt follow asked for its apparition at 119.0 s with `huntIn=12.9` (`HC_SCARE_WANTED reason=first_follow`). The request window was lead..12 s, so it was 0.9 s wide, and its one try logged `no_visible_point`.

**Change.**
- `HuntRules.ApparitionFollowGraceCut` 20: a follow due with at most 20 s of grace left ends the grace and fires (`HC_APPARITION_FOLLOWUP fired ... graceCut=`).
- `ApparitionFollowOweSeconds` 60: a follow that would still meet more grace, or any dice hold (the long quiet stretch or a lull), is **owed**, not armed (`HC_APPARITION_FOLLOWUP drawn=owed quiet=grace|held`).
  - Once the quiet ends within its lead, `Investigation.WantsApparitionNow` asks the director for an apparition (`HC_SCARE_WANTED reason=owed_follow`).
  - The next visible apparition that can carry it adopts it (`HC_APPARITION_FOLLOWUP adopted`), and its threat comes 12-19.5 s later.
  - It lapses 60 s after it became placeable. Any threat, Containment or the end cancels it.
  - Every cancel logs `HC_APPARITION_FOLLOWUP cancelled=<reason> state=armed|owed`, and the FF `HC_APPARITION_LEAD follow=cancelled` line is kept.
  - The dice hold is never cut, so the >= 120 s gap holds.
- The first-hunt follow's request window opens 8 s before its lead and stays open to 3 s before the deadline, while tying the deadline keeps the plan's band (`Investigation.FirstFollowWantEarly/Last`).
- ScareDirector re-arms its request (`next=0`) for every new request episode. FF logged the first request only once per run.
- `HC_RUN_PLAN` logs `firstLead=`: the FF `HC_APPARITION_LEAD first=` line was emitted before the log hook, so no FF log shows it.
- `HC_PLAY_STATS` adds `followDrawn= followOwed= followAdopted= followFired= followCancelled= followCancelledPct=`.
- Files: `Core/HuntRules.cs`, `Core/Investigation.cs`, `Runtime/ScareDirector.cs`, `Runtime/Game.cs`, `Runtime/Castle/PlaytestDriver.cs`.

**Tests (`WaveFGTests` 1-4).**
- The mechanics:
  - owed in the first hunt's grace and hold, and asked for only once the hold ends within the lead and the grace within lead + 20 s;
  - adopted, then the threat comes 12-19.5 s after the adopting apparition with the hold over;
  - the grace cut;
  - a threat cancels an owed follow, and an owed follow can lapse, both logged.
- The first-follow window is >= 8 s.
- The session model (now with ScareDirector's wanted-apparition bypass) over natural x3 sets of consecutive plans:

| Model | FF rule | fg-polish |
|---|---|---|
| Follow cancellations | 0.68 (FF logs 7/9 + held) | **0.12** |
| 3-run sets with >= 2 leads at 10-20 s | 0.82 | **0.95** |
| Pooled 25-60 s | 0.27 | 0.21 |
| <= 10 s | 0.005 | 0.003 |
| Share mean | 0.201 | 0.212 |
| Runs in 15-30 % | 0.94 | 0.955 |
| Runs with a >= 120 s gap | 1.000 | 1.000 |

- Unchanged bands: `WAVE_L3` 3-run apparition CV >= 0.5 in 0.98 of sets, >= 4 apparitions in 480 s 0.997, first-hunt spread 1.000, hunts <= 10 s after an apparition 0.000.

## Item 2. The right side snatch (m11) is rare

**Cause (log evidence): no code or geometry asymmetry is left; two mechanisms lost the right side.**
- The plan-time flank closures over every FF run are symmetric:
  - `flank line blocked` 54 L / 53 R;
  - `she faces him` 16 / 16;
  - `no floor at the flank start` 8 / 9.
  - Feasible flanks: L 6, R 4 of about 40 dive plans.
  - The side map is not swapped (`HC_GRAB_SIDE_MAP ... swapped=False`). The hand lantern and the held items are on layer 30, outside `FlightMask`.
- **natural4 (2 of the 3 right plans):** `plan=side-right`, both flanks open, R first. He dove from perch 5 (8.1 m) straight over her (`HC_COUNT_DIVE close_s=0.01 ... to=1.56`), lost sight below him, and `CountChase` set the goal to her last known spot. He was already within 1 m of it horizontally, so both dives ended `miss=True reason=abandoned: the dive ended, now Land (lost sight, reached her last known spot) distance=0.99 vertical=4.97 plan=side-right`. The plan's start point was 3.5 m away and was ignored once he could not see her.
- **natural3 (`HC_GRAB_VARIANT` lines 248, 286):** a feasible right flank lost the plan to the spin, side 0.57 / 0.64 vs spin 1.13 / 1.07. The two side clips share one deck group (`d[10]+d[11]`), so after the left commits (deck 10:1 11:0) the starved right side scored like a used one.
- In the main set (natural x3 + cold x2 + huntcam) the right flank was feasible once (natural3), so m11 = 0 there was these two mechanisms plus chance.

**Change.**
- **Blind dives keep their plan.** While she is remembered (`Memory.Knows`, 7 s), a planned side or spin keeps its last start point and he flies on to it. The give-up timer still runs (`HC_GRAB_VARIANT blind=True`; `HC_GRAB_FLANK side= stage=blind`). At that point he senses her within the start point's reach (`CountSees`, line of sight still required), so the commit gate can pass instead of landing.
- **Balance bonus.** A side with fewer commits than its pair scores `SideBalanceBonus` 0.6 more (`SideBalanceRules.Bonus`, logged `side=x.xx(+0.6 balance)`). The side group's target share is unchanged.
- **Logging.** `HC_GRAB_FLANK stage=plan` adds `yaws=` with every frame yaw tried and why it failed (`0:line,20:line,-15:floor,35:room`). FF logged only the yaw-0 reason.
- Files: `Runtime/Presence.cs`, `Runtime/Castle/CountClaudeProfile.cs`, `Core/GrabPlanRules.cs`.

**Tests (`WaveFGTests` 6).**
- **Fixed castle scenes:** a 3 m corridor, the 2.92 m upper gallery (back wall, rail with no floor past it), the hall floor between the piers (x +-3.9, every 3 m) with the table ahead, and the chapel aisle between pew rows.
  - Each scene is run with her centred and +-0.5 m off-centre, facing 0 / +-30 / +-60 / 90 deg, and mirrored: 72 combinations.
  - Left feasibility equals the mirror's right in all 72.
  - A *solid* torch hand (0.08 m, at the Investigator lantern offset) would close only left flank lines (5 cases, 0 right). So even a collider there could not explain a rare right side.
- `yaws=` is logged mirrored.
- The natural3 case outscores the spin before jitter with the bonus.
- **Side-mix model** (natural x3 + cold x2 + huntcam + fear, 19 dives at the FF feasibility rates, commit rate 0.75 -> 0.9 for the blind-dive fix): sets with m10 >= 1 and m11 >= 1 go from 0.56 to **0.69**, and crawl over 40 % goes from 0.28 to 0.21.
  - The cap is side feasibility (a flank is open in about 1 dive in 5), not the planner. One run set will still miss a side about 1 time in 3. Add a natural run rather than force it.

## Item 3. A fear hide grab slid 0.91 m

**Cause (fear log, lines 398-422).**
- `HC_GRAB_SEAT gap=0.92 offset=(-0.43, 0.82, -0.05) mode=9 path=hideopen note=none`.
- fe-count's reseat (open floor) and fe3-grab's frame-0 cut (side/spin) were both written `!grabHide && ...`. A hide grab is placed in the torn door and takes neither, so the 0.92 m seat slid.
- `FrameClaws`, the hide grab's claw framing, then eased the root about another 0.5 m toward her: root x 2.56 at t 0.01, 3.21 at 0.30, 3.27 at 0.46, with 13 `KeepSkullOut` pushes after.
- Measured `root_slide=0.91`, `root_ok=False`. The model reproduces it at 0.93.

**Change.**
- `GrabSlideRules.Seat`: a hide grab's frame-0 gap over `SeatMaxGap` is a hard cut, like side and spin (`HC_GRAB_CUT mode= path=hideopen|nearmiss ...`, `seat_cut=True`).
- A **root leash** on every grab path (`Presence.RootLeash`, `GrabSlideRules.Leash`): his rendered root stays within `SeatMaxGap - 0.02` = 0.43 m horizontally of where frame 0 drew it, whatever moves it (seat decay, `FrameClaws`, the door clamp, the open-floor reseat remainder). The excess stays in the seated root. `HC_GRAB_LEASH mode= path= t= wanted= held=` logs the first hold. Only the lens-in-skull push (`KeepSkullOut`) may pass the leash, and that is logged `HC_GRAB_LEASH over=skull_push`.
- In the driver, `root_ok` is now `root_slide <= 0.45` or a seat cut (was <= 0.5), with `leash_held=`. `HC_GRAB_SUMMARY rootSlideOk=` follows it.
- Files: `Runtime/Presence.cs`, `Core/GrabPlanRules.cs`, `Runtime/Castle/PlaytestDriver.cs`.

**Tests (`WaveFGTests` 5).**
- Seat paths: hide 0.92 -> cut, hide 0.14 -> keep, side 0.5 -> cut, open 0.6 -> reseat, vertical-only 1.32 -> keep.
- The fear case: 0.93 m unleashed, 0.43 m on the leash.
- 2,000 random grabs over every path: root travel <= 0.45 m or a cut in all of them.

## Item 4. S1 table 10.8k of 15k px and composition; the moat's sd and battlement seam

**S1: cause.** The integrator's sweep showed light cannot reach 15k px: the far half of the table is dark wood seen edge-on. From the FF eye (0, 2.95, 14.55) the ribcage fills the bottom centre and the table is small and far.

**S1: change, option "camera" from the brief.**
- The S1 capture eye moves to gallery height down the axis: (0, 3.6, 16.0), aimed at (0, 1.27, 28), about 11 deg down (`CaptureMode` S1, `CastleWorld.S1MoonEye`).
- The table's lit top now faces the camera near the frame centre, and the ribcage drops to the frame bottom.
- The layout is untouched: the skeleton sits 0.63 m from the table (0.6 required) and its head cannot move toward the door leaves. So `HC_SKELETON`, `HC_HALL_AXIS`, `HC_CHAPEL`, the great doors' swing and the two exempted table candles (14 m from the new eye) are unaffected.
- `MoonAimX` 2.6 -> 2.45 keeps the S5 rose moons inside the rose (x +-0.95, y 7.1).
- The S1 masks in `tools/ff_art_masks/` are regenerated for the new camera. The FF build's S1 frames will now WARN (camera mismatch) until the new build.

**S1: offline estimate.** The FF lantern-off frame was re-projected through the new camera with `tools/ff_hall_raycast.py` / `ff_hall_reproject.py`, then `check_art_views` logic was run on it (low bound / estimate):

| Test | Estimate |
|---|---|
| Table px, luma >= 14 | 18,281 / 19,502 (FF 10,775) |
| Lancets >= 150 rows | 3 / 5 |
| Moon in a lancet | the west lancet, 478 px |
| Near piers | 2.47 / 2.89 |
| Floor mirror | 276 px |
| Near-black | 42.8 / 39.9 % |

**Moat: cause.** Most of the FF failures were mask errors: the BVH proxy behind `check_art_views` has no shore rocks.
- About 95 % of the S4 left sd patch was the two lamp-lit foreground boulders (water alone: 14.5, not 16.3).
- The battlement_out mask ran onto the dark bank rocks, so its 32.9 "seam" was lamp-lit water against a rock inside the mask (water alone: 13.4).

`tools/fg_moat_masks.py` cuts these occluders out of the proxy masks (it never adds water pixels). The committed masks are corrected. On the FF frames this gives S4 right 12.6, left 14.5, battlement 13.4, bridge_left 6.5.

**Moat: water change** (`CastleWater.shader`):
- `_Ripple` .2 -> .15.
- The lamp glint is a narrower and dimmer lobe (`_LampRough` .11 -> .085 and `_LampGain` .55 -> .45, now properties); it stays a warm column.
- The mirrored castle and rock outcrop are less black (`_CastleTint` x2.2, `_RockTint` x3).
- `_HorizonGlow` .03 -> 0.
- `tools/ff_moat_twin.py` now carries ff-integ's values and these.
- Estimates on the FF frames (the twin reads 1-2.8x brighter than Unity, so it is used only as a delta):

| Test | Estimate | Pass line |
|---|---|---|
| S4 sd right | 11.6-11.7 | <= 12 |
| S4 sd left | 11.6-12.5 | <= 12, **borderline** |
| battlement step | 9.3-15.7 | <= 25 |
| bridge_left | 6.3-6.8 | <= 25 |
| Red moon cluster | 6.8-7.3k px | |
| Pale glade | 233-260 px | |

**Evidence (gitignored, inside this worktree):** `docs/haunted-castle/rounds/r14/captures/fg-art/` (`hall/{ff,fg,eval_fg,check_est,check_lower}`, `moat_mask_fix/`, `moat_est_{mult,add}/`, `glint_ff_vs_fg.png`). It goes when the worktree is removed; copy it first if wanted.

## Static checks (HEAD)

- `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py`: **PASS**, 1484 assertions (base 1455); the runtime player, runtime editor and editor assemblies compile; known warnings only. The rules run took 19.5-19.8 s on a loaded machine (limit 30 s).
- `check_castle_scares`: PASS.
- `check_audio_keys`: OK (69).
- `check_throwables_scene`: rc 0.
- `bellwether_fuzz`: **identical** (`ee3b74dc...fd68f2f`).
- No scene, prefab, animation or audio asset changes.

## Integrator recipe

1. Merge `hc/fg-polish`. **Re-bake both maps:** shared code changed (`Core/Investigation.cs`, `Core/HuntRules.cs`, `Core/GrabPlanRules.cs`, `Runtime/Game.cs`, `Runtime/ScareDirector.cs`, `Runtime/Presence.cs`), and `CastleWater.shader` / `CastleWorld` changed for the castle. Then build as LOOP.md says.
2. Expect the castle prepare log as before:
   - `HC_SKELETON PASS`, `HC_HALL_AXIS PASS`, `HC_CHAPEL PASS`;
   - `HC_DOOR_LEAF_SUMMARY problems=0`, `HC_KIT_AXIS_SUMMARY problems=0`;
   - `HC_CANDLES floating=0`, `HC_HIDE_SPOTS_SUMMARY failed=[]`, `HC_EXTERIOR_AXIS ok`.
3. Delete `hc.castle.grabdeck` once, then keep PlayerPrefs across the runs. Restore the owner's afterwards: `hc.castle.runs` `L,E,L` / `E,M,B`, grab deck deleted.
4. Runs through the slot, windowed, `open -g`, in this order:
   - natural x3;
   - cold x2;
   - fear;
   - huntcam;
   - siege_held x1 (`-hc-hold-dread 60 ... siege`).
5. Captures:
   - rooms off and on (`-hc-capture-size 1920x1080`);
   - exterior (`-hc-capture-art exterior`).
   - Then run `python3 tools/check_art_views.py <rooms_off> <rooms_on> <ext>` and `exposure_report.py` on rooms off/on.

**Bands:**
- **Item 1 (natural x3):**
  - >= 2 threats 10-20 s after an apparition, pooled (`HC_PLAY_STATS ... lead10_20=`);
  - `lead25_60` <= 50 %, `lead10` <= 35 %;
  - pooled `followCancelled/followDrawn` <= 30 % (`HC_APPARITION_FOLLOWUP cancelled=` lines / `drawn=` lines);
  - share 15-30 %, `longestGap` >= 120 s, `pctWithinScare10s` <= 35 %, >= 4 apparitions per 480 s.
  - Look for `HC_SCARE_WANTED reason=owed_follow` and `HC_APPARITION_FOLLOWUP adopted`, and report any `cancelled=lapsed`.
  - Cold x2 must still log `gate=cap` with the first hunt about 155-164 s in.
- **Item 2 (natural x3 + cold x2 + huntcam + fear):**
  - `HC_GRAB_COMMIT mode=10` >= 1 and `mode=11` >= 1;
  - report the `HC_GRAB_FLANK ... yaws=` histogram per side, any `stage=blind` lines and `side=...(+0.6 balance)` scores;
  - mode 9 <= 40 %, `diveUnresolved=0`.
  - The model expects both sides in about 7 sets of 10. If one is missing, add a natural run before judging.
- **Item 3 (every run):** every `HC_PLAY_GRAB_CONTINUITY` has `root_ok=True` (root_slide <= 0.45 or `seat_cut=True` with an `HC_GRAB_CUT ... path=` line); `HC_GRAB_SUMMARY rootSlideOk=n/n`. Report `HC_GRAB_LEASH` lines (expected on hide grabs and kills) and any `over=skull_push`. Look at the t0.01 / t0.16 frames of one hide grab and the huntcam kill: no slide, and the claws still come in.
- **Item 4:** `check_art_views`:
  - S1 table >= 15k px lantern off, lancets >= 3 at 150 rows, a moon in one, piers >= 2:1;
  - S4 sd right/left <= 12, battlement / bridge_left seam <= 25, red moon and pale glade kept;
  - S4 / S1 p99 <= 16.7.
  - Look at S1 against `refs/great_hall.png` (the table in front, three lancets dominant) and battlement_out (the lamp glint still reads).
  - If S4 left is still over 12: `-hc-moat "_CastleTint=0.03:0.036:0.066:1"`. The FF water for an A/B is `-hc-moat "_Ripple=.2,_CastleTint=0.010:0.012:0.022:1,_RockTint=0.0035:0.004:0.0065:1,_HorizonGlow=.03,_LampRough=.11,_LampGain=.55"`. The table level is set with `-hc-hall-table`.

## Risks

- **Unproven in the engine:** the owed follow (does the director find a visible point inside 60 s?), the blind-dive flight to a variant's start point and the sensing there, the root leash's effect on the kill framing (claws may stop a little further than 0.8 m from the lens), and every S1 / moat number (offline estimates only).
- **Share:** firing more follows raises the model share from 0.201 to 0.212. Watch for a natural over 30 %.
- **The leash:** it holds his rendered root, so on a hide grab with a large `FrameClaws` pull the claws come in less. The lens-in-skull push may still exceed it, and that is logged.
- **Side feasibility** caps item 2: a flank is open in about 1 dive in 5, so a run set can still miss m11 by chance.
- **The S1 eye** floats over the skull at gallery height, not a spot a player can stand on; a critic may object. With the player 1.45 m further in, the 8 m lamp rule may light different hall candles, so S1 exposure can shift (estimate near-black 40-43 %).
- **The moat masks changed:** the S4 and battlement numbers are not comparable with r14/FF without `tools/fg_moat_masks.py`. S4 left is borderline (11.6-12.5). The lighter mirrored castle lowers the castle's contrast in the water.
