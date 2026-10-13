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
        const string Folder="Assets/HollowCurrent/Resources/Characters/CountClaude/";
        const string Native=Folder+"Native/";
        // Animator state names, index == Mode int: 0 Perch, 1 Glide, 2 SearchGlide, 3 Dive, 4 Stagger, 5 Death.
        static readonly string[] Modes={"Perch","Glide","Search","Dive","Stagger","Death"};
        // Perch, Glide and Search loop; Dive, Stagger and Death hold their last frame (Mode stays set until the next Pose call).
        static readonly bool[] Loops={true,true,true,false,false,false};
        [Serializable] class Receipt{public float head_top_m,mesh_height_m;public int triangles,bones;}
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
            material.mainTexture=Texture(Folder+"Maps/Albedo.png",true);
            material.SetFloat("_Metallic",0f);material.SetFloat("_Glossiness",.22f);
            material=Save(material,Folder+"CountClaude.mat");
            foreach(var skin in skins)
            {
                skin.sharedMaterial=material;skin.shadowCastingMode=ShadowCastingMode.On;skin.receiveShadows=true;
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
            foreach(var wing in new[]{"WingL","WingTipL","WingR","WingTipR"})Bone(wing);
            var animator=body.GetComponent<Animator>();if(!animator)animator=body.AddComponent<Animator>();
            var avatar=AvatarBuilder.BuildGenericAvatar(body,"");avatar.name="CountClaude generic rig";
            if(!avatar.isValid)throw new Exception("Count Claude avatar could not bind to its exported skeleton");
            animator.avatar=Save(avatar,Folder+"CountClaudeAvatar.asset");animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var clips=new AnimationClip[Modes.Length];
            for(int i=0;i<Modes.Length;i++)
            {
                string path=Native+"CountClaude"+Modes[i]+".fbx";Configure(path,true);
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
                    // Navigation owns horizontal travel; the clips only lift the hips (hover, collapse) and lean.
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
                var state=machine.AddState(Modes[i]);state.motion=clips[i];if(i==0)machine.defaultState=state;
                if(i==1){state.speedParameter="LocomotionRate";state.speedParameterActive=true;} // wing-beat rate follows glide speed
                var transition=machine.AddAnyStateTransition(state);transition.hasExitTime=false;transition.duration=i==3?.08f:.2f;transition.canTransitionToSelf=false;transition.AddCondition(AnimatorConditionMode.Equals,i,"Mode");
            }
            animator.runtimeAnimatorController=controller;
            var actor=root.AddComponent<HearthboundActor>();actor.Animator=animator;actor.Head=headTip;actor.LeftFoot=Bone("LeftFoot");actor.BoneCount=skins[0].bones.Length;
            // The prefab sits beside the folder so Resources.Load<GameObject>("Characters/CountClaude") resolves it.
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/HollowCurrent/Resources/Characters/CountClaude.prefab");UnityEngine.Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
            Debug.Log("HC_COUNTCLAUDE_PASS skinned rig with wing bones, six independent motions, head top "+receipt.head_top_m+"m, triangles "+receipt.triangles);
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
