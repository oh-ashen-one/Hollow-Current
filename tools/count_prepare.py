"""Prepare Count Claude for Unity. CPU-only headless Blender, no network, no UI.

    /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/count_prepare.py [-- --preview DIR]

Input : tools/count_source/CountClaude_rigged.glb  (Higgsfield multi_image_to_3d -> 3d_rigging,
        see docs/haunted-castle/ASSET_LOG.md for job ids). The auto-rig has 24 humanoid bones, no wing bones, and
        wing vertices weighted to the leg bones, so this script re-skins the wings onto four new bones
        (WingL/WingTipL/WingR/WingTipR) and authors six clips itself.
Output: Assets/HollowCurrent/Resources/Characters/CountClaude/{Native/*.fbx,Maps/Albedo.png,Native/CountClaude.json}

Idempotent: always starts from the pristine source GLB. Metres, Z up in Blender, character faces -Y in Blender
(glTF +Z) and exports with axis_forward=-Z / axis_up=Y exactly like tools/prepare_splitgrin.py, so he faces +Z
in Unity. Head top = 2.15 m, feet at z=0, hips above the origin in x/y.
"""
from pathlib import Path
import bpy, bmesh, json, math, sys
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'tools/count_source/CountClaude_rigged.glb'
OUT = ROOT / 'Assets/HollowCurrent/Resources/Characters/CountClaude'
HEAD_TOP = 2.15
FPS = 30
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
PREVIEW = Path(argv[argv.index('--preview') + 1]) if '--preview' in argv else None
(OUT / 'Native').mkdir(parents=True, exist_ok=True)
(OUT / 'Maps').mkdir(parents=True, exist_ok=True)
bpy.context.preferences.filepaths.save_version = 0

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

# ---------------------------------------------------------------- wing bones and re-skin
def smooth(x):
    x = max(0.0, min(1.0, x)); return x * x * (3 - 2 * x)
bpy.context.view_layer.objects.active = rig; select_only(rig); bpy.ops.object.mode_set(mode='EDIT')
for side, s in (('L', 1), ('R', -1)):
    root = arm.edit_bones.new('Wing' + side); root.head = (s * .14, .12, 1.36); root.tail = (s * .22, .15, 1.00); root.parent = arm.edit_bones['Spine']
    tip = arm.edit_bones.new('WingTip' + side); tip.head = (s * .32, .16, .95); tip.tail = (s * .38, .17, .35); tip.parent = root
bpy.ops.object.mode_set(mode='OBJECT')

me = body.data
names = {g.index: g.name for g in body.vertex_groups}
original = [{names[g.group]: g.weight for g in v.groups} for v in me.vertices]
for g in list(body.vertex_groups):
    g.remove(range(len(me.vertices)))
for n in ('WingL', 'WingTipL', 'WingR', 'WingTipR'):
    body.vertex_groups.new(name=n)
def wing_factor(co):
    x, y, z = abs(co.x), co.y, co.z
    xmin = .22 if z >= .75 else (.22 + (.75 - z) / .35 * .12 if z >= .40 else .34)
    return min(smooth((x - xmin) / .07), smooth(y / .06), smooth((z - .22) / .08))
wing_verts = 0
skin = []
for v, old in zip(me.vertices, original):
    wf = wing_factor(v.co); w = {k: val * (1 - wf) for k, val in old.items()}
    if wf > 0:
        wing_verts += wf > .5
        side = 'L' if v.co.x > 0 else 'R'; t = smooth((1.15 - v.co.z) / .30)
        w['Wing' + side] = wf * (1 - t); w['WingTip' + side] = wf * t
    top = sorted(((k, val) for k, val in w.items() if val > 1e-4), key=lambda kv: -kv[1])[:4]
    total = sum(val for _, val in top); skin.append([(k, val / total) for k, val in top])
for i, entries in enumerate(skin):
    for k, val in entries: body.vertex_groups[k].add([i], val, 'REPLACE')

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
assert triangles < 40000 and abs(lo) < .005, (triangles, lo)

