#!/usr/bin/env python3
"""Haunted Castle furniture and set-dressing named by CastleLayout.ArtNames. Headless, idempotent:

    /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/castle_furniture.py [-- Name ...]

Exports Assets/HollowCurrent/Resources/Castle/<Name>.fbx and docs/haunted-castle/art/manifest_furniture.json.
Asset space: X right, Y depth (front = +Y -> Unity +Z), Z up, metres, origin at the base centre.
"""
import sys, math
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from castle_lib import *
from castle_gothic import turned_candlestick
from mathutils import Matrix

S, BS, OAK, BRASS, IRON, OX, SIL, BONE, PAPER, FLAME = (
    "Castle_Stone", "Castle_BlackStone", "Castle_Oak", "Castle_Brass", "Castle_Iron", "Castle_Oxblood",
    "Castle_Silver", "Castle_Bone", "Castle_Paper", "Castle_Flame")

def only():
    a = sys.argv
    return a[a.index("--") + 1:] if "--" in a else []

def winch():
    """Drawbridge winch: oak frame, chain drum with iron flanges, ratchet wheel and crank. 1.0 x 1.6 x 0.8. Front +Y."""
    b = Builder("Winch")
    for sx in (-1, 1):
        b.box((sx * .42, 0, .05), (.1, .8, .1), OAK)
        for sy in (-1, 1):
            b.tube([(sx * .42, sy * .3, .08), (sx * .42, sy * .08, 1.4)], .045, 6, OAK)
    b.box((0, 0, 1.45), (1.0, .14, .1), OAK)
    b.box((0, 0, 1.55), (.9, .12, .08), OAK)
    with b.xf(T(0, 0, 1.25) @ Ry(90)):
        b.lathe((0, 0, -.38), [(.16, 0), (.16, .76)], 10, OAK)
        for z in (-.3, .3):
            b.lathe((0, 0, z), [(.25, -.025), (.25, .025)], 12, IRON)
        b.lathe((0, 0, .44), [(.2, 0), (.2, .05)], 10, IRON)
    b.tube([(.5, 0, 1.25), (.5, .22, 1.25), (.62, .22, 1.25)], .02, 5, IRON)
    b.tube([(.6, .22, 1.25), (.6, .22, 1.5)], .025, 5, BRASS)
    for k in range(10):
        a = math.radians(k * 36)
        b.tube([(0, 0, 1.0), (0, .0, 1.0)], .0, 3, IRON) if False else None
    b.box((.0, .18, .35), (.3, .1, .7), OAK)
    return b

def lantern_cart():
    """Small two-wheeled hand cart with a hooded lantern on a post. 1.8 x 1.3 x ~1.0. Front (handles) +Y."""
    b = Builder("LanternCart")
    b.box((0, -.05, .55), (1.4, .7, .1), OAK)
    for sx in (-1, 1):
        b.box((sx * .7, -.05, .7), (.05, .7, .25), OAK)
    for sy in (-1, 1):
        b.box((0, -.05 + sy * .35, .7), (1.4, .05, .25), OAK)
    for sx in (-1, 1):
        wx = sx * .83
        b.ring_loop((wx, -.05, .38), (0, 1, 0), (0, 0, 1), .38, .03, 14, 4, OAK)
        with b.xf(T(wx, -.05, .38) @ Ry(90)):
            b.lathe((0, 0, -.04), [(.07, 0), (.07, .08)], 8, IRON)
            for k in range(6):
                with b.xf(Rz(k * 30)):
                    b.box((0, .0, 0), (.025, .7, .03), OAK)
    with b.xf(Ry(90)):
        b.lathe((0, 0, -.83), [(.025, 0), (.025, 1.66)], 6, IRON)
    for sx in (-1, 1):
        b.tube([(sx * .55, .3, .6), (sx * .55, .62, .64)], .03, 5, OAK)
    b.box((0, .62, .64), (1.1, .05, .05), OAK)
    b.tube([(-.55, -.38, .6), (-.55, -.38, 1.3)], .03, 6, OAK)
    b.box((-.55, -.2, 1.28), (.04, .36, .04), OAK)
    b.lathe((-.55, -.05, .98), [(.0, .0), (.1, .0), (.12, .04), (.12, .2), (.05, .26), (0, .3)], 6, IRON, cap=False)
    return b

