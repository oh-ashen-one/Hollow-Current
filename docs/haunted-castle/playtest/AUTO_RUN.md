# Automated castle playthrough (both endings)

Facts from the real combined player. Judging them is the critic's and the owner's job.

- **Player:** `/Users/midir/hollow-current-haunted-castle/Builds/macOS/Hollow Current.app`, both maps, built from commit `43c2152`. It starts in Bellwether, and the driver switches to Haunted Castle through the title map picker path (`SelectMap` + `BeginInvestigation`).
- **Driver:** `Assets/HollowCurrent/Runtime/Castle/PlaytestDriver.cs`, run as `-hc-playtest <outdir> escape|kill` through `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-playtest`.
  - **Input:** it plays through `Investigator.TestInput`, the same code path as keyboard and pad input: look, E press/hold, instrument button, tool select, torch. Every E press goes through the investigator's own interaction ray.
  - **Movement:** it teleports between stops; it does not walk except on the drawbridge.
  - **Safety:** driver-only `Investigation.Invulnerable` would count a grab instead of ending the run. It counted **0** grabs in both runs (`blockedDeaths=0`), so neither ending relied on it.
- **Screenshots** (one per phase, 1920x1080, gitignored): `/Users/midir/hollow-current-haunted-castle/docs/haunted-castle/playtest/auto/escape/` and `/Users/midir/hollow-current-haunted-castle/docs/haunted-castle/playtest/auto/kill/`.
- **Logs:** `/Users/midir/hollow-current-haunted-castle/.playtest-escape.log` and `/Users/midir/hollow-current-haunted-castle/.playtest-kill.log` (lines `HC_PLAY`, `HC_PLAY_BLOCKER`, `HC_PLAY_RESULT`).

## A. Escape ending: WON (82.9 s)

| t (s) | Phase | State after |
|---|---|---|
| 1.1 | begin (castle loaded, case started) | Briefing |
| 1.3 | field kit from the lantern cart (E) | equipped |
| 1.7 | gate keeper's letter read, entered the courtyard | Survey |
| 4.9 | compass held on the dining-hall residue | Induction recorded, Disturbance |
| 8.1 | grave-ash lantern held on the claw-scores | Handprint recorded |
| 12.9 | ear-trumpet: ask once, stay still | Voice recorded (3/3) |
| 13.6 | three Blood Ledger pages read | notes 4 |
| 13.8 | chapel bell rung (E) | Warning |
| 14.0 | hid in the Count's wardrobe, torch off | hidden |
| 22.8 | hunt started (Count on the west balcony perch) | Hunt |
| 60.8 | hunt survived, no grab | Recovery, survived |
| 61.0 | ledger closed (journal reconstruction: chapel bell / bargain's toll / let the last stroke fade) | Containment |
| 61.2 | chapel brazier relit (E) | breaker on |
| 74.9 | bell rope held 13.5 s | Return |
| 78.5 | drawbridge lowered (finished moving) | lowered |
| 82.9 | walked from the gatehouse over the bridge into the far-bank trigger | **Won**, "LEDGER CLOSED" |

`HC_PLAY_RESULT mode=escape won=True slain=False blockedDeaths=0 blockers=1`

The blocker was that the Count's wardrobe was not focused by the interaction ray from the driver's stand point. The driver then used the cupboard directly (hidden=true). In the earlier run the same wardrobe was focusable, so focusing this cupboard depends on where the player stands.

## B. Kill ending: WON (87.6 s)

| t (s) | Phase | State after |
|---|---|---|
| 1.1–13.6 | same as A up to the ledger | 3 records, ledger read (3 relic hints unlocked) |
| 13.8 | reliquary box used before the reveal | stays shut ("will not open") |
| 14.9 | grave-ash lantern held on the reliquary claw marks for 1 s | blade revealed |
| 16.6 | reliquary opened; hilt taken from the orrery socket; portrait door opened, moonstone taken | relics 3/3 |
| 27.2 | altar E held 10.5 s (9 s craft) | moonstone sword forged, rune ring ignites |
| 27.4 | chapel bell rung with the sword (calls him) | Warning |
| 36.4 | hit 1 (reach 0.97 m) | Stagger, then 20 s retreat |
| 58.9 | hit 2 (tier 1) | Stagger, retreat |
| 83.5 | hit 3 (tier 2) | Death (`HC_COUNT_DEATH`), WinByKill |
| 87.6 | ending screen | **Won**, kill text "Sunrise through the rose." |

`HC_PLAY_RESULT mode=kill won=True slain=True blockedDeaths=0 blockers=0`

## Fixed in piece-0 files during this run

- **The chapel bell did nothing before containment.** DESIGN.md says "ring it, the bargain hears you, survive the hunt". Now:
  - Before the ledger is closed (2+ records, no hunt survived yet), ringing it starts the warning.
  - With the sword, ringing it calls him back for the fight.
  - In containment it stays the hold-to-seal.
  - Files: `CastleRun.RingBell`, `Interactable`, `CaseBook.BellRung`.
- **Driver input:** a two-frame key press re-used E (entered and left the cupboard). The driver now presses for exactly one frame.

## Findings in other pieces' files (for the director)

1. **Count AI (Presence, count piece):**
   - In all three hunts the Count stayed on the west balcony perch at (-3.22, 4.25, 20.51). `HC_HUNT_SPAWN ... flight=Perched`, and every later Count line has the same position.
   - In the kill run the investigator stood 0.97 m from him for most of two hunts and was never grabbed (`blockedDeaths=0`); `seen=True` appears in some lines.
   - In the escape run he never left the perch while the investigator hid on the upper floor.
   - The hunt pressure the design describes (glide between perches, dive at the player) did not occur in these runs.
2. **No sword model:** after forging, the investigator still holds the current instrument (the grave-ash lantern in `kill/17_hit_1.png`); strikes use the instrument button.
3. **Hilt and moonstone focus:** both were focused normally by the interaction ray. The orrery socket's collision proxy did not block it.
