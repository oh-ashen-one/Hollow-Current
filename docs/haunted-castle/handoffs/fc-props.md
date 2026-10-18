# HANDOFF: piece fc-props (`hc/fc-props`, worktree `.wt/fc-props`)

Wave FC, from `haunted-castle` 0bd9db5. Model: Sonnet 5.5. Date 2026-10-18. Castle only; Bellwether untouched. Unity was **not** launched (no Library in the worktree, GPU slots held elsewhere); everything is verified statically plus an offline geometry check, with the recipe for the integrator at the end.

## A. Chapel bell and brazier overlap (fb-layout "Observation, NOT changed")

- **Cause.** Bell footprint 0.9 x 1.2 m (art 1.2 x 0.9 at yaw -90) centred (12.4, 11.2) = x 11.95..12.85, z 10.6..11.8; brazier (12.6, 11.2) with a 0.76 m bowl = x 12.22..12.98, z 10.82..11.58. They intersected by about 0.6 m in x and fully in z.
- **Fix (brazier moved, bell untouched).** `CastleLayout.BrazierPos` = (12.3, 0, 9.7): x 11.92..12.68, z 9.32..10.08. Clear floor to the bell 0.52 m (z), to the high altar step end (z 9.0) 0.32 m, east of the pew block (pews end x 10.75; checked against 10.95 to cover a fallen pew), so the pew-to-north-wall passage (z 10.13..12.7 between x 6.3 and 10.75) and the 2.4+ m side passages keep their widths. The brazier still reads in the nave: the S2 eye (7, 1.64, 6.5) looking +x now sees it right of the altar steps instead of behind the bell. The bell keeps its name `Castle / ChapelBell`, its position (`AnchorLocal` 12.4, 1.0, 11.2) and its rest rotation, so `CastleScares.cs:638` (bell swing), the `Anchor` interactable and the proofs (`Find(InteractKind.Anchor/Breaker)`) are unchanged. The floating-candle exclusion follows `BrazierPos`.
- **Regression check.** `CastleWorld.LogChapelProps()` (called right after `ChapelBrazier()`) logs at prepare time, an error on failure:
  `HC_CHAPEL_PROPS bell=[x 11.95..12.85 z 10.60..11.80] brazier=[x 11.92..12.68 z 9.32..10.08] overlap=False gap=0.52 (min 0.3) passages_clear=True PASS`
  It asserts: footprints disjoint, clear gap >= `CastleLayout.ChapelPropMinGap` 0.3 m, both east of the pew block, inside the room, and the brazier off the altar steps.

## B. Period instrument meshes (r12 critic lines ~161, ~237)

All three castle held instruments, not only the two the critic named (CaseBook calls them compass / grave-ash lantern / ear-trumpet; tool 2, the UV lamp, was the same modern-prop problem):

| Tool | Castle name | Was | Now |
|---|---|---|---|
| 1 | Hollow Compass | LED induction reader + a red cube needle | brass-and-glass compass case, bezel, suspension ring; parchment dial; glass glare disc; red-tipped needle with a bone tail that **trembles** (0.5 + 0.9 x reading degrees); five ember pips on the dial show the 0..5 reading. The LCD TextMesh is kept as data (`InductionDisplay`, used by the HUD text) but its renderer is disabled in the castle. |
| 2 | Grave-ash lantern | UV lamp mesh | brass cage lantern: foot, cap, bail ring, six iron bars, violet glass chimney round a glowing core (the UV spot light itself is unchanged) |
| 3 | Vellum Ear-Trumpet | cassette recorder | flared brass horn with a vellum wrap (runtime-painted texture), three leather bindings, dark bell interior |

- **Code.** New `Runtime/Castle/CastleInstrumentArt.cs` (+ `.meta`): a small procedural mesh builder (lathes, tube sweeps, flat facets; triangle winding derived from vertex normals) and the three recipes, `Create(index, parent, out CompassParts)`. `Investigator.BuildInstruments` uses it when `Game.Map==Castle` and `FieldKitArt.Create` otherwise; hand mount transform, `instrumentModels[]` indices, interaction and reading behaviour are unchanged. `UpdateNeedle` adds the tremble and lights the pips (castle only; `compassParts` is null in Bellwether). The old `instrumentModels[1].localRotation = 180` (UV lamp orientation) is now applied to Bellwether only. `HeldAssetName` still returns `InductionReader/UVLamp/EVPRecorder` (the role keys), so nothing keyed on it changes.
- **Bellwether.** No code path changed for it (`period` false); `NativeVerification.cs:336` (`HeldAssetName=="EVPRecorder"`, > 1000 vertices) is on the unchanged branch.
- **Assets.** None: meshes generated at runtime, the dial / vellum / glare textures painted at runtime from an integer hash (no files, so no `tools/` generator), `Standard` materials plus the castle's always-included `CastleEmissive` (glow) and `CastleSprite` (glass) shaders (Standard `_EMISSION` and alpha keywords are deliberately avoided: they can be stripped from a player build, see `CastleArt.FlameShaderOk`). Logged in `ASSET_LOG.md`.
- **Log.** Once at build time, an error if any count is 0 or > 3000: `HC_INSTRUMENTS map=castle compass=1277 trumpet=1026 period=True lantern=1150 (max 3000)`. The numbers above were measured offline by compiling `CastleInstrumentArt.cs` with Unity's Roslyn and running the geometry recipes under Unity's Mono (`ToObj`), so the player log should print exactly these.
- **Capture view.** `-hc-capture-art instruments` (new set in `CaptureMode.ArtRun`): four views from the chapel nave (eye (8, 1.64, 6.5) looking down the aisle): `art_instruments_held_compass_idle`, `..._held_compass_reading5` (needle swung to about 100 degrees, all five pips lit), `..._held_lantern`, `..._held_trumpet`. `Investigator.CaptureHeld(tool, level)` poses the held item by hand because the capture disables the player's Update (it logs `HC_CAPTURE_HELD tool= asset= verts= level=`). Use `-hc-capture-lantern on` for a lit nave.

