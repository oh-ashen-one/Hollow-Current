"""Prepare Count Claude (demon redesign) for Unity. CPU-only headless Blender, no network, no UI.

    /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/count_prepare.py [-- --preview DIR]

Input : tools/count_source/CountClaude_demon_rigged.glb (Customuse Tripo v3.1 multiview mesh + Customuse auto-rig; see
        docs/haunted-castle/ASSET_LOG.md) and tools/count_source/CountClaude_demon_albedo.png (tools/count_project_refs.py).
Output: Assets/HollowCurrent/Art/Characters/CountClaude/{Native/*.fbx,Maps/Albedo.png,Native/CountClaude.json}

Steps: turn the mesh to face -Y, scale the head top to 2.5 m (feet at z=0, hips over the origin); remap the generic
117-bone auto-rig to the canonical names (Hips, Spine02, Spine, neck, Head, Jaw, arms, legs) and fold every other
bone's weights into its mapped ancestor; classify the wing membrane from the rig's wing-chain weights and re-skin it
onto a fan per wing (root at the shoulder blade, five fan bones by angle, Ext children for a radial stretch, zero-weight
End markers for the runtime silhouette); double the single-sheet membrane faces; put the membrane in material slot 1;
author eight clips (Animator Mode = index): 0 Perch, 1 Glide, 2 Search, 3 Dive, 4 Stagger, 5 Death, 6 Grab (forward
wing-wrap, jaw open), 7 Crawl (four-limbed, under 2.0 m lintels). Contact poses use a two-bone IK on the posed skeleton;
every clip is measured against the evaluated mesh and the numbers go to the receipt.
"""
from pathlib import Path
import bpy, bmesh, json, math, sys
import numpy as np
from mathutils import Vector, Matrix, Quaternion, Euler

ROOT = Path(__file__).resolve().parents[1]
# Round 7 (owner-approved demon redesign): CustomUse Tripo v3.1 multiview mesh + CustomUse auto-rig (117 generic bones,
# wings rigged as four-finger chains). Texture: tools/count_project_refs.py bakes the reference views onto its UVs
# (the mesh came back untextured); the baked PNG is committed because the reference PNGs are not in git.
SOURCE = ROOT / 'tools/count_source/CountClaude_demon_rigged.glb'
ALBEDO = ROOT / 'tools/count_source/CountClaude_demon_albedo.png'
OUT = ROOT / 'Assets/HollowCurrent/Art/Characters/CountClaude'
HEAD_TOP = 2.5                     # a nine-foot demon; 2.5 m keeps him under the 3.0 m ceilings
K = HEAD_TOP / 2.15                # clip offsets and IK targets were authored for the 2.15 m round-6 figure
YAW = -90.0                        # Tripo faces +X; turn him to face -Y like the round-1 rig (X = his left, Y = back)
FPS = 30
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
PREVIEW = Path(argv[argv.index('--preview') + 1]) if '--preview' in argv else None
(OUT / 'Native').mkdir(parents=True, exist_ok=True)
(OUT / 'Maps').mkdir(parents=True, exist_ok=True)
bpy.context.preferences.filepaths.save_version = 0
D = math.radians

# ---------------------------------------------------------------- import, turn to face -Y, metres
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(SOURCE))
objs = list(bpy.context.scene.objects)
rig = next(o for o in objs if o.type == 'ARMATURE')
body = max((o for o in objs if o.type == 'MESH'), key=lambda o: len(o.data.vertices))
for o in objs:
    if o not in (body, rig):
        bpy.data.objects.remove(o, do_unlink=True)   # stray 80-tri icosphere from the rigger
body.name = 'CountClaudeBody'; rig.name = 'CountClaudeRig'
def select_only(*items):
    bpy.ops.object.select_all(action='DESELECT')
    for o in items: o.select_set(True)
    bpy.context.view_layer.objects.active = items[0]
rig.matrix_world = Matrix.Rotation(D(YAW), 4, 'Z') @ rig.matrix_world; bpy.context.view_layer.update()   # glTF objects use quaternions
select_only(body); bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM')
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
select_only(rig); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
body.parent = rig; body.matrix_parent_inverse = Matrix.Identity(4)
for m in body.modifiers:
    if m.type == 'ARMATURE': m.object = rig
arm = rig.data
me = body.data
# Bind the baked albedo (previews; the FBX strips paths and CountClaudeImport binds Maps/Albedo.png itself).
_mat = me.materials[0] if me.materials else bpy.data.materials.new('CountClaude')
if not me.materials: me.materials.append(_mat)
_mat.use_nodes = True; _nt = _mat.node_tree
_bsdf = next(n for n in _nt.nodes if n.type == 'BSDF_PRINCIPLED')
_tex = _nt.nodes.new('ShaderNodeTexImage'); _tex.image = bpy.data.images.load(str(ALBEDO)); _nt.links.new(_tex.outputs['Color'], _bsdf.inputs['Base Color'])

