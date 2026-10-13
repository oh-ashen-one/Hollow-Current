"""Original Bellwether mesh kit. Blender CPU geometry only; never renders.
Coordinates in helper functions are Unity metres (X right, Y up, Z depth).
The blend keeps each asset in its own collection. Exported HCM meshes preserve
split normals and UVs and are consumed by AuthoredImport in Unity.
"""
import bpy, math, json, struct
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/HollowCurrent/Art/Authored'
OUT.mkdir(parents=True,exist_ok=True)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
MATERIALS={
 'Paint':((.68,.65,.54,1),.23,0), 'Sage':((.29,.35,.29,1),.26,0),
 'Walnut':((.17,.085,.038,1),.3,0), 'Oak':((.30,.18,.085,1),.32,0),
 'Brass':((.43,.29,.105,1),.52,.75), 'Nickel':((.33,.36,.35,1),.6,.82),
 'Linen':((.54,.47,.34,1),.05,0), 'Ivory':((.78,.73,.61,1),.24,0),
 'Black':((.018,.022,.02,1),.22,0), 'Glass':((.21,.29,.32,.18),.88,0),
 'Shade':((.78,.67,.46,1),.1,0), 'Ceramic':((.49,.38,.24,1),.35,0),
 'Enamel':((.66,.66,.56,1),.42,0), 'Paper':((.73,.66,.48,1),.02,0)}
mats={}
for name,(color,smooth,metal) in MATERIALS.items():
 m=bpy.data.materials.new(name);m.diffuse_color=color;m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=color;bs.inputs['Roughness'].default_value=1-smooth;bs.inputs['Metallic'].default_value=metal
 mats[name]=m
assets={};active=None

def vec(p):return (p[0],-p[2],p[1])
def begin(name):
 global active
 active=bpy.data.collections.new(name);bpy.context.scene.collection.children.link(active);assets[name]=active

def keep(o,name,mat):
 o.name=name
 for c in list(o.users_collection):c.objects.unlink(o)
 active.objects.link(o);o.data.materials.append(mats[mat]);return o

