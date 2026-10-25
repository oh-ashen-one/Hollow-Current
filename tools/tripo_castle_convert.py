"""Tripo GLB -> castle prop source for Editor/TripoImport (headless Blender, CPU only).

usage: Blender -b -P tools/tripo_castle_convert.py -- --glb in.glb --name Candle --bounds x0,x1,y0,y1,z0,z1 [--fit uniform|stretch]
                 [--yaw 0] [--tris 2000] [--tex 1024] [--preview out.png]

Writes Assets/HollowCurrent/Art/Tripo/Castle/<name>/: import.json (output "Castle"), mesh-0-0.bytes (HCM1), material-0-color.png,
material-0-normal.png, material-0-metal.png (R = metallic, A = smoothness, Unity Standard). TripoImport turns that into
Resources/Castle/<name>.prefab, which CastleArt.Place and SurrealProps load by name.

Axes: Blender (x, y, z) -> Unity (-x, z, -y), the same mapping Unity's FBX import gives the old castle kit, with the triangle winding
reversed (handedness), so the model is not mirrored and Blender -Y (glTF +Z, Tripo's front) lands on Unity +Z, the CastleLayout contract
front. --yaw turns the model about the vertical first (degrees, Unity convention: positive turns +Z toward +X).
--bounds are the target Unity model-space bounds (the old asset's, from CastleKitAxis or measured); uniform fit keeps proportions,
fits inside the box, centres it on x/z and sits it on y0; stretch fills the box exactly.
"""
import bpy, bmesh, sys, json, struct, math, argparse, hashlib
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
ap = argparse.ArgumentParser()
ap.add_argument('--glb', required=True); ap.add_argument('--name', required=True)
ap.add_argument('--bounds', required=True); ap.add_argument('--fit', default='uniform', choices=['uniform', 'stretch', 'height'])
ap.add_argument('--yaw', type=float, default=0.0); ap.add_argument('--tris', type=int, default=4000)
ap.add_argument('--tex', type=int, default=1024); ap.add_argument('--preview', default='')
ap.add_argument('--out', default='')
ap.add_argument('--anchors', type=int, default=0, help='candle tips to find (flame anchors): the N highest points at least --anchor-radius apart in xz')
ap.add_argument('--anchor-radius', type=float, default=.05)
ap.add_argument('--pivot', default='box', choices=['box', 'base'], help='base: centre x/z on the bottom 4 percent of the model (a tree trunk)')
ap.add_argument('--top-at', type=float, default=0, help='tables: scale y so the largest upward-facing level (the top) sits at this height')
a = ap.parse_args(argv)
x0, x1, y0, y1, z0, z1 = [float(v) for v in a.bounds.split(',')]
OUT = Path(a.out) if a.out else ROOT / 'Assets/HollowCurrent/Art/Tripo/Castle' / a.name
OUT.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=a.glb)
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
assert meshes, 'no mesh in ' + a.glb
for o in bpy.context.scene.objects: o.select_set(o in meshes)
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1: bpy.ops.object.join()
obj = bpy.context.view_layer.objects.active
for o in list(bpy.context.scene.objects):
    if o is not obj and o.type != 'MESH': bpy.data.objects.remove(o)
obj.parent = None
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
src_tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)

# material textures (Tripo: one Principled BSDF with base colour, normal and glTF metallicRoughness)
mat = obj.active_material
def linked_image(sock):
    if not sock or not sock.is_linked: return None
    n = sock.links[0].from_node
    for _ in range(4):
        if n.type == 'TEX_IMAGE': return n.image
        ins = [i for i in n.inputs if i.is_linked]
        if not ins: return None
        n = ins[0].links[0].from_node
    return None
bsdf = next((n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None) if mat and mat.use_nodes else None
img_color = linked_image(bsdf.inputs['Base Color']) if bsdf else None
img_normal = linked_image(bsdf.inputs['Normal']) if bsdf else None
img_mr = linked_image(bsdf.inputs['Metallic']) if bsdf else None
if img_mr is None and bsdf: img_mr = linked_image(bsdf.inputs['Roughness'])
assert img_color is not None, 'no base colour texture'

# Tripo GLBs come split per face corner: weld by position first (UVs live on loops, so seams survive) or collapse shatters the mesh
bm = bmesh.new(); bm.from_mesh(obj.data)
bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=max(obj.dimensions) * 2e-5)
bm.to_mesh(obj.data); bm.free()
# decimate to the triangle budget (UVs are preserved by collapse)
if src_tris > a.tris:
    m = obj.modifiers.new('dec', 'DECIMATE'); m.decimate_type = 'COLLAPSE'; m.ratio = max(1e-4, a.tris / src_tris); m.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=m.name)
