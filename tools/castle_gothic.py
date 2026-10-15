"""Gothic detailing kit for the Haunted Castle (hc/hall-l). Blender only (imports bpy through castle_lib).

Shared by tools/castle_hall_kit.py (the great hall, chapel, windows, candles) and by castle_furniture.py / castle_props.py for the
turned candlestick. Everything here works on a castle_lib.Builder in "design space": X right, Y forward (the front of the asset, towards the
viewer in the room), Z up, metres. turn_to_unity() then bakes the 180 degree turn about Z that makes design-space +Y land on Unity +Z at yaw 0
(Blender's FBX export plus Unity's importer map Blender (x, y, z) to Unity (-x, z, -y); see docs/haunted-castle/handoffs/art-exterior2.md section 4).

Pieces:
  sweep()              a moulded 2D profile swept along a 3D path (ribs, arch bands, window tracery, string courses)
  gothic_window()      lancet window: surround band, spandrels, mullion, sub-arches, oculus with foils, glazing bars, sill
  cluster_column()     four main shafts + four fillets round a core, moulded base, imposts ring, bell capitals, octagonal abacus
  turned_candlestick() round brass baluster with drip pan and a tapered wax candle with a drip collar and runs
  ogive()              the rib/vault profile function (1 - |t|) ** alpha
"""
import math, sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from castle_lib import *
import castle_common as cc
import bmesh

DS, ST, OAK, PLANK, BRASS, IRON, OX, SIL, BONE = ("Castle_DressedStone", "Castle_Stone", "Castle_Oak", "Castle_OakPlank", "Castle_Brass", "Castle_Iron",
                                                   "Castle_Oxblood", "Castle_Silver", "Castle_Bone")
WAX, GL_AMBER, GL_BLUE, GL_RED, GL_INK, FLAME = "Castle_Wax", "Castle_GlassAmber", "Castle_GlassBlue", "Castle_GlassRed", "Castle_GlassInk", "Castle_Flame"
UV_TILE.update({DS: 1.5, WAX: .5, GL_AMBER: 1.0, GL_BLUE: 1.0, GL_RED: 1.0, GL_INK: 1.0})

# ------------------------------------------------------------------------------------------------------------------ axis
Z180 = Matrix.Rotation(math.pi, 4, 'Z')

def turn_to_unity(b):
    """Bake a 180 degree turn about Z into every part (vertices and pivot) so design-space front (+Y) imports as Unity +Z at yaw 0
    and design +X as Unity +X (the old kit's assets land turned by 180 degrees: front +Y on Unity -Z)."""
    for P in b.parts.values():
        bm = P["bm"]
        bmesh.ops.transform(bm, matrix=Z180, verts=bm.verts[:])
        P["origin"] = Z180 @ P["origin"]
    return b

# ------------------------------------------------------------------------------------------------------------------ profiles
def ogive(t, alpha=.6):
    """Pointed profile in [0,1] over t in [-1,1]: 0 at the springings (vertical tangent), 1 at the apex (a point)."""
    return max(0.0, 1.0 - abs(t)) ** alpha

def spread(n, k=1.8):
    """n+1 parameters in [-1,1], clustered towards both ends (where an ogive's slope is steep)."""
    out = []
    for i in range(n + 1):
        tau = -1 + 2 * i / n
        out.append(math.copysign(1 - (1 - abs(tau)) ** k, tau))
    return out

def bar_profile(w, d, ch=.012, front=1.0):
    """Rectangular bar, w across, d deep (u = depth, v = across), chamfered on the front corners. Flat shaded."""
    h, dd = w / 2, d / 2
    return [(-dd, -h, False), (dd - ch, -h, False), (dd, -h + ch, False), (dd, h - ch, False), (dd - ch, h, False), (-dd, h, False)]

def roll_profile(w, d, r=None):
    """Bar with a rounded (roll) front: u = depth, v = across. Smooth on the roll."""
    h, dd = w / 2, d / 2
    r = r or min(h, d * .35)
    pts = [(-dd, -h, False), (dd - r, -h, False)]
    for a in (-60, -30, 0, 30, 60):
        pts.append((dd - r + r * math.cos(math.radians(a)) * 1.0, r * math.sin(math.radians(a)) * (h / r), True))
    pts += [(dd - r, h, False), (-dd, h, False)]
    return pts

