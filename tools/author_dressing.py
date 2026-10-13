"""Original measured domestic meshes, shared by the CPU Blender authoring pass."""
def dressing_kit(ns):
    bpy,math=ns['bpy'],ns['math'];begin,box,pipe,lathe,vec=(ns[k] for k in ['begin','box','pipe','lathe','vec']);mats,MATERIALS=ns['mats'],ns['MATERIALS']
    for name,c,s in [('Clay',(.38,.17,.085,1),.08),('Leaf',(.095,.19,.07,1),.28),('LeafLight',(.18,.27,.08,1),.23),('Soil',(.025,.019,.01,1),.03),('Coffee',(.022,.013,.006,1),.6),('Screen',(.025,.055,.047,1),.68)]:
        MATERIALS[name]=(c,s,0);m=bpy.data.materials.new(name);m.diffuse_color=c;m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=c;bs.inputs['Roughness'].default_value=1-s;mats[name]=m
    begin('Mug')
    lathe('Glazed cup',[(0,.044),(.012,.050),(.115,.058),(.13,.057),(.132,.053),(.118,.051),(.025,.042)],'Ivory')
    pipe('Loop handle',[(.048,.107,0),(.089,.111,0),(.099,.069,0),(.073,.025,0),(.045,.036,0)],.011,'Ivory')
    lathe('Coffee surface',[(.112,.051),(.113,.051)],'Coffee')
    begin('HousePlant')
    lathe('Terracotta pot',[(0,.085),(.012,.09),(.22,.145),(.235,.158),(.265,.16),(.27,.146),(.24,.136)],'Clay')
    lathe('Pot soil',[(.243,.13),(.244,.13)],'Soil')
    for i in range(22):
        a=i*2.39996;h=.36+(i%7)*.062;r=.12+(i%3)*.05
        base=(0,.24,0);end=(math.cos(a)*r,h,math.sin(a)*r)
        pipe('Leaf petiole',[base,(end[0]*.35,h*.84,end[2]*.35),end],.0025,'Leaf')
        length=.18+(i%4)*.018
        def point(t,side):
            cx=end[0]+math.cos(a)*length*t;cz=end[2]+math.sin(a)*length*t;cy=h+.075*math.sin(t*math.pi)-.11*t*t
            w=.043*math.sin(t*math.pi)**.65
            return vec((cx-math.sin(a)*w*side,cy+.014*(1-abs(side))*math.sin(t*math.pi),cz+math.cos(a)*w*side))
        # One vertex at each tapered tip. Three coincident tip vertices made
        # zero-area quads whose solidified rims folded back onto themselves.
        verts=[point(0,0)];faces=[(0,2,1),(0,3,2)]
        for j in range(1,8):
            for side in [-1,0,1]:verts.append(point(j/8,side))
        verts.append(point(1,0))
        for j in range(6):
            for k in range(2):
                q=1+j*3+k;faces.append((q,q+1,q+4,q+3))
        faces.extend([(19,20,22),(20,21,22)])
        mesh=bpy.data.meshes.new('Leaf blade');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Curved leaf',mesh);ns['active'].objects.link(o);mesh.materials.append(mats['LeafLight' if i%4==0 else 'Leaf'])
        for face in mesh.polygons:face.use_smooth=True
        mod=o.modifiers.new('Leaf thickness','SOLIDIFY');mod.thickness=.0012
    begin('Television')
    box('Molded casing',(0,.34,0),(1.05,.68,.55),'Black',.055)
    box('Screen surround',(-.06,.35,-.281),(.84,.55,.034),'Walnut',.032)
    box('Convex glass face',(-.06,.35,-.304),(.77,.48,.038),'Screen',.04)
    for y in [.44,.53]:pipe('Tuning knob',[(.425,y,-.28),(.425,y,-.322)],.023,'Nickel')
    for i in range(11):box('Speaker grille',(.429,.17+i*.015,-.289),(.085,.005,.01),'Nickel',.001)
    for x in [-.36,.36]:box('Rubber foot',(x,.009,0),(.14,.028,.32),'Black')
