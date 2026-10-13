# HANDOFF: piece 0 (integrator), Haunted Castle map plumbing

Branch `haunted-castle` in `/Users/midir/hollow-current-haunted-castle` (main clone, pushed to origin). Model: Opus 5.5. Date 2026-10-13.
Merged pieces: hc/forge `ec2c7fa`, hc/props `6d24e03`, hc/audio `d1ddd9b`, hc/count `2a8c23a`, hc/layout `136b002`, hc/art `714ffe2` (round 2). Each piece's own handoff is archived in `docs/haunted-castle/handoffs/<piece>.md`.

## Verified (how)
- `python3 tools/check_source.py`: PASS 388 rules assertions; runtime C# compiles against the macOS player and editor assemblies (now recursive: `Runtime/Castle/` is compiled).
- Unity 6000.3.20f1, one instance at a time, every run through `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-integrator` (logs `/Users/midir/hollow-current-haunted-castle/.*.log`, gitignored):
  - `CountClaudeImport.Prepare` (nographics): HC_COUNTCLAUDE_PASS, 6 clips, 28 bones, prefab `Resources/Characters/CountClaude.prefab`.
  - `NativeBuild.ValidateCastleGeometry` (nographics): HC_CASTLE_GEOMETRY_PASS, 9 contract points reachable from the gatehouse (crypt, reliquary, tower orrery, portrait closet), HC_CASTLE_HUNTER_PASS.
  - `HouseLighting.PrepareAndBakeCastle` (graphics, batchmode): 4 lightmaps, 188 probes, 17 reflections; receipt `Art/LayoutData/CastleBake.json`.
  - `NativeBuild.BuildPreparedMacCastle`: Succeeded, `Builds/macOS-Castle/Hollow Current Castle.app`. Headless player boot (`-batchmode -nographics`, then SIGTERM): HC_AUDIO_READY 75 clips, HC_READY, HC_STARTUP_READY, no exceptions; only `HC_MISSING_ART speech voice_cc_arrival`.
  - Bellwether receipt was stale from the shared-source changes (BuildPreparedMac refused, loud message). Re-ran `PrepareAndBake`: same counts as shipped (4 / 124 / 9). `BuildPreparedMac` passed all geometry checks and built; headless boot HC_STARTUP_READY, 49 clips.
  - `NativeBuild.BuildPreparedMacAll`: both maps in `Builds/macOS/Hollow Current.app` (Succeeded, ~899 MB).
- Not verified: anything rendered or played (no captures, no input-driven run of the picker, map switch, forge, fight, stair loop or drawbridge). The critic/playtest owns that.

## What exists (signatures other pieces code against)
- `Core/MapId.cs`: `enum MapId { Bellwether, Castle }`; `MapCatalog.Get(MapId)` -> `MapInfo{SceneName, ScenePath, BakeReceiptPath, LightingSettingsPath, LayoutFolder, DisplayName, HunterName}`; `TryParse`, `TryFromSceneName`, `FromPreference`; PlayerPrefs key `hc.map`.
- `Core/CaseBook.cs`: every per-map word (evidence names/ids, instrument names, note titles/ids/art, journal choices, dispatch/goal text, kill ending, relic text). `CaseBook.For(map)`. Correct journal answer uses the same enum slots for both maps (chapel bell = `AnchorKind.MusicBox`). Castle note titles `Ledger: the steward / the astronomer / the painter` with ids `cc_ledger_steward/orrery/portrait` (= `LedgerHints`).
- `Core/Investigation.cs`: `new Investigation(CaseBook)`; `WinByKill()`, `HunterSlain`.
- `Runtime/World.cs`: `Build(MapId)`, `LoadOrBuild(MapId)`, `Map`, instance `Spawn`, `HunterSpawn`, `AnchorFloor`, `PerchPoints`, `IsInside/IsSafe`, `CheckpointPosition`, `StepSurface`, `NavBounds`, `Castle` (`World.CastleSpots`, serializable copy of `CastleWorld.Points`), `CastleRoutePoints()`. Castle: CastleWorld's own NavMesh is removed and rebuilt by `World.BuildNavigation` (castle skips trigger colliders, includes MeshColliders); missing kit/winch/bell/brazier/altar/relic/ledger interactables are added as fallbacks.
- `Runtime/HunterProfile.cs`: plain class, defaults = Split Grin; `SplitGrinProfile`; `HunterProfile.For(map)`; virtual `OnStagger(Presence)`, `OnDeath(Presence)`. `Presence.Profile`, `Presence.Stagger()`, `Presence.Defeat()`.
- `Runtime/CastleRun.cs` (castle only, `Game.Castle`): owns `Forge`, `Fight`; altar hold -> `Forge.Tick` (Craft only via tick), strike = instrument button within 3.2 m / 55 deg during a hunt, `Fight.Killed` -> `Defeat` + `WinByKill`; props on CastleWorld anchors; AUDIO.md loops/one-shots.
- `Runtime/MissingArt.cs`: castle-only magenta/silent placeholders (`MissingArt.Placeholders`), logged as `HC_MISSING_ART`.
- `InteractKind` gained `Relic`, `Altar` (appended); `Interactable.RelicIndex`.
- Editor entry points and receipt policy: README "Maps" table.

