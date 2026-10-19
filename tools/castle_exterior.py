#!/usr/bin/env python3
"""Haunted Castle exterior kit (round 12, hc/art-exterior). Headless, idempotent, no Unity, no GPU:

    /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/castle_exterior.py [-- --proof DIR] [--no-export]

Exports Assets/HollowCurrent/Resources/Castle/Ext*.fbx (every piece is authored IN CASTLE-LOCAL COORDINATES, so
CastleExterior.Build places each piece at the castle root origin with identity rotation) and
docs/haunted-castle/art/manifest_exterior.json (measured size, triangle count and part list per piece).
--proof DIR renders the offline proof images (Blender Workbench composited over the real sky, tools/castle_exterior_proof.py).

Authoring space: all helpers take UNITY castle-local coordinates (x right, y up, z forward: footprint x +-14, z 0..28,
gate wall on z = 0, +z into the castle). Round 12 polish (axis fix): the FBX exporter (axis_forward=-Z, axis_up=Y) and Unity's importer
together map Blender (x, y, z) to Unity (-x, z, -y), a 180 degree turn about the vertical axis relative to the (x, z, y) swap the first
version of this kit assumed (CastleExterior.AxisCheck caught it: keep centre local z -19.5). So U() now writes Blender (-x, -z, y): the
piece lands in Unity exactly where it is authored, with no runtime correction (the check stays).

Gameplay contract: NOTHING here has a collider (CastleExterior never adds one), nothing stands inside the gate-wall-top standing
spot (3.6, 7.5, 0), the bridge corridor |x| < 2.4 for z < 0, or any interior room volume (check_clear()).
"""
import sys, math
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from castle_lib import *
import castle_common as cc

# ---- materials -----------------------------------------------------------------------------------------------------------
# Existing castle materials are reused (CastleExterior re-skins the stone slots with the same world-space surfaces the veneer uses).
# Two names are new: Castle_ExtGlow (unlit warm emission) and Castle_ExtBackdrop (the distant-ridge shader).
cc.MATERIALS.update({"Castle_Basalt": ((.16, .17, .21), .22, 0.0, True), "Castle_ExtWindow": ((.85, .58, .23), 0.0, 0.0, False), "Castle_ExtBackdrop": ((.05, .07, .12), 0.0, 0.0, False)})
UV_TILE.update({"Castle_Basalt": 3.3, "Castle_StoneDark": 2.0, "Castle_StoneWeathered": 2.0, "Castle_StoneMossy": 2.0, "Castle_FlagStone": 2.0})
ST, SW, SD, SM, SL = "Castle_Stone", "Castle_StoneWeathered", "Castle_StoneDark", "Castle_StoneMossy", "Castle_BlackStone"
BS = "Castle_Basalt"
IRON, OX, GLOW, BACK = "Castle_Iron", "Castle_Oxblood", "Castle_ExtWindow", "Castle_ExtBackdrop"
import castle_exterior_atlas as ATL

def only():
    a = sys.argv
    return a[a.index("--") + 1:] if "--" in a else []
ARGS = only()

def U(p): return (-p[0], -p[2], p[1])        # Unity (x, y, z) -> Blender (-x, -z, y); export + import turn it back to Unity (x, y, z)
def S(p): return (p[0], p[2], p[1])          # sizes: no sign change

# ---- shared constants (castle-local, must match CastleWorld.cs) ----------------------------------------------------------
GATE_Z = -.45                       # outer face of the gate wall (wall z -0.45..+0.45); the front veneer adds ~0.125 m of relief
WALL_TOP_FRONT, WALL_TOP_GATE = 6.4, 7.5
STAND = (3.6, 7.5, 0.0)             # Points.GatehouseBattlement (feet)
RECT = (-14.45, 14.45, -.45, 28.45)  # outer faces of the building shell

class K:
    """Builder wrapper that takes Unity castle-local coordinates."""
    def __init__(s, name): s.b = Builder(name)
    def box(s, c, sz, mat): s.b.box(U(c), S(sz), mat)
    def rbox(s, c, sz, yaw, mat):
        with s.b.xf(T(*U(c)) @ Rz(yaw)): s.b.box((0, 0, 0), S(sz), mat)
    def lathe(s, c, prof, seg, mat, smooth=False): s.b.lathe(U(c), prof, seg, mat, smooth, cap=False)
    def poly_xy(s, pts, z0, z1, mat): s.b.poly([(-x, y) for x, y in pts], -z1, -z0, mat, 'XZ')   # polygon (x, y) extruded along z
    def poly_zy(s, pts, x0, x1, mat): s.b.poly([(-z, y) for z, y in pts], -x1, -x0, mat, 'YZ')   # polygon (z, y) extruded along x
    def at(s, c, yaw): return s.b.xf(T(*U(c)) @ Rz(yaw))                 # local frame at c, yaw about vertical (x toward z)

# ---- primitives built from the basics ------------------------------------------------------------------------------------
def spire(k, c, r0, h, mat=SL, seg=12, eave=.2, rod=1.3, concave=1.0):
    """Steep slate cone with an eave flare, a concave taper and an iron tip rod. Closed solid (apex at both ends). c = base centre."""
    prof = [(0, 0), (r0 + eave, 0), (r0 + eave * .55, .1), (r0, .32)]
    n = 10
    for i in range(1, n):
        t = i / n
        prof.append((max(r0 * (1 - t) ** concave, .06), .32 + (h - .32) * t))
    prof += [(.05, h), (.04, h + rod), (0, h + rod + .3)]
    k.lathe(c, prof, seg, mat)

