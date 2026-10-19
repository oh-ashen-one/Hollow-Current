# Piece fd-grab: the kill presentation holds together (r13 critic top change 1)

Branch `hc/fd-grab` from `haunted-castle` `0c2796e`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/fd-grab`. Model: Opus 5.5. Date 2026-10-18.

This is code only. Nothing was launched, built or baked. The worktree has no Library folder, and LOOP.md does not allow builders to build. Every claim below about runtime behaviour is a design claim. The integrator must prove each one with the recipe at the end.

Static checks, all passing on the final tree:
- `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py`: PASS, 1209 rules assertions. Runtime and editor C# compile against the player and editor assemblies.
- `python3 tools/check_castle_scares.py`: PASS.
- `python3 tools/check_audio_keys.py`: OK.
- `python3 tools/bellwether_fuzz.py`: PASS, identical (sha256 `ee3b74dc…`, 4000 × 300).

Bellwether is untouched. Every Presence change sits behind `count != null`. The Interface changes only take effect when `Game.Map == MapId.Castle`.

Files changed:
- `Assets/HollowCurrent/Runtime/Presence.cs` (castle branch)
- `Assets/HollowCurrent/Runtime/Castle/CountClaudeProfile.cs` (new tuning fields)
- `Assets/HollowCurrent/Runtime/Interface.cs` (grab HUD)
- `Assets/HollowCurrent/Runtime/Castle/PlaytestDriver.cs` (measures and the teleport rule)

## Findings, causes and changes

### 1. Continuity (0 of 6 natural grabs passed)

**Cause.** Most of the failing steps were not pops. They were the clip's own motion over a screenshot frame. The driver takes a PNG at every `HC_COUNT_GRABCAM` sample, so the next frame lasts 0.1-0.2 s. The GrabDrop fall at about 5.5 m/s then moves 0.64 m in one frame.

The r13 measure compared each step with the median raw step of the neighbouring 60 fps frames plus 0.3 m. It ignored dt, although the critic's own definition is "clip velocity × dt + 0.3".

Some real pops existed too:
- The pose switch at the commit: the pitched dive pose snapped to the upright clip, and hide and side commits used a 0.06 s crossfade.
- Hide and side placement moved him at the commit.
- He was re-aimed every frame while committed. With any offset, that swings the skull.
- The kill re-entry re-aimed his rotation.

**Change.**
- **Presentation clock** (`PresentationClock`, `GrabMaxFrameStep` 1/30 s). While the grab camera runs, `Animator.speed` is scaled per frame. The clip, the root easing, FrameClaws and the camera then advance at most 1/30 s per rendered frame. A slow frame plays slower instead of skipping. A normal 60-90 fps frame is unchanged. While the game is paused, nothing advances.
- **Seat.** In the commit frame, every grab clip starts at once with `Animator.Play` (no crossfade). The root is offset so that his skull is exactly where the last rendered frame left it.
  - The offset decays over 0.35 s. A large offset takes longer: at most 3 m/s, never over 0.9 s.
  - A drop whose offset would lift the skull lowers the root's easing start instead.
  - Log: `HC_GRAB_SEAT gap= offset= seconds= note=`.
- **No re-aim.** He is no longer re-aimed while a grab is committed (CountChase), nor at the kill re-entry.
- **Drop-spin start.** The drop-spin now starts at its matching falling frame, as the drop does. It used to start at the peak, which lifted him.
- **Driver measure.** The driver now runs last in each frame (`[DefaultExecutionOrder(10000)]`), so it reads the rendered skull. `HC_PLAY_GRAB_CONTINUITY` now does the following:
  - Each step is judged against the neighbours' median speed × this frame's presentation dt (`Presence.GrabFrameDt`) + 0.3 m.
  - Mode 9 also has a 0.5 m per-step limit.
  - The commit gap is taken from the frame immediately before the commit, for every grab (hide grabs included).
  - It adds `min_eye_skull=` and `line_blocked=`.
  - It keeps the r13 measure as `legacy_pass=` / `legacy_pop=`.
  - Totals: `grabContinuity20=` in `HC_PLAY_STATS` and `continuity20=` in `HC_GRAB_SUMMARY`.

### 2. No skull in the commit frame (3 of 8)

**Cause.** The snap test used his renderers' bounds in the frustum (the wings), not the skull. For natural1 #1, `visible=True` was logged while `skull_px=(-630,-1378)`.

**Change.**
- In the first grab frame, the view's starting yaw and pitch are clamped to within 0.7 of the half field of view of the skull. This applies to every mode, side and drop-spin included. The turn continues from there.
- If the skull is still outside the frame (steep pitch), the view cuts to him.
- Log: `HC_GRAB_FIRSTFRAME skull_px=(x,y) visible= inFrame= lineClear= snapDeg= [cut=True] eyeToSkull= seatGap= mode= path=`.

### 3. Camera 0.31-0.46 m behind his cranium; candelabrum in the line

**Change.**
- `KeepSkullOut` holds the lens at least `MinSkullDistance` 0.65 m from his skull every presentation frame.
  - It pushes his root back along her line of sight, or along the door axis in a hide grab.
  - FrameClaws may no longer ease him closer than that.
- Small props (objects named `Candelabra`, plus `Floater.All`, with bounds under 2.6 m) within 0.22 m of the eye-to-skull or eye-to-claw line get their renderers and colliders disabled for the grab.
  - Candle lights stay on.
  - They are restored when the grab camera ends.
  - Log: `HC_GRAB_LINE hidden=`.
- Per grab: `HC_GRAB_CAMDIST min= at_t= ok= frames= lineClear=a/b blockers=[..] pushes= hiddenProps=[..]`. It is logged at the clip end or when the grab ends.
- Open-floor commits also need a clear eye line (see 4).

### 4. Crawl through the giant skeleton (death run)

**Change.**
- **Ring point.** The ring point of an open-floor dive is now searched in 25° steps either side of his bearing, 7 steps each way (`RingDirection`). A point is accepted only if all of these hold:
  - There is floor at her level (not a skeleton box top or a pew).
  - His body fits there.
  - The ceiling clears the clip's fall (for drops).
  - `GrabPathClear` passes. It casts the clip's body as a 0.3 m capsule 0.35-1.75 m over her floor, from the point to her. It also checks her eye line to his skull height.
- **Commit hold.** An open-floor commit waits while the path from his actual position is blocked. Log: `HC_GRAB_PATH held=True blocker=`.
- **Ground.** Next to her, a dive's "ground" is her floor, never the top of furniture or the skeleton's boxes. Log: `HC_COUNT_ROUTE ground=her_floor` (this is also the huntcam fault, 8).

### 5. HUD during the Return grab (dispatch card on every frame)

**Cause.** The grab-hidden branch still drew `SubtitleCard()`.

**Change.**
- From the commit frame, Interface draws nothing at all: no captions, no dispatch card, no prompt and no objective. A timed card resumes after the grab.
- Draws through Panel, Label, Track, Artwork and TextureFrame are counted while a grab is on.
- Per grab, Interface logs `HC_GRAB_HUD pixels=0 draws=0 frames= captionsHeld= fadeFrames= end=`. `captionsHeld` counts the frames whose caption was held back. `fadeFrames` counts the kill's own fade to black, which is presentation, not HUD.

### 6. Variety (modes 10, 11 and 12 appeared 0 of 12; drops at 20 %)

**Cause.**
- Side and drop-spin needed separate dice (0.6 and 0.45) and a bearing window at the commit: 35-110° for side, 50-120° for spin.
- The dive flew straight at her, so the bearing at the commit was usually small.
- The huntcam, death, kill and fear drivers stare at his head, which gives a bearing of about 0.
- The drop roll (0.6) was then often converted to a crawl (8→9 at low skull, or no headroom at the spot).

**Change.**
- `PlanVariant` at dive start chooses the grab from a deck kept across runs (PlayerPrefs `hc.castle.grabdeck`, open-floor commits by mode).
  - It takes the eligible grab furthest below its target share, plus jitter of up to 0.35.
  - Targets: drop 0.40, drop-spin 0.15, side 0.25, crawl the rest.
  - Drop and spin need headroom over her. Side needs her flank open and a ceiling of at least 2.45 m. The less-used side goes first.
- A planned drop or spin comes in high. Everything else comes in low.
- **Side approach.** The side snatch flies to the side clip's real start point: the skull position sampled from the clip, relative to the stand-off ahead of her view. It commits within 0.7 m of that point.
- **Spin approach.** The drop-spin flies to a point 80° off her view, on his side, where the ceiling clears its fall.
- Both points follow her view until he is 6 m away, then lock.
- **Fallback.** If she turns to face him, or the plan is not committed within 3.5 s plus the travel time, the plain grab is used (`HC_GRAB_VARIANT dropped=`).
- **10/11 mapping.** The mapping is now set from the clips' sampled start skulls before the first side grab (`HC_GRAB_SIDE_MAP`). The old on-screen check now only logs.
- Logs:
  - `HC_GRAB_VARIANT plan= scores=[..] deck=` and `locked=`.
  - `HC_GRAB_COMMIT mode= path= hide= return= headroom= planned=` (the mode actually played).
  - `HC_GRAB_MODE_HIST grabs= m6..m12 hide= return= openHeadroom= openHeadroomDrops= dropShareHeadroom=` (running, per run).
  - The driver prints `HC_GRAB_MODE_HIST final …` and `HC_GRAB_SUMMARY` at Finish and on quit (death and huntcam quit without Finish).

### 7. Lighting pop at the commit (natural3: silhouette at t=0.01, lit at t=0.16)

**Cause.** This was not the layer switch. `HC_TORCH_FAIL source=dive` blacked out her torch for 1 s from the dive's last second, and it came back about 0.15 s into the grab.

**Change.**
- A torch that is failed at the commit stays out for the whole clip. A burning torch keeps burning.
- The rim light holds through the grab.
- The grave-ash lantern gate is now permanent in the castle. He is on layer 29 from his first frame, and the UV light never lights that layer. There is no layer switch at the commit, and no purple in the fight frames either (`kill/17`, `18`).
- Log: `HC_GRAB_LIGHT torch=on|held-out|off rim= gate=always`.
- **Decision.** I read the brief's "the grave-ash lantern still turns his wings purple (recolour gate)" as a defect to gate, as r12 and r13 both list it.

### 8. huntcam: the dive hovered 4.4 m over her and nothing followed

**Cause.** The ring point lay over the skeleton. `GroundUnder` returned the box top (1.85 m) and the high hover was added on top of it. The commit's vertical limit was 3.4 m, so the dive stayed in Chase at that spot for the rest of the hunt.

**Change.**
- The ground fix and the ring search (4).
- The high dive's vertical limit is now HighDiveHover + 1.6 = 4.2 m. MatchClipStart already eases a higher root down.
- A dive that reaches its goal next to her without a commit for 1.5 s ends as a readable miss:
  - He pulls up to the nearest perch, and there is no new dive for 2.5 s.
  - It counts as a grab survived if he came within 1.6 m.
  - Log: `HC_COUNT_DIVE miss=True reason=<gate> distance= vertical=`.

### 9. Driver teleport during a grab

**Change.**
- `natural` waits for any grab presentation before its 20 s teleport (`HC_PLAY natural teleport_wait`), and its stay clock pauses through a grab.
- Every driver teleport now goes through `TeleportPlayer`. One made during a grab logs `HC_PLAY_TELEPORT during_grab=True`, and the total appears as `teleportsDuringGrab=` in `HC_GRAB_SUMMARY`.
- The Return-kill release now lets the presentation play for 1.3 s first (r13 natural3 #2 had only 4 frames).

## Integrator recipe

1. **Merge and build.** Merge `hc/fd-grab`, then run the four static checks above.
   - The change is C# only. There is no scene, prefab or asset change.
   - Build and re-bake as LOOP.md requires after a C# hash change.
2. **Run.** Launch each run with `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-fd-integ -- open -g -W -n -a "/Users/midir/hollow-current-haunted-castle/Builds/macOS/Hollow Current.app" --args -logFile <dir>/player.log -hc-playtest <dir> <mode>`.
   - Modes: `natural` three times, `fear`, `-hc-hold-dread 60 … siege` (siege_held), `death`, `huntcam`, `kill`.
   - One run at a time, windowed, SIGTERM then wait.
   - Keep PlayerPrefs between runs: the deck carries variety across runs.
3. **Grep, all runs pooled.**
   - **Continuity:** `grep -h HC_PLAY_GRAB_CONTINUITY */player.log`. Pass: at least 80 % of the lines with `frames>=20` show `pass=True`, and `crawl_ok` is not False (`HC_GRAB_SUMMARY … continuity20=a/b`). Report `legacy_pass` beside it for the critic.
   - **First frame:** `grep -h HC_GRAB_FIRSTFRAME`. Every line has `visible=True`, with `skull_px` inside 0..2560 × 0..1440. Check against each `grabcam_*_t0.01.png`.
   - **Distance and line:** `grep -h "HC_GRAB_CAMDIST min="`. Every grab has `min>=0.60` and `lineClear=n/n`. `HC_PLAY_GRAB_CONTINUITY … min_eye_skull=` and `line_blocked=0/N` are the driver's independent check. In siege_held, `HC_GRAB_LINE hidden=Candelabra` is expected where one stood in front.
   - **HUD:** `grep -h HC_GRAB_HUD`. Every line has `pixels=0 draws=0`. Look at the natural Return grab frames for the dispatch card; there must be none.
   - **Variety:** sum `HC_GRAB_COMMIT mode=` over all runs. Pass: m10 ≥ 1, m11 ≥ 1, m12 ≥ 1, and `dropShareHeadroom` ≥ 30 % in `HC_GRAB_MODE_HIST final`, pooled from `openHeadroom` and `openHeadroomDrops`. The `HC_GRAB_VARIANT plan=` and `dropped=` lines explain any missing variant.
   - **Skeleton (death):** the grab frames show no body inside the ribs. Look for `HC_GRAB_PATH held` or a `ring=turned …` reason when he came from the skeleton side, and `line_blocked=0/N`.
   - **Lighting:** `HC_COUNT_GRAB light=lantern-excluded … gate=always` appears once per run. `HC_GRAB_LIGHT torch=held-out` appears wherever `HC_TORCH_FAIL source=dive` preceded a grab. The `t0.01` and `t0.16` frames have the same light. The kill hit frames show no purple wings.
   - **huntcam:** after `HC_COUNT_DIVE close_s=` comes either `HC_GRAB_COMMIT` or `HC_COUNT_DIVE miss=True`. `HC_COUNT_AI state=Chase` never repeats the same `pos=` for more than 2 s.
   - **Driver:** `teleportsDuringGrab=0` in natural runs. Each grab has frames ≥ 20.

## Risks

- **Unproven.** None of this has run in the engine. Untested paths include:
  - the side and spin approaches;
  - the 10/11 mapping from the sampled clip starts;
  - the seat with real dive poses;
  - the `Animator.speed` clock, in both Normal and UnscaledTime modes.
- **Visible slide.** A large seat offset slides his whole body for up to 0.9 s, for example a side or spin placement that misses its start point, or a forced death. It is continuous, but visible.
- **Camera cut.** The first-frame clamp cuts the view by `snapDeg` (logged), up to roughly 100° for a drop-spin. The critic asked for the skull at t=0.01 over a smooth turn.
- **Hunt pacing.**
  - The readable miss replaces the old indefinite hover. A blocked path, a recommit block or the vertical gate now ends a dive.
  - A planned side or spin can spend up to 3.5 s plus travel before it falls back.
- **PlayerPrefs.** The deck (`hc.castle.grabdeck`) is written by automated runs too, as the run history already is.
- **Clipping.** KeepSkullOut can push him partly into geometry behind him in a tight hide alcove.
- **Hidden props.** A hidden candelabrum's mesh vanishes for the grab, but its light stays on.
- **Fallback field.** `SpinShare`, `SideShare` and `DropDiveShare` are retired but kept in the profile.