## What each piece does next
- Piece 2 (hc/art): merge takes your `Runtime/Castle/CastleArt.cs` over the layout stub (same `Place(string,Transform,Vector3,Quaternion)`). Commit `.meta` files for new assets (`CastleArt.cs.meta` already exists on haunted-castle: keep guid `git show haunted-castle:Assets/HollowCurrent/Runtime/Castle/CastleArt.cs.meta` or expect a meta conflict). Optional textures: `Resources/Castle/ClawScores`, `Resources/Castle/Ledger{Steward,Astronomer,Painter}`, `Resources/UI/BloodLedger/{Dialogue,Warning,Panel}` (currently magenta).
- Piece 4b (Count behaviour): edit `Runtime/Castle/CountClaudeProfile.cs` (now a `HunterProfile` subclass with your values) and override `OnStagger/OnDeath`; perch/glide/dive needs new hooks in `Presence.cs` (piece 0 file): send requests. Read `Game.Castle.Fight` for `SpeedMultiplier/Retreating`.
- Piece 6: add `voice_cc_arrival`, `voice_cc_kit`, `voice_cc_warning`, `voice_cc_return`, `voice_cc_contain`, `voice_cc_ending`, `voice_cc_hint_signal/trace/voice` (castle `Speak(id)` looks up `voice_cc_<id>`; `cc_*` ids as given).
- Piece 1: notes you place are extra flavour unless titled as in `CaseBook.Castle.NoteTitles`; fallbacks currently sit near Compass/Voice/Altar points.
- Every builder: after a shared-file change both receipts go stale; re-run the map's `PrepareAndBake*` before a prepared build.

## Round 2 (integrator)
- Relic gating: silver blade = the reliquary box (trigger around the box art), shut until the grave-ash lantern is held on its claw-scores for 1 s; hilt at the orrery asset's socket (manifest notes.socket, assumes Blender x,y,z -> Unity x,z,y); moonstone on the pedestal behind the portrait door. Ledger pages speak the matching relic hint.
- Far-bank escape: after the bell seal, once the drawbridge has lowered, entering `DrawbridgeTrigger` = `FinishAtVan`.
- Doors: with the 1.5 m probe 17/23 castle doors could not open (0 degrees) and the rest stopped at 55. `Door.FitProbe` + hinge moved to the clearer wall face: all 23 open 80-175 degrees (`ValidateCastleGeometry` threshold 75 for the castle; Bellwether unchanged at 85).
- Count sources moved to `Art/Characters/CountClaude`; castle `Speak("arrival")` -> `voice_cc_night` (also warning -> `cc_hunt_hungry`, return -> `cc_stay`).
- Sky + moons wired (`CastleArt.ApplySkybox`, moons along `Pale/RedMoonDirection`, baked). `ImportYaw` left at 0.
- Capture mode `-hc-capture <dir>` (`Runtime/Castle/CaptureMode.cs`); round 1 notes `docs/haunted-castle/rounds/r1/NOTES.md`.
- Builds: `Builds/macOS-Castle/Hollow Current Castle.app`, `Builds/macOS/Hollow Current.app` (both maps); both boot headless.

## Open issues
- `tools/audit_project.py` fails on its first, pre-existing check (`com.unity.inputsystem` is not a built-in package); the new scene checks pass when that loop is skipped.
- `CastleDrawbridge` moved from `CastleWorld.cs` to its own file (Unity scene-serialization requirement); hc/layout edits must follow.
- Relic gating, the far-bank escape and the forge/fight are compiled and engine-validated for reachability only; not input-playtested.
- No Blood Ledger UI art (`Resources/UI/BloodLedger/*`) or ledger page art: magenta placeholders in the castle UI.
- `GiantSkeleton` and other extra art (`BanquetTable`, `Banner`, ...) are not placed by the layout.
- Not yet run: any rendered capture or input playtest of either map.

