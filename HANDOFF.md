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

# Round 6 (hc/arch6): integrate the stone kit, make the candle rule undeniable, S5 ledge

Not seen in an engine; `check_source.py` PASS. Frame time not measured (S1 budget 14 ms): modules are about 370 pieces of 36 tris plus 8 piers' worth, 3 stone-family materials and the floor material, `isStatic` set in the editor build so the prepared scene batches equal-material modules; no new real-time lights.

## 1. Stone kit integrated
- **Column inside pier:** the hall's ColumnGreat art (round shaft + colonettes) stood inside the square PointedArch piers. `CastleWorld.Pier` now builds each colonnade pier from four Wall_2m module faces (0.9 m square, 8.8 m, coursed, 0.125 m skin) over the old collider box, the round column visual is gone (crypt columns unchanged). Arch piers and cross ribs spring from it.
- **Untextured faces:** the "dark band" / "flat black panel" were the `Castle_BlackStone` and flag-tinted module variants (very dark, almost no mortar). Both are removed from the module palette.
- **Whole-panel swaps:** modules are now 2 m (stretched 0.7-1.5x) and each module picks Weathered / Dark / Mossy by a seeded `System.Random` (lower band leans Mossy/Dark, upper Weathered), with tints equalising their mean luminance (~0.38) so neighbours differ in hue/texture, not brightness. `CastleArt.Decal` (cracks A/B, chips, stain, soot base, soot only at wall feet) lands on a seeded 30% of modules, 1.5 cm off the relief, decal shader depth-biased.
- **Gatehouse (S6):** the four inner faces of the gatehouse (x -6/6 for z 0-6, z 0 inside, z 6 gatehouse side) get modules via new veneer rules, so the room no longer repeats its 388 px tile.
- **Banners:** the six hall banners are now look5's `TapestryPanel`, their back face 2 cm proud of the 0.17 m relief (they were 8 cm in front of it, so the stripe is not wall z-fighting; if it persists it is the Oxblood texture's minification and belongs to look).

## 2. Candle lean (measured 0 degrees three rounds running)
- **Likely root cause found:** the Floater only got a target in `CastleRun`'s Hunt phase (or from capture staging), so candles beside a perched/gliding Count never had one. `Floater` now falls back to the active `Presence` (Count) whenever no target was set, re-resolved each second.
- **The lean itself:** `CandleFlame` rotates the flame billboard about the view axis, from the candle's own on-screen axis (world up flattened against the sight line) toward the side of the screen the Count is on, by `maxBendDegrees * Bend01` (55 deg x smoothstep over 14 m: 33 deg at 6 m, about 50 at 3 m). Works from any camera, whatever the Count's position.
- **Self-test:** `HC_LEAN_CHECK` (auto, up to 3 reports 2 s apart once a flame leans >= 20 deg; also `CandleFlame.LogLeanReport("S5")` for capture code to call after a shot) logs, for the three leaning flames nearest the camera, the on-screen tilt of the quad against the candle axis, from the screen positions of base and tip: `... flame quad is tilted N deg on screen from its candle axis (quad a, candle b from screen vertical; applied lean c)`. Integrator: call `CandleFlame.LogLeanReport("S5")` after the S5 shot in CaptureMode (not my file) to get the per-shot line.

## 3. S5 rose-ledge perch
- `S5Eye/S5Subject` in CastleLayout are now (0,6,25.3) / (0,5.6,27.3); hall floaters stay 1.2 m off those lines.
- Six floating candelabras in the lower third of that view: (-1.9,4.2,26.6), (1.9,4.2,26.6), (-3.2,4.8,26.3), (3.2,4.8,26.3), (-1.0,3.9,26.9), (1.0,3.9,26.9) (billboard flames and halos, no lights; they also give the warm key S5 lacked).
- Rose window moons: both are now painted into the opening itself (sprites at (+-0.72, 7.1, 28.85), 0.5 m radius), visible from every viewpoint in the hall instead of on one eye's ray; the real 55/42 deg sky moons cannot be framed by it. `WindowMoon` still hides them outside.

## 4. Dressing
- **Altar (S2):** silver-rimmed inset sockets on the top: blade slot (z 5.85), cross for the hilt (z 6.5), round moonstone seat (z 7.15); chips and stain decals on the altar front.
- **Crypt (S3):** a 14th niche on the east wall (z 27.0); every niche has a lit candle (billboard flame) on its coffin lid; the bridge art sat 0.6 m off its deck collider and 0.2 m too high (z 19.5, y -3.3): now centred on the deck (0,-3.5,20.1) with four lit candle posts on the curb ends beside it.
- **Skull:** the skeleton lies yaw 0 so the skull already points at the doors; turning the skull toward the room is impossible by placement in the 1.35 m strip (the mesh is one piece). A 3/4 skull needs the head rotated in `tools/castle_props.py` (art builder).
- **Drawbridge (S4):** the leaf's oak slots take a world-space plank surface (`Castle_Planks` if look6 provides it, else Castle_Oak brightened 1.5x); the chains were attached to a leaf that rendered as a dark featureless plane, which is why they seemed to end in the moat. They still anchor at the gate arch shoulders (x +-1.5, y 4.4); there is no chain art to continue to the winch inside the gatehouse.

## Not done / needs the integrator
- Frame time unmeasured; keep an eye on draw calls if batching is not merged in the player.
- The vault still has no ribs beyond the 4 cross ribs and arcades (no bosses/corbels/tracery); S4 keep still one tier; the real stairs untouched.
- Re-bake required (piers, tapestries, floaters moved).
