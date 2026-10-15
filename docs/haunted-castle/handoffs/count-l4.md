# Piece count-l4: drop continuity, the drop-spin loop, learned-hide bag (r13 f0c logs on f432fe5)

Branch `hc/count-l4` from `haunted-castle` `f432fe5`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/count-l4`. Model: Opus 5.5. Date 2026-10-15. This is code only: nothing was launched.

Static checks, all passing:
- `tools/check_source.py` (with `INPUT_SYSTEM_REFERENCE`): PASS 1103 rules assertions; runtime and editor C# compile.
- `tools/check_castle_scares.py`: PASS.
- `tools/bellwether_fuzz.py`: PASS, identical over 4000 × 300.

Files changed: `Assets/HollowCurrent/Runtime/Presence.cs` (castle branch) and `Assets/HollowCurrent/Runtime/Castle/CountClaudeProfile.cs`. F5 is not needed (no clip trim).

## 1. Drop continuity
### Cause
- **The sample never sampled.** count-l3 called `AnimationClip.SampleAnimation` on the silhouette root, which has no Animator. The player logged "Non-Legacy animations cannot be sampled outside the Editor without an Animator" (f0c natural2), so `clipHead=2.28` was the dive pose, not the clip.
- **The anticipation lift.** The GrabDrop and GrabDropSpin clips then open with a lift before the fall: the skull rose 1.3 m in huntcam1.
- **Commit too close.** The second huntcam drop committed 0.51 m from her: he was above her and the commit did not care how far he was.

### Fix
- **Correct sampling.** The clip's skull height over his origin is now sampled on the Animator's own GameObject every 1/60 s across the clip, with the figure upright.
  - It is cached per mode and primed at hunt start, in Update, so the Animator overwrites the pose before the frame renders.
  - Log: `HC_COUNT_CLIP_SKULL mode= clip= frames= start= peak=@t end=`.
- **Start at the first matching falling frame (GrabDrop).** Playback starts at the first frame after the peak where the clip's skull is at or below his skull over her floor (`Animator.Play` at that normalised time).
  - His origin is set so the skulls coincide. It is never placed under her floor.
  - If he is higher than the peak, playback starts at the peak and the root eases down; it only ever moves down.
  - If he is lower than the clip's landed skull, the grab switches to the crawl (`HC_COUNT_GRAB mode=8->9 reason=`).
- **GrabDropSpin** starts at its peak (she cannot see him yet). The camera's spin window is shifted into clip time.
- **Ring approach.** Open-floor dives now fly to the ring at the chosen clip's stand-off (backing out if he is inside it), and the commit must happen within 0.25 m of that ring.
- **Flare.** Over the last 1.2 m before the ring he flares upright, since the grab clips start upright.
- **High-dive hover** is set so his skull ends at the drop clip's peak height over her floor, so the commit has little left to re-seat.
- **Logs:**
  - `HC_COUNT_GRAB clip_start_t= mode= clip= skullFrom= skullClip=`
  - `HC_COUNT_GRAB root=matched ... skullDelta= fromLastDiveSample= blend=`. `fromLastDiveSample` is the 3D distance from the last pre-commit skull to the clip's start skull.
- **Offline check:** a scratch script simulated the start rule against a synthetic drop curve with a 1.3 m lift (2.3 → 3.6 m over 0.15 s, falling to 1.6 m by 0.5 s), for commit skull heights 1.0–6.8 m.
  - After the crawl fallback, the commit skull delta is 0 at the script's 0.1 m grid resolution.
  - The skull never rises after commit.
  - The real curve comes from `HC_COUNT_CLIP_SKULL` in the rerun.

## 2. Drop-spin loop
### Cause
- CommitGrab placed him exactly at the stand-off, and the next frame tested `distance < stand-off`. That read as out of reach, so contact decayed, the grab released, and he re-committed: 12 times in a row (natural1).
- The same release-then-resume pattern produced natural2's mode 3 / 14 m samples after a crawl commit lost line of sight on a pew.

### Fix
- A committed grab now stays committed until its clip has played (`ClipSecondsFor`: 1.6 / 1.4 / 1.2 / 1.9 s, minus any start offset) or she dies.
- Only the sword stagger ends it early (`CountStagger`).
- At clip end with her still alive (the driver's invulnerability) he lets go (`HC_COUNT_GRAB released=clip_end`).
- **Re-commit guard:**
  - No new grab within 2 s of any release, including `ReleaseGrab` from the driver.
  - No commit while his skull is more than the clip's reach + 0.6 m + 0.25 m out (horizontally).
  - Log: `HC_COUNT_GRAB recommit_blocked=True reason=released N s ago|skull X m out, limit Y`, at most once per second.

## 3. Learned-hide bag
- At Dread ≥ 50, the hide he keeps finding her in runs on a bag: exactly 1 open in every 3 visits, at a random position.
  - The bag starts with the first visit to that hide.
  - Other hides, and Dread below 50, keep the per-visit roll.
  - Watched hides always open.
- Logs: `HC_COUNT_NEARMISS bag=k/3 openAt=j open= hide= dread=`, also appended to the `HC_COUNT_NEARMISS begin` line.
- 6 visits at Dread 60 in one hide therefore give exactly 2 opens.

## Rerun log keys
- **huntcam / natural drops:**
  - `HC_COUNT_CLIP_SKULL mode=8` (the real peak and end);
  - `HC_COUNT_GRAB clip_start_t=` > 0;
  - `root=matched ... skullDelta≈0 fromLastDiveSample= ≤0.5`;
  - consecutive `HC_COUNT_GRABCAM headAboveEye` never increasing during a mode 8 grab.
- **natural1 drop-spin:** one `HC_COUNT_GRAB mode=12`, then no `GRABCAM end reason=released` before `released=clip_end` or the kill. Any second attempt within 2 s shows `recommit_blocked=True`.
- **natural2:** no `HC_COUNT_GRABCAM` sample in mode 3 during a grab.
- **siege50:** `HC_COUNT_NEARMISS bag=` lines with exactly one `open=True` per 3 visits.