# ---------------------------------------------------------------- canonical skeleton from the generic auto-rig
# Tripo bone -> the names the clips and CountClaudeImport use. Sides: after the turn, +X is his left.
MAP = {'tripo::Spine_0': 'Hips', 'tripo::Spine_1': 'Spine02', 'tripo::Spine_2': 'Spine', 'bone_4': 'neck', 'bone_5': 'Head',
       'bone_6': 'Jaw', 'tripo::Spine_3': 'LeftShoulder', 'tripo::Spine_4': 'LeftArm', 'tripo::Spine_5': 'LeftForeArm',
       'bone_10': 'LeftHand', 'bone_27': 'RightShoulder', 'bone_28': 'RightArm', 'bone_29': 'RightForeArm', 'bone_30': 'RightHand',
       'bone_85': 'LeftUpLeg', 'bone_86': 'LeftLeg', 'bone_87': 'LeftFoot', 'bone_88': 'LeftToeBase',
       'tripo::1_Left_Limb_0': 'RightUpLeg', 'tripo::1_Left_Limb_1': 'RightLeg', 'tripo::1_Left_Limb_2': 'RightFoot',
       'tripo::1_Left_Limb_3': 'RightToeBase'}
WING_ROOTS = ('tripo::Head_0', 'bone_67')          # the rigger's two wing chains (four fingers each)
# Coat panels hang from the root: back tails and the side front flap follow the thighs, the centre flap the hips.
COAT = {'tripo::0_Left_Limb_0': 'RightUpLeg', 'bone_109': 'LeftUpLeg', 'bone_102': 'RightUpLeg', 'bone_105': 'SPLIT', 'tripo::Root': 'Hips'}
missing = [n for n in list(MAP) + list(WING_ROOTS) if n not in arm.bones]
assert not missing, 'auto-rig layout changed, missing %s' % missing
def target_of(bone):
    b = bone
    while b is not None:
        if b.name in WING_ROOTS: return 'WING'
        if b.name in MAP: return MAP[b.name]
        if b.name in COAT: return COAT[b.name]
        b = b.parent
    return 'Hips'
TARGET = {b.name: target_of(b) for b in arm.bones}

# ---------------------------------------------------------------- scale to the head top, feet at z=0, hips over the origin
head_idx = [g.index for g in body.vertex_groups if TARGET.get(g.name) in ('Head', 'Jaw')]
head_top = max(v.co.z for v in me.vertices if any(g.group in head_idx and g.weight > .5 for g in v.groups))
low = min(v.co.z for v in me.vertices)
scale = HEAD_TOP / (head_top - low)
hips = arm.bones['tripo::Spine_0'].head_local.copy()
offset = Vector((-hips.x * scale, -hips.y * scale, -low * scale))
me.transform(Matrix.Translation(offset) @ Matrix.Scale(scale, 4)); me.update()
select_only(rig); bpy.ops.object.mode_set(mode='EDIT')
for eb in arm.edit_bones:
    eb.head = eb.head * scale + offset; eb.tail = eb.tail * scale + offset
bpy.ops.object.mode_set(mode='OBJECT')
hi = max(v.co.z for v in me.vertices); lo = min(v.co.z for v in me.vertices)
triangles = sum(len(p.vertices) - 2 for p in me.polygons)
assert triangles < 45000 and abs(lo) < .005, (triangles, lo)   # source mesh; the two-sided membrane adds faces below

# ---------------------------------------------------------------- wing fan rig, derived from the rig's wing chains
def smooth(x):
    x = max(0.0, min(1.0, x)); return x * x * (3 - 2 * x)
SIDES = (('L', 1), ('R', -1))
names = {g.index: g.name for g in body.vertex_groups}
original = [{names[g.group]: g.weight for g in v.groups} for v in me.vertices]
wingshare = []
for old in original:
    tot = sum(old.values()) or 1.0
    wingshare.append(sum(w for k, w in old.items() if TARGET.get(k) == 'WING') / tot)
co = [v.co.copy() for v in me.vertices]
# Pivot: each wing chain's root joint (the shoulder-blade knuckle); plane: mean horizontal direction to its membrane.
roots = {}
for bname in ('tripo::0_Right_Limb_0', 'tripo::1_Right_Limb_1'):
    h = arm.bones[bname].head_local.copy(); roots['L' if h.x > 0 else 'R'] = h
assert set(roots) == {'L', 'R'}, roots
PIVOT = {}; OUTWARD = {}
for s, sg in SIDES:
    P = roots[s]; PIVOT[s] = P
    pts = [c for c, w in zip(co, wingshare) if w > .5 and (c.x > 0) == (sg > 0)]
    d = Vector((0, 0, 0))
    for c in pts: d += Vector((c.x - P.x, c.y - P.y, 0))
    OUTWARD[s] = d.normalized()
FAN_AXIS = {s: Vector((0, 0, 1)).cross(OUTWARD[s]).normalized() for s, _ in SIDES}
def raw_angle(s, c):
    rel = c - PIVOT[s]; return math.degrees(math.atan2(rel.dot(OUTWARD[s]), rel.z)), math.hypot(rel.dot(OUTWARD[s]), rel.z)
phis = sorted(raw_angle('L' if c.x > 0 else 'R', c)[0] for c, w in zip(co, wingshare) if w > .5)
lo_phi = max(5.0, phis[int(len(phis) * .03)]); hi_phi = min(178.0, phis[int(len(phis) * .97)])
FAN_REST = [lo_phi + (hi_phi - lo_phi) * k / 4 for k in range(5)]
FAN_OPEN = [48.0, 72.0, 95.0, 115.0, 132.0]      # fully spread: hook out-up, fingers out-down
NFAN = len(FAN_REST)
radii_all = sorted(raw_angle('L' if c.x > 0 else 'R', c)[1] for c, w in zip(co, wingshare) if w > .5)
EXT_R = radii_all[int(len(radii_all) * .97)]      # radius of each fan bone's extension child (folded tip reach)
print('HC_WING pivots', {s: tuple(round(x, 3) for x in PIVOT[s]) for s in PIVOT}, 'outward', {s: tuple(round(x, 2) for x in OUTWARD[s]) for s in OUTWARD},
      'fan', [round(x, 1) for x in FAN_REST], 'ext_r', round(EXT_R, 2), flush=True)
