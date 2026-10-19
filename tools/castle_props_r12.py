#!/usr/bin/env python3
"""Haunted Castle props, round 12 (hc/art-props): hand-roots, books, relic sockets, the great-hall giant skeleton.

    /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/castle_props_r12.py [-- Name ...]
    python3 tools/castle_r12_textures.py      # the nine new materials (run first)

Exports Assets/HollowCurrent/Resources/Castle/<Name>.fbx and docs/haunted-castle/art/manifest_props_r12.json.
Asset space as in castle_props.py: X right, Y depth (front = +Y), Z up, metres. Exception: hand-roots are authored hanging
along -Z with the origin at the top of the strand (they import hanging along -Y in Unity, the HandRootMesh convention).
Replaces GiantSkeleton and FloatingBook from castle_props.py (those two builders are gone from its BUILDERS list).
"""
import sys, math, random
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from castle_lib import *
from mathutils import Matrix, Vector

LEATHER_A, LEATHER_B, LEATHER_C = "Castle_BookLeatherA", "Castle_BookLeatherB", "Castle_BookLeatherC"
PAGES, GILT, BARK, FLESH, GBONE, NICHE = ("Castle_BookPages", "Castle_GiltWorn", "Castle_RootBark", "Castle_RootFlesh",
                                          "Castle_BoneGrime", "Castle_NicheStone")
RUNE = "Castle_RuneGlow"

def only():
    a = sys.argv
    return a[a.index("--") + 1:] if "--" in a else []

# ------------------------------------------------------------------------------------------------------------------ sweeps
def sweep(b, pts, rad, mat, sides=7, up=(0, -1, 0), tile=.25, cap_end=True, cap_start=False, smooth=True, closed_faces=True):
    """Loft an (optionally elliptical) section along a polyline. rad: float | [float] | [(rx, ry)]. `up` fixes the ellipse's
    narrow axis (ry) so fingers stay flat across the palm. UV: u around (metres/tile), v along (metres/tile)."""
    pts = [Vector(p) for p in pts]; n = len(pts)
    if not isinstance(rad, (list, tuple)): rad = [rad] * n
    rad = [(r, r) if not isinstance(r, (list, tuple)) else r for r in rad]
    upv = Vector(up).normalized()
    rings = []; length = [0.0]
    for i in range(n):
        t = (pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]).normalized()
        if i: length.append(length[-1] + (pts[i] - pts[i - 1]).length)
        ref = upv if abs(t.dot(upv)) < .92 else Vector((1, 0, 0))
        side = t.cross(ref).normalized(); dep = side.cross(t).normalized()
        rx, ry = rad[i]
        if max(rx, ry) < 1e-5:
            rings.append([b._v(pts[i])]); continue
        rings.append([b._v(pts[i] + side * (math.cos(j * math.tau / sides) * rx) + dep * (math.sin(j * math.tau / sides) * ry)) for j in range(sides)])
    circ = math.tau * max(max(r) for r in rad)
    for i in range(n - 1):
        A, B = rings[i], rings[i + 1]
        v1, v2 = length[i] / tile, length[i + 1] / tile
        for j in range(sides):
            j2 = (j + 1) % sides
            u1, u2 = j / sides * circ / tile, (j + 1) / sides * circ / tile
            if len(A) == 1 and len(B) == 1: continue
            if len(A) == 1: b._f([A[0], B[j2], B[j]], mat, smooth, [(u1, v1), (u2, v2), (u1, v2)], closed_faces)
            elif len(B) == 1: b._f([A[j], A[j2], B[0]], mat, smooth, [(u1, v1), (u2, v1), (u1, v2)], closed_faces)
            else: b._f([A[j], A[j2], B[j2], B[j]], mat, smooth, [(u1, v1), (u2, v1), (u2, v2), (u1, v2)], closed_faces)
    if cap_end and len(rings[-1]) > 1:
        b._f(list(rings[-1]), mat, False, [((math.cos(j * math.tau / sides) * .5 + .5) * .2, (math.sin(j * math.tau / sides) * .5 + .5) * .2) for j in range(sides)])
    if cap_start and len(rings[0]) > 1:
        b._f(list(reversed(rings[0])), mat, False, [((math.cos(j * math.tau / sides) * .5 + .5) * .2, (math.sin(j * math.tau / sides) * .5 + .5) * .2) for j in range(sides)])

def lerp(a, b, t): return a + (b - a) * t

def rope(b, pts, rds, mat, strands=3, turns=1.6, sides=5, tile=.28, converge=.0, core=.62):
    """A root rope: a core tube plus `strands` thinner tubes spiralling around it, all merging to the core radius at the far end
    (converge 0..1 = how much the spiral tightens in the last quarter). pts: centre line, rds: its radius per point."""
    pts = [Vector(p) for p in pts]; n = len(pts)
    sweep(b, pts, [r * core for r in rds], mat, sides=sides + 1, tile=tile, cap_end=False)
    tans = [(pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]).normalized() for i in range(n)]
    sds = []; prev = tans[0].cross(Vector((1, 0, 0)) if abs(tans[0].x) < .9 else Vector((0, 1, 0))).normalized()
    for t in tans:                                  # parallel transport: no frame flips along the rope
        prev = (prev - t * prev.dot(t)).normalized(); sds.append(prev)
    for st in range(strands):
        sp, sr = [], []
        for i in range(n):
            k = i / (n - 1)
            t = tans[i]; sd = sds[i]; dp = t.cross(sd).normalized()
            ang = math.tau * st / strands + k * turns * math.tau + .55 * math.sin(k * 7.3 + st * 2.1)
            rho = rds[i] * (.62 + .22 * math.sin(k * 11 + st * 1.7)) * (1 - converge * max(0, (k - .72) / .28) ** 1.5)
            sp.append(pts[i] + (sd * math.cos(ang) + dp * math.sin(ang)) * rho)
            sr.append(rds[i] * (.50 + .12 * math.sin(k * 13 + st * 2.9)) * (1 - .24 * max(0, (k - .75) / .25) * converge))
        sweep(b, sp, sr, mat, sides=sides, tile=tile, cap_end=False)

# ------------------------------------------------------------------------------------------------------------------ hand
# Local hand frame: wrist at the origin, fingers along -Z, palm faces +Y (back of the hand -Y), thumb on +X.
# Real-world sizes in metres, then scaled by `s` (hand-roots read at about 1.5 x real) and by `longer` for spindly fingers.
FINGERS = {  # name: (x at knuckle, phalanx lengths, base radius, splay deg)
    "index": (.0285, (.040, .025, .021), .0098, 4), "middle": (.0098, (.045, .028, .022), .0105, 0),
    "ring": (-.0098, (.042, .026, .021), .0098, -3), "pinky": (-.0285, (.034, .020, .018), .0085, -8),
}
PALM = .105

