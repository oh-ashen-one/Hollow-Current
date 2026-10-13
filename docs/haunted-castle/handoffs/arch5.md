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

# Round 5 (hc/arch5): architecture from the art kit

Not seen in an engine; `check_source.py` PASS. Frame time not measured (no Unity): the hall adds about 42 floor tiles (24 tris), about 25 wall modules (36 tris), 6 arches (116), 4 ribs (180) and one 17x6 vault grid, no real-time lights, no colliders.

## What was rebuilt
- **Wall veneer** (`CastleWorld.FaceTiles`, rules table `VeneerRules`): the cube shells stay (colliders, nav, doors, Points unchanged). Every wall piece on the room side of the great hall (both spine walls, front, back), the chapel (4 walls), the crypt (4 walls) and the outer face of the front wall is tiled with `Wall_4m`/`Wall_2m` art modules (plinth, cornice, 3.2 m bands), squeezed to 0.125 m thickness (so they read as coursed relief, 0.13 m proud of the cube face, no collision change), stretched to fit each piece, skipped around doors/windows/niches. Stone varies per module through `CastleArt.Surface(World)`: Castle_StoneWeathered / StoneDark / StoneMossy (look5 names; fall back to tinted Castle_Stone until hc/look5 is merged), Castle_BlackStone one in twelve, FlagStone, warm Stone. Banners moved 0.17 m off the wall so the relief does not swallow them.
- **Floors:** `FloorTile` art over the hall (42), chapel (28) and crypt (outside the channel, about 24) floors, world-space flagstone with per-tile tint and rotation; tops 0.01 m over the cube floors.
- **Great hall:** 6 pointed arches between the colonnade columns (piers inside the column footprints), 4 cross vault ribs springing from the columns at 6 m (apex 8.84), a lit elliptic vault ceiling under the roof slab (`Hall vault`, y 8.4 at the walls to 8.95, stone albedo plus a faint cold emission, a `VaultCeiling` marker so it stays out of the bake and is lit by probes and realtime lights, instead of the black baked cube).
- **Crypt:** the 13 niches, column/rib kit and new floor tiles; **chapel:** pews, font, brazier, candles as before plus tiles; **exterior:** towers/spires as dressing4, plus the outer-face veneer on the front wall. Stair flights are NOT swapped (the box steps are the collision; the art flight's rise/run differ by 0.02/0.02 m and none of S1-S5 sees a stair).
- `HC_ARCH_KIT` logs the number of tiles placed.

## Regressions
- **Black standing flames:** cause found in `HouseLighting.Bake`: `foreach(var material in materials)material.globalIlluminationFlags=EmissiveIsBlack` runs over every material in the layout, including `Castle_Flame`, so the art candelabra's mesh flames render black. `CastleWorld.UnifyFlames` hides those flame meshes and adds the same `CandleFlame` billboard (additive teardrop plus halo) used by the floating candles, plus a motionless `Floater` so the standing candelabras lean toward the Count too. The 40-odd standing candelabras are now non-static (they carry a script; lit by their own candle light and probes).
- **S2 flames:** all candles burn now: 56 floating candles (40 placed inside the S2 view cone from (7,1.64,6.5), x 8.6-13.2, within +-45 degrees, y 1.5-5.2) plus the 6 standing candelabra candles that were black: well over 30 flame billboards in frame. No new Light components.
- **Lean:** the r4 screen-space tilt rotated toward the Count's on-screen position, which is straight up when he perches above the candles (zero visible lean). It now leans along the horizontal part of the offset projected on the camera plane, and when that runs along the sight line (S5 stands beside the candles looking at him) toward the screen side he is on, by `Floater.maxBendDegrees * Bend01` (55 deg max, about 33 at 6 m, about 50 at 3 m) relative to the candle's own (vertical) axis. `HC_LEAN_CHECK flame leans N deg off its candle ...` logs the first flame at or over 20 degrees.
- **Ribs and the table (numeric check):** `CastleWorld.SkeletonLift` samples the spine, 30 rib curves and the legs (formulas copied from `tools/castle_props.py`), keeps the points inside the table footprint, and logs `HC_SKELETON_TABLE ... min bone y ... clearance`. It showed the r4 layout (4.95 x 1.4 m table) really did intersect (end ribs and hips down to y 0.46 against a 0.95 m top): the first/last ribs bow only 0.4-0.6 m out and end near the floor. The table is now 3.5 x 1.0 m, centred on the spine 1.5 m toward the head, where the ribs bow out: computed clearance 0.58 m, no lift needed (the code lifts the skeleton automatically if a future change drops it under 0.2 m).
- **Ceiling:** see Great hall above. The look builder's vault fill light should aim at `Hall vault` (non-static, so realtime lights light it).

## Not done / risks
- The tiles are 0.125 m deep reliefs, not 0.5 m masonry: the walls still have box silhouettes; true thick walls would need collision proxies and a route re-validation.
- Tower/spire "art" does not exist in Resources/Castle; the towers stay primitives with the stone and blackstone materials.
- Static batching is not flagged by the bake (only ContributeGI): the veneer relies on dynamic batching of equal-material modules.
- Re-bake required; `ValidateCastleGeometry` and a frame-time capture of S1 (budget 14 ms) are still to run.