def drum(k, cx, cz, r, y0, y1, mat, seg=20, plinth=.0):
    prof = [(0, y0)]
    if plinth: prof += [(r + .12, y0), (r + .12, y0 + .5), (r, y0 + .62)]       # wave L: walls are vertical; a 12 cm batter only at the very foot
    else: prof += [(r, y0)]
    prof += [(r, y1), (0, y1)]
    k.lathe((cx, 0, cz), prof, seg, mat)

def corbel_ring(k, cx, cz, r, y, mat, seg=20, flare=.62, depth=1.15):
    """Machicolation flare: a stepped cone from the shaft radius out to r + flare over `depth`, closed on top."""
    prof = [(0, y), (r, y), (r + flare * .25, y + depth * .25), (r + flare * .6, y + depth * .55), (r + flare, y + depth * .8),
            (r + flare, y + depth), (0, y + depth)]
    k.lathe((cx, 0, cz), prof, seg, mat)

def merlons(k, cx, cz, R, y, n, w, h, t, mat, phase=0.0):
    for i in range(n):
        th = phase + 360.0 * i / n
        k.rbox((cx + R * math.cos(math.radians(th)), y + h / 2, cz + R * math.sin(math.radians(th))), (w, h, t), th + 90, mat)

def merlon_row(k, a, b, y, n, w, h, t, mat, axis='x'):
    """n merlons along a straight run a -> b (unity (x, z) pairs); thickness t perpendicular to the run."""
    for i in range(n):
        f = (i + .5) / n
        x = a[0] + (b[0] - a[0]) * f; z = a[1] + (b[1] - a[1]) * f
        k.box((x, y + h / 2, z), (w, h, t) if axis == 'x' else (t, h, w), mat)

def face(k, pts, outward, mat, uvs):
    """One-sided polygon (Unity castle-local points), wound so its normal points toward `outward`; explicit UVs."""
    b = k.b
    P = [Vector(U(p)) for p in pts]; out = Vector(U(outward)); uvs = list(uvs)
    n = Vector((0, 0, 0))
    for i in range(len(P)):                                       # Newell normal
        a, c = P[i], P[(i + 1) % len(P)]
        n += Vector(((a.y - c.y) * (a.z + c.z), (a.z - c.z) * (a.x + c.x), (a.x - c.x) * (a.y + c.y)))
    if n.dot(out) < 0: P.reverse(); uvs.reverse()
    b._f([b._v(tuple(p)) for p in P], mat, False, uvs=uvs, closed=False)

_win_count = [0]
def window(k, kind, pts2, to3d, outward):
    """Lit window from the atlas. pts2 = polygon (t, y) in the wall plane (t along the wall, y up); to3d(t, y) gives the Unity point."""
    name = kind
    if kind == "slit":
        name = "slit%d" % ((_win_count[0] * 3 + _win_count[0] // 4) % ATL.SLIT_VARIANTS)       # spread brightness variants
        _win_count[0] += 1
    u0, v0, u1, v1 = ATL.uv_rect(name); pad = 3 / ATL.SIZE
    u0 += pad; v0 += pad; u1 -= pad; v1 -= pad
    ts = [p[0] for p in pts2]; ys = [p[1] for p in pts2]
    tmin, tmax, ymin, ymax = min(ts), max(ts), min(ys), max(ys)
    uvs = [(u0 + (u1 - u0) * (t - tmin) / (tmax - tmin), v0 + (v1 - v0) * (y - ymin) / (ymax - ymin)) for t, y in pts2]
    face(k, [to3d(t, y) for t, y in pts2], outward, GLOW, uvs)

def lancet(k, along, y0, w, h, face_dir, plane):
    """Lit pointed window proud of a wall face by 0.03. face_dir N faces -z, S +z, W -x, E +x; plane = wall coordinate."""
    pts = [(along - w / 2, y0), (along + w / 2, y0), (along + w / 2, y0 + h * .72), (along, y0 + h), (along - w / 2, y0 + h * .72)]
    kind = "slit" if w < .6 else "lancet"
    if face_dir in "NS":
        z = plane - .03 if face_dir == "N" else plane + .03
        window(k, kind, pts, lambda t, y: (t, y, z), (0, 0, -1 if face_dir == "N" else 1))
    else:
        x = plane - .03 if face_dir == "W" else plane + .03
        window(k, kind, pts, lambda t, y: (x, y, t), (-1 if face_dir == "W" else 1, 0, 0))

def radial_slit(k, cx, cz, apothem, phi, y0, w, h):
    """Lit slit on the drum facet whose centre angle is phi (unity angle from +x toward +z); apothem = facet distance."""
    nx, nz = math.cos(math.radians(phi)), math.sin(math.radians(phi)); tx, tz = -nz, nx
    r = apothem + .03
    pts = [(-w / 2, y0), (w / 2, y0), (w / 2, y0 + h * .72), (0, y0 + h), (-w / 2, y0 + h * .72)]
    window(k, "slit", pts, lambda t, y: (cx + nx * r + tx * t, y, cz + nz * r + tz * t), (nx, 0, nz))

# ---- deterministic noise ---------------------------------------------------------------------------------------------------
def _h(ix, iy, seed):
    n = (ix * 374761393 + iy * 668265263 + seed * 1442695040888963407) & 0xFFFFFFFFFFFFFFFF
    n = ((n ^ (n >> 13)) * 1274126177) & 0xFFFFFFFF
    return ((n ^ (n >> 16)) & 0xFFFF) / 65535.0
def vnoise(x, y, seed=0):
    ix, iy = math.floor(x), math.floor(y); fx, fy = x - ix, y - iy
    fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy)
    a, b, c, d = _h(ix, iy, seed), _h(ix + 1, iy, seed), _h(ix, iy + 1, seed), _h(ix + 1, iy + 1, seed)
    return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy
def fbm(x, y, seed=0, octaves=4):
    s = 0; amp = .5; tot = 0
    for o in range(octaves):
        s += amp * vnoise(x, y, seed + o * 17); tot += amp; x *= 2.03; y *= 2.03; amp *= .5
    return s / tot
def smooth01(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a))); return t * t * (3 - 2 * t)

