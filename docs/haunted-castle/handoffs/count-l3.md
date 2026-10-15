# Piece count-l3: drop-grab pop and claw_px_proj=0 (r13 proof on 61cfbb7)

Branch `hc/count-l3` from `haunted-castle` `61cfbb7`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/count-l3`. Model: Opus 5.5. Date 2026-10-14. This is code only: nothing was launched.

Static checks, all passing:
- `tools/check_source.py` (with `INPUT_SYSTEM_REFERENCE`): PASS 1100 rules assertions; runtime and editor C# compile.
- `tools/check_castle_scares.py`: PASS.
- `tools/bellwether_fuzz.py`: PASS, identical over 4000 × 300.

Files changed: `Assets/HollowCurrent/Runtime/Presence.cs` (castle branch) and `Assets/HollowCurrent/Runtime/Castle/CountClaudeProfile.cs`.

## 1. Drop pop
### Cause
The r13 tables (`rounds/r13/captures/f0a/*/grabcam.txt`, `f0b/huntcam1`) show `headAboveEye` going 1.6 → 4.6 between t=0.01 and 0.16 on every mode 8 grab.
- The GrabDrop clip's first frame has his skull about 4 m over his origin (the clip "falls from 4.72 m").
- The origin was left at the high dive's hover, about 2.35 m.
- The same mismatch, smaller, caused natural1's mode 9 step of 1.07 m.

### Fix
- **Head-matched commit (`MatchClipStart`, modes 8 and 9, open floor):**
  - At commit his current skull height is read (as seen, still pitched).
  - The figure is set upright and the clip is sampled at t=0 (`AnimationClip.SampleAnimation`), which gives the clip's skull height over his origin.
  - His origin is then placed so the clip's first-frame skull is exactly where his skull was.
  - Log: `HC_COUNT_GRAB root=matched mode= clip= headNow= clipHead= originY= floor= blend=`.
- **Root blend.** The origin then eases (smoothstep) to her floor over the clip's fall: 0.5 s for the drop, 0.35 s for the crawl. It runs on its own clock, so the kill re-entry cannot rewind it.
  - Log: `HC_COUNT_GRAB root=landed y= t=`.
- Claw easing waits until the root has landed.
- **Hides now always take GrabCrawl from the door.** The witnessed-hide dive, the hide open and the near-miss open all use it. A drop there would start 4.7 m over a crawling Count, which is the same pop.
- **Modes 10-12 are unchanged:** placement happens before the t=0 sample and nothing moves him afterwards, so `pos=` changes only with the clip.
- **Denser samples.** Grab-cam samples are now every 0.15 s from 0.3 s to 1.0 s (0, 0.15, 0.3, 0.45, 0.6, 0.75, 0.9, 1.0), so a 0.5 m step test sees the motion instead of a 0.3 → 1.0 s gap.

### Verified without the engine
I replayed the r13 tables with the new root rule: a scratch script reading each grab's logged `headAboveEye`/`eyeToSkull`, with the old origin at the dive hover.
- **Commit step (t=0.01 → first sample):**
  - mode 8: natural1 2.54 → 0.08, natural2 2.55 → 0.06, natural3 2.42 → 0.14, huntcam1 2.66 → 0.22;
  - mode 9: natural1 1.07 → 0.68 (that grab's dive was 1.2 m higher than the crawl start; it eases down over 0.35 s); death, fear and siege stay ≤ 0.17.
- The remaining larger steps in the replay sit between the old 0.3 s and 1.0 s samples (0.7 s apart): the clip's own fall plus claw easing. The new 0.15 s samples split them.
- The windowed rerun must confirm the real values.

## 2. claw_px_proj=0
### Cause
Every r13 sample logged `hands=[L=0(0in,0/0clear) R=0(0in,…)]`, so not one hand vertex projected inside the frame. The hand vertices exist (`HC_COUNT_HANDS vertices=930 groups=2`), so the positions were wrong.
- They came from `SkinnedMeshRenderer.BakeMesh(mesh, false)` multiplied by the renderer's `localToWorldMatrix`.
- The import puts a normalising scale on a carrier above the renderer, and BakeMesh's scale convention does not compose with that matrix, so the points landed far off screen.

### Fix
- The hand vertices are skinned by hand from the live bones: Σ weight × `bone.localToWorldMatrix` × `bindpose` × vertex. That is the same linear-blend skinning the renderer draws, and it does not depend on BakeMesh.
- The occlusion test also ignores the player's own collider.

### Audit fields
`hands=[L=px(Npts,Nin,clear/tested,dBone=D,first=<reason>) R=…]`:
- `pts` is the hand vertices sampled (≤ 64 per hand).
- `in` is how many fall inside the frame.
- `clear` and `tested` count the line-of-sight checks.
- `dBone` is the distance from the skinned points' centroid to the hand bone. It must be under about 0.4 m; anything larger means the skinning is still off.
- `first=` is the first rejection: `behind (z …)`, `outside (x,y)` or `occluded by <collider>`.

### Not verifiable offline
The pixel agreement (within 20 % of a pixel measure on the fear and huntcam frames) needs the rerun. Check `dBone` first.

## For the integrator's rerun (fear / huntcam / death)
- **Mode 8 and 9 grabs:**
  - `HC_COUNT_GRAB root=matched` at commit.
  - Consecutive `HC_COUNT_GRABCAM eyeToSkull` within 0.5 m.
  - `root=landed` by 0.5 s (drop) or 0.35 s (crawl).
- **Hide grabs:** `mode=9` with `reason=hide entrance (crawl from the door)` or `hide opened (GrabCrawl)`.
- **Every sample:** `hands=[… dBone<0.4 …]`, and `claw_px_proj` within 20 % of the frame's measured claw width at the same time.
