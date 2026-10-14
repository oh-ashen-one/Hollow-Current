using System;
using System.Collections.Generic;
using UnityEngine;
namespace HollowCurrent
{
    public sealed class Soundscape : MonoBehaviour
    {
        readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        readonly List<AudioSource> voices=new List<AudioSource>();
        readonly List<AudioLowPassFilter> filters=new List<AudioLowPassFilter>();
        readonly HashSet<string> generated=new HashSet<string>();
        readonly List<float> speechUntil=new List<float>();
        Game game;int cursor,foot;float occlusionTime;
        bool castle;
        AudioSource rain,room,dispatch,interfaceVoice,safeMusic,houseMusic,huntMusic,playerBreath;
        float duck=1,uiCooldown;
        bool lastJournal;
        // Castle mixer (FEAR_PLAN item 12): no house music, dread heartbeat, battlement wind, a rare line,
        // and the Count's claws (item 13). All null/unused in Bellwether.
        AudioSource heartbeat,wind;
        readonly List<Vector3> windows=new List<Vector3>();
        bool rareLine,rareDone;float rareAt=-1;
        Vector3 clawLast;bool clawTracking;int clawIndex=-1;
        // Per-run offset on the heartbeat curve (critic r10 #2): +-8 felt Dread, so silence never reads as an exact threshold.
        float heartbeatOffset;
        // Active silence (DuckBed): envelope 1 -> depth over 1 s, hold, back to 1 over 3 s; scales wind, room tone, heartbeat.
        float duckDepth=1,duckHold,duckClock=-1,duckLevel=1;
        float roomBase,windBase,heartBase;
        public float HeartbeatVolume=>heartbeat?heartbeat.volume:0;
        public float WindVolume=>wind?wind.volume:0;
        public int ClawStepCount {get;private set;}
        string lastNote;
        public int PlayedCues {get;private set;}
        public int PlayedSpeech {get;private set;}
        public int GeneratedCues {get;private set;}
        public int MusicFrames {get;private set;}
        public int BreathingFrames {get;private set;}
        public int DuckedFrames {get;private set;}
        public int MusicMask {get;private set;}
        public void Initialize(Game owner)
        {
            game=owner;
            foreach(var clip in Resources.LoadAll<AudioClip>("Audio"))clips[clip.name]=clip;
            foreach(var clip in Resources.LoadAll<AudioClip>("HiggsfieldAudio")){clips[clip.name]=clip;generated.Add(clip.name);clip.LoadAudioData();}
            // The castle layers its own clips (Resources/CastleAudio, keyed by file
            // name) over the shared ones; missing required clips become silence.
            castle=game.Map==MapId.Castle;
            if(castle)foreach(var clip in Resources.LoadAll<AudioClip>("CastleAudio")){clips[clip.name]=clip;clip.LoadAudioData();}
            foreach(string id in new[]{"music_safety","music_house","music_hunt","breath_hidden","breath_presence","apparition","capture","ui_paper","evidence_filed","containment_release"})
                if(!generated.Contains(id)){if(!castle)throw new InvalidOperationException("Required Higgsfield audio missing: "+id);if(!clips.ContainsKey(id)){MissingArt.Report("audio",id);clips[id]=MissingArt.Silence;}}
            foreach(string id in new[]{"step_wood_0","step_tile_0","step_carpet_0","step_gravel_0","rain","roomtone","voice_evp","voice_arrival"})
                if(!clips.ContainsKey(id)){if(!castle)throw new InvalidOperationException("Required authored audio missing: "+id);MissingArt.Report("audio",id);clips[id]=MissingArt.Silence;}
            for(int i=0;i<12;i++)
            {
                var g=new GameObject("Spatial voice "+i);g.SetActive(false);g.transform.SetParent(transform);var a=g.AddComponent<AudioSource>();
                a.playOnAwake=false;a.spatialBlend=1;a.rolloffMode=AudioRolloffMode.Logarithmic;a.minDistance=1.5f;a.maxDistance=22;a.dopplerLevel=0;
                var filter=g.AddComponent<AudioLowPassFilter>();filter.enabled=false;voices.Add(a);filters.Add(filter);speechUntil.Add(0);g.SetActive(true);
            }
            rain=Loop("rain",.15f);room=Loop("roomtone",.25f);
            dispatch=gameObject.AddComponent<AudioSource>();dispatch.playOnAwake=false;dispatch.spatialBlend=0;dispatch.volume=.55f;
            interfaceVoice=gameObject.AddComponent<AudioSource>();interfaceVoice.playOnAwake=false;interfaceVoice.spatialBlend=0;interfaceVoice.ignoreListenerPause=true;
            safeMusic=Loop("music_safety",0);houseMusic=Loop("music_house",0);huntMusic=Loop("music_hunt",0);playerBreath=Loop("breath_hidden",0);
            safeMusic.ignoreListenerPause=houseMusic.ignoreListenerPause=huntMusic.ignoreListenerPause=true;
            if(castle)
            {
                heartbeat=CastleLoop("heartbeat_loop");wind=CastleLoop("wind_battlements_loop");
                foreach(var l in game.World.WindowLights)if(l)windows.Add(l.transform.position);
                foreach(var w in game.World.Root.GetComponentsInChildren<WindowMoon>(true))windows.Add(w.transform.position);
                // One run in three may hear the rare line once (rolled now, played after the first hunt).
                rareLine=UnityEngine.Random.value<1f/3;heartbeatOffset=UnityEngine.Random.Range(-8f,8f);
                Debug.Log("HC_CASTLE_MIX heartbeat="+clips.ContainsKey("heartbeat_loop")+" wind="+clips.ContainsKey("wind_battlements_loop")+" windows="+windows.Count+" rareLine="+rareLine);
            }
            var basement=new GameObject("Basement acoustic volume");basement.transform.SetParent(transform);basement.transform.position=new Vector3(0,-2,7);
            var zone=basement.AddComponent<AudioReverbZone>();zone.reverbPreset=AudioReverbPreset.StoneCorridor;zone.minDistance=2;zone.maxDistance=4.5f;
            Debug.Log("HC_AUDIO_READY "+clips.Count+" authored clips including intelligible voice takes");
        }
        AudioSource Loop(string name,float volume)
        {
            var a=gameObject.AddComponent<AudioSource>();a.clip=clips[name];a.loop=true;a.playOnAwake=false;a.volume=volume;a.spatialBlend=0;a.Play();return a;
        }
        AudioSource CastleLoop(string name)
        {
            if(!clips.ContainsKey(name)){MissingArt.Report("audio",name);clips[name]=MissingArt.Silence;}
            var a=Loop(name,0);a.ignoreListenerPause=false;return a;
        }
        // Felt Dread 20 -> 0, 45 -> 0.15, 70 -> 0.35, 100 -> 0.55 (AUDIO.md levels; the curve starts at 20 after critic r10 #2) and is
        // shifted by the per-run offset. No hunt floor: the heartbeat follows the felt reading only (#39).
        public static float HeartbeatForDread(float dread)
        {
            if(dread<=20)return 0;if(dread<=45)return Mathf.Lerp(0,.15f,(dread-20)/25);if(dread<=70)return Mathf.Lerp(.15f,.35f,(dread-45)/25);
            return Mathf.Lerp(.35f,.55f,Mathf.Clamp01((dread-70)/30));
        }
        // Investigation.FeltDread (hc/rules2: lagged, noisy, lying 0-100) once merged; the true Dread until then.
        static Func<Investigation,float> felt;static bool feltResolved;
        public static float FeltDread(Investigation c)
        {
            if(!feltResolved)
            {
                feltResolved=true;var p=typeof(Investigation).GetProperty("FeltDread");
                if(p!=null&&p.PropertyType==typeof(float))felt=(Func<Investigation,float>)Delegate.CreateDelegate(typeof(Func<Investigation,float>),p.GetGetMethod());
                Debug.Log("HC_FELT_DREAD source="+(felt!=null?"FeltDread":"Dread"));
            }
            return felt!=null?felt(c):c.Dread;
        }
        // Contract for F3 (critic r10 #38): an active silence. Ducks wind, room tone and heartbeat to `depth` over 1 s, holds for
        // `seconds`, restores over 3 s. Castle only.
        public void DuckBed(float seconds,float depth)
        {
            if(!castle)return;
            duckDepth=Mathf.Clamp01(depth);duckHold=Mathf.Max(0,seconds);duckClock=0;
            Debug.Log("HC_DUCK seconds="+duckHold.ToString("F1")+" depth="+duckDepth.ToString("F2"));
        }
        float DuckEnvelope(float dt)
        {
            if(duckClock<0)return 1;
            duckClock+=dt;
            if(duckClock<1)return Mathf.Lerp(1,duckDepth,duckClock);
            if(duckClock<1+duckHold)return duckDepth;
            if(duckClock<4+duckHold)return Mathf.Lerp(duckDepth,1,(duckClock-1-duckHold)/3);
            duckClock=-1;return 1;
        }
        public void Speak(string id,Vector3? location=null)
        {
            // Castle speech uses its own cc_ lines only (voice_cc_*); never the house's voices.
            // Shared ids map onto Count lines; "cc_*" ids are used as given.
            AudioClip clip;
            if(castle)
            {
                string key=id.StartsWith("cc_",StringComparison.Ordinal)?id:id=="evp"?"cc_bargain":id=="whisper"?"cc_listen":id=="arrival"?"cc_night":id=="warning"?"cc_hunt_hungry":id=="return"?"cc_stay":id=="dispatch"?"cc_dispatch":"cc_"+id;
                if(!clips.TryGetValue("voice_"+key,out clip)){MissingArt.Report("speech","voice_"+key);return;}
            }
            else if(!clips.TryGetValue("voice_"+id,out clip))throw new InvalidOperationException("Missing speech "+id);
            PlayedSpeech++;
            if(location.HasValue){int slot=Play(clip,location.Value,.70f,1);speechUntil[slot]=Time.unscaledTime+clip.length;return;}
            dispatch.Stop();dispatch.clip=clip;dispatch.Play();
        }
        public void Cue(string id,Vector3 position,float volume=.55f)
        {
            if(id=="step")
            {
                string material=game.World.StepSurface(position);
                id="step_"+material+"_"+(foot++%4);
            }
            if(id=="voice"){Speak("evp",position);return;}
            if(id=="breath")id="breath_presence";
            if(!clips.TryGetValue(id,out var clip)){if(castle){MissingArt.Report("audio",id);return;}throw new InvalidOperationException("Missing sound "+id);}
            if(generated.Contains(id))GeneratedCues++;
            Play(clip,position+Vector3.up*.12f,volume,1+(cursor%5-2)*.012f);
        }
        public bool Has(string id)=>clips.ContainsKey(id);
        // Looping 3D source (castle loops: AUDIO.md); silent if the clip is missing.
        public AudioSource SpatialLoop(string id,Transform parent,Vector3 position,float volume,float minDistance,float maxDistance)
        {
            var g=new GameObject("Spatial loop / "+id);g.transform.SetParent(parent?parent:transform,false);g.transform.position=position;
            var a=g.AddComponent<AudioSource>();
            if(!clips.TryGetValue(id,out var clip)){MissingArt.Report("audio",id);clip=MissingArt.Silence;}
            a.clip=clip;a.loop=true;a.playOnAwake=false;a.volume=volume;a.spatialBlend=1;a.rolloffMode=AudioRolloffMode.Logarithmic;a.minDistance=minDistance;a.maxDistance=maxDistance;a.dopplerLevel=0;a.Play();
            return a;
        }
        public void UI(string id="ui_paper")
        {
            if(game.Loading||Time.unscaledTime<uiCooldown)return;
            uiCooldown=Time.unscaledTime+.10f;interfaceVoice.Stop();interfaceVoice.clip=clips[id];interfaceVoice.volume=.42f;interfaceVoice.Play();GeneratedCues++;
        }
        int Play(AudioClip clip,Vector3 position,float volume,float pitch)
        {
            // Preserve intelligible spatial speech when footsteps/scares share the pool.
            int first=cursor++%voices.Count,index=-1;
            for(int i=0;i<voices.Count;i++){int candidate=(first+i)%voices.Count;if(!voices[candidate].isPlaying){index=candidate;break;}if(index<0&&speechUntil[candidate]<=Time.unscaledTime)index=candidate;}
            if(index<0)index=first;
            var a=voices[index];a.Stop();speechUntil[index]=0;a.transform.position=position;a.clip=clip;a.volume=volume;a.pitch=pitch;
            filters[index].cutoffFrequency=22000;filters[index].enabled=true;a.Play();PlayedCues++;return index;
        }
        void CastleMix(float dt,float bed,bool hunting,bool terminal)
        {
            var p=game.Player;float dread=FeltDread(game.Case)+heartbeatOffset;
            duckLevel=DuckEnvelope(dt);
            float beat=terminal||!game.Started?0:HeartbeatForDread(dread)*bed;
            heartBase=Mathf.MoveTowards(heartBase,beat,dt*.55f); // ~1 s ramp
            heartbeat.volume=heartBase*duckLevel;
            float nearWindow=99;foreach(var w in windows)nearWindow=Mathf.Min(nearWindow,Vector3.Distance(p.transform.position,w));
            float air=!game.Started||terminal?0:!p.Inside?.15f:nearWindow<2.5f?.10f:0;
            windBase=Mathf.MoveTowards(windBase,air*bed,dt*.25f);wind.volume=windBase*duckLevel;
            RareLine(hunting);
            ClawSteps(hunting);
        }
        // cc_listen at most once per run, in one run of three, at a quiet moment after the first hunt,
        // 2 m behind and 1.5 m above the player's head (below the ceiling).
        void RareLine(bool hunting)
        {
            if(!rareLine||rareDone||game.Case.Hunts<1||game.Modal)return;
            if(hunting){rareAt=-1;return;}
            if(rareAt<0){rareAt=Time.time+UnityEngine.Random.Range(20f,90f);return;}
            if(Time.time<rareAt)return;
            bool speaking=dispatch.isPlaying;for(int i=0;i<voices.Count;i++)if(voices[i].isPlaying&&speechUntil[i]>Time.unscaledTime)speaking=true;
            if(speaking||game.Player.InVan||game.Player.Hidden||game.Case.Phase==Phase.Warning){rareAt=Time.time+5;return;}
            var view=game.Player.View.transform;var back=Vector3.ProjectOnPlane(view.forward,Vector3.up);if(back.sqrMagnitude<.01f)back=game.Player.transform.forward;back.Normalize();
            var at=view.position-back*2+Vector3.up*1.5f;
            if(Physics.Raycast(view.position,Vector3.up,out var ceiling,1.6f,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore))at.y=Mathf.Min(at.y,ceiling.point.y-.2f);
            rareDone=true;Speak("cc_listen",at);Debug.Log("HC_RARE_LINE cc_listen at="+at+" t="+game.Case.Elapsed.ToString("F1"));
        }
        // Claw steps from the Count's ground movement: every 0.5 m he travels on a floor (not gliding),
        // 5 % volume outside hunts, full in hunts.
        void ClawSteps(bool hunting)
        {
            var creature=game.Creature;var actor=creature?creature.Actor:null;
            if(!actor||!actor.gameObject.activeInHierarchy||game.Modal){clawTracking=false;return;}
            var at=actor.transform.position;
            bool grounded=at.y-World.FloorBelow(at.y)<.5f;
            if(!clawTracking||!grounded){clawLast=at;clawTracking=grounded;return;}
            if(Vector3.ProjectOnPlane(at-clawLast,Vector3.up).magnitude<.5f)return;
            clawLast=at;int next=clawIndex<0?UnityEngine.Random.Range(0,4):UnityEngine.Random.Range(0,3);if(clawIndex>=0&&next>=clawIndex)next++;clawIndex=next;
            Cue("claw_steps_"+clawIndex,at,hunting?.8f:.05f);ClawStepCount++;
        }
        void Update()
        {
            if(game.Player==null)return;
            float dt=Time.unscaledDeltaTime;
            bool speaking=dispatch.isPlaying;
            for(int i=0;i<voices.Count;i++)if(voices[i].isPlaying&&speechUntil[i]>Time.unscaledTime)speaking=true;
            duck=Mathf.MoveTowards(duck,speaking?.22f:1,dt*(speaking?5f:.75f));
            bool quiet=game.Loading||game.Paused||game.Journal||game.NoteBody!=null;
            bool hunting=game.Case.Phase==Phase.Hunt||game.Case.Phase==Phase.Warning||castle&&game.Case.HuntActive;
            bool safe=!game.Started||(game.Player.InVan&&!hunting&&!game.Scares.Interference);
            bool terminal=game.Case.Terminal;
            float bed=game.Loading||terminal?0:quiet?.16f:1;
            // Castle (critic r10 #13/#37): no music in the gatehouse or in hunts; the title keeps the safety theme.
            if(castle&&game.Started)safe=false;
            safeMusic.volume=Mathf.MoveTowards(safeMusic.volume,safe?.32f*bed*duck:0,dt*.25f);
            // The castle has no house music (silence is the bed) and caps the hunt music at 0.15.
            houseMusic.volume=Mathf.MoveTowards(houseMusic.volume,!safe&&!hunting&&!castle?.28f*bed*duck:0,dt*.25f);
            huntMusic.volume=Mathf.MoveTowards(huntMusic.volume,!safe&&hunting&&!castle?.38f*bed*duck:0,dt*.35f);
            if(castle)CastleMix(dt,bed,hunting,terminal);
            // Castle: she breathes whenever she hides, so the breath never marks a hunt (critic r10 #1).
            float breathLevel=game.Started&&!game.Modal&&game.Player.Hidden&&(hunting||castle)?.28f*duck:0;
            playerBreath.volume=Mathf.MoveTowards(playerBreath.volume,breathLevel,dt*.35f);
            if(safeMusic.volume+houseMusic.volume+huntMusic.volume>.02f&&game.Volume>0)MusicFrames++;
            if(safeMusic.volume>.02f)MusicMask|=1;if(houseMusic.volume>.02f)MusicMask|=2;if(huntMusic.volume>.02f)MusicMask|=4;
            if(playerBreath.volume>.02f&&!AudioListener.pause&&game.Volume>0)BreathingFrames++;
            if(speaking&&duck<.7f)DuckedFrames++;
            if(!game.Loading&&game.Started&&((game.Journal&&!lastJournal)||(game.NoteBody!=null&&game.NoteBody!=lastNote)))UI();
            lastJournal=game.Journal;lastNote=game.NoteBody;
            rain.volume=Mathf.MoveTowards(rain.volume,game.Player.Inside?.035f:game.Player.InVan?.10f:.20f,Time.unscaledDeltaTime*.25f);
            if(castle){roomBase=Mathf.MoveTowards(roomBase,game.Player.Inside&&game.Case.BreakerOn?.24f:0,Time.unscaledDeltaTime*.25f);room.volume=roomBase*duckLevel;}
            else room.volume=Mathf.MoveTowards(room.volume,game.Player.Inside&&game.Case.BreakerOn?.24f:0,Time.unscaledDeltaTime*.25f);
            occlusionTime-=Time.unscaledDeltaTime;if(occlusionTime>0)return;occlusionTime=.12f;
            for(int i=0;i<voices.Count;i++)
            {
                if(!voices[i].isPlaying){filters[i].enabled=false;continue;}
                filters[i].enabled=true;
                bool obstructed=Physics.Linecast(game.Player.View.transform.position,voices[i].transform.position,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore);
                filters[i].cutoffFrequency=obstructed?1150:22000;
            }
        }
    }
}
