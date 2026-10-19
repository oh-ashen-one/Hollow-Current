using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        // -hc-capture-size WxH overrides the 2560x1440 default (e.g. 1920x1080 for the title at the shipped window size).
        static int width=Width,height=Height,windowWidth=Width,windowHeight=Height;
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
            var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-hc-capture-size"){var p=args[i+1].Split('x');if(p.Length==2&&int.TryParse(p[0],out var w)&&int.TryParse(p[1],out var h)){width=w;height=h;}}
            ApplyWindow();
            System.IO.Directory.CreateDirectory(Directory);
            StartCoroutine(Run());
        }
        static void Log(string line)=>Debug.Log("HC_CAPTURE "+line);
        // Always windowed (owner rule: never full screen). A size this display cannot hold in a window (title bar and menu bar
        // included) is scaled down to the largest 16:9 window that fits, and the log says so.
        static void ApplyWindow()
        {
            var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-hc-capture-size"){var p=args[i+1].Split('x');if(p.Length==2&&int.TryParse(p[0],out var w)&&int.TryParse(p[1],out var h)){width=w;height=h;}}
            int dw=Display.main.systemWidth,dh=Display.main.systemHeight-80;int rw=width,rh=height;
            if(rw>dw||rh>dh){float k=Mathf.Min((float)dw/rw,(float)dh/rh);rw=Mathf.FloorToInt(rw*k/16)*16;rh=Mathf.RoundToInt(rw*(float)height/width);}
            Screen.SetResolution(rw,rh,FullScreenMode.Windowed);windowWidth=rw;windowHeight=rh;
            Log("requested "+width+"x"+height+" window "+rw+"x"+rh+(rw!=width?" (largest window that fits)":"")+" display="+Display.main.systemWidth+"x"+Display.main.systemHeight);
        }
        // -hc-capture <dir> -hc-capture-title: from the combined player's title (Bellwether scene, case not started), capture the
        // title with the Castle and then the Bellwether card selected, then quit. Never starts a case.
        public static bool TitleOnly=>Array.IndexOf(Environment.GetCommandLineArgs(),"-hc-capture-title")>=0;
        public void InitializeTitle(Game owner)
        {
            game=owner;ui=owner.GetComponent<Interface>();Application.runInBackground=true;ApplyWindow();
            System.IO.Directory.CreateDirectory(Directory);StartCoroutine(RunTitle());
        }
        IEnumerator RunTitle()
        {
            while(game.Loading)yield return null;
            yield return new WaitForSecondsRealtime(2);
            string size=Screen.width+"x"+Screen.height;
            game.SelectMap(MapId.Castle);yield return new WaitForSecondsRealtime(2.5f);
            yield return Shot("S7_title_castle_"+size,"rounds/r10/title_mock.png",Vector3.zero,Vector3.zero,"title, Haunted Castle selected");
            game.SelectMap(MapId.Bellwether);yield return new WaitForSecondsRealtime(2.5f);
            yield return Shot("S7_title_bellwether_"+size,"rounds/r10/title_mock_bellwether.png",Vector3.zero,Vector3.zero,"title, Bellwether selected");
            Log("TITLE DONE");
            Game.RestoreOwnerWindow();yield return null;yield return null;
            Application.Quit();
        }
        // Camera at eye (world), looking at target; the investigator body follows.
        void Place(Vector3 eye,Vector3 target)
        {
            var p=game.Player;var d=target-eye;
            p.transform.position=eye-Vector3.up*p.View.transform.localPosition.y;
            p.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,0);
            p.View.transform.localRotation=Quaternion.Euler(-Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg,0,0);
            Physics.SyncTransforms();
        }
        // Optional subset: -hc-capture-shots S1,S5,H05 (prefix match); other shots are skipped.
        static string[] filter;
        static bool Wanted(string id)
        {
            if(filter==null)
            {
                filter=new string[0];var args=Environment.GetCommandLineArgs();
                for(int i=0;i<args.Length-1;i++)if(args[i]=="-hc-capture-shots")filter=args[i+1].Split(',');
            }
            if(filter.Length==0)return true;
            foreach(var f in filter)if(id.StartsWith(f,StringComparison.Ordinal))return true;
            return false;
        }
        IEnumerator Shot(string id,string reference,Vector3 eye,Vector3 target,string note="")
        {
            if(!Wanted(id)){Log(id+" skipped (shot filter)");yield break;}
            // The window can be resized from outside mid-run (a display change resized it to 2508x1490 in r12): put the capture size
            // back before every shot so framing stays identical across shots.
            if(!TitleOnly&&(Screen.width!=windowWidth||Screen.height!=windowHeight)){Log("window "+Screen.width+"x"+Screen.height+" resized back to "+windowWidth+"x"+windowHeight);Screen.SetResolution(windowWidth,windowHeight,FullScreenMode.Windowed);yield return null;yield return null;yield return new WaitForSecondsRealtime(.5f);}
            yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            string file=Path.Combine(Directory,id+".png");
            File.WriteAllBytes(file,texture.EncodeToPNG());
            entries.Add("{\"id\":\""+id+"\",\"ref\":\""+reference+"\",\"file\":\""+file.Replace("\\","/")+"\",\"size\":\""+texture.width+"x"+texture.height+"\",\"eye\":\""+V(eye)+"\",\"target\":\""+V(target)+"\",\"note\":\""+note+"\"}");
            Log(id+" "+texture.width+"x"+texture.height+" "+file);
            Destroy(texture);
        }
        // -hc-capture-art exterior|props|doors|layout|kit|instruments: extra art-review views (castle-local = world coordinates), eye -> target at the player's
        // 72 deg FOV, plus a frame-time sample at the crypt view for props. Used with the normal full capture for S1-S4.
        static string ArtSet{get{var a=Environment.GetCommandLineArgs();for(int i=0;i<a.Length-1;i++)if(a[i]=="-hc-capture-art")return a[i+1];return null;}}
        IEnumerator ArtRun(string set)
        {
            game.StartCase();ui.enabled=false;yield return null;yield return null;game.Player.enabled=false;game.Player.View.fieldOfView=72;
            Torch(LanternOn);Log("lantern "+(LanternOn?"on":"off")+" torchScale="+game.Player.TorchScale.ToString("F2")+" intensity="+game.Player.Torch.intensity.ToString("F2")+" range="+game.Player.Torch.range.ToString("F1")+" angle="+game.Player.Torch.spotAngle.ToString("F0")+" mode="+game.Player.Torch.renderMode);
            var views=new List<(string id,Vector3 eye,Vector3 target)>();
            if(set=="exterior")
            {
                views.Add(("bridge_gate",new Vector3(0,1.7f,-4.2f),new Vector3(0,5.2f,3)));
                views.Add(("bridge_keep",new Vector3(0,1.7f,-6.1f),new Vector3(0,16,19.5f)));
                views.Add(("bridge_left",new Vector3(-1.2f,1.7f,-3.5f),new Vector3(-14,4.5f,3)));
                views.Add(("gate_out_S6",new Vector3(.3f,1.64f,1.4f),new Vector3(.2f,2.2f,-20)));   // wave L: eye inside the arch, leaf lowered, so the frame is the bridge, moat and far bank
                views.Add(("courtyard_keep",new Vector3(0,1.64f,7),new Vector3(0,20,19.5f)));
                views.Add(("courtyard_wall",new Vector3(3,1.64f,11.5f),new Vector3(-.8f,9,3)));
                views.Add(("courtyard_east",new Vector3(-4,1.64f,9),new Vector3(6,8,5)));
                views.Add(("battlement_out",new Vector3(3.6f,9,-.4f),new Vector3(0,3,-30)));   // wave L: looks down the approach: bridge, moat, far bank and the ridge on the horizon
                views.Add(("battlement_in",new Vector3(3.6f,9,.4f),new Vector3(0,9,16)));
            }
            else if(set=="doors")
            {
                // fb-doors (owner items 3-5): the Hall great doors closed and open, 3 m back on the courtyard side (the owner's approach, door
                // leaves must be visible and fill the opening) and 3 m back inside the hall. The open views open both leaves first.
                views.Add(("hall_doors_closed",new Vector3(0,1.64f,11),new Vector3(0,2.2f,14)));
                views.Add(("hall_doors_closed_inside",new Vector3(1.6f,1.64f,17.2f),new Vector3(0,2.2f,14)));
                views.Add(("hall_doors_open",new Vector3(0,1.64f,11),new Vector3(0,2.2f,14)));
                views.Add(("hall_doors_open_inside",new Vector3(1.6f,1.64f,17.2f),new Vector3(0,2.2f,14)));
            }
            else if(set=="kit")
            {
                // fc-fbx (old-kit axis fix and door leaf thickness): the dining-hall fireplace between its two lancet windows from inside the room
                // (fireplace, chairs and sideboard were turned; the window tracery is the hall kit), the portrait hall's south wall frames and the
                // portrait door's canvas, and the closed Hall great doors seen almost edge-on from the hall side (slab inside the frame, no proud leaf);
                // fc-art2 adds the telescope and the kitchen hearth.
                views.Add(("kit_window_inside",new Vector3(-7.2f,1.64f,15.0f),new Vector3(-13.55f,1.4f,16.0f)));
                views.Add(("kit_portrait",new Vector3(8.0f,4.84f,17.5f),new Vector3(9.4f,4.6f,13.8f)));
                views.Add(("kit_door_edge",new Vector3(2.7f,1.7f,14.85f),new Vector3(-.6f,1.9f,14.32f)));
                // fc-art2: the observatory telescope from the south along the strip between the orrery and the stair opening (tube heading to the north window at x 12),
                // and the kitchen hearth with the west lancet beside it from the north-east corner of the room (window and hearth side by side, no overlap)
                views.Add(("kit_telescope",new Vector3(11.0f,8.04f,20.3f),new Vector3(11.7f,8.7f,26.8f)));
                views.Add(("kit_hearth",new Vector3(-10.5f,1.64f,9.2f),new Vector3(-13.3f,1.4f,5.0f)));
            }
            else if(set=="layout")
            {
                // wave FB (hc/fb-layout): the great-hall skeleton on the axis and the thinned-out chapel. Run once with -hc-capture-lantern off and once
                // with on, then tools/exposure_report.py <dir> --lantern off|on. The skeleton spans x -0.88..0.93, z 15.35..21.41 (CastleLayout.SkeletonPlanBounds);
                // the chapel aisle is z 5.3-7.7 and the font stands at (12.4, 1.05).
                views.Add(("hall_from_doors",new Vector3(0,1.64f,14.6f),new Vector3(0,1.2f,22.5f)));                       // the owner's view: what you walk into from the great doors
                views.Add(("hall_skeleton_side_west",new Vector3(-3.0f,1.64f,18.4f),new Vector3(0,.9f,18.4f)));               // between the piers, broadside on the ribcage
                views.Add(("hall_skeleton_side_east",new Vector3(3.0f,1.64f,18.4f),new Vector3(0,.9f,18.4f)));
                views.Add(("hall_skeleton_head",new Vector3(-1.4f,1.5f,15.0f),new Vector3(.05f,.4f,16.0f)));                 // the skull, 1 m inside the doors
                views.Add(("hall_skeleton_tail",new Vector3(1.6f,1.5f,23.0f),new Vector3(.3f,.5f,20.5f)));                    // from the table end, the tail clear of the table front
                views.Add(("hall_pier_gap",new Vector3(-4.6f,1.64f,20.1f),new Vector3(-0.5f,.9f,18.4f)));                    // from the west bay through the pier gap: nothing pinned between columns
                views.Add(("chapel_from_great_doors",new Vector3(6.6f,1.64f,9.0f),new Vector3(11.5f,1.1f,6.5f)));            // entering from the courtyard doors
                views.Add(("chapel_from_guard_door",new Vector3(6.6f,1.64f,3.0f),new Vector3(11.5f,1.1f,6.5f)));             // entering from the gatehouse door
                views.Add(("chapel_aisle",new Vector3(7.0f,1.64f,6.5f),new Vector3(13.0f,1.1f,6.5f)));                       // S2 eye along the aisle: the font no longer stands in front of it
                views.Add(("chapel_font_corner",new Vector3(9.5f,1.64f,4.4f),new Vector3(12.4f,.8f,1.0f)));                  // the font in the south-east corner
                views.Add(("chapel_confessional",new Vector3(8.2f,1.64f,9.4f),new Vector3(7.3f,1.2f,12.5f)));               // the hide spot's approach
                views.Add(("chapel_altar_approach",new Vector3(9.0f,1.64f,6.5f),new Vector3(12.9f,1.3f,6.5f)));             // the sockets seen from the pews' end
            }
            else if(set=="instruments")
            {
                // fc-props: the period instruments as held, from the player camera. The eye stands in the chapel nave (candle-lit, altar behind) looking down the
                // aisle; CaptureMode poses the held instrument by hand (Investigator.CaptureHeld, the player's Update is off). Run once with -hc-capture-lantern on
                // (the flashlight at the left hand lights the nave) and once off. Log lines: HC_INSTRUMENTS (at build) and HC_CAPTURE_HELD (per view).
                var eye=new Vector3(8.0f,1.64f,6.5f);var look=new Vector3(12.9f,1.3f,6.5f);
                views.Add(("held_compass_idle",eye,look));
                views.Add(("held_compass_reading5",eye,look));
                views.Add(("held_lantern",eye,look));
                views.Add(("held_trumpet",eye,look));
                views.Add(("held_radio",eye,look));   // fc-art2: the resonance box raised as the R key does (HC_CAPTURE_HELD tool=4 asset=FieldRadio)
            }
            else
            {
                // round 13: the old eye (2, 1.64, 12) stood in the courtyard and framed the dead tree. The skull lies at about (-4.55, 0.35, 16.8)
                // between the piers at z 15.6 and 18.6; these eyes look through the gap (z 16.05 to 18.15) from inside the hall.
                // Wave FB: the skeleton lies on the hall axis now (spine x 0, z 15.35-21.41, skull at about (0.03, 0.4, 15.6)); the ids stay so
                // earlier rounds' captures compare by name, the eyes follow the skull.
                views.Add(("hall_skull_from_east",new Vector3(2.6f,1.5f,18.8f),new Vector3(.03f,.4f,16.0f)));
                views.Add(("hall_skull_close",new Vector3(1.6f,1.3f,17.2f),new Vector3(.03f,.4f,16.0f)));
                views.Add(("hall_west_end_skull",new Vector3(-2.4f,1.64f,21.0f),new Vector3(.03f,.9f,16.5f)));
                views.Add(("hall_table_books",new Vector3(1.1f,1.35f,21.0f),new Vector3(0,.78f,23.7f)));
                views.Add(("crypt_ceiling",new Vector3(0,-1.56f,20),new Vector3(0,-.2f,23)));
            }
            foreach(var v in views)
            {
                // gate_out_S6 looks out through the gate at the lowered leaf (r12 critic ask); the bridge goes back up afterwards.
                var bridge=game.World.Castle!=null?game.World.Castle.Drawbridge:null;bool lowered=v.id=="gate_out_S6"&&bridge;
                if(lowered)bridge.Snap(true);
                // r13: the closed "Gate" door leaves filled this view; open them for the shot and close them after.
                var gateLeaves=new List<Door>();
                if(v.id=="gate_out_S6")foreach(var it in game.World.Root.GetComponentsInChildren<Interactable>(true))if(it.Kind==InteractKind.Door&&it.Label=="Gate"&&it.Door){it.Door.Open=true;gateLeaves.Add(it.Door);}
                if(v.id=="gate_out_S6")
                {
                    Log("gate_out_S6 gate leaves opened="+gateLeaves.Count+" bridge="+(bridge?("lowered="+bridge.Lowered+" angle="+bridge.Angle.ToString("F0")):"none"));
                    // r13 diagnosis: what fills this view (renderers whose bounds the view ray meets within 6 m).
                    var ray=new Ray(v.eye,(v.target-v.eye).normalized);var hits=new List<string>();
                    foreach(var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(r.enabled&&r.gameObject.activeInHierarchy&&r.bounds.IntersectRay(ray,out float d)&&d<6)hits.Add(r.name+"@"+d.ToString("F1"));
                    Log("gate_out_S6 ray renderers: "+string.Join(", ",hits));
                    // r13 diagnosis (-hc-capture-gate-diag): the same view with each candidate hidden in turn.
                    if(Environment.GetCommandLineArgs().Contains("-hc-capture-gate-diag"))
                        foreach(var name in new[]{"GateArch","ExtGate","Drawbridge","ExtRock","ExtWalls"})
                        {
                            var off=new List<Renderer>();
                            foreach(var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(r.enabled&&r.name.EndsWith(name)){r.enabled=false;off.Add(r);}
                            Place(v.eye,v.target);yield return new WaitForSecondsRealtime(.5f);
                            yield return Shot("art_"+set+"_gate_diag_without_"+name,"(diag)",v.eye,v.target,"hidden "+off.Count);
                            foreach(var r in off)r.enabled=true;
                        }
                }
                // fb-doors: the *_open views swing the Hall great doors open from where the camera stands, so the swing side is chosen the way the player's would be.
                var hallLeaves=new List<Door>();
                if(set=="doors")
                {
                    Place(v.eye,v.target);yield return null;yield return null;
                    foreach(var d in game.World.Doors)if(d&&d.name.Contains("Hall great doors")){if(v.id.Contains("_open"))d.Open=true;hallLeaves.Add(d);}
                }
                if(set=="instruments")game.Player.CaptureHeld(v.id.StartsWith("held_compass")?1:v.id=="held_lantern"?2:v.id=="held_radio"?4:3,v.id.EndsWith("reading5")?5:0);
                Place(v.eye,v.target);yield return new WaitForSecondsRealtime(gateLeaves.Count>0||hallLeaves.Count>0?3f:1.5f);
                foreach(var d in hallLeaves)Log("doors "+v.id+" "+d.name+" open="+d.Open+" openAngle="+d.OpenAngle.ToString("F0")+" leafRot="+d.transform.localEulerAngles.y.ToString("F0")+" ignoringPlayer="+d.IgnoringPlayer);
                if(set=="kit")KitLog(v.id);
                yield return Shot("art_"+set+"_"+v.id,"(r12 art review)",v.eye,v.target,"fov 72");
                if(lowered)bridge.Snap(false);
                foreach(var d in gateLeaves)d.Open=false;
                foreach(var d in hallLeaves)d.Open=false;
                if(v.id=="crypt_ceiling"){yield return CryptFrames("after_shot");yield return new WaitForSecondsRealtime(1);yield return CryptFrames("settled");}
            }
            File.WriteAllText(Path.Combine(Directory,"capture_art_"+set+".json"),"{\"entries\":[\n"+string.Join(",\n",entries)+"\n]}\n");
            Log("ART DONE "+set+" "+entries.Count+" entries");
            Game.RestoreOwnerWindow();yield return null;yield return null;
            Application.Quit();
        }
        // fc-fbx: per kit view, the Hall great door and portrait door leaves (art scale along the thickness axis, collision panel thickness,
        // leaf renderer thickness) and the portrait door canvas facing, so the frames can be read against the prepare log's HC_DOOR_LEAF_THICKNESS.
        void KitLog(string view)
        {
            foreach(var d in game.World.Doors)
            {
                if(!d||!(d.name.Contains("Hall great doors")||d.name.Contains("Portrait door")))continue;
                Transform leaf=null,panel=null;
                foreach(Transform c in d.transform){if(c.name.StartsWith("Castle / ",StringComparison.Ordinal))leaf=c;else if(c.GetComponent<Interactable>())panel=c;}
                var b=new Bounds();bool any=false;
                if(leaf)foreach(var r in leaf.GetComponentsInChildren<Renderer>()){if(!any){b=r.bounds;any=true;}else b.Encapsulate(r.bounds);}
                var canvas=leaf?leaf.Find("Canvas"):null;
                Log("kit "+view+" "+d.name+" leafScaleZ="+(leaf?leaf.localScale.z.ToString("F3"):"none")+" panelZ="+(panel?panel.localScale.z.ToString("F3"):"none")+
                    " leafBounds="+(any?b.size.ToString("F3"):"none")+(canvas?" canvasFacing="+canvas.forward.ToString("F2"):""));
            }
        }
        // Crypt frame-time sample with hitch markers (r12 crypt 118.89 ms frame): per frame it records GC collections, managed allocation,
        // texture memory and streaming loads, and the profiler markers that a release player exposes (Valid=False when it does not).
        // Every frame over 33 ms is logged with what changed in it; audio clips that finished loading during the window are counted.
        IEnumerator CryptFrames(string window)
        {
            string[] markers={"GC.Collect","Shader.CreateGPUProgram","Texture.AwakeUploadFromMainThread","AsyncUploadManager.AsyncResourceUpload","Loading.ReadObject","NavMesh.BuildNavMeshData","AudioClip.LoadAudioData","Camera.Render","Gfx.WaitForPresentOnGfxThread"};
            var recs=new List<Unity.Profiling.ProfilerRecorder>();
            foreach(var m in markers)recs.Add(Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal,m));
            var alloc=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame");
            var texMem=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"Texture Memory");
            var valid=new StringBuilder();for(int i=0;i<markers.Length;i++)valid.Append(markers[i]+"="+recs[i].Valid+" ");
            Log("CRYPT_MARKERS "+window+" "+valid+"GCAllocInFrame="+alloc.Valid+" TextureMemory="+texMem.Valid);
            int audioLoaded0=0;foreach(var c in Resources.FindObjectsOfTypeAll<AudioClip>())if(c.loadState==AudioDataLoadState.Loaded)audioLoaded0++;
            int nav0=CastleWorld.NavBuilds;
            int g0=GC.CollectionCount(0),g1=GC.CollectionCount(1),g2=GC.CollectionCount(2);
            ulong tex=Texture.currentTextureMemory;
            var samples=new List<float>();float start=Time.realtimeSinceStartup,end=start+6;int hitches=0;
            while(Time.realtimeSinceStartup<end)
            {
                yield return null;
                float ms=Time.unscaledDeltaTime*1000;samples.Add(ms);
                int c0=GC.CollectionCount(0),c1=GC.CollectionCount(1),c2=GC.CollectionCount(2);ulong t=Texture.currentTextureMemory;
                if(ms>33)
                {
                    hitches++;var sb=new StringBuilder();
                    for(int i=0;i<markers.Length;i++)if(recs[i].Valid&&recs[i].LastValue>0)sb.Append(markers[i]+"="+(recs[i].LastValue/1e6).ToString("F2")+"ms ");
                    Log("CRYPT_HITCH "+window+" frame="+samples.Count+" t="+(Time.realtimeSinceStartup-start).ToString("F2")+"s ms="+ms.ToString("F2")+" gc0="+(c0-g0)+" gc1="+(c1-g1)+" gc2="+(c2-g2)
                        +" alloc="+(alloc.Valid?alloc.LastValue.ToString():"n/a")+" texDelta="+((long)t-(long)tex)+" texStreamingLoading="+Texture.streamingTextureLoadingCount+" texPending="+Texture.streamingTexturePendingLoadCount
                        +" navBuilds="+(CastleWorld.NavBuilds-nav0)+" markers=["+sb+"]");
                }
                g0=c0;g1=c1;g2=c2;tex=t;
            }
            int audioLoaded1=0;foreach(var c in Resources.FindObjectsOfTypeAll<AudioClip>())if(c.loadState==AudioDataLoadState.Loaded)audioLoaded1++;
            foreach(var r in recs)r.Dispose();alloc.Dispose();texMem.Dispose();
            samples.Sort();float sum=0;foreach(var x in samples)sum+=x;
            Log("FRAMETIME crypt_"+window+" frames="+samples.Count+" mean="+(sum/samples.Count).ToString("F2")+"ms p99="+samples[Mathf.Min(samples.Count-1,(int)(samples.Count*.99f))].ToString("F2")+"ms max="+samples[samples.Count-1].ToString("F2")+"ms hitches_over_33ms="+hitches+" audioClipsLoadedDuring="+(audioLoaded1-audioLoaded0));
        }
        // The investigator's flashlight as in play (Investigator input is off in capture); the castle's TorchScale is applied like Investigator.Update does.
        void Torch(bool on){var l=game.Player.Torch;l.enabled=on;l.intensity=.85f*game.Player.Brightness*game.Player.TorchScale;}
        // -hc-capture-lantern off|on (wave FB): the grave-ash lantern state for the world shots (default off = the kit is not collected, as before).
        // "on" lights the lantern for every world shot (S1-S3, the art views), so tools/exposure_report.py --lantern on judges the lantern-lit
        // interior (12-25 % near-black); "off" (default) is the dark-room case (35-55 %). S5/S6 and the hunt frames always had the lantern on.
        public static bool LanternOn{get{var a=Environment.GetCommandLineArgs();for(int i=0;i<a.Length-1;i++)if(a[i]=="-hc-capture-lantern")return a[i+1]=="on";return false;}}
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
            if(ArtSet!=null){yield return ArtRun(ArtSet);yield break;}
            Log("screen "+Screen.width+"x"+Screen.height+" quality="+QualitySettings.names[QualitySettings.GetQualityLevel()]+" aa="+QualitySettings.antiAliasing+" shadows="+QualitySettings.shadowResolution+"/"+QualitySettings.shadowDistance+" targetFps="+Application.targetFrameRate+" vsync="+QualitySettings.vSyncCount);
            // S7: title screen with the map picker (Castle selected).
            game.SelectMap(MapId.Castle);
            yield return new WaitForSecondsRealtime(1);
            // Blind critic r10 V9: in the combined player the case has already started here, so this is the gatehouse HUD, not the
            // title (title captures come from -hc-capture-title). Named by content.
            yield return Shot(game.Started?"S7_gatehouse_hud_case_start":"S7_title",game.Started?"(gameplay HUD)":"(title screen)",Vector3.zero,Vector3.zero,game.Started?"gatehouse HUD at case start (dispatch card)":"title with map picker");
            // World shots: no HUD, investigator input off, kit not collected (torch off).
            game.StartCase();ui.enabled=false;yield return null;yield return null;
            game.Player.enabled=false;
            Torch(LanternOn);Log("lantern "+(LanternOn?"on":"off")+" torchScale="+game.Player.TorchScale.ToString("F2")+" intensity="+game.Player.Torch.intensity.ToString("F2")+" range="+game.Player.Torch.range.ToString("F1")+" angle="+game.Player.Torch.spotAngle.ToString("F0")+" mode="+game.Player.Torch.renderMode);
            var spots=game.World.Castle;
            // S1: great hall from the entrance doors at eye level.
            var e=new Vector3(0,1.64f,14.7f);var t=new Vector3(0,2.6f,27);Place(e,t);
            yield return new WaitForSecondsRealtime(1.5f);yield return Measure("S1_great_hall",3);
            yield return Shot("S1_great_hall","refs/great_hall.png",e,t);CandleFlame.LogLeanReport("S1");SetPiece.Report(game.Player.View,"S1");
            // S2: chapel altar from the nave.
            // Round 7: pitched 10 degrees down toward the altar top so its relic sockets read.
            e=new Vector3(7.0f,1.64f,6.5f);t=e+new Vector3(Mathf.Cos(10*Mathf.Deg2Rad),-Mathf.Sin(10*Mathf.Deg2Rad),0)*6;Place(e,t);
            yield return new WaitForSecondsRealtime(1.5f);yield return Shot("S2_chapel_altar","refs/chapel_altar.png",e,t);SetPiece.Report(game.Player.View,"S2");
            // S3: crypt from the stair foot looking at the sarcophagus.
            // Round 7: eye from hc/arch7's HANDOFF (crypt axis, looking at the sarcophagus).
            e=new Vector3(0,-1.56f,15.4f);t=new Vector3(0,-2.3f,24.0f);Place(e,t);
            yield return new WaitForSecondsRealtime(1.5f);yield return Shot("S3_crypt","refs/crypt.png",e,t);SetPiece.Report(game.Player.View,"S3");
            // S4 (round 5 brief; the 2.2 m eye limit is lifted for this shot only): pulled
            // back along the bridge axis to 14 m from the gate at 3.5 m. Pitch and FOV are
            // searched so both moons sit fully in open sky (centre inset by the disc radius,
            // not occluded), the gate base sits in the lower third and the lowered bridge's
            // far end stays in frame. Each moon is logged in-frame/occluded.
            if(spots!=null&&spots.Drawbridge)spots.Drawbridge.Snap(true);
            Physics.SyncTransforms();
            float fov=game.Player.View.fieldOfView;
            // Round 6: the search also walks eye height 3.5 -> 2.5 m and distance 14 -> 18 m back
            // from the gate; the bridge constraint is only "far end of the lowered leaf in frame".
            e=S4Eye;float s4Pitch=S4Pitch,s4Fov=S4Fov;string s4="fallback";
            var moonDirs=new[]{("pale",CastleArt.PaleMoonDirection,.19f),("red",CastleArt.RedMoonDirection,.11f)}; // disc radius + 4% margin (r6: pale moon touched the top edge)
            foreach(float back in new[]{14f,15f,16f,17f,18f})foreach(float height in new[]{3.5f,3f,2.5f})foreach(float f in new[]{90f,95f})
            {
                if(s4!="fallback")break;
                var eye=new Vector3(0,height,-back);
                for(float p=0;p<=30.01f&&s4=="fallback";p+=1.5f)
                {
                    game.Player.View.fieldOfView=f;var aim=eye+Quaternion.Euler(-p,0,0)*Vector3.forward*20;Place(eye,aim);
                    var cam=game.Player.View;bool ok=true;
                    foreach(var m in moonDirs)
                    {
                        var vp=cam.WorldToViewportPoint(eye+m.Item2*500);
                        if(vp.z<0||vp.x<m.Item3||vp.x>1-m.Item3||vp.y<m.Item3||vp.y>1-m.Item3||Physics.Raycast(eye,m.Item2,600,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore))ok=false;
                    }
                    var gate=cam.WorldToViewportPoint(new Vector3(0,0,0));if(gate.z<0||gate.y<.08f||gate.y>.34f)ok=false;
                    var leafEnd=cam.WorldToViewportPoint(new Vector3(0,0,-6.45f));if(leafEnd.z<0||leafEnd.y<0||leafEnd.y>1||leafEnd.x<0||leafEnd.x>1)ok=false;
                    if(ok){e=eye;s4Pitch=p;s4Fov=f;s4="eye="+V(eye)+" pitch="+p+" fov="+f;}
                }
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
            yield return new WaitForSecondsRealtime(1.5f);yield return Shot("S4_exterior_sky","refs/exterior_sky.png",e,t,"eye "+V(e)+", search "+s4+";"+moons);SetPiece.Report(game.Player.View,"S4");
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
            // r5: RoseLedgeFraming puts the eye on the line from CastleLayout.RoseWindowEye through
            // him (that line rises ~21 deg) at the distance fill 0.58 needs at FOV 72 (~2.8 m), so the
            // camera sat close below him. Here the same target is shot from a flatter, farther eye:
            // pitch 4 deg up, FOV narrowed so his posed height still fills 58% of the frame.
            float s5Fov=game.Player.View.fieldOfView;var hidden=new List<Renderer>();
            // r8 first pass: Presence.HideFloatersOnViewLine (RoseLedgeFraming) deactivated the six
            // neverHide ledge candelabras too (HC_S5_CANDLES 0). Re-show them for the shot.
            int reshown=0;
            foreach(var floater in FindObjectsByType<Floater>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(floater&&floater.neverHide&&!floater.gameObject.activeSelf){floater.gameObject.SetActive(true);reshown++;}
            Log("S5 neverHide floaters re-shown "+reshown);
            if(s5.StartsWith("rose ledge",StringComparison.Ordinal))
            {
                // Round 7: fill by the body (feet to head top), not the pose bounds incl. wings
                // (r6 measured 34.7% of frame height against a 58% request).
                var pose=game.Creature.Actor.PoseBounds(0);
                float feetY=game.Creature.transform.position.y,headTop=(game.Creature.Actor.Head?game.Creature.Actor.Head.position.y:pose.max.y)+.15f;
                // r7 first pass: the demon's perch crouch puts his head top only 0.94 m above his feet
                // while the hands hang below the ledge, so head-to-feet alone framed him at 1.7 m and
                // cropped him. The visible figure measured ~0.6 of PoseBounds in r6 (34.7% at a 58%
                // request), so the framed height is the larger of the two, centred on the pose.
                float bodyHeight=Mathf.Max(.5f,Mathf.Max(headTop-feetY,.6f*pose.size.y));var bodyCentre=pose.center;
                var flat=t-e;flat.y=0;if(flat.sqrMagnitude<.01f)flat=Vector3.forward;flat.Normalize();
                const float s5ViewFov=50,fill=.58f,pitch=4;
                float d=bodyHeight/(fill*2*Mathf.Tan(s5ViewFov*.5f*Mathf.Deg2Rad));
                t=bodyCentre;e=t-flat*d*Mathf.Cos(pitch*Mathf.Deg2Rad)-Vector3.up*d*Mathf.Sin(pitch*Mathf.Deg2Rad);
                game.Player.View.fieldOfView=s5ViewFov;
                // Floaters within 1.2 m of the new eye-to-Count line are hidden for this shot only.
                foreach(var floater in Floater.All)
                {
                    if(!floater||floater.neverHide)continue;var q=floater.transform.position;var seg=t-e;float u=Mathf.Clamp01(Vector3.Dot(q-e,seg)/seg.sqrMagnitude);
                    if(Vector3.Distance(q,e+seg*u)<1.2f)foreach(var r in floater.GetComponentsInChildren<Renderer>())if(r.enabled){r.enabled=false;hidden.Add(r);}
                }
                s5+=" -> flattened: pitch "+pitch+" deg, FOV "+s5ViewFov+", distance "+d.ToString("F2")+" m, framed height max(head-to-feet "+(headTop-feetY).ToString("F2")+", 0.6 x pose) "+bodyHeight.ToString("F2")+" m, pose height "+pose.size.y.ToString("F2")+" m, floaters hidden "+hidden.Count;
            }
            Place(e,t);
            int moonsInFrame=0;
            foreach(var tr in game.World.Root.GetComponentsInChildren<Transform>(true))if(tr.name=="Pale moon"||tr.name=="Red moon"){var vp=game.Player.View.WorldToViewportPoint(tr.position);if(vp.z>0&&vp.x>0&&vp.x<1&&vp.y>0&&vp.y<1)moonsInFrame++;}
            Log("S5 "+s5+" eye="+V(e)+" target="+V(t)+" moonSpritesInFrame="+moonsInFrame);
            yield return new WaitForSecondsRealtime(.5f);yield return Shot("S5_lord_perch","refs/lord_perch.png",e,t,s5+"; moon sprites in frame "+moonsInFrame+"; Count feet "+V(game.Creature.transform.position)+", investigator torch on");
            CandleFlame.LogLeanReport("S5");
            foreach(var r in hidden)if(r)r.enabled=true;game.Player.View.fieldOfView=s5Fov;
            game.Creature.Dismiss();
            // S6: Count Claude standing on the courtyard floor (Mode 2 Search) with a 1.80 m
            // mannequin (Presence.ScaleReference) beside him for scale.
            // S6 in the gatehouse facing the gate (hc/count4): the gatehouse table and the
            // courtyard doorway are behind the camera.
            game.Creature.ManifestAt(new Vector3(-.2f,0,2.2f),0,9999,true);
            var man=Presence.ScaleReference(new Vector3(1.05f,0,2.2f),0);
            // Round 7: the demon's head is ~2.5 m; aim at mid-height so head, feet and the 1.80 m
            // figure stay in frame, and log their viewport positions.
            e=new Vector3(.4f,1.3f,5.6f);t=new Vector3(.4f,1.25f,2.2f);Place(e,t);
            yield return new WaitForSecondsRealtime(1.5f);
            {
                var cam=game.Player.View;var head=game.Creature.Actor.Head?game.Creature.Actor.Head.position+Vector3.up*.15f:new Vector3(-.2f,2.5f,2.2f);
                string Vp(Vector3 w){var v=cam.WorldToViewportPoint(w);return "("+v.x.ToString("F2")+","+v.y.ToString("F2")+")";}
                Log("S6 framing countHeadTop="+Vp(head)+" countFeet="+Vp(new Vector3(-.2f,0,2.2f))+" figureTop="+Vp(new Vector3(1.05f,1.8f,2.2f))+" figureFeet="+Vp(new Vector3(1.05f,0,2.2f))+" headTopY="+head.y.ToString("F2"));
            }
            yield return Shot("S6_lord_sheet","refs/lord_sheet.png",e,t,"gatehouse facing the gate (eye 0.4,1.3,5.6); Count feet (-0.2,0,2.2) Mode "+game.Creature.Profile.ModeSearch+", 1.80 m ScaleReference at (1.05,0,2.2), investigator torch on");
            game.Creature.Dismiss();Torch(LanternOn);CastleRun.ClearFlameTarget();Destroy(man);
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
            game.Case.BeginWarning();game.Creature.SetHuntOrigin(origin);game.Case.Tick(game.Case.CurrentWarningSeconds+.05f,true); // wave H: warnings are drawn 5-14 s
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
                if(i==4)CandleFlame.LogLeanReport("H05");if(i==7)CandleFlame.LogLeanReport("H08");
            }
            game.Creature.EndCaptureChoreography();
            Record("hunt_dive",samples);
            File.WriteAllText(Path.Combine(Directory,"capture.json"),"{\"unity\":\""+Application.unityVersion+"\",\"screen\":\""+Screen.width+"x"+Screen.height+"\",\"quality\":\""+QualitySettings.names[QualitySettings.GetQualityLevel()]+"\",\"antiAliasing\":"+QualitySettings.antiAliasing+",\"shadowResolution\":\""+QualitySettings.shadowResolution+"\",\"shadowDistance\":"+QualitySettings.shadowDistance+",\"targetFrameRate\":"+Application.targetFrameRate+",\"vSync\":"+QualitySettings.vSyncCount+",\"fov\":"+game.Player.View.fieldOfView+",\"entries\":[\n"+string.Join(",\n",entries)+"\n]}\n");
            Log("DONE "+entries.Count+" entries");
            Game.RestoreOwnerWindow();yield return null;yield return null;
            Application.Quit();
        }
    }
}
