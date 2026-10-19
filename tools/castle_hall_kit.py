#!/usr/bin/env python3
"""Haunted Castle round 13 (hc/hall-l): the great hall's gothic architecture, lancet windows, turned candles, the trestle table, the chapel's
pews, altar and broken sunburst window. Headless, idempotent, no Unity, no GPU engine:

    /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/castle_hall_kit.py [-- Name ...]
    python3 tools/castle_hall_textures.py      # the new materials (run first)

Exports Assets/HollowCurrent/Resources/Castle/<Name>.fbx, .fbx.meta (generateSecondaryUV on, so the bake always has lightmap UVs) and
docs/haunted-castle/art/manifest_hall.json (sizes + tri counts; tools/castle_verify.py reads it).

Axis: assets are authored in design space (X right, Y forward = the front, Z up) and turn_to_unity() bakes a 180 degree turn about Z, so the front
lands on Unity +Z at yaw 0 and +X on +X (docs/haunted-castle/handoffs/art-exterior2.md section 4: Blender (x, y, z) -> Unity (-x, z, -y)).
These pieces therefore honour the CastleArt contract ("front = +Z at yaw 0"); the older kit does not (its front lands on -Z).

HallVault is authored in castle-local coordinates (design (x, y, z) = castle (x, z, y)) and placed at the castle root with no rotation.
"""
import sys, math, random
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from castle_gothic import *

def only():
    a = sys.argv
    return a[a.index("--") + 1:] if "--" in a else []

BS_ = "Castle_BlackStone"

# ================================================================================================ geometry constants (castle-local, metres)
COL_X = 3.9                                   # colonnade row, x = +-3.9
COL_Z = [15.6, 18.6, 21.6, 24.4]              # column centres (z)
WALL_S, WALL_N = 14.425, 27.425               # inner faces of the south (0.6 m wall at z 14) and north (0.9 m wall at z 28) walls with the 0.125 m veneer
WALL_X = 5.575                                # inner faces of the side walls (with the veneer)
CAP_TOP = 5.85                                # abacus top: the springing of the ribs
RIB_DEPTH = .235
WEB_SPRING = CAP_TOP + .22                    # webbing surface at the springing (the rib hangs 0.235 below it)
APEX = 8.74                                   # webbing surface at the crown (the old cove ceiling above is 8.95 on the axis, 8.80 over the colonnades)
VH = APEX - WEB_SPRING
ALPHA = .62

def yweb(t):
    return WEB_SPRING + VH * ogive(t, ALPHA)

# ================================================================================================ windows
def window_builder(name, W, H, lights=2):
    return lambda: gothic_window(name, W, H, lights=lights)

def pointed_arch():
    """Moulded arcade arch: clear span 3.0, springs at 2.0, 0.45 band, 0.5 deep, chamfered on every edge. Origin: floor, centred. Same
    placements as before. Wave FD (r13 critic: "clustered columns: the piers are square blocks"): the two square legs from the floor to the
    springing are gone; the arch now springs from the cluster columns' impost rings at 2.0 (the columns stand at the arch ends), so the
    colonnade reads as clustered shafts instead of 0.45 x 0.5 square piers."""
    hw, t, spring, R = 1.5, .45, 2.0, 3.0
    mid = t / 2
    arc = arch_curve(hw, spring, R, 14, mid)
    path = [(x, 0, z) for x, z in arc]
    h, dd, c = t / 2, .25, .06
    prof = [(-dd + c, -h, False), (dd - c, -h, False), (dd, -h + c, False), (dd, h - c, False), (dd - c, h, False), (-dd + c, h, False), (-dd, h - c, False), (-dd, -h + c, False)]
    b = Builder("PointedArch")
    sweep(b, prof, path, DS, N=(0, 1, 0))
    b.notes.update(clear_span=2 * hw, intrados_apex=round(spring + arch_rise(hw, R), 3))
    return b

# ================================================================================================ columns, corbels, mouldings
def hall_column():
    return cluster_column("HallColumn", CAP_TOP)

def hall_corbel():
    """Rib springer for the end walls: an engaged half cone under an octagonal abacus, 0.55 high. Origin: centre of the back face at the nose
    (z = 0), front +Y. Top at z = 0.55 so its abacus top meets the column abacus (y 5.85) when the nose is at 5.30."""
    b = Builder("HallCorbel")
    prof = [(0, 0), (.05, 0), (.06, .02), (.14, .10), (.22, .22), (.30, .34), (.355, .43), (.37, .44), (.37, .46), (.0, .46)]
    b.lathe((0, 0, 0), prof, 16, DS, cap=False)
    octagon(b, (0, 0, 0), .84, .44, .55, DS, chamfer=.03)
    return b

def plinth(length=4.0):
    """Moulded base for the hall walls: 0.22 deep, 0.44 high, a bead and a chamfered top. 4.0 m long tiles end to end. Origin: centre of the back
    face on the wall, base at z = 0, front +Y."""
    pts = [(0, 0), (.22, 0), (.22, .26), (.205, .285), (.205, .31), (.225, .33), (.225, .365), (.19, .40), (.15, .44), (0, .44)]
    b = Builder("HallPlinth")
    b.poly(pts, -length / 2, length / 2, DS, 'YZ')
    return b

