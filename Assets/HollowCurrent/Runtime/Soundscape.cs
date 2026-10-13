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
            var basement=new GameObject("Basement acoustic volume");basement.transform.SetParent(transform);basement.transform.position=new Vector3(0,-2,7);
            var zone=basement.AddComponent<AudioReverbZone>();zone.reverbPreset=AudioReverbPreset.StoneCorridor;zone.minDistance=2;zone.maxDistance=4.5f;
            Debug.Log("HC_AUDIO_READY "+clips.Count+" authored clips including intelligible voice takes");
        }
        AudioSource Loop(string name,float volume)
        {
            var a=gameObject.AddComponent<AudioSource>();a.clip=clips[name];a.loop=true;a.playOnAwake=false;a.volume=volume;a.spatialBlend=0;a.Play();return a;
        }
        public void Speak(string id,Vector3? location=null)
        {
            // Castle speech uses its own cc_ lines only (voice_cc_<id>); never the house's voices.
            AudioClip clip;
            if(castle){if(!clips.TryGetValue("voice_cc_"+id,out clip)){MissingArt.Report("speech","voice_cc_"+id);return;}}
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
        void Update()
        {
            if(game.Player==null)return;
            float dt=Time.unscaledDeltaTime;
            bool speaking=dispatch.isPlaying;
            for(int i=0;i<voices.Count;i++)if(voices[i].isPlaying&&speechUntil[i]>Time.unscaledTime)speaking=true;
            duck=Mathf.MoveTowards(duck,speaking?.22f:1,dt*(speaking?5f:.75f));
            bool quiet=game.Loading||game.Paused||game.Journal||game.NoteBody!=null;
            bool hunting=game.Case.Phase==Phase.Hunt||game.Case.Phase==Phase.Warning;
            bool safe=!game.Started||(game.Player.InVan&&!hunting&&!game.Scares.Interference);
            bool terminal=game.Case.Terminal;
            float bed=game.Loading||terminal?0:quiet?.16f:1;
            safeMusic.volume=Mathf.MoveTowards(safeMusic.volume,safe?.32f*bed*duck:0,dt*.25f);
            houseMusic.volume=Mathf.MoveTowards(houseMusic.volume,!safe&&!hunting?.28f*bed*duck:0,dt*.25f);
            huntMusic.volume=Mathf.MoveTowards(huntMusic.volume,!safe&&hunting?.38f*bed*duck:0,dt*.35f);
            float breathLevel=game.Started&&!game.Modal&&game.Player.Hidden&&hunting?.28f*duck:0;
            playerBreath.volume=Mathf.MoveTowards(playerBreath.volume,breathLevel,dt*.35f);
            if(safeMusic.volume+houseMusic.volume+huntMusic.volume>.02f&&game.Volume>0)MusicFrames++;
            if(safeMusic.volume>.02f)MusicMask|=1;if(houseMusic.volume>.02f)MusicMask|=2;if(huntMusic.volume>.02f)MusicMask|=4;
            if(playerBreath.volume>.02f&&!AudioListener.pause&&game.Volume>0)BreathingFrames++;
            if(speaking&&duck<.7f)DuckedFrames++;
            if(!game.Loading&&game.Started&&((game.Journal&&!lastJournal)||(game.NoteBody!=null&&game.NoteBody!=lastNote)))UI();
            lastJournal=game.Journal;lastNote=game.NoteBody;
            rain.volume=Mathf.MoveTowards(rain.volume,game.Player.Inside?.035f:game.Player.InVan?.10f:.20f,Time.unscaledDeltaTime*.25f);
            room.volume=Mathf.MoveTowards(room.volume,game.Player.Inside&&game.Case.BreakerOn?.24f:0,Time.unscaledDeltaTime*.25f);
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
