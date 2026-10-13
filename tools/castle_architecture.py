#!/usr/bin/env python3
"""Haunted Castle architecture kit. Headless, idempotent:

    /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/castle_architecture.py [-- Name ...]

Exports Assets/HollowCurrent/Resources/Castle/<Name>.fbx (one FBX per asset, mesh parts only) and
docs/haunted-castle/art/manifest_architecture.json (measured sizes + tri counts).
Dimensions follow docs/haunted-castle/LOOP.md: floors 3.2 m apart, doors 1.5x2.0, great doors 3.0x4.5,
service doors 0.9x2.0. Asset space: X right, Y depth (front = +Y), Z up; pivots noted per asset.
"""
import sys, math
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from castle_lib import *

S, BS, OAK, BRASS, IRON, OX = "Castle_Stone", "Castle_BlackStone", "Castle_Oak", "Castle_Brass", "Castle_Iron", "Castle_Oxblood"
PLANK = "Castle_OakPlank"   # wide weathered boards with nails (round 6: the lowered leaf read as a featureless plane)
H = 3.2  # storey height

def only():
    a = sys.argv
    return a[a.index("--") + 1:] if "--" in a else []

def wall(name, W, window):
    """Origin: bottom centre of the wall, along X, thickness (0.5) along Y. Symmetric front/back."""
    T = 0.5
    b = Builder(name)
    b.box((0, 0, .175), (W, T + .12, .35), S)               # plinth
    b.box((0, 0, H - .09), (W, T + .16, .18), S)            # cornice
    if not window:
        b.box((0, 0, H / 2), (W, T, H), S)
    else:
        hw, sill, spring, R = (.4, .95, 2.05, .8) if W < 3 else (.65, .85, 1.75, 1.3)
        b.box_mm((-W / 2, -T / 2, 0), (-hw, T / 2, H), S)
        b.box_mm((hw, -T / 2, 0), (W / 2, T / 2, H), S)
        b.box_mm((-hw, -T / 2, 0), (hw, T / 2, sill), S)
        curve = arch_curve(hw, spring, R, 6)
        b.poly(curve + [(hw, H), (-hw, H)], -T / 2, T / 2, S, 'XZ', smooth_range=(0, len(curve) - 1))
        b.box((0, 0, sill + .03), (2 * hw + .16, T + .1, .06), S)   # projecting sill
        if W > 3:
            top = spring + arch_rise(hw, R) - .03
            b.box((0, 0, (sill + top) / 2), (.07, .14, top - sill), S)  # mullion
        b.notes.update(window_width=2 * hw, window_sill=sill, window_apex=round(spring + arch_rise(hw, R), 3))
    b.notes["footprint"] = [W, T]
    return b

def column(name="Column", height=3.0, foot=.7, bands=()):
    """Gothic column with capital: square plinth + round shaft with four engaged colonettes + flared capital + abacus.
    Footprint foot x foot, origin floor centre. Column = crypt (3.0 m, .7), ColumnGreat = great hall (8.8 m, .9)."""
    k = foot / .9
    b = Builder(name)
    pl, cap_h, aba = .18 * k + .02, .30 * k, .16 * k
    b.box((0, 0, pl / 2), (foot, foot, pl), S)
    r = .31 * k
    z0, z1 = pl, height - aba - cap_h
    prof = [(0, pl), (.44 * k, pl), (.44 * k, pl + .08 * k), (r + .04, pl + .14), (r, pl + .18), (r, z1)]
    rc = foot * .47
    prof += [(r + .03 * k, z1 + .06 * k), (r + (rc - r) * .7, z1 + .2 * k), (rc, height - aba), (0, height - aba)]
    b.lathe((0, 0, 0), prof, 16, S, cap=False)
    # Colonettes are shallow relief: centred inside the shaft surface (0.9 r) so they overlap the shaft silhouette and
    # protrude only ~0.05 m, with smooth shading. Round 2 fix: at 1.18 r they stood 0.14 m proud and read as separate halves.
    for kk in range(4):
        a_ = math.radians(45 + 90 * kk)
        cr = .05 * k + .02
        b.lathe((r * .9 * math.cos(a_), r * .9 * math.sin(a_), 0), [(cr, pl + .18), (cr, z1 - .02), (cr * .5, z1 + .02)], 8, S)
    for z in bands:
        b.lathe((0, 0, 0), [(r + .03, z - .06), (r + .06, z), (r + .03, z + .06)], 12, S, cap=False)
    b.box((0, 0, height - aba / 2), (foot, foot, aba), S)
    return b