def add_hand(b, wrist, tvec, twist_deg, s, pose, rnd, mirror=1, longer=1.5):
    """pose: dict(curl=[deg per joint], spread=deg, hook=0..1). wrist: world point where the strand ends; tvec: strand direction."""
    q = Vector((0, 0, -1)).rotation_difference(Vector(tvec).normalized())
    M = Matrix.Translation(wrist) @ q.to_matrix().to_4x4() @ Matrix.Rotation(math.radians(twist_deg), 4, 'Z') @ Matrix.Scale(s, 4)
    R = M.to_3x3().normalized()
    up_w = R @ Vector((0, -1, 0))
    W = lambda p: M @ Vector((p[0] * mirror, p[1], p[2]))
    # palm and wrist: elliptical sections, the root's last ring (r .021 round) opens into the wrist
    zs = [0.0, -.025, -.060, -.092, -PALM]
    rr = [(.0150, .0130), (.0200, .0145), (.0305, .0150), (.0355, .0140), (.0360, .0125)]
    sweep(b, [W((0, 0, z)) for z in zs], [(rx * s, ry * s) for rx, ry in rr], FLESH, sides=9, up=up_w, tile=.14, cap_end=True)
    # back of the hand: four metacarpal ridges ending in knuckle swellings, plus the thenar mass under the thumb
    for name, (kx, lens, r0, splay) in FINGERS.items():
        mp = [W((kx * .35, -.0105, -.020)), W((kx * .7, -.0115, -.060)), W((kx, -.0105, -PALM+.002))]
        sweep(b, mp, [(.0060 * s, .0050 * s), (.0072 * s, .0058 * s), (r0 * 1.22 * s, r0 * 1.05 * s)], FLESH, sides=6, up=up_w, tile=.10, cap_end=True)
    sweep(b, [W((.010, .004, -.006)), W((.020, .010, -.030)), W((.020, .012, -.058))], [(.014 * s, .011 * s), (.0165 * s, .0125 * s), (.0100 * s, .0090 * s)], FLESH, sides=7, up=up_w, tile=.12, cap_end=True)
    # fingers: three phalanges with knuckle swellings, curled toward the palm (+Y)
    curl = pose["curl"]; spread = pose["spread"]; hook = pose.get("hook", 0)
    for name, (kx, lens, r0, splay) in FINGERS.items():
        pts, rds = [], []
        base = Vector((kx, .0, -PALM + .004)); d_splay = math.radians(splay * spread + rnd.uniform(-3, 3))
        c_acc = 0; pos = base
        for ph, L in enumerate(lens):
            L *= longer
            c_acc += math.radians(curl[ph] * (1 + hook * .6) * rnd.uniform(.75, 1.25))
            d = Vector((math.sin(d_splay) * math.cos(c_acc), math.sin(c_acc), -math.cos(d_splay) * math.cos(c_acc)))
            for k in (0, .5):
                p = pos + d * L * k
                pts.append(p); rds.append(r0 * .86 * (1 - .13 * ph - .05 * k) * (1.26 if k == 0 else .80))
            pos = pos + d * L
        pts.append(pos); rds.append(r0 * .52)
        # joint swellings: the section at each joint start is fatter (index 0, 2, 4 are joint starts)
        pts.append(pos + d * .007 * s * 0 + d * .004); rds.append(0)
        sweep(b, [W(p) for p in pts], [(r * s * 1.12, r * s * .92) for r in rds], FLESH, sides=8, up=up_w, tile=.10, cap_end=False)
    # thumb: metacarpal off the palm side, then two phalanges
    th = Vector((.016, .010, -.040)); dirs = [(.34, .26, -.90), (.28, .38, -.88), (.20, .48, -.85)]
    tl = [.032, .030 * longer, .025 * longer]; pts = [th]; rds = [.0125]; pos = th
    for i, (dx, dy, dz) in enumerate(dirs):
        d = Vector((dx, dy * (1 + hook * .5), dz)).normalized()
        pos = pos + d * tl[i]; pts.append(pos); rds.append(.0115 - .0025 * i + (.0015 if i == 0 else 0))
    pts.append(pos + d * .004); rds.append(0)
    sweep(b, [W(p) for p in pts], [(r * s * 1.1, r * s * .95) for r in rds], FLESH, sides=7, up=up_w, tile=.10, cap_end=False)

# ------------------------------------------------------------------------------------------------------------------ root strand + hands
POSES = [   # one per variant: curl per joint (deg), spread, hook
    dict(curl=[12, 22, 14], spread=1.2, hook=0.0),   # limp, drooping
    dict(curl=[26, 38, 28], spread=.6, hook=.5),     # clawed
    dict(curl=[6, 12, 8], spread=2.4, hook=0.0),     # splayed open, reaching
    dict(curl=[34, 50, 36], spread=.3, hook=.8),     # grasping
    dict(curl=[16, 28, 20], spread=1.6, hook=.2),    # long, half curled
    dict(curl=[10, 30, 40], spread=1.0, hook=.4),    # fingertips hooked
]
LENGTHS = [1.0, 1.3, 1.6, 1.9, 2.2, 1.45]   # strand lengths, = SurrealProps.HandRootLengths
S_MAIN = 1.75

def rootlet(b, rnd, p0, outward, r0, length, steps, droop=.5, mat=None, sides=4):
    """A thin dead root: leaves `p0` along `outward`, curls and droops, tapers to a hair."""
    d = (Vector(outward) + Vector((0, 0, -.35))).normalized()
    pts = [Vector(p0)]; rds = [r0]; sl = length / steps
    for i in range(1, steps + 1):
        d = (d + Vector((rnd.uniform(-.35, .35), rnd.uniform(-.35, .35), -droop)) * .5).normalized()
        pts.append(pts[-1] + d * sl); rds.append(lerp(r0, .0010, (i / steps) ** .9))
    sweep(b, pts, rds, mat or BARK, sides=sides, tile=.2, cap_end=True)

def hand_root(variant):
    rnd = random.Random(9100 + variant * 37)
    L = LENGTHS[variant]
    b = Builder("HandRoot%d" % variant)
    # main strand: top at the origin hanging -Z, an S-curve that wanders sideways, thick and knotted at the top, ropy
    n = 16; ph = rnd.uniform(0, math.tau)
    pts, rds = [], []
    amp = .13 + .02 * variant
    for i in range(n + 1):
        k = i / n
        x = math.sin(k * 3.1 + ph) * amp * k ** .8 + math.sin(k * 8 + ph * 2) * .02 * k + math.sin(k * 14 + ph) * .008 * k
        y = math.cos(k * 2.3 + ph) * amp * .8 * k ** .8 + math.cos(k * 6.5 + ph) * .018 * k
        pts.append((x, y, -k * L))
        r = lerp(.050, S_MAIN * .0155 * .95, k ** .7)
        r *= 1 + .16 * math.sin(k * 21 + ph) * (1 - k) + (.38 * (1 - k) if abs(k - .30 - .12 * rnd.random()) < .045 else 0) + (.28 * (1 - k) if abs(k - .62) < .03 else 0)
        rds.append(r)
    rds[-1] = S_MAIN * .0155 * .95
    rope(b, pts, rds, BARK, strands=3, turns=1.3 + .15 * (variant % 3), converge=1.0)
    pose = POSES[variant]
    twist = rnd.uniform(-40, 40)
    tend = (Vector(pts[-1]) - Vector(pts[-2])).normalized()
    add_hand(b, Vector(pts[-1]), tend, twist, S_MAIN, pose, rnd, mirror=1 if variant % 2 == 0 else -1)
    # side branches: ropes ending in smaller hands, tips kept within 0.12 m of the main hand's depth
    branch_pts = []
    for j in range(2):
        start = int(n * rnd.uniform(.22, .52)); p0 = Vector(pts[start])
        s_h = .86 - .14 * j
        drop = rnd.uniform(.40, .64) * (L - (-p0.z)) + .25
        lat = (-1 if j == 0 else 1) * rnd.uniform(.24, .40) * (1 if variant % 2 == 0 else -1); lat2 = rnd.uniform(-.14, .14)
        end = p0 + Vector((lat, lat2, -drop))
        bp, br = [], []
        for i in range(11):
            t = i / 10
            bow = math.sin(t * math.pi) * rnd.uniform(.07, .14) * (1 if j == 0 else -1)
            bp.append(p0 + (end - p0) * t + Vector((bow, math.sin(t * 4.5 + j + ph) * .045, -.06 * math.sin(t * math.pi))))
            br.append(lerp(rds[start] * .66, S_MAIN * s_h * .0155 * .95, t ** .8) * (1 + .12 * math.sin(t * 17 + j)))
        branch_pts.append((bp, br))
        rope(b, bp, br, BARK, strands=2, turns=1.0, converge=1.0, sides=5, core=.7)
        pose_b = dict(curl=[c + rnd.uniform(-6, 8) for c in pose["curl"]], spread=pose["spread"], hook=pose.get("hook", 0))
        add_hand(b, bp[-1], (bp[-1] - bp[-2]).normalized(), rnd.uniform(-70, 70), S_MAIN * s_h, pose_b, rnd, mirror=rnd.choice([-1, 1]))
    # a thick dead branch that never reaches a hand (every other variant) and many hair roots on the strand and the branches
    if variant % 2:
        start = int(n * rnd.uniform(.12, .30)); p0 = Vector(pts[start])
        ang = rnd.uniform(0, math.tau)
        rootlet(b, rnd, p0 + Vector((math.cos(ang), math.sin(ang), 0)) * rds[start] * .7, (math.cos(ang), math.sin(ang), -.3), rds[start] * .42, rnd.uniform(.55, .85), 8, .35, sides=5)
    for j in range(7 + variant % 3):
        start = int(n * rnd.uniform(.03, .78)); p0 = Vector(pts[start]); a = rnd.uniform(0, math.tau)
        rootlet(b, rnd, p0 + Vector((math.cos(a), math.sin(a), 0)) * rds[start] * .65, (math.cos(a), math.sin(a), 0), rds[start] * .30, rnd.uniform(.12, .38), rnd.randint(4, 5), .45)
    for (bp, br) in branch_pts:
        for j in range(2):
            i = rnd.randint(2, 8); a = rnd.uniform(0, math.tau)
            rootlet(b, rnd, bp[i] + Vector((math.cos(a), math.sin(a), 0)) * br[i] * .6, (math.cos(a), math.sin(a), 0), br[i] * .35, rnd.uniform(.10, .26), 4, .45)
    b.notes = dict(strand_length=L)
    return b

