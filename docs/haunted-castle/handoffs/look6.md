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

## hc/look6 (round 6): exposure convergence, planks, mossy rework, gore set
No Unity. `check_source.py` PASS (394 assertions, runtime + editor C#), `castle_verify.py` HC_VERIFY_PASS (72 assets, 0 fails). Exposure values are unmeasured: check the next capture against the 12-30% band.

### Root cause worth knowing: pixel-light limit
`ProjectSettings/QualitySettings.asset` is Ultra with `pixelLightCount: 4`, forward rendering. Each object gets only 4 per-pixel lights; the rest degrade to vertex/SH. My earlier warm fills (courtyard, balcony, crypt) competed with the candle lamps, torch and moons and lost: S6 stayed at 0.1% warm and "effectively the identical image" after round 5. Every fill light created by `CastleLightTuning` now has `renderMode = ForcePixel` (about 20 lights, short ranges, no shadows). If frame time suffers, drop to `Important` selectively.

### Exposure (castle-local, realtime unless noted)
- S1 (7.3% near-black, too bright) and hunt H06-H10 (8-12%): `FillSky` (0.010,0.014,0.024) and `FillGround` (0.008,0.010,0.018) (were 0.013,0.018,0.030 / 0.010,0.012,0.022); vault fill intensity 0.42 (was 0.60); candle intensity scale 0.9 (was 1.0).
- S2 chapel (35.3%, too dark): "Chapel sun-window moonlight" spot at (13.1,3.3,6.5) aimed -x and slightly down, angle 110, range 10, colour (0.42,0.56,0.90), intensity 1.1; two "Chapel altar candle fill" points at (12.5,1.5,4.8) and (12.5,1.5,8.2), colour (1.0,0.70,0.45), intensity 1.0, range 5; "Chapel nave candle fill" at (9.0,2.6,6.5), intensity 0.7, range 7.
- S3 crypt (4.2% near-black, 16.2% warm): crypt candle pools intensity 0.65 (was 1.6, -60%), range 4.5; violet rune glow (0.90) and red blood glow (1.2) kept; the shader fill is scaled to 0.45x while the camera is in the crypt (`CryptFillScale`, smoothly blended with the fog profile), so it gets real shadow and stays cold with warm pools.
- S4 exterior (wall luma 12.6, needs >=18): facade spot intensity 1.35 (was 0.95); new "Keep moon fill" spot at (0,18,-24) aimed at (0,10,4), angle 60, range 50, colour (0.40,0.55,0.90), intensity 1.2; new "Bridge moon fill" point at (0,2.5,-4.5), intensity 0.9, range 8 (cold pool on the planks). All three (facade, keep, bridge) are enabled only while the camera is outside the gate (castle-local z < 0.5).
- S5: "Balcony warm fill" intensity 1.4, range 8 at (-3.5,4.9,19.5) plus a second at (-5.2,4.8,22.5), intensity 0.98, range 7 (warm pools for the rose-window ledge view; the candelabras themselves are the dressing builder's). Moons: the sky is unchanged (pale 55 deg, red 42 deg); whether they show through the rose window is layout.
- S6: courtyard candle fills intensity 1.6 (was 1.0), range 8 (was 6.5). These are the lights that were losing the pixel-light race.

### Drawbridge planks
New material `Castle_OakPlank` (4 wide boards per metre, deep gaps, long grain, knots, nails at the board edges, weathering; mean (0.40,0.28,0.17), smoothness 0.30; textures `Castle_OakPlank_{albedo,normal,mask}.png`, `.mat`). The `Drawbridge.fbx` leaf and side beams now use it (re-exported only that FBX; 336 tris, manifest entry updated). Planks were also dark: the bridge now gets the cold bridge fill above. `CastleAtmosphere` upgrades `Castle_OakPlank` as a prop material. Dressing: use `Castle_OakPlank` for any other plank surface (scaffolds, gate, jetty).

### Castle_StoneMossy rework
Mask-driven now: a stone base (mean (0.42,0.45,0.48), so it sits in the same family as `Castle_StoneWeathered`) with moss (0.17,0.25,0.10) and damp slime only where moisture collects: joints, chipped bevels and cracks (crevice = 1 - AO), gated by sparse noise, plus a little growth at the foot of some courses. Block faces stay stone, so it no longer reads as a flat green panel. Smoothness rises only inside the moss. Rain streaks on `Castle_StoneWeathered`/mossy/the stain decal were running sideways (the anisotropic noise axes were swapped); they run vertically now.

### Castle_Gore set (for the flayed-demon Count)
Textures (1024, tileable, `Textures/<name>_albedo|normal|mask.png`) and Standard `.mat` files: `Castle_GoreSkin` (pallid grey-white, mean (0.60,0.56,0.55), blue-grey veins, split patches showing red, smoothness 0.38), `Castle_GoreFlesh` (exposed striated muscle with pale fascia, mean (0.42,0.06,0.06), smoothness 0.86-0.96 wet), `Castle_GoreMembrane` (wing membrane, mean (0.62,0.50,0.50), red vein web, smoothness 0.32). `Textures/Castle_GoreWet.png` (512 RGBA, dark-red blood with vertical drip runs; alpha = coverage). Shader `HollowCurrent/CastleGore` (UV-mapped, properties `_MainTex`, `_BumpMap`, `_MaskTex`, `_Smooth`, `_WetTex`, `_WetTile` (3), `_Wet` (0.6), `_WetSmooth` (0.92), `_Trans`, `_TransAmt`, `_TransPow`, `_EmissionColor`): blood overlay with wet specular, plus a cheap translucency glow at grazing angles, plus the global castle ambient fill.
API for the Count builder: `CastleArt.Gore(CastleArt.GoreKind.Skin|Flesh|Membrane)` returns a ready material; `CastleArt.Gore(kind, sourceMaterial, wet)` keeps the source's generated albedo and normal and adds the skin tint, blood overlay and translucency (`wet` 0..1; use 0.3-0.5 on the face, 0.8 on the chest/forearms, 0.2 on the wings with `GoreKind.Membrane`). Falls back to plain Standard if the shader fails to compile. The Count's own import/prefab code applies it; `CastleAtmosphere` does not touch it.
