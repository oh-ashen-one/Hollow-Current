using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HollowCurrent
{
    // fb-onboard (owner feedback 2026-10-15, items 6, 7 and 12). Hari played the castle for five minutes: "not sure at all what I'm
    // supposed to be doing ... nothing scary has happened ... no sense of being watched". Root cause: the case waited in Briefing for the
    // equipment rack, so nothing ran (Game now equips at spawn). This component is the castle's opening on top of that:
    //   1. the watched layer: low-cost presence cues from spawn (first <= 25 s, then every 20-45 s until the first apparition, 30-50 s
    //      after it) for the run's first OpeningSeconds: a far door slams, steps behind her, breath or a whisper at the ear, portrait eyes
    //      turn to her, a figure in a far doorway that is gone when looked at, the torch stutters, a candle cluster gutters. Every cue is
    //      consequence-free (no Dread, no rule input) and logs "HC_WATCHED kind=<k> t=<case seconds>".
    //   2. the objective log (HC_OBJECTIVE on every step change of Interface's castle objective) and an idle nudge after 60 s on one
    //      step (HC_NUDGE, Game.Notice).
    //   3. the hide-spot audit: HC_HIDE_SPOTS per hide (collider bounds, whether the interaction ray from 1.2 m in front hits it first).
    //   4. HC_ONBOARD: time to the first cue, cues before the first hunt, time to the first objective change (60/180/300 s, the first hunt,
    //      quit), for the integrator's natural runs and PlaytestDriver's cold mode.
    // Nothing here reads Investigation rules beyond the phase (and only where a `tell-safe:` comment says why), Elapsed, Hunts and
    // Apparitions (the run's own counters). Bellwether never has this component.
    public sealed class CastleOnboarding : MonoBehaviour
    {
        Game game;
        public void Initialize(Game owner){game=owner;ResetBudgets();}

        // ---- metrics (HC_ONBOARD) --------------------------------------------------------------------------------------------------
        public float FirstCueAt {get;private set;}=-1;
        public int Cues {get;private set;}
        public int CuesBeforeFirstHunt {get;private set;}
        public float FirstHuntAt {get;private set;}=-1;
        public float FirstObjectiveChangeAt {get;private set;}=-1;
        public int ObjectiveChanges {get;private set;}
        public int Nudges {get;private set;}
        public bool BriefingAfterOneSecond {get;private set;}
        public string Step=>step;
        public int HideSpotsAudited {get;private set;}
        public int HideSpotsFocusable {get;private set;}
        readonly Dictionary<string,int> kindCounts=new Dictionary<string,int>();
        float startReal=-1;int lastHunts=-1;bool logged60,logged180,logged300;

        // ---- the watched layer: cadence and per-kind budgets -------------------------------------------------------------------------
        public const float OpeningSeconds=300,FirstCueMin=6,FirstCueMax=20,GapMin=20,GapMax=45,LateGapMin=30,LateGapMax=50;
        // kind => (max uses per run, case seconds between two uses of the same kind). Never the same kind twice in a row.
        static readonly Dictionary<string,int[]> WatchedTable=new Dictionary<string,int[]>
        {
            {"door_slam",new[]{4,60}},{"footsteps_behind",new[]{3,90}},{"ear_breath",new[]{3,60}},{"ear_whisper",new[]{2,90}},
            {"portrait_eyes",new[]{3,60}},{"doorway_figure",new[]{3,60}},{"lantern_flicker",new[]{3,60}},{"candle_gutter",new[]{2,90}},
        };
        static readonly string[] Kinds={"door_slam","footsteps_behind","ear_breath","ear_whisper","portrait_eyes","doorway_figure","lantern_flicker","candle_gutter"};
        static readonly float[] KindWeights={1.3f,1.1f,1f,.7f,.9f,1.2f,.6f,.5f};
        // The first cue of the run is one that says "something is here" from anywhere, the gatehouse included.
        static readonly string[] FirstKinds={"door_slam","footsteps_behind","ear_breath","doorway_figure"};
        readonly Dictionary<string,int> used=new Dictionary<string,int>();
        readonly Dictionary<string,float> lastUse=new Dictionary<string,float>();
        string lastKind="";
        float nextCue=-1;
        const int OcclusionMask=~((1<<2)|(1<<30));
        void ResetBudgets(){used.Clear();lastUse.Clear();lastKind="";nextCue=-1;}
        bool Can(string kind)
        {
            if(kind==lastKind)return false; // never the same kind twice in a row
            if(!WatchedTable.TryGetValue(kind,out var b))return false;
            used.TryGetValue(kind,out int n);if(n>=b[0])return false;
            return !lastUse.TryGetValue(kind,out float at)||game.Case.Elapsed-at>=b[1];
        }
        void Fire(string kind,string detail)
        {
            used.TryGetValue(kind,out int n);used[kind]=n+1;lastUse[kind]=game.Case.Elapsed;lastKind=kind;
            kindCounts.TryGetValue(kind,out int k);kindCounts[kind]=k+1;
            Cues++;if(game.Case.Hunts==0)CuesBeforeFirstHunt++;
            if(FirstCueAt<0)FirstCueAt=game.Case.Elapsed;
            Debug.Log("HC_WATCHED kind="+kind+" t="+game.Case.Elapsed.ToString("F1")+" n="+Cues+" budget="+(n+1)+"/"+WatchedTable[kind][0]+" apparitions="+game.Case.Apparitions+" hunts="+game.Case.Hunts+(detail.Length>0?" "+detail:""));
        }
        Transform Eye=>game.Player.View.transform;
        bool Ready=>game&&game.Player&&game.Player.View&&game.World!=null&&game.World.Root&&game.Case!=null;
        // A cue may start now: the run is live and on screen, she is not hidden (the hidden layer has its own clock) and no grab is
        // being presented. tell-safe: Briefing and the endings have no run; no other phase is read here.
        bool CanCue=>Ready&&game.Started&&!game.Modal&&!game.Player.Hidden&&!game.Case.Terminal&&game.Case.Phase!=Phase.Briefing
            &&!(game.Creature&&(game.Creature.GrabCamActive||game.Creature.GrabInProgress));

        void Start()
        {
            if(!Ready)return;
            var hides=new List<Interactable>();
            foreach(var item in game.World.Root.GetComponentsInChildren<Interactable>(false))if(item.Kind==InteractKind.Hide||item.Kind==InteractKind.Nailed)hides.Add(item);
            AuditHides(hides,true,"runtime",out int audited,out int focusable);HideSpotsAudited=audited;HideSpotsFocusable=focusable;
            // The equipment rack is a prop now (Game equips at spawn): its prompt says so, and E on it gives a notice (Interactable).
            foreach(var item in game.World.Root.GetComponentsInChildren<Interactable>(true))if(item.Kind==InteractKind.Kit)item.Label="Equipment rack · empty, you carry the instruments";
            Debug.Log("HC_ONBOARD_START equipped="+game.Case.Equipped+" phase="+game.Case.Phase+" plan="+(game.RunPlan!=null?game.RunPlan.Band.ToString():"none")+" hides="+focusable+"/"+audited);
        }

        void Update()
        {
            if(!Ready||!game.Started)return;
            var c=game.Case;
            if(startReal<0)startReal=Time.realtimeSinceStartup;
            // tell-safe: the onboarding proof only (a castle run must never wait in Briefing); logged, never a scare input.
            if(!BriefingAfterOneSecond&&Time.realtimeSinceStartup-startReal>1&&c.Phase==Phase.Briefing){BriefingAfterOneSecond=true;Debug.Log("HC_ONBOARD_BRIEFING_STUCK t_real="+(Time.realtimeSinceStartup-startReal).ToString("F1"));}
            if(lastHunts<0)lastHunts=c.Hunts; // a checkpoint restore starts with its saved hunts
            if(c.Hunts>0&&lastHunts==0){FirstHuntAt=c.Elapsed;LogSummary("first_hunt");}
            lastHunts=c.Hunts;
            TickWatched(c);
            TickObjective(c);
            if(!logged60&&c.Elapsed>=60){logged60=true;LogSummary("t60");}
            if(!logged180&&c.Elapsed>=180){logged180=true;LogSummary("t180");}
            if(!logged300&&c.Elapsed>=OpeningSeconds){logged300=true;LogSummary("t300");}
        }
        void OnApplicationQuit(){if(Ready&&game.Started)LogSummary("quit");}

        void TickWatched(Investigation c)
        {
            float t=c.Elapsed;
            if(t>=OpeningSeconds)return;
            if(nextCue<0){nextCue=t+Random.Range(FirstCueMin,FirstCueMax);Debug.Log("HC_WATCHED_CLOCK first_at="+nextCue.ToString("F1"));}
            if(t<nextCue)return;
            if(!CanCue){nextCue=t+1.5f;return;}
            bool fired=WatchedCue();
            nextCue=t+(fired?(c.Apparitions>0?Random.Range(LateGapMin,LateGapMax):Random.Range(GapMin,GapMax)):3f);
            if(!fired)Debug.Log("HC_WATCHED_SKIP t="+t.ToString("F1")+" reason=no_cue_possible retry_in=3");
        }
        // One cue: the first of the run from FirstKinds, then a weighted draw (refused kinds fall through to the next).
        public bool WatchedCue()
        {
            if(!CanCue)return false;
            var pool=new List<int>();var weights=new List<float>();
            for(int i=0;i<Kinds.Length;i++)
            {
                if(Cues==0&&System.Array.IndexOf(FirstKinds,Kinds[i])<0)continue;
                if(!Can(Kinds[i]))continue;
                float recency=lastUse.TryGetValue(Kinds[i],out float at)?Mathf.Clamp((game.Case.Elapsed-at)/180f,.2f,1f):1.25f;
                pool.Add(i);weights.Add(KindWeights[i]*recency);
            }
            while(pool.Count>0)
            {
                float sum=0;foreach(float w in weights)sum+=w;
                float r=Random.value*sum,acc=0;int pick=pool.Count-1;
                for(int k=0;k<pool.Count;k++){acc+=weights[k];if(r<=acc){pick=k;break;}}
                if(TryKind(Kinds[pool[pick]]))return true;
                pool.RemoveAt(pick);weights.RemoveAt(pick);
            }
            // The first cue must land: if no "something is here" kind fits, the breath at the ear always can.
            return Cues==0&&EarBreath(true);
        }
        bool TryKind(string kind)
        {
            switch(kind)
            {
                case "door_slam":return DoorSlam();
                case "footsteps_behind":return FootstepsBehind();
                case "ear_breath":return EarBreath();
                case "ear_whisper":return EarWhisper();
                case "portrait_eyes":return PortraitStare();
                case "doorway_figure":return DoorwayFigure();
                case "lantern_flicker":return LanternFlicker();
                default:return CandleGutter();
            }
        }

        // ---- a door slams somewhere out of sight, 7-16 m away -----------------------------------------------------------------------
        public bool DoorSlam()
        {
            if(!CanCue||!Can("door_slam"))return false;
            var eye=Eye.position;var player=game.Player.transform.position;var root=game.World.Root;
            Vector3 best=default;float bestScore=-1;bool found=false,occluded=false;
            foreach(var d in CastleLayout.Doors)
            {
                if(d.Kind==CastleDoorKind.Gate)continue;
                var p=root.TransformPoint(d.Position)+Vector3.up*1.1f;
                var flat=p-player;flat.y=0;float dist=flat.magnitude;
                if(dist<7||dist>16||Mathf.Abs(p.y-(player.y+1.1f))>4)continue;
                bool hidden=Physics.Linecast(eye,p,OcclusionMask,QueryTriggerInteraction.Ignore);
                float score=(hidden?10:0)+Random.value*4;
                if(score>bestScore){bestScore=score;best=p;found=true;occluded=hidden;}
            }
            if(!found)return false;
            StartCoroutine(Slam(best));
            Fire("door_slam","at="+best.ToString("F1")+" distance="+Vector3.Distance(best,player).ToString("F1")+" occluded="+occluded);
            return true;
        }
        IEnumerator Slam(Vector3 at)
        {
            game.Sound.Cue("door",at,.7f);
            yield return new WaitForSeconds(Random.Range(.35f,.6f));
            if(!Ready)yield break;
            game.Sound.Cue("latch",at,1f);game.Sound.Cue("knock",at,.75f);
        }

        // ---- claw steps behind her (CastleScares' decoy footsteps: 3-5 m behind, no wall between, each step farther away) -------------
        public bool FootstepsBehind()
        {
            if(!CanCue||!Can("footsteps_behind")||!game.CastleDread)return false;
            if(!game.CastleDread.FootstepsBehind(true))return false;
            Fire("footsteps_behind","via=castle_scares");
            return true;
        }

        // ---- breath or a whisper just behind her head ------------------------------------------------------------------------------
        public bool EarBreath(bool firstFallback=false)
        {
            if(!CanCue||!Can("ear_breath")&&!firstFallback)return false;
            float side=Random.value<.5f?-1:1;
            string clip=game.Sound.Has("castle_hide_breath_0")&&game.Sound.Has("castle_hide_breath_1")?"castle_hide_breath_"+Random.Range(0,2):"breath_presence";
            StartCoroutine(Breathe(clip,side));
            Fire("ear_breath","clip="+clip+" side="+(side<0?"left":"right")+(firstFallback?" fallback=first_cue":""));
            return true;
        }
        IEnumerator Breathe(string clip,float side)
        {
            var view=Eye;game.Sound.Cue(clip,view.position-view.forward*.45f+view.right*(.3f*side),.42f);
            yield return new WaitForSeconds(Random.Range(1.6f,2.4f));
            if(!Ready)yield break;
            view=Eye;game.Sound.Cue(clip,view.position-view.forward*.5f+view.right*(.3f*side),.3f);
        }
        public bool EarWhisper()
        {
            if(!CanCue||!Can("ear_whisper"))return false;
            var view=Eye;float side=Random.value<.5f?-1:1;
            var at=view.position-view.forward*.7f+view.right*(.45f*side);
            string clip=game.Sound.Has("castle_hide_whisper")?"castle_hide_whisper":"breath_presence";
            game.Sound.Cue(clip,at,.32f);
            Fire("ear_whisper","clip="+clip+" side="+(side<0?"left":"right"));
            return true;
        }

        // ---- a portrait's eyes turn to her and hold ----------------------------------------------------------------------------------
        PortraitEyes[] portraits;
        public bool PortraitStare()
        {
            if(!CanCue||!Can("portrait_eyes"))return false;
            if(portraits==null)portraits=FindObjectsByType<PortraitEyes>(FindObjectsSortMode.None);
            var view=Eye;PortraitEyes best=null;float bestDistance=float.MaxValue;
            foreach(var p in portraits)
            {
                if(!p||!p.isActiveAndEnabled)continue;
                var face=p.transform.position+p.transform.forward*.08f;var delta=face-view.position;float d=delta.magnitude;
                if(d<2.5f||d>12||Vector3.Dot(p.transform.forward,-delta)<=0)continue; // the painting faces her
                if(Vector3.Angle(view.forward,delta)>75)continue;                    // in view or the near periphery
                if(Physics.Linecast(view.position,face,OcclusionMask,QueryTriggerInteraction.Ignore))continue;
                if(d<bestDistance){bestDistance=d;best=p;}
            }
            if(!best)return false;
            StartCoroutine(Stare(best));
            Fire("portrait_eyes","portrait="+best.name+" distance="+bestDistance.ToString("F1"));
            return true;
        }
        IEnumerator Stare(PortraitEyes eyes)
        {
            float speed=eyes.followSpeed,offset=eyes.maxOffset,range=eyes.trackRange;
            eyes.followSpeed=30;eyes.maxOffset=offset*2.2f;eyes.trackRange=Mathf.Max(range,16);
            int creak=Random.Range(0,3);game.Sound.Cue(creak==0?"creak_0":creak==1?"creak_1":"creak_2",eyes.transform.position,.28f);
            yield return new WaitForSeconds(Random.Range(6f,9f));
            if(!eyes)yield break;
            eyes.followSpeed=speed;eyes.maxOffset=offset;eyes.trackRange=range;
        }

        // ---- a dark figure in a far doorway, gone when she looks at it ---------------------------------------------------------------
        GameObject figure;Material figureMaterial;Vector3 figureChest;float figureAge,figureFade;
        public bool DoorwayFigure()
        {
            if(!CanCue||figure||!Can("doorway_figure"))return false;
            // tell-safe: withheld in Hunt only, because a figure then reads as the Count himself (CastleScares withholds its window
            // silhouette for the same reason); the draw falls through to another kind, so the cadence is the same in every phase.
            if(game.Case.Phase==Phase.Hunt)return false;
            var view=Eye;var player=game.Player.transform.position;var root=game.World.Root;
            Vector3 feet=default;float bestScore=-1;bool found=false;
            foreach(var d in CastleLayout.Doors)
            {
                if(d.Kind==CastleDoorKind.Gate)continue;
                var door=root.TransformPoint(d.Position);
                if(Mathf.Abs(door.y-player.y)>.8f)continue;
                var away=door-player;away.y=0;float dist=away.magnitude;
                if(dist<6||dist>18)continue;
                var stand=door+away.normalized*.35f;var chest=stand+Vector3.up*1.35f;var delta=chest-view.position;
                float angle=Vector3.Angle(view.forward,delta);
                if(angle<18||angle>60)continue;                                        // the periphery, never dead ahead
                if(Physics.Linecast(view.position,chest,OcclusionMask,QueryTriggerInteraction.Ignore))continue; // an open doorway only
                float score=dist+Random.value*4;
                if(score>bestScore){bestScore=score;feet=stand;found=true;}
            }
            if(!found)return false;
            figureChest=feet+Vector3.up*1.35f;figureAge=0;figureFade=0;
            figure=new GameObject("Watched / doorway figure");
            figure.transform.position=feet+Vector3.up*1.1f;figure.transform.localScale=new Vector3(.85f,2.2f,1);
            figure.AddComponent<MeshFilter>().sharedMesh=SurrealProps.CentreQuad;
            var renderer=figure.AddComponent<MeshRenderer>();
            figureMaterial=SurrealProps.Unlit("Doorway figure",CastleScares.FigureTexture,new Color(0,0,0,0));
            renderer.sharedMaterial=figureMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            FaceFigure();
            Fire("doorway_figure","at="+feet.ToString("F1")+" distance="+Vector3.Distance(feet,player).ToString("F1"));
            return true;
        }
        void FaceFigure()
        {
            var flat=Vector3.ProjectOnPlane(figureChest-Eye.position,Vector3.up);
            if(flat.sqrMagnitude>1e-4f)figure.transform.rotation=Quaternion.LookRotation(flat,Vector3.up);
        }
        void LateUpdate()
        {
            if(!figure)return;
            if(!Ready){KillFigure(false);return;}
            if(game.Modal)return;
            figureAge+=Time.deltaTime;
            float angle=Vector3.Angle(Eye.forward,figureChest-Eye.position);
            if(angle<10){KillFigure(true);return;}  // looked at: gone
            if(figureAge>7){KillFigure(false);return;}
            FaceFigure();
            figureFade=Mathf.MoveTowards(figureFade,1,Time.deltaTime/.6f);
            figureMaterial.color=new Color(0,0,0,figureFade*.9f);
        }
        void KillFigure(bool lookedAt)
        {
            if(figure)Destroy(figure);
            if(figureMaterial)Destroy(figureMaterial);
            figure=null;figureMaterial=null;
            Debug.Log("HC_WATCHED_GONE kind=doorway_figure looked_at="+lookedAt+" after="+figureAge.ToString("F1"));
        }
        void OnDestroy(){if(figure)Destroy(figure);if(figureMaterial)Destroy(figureMaterial);}

        // ---- the torch stutters (three drop-outs in under a second; not the dive's torch failure, which is HC_TORCH_FAIL) -------------
        public bool LanternFlicker()
        {
            if(!CanCue||!Can("lantern_flicker")||!game.Scares||!game.Player.TorchOn||!game.Case.Equipped||game.Scares.TorchFailed)return false;
            StartCoroutine(Stutter());
            Fire("lantern_flicker","");
            return true;
        }
        IEnumerator Stutter()
        {
            foreach(float off in new[]{.08f,.06f,.14f})
            {
                if(!Ready||!game.Scares)yield break;
                game.Scares.Blackout(off);
                yield return new WaitForSeconds(off+Random.Range(.1f,.22f));
            }
        }

        // ---- a candle cluster near her gutters (CastleScares' beat, its own budget too) -----------------------------------------------
        public bool CandleGutter()
        {
            if(!CanCue||!Can("candle_gutter")||!game.CastleDread)return false;
            if(!game.CastleDread.CandleGutter())return false;
            Fire("candle_gutter","via=castle_scares");
            return true;
        }

        // ---- the objective log and the idle nudge ------------------------------------------------------------------------------------
        public const float NudgeAfter=60,NudgeRepeat=90;public const int NudgesPerStep=3;
        string step;float nextNudge=-1,objectivePoll;int nudgesThisStep;
        void TickObjective(Investigation c)
        {
            objectivePoll-=Time.unscaledDeltaTime;if(objectivePoll>0)return;objectivePoll=.25f;
            var ui=game.GetComponent<Interface>();if(!ui)return;
            var o=ui.Objective();
            if(step==null){step=o.Step;nextNudge=c.Elapsed+NudgeAfter;Debug.Log("HC_OBJECTIVE t="+c.Elapsed.ToString("F1")+" step="+o.Step+" initial=true text=\""+o.Text+"\"");return;}
            if(o.Step!=step)
            {
                ObjectiveChanges++;if(FirstObjectiveChangeAt<0)FirstObjectiveChangeAt=c.Elapsed;
                Debug.Log("HC_OBJECTIVE t="+c.Elapsed.ToString("F1")+" step="+o.Step+" from="+step+" n="+ObjectiveChanges+" text=\""+o.Text+"\" tracker=\""+o.Tracker+"\"");
                step=o.Step;nudgesThisStep=0;nextNudge=c.Elapsed+NudgeAfter;return;
            }
            if(nudgesThisStep>=NudgesPerStep||c.Elapsed<nextNudge)return;
            // tell-safe: a nudge waits for a calm moment (never in a Warning or a hunt, never while hidden); it reads no other rule.
            bool calm=c.Phase==Phase.Survey||c.Phase==Phase.Disturbance||c.Phase==Phase.Recovery;
            if(!calm||game.Modal||game.Player.Hidden){nextNudge=c.Elapsed+5;return;}
            string text=CastleObjective.Nudge(step,c,ui.RelicsFound);
            if(text==null){nudgesThisStep=NudgesPerStep;return;}
            nudgesThisStep++;Nudges++;nextNudge=c.Elapsed+NudgeRepeat;
            game.Notice(text,7);
            Debug.Log("HC_NUDGE t="+c.Elapsed.ToString("F1")+" step="+step+" n="+nudgesThisStep+" text=\""+text+"\"");
        }

        // ---- HC_ONBOARD ---------------------------------------------------------------------------------------------------------------
        public string SummaryFields()
        {
            var c=game.Case;var kinds=new StringBuilder();
            foreach(var k in Kinds)if(kindCounts.TryGetValue(k,out int n))kinds.Append(k).Append(':').Append(n).Append(',');
            return "firstCue="+(FirstCueAt<0?"none":FirstCueAt.ToString("F1"))+" cues="+Cues+" cuesBeforeFirstHunt="+CuesBeforeFirstHunt
                +" firstHunt="+(FirstHuntAt<0?"none":FirstHuntAt.ToString("F1"))+" firstObjectiveChange="+(FirstObjectiveChangeAt<0?"none":FirstObjectiveChangeAt.ToString("F1"))
                +" objectiveChanges="+ObjectiveChanges+" step="+(step??"none")+" nudges="+Nudges+" briefingAfter1s="+BriefingAfterOneSecond
                +" plan="+(game.RunPlan!=null?game.RunPlan.Band.ToString():"none")+" firstHuntDeadline="+c.FirstHuntDeadline.ToString("F1")
                +" hideSpots="+HideSpotsFocusable+"/"+HideSpotsAudited+" kinds=["+kinds.ToString().TrimEnd(',')+"]";
        }
        void LogSummary(string reason){Debug.Log("HC_ONBOARD reason="+reason+" t="+game.Case.Elapsed.ToString("F1")+" phase="+game.Case.Phase+" "+SummaryFields());}

        // ---- hide spots: the focus trigger and the audit ------------------------------------------------------------------------------
        // The hide's focus trigger (a thin box in the door plane, CastleWorld.Hide) is extended 0.20 m out into the room, so the
        // interaction ray meets it before the solid lower door, a louvre or a nearby prop's collision proxy (it was a tie with the door
        // panel, settled by the 12 cm RaycastAll rule). The back face stays at the door. Triggers do not collide and are not in the
        // nav bake. Idempotent (CastleWorld.Hide applies it at prepare time; Start applies it to scenes prepared before fb-onboard).
        public const float HideFocusProud=.20f;
        public static void ProtrudeHideFocus(Interactable h)
        {
            var box=h?h.GetComponent<BoxCollider>():null;if(!box||box.size.z>=2.5f)return;
            float depth=h.transform.lossyScale.z;if(depth<=1e-4f)return;
            float back=.5f*depth,front=-.5f*depth-HideFocusProud; // metres along local z; local -z is the room side
            box.size=new Vector3(box.size.x,box.size.y,(back-front)/depth);box.center=new Vector3(box.center.x,box.center.y,(back+front)*.5f/depth);
        }
        // HC_HIDE_SPOTS: for each hide (and nailed marker), its collider bounds, the stand point 1.2 m in front of the door face at
        // eye height, and whether Investigator.FocusRay (the ray E uses) from there meets this interactable when aimed at the
        // collider centre, the door at eye level and the lower door. Shared by the runtime (Start) and NativeBuild.ValidateCastleWorld.
        public static void AuditHides(IEnumerable<Interactable> hides,bool fix,string source,out int audited,out int focusable)
        {
            audited=0;focusable=0;var failed=new List<string>();
            if(fix)foreach(var h in hides)if(h&&h.Kind==InteractKind.Hide)ProtrudeHideFocus(h);
            Physics.SyncTransforms();
            foreach(var h in hides)
            {
                if(!h)continue;audited++;
                var col=h.GetComponent<Collider>();var bounds=col?col.bounds:new Bounds(h.transform.position,Vector3.zero);
                var outward=Vector3.ProjectOnPlane(-h.transform.forward,Vector3.up);outward=outward.sqrMagnitude<1e-4f?Vector3.forward:outward.normalized;
                var face=h.transform.position;float floor=face.y-1f;
                var stand=new Vector3(face.x,floor,face.z)+outward*1.2f;var eye=stand+Vector3.up*1.64f;
                bool standClear=!Physics.CheckCapsule(stand+Vector3.up*.4f,stand+Vector3.up*1.5f,.27f,OcclusionMask,QueryTriggerInteraction.Ignore);
                bool Aim(Vector3 target,out string first)
                {
                    var f=Investigator.FocusRay(eye,(target-eye).normalized,out var hit);
                    first=hit.collider?hit.collider.name+"@"+hit.distance.ToString("F2"):"nothing";
                    return f==h;
                }
                bool centre=Aim(bounds.center,out string firstCentre);
                bool atEye=Aim(new Vector3(face.x,floor+1.45f,face.z),out _);
                bool low=Aim(new Vector3(face.x,floor+.7f,face.z),out _);
                bool pass=centre&&atEye;
                if(pass)focusable++;else failed.Add(h.name);
                Debug.Log("HC_HIDE_SPOTS source="+source+" name=\""+h.name+"\" kind="+h.Kind+" center="+bounds.center.ToString("F2")+" size="+bounds.size.ToString("F2")+" face="+face.ToString("F2")
                    +" stand="+stand.ToString("F2")+" standClear="+standClear+" first="+firstCentre+" focusCentre="+centre+" focusEye="+atEye+" focusLow="+low+" pass="+pass);
            }
            Debug.Log("HC_HIDE_SPOTS_SUMMARY source="+source+" audited="+audited+" focusable="+focusable+" failed=["+string.Join(",",failed)+"] proud="+HideFocusProud.ToString("F2"));
        }
    }
}
