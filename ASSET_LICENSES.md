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

`Assets/HollowCurrent/Resources/HiggsfieldAudio/` contains ten original generated music/SFX recordings. These recordings are **not covered by the code's MIT license or the CC BY grant above**. The owner permits copying, modification, redistribution and commercial use of these recordings in games and other creative works, with credit to “Hollow Current by Ashen — audio generated with Higgsfield”. Do not use them to train, fine-tune, distill or improve AI/ML models. Preserve this notice with redistributions.

[Higgsfield Terms §4.4](https://higgsfield.ai/terms-of-use-agreement) state that Higgsfield does not claim output ownership or restrict commercial use; §§5.1/5.2 restrict using outputs for AI training. [Provider ownership guidance](https://higgsfield.ai/creator-hub/help-center/account/who-owns-my-generations-and-can-i-use-them-commercially) describes surviving and transferable/sublicensable output rights. This limited asset grant deliberately does not promise unrestricted AI-training rights. The source code remains open source; the media set has these separate terms.

## Excluded or independently obtained material

- Original macOS Daniel/Moira system-voice recordings and dry takes: excluded. Apple's [macOS license §2](https://www.apple.com/legal/sla/docs/macOSTahoe.pdf) limits System Voice original-content use to personal, noncommercial projects. Public replacement files use eSpeak NG; the private originals remain preserved outside this repository.
- System fonts: no font binaries are shipped. The UI requests available OS fonts and falls back through Unity; install fonts only under their own licenses.
- Unity Editor/Player binaries, Unity package caches, build caches and installers: excluded. Install Unity and restore the versioned package manifest under Unity's and the packages' respective terms.
- Provider account exports, access tokens, billing ledgers, signed download URLs, local operational history, unused HD masters, runtime logs and local paths: excluded.

No assertion is made that provider terms are an infringement warranty. The review establishes recorded provenance and permission bases for these specific project assets, not rights to unrelated third-party content or provider services.