def pointed_arch():
    """Free-standing arch on two piers. Origin: floor, centred. Clear span 3.0, springs at 2.0, apex of
    intrados 4.6, depth 0.5."""
    hw, t, spring, R, n = 1.5, .45, 2.0, 3.0, 6
    inner = arch_curve(hw, spring, R, n); outer = arch_curve(hw, spring, R, n, t)
    pts = [(-hw - t, 0), (-hw, 0)] + inner + [(hw, 0), (hw + t, 0)] + list(reversed(outer))
    b = Builder("PointedArch")
    b.poly(pts, -.25, .25, S, 'XZ', smooth_range=[(2, 2 + len(inner) - 1), (len(inner) + 4, len(pts) - 1)])
    b.notes.update(clear_span=2 * hw, intrados_apex=round(spring + arch_rise(hw, R), 3))
    return b

def vault_rib():
    """Pointed rib, springs from z=0 at x=+-2.0 (4 m span), centred, section 0.22 x 0.3. Origin: spring-line centre."""
    hw, R, t = 2.0, 2.6, .3
    inner = arch_curve(hw, 0, R, 8); outer = arch_curve(hw, 0, R, 8, t)
    pts = inner + list(reversed(outer))
    b = Builder("VaultRib")
    b.poly(pts, -.11, .11, S, 'XZ', smooth_range=[(0, len(inner) - 1), (len(inner), len(pts) - 1)])
    rise = arch_rise(hw, R)
    b.lathe((0, 0, rise + .1), [(0, -.2), (.13, -.15), (.2, -.02), (.13, .08), (0, .12)], 8, S, cap=False)
    b.notes["span"] = 4.0
    return b

def rose_window():
    """Tracery frame, 3.0 m across, 0.5 m deep, in the XZ plane facing +-Y. Origin: bottom centre of the opening
    (circle centre is 1.5 m above the origin)."""
    b = Builder("RoseWindow")
    with b.xf(T(0, 0, 1.5) @ Rx(90)):
        b.lathe((0, 0, 0), [(1.3, -.25), (1.5, -.25), (1.5, .25), (1.3, .25)], 24, S, closed_profile=True, smooth=False)
        b.lathe((0, 0, 0), [(0, -.15), (.3, -.15), (.3, .15), (0, .15)], 12, S, cap=False, smooth=False)
        for i in range(12):
            with b.xf(Rz(i * 30)):
                b.box((.8, 0, 0), (1.05, .09, .26), S)
        for i in range(6):
            a_ = math.radians(30 + 60 * i)
            b.ring_loop((.75 * math.cos(a_), .75 * math.sin(a_), 0), (1, 0, 0), (0, 1, 0), .25, .035, 10, 4, S)
    return b

def floor_tile():
    """2 x 2 m flagstone, top surface at z=0, 0.25 m thick, origin centre of top face."""
    b = Builder("FloorTile")
    b.box((0, 0, -.125), (2, 2, .25), S)
    b.box((0, 0, .005), (1.94, 1.94, .01), S)
    return b

def stair_flight():
    """3.2 m rise in 18 steps (riser 0.178, tread 0.28, run 5.04), 2.0 m wide + 0.3 m parapet cheeks both sides.
    Origin: floor centre of the bottom step's front edge; climbs towards +Y."""
    n = 18; r = H / n; t = .28
    pts = [(0, 0)]
    for i in range(1, n + 1):
        pts += [((i - 1) * t, i * r), (i * t, i * r)]
    pts += [(n * t, 0)]
    pts = [(0, 0), (0, r)] + [p for i in range(1, n + 1) for p in (((i - 1) * t, i * r), (i * t, i * r))][1:] + [(n * t, 0)]
    b = Builder("StairFlight")
    b.poly(pts, -1.0, 1.0, S, 'YZ')
    L = n * t
    for sx in (-1, 1):
        x0, x1 = (1.0, 1.3) if sx > 0 else (-1.3, -1.0)
        b.poly([(0, 0), (L, 0), (L, H + .95), (0, .95)], x0, x1, S, 'YZ')
    b.notes.update(rise=H, steps=n, riser=round(r, 4), tread=t, run=round(L, 3))
    return b

