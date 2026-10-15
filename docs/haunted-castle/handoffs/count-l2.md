# Piece count-l2: wiring F5's wave L grabs (modes 10-12) on top of count-l

Branch `hc/count-l2` = `hc/count-l` (`dd77242`) + `hc/grabs-l` (`6ee726e`, merged, no rebase). It replaces count-l for the integrator.
Worktree `/Users/midir/hollow-current-haunted-castle/.wt/count-l2`. Model: Opus 5.5. Date 2026-10-14. This is code only: nothing was launched.

Static checks, all passing:
- `tools/check_source.py` (with `INPUT_SYSTEM_REFERENCE`): PASS 1071 rules assertions; runtime and editor C# compile, including F5's `CountClaudeImport`.
- `tools/check_castle_scares.py`: PASS.
- `tools/bellwether_fuzz.py`: PASS, identical over 4000 × 300.

Everything in `count-l.md` still applies; this file adds the new modes only.

## Files
- `Assets/HollowCurrent/Runtime/Presence.cs` (castle branch)
- `Assets/HollowCurrent/Runtime/Castle/CountClaudeProfile.cs`
- `Assets/HollowCurrent/Runtime/HearthboundActor.cs`: **taken over by F2 for this change.** `ModeFrames` is now sized 13 (modes 0-12). Only that line changed; Bellwether only uses modes 0-3, and the counting is guarded by length.

## Profile
- **Modes:** `ModeGrabSideLId=10`, `ModeGrabSideRId=11`, `ModeGrabDropSpinId=12`, `ModeCount=13`. Each is a settable field (-1 disables it).
- **From the receipt** (`ApplyReceipt` reads `CountClaudeModes.json`):
  - from `grab_variants[]`: the per-mode reach (fallbacks 1.596 / 1.607 / 1.614) and GrabDropSpin's `top_m_max` (4.735);
  - the spin window `grab_spin_start_s`/`end_s`/`yaw_deg` (0.5 / 1.15 / 120);
  - `grab_side_start_x_m` (3.4).
- `StandOffFor` covers 10-12 (reach + 0.6 m).
- `HC_COUNT_REACH` now also logs `sideL= sideR= spin= spinWindow=`.

## Selection (`PredictGrab`, every dive frame; hide entrances keep the drop/crawl rule)
- **High dive** (ceiling clears the drop): 60 % of dives with headroom (was 50 %).
  - Mode 12 when the spin roll (45 %) passes, his bearing from her view is 50-120°, and the ceiling at the spot behind her clears the spin clip's 4.735 m.
  - Otherwise mode 8.
  - That keeps mode 8 at ≥ 0.6 × 0.55 ≈ 33 % of headroom grabs even if she always faces away, and higher in practice, because a diving Count is usually in front of her.
- **She is crouched, he is under a low lintel or on foot:** mode 9.
- **Low dive, bearing 35-110° from her view, side roll (60 %) passed, flank clear:** a side snatch.
  - Mode 10 from her left, mode 11 from her right.
  - "Flank clear" means two line-of-sight tests are clear: her eye to the stand-off point, and that point to the clip's start 3.4 m to the side.
  - A side snatch commits within 3.8 m of her (`SideCommitDistance`), because the clip brings him in from the flank.
- **Otherwise:** the count-l rule (`ChooseGrab`), usually 9.
- Every grab logs `HC_COUNT_GRAB mode= height= reason=`. Example reasons: `drop behind her (she faces away, bearing 87)`, `side snatch from her left (bearing -64)`, `high dive, drop (bearing 12, spin not rolled)`.

## Placement and camera
- **Side (10/11):**
  - His origin goes to the stand-off straight ahead of her view, facing her (`HC_COUNT_GRAB placed=side at= bearing=`).
  - The camera turns (0.15 s) to a fixed point 1.55 m over his origin and holds there for the clip's 1.2 s while he enters from the flank, then frames as usual.
  - No claw easing on the variants.
- **Spin (12):**
  - His origin goes to the stand-off on his side of her, behind her, facing her (`placed=spin`).
  - Her view is held from commit to 0.5 s, then spun to his skull (yaw and pitch, no roll) by 1.15 s, then held.
  - Logs: `HC_COUNT_GRABCAM spin=start t= yawOff=`, and an extra `HC_COUNT_GRABCAM` sample at 1.45 s (spin end + 0.3 s), which is where F5's camera check passes.
- The commit-frame snap is skipped for 10-12 (by design he enters the frame, or is unseen until the spin).
- **Side check.** On the first side grab of a run, at t ≥ 0.1 s, the lead claw (the hand furthest from the frame centre) must be on the side the snatch came from. If not, the 10/11 mapping is swapped for the rest of the run.
  - Log: `HC_COUNT_GRAB side_check=ok|reversed mode= leadX= expected= swapped=`.
- `HC_COUNT_MODES bound=` now lists all 13 states. `frames=` is the 13-slot `ModeFrames`.

## Integrator
1. **Run `CountClaudeImport.Prepare`** (F5's step) before any build. Expect `HC_COUNTCLAUDE_VARIANTS GrabSideL=1.20s GrabSideR=1.20s GrabDropSpin=1.90s` and `13 independent motions`.
   - Without it, modes 10-12 are unbound and this code falls back to 8/9: `HasModeState` is false and no reason mentions side or spin.
2. **eyeToSkull continuity (≤ 0.5 m between samples)** still holds for repositioning: placement happens before the t=0 sample, and nothing moves him afterwards. The clips' own motion is large, though: the side snatch crosses 3.4 m in about 0.4 s, and the spin falls from 2.2 m. Judge pops from the `pos=` field of `HC_COUNT_GRABCAM`, not from eyeToSkull, for 10-12.

## What a windowed run must prove
- `HC_COUNT_MODES bound=... 10:GrabSideL=1 11:GrabSideR=1 12:GrabDropSpin=1`.
- **Over the natural/huntcam set:**
  - `HC_COUNT_GRAB mode=8` on at least 30 % of grabs whose dive logged `style=high` or headroom.
  - At least one each of mode 10 or 11, and of 12, with their reasons.
  - Frames per mode > 0 in `HC_COUNT_MODES frames=`.
- **Side grabs:** one `side_check=` line, and the capture at 0.3 s shows the claw coming from the logged side.
- **Spin grabs:** `spin=start`, then the 1.45 s `HC_COUNT_GRABCAM` line with `skull_in_central60=True claw_ok=True`.
