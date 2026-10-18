# HANDOFF: fc-bw60b (Bellwether `performance_pass`: subtract Present wait from main-thread engine cost)

Branch `hc/fc-bw60b` (from `haunted-castle` e83d33a). Model: Sonnet 5.5. Date 2026-10-18. No Unity run (engine owned elsewhere); C# verified by `tools/check_source.py` only (PASS, rules tests included).

## Diagnosis (evidence: wave FC `-hc-verify`, `.playtest-fc-fc2-bwverify.log`, `docs/haunted-castle/rounds/r13/captures/fc/fc2/bwverify/native-report.json` in the main clone)
Setup: `targetFrameRate=90`, vSync 0, background `open -g` window. Wall clock: p50 11.1 ms, p99 11.9, mean 89.2 fps. `engine_cost`: 36988 samples, p50 10.5, p99 11.7, **104 over 16.667 ms**, max 84.3. The report stores the first 64 of the 104 (`engine_cost_slow_frames`); histogram of those by what bound them:

main thread (`cpu_main_ms`, 53 of 64 frames; render thread <= 7.3 ms and GPU <= 5.1 ms on all of these)
```
16.0-16.99  20  ####################   (16.7 x9, 16.8 x4, 16.9 x7)
17.0-17.99  22  ######################  (17.0-17.7)
18.0-18.99   3  ###
21.0-21.99   5  #####   (21.1-21.6)
22.0-22.99   2  ##
32.0         1  #
```
- 45 of the 53 main-thread frames sit in 16.7-18.5 ms, and 39 within 16.7 +- 1.0 ms: a display-rate signature (one 60 Hz tick), not a spread like real cost. The 21-22 ms group (7) is a tick plus a partial second wait. Render thread and GPU on those same frames are 1-7 ms, so nothing was busy for 16.7 ms: the main thread was blocked. Confirmed the director's hypothesis for the main-thread population.
- 11 further frames are not main-thread at all: `cpu_main_ms` 8-11.5 but `gpu_ms` 16.7-19.4, all in Hunt/Recovery/Disturbance (samples 19680-24852). Subtracting Present wait does NOT touch these. See "Unresolved" below.

## API check
Unity 6000.3.20f1 `UnityEngine.CoreModule.xml` documents `FrameTiming.cpuMainThreadPresentWaitTime` ("CPU time the last frame spent waiting for Present on the main thread, in ms"), `cpuMainThreadFrameTime` ("total time between start of the frame and when the main thread finished the job"), `cpuTimePresentCalled` and `cpuTimeFrameComplete` (both clock timestamps, unit not documented), `syncInterval`. So the field exists and the proposed subtraction is the documented-consistent fix.

## What changed
- `Assets/HollowCurrent/Core/PerformanceVerdict.cs`: `PerformanceVerdict.EngineCostMs(main, presentWait, render, gpu)` = max(max(main - presentWait, 0), render, gpu); negative/NaN wait subtracts nothing. `CostStatistics.Measure(..., presentWaits)` adds `present_wait_mean_ms`, `present_wait_max_ms`, `present_wait_frames` (frames with wait > 0). The 16.667 ms per-frame rule, p99, 1 % low and one-second-window terms are untouched.
- `Assets/HollowCurrent/Runtime/NativeVerification.cs`: `PollEngineCost` reads `cpuMainThreadPresentWaitTime` and uses `EngineCostMs`. `engine_cost_slow_frames` entries now carry `present_wait_ms`, `cpu_main_minus_wait_ms` and `bound_by` (`main_thread`/`render_thread`/`gpu`). `HC_PERF` end line gains `present_wait_mean_ms= present_wait_max_ms= present_wait_frames=`. Source string now reads `FrameTimingManager max(cpuMainThread - cpuMainThreadPresentWait, cpuRenderThread, gpu)`.
- `Tests/RulesTests.cs` (`PerformanceVerdictTests`): subtraction (16.9 - 7 -> 9.9), wait-only main thread costs its render figure, clamp at 0, negative/NaN wait, GPU/render never reduced, real busy 19.6 ms still fails; series test: frames that only waited pass, the same frames with no reported wait fail (`engine_cost_over_16ms=100`), and a real 17 ms of work after the wait still fails; mean/max/frames reported.

Decision on "exclude frames stalled on present": not done. `cpuTimePresentCalled`/`cpuTimeFrameComplete` have no documented unit (timestamps), the subtraction already removes the wait in-place, and dropping whole frames could hide real stalls. Documented, not implemented.

## Integrator must run
1. Re-bake Bellwether (unchanged inputs but the standard pre-build step) and **rebuild the player** (non-castle `.cs` changed).
2. `Builds/macOS/Hollow Current.app -hc-verify` via the GPU slot, windowed, `open -g` (AUTO_RUN.md).
3. Expected `HC_PERF` start line unchanged: `HC_PERF engine_cost_source=FrameTimingManager target_fps=90 vsync=0`. Expected end line shape:
   `HC_PERF pass=<bool> reason=... frames_over_16ms=~88 p99=~11.9 floor=60 frames=~37000 engine_cost_over_16ms=<N> engine_cost_p99=~11 engine_cost_samples=~37000 present_wait_mean_ms=<a few ms> present_wait_max_ms=~17-25 present_wait_frames=<thousands> ui_frames_over_16ms=<n> harness_excluded_frames=~86`.
   - If `present_wait_max_ms=0.00 present_wait_frames=0`: the Metal player does not populate `cpuMainThreadPresentWaitTime`; nothing was subtracted and the flag will behave as before. Report it; the next step would be an alternative (frameStartTimestamp -> cpuTimePresentCalled delta once the timestamp unit is measured), do not relax the floor.
   - `engine_cost_slow_frames[].bound_by` shows what any remaining over-budget frame is bound by.

## Unresolved / risks
- The 11 GPU-bound frames (`gpu_ms` 16.7-19.4, Hunt/Recovery) will still fail the flag. They cluster just above 16.7 ms and the GPU was shared with Unreal sessions at the time, so they may be GPU contention or compositor/drawable pacing rather than game cost, but that is not provable from this report. If the re-run still fails on `bound_by=gpu` frames: run once with no other GPU session active (via the slot) before treating it as a real Hunt-phase GPU cost; if it persists, it is genuine work for the fog/light budget, not a measurement fix.
- 40 of the 104 over-budget frames are not in the stored 64 (including the 84.3 ms max); the re-run's list is again the first 64. If the count after the fix is small, the full set is inspectable; if large, raise the 64 cap.
- Subtracting a wait that is really caused by a slow GPU is safe: the GPU term (`gpuFrameTime`) is kept in full, so GPU-bound frames still count.
