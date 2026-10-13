using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HollowCurrent
{
    public sealed class Game : MonoBehaviour
    {
        // The active map, from the loaded scene's name (MapCatalog). Unknown scenes
        // (editor test scenes, "Untitled") are Bellwether.
        public MapId Map {get;private set;}
        public MapInfo MapInfo=>MapCatalog.Get(Map);
        // Title-screen choice (PlayerPrefs "hc.map"). BEGIN INVESTIGATION loads it
        // when it differs from the scene that is open.
        public MapId SelectedMap {get;private set;}
        // The map a loading screen is loading (equals Map for a retry).
        public MapId LoadingTarget {get;private set;}
        static bool startOnLoad;
        public static bool MapAvailable(MapId map)=>Application.CanStreamedLevelBeLoaded(MapCatalog.Get(map).SceneName);
        public Investigation Case {get;private set;}
        public World World {get;private set;}
        public Investigator Player {get;private set;}
        public Presence Creature {get;private set;}
        public ScareDirector Scares {get;private set;}
        public Soundscape Sound {get;private set;}
        public bool Started, Paused, Journal;
        public bool Loading {get;private set;}
        public float LoadingProgress {get;private set;}
        public bool HelpHints,Subtitles=true;
        public string NoteTitle, NoteBody;
        public bool Modal => Loading || !Started || Paused || Journal || NoteBody!=null || Case.Terminal;
        public string Subtitle {get;private set;}
        public readonly List<string> Log=new List<string>();
        public float SubtitleTime;
        public float Volume=.65f;
        public float SubtitleScale=1,CardOpacity=.94f;
        float radioCooldown;
        // Checkpoints are kept per map: retrying or switching maps never restores
        // one map's progress into the other.
        static readonly Dictionary<MapId,Investigation.Checkpoint> checkpoints=new Dictionary<MapId,Investigation.Checkpoint>();
        static readonly Dictionary<MapId,Vector3> checkpointPositions=new Dictionary<MapId,Vector3>();
        static bool restoreCheckpoint;
        int checkpointProgress=-1;
        public bool HasCheckpoint=>checkpoints.ContainsKey(Map);
        int manifestation;
        bool uiProbe,evidenceAppearanceScheduled;
        public int ManifestationCount => Scares?Scares.Events:manifestation;
        public int DollMovementCount {get;private set;}
        readonly Dictionary<Light,float> lampLevels=new Dictionary<Light,float>();
        readonly HashSet<Light> darkCircuits=new HashSet<Light>();
        readonly Dictionary<Transform,Vector3> dollHomes=new Dictionary<Transform,Vector3>();
        public int FrameSamples;
        public float FrameTotal,WorstFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register(){SceneManager.sceneLoaded-=Boot;SceneManager.sceneLoaded+=Boot;}
        static void Boot(Scene scene,LoadSceneMode mode)
        {
            if(FindFirstObjectByType<Game>()==null)new GameObject("Hollow Current / Game").AddComponent<Game>();
        }
        void Awake()
        {
            Loading=true;
            Map=MapCatalog.TryFromSceneName(SceneManager.GetActiveScene().name,out var sceneMap)?sceneMap:MapId.Bellwether;LoadingTarget=Map;
            SelectedMap=MapCatalog.FromPreference(PlayerPrefs.GetInt(MapCatalog.PreferenceKey,(int)Map));if(SelectedMap!=Map&&!MapAvailable(SelectedMap))SelectedMap=Map;
            gameObject.AddComponent<PadInput>();
            uiProbe=Array.IndexOf(Environment.GetCommandLineArgs(),"-hc-ui-probe")>=0;
            Application.targetFrameRate=90;QualitySettings.vSyncCount=0;QualitySettings.antiAliasing=4;QualitySettings.shadowResolution=ShadowResolution.Medium;QualitySettings.shadowDistance=16;
            // A finished authored scene owns its ambient/probe lighting. Keep the
            // fallback for unbaked iteration builds without overwriting a bake.
            bool bakedHouse=LightmapSettings.lightmaps.Length>0;
            if(!bakedHouse)
            {RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.105f,.13f,.16f);RenderSettings.ambientEquatorColor=new Color(.055f,.06f,.07f);RenderSettings.ambientGroundColor=new Color(.025f,.028f,.03f);}
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.025f,.04f,.055f);RenderSettings.fogDensity=.012f;RenderSettings.fogMode=FogMode.Exponential;
            Case=new Investigation();Case.Message+=Say;Case.Changed+=PhaseChanged;
            World=new World();World.LoadOrBuild(Map);World.BuildNavigation();
            if(!bakedHouse)
            {var moon=new GameObject("Cold overcast sky").AddComponent<Light>();moon.transform.SetParent(World.Root);moon.type=LightType.Directional;moon.color=new Color(.36f,.51f,.75f);moon.intensity=.16f;moon.transform.rotation=Quaternion.Euler(55,-30,0);moon.shadows=LightShadows.None;}
            Sound=gameObject.AddComponent<Soundscape>();Sound.Initialize(this);
            var p=new GameObject("Investigator");p.transform.position=World.Spawn;Player=p.AddComponent<Investigator>();Player.Initialize(this);
            Player.Sensitivity=Mathf.Clamp(PlayerPrefs.GetFloat("hc.sensitivity",1.65f),.3f,4);Player.Brightness=Mathf.Clamp(PlayerPrefs.GetFloat("hc.brightness",1),.7f,1.8f);
            SubtitleScale=Mathf.Clamp(PlayerPrefs.GetFloat("hc.subtitle-scale",1),.85f,1.3f);CardOpacity=Mathf.Clamp(PlayerPrefs.GetFloat("hc.card-opacity",.94f),.55f,1);
            Player.InvertY=PlayerPrefs.GetInt("hc.invert-y",0)!=0;Volume=Mathf.Clamp01(PlayerPrefs.GetFloat("hc.volume",.65f));Subtitles=PlayerPrefs.GetInt("hc.subtitles",1)!=0;HelpHints=PlayerPrefs.GetInt("hc.help",0)!=0;
            var entity=new GameObject("Presence senses and navigation");Creature=entity.AddComponent<Presence>();Creature.Initialize(this);
            Scares=gameObject.AddComponent<ScareDirector>();Scares.Initialize(this);
            foreach(var lamp in World.Lamps)lampLevels[lamp]=lamp.intensity;
            foreach(var doll in World.Dolls)dollHomes[doll]=doll.position;
            gameObject.AddComponent<Interface>().Game=this;
            Say("Dispatch / Bellwether House. Family evacuated. Keep an escape route. She can reach the van. Collect the kit and read your brief.");
            ApplyFocus();Debug.Log("HC_READY source-generated house; runtime route remains subject to playtest.");
            if(NativeVerification.Requested&&FindFirstObjectByType<NativeVerification>()==null)new GameObject("Opt-in native verification").AddComponent<NativeVerification>();
        }
        IEnumerator Start()
        {
            // Complete startup allocations while the loading presentation is
            // visible, instead of collecting that garbage on entering the house.
            yield return null;yield return new WaitForEndOfFrame();
            GC.Collect();LoadingProgress=1;yield return new WaitForEndOfFrame();
            Loading=false;
            if(restoreCheckpoint&&checkpoints.TryGetValue(Map,out var checkpoint))
            {restoreCheckpoint=false;evidenceAppearanceScheduled=true;Case.RestoreCheckpoint(checkpoint);Player.Teleport(checkpointPositions[Map]);Started=true;Scares.RetryGrace();Say("Checkpoint restored / Your records are intact. Find cover before the signal rises.");}
            if(startOnLoad){startOnLoad=false;StartCase();}
            ApplyFocus();Debug.Log("HC_STARTUP_READY");
        }
        void Update()
        {
            if(Loading)return;
            var pad=PadInput.Current;
            if(pad&&pad.Disconnected&&Started&&!Modal){Paused=true;ApplyFocus();}
            if(pad&&pad.Pause&&!Case.Terminal)
            {
                if(NoteBody!=null)NoteBody=null;else if(Journal)Journal=false;else Paused=!Paused;
                SavePreferences();ApplyFocus();
            }
            if(uiProbe&&Input.anyKeyDown)Debug.Log("HC_UI_INPUT focused="+Application.isFocused+" mouse="+Input.mousePosition+" screen="+Screen.width+"x"+Screen.height);
            if(!Started&&!Paused&&(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter)))BeginInvestigation();
            if(!Started&&!Paused&&Input.GetKeyDown(KeyCode.Alpha1))SelectMap(MapId.Bellwether);
            if(!Started&&!Paused&&Input.GetKeyDown(KeyCode.Alpha2))SelectMap(MapId.Castle);
            if(!Started&&!Paused&&Input.GetKeyDown(KeyCode.S)){Paused=true;ApplyFocus();}
            if(!Started&&Input.GetKeyDown(KeyCode.Q)){Quit();return;}
            if(Paused||Case.Terminal)
            {
                if(Input.GetKeyDown(KeyCode.N)){Retry();return;}
                if(Input.GetKeyDown(KeyCode.Q)){Quit();return;}
            }
            if(Paused)
            {
                if(Input.GetKeyDown(KeyCode.Alpha1))Player.Sensitivity=Mathf.Max(.3f,Player.Sensitivity-.15f);
                if(Input.GetKeyDown(KeyCode.Alpha2))Player.Sensitivity=Mathf.Min(4,Player.Sensitivity+.15f);
                if(Input.GetKeyDown(KeyCode.Alpha3))Player.Brightness=Mathf.Max(.7f,Player.Brightness-.1f);
                if(Input.GetKeyDown(KeyCode.Alpha4))Player.Brightness=Mathf.Min(1.8f,Player.Brightness+.1f);
                if(Input.GetKeyDown(KeyCode.Alpha5))Volume=Mathf.Max(0,Volume-.05f);
                if(Input.GetKeyDown(KeyCode.Alpha6))Volume=Mathf.Min(1,Volume+.05f);
                if(Input.GetKeyDown(KeyCode.I))Player.InvertY=!Player.InvertY;
                if(Input.GetKeyDown(KeyCode.T))Subtitles=!Subtitles;
            }
            if(Input.GetKeyDown(KeyCode.H)&&(Started||Paused)&&!Case.Terminal)HelpHints=!HelpHints;
            if(Input.GetKeyDown(KeyCode.Escape)||(pad&&pad.Cancel&&Modal))
            {
                if(!Started&&Paused){Paused=false;SavePreferences();}
                else if(NoteBody!=null)NoteBody=null;
                else if(Journal)Journal=false;
                else if(Started&&!Case.Terminal)Paused=!Paused;
                ApplyFocus();
            }
            if((Input.GetKeyDown(KeyCode.J)||(pad&&pad.Journal))&&Started&&!Paused&&NoteBody==null&&!Case.Terminal){Journal=!Journal;ApplyFocus();}
            if(!Modal)
            {
                float dt=Time.deltaTime;Case.Tick(dt,Player.Inside);SubtitleTime-=dt;radioCooldown-=dt;
                FrameSamples++;FrameTotal+=Time.unscaledDeltaTime;WorstFrame=Mathf.Max(WorstFrame,Time.unscaledDeltaTime);
                int progress=(Case.Equipped?1:0)+Case.Evidence.Count*10+Case.Notes.Count*100+(Case.Survived?10000:0)+(Case.Diagnosed?100000:0);
                if(progress!=checkpointProgress&&Case.Phase!=Phase.Warning&&Case.Phase!=Phase.Hunt&&Case.Phase!=Phase.Return)
                {
                    checkpointProgress=progress;checkpoints[Map]=Case.SaveCheckpoint();
                    checkpointPositions[Map]=World.CheckpointPosition(Player.transform.position,Player.Inside);
                }
            }
            // Budget visible practical lights without allocating a new material or shadow map.
            Light nearest=null,second=null;float nearestDistance=float.MaxValue,secondDistance=float.MaxValue;
            foreach(var lamp in World.Lamps)
            {
                float d=Vector3.Distance(Player.transform.position,lamp.transform.position);
                if(!Case.BreakerOn||darkCircuits.Contains(lamp)||d>=8||Mathf.Abs(Player.transform.position.y-lamp.transform.position.y+2.5f)>=3)continue;
                if(d<nearestDistance){second=nearest;secondDistance=nearestDistance;nearest=lamp;nearestDistance=d;}
                else if(d<secondDistance){second=lamp;secondDistance=d;}
            }
            foreach(var kv in lampLevels)
            {
                bool local=Vector3.Distance(Player.transform.position,kv.Key.transform.position)<8&&Mathf.Abs(Player.transform.position.y-kv.Key.transform.position.y+2.5f)<3;
                kv.Key.enabled=local&&Case.BreakerOn&&!darkCircuits.Contains(kv.Key);
                kv.Key.shadows=kv.Key==nearest||kv.Key==second?LightShadows.Soft:LightShadows.None;
                kv.Key.intensity=kv.Value*(Scares?Scares.LightMultiplier:1)*(Case.Phase==Phase.Warning&&Mathf.Sin(Time.time*17)>0?.08f:1);
            }
            foreach(var light in World.WindowLights)
            {
                if(light.bakingOutput.isBaked)continue;
                bool near=Vector3.Distance(Player.transform.position,light.transform.position)<7;
                bool floor=Mathf.Abs(Player.transform.position.y-light.transform.position.y+1.5f)<2;
                light.enabled=near&&floor;
            }
            AudioListener.volume=Volume;
        }
        void OnGUI()
        {
            if(uiProbe&&(Event.current.isKey||Event.current.type==EventType.MouseDown||Event.current.type==EventType.MouseUp))
                Debug.Log("HC_UI_EVENT "+Event.current.type+" mouse="+Event.current.mousePosition+" key="+Event.current.keyCode+" focused="+Application.isFocused);
        }
        void OnApplicationFocus(bool focus){if(uiProbe)Debug.Log("HC_UI_FOCUS "+focus);if(!focus&&Started&&!Case.Terminal&&!NativeVerification.Requested){Paused=true;ApplyFocus();}}
        public void SelectMap(MapId map)
        {
            if(Loading||Started||map==SelectedMap||!MapAvailable(map))return;
            SelectedMap=map;PlayerPrefs.SetInt(MapCatalog.PreferenceKey,(int)map);PlayerPrefs.Save();Sound?.UI();
        }
        // Title BEGIN: start here, or load the chosen map's scene and start there.
        public void BeginInvestigation()
        {
            if(Loading||Paused)return;
            if(SelectedMap==Map||!MapAvailable(SelectedMap)){StartCase();return;}
            restoreCheckpoint=false;startOnLoad=true;BeginReload(SelectedMap);
        }
        public void StartCase(){if(Loading||Paused)return;SavePreferences();Started=true;ApplyFocus();if(uiProbe)Debug.Log("HC_UI_STARTED cursor="+Cursor.lockState);}
        public void Resume(){SavePreferences();Paused=false;Journal=false;NoteBody=null;ApplyFocus();}
        public void ApplyFocus(){bool modal=Modal;Time.timeScale=modal?0:1;AudioListener.pause=Loading||!Started||Paused||Journal||NoteBody!=null;Cursor.lockState=modal||NativeVerification.Requested?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=(modal&&!PadInput.Active)||NativeVerification.Requested;if(uiProbe)Debug.Log("HC_UI_STATE started="+Started+" paused="+Paused+" journal="+Journal+" cursor="+Cursor.lockState+" visible="+Cursor.visible+" timeScale="+Time.timeScale);}
        public void ShowNote(string title,string body){NoteTitle=title;NoteBody=body;ApplyFocus();}
        public void Say(string value)
        {
            Log.Add(value);if(Log.Count>30)Log.RemoveAt(0);
            if(FieldMessages.ShowCard(value)){Subtitle=value;SubtitleTime=FieldMessages.CardSeconds(value);}
            if(Sound==null)return;
            if(value.Contains("Family evacuated"))Sound.Speak("arrival");
            else if(value.Contains("Three instruments"))Sound.Speak("kit");
        }
        public void ToggleCircuit(Light lamp,Vector3 switchPosition)
        {
            Sound.Cue("switch",switchPosition,.35f);
            if(!Case.BreakerOn){Say("The switch is dead. The breaker is out.");return;}
            if(!lamp)return;
            if(!darkCircuits.Remove(lamp))darkCircuits.Add(lamp);
        }
        public void RadioHint()
        {
            if(radioCooldown>0)return;radioCooldown=8;
            if(Scares&&Scares.Interference){Sound.Speak("whisper");Say("Radio / ...keep... the house... warm...");return;}
            if(Case.Phase==Phase.Hunt||Case.Phase==Phase.Warning){Say("Radio / Find cover. Light off. Stay quiet.");Sound.Speak("warning");return;}
            if(Case.Diagnosed){Say("Dispatch / "+Case.Goal);Sound.Speak(Case.Phase==Phase.Return?"return":"contain");return;}
            string hint=!Case.Evidence.Contains(Evidence.Induction)?"hint_signal":!Case.Evidence.Contains(Evidence.Handprint)?"hint_trace":"hint_voice";
            Say("Dispatch / "+(hint=="hint_signal"?"Follow the meter through the rooms they used most.":hint=="hint_trace"?"Look at what the family left behind. Ultraviolet may reveal a trace.":"The recorder needs a quiet room. Ask once, then listen."));Sound.Speak(hint);
        }
        void PhaseChanged(Phase phase)
        {
            Creature?.PhaseChanged(phase);
            if(phase==Phase.Disturbance&&!Case.Survived&&!evidenceAppearanceScheduled)
            {evidenceAppearanceScheduled=true;StartCoroutine(RevealFirstRecord());}
            if(phase==Phase.Warning){Scares?.Pulse(Player.transform.position+Player.transform.forward*3,9);Sound.Cue("knock",Player.transform.position+Vector3.forward*3);Sound.Speak("warning");}
            if(phase==Phase.Return)Sound.Speak("return");
            if(phase==Phase.Containment)Sound.Speak("contain");
            if(phase==Phase.Won)Sound.Speak("ending");
            if(phase==Phase.Return)Sound.Cue("containment_release",World.Anchor,.65f);
            if(phase==Phase.Won||phase==Phase.Dead){Sound.Cue(phase==Phase.Won?"evidence_filed":"capture",Player.transform.position,.65f);ApplyFocus();}
        }
        IEnumerator RevealFirstRecord()
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(8f,14f));
            Scares.Dread();
        }
        void Manifest(){if(Scares.Trigger())manifestation++;}
        void LateUpdate()
        {
            if(Case.Phase!=Phase.Dead||Creature.PresentationRemaining<=0)return;
            var direction=Creature.Actor.Head.position-Player.View.transform.position;
            if(direction.sqrMagnitude>.04f)Player.View.transform.rotation=Quaternion.Slerp(Player.View.transform.rotation,Quaternion.LookRotation(direction),Time.unscaledDeltaTime*3);
        }
        public void SavePreferences()
        {
            if(!Player)return;
            bool changed=!Mathf.Approximately(PlayerPrefs.GetFloat("hc.subtitle-scale",-1),SubtitleScale)||!Mathf.Approximately(PlayerPrefs.GetFloat("hc.card-opacity",-1),CardOpacity)||!Mathf.Approximately(PlayerPrefs.GetFloat("hc.sensitivity",-1),Player.Sensitivity)||!Mathf.Approximately(PlayerPrefs.GetFloat("hc.brightness",-1),Player.Brightness)||!Mathf.Approximately(PlayerPrefs.GetFloat("hc.volume",-1),Volume)||PlayerPrefs.GetInt("hc.invert-y",-1)!=(Player.InvertY?1:0)||PlayerPrefs.GetInt("hc.subtitles",-1)!=(Subtitles?1:0)||PlayerPrefs.GetInt("hc.help",-1)!=(HelpHints?1:0);
            if(!changed)return;
            PlayerPrefs.SetFloat("hc.subtitle-scale",SubtitleScale);PlayerPrefs.SetFloat("hc.card-opacity",CardOpacity);PlayerPrefs.SetFloat("hc.sensitivity",Player.Sensitivity);PlayerPrefs.SetFloat("hc.brightness",Player.Brightness);PlayerPrefs.SetFloat("hc.volume",Volume);
            PlayerPrefs.SetInt("hc.invert-y",Player.InvertY?1:0);PlayerPrefs.SetInt("hc.subtitles",Subtitles?1:0);PlayerPrefs.SetInt("hc.help",HelpHints?1:0);PlayerPrefs.Save();
        }
        public void Quit(){SavePreferences();Application.Quit();}
        public void RetryCheckpoint(){if(Loading||!HasCheckpoint)return;restoreCheckpoint=true;BeginReload(Map);}
        public void Retry(){if(Loading)return;checkpoints.Remove(Map);checkpointPositions.Remove(Map);restoreCheckpoint=false;BeginReload(Map);}
        void BeginReload(MapId target){LoadingTarget=target;SavePreferences();Loading=true;LoadingProgress=0;ApplyFocus();StartCoroutine(ReloadCase(target));}
        IEnumerator ReloadCase(MapId target)
        {
            // Present a real transition frame before starting asynchronous I/O.
            yield return null;yield return new WaitForEndOfFrame();
            // Load the map's own scene by name; fall back to the active build
            // index only if that scene is not in this player's build.
            string scene=MapCatalog.Get(target).SceneName;
            var operation=Application.CanStreamedLevelBeLoaded(scene)?SceneManager.LoadSceneAsync(scene):SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);operation.allowSceneActivation=false;
            while(operation.progress<.9f){LoadingProgress=Mathf.Clamp01(operation.progress/.9f);yield return null;}
            LoadingProgress=1;yield return new WaitForEndOfFrame();operation.allowSceneActivation=true;
        }
        void OnDestroy(){World?.Dispose();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;Time.timeScale=1;AudioListener.pause=false;}
    }
}
