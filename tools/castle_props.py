#!/usr/bin/env python3
"""Haunted Castle props: furniture, the giant skeleton, surreal dressing. Headless, idempotent:

    /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/castle_props.py [-- Name ...]

Exports Assets/HollowCurrent/Resources/Castle/<Name>.fbx and docs/haunted-castle/art/manifest_props.json.
Asset space: X right, Y depth (front = +Y), Z up, metres, origin at the floor centre unless noted.
"""
import sys, math, random, bmesh
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from castle_lib import *
from mathutils import Matrix

S, BS, OAK, BRASS, IRON, OX, SIL, BONE, PAPER, FLAME, CANVAS = (
    "Castle_Stone", "Castle_BlackStone", "Castle_Oak", "Castle_Brass", "Castle_Iron", "Castle_Oxblood",
    "Castle_Silver", "Castle_Bone", "Castle_Paper", "Castle_Flame", "Castle_Canvas")

def only():
    a = sys.argv
    return a[a.index("--") + 1:] if "--" in a else []

def sphere(b, c, r, mat, seg=8, smooth=True):
    b.lathe(c, [(0, -r), (.72 * r, -.7 * r), (r, 0), (.72 * r, .7 * r), (0, r)], seg, mat, smooth=smooth)

def catmull(ctrl, y):
    ys = [c[0] for c in ctrl]
    if y <= ys[0]: return ctrl[0][1]
    if y >= ys[-1]: return ctrl[-1][1]
    i = max(k for k in range(len(ys) - 1) if ys[k] <= y)
    t = (y - ys[i]) / (ys[i + 1] - ys[i])
    p0 = ctrl[max(i - 1, 0)][1]; p1 = ctrl[i][1]; p2 = ctrl[i + 1][1]; p3 = ctrl[min(i + 2, len(ctrl) - 1)][1]
    return .5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3)

def loft(b, c, sections, sides, mat, smooth=True):
    """sections: [(y, rx, rz, zc)] elliptical rings along Y, centred on c."""
    rings = []
    for (y, rx, rz, zc) in sections:
        rings.append([b._v((c[0] + rx * math.cos(j * math.tau / sides), c[1] + y, c[2] + zc + rz * math.sin(j * math.tau / sides)))
                      for j in range(sides)])
    for i in range(len(rings) - 1):
        for j in range(sides):
            j2 = (j + 1) % sides
            b._f([rings[i][j], rings[i][j2], rings[i + 1][j2], rings[i + 1][j]], mat, smooth)
    b._f(list(reversed(rings[0])), mat); b._f(list(rings[-1]), mat)

# ---------------------------------------------------------------- seating / tables
def pew():
    """Oak pew, 2.4 x 1.0 x 0.7 m. Seat faces +Y (front); backrest at the rear; kneeler in front. Origin: floor centre."""
    b = Builder("Pew")
    for sx in (-1, 1):
        x = sx * 1.15
        b.poly([(-.32, 0), (.38, 0), (.38, .46), (.28, .46), (.28, .12), (-.02, .12), (-.02, .5), (-.1, .86), (-.24, 1.0), (-.32, .86)], x - .05, x + .05, OAK, 'YZ')
    b.box((0, 0, .47), (2.3, .5, .06), OAK)
    b.box((0, -.28, .64), (2.3, .05, .22), OAK)
    b.box((0, -.28, .9), (2.3, .05, .2), OAK)
    b.box((0, .33, .1), (2.3, .1, .2), OAK)
    return b