def balustrade():
    """2.0 m stone balustrade section, 1.1 m high, 0.2 m deep. Origin: bottom centre."""
    b = Builder("Balustrade")
    b.box((0, 0, .09), (2.0, .2, .18), S)
    b.box((0, 0, 1.04), (2.0, .2, .12), S)
    for sx in (-1, 1):
        b.box((sx * .93, 0, .56), (.14, .18, 1.12), S)
    for i in range(9):
        x = -.72 + i * .18
        b.lathe((x, 0, 0), [(.04, .18), (.06, .38), (.04, .6), (.05, .82), (.04, .98)], 6, S, cap=True)
    return b

def crypt_rib():
    """8.4 m cambered stone rib between a pair of crypt columns, 0.6 x 0.6. Origin: bottom centre (springs at z=0)."""
    n = 14
    bot = [(-4.2 + 8.4 * i / n, .28 * (1 - (-1 + 2 * i / n) ** 2)) for i in range(n + 1)]
    b = Builder("CryptRib")
    b.poly(bot + [(4.2, .6), (-4.2, .6)], -.3, .3, S, 'XZ', smooth_range=(0, n))
    b.box((0, 0, .26), (.5, .5, .1), S)
    return b

def rafter_beam():
    """11.4 m oak tie-beam with chamfered ends and four iron straps. Origin: bottom centre."""
    b = Builder("RafterBeam")
    b.box((0, 0, .25), (11.4, .5, .5), OAK)
    for x in (-4.5, -1.5, 1.5, 4.5):
        b.box((x, 0, .25), (.12, .54, .54), IRON)
    for sx in (-1, 1):
        b.box((sx * 5.6, 0, .25), (.3, .56, .56), IRON)
    return b

def crypt_bridge():
    """1.4 x 1.2 m flagstone slab bridge, 0.3 thick, worn kerbs. Origin: bottom centre; spans along Y."""
    b = Builder("CryptBridge")
    b.box((0, 0, .11), (1.4, 1.2, .22), S)
    for sx in (-1, 1):
        b.box((sx * .62, 0, .26), (.16, 1.2, .1), S)
    b.box((0, 0, .27), (1.0, 1.0, .06), S)
    return b

def buttress():
    """Stepped stone buttress, 0.9 wide x 6.4 high x 1.2 deep, weathered offsets on both faces (symmetric, so it works
    whichever side faces the wall). Origin: base centre."""
    pts = [(-.6, 0), (.6, 0), (.6, 1.3), (.4, 1.7), (.4, 3.6), (.28, 4.0), (.28, 5.6), (.0, 6.0), (-.28, 5.6), (-.28, 4.0), (-.4, 3.6), (-.4, 1.7), (-.6, 1.3)]
    b = Builder("Buttress")
    b.poly(pts, -.45, .45, S, 'YZ')
    b.lathe((0, 0, 6.0), [(0, 0), (.14, .0), (.1, .25), (0, .4)], 6, S, cap=False)
    return b

def battlement():
    """4.0 m crenellated parapet, 0.8 thick, 1.0 high, four merlons. Origin: bottom centre."""
    b = Builder("Battlement")
    b.box((0, 0, .225), (4.0, .8, .45), S)
    b.box((0, 0, .45), (4.0, .88, .06), S)
    for x in (-1.5, -.5, .5, 1.5):
        b.box((x, 0, .725), (.62, .7, .5), S)
    return b

def gate_arch():
    """Gate surround for the 3.0 x 4.5 opening: 0.4 m piers, lintel and a pointed gable to 5.6 m. 3.8 x 5.6 x 1.0.
    Origin: floor centre of the opening, centred on the wall line."""
    b = Builder("GateArch")
    for sx in (-1, 1):
        b.box((sx * 1.7, 0, 2.25), (.4, 1.0, 4.5), S)
        b.box((sx * 1.7, 0, .15), (.5, 1.0, .3), S)
        b.lathe((sx * 1.7, 0, 4.9), [(.11, 0), (.11, .35), (0, .7)], 6, S, cap=False)
    b.box((0, 0, 4.7), (3.8, 1.0, .4), S)
    b.poly([(-1.9, 4.9), (1.9, 4.9), (.0, 5.6)], -.5, .5, S, 'XZ')
    for sx in (-1, 1):
        b.box((sx * 1.2, .35, 4.35), (.3, .3, .3), S)
    return b

