"""Print extremes of an imported FBX:  Blender -b -P tools/props_bounds.py -- file.fbx   (Blender axes after import: X, Y length, Z up)"""
import bpy, sys
from mathutils import Vector
f = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=f)
vs = [(o.matrix_world @ v.co) for o in bpy.context.scene.objects if o.type == "MESH" for v in o.data.vertices]
for ax, n in enumerate("XYZ"):
    lo = min(vs, key=lambda v: v[ax]); hi = max(vs, key=lambda v: v[ax])
    print("HC_BOUNDS", n, "min %.3f at (%.2f %.2f %.2f)" % (lo[ax], *lo), "max %.3f at (%.2f %.2f %.2f)" % (hi[ax], *hi))