# ---------------------------------------------------------------- clips
scene = bpy.context.scene; scene.render.fps = FPS
rig.animation_data_create()
D = math.radians
def lerp(a, b, t): return a + (b - a) * t
def ease(t): return smooth(t)
class Pose:
    def __init__(s): s.rot = {}; s.loc = {}
    def r(s, bone, x=0, y=0, z=0):
        a = s.rot.get(bone, (0, 0, 0)); s.rot[bone] = (a[0] + x, a[1] + y, a[2] + z)
    def l(s, bone, x=0, y=0, z=0):
        a = s.loc.get(bone, (0, 0, 0)); s.loc[bone] = (a[0] + x, a[1] + y, a[2] + z)
    # armature-space axes: X character-left, Y back, Z up. +X rot: down-pointing limbs swing back, up-pointing lean forward.
    def wings(s, roll, tip, yaw, pitch=0):
        # roll: pendulum outward (deg); tip: extra outward bend of outer wing; yaw: + sweeps outer edge back, - forward
        for side, sg in (('L', 1), ('R', -1)):
            s.r('Wing' + side, x=D(pitch), y=-sg * D(roll), z=sg * D(yaw))
            s.r('WingTip' + side, x=0, y=-sg * D(tip), z=sg * D(yaw) * .5)
    def arms(s, swing, out=0, forearm=0, hand=0, inward=0):
        for side, sg in (('Left', 1), ('Right', -1)):
            s.r(side + 'Arm', x=D(swing), y=-sg * D(out), z=-sg * D(inward))
            s.r(side + 'ForeArm', x=D(forearm)); s.r(side + 'Hand', x=D(hand))
    def legs(s, thigh, knee, foot=0):
        for side in ('Left', 'Right'):
            s.r(side + 'UpLeg', x=D(thigh)); s.r(side + 'Leg', x=D(knee)); s.r(side + 'Foot', x=D(foot))
    def spine(s, lean, head=0, yaw=0, tilt=0):
        # lean distributed over the four spine bones; head counter-rotates so the gaze stays level
        for b, f in (('Spine02', .25), ('Spine01', .30), ('Spine', .30)): s.r(b, x=D(lean * f))
        s.r('neck', x=D(-lean * .15 + head * .5), z=D(yaw * .4)); s.r('Head', x=D(-lean * .25 + head * .5), y=D(tilt), z=D(yaw * .6))

def perch(p, ph, f):
    br = math.sin(ph * 2 * math.pi); look = math.tanh(2.2 * math.sin(ph * 2 * math.pi + .6))
    p.spine(2 + 1.2 * br, head=3, yaw=22 * look, tilt=3 * look)
    p.arms(5, forearm=-14 + 3 * br, hand=8, out=-2); p.wings(-6 + 1.5 * br, -4, 10 + 2 * br, pitch=1.5 * br)
    p.l('Hips', z=.008 * br)
def glide(p, ph, f):
    a = ph * 2 * math.pi; flap = math.sin(a)
    p.spine(12 + 2 * flap, head=4, yaw=4 * math.sin(a * .5)); p.legs(16 + 4 * flap, 22 + 6 * flap, 25)
    p.arms(28, out=8, forearm=14, hand=16, inward=-3); p.wings(26 + 10 * flap, 5 + 8 * math.sin(a - .9), 8 - 3 * flap)
    p.l('Hips', z=.07 + .035 * math.sin(a - .4), y=.02 * flap)
def search(p, ph, f):
    a = ph * 2 * math.pi; look = math.tanh(3.0 * math.sin(a))
    p.spine(7, head=6, yaw=62 * look, tilt=5 * look); p.legs(9, 14, 18)
    p.arms(14, out=6, forearm=22, hand=18); p.wings(17 + 5 * math.sin(a * 2), 6 + 3 * math.sin(a * 2 - .8), 10)
    p.l('Hips', z=.04 + .018 * math.sin(a * 2))
def dive(p, ph, f):
    # f in 0..1, not a loop: stoop and sweep wings back, then snap arms forward and wing-wrap
    a = ease(min(1, f / .30)); b = ease(max(0, min(1, (f - .30) / .28)))
    p.spine(lerp(0, 32, a) + 6 * b, head=-4 * b); p.legs(lerp(0, 24, a), lerp(0, 32, a), 30 * a)
    p.arms(lerp(0, -62, a) + lerp(0, -30, b), out=lerp(0, 10, a) - 10 * b, forearm=lerp(0, -10, a) - 8 * b, hand=-18 * b, inward=lerp(0, 4, a) + 10 * b)
    p.wings(lerp(0, 8, a) - 18 * b, lerp(0, 6, a) - 10 * b, lerp(0, 38, a) - 82 * b, pitch=-6 * b)
    p.l('Hips', z=-.10 * a, y=-.16 * b)
def stagger(p, ph, f):
    a = ease(min(1, f / .16)); rec = ease(max(0, min(1, (f - .30) / .70))); sh = math.sin(f * 38) * (1 - f) ** 2
    p.spine(lerp(-26, 14, rec) * a, head=lerp(-12, 6, rec) * a, yaw=7 * sh); p.legs(lerp(-10, 16, rec) * a, lerp(14, 38, rec) * a, 12 * a)
    p.arms(lerp(-26, 20, rec) * a, out=lerp(48, 14, rec) * a, forearm=lerp(30, 40, rec) * a, hand=22 * a)
    p.wings(lerp(42, 16, rec) * a + 4 * sh, lerp(12, 6, rec) * a + 6 * sh, lerp(-10, 18, rec) * a)
    p.l('Hips', z=-.14 * a * (1 - rec * .5), y=.16 * a * (1 - rec))
def death(p, ph, f):
    a = ease(min(1, f / .42)); b = ease(max(0, min(1, (f - .30) / .55)))
    p.spine(lerp(0, 28, a) + 22 * b, head=14 * a + 10 * b, yaw=16 * b, tilt=8 * b)
    p.legs(-84 * a, 98 * a, 6 * a); p.l('Hips', z=-.52 * a - .04 * b, y=-.32 * a - .20 * b)
    p.arms(lerp(0, 18, a) + 12 * b, out=14 * a, forearm=22 * a, hand=26 * a)
    p.wings(lerp(0, 30, a) - 34 * b, lerp(0, 10, a) - 14 * b, lerp(0, 10, a) + 14 * b, pitch=-8 * b)
