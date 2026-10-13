using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
namespace HollowCurrent.Editor
{
    public static class SplitGrinImport
    {
        const string Source="Assets/HollowCurrent/Art/Characters/SplitGrin/";
        const string Target="Assets/HollowCurrent/Resources/Characters/";
        static readonly string[] Modes={"Idle","Walk","Search","Reach"};
        public static void BuildVerified(){ControllerAudit.Run();NativeBuild.BuildPreparedMac();}
        public static void PrepareAndBake(){Prepare();HouseLighting.Bake();}
        public static void Prepare()
        {
            Directory.CreateDirectory(Target);AssetDatabase.Refresh();
            string bodyPath=Source+"Native/SplitGrinBody.fbx";
            Configure(bodyPath,false);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(bodyPath);
            if(!source)throw new Exception("Original rigged SplitGrin body is missing");
            var root=new GameObject("SplitGrin");
            var carrier=new GameObject("Grounded visual").transform;carrier.SetParent(root.transform,false);
            var body=UnityEngine.Object.Instantiate(source,carrier,false);body.name="Body";
            var skins=body.GetComponentsInChildren<SkinnedMeshRenderer>();
            if(skins.Length!=1||skins[0].bones.Length<20||skins[0].sharedMesh.vertexCount<10000)throw new Exception("SplitGrin requires a real skinned humanoid mesh");
            var material=new Material(Shader.Find("Standard")){name="SplitGrin / original PBR",color=Color.white};
            material.mainTexture=Texture(Source+"Maps/Albedo.png",false,true);
            material.SetTexture("_BumpMap",Texture(Source+"Maps/Normal.png",true,false));material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap",Texture(Source+"Maps/MetallicSmoothness.png",false,false));material.EnableKeyword("_METALLICGLOSSMAP");material.SetFloat("_GlossMapScale",.65f);
            material=Save(material,Target+"SplitGrin.mat");
            foreach(var skin in skins)
            {
                skin.sharedMaterial=material;skin.shadowCastingMode=ShadowCastingMode.On;skin.receiveShadows=true;
                skin.lightProbeUsage=LightProbeUsage.BlendProbes;skin.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;
                skin.localBounds=new Bounds(skin.localBounds.center,skin.localBounds.size*2.5f);
            }
            // Measure the bind mesh, not the expanded animation culling bounds.
            // Imported bind vertices are in renderer-local space. BakeMesh can
            // incorporate FBX scale already; transforming that result again was
            // applying the export's 100x scale twice.
            var points=skins[0].sharedMesh.vertices;var bounds=new Bounds(skins[0].transform.TransformPoint(points[0]),Vector3.zero);
            foreach(var v in points)bounds.Encapsulate(skins[0].transform.TransformPoint(v));
            if(bounds.size.y<.0001f||bounds.size.y>1000)throw new Exception("Invalid imported humanoid height: "+bounds.size.y);
            Debug.Log("HC_CHARACTER_SOURCE_UNITS imported height="+bounds.size.y+"; explicit metre normalization follows");
            float scale=1.98f/bounds.size.y;carrier.localScale=Vector3.one*scale;
            carrier.localPosition=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z)*scale;
            var normalized=new Bounds(skins[0].transform.TransformPoint(points[0]),Vector3.zero);
            foreach(var v in points)normalized.Encapsulate(skins[0].transform.TransformPoint(v));
            if(Mathf.Abs(normalized.size.y-1.98f)>.01f)throw new Exception("Character metre normalization failed: "+normalized.size.y);
            var headTip=body.GetComponentsInChildren<Transform>().First(x=>x.name=="Head");
            var footTip=body.GetComponentsInChildren<Transform>().First(x=>x.name=="LeftToe");
            float skeletonHeight=Mathf.Abs(headTip.position.y-footTip.position.y);
            Debug.Log("HC_CHARACTER_WORLD_SKELETON height="+skeletonHeight);
            if(skeletonHeight<1.4f||skeletonHeight>2.2f)throw new Exception("Mesh/skeleton world scale mismatch: "+skeletonHeight);
            var animator=body.GetComponent<Animator>();if(!animator)animator=body.AddComponent<Animator>();
            var avatar=AvatarBuilder.BuildGenericAvatar(body,"");avatar.name="SplitGrin generic rig";
            if(!avatar.isValid)throw new Exception("SplitGrin avatar could not bind to its exported skeleton");
            animator.avatar=Save(avatar,Target+"SplitGrinAvatar.asset");animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var clips=new AnimationClip[Modes.Length];
            for(int i=0;i<Modes.Length;i++)
            {
                string path=Source+"Native/SplitGrin"+Modes[i]+".fbx";Configure(path,true);
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
                        string leaf=binding.path.Split('/').Last();var t=body.GetComponentsInChildren<Transform>().SingleOrDefault(x=>x.name==leaf);
                        if(!t)throw new Exception("Animation binding is absent from the rig: "+binding.path);
                        corrected.path=AnimationUtility.CalculateTransformPath(t,body.transform);
                        AnimationUtility.SetEditorCurve(clip,binding,null);
                    }
                    // Navigation owns translation. Retain the original vertical
                    // gait and all joint rotations while removing forward travel.
                    if(Modes[i]=="Walk"&&corrected.path.EndsWith("Hips")&&corrected.propertyName.StartsWith("m_LocalPosition."))
                    {
                        // The FBX rig may retain a rotated coordinate basis. Remove
                        // net travel on all position axes without erasing the gait's
                        // vertical bob or lateral sway by assuming an axis convention.
                        float drift=curve.Evaluate(clip.length)-curve.Evaluate(0);var keys=curve.keys;
                        for(int k=0;k<keys.Length;k++){keys[k].value-=drift*keys[k].time/clip.length;keys[k].inTangent-=drift/clip.length;keys[k].outTangent-=drift/clip.length;}
                        curve.keys=keys;
                    }
                    AnimationUtility.SetEditorCurve(clip,corrected,curve);
                }
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=Modes[i]!="Reach";settings.loopBlend=Modes[i]!="Reach";AnimationUtility.SetAnimationClipSettings(clip,settings);
                clips[i]=Save(clip,Target+"SplitGrin"+Modes[i]+".anim");
                Debug.Log("HC_CHARACTER_CLIP "+Modes[i]+" seconds="+clips[i].length+" bindings="+bindings.Length);
            }
            string controllerPath=Target+"SplitGrin.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.parameters=new[]{new AnimatorControllerParameter{name="Mode",type=AnimatorControllerParameterType.Int},new AnimatorControllerParameter{name="LocomotionRate",type=AnimatorControllerParameterType.Float,defaultFloat=1}};
            var machine=controller.layers[0].stateMachine;
            foreach(var t in machine.anyStateTransitions)machine.RemoveAnyStateTransition(t);
            foreach(var state in machine.states)machine.RemoveState(state.state);
            for(int i=0;i<Modes.Length;i++)
            {
                var state=machine.AddState(Modes[i]);state.motion=clips[i];if(i==0)machine.defaultState=state;
                if(i==1){state.speedParameter="LocomotionRate";state.speedParameterActive=true;}
                var transition=machine.AddAnyStateTransition(state);transition.hasExitTime=false;transition.duration=.18f;transition.canTransitionToSelf=false;transition.AddCondition(AnimatorConditionMode.Equals,i,"Mode");
            }
            animator.runtimeAnimatorController=controller;
            var actor=root.AddComponent<HearthboundActor>();actor.Animator=animator;actor.Head=body.GetComponentsInChildren<Transform>().First(x=>x.name=="Head");actor.LeftFoot=body.GetComponentsInChildren<Transform>().First(x=>x.name=="LeftFoot");actor.BoneCount=skins[0].bones.Length;
            PrefabUtility.SaveAsPrefabAsset(root,Target+"SplitGrin.prefab");UnityEngine.Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
            Debug.Log("HC_SPLITGRIN_PASS original skinned humanoid, four independent motions, normalized height 1.9m");
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
        static Texture2D Texture(string path,bool normal,bool srgb)
        {
            var t=AssetImporter.GetAtPath(path) as TextureImporter;if(!t)throw new Exception("Missing original character texture: "+path);
            t.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;t.textureShape=TextureImporterShape.Texture2D;t.sRGBTexture=srgb;t.maxTextureSize=2048;t.mipmapEnabled=true;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
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
