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

## hc/sword: Moonstone Sword and the three relic pickups
No Unity. `castle_verify.py` HC_VERIFY_PASS (76 assets), `check_source.py` PASS. Meshes in `tools/castle_props.py`, exports `Resources/Castle/{MoonstoneSword,RelicBlade,RelicHilt,RelicMoonstone}.fbx` (each under 1.7k tris; manifest `docs/haunted-castle/art/manifest_props.json`). Names are in `CastleArt.Names` (+ `All`): `MoonstoneSword`, `RelicBlade`, `RelicHilt`, `RelicMoonstone`.
Materials: `Castle_Silver` blade, `Castle_Iron` guard and pommel ball, `Castle_Oxblood` red leather grip (cord wraps), new `Castle_RuneGlow` (unlit `CastleEmissive`, HDR blue (0.25,0.60,1.6), the engraved rune line on both blade faces), new `Castle_Moonstone` (`CastleLit` shader + 1024 textures `Castle_Moonstone_{albedo,normal,mask}`, soft emission (0.10,0.17,0.38), no keyword needed). If either custom shader fails to compile, `CastleArt` falls back to a Standard material from `Specs`.

### Sizes and pivots (Blender space; X right, Y front, Z up; Unity front = +Z)
- `MoonstoneSword`: 1.10 m overall (tip z +0.925, pommel stone z -0.177). **Origin = centre of the grip**, blade along +Z, guard across X (0.28 wide), flat blade faces toward +-Y (rune line on both). Moonstone sphere r 0.0235 in the pommel at z -0.153.
- `RelicBlade`: 0.94 m along Y, 0.044 wide, lies flat, origin = centre, base on z 0. `RelicHilt`: 0.28 across X x 0.26 along Y, lies flat (crossguard across X, grip along Y, pommel socket empty with a dark floor), origin = centre, base z 0. `RelicMoonstone`: 0.21 x 0.19 x 0.19 uncut cluster, origin = base centre, z 0.

### Showing the sword in the investigator's hands after `Forge.Craft`
`Investigator.BuildInstruments` (shared file, piece 0) builds `heldTool` (mount at camera-local (0.28,-0.16,0.42), rot (8,-8,0)) with `instrumentModels[0..2]` from `FieldKitArt`, and `Investigator.Update` toggles them by `Tool`. Suggested change, no new input:
1. In `BuildInstruments`, after the instrument models: `var sword = CastleArt.Place(CastleArt.Names.MoonstoneSword, heldTool, Vector3.zero, Quaternion.identity)` is not suitable because it is a world prop; instead instantiate with `Instantiate(Resources.Load<GameObject>("Castle/MoonstoneSword"), camera)` and apply `CastleArt` materials via a small public `CastleArt.Dress(Transform)` (the private `ApplyMaterials`; make it public or call `CastleArt.Place(..., camera, ...)`, which already returns the object parented to the camera). Set its layer to 30 for all renderers (the equipment overlay camera), keep `shadowCastingMode` off.
2. Pose: grip centre at camera-local about (0.24, -0.20, 0.45), rotation `Quaternion.Euler(-62, 8, -14)` so the blade rises up and forward-left of the view and the guard is toward the viewer; scale 1. Idle bob = the existing `heldTool` sway; swing = lerp the rotation to `Euler(-10, 25, -40)` over 0.18 s and back when `CastleRun` fires a strike (the `player.FirePressed` branch at `CastleRun.cs:110`).
3. Visibility: `sword.SetActive(Game.Castle && Game.Castle.Forge.HasSword && Game.Case.Equipped && !Game.Modal && !Hidden)`; while it is active hide `heldTool` (the instrument) and put the left hand on the hilt or keep `leftHand` as is. The flashlight stays on its own mount.
4. Light: the rune line is unlit-emissive (reads in the dark); the blade is `Castle_Silver` lit by the existing overlay "soft field-kit bounce" light (cull mask layer 30).

### Where the relic pickups use the new meshes
`World.cs` (castle section, around line 205-232) creates the relic interactables: relic 0 is a trigger volume around `ReliquaryBox`, relics 1 and 2 are placeholder `brass` boxes `new Vector3(.3f,.16f,.3f)` at `relics[i] + up * .08`. Replace them with the art, keeping the interactable box as an invisible trigger: relic 0 `RelicBlade` lying on the reliquary box floor (inside the box volume, rotate yaw to lie along the box's long axis), relic 1 `RelicHilt` in the orrery planet socket (`orrery.TransformPoint(OrrerySocket)`, lay it with the grip along the socket's horizontal axis), relic 2 `RelicMoonstone` on the portrait-closet pedestal (`Art.Pedestal` top, origin is the base centre, so place at the pedestal top y). Use `CastleArt.Place(CastleArt.Names.RelicBlade|RelicHilt|RelicMoonstone, Root, position, yaw)`; when `CastleRun.TakeRelic` picks one up, disable that object (`item.gameObject.SetActive(false)` already hides the interactable; also `SetActive(false)` on the placed mesh). The moonstone's soft emission and the blade's rune line make all three read in dark rooms.
