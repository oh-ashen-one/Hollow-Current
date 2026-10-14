# HANDOFF: piece F0 (integrator), Haunted Castle waves F, G and H (fear plan)

Branch `haunted-castle` in `/Users/midir/hollow-current-haunted-castle` (main clone, pushed to origin). Model: Opus 5.5. Date 2026-10-14.

**Merged, in order:**
- **Wave F:** hc/audio2 `0dc4e52`, hc/dread `49cadd2`, hc/scares `d50a54f`, hc/count11 `fc0175c`, hc/grabs `ae82eb3` and `fef50da`, hc/title `3d9a17e`, hc/wallart `dab74a5` (patch applied in `b5da27e`), hc/throws `12e5961`.
- **Wave G:** hc/scares2, hc/audio3, hc/count12, hc/rules2.
- **Wave H:** hc/count13, hc/scares3, hc/rules3.

Each piece's own handoff is archived in `docs/haunted-castle/handoffs/<piece>.md`.

## State

- **Build:** `/Users/midir/hollow-current-haunted-castle/Builds/macOS/Hollow Current.app`, with both maps.
  - Built from source `430a266`; the bake outputs are committed as `a412df7`.
  - It opens as a 1920x1080 window.
  - Bake receipts are current: Castle 5 maps, 196 probes, 17 reflections; Bellwether 4 maps, 124 probes, 9 reflections.
  - `check_throwables_scene.py` exits 0.
- **Checks:**
  - `check_source.py`: PASS, 970 assertions.
  - `check_castle_scares.py`: PASS.
  - `check_audio_keys.py`: OK.
  - `bellwether_fuzz.py`: PASS.
- **Proofs:** the windowed proof table is in `docs/haunted-castle/playtest/AUTO_RUN.md`; owner notes are in `docs/haunted-castle/PLAYTEST.md`.

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
| hc/rules2 | `2612896` | `e6b0aa1` | `53da00f`, below |

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

## Wave H (r10b static critic `docs/haunted-castle/rounds/r10/CRITIC_STATIC_B.md`, 5/10) and the windowed checklist (2026-10-14)

**Merged:**
- hc/count13 `98532bf` (`d57e1f0`)
- hc/scares3 `632d859` (`05ee819`)
- hc/rules3 `6c8cf9c` (`23a8d7b`, RulesTests conflict resolved keeping both sides)

**F0 items:**
- `feeeed6`: N1 held goal at the winch and on the radio; N4 line-of-sight interference during hunts only; N5 phase-blind castle actions; N9 swoop card 2 s after the sealed message.
- `a9a7161`: the rules3 integrator diff (also N13), the claw volume hook, the 6-30 s post-Recovery gap and the 5 % decoy tells.
- `4294323`: the cursor rule.
- `e1e360a`:
  - The Bellwether focus-ray fix.
  - Castle chase contact decays instead of resetting, so the bridge is lethal.
  - No hunt voice line in a hunt's first 8 s unless he has seen her.
  - Castle scare rhythm: rests 35-70 s, appearances every 60-110 s, and the director runs in Recovery.
  - Title fog as one masked band.
- `430a266`: `HC_SCARE_SKIP` reasons and the driver's watched-hide timing.

**Checks:**
- `check_source.py`: 970 assertions.
- `check_castle_scares.py`: PASS.
- `check_audio_keys.py`: OK.
- `bellwether_fuzz.py`: PASS (run after every merge).

**Build:** `Builds/macOS/Hollow Current.app` from bake commit `a412df7` (source `430a266`). `check_throwables_scene.py` exits 0 on it.

**Proof results:** in `docs/haunted-castle/playtest/AUTO_RUN.md`.

| Status | Proofs |
|---|---|
| PASS | escape; Long Night; natural (won); throw + poltergeist; lies + silence + duck; false warning; watched hide with the torch off from a perch; near-miss with an open; Recovery cut; swoop legs clear; lethal bridge; low-Dread hunt; death + retry; Bellwether `-hc-verify` |
| Partial | no-tell first 6 s: wing loop +0.6 on a full first leg in both samples; everything else unchanged |
| Open | apparitions (1 in a natural 10 minutes, so no natural throws or escalation) |