bm = bmesh.new(); bm.from_mesh(obj.data)
bmesh.ops.triangulate(bm, faces=bm.faces[:]); bm.to_mesh(obj.data); bm.free()
me = obj.data
me.calc_loop_triangles()
uv = me.uv_layers.active.data if me.uv_layers.active else None
assert uv is not None, 'no UVs'

# Unity-space corners: (-x, z, -y), turned by --yaw about +Y
co = np.array([v.co[:] for v in me.vertices], dtype=np.float64)
U = np.stack([-co[:, 0], co[:, 2], -co[:, 1]], 1)
yaw = math.radians(a.yaw); c, s = math.cos(yaw), math.sin(yaw)
U = np.stack([U[:, 0] * c + U[:, 2] * s, U[:, 1], -U[:, 0] * s + U[:, 2] * c], 1)
lo, hi = U.min(0), U.max(0); size = np.maximum(hi - lo, 1e-6)
tgt = np.array([x1 - x0, y1 - y0, z1 - z0])
if a.fit == 'stretch': k = tgt / size
elif a.fit == 'height': k = np.full(3, tgt[1] / size[1])
else: k = np.full(3, float(np.min(tgt / size)))
U = (U - lo) * k
U[:, 0] += x0 + (tgt[0] - size[0] * k[0]) * .5
U[:, 1] += y0
U[:, 2] += z0 + (tgt[2] - size[2] * k[2]) * .5
if a.top_at > 0:
    tri = np.array([t.vertices[:] for t in me.loop_triangles])
    pa, pb, pc = U[tri[:, 0]], U[tri[:, 1]], U[tri[:, 2]]
    n = np.cross(pb - pa, pc - pa); area = np.linalg.norm(n, axis=1) * .5
    up = np.abs(n[:, 1]) / np.maximum(np.linalg.norm(n, axis=1), 1e-12) > .92
    ys = (pa[:, 1] + pb[:, 1] + pc[:, 1]) / 3
    hi_y = U[:, 1].max(); bins = np.linspace(y0, hi_y, 120)
    hist, _ = np.histogram(ys[up & (ys > y0 + (hi_y - y0) * .25)], bins=bins, weights=area[up & (ys > y0 + (hi_y - y0) * .25)])
    top = (bins[np.argmax(hist)] + bins[np.argmax(hist) + 1]) * .5
    U[:, 1] = y0 + (U[:, 1] - y0) * ((a.top_at - y0) / max(top - y0, 1e-6))
    k = k * np.array([1, (a.top_at - y0) / max(top - y0, 1e-6), 1])
    print('HC_TRIPO_TOP found %.3f -> %.3f' % (top, a.top_at), flush=True)
if a.pivot == 'base':
    foot = U[U[:, 1] <= y0 + (U[:, 1].max() - y0) * .04]
    cx, cz = foot[:, 0].mean(), foot[:, 2].mean()
    U[:, 0] += (x0 + x1) * .5 - cx; U[:, 2] += (z0 + z1) * .5 - cz
anchors = []
if a.anchors > 0:
    order = np.argsort(-U[:, 1])
    for i in order:
        p = U[i]
        if all((p[0] - q[0]) ** 2 + (p[2] - q[2]) ** 2 >= a.anchor_radius ** 2 for q in anchors): anchors.append(p.copy())
        if len(anchors) == a.anchors: break

# split normals per corner, same mapping (non-uniform k: inverse-transpose)
verts = []; idx = []
inv = 1.0 / k
for tri in me.loop_triangles:
    corner = []
    for li in tri.loops:
        lp = me.loops[li]; vi = lp.vertex_index
        n = me.corner_normals[li].vector if hasattr(me, 'corner_normals') else lp.normal
        nu = np.array([-n[0], n[2], -n[1]])
        nu = np.array([nu[0] * c + nu[2] * s, nu[1], -nu[0] * s + nu[2] * c]) * inv
        nu /= max(np.linalg.norm(nu), 1e-9)
        u = uv[li].uv
        corner.append((*U[vi], *nu, u[0], u[1]))
    base = len(verts); verts.extend(corner)
    idx.extend([base, base + 2, base + 1])     # reversed winding (handedness)
# weld identical corners
key = {}; vout = []; remap = []
for v in verts:
    kk = tuple(round(x, 5) for x in v)
    if kk not in key: key[kk] = len(vout); vout.append(v)
    remap.append(key[kk])
iout = [remap[i] for i in idx]
with (OUT / 'mesh-0-0.bytes').open('wb') as f:
    f.write(struct.pack('<Iii', 0x48434d31, len(vout), len(iout)))
    for v in vout: f.write(struct.pack('<8f', *v))
    f.write(struct.pack('<%di' % len(iout), *iout))