def fan_dir(side, phi):
    return (OUTWARD[side] * math.sin(D(phi)) + Vector((0, 0, 1)) * math.cos(D(phi))).normalized()
def fan_angle(side, c):
    phi, r = raw_angle(side, c)
    if phi < -90: phi += 360                     # below and inboard of the pivot: nearest to the lowest finger, not the hook
    return phi, r

radii = {(s, k): [] for s, _ in SIDES for k in range(NFAN)}
skin = []; wing_verts = 0
for c, old, wf in zip(co, original, wingshare):
    w = {}
    tot = sum(old.values()) or 1.0
    for k, val in old.items():
        t = TARGET.get(k, 'Hips')
        if t == 'SPLIT': t = 'LeftUpLeg' if c.x > 0 else 'RightUpLeg'   # centre coat flap follows the thigh on its side
        if t != 'WING': w[t] = w.get(t, 0) + val / tot
    if wf > 0:
        side = 'L' if c.x > 0 else 'R'
        phi, r = fan_angle(side, c)
        rel = c - PIVOT[side]
        inboard = smooth(-rel.dot(OUTWARD[side]) / .06) * (1 - smooth((abs(rel.z) - .10) / .14))
        rootw = max(1 - smooth(r / .18), inboard)
        p = min(max(phi, FAN_REST[0]), FAN_REST[-1])
        k = max(i for i in range(NFAN - 1) if FAN_REST[i] <= p) if p < FAN_REST[-1] else NFAN - 2
        t = (p - FAN_REST[k]) / (FAN_REST[k + 1] - FAN_REST[k])
        w['Wing' + side] = w.get('Wing' + side, 0) + wf * rootw
        e = min(r / EXT_R, 1.0)
        for kk, share in ((k, 1 - t), (k + 1, t)):
            base = wf * (1 - rootw) * share
            w['WingFan%s%d' % (side, kk)] = w.get('WingFan%s%d' % (side, kk), 0) + base * (1 - e)
            w['WingFan%s%dExt' % (side, kk)] = w.get('WingFan%s%dExt' % (side, kk), 0) + base * e
        if wf > .5:
            wing_verts += 1; radii[(side, k if t < .5 else k + 1)].append(r)
    top = sorted(((k, val) for k, val in w.items() if val > 1e-4), key=lambda kv: -kv[1])[:4]
    total = sum(val for _, val in top) or 1.0; skin.append([(k, val / total) for k, val in top])

# Rebuild the armature: keep the mapped bones (renamed), drop the rest, add the wing fans.
bpy.context.view_layer.objects.active = rig; select_only(rig); bpy.ops.object.mode_set(mode='EDIT')
eb = arm.edit_bones
for old, new in MAP.items(): eb[old].name = new
for b in list(eb):
    if b.name not in MAP.values():
        eb.remove(b)
for leg in ('LeftUpLeg', 'RightUpLeg'): eb[leg].parent = eb['Hips']
eb['Hips'].parent = None
for s, sg in SIDES:
    root = eb.new('Wing' + s); root.head = PIVOT[s]; root.tail = PIVOT[s] + Vector((0, .12, 0)); root.parent = eb['Spine']
    for k, phi in enumerate(FAN_REST):
        fb = eb.new('WingFan%s%d' % (s, k)); fb.head = PIVOT[s]; fb.tail = PIVOT[s] + fan_dir(s, phi) * .25; fb.parent = root
        rr = sorted(radii[(s, k)]); reach = rr[int(len(rr) * .97)] if rr else EXT_R
        ext = eb.new('WingFan%s%dExt' % (s, k)); ext.head = PIVOT[s] + fan_dir(s, phi) * EXT_R
        ext.tail = ext.head + fan_dir(s, phi) * .05; ext.parent = fb
        end = eb.new('WingFan%s%dEnd' % (s, k)); end.head = PIVOT[s] + fan_dir(s, phi) * reach
        end.tail = end.head + fan_dir(s, phi) * .05; end.parent = ext; end.use_deform = False
bpy.ops.object.mode_set(mode='OBJECT')
for g in list(body.vertex_groups): body.vertex_groups.remove(g)
for b in arm.bones:
    if b.use_deform: body.vertex_groups.new(name=b.name)
for i, entries in enumerate(skin):
    for k, val in entries: body.vertex_groups[k].add([i], val, 'REPLACE')
print('HC_RIG bones', len(arm.bones), 'wing vertices', wing_verts, 'head top', round(hi - lo, 3), flush=True)