# =================================================================================================================================
# PIECE 1: the keep (replaces the old plain box and its cone)
# =================================================================================================================================
KEEP_C = (2.3, 20.4)       # centre (x, z) on the great-hall roof (roof top y 9.2, x +-6, z 14..28); off-axis so both moons clear it
KEEP_HALF = 2.8
KEEP_BASE, KEEP_TOP = 9.0, 32.0
KEEP_SIZE = (5.6, 23.0, 5.6)   # reported to the SetPiece tag (body)

def keep():
    k = K("ExtKeep")
    cx, cz = KEEP_C; h = KEEP_HALF; T = KEEP_TOP
    k.box((cx, (KEEP_BASE + T) / 2, cz), (2 * h, T - KEEP_BASE, 2 * h), SW)
    k.box((cx, KEEP_BASE + .5, cz), (2 * h + .5, 1.0, 2 * h + .5), SD)                      # plinth
    for y in (14.4, 19.8, 25.4):                                                              # string courses
        k.box((cx, y, cz), (2 * h + .4, .34, 2 * h + .4), SD)
    # stepped corner buttresses: three tiers, each with a cap slab and a pinnacle on the top tier
    tiers = [(KEEP_BASE, 16.0, 1.5, .5), (16.0, 23.5, 1.2, .36), (23.5, T - 2.0, .95, .22)]
    for sx in (-1, 1):
        for sz in (-1, 1):
            for (ya, yb, w, off) in tiers:
                px, pz = cx + sx * (h + off - w / 2 + .05), cz + sz * (h + off - w / 2 + .05)
                k.box((px, (ya + yb) / 2, pz), (w, yb - ya, w), SD)
                k.box((px, yb + .09, pz), (w + .16, .18, w + .16), SD)
                if yb > 20: k.lathe((px, yb + .18, pz), [(0, 0), (w * .42, 0), (w * .3, .5), (.05, 1.6), (0, 1.9)], 6, SD)   # pinnacle
    # machicolated crown: corbels under an overhanging slab, merlons on top
    y_c = T - 1.8
    for i in range(-3, 4):
        a = i * .85
        k.box((cx + a, y_c + .85, cz - h - .38), (.38, 1.7, .76), SD)
        k.box((cx + a, y_c + .85, cz + h + .38), (.38, 1.7, .76), SD)
        k.box((cx - h - .38, y_c + .85, cz + a), (.76, 1.7, .38), SD)
        k.box((cx + h + .38, y_c + .85, cz + a), (.76, 1.7, .38), SD)
    top_h = h + .85
    k.box((cx, T + .15, cz), (2 * top_h, .4, 2 * top_h), SW)                                  # overhang slab, top y T + .35
    for side in (-1, 1):
        merlon_row(k, (cx - top_h + .45, cz + side * (top_h - .22)), (cx + top_h - .45, cz + side * (top_h - .22)), T + .35, 6, .8, 1.15, .5, SW)
        merlon_row(k, (cx + side * (top_h - .22), cz - top_h + .45), (cx + side * (top_h - .22), cz + top_h - .45), T + .35, 6, .8, 1.15, .5, SW, axis='z')
    # four bartizan turrets on the crown corners (corbel cone, shaft, slate spire) of different heights
    spec = {(-1, -1): (T + 5.0, 10.5), (1, -1): (T + 3.4, 8.2), (-1, 1): (T + 2.4, 6.8), (1, 1): (T + 4.0, 9.0)}
    for (sx, sz), (top, sh) in spec.items():
        tx, tz = cx + sx * (top_h - .45), cz + sz * (top_h - .45)
        k.lathe((tx, 0, tz), [(0, T - 3.4), (.3, T - 3.4), (.55, T - 2.4), (.8, T - 1.4), (.95, T - .8), (0, T - .8)], 12, SD)
        drum(k, tx, tz, .8, T - .8, top, SW, seg=12)
        spire(k, (tx, top, tz), .95, sh, eave=.28)
    # central drum and the tall spire
    drum(k, cx, cz, 2.0, T + .3, T + 5.2, SW, seg=16)
    corbel_ring(k, cx, cz, 2.0, T + 3.2, SD, seg=16, flare=.4, depth=1.0)
    spire(k, (cx, T + 5.4, cz), 2.25, 14.5, eave=.4, seg=16, rod=1.6)   # wave FD: 11.5 -> 14.5
    # lit windows: courtyard/gate face (-z) and back face (+z), then the sides
    zf, zb = cz - h, cz + h
    xs = h - 1.35
    for (face, zc, sgn) in (('N', zf, -1), ('S', zb, 1)):
        lancet(k, cx, 17.0, 1.1, 6.2, face, zc)
        for xo in (-xs, xs):
            for y0 in (17.6, 22.6): lancet(k, cx + xo, y0, .34, 1.9, face, zc)
        for xo in (-1.0, 1.0): lancet(k, cx + xo, 27.0, .34, 1.9, face, zc)
        for sx in (-1, 1): k.box((cx + sx * .72, 20.3, zc + sgn * .05), (.2, 6.6, .16), SD)    # jambs
        k.box((cx, 23.3, zc + sgn * .05), (1.7, .22, .16), SD)
    for (face, xc) in (('W', cx - h), ('E', cx + h)):
        for zo in (-1.2, 0.0, 1.2): lancet(k, cz + zo, 17.6, .34, 1.9, face, xc)
        for zo in (-0.7, 0.7): lancet(k, cz + zo, 22.6, .34, 1.9, face, xc)
        for zo in (-0.7, 0.7): lancet(k, cz + zo, 27.0, .34, 1.9, face, xc)
    return k