def root_crown():
    """Unit crown for the runtime scale-to-ceiling cone: +Z (Unity +Y) from 0 (strand top) to 1 (ceiling), six roots splaying out
    over the ceiling plane at the top. Scaled along Y by the crown length (0.06..0.5 m) so it works as a flared collar."""
    rnd = random.Random(515)
    b = Builder("RootCrown")
    for i in range(7):
        a = math.tau * i / 7 + rnd.uniform(-.25, .25)
        reach = rnd.uniform(.22, .36)
        pts, rds = [], []
        for j in range(9):
            t = j / 8
            sw = math.sin(t * 5 + i) * .03 * t
            rr = reach * (t ** 1.6)
            pts.append((math.cos(a) * rr + math.cos(a + 1.57) * sw, math.sin(a) * rr + math.sin(a + 1.57) * sw, t))
            rds.append(lerp(.030, .014, t) if i else lerp(.042, .02, t))
        sweep(b, pts, rds, BARK, sides=6, tile=.30, cap_end=False)
    return b

# ------------------------------------------------------------------------------------------------------------------ books
def book_uv_rect(face_pts, rect):
    return rect

def add_cover_board(b, mat, x0, x1, y0, y1, z0, z1, tilt_rot=None, outer_up=True):
    """A cover board as a box with the outer face mapped to the whole cover texture and the rest to the plain border band."""
    pass

def book_open(size_scale, leather, name):
    """Open book, base 0.40 x 0.28 m spread (x spine to outer 0.205 each side). Origin: centre of the spine, pages face +Z.
    Boards tilted up 13 deg from the spine; page blocks with a curved top, fanned loose sheets, a ribbon, leather corners."""
    b = Builder(name)
    S = size_scale
    for sx in (-1, 1):
        ang = math.radians(sx * 13)
        # board frame: u along the board away from the spine, tilted: rot about Y axis
        R = Matrix.Rotation(-ang, 4, 'Y')
        T0 = Matrix.Translation((0, 0, 0))
        bw, bl, bt = .212, .292, .0075       # board width (from spine), length (Y), thickness
        def Bp(u, y, z): return (R @ Vector((sx * u, y, z)))[:]
        # board outer (underside) with the cover design, top face (endpaper) plain
        o = [(.004, -bl / 2), (bw, -bl / 2), (bw, bl / 2), (.004, bl / 2)]
        zb = -bt
        # underside (design): u increases away from the spine; uv u=0..1 across the cover width, v along the length
        v = [b._v(Bp(u, y, zb)) for (u, y) in o]
        uvs = [(.0 + (u - .004) / (bw - .004) * 1.0, (y + bl / 2) / bl) for (u, y) in o]
        if sx > 0: uvs = [(1 - u, v_) for (u, v_) in uvs]
        b._f(list(reversed(v)) if sx > 0 else v, leather, False, [(uvs[i][0], uvs[i][1]) for i in (range(3, -1, -1) if sx > 0 else range(4))], closed=False)
        # top (endpaper): plain border band of the leather map
        v2 = [b._v(Bp(u, y, 0)) for (u, y) in o]
        uvp = [(.01, .01), (.04, .01), (.04, .04), (.01, .04)]
        b._f(v2 if sx > 0 else list(reversed(v2)), leather, False, uvp, closed=False)
        # four edges
        for i in range(4):
            j = (i + 1) % 4
            a1, a2 = v[i], v[j]; c1, c2 = v2[i], v2[j]
            b._f([a1, a2, c2, c1] if sx > 0 else [a2, a1, c1, c2], leather, False, [(.01, .01), (.03, .01), (.03, .03), (.01, .03)], closed=False)
        # page block: curved top, from the gutter outward; pages are shorter than the board
        N = 12; pw = bw - .012; pl = bl - .014; thick = .030
        rows = []
        for i in range(N + 1):
            s = i / N; u = .008 + s * pw
            zt = thick * (.55 + .45 * min(1, s * 5)) * (1 - .22 * s ** 2) + .004 * math.exp(-s * 10) * -1  # gutter dips, edge sags a little
            rows.append((u, zt))
        pvl = []
        for (u, zt) in rows:
            pvl.append((b._v(Bp(u, -pl / 2, 0)), b._v(Bp(u, pl / 2, 0)), b._v(Bp(u, -pl / 2, zt)), b._v(Bp(u, pl / 2, zt))))
        for i in range(N):
            a, c = pvl[i], pvl[i + 1]
            u0, u1 = rows[i][0] / pw, rows[i + 1][0] / pw
            uvx0, uvx1 = (.97 * u0, .97 * u1)
            # top surface (page face): u across (from spine), v along length, texture rows .15..1
            b._f([a[2], c[2], c[3], a[3]] if sx > 0 else [a[3], c[3], c[2], a[2]], PAGES, True,
                 [(uvx0, .16), (uvx1, .16), (uvx1, .99), (uvx0, .99)] if sx > 0 else [(uvx0, .99), (uvx1, .99), (uvx1, .16), (uvx0, .16)], closed=False)
        # fore edge (outer side) with the layered band, and head/tail edges
        a = pvl[-1]
        b._f([a[0], a[2], a[3], a[1]] if sx > 0 else [a[1], a[3], a[2], a[0]], PAGES, False, [(0, .02), (0, .10), (1, .10), (1, .02)], closed=False)
        for end in (0, 1):
            for i in range(N):
                p, q = pvl[i], pvl[i + 1]
                qs = [p[end], q[end], q[2 + end], p[2 + end]]
                b._f(qs if (sx > 0) == (end == 1) else list(reversed(qs)), PAGES, False, [(0, .02), (.05, .02), (.05, .10), (0, .10)], closed=False)
        # loose fanned sheets lifted off the block near the outer edge (curved strips, double sided)
        rnds = random.Random(31 + sx * 7 + int(S * 100))
        for k in range(3 + (1 if S > 1 else 0)):
            lift = rnds.uniform(.012, .05) + k * .006
            lw = pw * rnds.uniform(.82, .98); ll = pl * rnds.uniform(.9, .985); ofs = rnds.uniform(-.02, .02)
            strip = []
            for i in range(9):
                s = i / 8
                u = .010 + s * lw
                zt = thick * .95 + lift * (s ** 1.6) + .004 * math.sin(s * math.pi) * (1 + k)
                tip = (1 - .04 * s * (1 + k))
                strip.append((b._v(Bp(u, -ll / 2 * tip + ofs, zt)), b._v(Bp(u, ll / 2 * tip + ofs, zt + .004 * s * rnds.uniform(-1, 1)))))
            for i in range(8):
                a_, c_ = strip[i], strip[i + 1]
                uv = ([(i / 8 * .97, .16), ((i + 1) / 8 * .97, .16), ((i + 1) / 8 * .97, .99), (i / 8 * .97, .99)])
                b._f([a_[0], c_[0], c_[1], a_[1]] if sx > 0 else [a_[1], c_[1], c_[0], a_[0]], PAGES, True, uv, closed=False)
                b._f([a_[1], c_[1], c_[0], a_[0]] if sx > 0 else [a_[0], c_[0], c_[1], a_[1]], PAGES, True, uv, closed=False)
        # leather corner caps (worn gilt) on the outer corners, thin brass-gilt wedges on the board
        for yy in (-1, 1):
            cpts = [Bp(bw - .035, yy * (bl / 2 - .003), zb - .0008), Bp(bw + .001, yy * (bl / 2 - .003), zb - .0008), Bp(bw + .001, yy * (bl / 2 - .035), zb - .0008)]
            b._f([b._v(c) for c in (cpts if (sx > 0) == (yy > 0) else cpts[::-1])], GILT, False, [(0, 0), (1, 0), (0, 1)], closed=False)
    # spine: a leather cylinder segment joining the two boards (hinge), with raised bands
    spts = [Vector((math.sin(a) * .0075, 0, -math.cos(a) * .0085 - .0005)) for a in [math.radians(t) for t in range(-90, 91, 30)]]
    # (simple: a half-round bar along Y under the gutter)
    ring = [(math.cos(math.radians(t)) * .011, math.sin(math.radians(t)) * .0105 - .0075) for t in range(180, 361, 30)]
    L_ = .292
    cols = [[b._v((x, y, z)) for (x, z) in ring] for y in (-L_ / 2, L_ / 2)]
    for i in range(len(ring) - 1):
        b._f([cols[0][i], cols[0][i + 1], cols[1][i + 1], cols[1][i]], leather, True,
             [(.01, .0), (.01, .0), (.03, 1), (.03, 1)] if False else [(.01, .01), (.03, .01), (.03, .04), (.01, .04)], closed=False)
    # ribbon marker: a short silk tongue hanging off the foot of the spine
    rp = [Vector((0, -.130, .026)), Vector((.002, -.165, .016)), Vector((.008, -.188, .000)), Vector((.014, -.205, -.022))]
    sweep(b, rp, [(.0075, .0006), (.0075, .0006), (.0070, .0006), (.0050, .0005)], leather, sides=4, up=(0, 0, 1), tile=2.0, cap_end=True)
    # scale to the requested size
    if abs(S - 1) > 1e-6:
        for P_ in b.parts.values():
            bm = P_["bm"]
            import bmesh as _bm
            _bm.ops.scale(bm, vec=(S, S, S), verts=bm.verts[:], space=Matrix.Identity(4))
    return b

