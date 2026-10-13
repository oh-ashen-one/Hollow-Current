# HANDOFF: hc/flames (white flame cards)

**Root cause (by inspection, not yet confirmed in a player log):** the castle is built by `CastleWorld` in the editor and saved into the prepared scene (`World.LoadOrBuild` loads the saved `WorldLayout`). The flame, glow, moon and sarcophagus-rune materials were created with procedural `new Texture2D` textures made non-readable (`Apply(true,true)`). Those textures did not survive scene serialization and reached the player blank (default white, alpha 1), so the additive/alpha shaders drew solid cards. The white squares in r3 S1 are the flame quads and the larger halo quads at full size. The rune ring and bats are built at runtime by CastleRun, which is why they looked right.

**Fix**
- `tools/castle_surreal_textures.py` bakes `Surreal_Flame`, `Surreal_FlameGlow`, `Surreal_Runes`, `Surreal_MoonPale`, `Surreal_MoonRed` PNGs (+ metas: uncompressed, alpha as transparency, clamped) to `Resources/Castle/Textures`. `SurrealProps.LoadSprite` loads them; the procedural versions remain as fallback and are now readable.
- Runtime repair: `CandleFlame.Start` re-assigns the shared runtime flame/glow materials, `WindowMoon.Start` the moon material, `RunePulse.Start` the violet rune material, so a stale or broken scene material cannot matter even before a re-bake.
- Self-check, once per run, in the player log: `HC_FLAME_CHECK scene material: shader=... tex=name WxH format readable=... | runtime material: shader=HollowCurrent/CastleFlame tex=Surreal_Flame 64x128 RGBA32 readable=False | queue=3001`. Expected "runtime" shader `HollowCurrent/CastleFlame` and texture 64x128. If the runtime shader reads `Standard`, the shader lookup failed instead (then say so).

**Integrator:** import the PNGs (Unity creates/keeps the metas), re-run the Castle prepare/bake so the scene stops carrying the broken textures (not required for the repair above), rebuild.
