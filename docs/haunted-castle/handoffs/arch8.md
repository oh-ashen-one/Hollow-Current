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

# Round 8 (hc/arch8): flames stay on their wicks, S5 candles, set pieces sized

Not seen in an engine; `check_source.py` PASS. The shader was edited without a compiler: if it fails to compile, `HC_FLAME_CHECK` / `HC_LEAN_CHECK` print `shader ... (NOT SUPPORTED)`.

## 1. Detached / streaking flames
- **Cause (by arithmetic):** the r7 vertex shader sheared the quad: tip offset `tan(a) * height`. At the 55 degree cap on a 0.17 m flame seen from 0.7 m that is a 0.24 m (about 340 px) sideways step for the tip, and the length grew by 1/cos(a): the bright upper body detached from the faint base (petals) or stretched (streaks). The base vertices were already exactly 0; the quad itself was fine.
- **Fix:** the flame is now rotated about its wick: offset = `right*sign*sin(a)*height*f - quadUp*(1-cos(a))*height*f` (f = local y, 0 at the base vertices). Length is preserved, f = 0 is exactly zero, and `a` is clamped to 45 degrees in the shader, in `CandleFlame` (`MaxLean`) and in the C# mirror `LeanVertexOffset`.
- **Verification log:** `HC_LEAN_CHECK` now prints, for the 3 nearest leaning candles, the wick's screen position before and after the lean (`moved N px`, must be 0.00), the tip's angle against the candle axis, the tip's distance from the wick in px against the unleaned flame length (these two should be close, no streak), and the property block read back from the renderer. S5 and H05 call `CandleFlame.LogLeanReport` already.

## 2. S5 candles
- The r7 ledge candelabras hung at y 3.9-4.8, below the S5 frame: the capture eye is at about (0,6.33,24.2) looking at (0,6.52,26.9), FOV 50, pitch 4 up (frame at z 26.5: y 5.4-7.6). Cause was placement, not the hide filter. `CastleLayout.S5LedgeCandelabras`: hubs (+-1.6,5.4,26.8), (+-2.0,5.2,27.0), (+-2.3,5.6,27.3), scale 1.6, billboard flames and halos, `Floater.neverHide = true`; `CaptureMode` skips `neverHide` floaters in its hide-for-shot pass (one-line edit in that file). Offline projection with that camera puts all six flames at viewport x 0.05-0.95, y 0.21-0.37 (lower third), 1.6-2.3 m off the eye-to-Count line.
- `HC_S5_CANDLES n (enabled m, with a flame in frame k)` is logged right after the S5 lean report.
- `CastleLayout.S5Eye/S5Subject` follow the new eye for the floater filter.

## 3. Set pieces from the capture eyes (`SetPiece` component, `HC_SETPIECE <shot> <name> <h>%` logged after S1-S4 shots)
Offline estimates with a plain pinhole model (bounds, no occlusion):
- **S2 altar sockets: 7.1%** of frame height from (7,1.64,6.5) pitched 10 down, FOV 72. The sockets are now three silver relic plinths 0.30 m tall (blade, cross, round) on the 8 degree slab, each with a dark inset face and a lit ember (billboard flame).
- **S3 vault: 41.5%** (three pointed VaultRib arches over the crypt at z 18, 21, 24, springing at x +-5.6, y -1.7, apex -0.28) and **niche band 10.6%** (the 5 north-wall niches with lit candles) from (0,-1.56,15.4) to (0,-2.3,24).
- **S4 keep: 21.6%** whole block, about 11% above the wall line: a crenellated 8 x 14.4 x 8 m keep block on the hall roof (z 14-22, top y 23.6, coursed faces, Battlement art, spire to y 35.6) replaces the cylinder keep; the front walls hide everything under about 19.6 degrees from the S4 eye. **Plank deck: 14.3%** (the leaf; it was already in frame, rows about 1226-1387, but dark): planks now a brightened world-space Castle_OakPlank with 0.3 m boards (tile 0.83/m), plus four lit candle posts on the parapets.
- **S1 dais + throne: 17.5%** (two 0.2 m steps, z 25.5-27.55, throne on top, facing the doors) and **chandelier: 40.8%** (scaled 1.4: ring 2.4 m, hub (0,3.9,22.9)). The table shrank to 3.3 m (z 22.05-25.35) to make room.
- Tag objects carry only explicit bounds (separate empty objects), so the keep, dais and arches stay in the bakeable statics except the altar slab, chandelier and deck (already scripted).

## Needs the integrator
- Re-bake (new geometry in hall, crypt, exterior, chapel).
- The keep block and its tiles are lit only by the baked moons and the exterior fills; if the next S4 shows it as a flat silhouette, that is a lighting question for look.
