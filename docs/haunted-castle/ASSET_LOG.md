# Haunted Castle asset log (audio, piece 6)

Paid generations: Higgsfield CLI `higgsfield generate create seed_audio --prompt ... --sample_rate 48000 --format wav --wait --json`, one job at a time, 2026-10-13. Credits are the per-job `spend` rows from `higgsfield account transactions` (the CLI cost estimate returned 0.1 and was wrong). Raw downloads are stereo 48 kHz and are not committed; `tools/audio_castle_post.py` makes the committed mono 48 kHz WAV (trim silence, gain to -30 LUFS integrated, -1.5 dBFS ceiling, 0.3 s crossfade for loops). Account has other sessions spending credits, so balance deltas are not used.

| Clip | Model | Credits | Job id | Prompt | File |
|---|---|---|---|---|---|
| wing_beats_loop | seed_audio (Seed Audio 1.0) | 1.7 | `5c335f14-8353-48e1-bef6-a7a8874a7fea` | Sound effect only, no music, no speech. Slow heavy leathery bat-wing beats of a giant creature hovering in a stone hall: deep rhythmic whumps of membrane catching air, about one beat every 0.8 seconds, dry close recording, seamless loop, about 6 seconds. | `Assets/HollowCurrent/Resources/CastleAudio/wing_beats_loop.wav` (5.57 s) |
| dive_whoosh | seed_audio (Seed Audio 1.0) | 1.6 | `2e0c964c-1f40-41f9-a06c-464fa4d4abfa` | Sound effect only, no music, no speech. A huge winged predator folds its wings and dives fast past the listener: rising rush of air, a sharp tearing whoosh at the pass, then a falling doppler tail. Dry, cinematic, about 2 seconds. | `Assets/HollowCurrent/Resources/CastleAudio/dive_whoosh.wav` (3.33 s) |
| wing_wrap_grab | seed_audio (Seed Audio 1.0) | 1.9 | `b55a2327-30b2-4364-abfa-7384879441c8` | Sound effect only, no music, no speech. A giant vampire's leathery wings snap shut around the listener: a sudden heavy membrane flap, a thick cloth-and-leather wrap with a creak of tendon and a muffled thud as the world goes close and dull. Dry, close, about 1.5 seconds. | `Assets/HollowCurrent/Resources/CastleAudio/wing_wrap_grab.wav` (1.44 s) |
| stagger_screech | seed_audio (Seed Audio 1.0) | 1.7 | `0f15dd9d-c71a-4d06-b6c5-7f3a3c052412` | Sound effect only, no music, no words. A monstrous winged vampire lord screeches in pain and rage after being struck by a holy blade: a harsh inhuman shriek with a rasping, tearing overtone that breaks into a ragged hiss, hall reverb. About 2.5 seconds. | `Assets/HollowCurrent/Resources/CastleAudio/stagger_screech.wav` (3.86 s) |
| death_wail_sunrise | seed_audio (Seed Audio 1.0) | 2.0 | `9e1b3219-518f-4d3a-b664-9aa67d6e480f` | Sound effect and ambience only, no words. A vampire lord's long dying wail that falls in pitch and dissolves into airy dust, then a warm, hopeful orchestral string-and-choir swell rising like sunrise through a stained glass window, ending on a soft sustained major chord. About 9 seconds. | `Assets/HollowCurrent/Resources/CastleAudio/death_wail_sunrise.wav` (8.92 s) |
| chapel_bell_toll | seed_audio (Seed Audio 1.0) | 1.6 | `3d81d29f-99c7-4995-b2ce-25fa6eeb633b` | Sound effect only, no music, no speech. A single deep toll of a large old bronze church bell in a stone chapel, strong clapper strike, long ominous humming decay with slow beating overtones, natural stone reverb. About 7 seconds. | `Assets/HollowCurrent/Resources/CastleAudio/chapel_bell_toll.wav` (6.90 s) |
| forge_hold_hum | seed_audio (Seed Audio 1.0) | 1.9 | `90a0534b-effd-4708-b482-eec3af3d4f4d` | Sound effect only, no music, no speech. A low magical resonant hum of enchanted metal being forged at a stone altar: steady deep 110 Hz drone with a shimmering rising harmonic overtone and faint crackling sparks, constant and sustained so it can loop seamlessly. About 5 seconds. | `Assets/HollowCurrent/Resources/CastleAudio/forge_hold_hum.wav` (6.12 s) |
| drawbridge_chains | seed_audio (Seed Audio 1.0) | 1.7 | `f34e4f5f-d38e-4b13-9525-488797b38c0a` | Sound effect only, no music, no speech. Heavy iron chains grinding and clanking over a stone winch as a huge wooden drawbridge slowly lowers: rhythmic link-by-link clanks, deep wood and stone groaning, ending in a heavy thud as it lands. About 8 seconds. | `Assets/HollowCurrent/Resources/CastleAudio/drawbridge_chains.wav` (7.92 s) |
| candle_whisper_bed | seed_audio (Seed Audio 1.0) | 2.0 | `dcf0e64b-4be5-4d44-b2b1-6d347313f949` | Ambient sound bed only, no intelligible words. Dozens of floating candles in a vast dark hall: faint overlapping airy whispering breaths and soft candle-flame flutters, distant faint chimes of glass, constant gentle eerie texture without any distinct events, seamless loop. About 8 seconds. | `Assets/HollowCurrent/Resources/CastleAudio/candle_whisper_bed.wav` (7.53 s) |
| crypt_blood_trickle | seed_audio (Seed Audio 1.0) | 1.8 | `8ebb04d9-a782-4810-a797-2002f8ef3867` | Sound effect only, no music, no speech. A thick slow liquid trickling and gurgling along a narrow stone channel in a damp crypt: soft wet drips, viscous running trickle, small gulps, echoing stone room tone, constant steady texture, seamless loop. About 8 seconds. | `Assets/HollowCurrent/Resources/CastleAudio/crypt_blood_trickle.wav` (7.54 s) |
| orrery_tick | none (synthesised locally, `tools/audio_orrery_tick.py`) | 0 | n/a | n/a | `Assets/HollowCurrent/Resources/CastleAudio/orrery_tick.wav` (0.28 s) |

