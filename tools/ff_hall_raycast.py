"""Wave FF (hc/ff-art) offline great-hall proof and S1 test masks (CPU only: FBX import + BVH ray casts, no render engine, no GPU).

    /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup -P tools/ff_hall_raycast.py -- OUT_DIR [--design old|ff] [--scale 1.0]
        [--eye x,y,z --target x,y,z] [--masks]

Imports the exported hall kit FBX files (columns, arcade, corbels, vault, balustrades, windows, table, chairs, throne, tapestries, skeleton,
floating candelabras) and places them as CastleWorld / CastleHallKit / CastleLayout do (castle-local Unity coordinates; Blender (x, y, z) =
Unity (-x, -z, y)), adds context boxes (floor, walls with the window openings, galleries, ledge, dais, cove ceiling, chandelier, books) and a sky
panel behind every opening, then casts one ray per pixel from the S1 eye at 1920x1080, FOV 72. --design old is build 6229407 (r14), ff is this
branch (keep in step with CastleLayout.Windows / FfHall and CastleWorld.HallArcade). Writes OUT_DIR/<view>.png (flat colour per class),
OUT_DIR/<view>_class.npz and OUT_DIR/hall_metrics.json; with --masks also tools/ff_art_masks/S1_*.png + hall_masks.json for
tools/check_art_views.py (lancet openings, piers, table, floor; the rose window's x and the horizon row).
"""
import sys, math, json
from pathlib import Path
import bpy, numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

sys.path.insert(0, str(Path(__file__).resolve().parent))
import castle_common as cc
ARGV = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
def arg(k, d=None): return ARGV[ARGV.index(k) + 1] if k in ARGV else d
OUT = Path(ARGV[0] if ARGV and not ARGV[0].startswith("--") else "/tmp/ffhall"); OUT.mkdir(parents=True, exist_ok=True)
SCALE = float(arg("--scale", "1.0"))
DESIGN = arg("--design", "ff")
FF = DESIGN == "ff"
W, H, FOV = 1920, 1080, 72.0

# ---------------------------------------------------------------- the design (mirror of the C#)
# north wall: (x, width, sill, top, art)
NORTH_OLD = [(-4.85, 1.0, 3.6, 8.0, "GalleryLancetWindow"), (4.85, 1.0, 3.6, 8.0, "GalleryLancetWindow"),
             (-2.6, 1.0, 4.3, 7.1, "HallLancetWindow"), (2.6, 1.0, 4.3, 7.1, "HallLancetWindow"), (0.0, 3.0, 5.6, 8.6, "RoseWindow")]
NORTH_FF = [(-4.85, 1.0, 3.6, 8.0, "GalleryLancetWindow"), (4.85, 1.0, 3.6, 8.0, "GalleryLancetWindow"),
            (-2.6, 1.0, 3.55, 7.1, "HallLancetWindow"), (2.6, 1.0, 3.55, 7.1, "HallLancetWindow"), (0.0, 3.0, 5.6, 8.6, "RoseWindow")]
NORTH = NORTH_FF if FF else NORTH_OLD
# wave FF blind gallery lancets on the side walls (CastleLayout.GalleryBlindLancets): (x face, z centre, width, sill, top)
SIDE = [(-5.575, 21.6, 1.5, 5.1, 7.35), (5.575, 21.6, 1.5, 5.1, 7.35)] if FF else []
LEDGE_OLD = [(-1.6, 5.4, 26.8), (1.6, 5.4, 26.8), (-2.0, 5.2, 27.0), (2.0, 5.2, 27.0), (-2.3, 5.6, 27.3), (2.3, 5.6, 27.3)]
LEDGE_FF = [(-1.45, 5.4, 26.8), (1.45, 5.4, 26.8), (-1.75, 5.2, 27.0), (1.75, 5.2, 27.0), (-1.95, 5.6, 27.3), (1.95, 5.6, 27.3)]
LEDGE = LEDGE_FF if FF else LEDGE_OLD
ARCADE = not FF               # wave FF: no PointedArch under the galleries (the columns stand floor to gallery)
HALL_CANDELABRAS = [(-2.3, 2.7, 17.2), (2.4, 3.0, 17.8), (-1.2, 4.8, 16.5), (1.4, 5.4, 18.9), (-1.8, 3.3, 15.8), (2.5, 4.4, 22.1), (-0.4, 6.0, 20.5), (-4.8, 6.2, 17.5),
                    (4.6, 5.4, 16.8), (-3.9, 5.8, 23.4), (3.9, 5.6, 24.2), (0.3, 3.5, 23.1), (2.0, 6.4, 24.0), (-2.1, 6.6, 23.0)]
