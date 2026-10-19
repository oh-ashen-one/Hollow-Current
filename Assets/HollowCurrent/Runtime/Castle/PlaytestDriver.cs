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
    // cold (fb-onboard, owner feedback 2026-10-15): a first-time player. Never touches the equipment rack, starts with no run history
    // (Game: the Early first-hunt band, nothing written back), reads the gate keeper's letter, then walks the great hall on foot for 180 s
    // of case time and presses E on the five ground-floor hides. Asserts >= 4 HC_WATCHED cues, the first <= 25 s, the case never in
    // Briefing after 1 s, and every hide either hiding her or giving a notice. Summary: HC_PLAY_COLD, HC_ONBOARD, HC_PLAY_STATS.
    // fear: the wave F proof run (FEAR_PLAN). It steers the real rules engine through Investigation.Dice and
    // AddDread (logged as driver-forced) to get a false warning, a no-tell hunt, a told hunt it hides in while
    // watched, the gatehouse visit and the hostile return, and calls the director's real throw/beat methods.
    // Drives the investigator through Investigator.TestInput (the same path as keyboard and
    // pad input: look, use, hold, fire, tool select, torch), teleporting between stops instead
    // of pathing. Logs every phase as HC_PLAY and saves a screenshot per phase into <outdir>.
    // Driver-only safety: Case.Invulnerable counts a grab instead of ending the run; whether
    // it was needed is logged (blockedDeaths). Started from Bellwether, it switches to Castle.
    // fd-grab: runs after every other script each frame, so its grab measures read the skull and camera as they are rendered
    // (Presence seats and frames the grab in its LateUpdate).
    [DefaultExecutionOrder(10000)]
    public sealed class PlaytestDriver : MonoBehaviour
    {
        public static string OutDir {get;private set;}
        public static string Mode {get;private set;}
        // Kill run: seconds without strikes after he becomes hittable (-hc-playtest-window, default 3).
        public static float StrikeWindow=3f;
        // siege with -hc-hold-dread <d> (wave L, r12 critic pass 2): Dread held in d-10..d+10 instead of 82+, leaving and re-entering
        // the same hide after every hunt (each re-entry is a door-held roll at Dread >= 60) until 6 hidden hunts. -1 = classic siege.
        public static float HoldDread=-1;
        public static bool Requested
        {
            get
            {
                if(OutDir!=null)return true;
                var args=Environment.GetCommandLineArgs();
                for(int i=0;i<args.Length-1;i++)if(args[i]=="-hc-hold-dread"&&float.TryParse(args[i+1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var hd))HoldDread=hd;
                for(int i=0;i<args.Length-1;i++)if(args[i]=="-hc-playtest-window"&&float.TryParse(args[i+1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var w))StrikeWindow=w;
                for(int i=0;i<args.Length-1;i++)if(args[i]=="-hc-playtest"){OutDir=args[i+1];Mode=i+2<args.Length?args[i+2]:"escape";return true;}
                return false;
            }
        }
        Game game;FieldInput input;float start;int shot;
        // fear mode: counts of HC_ log lines by prefix (Application.logMessageReceived), summarised at the end.
        readonly Dictionary<string,int> tags=new Dictionary<string,int>();
        static readonly string[] Tags={"HC_LIE","HC_DUCK","HC_SCARE_CASTLE silence","HC_COUNT_WITNESSED","HC_COUNT_NEARMISS open=True","HC_COUNT_RETURN_SWOOP","HC_COUNT_DIVE_LAST","HC_RECOVERY","HC_NAIL_HIDES","HC_COUNT_PERCH flight observed=True","HC_THROW_SKIP","HC_MISSING_ART","HC_THROW ","HC_THROW poltergeist","HC_SCARE_CASTLE","HC_COUNT_PERCH moved","HC_COUNT_PERCH landed","HC_COUNT_NEARMISS begin","HC_COUNT_NEARMISS hold","HC_WARNING","HC_FALSE_WARNING","HC_HUNT_START","HC_HUNT_SPAWN hostileReturn=True","HC_COUNT_GATEHOUSE landed","HC_COUNT_VOICE","HC_EVP","HC_COUNT_GAMBLE","HC_TORCH_FAIL","HC_COUNT_DREAD","HC_COUNT_GRAB","HC_COUNT_LEARNED","HC_ESCALATE","HC_RARE_LINE","HC_DOOR_HELD"};
        void OnLog(string message,string stack,LogType type)
        {
            foreach(var t in Tags)if(message.StartsWith(t,StringComparison.Ordinal)){tags.TryGetValue(t,out int n);tags[t]=n+1;}
            if(message.StartsWith("HC_COUNT_GRAB mode=",StringComparison.Ordinal)){var mm=message.Substring(19);int sp=mm.IndexOf(' ');if(int.TryParse(sp>0?mm.Substring(0,sp):mm,out int gm))lastGrabMode=gm;}
            // fd-grab: HC_GRAB_COMMIT carries the mode actually played (after 8->9 or headroom switches); the run's grab records.
            if(message.StartsWith("HC_GRAB_COMMIT mode=",StringComparison.Ordinal)){var mm=message.Substring(20);int sp=mm.IndexOf(' ');if(int.TryParse(sp>0?mm.Substring(0,sp):mm,out int gm)){lastGrabMode=gm;commits.Add((gm,message.Contains(" hide=True"),message.Contains(" return=True"),message.Contains(" headroom=True")));}}
            // fe-integ: frames of an armed escalation (fe-count: the apparition vanishes, the silent tell and the hunt come 11.5-13.5 s later).
            if(message.StartsWith("HC_APPARITION_GATE deferred=escalation",StringComparison.Ordinal)&&escShots<2&&isActiveAndEnabled){escShots++;StartCoroutine(EscalationShots(escShots));}
            if(message.StartsWith("HC_GRAB_FIRSTFRAME",StringComparison.Ordinal)){firstFrames++;if(message.Contains(" visible=True"))firstVisible++;}
            // fe-count: every dive must end in a commit or a readable miss (HC_COUNT_DIVE end=commit / miss=True).
            if(message.StartsWith("HC_COUNT_DIVE position=",StringComparison.Ordinal))diveStarts++;
            if(message.StartsWith("HC_COUNT_DIVE end=commit",StringComparison.Ordinal))diveCommits++;
            if(message.StartsWith("HC_COUNT_DIVE miss=True",StringComparison.Ordinal))diveMisses++;
            if(message.StartsWith("HC_GRAB_CAMDIST min=",StringComparison.Ordinal)){camDists++;var v=message.Substring(20);int sp=v.IndexOf(' ');if(float.TryParse(sp>0?v.Substring(0,sp):v,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float d)){camDistMin=Mathf.Min(camDistMin,d);if(d>=.6f)camDistOk++;}
                int lc=message.IndexOf(" lineClear=",StringComparison.Ordinal);if(lc>=0){var parts=message.Substring(lc+11).Split(' ')[0].Split('/');if(parts.Length==2&&parts[0]==parts[1])lineAllClear++;}}
            if(message.StartsWith("HC_GRAB_HUD pixels=",StringComparison.Ordinal)){hudRecords++;if(message.StartsWith("HC_GRAB_HUD pixels=0 ",StringComparison.Ordinal))hudZero++;}
            if(game&&game.Case!=null)
            {
                if(message.StartsWith("HC_SCARE visible=",StringComparison.Ordinal))scareTimes.Add(game.Case.Elapsed);
                if(message.StartsWith("HC_HUNT_START",StringComparison.Ordinal))huntStarts.Add((game.Case.Elapsed,message.Contains(" phase=Return")));
            }
            // Wave K: a screenshot at every grab-camera sample (commit, +0.3 s, +1.0 s, kill), named by kind and time.
            if(message.StartsWith("HC_COUNT_GRABCAM t=",StringComparison.Ordinal))
            {
                // Wave L: count-l logs path= (kind= before count17) and the pixel measures skull_in_central60= / claw_px_proj=; they are
                // written next to the frame name in grabcam.txt so the critic can check the measure against the picture.
                string kind="grab",tt="t";var keep=new System.Text.StringBuilder();
                foreach(var part in message.Split(' '))
                {
                    if(part.StartsWith("kind=",StringComparison.Ordinal)||part.StartsWith("path=",StringComparison.Ordinal))kind=part.Substring(5);
                    if(part.StartsWith("t=",StringComparison.Ordinal))tt=part.Substring(2);
                    foreach(var k in GrabKeys)if(part.StartsWith(k,StringComparison.Ordinal))keep.Append(part).Append(' ');
                }
                pendingGrabShots.Enqueue(("grabcam_"+kind+"_t"+tt,keep.ToString().Trim()));
            }
        }
        static readonly string[] GrabKeys={"t=","path=","kind=","visible=","blend=","kill=","mode=","skull_px=","skull_in_central60=","claw_px_proj=","claw_ok=","eyeToSkull=","headAboveEye="};
        // Wave L run statistics (r12 critic pass tests), printed as HC_PLAY_STATS by Finish: apparition times, hunt starts,
        // threat time (Warning, Hunt, hostile Return), the longest stretch without one, and hunts spent hidden.
        readonly List<float> scareTimes=new List<float>();
        readonly List<(float t,bool ret)> huntStarts=new List<(float,bool)>();
        float threatTime,gapStart,longestGap,lastStatT;bool inThreat,statsOn,huntHidden,inHunt;int hiddenHunts,hiddenSurvived,blockedAtHunt;
        void Update()
        {
            if(!game||game.Case==null||!game.Started||!statsOn)return;
            var c=game.Case;float t=c.Elapsed,dt=Mathf.Max(0,t-lastStatT);lastStatT=t;
            bool threat=c.Phase==Phase.Warning||c.Phase==Phase.Hunt||c.Phase==Phase.Return&&c.HuntActive;
            if(threat)threatTime+=dt;
            if(threat&&!inThreat)longestGap=Mathf.Max(longestGap,t-gapStart);
            if(!threat&&inThreat)gapStart=t;
            inThreat=threat;
            bool hunt=c.Phase==Phase.Hunt;
            if(hunt&&!inHunt){huntHidden=game.Player.Hidden;blockedAtHunt=c.BlockedDeaths;}
            if(hunt&&game.Player.Hidden)huntHidden=true;
            if(!hunt&&inHunt&&huntHidden){hiddenHunts++;if(c.BlockedDeaths==blockedAtHunt&&c.Phase!=Phase.Dead)hiddenSurvived++;Debug.Log("HC_PLAY_HIDDEN_HUNT n="+hiddenHunts+" survived="+(c.BlockedDeaths==blockedAtHunt&&c.Phase!=Phase.Dead)+" dread="+c.Dread.ToString("F1"));}
            inHunt=hunt;
        }
        string Stats()
        {
            var c=game.Case;float t=c.Elapsed;float gap=Mathf.Max(longestGap,inThreat?0:t-gapStart);
            int nonReturn=0,afterScare=0;float first=-1;
            foreach(var h in huntStarts){if(h.ret)continue;nonReturn++;if(first<0)first=h.t;foreach(var a in scareTimes)if(h.t-a>=0&&h.t-a<=10){afterScare++;break;}}
            string cv="n/a";
            if(scareTimes.Count>=3)
            {
                var gaps=new List<float>();for(int i=1;i<scareTimes.Count;i++)gaps.Add(scareTimes[i]-scareTimes[i-1]);
                float mean=0;foreach(var g in gaps)mean+=g;mean/=gaps.Count;float v=0;foreach(var g in gaps)v+=(g-mean)*(g-mean);v/=gaps.Count;
                cv=(Mathf.Sqrt(v)/Mathf.Max(.01f,mean)).ToString("F2");
            }
            var arrivals=new System.Text.StringBuilder();foreach(var a in scareTimes)arrivals.Append(a.ToString("F0")).Append(',');
            return "huntsNonReturn="+nonReturn+" withinScare10s="+afterScare+" pctWithinScare10s="+(nonReturn>0?(100f*afterScare/nonReturn).ToString("F0"):"n/a")
                +" apparitions="+scareTimes.Count+" apparitionCV="+cv+" apparitionTimes=["+arrivals.ToString().TrimEnd(',')+"]"
                +" firstHunt="+(first<0?"none":first.ToString("F0"))+" huntShare="+(t>0?(threatTime/t).ToString("F3"):"0")+" longestGap="+gap.ToString("F0")
                +" grabContinuity="+continuityPass+"/"+continuityGrabs+" grabContinuity20="+continuityPass20+"/"+continuity20+" hiddenHunts="+hiddenHunts+" hiddenSurvived="+hiddenSurvived+" hiddenSurvivalPct="+(hiddenHunts>0?(100f*hiddenSurvived/hiddenHunts).ToString("F0"):"n/a")+" runSeconds="+t.ToString("F0")
                +(game.Onboarding?" onboard: "+game.Onboarding.SummaryFields():"");
        }
        readonly Queue<(string name,string measures)> pendingGrabShots=new Queue<(string,string)>();
        void LateUpdate(){if(pendingGrabShots.Count>0&&!grabShotBusy)StartCoroutine(GrabShot(pendingGrabShots.Dequeue()));GrabContinuity();}
        // fd-grab: the run's grab records, printed by Finish and on quit (death and huntcam quit without Finish).
        readonly List<(int mode,bool hide,bool ret,bool headroom)> commits=new List<(int,bool,bool,bool)>();
        int diveStarts,diveCommits,diveMisses;
        int firstFrames,firstVisible,camDists,camDistOk,lineAllClear,hudRecords,hudZero,continuity20,continuityPass20;float camDistMin=99;bool grabSummaryPrinted;
        string GrabSummary()
        {
            var hist=new int[16];int open=0,openHeadroom=0,openHeadroomDrops=0,hide=0,ret=0;
            foreach(var g in commits){if(g.mode>=0&&g.mode<hist.Length)hist[g.mode]++;if(g.hide)hide++;if(g.ret)ret++;if(!g.hide&&!g.ret){open++;if(g.headroom){openHeadroom++;if(g.mode==8||g.mode==12)openHeadroomDrops++;}}}
            var b=new System.Text.StringBuilder();for(int m=6;m<=12;m++)b.Append(" m").Append(m).Append('=').Append(hist[m]);
            return "grabs="+commits.Count+b+" hide="+hide+" return="+ret+" open="+open+" openHeadroom="+openHeadroom+" openHeadroomDrops="+openHeadroomDrops+
                " dropShareHeadroom="+(openHeadroom>0?(100f*openHeadroomDrops/openHeadroom).ToString("F0")+"%":"n/a");
        }
        void PrintGrabSummary()
        {
            if(grabSummaryPrinted||game==null||game.Map!=MapId.Castle)return;grabSummaryPrinted=true;
            if(game.Creature)game.Creature.CloseOpenDive("the run ended mid-dive (driver summary)");   // fe-count: counted below
            Debug.Log("HC_GRAB_MODE_HIST final "+GrabSummary());
            Debug.Log("HC_GRAB_SUMMARY grabs="+commits.Count+" continuity="+continuityPass+"/"+continuityGrabs+" continuity20="+continuityPass20+"/"+continuity20+
                " firstFrameVisible="+firstVisible+"/"+firstFrames+" camDistOk="+camDistOk+"/"+camDists+" camDistMin="+(camDists>0?camDistMin.ToString("F2"):"n/a")+
                " lineAllClear="+lineAllClear+"/"+camDists+" hudZero="+hudZero+"/"+hudRecords+" teleportsDuringGrab="+grabTeleports+
                " dives="+diveStarts+" diveCommits="+diveCommits+" diveMisses="+diveMisses+" diveUnresolved="+Mathf.Max(0,diveStarts-diveCommits-diveMisses));
        }
        void OnApplicationQuit(){PrintGrabSummary();}
        // Wave L3 continuity measure (critic r13 kill pass), per grab, from the skull's world position every frame (after animation):
        //   commit: the skull at the first grab-camera frame is within 0.5 m of the last frame's skull before it;
        //   fall (drop modes 8 and 12): no frame before the skull's lowest point rises more than 0.05 m (moving against the fall);
        //   step: no frame step exceeds the clip's own skull speed x dt + 0.3 m. fd-grab: the clip speed is the median speed (step / dt)
        //   of the neighbouring frames (+-5) and dt is the frame's presentation step (Presence.GrabFrameDt), so a slow screenshot frame
        //   is judged against the motion it really carried (r13: steps of 0.44-0.87 m were clip speed x 0.1-0.2 s frames, judged against
        //   a 60 fps neighbour step). The r13 measure (raw neighbour steps) is kept as legacy_pass.
        //   crawl (mode 9): no step over 0.5 m. Also per grab: the closest eye-to-skull distance and frames with a blocked eye line.
        // HC_PLAY_GRAB_CONTINUITY per grab with the offending frame; totals in HC_PLAY_STATS and HC_GRAB_SUMMARY.
        int lastGrabMode=-1,continuityGrabs,continuityPass;Vector3 lastPreSkull;int lastPreFrame=-9;int grabModeAtCommit;float grabClock;
        readonly List<(float t,Vector3 p,float dt,float eye,bool line)> grabSkull=new List<(float,Vector3,float,float,bool)>();
        Vector3 commitPreSkull;bool commitHasPre;
        void GrabContinuity()
        {
            if(!game||!game.Creature||game.Map!=MapId.Castle)return;
            var cr=game.Creature;bool cam=cr.GrabCamActive;var skull=cr.HeadPosition;
            if(cam&&!inGrab)
            {
                inGrab=true;grabEvaluated=false;grabSkull.Clear();grabClock=0;grabModeAtCommit=lastGrabMode;
                commitHasPre=lastPreFrame==Time.frameCount-1;commitPreSkull=lastPreSkull;
            }
            if(inGrab&&!grabEvaluated)
            {
                float dt=cr.GrabFrameDt>0?cr.GrabFrameDt:Time.unscaledDeltaTime;
                if(cam&&grabClock<=1.2f)
                {
                    grabClock+=grabSkull.Count==0?0:dt;
                    var eye=game.Player.View.transform.position;
                    bool line=!Physics.Linecast(eye,skull,out var hit,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore)||hit.collider.transform.IsChildOf(cr.transform)||(cr.Actor&&hit.collider.transform.IsChildOf(cr.Actor.transform));
                    grabSkull.Add((grabClock,skull,dt,Vector3.Distance(eye,skull),line));
                }
                if((!cam||grabClock>1.2f)&&grabSkull.Count>0){EvaluateGrab();grabEvaluated=true;}
            }
            if(!cam&&inGrab)inGrab=false;
            if(!cam&&cr.Actor&&cr.Actor.gameObject.activeInHierarchy){lastPreSkull=skull;lastPreFrame=Time.frameCount;}
        }
        bool inGrab,grabEvaluated;
        static float Median(List<float> v){if(v.Count==0)return 0;v.Sort();return v[v.Count/2];}
        void EvaluateGrab()
        {
            continuityGrabs++;var g=grabSkull;int mode=grabModeAtCommit;bool drop=mode==8||mode==12,crawl=mode==9;
            float commitGap=commitHasPre?Vector3.Distance(commitPreSkull,g[0].p):-1;bool commitOk=commitGap<0||commitGap<=.5f;
            int low=0;for(int i=1;i<g.Count;i++)if(g[i].p.y<g[low].p.y)low=i;
            string against="none";bool fallOk=true;
            if(drop)for(int i=1;i<=low;i++)if(g[i].p.y-g[i-1].p.y>.05f){fallOk=false;against="t="+g[i].t.ToString("F2")+" rise="+(g[i].p.y-g[i-1].p.y).ToString("F2");break;}
            var steps=new List<float>();var speeds=new List<float>();
            for(int i=1;i<g.Count;i++){float st=Vector3.Distance(g[i].p,g[i-1].p);steps.Add(st);speeds.Add(st/Mathf.Max(1e-4f,g[i].dt));}
            string pop="none",legacyPop="none";bool stepOk=true,legacyOk=true,crawlOk=true;float maxStep=0,maxDt=0;
            for(int i=0;i<steps.Count;i++)
            {
                var nearSteps=new List<float>();var nearSpeeds=new List<float>();
                for(int j=Mathf.Max(0,i-5);j<=Mathf.Min(steps.Count-1,i+5);j++)if(j!=i){nearSteps.Add(steps[j]);nearSpeeds.Add(speeds[j]);}
                float dt=g[i+1].dt;maxStep=Mathf.Max(maxStep,steps[i]);maxDt=Mathf.Max(maxDt,dt);
                float limit=Median(nearSpeeds)*dt+.3f;
                if(steps[i]>limit&&stepOk){stepOk=false;pop="t="+g[i+1].t.ToString("F2")+" step="+steps[i].ToString("F2")+" dt="+dt.ToString("F3")+" limit="+limit.ToString("F2");}
                if(crawl&&steps[i]>.5f&&crawlOk){crawlOk=false;if(pop=="none")pop="t="+g[i+1].t.ToString("F2")+" crawl step="+steps[i].ToString("F2");}
                if(steps[i]>Median(nearSteps)+.3f&&legacyOk){legacyOk=false;legacyPop="t="+g[i+1].t.ToString("F2")+" step="+steps[i].ToString("F2");}
            }
            float minEye=99;int blocked=0;foreach(var s in g){minEye=Mathf.Min(minEye,s.eye);if(!s.line)blocked++;}
            bool pass=commitOk&&fallOk&&stepOk&&crawlOk;if(pass)continuityPass++;
            if(g.Count>=20){continuity20++;if(pass)continuityPass20++;}
            Debug.Log("HC_PLAY_GRAB_CONTINUITY n="+continuityGrabs+" mode="+mode+" frames="+g.Count+" commit_gap="+(commitGap<0?"n/a":commitGap.ToString("F2"))+" commit_ok="+commitOk+" fall_ok="+(drop?fallOk.ToString():"n/a")+" against="+against+
                " step_ok="+stepOk+" crawl_ok="+(crawl?crawlOk.ToString():"n/a")+" max_step="+maxStep.ToString("F2")+" max_dt="+maxDt.ToString("F3")+" pop="+pop+" min_eye_skull="+minEye.ToString("F2")+" line_blocked="+blocked+"/"+g.Count+
                " pass="+pass+" legacy_pass="+(commitOk&&fallOk&&legacyOk)+" legacy_pop="+legacyPop);
        }
        bool grabShotBusy;
        IEnumerator GrabShot((string name,string measures) g)
        {
            grabShotBusy=true;yield return Shot(g.name);
            string file=shot.ToString("00")+"_"+g.name+".png";
            File.AppendAllText(Path.Combine(OutDir,"grabcam.txt"),file+"\t"+g.measures+"\n");Debug.Log("HC_PLAY_GRABSHOT "+file+" "+g.measures);
            grabShotBusy=false;
        }
        int Tag(string t)=>tags.TryGetValue(t,out int n)?n:0;
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
        // The owner's window size is put back by Game.RestoreOwnerWindow on quit.
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
        int escShots;
        IEnumerator EscalationShots(int n)
        {
            float t0=Time.realtimeSinceStartup;
            foreach(var at in new[]{0f,1f,3f,6f,9f,11f,12.2f,13.2f,14.5f,16.5f})
            {
                while(Time.realtimeSinceStartup-t0<at)yield return null;
                Debug.Log("HC_PLAY_ESC_SHOT n="+n+" at="+at.ToString("F1")+" t="+(game&&game.Case!=null?game.Case.Elapsed.ToString("F1"):"-")+" phase="+(game&&game.Case!=null?game.Case.Phase.ToString():"-"));
                yield return Shot("esc"+n+"_t+"+at.ToString("F1"));
            }
        }
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
            if(found){TeleportPlayer(best+Vector3.up*.05f);input.Look=target;}
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
            if(!focused)
            {
                var v=game.Player.View.transform;string hitName=Physics.Raycast(v.position,v.forward,out var fh,2.7f)?fh.collider.name+"@"+fh.distance.ToString("F2"):"nothing";
                Debug.Log("HC_PLAY focus_fail="+item.name+" hit="+hitName+" eye="+v.position+" forward="+v.forward);
                Blocker(what+": not focusable by the interaction ray (focus="+(game.Player.Focus?game.Player.Focus.Label:"none")+", hit="+hitName+")");
            }
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
            // Wave K: an apparition's interference (or a close hunting Count) can suspend recording; wait it out once and retry.
            if(!game.Case.Evidence.Contains(evidence)&&game.Player.Instrument.StartsWith("INTERFERENCE",StringComparison.Ordinal))
            {
                Debug.Log("HC_PLAY record_retry "+what+" after interference");
                float until=Time.realtimeSinceStartup+20;while(game.Player.Instrument.StartsWith("INTERFERENCE",StringComparison.Ordinal)&&Time.realtimeSinceStartup<until)yield return null;
                yield return Frames(6);
                if(tool==3){yield return Press(i=>i.Fire=true);yield return Wait(4.6f);}
                else{input.FireHeld=true;float end2=Time.realtimeSinceStartup+4f;while(Time.realtimeSinceStartup<end2&&!game.Case.Evidence.Contains(evidence))yield return null;Reset();}
            }
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
            game.Case.Invulnerable=true;statsOn=true;lastStatT=game.Case.Elapsed;gapStart=game.Case.Elapsed;
            input=new FieldInput();game.Player.TestInput=input;
            yield return Step("begin_"+Mode,"rules="+game.Case.Rules.Name);
            Application.logMessageReceived+=OnLog;
            if(Mode=="fear"){yield return Fear();yield break;}
            if(Mode=="siege"){yield return Siege();yield break;}
            if(Mode=="lowdread"){yield return LowDread();yield break;}
            if(Mode=="natural"){yield return Natural();yield break;}
            if(Mode=="death"){yield return Death();yield break;}
            if(Mode=="huntcam"){yield return HuntCam();yield break;}
            if(Mode=="cold"){yield return Cold();yield break;}
            if(Mode=="doorproof"){yield return DoorProof();yield break;}
            if(Mode=="settings"){yield return SettingsPages();yield break;}
            // Field kit from the lantern cart.
            yield return Use(Find(InteractKind.Kit),"field kit");
            if(!game.Case.Equipped)Blocker("kit not equipped");
            yield return Step("kit_taken");
            // Arrival letter (gatehouse).
            yield return Use(Find(InteractKind.Note,game.Book.ArrivalNoteTitle),"arrival letter");
            // Enter the castle proper (Survey).
            TeleportPlayer(new Vector3(0,.1f,10));yield return Frames(4);
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
            // Long Night (WarningSeconds 0) has no Warning: the bell starts the hunt at once ("He is already coming."), by design.
            bool longNight=game.Case.Rules.WarningSeconds<=0;
            if(longNight?game.Case.Phase!=Phase.Hunt:game.Case.Phase!=Phase.Warning)Blocker("bell did not start the "+(longNight?"hunt (Long Night)":"warning")+" (phase "+game.Case.Phase+")");
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
            TeleportPlayer(new Vector3(0,.1f,1.2f));input.Look=new Vector3(0,1.6f,-20);yield return Frames(3);
            input.Move=new Vector2(0,1);float walk=Time.realtimeSinceStartup+12;
            while(Time.realtimeSinceStartup<walk&&game.Case.Phase!=Phase.Won)yield return null;
            Reset();
            if(game.Case.Phase!=Phase.Won)
            {
                Blocker("walking over the bridge did not reach the far-bank trigger (stopped at "+game.Player.transform.position+")");
                TeleportPlayer(game.World.Castle.DrawbridgeTrigger.center-Vector3.up*1.0f);yield return Frames(5);
            }
            yield return Finish();
        }
        // ---- fear mode ----------------------------------------------------------------------------------
        readonly Queue<float> dice=new Queue<float>();
        IEnumerator WaitFor(Func<bool> done,float seconds){float end=Time.realtimeSinceStartup+seconds;while(!done()&&Time.realtimeSinceStartup<end)yield return null;}
        // Wave I: Investigation.ForceDread (ambient Dread, clears the post-hunt grace, restarts the dice schedule); AddDread is now capped
        // at 25 per hunt and decays, so it cannot hold a test level.
        void ForceDread(float target,string why){var c=game.Case;float before=c.Dread;c.ForceDread(target);Debug.Log("HC_PLAY_FORCED dread "+before.ToString("F1")+"->"+c.Dread.ToString("F1")+" ("+why+")");}
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
            TeleportPlayer(new Vector3(0,.1f,10));yield return Frames(4);
            yield return Step("entered",game.Case.Phase.ToString());
            yield return Record(Evidence.Induction,game.World.Induction,1,"compass");
            // 1. Dread moves on its own: 8 s inside.
            float d0=c.Dread;yield return Wait(8);yield return Step("dread_natural","from="+d0.ToString("F2")+" to="+c.Dread.ToString("F2")+" lit="+(c.Dread-d0<.8f));
            // 2. Great hall frame time (standing at the rune ring, looking down the hall at the table).
            var hall=game.World.Castle.RuneRingCenter;TeleportPlayer(new Vector3(hall.x,0.05f,hall.z-3.5f));input.Look=new Vector3(hall.x,1.2f,hall.z+4);yield return Wait(1);
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
            var hall2=game.World.Castle.RuneRingCenter;TeleportPlayer(new Vector3(hall2.x,.05f,hall2.z-3.5f));yield return SelectTool(1);
            ForceDread(78,"Lies threshold for the lie beats");
            for(int i=0;i<8&&c.Phase==Phase.Disturbance;i++){game.Scares.Dread();yield return Wait(3f);if(c.Dread<72)ForceDread(78,"keep Lies");}
            yield return Step("lies","HC_LIE="+Tag("HC_LIE")+" silence="+Tag("HC_SCARE_CASTLE silence")+" duck="+Tag("HC_DUCK"));
            // 8. Battlement visibility from the courtyard, then the gatehouse visit (40 s inside after a survived hunt).
            Battlement();
            TeleportPlayer(game.World.Spawn);input.Look=game.World.Castle.GatehouseBattlement+Vector3.up*1.2f;yield return Frames(4);
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
            TeleportPlayer(new Vector3(0,.1f,10));yield return Frames(4);
            yield return Record(Evidence.Induction,game.World.Induction,1,"compass");
            if(second)yield return Record(Evidence.Handprint,game.World.Print,2,"grave-ash lantern");
            yield return Step("opened","runDreadMin="+game.Case.RunDreadMin.ToString("F1"));
        }
        // siege: real dice, Dread held at 82+ by the driver, hidden with the torch off in the far hide. Proves near-miss (and
        // its open at Dread >= 80) and a Recovery cut short. Ends after an open and a Recovery cut, or 12 minutes.
        IEnumerator Siege()
        {
            if(HoldDread>0){yield return SiegeHeld();yield break;}
            var c=game.Case;yield return Opening(false);
            var hide=FarHide();yield return Torch(false);if(hide)yield return Use(hide,"hiding place "+hide.Label);
            yield return Step("siege_hidden",hide?hide.name:"none");
            float end=Time.realtimeSinceStartup+720;int recoveryCuts=0;Phase last=c.Phase;
            while(Time.realtimeSinceStartup<end&&!(Tag("HC_COUNT_NEARMISS open=True")>0&&recoveryCuts>0))
            {
                // Wave I: hold Dread as ambient (event Dread decays) and, once, clear the post-hunt grace at a Recovery
                // start so the Recovery dice (1/8 at >= 80) can still be proven; logged as driver-forced.
                if(c.Dread<82){c.ForceDread(86);Debug.Log("HC_PLAY_FORCED dread ->86 ambient (siege)");}
                if(c.Phase==Phase.Recovery&&last!=Phase.Recovery&&recoveryCuts==0&&c.GraceRemaining>0){Debug.Log("HC_PLAY_FORCED grace "+c.GraceRemaining.ToString("F0")+"->0 (siege recovery-cut proof)");c.ForceDread(Mathf.Max(86,c.Dread));}
                if(c.Phase!=last){if(last==Phase.Recovery&&(c.Phase==Phase.Hunt||c.Phase==Phase.Warning)){recoveryCuts++;Debug.Log("HC_PLAY_RECOVERY_CUT to="+c.Phase+" recoveryCuts="+recoveryCuts);}last=c.Phase;}
                if(!game.Player.Hidden&&c.Phase!=Phase.Hunt){if(game.Player.TorchOn)yield return Torch(false);if(hide)yield return Use(hide,"re-hide");}
                yield return null;
            }
            yield return Step("siege_done","hunts="+c.Hunts+" nearMissBegin="+Tag("HC_COUNT_NEARMISS begin")+" nearMissOpen="+Tag("HC_COUNT_NEARMISS open=True")+" recoveryCuts="+recoveryCuts+" blocked="+c.BlockedDeaths);
            yield return Finish();
        }
        // siege -hc-hold-dread d (wave L): real dice and real near-miss/grab rolls with Dread held in d-10..d+10 (driver-forced to d
        // when it leaves the band, logged). Hidden with the torch off in the far hide; after every hunt she leaves the hide and, 2 s
        // later, re-enters it (Dread raised to at least 60 first, so each entry is a door-held roll; a held door is retried every
        // 3 s). Ends after 6 hidden hunts or 20 minutes (wave L3: 15 minutes reached only 5). Proves: near-miss open or a grab, HC_DOOR_HELD, hidden survival %.
        IEnumerator SiegeHeld()
        {
            var c=game.Case;yield return Opening(false);
            float lo=HoldDread-10,hi=HoldDread+10;
            var hide=FarHide();yield return Torch(false);
            ForceDread(Mathf.Max(60,HoldDread),"siege-held entry");
            if(hide)yield return Use(hide,"hiding place "+hide.Label);
            yield return Step("siege_held_hidden",(hide?hide.name:"none")+" band="+lo.ToString("F0")+"-"+hi.ToString("F0"));
            float end=Time.realtimeSinceStartup+1200;int attempts=1,held=0;Phase last=c.Phase;bool hadHunt=false;float rehideAt=-1;
            while(Time.realtimeSinceStartup<end&&hiddenHunts<6)
            {
                if(c.Terminal)break;
                if(c.Dread<lo||c.Dread>hi)ForceDread(HoldDread,"siege-held band");
                if(c.Phase!=last)
                {
                    if(last==Phase.Hunt){hadHunt=true;}
                    last=c.Phase;
                }
                bool quiet=c.Phase!=Phase.Hunt&&c.Phase!=Phase.Warning;
                // After a hunt: step out, then back in after 4 s.
                if(hadHunt&&quiet&&game.Player.Hidden){yield return Press(i=>i.Use=true);hadHunt=false;rehideAt=Time.realtimeSinceStartup+2;Debug.Log("HC_PLAY_SIEGE_HELD left hide dread="+c.Dread.ToString("F1"));}
                if(!game.Player.Hidden&&hide&&(rehideAt<0||Time.realtimeSinceStartup>=rehideAt||!quiet))
                {
                    if(c.Dread<60)ForceDread(Mathf.Max(60,HoldDread),"siege-held re-entry at door-held Dread");
                    if(game.Player.TorchOn)yield return Torch(false);
                    int before=c.DoorsHeld;yield return Use(hide,"re-hide");attempts++;
                    if(c.DoorsHeld>before){held++;rehideAt=Time.realtimeSinceStartup+3;Debug.Log("HC_PLAY_SIEGE_HELD door held attempt="+attempts);}
                    else rehideAt=-1;
                }
                yield return null;
            }
            yield return Step("siege_held_done","hiddenHunts="+hiddenHunts+" hiddenSurvived="+hiddenSurvived+" hideAttempts="+attempts+" doorsHeld="+c.DoorsHeld+" nearMissBegin="+Tag("HC_COUNT_NEARMISS begin")+" nearMissOpen="+Tag("HC_COUNT_NEARMISS open=True")+" blocked="+c.BlockedDeaths);
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
            var hall=game.World.Castle.RuneRingCenter;TeleportPlayer(new Vector3(hall.x,.05f,hall.z-4));
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
        bool GrabBusy=>game&&game.Creature&&(game.Creature.GrabCamActive||game.Creature.GrabInProgress);
        // fd-grab: every driver teleport goes through here; one made during a grab presentation is logged (it contaminates the grab's
        // continuity measure; the natural loop waits for the grab instead).
        int grabTeleports;
        void TeleportPlayer(Vector3 to)
        {
            if(GrabBusy){grabTeleports++;Debug.Log("HC_PLAY_TELEPORT during_grab=True n="+grabTeleports+" to="+to+" mode="+Mode);}
            game.Player.Teleport(to);
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
                // fd-grab (r13 natural1 #2: the 20 s teleport moved her mid-grab): never while a grab is presented; the stay clock
                // pauses through a grab too.
                if(GrabBusy)Debug.Log("HC_PLAY natural teleport_wait reason=grab under way");
                while(GrabBusy)yield return null;
                if(NavMesh.SamplePosition(p,out var hit,3,NavMesh.AllAreas))TeleportPlayer(hit.position+Vector3.up*.05f);
                float stay=Time.realtimeSinceStartup+20;
                while(Time.realtimeSinceStartup<stay||GrabBusy)
                {
                    if(GrabBusy){stay+=Time.unscaledDeltaTime;yield return null;continue;}
                    input.Look=game.Player.View.transform.position+Quaternion.Euler(0,Time.realtimeSinceStartup*12,0)*Vector3.forward;yield return null;
                }
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
            var hall=game.World.Castle.RuneRingCenter;TeleportPlayer(new Vector3(hall.x,.05f,hall.z-3));yield return Torch(true);
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
        // huntcam: a NATURALLY triggered hunt seen from the player's eye (no dice or Dread forcing, no invulnerability). Two records,
        // then she stands in the great hall, torch on, facing him; from the first frame of his dive, 10 screenshots ~0.2 s apart
        // through the dive and the grab presentation. For the blind critic (pitch, close time, grab mode, claw size).
        IEnumerator HuntCam()
        {
            var c=game.Case;c.Invulnerable=false;
            yield return Opening(true);
            var hall=game.World.Castle.RuneRingCenter;TeleportPlayer(new Vector3(hall.x,.05f,hall.z-3));yield return Torch(true);
            float end=Time.realtimeSinceStartup+420;
            while(!game.Creature.Diving&&c.Phase!=Phase.Dead&&Time.realtimeSinceStartup<end)
            {
                if(c.Phase==Phase.Hunt||c.Phase==Phase.Return)input.Look=game.Creature.HeadPosition;
                yield return null;
            }
            if(!game.Creature.Diving&&c.Phase!=Phase.Dead){Blocker("no natural dive within 420 s (hunts="+c.Hunts+")");yield return Finish();yield break;}
            Debug.Log("HC_PLAY huntcam dive_start t="+(Time.realtimeSinceStartup-start).ToString("F1")+" threatFrom="+c.ThreatFrom+" noTell="+c.NoTellHunt+" hunts="+c.Hunts+" "+game.Creature.DiagnosticState);
            float diveStart=Time.realtimeSinceStartup;
            for(int i=0;i<10;i++)
            {
                if(c.Phase!=Phase.Dead)input.Look=game.Creature.HeadPosition;
                float d=Vector3.Distance(game.Creature.transform.position,game.Player.transform.position);
                yield return Shot("hunt_natural_"+(i+1).ToString("00")+"_d"+d.ToString("F1")+"m_"+c.Phase);
                Debug.Log("HC_PLAY huntcam frame="+(i+1)+" t_since_dive="+(Time.realtimeSinceStartup-diveStart).ToString("F2")+" distance="+d.ToString("F2")+" phase="+c.Phase+" mode="+(game.Creature.Actor?game.Creature.Actor.Mode:-1));
                float next=Time.realtimeSinceStartup+.2f;while(Time.realtimeSinceStartup<next)yield return null;
            }
            yield return Wait(2);
            Debug.Log("HC_PLAY_RESULT mode=huntcam won=False dead="+(c.Phase==Phase.Dead)+" hunts="+c.Hunts+" blockers="+blockers.Count);
            yield return Wait(1);Application.Quit();
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
            var c=game.Case;TeleportPlayer(game.World.Spawn+Vector3.zero);
            // Stand in the open on the hall floor with the torch on, wait until he sees her, then hide in the nearest cupboard.
            // Stand at the exit of the hall-floor hide nearest the rune ring with the torch on, wait until he sees her,
            // then switch the torch OFF and hide while he watches (critic r10 #16: the open must not depend on the torch).
            var hall=game.World.Castle.RuneRingCenter;
            Interactable near=null;float best=99;
            foreach(var h in game.World.HidingPlaces){if(!h||!h.gameObject.activeInHierarchy||Mathf.Abs(h.HidePoint.y)>1)continue;float d=Vector3.Distance(h.HidePoint,hall);if(d<best){best=d;near=h;}}
            if(near)TeleportPlayer(near.ExitPoint);else TeleportPlayer(new Vector3(hall.x,.05f,hall.z-4));
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
            TeleportPlayer(new Vector3(0,.1f,1.2f));input.Look=new Vector3(0,1.6f,-20);
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
            input.Move=new Vector2(0,1);float walk=Time.realtimeSinceStartup+14,closest=99;int blocked=c.BlockedDeaths;float nextLog=0;bool returnKill=false;
            while(Time.realtimeSinceStartup<walk&&c.Phase!=Phase.Won)
            {
                var at=game.Creature.transform.position;closest=Mathf.Min(closest,Vector3.Distance(at,game.Player.transform.position));
                // Wave L2: a grab the driver's invulnerability blocked on the hostile Return is the run's outcome for a mortal player.
                // Log it, release the grab and put her on the far bank at once, so the run never stalls (death/huntcam stay mortal).
                if(c.BlockedDeaths>blocked&&!returnKill)
                {
                    returnKill=true;
                    // fd-grab (r13 natural3 #2: 4 frames, not judged): the grab presentation plays 1.3 s before the driver lets go.
                    float held=Time.realtimeSinceStartup;while(game.Creature.GrabCamActive&&Time.realtimeSinceStartup-held<1.3f)yield return null;
                    Debug.Log("HC_RETURN_KILL would_die=true phase="+c.Phase+" huntActive="+c.HuntActive+" player="+game.Player.transform.position+" count="+at+" dist="+Vector3.Distance(at,game.Player.transform.position).ToString("F2"));
                    game.Creature.ReleaseGrab("driver return kill");Reset();TeleportPlayer(mid-Vector3.up*1.0f);yield return Frames(5);continue;
                }
                if(Time.realtimeSinceStartup>nextLog){nextLog=Time.realtimeSinceStartup+.5f;Debug.Log("HC_PLAY_BRIDGE player="+game.Player.transform.position+" count="+at+" huntActive="+c.HuntActive+" dist="+Vector3.Distance(at,game.Player.transform.position).ToString("F1"));}
                yield return null;
            }
            Reset();
            yield return Step("bridge_crossed","won="+(c.Phase==Phase.Won)+" closest="+closest.ToString("F2")+" grabsOnBridge="+(c.BlockedDeaths-blocked)+" far="+mid);
            if(returnKill)Debug.Log("HC_PLAY_RETURN_KILL won="+(c.Phase==Phase.Won)+" (a mortal player died on the bridge)");
            if(c.Phase!=Phase.Won){Blocker("bridge walk did not reach the far bank (at "+game.Player.transform.position+")");TeleportPlayer(mid-Vector3.up*1.0f);yield return Frames(5);}
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
                    TeleportPlayer(stand+Vector3.up*.05f);
                }
                yield return null;
            }
            if(!castle.Fight.Dead)Blocker("fight not finished in 300 s (hits "+castle.Fight.Hits+", bell rings "+rings+")");
            yield return Wait(2.5f);
        }
        // ---- doorproof (fd-rules, r13 critic "door swing unproven") ---------------------------------------------------------------------
        // The Hall great doors (both leaves) and two interior doors (Dining hall door, Vestibule hall door), from EACH side: stand 3.2 m out,
        // walk up to 1.5 m on foot (TestInput), look at the leaf, press E through the interaction ray like a player. Door logs HC_DOOR_STATE
        // name= side= swing= angle= by=player; 0.5 s after the press a frame is saved and the leaf's real side is measured (its probe centre in the
        // closed hinge frame) against hers: HC_DOORPROOF name= side= swing=away|toward angle= leafSide= playerSide= focus=. Then E again closes
        // it. Ends with HC_DOORPROOF toward=<n> toggles=<m> PASS|FAIL (PASS = every toggle away, every leaf focused by the ray).
        static readonly string[] ProofDoors={"Hall great doors","Dining hall door","Vestibule hall door"};
        IEnumerator DoorProof()
        {
            var c=game.Case;int toward=0,toggles=0,unfocused=0;
            // Driver choice (logged): the Late first-hunt band (160-200 s), so the ~70 s proof runs before the first hunt and the Count does not
            // open these doors himself mid-proof.
            c.UseRunPlan(new RunPlan{Band=FirstHuntBand.Late});Debug.Log("HC_DOORPROOF plan=Late (driver) t="+c.Elapsed.ToString("F1"));
            var leaves=new List<(Door door,Interactable panel)>();
            foreach(var d in game.World.Doors)
            {
                if(!d)continue;var it=d.GetComponentInChildren<Interactable>(true);
                if(it&&it.Kind==InteractKind.Door&&Array.IndexOf(ProofDoors,it.Label)>=0)leaves.Add((d,it));
            }
            Debug.Log("HC_DOORPROOF begin leaves="+leaves.Count+" doors="+string.Join(",",leaves.ConvertAll(l=>l.door.name)));
            if(leaves.Count<4)Blocker("doorproof: expected 4 leaves (hall x2, dining, vestibule), found "+leaves.Count);
            yield return Torch(true);
            foreach(var (door,panel) in leaves)
            foreach(int side in new[]{-1,1})
            {
                if(door.Open){door.Toggle();yield return Wait(1.2f);}
                var closed=(door.transform.parent?door.transform.parent.rotation:Quaternion.identity)*door.Closed;
                var col=panel.GetComponent<Collider>();var centre=col?col.bounds.center:panel.transform.position;
                var normal=closed*Vector3.forward;normal.y=0;normal.Normalize();
                var floor=new Vector3(centre.x,door.transform.position.y,centre.z);
                string tag=door.name.Replace(' ','_').Replace('(','_').Replace(')','_').Replace('/','_')+"_"+(side<0?"m":"p")+"z";   // fd-integ: door names hold a '/' ("Great door / ..."): it made a sub-path and the shot threw
                // Stand 3.2 m out on this side (NavMesh), then walk up to the door on foot.
                // fd-integ: the hall piers stand beside the dining and vestibule doors, so the stand point straight out could put a pier between
                // her and the leaf (focus=none). Take the first of straight out / 0.8 m / 1.4 m either side whose eye line to the leaf is clear.
                var across=Vector3.Cross(Vector3.up,normal);NavMeshHit fh=default;bool stand=false;
                foreach(float lateral in new[]{0f,.8f,-.8f,1.4f,-1.4f})
                {
                    var far=floor+normal*side*3.2f+across*lateral;
                    if(!NavMesh.SamplePosition(far,out var h,1.2f,NavMesh.AllAreas)||Mathf.Sign(door.SideOf(h.position))!=side)continue;
                    var eye=h.position+Vector3.up*1.6f;var hits=Physics.RaycastAll(eye,centre-eye,Vector3.Distance(eye,centre)-.15f,~0,QueryTriggerInteraction.Ignore);
                    bool clear=true;foreach(var hit in hits)if(!hit.collider.transform.IsChildOf(door.transform)&&!hit.collider.GetComponentInParent<Investigator>()){clear=false;break;}
                    if(!stand||clear){fh=h;stand=true;}
                    if(clear){if(lateral!=0)Debug.Log("HC_DOORPROOF stand name=\""+door.name+"\" lateral="+lateral.ToString("F1")+" (straight-out line blocked)");break;}
                }
                if(!stand){Blocker("doorproof "+door.name+" side "+side+": no walkable stand point");continue;}
                game.Player.Teleport(fh.position+Vector3.up*.05f);input.Look=centre;yield return Frames(4);
                // fd-integ: a leaf can re-open behind the driver's back between the sides (by=auto while she still stood on the other side), so
                // close it again here, on this side, before she walks up; an auto-open from now on is hers.
                if(door.Open){door.Toggle();Debug.Log("HC_DOORPROOF reclose name=\""+door.name+"\" before side "+(side<0?"-z":"+z"));yield return Wait(1.2f);}
                float walkEnd=Time.realtimeSinceStartup+5;
                while(Time.realtimeSinceStartup<walkEnd)
                {
                    var p=game.Player.transform.position;var flat=floor-p;flat.y=0;
                    if(flat.magnitude<=1.5f)break;
                    input.Look=centre;input.Move=new Vector2(0,1);yield return null;
                }
                input.Move=Vector2.zero;input.Look=centre;yield return Frames(6);
                var player=game.Player.transform.position;int playerSide=door.SideOf(player)<0?-1:1;
                // fd-integ: castle doors also open by themselves when she walks into the leaf (Door logs by=auto with the same swing rule);
                // the first doorproof run then closed that open leaf with E and reported "E did not open it". An auto-open counts as the
                // opening (by=auto in the line below), it is not a focus failure.
                bool autoOpened=door.Open;
                bool focused=autoOpened||game.Player.Focus==panel;
                if(!focused)
                {
                    // Aim again: this leaf's half a little lower, then the collider point nearest the eye, then a hand-width either side of it.
                    var eye=game.Player.View.transform.position;var near=col?col.ClosestPoint(eye):centre;
                    foreach(var aim in new[]{centre+Vector3.down*.4f,near,near+across*.3f,near-across*.3f,near+Vector3.down*.3f})
                    {input.Look=aim;yield return Frames(6);if(game.Player.Focus==panel){focused=true;break;}}
                }
                if(autoOpened){}
                else if(focused)yield return Press(i=>i.Use=true);
                else{unfocused++;Debug.Log("HC_DOORPROOF focus_fail name=\""+door.name+"\" focus="+(game.Player.Focus?game.Player.Focus.name:"none"));door.Toggle();}
                float pressed=Time.realtimeSinceStartup;
                if(!door.Open){Blocker("doorproof "+door.name+": E did not open it");continue;}
                toggles++;
                while(Time.realtimeSinceStartup-pressed<.5f)yield return null;
                int leafSide=door.LeafSideNow();bool away=leafSide!=0&&leafSide!=playerSide;
                if(!away)toward++;
                Debug.Log("HC_DOORPROOF name=\""+door.name+"\" side="+(playerSide<0?"-z":"+z")+" swing="+(away?"away":"toward")+" angle="+door.OpenAngle.ToString("F0")+" chosen="+(door.LastSwingAway?"away":"toward")
                    +" leafSide="+leafSide+" playerSide="+playerSide+" focus="+(autoOpened?"auto":focused.ToString())+" by="+door.LastOpener+" leafRot="+door.transform.localEulerAngles.y.ToString("F0")+" player="+player.ToString("F1")+" t="+c.Elapsed.ToString("F0"));
                yield return Shot("doorproof_"+tag+"_open05");
                yield return Wait(1f);
                // Close it again with E, like a player (the ray may now miss the swung leaf: then Toggle, logged).
                if(game.Player.Focus==panel)yield return Press(i=>i.Use=true);else{door.Toggle();Debug.Log("HC_DOORPROOF close=direct name=\""+door.name+"\"");}
                yield return Wait(1.2f);
            }
            input.Move=Vector2.zero;
            bool pass=toward==0&&toggles>=8&&unfocused==0;
            Debug.Log("HC_DOORPROOF toward="+toward+" toggles="+toggles+" unfocused="+unfocused+" leaves="+leaves.Count+" "+(pass?"PASS":"FAIL"));
            if(!pass)Blocker("doorproof: toward="+toward+" toggles="+toggles+" unfocused="+unfocused);
            yield return Finish();
        }
        // ---- cold: a first-time player (fb-onboard) ------------------------------------------------------------------------------
        static readonly string[] ColdHides={"Hall armoire","Dining armoire","Kitchen larder","Confessional","Vestibule press"};
        // fe-integ: the castle pause settings card on all three pages (fe-ui; HC_DOC_LAYOUT screen=settings). Paused castle play is the only place
        // the castle card is drawn (the -hc-ui-only route runs in Bellwether). Frames NN_settings_p0..p2; no input, no gameplay claim.
        IEnumerator SettingsPages()
        {
            var ui=game.GetComponent<Interface>();
            for(int p=0;p<3;p++)
            {
                if(ui)ui.ShowSettingsPage(p);else{game.Paused=true;game.ApplyFocus();}
                yield return Wait(1.2f);yield return Step("settings_p"+p);
            }
            game.Resume();yield return Wait(.5f);
            Debug.Log("HC_PLAY_RESULT mode="+Mode+" won=False slain=False blockedDeaths=0 blockers="+(ui?0:1)+(ui?"":" [no Interface]"));
            yield return Wait(1);Application.Quit();
        }
        IEnumerator Cold()
        {
            var c=game.Case;float realStart=Time.realtimeSinceStartup;bool briefing=false;
            yield return Step("cold_spawn","phase="+c.Phase+" equipped="+c.Equipped+" plan="+(game.RunPlan!=null?game.RunPlan.Band.ToString():"none"));
            // The rack is never touched: the case must already be out of Briefing.
            IEnumerator Idle(float seconds,Vector3 look)
            {
                float end=Time.realtimeSinceStartup+seconds;
                while(Time.realtimeSinceStartup<end){if(Time.realtimeSinceStartup-realStart>1&&c.Phase==Phase.Briefing)briefing=true;input.Look=look;yield return null;}
            }
            var eye0=game.Player.View.transform.position;
            yield return Idle(3,eye0+game.Player.transform.forward*4);  // reads the arrival card
            yield return Use(Find(InteractKind.Note,game.Book.ArrivalNoteTitle),"arrival letter");
            yield return Step("cold_letter_read","objective="+(game.Onboarding?game.Onboarding.Step:"none"));
            // Walk the great hall on foot (the Investigator's own movement through TestInput), along nav paths, for 180 s of case time.
            var route=new[]{new Vector3(0,.05f,7.5f),new Vector3(0,.05f,12.5f),new Vector3(0,.05f,17.5f),new Vector3(0,.05f,23f),new Vector3(2.6f,.05f,20f),new Vector3(-2.6f,.05f,24.5f),new Vector3(-2.6f,.05f,16f),new Vector3(2.4f,.05f,15.5f),new Vector3(0,.05f,10f)};
            int leg=0,unstick=0;float lastShot=c.Elapsed;
            while(c.Elapsed<180&&!c.Terminal)
            {
                var goal=route[leg++%route.Length];
                var path=new NavMeshPath();var from=game.Player.transform.position;
                Vector3[] corners=NavMesh.SamplePosition(goal,out var gh,1.5f,NavMesh.AllAreas)&&NavMesh.CalculatePath(from,gh.position,NavMesh.AllAreas,path)&&path.corners.Length>0?path.corners:new[]{goal};
                foreach(var corner in corners)
                {
                    float legEnd=Time.realtimeSinceStartup+20,stuckClock=0;var last=game.Player.transform.position;
                    while(c.Elapsed<180&&!c.Terminal&&Time.realtimeSinceStartup<legEnd)
                    {
                        if(Time.realtimeSinceStartup-realStart>1&&c.Phase==Phase.Briefing)briefing=true;
                        if(game.Modal){input.Move=Vector2.zero;yield return null;continue;}
                        var p=game.Player.transform.position;var flat=corner-p;flat.y=0;
                        if(flat.magnitude<.45f)break;
                        input.Look=game.Player.View.transform.position+flat.normalized*4;input.Move=new Vector2(0,1);
                        stuckClock+=Time.deltaTime;
                        if(stuckClock>2.5f)
                        {
                            if(Vector3.Distance(p,last)<.4f)
                            {
                                // A closed door in the way: press E on it like a player would, then (if still stuck) step past.
                                if(game.Player.Focus&&game.Player.Focus.Kind==InteractKind.Door){input.Move=Vector2.zero;yield return Press(i=>i.Use=true);Debug.Log("HC_PLAY cold_door_opened "+game.Player.Focus.name);}
                                else{unstick++;TeleportPlayer(corner+Vector3.up*.05f);Debug.Log("HC_PLAY cold_unstick n="+unstick+" at="+p.ToString("F1")+" to="+corner.ToString("F1"));}
                            }
                            stuckClock=0;last=game.Player.transform.position;
                        }
                        if(c.Elapsed-lastShot>=30){lastShot=c.Elapsed;input.Move=Vector2.zero;yield return Step("cold_walk_t"+c.Elapsed.ToString("F0"),"phase="+c.Phase+" cues="+(game.Onboarding?game.Onboarding.Cues:0));}
                        yield return null;
                    }
                }
                input.Move=Vector2.zero;
            }
            input.Move=Vector2.zero;
            var onboard=game.Onboarding;
            yield return Step("cold_walk_done",onboard?onboard.SummaryFields():"no onboarding");
            // Every ground-floor hide answers E: she hides, or a notice says why (held door, nailed shut).
            int answered=0,hidden=0;
            foreach(var name in ColdHides)
            {
                Interactable spot=null;
                foreach(var item in game.World.Root.GetComponentsInChildren<Interactable>(true))if((item.Kind==InteractKind.Hide||item.Kind==InteractKind.Nailed)&&(item.name==name||item.name=="Nailed shut / "+name)&&item.gameObject.activeInHierarchy){spot=item;break;}
                if(!spot){Blocker("cold hide "+name+": no active interactable");continue;}
                yield return WaitFor(()=>!game.Modal,30);
                float noticeBefore=game.NoticeTime;string noticeText=game.NoticeText;
                yield return Use(spot,"cold hide "+name);
                yield return Frames(3);
                bool hid=game.Player.Hidden,noticed=game.NoticeTime>0&&(game.NoticeText!=noticeText||game.NoticeTime>noticeBefore);
                if(hid||noticed)answered++;if(hid)hidden++;
                Debug.Log("HC_PLAY_COLD_HIDE name=\""+name+"\" hidden="+hid+" notice="+(noticed?"\""+game.NoticeText+"\"":"none")+" focus="+(game.Player.Focus?game.Player.Focus.name:"none")+" phase="+c.Phase);
                if(hid){yield return Shot("cold_hidden_"+name.Replace(' ','_'));yield return Press(i=>i.Use=true);yield return Frames(3);}
                if(!hid&&!noticed)Blocker("cold hide "+name+": E did nothing (no hide, no notice)");
            }
            int cues=onboard?onboard.Cues:0;float first=onboard?onboard.FirstCueAt:-1;
            if(cues<4)Blocker("cold: only "+cues+" watched cues in 180 s (need >= 4)");
            if(first<0||first>25)Blocker("cold: first watched cue at "+(first<0?"none":first.ToString("F1"))+" s (need <= 25)");
            if(briefing||onboard&&onboard.BriefingAfterOneSecond)Blocker("cold: the case was in Briefing after 1 s");
            if(c.Equipped&&onboard&&onboard.FirstObjectiveChangeAt<0)Blocker("cold: the objective never changed (letter read?)");
            Debug.Log("HC_PLAY_COLD watched="+cues+" firstCue="+(first<0?"none":first.ToString("F1"))+" briefingAfter1s="+briefing+" hidesAnswered="+answered+"/"+ColdHides.Length+" hidden="+hidden
                +" unstick="+unstick+" pass="+(cues>=4&&first>=0&&first<=25&&!briefing&&answered==ColdHides.Length)+" "+(onboard?onboard.SummaryFields():""));
            yield return Finish();
        }
        IEnumerator Finish()
        {
            yield return Wait(1.5f);
            var c=game.Case;
            yield return Step(c.Phase==Phase.Won?(c.HunterSlain?"won_kill":"won_escape"):"not_won","goal="+c.Goal);
            if(Mode!="escape"&&Mode!="kill"){var sb=new System.Text.StringBuilder();foreach(var t in Tags)sb.Append(t.Trim().Replace(' ','_')).Append('=').Append(Tag(t)).Append(' ');Debug.Log("HC_PLAY_SUMMARY "+sb+"dread="+c.Dread.ToString("F1")+" hunts="+c.Hunts);}
            Debug.Log("HC_PLAY_STATS mode="+Mode+" "+Stats());
            PrintGrabSummary();
            Debug.Log("HC_PLAY_RESULT mode="+Mode+" won="+(c.Phase==Phase.Won)+" slain="+c.HunterSlain+" blockedDeaths="+c.BlockedDeaths+" blockers="+blockers.Count+(blockers.Count>0?" ["+string.Join(" | ",blockers)+"]":""));
            yield return Wait(1);Application.Quit();
        }
    }
}