def chapel_bell():
    """Bell on an oak A-frame yoke. 1.2 x 2.0 x 0.9. Bell axis vertical, swings about X. Front +Y."""
    b = Builder("ChapelBell")
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.tube([(sx * .58, sy * .38, 0), (sx * .56, 0, 1.9)], .05, 6, OAK)
    b.tube([(-.6, 0, 1.85), (.6, 0, 1.85)], .065, 6, OAK)
    b.tube([(-.56, 0, 1.78), (.56, 0, 1.78)], .035, 5, IRON)
    b.lathe((0, 0, 0), [(.0, 1.73), (.12, 1.72), (.15, 1.62), (.22, 1.3), (.3, 1.05), (.4, .85), (.43, .8), (.39, .78), (.33, .86), (.22, 1.08), (.12, 1.45), (0, 1.55)], 12, BRASS, cap=False)
    b.tube([(0, 0, 1.55), (0, 0, .78), (0, 0, .72)], [.02, .02, .05], 6, IRON)
    b.box((0, 0, 1.76), (.3, .12, .06), IRON)
    return b

def table(name, length, depth, top_z=.78):
    b = Builder(name)
    b.box((0, 0, top_z - .04), (length, depth, .08), OAK)
    for sy in (-1, 1):
        b.box((0, sy * (depth / 2 - .08), top_z - .12), (length - .4, .05, .14), OAK)
    for sx in (-1, 1):
        x = sx * (length / 2 - .5)
        b.box((x, 0, (top_z - .08) / 2), (.12, depth - .3, top_z - .08), OAK)
        b.box((x, 0, .06), (.3, depth - .1, .12), OAK)
    b.box((0, 0, .2), (length - 1.0, .08, .08), OAK)
    b.box((0, 0, top_z + .005), (length - .6, depth * .45, .012), OX)
    return b

def long_table():
    """Great-hall table 6.6 x 1.3 m with an oxblood runner, brass candlesticks and silver goblets (no chairs). Front +Y."""
    b = table("LongTable", 6.6, 1.3, .76)
    for x in (-2.0, 0, 2.0):
        b.lathe((x, 0, .77), [(.05, 0), (.05, .02), (.02, .05), (.02, .18), (.04, .2), (0, .2)], 6, BRASS, cap=False)
        b.lathe((x, 0, .97), [(.018, 0), (.018, .13), (0, .13)], 6, BONE, cap=False)
    for x in (-2.9, -1.5, -.5, .6, 1.4, 2.7):
        for sy in (-1, 1):
            b.lathe((x, sy * .4, .77), [(0, 0), (.03, 0), (.012, .04), (.012, .08), (.045, .13), (.04, .14), (.03, .12), (0, .11)], 6, SIL, cap=False)
    return b

def dining_table():
    """Dining-hall table 5.6 x 1.5 m with plates, a candelabra stub and cloth runner. Long axis X, front +Y."""
    b = table("DiningTable", 5.6, 1.5, .78)
    for x in (-1.6, 0, 1.6):                        # round 13: turned brass candlesticks with 30 cm tapers (unlit: the table is baked, the candelabra beside it burns)
        turned_candlestick(b, (x, 0, .79), 1.0, .30, seg=12, flame=False)
    for x in (-2.0, -1.0, 0.0, 1.0, 2.0):
        for sy in (-1, 1):
            b.lathe((x, sy * .5, .79), [(0, 0), (.14, 0), (.16, .015), (.0, .015)], 10, SIL, cap=False)
    return b

def high_back_chair():
    """Carved oak chair, 0.54 x 0.54 x 1.3. Seat faces +Y."""
    b = Builder("HighBackChair")
    hb_chair(b, (0, 0, 0), 0)
    return b

def throne():
    """High-backed throne, 1.0 x 1.9 x 0.9: black stone base, oak seat with oxblood cushion, pointed back with brass finials."""
    b = Builder("Throne")
    b.box((0, 0, .1), (1.0, .9, .2), BS)
    b.box((0, .0, .33), (.82, .72, .26), OAK)
    b.box((0, .06, .49), (.7, .6, .1), OX)
    for sx in (-1, 1):
        b.box((sx * .44, .05, .55), (.12, .7, .08), OAK)
        b.box((sx * .44, .05, .3), (.1, .6, .3), OAK)
        b.lathe((sx * .44, .35, .59), [(0, 0), (.05, 0), (.04, .07), (0, .1)], 6, BRASS)
    b.poly([(-.45, .2), (.45, .2), (.45, 1.25), (.3, 1.55), (0, 1.9), (-.3, 1.55), (-.45, 1.25)], -.34, -.2, OAK, 'XZ')
    b.poly([(-.35, .5), (.35, .5), (.35, 1.2), (0, 1.7), (-.35, 1.2)], -.2, -.17, OX, 'XZ')
    for sx in (-1, 1):
        b.lathe((sx * .43, -.27, 1.3), [(.035, 0), (.035, .25), (0, .4)], 6, BRASS, cap=False)
    b.lathe((0, -.27, 1.9), [(0, 0), (.05, 0), (0, .0)], 4, BRASS, cap=False) if False else None
    return b

