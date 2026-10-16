# UI variants task handoff

- Repository verified: `https://github.com/oh-ashen-one/Hollow-Current.git`.
- Task branch: `codex/ui-variants`; base commit `be4c828022ed7df99d87ba701c75246392140fcf` from the current Haunted Castle integration checkout.
- Task ownership: only `docs/haunted-castle/ui/variants/`. The shared integration checkout and existing worktrees were left untouched. This is a new task-specific handoff, not the protected CODEX_HANDOFF.md.
- Deliverables: five art directions, 40 full-size PNGs, per-direction style/asset documents, reproducible entry points, original procedural textures, original OFL fonts/licences, comparison sheet and ranked index.
- Verification: `python3 verify.py`, 5,776 checks passing, 81 deterministic output files, no text intersections, all required strings and font glyphs present. See `QA_REPORT.md` and `verification.json`.
- Suggested choice: Last light first, Dead channel second, Two moons third, Casefile fourth, Occult folio fifth. Owner selection is the next product decision.
- Future work: after the owner selects a direction, implement its textures/fonts/rectangles in a new scoped engine task. No engine changes or merge are part of this task.
- The task branch is to be pushed and verified against `git ls-remote`; the final response records the immutable commit and remote verification. Temporary sparse worktree cleanup happens only after that verification and delivery archive creation.
