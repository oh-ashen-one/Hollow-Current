# Hiding-breath correction — 2026-10-25

Owner rejected the earlier hiding breath as sounding sexual. Six Drama 3 breath masters were regenerated without sigh/groan directions: player hiding, house presence, Count presence, two Castle hiding variants, and the hiding exhale. The unused legacy house-breath alias matches the corrected presence clip.

`tools/fish_breath_texture.py` transfers the Fish master’s slow energy envelopes onto six independent filtered noise bands, preserving breath timing while removing voiced pitch. No original voiced waveform is mixed back in. Player breath is normalized to -32 LUFS; existing routing, ducking, clip GUIDs and presence cooldown limits are preserved. Pain/death vocals and dialogue remain unchanged.

Reproduce with `tools/revise_fish_breaths.py` (cached by default; paid requests require `--generate`, maximum reservation $0.30, no automatic retries). Raw masters, requests, transcripts and usage are stored here. Run `tools/check_fish_integration.py`, `tools/check_fish_breaths.py` and `tools/check_audio_quality.py` after generation.

Mechanical tests verify wordless ASR, hashes, format, pitch correlation, peaks and seamless player looping. No listening-capable tool is available; owner listening remains the quality judgment. This revision supersedes the original six breath takes and their corresponding auditions.
