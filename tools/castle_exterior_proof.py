"""Offline proof renders for the exterior kit (Blender Workbench, no Unity, no GPU engine).

Called from tools/castle_exterior.py --proof DIR. Builds the kit pieces, a proxy of the existing building massing
(wall boxes, roofs, gate arch, drawbridge deck and chains, moat, banks) and renders the player's vantage points:
the camera clip is the player camera's (near .04, far 75), the sky is the real SkyCastle equirect (docs/haunted-castle/art/sky_preview.jpg)
composited behind the render by camera direction. Lighting is Workbench STUDIO with moonlit viewport colours: it proves silhouette,
massing, clearance and the backdrop, not final lighting or textures.
"""
import math, json
from pathlib import Path
import bpy, numpy as np
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
SKY = ROOT / "docs/haunted-castle/art/sky_preview.jpg"

PALETTE = {  # viewport colours (moonlit: dark blue-grey stone, warm glow)
    "Castle_Stone": (.20, .21, .27), "Castle_StoneWeathered": (.18, .20, .27), "Castle_StoneDark": (.095, .10, .13),
    "Castle_StoneMossy": (.12, .15, .14), "Castle_BlackStone": (.05, .055, .085), "Castle_Iron": (.06, .06, .07),
    "Castle_Oxblood": (.30, .04, .06), "Castle_ExtGlow": (1.0, .66, .22), "Castle_ExtBackdrop": (.05, .07, .12),
    "Proof_Wall": (.15, .16, .21), "Proof_Roof": (.085, .09, .115), "Proof_Blood": (.30, .02, .035), "Proof_Earth": (.055, .06, .07),
    "Proof_Wood": (.20, .12, .07), "Proof_Far": (.040, .055, .095), "Proof_Near": (.020, .028, .052),
}

VIEWS = [  # name, eye, target (castle-local Unity coords), vertical fov, size
    ("S4_exterior_sky", (0, 2.5, -14), None, 95, (2416, 1292)),                  # CaptureMode S4: pitch 27 (looks up from the far bank)
    ("bridge_gate", (0, 1.7, -4.2), (0, 5.2, 3), 72, (1920, 1080)),               # on the lowered bridge, looking at the gate
    ("bridge_keep", (0, 1.7, -6.1), (0, 16.0, 19.5), 72, (1920, 1080)),           # far end of the bridge, tilted up at the keep
    ("bridge_left", (-1.2, 1.7, -3.5), (-14, 4.5, 3.0), 72, (1920, 1080)),        # on the bridge, looking along the west moat and rock
    ("gate_out_S6", (0.4, 1.3, 5.6), (0.4, 3.0, -16), 72, (1920, 1080)),         # from the S6 eye, looking out through the gate opening
    ("courtyard_keep", (0, 1.64, 7.0), (0, 20.0, 19.5), 72, (1920, 1080)),       # in the courtyard, looking up at the keep
    ("courtyard_wall", (3.0, 1.64, 11.5), (-0.8, 9.0, 3.0), 72, (1920, 1080)),    # courtyard, looking back at the gate wall top
    ("courtyard_east", (-4.0, 1.64, 9.0), (6.0, 8.0, 5.0), 72, (1920, 1080)),    # courtyard, looking along the wall walk
    ("battlement_out", (3.6, 9.0, -0.4), (0.5, 3.0, -14), 72, (1920, 1080)),     # eye on the gate wall top (the Count's perch) looking out
    ("battlement_in", (3.6, 9.0, 0.4), (0, 9.0, 16), 72, (1920, 1080)),          # same spot looking into the castle
]

def mat_for(name):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name); m.use_nodes = True
    base = name.split(".")[0]
    c = PALETTE.get(base, (.1, .1, .1))
    m.diffuse_color = (c[0], c[1], c[2], 1.0)
    return m

