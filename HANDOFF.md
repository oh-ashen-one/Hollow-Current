# HANDOFF: piece F0 (integrator), Haunted Castle waves F, G and H (fear plan)

Branch `haunted-castle` in `/Users/midir/hollow-current-haunted-castle` (main clone, pushed to origin). Model: Opus 5.5. Date 2026-10-14.

**Merged, in order:**
- **Wave F:** hc/audio2 `0dc4e52`, hc/dread `49cadd2`, hc/scares `d50a54f`, hc/count11 `fc0175c`, hc/grabs `ae82eb3` and `fef50da`, hc/title `3d9a17e`, hc/wallart `dab74a5` (patch applied in `b5da27e`), hc/throws `12e5961`.
- **Wave G:** hc/scares2, hc/audio3, hc/count12, hc/rules2.
- **Wave H:** hc/count13, hc/scares3, hc/rules3.

Each piece's own handoff is archived in `docs/haunted-castle/handoffs/<piece>.md`.

## State

- **Build:** `/Users/midir/hollow-current-haunted-castle/Builds/macOS/Hollow Current.app`, with both maps.
  - Built from source `3bff23e` (the r11 fixes); the bake outputs are committed as `0b08422`. The hc/grabs2 clips are imported.
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
- hc/count14 `a9d6e40` (merge below; handoff `docs/haunted-castle/handoffs/count14.md`)
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

**Build:** `Builds/macOS/Hollow Current.app` from bake commit `fff0de7` (source `a290313`). `check_throwables_scene.py` exits 0 on it.

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
3. **Fear run:** no-tell first 6 s with `changedFrames=0` and **no-tell first leg silent**. Expect `HC_COUNT_LEG first=silent reason=` (hc/count14: no-tell and hostile-return hunts always fly a silent first leg, then a silent or soft second leg; the breath one-shot is held for 8 s unless seen), with `wingMaxDelta` near 0.
4. **Kill, siege (near-miss open, Recovery cut) and death + retry:** these passed only on the r1 build; rerun them on the current build.
5. **Bellwether `-hc-verify`:** `HC_NATIVE_ROUTE_PASS` again, after the director change (shared file).
6. **Title captures:** at 1920x1080 and at the largest window, to confirm the fog band.
7. **Blind critic round** on the captures and logs.

## Wave I (blind critic `docs/haunted-castle/rounds/r10/CRITIC_BLIND.md`, 4.5)

**F0 (`6a2e0d4`, code only, not built):**
- **Candle lean:** at most 22 deg, eased by distance to the Count (full within 3 m, none beyond 8 m), in CandleFlame and Floater. False tells keep the full lean, also capped at 22 deg.
- **Scale-reference figure:** hidden in scripted runs.
- **S7 capture:** named by content (`S7_gatehouse_hud_case_start` vs the title captures).
- **Driver:** expects Long Night's bell to start the hunt (by design: "He is already coming."), and logs `HC_PLAY focus_fail=` with the hit object.

**4(a), compass not recording (r1 kill and r1 natural):**
- Cause: wave G #9 interference. Any Count within 12 m, through walls and between hunts, suspended recording ("INTERFERENCE · RECORDING SUSPENDED").
- Fixed by N4 in `feeeed6`: interference now needs a hunt, under 12 m, and line of sight. r2 natural recorded all three, and r2/r3 escape and fear had 0 blockers.

**Merged:** hc/count15 `eb25966`; handoff `docs/haunted-castle/handoffs/count15.md`. It covers:
- Flight pitch 38-68 deg nose-down live (40/48 deg in capture) and a 5.2 m/s dive.
- Grab selection 8/9/6 by situation, with the kill pose on the floor.
- Door-aware routing (the two wall crossings were closing service doors).
- Wing stretch capped at 1.6x (`HC_COUNT_WING_CLAMP`).
- A log-only learned cue, a randomised and held hungry line, and grab-survived Dread once per hunt.

**Dread budget per hunt, which hc/rules4's per-hunt cap must cover:**

| Source | Dread |
|---|---|
| Count side (Presence) | at most +35 per hunt (seen +15, grab survived +20, once each) |
| ScareDirector | +12 per apparition, +8 per throw/poltergeist |

**Merged:** hc/rules4 `9b9cde8`; handoff `docs/haunted-castle/handoffs/rules4.md`.
- **What it does:** event-Dread decay (0.4/s in Recovery, 0.15/s calm, × `DreadRelief`, floor 20 after the first hunt), a per-hunt `AddDread` cap of 25, a 30-90 s post-hunt grace, and `HuntShare`. `check_source` now runs 1018 assertions.
- **Integrator diff applied:**
  - `DreadRelief`: gatehouse 3x, chapel fire 2x, otherwise 1x.
  - `HC_RULES` every 30 s with `huntShare=`; `HC_RECOVERY grace= huntDreadAdded= huntShare=`.
  - Siege mode clears the grace once for the Recovery-cut proof.
- **Driver change:** its `ForceDread` helper now calls `Investigation.ForceDread`, because the capped, decaying `AddDread` can no longer hold a test level.

**Pending:**
- F5's regenerated grab clips (wing stretch ≤ 1.6). Their `.anim` import (`CountClaudeImport.Prepare`) needs Unity, so it waits for the window.
- When hc/rules4 lands: wire `DreadRelief` (gatehouse 3x, within 4 m of the chapel fire 2x, else 1x).

