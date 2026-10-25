# Hollow Current: The Bellwether House

An original single-player ghost investigation in a furnished three-floor family house. Collect evidence with an induction reader, UV light and EVP recorder; piece together the family's story; survive a sight-and-sound-driven hunt; quiet the music box and return to the van.

**Bellwether House implementation is complete.** See [MAP_STATUS.md](MAP_STATUS.md) for the final feedback fixes and the exact validation boundary.

The project includes the Split Grin character, authored running/search/grab animations, Mourning Note interface, procedural house, investigative rules, controller support and local audio. No runtime API keys or external generation services are required.

## Requirements

- **Unity 6000.3.20f1**, installed through Unity Hub.
- macOS build support for the tested native target. Other platform exports are not verified.
- Unity Input System **1.20.1**, restored through `Packages/manifest.json`.
- Python 3 for optional authoring/check scripts; eSpeak NG and FFmpeg only if regenerating the bundled public-edition voices.

## Open and play

1. Clone this repository and add its root folder as a Unity Hub project.
2. Open with Unity 6000.3.20f1 and allow package/asset import to finish.
3. Open `Assets/HollowCurrent/Scenes/Bellwether.unity`.
4. Enter Play mode. Runtime boot creates the investigator, interface and investigation state.
5. Use **Begin Investigation** on the title screen.

If regenerating the authored scene, use the existing editor methods below. The committed runtime assets and public voice files are already included; no paid generation account is needed. Optional authoring scripts for reconstructing models may expect their original provider/master inputs; unused HD masters are excluded from this repository.

## Build macOS

Close other copies of this project before batch commands. Set `UNITY` to the installed editor executable for your machine. These commands rebuild the authored scene/bake, then build it without regenerating it again:

```sh
UNITY='/Applications/Unity/Hub/Editor/6000.3.20f1/Unity.app/Contents/MacOS/Unity'
"$UNITY" -batchmode -quit -projectPath "$PWD" -executeMethod HollowCurrent.Editor.HouseLighting.PrepareAndBake -logFile lighting.log
"$UNITY" -batchmode -nographics -quit -projectPath "$PWD" -executeMethod HollowCurrent.Editor.NativeBuild.BuildPreparedMac -logFile build.log
```

Output: `Builds/macOS/Hollow Current.app`.

### Maps (Bellwether and the Haunted Castle)

Scene names, bake receipts and display names live only in `Assets/HollowCurrent/Core/MapId.cs` (`MapId`, `MapCatalog`). Each map has explicit editor entry points for `-executeMethod`, rather than a command-line map argument:

| Step | Bellwether (unchanged names) | Castle |
| --- | --- | --- |
| Regenerate scene | `HollowCurrent.Editor.NativeBuild.Prepare` | `HollowCurrent.Editor.NativeBuild.PrepareCastle` |
| Prepare + bake (graphics session) | `HollowCurrent.Editor.HouseLighting.PrepareAndBake` | `HollowCurrent.Editor.HouseLighting.PrepareAndBakeCastle` |
| Prepared build (`-nographics`) | `HollowCurrent.Editor.NativeBuild.BuildPreparedMac` | `HollowCurrent.Editor.NativeBuild.BuildPreparedMacCastle` |
| Unbaked iteration build | `HollowCurrent.Editor.NativeBuild.BuildMac` | `HollowCurrent.Editor.NativeBuild.BuildMacCastle` |
| Engine geometry check | `HollowCurrent.Editor.NativeBuild.HeadlessValidate` | `HollowCurrent.Editor.NativeBuild.ValidateCastleGeometry` |

`HollowCurrent.Editor.NativeBuild.BuildPreparedMacAll` validates both receipts and builds one player with both scenes (title-screen map picker). Castle builds go to `Builds/macOS-Castle/Hollow Current Castle.app`.

Each map has its own receipt (`Art/LayoutData/BellwetherBake.json`, `Art/LayoutData/CastleBake.json`). The Castle receipt hashes every `.cs`/`.shader` under `Assets/HollowCurrent` (except `NativeVerification.cs`). The Bellwether receipt hashes the same set minus castle-only sources (`Runtime/Castle/`, `Core/Forge.cs`, `Editor/CountClaudeImport.cs`, `Resources/Castle*/`, `Resources/Characters/CountClaude/`, `Art/LayoutData/Castle/`), so castle-only work does not stale the house bake. Changes to shared sources (for example `Game.cs`) still stale both receipts: the prepared build then refuses with a message naming the bake method to re-run. The lighting step needs a working graphics session; do not add `-nographics` to that step. The prepared build validates its bake receipt and rejects stale scene/source dependencies. A Unity license and platform toolchain are independently required.

