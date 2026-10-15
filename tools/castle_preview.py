#!/usr/bin/env python3
"""Headless contact-sheet render of Resources/Castle/*.fbx (Blender workbench, CPU-light, no game engine).

    Blender -b -P tools/castle_preview.py -- <out.png> [Name ...]
One tile per asset (3/4 view, front = +Y faces the lower-right), assembled with PIL (system python not needed:
Blender's bundled Pillow is used when present, otherwise tiles are left as <out>_<Name>.png).
"""
import bpy, sys, math
from pathlib import Path
from mathutils import Vector
sys.path.insert(0, str(Path(__file__).resolve().parent))
import castle_common as cc
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
out = Path(args[0] if args else "/tmp/castle_preview.png")
names = args[1:]
files = sorted(cc.CASTLE.glob("*.fbx"))
if names: files = [f for f in files if f.stem in names]
import os
TILE = int(os.environ.get('HC_TILE', 480))
tiles = []
for f in files:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(f))
    sc = bpy.context.scene
    objs = [o for o in sc.objects if o.type == 'MESH']
    for o in objs:
        for m in o.data.materials:
            if m and m.use_nodes:
                b = next((n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
                if b: m.diffuse_color = b.inputs["Base Color"].default_value
    mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
    for o in objs:
        for v in o.data.vertices:
            w = o.matrix_world @ v.co
            mn = Vector(min(mn[k], w[k]) for k in range(3)); mx = Vector(max(mx[k], w[k]) for k in range(3))
    ctr = (mn + mx) / 2; ext = max(mx - mn)
    sc.render.engine = 'BLENDER_WORKBENCH'
    sc.display.shading.light = 'STUDIO'; sc.display.shading.color_type = 'MATERIAL'
    sc.display.shading.show_shadows = True; sc.display.shading.show_cavity = True
    sc.render.resolution_x = sc.render.resolution_y = TILE
    sc.world = bpy.data.worlds.new("w"); sc.world.color = (.12, .13, .16)
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = ext * 1.25; cam.data.clip_end = 1000; cam.data.clip_start = .01
    import os
    az, el = math.radians(float(os.environ.get('HC_AZ', 35))), math.radians(float(os.environ.get('HC_EL', 22)))
    d = Vector((math.sin(az) * math.cos(el), math.cos(az) * math.cos(el), math.sin(el)))  # camera on the +Y (front) side
    cam.location = ctr + d * ext * 3
    cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
    p = out.with_name(out.stem + "_" + f.stem + ".png")
    sc.render.filepath = str(p)
    bpy.ops.render.render(write_still=True)
    tiles.append((f.stem, p))
import subprocess, json
ASSEMBLE = """
import sys, json, math
from PIL import Image, ImageDraw
out, tile, items = sys.argv[1], int(sys.argv[2]), json.loads(sys.argv[3])
import os
cols = int(os.environ.get('HC_COLS', 6)); rows = math.ceil(len(items) / cols)
sheet = Image.new("RGB", (cols * tile, rows * tile), (30, 32, 40)); dr = ImageDraw.Draw(sheet)
import os
for i, (n, p) in enumerate(items):
    im = Image.open(p).convert("RGB"); x, y = (i % cols) * tile, (i // cols) * tile
    sheet.paste(im, (x, y)); dr.text((x + 8, y + 6), n, fill=(255, 255, 255)); os.unlink(p)
sheet.save(out)
"""
subprocess.run(["/usr/bin/env", "python3", "-c", ASSEMBLE, str(out), str(TILE), json.dumps([(n, str(p)) for n, p in tiles])], check=True)
print("HC_PREVIEW", out, len(tiles))