def string_course(length=4.0):
    """Moulded band for the hall walls (cavetto, fillet, roll, drip), 0.24 deep, 0.30 high; 4.0 m long. Origin: centre of the back face on the wall,
    bottom at z = 0, front +Y."""
    pts = [(0, 0), (.10, 0), (.10, .03), (.14, .06), (.14, .10), (.20, .13), (.235, .17), (.24, .21), (.22, .245), (.15, .27), (.15, .30), (0, .30)]
    b = Builder("HallStringCourse")
    b.poly(pts, -length / 2, length / 2, DS, 'YZ')
    return b

# ================================================================================================ the vault
def surface(b, mat, fn, nt, ns, t_spread=False, s_spread=True, fan=True):
    """Triangle-fan / quad grid from the centre: point(t, s) for t in 0..1 (centre -> edge), s in -1..1. Faces point down (-Z)."""
    sv = spread(ns) if s_spread else [-1 + 2 * i / ns for i in range(ns + 1)]
    tv = [k / nt for k in range(nt + 1)]
    centre = b._v(fn(0.0, 0.0))
    rows = [[b._v(fn(tv[k], s)) for s in sv] for k in range(1, nt + 1)]
    def add(vs):
        pts = [v.co for v in vs]
        n = (pts[1] - pts[0]).cross(pts[2] - pts[0])
        if n.z > 0:
            vs = list(reversed(vs))
        b._f(vs, mat, True, closed=False)
    for m in range(ns):
        add([centre, rows[0][m], rows[0][m + 1]])
    for k in range(nt - 1):
        for m in range(ns):
            add([rows[k][m], rows[k][m + 1], rows[k + 1][m + 1], rows[k + 1][m]])

def hall_vault():
    """The great hall's ribbed vault, authored in castle coordinates (design x = castle x, design y = castle z, design z = castle y). Over the nave
    (x +-3.9, between the colonnades): four quadripartite bays (z 15.6-18.6-21.6-24.4-27.425) and a barrel over the entrance bay, springing from
    the column abaci at y 5.85 and meeting at a flat ridge at y 8.74 under the old cove ceiling (8.4-8.95). Ceiling = max(transverse barrel,
    longitudinal barrel) per bay, which puts the groins exactly on the plan diagonals: the diagonal ribs follow them. Parts: Webbing, Ribs
    (ridge, transverse ribs, diagonals, wall ribs, formerets, bosses). Triangle count is in the manifest."""
    b = Builder("HallVault")
    del b.parts["HallVault"]
    # --- webbing
    b.part("Webbing", (0, 0, 0))
    lines = COL_Z + [WALL_N]
    for i in range(len(lines) - 1):
        z0, z1 = lines[i], lines[i + 1]
        zc, hb = (z0 + z1) / 2, (z1 - z0) / 2
        def n_(t, s, zc=zc, hb=hb):          # near the transverse ribs: v >= |u|: surface follows the transverse barrel yT(u)
            u, v = s * t, t
            return (COL_X * u, zc + hb * v, yweb(u))
        def s_(t, s, zc=zc, hb=hb):
            u, v = s * t, -t
            return (COL_X * u, zc + hb * v, yweb(u))
        def e_(t, s, zc=zc, hb=hb):          # near the walls (|u| >= |v|): longitudinal barrel yL(v)
            u, v = t, s * t
            return (COL_X * u, zc + hb * v, yweb(v))
        def w_(t, s, zc=zc, hb=hb):
            u, v = -t, s * t
            return (COL_X * u, zc + hb * v, yweb(v))
        for fn in (n_, s_, e_, w_):
            surface(b, DS, fn, 10, 26)
    # entrance bay: plain barrel over x +-3.9, z WALL_S..15.6
    xs = spread(26)
    for z0, z1 in ((WALL_S, COL_Z[0]),):
        for m in range(len(xs) - 1):
            p = [(COL_X * xs[m], z0, yweb(xs[m])), (COL_X * xs[m + 1], z0, yweb(xs[m + 1])), (COL_X * xs[m + 1], z1, yweb(xs[m + 1])), (COL_X * xs[m], z1, yweb(xs[m]))]
            vs = [b._v(q) for q in p]
            n = (vs[1].co - vs[0].co).cross(vs[2].co - vs[0].co)
            b._f(list(reversed(vs)) if n.z > 0 else vs, DS, True, closed=False)
    # --- ribs
    b.part("Ribs", (0, 0, 0))
    rp = rib_profile(.22, RIB_DEPTH)
    thin = rib_profile(.17, .19)
    ts = spread(36)
    # transverse ribs across the nave at each column line, formerets on both end walls
    for z in COL_Z:
        sweep(b, rp, [(COL_X * t, z, yweb(t)) for t in ts], DS)
    for z in (WALL_S + .035, WALL_N - .035):
        sweep(b, thin, [(COL_X * t, z, yweb(t)) for t in ts], DS)
    # diagonals and wall ribs, bay by bay
    for i in range(len(lines) - 1):
        z0, z1 = lines[i], lines[i + 1]
        zc, hb = (z0 + z1) / 2, (z1 - z0) / 2
        sweep(b, rp, [(COL_X * t, zc + hb * t, yweb(t)) for t in ts], DS)
        sweep(b, rp, [(COL_X * t, zc - hb * t, yweb(t)) for t in ts], DS)
        for sx in (-1, 1):
            sweep(b, thin, [(sx * COL_X, zc + hb * t, yweb(t)) for t in ts], DS)
    # entrance bay wall ribs (flat) and the ridge
    for sx in (-1, 1):
        sweep(b, thin, [(sx * COL_X, WALL_S + .035, WEB_SPRING), (sx * COL_X, COL_Z[0], WEB_SPRING)], DS)
    sweep(b, rib_profile(.18, .20), [(0, WALL_S + .035, APEX), (0, WALL_N - .035, APEX)], DS)
    # bosses where the diagonals cross, and a smaller one over the entrance
    def boss(zc, k=1.0):
        prof = [(0, .03), (.34, .03), (.345, -.03), (.30, -.07), (.315, -.10), (.265, -.14), (.185, -.165), (.11, -.19), (.125, -.235), (.06, -.285), (0, -.31)]
        b.lathe((0, zc, APEX), [(r * k, z * k) for r, z in prof], 20, DS, cap=False)
        for j in range(8):                               # petals round the boss, drooping
            a = j * math.tau / 8
            b.tube([(0.13 * k * math.cos(a), zc + .13 * k * math.sin(a), APEX - .15 * k), (0.25 * k * math.cos(a), zc + .25 * k * math.sin(a), APEX - .13 * k),
                    (0.31 * k * math.cos(a), zc + .31 * k * math.sin(a), APEX - .09 * k)], [.032 * k, .03 * k, .012 * k], 6, DS, cap=True)
    for i in range(len(lines) - 1):
        boss((lines[i] + lines[i + 1]) / 2)
    boss((WALL_S + COL_Z[0]) / 2, .62)
    b.notes.update(webbing_apex=APEX, spring=CAP_TOP, bays=len(lines) - 1)
    return b