def book_closed(S, leather, name, clasp=False, dims=(.20, .14, .035)):
    """Closed book lying flat, spine on -X: 0.20 x 0.14 x 0.035 m at S=1. Origin: centre of the underside (rests on z=0). The front
    (top) board carries the cover design, the page block is set back, the spine has raised bands, corners and an optional clasp are
    worn gilt. Throwable: bounds stay under 0.4 m at S <= 1.9."""
    b = Builder(name)
    w, d, T, bt = dims[0] * S, dims[1] * S, dims[2] * S, .0055 * S
    ov = .004 * S                                              # board overhang past the pages
    # boards
    for top in (True, False):
        z0, z1 = (T - bt, T) if top else (0, bt)
        x0, x1, y0, y1 = -w / 2, w / 2, -d / 2, d / 2
        zf = z1 if top else z0
        face = [b._v((x, y, zf)) for (x, y) in ((x0, y0), (x1, y0), (x1, y1), (x0, y1))]
        # design: u across the width from the spine, v along the depth (texture is square: 0.22 x 0.30 m of design)
        uvs = [(0, 0), (1, 0), (1, 1), (0, 1)]
        b._f(face if top else list(reversed(face)), leather, False, uvs if top else [uvs[0], uvs[3], uvs[2], uvs[1]], closed=False)
        back = [b._v((x, y, z0 if top else z1)) for (x, y) in ((x0, y0), (x1, y0), (x1, y1), (x0, y1))]
        b._f(list(reversed(back)) if top else back, leather, False, [(.01, .01), (.04, .01), (.04, .04), (.01, .04)], closed=False)
        for i in range(4):
            j = (i + 1) % 4
            b._f([face[i], face[j], back[j], back[i]] if (top == (i in (0, 2) or True)) and False else ([face[j], face[i], back[i], back[j]] if top else [face[i], face[j], back[j], back[i]]),
                 leather, False, [(.01, .01), (.04, .01), (.04, .02), (.01, .02)], closed=False)
    # page block, inset on three sides (head, tail, fore edge), flush at the spine
    px0, px1, py0, py1, pz0, pz1 = -w / 2 + .008 * S, w / 2 - ov, -d / 2 + ov, d / 2 - ov, bt, T - bt
    pv = {(x, y, z): b._v((x, y, z)) for x in (px0, px1) for y in (py0, py1) for z in (pz0, pz1)}
    def q(a_, b_, c_, d_, uv): b._f([pv[a_], pv[b_], pv[c_], pv[d_]], PAGES, False, uv, closed=False)
    band = [(0, .02), (1, .02), (1, .10), (0, .10)]
    q((px1, py0, pz0), (px1, py1, pz0), (px1, py1, pz1), (px1, py0, pz1), band)     # fore edge (+X)
    q((px0, py0, pz0), (px1, py0, pz0), (px1, py0, pz1), (px0, py0, pz1), band)     # tail (-Y), reversed below for orientation
    q((px1, py1, pz0), (px0, py1, pz0), (px0, py1, pz1), (px1, py1, pz1), band)     # head (+Y)
    # spine: half-round bar with 4 raised bands
    ring = [(-w / 2 - math.sin(math.radians(t)) * T / 2 * 1.0, T / 2 + math.cos(math.radians(t)) * T / 2) for t in range(0, 181, 30)]
    cols = [[b._v((x, y, z)) for (x, z) in ring] for y in (-d / 2, d / 2)]
    for i in range(len(ring) - 1):
        b._f([cols[0][i], cols[1][i], cols[1][i + 1], cols[0][i + 1]], leather, True, [(.01, .01), (.04, .01), (.04, .02), (.01, .02)], closed=False)
    for k in range(4):
        y = -d / 2 + d * (k + 1) / 5
        arc = [(x * 1.0 - (T * .02 if False else 0) + (-.0012 * S) * math.sin(math.radians(t)), y, z + .0012 * S * math.cos(math.radians(t)))
               for (x, z), t in zip(ring, range(0, 181, 30))]
        sweep(b, arc, [(.0016 * S, .0018 * S)] * len(arc), GILT, sides=4, up=(0, 1, 0), tile=.1, cap_end=True)
    # worn gilt corner caps on the top board
    for sx in (-1, 1):
        for sy in (-1, 1):
            cx, cy = sx * w / 2, sy * d / 2
            pts = [(cx, cy - sy * .03 * S), (cx, cy), (cx - sx * .03 * S, cy)]
            b._f([b._v((x, y, T + .0004 * S)) for (x, y) in (pts if sx * sy > 0 else pts[::-1])], GILT, False, [(0, 0), (1, 0), (0, 1)], closed=False)
    if clasp:
        b.box((w / 2 + .001, 0, T / 2), (.006 * S, .020 * S, T * .9), GILT)
        b.box((w / 2 - .02 * S, 0, T + .0015 * S), (.05 * S, .018 * S, .003 * S), GILT)
    return b

# ------------------------------------------------------------------------------------------------------------------ relic sockets
import castle_lib
castle_lib.UV_TILE.update({NICHE: .6, GILT: .3, GBONE: 1.0, BARK: .3, FLESH: .12, RUNE: 1.0, PAGES: 1.0})

def poly_offset(pts, d):
    """Miter offset of a CCW polygon: d > 0 grows it, d < 0 shrinks it."""
    n = len(pts); out = []
    for i in range(n):
        p0, p1, p2 = Vector(pts[i - 1]), Vector(pts[i]), Vector(pts[(i + 1) % n])
        e1 = (p1 - p0).normalized(); e2 = (p2 - p1).normalized()
        n1 = Vector((e1.y, -e1.x)); n2 = Vector((e2.y, -e2.x)); m = (n1 + n2).normalized()
        k = 1.0 / max(.35, m.dot(n1))
        out.append((p1 + m * d * k)[:])
    return out

def circle_pts(r, n=24): return [(r * math.cos(math.tau * i / n), r * math.sin(math.tau * i / n)) for i in range(n)]

