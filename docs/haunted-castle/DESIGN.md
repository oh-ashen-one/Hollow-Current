# Haunted Castle — design (branch `haunted-castle`)

Second map for Hollow Current, separate from the Bellwether house. Same investigation loop, a new place, a new hunter, and one new beat: craft a weapon and fight him.

Owner decisions (2026-10-13):
- Majority of the Bellwether loop carries over; add an optional weapon craft that lets you defeat the hunter.
- Hunter: winged vampire lord. Name: **Count Claude** (owner decision 2026-10-13).
- Surrealism: heavy. You are not on a human planet.
- Castle: medium, hand-authored, ~10–14 rooms.
- Weapon: hidden recipe, three relics, one fight. Escape via the drawbridge still wins.
- Images: Higgsfield GPT Image 2.5 (owner's call, overrides the Codex-imagegen default). 3D: Higgsfield `generate_3d` / `multi_image_to_3d`, one job at a time, logged in `ASSET_LICENSES.md`.

## Fiction

Count Claude was a margrave who bargained with something under the castle for a night that never ends. The castle slipped half out of the world: two moons, a red moat, candles that float, a doorway that sometimes returns you to the room you left. The family story (Bellwether's "Mourning Note" idea) becomes the **Blood Ledger**: the household's last ledger entries, written as the bargain took them one by one. Reading the ledger reveals where the three relics were hidden by the last steward who tried to kill him.

## The loop (mapped from Bellwether)

| Bellwether | Haunted Castle |
|---|---|
| Van (safe start / finish) | **Drawbridge gatehouse**: your lantern cart. Outside the walls is safe. |
| Induction reader | **Hollow Compass**: a needle that spins near the Count's residue; same ranges and behaviour as induction. |
| UV light / handprints | **Grave-ash lantern**: reveals claw-scores and wing-drag on walls and floors. |
| EVP recorder | **Vellum ear-trumpet**: records the Count's whispers (reuses the EVP capture logic and the `Speak(id)` voice path). |
| Mourning Note entries | **Blood Ledger** entries (same Interface journal, different art and titles). |
| Music box → let the song end | **Music box becomes the Chapel bell**: ring it, the bargain hears you, survive the hunt, then the ledger can be closed. |
| Return to van | **Lower the drawbridge and cross it**. |
| Split Grin | **The Hollow Count** (new Presence profile, see below). |

Win conditions:
1. **Escape** (same as Bellwether): complete evidence, close the ledger, survive the hunt, cross the drawbridge.
2. **Kill**: forge the Moonstone Sword at the chapel altar, confront him in the great hall, land three clean hits while he dives and retreats. Ends with sunrise through the rose window.

## The Count — behaviour deltas from Split Grin

Reuse `Presence.cs` phases/flags; add a `HunterProfile` with these overrides:
- **Vertical presence.** Perch points on balconies, the rose-window ledge and the library gallery. Between hunts he is seen perched, not patrolling. Idle = perched, wings folded; "Search" = glide between perch points; "Reach" = dive.
- **Sight and sound.** Sight cone 14 m / 80° (slightly wider than Split Grin's 12 m / 72°), but he must have line of sight from above: ground-floor cover (tables, pews, cupboards) hides you; crouch-walking under galleries is safe-ish.
- **The dive.** Chase is a glide: faster than sprint on long straights (4.6 m/s vs 3.9), slower in tight rooms (3.0 m/s) because wings clip columns. Players escape by breaking line of sight around columns and through narrow service doors (0.9 m, he must land and walk).
- **Candle rule.** Floating candles bend their flames toward him. They are the tell before he is visible.
- **Grab** stays at 1.05 m; the grab animation is a wing-wrap.
- **With the sword**: he gets a stagger state on hit and retreats to a perch for 20 s, then comes back lower and angrier. Three hits kills.

Animator Mode mapping (keeps `HearthboundActor` contract): 0 Perch-Idle, 1 Glide, 2 Search-Glide, 3 Dive/Reach; new 4 Stagger, 5 Death.

## The castle — rooms (hand-authored, `CastleWorld.Build`)

Three vertical layers like the house (y = −3.2 / 0 / 3.2) plus one tower level (y = 6.4). Footprint ~ x ±14, z 0–28.

Ground (y 0): 1 Gatehouse + drawbridge (start), 2 Courtyard (open to the sky, two moons, dead fountain), 3 Great hall (table, giant skeleton, rune ring; the fight arena), 4 Dining hall, 5 Kitchen (surreal: knives orbiting the block), 6 Chapel (altar = forge; relic recesses).
Upper (y 3.2): 7 Library gallery (floating books, perch), 8 Portrait hall (eyes follow; one portrait is a door), 9 Count's bedchamber (ledger pages), 10 Balcony over the great hall (perch).
Tower (y 6.4): 11 Observatory (the two moons, a brass orrery with a third, missing planet).
Crypt (y −3.2): 12 Crypt (sarcophagus, blood channel, hand-roots), 13 Reliquary (one relic), 14 Service stair that sometimes returns you to the room you left (the one geometry trick: a teleport volume, triggered once per investigation).

Relic placement (hidden; the ledger entries hint):
- **Silver blade**: Reliquary, inside a reliquary box that only opens after the Grave-ash lantern reveals the claw marks on it.
- **Hallowed hilt**: Observatory, in the orrery's empty planet socket.
- **Moonstone**: Portrait hall, behind the portrait-door.

## Surreal set-dressing list (heavy)

Floating candelabras and books (slow bob, flames bend toward the Count), rune ring on the great-hall floor (glows as you pass), giant skeleton draped over the hall, two moons in every window (skybox), blood-red moat and crypt channel, hand-roots from the crypt ceiling, orbiting kitchen knives, portraits whose eyes track, bats that are too large, stars that are too big, the service stair loop. Everything is a static mesh plus one small script; no new physics.

## Pieces (for the build loop)

| # | Piece | Owner model | Files |
|---|---|---|---|
| 0 | Map plumbing: `MapId` enum, scene `Castle.unity`, map-keyed checkpoint, bake receipt per map, build method, title-screen map picker | Main session (coupled) | Game.cs, Interface.cs, HouseLighting.cs, NativeBuild.cs, Investigation.cs |
| 1 | `CastleWorld` layout: rooms, doors, nav, spawn/anchor points, lighting rig | sonnet-builder | new `CastleWorld.cs`, `CastleLayout.cs` |
| 2 | Castle art: architecture meshes (Blender script, same pipeline as `Resources/Architecture`), materials, skybox with two moons | sonnet-builder | `tools/castle_*.py`, `Resources/Castle/` |
| 3 | Surreal props + scripts (floaters, rune ring, candle-tell, portrait eyes, stair loop) | sonnet-builder | `Runtime/Castle/*.cs` |
| 4 | The Hollow Count: model from the sheet (Higgsfield 3D), rig/animations, `HunterProfile`, perch/glide/dive, stagger/death | opus-builder | `Presence.cs` profile split, `Resources/Characters/CountClaude` |
| 5 | Tools reskin + Blood Ledger + relic/craft/fight rules | sonnet-builder (rules) + main (Investigation.cs) | `Core/Investigation.cs`, `Interface.cs`, new `Core/Forge.cs` |
| 6 | Audio: Count whispers via `generate_open_voice`, wing/dive/stagger SFX (Higgsfield Seed Audio), chapel bell | sonnet-builder | `tools/voice-lines.json`, `Resources/CastleAudio/` |
| 7 | Captures + critic rounds against `docs/haunted-castle/refs/` | opus-critic | `docs/haunted-castle/rounds/` |

Blockers found in the survey that piece 0 must clear first: scene/bake names hard-coded in `HouseLighting.cs:15,17,72`, `NativeBuild.cs:15,47–50`, `ControllerReleaseBuild.cs:25–28`; bake receipt hashes every `.cs` under `Assets/HollowCurrent` (`HouseLighting.cs:169–178`) so Bellwether's receipt must be regenerated or keyed per map; static checkpoint (`Game.cs:29–31`); fixed coordinates in `World.cs:19–25`, `Presence.cs:63`, `Investigator.cs:37–38`; required-asset throws in `AuthoredArt.cs:10`, `DetailArt.cs:7`, `Soundscape.cs:31–33`, `Presence.cs:32`.

## Scale constants (reuse)

Player: standing 1.8 m, crouch 1.15, walk 2.35, sprint 3.9, camera 1.64, reach 2.7. Doors 1.5 × 2.0 m. Floors every 3.2 m. Nav agent r 0.28 / h 1.8. Castle doors: great doors 3.0 × 4.5 m, service doors 0.9 × 2.0 m (Count cannot fly through).

## References

`docs/haunted-castle/refs/` — prompts (`*.prompt.txt`), job records (`*.job.json`) and chosen images. `lord_sheet.png` is canonical for the Count (black hair, grey skin, oxblood coat, hooked wings). Reference images were generated with Higgsfield GPT Image 2.5 on 2026-10-13 and are licensed per `ASSET_LICENSES.md`.

## Performance

Mac Studio M3 Ultra, 2560×1440 native, 60 fps budget (16.67 ms). Floaters and runes are instanced static meshes; the giant skeleton is one mesh. Measure the great hall from the entrance (heaviest view) in a real build before and after the surreal dressing.
