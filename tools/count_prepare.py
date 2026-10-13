"""Prepare Count Claude for Unity. CPU-only headless Blender, no network, no UI.

    /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/count_prepare.py [-- --preview DIR]

Input : tools/count_source/CountClaude_rigged.glb  (Higgsfield multi_image_to_3d -> 3d_rigging,
        see docs/haunted-castle/ASSET_LOG.md for job ids). The auto-rig has 24 humanoid bones, no wing bones, and
        wing vertices weighted to the leg bones.
Output: Assets/HollowCurrent/Art/Characters/CountClaude/{Native/*.fbx,Maps/Albedo.png,Native/CountClaude.json}

Round 2 rig (docs/haunted-castle/rounds/r1/CRITIC.md, S6 "lower wing fingers bend horizontally"):
  * The old WingTip bone pivoted at waist height (z 0.95), so any wing roll swung the lower membrane sideways.
  * Each wing is now a FAN: a root bone at the shoulder blade (WingL/WingR) and five fan bones (WingFanL0..4) that
    share that pivot and point up (hook) .. down (lowest finger) inside the folded wing's own plane. Membrane vertices
    are weighted by their angle around the pivot, so opening the fan swings the hook out-up and the fingers out-down
    as one sheet: wings fold at the back (Perch) and spread (Glide/Dive) without a hinge at the waist.
  * Zero-weight marker bones (WingFanL0End..) sit at each fan sector's outer radius so the runtime can measure the
    posed silhouette from bone positions (camera clearance, capture framing) without baking the skinned mesh.
Clips (Animator Mode ints): 0 Perch (gargoyle crouch on a rail, hands on the rail, wings half-open behind),
1 Glide (pitched forward, wings spread and beating), 2 Search (landed upright prowl, head scanning),
3 Dive (looping stoop: body steep, wings swept back, claws reaching), 4 Stagger, 5 Death, 6 Grab (wing-wrap).
Contact poses use an analytic two-bone IK on the posed skeleton (feet and hands planted), and every clip is checked
against the evaluated skinned mesh (floor contact, wing span, forward reach) with the numbers written to the receipt.

Idempotent: always starts from the pristine source GLB. Metres, Z up in Blender, character faces -Y in Blender
(glTF +Z) and exports with axis_forward=-Z / axis_up=Y exactly like tools/prepare_splitgrin.py, so he faces +Z
in Unity. Head top = 2.15 m, feet at z=0, hips above the origin in x/y.
"""
from pathlib import Path
import bpy, bmesh, json, math, sys
import numpy as np
from mathutils import Vector, Matrix, Quaternion, Euler

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'tools/count_source/CountClaude_rigged.glb'
OUT = ROOT / 'Assets/HollowCurrent/Art/Characters/CountClaude'
HEAD_TOP = 2.15
FPS = 30
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
PREVIEW = Path(argv[argv.index('--preview') + 1]) if '--preview' in argv else None
(OUT / 'Native').mkdir(parents=True, exist_ok=True)
(OUT / 'Maps').mkdir(parents=True, exist_ok=True)
bpy.context.preferences.filepaths.save_version = 0
D = math.radians

# ---------------------------------------------------------------- import and unit conversion
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(SOURCE))
objs = list(bpy.context.scene.objects)
body = next(o for o in objs if o.type == 'MESH' and o.name == 'char1')
rig = next(o for o in objs if o.type == 'ARMATURE')
for o in objs:
    if o not in (body, rig):
        bpy.data.objects.remove(o, do_unlink=True)   # stray 80-tri icosphere from the rigger
body.name = 'CountClaudeBody'; rig.name = 'CountClaudeRig'
def select_only(*items):
    bpy.ops.object.select_all(action='DESELECT')
    for o in items: o.select_set(True)
    bpy.context.view_layer.objects.active = items[0]
select_only(body); bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM')
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
select_only(rig); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
body.parent = rig; body.matrix_parent_inverse = Matrix.Identity(4)
for m in body.modifiers:
    if m.type == 'ARMATURE': m.object = rig
