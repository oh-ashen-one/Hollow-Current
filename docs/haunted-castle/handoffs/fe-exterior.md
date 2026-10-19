# Handoff: fe-exterior (wave FE): S4 exterior frame time, moat glints, hall lantern-off knob

Branch `hc/fe-exterior` (from `haunted-castle` `408f406`), main clone, owns the engine. Builder: Opus 5.5, 2026-10-19.
Three bake+build+capture rounds (the budget): r1 `af7a58a`/`d3af2bf`, r2 `5ae7616`/`032f6b1`, r3 `3e34883`/`0a00c11`.
Castle-only code, so only the Castle was re-baked. Evidence (local, gitignored): `docs/haunted-castle/rounds/r13/captures/fe/{r0,r1,r2,r3}/`.
Logs: `.playtest-fe-r*-*.log`, `.castle-bake-fe{1,2,3}.log`, `.all-build-fe{1,2,3}.log`.

## Result (r3 = final build)

| Item | FD (408f406) | r3 | Band |
|---|---|---|---|
| S4 frame time (rooms_off) | mean 16.7, p99 23.1 | mean 14.3, p99 22.9 | **FAIL** (p99 <= 16.7) |
| S4 frame time (rooms_on) | mean 16.7, p99 23.1 | mean 14.6, p99 23.0 | **FAIL** |
| battlement_out | p99 16.5, max 20.4 | p99 11.8, max 17.6 | pass |
| gate_out_S6 | p99 11.9 | p99 11.8 | pass |
| S1 | p99 11.1-11.7 | p99 11.1 | pass |
| Moat bright px2, gate_out / S4 / battlement | 273 / 263 / 201 | 868 / 511 / 2077 | pass (>= 400) |
| Moat R/G | 3.64 / 2.42 / 3.43 | 2.91 / 2.60 / 3.01 | pass (>= 1.6) |
| Hall off: S1 / from_doors / skeleton_head / side_east | 30.4 / 32.6 / 31.8 / 30.4 | 30.2 / 32.4 / 31.9 / 30.3 | **FAIL** (35-55), see item 3 |
| Hall off: side_west | 52.1 | 52.1 | ok |
| Lantern on (all views) | as FD | within 0.3 of FD | unchanged |

## 1. S4 frame time: what costs, what changed, what is left

**Measured, not guessed.** `CaptureMode -hc-exterior-profile` switches one piece off at a time at the S4 eye and battlement_out and logs
`HC_CAPTURE EXTERIOR_PROFILE ... frames mean p99 main/render/gpu (FrameTimingManager) batches setpass draw_calls`. Every FRAMETIME line now
carries the same engine split. `HC_EXTERIOR_COST` (prepare and runtime) lists renderers, draws, tris, realtime lights and forward-add passes per piece.

- The frame is **render-thread bound**, not GPU bound: S4 render thread 11.6 ms, GPU 3.0 ms; 960x540 gave the same frame time as 1920x1080 (`fe/r0`).
- What fd-art added costs little: water, kit pieces, causeway, lamps, landing, braziers, trees, rocks, banks and fog each change S4 by
  under 0.3 ms and under 170 batches (prepare log: approach 170 renderers; the whole exterior is about 200 draws). Cheaper water would not
  move the frame (the water is 4 boxes, 48 tris, no texture; GPU 3 ms).
- The cost is the **interior seen from outside**: S4 drew 10,616 batches; with the 1,946 interior renderers hidden, 748 batches and every
  frame on the 11.1 ms cadence. They are behind the curtain wall (invisible) but in the frustum, and each takes forward-add passes from
  (a) the three ForcePixel outside fills (facade range 34, keep range 50, bridge 8) and (b) the interior's own realtime lights.
- Unity occlusion data (r1) did not help: 10,707 batches without it, 10,616 with it (the gate arch, door portals and window openings leak).
  It was dropped in r2. The bake-ambient knob was also dropped (see item 3).

**Changes kept (r3):** `CastleLightTuning.TagInterior` moves the deep interior's render-only renderers (no collider, off the outer faces, not in
the gatehouse/courtyard block |x| <= 6.5 z <= 14.5, not front roofs/wall walks) to layer 27 (`InteriorLayer`), which the outside fills do not light
(`HC_EXTERIOR_FILL_MASK moved=1037+54`). Cameras and every other light still render/light layer 27; physics is untouched.
r2 had a narrower rule that left the gatehouse doors black in S4 and the parapets dark in battlement_in; r3's zones fixed both
(r1 vs r3 frame diff: bridge_gate/keep/left 0 px, courtyard views <2k px, S4 only the water band).

