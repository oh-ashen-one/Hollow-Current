#!/usr/bin/env python3
"""Haunted Castle props: furniture, the giant skeleton, surreal dressing. Headless, idempotent:

    /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/castle_props.py [-- Name ...]

Exports Assets/HollowCurrent/Resources/Castle/<Name>.fbx and docs/haunted-castle/art/manifest_props.json.
Asset space: X right, Y depth (front = +Y), Z up, metres, origin at the floor centre unless noted.
"""
import sys, math, random
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from castle_lib import *

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
def chair(b, c=(0, 0, 0), yaw=0):
    with b.xf(T(*c) @ Rz(yaw)):
        b.box((0, 0, .46), (.5, .5, .06), OAK)
        for sx in (-1, 1):
            for sy in (-1, 1):
                b.box((sx * .21, sy * .21, .215), (.05, .05, .43), OAK)
            b.box((sx * .22, -.22, .88), (.05, .05, .84), OAK)
        b.poly([(-.22, .55), (.22, .55), (.22, 1.15), (0, 1.4), (-.22, 1.15)], -.245, -.205, OAK, 'XZ')
        b.box((0, -.22, .42), (.44, .04, .04), OAK)

def pew():
    b = Builder("Pew")
    for sx in (-1, 1):
        x = sx * 1.2
        b.poly([(-.3, 0), (.32, 0), (.32, .52), (-.02, .52), (-.12, 1.0), (-.26, 1.14), (-.32, 1.0)], x - .05, x + .05, OAK, 'YZ')
    b.box((0, 0, .47), (2.3, .5, .06), OAK)
    b.box((0, -.27, .64), (2.3, .05, .22), OAK)
    b.box((0, -.27, .9), (2.3, .05, .22), OAK)
    b.box((0, .45, .06), (2.3, .24, .12), OAK)
    b.notes["length"] = 2.5
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
        chair(b, (x, -.95, 0), 0); chair(b, (x, .95, 0), 180)
    chair(b, (-4.55, 0, 0), -90); chair(b, (4.55, 0, 0), 90)
    return b

# ---------------------------------------------------------------- ritual furniture
def altar():
    """Black altar, 2.8 x 1.3 x 1.13 m, three pointed recesses in the +Y face for blade, hilt, gem
    (niche centres x = -0.8, 0, 0.8; each 0.56 wide, 0.38..0.96 high, 0.4 deep)."""
    b = Builder("Altar")
    b.box((0, 0, .1), (2.8, 1.3, .2), BS)
    b.box_mm((-1.2, -.5, .2), (1.2, .1, 1.0), BS)
    cxs = (-.8, 0, .8)
    edges = [-1.2] + [v for cx in cxs for v in (cx - .28, cx + .28)] + [1.2]
    for k in range(4):
        x0, x1 = edges[2 * k], edges[2 * k + 1]
        b.box_mm((x0, .1, .2), (x1, .5, 1.0), BS)
    b.box_mm((-1.2, .1, .2), (1.2, .5, .38), BS)
    for cx in cxs:
        curve = [(x + cx, z) for (x, z) in arch_curve(.28, .72, .28, 5)]
        b.poly(curve + [(cx + .28, 1.0), (cx - .28, 1.0)], .1, .5, BS, 'XZ', smooth_range=(0, len(curve) - 1))
    b.box((0, 0, 1.05), (2.6, 1.2, .14), BS)
    b.box((0, .605, 1.05), (2.5, .02, .03), BRASS)
    b.box((0, -.05, 1.127), (1.4, .8, .012), OX)
    b.notes.update(niche_x=list(cxs), niche_size=[.56, .58, .4])
    return b

def stone_font():
    b = Builder("StoneFont")
    b.lathe((0, 0, 0), [(0, 0), (.4, 0), (.4, .1), (.25, .18), (.2, .35), (.2, .75), (.3, .82), (.42, .95), (.52, 1.05),
                        (.52, 1.08), (.44, 1.08), (.4, 1.0), (0, .94)], 8, S, smooth=False)
    b.lathe((0, 0, .97), [(.0, 0), (.38, 0)], 8, BS, cap=False) if False else None
    b.lathe((0, 0, 0), [(0, 1.0), (.36, 1.0), (.36, 1.005)], 8, BS, cap=False)
    return b