def banquet_table():
    """8 m trestle table, oxblood runner, three brass candlesticks, silver goblets, 14 high-backed chairs
    (6 per side + head and foot). Origin: floor centre; long axis X."""
    b = Builder("BanquetTable")
    b.box((0, 0, .76), (8.0, 1.1, .08), OAK)
    for sy in (-1, 1):
        b.box((0, sy * .5, .66), (7.6, .04, .12), OAK)
    for sx in (-1, 1):
        x = sx * 3.4
        b.box((x, 0, .35), (.1, .9, .7), OAK)
        b.box((x, 0, .05), (.2, 1.0, .1), OAK)
    b.box((0, 0, .18), (6.8, .08, .08), OAK)
    b.box((0, 0, .812), (6.4, .5, .012), OX)
    for x in (-2.2, 0, 2.2):
        b.lathe((x, 0, .818), [(.05, 0), (.05, .02), (.02, .05), (.02, .3), (.04, .32), (0, .32)], 6, BRASS, cap=False)
        b.lathe((x, 0, 1.14), [(.018, 0), (.018, .16), (0, .16)], 6, BONE, cap=False)
    for i, x in enumerate((-2.9, -1.7, -.5, .8, 1.9, 3.0)):
        for sy in (-1, 1):
            b.lathe((x + .15, sy * .38, .818), [(0, 0), (.03, 0), (.012, .04), (.012, .08), (.045, .13), (.04, .14), (.03, .12), (0, .11)], 6, SIL, cap=False)
    for x in (-3.0, -1.8, -.6, .6, 1.8, 3.0):
        hb_chair(b, (x, -.95, 0), 0); hb_chair(b, (x, .95, 0), 180)
    hb_chair(b, (-4.55, 0, 0), -90); hb_chair(b, (4.55, 0, 0), 90)
    return b

# ---------------------------------------------------------------- ritual furniture
def altar():
    """Black altar, 2.4 x 1.1 x 1.1 m, three pointed recesses in the +Y face for blade, hilt, gem
    (niche centres x = -0.65, 0, 0.65; each 0.5 wide, 0.34..0.94 high, 0.35 deep). Origin: floor centre."""
    b = Builder("Altar")
    b.box((0, 0, .08), (2.4, 1.1, .16), BS)
    b.box_mm((-1.1, -.5, .16), (1.1, .05, .98), BS)
    cxs = (-.65, 0, .65)
    edges = [-1.1] + [v for cx in cxs for v in (cx - .25, cx + .25)] + [1.1]
    for k in range(4):
        b.box_mm((edges[2 * k], .05, .16), (edges[2 * k + 1], .4, .98), BS)
    b.box_mm((-1.1, .05, .16), (1.1, .4, .34), BS)
    for cx in cxs:
        curve = [(x + cx, z) for (x, z) in arch_curve(.25, .62, .35, 5)]
        b.poly(curve + [(cx + .25, .98), (cx - .25, .98)], .05, .4, BS, 'XZ', smooth_range=(0, len(curve) - 1))
    b.box((0, 0, 1.04), (2.4, 1.1, .12), BS)
    b.box((0, .555, 1.04), (2.3, .02, .03), BRASS)
    b.box((0, -.05, 1.106), (1.3, .75, .012), OX)
    b.notes.update(niche_x=list(cxs), niche_size=[.5, .6, .35])
    return b

def stone_font():
    b = Builder("StoneFont")
    b.lathe((0, 0, 0), [(0, 0), (.4, 0), (.4, .1), (.25, .18), (.2, .35), (.2, .75), (.3, .82), (.42, .95), (.52, 1.05),
                        (.52, 1.08), (.44, 1.08), (.4, 1.0), (0, .94)], 8, S, smooth=False)
    b.lathe((0, 0, .97), [(.0, 0), (.38, 0)], 8, BS, cap=False) if False else None
    b.lathe((0, 0, 0), [(0, 1.0), (.36, 1.0), (.36, 1.005)], 8, BS, cap=False)
    return b

def sarcophagus():
    """Stone sarcophagus, trough 2.0 x 1.0 x 1.0 m, lid pushed half off towards +X and skewed 9 deg (overall ~2.4 m).
    Origin: floor centre of the trough."""
    b = Builder("Sarcophagus")
    b.box((0, 0, .1), (2.0, 1.0, .2), S)
    for sy in (-1, 1):
        b.box((0, sy * .42, .6), (2.0, .16, .8), S)
    for sx in (-1, 1):
        b.box((sx * .92, 0, .6), (.16, .68, .8), S)
    b.box((0, 0, .205), (1.7, .68, .01), BS)
    with b.xf(about((.4, 0, 1.03), Rz(5))):
        b.box((.4, 0, 1.04), (2.1, 1.0, .12), S)
        b.box((.4, 0, 1.15), (1.9, .8, .1), S)
        b.box((.4, 0, 1.205), (.05, .5, .01), BRASS); b.box((.4, .1, 1.205), (.34, .05, .01), BRASS)
    return b

