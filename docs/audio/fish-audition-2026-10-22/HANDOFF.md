# Fish Audio vocal auditions — 2026-10-22

Branch: `fish-audio-integration`. Scope confirmed by owner: both maps, all voices and vocal sounds; exclude ambience, footsteps, doors, music, and nonvocal scare effects. These are auditions for owner listening, not accepted or integrated game assets.

## Delivered

- Eight Drama 3 takes: Count stalking, discovery, and hunt; Bellwether dispatch warning, house whisper and EVP; concealed-player breathing and the Count's door breath.
- Original character reference voices from Fish `voice-design-1`: Count, dispatch operator, house presence, investigator. No real person's recording was used.
- `plan.json` preserves acting prompts. `replacement-map.json` maps all 28 existing scripted lines (17 Castle, 11 Bellwether), plus the vocal-effects scope.
- `raw/` preserves accepted Drama 3 outputs, `references/` preserves reference voices, and `*_new.wav` contains the proposed post-processing. Original production resources remain unchanged.
- Hosted TTS explicitly used the `model: drama-3` header and MessagePack inline audio references. API WAV output is 44.1 kHz; delivery is resampled to 48 kHz mono PCM16.
- Original voice clips and new clips are level-matched in the chat's before/after exports: current audio, 1.2-second pause, proposed audio. No backing music was added.

## Validation and limitations

- All eight exported takes pass `audio-validation.json`: 48 kHz mono, dialogue approximately -24.2 LUFS, breaths -28 LUFS, true peaks below -3 dBFS.
- Fish `transcribe-1-pro` checks match all six scripted lines. The two final breath takes are annotated as non-speech breathing/sighs. ASR is only a content check; it does not establish subjective scariness or character suitability.
- Initial natural-language-only breath prompts incorrectly reproduced reference speech. Those takes were rejected. The final breath takes use explicit paralinguistic tags; rejected takes are not part of this archive.
- No Unity instance, build, scene, gameplay trigger, or runtime audio resource was changed. No claim of in-game acceptance or full replacement is made.

## Pipeline

`tools/fish_audio_audition.py --output <new-output-directory> --generate` performs a bounded paid audition run. A run creates fresh voice references unless cached responses already exist. Reuse the archived `references/` voices during implementation rather than silently casting new ones.

The script uses private `~/.claude/secrets/fish-audio.key` (0600). Never copy the key into this repository or a build. Unknown request completions leave a pending marker; no automatic paid retries. Request fingerprints reject changed prompts against an existing response cache. New model outputs are nondeterministic and require listening.

## Next

Owner listens to the auditions and chooses/corrects voice direction. Then replace the 28 scripted voices and approved vocal effects while preserving resource IDs and Unity GUIDs. Keep the Castle speech-from-Count-head rule, distance/occlusion behavior, and wordless EVP/hiding textures. Retain raw masters and handle spatialization in the game. Player breathing needs loop editing and loop-boundary verification before deployment. Bellwether radio filtering and the restrained room tails are audition processing, subject to the final in-game mix.

The separate `hc/tripo-fixes` branch changes dispatch arrival/kit behavior; reconcile that branch's event policy before a later integration. This audition branch does not re-enable or suppress any dialogue event.
