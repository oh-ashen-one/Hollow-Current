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

# Piece 4f — Count Claude round 6 (branch `hc/count6`, Opus 5.5)

Commit `4e87630` (+ this handoff). `check_source.py` PASS; Unity not launched.

## BLOCKER: demon mesh not generated (owner/coordinator decision needed)
- `multi_image_to_3d` with front+side+back: job `ae97187c-4128-4440-b0d5-edec63f27a8c` ended **`nsfw`** (provider moderation).
- One retry with front+back only: job `d9c5eafa-b063-4a0c-b20b-69c5f8021871` ended **`nsfw`**. Stopped per the one-retry rule; `3d_rigging` was not run.
- **Credits: 0 net** (2 × 30 spent, 2 × 30 refunded). **But the first spend triggered a 400-credit Auto Top-Up on the owner's account**
  (21:47:26Z); balance is now 547.64. Logged in `docs/haunted-castle/ASSET_LOG.md`; no ASSET_LICENSES line (no asset).
- Options, none taken (each needs an OK and new paid jobs): (a) a moderation-safe input set (desaturated, bloodless "clay" crops
  of the same sheet) for the mesh, then gore via the look builder's `CastleArt.Gore` materials; (b) another 3D provider.
- So the model is still the round-5 mesh: **head top 2.15 m, 36,405 tris, 56 bones**. The 2.5 m rescale, re-skin and
  split-jaw bones wait for a mesh. Profile scale values are unchanged (ModelHeight 2.15).
- Gore materials (`CastleArt.Gore`, merged into haunted-castle from hc/look6): not applied. The current model has one material
  slot for coat, skin and wings, so Skin/Flesh/Membrane can't be split yet. Apply on the demon mesh (skin: body, flesh: chest
  and forearms, membrane: wings, wet on mouth and claws).

## What changed anyway
- **Grab (Mode 6), forward wing-wrap:** wings swept forward and opened (yaw 80, stretch 1.8); claws reach toward the lens at
  shoulder width beside a clear face. In a Blender camera matching the capture (2.8 m, eye 0.39 m above his origin, vFOV 66,
  aimed at his bounds centre), the mesh spans x −0.02..1.02 (wing tips leave both frame edges) and y 0.16..0.80 (feet in frame).
  `GrabReach` 1.70 m (the wing tips lead), so a gameplay grab presentation stands him 2.35 m from the camera.
- **Crawl (Mode 7, new):** stooped four-limbed walk, top 1.83 m (fits 2.0 m lintels). Presence uses it on foot at service doors
  (`CastleLayout.IsServiceDoor`) and wherever the clear height over the floor is under `CrawlCeiling` 2.3 m, at 2.4 m/s.
  `CountClaudeImport` now builds 8 states (Crawl loops, speed follows LocomotionRate); `HearthboundActor.ModeFrames` has 8 entries.
- **Capture:** hold height 1.25 m (lowest point 1.35 m, above the 1.1 m fog); the climax aims at his bounds centre (r5 H10 feet cut)
  and lunges to 2.5 m; glide leans back 34 deg with the heading 80% toward the camera (H05 chest/face).

| Mode | Clip | Length | Wrap | Max span | Max top |
|---|---|---|---|---|---|
| 0 | Perch | 3.2 s | loop | span 3.19 m | top 1.19 m |
| 1 | Glide | 2.0 s | loop | span 4.45 m | top 2.12 m |
| 2 | Search | 2.6 s | loop | span 2.14 m | top 2.21 m |
| 3 | Dive | 1.0 s | loop | span 2.86 m | top 1.55 m |
| 4 | Stagger | 1.3 s | hold last | span 2.67 m | top 2.24 m |
| 5 | Death | 2.4 s | hold last | span 2.89 m | top 2.25 m |
| 6 | Grab | 0.9 s | hold last | span 3.55 m | top 2.57 m |
| 7 | Crawl | 1.4 s | loop | span 1.28 m | top 1.83 m |

## Integrator steps
1. Merge `hc/count6`; run `CountClaudeImport.Prepare` (new `CountClaudeCrawl.fbx`, meta committed). Expect 8 `HC_CHARACTER_CLIP`
   lines incl. `Crawl` and `HC_COUNTCLAUDE_PASS ... 8 independent motions`; no `HC_COUNTCLAUDE_GRAB_REACH` warning. Commit
   `Resources/Characters/CountClaude*` (new `CountClaudeCrawl.anim`).
2. CaptureMode: no call changes (hunt choreography and S5/S6 as round 5). ValidateCastleGeometry contract unchanged (56 bones, Animator).
