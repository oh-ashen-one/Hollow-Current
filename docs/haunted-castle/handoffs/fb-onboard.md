# Handoff: fb-onboard (owner feedback items 6, 7 and 12)

Branch `hc/fb-onboard`, worktree `.wt/onboard`. Base `d0903fa`. Model: Opus 5.5. Date 2026-10-15. This work was not built or run (LOOP rule 3: builders do not launch Unity). Everything below was checked statically. The integrator's build and runs are the proof.

## Root cause (confirmed in source)

The castle case starts in `Phase.Briefing`. It leaves Briefing only through `Investigation.EnterHouse()`, and that call needs `Equipped`. `Equipped` is set only by E on the gatehouse "Equipment rack" (`InteractKind.Kit`, `CastleWorld.Gatehouse`). The HUD told the player to use the "lantern cart", but the rack is a separate box 1.9 m away from the cart art.

While the case is in Briefing:
- `Investigator.Hide` returns silently, which explains items 6 and 7.
- `ScareDirector.Update` and `CastleScares.Active` stay off, and `Investigation.Tick` returns before it runs any clock. No hunt can start, which explains item 12.

Every scripted proof took the kit in its first second, so no automated run ever saw this state.

A second silent-E bug: `Game.Notice` (for example "The door is held.") wrote to `Subtitle`, and the HUD drops that text through `FieldMessages.ShowCard`. Notices were therefore never drawn.

## What changed

| File | Change | Item |
|---|---|---|
| `Runtime/Game.cs` | Castle: `Case.Equip(); Case.EnterHouse();` in Awake, before `Message`/`Changed` are wired, so no kit line or voice plays. The case is in Survey from frame 0. This is skipped in capture mode, so its stills stay calm. `Notice` now sets `NoticeText`/`NoticeTime` and logs `HC_NOTICE`. The cold driver mode starts with no run history and writes none. The castle adds `CastleOnboarding`. | 6, 7, 12 |
| `Runtime/Interactable.cs` | On the castle, E on the rack gives "The rack is empty. You already carry the instruments." Bellwether is unchanged. New appended enum value `InteractKind.Nailed`: E gives "Nailed shut. Find another hiding place." plus a rattle. | 7 |
| `Runtime/Investigator.cs` | The focus ray is factored into `static FocusRay(...)` (same logic) so the audit tests exactly what E does. Hide in Briefing gives a notice on the castle; Bellwether keeps its silent refusal. Logs `HC_PLAYER_HID`. | 6, 7 |
| `Runtime/Castle/CastleWorld.cs` | `Hide()` calls `CastleOnboarding.ProtrudeHideFocus`. Long Night `Nail()` leaves a focus-only `Nailed` trigger over the door, removed by `UnnailHides`. | 7 |
| `Runtime/Castle/CastleOnboarding.cs` (new) | The watched layer, the objective log and idle nudge, the hide audit and fix, and `HC_ONBOARD` (details below). | 6, 7, 12 |
| `Core/CastleObjective.cs` (new, pure, tested) | The castle objective chain and nudges. | 12 |
| `Runtime/Interface.cs` | Castle `HudGoal()` = `CastleObjective` (cached once per frame). The kicker reads OBJECTIVE UPDATED for 5 s after a step change. A tracker line shows `RECORDS n/3 · LEDGER n/3 · RELICS n/3 · HUNT SURVIVED`. The notice line sits under the interaction prompt. The `Note()` screen was not touched. | 7, 12 |
| `Core/HuntRules.cs`, `Core/Investigation.cs` | New `FirstHuntFromSpawn` (castle and Long Night true, Bellwether false). The first-hunt guarantee (`TickFirstHunt`) counts from spawn in Survey and Disturbance, not from the 2nd record, so a player who records nothing still meets the Count inside the plan's band. New diagnostics: `FirstHuntDeadline`, `FirstHuntClock`. The ScareRhythm clock is not rewritten. | 12 |
| `Core/ScareRhythm.cs` | `RunHistory.Next` with no history returns the Early band (60-100 s). The anti-repeat rule is unchanged otherwise. | 12 |
| `Core/CaseBook.cs` | Castle `Arrival` card: "You carry the instruments. Read the gate keeper's letter on the table, then find the Blood Ledger." Arrival still voices `cc_night`. | 12 |
| `Runtime/Castle/CastleScares.cs` | `FigureTexture` is `internal` (one line), used by the doorway figure. | 12 |
| `Runtime/Castle/PlaytestDriver.cs` | New mode `cold`. `HC_PLAY_STATS` gains `onboard: ...`. | 12 |
| `Editor/NativeBuild.cs` | `HC_HIDE_SPOTS` at castle prepare (`source=prepare`) and in `ValidateCastleWorld` (`source=validate`). It warns and does not fail. | 7 |
| `Tests/RulesTests.cs` | Mechanism tests of the dice, the floor and the record-keyed guarantee use `RecordKeyed(HuntRules.Castle)`. Added: from-spawn bands for every band and roll, records do not restart the clock, an early hunt ends the clock, no clock in Briefing, no history gives Early, and `OnboardTests` for the objective chain. | 12 |
| `tools/check_castle_scares.py` | Lints the watched layer. Every kind has a budget (max >= 1, gap >= 60 s) and checks `Can`/`Fire`. It also checks the cadence constants (first cue <= 25 s, gaps 20-45 s), `tell-safe:` on phase reads, and that no rule input exists (no `AddDread` and the like). | 12 |