## hc/look3 (round 3): contrast, fog, warm candles, higher moons
No Unity; `check_source.py` PASS (394 assertions, runtime + editor C#). Not measured visually: the critic's targets (10-25% of pixels below luma 8 in S1/S3/S5, min luma <=4, warm share >=5%) are what to check in the next capture.

### Changes
- **Black lift removed** (code and fields deleted from CastleAtmosphere). Vignette kept, `VignetteStrength` 0.22.
- **Lower fill/ambient so shadows can reach black.** AmbientSky (0.16,0.20,0.30), AmbientEquator (0.095,0.12,0.185), AmbientGround (0.04,0.04,0.06) gamma; shader fill (linear) Sky (0.016,0.022,0.036), Ground (0.004,0.004,0.008) (was 0.050/0.068/0.110 and 0.014/0.014/0.024). Stone world tint 0.78/0.78/0.82, wall 0.64/0.64/0.70 (less flat brightness), cobble 0.70, earth 0.40.
- **Fog:** exponential squared, colour (0.30,0.37,0.52) (lighter than the walls so distance dissolves into mist), density 0.050 (12 m keeps 70%, 25 m 21%, 28 m 12%).
- **Ground fog** (`GroundFog`): 6 alpha-blended noise layers per area at y = 0.08 ... 1.1 m above the great-hall floor (centre (0,0,21), 12x14 m) and the crypt floor (centre (0,-3.2,21), 12x14 m), colour (0.34,0.42,0.58), layer alpha 0.16 x (1.25 at the floor to 0.6 at 1.1 m), each layer scrolling at its own speed, faded out within 0.9-2.8 m of the camera, soft plane edges. New `CastleFogLayer.shader` + `Castle_FogNoise.png`; if the shader fails to compile the layers are skipped with a warning and exponential fog remains.
- **Floors:** cube "Slab / x" objects (floors and ceilings) now use flagstone instead of the wall coursing (the S1 floor read as brick). Flagstone smoothness raised: dry slab 0.42, puddled patches up to ~0.92 (was 0.30 / 0.80), more wet area.
- **Candle flames** (`Castle_Flame.mat`): albedo (0.10,0.04,0.01), emission (1.15,0.42,0.07); was (1,0.62,0.22)/(2.4,1.2,0.35) which clipped to the white cards in the critic's S1/S5. Flame shape/bend is still piece 3.
- **`CastleLightTuning.cs`** (new MonoBehaviour; add next to CastleAtmosphere): by name. "Candle light": colour (1.0,0.62,0.30), intensity x2.4 (0.6-1.1 becomes 1.4-2.6), range x1.3 clamped 6.5-10 m. "Window moonlight": colour (0.36,0.51,0.75), intensity crypt 0.17 -> 0.43, floor windows 0.36 -> 0.80, rose 0.50 -> 1.10. "Castle moonlight" 0.22 -> 0.16, "Castle second moon" colour (0.62,0.24,0.34) 0.06 -> 0.05. Lights are not renamed (World.cs looks up "Castle second moon").
- **Sky:** pale moon elevation 27 -> **55 deg** (azimuth -34), blood moon 24 -> **42 deg** (azimuth +42); six 2048 faces regenerated; `CastleArt.PaleMoonDirection` = Direction(-34,55), `RedMoonDirection` = Direction(42,42) (World.cs and CaptureMode already read these, so the directional moons and the S4 check follow). Unit directions: pale (-0.4623,0.8192,0.3433)... computed from azimuth from +Z towards +X: (sin az cos el, sin el, cos az cos el). The window moon sprites hc/dressing placed should be re-aimed along these (pale moon now high above-left of the gate, red high right), or removed if the sky shows through the windows.

### Requests for the integrator
1. Add `CastleLightTuning` to the castle scene next to `CastleAtmosphere`. Candle changes are realtime and need no re-bake. The window spots and both directional moons are Baked, so their new intensities only appear after a re-bake: call `CastleLightTuning.Tune(layoutRoot)` in `HouseLighting.Bake` (MapId.Castle) before baking, and the moon directions change with the new `CastleArt` constants.
2. **Bake ambient for MapId.Castle** (replaces (0.08,0.105,0.14) / (0.035,0.045,0.055) / (0.018,0.02,0.023)): sky **(0.050,0.065,0.100)**, equator **(0.022,0.028,0.040)**, ground **(0.010,0.012,0.016)**.
3. Re-bake the castle (sky faces, `.cs`/`.shader` files and light values changed).