def fireplace():
    """Stone fireplace with carved mantel and a dying fire, 2.6 x 2.6 x 0.9. Firebox opening 1.3 x 1.1. Front +Y."""
    b = Builder("Fireplace")
    for sx in (-1, 1):
        b.box((sx * .95, 0, .65), (.7, .9, 1.3), S)
        b.box((sx * .95, .02, .08), (.78, .94, .16), S)
    b.box((0, 0, 1.2), (2.6, .9, .2), S)
    b.box((0, .1, 1.38), (2.6, .9 + .1, .16), S)            # mantel shelf
    b.box((0, -.12, 2.0), (1.9, .55, 1.25), S)               # chimney breast
    b.box((0, -.12, 2.55), (1.6, .45, .1), S)
    b.box((0, -.3, .55), (1.3, .3, 1.1), BS)                 # firebox back
    b.box((0, .0, .02), (1.3, .8, .04), BS)
    b.tube([(-.45, .0, .13), (.4, -.05, .17)], .07, 6, OAK)
    b.tube([(-.35, .1, .12), (.3, -.12, .2)], .06, 6, OAK)
    b.box((0, -.02, .12), (.9, .3, .05), FLAME)
    for sx in (-1, 1):
        b.lathe((sx * .5, .22, 0), [(.03, 0), (.03, .3), (.05, .36)], 6, IRON, cap=False)
    return b

def sideboard():
    """Oak sideboard with three panelled doors and brass pulls, 2.6 x 1.1 x 0.6. Front +Y."""
    b = Builder("Sideboard")
    b.box((0, 0, .08), (2.5, .5, .16), OAK)
    b.box((0, 0, .62), (2.5, .52, .9), OAK)
    b.box((0, .02, 1.07), (2.6, .6, .06), OAK)
    for i in (-1, 0, 1):
        x = i * .8
        b.box((x, .27, .62), (.72, .03, .72), OAK)
        b.box((x, .29, .62), (.5, .02, .5), OAK)
        b.box((x + (.28 if i != 1 else -.28), .31, .62), (.04, .03, .12), BRASS)
    return b

def kitchen_hearth():
    """Great kitchen hearth with iron crane and spit, 2.8 x 2.4 x 1.0. Opening 2.0 x 1.2. Front +Y."""
    b = Builder("KitchenHearth")
    for sx in (-1, 1):
        b.box((sx * 1.1, 0, .7), (.6, 1.0, 1.4), S)
        b.box((sx * 1.1, 0, .08), (.8, 1.04, .16), S)
    b.box((0, .0, 1.5), (2.8, 1.0, .2), OAK)
    b.poly([(-1.4, 1.6), (1.4, 1.6), (.5, 2.4), (-.5, 2.4)], -.5, .5, S, 'XZ')
    b.box((0, -.3, .6), (1.6, .4, 1.2), BS)
    b.box((0, .0, .03), (1.6, .9, .06), BS)
    b.tube([(.65, .1, .1), (.65, .1, 1.35), (.0, .1, 1.35)], .035, 5, IRON)
    b.tube([(.0, .1, 1.35), (.0, .1, 1.0)], .015, 4, IRON)
    b.tube([(-.7, .1, .5), (.7, .1, .5)], .02, 5, IRON)
    b.box((0, -.05, .1), (1.0, .4, .06), FLAME)
    b.tube([(-.5, .0, .15), (.5, .0, .15)], .09, 6, OAK)
    return b

def butcher_block():
    """Oak chopping-block table with a cleaver stuck in it, 1.5 x 0.9 x 1.0. Front +Y."""
    b = Builder("ButcherBlock")
    b.box((0, 0, .71), (1.4, 1.0, .22), OAK)
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.box((sx * .6, sy * .38, .3), (.14, .14, .6), OAK)
    b.box((0, 0, .2), (1.2, .7, .05), OAK)
    b.box((.25, .1, .87), (.22, .03, .14), IRON)
    b.box((.38, .1, .9), (.18, .03, .035), OAK)
    return b