def box(name,p,s,mat='Paint',bevel=.008):
 bpy.ops.mesh.primitive_cube_add(size=1,location=vec(p));o=bpy.context.object;o.scale=(s[0],s[2],s[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 keep(o,name,mat)
 if bevel:
  mod=o.modifiers.new('Crafted edge radii','BEVEL');mod.width=min(bevel,min(s)*.49);mod.segments=3
  for face in o.data.polygons:face.use_smooth=True
  mod=o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');mod.keep_sharp=True
 return o

def lathe(name,profile,mat='Walnut',p=(0,0,0),segments=32):
 vertices=[];faces=[]
 for height,radius in profile:
  for j in range(segments):
   a=j*math.tau/segments;vertices.append(vec((p[0]+radius*math.cos(a),p[1]+height,p[2]+radius*math.sin(a))))
 for i in range(len(profile)-1):
  for j in range(segments):
   a=i*segments+j;b=i*segments+(j+1)%segments;faces.append((a,a+segments,b+segments,b))
 faces.append(tuple(range(segments)));faces.append(tuple((len(profile)-1)*segments+j for j in reversed(range(segments))))
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);active.objects.link(o);mesh.materials.append(mats[mat])
 for poly in mesh.polygons:poly.use_smooth=len(poly.vertices)==4
 return o

def pipe(name,points,radius,mat='Brass'):
 curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.resolution_u=12;curve.bevel_depth=radius;curve.bevel_resolution=3
 sp=curve.splines.new('BEZIER');sp.bezier_points.add(len(points)-1)
 for bp,p in zip(sp.bezier_points,points):bp.co=vec(p);bp.handle_left_type='AUTO';bp.handle_right_type='AUTO'
 o=bpy.data.objects.new(name,curve);active.objects.link(o);curve.materials.append(mats[mat]);return o

def panel(name,x,y,z,w,h,mat):
 box(name+' recessed center',(x,y,z+.012),(w-.11,h-.11,.027),mat)
 for sign in [-1,1]:
  box(name+' stile',(x+sign*(w-.055)/2,y,z),( .055,h,.045),mat)
  box(name+' rail',(x,y+sign*(h-.055)/2,z),(w,.055,.045),mat)

def export():
 manifest={'version':1,'coordinate_system':'Unity Y up metres','assets':[],'materials':[dict(name=n,color=c,smoothness=s,metallic=m) for n,(c,s,m) in MATERIALS.items()]}
 deps=bpy.context.evaluated_depsgraph_get()
 for name,collection in assets.items():
  bymat={}
  for obj in collection.objects:
   if obj.type not in ('MESH','CURVE'):continue
   ev=obj.evaluated_get(deps);mesh=ev.to_mesh();mesh.calc_loop_triangles()
   material=obj.data.materials[0].name if obj.data.materials else 'Paint'
   vertices,indices=bymat.setdefault(material,([],[]))
   normalmat=obj.matrix_world.to_3x3().inverted().transposed()
   for tri in mesh.loop_triangles:
    for li in tri.loops:
     loop=mesh.loops[li];v=obj.matrix_world@mesh.vertices[loop.vertex_index].co
     normal=normalmat@mesh.corner_normals[li].vector
     uv=mesh.uv_layers.active.data[li].uv[:] if mesh.uv_layers.active else (v.x,v.z)
     indices.append(len(vertices));vertices.append((v.x,v.z,-v.y,normal.x,normal.z,-normal.y,uv[0],uv[1]))
   ev.to_mesh_clear()
  parts=[]
  for material,(verts,inds) in bymat.items():
   filename=name+'-'+material+'.hcm'
   with (OUT/filename).open('wb') as f:
    f.write(struct.pack('<Iii',0x48434d31,len(verts),len(inds)))
    for v in verts:f.write(struct.pack('<8f',*v))
    f.write(struct.pack('<'+'i'*len(inds),*inds))
   parts.append({'file':filename,'material':material,'vertices':len(verts),'triangles':len(inds)//3})
  assert parts,name
  manifest['assets'].append({'name':name,'parts':parts})
 (OUT/'kit.json').write_text(json.dumps(manifest,indent=2)+'\n')
 source=ROOT/'ArtSource';source.mkdir(exist_ok=True)
 bpy.ops.wm.save_as_mainfile(filepath=str(source/'BellwetherArchitecture.blend'))
 print('HC_BLENDER_EXPORT_PASS '+str(len(assets))+' original assets; '+str(sum(p['triangles'] for a in manifest['assets'] for p in a['parts']))+' triangles',flush=True)

begin('SashWindow')
for s in [-1,1]:
 box('Timber jamb',(s*.94,0,0),(.10,1.48,.28),'Paint')
 box('Architrave upright',(s*1.015,0,-.115),(.07,1.58,.055),'Paint')
 box('Top and bottom rail',(0,s*.7,0),(1.92,.10,.28),'Paint')
 box('Outer molding',(0,s*.77,-.115),(2.1,.055,.055),'Paint')
box('Deep sill',(0,-.755,-.14),(2.17,.095,.48),'Ivory')
box('Glass pane',(0,0,.035),(1.80,1.3,.008),'Glass',0)
box('Sash meeting rail',(0,0,-.04),(1.8,.055,.065),'Paint')
box('Sash stile',(0,0,-.04),(.045,1.3,.065),'Paint')
for s in [-1,1]:box('Sash latch',(s*.11,.055,-.09),(.095,.025,.028),'Brass')

begin('CurtainPair')
for side in [-1,1]:
 verts=[];faces=[];nx=24;ny=18
 for j in range(ny+1):
  v=j/ny
  for i in range(nx+1):
   u=i/nx;x=side*(.92+(u-.5)*(.40+v*.09));y=.86-v*1.83
   z=-.22+.053*math.sin(u*math.tau*4)+.025*math.sin(v*math.pi)*math.sin(u*9)
   verts.append(vec((x,y+.012*math.sin(u*math.tau*4),z)))
 for j in range(ny):
  for i in range(nx):
   a=j*(nx+1)+i;faces.append((a,a+1,a+nx+2,a+nx+1))
 mesh=bpy.data.meshes.new('Woven curtain');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Draped linen',mesh);active.objects.link(o);mesh.materials.append(mats['Linen'])
 for f in mesh.polygons:f.use_smooth=True
 mod=o.modifiers.new('Woven thickness','SOLIDIFY');mod.thickness=.004
 for j in range(6):
  x=side*(.76+j*.065);pipe('Curtain hanging loop',[(x,.86,-.22),(x,.96,-.19),(x,.94,-.12),(x,.86,-.15)],.005,'Brass')
pipe('Curtain pole',[(-1.23,.96,-.18),(1.23,.96,-.18)],.018,'Walnut')

begin('BaseCabinet')
box('Carcass',(0,.45,.02),(.8,.9,.72),'Sage')
box('Toe recess',(0,.06,-.365),(.75,.12,.045),'Black')
panel('Frame and panel door',0,.44,-.37,.745,.7,'Sage')
box('Drawer',(0,.825,-.37),(.745,.10,.035),'Sage')
for y in [.825,.56]:pipe('Aged drawer pull',[(-.085,y,-.4),(-.085,y,-.44),(.085,y,-.44),(.085,y,-.4)],.008,'Brass')
box('Countertop',(0,.95,0),(.825,.065,.81),'Ivory',.012)
box('Backsplash',(0,1.06,.35),(.8,.18,.04),'Ivory')

begin('WallCabinet')
box('Carcass',(0,0,0),(.8,.78,.38),'Paint')
panel('Upper panel',0,0,-.205,.745,.72,'Paint')
pipe('Upper pull',[(.26,-.23,-.23),(.26,-.23,-.27),(.26,-.1,-.27),(.26,-.1,-.23)],.007,'Brass')
box('Upper cornice',(0,.42,-.01),(.85,.065,.43),'Paint')

begin('PanelDoor')
box('Door leaf',(0,1.1,0),(1.6,2.2,.09),'Walnut')
for side in [-1,1]:
 for x in [-.37,.37]:
  panel('Lower inset',x,.56,side*.052,.64,.73,'Walnut')
  panel('Upper inset',x,1.54,side*.052,.64,.96,'Walnut')
 box('Latch plate',(.60,1.03,side*.064),(.065,.18,.025),'Brass')
 pipe('Lever handle',[(.60,1.05,side*.08),(.60,1.05,side*.13),(.45,1.05,side*.13)],.018,'Brass')

begin('Baluster')
lathe('Turned timber spindle',[(0,.033),(.10,.033),(.13,.026),(.19,.024),(.24,.034),(.28,.028),(.33,.018),(.75,.018),(.80,.027),(.84,.03),(.88,.022),(1.04,.026)],'Walnut',segments=20)

begin('CeilingLamp')
lathe('Ceiling rose',[(0,.10),(-.025,.11),(-.042,.075)],'Paint')
lathe('Stem',[(-.04,.014),(-.18,.014)],'Brass',segments=16)
lathe('Opal glass diffuser',[(-.17,.06),(-.19,.13),(-.25,.22),(-.31,.23),(-.34,.18),(-.35,.04)],'Shade')
lathe('Metal shade rim',[(-.304,.231),(-.324,.233)],'Brass')

begin('TableLamp')
lathe('Ceramic foot',[(0,.13),(.025,.15),(.055,.12),(.13,.08),(.27,.085),(.32,.04)],'Ceramic')
lathe('Fitting',[(.32,.018),(.43,.018)],'Brass')
lathe('Linen shade',[(.33,.23),(.34,.235),(.62,.115),(.63,.12)],'Shade')
for y,r in [(.34,.235),(.625,.12)]:lathe('Shade piping',[(y-.006,r),(y+.006,r)],'Linen')

for asset,is_switch in [('Switch',True),('Socket',False)]:
 begin(asset);box('Bakelite plate',(0,0,0),(.084,.132,.014),'Ivory',.01)
 if is_switch:box('Rocker',(0,0,-.012),(.032,.055,.012),'Paint',.005)
 else:
  for y in [-.03,.03]:
   for x in [-.013,.013]:box('Outlet slot',(x,y,-.009),(.006,.017,.005),'Black',.001)
 for y in [-.052,.052]:box('Countersunk screw',(0,y,-.009),(.008,.008,.005),'Nickel',.003)

begin('CoffeeTable')
box('Rounded walnut top',(0,.47,0),(1.2,.085,1.7),'Walnut',.025)
box('Inlaid top',(0,.514,0),(1.07,.006,1.57),'Oak',.005)
box('Lower shelf',(0,.17,0),(1.05,.04,1.55),'Walnut')
for x in [-.49,.49]:
 for z in [-.74,.74]:lathe('Turned table leg',[(0,.03),(.04,.04),(.07,.03),(.32,.028),(.40,.05),(.44,.05)],'Walnut',(x,0,z),20)

begin('MusicBox')
box('Carved body',(0,0,0),(.55,.27,.38),'Walnut',.018)
box('Inlaid lid',(0,.15,0),(.58,.045,.4),'Oak',.014)
for s in [-1,1]:
 box('Brass corner',(s*.255,0,-.195),(.025,.24,.012),'Brass',.004)
box('Latch',(0,.02,-.207),(.055,.066,.024),'Brass')
for i in range(12):box('Music grille',(-.17+i*.03,-.04,-.196),(.011,.06,.005),'Black',.002)
lathe('Winding key shaft',[(0,.012),(.07,.012)],'Brass',(.22,-.135,0),12)

begin('KitchenSink')
box('Sink rim',(0,0,0),(1.02,.035,.59),'Nickel')
box('Dark bowl inset',(0,.019,-.005),(.86,.008,.44),'Black',.025)
box('Bowl bottom',(0,.023,-.005),(.74,.01,.33),'Nickel',.025)
pipe('Swan neck faucet',[(.30,.025,.24),(.30,.23,.24),(.30,.33,.16),(.30,.24,.025)],.019,'Nickel')
for x in [.11,.47]:lathe('Tap handle',[(0,.025),(.08,.025),(.09,.035)],'Nickel',(x,0,.23),16)

begin('Radiator')
for i in range(12):
 x=-.53+i*.096
 box('Enamel radiator rib',(x,0,0),(.075,.62,.14),'Enamel',.025)
for y in [-.23,.23]:pipe('Manifold',[(-.6,y,.04),(.6,y,.04)],.035,'Enamel')
pipe('Feed',[(.57,-.23,.04),(.7,-.23,.04),(.7,-.43,.04)],.014,'Nickel')

begin('PictureFrame')
box('Backing',(0,0,0),(.46,.59,.022),'Walnut')
box('Paper mount',(0,0,-.018),(.405,.535,.01),'Paper')
box('Picture opening',(0,0,-.027),(.33,.45,.004),'Sage')
for s in [-1,1]:
 box('Frame stile',(s*.218,0,-.026),(.034,.60,.034),'Walnut')
 box('Frame rail',(0,s*.283,-.026),(.465,.034,.034),'Walnut')

import sys
sys.path.insert(0,str(ROOT/"tools"))
from author_hands import hands_kit
hands_kit(globals())
from author_dressing import dressing_kit
dressing_kit(globals())
from author_exterior import exterior_kit
exterior_kit(globals())
from author_vehicle import vehicle_kit
vehicle_kit(globals())
export()
