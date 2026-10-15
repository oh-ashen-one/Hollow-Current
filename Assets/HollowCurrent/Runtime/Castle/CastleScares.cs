using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace HollowCurrent
{
    // Castle scares (fear plan wave F, piece F3): consequence-free dread beats, peripheral sightings, false tells and the
    // poltergeist throw. Every effect is a plain call on this MonoBehaviour. Nothing here reads Investigation rules beyond
    // Game.Case.Phase (no beat plays during a warning, hunt or recovery). The integrator adds the component in Game for the castle,
    // calls Initialize(this), and decides when each call happens (see HANDOFF.md).
    // Every beat logs "HC_SCARE_CASTLE <beat>". Audio goes through Game.Sound.Cue, which is silent for a missing castle clip.
    public sealed class CastleScares : MonoBehaviour
    {
        // The integrator's compass needle spins while this is true (set for 3 s by FalseCompassReading).
        public bool FalseCompass;
        public int Beats {get;private set;}
        public string LastBeat {get;private set;}
        public int Thrown {get;private set;}
        // Silence is an active beat: the integrator wires this to Soundscape.DuckBed(float seconds,float depth) (ducks wind, room tone and
        // heartbeat to `depth` over 1 s, holds, restores over 3 s): `CastleDread.DuckBed=Sound.DuckBed;` in Game. Null = the beat still logs.
        public System.Action<float,float> DuckBed;
        public bool Silent=>Time.time<silenceUntil;
        // cc_listen is heard at most once per run: the phantom ear-trumpet line sets this, and Soundscape's rare line should skip when it is
        // set and call MarkListenPlayed() when it plays (see the scares2 handoff).
        public bool ListenPlayed {get;private set;}
        public void MarkListenPlayed(){ListenPlayed=true;}
        // Apparition escalation range: ScareDirector.Trigger accepts an apparition closer than this as a hunt candidate (was 4 m).
        public const float EscalateRange=5.5f;
        float silenceUntil,footstepsCooldown;

        const int OcclusionMask=~((1<<2)|(1<<30)); // same occlusion mask as ScareDirector
        Game game;
        float falseCompassTimer;
        Transform bell;
        Quaternion bellRest;
        bool bellSwinging,silhouetteBusy;
        readonly HashSet<CandleFlame> guttering=new HashSet<CandleFlame>();
        static Material silhouetteMaterial;
        static Texture2D wingTexture,figureTexture;

        public void Initialize(Game owner){game=owner;ResetRun();}

        // ---- per-run budgets and cooldowns (r11: bell_single fired 9 times in 18 minutes) -------------------------------------------------
        // Every beat, lie and ambient pick has an entry: at most Max uses per run, at least Gap seconds between two uses of the same beat,
        // and no beat twice in a row. Beat selection is weighted away from recently used beats (see Weight). A refused beat returns false
        // and the caller falls through to the next, so a budget never makes a tick silent by itself.
        sealed class BeatBudget
        {
            public readonly int Max;public readonly float Gap;public int Used;public float Last=-1e9f;
            public BeatBudget(int max,float gap){Max=max;Gap=gap;}
        }
        // key => (max per run, seconds between repeats). The bell is a rare false tell; peripheral and lie beats are capped too.
        static readonly Dictionary<string,int[]> BudgetTable=new Dictionary<string,int[]>
        {
            {"book_drop",new[]{6,90}},{"knife_stick",new[]{4,90}},{"bat_pass",new[]{6,90}},{"bell_single",new[]{2,240}},{"candle_gutter",new[]{6,90}},
            {"silence",new[]{8,90}},{"poltergeist",new[]{3,150}},{"window_silhouette",new[]{8,90}},{"balustrade_shape",new[]{3,180}},
            {"false_candle_tell",new[]{5,90}},{"hide_breath",new[]{8,90}},{"hide_clawstep",new[]{8,90}},{"hide_handle",new[]{8,90}},{"hide_whisper",new[]{8,90}},{"false_compass",new[]{4,120}},{"phantom_listen",new[]{1,100000}},{"footsteps_behind",new[]{6,90}},
        };
        readonly Dictionary<string,BeatBudget> budgets=new Dictionary<string,BeatBudget>();
        string lastKey="",pickNote="direct";
        // Clears every budget (a new run). Initialize calls it.
        public void ResetRun()
        {
            budgets.Clear();
            foreach(var pair in BudgetTable)budgets[pair.Key]=new BeatBudget(pair.Value[0],pair.Value[1]);
            lastKey="";ListenPlayed=false;
        }
        bool Can(string key)
        {
            if(key==lastKey)return false; // never twice in a row
            return !budgets.TryGetValue(key,out var b)||b.Used<b.Max&&Time.time-b.Last>=b.Gap;
        }
        void Spend(string key)
        {
            lastKey=key;
            if(budgets.TryGetValue(key,out var b)){b.Used++;b.Last=Time.time;}
        }
        // "pick=<how> budget=<used>/<max> gap=<s> since=<s since the previous use or ->", the suffix of every beat and lie log line.
        string BudgetText(string key,float since)
        {
            if(!budgets.TryGetValue(key,out var b))return "pick="+pickNote+" budget=none";
            return "pick="+pickNote+" budget="+b.Used+"/"+b.Max+" gap="+b.Gap.ToString("F0")+" since="+(since>1e6f?"-":since.ToString("F0"));
        }
        float SinceLast(string key)=>budgets.TryGetValue(key,out var b)?Time.time-b.Last:0;
        // Relative weight of a beat for the next pick: 0 when refused; never used = 1.25; otherwise it recovers with the time since its last
        // use (240 s to full) and shrinks as its budget is used up.
        float Weight(string key)
        {
            if(!Can(key))return 0;
            if(!budgets.TryGetValue(key,out var b))return 1;
            float recency=b.Used==0?1.25f:Mathf.Clamp((Time.time-b.Last)/240f,.12f,1f);
            return recency*(1-.5f*b.Used/b.Max);
        }
        static readonly string[] BeatKeys={"book_drop","knife_stick","bat_pass","bell_single","candle_gutter"};
        // Draws beat indices (into BeatKeys) without replacement, each by Weight; `from` limits the candidates.
        List<int> WeightedOrder(string[] keys,int[] from,out List<float> weights,out float total)
        {
            var order=new List<int>();weights=new List<float>();var pool=new List<int>(from);var w=new List<float>();total=0;
            foreach(int i in pool){float x=Weight(keys[i]);w.Add(x);total+=x;}
            float firstTotal=total;
            while(pool.Count>0)
            {
                float sum=0;foreach(float x in w)sum+=x;
                if(sum<=0)break;
                float r=Random.value*sum,acc=0;int pick=w.Count-1;
                for(int k=0;k<w.Count;k++){acc+=w[k];if(r<=acc&&w[k]>0){pick=k;break;}}
                order.Add(pool[pick]);weights.Add(w[pick]);pool.RemoveAt(pick);w.RemoveAt(pick);
            }
            total=firstTotal;return order;
        }
        bool Try(int index,string note)
        {
            pickNote=note;
            bool ok=TryBeat(index);
            pickNote="direct";
            return ok;
        }

        bool Ready=>game&&game.Player&&game.Player.View&&game.World!=null&&game.World.Root;
        Transform Root=>game.World.Root;
        Transform Eye=>game.Player.View.transform;
        // Present: the run exists and is not paused. Running effects (a shape, a footstep sequence) only ever stop on this, never on a phase.
        bool Present=>Ready&&!game.Modal;
        // Active: a new beat may start. Hiding and menus defer a beat (the player's own action, not a game state).
        // tell-safe: the only phases excluded are Briefing and Dead/Won, where no hunt can begin or be under way; Warning, Hunt, Recovery,
        // Containment and Return all allow beats, so the scare layer is not silent exactly while a hunt is in progress.
        bool Active=>Present&&!game.Player.Hidden&&game.Case.Phase!=Phase.Briefing&&!game.Case.Terminal;
        // The phases in which ScareDirector.Update returns before its dread tick (Warning, Hunt, Recovery, Containment, Return).
        // tell-safe: this property only ADDS beats (the ambient substitute below) where the director is quiet, so that their absence is not a signal.
        bool DirectorQuiet
        {
            get
            {
                var phase=game.Case.Phase;
                return phase==Phase.Warning||phase==Phase.Hunt||phase==Phase.Recovery||phase==Phase.Containment||phase==Phase.Return;
            }
        }
        void Log(string beat)
        {
            string key=beat.Split(' ')[0];
            float since=SinceLast(key);
            Spend(key);
            LastBeat=key;Beats++;
            Debug.Log("HC_SCARE_CASTLE "+beat+" "+BudgetText(key,since));
        }

        void Update()
        {
            if(falseCompassTimer>0){falseCompassTimer-=Time.deltaTime;if(falseCompassTimer<=0)FalseCompass=false;}
            if(shape)TickShape();
            else if(shapeArmed)TryPlaceShape();
            TickAmbient();
            TrackHunts();
            TickHidden();
        }

        // ---- the hidden player gets beats too (critic r12: 12 minutes in one wardrobe produced 1 scare event) -------------------------------
        // While the player is hidden the ordinary layer is off (Active needs !Hidden), so this runs on its own clock: the first beat 20-45 s
        // after hiding, then one every 40-70 s, each a breath at the slats, a claw step passing the hide, a handle rattle (a lie) or a whisper
        // (a lie), drawn by Weight from the hide_* budgets. The clock and the draw are identical in every phase, so a hidden beat is as likely
        // in a calm stretch as in a hunt and says nothing about either; the one exception is the first 5 s of a hunt, which it never covers.
        // Audio: wanted keys from F4 (castle_hide_*), existing clips as placeholders until they land (HideClips; resolved with Sound.Has).
        static readonly string[] HideKeys={"hide_breath","hide_clawstep","hide_handle","hide_whisper"};
        // pairs of (wanted F4 key, placeholder that exists today); the wanted key is used as soon as its clip is in CastleAudio
        static readonly string[] HideClips={"castle_hide_breath","breath_presence","castle_hide_clawstep","claw_steps_","castle_hide_handle","door_rattle","castle_hide_whisper","breath_presence"};
        float hiddenFor,hiddenNext=-1;
        void TickHidden()
        {
            // tell-safe: Briefing and Dead/Won have no run to hide in; no other phase is read here.
            if(!Present||!game.Player.Hidden||game.Case.Phase==Phase.Briefing||game.Case.Terminal){hiddenFor=0;hiddenNext=-1;return;}
            hiddenFor+=Time.deltaTime;
            if(hiddenNext<0)hiddenNext=Random.Range(20f,45f);
            hiddenNext-=Time.deltaTime;
            if(hiddenNext>0)return;
            // tell-safe: the only phase-dependent rule: a beat that falls due in the first 5 s of a hunt waits until they are over, so it never
            // covers the Count's own entrance. The 40-70 s clock is otherwise the same in every phase.
            if(game.Case.Phase==Phase.Hunt&&game.Case.PhaseTime<5){hiddenNext=5-game.Case.PhaseTime;return;}
            hiddenNext=Random.Range(40f,70f);
            if(!HiddenBeat())hiddenNext=10f; // every hidden budget is spent or cooling: try again soon
        }
        string HideClip(int kind)
        {
            string wanted=HideClips[kind*2],fallback=HideClips[kind*2+1];
            if(fallback.EndsWith("_"))fallback+=Random.Range(0,4);
            return game.Sound.Has(wanted)?wanted:fallback;
        }
        Interactable NearestHide()
        {
            Interactable best=null;float bestDistance=float.MaxValue;var at=game.Player.transform.position;
            foreach(var item in FindObjectsByType<Interactable>(FindObjectsSortMode.None))
            {
                if(item.Kind!=InteractKind.Hide)continue;
                float d=Vector3.Distance(item.HidePoint,at);
                if(d<bestDistance){bestDistance=d;best=item;}
            }
            return best;
        }
        public bool HiddenBeat()
        {
            if(!Present||!game.Player.Hidden)return false;
            var spot=NearestHide();
            if(!spot)return false;
            var door=spot.transform.position;
            var outward=Vector3.ProjectOnPlane(spot.ExitPoint-spot.HidePoint,Vector3.up);
            if(outward.sqrMagnitude<.01f)outward=spot.transform.forward;
            outward.Normalize();
            var order=WeightedOrder(HideKeys,new[]{0,1,2,3},out var weights,out float total);
            for(int i=0;i<order.Count;i++)
            {
                int kind=order[i];string key=HideKeys[kind];
                pickNote="hidden weighted w="+weights[i].ToString("F2")+"/"+total.ToString("F2")+" rank="+(i+1);
                Log(key);
                pickNote="direct";
                Debug.Log("HC_HIDE kind="+key+" hidden_s="+hiddenFor.ToString("F0")+" phase="+game.Case.Phase+" door="+door.ToString("F1")+" clip="+HideClip(kind));
                switch(kind)
                {
                    case 0:StartCoroutine(HideBreath(HideClip(0),door+Vector3.up*.5f,outward));break;
                    case 1:StartCoroutine(HideClaws(door,outward));break;
                    case 2:StartCoroutine(HideHandle(HideClip(2),door));break;
                    default:game.Sound.Cue(HideClip(3),door+outward*.9f+Vector3.up*.6f,.35f);break;
                }
                return true;
            }
            return false;
        }
        IEnumerator HideBreath(string clip,Vector3 at,Vector3 outward)
        {
            game.Sound.Cue(clip,at+outward*.15f,.55f);
            yield return new WaitForSeconds(Random.Range(1.4f,2.2f));
            if(Ready)game.Sound.Cue(clip,at+outward*.15f,.4f);
        }
        // Three to five claw steps passing along the outside of the door, 1.2 m from it, left to right or right to left.
        IEnumerator HideClaws(Vector3 door,Vector3 outward)
        {
            var tangent=Vector3.Cross(Vector3.up,outward).normalized*(Random.value<.5f?-1:1);
            int steps=Random.Range(4,6);
            for(int k=0;k<steps;k++)
            {
                if(!Ready)yield break;
                float along=-1.8f+k*(3.6f/(steps-1));
                game.Sound.Cue(HideClip(1),door+outward*1.2f+tangent*along,.6f);
                yield return new WaitForSeconds(Random.Range(.5f,.7f));
            }
        }
        // The latch rattled once or twice, as if tried and let go.
        IEnumerator HideHandle(string clip,Vector3 door)
        {
            game.Sound.Cue(clip,door+Vector3.up*.2f,.65f);
            if(Random.value<.5f)
            {
                yield return new WaitForSeconds(Random.Range(.35f,.7f));
                if(Ready)game.Sound.Cue(clip,door+Vector3.up*.2f,.5f);
            }
        }

        // ---- escalation history (the "at most 1 in 3 hunts is escalation-born" part of the rule) -------------------------------------------
        readonly List<bool> huntBorn=new List<bool>();
        int trackedHunts;float lastEscalationAt=-999;
        void TrackHunts()
        {
            if(game==null||game.Case==null)return;
            int hunts=game.Case.Hunts;
            if(hunts<trackedHunts)trackedHunts=hunts; // a restored checkpoint
            while(trackedHunts<hunts){huntBorn.Add(Time.time-lastEscalationAt<12f);trackedHunts++;}
        }

        // ---- ambient substitute for the phases where the director is quiet (critic N11, scare-layer silence as a tell) ------------------
        // ScareDirector.Update returns before its dread tick in Warning, Hunt, Recovery, Containment and Return, so without this the whole
        // scare layer would stop for roughly 80 s around every hunt. While the director is quiet this fires a cheap, consequence-free beat
        // on the same cadence (first after 8-30 s, then every 22-38 s): candle gutter, book drop, knife, bat, window silhouette, gallery
        // shape, and footsteps behind when Lies. Not the bell (a summons read), not silence, not the false candle tell or compass (they
        // would fight the real candle tell and compass during a hunt). Logged as HC_AMBIENT with the phase, for the windowed run only.
        float ambientTimer=-1;
        void TickAmbient()
        {
            if(!Present||!DirectorQuiet){ambientTimer=-1;return;}
            if(ambientTimer<0){ambientTimer=Random.Range(8f,30f);return;}
            ambientTimer-=Time.deltaTime;
            if(ambientTimer>0)return;
            ambientTimer=Random.Range(22f,38f);
            if(AmbientBeat())Debug.Log("HC_AMBIENT phase="+game.Case.Phase+" beat="+LastBeat);
            else Debug.Log("HC_AMBIENT phase="+game.Case.Phase+" beat=none");
        }
        public bool AmbientBeat()
        {
            if(!Active)return false;
            float r=Random.value;
            pickNote="ambient";
            bool done=false;
            if(game.Case.Lies&&r<.2f&&FootstepsBehind())done=true;
            // tell-safe: only the window silhouette is withheld in Hunt itself (it reads as the Count); every other ambient beat still fires there.
            else if(r<.4f&&game.Case.Phase!=Phase.Hunt&&WindowSilhouette())done=true;
            else if(r<.55f&&BalustradeShape())done=true;
            pickNote="direct";
            if(done)return true;
            var order=WeightedOrder(BeatKeys,new[]{0,1,2,4},out var weights,out float total);
            for(int i=0;i<order.Count;i++)
                if(Try(order[i],"ambient weighted w="+weights[i].ToString("F2")+"/"+total.ToString("F2")+" rank="+(i+1)))return true;
            return false;
        }

        // ===================================================================================================================
        // Item 15: dread beats (no sting, no card)
        // ===================================================================================================================
        // Picks one castle dread beat. Silence is an active beat (see Silence). Its chance is keyed on FeltDread, what the heartbeat and the
        // journal pulse may show, never on real Dread (critic N11: real Dread would make silence a statistical gauge): a flat 15 % at any felt
        // level so silence also happens when the player feels calm, rising to 40 % at felt 100. A beat that cannot happen here (no book pile,
        // no knife, no bell or candles near the player) falls through to the next, and the last resort is silence. Returns false only when
        // not Active. DreadTick passes allowSilence=false because it has rolled silence already.
        public float SilenceChance
        {
            get
            {
                float felt=game&&game.Case!=null?game.Case.FeltDread:0;
                return .15f+.25f*Mathf.Clamp01((felt-40f)/60f);
            }
        }
        public bool Beat(bool allowSilence=true)
        {
            if(!Active)return false;
            if(allowSilence&&Random.value<SilenceChance&&Can("silence"))
            {
                pickNote="silence_roll chance="+SilenceChance.ToString("F2");var ok=Silence();pickNote="direct";if(ok)return true;
            }
            var order=WeightedOrder(BeatKeys,new[]{0,1,2,3,4},out var weights,out float total);
            for(int i=0;i<order.Count;i++)
                if(Try(order[i],"weighted w="+weights[i].ToString("F2")+"/"+total.ToString("F2")+" rank="+(i+1)))return true;
            pickNote="fallback";var silent=Silence();pickNote="direct";
            return silent;
        }
        bool TryBeat(int index)
        {
            switch(index)
            {
                case 0:return BookDrop();
                case 1:return KnifeStick();
                case 2:return BatPass();
                case 3:return BellToll();
                default:return CandleGutter();
            }
        }

        // ---- silence: the bed drops out for 6-10 s ---------------------------------------------------------------------------------
        // Nothing else happens. The wind, room tone and heartbeat duck to 10 % through Soundscape.DuckBed, so the quiet is audible.
        public bool Silence()
        {
            if(!Ready||!Can("silence"))return false;
            float seconds=Random.Range(6f,10f);
            silenceUntil=Time.time+seconds;
            if(DuckBed!=null)DuckBed(seconds,.1f);
            Log("silence seconds="+seconds.ToString("F1")+" duck="+(DuckBed!=null?"wired":"unwired")+" felt="+game.Case.FeltDread.ToString("F0")+" dread="+game.Case.Dread.ToString("F0"));
            return true;
        }

        // ---- one castle dread tick (the whole of ScareDirector.CastleBeat) ------------------------------------------------------------
        // Order: silence roll, lies (Dread >= LieThreshold, plus a 12 % decoy of footsteps below it), peripheral sightings, then Beat. Returns false only when not Active.
        public bool DreadTick()
        {
            if(!Active)return false;
            if(Random.value<SilenceChance&&Can("silence"))
            {
                pickNote="tick silence_roll chance="+SilenceChance.ToString("F2");var ok=Silence();pickNote="direct";if(ok)return true;
            }
            pickNote="tick";
            bool done=TickChoice();
            pickNote="direct";
            return done||Beat(false);
        }
        bool TickChoice()
        {
            if(game.Case.Lies)
            {
                if(Random.value<1f/3&&FalseCandleTell())return true;
                if(game.Player.Tool==1&&game.Case.Equipped&&Random.value<.5f&&FalseCompassReading())return true;
                if(Random.value<.5f&&Lie())return true;
            }
            else if(Random.value<.12f&&FootstepsBehind(true))return true; // decoy: steps behind also happen below the lie threshold (critic N11)
            float r=Random.value;
            if(r<.2f&&WindowSilhouette())return true;
            if(r<.35f&&BalustradeShape())return true;
            return false;
        }

        // ---- item 5 lies that need no equipment: phantom ear-trumpet and footsteps behind ---------------------------------------------
        // tell-safe: lies obey Active only, with no hunt or phase test, so a hunt neither starts nor stops one (lies are consequence-free).
        bool LieAllowed=>Active;
        // Picks the phantom cc_listen (once per run) or footsteps behind; false when neither can happen now.
        public bool Lie()
        {
            if(!LieAllowed)return false;
            bool listenFirst=Random.value<.4f;
            if(listenFirst)return PhantomListen()||FootstepsBehind();
            return FootstepsBehind()||PhantomListen();
        }
        // The ear-trumpet's whisper without the ear-trumpet: cc_listen from behind and above the player, once per run.
        public bool PhantomListen()
        {
            if(!LieAllowed||ListenPlayed||!game.Case.Lies||!Can("phantom_listen"))return false;
            var view=Eye;var back=Vector3.ProjectOnPlane(view.forward,Vector3.up);if(back.sqrMagnitude<.01f)back=game.Player.transform.forward;back.Normalize();
            var at=view.position-back*2f+Vector3.up*1.5f;
            if(Physics.Raycast(view.position,Vector3.up,out var ceiling,1.6f,OcclusionMask,QueryTriggerInteraction.Ignore))at.y=Mathf.Min(at.y,ceiling.point.y-.2f);
            ListenPlayed=true;
            game.Sound.Speak("cc_listen",at);
            float since=SinceLast("phantom_listen");Spend("phantom_listen");
            Debug.Log("HC_LIE kind=phantom_listen at="+at.ToString("F1")+" dread="+game.Case.Dread.ToString("F0")+" "+BudgetText("phantom_listen",since));
            return true;
        }
        // Four or five claw steps (they always finish: nothing stops them on a phase change) 3-5 m behind the player, each farther away than the last, then nothing there.
        public bool FootstepsBehind(bool decoy=false)
        {
            if(!LieAllowed||Time.time<footstepsCooldown||(!decoy&&!game.Case.Lies)||!Can("footsteps_behind"))return false;
            var player=game.Player.transform.position;var view=Eye;
            var back=-Vector3.ProjectOnPlane(view.forward,Vector3.up);if(back.sqrMagnitude<.01f)back=-game.Player.transform.forward;back.Normalize();
            foreach(float angle in new[]{0f,20f,-20f,40f,-40f})foreach(float distance in new[]{4f,3.4f,4.8f,3f})
            {
                var desired=player+Quaternion.Euler(0,angle,0)*back*distance;
                if(!NavMesh.SamplePosition(desired,out var hit,1,NavMesh.AllAreas)||Mathf.Abs(hit.position.y-player.y)>.6f)continue;
                var flat=hit.position-player;flat.y=0;
                if(flat.magnitude<3f||flat.magnitude>5f)continue;
                if(Physics.Linecast(player+Vector3.up,hit.position+Vector3.up,OcclusionMask,QueryTriggerInteraction.Ignore))continue; // no wall between
                footstepsCooldown=Time.time+60;
                StartCoroutine(Footsteps(hit.position,flat.normalized));
                float since=SinceLast("footsteps_behind");Spend("footsteps_behind");
                Debug.Log("HC_LIE kind=footsteps_behind at="+hit.position.ToString("F1")+" distance="+flat.magnitude.ToString("F1")+" decoy="+decoy+" dread="+game.Case.Dread.ToString("F0")+" "+BudgetText("footsteps_behind",since));
                return true;
            }
            return false;
        }
        IEnumerator Footsteps(Vector3 start,Vector3 away)
        {
            int index=Random.Range(0,4);
            for(int step=0;step<5;step++)
            {
                if(!Ready)yield break; // only the run ending stops it; a hunt, warning or return never does (critic N11b)
                var at=start+away*(.6f*step);
                if(NavMesh.SamplePosition(at,out var hit,.8f,NavMesh.AllAreas))at=hit.position;
                game.Sound.Cue("claw_steps_"+index,at,.55f);
                index=(index+1+Random.Range(0,3))%4;
                yield return new WaitForSeconds(Random.Range(.5f,.7f));
            }
        }

        // ---- escalation: an apparition within range tries to become a hunt (critic r11: 0 of 8) ---------------------------------------------
        // ScareDirector.Trigger calls this instead of Investigation.TryEscalate. Root cause of 8/8 failures: TryEscalate needs Phase ==
        // Disturbance and GraceRemaining == 0, and the director now lands apparitions 20-40 s after a Recovery starts, i.e. inside the 30-90 s
        // post-hunt grace (and sometimes still in Recovery); the rest is the 20 % roll (0.8^8 = 17 % even with every gate open). The log
        // carried no reason. This logs one: reason=ok|roll|grace|phase_<p>|range|rules_off|terminal. When the attempt is blocked by design
        // (grace, Recovery, Survey) its roll is spent on a payoff instead (50 %): a poltergeist throw, else a candle gutter, else a bat.
        public bool Escalate(float near)
        {
            var c=game.Case;float chance=c.Rules.EventEscalationChance;
            string reason,substitute="none";bool escalated=false;
            if(chance<=0)reason="rules_off";
            else if(c.Terminal)reason="terminal";
            else if(near>=EscalateRange)reason="range";
            // tell-safe: this mirrors the hunt-conversion rule itself (Disturbance and no post-hunt grace, as Investigation.TryEscalate); it gates
            // whether a hunt can start, not a scare effect, and a blocked attempt still pays off below, so the apparition is never bare.
            else if(c.Phase==Phase.Disturbance&&c.GraceRemaining<=0)
            {
                if(!RuleAllows(out var why))reason="rule_"+why;
                else
                {
                    escalated=c.TryEscalate();
                    reason=escalated?"ok":"roll";
                    if(escalated)lastEscalationAt=Time.time;
                }
            }
            else
            {
                // tell-safe: only names the blocking reason for the log.
                reason=c.Phase!=Phase.Disturbance?"phase_"+c.Phase:"grace";
                // tell-safe: the substitute fires in whatever phase the apparition came in, so a blocked escalation is not visible as a missing effect.
                if(Active&&Random.value<.5f)
                {
                    pickNote="escalation_payoff";
                    if(Poltergeist()>0)substitute="poltergeist";
                    else if(CandleGutter())substitute="candle_gutter";
                    else if(BatPass())substitute="bat_pass";
                    pickNote="direct";
                }
            }
            Debug.Log("HC_ESCALATE near="+near.ToString("F1")+" escalated="+escalated+" reason="+reason+" phase="+c.Phase+" grace="+c.GraceRemaining.ToString("F0")+" chance="+chance.ToString("F2")+" substitute="+substitute);
            return escalated;
        }

        // The escalation rule (r12: 5 of 7 non-Return hunts began within 5 s of an apparition). Local equivalent of F1's pure
        // HuntRules.ShouldEscalate from hc/rules-l; when that lands, replace the body of this method with one call to it:
        //   never on the first apparition; about 15 % with the torch lit, 25 % dark; no escalation-born hunt among the last two hunts
        //   (so at most one hunt in three is escalation-born). Investigation.TryEscalate still rolls Rules.EventEscalationChance itself, so
        //   this gate passes with probability target / EventEscalationChance and the product is the target.
        bool RuleAllows(out string why)
        {
            int apparition=game.Scares?game.Scares.Events:0;
            if(apparition<=1){why="first_apparition";return false;}
            int n=huntBorn.Count;
            if(n>=1&&huntBorn[n-1]||n>=2&&huntBorn[n-2]){why="recent_escalation_born";return false;}
            float target=game.Player.TorchOn?.15f:.25f,rules=game.Case.Rules.EventEscalationChance;
            float pass=rules>0?Mathf.Min(1,target/rules):0;
            if(Random.value>=pass){why="roll_p="+pass.ToString("F2");return false;}
            why="ok";return true;
        }

        // ---- the apparition point (fear plan 3, critic #27) ------------------------------------------------------------------------
        // The castle's version of ScareDirector.VisiblePoint: the same tests (nav floor, clear torso and head lines, body clearance, inside the
        // 44 degree cone), but 3.4 m is tried first and 4.6 m replaces 4.8, so an apparition lands inside EscalateRange in open halls.
        public bool ApparitionPoint(out Vector3 point)
        {
            point=Vector3.zero;
            if(!Ready)return false;
            var view=Eye;var forward=Vector3.ProjectOnPlane(view.forward,Vector3.up).normalized;
            if(forward.sqrMagnitude<.1f)forward=game.Player.transform.forward;
            var player=game.Player.transform.position;
            foreach(float angle in new[]{24f,-24f,0f,38f,-38f})foreach(float distance in new[]{3.4f,4.6f,6f})
            {
                var desired=player+Quaternion.Euler(0,angle,0)*forward*distance;
                if(!NavMesh.SamplePosition(desired,out var hit,1,NavMesh.AllAreas)||Mathf.Abs(hit.position.y-player.y)>.6f)continue;
                var candidate=hit.position;var eye=candidate+Vector3.up*1.65f;
                if(Vector3.Distance(candidate,player)<2.8f||Vector3.Angle(view.forward,eye-view.position)>44)continue;
                if(Physics.Linecast(view.position,eye,OcclusionMask,QueryTriggerInteraction.Ignore)||Physics.Linecast(view.position,candidate+Vector3.up*.8f,OcclusionMask,QueryTriggerInteraction.Ignore))continue;
                if(Physics.CheckCapsule(candidate+Vector3.up*.4f,candidate+Vector3.up*1.7f,.28f,OcclusionMask,QueryTriggerInteraction.Ignore))continue;
                point=candidate;return true;
            }
            return false;
        }

        // ---- a book drops from the nearest floating book pile ------------------------------------------------------------------
        public bool BookDrop()
        {
            if(!Ready||!Can("book_drop"))return false;
            var player=game.Player.transform.position;
            Transform book=null;Floater owner=null;Transform pile=null;float best=14f*14f;
            foreach(var floater in Floater.All)
            {
                if(!floater)continue;
                bool isPile=floater.name.StartsWith("Floating book stack");
                bool isBook=floater.name.StartsWith("Castle / FloatingBook");
                if(!isPile&&!isBook)continue;
                if(isPile&&floater.transform.childCount==0)continue;
                var p=floater.transform.position;
                if(Mathf.Abs(p.y-player.y)>5)continue;
                var flat=p-player;flat.y=0;
                if(flat.sqrMagnitude>=best)continue;
                best=flat.sqrMagnitude;owner=floater;pile=isPile?floater.transform:null;book=isPile?null:floater.transform;
            }
            if(!owner)return false;
            if(pile)book=pile.GetChild(pile.childCount-1); // the top book of the pile
            else owner.enabled=false;                       // a lone floating book simply stops floating
            DropBook(book);
            Log("book_drop");return true;
        }
        void DropBook(Transform book)
        {
            book.SetParent(Root,true);
            var size=CastleLayout.ArtSize(CastleLayout.ArtNames.FloatingBook);
            var collider=book.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,size.y*.5f,0);collider.size=size;
            Physics.IgnoreCollision(collider,game.Player.Controller);
            var body=book.gameObject.AddComponent<Rigidbody>();body.mass=.6f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.interpolation=RigidbodyInterpolation.Interpolate;
            body.linearVelocity=Vector3.down*.4f;body.AddTorque(new Vector3(Random.Range(-2f,2f),Random.Range(-3f,3f),Random.Range(-2f,2f)),ForceMode.VelocityChange);
            var cue=book.gameObject.AddComponent<CueOnImpact>();cue.Sound=game.Sound;cue.Key="book_drop";cue.Volume=.9f;
        }

        // ---- one knife leaves the kitchen orbit and sticks in the block ----------------------------------------------------------
        KnifeOrbit orbit;
        public bool KnifeStick()
        {
            if(!Ready||!Can("knife_stick"))return false;
            if(!orbit)orbit=FindFirstObjectByType<KnifeOrbit>();
            if(!orbit||orbit.ActiveKnives==0)return false;
            var player=game.Player.transform.position;var centre=orbit.transform.position;
            var flat=centre-player;flat.y=0;
            if(flat.magnitude>18||Mathf.Abs(centre.y-player.y)>4)return false;
            var knife=orbit.ReleaseKnife(player);
            if(!knife)return false;
            // The block top is 0.58 m below the orbit centre; the blade stands in it at about 63 degrees, tip down.
            var local=new Vector3(Random.Range(-.1f,.5f),-.58f+.23f,Random.Range(-.3f,.3f));
            var target=orbit.transform.TransformPoint(local);
            var heading=Random.insideUnitCircle.normalized;
            var direction=(Vector3.down+new Vector3(heading.x,0,heading.y)*.5f).normalized;
            StartCoroutine(KnifeFlight(knife,target,Quaternion.LookRotation(direction,Vector3.up)));
            Log("knife_stick");return true;
        }
        IEnumerator KnifeFlight(Transform knife,Vector3 target,Quaternion stuck)
        {
            var from=knife.position;var fromRotation=knife.rotation;
            const float flight=.5f;
            for(float t=0;t<flight;t+=Time.deltaTime)
            {
                if(!knife)yield break;
                float k=t/flight;k*=k; // accelerates into the block
                knife.position=Vector3.Lerp(from,target,k);knife.rotation=Quaternion.Slerp(fromRotation,stuck,Mathf.Clamp01(t/(flight*.6f)));
                yield return null;
            }
            if(!knife)yield break;
            knife.position=target;knife.rotation=stuck;
            game.Sound.Cue("knife_stick",target,.9f);
            for(float t=0;t<.9f;t+=Time.deltaTime)
            {
                if(!knife)yield break;
                knife.rotation=stuck*Quaternion.Euler(Mathf.Sin(t*55)*7f*(1-t/.9f),0,0); // the blade quivers and settles
                yield return null;
            }
            if(knife)
            {
                knife.rotation=stuck;
                var marker=knife.gameObject.AddComponent<Throwable>(); // a stuck kitchen knife can be thrown later
                if(!marker.BoundsValid)Destroy(marker);
            }
        }

        // ---- a giant bat crosses the torch beam three metres ahead -----------------------------------------------------------------
        public bool BatPass()
        {
            if(!Ready||!Can("bat_pass"))return false;
            var eye=Eye;var forward=Vector3.ProjectOnPlane(eye.forward,Vector3.up);
            if(forward.sqrMagnitude<.05f)forward=game.Player.transform.forward;
            forward.Normalize();
            var right=Vector3.Cross(Vector3.up,forward);
            float distance=3f;
            var centre=eye.position+forward*distance+Vector3.up*.05f;
            if(Physics.Linecast(eye.position,centre,OcclusionMask,QueryTriggerInteraction.Ignore)){distance=2f;centre=eye.position+forward*distance+Vector3.up*.05f;}
            if(Physics.Linecast(eye.position,centre,OcclusionMask,QueryTriggerInteraction.Ignore))return false;
            float side=Random.value<.5f?-1:1;
            var start=centre-right*3.4f*side;var end=centre+right*3.4f*side;
            if(Physics.Linecast(start,end,OcclusionMask,QueryTriggerInteraction.Ignore))return false;
            var bat=new GameObject("Castle bat pass");
            bat.transform.position=start;
            bat.AddComponent<MeshFilter>().sharedMesh=SurrealProps.CentreQuad;
            var renderer=bat.AddComponent<MeshRenderer>();renderer.sharedMaterial=SurrealProps.BatMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            var crossing=bat.AddComponent<CrossingBat>();crossing.Begin(eye,start,end,1.15f,1.5f);
            game.Sound.Cue("bat_pass",centre,.9f);
            Log("bat_pass");return true;
        }

        // ---- the chapel bell tolls once by itself (a false tell: this is not the summons) ---------------------------------------
        public bool BellToll()
        {
            if(!Ready||!Can("bell_single"))return false;
            Vector3 anchor;
            var spots=game.World.Castle;
            if(spots!=null)anchor=spots.Anchor;
            else{FindBell();if(!bell)return false;anchor=bell.position;}
            var delta=anchor-game.Player.transform.position;
            if(delta.magnitude>34)return false;
            game.Sound.Cue("bell_single",anchor+Vector3.up*1.2f,1f);
            FindBell();
            if(bell&&!bellSwinging)StartCoroutine(SwingBell());
            Log("bell_single");return true; // deliberately does not call CastleRun.RingBell: ringing it is the summons
        }
        void FindBell()
        {
            if(bell)return;
            foreach(var t in Root.GetComponentsInChildren<Transform>(true))if(t.name=="Castle / ChapelBell"){bell=t;bellRest=t.localRotation;break;}
        }
        IEnumerator SwingBell()
        {
            bellSwinging=true;
            for(float t=0;t<2.6f;t+=Time.deltaTime)
            {
                if(!bell)break;
                bell.localRotation=bellRest*Quaternion.Euler(Mathf.Sin(t*6.5f)*2.4f*Mathf.Exp(-t*1.3f),0,0);
                yield return null;
            }
            if(bell)bell.localRotation=bellRest;
            bellSwinging=false;
        }

        // ---- a candle cluster near the player gutters ----------------------------------------------------------------------------
        public bool CandleGutter()
        {
            if(!Ready||!Can("candle_gutter"))return false;
            var player=game.Player.transform.position;
            CandleFlame seed=null;float best=7f*7f;
            foreach(var flame in CandleFlame.All)
            {
                if(!flame||!flame.isActiveAndEnabled||guttering.Contains(flame))continue;
                var d=flame.transform.position-player;
                if(Mathf.Abs(d.y)>3.5f)continue;
                d.y=0;
                if(d.sqrMagnitude<best){best=d.sqrMagnitude;seed=flame;}
            }
            if(!seed)return false;
            var cluster=new List<CandleFlame>();
            foreach(var flame in CandleFlame.All)
            {
                if(cluster.Count>=14)break;
                if(flame&&flame.isActiveAndEnabled&&!guttering.Contains(flame)&&(flame.transform.position-seed.transform.position).sqrMagnitude<2.5f*2.5f)cluster.Add(flame);
            }
            foreach(var flame in cluster)guttering.Add(flame);
            StartCoroutine(Gutter(cluster));
            Log("candle_gutter");return true;
        }
        IEnumerator Gutter(List<CandleFlame> cluster)
        {
            var rest=new Vector3[cluster.Count];
            for(int i=0;i<rest.Length;i++)rest[i]=cluster[i]?cluster[i].transform.localScale:Vector3.one;
            const float shrink=.25f,hold=2f,regrow=.6f;
            for(float t=0;t<shrink+hold+regrow;t+=Time.deltaTime)
            {
                float k=t<shrink?Mathf.Lerp(1,.2f,t/shrink):t<shrink+hold?.2f:Mathf.Lerp(.2f,1,(t-shrink-hold)/regrow);
                for(int i=0;i<rest.Length;i++)if(cluster[i])cluster[i].transform.localScale=rest[i]*k;
                yield return null;
            }
            for(int i=0;i<rest.Length;i++)
            {
                if(cluster[i])cluster[i].transform.localScale=rest[i];
                guttering.Remove(cluster[i]);
            }
        }

        // ===================================================================================================================
        // Item 8: poltergeist
        // ===================================================================================================================
        // Throws every Throwable within 6 m of the player that is in view (65 degrees, clear line) at once, away from the player in a
        // fan. Returns how many left their place. The integrator calls this one apparition in six instead of ScareDirector.ThrowNearbyProp.
        public int Poltergeist()
        {
            if(!Ready||game.Modal){Debug.Log("HC_THROW_SKIP reason=not_ready source=poltergeist ready="+Ready+" modal="+(Ready&&game.Modal));return 0;}
            if(!Can("poltergeist")){Debug.Log("HC_THROW_SKIP reason=budget source=poltergeist "+BudgetText("poltergeist",SinceLast("poltergeist")));return 0;}
            var eye=Eye;var player=game.Player.transform.position;
            var candidates=new List<Throwable>(Throwable.All);
            int thrown=0;Vector3 sum=Vector3.zero;var skips=new Dictionary<string,int>();
            foreach(var item in candidates)
            {
                if(!item)continue;
                if(item.Thrown||item.GetComponent<Rigidbody>()){Throwable.LogSkip("poltergeist","thrown",item.transform,-1,0,null,skips);continue;}
                var centre=item.WorldCenter;var delta=centre-eye.position;float distance=delta.magnitude,angle=Vector3.Angle(eye.forward,delta);
                if(distance>6){Throwable.LogSkip("poltergeist","far",item.transform,distance,angle,null,skips);continue;}
                if(angle>65){Throwable.LogSkip("poltergeist","view_angle",item.transform,distance,angle,null,skips);continue;}
                // A table's collision proxy that encloses the prop does not count as cover (see Throwable.SightBlocked).
                if(Throwable.SightBlocked(eye.position,centre,out var blocker)){Throwable.LogSkip("poltergeist","sight_line",item.transform,distance,angle,blocker,skips);continue;}
                if(item.StaticBatched){Throwable.LogSkip("poltergeist","static_batched",item.transform,distance,angle,"castle_scene_needs_PrepareCastle",skips);continue;}
                var away=centre-player;away.y=0;
                away=away.sqrMagnitude<.01f?Random.onUnitSphere:away.normalized;
                away=Quaternion.Euler(0,Random.Range(-70f,70f),0)*away;away.y=0;
                var velocity=away.normalized*Random.Range(2.2f,4.6f)+Vector3.up*Random.Range(1.8f,3.2f);
                if(item.Launch(game,velocity)){thrown++;sum+=centre;}
                else Throwable.LogSkip("poltergeist","launch_refused",item.transform,distance,angle,null,skips);
            }
            if(thrown>0)
            {
                Thrown+=thrown;
                game.Sound.Cue("knock",sum/thrown,.8f);
                Log("poltergeist "+thrown);
            }
            else Throwable.LogSkipSummary("poltergeist",skips,candidates.Count);
            return thrown;
        }

        // ===================================================================================================================
        // Item 9: visual beats with no consequence
        // ===================================================================================================================
        // A flat dark winged silhouette glides across the outside of a window for 1.8 s, chosen from the windows the player sees in the
        // periphery (25 to 70 degrees off the view centre, clear line, within 18 m). False when no window qualifies.
        public bool WindowSilhouette()
        {
            if(!Active||silhouetteBusy||!Can("window_silhouette"))return false;
            var eye=Eye;var root=Root;
            var options=new List<CastleWindowDef>();
            foreach(var w in CastleLayout.Windows)
            {
                WindowPose(w,out var centre,out var inward);
                var delta=centre-eye.position;
                if(delta.magnitude>18||delta.magnitude<2.5f)continue;
                if(Vector3.Dot(eye.position-centre,inward)<.2f)continue; // the player must be on the room side
                float angle=Vector3.Angle(eye.forward,delta);
                if(angle<25||angle>70)continue;
                if(Physics.Linecast(eye.position,centre+inward*.25f,OcclusionMask,QueryTriggerInteraction.Ignore))continue;
                options.Add(w);
            }
            if(options.Count==0)return false;
            var pick=options[Random.Range(0,options.Count)];
            WindowPose(pick,out var windowCentre,out var windowInward);
            var tangent=root.TransformDirection(pick.RunsAlongX?Vector3.right:Vector3.forward);
            float flip=Random.value<.5f?-1:1;
            float span=pick.Width*.5f+.9f;
            var outside=windowCentre-windowInward*.9f;
            var bat=new GameObject("Window silhouette");
            bat.AddComponent<MeshFilter>().sharedMesh=SurrealProps.CentreQuad;
            var renderer=bat.AddComponent<MeshRenderer>();renderer.sharedMaterial=SilhouetteMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            var crossing=bat.AddComponent<CrossingBat>();
            crossing.flap=false;crossing.wingspan=Mathf.Clamp(pick.Width*1.3f,1.1f,2.2f);
            crossing.Begin(eye,outside-tangent*span*flip,outside+tangent*span*flip,1.8f,crossing.wingspan);
            crossing.OnDone=()=>silhouetteBusy=false;
            silhouetteBusy=true;
            Log("window_silhouette");return true;
        }
        void WindowPose(CastleWindowDef w,out Vector3 centre,out Vector3 inward)
        {
            float cy=w.FloorY+(w.Sill+w.Top)*.5f;
            var local=w.RunsAlongX?new Vector3(w.Center,cy,w.Line):new Vector3(w.Line,cy,w.Center);
            var normal=w.RunsAlongX?new Vector3(0,0,w.Inward):new Vector3(w.Inward,0,0);
            centre=Root.TransformPoint(local);inward=Root.TransformDirection(normal);
        }

        // A dark upright shape stands behind the great-hall gallery balustrade. It shows only while the camera is not pointed within 20
        // degrees of it, and is gone for good the first time the player looks straight at it (or after 40 s, or when the player leaves the
        // hall floor). The call arms it; it appears as soon as a gallery spot sits 28 to 65 degrees off the view centre.
        bool shapeArmed;GameObject shape;float shapeAge,shapeArmedTime,shapeFade;Vector3 shapeChest;Material shapeMaterial;
        public bool BalustradeShape()
        {
            if(!Active||shape||shapeArmed||!OnHallFloor()||!Can("balustrade_shape"))return false;
            shapeArmed=true;shapeArmedTime=0;
            return true;
        }
        bool OnHallFloor()
        {
            var p=Root.InverseTransformPoint(game.Player.transform.position);
            return Mathf.Abs(p.x)<3.2f&&p.z>14.3f&&p.z<27.3f&&p.y>-.3f&&p.y<1f;
        }
        void TryPlaceShape()
        {
            shapeArmedTime+=Time.deltaTime;
            if(shapeArmedTime>25||!Present){shapeArmed=false;return;}
            if(!OnHallFloor())return;
            var eye=Eye;
            Vector3 chosen=default;bool found=false;float bestScore=-1;
            foreach(int side in new[]{-1,1})
                for(float z=15.2f;z<=25.6f;z+=1.6f)
                {
                    var feet=Root.TransformPoint(new Vector3(side*3.45f,3.2f,z));
                    var chest=feet+Vector3.up*1.35f;
                    var delta=chest-eye.position;
                    float angle=Vector3.Angle(eye.forward,delta);
                    if(delta.magnitude<4||delta.magnitude>17||angle<28||angle>65)continue;
                    if(Physics.Linecast(eye.position,feet+Vector3.up*1.9f,OcclusionMask,QueryTriggerInteraction.Ignore))continue;
                    float score=delta.magnitude+Random.value*3;
                    if(score>bestScore){bestScore=score;chosen=feet;found=true;}
                }
            if(!found)return;
            shapeArmed=false;shapeAge=0;shapeFade=0;
            shape=new GameObject("Gallery shape");
            shape.transform.position=chosen+Vector3.up*1.05f;
            shapeChest=chosen+Vector3.up*1.35f;
            shape.AddComponent<MeshFilter>().sharedMesh=SurrealProps.CentreQuad;
            var renderer=shape.AddComponent<MeshRenderer>();
            shapeMaterial=SurrealProps.Unlit("Gallery shape",FigureTexture,new Color(0,0,0,0));
            renderer.sharedMaterial=shapeMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            shape.transform.localScale=new Vector3(.8f,2.1f,1);
            Log("balustrade_shape");
        }
        void TickShape()
        {
            if(!Ready){KillShape(false);return;}
            shapeAge+=Time.deltaTime;
            var eye=Eye;
            if(shapeAge>40||!OnHallFloor()||!Present){KillShape(false);return;}
            var delta=shapeChest-eye.position;
            float angle=Vector3.Angle(eye.forward,delta);
            if(angle<20){KillShape(true);return;}
            // faces the viewer about the vertical axis, so it reads as a flat upright figure
            var flat=Vector3.ProjectOnPlane(delta,Vector3.up);
            if(flat.sqrMagnitude>1e-4f)shape.transform.rotation=Quaternion.LookRotation(flat,Vector3.up);
            shapeFade=Mathf.MoveTowards(shapeFade,1,Time.deltaTime/.7f);
            shapeMaterial.color=new Color(0,0,0,shapeFade*.92f);
        }
        void KillShape(bool lookedAt)
        {
            if(shape)Destroy(shape);
            if(shapeMaterial)Destroy(shapeMaterial);
            shape=null;shapeMaterial=null;shapeArmed=false;
            if(lookedAt)Debug.Log("HC_SCARE_CASTLE balustrade_shape_gone");
        }

        // ===================================================================================================================
        // Item 5: false tells
        // ===================================================================================================================
        // Every candle flame leans toward an empty doorway 6 to 10 m from the player for 4 s, then relaxes.
        public bool FalseCandleTell()
        {
            if(!Ready||game.Modal||!Can("false_candle_tell"))return false;
            var player=game.Player.transform.position;
            var options=new List<Vector3>();
            foreach(var door in CastleLayout.Doors)
            {
                if(door.Kind==CastleDoorKind.Gate)continue;
                var point=Root.TransformPoint(door.Position+Vector3.up*1f);
                var flat=point-player;
                if(Mathf.Abs(flat.y)>2.2f)continue;
                flat.y=0;
                float d=flat.magnitude;
                if(d<6||d>10)continue;
                if(game.Creature&&game.Creature.isActiveAndEnabled&&Vector3.Distance(game.Creature.transform.position,point)<5)continue; // the doorway must be empty
                options.Add(point);
            }
            if(options.Count==0)return false;
            var target=options[Random.Range(0,options.Count)];
            Floater.FalseTarget(target,4f);
            Debug.Log("HC_LIE kind=false_candle at="+target.ToString("F1"));
            Log("false_candle_tell");return true;
        }
        // Lets the integrator's compass needle spin at nothing for 3 s.
        public bool FalseCompassReading()
        {
            if(!Ready||!Can("false_compass"))return false;
            FalseCompass=true;falseCompassTimer=3f;
            Debug.Log("HC_LIE kind=false_compass");
            Log("false_compass");
            return true;
        }

        // ---- generated textures ----------------------------------------------------------------------------------------------------
        static Material SilhouetteMaterial
        {
            get
            {
                if(!silhouetteMaterial)silhouetteMaterial=SurrealProps.Unlit("Window silhouette",WingTexture,new Color(0,0,0,1));
                return silhouetteMaterial;
            }
        }
        static float Smooth(float a,float b,float x){float k=Mathf.Clamp01((x-a)/(b-a));return k*k*(3-2*k);}
        // A black bat seen from the front: a small body, ears, two scalloped wings rising to hooked tips. Only the alpha matters.
        static Texture2D WingTexture
        {
            get
            {
                if(wingTexture)return wingTexture;
                wingTexture=SurrealProps.MakeTexture("Window silhouette wings",128,64,(u,v)=>
                {
                    float x=Mathf.Abs(u-.5f)*2; // 0 at the body, 1 at the wing tip
                    float alpha=0;
                    float body=Mathf.Clamp01(1-(x*x/(.07f*.07f)+(v-.5f)*(v-.5f)/(.2f*.2f)));
                    float ears=(v>.68f&&v<.86f)?Mathf.Clamp01(1-Mathf.Abs(x-.045f)/Mathf.Max(.001f,.035f*(.86f-v)/.18f)):0;
                    alpha=Mathf.Max(body>0?1:0,ears>0?1:0);
                    if(x>.05f)
                    {
                        float top=.55f+.35f*Mathf.Pow(x,1.5f);
                        float tip=Mathf.Pow(x,4);
                        float bottom=Mathf.Lerp(.5f-.22f*(1-x)-.09f*Mathf.Abs(Mathf.Sin(x*3*Mathf.PI))*(1-tip),top,tip);
                        float coverage=Smooth(0,.04f,v-bottom)*Smooth(0,.04f,top-v);
                        alpha=Mathf.Max(alpha,coverage);
                    }
                    return new Color32(0,0,0,(byte)Mathf.RoundToInt(alpha*255));
                });
                return wingTexture;
            }
        }
        // A tall cloaked figure with a small head: only the alpha matters.
        static Texture2D FigureTexture
        {
            get
            {
                if(figureTexture)return figureTexture;
                figureTexture=SurrealProps.MakeTexture("Gallery shape figure",64,128,(u,v)=>
                {
                    float x=Mathf.Abs(u-.5f)*2; // 0 at the centre line, 1 at the texture edge
                    float head=Mathf.Clamp01(1-(x*x/(.2f*.2f)+(v-.9f)*(v-.9f)/(.07f*.07f)));
                    float neck=(v>.78f&&v<=.84f)?Smooth(.16f,.1f,x):0;
                    float shoulders=Smooth(.8f,.62f,v); // 0 near the head, 1 below the shoulders
                    float half=Mathf.Lerp(.1f,.62f,shoulders)*(1-.18f*Smooth(.3f,0,v));
                    float body=v<.8f?Smooth(half+.04f,half-.04f,x):0;
                    float alpha=Mathf.Max(head>0?1:0,Mathf.Max(neck,body));
                    return new Color32(0,0,0,(byte)Mathf.RoundToInt(alpha*255));
                });
                return figureTexture;
            }
        }
        void OnDestroy()
        {
            Floater.ClearFalseTarget();
            if(shape)Destroy(shape);
            if(shapeMaterial)Destroy(shapeMaterial);
        }
    }

    // First hard impact plays a cue (a book hitting the floor). Runtime only; never saved into a scene.
    public sealed class CueOnImpact : MonoBehaviour
    {
        public Soundscape Sound;public string Key;public float Volume=.8f;bool done;
        void OnCollisionEnter(Collision collision)
        {
            if(done||collision.relativeVelocity.magnitude<.8f)return;
            done=true;if(Sound)Sound.Cue(Key,transform.position,Volume);
        }
    }

    // A camera-facing flat creature that travels from `start` to `end` and destroys itself. With flap it narrows and widens like a
    // bat in flight (the GiantBat formula). Runtime only; never saved into a scene.
    public sealed class CrossingBat : MonoBehaviour
    {
        public bool flap=true;public float wingspan=1.5f;public System.Action OnDone;
        Transform viewer;Vector3 start,end;float duration,elapsed;
        public void Begin(Transform eye,Vector3 from,Vector3 to,float seconds,float span)
        {
            viewer=eye;start=from;end=to;duration=Mathf.Max(.2f,seconds);wingspan=span;elapsed=0;
            transform.position=from;
        }
        void Update()
        {
            elapsed+=Time.deltaTime;
            float k=Mathf.Clamp01(elapsed/duration);
            var position=Vector3.Lerp(start,end,k);
            position.y+=Mathf.Sin(k*Mathf.PI*2.4f)*.14f+Mathf.Sin(k*Mathf.PI)*.18f;
            transform.position=position;
            float beat=flap?Mathf.Abs(Mathf.Cos(elapsed*2.4f*Mathf.PI*2)):1;
            transform.localScale=new Vector3(wingspan*(.4f+.6f*beat),wingspan*.5f*(.92f+.08f*beat),1);
            if(viewer)
            {
                var to=position-viewer.position;
                if(to.sqrMagnitude>1e-4f)transform.rotation=Quaternion.LookRotation(to,Vector3.up)*Quaternion.Euler(0,0,Mathf.Sin(elapsed*6)*10f);
            }
            if(k>=1)Done();
        }
        void Done(){var done=OnDone;OnDone=null;if(done!=null)done();Destroy(gameObject);}
        void OnDestroy(){var done=OnDone;OnDone=null;if(done!=null)done();}
    }
}