def cauldron():
    """Iron cauldron on three legs with blood, 1.0 x 0.9 x 1.0. Front +Y."""
    b = Builder("Cauldron")
    b.lathe((0, 0, 0), [(0, .2), (.3, .2), (.44, .34), (.5, .55), (.48, .76), (.52, .8), (.5, .84), (.44, .8), (.45, .6), (.4, .4), (0, .36)], 10, IRON, cap=False)
    b.lathe((0, 0, 0), [(0, .72), (.44, .72)], 10, OX, cap=False)
    for k in range(3):
        a = math.radians(90 + k * 120)
        b.tube([(.3 * math.cos(a), .3 * math.sin(a), .3), (.38 * math.cos(a), .38 * math.sin(a), 0)], .04, 5, IRON)
    b.tube([(-.5, 0, .82), (-.3, 0, .98), (0, 0, 1.0), (.3, 0, .98), (.5, 0, .82)], .015, 5, IRON, cap=True)
    return b

def reading_table():
    """Oak reading table with turned legs, an open tome and a candlestick, 2.0 x 0.85 x 1.0. Front +Y."""
    b = Builder("ReadingTable")
    b.box((0, 0, .8), (2.0, 1.0, .08), OAK)
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.lathe((sx * .85, sy * .4, 0), [(.04, 0), (.06, .1), (.04, .25), (.07, .45), (.04, .6), (.05, .76)], 6, OAK, cap=True)
    b.box((0, 0, .5), (1.7, .06, .05), OAK) if False else None
    for sx in (-1, 1):
        b.box((sx * .85, 0, .3), (.05, .8, .05), OAK)
    with b.xf(about((.25, .05, .85), Rx(6))):
        b.box((.25 - .105, .05, .865), (.21, .28, .02), OX); b.box((.25 + .105, .05, .865), (.21, .28, .02), OX)
        b.box((.25 - .1, .05, .88), (.19, .26, .02), PAPER); b.box((.25 + .1, .05, .88), (.19, .26, .02), PAPER)
    turned_candlestick(b, (-.6, .1, .84), 1.0, .30, seg=12, flame=False)
    return b

def lectern():
    """Oak lectern with a sloped reading desk and an open book, 0.7 x 1.2 x 0.6. Front +Y."""
    b = Builder("Lectern")
    b.lathe((0, 0, 0), [(0, 0), (.25, 0), (.25, .06), (.1, .12), (.07, .3), (.09, .6), (.07, .9), (.1, .95), (0, .95)], 8, OAK)
    with b.xf(about((0, 0, 1.0), Rx(-22))):
        b.box((0, 0, 1.0), (.68, .5, .06), OAK)
        b.box((0, -.03, 1.045), (.46, .34, .02), PAPER)
        b.box((0, .22, 1.05), (.68, .04, .06), OAK)
    b.lathe((0, 0, 0), [(0, 1.12), (.06, 1.12), (.04, 1.2), (0, 1.2)], 6, BRASS) if False else None
    return b

def canopy_bed():
    """Four-poster bed with oxblood drapes, linen and a carved headboard, 2.4 x 2.8 x 2.6. Head at -Y, foot (front) +Y."""
    b = Builder("CanopyBed")
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.lathe((sx * 1.1, sy * 1.2, 0), [(.07, 0), (.1, .12), (.06, .5), (.08, 1.2), (.05, 2.4), (.07, 2.6), (.04, 2.8)], 8, OAK, cap=True)
    b.box((0, 0, .32), (2.2, 2.4, .3), OAK)
    b.box((0, .0, .56), (2.0, 2.2, .24), PAPER)
    for sx in (-.5, .5):
        b.box((sx, -.85, .72), (.7, .4, .14), PAPER)
    b.box((0, .35, .7), (2.02, 1.3, .08), OX)
    b.poly([(-1.1, .3), (1.1, .3), (1.1, 1.5), (.55, 1.9), (0, 2.2), (-.55, 1.9), (-1.1, 1.5)], -1.3, -1.2, OAK, 'XZ')
    b.box((0, .0, 2.7), (2.4, 2.6, .1), OAK)
    for sx in (-1, 1):
        b.box((sx * 1.18, -.4, 1.75), (.04, 1.5, 1.8), OX)
        b.box((sx * 1.18, .95, 1.9), (.04, .5, 1.4), OX)
    b.box((0, -1.22, 2.0), (2.1, .04, .9), OX)
    return b

