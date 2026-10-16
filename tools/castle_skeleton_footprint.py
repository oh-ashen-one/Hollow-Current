#!/usr/bin/env python3
"""Walkable footprint of the great-hall giant skeleton (wave FB, hc/fb-layout).

    blender -b -P tools/castle_skeleton_footprint.py -- [--scale 0.6 0.6 0.5] [--posy 0.05]

Imports Assets/HollowCurrent/Resources/Castle/GiantSkeleton.fbx, samples every triangle of the carcass (~0.04 m spacing), keeps the
points whose WORLD height (posy + art z * scale y) lies between the step offset (0.26 m) and head clearance (1.85 m) of the player
controller, bins them into 0.5 x 0.5 ART-unit cells in the plan (x lateral, y along the body, +y = head) and merges the cells into
rectangles. Prints the C# initialiser for CastleLayout.SkeletonFootprint plus an ASCII map, and the art extents for SkeletonArt*.
Bone below the step offset (loose ribs and claws on the floor) and above the head (the gallery slab is at 3.0 m) does not block.
Re-run it whenever SkeletonScaleVec or the art changes; the scale below must equal CastleLayout.SkeletonScaleVec.
"""
import bpy, sys, math, collections
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SX, SY, SZ = 0.6, 0.6, 0.5
POSY = 0.05
if "--scale" in args:
    i = args.index("--scale"); SX, SY, SZ = (float(v) for v in args[i + 1:i + 4])
if "--posy" in args: POSY = float(args[args.index("--posy") + 1])
STEP, HEAD = 0.26, 1.85
CELLX = CELLY = 0.5
FBX = "Assets/HollowCurrent/Resources/Castle/GiantSkeleton.fbx"
import os
root = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(root, FBX))
cells = set()
xs, ys, zs = [], [], []
for o in bpy.context.scene.objects:
    if o.type != 'MESH': continue
    me = o.data; me.calc_loop_triangles(); mw = o.matrix_world
    for v in me.vertices:
        w = mw @ v.co; xs.append(w.x); ys.append(w.y); zs.append(w.z)
    for tri in me.loop_triangles:
        p = [mw @ me.vertices[i].co for i in tri.vertices]
        edge = max((p[0] - p[1]).length, (p[1] - p[2]).length, (p[0] - p[2]).length)
        k = max(1, int(edge / 0.04))
        for i in range(k + 1):
            for j in range(k + 1 - i):
                u, v_ = i / k, j / k
                q = p[0] * u + p[1] * v_ + p[2] * (1 - u - v_)
                if STEP <= POSY + q.z * SY <= HEAD:
                    cells.add((math.floor(q.x / CELLX), math.floor(q.y / CELLY)))
cols = collections.defaultdict(list)
for cx, cy in sorted(cells): cols[cx].append(cy)
runs = []
for cx, col in cols.items():
    col.sort(); s = prev = col[0]
    for y in col[1:]:
        if y != prev + 1: runs.append((cx, s, prev)); s = y
        prev = y
    runs.append((cx, s, prev))
runs.sort(key=lambda r: (r[1], r[2], r[0]))
boxes = []
for r in runs:
    if boxes and boxes[-1][2] == r[1] and boxes[-1][3] == r[2] and boxes[-1][1] == r[0] - 1: boxes[-1][1] = r[0]
    else: boxes.append([r[0], r[0], r[1], r[2]])
print("HC_SKELETON_FOOTPRINT art extents x %.3f..%.3f y %.3f..%.3f z %.3f..%.3f; scale (%.2f,%.2f,%.2f) posy %.2f; %d cells -> %d boxes" % (min(xs), max(xs), min(ys), max(ys), min(zs), max(zs), SX, SY, SZ, POSY, len(cells), len(boxes)))
print("        public static readonly Vector4[] SkeletonFootprint =\n        {")
print("            " + ", ".join("new Vector4(%.2ff, %.2ff, %.2ff, %.2ff)" % (b[0] * CELLX, (b[1] + 1) * CELLX, b[2] * CELLY, (b[3] + 1) * CELLY) for b in boxes) + ",")
print("        };")
for y in range(max(c[1] for c in cells), min(c[1] for c in cells) - 1, -1):
    print("%5.1f " % (y * CELLY) + "".join("#" if (x, y) in cells else "." for x in range(min(c[0] for c in cells), max(c[0] for c in cells) + 1)))
