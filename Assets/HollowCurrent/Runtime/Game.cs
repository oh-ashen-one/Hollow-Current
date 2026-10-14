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
        // Owner rule (2026-10-14): the game opens windowed, never covering the whole screen. Checked once per process.
        static bool windowChecked;bool windowSwitched;
        public const int WindowWidth=1920,WindowHeight=1080;
        void EnsureWindowed()
        {
            if(windowChecked||PlaytestDriver.Requested||CaptureMode.Requested||Application.isBatchMode)return;
            windowChecked=true;
            if(Screen.fullScreenMode==FullScreenMode.Windowed&&!Screen.fullScreen)return;
            int dw=Display.main.systemWidth,dh=Display.main.systemHeight;
            int w=Mathf.Min(WindowWidth,Mathf.RoundToInt(dw*.75f)),h=Mathf.RoundToInt(w*9f/16f);
            if(h>dh*.85f){h=Mathf.RoundToInt(dh*.85f);w=Mathf.RoundToInt(h*16f/9f);}
            Screen.SetResolution(w,h,FullScreenMode.Windowed);windowSwitched=true;
        }
        public static bool MapAvailable(MapId map)=>Application.CanStreamedLevelBeLoaded(MapCatalog.Get(map).SceneName);
        public Investigation Case {get;private set;}
        public CaseBook Book=>Case.Book;
        public World World {get;private set;}
        public Investigator Player {get;private set;}
        public Presence Creature {get;private set;}
        public ScareDirector Scares {get;private set;}
        // Castle-only run state (relics, forge, fight, props); null in Bellwether.
        public CastleRun Castle {get;private set;}
        public Soundscape Sound {get;private set;}
        // Castle dread beats (F3); null in Bellwether.
        public CastleScares CastleDread {get;private set;}
        // Death penalty (FEAR_PLAN item 7): the relic index moved to its alternate place this run, or -1.
        public int MovedRelic {get;private set;}=-1;
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
        // Wave F (FEAR_PLAN.md) castle settings, kept in PlayerPrefs. Long Night applies to the next castle case.
        public bool LongNight,MicEnabled;
        public float MicThreshold=.02f;
        public static bool Automated=>PlaytestDriver.Requested||CaptureMode.Requested||NativeVerification.Requested||Application.isBatchMode;
        public const string LongNightKey="hc.long-night",MicKey="hc.microphone",MicThresholdKey="hc.mic-threshold";
        // Castle fear plumbing: candle lights within 6 m count as lit (Dread rises slower), the gatehouse and
        // the lit chapel fire are where Dread falls.
        readonly List<Light> candleLights=new List<Light>();
        bool candlesCached,litNearby,appliedLongNight;
        float litCheck,rulesLog=30;
        Vector3 chapelFire;bool hasChapelFire;
        Phase lastPhase;

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
            EnsureWindowed();ScriptedWindow();
            gameObject.AddComponent<PadInput>();
            uiProbe=Array.IndexOf(Environment.GetCommandLineArgs(),"-hc-ui-probe")>=0;
            Application.targetFrameRate=90;QualitySettings.vSyncCount=0;QualitySettings.antiAliasing=4;QualitySettings.shadowResolution=ShadowResolution.Medium;QualitySettings.shadowDistance=16;
            // A finished authored scene owns its ambient/probe lighting. Keep the
            // fallback for unbaked iteration builds without overwriting a bake.
            bool bakedHouse=LightmapSettings.lightmaps.Length>0;
            if(!bakedHouse)
            {RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.105f,.13f,.16f);RenderSettings.ambientEquatorColor=new Color(.055f,.06f,.07f);RenderSettings.ambientGroundColor=new Color(.025f,.028f,.03f);}
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.025f,.04f,.055f);RenderSettings.fogDensity=.012f;RenderSettings.fogMode=FogMode.Exponential;
            MissingArt.Placeholders=Map!=MapId.Bellwether;
            LongNight=PlayerPrefs.GetInt(LongNightKey,0)!=0;MicEnabled=PlayerPrefs.GetInt(MicKey,0)!=0;MicThreshold=Mathf.Clamp(PlayerPrefs.GetFloat(MicThresholdKey,.02f),.005f,.08f);
            // Automated runs never inherit a player's dial or microphone: Long Night only by flag, microphone never.
            if(Automated){LongNight=Array.IndexOf(Environment.GetCommandLineArgs(),"-hc-long-night")>=0;MicEnabled=false;}
            // Castle rules (HuntRules.Castle or the Long Night dial) and real dice; Bellwether is built exactly as before.
            appliedLongNight=LongNight;
            Case=Map==MapId.Castle?new Investigation(CaseBook.For(Map),()=>UnityEngine.Random.value,HuntRules.For(Map,LongNight)):new Investigation(CaseBook.For(Map));
            Case.Message+=Say;Case.Changed+=PhaseChanged;
            if(Map==MapId.Castle)Debug.Log("HC_RULES "+Case.Rules.Name+" dread="+Case.Dread.ToString("F0")+" runDreadMin="+Case.RunDreadMin.ToString("F1")+" felt="+Case.FeltDread.ToString("F1"));
            World=new World();World.LoadOrBuild(Map);World.BuildNavigation();
            if(Map==MapId.Castle)CastleArt.ApplySkybox();
            if(!bakedHouse&&Map!=MapId.Castle)
            {var moon=new GameObject("Cold overcast sky").AddComponent<Light>();moon.transform.SetParent(World.Root);moon.type=LightType.Directional;moon.color=new Color(.36f,.51f,.75f);moon.intensity=.16f;moon.transform.rotation=Quaternion.Euler(55,-30,0);moon.shadows=LightShadows.None;}
            Sound=gameObject.AddComponent<Soundscape>();Sound.Initialize(this);
            var p=new GameObject("Investigator");p.transform.position=World.Spawn;Player=p.AddComponent<Investigator>();Player.Initialize(this);
            if(Map==MapId.Castle)Player.View.clearFlags=CameraClearFlags.Skybox; // two-moon sky through windows and the courtyard
            Player.Sensitivity=Mathf.Clamp(PlayerPrefs.GetFloat("hc.sensitivity",1.65f),.3f,4);Player.Brightness=Mathf.Clamp(PlayerPrefs.GetFloat("hc.brightness",1),.7f,1.8f);
            SubtitleScale=Mathf.Clamp(PlayerPrefs.GetFloat("hc.subtitle-scale",1),.85f,1.3f);CardOpacity=Mathf.Clamp(PlayerPrefs.GetFloat("hc.card-opacity",.94f),.55f,1);
            Player.InvertY=PlayerPrefs.GetInt("hc.invert-y",0)!=0;Volume=Mathf.Clamp01(PlayerPrefs.GetFloat("hc.volume",.65f));Subtitles=PlayerPrefs.GetInt("hc.subtitles",1)!=0;HelpHints=PlayerPrefs.GetInt("hc.help",0)!=0;
            var entity=new GameObject("Presence senses and navigation");Creature=entity.AddComponent<Presence>();Creature.Initialize(this);
            if(Map==MapId.Castle)
            {
                // Critic r10 #14: the torch fails in the last second of a dive (hc/count12's DiveLastSecond), not at its start.
                Creature.DiveLastSecond+=()=>{Scares.Blackout(1);Debug.Log("HC_TORCH_FAIL dive last second eta="+Creature.DiveEndsIn.ToString("F2"));};
                Creature.Beat+=CountBeat;
            }
            Scares=gameObject.AddComponent<ScareDirector>();Scares.Initialize(this);
            if(Map==MapId.Castle){Castle=gameObject.AddComponent<CastleRun>();Castle.Initialize(this);gameObject.AddComponent<CastleAtmosphere>();gameObject.AddComponent<CastleLightTuning>();}
            if(Map==MapId.Castle)
            {
                CastleDread=gameObject.AddComponent<CastleScares>();CastleDread.Initialize(this);CastleDread.DuckBed=Sound.DuckBed;
                // Long Night: nail hides at run start (the static list from a previous scene is cleared first).
                CastleWorld.UnnailHides();
                // Nailed spots are deactivated; drop them from the hunter's search list too.
                if(Case.Rules.NailedHides>0&&CastleWorld.NailHides(Case.Rules.NailedHides,Player.transform.position)>0)World.HidingPlaces.RemoveAll(h=>!h||!h.gameObject.activeInHierarchy);
            }
            foreach(var lamp in World.Lamps)lampLevels[lamp]=lamp.intensity;
            foreach(var doll in World.Dolls)dollHomes[doll]=doll.position;
            gameObject.AddComponent<Interface>().Game=this;
            Say(Book.Arrival);
            ApplyFocus();Debug.Log("HC_READY source-generated house; runtime route remains subject to playtest.");
            // An unfocused player stops updating unless it runs in the background: capture and the
            // playtest driver are launched from a script, so keep them running before any switch.
            if(ScriptedRun){Application.runInBackground=true;Debug.Log("HC_CURSOR free (scripted run: cursor never locked or hidden; no real mouse/keyboard input read)");}
            if(Map==MapId.Castle&&CaptureMode.Requested)gameObject.AddComponent<CaptureMode>().Initialize(this);
            if(PlaytestDriver.Requested)gameObject.AddComponent<PlaytestDriver>().Initialize(this);
            if(NativeVerification.Requested&&FindFirstObjectByType<NativeVerification>()==null)new GameObject("Opt-in native verification").AddComponent<NativeVerification>();
        }
        IEnumerator Start()
        {
            // Complete startup allocations while the loading presentation is
            // visible, instead of collecting that garbage on entering the house.
            yield return null;yield return new WaitForEndOfFrame();
            // r11 critic (verify: one 35.9 ms frame at Bellwether's first Warning, 409 s in, basement): compile the shader variants in
            // memory during the loading screen instead of on first use. Performance only; no gameplay change.
            Shader.WarmupAllShaders();
            GC.Collect();LoadingProgress=1;yield return new WaitForEndOfFrame();
            Loading=false;
            if(restoreCheckpoint&&checkpoints.TryGetValue(Map,out var checkpoint))
            {restoreCheckpoint=false;evidenceAppearanceScheduled=true;Case.RestoreCheckpoint(checkpoint);if(Case.DeathPenaltyApplied)DeathPenalty();Player.Teleport(checkpointPositions[Map]);Started=true;Scares.RetryGrace();Say("Checkpoint restored / Your records are intact. Find cover before the signal rises.");}
            if(startOnLoad){startOnLoad=false;StartCase();}
            // Capture mode in the combined player (starts in Bellwether): switch to the castle.
            // The case starts on arrival, so S7 (title) is only meaningful in a castle-only build.
            if(CaptureMode.Requested&&CaptureMode.TitleOnly&&!Started){gameObject.AddComponent<CaptureMode>().InitializeTitle(this);ApplyFocus();Debug.Log("HC_STARTUP_READY");yield break;}
            if(CaptureMode.Requested&&Map!=MapId.Castle&&MapAvailable(MapId.Castle))
            {
                // Same path as the title picker (and the playtest driver): select, then begin.
                yield return new WaitForSecondsRealtime(1);Debug.Log("HC_CAPTURE switching to Castle from "+Map);
                SelectMap(MapId.Castle);BeginInvestigation();yield break;
            }
            if(!PlaytestDriver.Requested&&!CaptureMode.Requested&&!Application.isBatchMode)
                Debug.Log("HC_WINDOWED switched="+windowSwitched+" mode="+Screen.fullScreenMode+" fullScreen="+Screen.fullScreen+" size="+Screen.width+"x"+Screen.height+" display="+Display.main.systemWidth+"x"+Display.main.systemHeight);
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
            if(uiProbe&&RealAnyKey)Debug.Log("HC_UI_INPUT focused="+Application.isFocused+" mouse="+Input.mousePosition+" screen="+Screen.width+"x"+Screen.height);
            if(!Started&&!Paused&&(RealKey(KeyCode.Return)||RealKey(KeyCode.KeypadEnter)))BeginInvestigation();
            if(!Started&&!Paused&&RealKey(KeyCode.Alpha1))SelectMap(MapId.Bellwether);
            if(!Started&&!Paused&&RealKey(KeyCode.Alpha2))SelectMap(MapId.Castle);
            if(!Started&&!Paused&&RealKey(KeyCode.S)){Paused=true;ApplyFocus();}
            bool scripted=PlaytestDriver.Requested||CaptureMode.Requested;
            if(!scripted&&!Started&&RealKey(KeyCode.Q)){Quit();return;}
            if(!scripted&&(Paused||Case.Terminal))
            {
                if(RealKey(KeyCode.N)){Retry();return;}
                if(RealKey(KeyCode.Q)){Quit();return;}
            }
            if(Paused)
            {
                if(RealKey(KeyCode.Alpha1))Player.Sensitivity=Mathf.Max(.3f,Player.Sensitivity-.15f);
                if(RealKey(KeyCode.Alpha2))Player.Sensitivity=Mathf.Min(4,Player.Sensitivity+.15f);
                if(RealKey(KeyCode.Alpha3))Player.Brightness=Mathf.Max(.7f,Player.Brightness-.1f);
                if(RealKey(KeyCode.Alpha4))Player.Brightness=Mathf.Min(1.8f,Player.Brightness+.1f);
                if(RealKey(KeyCode.Alpha5))Volume=Mathf.Max(0,Volume-.05f);
                if(RealKey(KeyCode.Alpha6))Volume=Mathf.Min(1,Volume+.05f);
                if(RealKey(KeyCode.I))Player.InvertY=!Player.InvertY;
                if(RealKey(KeyCode.T))Subtitles=!Subtitles;
            }
            if(RealKey(KeyCode.H)&&(Started||Paused)&&!Case.Terminal)HelpHints=!HelpHints;
            if(!scripted&&(RealKey(KeyCode.Escape)||(pad&&pad.Cancel&&Modal)))
            {
                if(!Started&&Paused){Paused=false;SavePreferences();}
                else if(NoteBody!=null)NoteBody=null;
                else if(Journal)Journal=false;
                else if(Started&&!Case.Terminal)Paused=!Paused;
                ApplyFocus();
            }
            if((RealKey(KeyCode.J)||(pad&&pad.Journal))&&Started&&!Paused&&NoteBody==null&&!Case.Terminal){Journal=!Journal;ApplyFocus();}
            if(!Modal)
            {
                float dt=Time.deltaTime;if(Map==MapId.Castle)CastleTick(dt);else Case.Tick(dt,Player.Inside);SubtitleTime-=dt;radioCooldown-=dt;
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
            // Bellwether's warning trips the breaker and kills every practical lamp. The
            // castle's candles are its hunt tell (flames lean toward the Count), so they
            // stay lit; only the chapel brazier (the breaker interactable) goes out.
            bool lampsPowered=Case.BreakerOn||Map==MapId.Castle;
            foreach(var lamp in World.Lamps)
            {
                float d=Vector3.Distance(Player.transform.position,lamp.transform.position);
                if(!lampsPowered||darkCircuits.Contains(lamp)||d>=8||Mathf.Abs(Player.transform.position.y-lamp.transform.position.y+2.5f)>=3)continue;
                if(d<nearestDistance){second=nearest;secondDistance=nearestDistance;nearest=lamp;nearestDistance=d;}
                else if(d<secondDistance){second=lamp;secondDistance=d;}
            }
            foreach(var kv in lampLevels)
            {
                bool local=Vector3.Distance(Player.transform.position,kv.Key.transform.position)<8&&Mathf.Abs(Player.transform.position.y-kv.Key.transform.position.y+2.5f)<3;
                kv.Key.enabled=local&&lampsPowered&&!darkCircuits.Contains(kv.Key);
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
        void OnApplicationFocus(bool focus){if(uiProbe)Debug.Log("HC_UI_FOCUS "+focus);if(!focus&&Started&&!Case.Terminal&&!NativeVerification.Requested&&!CaptureMode.Requested&&!PlaytestDriver.Requested){Paused=true;ApplyFocus();}}
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
        public void StartCase()
        {
            if(Loading||Paused)return;
            // The Long Night dial was changed on this castle's title after the case was built: rebuild it.
            if(Map==MapId.Castle&&LongNight!=appliedLongNight){SavePreferences();restoreCheckpoint=false;startOnLoad=true;BeginReload(Map);return;}
            BeginCase();
        }
        void BeginCase(){SavePreferences();Started=true;ApplyFocus();if(uiProbe)Debug.Log("HC_UI_STARTED cursor="+Cursor.lockState);}
        public void Resume(){SavePreferences();Paused=false;Journal=false;NoteBody=null;ApplyFocus();}
        // Owner rule (2026-10-14, "stop taking my mouse"): automated runs never lock or hide the cursor and never read real input.
        // Owner rule: real keyboard input never reaches a scripted run.
        static bool RealKey(KeyCode k)=>!ScriptedRun&&Input.GetKeyDown(k);
        static bool RealAnyKey=>!ScriptedRun&&Input.anyKeyDown;
        // Scripted runs open their own 1920x1080 window whatever the saved prefs say, and put the owner's saved window size back on
        // quit (never full screen).
        static bool scriptedWindowSet;static int ownerWidth,ownerHeight;
        static void ScriptedWindow()
        {
            if(scriptedWindowSet||!ScriptedRun)return;scriptedWindowSet=true;
            ownerWidth=Screen.width;ownerHeight=Screen.height;
            Screen.SetResolution(WindowWidth,WindowHeight,FullScreenMode.Windowed);
            Debug.Log("HC_INPUT scripted=true devices_ignored=keyboard,mouse,gamepad (TestInput only); window 1920x1080, owner window "+ownerWidth+"x"+ownerHeight+" restored on quit");
        }
        public static void RestoreOwnerWindow()
        {
            if(!scriptedWindowSet)return;
            int w=ownerWidth>=640?ownerWidth:WindowWidth,h=ownerHeight>=360?ownerHeight:WindowHeight;
            Screen.SetResolution(w,h,FullScreenMode.Windowed);
        }
        public static bool ScriptedRun=>PlaytestDriver.Requested||CaptureMode.Requested||NativeVerification.Requested;
        public void ApplyFocus(){bool modal=Modal;Time.timeScale=modal?0:1;AudioListener.pause=Loading||!Started||Paused||Journal||NoteBody!=null;Cursor.lockState=modal||ScriptedRun?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=(modal&&!PadInput.Active)||ScriptedRun;if(uiProbe)Debug.Log("HC_UI_STATE started="+Started+" paused="+Paused+" journal="+Journal+" cursor="+Cursor.lockState+" visible="+Cursor.visible+" timeScale="+Time.timeScale);}
        public void ShowNote(string title,string body){NoteTitle=title;NoteBody=body;ApplyFocus();if(Castle)Castle.NoteRead(title);}
        public void Say(string value)
        {
            Log.Add(value);if(Log.Count>30)Log.RemoveAt(0);
            if(FieldMessages.ShowCard(value)){Subtitle=value;SubtitleTime=FieldMessages.CardSeconds(value);}
            if(Sound==null)return;
            if(value.Contains(Book.ArrivalCue))Sound.Speak("arrival");
            else if(value.Contains(Book.KitCue))Sound.Speak("kit");
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
            if(Map==MapId.Castle)
            {
                // Critic r10 #8/#30: the castle radio is no hunt detector and no cc_listen source. During interference, or a
                // Warning/hunt half the time, it gives only static; otherwise a hint (in a hunt, a random one).
                bool danger=Case.Phase==Phase.Warning||Case.HuntActive;
                if(Scares&&Scares.Interference||danger&&UnityEngine.Random.value<.5f){Sound.Cue("radio",Player.transform.position,.45f);Say(Book.RadioInterference);return;}
                if(danger&&!Case.Diagnosed){CastleHint(UnityEngine.Random.Range(0,3));return;}
            }
            else
            {
                if(Scares&&Scares.Interference){Sound.Speak("whisper");Say(Book.RadioInterference);return;}
                if(Case.Phase==Phase.Hunt||Case.Phase==Phase.Warning){Say(Book.RadioHunt);Sound.Speak("warning");return;}
            }
            if(Case.Diagnosed){Say("Dispatch / "+HeldGoal());Sound.Speak(Case.Phase==Phase.Return?"return":"contain");return;}
            string hint=!Case.Evidence.Contains(Evidence.Induction)?"hint_signal":!Case.Evidence.Contains(Evidence.Handprint)?"hint_trace":"hint_voice";
            Say("Dispatch / "+(hint=="hint_signal"?Book.HintSignal:hint=="hint_trace"?Book.HintTrace:Book.HintVoice));Sound.Speak(hint);
        }
        void CastleHint(int which)
        {
            string hint=which==0?"hint_signal":which==1?"hint_trace":"hint_voice";
            Say("Dispatch / "+(which==0?Book.HintSignal:which==1?Book.HintTrace:Book.HintVoice));Sound.Speak(hint);
        }
        // Castle: dread-aware tick (HuntRules.Castle: dice hunts, lies, hostile return) and the
        // torch failing for a second when he dives.
        void CastleTick(float dt)
        {
            if(!candlesCached)CacheCandles();
            litCheck-=dt;if(litCheck<=0){litCheck=.25f;litNearby=CandleWithin(6);}
            bool atFire=hasChapelFire&&Case.BreakerOn&&Vector3.Distance(Player.transform.position,chapelFire)<3;
            // Wave I: event Dread decays 3x faster in the gatehouse, 2x at the chapel fire.
            Case.DreadRelief=Player.InVan?3:atFire?2:1;
            Case.Tick(dt,Player.Inside,litNearby,Player.InVan||atFire);
            rulesLog-=dt;
            if(rulesLog<=0){rulesLog=30;Debug.Log("HC_RULES t="+Case.Elapsed.ToString("F0")+" phase="+Case.Phase+" dread="+Case.Dread.ToString("F1")+" event="+Case.DreadEvent.ToString("F1")+" felt="+Case.FeltDread.ToString("F1")+" runDreadMin="+Case.RunDreadMin.ToString("F1")+" grace="+Case.GraceRemaining.ToString("F0")+" relief="+Case.DreadRelief.ToString("F0")+" governor="+Case.Governor.ToString("F2")+" huntShare="+Case.HuntShare.ToString("F3")+" hunts="+Case.Hunts);}

        }
        // FEAR_PLAN item 7: after a death he has moved the next relic or ledger page (F3's RelicAltPositions).
        // The relic state lives in CastleRun.Forge, which a checkpoint reload rebuilds, so every relic is back in place:
        // the moved one rotates with the death count (CastleRun.MoveRelicToAlternate moves interactable and art).
        void DeathPenalty()
        {
            int pick=(Case.Deaths+2)%3;
            bool moved=Castle&&Castle.MoveRelicToAlternate(pick);if(moved)MovedRelic=pick;
            Debug.Log("HC_DEATH_PENALTY deaths="+Case.Deaths+" dread="+Case.Dread.ToString("F0")+" relic="+pick+" moved="+moved);
        }
        const string CastleReturnSwoop="Dispatch / He is out. He is over the courtyard. Get across that bridge.";
        // Castle (critic r10b N1): every on-demand goal text shows the HUD's held goal, never the hunt goal. Bellwether: Case.Goal.
        public string HeldGoal()
        {
            if(Map!=MapId.Castle)return Case.Goal;
            var ui=GetComponent<Interface>();return ui?ui.HudGoal():Case.Goal;
        }
        IEnumerator SayLater(float seconds,string text){yield return new WaitForSeconds(seconds);if(!Case.Terminal)Say(text);}
        void CountBeat(string id)
        {
            // Critic r10 #34: the hostile return's dispatch card appears with his visible swoop over the courtyard.
            // (A Game constant, not a CaseBook field: RulesTests require every CaseBook string on both maps.)
            // Critic r10b N9: the swoop beat fires inside Seal's Set(Return), before SealedMessage; show it 2 s later so both are read.
            if(id=="return-swoop")StartCoroutine(SayLater(2,CastleReturnSwoop));
        }
        void CacheCandles()
        {
            candlesCached=true;candleLights.Clear();
            foreach(var lamp in World.Lamps)if(lamp)candleLights.Add(lamp);
            foreach(var flame in CandleFlame.All)if(flame&&flame.flameLight&&!candleLights.Contains(flame.flameLight))candleLights.Add(flame.flameLight);
            foreach(var item in World.Root.GetComponentsInChildren<Interactable>(true))if(item.Kind==InteractKind.Breaker){chapelFire=item.transform.position;hasChapelFire=true;break;}
            Debug.Log("HC_FEAR_CANDLES lights="+candleLights.Count+" chapelFire="+hasChapelFire);
        }
        bool CandleWithin(float radius)
        {
            var p=Player.transform.position;float r2=radius*radius;
            foreach(var l in candleLights)if(l&&l.isActiveAndEnabled&&l.intensity>.05f&&(l.transform.position-p).sqrMagnitude<r2)return true;
            return false;
        }
        // A short action card that is not dialogue (FieldMessages would drop it): "The door is held."
        public void Notice(string value,float seconds=2.6f){Log.Add(value);if(Log.Count>30)Log.RemoveAt(0);Subtitle=value;SubtitleTime=seconds;}
        void PhaseChanged(Phase phase)
        {
            var previous=lastPhase;lastPhase=phase;
            if(Map==MapId.Castle)
            {
                // False warnings strobe and radio like real ones (Investigation never turns them into a hunt);
                // no-tell hunts skip the Warning phase, so nothing here strobes or speaks for them.
                if(phase==Phase.Warning)Debug.Log("HC_WARNING false="+Case.FalseWarning+" escalated="+Case.EscalatedWarning+" length="+Case.CurrentWarningSeconds.ToString("F1")+" threatFrom="+Case.ThreatFrom+" dread="+Case.Dread.ToString("F1"));
                if(previous==Phase.Warning&&phase!=Phase.Hunt&&!Case.Terminal)Debug.Log("HC_FALSE_WARNING ended phase="+phase);
                if(phase==Phase.Hunt||phase==Phase.Return&&Case.HuntActive)Debug.Log("HC_HUNT_START t="+Case.Elapsed.ToString("F0")+" phase="+phase+" noTell="+Case.NoTellHunt+" from="+previous+" escalated="+Case.EscalatedWarning+" threatFrom="+Case.ThreatFrom+" dread="+Case.Dread.ToString("F1")+" felt="+Case.FeltDread.ToString("F1")+" runDreadMin="+Case.RunDreadMin.ToString("F1")+" huntSeconds="+Case.CurrentHuntSeconds.ToString("F1")+" breaker="+Case.BreakerOn+" hunts="+Case.Hunts);
                if(phase==Phase.Recovery)Debug.Log("HC_RECOVERY seconds="+Case.CurrentRecoverySeconds.ToString("F1")+" grace="+Case.GraceSeconds.ToString("F1")+" huntDreadAdded="+Case.HuntDreadAdded.ToString("F1")+" huntShare="+Case.HuntShare.ToString("F3")+" governor="+Case.GovernorAtLastGrace.ToString("F2")+" governorShare="+Case.GovernorShare.ToString("F3")+" dread="+Case.Dread.ToString("F1")+" felt="+Case.FeltDread.ToString("F1"));
            }
            Creature?.PhaseChanged(phase);
            if(phase==Phase.Disturbance&&!Case.Survived&&!evidenceAppearanceScheduled)
            {evidenceAppearanceScheduled=true;StartCoroutine(RevealFirstRecord());}
            if(phase==Phase.Warning)
            {
                // Castle escalation (a 1.5 s tell after a close apparition) strobes but has no dispatch line.
                bool escalation=Map==MapId.Castle&&Case.EscalatedWarning; // wave H: told/false warnings are drawn 5-14 s
                Scares?.Pulse(Player.transform.position+Player.transform.forward*3,Map==MapId.Castle?Case.CurrentWarningSeconds:9);Sound.Cue("knock",Player.transform.position+Vector3.forward*3);if(!escalation)Sound.Speak("warning");
            }
            // Castle hostile return (critic r10 #34): the Count's swoop speaks cc_dispatch (voice_cc_dispatch) and raises the
            // "return-swoop" beat, which shows the card (CountBeat). Bellwether keeps "return".
            if(phase==Phase.Return&&!(Map==MapId.Castle&&Case.Rules.HostileReturn))Sound.Speak("return");
            // Castle: the sealed bell lowers the drawbridge for the escape.
            if(phase==Phase.Return&&Map==MapId.Castle&&World.Castle!=null&&World.Castle.Drawbridge)
            {
                // Hostile return (FEAR_PLAN item 4): the bridge takes the whole chain clip to come down (landing thud at ~7.2 s),
                // so the escape is a wait under the hunt, not a 3 s formality.
                if(Case.Rules.HostileReturn)World.Castle.Drawbridge.DegreesPerSecond=CastleDrawbridge.RaisedAngle/7.2f;
                World.Castle.Drawbridge.Lower();
            }
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
            if(ScriptedRun){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            if(Case.Phase!=Phase.Dead||Creature.PresentationRemaining<=0)return;
            var direction=Creature.Actor.Head.position-Player.View.transform.position;
            if(direction.sqrMagnitude>.04f)Player.View.transform.rotation=Quaternion.Slerp(Player.View.transform.rotation,Quaternion.LookRotation(direction),Time.unscaledDeltaTime*3);
        }
        // In the castle, or choosing it on the title (its CASTLE NIGHT page is open to the player).
        bool CastleSession=>Map==MapId.Castle||!Started&&SelectedMap==MapId.Castle;
        public void SavePreferences()
        {
            if(!Player)return;
            bool changed=!Mathf.Approximately(PlayerPrefs.GetFloat("hc.subtitle-scale",-1),SubtitleScale)||!Mathf.Approximately(PlayerPrefs.GetFloat("hc.card-opacity",-1),CardOpacity)||!Mathf.Approximately(PlayerPrefs.GetFloat("hc.sensitivity",-1),Player.Sensitivity)||!Mathf.Approximately(PlayerPrefs.GetFloat("hc.brightness",-1),Player.Brightness)||!Mathf.Approximately(PlayerPrefs.GetFloat("hc.volume",-1),Volume)||PlayerPrefs.GetInt("hc.invert-y",-1)!=(Player.InvertY?1:0)||PlayerPrefs.GetInt("hc.subtitles",-1)!=(Subtitles?1:0)||PlayerPrefs.GetInt("hc.help",-1)!=(HelpHints?1:0)
                ||!Automated&&CastleSession&&(PlayerPrefs.GetInt(LongNightKey,0)!=(LongNight?1:0)||PlayerPrefs.GetInt(MicKey,0)!=(MicEnabled?1:0)||!Mathf.Approximately(PlayerPrefs.GetFloat(MicThresholdKey,.02f),MicThreshold));
            if(!changed)return;
            PlayerPrefs.SetFloat("hc.subtitle-scale",SubtitleScale);PlayerPrefs.SetFloat("hc.card-opacity",CardOpacity);PlayerPrefs.SetFloat("hc.sensitivity",Player.Sensitivity);PlayerPrefs.SetFloat("hc.brightness",Player.Brightness);PlayerPrefs.SetFloat("hc.volume",Volume);
            PlayerPrefs.SetInt("hc.invert-y",Player.InvertY?1:0);PlayerPrefs.SetInt("hc.subtitles",Subtitles?1:0);PlayerPrefs.SetInt("hc.help",HelpHints?1:0);
            // Castle settings are written from castle sessions only (critic r10 #25).
            if(!Automated&&CastleSession){PlayerPrefs.SetInt(LongNightKey,LongNight?1:0);PlayerPrefs.SetInt(MicKey,MicEnabled?1:0);PlayerPrefs.SetFloat(MicThresholdKey,MicThreshold);}
            PlayerPrefs.Save();
        }
        public void Quit(){SavePreferences();if(Automated)Debug.Log("HC_QUIT_CALLED "+Environment.StackTrace);Application.Quit();}
        void OnApplicationQuit(){RestoreOwnerWindow();if(PlaytestDriver.Requested||CaptureMode.Requested)Debug.Log("HC_APP_QUIT t="+Time.realtimeSinceStartup.ToString("F1")+" map="+Map+" started="+Started+" paused="+Paused);}
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