def telescope():
    """Brass telescope on a tripod, eyepiece on the front (+Y) side, objective pointing up and back (-Y), 1.0 x 2.2 x 2.4."""
    b = Builder("Telescope")
    for k in range(3):
        a = math.radians(90 + k * 120)
        b.tube([(0, 0, 1.1), (.46 * math.cos(a), .46 * math.sin(a), 0)], .035, 6, OAK)
    b.lathe((0, 0, 1.05), [(.1, 0), (.1, .1), (.06, .16)], 8, BRASS)
    with b.xf(T(0, 0, 1.15) @ Rx(60)):
        b.lathe((0, 0, -.6), [(.04, 0), (.05, .1), (.07, .3), (.09, .8), (.11, 1.6), (.13, 2.2), (.14, 2.5), (.11, 2.8)], 10, BRASS, cap=False)
        b.lathe((0, 0, -.7), [(.03, 0), (.03, .1), (.05, .1)], 6, IRON)
        for z in (.4, 1.2, 2.0):
            b.lathe((0, 0, z), [(.12 + z * .02, -.03), (.13 + z * .02, 0), (.12 + z * .02, .03)], 10, SIL, cap=False)
    return b

def reliquary_box():
    """Iron-bound reliquary casket on a plinth, gabled lid, 1.0 x 1.0 x 0.7 (W x H x D). Claw-score area on the +Y face."""
    b = Builder("ReliquaryBox")
    b.box((0, 0, .15), (1.0, .7, .3), S)
    b.box((0, 0, .5), (.9, .6, .4), OAK)
    b.poly([(-.3, .7), (.3, .7), (0, .98)], -.46, .46, OAK, 'YZ')
    for x in (-.3, .3):
        b.box((x, 0, .5), (.06, .64, .42), IRON)
        b.box((x, 0, .78), (.05, .62, .02), IRON)
    b.box((0, .31, .55), (.12, .03, .16), BRASS)
    b.lathe((0, .33, .5), [(.0, 0), (.025, 0), (.0, .03)], 6, BRASS) if False else None
    return b

def pedestal():
    """Stone pedestal with a black dish for a small relic, 0.6 x 1.0 x 0.6."""
    b = Builder("Pedestal")
    b.lathe((0, 0, 0), [(0, 0), (.3, 0), (.3, .08), (.2, .14), (.15, .3), (.15, .8), (.24, .88), (.3, .93), (.3, .97), (0, .97)], 8, S)
    b.lathe((0, 0, 0), [(0, .96), (.24, .96), (.2, .985), (0, .99)], 8, BS, cap=False)
    return b

BUILDERS = [
    # LongTable moved to tools/castle_hall_kit.py (round 13: the 3.3 m trestle table)
    ("Winch", winch, 1500), ("LanternCart", lantern_cart, 2000), ("ChapelBell", chapel_bell, 1500),
    ("DiningTable", dining_table, 5000), ("HighBackChair", high_back_chair, 500), ("Throne", throne, 1000),
    ("Fireplace", fireplace, 1200), ("Sideboard", sideboard, 800), ("KitchenHearth", kitchen_hearth, 1200),
    ("ButcherBlock", butcher_block, 500), ("Cauldron", cauldron, 800), ("ReadingTable", reading_table, 3000),
    ("Lectern", lectern, 600), ("CanopyBed", canopy_bed, 1500), ("Telescope", telescope, 1500),
    ("ReliquaryBox", reliquary_box, 600), ("Pedestal", pedestal, 600),
]

def main():
    wanted = only()
    reset_scene()
    for name, make, budget in BUILDERS:
        if wanted and name not in wanted:
            continue
        export_asset(make(), budget)
    if wanted:   # partial run: merge into the committed manifest instead of replacing it
        import json
        mp = cc.ROOT / "docs/haunted-castle/art/manifest_furniture.json"
        old = json.loads(mp.read_text()) if mp.exists() else {}
        old.update(MANIFEST); MANIFEST.clear(); MANIFEST.update(old)
    write_manifest("furniture")
    print("HC_FURNITURE_PASS", len(MANIFEST))

if __name__ == "__main__":
    main()