## Controls

| Action | Keyboard/mouse | DualSense |
| --- | --- | --- |
| Move / look | WASD / mouse | Left / right stick |
| Interact / hold an action | E | Cross |
| Flashlight | F | Square |
| Journal | J | Triangle |
| Crouch | Left Ctrl / C | Circle |
| Sprint | Shift | L3 |
| Use instrument | Left mouse | R2 |
| Radio | R | L2 |
| Pause | Escape | Options |

The game includes checkpoint retry and a fresh investigation option. Physical Bluetooth/AirPlay behavior depends on the user's setup; the automated controller checks are not a claim of universal hardware verification.

## Tests and development

After Unity has imported the Input System package, `python3 tools/check_source.py` compiles against installed Unity assemblies and runs engine-independent gameplay rules. It does not launch the game or verify graphics. Set `UNITY_CONTENTS` if the editor is installed elsewhere. It compiles every `.cs` under `Core/`, `Runtime/` and `Editor/` recursively (including `Runtime/Castle/`). A checkout without an imported `Library/` (for example a builder worktree) can borrow the Input System assembly from an imported clone: `INPUT_SYSTEM_REFERENCE=/path/to/imported/clone/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py`. `HollowCurrent.Editor.NativeBuild.HeadlessValidate` is the separate engine geometry/asset validation method. Native route instrumentation is opt-in via `-hc-verify`; ordinary launches never run it.

The public-edition radio/EVP voices are intentionally different from the private prototype's Apple System Voices. Regenerate with:

```sh
# macOS: brew install espeak-ng ffmpeg
# Debian/Ubuntu: sudo apt install espeak-ng ffmpeg
python3 tools/generate_open_voice.py
```

### Haunted Castle assets

Castle procedural art is regenerated headlessly from committed scripts; run the build script through the shared GPU slot coordinator. The shared exporter preserves all Tripo replacement prefabs, and pre/post checks reject duplicate FBX resources or incomplete imports. `python3 tools/test_castle_replacements.py` covers failure handling; `Blender -b --python-exit-code 1 -P tools/test_castle_rebuild_blender.py` exercises legacy generators without changing shipped art. Pipeline: `tools/castle_build_all.sh` runs `python3 tools/castle_textures.py`, `python3 tools/castle_sky.py 2048`, then Blender `-b -P` on `tools/castle_architecture.py`, `tools/castle_props.py`, `tools/castle_furniture.py` and `tools/castle_verify.py` (`tools/castle_preview.py` renders a contact sheet). Count Claude: `/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/count_prepare.py`, then the Unity editor method `HollowCurrent.Editor.CountClaudeImport.Prepare`. Castle UI cards and title backdrop: `python3 tools/ui_blood_ledger.py`. Voices and vocal effects on both maps use the committed Fish Audio Drama 3 assets (see `docs/audio/fish-integration/HANDOFF.md`); do not run the legacy voice generator over them. Orrery tick: `python3 tools/audio_orrery_tick.py`.

## Licensing

Project code and documentation: **MIT**. Most project-owned media: **CC BY 4.0**. Higgsfield audio has a separate creative-use redistribution grant and an AI-training restriction. Unity and dependencies retain their own licenses. Read [ASSET_LICENSES.md](ASSET_LICENSES.md) before reusing media.

This is a clean public source snapshot. Private history, local account/credential material, machine-specific launch infrastructure and personal operational records are preserved outside this repository, rather than published.

## Branch policy

The owner approved the initial public `main` snapshot and, on 2026-10-13, approved merging the completed cabinet-visibility, scare-pacing, caption and research work. Future changes should use feature branches; subsequent merges into `main` require owner approval.

## Fish Audio voices (2026-10-22)

Both maps use offline Fish Audio Drama 3 voice assets on `fish-audio-integration`. Read `docs/audio/fish-integration/HANDOFF.md` for acceptance and build status. `python3 tools/check_fish_integration.py` checks complete voice coverage, masters, transcripts and installed hashes; `python3 tools/fish_audio_integrate.py` reprocesses the cached takes without generating paid audio. New paid requests require explicit `--generate` and a private key outside the repo. Do not run the legacy eSpeak/Kokoro generators over the Fish masters.

An explicitly requested native verification run can add `-hc-audio-proof <evidence-directory>` to log real decoded vocal-clip playback and sample the listener's output. This passive instrumentation is inactive in normal play and does not prove a human listening verdict.