# =================================================================================================================================
# PIECE 2: gatehouse approach (drum towers, parapet, hood moulding, raised portcullis, banners)
# =================================================================================================================================
DRUM_C = ((-8.6, -2.2), (8.6, -2.2)); DRUM_R = 1.7; DRUM_TOP = 13.6     # wave FD (r13 critic: "squat drums"): 9.4 -> 13.6 and a slate pepperpot roof

def pointed(hw, rise, off, y0, n=10):
    """Left half (spring -> apex) of a two-centre pointed arch; offset > 0 gives a constant-thickness band outside it."""
    c = (rise * rise - hw * hw) / (2 * hw); R = hw + c + off
    th_a = math.atan2(math.sqrt(max(R * R - c * c, 0)), -c)
    return [(c + R * math.cos(t), y0 + R * math.sin(t)) for t in [math.pi + (th_a - math.pi) * i / n for i in range(n + 1)]]

def gate():
    k = K("ExtGate")
    for (dx, dz) in DRUM_C:                                   # twin drum towers
        drum(k, dx, dz, DRUM_R, -1.3, DRUM_TOP - 1.4, ST, seg=20, plinth=.3)
        corbel_ring(k, dx, dz, DRUM_R, DRUM_TOP - 1.4, SD, seg=20, flare=.4, depth=1.4)
        merlons(k, dx, dz, DRUM_R + .3, DRUM_TOP, 12, .95, 1.0, .5, ST, phase=360 / 24)
        for yb in (3.0, 7.0, 10.6):
            k.lathe((dx, 0, dz), [(0, yb), (DRUM_R + .12, yb), (DRUM_R + .12, yb + .2), (0, yb + .2)], 20, SD)   # string courses
        # pepperpot roof rising inside the merlons; tip kept under elevation 45 deg from the S4 eye (0, 2.5, -22) so the pale moon stays clear
        spire(k, (dx, DRUM_TOP - .05, dz), DRUM_R - .2, 6.4, eave=.2, seg=20, rod=.9, concave=1.15)
        apo = DRUM_R * math.cos(math.radians(9))
        for j in (13, 14, 15, 16):                              # facets facing the approach (243..297 deg)
            radial_slit(k, dx, dz, apo, (j + .5) * 18, 4.6 if j % 2 else 5.5, .26, 1.5)
            radial_slit(k, dx, dz, apo, (j + .5) * 18, 8.4 if j % 2 else 9.1, .26, 1.4)
        for j in (9, 10) if dx < 0 else (0, 19):                # and the outer flank
            radial_slit(k, dx, dz, apo, (j + .5) * 18, 5.1, .26, 1.5)
            radial_slit(k, dx, dz, apo, (j + .5) * 18, 8.8, .26, 1.4)
    # gate wall: kept flush at the top (r12 polish: any parapet, merlon or corbel there sits in front of the pale/red moon from the bridge)
    k.box((0, WALL_TOP_GATE - .4, GATE_Z - .06), (9.6, .22, .22), SD)                                # flush crown course (top 7.2, 0.1 m proud: no projection above the wall line)
    # pointed hood moulding over the rectangular opening; the raised portcullis hangs inside it against a lit tympanum
    hw, y0, rise = 1.95, 4.75, 1.3
    zf = GATE_Z - .34                                          # hood centre: z -0.79, depth .36 -> -0.97..-0.61 (embeds in the veneer)
    inner = pointed(hw, rise, 0.0, y0); outer = pointed(hw, rise, .3, y0)
    mirror = lambda pts: [(-x, y) for x, y in pts]
    for (O, I) in ((outer, inner), (mirror(outer), mirror(inner))):
        for i in range(len(O) - 1):
            k.poly_xy([O[i], O[i + 1], I[i + 1], I[i]], zf - .18, zf + .18, SD)
    for sx in (-1, 1):
        k.box((sx * (hw + .15), 4.65, zf), (.3, .8, .36), SD)                                         # springing blocks
    left = [(x * .94, y0 + (y - y0) * .94) for x, y in inner]
    tym = left[:-1] + [(-x, y) for x, y in reversed(left)]
    window(k, "gate", tym, lambda t, y: (t, y, zf + .04), (0, 0, -1))
    for x in (-1.2, -.8, -.4, 0.0, .4, .8, 1.2):                                                       # portcullis bars with spikes
        top = y0 + (rise * .9) * (1 - (abs(x) / hw) ** 1.8) - .12
        k.box((x, (y0 + .05 + top) / 2, zf - .04), (.07, top - y0 - .05, .07), IRON)
        k.poly_xy([(x - .06, y0 + .05), (x + .06, y0 + .05), (x, y0 - .22)], zf - .075, zf - .005, IRON)
    for y in (y0 + .65, y0 + 1.35):
        k.box((0, y, zf - .04), (2.5, .07, .07), IRON)
    for sx in (-1, 1):                                                                                  # banners and slits on the flanks
        bx = sx * 3.3
        k.poly_xy([(bx - .45, 6.8), (bx + .45, 6.8), (bx + .45, 4.6), (bx, 4.1), (bx - .45, 4.6)], GATE_Z - .185, GATE_Z - .145, OX)
        k.box((bx, 6.86, GATE_Z - .16), (1.1, .08, .12), IRON)
    k.box((0, 3.95, GATE_Z - .13), (4.3, .22, .26), SD)
    return k

