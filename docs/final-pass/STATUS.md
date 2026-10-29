# Final source pass — October 29, 2026

**Source implementation is complete. Fresh prepared scenes and a new macOS app are not produced by this pass.** The owner waived live playtests and declined pausing GPU training. The normal Castle bake waited 180 seconds at the shared two-client GPU cap, then returned `cap-full-timeout` (75) without launching Unity. No foreign job was paused or stopped.

The October 25 combined app and October 29 recording-checkpoint app predate these changes. Do not present them as final-pass builds. Run `./tools/build_macos.sh` when GPU capacity is available: it prepares/bakes both maps, builds the player, records its exact source inputs and assembly, and creates the macOS ZIP. `./tools/play.sh` refuses stale builds.

## Implemented source changes

| Issue | Change |
|---|---|
| #3 | Reader/UV records use a short dwell and wider targeting. EVP has a short listen and no stationary/hunt prohibition. The music-box interaction can record its voice. Three records identify the source automatically: no answer quiz, compulsory family-note collection, or mandatory survived hunt. Closure holds take three seconds. |
| #4 | Gatekeeper's letter moved into the forward approach on a sideboard with a candlestick and a clearer entry-table objective. |
| #5 | Hilt moved from the high orrery socket to a low pedestal beside it; relic focus volumes enlarged. |
| #6 | Standard Castle has visible warnings, longer recovery/grace, slower approaches and a longer grab threshold. Evidence, notes, relics and forged sword are checkpointed and persisted per map. Death no longer relocates items. Continue is the first recovery action. |
| #7 | A 2.2 m break in the east gallery rail and floor edge provides a drop to the main hall. |
| #8 | Two successful hits; 0.75 s stagger plus 4.25 s retreat. Re-approach starts without another idle-perch delay, respecting last-known position/line of sight. Sword strikes respect walls. The active sword encounter stays open until resolved. |
| #9 | Explicit sword/escape selection, path-specific counters, real room names, pickup confirmation, missing-item feedback and visible forge progress. Equipment/HUD follow the selected path. |

Other integrations: all wall art and the recording-checkpoint branch; per-painting adult pupil calibration with reduced travel, static child portrait; default 60 fps/coordinator override; objective text cached by progress; paper audio tail trimmed; exact approved Fish breath unchanged. README, ABOUT_ME, contribution notes, provenance and guarded build/launch scripts are included.

## Verification and limits

`checks.json` records the source-only results: player/editor C# compilation, 1,778 deterministic assertions (historical tuning fixtures plus current release contracts), 260 Fish checks, strict quality over 103 audio clips, audio routing, five rebuild-regression tests, 40 protected Tripo names, and 11 wall-art checks. Gitleaks found zero leaks in the working files and the 493-commit history scan. An exact Fish-key scan of Git objects and changed text also found zero matches, without exposing the key.

No fresh runtime run or performance measurement was performed. Actual human usability, rendered drop landing/eye alignment and fight timing remain unverified in-game. Older native test reports are historical and do not establish a pass for this source release.
