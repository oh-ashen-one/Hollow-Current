#!/usr/bin/env python3
"""Offline lit proof renders for the r12 art-props families (CPU Blender, no Unity, no GPU engine).

    Blender -b -P tools/props_proof.py -- <in.fbx|glb> <out.png> [--az 30 --el 15 --dist 1.0 --res 1100x900 --samples 48
                                          --center x,y,z --albedo-dir DIR --world 0.12 --key 600 --floor 0|1 --bg 0.30]
Imports the file, swaps each material for the same-named Castle_* Principled set built from
Assets/HollowCurrent/Resources/Castle/Textures/<name>_{albedo,normal,mask}.png when those exist (so the proof shows the
texture the player gets), lights it with a warm key + cold fill + rim against a neutral grey backdrop.
"""
import bpy, sys, math, os
from pathlib import Path
from mathutils import Vector
ROOT = Path(__file__).resolve().parents[1]
TEX = ROOT / "Assets/HollowCurrent/Resources/Castle/Textures"
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
src, out = argv[0], argv[1]
opt = dict(zip(argv[2::2], argv[3::2]))
az = math.radians(float(opt.get("--az", 30))); el = math.radians(float(opt.get("--el", 15)))
dist = float(opt.get("--dist", 1.0)); res = [int(v) for v in opt.get("--res", "1100x900").split("x")]
samples = int(opt.get("--samples", 48)); bgv = float(opt.get("--bg", .30)); key = float(opt.get("--key", 700))
floor = opt.get("--floor", "0") == "1"
figure = opt.get("--figure")   # x,y: a 1.8 m grey capsule for scale
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
def load_one(spec):
    parts = spec.split("@"); path = parts[0]
    off = Vector([float(v) for v in parts[1].split(",")]) if len(parts) > 1 else Vector((0, 0, 0))
    rot = math.radians(float(parts[2])) if len(parts) > 2 else 0.0
    sc_ = float(parts[3]) if len(parts) > 3 else 1.0
    before = set(bpy.context.scene.objects)
    if path.lower().endswith((".glb", ".gltf")): bpy.ops.import_scene.gltf(filepath=path)
    else: bpy.ops.import_scene.fbx(filepath=path)
    new = [o for o in bpy.context.scene.objects if o not in before]
    from mathutils import Matrix
    M = Matrix.Translation(off) @ Matrix.Rotation(rot, 4, "Z") @ Matrix.Scale(sc_, 4)
    for o in new:
        if o.parent is None: o.matrix_world = M @ o.matrix_world
    bpy.context.view_layer.update()
for spec in src.split(";"): load_one(spec)

def img(path, nc=False):
    if not path.exists(): return None
    im = bpy.data.images.load(str(path))
    if nc: im.colorspace_settings.name = "Non-Color"
    return im

def build_mat(m):
    name = m.name.split(".")[0]
    if not name.startswith("Castle_"): return
    a, n, k = TEX / (name + "_albedo.png"), TEX / (name + "_normal.png"), TEX / (name + "_mask.png")
    if not a.exists(): return
    m.use_nodes = True; nt = m.node_tree
    b = next(x for x in nt.nodes if x.type == "BSDF_PRINCIPLED")
    t = nt.nodes.new("ShaderNodeTexImage"); t.image = img(a); nt.links.new(t.outputs["Color"], b.inputs["Base Color"])
    if n.exists():
        tn = nt.nodes.new("ShaderNodeTexImage"); tn.image = img(n, True)
        nm = nt.nodes.new("ShaderNodeNormalMap"); nm.inputs["Strength"].default_value = 1.0
        nt.links.new(tn.outputs["Color"], nm.inputs["Color"]); nt.links.new(nm.outputs["Normal"], b.inputs["Normal"])
    if k.exists():
        tk = nt.nodes.new("ShaderNodeTexImage"); tk.image = img(k, True)
        sep = nt.nodes.new("ShaderNodeSeparateColor"); nt.links.new(tk.outputs["Color"], sep.inputs["Color"])
        inv = nt.nodes.new("ShaderNodeMath"); inv.operation = "SUBTRACT"; inv.inputs[0].default_value = 1.0
        # mask pack: r = metal, a = smoothness (see castle_textures.pack_mask); use alpha as smoothness
        nt.links.new(tk.outputs["Alpha"], inv.inputs[1]); nt.links.new(inv.outputs[0], b.inputs["Roughness"])
        nt.links.new(sep.outputs["Red"], b.inputs["Metallic"])