def rib_profile(w=.22, depth=.235):
    """Vault rib hung below its path (u across, v up; v = 0 is the webbing surface): keel with chamfers and a bead on the nose."""
    h = w / 2
    return [(-h, .035, False), (-h, -depth * .42, False), (-h * .68, -depth * .72, False), (-h * .36, -depth * .93, True),
            (0, -depth, True), (h * .36, -depth * .93, True), (h * .68, -depth * .72, False), (h, -depth * .42, False), (h, .035, False)]

# ------------------------------------------------------------------------------------------------------------------ sweep
def sweep(b, prof, path, mat, closed=False, N=None, hint=(0, 0, 1), cap=True, tile=None):
    """Sweep the profile [(u, v, smooth_to_next)] along path ((x, y, z) points).
    N given: planar path whose plane normal is N; u runs along N, v along (tangent x N), i.e. outward of a curve that turns left
    (an arch walked left to right has v up and outwards). N None: a path in a vertical plane; u horizontal, v upwards (hint = up).
    Faces carry UVs (metres along the path, metres round the profile) / tile."""
    tile = tile or UV_TILE.get(mat, 1.0)
    P = []
    for p in path:
        p = Vector(p)
        if not P or (p - P[-1]).length > 1e-5:
            P.append(p)
    n = len(P)
    if n < 2:
        return
    tans = []
    for i in range(n):
        t = (P[(i + 1) % n] - P[(i - 1) % n]) if closed else (P[min(i + 1, n - 1)] - P[max(i - 1, 0)])
        tans.append(t.normalized())
    rings, length, sprev = [], [0.0], None
    for i in range(n):
        t = tans[i]
        if N is not None:
            s = Vector(N).normalized(); nn = t.cross(s).normalized()
        else:
            s = t.cross(Vector(hint))
            if s.length < 1e-4:
                s = sprev if sprev is not None else t.cross(Vector((0, 1, 0)))
            s = s.normalized(); nn = s.cross(t).normalized(); sprev = s
        if i:
            length.append(length[-1] + (P[i] - P[i - 1]).length)
        rings.append([b._v(P[i] + s * u + nn * v) for (u, v, _) in prof])
    if closed:
        length.append(length[-1] + (P[0] - P[-1]).length)
    m = len(prof)
    perim = [0.0]
    for k in range(m):
        a, c = prof[k], prof[(k + 1) % m]
        perim.append(perim[-1] + math.hypot(c[0] - a[0], c[1] - a[1]))
    for i in range(n if closed else n - 1):
        i2 = (i + 1) % n
        for k in range(m):
            k2 = (k + 1) % m
            b._f([rings[i][k], rings[i][k2], rings[i2][k2], rings[i2][k]], mat, prof[k][2],
                 [(length[i] / tile, perim[k] / tile), (length[i] / tile, perim[k + 1] / tile),
                  (length[i + 1] / tile, perim[k + 1] / tile), (length[i + 1] / tile, perim[k] / tile)])
    if cap and not closed:
        b._f(list(reversed(rings[0])), mat); b._f(list(rings[-1]), mat)

def circle_path(c, r, ax=(1, 0, 0), ay=(0, 0, 1), n=24):
    c, ax, ay = Vector(c), Vector(ax), Vector(ay)
    return [c + ax * (r * math.cos(i * math.tau / n)) + ay * (r * math.sin(i * math.tau / n)) for i in range(n)]

