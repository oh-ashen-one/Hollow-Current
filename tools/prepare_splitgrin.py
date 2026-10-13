"""Preserve Customuse source; author a skinned performance mesh and horror motions.
CPU-only Blender authoring, no render, network or UI.
"""
from pathlib import Path
import bpy,json,math
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Art/Customuse/SplitGrin/HC_SplitGrin_Customuse.glb'
OUT=ROOT/'Assets/HollowCurrent/Art/Characters/SplitGrin'
(OUT/'Native').mkdir(parents=True,exist_ok=True);(OUT/'Maps').mkdir(exist_ok=True)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(SOURCE))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert meshes
bpy.ops.object.select_all(action='DESELECT')
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join();body=bpy.context.object;body.name='SplitGrinBody'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
coords=[v.co.copy() for v in body.data.vertices];lo=Vector(tuple(min(v[i] for v in coords) for i in range(3)));hi=Vector(tuple(max(v[i] for v in coords) for i in range(3)))
scale=1.98/(hi.z-lo.z);center=(hi+lo)/2
for v in body.data.vertices:v.co=(v.co-Vector((center.x,center.y,lo.z)))*scale
triangles=sum(len(f.vertices)-2 for f in body.data.polygons)
if triangles>50000:
 dec=body.modifiers.new('Performance mesh 50k','DECIMATE');dec.ratio=50000/triangles;bpy.ops.object.modifier_apply(modifier=dec.name)
# Weld positional seams for the heat solver; UVs remain per face corner.
bpy.context.view_layer.objects.active=body;body.select_set(True)
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.remove_doubles(threshold=.0001);bpy.ops.object.mode_set(mode='OBJECT')
for f in body.data.polygons:f.use_smooth=True
# Extract exact source maps; engine conversion below independently reads the GLB.
# New skeleton is fitted to this lowered-arm, tilted-head reference rather than
# binding it to the unrelated previous character's T-pose.
arm=bpy.data.armatures.new('SplitGrinSkeleton');rig=bpy.data.objects.new('SplitGrinRig',arm);bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig;rig.select_set(True);body.select_set(False);bpy.ops.object.mode_set(mode='EDIT')
def bone(name,a,b,parent=None,deform=True):
 x=arm.edit_bones.new(name);x.head=a;x.tail=b;x.use_deform=deform
 if parent:x.parent=arm.edit_bones[parent]
 return x
bone('Root',(0,0,0),(0,0,.12),deform=False)
bone('Hips',(0,0,.88),(0,0,1.06),'Root');bone('Spine',(0,0,1.06),(0,0,1.27),'Hips');bone('Chest',(0,0,1.27),(0,0,1.48),'Spine');bone('Neck',(0,0,1.48),(-.035,0,1.64),'Chest');bone('Head',(-.035,0,1.64),(-.09,0,1.96),'Neck')
for side,sign in [('Left',1),('Right',-1)]:
 bone(side+'Clavicle',(0,0,1.46),(sign*.19,0,1.46),'Chest')
 bone(side+'UpperArm',(sign*.19,0,1.46),(sign*.30,-.01,1.13),side+'Clavicle')
 bone(side+'Forearm',(sign*.30,-.01,1.13),(sign*.40,-.035,.88),side+'UpperArm')
 bone(side+'Hand',(sign*.40,-.035,.88),(sign*.43,-.055,.76),side+'Forearm')
 for finger in range(5):
  base=Vector((sign*(.39+finger*.014),-.055-(finger%2)*.015,.77))
  parent=side+'Hand'
  for joint in range(3):
   name=f'{side}Finger{finger}_{joint}';end=base+Vector((sign*.005,-.012,-.027));bone(name,base,end,parent);base=end;parent=name
 bone(side+'Thigh',(sign*.105,0,.94),(sign*.12,0,.53),'Hips')
 bone(side+'Shin',(sign*.12,0,.53),(sign*.12,0,.12),side+'Thigh')
 bone(side+'Foot',(sign*.12,0,.12),(sign*.12,-.16,.065),side+'Shin')
 bone(side+'Toe',(sign*.12,-.16,.065),(sign*.12,-.23,.045),side+'Foot')