Total Seed Audio spend: 17.9 credits over 10 jobs. Job 5 returned HTTP 503 after it was accepted and charged; it was recovered with `higgsfield generate wait <id>` (no duplicate job).

Voice lines (superseded 2026-10-15: the castle voice is now Kokoro-82M, see the last section; the eSpeak recipe follows for history): no paid generation. 15 `cc_*` entries (now 18 with `cc_kit`, `cc_ending`, `cc_contain`) in `tools/voice-lines.json` rendered by `python3 tools/generate_open_voice.py --castle` (eSpeak NG `en-gb-x-rp+m7`, pitch 14-18, 112-132 wpm, ffmpeg 0.88x pitch drop plus a six-tap stone-hall echo chain) into `Assets/HollowCurrent/Resources/CastleAudio/voice_cc_*.wav`.

## Count Claude (piece 4a, merged from ASSET_LOG_count.md)

Paid generations, one job at a time, 2026-10-13, Higgsfield CLI 1.1.23. Credits are the `spend` rows from `higgsfield account transactions` (balance deltas are polluted by the audio session spending in parallel, and `generate cost` is not supported for these two models).

| Step | Model | Credits | Job id | Inputs | Output |
|---|---|---|---|---|---|
| Mesh + texture | `multi_image_to_3d` (Multi-Image to 3D) | 30 | `0599a54a-d8de-42fe-a9cb-b3a3ec94d655` | `docs/haunted-castle/refs/count_front.png`, `count_side.png` (trimmed to 765 px wide to drop the back-view sliver), `count_back.png`; `--should_texture true --enable_rigging false --enable_animation false` (the CLI rejects the call unless those two are passed explicitly) | `tools/count_source/CountClaude_mesh.glb` (4.9 MB, 30,696 tris, 2048 px base-colour texture). Record: `refs/count_mesh.job.json` |
| Rig | `3d_rigging` (3D Rigging) | 5 | `325ca2e8-31f1-4706-9063-ae103a0b714d` | the mesh job's GLB URL, `--enable_animation false` | `tools/count_source/CountClaude_rigged.glb` (6.3 MB, 24-bone Meshy-style humanoid, no wing bones). Record: `refs/count_rig.job.json` |

Total: 35 credits, 2 jobs (second mesh job not needed: wings, hooked tips, coat and silhouette all present).

Animation clips: the Higgsfield animation library (678 humanoid actions, `animation_action_id`) was **not** used. It is biped-only (the wings would not move), one action per paid job, and none of the six modes exist in it. All six clips are authored locally by `tools/count_prepare.py` (free, regenerable).

Local processing: `tools/count_prepare.py` re-skins the wings onto four new bones (`WingL`, `WingTipL`, `WingR`, `WingTipR`; the auto-rig had weighted 10k wing vertices to the leg bones), rescales to a 2.15 m head, grounds the feet, and writes `Assets/HollowCurrent/Resources/Characters/CountClaude/Native/*.fbx`, `Maps/Albedo.png` and `Native/CountClaude.json`.

## Count Claude demon redesign (piece 4, round 6, `hc/count6`)

Higgsfield CLI 1.1.23, 2026-10-13, one job at a time. Credits from `higgsfield account transactions`.

| Asset | Model | Credits (net) | Job id | Inputs | Result |
|---|---|---|---|---|---|
| Demon mesh, attempt 1 | `multi_image_to_3d` | 30 spent, 30 refunded = **0** | `ae97187c-4128-4440-b0d5-edec63f27a8c` | `refs/demon_front.png`, `demon_side.png` (trimmed to 690 px wide to drop the back-view sliver), `demon_back.png`; `--should_texture true --enable_rigging false --enable_animation false` | **Refused: status `nsfw`** (provider moderation). No asset. |
| Demon mesh, attempt 2 (front + back only) | `multi_image_to_3d` | 30 spent, 30 refunded = **0** | `d9c5eafa-b063-4a0c-b20b-69c5f8021871` | `demon_front.png`, `demon_back.png`, same flags | **Refused: status `nsfw`**. No asset. Stopped per the one-retry rule; the `3d_rigging` job was not run. |