HALL_BOOKS = [(2.0, 3.3, 17.5), (3.0, 4.3, 19.2), (1.2, 5.2, 21.0), (2.6, 3.6, 21.5), (-2.2, 3.1, 16.8), (0.3, 4.6, 17.6), (-0.5, 5.6, 22.4), (3.2, 5.2, 23.0),
              (1.8, 2.8, 19.5), (-2.6, 5.6, 24.0)]
S5E, S5S = np.array([0, 6.33, 24.2]), np.array([0, 6.52, 26.9]); S6E, S6S = np.array([-.3, 1.15, 1.0]), np.array([-.9, 1.2, 4.4])
def seg_d(p, a, b):
    ab = b - a; t = np.clip(np.dot(p - a, ab) / max(1e-4, ab @ ab), 0, 1); return np.linalg.norm(p - (a + ab * t))
def near_line(p, r=1.2): p = np.array(p); return seg_d(p, S5E, S5S) < r or seg_d(p, S6E, S6S) < r
EYE_OLD, TGT_OLD = (0.0, 2.3, 14.55), (0.0, 2.3, 27.0)
EYE_FF, TGT_FF = (0.0, 3.6, 16.0), (0.0, 1.27, 28.0)            # CaptureMode S1 (wave FG, hc/fg-polish: gallery height, 11 deg down the axis)
# wave FF S1 was eye (0, 2.95, 14.55) target (0, 2.55, 27.0): pass it with --eye / --target to reproduce the FF masks
eye = tuple(map(float, arg("--eye").split(","))) if arg("--eye") else (EYE_FF if FF else EYE_OLD)
tgt = tuple(map(float, arg("--target").split(","))) if arg("--target") else (TGT_FF if FF else TGT_OLD)

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
def B(p): return Vector((-p[0], -p[2], p[1]))
cache = {}; CLASS = {}; placed = []
def load(name):
    if name in cache: return cache[name]
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(cc.CASTLE / (name + ".fbx")))
    objs = [o for o in bpy.data.objects if o not in before and o.type == 'MESH']
    for o in objs: o.hide_render = True
    cache[name] = objs
    return objs
def place(name, pos, yaw=0.0, scale=(1, 1, 1), cls=None):
    out = []
    for o in load(name):
        c = o.copy(); c.data = o.data; sc.collection.objects.link(c)
        M = Matrix.Translation(B(pos)) @ Matrix.Rotation(-math.radians(yaw), 4, 'Z') @ Matrix.Diagonal((scale[0], scale[2], scale[1], 1))
        c.matrix_world = M @ o.matrix_world
        CLASS[c.name] = cls or name; out.append(c); placed.append(c)
    return out
def box(c, size, cls, rot_y=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1); o = bpy.context.active_object
    o.dimensions = (size[0], size[2], size[1]); o.location = B(c); o.rotation_euler = (0, 0, -math.radians(rot_y))
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    CLASS[o.name] = cls; placed.append(o); return o

# ---- context
box((0, -.05, 21), (12, .1, 14), "floor")
for sx in (-1, 1): box((sx * 5.9, 4.6, 21), (.65, 9.2, 14), "wall")
box((0, 4.6, 14.1), (12.4, 9.2, .65), "wall")
xs = sorted(set([-6.0, 6.0] + [w[0] - w[1] / 2 for w in NORTH] + [w[0] + w[1] / 2 for w in NORTH]))
for i in range(len(xs) - 1):
    xa, xb = xs[i], xs[i + 1]; mid = (xa + xb) / 2
    cov = [w for w in NORTH if w[0] - w[1] / 2 < mid < w[0] + w[1] / 2]
    y = 0.0
    for w in sorted(cov, key=lambda w: w[2]):
        box((mid, (y + w[2]) / 2, 28.0), (xb - xa, w[2] - y, .9), "wall"); y = w[3]
    box((mid, (y + 9.2) / 2, 28.0), (xb - xa, 9.2 - y, .9), "wall")
