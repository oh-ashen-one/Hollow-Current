# Combined Fish + Tripo release — 2026-10-25

Owner-authorized main merge; both-map macOS build passed all four offline native routes. Includes the prop rebuild repair and exact owner-approved natural hiding breath. Read `docs/integration/2026-10-25/README.md` for results, delivery and remaining validation limits. Prior handoffs below are historical.

# Fish Audio integration — completed automated delivery, 2026-10-22

Branch `fish-audio-integration`: all 28 dialogue lines and discrete vocal effects integrated; fresh two-map macOS app built; Bellwether and Castle escape/fear/kill routes pass offline. User preferences restored, task engines closed and slots released. Read `docs/audio/fish-integration/HANDOFF.md` for deliverables, evidence and limitations. Owner listening/playtest acceptance remains pending; no main merge was authorized.

Earlier handoffs below describe historical builds.

---

# HANDOFF: owner playtest fixes + Tripo castle props (`hc/tripo-fixes`, 2026-10-19). Read this section first.

Branch `hc/tripo-fixes` (from `haunted-castle` `0e2c1d6`), pushed. `main` and `haunted-castle` untouched; merging needs the owner.
Builder: Devin. Studio clone `~/hc-tripo` (own clone), laptop clone `~/hollow-current`.

- **Fixes:** courtyard tree moved to the NE corner with a trunk-only collider (the 2.4 m Prop box blocked the Dining courtyard door);
  Count hidden unless apparition/staged scare, chase/dive, grab, near-miss/return swoop, sword fight or death (`Presence.CountVisibilityGate`,
  `forceRenderingOff`, rim light gated, `HC_COUNT_VISIBLE`), breathes while unseen and watching (`HC_COUNT_WATCH_TELL`), step test adds a
  1.3 m line (`LegBlocked`); castle darker (`CastleLightTuning`, `CastleAtmosphere`: candles .26, lantern 1.15 / 11 m / 72 deg, lantern glow
  .75 / 3.2 m, ambient .45, fills and moonlight lower) - the old exposure bands (35-55 / 12-25 % near-black) are superseded by the owner's
  "more darkness"; Bellwether: no dispatch voice on arrival/kit, noise-only `step_{wood,tile,carpet}_*` (`tools/audio_castle_steps.py wood tile carpet`),
  arrival note on a console table with legs, beds probe-lit (`HouseLighting.ProbeLit`; bed glitch cause inferred, not seen in a capture).
- **Tripo props:** 36 Tripo Studio web generations (owner account, existing Chrome tab, 55 credits each, 1,980 total; task ids in
  `docs/haunted-castle/ASSET_LOG.md`) -> `tools/tripo_castle_convert.py` (spec: bounds, fit, yaw, budget, anchors per prop) ->
  `Art/Tripo/Castle/<name>` -> `TripoImport.Prepare` -> 40 `Resources/Castle/<name>.prefab`. 35 replaced FBX removed. `SurrealProps` uses
  `TripoCandle`, `TripoCandelabraTable`, `FloatingBook`, `Tome`, `Chandelier`, `Sconce`, `Knife` with procedural fallbacks. Orrery and
  Telescope were generated but not shipped (socket/pose coordinates measured on the old meshes). Source GLBs: Studio `~/hc-tripo-work/glb`.
- **Verified:** `check_source.py` PASS (1615); castle prepare `HC_KIT_AXIS_SUMMARY problems=0`, `HC_CANDLES floating=0 PASS`, 7 known portrait
  `HC_MISSING_ART`; both maps baked (Castle 7/197/17, Bellwether 4/124/9); `BuildPreparedMacAll` Success; `-hc-capture-art layout|kit`
  captures inspected (hall, chapel). Not verified: a played run (Count visibility in a real hunt, courtyard exit, bed in the house).
- **Rebuild note:** `tools/castle_build_all.sh` would regenerate the removed FBX; delete them again (or skip those names) so the prefabs load.

# HANDOFF: session close. Wave FG integrated, two r15 fixes, built and proved (fg-integ). Read this section first.

