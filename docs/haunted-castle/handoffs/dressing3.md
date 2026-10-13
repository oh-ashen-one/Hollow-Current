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

# Round 3 (hc/dressing3, merged with haunted-castle incl. hc/look3)

Not seen in an engine; `check_source.py` PASS. Every claim below is by construction, not by capture.

## Fixes
1. **Flames were white cards because the sprite shader was not in the player build.** `Shader.Find("Sprites/Default")` returns null there (Always Included Shaders holds only Standard), so every `SurrealProps.Unlit` material fell back to opaque Standard. This also whitened the window moons, the chapel window quads and the rune sprites. New `Resources/Castle/Shaders/CastleSprite.shader` (alpha blend) and `CastleFlame.shader` (additive, premultiplied), both unlit, loaded via `Resources.Load`. `Unlit` uses the first; flames and halos use `UnlitAdditive`. Flame is a 0.075 x 0.15 m teardrop on candelabras (x1.6 floater scale = 0.12 x 0.24), 0.11 x 0.22 on chapel candles, 0.17 x 0.34 on the hero candle, each with a soft additive halo billboard (`CandleFlame.glow`), no lights. Lean: `Floater.bendRange` 14 m, `maxBendDegrees` 55, so about 33 degrees at 6 m (was 19). `SetFlameTarget` is still driven by CastleRun/CaptureMode.
2. **Chapel candles:** 44 floating candles, 33 lit (flame + halo billboards), 11 dark, 1 hero (scale 1.5, brightest). Point-light count unchanged (zero lights added anywhere in this round).
3. **Great hall:** skeleton scale 1.0, yaw 58, centre (0,0,25.4): it lies diagonally with the skull front-left at about (-4.4,22.8) in the gap between the west columns (z 21.6 / 24.4), tail into the back wall. The art's head (+Y) imports as local -Z (seen in r2 S1: the long thin tail was at screen left, the head at right with yaw -90). The table moved to (2.0,0,22.75), clear of every rib (rib tips reach z about 23.6), chairs only at its east end, the rune ring moved 1 m toward the entrance (centre z 18.5, far edge 22.17). Books: 10, x 2.2 scale, all inside the S1 frustum (z 17-24, y 2.7-5.4). Candelabras 14 as before. Throne unchanged (-5.1,0,18.2).
4. **Sun window:** my quad was the 60,322 px white slab (same size as the 1.9 x 3.2 m quad at 5.5 m). Removed it; CastleAtmosphere's own pane (Castle_SunWindow material, blue rim) stays as the only window.
5. **Blood:** trench strips and the four moat boxes now take `CastleArt.Surface("Castle_Blood", Liquid)` directly; the Standard fallback `bloodpool` is red (0.36,0.02,0.03), smoothness 0.55 (0.9 reflected the blue sky, which made the channel blue-grey).
6. **S4 keep:** 5 round towers with conical slate spires (gate towers W/E apex y 17.4, rear W 17.4, keep 24.2, observatory 19.6), primitives with the stone/blackstone materials. Drawbridge: `BuildGate` now measures the leaf art's mesh centre in hinge space and turns it 180 about the hinge if it sits on the +Z (gatehouse) side, which would explain "no drawbridge" from the far bank; `DrawbridgeChains` art is placed (same fix) and shown only while the bridge is down (`ChainVisibility`). I did not touch the camera or the moon clipping: the S4 eye is chosen by CaptureMode (not mine), and from the lowered-bridge eye the 55/42 degree moons clear the walls by geometry (rays rise 55/42 degrees from a 2 m eye).
7. **Fog card in H01-H03:** not one of my props. In H01 the blob is a soft blue-white pool on the floor in front of the camera, which is the investigator torch cone through the new fog: `Torch(true)` is switched on only for the hunt and S5/S6 captures, S1 (same hall, same props, no torch) has none. My billboards (bats, flames, moons) are all small or far. Nothing removed.

## look3 merge and the window moons
hc/look3 raised the sky moons to 55 / 42 degrees. The rose window is at about 22 degrees from the S1 eye and the chapel east windows top out near 25 degrees from the S2 eye, so the real moons cannot be framed by those windows; the six sprites stay, aimed along the rays from the capture eyes through each opening (independent of the sky vectors). New `WindowMoon` hides each sprite while the viewer is outside the castle footprint, so S4 shows only the two real moons.

## Counts (round 3 total)
Hall: 1 skeleton, 6 banners, 14 candelabras (70 flame billboards + halos), 10 books. Chapel: 44 candles (33 lit), 4 fallen pews, font, brazier (flame + halo), 6 moon sprites shared with the hall. Crypt: unchanged (30 roots, 13 niches, 36 glow runes, trench). Exterior: 5 towers/spires, drawbridge chains.

## Needs the integrator
- New files need Unity import (metas written with random guids): `Resources/Castle/Shaders/CastleSprite.shader`, `CastleFlame.shader`, `Runtime/Castle/WindowMoon.cs`, `ChainVisibility.cs`.
- Re-bake the castle (geometry moved: table, ring, skeleton, towers).
- If the drawbridge leaf logged as flipped is wrong in play, set `CastleArt.ImportYaw` instead; the chains and the leaf then both need no fix.
