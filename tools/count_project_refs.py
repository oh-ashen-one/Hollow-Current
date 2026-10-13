"""Bake a base-colour texture for the Count Claude demon mesh by projecting the owner-approved reference views
onto it. CPU-only headless Blender, no network.

    /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/count_project_refs.py -- \
        --mesh tools/count_source/CountClaude_demon_rigged.glb --refs <dir with demon_front.png demon_side.png demon_back.png> \
        --out tools/count_source/CountClaude_demon_albedo.png [--size 2048] [--yaw -90]

Why: the CustomUse Tripo v3.1 multiview job returned geometry without a texture. The reference PNGs are not in git
(docs/haunted-castle/refs/*.png is ignored), so the baked texture is committed as a source file and count_prepare.py
only reads it.

Method: the mesh is turned to face -Y (--yaw degrees about Z) with its feet on z=0. Four orthographic views (front
from -Y, back from +Y, his left side from +X using demon_side.png, his right side from -X using the mirrored side
image) are fitted to each reference's silhouette (uniform scale from the heights, centred on the torso column).
Every texel of the mesh's UV map is rasterised; its surface point is z-tested against each view's depth buffer (so
an arm does not paint the torso behind it) and coloured by a normal-weighted blend of the views that see it. Texels
no view sees are filled from neighbours, then the UV gutters are dilated.
"""
from pathlib import Path
import bpy, sys, math
import numpy as np
from mathutils import Matrix

argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
def arg(name, default=None): return argv[argv.index(name) + 1] if name in argv else default
MESH = Path(arg('--mesh')); REFS = Path(arg('--refs')); OUT = Path(arg('--out'))
SIZE = int(arg('--size', 2048)); YAW = float(arg("--yaw", -90)); DEPTH_RES = 1024

def load(path):
    im = bpy.data.images.load(str(path)); w, h = im.size
    px = np.empty(w * h * 4, dtype=np.float32); im.pixels.foreach_get(px)
    return px.reshape(h, w, 4)[::-1, :, :3].copy()          # row 0 = top
def silhouette(img):
    bg = np.median(np.concatenate([img[:, :8].reshape(-1, 3), img[:, -8:].reshape(-1, 3)]), axis=0)
    mask = np.abs(img - bg).sum(2) > .10
    rows = np.where(mask.any(1))[0]; top, bottom = rows.min(), rows.max()
    band = mask[int(top + .25 * (bottom - top)):int(top + .55 * (bottom - top))]   # chest/hips rows: body column
    cols = np.where(band.any(0))[0]; mid = np.median(np.where(band)[1]) if band.any() else (cols.min() + cols.max()) / 2
    return top, bottom, mid

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete()
bpy.ops.import_scene.gltf(filepath=str(MESH))
body = max((o for o in bpy.context.scene.objects if o.type == 'MESH'), key=lambda o: len(o.data.vertices))
me = body.data
M = Matrix.Rotation(math.radians(YAW), 4, 'Z') @ body.matrix_world
co = np.array([(M @ v.co)[:] for v in me.vertices]); co[:, 2] -= co[:, 2].min()
tris = []; uvs = []
uv = me.uv_layers.active.data
for p in me.polygons:
    for k in range(1, len(p.loop_indices) - 1):
        li = (p.loop_indices[0], p.loop_indices[k], p.loop_indices[k + 1])
        tris.append([me.loops[i].vertex_index for i in li]); uvs.append([uv[i].uv[:] for i in li])
tris = np.array(tris); uvs = np.array(uvs)
P = co[tris]                                                # (T,3,3)
fn = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0]); fn /= np.linalg.norm(fn, axis=1, keepdims=True) + 1e-12
H = co[:, 2].max()

side = load(REFS / 'demon_side.png')
views = []  # (name, image, screen-x axis, depth axis (towards camera), image)
for name, img, right, toward in (('front', load(REFS / 'demon_front.png'), np.array([1, 0, 0]), np.array([0, -1, 0])),
                                 ('back', load(REFS / 'demon_back.png'), np.array([-1, 0, 0]), np.array([0, 1, 0])),
                                 ('left', side, np.array([0, 1, 0]), np.array([1, 0, 0])),
                                 ('right', side[:, ::-1].copy(), np.array([0, -1, 0]), np.array([-1, 0, 0]))):
    top, bottom, mid = silhouette(img)
    sx = co @ right
    band = (co[:, 2] > .45 * H) & (co[:, 2] < .75 * H)
    centre = np.median(sx[band])
    scale = (bottom - top) / H                              # reference pixels per metre
    def to_img(pts, right=right, centre=centre, scale=scale, top=top, mid=mid):
        return np.stack([mid + (pts @ right - centre) * scale, top + (H - pts[:, 2]) * scale], 1)
    # depth buffer (larger value = nearer the camera)
    hgt, wid = img.shape[:2]; dscale = DEPTH_RES / max(hgt, wid)
    zbuf = np.full((int(hgt * dscale) + 2, int(wid * dscale) + 2), -1e9, dtype=np.float32)
    flat = P.reshape(-1, 3); xy = to_img(flat) * dscale; z = flat @ toward
    xy = xy.reshape(-1, 3, 2); z = z.reshape(-1, 3)
    for t in range(len(tris)):
        a, b, c = xy[t]; x0, y0 = np.floor(np.minimum(np.minimum(a, b), c)).astype(int); x1, y1 = np.ceil(np.maximum(np.maximum(a, b), c)).astype(int)
        x0 = max(x0, 0); y0 = max(y0, 0); x1 = min(x1, zbuf.shape[1] - 1); y1 = min(y1, zbuf.shape[0] - 1)
        if x1 < x0 or y1 < y0: continue
        gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + .5, np.arange(y0, y1 + 1) + .5)
        d = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
        if abs(d) < 1e-9: continue
        l1 = ((b[1] - c[1]) * (gx - c[0]) + (c[0] - b[0]) * (gy - c[1])) / d; l2 = ((c[1] - a[1]) * (gx - c[0]) + (a[0] - c[0]) * (gy - c[1])) / d; l3 = 1 - l1 - l2
        ins = (l1 >= -.01) & (l2 >= -.01) & (l3 >= -.01)
        if not ins.any(): continue
        zz = l1 * z[t, 0] + l2 * z[t, 1] + l3 * z[t, 2]
        sub = zbuf[y0:y1 + 1, x0:x1 + 1]; np.maximum(sub, np.where(ins, zz, -1e9), out=sub)
    views.append((name, img, toward, to_img, zbuf, dscale))
    print('HC_PROJECT view', name, 'ref silhouette rows', top, bottom, 'mid', round(float(mid), 1), 'px/m', round(float(scale), 1), flush=True)