Branch `haunted-castle` = `hc/integ-fg`, pushed. Integrator: Opus 5.5 (piece fg-integ), main clone, 2026-10-19. **This closes the session**: no piece branch is open, nothing of mine is running, the GPU slot table is 0/2.
- PlayerPrefs restored: `unity.Bellwether Field Office.Hollow Current` `hc.castle.runs` = `L,E,L` (never changed); `com.bellwether.hollowcurrent` put back from `L,E,L` to `E,M,B`; `hc.castle.grabdeck` deleted (absent at the start, so the runs used a fresh-player deck carried across runs). Backups: session scratchpad `fginteg/{u,c}.backup.plist`.
- `docs/haunted-castle/CODEX_HANDOFF.md` is still uncommitted and untouched (Codex's). `rounds/r13/critic/`, `r14/critic/`, `r15/critic/` and `rounds/r14/captures/fg-art-offline/` stay untracked.
- Worktrees: `.wt/fg-polish` removed (branch `hc/fg-polish` stays on origin). Left in place, all Codex's: `.wt/wallart-art`, `.wt/codex-ui`, `.wt/codex-variants`.
- `main` untouched. Merging `haunted-castle` to `main` needs the owner.

**Merged on `hc/integ-fg` (from `haunted-castle` `4e65cc1`, code `9269090` + r15 critic report):** hc/fg-polish `35b531b` (merge `5f4d6ee`, clean). Handoff: `docs/haunted-castle/handoffs/fg-polish.md` (owed apparition follow-ups, blind dives keep their side/spin plan + side balance bonus, 0.45 m seat cut + 0.43 m root leash on every grab path, S1 capture eye at gallery height, corrected moat masks, calmer water).

**Integrator changes: the two r15 required fixes** (`a02b5b6`)
1. **Doors close with E from the opening** (r15 critic 8f: 8/8 doorproof closes needed `close=direct`; a 155 deg leaf lies flat on the wall, out of the interact ray).
   - `CastleWorld.BuildDoor`: every castle leaf except the portrait door gets a static `<leaf> / opening` door volume: a trigger filling that leaf's half of the doorway through the whole wall (`OpeningDepth` 1.1 m), on layer 2 (Ignore Raycast), no renderer. Sight lines, the Count's queries (all default-mask or `Ignore` queries), the nav bake (skips triggers) and the swing probe never see it. 22 volumes in the prepared scene.
   - `Investigator.FocusRay`: only when nothing else is focused, the nearest layer-2 door volume the ray meets before the first solid hit is the focus. Bellwether has no layer-2 interactables: focus unchanged, `bellwether_fuzz` identical.
   - `Door.Toggle(by)`: the castle logs `HC_DOOR_STATE ... state=closed closed_by=E|direct`. `Interactable.Use` passes `"E"`.
   - `doorproof`: after each opening she looks at the middle of that leaf's half of the doorway and presses E; logs `HC_DOORPROOF close=E via=leaf|opening`; a press that does not close is a close failure (`close=direct close_fail`). PASS now needs `closes >= 8` and `close_failures=0`. Each closed leaf is captured (`doorproof_<door>_<side>_closed`).
   - The cold route never presses E on an already open door (an open doorway is focusable now).
2. **Castle UI says lantern / resonance box** (r15 critic: "Flashlight" and "radio" leak into the castle UI). Castle only, Bellwether strings unchanged:
   - Settings `Lantern level`; settings footer `F lantern · R resonance box`; loading footer; HUD pill `LANTERN`; help hint `R RESONANCE BOX` (pad `L2/LT RESONANCE BOX`); hide prompt `LANTERN OFF`.
   - Castle `CaseBook`: `Resonance box / ...` cards (interference, hunt), kit label `..., resonance box`, lost card `Douse the lantern and still the resonance box.` `FieldMessages.ShowCard` also shows `Resonance box / ` cards.
   - Tests: `RulesTests.WaveFGTests` asserts no castle CaseBook string contains radio/flashlight, castle cards still show, Bellwether's unchanged; `check_castle_scares.py` fails any Interface.cs literal naming flashlight/radio without a castle alternative on its line. `tools/docui_journal_mock.py` settings slider renamed to match.
   - Not renamed (not player-facing): asset/role keys (`FieldRadio`, `held_radio`, `HC_INSTRUMENTS radio=`), the `radio` sound cue id.

**Build:** `Builds/macOS/Hollow Current.app` (both maps), Assembly-CSharp.dll **2026-10-19 17:43:53**, built right after bake commit `0a15d1b` (17:43:37). Both maps re-baked (shared code changed). All proofs below ran on it.

**Prepare log** (`.castle-bake-fg.log`): `HC_KIT_AXIS_SUMMARY problems=0`, `HC_DOOR_LEAF_SUMMARY doors=23 problems=0` (both stages), `HC_CANDLES floating=0 PASS`, `HC_CHAPEL_PROPS overlap=False PASS`, `HC_TELESCOPE clear=True`, `HC_HEARTH window_overlap=False`, `HC_HIDE_SPOTS_SUMMARY audited=8 focusable=8 failed=[]`, `HC_SKELETON PASS`, `HC_HALL_AXIS PASS`, `HC_CHAPEL`, `HC_EXTERIOR_AXIS ok`; Castle 8 maps / 197 probes / 17 reflections, Bellwether 4 / 124 / 9; only the 7 known portrait `HC_MISSING_ART`; 0 compile errors/exceptions.

**Static checks (HEAD):** check_source PASS (**1615** assertions incl. rules tests; known CS0649 warnings), check_castle_scares PASS (incl. the new string lint), check_audio_keys OK (69), check_audio_quality OK, check_throwables_scene rc 0, check_castle_doors OK (23 leaves), check_castle_kit OK, bellwether_fuzz **identical** (`ee3b74dc...fd68f2f`), check_voice_logs PASS (8 castle logs, all `from=head`; escape `--require-evp` PASS).

**Proofs** (logs `.playtest-fg-<run>.log`, frames `docs/haunted-castle/rounds/r15/captures/fg/<run>/`, local, gitignored). Runs: doorproof, natural x3, cold x2, fear, huntcam, siege_held x1 (`-hc-hold-dread 60`), escape, settings, Bellwether `-hc-verify` and `-hc-ui-only`; captures rooms off/on, exterior, lantern. No tuning round (director's order): numbers as they are.

| Proof | Band | FG (this build) | Verdict |
|---|---|---|---|
| **Door close by E** (doorproof) | 0 `close=direct` | `HC_DOORPROOF toward=0 toggles=8 unfocused=0 closes=8 close_failures=0 leaves=4 PASS`; 8/8 `close=E via=opening`, `HC_DOOR_STATE closed_by=E` x8 (the one `closed_by=direct` is the driver's re-close of an auto-open before a side) | **PASS** |
| Castle strings | no Flashlight/radio | settings frame shows `Lantern level`, footer `F lantern · R resonance box`; HUD `LANTERN`, `R RESONANCE BOX` (doorproof frames) | **PASS** |
| Threats 10-20 s after an apparition (natural x3, pooled) | >= 2 | **4** (1 / 2 / 1) | PASS |
| Threats 25-60 s / within 10 s | <= 50 % / <= 35 % | 0/8 / 0/8 | PASS |
| Follow-ups | cancelled <= 30 % | drawn 5, owed 4, adopted 3, fired 3, **cancelled 2 = 40 %** (both `cancelled=phase_Containment`: the case ended, not grace); `HC_SCARE_WANTED reason=owed_follow` x3, `adopted` x3, `lapsed` 0 | **FAIL** on the letter; no grace/hold cancel (FF: 7/9 by grace) |
| Share / longest gap / first hunts | 15-30 % / >= 120 s | 20.3 / 26.5 / 21.6 %; 132 / 121 / 193 s; first hunts 133 / 89 / 194 (spread 105 s) | PASS |
| Apparitions < 480 s / CV pooled | >= 4 / >= 0.5 | 5 / 5 / 5; CV 0.60 (17 gaps) | PASS |
| Cold x2 | gate=cap, first hunt ~155-164 s | `gate=cap firstDocAt=-1`, first hunt 159.7 / 160.5 s, warning lead 9.7 / 10.5 s, `HC_PLAY_COLD pass=True` x2 | PASS |
| Grab mix (natural x3 + cold x2 + huntcam + fear, 17 commits) | m10 >= 1, m11 >= 1, m9 <= 40 % | m8 5, m9 6 (**35 %**), m10 3, m11 2, **m12 1** (fear Return drop-spin) | PASS (drop-spin back; r15 had 0) |
| Side planning | report | `stage=blind` x1 (natural3, R, flew 5.49 m on to the start point); `(+0.6 balance)` scores x5; flank yaw reasons mostly `line` / `floor` on both sides (L open 6, R open 6 of all tried yaws) | report |
| Dives unresolved / torch dive share | 0 / <= 30 % | 0 in every run / 6 of 22 natural dives = 27 % | PASS |
| Dive misses (natural x3) | | 16 misses over 22 dives: 8 `recommit blocked: released 1.5-1.9 s ago` (driver invulnerability), 4 `abandoned: the hunt ended`, 3 `moving away`; 0 `path blocked by ... Ledger` | note |
| Root slide (every grab, 27) | root_slide <= 0.45 or seat cut | **26/27**: natural1 drop `root_slide=0.46 root_ok=False` (`HC_GRAB_LEASH over=skull_push travel=0.44`); seat cuts 4 (side 0.46 / 0.85, fear hideopen 1.11); leash held 0.02 m on most dive grabs | **FAIL** by 0.01 m on one grab |
| Commit vs last dive sample (r15 test 3) | <= 0.5 m | 0.23, 0.58-0.71, 0.94 (10/11 over) | FAIL (not in FG scope) |
| Hide grabs (grab_luma) | t 0.16 mean >= 18, box >= 5 % | t 0.16: 4/4 PASS (30.2, 29.0, 31.3, fear 46.6); **t 0.01: 0/4** (5.5 x3, 11.3) | t 0.16 PASS; t 0.01 FAIL (r15 test) |
| Grab continuity / HUD / first frame | | every run `continuity n/n`, `hudZero n/n`, `firstFrameVisible n/n`, cam min 0.65 | PASS |
| Hidden survival (siege_held x1) | <= 85 % | 3/6 = 50 % (one siege only) | PASS (low; one run) |
| S1 lancets / moon / piers / floor mirror | >= 3 / 1 / >= 2 / >= 80 px | 6 lancets, moon 489 px (west), piers 2.47 / 2.89, mirror 370 px | PASS |
| **S1 table lantern off** | >= 15k px | **3,089** (10,416 >= 8; proxy 27,773 visible): est. was 18-19k | **FAIL**, worse than FF 10.8k |
| S4 sd right / left | <= 12 | **11.1 / 10.5** (both rooms captures) | PASS |
| Seams battlement_out / bridge_left | <= 25 | **9.3 / 2.5** | PASS |
| S4 red moon / pale glade / R/G | | 7,978 px / 234 px / 8.15 | PASS |
| Frame time p99 | <= 16.7 | S1 11.69 / 11.68, S4 11.13 / 11.55 (rooms off / on) | PASS (checker WARNs only because the frame-time lines sit in `.playtest-fg-rooms_*.log`, not `player.log`) |
| Exposure lantern off | 35-55 | S1 **42.3** (FF 30.2: the new eye), S2 41.9, S3 35.9 | PASS |
| Exposure lantern on | 12-25 | S1 16.4, S2 21.6, S3 16.7 | PASS |
| fear / huntcam / escape | | fear WON 0 blockers; huntcam `dead=True`, commit m8; escape WON 0 blockers | PASS |
| UI | overlaps 0 | `HC_DOC_LAYOUT overlaps=0` on settings, journal, ending | PASS |
| Bellwether | | `-hc-verify`: `HC_NATIVE_ROUTE_PASS`, 64 PASS, 0 FAIL, `HC_PERF pass=False engine_cost_over_16ms=112 (max 33.2)` (known); `-hc-ui-only` reached `ui-journal-ready`, closed by SIGTERM at its timeout (as FD-FF) | as before |
| Exceptions | 0 | 0 in every log | PASS |

`tools/check_art_views.py captures/fg/{rooms_off,rooms_on,ext}`: `HC_ART_VIEWS pass=False fail=1 warn=1 tests=22` (fail: S1 table; warn: frame-time lookup). FF gave fail=6.

**Looked at (frames)**
- S1 (new gallery-height eye) vs `refs/great_hall.png`: the frame is now mostly balconies, balustrades and floating candles; the table is a small dark shape in the middle distance (lantern off) and the shrouded ribcage is a red lump at the bottom edge inside the rune ring. Three north lancets with both moons sit small at the top centre behind the chandelier. Further from the reference than FF (table in front, skeleton behind, three lancets dominant). The offline estimate did not hold: the table's lit top is not what the camera sees.
- Moat (S4, battlement_out): smooth dark-red water, no glitter, no seam; the lamp glint is a narrow warm column. In S4 the moat is only a thin red band below the bridge; no mirrored castle reads.
- Side snatch (`natural3/10-13`, m11 in the upper gallery corridor): lantern-lit and readable from t 0.01, claws reach toward her by t 0.46; the held ear-trumpet still covers his torso and right claw at t 0.16-0.46 (the r15 occlusion complaint stands).
- Hide grab (`siege1/06-07`, near-miss): t 0.01 is black with ember eyes and red wings; t 0.16 her lantern lights him fully in the doorway.
- Doors (`doorproof/12-13`, `04-05`): open leaf with `[E] Close Dining hall door` aimed at the empty opening, then closed; same for the hall great doors.

**Session state for the next one**
- Build to play: `Builds/macOS/Hollow Current.app` (17:43:53), both maps.
- What passes: the fear loop (share, gaps, apparition clock incl. the new owed follow-ups, cold gate), grab variety (all five modes incl. drop-spin), hide grabs at t 0.16, the moat tests, exposure on/off, doors (open away + close by E), castle wording, UI layout, voice from his head, 0 exceptions.

**What the r15 critic still wants (CRITIC.md section 9), with its tests**
1. S1 and S4 to the reference (blind S1 >= 6, S4 >= 6). S1: >= 2 lancets each >= 25 % of frame height with a moon >= 60 px, floor mist sd <= 6 and mean >= 20 beyond the ring, >= 2 banners >= 120 px, skull >= 8 % of `hall_skull_from_east` with 2 dark sockets. S4: `check_art_views` all PASS on a critic capture (now true here except S1 table), a castle reflection cluster >= 2,000 px of luma >= 25 below the keep, ridge cards luma sd >= 6. **This build moved S1 away from the reference**: the next S1 work is layout (table near the eye, skeleton behind, lancets large), not the camera height.
2. Grabs readable at +0.3 s: claw >= 200 px from pixels in >= 80 % of grabs; held instrument covers <= 10 % of `countRect` to +0.6 s (lower or drop it at commit; still occluding here); hide-grab t 0.01 mean >= 18 (5.5 here); no two side snatches share `skull_px` within 20 px at t >= 0.46; side `min_eye_skull <= 1.0` (1.53-1.55 here); >= 1 drop-spin (1 here); `claw_px_proj` with first-person occluders.
3. Commit seat and blocked dives: `fromLastDiveSample <= 0.5` (10/11 over here); no `seatCut=True` (2 here, by design of fg-polish's cut); no mode-9 with `originY - floor > 0.5`; mode-9 skull steps <= 0.5 m per sample; 0 `path blocked by ... Ledger` (0 here in 3 naturals).
- Secondary (CRITIC.md): torch dive <= 30 % (27 % here), hidden beats in hunts within +-30 % of calm, cold first hunt after the first record or warning >= 9 s (9.7 / 10.5 s here), kit portrait mean >= 15 and a hearth with >= 3 flames, Bellwether `HC_PERF pass=True`, driver releasing after the first blocked kill (`teleportsDuringGrab=0` in cold: 0 here), 16:9 capture frames.

**Owner decisions pending**
1. Playtest feedback on this build (Hari plays; the last owner notes are from 2026-10-15).
2. Codex UI pick (`.wt/codex-ui`, `hc/codex-ui-prompt`; variants in `.wt/codex-variants`).
3. Voice audition: the Kokoro `bm_george` hunt lines (incl. the four FF lines `cc_hunt_found_b/c`, `smell_b`, `run_b`) need an owner listen.
4. docui approval: the nine-slice document cards (journal, settings, ending) and the Codex UI direction.
5. Build distribution: how and to whom the macOS build goes (nothing has been distributed); merging `haunted-castle` to `main`.

**Open items (new first)**
1. S1 table 3,089 px (FF 10.8k) with the gallery-height eye, and the composition is further from the reference; revert the eye or change the hall layout.
2. Follow cancellations 2/5 by Containment (case end), 0 by grace; the band counts them.
3. One drop grab slid 0.46 m (leash 0.43 + a 0.44 m skull push), 0.01 over the rule.
4. Dive commit vs last dive sample still ~0.6 m (r15 test 3); hide-grab t 0.01 frames still black (r15 test 2); held instrument over the side snatch.
5. Carried: Bellwether `performance_pass`, the 7 missing portraits, chapel confessional/guard_door exposure (as FE), `tools/ff_moat_twin.py` vs the engine.

---

# HANDOFF: wave FF integrated, tuned once, built and proved (ff-integ). Read this section first.

Branch `haunted-castle` = `hc/integ-ff`, pushed. Integrator: Opus 5.5 (piece ff-integ), main clone, 2026-10-19. Nothing of mine left running; my GPU slot released (the one remaining holder is another session's Unreal editor).
- PlayerPrefs restored: `unity.Bellwether Field Office.Hollow Current` `hc.castle.runs` = `L,E,L` (never changed); `com.bellwether.hollowcurrent` put back from `E,M,L` to `E,M,B`; `hc.castle.grabdeck` deleted (absent at the start, so every run used a fresh-player deck that carried across runs). Backups: session scratchpad `ffinteg/{u,c}.backup.plist`.
- `CODEX_HANDOFF.md` still uncommitted and untouched; `rounds/r13/critic/` and `rounds/r14/critic/` stay untracked. Worktrees `.wt/fe3-grab`, `.wt/ff-count`, `.wt/ff-art` removed (branches stay on origin; ff-art's offline proof folder inside its worktree went with it).

**Merged on `hc/integ-ff` (from `haunted-castle` `3e84389`), in order:** hc/ff-art `7791610` (`555fbb0`), hc/ff-count `e7d7ce6` incl. hc/fe3-grab `82e9e2a` (`1ed4e4b`). Both clean (Game.cs and ASSET_LOG.md auto-merged; the table-candle 14 m rule and the torch plan sit side by side). Handoffs: `docs/haunted-castle/handoffs/{ff-art,ff-count,fe3-grab}.md`.

**Integrator change: the one tuning pass** (`9269090`; values from capture sweeps on the first FF build `60e4443`, folders `captures/ff/sweep/{m1..m4,t1..t3,x1,x2,x1on}`)
- Hall (S1 failed table and floor mirror): `CastleLightTuning.TableCandleIntensity` .7 -> 1.2, floor sheen `_Strength` 1 -> 1.6 (`CastleWorld.FloorSheenStrength`), table sheen 4 -> 6.4, `CastleAtmosphere.HallMistBrightness` .30 -> .26 (pays for the extra light: S1 off near-black 30.6 -> 30.2). Table px at luma >= 14: 7.6k -> 10.8k; 1.6 / 2.2 gave only 12.3k / 12.8k, so **15k is out of reach by light** (far half of the table is dark wood edge-on, 14.2k px >= 8). Floor mirror 64 -> 362 px (pass).
- Moat (S4 failed glitter sd, red moon, both seams): `CastleWater.shader` defaults `_Reflect` 2.2 -> 1.4, `_Ripple` .3 -> .2, `_SkySoft` .06 -> .15, `_RedGlade` 1 -> 4, `_RedGladeWidth` .07 -> .10, `_GladeBody` .25 -> .08, and new `_RockFade` 4 m: the rock outcrop's enter-only reflection fades in with the ray's run to the slab. ff-art's enter-only fix had turned the old seam into a straight bright/dark line along z -6.6 across S4 (R 200 -> 35); it is gone. `tools/ff_moat_twin.py` does not carry these values yet.
- Both maps re-baked after it (`b8662ce`).

**Build:** `Builds/macOS/Hollow Current.app` (both maps), Assembly-CSharp.dll **2026-10-19 14:03:44**, built right after bake commit `b8662ce` (14:03:31; code `9269090`). All proofs below ran on it (the first build `60e4443` was used only for the sweeps).

**Prepare log** (`.castle-bake-ff2.log`): `HC_KIT_AXIS_SUMMARY problems=0`, `HC_DOOR_LEAF_SUMMARY problems=0` (both stages), `HC_CANDLES floating=0 PASS`, `HC_CHAPEL_PROPS overlap=False`, `HC_TELESCOPE clear=True`, `HC_HEARTH window_overlap=False PASS`, `HC_HIDE_SPOTS_SUMMARY failed=[]`, `HC_SKELETON PASS`, `HC_HALL_AXIS PASS`, `HC_CHAPEL PASS`, `HC_EXTERIOR_AXIS ok`; Castle 8 maps / 197 probes / 17 reflections, Bellwether 4 / 124 / 9. No CastleWater/FloorSheen "unavailable" warning, no `HC_MISSING_ART` except the 7 known portraits.

**Static checks (HEAD):** check_source PASS (1455 assertions incl. rules tests; known CS0649/CS0219/CS0162 warnings), check_castle_scares PASS, check_audio_keys OK (69 uses), check_audio_quality OK (new `voice_cc_hunt_found_b/c`, `smell_b`, `run_b` at -24.2 LUFS, peaks -9.7..-10.9, tails 0.12-0.18 s), check_throwables_scene rc 0, check_castle_doors OK, check_castle_kit OK (log and scene), bellwether_fuzz identical (`ee3b74dc...`).

**Proofs** (logs `.playtest-ff-<run>.log`, frames `docs/haunted-castle/rounds/r14/captures/ff/<run>/`, local, gitignored). Runs: natural x5 (natural4/5 added because natural1-3 gave only 5 apparition leads, ff-count's recipe), cold x2, siege_held x2, fear, huntcam, death, escape, doorproof, settings, Bellwether `-hc-verify` and `-hc-ui-only`; captures rooms off/on, ext, layout off/on, lantern, doors, title.

| Proof | r14 critic (6229407) | FF (this build) | Verdict |
|---|---|---|---|
| Hunt share, naturals | 16.1 / 25.1 / 20.3 % | 22.9 / 27.8 / 21.4 (n4 23.3, n5 22.2) | PASS |
| Longest gap | 162 / 150 / 182 | 165 / 177 / 220 (145, 207) | PASS |
| Hunts <= 10 s after an apparition | 0/7 | 0/11 naturals, 0 in sieges/fear | PASS |
| Apparition CV pooled | 0.50 | 0.71 (n1-3, 15 gaps), 0.72 (n1-5) | PASS |
| First-hunt spread | 105 s | 86 s (92 / 178 / 141) | PASS |
| Visible apparitions < 480 s | 4-5 | 6 / 4 / 4 / 5 / 5 | PASS |
| Hidden survival (siege_held x2) | 8/11 = 73 % | 8/12 = 67 % (4/6, 4/6) | PASS |
| Clock: threats 25-60 s after an apparition | 6/6 | 1/5 (n1-3); **2/8 = 25 %** (n1-5) | PASS (<= 50 %) |
| Clock: >= 1 threat 10-20 s after one | 0 | 0 (n1-3); **1** (n1-5: natural4 `follow=fired since=15.5`) | PASS only with n4/n5 (see open item 1) |
| Grab mix, natural x3 + cold x2 + huntcam | m9 59 %; side 0 | 11 commits: m8 4, m9 4 (**36 %**), m10 2, m11 0, m12 1 | m9 PASS, m10 PASS, **m11 0** in this set; natural5 has the only m11 |
| Grab mix, natural x5 | | 12: m8 4, m9 2 (17 %), m10 2, m11 1, m12 3 | PASS |
| Grab mix, every run incl. sieges/fear | | 22: m8 7, m9 9 (41 %; 5 of them hide grabs), m10 2, m11 1, m12 3 | hide grabs are crawls by design |
| Drops: clip_start_t <= 0.1, headAboveEye >= 1.0 at t 0.01 | starts .22-.50 | 0.00-0.07; 2.69-3.22 m (5 drops); rise `result=commit` x4, `timeout` x1 | PASS |
| Planned drop -> 9 with ceilingMax >= 4 | present | 0 in naturals; 1 in fear (`path=hideopen`, a hide grab) | PASS (note) |
| Dropped side -> side commit same hunt | 2 drops | no `dropped=side-*` at all; `stage=adapt` x2 | PASS |
| Hide grabs (grab_luma t 0.16) | 5.0 / 5.0 / 9.6 | siege1 31.1 / 27.4, siege2 30.3 / 27.9, fear 51.4; box > 60: 12-30 % | PASS 5/5 |
| Pop logging / root slide | blind | samples carry skull/rootReseat/countRect; `rootSlideOk` 21/22; fear m9 hideopen `root_slide=0.91` | note |
| Variant continuity / cuts | | `variantContinuity` 6/6, `variantCuts=0` | PASS |
| Grab continuity / HUD / first frame | 17/17 | every grab `pass=True`, HUD 0 px, first frame visible, cam min 0.65 | PASS |
| Dives resolved | | 25 dives, `diveUnresolved=0` everywhere | PASS |
| Torch dive share | 7/13 | 4/25 dives = 16 %; failures 26 (dive 4, hunt 22) | PASS |
| Post-win kill | `would_die=true phase=Won` x2 | 0; Return kills read `phase=Return huntActive=True phaseAfterPresentation=Return` | PASS |
| Cold x2 | gate on the letter, hunt 105 s | `gate=cap firstDocAt=-1 firstDocKind=none letterAt=4`, first hunt 161 / 163 s, told (lead 11.2 / 12.8), `HC_PLAY_COLD pass=True` x2 | PASS |
| Voice | found 10/27 | naturals 17 lines, max 24 % (`cc_hunt_found_b/c`, `cc_bargain`); all runs 33 lines, max 18 %; `from_other=0` every log | PASS |
| S1 lancets >= 150 px, moon | fail | 3 lancets (N +-2.6 192 rows, S-5.6 284), moon 373 px in the west lancet | PASS |
| S1 piers >= 2:1 | fail | 2.73 (proxy geometry) | PASS |
| S1 table >= 15k px lantern off | fail | **10,775** | **FAIL** (light-limited) |
| S1 floor mirror | fail | 362 px | PASS (after tuning) |
| S4 red moon mirrored | 3 px | 5,834 px | PASS (after tuning) |
| S4 pale glade / moat R/G | | 188 px / 3.54 | PASS |
| S4 seam battlement_out / bridge_left | 121.4 / 27.7 | **32.9** / 6.5 | battlement **FAIL**: the step is the warm lamp-glint column beside the bridge pier at x 884, not a material seam |
| S4 moat sd right / left | 30.7 / 22.4 | **12.6 / 16.3** | **FAIL** (<= 12): no glitter left; the left patch holds the pale moon column's edge and a bright sky band beside the rock |
| S4 / S1 frame time p99 | 17.56 | S4 11.11 / 11.36, S1 11.11 / 11.54, hunt_dive 11.22 | PASS |
| Exposure lantern off (35-55) | S1 30.0 | S1 30.2; hall from_doors 35.8, side_east 36.8 (now in band), pier_gap 36.7, tail 37.6, side_west 49.4, skeleton_head 33.0; S2 42.1, S3 36.0 | S1/skeleton_head WARN (director accepts 29-35) |
| Exposure lantern on (12-25) | | S1 13.2, S2 21.6, S3 16.7, hall 12.2-21.5; chapel confessional 25.4, from_great_doors 27.5, guard_door 35.1 (as FE) | as FE |
| fear / huntcam / death / escape | | fear WON 0 blockers; huntcam `dead=True`, commit m8; death `deaths=1 penalty=True`; escape WON 0 blockers | PASS |
| doorproof / doors | | `HC_DOORPROOF toward=0 toggles=8 unfocused=0 leaves=4 PASS`; doors capture both leaves `swing=away` | PASS |
| UI | | `HC_DOC_LAYOUT overlaps=0` on journal, ending, ending_lost, settings | PASS |
| Bellwether | `performance_pass=False` | `-hc-verify`: `HC_NATIVE_ROUTE_PASS`, 64 PASS, 0 FAIL, `HC_PERF pass=False engine_cost_over_16ms=88 (max 33.2)` (known); `-hc-ui-only`: `HC_VERIFY PASS Native UI at 1920x1080`, closed by SIGTERM at its timeout (as FD/FE) | as before |
| Exceptions | 0 | 0 in every log | PASS |

`tools/check_art_views.py captures/ff/{rooms_off,rooms_on,ext}`: `HC_ART_VIEWS pass=False fail=6 warn=1 tests=23` (fails: S1 table; battlement seam; S4 sd right/left on both rooms captures). Before the tuning pass the same build of the merged code gave fail=10.

**Looked at (frames)**
- S1 vs `refs/great_hall.png`: the north lancets now read as windows, the pale moon in the west one and the blood moon in the east one as in the reference; the blind lancets at the top corners read as leaded windows; the table is lit and reads, but it is small and far, the shrouded ribcage fills the bottom centre, and the hanging candelabra and gallery rail cut across the lancets. Composition still far from the reference (table in front, skeleton behind, three tall lancets dominant).
- S4 vs `refs/exterior_sky.png`: the moat is now smooth blood with a red column under the red moon and a pale one under the pale moon; no glitter, no z -6.6 line. It still reads as flat bright red (little mirrored castle). Moons are elliptical at FOV 90 (unchanged).
- S5: both moons sit in the rose behind the Count, large (the distant-moon trick at 3 m), readable.
- Hide-open grab (`siege1/06-07`): t 0.01 dark, t 0.16 her lantern beam and warm glow light him in the doorway: reads as her lantern swinging up, no flat lift.
- Side snatch (`natural1/18-21`, m10 left): he enters from her left edge with the torch out, lantern-lit only; at t 0.30-0.46 her hand lantern covers part of his body. Readable, dark.
- Drop (`natural1/10-13`): he falls from 2.7 m over her eye, continuous, claws spread at t 0.16-0.30. Good.
- battlement_out: dark water, a warm glint column under the bridge lamp (the remaining step); no grey lobe seam.

**Open items (new first)**
1. Follow leads are almost always cancelled: 9 follows drawn over 5 naturals, 1 fired, 7 `cancelled reason=grace`, 1 `held`. Apparitions after the first hunt mostly land in the post-hunt grace (30-90 s), so a follow due 12-19.5 s later is still inside it. The 10-20 s band passes only by the one natural4 follow. Fix for the next wave: at the draw, only take a follow if the grace ends before its due time (else draw again between rest/open), or let a follow due in the last part of a grace end the grace; re-check share (naturals now 21-28 %).
2. S1 table 10.8k px (15k): needs a layout/camera change (table nearer the eye or skeleton/table order as in the reference), not light.
3. S4 moat sd 12.6 / 16.3 and the battlement lamp-glint step 32.9: the tests count the moon column edge and the lamp column; the water itself has no glitter. A critic may still call the moat flat bright red.
4. m11 committed once in 5 naturals (none in natural x3 + cold + huntcam); fear's hideopen crawl slid 0.91 m (`root_slide`), one drop rise ended `result=timeout`.
5. Carried: hall lantern-off S1 30.2 / skeleton_head 33.0 (< 35), chapel confessional / guard_door off and on out of band (as FE), Bellwether `performance_pass`, owner listen of the Kokoro lines (4 new), FD item 6. Merges to `main` need the owner.

---

# HANDOFF: fe2-count (wave FE2: grab variety, siege hidden survival, apparition floor). Read this section first.

Branch `haunted-castle` = `hc/fe2-count`, pushed. Builder: Opus 5.5 (piece fe2-count), main clone, 2026-10-19. Nothing left running; GPU slot released (0/2 holders).
- **Final build:** `Builds/macOS/Hollow Current.app` (both maps), Assembly-CSharp.dll 2026-10-19 08:31, built right after bake commit `b783b1c` (r3; code `181057e`). Three bake+build rounds (the budget): r1 `a3b5e69`, r2 `d263f15`, r3 `b783b1c`. Both maps re-baked every round (Presence/ScareDirector/Core changed).
- PlayerPrefs restored: `unity.Bellwether Field Office.Hollow Current` `hc.castle.runs` = `L,E,L`; `com.bellwether.hollowcurrent` = `E,M,B`; `hc.castle.grabdeck` deleted. The grab deck carried across r1-r3 runs and was reset once before r3 natural4 (fresh-player deck). Backups: session scratchpad `fe2/{u,c}.backup.plist`.
- `CODEX_HANDOFF.md` still uncommitted and untouched; `rounds/r13/critic/` untracked. Evidence (local, gitignored): logs `.playtest-fe2-<round>-<run>.log`, frames `docs/haunted-castle/rounds/r13/captures/fe2/<round>/<run>/`. Analysis script: scratchpad `fe2/an.py`.

**What changed** (`ab1eab6`, `dfc21c0`, `181057e`)
- Grab variety (`Presence.cs`, `CountClaudeProfile.cs`): drop-spin start searched over bearings 80/100/60/120/140/160 (the 3 m drawbridge only has points behind her); side-snatch frame may turn 0/20/-15/35 deg toward his side; an approach may bend (NavMesh route <= 4 legs and <= 1.7x straight + 1 m, or within 9 m of him any route <= 14 m whose last sample sees her eye); a dive senses her within 3 m whatever his facing (fear's "outside the sight cone" hover over her head); a planned drop-spin with him already above her (>= 1.8 m, within stand-off + 0.2) commits from there; after 2 abandoned variant plans in a hunt the next dive forces the simplest feasible variant; floor step 0.4 m (her transform on a 0.27 m altar step read "no floor at her level" everywhere); the drop-spin view follows his skull at 0.55 of the half FOV (r1 fear left the frame at t 0.16-0.30); a released grab ends its plan; deck targets drop .45 / side .22 / spin .15. Every n/a reason is now per side (`L ...; R ...`).
- Siege (`NearMissDice.cs`, `Presence.cs`): base open roll 0.24 -> 0.28; at Dread >= 50, after 2 failed 0.33 first-visit attempts he checks her hide (`reason=pity`). Model (`WaveFE2Tests`, 20k sets): mean hidden survival 72.6 %, pooled <= 85 % in 89 % of 3-siege sets, quick re-open 82 %.
- Apparitions (`ScareRhythm.cs`, `ScareDirector.cs`): a due apparition with no visible point retries every 2.5-4 s (was 12-25 s) with a wider fan when the floor is behind (`VisiblePointWide`); while the floor is behind, the grace's last-20 s margin no longer blocks (the 11.5 s hunt gate still holds); a due time lost in a block with the count 2 behind comes 4-10 s after the lift. Model: >= 4 in 480 s 0.969 -> 0.998, 3-run CV >= 0.5 share 0.92 (bar 0.9), hunts <= 10 s after an apparition 0.
- Driver (`PlaytestDriver.cs`): natural stops stand on the NavMesh floor under the stop (the rune-ring stop put her on a 1.88 m top, the print stop on the floor above); forced dice wait out the apparition gate; a failed recording is retried once after interference (r2 fear lost 5 steps to these two).
- `CaptureMode.cs`: `FRAMETIME_HITCH` lists every frame > 14 ms with GC / alloc / log / texture-streaming counts.

**Diagnosis, band 2:** dice, not a regression. The FE gate/floor did not reduce visits per hunt; FE siege1 rolled six 0.33 first-visit attempts in a row (never came), siege3 three, so visits were 0/5/3 (FD 5/3/5) and 1 open in 8 visits. The pre-change model gives 78.8 % expected survival with only 72 % of 3-siege sets passing.

| Band | FE (5d4ab3b) | r1 | r2 | **r3 final** | Verdict |
|---|---|---|---|---|---|
| m10 / m11 / m12 commits | 0 / 0 / 0 of 29 | 2 / 0 / 2 | 3 / 1 / 0 | **0 / 3 / 3** (9 naturals + fear + huntcam) | m10 **FAIL** on the final build (5 on r1+r2) |
| Open grabs; drops with headroom | 18; 6/15 = 40 % | 9; 4/7 | 11; 3/9 = 33 % | **24; 11/15 = 73 %** | PASS |
| Dives unresolved | 0 | 0 | 0 | **0** (38 dives, 32 commits, 13 misses) | PASS |
| Continuity / HUD / first frame / cam | pass | 16/16, 0 px, all visible, 0.65 | 18/18 | **39/39 `pass=True`, HUD 0 px, 39/39 visible, cam min 0.65** | PASS |
| Hidden survival, siege_held x3 | 17/18 = 94 % | 15/18 = 83 % | 12/18 = 67 % | **12/18 = 67 %**; visits 4/6/6, opens 1/2/3; re-open within 2 visits in siege2 and siege3 | PASS (r1-r3 pooled 39/54 = 72 %) |
| Visible apparitions < 480 s | 7/9 runs >= 4 | 5/4/3 | 5/4/5 | **9/9 runs >= 4** (5 4 5 / 4 5 5 / 5 5 4) | PASS |
| Apparition CV, 3-run sets | 0.64-0.72 | 0.56 | 0.63 | **0.55 / 0.43 / 0.69** (9-run pool 0.57) | PASS on the test set (natural1-3); one extra set 0.43 |
| Hunts <= 10 s after an apparition | 0 | 0/28 | 0/27 | **0/44** | PASS |
| Hunt share / longest gap (naturals) | 15-30 % | | | 0.164-0.262 / 131-227 s | PASS |
| S4 p99 rooms_off / rooms_on | 17.97 / 15.83 | 14.59 / 11.48 | | **11.57 / 11.40** | PASS |
| fear / huntcam / Bellwether `-hc-verify` | | WON / commit m8 | fear FAIL (driver) | **fear WON 0 blockers; huntcam commit m8; `HC_NATIVE_ROUTE_PASS`, 64 PASS, 0 FAIL** (`HC_PERF pass=False engine_cost_over_16ms=121`, known) | PASS |
| Exceptions | 0 | 0 | 0 | 0 | PASS |

Grab histogram, final build (HC_GRAB_COMMIT, all r3 runs incl. sieges): m8 16, m9 17 (6 of them siege hide grabs), m10 0, m11 3, m12 3.

**Looked at (frames):** side snatch `r1/natural3/10-16` (m10, gallery): he lunges in from her left edge, skull at x 0.32 at +0.30 s, centred by 0.60, no pop (seat 0.25 m). `r3/natural6` m11 (frame turned 35 deg): skull in the central 60 % at every sample, continuity pass, but seat gap 0.83 m (variant seats are not reseated). Drop-spin `r3/natural2/10-16`: he is in frame at every sample and falls continuously, skull x 0.68 at +0.30 s, but at +0.30 s her own torch hand covers his head; r1's drawbridge spin (`r1/fear/32-39`, before the view tracking) left the frame at 0.16-0.30, fixed in r2.

**S4:** the hitches are isolated 15-19 ms frames (2-3 per 3 s window) with no GC collection, no log line, no texture streaming in them (`FRAMETIME_HITCH`); not reproducible as a cause, so no fix. p99 passed in every r1/r3 capture.

**Open items (new first)**
1. m10 (left side snatch) 0 on the final build: the deck carried 5 left commits from r1/r2, so left plans were rare; the two that came (`r3/natural9`) were dropped "at the start point, came in low (1.5 m)" (PredictGrab's bearing / SideClear check at commit fails) and by a 8-leg detour. Next step: log the bearing and SideClear frame at that drop and let a planned side commit when the bearing is within 25-115 deg.
2. Variant seat gaps 0.6-1.0 m (m11 0.83/1.01, m12 0.60-0.61; r2 m10 2.91) slide over 0.35-0.9 s: the FE reseat rule exempts side/spin. Frames show no pop, but a critic may see the slide.
3. One extra 3-run set had apparition CV 0.43 (floor regularises the gaps); the model keeps 92 % of sets >= 0.5.
4. 13 of 38 dives end as misses (moving away from a raised/hall spot, recommit blocks after a driver-blocked kill, "path blocked by Ledger: the painter" at the altar).
5. Carried from FE: hall lantern-off exposure (4 views 30-32 vs 35), FD items 6/8. Merges to `main` need the owner.

---

# HANDOFF: wave FE integrated, built and proved (fe-integ). Read this section first.

Branch `haunted-castle` = `hc/integ-fe`, pushed. Integrator: Opus 5.5 (piece fe-integ), main clone, 2026-10-19. Nothing left running; GPU slot released (0/2 holders).
- PlayerPrefs: `unity.Bellwether Field Office.Hollow Current` `hc.castle.runs` = `L,E,L` (never changed); `com.bellwether.hollowcurrent` put back from `E,M,L` to `E,M,B`; `hc.castle.grabdeck` deleted (written by the runs). Backups: session scratchpad `feinteg/{u,c}.backup.plist`. These were taken after the hall round, so `c` holds `L,E,L`; the target values above are the ones restored.
- `CODEX_HANDOFF.md` is still uncommitted and untouched; `rounds/r13/critic/` stays untracked. Worktrees `.wt/fe-count` and `.wt/fe-ui` removed (branches stay on origin).

**Merged on `hc/integ-fe` (from `haunted-castle` `1cd1ac9`), in order:** hc/fe-exterior-next `b99f1ef`, hc/fe-ui `80f7d0c`, hc/fe-count `7a65068`. All merged cleanly (RulesTests.cs and CaptureMode.cs auto-merged). Handoffs: `docs/haunted-castle/handoffs/fe-{exterior,ui,count}.md`.

**Integrator changes**
- Hall-only lantern-off round (director's one round), **reverted**: `3a441f5` scaled the four standing hall floor candelabras x0.75 and their floor-sheen reflections x0.5 (runtime, castle only, with `-hc-hall-candle`/`-hc-hall-sheen` capture overrides); `5224b8a` reverts it. Measured on the round's build (`92f2ab4`): S1 off 30.2 -> 31.2, from_doors 33.3, skeleton_head 33.8, side_east 31.8 (all < 35), side_west off 54.1 and on 26.5 (out of 12-25). A sweep with both knobs at 0 (stands dark, no stand reflections) gave S1 32.1, from_doors 34.2, skeleton_head 35.1, side_east 33.0: **this lever cannot reach 35 even at zero.** The lit pixels in the lantern-off S1 frame are the pale gallery balustrades, the piers and the vault (baked window light plus the balcony warm fills and the floating-candle flames), not the floor stands. Evidence: `captures/fe/{rooms,layout}_{off,on}` of that build were overwritten by the final build; the sweep is `captures/fe/sweep0_{rooms,layout}_off`, logs `.playtest-fe-sweep0_*.log`.
- `2ed3b5e` new castle driver mode `settings` (`-hc-playtest <dir> settings`): pauses castle play and frames settings pages 0-2. fe-ui's `-hc-verify -hc-ui-only` recipe runs in Bellwether, where the castle card is never drawn. `Interface.ShowSettingsPage(int)` added for it; Bellwether paths unchanged (fuzz identical).
- `5b32ead` driver frames of an armed escalation (`HC_PLAY_ESC_SHOT`, frames `NN_esc1_t+X.png`, 0-16.5 s after the apparition). `21ada18` huntcam frames the castle lost card (`NN_lost_card.png`).
- `e3b4094` the natural driver waits out a dive (<= 12 s) before its 20 s walk teleport. In natural4/5 both side-snatch plans ended `miss=True reason=moving away` because the walk teleported her from the gallery (9.0, 3.3, 16.5) to (12.0, 0.35, 6.5) mid-dive. Only natural7-9 ran with this.

**Builds.** Final: `Builds/macOS/Hollow Current.app` (both maps), binary (Managed/Assembly-CSharp.dll) 2026-10-19 04:54:46, after bake commit `92e6b88` (04:54:34). Every `.cs` change re-baked both maps (5 rounds, Castle 8 maps / 197 probes / 17 reflections, Bellwether 4 / 124 / 9). Proof builds differ only in `PlaytestDriver.cs` (+ the `Interface` settings hook from B1 on): B1 `6baaaee` (02:32:45: all captures, natural1-3, cold, siege1-2, fear, huntcam, death, escape, doorproof, settings, Bellwether verify/ui-only), B2 `96beecf` (04:07:00: huntcam2, natural4-6, siege3), B3 = final (natural7-9).

**Prepare log** (`.castle-bake-fei5.log`, same every round): `HC_KIT_AXIS_SUMMARY problems=0`, `HC_DOOR_LEAF_SUMMARY problems=0` (both stages), `HC_CANDLES floating=0 PASS`, `HC_CHAPEL_PROPS overlap=False`, `HC_TELESCOPE clear=True`, `HC_HEARTH window_overlap=False PASS`, `HC_HIDE_SPOTS_SUMMARY failed=[]`, `HC_SKELETON PASS`, `HC_HALL_AXIS PASS`, `HC_CHAPEL PASS`, `HC_EXTERIOR_APPROACH braziers=2 lamps=4`, `HC_EXTERIOR_AXIS ok`, `HC_EXTERIOR_COST stage=prepare total renderers=2174 draws=2587 rt_lights=45 lit_passes=6726`.

**Static checks (HEAD):** check_source PASS (1301 assertions incl. rules tests; known CS0649/CS0219 warnings), check_castle_scares PASS, check_audio_keys OK, check_audio_quality OK, check_throwables_scene rc 0, check_castle_doors OK, check_castle_kit OK (log and scene), bellwether_fuzz identical (`ee3b74dc...`), `docui_journal_mock.py` overlaps=0 on journal/ending/ending_lost/settings.

**Proofs** (logs `.playtest-fe-<run>.log`, frames `docs/haunted-castle/rounds/r13/captures/fe/<run>/`, local, gitignored)

| Proof | r12 / FD (408f406) | FE (this build) | Verdict |
|---|---|---|---|
| Hunts <= 10 s after an apparition (pooled naturals + sieges) | 3/6 = 50 % | **0/37 = 0 %**; every close one logged `HC_APPARITION_GATE` | PASS (<= 35 %) |
| Hunt share, naturals | 0.174 / 0.089 / 0.244 | 0.173 0.196 0.229 / 0.235 0.195 0.172 / 0.201 0.207 0.175 (Late runs 3, 5, 7: 0.229, 0.195, 0.201) | PASS x9 (15-30 %) |
| Longest gap | 226 / 298 / 155 | naturals 142-216, sieges 162-219 | PASS (>= 120) |
| Apparition CV (3-run pools) | 0.57 | 0.64 / 0.69 / 0.72 | PASS (>= 0.5) |
| First-hunt spread (3-run sets) | 118 s | 68 / 108 / 90 s (hunts 127 98 166, 84 192 145, 189 130 99) | PASS (>= 45) |
| Hidden survival (siege_held) | 11/18 = 61 % | **17/18 = 94 %** (6/6, 5/6, 6/6) | **FAIL** (<= 85 %): dice. NearMissDice/open roll unchanged since FD; siege1 rolled 6 base attempts all > 0.33, siege3 3 open rolls 0.37/0.40/0.67 vs p 0.24. FD+FE pooled 28/36 = 78 % |
| Visible apparitions < 480 s | 5 / 5 / 4 | 5 4 5 / 3 5 4 / 3 4 5 | **FAIL in 2 of 9** (>= 4): natural4 [220,251,460,490], natural7 [90,333,478] |
| Escalation | hunt 1 s after apparition | natural2, 4, 9: `deferred=escalation tellIn=10.9-11.0 huntIn=12.4-12.5` -> `escalation=fired` -> `HC_WARNING escalated=True length=1.5` -> hunt | PASS (see judgement below) |
| Share floor | n/a | armed 4x; cancelled by a real hunt each time (`reason=phase_Hunt/Warning`), pulled once (natural1, t 379) | works |
| Grab variety (all runs) | m8 4, m9 many, m11 1, m10 0, m12 0; openHeadroom 0 | commits m8 15, m9 14, **m10 0, m11 0, m12 0**; open 18 (>= 12), openHeadroom 15, openHeadroomDrops 6 (40 %) | **FAIL** (m10/m11/m12 >= 1 each); openHeadroom > 0 and drop share PASS |
| Dives resolved | 0 misses logged | 40 dives, 25 commits, 16 misses, **diveUnresolved=0 in every run** | PASS |
| huntcam | no commit, no miss | huntcam: drop-spin planned, fell through to drop, `end=commit mode=8`; huntcam2: `end=commit mode=9` | PASS |
| First frame | 8/9 visible | 25/25 `visible=True`; snap > 90 only with `cut=True` (96-139 deg); seatGap <= 0.45 except crawls 0.70 / 0.83 / 1.20 (vertical: he climbs to her raised eye, frames show no slide) | PASS with note |
| Grab continuity / HUD / cam | | every grab `pass=True`, HUD `pixels=0`, cam min 0.65, 0 teleports during a grab | PASS |
| hunt_dive frame time | p99 110 ms | `hunt_dive` p99 11.74 max 15.42 (18 of 880 capture frames excluded); `hunt_dive_raw` p99 110.5 | PASS |
| S4 frame time (p99 <= 16.7) | mean 16.7 p99 23.1 (fe r3 22.9) | rooms_off mean 11.23 **p99 17.97** (max 18.6; main thread 17.96, render 7.8); rooms_on mean 11.19 p99 15.83; profile baseline 11.53; `HC_DEEP_INTERIOR_DOORS gatehouse_exits=4`, `MASK on=True lights=57` | mean fixed; p99 PASS on rooms_on, **0.3 ms over on rooms_off**: one-tick main-thread hitches (S1 rooms_off p99 16.50 the same way), not the render thread |
| gate_out / battlement_out / S1 p99 | 11.9 / 16.5 / 11.1-11.7 | 11.80 / 11.63 / 11.69-16.50 | PASS |
| Light-mask look (S4, gate_out, battlement, S1 vs fe r3) | | S4 34 px, gate_out 0, bridge views 0, S1 495 px (flames) differ by > 12; battlement_out only moat glitter re-rolls, battlement_in only the Count's pose | no visible change |
| Moat | bright 273/263/201 | gate_out 868 R/G 2.91, S4 505 R/G 2.60, battlement 2092 R/G 3.01 | PASS |
| Exposure lantern off (35-55) | S1 30.4; hall 32.6/31.8/30.4 | S1 30.0, from_doors 32.3, skeleton_head 31.7, side_east 30.4 (hall round reverted); S2 42.0, S3 36.2, side_west 52.2, tail 37.1, pier_gap 38.8; chapel altar_approach 26.4, confessional 62.3, guard_door 60.1 out as before | **FAIL** (4 hall views, unchanged) |
| Exposure lantern on (12-25) | | S1 13.7, S2 21.7, S3 16.7, hall 13.1-20.3, side_west 25.1; chapel confessional 25.4, great_doors 27.3, guard_door 35.2 (as FD) | as FD |
| UI: HC_DOC_LAYOUT | journal/ending 0 | journal 0 (escape, fear), ending 0 (escape, fear, naturals), ending_lost 0 (death, huntcam, huntcam2), settings 0 (settings run); no `violation` line | PASS |
| UI frames | | settings p0-p2 (`settings/02-04`): text clear of filigree and seal; lost card (`huntcam2/24_lost_card.png`): paper card, RETRY CHECKPOINT + NEW INVESTIGATION + EXIT, no overlaps, no seams; FILE RECONSTRUCTION slip (`escape/16_journal.png`) readable, ink (61,34,22), about 5.8:1 | PASS |
| voice | from_other=0 | every log `from_other=0`, 0 `voice not from the Count`; escape `HC_EVP from=trumpet verbal=False clip=castle_evp_static` | PASS |
| cold x1 | PASS | `HC_PLAY_COLD ... hidesAnswered=5/5 pass=True`, `HC_HUNT_TELL kind=warning lead_s=13.4 first=True cold=True gate=page`, first hunt 107 s | PASS |
| escape / death / fear / doorproof | | escape WON 0 blockers; death `deaths=1 penalty=True`; fear WON 0 blockers; `HC_DOORPROOF toward=0 toggles=8 unfocused=0 leaves=4 PASS`; doors capture both leaves `swing=away` | PASS |
| Bellwether | | `-hc-verify`: `HC_NATIVE_ROUTE_PASS`, 0 `HC_VERIFY FAIL`, `HC_PERF pass=False engine_cost_over_16ms=100 (max 32.4)` (known); `-hc-ui-only`: `HC_VERIFY PASS Native UI at 1920x1080`, waits for keys at reconstruction, closed by SIGTERM (as FD) | as FD |
| Exceptions | | 0 in every castle log | PASS |

**Looked at (frames)**
- Escalation (`natural4/10-27_esc1_*`, also natural9): t+0 he rushes across the chapel pews close to her, t+1 he is gone in a cold mist puff, t+3..9 normal play (no Count, no UI change), t+11 Warning (`BREAK LINE OF SIGHT`, `SIGNAL COLLAPSING`, lights down), t+13.2 hunt from the rose-window perch, t+16.5 grab. **It reads as a deliberate tell, not a bug**: the vanish is clean and the hunt arrives with the full Warning. The 10 s of quiet loses the causal link a little; a player will more likely read "he left, then came back" than "that apparition escalated".
- Grabs: no side-snatch or drop-spin commit occurred. Turning first frames (35-79 deg) put him at the frame edge partly behind the lantern/hand, centred by t0.16; hard cuts (96-139 deg) are centred from frame 0. fear's Return grab on the drawbridge: four drop-spin dives in a row abandoned with him 0.2-0.4 m above her ("lost sight, reached her last known spot"), then a plain drop; its t0.01 frame is mostly dark wing.

**Open items (new first)**
1. Grab variety: 0 side (m10/m11) and 0 drop-spin (m12) commits in 29. Variant plans are mostly n/a (`flank closed`, `no open start point`, `no direct flight to its start point`, `she faces him`). The side plans that did happen (natural4/5/6, all from the gallery spot (9, 3.3, 16.5)) ended as a driver-teleport miss (fixed in the driver, `e3b4094`) or `dropped ... detour: 3 waypoints`. fe-count's feasibility gates are probably too strict for this castle; next step is fe-count's own recipe: log the n/a reasons per room and loosen one gate.
2. fear Return: repeated drop-spin dives abandoned directly above her (`CountChase` lands when `!CanSeePlayer && toGoal < 1` while a spin plan is still active). Resolved, but it reads as a hover; a plan should drop to the plain grab when he is already over her.
3. Hall lantern-off: 4 views 30-32 % against 35. The floor stands are not the cause (sweep at 0 gives 32-35). The bright surfaces are the gallery balustrades, piers and vault; next lever is the balcony warm fills (`BalconyFillIntensity` .7) or the baked hall window light, outside the director's one round.
4. Hidden survival 94 % on 3 sieges (dice; FD+FE 78 %). Run more sieges before tuning `NearMissDice`.
5. Visible apparitions < 4 in 8 min in 2 of 9 naturals (model 96.9 %; here 78 %).
6. S4 rooms_off p99 17.97 (main-thread one-tick hitches; mean 11.2, render thread 7.8 ms, fixed by the mask). Same signature as Bellwether's `performance_pass` item.
7. Carried: FD open items 6 (`showcase landed seen=False`), 8 (Bellwether `performance_pass`, floating hall candelabras, human listen of `castle_evp_static`/`castle_hide_whisper`); chapel views off/on (fc-light2 item 1). Merges to `main` need the owner.

---

# HANDOFF: fe-exterior (wave FE: S4 exterior frame time, moat glints, hall lantern-off knob). Read this section first.

Branch `haunted-castle` = `hc/fe-exterior`, pushed. Builder: Opus 5.5 (piece fe-exterior), main clone, 2026-10-19. Nothing left running; GPU slot released.
- PlayerPrefs: `unity.Bellwether Field Office.Hollow Current` `hc.castle.runs` = `L,E,L` (never changed); `com.bellwether.hollowcurrent` put back to `E,M,B` after every round (runs wrote `L,E,L` / `M,L,L`); no `hc.castle.grabdeck` key left. Backups: session scratchpad `fe/{u,c}.backup.plist`.
- `CODEX_HANDOFF.md` still uncommitted and untouched; `rounds/r13/critic/` untracked. Fix branch `hc/fe-exterior-next` is pushed but NOT merged or built (item 1).
- Details, method and risks: `docs/haunted-castle/handoffs/fe-exterior.md`.

**Build:** `Builds/macOS/Hollow Current.app` (both maps), binary 2026-10-19 02:00:37, built from the tree committed as bake `0a00c11` (02:00:39). Three castle bake+build+capture rounds (the budget): r1 `d3af2bf`, r2 `032f6b1`, r3 `0a00c11`; Bellwether not re-baked (castle-only code). Castle 8 maps / 197 probes / 17 reflections.

| Item | FD (408f406) | fe r3 | Verdict |
|---|---|---|---|
| S4 frame time rooms_off / rooms_on | mean 16.7, p99 23.1 | mean 14.3 / 14.6, p99 22.9 / 23.0 | **FAIL** (p99 <= 16.7) |
| battlement_out / gate_out_S6 | p99 16.5 (max 20.4) / 11.9 | p99 11.8 (max 17.6) / 11.8 | pass |
| Moat bright px2 gate_out / S4 / battlement | 273 / 263 / 201 | 868 / 511 / 2077 (R/G 2.91 / 2.60 / 3.01) | pass |
| Hall off S1 / from_doors / skeleton_head / side_east | 30.4 / 32.6 / 31.8 / 30.4 | 30.2 / 32.4 / 31.9 / 30.3 | **FAIL** (35-55); side_west 52.1 |
| All other exposure (off and on) | | within 0.3 of FD | unchanged |

1. **S4 (still out of band).** Measured with the new `-hc-exterior-profile` (per-piece toggles, FrameTimingManager split, render counters) and `HC_EXTERIOR_COST`: the frame is render-thread bound (11.6 ms, GPU 3 ms, same at 960x540). fd-art's pieces cost < 0.3 ms each; the cost is ~1,950 interior renderers behind the curtain wall drawn with forward-add passes from the 3 ForcePixel outside fills and the interior lights (10,616 batches; interior hidden: 748). Occlusion data had no effect (dropped). Kept: the outside fills skip the deep interior (layer 27, `HC_EXTERIOR_FILL_MASK`), no visual change in any exterior or interior capture. Not working: the deep-interior mask for the interior lights looked for a `Gate` door, but the gate is the drawbridge (`HC_DEEP_INTERIOR_DOORS gate=0`), so it never switched on; the profile variant with those lights culled gives S4 mean 11.17 / p99 11.69 (4,125 batches). `hc/fe-exterior-next` keys it on the three gatehouse exits instead: needs one castle re-bake, build and rooms_off capture.
2. **Moat (pass).** Blood-moon glade, wider pale glade with a weaker smooth lobe, glitter points on facing chop facets, castle-end lamp glints doubled; values from a `-hc-moat` sweep. Possible critic note: the pale glade's silver glitter covers a wide patch from the battlement.
3. **Hall lantern-off (one knob, failed, stopped).** Castle bake ambient x0.5 moved every view 0.0-0.2 points (reverted). No uniform knob can work: S1 needs +4.6 with only 3.4 % of pixels at luma 8-9, while side_west would pass 55 first. Needs a hall-local change on the near candelabras / east floor sheen (outside the fallback table): director's call.

**Static checks (HEAD):** check_source PASS (1260), check_castle_scares PASS, check_audio_keys OK, check_throwables_scene rc 0, check_castle_kit OK (`.castle-bake-fe3.log` and scene), check_castle_doors OK; prepare log `HC_KIT_AXIS_SUMMARY problems=0`, `HC_DOOR_LEAF_SUMMARY problems=0`, `HC_CANDLES floating=0 PASS`, `HC_SKELETON/HALL_AXIS/CHAPEL PASS`, `HC_HIDE_SPOTS_SUMMARY failed=[]`. Evidence (local): `docs/haunted-castle/rounds/r13/captures/fe/r3/{rooms_off,rooms_on,ext,layout_off,layout_on}`, logs `.playtest-fe-r3-*.log`.

**Open items (new first):** 1. S4 p99 (above, fix branch ready). 2. Hall lantern-off 4 views (above). 3. hunt_dive capture p99 110 ms unchanged (FD open item 4). 4. FD open items 1, 2, 6, 7, 8 carried. Merges to `main` need the owner.

---

# HANDOFF: wave FD (r13 critic fixes) integrated, built and proved (read this section first)

Branch `haunted-castle` = `hc/integ-fd`, pushed. Integrator: Opus 5.5 (piece fd-integ), main clone, 2026-10-18/19. Nothing left running; GPU slot released (0/2 holders).
- PlayerPrefs restored: `unity.Bellwether Field Office.Hollow Current` `hc.castle.runs` = `L,E,L` (never touched); `com.bellwether.hollowcurrent` `hc.castle.runs` put back from `M,L,L` to `E,M,B`, and the new fd-grab deck key `hc.castle.grabdeck` (written by the test runs, `0,..,8,6,0,1,0`) deleted. Backups: session scratchpad `fdinteg/{u,c}.backup.plist`.
- `CODEX_HANDOFF.md` is still uncommitted and untouched; `rounds/r13/critic/` stays untracked.

**Merged on `hc/integ-fd` (from `haunted-castle` `0c2796e`):** hc/fc-art2 `f4baa2e`, hc/fd-journal `feabe83`, hc/fd-voice `1054910`, hc/fd-rules `321aac2`, hc/fd-grab `8711324`, hc/fd-art `b696de0`. Conflicts: `ASSET_LOG.md` twice (all sections kept), `PlaytestDriver.cs` (natural teleport guard: fd-grab's `GrabBusy` version kept, fd-rules' copy dropped), `Interface.cs` (`Track`: fd-grab's `CountHudDraw()` + fd-journal's castle kicker colour). `CaptureMode.cs`, `Presence.cs`, `CastleScares.cs`, `CastleWorld.cs` merged cleanly. Handoffs: `docs/haunted-castle/handoffs/fd-*.md`, `fc-art2.md`.

**Integrator changes**
- `d320c45` `PanelCard.png.meta`: `nPOTScale` None + clamp. The default import made the 1448x1426 card 1024x1024, so `Interface.CastlePanel` was false and every castle journal fell back to the old stretched card (`HC_DOC_PANEL card=1024x1024`). Now `card=1448x1426`.
- `d320c45` doorproof shot names contained `/` (door names), so the first shot threw `DirectoryNotFoundException` and the proof hung after leaf 1. `5ff0d5c` + `67a633a`: a leaf that opens by itself as she walks in counts as the opening; the leaf is re-closed after the teleport to each side (it had re-opened `by=auto` while she was still on the other side); stand points move 0.8-1.4 m sideways when a hall pier blocks the eye line.
- `5ff0d5c` journal: nine-slice cell edges rounded to whole pixels (1 px dark seams at ColR/RowB on both cards in the first escape run); `InkSegment` pivot in screen space (the PULSE trace had scattered into loose dashes over the ledger rows; also visible in r13).
- `d320c45` `tools/check_voice_logs.py` matched `HC_COUNT_VOICE=0` inside `HC_PLAY_SUMMARY` as a voice line; tags now match as tokens.
- No lighting knob changed: fd-art did not move the hall out of band (see exposure), so fc-light2's one-round fallback was not applied.

**Builds.** Final: `Builds/macOS/Hollow Current.app` (both maps), binary 2026-10-19 01:09:07, after bake commit `a29044e` (01:08:54). Every `.cs` change re-baked both maps (4 rounds; Castle 8 maps / 197 probes / 17 reflections). Proof builds: B1 `c6974b6` (22:40:31, captures, cold, natural), B2 `5216d5b` (23:40:10, death, huntcam, siege x3, bwverify), B3 `f7a036f` (00:50:36, escape, kill, fear, ui-only), B4 = final (doorproof). B1-B4 differ only in the driver's doorproof, the PanelCard import and the two journal draw fixes.

**Prepare log** (`.castle-bake-fd4.log`, same in every round): `HC_KIT_AXIS_SUMMARY problems=0`, `HC_DOOR_LEAF_SUMMARY problems=0` (both stages), `HC_CANDLES floating=0 PASS`, `HC_CHAPEL_PROPS overlap=False gap=0.52`, `HC_TELESCOPE aim=window clear=True`, `HC_HEARTH window_overlap=False PASS`, `HC_HIDE_SPOTS_SUMMARY failed=[]`, `HC_SKELETON y_max=1.48 table_gap=0.63 PASS`, `HC_HALL_AXIS PASS`, `HC_CHAPEL PASS`, `HC_EXTERIOR_APPROACH lake braziers=2 lamps=4`, `HC_EXTERIOR_AXIS ok`. Runtime: `HC_INSTRUMENTS ... radio=1105 period=True`, `HC_COMPASS_DIAL dial_shader=HollowCurrent/CastleSprite glass_dome=False`.

**Static checks (HEAD):** check_source PASS (1260 assertions), check_castle_scares PASS, check_audio_keys OK, check_audio_quality OK, check_throwables_scene rc 0, check_castle_doors OK, check_castle_kit OK (log and scene), bellwether_fuzz identical, `docui_journal_mock.py` overlaps=0, `check_voice_logs.py` PASS on all 13 castle logs (`--require-evp --require-summary` on escape).

**Proofs** (logs `.playtest-fd-*.log` (B2 runs `.playtest-fd-b2-*`), frames `docs/haunted-castle/rounds/r13/captures/fd/<run>/`, local, gitignored)

| Proof | Result |
|---|---|
| Shaders | No pink Count (S5, grab and fight frames); floor sheen and blood lake render; wings dark red, no purple under the grave-ash lantern |
| Exposure off (35-55) | S1 30.4 (new 2.3 m eye), S2 42.2, S3 36.0; hall from_doors 32.6, skeleton_head 31.8, side_east 30.4 FAIL (fc3 30.5/29.5/27.5: 2-3 points darker), pier_gap 38.9, side_west 52.1, tail 37.0 ok. Chapel as fc3 (altar_approach 26.6, confessional 62.6, guard_door 60.4, font 55.3 out) |
| Exposure on (12-25) | S1 14.0, S2 21.7, S3 16.7 ok; hall 13.1-20.3 ok, side_west 25.1; chapel confessional 25.4, great_doors 27.2, guard_door 35.4 (= fc3, fc-light2 item 1) |
| S1 / S4 frames | Table, chairs, throne read over the shrouded skeleton; S4 search took the 22 m eye, FOV 90, both moons in frame unoccluded, spires and red lake in frame (moons visibly stretched at FOV 90) |
| Moat test | R/G gate_out 3.64, S4 2.42, battlement 3.43 PASS (>= 1.6); bright px2 273 / 263 / 201 FAIL (>= 400; masks made at 2416x1292, resized to 1920x1080). The lake reads flat saturated red with one white sparkle patch |
| Frame time | S1 p99 11.1-11.7, gate_out 11.9, battlement_out 16.5 (max 20.4); **S4 mean 16.7 p99 23.1 FAIL**; hunt_dive capture p99 110 ms / max 122 ms (fc3 max 21.3), but fear great hall p99 11.53 (3 frames > 16.7) |
| cold x2 | PASS: `cold=True coldCap=150`, letter read at 4 s, `HC_HUNT_TELL kind=warning lead_s=12.4 / 13.1 first=True cold=True gate=page`, first hunt 95 / 99 s, hides 5/5, blockers 0 |
| natural x3 | WON x3, blockers 0, 0 exceptions. Plans E / L / M. First hunt 77 / 195 / 148, share 0.174 / 0.089 / 0.244, longest gap 226 / 298 / 155, apparitions < 480 s 5 / 5 / 4, `perch flight observed=True showcase=True` 2 / 1 / 1 (`showcase landed seen=False` in all three), teleports during grab 0 |
| r12 pass tests (naturals, sieges) | hunts <= 10 s after an apparition 3/6 = **50 % FAIL** (natural1 2/2); share 15-30 % **FAIL** in natural2 (0.089, Late band); pooled apparition CV 0.57 PASS; first-hunt spread 118 s PASS; gap >= 120 s PASS x3; hidden survival 11/18 = 61 % PASS |
| siege_held x3 | `openroll p=` lines, no `bag=`; opens 1 / 3 / 3, back-to-back opens within 2 visits in siege2 and siege3 PASS; survival 83 / 50 / 50 %; `HC_DOOR_HELD` 2/8 each |
| doorproof | final build: `HC_DOORPROOF toward=0 toggles=8 unfocused=0 leaves=4 PASS`, 8 frames show each leaf swinging away |
| doors capture | `hall_doors_open_inside` both leaves `swing=away` (angles 55 / -55), frame shows both leaves out into the courtyard |
| grab (all runs) | continuity 9/9 naturals + fear, death, sieges all `pass=True` (legacy_pass True); HUD `pixels=0 draws=0` every grab; cam distance min 0.65; first frame visible 8/9 natural grabs (natural2 return drop `visible=False lineClear=False`); snapDeg up to 138, seat gap up to 0.90 (natural1 return grab: he slides ~1 m back between t 0.01 and 0.16). **Variety FAIL:** m8 4, m9 many, m11 1, m10 0, m12 0; `openHeadroom=0` in every run so `dropShareHeadroom=n/a`. 0 `HC_COUNT_DIVE miss=True`. **huntcam FAIL:** planned drop-spin, he flew 12 m away along the gallery, no commit and no miss before the driver quit |
| voice | every log `from_other=0`, 0 `voice not from the Count`; escape `HC_EVP from=trumpet verbal=False clip=castle_evp_static`; B2 kill `HC_EVP from=head verbal=True id=cc_bargain` |
| journal / end card | `HC_DOC_PANEL card=1448x1426`, `HC_DOC_LAYOUT screen=journal overlaps=0`, `screen=ending overlaps=0` (escape, kill, fear); no seams, PULSE trace drawn as a line (`escape/16_journal.png`, `kill/20_won_kill.png`). Disabled FILE RECONSTRUCTION label still unreadable (pre-existing) |
| title | Bellwether card reads as a lit porch (`title/S7_title_castle_1920x1080.png`) |
| escape / kill / fear / death | escape WON 0 blockers; kill WON slain; fear WON on B3 (B2 fear lost one grave-ash record attempt and cascaded to 4 blockers; not reproduced); death + penalty PASS |
| instruments / kit | compass ivory dial, upright numerals, orange pips, no veil; radio = resonance box (`HC_CAPTURE_HELD tool=4 verts=65545`: counts more than the 1105-vertex box); telescope heads to the north window; hearth beside the west lancet |
| Bellwether `-hc-verify` | `HC_NATIVE_ROUTE_PASS`, 0 `HC_VERIFY FAIL`; `HC_PERF pass=False engine_cost_over_16ms=95 (max 33.1 ms)` (known) |
| Bellwether `-hc-ui-only` | `HC_VERIFY PASS Native UI at 1920x1080`; waits for human keys at the reconstruction step, closed by SIGTERM (as in FB) |

**Open items (new first)**
1. r12 pass tests failing on this sample: hunts within 10 s of an apparition 50 % (natural1 2/2) and natural2 share 0.089. fd-rules' apparition floor raises apparition count; its 15-50 s post-hunt apparitions may pull hunts close to apparitions. More naturals before tuning.
2. Grab variety: no side-left (m10) or drop-spin (m12) commit in 17 grabs; `openHeadroom` is 0 in every run (the headroom flag on open-floor commits never set), so the drop-share gate cannot be judged. huntcam's planned drop-spin led him away from her (fd-grab spin approach point). 0 readable misses.
3. S4 exterior frame time p99 23 ms (mean 16.7, at the 90 fps cap); moat bright-glint area below the critic's 400 px2 in all three views; moons stretched at FOV 90.
4. hunt_dive capture: about 9 frames of 110-122 ms per capture (fc3 max 21 ms). Gameplay great-hall frames are fine (p99 11.5); likely the screenshot frames are now counted. Not diagnosed.
5. Hall lantern-off still 30-33 % in 4 views (S1 30.4, from_doors 32.6, skeleton_head 31.8, side_east 30.4); chapel views off/on as fc3 (fc-light2 item 1: `ChapelCandleIntensity` .75 -> .9).
6. `showcase landed seen=False` in every natural; the observed flight is logged at departure/arrival but she does not see the landing.
7. Disabled FILE RECONSTRUCTION button label is unreadable on the castle card; settings and lost cards still use the stretched card.
8. Carried: Bellwether `performance_pass` False; floating hall candelabras (design floaters); human audio listen of `castle_evp_static` / `castle_hide_whisper`; merges to `main` need the owner.

---

# HANDOFF: integrator fc-integ2 (fc-bw60b merged: Present-wait subtraction for Bellwether `performance_pass`). Read this section first.

Branch `haunted-castle` = `hc/integ-fc2`, pushed. Integrator: Sonnet 5.5, main clone, 2026-10-18. Nothing left running; GPU slot released.
- The owner's `hc.castle.runs` (`unity.Bellwether Field Office.Hollow Current`) is still `L,E,L`.
- The test runs rewrote `com.bellwether.hollowcurrent` `hc.castle.runs` from `E,M,B` to `L,E,L`. It was put back to `E,M,B` (backups: session scratchpad `fcinteg2/{u,c}.backup.plist`).
- `CODEX_HANDOFF.md` is still uncommitted and was not touched.

**Build:** `Builds/macOS/Hollow Current.app` (both maps), binary 2026-10-18 20:01:10, built from bake commit `7bda81a` (committed 20:00:51), which sits on the merge of `hc/fc-bw60b` (`4b7c433`). Both maps were re-baked (three `.cs` files changed). Castle prepare log `.castle-bake-fcinteg2.log`: `HC_KIT_AXIS_SUMMARY problems=0`, `HC_DOOR_LEAF_SUMMARY problems=0`, `HC_CANDLES placed=46 ... floating=0 PASS`, `HC_CHAPEL_PROPS overlap=False gap=0.52 PASS`, `HC_HIDE_SPOTS_SUMMARY failed=[]`, `HC_SKELETON PASS`, `HC_CHAPEL PASS`; `tools/check_castle_kit.py` OK.

**Static checks:** check_source PASS (rules tests included), check_castle_scares PASS, check_audio_keys OK, check_throwables_scene rc 0.

**Proofs** (evidence local/gitignored: `docs/haunted-castle/rounds/r13/captures/fc/fc4/{bwverify,cold,escape}`, logs `.playtest-fcinteg2-*.log`)

| Run | Result |
|---|---|
| Bellwether `-hc-verify` | `HC_NATIVE_ROUTE_PASS`, 0 `HC_VERIFY FAIL`. `performance_pass` is still **False** (below) |
| Castle `cold` | `pass=True`: `HC_ONBOARD_START equipped=True`, first cue 9.2 s, 5 cues, hides answered 5/5, first hunt 81.9 s, blockers 0, 0 exceptions |
| Castle `escape` | `won=True`, blockers 0, 4 `HC_DOC_OPEN` (letter + 3 ledger pages), 0 exceptions |

`HC_PERF` end line, verbatim:
`HC_PERF pass=False reason=engine_cost_over_16ms=92(max 105.6 ms) frames_over_16ms=97 p99=11.9 floor=60 frames=37188 engine_cost_over_16ms=92 engine_cost_p99=11.6 engine_cost_samples=36979 present_wait_mean_ms=1.00 present_wait_max_ms=16.83 present_wait_frames=36979 ui_frames_over_16ms=4 harness_excluded_frames=86`

**Why `performance_pass` is still False (nothing relaxed).** The Metal player does populate `cpuMainThreadPresentWaitTime` (mean 1.00 ms, max 16.83 ms, on every frame), so the subtraction runs, but it does not touch the slow frames. Of the 64 stored `engine_cost_slow_frames` (of 92), 63 are `bound_by=main_thread` and 1 is `gpu` (Recovery, 18.1 ms). On the main-thread frames:
- `cpu_main_ms` is 16.7-18.4 (a few 21-22, one 44.1, one 105.7 in Survey); `present_wait_ms` is about 0 (one frame 0.8), so there is nothing to subtract.
- The render thread is 0.4-4.6 ms and the GPU 1.1-3.1 ms on the same frames: the main thread was blocked or busy while render and GPU idled.
- The 16.7-18.4 ms values still cluster at one 60 Hz tick, as in the earlier diagnosis, but the wait is not booked as Present wait. It could be a drawable/compositor wait that `FrameTimingManager` reports elsewhere, or a periodic main-thread task; this data cannot tell them apart. The frames spread over Briefing, Survey, Disturbance, Hunt, Recovery and Containment, not one phase.
- GPU contention was not the cause: `gpu_slot.sh status` before and after the run showed 0 holders and 0 Unreal processes (system GPU utilisation 23-37 % from non-slot work, load average 6-10).

**Open items (new first, then carried over)**
1. `performance_pass` False: 92 main-thread-bound frames of 36979 at the one-tick signature with Present wait 0. Next step is a diagnostic, not a relaxation: log `cpuTimePresentCalled` / `cpuTimeFrameComplete` deltas (measure their unit) and `Time.unscaledDeltaTime` against `cpuMainThreadFrameTime` for those frames, plus which script phase ran, to split drawable-wait from real main-thread work. The 105.7 and 44.1 ms Survey frames are real one-off hitches worth a look.
2. Three chapel views with the lantern on are too dark: from_guard_door 34.4, from_great_doors 27.1, confessional 25.4 (fc-light2 item 1); next lever `ChapelCandleIntensity` .75 -> .9.
3. S1 with the lantern on: far wall and vault dim but readable at range 15.
4. Hall with the lantern off is still 27-30 % near-black in 4 views (bake ambient).
5. Hall floating candelabras and books are unchanged (design floaters); director's call whether they get holders.
6. The compass dial reads neutral grey, not parchment. The field radio is still the modern mesh.
7. Earlier FC open items (telescope points into the room; kitchen hearth covers a window) still stand. Merges to `main` need the owner.

---

# HANDOFF: piece fc-light2 (lantern pool, chapel candle sconces, compass glass, castle hand lantern). Read this section first.

Branch `haunted-castle` = `hc/fc-light2`, pushed. Builder: Opus 5.5, in the main clone, 2026-10-18. Nothing left running. The GPU slot was released; the one remaining holder is another session's Unreal editor.
- The owner's `hc.castle.runs` (`unity.Bellwether Field Office.Hollow Current`) is still `L,E,L`.
- The test runs rewrote `com.bellwether.hollowcurrent` `hc.castle.runs` from `E,M,B` to `E,M,L`. It was put back to `E,M,B`; backups are in the session scratchpad `fc3/prefs*.backup.plist`.
- `CODEX_HANDOFF.md` is still uncommitted and was not touched.

**Build:** `Builds/macOS/Hollow Current.app` (both maps). Binary 2026-10-18 19:45:41, built from the tree committed as bake commit `8ae142e`.
- Castle bake: 7 maps, 196 probes, 17 reflections.
- Bellwether was re-baked in rounds 1 and 2 (`Investigator.cs` changed). Round 3 changed only castle code, so only the castle was re-baked.
- Commits: code `80fb55b`, round 2 code `8bc5653`, round 3 code `ac78c2b`. Bake commits: `378b23f`, `5453bc9`, `8ae142e`.

**What changed**
1. **Lantern = a pool** (`CastleLightTuning`, castle only).
   - `TorchIntensityScale` 2.4 -> 1.8, `TorchRange` 18 -> 15, `TorchSpotAngle` 100 -> 88. New `TorchInnerFraction` .35 (was .6) and a warmer `TorchColor` (1, .80, .58).
   - No continuous torch flicker exists (only the `lantern_flicker` scare cue), so none was added.
   - Three bake+build+capture rounds:
     - r1 (1.2 / 65 / 13): every room 22-51 % near-black with the lantern on, almost the lantern-off frame.
     - r2 (1.7 / 75 / 15): hall in band, chapel 32-43 %.
     - r3 (1.8 / 88 / 15): final.
   - The response is very non-linear: range and angle each move the frame by 10-20 points.
2. **Chapel candles stand in wall sconces** (`CastleWorld.ChapelCandles`, `ChapelWallSpot`, `LogCandles`; `SurrealProps.MakeWallCandle`; `SurrealAssets.SconceMesh`).
   - The generator keeps its seed and in-cone bias. Each taper moves to the nearest solid spot on a chapel wall at the same height, sliding along the wall past openings: windows, doors, the altar/sunburst block, the confessional, the bell frame and the font.
   - It stands in a procedural iron sconce (wall plate, curled arm, stay, drip pan, socket). The candle keeps its Floater for the candle rule (flames still lean) but no longer bobs, drifts or spins.
   - Count unchanged at 46. About 25 are in the S2 view (was about 37).
   - Prepare log: `HC_CANDLES placed=46 sconced=46 snapped=0 removed=0 floating=0 (chapel tapers; designed hall floaters=23 not judged) PASS`. The prepare throws if floating > 0.
   - `tools/check_castle_kit.py <prepare log>` now also fails on a missing `HC_CANDLES` line or floating > 0 (`--allow-no-candles` for old logs).
   - Not changed: the great hall's floating candelabras and books. DESIGN.md defines them as the surreal floaters ("candles that float", the candle rule), so they are counted, not judged. Director's call whether they also get holders.
3. **Compass glass** (`CastleInstrumentArt`).
   - Cause: held items sit on equipment layer 30, which the torch does not light, so the Standard dial rendered dark grey under a .05 white veil and read as a mirror.
   - Fix: the dial is now drawn unlit at a dim level (`DialLevel` .46). The glass is a faint rim plus one soft streak, with no veil.
   - Result: the dial, its ticks, the needle and the ember pips read in `instruments_on` (compass_idle / reading5).
   - The dial reads pale neutral grey rather than parchment-warm. The tint does not seem to reach it; not investigated further.
4. **Castle hand lantern** (`Investigator.BuildInstruments`, `CastleInstrumentArt.CreateHandLantern`).
   - The left hand in the castle carries the brass cage lantern with amber glass and a warm core, instead of the flashlight mesh. Bellwether is unchanged (`-hc-verify` "flashlight meshes mounted" PASS).

**Exposure, near-black %** (fc2 = build e83d33a; fc3 = this build, 1920x1080 except the layout sets, which open at 2416x1292). Bold = out of band.

| View | off fc2 | off fc3 (35-55) | on fc2 | on fc3 (12-25) |
|---|---|---|---|---|
| S1_great_hall | 30.1 | **30.4** | 7.4 | 15.0 |
| S2_chapel_altar | 41.5 | 41.7 | 9.4 | 21.7 |
| S3_crypt | 36.2 | 36.1 | 7.6 | 16.8 |
| chapel_aisle | 40.9 | 41.5 | 10.5 | 22.9 |
| chapel_altar_approach | 25.6 | **25.8** | 10.1 | 18.8 |
| chapel_confessional | 63.1 | **63.0** | 12.5 | **25.4** |
| chapel_font_corner | 49.5 | 49.3 | 5.6 | 15.4 |
| chapel_from_great_doors | 48.3 | 48.5 | 12.4 | **27.1** |
| chapel_from_guard_door | 59.2 | **59.5** | 15.1 | **34.4** |
| hall_from_doors | 30.2 | **30.5** | 7.3 | 14.6 |
| hall_pier_gap | 35.4 | 35.6 | 7.5 | 16.2 |
| hall_skeleton_head | 29.2 | **29.5** | 9.5 | 19.0 |
| hall_skeleton_side_east | 27.5 | **27.5** | 6.5 | 13.2 |
| hall_skeleton_side_west | 48.1 | 48.3 | 10.5 | 24.4 |
| hall_skeleton_tail | 36.0 | 36.1 | 6.5 | 13.8 |

- Lantern on: 12 of 15 views in band (fc2: 3).
- Lantern off moved at most +0.6 (the sconced candles carry no light). The off misses are the pre-existing ones (hall bake/mist, chapel doors), not touched by brief.
- Frame time (`capture.json`, 90 fps cap): S1 on mean 11.11 ms, max 11.39; hunt_dive mean 11.17, max 21.3. fc2: 11.11 / 11.33, hunt_dive 11.19-11.20 / 22.3.

**Static checks (HEAD):**
- check_source PASS; check_castle_scares PASS; check_audio_keys OK; check_throwables_scene rc 0; check_castle_doors OK.
- check_castle_kit OK on `.castle-bake-fc3-r3.log`.
- Prepare log: `HC_KIT_AXIS_SUMMARY problems=0`, `HC_DOOR_LEAF_SUMMARY problems=0`, `HC_HIDE_SPOTS_SUMMARY failed=[]`, `HC_SKELETON PASS`, `HC_CHAPEL PASS`.
- Bellwether `-hc-verify`: `HC_NATIVE_ROUTE_PASS`, 0 `HC_VERIFY FAIL`. `HC_PERF pass=False engine_cost_over_16ms=83`, the known open item.

**Evidence** (local, gitignored): `docs/haunted-castle/rounds/r13/captures/fc/fc3/r3/{rooms,layout}_{off,on}`, `instruments_on`, `bwverify`. Rounds 1 and 2 keep their rooms sets only. Logs are `.playtest-fc3-r*-*.log` and `.castle-bake-fc3-r*.log`.

**Open items**
1. Three chapel views with the lantern on are too dark: from_guard_door 34.4, from_great_doors 27.1, confessional 25.4. These are wide door-side views whose frame edges fall outside even an 88 degree cone.
   - A wider cone floods the hall again; the rounds budget is used up.
   - Next lever: a little more chapel-only fill (`ChapelCandleIntensity` .75 -> .9), not the lantern.
2. S1 with the lantern on: the far wall and vault are dim but still read at range 15. They do not fully fall to black; range 13 made the hall 28 %.
3. Hall with the lantern off is still 27-30 % in 4 views (fc open item 2, bake ambient). Not touched, per brief.
4. Hall floating candelabras and books are unchanged (design floaters; see item 2 above).
5. The compass dial reads neutral grey, not parchment.
6. The field radio is still the modern mesh.
7. Earlier FC open items 3-7 still stand. Merges to `main` need the owner.

---


# HANDOFF: wave FC (post-feedback open items) integrated, built and proved (read this section first)

Branch `haunted-castle` = `hc/integ-fc`, pushed. Integrator: Opus 5.5 (piece fc-integ), 2026-10-18. Nothing left running, GPU slot released, the owner's `hc.castle.runs` is unchanged (`L,E,L`, backed up before and checked after), `.wt/wallart-art` / `.wt/codex-*` (Codex) and the uncommitted `CODEX_HANDOFF.md` edit untouched.

**Merged on `hc/integ-fc` (from `haunted-castle` `0bd9db5`):** hc/fc-bw60 `f3a39b2` (Bellwether `performance_pass` on FrameTimingManager engine cost), hc/fc-light `ebc2b07` (hall mist .5 / alpha .12, castle lantern ForcePixel 100 deg), hc/fc-fbx `17cd51e` (door slabs thinned, collider = slab, portrait-door canvas, 45 old-kit pieces turned via `CastleKitAxis`), hc/fc-props `f0baa9c` (brazier moved, period held instruments, ASSET_LOG retired rows). One conflict: `CaptureMode.cs` header comment (the `kit` and `instruments` capture sets merged cleanly, both kept). Handoffs: `docs/haunted-castle/handoffs/fc-*.md`.

**Integrator changes:**
- `CastleKitAxis.TurnedModel` bug (`67af5e8`): `Transform.Find("FBX / <art>")` splits at the slash, so the prepare log said `turned=0 problems=22` although the scene held all 45 turned pieces (scene-mode `check_castle_kit.py`: turned=45 problems=0). Now compares child names and tests the rotation. Re-baked: `HC_KIT_AXIS_SUMMARY turned=45 problems=0`.
- fc-light one-round fallback (`e5081a2`, the two matching rows of the fc-light table): `HallWindowMoonlight` .48 -> .30 (S1 off was 30.3 % < 31) and `TorchRange` 24 -> 18 (every hall view on was 6-10 % < 12). Measured effect: none on lantern-off (S1 30.3 -> 30.1), lantern-on mean luma 66 -> 56 but near-black unchanged (7-10 %). No further tuning (brief: one round).

**Build:** `Builds/macOS/Hollow Current.app` (both maps), binary 2026-10-18 18:36:11, after the bake commit `65a5f58` (18:35:56). Bakes: Castle 7 maps / 196 probes / 17 reflections, Bellwether 4 / 124 / 9 (Bellwether re-baked once after the merges; the later commits are castle-only).

**Static checks (HEAD):** check_source PASS (1199 assertions), check_castle_scares PASS, check_audio_keys OK, check_audio_quality OK, check_throwables_scene OK, check_castle_doors OK (collider = slab), check_castle_kit OK (scene and `.castle-bake-fc.log`), bellwether_fuzz identical, `audio_castle_sfx.py --check` 0 MISSING. Prepare log: 23 `HC_DOOR_LEAF_THICKNESS fitted=True`, `HC_DOOR_LEAF_SUMMARY problems=0` (both stages), `HC_PORTRAIT_CANVAS flipped=True now=+z`, no `HC_KIT_AXIS_IMPORT`, `HC_HIDE_SPOTS_SUMMARY failed=[]`, `HC_SKELETON PASS`, `HC_HALL_AXIS PASS`, `HC_CHAPEL PASS`, `HC_CHAPEL_PROPS overlap=False gap=0.52 PASS`; runtime `HC_INSTRUMENTS compass=1277 trumpet=1026 period=True lantern=1150`.

**Proofs** (logs `.playtest-fc-*.log` in the clone; frames `docs/haunted-castle/rounds/r13/captures/fc/fc2/` final build, `fc1/` first build before the fallback; local, gitignored):

| Proof | Result |
|---|---|
| fc-light first check | PASS: `HC_CAPTURE lantern on ... angle=100 mode=ForcePixel`; in-cone reach 92-97 % on S1-S3 and every layout view (fb2: 4-37 %). The pixel-light diagnosis was right |
| exposure lantern off (35-55) | S1 30.1 (fb2 26.8), S2 41.5, S3 36.2. Hall views: from_doors 30.2, skeleton_head 29.2, side_east 27.5 (FAIL); pier_gap 35.4, side_west 48.1, tail 36.0 (in band; fb2 21-32). Hall darker than fb2 by 3-16 points, 3 of 6 hall views in band. Chapel unchanged (altar_approach 25.6 too bright, confessional 63.1 / guard door 59.2 too dark) |
| exposure lantern on (12-25) | FAIL the other way: S1 7.4, S2 9.4 (fb2 40.0), S3 7.6 (fb2 28.9); 3 of 12 layout views in band (confessional 12.5, chapel doors 12.4 / 15.1). Chapel and crypt are much better lit than before; the hall is floodlit to the far wall and vault (mean luma 54-77). Skeleton and table read clearly |
| frame time | S1 mean 11.11 ms max 11.33, hunt_dive mean 11.19-11.20 max 22.3 (fb2 11.15 / 21.8): no ForcePixel cost at the 90 fps cap. The `full` capture had one 189 ms hunt_dive hitch while another session's Unreal capture ran |
| doors | PASS: closed leaves fill the opening (not see-through); open from inside both leaves swing into the hall (openAngle -155/+155) |
| kit (3 views, lantern on/off) | fireplace front (mantel, recessed firebox) faces the dining room between the lancets; chairs face the table; portrait door now shows its painting, two south-wall frames; `kit_door_edge` the hall leaf a thin slab inside the reveal, rings a few cm proud; log `leafScaleZ=0.833 panelZ=0.100` / portrait `0.700 / 0.070 canvasFacing=(-1,0,0)`. S1: throne faces the hall, tapestries show their emblems |
| layout / chapel | brazier stands right of the altar steps, apart from the bell (S2, chapel views) |
| instruments (4 held views, on/off) | lantern: brass cage with violet chimney, reads period, glove holds the foot. Trumpet: flared horn, held by the narrow end, readable in the dark. Compass: brass case with bail ring and pips; the glass glare hides the dial and needle, so it reads more like a hand mirror (tune `GlareTexture`). Left-hand flashlight still the modern mesh |
| cold | PASS: `HC_ONBOARD_START equipped=True`, first cue 18.5 s, 5 cues, Early band, Warning -> hunt 84 s, hides 5/5, runtime hides 8/8, blockers 0 |
| natural (history not cleared) | WON: band Mid, first hunt 145 s, share 0.197, longest gap 134 s, 2 hunts, 3 `spoke`, `HC_VOICE_SUMMARY dropped=0`, 0 "cannot be sampled", 0 exceptions; grabContinuity 0/3 (known r13 metric) |
| escape | WON, blockers 0, 4 `HC_DOC_OPEN` (letter + 3 ledger pages), 0 exceptions |
| Bellwether `-hc-verify` | Route PASS (`HC_NATIVE_ROUTE_PASS`). `HC_PERF engine_cost_source=FrameTimingManager`, then `pass=False reason=engine_cost_over_16ms=104(max 84.3 ms)`, wall p99 11.9 ms, 1 % low 63.6 fps, 86 harness frames excluded. Of the 64 logged slow frames, 56 have main-thread 16.7-17.5 ms (1.5 x the 11.1 ms frame), GPU <= 19 ms, spread over every phase: `cpuMainThreadFrameTime` on Metal appears to include the frame limiter's wait (fc-bw60's stated risk). Strict rule kept, not relaxed |

**Open items:**
1. Lantern now too strong: with ForcePixel working, the integ-fb compensation (castle `TorchIntensityScale` 2.4, range 24 -> now 18) floods the hall. Next round: lower `CastleLightTuning.TorchIntensityScale` toward fb's 1.35 (and/or `TorchRange` 12) and recapture with `tools/exposure_report.py` / `tools/hall_light_model.py reach`.
2. Hall lantern-off still 27-30 % in 3 views: neither the mist (halved) nor the hall window moonlight (.30, no measurable change) is the remaining floor light. Candidates: the castle bake ambient / reflection probes and the emissive rune ring. Needs a per-light diagnosis, not another blind cut.
3. Bellwether `performance_pass` False on engine cost: check what `FrameTimingManager.cpuMainThreadFrameTime` includes on Metal (limiter wait) before using it as the 60-fps gate; the 4 UI note-open frames are still counted.
4. Compass glass glare hides the dial; flashlight and field radio are still the modern meshes.
5. Telescope still points into the observatory; kitchen hearth still covers the west lancet (fc-fbx notes).
6. Art capture sets (`-hc-capture-art`) ignore `-hc-capture-size` and open at the largest window that fits (2416x1292 here, 3440x1440 display); rooms sets are 1920x1080.
7. From FB: human audio listen, doc-UI sign-off, r13 critic metrics (apparition CV, grab continuity), the r13 blind critic has not run on this build. Merges to `main` need the owner.

---


# HANDOFF: wave FB (owner feedback 2026-10-15) integrated, built and proved (read this section first)

Branch `haunted-castle` = `hc/integ-fb`, pushed. Integrator: Opus 5.5 (piece fb-integ). Owner feedback: `docs/haunted-castle/rounds/r13/OWNER_FEEDBACK.md` (items 1-13). Nothing left running, GPU slot released, `.wt/wallart-art` (Codex) and the uncommitted `CODEX_HANDOFF.md` edit untouched.

**Merged on `hc/integ-fb` (from `hc/owner-feedback-1` `d0903fa`):** hc/fb-onboard `019699d` (items 6, 7, 12), hc/fb-doors `9aa6235` (3, 4, 5), hc/fb-audio `76265d3` (1, 2, 11), hc/fb-layout `1598b76` (8, 9, 12 lighting), hc/fb-docui `986349e` (10), hc/fb-voice2 `309d348` (13). Conflicts: `CaptureMode.cs` (doors + layout capture sets, both kept), `ASSET_LOG.md` (both sections kept). Handoffs: `docs/haunted-castle/handoffs/fb-*.md`.

**Integrator changes:**
- Skeleton vs great doors: at z 18.4 / length .5 the skull sat 1.04 m from the east great-door hinge, so that leaf could not swing into the hall and opened toward the player. Now `SkeletonPos` z 18.72, `SkeletonScaleVec` (.6, .6, .45): skeleton z 15.97-21.42, table gap 0.63, `columns_hit=[]`; both hall leaves now open 155 deg into the hall from the courtyard.
- One lighting round (`CastleLightTuning`/`CastleAtmosphere`): candles .53 -> .40, rose moon .66 -> .50, vault fill .10 -> .05, crypt cold .42 -> .32, ambient .75 -> .65, chapel candles .6 -> .75; castle lantern scale 1.35 -> 2.4, range 24, cone 80 deg (new `TorchSpotAngle`, castle only).
- fb-voice2 follow-up: deleted the nine retired `voice_cc_*` clips (hunt_hungry, dispatch, night, kit, ending, contain, hint_signal/trace/voice) and their 8 remaining `voice-lines.json` entries.
- Doors capture inside views moved east of the skeleton.

**Build:** `Builds/macOS/Hollow Current.app` (both maps, windowed 1920x1080), binary 2026-10-15 20:58:57 from the tree committed as `627f588` (bake commit). Bakes: Castle 7 maps / 197 probes / 17 reflections, Bellwether 4 / 124 / 9 (Bellwether had to be re-baked: its receipt hashes every non-castle `.cs`, so Interface/World/Game/Investigator edits stale it; any `.cs` change needs both re-bakes before `BuildPreparedMacAll`).

**Static checks (on `627f588`):** check_source PASS (1191 assertions), check_castle_scares PASS, check_audio_keys OK, check_audio_quality OK (not strict), check_throwables_scene OK, bellwether_fuzz identical, check_castle_doors OK. Prepare log: 23 `HC_DOOR_LEAF_AXIS turned=True`, `HC_DOOR_LEAF_SUMMARY problems=0` (both stages), `HC_HIDE_SPOTS_SUMMARY failed=[]` (prepare, validate, runtime), `HC_SKELETON columns_hit=[] PASS`, `HC_HALL_AXIS PASS`, `HC_CHAPEL aisle=2.35 passages 2.43/2.57 PASS`; runtime `HC_DOC_UI fonts_complete=True paper=True`.

**Proofs on the final build** (logs `.playtest-fb-*.log` in the clone, frames `docs/haunted-castle/rounds/r13/captures/fb2/`, local; first-build runs in `fb/`):

| Proof | Result |
|---|---|
| cold 1 | PASS: no Briefing after 1 s, 6 HC_WATCHED in 180 s, first 7.5 s, Warning->hunt 74 s (Early), 4 HC_OBJECTIVE, hides 5/5 hidden, runtime hides 8/8, blockers 0 |
| cold 2 | PASS: 6 cues, first 6.7 s, hunt 103 s after a Warning at ~92 s, hides 5/5, blockers 0 |
| natural (cleared `hc.castle.runs`) | WON: band Early, first hunt 91 s, share 0.208, longest gap 233 s, 3 hunts, 1 mode 12 grab (no loop), no "cannot be sampled", 0 exceptions; `HC_VOICE_SUMMARY lines=2 spatial=2 dropped=0`, 1 `HC_VOICE_GATE spoke`, 0 `cc_hunt_hungry`, 0 dispatch. Onboard: firstCue 17.7, 7 cues, 2 before the first hunt. Only 2 apparitions, grabContinuity 0/3 (r13 critic metrics, not FB) |
| natural (history not cleared) | WON: first hunt 77 s, share 0.276, gap 187 s, 4 hunts / 4 `spoke` (one per hunt), dropped 0, no cc_hunt_hungry |
| doors capture | PASS: closed leaves fill the opening from the courtyard; opened from the courtyard both swing into the hall (openAngle -155/+155). From inside they also open into the hall (hinged on the inner face) and pass through the player |
| layout capture | skeleton centred on the axis, clear of every pier, skull ~1.7 m inside the doors; chapel aisle and side passages open, font in the SE corner |
| exposure (lantern off / on, near-black %) | S1 26.8 / 19.4, S2 41.3 / 40.0, S3 35.1 / 28.9 (round 1 was 24.7/21.9, 46.9/46.7, 32.5/30.4). Off in band: S2, S3; hall views 21-32 % off (FAIL, too bright); on in band: S1 and 4 hall views; chapel views 25-55 % on (too dark) |
| docui | letter, 3 ledger pages, S8 journal ledger, Bellwether child's drawing: readable, no duplicated heading, text sits on (not through) the ruled lines; no offset change made |
| escape | WON, blockers 0, 4 `HC_DOC_OPEN`, dropped 0 |
| Bellwether `-hc-verify` | PASS (`HC_NATIVE_ROUTE_PASS`, 0 frames over 33 ms, p99 11.8 ms; `performance_pass` False = the known 60-fps floor flag). `-hc-ui-only` UI PASS (that mode waits for human input at the end; the runner's SIGTERM closed it) |

**Open items:**
1. Hall still too bright with the lantern off (21-32 % near-black against 35-55 %). Realtime cuts barely moved it (candles -25 % gave +2 points): the hall is lit by the bake (HouseLighting castle bake ambient x0.68, window moonlight). Next: lower the castle bake ambient and `WindowMoonlightFloor`, re-bake, recapture. Chapel is the opposite with the lantern on (narrow-room views 40-55 %).
2. Audio: human listen, owner pending (Kokoro `bm_george` voice and stone steps). Voice audition set: `docs/haunted-castle/audition/voice_<voice>_cc_hunt_hungry.wav` (the line itself is now retired; the files compare voices only). Re-render: `tools/generate_castle_voice.py --voice <v>`.
3. Doc UI mocks for owner sign-off: `docs/haunted-castle/rounds/r13/docui_mock_*.png`; in-engine captures in `fb2/escape/` and `fb2/s8/`.
4. Cold driver: one `cold_unstick` per run at (1.7, 14.6), the corner behind the open east hall leaf (its nav corner hugs the jamb). A steering player sees the leaf; not a blocker.
5. `ASSET_LOG.md` rows still list the nine retired voice clips; `tools/audio_castle_sfx.py --check` reports them missing (harmless).
6. r13 critic metrics still open (apparition CV, grab continuity), from the previous section's "Next session" list. The r13 blind critic (`rounds/r13/CRITIC_BRIEF.md`) has not run on this build.
7. Merges to `main` need the owner.

---


# HANDOFF: Haunted Castle loop STOPPED by owner 2026-10-15 (read this section first)

Branch `haunted-castle` = `hc/integ-l3` in `/Users/midir/hollow-current-haunted-castle`, pushed. Director: Fable 5.1; integrator Opus 5.5 (stopped mid proof run on owner's order, nothing left running, GPU slot free, merged worktrees removed, `.wt/wallart-art` is Codex's and untouched).

**State at stop**
- Last commit `e80fa28` (re-bake Castle + Bellwether after wave L3). Build `Builds/macOS/Hollow Current.app` is from that commit (binary 00:30:21, commit 00:30:07), windowed 1920x1080, both maps.
- Merged since r12 (5.0/10, `docs/haunted-castle/rounds/r12/CRITIC.md`): wave L (hc/rules-l, audio-l, scares-l, count-l2 [= count-l + grabs-l], exterior-l, hall-l [= props-l]), L2 (rules-l2, count-l3), L3 (rules-l3 `f58d848`, count-l4 `44b8b6c`) plus integrator work (torch-failure 50 %, dispatch card/ledger layout, `-hc-hold-dread` siege, HC_PLAY_STATS with the critic's metrics, drawbridge un-batched, Return-grab no-stall, run-plan persistence `hc.castle.runs`). Every piece handoff is in `docs/haunted-castle/handoffs/`.
- Static checks on `e80fa28`: check_source PASS (1132 assertions), check_castle_scares PASS, check_audio_keys OK, check_throwables_scene OK (drawbridge parts not batched), bellwether_fuzz identical (per F1/F2 on their branches; rerun to confirm on HEAD).
- Proofs on this build were INTERRUPTED. Completed: natural1 (`HC_PLAY_STATS`: share 0.236, longest gap 260 s, first hunt 130 s, 0 of 2 hunts within 10 s of an apparition, only 2 apparitions, grabContinuity 0/2 = FAIL), fear (share 0.327, hidden survival 50 %). natural2, natural3, siege50 logs exist but were cut before their summary. `HC_RUN_PLAN` bands rotated Late/Early/Mid across the three naturals (anti-repeat works). count-l4's `root=matched skullDelta=0.00 fromLastDiveSample=0.52–0.58` shows the commit seat is continuous but the continuity window (0.5 m) still fails by a few cm; the mode 8 clip's own fall is what the window must tolerate (see `rounds/r13/CRITIC_BRIEF.md` for the continuity definition). The learned-hide bag logged 0 opens in the cut siege50 (check whether the run reached 3 visits). The previous full proof table (L2 build f432fe5) is "Round 9" in `docs/haunted-castle/playtest/AUTO_RUN.md`; this build's partial logs are `.playtest-*.log` in the clone (gitignored).

**Next session, in order**
1. Rerun the reduced proof set on the current build: natural x3 back-to-back, siege50 (`-hc-playtest <dir> siege -hc-hold-dread 60`, long enough for 6 hidden hunts), huntcam, fear, verify. Confirm: apparition CV pooled over 3 runs >= 0.5, first-hunt spread >= 45 s, share 15–30 %, gap >= 120 s in every run, hidden survival <= 85 % with `bag=` opens, drop-grab continuity, no mode 12 re-commit loop, no "Non-Legacy animations cannot be sampled" warning.
2. If the table passes, spawn the r13 blind critic with `docs/haunted-castle/rounds/r13/CRITIC_BRIEF.md` (fill in the build hash). If not, fix (F1 rules: Core; F2 count: Presence/CountClaudeProfile) and rebuild (no bake needed for code-only changes).
3. Known open items not in any brief: the modern-looking instrument meshes, a human listening pass on all audio (ASSET_LOG "Human listen: not yet"), the Bellwether 60-fps floor flag, Codex's wall-art portraits (`CODEX_HANDOFF.md`, 7 textures still missing).
4. Merges to `main` need the owner.

---

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

## Wave L2 (round 9) on `hc/integ-l2`

- Merged rules-l2 and count-l3; `haunted-castle` fast-forwarded. Results: `docs/haunted-castle/playtest/AUTO_RUN.md`, "Round 9".
- Fixed: drawbridge leaf static-batched (never lowered on screen); driver bridge stall (`HC_RETURN_KILL`).
- Open: pooled apparition CV 0.44 over 10 gaps; first-hunt span 30 s; natural2 longest gap 111 s; siege-held sample had 0 opens; mode 8 drop still steps ~2.2 m in the first 0.15 s; mode 12 commit/release loop.

## Wave L (round 8) on `hc/integ-l`

- All nine wave L branches merged on `hc/integ-l` (order: rules-l, audio-l, scares-l, count-l / count-l2, exterior-l, props-l, hall-l); `haunted-castle` fast-forwarded to it. Build: `Builds/macOS/Hollow Current.app` from `hc/integ-l` (Castle and Bellwether baked; `CountClaudeImport.Prepare` run).
- Proof table and critic pass-test numbers: `docs/haunted-castle/playtest/AUTO_RUN.md`, "Round 8".
- Open: pooled apparition CV 0.43 (< 0.5); every mode 8 drop lifts him about 3 m in the first 0.16 s (eyeToSkull step fails); `claw_px_proj` is always 0 (hand-vertex projection finds nothing in frame); `gate_out_S6` planks are part of GateArch or ExtGate; driver bridge blocker from the 2.3 m Return grab (invulnerable driver only).
- New driver modes: `-hc-playtest <dir> siege -hc-hold-dread 60` (hide contract); every run prints `HC_PLAY_STATS`; grab frames list their measures in `<dir>/grabcam.txt`.
- Never run `tools/audio_castle_sfx.py` with no arguments (F4: it regenerates the wave K repaired clips without tail trims).

## Round 7b (build `e57a00d`): hc/count17 + hc/art-exterior2 merged

- Proof table: `docs/haunted-castle/playtest/AUTO_RUN.md`, "Round 7b"; pre-merge table on `ac43efa` under "Round 7".
- Open: natural2 share 0.224 and no between-hunt gap ≥ 120 s; watched-hide grab path label is `dive`, and at t 1.00 he is not visible; HUD prompt shows during grabs; `gate_out_S6` faces the closed gate doors; verify floor flag still False.
- Crypt "hitch" was the screenshot encode frame (capture artefact).
- Not built yet (r12 critic holds the engine): `Interface.cs` hides every castle HUD line and prompt during a grab (`HC_UI grab_hidden=true`), captions stay. Interface.cs is shared, so the next build needs both the Castle and Bellwether re-bakes, then `-hc-verify`.
- hc/count18 `b5113b9` merged (code only, not built): live skinned bounds (`updateWhenOffscreen`), door-grab floor/door clamp, `visible=` frustum+occlusion, `path=hideopen`. After the next build: fear run (watched hide) and the great-hall frame time (skinned bounds cost).

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