def wid(art, x): return "lancet_N%+.2f" % x if art != "RoseWindow" else "rose"
for (x, wd, sill, top, art) in NORTH:                         # what each opening looks out on (night sky), one class per opening
    box((x, (sill + top) / 2, 29.25 if FF else 28.7), (wd + .6, top - sill + .6, .1), "sky_" + wid(art, x))
for sx in (-1, 1): box((sx * 4.35, 3.1, 21), (2.7, .2, 13.4), "gallery")
box((0, 3.1, 26.85), (6.0, .2, 1.7), "gallery")
box((0, 5.3, 27.35), (4.0, .3, .9), "ledge")
box((0, 8.7, 21), (12, .5, 14), "roof")
box((0, .1, 26.525), (5.0, .2, 2.05), "dais"); box((0, .3, 26.925), (3.2, .2, 1.25), "dais")
# chandelier: ring 2.4 m across (scale 1.4) at hub (0, 3.9, 22.9), candles, chain to the vault
bpy.ops.mesh.primitive_torus_add(major_radius=1.2, minor_radius=.05, location=B((0, 3.9, 22.9))); o = bpy.context.active_object; CLASS[o.name] = "chandelier"; placed.append(o)
for k in range(7):
    a = 2 * math.pi * k / 7; box((1.15 * math.cos(a), 4.15, 22.9 + 1.15 * math.sin(a)), (.06, .4, .06), "chandelier")
box((0, 6.3, 22.9), (.05, 4.7, .05), "chandelier")
for p in HALL_BOOKS:
    if not near_line(p): box(p, (.8, .2, .6), "book")
# ---- kit
for sx in (-1, 1):
    for z in (15.6, 18.6, 21.6, 24.4): place("HallColumn", (sx * 3.9, 0, z), cls="column_%s%.1f" % ("W" if sx < 0 else "E", z))
    if ARCADE:
        for zc in (17.1, 20.1, 23.0): place("PointedArch", (sx * 3.9, 0, zc), 90, (1, 1, .93 if zc == 23.0 else 1.0), cls="arch")
    place("HallCorbel", (sx * 3.9, 5.30, 14.425), 0, cls="arch"); place("HallCorbel", (sx * 3.9, 5.30, 27.425), 180, cls="arch")
    n = 6
    for i in range(n): place("Balustrade", (sx * 3.0, 3.2, 14.3 + 11.7 * (i + .5) / n), 90, cls="balustrade")
for i in range(3): place("Balustrade", (-3.0 + 6.0 * (i + .5) / 3, 3.2, 26.0), 0, cls="balustrade")
place("HallVault", (0, 0, 0), cls="vault")
for (x, wd, sill, top, art) in NORTH:
    place(art, (x, sill, 28.0), 180, cls=wid(art, x))
for (x, zc, wd, sill, top) in SIDE:
    yaw = 90 if x < 0 else -90
    place("GalleryBlindLancet", (x, sill, zc), yaw, cls="lancet_S%+.1f" % x)                                    # tracery half sunk in the wall
    box((x + (.012 if x < 0 else -.012), (sill + top) / 2, zc), (.01, top - sill, wd - .1), "lancet_S%+.1f" % x)  # the moonlit glass panel
place("LongTable", (0, 0, 23.7), 90, cls="table")
for z in (22.9, 24.5):
    place("HighBackChair", (1.15, 0, z), -90, cls="table"); place("HighBackChair", (-1.15, 0, z), 90, cls="table")
place("Throne", (0, .4, 27.05), 180, cls="throne")
for (x, y, z, yaw) in ((-5.51, 3.5, 19.5, 90), (-5.51, 3.5, 23.3, 90), (5.51, 3.5, 19.5, -90), (5.51, 3.5, 23.3, -90), (-2.4, .28, 27.36, 180), (2.4, .28, 27.36, 180)):
    place("TapestryPanel", (x, y, z), yaw, cls="banner")
sk = place("GiantSkeleton", (0, .05, 18.72), 0, (.6, .5, .45), cls="skeleton")
for o in sk:
    if o.data.name.startswith("Shroud") or o.name.startswith("Shroud"): CLASS[o.name] = "shroud"