def sarcophagus():
    """Stone sarcophagus, 2.3 x 1.0 x 0.85 m trough with the lid pushed half off (+X) and skewed 9 deg.
    Origin: floor centre of the trough."""
    b = Builder("Sarcophagus")
    b.box((0, 0, .1), (2.3, 1.0, .2), S)
    for sy in (-1, 1):
        b.box((0, sy * .42, .525), (2.3, .16, .65), S)
    for sx in (-1, 1):
        b.box((sx * 1.07, 0, .525), (.16, .68, .65), S)
    b.box((0, 0, .205), (2.0, .68, .01), BS)
    with b.xf(about((.95, 0, .93), Rz(9))):
        b.box((.95, 0, .93), (2.4, 1.1, .16), S)
        b.box((.95, 0, 1.03), (2.2, .86, .06), S)
        b.box((.95, 0, 1.065), (.06, .6, .01), BRASS); b.box((.95, .12, 1.065), (.4, .06, .01), BRASS)
    b.notes["lid_offset"] = .95
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
    # skull (head +Y, snout points +Y, resting low, gape open)
    sc = (0.0, 5.2, .75)
    with b.xf(T(*sc) @ Rx(-8)):
        loft(b, (0, 0, 0), [(-.75, .26, .30, .1), (-.5, .35, .40, .14), (-.1, .30, .34, .1), (.3, .2, .22, .02), (.7, .13, .14, -.02), (.75, .11, .12, -.03)], 8, BONE, smooth=False)
        with b.xf(about((0, -.4, -.2), Rx(-14))):
            loft(b, (0, 0, 0), [(-.4, .17, .09, -.36), (-.1, .16, .08, -.36), (.3, .11, .06, -.34), (.7, .08, .05, -.32)], 6, BONE, smooth=False)
            for sx_ in (-1, 1):
                for i in range(4):
                    b.tube([(sx_ * .09, .1 + i * .15, -.3), (sx_ * .09, .1 + i * .15, -.2)], [.018, 0], 4, BONE)
        for sx_ in (-1, 1):
            for i in range(4):
                b.tube([(sx_ * .1, .12 + i * .15, -.03), (sx_ * .1, .12 + i * .15, -.15)], [.02, 0], 4, BONE)
            sphere(b, (sx_ * .24, .0, .2), .095, BS, seg=8)
    return b

# ---------------------------------------------------------------- surreal dressing
def candelabra():
    """Five-candle brass candelabrum, 0.85 m tall, parts: body + Flame_0..4 (Castle_Flame), each flame's
    pivot at its base so a script can bend it. Origin: base centre."""
    b = Builder("Candelabra")
    b.lathe((0, 0, 0), [(0, 0), (.14, 0), (.14, .025), (.07, .07), (.04, .12), (.05, .2), (.03, .26), (.035, .34), (.03, .45), (.05, .5), (.05, .52), (0, .52)], 10, BRASS)
    tips = [(0, 0, .52)]
    for k in range(4):
        a = math.radians(45 + 90 * k); ca, sa = math.cos(a), math.sin(a)
        pts = [(.03 * ca, .03 * sa, .38), (.1 * ca, .1 * sa, .35), (.2 * ca, .2 * sa, .41), (.27 * ca, .27 * sa, .52)]
        b.tube(pts, .015, 5, BRASS)
        b.lathe((.27 * ca, .27 * sa, .52), [(0, 0), (.035, 0), (.045, .035), (0, .035)], 8, BRASS)
        tips.append((.27 * ca, .27 * sa, .555))
    for i, t in enumerate(tips):
        h = .22 if i == 0 else .17
        b.lathe(t, [(.022, 0), (.022, h), (0, h)], 6, BONE, cap=False)
    for i, t in enumerate(tips):
        h = .22 if i == 0 else .17
        base = (t[0], t[1], t[2] + h)
        b.part("Flame_%d" % i, base)
        b.lathe(base, [(0, 0), (.012, .015), (.016, .04), (.008, .07), (0, .098)], 6, FLAME, smooth=True, cap=False)
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
    """1.1 x 1.5 m gilt frame (depth 0.08, back plane y=0, front +Y), canvas is its own part 'Canvas' with 0..1 UVs
    (Castle_Canvas). Origin: bottom centre of the frame."""
    b = Builder("PortraitFrame")
    for sy_, zc, w, h in ((1, 1.45, 1.1, .1), (1, .05, 1.1, .1)):
        b.box((0, .04, zc), (w, .08, h), BRASS)
    for sx in (-1, 1):
        b.box((sx * .5, .04, .75), (.1, .08, 1.3), BRASS)
        b.box((sx * .43, .06, .75), (.04, .06, 1.2), BRASS)
    b.box((0, .06, 1.38), (.9, .06, .04), BRASS); b.box((0, .06, .12), (.9, .06, .04), BRASS)
    b.part("Canvas", (0, .045, .75)); b.use("Canvas")
    w, h = .9, 1.3
    v = [b._v((x, .045, .75 + z)) for x, z in ((-w / 2, -h / 2), (w / 2, -h / 2), (w / 2, h / 2), (-w / 2, h / 2))]
    b._f(v, CANVAS, False, [(0, 0), (1, 0), (1, 1), (0, 1)], closed=False)
    b.use("PortraitFrame")
    return b

