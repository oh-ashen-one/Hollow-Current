# Combined Fish + Tripo delivery — 2026-10-25

Both maps are in one macOS app built with Unity 6000.3.20f1, combining `fish-audio-integration` (637ec04) and `hc/tripo-fixes` (0441cb1), plus the rebuild repair and owner-approved breathing correction. The owner explicitly authorized merging into main.

## Included

- Fish Audio Drama 3 dialogue and vocal effects on Bellwether House and Haunted Castle; offline playback with no embedded API key.
- All 40 Tripo Castle prefabs, the Tripo playtest fixes and darker Castle lighting, plus smoked-glass/gold document UI.
- The exact owner-selected `natural-player-breath-audition.wav` as the shared hiding loop. Approved master: `docs/audio/fish-integration/breath-revision-2026-10-25/approved-player-hiding-breath.wav`. Noise-vocoder processing was rejected and removed. Other regenerated breath takes use their natural performances; only the player take has explicit owner listening approval.
- Rebuild guard: legacy generators cannot recreate FBX names owned by Tripo prefabs. Incomplete imports and duplicate resource names fail closed. The checker accepts valid replacements; hall-kit metadata no longer depends on the deleted Pew FBX.

## Verified on this combined build

- Both authored lighting receipts accepted by `BuildPreparedMacAll`; successful Null-graphics build.
- Runtime/player/editor C# compile and 1,615 deterministic gameplay assertions.
- 260 Fish provenance/content/format/GUID checks, including byte equality with the approved audition; audio-key, audio-quality, Castle scare and prepared-throwable checks pass.
- Five rebuild failure-handling tests; Blender exercised 26 legacy generator paths, guarded all 40 replacement names, left 680 protected files byte-identical and still exported the retained procedural Orrery. All 40 Castle mesh payloads have valid indices/finite vertices.
- Four rendered native routes with per-process network denial: Bellwether full investigation/hunt/ending/death/retry; Castle escape; Castle fear; Castle kill. All terminal route results passed, with zero Castle blockers.
- Every observed vocal clip's decoded sample count/rate/channels matches the current source WAV. All four runs have nonzero mixed audio and zero clipped samples. Castle spoken lines route from the Count's head.
- Original player preference dictionaries restored exactly; task-owned engines closed.
- Packaged app's 153 files match the tested build; ZIP integrity and UTF-8/UTF-16 credential scan pass.

## Limits

Bellwether recorded four engine-cost frames above 16.7 ms (maximum 30.7 ms, engine-cost p99 11.4 ms); its strict performance gate is false. These were shared-GPU capture runs, not isolated performance certification.

The fear fixture uses invulnerability: 66 death frames were blocked, mostly on the return bridge. Its attempted witnessed-hide step reported `seenBeforeHiding=False`; actual detection-before-hiding remains unverified in this run. Escape and kill recorded zero prevented deaths. This is automated behavioral coverage, not owner gameplay acceptance. Captures inspected: Bellwether furnishings/bedside, Castle great hall and smoked-glass letter. The separate wall-art branch was not part of this merge.

The inherited `ui_paper` trailing-silence warning remains. Three earlier native attempts ended during audio review before their terminal result; only the four `-approved` runs establish this delivery's pass.

## Playable outputs

In this chat's `outputs/`: `Hollow-Current-Combined/Hollow Current.app` and `Hollow-Current-Combined-macOS.zip`. Choose Bellwether House or Haunted Castle from the title screen. The delivery JSON records the final GitHub main revision and ZIP hash.