arm = rig.data
assert abs(arm.bones['head_end'].head_local.z - 1.648) < .05, 'unexpected rig scale %s' % arm.bones['head_end'].head_local.z
me = body.data

# ---------------------------------------------------------------- scale to 2.15 m head, feet at z=0, hips on x/y origin
head_top = max(v.co.z for v in me.vertices if abs(v.co.x) < .12 and v.co.y < .05)
scale = HEAD_TOP / head_top
hips = arm.bones['Hips'].head_local.copy()
offset = Vector((-hips.x * scale, -hips.y * scale, -min(v.co.z for v in me.vertices) * scale))
me.transform(Matrix.Translation(offset) @ Matrix.Scale(scale, 4)); me.update()
select_only(rig); bpy.ops.object.mode_set(mode='EDIT')
for eb in arm.edit_bones:
    eb.head = eb.head * scale + offset; eb.tail = eb.tail * scale + offset
bpy.ops.object.mode_set(mode='OBJECT')
hi = max(v.co.z for v in me.vertices); lo = min(v.co.z for v in me.vertices)
triangles = sum(len(p.vertices) - 2 for p in me.polygons)
assert triangles < 40000 and abs(lo) < .005, (triangles, lo)   # source mesh; the two-sided membrane adds faces below

# ---------------------------------------------------------------- wing fan rig (post-scale metres)
def smooth(x):
    x = max(0.0, min(1.0, x)); return x * x * (3 - 2 * x)
SIDES = (('L', 1), ('R', -1))
PIVOT = {s: Vector((sg * .30, .31, 1.72)) for s, sg in SIDES}               # shoulder-blade root of each wing
OUTWARD = {s: Vector((sg * .87, .49, 0)).normalized() for s, sg in SIDES}   # folded wing plane runs out and back
FAN_AXIS = {s: Vector((0, 0, 1)).cross(OUTWARD[s]).normalized() for s, _ in SIDES}  # +angle: up -> outward
FAN_REST = [15.0, 55.0, 95.0, 135.0, 172.0]
EXT_R = 1.4                                       # radius of each fan bone's extension child (folded tip reach)      # in-plane angle from straight up: hook .. lowest finger
FAN_OPEN = [48.0, 72.0, 95.0, 115.0, 132.0]      # fully spread: hook out-up, fingers out-down
NFAN = len(FAN_REST)
def fan_dir(side, phi):
    return (OUTWARD[side] * math.sin(D(phi)) + Vector((0, 0, 1)) * math.cos(D(phi))).normalized()
def wing_factor(co):
    ax, y, z = abs(co.x), co.y, co.z
    membrane = smooth((ax - .27) / .07) * smooth((y - .12) / .06)
    root = smooth((z - 1.36) / .10) * smooth((ax - .13) / .12) * smooth((y - .16) / .08)
    tip = smooth((y - .30) / .04) * smooth((ax - .40) / .04) * smooth((z - .30) / .04)   # lowest fingers hang to z .35
    return max(membrane * smooth((z - .40) / .05), root, tip)
def fan_angle(side, co):
    rel = co - PIVOT[side]; u = rel.dot(OUTWARD[side]); v = rel.z
    phi = math.degrees(math.atan2(u, v))
    if phi < -90: phi += 360                     # below and inboard of the pivot: nearest to the lowest finger, not the hook
    return phi, math.hypot(u, v)

# Below the shoulder blades the folded wing's inner edge is welded to the coat back. Split the mesh along that seam
# (faces classed by their mean wing factor) so the membrane can open without dragging coat triangles into shards.
SEAM_TOP = 1.25
bm = bmesh.new(); bm.from_mesh(me)
def face_is_wing(f): return sum(wing_factor(v.co) for v in f.verts) / len(f.verts) > .5
seam = [e for e in bm.edges if len(e.link_faces) == 2 and (e.verts[0].co.z + e.verts[1].co.z) / 2 < SEAM_TOP
        and face_is_wing(e.link_faces[0]) != face_is_wing(e.link_faces[1])]