seed = 100
for p in HALL_CANDELABRAS:
    if near_line(p): continue
    place("Candelabra", p, (seed * 57) % 360, (1.6, 1.6, 1.6), cls="candelabra"); seed += 1
for p in LEDGE:
    place("Candelabra", p, (seed * 57) % 360, (1.6, 1.6, 1.6), cls="candelabra"); seed += 1
for (x, y, z, s) in ((-3.4, 0, 20.1, 1.0), (3.4, 0, 20.1, 1.0), (-3.4, 0, 16.9, 1.0), (3.4, 0, 16.9, 1.0)):   # floor candle stands (sheen sources)
    place("Candelabra", (x, y, z), 0, (s, s, s), cls="candelabra")
bpy.context.view_layer.update()

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

w, h = int(W * SCALE), int(H * SCALE)
e = np.array(eye, float); f = np.array(tgt, float) - e; f /= np.linalg.norm(f)
r = np.cross([0, 1, 0], f); r /= np.linalg.norm(r); u = np.cross(f, r)
th = math.tan(math.radians(FOV) / 2); asp = W / H
cid = np.full((h, w), -1, np.int16); depth = np.zeros((h, w), np.float32); hit = np.zeros((h, w, 3), np.float32)
o = B(eye)
for j in range(h):
    for i in range(w):
        d = f + ((i + .5) / w * 2 - 1) * asp * th * r + (1 - (j + .5) / h * 2) * th * u
        loc, n_, fi, dd = tree.ray_cast(o, Vector((-d[0], -d[2], d[1])).normalized(), 80.0)
        if loc is not None: cid[j, i] = cls_of[fi]; depth[j, i] = dd; hit[j, i] = (-loc.x, loc.z, -loc.y)
name = "S1_%s" % DESIGN
rng = np.random.default_rng(3)
pal = {n: rng.integers(40, 255, 3) for n in names}
fixed = {"table": (230, 60, 40), "skeleton": (220, 210, 170), "shroud": (140, 20, 30), "floor": (50, 50, 60), "wall": (80, 80, 90), "vault": (110, 110, 120),
         "gallery": (90, 70, 50), "balustrade": (150, 150, 120), "dais": (60, 60, 70), "throne": (200, 120, 200), "banner": (160, 40, 60), "roof": (30, 30, 30),
         "rose": (120, 160, 255), "candelabra": (200, 160, 60), "arch": (40, 120, 60), "chandelier": (90, 90, 40), "book": (120, 70, 30), "ledge": (70, 70, 80)}
for n in names:
    if n in fixed: pal[n] = np.array(fixed[n])
    elif n.startswith("lancet"): pal[n] = np.array((250, 230, 90))
    elif n.startswith("column"): pal[n] = np.array((60, 200, 90))