# =================================================================================================================================
# PIECE 3: corner towers, observatory spire, wing roofs
# =================================================================================================================================
CR = 1.8                                                      # corner tower radius
# Wave FD (r13 critic: "towers read as squat drums with cone caps"): (x, z, shaft top, spire height, turret height). Each tower now has a tall
# shaft, a machicolated parapet, a narrower turret stage (radius TR) with its own slits and a slender spire on the turret. The front pair stays
# under the moons from the S4 eye (0, 2.5, -22): front-left tip el < 42.5 deg (pale moon az -34), front-right < 38.5 deg (red moon az 42),
# checked by the --proof moon ray casts; the back pair, 53 m away and far from both moon azimuths, are the tall ones.
CORNER = {(-1, -1): (-15.3, -1.35, 15.2, 7.4, 2.4), (1, -1): (15.3, -1.35, 13.8, 6.4, 2.2), (-1, 1): (-15.3, 29.35, 19.5, 13.5, 3.6), (1, 1): (15.3, 29.35, 18.0, 12.0, 3.2)}
TR = 1.2
OBS_C, OBS_R = (11.4, 25.4), 2.4

def towers():
    k = K("ExtTowers")
    apo = CR * math.cos(math.radians(9))
    tapo = TR * math.cos(math.radians(11.25))
    for (sx, sz), (x, z, top, sh, th) in CORNER.items():
        drum(k, x, z, CR, -1.3, top - 1.2, SW, seg=20, plinth=.3)
        corbel_ring(k, x, z, CR, top - 1.2, SD, seg=20, flare=.5, depth=1.2)
        for yb in (3.2, 6.0, 8.8, 11.6, 14.4):
            if yb < top - 2.0: k.lathe((x, 0, z), [(0, yb), (CR + .12, yb), (CR + .12, yb + .2), (0, yb + .2)], 20, SD)      # string courses
        merlons(k, x, z, CR + .3, top, 12, .9, 1.0, .5, SW, phase=360 / 24)                    # crenellated parapet round the turret
        drum(k, x, z, TR, top - .1, top + th, SW, seg=16)                                       # the turret stage
        k.lathe((x, 0, z), [(0, top + th - .3), (TR + .14, top + th - .3), (TR + .14, top + th - .1), (0, top + th - .1)], 16, SD)
        spire(k, (x, top + th, z), TR + .02, sh, eave=.16, seg=16, rod=1.0 if sz < 0 else 1.5)
        face_z = (13, 14, 15, 16) if sz < 0 else (3, 4, 5, 6)      # facets facing -z (front pair) or +z (back pair)
        face_x = (9, 10) if sx < 0 else (0, 19)
        for n, j in enumerate(face_z):
            radial_slit(k, x, z, apo, (j + .5) * 18, (4.6, 6.6, 8.2, 5.8)[n], .26, 1.6)
            if top > 12.5: radial_slit(k, x, z, apo, (j + .5) * 18, (10.6, 12.2, 11.4, 13.0)[n] if top > 16 else (10.4, 11.2, 10.8, 10.0)[n], .26, 1.5)
        for n, j in enumerate(face_x): radial_slit(k, x, z, apo, (j + .5) * 18, (5.8, 8.0)[n], .26, 1.6)
        for j in ((11, 12) if sz < 0 else (3, 4)):                 # one lit slit on the turret, toward the bridge (front) or the far side (back)
            radial_slit(k, x, z, tapo, (j + .5) * 22.5, top + .6, .22, 1.1)
    ox, oz = OBS_C                                                # observatory drum on the east roof block (roof top y 11.6)
    drum(k, ox, oz, OBS_R, 11.4, 16.2, SW, seg=16)
    corbel_ring(k, ox, oz, OBS_R, 15.0, SD, seg=16, flare=.45, depth=1.2)
    spire(k, (ox, 16.3, oz), 2.3, 14.0, eave=.3, seg=16, rod=1.6)        # wave FD: 11.5 -> 14 (more vertical skyline)
    for j in (11, 12, 13): radial_slit(k, ox, oz, OBS_R * math.cos(math.radians(11.25)), (j + .5) * 22.5, 12.8, .3, 1.7)
    def gable(x0, x1, z0, z1, y0, ridge):                         # steep slate gable inside the existing roof parapets
        xm = (x0 + x1) / 2
        k.poly_xy([(x0, y0), (x1, y0), (xm, ridge)], z0, z1, SL)
        k.box((xm, ridge + .08, (z0 + z1) / 2), (.22, .16, z1 - z0), SD)
    gable(-13.0, -7.0, 1.4, 26.6, 6.4, 10.2)
    gable(7.0, 13.0, 1.4, 19.0, 6.4, 10.2)
    for (x, z) in ((-10.0, 8.0), (-10.0, 19.0), (10.0, 9.0)):     # chimney stacks on the ridges
        k.box((x, 11.4, z), (1.1, 2.6, 1.1), SD); k.box((x, 12.8, z), (1.4, .3, 1.4), SW)
    return k

