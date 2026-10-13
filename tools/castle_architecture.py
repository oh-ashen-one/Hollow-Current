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

def column():
    b = Builder("Column")
    b.box((0, 0, .09), (.9, .9, .18), S)
    prof = [(0, .18), (.44, .18), (.44, .26), (.36, .32), (.31, .36), (.31, 2.72), (.37, 2.78), (.52, 2.92), (.55, 3.02), (0, 3.02)]
    b.lathe((0, 0, 0), prof, 12, S, cap=False)
    for k in range(4):
        a = math.radians(45 + 90 * k)
        b.lathe((.37 * math.cos(a), .37 * math.sin(a), 0), [(.07, .36), (.07, 2.72)], 6, S)
    b.box((0, 0, 3.11), (1.0, 1.0, .18), S)
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
    """Tracery frame, 3.2 m across, 0.4 m thick, in the XZ plane facing +-Y. Origin: centre of the circle."""
    b = Builder("RoseWindow")
    with b.xf(Rx(90)):
        b.lathe((0, 0, 0), [(1.45, -.2), (1.6, -.2), (1.6, .2), (1.45, .2)], 24, S, closed_profile=True, smooth=False)
        b.lathe((0, 0, 0), [(0, -.12), (.32, -.12), (.32, .12), (0, .12)], 12, S, cap=False, smooth=False)
        for i in range(12):
            with b.xf(Rz(i * 30)):
                b.box((.875, 0, 0), (1.17, .08, .24), S)
        for i in range(6):
            a = math.radians(30 + 60 * i)
            b.ring_loop((.8 * math.cos(a), .8 * math.sin(a), 0), (1, 0, 0), (0, 1, 0), .27, .035, 10, 4, S)
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

def balcony_railing():
    """2.0 m stone balustrade, 1.0 m high. Origin: bottom centre; front +Y."""
    b = Builder("BalconyRailing")
    b.box((0, 0, .08), (2.0, .3, .16), S)
    b.box((0, 0, .95), (2.0, .3, .1), S)
    for sx in (-1, 1):
        b.box((sx * .92, 0, .5), (.16, .26, 1.0), S)
    for i in range(9):
        x = -.72 + i * .18
        b.lathe((x, 0, 0), [(.04, .16), (.06, .35), (.04, .55), (.05, .75), (.04, .9)], 6, S, cap=True)
    return b

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

def drawbridge():
    """Lowered drawbridge, 3.0 x 6.0 m deck, top at z=0, hinge edge at y=0 (extends to +Y). Two chains rise from the
    far corners to anchors 4.4 m up over the gate (y=0.2)."""
    b = Builder("Drawbridge")
    n = 20; pl = 6.0 / n
    for i in range(n):
        b.box((0, (i + .5) * pl, -.125), (3.0, pl - .02, .25), OAK)
    for sx in (-1, 1):
        b.box((sx * 1.4, 3.0, .06), (.22, 6.0, .12), OAK)
    for y in (.5, 2.0, 4.0, 5.6):
        b.box((0, y, .005), (3.0, .14, .01), IRON)
    chain_pitch = .27
    for sx in (-1, 1):
        a = Vector((sx * 1.45, 5.9, .12)); c = Vector((sx * 1.45, .2, 4.4))
        d = c - a; L = d.length; dn = d.normalized()
        side = Vector((1, 0, 0)); up = dn.cross(side).normalized(); side2 = up.cross(dn).normalized()
        k = int(L / chain_pitch)
        for i in range(k):
            ctr = a + dn * (chain_pitch * (i + .5))
            b.ring_loop(ctr, dn, side2 if i % 2 == 0 else up, .13, .02, 6, 3, IRON, ry=.07)
    b.notes.update(deck=[3.0, 6.0])
    return b

BUILDERS = [
    ("Wall_2m", lambda: wall("Wall_2m", 2.0, False), 200),
    ("Wall_4m", lambda: wall("Wall_4m", 4.0, False), 200),
    ("Wall_2m_Window", lambda: wall("Wall_2m_Window", 2.0, True), 200),
    ("Wall_4m_Window", lambda: wall("Wall_4m_Window", 4.0, True), 200),
    ("Column", column, 800),
    ("PointedArch", pointed_arch, 600),
    ("VaultRib", vault_rib, 600),
    ("RoseWindow", rose_window, 1500),
    ("FloorTile", floor_tile, 100),
    ("StairFlight", stair_flight, 600),
    ("BalconyRailing", balcony_railing, 700),
    ("GreatDoor", great_door, 1200),
    ("ServiceDoor", service_door, 800),
    ("Door", std_door, 800),
    ("CoffinNiche", coffin_niche, 800),
    ("StoneBridge", stone_bridge, 800),
    ("Drawbridge", drawbridge, 3000),
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
