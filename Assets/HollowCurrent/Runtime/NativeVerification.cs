using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace HollowCurrent
{
    // Deliberate local native integration test. No provider, OS automation or network.
    // Traverses with the real CharacterController and the same equipment code.
    public sealed class NativeVerification : MonoBehaviour
    {
        public static bool Requested => Array.IndexOf(Environment.GetCommandLineArgs(),"-hc-verify")>=0;
        public static bool EnhancementsOnly => Array.IndexOf(Environment.GetCommandLineArgs(),"-hc-enhancements")>=0;
        public static bool ArtOnly => Array.IndexOf(Environment.GetCommandLineArgs(),"-hc-art-only")>=0;
        public static bool ProfileOnly => Array.IndexOf(Environment.GetCommandLineArgs(),"-hc-profile")>=0;
        public static bool KitOnly => Array.IndexOf(Environment.GetCommandLineArgs(),"-hc-kit-only")>=0;
        public static bool UIOnly => Array.IndexOf(Environment.GetCommandLineArgs(),"-hc-ui-only")>=0;
        [Serializable] sealed class NativeReport
        {
            public bool passed;public int width,height,frames,frames_over_33ms,frames_over_16ms,audio_cues,speech_takes,animated_frames,character_bones;
            public float p50_ms,p95_ms,p99_ms;
            public string unity,graphics,cpu,scope,color_space;
            public bool authored_scene,captures_enabled,performance_pass;
            public FrameStatistics timing;
            public int excluded_loading_frames;
            public float excluded_loading_ms;
            public SlowFrame[] slow_frames;
            public FrameStatistics ui_timing;
            public SlowFrame[] ui_slow_frames;
            public int higgsfield_cues,music_frames,breathing_frames,ducked_frames,music_state_mask,audio_nonzero_samples;
            public float audio_output_peak;
        }
        [Serializable] sealed class SlowFrame
        {
            public int sample,gc0,gc1,gc2,tool;
            public float elapsed_seconds,frame_ms;
            public string phase;
            public Vector3 position;
        }
        Game game;
        string folder;
        readonly List<string> events=new List<string>();
        readonly List<float> frames=new List<float>(120000);
        float start;
        bool finished;
        int previousAudio,previousSpeech,previousAnimation;
        int previousGenerated,previousMusic,previousBreathing,previousDucked,previousMusicMask;
        readonly float[] audioProbe=new float[512];
        int audioNonzero;
        float audioPeak,audioProbeAt;
        int loadingFrames,loadingCaptures;
        float loadingMilliseconds;
        bool loadingCaptureQueued;
        readonly List<SlowFrame> slowFrames=new List<SlowFrame>(64);
        readonly List<float> uiFrames=new List<float>(4000);
        readonly List<SlowFrame> uiSlowFrames=new List<SlowFrame>(32);
        int lastGc0,lastGc1,lastGc2;
        bool previousIntervalPlayable;
        Game previousIntervalGame;
        void Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-hc-evidence");
            folder=i>=0&&i+1<args.Length?args[i+1]:Path.Combine(Application.persistentDataPath,"verification");
            Directory.CreateDirectory(folder);start=Time.realtimeSinceStartup;
            StartCoroutine(Guard(Route()));
        }
        void LateUpdate()
        {
            if(Time.unscaledTime>=audioProbeAt)
            {
                audioProbeAt=Time.unscaledTime+.1f;AudioListener.GetOutputData(audioProbe,0);
                foreach(float sample in audioProbe){float level=Mathf.Abs(sample);audioPeak=Mathf.Max(audioPeak,level);if(level>.00001f)audioNonzero++;}
            }
            int gc0=GC.CollectionCount(0),gc1=GC.CollectionCount(1),gc2=GC.CollectionCount(2);
            if(game!=null&&game.Loading)
            {
                loadingFrames++;loadingMilliseconds+=Time.unscaledDeltaTime*1000;
                if(!ProfileOnly&&!loadingCaptureQueued){loadingCaptureQueued=true;StartCoroutine(CaptureLoadingFrame(game));}
                previousIntervalPlayable=false;previousIntervalGame=game;
                lastGc0=gc0;lastGc1=gc1;lastGc2=gc2;return;
            }
            loadingCaptureQueued=false;
            bool playable=game!=null&&game.Started&&!game.Paused&&!game.Journal&&game.NoteBody==null&&!finished;
            if(playable&&previousIntervalPlayable&&previousIntervalGame==game)
            {
                float ms=Time.unscaledDeltaTime*1000;frames.Add(ms);
                if(ms>16.667f)slowFrames.Add(new SlowFrame{sample=frames.Count,elapsed_seconds=Time.realtimeSinceStartup-start,frame_ms=ms,phase=game.Case.Phase.ToString(),position=game.Player.transform.position,tool=game.Player.Tool,gc0=gc0-lastGc0,gc1=gc1-lastGc1,gc2=gc2-lastGc2});
            }
            else if(playable&&previousIntervalGame!=game){loadingFrames++;loadingMilliseconds+=Time.unscaledDeltaTime*1000;}
            else if(game!=null&&game.Started&&previousIntervalGame==game&&!finished)
            {
                float ms=Time.unscaledDeltaTime*1000;uiFrames.Add(ms);
                if(ms>16.667f)uiSlowFrames.Add(new SlowFrame{sample=uiFrames.Count,elapsed_seconds=Time.realtimeSinceStartup-start,frame_ms=ms,phase="UI transition / "+game.Case.Phase,position=game.Player.transform.position,tool=game.Player.Tool,gc0=gc0-lastGc0,gc1=gc1-lastGc1,gc2=gc2-lastGc2});
            }
            previousIntervalPlayable=playable;previousIntervalGame=game;
            lastGc0=gc0;lastGc1=gc1;lastGc2=gc2;
        }
        IEnumerator CaptureLoadingFrame(Game current)
        {
            yield return new WaitForEndOfFrame();
            if(current&&current.Loading){ScreenCapture.CaptureScreenshot(Path.Combine(folder,"ui-loading-"+(++loadingCaptures)+".png"));Record("Native loading screen presented before scene activation");}
        }
        void Record(string text){events.Add((Time.realtimeSinceStartup-start).ToString("F2",CultureInfo.InvariantCulture)+" "+text);if(!ProfileOnly){Debug.Log("HC_VERIFY "+text);File.WriteAllLines(Path.Combine(folder,"route-events.txt"),events);}}
        void Require(bool condition,string message){if(!condition)throw new Exception(message);Record("PASS "+message);}
        IEnumerator Guard(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            while(stack.Count>0)
            {
                object next=null;bool moved=false;Exception error=null;
                try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}
                catch(Exception e){error=e;}
                if(error!=null)
                {
                    yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"failure.png"));
                    yield return null;yield return null;Complete(false,error.ToString());yield break;
                }
                if(!moved){stack.Pop();continue;}
                if(next is IEnumerator nested)stack.Push(nested);else yield return next;
            }
            Complete(true,EnhancementsOnly?"Native Split Grin poses, fake rush, equipment failures, doors, grounding, thrown prop, basement flash, van pursuit, checkpoint and fresh retry passed.":UIOnly?"Prepared native UI layouts, actual keyboard reconstruction and asynchronous retry transitions passed; this is not a gameplay-route result.":KitOnly?"Original field-kit meshes, switching, radio, independent lighting and all three actual evidence captures passed.":ArtOnly?"Native three-floor art/controller inspection passed; no ending/death assertion in this focused run.":"Native controller/tool route, ending, forced death scenario and retry passed. Human input and first-time pacing remain separate.");
        }
        void Complete(bool passed,string detail)
        {
            finished=true;Record((passed?"PASS ":"FAIL ")+detail);
            var timing=FrameStatistics.Measure(frames);
            var uiTiming=FrameStatistics.Measure(uiFrames);
            File.WriteAllLines(Path.Combine(folder,"route-events.txt"),events);
            var report=new NativeReport{passed=passed,width=Screen.width,height=Screen.height,frames=frames.Count,
                p50_ms=timing.p50_ms,p95_ms=timing.p95_ms,p99_ms=timing.p99_ms,frames_over_33ms=frames.FindAll(x=>x>33.33f).Count,frames_over_16ms=frames.FindAll(x=>x>16.667f).Count,animated_frames=previousAnimation+game.Creature.Actor.AnimatedFrames,character_bones=game.Creature.Actor.BoneCount,
                unity=Application.unityVersion,graphics=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,
                scope=EnhancementsOnly?"enhancement scenarios; not a full investigation":UIOnly?"prepared native UI, keyboard reconstruction and loading":KitOnly?"equipment":ArtOnly?"three-floor visual/controller":ProfileOnly?"capture-free full investigation and retry":"full investigation and retry",
                color_space=QualitySettings.activeColorSpace.ToString(),authored_scene=FindFirstObjectByType<WorldLayout>()!=null,
                higgsfield_cues=previousGenerated+game.Sound.GeneratedCues,music_frames=previousMusic+game.Sound.MusicFrames,breathing_frames=previousBreathing+game.Sound.BreathingFrames,ducked_frames=previousDucked+game.Sound.DuckedFrames,music_state_mask=previousMusicMask|game.Sound.MusicMask,audio_nonzero_samples=audioNonzero,audio_output_peak=audioPeak,
                audio_cues=previousAudio+game.Sound.PlayedCues,speech_takes=previousSpeech+game.Sound.PlayedSpeech,captures_enabled=!ProfileOnly,timing=timing,performance_pass=passed&&ProfileOnly&&timing.meets_60_fps_floor&&uiTiming.frames_over_16ms==0,excluded_loading_frames=loadingFrames,excluded_loading_ms=loadingMilliseconds,slow_frames=slowFrames.ToArray(),ui_timing=uiTiming,ui_slow_frames=uiSlowFrames.ToArray()};
            File.WriteAllText(Path.Combine(folder,"native-report.json"),JsonUtility.ToJson(report,true));
            Debug.Log(passed?(EnhancementsOnly?"HC_NATIVE_ENHANCEMENTS_PASS":UIOnly?"HC_NATIVE_UI_PASS":KitOnly?"HC_NATIVE_KIT_PASS":ArtOnly?"HC_NATIVE_ART_PASS":"HC_NATIVE_ROUTE_PASS"):"HC_NATIVE_ROUTE_FAIL");Application.Quit(passed?0:2);
        }
        void RetryCase()
        {previousAudio+=game.Sound.PlayedCues;previousSpeech+=game.Sound.PlayedSpeech;previousAnimation+=game.Creature.Actor.AnimatedFrames;previousGenerated+=game.Sound.GeneratedCues;previousMusic+=game.Sound.MusicFrames;previousBreathing+=game.Sound.BreathingFrames;previousDucked+=game.Sound.DuckedFrames;previousMusicMask|=game.Sound.MusicMask;game.Retry();}
        IEnumerator RestartAndBind()
        {
            var previous=game;RetryCase();float deadline=Time.realtimeSinceStartup+30;
            while(true)
            {
                var next=FindFirstObjectByType<Game>();if(next&&next!=previous&&!next.Loading)break;
                if(Time.realtimeSinceStartup>deadline)throw new Exception("Asynchronous retry did not create a fresh case");yield return null;
            }
            yield return null;Bind();
        }
        IEnumerator CaptureUI(string name)
        {yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return null;yield return new WaitForEndOfFrame();yield return null;Record("Capture "+name);}
        IEnumerator UIRoute()
        {
            game=FindFirstObjectByType<Game>();Require(Screen.width==1920&&Screen.height==1080,"Native UI at 1920x1080");
            float originalSensitivity=game.Player.Sensitivity,originalBrightness=game.Player.Brightness,originalVolume=game.Volume;
            bool originalInvert=game.Player.InvertY,originalSubtitles=game.Subtitles,originalHelp=game.HelpHints;
            yield return CaptureUI("ui-title");game.Paused=true;game.ApplyFocus();yield return CaptureUI("ui-title-settings");game.Resume();Bind();
            game.Paused=true;game.ApplyFocus();yield return CaptureUI("ui-pause-settings");game.Resume();
            game.Journal=true;game.ApplyFocus();yield return CaptureUI("ui-journal-empty");game.Journal=false;game.ApplyFocus();
            var note=Item(InteractKind.Note,"Child's drawing");note.Use(game);yield return CaptureUI("ui-note");game.NoteBody=null;game.ApplyFocus();
            Record("Prepare evidence and hunt state for UI-only keyboard validation; no gameplay-route claim");
            game.Case.Equip();game.Case.EnterHouse();foreach(Evidence e in Enum.GetValues(typeof(Evidence)))game.Case.Record(e);
            foreach(string title in new[]{"Family photograph","Child's drawing","Furnace diary"})game.Case.Read(title);
            game.Case.BeginWarning();game.Case.Tick(9,false);game.Case.Tick(38,false);
            game.Journal=true;game.ApplyFocus();yield return CaptureUI("ui-journal-ready");
            Record("HC_UI_AWAIT_RECONSTRUCTION press 3, 4, 7 then Enter through normal OS input");
            float deadline=Time.realtimeSinceStartup+600;
            while(!game.Case.Diagnosed){if(Time.realtimeSinceStartup>deadline)throw new Exception("Native keyboard reconstruction not submitted");yield return null;}
            Require(game.Case.Phase==Phase.Containment,"Actual journal keyboard submission reaches containment");
            game.Journal=true;game.ApplyFocus();yield return CaptureUI("ui-journal-filed");game.Journal=false;game.ApplyFocus();
            game.Case.RestorePower();game.Case.Seal(12,true,true);game.Case.FinishAtVan();yield return CaptureUI("ui-results");
            yield return RestartAndBind();Require(game.Case.Phase==Phase.Briefing,"UI retry loads a fresh case");
            game.Case.BeginWarning();game.Case.Tick(9,false);game.Case.Die();yield return new WaitForSecondsRealtime(3.1f);yield return CaptureUI("ui-death");
            yield return RestartAndBind();Require(loadingCaptures>=2,"Both native loading transitions captured");
            Require(Mathf.Abs(game.Player.Sensitivity-originalSensitivity)<.001f&&Mathf.Abs(game.Player.Brightness-originalBrightness)<.001f&&Mathf.Abs(game.Volume-originalVolume)<.001f&&game.Player.InvertY==originalInvert&&game.Subtitles==originalSubtitles&&game.HelpHints==originalHelp,"Restored settings persist through both scene reloads");
        }
        void Bind()
        {
            game=FindFirstObjectByType<Game>();game.StartCase();game.Player.TestInput=new FieldInput();
            game.Case.Changed+=p=>Record("Phase "+p);
        }
        IEnumerator Look(Vector3 at)
        {
            game.Player.TestInput=new FieldInput{Look=at};yield return null;yield return null;
        }
        IEnumerator Walk(Vector3 destination)
        {
            var p=game.Player;Record("Walk to "+destination);
            if(!NavMesh.SamplePosition(p.transform.position,out var origin,1,NavMesh.AllAreas)||!NavMesh.SamplePosition(destination,out var target,.7f,NavMesh.AllAreas))throw new Exception("No nav sample for walking route");
            var path=new NavMeshPath();
            if(!NavMesh.CalculatePath(origin.position,target.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("No complete walking route: "+destination);
            foreach(var corner in path.corners)
            {
                var walkingInput=new FieldInput{Move=Vector2.up};
                float limit=Time.realtimeSinceStartup+Vector3.Distance(p.transform.position,corner)/1.2f+9;
                while(new Vector2(p.transform.position.x-corner.x,p.transform.position.z-corner.z).magnitude>.22f||Mathf.Abs(p.transform.position.y-corner.y)>.5f)
                {
                    if(game.Case.Terminal)throw new Exception("Case terminated during route "+game.Case.Phase);
                    if(Time.realtimeSinceStartup>limit)
                    {
                        var nearby=Physics.OverlapCapsule(p.transform.position+Vector3.up*.3f,p.transform.position+Vector3.up*1.5f,.38f,~(1<<2),QueryTriggerInteraction.Ignore);
                        string names="";foreach(var c in nearby)names+=c.name+"; ";
                        throw new Exception("Controller stuck at "+p.transform.position+" toward "+corner+" final "+destination+" nearby="+names);
                    }
                    var look=new Vector3(corner.x,p.View.transform.position.y,corner.z);
                    walkingInput.Look=look;walkingInput.Use=p.Focus!=null&&p.Focus.Kind==InteractKind.Door&&!p.Focus.Door.Open;p.TestInput=walkingInput;
                    yield return null;
                }
            }
            p.TestInput=new FieldInput();yield return null;
        }
        Interactable Item(InteractKind kind,string name=null)
        {
            foreach(var x in game.World.Root.GetComponentsInChildren<Interactable>())if(x.Kind==kind&&(name==null||x.name==name))return x;
            throw new Exception("Missing interactable "+kind+" / "+name);
        }
        IEnumerator Use(Interactable item)
        {
            yield return Look(item.transform.position);
            if(game.Player.Focus!=item)
            {
                var p=game.Player;string hitName=Physics.Raycast(p.View.transform.position,p.View.transform.forward,out var hit,2.7f)?hit.collider.name:"nothing";
                Record("Focus diagnostic: modal="+game.Modal+" pos="+p.View.transform.position+" forward="+p.View.transform.forward+" target="+item.transform.position+" hit="+hitName);
            }
            Require(game.Player.Focus==item,"Raycast focus "+item.name);
            game.Player.TestInput.Use=true;yield return null;game.Player.TestInput.Use=false;yield return null;
        }
        IEnumerator Capture(string name,Vector3 at)
        {
            yield return Look(at);yield return new WaitForSecondsRealtime(.25f);
            yield return new WaitForEndOfFrame();if(!ProfileOnly)ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));
            yield return null;yield return new WaitForEndOfFrame();yield return null;Record("Capture "+name);
        }
        IEnumerator ReadContext(string name,Vector3 approach)
        {
            yield return Walk(approach);yield return Use(Item(InteractKind.Note,name));
            Require(game.NoteBody!=null,"Context note opens: "+name);game.NoteBody=null;game.ApplyFocus();
        }
        IEnumerator HideCheck(string name)
        {
            var spot=Item(InteractKind.Hide,name);yield return Walk(spot.ExitPoint);yield return Use(spot);
            Require(game.Player.Hidden,"Enter "+name);game.Player.TestInput.Use=true;yield return null;game.Player.TestInput.Use=false;yield return null;
            Require(!game.Player.Hidden,"Exit "+name);
        }
        IEnumerator RecordWithTool(Evidence evidence,int tool,Vector3 target)
        {
            float deadline=Time.realtimeSinceStartup+35;
            while(!game.Case.Evidence.Contains(evidence))
            {
                if(game.Case.Terminal||Time.realtimeSinceStartup>deadline)throw new Exception("Evidence recording did not complete: "+evidence);
                if(game.Scares.Interference){yield return null;continue;}
                game.Player.TestInput=new FieldInput{Look=target,SelectTool=tool,Fire=tool==3,FireHeld=tool!=3};yield return null;game.Player.TestInput.Fire=false;
                yield return new WaitForSeconds(tool==3?4.6f:3.5f);game.Player.TestInput=new FieldInput();
            }
        }
        IEnumerator EnhancementsRoute()
        {
            foreach(var door in game.World.Doors)
            {
                door.Open=true;yield return new WaitForSeconds(1.3f);
                Require(Mathf.Abs(door.OpenAngle)>=80&&door.HasClearOpenPose,"Door clears static geometry: "+door.name+" angle="+door.OpenAngle);
            }
            foreach(var t in game.World.Root.GetComponentsInChildren<Transform>())
            {
                if(!t.name.StartsWith("Original crafted ChildBed")&&!t.name.StartsWith("Original crafted ParentsBed"))continue;
                float bottom=float.MaxValue;foreach(var r in t.GetComponentsInChildren<Renderer>())bottom=Mathf.Min(bottom,r.bounds.min.y);
                Require(Mathf.Abs(bottom-3.2f)<.02f,"Bed feet contact upstairs floor: "+t.name);
            }
            game.Player.Teleport(new Vector3(0,0,2));game.Case.EnterHouse();yield return Look(new Vector3(0,1.5f,7));
            Require(game.Creature.ManifestAt(new Vector3(0,0,7),180,10,true),"Visible apparition fixture starts");
            game.Creature.FacePlayer();game.Say("Dispatch / The signal is changing. Keep your light steady.");game.Scares.Pulse(new Vector3(0,0,7));yield return null;
            Require(game.Scares.TorchFailed&&game.Scares.Interference&&!game.Player.Torch.enabled,"Scare temporarily cuts flashlight and scrambles equipment");
            yield return Capture("splitgrin-fog",new Vector3(0,1.5f,7));
            yield return new WaitForSeconds(2);Require(!game.Scares.TorchFailed&&game.Player.Torch.enabled,"Flashlight automatically recovers");
            for(int mode=0;mode<4;mode++)
            {game.Creature.ManifestAt(new Vector3(0,0,7),180,5,true);game.Creature.Actor.Pose(mode,3);yield return new WaitForSeconds(.4f);yield return Capture("splitgrin-pose-"+mode,new Vector3(0,1.35f,7));}
            game.Creature.ManifestAt(new Vector3(0,0,7),90,6,true);game.Creature.Actor.Pose(1,3.15f);
            for(int sample=0;sample<4;sample++){yield return new WaitForSeconds(.1f);yield return Capture("run-profile-"+sample,new Vector3(0,1.05f,7));}
            Record("Run posture head height="+(game.Creature.Actor.Head.position.y-game.Creature.transform.position.y));
            game.Say("Evidence secured / An impossible movement. The record is saved in your journal.");yield return Capture("dead-air-evidence",new Vector3(0,1.35f,7));
            float oldSize=game.SubtitleScale,oldOpacity=game.CardOpacity;game.SubtitleScale=1.15f;game.CardOpacity=.82f;game.SavePreferences();
            FindFirstObjectByType<Interface>().ShowDisplayPreferences();yield return CaptureUI("dead-air-display-settings");
            bool saved=Mathf.Abs(PlayerPrefs.GetFloat("hc.subtitle-scale")-1.15f)<.01f&&Mathf.Abs(PlayerPrefs.GetFloat("hc.card-opacity")-.82f)<.01f;
            game.SubtitleScale=oldSize;game.CardOpacity=oldOpacity;game.SavePreferences();game.Resume();Require(saved,"Dialogue size and opacity preferences save correctly");
            game.Creature.ManifestAt(new Vector3(0,0,7),180,1.6f,true);game.Creature.FacePlayer();game.Creature.BeginFakeRush();
            float startDistance=Vector3.Distance(game.Creature.transform.position,game.Player.transform.position);yield return new WaitForSeconds(.25f);
            Require(Vector3.Distance(game.Creature.transform.position,game.Player.transform.position)<startDistance-.4f,"Fake rush moves toward the player");
            yield return Capture("splitgrin-rush",game.Creature.Actor.Head.position);yield return new WaitForSeconds(2);
            Require(!game.Case.Terminal&&game.Creature.PresentationRemaining<=0,"Fake rush vanishes without killing");
            game.Player.Teleport(new Vector3(-4.55f,0,5.5f));yield return Look(new Vector3(-5.15f,.63f,4.4f));
            Require(game.Scares.ThrowNearbyProp(),"Nearby visible cup becomes a thrown rigidbody");
            var thrown=game.Scares.LastThrownProp;var cupRenderer=thrown.GetComponentInChildren<Renderer>();var cupStart=cupRenderer.bounds.center;float playerY=game.Player.transform.position.y;
            Require(!cupRenderer.isPartOfStaticBatch&&thrown.GetComponent<BoxCollider>().size.magnitude<.4f,"Throwable cup has independent render geometry and cup-sized collision");
            yield return new WaitForSeconds(.2f);
            Require(Vector3.Distance(cupRenderer.bounds.center,cupStart)>.1f&&Mathf.Abs(game.Player.transform.position.y-playerY)<.2f,"Thrown cup visibly moves without displacing the investigator");
            yield return Capture("cup-thrown",new Vector3(-5.2f,.6f,4.6f));
            game.Player.Teleport(new Vector3(0,-3.2f,9));yield return Look(new Vector3(5,-1.6f,10));
            float end=Time.realtimeSinceStartup+110;while(game.Scares.BasementFlashes==0&&Time.realtimeSinceStartup<end)yield return null;
            Require(game.Scares.BasementFlashes==1&&!game.Case.Terminal,"First basement appearance is a nonlethal flash");
            game.Player.Teleport(new Vector3(0,0,-10.2f));yield return Look(new Vector3(0,1.6f,-6));
            game.Creature.SetHuntOrigin(new Vector3(0,0,-6));game.Case.BeginWarning();yield return new WaitForSeconds(9.1f);
            game.Player.TestInput=new FieldInput{Radio=true,Look=new Vector3(0,1.5f,-6)};yield return null;game.Player.TestInput.Radio=false;yield return Capture("van-hunt-start",game.Creature.Actor.Head.position);
            end=Time.realtimeSinceStartup+20;while(!game.Case.Terminal&&Time.realtimeSinceStartup<end)yield return null;
            Require(game.Player.InVan&&game.Case.Phase==Phase.Dead,"Observed player can be pursued and grabbed inside the van / "+game.Creature.DiagnosticState);
            yield return new WaitForSecondsRealtime(.7f);yield return Capture("splitgrin-van-grab",Vector3.zero);
            Require(game.HasCheckpoint,"Death offers a saved checkpoint");var old=game;previousAnimation+=game.Creature.Actor.AnimatedFrames;game.RetryCheckpoint();
            while(!FindFirstObjectByType<Game>()||FindFirstObjectByType<Game>()==old||FindFirstObjectByType<Game>().Loading)yield return null;
            Bind();Require(game.Case.Equipped&&!game.Case.Terminal,"Checkpoint retry restores kit and a playable investigation");
            yield return Capture("checkpoint-restored",new Vector3(0,1.5f,5));
            yield return RestartAndBind();Require(!game.Case.Equipped&&game.Case.Evidence.Count==0,"Fresh investigation remains available separately");
        }
        IEnumerator Route()
        {
            // Allow the real title/loading presentation to settle before the
            // gameplay measurement begins; initial loading is not gameplay.
            yield return new WaitForSecondsRealtime(1);
            if(UIOnly){yield return UIRoute();yield break;}
            Bind();
            Require(Screen.width==1920&&Screen.height==1080,"Native 1920x1080 output");
            Require(FindFirstObjectByType<WorldLayout>()!=null,"Saved authored house scene loaded");
            yield return Use(Item(InteractKind.Kit));Require(game.Case.Equipped,"Kit collected through interaction");
            Require(game.Player.HeldAssetName=="InductionReader"&&game.Player.HeldVertexCount>1000&&game.Player.FlashlightVertexCount>1000,"Actual original reader and flashlight meshes mounted");
            Require(game.Player.InductionDisplay=="0.0","Physical induction display starts at zero in the van");
            yield return Capture("tool-01-induction",new Vector3(0,1.6f,-4));
            game.Player.TestInput=new FieldInput{SelectTool=2,Torch=true};yield return null;game.Player.TestInput=new FieldInput();yield return null;
            Require(!game.Player.TorchOn&&game.Player.Ultraviolet.enabled,"UV operates independently with flashlight off");
            Require(game.Player.HeldAssetName=="UVLamp"&&game.Player.HeldVertexCount>1000,"Actual UV lamp mesh selected");
            yield return Capture("tool-02-uv",new Vector3(0,1.6f,-4));
            game.Player.TestInput=new FieldInput{SelectTool=3};yield return null;game.Player.TestInput=new FieldInput();yield return null;
            Require(!game.Player.TorchOn&&!game.Player.Ultraviolet.enabled,"Recorder selection does not switch flashlight on");
            Require(game.Player.HeldAssetName=="EVPRecorder"&&game.Player.HeldVertexCount>1000,"Actual EVP recorder mesh selected");
            yield return Capture("tool-03-recorder",new Vector3(0,1.6f,-4));
            game.Player.TestInput=new FieldInput{Radio=true};yield return null;game.Player.TestInput=new FieldInput();yield return null;
            Require(game.Player.HeldAssetName=="FieldRadio"&&game.Player.HeldVertexCount>1000,"Radio call raises original radio mesh");
            yield return Capture("tool-04-radio",new Vector3(0,1.6f,-4));
            yield return new WaitForSeconds(1.4f);Require(game.Player.HeldAssetName=="EVPRecorder","Radio lowers back to selected tool");
            game.Player.TestInput=new FieldInput{SelectTool=1,Torch=true};yield return null;game.Player.TestInput=new FieldInput();yield return null;
            Require(game.Player.TorchOn,"Independent flashlight restores");
            yield return Capture("01-van",new Vector3(1.3f,1.1f,-11.5f));
            if(EnhancementsOnly){yield return EnhancementsRoute();yield break;}
            if(KitOnly)
            {
                yield return Capture("tool-05-rack",Item(InteractKind.Kit).transform.position);
                yield return Walk(new Vector3(-5.3f,0,8.65f));
                yield return RecordWithTool(Evidence.Induction,1,game.World.Induction);
                Require(game.Case.Evidence.Contains(Evidence.Induction),"New reader captures induction through normal equipment code");
                Require(game.Player.InductionDisplay=="5.0","Physical induction display follows the evidence signal");
                yield return Capture("tool-06-induction-evidence",game.World.Induction);
                yield return Walk(new Vector3(-7.15f,3.2f,3.8f));
                yield return RecordWithTool(Evidence.Handprint,2,game.World.Print);
                Require(game.Case.Evidence.Contains(Evidence.Handprint),"New UV lamp captures the upstairs trace");
                yield return Capture("tool-07-uv-evidence",game.World.Print);
                yield return Walk(new Vector3(5.7f,-3.2f,10));
                yield return RecordWithTool(Evidence.Voice,3,game.World.Anchor);
                Require(game.Case.Evidence.Contains(Evidence.Voice),"New recorder captures an EVP response while still");
                yield return Capture("tool-08-recorder-evidence",game.World.Anchor);
                yield return Walk(game.World.Spawn);Require(game.Player.InVan,"Field-kit route returns through stairs and door to the van");
                yield break;
            }
            yield return Walk(new Vector3(5.5f,0,-5.8f));yield return Capture("detail-field-van",new Vector3(0,1.35f,-11.4f));
            yield return Walk(new Vector3(0,0,-7.3f));yield return Capture("detail-exterior",new Vector3(0,4,5));
            yield return Walk(new Vector3(0,0,-3));yield return Capture("02-porch",new Vector3(0,2,1));
            yield return Walk(new Vector3(0,0,2));
            yield return Use(Item(InteractKind.Note,"Foyer letter"));Require(game.NoteBody!=null,"Note opens and pauses");game.Resume();
            yield return Walk(new Vector3(-4.4f,0,5.6f));yield return Capture("art-living",new Vector3(-6.6f,.8f,4));
            yield return Walk(new Vector3(4.8f,0,10));yield return Capture("art-kitchen",new Vector3(7,1.1f,10.2f));
            yield return Capture("detail-kitchen",new Vector3(6.15f,1.2f,13.5f));
            yield return HideCheck("Hall coat cupboard");
            yield return Walk(new Vector3(0,3.2f,10));yield return HideCheck("Upstairs linen cupboard");
            if(!ArtOnly)yield return ReadContext("Family photograph",new Vector3(-5.65f,0,11.8f));
            yield return Walk(new Vector3(-4.3f,3.2f,9));yield return Capture("art-parents",new Vector3(-5.8f,4,10.5f));
            yield return Walk(new Vector3(-6.7f,3.2f,8.55f));yield return Capture("detail-bedside",new Vector3(-7.4f,4.05f,9.6f));
            yield return Walk(new Vector3(5,3.2f,11));yield return Capture("art-bath",new Vector3(6.6f,3.8f,12.5f));
            yield return Capture("detail-vanity",new Vector3(3.3f,4.25f,13.55f));
            yield return Capture("detail-toilet",new Vector3(7.48f,3.65f,9.7f));
            yield return Walk(new Vector3(5,3.2f,5));yield return Capture("art-wardrobe",new Vector3(6.7f,4.3f,3.2f));
            yield return Walk(new Vector3(-4.7f,3.2f,4.8f));yield return Capture("03-child-room",new Vector3(-6.7f,4.3f,3));
            yield return Capture("detail-toy-chest",new Vector3(-5.7f,3.65f,5.9f));
            if(!ArtOnly)yield return ReadContext("Child's drawing",new Vector3(-5.7f,3.2f,5));
            yield return Walk(new Vector3(0,-3.2f,10));yield return HideCheck("Basement steel locker");
            yield return Walk(new Vector3(4.8f,-3.2f,3));yield return Capture("art-laundry",new Vector3(6.8f,-2.7f,2.5f));
            yield return Walk(new Vector3(-5.6f,-3.2f,7.6f));yield return Capture("detail-workbench",new Vector3(-5.6f,-2.15f,8.8f));
            yield return Walk(new Vector3(-5.1f,-3.2f,4.6f));yield return Capture("detail-storage",new Vector3(-6.7f,-2.55f,3.2f));
            yield return Walk(new Vector3(4.8f,-3.2f,9.5f));yield return Capture("04-basement",game.World.Anchor);
            if(ArtOnly)
            {
                yield return Walk(new Vector3(5,-3.2f,8.3f));
                Record("Forced warning for focused apparition-facing capture");game.Case.BeginWarning();
                yield return new WaitForSeconds(9.2f);yield return Capture("art-hunt",new Vector3(5,-1.4f,9.5f));
                Require(game.Creature.Actor.AnimatedFrames>5&&game.Creature.Actor.BoneCount>=20,"Actual skeletal motion updates the apparition");
                yield break;
            }
            yield return Walk(new Vector3(-5.3f,0,8.65f));yield return Capture("05-dining",game.World.Induction);
            yield return RecordWithTool(Evidence.Induction,1,game.World.Induction);
            Require(game.Case.Evidence.Contains(Evidence.Induction),"Induction captured by aim and hold");
            yield return new WaitForSeconds(1.1f);yield return Capture("10-first-apparition",new Vector3(-5.5f,1.65f,12.3f));
            Require(game.Creature.Appearances>0&&!game.Case.Terminal,"Real manifestations occurred without automatic death");
            Require(game.Creature.Actor.Head.position.y-game.Creature.transform.position.y>1.0f&&game.Creature.Actor.Head.position.y-game.Creature.transform.position.y<2.2f,"Apparition rig retains its intended scale while upright or hunched");
            yield return Walk(new Vector3(-7.15f,3.2f,3.8f));
            yield return RecordWithTool(Evidence.Handprint,2,game.World.Print);
            Require(game.Case.Evidence.Contains(Evidence.Handprint),"UV trace captured by aim and hold");
            var firstHide=Item(InteractKind.Hide,"Upstairs linen cupboard");yield return Walk(firstHide.ExitPoint);
            float deadline=Time.realtimeSinceStartup+110;
            while(game.Case.Phase!=Phase.Warning){if(Time.realtimeSinceStartup>deadline)throw new Exception("Natural warning did not trigger");yield return null;}
            yield return Capture("06-warning",new Vector3(0,1.7f,8));
            yield return Use(firstHide);if(game.Player.TorchOn){game.Player.TestInput=new FieldInput{Torch=true};yield return null;game.Player.TestInput=new FieldInput();}
            while(!game.Case.Survived&&!game.Case.Terminal)yield return null;Require(!game.Case.Terminal,"Natural hunt survived by breaking sight and hiding");
            game.Player.TestInput=new FieldInput{Use=true};yield return null;game.Player.TestInput=new FieldInput{Torch=true};yield return null;game.Player.TestInput=new FieldInput();
            Require(game.ManifestationCount>=1&&game.Scares.BasementFlashes<=1,"Sparse natural activity occurred; basement surprise never repeats");
            yield return Walk(new Vector3(5.7f,-3.2f,10));
            yield return RecordWithTool(Evidence.Voice,3,game.World.Anchor);
            Require(game.Case.Evidence.Contains(Evidence.Voice),"EVP response captured while still");
            Require(!game.Case.MysteryReady,"Instrument records alone leave the family mystery incomplete");
            yield return ReadContext("Basement diary",new Vector3(-5.6f,-3.2f,7.6f));
            Require(game.Case.MysteryReady,"Evidence and three family clues support reconstruction");
            Require(!game.Case.Conclude(AnchorKind.Doll,HauntingPattern.DomesticLoop,ClosureMethod.LetSongEnd),"Wrong anchor remains recoverable");
            Require(!game.Case.Conclude(AnchorKind.MusicBox,HauntingPattern.DomesticLoop,ClosureMethod.MoveObject),"Unsupported closure remains recoverable");
            Require(game.Case.Conclude(AnchorKind.MusicBox,HauntingPattern.DomesticLoop,ClosureMethod.LetSongEnd),"Supported reconstruction unlocks containment");
            yield return Walk(new Vector3(6.7f,-3.2f,7.9f));yield return Use(Item(InteractKind.Breaker));Require(game.Case.BreakerOn,"Breaker restored through interaction");
            yield return Walk(new Vector3(5.7f,-3.2f,10));yield return Look(game.World.Anchor);game.Player.TestInput.UseHeld=true;yield return new WaitForSeconds(12.5f);game.Player.TestInput.UseHeld=false;
            Require(game.Case.Phase==Phase.Return,"Anchor sealed through aimed hold");
            yield return Walk(game.World.Spawn);yield return Use(Item(InteractKind.Van));Require(game.Case.Phase==Phase.Won,"Return terminal closes case");
            yield return Capture("07-ending",game.Player.View.transform.position+Vector3.forward);
            yield return RestartAndBind();Require(game.Case.Evidence.Count==0&&!game.Case.Equipped,"Retry resets evidence and kit");
            yield return Use(Item(InteractKind.Kit));
            var quietHide=Item(InteractKind.Hide,"Upstairs linen cupboard");
            yield return Walk(quietHide.ExitPoint);yield return Use(quietHide);
            game.Player.TestInput=new FieldInput{Torch=true};yield return null;game.Player.TestInput=new FieldInput();yield return null;
            Require(game.Player.Hidden&&!game.Player.TorchOn,"Quiet hiding scenario has torch off");
            Record("Forced warning for separate quiet-hiding scenario; first case used natural progression");game.Case.BeginWarning();
            yield return new WaitForSeconds(9.2f);
            Require(game.Case.Phase==Phase.Hunt&&!game.Creature.CanSeePlayer,"Concealed unobserved player is not visible");
            yield return new WaitForSeconds(7.5f);
            Require(!game.Creature.Memory.Knows,"Creature has no hidden-position observation without a signal");
            while(!game.Case.Survived&&!game.Case.Terminal)yield return null;
            Require(game.Case.Survived&&!game.Case.Terminal,"Quiet hiding survives an entire hunt");
            game.Player.TestInput=new FieldInput{Use=true};yield return null;game.Player.TestInput=new FieldInput();yield return null;
            Require(!game.Player.Hidden,"Can leave cupboard after hunt");
            yield return RestartAndBind();
            yield return Use(Item(InteractKind.Kit));yield return Walk(new Vector3(5,-3.2f,8.3f));
            Record("Forced warning for separate death/retry test; first case used natural progression");game.Case.BeginWarning();
            yield return new WaitForSeconds(9.2f);game.Player.TestInput=new FieldInput{Radio=true,Look=new Vector3(5,-1.4f,9.5f)};yield return null;game.Player.TestInput.Radio=false;
            yield return Capture("08-hunt",new Vector3(5,-1.4f,9.5f));
            deadline=Time.realtimeSinceStartup+30;while(!game.Case.Terminal){if(Time.realtimeSinceStartup>deadline)throw new Exception("Exposed player not caught");yield return null;}
            Require(game.Case.Phase==Phase.Dead,"Observed exposed player caught");
            yield return new WaitForSecondsRealtime(.65f);
            Require(game.Creature.Actor.Animator.GetCurrentAnimatorStateInfo(0).IsName("Reach"),"Capture animation advances while gameplay time is stopped");
            yield return Capture("08-capture-reach",Vector3.zero);
            yield return new WaitForSecondsRealtime(2.3f);yield return Capture("09-death",Vector3.zero);
            yield return RestartAndBind();Require(game.Case.Phase==Phase.Briefing&&!game.Case.Equipped,"Death retry creates fresh playable case");
            Require(previousGenerated>20&&previousMusicMask==7,"Generated effects and all three music states executed");
            Require(previousBreathing>50&&previousDucked>100,"Hiding breath and speech ducking executed");
            Require(audioNonzero>5000&&audioPeak>.001f&&audioPeak<.71f,"Native output contains audio below the -3 dBFS sample-peak ceiling");
        }
    }
}