Captures are in `docs/haunted-castle/rounds/r10/captures/` and are ready for the blind critic.

**Code-only since the owner's stop (13:48):** the castle apparition rhythm fix (no rest after a Warning or hunt, 20-40 s after Recovery, checks every 12-25 s); the cause is in AUTO_RUN.md. The natural run r3 was **aborted by the owner** (SIGTERM, clean exit, not retried).

### Remaining windowed list (only when the owner lifts the stop; launch with `open -g` through `gpu_slot`, never full screen)

1. Re-bake both maps (`PrepareAndBakeCastle`, then `PrepareAndBake`). `check_throwables_scene.py` must exit 0. Then build (`BuildPreparedMacAll`).
2. **Natural run (no forcing):**
   - More than 1 `HC_SCARE visible=` in 10 minutes.
   - `HC_THROW` or `HC_SCARE_CASTLE poltergeist` in natural play.
   - `HC_ESCALATE escalated=True` at least once (rerun up to 3 times).
   - Few `HC_SCARE_SKIP reason=rhythm canSurprise=False` lines.
3. **Fear run:** no-tell first 6 s with `changedFrames=0`. Report the first wing leg; a silent leg is possible.
4. **Kill, siege (near-miss open, Recovery cut) and death + retry:** these passed only on the r1 build; rerun them on the current build.
5. **Bellwether `-hc-verify`:** `HC_NATIVE_ROUTE_PASS` again, after the director change (shared file).
6. **Title captures:** at 1920x1080 and at the largest window, to confirm the fog band.
7. **Blind critic round** on the captures and logs.

## Rules for the next integrator

**Deliberate Bellwether change (for Hari to accept):** `Investigator` focus ray, in both maps (`e1e360a`). An interactable whose collider the same ray meets within 12 cm behind the first hit is focused. This fixes the coat cupboard: its hide trigger sits in the same plane as its solid lower door from `89dac7e` on `main`, which made `-hc-verify` fail. Bellwether rules are unchanged (fuzz identical).

**Owner rules (2026-10-14):**
- **Windowed only:** never fullscreen. Captures that cannot fit use the largest 16:9 window that does.
- **"When you're testing the game stop taking my mouse":**
  - Scripted runs (`-hc-playtest`, `-hc-verify`, `-hc-capture*`) never lock or hide the cursor: `Game.LateUpdate` forces it free every frame and logs `HC_CURSOR free` once.
  - They run in the background, and the Investigator reads no real mouse, keyboard or pad input in them (TestInput only, otherwise idle).
  - Launch them without activating the window: `gpu_slot.sh capture --label hc-f0 -- open -g -W -n -a "<app>" --args -logFile <log> ...`.
  - Owner-launched play keeps the normal cursor lock.


- Unity is launched only through `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-f0 -- ...`, one instance at a time; stop it with SIGTERM.
- Bakes use `-batchmode -quit` (graphics). Import and build add `-nographics`.
- **Any `.cs` change stales both receipts.** Re-run `HouseLighting.PrepareAndBakeCastle` and `PrepareAndBake` before `NativeBuild.BuildPreparedMacAll`. Only castle-only paths leave the Bellwether receipt alone.
- **Scripted runs need a window**, because batchmode stalls at `WaitForEndOfFrame`. Use a 1920x1080 window, and never while the owner streams.

## Worktrees

- Removed 2026-10-14: `.wt/{audio2,count11,dread,grabs,scares,throws,title,wallart}`. Each was merged into `haunted-castle`, on origin and clean.
- Kept: `.wt/wallart-art` (`hc/wallart-art`). It has no remote branch and has uncommitted `Resources/Castle/Portraits/`.

## For Codex
Wall-art handoff (7 castle canvases, 3 Blood Ledger pages, 1 claw-scores decal; prompts, specs, checker, fallback prompts, licence lines): `docs/haunted-castle/CODEX_HANDOFF.md`. Runtime patch for per-frame paintings and animated pupils (integrator applies, needs a castle re-bake): `docs/haunted-castle/wallart_runtime.patch`. Piece T2 status: `docs/haunted-castle/handoffs/wallart.md`.