**Windowed proof lines to add (wave I):**
- [ ] Candle lean ≤ 22 deg on screen in S1/S5 (`HC_LEAN_CHECK`).
- [ ] No scale-reference figure in S6 (`HC_SCALE_REFERENCE hidden`).
- [ ] hc/count15:
  - Flight pitch readable.
  - Dive close time logged.
  - Grab mode 8/9 shown.
  - `wallCrossings=0` in every hunt.
  - The wing tip has no crumple in H09/H10.
- [ ] hc/rules4: `huntShare` ≤ 0.30 over a natural 10-minute run (last `HC_RULES … huntShare=`).
- [ ] Every `HC_RECOVERY` line has a `grace=` of 30-90 s, and no dice hunt starts inside a grace (bell and gamble excepted).
- [ ] Event Dread decays: `HC_RULES event=` falls between hunts, faster in the gatehouse.

## Round 7b (build `e57a00d`): hc/count17 + hc/art-exterior2 merged

- Proof table: `docs/haunted-castle/playtest/AUTO_RUN.md`, "Round 7b"; pre-merge table on `ac43efa` under "Round 7".
- Open: natural2 share 0.224 and no between-hunt gap ≥ 120 s; watched-hide grab path label is `dive`, and at t 1.00 he is not visible; HUD prompt shows during grabs; `gate_out_S6` faces the closed gate doors; verify floor flag still False.
- Crypt "hitch" was the screenshot encode frame (capture artefact).

## Art pass merged (round 7, build `ac43efa`)

- hc/art-exterior merged as `d904e77`, hc/art-props as `415eb9d` (conflicts in `ASSET_LOG.md` and `tools/castle_verify.py` kept both sides).
- New capture mode: `-hc-capture <dir> -hc-capture-art exterior|props`. The capture window is resized back to the requested size before every shot.
- Results and capture list: `docs/haunted-castle/playtest/AUTO_RUN.md`, "Round 7". Captures are local in `rounds/r12/captures/art/`.
- Open: the exterior group is turned 180 degrees (`HC_EXTERIOR_AXIS`); 7 portraits wait for Codex; one 118.9 ms crypt hitch; the watched-hide grab camera took the hide-open path this run, skull off-screen at t 0.01.

## Round 11 art pass (next wave, after the fear loop is proven)

From the blind critic's room findings (V1-V4):
1. **Crypt:** the ceiling hand-roots are tan cubes with stick fingers. Make them organic roots/hands, plus coffin niches and a carved sarcophagus.
2. **Great hall:** the draped giant skeleton is only a small rib fragment. Make it ≥ 30 % of frame width, and add banners and tall lancet windows with both moons.
3. **Exterior:** the castle is a trapezoid box with five cones over a black void. Put it on a rock with a spire cluster, mountains, trees and bats, and replace the flat red moat.
4. **Chapel:** the palette is inverted (warm amber instead of cold blue), the candles are 3-5x scale, and the stained glass is missing. Make it cold, add the broken stained glass and true-scale candles, and add the font.

## Round 4 proof results (build `fff0de7`), open items

Full table in AUTO_RUN.md.

| Status | Proofs |
|---|---|
| PASS | escape; kill; Long Night; death + retry; siege (near-miss open, Recovery cut); natural (won, huntShare 0.273, grace 33-87 s, event Dread decays, 4 apparitions, 1 escalation); huntcam (close in 1.38 s, mode 9 grab) |
| FAIL | `-hc-verify` (window 1022x579); natural unforced throws (every throwable more than 6 m away) |
| INVALID | the fear run (a real gamepad disconnect paused it) |
| Partial | pitch 35 at slow travel; S5 candle lean 23-25 deg on screen |

**Fixes not yet started (the coordinator asked for the report first):**
1. Scripted runs should ignore real pad connect/disconnect, so it cannot pause the game.
2. `-hc-verify` should set a 1920x1080 window at start.
3. Natural throws: place or seek throwables near where apparitions land.
4. Then rerun fear and `-hc-verify`.

## Round 5 (build `0b08422`)

Table in AUTO_RUN.md.

| Status | Proofs |
|---|---|
| PASS | `-hc-verify`; fear: no-tell silent first leg, `changedFrames=0`, wing 0.000, watched hide with the torch off, `wallCrossings=0`, pitch ≥ 38, grab mode 8; natural: won, 2 unforced throws, 4 apparitions, 1 escalation, grace 40-76 s |
| FAIL | natural `huntShare` 0.362 > 0.30. Cause: the invulnerable, exposed driver gains the +25 per-hunt cap every hunt, Dread reaches 100, and at Dread ≥ 80 a hunt follows each grace |
| Not seen | `HC_COUNT_WING_CLAMP` |
| Not reached this run | the bridge grab (earlier rounds proved it) |

The blind critic is next. No further fixes were started.

## hc/rules5: pacing governor (code only, not built yet)

**Merged:** hc/rules5 `4283f55`; handoff `docs/haunted-castle/handoffs/rules5.md`.
- **Grace:** multiplied by (1 + 12 × (share − 0.25)), capped at 3. After 3 minutes below a share of 0.12 the grace is 0.8×.
- **High-Dread dice odds:** divided by the same factor.
- **Share:** measured from the first threat, over a 300-600 s window.
- **Integrator diff applied:**
  - `HC_RECOVERY governor= governorShare=`
  - `HC_RULES governor=`
- **Checks:** `check_source` 1036 assertions, `check_castle_scares` PASS, `check_audio_keys` OK, `bellwether_fuzz` PASS.

**Windowed proof line (next build):**
- [ ] A natural 10-minute run has a final `huntShare` ≤ 0.33, even with the immortal driver.
- [ ] `governor=` > 1 appears on `HC_RECOVERY` / `HC_RULES` whenever the share is above 0.25.

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