# Two-sided membrane (round 3: the hunt showed a wing as a black smear). About half of the membrane is a single sheet,
# so Unity's back-face culling hides it, or shows its dark inside, from behind. Duplicate every single-sheet wing face
# with reversed winding and negated custom normals; faces that already have an opposite-facing twin within 1.5 cm
# (the double-layered parts) are left alone so nothing z-fights.
from mathutils import kdtree
corner = [tuple(c.vector) for c in me.corner_normals]
loop_normals = {pl.index: [corner[li] for li in pl.loop_indices] for pl in me.polygons}
loop_verts = {pl.index: list(pl.vertices) for pl in me.polygons}
wingw = [0.0] * len(me.vertices)
gname = {g.index: g.name for g in body.vertex_groups}
for v in me.vertices: wingw[v.index] = sum(g.weight for g in v.groups if gname[g.group].startswith('Wing'))
wing_faces = [pl for pl in me.polygons if all(wingw[i] > .5 for i in pl.vertices)]
tree = kdtree.KDTree(len(wing_faces))
for n, pl in enumerate(wing_faces): tree.insert(pl.center, n)
tree.balance()
single = set()
for n, pl in enumerate(wing_faces):
    if not any(m != n and wing_faces[m].normal.dot(pl.normal) < -.5 for _, m, _ in tree.find_range(pl.center, .015)): single.add(pl.index)
original_polys = len(me.polygons)
bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
src_face = bm.faces.layers.int.new('hc_src_face'); src_vert = bm.verts.layers.int.new('hc_src_vert')
bm.verts.index_update()
for f in bm.faces: f[src_face] = f.index           # duplicates inherit these values from their source
for v in bm.verts: v[src_vert] = v.index
dup = bmesh.ops.duplicate(bm, geom=[bm.faces[i] for i in sorted(single)])
new_faces = [f for f in dup['geom'] if isinstance(f, bmesh.types.BMFace)]
bmesh.ops.reverse_faces(bm, faces=new_faces)
bm.to_mesh(me); bm.free(); me.update()
sf = me.attributes['hc_src_face'].data; sv = me.attributes['hc_src_vert'].data
normals = []
for pl in me.polygons:
    s0 = sf[pl.index].value
    if pl.index < original_polys:
        normals.extend(loop_normals[pl.index])
    else:
        for li in pl.loop_indices:
            k = loop_verts[s0].index(sv[me.loops[li].vertex_index].value)
            normals.append(tuple(-c for c in loop_normals[s0][k]))
me.normals_split_custom_set(normals)
me.attributes.remove(me.attributes['hc_src_face']); me.attributes.remove(me.attributes['hc_src_vert']); me.update()
two_sided_faces = len(me.polygons) - original_polys
triangles = sum(len(pl.vertices) - 2 for pl in me.polygons)
assert triangles < 50000, triangles
print('HC_TWO_SIDED single-sheet wing faces duplicated', two_sided_faces, 'of', len(wing_faces), 'triangles', triangles, flush=True)
# Material slot 1 = the skin-wing membrane (CastleArt.Gore Membrane in the castle), slot 0 = body and coat (Gore Skin).
if len(me.materials) < 2:
    membrane = me.materials[0].copy(); membrane.name = 'CountClaudeMembrane'; me.materials.append(membrane)
wingw = [0.0] * len(me.vertices)
gname = {g.index: g.name for g in body.vertex_groups}
for v in me.vertices: wingw[v.index] = sum(g.weight for g in v.groups if gname[g.group].startswith('Wing'))
membrane_faces = 0
for pl in me.polygons:
    pl.material_index = 1 if all(wingw[i] > .5 for i in pl.vertices) else 0; membrane_faces += pl.material_index
print('HC_MATERIAL_SLOTS body', len(me.polygons) - membrane_faces, 'membrane', membrane_faces, flush=True)

# ---------------------------------------------------------------- pose helpers
scene = bpy.context.scene; scene.render.fps = FPS
rig.animation_data_create()
for b in rig.pose.bones: b.rotation_mode = 'QUATERNION'
def lerp(a, b, t): return a + (b - a) * t
def ease(t): return smooth(t)
def Q(axis, deg): return Quaternion(Vector(axis).normalized(), D(deg))
class Pose:
    """Rotations are deltas in armature axes (X character-left, Y back, Z up) applied relative to the parent:
    +X swings a down-pointing limb back and tilts an up-pointing bone forward."""
    def __init__(s): s.q = {}; s.loc = {}; s.scl = {}
    def rq(s, bone, q): s.q[bone] = q @ s.q.get(bone, Quaternion())
    def r(s, bone, x=0, y=0, z=0): s.rq(bone, Euler((D(x), D(y), D(z)), 'XYZ').to_quaternion())
    def l(s, bone, x=0, y=0, z=0, raw=False):
        # Offsets were authored for the 2.15 m figure: scale them with K (wing Ext stretches pass raw metres).
        f = 1.0 if raw else K
        a = s.loc.get(bone, (0, 0, 0)); s.loc[bone] = (a[0] + x * f, a[1] + y * f, a[2] + z * f)
    def wings(s, spread, yaw=0, beat=0, pitch=0, curl=0, lag=0, stretch=1.0):
        """spread 0 = folded rest .. 1 = open fan; yaw + sweeps the wing forward, - back; beat + raises it;
        pitch + tilts its top back; curl bends the lower fingers further out; lag delays the lower fan (flap wave)."""
        for side, sg in SIDES:
            s.rq('Wing' + side, Q((0, 0, -sg), yaw) @ Q((0, -sg, 0), beat) @ Q((-1, 0, 0), pitch))
            for k in range(NFAN):
                f = k / (NFAN - 1)
                ang = lerp(FAN_REST[k], FAN_OPEN[k], spread) - FAN_REST[k] + curl * f + lag * f * f
                s.rq('WingFan%s%d' % (side, k), Quaternion(FAN_AXIS[side], D(ang)))
                if stretch != 1.0: s.l('WingFan%s%dExt' % (side, k), *(fan_dir(side, FAN_REST[k]) * ((lerp(1.0, stretch, f) - 1) * EXT_R)), raw=True)
    def arms(s, swing, out=0, forearm=0, hand=0, inward=0):
        for side, sg in (('Left', 1), ('Right', -1)):
            s.r(side + 'Arm', x=swing, y=-sg * out, z=-sg * inward)
            s.r(side + 'ForeArm', x=forearm); s.r(side + 'Hand', x=hand)
    def legs(s, thigh, knee, foot=0, spread=0):
        for side, sg in (('Left', 1), ('Right', -1)):
            s.r(side + 'UpLeg', x=thigh, y=-sg * spread); s.r(side + 'Leg', x=knee); s.r(side + 'Foot', x=foot)
    def spine(s, lean, head=0, yaw=0, tilt=0, neck=None):
        for b, f in (('Spine02', .45), ('Spine', .40)): s.r(b, x=lean * f)   # the auto-rig has two spine joints above the hips
        s.r('neck', x=(-lean * .15 + head * .5) if neck is None else neck, z=yaw * .4)
        s.r('Head', x=-lean * .25 + head * .5, y=tilt, z=yaw * .6)

