#!/usr/bin/env python3
"""Round 13 (hc/hall-l) materials: Castle_DressedStone (tileable PBR set, 1.5 m tile), Castle_Wax, the four stained-glass materials.

    python3 tools/castle_hall_textures.py

Reuses the noise toolbox and writers of tools/castle_textures.py (it does NOT call its main(): no existing texture is rewritten). Deterministic,
idempotent, numpy + Pillow only. Writes Resources/Castle/Textures/Castle_DressedStone_{albedo,normal,mask}.png (+ .meta) and Materials/*.mat.

Castle_DressedStone is a fine-tooled pale ashlar: three 0.5 m courses of big blocks with shallow joints, diagonal chisel striations, a few chips
and soot streaks. It is the stone of ribs, columns, tracery and string courses, so they read as worked stone, not as the brick-tiled wall.
Glass: Castle_GlassAmber/Blue/Red go through the unlit CastleEmissive shader (the bake forces emission black on lit materials, so lit emission
would vanish); Castle_GlassInk is a dark, glossy, non-emissive Standard material (the black paint).
"""
import sys
from pathlib import Path
import numpy as np
sys.path.insert(0, str(Path(__file__).resolve().parent))
import castle_common as cc
import castle_textures as ct

def dressed_stone(n, rng, base):
    r = ct.stone(n, rng, base, rows=3, mortar_px=3, bevel=12, polish=0.10, cols=(1, 2, 2), chip_t=0.80, crack_k=210)
    # chisel striations: fine diagonal scratches (periodic), raised in the height, slightly darker in the albedo
    sc = ct.scratches(n, rng, 900, n * .28, angle=np.pi * .27, wide=1)
    sc2 = ct.scratches(n, rng, 500, n * .20, angle=np.pi * .27 + .12, wide=1)
    tool = ct.blur(np.clip(sc + .6 * sc2, 0, 1), 1)
    r["height"] = ct.norm01(r["height"] * .8 + .26 * tool)
    r["albedo"] = np.clip(r["albedo"] * (1 - .10 * tool[..., None]), 0, 1)
    r["ns"] = 7.5
    r["smooth"] = np.clip(r["smooth"] + .06, .1, .5)
    return r

def main():
    n = ct.SIZE
    name = "Castle_DressedStone"
    base, smooth, metal_v, _ = cc.MATERIALS[name]
    rng = np.random.default_rng(1313)
    r = dressed_stone(n, rng, base)
    g = ct.save_set(name, r, n)
    cc.write_standard_mat(cc.MATS / (name + ".mat"), name, base, smooth, metal_v, g["albedo"], g["normal"], mask_guid=g["mask"], bump_scale=1.0)
    print("texture", name, n)
    base, smooth, _, _ = cc.MATERIALS["Castle_Wax"]
    cc.write_standard_mat(cc.MATS / "Castle_Wax.mat", "Castle_Wax", base, smooth, 0.0)
    base, smooth, _, _ = cc.MATERIALS["Castle_GlassInk"]
    cc.write_standard_mat(cc.MATS / "Castle_GlassInk.mat", "Castle_GlassInk", base, smooth, 0.0)
    # unlit HDR stained glass: values chosen so the glass reads in a dark chapel without blooming past the moonlight (the bake cannot touch these)
    for mat, col in (("Castle_GlassAmber", (1.30, 0.80, 0.22)), ("Castle_GlassBlue", (0.30, 0.52, 1.25)), ("Castle_GlassRed", (1.00, 0.13, 0.08)),
                     ("Castle_GlassGreen", (0.20, 0.72, 0.40)), ("Castle_GlassPale", (0.50, 0.58, 0.78))):   # wave FD: green and pale quarries
        ct.write_shader_mat(mat, "CastleEmissive", colors={"_Color": (*col, 1)})
        print("material", mat)
    print("CASTLE_HALL_TEXTURES_PASS")

if __name__ == "__main__":
    main()
