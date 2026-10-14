# Asset permissions and provenance

Review date: 2026-10-13. This project contains AI-assisted media; output uniqueness and copyright protection are not guaranteed. These grants cover only rights held by the project owner, and do not grant provider trademarks or model/software weights.

## Project asset grant

Except for the Higgsfield recordings and third-party software described below, the owner licenses the included project artwork, textures, models, animations and audio under **Creative Commons Attribution 4.0 International (CC BY 4.0)**, to the extent the owner holds copyright or related rights. Credit “Hollow Current by Ashen” and preserve the relevant provider credits below when redistributing these assets. See the [license text](https://creativecommons.org/licenses/by/4.0/legalcode.en).

| Included material | Origin and reviewed basis |
| --- | --- |
| `Assets/HollowCurrent/Art/Tripo/`, derived furniture/tool prefabs and Hearthbound character | Original project generations from Tripo. Owner confirmed on 2026-10-13 that generation occurred on a paid plan. [Tripo terms §5.2.2](https://www.tripo3d.ai/terms) and [commercial-use guidance](https://www.tripo3d.ai/help/privacy-policy/how-to-use-tripo-models-commercially) provide paid users modification, distribution and licensing rights. No community-library models are being represented as ours. |
| Split Grin character, fitted skeleton and authored motions | Customuse Hunyuan 3D Pro reconstruction of the owner's selected original AI concept, followed by local Blender optimization, skinning and animation. [Customuse terms §4](https://customuse.com/terms) retain user content rights and allow lawful personal/commercial use, subject to any additional feature or third-party terms. The recorded workflow uses a supplied original image, not a third-party template. Provider model weights/software are not included. |
| UI, surface textures, concept/reference artwork | Original project prompts through OpenAI image generation, with local integration. [OpenAI terms, Content](https://openai.com/policies/row-terms-of-use/) assign OpenAI's rights in output to the user to the extent permitted by law. No real person's portrait or protected reference-game art is included. |
| Authored architecture, hands, environmental geometry, procedural Foley, rigging and animation | Created locally for this project. Source and tool scripts are included where needed for the runtime asset pipeline. |
| `Resources/Audio/voice_*.wav` | Public-edition replacement dialogue synthesized with eSpeak NG from original written lines, then processed with FFmpeg. Reproducible with `tools/generate_open_voice.py`. eSpeak NG's GPL governs its software; generated original speech is not a copy of that software. See [eSpeak licensing](https://espeak.sourceforge.net/license.html). No eSpeak or FFmpeg binaries are bundled. |

## Higgsfield recordings: separate asset permission

`Assets/HollowCurrent/Resources/HiggsfieldAudio/` contains ten original generated music/SFX recordings (plus the ten Castle clips listed below). These recordings are **not covered by the code's MIT license or the CC BY grant above**. The owner permits copying, modification, redistribution and commercial use of these recordings in games and other creative works, with credit to “Hollow Current by Ashen — audio generated with Higgsfield”. Do not use them to train, fine-tune, distill or improve AI/ML models. Preserve this notice with redistributions.

[Higgsfield Terms §4.4](https://higgsfield.ai/terms-of-use-agreement) state that Higgsfield does not claim output ownership or restrict commercial use; §§5.1/5.2 restrict using outputs for AI training. [Provider ownership guidance](https://higgsfield.ai/creator-hub/help-center/account/who-owns-my-generations-and-can-i-use-them-commercially) describes surviving and transferable/sublicensable output rights. This limited asset grant deliberately does not promise unrestricted AI-training rights. The source code remains open source; the media set has these separate terms.

### Haunted Castle audio (`Assets/HollowCurrent/Resources/CastleAudio/`, added 2026-10-13)

The ten clips below are Higgsfield Seed Audio 1.0 outputs under the same grant and restrictions as the recordings above (credit “Hollow Current by Ashen — audio generated with Higgsfield”; no AI/ML training use). Job ids and prompts are in `docs/haunted-castle/ASSET_LOG.md`.

- `CastleAudio/wing_beats_loop.wav` — Higgsfield Seed Audio 1.0, job `5c335f14-8353-48e1-bef6-a7a8874a7fea`.
- `CastleAudio/dive_whoosh.wav` — Higgsfield Seed Audio 1.0, job `2e0c964c-1f40-41f9-a06c-464fa4d4abfa`.
- `CastleAudio/wing_wrap_grab.wav` — Higgsfield Seed Audio 1.0, job `b55a2327-30b2-4364-abfa-7384879441c8`.
- `CastleAudio/stagger_screech.wav` — Higgsfield Seed Audio 1.0, job `0f15dd9d-c71a-4d06-b6c5-7f3a3c052412`.
- `CastleAudio/death_wail_sunrise.wav` — Higgsfield Seed Audio 1.0, job `9e1b3219-518f-4d3a-b664-9aa67d6e480f`.
- `CastleAudio/chapel_bell_toll.wav` — Higgsfield Seed Audio 1.0, job `3d81d29f-99c7-4995-b2ce-25fa6eeb633b`.
- `CastleAudio/forge_hold_hum.wav` — Higgsfield Seed Audio 1.0, job `90a0534b-effd-4708-b482-eec3af3d4f4d`.
- `CastleAudio/drawbridge_chains.wav` — Higgsfield Seed Audio 1.0, job `f34e4f5f-d38e-4b13-9525-488797b38c0a`.
- `CastleAudio/candle_whisper_bed.wav` — Higgsfield Seed Audio 1.0, job `dcf0e64b-4be5-4d44-b2b1-6d347313f949`.
- `CastleAudio/crypt_blood_trickle.wav` — Higgsfield Seed Audio 1.0, job `8ebb04d9-a782-4810-a797-2002f8ef3867`.

`CastleAudio/orrery_tick.wav` is synthesised locally by `tools/audio_orrery_tick.py` (project CC BY 4.0 grant). `CastleAudio/voice_cc_*.wav` are eSpeak NG recordings of original lines (same basis as `Resources/Audio/voice_*.wav`), reproducible with `python3 tools/generate_open_voice.py --castle`.

## Excluded or independently obtained material

- Original macOS Daniel/Moira system-voice recordings and dry takes: excluded. Apple's [macOS license §2](https://www.apple.com/legal/sla/docs/macOSTahoe.pdf) limits System Voice original-content use to personal, noncommercial projects. Public replacement files use eSpeak NG; the private originals remain preserved outside this repository.
- System fonts: no font binaries are shipped. The UI requests available OS fonts and falls back through Unity; install fonts only under their own licenses.
- Unity Editor/Player binaries, Unity package caches, build caches and installers: excluded. Install Unity and restore the versioned package manifest under Unity's and the packages' respective terms.
- Provider account exports, access tokens, billing ledgers, signed download URLs, local operational history, unused HD masters, runtime logs and local paths: excluded.

No assertion is made that provider terms are an infringement warranty. The review establishes recorded provenance and permission bases for these specific project assets, not rights to unrelated third-party content or provider services.

## Higgsfield 3D: Count Claude (added 2026-10-13)

`Assets/HollowCurrent/Art/Characters/CountClaude/` (sources), `Assets/HollowCurrent/Resources/Characters/CountClaude/` (generated prefab, controller, clips) and `tools/count_source/` hold the Haunted Castle hunter: a Higgsfield `multi_image_to_3d` mesh and texture (job `0599a54a-d8de-42fe-a9cb-b3a3ec94d655`) from original GPT Image 2.5 concept views, rigged by Higgsfield `3d_rigging` (job `325ca2e8-31f1-4706-9063-ae103a0b714d`), then re-skinned, scaled and animated locally with `tools/count_prepare.py`. The generated mesh and texture fall under the same Higgsfield asset permission and restrictions as the recordings above (copying, modification, redistribution and commercial use allowed with credit to “Hollow Current by Ashen — 3D generated with Higgsfield”; no AI/ML training use). The six animation clips are original project work (CC BY 4.0). Provenance and costs: `docs/haunted-castle/ASSET_LOG.md` (Count Claude section).

## Haunted Castle art (added 2026-10-13)

Original project work, CC BY 4.0 like the rest of the project media. Produced headless by the committed generators (no downloaded packs, no paid generation; nothing to log in `docs/haunted-castle/ASSET_LOG.md`):

- `Resources/Castle/*.fbx` architecture (walls, windows, arches, vault and crypt ribs, columns, buttresses, battlements, gate arch, drawbridge and chains, stairs, bridges, floor tiles, door frames and leaves): `tools/castle_architecture.py` (Blender).
- `Resources/Castle/*.fbx` furniture (tables, chairs, throne, pews, altar, beds, bookshelves, hearths, sideboard, lectern, reliquary box, sarcophagus, coffin niche, font, pedestal): `tools/castle_furniture.py` (Blender).
- `Resources/Castle/*.fbx` props (candelabra, chapel bell, orrery, telescope, portraits, fountain, dead tree, banner, floating book, cauldron, butcher block, winch, lantern cart, giant skeleton): `tools/castle_props.py` (Blender).
- `Resources/Castle/Textures/*.png` and `Resources/Castle/Materials/*.mat` (stone, black stone, oak, brass, oxblood, silver, iron, bone, portrait canvas): `tools/castle_textures.py`.
- `Resources/Castle/Sky/*.png` and `Resources/Castle/SkyCastle.mat` (two-moon skybox): `tools/castle_sky.py`.

Shared helpers: `tools/castle_common.py`, `tools/castle_lib.py`; verification `tools/castle_verify.py`; previews `tools/castle_preview.py`; all in order: `tools/castle_build_all.sh`.
- `Resources/UI/BloodLedger/{Panel,Dialogue,Warning}.png` and `Resources/UI/CastleBackdrop.png` (castle card art and title key art): derived from the project's Mourning Note cards and the castle sky by `tools/ui_blood_ledger.py`; original project work, CC BY 4.0.
- Surreal prop textures in `Resources/Castle/Textures/` (candle flame, halo, rune, window moon): `tools/castle_surreal_textures.py`; original project work, CC BY 4.0.

## Customuse 3D: Count Claude demon (added 2026-10-13, round 7)

The Haunted Castle hunter mesh is now the owner-approved demon redesign, which supersedes the Higgsfield mesh above in the shipped
character. `tools/count_source/CountClaude_demon_mesh.glb` is a Customuse Tripo v3.1 multiview reconstruction generated on the
owner's Customuse account from the owner's original GPT Image 2.5 concept views (`docs/haunted-castle/refs/demon_front/side/back.png`).
`tools/count_source/CountClaude_demon_rigged.glb` is the same mesh after Customuse Mesh Clean-up and Auto Rig (Tripo provider). The
workflow is `79358063-a4bd-4a8e-ac51-50effc66655b`. [Customuse terms §4](https://customuse.com/terms) retain user content rights and
allow lawful personal and commercial use, subject to any feature or third-party terms; the workflow uses only the owner's supplied
images. `tools/count_source/CountClaude_demon_albedo.png` is projected locally from those concept views by `tools/count_project_refs.py`.
The skeleton remap, wing re-skin, scaling and the eight animation clips are original project work (CC BY 4.0), produced by
`tools/count_prepare.py`. Provider model weights and software are not included. Provenance and credits: `docs/haunted-castle/ASSET_LOG.md`.

## Moonstone Sword and relics (added 2026-10-13, round 9)

`MoonstoneSword`, `RelicBlade`, `RelicHilt` and `RelicMoonstone` (`Resources/Castle/*.fbx`) are original meshes modelled procedurally in
`tools/castle_props.py` (headless Blender, no downloaded assets) from the project's own concept images `docs/haunted-castle/refs/weapon.png` and
`refs/relics.png`. The materials `Castle_RuneGlow` and `Castle_Moonstone` and the moonstone textures are generated by
`tools/castle_textures.py`. Original project work, CC BY 4.0.

## Haunted Castle wall art (added 2026-10-14)

Credit: Hollow Current by Ashen. Built-in OpenAI image generation (`image_gen.imagegen`); model version not exposed. Original fictional project art, prepared with `tools/wallart_prepare.py`, under the existing CC BY 4.0 project asset grant and OpenAI output-rights basis documented above.

- `Assets/HollowCurrent/Resources/Castle/Portraits/Portrait_margrave.png` — original project prompt `CODEX_HANDOFF.md` P1 through Codex built-in imagegen; 4 generation/edit calls; Two initial generations; two pupil/alignment edits. Selected first edit; second edit overshot upward. Pupil-free; vertical mismatch remains.
- `Assets/HollowCurrent/Resources/Castle/Portraits/Portrait_margravine.png` — original project prompt `CODEX_HANDOFF.md` P2 through Codex built-in imagegen; 3 generation/edit calls; Two initial generations, then pupil/alignment edit. Pupil-free; small vertical mismatch.
- `Assets/HollowCurrent/Resources/Castle/Portraits/Portrait_steward.png` — original project prompt `CODEX_HANDOFF.md` P3 through Codex built-in imagegen; 4 generation/edit calls; Two initial generations, then two edits to lower head and remove pupil remnants. Pupil-free; eye spacing narrower than target.
- `Assets/HollowCurrent/Resources/Castle/Portraits/Portrait_astronomer.png` — original project prompt `CODEX_HANDOFF.md` P4 through Codex built-in imagegen; 2 generation/edit calls; One generation and one pupil/alignment edit. Pupil-free; small position differences.
- `Assets/HollowCurrent/Resources/Castle/Portraits/Portrait_painter.png` — original project prompt `CODEX_HANDOFF.md` P5 through Codex built-in imagegen; 3 generation/edit calls; One generation and two position edits. Retained original, which was closer to target than the edited results. Pupil-free; vertical/spacing mismatch remains.
- `Assets/HollowCurrent/Resources/Castle/Portraits/Portrait_cellarer.png` — original project prompt `CODEX_HANDOFF.md` P6 through Codex built-in imagegen; 2 generation/edit calls; One generation and one alignment edit. Pupil-free; eye spacing narrower than target.
- `Assets/HollowCurrent/Resources/Castle/Portraits/Portrait_child.png` — original project prompt `CODEX_HANDOFF.md` P7 through Codex built-in imagegen; 1 generation/edit calls; Full figure and unlit candle preserved after crop. Dress more ornate than plain linen; eyes approximately 14% down versus prompt 18%. No animated pupils on door.
- `Assets/HollowCurrent/Resources/Castle/LedgerSteward.png` — original project prompt `CODEX_HANDOFF.md` L1 through Codex built-in imagegen; 2 generation/edit calls; Edited initial complete sword to a blade with bare tang; crypt/channel/claw clue retained.
- `Assets/HollowCurrent/Resources/Castle/LedgerAstronomer.png` — original project prompt `CODEX_HANDOFF.md` L2 through Codex built-in imagegen; 1 generation/edit calls; Orrery, empty orbit circle, hilt and star charts present; moon studies more elaborate than the prompt.
- `Assets/HollowCurrent/Resources/Castle/LedgerPainter.png` — original project prompt `CODEX_HANDOFF.md` L3 through Codex built-in imagegen; 1 generation/edit calls; Portrait-door latch, struck-out eyes, milky stone and pigment smears present.
- `Assets/HollowCurrent/Resources/Castle/ClawScores.png` — original project prompt `CODEX_HANDOFF.md` C1 through Codex built-in imagegen; 1 generation/edit calls; Four gouges, crossing set and curved wing-drag; black converted to alpha by supplied script.