# ------------------------------------------------------------------------------------------------------------------ gothic window
def gothic_window(name, W, H, lights=2, d=.52, band=.13, mat=DS, glazing=True):
    """Lancet window for a W x H rectangular wall opening. Design space: origin = centre of the sill, front +Y (into the room), depth d
    centred on y = 0. Open tracery, no glass: the sky (and the moons) show through.
      surround   jambs and a pointed arch band, 0.13 wide, chamfered inner arris (one sweep)
      spandrels  stone between the arch and the opening's top corners, so the opening is sealed
      lights     1 or 2 pointed lights with a roll-moulded mullion; each light has its own arch
      oculus     in the head: a ring with four foils when 2 lights
      glazing    thin iron saddle bars across each light
    Returns the Builder. notes carry spring, the arch apex and the oculus."""
    b = Builder(name)
    hw = W / 2; hwi = hw - band; R = 2 * hwi
    apex_out = max(z for x, z in arch_curve(hwi, 0, R, 24, band))
    spring = H - apex_out
    rise_i = arch_rise(hwi, R)
    mid = band / 2
    # 1. surround: left jamb up, arch, right jamb down (one sweep, planar in XZ, normal +Y)
    arch_mid = arch_curve(hwi, spring, R, 16, mid)
    path = [(-(hwi + mid), 0, .0)] + [(x, 0, z) for x, z in arch_mid] + [(hwi + mid, 0, .0)]
    h2 = band / 2; dd = d / 2; ch = .035
    surround = [(-dd, -h2, False), (-dd, h2, False), (dd - ch, h2, False), (dd, h2 - ch, False), (dd, -h2 + .05, False), (dd - .05, -h2, False)]
    sweep(b, surround, path, mat, N=(0, 1, 0))
    # 2. spandrels
    ext = arch_curve(hwi, spring, R, 16, band)
    n_ = 16
    b.poly(ext[:n_ + 1] + [(-hw, H)], -dd, dd, mat, 'XZ')
    b.poly(ext[n_:] + [(hw, H)], -dd, dd, mat, 'XZ')
    # 3. mullion(s), sub-arches (lights = 0: surround only, the caller fits its own tracery)
    m = .07
    bar_d = d * .46
    oc = None
    if lights >= 2:
        hl = (hwi - m / 2) / 2
        rise_s = arch_rise(hl, 2 * hl)
        for sx in (-1, 1):
            xc = sx * (hwi + m / 2) / 2
            sub = arch_curve(hl, spring, 2 * hl, 10, m / 2)
            sweep(b, bar_profile(m, bar_d, .014), [(xc + x, .0, z) for x, z in sub], mat, N=(0, 1, 0))
        z_sub = spring + rise_s + m          # top of the sub-arch extrados at its crown
        oc_r = (spring + rise_i - z_sub) / 2 - .035
        oc_c = (spring + rise_i + z_sub) / 2
        oc = (oc_c, max(.06, oc_r))
        sweep(b, roll_profile(m, bar_d), [(0, 0, .06), (0, 0, oc_c - oc[1])], mat, N=(0, 1, 0))
    # 4. oculus with foils
    if oc:
        c, r = oc
        tube = bar_profile(.045, bar_d * .9, .01)
        sweep(b, tube, [(x, 0, z) for x, z in [(p.x, p.z) for p in circle_path((0, 0, c), r, n=20)]], mat, closed=True, N=(0, 1, 0))
        fr = r * .5
        for k in range(4):
            a = math.radians(45 + 90 * k)
            fc = (r * .5 * math.cos(a), 0, c + r * .5 * math.sin(a))
            sweep(b, bar_profile(.032, bar_d * .8, .008), [(p.x, 0, p.z) for p in circle_path(fc, fr * .9, n=12)], mat, closed=True, N=(0, 1, 0))
        b.notes.update(oculus_centre=round(c, 3), oculus_radius=round(r, 3))
    # 5. glazing bars (iron saddle bars across each light, below the spring)
    if glazing and lights >= 1:
        light_x = [(-hwi, hwi)] if lights == 1 else [(-hwi, -m / 2), (m / 2, hwi)]
        nb = max(2, int(spring / .55))
        for (x0, x1) in light_x:
            for k in range(1, nb + 1):
                z = spring * k / (nb + 1)
                b.box(((x0 + x1) / 2, -.02, z), (x1 - x0, .026, .02), IRON)
    # 6. sill: sloped front, a drip below
    sill = [(-dd, 0), (dd + .09, 0), (dd + .09, .035), (dd + .03, .075), (-dd, .075)]
    b.poly(sill, -hw, hw, mat, 'YZ')
    b.notes.update(spring=round(spring, 3), apex=round(spring + rise_i, 3), band=band, depth=d, lights=lights, hwi=hwi, R=R, rise_i=round(rise_i, 3))
    return b

