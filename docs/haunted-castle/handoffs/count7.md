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

# Piece 4g — Count Claude round 7: the demon (branch `hc/count7`, Opus 5.5)

Commit `168f1ee` (+ this handoff). `check_source.py` PASS. Unity not launched; Blender headless only.

## Generation (Customuse, owner account; details in `docs/haunted-castle/ASSET_LOG.md`)
- Workflow `79358063-a4bd-4a8e-ac51-50effc66655b`: Tripo v3.1 multiview mesh **100 credits** (4621 → 4521), clean-up + auto-rig
  (Tripo provider) **35 credits** (→ 4486). **Total 135.** Not moderated. Licence line added to `ASSET_LICENSES.md`.
- The mesh came back **untextured**. `tools/count_project_refs.py` projects the three reference views onto its UVs, with per-view
  depth tests for occlusion and a mirrored side view for his right. The result, `tools/count_source/CountClaude_demon_albedo.png`,
  is committed because the reference PNGs are gitignored.
- Inspected in headless Blender: skin-membrane wings with hooks, skinless skull with the split jaw and blood, flayed red chest,
  six-finger-length claws, shredded coat and collar. Previews are in the session scratchpad (not committed).

## Model
- Head top **2.5 m**; **46,306 triangles** (38,875 source + the doubled single-sheet membrane); 54 bones (22 remapped humanoid +
  Jaw, 2 wing roots, 10 fan + 10 Ext, 10 zero-weight End markers); 2 submeshes (0 body/coat, 1 wing membrane).
- The rig maps 117 generic Tripo bones to the canonical names; unmapped bones fold into their mapped ancestor (fingers into the
  hands; coat back tails and side flap into the thighs; centre flap split by side). The wing membrane comes from the rig's wing
  chains, re-skinned onto the fan derived from those chains (pivots at the shoulder blades, fan 5–175 deg).
- Gore: Presence swaps `CastleArt.Gore(Skin, …, .55)` onto submesh 0 and `Gore(Membrane, …, .35)` onto submesh 1 in the castle
  (`CountClaudeProfile.GoreMaterials`). Look-dev is unverified in engine; turn the toggle off if the coat reads skin-tinted.

| Mode | Clip | Length | Wrap | Max span | Max top |
|---|---|---|---|---|---|
| 0 | Perch | 3.2 s | loop | 3.47 m | 1.66 m |
| 1 | Glide | 2.0 s | loop | 4.57 m | 2.33 m |
| 2 | Search | 2.6 s | loop | 2.32 m | 2.50 m |
| 3 | Dive | 1.0 s | loop | 2.52 m | 1.89 m |
| 4 | Stagger | 1.3 s | hold last | 2.97 m | 2.60 m |
| 5 | Death | 2.4 s | hold last | 2.85 m | 2.63 m |
| 6 | Grab | 0.9 s | hold last | 4.39 m | 2.72 m |
| 7 | Crawl | 1.4 s | loop | 2.18 m | 1.75 m |

- Crawl (top 1.75 m) is used on foot at service doors and where the clear height is under `CrawlCeiling` 2.45 m.
- Grab, in a Blender camera matching the capture (2.8 m, eye 0.39 m above his origin, vFOV 66, aimed at his bounds centre): wing
  tips leave both frame edges (x 0.00–1.06), the claws reach forward-down at the lens beside a clear face, the jaw gapes, and his
  feet stay in frame (y 0.12–0.83). The membranes read thin and edge-on at that angle.
- Exported FBX re-imported in Blender: Perch/Glide/Dive/Grab/Death world joints match the source rig exactly.

## Integrator steps
1. Merge `hc/count7`; run `CountClaudeImport.Prepare` (all FBX, the albedo and bone names changed). Expect `HC_CHARACTER_RIG bones=54`,
   eight `HC_CHARACTER_CLIP` lines and `HC_COUNTCLAUDE_PASS ... 8 independent motions`; no `HC_COUNTCLAUDE_GRAB_REACH` warning.
   Commit `Resources/Characters/CountClaude*`. ValidateCastleGeometry contract unchanged (rigged, ≥20 bones, Animator).
2. In the castle log expect `HC_COUNT_GORE applied`.
3. CaptureMode: no call changes. He is 16% taller, so S6 framing should be re-checked (head 2.5 m against the 1.8 m figure =
   1.39×). For S5, `RoseLedgeFraming` re-sizes from his bounds.

## Not done
- No provider texture (projection from three views only; the right side mirrors the left).
- Membrane reads edge-on in the Grab at the capture angle. Death still reclines on the wings.