# =================================================================================================================================
# PIECE 4: curtain-wall dressing (outer string courses and corbel tables, courtyard wall walk)
# =================================================================================================================================
def walls():
    k = K("ExtWalls")
    x0, x1, z0, z1 = RECT
    for (xa, xb) in ((-14.45, -9.0), (9.0, 14.45)):               # front side sections (the gate wall is dressed by ExtGate)
        k.box(((xa + xb) / 2, 4.1, GATE_Z - .12), (xb - xa, .24, .3), SD)
        k.box(((xa + xb) / 2, WALL_TOP_FRONT - .12, GATE_Z - .17), (xb - xa, .24, .42), SD)
        for i in range(int((xb - xa) / .9)):
            k.box((xa + .45 + i * .9, WALL_TOP_FRONT - .55, GATE_Z - .2), (.3, .66, .4), SD)
    for xs, sgn in ((x0, -1), (x1, 1)):                           # west and east outer faces
        k.box((xs + sgn * .12, 4.1, 14.0), (.3, .24, 29.0), SD)
        k.box((xs + sgn * .17, WALL_TOP_FRONT - .12, 14.0), (.42, .24, 29.0), SD)
        for i in range(30):
            k.box((xs + sgn * .2, WALL_TOP_FRONT - .55, .45 + i * .93), (.4, .66, .3), SD)
    # back face string course; wave FF: it stops at the great hall's north lancets (x -+2.6, sill 3.55 since hc/ff-art), which it crossed at 4.1
    for (xa, xb) in ((-14.5, -3.15), (-2.05, 2.05), (3.15, 14.5)):
        k.box(((xa + xb) / 2, 4.1, z1 + .12), (xb - xa, .24, .3), SD)
    for sx in (-1, 1):                                            # courtyard wall walk: crenellated rims and a paved walk
        merlon_row(k, (sx * 6.0, 1.2), (sx * 6.0, 13.2), WALL_TOP_FRONT, 8, 1.0, 1.1, .6, SW, axis='z')
        k.box((sx * 6.0, WALL_TOP_FRONT + .06, 7.2), (.7, .12, 13.8), SD)
    merlon_row(k, (-5.2, 6.0), (5.2, 6.0), WALL_TOP_GATE, 7, 1.0, 1.1, .6, SW)      # gatehouse back wall
    k.box((0, 7.0, 13.88), (12.4, .24, .3), SD)
    for x in (-2.8, 0.0, 2.8): k.box((x, 8.0, 13.6), (.6, 1.1, .3), SD)
    return k

# =================================================================================================================================
# PIECE 5: rock outcrop under the walls, flanking crag stacks
# =================================================================================================================================
def rect_d(x, z):
    x0, x1, z0, z1 = RECT
    return math.hypot(max(x0 - x, x - x1, 0.0), max(z0 - z, z - z1, 0.0))

# ground-floor windows on the outer faces: (axis of the wall line, line, centre along it, half width, cap = sill - 0.1). The apron must never rise into
# an opening (the rock is render-only; from inside it would show as a stone face in the window).
WINDOW_CAPS = [("x", -14.45, 5.0, .6, .9), ("x", -14.45, 13.5, .6, .9), ("x", -14.45, 18.5, .6, .9),
               ("z", -.45, -10.0, .6, .9), ("z", -.45, 8.5, .7, 1.3), ("z", -.45, 11.5, .7, 1.3),
               ("z", -.45, 0.0, 6.3, 3.5),            # the gate wall flank carries banners (lowest tip y 4.1) and slits (y 4.9)
               ("x", 14.45, 3.5, .7, 1.3), ("x", 14.45, 9.5, .7, 1.3), ("z", 28.45, -4.4, .7, 1.7), ("z", 28.45, 4.4, .7, 1.7),
               # wave FD: the outcrop's plateau reaches 5.5 m, so the chapel sunburst (sill 2.9) and the first-floor lancets (sill 4.2) get caps too
               ("x", 14.45, 6.5, 1.0, 2.8), ("x", -14.45, 5.0, .6, 4.1), ("x", -14.45, 13.5, .6, 4.1), ("x", -14.45, 18.5, .6, 4.1),
               ("z", -.45, -10.0, .6, 4.1), ("x", 14.45, 18.0, .6, 4.1)]

def apron_height(x, z):
    """Wave FD (r13 critic: "the rock base is a few lumps"): the castle stands on a real basalt outcrop. A ridged upper plateau (3 to 5.5 m) against
    the walls, a first scarp down to a broken terrace, then the main cliff 3.8 to 6.6 m from the wall that drops into the moat, with strata, rubble
    and buttress spurs every few metres. Wave L's caps still keep it out of every ground-floor opening, and the bridge corridor stays open water."""
    d = rect_d(x, z)
    n1 = fbm(x * .23, z * .23, 11)
    ridge = 1 - abs(2 * fbm(x * .45 + 9, z * .45, 17) - 1)
    # buttress spurs: angular ribs along the perimeter, every ~4.5 m, that push the cliff edge out and stand higher
    along = (x + z) if abs(x) > 14.45 else x
    spur = max(0.0, math.cos(along * 1.4 + 3.0 * fbm(x * .1, z * .1, 23))) ** 6
    edge = 3.8 + 2.0 * n1 + 1.3 * spur                         # distance of the cliff edge from the wall
    front = smooth01(-.45, -2.5, z) * (1 - smooth01(14.0, 17.5, abs(x)))
    edge *= 1 - .32 * front                                    # the gate face keeps a narrower shelf so the lake reaches the castle in S4
    terrace = edge - 1.6 - .8 * fbm(x * .6, z * .6, 29)
    plateau = 3.1 + 1.7 * ridge + 1.0 * (n1 - .5) + 1.2 * spur
    t1 = smooth01(terrace - .5, terrace + .15, d)               # upper scarp
    t2 = smooth01(edge - .7, edge + .2, d)                      # main cliff
    mid = plateau - 1.3 - .7 * fbm(x * 1.1, z * 1.1, 31)
    h = plateau * (1 - t1) + mid * t1
    h = h * (1 - t2) + (-2.7) * t2                               # wave FD: the water is at -1.6 now
    h += .32 * (fbm(x * 1.7, z * 1.7, 5) - .5) * (1 - .7 * t2)
    if h > 0: h = h * .55 + (math.floor(h * 2.2) / 2.2) * .45                  # strata
    for (axis, line, centre, half, cap) in WINDOW_CAPS:        # keep the openings clear
        al = x if axis == "z" else z; dist = abs((z if axis == "z" else x) - line)
        if dist < 7.6:                                         # wave FD: the whole apron (it reaches 7.4 m out), not just 4.2 m
            cap_here = cap + (5.5 - cap) * smooth01(half + .3, half + 3.4, abs(al - centre))
            h = min(h, cap_here)
    if z < 1.0 and abs(x) < 4.6:                                # the bridge corridor and the gate opening (wall z -0.45..0.45) stay clear
        h = -2.5 + (h + 2.5) * smooth01(2.5, 4.6, abs(x))
    return h