Account note: the first 30-credit spend triggered an **Auto Top-Up grant of 400 credits** (2026-10-13T21:47:26Z) on the owner's account; the balance went from 147.64 to 547.64 after both refunds.
No `ASSET_LICENSES.md` line was added because no asset was produced.

## Count Claude demon (piece 4, round 7, `hc/count7`): Customuse

Customuse MCP (`~/.config/customuse/mcp.sh`), owner account, 2026-10-13. Workflow `79358063-a4bd-4a8e-ac51-50effc66655b`
("Hollow Current - Count Claude demon (r7)"). Credits come from `customuse_get_credits_balance` before and after each run.

| Step | Node / model | Credits before → after | Inputs | Output |
|---|---|---|---|---|
| Upload ×3 | `imageUpload` (`customuse_upload_media`, base64 JPEG q93) | 4621 → 4621 | `demon_front.png`, `demon_side.png` (trimmed to 690 px to drop the back-view sliver; the character's left side, wired to `left-image-in`), `demon_back.png` | nodes `0fc62738…`, `48bca203…`, `f2657b22…` (public.customuse.com URLs) |
| Mesh | `tripoV31Mesh3d` (Tripo v3.1), multiview, faceLimit 40000, 4K, isPrivate true; node `5fe8bc94-6622-4ac2-855c-0805353115b3` | 4621 → **4521 (100)** | front, left, back | `https://public.customuse.com/uploader/files/8f84b41c-5b74-4ba9-9641-c472863bc6e5/633dacc0-8a02-4686-bda6-f83441cca8de.glb` → `tools/count_source/CountClaude_demon_mesh.glb` (38,933 tris, **untextured**, 1.0 m tall, faces +X) |
| Clean-up + rig | `meshCleanup` (node `1a7e4202…`) cascaded into `autoRigging` (provider tripo, biped; node `c45974cb-7419-400d-8402-99ab2b6f2f2c`) | 4521 → **4486 (35, both nodes)** | the mesh | `https://public.customuse.com/uploader/files/dd160eb0-cf91-4d43-b752-ff8f43bc09d7/bd938e6c-4639-424c-9685-e40f00ce0b06.glb` → `tools/count_source/CountClaude_demon_rigged.glb` (38,875 tris, 117 generic bones incl. two 4-finger wing chains) |

Total: **135 credits**. Not moderated (the round-6 Higgsfield attempts were refused as `nsfw`). The URLs are public even though
`isPrivate` was set on the mesh node. The texture was not generated by the provider; `tools/count_project_refs.py` projects the
reference views locally (free) into `tools/count_source/CountClaude_demon_albedo.png` (2048², 5.9 MB).

## Fear wave audio (piece F4, `hc/audio2`): procedural, no cost

2026-10-13. **No paid generation; cost 0.** Generator: `tools/audio_castle_sfx.py` (numpy/scipy damped modal synthesis, filtered noise,
synthetic-reverb convolution; deterministic seeds; loops are periodic by construction). Voices: `tools/generate_open_voice.py --castle`
(eSpeak NG + ffmpeg) from the new `cc_kit` and `cc_ending` entries in `tools/voice-lines.json`.

| Clip | Source | Cost |
|---|---|---|
| `heartbeat_loop`, `wind_battlements_loop`, `door_rattle`, `book_drop`, `knife_stick`, `claw_steps_0..3`, `bat_pass` | `tools/audio_castle_sfx.py` (fully synthetic) | 0 |
| `bell_single` | `tools/audio_castle_sfx.py` cuts one strike out of the existing `chapel_bell_toll.wav` (derived from the Higgsfield bell; no new job) | 0 |
| ~~`voice_cc_kit`, `voice_cc_ending`~~ (retired 2026-10-15, see "Retired 2026-10-15" below) | eSpeak NG, Count treatment from `CASTLE_FX` | 0 |
| ~~`voice_cc_contain`~~ (retired 2026-10-15) | eSpeak NG, Count treatment from `CASTLE_FX` (added 2026-10-14 on `hc/throws`: `python3 tools/generate_open_voice.py --castle cc_contain`; the same pipeline reproduces the committed `voice_cc_kit.wav` byte for byte with eSpeak NG 1.52.0) | 0 |

No `ASSET_LICENSES.md` line is needed: everything here is generated from our own scripts (eSpeak NG output is redistributable per the existing voice entries).

## Title screen key art (piece T1, `hc/title`)

2026-10-13, Higgsfield CLI, one job, `docs/haunted-castle/gen.sh title_1 16:9`. The owner rejected the procedural title backdrop ("PS2 slop"); this replaces `Resources/UI/CastleBackdrop.png`.

| Job id | Model | Credits | Prompt | Output |
|---|---|---|---|---|
| `cc99ba72-efb5-432e-9108-49f1baabb55b` | `gpt_image_2_5` (2k, high, 16:9; delivered 2688x1520) | 2.75 (`higgsfield account transactions` spend row) | `refs/title_1.prompt.txt` | Full-res PNG not committed (6.2 MB, regenerate with gen.sh). Committed: `refs/title_1.preview.webp` (1280 wide), `refs/title_1.job.json`. Shipped: `Resources/UI/CastleBackdrop.png` (centre-cropped to 16:9, Lanczos to 2560x1440, 4.9 MB; importer max size raised to 4096) |

Accepted on the first job (no second job needed): no text, painterly, mean luma of the left 30 % at 1920 wide = 19.1 (limit 60), one small winged silhouette on the tallest tower against the white moon, two moons (bone-white left, blood-red right).

## Castle audio inventory with licence and listening status (wave G, `hc/audio3`, 2026-10-14)

Cost this wave: **0** (no paid generation). New this wave: `voice_cc_hint_signal`, `voice_cc_hint_trace`, `voice_cc_hint_voice`, `voice_cc_dispatch`
(eSpeak NG, same recipe as `voice_cc_kit`). **Human listen = `not yet` for every clip**: no person has played any of these in the game; the
critic or owner replaces the cell with a name and date once a clip is cleared. Missing-key check: `python3 tools/check_audio_keys.py`.

| Clip | Origin / generator | Licence | Human listen |
|---|---|---|---|
| `bat_pass.wav` | `tools/audio_castle_sfx.py` (numpy/scipy synthesis, deterministic) | Project CC BY 4.0 (own script, no third-party samples) | not yet |
| `bell_single.wav` | Derived from `chapel_bell_toll.wav` by `tools/audio_castle_sfx.py` (one strike, tail cut to 3 s) | Same Higgsfield grant as its source (derivative) | not yet |
| `book_drop.wav` | `tools/audio_castle_sfx.py` (numpy/scipy synthesis, deterministic) | Project CC BY 4.0 (own script, no third-party samples) | not yet |
| `candle_whisper_bed.wav` | Higgsfield Seed Audio 1.0 (job in the section above; post: `tools/audio_castle_post.py`) | Higgsfield grant in `ASSET_LICENSES.md` (credit "audio generated with Higgsfield"; no AI/ML training use) | not yet |
| `chapel_bell_toll.wav` | Higgsfield Seed Audio 1.0 (job in the section above; post: `tools/audio_castle_post.py`) | Higgsfield grant in `ASSET_LICENSES.md` (credit "audio generated with Higgsfield"; no AI/ML training use) | not yet |
| `claw_steps_0.wav` | `tools/audio_castle_sfx.py` (numpy/scipy synthesis, deterministic) | Project CC BY 4.0 (own script, no third-party samples) | not yet |
| `claw_steps_1.wav` | `tools/audio_castle_sfx.py` (numpy/scipy synthesis, deterministic) | Project CC BY 4.0 (own script, no third-party samples) | not yet |
| `claw_steps_2.wav` | `tools/audio_castle_sfx.py` (numpy/scipy synthesis, deterministic) | Project CC BY 4.0 (own script, no third-party samples) | not yet |
| `claw_steps_3.wav` | `tools/audio_castle_sfx.py` (numpy/scipy synthesis, deterministic) | Project CC BY 4.0 (own script, no third-party samples) | not yet |
| `crypt_blood_trickle.wav` | Higgsfield Seed Audio 1.0 (job in the section above; post: `tools/audio_castle_post.py`) | Higgsfield grant in `ASSET_LICENSES.md` (credit "audio generated with Higgsfield"; no AI/ML training use) | not yet |
| `death_wail_sunrise.wav` | Higgsfield Seed Audio 1.0 (job in the section above; post: `tools/audio_castle_post.py`) | Higgsfield grant in `ASSET_LICENSES.md` (credit "audio generated with Higgsfield"; no AI/ML training use) | not yet |
| `dive_whoosh.wav` | Higgsfield Seed Audio 1.0 (job in the section above; post: `tools/audio_castle_post.py`) | Higgsfield grant in `ASSET_LICENSES.md` (credit "audio generated with Higgsfield"; no AI/ML training use) | not yet |
| `door_rattle.wav` | `tools/audio_castle_sfx.py` (numpy/scipy synthesis, deterministic) | Project CC BY 4.0 (own script, no third-party samples) | not yet |
| `drawbridge_chains.wav` | Higgsfield Seed Audio 1.0 (job in the section above; post: `tools/audio_castle_post.py`) | Higgsfield grant in `ASSET_LICENSES.md` (credit "audio generated with Higgsfield"; no AI/ML training use) | not yet |
| `forge_hold_hum.wav` | Higgsfield Seed Audio 1.0 (job in the section above; post: `tools/audio_castle_post.py`) | Higgsfield grant in `ASSET_LICENSES.md` (credit "audio generated with Higgsfield"; no AI/ML training use) | not yet |
| `heartbeat_loop.wav` | `tools/audio_castle_sfx.py` (numpy/scipy synthesis, deterministic) | Project CC BY 4.0 (own script, no third-party samples) | not yet |
| `knife_stick.wav` | `tools/audio_castle_sfx.py` (numpy/scipy synthesis, deterministic) | Project CC BY 4.0 (own script, no third-party samples) | not yet |
| `orrery_tick.wav` | `tools/audio_orrery_tick.py` (synthetic) | Project CC BY 4.0 (own script) | not yet |
| `stagger_screech.wav` | Higgsfield Seed Audio 1.0 (job in the section above; post: `tools/audio_castle_post.py`) | Higgsfield grant in `ASSET_LICENSES.md` (credit "audio generated with Higgsfield"; no AI/ML training use) | not yet |
| `voice_cc_bargain.wav` | `tools/generate_castle_voice.py cc_bargain` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_hunt_found.wav` | `tools/generate_castle_voice.py cc_hunt_found` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_hunt_run.wav` | `tools/generate_castle_voice.py cc_hunt_run` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_hunt_smell.wav` | `tools/generate_castle_voice.py cc_hunt_smell` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_ledger_orrery.wav` | `tools/generate_castle_voice.py cc_ledger_orrery` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_ledger_portrait.wav` | `tools/generate_castle_voice.py cc_ledger_portrait` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_ledger_steward.wav` | `tools/generate_castle_voice.py cc_ledger_steward` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_listen.wav` | `tools/generate_castle_voice.py cc_listen` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_relic_blade.wav` | `tools/generate_castle_voice.py cc_relic_blade` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_relic_hilt.wav` | `tools/generate_castle_voice.py cc_relic_hilt` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_relic_moon.wav` | `tools/generate_castle_voice.py cc_relic_moon` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_stay.wav` | `tools/generate_castle_voice.py cc_stay` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_steward.wav` | `tools/generate_castle_voice.py cc_steward` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `wind_battlements_loop.wav` | `tools/audio_castle_sfx.py` (numpy/scipy synthesis, deterministic) | Project CC BY 4.0 (own script, no third-party samples) | not yet |
| `wing_beats_loop.wav` | Higgsfield Seed Audio 1.0 (job in the section above; post: `tools/audio_castle_post.py`) | Higgsfield grant in `ASSET_LICENSES.md` (credit "audio generated with Higgsfield"; no AI/ML training use) | not yet |
| `wing_wrap_grab.wav` | Higgsfield Seed Audio 1.0 (job in the section above; post: `tools/audio_castle_post.py`) | Higgsfield grant in `ASSET_LICENSES.md` (credit "audio generated with Higgsfield"; no AI/ML training use) | not yet |

### Retired 2026-10-15 (wave FB, fb-voice2): nine `voice_cc_*` clips

`integ-fb` (commit 7eb25f9) removed nine Count voice lines and their `tools/voice-lines.json` entries per `fb-voice2` (owner item 13: nothing should be spoken as if from a house-wide speaker; the castle now voices only located Count and `cc_`/`evp`/`whisper` lines, see `handoffs/fb-voice2.md`): `voice_cc_hunt_hungry`, `voice_cc_dispatch`, `voice_cc_night`, `voice_cc_kit`, `voice_cc_ending`, `voice_cc_contain`, `voice_cc_hint_signal`, `voice_cc_hint_trace`, `voice_cc_hint_voice`. The `.wav` files are no longer in `Resources/CastleAudio`; `python3 tools/audio_castle_sfx.py --check` no longer expects the seven it used to own (`VOICES` is empty, `RETIRED_VOICES` lists them). Their rows are kept here as history only (the file and the original licence statement no longer ship, nothing to redistribute):

| Clip (retired) | Origin / generator when shipped | Licence when shipped | Human listen |
|---|---|---|---|
| `voice_cc_contain.wav` | `tools/generate_castle_voice.py cc_contain` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_dispatch.wav` | `tools/generate_castle_voice.py cc_dispatch` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_ending.wav` | `tools/generate_castle_voice.py cc_ending` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_hint_signal.wav` | `tools/generate_castle_voice.py cc_hint_signal` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_hint_trace.wav` | `tools/generate_castle_voice.py cc_hint_trace` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_hint_voice.wav` | `tools/generate_castle_voice.py cc_hint_voice` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_hunt_hungry.wav` | `tools/generate_castle_voice.py cc_hunt_hungry` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_kit.wav` | `tools/generate_castle_voice.py cc_kit` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |
| `voice_cc_night.wav` | `tools/generate_castle_voice.py cc_night` (Kokoro-82M `bm_george` + dark hall FX chain; replaced the eSpeak NG render on `hc/fb-audio`, 2026-10-15) | Line text: project CC BY 4.0. Voice model: Kokoro-82M, Apache-2.0, output redistributable (`ASSET_LICENSES.md`) | not yet (owner pending) |

## Wave K audio repair (`hc/audio4`, 2026-10-14, from the r11 critic): cost 0

CPU-only, in place on the committed WAVs; `tools/audio_castle_fix.py` (crossfade loops, trim voice and bell tails, set voice loudness to the Bellwether median of -24.2 LUFS) and the gate `tools/check_audio_quality.py`. The three repaired loops are Higgsfield Seed Audio outputs (jobs in the sections above); the crossfaded versions are derivatives under the same grant. Every clip keeps **Human listen = not yet** (see the inventory above; nothing was listened to, results are measurements only).

| clip | dur s | LUFS | peak dBFS | tail s | wrap step |
|---|---|---|---|---|---|
| crypt_blood_trickle | 7.54 -> 7.24 | -30.0 -> -30.0 | -12.5 -> -12.5 | 0.00 -> 0.00 | 0.0327 -> 0.0006 |
| forge_hold_hum | 6.12 -> 5.82 | -29.9 -> -29.9 | -13.2 -> -13.2 | 0.00 -> 0.00 | 0.0124 -> 0.0008 |
| candle_whisper_bed | 7.53 -> 7.23 | -30.0 -> -29.9 | -12.2 -> -12.2 | 0.00 -> 0.00 | 0.0081 -> 0.0000 |
| bell_single | 3.00 -> 2.85 | -30.0 -> -30.0 | -14.9 -> -14.9 | 0.30 -> 0.15 | 0.0000 -> 0.0002 |
| voice_cc_bargain | 9.14 -> 7.54 | -29.0 -> -24.2 | -12.3 -> -7.5 | 1.75 -> 0.06 | 0.0008 -> 0.0014 |
| ~~voice_cc_contain~~ (retired) | 9.06 -> 7.46 | -29.4 -> -24.2 | -12.4 -> -7.2 | 1.75 -> 0.10 | 0.0003 -> 0.0005 |
| ~~voice_cc_dispatch~~ (retired) | 8.24 -> 6.64 | -28.6 -> -24.2 | -12.0 -> -7.6 | 1.75 -> 0.15 | 0.0000 -> 0.0000 |
| ~~voice_cc_ending~~ (retired) | 10.42 -> 8.84 | -29.6 -> -24.2 | -11.4 -> -6.0 | 1.73 -> 0.15 | 0.0000 -> 0.0000 |
| ~~voice_cc_hint_signal~~ (retired) | 10.03 -> 8.44 | -28.6 -> -24.2 | -11.1 -> -6.8 | 1.74 -> 0.13 | 0.0000 -> 0.0000 |
| ~~voice_cc_hint_trace~~ (retired) | 9.54 -> 7.97 | -28.8 -> -24.2 | -12.0 -> -7.5 | 1.71 -> 0.15 | 0.0000 -> 0.0000 |
| ~~voice_cc_hint_voice~~ (retired) | 9.24 -> 7.48 | -28.5 -> -24.2 | -11.0 -> -6.7 | 1.92 -> 0.06 | 0.0005 -> 0.0009 |
| voice_cc_hunt_found | 3.76 -> 2.22 | -30.3 -> -24.2 | -13.5 -> -7.4 | 1.69 -> 0.15 | 0.0000 -> 0.0000 |
| ~~voice_cc_hunt_hungry~~ (retired) | 6.44 -> 4.89 | -28.9 -> -24.2 | -12.8 -> -8.1 | 1.69 -> 0.15 | 0.0000 -> 0.0000 |
| voice_cc_hunt_run | 6.27 -> 4.72 | -29.1 -> -24.2 | -12.6 -> -7.9 | 1.69 -> 0.15 | 0.0002 -> 0.0004 |
| voice_cc_hunt_smell | 5.14 -> 3.60 | -28.8 -> -24.2 | -13.7 -> -9.2 | 1.69 -> 0.14 | 0.0009 -> 0.0014 |
| ~~voice_cc_kit~~ (retired) | 9.63 -> 8.08 | -30.3 -> -24.2 | -11.6 -> -5.5 | 1.69 -> 0.15 | 0.0000 -> 0.0000 |
| voice_cc_ledger_orrery | 18.10 -> 16.71 | -29.2 -> -24.2 | -11.8 -> -6.7 | 1.54 -> 0.15 | 0.0000 -> 0.0000 |
| voice_cc_ledger_portrait | 17.42 -> 16.03 | -29.1 -> -24.2 | -12.5 -> -7.7 | 1.54 -> 0.15 | 0.0000 -> 0.0000 |
| voice_cc_ledger_steward | 18.35 -> 16.91 | -29.0 -> -24.2 | -12.4 -> -7.6 | 1.59 -> 0.11 | 0.0003 -> 0.0005 |
| voice_cc_listen | 7.17 -> 5.57 | -28.6 -> -24.2 | -12.3 -> -7.8 | 1.75 -> 0.06 | 0.0008 -> 0.0013 |
| ~~voice_cc_night~~ (retired) | 7.88 -> 6.43 | -28.1 -> -24.2 | -13.4 -> -9.5 | 1.60 -> 0.15 | 0.0000 -> 0.0000 |
| voice_cc_relic_blade | 8.87 -> 7.40 | -29.4 -> -24.2 | -12.6 -> -7.4 | 1.63 -> 0.13 | 0.0000 -> 0.0000 |
| voice_cc_relic_hilt | 9.77 -> 8.32 | -29.6 -> -24.2 | -13.3 -> -7.9 | 1.60 -> 0.15 | 0.0002 -> 0.0003 |
| voice_cc_relic_moon | 8.39 -> 6.89 | -28.2 -> -24.2 | -12.8 -> -8.8 | 1.64 -> 0.13 | 0.0002 -> 0.0002 |
| voice_cc_stay | 9.00 -> 7.55 | -28.6 -> -24.2 | -12.0 -> -7.5 | 1.60 -> 0.15 | 0.0000 -> 0.0000 |
| voice_cc_steward | 9.29 -> 7.84 | -29.4 -> -24.2 | -13.2 -> -8.0 | 1.60 -> 0.15 | 0.0000 -> 0.0000 |

## Round 12 art props (piece `hc/art-props`): one CustomUse job (rejected), everything shipped is procedural

2026-10-14. Hand-roots, books, relic sockets and the giant skeleton are modelled in headless Blender by `tools/castle_props_r12.py`
(Blender CPU, no network) with textures from `tools/castle_r12_textures.py` (numpy + Pillow, fixed seeds). The only paid generation was
one CustomUse job for the hand base mesh; the result was too poor to ship, so the hand is sculpted procedurally (`add_hand`).

| Step | Node / model | Credits before → after | Inputs | Output |
|---|---|---|---|---|
| Upload | `imageUpload` (`customuse_upload_media`, base64 JPEG q93), workflow `a895a933-7b8e-454e-864e-6bffd8dba22a` ("Hollow Current - hand-root (r12 art-props)") | 4486 → 4486 | `refs/hand_root_crop.jpg` (one hand and wrist crop of `refs/crypt.png`, background masked to flat grey, 900 px) | node `3d82e6c8-e60d-4bc1-9c28-bf3f2f9377df` |
| Mesh | `tripoV31Mesh3d` (Tripo v3.1), single image, faceLimit 15000, 2K, isPrivate true; node `7c1895e2-bafb-4aa6-8b51-0f1c755352da` | 4486 → **4386 (100)** | the crop | `https://public.customuse.com/uploader/files/582806b9-6e5e-42f4-a503-4d55170d9545/4ecaa09c-33a7-4eaf-b510-7ee815e604c3.glb` (14,488 tris, untextured) |

Review: **rejected**. The fingers fused into a paddle with one detached stub, the wrist was a flat-cut tube, and there was no usable finger
separation to decimate (`meshDecimation` was not run; there was nothing to keep at 4k tris). One job only, per the one-paid-job rule. The
GLB is not committed. Total CustomUse spend for this piece: **100 credits** (Tripo v3.1). Licence: [Customuse terms §4](https://customuse.com/terms) as
for the Count (see `ASSET_LICENSES.md`); nothing generated ships, so no file needs a licence line beyond the project's own.

Shipped art (all original project work, CC BY 4.0, no generated mesh): `HandRoot0..5.fbx`, `RootCrown.fbx`, `FloatingBook{Small,,Large}.fbx`,
`BookClosed{Small,,Large}.fbx`, `RelicSocket{Blade,Hilt,Moon}.fbx`, `GiantSkeleton.fbx`, and nine materials: `Castle_BookLeatherA/B/C`,
`Castle_BookPages`, `Castle_GiltWorn`, `Castle_RootBark`, `Castle_RootFlesh`, `Castle_BoneGrime`, `Castle_NicheStone`.

## Wave L hidden-player beats (`hc/audio-l`, 2026-10-14): cost 0

Requested by F3 after the r12 critic. CPU-only synthesis (damped modal impacts, shaped noise, formant-filtered whisper noise, synthetic reverb). Human listen is **not yet** for every clip: nothing here has been heard by a person, measurements only (`python3 tools/check_audio_quality.py`, `check_audio_keys.py`). Voices were deliberately not re-rendered at 48 kHz (reasons in `AUDIO.md`, section "Hidden-player beats and voice sample rate").

| Clip | Origin / generator | Licence | Dur s | LUFS | Peak dBFS | Human listen |
|---|---|---|---|---|---|---|
| `castle_hide_breath_0.wav` | `tools/audio_castle_sfx.py` (numpy/scipy synthesis, deterministic seeds), then `tools/audio_castle_fix.py` tail trim | Project CC BY 4.0 (own script, no third-party samples) | 4.32 | -28.0 | -13.1 | not yet |
| `castle_hide_breath_1.wav` | `tools/audio_castle_sfx.py` (numpy/scipy synthesis, deterministic seeds), then `tools/audio_castle_fix.py` tail trim | Project CC BY 4.0 (own script, no third-party samples) | 3.50 | -28.0 | -12.8 | not yet |
| `castle_hide_clawstep.wav` | `tools/audio_castle_sfx.py` (numpy/scipy synthesis, deterministic seeds), then `tools/audio_castle_fix.py` tail trim | Project CC BY 4.0 (own script, no third-party samples) | 1.68 | -28.0 | -8.9 | not yet |
| `castle_hide_handle.wav` | `tools/audio_castle_sfx.py` (numpy/scipy synthesis, deterministic seeds), then `tools/audio_castle_fix.py` tail trim | Project CC BY 4.0 (own script, no third-party samples) | 1.00 | -29.1 | -5.0 | not yet |
| `castle_hide_whisper.wav` | `tools/audio_castle_sfx.py` (numpy/scipy synthesis, deterministic seeds), then `tools/audio_castle_fix.py` tail trim | Project CC BY 4.0 (own script, no third-party samples) | 2.90 | -28.0 | -13.7 | not yet |

## Owner feedback FB: castle footsteps and the Count's voice (`hc/fb-audio`, 2026-10-15): cost 0

No paid generation. Owner items 1, 2 and 11 of `rounds/r13/OWNER_FEEDBACK.md`.

| Asset | Source | Licence | Human listen |
|---|---|---|---|
| `step_stone_0..3.wav`, `step_gravel_0..3.wav` (CastleAudio, 48 kHz mono, peak -14 dBFS) | `tools/audio_castle_steps.py` (numpy/scipy filtered noise events: heel, sole scuff, cloth swish, toe settle, short hall tail; deterministic seeds). No tonal component (`check_audio_quality.py` tonality rule). | Project CC BY 4.0 (own script, no third-party samples) | Human listen: owner pending |
| `voice_cc_*.wav` x22 when written (nine were retired the same day, 13 remain; see "Retired 2026-10-15") (48 kHz mono, -24.2 LUFS, rewritten texts in `tools/voice-lines.json`) | `tools/generate_castle_voice.py`: Kokoro-82M (hexgrad, Apache-2.0, v1.0 weights from Hugging Face `hexgrad/Kokoro-82M`), voice `bm_george`, speed 0.84, then 5 % pitch drop, breath layer, warm EQ, compression, stone-hall reverb. Python 3.12 venv with `kokoro 0.9.4`, `misaki 0.9.4`, `torch 2.14.1`, `spacy 3.8.16` + `en_core_web_sm 3.8.0` (MIT), `espeakng-loader 0.2.4` (espeak-ng GPL-3 is used as a phonemiser program only; no eSpeak audio is in the clips). CPU only. | Kokoro-82M: Apache-2.0; its output is redistributable. Line text: project CC BY 4.0. | Human listen: owner pending |
| `docs/haunted-castle/audition/voice_<voice>_cc_hunt_hungry.wav` (`bm_george`, `bm_lewis`, `am_michael`, `am_adam`, `bm_fable`) | `python tools/generate_castle_voice.py --audition` (same FX chain) | same as above | Human listen: owner pending |

Bellwether's eSpeak lines (`Resources/Audio/voice_*`) are unchanged. `generate_open_voice.py` no longer renders `cc_*` entries (it exits with a pointer to the Kokoro script unless `--legacy-espeak-castle` is given).

## Document / note UI (piece `hc/fb-docui`, 2026-10-15): cost 0, three OFL fonts, three procedural sheets

Owner feedback item 10 ("this letter shit is too ugly"). Nothing paid, nothing generated by a model. Fonts are unmodified Google Fonts
files; the paper is numpy + Pillow.

| Asset | Origin | Licence |
|---|---|---|
| `Resources/UI/Fonts/IMFellEnglish-Regular.ttf`, `IMFellEnglish-Italic.ttf` | `google/fonts` `ofl/imfellenglish/IMFeENrm28P.ttf`, `IMFeENit28P.ttf`, renamed only; `OFL-IMFellEnglish.txt` beside them | SIL OFL 1.1, (c) 2010 Igino Marini, no Reserved Font Name |
| `Resources/UI/Fonts/LaBelleAurore-Regular.ttf` | `google/fonts` `ofl/labelleaurore/LaBelleAurore.ttf`; `OFL-LaBelleAurore.txt` | SIL OFL 1.1, (c) 2010 Kimberly Geswein |
| `Resources/UI/Fonts/CourierPrime-Regular.ttf`, `CourierPrime-Bold.ttf` | `google/fonts` `ofl/courierprime/`; `OFL-CourierPrime.txt` | SIL OFL 1.1, (c) 2015 The Courier Prime Project Authors |
| `Resources/UI/Documents/DocLedger.jpg`, `DocLetter.jpg`, `DocField.jpg` (2048x1152, JPEG q90) | `python3 tools/docui_paper.py` (fixed seeds 1015 / 2026 / 3037): paper fibre noise, foxing, tide lines, blood, ink blots, charred edge, folds, broken seal (letter), punch holes and coffee ring (field) | Project CC BY 4.0 |

Contrast of every ink colour against the worst 0.5 % of each sheet's text zone is printed by `python3 tools/docui_paper.py --check` (body 5.7:1 ledger,
6.5:1 letter, 11.7:1 field; titles and kickers >= 4.9:1; close line >= 5.2:1). The static look proofs are `docs/haunted-castle/rounds/r13/docui_mock_*.png`
(`tools/docui_mock.py`, real note texts and fonts; PIL, not the engine).

## Period held instruments (piece `hc/fc-props`, wave FC, 2026-10-18): cost 0, no files

Owner/critic r12 ("modern props under gothic names"). The castle's three held instruments are generated in code by `Runtime/Castle/CastleInstrumentArt.cs` (Bellwether keeps its Tripo field-kit meshes unchanged). **No binary asset, no model, no generation job, no new texture file**: meshes are built at runtime from primitives (lathes and tube sweeps), the three small textures are painted at runtime from an integer hash (no files, so no `tools/` generator is needed), materials are Unity `Standard` plus the existing `CastleEmissive` / `CastleSprite` shaders. All of it is original project work, CC BY 4.0.

| Instrument | Castle name | Geometry | Vertices (printed by `HC_INSTRUMENTS`) |
|---|---|---|---|
| tool 1 (was the induction reader) | Hollow Compass | brass case, bezel and suspension ring; parchment dial (runtime texture: inked ring, 24 ticks, red 12 o'clock mark); glass glare disc; red-tipped needle; five reading pips | 1277 (measured offline with the same builder code) |
| tool 2 (was the UV lamp) | Grave-ash Lantern | brass foot, cap and bail ring; six iron cage bars; violet glass chimney (translucent sprite) round a glowing core | 1150 |
| tool 3 (was the EVP recorder) | Vellum Ear-Trumpet | flared brass horn, vellum wrap (runtime texture), three leather bindings, dark bell interior | 1026 |