bmesh.ops.split_edges(bm, edges=seam)
lower_class = {}
for v in bm.verts:
    if v.co.z < SEAM_TOP and v.link_faces: lower_class[v.index] = face_is_wing(v.link_faces[0])
bm.to_mesh(me); bm.free(); me.update()
seam_edges = len(seam)
names = {g.index: g.name for g in body.vertex_groups}
original = [{names[g.group]: g.weight for g in v.groups} for v in me.vertices]
ARMS = ('LeftArm', 'LeftForeArm', 'LeftHand', 'RightArm', 'RightForeArm', 'RightHand')
def behind_back(co):   # the rigger also put the wing roots and the collar on the arms (LeftArm .8 at y .3, .4 at the nape)
    return co.z > 1.2 and (co.y > .14 or (abs(co.x) < .15 and co.y > .10))
radii = {(s, k): [] for s, _ in SIDES for k in range(NFAN)}
skin = []; wing_verts = 0
for v, old in zip(me.vertices, original):
    wf = wing_factor(v.co)
    if v.index in lower_class: wf = 1.0 if lower_class[v.index] else 0.0
    if wf <= 0 and not behind_back(v.co):
        w = dict(old)
    elif wf <= 0:
        w = {}
        for k, val in old.items():
            key = 'Spine' if k in ARMS else k; w[key] = w.get(key, 0) + val
    else:
        side = 'L' if v.co.x > 0 else 'R'
        w = {}
        for k, val in old.items():                       # the rigger put the membrane on the legs: move it to the spine
            key = 'Spine' if k in ARMS and behind_back(v.co) else k; w[key] = w.get(key, 0) + val * (1 - wf)
        phi, r = fan_angle(side, v.co)
        rel = v.co - PIVOT[side]   # the junction with the back (inboard of the pivot, near its height) follows the root only
        inboard = smooth(-rel.dot(OUTWARD[side]) / .05) * (1 - smooth((abs(rel.z) - .08) / .12))
        rootw = max(1 - smooth(r / .16), inboard)
        p = min(max(phi, FAN_REST[0]), FAN_REST[-1])
        k = max(i for i in range(NFAN - 1) if FAN_REST[i] <= p) if p < FAN_REST[-1] else NFAN - 2
        t = (p - FAN_REST[k]) / (FAN_REST[k + 1] - FAN_REST[k])
        w['Wing' + side] = w.get('Wing' + side, 0) + wf * rootw
        # Radial split between each fan bone and its extension child (weight r / EXT_R): translating the extension
        # outward by (s - 1) * EXT_R stretches the finger linearly by s. (Blender's FBX export drops bone scale.)
        e = min(r / EXT_R, 1.0)
        for kk, share in ((k, 1 - t), (k + 1, t)):
            base = wf * (1 - rootw) * share
            w['WingFan%s%d' % (side, kk)] = w.get('WingFan%s%d' % (side, kk), 0) + base * (1 - e)
            w['WingFan%s%dExt' % (side, kk)] = w.get('WingFan%s%dExt' % (side, kk), 0) + base * e
        if wf > .5:
            wing_verts += 1; radii[(side, k if t < .5 else k + 1)].append(r)
    top = sorted(((k, val) for k, val in w.items() if val > 1e-4), key=lambda kv: -kv[1])[:4]
    total = sum(val for _, val in top); skin.append([(k, val / total) for k, val in top])

