# HANDOFF: fc-bw60 (Bellwether `-hc-verify` `performance_pass` flag)

Branch `hc/fc-bw60` (from `haunted-castle` 0bd9db5). Model: Sonnet 5.5. Date 2026-10-18. No Unity run (no Library, both GPU slots held); C# verified by `tools/check_source.py` only.

## Root cause (evidence: `docs/haunted-castle/rounds/r13/captures/fb2/bwverify/native-report.json`, `.playtest-fb-bwverify.log`)
`performance_pass = passed && ProfileOnly && timing.meets_60_fps_floor && uiTiming.frames_over_16ms==0` was False for three independent reasons. Any one of them is enough, so the flag could never be True in a normal verify run.

1. **`ProfileOnly` is only set by `-hc-profile`.** Normal `-hc-verify` (report: `captures_enabled true`, scope "full investigation and retry") never sets it, so the term is constant false. This was (a) in the brief and is the structural bug.
2. **The harness measured itself.** Every `ScreenCapture.CaptureScreenshot` readback stalls the next frame(s) and those frames were counted as gameplay. In the report 30 of the 90 slow frames (`frames_over_16ms` 90) fall inside the 30 `Capture ...` events of `route-events.txt` (one per capture, 21-28 ms). That is (b).
3. **Wall-clock strictness on a 90 fps paced, background window.** The game sets `Application.targetFrameRate=90`, vSync 0 (`Game.cs:109`), so the median frame is 11.1 ms (mean 89.3 fps, 1 % low 64.2 fps, min one-second window 86.1 fps). The other 60 slow frames are 16.7-18 ms (40 of them) and 21-22 ms: one limiter tick overshot, with the window under `open -g` on a machine shared with Unreal sessions (c)/(d). `FrameStatistics.meets_60_fps_floor` is "zero wall-clock frames over 16.667 ms", so 0.16 % of frames flip it. Wall-clock cannot tell "game cost > 16.7 ms" from "OS did not schedule the player for 17 ms". The 4 `ui_timing` frames (21-33 ms) are all the first frame after a note opens (`Note opens and pauses`) and are also counted.

`FrameStatistics` (Core) and the floor definition are unchanged. `Application.targetFrameRate`/vSync unchanged.

## What changed
- `Assets/HollowCurrent/Core/PerformanceVerdict.cs` (+ .meta): `CostStatistics` (engine cost series stats) and `PerformanceVerdict.Evaluate(routePassed, timing, ui, cost, out reason)`.
  - Engine cost available (>= 1000 samples): fail if any engine-cost frame > 16.667 ms, or wall-clock p99 > 16.667 ms, or 1 % low < 60 fps, or any one-second window < 60 fps. Wall-clock frames over 16.667 ms are then reported (count in the reason) but not fatal on their own.
  - Engine cost unavailable: the strict rule stays: zero wall-clock frames over 16.667 ms and zero UI frames over 16.667 ms.
  - Always: route must have passed, >= 3000 measured frames, no invalid samples.
- `Assets/HollowCurrent/Runtime/NativeVerification.cs`:
  - `ProfileOnly` removed from the formula; `performance_pass` now comes from `PerformanceVerdict`.
  - Engine cost per frame = max(`cpuMainThreadFrameTime`, `cpuRenderThreadFrameTime`, `gpuFrameTime`) from `FrameTimingManager`, unaffected by the OS not scheduling the window. Samples arrive ~4 frames late, so a sample is kept only if the current and the previous 8 frames were measured (not loading, not a capture window).
  - All `CaptureScreenshot` calls go through `Shot()`, which excludes the next 3 frames from wall-clock timing (counted, `excluded_harness_frames`) and blacks out engine cost for 12 frames.
  - Report gains `performance_reason`, `engine_cost` (samples, over_16ms, p50/p99/max, source, skipped), `engine_cost_slow_frames` (up to 64, with main/render/gpu ms and phase), `excluded_harness_frames`.
  - New log lines: at start `HC_PERF engine_cost_source=… target_fps=… vsync=…`; at the end `HC_PERF pass=… reason=… frames_over_16ms=… p99=… floor=60 frames=… engine_cost_over_16ms=… engine_cost_p99=… engine_cost_samples=… ui_frames_over_16ms=… harness_excluded_frames=…` (printed before `HC_NATIVE_ROUTE_PASS`).
- `ProjectSettings/ProjectSettings.asset` and `Editor/NativeBuild.cs`: `enableFrameTimingStats` 0 -> 1 (FrameTimingManager returns nothing without it; the player needs a rebuild).
- `Tests/RulesTests.cs`: `PerformanceVerdictTests` (clean passes; 10 jitter frames with clean engine cost pass and say so; same jitter without engine cost fails strict; one engine frame over 16.667 ms fails; sustained 25 ms drop fails; failed route / too few frames fail).

## Verified
`INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py`: PASS (rules tests including the new ones; player and editor compile). Not run in Unity.

## Integrator must run
1. Rebuild the player (`BuildPreparedMac` path in LOOP.md; the `enableFrameTimingStats` setting needs a fresh build). Bellwether scene/bake unchanged.
2. `Builds/macOS/Hollow Current.app -hc-verify` (same command as AUTO_RUN.md, Bellwether, via the GPU slot, windowed, `open -g`). Then check in the log:
   - `HC_PERF engine_cost_source=FrameTimingManager …` (if it says `unavailable`, Frame Timing Stats did not reach the build: the strict rule applies and the flag will stay False; report that, do not edit the floor).
   - `HC_PERF pass=True reason=ok engine_cost_clean wall_frames_over_16ms=N …` on a clean run, and `performance_pass: true` in `native-report.json`.
   - If it prints `pass=False`, the reason names the term (`engine_cost_over_16ms=…(max … ms)`, `wall_p99_ms`, `one_percent_low_fps`, `min_one_second_fps`) and `engine_cost_slow_frames` in the report gives main/render/gpu ms and phase for each offending frame. Take those as the real work list.
3. Optional sanity: run once with another heavy session active; a loaded machine should raise wall-clock jitter but not engine cost, and `performance_pass` should follow engine cost.

## Risks and unknowns
- FrameTimingManager semantics are unverified on Metal here: `cpuMainThreadFrameTime` might include limiter sleep (then cost reads ~11 ms and an overshoot shows as cost > 16.7 -> honest false-fail, no false pass), and `gpuFrameTime` can read 0 on some drivers (then CPU times alone are used). Frame timings can be absent; the code falls back to the strict rule.
- Wall-clock frames over 16.667 ms no longer fail the flag by themselves when engine cost is clean; that is the deliberate change. The sustained-rate terms (p99, 1 % low, one-second window) keep wall-clock honest. If the owner wants the old per-frame wall-clock floor back, it is the `else` branch in `PerformanceVerdict.Evaluate` (apply it always).
- The 4 UI frames at note-open (21-33 ms) are a possible real hitch (first note layout); they now count through engine cost if the engine sees them. Not investigated here.
- Capture windows are excluded from timing (~100 frames of ~37k). `-hc-profile` (no captures) remains the cleanest timing run.
