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

## hc/look8 (round 8): half-steps, facade restored halfway, banner striping
No Unity. `check_source.py` PASS (394 assertions, runtime + editor C#), `castle_verify.py` HC_VERIFY_PASS (72 assets).
`python3 tools/exposure_report.py docs/haunted-castle/rounds/r7/captures --prev docs/haunted-castle/rounds/r6/captures` (before): S1 11.6 near-black (too bright, warm 9.0), S3 23.1 (in band, warm 9.5), S4 6.2 (too bright, wall luma 28.6 -> 15.1 after the r7 dimming), S5 15.1, S6 34.2 (too dark), S2 20.6, hunt 14.4-20.5.

### Changes and predictions (each a half-step toward the 20% target or the warm band; predictions are fits, not simulations)
| shot | before (dark%, warm%) | change | predicted |
|---|---|---|---|
| S1 | 11.6, 9.0 | vault fill 0.42 -> 0.32; candle intensity scale 0.90 -> 0.82; candle colour (1.0,0.74,0.50) -> (1.0,0.77,0.55) | dark ~14-16 (aim 15.8), warm ~7 |
| S3 | 23.1, 9.5 | crypt warm pool 1.10 -> 0.85 (candle colour change also trims it) | dark ~25, warm ~7.5 |
| S6 | 34.2, 1.7 | courtyard fills 2.2 -> 2.6 (range 9 unchanged); r6 -> r7 slope was about -17 points per unit of intensity, so +0.4 is the half-step | dark ~27-31 (aim 27.1) |
| S4 | 6.2, 0.4 | facade spot 0.80 -> 1.05 (restored halfway between 0.80 and 1.35, as asked); keep 0.35 and bridge 0.30 unchanged | wall luma ~21-22 (r5/r6 28.6, r7 15.1); dark% stays ~5-6, i.e. still above the band's bright edge: brightness of the wall and the near-black share of the whole frame pull opposite ways here, the sky and foreground dominate the dark pixels, so do not chase it with the spot |
| S2, S5, hunt | in band | unchanged (the S1 candle change nudges H01-H08 by about -1 to -2 points of near-black, they stay at 15-22) | same |

### S5 banner striping (p90 row diff 5.4 -> 6.9, worse)
Cause is the cloth, not a missing decal offset: the 8 px plain weave of `Castle_Oxblood` was sampled with bilinear filtering (`filterMode: 1`) and 4x aniso, so at grazing angles the weave aliases into 1-2 px row stripes (and the banner can also z-fight the wall it hangs on). Fixes:
- `Castle_Oxblood_{albedo,normal,mask}.png.meta`: `filterMode: 2` (trilinear), `aniso: 8`, mips on (they already were). `castle_common.write_texture_meta` gained `filter_mode` / `aniso` arguments; every other texture keeps bilinear/4.
- The weave normal strength is lower (`ns` 3.0 -> 1.8) and its occlusion variation 0.25 -> 0.15, so the residual aliasing has less contrast. `Castle_Oxblood_normal.png` and `_mask.png` regenerated (albedo unchanged).
- `Banner.fbx` and `TapestryPanel.fbx` are tilted 1 deg about their rod (`Rx(1)` in `castle_props.py::banner` and `castle_architecture.py::tapestry_panel`): the hem stands about 5 cm off the wall (3 m x tan 1 deg), so the cloth is never coplanar with the wall at grazing angles; the 2 cm offset alone was not enough. Sizes: Banner 1.39 x 0.13 x 2.98, TapestryPanel 1.52 x 0.09 x 2.70 (depth was 0.07; manifests updated). The layout builder should keep the rod flush to the wall; the tilt is baked in the mesh.
Expected: p90 row diff <= 3 on both banners; verify with the next capture.