def rock():
    k = K("ExtRock"); b = k.b
    step = .36
    xs = [-22.5 + i * step for i in range(int(45 / step) + 1)]
    zs = [-8.6 + j * step for j in range(int(46.5 / step) + 1)]
    H = {}
    for i, x in enumerate(xs):
        for j, z in enumerate(zs):
            H[(i, j)] = apron_height(x, z) if rect_d(x, z) <= 7.4 else None
    V = {}
    def vert(i, j):
        if (i, j) not in V: V[(i, j)] = b._v(U((xs[i], H[(i, j)], zs[j])))
        return V[(i, j)]
    for i in range(len(xs) - 1):
        for j in range(len(zs) - 1):
            cs = [(i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)]
            if any(H[c] is None for c in cs): continue
            if all(rect_d(xs[c[0]], zs[c[1]]) <= 0 for c in cs): continue
            if max(H[c] for c in cs) < -2.1: continue
            b._f([vert(*c) for c in cs], BS, False, closed=False)
    for (cx, cz, r, hh, seed) in ((-23.5, 4.0, 3.6, 12.5, 1), (-26.5, 15.0, 4.4, 15.5, 2), (-23.8, 26.5, 3.4, 10.5, 3),
                                  (23.8, 3.0, 3.4, 11.0, 4), (26.8, 14.0, 4.5, 14.5, 5), (24.0, 27.0, 3.6, 12.0, 6),
                                  (-31.0, 8.0, 3.0, 7.5, 7), (31.0, 20.0, 3.0, 8.0, 8),
                                  # wave FD: crags rising from the lake round the castle's front corners and beside the causeway's gorge
                                  (-19.8, -5.2, 2.5, 7.5, 9), (19.6, -5.8, 2.2, 6.0, 10), (-21.6, -11.0, 2.0, 4.5, 11), (22.0, -12.5, 1.8, 3.6, 12)):
        crag(k, cx, cz, r, hh, seed)
    return k

def crag(k, cx, cz, r, hgt, seed, seg=15, rings=13):
    b = k.b; grid = []
    for i in range(rings + 1):
        t = i / rings
        y = -2.6 + (hgt + 2.6) * t ** .85
        base_r = r * (1 - .86 * t ** 1.6)
        row = []
        for j in range(seg):
            th = j * math.tau / seg
            n = fbm(math.cos(th) * 1.3 + seed * 3.7, math.sin(th) * 1.3 + t * 3.1, seed + 40) - .5
            rr = max(base_r * (1 + .55 * n) + .4 * (fbm(th * 2 + seed, t * 7, seed) - .5) * (1 - t), .12)
            yy = y + .35 * (fbm(th * 1.7, t * 5 + seed, seed + 7) - .5)
            row.append(b._v(U((cx + rr * math.cos(th), yy, cz + rr * math.sin(th)))))
        grid.append(row)
    for i in range(rings):
        for j in range(seg):
            j2 = (j + 1) % seg
            b._f([grid[i][j], grid[i][j2], grid[i + 1][j2], grid[i + 1][j]], BS, False, closed=False)
    top = b._v(U((cx + .15 * r * (fbm(seed, 2) - .5), hgt + .35, cz)))
    for j in range(seg):
        b._f([grid[rings][j], grid[rings][(j + 1) % seg], top], BS, False, closed=False)

# =================================================================================================================================
# PIECE 6: far backdrop ring. UV.x = layer id (0 far ridge, 1 near ridge, 2 mist band), UV.y = height fraction (0 base .. 1 crest).
# The shader (CastleBackdrop) is unlit, ignores fog and is alpha-blended, so it never enters the lightmap bake.
# Radii keep every column within the player camera's 75 m far clip from the S4 eye (0, 2.5, -14): the far ring's +z crest
# is at z 58 (72 m).
# =================================================================================================================================
# Shader constants (mirrored in CastleExterior.cs). Values are DISPLAY colours (the project is Linear but SetColor takes them as typed).
BACKDROP = dict(far_low=(.20, .25, .38), far_high=(.085, .105, .19), near_low=(.10, .13, .21), near_high=(.04, .05, .095), haze=(.20, .26, .40, .40))
BACKDROP_PROOF = {"far": (.14, .175, .28), "near": (.07, .09, .15)}   # what the visible part of each layer averages to
if "--old-backdrop" in ARGS: BACKDROP_PROOF = {"far": (.028, .04, .07), "near": (.015, .02, .04)}     # calibration only: the r12 first-pass values
BACK_C = (0.0, 12.0)
LAYERS = [(0, 47.0, 46.0, -6.0, .85, 21, 480), (2, 44.0, 43.0, -6.0, 1.00, 0, 120), (1, 39.5, 38.5, -6.0, .45, 33, 480)]
# Wave FD: the S4 eye moved to (0, 2.5, -22) (CaptureMode, behind the far landing), so the north half of each ring is pulled in to stay inside the
# 75 m far clip from there (far crest z 52 -> 74 m). The south half keeps its radius: the blood lake (z -0.45..-16, x +-24) and the far landing
# (to z -20.4) lie inside the near ridge everywhere.
RZ_NORTH = {0: 40.0, 2: 37.5, 1: 34.0}