def drawbridge():
    """Lowered drawbridge leaf 3.2 x 6.0 m, 0.25 thick. Origin: hinge line on the top surface, deck extends towards -Y
    (Unity -Z, the layout's lowered pose). Planks across, side beams, iron straps. Chains are the separate asset
    DrawbridgeChains."""
    b = Builder("Drawbridge")
    n = 20; pl = 6.0 / n
    for i in range(n):
        b.box((0, -(i + .5) * pl, -.125), (3.2, pl - .02, .25), PLANK)
    for sx in (-1, 1):
        b.box((sx * 1.5, -3.0, -.125), (.2, 6.0, .25), PLANK)
    for y in (-.5, -2.0, -4.0, -5.6):
        b.box((0, y, -.005), (3.2, .14, .01), IRON)
    for sx in (-1, 1):
        b.box((sx * 1.5, -5.9, -.125), (.24, .16, .27), IRON)
    return b

def drawbridge_chains():
    """Pair of chains from the far corners of the lowered leaf (y=-5.9) to anchors 4.4 m up at the gate (y=-0.3).
    Static: they do not follow the leaf when it is raised. Origin: hinge line, top surface."""
    b = Builder("DrawbridgeChains")
    for sx in (-1, 1):
        a = Vector((sx * 1.5, -5.9, .0)); c = Vector((sx * 1.5, -.3, 4.4))
        d = c - a; L = d.length; dn = d.normalized()
        side = Vector((1, 0, 0)); up = dn.cross(side).normalized(); side2 = up.cross(dn).normalized()
        k = int(L / .27)
        for i in range(k):
            ctr = a + dn * (.27 * (i + .5))
            b.ring_loop(ctr, dn, side2 if i % 2 == 0 else up, .13, .02, 6, 3, IRON, ry=.07)
    return b

def door_frame(name, ow, oh, jamb, lint, top, depth):
    """Stone frame around an ow x oh opening: two jambs, lintel and a low pointed gable to `top`. Origin: floor centre."""
    b = Builder(name)
    W = ow + 2 * jamb
    for sx in (-1, 1):
        b.box((sx * (ow / 2 + jamb / 2), 0, oh / 2), (jamb, depth, oh), S)
        b.box((sx * (ow / 2 + jamb / 2), 0, .08), (jamb + .06, depth + .04, .16), S)
    b.box((0, 0, oh + lint / 2), (W, depth, lint), S)
    b.poly([(-W / 2, oh + lint), (W / 2, oh + lint), (0, top)], -depth / 2, depth / 2, S, 'XZ')
    b.notes.update(opening=[ow, oh])
    return b

def door_leaf(name, w, h, t, straps, ring_r, portrait=False):
    """One door leaf. Origin: hinge edge at floor level; the leaf extends +X, thickness centred on Y."""
    b = Builder(name)
    b.box((w / 2, 0, h / 2), (w, t, h), OAK)
    for z in straps:
        b.box((w / 2, 0, z), (w - .04, t + .03, .15 if h > 3 else .09), IRON)
    for sy in (-1, 1):
        rc = (w - (.2 if h > 3 else .15), sy * (t / 2 + .03), h * .46 - ring_r)
        with b.xf(about(rc, Rx(90))):
            b.ring_loop(rc, (1, 0, 0), (0, 1, 0), ring_r, .022 if h < 3 else .03, 10, 4, BRASS)
        b.box((rc[0], sy * (t / 2 + .015), rc[2] + ring_r + .04), (.1 if h < 3 else .15, .03, .1 if h < 3 else .15), BRASS)
    if portrait:
        b.part("Canvas", (w / 2, t / 2 + .012, h * .55)); b.use("Canvas")
        cw, ch = w * .7, h * .62
        v = [b._v((w / 2 + x, t / 2 + .012, h * .55 + z)) for x, z in ((-cw / 2, -ch / 2), (cw / 2, -ch / 2), (cw / 2, ch / 2), (-cw / 2, ch / 2))]
        b._f(v, "Castle_Canvas", False, [(0, 0), (1, 0), (1, 1), (0, 1)], closed=False)
        b.use(name)
    return b