def carved_socket(name, outline, pocket_d, rune_fn, height=.31, pocket_depth=.042):
    """A carved stone niche for a relic: foot, shaft, cornice with a lip, a pocket following the relic's outline with sloped walls
    and a dark floor, worn gilt on the lip and on a fillet round the shaft, and a separate 'Rune' part (Castle_RuneGlow) lying on
    the pocket floor that the game lights when the relic is placed. Origin: centre of the base, top at `height`."""
    b = Builder(name)
    prof = [(0, 0), (0, .060), (-.013, .066), (-.013, .235), (-.009, .245), (-.009, .262), (.0, .272), (.0, height)]   # (offset m, z)
    rings = [[b._v((x, y, z)) for (x, y) in poly_offset(outline, d)] for (d, z) in prof]
    n = len(outline)
    bottom = [b._v((x, y, 0)) for (x, y) in poly_offset(outline, 0)]
    b._f(list(reversed(bottom)), NICHE, False)
    for i in range(len(prof) - 1):
        for j in range(n):
            j2 = (j + 1) % n
            b._f([rings[i][j], rings[i][j2], rings[i + 1][j2], rings[i + 1][j]], NICHE, False)
    lip_in = -.028
    ztop = height
    # top lip: flat ring from the cornice edge to the pocket rim
    top_in = [b._v((x, y, ztop)) for (x, y) in poly_offset(outline, lip_in)]
    for j in range(n):
        j2 = (j + 1) % n
        b._f([rings[-1][j], rings[-1][j2], top_in[j2], top_in[j]], NICHE, False)
    # pocket: sloped walls down to the floor
    floor_z = ztop - pocket_depth
    mid = [b._v((x, y, ztop - .006)) for (x, y) in poly_offset(outline, pocket_d)]
    floor = [b._v((x, y, floor_z)) for (x, y) in poly_offset(outline, pocket_d - .012)]
    for j in range(n):
        j2 = (j + 1) % n
        b._f([top_in[j], top_in[j2], mid[j2], mid[j]], NICHE, False)
        b._f([mid[j], mid[j2], floor[j2], floor[j]], NICHE, False)
    b._f(list(reversed(floor)), NICHE, False)
    # worn gilt: a raised strip along the lip's inner edge and a thin fillet band round the neck of the shaft
    def strip(d_in, d_out, z, h=.0025):
        a_ = [b._v((x, y, z + h)) for (x, y) in poly_offset(outline, d_in)]
        c_ = [b._v((x, y, z + h)) for (x, y) in poly_offset(outline, d_out)]
        a0 = [b._v((x, y, z)) for (x, y) in poly_offset(outline, d_in)]
        c0 = [b._v((x, y, z)) for (x, y) in poly_offset(outline, d_out)]
        for j in range(n):
            j2 = (j + 1) % n
            b._f([a_[j], a_[j2], c_[j2], c_[j]], GILT, False)
            b._f([a0[j], a0[j2], a_[j2], a_[j]], GILT, False)
            b._f([c_[j], c_[j2], c0[j2], c0[j]], GILT, False)
    strip(-.0225, -.0165, ztop)
    strip(-.0055, -.0005, ztop)
    # rune part: flat strokes just above the floor
    b.part("Rune", (0, 0, 0))
    rune_fn(b, floor_z + .002)
    return b

def rune_stroke(b, c, length, ang, zt, w=.007, h=.0025):
    with b.xf(T(c[0], c[1], zt) @ Rz(ang)):
        b.box((0, 0, 0), (w, length, h), RUNE)

# angular futhark-style glyphs: each is a list of segments in a unit box (x -.5..+.5, y -.5..+.5, staves along y)
GLYPHS = [
    [((0, -.5), (0, .5)), ((0, .5), (.45, .15)), ((0, .1), (.45, -.25))],                         # fehu-like
    [((-.25, -.5), (-.25, .5)), ((-.25, .5), (.25, .1)), ((.25, .1), (.25, -.5))],                # uruz-like
    [((0, -.5), (0, .5)), ((0, .35), (.4, .1)), ((.4, .1), (0, -.15))],                           # thurisaz-like
    [((-.2, -.5), (-.2, .5)), ((-.2, .5), (.3, .25)), ((.3, .25), (-.2, 0)), ((-.2, 0), (.3, -.5))],   # raido-like
    [((.3, .5), (-.3, 0)), ((-.3, 0), (.3, -.5))],                                                # kaunan-like
    [((0, -.5), (0, .5)), ((0, .5), (-.4, 0)), ((0, .5), (.4, 0))],                               # algiz-like
    [((0, -.5), (0, .5)), ((-.35, .35), (0, 0)), ((.35, .35), (0, 0)), ((-.2, -.2), (.2, -.2))],   # mixed
]

def glyph(b, c, size, gi, z0, rot=0.0, w=.0075):
    r = math.radians(rot); cr, sr = math.cos(r), math.sin(r)
    def tp(p): return (c[0] + (p[0] * cr - p[1] * sr) * size, c[1] + (p[0] * sr + p[1] * cr) * size)
    for (p, q) in GLYPHS[gi % len(GLYPHS)]:
        a_, b__ = tp(p), tp(q)
        ln = math.hypot(b__[0] - a_[0], b__[1] - a_[1]); ang = math.degrees(math.atan2(b__[1] - a_[1], b__[0] - a_[0])) - 90
        rune_stroke(b, ((a_[0] + b__[0]) / 2, (a_[1] + b__[1]) / 2), ln + w * .6, ang, z0, w=w)

def rune_blade(b, z0):
    for i, y in enumerate((-.26, -.13, 0.0, .13, .24)):
        glyph(b, (0, y), .095, i, z0, 0)
    rune_stroke(b, (-.06, 0), .5, 0, z0, w=.004); rune_stroke(b, (.06, 0), .5, 0, z0, w=.004)

def rune_hilt(b, z0):
    glyph(b, (0, 0), .20, 5, z0, 0, w=.010)
    for k, (cx, cy, rot) in enumerate(((-.15, 0, 90), (.15, 0, -90), (0, .17, 0), (0, -.17, 180))):
        glyph(b, (cx, cy), .06, k, z0, rot, w=.006)

def rune_moon(b, z0):
    for i in range(12):
        a = math.tau * i / 12
        glyph(b, (math.cos(a) * .125, math.sin(a) * .125), .052, i, z0, math.degrees(a) - 90, w=.0055)
    for i in range(24):
        a = math.tau * (i + .5) / 24
        rune_stroke(b, (math.cos(a) * .075, math.sin(a) * .075), .02, math.degrees(a) + 90, z0, w=.0035)
    glyph(b, (0, 0), .06, 6, z0, 0, w=.006)

BLADE_OUTLINE = [(-.11, -.33), (.11, -.33), (.11, .20), (0, .33), (-.11, .20)]
HILT_OUTLINE = [(-.095, -.25), (.095, -.25), (.095, -.085), (.22, -.085), (.22, .085), (.095, .085), (.095, .25), (-.095, .25), (-.095, .085), (-.22, .085), (-.22, -.085), (-.095, -.085)]

# ------------------------------------------------------------------------------------------------------------------ relic tablet (round 13)
def niche_on(b, outline, c, z0, boss=.050, pocket_d=-.046, depth=.036, rim=-.026):
    """A shallow carved boss on the tablet: a low moulded rim (about 5 cm proud) around a pocket that follows the relic's outline,
    sloped walls and a floor 1 cm below the tablet face. Gilt lip. Returns the floor height. `c` is the niche centre on the tablet."""
    cx, cy = c
    P = lambda d: [(x + cx, y + cy) for (x, y) in poly_offset(outline, d)]
    n = len(outline)
    prof = [(0, 0), (0, .012), (-.006, .020), (-.006, boss - .014), (-.0015, boss - .006), (0, boss)]
    rings = [[b._v((x, y, z0 + z)) for (x, y) in P(d)] for (d, z) in prof]
    for i in range(len(prof) - 1):
        for j in range(n):
            j2 = (j + 1) % n
            b._f([rings[i][j], rings[i][j2], rings[i + 1][j2], rings[i + 1][j]], NICHE, False)
    ztop = z0 + boss
    top_in = [b._v((x, y, ztop)) for (x, y) in P(rim)]
    for j in range(n):
        j2 = (j + 1) % n
        b._f([rings[-1][j], rings[-1][j2], top_in[j2], top_in[j]], NICHE, False)
    mid = [b._v((x, y, ztop - .008)) for (x, y) in P(pocket_d)]
    floor_z = z0 + boss - depth
    floor = [b._v((x, y, floor_z)) for (x, y) in P(pocket_d - .012)]
    for j in range(n):
        j2 = (j + 1) % n
        b._f([top_in[j], top_in[j2], mid[j2], mid[j]], NICHE, False)
        b._f([mid[j], mid[j2], floor[j2], floor[j]], NICHE, False)
    b._f(list(reversed(floor)), NICHE, False)
    # worn gilt strip on the lip
    d_in, d_out, h = rim - .0005, rim + .0075, .0025
    a_ = [b._v((x, y, ztop + h)) for (x, y) in P(d_in)]; c_ = [b._v((x, y, ztop + h)) for (x, y) in P(d_out)]
    a0 = [b._v((x, y, ztop)) for (x, y) in P(d_in)]; c0 = [b._v((x, y, ztop)) for (x, y) in P(d_out)]
    for j in range(n):
        j2 = (j + 1) % n
        b._f([a_[j], a_[j2], c_[j2], c_[j]], GILT, False)
        b._f([a0[j], a0[j2], a_[j2], a_[j]], GILT, False)
        b._f([c_[j], c_[j2], c0[j2], c0[j]], GILT, False)
    return floor_z

