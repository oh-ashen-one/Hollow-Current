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
        // S4 eye: on the lowered drawbridge (hinge z -0.45, leaf to z -6.45), 2.0 m above
        // the planks, facing the gate (+z). Sprite moons for S4 can be placed on rays from here.
        // Round 5: 14 m back from the gate along the bridge axis, 3.5 m up (height limit lifted
        // for S4 only). Pitch/FOV below are the fallback when the framing search finds nothing.
        public static readonly Vector3 S4Eye=new Vector3(0,3.5f,-14f);
        public const float S4Pitch=18,S4Fov=95;
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
        // The investigator's flashlight as in play (Investigator input is off in capture).
        void Torch(bool on){var l=game.Player.Torch;l.enabled=on;l.intensity=.85f*game.Player.Brightness;}
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
            // S4 (round 5 brief; the 2.2 m eye limit is lifted for this shot only): pulled
            // back along the bridge axis to 14 m from the gate at 3.5 m. Pitch and FOV are
            // searched so both moons sit fully in open sky (centre inset by the disc radius,
            // not occluded), the gate base sits in the lower third and the lowered bridge's
            // far end stays in frame. Each moon is logged in-frame/occluded.
            if(spots!=null&&spots.Drawbridge)spots.Drawbridge.Snap(true);
            Physics.SyncTransforms();
            float fov=game.Player.View.fieldOfView;
            e=S4Eye;float s4Pitch=S4Pitch,s4Fov=S4Fov;string s4="fallback";
            var moonDirs=new[]{("pale",CastleArt.PaleMoonDirection,.15f),("red",CastleArt.RedMoonDirection,.07f)};
            foreach(float f in new[]{90f,95f})
            {
                for(float p=0;p<=30.01f&&s4=="fallback";p+=1.5f)
                {
                    game.Player.View.fieldOfView=f;var aim=e+Quaternion.Euler(-p,0,0)*Vector3.forward*20;Place(e,aim);
                    var cam=game.Player.View;bool ok=true;
                    foreach(var m in moonDirs)
                    {
                        var vp=cam.WorldToViewportPoint(e+m.Item2*500);
                        if(vp.z<0||vp.x<m.Item3||vp.x>1-m.Item3||vp.y<m.Item3||vp.y>1-m.Item3||Physics.Raycast(e,m.Item2,600,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore))ok=false;
                    }
                    var gate=cam.WorldToViewportPoint(new Vector3(0,0,0));if(gate.z<0||gate.y<.08f||gate.y>.34f)ok=false;
                    var bridge=cam.WorldToViewportPoint(new Vector3(0,0,-6.4f));if(bridge.z<0||bridge.y<.01f)ok=false;
                    if(ok){s4Pitch=p;s4Fov=f;s4="pitch="+p+" fov="+f;}
                }
                if(s4!="fallback")break;
            }
            game.Player.View.fieldOfView=s4Fov;t=e+Quaternion.Euler(-s4Pitch,0,0)*Vector3.forward*20;Place(e,t);Log("S4 search "+s4);
            string moons="";
            foreach(var m in moonDirs)
            {
                var vp=game.Player.View.WorldToViewportPoint(e+m.Item2*500);
                bool inFrame=vp.z>0&&vp.x>0&&vp.x<1&&vp.y>0&&vp.y<1;
                bool blocked=Physics.Raycast(e,m.Item2,600,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore);
                moons+=" "+m.Item1+"_moon inFrame="+inFrame+" occluded="+blocked+" viewport=("+vp.x.ToString("F2")+","+vp.y.ToString("F2")+")";
            }
            Log("S4"+moons);
            yield return new WaitForSecondsRealtime(1.5f);yield return Shot("S4_exterior_sky","refs/exterior_sky.png",e,t,"eye "+V(e)+" (14 m back on the bridge axis, 3.5 m), "+s4+";"+moons);
            game.Player.View.fieldOfView=fov;
            if(spots!=null&&spots.Drawbridge)spots.Drawbridge.Snap(false);
            // S5 (round 5): Count Claude crouched on the rose-window ledge (Presence.StageOnRoseLedge,
            // hc/count5) with both moons in the window above his head; RoseLedgeFraming fills 58% of
            // the frame height. Fallback: the round-4 balcony perch + CaptureFraming.
            Torch(true);Floater.SetFlameTarget(game.Creature.transform);
            bool staged=game.Creature.StageOnRoseLedge(true);string s5="rose ledge, RoseLedgeFraming(0.58)";
            yield return new WaitForSecondsRealtime(1.5f);
            if(!staged||!game.Creature.RoseLedgeFraming(.58f,out e,out t))
            {
                game.Creature.Dismiss();staged=game.Creature.StageForCapture(0,true);yield return new WaitForSecondsRealtime(1.5f);
                if(!game.Creature.CaptureFraming(.5f,30,out e,out t)){e=new Vector3(3.6f,1.64f,15.2f);t=game.Creature.transform.position+Vector3.up*1.2f;}
                s5="FALLBACK balcony perch 0, CaptureFraming(0.5,30)";Log("S5 rose-ledge staging failed; fallback used");
            }
            Place(e,t);
            yield return new WaitForSecondsRealtime(.5f);yield return Shot("S5_lord_perch","refs/lord_perch.png",e,t,s5+"; Count feet "+V(game.Creature.transform.position)+", investigator torch on");
            game.Creature.Dismiss();
            // S6: Count Claude standing on the courtyard floor (Mode 2 Search) with a 1.80 m
            // mannequin (Presence.ScaleReference) beside him for scale.
            // S6 in the gatehouse facing the gate (hc/count4): the gatehouse table and the
            // courtyard doorway are behind the camera.
            game.Creature.ManifestAt(new Vector3(-.2f,0,2.2f),0,9999,true);
            var man=Presence.ScaleReference(new Vector3(1.05f,0,2.2f),0);
            e=new Vector3(.4f,1.15f,5.6f);t=new Vector3(.4f,1.1f,2.2f);Place(e,t);
            yield return new WaitForSecondsRealtime(1.5f);yield return Shot("S6_lord_sheet","refs/lord_sheet.png",e,t,"gatehouse facing the gate; Count feet (-0.2,0,2.2) Mode "+game.Creature.Profile.ModeSearch+", 1.80 m ScaleReference at (1.05,0,2.2), investigator torch on");
            game.Creature.Dismiss();Torch(false);CastleRun.ClearFlameTarget();Destroy(man);
            // S8: journal open on a Blood Ledger entry.
            ui.enabled=true;game.Journal=true;var book=game.Book;
            string body=book.NoteBodies[0];foreach(var item in game.World.Root.GetComponentsInChildren<Interactable>(true))if(item.Kind==InteractKind.Note&&item.Label==book.NoteTitles[0])body=item.Body;
            game.ShowNote(book.NoteTitles[0],body);
            yield return new WaitForSecondsRealtime(1.5f);yield return Shot("S8_journal_ledger","(journal)",Vector3.zero,Vector3.zero,book.NoteTitles[0]);
            game.NoteBody=null;game.Journal=false;ui.enabled=false;game.ApplyFocus();yield return null;
            // Hunt: hc/count3's scripted capture choreography from the east balcony perch
            // (H01-H03 Perch, H04-H06 Glide, H07 Dive, H08-H09 Hold at 2.5 m, H10 Feint).
            // The investigator's view tracks him; shot k is taken at CaptureClock = k.
            e=new Vector3(0,1.64f,15.6f);var origin=new Vector3(4.6f,0,26.8f);t=new Vector3(1.5f,1.6f,24f);Place(e,t);Torch(true);
            game.Case.Equip();game.Case.EnterHouse();game.Case.Record(Evidence.Induction);game.Case.Record(Evidence.Handprint);
            game.Case.BeginWarning();game.Creature.SetHuntOrigin(origin);game.Case.Tick(9.05f,true);
            Log("hunt phase="+game.Case.Phase+" "+game.Creature.DiagnosticState);
            // The warning-phase scare mist (ScareDirector.Pulse, 2.8 s lifetime) spawns 3 m ahead.
            yield return new WaitForSecondsRealtime(3f);
            // The warning's ScareDirector.Pulse(…, 9) also starts 9 s of interference, which
            // strobes every candle to 8% (LightMultiplier): wait it out so H frames share S1's light.
            float interference=Time.realtimeSinceStartup;while(game.Scares&&game.Scares.Interference)yield return null;
            Log("hunt interference waited "+(Time.realtimeSinceStartup-interference).ToString("F1")+" s");
            bool choreo=game.Creature.BeginCaptureChoreography(1);Log("choreography begin="+choreo);
            var samples=new List<float>();
            for(int i=0;i<10;i++)
            {
                while(game.Creature.CaptureClock<i+1){yield return null;samples.Add(Time.unscaledDeltaTime*1000);}
                var view=game.Player.View.transform;
                yield return Shot("H"+(i+1).ToString("00")+"_hunt_dive","(hunt sequence)",view.position,view.position+view.forward*5,"clock="+game.Creature.CaptureClock.ToString("F2")+" phase="+game.Case.Phase+" "+game.Creature.CaptureNote);
            }
            game.Creature.EndCaptureChoreography();
            Record("hunt_dive",samples);
            File.WriteAllText(Path.Combine(Directory,"capture.json"),"{\"unity\":\""+Application.unityVersion+"\",\"screen\":\""+Screen.width+"x"+Screen.height+"\",\"quality\":\""+QualitySettings.names[QualitySettings.GetQualityLevel()]+"\",\"antiAliasing\":"+QualitySettings.antiAliasing+",\"shadowResolution\":\""+QualitySettings.shadowResolution+"\",\"shadowDistance\":"+QualitySettings.shadowDistance+",\"targetFrameRate\":"+Application.targetFrameRate+",\"vSync\":"+QualitySettings.vSyncCount+",\"fov\":"+game.Player.View.fieldOfView+",\"entries\":[\n"+string.Join(",\n",entries)+"\n]}\n");
            Log("DONE "+entries.Count+" entries");
            Application.Quit();
        }
    }
}