def lancet_window(name, W, H, depth=.35, mullions=1):
    """Open stone tracery for a pointed-arch window (no glass: moonlight/sky shows through). W x H x 0.35, origin at the
    sill centre. Jambs, arch band, sill, mullion(s), iron glazing bars, a roundel in the head."""
    b = Builder(name)
    j = .1; hwi = W / 2 - j
    Ri = hwi * 2
    # outer apex relative to spring: use arch_curve with offset j
    outer = arch_curve(hwi, 0, Ri, 8, j); inner = arch_curve(hwi, 0, Ri, 8)
    apex = max(z for x, z in outer)
    spring = H - apex
    inner = [(x, z + spring) for x, z in inner]; outer = [(x, z + spring) for x, z in outer]
    for sx in (-1, 1):
        b.box((sx * (W / 2 - j / 2), 0, spring / 2 + .04), (j, depth, spring - .08 + .0), S)
    b.poly(inner + list(reversed(outer)), -depth / 2, depth / 2, S, 'XZ', smooth_range=[(0, len(inner) - 1), (len(inner), 2 * len(inner) - 1)])
    b.box((0, 0, .04), (W, depth, .08), S)
    top_in = max(z for x, z in inner)
    for i in range(mullions):
        x = 0 if mullions == 1 else (-.5 + (i + .5) / mullions * 1.0 - 0.0) * 2 * hwi * .66
        b.box((x, 0, (.08 + top_in - .25) / 2), (.06, depth * .8, top_in - .25 - .08), S)
    for z in (H * .35, H * .62):
        if z < spring + .1:
            b.box((0, 0, z), (2 * hwi, .04, .025), IRON)
    r = min(.14, hwi * .5)
    if top_in - spring > .4:
        b.ring_loop((0, 0, spring + .12 + r), (1, 0, 0), (0, 0, 1), r, .025, 10, 4, S)
    b.notes["sill_to_apex"] = H
    return b

DOOR_LEAF_SPECS = {"DoorLeaf": (1.5, 2.0, .09, [.4, 1.6], .07), "GreatDoorLeaf": (1.5, 4.5, .12, [.55, 1.7, 2.9, 4.05], .14),
                   "ServiceDoorLeaf": (.9, 2.0, .08, [.35, 1.65], .06), "PortraitDoorLeaf": (.9, 2.0, .1, [.35, 1.65], .06)}

def door(name, W, Ht, frame, ring_r, straps, leaves=1, depth=.5, leaf_t=.12):
    """Frame part + hinged leaf part(s). Opening is W x Ht. Origin: bottom centre of the opening at the
    wall midline; LeafL/LeafR have their pivot (origin) on the hinge edge, hinge axis = vertical (+Z)."""
    b = Builder(name)
    fh = Ht + frame
    b.box(((-W / 2 - frame / 2), 0, Ht / 2 + frame / 2), (frame, depth, fh), S)
    b.box(((W / 2 + frame / 2), 0, Ht / 2 + frame / 2), (frame, depth, fh), S)
    b.box((0, 0, Ht + frame / 2), (W + 2 * frame, depth, frame), S)
    lw = (W / 2 if leaves == 2 else W) - .01
    defs = [("LeafL", -W / 2, 1)] if leaves == 1 else [("LeafL", -W / 2, 1), ("LeafR", W / 2, -1)]
    for pname, hx, sgn in defs:
        b.part(pname, (hx, 0, 0))
        cx = hx + sgn * (lw / 2 + .005)
        b.box((cx, 0, Ht / 2), (lw, leaf_t, Ht - .02), OAK)
        for z in straps:
            b.box((cx, 0, z), (lw - .04, leaf_t + .04, .15 if Ht > 3 else .09), IRON)
        hx2 = hx + sgn * (lw - .22 if leaves == 2 else lw - .16)
        rc = (hx2, leaf_t / 2 + .035, Ht * .47 - ring_r)
        with b.xf(about(rc, Rx(90))):
            b.ring_loop(rc, (1, 0, 0), (0, 1, 0), ring_r, .022 if Ht < 3 else .03, 10, 4, BRASS)
        b.box((hx2, leaf_t / 2 + .02, Ht * .47 + .02), (.1 if Ht < 3 else .16, .03, .1 if Ht < 3 else .16), BRASS)
    b.notes.update(opening=[W, Ht], leaves=leaves)
    return b

def great_door():
    b = door("GreatDoor", 3.0, 4.5, .3, .14, [.55, 1.7, 2.9, 4.05], leaves=2, depth=.5, leaf_t=.14)
    return b