def relic_tablet():
    """One carved reliquary slab for the three relics (replaces three tall free-standing niches that read as tins): a moulded stone
    tablet 0.88 x 2.04 m, 7 cm thick, a beaded gilt border, corner roundels, and three low carved bosses with pockets in the outlines
    of the blade, the hilt (cross) and the moonstone, centred on tablet z -0.65, 0, +0.65 as the old sockets were. Parts 'RuneBlade',
    'RuneHilt', 'RuneMoon' hold the glyph strokes SocketRune lights. Origin: centre of the underside."""
    b = Builder("RelicTablet")
    W, L, T0 = .44, 1.02, .070
    rect = [(-W, -L), (W, -L), (W, L), (-W, L)]
    # two-stage moulded plinth: full footprint to 3 cm, then inset 2 cm to 7 cm with a chamfer
    prof = [(0, 0), (0, .030), (-.012, .036), (-.020, .042), (-.020, T0 - .008), (-.012, T0)]
    rings = [[b._v((x, y, z)) for (x, y) in poly_offset(rect, d)] for (d, z) in prof]
    for i in range(len(prof) - 1):
        for j in range(4):
            j2 = (j + 1) % 4
            b._f([rings[i][j], rings[i][j2], rings[i + 1][j2], rings[i + 1][j]], NICHE, False)
    b._f(list(reversed([b._v((x, y, 0)) for (x, y) in rect])), NICHE, False)
    b._f([b._v((x, y, T0)) for (x, y) in poly_offset(rect, -.032)], NICHE, False)
    # gilt border bead and an inner fillet on the top face
    for d_in, d_out in ((-.040, -.048), (-.072, -.076)):
        a_ = [b._v((x, y, T0 + .003)) for (x, y) in poly_offset(rect, d_in)]; c_ = [b._v((x, y, T0 + .003)) for (x, y) in poly_offset(rect, d_out)]
        a0 = [b._v((x, y, T0)) for (x, y) in poly_offset(rect, d_in)]; c0 = [b._v((x, y, T0)) for (x, y) in poly_offset(rect, d_out)]
        for j in range(4):
            j2 = (j + 1) % 4
            b._f([a_[j], a_[j2], c_[j2], c_[j]], GILT, False)
            b._f([a0[j], a0[j2], a_[j2], a_[j]], GILT, False)
            b._f([c_[j], c_[j2], c0[j2], c0[j]], GILT, False)
    # corner roundels: low stone discs with a gilt ring
    for sx in (-1, 1):
        for sy in (-1, 1):
            c = (sx * (W - .105), sy * (L - .105))
            ring_pts = [(x + c[0], y + c[1]) for (x, y) in circle_pts(.052, 14)]
            rr = [[b._v((x, y, T0 + z)) for (x, y) in [(px + c[0], py + c[1]) for (px, py) in poly_offset(circle_pts(.052, 14), d)]] for (d, z) in ((0, 0), (0, .010), (-.008, .018), (-.020, .020))]
            for i in range(3):
                for j in range(14):
                    j2 = (j + 1) % 14
                    b._f([rr[i][j], rr[i][j2], rr[i + 1][j2], rr[i + 1][j]], NICHE if i < 2 else GILT, False)
            b._f([rr[3][j] for j in range(14)], GILT, False)
    floors = {}
    floors["Blade"] = niche_on(b, BLADE_OUTLINE, (0, -.65), T0)
    floors["Hilt"] = niche_on(b, HILT_OUTLINE, (0, 0), T0, pocket_d=-.040)
    floors["Moon"] = niche_on(b, circle_pts(.20), (0, .65), T0)
    for name, fn, c in (("RuneBlade", rune_blade, (0, -.65)), ("RuneHilt", rune_hilt, (0, 0)), ("RuneMoon", rune_moon, (0, .65))):
        b.part(name, (0, 0, 0))
        with b.xf(T(c[0], c[1], 0)):
            fn(b, floors[name[4:]] + .002)
    return b

# ------------------------------------------------------------------------------------------------------------------ giant skeleton
BS = "Castle_BlackStone"

def catmull(ctrl, y):
    ys = [c[0] for c in ctrl]
    if y <= ys[0]: return ctrl[0][1]
    if y >= ys[-1]: return ctrl[-1][1]
    i = max(k for k in range(len(ys) - 1) if ys[k] <= y)
    t = (y - ys[i]) / (ys[i + 1] - ys[i])
    p0 = ctrl[max(i - 1, 0)][1]; p1 = ctrl[i][1]; p2 = ctrl[i + 1][1]; p3 = ctrl[min(i + 2, len(ctrl) - 1)][1]
    return .5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3)

def loft_rings(b, c, sections, sides, mat, smooth=True):
    """sections: [(y, rx, rz, zc)] elliptical rings along Y, centred on c (same as castle_props.loft)."""
    rings = []
    for (y, rx, rz, zc) in sections:
        rings.append([b._v((c[0] + rx * math.cos(j * math.tau / sides), c[1] + y, c[2] + zc + rz * math.sin(j * math.tau / sides))) for j in range(sides)])
    for i in range(len(rings) - 1):
        for j in range(sides):
            j2 = (j + 1) % sides
            b._f([rings[i][j], rings[i][j2], rings[i + 1][j2], rings[i + 1][j]], mat, smooth)
    b._f(list(reversed(rings[0])), mat); b._f(list(rings[-1]), mat)