**Still out of band and why:** r3 also masks the interior lights from layer 27 while the camera is outside and the gate chain is shut,
but the lookup used the label `Gate`, and the castle's "Gate" is the drawbridge, which has no door leaves (`HC_DEEP_INTERIOR_DOORS gate=0`),
so that mask never switched on. With it on, the profile's `off_lights_interior` variant gives the expected result: 4,125 batches, render
thread 4.7 ms, S4 mean 11.17, p99 11.69.

**Next step (one castle re-bake, one build, one rooms_off capture):** branch `hc/fe-exterior-next` (not merged, not built) keys the mask on
the gatehouse exits instead (`Gatehouse great doors`, `Gatehouse kitchen passage`, `Gatehouse guard door`, all shut). Verify:
`HC_DEEP_INTERIOR_DOORS gatehouse=... ` with a non-zero count, `HC_DEEP_INTERIOR_MASK on=True` before S4, `FRAMETIME S4_exterior_sky p99 <= 16.7`,
and a frame diff of the exterior set against `fe/r3/ext` (only the water should differ).

## 2. Moat glints (pass)

`CastleWater.shader`: a second glade for the blood moon at its own azimuth (same elevation cheat and behind-camera rule as the pale glade),
a wider pale glade (`_GladeRough` .26) with a weaker smooth lobe (`_GladeBody` .45) so the water between glints stays red, and glitter: small
round points (12 cells/m, 26 % lit, re-rolled 4 x/s) on chop facets that face each glade or lamp, in a halo 1.6 x the glade width. Castle-end
lamp glints .3 -> .6. r1's 10 cm square cells read as confetti; r2 made them points; the values come from an r2 `-hc-moat` sweep
(`CaptureMode -hc-moat "_Prop=value,..."` sets CastleWater properties for one capture). Frames: `fe/r3/ext/art_exterior_{gate_out_S6,battlement_out}.png`,
`fe/r3/rooms_off/S4_exterior_sky.png`. Risk: the pale glade's glitter is silver over a wide patch from the battlement; a critic may want it smaller.

## 3. Hall lantern-off (one knob, failed, stopped)

Knob: castle bake ambient (fc-light fallback row; `HallWindowMoonlight` was already measured as no-effect in integ-fc), applied as
`CastleLightTuning.BakeAmbientScale` in the castle-only bake hook. r1 at x0.5: every hall, chapel and crypt view moved 0.0-0.2 points, lantern
off and on (S1 30.4, from_doors 32.6, skeleton_head 31.9, side_east 30.3, side_west 52.4). The enclosed castle gets almost no sky ambient. Reverted to 1.

Why no single global knob can do it (from the FD frames' luma histograms): S1 needs +4.6 points but only 3.4 % of its pixels sit at luma 8-9,
so a uniform darkening must reach luma ~10.3 (about x0.58 linear); side_west has 8.8 % at 8-9 and would pass 55 long before S1 reaches 35.
The four failing views are lit by the near candelabras, the lit east piers and the wet-floor sheen streaks (side_east), which side_west lacks.
A fix needs a hall-local, east/near-camera change (floor sheen strength, the near floating candelabras), which is outside the fallback table.

## Verification (r3)

check_source PASS (1260 assertions, known CS0649 warnings only), check_castle_scares PASS, check_audio_keys OK, check_throwables_scene rc 0,
check_castle_kit OK (`.castle-bake-fe3.log` and scene), check_castle_doors OK. Prepare log: `HC_KIT_AXIS_SUMMARY problems=0`,
`HC_DOOR_LEAF_SUMMARY problems=0` (both stages), `HC_CANDLES floating=0 PASS`, `HC_SKELETON PASS`, `HC_HALL_AXIS PASS`, `HC_CHAPEL PASS`,
`HC_HIDE_SPOTS_SUMMARY failed=[]`, `HC_EXTERIOR_APPROACH ... braziers=2 lamps=4`, `HC_EXTERIOR_AXIS ok`, `HC_LIGHTING_BAKED map=Castle maps=8 probes=197 reflections=17`.
Captures: rooms_off/on, ext, layout_off/on (all 1920x1080, `open -g`, through the slot). Exposure: every judged view within 0.3 of FD.

## Risks

- Layer 27 is now used by the castle at runtime (unnamed in TagManager). Anything that later filters renderers by layer 0 must allow 27.
- The fill mask reads world bounds at Awake and again 7.5 s later; props spawned later stay on layer 0 (lit by the fills, as in FD).
- The bake commits carry the usual prepare churn (new `Mesh-50x.asset` files each prepare, as in FD's bakes).