objs = [o for o in sc.objects if o.type == "MESH"]
for o in objs:
    for m in o.data.materials:
        if m: build_mat(m)
mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
for o in objs:
    for c in o.bound_box:
        w = o.matrix_world @ Vector(c)
        mn = Vector(min(mn[i], w[i]) for i in range(3)); mx = Vector(max(mx[i], w[i]) for i in range(3))
ctr = (mn + mx) / 2
if "--center" in opt: ctr = Vector([float(v) for v in opt["--center"].split(",")])
ext = max(mx - mn)
# neutral grey backdrop: world colour + a big floor-less cyclorama is overkill; use world only (+ optional floor plane)
sc.world = bpy.data.worlds.new("w"); sc.world.use_nodes = True
bg = sc.world.node_tree.nodes["Background"]; bg.inputs["Color"].default_value = (bgv, bgv, bgv, 1); bg.inputs["Strength"].default_value = 1.0
def light(name, loc, energy, color, size=1.0):
    d = bpy.data.lights.new(name, "AREA"); d.energy = energy; d.color = color; d.size = size * ext
    o = bpy.data.objects.new(name, d); sc.collection.objects.link(o); o.location = loc
    o.rotation_euler = (ctr - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
R = ext * 2.2
light("key", ctr + Vector((R * .8, -R * .9, R * .8)), key * ext * ext, (1.0, .82, .62), .8)
light("fill", ctr + Vector((-R * 1.0, -R * .4, R * .3)), key * .35 * ext * ext, (.55, .65, 1.0), 1.2)
light("rim", ctr + Vector((-R * .3, R * 1.0, R * .6)), key * .5 * ext * ext, (.7, .75, 1.0), .6)
if floor:
    bpy.ops.mesh.primitive_plane_add(size=ext * 8, location=(ctr.x, ctr.y, mn.z)); f = bpy.context.object
    fm = bpy.data.materials.new("floor"); fm.use_nodes = True
    fm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (.32, .32, .32, 1)
    fm.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = .9
    f.data.materials.append(fm)
if figure:
    fx, fy = [float(v) for v in figure.split(",")]
    bpy.ops.mesh.primitive_cylinder_add(radius=.22, depth=1.5, location=(fx, fy, mn.z + .75)); body = bpy.context.object
    bpy.ops.mesh.primitive_uv_sphere_add(radius=.12, location=(fx, fy, mn.z + 1.66)); head = bpy.context.object
    fm2 = bpy.data.materials.new("figure"); fm2.use_nodes = True
    fm2.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (.5, .22, .12, 1)
    for o_ in (body, head): o_.data.materials.append(fm2)
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
cam.data.lens = 50; cam.data.clip_start = .01; cam.data.clip_end = 1000
d = Vector((math.sin(az) * math.cos(el), -math.cos(az) * math.cos(el), math.sin(el)))
cam.location = ctr + d * ext * 2.4 * dist
cam.rotation_euler = (ctr - cam.location).to_track_quat("-Z", "Y").to_euler()
sc.render.engine = "CYCLES"; sc.cycles.device = "CPU"; sc.cycles.samples = samples; sc.cycles.use_denoising = True
sc.render.resolution_x, sc.render.resolution_y = res
sc.view_settings.view_transform = "Filmic" if "Filmic" in [i.identifier for i in bpy.types.ColorManagedViewSettings.bl_rna.properties["view_transform"].enum_items] else "Standard"
sc.render.filepath = out
bpy.ops.render.render(write_still=True)
print("HC_PROOF", out, "tris", sum(len(o.data.polygons) for o in objs), "size", [round(v, 3) for v in (mx - mn)])
