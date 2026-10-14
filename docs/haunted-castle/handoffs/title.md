# HANDOFF: piece T1 "title" (`hc/title`, worktree `.wt/title`)

Remote branch: `origin/hc/title` (branched from `origin/haunted-castle` e260b1e).

## Done (verified how)
- Key art: one Higgsfield GPT Image 2.5 job (`cc99ba72-efb5-432e-9108-49f1baabb55b`, 2.75 credits), accepted first time. Self-check: no text, painterly, mean luma of left 30 % at 1920 wide = 19.1, one small winged silhouette on the tallest tower against the bone-white moon, two moons. Shipped as `Assets/HollowCurrent/Resources/UI/CastleBackdrop.png` (2560x1440, 4.9 MB). Preview `docs/haunted-castle/refs/title_1.preview.webp`; job record `refs/title_1.job.json`; log in `ASSET_LOG.md`.
- `Runtime/TitleScreen.cs` (new MonoBehaviour, added to the Interface's GameObject on first Title draw): Ken Burns 1.00 to 1.06 over 40 s with a 4 s overlap crossfade loop, 0.6 s map crossfade, fog (generated 256 px tiling noise, alpha 0.12, 4 design px/s, strip-faded over the lower half), vignette, left gradient 0.78 to 0 over 45 %, tracked type, card art crops. Fog, vignette and gradient textures are destroyed in its `OnDestroy`.
- `Interface.Title()` rewritten plus private helpers at the end of the class (`TitleHit`, `TitleBegin`, `TitleLink`, `TitleCard`, `TitleFrame`, `titleTagline`). Control ids unchanged (`hc-ui/BEGIN INVESTIGATION` registered first, then `hc-ui/map/<Id>`, `hc-ui/FIELD SETTINGS`, `hc-ui/EXIT TO DESKTOP`), so D-pad and `ConsumeControllerAction` behave as before. `Game.SelectMap/BeginInvestigation` untouched. `Loading()`, `Settings()`, `Ending()`, `Backdrop()`, `Brand()` untouched.
- Font: Cinzel (OFL), static weight-500 instance at `Resources/UI/Fonts/Cinzel-Medium.ttf` + `OFL.txt`; loaded with `Resources.Load<Font>("UI/Fonts/Cinzel-Medium")`, falls back to the dynamic Georgia.
- `python3 tools/check_source.py` (with `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll`): PASS. No Unity launched, so nothing is verified in a player yet.
- Mock: `docs/haunted-castle/rounds/r10/title_mock.png` (castle selected), `title_mock_bellwether.png`, generator `title_mock.py`. The mock uses the same coordinates, fonts and sizes as `Title()`; Unity line-box metrics may shift type by a few pixels.

## Integrator: nothing beyond merge + build, except
- Let Unity import `Resources/UI/Fonts/Cinzel-Medium.ttf` (it creates the `.meta`; commit it). Default TrueType importer settings are right (dynamic, include font data).
- `CastleBackdrop.png.meta` already raised: max texture size 4096, high-quality compression (was 2048 / normal), otherwise the 2560 art would be downsampled.
- First build: check type placement at 1920x1080 and 2560x1440 (title top at y 78, cards y 440, BEGIN y 668 in the 1600x900 design space), and that "not in this build" shows when a scene is missing.

## Not done / notes
- The not-in-build card is drained with a neutral veil, not a true desaturation (the UIArtwork shader only multiplies).
- Brand strip is now two 12 px lines at top-left only (right-hand case tag and short rule dropped on the title; other screens keep `Brand()`).