# ------------------------------------------------------------------------------------------------------------------ cluster column
def lathe_at(b, c, prof, seg, mat, **kw):
    b.lathe(c, prof, seg, mat, **kw)

def octagon(b, c, across_flats, z0, z1, mat, chamfer=0.0):
    """Regular octagonal block, flats facing the axes, from z0 to z1 (an optional chamfer on the top edge)."""
    rv = across_flats / 2 / math.cos(math.radians(22.5))
    prof = [(0, z0), (rv, z0), (rv, z1 - chamfer), (rv - chamfer, z1), (0, z1)] if chamfer else [(0, z0), (rv, z0), (rv, z1), (0, z1)]
    with b.xf(about(c, Rz(22.5))):
        b.lathe(c, prof, 8, mat, smooth=False, cap=False)

def cluster_column(name="HallColumn", height=5.85, seg=16, mat=DS, imposts=(2.0,), band_z=(3.6,)):
    """A bundle of four main shafts (N, E, S, W; r 0.15 at 0.27 from the axis) and four diagonal fillets round a core, on a stepped plinth with
    moulded bases, ring bands at the arcade imposts, bell capitals under one octagonal abacus whose top is at `height`.
    Footprint 0.92 x 0.92 (the hall's pier collider is 0.9 square). Origin: floor centre."""
    b = Builder(name)
    # plinth: two steps
    b.box((0, 0, .15), (.92, .92, .30), mat)
    b.box((0, 0, .36), (.86, .86, .12), mat)
    zb = .42                                   # top of plinth
    # base moulding profile (r, z): bead, scotia, bead
    def base(r):
        return [(0, zb), (r + .045, zb), (r + .055, zb + .018), (r + .05, zb + .05), (r + .022, zb + .075), (r + .005, zb + .095), (r + .018, zb + .12),
                (r + .03, zb + .145), (r + .028, zb + .175), (r, zb + .205)]
    z0 = zb + .205
    zc0 = height - .88                         # start of capital
    # core
    rc = .255
    prof = base(rc)
    prof += [(rc, z0)] + [(rc, z) for z in imposts]
    core = base(rc)
    for z in imposts:
        core += [(rc, z - .09), (rc + .04, z - .06), (rc + .05, z), (rc + .04, z + .06), (rc, z + .09)]
    for z in band_z:
        core += [(rc, z - .04), (rc + .025, z), (rc, z + .04)]
    core += [(rc, zc0), (rc + .04, zc0 + .05), (rc + .07, zc0 + .22), (rc + .10, zc0 + .50), (rc + .115, zc0 + .66), (rc + .09, zc0 + .66), (0, zc0 + .66)]
    b.lathe((0, 0, 0), core, seg, mat, cap=False)
    # four main shafts
    rs, off = .15, .245
    for k in range(4):
        a = math.radians(90 * k)
        c = (off * math.cos(a), off * math.sin(a), 0)
        sh = base(rs)
        for z in imposts:
            sh += [(rs, z - .09), (rs + .03, z - .06), (rs + .04, z), (rs + .03, z + .06), (rs, z + .09)]
        for z in band_z:
            sh += [(rs, z - .035), (rs + .022, z), (rs, z + .035)]
        # capital: neck ring, flaring bell, round abacus bead
        sh += [(rs, zc0 - .03), (rs + .025, zc0), (rs + .025, zc0 + .04), (rs, zc0 + .06),
               (rs + .012, zc0 + .14), (rs + .03, zc0 + .30), (rs + .045, zc0 + .45), (rs + .05, zc0 + .56), (rs + .052, zc0 + .60),
               (rs + .04, zc0 + .66), (0, zc0 + .66)]
        b.lathe(c, sh, seg, mat, cap=False)
    # four diagonal fillets
    rf, foff = .075, .31
    for k in range(4):
        a = math.radians(45 + 90 * k)
        c = (foff * math.cos(a) * .72, foff * math.sin(a) * .72, 0)
        fl = [(0, zb), (rf + .03, zb), (rf + .035, zb + .04), (rf, zb + .09), (rf, zc0 + .10), (rf + .02, zc0 + .14), (rf + .035, zc0 + .30), (rf + .02, zc0 + .46), (0, zc0 + .50)]
        b.lathe(c, fl, 8, mat, cap=False)
    # abacus: regular octagon (0.90 across flats), two steps, flat top at `height`
    zt = height
    octagon(b, (0, 0, 0), .86, zt - .22, zt - .10, mat)
    octagon(b, (0, 0, 0), .90, zt - .10, zt, mat, chamfer=.03)
    b.notes.update(height=height, footprint=.92, spring=height)
    return b

