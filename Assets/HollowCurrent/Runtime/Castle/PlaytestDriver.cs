using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace HollowCurrent
{
    // Opt-in automated playtest of the castle loop in the real player:
    //   -hc-playtest <outdir> escape|kill|fear
    // fear: the wave F proof run (FEAR_PLAN). It steers the real rules engine through Investigation.Dice and
    // AddDread (logged as driver-forced) to get a false warning, a no-tell hunt, a told hunt it hides in while
    // watched, the gatehouse visit and the hostile return, and calls the director's real throw/beat methods.
    // Drives the investigator through Investigator.TestInput (the same path as keyboard and
    // pad input: look, use, hold, fire, tool select, torch), teleporting between stops instead
    // of pathing. Logs every phase as HC_PLAY and saves a screenshot per phase into <outdir>.
    // Driver-only safety: Case.Invulnerable counts a grab instead of ending the run; whether
    // it was needed is logged (blockedDeaths). Started from Bellwether, it switches to Castle.
    public sealed class PlaytestDriver : MonoBehaviour
    {
        public static string OutDir {get;private set;}
        public static string Mode {get;private set;}
        // Kill run: seconds without strikes after he becomes hittable (-hc-playtest-window, default 3).
        public static float StrikeWindow=3f;
        public static bool Requested
        {
            get
            {
                if(OutDir!=null)return true;
                var args=Environment.GetCommandLineArgs();
                for(int i=0;i<args.Length-1;i++)if(args[i]=="-hc-playtest-window"&&float.TryParse(args[i+1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var w))StrikeWindow=w;
                for(int i=0;i<args.Length-1;i++)if(args[i]=="-hc-playtest"){OutDir=args[i+1];Mode=i+2<args.Length?args[i+2]:"escape";return true;}
                return false;
            }
        }
        Game game;FieldInput input;float start;int shot;
        // fear mode: counts of HC_ log lines by prefix (Application.logMessageReceived), summarised at the end.
        readonly Dictionary<string,int> tags=new Dictionary<string,int>();
        static readonly string[] Tags={"HC_LIE","HC_DUCK","HC_SCARE_CASTLE silence","HC_COUNT_WITNESSED","HC_COUNT_NEARMISS open=True","HC_COUNT_RETURN_SWOOP","HC_COUNT_DIVE_LAST","HC_RECOVERY","HC_NAIL_HIDES","HC_COUNT_PERCH flight observed=True","HC_THROW_SKIP","HC_MISSING_ART","HC_THROW ","HC_THROW poltergeist","HC_SCARE_CASTLE","HC_COUNT_PERCH moved","HC_COUNT_PERCH landed","HC_COUNT_NEARMISS begin","HC_COUNT_NEARMISS hold","HC_WARNING","HC_FALSE_WARNING","HC_HUNT_START","HC_HUNT_SPAWN hostileReturn=True","HC_COUNT_GATEHOUSE landed","HC_COUNT_VOICE","HC_TORCH_FAIL","HC_COUNT_DREAD","HC_COUNT_GRAB","HC_COUNT_LEARNED","HC_ESCALATE","HC_RARE_LINE","HC_DOOR_HELD"};
        void OnLog(string message,string stack,LogType type){foreach(var t in Tags)if(message.StartsWith(t,StringComparison.Ordinal)){tags.TryGetValue(t,out int n);tags[t]=n+1;}}
        int Tag(string t)=>tags.TryGetValue(t,out int n)?n:0;
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
        void OnApplicationQuit(){Screen.SetResolution(Game.WindowWidth,Game.WindowHeight,FullScreenMode.Windowed);}
        readonly List<string> blockers=new List<string>();
        public void Initialize(Game owner)
        {
            game=owner;Application.runInBackground=true;
            // Windowed 1920x1080 (owner rule: never the whole screen); quitting leaves the window prefs windowed.
            Screen.SetResolution(Game.WindowWidth,Game.WindowHeight,FullScreenMode.Windowed);
            Directory.CreateDirectory(OutDir);StartCoroutine(Run());
        }
        void Log(string phase,string detail="")
        {
            var c=game.Case;var castle=game.Castle;
            Debug.Log("HC_PLAY phase="+phase+" t="+(Time.realtimeSinceStartup-start).ToString("F1")+" case="+c.Phase+" dread="+c.Dread.ToString("F1")+" hunts="+c.Hunts+" evidence="+c.Evidence.Count+" notes="+c.Notes.Count+" survived="+c.Survived+
                (castle?" relics="+castle.Forge.RelicCount+" sword="+castle.Forge.HasSword+" hits="+castle.Fight.Hits:"")+" blockedDeaths="+c.BlockedDeaths+(detail.Length>0?" "+detail:""));
        }
        void Blocker(string what){blockers.Add(what);Debug.Log("HC_PLAY_BLOCKER "+what);}
        IEnumerator Shot(string phase)
        {
            yield return new WaitForEndOfFrame();
            var tex=ScreenCapture.CaptureScreenshotAsTexture();shot++;
            File.WriteAllBytes(Path.Combine(OutDir,shot.ToString("00")+"_"+phase+".png"),tex.EncodeToPNG());Destroy(tex);
        }
        IEnumerator Step(string phase,string detail=""){Log(phase,detail);yield return Shot(phase);}
        IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        IEnumerator Wait(float seconds){float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end)yield return null;}
        void Reset(){input.Use=input.UseHeld=input.Fire=input.FireHeld=input.Torch=input.Radio=input.Sprint=input.Crouch=false;input.SelectTool=0;input.Move=Vector2.zero;}
        // One frame only: a two-frame press re-uses the same E on the next frame (entering a
        // cupboard and leaving it again).
        IEnumerator Press(Action<FieldInput> set){set(input);yield return null;Reset();yield return null;}
        IEnumerator SelectTool(int tool){if(game.Player.Tool==tool)yield break;yield return Press(i=>i.SelectTool=tool);}
        IEnumerator Torch(bool on){if(game.Player.TorchOn!=on)yield return Press(i=>i.Torch=true);}
        // Stand on the nearest walkable spot ~distance from the target (same floor), facing it.
        bool StandNear(Vector3 target,float distance,float floorY)
        {
            var best=Vector3.zero;bool found=false;float score=float.MaxValue;
            for(int k=0;k<16;k++)
            {
                float a=k*Mathf.PI/8;var probe=new Vector3(target.x+Mathf.Cos(a)*distance,floorY+.1f,target.z+Mathf.Sin(a)*distance);
                if(!NavMesh.SamplePosition(probe,out var hit,.8f,NavMesh.AllAreas)||Mathf.Abs(hit.position.y-floorY)>.5f)continue;
                var eye=hit.position+Vector3.up*1.64f;var d=target-eye;
                bool clear=!Physics.Raycast(eye,d.normalized,Mathf.Max(0,d.magnitude-.15f),~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore);
                float s=Mathf.Abs(Vector3.Distance(hit.position,new Vector3(target.x,floorY,target.z))-distance)+(clear?0:10);
                if(s<score){score=s;best=hit.position;found=true;}
            }
            if(found){game.Player.Teleport(best+Vector3.up*.05f);input.Look=target;}
            return found;
        }
        static float Floor(float y)=>World.FloorBelow(y);
        Interactable Find(InteractKind kind,string label=null,int relic=-1)
        {
            foreach(var item in game.World.Root.GetComponentsInChildren<Interactable>(true))
                if(item.Kind==kind&&(label==null||item.Label==label)&&(relic<0||item.RelicIndex==relic))return item;
            return null;
        }
        // Focus the item through the investigator's own interaction ray, then press E.
        IEnumerator Use(Interactable item,string what,float distance=1.4f,bool hold=false,float holdSeconds=0)
        {
            if(!item){Blocker(what+": interactable missing");yield break;}
            var col=item.GetComponent<Collider>();var point=col?col.bounds.center:item.transform.position;
            if(!StandNear(point,distance,Floor(point.y-.2f)))Blocker(what+": no walkable stand point near "+point);
            yield return Frames(4);
            if(game.Player.Focus!=item)
            {
                // Try the other side, then fall back to a direct Use (logged as a blocker).
                StandNear(point,distance*.7f,Floor(point.y-.2f));yield return Frames(4);
            }
            bool focused=game.Player.Focus==item;
            if(!focused)Blocker(what+": not focusable by the interaction ray (focus="+(game.Player.Focus?game.Player.Focus.Label:"none")+")");
            if(hold){input.UseHeld=true;float end=Time.realtimeSinceStartup+holdSeconds;while(Time.realtimeSinceStartup<end){yield return null;}Reset();yield break;}
            if(focused)yield return Press(i=>{i.Use=true;i.UseHeld=true;});else item.Use(game);
            yield return Frames(2);
            if(game.NoteBody!=null){yield return Shot("note_"+item.Label.Replace(' ','_').Replace(':','_'));game.NoteBody=null;game.ApplyFocus();}
        }
        IEnumerator Record(Evidence evidence,Vector3 point,int tool,string what)
        {
            yield return SelectTool(tool);
            if(!StandNear(point,tool==3?2f:2.2f,Floor(point.y-.3f)))Blocker(what+": no stand point");
            yield return Frames(6);
            if(tool==3){yield return Press(i=>i.Fire=true);yield return Wait(4.6f);}
            else{input.FireHeld=true;float end=Time.realtimeSinceStartup+4f;while(Time.realtimeSinceStartup<end&&!game.Case.Evidence.Contains(evidence))yield return null;Reset();}
            if(!game.Case.Evidence.Contains(evidence))Blocker(what+": evidence not recorded (instrument: "+game.Player.Instrument+")");
            yield return Step("evidence_"+evidence,game.Player.Instrument);
        }
        IEnumerator SurviveHunt()
        {
            // Hide (torch off) in a cupboard away from the crypt during the warning.
            Interactable hide=null;float far=0;
            foreach(var h in game.World.HidingPlaces){float d=Vector3.Distance(h.HidePoint,game.World.HunterSpawn);if(d>far){far=d;hide=h;}}
            yield return Torch(false);
            if(hide)yield return Use(hide,"hiding place "+hide.Label);
            if(!game.Player.Hidden)Blocker("could not hide");
            yield return Step("hidden",hide?hide.Label:"none");
            while(game.Case.Phase==Phase.Warning)yield return null;
            yield return Step("hunt_started",game.Creature.DiagnosticState);
            int blocked=game.Case.BlockedDeaths;
            while(game.Case.Phase==Phase.Hunt)yield return null;
            yield return Step("hunt_survived",game.Case.BlockedDeaths>blocked?"grabs blocked by driver invulnerability: "+(game.Case.BlockedDeaths-blocked):"no grab");
            if(game.Player.Hidden)yield return Press(i=>i.Use=true);
            yield return Torch(true);
        }
        IEnumerator Run()
        {
            start=Time.realtimeSinceStartup;
            while(game.Loading)yield return null;yield return Wait(1);
            if(game.Map!=MapId.Castle)
            {
                Debug.Log("HC_PLAY phase=switch_to_castle from="+game.Map);
                game.SelectMap(MapId.Castle);game.BeginInvestigation();yield break; // the castle scene's Game starts a new driver
            }
            if(!game.Started)game.StartCase();
            game.Case.Invulnerable=true;
            input=new FieldInput();game.Player.TestInput=input;
            yield return Step("begin_"+Mode,"rules="+game.Case.Rules.Name);
            Application.logMessageReceived+=OnLog;
            if(Mode=="fear"){yield return Fear();yield break;}
            if(Mode=="siege"){yield return Siege();yield break;}
            if(Mode=="lowdread"){yield return LowDread();yield break;}
            if(Mode=="natural"){yield return Natural();yield break;}
            if(Mode=="death"){yield return Death();yield break;}
            // Field kit from the lantern cart.
            yield return Use(Find(InteractKind.Kit),"field kit");
            if(!game.Case.Equipped)Blocker("kit not equipped");
            yield return Step("kit_taken");
            // Arrival letter (gatehouse).
            yield return Use(Find(InteractKind.Note,game.Book.ArrivalNoteTitle),"arrival letter");
            // Enter the castle proper (Survey).
            game.Player.Teleport(new Vector3(0,.1f,10));yield return Frames(4);
            yield return Step("entered",game.Case.Phase.ToString());
            yield return Record(Evidence.Induction,game.World.Induction,1,"compass");
            yield return Record(Evidence.Handprint,game.World.Print,2,"grave-ash lantern");
            yield return Record(Evidence.Voice,game.World.Voice,3,"ear-trumpet");
            foreach(var title in game.Book.NoteTitles)yield return Use(Find(InteractKind.Note,title),"ledger "+title);
            yield return Step("ledger_read",string.Join(",",game.Case.Notes));
            if(Mode=="kill")yield return Relics();
            // Ring the chapel bell: the bargain hears you; survive the hunt.
            yield return Use(Find(InteractKind.Anchor),"chapel bell");
            yield return Step("bell_rung",game.Case.Phase.ToString());
            if(game.Case.Phase!=Phase.Warning)Blocker("bell did not start the warning (phase "+game.Case.Phase+")");
            if(Mode=="kill"){yield return Fight();yield return Finish();yield break;}
            yield return SurviveHunt();
            // Close the ledger: the journal reconstruction (same call as the journal's FILE RECONSTRUCTION).
            game.Journal=true;game.ApplyFocus();yield return Shot("journal");
            bool solved=game.Case.Conclude(AnchorKind.MusicBox,HauntingPattern.DomesticLoop,ClosureMethod.LetSongEnd);game.Journal=false;game.ApplyFocus();
            yield return Step("ledger_closed","conclude="+solved);
            if(!solved)Blocker("reconstruction rejected (mysteryReady="+game.Case.MysteryReady+")");
            // Relight the brazier, then hold the bell rope for the seal.
            yield return Use(Find(InteractKind.Breaker),"chapel brazier");
            yield return Step("brazier_lit","breaker="+game.Case.BreakerOn);
            yield return Use(Find(InteractKind.Anchor),"chapel bell (seal)",1.4f,true,13.5f);
            yield return Step("bell_sealed",game.Case.Phase.ToString());
            if(game.Case.Phase!=Phase.Return)Blocker("seal did not complete (progress "+game.Case.SealProgress.ToString("F2")+")");
            var bridge=game.World.Castle.Drawbridge;
            {float until=Time.realtimeSinceStartup+12;yield return Wait(1);while(bridge&&bridge.Moving&&Time.realtimeSinceStartup<until)yield return null;}
            yield return Step("drawbridge_lowered","lowered="+(bridge&&bridge.Lowered)+" moving="+(bridge&&bridge.Moving));
            // Walk out over the bridge to the far bank.
            game.Player.Teleport(new Vector3(0,.1f,1.2f));input.Look=new Vector3(0,1.6f,-20);yield return Frames(3);
            input.Move=new Vector2(0,1);float walk=Time.realtimeSinceStartup+12;
            while(Time.realtimeSinceStartup<walk&&game.Case.Phase!=Phase.Won)yield return null;
            Reset();
            if(game.Case.Phase!=Phase.Won)
            {
                Blocker("walking over the bridge did not reach the far-bank trigger (stopped at "+game.Player.transform.position+")");
                game.Player.Teleport(game.World.Castle.DrawbridgeTrigger.center-Vector3.up*1.0f);yield return Frames(5);
            }
            yield return Finish();
        }
        // ---- fear mode ----------------------------------------------------------------------------------
        readonly Queue<float> dice=new Queue<float>();
        IEnumerator WaitFor(Func<bool> done,float seconds){float end=Time.realtimeSinceStartup+seconds;while(!done()&&Time.realtimeSinceStartup<end)yield return null;}
        void ForceDread(float target,string why){var c=game.Case;float before=c.Dread;c.AddDread(target-c.Dread);Debug.Log("HC_PLAY_FORCED dread "+before.ToString("F1")+"->"+c.Dread.ToString("F1")+" ("+why+")");}
        // Dice steps: Investigation.ForceDread sets Dread and restarts the dice schedule, so the queue always
        // begins with an interval roll: [interval, hunt roll, tell roll, false-alarm roll (dice only),
        // warning length (told/false), hunt length (when the hunt starts)]. Holds for any RunDreadMin (30-50)
        // and any Dread at Disturbance entry (RulesTests WaveH "driver queues line up").
        void ForceDice(string why,params float[] queue)
        {
            var c=game.Case;float before=c.Dread;c.ForceDread(Mathf.Max(55,c.RunDreadMin+5));foreach(var v in queue)dice.Enqueue(v);
            Debug.Log("HC_PLAY_FORCED dread "+before.ToString("F1")+"->"+c.Dread.ToString("F1")+" boundary="+c.RunDreadMin.ToString("F1")+" queue="+string.Join(",",queue)+" ("+why+")");
        }
        Interactable FarHide()
        {
            Interactable hide=null;float far=0;
            foreach(var h in game.World.HidingPlaces){if(!h||!h.gameObject.activeInHierarchy)continue;float d=Vector3.Distance(h.HidePoint,game.World.HunterSpawn);if(d>far){far=d;hide=h;}}
            return hide;
        }
        IEnumerator Fear()
        {
            var c=game.Case;
            // Driver-forced dice: queued values first, then 0.99 (no hunt, longest interval), so nothing random fires.
            c.Dice=()=>dice.Count>0?dice.Dequeue():.99f;
            yield return Use(Find(InteractKind.Kit),"field kit");
            yield return Use(Find(InteractKind.Note,game.Book.ArrivalNoteTitle),"arrival letter");
            game.Player.Teleport(new Vector3(0,.1f,10));yield return Frames(4);
            yield return Step("entered",game.Case.Phase.ToString());
            yield return Record(Evidence.Induction,game.World.Induction,1,"compass");
            // 1. Dread moves on its own: 8 s inside.
            float d0=c.Dread;yield return Wait(8);yield return Step("dread_natural","from="+d0.ToString("F2")+" to="+c.Dread.ToString("F2")+" lit="+(c.Dread-d0<.8f));
            // 2. Great hall frame time (standing at the rune ring, looking down the hall at the table).
            var hall=game.World.Castle.RuneRingCenter;game.Player.Teleport(new Vector3(hall.x,0.05f,hall.z-3.5f));input.Look=new Vector3(hall.x,1.2f,hall.z+4);yield return Wait(1);
            var times=new List<float>();float end=Time.realtimeSinceStartup+10;while(Time.realtimeSinceStartup<end){yield return null;times.Add(Time.unscaledDeltaTime*1000);}
            times.Sort();float sum=0;foreach(var t in times)sum+=t;
            Debug.Log("HC_PLAY_FRAMES great_hall frames="+times.Count+" mean_ms="+(sum/times.Count).ToString("F2")+" p50_ms="+times[times.Count/2].ToString("F2")+" p95_ms="+times[(int)(times.Count*.95f)].ToString("F2")+" p99_ms="+times[(int)(times.Count*.99f)].ToString("F2")+" worst_ms="+times[times.Count-1].ToString("F2")+" over_16.7="+times.FindAll(t=>t>16.7f).Count+" screen="+Screen.width+"x"+Screen.height);
            yield return Step("great_hall_frames");
            // 3. Throwables: the director's real throw, then the poltergeist, facing the hall table.
            // Try the ground-floor throwables nearest the rune ring in turn: stand 2.2 m away facing it, call the director's
            // real ThrowNearbyProp; log why a candidate fails (distance, view angle, what the sight line hits).
            var candidates=new List<Throwable>();foreach(var th in Throwable.All)if(th&&th.transform.position.y>-.5f&&th.transform.position.y<2)candidates.Add(th);
            candidates.Sort((a,b)=>Vector3.Distance(a.transform.position,hall).CompareTo(Vector3.Distance(b.transform.position,hall)));
            bool threw=false;
            for(int k=0;k<Mathf.Min(6,candidates.Count)&&!threw;k++)
            {
                var t=candidates[k].transform;var rs=t.GetComponentsInChildren<Renderer>();var centre=rs.Length>0?rs[0].bounds.center:t.position;
                StandNear(centre,2.2f,0);input.Look=centre;yield return Frames(6);
                var eye=game.Player.View.transform;var delta=centre-eye.position;
                string hitName=Physics.Linecast(eye.position,centre,out var hit,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore)?hit.collider.name+"@"+hit.distance.ToString("F2")+"/"+delta.magnitude.ToString("F2"):"clear";
                Debug.Log("HC_PLAY_THROW_PROBE "+t.name+" at="+t.position+" d="+delta.magnitude.ToString("F2")+" angle="+Vector3.Angle(eye.forward,delta).ToString("F0")+" renderers="+rs.Length+" body="+(t.GetComponent<Rigidbody>()!=null)+" line="+hitName+" sight="+(Throwable.SightBlocked(eye.position,centre,out var sightBlocker)?sightBlocker:"clear")+" batched="+candidates[k].StaticBatched);
                threw=game.Scares.ThrowNearbyProp();
            }
            yield return Wait(1.2f);yield return Step("throw","thrown="+threw+" prop="+(game.Scares.LastThrownProp?game.Scares.LastThrownProp.name:"none"));
            int flung=game.CastleDread?game.CastleDread.Poltergeist():-1;yield return Wait(1.2f);yield return Step("poltergeist","flung="+flung);
            // 4. Castle dread beats through ScareDirector.Dread (HC_SCARE_CASTLE).
            for(int i=0;i<5;i++){game.Scares.Dread();yield return Wait(2.2f);}
            yield return Step("castle_beats","scareCastle="+Tag("HC_SCARE_CASTLE"));
            // 5. False warning: real dice path (interval 0 -> 2 s, hunt roll 0, no-tell roll 0.9, false-alarm roll 0, warning length 0.5 -> 9 s).
            ForceDice("false warning",0f,0f,.9f,0f,.5f);
            yield return WaitFor(()=>c.Phase==Phase.Warning,20);
            yield return Step("warning","false="+c.FalseWarning+" length="+c.WarningLength.ToString("F1"));
            if(!c.FalseWarning)Blocker("forced dice did not give a false warning (phase "+c.Phase+")");
            int huntsBefore=c.Hunts;
            yield return WaitFor(()=>c.Phase!=Phase.Warning,15);
            yield return Step("false_warning_over","hunts "+huntsBefore+"->"+c.Hunts);
            if(c.Phase==Phase.Hunt)yield return WaitFor(()=>c.Phase!=Phase.Hunt,60); // hunts are 25-50 s (wave G)
            // 6. No-tell hunt while hidden with the torch off (near-miss chance 1 in 3).
            var hide=FarHide();yield return Torch(false);
            if(hide)yield return Use(hide,"hiding place "+hide.Label);
            if(!game.Player.Hidden)Blocker("could not hide before the no-tell hunt");
            yield return Step("hidden",hide?hide.name:"none");
            yield return WaitFor(()=>c.Phase==Phase.Disturbance,60);
            ForceDice("no-tell hunt",0f,0f,0f,.5f); // interval 0, hunt roll 0, no-tell roll 0, hunt length 0.5 -> 38 s
            StartCoroutine(ChannelWatch("no_tell"));
            yield return WaitFor(()=>c.Phase==Phase.Hunt,20);
            yield return Step("no_tell_hunt","noTell="+c.NoTellHunt+" warningSeen="+(Tag("HC_WARNING")>1));
            if(!c.NoTellHunt)Blocker("forced dice did not give a no-tell hunt");
            yield return TrackHunt("no_tell");
            if(game.Player.Hidden)yield return Press(i=>i.Use=true);
            yield return Torch(true);
            // 7. Told hunt; he sees her, then she hides in front of him (witnessed: he opens it, the driver blocks the grab).
            yield return WaitFor(()=>c.Phase==Phase.Disturbance,50); // recoveries are 10-40 s
            ForceDice("told hunt",0f,0f,.9f,.9f,.5f,.5f); // interval 0, hunt 0, no-tell 0.9, false alarm 0.9, warning 9 s, hunt 38 s
            yield return WaitFor(()=>c.Phase==Phase.Warning||c.Phase==Phase.Hunt,20);
            yield return Step("told_warning","false="+c.FalseWarning);
            if(c.Phase!=Phase.Warning&&c.Phase!=Phase.Hunt)Blocker("forced dice did not give the told hunt");
            yield return WaitFor(()=>c.Phase==Phase.Hunt,12);
            yield return Witnessed();
            yield return TrackHunt("told");
            if(game.Player.Hidden)yield return Press(i=>i.Use=true);
            yield return Torch(true);
            yield return WaitFor(()=>c.Phase==Phase.Disturbance||c.Phase==Phase.Recovery,5);
            // 7b. Lies and active silence: Dread above the Lies threshold (70), then the director's real dread beats.
            yield return WaitFor(()=>c.Phase==Phase.Disturbance,50);
            if(game.Player.Hidden)yield return Press(i=>i.Use=true);
            var hall2=game.World.Castle.RuneRingCenter;game.Player.Teleport(new Vector3(hall2.x,.05f,hall2.z-3.5f));yield return SelectTool(1);
            ForceDread(78,"Lies threshold for the lie beats");
            for(int i=0;i<8&&c.Phase==Phase.Disturbance;i++){game.Scares.Dread();yield return Wait(3f);if(c.Dread<72)ForceDread(78,"keep Lies");}
            yield return Step("lies","HC_LIE="+Tag("HC_LIE")+" silence="+Tag("HC_SCARE_CASTLE silence")+" duck="+Tag("HC_DUCK"));
            // 8. Battlement visibility from the courtyard, then the gatehouse visit (40 s inside after a survived hunt).
            Battlement();
            game.Player.Teleport(game.World.Spawn);input.Look=game.World.Castle.GatehouseBattlement+Vector3.up*1.2f;yield return Frames(4);
            yield return Step("gatehouse_wait","inVan="+game.Player.InVan+" survived="+c.Survived);
            yield return WaitFor(()=>Tag("HC_COUNT_GATEHOUSE landed")>0,60);
            input.Look=game.Creature.HeadPosition;yield return Wait(1.5f);
            yield return Step("gatehouse_visit","landed="+Tag("HC_COUNT_GATEHOUSE landed")+" count="+game.Creature.transform.position);
            yield return Wait(4);
            // 9. The rest of the case, then the hostile return over the bridge.
            yield return Record(Evidence.Handprint,game.World.Print,2,"grave-ash lantern");
            yield return Record(Evidence.Voice,game.World.Voice,3,"ear-trumpet");
            foreach(var title in game.Book.NoteTitles)yield return Use(Find(InteractKind.Note,title),"ledger "+title);
            if(c.Phase==Phase.Hunt||c.Phase==Phase.Warning||c.Phase==Phase.Recovery)yield return WaitFor(()=>c.Phase==Phase.Disturbance,100); // a hunt (<=50 s) plus a recovery (<=40 s)
            game.Journal=true;game.ApplyFocus();yield return Shot("journal_pulse");
            bool solved=c.Conclude(AnchorKind.MusicBox,HauntingPattern.DomesticLoop,ClosureMethod.LetSongEnd);game.Journal=false;game.ApplyFocus();
            yield return Step("ledger_closed","conclude="+solved);
            if(!solved)Blocker("reconstruction rejected (mysteryReady="+c.MysteryReady+")");
            yield return Use(Find(InteractKind.Breaker),"chapel brazier");
            yield return Use(Find(InteractKind.Anchor),"chapel bell (seal)",1.4f,true,13.5f);
            yield return Step("bell_sealed",c.Phase+" huntActive="+c.HuntActive);
            if(c.Phase!=Phase.Return||!c.HuntActive)Blocker("seal did not start the hostile return");
            yield return HostileBridge();
            yield return Finish();
        }
        // Per-frame channel log around a forced no-tell hunt: everything the player can read from the HUD, radio-free mixer and
        // the Count's lamps, compared with the frame before the hunt starts, for the first 6 s (critic r10 single biggest gap).
        IEnumerator ChannelWatch(string label)
        {
            var c=game.Case;var ui=game.GetComponent<Interface>();var castle=game.Castle;
            string Snap()=>"goal="+(ui?ui.HudGoal():c.Goal)+"|instrument="+game.Player.Instrument+"|huntMusic="+(game.Sound.HuntMusicVolume>.01f)+"|safeMusic="+(game.Sound.SafeMusicVolume>.01f)+"|houseMusic="+(game.Sound.HouseMusicVolume>.01f)+"|breath="+(game.Sound.BreathVolume>.01f)+"|room="+(game.Sound.RoomVolume>.01f)+"|breaker="+c.BreakerOn+"|danger="+(c.Phase==Phase.Warning||game.Scares.Interference);
            int voice=0,rim=0,smell=0;
            void Count(string m,string st,LogType t){if(m.StartsWith("HC_COUNT_VOICE",StringComparison.Ordinal))voice++;if(m.StartsWith("HC_COUNT_RIM",StringComparison.Ordinal))rim++;if(m.Contains("cc_hunt_smell"))smell++;}
            string before=Snap();float heartBefore=game.Sound.HeartbeatVolume,wingBefore=castle?castle.WingVolume:0;
            float wait=Time.realtimeSinceStartup+25;
            while(c.Phase!=Phase.Hunt&&Time.realtimeSinceStartup<wait){before=Snap();heartBefore=game.Sound.HeartbeatVolume;wingBefore=castle?castle.WingVolume:0;yield return null;}
            if(c.Phase!=Phase.Hunt){Debug.Log("HC_PLAY_CHANNELS "+label+" no hunt started");yield break;}
            Application.logMessageReceived+=Count;
            int frames=0,changed=0;string firstChange=null;float heartMax=0,wingMax=0;float end=Time.realtimeSinceStartup+6;
            while(Time.realtimeSinceStartup<end)
            {
                frames++;string now=Snap();if(now!=before){changed++;if(firstChange==null)firstChange=now;}
                heartMax=Mathf.Max(heartMax,Mathf.Abs(game.Sound.HeartbeatVolume-heartBefore));wingMax=Mathf.Max(wingMax,Mathf.Abs((castle?castle.WingVolume:0)-wingBefore));
                yield return null;
            }
            Application.logMessageReceived-=Count;
            Debug.Log("HC_PLAY_CHANNELS "+label+" noTell="+c.NoTellHunt+" frames="+frames+" changedFrames="+changed+" heartbeatMaxDelta="+heartMax.ToString("F3")+" wingMaxDelta="+wingMax.ToString("F3")+" voiceLines="+voice+" rimLines="+rim+" smellLines="+smell+" before=["+before+"]"+(firstChange!=null?" firstChange=["+firstChange+"]":""));
        }
        // Common opening for the extra modes: kit, letter, into the castle, compass record (Disturbance).
        IEnumerator Opening(bool second)
        {
            yield return Use(Find(InteractKind.Kit),"field kit");
            yield return Use(Find(InteractKind.Note,game.Book.ArrivalNoteTitle),"arrival letter");
            game.Player.Teleport(new Vector3(0,.1f,10));yield return Frames(4);
            yield return Record(Evidence.Induction,game.World.Induction,1,"compass");
            if(second)yield return Record(Evidence.Handprint,game.World.Print,2,"grave-ash lantern");
            yield return Step("opened","runDreadMin="+game.Case.RunDreadMin.ToString("F1"));
        }
        // siege: real dice, Dread held at 82+ by the driver, hidden with the torch off in the far hide. Proves near-miss (and
        // its open at Dread >= 80) and a Recovery cut short. Ends after an open and a Recovery cut, or 12 minutes.
        IEnumerator Siege()
        {
            var c=game.Case;yield return Opening(false);
            var hide=FarHide();yield return Torch(false);if(hide)yield return Use(hide,"hiding place "+hide.Label);
            yield return Step("siege_hidden",hide?hide.name:"none");
            float end=Time.realtimeSinceStartup+720;int recoveryCuts=0;Phase last=c.Phase;
            while(Time.realtimeSinceStartup<end&&!(Tag("HC_COUNT_NEARMISS open=True")>0&&recoveryCuts>0))
            {
                if(c.Dread<82)ForceDread(86,"siege");
                if(c.Phase!=last){if(last==Phase.Recovery&&(c.Phase==Phase.Hunt||c.Phase==Phase.Warning)){recoveryCuts++;Debug.Log("HC_PLAY_RECOVERY_CUT to="+c.Phase+" recoveryCuts="+recoveryCuts);}last=c.Phase;}
                if(!game.Player.Hidden&&c.Phase!=Phase.Hunt){if(game.Player.TorchOn)yield return Torch(false);if(hide)yield return Use(hide,"re-hide");}
                yield return null;
            }
            yield return Step("siege_done","hunts="+c.Hunts+" nearMissBegin="+Tag("HC_COUNT_NEARMISS begin")+" nearMissOpen="+Tag("HC_COUNT_NEARMISS open=True")+" recoveryCuts="+recoveryCuts+" blocked="+c.BlockedDeaths);
            yield return Finish();
        }
        // lowdread: real dice. The bell gives the first hunt; afterwards the driver keeps Dread below this run's boundary
        // (RunDreadMin) and waits up to 10 minutes for a hunt that starts below it.
        IEnumerator LowDread()
        {
            var c=game.Case;yield return Opening(true);
            yield return Use(Find(InteractKind.Anchor),"chapel bell");
            yield return SurviveHunt();
            yield return WaitFor(()=>c.Phase==Phase.Disturbance,60);
            var hall=game.World.Castle.RuneRingCenter;game.Player.Teleport(new Vector3(hall.x,.05f,hall.z-4));
            float start=Time.realtimeSinceStartup,end=start+600;int hunts=c.Hunts;bool low=false;
            while(Time.realtimeSinceStartup<end)
            {
                if(c.Phase==Phase.Disturbance&&c.Dread>c.RunDreadMin-3)ForceDread(Mathf.Max(0,c.RunDreadMin-12),"stay below the run boundary");
                if(c.Hunts>hunts){low=c.Dread<c.RunDreadMin;Debug.Log("HC_PLAY_LOWDREAD hunt after="+(Time.realtimeSinceStartup-start).ToString("F0")+"s dread="+c.Dread.ToString("F1")+" runDreadMin="+c.RunDreadMin.ToString("F1")+" noTell="+c.NoTellHunt+" phase="+c.Phase);break;}
                yield return null;
            }
            yield return Step("lowdread_done","lowHunt="+low+" waited="+(Time.realtimeSinceStartup-start).ToString("F0")+"s");
            if(c.Phase==Phase.Hunt||c.Phase==Phase.Warning)yield return WaitFor(()=>c.Phase!=Phase.Hunt&&c.Phase!=Phase.Warning,70);
            yield return Finish();
        }
        // natural: no dice or Dread forcing. The escape route with an 8 minute walk through the rooms (torch on, never hidden)
        // before the bell, so the castle's own rhythm, beats, throws and hunts show up as they would for a player.
        IEnumerator Natural()
        {
            var c=game.Case;yield return Opening(true);
            yield return Record(Evidence.Voice,game.World.Voice,3,"ear-trumpet");
            foreach(var title in game.Book.NoteTitles)yield return Use(Find(InteractKind.Note,title),"ledger "+title);
            var spots=new[]{game.World.Castle.RuneRingCenter,game.World.Induction,game.World.Print,game.World.Voice,game.World.Castle.AltarPosition,game.World.Anchor};
            float end=Time.realtimeSinceStartup+480;int k=0;
            while(Time.realtimeSinceStartup<end)
            {
                var p=spots[k++%spots.Length];
                if(NavMesh.SamplePosition(p,out var hit,3,NavMesh.AllAreas))game.Player.Teleport(hit.position+Vector3.up*.05f);
                float stay=Time.realtimeSinceStartup+20;
                while(Time.realtimeSinceStartup<stay){input.Look=game.Player.View.transform.position+Quaternion.Euler(0,Time.realtimeSinceStartup*12,0)*Vector3.forward;yield return null;}
            }
            yield return Step("natural_walk_done","hunts="+c.Hunts+" survived="+c.Survived+" apparitions="+game.Scares.Events+" thrown="+game.Scares.ThrownProps);
            while(c.Phase==Phase.Hunt||c.Phase==Phase.Warning||c.Phase==Phase.Recovery)yield return null;
            if(!c.Survived){yield return Use(Find(InteractKind.Anchor),"chapel bell");yield return SurviveHunt();yield return WaitFor(()=>c.Phase!=Phase.Hunt&&c.Phase!=Phase.Recovery,60);}
            game.Journal=true;game.ApplyFocus();bool solved=c.Conclude(AnchorKind.MusicBox,HauntingPattern.DomesticLoop,ClosureMethod.LetSongEnd);game.Journal=false;game.ApplyFocus();
            yield return Step("ledger_closed","conclude="+solved);
            yield return Use(Find(InteractKind.Breaker),"chapel brazier");
            yield return Use(Find(InteractKind.Anchor),"chapel bell (seal)",1.4f,true,13.5f);
            yield return Step("bell_sealed",c.Phase+" huntActive="+c.HuntActive);
            if(c.Phase==Phase.Return)yield return HostileBridge();
            yield return Finish();
        }
        // death: no driver invulnerability. Two records (a checkpoint), the bell, stand in the open with the torch on until he
        // kills her; then RETRY CHECKPOINT. The retried scene's driver checks the death penalty (Deaths, Dread 60, moved relic).
        static bool deathRetried;
        IEnumerator Death()
        {
            var c=game.Case;
            if(deathRetried)
            {
                yield return Step("after_retry","deaths="+c.Deaths+" penalty="+c.DeathPenaltyApplied+" dread="+c.Dread.ToString("F1")+" movedRelic="+game.MovedRelic+" phase="+c.Phase+" evidence="+c.Evidence.Count);
                Debug.Log("HC_PLAY_RESULT mode=death won=False deaths="+c.Deaths+" penalty="+c.DeathPenaltyApplied+" dread="+c.Dread.ToString("F1")+" movedRelic="+game.MovedRelic+" blockers="+blockers.Count);
                yield return Wait(1);Application.Quit();yield break;
            }
            c.Invulnerable=false;
            yield return Opening(true);
            yield return Wait(1.5f); // let the checkpoint be saved after the second record
            yield return Use(Find(InteractKind.Anchor),"chapel bell");
            var hall=game.World.Castle.RuneRingCenter;game.Player.Teleport(new Vector3(hall.x,.05f,hall.z-3));yield return Torch(true);
            float end=Time.realtimeSinceStartup+240;
            while(c.Phase!=Phase.Dead&&Time.realtimeSinceStartup<end)
            {
                input.Look=game.Creature.HeadPosition;
                if(c.Phase==Phase.Disturbance&&c.Dread<85)ForceDread(90,"call another hunt (death run)");
                yield return null;
            }
            yield return Step("dead","phase="+c.Phase+" deaths="+c.Deaths+" checkpoint="+game.HasCheckpoint);
            if(c.Phase!=Phase.Dead){Blocker("no death in 240 s");yield return Finish();yield break;}
            yield return Wait(Mathf.Max(1,game.Creature.Profile.CapturePresentationSeconds+1));
            deathRetried=true;Debug.Log("HC_PLAY phase=retry_checkpoint");game.RetryCheckpoint();
        }
        // Follow a hunt to its end: closest approach, near-miss and a wall check on his ground (crawl) moves.
        IEnumerator TrackHunt(string label)
        {
            var c=game.Case;float closest=99;int wallHits=0,groundSteps=0;Vector3 last=game.Creature.transform.position;
            float end=Time.realtimeSinceStartup+70; // hunts are 25-50 s (wave G)
            while(c.Phase==Phase.Hunt&&Time.realtimeSinceStartup<end)
            {
                var at=game.Creature.transform.position;closest=Mathf.Min(closest,Vector3.Distance(at,game.Player.transform.position));
                bool grounded=at.y-World.FloorBelow(at.y)<.5f&&last.y-World.FloorBelow(last.y)<.5f;
                if(grounded&&(at-last).sqrMagnitude>.0004f)
                {
                    groundSteps++;
                    if(Physics.Linecast(last+Vector3.up*.35f,at+Vector3.up*.35f,out var hit,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore)&&!hit.collider.transform.IsChildOf(game.Creature.transform)&&!(game.Creature.Actor&&hit.collider.transform.IsChildOf(game.Creature.Actor.transform)))
                    {wallHits++;if(wallHits<=5)Debug.Log("HC_PLAY_WALL "+label+" from="+last+" to="+at+" hit="+hit.collider.name);}
                }
                last=at;yield return null;
            }
            yield return Step(label+"_hunt_over","closest="+closest.ToString("F2")+" nearMissBegin="+Tag("HC_COUNT_NEARMISS begin")+" nearMissHold="+Tag("HC_COUNT_NEARMISS hold")+" groundSteps="+groundSteps+" wallCrossings="+wallHits+" blockedDeaths="+c.BlockedDeaths);
        }
        IEnumerator Witnessed()
        {
            var c=game.Case;game.Player.Teleport(game.World.Spawn+Vector3.zero);
            // Stand in the open on the hall floor with the torch on, wait until he sees her, then hide in the nearest cupboard.
            // Stand at the exit of the hall-floor hide nearest the rune ring with the torch on, wait until he sees her,
            // then switch the torch OFF and hide while he watches (critic r10 #16: the open must not depend on the torch).
            var hall=game.World.Castle.RuneRingCenter;
            Interactable near=null;float best=99;
            foreach(var h in game.World.HidingPlaces){if(!h||!h.gameObject.activeInHierarchy||Mathf.Abs(h.HidePoint.y)>1)continue;float d=Vector3.Distance(h.HidePoint,hall);if(d<best){best=d;near=h;}}
            if(near)game.Player.Teleport(near.ExitPoint);else game.Player.Teleport(new Vector3(hall.x,.05f,hall.z-4));
            yield return Torch(true);
            float end=Time.realtimeSinceStartup+30;
            while(c.Phase==Phase.Hunt&&!game.Creature.CanSeePlayer&&Time.realtimeSinceStartup<end){input.Look=game.Creature.HeadPosition;yield return null;}
            // Torch off first, then hide in the same frame he sees her (WitnessHide samples his sight at that instant).
            yield return Torch(false);
            float again=Time.realtimeSinceStartup+10;
            while(c.Phase==Phase.Hunt&&!game.Creature.CanSeePlayer&&Time.realtimeSinceStartup<again){input.Look=game.Creature.HeadPosition;yield return null;}
            bool seen=game.Creature.CanSeePlayer;
            if(near)game.Player.Hide(near);
            yield return Step("witnessed_hide","seenBeforeHiding="+seen+" torch="+game.Player.TorchOn+" hide="+(near?near.name:"none")+" flight="+game.Creature.DiagnosticState);
        }
        void Battlement()
        {
            var target=game.World.Castle.GatehouseBattlement+Vector3.up*1.4f;int tested=0,visible=0;Vector3 bestFrom=Vector3.zero;float bestD=0;
            for(float x=-14;x<=14;x+=2)for(float z=-24;z<=28;z+=2)
            {
                if(!NavMesh.SamplePosition(new Vector3(x,0,z),out var hit,.6f,NavMesh.AllAreas)||Mathf.Abs(hit.position.y)>.4f)continue;
                if(game.World.IsInside(hit.position)||game.World.IsSafe(hit.position))continue;
                tested++;var eye=hit.position+Vector3.up*1.64f;
                if(!Physics.Linecast(eye,target,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore)){visible++;float d=Vector3.Distance(eye,target);if(d>bestD){bestD=d;bestFrom=eye;}}
            }
            Debug.Log("HC_PLAY_BATTLEMENT target="+target+" outdoorPoints="+tested+" withClearLine="+visible+(visible>0?" farthest="+bestFrom+" d="+bestD.ToString("F1"):""));
        }
        IEnumerator HostileBridge()
        {
            var c=game.Case;var bridge=game.World.Castle.Drawbridge;
            int colliders=bridge?bridge.GetComponentsInChildren<Collider>().Length:0;
            // Wait at the inner gate (as a player must) while the bridge comes down and he hunts; walk when it is down
            // and either he is within 8 m or 20 s have passed.
            game.Player.Teleport(new Vector3(0,.1f,1.2f));input.Look=new Vector3(0,1.6f,-20);
            float waitStart=Time.realtimeSinceStartup,waitClosest=99,nextWaitLog=0;
            while(Time.realtimeSinceStartup-waitStart<20&&c.Phase==Phase.Return)
            {
                float dist=Vector3.Distance(game.Creature.transform.position,game.Player.transform.position);waitClosest=Mathf.Min(waitClosest,dist);
                if(Time.realtimeSinceStartup>nextWaitLog){nextWaitLog=Time.realtimeSinceStartup+1;Debug.Log("HC_PLAY_BRIDGE_WAIT t="+(Time.realtimeSinceStartup-waitStart).ToString("F1")+" moving="+(bridge&&bridge.Moving)+" count="+game.Creature.transform.position+" dist="+dist.ToString("F1")+" blocked="+c.BlockedDeaths);}
                if(bridge&&!bridge.Moving&&dist<8)break;
                yield return null;
            }
            Debug.Log("HC_PLAY_BRIDGE_WAIT done seconds="+(Time.realtimeSinceStartup-waitStart).ToString("F1")+" closest="+waitClosest.ToString("F1")+" lowerSeconds="+(bridge?CastleDrawbridge.RaisedAngle/bridge.DegreesPerSecond:0).ToString("F1"));
            var mid=game.World.Castle.DrawbridgeTrigger.center;
            bool ground=Physics.Raycast(new Vector3(0,3,-3),Vector3.down,out var hit,8,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore);
            yield return Step("drawbridge_lowered","colliders="+colliders+" groundHit="+(ground?hit.collider.name+"@"+hit.point.y.ToString("F2"):"none")+" count="+game.Creature.transform.position);
            input.Look=new Vector3(0,1.6f,-20);yield return Frames(3);
            input.Move=new Vector2(0,1);float walk=Time.realtimeSinceStartup+14,closest=99;int blocked=c.BlockedDeaths;float nextLog=0;
            while(Time.realtimeSinceStartup<walk&&c.Phase!=Phase.Won)
            {
                var at=game.Creature.transform.position;closest=Mathf.Min(closest,Vector3.Distance(at,game.Player.transform.position));
                if(Time.realtimeSinceStartup>nextLog){nextLog=Time.realtimeSinceStartup+.5f;Debug.Log("HC_PLAY_BRIDGE player="+game.Player.transform.position+" count="+at+" huntActive="+c.HuntActive+" dist="+Vector3.Distance(at,game.Player.transform.position).ToString("F1"));}
                yield return null;
            }
            Reset();
            yield return Step("bridge_crossed","won="+(c.Phase==Phase.Won)+" closest="+closest.ToString("F2")+" grabsOnBridge="+(c.BlockedDeaths-blocked)+" far="+mid);
            if(c.Phase!=Phase.Won){Blocker("bridge walk did not reach the far bank (at "+game.Player.transform.position+")");game.Player.Teleport(mid-Vector3.up*1.0f);yield return Frames(5);}
        }
        IEnumerator Relics()
        {
            var castle=game.Castle;
            // Silver blade: the lantern on the reliquary's claw marks, then open the box.
            var box=Find(InteractKind.Relic,null,0);
            yield return Use(box,"reliquary (sealed)");
            yield return Step("reliquary_sealed","bladeRevealed="+castle.BladeRevealed);
            yield return SelectTool(2);
            Transform claws=null;foreach(var t in game.World.Root.GetComponentsInChildren<Transform>(true))if(t.name=="Reliquary claw-scores")claws=t;
            if(claws){StandNear(claws.position,1.8f,Floor(claws.position.y-.2f));float end=Time.realtimeSinceStartup+3;while(Time.realtimeSinceStartup<end&&!castle.BladeRevealed)yield return null;}
            if(!castle.BladeRevealed)Blocker("lantern did not reveal the reliquary claw marks");
            yield return Step("claws_revealed","bladeRevealed="+castle.BladeRevealed);
            yield return Use(box,"reliquary (open)");
            // Hallowed hilt: the orrery socket. Moonstone: behind the portrait door.
            yield return Use(Find(InteractKind.Relic,null,1),"orrery socket",1.2f);
            Interactable portrait=null;foreach(var item in game.World.Root.GetComponentsInChildren<Interactable>(true))if(item.Kind==InteractKind.Door&&item.Label=="Portrait door")portrait=item;
            if(portrait&&portrait.Door&&!portrait.Door.Open)yield return Use(portrait,"portrait door");
            yield return Wait(1.2f);
            yield return Use(Find(InteractKind.Relic,null,2),"moonstone pedestal",1.2f);
            yield return Step("relics_found","relics="+castle.Forge.RelicCount);
            if(castle.Forge.RelicCount<3)Blocker("relics found "+castle.Forge.RelicCount+"/3");
            // Forge: hold E at the altar for the 9 s craft.
            yield return Use(Find(InteractKind.Altar),"forge altar",1.4f,true,10.5f);
            yield return Step("sword_forged","sword="+castle.Forge.HasSword+" progress="+castle.Forge.CraftProgress01.ToString("F2"));
            if(!castle.Forge.HasSword)Blocker("forge did not complete");
        }
        IEnumerator Fight()
        {
            var castle=game.Castle;int rings=1;float limit=Time.realtimeSinceStartup+300,nextApproach=0;
            // Give him a window: no strike and no step towards him for the first 3 s after he becomes
            // hittable (a hunt starts or a retreat ends), so his own glide/dive/grab can play out.
            float Window=StrikeWindow;float hittableSince=-1;int grabs=game.Case.BlockedDeaths;float lastGrabLog=-9; // one line per grab burst (invulnerability re-grabs every frame)
            while(!castle.Fight.Dead&&Time.realtimeSinceStartup<limit)
            {
                if(game.Case.BlockedDeaths>grabs&&Time.realtimeSinceStartup-lastGrabLog>1f){grabs=game.Case.BlockedDeaths;lastGrabLog=Time.realtimeSinceStartup;Debug.Log("HC_PLAY phase=grabbed t="+(Time.realtimeSinceStartup-start).ToString("F1")+" blockedFrames="+grabs+" "+game.Creature.DiagnosticState);}
                bool hittable=game.Case.Phase==Phase.Hunt&&castle.Fight.CanBeHit;
                if(!hittable)hittableSince=-1;else if(hittableSince<0){hittableSince=Time.realtimeSinceStartup;Debug.Log("HC_PLAY window t="+(hittableSince-start).ToString("F1")+" hold strikes "+Window+" s");}
                bool windowOpen=hittable&&Time.realtimeSinceStartup-hittableSince<Window;
                if(game.Case.Phase!=Phase.Warning&&game.Case.Phase!=Phase.Hunt)
                {
                    if(game.Case.Terminal)break;
                    yield return Use(Find(InteractKind.Anchor),"chapel bell (call)");rings++;
                    continue;
                }
                if(game.Case.Phase==Phase.Warning){yield return null;continue;}
                // Track him; strike (the instrument button) whenever he is in reach and hittable.
                // If he stays far (perched/retreating) step to a walkable spot below him.
                var him=game.Creature.transform.position;input.Look=him+Vector3.up*1.2f;
                float reach=Vector3.Distance(game.Player.View.transform.position,him+Vector3.up*1.2f);
                if(windowOpen){yield return null;continue;}
                if(castle.Fight.CanBeHit&&reach<3.0f)
                {
                    int before=castle.Fight.Hits;yield return Press(i=>i.Fire=true);
                    if(castle.Fight.Hits>before)yield return Step("hit_"+castle.Fight.Hits,"staggered="+castle.Fight.Staggered+" tier="+castle.Fight.Tier+" countPos="+him+" reach="+reach.ToString("F2"));
                    continue;
                }
                if(castle.Fight.CanBeHit&&Time.realtimeSinceStartup>nextApproach&&NavMesh.SamplePosition(new Vector3(him.x,World.FloorBelow(him.y),him.z),out var near,3f,NavMesh.AllAreas)&&him.y-near.position.y<2.2f)
                {
                    nextApproach=Time.realtimeSinceStartup+1.5f;
                    var dir=Vector3.ProjectOnPlane(game.Player.transform.position-him,Vector3.up);if(dir.sqrMagnitude<.01f)dir=Vector3.back;
                    var stand=near.position+dir.normalized*1.4f;if(NavMesh.SamplePosition(stand,out var s2,1.5f,NavMesh.AllAreas))stand=s2.position;
                    game.Player.Teleport(stand+Vector3.up*.05f);
                }
                yield return null;
            }
            if(!castle.Fight.Dead)Blocker("fight not finished in 300 s (hits "+castle.Fight.Hits+", bell rings "+rings+")");
            yield return Wait(2.5f);
        }
        IEnumerator Finish()
        {
            yield return Wait(1.5f);
            var c=game.Case;
            yield return Step(c.Phase==Phase.Won?(c.HunterSlain?"won_kill":"won_escape"):"not_won","goal="+c.Goal);
            if(Mode!="escape"&&Mode!="kill"){var sb=new System.Text.StringBuilder();foreach(var t in Tags)sb.Append(t.Trim().Replace(' ','_')).Append('=').Append(Tag(t)).Append(' ');Debug.Log("HC_PLAY_SUMMARY "+sb+"dread="+c.Dread.ToString("F1")+" hunts="+c.Hunts);}
            Debug.Log("HC_PLAY_RESULT mode="+Mode+" won="+(c.Phase==Phase.Won)+" slain="+c.HunterSlain+" blockedDeaths="+c.BlockedDeaths+" blockers="+blockers.Count+(blockers.Count>0?" ["+string.Join(" | ",blockers)+"]":""));
            yield return Wait(1);Application.Quit();
        }
    }
}
