# Piece count12 (wave G): the Count's fixes for the r10 static critique

Branch `hc/count12` from `haunted-castle` `c4b24c1`, worktree `/Users/midir/hollow-current-haunted-castle/.wt/count12`. Model: Opus 5.5. Date 2026-10-14.
The owner was streaming during this wave, so only the code changed: nothing was launched, built, baked or rendered.
`python3 tools/check_source.py` passes (487 rules assertions; runtime and editor C# compile against the player and editor assemblies).
Source: `docs/haunted-castle/rounds/r10/CRITIC_STATIC.md`. That file is untracked in the main clone at `/Users/midir/hollow-current-haunted-castle/docs/haunted-castle/rounds/r10/CRITIC_STATIC.md`.

Files changed:
- `Assets/HollowCurrent/Runtime/Presence.cs`: castle branch. The shared methods only gain `count!=null` lines, so Split Grin behaves identically.
- `Assets/HollowCurrent/Runtime/Castle/CastleRun.cs`
- `Assets/HollowCurrent/Runtime/Castle/CountClaudeProfile.cs`

## Changes (critic number, then what it does now)
- **#5** CastleRun no longer speaks `cc_hunt_smell` at hunt start. The line plays only at the near-miss slats (Beat `near-miss`).
- **#6** The wing loop follows `Presence.WingsBeating`: visible, in the Glide or Dive pose, and with measured speed above 0.4 m/s. This applies in hunts and between them, never by HuntActive. Teleports read as speed 0. The candle bed swells only when he is within 12 m of the hall candles, in any phase; it no longer follows the hunt state.
- **#11** One rim value in every state: `RimLightScale` 0.35 (staged captures and the choreography keep 1).
  - Between hunts, when a perch move is due, there is a 1-in-5 roll (`ObservedReperchChance`) for a visible flight to another perch, even while she watches. Nothing can kill during it.
  - Log: `HC_COUNT_PERCH flight observed= roll=`. If no perch qualifies or no route exists: `flight rejected reason=`.
- **#14** New `Presence.DiveEndsIn`: seconds until he reaches grab range, computed as (distance - GrabDistance) / speed. It is +Infinity when he is not diving.
  - New event `Presence.DiveLastSecond` fires once per dive at an ETA of 1 s or less while he sees her (`HC_COUNT_DIVE_LAST eta=`).
  - The torch failure itself is the integrator's (see hooks).
- **#16/#28** A hide he saw her enter is opened from any hunt state: perch, flight, land or dive. Nothing about the torch matters.
  - `WitnessHide` arms the opening (`HC_COUNT_WITNESSED hide at`) and records the hide as his last known place.
  - The sequence: he flies to the door and crawls to 0.5 m from it, then pauses 1.6 s (`WitnessedPauseSeconds`). Beat `hide-open` makes CastleRun rattle the door 0.5 s before the grab, then he grabs with GrabDrop (falling back to ChooseGrab if the state is missing).
  - Logs: `HC_COUNT_GRAB ... hideEntrance=True witnessed=True torch=False`, then `Die()`. If the driver's invulnerability blocks the kill: `grab blocked reason=`, and he leaves.
- **#17** Near-miss: at Dread 80 or more, or in Long Night, it opens 1 time in 4 (`NearMissOpenChance`) with GrabCrawl.
  - The begin line logs `open=`, `openRoll=`, `eligible=`, `dread=` and `longNight=`.
  - The end logs `HC_COUNT_NEARMISS open=True/False`.
- **#20** In `HearPlayerRoughly`, a snapped guess closer than the minimum offset (3 m when hidden) is rejected and another bearing is tried, up to 8 times. Each rejection logs `HC_COUNT_HEARD rejected ... reason=` (no mesh, other floor, or snapped under the minimum).
  - The fallback, when hidden, is an unsnapped point at the minimum offset (`HC_COUNT_HEARD fallback reason=`).
- **#29** His last known place (`lastSeen`) is also set when he hears her and when he sees her hide. The next hunt, including a hostile return after its swoop, goes there first (`HC_COUNT_LEARNED ... hostileReturn=`).
- **#31** Near-miss and witnessed landings try four points: 2.2, 1.4 and 0.7 m in front of the door, then the door point itself, then the nearest NavMesh point within 3 m.
  - Each miss logs `landing rejected ... reason=no route`.
  - If all fail: `abort=route reason=`, and he prowls where he is.
  - Other aborts: `abort=spot`, `abort=seen`, `abort=left`.
- **#34** A hostile return opens with a harmless, visible swoop.
  - It flies sky (6, 10, 12) to low over the fountain (0, 4.6, 9.5) to out over the gate wall top (0, 8.7, 0.4). These are castle-local `ReturnSwoopLocal` points. No dive and no search happen during it.
  - Presence calls `Game.Sound.Speak("dispatch")` and raises Beat `return-swoop`.
  - Logs: `HC_COUNT_RETURN_SWOOP begin ... leg1clear= leg2clear=` and `end`. After the swoop he searches the last known place, or hears her roughly.
- **#35** With the torch on and not hidden, his sight range is 14 m × 1.5 = 21 m (`TorchSightScale`). `HC_COUNT_AI why=` prints the effective range.
- **Other `reason=` lines:**
  - Perch choice prints `x(current|near N m|same-floor|in-view)` per perch.
  - `HC_COUNT_ROUTE abort=perch|search reason=`.
  - `HC_COUNT_PERCH abort=hide reason=`.

## Hooks the integrator must wire
1. **Torch failure (#14).** In `Game.cs`, replace the `Creature.Diving` rising edge with `Creature.DiveLastSecond += () => Scares.Blackout(1);` (or poll `Creature.DiveEndsIn <= 1`), and keep the `HC_TORCH_FAIL` log.
2. **Hostile-return card (#34).** Show the new CaseBook line on Beat `return-swoop` (subscribe to `Creature.Beat`).
   - `Speak("dispatch")` in the castle resolves to `voice_cc_dispatch`, which does not exist yet and will log `HC_MISSING_ART`.
   - Either add the voice (F4), or map `"dispatch"` in `Soundscape.Speak` to the radio dispatch line you choose.
3. **Nothing else.** The wing loop, door rattle and candle bed are in CastleRun; the rest is inside Presence.

## What a windowed run must prove
- A no-tell hunt start gives no voice, no wing-loop change while he is perched, and no rim change (per-frame log: `HC_COUNT_RIM` must not appear at the hunt edge; wing volume stays 0 until he moves).
- A watched hide with the **torch off** while he is perched or travelling ends in `HC_COUNT_WITNESSED begin`, then `hold`, then a door rattle, then `HC_COUNT_GRAB hideEntrance=True witnessed=True torch=False`, then death (or `grab blocked` under the driver).
- A near-miss at Dread 80 or more (or Long Night `-hc-long-night`) logs `open=` on every begin, and over about 12 near-misses at least one `open=True` with a crawl grab.
- No `HC_COUNT_HEARD guess=... offset=` below 3.0 while `hidden=True`.
- `HC_COUNT_LEARNED` appears at the second hunt's start and at a hostile return's swoop end.
- The hostile return shows the swoop over the courtyard (check that both `legNclear=True`, or that no wall clips on screen), the dispatch card or voice, and only then dives on the bridge.
- Between hunts, `HC_COUNT_PERCH flight observed=True` occurs about once in five perch moves; the wing loop is audible only while he is flying.
- `HC_COUNT_DIVE_LAST` precedes each grab by about 1 s, and the torch failure (once wired) lines up with it.
- With the torch on in the open, he sees her at 15-21 m (`HC_COUNT_AI seen=True dist>14`); with it off, no sighting beyond 14 m.
- Bellwether: unchanged (RulesTests green; no `HC_COUNT_` lines).