bpy.context.view_layer.objects.active = rig; select_only(rig); bpy.ops.object.mode_set(mode='EDIT')
for s, sg in SIDES:
    root = arm.edit_bones.new('Wing' + s); root.head = PIVOT[s]; root.tail = PIVOT[s] + Vector((0, .12, 0))
    root.parent = arm.edit_bones['Spine']
    for k, phi in enumerate(FAN_REST):
        fb = arm.edit_bones.new('WingFan%s%d' % (s, k)); fb.head = PIVOT[s]; fb.tail = PIVOT[s] + fan_dir(s, phi) * .25
        fb.parent = root
        rr = sorted(radii[(s, k)]); reach = rr[int(len(rr) * .97)] if rr else .5
        ext = arm.edit_bones.new('WingFan%s%dExt' % (s, k)); ext.head = PIVOT[s] + fan_dir(s, phi) * EXT_R
        ext.tail = ext.head + fan_dir(s, phi) * .05; ext.parent = fb
        end = arm.edit_bones.new('WingFan%s%dEnd' % (s, k)); end.head = PIVOT[s] + fan_dir(s, phi) * reach
        end.tail = end.head + fan_dir(s, phi) * .05; end.parent = ext; end.use_deform = False
bpy.ops.object.mode_set(mode='OBJECT')
for g in list(body.vertex_groups):
    g.remove(range(len(me.vertices)))
for b in arm.bones:
    if b.use_deform and b.name not in body.vertex_groups: body.vertex_groups.new(name=b.name)
for i, entries in enumerate(skin):
    for k, val in entries: body.vertex_groups[k].add([i], val, 'REPLACE')

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
    def l(s, bone, x=0, y=0, z=0):
        a = s.loc.get(bone, (0, 0, 0)); s.loc[bone] = (a[0] + x, a[1] + y, a[2] + z)
    def wings(s, spread, yaw=0, beat=0, pitch=0, curl=0, lag=0, stretch=1.0):
        """spread 0 = folded rest .. 1 = open fan; yaw + sweeps the wing forward, - back; beat + raises it;
        pitch + tilts its top back; curl bends the lower fingers further out; lag delays the lower fan (flap wave)."""
        for side, sg in SIDES:
            s.rq('Wing' + side, Q((0, 0, -sg), yaw) @ Q((0, -sg, 0), beat) @ Q((-1, 0, 0), pitch))
            for k in range(NFAN):
                f = k / (NFAN - 1)
                ang = lerp(FAN_REST[k], FAN_OPEN[k], spread) - FAN_REST[k] + curl * f + lag * f * f
                s.rq('WingFan%s%d' % (side, k), Quaternion(FAN_AXIS[side], D(ang)))
                if stretch != 1.0: s.l('WingFan%s%dExt' % (side, k), *(fan_dir(side, FAN_REST[k]) * ((lerp(1.0, stretch, f) - 1) * EXT_R)))
    def arms(s, swing, out=0, forearm=0, hand=0, inward=0):
        for side, sg in (('Left', 1), ('Right', -1)):
            s.r(side + 'Arm', x=swing, y=-sg * out, z=-sg * inward)
            s.r(side + 'ForeArm', x=forearm); s.r(side + 'Hand', x=hand)
    def legs(s, thigh, knee, foot=0, spread=0):
        for side, sg in (('Left', 1), ('Right', -1)):
            s.r(side + 'UpLeg', x=thigh, y=-sg * spread); s.r(side + 'Leg', x=knee); s.r(side + 'Foot', x=foot)
    def spine(s, lean, head=0, yaw=0, tilt=0, neck=None):
        for b, f in (('Spine02', .25), ('Spine01', .30), ('Spine', .30)): s.r(b, x=lean * f)
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
    for b, a in (('Spine02', 22), ('Spine01', 20), ('Spine', 14 + 1.5 * br)): p.r(b, x=a)
    p.r('neck', x=-44, z=6 * look); p.r('Head', x=-38, y=2 * look, z=10 * look)
    p.wings(.22 + .03 * br, yaw=-24, beat=36 + 2 * br, pitch=40, curl=2 * br, stretch=1.12)