# ================================================================================================ candles
def candle_head(b, c, s=1.0, taper=.30, seg=10, flame=True):
    """Drip pan, socket and tapered wax candle with its collar, pool and two runs, for candelabra arms (the pan sits at c)."""
    x, y, z0 = c
    k = s
    pan = [(0, 0), (.021, 0), (.045, .004), (.050, .007), (.050, .012), (.044, .012), (.030, .010), (.025, .016), (.022, .032), (.0245, .040), (.0235, .044), (.0145, .044), (0, .044)]
    b.lathe((x, y, z0), [(r * k, z * k) for r, z in pan], seg, BRASS, cap=False)
    base = .036
    zt = base + taper
    waxp = [(0, base), (.0150, base), (.0185, base + .012), (.0160, base + .026), (.0135, base + .045), (.0128, zt - .020), (.0120, zt - .006), (.0125, zt), (.0075, zt - .004), (0, zt - .007)]
    b.lathe((x, y, z0), [(r * k, z * k) for r, z in waxp], seg, WAX, cap=False)
    for a, ln in ((35, .05), (200, .033)):
        ar = math.radians(a); rr = .0132; ztop = zt - .008
        b.tube([(x + rr * math.cos(ar) * k, y + rr * math.sin(ar) * k, z0 + ztop * k), (x + (rr + .0035) * math.cos(ar) * k, y + (rr + .0035) * math.sin(ar) * k, z0 + (ztop - ln * .55) * k),
                (x + (rr + .002) * math.cos(ar) * k, y + (rr + .002) * math.sin(ar) * k, z0 + (ztop - ln) * k)], [.003 * k, .0036 * k, 0], 5, WAX, cap=False)
    if flame:
        candle_flame(b, (x, y, z0 + zt * k), .06 * k)
    return z0 + zt * k

def candelabra():
    """Five-candle brass candelabrum: round turned baluster on a domed foot, four curved scrolled arms ending in drip pans, 30 cm tapers. Parts:
    body + Flame_0..4 (Castle_Flame; each pivot at its candle top so the runtime puts a billboard on it). Height about 1.5, spread 0.56.
    Origin: base centre."""
    b = Builder("Candelabra")
    stem = [(0, 0), (.20, 0), (.215, .012), (.205, .03), (.15, .06), (.095, .10), (.06, .15), (.045, .20), (.05, .24), (.07, .28), (.075, .32), (.055, .36),
            (.040, .42), (.034, .52), (.045, .58), (.062, .62), (.050, .66), (.034, .72), (.032, .84), (.040, .90), (.056, .94), (.052, .985), (.030, 1.02), (.026, 1.04),
            (.040, 1.055), (.034, 1.085), (0, 1.085)]
    b.lathe((0, 0, 0), stem, 12, BRASS, cap=False)
    # four arms: out and up in a scroll, ending in a drip pan; the centre candle sits on the stem top
    tops = []
    for k in range(4):
        a = math.radians(45 + 90 * k); ca, sa = math.cos(a), math.sin(a)
        pts = [(.0, .0, .98), (.07, .0, .94), (.15, .0, .93), (.22, .0, .98), (.27, .0, 1.08), (.275, .0, 1.15)]
        pts = [(p[0] * ca - p[1] * sa, p[0] * sa + p[1] * ca, p[2]) for p in pts]
        b.tube(pts, [.020, .019, .017, .016, .015, .014], 8, BRASS, cap=False)
        tops.append((.275 * ca, .275 * sa, 1.15))
    heads = [(0, 0, 1.085)] + tops
    for h in heads:
        candle_head(b, h, 1.0, .30)
    return b