# ---------------------------------------------------------------- the skeleton
def giant_skeleton():
    """One mesh, 12 m nose-to-tail along Y (head +Y), ~2.9 m high at the spine, ribs reach the floor. Origin: floor under the
    pelvis-ish centre. Bone material only plus black eye sockets."""
    rnd = random.Random(1313)
    b = Builder("GiantSkeleton")
    ctrl = [(-6.0, .12), (-4.5, .32), (-3.0, .8), (-1.5, 1.75), (0.0, 2.55), (1.2, 2.8), (2.5, 2.55), (3.6, 1.95), (4.4, 1.3), (4.9, .95)]
    def sx(y):
        return .9 * math.sin((y + 6) * .55) * max(0.0, (-1.6 - y) / 4.4)
    ys = [-5.9 + i * .22 for i in range(int((4.6 + 5.9) / .22) + 1)]
    for y in ys:
        z = catmull(ctrl, y); dz = (catmull(ctrl, y + .05) - catmull(ctrl, y - .05)) / .1
        pitch = math.degrees(math.atan(dz))
        taper = .17 if -1.8 < y < 3.4 else (.17 - (-1.8 - y) * .028 if y <= -1.8 else .12)
        r = max(.05, taper)
        c = (sx(y), y, z)
        with b.xf(T(*c) @ Rx(pitch - 90)):
            b.lathe((0, 0, 0), [(r, -.1), (r * 1.05, .0), (r, .1)], 6, BONE, smooth=False)
        if -2.2 < y < 3.8:
            h = .5 if -1.0 < y < 3.0 else .3
            b.lathe((c[0], c[1], c[2] + r * .8), [(.04, 0), (0, h)], 4, BONE, smooth=False)
    # ribs
    for k in range(15):
        yk = -1.2 + k * .3
        env = math.sin(math.pi * (k + 1.6) / 17.0)
        z0 = catmull(ctrl, yk) - .03
        Rz_ = min(1.45 * env, (z0 - .06) / 1.84)
        Rx_ = 1.85 * env
        for side in (-1, 1):
            npts = 8 if rnd.random() > .22 else rnd.choice([4, 5, 6])
            pts = []; radii = []
            for i in range(npts):
                phi = .12 + (2.55 - .12) * i / 7
                x = side * (.14 + Rx_ * math.sin(phi)) + rnd.uniform(-.03, .03)
                z = z0 - Rz_ * (1 - math.cos(phi))
                pts.append((x, yk + rnd.uniform(-.03, .03) + .0 + (.14 * i / 7), z))
                radii.append(.075 - .05 * i / 7)
            b.tube(pts, radii, 5, BONE, smooth=True)
    # limbs
    for (yy, zz, leg) in ((2.2, 1.9, "front"), (-2.0, 1.75, "hind")):
        for side in (-1, 1):
            hip = (side * .55, yy, zz)
            knee = (side * 1.15, yy + (.5 if leg == "front" else -.35), .95)
            paw = (side * 1.3, yy + (1.05 if leg == "front" else .5), .09)
            b.tube([hip, knee, paw], [.12, .085, .06], 6, BONE)
            for cl in (-.1, 0, .1):
                b.tube([(paw[0] + cl, paw[1], paw[2]), (paw[0] + cl * 1.6, paw[1] + .3, paw[2] - .02)], [.035, 0], 4, BONE)
    # skull (round 4): ~1.6 m long, elongated predator cranium with brow ridges, ringed eye sockets, nasal openings,
    # cheek arches, a hinged lower jaw hanging open and rows of teeth with two long fangs. Head +Y, resting low.
    # The whole head is yawed 40 deg about its occiput (Blender Rz -40 = Unity yaw -40) so the snout turns toward the viewer's side
    # of the hall and S1 sees a three-quarter head (eye sockets, jaw) whatever the body's placement yaw is.
    sc = (0.0, 5.40, .75)
    with b.xf(T(*sc) @ about((0, -.75, 0), Rz(-40)) @ Rx(-8)):
        loft(b, (0, 0, 0), [(-.80, .10, .12, .30), (-.68, .24, .25, .32), (-.45, .34, .32, .34), (-.22, .32, .27, .28),
                            (.02, .24, .21, .21), (.30, .18, .16, .15), (.58, .13, .12, .11), (.80, .09, .09, .08), (.84, .05, .05, .07)], 12, BONE, smooth=True)
        for sx_ in (-1, 1):
            # brow ridge over each orbit
            b.tube([(sx_ * .12, -.02, .50), (sx_ * .27, -.15, .50), (sx_ * .34, -.32, .42)], [.045, .055, .04], 6, BONE)
            # eye socket: bone rim ring with a dark recess
            ring = [(sx_ * .30, -.14 + .12 * math.cos(a_), .27 + .12 * math.sin(a_)) for a_ in [i * math.tau / 10 for i in range(10)]]
            b.tube(ring, .032, 5, BONE, closed=True)
            b.lathe((sx_ * .27, -.14, .27), [(0, -.07), (.085, -.04), (.11, .03), (.075, .08), (0, .09)], 8, BS, smooth=True)
            # nasal opening
            sphere(b, (sx_ * .06, .60, .215), .04, BS, seg=6)
            # cheek arch
            b.tube([(sx_ * .33, -.38, .14), (sx_ * .36, -.15, .08), (sx_ * .24, .18, .08)], [.035, .04, .03], 5, BONE)
            # upper teeth
            for i in range(6):
                yy = .02 + i * .13
                b.tube([(sx_ * (.13 - i * .010), yy, .03), (sx_ * (.13 - i * .010), yy, -.09 - (.03 if i in (2, 3) else 0))], [.028, 0], 5, BONE)
        # the two long fangs
        for sx_ in (-1, 1):
            b.tube([(sx_ * .10, .50, .04), (sx_ * .10, .52, -.24)], [.034, 0], 5, BONE)
        # lower jaw, hinged behind the cheek and hanging open 24 deg
        with b.xf(about((0, -.42, .10), Rx(-24))):
            loft(b, (0, 0, 0), [(-.46, .22, .08, .02), (-.20, .18, .07, -.01), (.20, .13, .055, -.03), (.62, .08, .045, -.035), (.80, .05, .04, -.035)], 8, BONE, smooth=True)
            for sx_ in (-1, 1):
                b.tube([(sx_ * .20, -.40, .05), (sx_ * .24, -.46, .30)], [.05, .04], 5, BONE)   # ascending ramus to the joint
                for i in range(5):
                    yy = .04 + i * .15
                    b.tube([(sx_ * (.10 - i * .008), yy, .0), (sx_ * (.10 - i * .008), yy, .11)], [.026, 0], 5, BONE)
    return b

