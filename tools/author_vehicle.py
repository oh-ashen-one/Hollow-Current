"""Original field-van mesh kit fitted around the existing playable cargo cabin.
CPU authoring only; no render, network, or game-loop changes. The body origin is
cargo-floor center. Rear entrance is local +Z; driver cab extends toward -Z.
"""
def vehicle_kit(ns):
    bpy,math=ns['bpy'],ns['math'];begin,box,pipe,lathe,vec=(ns[k] for k in ['begin','box','pipe','lathe','vec'])
    mats,materials=ns['mats'],ns['MATERIALS']
    for name,color,smooth,metal in [
        ('VehicleCream',(.66,.65,.53,1),.32,.18),('VehicleSage',(.25,.31,.27,1),.30,.18),
        ('VehicleNavy',(.055,.075,.09,1),.28,.22),('TireRubber',(.017,.019,.019,1),.10,0),
        ('CabGlass',(.025,.055,.063,1),.86,.22),('TailLens',(.25,.014,.011,1),.55,.05),
        ('AmberLens',(.56,.20,.028,1),.48,.03),('LampLens',(.61,.60,.49,1),.65,.05)]:
        materials[name]=(color,smooth,metal);m=bpy.data.materials.new(name);m.diffuse_color=color;m.use_nodes=True
        bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=color
        bs.inputs['Roughness'].default_value=1-smooth;bs.inputs['Metallic'].default_value=metal;mats[name]=m

    def surface(name,vertices,faces,material):
        data=bpy.data.meshes.new(name);data.from_pydata([vec(v) for v in vertices],[],faces);data.update()
        obj=bpy.data.objects.new(name,data);ns['active'].objects.link(obj);data.materials.append(mats[material]);return obj

    begin('FieldVanWheel')
    lathe('Pneumatic tire',[(-.13,.285),(-.145,.385),(-.115,.472),(-.06,.49),(.06,.49),(.115,.472),(.145,.385),(.13,.285)],'TireRubber',segments=64)
    lathe('Steel wheel dish',[(.135,.28),(.155,.285),(.17,.265),(.178,.205),(.19,.16),(.20,.055)],'Nickel',segments=48)
    lathe('Hub center',[(.196,.10),(.22,.105),(.24,.085)],'Nickel',segments=40)
    for i in range(8):
        a=i*math.tau/8
        lathe('Wheel bolt',[(.184,.012),(.205,.012)],'Nickel',(.14*math.cos(a),0,.14*math.sin(a)),8)
        lathe('Wheel vent',[(.177,.025),(.181,.025)],'Black',(.225*math.cos(a+.2),0,.225*math.sin(a+.2)),12)
    for i in range(64):
        a=i*math.tau/64
        tread=box('Tread block',(.49*math.cos(a),0,.49*math.sin(a)),(.017,.205,.032),'TireRubber',.002)
        tread.rotation_euler[2]=-a

    begin('FieldVanBody')
    # Cargo cabin: retain the exact established clear interior and floor height.
    box('Cargo floor',(0,-.035,0),(3.7,.07,5.6),'VehicleNavy',.018)
    box('Rubber cargo runner',(0,.004,0),(1.40,.012,4.60),'TireRubber',.009)
    box('Rounded cargo roof',(0,2.65,0),(3.94,.20,5.78),'VehicleCream',.09)
    box('Cab bulkhead interior',(0,1.30,-2.72),(3.54,2.6,.025),'VehicleCream',.012)
    for side in [-1,1]:
        x=side*1.86
        box('Inner washable wall',(side*1.778,1.30,0),(.020,2.6,5.6),'VehicleCream',.009)
        box('Cargo upper outer skin',(side*1.92,1.95,0),(.09,1.36,5.62),'VehicleCream',.035)
        box('Cargo sage band',(side*1.926,1.125,0),(.10,.29,5.62),'VehicleSage',.015)
        # Lower sill sections leave a real wheel-arch gap at the rear axle.
        for z0,z1 in [(-2.8,.97),(2.23,2.8)]:
            box('Lower sill',(side*1.925,.47,(z0+z1)/2),(.10,.94,z1-z0),'VehicleNavy',.018)
        arch=[]
        for i in range(25):
            a=i*math.pi/24;arch.append((side*1.99,.36+.62*math.sin(a),1.60+.62*math.cos(a)))
        pipe('Rolled rear wheel arch',arch,.035,'VehicleNavy')
        pipe('Cargo gutter',[(side*1.97,2.62,-2.82),(side*1.97,2.62,2.82)],.025,'Nickel')
        for z in [-2.77,-1.45,-.1,1.1,2.76]:
            box('Body panel seam',(side*1.974,1.91,z),(.005,1.30,.008),'VehicleNavy',0)
        for z in [-2.70,-1.35,0,1.35,2.70]:
            box('Inner wall rib',(side*1.757,1.3,z),(.035,2.55,.035),'VehicleCream',.008)
        # Open barn doors swing away from the central aisle and rear wheels.
        # They are fixed parked geometry; no animation or new interaction state.
        angle=-side*math.radians(25);center=(side*2.671,1.29,3.198)
        door=box('Open rear door',center,(1.79,2.54,.09),'VehicleCream',.028)
        door.rotation_euler[2]=angle
        inset=box('Rear door inset',(center[0]+math.sin(angle)*.062,center[1],center[2]+math.cos(angle)*.062),(1.55,2.25,.025),'VehicleCream',.018)
        inset.rotation_euler[2]=angle
        for y in [.38,1.18,2.02]:
            box('Rear door hinge',(side*1.90,y,2.82),(.12,.12,.13),'Nickel',.015)
        pipe('Rear door pull',[(side*3.10,1.00,3.46),(side*3.10,1.00,3.51),(side*3.10,1.28,3.51),(side*3.10,1.28,3.46)],.018,'Black')
        box('Rear upright',(x,1.3,2.82),(.13,2.6,.13),'VehicleCream',.035)
        box('Tail lamp bezel',(side*1.72,.34,2.887),(.20,.36,.065),'Black',.035)
        box('Tail lamp lens',(side*1.72,.39,2.932),(.15,.20,.025),'TailLens',.026)
        box('Reverse lamp lens',(side*1.72,.245,2.932),(.15,.06,.025),'LampLens',.012)
        box('Rear upper marker',(side*1.57,2.64,2.895),(.18,.075,.025),'TailLens',.012)
    box('Rear lintel',(0,2.61,2.82),(3.70,.15,.13),'VehicleCream',.035)
    box('Flush threshold',(0,.012,2.79),(3.56,.024,.13),'Nickel',.009)
    for z in [-2,-.7,.6,1.9]:box('Interior roof rib',(0,2.53,z),(3.55,.07,.04),'VehicleCream',.01)
    box('Rear bumper',(0,-.05,2.99),(3.98,.08,.22),'VehicleNavy',.035)
    box('Rear blank registration plate',(0,.15,2.902),(.43,.17,.02),'Ivory',.006)
    # Compact forward cab, separate from the playable sealed bulkhead.
    box('Cab lower formed body',(0,.59,-3.68),(3.48,1.18,1.83),'VehicleCream',.12)
    box('Cab sage lower face',(0,.66,-4.607),(3.30,.39,.055),'VehicleSage',.025)
    box('Cab roof',(0,2.55,-3.54),(3.60,.20,1.70),'VehicleCream',.10)
    for side in [-1,1]:
        x=side*1.75
        vertices=[(x,1.20,-2.91),(x,1.20,-4.55),(x,2.43,-4.10),(x,2.43,-2.91)]
        surface('Cab side glazing',vertices,[(0,1,2,3),(3,2,1,0)],'CabGlass')
        for a,b in zip(vertices,vertices[1:]+vertices[:1]):pipe('Cab window rubber',[a,b],.038,'Black')
        pipe('Door seam',[(side*1.753,.16,-2.94),(side*1.753,1.12,-2.94),(side*1.753,1.12,-4.42),(side*1.753,.16,-4.42)],.009,'VehicleNavy')
        box('Cab handle',(side*1.79,1.07,-3.13),(.05,.052,.19),'Nickel',.015)
        pipe('Mirror arm',[(side*1.75,1.92,-4.15),(side*2.07,1.92,-4.17),(side*2.07,1.65,-4.17)],.025,'Nickel')
        box('Mirror casing',(side*2.07,1.72,-4.15),(.22,.49,.10),'Black',.045)
        box('Mirror face',(side*2.07,1.72,-4.213),(.175,.42,.014),'CabGlass',.025)
        arch=[(side*1.80,.36+.58*math.sin(i*math.pi/24),-3.85+.58*math.cos(i*math.pi/24)) for i in range(25)]
        pipe('Front wheel arch',arch,.04,'VehicleNavy')
        box('Front lamp bezel',(side*1.14,.75,-4.654),(.47,.35,.06),'VehicleNavy',.04)
        box('Headlamp lens',(side*1.14,.76,-4.699),(.37,.25,.026),'LampLens',.03)
        box('Front indicator',(side*1.57,.70,-4.665),(.14,.15,.033),'AmberLens',.015)
    windshield=[(-1.69,1.22,-4.565),(1.69,1.22,-4.565),(1.69,2.44,-4.105),(-1.69,2.44,-4.105)]
    surface('Broad sloped windshield',windshield,[(0,1,2,3),(3,2,1,0)],'CabGlass')
    for a,b in zip(windshield,windshield[1:]+windshield[:1]):pipe('Windshield gasket',[a,b],.043,'Black')
    pipe('Windshield divider',[(0,1.22,-4.60),(0,2.44,-4.14)],.028,'VehicleCream')
    for side in [-1,1]:
        pipe('Wiper arm',[(side*.46,1.28,-4.565),(side*.77,1.64,-4.43)],.012,'Black')
        pipe('Wiper blade',[(side*.49,1.70,-4.405),(side*1.08,1.72,-4.399)],.016,'Black')
    box('Front radiator opening',(0,.60,-4.66),(1.05,.40,.07),'Black',.04)
    for y in [.46,.54,.62,.70,.78]:box('Radiator grille bar',(0,y,-4.705),(1.0,.022,.022),'Nickel',.006)
    box('Front bumper',(0,.15,-4.76),(3.62,.17,.20),'VehicleNavy',.045)
    box('Roof ventilation unit',(0,2.81,-.9),(1.10,.19,.85),'VehicleCream',.065)
    for i in range(8):box('Roof vent louver',(-.38+i*.11,2.925,-.9),(.065,.015,.55),'VehicleNavy',.004)
    lathe('Antenna mount',[(0,.06),(.06,.045)],'Black',(.95,2.76,-2.1),20)
    pipe('Flexible radio antenna',[(.95,2.80,-2.1),(.96,3.20,-2.09),(1.0,3.62,-2.05)],.007,'Black')