## C. ASSET_LOG cleanup (HANDOFF open item 5)

- `docs/haunted-castle/ASSET_LOG.md`: the nine retired clips (`hunt_hungry, dispatch, night, kit, ending, contain, hint_signal, hint_trace, hint_voice`) left the Kokoro inventory table and are kept verbatim in a new subsection "Retired 2026-10-15 (wave FB, fb-voice2): nine `voice_cc_*` clips"; the eSpeak recipe rows and the wave K stats rows are struck through; the "x22" line says 13 remain (the 13 files on disk).
- `tools/audio_castle_sfx.py`: `VOICES` is empty, the seven it used to own are in `RETIRED_VOICES` (doc string updated). `--check` prints no `MISSING` row (was 7). Only `--check` was run; the script was never run without arguments. `python3 tools/check_audio_keys.py`: OK.

## How verified

- `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py`: PASS (runtime + editor compile; only the known CS0649 warnings). `tools/check_castle_scares.py` PASS. `tools/check_audio_keys.py` OK. `tools/check_throwables_scene.py` exit 0. `python3 tools/audio_castle_sfx.py --check`: 0 MISSING.
- Offline geometry check (not committed, scratch only): built every recipe under Unity's Mono and imported the OBJs into headless Blender with back-face culling on: no inside-out faces (every triangle's winding agrees with its vertex normal), and the compass, lantern and trumpet read as intended (silhouettes checked in renders). Vertex counts as above.
- NOT verified (needs the engine): how the glove sits on each new item, the real lighting of brass/vellum in the dark castle, the dial texture and glass glare as rendered, the tremble feel.

## What the integrator must run

1. Prepare + bake the castle (`HouseLighting.PrepareAndBakeCastle`): read the log for `HC_CHAPEL ... PASS` and the new `HC_CHAPEL_PROPS ... overlap=False ... PASS` (brazier moved, so re-bake lighting: the brazier flame is a sprite, no baked light, but the bake receipts' source hash changes anyway). `check_throwables_scene.py` after Prepare.
2. Player build, then `-hc-capture <dir> -hc-capture-art layout [-hc-capture-lantern on]`: look at `art_layout_chapel_aisle`, `..._chapel_altar_approach`, `..._chapel_from_great_doors` (brazier right of the altar steps, bell alone in the corner behind it); add `-hc-capture-art instruments` and check the four held views plus `HC_INSTRUMENTS` (build log) and `HC_CAPTURE_HELD` lines.
3. Bellwether: `-hc-verify` unchanged (must still pass, `HeldAssetName` / vertex-count requires on the Tripo meshes).
4. Playtest proofs that use the bell/brazier (`Find(InteractKind.Anchor)`, `Find(InteractKind.Breaker)`) unchanged; confirm the brazier is still reachable from the aisle (it is about 1.6 m from the aisle edge).

## Risks

- The held items are untested against the glove rig: the compass sits at the mount origin lifted 12 mm, the lantern offset (+3 cm, +8 mm), the trumpet's grip point near (0.03, -0.04, 0). If a finger clips through, nudge the `visual.localPosition` lines in `BuildCompass/BuildLantern` or the `p0..p2` points in `TrumpetGeometry`.
- The dial reads only if the glare disc is not too white; tune `GlareTexture` alpha (0.05 veil) or drop the glass submesh if it washes the dial.
- Flashlight (left hand) and field radio are still the Tripo meshes; the critic named only the instruments, so they were left as is. If the owner wants the radio or the flashlight period too, they use the same builder (`Mb.Lathe/Sweep`).
- The brazier now stands about 2 m closer to the nave; its flame sprite is visible from the S2 eye and the "lit" candle count in S2 is unchanged (candle exclusion follows the new position).
