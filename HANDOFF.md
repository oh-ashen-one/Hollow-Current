# HANDOFF: piece F0 (integrator), Haunted Castle wave F (fear plan)

Branch `haunted-castle` in `/Users/midir/hollow-current-haunted-castle` (main clone, pushed to origin). Model: Opus 5.5. Date 2026-10-14.

**Merged, in order:**
- hc/audio2 `0dc4e52`
- hc/dread `49cadd2`
- hc/scares `d50a54f`
- hc/count11 `fc0175c`
- hc/grabs `ae82eb3`, then `fef50da`
- hc/title `3d9a17e`
- hc/wallart `dab74a5`; `docs/haunted-castle/wallart_runtime.patch` is applied in `b5da27e`

Each piece's own handoff is archived in `docs/haunted-castle/handoffs/<piece>.md`: audio2, dread, scares, count11, grabs, title, wallart. Earlier rounds are in `docs/haunted-castle/handoffs/`.

## State

- **Build:** `/Users/midir/hollow-current-haunted-castle/Builds/macOS/Hollow Current.app`, with both maps.
  - It is built from `b5da27e` source; the bake outputs are committed as `34a0de9`.
  - Both bake receipts are current: Castle 5 maps, 196 probes, 17 reflections; Bellwether 4 maps, 124 probes, 9 reflections.
  - `CountClaudeImport.Prepare` logged `HC_COUNTCLAUDE_PASS`, with 10 motions and the GrabDrop/GrabCrawl reaches 1.758/1.729.
- **`python3 tools/check_source.py`:** PASS, 487 rules assertions. That includes 4 new ones for `Investigation.BeginNoTellHunt`.
- **Playthroughs:** escape, kill and fear all WON on this build with 0 blockers. Evidence and the proof table are in `docs/haunted-castle/playtest/AUTO_RUN.md`; owner notes are in `docs/haunted-castle/PLAYTEST.md`.
- **Unbuilt commit:** `8485c6a` (the playtest-driver throw probe) is committed but not built. It needs a castle re-bake and a build; the Bellwether receipt is unaffected.

## What F0 added, besides the merges

- **`Game.cs`:**
  - The castle case runs on `HuntRules.For(map, LongNight)` with real dice and the 4-argument tick. "Lit" means a candle light within 6 m; Dread recovers in the gatehouse or at the lit chapel fire.
  - The torch blacks out on a dive. Logs: `HC_WARNING`, `HC_FALSE_WARNING`, `HC_HUNT_START`.
  - An escalated warning has no dispatch line.
  - Long Night uses F3's `CastleWorld.NailHides` and removes the nailed spots from `World.HidingPlaces`.
  - After a death, the penalty calls `CastleRun.MoveRelicToAlternate`.
  - The hostile return lowers the bridge over 7.2 s.
  - Windowed launch (`HC_WINDOWED`). Scripted runs ignore stray quit, retry and Escape keys.
- **`ScareDirector.cs`:** the castle beat set:
  - Lies come first, then the window silhouette or balustrade shape, then `Beat()`.
  - Throwables are in the prop list; one throw in six is the poltergeist.
  - Dread is added on an apparition or a throw. An apparition within 4 m calls `TryEscalate`.
- **`Interface.cs`:**
  - The journal pulse line (castle only, Dread ±2, no number).
  - The CASTLE NIGHT settings page: Long Night, Microphone, and a threshold with a live level bar.
  - The castle warning countdown uses `WarningLength`.
- **`Investigator.cs`:**
  - "The door is held." when Dread is 90 or above.
  - The opt-in microphone: 16 kHz, 1 s ring, RMS every 0.1 s; during `HuntActive` it calls `Hear(pos, 9)`. It never starts in automated runs.
  - The hollow-compass needle spins while `CastleDread.FalseCompass` is set.
- **`Soundscape.cs`:** the castle mix:
  - No house music; hunt music capped at 0.15.
  - The heartbeat follows the AUDIO.md curve and holds 0.5 in hunts. A no-tell hunt has no audio cue for its first 4 s.
  - Wind is 0.15 outdoors and 0.10 at windows.
  - The rare `cc_listen` plays in one run of three, at most once.
  - Claw steps come from his ground movement: 5 % outside hunts, 0.8 in hunts.
- **`Core/Investigation.cs`:** `BeginNoTellHunt()` applies to castle rules only, and the sarcophagus gamble uses it. `InteractKind.Sarcophagus` routes to `CastleRun.Sarcophagus()`.
- **Grabs:**
  - GrabDrop is the default whenever he is airborne.
  - The stand-off is the clip's reach plus 0.6 m: 2.36 m for drop, 2.33 m for crawl.
  - GrabCrawl is enabled after `fef50da`. `HearthboundActor.ModeFrames` is sized 10.
- **Project settings:** `microphoneUsageDescription` is set (the build fails without it); `defaultIsNativeResolution` is 0 and `fullscreenMode` is 3 (Windowed).
- **Playtest driver `fear` mode:** forced dice through the real rules, frame times, throws and beats, battlement line of sight, the gatehouse visit, and a bridge wait and crossing.

## Not done or not passing (needs a window: schedule after the stream)

1. **Throwables never flew** (`HC_THROW=0`, poltergeist 0). Rebake the castle and build with `8485c6a`, then run `fear` and read the `HC_PLAY_THROW_PROBE` lines.
2. **Bellwether automated route:** `-hc-verify` in the combined player; expect `HC_NATIVE_ROUTE_PASS`. Not run on this build.
3. **Title captures:** at 2560x1440 and at 1920x1080 (`-hc-capture <dir> -hc-capture-shots S7 -hc-capture-size 1920x1080`), compared with `.wt/title/docs/haunted-castle/rounds/r10/title_mock.png`. Not run.
4. **A near-miss on this build** (it is a 1-in-3 roll; rerun escape or fear until one rolls) and its crawl path wall check.
5. **Unchecked features:** Long Night (`-hc-long-night`, escape mode), the death penalty (a death and a checkpoint retry), door held, and the rare line. These are compiled and wired but not exercised.
6. **Missing clip:** `voice_cc_contain`. F4 can add it via `tools/generate_open_voice.py --castle`.

## Rules for the next integrator

- Unity is launched only through `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-f0 -- ...`, one instance at a time; stop it with SIGTERM.
- Bakes use `-batchmode -quit` (graphics). Import and build add `-nographics`.
- **Any `.cs` change stales both receipts.** Re-run `HouseLighting.PrepareAndBakeCastle` and `PrepareAndBake` before `NativeBuild.BuildPreparedMacAll`. Only castle-only paths leave the Bellwether receipt alone.
- **Scripted runs need a window**, because batchmode stalls at `WaitForEndOfFrame`. Use a 1920x1080 window, and never while the owner streams.

## For Codex
Wall-art handoff (7 castle canvases, 3 Blood Ledger pages, 1 claw-scores decal; prompts, specs, checker, fallback prompts, licence lines): `docs/haunted-castle/CODEX_HANDOFF.md`. Runtime patch for per-frame paintings and animated pupils (integrator applies, needs a castle re-bake): `docs/haunted-castle/wallart_runtime.patch`. Piece T2 status: `docs/haunted-castle/handoffs/wallart.md`.
