# HANDOFF: hc/dressing (round 2, pieces 1+3 combined)

Branch `hc/dressing` (worktree `/Users/midir/hollow-current-haunted-castle/.wt/dressing`), based on `haunted-castle` 140b47a. Unity was not launched; verification is `python3 tools/check_source.py` (PASS, with `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll`). Nothing here has been seen in an engine frame.

## Placed per room (all procedural or existing Resources/Castle art)
- Great hall: GiantSkeleton x1 (scale 0.88, yaw -90, spine along x at z 25.6, table and chairs moved under its ribs; throne moved to the west wall z 18.2), 14 floating candelabras (shared mesh, 70 billboard flames, 0 point lights, scale 1.6, y 2.7-6.4), 8 FloatingBook (art mesh x1.8, Floater, y 2.7-5.9), 6 Banner (4 on the side walls between clerestory windows, 2 flanking the rose window), rune ring now 24 glyphs at 3 m (factory clamps CastleRun's request of 28; glyph alpha/brightness floor raised so idle glyphs show; ring size 0.51 m).
- Chapel: 44 floating candles at y 1.5-5.0 (1 lit, billboard flame, no light), stone font with glossy red liquid disc at (7.55,0,6.95), 4 fallen pews (rolled onto their sides, with collision proxies), lit brazier as the Breaker interactable at (12.6,0,11.2), painted-over sun window behind the altar (procedural pointed-arch texture with black ink blot, stone jambs and sill).
- Crypt: 30 hand-root strands (6 shared mesh variants, 2 materials, crown cone to the ceiling, tops 2.6-3.0 m above the floor, hands end 0.4-2.3 m above floor, slow sway via Floater), 13 coffin niches (CoffinNiche art, west 3, south 5, north 5; the wall gets a real 1.1 x 0.8 cut via Wall() ops, block hidden behind the wall, coffin protrudes 0.25-0.55 m with a collision proxy), 36 violet glyph quads on the sarcophagus (shared unlit material pulsed by RunePulse), blood channel rebuilt as a 0.3 m deep trench (bed -3.5, liquid surface -3.3 = 0.1 below floor, 8 trench-wall strips), glossy `bloodpool` material (also made `blood`, the moat, glossy 0.9).
- Moons: 6 sprite moons (pale + red) placed on the rays from the capture eyes (0,1.64,14.7) and (7,1.64,6.5) through the rose window and the two chapel east windows (the skybox moon azimuths -34/+42 cannot be framed by those windows).

## Lights
No new `Light` components. Point lights are exactly the CastleLayout.Candles list (51 defs; two table candles now follow `HallTableZ`, one crypt candle moved from (-4.6,20) to (-4.5,19) to clear the niche coffins) plus the window spot lights and the two directional moons.

## Fixed
- Floater.UpdateFlames threw a NullReferenceException every frame after `Floater.SetFlameTarget(null)` while flames were still bent. It now remembers the last target position, flames ease upright at relaxSpeed 1/s (about 1 s from full lean), no target dereference.

## Not done / assumptions (please check in-engine)
- Split columns (S1): no wall segments are placed there; each ColumnGreat is one Prop. The "split" look in S1 is the art's shaft plus four engaged colonettes lit from one side at a wide-angle edge. Fix belongs in `tools/castle_architecture.py::column` (colonette lathe/normals), not in the layout. Left unchanged.
- The RGB 0,0,0 slab in S2 is assumed to be World.cs's fallback "Fallback / chapel brazier" black box (no Breaker existed in CastleWorld). A real brazier Breaker is now placed, so the fallback is not created. If the slab persists it is something else.
- ImportYaw / FBX axes are unverified: skeleton head direction (-x) and book pages-up assume Blender Y->Unity Z and Z->Y.
- Hand-root "2.6-3.0 m" is read as the strand-top height; hands hang below that.
- No ground fog, no stone/wet floor textures, no S3 lift: not in this brief.
- Rose window art is still a square quad (art piece).
- Bake receipts hash every .cs: Castle must be re-baked.

## Requests for integrator
- `CastleRun.Start` places 6 more floating candelabras at 2.7 m and passes 28 runes; change its rune count to 24 (the factory clamps anyway). CastleRun's bat flock and ring are untouched.
- Optional: set `CastleRun`-side `Floater.SetFlameTarget(null)` usage is now safe.