def pose_to_bones(pose):
    for b in rig.pose.bones:
        b.rotation_quaternion = Quaternion(); b.location = (0, 0, 0); b.scale = (1, 1, 1)
    for name, delta in pose.q.items():
        rest = arm.bones[name].matrix_local.to_quaternion()
        rig.pose.bones[name].rotation_quaternion = rest.inverted() @ delta @ rest
    for name, v in pose.scl.items():
        rig.pose.bones[name].scale = (1.0, v, 1.0)
    for name, v in pose.loc.items():
        rest = arm.bones[name].matrix_local.to_quaternion(); rig.pose.bones[name].location = rest.inverted() @ Vector(v)
def update(): bpy.context.view_layer.update()
def rotate_about(pb, pivot, q):
    pb.matrix = Matrix.Translation(pivot) @ q.to_matrix().to_4x4() @ Matrix.Translation(-pivot) @ pb.matrix; update()
def two_bone(upper, lower, end, target, pole):
    """Analytic two-bone IK on the posed skeleton (pose space). Bone heads are joints; tails are ignored."""
    pu, pl, pe = (rig.pose.bones[n] for n in (upper, lower, end))
    S = pu.head.copy(); E = pl.head.copy(); W = pe.head.copy()
    a = (E - S).length; b = (W - E).length; T = Vector(target)
    d = min(max((T - S).length, abs(a - b) + 1e-3), a + b - 1e-3); x = (T - S).normalized(); T = S + x * d
    p = Vector(pole) - S; y = (p - x * p.dot(x)).normalized()
    alpha = math.acos(max(-1, min(1, (a * a + d * d - b * b) / (2 * a * d))))
    E2 = S + a * (math.cos(alpha) * x + math.sin(alpha) * y)
    rotate_about(pu, S, (E - S).rotation_difference(E2 - S))
    W1 = pe.head.copy(); rotate_about(pl, E2, (W1 - E2).rotation_difference(T - E2))
def plant(foot, yaw=0):
    pb = rig.pose.bones[foot]; rest = arm.bones[foot].matrix_local.to_3x3()
    pb.matrix = Matrix.Translation(pb.head) @ (Matrix.Rotation(D(yaw), 3, 'Z') @ rest).to_4x4(); update()
def lift_hips(dz):
    pb = rig.pose.bones['Hips']; pb.matrix = Matrix.Translation((0, 0, dz)) @ pb.matrix; update()
def mesh_points():
    dg = bpy.context.evaluated_depsgraph_get(); eo = body.evaluated_get(dg); m = eo.to_mesh()
    co = np.empty(len(m.vertices) * 3, dtype=np.float32); m.vertices.foreach_get('co', co); eo.to_mesh_clear()
    return co.reshape(-1, 3)

# ---------------------------------------------------------------- clips
def perch(p, ph, f):
    # Gargoyle crouch on a balustrade (refs/lord_perch.png): hips sunk between spread knees, torso past horizontal,
    # head up watching the room, claws planted on the rail in front of the toes, wings half-open and raised behind.
    br = math.sin(ph * 2 * math.pi); look = math.tanh(2.2 * math.sin(ph * 2 * math.pi + .6))
    p.l('Hips', z=-.86 + .010 * br, y=.10); p.r('Hips', x=38)
    for b, a in (('Spine02', 42), ('Spine', 14 + 1.5 * br)): p.r(b, x=a)
    p.r('neck', x=-44, z=6 * look); p.r('Head', x=-38, y=2 * look, z=10 * look)
    p.wings(.22 + .03 * br, yaw=-24, beat=36 + 2 * br, pitch=40, curl=2 * br, stretch=1.12)
def perch_ik(f, ph):
    br = math.sin(ph * 2 * math.pi)
    for side, sg in (('Left', 1), ('Right', -1)):
        two_bone(side + 'UpLeg', side + 'Leg', side + 'Foot', (sg * .30 * K, .03 * K, ANKLE), (sg * 1.4 * K, -.6 * K, .5 * K)); plant(side + 'Foot', sg * 16)
        two_bone(side + 'Arm', side + 'ForeArm', side + 'Hand', (sg * .22 * K, -.32 * K, (.06 + .004 * br) * K), (sg * K, .4 * K, .9 * K))   # wrists at the rail's front edge, claws over it
