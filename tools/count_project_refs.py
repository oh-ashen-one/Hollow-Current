"""Bake the Count Claude demon textures by projecting the owner-approved reference views onto the mesh.
CPU-only headless Blender, no network.

    /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/count_project_refs.py -- \
        --mesh tools/count_source/CountClaude_demon_rigged.glb --refs <dir with demon_front.png demon_side.png demon_back.png> \
        --out tools/count_source/CountClaude_demon_albedo.png [--size 2048] [--yaw -90]

Writes three PNGs next to --out: the albedo, `*_emission.png` (the ember eyes) and `*_flesh.png` (UV mask of the flayed
chest and forearms; count_prepare.py turns it into the Flesh material slot).

Why: the Customuse Tripo v3.1 multiview job returned geometry without a texture. The reference PNGs are not in git
(docs/haunted-castle/refs/*.png is ignored), so the baked textures are committed as source files.

Method: the mesh is turned to face -Y with its feet on z=0. Four orthographic views (front -Y, back +Y, his left side +X
from demon_side.png, his right side -X from the mirrored side image) are fitted to each reference's silhouette. Every
UV texel is rasterised, z-tested against each view's depth buffer and coloured by a bilinear, normal-weighted blend
(facing^4, views under 0.25 facing ignored, so hem strips and wing strands are not smeared by grazing views). Region
rules from the auto-rig's weights (round 8, critic r7 section 6):
  * head and torso: the front view dominates (x4) so the face and the flayed chest come from the sharpest view;
  * head: tones under 0.22 luma are crushed towards black (eye sockets, the split jaw's interior) and two small ember
    spots at the reference's eye positions go to the emission map;
  * flesh mask = red-dominant texels on the torso and forearms; everything outside it has its chroma clamped (x0.55),
    so the body reads grey-white with red patches as on the sheet instead of one red mass.
Unseen texels are filled from neighbours, then the UV gutters are dilated.
"""
from pathlib import Path
import bpy, sys, math
import numpy as np
from mathutils import Matrix

argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
def arg(name, default=None): return argv[argv.index(name) + 1] if name in argv else default
MESH = Path(arg('--mesh')); REFS = Path(arg('--refs')); OUT = Path(arg('--out'))
SIZE = int(arg('--size', 2048)); YAW = float(arg('--yaw', -90)); DEPTH_RES = 1024
EMBERS = ((451, 88), (496, 88))          # ember eye centres in demon_front.png pixels
EMBER_R = 6.0
MOUTH = (467, 100, 485, 172)             # dark split-jaw slit in demon_front.png (x0, y0, x1, y1)
HEAD_BONES = {'bone_4', 'bone_5', 'bone_6'}
TORSO_BONES = {'tripo::Spine_0', 'tripo::Spine_1', 'tripo::Spine_2', 'tripo::Spine_3', 'bone_27'}
FOREARM_BONES = {'tripo::Spine_5', 'bone_29', 'bone_46'}
WING_ROOTS = {'tripo::Head_0', 'bone_67'}
HEAD, TORSO, FOREARM, WING, OTHER = range(5)

def load(path):
    im = bpy.data.images.load(str(path)); w, h = im.size
    px = np.empty(w * h * 4, dtype=np.float32); im.pixels.foreach_get(px)
    return px.reshape(h, w, 4)[::-1, :, :3].copy()          # row 0 = top
def silhouette(img):
    bg = np.median(np.concatenate([img[:, :8].reshape(-1, 3), img[:, -8:].reshape(-1, 3)]), axis=0)
    mask = np.abs(img - bg).sum(2) > .10
    rows = np.where(mask.any(1))[0]; top, bottom = rows.min(), rows.max()
    band = mask[int(top + .25 * (bottom - top)):int(top + .55 * (bottom - top))]
    cols = np.where(band.any(0))[0]; mid = np.median(np.where(band)[1]) if band.any() else (cols.min() + cols.max()) / 2
    return top, bottom, mid