def ridge_height(a_deg, scale, seed):
    """Crest height above y = 0 for azimuth a (deg from +z toward +x): lower behind the castle, higher on the flanks."""
    a = math.radians(a_deg)
    flank = smooth01(18, 75, abs(((a_deg + 180) % 360) - 180))
    n = fbm(math.cos(a) * 2.2 + seed, math.sin(a) * 2.2 + seed * .3, seed, 4)
    sharp = abs(2 * fbm(a * 5 + seed, seed * .11, seed + 9, 3) - 1)
    jag = abs(2 * fbm(a * 14 + seed, seed * .7, seed + 3, 3) - 1) ** 2      # wave FD: broken, jagged crests instead of smooth "low-poly hills"
    return (5.0 + 20.0 * n * (.35 + .65 * flank) + 7.0 * sharp * flank + 4.5 * jag * (.3 + .7 * flank)) * scale * (.55 + .45 * flank)

def backdrop():
    k = K("ExtBackdrop"); b = k.b
    for (lid, rx, rz, ybase, scale, seed, cnt) in LAYERS:
        cols = []
        for i in range(cnt + 1):
            a = 360.0 * i / cnt; ar = math.radians(a)
            top = ridge_height(a, scale, seed) if lid != 2 else 11.0 + 3.0 * fbm(a * .08, 4, 7)
            rzz = RZ_NORTH.get(lid, rz) if math.cos(ar) > 0 else rz
            cols.append((BACK_C[0] + rx * math.sin(ar), BACK_C[1] + rzz * math.cos(ar), top))
        for i in range(cnt):
            (x0, z0, t0), (x1, z1, t1) = cols[i], cols[i + 1]
            pts = [b._v(U((x0, ybase, z0))), b._v(U((x1, ybase, z1))), b._v(U((x1, t1, z1))), b._v(U((x0, t0, z0)))]
            b._f(pts, BACK, False, uvs=[(lid, 0.0), (lid, 0.0), (lid, 1.0), (lid, 1.0)], closed=False)
    return k

# =================================================================================================================================
PIECES = [("ExtKeep", keep, 30000), ("ExtGate", gate, 30000), ("ExtTowers", towers, 40000), ("ExtWalls", walls, 20000),
          ("ExtRock", rock, 60000), ("ExtBackdrop", backdrop, 6000)]

def check_clear():
    """Fail loudly if a tower cylinder enters an interior room, the standing spot or the bridge corridor."""
    problems = []
    inner = (-13.55, 13.55, .45, 27.55)                           # inner faces of the 0.9 m outer walls (the rooms); shaft radius only:
    # the plinth flare below y -0.4 is under every floor slab and the corbel flare above y 8 is over every ceiling
    for name, c, r in [("gate%d" % i, DRUM_C[i], DRUM_R) for i in (0, 1)] + [("corner%d%d" % key, (v[0], v[1]), CR) for key, v in CORNER.items()]:
        for a in range(0, 360, 5):
            x = c[0] + r * math.cos(math.radians(a)); z = c[1] + r * math.sin(math.radians(a))
            if inner[0] < x < inner[1] and inner[2] < z < inner[3]: problems.append("%s enters interior at %.2f,%.2f" % (name, x, z))
    for (dx, dz) in DRUM_C:
        d = math.hypot(STAND[0] - dx, STAND[2] - dz) - DRUM_R - .52
        if d < 1.0: problems.append("drum tower %.2f m from the standing spot" % d)
        if abs(dx) - DRUM_R - .6 < 2.4: problems.append("drum tower intrudes on the bridge corridor")
    # the standing crenel (x 3.0..4.2) must be open: no parapet box centred in it
    if any(abs(x - 3.6) < 1.05 for x in (-4.65, -3.15, -1.65, -.15, 1.35, 2.55, 4.65)): problems.append("merlon in the standing crenel")
    if problems: raise SystemExit("HC_EXTERIOR_CLEAR FAIL " + "; ".join(problems))
    print("HC_EXTERIOR_CLEAR PASS (towers outside every room, standing spot clear >= 1.0 m, bridge corridor and crenel open)")

def build_all(export=True):
    total = 0
    for name, make, budget in PIECES:
        k = make()
        if export: total += export_asset(k.b, budget)["tris"]
        else: k.b.finish()
    return total

if __name__ == "__main__":
    check_clear()
    reset_scene()
    proof = ARGS[ARGS.index("--proof") + 1] if "--proof" in ARGS else None
    if "--no-export" not in ARGS:
        atlas = cc.TEX / "Castle_ExtWindows.png"
        ATL.write_png(atlas, ATL.build()); cc.write_texture_meta(atlas, "emission")
        total = build_all(True)
        write_manifest("exterior")
        print("HC_EXTERIOR_EXPORT tris=%d (budget 150000)" % total)
    if proof:
        reset_scene()
        import castle_lib; castle_lib._MAT_CACHE.clear()       # the factory reset invalidated every cached material
        import castle_exterior_proof as P
        P.run(proof, sys.modules[__name__])
