# HANDOFF: piece 3, surreal props (`hc/props`)

Worktree: `/Users/midir/hollow-current-haunted-castle/.wt/props`. All files in `Assets/HollowCurrent/Runtime/Castle/` (namespace `HollowCurrent`). No references to Game, Investigation, Presence, Interactable or any other project type. Build and tests only; nothing was run in Unity or rendered.

## Verified
- `python3 tools/check_source.py` passes, but it globs `Runtime/*.cs` non-recursively, so it does NOT compile `Runtime/Castle/`. Verified with a copy of the script using `rglob` (and `INPUT_SYSTEM_REFERENCE` pointing at the main clone's `Library/ScriptAssemblies/Unity.InputSystem.dll`, since this fresh worktree has no `Library/`): both the player and editor assembly compiles pass, 123 rules assertions pass.
- Not verified: anything visual or in-engine (materials, trigger events, billboard facing, flame lean direction, texture look). Reasoned by inspection only.

## Shared conventions
- Plain MonoBehaviours, tunable public fields with tooltips, lazy init in `Start()` (so a factory or the layout can assign fields right after `AddComponent`), no per-frame allocation, shared meshes/textures/materials built once in `SurrealProps`/`SurrealAssets`.
- `SurrealProps.Viewer` (Transform, optional): billboards, light fade and "empty target" fallbacks follow it; if unset they use `Camera.main`. Piece 0 should set it to the player camera.
- `SurrealProps.GlowShader` (optional): override the Sprites/Default shader (e.g. an additive one from Resources).
- Billboard/glow materials use `Shader.Find("Sprites/Default")` (kept in builds via the project's sprites-default reference); lit props use `Standard`.

## API per script and how to place it (all factories: `parent` may be null, position is local to parent)
- `Floater`: bob/drift/tilt; `Transform flameTarget`, `Transform[] flames`, `bendRange` 12, `maxBendDegrees` 38; `static List<Floater> All`; `Floater.SetFlameTarget(Transform)` sets every floater (and later ones) in one call; `Bend01` readout. Place: `SurrealProps.MakeFloater(parent, pos, FloaterKind.Candelabra|ClosedBook|OpenBook, seed, lightedFlames=1)`. Candelabra = five candles, one real point light by default.
- `CandleFlame`: flicker, billboard (bottom pivot), optional point light faded out between 10 and 16 m from the viewer. `SurrealProps.MakeCandleFlame(parent, pos, height, withLight)`.
- `RuneRing`: `watcher` (null = camera), `watchRadius`, `Ignite()`, `Extinguish()`, `Ignited`, `Energy`. Ring lies in the XZ plane of its transform. `SurrealProps.MakeRuneRing(parent, pos, radius=5.5, runeCount=28, watcher)`. Great hall: radius about 5.5 at floor y.
- `PortraitEyes`: `target` (null = camera), `pupils[]`, `maxOffset`, `maxAngleDegrees` 22, `trackRange` 9. Portrait faces its +Z. `SurrealProps.MakePortraitEyes(parent, pos, rot, width=.9, height=1.3, target)` builds frame, painting and pupils.
- `StairLoop`: `bool Armed` (default false), `Transform destination`, optional `player` filter, `event Action Fired`, `LastYawDelta`, `LastPlayer`, `Arm()`, `Disarm()`, `ResetForNewRun()`, `HasFired`. Trigger is entered by any `CharacterController`; the move is disable, `SetPositionAndRotation`, enable, `Physics.SyncTransforms`. Position keeps its offset in the volume's frame, yaw is rotated by the volume-to-destination yaw difference. `SurrealProps.MakeStairLoop(parent, pos, size, destination, armed=false, rot)`; the volume is on layer 2 (Ignore Raycast) so interaction rays skip it.
- `KnifeOrbit`: `knives[]` (empty = children), `radius`, `degreesPerSecond` 14, jitter fields. `SurrealProps.MakeKnifeOrbit(parent, pos, knives=7, radius=.9, knifeScale=1.4)`.
- `GiantBat`: `Startle()`, `event Startled`, `IsScattered`, optional auto-startle (`startleRadius` + `watcher`). Bats move in world space, keep the parent at scale 1. `SurrealProps.MakeGiantBat(parent, pos, bats=6 (clamped 5-8), radius=6, height=3.5, wingspan=1.6)`.

## Requests for the integrator (piece 0)
1. `tools/check_source.py` line 37: change `.glob("*.cs")` to `.rglob("*.cs")` for `Runtime`, otherwise `Runtime/Castle/` is never compiled.
2. Wire at investigation start: `StairLoop.Armed = true` (or `ResetForNewRun()`), `SurrealProps.Viewer = player camera`, `RuneRing.watcher = player`; on hunter spawn: `Floater.SetFlameTarget(hunter.transform)`; for the fight: `RuneRing.Ignite()`; on a scare: `GiantBat.Startle()`.
3. Only `.cs.meta` files are committed for these scripts; the `Runtime/Castle` folder `.meta` is left for Unity or another piece so branches do not conflict.
4. Prefer Resources art from piece 2 later: the prop meshes here are small procedural stand-ins (candelabra, books, knife); replace by parenting real meshes under the factory results and disabling the generated renderer.

## Open issues / risks
- Visual quality is unchecked: rune glyphs, bat silhouette, painted portrait and flame texture are procedural and need a look in a capture.
- Billboard flames and bats use alpha-blended Sprites/Default, not additive; they will not glow through fog. Use `GlowShader` if the critic wants bloom-style flames.
- Flame lights: five lit candles per candelabra is avoided (one light each); with many floaters, count lights against the forward-rendering per-object limit.
- Rune glow is quantised to 12 shared materials per ring (batching), not continuous.
