# Piece count17: the grab camera for hide grabs (fear run on ac43efa)

Branch `hc/count17` from `haunted-castle` `3d48761`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/count17`. Model: Opus 5.5. Date 2026-10-14.
This is code only: nothing was launched. `python3 tools/check_source.py` passes (1071 rules assertions; runtime and editor C# compile).
Files changed: `Assets/HollowCurrent/Runtime/Presence.cs` (castle branch) and `Assets/HollowCurrent/Runtime/Castle/CountClaudeProfile.cs`.

## What the fear run showed (`.playtest-fear.log` 287-306)
- **t = 0.01:** he was crawling at the door, 0.40 m from her eye, below and behind the camera. That gave `skull_px=(-14289,-43537)`.
- **t = 0.30:** GrabDrop played with his origin on her floor, 0.93 m out. The clip's own fall (receipt `top_m_max` 4.72 m) put his skull 2.48 m above her eye and 2.56 m away, so `claw_px=139`.
- The r11 transform-level "drop from above" was stacked on a clip that already falls. It is removed.

## Change
### CommitGrab (every grab path)
- **GrabDrop needs headroom.** The clip's own fall needs `GrabDropClipTop` (4.72 m) + 0.1 m of clear ceiling over the floor where he will stand.
  - With headroom: `HC_COUNT_GRAB from=above dy=<clip top - her eye height> ceiling=`.
  - Without: the grab becomes GrabCrawl from the door, logged `HC_COUNT_GRAB from=door reason=headroom ceiling= needs= mode=8->9`.
- **Hide grabs** (watched open, near-miss open, and a dive that ends at a hide entrance):
  - He is placed in the torn door opening, on her floor, at the chosen clip's stand-off from her, facing her (`HC_COUNT_GRAB placed=door at= standOff= mode=`).
  - The grab stays committed after the move (`hideGrabLocked` while she is hidden).
  - `FrameClaws` then eases him so the nearer claw sits about 0.8 m from her lens, with no joint closer than 0.6 m.
- **The Dead presentation** keeps a hide grab where it stands, and places any other grab at its stand-off as before. No extra lift is added.

### The view turn
- Yaw and pitch are blended separately over 0.15 s from wherever she was looking, the shortest way round (up to 180° of yaw, e.g. facing the wardrobe's back wall). There is no roll, then the view holds on him.

### Logs
- `HC_COUNT_GRABCAM t= path=hideopen|nearmiss|dive blend=turning|done kill= mode= skull_px= skull_in_centre60= claw_px= claw_ok= eyeToSkull= headAboveEye=`
- They are logged at the commit (`blend=turning`), at the end of the turn (`t≥0.15, blend=done`: the grab frame), at +0.3 s, at +1.0 s, and at the kill frame.
- `HC_COUNT_GRABCAM begin path= hide=` and `end path= reason=`.

### Sarcophagus
- Nothing grabs there. The gamble's hunt is a normal hunt, so its grabs take the dive path and the same headroom rule (the crypt ceiling chooses GrabCrawl).

## Hooks
- None. The integrator's 26341c8 already skips the Dead-phase look-at and the win while `GrabCamActive`.

## What a windowed run must prove
- **Fear run, watched hide with the torch off:**
  - `HC_COUNT_GRAB placed=door`, then `HC_COUNT_GRABCAM ... path=hideopen blend=done skull_in_centre60=True claw_ok=True`.
  - The same at `t≥0.30`.
  - In the hall, `from=above`; in a low room, `from=door reason=headroom`.
- **Siege run, near-miss open:** the same with `path=nearmiss`.
- **Huntcam and death runs:** `path=dive` lines with `blend=done skull_in_centre60=True claw_ok=True`. Any mode 8 is preceded by `from=above dy=`.