def candlestick_asset():
    b = Builder("Candlestick")
    turned_candlestick(b, (0, 0, 0), 1.0, .30, flame=True)
    return b

# ================================================================================================ the great hall table
def goblet(b, c, s=1.0, mat=SIL):
    x, y, z = c
    prof = [(0, 0), (.032, 0), (.034, .004), (.014, .012), (.010, .030), (.012, .045), (.026, .055), (.038, .078), (.040, .098), (.036, .100), (.034, .082), (.022, .062), (0, .058)]
    b.lathe((x, y, z), [(r * s, zz * s) for r, zz in prof], 10, mat, cap=False)

def trestle_table():
    """Great-hall banquet table, 3.3 x 1.3 m, planked top at 0.775 m on two trestles with a tenoned, wedged stretcher; an oxblood runner hangs
    over both ends; eight turned candlesticks (no flames in the mesh: Flame_n parts) and silver goblets. Long axis X, origin: floor centre.
    The runtime collision is a 3.3 x 0.775 x 1.3 box (CastleWorld), so thrown props that rest on the top are not blocked."""
    L, Wd, top = 3.3, 1.3, .775
    b = Builder("LongTable")
    nb = 5; gap = .008; pw = (Wd - gap * (nb - 1)) / nb
    rnd = random.Random(13)
    for k in range(nb):
        y = -Wd / 2 + pw / 2 + k * (pw + gap)
        trim = rnd.uniform(0, .05)
        b.box((rnd.uniform(-.01, .01), y, top - .03), (L - trim, pw, .06), PLANK)
    for sx in (-1, 1):                                       # cross battens under the top
        b.box((sx * (L / 2 - .55), 0, top - .075), (.07, Wd - .16, .05), OAK)
    for sx in (-1, 1):
        x = sx * (L / 2 - .55)
        b.box((x, 0, .05), (.10, Wd - .26, .10), OAK)           # sled foot
        for sy in (-1, 1):                                       # splayed legs: polygon in (y, z), thickness 0.1 in x
            yb, yt = sy * (Wd / 2 - .17), sy * (Wd / 2 - .30)
            b.poly([(yb - .055, .10), (yb + .055, .10), (yt + .055, top - .12), (yt - .055, top - .12)] if sy > 0 else
                   [(yb + .055, .10), (yb - .055, .10), (yt - .055, top - .12), (yt + .055, top - .12)], x - .05, x + .05, OAK, 'YZ')
        b.box((x, 0, top - .17), (.12, Wd - .34, .10), OAK)       # head rail under the batten
        b.box((x, 0, top - .38), (.07, Wd - .44, .07), OAK)       # lower cross rail
    # stretcher through both trestles with wedges
    b.box((0, 0, .30), (L - .70, .08, .10), OAK)
    for sx in (-1, 1):
        x = sx * (L / 2 - .55)
        b.box((sx * (L / 2 - .38), 0, .30), (.14, .08, .10), OAK)     # tenon end
        b.box((x + sx * .13, 0, .30), (.035, .05, .22), IRON)       # wedge
    # runner: lies along the table and hangs 0.26 m over both ends (path: down, along, down)
    w = .56
    nx, ny = 4, 44
    hang = .26
    ltot = L + 2 * hang
    def fn(i, j):
        t_ = j / ny * ltot
        yy = (i / nx - .5) * w + .008 * math.sin(j * .9 + i * 1.7)
        if t_ < hang:
            x, z = -L / 2 - .008 - .004 * math.sin(i * 2.1 + t_ * 30), top + .006 - (hang - t_)
        elif t_ < hang + L:
            x, z = -L / 2 + (t_ - hang), top + .0065 + .002 * math.sin(t_ * 5 + i)
        else:
            x, z = L / 2 + .008 + .004 * math.sin(i * 2.1 + t_ * 30), top + .006 - (t_ - hang - L)
        return (x, yy, z)
    b.grid_cloth(nx, ny, fn, OX)
    # candlesticks (the flames are billboards at runtime) and goblets
    for ax in (-1.25, -.6, .6, 1.25):
        for ly in (-.27, .27):
            turned_candlestick(b, (ax, ly, top + .007), 1.0, .30, seg=12, flame=True)
    for x in (-1.45, -.75, -.25, .3, .7, 1.35):
        for sy in (-1, 1):
            goblet(b, (x, sy * .4 + (.04 if x > 0 else -.03), top + .008), 1.0)
    b.notes.update(top=top, length=L, width=Wd)
    return b