# ---------------------------------------------------------------- surreal dressing
def candelabra():
    """Tall five-candle brass candelabrum, 0.5 x 1.7 x 0.5 m, parts: body + Flame_0..4 (Castle_Flame); each flame's pivot
    is at its base so a script can bend it. Origin: base centre."""
    b = Builder("Candelabra")
    b.lathe((0, 0, 0), [(0, 0), (.2, 0), (.2, .04), (.1, .12), (.05, .22), (.07, .3), (.04, .4), (.05, .6), (.035, .75), (.04, .95),
                        (.03, 1.1), (.05, 1.2), (.05, 1.24), (0, 1.24)], 10, BRASS)
    tips = [(0, 0, 1.24)]
    for k in range(4):
        a = math.radians(45 + 90 * k); ca, sa = math.cos(a), math.sin(a)
        pts = [(.03 * ca, .03 * sa, 1.0), (.1 * ca, .1 * sa, .97), (.2 * ca, .2 * sa, 1.04), (.27 * ca, .27 * sa, 1.17)]
        b.tube([(x * .85, y * .85, z) for x, y, z in pts], .018, 5, BRASS)
        b.lathe((.23 * ca, .23 * sa, 1.17), [(0, 0), (.035, 0), (.045, .035), (0, .035)], 8, BRASS)
        tips.append((.23 * ca, .23 * sa, 1.205))
    heights = [.3] + [.22] * 4
    for t, h in zip(tips, heights):
        b.lathe(t, [(.024, 0), (.024, h), (0, h)], 6, BONE, cap=False)
    for i, (t, h) in enumerate(zip(tips, heights)):
        base = (t[0], t[1], t[2] + h)
        b.part("Flame_%d" % i, base)
        b.lathe(base, [(0, 0), (.016, .02), (.021, .055), (.011, .1), (0, .14)], 6, FLAME, smooth=True, cap=False)
        b.use("Candelabra")
    return b