def service_door():
    return door("ServiceDoor", .9, 2.0, .12, .06, [.35, 1.65], leaves=1, depth=.35, leaf_t=.07)

def std_door():
    return door("Door", 1.5, 2.0, .15, .07, [.4, 1.6], leaves=1, depth=.4, leaf_t=.08)

def coffin_niche():
    """Crypt wall niche with a coffin half drawn out onto a slab. Origin: floor centre of the front face of the
    block; the recess runs towards -Y, the coffin and slab project to +Y (front)."""
    b = Builder("CoffinNiche")
    b.box_mm((-1.1, -1.2, 0), (1.1, -1.0, 1.45), S)
    b.box_mm((-1.1, -1.2, 0), (-.55, 0, 1.45), S)
    b.box_mm((.55, -1.2, 0), (1.1, 0, 1.45), S)
    b.box_mm((-.55, -1.2, 0), (.55, 0, .35), S)
    b.box_mm((-.55, -1.2, 1.15), (.55, 0, 1.45), S)
    b.box_mm((-.6, 0, .0), (.6, 1.1, .3), S)                     # slab
    pts = [(-.19, -.85), (-.31, -.25), (-.17, 1.15), (.17, 1.15), (.31, -.25), (.19, -.85)]
    b.poly(pts, .3, .72, OAK, 'XY')
    with b.xf(about((0, 0, .72), Rz(7)) @ T(.02, .22, 0)):
        b.poly(pts, .72, .79, OAK, 'XY')
    b.box((0, .1, .805), (.05, .5, .012), BRASS); b.box((0, .1, .805), (.26, .05, .012), BRASS)
    return b

def stone_bridge():
    """8 m long x 2.6 m wide arched bridge. Origin: centre of the deck top surface; spans along Y."""
    hw, spring, R = 2.6, -3.4, 2.6
    arch = arch_curve(hw, spring, R, 8)
    pts = [(-4, spring), (-4, 0), (4, 0), (4, spring), (hw, spring)] + list(reversed(arch))[1:-1] + [(-hw, spring)]
    b = Builder("StoneBridge")
    b.poly(pts, -1.3, 1.3, S, 'YZ')
    for sx in (-1, 1):
        b.box((sx * 1.15, 0, .4), (.3, 8, .8), S)
        b.box((sx * 1.15, 0, .85), (.38, 8.06, .1), S)
    b.notes.update(span=2 * hw, length=8, width=2.6)
    return b

# ---------------------------------------------------------------- round 5 detail kit (hc/look5)
# Wall-mounted details. Origin: centre of the BACK face on the wall, base at z=0; front (away from the wall) is +Y in asset
# space, which is the art contract's +Z at yaw 0 in Unity.
def sphere_(b, c, r, mat, seg=8):
    b.lathe(c, [(0, -r), (.72 * r, -.7 * r), (r, 0), (.72 * r, .7 * r), (0, r)], seg, mat)

def string_course():
    """Moulded stone band, 4.0 long (X) x 0.24 high x 0.20 deep: cavetto, fillet, ovolo, drip edge. Tiles end to end."""
    b = Builder("StringCourse")
    prof = [(0, 0), (.12, 0), (.12, .03), (.17, .05), (.18, .09), (.14, .12), (.14, .15), (.19, .17), (.20, .21), (.17, .24), (0, .24)]
    b.poly(prof, -2.0, 2.0, S, 'YZ')
    return b

def corbel():
    """Stepped stone bracket, 0.40 wide x 0.55 high x 0.45 deep, top flush with the wall plate. Tapering to a nose."""
    b = Builder("Corbel")
    prof = [(0, .55), (.45, .55), (.45, .45), (.37, .45), (.37, .33), (.27, .33), (.27, .21), (.17, .21), (.17, .09), (.07, 0), (0, 0)]
    b.poly(prof, -.20, .20, S, 'YZ')
    for sx in (-1, 1):                                            # chamfered side blocks
        b.box((sx * .185, .17, .40), (.03, .30, .30), S)
    b.box((0, .225, .565), (.40, .45, .03), S)                    # abacus lip
    return b