def glide(p, ph, f):
    a = ph * 2 * math.pi; flap = math.sin(a)
    p.r('Hips', x=30); p.l('Hips', z=.03 * math.sin(a - .4), y=.02 * flap)
    p.spine(8 + 2 * flap, neck=-24, head=-36, yaw=5 * math.sin(a * .5))
    p.legs(14 + 3 * flap, 20 + 5 * flap, 38, spread=3); p.arms(36 + 4 * flap, out=8, forearm=18, hand=12, inward=-6)
    p.wings(.98 + .06 * math.sin(a - .5), yaw=26, beat=10 + 24 * flap, pitch=10, curl=-6 * math.sin(a - 1.0), lag=10 * math.sin(a - 1.4), stretch=1.45)
def search(p, ph, f):
    # Landed prowl as on the sheet: upright, arms at his sides, wings folded to the shins and twitching, head scanning.
    a = ph * 2 * math.pi; look = math.tanh(3.0 * math.sin(a))
    p.r('Hips', x=6); p.l('Hips', z=-.04 + .008 * math.sin(2 * a))
    p.spine(8, head=4, yaw=58 * look, tilt=5 * look); p.legs(-10, 18, -8, spread=2)
    p.arms(4, out=-10, forearm=-12 + 3 * math.sin(2 * a + .5), hand=8, inward=2)
    p.wings(.03 + .03 * math.sin(2 * a), yaw=-4, beat=3 + 2 * math.sin(2 * a - .4), pitch=2, curl=-16 + 3 * math.sin(2 * a - .9), stretch=1.26)
def search_ik(f, ph):
    for side, sg in (('Left', 1), ('Right', -1)):
        two_bone(side + 'UpLeg', side + 'Leg', side + 'Foot', (sg * .24 * K, (.12 if sg > 0 else -.08) * K, ANKLE), (sg * .3 * K, -K, .6 * K)); plant(side + 'Foot', sg * 10)
def dive(p, ph, f):
    a = ph * 2 * math.pi; fl = math.sin(a)
    p.r('Hips', x=52); p.l('Hips', z=.02 * fl)
    p.spine(10, neck=-36, head=-44); p.legs(18 + 3 * fl, 26 + 4 * fl, 42, spread=2)
    p.arms(-70 + 4 * fl, out=14, forearm=-6, hand=-14, inward=6)
    p.wings(.58 + .05 * fl, yaw=-38, beat=6 + 7 * fl, pitch=26, curl=-4 * math.sin(a - .8), lag=6 * math.sin(a - 1.2), stretch=1.35)
def grab(p, ph, f):
    # Round 6 climax: a forward wing-wrap. He rears up, looks down at the lens, both arms reach straight at it at
    # shoulder width, and the wings sweep forward on both sides (yaw ~52 deg, membranes' inner faces toward the camera)
    # so their tips reach the frame edges at the 2.8 m capture hold.
    a = ease(min(1, f / .45)); b = ease(max(0, min(1, (f - .15) / .6)))
    p.r('Hips', x=lerp(48, 6, a)); p.spine(lerp(10, 4, a), neck=lerp(-36, 14, a), head=lerp(-44, 22, a))
    p.r('Jaw', x=lerp(0, JAW_OPEN, b))                       # the split jaw gapes at the lens
    p.legs(lerp(18, 8, a), lerp(26, 22, a), lerp(42, 30, a))
    p.arms(lerp(-70, -40, b), out=lerp(14, 8, b), forearm=lerp(-6, -12, b), hand=lerp(-14, -10, b), inward=lerp(6, -2, b))   # forward-down at the lens
    p.wings(lerp(.6, 1.0, b), yaw=lerp(-30, 84, a), beat=lerp(6, 20, a), pitch=lerp(26, 6, a), curl=lerp(0, -22, b), stretch=2.0)
def crawl(p, ph, f):
    # Mode 7: stooped four-limbed crawl under 2.0 m lintels. Hips ~0.9 m, torso near horizontal, head up, wings folded
    # flat along the back; hands and feet are planted by crawl_ik in a diagonal gait. Navigation moves him.
    a = ph * 2 * math.pi
    p.l('Hips', z=-.62 + .02 * abs(math.cos(a))); p.r('Hips', x=70)
    p.spine(8, neck=-52, head=-30, yaw=6 * math.sin(a))
    p.wings(0.0, yaw=-14, beat=-4, pitch=62, curl=-6, stretch=.82)
def crawl_ik(f, ph):
    a = ph * 2 * math.pi
    for side, sg, off in (('Left', 1, 0.0), ('Right', -1, math.pi)):
        s1 = math.sin(a + off); lift = max(0.0, math.cos(a + off))
        foot = (sg * .26 * K, (.40 + .16 * s1) * K, ANKLE + .10 * K * lift)
        two_bone(side + 'UpLeg', side + 'Leg', side + 'Foot', foot, (sg * .6 * K, -1.2 * K, .9 * K)); plant(side + 'Foot', sg * 8)
        s2 = math.sin(a + off + math.pi); lift2 = max(0.0, math.cos(a + off + math.pi))   # diagonal: hand opposite its foot
        hand = (sg * .34 * K, (-.66 - .16 * s2) * K, (.40 + .08 * lift2) * K)   # long fingers reach the floor
        two_bone(side + 'Arm', side + 'ForeArm', side + 'Hand', hand, (sg * 1.2 * K, .3 * K, .5 * K))
