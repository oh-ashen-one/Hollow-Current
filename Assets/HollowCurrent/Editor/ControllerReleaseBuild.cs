using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace HollowCurrent.Editor
{
    public static class ControllerReleaseBuild
    {
        [Serializable] sealed class Entry {public string path,sha256;}
        [Serializable] sealed class Proof {public string baseline,original_receipt_sha256;public Entry[] files;}
        [Serializable] sealed class Receipt {public int lightmaps,lightProbes,reflections;}
        static string Hash(string path)
        {using(var s=SHA256.Create())using(var f=File.OpenRead(path))return BitConverter.ToString(s.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
        public static void Run()
        {
            if(!Application.isBatchMode||SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)throw new Exception("Controller build requires guarded Null device");
            var proof=JsonUtility.FromJson<Proof>(File.ReadAllText("evidence/controller-build-proof.json"));
            if(proof.baseline!="680c29caff7db7eda4f8993df4483c65d565ca92"||proof.files==null||proof.files.Length<100)throw new Exception("Missing verified controller-only change proof");
            foreach(var item in proof.files)if(!File.Exists(item.path)||Hash(item.path)!=item.sha256)throw new Exception("Source changed after the controller-only proof: "+item.path);
            string receiptPath=MapCatalog.Get(MapId.Bellwether).BakeReceiptPath;
            if(Hash(receiptPath)!=proof.original_receipt_sha256)throw new Exception("Original bake receipt changed; refusing runtime-only build");
            var receipt=JsonUtility.FromJson<Receipt>(File.ReadAllText(receiptPath));
            string scene=MapCatalog.Get(MapId.Bellwether).ScenePath;
            EditorSceneManager.OpenScene(scene,OpenSceneMode.Single);
            if(LightmapSettings.lightmaps.Length!=receipt.lightmaps||receipt.lightmaps<1||!LightmapSettings.lightProbes||LightmapSettings.lightProbes.count!=receipt.lightProbes)throw new Exception("Saved baked lighting missing after import");
            foreach(var map in LightmapSettings.lightmaps)if(!map.lightmapColor)throw new Exception("A saved lightmap did not import");
            var layout=UnityEngine.Object.FindFirstObjectByType<WorldLayout>();if(!layout)throw new Exception("Authored house missing");
            var probes=layout.GetComponentsInChildren<ReflectionProbe>();if(probes.Length!=receipt.reflections)throw new Exception("Saved room reflections missing");
            foreach(var probe in probes)if(probe.mode!=ReflectionProbeMode.Custom||!probe.customBakedTexture)throw new Exception("A saved reflection did not import");
            ControllerAudit.Run();
            var world=new World();world.LoadOrBuild();
            try
            {
                Physics.SyncTransforms();world.BuildNavigation();
                typeof(NativeBuild).GetMethod("ValidateWorld",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{world});
            }
            finally{world.Dispose();}
            EditorSceneManager.OpenScene(scene,OpenSceneMode.Single);
            Debug.Log("HC_CONTROLLER_CONTENT_FROZEN_PASS retained original receipt, authored assets and lighting; files="+proof.files.Length);
            Debug.Log("HC_BUILD_DEVICE Null");
            typeof(NativeBuild).GetMethod("BuildOpenScene",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        }
    }
}
