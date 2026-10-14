# Piece count18: door-grab visibility (r7b fear run on e57a00d)

Branch `hc/count18` from `haunted-castle` `b83a8b3`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/count18`. Model: Opus 5.5. Date 2026-10-14.
This is code only: nothing was launched. `python3 tools/check_source.py` passes (1071 rules assertions; runtime and editor C# compile).
Files changed: `Assets/HollowCurrent/Runtime/Presence.cs` (castle branch) and `Assets/HollowCurrent/Runtime/Castle/CountClaudeProfile.cs`.

## What the run showed
- **The grab sequence** (`.playtest-fear.log` 280-291, captures `rounds/r12/captures/r7b/fear/17-20`):
  - The crawl grab from the door was placed at (2.97, 0, 21.00).
  - `FrameClaws` then eased him in to (4.26, 0, 20.99). That is 0.61 m outside the door plane (x 4.87) and on her floor.
- **At t=1.00** the log projected his skull at the centre, 1.46 m from her eye, but the frame shows only stone.
- **The likely cause is frustum culling of the skinned mesh.** The Count's `SkinnedMeshRenderer` bounds come from the bind pose (upright, around his origin). The GrabCrawl end pose reaches 1.7-2.2 m forward and low, so once the view was aimed at the skull, the stale bounds box fell outside the frustum and Unity culled a mesh that was in front of the lens.
- At t=0.16-0.30 the bounds were still partly in view, which is why he showed up then.

## Change
- **Live bounds.** Every Count `SkinnedMeshRenderer` gets `updateWhenOffscreen = true` at init, so its bounds follow the current pose (`HC_COUNT_SKIN_BOUNDS live renderers=`). This applies to every pose, not only grabs.
- **Door clamp, every frame of a door grab (hunt grab and kill presentation).** The door geometry is recorded at commit (`CommitGrab`). His origin is then held:
  - on her floor: y = her feet, never below;
  - at least 0.05 m beyond the door plane (`DoorOutside`), so never inside or behind the wardrobe;
  - within 0.3 m of the door's axis (`DoorLateral`), so inside the door opening.

  The first clamp in a grab logs `HC_COUNT_GRABCAM clamp=door along= lateral= doorDistance= floor=`.
  - The depth stays free beyond the door plane because the clip's claws reach 1.6-1.7 m ahead of his origin. If his origin were held within 0.3 m of the door plane, the claws would pass through her lens. `FrameClaws` still eases the nearer claw to about 0.8 m, with no joint inside 0.6 m.
- **`visible=` in every `HC_COUNT_GRABCAM` line:**
  - `True` when any of his renderers' (now live) bounds are in the camera frustum and the line from the lens to his skull or chest is clear (or hits himself).
  - Otherwise `False reason=outside frustum` or `False reason=occluded by <collider>`.
  - The line also logs `pos=`.
- **Path label:** a grab at a hide entrance is `path=hideopen` even when the dive delivered it (the run's case: witnessed during a dive). Visit opens stay `hideopen` (watched) or `nearmiss`. Open-floor grabs stay `dive`.

## Hooks
- None.

## What a windowed run must prove
- On the fear run's watched hide, every `HC_COUNT_GRABCAM` line at `blend=done` (t=0.16, 0.30, 1.00) reads `path=hideopen visible=True`, with `pos` y equal to her floor and `pos` outside the door plane.
- Captures at t=0.30 and t=1.00 show him (skull and claws) in the door opening.
- If a line reads `visible=False reason=occluded by X`, then X names what blocks him.
