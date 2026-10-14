using UnityEngine;

namespace HollowCurrent
{
    // Optional local test input, consumed by exactly the same controller/tool path.
    // Never enabled by the normal game launch and never sends OS input.
    public sealed class FieldInput
    {
        public Vector2 Move;
        public Vector3? Look;
        public bool Use, UseHeld, Fire, FireHeld, Torch, Radio, Sprint, Crouch;
        public int SelectTool;
    }
    [RequireComponent(typeof(CharacterController))]
    public sealed class Investigator : MonoBehaviour
    {
        public Game Game;
        public Camera View;
        public Light Torch, Ultraviolet;
        Light hiddenVision;
        float hideYaw;
        public CharacterController Controller;
        public Interactable Focus;
        public bool Hidden { get; private set; }
        public Vector3 HideExit => hiding==null?transform.position:hiding.ExitPoint;
        public bool TorchOn { get; private set; } = true;
        public bool Running { get; private set; }
        public bool Moving { get; private set; }
        public bool Crouched { get; private set; }
        public float Stamina { get; private set; } = 1;
        public int Tool { get; private set; } = 1;
        public float Capture { get; private set; }
        public string Instrument { get; private set; } = "FIELD KIT NOT COLLECTED";
        public float Sensitivity = 1.65f;
        public bool InvertY;
        public float Brightness = 1;
        // Map regions come from the active World (Bellwether: house / van; Castle: walls / gatehouse).
        public bool Inside => Game.World.IsInside(transform.position);
        public bool InVan => Game.World.IsSafe(transform.position);
        float pitch,vertical,footstep,evp,scanAngle,readerPulse;
        Vector3 lastForward;
        Interactable hiding;
        Transform heldTool;
        FirstPersonHands workingHand,radioHand,leftHand;
        float equipPose=1;int priorTool=1;
        readonly Transform[] instrumentModels=new Transform[3];
        // Castle: the forged moonstone sword replaces the held instrument (hc/sword pose).
        Transform sword;float swing;
        static readonly Vector3 SwordPosition=new Vector3(.24f,-.20f,.45f);
        // The FBX export maps Blender Z (blade) to Unity +Y and the flat faces to +-Z, so pose by
        // blade direction and face normal in camera space: idle rises up and forward-left with the
        // rune face toward the view; the strike sweeps it down and across to the left.
        Quaternion SwordIdle,SwordStrike; // set in MakeSword (no Unity math in field initialisers)
        static Quaternion SwordPose(Vector3 blade,Vector3 face){blade.Normalize();return Quaternion.LookRotation(Vector3.ProjectOnPlane(face,blade).normalized,blade);}
        public bool SwordHeld=>sword&&sword.gameObject.activeSelf;
        Transform torchModel;
        Transform radioModel;
        TextMesh inductionReadout;
        // Castle hollow compass: a needle over the reader's face points at the compass spot and spins at nothing
        // while CastleScares.FalseCompass is set (FEAR_PLAN item 5).
        Transform needle;
        public bool NeedleSpinning {get;private set;}
        // Item 19: opt-in microphone (castle only, never in automated runs). One Microphone.Start per enable,
        // 16 kHz, 1 s ring buffer, RMS every 0.1 s; above Game.MicThreshold during a hunt he hears you at 9 m.
        AudioClip micClip;bool micTried;float micPoll,micHeardAt=-99;
        readonly float[] micSamples=new float[1600];
        public float MicLevel {get;private set;}
        public bool MicLive=>micClip!=null;
        public int MicHeard {get;private set;}
        float radioPose;
        public string HeldAssetName => radioPose>0?"FieldRadio":FieldKitArt.Names[Tool-1];
        public int HeldVertexCount => FieldKitArt.VertexCount(radioPose>0?radioModel:instrumentModels[Tool-1]);
        public int FlashlightVertexCount => FieldKitArt.VertexCount(torchModel);
        public string InductionDisplay => inductionReadout.text;
        public FieldInput TestInput;
        // This frame's interact-hold and instrument-press (live or test input).
        public bool UseHeld=>input!=null&&input.UseHeld;
        public bool FirePressed=>input!=null&&input.Fire;
        FieldInput input;
        readonly FieldInput liveInput=new FieldInput();
        static readonly string[] InductionLevels={"0.0","1.0","2.0","3.0","4.0","5.0"};
        public void Initialize(Game game)
        {
            // The head camera can cast through the capsule's rounded shoulder.
            // Keep player collision, but exclude our own capsule from query rays.
            gameObject.layer=2; // Unity's built-in Ignore Raycast layer.
            Game=game;Controller=GetComponent<CharacterController>();Controller.height=1.8f;Controller.radius=.27f;Controller.center=Vector3.up*.9f;Controller.stepOffset=.26f;Controller.slopeLimit=48;
            var cam=new GameObject("Investigator camera");cam.transform.SetParent(transform,false);cam.transform.localPosition=Vector3.up*1.64f;
            View=cam.AddComponent<Camera>();View.nearClipPlane=.04f;View.farClipPlane=75;View.fieldOfView=72;View.backgroundColor=new Color(.025f,.035f,.055f);View.clearFlags=CameraClearFlags.SolidColor;cam.AddComponent<AudioListener>();
            // Instruments have a separate depth buffer so approaching a wall
            // cannot cut the hand/tool mesh away while its LCD remains visible.
            View.cullingMask&=~(1<<30);
            var overlay=new GameObject("First-person equipment camera");overlay.transform.SetParent(cam.transform,false);
            var equipmentView=overlay.AddComponent<Camera>();equipmentView.clearFlags=CameraClearFlags.Depth;equipmentView.cullingMask=1<<30;equipmentView.depth=View.depth+1;equipmentView.fieldOfView=View.fieldOfView;equipmentView.nearClipPlane=.01f;equipmentView.farClipPlane=2;equipmentView.useOcclusionCulling=false;
            Torch=Lamp("Independent flashlight",new Color(1,.89f,.72f),52,20,2.8f);Torch.shadows=LightShadows.Hard;
            Ultraviolet=Lamp("Ultraviolet emitter",new Color(.49f,.1f,1),65,5,1.8f);
            // World beams must not illuminate the back of their own instrument.
            Torch.cullingMask&=~(1<<30);Ultraviolet.cullingMask&=~(1<<30);
            BuildInstruments(cam.transform);
            var adaptation=new GameObject("Dim cabinet dark adaptation");adaptation.transform.SetParent(cam.transform,false);
            adaptation.transform.localPosition=new Vector3(0,0,.48f);
            hiddenVision=adaptation.AddComponent<Light>();hiddenVision.type=LightType.Point;hiddenVision.range=2.6f;
            hiddenVision.color=new Color(.48f,.55f,.64f);hiddenVision.intensity=.09f;hiddenVision.shadows=LightShadows.None;
            hiddenVision.cullingMask&=~(1<<30);hiddenVision.enabled=false;
        }
        void BuildInstruments(Transform camera)
        {
            heldTool=new GameObject("Field instrument mount").transform;heldTool.SetParent(camera,false);
            heldTool.localPosition=new Vector3(.28f,-.16f,.42f);heldTool.localRotation=Quaternion.Euler(8,-8,0);
            float[] lengths={.19f,.19f,.17f};
            for(int i=0;i<3;i++)instrumentModels[i]=FieldKitArt.Create(heldTool,FieldKitArt.Names[i],lengths[i],Vector3.zero,Quaternion.identity);
            var readout=new GameObject("Live induction LCD");readout.transform.SetParent(instrumentModels[0],false);readout.transform.localPosition=new Vector3(0,.044f,-.041f);readout.layer=30;
            inductionReadout=readout.AddComponent<TextMesh>();inductionReadout.text="0.0";inductionReadout.fontSize=32;inductionReadout.characterSize=.0048f;inductionReadout.anchor=TextAnchor.MiddleCenter;inductionReadout.alignment=TextAlignment.Center;inductionReadout.color=new Color(.35f,.9f,.48f);
            readout.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            if(Game.Map==MapId.Castle)
            {
                var n=GameObject.CreatePrimitive(PrimitiveType.Cube);n.name="Hollow compass needle";Destroy(n.GetComponent<Collider>());n.layer=30;
                needle=n.transform;needle.SetParent(readout.transform,false);needle.localPosition=new Vector3(0,0,-.002f);needle.localScale=new Vector3(.028f,.0022f,.0008f);
                var r=n.GetComponent<Renderer>();r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
                var shader=Shader.Find("Sprites/Default");if(!shader)shader=Shader.Find("UI/Default");if(shader)r.sharedMaterial=new Material(shader){color=new Color(.86f,.22f,.16f)};
            }
            instrumentModels[1].localRotation=Quaternion.Euler(0,180,0); // UV emitter points into the scene.
            workingHand=FirstPersonHands.Attach(heldTool,new Vector3(.052f,-.050f,-.003f),new Vector3(4,-15,82));
            // The exported flashlight lies on X with its broad reflector at -X.
            torchModel=FieldKitArt.Create(camera,"Flashlight",.22f,new Vector3(-.20f,-.24f,.44f),Quaternion.Euler(0,90,0));
            radioModel=FieldKitArt.Create(camera,"FieldRadio",.20f,new Vector3(.28f,-.16f,.42f),Quaternion.Euler(8,-8,0));
            radioHand=FirstPersonHands.Attach(radioModel,new Vector3(.052f,-.049f,-.003f),new Vector3(4,-15,82));
            leftHand=FirstPersonHands.Attach(camera,new Vector3(-.19f,-.265f,.385f),new Vector3(-12,15,-78),true);
            var fill=new GameObject("Soft field-kit bounce");fill.transform.SetParent(camera,false);fill.transform.localPosition=new Vector3(.03f,.05f,-.08f);
            var light=fill.AddComponent<Light>();light.type=LightType.Point;light.range=1.1f;light.intensity=.18f;light.color=new Color(.82f,.9f,1);light.cullingMask=1<<30;light.shadows=LightShadows.None;
        }
        Transform MakeSword()
        {
            SwordIdle=SwordPose(new Vector3(-.22f,.80f,.56f),new Vector3(.35f,0,-1));SwordStrike=SwordPose(new Vector3(-.75f,.05f,.66f),new Vector3(0,1,-.35f));
            var t=(Transform)CastleArt.Place(CastleArt.Names.MoonstoneSword,View.transform,SwordPosition,SwordIdle);
            foreach(var c in t.GetComponentsInChildren<Collider>())Destroy(c);
            foreach(var r in t.GetComponentsInChildren<Renderer>()){r.gameObject.layer=30;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
            t.gameObject.layer=30;Debug.Log("HC_SWORD_HELD renderers="+t.GetComponentsInChildren<Renderer>().Length);
            return t;
        }
        Light Lamp(string label,Color color,float angle,float range,float power)
        {
            var g=new GameObject(label);g.transform.SetParent(View.transform,false);g.transform.localPosition=new Vector3(.08f,-.05f,.04f);var l=g.AddComponent<Light>();l.type=LightType.Spot;l.color=color;l.spotAngle=angle;l.innerSpotAngle=angle*.6f;l.range=range;l.intensity=power;return l;
        }
        void Update()
        {
            if(Game==null)return;
            UpdateMicrophone();
            Torch.enabled=Game.Case.Equipped&&TorchOn&&!(Game.Scares&&Game.Scares.TorchFailed);Torch.intensity=.85f*Brightness;
            Ultraviolet.enabled=Game.Case.Equipped&&Tool==2&&!Hidden;
            hiddenVision.enabled=Hidden&&!Game.Modal;hiddenVision.intensity=.09f*Brightness;
            bool swordOut=Game.Castle&&Game.Castle.Forge.HasSword&&Game.Case.Equipped&&!Game.Modal&&!Hidden;
            if(swordOut&&!sword)sword=MakeSword();
            if(sword)
            {
                sword.gameObject.SetActive(swordOut&&radioPose<=0);
                if(FirePressed&&swing<=0)swing=.36f;
                swing=Mathf.Max(0,swing-Time.deltaTime);float s=swing>.18f?(.36f-swing)/.18f:swing/.18f;
                sword.localPosition=SwordPosition+new Vector3(0,Mathf.Sin(Time.time*(Moving?7:1.6f))*(Moving?.0035f:.001f),0);
                sword.localRotation=Quaternion.Slerp(SwordIdle,SwordStrike,Mathf.SmoothStep(0,1,s));
            }
            heldTool.gameObject.SetActive(Game.Case.Equipped&&!Game.Modal&&!Hidden&&radioPose<=0&&!SwordHeld);
            radioModel.gameObject.SetActive(Game.Case.Equipped&&!Game.Modal&&!Hidden&&radioPose>0);
            torchModel.gameObject.SetActive(Game.Case.Equipped&&!Game.Modal&&!Hidden);
            leftHand.gameObject.SetActive(Game.Case.Equipped&&!Game.Modal&&!Hidden);
            for(int i=0;i<instrumentModels.Length;i++)instrumentModels[i].gameObject.SetActive(i==Tool-1);
            if(Game.Modal||Game.Case.Terminal){Moving=false;Running=false;Focus=null;return;}
            float dt=Time.deltaTime;radioPose=Mathf.Max(0,radioPose-dt);
            if(Tool!=priorTool){equipPose=0;priorTool=Tool;}
            equipPose=Mathf.MoveTowards(equipPose,1,dt*4.8f);
            float lowered=(1-Mathf.SmoothStep(0,1,equipPose))*.25f;
            heldTool.localPosition=new Vector3(.28f,-.16f-lowered+Mathf.Sin(Time.time*(Moving?7:1.6f))*(Moving?.0035f:.001f),.42f);
            heldTool.localRotation=Quaternion.Euler(8+lowered*90,-8,Mathf.Sin(Time.time*1.7f)*.4f);
            float raised=Mathf.Min(Mathf.Clamp01((1.35f-radioPose)/.16f),Mathf.Clamp01(radioPose/.2f));
            radioModel.localPosition=new Vector3(.28f,-.16f-(1-raised)*.14f,.42f);
            if(TestInput==null)
            {
                liveInput.Move=new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));liveInput.Use=Input.GetKeyDown(KeyCode.E);liveInput.UseHeld=Input.GetKey(KeyCode.E);
                liveInput.Fire=Input.GetMouseButtonDown(0);liveInput.FireHeld=Input.GetMouseButton(0);liveInput.Torch=Input.GetKeyDown(KeyCode.F);liveInput.Radio=Input.GetKeyDown(KeyCode.R);
                liveInput.Sprint=Input.GetKey(KeyCode.LeftShift);liveInput.Crouch=Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.C);liveInput.SelectTool=0;liveInput.Look=null;
                var pad=PadInput.Current;
                if(pad)pad.Apply(liveInput,Tool);
            }
            input=TestInput??liveInput;
            workingHand.Operate(input.Fire||input.FireHeld);radioHand.Operate(radioPose>0);
            if(TestInput==null)
            {
                transform.Rotate(0,Input.GetAxisRaw("Mouse X")*Sensitivity,0);
                pitch=Mathf.Clamp(pitch+Input.GetAxisRaw("Mouse Y")*Sensitivity*(InvertY?1:-1),-84,84);
                var pad=PadInput.Current;
                if(pad&&pad.Connected)
                {
                    var delta=pad.LookDelta(Sensitivity,InvertY,dt);transform.Rotate(0,delta.x,0);
                    pitch=Mathf.Clamp(pitch+delta.y,-84,84);
                }
                for(int i=1;i<=3;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0+i)))input.SelectTool=i;
            }
            else if(input.Look.HasValue)
            {
                var direction=input.Look.Value-View.transform.position;
                transform.rotation=Quaternion.Euler(0,Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,0);
                pitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;
            }
            if(Hidden)
            {
                float yaw=hideYaw+Mathf.Clamp(Mathf.DeltaAngle(hideYaw,transform.eulerAngles.y),-32,32);
                transform.rotation=Quaternion.Euler(0,yaw,0);pitch=Mathf.Clamp(pitch,-16,16);
            }
            View.transform.localRotation=Quaternion.Euler(pitch,0,0);
            if(input.Torch&&Game.Case.Equipped)TorchOn=!TorchOn;
            if(input.Radio){radioPose=1.35f;Game.RadioHint();Game.Sound.Cue("radio",transform.position);Game.Creature.Hear(transform.position,11);}
            if(input.SelectTool>=1&&input.SelectTool<=3&&input.SelectTool!=Tool){Tool=input.SelectTool;Capture=0;evp=0;}
            if(Hidden)
            {
                Moving=false;Running=false;Focus=null;
                if(input.Use)ExitHide();
            }
            else
            {
                Move(dt);
                Focus=null;
                if(Physics.Raycast(View.transform.position,View.transform.forward,out var hit,2.7f))Focus=hit.collider.GetComponent<Interactable>();
                if(input.Use&&Focus!=null)Focus.Use(Game);
            }
            UseEquipment(dt);
            bool sealing=Focus!=null&&Focus.Kind==InteractKind.Anchor&&input.UseHeld;
            Game.Case.Seal(dt,sealing,Vector3.Distance(transform.position,Game.World.Anchor)<3);
            if(Inside)Game.Case.EnterHouse();
            if(transform.position.y < -8)Teleport(Game.World.Spawn);
        }
        void Move(float dt)
        {
            Crouched=input.Crouch;
            float h=Crouched?1.15f:1.8f;
            if(!Crouched&&Controller.height<1.7f&&Physics.SphereCast(transform.position+Vector3.up*.8f,.25f,Vector3.up,out _,.9f))h=1.15f;
            Controller.height=h;Controller.center=Vector3.up*h*.5f;
            Vector3 movement=Vector3.ClampMagnitude(new Vector3(input.Move.x,0,input.Move.y),1);
            Moving=movement.sqrMagnitude>.01f;
            Running=input.Sprint&&Moving&&!Crouched&&Stamina>.04f;
            Stamina=Mathf.Clamp01(Stamina+(Running?-.16f:.1f)*dt);
            float speed=Crouched?1.25f:Running?3.9f:2.35f;
            if(Controller.isGrounded&&vertical<0)vertical=-2;
            vertical-=18*dt;
            Controller.Move((transform.TransformDirection(movement)*speed+Vector3.up*vertical)*dt);
            View.transform.localPosition=Vector3.Lerp(View.transform.localPosition,Vector3.up*(h-.16f),dt*12);
            if(Moving&&Controller.isGrounded)
            {
                footstep-=dt;
                if(footstep<=0){footstep=Running?.3f:Crouched?.8f:.51f;Game.Sound.Cue("step",transform.position,.22f);Game.Creature.Hear(transform.position,Running?10:Crouched?1.8f:4.5f);}
            }
        }
        bool AimAt(Vector3 point,float range,float cone)
        {
            Vector3 delta=point-View.transform.position;
            if(delta.magnitude>range||Vector3.Angle(View.transform.forward,delta)>cone)return false;
            if(Physics.Raycast(View.transform.position,delta.normalized,out var hit,delta.magnitude-.12f))return false;
            return true;
        }
        void UpdateNeedle()
        {
            if(!needle)return;
            NeedleSpinning=FearLink.FalseCompass(Game.Scares.Castle);
            float angle;
            if(NeedleSpinning)angle=Time.time*840+Mathf.Sin(Time.time*13)*40;
            else
            {
                var to=Vector3.ProjectOnPlane(Game.World.Induction-transform.position,Vector3.up);
                angle=to.sqrMagnitude<.01f?0:-Vector3.SignedAngle(transform.forward,to,Vector3.up)+90;
            }
            needle.localRotation=Quaternion.Euler(0,0,angle);
        }
        void UpdateMicrophone()
        {
            bool want=Game.Map==MapId.Castle&&Game.MicEnabled&&Game.Started&&!Game.Automated;
            if(!want){StopMicrophone();return;}
            if(micClip==null)
            {
                if(micTried)return;micTried=true;
                if(Microphone.devices.Length==0){Debug.Log("HC_MIC no device");return;}
                micClip=Microphone.Start(null,true,1,16000);Debug.Log("HC_MIC start device="+Microphone.devices[0]+" ok="+(micClip!=null));return;
            }
            micPoll-=Time.unscaledDeltaTime;if(micPoll>0)return;micPoll=.1f;
            int position=Microphone.GetPosition(null);if(position<=0)return;
            int start=position-micSamples.Length;if(start<0)start+=micClip.samples;
            if(!micClip.GetData(micSamples,start))return; // GetData wraps past the end of the 1 s ring
            float sum=0;foreach(float v in micSamples)sum+=v*v;
            MicLevel=Mathf.Sqrt(sum/micSamples.Length);
            if(!Game.Modal&&FearLink.HuntActive(Game.Case)&&MicLevel>Game.MicThreshold&&Time.time-micHeardAt>.5f)
            {micHeardAt=Time.time;MicHeard++;Game.Creature.Hear(transform.position,9f);Debug.Log("HC_MIC heard rms="+MicLevel.ToString("F3"));}
        }
        void StopMicrophone()
        {
            micTried=false;MicLevel=0;if(micClip==null)return;
            Microphone.End(null);micClip=null;Debug.Log("HC_MIC stop");
        }
        void OnDestroy()=>StopMicrophone();
        void OnApplicationQuit()=>StopMicrophone();
        void UseEquipment(float dt)
        {
            UpdateNeedle();
            if(NeedleSpinning&&Tool==1)inductionReadout.text=InductionLevels[Game.Scares.FalseReading];
            if(Game.Case.Phase==Phase.Warning||Game.Case.Phase==Phase.Hunt||Game.Scares.Interference)inductionReadout.text=InductionLevels[Game.Scares.FalseReading];
            if(!Game.Case.Equipped||Hidden){Game.World.Trace.gameObject.SetActive(false);return;}
            if(Game.Case.Phase==Phase.Warning||Game.Case.Phase==Phase.Hunt||Game.Scares.Interference)
            {
                Capture=0;evp=0;Instrument="INTERFERENCE · RECORDING SUSPENDED · BREAK LINE OF SIGHT";
                Game.World.Trace.gameObject.SetActive(false);return;
            }
            scanAngle=Vector3.Angle(lastForward,View.transform.forward);lastForward=View.transform.forward;
            bool holding=input.FireHeld,valid=false;Evidence evidence=Evidence.Induction;
            float dist=Vector3.Distance(View.transform.position,Game.World.Induction);
            if(Tool==1)
            {
                bool near=AimAt(Game.World.Induction,3.4f,19);
                int reading=near?5:Mathf.Clamp(Mathf.CeilToInt((8-dist)*.55f),0,4);
                string display=InductionLevels[NeedleSpinning?Game.Scares.FalseReading:reading];if(inductionReadout.text!=display)inductionReadout.text=display;
                readerPulse-=dt;
                if(reading>0&&readerPulse<=0){readerPulse=1.35f-reading*.21f;Game.Sound.Cue("confirm",View.transform.position,.055f);}

                Instrument=near?Game.Book.ReaderNear:dist<7?Game.Book.ReaderClose:Game.Book.ReaderIdle;
                valid=near&&scanAngle<1.7f;
            }
            bool trace=Tool==2&&AimAt(Game.World.Print,4.5f,25);
            Game.World.Trace.gameObject.SetActive(trace||Game.Case.Evidence.Contains(Evidence.Handprint)&&Tool==2&&Vector3.Distance(View.transform.position,Game.World.Print)<5);
            if(Tool==2){Instrument=trace?Game.Book.TraceFound:Game.Book.TraceIdle;valid=trace;evidence=Evidence.Handprint;}
            if(Tool==3)
            {
                Instrument=PadInput.Active?Game.Book.AskPad:Game.Book.AskKey;
                if(input.Fire&&evp<=0){evp=4.2f;Game.Say(Game.Book.Question);Game.Sound.Cue("radio",transform.position);Game.Creature.Hear(transform.position,8);}
                if(evp>0)
                {
                    evp-=dt;Instrument=Game.Book.Listening;
                    if(evp<=0)
                    {
                        if(Vector3.Distance(transform.position,Game.World.Voice)<4&&!Moving&&Game.Case.Phase!=Phase.Hunt)
                        {Game.Case.Record(Evidence.Voice);Game.Say(Game.Book.Reply);Game.Sound.Cue("voice",Game.World.Voice);}
                        else Game.Say(Game.Book.NoReply);
                    }
                }
                Capture=0;return;
            }
            if(valid&&holding)Capture+=dt/3;else Capture=Mathf.Max(0,Capture-dt*.8f);
            if(Capture>=1){if(Game.Case.Record(evidence))Game.Sound.Cue("evidence_filed",transform.position,.45f);Capture=0;}
        }
        public void Hide(Interactable spot)
        {
            if(Game.Case.Phase==Phase.Briefing)return;
            if(Game.Map==MapId.Castle)
            {
                if(spot.Nailed){Game.Notice("Nailed shut.");Game.Sound.Cue("knock",spot.transform.position,.35f);return;}
                // Above the DoorHeld threshold something holds the door from inside (FEAR_PLAN item 5).
                if(FearLink.DoorHeld(Game.Case)){Game.Notice("The door is held.");Game.Sound.Cue("door_rattle",spot.transform.position,.8f);Debug.Log("HC_DOOR_HELD hide="+spot.name);return;}
            }
            Game.Creature.WitnessHide(spot.HidePoint);
            hiding=spot;Hidden=true;Teleport(spot.HidePoint);
            var outward=Vector3.ProjectOnPlane(spot.ExitPoint-spot.HidePoint,Vector3.up).normalized;
            hideYaw=Quaternion.LookRotation(outward).eulerAngles.y;transform.rotation=Quaternion.Euler(0,hideYaw,0);
            pitch=0;View.transform.localRotation=Quaternion.identity;View.transform.localPosition=new Vector3(0,1.65f,.18f);
            Crouched=false;Moving=false;Running=false;
        }
        void ExitHide(){Game.Creature.LeaveHide();Hidden=false;Teleport(hiding.ExitPoint);View.transform.localPosition=Vector3.up*1.64f;hiddenVision.enabled=false;hiding=null;}
        public void Teleport(Vector3 point){Controller.enabled=false;transform.position=point;Controller.enabled=true;vertical=0;}
    }
}
