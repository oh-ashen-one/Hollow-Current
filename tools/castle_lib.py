"""Blender (bpy/bmesh) helpers shared by castle_architecture.py and castle_props.py.

Run inside Blender only:  Blender -b -P tools/castle_architecture.py
Asset space is Blender space: X right, Y depth (front of the asset = +Y), Z up, metres.
Exported with axis_forward=-Z, axis_up=Y so Blender Z becomes Unity Y and Blender Y becomes Unity Z.
Each asset is one or more mesh objects (parts) at the top level of its FBX (no empties);
a part's object origin is its pivot (door leaves hinge, candle flames base).
"""
import bpy, bmesh, math, json, sys
from pathlib import Path
from mathutils import Vector, Matrix
sys.path.insert(0, str(Path(__file__).resolve().parent))
import castle_common as cc

UV_TILE = {"Castle_Stone": 2.0, "Castle_BlackStone": 1.5, "Castle_Oak": 1.0, "Castle_Brass": 0.5,
           "Castle_Oxblood": 1.0, "Castle_Silver": 0.5, "Castle_Iron": 0.5, "Castle_Bone": 1.0}
MANIFEST = {}

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1.0

_MAT_CACHE = {}
def get_mat(name):
    if name in _MAT_CACHE and _MAT_CACHE[name].name in bpy.data.materials:
        return _MAT_CACHE[name]
    color, smooth, metal, _ = cc.MATERIALS[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Roughness"].default_value = 1.0 - smooth
    bsdf.inputs["Metallic"].default_value = metal
    if name == "Castle_Flame":
        for key in ("Emission Color", "Emission"):
            if key in bsdf.inputs:
                bsdf.inputs[key].default_value = (1.0, 0.62, 0.22, 1)
                break
        if "Emission Strength" in bsdf.inputs:
            bsdf.inputs["Emission Strength"].default_value = 3.0
    _MAT_CACHE[name] = m
    return m

def T(x, y, z): return Matrix.Translation((x, y, z))
def Rz(deg): return Matrix.Rotation(math.radians(deg), 4, 'Z')
def Rx(deg): return Matrix.Rotation(math.radians(deg), 4, 'X')
def Ry(deg): return Matrix.Rotation(math.radians(deg), 4, 'Y')
def about(c, M): return T(*c) @ M @ T(-c[0], -c[1], -c[2])

def arch_curve(hw, spring, R, n, offset=0.0):
    """Pointed-arch intrados/extrados, left -> right, jamb at x=+-hw springing at z=spring.
    Same centres for any offset, so offset>0 gives a constant-thickness band."""
    xl = -hw + R
    rr = R + offset
    th_a = math.acos(max(-1.0, min(1.0, -xl / rr)))
    left = []
    for i in range(n + 1):
        th = math.pi + (th_a - math.pi) * i / n
        left.append((xl + rr * math.cos(th), spring + rr * math.sin(th)))
    right = [(-x, z) for (x, z) in reversed(left[:-1])]
    return left + right

def arch_rise(hw, R):
    xl = -hw + R
    return R * math.sin(math.acos(-xl / R))

class Builder:
    def __init__(self, name):
        self.name = name
        self.parts = {}            # part -> dict(bm, origin)
        self.cur = None
        self.mats = []
        self.closed_faces = {}
        self.part(name, (0, 0, 0))
        self.notes = {}

    # ---- parts -------------------------------------------------------
    def part(self, pname, origin=(0, 0, 0)):
        if pname not in self.parts:
            bm = bmesh.new()
            self.parts[pname] = dict(bm=bm, origin=Vector(origin), uv=bm.loops.layers.uv.new("UVMap"),
                                     fix=bm.faces.layers.int.new("fixed"), closed=[])
        self.cur = self.parts[pname]
        return self

    def use(self, pname):
        self.cur = self.parts[pname]

    def mi(self, mat):
        if mat not in self.mats:
            self.mats.append(mat)
        return self.mats.index(mat)

    # ---- low level ----------------------------------------------------
    def _v(self, p):
        return self.cur["bm"].verts.new(Vector(p))

    def _f(self, verts, mat, smooth=False, uvs=None, closed=True):
        bm = self.cur["bm"]
        try:
            f = bm.faces.new(verts)
        except ValueError:
            return None
        f.material_index = self.mi(mat)
        f.smooth = smooth
        if uvs is not None:
            f[self.cur["fix"]] = 1
            for loop, uv in zip(f.loops, uvs):
                loop[self.cur["uv"]].uv = uv
        if closed:
            self.cur["closed"].append(f)
        return f

    class _Xf:
        def __init__(s, b, M): s.b, s.M = b, M
        def __enter__(s):
            s.part = s.b.cur
            s.start = len(s.part["bm"].verts)
        def __exit__(s, *a):
            bm = s.part["bm"]; bm.verts.ensure_lookup_table()
            bmesh.ops.transform(bm, matrix=s.M, verts=list(bm.verts)[s.start:])
    def xf(self, M):
        return Builder._Xf(self, M)

    # ---- primitives ----------------------------------------------------
    def box(self, c, s, mat, smooth=False):
        x, y, z = c; hx, hy, hz = s[0] / 2, s[1] / 2, s[2] / 2
        v = [self._v((x + sx * hx, y + sy * hy, z + sz * hz)) for sz in (-1, 1) for sy in (-1, 1) for sx in (-1, 1)]
        for q in ((0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)):
            self._f([v[i] for i in q], mat, smooth)

    def box_mm(self, mn, mx, mat):
        self.box(((mn[0] + mx[0]) / 2, (mn[1] + mx[1]) / 2, (mn[2] + mx[2]) / 2),
                 (mx[0] - mn[0], mx[1] - mn[1], mx[2] - mn[2]), mat)

    def poly(self, pts, a0, a1, mat, plane='XZ', smooth_range=None):
        """Extrude a simple 2D polygon. plane XZ: pts=(x,z) extruded along y a0..a1;
        YZ: pts=(y,z) along x; XY: pts=(x,y) along z. smooth_range=(i0,i1): side segments i0..i1 smooth."""
        def P(p, a):
            if plane == 'XZ': return (p[0], a, p[1])
            if plane == 'YZ': return (a, p[0], p[1])
            return (p[0], p[1], a)
        rngs = [] if smooth_range is None else ([smooth_range] if isinstance(smooth_range[0], int) else smooth_range)
        n = len(pts)
        f0 = [self._v(P(p, a0)) for p in pts]; f1 = [self._v(P(p, a1)) for p in pts]
        self._f(f0, mat); self._f(list(reversed(f1)), mat)
        for i in range(n):
            j = (i + 1) % n
            sm = any(a <= i < b for (a, b) in rngs)
            self._f([f0[i], f0[j], f1[j], f1[i]], mat, sm)

    def lathe(self, c, profile, seg, mat, smooth=True, cap=True, closed_profile=False, tile=None):
        """Surface of revolution around Z through c. profile=[(r, z_rel)...]."""
        tile = tile or UV_TILE.get(mat, 1.0)
        rings = []
        for (r, z) in profile:
            if r < 1e-6:
                rings.append([self._v((c[0], c[1], c[2] + z))])
            else:
                rings.append([self._v((c[0] + r * math.cos(j * math.tau / seg), c[1] + r * math.sin(j * math.tau / seg), c[2] + z))
                              for j in range(seg)])
        vlen = [0.0]
        for i in range(1, len(profile)):
            vlen.append(vlen[-1] + math.hypot(profile[i][0] - profile[i - 1][0], profile[i][1] - profile[i - 1][1]))
        pairs = [(i, i + 1) for i in range(len(profile) - 1)]
        if closed_profile:
            pairs.append((len(profile) - 1, 0))
            vlen.append(vlen[-1] + math.hypot(profile[0][0] - profile[-1][0], profile[0][1] - profile[-1][1]))
        for (i, k) in pairs:
            A, B = rings[i], rings[k]
            r1, r2 = profile[i][0], profile[k][0]
            v1, v2 = vlen[i] / tile, (vlen[k] if k > i else vlen[-1]) / tile
            for j in range(seg):
                j2 = (j + 1) % seg
                u1, u2 = j / seg * math.tau * max(r1, r2, .05) / tile, (j + 1) / seg * math.tau * max(r1, r2, .05) / tile
                if len(A) == 1 and len(B) == 1: continue
                if len(A) == 1:
                    self._f([A[0], B[j2], B[j]], mat, smooth, [(u1, v1), (u2, v2), (u1, v2)])
                elif len(B) == 1:
                    self._f([A[j], A[j2], B[0]], mat, smooth, [(u1, v1), (u2, v1), (u1, v2)])
                else:
                    self._f([A[j], A[j2], B[j2], B[j]], mat, smooth, [(u1, v1), (u2, v1), (u2, v2), (u1, v2)])
        if cap and not closed_profile:
            if len(rings[0]) > 1: self._f(list(reversed(rings[0])), mat)
            if len(rings[-1]) > 1: self._f(list(rings[-1]), mat)

    def tube(self, pts, radii, sides, mat, closed=False, cap=True, smooth=True, tile=None, twist=0.0):
        """Sweep a circle along a polyline. radii: float or list. Radius<1e-4 collapses to a point."""
        tile = tile or UV_TILE.get(mat, 1.0)
        pts = [Vector(p) for p in pts]
        n = len(pts)
        if not isinstance(radii, (list, tuple)): radii = [radii] * n
        tans = []
        for i in range(n):
            if closed:
                t = pts[(i + 1) % n] - pts[(i - 1) % n]
            else:
                t = pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]
            tans.append(t.normalized())
        ref = Vector((0, 0, 1)) if abs(tans[0].z) < 0.9 else Vector((1, 0, 0))
        nrm = (ref - tans[0] * ref.dot(tans[0])).normalized()
        rings = []; length = [0.0]
        for i in range(n):
            if i > 0:
                nrm = nrm - tans[i] * nrm.dot(tans[i])
                nrm = nrm.normalized() if nrm.length > 1e-6 else Vector((0, 0, 1))
                length.append(length[-1] + (pts[i] - pts[i - 1]).length)
            bn = tans[i].cross(nrm)
            if radii[i] < 1e-4:
                rings.append([self._v(pts[i])])
            else:
                rings.append([self._v(pts[i] + (nrm * math.cos(j * math.tau / sides + twist) + bn * math.sin(j * math.tau / sides + twist)) * radii[i])
                              for j in range(sides)])
        if closed:
            length.append(length[-1] + (pts[0] - pts[-1]).length)
        segs = n if closed else n - 1
        circ = math.tau * max(radii) / tile
        for i in range(segs):
            k = (i + 1) % n
            A, B = rings[i], rings[k]
            v1, v2 = length[i] / tile, length[i + 1] / tile
            for j in range(sides):
                j2 = (j + 1) % sides
                u1, u2 = j / sides * circ, (j + 1) / sides * circ
                if len(A) == 1 and len(B) == 1: continue
                if len(A) == 1: self._f([A[0], B[j2], B[j]], mat, smooth, [(u1, v1), (u2, v2), (u1, v2)])
                elif len(B) == 1: self._f([A[j], A[j2], B[0]], mat, smooth, [(u1, v1), (u2, v1), (u1, v2)])
                else: self._f([A[j], A[j2], B[j2], B[j]], mat, smooth, [(u1, v1), (u2, v1), (u2, v2), (u1, v2)])
        if cap and not closed:
            if len(rings[0]) > 1: self._f(list(reversed(rings[0])), mat)
            if len(rings[-1]) > 1: self._f(list(rings[-1]), mat)

    def ring_loop(self, center, ax, ay, R, r, segs, sides, mat, ry=None):
        """Closed planar loop (circle/ellipse) tube; ax, ay in-plane unit vectors."""
        ry = ry or R
        c = Vector(center); ax = Vector(ax); ay = Vector(ay)
        pts = [c + ax * (R * math.cos(i * math.tau / segs)) + ay * (ry * math.sin(i * math.tau / segs)) for i in range(segs)]
        self.tube(pts, r, sides, mat, closed=True)

    def grid_cloth(self, nx, ny, fn, mat, thickness=0.006):
        """Open double-sided cloth. fn(i,j)->(x,y,z) for 0<=i<=nx,0<=j<=ny; second skin offset along +-y."""
        for sgn in (1, -1):
            vs = {}
            for i in range(nx + 1):
                for j in range(ny + 1):
                    p = fn(i, j); vs[(i, j)] = self._v((p[0], p[1] + sgn * thickness / 2, p[2]))
            for i in range(nx):
                for j in range(ny):
                    quad = [vs[(i, j)], vs[(i + 1, j)], vs[(i + 1, j + 1)], vs[(i, j + 1)]]
                    if sgn < 0: quad = list(reversed(quad))
                    self._f(quad, mat, True, closed=False)

    # ---- finalize ----------------------------------------------------
    def finish(self):
        """Build mesh objects; returns (objects, info)."""
        objs = []; info = dict(parts={}, tris=0)
        for pname, P in self.parts.items():
            bm = P["bm"]
            bm.faces.ensure_lookup_table()
            closed = [f for f in P["closed"] if f.is_valid]
            if closed:
                bmesh.ops.recalc_face_normals(bm, faces=closed)
            bm.normal_update()
            uv = P["uv"]; fix = P["fix"]
            for f in bm.faces:
                if f[fix]: continue
                tile = UV_TILE.get(self.mats[f.material_index], 1.0)
                nx, ny, nz = (abs(f.normal.x), abs(f.normal.y), abs(f.normal.z))
                for loop in f.loops:
                    p = loop.vert.co
                    if nz >= nx and nz >= ny: uvc = (p.x, p.y)
                    elif nx >= ny: uvc = (p.y, p.z)
                    else: uvc = (p.x, p.z)
                    loop[uv].uv = (uvc[0] / tile, uvc[1] / tile)
            bmesh.ops.triangulate(bm, faces=bm.faces[:], quad_method='BEAUTY', ngon_method='EAR_CLIP')
            bmesh.ops.translate(bm, vec=-P["origin"], verts=bm.verts[:])
            mesh = bpy.data.meshes.new(pname)
            bm.to_mesh(mesh); bm.free()
            for m in self.mats: mesh.materials.append(get_mat(m))
            obj = bpy.data.objects.new(pname, mesh)
            obj.location = P["origin"]
            bpy.context.scene.collection.objects.link(obj)
            objs.append(obj)
        bpy.context.view_layer.update()
        mn = Vector((1e9, 1e9, 1e9)); mx = Vector((-1e9, -1e9, -1e9)); tris = 0
        for obj in objs:
            t = len(obj.data.polygons)
            tris += t
            pb = [Vector(c) for c in obj.bound_box]
            wmn = Vector((min((obj.matrix_world @ c)[k] for c in pb) for k in range(3)))
            wmx = Vector((max((obj.matrix_world @ c)[k] for c in pb) for k in range(3)))
            info["parts"][obj.name] = dict(tris=t, min=[round(v, 4) for v in wmn], max=[round(v, 4) for v in wmx],
                                           size=[round(wmx[k] - wmn[k], 4) for k in range(3)])
            mn = Vector(min(mn[k], wmn[k]) for k in range(3)); mx = Vector(max(mx[k], wmx[k]) for k in range(3))
        info.update(tris=tris, size=[round(mx[k] - mn[k], 4) for k in range(3)],
                    min=[round(v, 4) for v in mn], max=[round(v, 4) for v in mx], materials=self.mats, notes=self.notes)
        return objs, info

