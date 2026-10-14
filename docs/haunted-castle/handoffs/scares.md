# HANDOFF: F3 scares (hc/scares), Haunted Castle fear plan items 5, 7, 8, 9, 15 and the item 6 hide-nailing

Branch `hc/scares` (from `haunted-castle` `fbd5807`), worktree `/Users/midir/hollow-current-haunted-castle/.wt/scares`. Model: Sonnet 5.5. Date 2026-10-13.
No Unity was launched. Verified only by `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py`: 396 rules assertions PASS and the runtime and editor C# compile clean (no new warnings).

## Files
- NEW `Assets/HollowCurrent/Runtime/Castle/CastleScares.cs` (+ .meta): `CastleScares` MonoBehaviour, plus runtime-only helpers `CueOnImpact` and `CrossingBat` in the same file.
- NEW `Assets/HollowCurrent/Runtime/Castle/Throwable.cs` (+ .meta): marker component, static `Throwable.All`, `Launch(game,velocity)`.
- `CastleWorld.cs`: 38 throwables, 3 floating book piles, dressing table, Sarcophagus interactable, `Points.RelicAltPositions[3]`, `Points.GatehouseBattlement`, `CastleWorld.NailHides` / `UnnailHides`.
- `Floater.cs`: `Floater.FalseTarget(point,seconds)`, `Floater.FalseActive`, `Floater.ClearFalseTarget()`.
- `KnifeOrbit.cs`: `ReleaseKnife(worldPoint)`, `ActiveKnives`. `WindowMoon.cs` and `SetPiece.cs` unchanged (nothing needed there).

## Wiring the integrator must add (shared files, not touched by me)
1. `Game.cs`: property `public CastleScares CastleDread {get;private set;}` and in the castle branch (line ~94, next to `CastleRun`):
   `CastleDread=gameObject.AddComponent<CastleScares>();CastleDread.Initialize(this);`
2. `ScareDirector.Initialize` after the Mug scan: `if(game.Map==MapId.Castle)foreach(var t in game.World.Root.GetComponentsInChildren<Throwable>())props.Add(t.transform);`
   (stuck kitchen knives gain Throwable at runtime and are not in this list; `Poltergeist` uses `Throwable.All` so it does see them.)