def run(outdir, ext):
    out = Path(outdir); out.mkdir(parents=True, exist_ok=True)
    K = ext.K; U = ext.U
    # ---- kit pieces
    kit = []
    for name, make, budget in ext.PIECES:
        objs, info = make().b.finish()
        kit.append((name, objs, info))
    # backdrop: assign per-layer proof materials from the UV layer id
    back = next(objs for n, objs, i in kit if n == "ExtBackdrop")[0]
    me = back.data
    for m in ("Proof_Far", "Proof_Near", "Proof_Haze"): me.materials.append(mat_for(m))
    uv = me.uv_layers[0].data
    drop = []
    for p in me.polygons:
        lid = round(uv[p.loop_indices[0]].uv.x)
        p.material_index = len(me.materials) - 3 + (0 if lid == 0 else 1)
        if lid == 2: drop.append(p.index)                       # the mist band is alpha-blended in game; not drawn by Workbench
    import bmesh
    bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[bm.faces[i] for i in drop], context='FACES'); bm.to_mesh(me); bm.free()
    # ---- proxy of the existing building massing (castle-local Unity coordinates)
    px = K("Proxy")
    def blk(x0, x1, y0, y1, z0, z1, mat="Proof_Wall"): px.box(((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), (x1 - x0, y1 - y0, z1 - z0), mat)
    wall = "Castle_Stone"
    cc = ext.cc
    for nm in ("Proof_Wall", "Proof_Roof", "Proof_Blood", "Proof_Earth", "Proof_Wood"):
        cc.MATERIALS[nm] = ((.1, .1, .1), 0, 0, False)
    blk(-14.45, -1.5, -3.2, 6.4, -.45, .45); blk(1.5, 14.45, -3.2, 6.4, -.45, .45)             # front wall (gate opening x +-1.5 up to 4.5)
    blk(-6, -1.5, 6.4, 7.5, -.45, .45); blk(1.5, 6, 6.4, 7.5, -.45, .45); blk(-1.5, 1.5, 4.5, 7.5, -.45, .45)
    blk(-14.45, -13.55, -3.2, 6.4, -.45, 28.45); blk(13.55, 14.45, -3.2, 6.4, -.45, 19.4); blk(13.55, 14.45, -3.2, 11.6, 19.4, 28.45)
    blk(-14.45, -6, -3.2, 6.4, 27.55, 28.45); blk(-6, 6, -3.2, 9.2, 27.55, 28.45); blk(6, 14.45, -3.2, 11.6, 27.55, 28.45)
    for sx in (-1, 1):
        blk(sx * 6 - .3, sx * 6 + .3, -3.2, 6.4, 0, 14); blk(sx * 6 - .3, sx * 6 + .3, -3.2, 9.2, 14, 28)
    blk(-6, 6, -3.2, 7.5, 5.7, 6.3); blk(-6, 6, -3.2, 9.2, 13.7, 14.3)
    blk(-6, 6, 5.0, 5.2, -.45, 6.3, "Proof_Roof"); blk(-14, -6, 6.2, 6.4, 0, 28, "Proof_Roof"); blk(6, 14, 6.2, 6.4, 0, 28, "Proof_Roof")
    blk(-6, 6, 9.0, 9.2, 14, 28, "Proof_Roof"); blk(6, 14, 11.4, 11.6, 19.4, 28, "Proof_Roof"); blk(6, 14, 6.4, 11.6, 19.1, 19.7)
    blk(-6, 6, -.02, 0, 0, 14, "Proof_Roof")                                                       # courtyard / gatehouse floor
    for z0, z1 in ((-5.6, -.45), (28.45, 33.6)): blk(-19.5, 19.5, -.6 - .05, -.6, z0, z1, "Proof_Blood")
    for x0, x1 in ((-19.5, -14.45), (14.45, 19.5)): blk(x0, x1, -.65, -.6, -.45, 28.45, "Proof_Blood")
    blk(-60, 60, -2.0, 0, -60.4, -5.6, "Proof_Earth")                                              # far bank (the proxy ground)
    for x0, x1 in ((-60.5, -19.5), (19.5, 60.5)): blk(x0, x1, -2.0, 0, -5.6, 33.6, "Proof_Earth")
    blk(-60, 60, -2.0, 0, 33.6, 90, "Proof_Earth")
    # gate arch (rectangular opening 3.0 x 4.5 with a lintel and a small gable) and the lowered drawbridge with parapets
    for sx in (-1, 1): px.box((sx * 1.7, 2.25, 0), (.4, 4.5, 1.0), "Castle_Stone")
    px.box((0, 4.7, 0), (3.8, .4, 1.0), "Castle_Stone")
    px.poly_xy([(-1.9, 4.9), (1.9, 4.9), (0, 5.6)], -.5, .5, "Castle_Stone")
    px.box((0, -.125, -3.45), (3.24, .25, 6.0), "Proof_Wood")
    for sx in (-1, 1): px.box((sx * 1.5, .45, -3.45), (.2, .9, 6.0), "Castle_Stone")
    for sx in (-1, 1):                                                                                # the two chains
        a = np.array([sx * 1.5, 0.0, -6.35]); c = np.array([sx * 1.5, 4.4, -.75])
        for i in range(24):
            q = a + (c - a) * (i + .5) / 24
            px.box(tuple(q), (.12, .12, .3), "Castle_Iron")
    # buttresses on the front wall
    for sx in (-1, 1): px.box((sx * 8, 1.6, -.9), (.9, 6.4, 1.2), "Castle_Stone")
    pobjs, pinfo = px.b.finish()
    allobjs = [o for n, objs, i in kit for o in objs] + pobjs
    for o in allobjs:
        for slot in o.material_slots: mat_for(slot.material.name)
        o.data.shade_flat()
    # ---- render setup
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_WORKBENCH'
    sc.render.film_transparent = True
    sc.view_settings.view_transform = 'Standard'; sc.view_settings.look = 'None'
    sc.render.image_settings.file_format = 'PNG'; sc.render.image_settings.color_mode = 'RGBA'
    sc.display.render_aa = '8'
    sh = sc.display.shading
    sh.color_type = 'MATERIAL'; sh.show_cavity = False; sh.show_object_outline = False
    sc.display.light_direction = (-.35, .55, .75)   # moonlight from the pale-moon side, high
    cam_data = bpy.data.cameras.new("proof"); cam = bpy.data.objects.new("proof", cam_data); sc.collection.objects.link(cam); sc.camera = cam
    cam_data.sensor_fit = 'VERTICAL'; cam_data.clip_start = .04; cam_data.clip_end = 75.0
    backobj = back
    others = [o for o in allobjs if o is not backobj]
    views_json = []
    for (name, eye, target, fov, (W, Hh)) in VIEWS:
        e = np.array(eye, float)
        if target is None:                                         # S4: pitch 27 up
            d = np.array([0, math.sin(math.radians(27)), math.cos(math.radians(27))])
        else:
            d = np.array(target, float) - e; d /= np.linalg.norm(d)
        cam.location = Vector(U(tuple(e)))
        cam.rotation_euler = Vector(U(tuple(d))).to_track_quat('-Z', 'Y').to_euler()
        cam_data.angle = math.radians(fov)
        sc.render.resolution_x, sc.render.resolution_y, sc.render.resolution_percentage = W, Hh, 100
        raw = out / "_raw"; raw.mkdir(exist_ok=True)
        for tag, objs, light in (("back", [backobj], 'FLAT'), ("scene", others, 'STUDIO')):
            for o in allobjs: o.hide_render = o not in objs
            sh.light = light
            sc.render.filepath = str(raw / ("%s_%s.png" % (name, tag)))
            bpy.ops.render.render(write_still=True)
        views_json.append(dict(name=name, eye=list(map(float, e)), dir=list(map(float, d)), fov=fov, size=[W, Hh]))
        print("HC_PROOF", name, W, Hh)
    (out / "_raw" / "views.json").write_text(json.dumps(views_json, indent=1))
    # ---- measurements
    report = {}
    # moon clearance at the S4 eye: smallest angular gap between any kit vertex and each moon disc
    e = np.array((0, 2.5, -14.0))
    def unit(az, el): a, l = math.radians(az), math.radians(el); return np.array([math.sin(a) * math.cos(l), math.sin(l), math.cos(a) * math.cos(l)])
    moons = {"pale": (unit(-34, 55), 10.8), "red": (unit(42, 42), 4.2)}
    pts = []
    for n, objs, info in kit:
        if n in ("ExtBackdrop", "ExtRock"): continue
        for o in objs:
            for v in o.data.vertices:
                w = o.matrix_world @ v.co; pts.append((w.x, w.z, w.y))      # Blender (x, y_b, z_b) -> Unity (x, y=z_b, z=y_b)
    P = np.array(pts, float); D = P - e; D /= np.linalg.norm(D, axis=1, keepdims=True)
    for mname, (m, rad) in moons.items():
        ang = np.degrees(np.arccos(np.clip(D @ m, -1, 1)))
        report["moon_gap_%s_deg" % mname] = round(float(ang.min() - rad), 2)
    report["max_vertex_y"] = round(float(P[:, 1].max()), 2)
    # gameplay clearance, every kit vertex (rock included, backdrop excluded): rooms, the Count's standing spot, the bridge corridor
    allp = []
    for n, objs, info in kit:
        if n == "ExtBackdrop": continue
        for o in objs:
            for v in o.data.vertices:
                w = o.matrix_world @ v.co; allp.append((w.x, w.z, w.y))
    A = np.array(allp, float)
    def inside(box): lo, hi = np.array(box[0]) + .02, np.array(box[1]) - .02; return int(np.all((A > lo) & (A < hi), axis=1).sum())   # 2 cm tolerance: shared faces do not count
    rooms = {"west_wing": ((-13.55, -3.2, .45), (-6.3, 6.2, 27.55)), "east_wing": ((6.3, -3.2, .45), (13.55, 6.2, 27.55)),
             "great_hall": ((-5.7, -3.2, 14.3), (5.7, 9.0, 27.55)), "gatehouse": ((-5.7, 0, .45), (5.7, 5.0, 5.7)),
             "observatory": ((6.3, 6.4, 19.7), (13.55, 11.4, 27.55))}
    report["vertices_inside_rooms"] = {k_: inside(v_) for k_, v_ in rooms.items()}
    report["vertices_in_standing_spot_box"] = inside(((3.0, 7.5, -1.2), (4.2, 10.7, 1.2)))
    report["vertices_in_bridge_corridor"] = inside(((-1.7, 0.0, -7.0), (1.7, 3.2, -.5)))
    report["triangles_total"] = int(sum(info["tris"] for n, objs, info in kit))
    (out / "exterior_proof_report.json").write_text(json.dumps(report, indent=1) + "\n")
    print("HC_PROOF_REPORT", report)