def gargoyle_spout():
    """Stone gargoyle on a spout, 0.50 wide x 0.60 high x 1.10 projecting. Open mouth, horns, folded wings; dark throat
    where the water would run. Origin: centre of the back plate."""
    b = Builder("GargoyleSpout")
    b.box((0, .06, .30), (.50, .12, .60), S)                      # back plate
    b.tube([(0, .05, .28), (0, .35, .27), (0, .70, .22)], [.17, .15, .12], 8, S)       # neck / body spout
    # head
    sphere_(b, (0, .78, .26), .17, S, 8)
    b.tube([(0, .88, .27), (0, 1.02, .24), (0, 1.10, .22)], [.12, .10, .08], 8, S)      # snout
    b.box((0, 1.00, .15), (.16, .26, .05), S)                                         # lower jaw
    b.lathe((0, 1.095, .20), [(0, 0), (.055, 0), (.055, -.01), (0, -.01)], 8, BS, cap=False)   # throat
    for sx in (-1, 1):
        sphere_(b, (sx * .105, .92, .33), .04, BS, 6)                                  # eyes
        b.tube([(sx * .08, .88, .38), (sx * .14, .86, .52), (sx * .12, .84, .62)], [.045, .03, 0], 6, S)   # horns
        b.tube([(sx * .12, .90, .36), (sx * .20, .90, .33)], [.035, .02], 5, S)       # brow
        # folded wing: slab hugging the flank
        b.poly([(.05, .0), (.42, .06), (.55, .26), (.30, .52), (.10, .46)], sx * .15 if sx > 0 else -.20, sx * .20 if sx > 0 else -.15, S, 'YZ')
        for i in range(3):                                                             # fangs
            b.tube([(sx * (.05 + .02 * i), 1.07 - .04 * i, .17), (sx * (.05 + .02 * i), 1.07 - .04 * i, .11)], [.014, 0], 4, S)
    b.tube([(0, .60, .10), (0, .75, .08), (0, .82, .10)], [.07, .05, .04], 6, S)      # chest
    return b

def tapestry_panel():
    """Hanging tapestry, 1.40 wide x 2.85 high x 0.06 deep: oxblood cloth with brass-braid border, bat-and-moon emblem,
    fringe, rod and finials. Origin: centre of the back face, base at the fringe."""
    b = Builder("TapestryPanel")
    with b.xf(T(0, 0, -.14)):                                        # fringe bottom sits on z=0
        nx, ny = 6, 16
        def fn(i, j):
            u = i / nx - .5
            y = .028 + .012 * math.sin(j * .8 + i * .9) * (j / ny) + .012
            return (u * 1.30, y, 2.72 - j / ny * 2.5)
        b.grid_cloth(nx, ny, fn, OX)
        for z in (.26, 2.70):                                            # horizontal braid bands
            b.box((0, .05, z), (1.30, .012, .06), BRASS)
        for sx in (-1, 1):
            b.box((sx * .62, .05, 1.48), (.06, .012, 2.44), BRASS)       # vertical braid
        b.poly([(0, 1.95), (.30, 1.55), (0, 1.15), (-.30, 1.55)], .045, .06, BRASS, 'XZ')      # diamond field
        with b.xf(Rx(90)):
            b.lathe((0, 1.55, -.052), [(0, 0), (.14, 0), (.14, -.012), (0, -.012)], 12, BRASS, cap=False)   # moon disc (in the XZ plane)
        b.tube([(-.20, .06, 1.22), (-.08, .062, 1.40), (0, .062, 1.30), (.08, .062, 1.40), (.20, .06, 1.22)], .012, 4, IRON)   # bat wings
        for k in range(-6, 7):                                           # fringe
            b.tube([(k * .1, .05, .26), (k * .1, .052, .14)], [.012, .006], 4, BRASS)
        with b.xf(Ry(90)):
            b.lathe((-2.80, .05, -.72), [(.022, 0), (.022, 1.44)], 8, IRON)
        for sx in (-1, 1):
            sphere_(b, (sx * .72, .05, 2.80), .04, BRASS, 6)
    return b


