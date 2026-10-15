"""Offline proof renders and measurements for the exterior kit (Blender Workbench + BVH ray casts, no Unity, no GPU engine).

Called from tools/castle_exterior.py --proof DIR [--moons-only]. Builds the kit pieces, a proxy of the existing building massing (wall boxes,
roofs, gate arch, drawbridge deck and chains, moat, banks) and
  1. renders the player's vantage points as four Workbench layers per view (back ridge ring / lit scene / window mask / window texture)
     that tools/castle_exterior_composite.py puts over the real SkyCastle equirect (windows with the engine's shader maths);
  2. casts rays through both moon discs from every eye in MOON_EYES, with and without the kit, and writes the occluded fractions.
The camera clip is the player camera's (.04 to 75). Lighting is Workbench STUDIO with moonlit viewport colours: it proves silhouette, massing,
window texture, clearance and the backdrop, not final lighting.
"""
import math, json
from pathlib import Path
import bpy, bmesh, numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parents[1]
PREFIX = "exteriorL_proof"

def lin(c):                      # display (sRGB-ish) value -> the linear value Blender's Standard transform will show again
    return tuple(float(x) ** 2.2 for x in c)

PALETTE = {  # DISPLAY colours (what the screen shows); lin() turns them into viewport colours
    "Castle_Stone": (.40, .42, .50), "Castle_StoneWeathered": (.38, .41, .50), "Castle_StoneDark": (.24, .25, .31),
    "Castle_StoneMossy": (.27, .31, .30), "Castle_BlackStone": (.13, .14, .20), "Castle_Iron": (.17, .17, .19),
    "Castle_Oxblood": (.52, .17, .21), "Castle_ExtWindow": (.85, .58, .23), "Castle_ExtBackdrop": (.2, .2, .3),
    "Proof_Wall": (.36, .38, .46), "Proof_Roof": (.22, .23, .29), "Proof_Water": (.10, .02, .03), "Castle_Basalt": (.17, .19, .25), "Proof_Earth": (.13, .14, .17),
    "Proof_Wood": (.42, .32, .24), "Proof_Far": None, "Proof_Near": None,
}

VIEWS = [  # name, eye, target (castle-local Unity coords), vertical fov, size
    ("S4_exterior_sky", (0, 2.5, -14), None, 95, (2416, 1292)),
    ("bridge_gate", (0, 1.7, -4.2), (0, 5.2, 3), 72, (1920, 1080)),
    ("bridge_keep", (0, 1.7, -6.1), (0, 16.0, 19.5), 72, (1920, 1080)),
    ("bridge_left", (-1.2, 1.7, -3.5), (-14, 4.5, 3.0), 72, (1920, 1080)),
    ("gate_out_S6", (0.3, 1.64, 1.4), (0.2, 2.2, -20), 72, (1920, 1080)),
    ("courtyard_keep", (0, 1.64, 7.0), (0, 20.0, 19.5), 72, (1920, 1080)),
    ("courtyard_wall", (3.0, 1.64, 11.5), (-0.8, 9.0, 3.0), 72, (1920, 1080)),
    ("courtyard_east", (-4.0, 1.64, 9.0), (6.0, 8.0, 5.0), 72, (1920, 1080)),
    ("battlement_out", (3.6, 9.0, -0.4), (0.0, 3.0, -30), 72, (1920, 1080)),
    ("battlement_in", (3.6, 9.0, 0.4), (0, 9.0, 16), 72, (1920, 1080)),
]
MOON_EYES = {  # the three the coordinator named, plus the two neighbours the captures used
    "S4": (0, 2.5, -14.0), "bridge_mid": (0, 1.7, -3.45), "bridge_end": (0, 1.7, -6.1),
    "courtyard_capture": (0, 1.64, 7.0), "courtyard_centre": (0, 1.64, 10.0),
}
# SkyCastle (tools/castle_sky.py): azimuth from +z toward +x; angular radii 11 and 4.2 deg, measured from the texture; +.5 deg margin
MOONS = {"pale": (-34.0, 55.0, 11.5), "red": (42.0, 42.0, 4.8)}

def unit(az, el):
    a, l = math.radians(az), math.radians(el)
    return np.array([math.sin(a) * math.cos(l), math.sin(l), math.cos(a) * math.cos(l)])

def mat_for(ext, name, png=None):
    base = name.split(".")[0]
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
    m.use_nodes = True
    c = PALETTE.get(base) or (.1, .1, .1)
    c = lin(c)
    m.diffuse_color = (c[0], c[1], c[2], 1.0)
    return m

def disc_dirs(az, el, radius_deg):
    c = unit(az, el); r = np.cross([0, 1, 0], c); r /= np.linalg.norm(r); u = np.cross(c, r)
    out = [c]
    for ring in range(1, 7):
        a = math.radians(radius_deg * ring / 6); n = 6 * ring
        for i in range(n):
            p = 2 * math.pi * i / n
            out.append(c * math.cos(a) + (r * math.cos(p) + u * math.sin(p)) * math.sin(a))
    return out