def floating_book():
    """Open book, 0.40 x 0.28 m spread. Origin: centre of the spine, pages face +Z."""
    b = Builder("FloatingBook")
    b.box((0, 0, -.012), (.025, .28, .05), OX)
    for sx in (-1, 1):
        with b.xf(about((0, 0, 0), Ry(sx * 14))):
            b.box((sx * .105, 0, -.02), (.21, .285, .012), OX)
            b.box((sx * .1, 0, .008), (.19, .26, .036), PAPER)
    return b

def portrait_frame():
    """0.9 x 1.3 m gilt frame (0.1 deep, back plane y=-0.05, front +Y), canvas is its own part 'Canvas' with 0..1 UVs
    (Castle_Canvas). Origin: bottom centre of the frame."""
    b = Builder("Portrait")
    W, Hh, f = .9, 1.3, .09
    b.box((0, 0, Hh - f / 2), (W, .1, f), BRASS); b.box((0, 0, f / 2), (W, .1, f), BRASS)
    for sx in (-1, 1):
        b.box((sx * (W / 2 - f / 2), 0, Hh / 2), (f, .1, Hh - 2 * f), BRASS)
        b.box((sx * (W / 2 - f - .02), .02, Hh / 2), (.04, .06, Hh - 2 * f - .1), BRASS)
    b.box((0, .02, Hh - f - .02), (W - 2 * f, .06, .04), BRASS); b.box((0, .02, f + .02), (W - 2 * f, .06, .04), BRASS)
    cw, ch = W - 2 * f - .06, Hh - 2 * f - .06
    b.part("Canvas", (0, .0, Hh / 2)); b.use("Canvas")
    v = [b._v((x, .0, Hh / 2 + z)) for x, z in ((-cw / 2, -ch / 2), (cw / 2, -ch / 2), (cw / 2, ch / 2), (-cw / 2, ch / 2))]
    b._f(v, CANVAS, False, [(0, 0), (1, 0), (1, 1), (0, 1)], closed=False)
    b.use("Portrait")
    return b

def bookshelf():
    """2.0 x 0.45 x 3.0 m oak shelving with ~80 books. Origin: floor centre; front +Y."""
    rnd = random.Random(99)
    b = Builder("Bookshelf")
    for sx in (-1, 1):
        b.box((sx * .97, 0, 1.5), (.06, .5, 3.0), OAK)
    b.box((0, -.2, 1.5), (1.9, .03, 2.9), OAK)
    b.box((0, 0, 2.97), (2.0, .5, .06), OAK)
    zs = [.06, .66, 1.26, 1.86, 2.46]
    for z in zs:
        b.box((0, 0, z), (1.94, .42, .04), OAK)
    mats = [OX, OAK, IRON, BRASS, OX]
    for z in zs:
        x = -.91
        top = z + .02
        gap = (zs[zs.index(z) + 1] if zs.index(z) < 4 else 2.97) - z - .04
        while x < .88:
            w = rnd.uniform(.035, .075); h = rnd.uniform(.5, .92) * min(gap, .46)
            if rnd.random() < .08: x += rnd.uniform(.1, .25); continue
            lean = rnd.uniform(-9, 9) if rnd.random() < .08 else 0
            with b.xf(about((x + w / 2, 0, top), Ry(lean))):
                b.box((x + w / 2, -.01, top + h / 2), (w * .97, .27, h), rnd.choice(mats))
            x += w + .004
    return b

