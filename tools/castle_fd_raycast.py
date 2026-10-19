"""Wave FD (hc/fd-art) offline exterior proof, Blender half. CPU only: BVH ray casts, no render engine, no GPU.

    /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup -P tools/castle_fd_raycast.py -- OUT_DIR [--scale 0.25]

Builds the exterior kit exactly as tools/castle_exterior.py exports it, plus a proxy of the castle massing and of the wave FD approach
(blood lake at y -1.6, side/back moats, banks, causeway with piers, rails and lamp posts, far landing with braziers, ruined gateway, broken wall,
shore rocks), all in castle-local Unity coordinates (mirrors CastleWorld.Exterior / CastleExterior.Approach; keep in step). Casts one ray per pixel
for the capture views and writes OUT_DIR/<view>.npz (hit, distance, normal, material id, hit point, ray direction) and materials.json.
tools/castle_fd_shade.py shades them (sky equirect, moonlight, brazier lights, exponential fog, and a numpy twin of CastleWater.shader).
"""
import sys, math, json
from pathlib import Path
import bpy, bmesh, numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

sys.path.insert(0, str(Path(__file__).resolve().parent))
ARGV = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = Path(ARGV[0] if ARGV else "/tmp/fdproof"); OUT.mkdir(parents=True, exist_ok=True)
SCALE = float(ARGV[ARGV.index("--scale") + 1]) if "--scale" in ARGV else .25
ONLY = ARGV[ARGV.index("--views") + 1].split(",") if "--views" in ARGV else None
sys.argv = [sys.argv[0], "--", "--no-export"]          # castle_exterior reads its own ARGS at import
import castle_exterior as ext

WATER_Y = -1.6
VIEWS = [  # name, eye, target or (pitch) , vertical fov, size (the capture sizes)
    ("S4_exterior_sky", (0, 1.64, -22.0), ("pitch", 28.5), 90, (2416, 1292)),
    ("art_exterior_gate_out_S6", (0.3, 1.64, 1.4), (0.2, 2.2, -20), 72, (2416, 1292)),
    ("art_exterior_battlement_out", (3.6, 9.0, -0.4), (0.0, 3.0, -30), 72, (2416, 1292)),
    ("art_exterior_bridge_keep", (0, 1.7, -6.1), (0, 16.0, 19.5), 72, (2416, 1292)),
    ("S4_old_eye", (0, 2.5, -14.0), ("pitch", 27.0), 95, (2416, 1292)),
]
# wave FF (hc/ff-art): the r14 capture set at the critic's 1920x1080 (S4 search eye, art_exterior views at the player's 72 deg FOV), incl. bridge_left
VIEWS_FF = [
    ("S4_exterior_sky", (0, 1.64, -22.0), ("pitch", 28.5), 90, (1920, 1080)),
    ("art_exterior_battlement_out", (3.6, 9.0, -0.4), (0.0, 3.0, -30), 72, (1920, 1080)),
    ("art_exterior_bridge_left", (-1.2, 1.7, -3.5), (-14, 4.5, 3), 72, (1920, 1080)),
    ("art_exterior_gate_out_S6", (0.3, 1.64, 1.4), (0.2, 2.2, -20), 72, (1920, 1080)),
]
if "--ff" in ARGV: VIEWS = VIEWS_FF

