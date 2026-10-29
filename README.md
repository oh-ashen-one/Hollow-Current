# Hollow Current

An original, single-player supernatural investigation game by **Ashen**, built in Unity. Explore a rain-soaked family home and a haunted castle, follow the traces left behind, and decide how to bring each haunting to an end.

**Two maps. Keyboard/mouse and PlayStation/Xbox controller support. Offline Fish Audio voices.**

- **The Bellwether House:** investigate a repeated family routine and quiet the object keeping it alive.
- **Haunted Castle:** explore a castle beneath two moons. Break its bargain and escape, or forge a sword and face Count Claude.

## Play

Use the macOS app attached to a [GitHub release](https://github.com/oh-ashen-one/Hollow-Current/releases), when available, or build it from source below. This repository is the source of truth; old local apps do not update when you pull Git.

The source-checkout launcher, `./tools/play.sh`, checks the build receipt before opening the app. It refuses an app built from different game sources rather than silently launching an outdated copy.

The release runs locally: **no Fish account, API key, subscription, or network connection is required to play.** Voice generation tools are for developers; generated audio is already included.

## Controls

| Action | Keyboard/mouse | PlayStation | Xbox |
|---|---|---|---|
| Move / look | WASD / mouse | Sticks | Sticks |
| Interact / hold to finish an action | E | Cross | A |
| Cycle instruments | 1, 2, 3 | L1 / R1 | LB / RB |
| Ask with recorder / strike with sword | Left mouse | R2 | RT |
| Radio / resonance box | R | L2 | LT |
| Flashlight / lantern | F | Square | X |
| Journal | J | Triangle / touchpad | Y / View |
| Sprint | Shift | Left-stick click | Left-stick click |
| Crouch | Ctrl | Circle | B |
| Pause | Escape | Options | Menu |

**Reader and UV evidence record automatically when you keep the clue in view.** A progress bar and confirmation show that the record was secured. The recorder takes a short listen after one press; interacting with Bellwether's music box can also capture its voice.

## How the investigations work

Explore, inspect what you find, and use the appropriate instrument. Nearby effects and jump scares are events; the evidence counter confirms actual recorded clues.

In **Bellwether**, three instrument records identify the source automatically. Family notes deepen the story without requiring a multiple-choice answer sheet or a mandatory hunt. Follow the current objective to restore power, quiet the music box with a three-second interaction, and return to the van.

In the **Castle**, the journal lets you choose a path:

- **Sword path:** read the ledger, gather three relics, and hold interact at the chapel forge altar for three seconds. Ring the bell and land **two** sword hits. The Count recovers for **five seconds total** after the first hit before beginning his next approach.
- **Escape path:** gather three records and the ledger pages, quiet the chapel bell, then lower and cross the drawbridge.

The chapel forge is the raised altar facing the pews. The crypt sarcophagus and great-hall skeleton are different objects. Room names, current-path objectives, pickup confirmations and missing-item prompts help you distinguish them.

The standard Castle gives warnings and breathing room between hunts. **Long Night** is the optional harder mode. The microphone option is off by default and is never required.

## Progress and settings

Evidence, filed notes, collected relics and a forged sword are saved at progress milestones. **Continue** restores them; death does not relocate relics. **New Investigation** deliberately starts that map over. Each map keeps its own save.

Saves and settings use Unity's application data location. On macOS, progress files are normally under `~/Library/Application Support/Bellwether Field Office/Hollow Current/`. Recording and automated-verification launch options do not overwrite ordinary disk saves.

Connect your intended audio output before launching. The game has a master-volume control and subtitles. Castle speech is positional and can be muffled by walls; Bellwether dispatch is heard over the radio. Quiet stretches are intentional.

## Build from source

Requirements:

- Unity **6000.3.20f1**, including macOS build support; see `ProjectSettings/ProjectVersion.txt`.
- Git and sufficient disk space for Unity's generated import/build cache.
- Python 3 for the optional tooling. Install dependencies in a virtual environment from `tools/requirements.txt`.

Open the project in the pinned Unity Editor, restore its pinned packages, then use these editor entry points **one at a time**:

1. `HollowCurrent.Editor.HouseLighting.PrepareAndBakeCastle`
2. `HollowCurrent.Editor.HouseLighting.PrepareAndBake`
3. `HollowCurrent.Editor.NativeBuild.BuildPreparedMacAll`

The prepared build validates both lighting receipts. Source or scene changes require fresh preparation/bakes; do not bypass a stale receipt. CLI imports/builds use `-batchmode -nographics -quit`; lighting preparation/bakes need graphics. On a shared workstation, use its GPU coordinator and wait for available capacity.

Output: `Builds/macOS/Hollow Current.app`. Run `python3 tools/write_build_receipt.py` after a successful build, then `./tools/play.sh`.

The runtime respects `GPU_SLOT_FPS_CAP`, defaulting to 60 fps. Platform coverage is currently macOS; Windows/Linux binaries are not provided or verified by this project.

## Development checks

```sh
python3 tools/check_source.py
python3 tools/check_fish_integration.py
python3 tools/check_audio_quality.py --strict
python3 tools/check_audio_keys.py
python3 tools/check_castle_scares.py
python3 tools/castle_replacements.py
python3 tools/test_castle_replacements.py
```

The C# checks use the pinned installed Unity assemblies and an imported `Unity.InputSystem.dll`. `--compile-only` explicitly skips rule execution. Historical balance scenarios use their original test fixtures; final-release contracts separately check the current rules. These checks do not establish human gameplay quality or rendered performance.

Legacy native route drivers and their historical results are retained for development. The final pass's verification scope is recorded in `docs/final-pass/STATUS.md`; do not treat older runs as tests of newer code.

Procedural art regeneration uses `tools/castle_build_all.sh`. The shared exporter protects the authored Tripo replacement prefabs and rejects conflicting FBX names. Preserve `.meta` files so references remain stable.

## Credits and licensing

- **Code:** [MIT](LICENSE).
- **Project media:** generally CC BY 4.0 to the extent rights are held by the author, with separate provider/third-party conditions. **The code license does not cover every asset.** See [ASSET_LICENSES.md](ASSET_LICENSES.md) before reusing media.
- **Voices and vocal effects:** Fish Audio Drama 3, using original fictional character references. The owner-approved natural hiding breath is preserved exactly.
- **Props and characters:** Tripo/Customuse generations and project-authored work; detailed provenance is in the asset ledger.
- **Artwork:** original project imagery generated with OpenAI tools and prepared for the game.
- **Selected music and effects:** Higgsfield, with the specific credit and no-training conditions documented in the asset license file.
- **Fonts:** the bundled fonts retain their Open Font License files.

Credit: **Hollow Current by Ashen**. Provider model weights, credentials and Unity installations are not included.

[About the creator](ABOUT_ME.md) · [Report an issue](https://github.com/oh-ashen-one/Hollow-Current/issues) · [Contributing](CONTRIBUTING.md)
