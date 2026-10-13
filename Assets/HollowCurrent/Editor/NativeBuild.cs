using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace HollowCurrent.Editor
{
    public static class NativeBuild
    {
        const string ScenePath="Assets/HollowCurrent/Scenes/Bellwether.unity";
        // One bounded first invocation: no native player build, screenshot, GI bake or Play Mode.
        public static void HeadlessValidate()
        {
            if(!Application.isBatchMode||SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
                throw new Exception("Headless validation requires batch mode and the Null graphics device");
            Debug.Log("HC_HEADLESS_DEVICE Null");
            TripoImport.Prepare();AuthoredImport.Prepare();CharacterImport.Prepare();SplitGrinImport.Prepare();Prepare();ValidateGeometry();Debug.Log("HC_HEADLESS_VALIDATION_PASS");
        }
        [MenuItem("Hollow Current/Prepare scene")]
        public static void Prepare()
        {
            Directory.CreateDirectory("Assets/HollowCurrent/Scenes");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var authored=new World();authored.Build();
            var snapshot=authored.Root.gameObject.AddComponent<WorldLayout>();
            snapshot.Trace=authored.Trace;snapshot.Anchor=authored.Anchor;snapshot.Induction=authored.Induction;snapshot.Voice=authored.Voice;snapshot.Print=authored.Print;
            snapshot.Lamps=authored.Lamps.ToArray();snapshot.WindowLights=authored.WindowLights.ToArray();snapshot.Dolls=authored.Dolls.ToArray();snapshot.Doors=authored.Doors.ToArray();snapshot.SearchPoints=authored.SearchPoints.ToArray();
            snapshot.MaterialNames=new List<string>(authored.Materials.Keys).ToArray();snapshot.Materials=new List<Material>(authored.Materials.Values).ToArray();
            PersistLayoutAssets(authored.Root);
            AssetDatabase.SaveAssets();
            foreach(var component in authored.Root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var script=MonoScript.FromMonoBehaviour(component);
                if(!script||string.IsNullOrEmpty(AssetDatabase.GetAssetPath(script)))throw new Exception("Scene component requires its own persistent script asset: "+component.GetType().Name);
            }
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var reloaded=UnityEngine.Object.FindFirstObjectByType<WorldLayout>();
            if(!reloaded||!reloaded.Trace||reloaded.Doors.Length!=4||reloaded.Materials.Length<18)throw new Exception("Authored scene reload failed");
            foreach(var door in reloaded.Doors)if(!door)throw new Exception("Door did not survive scene serialization");
            Debug.Log("HC_SAVED_SCENE_PASS stable component scripts; scene reopened from disk");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            PlayerSettings.companyName="Bellwether Field Office";PlayerSettings.productName="Hollow Current";
            PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.bundleVersion="0.2.0";PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"com.bellwether.hollowcurrent");
            PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
            PlayerSettings.runInBackground=false;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetArchitecture(UnityEditor.Build.NamedBuildTarget.Standalone,1);
            // Runtime creates Standard materials; retain that shader in standalone builds.
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");var standard=Shader.Find("Standard");bool found=false;
            for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==standard)found=true;
            if(!found){int slot=shaders.arraySize;shaders.InsertArrayElementAtIndex(slot);shaders.GetArrayElementAtIndex(slot).objectReferenceValue=standard;graphics.ApplyModifiedPropertiesWithoutUndo();}
            AssetDatabase.SaveAssets();Debug.Log("HC_SCENE_PREPARED");
        }
        static void PersistLayoutAssets(Transform root)
        {
            const string folder="Assets/HollowCurrent/Art/LayoutData";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            int materialIndex=0,meshIndex=0;
            var remap=new Dictionary<Material,Material>();var data=root.GetComponent<WorldLayout>();
            Material Persist(Material material)
            {
                if(!material||AssetDatabase.Contains(material))return material;
                if(remap.TryGetValue(material,out var known))return known;
                string path=folder+"/Surface-"+(materialIndex++)+".mat";
                var old=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(old){EditorUtility.CopySerialized(material,old);remap[material]=old;return old;}
                AssetDatabase.CreateAsset(material,path);remap[material]=material;return material;
            }
            for(int i=0;i<data.Materials.Length;i++)data.Materials[i]=Persist(data.Materials[i]);
            foreach(var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials=r.sharedMaterials;
                for(int i=0;i<materials.Length;i++)materials[i]=Persist(materials[i]);
                r.sharedMaterials=materials;
                bool moving=r.GetComponentInParent<Door>()||r.GetComponentInParent<Interactable>();
                foreach(var parent in r.GetComponentsInParent<Transform>())if(parent.name=="Porcelain doll"||parent.name=="Authored / Mug")moving=true;
                if(moving)GameObjectUtility.SetStaticEditorFlags(r.gameObject,0);
                if(!moving&&r.enabled)GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);
            }
            foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh=f.sharedMesh;if(!mesh||AssetDatabase.Contains(mesh)||mesh.name=="Cube"||mesh.name=="Sphere"||mesh.name=="Quad")continue;
                string path=folder+"/Mesh-"+(meshIndex++)+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(old){EditorUtility.CopySerialized(mesh,old);f.sharedMesh=old;}else AssetDatabase.CreateAsset(mesh,path);
            }
        }
        [MenuItem("Hollow Current/Build macOS")]
        public static void BuildMac()
        {
            if(Application.isBatchMode&&SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
                throw new Exception("CLI builds must use the Null graphics device");
            Debug.Log("HC_BUILD_DEVICE "+SystemInfo.graphicsDeviceType);
            TripoImport.Prepare();AuthoredImport.Prepare();CharacterImport.Prepare();SplitGrinImport.Prepare();ValidateGeometry();
            Prepare();HouseLighting.ValidateSetup();BuildOpenScene();
        }
        // A baked release must not rerun importers or regenerate the house: both
        // would invalidate saved UVs, lighting, and scene references.
        [MenuItem("Hollow Current/Build prepared macOS release")]
        public static void BuildPreparedMac()
        {
            if(Application.isBatchMode&&SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
                throw new Exception("CLI builds must use the Null graphics device");
            Debug.Log("HC_BUILD_DEVICE "+SystemInfo.graphicsDeviceType);
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            HouseLighting.ValidatePreparedScene();
            var world=new World();world.LoadOrBuild();
            try{Physics.SyncTransforms();world.BuildNavigation();ValidateWorld(world);}
            finally{world.Dispose();}
            // Dispose releases the validation instance. Reopen the exact saved
            // scene before build, without serializing any temporary nav state.
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            HouseLighting.ValidatePreparedScene();BuildOpenScene();
            Debug.Log("HC_PREPARED_BUILD_PASS preserved authored lighting");
        }
        static void BuildOpenScene()
        {
            Directory.CreateDirectory("Builds/macOS");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/macOS/Hollow Current.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.CleanBuildCache});
            Debug.Log("HC_BUILD_RESULT "+report.summary.result+" "+report.summary.totalSize);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Native build failed: "+report.summary.result);
        }
        // Engine-based checks are intentionally separate from safe compiler-only checks.
        // Invoke only after headless engine coordination has been granted.
        public static void ValidateGeometry()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var world=new World();world.Build();Physics.SyncTransforms();world.BuildNavigation();
            try{ValidateWorld(world);}finally{world.Dispose();}
        }
        static void ValidateWorld(World world)
        {
            ValidateFurniture(world);
            foreach(var door in world.Doors)
            {
                door.Toggle();Debug.Log("HC_DOOR_CLEARANCE pivot="+door.transform.position+" angle="+door.OpenAngle+" clear="+door.HasClearOpenPose);
                if(Mathf.Abs(door.OpenAngle)<85||!door.HasClearOpenPose)throw new Exception("Door has no usable clear opening: "+door.transform.position);
            }
            var treeGroups=world.Root.GetComponentsInChildren<LODGroup>();
            if(treeGroups.Length!=14)throw new Exception("Expected fourteen authored tree LOD groups");
            foreach(var group in treeGroups)
            {
                var levels=group.GetLODs();if(levels.Length!=2)throw new Exception("Tree requires near and distant geometry");
                int near=0,far=0;
                foreach(var r in levels[0].renderers)near+=r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3;
                foreach(var r in levels[1].renderers)far+=r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3;
                if(near<=0||far<=0||far>=near*.6f)throw new Exception("Tree LOD does not reduce geometry sufficiently");
            }
            foreach(string asset in new[]{"HouseRoof","FencePanel","FencePost","FieldVanBody","FieldVanWheel"})
                if(!Resources.Load<GameObject>("Architecture/"+asset))throw new Exception("Exterior architecture missing: "+asset);
            Debug.Log("HC_EXTERIOR_PASS authored roof and fence; fourteen trees with verified near/distant meshes");
            foreach(float z in new[]{-11.4f,-9.5f,-8.3f,-7.5f})
                if(Physics.CheckCapsule(new Vector3(0,.30f,z),new Vector3(0,1.50f,z),.27f,~0,QueryTriggerInteraction.Ignore))throw new Exception("Field van aisle obstructed at "+z);
            int vehicleBodies=0,vehicleWheels=0;
            foreach(var t in world.Root.GetComponentsInChildren<Transform>())
            {if(t.name=="Authored / FieldVanBody")vehicleBodies++;if(t.name=="Authored / FieldVanWheel")vehicleWheels++;}
            if(vehicleBodies!=1||vehicleWheels!=4)throw new Exception("Original field vehicle not completely integrated");
            Debug.Log("HC_VEHICLE_PASS original shell and four wheels; cargo/rear aisle clear at player capsule size");
            foreach(float floor in new[]{0f,3.2f})foreach(float x in new[]{-5.2f,5.2f})
            {
                if(!Physics.Raycast(new Vector3(x,floor+1.625f,-1),Vector3.forward,out var hit,2,~0,QueryTriggerInteraction.Ignore)||hit.collider.name!="Front closed window collision")
                    throw new Exception("Front window opening is obstructed or its collision pane is missing: "+new Vector2(x,floor));
            }
            Debug.Log("HC_FACADE_PASS four genuine openings with closed glass collision");
            foreach(string name in DetailArt.Required)
            {
                bool found=false;foreach(var t in world.Root.GetComponentsInChildren<Transform>())if(t.name=="Original detail / "+name)found=true;
                if(!found)throw new Exception("Missing integrated fixture "+name);
            }
            Debug.Log("HC_DETAIL_PASS "+DetailArt.Required.Length+" optimized original room fixture types");
            var destinations=new[]{World.Spawn,new Vector3(0,0,3),new Vector3(-5,0,8),new Vector3(5,0,10),new Vector3(-5,3.2f,5),new Vector3(5,3.2f,10),new Vector3(0,-3.2f,3),new Vector3(5,-3.2f,10),new Vector3(0,0,-7.3f),new Vector3(-6.7f,3.2f,8.55f),new Vector3(-5.1f,-3.2f,4.6f),new Vector3(5.5f,0,-5.8f)};
            if(!NavMesh.SamplePosition(World.Spawn,out var origin,1,NavMesh.AllAreas))throw new Exception("Van has no navigation surface");
            foreach(var point in destinations)
            {
                if(!NavMesh.SamplePosition(point,out var sample,.65f,NavMesh.AllAreas))throw new Exception("No walkable surface: "+point);
                var path=new NavMeshPath();if(!NavMesh.CalculatePath(origin.position,sample.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("Disconnected floor/route: "+point);
            }
            foreach(var item in world.Root.GetComponentsInChildren<Interactable>())
            {
                if(item.Kind!=InteractKind.Hide)continue;
                if(!NavMesh.SamplePosition(item.ExitPoint,out _,.7f,NavMesh.AllAreas))throw new Exception("Hide exit not navigable: "+item.name);
            }
            Debug.Log("HC_GEOMETRY_PASS "+destinations.Length+" destination routes; three hiding exits");
        }
        static void ValidateFurniture(World world)
        {
            if(!world.Materials["floorwood"].mainTexture)throw new Exception("Original oak floor texture missing");
            if(!world.Materials["uvtrace"].mainTexture)throw new Exception("Original UV handprint texture missing");
            string[] required={"Sofa","DiningTable","DiningChair","Wardrobe","ChildBed","ParentsBed","Washer","Refrigerator","Sideboard","Doll","Bookcase","Furnace","Bathtub","InductionReader","UVLamp","EVPRecorder","Flashlight","FieldRadio"};
            foreach(string name in required)
            {
                var prefab=Resources.Load<GameObject>("Furniture/"+name);
                if(!prefab)throw new Exception("Required furniture prefab missing: "+name);
                var meshes=prefab.GetComponentsInChildren<MeshFilter>();
                if(meshes.Length==0)throw new Exception("Furniture contains no meshes: "+name);
                foreach(var mesh in meshes)
                {
                    var renderer=mesh.GetComponent<Renderer>();
                    if(!mesh.sharedMesh||mesh.sharedMesh.vertexCount==0||!renderer||!renderer.sharedMaterial||!renderer.sharedMaterial.mainTexture)
                        throw new Exception("Incomplete furniture mesh/material: "+name);
                }
            }
            int placed=0;
            foreach(var item in world.Root.GetComponentsInChildren<Transform>())if(item.name.StartsWith("Original crafted "))placed++;
            if(placed!=20)throw new Exception("Expected 20 original furniture placements; found "+placed);
            Debug.Log("HC_FURNITURE_PASS 13 textured prefabs; 20 authored placements");
            Debug.Log("HC_FIELD_KIT_PASS 5 original textured tool assets");
        }
    }
}