BUILDERS = [
    ("Wall_2m", lambda: wall("Wall_2m", 2.0, False), 200), ("Wall_4m", lambda: wall("Wall_4m", 4.0, False), 200),
    ("Wall_2m_Window", lambda: wall("Wall_2m_Window", 2.0, True), 200), ("Wall_4m_Window", lambda: wall("Wall_4m_Window", 4.0, True), 200),
    ("Column", lambda: column("Column", 3.0, .7), 800),
    ("ColumnGreat", lambda: column("ColumnGreat", 8.8, .9, bands=(2.9, 5.8)), 1000),
    ("PointedArch", pointed_arch, 600), ("VaultRib", vault_rib, 600), ("CryptRib", crypt_rib, 400),
    ("RoseWindow", rose_window, 1500), ("FloorTile", floor_tile, 100), ("StairFlight", stair_flight, 600),
    ("Balustrade", balustrade, 700), ("RafterBeam", rafter_beam, 300), ("CryptBridge", crypt_bridge, 200),
    ("Buttress", buttress, 300), ("Battlement", battlement, 200), ("GateArch", gate_arch, 400),
    ("Drawbridge", drawbridge, 3000), ("DrawbridgeChains", drawbridge_chains, 3000),
    ("StringCourse", string_course, 400), ("Corbel", corbel, 300), ("GargoyleSpout", gargoyle_spout, 1200), ("TapestryPanel", tapestry_panel, 1500),
    ("DoorFrame", lambda: door_frame("DoorFrame", 1.5, 2.0, .2, .2, 2.6, .8), 400),
    ("GreatDoorFrame", lambda: door_frame("GreatDoorFrame", 3.0, 4.5, .3, .3, 5.2, .9), 400),
    ("ServiceDoorFrame", lambda: door_frame("ServiceDoorFrame", .9, 2.0, .15, .15, 2.3, .8), 400),
    ("DoorLeaf", lambda: door_leaf("DoorLeaf", *DOOR_LEAF_SPECS["DoorLeaf"]), 800),
    ("GreatDoorLeaf", lambda: door_leaf("GreatDoorLeaf", *DOOR_LEAF_SPECS["GreatDoorLeaf"]), 800),
    ("ServiceDoorLeaf", lambda: door_leaf("ServiceDoorLeaf", *DOOR_LEAF_SPECS["ServiceDoorLeaf"]), 800),
    ("PortraitDoorLeaf", lambda: door_leaf("PortraitDoorLeaf", *DOOR_LEAF_SPECS["PortraitDoorLeaf"], portrait=True), 800),
    ("LancetWindow", lambda: lancet_window("LancetWindow", 1.2, 1.6), 600),
    ("TallLancetWindow", lambda: lancet_window("TallLancetWindow", 1.4, 3.5), 600),
    ("ClerestoryWindow", lambda: lancet_window("ClerestoryWindow", 1.2, 1.8), 600),
    ("ObservatoryWindow", lambda: lancet_window("ObservatoryWindow", 1.6, 2.7, mullions=2), 700),
    ("GreatDoor", great_door, 1200), ("ServiceDoor", service_door, 800), ("Door", std_door, 800),
    ("CoffinNiche", coffin_niche, 800), ("StoneBridge", stone_bridge, 800),
]

def spec_for(name):
    def check(info):
        if name.startswith("Wall_"):
            W = float(name.split("_")[1][:-1])
            assert approx(info["size"][0], W, .001) and approx(info["size"][2], H, .001), (name, info["size"])
        if name == "GreatDoor":
            for p in ("LeafL", "LeafR"):
                s = info["parts"][p]["size"]; assert approx(s[2], 4.48, .03) and approx(s[0], 1.49, .02), (p, s)
        if name == "ServiceDoor":
            s = info["parts"]["LeafL"]["size"]; assert approx(s[2], 1.98, .03) and approx(s[0], .89, .02), s
        if name == "Door":
            s = info["parts"]["LeafL"]["size"]; assert approx(s[2], 1.98, .03) and approx(s[0], 1.49, .02), s
        if name in DOOR_LEAF_SPECS:
            w, h = DOOR_LEAF_SPECS[name][:2]
            s = info["parts"][name]["size"]; assert approx(s[0], w, .01) and approx(s[2], h, .01), (name, s)
        if name == "StairFlight":
            assert approx(info["notes"]["rise"], 3.2, 1e-6)
    return check

def main():
    wanted = only()
    reset_scene()
    for name, make, budget in BUILDERS:
        if wanted and name not in wanted:
            continue
        export_asset(make(), budget, spec_for(name))
    write_manifest("architecture")
    print("HC_ARCHITECTURE_PASS", len(MANIFEST))

if __name__ == "__main__":
    main()
