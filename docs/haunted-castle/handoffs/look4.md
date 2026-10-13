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

## hc/look4 (round 4): exposure grade, crypt/exterior fills, fog, blood, skull
No Unity. `check_source.py` PASS (394 assertions, runtime + editor C#), `castle_verify.py` HC_VERIFY_PASS (68 assets, 0 fails). Targets (12-30% of pixels below luma 8 in every shot) are unmeasured until the next capture.

### Why the grade differed (found by reading Game.cs / Investigation.cs)
1. `Investigation.BeginWarning()` set `BreakerOn=false`; Game.Update disables every lamp when the breaker is off. In the castle that killed every candle in the hunt frames: blue-black hall, 0% warm. The integrator already fixed this (candles stay lit); other hunt-time behaviours (strobe 8%, ScareDirector flicker, 8 m / same-floor lamp budget, ~5.5 m cut-off) are left alone and the new fills below are deliberately NOT in `World.Lamps`, so they are immune to all of them.
2. **My earlier candle tuning was being overwritten.** Game.Awake adds `CastleLightTuning` and then records each lamp's intensity into `lampLevels`, which Game.Update writes back every frame. I tuned in `Start`, so the x2.4 intensity never stuck (only colour/range did); S1's orange came from the colour change alone. Tuning now runs in `Awake`, so recorded levels are the tuned ones. Because the intensity now actually applies, `CandleIntensityScale` is 1.0 (the old 2.4 was never seen) and the colour is less saturated, (1.0, 0.70, 0.42), to bring S1 from 20.8% warm towards 3-8% while S2/S5 stay warm. Range x1.3 clamped to 6.5-10 m (unchanged).

### New realtime fill lights (CastleLightTuning, castle-local, no shadows)
- Hall vault fill: point (0, 7.0, 21), colour (0.42, 0.55, 0.90), intensity 0.35, range 10 (ceiling/ribs read; S1 ceiling was 81% below 8).
- Crypt candle pools: 4 points at (+-3.45, -2.3, 19.9) and (+-3.45, -2.3, 25.1), colour (1.0, 0.66, 0.38), intensity 1.3, range 5 (floor/curb pools, specular on the blood).
- Crypt rune glow: point (0, -1.5, 22.5), colour (0.60, 0.32, 0.90), intensity 0.9, range 5.
- Crypt blood glow: point (0, -2.7, 20.1), colour (0.95, 0.12, 0.10), intensity 0.9, range 4.
- Facade moon fill: spot at (0, 9, -18) aimed at (0, 5, 0), angle 80, range 34, colour (0.40, 0.55, 0.88), intensity 1.2; enabled only while the camera is outside the gate (castle-local z < 0.5). The pale moon is behind the castle seen from the quay, so the front face was backlit and black.
- Fog layers: 8 layers (was 6), alpha 0.26 x (1.4 at the floor to 0.5 at 1.1 m) (was 0.16 x 1.25-0.6), cloud threshold lowered, plane edge falloff 0.32 of the plane (was 0.18) so the hard intersection edge near walls softens. This should show as a veil from the entrance camera without touching the far wall above 1.1 m.
- Ambient fill for downward faces: `FillGround` (0.010, 0.012, 0.022), was (0.004, 0.004, 0.008). Other fills unchanged.

### Blood
`Castle_Blood` smoothness 0.95-0.98 (was 0.90-0.98) and emission (0.30, 0.012, 0.020) (was 0.16, 0.004, 0.008), in the texture, `Castle_Blood.mat` and `CastleArt.LiquidEmission`. The liquid material is persisted into the baked scene at Prepare, so `CastleAtmosphere.Upgrade` also pushes the new emission onto existing "Castle surface / Castle_Blood" materials at runtime (no Prepare needed). Specular highlights come from the new crypt lights.

### Giant skeleton skull (tools/castle_props.py::giant_skeleton)
New ~1.6 m skull: elongated 12-sided cranium, brow ridges, ringed eye sockets with dark recesses, nasal openings, cheek arches, hinged lower jaw hanging open 24 deg, 6+5 teeth per side and two long fangs. The head is yawed 40 deg about the occiput in the mesh (Blender Rz -40 = Unity yaw -40, i.e. snout toward -Z in the asset's local frame; hc/dressing4 asked for ~30; with the stated placement yaw 53.1 the snout goes from ~90 deg off the S1 line of sight to ~50 deg) and moved to y=5.40 to keep the bounds: size 4.106 x 11.90 x 3.385 (was 11.96), min/max unchanged within 0.05 m. Tris 4,890 (budget 15,000). Only `GiantSkeleton.fbx` and its manifest entry changed. If the dressing builder rotates the skeleton again, re-aim by adjusting `Rz(-40)` in the `sc` block.

### Requests for the integrator
1. Re-bake the castle (ceiling/vault fill is realtime but the new `Castle fill lights` group lives under the layout root; baked window/moon values from round 3 still need the `CastleLightTuning.Tune(root)` call and the bake ambient given in the hc/look3 section).
2. If any new light must not exist in the baked scene, set `AddFillLights = false`.
