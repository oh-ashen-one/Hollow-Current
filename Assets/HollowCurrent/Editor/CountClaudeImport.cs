using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
namespace HollowCurrent.Editor
{
    // Count Claude, the Haunted Castle hunter. Mirrors SplitGrinImport: a skinned generic rig plus one FBX per motion,
    // an Animator with Any-State transitions driven by the Mode int, and a prefab carrying HearthboundActor.
    // The FBX files, texture and receipt are produced by tools/count_prepare.py (headless Blender).
    // Entry point for batch mode: -executeMethod HollowCurrent.Editor.CountClaudeImport.Prepare
    public static class CountClaudeImport
    {
        // Generated, shipped assets (prefab beside this folder, controller, clips, material, avatar).
        const string Folder="Assets/HollowCurrent/Resources/Characters/CountClaude/";
        // Sources (FBX, albedo, receipt) live outside Resources so they do not ship in the player.
        const string Source="Assets/HollowCurrent/Art/Characters/CountClaude/";
        const string Native=Source+"Native/";
        // Animator state names, index == Mode int: 0 Perch, 1 Glide, 2 SearchGlide, 3 Dive, 4 Stagger, 5 Death, 6 Grab (wing-wrap),
        // 7 Crawl, 8 GrabDrop (falls from above onto the camera), 9 GrabCrawl (low lunge from the crawl, rears up on the lens).
        static readonly string[] Modes={"Perch","Glide","Search","Dive","Stagger","Death","Grab","Crawl","GrabDrop","GrabCrawl"};
        // Perch, Glide, Search, Dive and Crawl loop; Stagger, Death, Grab, GrabDrop and GrabCrawl hold their last frame (Mode stays set until the next Pose call).
        static readonly bool[] Loops={true,true,true,true,false,false,false,true,false,false};
        // Modes 0-7 are required; 8 and 9 (tools/count_prepare.py, F5) degrade silently when their FBX is absent: no state is added and
        // the runtime falls back to the Mode 6 wing-wrap (it reads CountClaudeModes.json beside the controller to see which exist).
        const int RequiredModes=8;
        // Round 2 wing rig (tools/count_prepare.py): a root at each shoulder blade, a five-bone fan sharing that pivot, and
        // zero-weight end markers the runtime reads for the posed silhouette (HearthboundActor.PoseBounds). Round 3: each fan
        // bone has an Ext child that stretches its finger (translation only) and the single-sheet membrane is doubled.
        static readonly string[] WingBones={"WingL","WingR","WingFanL0","WingFanL4","WingFanR0","WingFanR4","WingFanL0End","WingFanL4End","WingFanR0End","WingFanR4End","WingFanL4Ext","WingFanR4Ext","Jaw"};
        [Serializable] class Receipt{public float head_top_m,mesh_height_m,grab_reach_m,grab_drop_reach_m,grab_crawl_reach_m,shoulder_width_m,rest_span_m;public int triangles,bones,wing_vertices,body_tris,membrane_tris,flesh_tris;public int[] modes;}
        public static void Prepare()
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            string bodyPath=Native+"CountClaudeBody.fbx";
            var receipt=JsonUtility.FromJson<Receipt>(File.ReadAllText(Native+"CountClaude.json"));
            if(receipt==null||receipt.mesh_height_m<1||receipt.triangles<1)throw new Exception("CountClaude.json receipt is missing or invalid; run tools/count_prepare.py");
            Configure(bodyPath,false);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(bodyPath);
            if(!source)throw new Exception("Rigged Count Claude body is missing: "+bodyPath);
            var root=new GameObject("CountClaude");
            var carrier=new GameObject("Grounded visual").transform;carrier.SetParent(root.transform,false);
            var body=UnityEngine.Object.Instantiate(source,carrier,false);body.name="Body";
            var skins=body.GetComponentsInChildren<SkinnedMeshRenderer>();
            if(skins.Length!=1||skins[0].bones.Length<24||skins[0].sharedMesh.vertexCount<10000)throw new Exception("Count Claude requires one skinned humanoid mesh with wing bones");
            var material=new Material(Shader.Find("Standard")){name="CountClaude / albedo",color=Color.white};
            material.mainTexture=Texture(Source+"Maps/Albedo.png",true);
            // Round 8: ember eyes (tools/count_project_refs.py emission map), so the skull reads at hunt distances.
            if(File.Exists(Source+"Maps/Emission.png")){material.EnableKeyword("_EMISSION");material.SetTexture("_EmissionMap",Texture(Source+"Maps/Emission.png",true));material.SetColor("_EmissionColor",new Color(6f,6f,6f));material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;}
            material.SetFloat("_Metallic",0f);material.SetFloat("_Glossiness",.22f);
            material=Save(material,Folder+"CountClaude.mat");
            // Round 9: Unity's submesh order need not follow Blender's material slots (r8 put the membrane/flesh gore on the body).
            // Name a material per part and match each submesh to its part by triangle count from the receipt; Presence then
            // picks the CastleArt.Gore kind by material name.
            Material Part(string name){var m=new Material(material){name="CountClaude "+name};m.DisableKeyword("_EMISSION");m.SetTexture("_EmissionMap",null);m.SetColor("_EmissionColor",Color.black);return Save(m,Folder+"CountClaude"+char.ToUpper(name[0])+name.Substring(1)+".mat");}
            var membraneMat=Part("membrane");var fleshMat=Part("flesh");
            foreach(var skin in skins)
            {
                // Submesh 0 = body/coat (this material, with the ember emission), 1 = skin-wing membrane, 2 = flayed flesh; all get the
                // albedo material here, and Presence swaps CastleArt.Gore(Membrane / Flesh) onto 1 and 2 at runtime in the castle.
                var mesh=skin.sharedMesh;var mats=new Material[Mathf.Max(1,mesh.subMeshCount)];
                for(int i=0;i<mats.Length;i++)
                {
                    int tris=i<mesh.subMeshCount?(int)(mesh.GetSubMesh(i).indexCount/3):0;
                    int db=Mathf.Abs(tris-receipt.body_tris),dm=Mathf.Abs(tris-receipt.membrane_tris),df=Mathf.Abs(tris-receipt.flesh_tris);
                    mats[i]=receipt.membrane_tris>0&&dm<db&&dm<=df?membraneMat:receipt.flesh_tris>0&&df<db&&df<dm?fleshMat:material;
                    Debug.Log("HC_CHARACTER_SUBMESH "+i+" tris="+tris+" -> "+mats[i].name);
                }
                skin.sharedMaterials=mats;skin.shadowCastingMode=ShadowCastingMode.On;skin.receiveShadows=true;
                skin.lightProbeUsage=LightProbeUsage.BlendProbes;skin.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;
                skin.localBounds=new Bounds(skin.localBounds.center,skin.localBounds.size*2.5f);
            }
            // Measure the bind mesh (wing hooks included), not the animation culling bounds. The receipt holds the
            // authored metre height, so FBX unit-scale quirks are corrected by ratio instead of assumed.
            var points=skins[0].sharedMesh.vertices;var bounds=new Bounds(skins[0].transform.TransformPoint(points[0]),Vector3.zero);
            foreach(var v in points)bounds.Encapsulate(skins[0].transform.TransformPoint(v));
            if(bounds.size.y<.0001f||bounds.size.y>1000)throw new Exception("Invalid imported height: "+bounds.size.y);
            Debug.Log("HC_CHARACTER_SOURCE_UNITS imported height="+bounds.size.y+"; explicit metre normalization follows");
            float scale=receipt.mesh_height_m/bounds.size.y;carrier.localScale=Vector3.one*scale;
            // Blender already placed the soles at y=0 and the hips over the origin; keep that origin so Presence's transform
            // is where he stands, and only verify it below.
            carrier.localPosition=Vector3.zero;
            var normalized=new Bounds(skins[0].transform.TransformPoint(points[0]),Vector3.zero);
            foreach(var v in points)normalized.Encapsulate(skins[0].transform.TransformPoint(v));
            if(Mathf.Abs(normalized.size.y-receipt.mesh_height_m)>.02f)throw new Exception("Character metre normalization failed: "+normalized.size.y+" vs "+receipt.mesh_height_m);
            if(Mathf.Abs(normalized.min.y)>.03f)throw new Exception("Count Claude soles are not at y=0: "+normalized.min.y);
            var all=body.GetComponentsInChildren<Transform>();
            Transform Bone(string name){var t=all.FirstOrDefault(x=>x.name==name);if(!t)throw new Exception("Rig bone missing: "+name);return t;}
            var headTip=Bone("Head");var footTip=Bone("LeftToeBase");
            float skeletonHeight=Mathf.Abs(headTip.position.y-footTip.position.y);
            Debug.Log("HC_CHARACTER_WORLD_SKELETON height="+skeletonHeight+" head_top="+receipt.head_top_m);
            if(skeletonHeight<1.5f||skeletonHeight>receipt.head_top_m)throw new Exception("Mesh/skeleton world scale mismatch: "+skeletonHeight);
            foreach(var wing in WingBones)Bone(wing);
            float profileReach=CountClaudeProfile.Default.GrabReach;
            if(Mathf.Abs(receipt.grab_reach_m-profileReach)>.08f)Debug.LogWarning("HC_COUNTCLAUDE_GRAB_REACH receipt "+receipt.grab_reach_m+" m differs from CountClaudeProfile.GrabReach "+profileReach+" m; update the profile");
            Debug.Log("HC_CHARACTER_RIG bones="+receipt.bones+" wing_vertices="+receipt.wing_vertices+" rest_span="+receipt.rest_span_m+" shoulder_width="+receipt.shoulder_width_m+" grab_reach="+receipt.grab_reach_m);
            var animator=body.GetComponent<Animator>();if(!animator)animator=body.AddComponent<Animator>();
            var avatar=AvatarBuilder.BuildGenericAvatar(body,"");avatar.name="CountClaude generic rig";
            if(!avatar.isValid)throw new Exception("Count Claude avatar could not bind to its exported skeleton");
            animator.avatar=Save(avatar,Folder+"CountClaudeAvatar.asset");animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var clips=new AnimationClip[Modes.Length];
            for(int i=0;i<Modes.Length;i++)
            {
                string path=Native+"CountClaude"+Modes[i]+".fbx";
                if(i>=RequiredModes&&!File.Exists(path)){Debug.Log("HC_CHARACTER_CLIP_SKIPPED "+Modes[i]+" (no "+path+"; the runtime keeps the Mode 6 grab)");continue;}
                Configure(path,true);
                var imported=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(x=>!x.name.StartsWith("__preview")).ToArray();
                if(imported.Length!=1)throw new Exception("Expected one distinct "+Modes[i]+" motion, found "+imported.Length);
                var clip=new AnimationClip();EditorUtility.CopySerialized(imported[0],clip);clip.name=Modes[i];clip.legacy=false;
                var bindings=AnimationUtility.GetCurveBindings(clip);
                if(bindings.Length<50||clip.length<.5f)throw new Exception("Incomplete "+Modes[i]+" skeletal motion");
                foreach(var binding in bindings)
                {
                    var curve=AnimationUtility.GetEditorCurve(clip,binding);var corrected=binding;
                    if(binding.type==typeof(Transform)&&binding.path.Length>0&&!body.transform.Find(binding.path))
                    {
                        string leaf=binding.path.Split('/').Last();var t=all.SingleOrDefault(x=>x.name==leaf);
                        if(!t)throw new Exception("Animation binding is absent from the rig: "+binding.path);
                        corrected.path=AnimationUtility.CalculateTransformPath(t,body.transform);
                        AnimationUtility.SetEditorCurve(clip,binding,null);
                    }
                    // Presence owns travel and hover height; the clips carry the crouch, lean, collapse and the 0.10 m skim offset.
                    AnimationUtility.SetEditorCurve(clip,corrected,curve);
                }
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=Loops[i];settings.loopBlend=Loops[i];AnimationUtility.SetAnimationClipSettings(clip,settings);
                clips[i]=Save(clip,Folder+"CountClaude"+Modes[i]+".anim");
                Debug.Log("HC_CHARACTER_CLIP "+Modes[i]+" seconds="+clips[i].length+" bindings="+bindings.Length);
            }
            string controllerPath=Folder+"CountClaude.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.parameters=new[]{new AnimatorControllerParameter{name="Mode",type=AnimatorControllerParameterType.Int},new AnimatorControllerParameter{name="LocomotionRate",type=AnimatorControllerParameterType.Float,defaultFloat=1}};
            var machine=controller.layers[0].stateMachine;
            foreach(var t in machine.anyStateTransitions)machine.RemoveAnyStateTransition(t);
            foreach(var state in machine.states)machine.RemoveState(state.state);
            for(int i=0;i<Modes.Length;i++)
            {
                if(!clips[i])continue;
                var state=machine.AddState(Modes[i]);state.motion=clips[i];if(i==0)machine.defaultState=state;
                if(i==1||i==7){state.speedParameter="LocomotionRate";state.speedParameterActive=true;} // wing-beat rate follows glide speed
                var transition=machine.AddAnyStateTransition(state);transition.hasExitTime=false;transition.duration=i==3?.08f:i==6||i==8||i==9?.06f:.2f;transition.canTransitionToSelf=false;transition.AddCondition(AnimatorConditionMode.Equals,i,"Mode");
            }
            animator.runtimeAnimatorController=controller;
            var actor=root.AddComponent<HearthboundActor>();actor.Animator=animator;actor.Head=headTip;actor.LeftFoot=Bone("LeftFoot");actor.BoneCount=skins[0].bones.Length;
            // The prefab sits beside the folder so Resources.Load<GameObject>("Characters/CountClaude") resolves it.
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/HollowCurrent/Resources/Characters/CountClaude.prefab");UnityEngine.Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
            Debug.Log("HC_COUNTCLAUDE_GRABS drop="+(clips[8]?clips[8].length.ToString("0.00")+"s reach "+receipt.grab_drop_reach_m:"absent")+" crawl="+(clips[9]?clips[9].length.ToString("0.00")+"s reach "+receipt.grab_crawl_reach_m:"absent")+" receipt modes="+(receipt.modes==null?"none":string.Join(",",receipt.modes)));
            Debug.Log("HC_COUNTCLAUDE_PASS skinned rig with wing fans, "+clips.Count(c=>c!=null)+" independent motions, head top "+receipt.head_top_m+"m, triangles "+receipt.triangles);
        }
        static void Configure(string path,bool animation)
        {
            var importer=AssetImporter.GetAtPath(path) as ModelImporter;if(!importer)throw new Exception("Missing authored FBX: "+path);
            bool changed=importer.animationType!=ModelImporterAnimationType.Generic||importer.importAnimation!=animation||!importer.preserveHierarchy||importer.materialImportMode!=ModelImporterMaterialImportMode.None||importer.optimizeGameObjects||!importer.isReadable;
            if(!changed)return;
            importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.NoAvatar;
            importer.importAnimation=animation;importer.isReadable=true;importer.preserveHierarchy=true;importer.optimizeGameObjects=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;importer.SaveAndReimport();
        }
        static Texture2D Texture(string path,bool srgb)
        {
            var t=AssetImporter.GetAtPath(path) as TextureImporter;if(!t)throw new Exception("Missing character texture: "+path);
            t.textureType=TextureImporterType.Default;t.textureShape=TextureImporterShape.Texture2D;t.sRGBTexture=srgb;t.maxTextureSize=2048;t.mipmapEnabled=true;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static T Save<T>(T value,string path) where T:UnityEngine.Object
        {
            var old=AssetDatabase.LoadAssetAtPath<T>(path);
            if(old){EditorUtility.CopySerialized(value,old);UnityEngine.Object.DestroyImmediate(value);return old;}
            AssetDatabase.CreateAsset(value,path);return value;
        }
    }
}