def build():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    objs = []
    for name, make, budget in ext.PIECES:
        o, info = make().b.finish(); objs += o
    K = ext.K
    px = K("Proxy")
    for nm in ("Proof_Wall", "Proof_Roof", "Proof_Water", "Proof_Earth", "Proof_Wood", "Proof_Plank", "Proof_Iron", "Proof_Flame", "Proof_Tree", "Proof_Paving"):
        ext.cc.MATERIALS[nm] = ((.1, .1, .1), 0, 0, False)
    def blk(x0, x1, y0, y1, z0, z1, mat="Proof_Wall"): px.box(((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), (x1 - x0, y1 - y0, z1 - z0), mat)
    # castle massing (tools/castle_exterior_proof.py)
    blk(-14.45, -1.5, -3.2, 6.4, -.45, .45); blk(1.5, 14.45, -3.2, 6.4, -.45, .45)
    blk(-6, -1.5, 6.4, 7.5, -.45, .45); blk(1.5, 6, 6.4, 7.5, -.45, .45); blk(-1.5, 1.5, 4.5, 7.5, -.45, .45)
    blk(-14.45, -13.55, -3.2, 6.4, -.45, 28.45); blk(13.55, 14.45, -3.2, 6.4, -.45, 19.4); blk(13.55, 14.45, -3.2, 11.6, 19.4, 28.45)
    blk(-14.45, -6, -3.2, 6.4, 27.55, 28.45); blk(-6, 6, -3.2, 9.2, 27.55, 28.45); blk(6, 14.45, -3.2, 11.6, 27.55, 28.45)
    for sx in (-1, 1):
        blk(sx * 6 - .3, sx * 6 + .3, -3.2, 6.4, 0, 14); blk(sx * 6 - .3, sx * 6 + .3, -3.2, 9.2, 14, 28)
    blk(-6, 6, -3.2, 7.5, 5.7, 6.3); blk(-6, 6, -3.2, 9.2, 13.7, 14.3)
    blk(-6, 6, 5.0, 5.2, -.45, 6.3, "Proof_Roof"); blk(-14, -6, 6.2, 6.4, 0, 28, "Proof_Roof"); blk(6, 14, 6.2, 6.4, 0, 28, "Proof_Roof")
    blk(-6, 6, 9.0, 9.2, 14, 28, "Proof_Roof"); blk(6, 14, 11.4, 11.6, 19.4, 28, "Proof_Roof"); blk(6, 14, 6.4, 11.6, 19.1, 19.7)
    blk(-6, 6, -.02, 0, 0, 14, "Proof_Roof")
    for sx in (-1, 1): px.box((sx * 1.7, 2.25, 0), (.4, 4.5, 1.0), "Castle_Stone")
    px.box((0, 4.7, 0), (3.8, .4, 1.0), "Castle_Stone")
    px.box((0, -.125, -3.45), (3.24, .25, 6.0), "Proof_Plank")          # lowered leaf
    for sx in (-1, 1):                                                 # wave FD: open timber railing on the leaf
        for pz in (-.25, -1.2, -2.2, -3.2, -4.25, -5.3, -6.2): px.box((sx * 1.5, .45, pz - .45), (.13, .9, .13), "Proof_Plank")
        px.box((sx * 1.5, .97, -3.55), (.1, .09, 5.9), "Proof_Plank"); px.box((sx * 1.5, .52, -3.55), (.07, .07, 5.9), "Proof_Plank")
    for sx in (-1, 1):
        a = np.array([sx * 1.5, 0.0, -6.35]); c = np.array([sx * 1.5, 4.4, -.75])
        for i in range(24):
            q = a + (c - a) * (i + .5) / 24
            px.box(tuple(q), (.12, .12, .3), "Castle_Iron")
    # ---- wave FD approach (CastleWorld.Exterior + CastleExterior.Approach)
    near, far, hw, cs, ch = -.45, -16.0, 24.0, -6.3, 2.3
    blk(-hw, hw, WATER_Y - .05, WATER_Y, far, near, "Proof_Water")
    for z0, z1 in ((28.45, 33.6),): blk(-19.5, 19.5, WATER_Y - .05, WATER_Y, z0, z1, "Proof_Water")
    for x0, x1 in ((-19.5, -14.45), (14.45, 19.5)): blk(x0, x1, WATER_Y - .05, WATER_Y, near, 28.45, "Proof_Water")
    blk(-60, 60, -2.0, 0, far - 55, far, "Proof_Earth")
    for x0, x1 in ((-60.5, -19.5), (19.5, 60.5)): blk(x0, x1, -2.0, 0, near, 33.6, "Proof_Earth")
    for x0, x1 in ((-60.5, -hw), (hw, 60.5)): blk(x0, x1, -2.0, 0, far, near, "Proof_Earth")
    blk(-60, 60, -2.0, 0, 33.6, 90, "Proof_Earth")
    blk(-ch + .1, ch - .1, -.18, 0, far, cs, "Proof_Plank")
    for sx in (-1, 1): blk(sx * (ch - .08) - .11, sx * (ch - .08) + .11, -.44, -.08, far, cs, "Castle_StoneDark")
    z = cs - 1.6
    while z > far + .8:
        blk(-ch - .25, ch + .25, -3.0, -.2, z - .55, z + .55, "Castle_StoneDark")
        blk(-ch - .35, ch + .35, -.39, -.25, z - .65, z + .65, "Castle_StoneDark")
        z -= 3.6
    for sx in (-1, 1):
        x = sx * (ch - .1)
        z = cs - .1
        while z >= far - .01:
            blk(x - .08, x + .08, 0, 1.1, z - .08, z + .08, "Proof_Plank"); z -= 1.8
        blk(x - .05, x + .05, .97, 1.07, far, cs, "Proof_Plank"); blk(x - .035, x + .035, .51, .59, far, cs, "Proof_Plank")
        for lz in (-7.4, far + .6):
            lx = sx * 2.3
            blk(lx - .055, lx + .055, 0, 2.1, lz - .055, lz + .055, "Proof_Iron")
            blk(lx - .15, lx + .15, 1.92, 2.35, lz - .15, lz + .15, "Proof_Iron")
            blk(lx - .07, lx + .07, 1.97, 2.2, lz - .07, lz + .07, "Proof_Flame")
    blk(-5.5, 5.5, -.08, .02, far - 8.8, far, "Proof_Paving")
    for sx in (-1, 1):
        bx, bz = sx * 3.0, -23.0
        blk(bx - .41, bx + .41, 1.03, 1.13, bz - .41, bz + .41, "Proof_Iron"); blk(bx - .05, bx + .05, 0, 1.05, bz - .05, bz + .05, "Proof_Iron")
        blk(bx - .2, bx + .2, 1.15, 1.85, bz - .2, bz + .2, "Proof_Flame")
        x, h = sx * 4.5, (4.6 if sx < 0 else 3.6)
        gz = -23.9
        blk(x - .5, x + .5, 0, h, gz - .5, gz + .5, "Castle_StoneDark"); blk(x - .6, x + .6, h, h + .18, gz - .6, gz + .6, "Castle_StoneDark")
        for k in range(4):
            wx, wh = sx * (5.8 + k * 1.6), 1.7 - k * .35 + (k % 2) * .2
            blk(wx - .78, wx + .78, 0, wh, gz - .35, gz + .35, "Castle_StoneDark")
    blk(-4.6, -2.2, 4.6, 5.05, -24.3, -23.5, "Castle_StoneDark")
    for (tx, tz, s_) in ((-8.6, -18.6, 1.7), (10.2, -19.2, 2.0), (-15.5, -18.0, 2.3), (17, -17.8, 1.6), (-23, 2.2, 2.6), (24.5, 4.0, 2.2), (-29, -1.2, 1.9), (30, .6, 2.4)):
        blk(tx - .12 * s_, tx + .12 * s_, 0, 2.6 * s_, tz - .12 * s_, tz + .12 * s_, "Proof_Tree")
        blk(tx - .9 * s_, tx + .1 * s_, 1.6 * s_, 1.7 * s_, tz - .05, tz + .05, "Proof_Tree"); blk(tx - .1, tx + .8 * s_, 2.0 * s_, 2.1 * s_, tz - .05, tz + .05, "Proof_Tree")
    rnd = np.random.default_rng(7717)
    def rock(x, y, z, s): px.rbox((x, y, z), (s * (.9 + rnd.random() * .5), s * .7, s * (.9 + rnd.random() * .5)), float(rnd.random() * 90), "Castle_Basalt")
    x = -hw
    while x <= hw:
        if abs(x) >= ch + .8:
            s_ = .6 + rnd.random() * 1.4; rock(x, WATER_Y + .05 + s_ * .1, far + (rnd.random() - .5) * 1.4, s_)
        x += 1.7
    for sx in (-1, 1):
        z = far + 1
        while z <= near - .5:
            s_ = .7 + rnd.random() * 1.5; rock(sx * (hw + (rnd.random() - .3) * 1.2), WATER_Y + .1 + s_ * .12, z, s_); z += 1.8
        x = 20.0
        while x <= hw:
            s_ = .7 + rnd.random() * 1.6; rock(sx * x, WATER_Y + .1 + s_ * .12, near + (rnd.random() - .3) * 1.2, s_); x += 1.8
    for (ox, oz, s_) in ((-11.5, -21.4, 2.6), (13.4, -21.8, 3.1), (-20, -20.4, 3.6), (21.5, -20.8, 2.8), (-29, -14.8, 4.2), (30, -12.4, 3.8)):
        rock(ox, -.3, oz, s_)
    po, pinfo = px.b.finish(); objs += po
    return objs

def joined(objs):
    """One BVH over every object, with a material name per triangle (BVHTree.FromPolygons)."""
    dg = bpy.context.evaluated_depsgraph_get()
    verts, polys, mats = [], [], []
    for o in objs:
        me = o.evaluated_get(dg).to_mesh()
        me.calc_loop_triangles()
        base = len(verts); M = o.matrix_world
        verts += [tuple(M @ v.co) for v in me.vertices]
        for t in me.loop_triangles:
            polys.append(tuple(base + i for i in t.vertices))
            m = o.material_slots[t.material_index].material if o.material_slots else None
            mats.append(m.name.split(".")[0] if m else "none")
        o.evaluated_get(dg).to_mesh_clear()
    names = sorted(set(mats)); idx = {n: i for i, n in enumerate(names)}
    return BVHTree.FromPolygons(verts, polys, all_triangles=True), np.array([idx[m] for m in mats], np.int16), names

def U(p): return Vector((-p[0], -p[2], p[1]))
def toU(v): return (-v.x, v.z, -v.y)

def main():
    objs = build()
    tree, mat_of, names = joined(objs)
    (OUT / "materials.json").write_text(json.dumps(names))
    for name, eye, tgt, fov, (W, H) in VIEWS:
        if ONLY and name not in ONLY: continue
        w, h = int(W * SCALE), int(H * SCALE)
        e = np.array(eye, float)
        if tgt[0] == "pitch":
            p = math.radians(tgt[1]); f = np.array([0, math.sin(p), math.cos(p)])
        else:
            f = np.array(tgt, float) - e; f /= np.linalg.norm(f)
        r = np.cross([0, 1, 0], f); r /= np.linalg.norm(r); u = np.cross(f, r)
        th = math.tan(math.radians(fov) / 2); asp = W / H
        ys, xs = np.mgrid[0:h, 0:w].astype(np.float64)
        nx = ((xs + .5) / w * 2 - 1) * asp * th; ny = (1 - (ys + .5) / h * 2) * th
        d = f[None, None] + nx[..., None] * r[None, None] + ny[..., None] * u[None, None]
        d /= np.linalg.norm(d, axis=-1, keepdims=True)
        # r = cross(up, f) is the camera's right in Unity's frame (f = +z gives r = +x), the same convention as castle_exterior_composite.py
        hit = np.zeros((h, w), bool); dist = np.full((h, w), np.inf, np.float32); nrm = np.zeros((h, w, 3), np.float32)
        mid = np.full((h, w), -1, np.int16); pos = np.zeros((h, w, 3), np.float32)
        o = U(eye)
        for j in range(h):
            for i in range(w):
                dv = d[j, i]
                loc, n, fi, dd = tree.ray_cast(o, Vector((-dv[0], -dv[2], dv[1])), 75.0)
                if loc is not None:
                    hit[j, i] = True; dist[j, i] = dd; mid[j, i] = mat_of[fi]
                    nn = toU(n); pos[j, i] = toU(loc)
                    if np.dot(nn, dv) > 0: nn = (-nn[0], -nn[1], -nn[2])
                    nrm[j, i] = nn
        np.savez_compressed(OUT / (name + ".npz"), hit=hit, dist=dist, nrm=nrm, mid=mid, pos=pos, dir=d.astype(np.float32), eye=e, fwd=f, fov=fov, size=np.array([W, H]))
        print("HC_FD_RAYCAST", name, w, h, "hit %.1f%%" % (100 * hit.mean()), flush=True)

main()