### The watched layer (`CastleOnboarding`)

- **Cadence:** the first cue comes 6-20 s after spawn, measured in case seconds, which stop in menus. After that a cue comes every 20-45 s until the first apparition (`Case.Apparitions > 0`), then every 30-50 s, for the run's first 300 s.
- **Deferral:** a cue waits while the player is hidden (the hidden layer has its own clock), in a modal, or during a grab presentation.
- **Kinds and budgets** (max uses, gap in s): `door_slam` (4, 60), `footsteps_behind` (3, 90), `ear_breath` (3, 60), `ear_whisper` (2, 90), `portrait_eyes` (3, 60), `doorway_figure` (3, 60), `lantern_flicker` (3, 60), `candle_gutter` (2, 90).
- **What each kind does:**
  - `door_slam`: `door`, then `latch` + `knock` at a door 7-16 m away, preferring one out of sight.
  - `footsteps_behind`: `CastleScares.FootstepsBehind(decoy)`.
  - `ear_breath`: `castle_hide_breath_0/1` 0.45 m behind the head.
  - `ear_whisper`: `castle_hide_whisper`.
  - `portrait_eyes`: the pupils of a `PortraitEyes` within 2.5-12 m snap to her for 6-9 s, with a `creak_n`.
  - `doorway_figure`: the gallery-shape figure in an open doorway 6-18 m away, 18-60 degrees off centre. It is gone when looked at (within 10 degrees) or after 7 s. It is withheld in Hunt (`tell-safe`), and the draw falls through to another kind.
  - `lantern_flicker`: three short `Scares.Blackout` drop-outs, not logged as `HC_TORCH_FAIL`.
  - `candle_gutter`: `CastleScares.CandleGutter`.
- **Rules:** never the same kind twice in a row. The first cue is drawn from door, steps, breath or figure, and falls back to the ear breath, which always works. No cue touches Dread or the rules. Only AUDIO.md ids or the existing `Resources/Audio` ids are used (`check_audio_keys` OK).
- **Logs:** `HC_WATCHED_CLOCK first_at=`, `HC_WATCHED kind=<k> t=<s> n= budget= apparitions= hunts= ...`, `HC_WATCHED_GONE`, `HC_WATCHED_SKIP`.

### Objective chain (`CastleObjective`)

1. Letter: "Read the gate keeper's letter on the gatehouse table [E]."
2. Ledger: "Find the Blood Ledger pages (n/3): <rooms still unread>". The rooms are the dining hall (west wing), the portrait hall (upstairs, east wing) and the chapel (east of the courtyard).
3. Then, in order of what applies:
   - Reconstruct (records 3/3 + hunt survived) → `GoalReconstruct`.
   - Fight (sword): ring the bell.
   - Forge (3 relics): at the altar.
   - Relics: "(k/3). Next: <place>".
