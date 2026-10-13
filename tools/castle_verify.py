#!/usr/bin/env python3
"""Re-import every Resources/Castle/*.fbx in headless Blender, print dimensions and triangle counts and check them
against the generator manifests (docs/haunted-castle/art/manifest_*.json) and the LOOP.md dimension spec.

    Blender -b -P tools/castle_verify.py
Exit status is non-zero (HC_VERIFY_FAIL) if any check fails.
"""
import bpy, sys, json
from pathlib import Path
from mathutils import Vector
sys.path.insert(0, str(Path(__file__).resolve().parent))
import castle_common as cc

manifest = {}
for p in sorted((cc.ROOT / "docs/haunted-castle/art").glob("manifest_*.json")):
    manifest.update(json.loads(p.read_text()))
fails = []
rows = []

def bbox(objs):
    mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
    for o in objs:
        for v in o.data.vertices:
            w = o.matrix_world @ v.co
            mn = Vector(min(mn[k], w[k]) for k in range(3)); mx = Vector(max(mx[k], w[k]) for k in range(3))
    return mn, mx

for f in sorted(cc.CASTLE.glob("*.fbx")):
    name = f.stem
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(f))
    objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    mn, mx = bbox(objs)
    size = [round(mx[k] - mn[k], 3) for k in range(3)]
    tris = sum(len(o.data.polygons) for o in objs if all(len(p.vertices) == 3 for p in o.data.polygons)) or sum(len(o.data.loop_triangles) for o in objs)
    for o in objs: o.data.calc_loop_triangles()
    tris = sum(len(o.data.loop_triangles) for o in objs)
    mats = sorted({m.name.split(".")[0] for o in objs for m in o.data.materials if m})
    uv = all(o.data.uv_layers for o in objs)
    m = manifest.get(name)
    note = []
    if not m:
        fails.append(name + ": not in manifest")
    else:
        # FBX re-import: X stays X, Blender Y/Z come back as the exported Y(up)->Z. Compare sorted-by-axis sizes.
        if any(abs(a - b) > .01 for a, b in zip(size, m["size"])):
            fails.append("%s: size %s != manifest %s" % (name, size, m["size"]))
        if tris != m["tris"]:
            fails.append("%s: tris %d != manifest %d" % (name, tris, m["tris"]))
        if m.get("budget") and tris > m["budget"]:
            fails.append("%s: %d tris over budget %d" % (name, tris, m["budget"]))
    if not uv: fails.append(name + ": missing UVs")
    if not all(x.startswith("Castle_") for x in mats): fails.append("%s: unexpected materials %s" % (name, mats))
    byname = {o.name.split(".")[0]: o for o in objs}
    def psize(n):
        o = byname[n]; a, b = bbox([o]); return [round(b[k] - a[k], 3) for k in range(3)]
    def near(v, t, tol): return abs(v - t) <= tol
    if name.startswith("Wall_"):
        W = float(name.split("_")[1][:-1])
        if not (near(size[0], W, .005) and near(size[2], 3.2, .005)): fails.append("%s: wall dims %s" % (name, size))
        if tris >= 200: fails.append("%s: wall %d tris >= 200" % (name, tris))
    if name == "GreatDoor":
        for lf in ("LeafL", "LeafR"):
            s = psize(lf)
            if not (near(s[0], 1.49, .02) and near(s[2], 4.48, .03)): fails.append("GreatDoor %s %s" % (lf, s))
        note.append("opening 3.0x4.5")
    if name == "ServiceDoor":
        s = psize("LeafL")
        if not (near(s[0], .89, .02) and near(s[2], 1.98, .03)): fails.append("ServiceDoor leaf %s" % s)
        note.append("opening 0.9x2.0")
    if name == "Door":
        s = psize("LeafL")
        if not (near(s[0], 1.49, .02) and near(s[2], 1.98, .03)): fails.append("Door leaf %s" % s)
        note.append("opening 1.5x2.0")
    if name == "StairFlight":
        note.append("rise %.2f" % m["notes"]["rise"] if m else "")
    if name == "GiantSkeleton" and not (11.4 <= size[1] <= 12.6): fails.append("skeleton length %s" % size)
    if name == "GiantSkeleton" and tris >= 15000: fails.append("skeleton tris")
    if not name.startswith(("Giant",)) and not name.startswith("Wall_") and tris >= 3000 and name != "GiantSkeleton":
        fails.append("%s: %d tris >= 3000" % (name, tris))
    nom = cc.NOMINAL.get(name)
    if nom:
        got = (size[0], size[2], size[1])           # Unity W, H, D
        dev = [round(g - n, 2) for g, n in zip(got, nom)]
        note.append("vs layout %s dev %s" % (nom, dev))
        if any(abs(d) > max(.2, .2 * n) for d, n in zip(dev, nom)):
            fails.append("%s: size (W,H,D) %s deviates from layout nominal %s" % (name, got, nom))
    free_pivot = {"Drawbridge", "DrawbridgeChains", "RoseWindow"}
    if name not in free_pivot and not name.startswith("Wall_") and abs(mn.z) > .03 and name not in ("FloorTile", "FloatingBook", "Orrery"):
        note.append("PIVOT z_min=%.2f" % mn.z)
    rows.append((name, size, tris, len(objs), f.stat().st_size, mats, "; ".join(note)))

print("HC_VERIFY %-16s %-26s %6s %5s %9s" % ("asset", "size X x Y x Z (m)", "tris", "parts", "bytes"))
for r in rows:
    print("HC_VERIFY %-17s %-26s %6d %5d %9d  %s %s" % (r[0], "%.2f x %.2f x %.2f" % tuple(r[1]), r[2], r[3], r[4], ",".join(x[7:] for x in r[5]), r[6]))
(cc.ROOT / "docs/haunted-castle/art/verify.json").write_text(json.dumps(
    {r[0]: dict(size=r[1], tris=r[2], parts=r[3], bytes=r[4]) for r in rows}, indent=1, sort_keys=True) + "\n")
missing = [n for n in manifest if not (cc.CASTLE / (n + ".fbx")).exists()]
fails += ["missing fbx " + n for n in missing]
print("HC_VERIFY_%s assets=%d fails=%d" % ("FAIL" if fails else "PASS", len(rows), len(fails)))
for x in fails: print("  FAIL", x)
if fails: sys.exit(1)
