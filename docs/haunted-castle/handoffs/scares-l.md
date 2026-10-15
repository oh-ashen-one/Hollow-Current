# HANDOFF: scares-l (wave L, hc/scares-l)

Base `haunted-castle` af96343. Model: Sonnet 5.5. 2026-10-14. Code only, no Unity, no game. Verified: `check_source.py` (1071 assertions PASS, compiles), `tools/check_castle_scares.py` PASS (mutation-checked: a phase read inside `HiddenBeat` fails it), `tools/check_audio_keys.py` OK (65 uses, no missing keys). Only `Assets/HollowCurrent/Runtime/Castle/CastleScares.cs` and `tools/check_castle_scares.py` changed. ScareDirector, Game and Presence untouched; no integrator diff is needed for this wave.

## 1. Beats for the hidden player (critic r12: 12 minutes hidden = 1 scare event)
- `TickHidden` runs its own clock while `Player.Hidden`: first beat 20-45 s after hiding, then one every 40-70 s. `HiddenBeat()` picks a kind by `Weight` from four budgeted keys (`hide_breath`, `hide_clawstep`, `hide_handle`, `hide_whisper`; each max 8 per run, gap 90 s, shared no-twice-in-a-row rule) and spends it with the usual `pick= budget=` suffix. If every kind is refused it retries in 10 s.
  - **Breath at the slats:** two breaths at the door of the nearest hide (`Interactable` of kind Hide), 1.4-2.2 s apart.
  - **Claw step passing:** 4-5 steps along the outside of the door, 1.2 m out, left-to-right or the reverse, 0.5-0.7 s apart.
  - **Handle rattle (lie):** the latch rattled once or twice at the door.
  - **Whisper (lie):** one quiet whisper 0.9 m outside the door.
- **No-tell parity:** the clock and the draw read no phase and no Dread. The only phase reference is the guard that a beat falling due in the first 5 s of a hunt waits for them to pass (`// tell-safe:` comment), plus Briefing/Dead exclusions. `HiddenBeat` and the three coroutines read nothing hunt-related (the lint enforces this).
- **Audio:** wanted keys `castle_hide_breath`, `castle_hide_clawstep`, `castle_hide_handle`, `castle_hide_whisper` (from F4). Until they exist on disk the placeholders play: `breath_presence`, `claw_steps_0..3`, `door_rattle`, `breath_presence` (resolved at play time with `Sound.Has`, so dropping the files into `Resources/CastleAudio` switches them with no code change). The keys sit in a string array (`HideClips`), not as literals in `Cue`, so `check_audio_keys.py` passes now; `check_castle_scares.py` prints which wanted keys are still waiting.
- **Logs:** `HC_SCARE_CASTLE hide_breath pick=hidden weighted w=../.. rank=1 budget=1/8 gap=90 since=-` and `HC_HIDE kind=hide_breath hidden_s=.. phase=.. door=.. clip=..` (the phase appears in the log only, for the critic's parity count).

## 2. Escalation rule seam (hc/rules-l has not landed)
`CastleScares.Escalate(near)` now calls `RuleAllows(out why)` before `Investigation.TryEscalate`:
- never on the first apparition (`ScareDirector.Events <= 1`), `reason=rule_first_apparition`;
- no escalation-born hunt among the last two hunts (history kept by `TrackHunts`; a hunt is escalation-born if `Escalate` succeeded within 12 s before it), so at most one hunt in three: `reason=rule_recent_escalation_born`;
- about 15 % with the torch lit, 25 % dark. `TryEscalate` still rolls `Rules.EventEscalationChance` (0.35 in the castle) itself, so the gate passes with probability target / 0.35 and the product is the target: `reason=rule_roll_p=0.43` when it declines.
When `HuntRules.ShouldEscalate` lands on hc/rules-l, replace the body of `RuleAllows` with a call to it (the seam is one method; its signature takes only what the rule needs: apparition number, torch lit, hunt history). If F1's `TryEscalate` then stops rolling its own chance, drop the `target / EventEscalationChance` scaling. The `HC_ESCALATE` log line is unchanged and now shows `reason=rule_*` for refusals.

## What a windowed run must prove (log keys)
- Siege-style run, 12 minutes in one hide: 12-18 `HC_HIDE kind=` lines, intervals 40-70 s (first 20-45 s), all four kinds present, none more than 8, none twice in a row, each with `pick=hidden weighted`.
- Parity: across hidden periods in Calm and Hunt, the `HC_HIDE ... phase=` split matches the time spent hidden in each (beats per minute hidden equal within noise); no `HC_HIDE` with `phase=Hunt` earlier than 5 s after `HC_HUNT_START`.
- Placeholders audible now; no `HC_MISSING_ART` for `castle_hide_*` (they are resolved with `Has`); after F4 lands the files `clip=castle_hide_*` appears in `HC_HIDE`.
- `HC_ESCALATE` over 3 natural runs: the first apparition always `reason=rule_first_apparition`; escalation-born hunts at most 1 in 3 (`escalated=True` never within two hunts of another); share of hunts starting within 5 s of an apparition falls from 5/7 (r12) toward 15-25 % of attempts.
