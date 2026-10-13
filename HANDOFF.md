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

# Piece 4c — Count Claude round 3 (branch `hc/count3`, Opus 5.5)

Commits: `2e88dd4` (wings: two-sided membrane, finger Ext bones, folded Search pose), `fa5a3b3` (capture choreography,
tracking camera, ledge-launch fix, rim light). `tools/check_source.py` PASS. Unity not launched; Blender headless only.
The exported FBX were re-imported in Blender: every clip's world joint positions (incl. wing-tip markers) match the source rig.

## Findings (r2 critic items)
- **Hunt off camera (H02–H10):** log shows `HC_COUNT_DIVE` then nothing. The straight swoop's sphere cast hit the
  rail coping under his own feet (feet 0.30 m behind its front edge, 35 deg downward sweep, radius 0.28 m), so
  PlanRoute fell back to the NavMesh route back across the balcony deck, behind the balustrade and out of view, never
  reaching the 2.5 m hold (no feint logged); the capture camera did not track. Fixed: PlanRoute now inserts a launch point
  0.6 m forward / 0.5 m up when that clears the swoop. The capture no longer depends on the AI: see the choreography below.
- **Wings stop at the hip (S6):** S6 used the Search clip, which spread the fan (0.62) and lifted the fingers. Search now
  keeps the wings folded to the shins (rest tip z 0.35–0.48 m = sheet proportion), arms at his sides. Spread poses are
  lengthened by Ext bones (Glide span 4.0 m, Dive 2.6 m).
- **Wing as a black smear:** 5709 of 8296 membrane faces were a single sheet (back-face culled from behind). They are now
  doubled with reversed winding and negated custom normals; material stays Standard (lit). Plus a cold shadowless rim
  light (range 3.6, intensity 1.1) parented behind his shoulders (`CountClaudeProfile.RimLightRange/Intensity`).
- **Tree behind him (S6):** not mine: the courtyard `DeadTree` prop at (-4.2, 0, 12.4) (`CastleWorld.Courtyard`). The r2 S6
  camera (z 7.4 looking +z) put it straight behind him; S6 moves to the gatehouse (below).
- **830×690 px "fog card" (H01/H02):** not mine: `Game` calls `ScareDirector.Pulse(player + forward*3)` on `Phase.Warning`,
  which emits the "Local manifestation mist" particles (2.8 s lifetime, size 1.6) 3 m in front of the camera. The capture
  enters Warning and Hunt in the same frame, so H01/H02 catch it. Wait 3 s after `BeginWarning` before the first H shot.

## Integrator steps
1. Merge `hc/count3`, then **re-run the import** (bones changed 46 → 56, mesh doubled): `CountClaudeImport.Prepare` through
   `gpu_slot.sh`, `-batchmode -nographics -quit`. Expect `HC_CHARACTER_RIG bones=56`, seven `HC_CHARACTER_CLIP`,
   `HC_COUNTCLAUDE_PASS`. Commit `Resources/Characters/CountClaude.prefab` and `Resources/Characters/CountClaude/*`.
2. Hunt capture (`CaptureMode.cs`), replacing the 1 s realtime loop:
   ```csharp
   e=new Vector3(0,1.64f,15.6f);t=new Vector3(1.5f,1.6f,24f);Place(e,t);Torch(true);
   game.Case.Equip();game.Case.EnterHouse();game.Case.Record(Evidence.Induction);game.Case.Record(Evidence.Handprint);
   game.Case.BeginWarning();game.Creature.SetHuntOrigin(origin);game.Case.Tick(9.05f,true);
   yield return new WaitForSecondsRealtime(3f);                 // warning mist (ScareDirector.Pulse) dies at 2.8 s
   game.Creature.BeginCaptureChoreography(1);                   // 1 = east balcony perch; hold 2.5 m (default)
   for(int i=0;i<10;i++){
       while(game.Creature.CaptureClock<i+1)yield return null;  // deterministic: shot k at choreography second k
       var view=game.Player.View.transform;
       yield return Shot("H"+(i+1).ToString("00")+"_hunt_dive","(hunt sequence)",view.position,view.position+view.forward*5,game.Creature.CaptureNote);
   }
   game.Creature.EndCaptureChoreography();
   ```
   Timeline: H01–H03 Perch (Mode 0), H04–H06 Glide (Mode 1, airborne, wings spread), H07 Dive, H08–H09 Hold at 2.5 m (Mode 3),
   H10 Feint (Mode 1). The investigator's view tracks him (yaw on the body, pitch on the view, eye height unchanged).
   Log check: `HC_COUNT_CAPTURE begin ...`, then `phase=Perch|Glide|Dive|Hold|Feint` lines with `clock=0.00/3.50/6.50/8.00/9.50`;
   `HC_CAPTURE_CAMERA_PUSHED` should not appear.
3. S6 (scale + sheet), moved to the gatehouse: `game.Creature.ManifestAt(new Vector3(-0.9f,0,4.4f),180,9999,true);` (Mode 2,
   no Pose override), `Presence.ScaleReference(new Vector3(0.35f,0,4.4f),180)`, eye `(-0.3,1.15,1.0)`, target `(-0.3,1.1,4.4)`.
   Behind him is the gatehouse inner wall and great doors; the lantern cart (3.6,0,3) and winch (-2.6,0,1.4) are outside the frame.
4. S5 unchanged (`StageForCapture(0)` + `CaptureFraming(.5f,30,...)`).

## Not done
- Nothing verified in Unity (choreography path vs hall geometry, rim light look, two-sided normals in engine).
- S5 left-claw clip through the rail and "one knee up on the coping" (critic r2 S5) not addressed this round.