def stagger(p, ph, f):
    a = ease(min(1, f / .14)); rec = ease(max(0, min(1, (f - .32) / .68))); sh = math.sin(f * 38) * (1 - f) ** 2
    p.r('Hips', x=lerp(-14, 10, rec) * a); p.l('Hips', y=.14 * a * (1 - rec), z=-.06 * a)
    p.spine(lerp(-24, 12, rec) * a, head=lerp(-26, 4, rec) * a, yaw=8 * sh)
    p.legs(lerp(-10, 6, rec) * a, lerp(16, 20, rec) * a, lerp(-6, 0, rec) * a)
    p.arms(lerp(-30, 10, rec) * a, out=lerp(38, 14, rec) * a, forearm=lerp(40, 34, rec) * a, hand=22 * a)
    p.wings(lerp(.2, lerp(.55, .45, rec), a) + .05 * sh, yaw=lerp(0, lerp(10, 2, rec), a), beat=lerp(0, lerp(14, 8, rec), a) + 6 * sh, pitch=lerp(0, lerp(34, 10, rec), a), curl=10 * sh, stretch=1.3)
def death(p, ph, f):
    # buckles at the knees, then topples backward (away from the sword) and lies flat on his spread wings
    a = ease(min(1, f / .38)); b = ease(max(0, min(1, (f - .30) / .55)))
    p.r('Hips', x=lerp(0, 14, a) - 90 * b); p.l('Hips', z=-.45 * a - .40 * b, y=.10 * a + .50 * b)
    p.spine(lerp(0, 22, a) - 26 * b, head=10 * a + 4 * b, yaw=22 * b, tilt=8 * b)
    p.legs(lerp(0, -8, a) + 2 * b, lerp(0, 90, a) - 80 * b, lerp(0, 30, a) + 10 * b)
    p.arms(lerp(0, 14, a) + 16 * b, out=lerp(0, 16, a) + 34 * b, forearm=lerp(0, 24, a) - 8 * b, hand=20 * a)
    p.wings(lerp(.2, .8, a) + .15 * b, yaw=lerp(0, -8, a) + 8 * b, beat=lerp(0, 10, a) - 4 * b, pitch=lerp(0, 6, a) - 6 * b, curl=-6 * b, stretch=1.12)

# (name, pose fn, seconds, loop, post-IK fn, floor rule): 'plant' keeps the lowest mesh point on z=0,
# 'lift' only lifts it back above z=0 (airborne clips), None leaves the IK-planted feet alone.
# 'skim' shifts the whole airborne clip by one constant so its lowest point over the clip is SKIM above the origin:
# the runtime's hover height is then clearance under the feet/coat, and Mode 1 at hover 0 walks a service door.
SKIM = .10
JAW_OPEN = -28.0
ANKLE = arm.bones['LeftFoot'].head_local.z           # rest ankle height (feet planted by IK)
CLIPS = [('Perch', perch, 3.2, True, perch_ik, None), ('Glide', glide, 2.0, True, None, 'skim'),
         ('Search', search, 2.6, True, search_ik, None), ('Dive', dive, 1.0, True, None, 'skim'),
         ('Stagger', stagger, 1.3, False, None, 'plant'), ('Death', death, 2.4, False, None, 'plant'),
         ('Grab', grab, 0.9, False, None, 'skim'), ('Crawl', crawl, 1.4, True, crawl_ik, None)]
DEFORM = [b.name for b in arm.bones]
actions = {}; metrics = {}
def lowest_over(fn, end, loop, ik):
    low = 1e9
    for frame in range(0, end + 1):
        ph = (frame / end) % 1.0 if loop else frame / end
        pose = Pose(); fn(pose, ph, frame / end); pose_to_bones(pose); update()
        if ik: ik(frame / end, ph)
        low = min(low, float(mesh_points()[:, 2].min()))
    return low
for name, fn, seconds, loop, ik, floor in CLIPS:
    end = round(seconds * FPS)
    shift = (SKIM if floor == 'skim' else 0.0) - lowest_over(fn, end, loop, ik) if floor in ('skim', 'ground') else 0.0
    action = bpy.data.actions.new(name); action.use_fake_user = True; rig.animation_data.action = action
    if action.slots: rig.animation_data.action_slot = action.slots[0]
    prev = {}; spans = []; tops = []; lows = []; reach = []
    for frame in range(0, end + 1):
        ph = (frame / end) % 1.0 if loop else frame / end; f = frame / end
        if loop and frame == end: ph = 0.0
        pose = Pose(); fn(pose, ph, f); pose_to_bones(pose); update()
        if ik: ik(f, ph)
        if shift: lift_hips(shift)
        pts = mesh_points()
        if floor == 'plant':
            lift_hips(-float(pts[:, 2].min())); pts = mesh_points()
        spans.append(float(pts[:, 0].max() - pts[:, 0].min())); tops.append(float(pts[:, 2].max()))
        lows.append(float(pts[:, 2].min())); reach.append(float(-pts[:, 1].min()))
        for b in rig.pose.bones:
            q = b.rotation_quaternion.copy()
            if b.name in prev and prev[b.name].dot(q) < 0: q.negate(); b.rotation_quaternion = q
            prev[b.name] = q
            b.keyframe_insert('rotation_quaternion', frame=frame, group=b.name); b.keyframe_insert('location', frame=frame, group=b.name); b.keyframe_insert('scale', frame=frame, group=b.name)
    actions[name] = (action, end, loop)
    metrics[name] = {'seconds': round(end / FPS, 3), 'loop': loop, 'hip_shift_m': round(shift, 3), 'span_m_min': round(min(spans), 3), 'span_m_max': round(max(spans), 3),
                     'top_m_max': round(max(tops), 3), 'lowest_z_min': round(min(lows), 3), 'forward_reach_m_max': round(max(reach), 3),
                     'forward_reach_m_last': round(reach[-1], 3)}
    print('HC_CLIP', name, json.dumps(metrics[name]), flush=True)
