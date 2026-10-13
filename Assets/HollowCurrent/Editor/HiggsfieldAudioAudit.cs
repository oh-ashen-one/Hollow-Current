using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace HollowCurrent.Editor
{
    public static class HiggsfieldAudioAudit
    {
        public static void Run()
        {
            if(!Application.isBatchMode||SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
                throw new Exception("Audio import audit requires the coordinated Null-device editor");
            AssetDatabase.Refresh();
            string[] ids={"music_safety","music_house","music_hunt","breath_hidden","breath_presence","apparition","capture","ui_paper","evidence_filed","containment_release"};
            foreach(string id in ids)
            {
                string path="Assets/HollowCurrent/Resources/HiggsfieldAudio/"+id+".wav";
                if(!File.Exists(path))throw new Exception("Generated clip missing: "+id);
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);
                var settings=importer.defaultSampleSettings;
                settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;
                settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;settings.preloadAudioData=true;
                importer.defaultSampleSettings=settings;importer.loadInBackground=false;importer.SaveAndReimport();
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);clip.LoadAudioData();
                if(clip.length<.1f||clip.frequency!=48000)throw new Exception("Clip format invalid: "+id);
                var samples=new float[clip.samples*clip.channels];
                if(!clip.GetData(samples,0))throw new Exception("Imported PCM unavailable: "+id);
                float peak=0;double power=0;
                foreach(float value in samples){if(float.IsNaN(value)||float.IsInfinity(value))throw new Exception("Invalid audio sample");peak=Mathf.Max(peak,Mathf.Abs(value));power+=value*value;}
                if(peak>.26f||power/samples.Length<1e-9)throw new Exception("Clip silent or above normalized peak budget: "+id);
                Debug.Log("HC_GENERATED_AUDIO_IMPORT "+id+" seconds="+clip.length+" channels="+clip.channels+" peak="+peak);
            }
            AssetDatabase.SaveAssets();Debug.Log("HC_AUDIO_IMPORT_PASS 10 generated clips preloaded as PCM");
            Debug.Log("HC_HEADLESS_DEVICE Null");
        }
    }
}
