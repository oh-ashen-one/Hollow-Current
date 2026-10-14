# Piece count14: quiet start for no-tell and hostile-return hunts

Branch `hc/count14` from `haunted-castle` `3134424`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/count14`. Model: Opus 5.5. Date 2026-10-14.
Code only: nothing was launched. `python3 tools/check_source.py` passes (970 rules assertions; runtime and editor C# compile).

Problem (runtime evidence): in both sampled no-tell hunts, the wing loop jumped by +0.6 at take-off because the first flight leg rolled "full".

## Change (Presence.cs castle branch, CountClaudeProfile.cs)
- A hunt that starts as a no-tell hunt (`Case.NoTellHunt`) or as a hostile return flies its first leg silent and its second leg silent or soft (50/50).
  - The first leg is whichever comes first: perch take-off, search, or the return swoop.
  - Logs: `HC_COUNT_LEG first=silent reason=notell|return leg=` and `HC_COUNT_LEG second=silent|soft reason=`.
  - Told hunts keep the one-third roll (`HC_COUNT_WINGS`). A dive is still always full volume, and he only dives once he has seen her.
- For the first `QuietStartSeconds` (8 s) of such a hunt, he plays no one-shot unless he has seen her.
  - His breath is the only one-shot his take-off can make. It is held (logged once: `HC_COUNT_ONESHOT held=breath reason=`).
  - Hunt voices were already held by the integrator's 8 s rule.
  - Doors he flies through are opened silently (`Door.Open` is set directly; the latch and door sounds are only in `Door.Toggle`).
  - The Return's `drawbridge_chains` is the bridge itself lowering (phase change), so it is not his take-off and is left as it is.

## Windowed run must show
- In every no-tell hunt and every hostile return:
  - `HC_COUNT_LEG first=silent`, then `HC_COUNT_LEG second=silent|soft`.
  - The channel-log wing volume stays 0 through the first leg and at most 0.3 on the second.
  - No `breath` cue in the first 8 s unless `seen=True` appears in `HC_COUNT_AI` first.
- In told hunts, the `HC_COUNT_WINGS` loudness mix is unchanged.