def export_asset(builder, budget=None, spec=None):
    """Build, assert budget/spec, export Resources/Castle/<name>.fbx, clean the scene."""
    objs, info = builder.finish()
    name = builder.name
    if budget is not None:
        assert info["tris"] <= budget, "%s: %d tris exceeds budget %d" % (name, info["tris"], budget)
    if spec:
        spec(info)
    cc.CASTLE.mkdir(parents=True, exist_ok=True)
    for o in bpy.context.scene.objects: o.select_set(False)
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    path = cc.CASTLE / (name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={'MESH'},
                             axis_forward='-Z', axis_up='Y', global_scale=1.0, apply_unit_scale=True,
                             apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
                             mesh_smooth_type='FACE', use_mesh_modifiers=True, add_leaf_bones=False,
                             use_tspace=False, path_mode='STRIP', embed_textures=False)
    for o in objs:
        mesh = o.data
        bpy.data.objects.remove(o); bpy.data.meshes.remove(mesh)
    info["budget"] = budget
    info["file"] = path.relative_to(cc.ROOT).as_posix()
    info["bytes"] = path.stat().st_size
    MANIFEST[name] = info
    print("HC_ASSET %-18s tris=%5d size=%s parts=%s" % (name, info["tris"], info["size"], list(info["parts"])), flush=True)
    return info

def write_manifest(tag):
    mdir = cc.ROOT / "docs/haunted-castle/art"
    mdir.mkdir(parents=True, exist_ok=True)
    p = mdir / ("manifest_%s.json" % tag)
    p.write_text(json.dumps(MANIFEST, indent=1, sort_keys=True) + "\n")
    print("HC_MANIFEST %s %d assets" % (p, len(MANIFEST)))

def approx(a, b, tol=0.02):
    return abs(a - b) <= tol