# ================================================================================================ chapel
def poppyhead(b, c, s=1.0, mat=OAK):
    x, y, z = c
    prof = [(0, 0), (.028 * s, 0), (.03 * s, .02 * s), (.02 * s, .035 * s), (.034 * s, .055 * s), (.04 * s, .085 * s), (.028 * s, .11 * s), (.012 * s, .135 * s), (0, .16 * s)]
    b.lathe((x, y, z), prof, 10, mat, cap=False)

def pew():
    """Chapel pew, 2.4 x 0.7 m (+ the kneeler), 1.0 high. Solid end panels with a stepped arm and a poppyhead finial on the tall back post, a planked
    seat, a panelled back with a rail and a book ledge, and a padded kneeler on two brackets in front. Front (the way the sitter faces) = +Y.
    Origin: floor centre."""
    b = Builder("Pew")
    for sx in (-1, 1):
        x = sx * 1.165
        pts = [(-.33, 0), (.34, 0), (.34, .40), (.30, .46), (.17, .47), (.06, .52), (-.02, .66), (-.08, .86), (-.14, .96), (-.22, .985), (-.30, .96), (-.34, .84), (-.34, .46)]
        b.poly(pts, x - .035, x + .035, OAK, 'YZ')
        b.box((x, .12, .30), (.075, .52, .06), OAK)                       # foot rail
        poppyhead(b, (x, -.22, .985), 1.0)
        b.box((x, .0, .44), (.095, .62, .03), OAK)                         # arm cap
    b.box((0, .03, .44), (2.30, .44, .05), PLANK)                      # seat
    b.box((0, -.01, .53), (2.30, .03, .085), OAK)                      # seat rear rail
    for z, h in ((.64, .15), (.84, .25)):                             # back: two panels between rails
        b.box((0, -.27, z), (2.30, .035, h), PLANK)
    b.box((0, -.27, .745), (2.30, .06, .045), OAK)                      # mid rail
    b.box((0, -.27, .975), (2.30, .075, .05), OAK)                      # top rail
    b.box((0, -.24, .93), (2.30, .17, .03), OAK)                        # book ledge (for the row behind)
    for x in (-.6, 0, .6):
        b.box((x, -.27, .44), (.045, .05, .34), OAK)                   # back stiles
    # kneeler
    for sx in (-1, 1):
        b.box((sx * 1.0, .47, .13), (.06, .13, .26), OAK)
    b.box((0, .47, .27), (2.1, .20, .05), PLANK)
    b.box((0, .47, .305), (2.0, .17, .03), OX)                          # worn cushion
    b.notes.update(seat_top=.465)
    return b

def altar():
    """Chapel altar, 2.4 x 1.1 x 1.1 m (top at 1.10): a chamfered plinth, a dressed-stone body with a blind pointed arcade on the front, a moulded
    mensa slab, and a torn oxblood altar cloth hanging over the front. Front (the nave side) = +Y. Origin: floor centre. The relic tablet sits on
    top (CastleWorld.AltarInlay), the cloth is below it."""
    b = Builder("Altar")
    b.box((0, 0, .06), (2.36, 1.06, .12), BS_)
    b.box((0, 0, .15), (2.28, .98, .06), DS)
    b.box((0, -.04, .56), (2.20, .82, .86), DS)                       # body core
    # blind arcade on the front: five pointed panels between six colonnettes
    for k in range(5):
        cx = -.72 + k * .36
        curve = [(cx + x, z) for x, z in arch_curve(.14, .66, .28, 7)]
        b.poly([(cx - .14, .20)] + curve + [(cx + .14, .20)], .365, .40, BS_, 'XZ', smooth_range=None)
    for k in range(6):
        x = -.90 + k * .36
        b.lathe((x, .39, .17), [(0, 0), (.044, 0), (.036, .03), (.030, .06), (.030, .70), (.040, .72), (.044, .75), (0, .75)], 10, DS, cap=False)
    b.box((0, .385, .965), (2.20, .05, .09), DS)                       # arcade impost/cornice
    b.box((0, .10, 1.05), (2.38, 1.10, .10), DS)
    b.box((0, .60, 1.075), (2.34, .03, .05), DS)                       # slab front moulding
    # altar cloth: top runner and a front drape, ragged
    nx, ny = 10, 24
    wcl = 1.15
    def fn(i, j):
        u = (i / nx - .5) * wcl
        s = j / ny
        # path: along the top from the back (y=-.2), over the edge at y=.56, then down the front
        if s < .45:
            y = -.2 + (s / .45) * .76; z = 1.106
        else:
            y = .56 + .012 * math.sin(u * 14 + s * 6); z = 1.106 - (s - .45) / .55 * .60
        wob = .012 * math.sin(u * 11 + s * 3.0) * (0 if s < .45 else (s - .45) / .55)
        y += wob
        hem = (0 if s < .45 else ((s - .45) / .55) ** 2) * .035 * math.sin(u * 23 + 1.3)
        return (u, y, z + hem)
    b.grid_cloth(nx, ny, fn, OX)
    b.notes.update(top=1.10, niche="blind arcade, front")
    return b