# ------------------------------------------------------------------------------------------------------------------ candles
def candle_flame(b, base, h=.06):
    """Flame part (Castle_Flame) with its pivot at `base`: a small teardrop lathe (the runtime hides it and puts a billboard on its base)."""
    b.part("Flame_%d" % len([p for p in b.parts if p.startswith("Flame_")]), base)
    b.lathe(base, [(0, 0), (.0085 * h / .06, .011 * h / .06), (.011 * h / .06, .032 * h / .06), (.006 * h / .06, .06 * h / .06), (0, h)], 8, FLAME, smooth=True, cap=False)

def turned_candlestick(b, c=(0, 0, 0), s=1.0, taper=.30, burn=1.0, seg=16, flame=False, brass=BRASS, wax=WAX):
    """Round turned brass candlestick, base at c: domed foot, baluster stem with a swelling knop, drip pan, socket; a tapered wax candle
    (radius 0.0135 -> 0.0115, i.e. 2.7 cm across, `taper` m long: 30 cm baseline agreed with hc/props-l) with a melted collar, a wax pool in
    the top and two drips running down. Everything is a surface of revolution (smooth normals): no hex prisms.
    Returns the candle-top height above c (m, times s). flame=True adds a Flame_<n> part (pivot at the candle top) as the runtime expects."""
    x, y, z0 = c
    k = s
    def L(profile, mat, cap=False):
        b.lathe((x, y, z0), [(r * k, z * k) for (r, z) in profile], seg, mat, cap=cap)
    body = [(0, 0), (.060, 0), (.064, .005), (.062, .012), (.048, .020), (.030, .030), (.020, .038), (.018, .048), (.026, .060), (.031, .070), (.026, .080), (.018, .090),
            (.016, .102), (.020, .112), (.024, .120), (.021, .124), (.045, .128), (.050, .131), (.050, .136), (.044, .136), (.030, .134), (.025, .140),
            (.022, .156), (.0245, .164), (.0235, .168), (.0145, .168), (0, .168)]
    L(body, brass)
    base = .160                              # wax starts in the socket, a little above the socket lip
    zt = base + taper                        # candle top
    waxp = [(0, base), (.0150, base), (.0185, base + .012), (.0160, base + .026), (.0135, base + .045), (.0128, zt - .020), (.0120, zt - .006),
            (.0125, zt), (.0075, zt - .004 * burn), (0, zt - .007 * burn)]
    L(waxp, wax)
    for a, ln in ((35, .052), (200, .034)):
        ar = math.radians(a); rr = .0132
        ztop = zt - .008
        pts = [(x + rr * math.cos(ar) * k, y + rr * math.sin(ar) * k, z0 + ztop * k),
               (x + (rr + .0035) * math.cos(ar) * k, y + (rr + .0035) * math.sin(ar) * k, z0 + (ztop - ln * .55) * k),
               (x + (rr + .002) * math.cos(ar) * k, y + (rr + .002) * math.sin(ar) * k, z0 + (ztop - ln) * k)]
        b.tube(pts, [.0030 * k, .0036 * k, 0], 5, wax, cap=False)
    if flame:
        candle_flame(b, (x, y, z0 + zt * k), .06 * k)
    return zt * k
