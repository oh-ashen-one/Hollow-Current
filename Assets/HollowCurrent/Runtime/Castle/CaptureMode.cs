using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace HollowCurrent
{
    // Opt-in critic capture for the Castle map: `-hc-capture <outdir>` (or env
    // HC_CAPTURE=<outdir>). After a 3 s warm-up it places the camera on a fixed shot
    // list, saves 2560x1440 PNGs, records a 10-frame hunt sequence and frame times to
    // capture.json, then quits. Never active in a normal launch.
    public sealed class CaptureMode : MonoBehaviour
    {
        public const int Width=2560,Height=1440;
        public static string Directory {get;private set;}
        public static bool Requested
        {
            get
            {
                if(Directory!=null)return true;
                var args=Environment.GetCommandLineArgs();
                for(int i=0;i<args.Length-1;i++)if(args[i]=="-hc-capture"){Directory=args[i+1];return true;}
                var env=Environment.GetEnvironmentVariable("HC_CAPTURE");
                if(!string.IsNullOrEmpty(env)){Directory=env;return true;}
                return false;
            }
        }
        Game game;Interface ui;
        readonly StringBuilder json=new StringBuilder();
        readonly List<string> entries=new List<string>();

        public void Initialize(Game owner)
        {
            game=owner;ui=owner.GetComponent<Interface>();
            Application.runInBackground=true;
            Screen.SetResolution(Width,Height,FullScreenMode.FullScreenWindow);
            System.IO.Directory.CreateDirectory(Directory);
            StartCoroutine(Run());
        }
        static void Log(string line)=>Debug.Log("HC_CAPTURE "+line);
        // Camera at eye (world), looking at target; the investigator body follows.
        void Place(Vector3 eye,Vector3 target)
        {
            var p=game.Player;var d=target-eye;
            p.transform.position=eye-Vector3.up*p.View.transform.localPosition.y;
            p.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,0);
            p.View.transform.localRotation=Quaternion.Euler(-Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg,0,0);
            Physics.SyncTransforms();
        }
        IEnumerator Shot(string id,string reference,Vector3 eye,Vector3 target,string note="")
        {
            yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            string file=Path.Combine(Directory,id+".png");
            File.WriteAllBytes(file,texture.EncodeToPNG());
            entries.Add("{\"id\":\""+id+"\",\"ref\":\""+reference+"\",\"file\":\""+file.Replace("\\","/")+"\",\"size\":\""+texture.width+"x"+texture.height+"\",\"eye\":\""+V(eye)+"\",\"target\":\""+V(target)+"\",\"note\":\""+note+"\"}");
            Log(id+" "+texture.width+"x"+texture.height+" "+file);
            Destroy(texture);
        }
        static string V(Vector3 v)=>v.x.ToString("F2")+","+v.y.ToString("F2")+","+v.z.ToString("F2");
        // Unscaled frame times for `seconds`: mean / p95 / max in milliseconds.
        IEnumerator Measure(string id,float seconds)
        {
            var samples=new List<float>();float end=Time.realtimeSinceStartup+seconds;
            while(Time.realtimeSinceStartup<end){yield return null;samples.Add(Time.unscaledDeltaTime*1000);}
            Record(id,samples);
        }
        void Record(string id,List<float> samples)
        {
            if(samples.Count==0)return;
            samples.Sort();float sum=0;foreach(var s in samples)sum+=s;
            float mean=sum/samples.Count,p95=samples[Mathf.Min(samples.Count-1,(int)(samples.Count*.95f))],max=samples[samples.Count-1];
            entries.Add("{\"frametime\":\""+id+"\",\"frames\":"+samples.Count+",\"mean_ms\":"+mean.ToString("F2")+",\"p95_ms\":"+p95.ToString("F2")+",\"max_ms\":"+max.ToString("F2")+"}");
            Log("FRAMETIME "+id+" frames="+samples.Count+" mean="+mean.ToString("F2")+"ms p95="+p95.ToString("F2")+"ms max="+max.ToString("F2")+"ms");
        }
        IEnumerator Run()
        {
            while(game.Loading)yield return null;
            var warm=Time.realtimeSinceStartup+3;while(Time.realtimeSinceStartup<warm)yield return null;
            Log("screen "+Screen.width+"x"+Screen.height+" quality="+QualitySettings.names[QualitySettings.GetQualityLevel()]+" aa="+QualitySettings.antiAliasing+" shadows="+QualitySettings.shadowResolution+"/"+QualitySettings.shadowDistance+" targetFps="+Application.targetFrameRate+" vsync="+QualitySettings.vSyncCount);
            // S7: title screen with the map picker (Castle selected).
            game.SelectMap(MapId.Castle);
            yield return new WaitForSecondsRealtime(1);
            yield return Shot("S7_title","(title screen)",Vector3.zero,Vector3.zero,"title with map picker");
            // World shots: no HUD, investigator input off, kit not collected (torch off).
            game.StartCase();ui.enabled=false;yield return null;yield return null;
            game.Player.enabled=false;
            var spots=game.World.Castle;
            // S1: great hall from the entrance doors at eye level.
            var e=new Vector3(0,1.64f,14.7f);var t=new Vector3(0,2.6f,27);Place(e,t);
            yield return new WaitForSecondsRealtime(1.5f);yield return Measure("S1_great_hall",3);
            yield return Shot("S1_great_hall","refs/great_hall.png",e,t);
            // S2: chapel altar from the nave.
            e=new Vector3(7.0f,1.64f,6.5f);t=spots!=null?spots.AltarPosition+Vector3.up*.3f:new Vector3(13,1.6f,6.5f);Place(e,t);
            yield return new WaitForSecondsRealtime(1.5f);yield return Shot("S2_chapel_altar","refs/chapel_altar.png",e,t);
            // S3: crypt from the stair foot looking at the sarcophagus.
            e=new Vector3(-5.2f,-3.2f+1.64f,25.0f);t=new Vector3(0,-2.6f,22.5f);Place(e,t);
            yield return new WaitForSecondsRealtime(1.5f);yield return Shot("S3_crypt","refs/crypt.png",e,t);
            // S4: from the far bank, back at the castle across the lowered drawbridge.
            if(spots!=null&&spots.Drawbridge)spots.Drawbridge.Snap(true);
            e=new Vector3(0,1.64f,-9.5f);t=new Vector3(0,5,8);Place(e,t);
            yield return new WaitForSecondsRealtime(1.5f);yield return Shot("S4_exterior_sky","refs/exterior_sky.png",e,t,"drawbridge lowered (Snap)");
            if(spots!=null&&spots.Drawbridge)spots.Drawbridge.Snap(false);
            // S5: great hall floor looking up at the balcony; Count Claude on the balcony perch, Mode 0.
            var perch=spots!=null&&spots.PerchPoints!=null&&spots.PerchPoints.Length>0?spots.PerchPoints[0]:new Vector3(-4.4f,3.2f,21);
            e=new Vector3(1.5f,1.64f,17.5f);t=perch+Vector3.up*1.2f;Place(e,t);
            var toCamera=e-perch;
            game.Creature.ManifestAt(perch,Mathf.Atan2(toCamera.x,toCamera.z)*Mathf.Rad2Deg,9999,true);game.Creature.Actor.Pose(game.Creature.Profile.ModeIdle);
            yield return new WaitForSecondsRealtime(1.5f);yield return Shot("S5_lord_perch","refs/lord_perch.png",e,t,"Count at perch "+V(perch)+" mode "+game.Creature.Profile.ModeIdle);
            // S6: Count Claude 3 m in front of the camera on the courtyard, Mode 1 Glide.
            e=new Vector3(-3,1.64f,8);var stand=new Vector3(-3,0,11);t=stand+Vector3.up*1.3f;Place(e,t);
            game.Creature.ManifestAt(stand,180,9999,true);game.Creature.Actor.Pose(game.Creature.Profile.ModeMove,3);
            yield return new WaitForSecondsRealtime(1.5f);yield return Shot("S6_lord_sheet","refs/lord_sheet.png",e,t,"Count 3 m ahead, mode "+game.Creature.Profile.ModeMove);
            game.Creature.Dismiss();
            // S8: journal open on a Blood Ledger entry.
            ui.enabled=true;game.Journal=true;var book=game.Book;
            string body=book.NoteBodies[0];foreach(var item in game.World.Root.GetComponentsInChildren<Interactable>(true))if(item.Kind==InteractKind.Note&&item.Label==book.NoteTitles[0])body=item.Body;
            game.ShowNote(book.NoteTitles[0],body);
            yield return new WaitForSecondsRealtime(1.5f);yield return Shot("S8_journal_ledger","(journal)",Vector3.zero,Vector3.zero,book.NoteTitles[0]);
            game.NoteBody=null;game.Journal=false;ui.enabled=false;game.ApplyFocus();yield return null;
            // Hunt: Count Claude dives at the investigator in the great hall; 10 frames, 1 s apart.
            e=new Vector3(0,1.64f,15.6f);var origin=new Vector3(1.5f,0,24f);t=origin+Vector3.up*1.6f;Place(e,t);
            game.Case.Equip();game.Case.EnterHouse();game.Case.Record(Evidence.Induction);game.Case.Record(Evidence.Handprint);
            game.Case.BeginWarning();game.Creature.SetHuntOrigin(origin);game.Case.Tick(9.05f,true);
            Log("hunt phase="+game.Case.Phase+" "+game.Creature.DiagnosticState);
            var samples=new List<float>();
            for(int i=0;i<10;i++)
            {
                float next=Time.realtimeSinceStartup+1;
                while(Time.realtimeSinceStartup<next){yield return null;samples.Add(Time.unscaledDeltaTime*1000);}
                var hunter=game.Creature.transform.position;
                yield return Shot("H"+(i+1).ToString("00")+"_hunt_dive","(hunt sequence)",e,t,"t="+(i+1)+"s phase="+game.Case.Phase+" hunter="+V(hunter)+" distance="+Vector3.Distance(hunter,game.Player.transform.position).ToString("F2"));
            }
            Record("hunt_dive",samples);
            File.WriteAllText(Path.Combine(Directory,"capture.json"),"{\"unity\":\""+Application.unityVersion+"\",\"screen\":\""+Screen.width+"x"+Screen.height+"\",\"quality\":\""+QualitySettings.names[QualitySettings.GetQualityLevel()]+"\",\"antiAliasing\":"+QualitySettings.antiAliasing+",\"shadowResolution\":\""+QualitySettings.shadowResolution+"\",\"shadowDistance\":"+QualitySettings.shadowDistance+",\"targetFrameRate\":"+Application.targetFrameRate+",\"vSync\":"+QualitySettings.vSyncCount+",\"fov\":"+game.Player.View.fieldOfView+",\"entries\":[\n"+string.Join(",\n",entries)+"\n]}\n");
            Log("DONE "+entries.Count+" entries");
            Application.Quit();
        }
    }
}