def perch_ik(f, ph):
    br = math.sin(ph * 2 * math.pi)
    for side, sg in (('Left', 1), ('Right', -1)):
        two_bone(side + 'UpLeg', side + 'Leg', side + 'Foot', (sg * .30, .03, .172), (sg * 1.4, -.6, .5)); plant(side + 'Foot', sg * 16)
        two_bone(side + 'Arm', side + 'ForeArm', side + 'Hand', (sg * .22, -.32, .06 + .004 * br), (sg * 1.0, .4, .9))   # wrists at the rail's front edge, claws over it
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
        two_bone(side + 'UpLeg', side + 'Leg', side + 'Foot', (sg * .24, .12 if sg > 0 else -.08, .172), (sg * .3, -1.0, .6)); plant(side + 'Foot', sg * 10)
def dive(p, ph, f):
    a = ph * 2 * math.pi; fl = math.sin(a)
    p.r('Hips', x=52); p.l('Hips', z=.02 * fl)
    p.spine(10, neck=-36, head=-44); p.legs(18 + 3 * fl, 26 + 4 * fl, 42, spread=2)
    p.arms(-70 + 4 * fl, out=14, forearm=-6, hand=-14, inward=6)
    p.wings(.58 + .05 * fl, yaw=-38, beat=6 + 7 * fl, pitch=26, curl=-4 * math.sin(a - .8), lag=6 * math.sin(a - 1.2), stretch=1.35)
def grab(p, ph, f):
    # Round 5 climax, seen from below and in front: he rears up, looks down at the lens with his face clear, both arms
    # reach forward-down at shoulder width (claws framing the camera, never crossing the face) and the wings sweep
    # forward around the frame edges.
    a = ease(min(1, f / .45)); b = ease(max(0, min(1, (f - .2) / .6)))
    p.r('Hips', x=lerp(48, 10, a)); p.spine(lerp(10, 6, a), neck=lerp(-36, 8, a), head=lerp(-44, 14, a))
    p.legs(lerp(18, 8, a), lerp(26, 22, a), lerp(42, 30, a))
    p.arms(lerp(-70, -80, b), out=lerp(14, 8, b), forearm=lerp(-6, -2, b), hand=lerp(-14, -24, b), inward=lerp(6, -6, b))
    p.wings(lerp(.6, .92, b), yaw=lerp(-30, 22, a), beat=lerp(6, 16, a), pitch=lerp(26, 38, a), curl=lerp(0, -10, b), stretch=1.4)
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
CLIPS = [('Perch', perch, 3.2, True, perch_ik, None), ('Glide', glide, 2.0, True, None, 'skim'),
         ('Search', search, 2.6, True, search_ik, None), ('Dive', dive, 1.0, True, None, 'skim'),
         ('Stagger', stagger, 1.3, False, None, 'plant'), ('Death', death, 2.4, False, None, 'plant'),
         ('Grab', grab, 0.9, False, None, 'skim')]
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
    shift = SKIM - lowest_over(fn, end, loop, ik) if floor == 'skim' else 0.0
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
image = next(i for i in bpy.data.images if i.size[0] >= 1024)
image.filepath_raw = str(OUT / 'Maps/Albedo.png'); image.file_format = 'PNG'; image.save()

# ---------------------------------------------------------------- preview (optional, not part of the deliverable)
def preview(directory):
    directory.mkdir(parents=True, exist_ok=True)
    sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.light = 'FLAT'; sc.display.shading.color_type = 'TEXTURE'
    sc.render.resolution_x = 520; sc.render.resolution_y = 720; sc.render.image_settings.file_format = 'PNG'
    sc.world = bpy.data.worlds.new('w'); sc.world.color = (.28, .28, .32)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = 3.2
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
          'vertices': len(me.vertices), 'bones': len(arm.bones), 'wing_vertices': wing_verts, 'wing_coat_seam_edges': seam_edges,
          'rest_span_m': round(float(rest_pts[:, 0].max() - rest_pts[:, 0].min()), 3), 'shoulder_width_m': round(shoulder_width, 3),
          'grab_reach_m': metrics['Grab']['forward_reach_m_last'], 'clips': metrics}
(OUT / 'Native/CountClaude.json').write_text(json.dumps(report, indent=2) + '\n')
print('HC_BLENDER_EXPORT_PASS CountClaude', json.dumps(report), flush=True)
