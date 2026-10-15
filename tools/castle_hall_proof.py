#!/usr/bin/env python3
"""Offline proof of the great hall kit (no Unity, no GPU engine): imports the exported FBX files into headless Blender, places them exactly as
CastleWorld/CastleHallKit place them (castle-local Unity coordinates and yaws, converted to Blender with Blender (x, y, z) = Unity (-x, -z, y)),
adds plain wall/floor/gallery context boxes, and renders Workbench views from the hall's capture eyes.

    /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/castle_hall_proof.py -- <out_prefix>

Writes <out_prefix>_<view>.png. The views are CPU-light Workbench renders, not the game's lighting: they prove geometry, scale, the axis
convention and that nothing intersects, not the look.
"""
import bpy, sys, math
from pathlib import Path
from mathutils import Vector, Matrix
sys.path.insert(0, str(Path(__file__).resolve().parent))
import castle_common as cc

out = (sys.argv[sys.argv.index("--") + 1:] or ["/tmp/hall"])[0]
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
cache = {}

def B(p):
    return Vector((-p[0], -p[2], p[1]))

def load(name):
    if name in cache:
        return cache[name]
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(cc.CASTLE / (name + ".fbx")))
    objs = [o for o in bpy.data.objects if o not in before]
    for o in objs:
        for m in o.data.materials if o.type == 'MESH' else []:
            if m and m.use_nodes:
                bsdf = next((n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
                if bsdf:
                    m.diffuse_color = bsdf.inputs["Base Color"].default_value
    cache[name] = objs
    for o in objs:
        o.hide_render = True        # the first import is the template; copies are placed
    return objs

def place(name, pos, yaw=0.0, scale=(1, 1, 1)):
    tmpl = load(name)
    for o in tmpl:
        c = o.copy(); c.data = o.data
        sc.collection.objects.link(c)
        c.hide_render = False
        # template objects sit at their FBX-local transform (blender frame); compose with the placement
        M = Matrix.Translation(B(pos)) @ Matrix.Rotation(-math.radians(yaw), 4, 'Z') @ Matrix.Diagonal((scale[0], scale[2], scale[1], 1))
        c.matrix_world = M @ o.matrix_world

def box(c, size, color):
    bpy.ops.mesh.primitive_cube_add(size=1)
    o = bpy.context.active_object
    o.dimensions = (size[0], size[2], size[1])
    o.location = B(c)
    m = bpy.data.materials.new("ctx"); m.diffuse_color = (*color, 1); o.data.materials.append(m)

# ---------------------------------------------------------------- context: floor, walls, galleries, old cove ceiling (flat stand-in)
G = (.16, .17, .20)
box((0, -.05, 21), (12, .1, 14), (.1, .1, .12))                         # floor
box((-5.9, 4.6, 21), (.65, 9.2, 14), G); box((5.9, 4.6, 21), (.65, 9.2, 14), G)       # side walls (inner face 5.575)
box((0, 4.6, 14.1), (12.4, 9.2, .65), G)                                  # south wall
box((0, 4.6, 27.9), (12.4, 9.2, .95), G)   # north wall: 0.9 m, inner face 27.45 (veneer face 27.425)                                  # north wall
box((-4.35, 3.1, 21), (2.7, .2, 13.4), (.2, .15, .1)); box((4.35, 3.1, 21), (2.7, .2, 13.4), (.2, .15, .1))   # galleries
box((0, 9.1, 21), (12, .2, 14), (.12, .12, .14))                          # roof slab (old cove sits just below it)

# ---------------------------------------------------------------- the kit
for sx in (-1, 1):
    for z in (15.6, 18.6, 21.6, 24.4):
        place("HallColumn", (sx * 3.9, 0, z))
    for zc in (17.1, 20.1, 23.0):
        place("PointedArch", (sx * 3.9, 0, zc), 90, (1, 1, .93 if zc == 23.0 else 1.0))
    place("HallCorbel", (sx * 3.9, 5.30, 14.425), 0)
    place("HallCorbel", (sx * 3.9, 5.30, 27.425), 180)
place("HallVault", (0, 0, 0))
# windows: north tall lancets and the six clerestory windows (yaw: +z room side = 0, east wall (room to -x) = -90, west = 90)
for sx in (-1, 1):
    place("TallLancetWindow", (sx * 4.4, 1.8, 28.0), 180)
for z in (17.5, 21.5, 25.0):
    place("ClerestoryWindow", (-6.0, 6.8, z), 90); place("ClerestoryWindow", (6.0, 6.8, z), -90)
# mouldings along the walls
for zc in (16.425, 20.425, 24.425):
    place("HallPlinth", (-5.575, 0, zc), 90); place("HallPlinth", (5.575, 0, zc), -90)
place("HallStringCourse", (0, 5.5, 14.425), 0)
# table and candles
place("LongTable", (0, 0, 23.7), 90)
for x in (-3.4, 3.4):
    for z in (20.1, 16.9):
        place("Candelabra", (x, 0, z))

# ---------------------------------------------------------------- render
sc.render.engine = 'BLENDER_WORKBENCH'
sc.display.shading.light = 'STUDIO'; sc.display.shading.color_type = 'MATERIAL'
sc.display.shading.show_shadows = True; sc.display.shading.show_cavity = True
sc.render.resolution_x, sc.render.resolution_y = 1280, 720
sc.world = bpy.data.worlds.new("w"); sc.world.color = (.1, .11, .14)
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
cam.data.sensor_fit = 'VERTICAL'; cam.data.angle_y = math.radians(60); cam.data.clip_start = .05; cam.data.clip_end = 100

VIEWS = {
    "S1_entrance": ((0, 1.64, 14.75), (0, 4.2, 27)),
    "vault_up": ((0, 1.64, 16.0), (0, 7.2, 23)),
    "aisle": ((-5.0, 1.6, 17.0), (2.5, 5.0, 23)),
    "north": ((0, 1.64, 22.0), (0, 5.0, 28)),
    "windows": ((3.0, 1.64, 17.0), (-5.5, 7.6, 21.5)),
}
for name, (eye, look) in VIEWS.items():
    cam.location = B(eye)
    d = B(look) - B(eye)
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = "%s_%s.png" % (out, name)
    bpy.ops.render.render(write_still=True)
    print("HC_PROOF", sc.render.filepath, flush=True)

# ================================================================================================ the chapel (altar, pews, the broken sun, side lancets)
for o in list(bpy.data.objects):
    if o is not cam:
        bpy.data.objects.remove(o)
cache.clear()

def cut_wall_x(xc, thick, z_lo, z_hi, openings, y_top=6.4):
    """Wall plane x = xc with rectangular openings [(z0, z1, y0, y1)] cut out of it, built from boxes."""
    cuts = sorted(openings)
    z = z_lo
    for z0, z1, y0, y1 in cuts:
        if z0 > z: box((xc, y_top / 2, (z + z0) / 2), (thick, y_top, z0 - z), G)
        box((xc, y0 / 2, (z0 + z1) / 2), (thick, y0, z1 - z0), G)
        box((xc, (y1 + y_top) / 2, (z0 + z1) / 2), (thick, y_top - y1, z1 - z0), G)
        z = z1
    if z < z_hi: box((xc, y_top / 2, (z + z_hi) / 2), (thick, y_top, z_hi - z), G)

box((10, -.05, 6.5), (8, .1, 13), (.1, .1, .12))                              # floor
box((10, 6.35, 6.5), (8, .1, 13), (.12, .12, .14))                            # ceiling
box((10, 3.2, -.1), (8, 6.4, .3), G); box((10, 3.2, 13.1), (8, 6.4, .3), G)   # side walls
box((6.1, 3.2, 6.5), (.3, 6.4, 13), G)                                         # west wall
cut_wall_x(13.95, .9, 0, 13, [(5.5, 7.5, 2.9, 6.0), (2.8, 4.2, 1.4, 4.9), (8.8, 10.2, 1.4, 4.9)])
box((15.2, 3.0, 6.5), (.2, 8.0, 14), (.05, .08, .2))                          # the sky behind the openings
box((12.575, .08, 6.5), (1.95, .16, 5.0), (.12, .12, .14)); box((12.875, .24, 6.5), (1.35, .16, 4.0), (.12, .12, .14))
place("Altar", (13.0, .32, 6.5), -90)
place("ChapelSunburst", (14.0, 2.9, 6.5), -90)
for z in (3.5, 9.5):
    place("TallLancetWindow", (14.0, 1.4, z), -90)
for x in (8.4, 9.7, 11.0):
    for z in (1.7, 4.2, 8.8, 11.3):
        place("Pew", (x, 0, z), 90)
for z in (4.8, 8.2):
    place("Candelabra", (12.7, .32, z))
CH_VIEWS = {"chapel_S2": ((7.0, 1.64, 6.5), (13.0, 2.6, 6.5)), "chapel_high": ((9.0, 1.64, 3.0), (13.5, 3.6, 6.6)), "chapel_pews": ((7.2, 1.64, 10.5), (11.5, .6, 4.0))}
for name, (eye, look) in CH_VIEWS.items():
    cam.location = B(eye)
    d = B(look) - B(eye)
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = "%s_%s.png" % (out, name)
    bpy.ops.render.render(write_still=True)
    print("HC_PROOF", sc.render.filepath, flush=True)
