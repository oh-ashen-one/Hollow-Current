"""Original Bellwether exterior kit; deterministic CPU mesh authoring, no render.
All dimensions are Unity metres. Branch cross-sections taper along measured
centerlines; lower-detail tree meshes share the same primary silhouette.
"""
def exterior_kit(ns):
    import random
    from mathutils import Vector
    bpy,math=ns['bpy'],ns['math']
    begin,box,pipe,vec=(ns[k] for k in ['begin','box','pipe','vec'])
    mats,materials=ns['mats'],ns['MATERIALS']
    for name,color,smooth,metal in [
        ('Bark',(.12,.095,.067,1),.08,0),
        ('WeatheredTimber',(.27,.26,.21,1),.12,0),
        ('Slate',(.065,.080,.085,1),.18,0),
        ('SlateLight',(.09,.10,.10,1),.16,0),
        ('SlateDark',(.047,.059,.064,1),.20,0),
        ('Gutter',(.23,.25,.24,1),.28,.45)]:
        materials[name]=(color,smooth,metal)
        m=bpy.data.materials.new(name);m.diffuse_color=color;m.use_nodes=True
        bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=color
        bs.inputs['Roughness'].default_value=1-smooth;bs.inputs['Metallic'].default_value=metal;mats[name]=m

    def mesh(name,vertices,faces,material,uv=None,smooth=False):
        data=bpy.data.meshes.new(name);data.from_pydata([vec(v) for v in vertices],[],faces);data.update()
        obj=bpy.data.objects.new(name,data);ns['active'].objects.link(obj);data.materials.append(mats[material])
        if uv:
            layer=data.uv_layers.new(name='Surface coordinates')
            for poly in data.polygons:
                for li in poly.loop_indices:layer.data[li].uv=uv[data.loops[li].vertex_index]
        for face in data.polygons:face.use_smooth=smooth
        return obj

    def beam(name,a,b,width,depth,material):
        midpoint=tuple((a[i]+b[i])*.5 for i in range(3));direction=Vector(vec(tuple(b[i]-a[i] for i in range(3))))
        obj=box(name,midpoint,(width,direction.length,depth),material,.004)
        obj.rotation_mode='QUATERNION';obj.rotation_quaternion=direction.to_track_quat('Z','Y');return obj

    def tube(name,points,radii,segments,phase):
        verts=[];faces=[];uv=[];length=0
        for i,p in enumerate(points):
            if i:length+=(Vector(p)-Vector(points[i-1])).length
            tangent=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])
            tangent.normalize();axis=Vector((0,0,1))
            if abs(tangent.dot(axis))>.9:axis=Vector((1,0,0))
            across=tangent.cross(axis).normalized();other=tangent.cross(across).normalized()
            for j in range(segments):
                angle=j*math.tau/segments
                radius=radii[i]*(1+.10*math.sin(angle*5+phase)+.045*math.sin(angle*9+i*.6))
                q=Vector(p)+radius*(math.cos(angle)*across+math.sin(angle)*other)
                verts.append(tuple(q));uv.append((j/segments,length))
        for i in range(len(points)-1):
            for j in range(segments):
                a=i*segments+j;b=i*segments+(j+1)%segments;faces.append((a,b,b+segments,a+segments))
        faces.extend([tuple(reversed(range(segments))),tuple((len(points)-1)*segments+j for j in range(segments))])
        return mesh(name,verts,faces,'Bark',uv,True)

    for variant in range(3):
        rng=random.Random(410+variant)
        # Freeze the complete skeleton once, then reduce radial detail and omit
        # only the finest twigs for the distant mesh.
        height=6.6+variant*.45;branches=[]
        trunk=[(0,0,0),(.025,.22,.025),(-.03,1.2,.01),(.08,2.3,-.035),(.17,3.6,.06),(.06,4.9,.12),(.24,height,.02)]
        branches.append(('Fluted trunk',trunk,[.30,.235,.19,.155,.115,.070,.012],16,0))
        for i in range(6):
            angle=i*math.tau/6+.25
            branches.append(('Root flare',[(0,.18,0),(.35*math.cos(angle),.08,.35*math.sin(angle)),(.65*math.cos(angle),.005,.65*math.sin(angle))],[.09,.05,.012],8,i))
        for i in range(10):
            angle=i*2.39996+variant*.42;h=2.15+i*.33
            length=2.6-i*.115+rng.uniform(-.22,.22)
            origin=(.08,h,0)
            end=(math.cos(angle)*length,h+1.1+rng.uniform(-.12,.5),math.sin(angle)*length)
            bend=(end[0]*.55,h+.25,end[2]*.55)
            branches.append(('Crown bough',[origin,bend,end],[.095-i*.004,.053-i*.002,.012],10,angle))
            for j in range(4):
                t=.38+j*.18;start=tuple(bend[k]*(1-t)+end[k]*t for k in range(3))
                a=angle+(-1 if j%2 else 1)*(.5+rng.random()*.45)
                reach=.7+rng.random()*.65
                tip=(start[0]+math.cos(a)*reach,start[1]+.65+rng.random()*.6,start[2]+math.sin(a)*reach)
                middle=tuple(start[k]+(tip[k]-start[k])*.55 for k in range(3))
                branches.append(('Branch fork',[start,middle,tip],[.025,.016,.003],7,a))
                for sign in [-1,1]:
                    twig=(tip[0]+math.cos(a+sign*.65)*.45,tip[1]+.38,tip[2]+math.sin(a+sign*.65)*.45)
                    branches.append(('Fine twig',[middle,tip,twig],[.009,.005,.0015],5,a+sign))
        for low in [False,True]:
            begin('WinterTree'+str(variant+1)+('Low' if low else ''))
            for name,points,radii,segments,phase in branches:
                if low and name=='Fine twig':continue
                tube(name,points,radii,max(5,segments//2) if low else segments,phase)

    def post():
        box('Fence post',(0,.63,0),(.135,1.26,.135),'WeatheredTimber',.009)
        box('Post cap',(0,1.285,0),(.18,.06,.18),'Paint',.012)
    begin('FencePost');post()
    begin('FencePanel')
    for y in [.35,.91]:box('Continuous fence rail',(0,y,.04),(1.6,.09,.065),'WeatheredTimber',.006)
    for i in range(10):
        x=-.72+i*.16;bottom=.10;shoulder=1.18+(i%3)*.012;tip=shoulder+.07
        profile=[(x-.053,bottom),(x+.053,bottom),(x+.053,shoulder),(x,tip),(x-.053,shoulder)]
        verts=[(px,py,z) for z in [-.04,.015] for px,py in profile]
        faces=[(4,3,2,1,0),(5,6,7,8,9)]
        for j in range(5):faces.append((j,(j+1)%5,(j+1)%5+5,j+5))
        obj=mesh('Pointed timber picket',verts,faces,'WeatheredTimber')
        bevel=obj.modifiers.new('Worn picket edges','BEVEL');bevel.width=.003;bevel.segments=2
        obj.modifiers.new('Picket face normals','WEIGHTED_NORMAL')

    begin('HouseRoof')
    half=8.38;depth=14.8;rise=2.20
    slate_parts={}
    cube_faces=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    # Solid substrate plus physical shingle courses. Each course is staggered;
    # the small offsets are deterministic and never affect house collision.
    for side in [-1,1]:
        a=Vector((side*half,0,0));b=Vector((0,rise,0));direction=b-a
        slope=direction.normalized();normal=Vector((side*rise,half,0)).normalized()
        for row in range(27):
            distance=(row+.5)*direction.length/27;center=a+slope*distance
            for col in range(40):
                z=-depth/2+(col+.5)*depth/40
                if row%2:z+=depth/160
                p=center+normal*(.022+row*.0004)
                material=['Slate','SlateLight','SlateDark'][(row*11+col*7)%7%3]
                vertices,faces=slate_parts.setdefault(material,([],[]));offset=len(vertices)
                along=Vector((half,-side*rise,0)).normalized()
                centerpoint=Vector((p.x,p.y,z))
                for sx,sy,sz in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]:
                    vertices.append(tuple(centerpoint+along*(sx*.185)+normal*(sy*.016)+Vector((0,0,sz*(depth/40-.008)*.5))))
                faces.extend(tuple(offset+j for j in face) for face in cube_faces)
        # Watertight six-face prism beneath the shingle courses.
        top=[(side*half,0,-depth/2),(0,rise,-depth/2),(0,rise,depth/2),(side*half,0,depth/2)]
        verts=top+[(x,y-.10,z) for x,y,z in top]
        faces=[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)]
        if side<0:faces=[tuple(reversed(face)) for face in faces]
        mesh('Roof substrate',verts,faces,'SlateDark')
        beam('Eaves fascia',(side*half,-.07,-depth/2),(side*half,-.07,depth/2),.16,.14,'Paint')
        for z in [-depth/2,depth/2]:beam('Gable bargeboard',(side*half,.03,z),(0,rise+.03,z),.14,.15,'Paint')
        pipe('Gutter outer lip',[(side*(half+.1),-.07,-depth/2),(side*(half+.1),-.07,depth/2)],.035,'Gutter')
        pipe('Rainwater downpipe',[(side*(half+.02),-.08,-6.95),(side*(half+.02),-.42,-6.95),(side*8.14,-.7,-6.95),(side*8.14,-5.95,-6.95),(side*8.42,-6.2,-6.95)],.055,'Gutter')
        # Concave open gutter, attached below the eave.
        verts=[];faces=[]
        for z in [-depth/2,depth/2]:
            for j in range(9):
                angle=j*math.pi/8;verts.append((side*(half+.025)+.085*math.cos(angle),-.07-.085*math.sin(angle),z))
        for j in range(8):faces.append((j,j+1,j+10,j+9))
        gutter=mesh('Open half-round gutter',verts,faces,'Gutter',smooth=True)
        wall=gutter.modifiers.new('Gutter sheet thickness','SOLIDIFY');wall.thickness=.003
    for material,(vertices,faces) in slate_parts.items():mesh('Overlapping slate courses',vertices,faces,material)
    for z in [-7.04,7.04]:
        mesh('Closed triangular gable',[(-8,0,z),(8,0,z),(0,2.10,z)],[(0,1,2),(2,1,0)],'Paint')
        for row in range(14):
            y=(row+.5)*.15;width=16*(1-(y+.065)/2.10)
            if width>0:box('Gable clapboard',(0,y,z+(-.018 if z<0 else .018)),(width,.15,.045),'Paint',.002)
        # Small louver provides a focal detail on the otherwise blank gable.
        box('Attic vent backing',(0,.70,z+(-.055 if z<0 else .055)),(.62,.53,.03),'Black',.025)
        for i in range(7):box('Attic vent louver',(0,.48+i*.07,z+(-.079 if z<0 else .079)),(.62,.04,.045),'WeatheredTimber',.002)
    pipe('Rounded ridge cap',[(0,rise+.04,-7.5),(0,rise+.04,7.5)],.075,'SlateDark')