bpy.ops.object.mode_set(mode='OBJECT')
# Bone-heat weights respect the connected body surface instead of assigning
# nearby skirt vertices to the wrists solely by Euclidean proximity.
body.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
for b in arm.bones:
 if 'Finger' in b.name:b.use_deform=False
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
assert all(sum(g.weight for g in v.groups)>.01 for v in body.data.vertices),'Bone heat left unweighted vertices'
rig.animation_data_create();scene=bpy.context.scene;scene.render.fps=30
for name,seconds in [('Idle',3),('Walk',.66),('Search',1.4),('Reach',1.15)]:
 action=bpy.data.actions.new(name);rig.animation_data.action=action
 end=round(seconds*30)
 for frame in range(0,end+1,1):
  phase=frame/end*2*math.pi
  for b in rig.pose.bones:b.rotation_mode='XYZ';b.rotation_euler=(0,0,0);b.location=(0,0,0)
  rig.pose.bones['Chest'].rotation_euler.x=math.radians(14)
  if name=='Idle':
   rig.pose.bones['Head'].rotation_euler.y=math.radians(7*math.sin(phase));rig.pose.bones['LeftUpperArm'].rotation_euler.x=.04*math.sin(phase*3)
  elif name=='Walk':
   # Alternating planted drive and high recovery, with a continuous pelvis.
   # Uneven shoulders/neck create the convulsion; root translation stays smooth.
   for side,sign in [('Left',1),('Right',-1)]:
    stride=phase+(0 if sign==1 else math.pi)
    rig.pose.bones[side+'Thigh'].rotation_euler.x=.72*math.sin(stride)
    rig.pose.bones[side+'Shin'].rotation_euler.x=-1.12*max(0,math.sin(stride))
    rig.pose.bones[side+'Foot'].rotation_euler.x=.28*math.sin(stride+.4)
    rig.pose.bones[side+'UpperArm'].rotation_euler.x=-.62-sign*.28*math.sin(phase)
    rig.pose.bones[side+'UpperArm'].rotation_euler.y=sign*.18
    rig.pose.bones[side+'Forearm'].rotation_euler.x=.42+.24*math.sin(stride+.8)
    rig.pose.bones[side+'Hand'].rotation_euler.y=sign*.42
   jerk=math.sin(phase*3)**9
   rig.pose.bones['Hips'].location.z=-.08+.045*(1-math.cos(phase*2))
   rig.pose.bones['Spine'].rotation_euler.x=.55
   rig.pose.bones['Chest'].rotation_euler.x=.52+.06*jerk
   rig.pose.bones['Chest'].rotation_euler.y=.13*math.sin(phase*2)
   rig.pose.bones['Neck'].rotation_euler.x=-.52
   rig.pose.bones['Head'].rotation_euler.x=-.22
   rig.pose.bones['Head'].rotation_euler.y=.22+.15*jerk
   rig.pose.bones['Head'].rotation_euler.z=.12*math.sin(phase*2)**7
   rig.pose.bones['LeftClavicle'].rotation_euler.z=.16*jerk
   rig.pose.bones['RightClavicle'].rotation_euler.z=-.12*math.sin(phase*3+.6)**9
  elif name=='Search':
   rig.pose.bones['Spine'].rotation_euler.x=.32;rig.pose.bones['Chest'].rotation_euler.x=.38;rig.pose.bones['Neck'].rotation_euler.x=-.35;rig.pose.bones['Head'].rotation_euler.z=.48*math.tanh(4*math.sin(phase));rig.pose.bones['Head'].rotation_euler.y=.2*math.sin(phase*3)**7
  else:
   reach=min(1,frame/max(1,end*.35));rig.pose.bones['Chest'].rotation_euler.x=.45*reach
   rig.pose.bones['LeftUpperArm'].rotation_euler.x=-1.35*reach;rig.pose.bones['RightUpperArm'].rotation_euler.x=-1.25*reach
   rig.pose.bones['LeftForearm'].rotation_euler.x=-.35*reach;rig.pose.bones['RightForearm'].rotation_euler.x=-.35*reach;rig.pose.bones['Head'].rotation_euler.y=.16*math.sin(phase*2)
  for b in rig.pose.bones:
   rest=b.bone.matrix_local.to_quaternion();world_delta=b.rotation_euler.to_quaternion()
   b.rotation_euler=(rest.inverted()@world_delta@rest).to_euler('XYZ')
   b.location=rest.inverted()@b.location
  if name=='Walk':
   bpy.context.view_layer.update()
   ankle=min(rig.pose.bones['LeftFoot'].matrix.translation.z,rig.pose.bones['RightFoot'].matrix.translation.z)
   rig.pose.bones['Hips'].location+=arm.bones['Hips'].matrix_local.to_quaternion().inverted()@Vector((0,0,.12-ankle))
  for b in rig.pose.bones:
   b.keyframe_insert('rotation_euler',frame=frame,group=b.name);b.keyframe_insert('location',frame=frame,group=b.name)
rig.animation_data.action=None
for b in rig.pose.bones:b.rotation_euler=(0,0,0);b.location=(0,0,0)
body.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/SplitGrin.blend'))
bpy.ops.export_scene.fbx(filepath=str(OUT/'Native/SplitGrinBody.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
body.select_set(False)
for action in list(bpy.data.actions):
 rig.animation_data.action=action
 if action.slots:rig.animation_data.action_slot=action.slots[0]
 scene.frame_start=int(action.frame_range[0]);scene.frame_end=int(action.frame_range[1])
 bpy.ops.export_scene.fbx(filepath=str(OUT/('Native/SplitGrin'+action.name+'.fbx')),use_selection=True,object_types={'ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
report={'source':str(SOURCE.relative_to(ROOT)),'bones':len(arm.bones),'deform_bones':sum(b.use_deform for b in arm.bones),'triangles':sum(len(f.vertices)-2 for f in body.data.polygons),'vertices':len(body.data.vertices),'motions':['Idle','Walk','Search','Reach'],'rig_method':'Custom fitted skeleton; bone-heat skin weights with animation axes transformed from armature space; original authored animation curves. Needs rendered deformation inspection.'}
pose_samples=[]
rig.animation_data.action=bpy.data.actions['Walk']
if rig.animation_data.action.slots:rig.animation_data.action_slot=rig.animation_data.action.slots[0]
for frame in [0,5,10,15,20]:
 scene.frame_set(frame);bpy.context.view_layer.update()
 chest=rig.pose.bones['Chest'];direction=chest.tail-chest.head
 pose_samples.append({'frame':frame,'head_z':rig.pose.bones['Head'].matrix.translation.z,'chest_forward_degrees':math.degrees(math.atan2(-direction.y,direction.z)),'left_ankle':list(rig.pose.bones['LeftFoot'].matrix.translation),'right_ankle':list(rig.pose.bones['RightFoot'].matrix.translation)})
report['run_pose_samples']=pose_samples
(ROOT/'evidence/splitgrin-rig.json').write_text(json.dumps(report,indent=2)+'\n')
print('HC_BLENDER_EXPORT_PASS SplitGrin',json.dumps(report),flush=True)
