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

This is the wave F state. Wave G (below) supersedes the heartbeat, music, HUD, radio, torch-failure and pulse notes here.

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

## Wave G (static fixes from `docs/haunted-castle/rounds/r10/CRITIC_STATIC.md`, round-10 static critic, 4/10, 44 findings)

All four wave G branches are merged; nothing from wave G is in a build yet.

| Piece | Branch tip | Merge | Integrator wiring |
|---|---|---|---|
| F0's own items | — | — | `d655eda` |
| hc/scares2 | `347060f` | `517da28` | `c180914` |
| hc/audio3 | `fee64f3` | `c285793` | `55cc989` (cc_dispatch key, `ASSET_LICENSES.md` lines) |
| hc/count12 | `f5e590b` | `f92bae3` | `99fbb0b`: torch fails on `DiveLastSecond`; the `return-swoop` beat shows the card; one dispatch voice |
| hc/rules2 | `2612896` | see `git log` | follow-up commit after the merge, below |

After the hc/rules2 merge:
- **Felt Dread:** the heartbeat and journal pulse read `Investigation.FeltDread` directly.
- **No numbers in castle warnings:** the castle Warning message and the bell card no longer say "Nine seconds". Long Night's bell card says "He is already coming.", and the HUD cue is the word "SIGNAL COLLAPSING".
- **Logs:** `HC_RULES` and `HC_HUNT_START` now log `runDreadMin`, `felt`, `huntSeconds` and `breaker`, and `HC_RECOVERY seconds=` is new.
- **Driver waits:**

| Wait | Old | New |
|---|---|---|
| Hunt end | 45 s | 60 s |
| Hunt tracker | 60 s | 70 s |
| Recovery | 40 s | 50 s |
| Ledger wait | 80 s | 100 s |

- **Driver Dread forcing:** the Dread blocker and the forced Dread use `RunDreadMin`.

**Checks (pure Python):**
- `tools/check_source.py`: PASS, 939 assertions.
- `tools/check_castle_scares.py`: PASS.
- `tools/check_audio_keys.py`: OK.

## After the stream: in this order (needs a window and the GPU slot)

1. **Re-prepare and re-bake the castle:** `HouseLighting.PrepareAndBakeCastle` through `gpu_slot`.
   - Then `python3 tools/check_throwables_scene.py` must exit 0. It exits 1 on the old `34a0de9` bake, where 38 of 38 throwables were static-batched.
2. **Re-bake Bellwether:** `HouseLighting.PrepareAndBake`. Shared `.cs` changes stale its receipt.
3. **Build:** `NativeBuild.BuildPreparedMacAll` (`-nographics`). The player is windowed at 1920x1080; check `HC_WINDOWED` on a plain launch.
4. **Proof runs** (`-hc-playtest <dir> escape|kill|fear` plus targeted runs). Grep the logs for:
   - [ ] **Throws:** `fear` with `HC_THROW` > 0 and `HC_THROW poltergeist` > 0. Natural play, with 2 or more evidence and at least 60 s in Disturbance, must also throw; otherwise `HC_THROW_SKIP reason=` explains why.
   - [ ] **Near-miss:** runs end to end (`HC_COUNT_NEARMISS begin`, then hold, then leave), with a door rattle and `cc_hunt_smell` at the slats. At Dread ≥ 80 or in Long Night, at least one `open=True` with a crawl grab.
   - [ ] **Watched hide:** with the **torch off**, while he is perched or travelling, ending in `HC_COUNT_WITNESSED`, then a door rattle, then `HC_COUNT_GRAB hideEntrance=True witnessed=True torch=False`.
   - [ ] **No-tell hunt:** for its first 6 s, no change in the HUD goal, radio, instrument, music, heartbeat, wing loop (while perched), rim light or voice. Use a per-frame channel log; `BreakerOn` stays true.
   - [ ] **Low-Dread hunt:** at Dread below `RunDreadMin`, for at least 10 minutes after the first hunt, at least one hunt starts.
   - [ ] **Recovery hunt:** at Dread ≥ 80, a Recovery is cut short at least once. `HC_HUNT_START` and `HC_RECOVERY` lengths vary.
   - [ ] **Hostile return:** `HC_COUNT_RETURN_SWOOP` with `leg1clear=True leg2clear=True`, then the dispatch card and `voice_cc_dispatch`, and only then a dive on the bridge.
   - [ ] **Escalation:** `HC_ESCALATE escalated=True` at least once in natural play.
   - [ ] **Silence beat:** `HC_SCARE_CASTLE silence ... duck=wired`, then `HC_DUCK`, with no hunt-state change within 6 s.
   - [ ] **Missing audio:** no `HC_MISSING_ART` across a full run that includes radio presses before diagnosis.
   - [ ] **`cc_listen`:** at most once per run (`HC_RARE_LINE` + `HC_LIE kind=phantom_listen` ≤ 1).
   - [ ] **Heartbeat:** silent during a hunt at Dread 30; it follows FeltDread with a 20-30 s lag.
   - [ ] **Bellwether `-hc-verify`:** `HC_NATIVE_ROUTE_PASS`, with the unchanged 95 / 9 / 38 / 32 cycle.
   - [ ] **Title captures:** `-hc-capture <dir> -hc-capture-shots S7` at 2560x1440 (full-screen-window for that run only) and with `-hc-capture-size 1920x1080`. Compare against `rounds/r10/title_mock.png` and `title_mock_bellwether.png` within a 2 px offset, and check for `HC_TITLE_FONT cinzel=True`.
   - [ ] **Long Night:** `-hc-long-night` with the escape route. `HC_NAIL_HIDES nailed=2`, no Warning, longer hunts.
   - [ ] **Death and retry:** a real death, then the checkpoint retry. `HC_DEATH_PENALTY ... moved=True` and `HC_RELIC_MOVE`; Dread restarts at 60.
5. **Frame time:** in the great hall at 1920x1080. The fear run logs `HC_PLAY_FRAMES great_hall`; the last value was 11.15 ms mean.
6. **Blind critic round** on the new build, then update `docs/haunted-castle/playtest/AUTO_RUN.md` and `PLAYTEST.md`.

## Rules for the next integrator

- Unity is launched only through `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-f0 -- ...`, one instance at a time; stop it with SIGTERM.
- Bakes use `-batchmode -quit` (graphics). Import and build add `-nographics`.
- **Any `.cs` change stales both receipts.** Re-run `HouseLighting.PrepareAndBakeCastle` and `PrepareAndBake` before `NativeBuild.BuildPreparedMacAll`. Only castle-only paths leave the Bellwether receipt alone.
- **Scripted runs need a window**, because batchmode stalls at `WaitForEndOfFrame`. Use a 1920x1080 window, and never while the owner streams.

## Worktrees

- Removed 2026-10-14: `.wt/{audio2,count11,dread,grabs,scares,throws,title,wallart}`. Each was merged into `haunted-castle`, on origin and clean.
- Kept: `.wt/wallart-art` (`hc/wallart-art`). It has no remote branch and has uncommitted `Resources/Castle/Portraits/`.

## For Codex
Wall-art handoff (7 castle canvases, 3 Blood Ledger pages, 1 claw-scores decal; prompts, specs, checker, fallback prompts, licence lines): `docs/haunted-castle/CODEX_HANDOFF.md`. Runtime patch for per-frame paintings and animated pupils (integrator applies, needs a castle re-bake): `docs/haunted-castle/wallart_runtime.patch`. Piece T2 status: `docs/haunted-castle/handoffs/wallart.md`.