CLIPS = [('Perch', perch, 3.2, True), ('Glide', glide, 1.5, True), ('Search', search, 2.6, True),
         ('Dive', dive, 1.1, False), ('Stagger', stagger, 1.7, False), ('Death', death, 2.4, False)]

def pose_to_bones(pose):
    for b in rig.pose.bones:
        b.rotation_mode = 'XYZ'; b.rotation_euler = (0, 0, 0); b.location = (0, 0, 0)
    for name, (x, y, z) in pose.rot.items():
        pb = rig.pose.bones[name]; rest = arm.bones[name].matrix_local.to_quaternion()
        delta = (Matrix.Rotation(z, 3, 'Z') @ Matrix.Rotation(y, 3, 'Y') @ Matrix.Rotation(x, 3, 'X')).to_quaternion()
        pb.rotation_euler = (rest.inverted() @ delta @ rest).to_euler('XYZ')
    for name, v in pose.loc.items():
        rest = arm.bones[name].matrix_local.to_quaternion(); rig.pose.bones[name].location = rest.inverted() @ Vector(v)

actions = {}
for name, fn, seconds, loop in CLIPS:
    action = bpy.data.actions.new(name); rig.animation_data.action = action
    end = round(seconds * FPS)
    for frame in range(0, end + 1):
        pose = Pose(); fn(pose, (frame / end) % 1.0 if loop else frame / end, frame / end)
        if loop and frame == end: pose = Pose(); fn(pose, 0.0, 0.0)
        pose_to_bones(pose)
        for b in rig.pose.bones:
            b.keyframe_insert('rotation_euler', frame=frame, group=b.name); b.keyframe_insert('location', frame=frame, group=b.name)
    actions[name] = (action, end, loop)
rig.animation_data.action = None
for b in rig.pose.bones: b.rotation_euler = (0, 0, 0); b.location = (0, 0, 0)

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
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = 3.0
    views = {'front': (0, -8, 1.15), 'side': (-8, 0, 1.15), 'back': (0, 8, 1.15)}
    rig.animation_data.action = None
    shots = [('rest', None, None)] + [(n, n, f) for n in actions for f in (0.0, .25, .5, .75, 1.0)]
    for label, clip, frac in shots:
        if clip:
            action, end, loop = actions[clip]; rig.animation_data.action = action
            if action.slots: rig.animation_data.action_slot = action.slots[0]
            sc.frame_set(int(round(end * frac)))
        else:
            for b in rig.pose.bones: b.rotation_euler = (0, 0, 0); b.location = (0, 0, 0)
        bpy.context.view_layer.update()
        for vname in (('front', 'side') if clip else ('front', 'side', 'back')):
            loc = Vector(views[vname]); cam.location = loc
            cam.rotation_euler = (Vector((0, 0, 1.15)) - loc).to_track_quat('-Z', 'Y').to_euler()
            sc.render.filepath = str(directory / ('%s_%s_%s.png' % (label, ('%03d' % int(frac * 100)) if clip else 'rest', vname)))
            bpy.ops.render.render(write_still=True)
    rig.animation_data.action = None
if PREVIEW:
    preview(PREVIEW)
    for b in rig.pose.bones: b.rotation_euler = (0, 0, 0); b.location = (0, 0, 0)
    bpy.ops.wm.save_as_mainfile(filepath=str(PREVIEW / 'count.blend'))

# ---------------------------------------------------------------- export (mirrors tools/prepare_splitgrin.py)
select_only(rig, body)
bpy.ops.export_scene.fbx(filepath=str(OUT / 'Native/CountClaudeBody.fbx'), use_selection=True, object_types={'ARMATURE', 'MESH'},
                         add_leaf_bones=False, bake_anim=False, axis_forward='-Z', axis_up='Y', path_mode='STRIP')
select_only(rig)
for name, (action, end, loop) in actions.items():
    rig.animation_data.action = action
    if action.slots: rig.animation_data.action_slot = action.slots[0]
    scene.frame_start = 0; scene.frame_end = end
    bpy.ops.export_scene.fbx(filepath=str(OUT / ('Native/CountClaude%s.fbx' % name)), use_selection=True, object_types={'ARMATURE'},
                             add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
                             bake_anim_simplify_factor=0, axis_forward='-Z', axis_up='Y', path_mode='STRIP')
report = {'source': str(SOURCE.relative_to(ROOT)), 'head_top_m': HEAD_TOP, 'mesh_height_m': round(hi - lo, 4), 'triangles': triangles,
          'vertices': len(me.vertices), 'bones': len(arm.bones), 'wing_vertices': wing_verts,
          'clips': {n: {'seconds': round(e / FPS, 3), 'loop': l} for n, (_, e, l) in actions.items()}}
(OUT / 'Native/CountClaude.json').write_text(json.dumps(report, indent=2) + '\n')
print('HC_BLENDER_EXPORT_PASS CountClaude', json.dumps(report), flush=True)