# rasterise UV space
acc = np.zeros((SIZE, SIZE, 3), np.float32); wsum = np.zeros((SIZE, SIZE), np.float32); covered = np.zeros((SIZE, SIZE), bool)
UVp = uvs * SIZE
for t in range(len(tris)):
    a, b, c = UVp[t]
    x0, y0 = np.floor(np.minimum(np.minimum(a, b), c)).astype(int); x1, y1 = np.ceil(np.maximum(np.maximum(a, b), c)).astype(int)
    x0 = max(x0, 0); y0 = max(y0, 0); x1 = min(x1, SIZE - 1); y1 = min(y1, SIZE - 1)
    if x1 < x0 or y1 < y0: continue
    gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + .5, np.arange(y0, y1 + 1) + .5)
    d = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
    if abs(d) < 1e-12: continue
    l1 = ((b[1] - c[1]) * (gx - c[0]) + (c[0] - b[0]) * (gy - c[1])) / d; l2 = ((c[1] - a[1]) * (gx - c[0]) + (a[0] - c[0]) * (gy - c[1])) / d; l3 = 1 - l1 - l2
    ins = (l1 >= -.02) & (l2 >= -.02) & (l3 >= -.02)
    if not ins.any(): continue
    pts = (l1[ins, None] * P[t, 0] + l2[ins, None] * P[t, 1] + l3[ins, None] * P[t, 2])
    yy = gy[ins].astype(int); xx = gx[ins].astype(int); covered[yy, xx] = True
    for name, img, toward, to_img, zbuf, dscale in views:
        facing = float(fn[t] @ toward)
        w = max(facing, 0.0) ** 2 + .02 * (facing > -.2)    # thin membranes seen edge-on still get a little colour
        if w <= 0: continue
        ij = to_img(pts); dz = zbuf[np.clip((ij[:, 1] * dscale).astype(int), 0, zbuf.shape[0] - 1), np.clip((ij[:, 0] * dscale).astype(int), 0, zbuf.shape[1] - 1)]
        vis = (pts @ toward) >= dz - .03
        if not vis.any(): continue
        ix = np.clip(ij[:, 0].astype(int), 0, img.shape[1] - 1); iy = np.clip(ij[:, 1].astype(int), 0, img.shape[0] - 1)
        col = img[iy, ix]
        acc[yy[vis], xx[vis]] += col[vis] * w; wsum[yy[vis], xx[vis]] += w
seen = wsum > 0
tex = np.zeros_like(acc); tex[seen] = acc[seen] / wsum[seen, None]
mean = tex[seen].mean(0) if seen.any() else np.array([.4, .35, .33])
# fill covered-but-unseen texels and dilate gutters (iterative neighbour average)
filled = seen.copy()
for it in range(24):
    if filled.all(): break
    pad = np.pad(tex, ((1, 1), (1, 1), (0, 0))); pm = np.pad(filled, 1).astype(np.float32)
    s = sum(pad[1 + dy:SIZE + 1 + dy, 1 + dx:SIZE + 1 + dx] * pm[1 + dy:SIZE + 1 + dy, 1 + dx:SIZE + 1 + dx, None] for dy in (-1, 0, 1) for dx in (-1, 0, 1))
    n = sum(pm[1 + dy:SIZE + 1 + dy, 1 + dx:SIZE + 1 + dx] for dy in (-1, 0, 1) for dx in (-1, 0, 1))
    grow = (~filled) & (n > 0)
    tex[grow] = s[grow] / n[grow, None]; filled |= grow
tex[~filled] = mean
print('HC_PROJECT texels covered', int(covered.sum()), 'seen', int(seen.sum()), 'unseen-covered', int((covered & ~seen).sum()), flush=True)
out = bpy.data.images.new('albedo', SIZE, SIZE, alpha=False)
rgba = np.concatenate([tex, np.ones((SIZE, SIZE, 1), np.float32)], 2)   # row 0 = v 0 = bottom, as Blender stores pixels
out.pixels.foreach_set(rgba.ravel()); out.filepath_raw = str(OUT); out.file_format = 'PNG'; out.save()
print('HC_PROJECT_PASS', OUT, flush=True)
