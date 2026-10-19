"""Wave FD (hc/fd-art) offline great-hall proof (CPU only: FBX import + BVH ray casts, no render engine, no GPU).

    /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup -P tools/castle_fd_hall_raycast.py -- OUT_DIR [--scale 0.25]

Imports the exported hall kit FBX files and the giant skeleton, places them as CastleWorld/CastleHallKit do (castle-local Unity coordinates;
Blender (x, y, z) = Unity (-x, -z, y)), adds context boxes (floor, walls with the window openings, galleries, balcony, dais, a sky panel behind
the north wall) and casts one ray per pixel from the S1 capture eye (and two alternates). Writes OUT_DIR/<view>.png (flat colour per class)
and OUT_DIR/hall_metrics.json: pixels per class (scaled to 2416 x 1292) and, per north-wall window, the visible height in pixels.
"""
import sys, math, json
from pathlib import Path
import bpy, numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

sys.path.insert(0, str(Path(__file__).resolve().parent))
import castle_common as cc
ARGV = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = Path(ARGV[0] if ARGV else "/tmp/fdhall"); OUT.mkdir(parents=True, exist_ok=True)
SCALE = float(ARGV[ARGV.index("--scale") + 1]) if "--scale" in ARGV else .25
SKEL_Y = float(ARGV[ARGV.index("--skel-y") + 1]) if "--skel-y" in ARGV else .6

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
def B(p): return Vector((-p[0], -p[2], p[1]))
cache = {}
CLASS = {}          # object name -> class
def load(name):
    if name in cache: return cache[name]
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(cc.CASTLE / (name + ".fbx")))
    objs = [o for o in bpy.data.objects if o not in before and o.type == 'MESH']
    for o in objs: o.hide_render = True
    cache[name] = objs
    return objs
placed = []
def place(name, pos, yaw=0.0, scale=(1, 1, 1), cls=None):
    out = []
    for o in load(name):
        c = o.copy(); c.data = o.data; sc.collection.objects.link(c)
        M = Matrix.Translation(B(pos)) @ Matrix.Rotation(-math.radians(yaw), 4, 'Z') @ Matrix.Diagonal((scale[0], scale[2], scale[1], 1))
        c.matrix_world = M @ o.matrix_world
        CLASS[c.name] = cls or name; out.append(c); placed.append(c)
    return out
def box(c, size, cls):
    bpy.ops.mesh.primitive_cube_add(size=1); o = bpy.context.active_object
    o.dimensions = (size[0], size[2], size[1]); o.location = B(c)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    CLASS[o.name] = cls; placed.append(o); return o

# ---- context
box((0, -.05, 21), (12, .1, 14), "floor")
for sx in (-1, 1): box((sx * 5.9, 4.6, 21), (.65, 9.2, 14), "wall")
box((0, 4.6, 14.1), (12.4, 9.2, .65), "wall")
NORTH = [(-4.85, 1.0, 3.6, 8.0, "GalleryLancetWindow"), (4.85, 1.0, 3.6, 8.0, "GalleryLancetWindow"), (-2.6, 1.0, 4.3, 7.1, "HallLancetWindow"),
         (2.6, 1.0, 4.3, 7.1, "HallLancetWindow"), (0.0, 3.0, 5.6, 8.6, "RoseWindow")]
xs = sorted(set([-6.0, 6.0] + [w[0] - w[1] / 2 for w in NORTH] + [w[0] + w[1] / 2 for w in NORTH]))
for i in range(len(xs) - 1):
    xa, xb = xs[i], xs[i + 1]; mid = (xa + xb) / 2
    cov = [w for w in NORTH if w[0] - w[1] / 2 < mid < w[0] + w[1] / 2]
    y = 0.0
    for w in sorted(cov, key=lambda w: w[2]):
        box((mid, (y + w[2]) / 2, 28.0), (xb - xa, w[2] - y, .9), "wall"); y = w[3]
    box((mid, (y + 9.2) / 2, 28.0), (xb - xa, 9.2 - y, .9), "wall")