# ================================================================================================ the chapel's broken sunburst window
def _annular_sector(cx, cz, r0, r1, a0, a1, n=3, jitter=0.0, rnd=None):
    """Polygon (x, z) of an annular sector, optionally with a ragged outer edge."""
    pts = []
    for i in range(n + 1):
        a = a0 + (a1 - a0) * i / n
        pts.append((cx + r1 * math.cos(a), cz + r1 * math.sin(a)))
    for i in range(n, -1, -1):
        a = a0 + (a1 - a0) * i / n
        rr = r0
        pts.append((cx + rr * math.cos(a), cz + rr * math.sin(a)))
    if jitter and rnd:
        pts = [(x + rnd.uniform(-jitter, jitter), z + rnd.uniform(-jitter, jitter)) if 0 < i < n else (x, z) for i, (x, z) in enumerate(pts)]
    return pts

def sunburst_window():
    """The painted-over, broken sun above the chapel altar. A lancet frame (2.0 x 3.1 m opening, sill at the altar's back wall) holding, in the
    head, a circular tracery ring with 16 rays in three rings of glass shards (central disc and inner rays painted black, amber and oxblood
    glass where the ink flaked, cold blue between the rays), a quarter of the shards missing (the moonlit sky shows through) and some cracked
    and tilted; below it two lights with a transom, glazed with the same blue / amber / ink shards, also broken. The glass is emissive (unlit
    CastleEmissive materials Castle_GlassAmber/Blue/Red; the ink is dark stone-grey and not emissive). Front +Y = into the chapel."""
    W, H = 2.0, 3.1
    b = gothic_window("ChapelSunburst", W, H, lights=0, d=.56, glazing=False)
    n = b.notes
    spring, hwi = n["spring"], n["hwi"]
    rnd = random.Random(1013)
    bar_d = .22
    zc, rc = spring + .62, .60
    gy = -.04                                    # the glass plane, behind the tracery front
    th = .008
    # tracery: outer ring, inner ring, 8 spokes
    sweep(b, bar_profile(.07, bar_d, .012), [(p.x, 0, p.z) for p in circle_path((0, 0, zc), rc + .035, n=40)], DS, closed=True, N=(0, 1, 0))
    sweep(b, bar_profile(.04, bar_d * .8, .008), [(p.x, 0, p.z) for p in circle_path((0, 0, zc), .18, n=24)], DS, closed=True, N=(0, 1, 0))
    for k in range(8):
        a = k * math.pi / 4 + math.pi / 16
        sweep(b, bar_profile(.032, bar_d * .7, .008), [(.18 * math.cos(a), 0, zc + .18 * math.sin(a)), (rc * math.cos(a), 0, zc + rc * math.sin(a))], DS, N=(0, 1, 0))
    # glass: centre disc, then 16 sectors x 3 rings; the 8 rays are wider than the 8 gaps between them
    disc = [(.165 * math.cos(i * math.tau / 20), zc + .165 * math.sin(i * math.tau / 20)) for i in range(20)]
    b.poly(disc, gy - th / 2, gy + th / 2, GL_INK, 'XZ')
    rings = [(.195, .335), (.345, .47), (.48, .59)]
    counts = {"ink": 0, "amber": 0, "red": 0, "blue": 0, "missing": 0, "tilted": 0}
    ray_w, gap_w = math.radians(28), math.radians(17)
    ang = -ray_w / 2
    names = {GL_INK: "ink", GL_AMBER: "amber", GL_RED: "red", GL_BLUE: "blue"}
    for k in range(16):
        ray = k % 2 == 0
        w = ray_w if ray else gap_w
        a0, a1 = ang + .012, ang + w - .012
        ang += w
        for ri, (r0, r1) in enumerate(rings):
            if ray and ri == 2 and (k // 2) % 2 == 1:
                continue                                              # the short rays end at the middle ring
            u = rnd.random()
            if u < .20 + (.08 if ri == 2 else 0):
                counts["missing"] += 1
                continue
            if ray:
                if ri == 0:   mat = GL_INK if u > .30 else GL_AMBER
                elif ri == 1: mat = GL_INK if u > .45 else (GL_AMBER if u > .30 else GL_RED)
                else:         mat = GL_AMBER if u > .62 else (GL_RED if u > .42 else GL_INK)
            else:
                mat = GL_BLUE if (ri != 2 or u > .55) else GL_RED
            counts[names[mat]] += 1
            # wave FD: each sector is leaded into 2 x 2 pieces (12 mm cames) instead of one flat shard; ink sectors stay whole
            splits = [(r0, r1, a0, a1)] if mat == GL_INK else [(ra, rb, aa, ab) for (ra, rb) in ((r0, (r0 + r1) / 2 - .006), ((r0 + r1) / 2 + .006, r1))
                                                                       for (aa, ab) in ((a0, (a0 + a1) / 2 - .006), ((a0 + a1) / 2 + .006, a1))]
            tilt = rnd.random() < .16                                   # cracked and sagging: tilted about its own centre
            for (ra, rb, aa, ab) in splits:
                poly = _annular_sector(0, zc, ra, rb, aa, ab, 2, .004, rnd)
                if tilt:
                    am = (aa + ab) / 2; rm = (ra + rb) / 2
                    cx, cz = rm * math.cos(am), zc + rm * math.sin(am)
                    with b.xf(about((cx, gy, cz), Rx(rnd.choice((-14, -9, 9, 14))) @ Ry(rnd.uniform(-8, 8)))):
                        b.poly(poly, gy - th / 2, gy + th / 2, mat, 'XZ')
                else:
                    b.poly(poly, gy - th / 2, gy + th / 2, mat, 'XZ')
            if tilt: counts["tilted"] += 1
    # lower lights: two lights, mullion + transom in stone. Wave FD (r13 critic: "the stained glass is a coarse pixel mosaic of flat blue, red
    # and yellow squares"): each pane of the light is now leaded glazing, a border of small alternating red and amber squares round a field
    # of diamond quarries (0.13 x 0.20 m) with 14 mm lead cames showing between them, the quarries in pale, blue, green and a few amber and
    # ink-painted ones, about a fifth broken out (the sky shows through), a few cracked and sagging.
    zl0, zl1 = .09, zc - rc - .09                              # light spans from the sill to the foot of the circle
    m = .08
    sweep(b, roll_profile(m, bar_d), [(0, 0, .075), (0, 0, zl1 + .03)], DS, N=(0, 1, 0))
    zt = zl0 + (zl1 - zl0) * .56
    b.box((0, 0, zt), (2 * hwi, bar_d * .8, .06), DS)
    b.box((0, 0, zl1 + .02), (2 * hwi, bar_d * .8, .07), DS)
    GL_GREEN, GL_PALE = "Castle_GlassGreen", "Castle_GlassPale"
    names.update({GL_GREEN: "green", GL_PALE: "pale"}); counts.update(green=0, pale=0)
    lead = .007                                                # half the came width
    def clip(poly, x0, x1, z0, z1):                            # Sutherland-Hodgman against the pane rectangle
        for (k, lim, keep_ge) in ((0, x0, True), (0, x1, False), (1, z0, True), (1, z1, False)):
            out = []
            for i in range(len(poly)):
                a_, b_ = poly[i], poly[(i + 1) % len(poly)]
                ina = a_[k] >= lim if keep_ge else a_[k] <= lim
                inb = b_[k] >= lim if keep_ge else b_[k] <= lim
                if ina: out.append(a_)
                if ina != inb:
                    t = (lim - a_[k]) / (b_[k] - a_[k])
                    out.append((a_[0] + (b_[0] - a_[0]) * t, a_[1] + (b_[1] - a_[1]) * t))
            poly = out
            if len(poly) < 3: return []
        return poly
    def shrink(poly, d):
        cx = sum(p[0] for p in poly) / len(poly); cz = sum(p[1] for p in poly) / len(poly)
        out = []
        for (x, z) in poly:
            vx, vz = x - cx, z - cz; L = math.hypot(vx, vz)
            out.append((x - vx / L * d, z - vz / L * d) if L > 2 * d else (x, z))
        return out
    def pane(poly, mat):
        if len(poly) < 3: return
        counts[names[mat]] += 1
        if rnd.random() < .07:                                  # cracked and sagging
            cx = sum(p[0] for p in poly) / len(poly); cz = sum(p[1] for p in poly) / len(poly)
            with b.xf(about((cx, gy, cz), Rx(rnd.choice((-12, -8, 8, 12))))):
                b.poly(poly, gy - th / 2, gy + th / 2, mat, 'XZ')
            counts["tilted"] += 1
        else:
            b.poly(poly, gy - th / 2, gy + th / 2, mat, 'XZ')
    for side in (-1, 1):
        x0, x1 = (-hwi + .02, -m / 2 - .01) if side < 0 else (m / 2 + .01, hwi - .02)
        for (z0, z1) in ((zl0 + .02, zt - .04), (zt + .04, zl1 - .03)):
            bw = .045                                          # border of small squares
            nb = max(2, int(round((x1 - x0) / bw)))
            for i in range(nb):
                xa, xb = x0 + (x1 - x0) * i / nb, x0 + (x1 - x0) * (i + 1) / nb
                for (za, zb) in ((z0, z0 + bw), (z1 - bw, z1)):
                    if rnd.random() < .12: counts["missing"] += 1; continue
                    pane(shrink([(xa, za), (xb, za), (xb, zb), (xa, zb)], lead), GL_RED if i % 2 else GL_AMBER)
            nz = max(2, int(round((z1 - z0 - 2 * bw) / bw)))
            for j in range(nz):
                za, zb = z0 + bw + (z1 - z0 - 2 * bw) * j / nz, z0 + bw + (z1 - z0 - 2 * bw) * (j + 1) / nz
                for (xa, xb) in ((x0, x0 + bw), (x1 - bw, x1)):
                    if rnd.random() < .12: counts["missing"] += 1; continue
                    pane(shrink([(xa, za), (xb, za), (xb, zb), (xa, zb)], lead), GL_AMBER if j % 2 else GL_RED)
            fx0, fx1, fz0, fz1 = x0 + bw, x1 - bw, z0 + bw, z1 - bw
            hx, hz = .065, .10
            row = 0; zz = fz0 - hz
            while zz < fz1 + hz:
                xx = fx0 + (hx if row % 2 else 0) - hx
                while xx < fx1 + hx:
                    dia = clip([(xx, zz - hz), (xx + hx, zz), (xx, zz + hz), (xx - hx, zz)], fx0, fx1, fz0, fz1)
                    if len(dia) >= 3:
                        u = rnd.random()
                        if u < .19 + (.08 if zz < fz0 + .25 else 0): counts["missing"] += 1
                        else:
                            mat = GL_PALE if u > .60 else (GL_BLUE if u > .38 else (GL_GREEN if u > .30 else (GL_INK if u > .24 else (GL_AMBER if u > .215 else GL_RED))))
                            pane(shrink(dia, lead), mat)
                    xx += 2 * hx
                zz += hz; row += 1
    b.notes.update(shards=counts, circle=[round(zc, 3), rc])
    return b

# ================================================================================================ manifest / main
def spec_for(name):
    def check(info):
        if name == "HallColumn":
            assert approx(info["size"][0], .90, .05) and approx(info["size"][2], CAP_TOP, .02), info["size"]
        if name == "LongTable":
            assert approx(info["notes"]["top"], .775, .001)
        if name == "Altar":
            assert approx(info["size"][0], 2.38, .05), info["size"]
        if name == "Pew":
            assert approx(info["size"][0], 2.4, .06), info["size"]
    return check

BUILDERS = [
    ("LancetWindow", window_builder("LancetWindow", 1.2, 1.6), 4000), ("TallLancetWindow", window_builder("TallLancetWindow", 1.4, 3.5), 4000),
    ("ClerestoryWindow", window_builder("ClerestoryWindow", 1.2, 1.8), 4000), ("ObservatoryWindow", window_builder("ObservatoryWindow", 1.6, 2.7), 4500),
    # wave FD (r13 critic: "lancet windows: none in the hall"): the north wall's lancets flanking the rose window and at the galleries' ends
    # wave FF (r14 critic: "no lancet windows in the S1 frame"): the flanking pair reaches down behind the north gallery's balustrade (sill 3.55,
    # was 4.3; the head stays under the end formeret), and a blind 1.5 x 2.25 lancet with moonlit glass on each side wall at gallery height
    ("HallLancetWindow", window_builder("HallLancetWindow", 1.0, 3.55), 4000), ("GalleryLancetWindow", window_builder("GalleryLancetWindow", 1.0, 4.4), 4500),
    ("GalleryBlindLancet", window_builder("GalleryBlindLancet", 1.5, 2.25), 4500),
    ("PointedArch", pointed_arch, 2000), ("HallColumn", hall_column, 7000), ("HallCorbel", hall_corbel, 1200),
    ("HallPlinth", plinth, 300), ("HallStringCourse", string_course, 300), ("HallVault", hall_vault, 45000),
    ("Candelabra", candelabra, 6000), ("Candlestick", candlestick_asset, 1500), ("LongTable", trestle_table, 12000),
    ("Pew", pew, 2500), ("Altar", altar, 6000), ("ChapelSunburst", sunburst_window, 16000),
]

def write_fbx_meta(name):
    """New FBX files get a .meta cloned from Pew.fbx.meta (same importer settings) with a path-derived guid. generateSecondaryUV is switched on for
    every asset of this kit (existing metas keep their guid, only that flag changes): the bake then always finds a valid second UV set."""
    meta = cc.CASTLE / (name + ".fbx.meta")
    if meta.exists():
        text = meta.read_text()
    else:
        tmpl = (cc.ROOT / "Assets/HollowCurrent/Resources/Castle/Pew.fbx.meta").read_text().splitlines()
        tmpl[1] = "guid: " + cc.guid(cc.rel_to_project(cc.CASTLE / (name + ".fbx")))
        text = "\n".join(tmpl) + "\n"
    text = text.replace("generateSecondaryUV: 0", "generateSecondaryUV: 1")
    meta.write_text(text)

def main():
    wanted = only()
    reset_scene()
    for name, make, budget in BUILDERS:
        if wanted and name not in wanted:
            continue
        b = make()
        turn_to_unity(b)
        export_asset(b, budget, spec_for(name))
        write_fbx_meta(name)
    if wanted:
        import json
        mp = cc.ROOT / "docs/haunted-castle/art/manifest_hall.json"
        old = json.loads(mp.read_text()) if mp.exists() else {}
        old.update(MANIFEST); MANIFEST.clear(); MANIFEST.update(old)
    write_manifest("hall")
    print("HC_HALL_KIT_PASS", len(MANIFEST))

if __name__ == "__main__":
    main()