def bilinear(img, x, y):
    h, w = img.shape[:2]; x = np.clip(x - .5, 0, w - 1.001); y = np.clip(y - .5, 0, h - 1.001)
    x0 = x.astype(int); y0 = y.astype(int); fx = (x - x0)[:, None]; fy = (y - y0)[:, None]
    return (img[y0, x0] * (1 - fx) * (1 - fy) + img[y0, x0 + 1] * fx * (1 - fy) + img[y0 + 1, x0] * (1 - fx) * fy + img[y0 + 1, x0 + 1] * fx * fy)

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete()
bpy.ops.import_scene.gltf(filepath=str(MESH))
body = max((o for o in bpy.context.scene.objects if o.type == 'MESH'), key=lambda o: len(o.data.vertices))
rig = next((o for o in bpy.context.scene.objects if o.type == 'ARMATURE'), None)
me = body.data
M = Matrix.Rotation(math.radians(YAW), 4, 'Z') @ body.matrix_world
co = np.array([(M @ v.co)[:] for v in me.vertices]); co[:, 2] -= co[:, 2].min()
# region per vertex from its dominant auto-rig bone (walking up to a known ancestor)
def region_of(bone):
    b = bone
    while b is not None:
        if b.name in WING_ROOTS: return WING
        if b.name in HEAD_BONES: return HEAD
        if b.name in FOREARM_BONES: return FOREARM
        if b.name in TORSO_BONES: return TORSO
        b = b.parent
    return OTHER
bone_region = {b.name: region_of(b) for b in rig.data.bones} if rig else {}
gname = {g.index: g.name for g in body.vertex_groups}
vreg = np.full(len(me.vertices), OTHER)
for v in me.vertices:
    if v.groups: vreg[v.index] = bone_region.get(gname[max(v.groups, key=lambda g: g.weight).group], OTHER)
tris = []; uvs = []; treg = []
uv = me.uv_layers.active.data
for p in me.polygons:
    for k in range(1, len(p.loop_indices) - 1):
        li = (p.loop_indices[0], p.loop_indices[k], p.loop_indices[k + 1])
        vi = [me.loops[i].vertex_index for i in li]
        tris.append(vi); uvs.append([uv[i].uv[:] for i in li]); treg.append(np.bincount(vreg[vi], minlength=5).argmax())
tris = np.array(tris); uvs = np.array(uvs); treg = np.array(treg)
P = co[tris]
fn = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0]); fn /= np.linalg.norm(fn, axis=1, keepdims=True) + 1e-12
H = co[:, 2].max()

side = load(REFS / 'demon_side.png')
views = []
for name, img, right, toward in (('front', load(REFS / 'demon_front.png'), np.array([1, 0, 0]), np.array([0, -1, 0])),
                                 ('back', load(REFS / 'demon_back.png'), np.array([-1, 0, 0]), np.array([0, 1, 0])),
                                 ('left', side, np.array([0, 1, 0]), np.array([1, 0, 0])),
                                 ('right', side[:, ::-1].copy(), np.array([0, -1, 0]), np.array([-1, 0, 0]))):
    top, bottom, mid = silhouette(img)
    sx = co @ right
    band = (co[:, 2] > .45 * H) & (co[:, 2] < .75 * H)
    centre = np.median(sx[band]); scale = (bottom - top) / H
    def to_img(pts, right=right, centre=centre, scale=scale, top=top, mid=mid):
        return np.stack([mid + (pts @ right - centre) * scale, top + (H - pts[:, 2]) * scale], 1)
    hgt, wid = img.shape[:2]; dscale = DEPTH_RES / max(hgt, wid)
    zbuf = np.full((int(hgt * dscale) + 2, int(wid * dscale) + 2), -1e9, dtype=np.float32)
    flat = P.reshape(-1, 3); xy = (to_img(flat) * dscale).reshape(-1, 3, 2); z = (flat @ toward).reshape(-1, 3)
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
    print('HC_PROJECT view', name, 'ref silhouette rows', top, bottom, 'px/m', round(float(scale), 1), flush=True)