box((0, 6, 31), (30, 14, .2), "sky")                                                       # what the openings look out on
for sx in (-1, 1): box((sx * 4.35, 3.1, 21), (2.7, .2, 13.4), "gallery")
box((0, 3.1, 26.85), (6.0, .2, 1.7), "gallery")
box((0, 9.1, 21), (12, .2, 14), "roof")
box((0, .1, 26.525), (5.0, .2, 2.05), "dais"); box((0, .3, 26.925), (3.2, .2, 1.25), "dais")
# ---- kit
for sx in (-1, 1):
    for z in (15.6, 18.6, 21.6, 24.4): place("HallColumn", (sx * 3.9, 0, z), cls="column")
    for zc in (17.1, 20.1, 23.0): place("PointedArch", (sx * 3.9, 0, zc), 90, (1, 1, .93 if zc == 23.0 else 1.0), cls="arch")
    place("HallCorbel", (sx * 3.9, 5.30, 14.425), 0, cls="arch"); place("HallCorbel", (sx * 3.9, 5.30, 27.425), 180, cls="arch")
    for zr in np.arange(15.3, 26.0, 2.0): place("Balustrade", (sx * 3.0, 3.2, zr + 1.0), 90, cls="balustrade")
for xr in (-2.0, 0.0, 2.0): place("Balustrade", (xr, 3.2, 26.0), 0, cls="balustrade")
place("HallVault", (0, 0, 0), cls="vault")
for (x, w, sill, top, art) in NORTH:
    place(art, (x, sill, 28.0), 180, cls="window_%s_%+.2f" % (art, x))
place("LongTable", (0, 0, 23.7), 90, cls="table")
for z in (22.9, 24.5):
    place("HighBackChair", (1.15, 0, z), -90, cls="table"); place("HighBackChair", (-1.15, 0, z), 90, cls="table")
place("Throne", (0, .4, 27.05), 180, cls="throne")
for (x, y, z, yaw) in ((-5.51, 3.5, 19.5, 90), (-5.51, 3.5, 23.3, 90), (5.51, 3.5, 19.5, -90), (5.51, 3.5, 23.3, -90), (-2.4, .28, 27.36, 180), (2.4, .28, 27.36, 180)):
    place("TapestryPanel", (x, y, z), yaw, cls="banner")
sk = place("GiantSkeleton", (0, .05, 18.72), 0, (.6, SKEL_Y, .45), cls="skeleton")
for o in sk:
    if o.data.name.startswith("Shroud") or o.name.startswith("Shroud"): CLASS[o.name] = "shroud"
bpy.context.view_layer.update()
zs = [(o.matrix_world @ v.co) for o in sk for v in list(o.data.vertices)[::50]]
head = max(zs, key=lambda p: -p.y)       # largest Unity z? Unity z = -Blender y; head should be at the SMALL unity z (toward the doors)
zmin_u = min(-p.y for p in zs); zmax_u = max(-p.y for p in zs)
print("HC_FD_HALL skeleton unity z range %.2f..%.2f, y max %.2f" % (zmin_u, zmax_u, max(p.z for p in zs)))

# ---- one BVH
dg = bpy.context.evaluated_depsgraph_get()
verts, polys, cls_of = [], [], []
names = sorted(set(CLASS.values())); idx = {n: i for i, n in enumerate(names)}
for o in placed:
    me = o.evaluated_get(dg).to_mesh(); me.calc_loop_triangles(); base = len(verts); M = o.matrix_world
    verts += [tuple(M @ v.co) for v in me.vertices]
    for t in me.loop_triangles: polys.append(tuple(base + i for i in t.vertices)); cls_of.append(idx[CLASS[o.name]])
    o.evaluated_get(dg).to_mesh_clear()
