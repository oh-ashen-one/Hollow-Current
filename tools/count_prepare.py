"""Prepare Count Claude (demon redesign) for Unity. CPU-only headless Blender, no network, no UI.

    /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/count_prepare.py [-- --preview DIR] [--only-new] [--proof DIR]

    --only A,B  re-do just the named clips (any mode, e.g. Grab,GrabDrop,GrabCrawl); same merge rules as --only-new
    --only-new  compute and export just the F5 clips (8 GrabDrop, 9 GrabCrawl); the mesh, the eight older FBX files and
                their receipt entries are left untouched and the receipt is merged (idempotent, re-runnable).
    --proof DIR render the F5 clips from the player's camera (GrabReach + 0.6 m out, eye 1.64 m, 2560x1440) to
                DIR/grab_drop_pose.png, grab_crawl_pose.png and the frame-0 images, and print the measurements.

Input : tools/count_source/CountClaude_demon_rigged.glb (Customuse Tripo v3.1 multiview mesh + Customuse auto-rig; see
        docs/haunted-castle/ASSET_LOG.md) and tools/count_source/CountClaude_demon_albedo.png (tools/count_project_refs.py).
Output: Assets/HollowCurrent/Art/Characters/CountClaude/{Native/*.fbx,Maps/Albedo.png,Native/CountClaude.json}

Steps: turn the mesh to face -Y, scale the head top to 2.5 m (feet at z=0, hips over the origin); remap the generic
117-bone auto-rig to the canonical names (Hips, Spine02, Spine, neck, Head, Jaw, arms, legs) and fold every other
bone's weights into its mapped ancestor; classify the wing membrane from the rig's wing-chain weights and re-skin it
onto a fan per wing (root at the shoulder blade, five fan bones by angle, Ext children for a radial stretch, zero-weight
End markers for the runtime silhouette); double the single-sheet membrane faces; put the membrane in material slot 1;
author eight clips (Animator Mode = index): 0 Perch, 1 Glide, 2 Search, 3 Dive, 4 Stagger, 5 Death, 6 Grab (forward
wing-wrap, jaw open), 7 Crawl (four-limbed, under 2.0 m lintels), 8 GrabDrop (falls from 2.2 m onto the camera, 1.6 s),
9 GrabCrawl (low lunge from the crawl, rears up, claws on the lens, 1.4 s). Contact poses use a two-bone IK on the posed skeleton;
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
EMISSION = ROOT / 'tools/count_source/CountClaude_demon_albedo_emission.png'   # ember eyes
FLESH = ROOT / 'tools/count_source/CountClaude_demon_albedo_flesh.png'         # flayed chest/forearm UV mask
OUT = ROOT / 'Assets/HollowCurrent/Art/Characters/CountClaude'
HEAD_TOP = 2.5                     # a nine-foot demon; 2.5 m keeps him under the 3.0 m ceilings
K = HEAD_TOP / 2.15                # clip offsets and IK targets were authored for the 2.15 m round-6 figure
YAW = -90.0                        # Tripo faces +X; turn him to face -Y like the round-1 rig (X = his left, Y = back)
FPS = 30
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
PREVIEW = Path(argv[argv.index('--preview') + 1]) if '--preview' in argv else None
PROOF = Path(argv[argv.index('--proof') + 1]) if '--proof' in argv else None
ONLY_NAMES = argv[argv.index('--only') + 1].split(',') if '--only' in argv else (['GrabDrop', 'GrabCrawl'] if '--only-new' in argv else None)
ONLY_NEW = ONLY_NAMES is not None   # partial run: compute/export just ONLY_NAMES (+ Crawl, measured only, as the shard/angle baseline), merge the receipt, leave the mesh alone
DIAG = '--diag' in argv   # --diag Crawl,GrabCrawl          # print per-clip skinning blow-out metrics for Crawl/Grab/GrabDrop/GrabCrawl and stop (no export)
RECEIPT = OUT / 'Native/CountClaude.json'
RUNTIME_MODES = OUT.parents[2] / 'Resources/Characters/CountClaude/CountClaudeModes.json'   # shipped: the runtime detects Mode 8/9 here
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
WING_STRETCH_CAP = 1.6              # F5 grab clips: outer wing finger never longer than 1.6x rest (2.2x drew rod-like fingers and zig-zag tips)
GRAB_STRETCH = WING_STRETCH_CAP     # Mode 6 wing-wrap (was 2.2); 1.3 was tried and only shrinks the membrane, the long leading finger stays
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
# Round 8: spreading keeps 72% of the folded fan's angular range (r7 compressed 170 deg into 84 deg, halving the membrane:
# the 'strands' in the grab). The Ext stretch makes up the rest; membrane area per clip is measured into the receipt.
FAN_MID = (FAN_REST[0] + FAN_REST[-1]) / 2
FAN_OPEN = [FAN_MID + (r - FAN_MID) * .72 for r in FAN_REST]
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
# Material slot 2 = flayed flesh (CastleArt.Gore Flesh in the castle), from the baked UV mask of red torso/forearm texels.
if len(me.materials) < 3:
    flesh_mat = me.materials[0].copy(); flesh_mat.name = 'CountClaudeFlesh'; me.materials.append(flesh_mat)
_fl = bpy.data.images.load(str(FLESH)); _fw, _fh = _fl.size
_fpx = np.empty(_fw * _fh * 4, dtype=np.float32); _fl.pixels.foreach_get(_fpx); _fpx = _fpx.reshape(_fh, _fw, 4)[..., 0]
_uv = me.uv_layers.active.data; flesh_faces = 0
for pl in me.polygons:
    if pl.material_index == 1: continue
    u = sum(_uv[li].uv[0] for li in pl.loop_indices) / len(pl.loop_indices); v = sum(_uv[li].uv[1] for li in pl.loop_indices) / len(pl.loop_indices)
    if _fpx[min(_fh - 1, max(0, int(v * _fh))), min(_fw - 1, max(0, int(u * _fw)))] > .5: pl.material_index = 2; flesh_faces += 1
print('HC_MATERIAL_SLOTS body', len(me.polygons) - membrane_faces - flesh_faces, 'membrane', membrane_faces, 'flesh', flesh_faces, flush=True)
def membrane_area():
    dg = bpy.context.evaluated_depsgraph_get(); m = body.evaluated_get(dg).to_mesh(); a = 0.0
    for pl in m.polygons:
        if pl.material_index == 1: a += pl.area
    body.evaluated_get(dg).to_mesh_clear(); return a / 2      # the membrane is doubled (two-sided): count one side
REST_MEMBRANE = membrane_area()

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
        s.stretch_used = max(getattr(s, 'stretch_used', 0.0), stretch)   # outer finger length factor, recorded per clip (receipt wing_stretch_max)
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
    if DIAG and upper.endswith('Arm'): print('HC_IK', upper, 'a', round(a, 2), 'b', round(b, 2), 'dist', round((T - S).length, 2), 'shoulder', tuple(round(v, 2) for v in S), flush=True)
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
    # Round 8 climax: a forward wing-wrap with the torso pitched ~33 deg toward the camera (critic r7: >= 25), the head
    # looking down the lens with the split jaw open, both claws reaching at the lens at shoulder width (arm = -85 relative
    # to the leaned torso = ~52 deg forward of straight down, i.e. along the line to a camera 2.8 m out and 1.6 m lower),
    # and the wings half-wrapped (yaw 45: the membranes' inner faces toward the camera, not edge-on) with full area.
    a = ease(min(1, f / .45)); b = ease(max(0, min(1, (f - .15) / .6)))
    p.r('Hips', x=lerp(48, 30, a)); p.spine(lerp(10, 10, a), neck=lerp(-36, -14, a), head=lerp(-44, -10, a))
    p.r('Jaw', x=lerp(0, JAW_OPEN, b))                       # the split jaw gapes at the lens
    p.legs(lerp(18, -6, a), lerp(26, 30, a), lerp(42, 34, a))
    p.arms(lerp(-70, -66, b), out=lerp(14, 22, b), forearm=lerp(-6, -10, b), hand=lerp(-14, -18, b), inward=lerp(6, 4, b))   # down-out past the lens, framing it
    p.wings(lerp(.6, 1.0, b), yaw=lerp(-30, 66, a), beat=lerp(6, 30, a), pitch=lerp(26, 10, a), curl=lerp(0, -10, b), stretch=GRAB_STRETCH)
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

# ---------------------------------------------------------------- F5 grab clips (Modes 8 and 9)
# Both are keyframed control curves (monotone cubic through (f, value) keys, f = 0..1 over the clip) feeding the same Pose/IK
# helpers as the older clips, in metres (armature axes: X character-left, Y back = -Y is forward toward the camera, Z up).
# The camera in the game sits GrabReach + 0.6 m in front of the origin at 1.64 m: every claw/eye target below is aimed at it.
def cubic(f, keys):
    ts = [k[0] for k in keys]; vs = [k[1] for k in keys]; n = len(keys)
    if f <= ts[0]: return vs[0]
    if f >= ts[-1]: return vs[-1]
    d = [(vs[i + 1] - vs[i]) / (ts[i + 1] - ts[i]) for i in range(n - 1)]
    m = [0.0] * n
    for i in range(1, n - 1):
        m[i] = 0.0 if d[i - 1] * d[i] <= 0 else 2 * d[i - 1] * d[i] / (d[i - 1] + d[i])      # monotone: no overshoot between keys
    m[0] = d[0] if n > 1 else 0.0; m[-1] = d[-1]
    i = max(j for j in range(n - 1) if ts[j] <= f); h = ts[i + 1] - ts[i]; u = (f - ts[i]) / h
    h00 = 2 * u ** 3 - 3 * u ** 2 + 1; h10 = u ** 3 - 2 * u ** 2 + u; h01 = -2 * u ** 3 + 3 * u ** 2; h11 = u ** 3 - u ** 2
    return h00 * vs[i] + h10 * h * m[i] + h01 * vs[i + 1] + h11 * h * m[i + 1]
def curves(f, table):
    return {k: cubic(f, v) for k, v in table.items()}

ARM_MIN_REACH = .76                      # metres; measured against the Mode 7 Crawl envelope (see limb_angle_max_deg in the receipt)
DROP_SECONDS, DROP_FALL_SECONDS, DROP_HEIGHT = 1.6, 0.5, 2.2
DROP_LAND = DROP_FALL_SECONDS / DROP_SECONDS
def drop_height(f):
    # starts DROP_HEIGHT above the origin and falls in DROP_FALL_SECONDS (constant acceleration), then lands
    u = min(1.0, f / DROP_LAND); return DROP_HEIGHT * (1 - u * u)
L = DROP_LAND
DROP = {   # chest = hips + spine pitch toward the lens; hy forward travel (-Y); hz crouch depth; hands/feet are IK targets (metres)
    # Limbs stay inside the Mode 7 Crawl envelope (UpLeg <= 120, Leg <= 118, Arm <= 95, ForeArm <= 70 deg) so the coat and forearm skin do not shear.
    'hips_x':  [(0, 14), (L, 22), (L + .06, 32), (.62, 42), (1, 46)],
    'hz':      [(0, -.02), (L, -.10), (L + .05, -.40), (.62, -.32), (1, -.32)],
    'hy':      [(0, .10), (L, .06), (L + .05, 0), (.62, -.08), (1, -.10)],
    'lean':    [(0, 4), (L, 6), (L + .06, 12), (.62, 22), (1, 28)],
    'neck':    [(0, -10), (L, -14), (L + .06, -26), (1, -40)],
    'head':    [(0, -6), (L, -10), (1, -30)],
    'jaw':     [(0, 0), (L, -6), (.50, -32), (1, -36)],
    'wspread': [(0, 1.0), (L, 1.0), (1, 1.0)],
    'wyaw':    [(0, -10), (L, 0), (L + .08, 26), (1, 38)],
    'wbeat':   [(0, 44), (L, 36), (L + .06, 8), (1, 26)],
    'wpitch':  [(0, 14), (L, 20), (1, 14)],
    'wcurl':   [(0, -8), (L, -2), (1, -10)],
    'wstr':    [(0, WING_STRETCH_CAP), (L, WING_STRETCH_CAP), (1, WING_STRETCH_CAP)],
    'ax':      [(0, .40), (L, .44), (.55, .30), (1, .30)],           # left hand target (+x left): out beside the face, inside the frame
    'ay':      [(0, -.50), (L, -.80), (.62, -1.05), (1, -1.15)],
    'az':      [(0, 2.35), (L, 1.45), (.62, 1.30), (1, 1.30)],
    'rx':      [(0, .40), (L, .40), (.55, .12), (1, .12)],           # right hand target (x is mirrored): one claw reaches the lower centre of the lens
    'ry':      [(0, -.50), (L, -.80), (.62, -1.08), (1, -1.12)],
    'rz':      [(0, 2.35), (L, 1.45), (.62, 1.22), (1, 1.22)],
    'fx':      [(0, .22), (L, .34), (1, .30)],
    'fy':      [(0, .14), (L, -.05), (1, -.02)],
    'fz':      [(0, .12), (L, .05), (L + .05, 0), (1, 0)],
}
def drop(p, ph, f):
    c = curves(f, DROP)
    p.l('Hips', y=c['hy'], z=c['hz'], raw=True); p.r('Hips', x=c['hips_x'])
    p.spine(c['lean'], neck=c['neck'], head=c['head']); p.r('Jaw', x=c['jaw'])
    p.legs(0, 0)
    p.wings(c['wspread'], yaw=c['wyaw'], beat=c['wbeat'], pitch=c['wpitch'], curl=c['wcurl'], stretch=c['wstr'])
def drop_ik(f, ph):
    c = curves(f, DROP)
    for side, sg in (('Left', 1), ('Right', -1)):
        two_bone(side + 'UpLeg', side + 'Leg', side + 'Foot', (sg * c['fx'], c['fy'], ANKLE + c['fz']), (sg * 1.3, -1.0, .7)); plant(side + 'Foot', sg * 12)
        k = ('a' if sg > 0 else 'r'); target = Vector((sg * c[k + 'x'], c[k + 'y'], c[k + 'z']))
        shoulder = rig.pose.bones[side + 'Arm'].head.copy(); gap = target - shoulder       # same elbow-range guard as GrabCrawl (ARM_MIN_REACH)
        if gap.length < ARM_MIN_REACH: target = shoulder + gap.normalized() * ARM_MIN_REACH
        two_bone(side + 'Arm', side + 'ForeArm', side + 'Hand', target, (sg * 1.1, .6, 1.1))
def drop_lift(f): return drop_height(f)

CRAWL_GRAB_SECONDS, LUNGE_M = 1.4, 1.2
CG = {   # limb angles are kept inside the Mode 7 Crawl envelope (UpLeg <= 120, Leg <= 118, Arm <= 95, ForeArm <= 70 deg): beyond it the coat panels and forearm skin shear into shards
    'hips_x':  [(0, 70), (.20, 74), (.34, 78), (.52, 76), (.78, 58), (1, 44)],
    'hz':      [(0, -.72), (.20, -.78), (.34, -.74), (.52, -.80), (.78, -.66), (1, -.56)],
    'hy':      [(0, 0), (.20, .14), (.34, -.05), (.52, -LUNGE_M), (.78, -.55), (1, -.15)],
    'lean':    [(0, 8), (.20, 4), (.52, -4), (.78, 10), (1, 16)],
    'neck':    [(0, -52), (.20, -58), (.52, -50), (.78, -54), (1, -62)],
    'head':    [(0, -30), (.52, -26), (1, -46)],
    'jaw':     [(0, 0), (.30, -6), (.40, -34), (.80, -34), (1, -36)],
    'wspread': [(0, 0), (.30, .1), (.52, .45), (.78, .9), (1, 1.0)],
    'wyaw':    [(0, -14), (.52, -10), (.78, 16), (1, 30)],
    'wbeat':   [(0, -4), (.52, 8), (.78, 22), (1, 26)],
    'wpitch':  [(0, 62), (.52, 40), (1, 14)],
    'wcurl':   [(0, -6), (1, -10)],
    'wstr':    [(0, .82), (.52, 1.3), (1, WING_STRETCH_CAP)],
    'rx':      [(0, .34), (.52, .30), (.78, .24), (1, .18)],         # right hand: closes on the lower centre of the lens
    'ry':      [(0, -.77), (.34, -.72), (.52, -1.95), (.78, -1.35), (1, -1.12)],
    'rz':      [(0, .47), (.34, .55), (.52, .30), (.78, .90), (1, 1.22)],
    'ax':      [(0, .34), (.52, .30), (1, .34)],
    'ay':      [(0, -.77), (.34, -.72), (.52, -1.95), (.78, -1.35), (1, -1.15)],
    'az':      [(0, .47), (.34, .55), (.52, .30), (.78, .85), (1, 1.32)],
    'fx':      [(0, .26), (1, .30)],
    'fy':      [(0, .40), (.34, .55), (.52, -.20), (.70, -.40), (1, -.04)],
    'fz':      [(0, 0), (.34, .02), (.46, .26), (.62, .04), (1, 0)],
}
def crawl_grab(p, ph, f):
    c = curves(f, CG)
    p.l('Hips', y=c['hy'], z=c['hz'], raw=True); p.r('Hips', x=c['hips_x'])
    p.spine(c['lean'], neck=c['neck'], head=c['head']); p.r('Jaw', x=c['jaw'])
    p.legs(0, 0)
    p.wings(c['wspread'], yaw=c['wyaw'], beat=c['wbeat'], pitch=c['wpitch'], curl=c['wcurl'], stretch=c['wstr'])
POLE = (1.2, .3, .5)                     # elbow pole (x mirrored, y relative to the hips, z)
def crawl_grab_ik(f, ph):
    c = curves(f, CG)
    for side, sg in (('Left', 1), ('Right', -1)):
        two_bone(side + 'UpLeg', side + 'Leg', side + 'Foot', (sg * c['fx'], c['fy'], ANKLE + c['fz']), (sg * .7, -1.2 + c['hy'], .9)); plant(side + 'Foot', sg * 8)
        k = ('a' if sg > 0 else 'r'); target = Vector((sg * c[k + 'x'], c[k + 'y'], c[k + 'z']))
        # keep the elbow inside the Crawl's range: the wrist never closer to the shoulder than ARM_MIN_REACH (a shorter reach folds the forearm past ~70 deg)
        shoulder = rig.pose.bones[side + 'Arm'].head.copy(); gap = target - shoulder
        if gap.length < ARM_MIN_REACH: target = shoulder + gap.normalized() * ARM_MIN_REACH
        two_bone(side + 'Arm', side + 'ForeArm', side + 'Hand', target, (sg * POLE[0], c['hy'] + POLE[1], POLE[2]))

# ---------------------------------------------------------------- wave L grab variants (Modes 10, 11, 12)
# Round 12 critic: "7 of 9 grabs are the same Mode 9 low crawl lunge". Two more kill presentations, authored like Modes 8/9
# (keyframed control tables + the same Pose/IK helpers, limbs inside the Mode 7 envelope, wing stretch <= WING_STRETCH_CAP):
#   10 GrabSideL / 11 GrabSideR  side snatch: he arrives from the player's left / right flank, one claw crosses the frame first,
#                                then the skull swings in to the lens (the two clips are mirror images; Generic rigs cannot mirror at runtime).
#   12 GrabDropSpin              ceiling drop that lands BEHIND the player (all-fours landing, head down); the runtime spins the camera
#                                (yaw <= SPIN_YAW_DEG, SPIN_START_S..SPIN_END_S) into his face, he rears up as it arrives, claws close from both sides.
# Clip space: camera at (0, -(reach + 0.6), 1.64) looking +Y, image right = +X, so the player's LEFT is -X. Body-local targets below are
# turned into armature space by to_world(); the body origin (ox, oy) and heading yaw are keyframed per clip.
SIDE_SECONDS, SPIN_SECONDS = 1.2, 1.9
SIDE_START_X = 3.4                  # metres from the frame centre where the snatch starts: outside the horizontal FOV at 2.2 m (+-2.8), so he enters the frame
SPIN_YAW_DEG, SPIN_START_S, SPIN_END_S = 120.0, 0.5, 1.15     # runtime camera yaw schedule for GrabDropSpin (documented in the receipt)
GRAB_VARIANT_REACH = 1.6            # final claw reach target (GrabDrop is 1.615): the stand-off stays reach + CameraClearance
def to_world(local, ox, oy, yaw_deg):
    c, sn = math.cos(D(yaw_deg)), math.sin(D(yaw_deg))
    return Vector((ox + c * local[0] - sn * local[1], oy + sn * local[0] + c * local[1], local[2]))
def guard_arm(side, target):
    shoulder = rig.pose.bones[side + 'Arm'].head.copy(); gap = target - shoulder
    return shoulder + gap.normalized() * ARM_MIN_REACH if gap.length < ARM_MIN_REACH else target

def side_table(S):
    # S = -1: from the player's left (x < 0), +1: from the right. Heading: facing the travel direction (-S * 90 deg), turning to the camera.
    lead_sg = 1 if S > 0 else -1                           # the camera-side arm leads: his left arm when travelling toward -X
    return {
     'ox':    [(0, S * SIDE_START_X), (.12, S * 2.35), (.28, S * 1.10), (.44, S * .32), (.60, S * .04), (1, 0)],
     'oy':    [(0, .25), (.30, .10), (.60, -.04), (1, -.10)],
     'yaw':   [(0, -S * 55), (.22, -S * 48), (.40, -S * 30), (.52, -S * 10), (.62, 0), (1, 0)],
     'hips_x':[(0, 52), (.25, 50), (.45, 44), (.62, 42), (1, 46)],
     'hz':    [(0, -.46), (.25, -.40), (.45, -.34), (.62, -.32), (1, -.32)],
     'lean':  [(0, 12), (.45, 16), (.62, 22), (1, 28)],
     'neck':  [(0, -14), (.40, -24), (.60, -38), (1, -40)],
     'head':  [(0, -6), (.40, -14), (.60, -26), (1, -30)],
     'hyaw':  [(0, 0), (.10, .85), (.40, .85), (.60, .35), (.70, 0), (1, 0)],      # head yaw as a fraction of -body yaw: he looks at her while running
     'jaw':   [(0, 0), (.25, -10), (.42, -34), (1, -36)],
     'wspread':[(0, .55), (.30, .8), (.5, 1.0), (1, 1.0)],
     'wyaw':  [(0, -22), (.30, -6), (.55, 24), (1, 34)],
     'wbeat': [(0, 12), (.40, 22), (1, 26)],
     'wpitch':[(0, 40), (.5, 20), (1, 14)],
     'wcurl': [(0, -4), (1, -10)],
     'wstr':  [(0, 1.2), (.5, 1.5), (1, WING_STRETCH_CAP)],
     # lead claw sweeps across the lens as he arrives, trailing claw follows; both close at the end (local = body frame, metres)
     'lx':    [(0, .30), (.25, .30), (.45, .20), (.62, .12), (1, .12)],
     'ly':    [(0, -.80), (.25, -1.00), (.45, -1.08), (.62, -1.08), (1, -.95)],
     'lz':    [(0, 1.00), (.25, 1.05), (.45, 1.10), (.62, 1.20), (1, 1.22)],
     'tx':    [(0, .30), (.30, .32), (.62, .30), (1, .30)],
     'ty':    [(0, -.30), (.30, -.70), (.62, -.98), (1, -.99)],
     'tz':    [(0, .80), (.30, 1.00), (.62, 1.26), (1, 1.30)],
     'fs':    [(0, 0), (.50, 1.0), (.62, 0), (1, 0)],      # stride amplitude (feet bound while he runs in)
     'lead_sg': lead_sg}
SIDE_TABLES = {'GrabSideL': side_table(-1), 'GrabSideR': side_table(1)}
def side_c(f, tab): return {k: (cubic(f, v) if isinstance(v, list) else v) for k, v in tab.items()}
def side_pose_for(tab):
    def fn(p, ph, f):
        c = side_c(f, tab)
        p.l('Hips', x=c['ox'], y=c['oy'], z=c['hz'], raw=True); p.r('Hips', x=c['hips_x']); p.r('Hips', z=c['yaw'])
        p.spine(c['lean'], neck=c['neck'], head=c['head'], yaw=-c['yaw'] * c['hyaw']); p.r('Jaw', x=c['jaw'])
        p.legs(0, 0)
        p.wings(c['wspread'], yaw=c['wyaw'], beat=c['wbeat'], pitch=c['wpitch'], curl=c['wcurl'], stretch=c['wstr'])
    return fn
def side_ik_for(tab):
    def ik(f, ph):
        c = side_c(f, tab); a = 2 * math.pi * 2.2 * min(1.0, f / .5)
        for side, sg in (('Left', 1), ('Right', -1)):
            ph_s = a + (0 if sg > 0 else math.pi); stride = c['fs']
            foot = to_world((sg * .30, .30 * stride * math.sin(ph_s) + .08, ANKLE + .22 * stride * max(0.0, math.cos(ph_s))), c['ox'], c['oy'], c['yaw'])
            two_bone(side + 'UpLeg', side + 'Leg', side + 'Foot', foot, to_world((sg * .7, -1.0, .8), c['ox'], c['oy'], c['yaw'])); plant(side + 'Foot', sg * 10 + c['yaw'])
            lead = sg == c['lead_sg']
            loc = (sg * (c['lx'] if lead else c['tx']), c['ly'] if lead else c['ty'], c['lz'] if lead else c['tz'])
            two_bone(side + 'Arm', side + 'ForeArm', side + 'Hand', guard_arm(side, to_world(loc, c['ox'], c['oy'], c['yaw'])), to_world((sg * 1.1, .6, 1.0), c['ox'], c['oy'], c['yaw']))
    return ik

SPL = (.5 / SPIN_SECONDS)       # landing time (0.5 s fall, same as GrabDrop)
SPIN = {   # all-fours landing behind the player, head down; he rears up as the camera spin arrives (lock ~ f .6), claws close from both sides
 'hips_x': [(0, 16), (SPL, 24), (SPL + .04, 48), (.45, 50), (.60, 46), (.74, 42), (1, 44)],
 'hz':     [(0, -.02), (SPL, -.10), (SPL + .04, -.46), (.45, -.46), (.60, -.40), (.74, -.34), (1, -.32)],
 'hy':     [(0, .10), (SPL, .06), (SPL + .04, .10), (.45, .10), (.60, .02), (.74, -.06), (1, -.09)],
 'lean':   [(0, 4), (SPL, 6), (.45, 4), (.74, 18), (1, 24)],
 'neck':   [(0, -10), (SPL, -14), (.45, -22), (.62, -44), (.74, -50), (1, -50)],
 'head':   [(0, -6), (.45, -8), (.70, -34), (1, -34)],
 'jaw':    [(0, 0), (SPL, -6), (.45, -8), (.60, -34), (1, -36)],
 'wspread':[(0, 1.0), (1, 1.0)],
 'wyaw':   [(0, -10), (SPL, 0), (SPL + .06, 34), (.45, 40), (.74, 38), (1, 38)],
 'wbeat':  [(0, 44), (SPL, 36), (SPL + .05, 6), (.45, 12), (.74, 24), (1, 26)],
 'wpitch': [(0, 14), (SPL, 20), (1, 14)],
 'wcurl':  [(0, -6), (1, -10)],
 'wstr':   [(0, WING_STRETCH_CAP), (1, WING_STRETCH_CAP)],
 'ax':     [(0, .40), (SPL, .40), (SPL + .04, .32), (.45, .32), (.62, .40), (.74, .34), (1, .30)],
 'ay':     [(0, -.50), (SPL, -.80), (SPL + .04, -.72), (.45, -.72), (.62, -.95), (.74, -1.12), (1, -1.16)],
 'az':     [(0, 2.35), (SPL, 1.45), (SPL + .04, .55), (.45, .55), (.62, .95), (.74, 1.28), (1, 1.30)],
 'fx':     [(0, .22), (SPL, .34), (1, .30)],
 'fy':     [(0, .14), (SPL, .20), (.45, .30), (.74, .05), (1, -.02)],
 'fz':     [(0, .12), (SPL, .05), (SPL + .04, 0), (1, 0)],
}
def spin(p, ph, f):
    c = curves(f, SPIN)
    p.l('Hips', y=c['hy'], z=c['hz'], raw=True); p.r('Hips', x=c['hips_x'])
    p.spine(c['lean'], neck=c['neck'], head=c['head']); p.r('Jaw', x=c['jaw']); p.legs(0, 0)
    p.wings(c['wspread'], yaw=c['wyaw'], beat=c['wbeat'], pitch=c['wpitch'], curl=c['wcurl'], stretch=c['wstr'])
def spin_ik(f, ph):
    c = curves(f, SPIN)
    for side, sg in (('Left', 1), ('Right', -1)):
        two_bone(side + 'UpLeg', side + 'Leg', side + 'Foot', (sg * c['fx'], c['fy'], ANKLE + c['fz']), (sg * 1.3, -1.0, .7)); plant(side + 'Foot', sg * 12)
        two_bone(side + 'Arm', side + 'ForeArm', side + 'Hand', guard_arm(side, Vector((sg * c['ax'], c['ay'], c['az']))), (sg * 1.1, .6, 1.1))
def spin_lift(f):
    u = min(1.0, f / SPL); return DROP_HEIGHT * (1 - u * u)

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
         ('Grab', grab, 0.9, False, None, 'skim'), ('Crawl', crawl, 1.4, True, crawl_ik, None),
         # F5: Mode 8 and 9 are authored grounded (lowest point on z=0 at every frame; GrabDrop adds its fall height on top).
         ('GrabDrop', drop, DROP_SECONDS, False, drop_ik, 'plant'), ('GrabCrawl', crawl_grab, CRAWL_GRAB_SECONDS, False, crawl_grab_ik, 'plant'),
         # wave L variants (Modes 10-12): see the block above
         ('GrabSideL', side_pose_for(SIDE_TABLES['GrabSideL']), SIDE_SECONDS, False, side_ik_for(SIDE_TABLES['GrabSideL']), 'plant'),
         ('GrabSideR', side_pose_for(SIDE_TABLES['GrabSideR']), SIDE_SECONDS, False, side_ik_for(SIDE_TABLES['GrabSideR']), 'plant'),
         ('GrabDropSpin', spin, SPIN_SECONDS, False, spin_ik, 'plant')]
VARIANT_NAMES = ('GrabSideL', 'GrabSideR', 'GrabDropSpin')
NEW_FROM = 8                                           # CLIPS[NEW_FROM:] are the F5 clips
LIFT = {'GrabDrop': drop_lift, 'GrabDropSpin': spin_lift}                         # extra airborne lift added after the grounding rule
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

def chest_pitch_deg():
    # hips joint -> neck joint on the posed skeleton: degrees forward of vertical (toward -Y, the camera side)
    h = rig.pose.bones['Hips'].head; n = rig.pose.bones['neck'].head
    return math.degrees(math.atan2(-(n.y - h.y), n.z - h.z))
hand_groups = [g.index for g in body.vertex_groups if g.name in ('LeftHand', 'RightHand')]
hand_verts = [v.index for v in me.vertices if sum(g.weight for g in v.groups if g.group in hand_groups) > .5]
def claw_points():
    return mesh_points()[hand_verts]
if ONLY_NEW and RECEIPT.exists():
    old_receipt = json.loads(RECEIPT.read_text()); metrics.update(old_receipt['clips'])
RUN = [c for c in CLIPS if c[0] in ONLY_NAMES or c[0] == 'Crawl'] if ONLY_NEW else CLIPS    # Crawl is measured, not exported, in partial runs
if DIAG: RUN = [c for c in CLIPS if c[0] in (argv[argv.index('--diag') + 1].split(',') if len(argv) > argv.index('--diag') + 1 else ('Crawl', 'GrabCrawl'))]
# Skinning blow-out check: every mesh edge's posed length against its rest length. Wings stretch on purpose (fan Ext stretch up to
# ~2.2x), so the limit is 3x; anything beyond is a triangle dragged between bones that moved apart (a 'shard').
REST_PTS = mesh_points(); EDGES = np.array([tuple(e.vertices) for e in me.edges]); REST_LEN = np.linalg.norm(REST_PTS[EDGES[:, 0]] - REST_PTS[EDGES[:, 1]], axis=1)
EDGE_OK = REST_LEN > .002; STRETCH_LIMIT = 3.0
SHARD_LEN = .15      # a stretched edge only reads as a visible shard once it is longer than this (metres, posed)
def stretched(pts):
    posed = np.linalg.norm(pts[EDGES[:, 0]] - pts[EDGES[:, 1]], axis=1)[EDGE_OK]; ratio = posed / REST_LEN[EDGE_OK]
    bad = (ratio > STRETCH_LIMIT) & (posed > SHARD_LEN); return int(bad.sum()), float(ratio.max())
vgroup = [max(((gname[g.group], g.weight) for g in v.groups), key=lambda t: t[1])[0] if len(v.groups) else '' for v in me.vertices]
for name, fn, seconds, loop, ik, floor in RUN:
    end = round(seconds * FPS)
    shift = (SKIM if floor == 'skim' else 0.0) - lowest_over(fn, end, loop, ik) if floor in ('skim', 'ground') else 0.0
    action = bpy.data.actions.new(name); action.use_fake_user = True; rig.animation_data.action = action
    if action.slots: rig.animation_data.action_slot = action.slots[0]
    prev = {}; spans = []; tops = []; lows = []; reach = []; areas = []; pitches = []; claw_reach = []; stretch_n = []; stretch_max = []; limb_max = {}; wing_stretch = []; lat_reach = []
    for frame in range(0, end + 1):
        ph = (frame / end) % 1.0 if loop else frame / end; f = frame / end
        if loop and frame == end: ph = 0.0
        pose = Pose(); fn(pose, ph, f); pose_to_bones(pose); update()
        if ik: ik(f, ph)
        if shift: lift_hips(shift)
        pts = mesh_points()
        if floor == 'plant':
            lift_hips(-float(pts[:, 2].min())); pts = mesh_points()
        if name in LIFT:
            lift_hips(LIFT[name](f)); pts = mesh_points()
        bad_n, bad_max = stretched(pts); stretch_n.append(bad_n); stretch_max.append(bad_max)
        if DIAG and (frame % 6 == 0 or frame == end):
            ratio = np.linalg.norm(pts[EDGES[:, 0]] - pts[EDGES[:, 1]], axis=1) / np.maximum(REST_LEN, 1e-6); badv = set(EDGES[(ratio > STRETCH_LIMIT) & EDGE_OK].reshape(-1).tolist()); groups = {}
            for vi in badv: groups[vgroup[vi]] = groups.get(vgroup[vi], 0) + 1
            print('HC_STRETCH', name, frame, 'edges>%.1fx' % STRETCH_LIMIT, bad_n, 'max ratio', round(bad_max, 2), 'bones', dict(sorted(groups.items(), key=lambda kv: -kv[1])[:6]), flush=True)
        if DIAG and (frame % 3 == 0 or frame == end):
            print('HC_ANGLES', name, frame, {n: round(math.degrees(rig.pose.bones[n].rotation_quaternion.angle)) for n in ('Hips', 'Spine02', 'Spine', 'neck', 'LeftUpLeg', 'LeftLeg', 'LeftFoot', 'RightUpLeg', 'RightLeg', 'LeftArm', 'LeftForeArm', 'LeftHand', 'RightArm', 'RightForeArm', 'RightHand')}, flush=True)
        for lb in ('UpLeg', 'Leg', 'Arm', 'ForeArm'):
            limb_max[lb] = max(limb_max.get(lb, 0), *(math.degrees(rig.pose.bones[sd + lb].rotation_quaternion.angle) for sd in ('Left', 'Right')))
        wing_stretch.append(getattr(pose, 'stretch_used', 1.0))
        pitches.append(chest_pitch_deg()); claws = claw_points(); claw_reach.append(float(-claws[:, 1].min())); lat_reach.append(float(np.abs(claws[:, 0]).max()))
        spans.append(float(pts[:, 0].max() - pts[:, 0].min())); tops.append(float(pts[:, 2].max()))
        lows.append(float(pts[:, 2].min())); reach.append(float(-pts[:, 1].min()))
        if frame % 6 == 0 or frame == end: areas.append(membrane_area() / REST_MEMBRANE)
        for b in rig.pose.bones:
            q = b.rotation_quaternion.copy()
            if b.name in prev and prev[b.name].dot(q) < 0: q.negate(); b.rotation_quaternion = q
            prev[b.name] = q
            b.keyframe_insert('rotation_quaternion', frame=frame, group=b.name); b.keyframe_insert('location', frame=frame, group=b.name); b.keyframe_insert('scale', frame=frame, group=b.name)
    actions[name] = (action, end, loop)
    metrics[name] = {'seconds': round(end / FPS, 3), 'loop': loop, 'hip_shift_m': round(shift, 3), 'span_m_min': round(min(spans), 3), 'span_m_max': round(max(spans), 3),
                     'top_m_max': round(max(tops), 3), 'lowest_z_min': round(min(lows), 3), 'forward_reach_m_max': round(max(reach), 3),
                     'forward_reach_m_last': round(reach[-1], 3), 'claw_reach_m_last': round(claw_reach[-1], 3), 'stretched_edges_max': max(stretch_n), 'stretched_edges_last': stretch_n[-1], 'edge_stretch_ratio_max': round(max(stretch_max), 2), 'wing_stretch_max': round(max(wing_stretch), 3), 'limb_angle_max_deg': {k: round(v) for k, v in limb_max.items()}, 'chest_pitch_deg_last': round(pitches[-1], 1), 'chest_pitch_deg_max': round(max(pitches), 1), 'membrane_area_ratio_min': round(min(areas), 3), 'membrane_area_ratio_last': round(areas[-1], 3)}
    if name in VARIANT_NAMES:   # frame-by-frame reach (frame 0 .. last) of the grab variants, forward = toward the camera (-Y)
        metrics[name].update({'fps': FPS, 'frames': end + 1, 'claw_reach_by_frame_m': [round(x, 3) for x in claw_reach], 'mesh_reach_by_frame_m': [round(x, 3) for x in reach],
                              'claw_lateral_by_frame_m': [round(x, 3) for x in lat_reach]})
    print('HC_CLIP', name, json.dumps({k: v for k, v in metrics[name].items() if not k.endswith('_by_frame_m')}), flush=True)
rig.animation_data.action = None
for b in rig.pose.bones: b.rotation_quaternion = Quaternion(); b.location = (0, 0, 0); b.scale = (1, 1, 1)
update()
rest_pts = mesh_points()
shoulder_width = (arm.bones['LeftArm'].head_local - arm.bones['RightArm'].head_local).length

# ---------------------------------------------------------------- texture and receipts
import shutil; shutil.copyfile(ALBEDO, OUT / 'Maps/Albedo.png'); shutil.copyfile(EMISSION, OUT / 'Maps/Emission.png')   # tools/count_project_refs.py

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
# ---------------------------------------------------------------- F5 proof renders (the player's camera; Workbench, no Unity)
PROFILE_GRAB_REACH = 1.39; CAMERA_CLEARANCE = .6; EYE = 1.64; VFOV = 72.0     # CountClaudeProfile.GrabReach/CameraClearance, Investigator camera
PROOF_W, PROOF_H = 2560, 1440
def proof(directory):
    from bpy_extras.object_utils import world_to_camera_view
    directory.mkdir(parents=True, exist_ok=True)
    sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'
    sc.display.shading.light = 'STUDIO'; sc.display.shading.color_type = 'TEXTURE'; sc.display.render_aa = '8'
    sc.render.image_settings.file_format = 'PNG'; sc.render.image_settings.color_mode = 'RGB'; sc.render.image_settings.compression = 100; sc.render.dither_intensity = 0
    sc.world = bpy.data.worlds.new('proof'); sc.world.color = (.03, .035, .05)
    cam = bpy.data.objects.new('proofcam', bpy.data.cameras.new('proofcam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.sensor_fit = 'VERTICAL'; cam.data.angle = D(VFOV); cam.data.clip_start = .04; cam.data.clip_end = 75
    head_groups = [g.index for g in body.vertex_groups if g.name in ('Head', 'Jaw')]
    head_verts = [v.index for v in me.vertices if sum(g.weight for g in v.groups if g.group in head_groups) > .5]
    left = [v for v in hand_verts if me.vertices[v].co.x > 0]; right = [v for v in hand_verts if me.vertices[v].co.x <= 0]
    results = {}
    for clip, stem in (('Grab', 'grab'), ('GrabDrop', 'grab_drop'), ('GrabCrawl', 'grab_crawl')):
        if clip not in actions: continue
        action, end, loop = actions[clip]; rig.animation_data.action = action
        if action.slots: rig.animation_data.action_slot = action.slots[0]
        standoff = PROFILE_GRAB_REACH + CAMERA_CLEARANCE
        sc.frame_set(end); update()
        target = Vector(mesh_points()[head_verts].mean(axis=0))                  # the camera looks at his face
        cam.location = Vector((0, -standoff, EYE)); cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler(); update()
        sc.render.resolution_x = PROOF_W; sc.render.resolution_y = PROOF_H; sc.render.resolution_percentage = 100
        pts = mesh_points()
        def project(co):
            v = world_to_camera_view(sc, cam, Vector(co)); return v.x * PROOF_W, (1 - v.y) * PROOF_H, v.z
        def hand_stats(idx):
            px = [project(pts[i]) for i in idx]; px = [q for q in px if q[2] > .04]
            xs = [q[0] for q in px]; ys = [q[1] for q in px]
            if not xs: return None
            cx = (min(xs) + max(xs)) / 2; cy = (min(ys) + max(ys)) / 2
            return {'x': [round(min(xs)), round(max(xs))], 'y': [round(min(ys)), round(max(ys))], 'width_px': round(max(xs) - min(xs)),
                    'centre_dist_px': round(math.hypot(cx - PROOF_W / 2, cy - PROOF_H / 2)), 'nearest_m': round(min(q[2] for q in px), 3)}
        vis = [q for q in (project(c) for c in pts[::7]) if q[2] > .04]
        hp = rig.pose.bones['Hips'].head; nk = rig.pose.bones['neck'].head
        a = project(hp); b = project(nk)
        results[clip] = {'wing_stretch_max': metrics[clip]['wing_stretch_max'], 'shard_edges_max': metrics[clip]['stretched_edges_max'], 'shard_edges_last': metrics[clip]['stretched_edges_last'],
                         'limb_angle_max_deg': metrics[clip]['limb_angle_max_deg'], 'camera': {'standoff_m': standoff, 'eye_m': EYE, 'vfov_deg': VFOV, 'look_at': [round(x, 3) for x in target]},
                         'chest_pitch_deg': round(chest_pitch_deg(), 1), 'neck_to_hips_px': round(math.hypot(a[0] - b[0], a[1] - b[1])),
                         'left_claw': hand_stats(left), 'right_claw': hand_stats(right),
                         'mesh_forward_reach_m': round(float(-pts[:, 1].min()), 3), 'claw_reach_m': round(float(-pts[hand_verts][:, 1].min()), 3),
                         'mesh_x_range_px': [round(min(q[0] for q in vis)), round(max(q[0] for q in vis))]}
        for label, frame in (('pose', end), ('frame0', 0)):
            sc.frame_set(frame); update()
            sc.render.filepath = str(directory / ('%s_%s.png' % (stem, label)))
            bpy.ops.render.render(write_still=True)
        # side-view contact sheet (orthographic, from his right): frames 0, landing/lunge, 60%, final, for reading the motion
        cam.data.type = 'ORTHO'; cam.data.ortho_scale = 5.6 if clip == 'GrabDrop' else 3.8 if clip == 'GrabCrawl' else 4.6; mid = 2.25 if clip == 'GrabDrop' else 1.0 if clip == 'GrabCrawl' else 1.6; cam.location = Vector((-9, -.6, mid)); cam.rotation_euler = (Vector((0, -.6, mid)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
        sc.render.resolution_x = 700; sc.render.resolution_y = 540
        tiles = []
        for fr in ((0.0, DROP_LAND, .6, .8, 1.0) if clip == 'GrabDrop' else (0.0, .34, .52, .8, 1.0) if clip == 'GrabCrawl' else (0.0, .25, .5, .75, 1.0)):
            sc.frame_set(int(round(end * fr))); update(); path = str(directory / '_tile.png'); sc.render.filepath = path; bpy.ops.render.render(write_still=True)
            im = bpy.data.images.load(path); arr = np.empty(im.size[0] * im.size[1] * 4, dtype=np.float32); im.pixels.foreach_get(arr); tiles.append(arr.reshape(im.size[1], im.size[0], 4)); bpy.data.images.remove(im)
        sheet = np.concatenate(tiles, axis=1); out = bpy.data.images.new('sheet', sheet.shape[1], sheet.shape[0], alpha=True); out.pixels.foreach_set(sheet.reshape(-1)); out.filepath_raw = str(directory / (stem + '_side.png')); out.file_format = 'PNG'; out.save()
        (directory / '_tile.png').unlink(missing_ok=True)
        cam.data.type = 'PERSP'
    results['mode7_crawl_baseline'] = {'shard_edges_max': metrics['Crawl']['stretched_edges_max'], 'limb_angle_max_deg': metrics['Crawl']['limb_angle_max_deg'], 'shard_edge_budget_115pct': round(metrics['Crawl']['stretched_edges_max'] * 1.15)}
    (directory / 'grab_proof_measurements.json').write_text(json.dumps(results, indent=2) + '\n')
    print('HC_PROOF', json.dumps(results), flush=True)

# ---------------------------------------------------------------- wave L proof: fixed grab camera, per-frame skull / claw framing
GRAB_VARIANT_STANDOFF = 1.615 + CAMERA_CLEARANCE      # same stand-off as GrabDrop (reach 1.615 + 0.6); the variants' own reach is below 1.615
def proof_variants(directory):
    directory.mkdir(parents=True, exist_ok=True)
    sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'
    sc.display.shading.light = 'STUDIO'; sc.display.shading.color_type = 'TEXTURE'; sc.display.render_aa = '8'
    sc.render.image_settings.file_format = 'PNG'; sc.render.image_settings.color_mode = 'RGB'; sc.render.image_settings.compression = 100; sc.render.dither_intensity = 0
    sc.world = bpy.data.worlds.new('proofv'); sc.world.color = (.03, .035, .05)
    cam = bpy.data.objects.new('proofvcam', bpy.data.cameras.new('proofvcam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.sensor_fit = 'VERTICAL'; cam.data.angle = D(VFOV); cam.data.clip_start = .04; cam.data.clip_end = 75
    head_groups = [g.index for g in body.vertex_groups if g.name in ('Head', 'Jaw')]
    head_verts = [v.index for v in me.vertices if sum(g.weight for g in v.groups if g.group in head_groups) > .5]
    left = [v for v in hand_verts if me.vertices[v].co.x > 0]; right = [v for v in hand_verts if me.vertices[v].co.x <= 0]
    tan_v = math.tan(D(VFOV / 2)); aspect = PROOF_W / PROOF_H
    results = {}
    for clip, stem in (('GrabSideL', 'grab_side_l'), ('GrabSideR', 'grab_side_r'), ('GrabDropSpin', 'grab_drop_spin')):
        if clip not in actions: continue
        action, end, loop = actions[clip]; rig.animation_data.action = action
        if action.slots: rig.animation_data.action_slot = action.slots[0]
        sc.frame_set(end); update(); target = Vector(mesh_points()[head_verts].mean(axis=0))
        cam.location = Vector((0, -GRAB_VARIANT_STANDOFF, EYE)); cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler(); update()
        inv = np.array(cam.matrix_world.inverted()); sc.render.resolution_x = PROOF_W; sc.render.resolution_y = PROOF_H; sc.render.resolution_percentage = 100
        def project(pts):
            h = np.concatenate([pts, np.ones((len(pts), 1), dtype=np.float32)], axis=1) @ inv.T; depth = -h[:, 2]
            ok = depth > .04; d = np.where(ok, depth, 1.0)
            return PROOF_W / 2 * (1 + (h[:, 0] / d) / (tan_v * aspect)), PROOF_H / 2 * (1 - (h[:, 1] / d) / tan_v), depth, ok
        def span(px, py, depth, ok, idx):
            x, y, dep, okk = px[idx], py[idx], depth[idx], ok[idx]; x, y = x[okk], y[okk]
            if not len(x): return None
            x0, x1 = float(np.clip(x.min(), 0, PROOF_W)), float(np.clip(x.max(), 0, PROOF_W))
            return {'x0': round(x0), 'x1': round(x1), 'width_px': round(x1 - x0), 'cx': round(float(x.mean())), 'cy': round(float(y.mean())), 'nearest_m': round(float(dep[okk].min()), 3)}
        frames = []; seconds = end / FPS
        for fr in range(end + 1):
            sc.frame_set(fr); update(); pts = mesh_points(); px, py, depth, ok = project(pts)
            skull = span(px, py, depth, ok, head_verts); lc = span(px, py, depth, ok, left); rc = span(px, py, depth, ok, right)
            sk_in = bool(skull and .2 * PROOF_W <= skull['cx'] <= .8 * PROOF_W and .2 * PROOF_H <= skull['cy'] <= .8 * PROOF_H)
            claw_w = max([c['width_px'] for c in (lc, rc) if c] or [0])
            frames.append({'t': round(fr / FPS, 3), 'skull_cx': skull and skull['cx'], 'skull_cy': skull and skull['cy'], 'skull_in_central60': sk_in, 'claw_px': claw_w,
                           'claw_reach_m': metrics[clip]['claw_reach_by_frame_m'][fr], 'mesh_reach_m': metrics[clip]['mesh_reach_by_frame_m'][fr]})
        first_claw = next((f['t'] for f in frames if f['claw_px'] >= 200), None); first_skull = next((f['t'] for f in frames if f['skull_in_central60']), None)
        t30 = frames[min(end, round(.3 * FPS))]; lock = SPIN_END_S if clip == 'GrabDropSpin' else 0.0; tl = frames[min(end, round((lock + .3) * FPS))]
        results[clip] = {'seconds': round(seconds, 3), 'camera': {'standoff_m': GRAB_VARIANT_STANDOFF, 'eye_m': EYE, 'vfov_deg': VFOV, 'look_at': [round(x, 3) for x in target]},
                         'first_claw_ge200px_s': first_claw, 'first_skull_central60_s': first_skull, 'at_plus_0.3s': t30, 'camera_lock_s': lock, 'at_lock_plus_0.3s': tl,
                         'final': frames[-1], 'frames': frames, 'wing_stretch_max': metrics[clip]['wing_stretch_max'], 'shard_edges_max': metrics[clip]['stretched_edges_max'],
                         'limb_angle_max_deg': metrics[clip]['limb_angle_max_deg'], 'chest_pitch_deg_last': metrics[clip]['chest_pitch_deg_last']}
        shots = [('pose', end), ('frame0', 0), ('t030', round(.3 * FPS))] + ([('lock_t030', round((SPIN_END_S + .3) * FPS))] if clip == 'GrabDropSpin' else [])
        for label, frame in shots:
            sc.frame_set(frame); update(); sc.render.filepath = str(directory / ('%s_%s.png' % (stem, label))); bpy.ops.render.render(write_still=True)
        # contact sheet from the player's camera at six moments (small): shows the approach / spin
        sc.render.resolution_x = 640; sc.render.resolution_y = 360; tiles = []
        for fr in sorted({0, round(.15 * end), round(.3 * end), round(.5 * end), round(.7 * end), end}):
            sc.frame_set(fr); update(); path = str(directory / ('_tile%d.png' % fr)); sc.render.filepath = path; bpy.ops.render.render(write_still=True)
            im = bpy.data.images.load(path); im.reload(); arr = np.empty(im.size[0] * im.size[1] * 4, dtype=np.float32); im.pixels.foreach_get(arr); tiles.append(arr.reshape(im.size[1], im.size[0], 4)); bpy.data.images.remove(im); Path(path).unlink(missing_ok=True)
        sheet = np.concatenate([np.concatenate(tiles[3:6], axis=1), np.concatenate(tiles[:3], axis=1)], axis=0)    # Blender image rows run bottom-up: the first array row is the bottom of the PNG
        out = bpy.data.images.new('sheetv', sheet.shape[1], sheet.shape[0], alpha=True); out.pixels.foreach_set(sheet.reshape(-1)); out.filepath_raw = str(directory / (stem + '_sheet.png')); out.file_format = 'PNG'; out.save()
    (directory / 'grab_variants_measurements.json').write_text(json.dumps(results, indent=1) + '\n')
    print('HC_PROOF_VARIANTS', json.dumps({k: {x: v[x] for x in ('first_claw_ge200px_s', 'first_skull_central60_s', 'at_plus_0.3s', 'at_lock_plus_0.3s', 'final')} for k, v in results.items()}), flush=True)

if PREVIEW:
    preview(PREVIEW)
    for b in rig.pose.bones: b.rotation_quaternion = Quaternion(); b.location = (0, 0, 0); b.scale = (1, 1, 1)
    bpy.ops.wm.save_as_mainfile(filepath=str(PREVIEW / 'count.blend'))

# ---------------------------------------------------------------- export (mirrors tools/prepare_splitgrin.py)
rig.animation_data.action = None
for b in rig.pose.bones: b.rotation_quaternion = Quaternion(); b.location = (0, 0, 0); b.scale = (1, 1, 1)
update()
if DIAG: sys.exit(0)
if PROOF:
    proof(PROOF)
    if any(n in actions for n in VARIANT_NAMES): proof_variants(PROOF)
rig.animation_data.action = None
for b in rig.pose.bones: b.rotation_quaternion = Quaternion(); b.location = (0, 0, 0); b.scale = (1, 1, 1)
update()
if not ONLY_NEW:
    select_only(rig, body)
    bpy.ops.export_scene.fbx(filepath=str(OUT / 'Native/CountClaudeBody.fbx'), use_selection=True, object_types={'ARMATURE', 'MESH'},
                             add_leaf_bones=False, bake_anim=False, axis_forward='-Z', axis_up='Y', path_mode='STRIP')
select_only(rig)
known = {c[0] for c in CLIPS}
for old in (OUT / 'Native').glob('CountClaude*.fbx'):
    if old.stem[len('CountClaude'):] not in known and old.stem != 'CountClaudeBody':
        old.unlink(); Path(str(old) + '.meta').unlink(missing_ok=True)
for name, (action, end, loop) in actions.items():
    if ONLY_NEW and name not in ONLY_NAMES: continue
    rig.animation_data.action = action
    if action.slots: rig.animation_data.action_slot = action.slots[0]
    scene.frame_start = 0; scene.frame_end = end
    bpy.ops.export_scene.fbx(filepath=str(OUT / ('Native/CountClaude%s.fbx' % name)), use_selection=True, object_types={'ARMATURE'},
                             add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
                             bake_anim_simplify_factor=0, axis_forward='-Z', axis_up='Y', path_mode='STRIP')
MODE_OF = {c[0]: i for i, c in enumerate(CLIPS)}
if ONLY_NEW and RECEIPT.exists():
    report = json.loads(RECEIPT.read_text())
else:
    report = {'source': str(SOURCE.relative_to(ROOT)), 'head_top_m': HEAD_TOP, 'mesh_height_m': round(hi - lo, 4), 'triangles': triangles,
              'vertices': len(me.vertices), 'bones': len(arm.bones), 'wing_vertices': wing_verts, 'membrane_faces': membrane_faces, 'flesh_faces': flesh_faces, 'body_tris': sum(len(pl.vertices) - 2 for pl in me.polygons if pl.material_index == 0),
              'membrane_tris': sum(len(pl.vertices) - 2 for pl in me.polygons if pl.material_index == 1),
              'flesh_tris': sum(len(pl.vertices) - 2 for pl in me.polygons if pl.material_index == 2), 'rest_membrane_m2': round(REST_MEMBRANE, 3),
              'rest_span_m': round(float(rest_pts[:, 0].max() - rest_pts[:, 0].min()), 3), 'shoulder_width_m': round(shoulder_width, 3)}
    report['grab_reach_m'] = metrics['Grab']['forward_reach_m_last']
if 'Grab' in (ONLY_NAMES or ['Grab']): report['grab_reach_m'] = metrics['Grab']['forward_reach_m_last']
report['clips'] = {k: metrics[k] for k in sorted(metrics, key=lambda n: MODE_OF.get(n, 99))}
# F5: forward reach of the furthest claw (hand-weighted vertices) from the origin at the final pose, and the Animator Mode of every clip.
report['grab_drop_reach_m'] = metrics['GrabDrop']['claw_reach_m_last']; report['grab_crawl_reach_m'] = metrics['GrabCrawl']['claw_reach_m_last']
report['grab_drop_mesh_reach_m'] = metrics['GrabDrop']['forward_reach_m_last']; report['grab_crawl_mesh_reach_m'] = metrics['GrabCrawl']['forward_reach_m_last']
# Wave L variants: reach, duration and the camera spin schedule, for the runtime (F2 picks the mode; the stand-off is reach + CameraClearance).
VARIANT_INFO = {'GrabSideL': 'from the player\'s left flank (clip -X)', 'GrabSideR': 'from the player\'s right flank (clip +X)', 'GrabDropSpin': 'ceiling drop behind the player, camera spun into his face'}
report['grab_variants'] = [{'name': n, 'mode': MODE_OF[n], 'seconds': metrics[n]['seconds'], 'claw_reach_m': metrics[n]['claw_reach_m_last'], 'mesh_reach_m': metrics[n]['forward_reach_m_last'],
                            'top_m_max': metrics[n]['top_m_max'], 'note': VARIANT_INFO[n]} for n in VARIANT_NAMES if n in metrics]
report['grab_side_reach_m'] = max(metrics[n]['claw_reach_m_last'] for n in ('GrabSideL', 'GrabSideR') if n in metrics)
report['grab_spin_reach_m'] = metrics['GrabDropSpin']['claw_reach_m_last']
report['grab_spin_yaw_deg'] = SPIN_YAW_DEG; report['grab_spin_start_s'] = SPIN_START_S; report['grab_spin_end_s'] = SPIN_END_S; report['grab_side_start_x_m'] = SIDE_START_X
report['modes'] = [MODE_OF[c[0]] for c in CLIPS]
report['mode_names'] = {str(MODE_OF[c[0]]): c[0] for c in CLIPS}
RECEIPT.write_text(json.dumps(report, indent=2) + '\n')
# Shipped in Resources so the player can detect Mode 8/9 (the receipt itself stays outside Resources).
RUNTIME_MODES.parent.mkdir(parents=True, exist_ok=True)
RUNTIME_MODES.write_text(json.dumps({k: report[k] for k in ('modes', 'mode_names', 'grab_reach_m', 'grab_drop_reach_m', 'grab_crawl_reach_m', 'grab_drop_mesh_reach_m', 'grab_crawl_mesh_reach_m', 'grab_variants', 'grab_side_reach_m', 'grab_spin_reach_m', 'grab_spin_yaw_deg', 'grab_spin_start_s', 'grab_spin_end_s', 'grab_side_start_x_m') if k in report}, indent=2) + '\n')
print('HC_BLENDER_EXPORT_PASS CountClaude', json.dumps(report), flush=True)
