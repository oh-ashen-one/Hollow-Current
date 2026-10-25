# Hiding-breath correction — 2026-10-25

Owner selected `natural-player-breath-audition.wav`. Its exact bytes are preserved as `approved-player-hiding-breath.wav` and installed as `Resources/HiggsfieldAudio/breath_hidden.wav`, the shared player hiding loop on both maps. The installer and checker enforce equality with that approved master. Do not apply noise conversion, pitch stripping, reverb or further normalization to this take.

Six Drama 3 breath masters were regenerated without sigh/groan directions: player hiding, house presence, Count presence, two Castle hiding variants, and the hiding exhale. The other five use natural Fish performances with normal level/room mixing. The unused legacy house-breath alias matches the corrected presence clip. Existing GUIDs, routing, ducking and presence cooldown limits remain intact. Dialogue and pain/death vocals are unchanged.

The owner rejected a noise-vocoder experiment as microphone scratching. That processor was removed; it is not part of this delivery. Only the natural player take has explicit owner listening approval.

`tools/revise_fish_breaths.py` reproduces from cache; new paid requests require `--generate`, with a $0.30 reservation cap and no automatic retries. Raw masters, requests, transcripts and usage are stored here. Run `tools/check_fish_integration.py` and `tools/check_audio_quality.py` after installation.