3. `ScareDirector.Dread()`, first line after the guard: `if(game.CastleDread){game.CastleDread.Beat();return;}` (Beat has the same phase/modal/hidden guard and returns false only when not calm).
4. `ScareDirector.Trigger`, replace `ThrowNearbyProp();` with `if(!(game.CastleDread&&Random.value<1f/6&&game.CastleDread.Poltergeist()>0))ThrowNearbyProp();`
5. Peripheral beats are not inside `Beat()` (they need the player's view). Suggested: in the same Dread() hook, `float r=Random.value; if(r<.2f&&game.CastleDread.WindowSilhouette()) return; if(r<.35f&&game.CastleDread.BalustradeShape()) return;` before `Beat()`. Both return false cheaply when no window/spot qualifies.
6. False tells: when `Case.Lies` (Dread >= 70): `CastleDread.FalseCandleTell()` for one candle tell in three; `CastleDread.FalseCompassReading()` when the compass is raised; Interface/compass code spins the needle while `Game.CastleDread.FalseCompass` is true (3 s).
7. `World.cs` `CastleSpots`: add `public Vector3[] RelicAltPositions;public Vector3 GatehouseBattlement;` and copy both in `From(p)` (`RelicAltPositions=p.RelicAltPositions,GatehouseBattlement=p.GatehouseBattlement`). They must be re-baked: run PrepareCastle again (the throwables, book piles, dressing table and Sarcophagus interactable are also scene content).
8. Long Night: `CastleWorld.NailHides(Case.Rules.NailedHides, Player.transform.position)` at run start; `CastleWorld.UnnailHides()` on a new run or retry. Nailed hides stay in `World.HidingPlaces`: filter on `gameObject.activeInHierarchy` wherever the Count picks a cupboard to search.
9. Sarcophagus (F2): interactable name `Sarcophagus`, Kind `Note` (the enum is in `Interactable.cs`, not mine), Label `Sarcophagus`. Hook it in `CastleRun.NoteRead(title)` on `title=="Sarcophagus"`. Side effect: `Case.Read` files a journal note "Sarcophagus" and `ShowNote` opens a card with flavour text; cleaner is to append `InteractKind.Sarcophagus` (end of the enum) with a `Use` case, then change `InteractKind.Note` to it in `CastleWorld.Crypt()` (one line). Trigger collider, 2.3 x 1.3 x 1.1 m over the sarcophagus.
10. F2 gatehouse event: use `Game.World.Castle.GatehouseBattlement` (once item 7 is in): castle-local (3.6, 7.5, 0), the gate wall top above the cart. The gatehouse ceiling top is 5.2, so from inside the gatehouse he is heard, not seen; he reads against the sky from the bank and through the gate. If a visible-from-inside landing is wanted instead, it has to be a different point (nothing inside the gatehouse is 3.5 m up with headroom for 2.5 m).

Audio keys used (all via `Game.Sound.Cue`, silent when missing): `book_drop`, `knife_stick`, `bat_pass`, `bell_single`, `knock` (existing). Log lines: `HC_SCARE_CASTLE book_drop | knife_stick | bat_pass | bell_single | candle_gutter | silence | poltergeist <n> | window_silhouette | balustrade_shape | balustrade_shape_gone | false_candle_tell | false_compass`, `HC_THROWABLES placed=38` at build, `HC_NAIL_HIDES nailed=<n>`.

## What each call does
- `Beat()`: roll 0..5; 5 (one in six) is silence. Else tries book drop, knife stick, bat pass, bell, candle gutter starting at the roll, first that applies wins, silence last.
  - Book drop: nearest pile (`Floating book stack A/B/C`, library x2 and bedchamber) or lone hall floating book within 14 m, top book detached with BoxCollider + Rigidbody, cue on first impact.
  - Knife: nearest orbiting knife to the player flies 0.5 s into the butcher block, quivers, `knife_stick`, then gets `Throwable`. Needs the player within 18 m of the orbit.
  - Bat: billboard with the GiantBat material crosses 3 m ahead (2 m if blocked) over 1.15 s, `bat_pass`.
  - Bell: `bell_single` at `Castle.Anchor`, bell art sways 2.6 s; never calls `RingBell`. Player within 34 m.
  - Candles: nearest `CandleFlame` within 7 m seeds a cluster (2.5 m, max 14) scaled to 0.2 for 2 s (0.25 s down, 0.6 s back).
- `Poltergeist()`: every `Throwable` within 6 m, inside 65 degrees and with a clear line, launched away from the player in a fan; returns the count.
- `WindowSilhouette()`: windows from `CastleLayout.Windows` on the room side, 25..70 degrees off centre, 2.5..18 m, clear line; a black bat quad (generated alpha texture) glides across 0.9 m outside the wall for 1.8 s.
- `BalustradeShape()`: arms; places a 0.8 x 2.1 m cloaked figure at x +-3.45, y 3.2 (behind the gallery rail) 28..65 degrees off centre, 4..17 m away, only while the player is on the hall floor; fades in; destroyed for good when the view comes within 20 degrees, after 40 s, or when the player leaves the hall.
- `FalseCandleTell()`: doorway 6..10 m away (same floor, no Count within 5 m), `Floater.FalseTarget(point,4)`; all Floater flames lean at 0.9+ toward it, then release to the hunter/auto target.
- `FalseCompassReading()`: `FalseCompass=true` for 3 s.

## Relic alternate places (`Points.RelicAltPositions`, castle-local, y = surface top + 0.1 as RelicLocal does)
| Relic | Alt (x,y,z) | Room | Furniture / stand point |
|---|---|---|---|
| 0 silver blade | (-9.9, 0.92, 4.6) | Kitchen | butcher block top 0.82 (x -10.25..-8.75, z 4.5..5.5), among the knife orbit; reach from z 3.6 or z 6.2 |
| 1 hallowed hilt | (-11.0, 1.2, 21.4) | Dining hall | sideboard top 1.10; its throwables leave x -11 clear; stand at (-11, 0, 20.0) |
| 2 moonstone | (-12.6, 4.05, 1.2) | Bedchamber | new dressing table (x -13.2..-12.0, z 0.9..1.5, top 3.95); stand at (-12.6, 3.2, 2.4) |
Originals are crypt / observatory / portrait closet, so every alt is in a different room. All three are on furniture whose proxy collider is solid, reachable from open nav floor within the 2.7 m reach; positions were picked from the layout code, not walked in a build.

## Throwables (38)
Great hall table 12 (6 plates, 2 books, 2 goblets, 2 candlesticks), dining table 7 + sideboard 4, kitchen prep table 7 (incl. 2 knives), library reading table 5, bedchamber dressing table 3. Primitive parts, colliders removed, each root's mesh bounds <= 0.3 m. Positions avoid the baked goblets, plates, tome and note boxes by construction (mirrored lateral/along pairs).

## Needs a real build to see or judge
All visuals and timings: silhouette readability through the window glass (draw order against the glass material), figure scale and the 20 degree vanish, bat size and speed, book/knife physics, whether the throwables sit flush on the table art (top heights taken from `tools/castle_furniture.py`: hall 0.775, dining 0.792, library 4.04, sideboard 1.10, prep table 0.9), the nailed planks on the cupboards, flame gutter look, false candle lean angle. `ThrowNearbyProp` on a Throwable has not been run; bounds satisfy its rule on paper (largest 0.26 m).