def run(outdir, ext):
    out = Path(outdir); out.mkdir(parents=True, exist_ok=True)
    K = ext.K; U = ext.U
    moons_only = "--moons-only" in ext.ARGS
    # ---- kit pieces
    kit = []
    for name, make, budget in ext.PIECES:
        objs, info = make().b.finish()
        kit.append((name, objs, info))
    back = next(objs for n, objs, i in kit if n == "ExtBackdrop")[0]
    me = back.data
    for m in ("Proof_Far", "Proof_Near"): me.materials.append(bpy.data.materials.new(m))
    uv = me.uv_layers[0].data
    drop = []
    for p in me.polygons:
        lid = round(uv[p.loop_indices[0]].uv.x)
        p.material_index = len(me.materials) - 2 + (0 if lid == 0 else 1)
        if lid == 2: drop.append(p.index)                       # the mist band is alpha-blended in game; not drawn by Workbench
    bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[bm.faces[i] for i in drop], context='FACES'); bm.to_mesh(me); bm.free()
    # ---- proxy of the existing building massing (castle-local Unity coordinates)
    px = K("Proxy")
    cc = ext.cc
    for nm in ("Proof_Wall", "Proof_Roof", "Proof_Water", "Proof_Earth", "Proof_Wood"):
        cc.MATERIALS[nm] = ((.1, .1, .1), 0, 0, False)
    def blk(x0, x1, y0, y1, z0, z1, mat="Proof_Wall"): px.box(((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), (x1 - x0, y1 - y0, z1 - z0), mat)
    blk(-14.45, -1.5, -3.2, 6.4, -.45, .45); blk(1.5, 14.45, -3.2, 6.4, -.45, .45)
    blk(-6, -1.5, 6.4, 7.5, -.45, .45); blk(1.5, 6, 6.4, 7.5, -.45, .45); blk(-1.5, 1.5, 4.5, 7.5, -.45, .45)
    blk(-14.45, -13.55, -3.2, 6.4, -.45, 28.45); blk(13.55, 14.45, -3.2, 6.4, -.45, 19.4); blk(13.55, 14.45, -3.2, 11.6, 19.4, 28.45)
    blk(-14.45, -6, -3.2, 6.4, 27.55, 28.45); blk(-6, 6, -3.2, 9.2, 27.55, 28.45); blk(6, 14.45, -3.2, 11.6, 27.55, 28.45)
    for sx in (-1, 1):
        blk(sx * 6 - .3, sx * 6 + .3, -3.2, 6.4, 0, 14); blk(sx * 6 - .3, sx * 6 + .3, -3.2, 9.2, 14, 28)
    blk(-6, 6, -3.2, 7.5, 5.7, 6.3); blk(-6, 6, -3.2, 9.2, 13.7, 14.3)
    blk(-6, 6, 5.0, 5.2, -.45, 6.3, "Proof_Roof"); blk(-14, -6, 6.2, 6.4, 0, 28, "Proof_Roof"); blk(6, 14, 6.2, 6.4, 0, 28, "Proof_Roof")
    blk(-6, 6, 9.0, 9.2, 14, 28, "Proof_Roof"); blk(6, 14, 11.4, 11.6, 19.4, 28, "Proof_Roof"); blk(6, 14, 6.4, 11.6, 19.1, 19.7)
    blk(-6, 6, -.02, 0, 0, 14, "Proof_Roof")
    for z0, z1 in ((-5.6, -.45), (28.45, 33.6)): blk(-19.5, 19.5, -.6 - .05, -.6, z0, z1, "Proof_Water")
    for x0, x1 in ((-19.5, -14.45), (14.45, 19.5)): blk(x0, x1, -.65, -.6, -.45, 28.45, "Proof_Water")
    blk(-60, 60, -2.0, 0, -60.4, -5.6, "Proof_Earth")
    for x0, x1 in ((-60.5, -19.5), (19.5, 60.5)): blk(x0, x1, -2.0, 0, -5.6, 33.6, "Proof_Earth")
    blk(-60, 60, -2.0, 0, 33.6, 90, "Proof_Earth")
    for sx in (-1, 1): px.box((sx * 1.7, 2.25, 0), (.4, 4.5, 1.0), "Castle_Stone")
    px.box((0, 4.7, 0), (3.8, .4, 1.0), "Castle_Stone")
    px.poly_xy([(-1.9, 4.9), (1.9, 4.9), (0, 5.6)], -.5, .5, "Castle_Stone")
    px.box((0, -.125, -3.45), (3.24, .25, 6.0), "Proof_Wood")
    for sx in (-1, 1): px.box((sx * 1.5, .45, -3.45), (.2, .9, 6.0), "Castle_Stone")
    for sx in (-1, 1):
        a = np.array([sx * 1.5, 0.0, -6.35]); c = np.array([sx * 1.5, 4.4, -.75])
        for i in range(24):
            q = a + (c - a) * (i + .5) / 24
            px.box(tuple(q), (.12, .12, .3), "Castle_Iron")
    for sx in (-1, 1): px.box((sx * 8, 1.6, -.9), (.9, 6.4, 1.2), "Castle_Stone")
    pobjs, pinfo = px.b.finish()
    allobjs = [o for n, objs, i in kit for o in objs] + pobjs
    for o in allobjs: o.data.shade_flat()

    # ---- moon clearance (BVH ray casts in Blender space; U maps Unity -> Blender)
    dg = bpy.context.evaluated_depsgraph_get()
    def bvh_of(objs): return [BVHTree.FromObject(o, dg) for o in objs]
    kit_objs = [o for n, objs, i in kit if n not in ("ExtBackdrop",) for o in objs]
    trees = {"existing": bvh_of(pobjs), "with_kit": bvh_of(pobjs + kit_objs)}
    def occluded(eye, d, tset):
        o = Vector(U(tuple(eye))); dv = Vector(U(tuple(d)))
        return any(t.ray_cast(o, dv, 300.0)[0] is not None for t in tset)
    moon_report = {}
    piece_trees = [(o.name, BVHTree.FromObject(o, dg)) for o in kit_objs]
    def blamed(eye, d):
        o = Vector(U(tuple(eye))); dv = Vector(U(tuple(d))); best = None
        for nm, t in piece_trees:
            h = t.ray_cast(o, dv, 300.0)
            if h[0] is not None and (best is None or h[3] < best[1]): best = (nm, h[3], (round(-h[0].x, 1), round(h[0].z, 1), round(-h[0].y, 1)))
        if best and "--debug-hits" in ext.ARGS: print("HC_HIT", eye, best[0], "at", best[2])
        return best[0] if best else None
    for ename, eye in MOON_EYES.items():
        for mname, (az, el, rad) in MOONS.items():
            dirs = disc_dirs(az, el, rad)
            res = {}
            for tag, tset in trees.items():
                hits = sum(1 for d in dirs if occluded(eye, d, tset))
                res[tag] = round(100.0 * hits / len(dirs), 1)
            c = occluded(eye, unit(az, el), trees["with_kit"])
            blame = {}
            for d in dirs:
                if not occluded(eye, d, trees["existing"]):
                    b_ = blamed(eye, d)
                    if b_: blame[b_] = blame.get(b_, 0) + 1
            moon_report["%s/%s" % (ename, mname)] = dict(existing_pct=res["existing"], with_kit_pct=res["with_kit"], added_pct=round(res["with_kit"] - res["existing"], 1), centre_blocked=bool(c), added_by=blame)
    for k_, v_ in moon_report.items(): print("HC_MOON", k_, v_)

    # ---- gameplay clearance, every kit vertex (rock included, backdrop excluded)
    allp = []
    for o in kit_objs:
        for v in o.data.vertices:
            w = o.matrix_world @ v.co; allp.append((-w.x, w.z, -w.y))       # Blender (-x, -z, y) -> Unity (x, y, z)
    A = np.array(allp, float)
    def inside(box): lo, hi = np.array(box[0]) + .02, np.array(box[1]) - .02; return int(np.all((A > lo) & (A < hi), axis=1).sum())
    rooms = {"west_wing": ((-13.55, -3.2, .45), (-6.3, 6.2, 27.55)), "east_wing": ((6.3, -3.2, .45), (13.55, 6.2, 27.55)),
             "great_hall": ((-5.7, -3.2, 14.3), (5.7, 9.0, 27.55)), "gatehouse": ((-5.7, 0, .45), (5.7, 5.0, 5.7)),
             "observatory": ((6.3, 6.4, 19.7), (13.55, 11.4, 27.55))}
    report = {"moon_clearance_pct_occluded": moon_report, "max_vertex_y": round(float(A[:, 1].max()), 2),
              "vertices_inside_rooms": {k_: inside(v_) for k_, v_ in rooms.items()},
              "vertices_in_standing_spot_box": inside(((3.0, 7.5, -1.2), (4.2, 10.7, 1.2))),
              "vertices_in_bridge_corridor": inside(((-1.7, 0.0, -7.0), (1.7, 3.2, -.5))),
              "triangles_total": int(sum(info["tris"] for n, objs, info in kit))}
    (out / (PREFIX + "_report.json")).write_text(json.dumps(report, indent=1) + "\n")
    print("HC_PROOF_REPORT", {k_: v_ for k_, v_ in report.items() if k_ != "moon_clearance_pct_occluded"})
    if moons_only: return

    if "--debug-center" in ext.ARGS:
        for (name, eye, target, fov, size) in VIEWS:
            if target is None: continue
            dd = np.array(target, float) - np.array(eye, float); dd /= np.linalg.norm(dd)
            o = Vector(U(tuple(eye))); dv = Vector(U(tuple(dd)))
            hits = []
            for ob in allobjs:
                h = BVHTree.FromObject(ob, dg).ray_cast(o, dv, 200.0)
                if h[0] is not None: hits.append((round(h[3], 2), ob.name, (round(-h[0].x, 2), round(h[0].z, 2), round(-h[0].y, 2))))
            print("HC_CENTER", name, sorted(hits)[:3])
    # ---- render setup: four layers per view
    atlas = out / "_raw" / "atlas.png"; (out / "_raw").mkdir(exist_ok=True)
    import castle_exterior_atlas as ATL
    ATL.write_png(atlas, ATL.build())
    img = bpy.data.images.load(str(atlas)); img.colorspace_settings.name = 'sRGB'
    wm = mat_for(ext, "Castle_ExtWindow")
    nt = wm.node_tree; tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = img; tex.interpolation = 'Linear'
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED"); nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    for o in allobjs:
        for slot in o.material_slots:
            if slot.material and slot.material.name.split(".")[0] != "Castle_ExtWindow": mat_for(ext, slot.material.name)
    far_c, near_c = ext.BACKDROP_PROOF["far"], ext.BACKDROP_PROOF["near"]
    bpy.data.materials["Proof_Far"].diffuse_color = (*lin(far_c), 1); bpy.data.materials["Proof_Near"].diffuse_color = (*lin(near_c), 1)
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_WORKBENCH'; sc.render.film_transparent = True
    sc.view_settings.view_transform = 'Standard'; sc.view_settings.look = 'None'
    sc.render.image_settings.file_format = 'PNG'; sc.render.image_settings.color_mode = 'RGBA'
    sc.display.render_aa = '8'
    sh = sc.display.shading; sh.color_type = 'MATERIAL'; sh.show_cavity = False; sh.show_object_outline = False
    sc.display.light_direction = (-.35, .55, .75)
    cam_data = bpy.data.cameras.new("proof"); cam = bpy.data.objects.new("proof", cam_data); sc.collection.objects.link(cam); sc.camera = cam
    cam_data.sensor_fit = 'VERTICAL'; cam_data.clip_start = .04; cam_data.clip_end = 75.0
    others = [o for o in allobjs if o is not back]
    def set_mats(mode):
        for m in bpy.data.materials:
            base = m.name.split(".")[0]
            if base == "Castle_ExtWindow":
                m.diffuse_color = (1, 0, 1, 1) if mode == "mask" else (*lin(PALETTE["Castle_ExtWindow"]), 1)
            elif base == "Proof_Water" and mode == "mask":
                m.diffuse_color = (0, 1, 0, 1)                    # the composite shades the moat itself (tools/castle_exterior_composite.py)
            elif mode in ("mask", "glow") and base not in ("Proof_Far", "Proof_Near"):
                m.diffuse_color = (0, 0, 0, 1)
            elif PALETTE.get(base):
                m.diffuse_color = (*lin(PALETTE[base]), 1)
    views_json = []
    raw = out / "_raw"
    for (name, eye, target, fov, (W, Hh)) in VIEWS:
        e = np.array(eye, float)
        d = np.array([0, math.sin(math.radians(27)), math.cos(math.radians(27))]) if target is None else (np.array(target, float) - e)
        d = d / np.linalg.norm(d)
        cam.location = Vector(U(tuple(e)))
        cam.rotation_euler = Vector(U(tuple(d))).to_track_quat('-Z', 'Y').to_euler()
        cam_data.angle = math.radians(fov)
        sc.render.resolution_x, sc.render.resolution_y, sc.render.resolution_percentage = W, Hh, 100
        for tag, objs, light, ctype, mode in (("back", [back], 'FLAT', 'MATERIAL', "scene"), ("scene", others, 'STUDIO', 'MATERIAL', "scene"),
                                              ("mask", others, 'FLAT', 'MATERIAL', "mask"), ("glow", others, 'FLAT', 'TEXTURE', "glow")):
            for o in allobjs: o.hide_render = o not in objs
            sh.light = light; sh.color_type = ctype; set_mats(mode)
            sc.render.filepath = str(raw / ("%s_%s.png" % (name, tag)))
            bpy.ops.render.render(write_still=True)
        views_json.append(dict(name=name, eye=list(map(float, e)), dir=list(map(float, d)), fov=fov, size=[W, Hh]))
        print("HC_PROOF", name, W, Hh)
    (raw / "views.json").write_text(json.dumps(views_json, indent=1))