img = np.zeros((h, w, 3), np.uint8)
for c, n in enumerate(names): img[cid == c] = pal[n]
import struct, zlib
def png(path, a):
    """Minimal PNG writer (Blender's python has no Pillow): RGB uint8 or bool/grey."""
    if a.ndim == 2: a = np.repeat(((a > 0) * 255).astype(np.uint8)[..., None], 3, 2) if a.dtype == bool else np.repeat(a[..., None], 3, 2)
    raw = b"".join(b"\x00" + a[y].tobytes() for y in range(a.shape[0]))
    def chunk(t, d): return struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d) & 0xffffffff)
    Path(path).write_bytes(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", a.shape[1], a.shape[0], 8, 2, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(raw, 6)) + chunk(b"IEND", b""))
png(OUT / (name + ".png"), img)
np.savez_compressed(OUT / (name + "_class.npz"), cid=cid, depth=depth, hit=hit, names=np.array(names), eye=e, fwd=f)

k = (W / w) * (H / h)
m = {"design": DESIGN, "eye": list(eye), "target": list(tgt)}
for c, n in enumerate(names):
    cnt = int((cid == c).sum())
    if cnt: m["px_" + n] = int(cnt * k)
lanc = {}
for c, n in enumerate(names):
    if n.startswith("lancet"):
        sel = (cid == c) | (cid == idx.get("sky_" + n, -9))           # the tracery plus the sky seen through its opening
        rows = np.where(sel.sum(1) >= 2)[0]
        lanc[n] = dict(visible_rows=int(len(rows) * H / h), top=int(rows.min() * H / h) if len(rows) else -1, bottom=int(rows.max() * H / h) if len(rows) else -1,
                       px=int(sel.sum() * k))
m["lancets"] = lanc
piers = {}
# a pier as the eye reads it: the column plus the arcade arch blocks that spring from it (within 1.6 m along z), below the gallery soffit (y < 3.0)
arch_c = idx.get("arch", -2)
for c, n in enumerate(names):
    if not n.startswith("column_"): continue
    x = -3.9 if n[7] == "W" else 3.9; z = float(n[8:])
    sel = ((cid == c) | ((cid == arch_c) & (np.abs(hit[..., 2] - z) < 1.6) & (np.sign(hit[..., 0]) == np.sign(x)))) & (hit[..., 1] < 3.0)
    ys, xs_ = np.where(sel)
    if len(ys) * k < 4000: continue
    hh, ww = (ys.max() - ys.min() + 1) * H / h, (xs_.max() - xs_.min() + 1) * W / w
    piers[n[7:]] = dict(x=x, z=z, depth=round(z - eye[2], 2), h=int(hh), w=int(ww), aspect=round(hh / ww, 2), px=int(len(ys) * k),
                        bbox=[int(xs_.min() * W / w), int(ys.min() * H / h), int(xs_.max() * W / w), int(ys.max() * H / h)])
m["piers"] = piers
def proj(p):
    v = np.array(p) - e; zz = v @ f
    return ((v @ r) / zz / (asp * th) * .5 + .5) * W, (.5 - (v @ u) / zz / th * .5) * H
m["rose_x"], m["rose_y"] = [round(v, 1) for v in proj((0, 7.1, 27.5))]
hor = e + f * 50; hor[1] = e[1]; m["horizon_y"] = round(proj(hor)[1], 1)
(OUT / ("%s_metrics.json" % name)).write_text(json.dumps(m, indent=1))
print("HC_FF_HALL", name, json.dumps({k_: v for k_, v in m.items() if k_ not in ("lancets", "piers")}), flush=True)
print("HC_FF_HALL lancets", json.dumps(lanc), flush=True)
print("HC_FF_HALL piers", json.dumps(piers), flush=True)

if "--masks" in ARGV:
    MD = cc.ROOT / "tools" / "ff_art_masks"; MD.mkdir(exist_ok=True)
    assert (w, h) == (W, H), "--masks needs --scale 1.0"
    def save(mask, fn): png(MD / fn, mask.astype(bool))
    meta = {"eye": list(eye), "fwd": [round(float(x), 5) for x in f], "fov": FOV, "size": [W, H], "design": DESIGN,
            "source": "tools/ff_hall_raycast.py --design %s (BVH proxy of the hall kit FBX + CastleLayout data)" % DESIGN,
            "rose_x": m["rose_x"], "horizon_y": m["horizon_y"], "lancets": {}, "piers": piers}
    for n, info in lanc.items():
        fn = "S1_%s.png" % n.replace("+", "p").replace("-", "m").replace(".", "_"); save((cid == idx[n]) | (cid == idx.get("sky_" + n, -9)), fn)
        meta["lancets"][n] = dict(info, mask=fn)
    tab = cid == idx["table"]; save(tab, "S1_table.png"); meta["table_mask"] = "S1_table.png"; meta["table_proxy_px"] = int(tab.sum() * k)
    fl = (cid == idx["floor"]) | (cid == idx["dais"]); save(fl, "S1_floor.png"); meta["floor_mask"] = "S1_floor.png"
    top = tab & (np.abs(hit[..., 1] - .775) < .03) & (np.abs(hit[..., 0]) <= .65) & (np.abs(hit[..., 2] - 23.7) <= 1.65)
    save(fl | top, "S1_mirror.png"); meta["mirror_mask"] = "S1_mirror.png"     # floor, dais and the polished table top (CastleWorld.FloorSheen)
    (MD / "hall_masks.json").write_text(json.dumps({"S1_great_hall": meta}, indent=1))
    print("HC_FF_HALL masks written", MD)
