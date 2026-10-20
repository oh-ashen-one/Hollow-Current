"""Print the Unity model-space bounds of Resources/Castle/<name>.fbx (Blender re-import, then (-x, z, -y), as CastleKitAxis measures them).

usage: Blender -b -P tools/tripo_castle_measure.py -- Name1 Name2 ...   -> one line per asset: HC_MEASURE name x0 x1 y0 y1 z0 z1 tris
"""
import bpy, sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
names = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
for name in names:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    p = ROOT / 'Assets/HollowCurrent/Resources/Castle' / (name + '.fbx')
    if not p.exists(): print('HC_MEASURE', name, 'missing'); continue
    bpy.ops.import_scene.fbx(filepath=str(p))
    lo = [1e9] * 3; hi = [-1e9] * 3; tris = 0
    for o in bpy.context.scene.objects:
        if o.type != 'MESH': continue
        tris += sum(len(f.vertices) - 2 for f in o.data.polygons)
        for v in o.data.vertices:
            w = o.matrix_world @ v.co; u = (-w.x, w.z, -w.y)
            for i in range(3): lo[i] = min(lo[i], u[i]); hi[i] = max(hi[i], u[i])
    print('HC_MEASURE %s %.3f %.3f %.3f %.3f %.3f %.3f %d' % (name, lo[0], hi[0], lo[1], hi[1], lo[2], hi[2], tris), flush=True)