def orrery():
    """Brass orrery on a pedestal, 1.2 m across, 1.1 m centre height. Parts: body + Sun (Castle_Flame, emissive).
    One planet is missing: an empty cup socket on the third arm (notes.socket) holds the hilt in the design."""
    b = Builder("Orrery")
    ORR = 2.3
    b.lathe((0, 0, 0), [(0, 0), (.35, 0), (.35, .06), (.2, .12), (.12, .2), (.1, .7), (.2, .76), (.2, .82), (0, .82)], 12, BRASS)
    c = (0, 0, 1.12)
    b.tube([(0, 0, .82), (0, 0, 1.05)], .035, 6, BRASS)
    orbits = [.15, .26, .37, .48, .6]
    planets = [.03, .04, None, .055, .035]
    tilts = [0, 9, -7, 13, -11]
    for i, (rr, pr, tl) in enumerate(zip(orbits, planets, tilts)):
        t = math.radians(tl)
        ax = Vector((1, 0, 0)); ay = Vector((0, math.cos(t), math.sin(t)))
        b.ring_loop(c, ax, ay, rr, .007, 24, 3, BRASS)
        a = math.radians(40 + i * 63)
        pos = Vector(c) + ax * (rr * math.cos(a)) + ay * (rr * math.sin(a))
        b.tube([c, tuple(pos)], .005, 3, BRASS)
        if pr:
            sphere(b, tuple(pos), pr, BRASS if i % 2 else SIL, seg=8)
        else:
            b.lathe(tuple(pos), [(0, -.01), (.03, -.01), (.045, .035), (.036, .04), (.0, .0)], 8, BRASS)
            b.notes["socket"] = [round(v, 3) for v in pos]
    b.part("Sun", c); sphere(b, c, .07, FLAME, seg=8)
    for P in b.parts.values():
        bm = P["bm"]; bm.verts.ensure_lookup_table()
        bmesh.ops.scale(bm, vec=(ORR, ORR, ORR), space=Matrix.Identity(4), verts=bm.verts[:])
        P["origin"] = P["origin"] * ORR
    b.notes["socket"] = [round(v * ORR, 3) for v in b.notes.get("socket", [0, 0, 0])]
    b.notes["scale"] = ORR
    return b

def dead_fountain():
    """Dry two-tier fountain, 2.6 x 1.8 x 2.6 m, cracked stump on top. Origin: floor centre."""
    b = Builder("Fountain")
    b.lathe((0, 0, 0), [(0, 0), (1.28, 0), (1.28, .45), (1.18, .5), (1.07, .45), (1.07, .13), (.4, .13), (.27, .25), (.23, 1.0), (.34, 1.1),
                        (.6, 1.2), (.6, 1.3), (.52, 1.3), (.42, 1.25), (0, 1.2)], 10, S, smooth=False)
    b.poly([(-.18, 0), (.16, -.14), (.22, .05), (.1, .17), (-.04, .1), (-.16, .16)], 1.2, 1.78, S, 'XY')
    return b

def drawbridge_unused():
    return None