def giant_skeleton():
    """Complete giant carcass, 12 m nose to tail along Y (head +Y), ribcage 3.0 m across (x -1.45 .. +1.54 about the spine) and 2.85 m
    high, 17 rib pairs, 48 vertebrae with neural spines, scapulae, pelvis, limbs, a half-sunk skull with the jaw hanging open, and
    loose bone on the floor. Origin: floor under the pelvis, spine on x = 0. Bone material (grimed) plus black eye sockets.
    CastleWorld places it with its spine at x -4.58 so the east rib tips stop at x -3.03 (CastleLayout.SkeletonMaxX = -3.0)."""
    rnd = random.Random(1313)
    b = Builder("GiantSkeleton")
    ctrl = [(-6.0, .14), (-4.5, .36), (-3.0, .85), (-1.5, 1.62), (0.0, 2.22), (1.2, 2.34), (2.5, 2.12), (3.6, 1.72), (4.4, 1.18), (4.9, .78), (5.4, .36)]
    def sx(y): return .9 * math.sin((y + 6) * .55) * max(0.0, (-1.6 - y) / 4.4)
    def spine_pt(y): return Vector((sx(y), y, catmull(ctrl, y)))
    # vertebrae: hourglass centrum, tilted neural spine, transverse processes
    ys = [-5.9 + i * .222 for i in range(int((5.15 + 5.9) / .222) + 1)]
    for idx, y in enumerate(ys):
        c = spine_pt(y); dz = (catmull(ctrl, y + .05) - catmull(ctrl, y - .05)) / .1
        pitch = math.degrees(math.atan(dz))
        r = .20 if -1.8 < y < 3.4 else max(.06, .20 - (-1.8 - y) * .030 if y <= -1.8 else .14)
        with b.xf(T(*c) @ Rx(pitch - 90)):
            b.lathe((0, 0, 0), [(r * .86, -.105), (r * 1.08, -.07), (r * .82, 0), (r * 1.08, .07), (r * .86, .105)], 8, GBONE, smooth=True)
        if -2.4 < y < 3.9:
            h = (.33 if -1.0 < y < 3.0 else .22) * (1 + rnd.uniform(-.1, .1))
            top = Vector((c.x, c.y - .09, c.z + r * .6 + h))
            sweep(b, [c + Vector((0, 0, r * .7)), c + Vector((0, -.03, r * .7 + h * .5)), top], [(.020, .055), (.016, .045), (.010, .030)], GBONE, sides=4, up=(0, 1, 0), tile=.8, cap_end=True)
        if y > -3.4:
            for side in (-1, 1):
                w = (.30 if -1.8 < y < 3.4 else .18)
                sweep(b, [c + Vector((side * r * .8, 0, 0)), c + Vector((side * (r + w * .6), 0, .02)), c + Vector((side * (r + w), .01, -.03))],
                      [(.026, .028), (.020, .022), (.012, .014)], GBONE, sides=5, up=(0, 1, 0), tile=.8, cap_end=True)
    # ribs: 17 pairs wrapped from the spine down to the floor; some broken short; flat bands (wide along the body axis)
    for k in range(17):
        yk = -1.5 + k * .295
        env = math.sin(math.pi * (k + 1.55) / 19.2)
        z0 = catmull(ctrl, yk) - .04
        Rz_ = min(1.42 * env, (z0 - .06) / 1.84)
        for side in (-1, 1):
            Rx_ = (1.36 if side > 0 else 1.28) * env
            npts = 12 if rnd.random() > .2 else rnd.choice([6, 8, 9])
            pts = []; rad = []
            for i in range(npts):
                phi = .10 + (2.55 - .10) * i / 11
                x = sx(yk) + side * (.15 + Rx_ * math.sin(phi)) + rnd.uniform(-.015, .015)
                z = z0 - Rz_ * (1 - math.cos(phi))
                pts.append((x, yk + rnd.uniform(-.02, .02) + .12 * (i / 11) * (1 if k % 2 else .6), max(.04, z)))
                f = i / 11
                rad.append((lerp(.052, .022, f ** .8), lerp(.100, .042, f ** .8)))
            sweep(b, pts, rad, GBONE, sides=6, up=(0, 1, 0), tile=.8, cap_end=True)
    # pelvis plates and shoulder blades
    for side in (-1, 1):
        sweep(b, [Vector((side * .18, -2.0, 1.78)), Vector((side * .55, -1.98, 1.55)), Vector((side * .95, -1.85, 1.15))], [(.20, .030), (.24, .028), (.16, .024)], GBONE, sides=6, up=(0, 0, 1), tile=.8, cap_end=True)
        sweep(b, [Vector((side * .18, -2.05, 1.7)), Vector((side * .38, -2.4, .90)), Vector((side * .5, -2.45, .35))], [(.05, .06), (.04, .05), (.03, .04)], GBONE, sides=6, up=(1, 0, 0), tile=.8, cap_end=True)
        sweep(b, [Vector((side * .22, 2.2, 2.28)), Vector((side * .62, 2.35, 1.80)), Vector((side * .95, 2.55, 1.30))], [(.20, .026), (.23, .024), (.12, .020)], GBONE, sides=6, up=(0, 0, 1), tile=.8, cap_end=True)
    # limbs (humerus/femur, shin, paw with three claws)
    for (yy, zz, leg) in ((2.4, 1.55, "front"), (-2.1, 1.45, "hind")):
        for side in (-1, 1):
            hip = Vector((side * .62, yy, zz))
            knee = Vector((side * 1.12, yy + (.45 if leg == "front" else -.35), .85))
            paw = Vector((side * 1.22, yy + (1.0 if leg == "front" else .5), .07))
            sweep(b, [hip, hip + (knee - hip) * .5 + Vector((side * .04, 0, .05)), knee, knee + (paw - knee) * .5, paw], [(.115, .105), (.085, .08), (.085, .08), (.06, .055), (.07, .06)], GBONE, sides=7, up=(0, 1, 0), tile=.8, cap_end=True)
            for cl in (-.1, 0, .1):
                b.tube([(paw.x + cl, paw.y, paw.z), (paw.x + cl * 1.5, paw.y + .24, paw.z - .02), (paw.x + cl * 1.7, paw.y + .42, paw.z - .06)], [.035, .026, 0], 5, GBONE)
    # skull: ~1.6 m, half sunk into the floor, snout down, yawed 40 deg about the occiput so S1 sees a three-quarter head
    sc = (.05, 5.62, .58)
    with b.xf(T(*sc) @ about((0, -.75, 0), Rz(-40)) @ Rx(-20)):
        # wave FD (r13 critic: "the giant skull from the east is a dark low-poly lump"): 22-sided rings and twice the sections (was 12 x 9)
        loft_rings(b, (0, 0, 0), [(-.80, .10, .12, .30), (-.74, .19, .20, .31), (-.68, .25, .26, .32), (-.57, .31, .30, .33), (-.45, .34, .32, .34),
                                  (-.33, .345, .31, .32), (-.22, .32, .27, .28), (-.10, .285, .24, .245), (.02, .24, .21, .21), (.16, .21, .185, .18),
                                  (.30, .18, .16, .15), (.44, .155, .14, .13), (.58, .13, .12, .11), (.70, .11, .105, .095), (.80, .09, .09, .08),
                                  (.84, .05, .05, .07)], 22, GBONE)
        # wave FD: a pair of ridged horns sweeping back from the cranium, so the head reads as a beast skull from the east and from behind
        for sx_ in (-1, 1):
            hp = [(sx_ * .26, -.52, .52), (sx_ * .46, -.78, .78), (sx_ * .64, -1.12, .98), (sx_ * .70, -1.48, 1.08), (sx_ * .60, -1.80, 1.06), (sx_ * .44, -1.98, .94)]
            b.tube(hp, [.15, .13, .105, .08, .05, 0], 10, GBONE)
            for i in range(1, 5):                                   # growth rings
                q = Vector(hp[i]); dq = (Vector(hp[i + 1]) - Vector(hp[i - 1])).normalized()
                ring = [q + (dq.cross(Vector((0, 0, 1))).normalized() * math.cos(a_) + dq.cross(dq.cross(Vector((0, 0, 1)))).normalized() * math.sin(a_)) * ([.15, .13, .105, .08, .05][i] + .012) for a_ in [k * math.tau / 10 for k in range(10)]]
                b.tube([tuple(v) for v in ring], .012, 4, GBONE, closed=True)
        # brow ridges over the sockets
        for sx_ in (-1, 1):
            b.tube([(sx_ * .08, -.30, .47), (sx_ * .24, -.22, .44), (sx_ * .38, -.10, .36)], [.05, .055, .035], 7, GBONE)
        # sagittal crest
        sweep(b, [(0, -.75, .62), (0, -.40, .66), (0, -.10, .50), (0, .25, .38)], [(.020, .080), (.026, .090), (.020, .070), (.012, .05)], GBONE, sides=4, up=(1, 0, 0), tile=.8, cap_end=True)
        for sx_ in (-1, 1):
            b.tube([(sx_ * .12, -.02, .50), (sx_ * .27, -.15, .50), (sx_ * .34, -.32, .42)], [.045, .055, .04], 6, GBONE)
            ring = [(sx_ * .30, -.14 + .12 * math.cos(a_), .27 + .12 * math.sin(a_)) for a_ in [i * math.tau / 10 for i in range(10)]]
            b.tube(ring, .032, 5, GBONE, closed=True)
            with b.xf(T(sx_ * .322, -.14, .27) @ Ry(90 * sx_)):          # eye socket: a black disc just proud of the cranium, inside the bone ring
                b.lathe((0, 0, 0), [(0, .022), (.075, .016), (.105, -.010)], 10, BS, smooth=False)
            with b.xf(T(sx_ * .075, .60, .235) @ Ry(70 * sx_)):         # nasal opening
                b.lathe((0, 0, 0), [(0, .012), (.04, .008), (.055, -.004)], 8, BS, smooth=False)
            b.tube([(sx_ * .33, -.38, .14), (sx_ * .36, -.15, .08), (sx_ * .24, .18, .08)], [.035, .04, .03], 5, GBONE)
            for i in range(6):
                yy = .02 + i * .13
                b.tube([(sx_ * (.13 - i * .010), yy, .03), (sx_ * (.13 - i * .010), yy, -.09 - (.03 if i in (2, 3) else 0))], [.028, 0], 5, GBONE)
            b.tube([(sx_ * .10, .50, .04), (sx_ * .10, .52, -.24)], [.034, 0], 5, GBONE)
        with b.xf(about((0, -.42, .10), Rx(-24))):
            loft_rings(b, (0, 0, 0), [(-.46, .22, .08, .02), (-.20, .18, .07, -.01), (.20, .13, .055, -.03), (.62, .08, .045, -.035), (.80, .05, .04, -.035)], 8, GBONE)
            for sx_ in (-1, 1):
                b.tube([(sx_ * .20, -.40, .05), (sx_ * .24, -.46, .30)], [.05, .04], 5, GBONE)
                for i in range(5):
                    yy = .04 + i * .15
                    b.tube([(sx_ * (.10 - i * .008), yy, .0), (sx_ * (.10 - i * .008), yy, .11)], [.026, 0], 5, GBONE)
    # wave FD (r13 critic: "a tan quadruped that reads like a museum dinosaur, not a draped corpse"): a torn oxblood shroud laid over the spine
    # and hanging down the ribs in three ragged panels with a jagged hem and holes. Two-sided (each face twice, 6 mm apart), clear of the
    # neural spines (+0.06) and the rib bands (+0.09), inside the measured carcass bounds.
    b.part("Shroud", (0, 0, 0))
    drnd = random.Random(2718)
    CLOTH = "Castle_Oxblood"
    def env_at(y): return math.sin(math.pi * ((y + 1.5) / .295 + 1.55) / 19.2)
    for (ya, yb_) in ((-1.15, .05), (.22, 1.12), (1.30, 2.02)):
        ny = max(3, int((yb_ - ya) / .09))
        nphi = 11
        hem = [drnd.uniform(1.05, 2.25) for _ in range(ny + 1)]
        for k_ in range(1, ny): hem[k_] = .5 * hem[k_] + .25 * (hem[k_ - 1] + hem[k_ + 1])          # ragged, not noisy
        grid = {}
        def cp(i, j, side):
            y = ya + (yb_ - ya) * i / ny
            e = env_at(y); z0 = catmull(ctrl, y) - .04
            Rx_ = 1.33 * e + .09; Rz_ = min(1.42 * e, (z0 - .06) / 1.84)
            phi = hem[i] * j / (nphi - 1)
            if j == 0: return (0.0, y, catmull(ctrl, y) + .52)
            x = side * (.15 + Rx_ * math.sin(phi)); z = z0 + .07 - Rz_ * (1 - math.cos(phi))
            if j == 1: z = max(z, catmull(ctrl, y) + .30)                               # over the transverse processes
            sag = .035 * math.sin(i * 2.1 + j * .7)
            return (x + side * sag, y, max(.03, z - abs(sag)))
        for side in (-1, 1):
            V_ = [[b._v(cp(i, j, side)) for j in range(nphi)] for i in range(ny + 1)]
            W_ = [[b._v(Vector(cp(i, j, side)) + Vector((side * .006, 0, .004))) for j in range(nphi)] for i in range(ny + 1)]
            for i in range(ny):
                for j in range(nphi - 1):
                    if j > 2 and drnd.random() < .09: continue                              # holes
                    if j >= nphi - 2 and drnd.random() < .35: continue                      # torn hem
                    uv = [(j * .17, (ya + (yb_ - ya) * i / ny)), ((j + 1) * .17, (ya + (yb_ - ya) * i / ny)), ((j + 1) * .17, (ya + (yb_ - ya) * (i + 1) / ny)), (j * .17, (ya + (yb_ - ya) * (i + 1) / ny))]
                    q = [V_[i][j], V_[i][j + 1], V_[i + 1][j + 1], V_[i + 1][j]]
                    r_ = [W_[i][j], W_[i][j + 1], W_[i + 1][j + 1], W_[i + 1][j]]
                    if side < 0: q = q[::-1]; r_ = r_[::-1]; uv = uv[::-1]
                    b._f(q, CLOTH, True, uv, False)
                    b._f(r_[::-1], CLOTH, True, uv[::-1], False)
    b.part("GiantSkeleton", (0, 0, 0))
    # loose bone on the floor (x kept inside the east limit): fallen ribs, vertebrae, a jaw splinter
    for i in range(5):
        y0 = rnd.uniform(-1.2, 4.2); x0 = rnd.uniform(-.9, .9)
        ang = math.pi / 2 * rnd.choice([-1, 1]) + rnd.uniform(-.5, .5); L = rnd.uniform(.5, .9)
        pts = [(x0 + math.cos(ang) * L * t, y0 + math.sin(ang) * L * t, .05 + .03 * math.sin(t * math.pi)) for t in (0, .25, .5, .75, 1)]
        sweep(b, pts, [(.03, .05), (.034, .06), (.032, .055), (.025, .045), (.014, .03)], GBONE, sides=6, up=(0, 0, 1), tile=.8, cap_end=True)
    for i in range(6):
        c = (rnd.uniform(-1.0, 1.1), rnd.uniform(-2.5, 4.4), .10)
        with b.xf(T(*c) @ Rz(rnd.uniform(0, 360)) @ Rx(rnd.uniform(60, 100))):
            b.lathe((0, 0, 0), [(.10, -.07), (.12, 0), (.10, .07)], 7, GBONE, smooth=True)
    return b

