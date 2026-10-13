using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace HollowCurrent.Editor
{
    public static class HouseLighting
    {
        // Per-map scene, receipt and lighting paths come from MapCatalog.
        // Each map has its own receipt (Art/LayoutData/<Map>Bake.json); baking
        // one map never rewrites the other's scene, receipt or lighting assets.
        [Serializable] sealed class BakeReceipt
        {
            public string sceneDependencies,sourceHash,utc,map,hashScope;
            public int lightmaps,lightProbes,reflections;
        }
        // Source files that only the castle depends on. They are excluded from
        // Bellwether's receipt so parallel castle work never stales the house bake.
        static readonly string[] CastleOnly={"Assets/HollowCurrent/Runtime/Castle/","Assets/HollowCurrent/Core/Forge.cs","Assets/HollowCurrent/Editor/CountClaudeImport.cs",
            "Assets/HollowCurrent/Resources/Castle/","Assets/HollowCurrent/Resources/CastleAudio/","Assets/HollowCurrent/Resources/Characters/CountClaude/","Assets/HollowCurrent/Art/Characters/CountClaude/","Assets/HollowCurrent/Art/LayoutData/Castle/"};
        // Refresh authored placement without rebuilding imported meshes and
        // discarding their already-generated secondary UVs.
        public static void PrepareAndBake(){NativeBuild.Prepare();Bake(MapId.Bellwether);}
        public static void PrepareAndBakeCastle(){NativeBuild.PrepareCastle();Bake(MapId.Castle);}
        public static void Bake()=>Bake(MapId.Bellwether);
        public static void BakeCastle()=>Bake(MapId.Castle);
        // Separate from -nographics builds: material extraction and probe
        // capture need the Metal device and the reviewed rendering lease.
        public static void Bake(MapId map)
        {
            var info=MapCatalog.Get(map);string folder=info.LayoutFolder;Directory.CreateDirectory(folder);
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new Exception("Lighting material extraction requires the coordinated graphics device");
            EditorSceneManager.OpenScene(info.ScenePath,OpenSceneMode.Single);
            var layout=UnityEngine.Object.FindFirstObjectByType<WorldLayout>();if(!layout)throw new Exception("Saved authored layout missing: "+info.ScenePath+". Run the map's Prepare first.");
            var lights=layout.GetComponentsInChildren<Light>(true);
            var enabled=lights.ToDictionary(l=>l,l=>l.enabled);
            var bakeTypes=lights.ToDictionary(l=>l,l=>l.lightmapBakeType);
            var materials=layout.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().ToArray();
            var emissionFlags=materials.ToDictionary(m=>m,m=>m.globalIlluminationFlags);
            bool success=false;
            try
            {
                RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.08f,.105f,.14f);
                RenderSettings.ambientEquatorColor=new Color(.035f,.045f,.055f);RenderSettings.ambientGroundColor=new Color(.018f,.02f,.023f);
                foreach(var light in lights)
                {
                    // Castle: the layout's directional moons are baked like the window lights.
                    bool moonlight=Array.IndexOf(layout.WindowLights,light)>=0||(map==MapId.Castle&&light.type==LightType.Directional);
                    light.enabled=moonlight;light.lightmapBakeType=moonlight?LightmapBakeType.Baked:LightmapBakeType.Realtime;
                }
                var oldMoon=layout.transform.Find("Baked overcast moon");if(oldMoon)UnityEngine.Object.DestroyImmediate(oldMoon.gameObject);
                if(map!=MapId.Castle){
                var moonObject=new GameObject("Baked overcast moon");moonObject.transform.SetParent(layout.transform,false);moonObject.transform.rotation=Quaternion.Euler(55,-30,0);
                var moon=moonObject.AddComponent<Light>();moon.type=LightType.Directional;moon.color=new Color(.36f,.51f,.75f);moon.intensity=.16f;moon.shadows=LightShadows.Soft;moon.lightmapBakeType=LightmapBakeType.Baked;}
                // Switchable bulbs must never leave permanently bright bounce
                // or bright reflections after the breaker has been switched off.
                foreach(var material in materials)material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                var visited=new HashSet<Mesh>();int count=0;
                foreach(var filter in layout.GetComponentsInChildren<MeshFilter>())
                {
                    var renderer=filter.GetComponent<MeshRenderer>();if(!renderer||!renderer.enabled||filter.GetComponent<TextMesh>())continue;
                    if(Movable(filter.transform,map))continue;
                    if(renderer.sharedMaterials.Any(m=>m&&m.renderQueue>=(int)RenderQueue.Transparent))
                    {
                        GameObjectUtility.SetStaticEditorFlags(filter.gameObject,GameObjectUtility.GetStaticEditorFlags(filter.gameObject)&~StaticEditorFlags.ContributeGI);
                        renderer.receiveGI=ReceiveGI.LightProbes;renderer.lightProbeUsage=LightProbeUsage.BlendProbes;continue;
                    }
                    var mesh=filter.sharedMesh;
                    if(mesh&&mesh.uv2.Length!=mesh.vertexCount&&visited.Add(mesh))
                    {if(!Unwrapping.GenerateSecondaryUVSet(mesh))throw new Exception("Lightmap UV generation failed: "+mesh.name);EditorUtility.SetDirty(mesh);}
                    GameObjectUtility.SetStaticEditorFlags(filter.gameObject,GameObjectUtility.GetStaticEditorFlags(filter.gameObject)|StaticEditorFlags.ContributeGI);
                    renderer.receiveGI=ReceiveGI.Lightmaps;count++;
                }
                var probes=CreateLightProbes(layout,map);
                var reflections=CreateReflections(layout,map);
                string settingsPath=info.LightingSettingsPath;
                var settings=AssetDatabase.LoadAssetAtPath<LightingSettings>(settingsPath);
                if(!settings){settings=new LightingSettings();AssetDatabase.CreateAsset(settings,settingsPath);}
                settings.bakedGI=true;settings.realtimeGI=false;settings.lightmapper=LightingSettings.Lightmapper.ProgressiveCPU;
                settings.lightmapResolution=10;settings.lightmapMaxSize=1024;settings.directSampleCount=32;settings.indirectSampleCount=64;settings.environmentSampleCount=64;settings.maxBounces=2;
                Lightmapping.lightingSettings=settings;AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                Debug.Log("HC_LIGHTING_BEGIN CPU lightmapper; static renderers="+count+" probes="+probes.probePositions.Length);
                if(!Lightmapping.Bake()||LightmapSettings.lightmaps.Length==0)throw new Exception(map+" lightmap bake failed");
                // Exclude objects that can change position or light state from
                // the static reflection snapshot, restoring every renderer even
                // if one of the synchronous captures fails.
                var dynamicRenderers=layout.GetComponentsInChildren<Renderer>().Where(r=>Movable(r.transform,map)).ToArray();
                var rendererStates=dynamicRenderers.ToDictionary(r=>r,r=>r.enabled);
                try
                {
                    foreach(var renderer in dynamicRenderers)renderer.enabled=false;
                    foreach(var probe in reflections)
                    {
                        string path=folder+"/Reflection-"+probe.name+".exr";
                        if(!Lightmapping.BakeReflectionProbe(probe,path))throw new Exception("Reflection bake failed: "+probe.name);
                        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                        var texture=AssetDatabase.LoadAssetAtPath<Cubemap>(path);
                        if(!texture)throw new Exception("Reflection output is not a cubemap: "+path);
                        probe.customBakedTexture=texture;
                    }
                }
                finally{foreach(var state in rendererStates)if(state.Key)state.Key.enabled=state.Value;}
                success=true;
            }
            finally
            {
                foreach(var state in enabled)if(state.Key)state.Key.enabled=state.Value;
                foreach(var state in emissionFlags)if(state.Key){state.Key.globalIlluminationFlags=state.Value;EditorUtility.SetDirty(state.Key);}
                if(!success)foreach(var state in bakeTypes)if(state.Key)state.Key.lightmapBakeType=state.Value;
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            }
            AssetDatabase.Refresh();
            var receipt=new BakeReceipt{sceneDependencies=AssetDatabase.GetAssetDependencyHash(info.ScenePath).ToString(),sourceHash=SourceHash(map),utc=DateTime.UtcNow.ToString("O"),map=map.ToString(),hashScope=HashScope(map),lightmaps=LightmapSettings.lightmaps.Length,lightProbes=LightmapSettings.lightProbes?LightmapSettings.lightProbes.count:0,reflections=layout.GetComponentsInChildren<ReflectionProbe>().Length};
            File.WriteAllText(info.BakeReceiptPath,JsonUtility.ToJson(receipt,true));AssetDatabase.ImportAsset(info.BakeReceiptPath);
            ValidatePreparedScene(map);Debug.Log("HC_LIGHTING_BAKED map="+map+" maps="+receipt.lightmaps+" probes="+receipt.lightProbes+" reflections="+receipt.reflections);
        }
        static bool Movable(Transform transform,MapId map)
        {
            if(transform.GetComponentInParent<Door>()||transform.GetComponentInParent<PracticalFixture>())return true;
            foreach(var parent in transform.GetComponentsInParent<Transform>())if(parent.name=="Porcelain doll"||parent.name=="Authored / Mug")return true;
            if(map==MapId.Castle)
            {
                // Castle props that animate (floaters, orbiting knives, bats, portrait
                // eyes) carry their own MonoBehaviour: keep them out of static GI.
                foreach(var behaviour in transform.GetComponentsInParent<MonoBehaviour>(true))
                    if(!(behaviour is WorldLayout)&&!(behaviour is WorldLabel)&&!(behaviour is Interactable))return true;
            }
            return false;
        }
        public static void ValidateSetup()=>ValidateSetup(MapId.Bellwether);
        public static void ValidateSetup(MapId map)
        {
            var layout=UnityEngine.Object.FindFirstObjectByType<WorldLayout>();if(!layout)throw new Exception("Saved authored layout missing: "+map);
            try
            {
                var probes=CreateLightProbes(layout,map);var reflections=CreateReflections(layout,map);
                foreach(var probe in reflections)
                    if(Physics.CheckSphere(probe.transform.position,.1f,~0,QueryTriggerInteraction.Ignore))throw new Exception("Reflection camera intersects geometry: "+probe.name);
                Debug.Log("HC_LIGHTING_SETUP_PASS unobstructed light probes="+probes.probePositions.Length+" reflection cameras="+reflections.Length+"; no bake or rendering performed");
            }
            finally{EditorSceneManager.OpenScene(MapCatalog.Get(map).ScenePath,OpenSceneMode.Single);}
        }
        static LightProbeGroup CreateLightProbes(WorldLayout layout,MapId map)
        {
            if(map==MapId.Castle)return CreateCastleLightProbes(layout);
            var old=layout.transform.Find("House light probes");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var obj=new GameObject("House light probes");obj.transform.SetParent(layout.transform,false);
            var probes=obj.AddComponent<LightProbeGroup>();var positions=new List<Vector3>();Physics.SyncTransforms();
            // Discard points in walls/furniture, and points over the stair void.
            // Two vertical bands support hands and the upright apparition.
            foreach(float floor in new[]{-3.2f,0,3.2f})foreach(float x in new[]{-6.8f,-4.7f,0,4.8f,6.8f})foreach(float z in new[]{2f,6f,10f,12.8f})foreach(float h in new[]{.55f,1.7f})
            {
                var point=new Vector3(x,floor+h,z);
                if(!Physics.CheckSphere(point,.12f,~0,QueryTriggerInteraction.Ignore)&&Physics.Raycast(new Vector3(x,floor+.2f,z),Vector3.down,.4f,~0,QueryTriggerInteraction.Ignore))positions.Add(point);
            }
            foreach(float x in new[]{-.65f,.65f})foreach(float z in new[]{-12.8f,-10f,-6f,-1.5f})foreach(float h in new[]{.55f,1.7f})
            {var point=new Vector3(x,h,z);if(!Physics.CheckSphere(point,.12f,~0,QueryTriggerInteraction.Ignore))positions.Add(point);}
            if(positions.Count<80)throw new Exception("Insufficient unobstructed house light probes: "+positions.Count);
            probes.probePositions=positions.Select(p=>obj.transform.InverseTransformPoint(p)).ToArray();return probes;
        }
        // Castle: a coarse grid over the contract footprint (x +-14, z 0-28) on the
        // four floor levels; points inside geometry or over voids are discarded.
        static LightProbeGroup CreateCastleLightProbes(WorldLayout layout)
        {
            var old=layout.transform.Find("Castle light probes");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var obj=new GameObject("Castle light probes");obj.transform.SetParent(layout.transform,false);
            var probes=obj.AddComponent<LightProbeGroup>();var positions=new List<Vector3>();Physics.SyncTransforms();
            foreach(float floor in CastleFloors)for(float x=-12;x<=12.01f;x+=4)for(float z=-2;z<=26.01f;z+=4)foreach(float h in new[]{.55f,1.7f})
            {
                var point=new Vector3(x,floor+h,z);
                if(!Physics.CheckSphere(point,.12f,~0,QueryTriggerInteraction.Ignore)&&Physics.Raycast(new Vector3(x,floor+.2f,z),Vector3.down,.4f,~0,QueryTriggerInteraction.Ignore))positions.Add(point);
            }
            if(positions.Count<MinimumProbes(MapId.Castle))throw new Exception("Insufficient unobstructed castle light probes: "+positions.Count);
            probes.probePositions=positions.Select(p=>obj.transform.InverseTransformPoint(p)).ToArray();return probes;
        }
        static readonly float[] CastleFloors={-3.2f,0,3.2f,6.4f};
        static int MinimumProbes(MapId map)=>map==MapId.Castle?40:80;
        static int MinimumReflections(MapId map)=>map==MapId.Castle?4:9;
        static ReflectionProbe[] CreateReflections(WorldLayout layout,MapId map)
        {
            if(map==MapId.Castle)return CreateCastleReflections(layout);
            var old=layout.transform.Find("House reflections");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("House reflections");root.transform.SetParent(layout.transform,false);var list=new List<ReflectionProbe>();
            void Add(string name,Vector3 center,Vector3 size)
            {
                var obj=new GameObject(name);obj.transform.SetParent(root.transform,false);obj.transform.position=center;
                var probe=obj.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Custom;probe.resolution=128;probe.hdr=true;
                probe.boxProjection=true;probe.size=size;probe.blendDistance=.4f;probe.nearClipPlane=.1f;probe.farClipPlane=25;
                probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=new Color(.012f,.018f,.026f);list.Add(probe);
            }
            Add("Living",new Vector3(-4.8f,1.5f,4.1f),new Vector3(6.3f,3,7.9f));
            Add("Dining",new Vector3(-4.8f,1.5f,11.1f),new Vector3(6.3f,3,5.5f));
            Add("Kitchen",new Vector3(5.1f,1.5f,10.5f),new Vector3(5.6f,3,6.5f));
            Add("Child",new Vector3(-4.8f,4.7f,3.8f),new Vector3(6.3f,3,6.6f));
            Add("Parents",new Vector3(-4.8f,4.7f,10.5f),new Vector3(6.3f,3,6.6f));
            Add("Bathroom",new Vector3(5.1f,4.7f,11.1f),new Vector3(5.6f,3,5.5f));
            Add("BasementWest",new Vector3(-4.8f,-1.7f,9.5f),new Vector3(6.3f,3,8.6f));
            Add("BasementEast",new Vector3(5.1f,-1.7f,8f),new Vector3(5.6f,3,11.6f));
            Add("Van",new Vector3(0,1.35f,-11),new Vector3(3.5f,2.5f,5.3f));return list.ToArray();
        }
        // One box-projected probe per occupied floor quadrant. Centres that sit in
        // geometry or have no floor beneath them are skipped, so the set follows
        // whatever CastleWorld actually built.
        static ReflectionProbe[] CreateCastleReflections(WorldLayout layout)
        {
            var old=layout.transform.Find("Castle reflections");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("Castle reflections");root.transform.SetParent(layout.transform,false);var list=new List<ReflectionProbe>();Physics.SyncTransforms();
            foreach(float floor in CastleFloors)foreach(float x in new[]{-7f,7f})foreach(float z in new[]{4.67f,14f,23.33f})
            {
                var center=new Vector3(x,floor+1.6f,z);
                if(Physics.CheckSphere(center,.15f,~0,QueryTriggerInteraction.Ignore)||!Physics.Raycast(center,Vector3.down,1.75f,~0,QueryTriggerInteraction.Ignore))continue;
                var obj=new GameObject("Castle "+(floor<-1?"crypt":floor<1?"ground":floor<4?"upper":"tower")+" "+(x<0?"west":"east")+" "+Mathf.RoundToInt(z));obj.transform.SetParent(root.transform,false);obj.transform.position=center;
                var probe=obj.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Custom;probe.resolution=128;probe.hdr=true;
                probe.boxProjection=true;probe.size=new Vector3(14,3.2f,9.33f);probe.blendDistance=.6f;probe.nearClipPlane=.1f;probe.farClipPlane=30;
                probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=new Color(.012f,.010f,.018f);list.Add(probe);
            }
            if(list.Count<MinimumReflections(MapId.Castle))throw new Exception("Insufficient unobstructed castle reflection volumes: "+list.Count);
            return list.ToArray();
        }
        static string HashScope(MapId map)=>map==MapId.Bellwether?"all .cs/.shader under Assets/HollowCurrent except NativeVerification.cs and castle-only paths":"all .cs/.shader under Assets/HollowCurrent except NativeVerification.cs";
        static string SourceHash(MapId map)
        {
            // Runtime verification instrumentation does not contribute to the
            // authored scene or light transport. Keep every other code/shader
            // dependency conservative, but permit profiling the same bake.
            // Bellwether additionally ignores castle-only sources (see CastleOnly).
            var paths=Directory.GetFiles("Assets/HollowCurrent","*",SearchOption.AllDirectories).Select(p=>p.Replace('\\','/')).Where(p=>(p.EndsWith(".cs",StringComparison.Ordinal)||p.EndsWith(".shader",StringComparison.Ordinal))&&!p.EndsWith("/NativeVerification.cs",StringComparison.Ordinal)&&(map!=MapId.Bellwether||!CastleOnly.Any(c=>p.StartsWith(c,StringComparison.Ordinal)))).OrderBy(p=>p,StringComparer.Ordinal);
            using(var sha=SHA256.Create())
            {
                var data=new StringBuilder();foreach(var path in paths)data.Append(path).Append('\n').Append(Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)))).Append('\n');
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(data.ToString())));
            }
        }
        public static void ValidatePreparedScene()=>ValidatePreparedScene(MapId.Bellwether);
        // Validates the currently open scene against the map's own receipt.
        public static void ValidatePreparedScene(MapId map)
        {
            var info=MapCatalog.Get(map);string bakeMethod=map==MapId.Bellwether?"HollowCurrent.Editor.HouseLighting.PrepareAndBake":"HollowCurrent.Editor.HouseLighting.PrepareAndBakeCastle";
            if(!File.Exists(info.BakeReceiptPath))throw new Exception("No verified "+map+" bake receipt ("+info.BakeReceiptPath+"). Run "+bakeMethod+" in a coordinated graphics session first.");
            var receipt=JsonUtility.FromJson<BakeReceipt>(File.ReadAllText(info.BakeReceiptPath));
            if(receipt==null||receipt.sourceHash!=SourceHash(map)||receipt.sceneDependencies!=AssetDatabase.GetAssetDependencyHash(info.ScenePath).ToString())throw new Exception("Authored "+map+" lighting is stale: source or scene dependencies changed since the bake. Re-run "+bakeMethod+".");
            var layout=UnityEngine.Object.FindFirstObjectByType<WorldLayout>();
            if(!layout||LightmapSettings.lightmaps.Length==0||LightmapSettings.lightmaps.Length!=receipt.lightmaps)throw new Exception("Prepared "+map+" lightmaps missing");
            foreach(var lightmap in LightmapSettings.lightmaps)if(!lightmap.lightmapColor)throw new Exception("Prepared lightmap texture missing");
            if(!LightmapSettings.lightProbes||LightmapSettings.lightProbes.count!=receipt.lightProbes||receipt.lightProbes<MinimumProbes(map))throw new Exception("Prepared light probes missing");
            var reflections=layout.GetComponentsInChildren<ReflectionProbe>();
            if(reflections.Length!=receipt.reflections||reflections.Length<MinimumReflections(map))throw new Exception("Prepared room reflections missing");
            foreach(var probe in reflections)if(probe.mode!=ReflectionProbeMode.Custom||!probe.customBakedTexture)throw new Exception("Unbaked reflection probe: "+probe.name);
            foreach(var component in layout.GetComponentsInChildren<MonoBehaviour>(true))
            {var script=MonoScript.FromMonoBehaviour(component);if(!script||string.IsNullOrEmpty(AssetDatabase.GetAssetPath(script)))throw new Exception("Prepared scene has transient script: "+component.GetType().Name);}
            Debug.Log("HC_PREPARED_LIGHTING_PASS map="+map+" maps="+receipt.lightmaps+" probes="+receipt.lightProbes+" reflections="+receipt.reflections);
        }
    }
}
