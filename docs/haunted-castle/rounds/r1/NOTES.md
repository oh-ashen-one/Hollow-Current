# Round 1 captures: Haunted Castle

Facts only. Judging these is the critic's job.

- Captures: `/Users/midir/hollow-current-haunted-castle/docs/haunted-castle/rounds/r1/captures/` (gitignored), with `capture.json` (cameras, notes, settings and frame times, as written by the player).
- Player: `/Users/midir/hollow-current-haunted-castle/Builds/macOS-Castle/Hollow Current Castle.app`, built with `HollowCurrent.Editor.NativeBuild.BuildPreparedMacCastle` from source commit `346bb51`. The bake that went into the build is committed as `84dedf6` (Castle receipt: 4 lightmaps, 196 probes, 17 reflections).
- Command: `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label hc-capture -- ".../Hollow Current Castle.app/Contents/MacOS/Hollow Current" -hc-capture /Users/midir/hollow-current-haunted-castle/docs/haunted-castle/rounds/r1/captures`. Slot hold was 34 s and exit was 0.
- Machine: Mac Studio M3 Ultra, Unity 6000.3.20f1 Mono player.
- Window: full-screen window at 2560x1440 on a 3440x1440 display. Every PNG is 2560x1440.
- Quality: level "Ultra", MSAA 4x, shadow resolution Medium, shadow distance 16 m, `Application.targetFrameRate` 90, vSync 0 (the frame-time floor is therefore 11.1 ms). Camera FOV 72°, built-in render pipeline.
- Capture mode setup: 3 s warm-up after load. The HUD is off for S1 to S6 and the hunt frames. Investigator input is disabled. The field kit was not collected for S1 to S4, so the flashlight is off there. For S5, S6 and the hunt the flashlight is on at its in-game intensity. Eye height is 1.64 m above the floor.

| Shot | File | Reference | Eye (x, y, z) | Look-at (x, y, z) | Staging |
|---|---|---|---|---|---|
| S1 | S1_great_hall.png | refs/great_hall.png | 0, 1.64, 14.70 | 0, 2.60, 27.00 | just inside the hall great doors |
| S2 | S2_chapel_altar.png | refs/chapel_altar.png | 7.00, 1.64, 6.50 | 13.00, 1.75, 6.50 | nave aisle, looking at the altar |
| S3 | S3_crypt.png | refs/crypt.png | -5.20, -1.56, 25.00 | 0, -2.60, 22.50 | crypt side of the service-stair door, looking at the sarcophagus |
| S4 | S4_exterior_sky.png | refs/exterior_sky.png | 0, 1.64, -9.50 | 0, 5.00, 8.00 | far bank; drawbridge snapped to lowered for the shot |
| S5 | S5_lord_perch.png | refs/lord_perch.png | 2.50, 1.64, 15.50 | -3.15, 5.52, 21.00 | Count Claude staged on the west balustrade top (-3.15, 4.32, 21.00), above balcony perch (-4.40, 3.20, 21.00), Mode 0 |
| S6 | S6_lord_sheet.png | refs/lord_sheet.png | -3.00, 1.64, 8.00 | -3.00, 1.30, 11.00 | Count Claude at (-3, 0, 11), 3 m ahead on the courtyard, Mode 1 |
| S7 | S7_title.png | (title) | n/a | n/a | title screen, Castle selected; Bellwether entry disabled (scene not in this castle-only build) |
| S8 | S8_journal_ledger.png | (journal) | n/a | n/a | note view from the journal on "Ledger: the steward" |
| H01 to H10 | H01_hunt_dive.png ... H10_hunt_dive.png | (hunt) | 0, 1.64, 15.60 | 1.50, 1.60, 24.00 | staged hunt: evidence x2, warning skipped by `Case.Tick(9.05)`, hunt origin (4.6, 0, 26.8); one frame per second |

Hunt sequence (from capture.json): t = 1 s hunter at (4.70, 0.05, 24.30), 9.89 m from the camera. t = 2 s 6.81 m. t = 3 s 3.41 m. t = 4 s phase Dead, 0.58 m (grab completed). t = 5 to 10 s Dead, hunter stationary at (0.24, 0.05, 16.12).

## Frame times (unscaled `Time.deltaTime`, in-player)

| Window | Frames | Mean | p95 | Max |
|---|---|---|---|---|
| S1 great hall, 3 s held view | 269 | 11.19 ms | 11.67 ms | 13.97 ms |
| Hunt / dive, 10 s (includes 10 PNG encodes and writes on the main thread) | 907 | 12.20 ms | 11.32 ms | 131.81 ms |

## Recorded state at capture time

- UI art: `Resources/UI/BloodLedger/{Dialogue,Warning,Panel}` is not present, so the castle card art is the magenta placeholder (S7, S8). The title backdrop is the existing `UI/MenuBackdrop` image.
- Castle note art (`Resources/Castle/Ledger*`) is not present, so S8 is the text-only note layout.
- `CastleArt.ImportYaw` is 0. No asset was re-oriented for this round.
- The sky material resolved through `CastleArt.ApplySkybox()`, and the castle camera clears to the skybox.
- The layout does not place `GiantSkeleton`. The castle's own floating props (six candelabra floaters around the rune ring, five books in the library gallery above the dining hall) come from `CastleRun`.
