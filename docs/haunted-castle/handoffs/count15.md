# Piece count15 (wave I): the hunt reads as a hunt (r10 blind critic)

Branch `hc/count15` from `haunted-castle` `0fb5aad`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/count15`. Model: Opus 5.5. Date 2026-10-14.
This is code only: the owner had the machine, so nothing was launched, built or rendered.
`python3 tools/check_source.py` (with `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll`) passes: 970 rules assertions, and runtime and editor C# compile.

Files changed (castle only; Bellwether paths are identical):
- `Assets/HollowCurrent/Runtime/Presence.cs`. The only edit to a shared method is `Dismiss`, which now also resets `localPosition` when `count != null`.
- `Assets/HollowCurrent/Runtime/Castle/CastleRun.cs`
- `Assets/HollowCurrent/Runtime/Castle/CountClaudeProfile.cs`

Note on the evidence: H01-H10 are produced by the scripted capture choreography (`BeginCaptureChoreography`), not by the live AI. Both are fixed.

## 1. Flight posture
- **Live flight.** In Glide and Dive, while moving faster than 1.2 m/s in Travel, Retreat, Chase, NearMiss or the gatehouse flight, the whole figure pitches nose-down. The pitch is `FlightPitchMin` 38 deg plus 0.6 x the descent angle, capped at 68 deg. It pivots 1.2 m above his feet and is applied in `LateUpdate` on the silhouette.
- **Stays upright:** perch, search, crawl, the grabs, stagger, death, staged shots and the grab presentation.
- **Dive arms.** In Dive the arms reach along the pitched forward. Upper arm, then forearm, are aimed at weight 0.8 (`LeftArm/ForeArm/Hand`, `RightArm/ForeArm/Hand`). This is procedural, on top of the clip.
- **Capture choreography.** Round 5's lean-back (Glide -34, Dive -16) is replaced by nose-down Glide 40 and Dive 48 (`CaptureGlidePitch`, `CaptureDivePitch`).
- Log: `HC_COUNT_POSE pitch= target= mode= state= speed=`, once per leg (0.4 s after the lean starts). The capture logs `HC_COUNT_POSE pitch= capture=True phase=Glide|Dive`.

## 2. Dive speed
- **Live flight.** Chase flies at `DiveSpeed` 5.2 m/s with acceleration 16 m/s² everywhere he can fly (tight rooms included). Service doors and low lintels keep the 2.4 m/s crawl.
- Log: `HC_COUNT_DIVE close_s= from= to= speed=` when she first comes within GrabDistance (1.05 m). A dive that ends without reaching that range logs `HC_COUNT_DIVE close_s=none from= closest= reason=`.
- **Capture choreography.** The glide ends 7 m out (was 5.5 m). The dive takes 1.0 s at near-constant speed to the grab stand-off, about 4.6 m/s.
- Log: `HC_COUNT_DIVE close_s=1.00 from=7.00 to=<stand-off> capture=True grabMode=`.

## 3. Grab selection
Height means his origin above her feet, using the larger of the current height and the recent decaying approach height.
- At a hide entrance: GrabDrop. This applies to the dive entrance grab, the witnessed open and the near-miss open. The near-miss open was GrabCrawl in wave G.
- If she is crouched, or he comes in under 1.6 m: GrabCrawl.
- Mode 6 wing-wrap only within 1.3 m at a height of 2.2 m or less.
- Anything higher: GrabDrop.
- A missing clip falls through to the next choice, with Mode 6 last.
- The kill presentation stands him on her floor at the clip's stand-off, so he never hovers.
- Log: `HC_COUNT_GRAB mode= height= reason= ...`.
- **Capture climax.** GrabDrop (else GrabCrawl, else the old Mode 6 hold and lunge), planted on her floor at `StandOffFor(mode)`, so the clip's claws end 0.6 m short of the lens. Log: `HC_COUNT_CAPTURE grab mode= standOff=`.
- **Binding.** On the first active frame: `HC_COUNT_MODES bound=0:Perch=1 1:Glide=1 ... 8:GrabDrop=1 9:GrabCrawl=1 frames=[...]`. It is checked against the controller's clips and `Animator.HasState`. At every hunt end: `HC_COUNT_MODES frames=[...]` with the `HearthboundActor.ModeFrames` counts per mode, so Mode 8/9 frames > 0 proves the clips played.

## 4. No wall crossings
Every flight step, and the near-miss crawl step, is linecast 0.35 m above his origin before he moves. This is the same test the playtest driver counts, excluding himself and her.
- A door is opened and waited for (up to 2 s).
- Anything else is re-routed (`PlanRoute` to the same goal).
- If that fails he stops; after 3 s he lands and prowls.
- The near-miss crawl holds where it is.
- Logs: `HC_COUNT_LEG blocked=door|wall fix=wait|replan|stop|land|hold|hold-here hit= at= for= state=`.
- Both r10 crossings (`.playtest-fear.log` lines 142 and 157) were him passing through closing service doors: "Hall stair door" and "Library stair door".

## 5. Wing crumple
- The source is the Mode 6 **Grab** clip. In `tools/count_prepare.py` `grab()` (line ~414), `p.wings(..., stretch=2.2)` drives the fan Ext bones to 2.2x their rest reach at the clip's end (f=1). The receipt shows `membrane_area_ratio_last` 2.086, against Glide 1.25.
- **GrabDrop** also asks for 1.7 to 2.1 (`wstr`, line ~485). Its receipt shows 1,113 stretched edges and an edge stretch ratio of up to 88.3, which is likely the "rod-like limb" in V8. **GrabCrawl** shows 586 stretched edges, ratio 75.5.
- **Runtime fix.** In `LateUpdate`, no `WingFan*Ext` bone may move past `WingExtMaxStretch` (1.6) x its bind offset. Glide 1.45, Dive 1.26 and GrabCrawl 1.6 are untouched; Grab and GrabDrop are clamped. Log: `HC_COUNT_WING_CLAMP mode= stretch= clampedTo=`, once per mode.
- **For F5:**
  - Bring the Grab `stretch` (2.2) and GrabDrop `wstr` (up to 2.1) down to at most 1.6.
  - Check GrabDrop's stretched edges in its last frames (V8).
  - Once that's done, the clamp can be set to 0.

## 6. Hunt-start tells
- The "learned" glide no longer raises a Beat; it logs `HC_COUNT_LEARNED` only.
- CastleRun logs `HC_COUNT_BEAT_AUDIO id played=` only when a sound actually played, so beats with no sound are no longer reported as audio.
- `cc_hunt_hungry` is no longer spoken on the search tick. It is queued with a random 0.8-4.5 s delay and held until 5 s (`VoiceGap`) after the last line. Unless he has seen her, it is also held past the hunt's first 8 s, with a fresh random delay on each hold (`HC_COUNT_VOICE queued ... reason=`, `HC_COUNT_VOICE dropped ... reason=cooldown`).
- The re-hear and search interval is drawn from 8-14 s each time (was a fixed 10 s), so search times do not repeat either.

## 7. Dread
- "Seen" is +15 once per hunt (unchanged: `seenThisHunt`).
- "Grab survived" is +20 at most once per hunt. Later ones log `HC_COUNT_DREAD grab survived ... +0 reason=already counted this hunt`.
- **Everything the Count side adds to Dread (for F1's decay balance):**
  - `Presence`: `Rules.DreadSeen` (15) at most once per hunt, when she has him in view within SightRange.
  - `Presence`: `Rules.DreadGrabSurvived` (20) at most once per hunt. Triggered by a dive that came within 1.6 m and ended without a kill, a near-miss that did not open, or a witnessed open blocked by the driver.
  - `CastleRun.Sarcophagus()`: +25 per ledger page read aloud, at most 3 per run.
  - Nothing else in Presence or CastleRun. For reference, the other sources are ScareDirector's `DreadApparition` 12 and `DreadThrow` 8 (per throw or poltergeist).
- So per hunt the Count adds at most 35, down from up to 15 + 20 x (dives missed + near-misses).

## Hooks for the integrator
- None required. All changes are inside Presence and CastleRun.
- Optional: the playtest driver's `TrackHunt` wall test is what `HC_COUNT_LEG blocked=` mirrors. If you want them to agree to the frame, keep its 0.35 m height.

## What a windowed run must show (exact keys)
1. `HC_COUNT_MODES bound=` lists `8:GrabDrop=1 9:GrabCrawl=1`. After the hunts, `HC_COUNT_MODES frames=[...]` has non-zero entries at index 8 or 9.
2. **Recaptured H01-H10:**
   - `HC_COUNT_POSE pitch=40 capture=True phase=Glide` and `pitch=48 ... phase=Dive`.
   - `HC_COUNT_DIVE close_s=1.00 from=7.00 to=2.3x capture=True grabMode=8`.
   - H04-H07 show him pitched nose-down; H08-H10 show the GrabDrop on the floor with no Mode 6.
3. **Live hunts:**
   - Every `HC_COUNT_DIVE close_s=` that is a number is ≤ 2.0 when `from=` ≤ 7.
   - `HC_COUNT_POSE pitch=` ≥ 35 on every Travel or Chase leg.
   - `HC_COUNT_GRAB mode=8|9` on all but in-reach low wraps. No `mode=6` with `height=` > 2.2.
4. **Fear run:** `wallCrossings=0` in every `*_hunt_over` line. `HC_COUNT_LEG blocked=door fix=wait` appears at the stair doors instead.
5. `HC_COUNT_WING_CLAMP mode=6 stretch=2.20 clampedTo=1.60`, and no zig-zag in the recaptured wing tips.
6. No `HC_COUNT_BEAT_AUDIO` within 10 s of a `noTell=True` hunt start (learned is log-only).
7. Over at least 5 hunts, the `HC_COUNT_VOICE cc_hunt_hungry t=` times spread by ≥ 5 s, none before t=8 without a prior `seen=True`.
8. At most one `HC_COUNT_DREAD seen` and one `HC_COUNT_DREAD grab survived ... +20` per hunt.