4. Warning, Containment (bell), Return (winch) and the endings use `Investigation.Goal`. Hunt and Recovery keep the calm goal (critic r10 #4).

The tracker line shows the other route. The idle nudge fires after 60 s on one step in a calm phase, then every 90 s, at most 3 per step (`HC_NUDGE`, `Game.Notice` for 7 s). `HC_OBJECTIVE t= step= from= text= tracker=` is logged on every change.

### Hide spots

`ProtrudeHideFocus` extends each hide's focus trigger 0.20 m into the room. Its back face stays at the door. It is idempotent, applied at prepare time and again at runtime for the scene baked before this change. The ray used to tie with the solid lower door and was settled only by the 12 cm rule.

`AuditHides` logs one line per hide:

`HC_HIDE_SPOTS source=runtime|prepare|validate name=".." kind= center= size= face= stand= standClear= first=<collider>@<d> focusCentre= focusEye= focusLow= pass=`

`pass` means the ray hits this spot when aimed at the collider centre and at the door at 1.45 m height. The aim starts 1.2 m in front of the door at eye height, using `Investigator.FocusRay`.

After the per-hide lines comes `HC_HIDE_SPOTS_SUMMARY audited= focusable= failed=[..]`. I could not run it, so the five named hides have not been checked against the real scene. That is integrator check 2.

## Verification done (static)

- `check_source.py`: PASS. 1168 assertions (was 1132), runtime and editor compile. Only the known CS0649 warnings remain, plus an existing CS0162 in CastleScares.cs:216.
- `check_castle_scares.py`: PASS, including the new watched-layer lint. I checked that it catches a cadence over 25 s and a missing `tell-safe`.
- `check_audio_keys.py`: OK.
- `bellwether_fuzz.py`: identical over 4000 seeds.
- `check_throwables_scene.py`: exit 0 (scene untouched).
- Session statistics after the from-spawn change, from the RulesTests 2000-run models:
  - WAVE_L3 critic route: first hunt p10/p50/p90 = 79/137/195 s, 3-run spread >= 45 s in 1.000 of windows, pooled apparition CV >= 0.5 in 0.910 of sets, share mean 0.187.
  - The WAVE_K/WAVE_L asserts all still pass.

## Integrator: build and runs

1. **Build.** Merge, then rebake and build as usual (any `.cs` change stales the receipts). `CastleWorld.Hide` changed, so the castle re-prepare bakes the proud triggers into the scene. Look for `HC_HIDE_SPOTS source=prepare` and `HC_HIDE_SPOTS_SUMMARY ... failed=[]` in the prepare log.
2. **Cold run, twice:**

   ```
   ~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-f0 -- open -g -W -n -a "<app>" --args -logFile <log> -hc-playtest <dir> cold
   ```

   It takes about 4 minutes and never touches the rack. Pass when all of these hold:
   - `HC_PLAY_COLD ... pass=True` and `HC_PLAY_RESULT ... blockers=0`.
   - `HC_ONBOARD_START equipped=True phase=Survey`.
   - No `HC_ONBOARD_BRIEFING_STUCK`.
   - `HC_WATCHED` count >= 4, the first at `t <= 25`.
   - `HC_RUN_PLAN band=Early`, and the first threat (`HC_WARNING`, or a no-tell `HC_HUNT_START`) at `t` 60-100. A told hunt's `HC_HUNT_START` comes 5-14 s after its Warning.
   - `HC_OBJECTIVE ... step=ledger from=letter` within the first ~15 s.
   - Five `HC_PLAY_COLD_HIDE` lines with `hidden=True` (or a notice), and `focus=` naming the spot.
   - `HC_HIDE_SPOTS_SUMMARY source=runtime ... failed=[]`.
   - Screenshots `cold_hidden_*` show the inside view.
3. **Naturals:** in `natural` x3, `HC_PLAY_STATS ... onboard: firstCue= cues= cuesBeforeFirstHunt= firstObjectiveChange= ...` must show firstCue <= 25 and cuesBeforeFirstHunt >= 1 (typically 2-3; an Early band can start the threat at 60 s). The first hunt counts from spawn now (the naturals record two items within ~10 s, so their bands barely move).
4. **Owner-style check (windowed, by hand if allowed):** start the castle, do not touch anything for 30 s, then press E on the Hall armoire. You should hear or see at least one cue, the HUD objective should name the letter, and E should hide you.

## Risks and notes

- **First-hunt timing:** a hunt now comes 60-200 s from spawn (Early 60-100 on a first run) even with no records. This is intended (item 12). The critic's first-hunt spread is measured from spawn now, not from the 2nd record.
- **Cue density:** the director's dread ticks (12-25 s) already ran in Survey, and the watched layer adds 20-45 s cues in the first 5 minutes. If the integrator's naturals feel crowded, raise `GapMin`/`GapMax` in `CastleOnboarding`. The lint allows at most 45 s.
- **Budgets shared with CastleScares:** `footsteps_behind` and `candle_gutter` go through CastleScares, so they spend its per-run budgets too (6 and 6; the watched layer uses at most 3 and 2).
- **Capture mode** keeps the old Briefing start and has no onboarding component, so stills get no hunts or figures.
- **Shared-file conflicts:** shared files were edited per this wave's brief: Game, Interface (HUD only), Investigator (Hide and the focus ray), Interactable, CastleWorld (`Hide`/`Nail` only).
  - The lighting, door, layout, note-UI and audio pieces may touch the same files. My hunks are small and local.
  - If fb-doors changes `Door.cs` audio ids, `door_slam` still uses the `door`/`latch`/`knock` clips directly.
- **Bellwether** rules are identical (fuzz) and its HUD path is unchanged. The rack and Briefing hide refusal stay silent there as before.