acc = np.zeros((SIZE, SIZE, 3), np.float32); wsum = np.zeros((SIZE, SIZE), np.float32)
emis = np.zeros((SIZE, SIZE), np.float32); regmap = np.full((SIZE, SIZE), -1, np.int8); headdark = np.zeros((SIZE, SIZE), np.float32)
UVp = uvs * SIZE
front_view = views[0]
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
    yy = gy[ins].astype(int); xx = gx[ins].astype(int); reg = treg[t]; regmap[yy, xx] = reg
    for vi, (name, img, toward, to_img, zbuf, dscale) in enumerate(views):
        facing = float(fn[t] @ toward)
        if facing < .25: continue
        w = facing ** 4 * (4.0 if vi == 0 and reg in (HEAD, TORSO) else 1.0)
        ij = to_img(pts); dz = zbuf[np.clip((ij[:, 1] * dscale).astype(int), 0, zbuf.shape[0] - 1), np.clip((ij[:, 0] * dscale).astype(int), 0, zbuf.shape[1] - 1)]
        vis = (pts @ toward) >= dz - .03
        if not vis.any(): continue
        col = bilinear(img, ij[:, 0], ij[:, 1])
        acc[yy[vis], xx[vis]] += col[vis] * w; wsum[yy[vis], xx[vis]] += w
        if vi == 0 and reg == HEAD:
            ex = ij[vis]
            for (cx, cy) in EMBERS:
                dd = np.hypot(ex[:, 0] - cx, ex[:, 1] - cy)
                e = np.clip(1 - dd / EMBER_R, 0, 1) ** 1.5
                np.maximum.at(emis, (yy[vis], xx[vis]), e)
            m0 = (ex[:, 0] > MOUTH[0]) & (ex[:, 0] < MOUTH[2]) & (ex[:, 1] > MOUTH[1]) & (ex[:, 1] < MOUTH[3])
            headdark[yy[vis][m0], xx[vis][m0]] = 1.0
seen = wsum > 0
tex = np.zeros_like(acc); tex[seen] = acc[seen] / wsum[seen, None]
mean = tex[seen].mean(0) if seen.any() else np.array([.4, .35, .33])
filled = seen.copy()
for it in range(32):
    if filled.all(): break
    pad = np.pad(tex, ((1, 1), (1, 1), (0, 0))); pm = np.pad(filled, 1).astype(np.float32)
    s = sum(pad[1 + dy:SIZE + 1 + dy, 1 + dx:SIZE + 1 + dx] * pm[1 + dy:SIZE + 1 + dy, 1 + dx:SIZE + 1 + dx, None] for dy in (-1, 0, 1) for dx in (-1, 0, 1))
    n = sum(pm[1 + dy:SIZE + 1 + dy, 1 + dx:SIZE + 1 + dx] for dy in (-1, 0, 1) for dx in (-1, 0, 1))
    grow = (~filled) & (n > 0)
    tex[grow] = s[grow] / n[grow, None]; filled |= grow
tex[~filled] = mean

# region rules
luma = tex @ np.array([.299, .587, .114], np.float32)
red_dom = (tex[..., 1] < .55 * tex[..., 0]) & (tex[..., 0] > .12)
flesh = red_dom & ((regmap == TORSO) | (regmap == FOREARM))
chroma = tex - luma[..., None]
outside = ~flesh & (regmap != HEAD)
tex[outside] = luma[outside, None] + chroma[outside] * .55
head = regmap == HEAD
crush = head & (luma < .22)
tex[crush] *= (np.clip(luma[crush] / .22, 0, 1) ** 1.6)[:, None]
slit = head & (headdark > 0) & (luma < .30); tex[slit] *= .25          # only the already-dark slit, not the jaw halves
ember = emis > .01
tex[ember] = tex[ember] * (1 - emis[ember, None]) + np.array([.35, .05, .02]) * emis[ember, None]
print('HC_PROJECT texels seen', int(seen.sum()), 'head', int(head.sum()), 'flesh', int(flesh.sum()), 'ember', int(ember.sum()),
      'red-dominant %.1f%%' % (100 * ((tex[..., 1] < .5 * tex[..., 0]) & (tex[..., 0] > .15))[regmap >= 0].mean()),
      'grey-white %.1f%%' % (100 * (((tex[..., 0] - tex[..., 2]) < .176) & (luma > .353))[regmap >= 0].mean()), flush=True)

def save(arr, path, rgb=True):
    a = arr if rgb else np.repeat(arr[..., None], 3, 2)
    img = bpy.data.images.new(path.stem, SIZE, SIZE, alpha=False)
    img.pixels.foreach_set(np.concatenate([np.clip(a, 0, 1), np.ones((SIZE, SIZE, 1), np.float32)], 2).ravel())
    img.filepath_raw = str(path); img.file_format = 'PNG'; img.save()
save(tex, OUT)
save(np.clip(emis, 0, 1)[..., None] * np.array([1.0, .32, .06], np.float32), OUT.with_name(OUT.stem + '_emission.png'))
save(flesh.astype(np.float32), OUT.with_name(OUT.stem + '_flesh.png'), rgb=False)
print('HC_PROJECT_PASS', OUT, flush=True)
