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

---

# Round 7 (hc/arch7): lean moved into the shader, dressing readable, pier overlap, chandelier

Not seen in an engine; `check_source.py` PASS. Shader changes are not compile-checked here (no Unity): `Resources/Castle/Shaders/CastleFlame.shader` was rewritten with GPU instancing and per-instance properties; if it fails to compile, `UnlitAdditive` falls back to the sprite shader and the log line `HC_FLAME_CHECK ... lean shader property _LeanAmount MISSING` says so.

## 1. Candle lean in the flame shader
- `CastleFlame.shader` has `_LeanDir` (world direction to the Count, w = side hint) and `_LeanAmount` (degrees) as instanced per-renderer properties. In the vertex stage each vertex moves along the camera-right of the view plane, toward the Count's side of the screen, by `tan(amount) * flame height * v.y` (the base-pivot quad has y 0..1), so the tip tilts exactly `_LeanAmount` degrees from the candle's own on-screen axis whatever the billboard transform does. The halo uses the same shader without a property block (amount 0), and follows the tip by half the shift.
- `CandleFlame` now only faces the viewer (`LookRotation(to, up)`), and each frame writes `_LeanDir/_LeanAmount` through a static `MaterialPropertyBlock` (`maxBendDegrees * Bend01`, 55 deg max, 33 at 6 m, ease toward 0 at 60 deg/s when there is no target, `Floater.relaxSpeed` eases the bend itself). The transform-based lean code from rounds 5-6 is deleted.
- Self-test (`HC_LEAN_CHECK`, `CandleFlame.LogLeanReport(tag)`, also automatic 3x once a flame leans >= 20 deg): for the three leaning flames nearest the camera it takes the flame's base and its tip with the shader's offset applied (`LeanTipOffset` mirrors the vertex code), projects both to screen with the camera and logs the angle against the candle's projected axis, plus the property block read back from the renderer (`_LeanAmount`, `_LeanDir`) and the material's shader name and `isSupported`. Integrator: call `CandleFlame.LogLeanReport("S5")` and `("H05")` after those shots in CaptureMode. A pixel-level read-back needs a render target; the projection uses the same arithmetic as the vertex stage, so a mismatch between this log and the critic's pixels would point at the shader failing to compile or at the property block not reaching the draw (read `shader ... isSupported` and `_LeanAmount` in the same line).

## 2. Dressing readable
- **Altar sockets (S2):** now on a thin slab tilted 8 degrees toward the nave (the plane faces the eye at about 10 degrees instead of 2) with 4 cm raised silver rims and dark inset faces: blade slot, cross (bar + guard), round moonstone seat. The altar front decals are cracks + stain (no chips).
- **S3 eye I designed for (integrator, please use):** eye **(0.0, -1.56, 15.4)**, target **(0.0, -2.3, 24.0)**, vertical FOV 72 (eye height 1.64 above the crypt floor, 0.8 m above the vault ribs' spring... the CryptRibs at z 16.5 and 26.5 read as two arch rings). From there the north wall's 5 niches (x -4.6..4.6, each with a lit candle on its coffin lid) face the camera across 12.6 m, the south arm's bridge (0,-3.5,20.1, 4 candle posts) sits in the lower third (about -18 degrees against a -6 degree pitch), the sarcophagus is centre. The old eye (-5.2,-1.56,25.0) -> (0,-2.6,22.5) sees only about 3 niches (north x 2.3-4.6 and east z 27).
- **Planks (S4):** `Castle_OakPlank` is already on the re-exported leaf; round 6's override replaced it with brightened plain oak (any slot whose name started with Castle_Oak, including OakPlank). Fixed: only a slot named exactly `Castle_Oak` is swapped, for the world-space `Castle_OakPlank` surface (1 tile per metre = 0.25 m boards).

## 3. S1 piers and decals
- Piers: 0.92 m (1 cm proud of the arch piers, so their coplanar faces cannot show through: that was the lighter inner box); the two x-facing panels are 0.25 m narrower than the z-facing ones, so no two modules overlap; one material (Weathered, world-space) for every pier band, arch and rib, so the course grid is continuous across them.
- Chip decal: at most one per room (hall, chapel, crypt, gatehouse, exterior); the pick list is mostly cracks A/B and stains; the altar front no longer has chips.

## 4. S1 focal subject
- Throne on the axis at (0,0,27.05) facing the doors (unchanged), plus an iron chandelier on x = 0 over the table: hub (0,3.9,24.0), 1.7 m ring, 12 candles each with billboard flame and halo (no lights), 4.7 m of chain to the vault, slow sway. It sits behind the S5 camera (eye z 25.3), so it is not in that frame.
