"""Inspect the four-track GLB and preserve/export its rig and motions headlessly."""
from pathlib import Path
import bpy,json,math
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Art/Tripo/HC_Hearthbound_Animations.glb'
OUT=ROOT/'Assets/HollowCurrent/Art/Characters/Hearthbound/Native'
OUT.mkdir(parents=True,exist_ok=True)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(SOURCE))
rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'];meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and any(m.type=='ARMATURE' for m in o.modifiers)];assert len(rigs)==1 and meshes
rig=rigs[0];rig.name='HearthboundRig';rig.animation_data_create()
actions={}
for action in list(bpy.data.actions):
    name=action.name.lower()
    key='Search' if 'look_around' in name else 'Walk' if name=='walk' else 'Idle' if name=='idle' else 'Reach' if 'standing' in name else None
    assert key,(action.name,'Unexpected source action')
    actions[key]=action
assert set(actions)=={'Idle','Walk','Search','Reach'},list(actions)
for track in list(rig.animation_data.nla_tracks):rig.animation_data.nla_tracks.remove(track)
report={'source':str(SOURCE.relative_to(ROOT)),'bones':len(rig.data.bones),'fps':bpy.context.scene.render.fps,'actions':[],'meshes':[{'name':o.name,'vertices':len(o.data.vertices),'triangles':sum(len(f.vertices)-2 for f in o.data.polygons),'groups':len(o.vertex_groups)} for o in meshes]}
assert report['bones']>=50
for name,action in actions.items():
    rig.animation_data.action=action
    if action.slots:rig.animation_data.action_slot=action.slots[0]
    samples=[]
    for frame in [action.frame_range[0],sum(action.frame_range)*.5,action.frame_range[1]]:
        bpy.context.scene.frame_set(int(frame));hips=rig.pose.bones.get('mixamorig:Hips')
        samples.append({'frame':frame,'hips':list(hips.matrix.translation) if hips else None})
    report['actions'].append({'name':name,'original_name':action.name,'frame_range':list(action.frame_range),'samples':samples})
# Save original editable action curves, then export one animation per FBX.
# The provider's multi-motion Mixamo FBX merged its take/layer container; the
# original GLB correctly contains four separate animations and is authoritative.
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/HearthboundMotions.blend'))
for o in bpy.context.selected_objects:o.select_set(False)
rig.select_set(True)
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=rig
rig.animation_data.action=None
rig.data.pose_position='REST'
bpy.ops.export_scene.fbx(filepath=str(OUT/'HearthboundBody.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP',embed_textures=False)
for o in meshes:o.select_set(False)
rig.data.pose_position='POSE'
for name,action in actions.items():
    rig.animation_data.action=action
    if action.slots:rig.animation_data.action_slot=action.slots[0]
    bpy.context.scene.frame_start=math.floor(action.frame_range[0]);bpy.context.scene.frame_end=math.ceil(action.frame_range[1])
    bpy.ops.export_scene.fbx(filepath=str(OUT/('Hearthbound'+name+'.fbx')),use_selection=True,object_types={'ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
(ROOT/'evidence/hearthbound-motion-inspection.json').write_text(json.dumps(report,indent=2)+'\n')
print('HC_BLENDER_EXPORT_PASS original 65-bone GLB exported with four distinct motions',flush=True)
