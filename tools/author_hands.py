"""Shared into the original Blender kit authoring context; all dimensions metres.
Articulated glove components retain visible seams and distinct padding. Runtime
joint transforms assemble them into an editable first-person hand rig.
"""
# Called by author_house_kit before export(); no separate engine or renderer.
def hands_kit(ns):
    bpy=ns['bpy'];begin=ns['begin'];box=ns['box'];pipe=ns['pipe'];lathe=ns['lathe'];vec=ns['vec'];mats=ns['mats'];MATERIALS=ns['MATERIALS']
    for name,color,smooth in [('Glove',(.048,.041,.029,1),.32),('Seam',(.14,.12,.08,1),.14),('Cuff',(.085,.105,.095,1),.08),('Sleeve',(.16,.185,.16,1),.12)]:
        MATERIALS[name]=(color,smooth,0);m=bpy.data.materials.new(name);m.diffuse_color=color;m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=color;bs.inputs['Roughness'].default_value=1-smooth;mats[name]=m
    begin('GlovePalm')
    box('Contoured palm',(0,0,0),(.072,.091,.036),'Glove',.016)
    box('Thenar padding',(-.024,-.019,-.012),(.024,.050,.015),'Glove',.008)
    for i in range(3):
        x=-.021+i*.021
        box('Knuckle padding',(x,.024,.017),(.018,.027,.011),'Glove',.006)
        pipe('Back stitch',[(x,-.024,.019),(x,.015,.025),(x,.038,.019)],.00075,'Seam')
    pipe('Palm perimeter',[(-.031,-.034,-.01),(-.036,.016,-.004),(-.022,.044,-.003),(.030,.037,-.002),(.034,-.026,-.008)],.0009,'Seam')
    begin('GloveFinger')
    box('Finger leather',(0,.017,0),(.0175,.035,.020),'Glove',.008)
    for side in [-1,1]:pipe('Finger side stitch',[(side*.007,.002,.006),(side*.007,.018,.008),(side*.006,.032,.007)],.00055,'Seam')
    begin('GloveTip')
    box('Rounded fingertip',(0,.014,0),(.0165,.028,.019),'Glove',.008)
    pipe('Tip seam',[(-.006,.003,.008),(-.005,.020,.008),(0,.026,.007),(.005,.020,.008),(.006,.003,.008)],.00055,'Seam')
    begin('JacketSleeve')
    lathe('Ribbed cuff',[(-.015,.032),(-.035,.035),(-.065,.036),(-.079,.039)],'Cuff',segments=32)
    for y in [-.023,-.032,-.041,-.05,-.059,-.068]:lathe('Cuff rib',[(y,.035),(y-.002,.036)],'Cuff',segments=32)
    lathe('Waxed cotton sleeve',[(-.067,.040),(-.095,.046),(-.125,.043),(-.16,.05),(-.20,.051),(-.245,.058),(-.30,.065),(-.37,.072)],'Sleeve',segments=32)
    for y,r in [(-.102,.047),(-.148,.05),(-.185,.052)]:lathe('Soft sleeve fold',[(y+.006,r-.003),(y,r+.003),(y-.009,r-.002)],'Sleeve',segments=32)