def bookshelf():
    """2.0 x 0.45 x 3.0 m oak shelving with ~80 books. Origin: floor centre; front +Y."""
    rnd = random.Random(99)
    b = Builder("Bookshelf")
    for sx in (-1, 1):
        b.box((sx * .97, 0, 1.5), (.06, .45, 3.0), OAK)
    b.box((0, -.2, 1.5), (1.9, .03, 2.9), OAK)
    b.box((0, 0, 3.0), (2.1, .5, .06), OAK)
    zs = [.06, .66, 1.26, 1.86, 2.46]
    for z in zs:
        b.box((0, 0, z), (1.94, .42, .04), OAK)
    mats = [OX, OAK, IRON, BRASS, OX]
    for z in zs:
        x = -.9
        top = z + .02
        gap = (zs[zs.index(z) + 1] if zs.index(z) < 4 else 2.97) - z - .04
        while x < .85:
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
    return b

def dead_fountain():
    b = Builder("DeadFountain")
    b.lathe((0, 0, 0), [(0, 0), (1.7, 0), (1.7, .55), (1.55, .62), (1.4, .55), (1.4, .15), (.5, .15), (.35, .3), (.3, 1.3), (.45, 1.4),
                        (.8, 1.5), (.8, 1.6), (.7, 1.6), (.55, 1.55), (0, 1.5)], 10, S, smooth=False)
    b.poly([(-.22, 0), (.2, -.18), (.28, .06), (.12, .22), (-.05, .12), (-.2, .2)], 1.5, 1.95, S, 'XY')
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
            dd = (dd + Vector((rnd.uniform(-.35, .35), rnd.uniform(-.35, .35), rnd.uniform(-.1, .25)))).normalized()
            cur = cur + dd * (length / segs)
            pts.append(cur.copy()); radii.append(r * (1 - .6 * (i + 1) / segs))
        radii[-1] = max(radii[-1] * .4, .004)
        b.tube([tuple(q) for q in pts], radii, 7 if depth == 0 else (6 if depth < 2 else 4), OAK)
        if depth >= 4: return
        kids = 4 if depth == 0 else 2
        for k in range(kids):
            i = rnd.randint(max(1, segs // 2 - 1), segs) if depth else rnd.randint(2, segs)
            base = pts[i]
            az = rnd.uniform(0, math.tau); sp = math.radians(rnd.uniform(28, 55))
            ax = dd.cross(Vector((0, 0, 1))).normalized() if abs(dd.z) < .9 else Vector((1, 0, 0))
            ay = dd.cross(ax).normalized()
            nd = dd * math.cos(sp) + (ax * math.cos(az) + ay * math.sin(az)) * math.sin(sp)
            nd.z += .15
            branch(tuple(base), tuple(nd), length * rnd.uniform(.66, .8), radii[i] * .8, depth + 1, 4 if depth < 1 else 3)
    branch((0, 0, 0), (0, 0, 1), 3.4, .42, 0, 5)
    for k in range(5):
        a = k * math.tau / 5 + .3
        b.tube([(.1 * math.cos(a), .1 * math.sin(a), .25), (.45 * math.cos(a), .45 * math.sin(a), .1), (.9 * math.cos(a), .9 * math.sin(a), -.02)], [.2, .14, .04], 5, OAK)
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
    ("FloatingBook", floating_book, 3000), ("PortraitFrame", portrait_frame, 3000), ("Bookshelf", bookshelf, 3000),
    ("Orrery", orrery, 3000), ("DeadFountain", dead_fountain, 3000), ("DeadTree", dead_tree, 3000), ("Banner", banner, 3000),
]

def spec_for(name):
    def check(info):
        if name == "GiantSkeleton":
            assert 11.4 <= info["size"][1] <= 12.6, info["size"]
        if name == "Pew":
            assert approx(info["size"][0], 2.5, .05), info["size"]
        if name == "Altar":
            assert approx(info["size"][0], 2.8, .01)
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