BUILDERS = [
    # (name, make, tris budget)
]

def register():
    for v in range(6):
        BUILDERS.append(("HandRoot%d" % v, (lambda v=v: hand_root(v)), 4000))
    BUILDERS.append(("RootCrown", root_crown, 1200))
    BUILDERS.append(("FloatingBook", lambda: book_open(1.0, LEATHER_A, "FloatingBook"), 3000))
    BUILDERS.append(("FloatingBookSmall", lambda: book_open(.72, LEATHER_B, "FloatingBookSmall"), 3000))
    BUILDERS.append(("FloatingBookLarge", lambda: book_open(1.32, LEATHER_C, "FloatingBookLarge"), 3000))
    BUILDERS.append(("GiantSkeleton", giant_skeleton, 40000))
    BUILDERS.append(("RelicSocketBlade", lambda: carved_socket("RelicSocketBlade", BLADE_OUTLINE, -.046, rune_blade), 3500))
    BUILDERS.append(("RelicSocketHilt", lambda: carved_socket("RelicSocketHilt", HILT_OUTLINE, -.040, rune_hilt), 3500))
    BUILDERS.append(("RelicSocketMoon", lambda: carved_socket("RelicSocketMoon", circle_pts(.20), -.046, rune_moon), 3500))
    BUILDERS.append(("RelicTablet", relic_tablet, 6000))
    for tag, sc_, lea in (("TomeSmall", .72, LEATHER_B), ("Tome", 1.0, LEATHER_A), ("TomeLarge", 1.34, LEATHER_C)):
        BUILDERS.append((tag, (lambda tag=tag, sc_=sc_, lea=lea: book_closed(sc_, lea, tag, clasp=(tag == "TomeLarge"), dims=(.22, .30, .075))), 1500))
    BUILDERS.append(("BookClosedSmall", lambda: book_closed(.72, LEATHER_B, "BookClosedSmall"), 1000))
    BUILDERS.append(("BookClosed", lambda: book_closed(1.0, LEATHER_A, "BookClosed"), 1000))
    BUILDERS.append(("BookClosedLarge", lambda: book_closed(1.5, LEATHER_C, "BookClosedLarge", clasp=True), 1000))

def write_fbx_meta(name):
    """New FBX files get a .meta cloned from FloatingBook.fbx.meta (same importer settings) with a path-derived guid, so the integrator's
    first import keeps them stable; existing metas (and their guids) are never touched."""
    meta = cc.CASTLE / (name + ".fbx.meta")
    if meta.exists(): return
    tmpl = (cc.CASTLE / "Pew.fbx.meta").read_text().splitlines()
    tmpl[1] = "guid: " + cc.guid(cc.rel_to_project(cc.CASTLE / (name + ".fbx")))
    meta.write_text("\n".join(tmpl) + "\n")

def main():
    register()
    wanted = only()
    reset_scene()
    for name, make, budget in BUILDERS:
        if wanted and name not in wanted: continue
        export_asset(make(), budget)
        write_fbx_meta(name)
    if wanted:   # partial run: merge into the committed manifest instead of replacing it
        import json
        mp = cc.ROOT / "docs/haunted-castle/art/manifest_props_r12.json"
        old = json.loads(mp.read_text()) if mp.exists() else {}
        old.update(MANIFEST); MANIFEST.clear(); MANIFEST.update(old)
    write_manifest("props_r12")
    print("HC_PROPS_R12_PASS", len(MANIFEST))

if __name__ == "__main__":
    main()