rig.animation_data.action = None
for b in rig.pose.bones: b.rotation_quaternion = Quaternion(); b.location = (0, 0, 0); b.scale = (1, 1, 1)
update()
rest_pts = mesh_points()
shoulder_width = (arm.bones['LeftArm'].head_local - arm.bones['RightArm'].head_local).length

# ---------------------------------------------------------------- texture and receipts
import shutil; shutil.copyfile(ALBEDO, OUT / 'Maps/Albedo.png')   # baked by tools/count_project_refs.py

# ---------------------------------------------------------------- preview (optional, not part of the deliverable)
def preview(directory):
    directory.mkdir(parents=True, exist_ok=True)
    sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.light = 'FLAT'; sc.display.shading.color_type = 'TEXTURE'
    sc.render.resolution_x = 520; sc.render.resolution_y = 720; sc.render.image_settings.file_format = 'PNG'
    sc.world = bpy.data.worlds.new('w'); sc.world.color = (.28, .28, .32)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = 3.2 * K
    views = {'front': (0, -8, 1.15), 'side': (-8, 0, 1.15), 'back': (0, 8, 1.15), 'three': (-5.6, -5.6, 2.6)}
    rig.animation_data.action = None
    shots = [('rest', None, None)] + [(n, n, f) for n in actions for f in (0.0, .25, .5, .75, 1.0)]
    for label, clip, frac in shots:
        if clip:
            action, end, loop = actions[clip]; rig.animation_data.action = action
            if action.slots: rig.animation_data.action_slot = action.slots[0]
            sc.frame_set(int(round(end * frac)))
        else:
            rig.animation_data.action = None
            for b in rig.pose.bones: b.rotation_quaternion = Quaternion(); b.location = (0, 0, 0); b.scale = (1, 1, 1)
        update()
        for vname in (('front', 'side', 'three') if clip else ('front', 'side', 'back', 'three')):
            loc = Vector(views[vname]); cam.location = loc
            cam.rotation_euler = (Vector((0, 0, 1.15)) - loc).to_track_quat('-Z', 'Y').to_euler()
            sc.render.filepath = str(directory / ('%s_%s_%s.png' % (label, ('%03d' % int(frac * 100)) if clip else 'rest', vname)))
            bpy.ops.render.render(write_still=True)
    rig.animation_data.action = None
if PREVIEW:
    preview(PREVIEW)
    for b in rig.pose.bones: b.rotation_quaternion = Quaternion(); b.location = (0, 0, 0); b.scale = (1, 1, 1)
    bpy.ops.wm.save_as_mainfile(filepath=str(PREVIEW / 'count.blend'))

# ---------------------------------------------------------------- export (mirrors tools/prepare_splitgrin.py)
rig.animation_data.action = None
for b in rig.pose.bones: b.rotation_quaternion = Quaternion(); b.location = (0, 0, 0); b.scale = (1, 1, 1)
update()
select_only(rig, body)
bpy.ops.export_scene.fbx(filepath=str(OUT / 'Native/CountClaudeBody.fbx'), use_selection=True, object_types={'ARMATURE', 'MESH'},
                         add_leaf_bones=False, bake_anim=False, axis_forward='-Z', axis_up='Y', path_mode='STRIP')
select_only(rig)
for old in (OUT / 'Native').glob('CountClaude*.fbx'):
    if old.stem[len('CountClaude'):] not in actions and old.stem != 'CountClaudeBody':
        old.unlink(); Path(str(old) + '.meta').unlink(missing_ok=True)
for name, (action, end, loop) in actions.items():
    rig.animation_data.action = action
    if action.slots: rig.animation_data.action_slot = action.slots[0]
    scene.frame_start = 0; scene.frame_end = end
    bpy.ops.export_scene.fbx(filepath=str(OUT / ('Native/CountClaude%s.fbx' % name)), use_selection=True, object_types={'ARMATURE'},
                             add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
                             bake_anim_simplify_factor=0, axis_forward='-Z', axis_up='Y', path_mode='STRIP')
report = {'source': str(SOURCE.relative_to(ROOT)), 'head_top_m': HEAD_TOP, 'mesh_height_m': round(hi - lo, 4), 'triangles': triangles,
          'vertices': len(me.vertices), 'bones': len(arm.bones), 'wing_vertices': wing_verts, 'membrane_faces': membrane_faces,
          'rest_span_m': round(float(rest_pts[:, 0].max() - rest_pts[:, 0].min()), 3), 'shoulder_width_m': round(shoulder_width, 3),
          'grab_reach_m': metrics['Grab']['forward_reach_m_last'], 'clips': metrics}
(OUT / 'Native/CountClaude.json').write_text(json.dumps(report, indent=2) + '\n')
print('HC_BLENDER_EXPORT_PASS CountClaude', json.dumps(report), flush=True)