# textures
def save_scaled(img, path, fn=None):
    w, h = img.size
    px = np.empty(w * h * 4, dtype=np.float32); img.pixels.foreach_get(px); px = px.reshape(h, w, 4)
    if fn: px = fn(px)
    out = bpy.data.images.new(path.stem, w, h, alpha=True, float_buffer=False)
    out.colorspace_settings.name = img.colorspace_settings.name if fn is None else 'Non-Color'
    out.pixels.foreach_set(px.ravel())
    if w > a.tex: out.scale(a.tex, a.tex)
    out.filepath_raw = str(path); out.file_format = 'PNG'; out.save()
save_scaled(img_color, OUT / 'material-0-color.png')
has_normal = img_normal is not None
if has_normal:
    img_normal.colorspace_settings.name = 'Non-Color'
    save_scaled(img_normal, OUT / 'material-0-normal.png', lambda p: p)
has_metal = img_mr is not None
if has_metal:
    img_mr.colorspace_settings.name = 'Non-Color'
    def mr(p):
        o = np.empty_like(p); metal = p[..., 2]; rough = p[..., 1]
        o[..., 0] = metal; o[..., 1] = metal; o[..., 2] = metal; o[..., 3] = 1.0 - rough
        return o
    save_scaled(img_mr, OUT / 'material-0-metal.png', mr)

manifest = {
    'name': a.name, 'output': 'Castle', 'source': 'Tripo Studio H3.1 (owner web account), ' + Path(a.glb).name,
    'sha256': hashlib.sha256(Path(a.glb).read_bytes()).hexdigest(),
    'materials': [{'stem': 'material-0', 'colorFactor': [1, 1, 1, 1], 'hasNormal': has_normal, 'hasMetal': has_metal}],
    'primitives': [{'file': 'mesh-0-0.bytes', 'material': 0, 'vertices': len(vout), 'triangles': len(iout) // 3}],
    'fit': {'bounds': [x0, x1, y0, y1, z0, z1], 'mode': a.fit, 'yaw': a.yaw, 'sourceTriangles': src_tris},
}
if anchors: manifest['anchors'] = [round(float(c), 4) for p in anchors for c in p]
(OUT / 'import.json').write_text(json.dumps(manifest, indent=2) + '\n')
mn = np.array(vout)[:, :3].min(0); mx = np.array(vout)[:, :3].max(0)
print('HC_TRIPO_CONVERT %s tris %d -> %d verts %d bounds %s..%s tex %d normal=%s metal=%s' % (
    a.name, src_tris, len(iout) // 3, len(vout), np.round(mn, 3).tolist(), np.round(mx, 3).tolist(), a.tex, has_normal, has_metal), flush=True)

# preview: three CPU Cycles views of the decimated, textured mesh (front, three-quarter, side), stitched side by side
if a.preview:
    scn = bpy.context.scene
    scn.render.engine = 'CYCLES'; scn.cycles.device = 'CPU'; scn.cycles.samples = 24; scn.cycles.use_denoising = False
    scn.render.resolution_x = 420; scn.render.resolution_y = 420; scn.render.film_transparent = False
    world = bpy.data.worlds.new('w'); world.use_nodes = True; scn.world = world
    world.node_tree.nodes['Background'].inputs[0].default_value = (.35, .35, .37, 1); world.node_tree.nodes['Background'].inputs[1].default_value = 1.2
    dims = obj.dimensions; r = max(dims) * 1.9; ctr = sum((obj.matrix_world @ v.co for v in me.vertices), obj.location * 0) / len(me.vertices)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); scn.collection.objects.link(cam); scn.camera = cam
    sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); scn.collection.objects.link(sun); sun.data.energy = 3
    sun.rotation_euler = (math.radians(50), 0, math.radians(30))
    shots = []
    for i, ang in enumerate([0, 40, 90]):
        t = math.radians(ang - 90 + a.yaw)
        cam.location = (ctr.x + r * math.cos(t), ctr.y + r * math.sin(t), ctr.z + r * .35)
        d = ctr - cam.location; cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
        p = str(OUT.parent / ('_preview_%s_%d.png' % (a.name, i))); scn.render.filepath = p
        bpy.ops.render.render(write_still=True); shots.append(p)
    ims = [bpy.data.images.load(p) for p in shots]
    W = sum(im.size[0] for im in ims); H = ims[0].size[1]
    canvas = np.zeros((H, W, 4), np.float32); x = 0
    for im in ims:
        w, h = im.size; buf = np.empty(w * h * 4, np.float32); im.pixels.foreach_get(buf); canvas[:, x:x + w] = buf.reshape(h, w, 4); x += w
    out = bpy.data.images.new('pv', W, H); out.pixels.foreach_set(canvas.ravel()); out.filepath_raw = a.preview; out.file_format = 'PNG'; out.save()
    for p in shots: Path(p).unlink(missing_ok=True)
    print('HC_TRIPO_PREVIEW ' + a.preview, flush=True)