tree = BVHTree.FromPolygons(verts, polys, all_triangles=True); cls_of = np.array(cls_of)
rng = np.random.default_rng(3)
colors = {n: (rng.integers(40, 255, 3)) for n in names}
fixed = {"sky": (90, 120, 200), "table": (230, 60, 40), "skeleton": (220, 210, 170), "shroud": (140, 20, 30), "column": (60, 200, 90), "arch": (40, 120, 60),
         "floor": (50, 50, 60), "wall": (80, 80, 90), "vault": (110, 110, 120), "gallery": (90, 70, 50), "balustrade": (150, 150, 120), "dais": (60, 60, 70),
         "throne": (200, 120, 200), "banner": (160, 40, 60), "roof": (30, 30, 30)}
for n in names:
    if n in fixed: colors[n] = np.array(fixed[n])
    elif n.startswith("window"): colors[n] = np.array((250, 230, 90))
VIEWS = [("S1_old", (0, 1.64, 14.7), (0, 2.6, 27)), ("S1_fd", (0, 2.25, 14.55), (0, 2.45, 27)), ("S1_fd_b", (0, 2.0, 14.55), (0, 2.3, 27))] + \
        [(n, tuple(map(float, e.split(","))), tuple(map(float, t.split(",")))) for n, e, t in [a.split(":") for a in ARGV if a.count(":") == 2]]
metrics = {"names": names}
W, H = 2416, 1292
for name, eye, tgt in VIEWS:
    w, h = int(W * SCALE), int(H * SCALE)
    e = np.array(eye, float); f = np.array(tgt, float) - e; f /= np.linalg.norm(f)
    r = np.cross([0, 1, 0], f); r /= np.linalg.norm(r); u = np.cross(f, r)
    th = math.tan(math.radians(72) / 2); asp = W / H
    img = np.zeros((h, w, 3), np.uint8); cid = np.full((h, w), -1, int)
    o = B(eye)
    for j in range(h):
        for i in range(w):
            d = f + ((i + .5) / w * 2 - 1) * asp * th * r + (1 - (j + .5) / h * 2) * th * u
            loc, n, fi, dd = tree.ray_cast(o, Vector((-d[0], -d[2], d[1])).normalized(), 80.0)
            if loc is not None:
                c = cls_of[fi]; cid[j, i] = c
                shade = .55 + .45 * abs(n.dot(Vector((.3, .5, .8)).normalized()))
                img[j, i] = np.clip(colors[names[c]] * shade, 0, 255)
    import struct, zlib
    def png(path, a):
        raw = b"".join(b"\x00" + a[y].tobytes() for y in range(a.shape[0]))
        def chunk(t, d): return struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d) & 0xffffffff)
        Path(path).write_bytes(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", a.shape[1], a.shape[0], 8, 2, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(raw, 6)) + chunk(b"IEND", b""))
    png(OUT / (name + ".png"), img)
    m = {}
    k = (W / w) * (H / h)
    for c, n in enumerate(names):
        cnt = int((cid == c).sum())
        if cnt: m[n] = int(cnt * k)
    # per window: visible vertical extent (rows that contain window tracery or sky seen through that window's opening)
    for (x, wd, sill, top, art) in NORTH:
        key = "window_%s_%+.2f" % (art, x)
        rows = set()
        if key in idx: rows |= set(np.where((cid == idx[key]).any(1))[0].tolist())
        # sky pixels inside the window's projected column range
        def proj(p):
            v = np.array(p) - e; zz = v @ f
            return ((v @ r) / zz / (asp * th) * .5 + .5) * w, (.5 - (v @ u) / zz / th * .5) * h
        x0, _ = proj((x - wd / 2, (sill + top) / 2, 27.5)); x1, _ = proj((x + wd / 2, (sill + top) / 2, 27.5))
        lo, hi = int(max(0, min(x0, x1))), int(min(w, max(x0, x1) + 1))
        if "sky" in idx and hi > lo: rows |= set(np.where((cid[:, lo:hi] == idx["sky"]).any(1))[0].tolist())
        m["px_tall_" + key] = int(len(rows) * H / h)
    metrics[name] = m
    print("HC_FD_HALL", name, json.dumps(m), flush=True)
(OUT / "hall_metrics.json").write_text(json.dumps(metrics, indent=1))