def dead_tree():
    """Leafless tree, ~6 m tall, ~5 m crown spread, trunk base r 0.45. Origin: base centre."""
    rnd = random.Random(7)
    b = Builder("DeadTree")
    def branch(p, d, length, r, depth, segs):
        pts = [Vector(p)]; radii = [r]
        cur = Vector(p); dd = Vector(d).normalized()
        for i in range(segs):
            dd = (dd + Vector((rnd.uniform(-.28, .28), rnd.uniform(-.28, .28), rnd.uniform(0, .3)))).normalized()
            cur = cur + dd * (length / segs)
            pts.append(cur.copy()); radii.append(r * (1 - .6 * (i + 1) / segs))
        radii[-1] = max(radii[-1] * .4, .004)
        b.tube([tuple(q) for q in pts], radii, 7 if depth == 0 else (6 if depth < 2 else 4), OAK)
        if depth >= 4: return
        kids = 4 if depth == 0 else 2
        for k in range(kids):
            i = rnd.randint(max(1, segs // 2 - 1), segs) if depth else rnd.randint(2, segs)
            base = pts[i]
            az = rnd.uniform(0, math.tau); sp = math.radians(rnd.uniform(12, 30))
            ax = dd.cross(Vector((0, 0, 1))).normalized() if abs(dd.z) < .9 else Vector((1, 0, 0))
            ay = dd.cross(ax).normalized()
            nd = dd * math.cos(sp) + (ax * math.cos(az) + ay * math.sin(az)) * math.sin(sp)
            nd.z += .3
            branch(tuple(base), tuple(nd), length * rnd.uniform(.66, .8), radii[i] * .8, depth + 1, 4 if depth < 1 else 3)
    branch((0, 0, 0), (0, 0, 1), 1.95, .25, 0, 5)
    bm = b.cur["bm"]; bm.verts.ensure_lookup_table()
    bmesh.ops.scale(bm, vec=(.78, 1.2, 1.0), space=Matrix.Identity(4), verts=bm.verts[:])
    for k in range(5):
        a = k * math.tau / 5 + .3
        b.tube([(.08 * math.cos(a), .08 * math.sin(a), .22), (.3 * math.cos(a), .3 * math.sin(a), .08), (.6 * math.cos(a), .6 * math.sin(a), -.02)], [.14, .1, .03], 5, OAK)
    return b

def banner():
    """Oxblood hanging banner, 1.0 x 3.0 m with a swallow tail, brass rod and emblem. Origin: floor centre,
    hung from z=3.05 (rod). Double-sided cloth, front +Y."""
    b = Builder("Banner")
    nx, ny = 4, 12
    def fn(i, j):
        u = i / nx * 2 - 1
        length = 2.9 - .5 * (1 - abs(u))
        z = 3.0 - j / ny * length
        y = .04 * math.sin(j * .55 + i * .7) * (j / ny) + .01
        return ((i / nx - .5) * 1.0, y, z)
    b.grid_cloth(nx, ny, fn, OX)
    with b.xf(Ry(90)):
        b.lathe((-3.04, .0, -.65), [(.025, 0), (.025, 1.3)], 8, BRASS)
    for sx in (-1, 1):
        sphere(b, (sx * .65, 0, 3.04), .045, BRASS, seg=6)
    b.poly([(0, 2.05), (.17, 1.85), (0, 1.55), (-.17, 1.85)], .03, .04, BRASS, 'XZ')
    return b

BUILDERS = [
    ("Pew", pew, 3000), ("BanquetTable", banquet_table, 3000), ("Altar", altar, 3000), ("StoneFont", stone_font, 3000),
    ("Sarcophagus", sarcophagus, 3000), ("GiantSkeleton", giant_skeleton, 15000), ("Candelabra", candelabra, 3000),
    ("FloatingBook", floating_book, 3000), ("Portrait", portrait_frame, 3000), ("Bookshelf", bookshelf, 3000),
    ("Orrery", orrery, 3000), ("Fountain", dead_fountain, 3000), ("DeadTree", dead_tree, 3000), ("Banner", banner, 3000),
]

def spec_for(name):
    def check(info):
        if name == "GiantSkeleton":
            assert 11.4 <= info["size"][1] <= 12.6, info["size"]
        if name == "Pew":
            assert approx(info["size"][0], 2.4, .05), info["size"]
        if name == "Altar":
            assert approx(info["size"][0], 2.4, .01), info["size"]
    return check

def main():
    wanted = only()
    reset_scene()
    for name, make, budget in BUILDERS:
        if wanted and name not in wanted:
            continue
        export_asset(make(), budget, spec_for(name))
    write_manifest("props")
    print("HC_PROPS_PASS", len(MANIFEST))

if __name__ == "__main__":
    main()
