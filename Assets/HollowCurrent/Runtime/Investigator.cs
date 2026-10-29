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
        public bool SwordEquipped=>Game.Castle&&Game.Castle.SwordRoute&&Game.Castle.Forge.HasSword;
        public float Capture { get; private set; }
        public string Instrument { get; private set; } = "FIELD KIT NOT COLLECTED";
        public float Sensitivity = 1.65f;
        public bool InvertY;
        public float Brightness = 1;
        // Multiplier on the torch intensity set by the castle's light tuning (CastleLightTuning.TorchIntensityScale); 1 = Bellwether.
        public float TorchScale = 1;
        // ff-count (r14: hide grabs were near black): Presence sets this to 1 for a hide grab's presentation (the door torn open, her lantern
        // swings up): the hand lantern's beam is lit whatever TorchOn or a torch failure says, plus a warm glow at the lantern in her hand.
        public float LanternRaise;
        public float LanternGlowIntensity = 1.6f, LanternGlowRange = 5.5f;
        Light lanternGlow;
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
        CastleInstrumentArt.CompassParts compassParts;
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
        readonly FieldInput idleInput=new FieldInput(); // scripted runs before TestInput is set: no input at all
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
            var glow=new GameObject("Raised lantern glow");glow.transform.SetParent(cam.transform,false);glow.transform.localPosition=new Vector3(-.22f,-.16f,.42f);
            lanternGlow=glow.AddComponent<Light>();lanternGlow.type=LightType.Point;lanternGlow.color=new Color(1f,.74f,.46f);lanternGlow.shadows=LightShadows.None;
            lanternGlow.cullingMask&=~(1<<30);lanternGlow.enabled=false;
        }
        void BuildInstruments(Transform camera)
        {
            heldTool=new GameObject("Field instrument mount").transform;heldTool.SetParent(camera,false);
            heldTool.localPosition=new Vector3(.28f,-.16f,.42f);heldTool.localRotation=Quaternion.Euler(8,-8,0);
            float[] lengths={.19f,.19f,.17f};
            // fc-props: the castle holds period instruments (brass compass, grave-ash lantern, vellum ear-trumpet) instead of the field-kit meshes Bellwether keeps.
            bool period=Game.Map==MapId.Castle;
            for(int i=0;i<3;i++)
            {
                if(period){instrumentModels[i]=CastleInstrumentArt.Create(i,heldTool,out var made);if(made!=null)compassParts=made;}
                else instrumentModels[i]=FieldKitArt.Create(heldTool,FieldKitArt.Names[i],lengths[i],Vector3.zero,Quaternion.identity);
            }
            var readout=new GameObject("Live induction LCD");readout.transform.SetParent(instrumentModels[0],false);readout.transform.localPosition=new Vector3(0,.044f,-.041f);readout.layer=30;
            inductionReadout=readout.AddComponent<TextMesh>();inductionReadout.text="0.0";inductionReadout.fontSize=32;inductionReadout.characterSize=.0048f;inductionReadout.anchor=TextAnchor.MiddleCenter;inductionReadout.alignment=TextAlignment.Center;inductionReadout.color=new Color(.35f,.9f,.48f);
            readout.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            if(Game.Map==MapId.Castle)
            {
                // fc-props: the period compass has no LCD. The number stays as data (InductionDisplay, the HUD ledger text); the player reads the
                // needle (it trembles harder as the reading rises) and the five ember pips on the dial.
                readout.GetComponent<Renderer>().enabled=false;
                if(compassParts!=null)needle=compassParts.Needle;
            }
            else instrumentModels[1].localRotation=Quaternion.Euler(0,180,0); // UV emitter points into the scene.
            workingHand=FirstPersonHands.Attach(heldTool,new Vector3(.052f,-.050f,-.003f),new Vector3(4,-15,82));
            // The exported flashlight lies on X with its broad reflector at -X.
            // fc-light2: in the castle the left hand carries the brass hand lantern (the castle's light source) instead of the modern flashlight mesh; Bellwether keeps the flashlight.
            if(period){torchModel=CastleInstrumentArt.CreateHandLantern(camera);torchModel.localPosition=new Vector3(-.215f,-.19f,.45f);torchModel.localRotation=Quaternion.Euler(4,0,6);torchModel.localScale=Vector3.one*.95f;}
            else torchModel=FieldKitArt.Create(camera,"Flashlight",.22f,new Vector3(-.20f,-.24f,.44f),Quaternion.Euler(0,90,0));
            // fc-art2: the castle's field radio (R key: hints / static) is a period brass-and-walnut resonance box; Bellwether keeps the FieldRadio mesh
            // (NativeVerification: HeldAssetName "FieldRadio", more than 1000 vertices; HeldAssetName reports the same role key in both maps).
            if(period){radioModel=CastleInstrumentArt.CreateRadio(camera);radioModel.localPosition=new Vector3(.28f,-.16f,.42f);radioModel.localRotation=Quaternion.Euler(8,-8,0);}
            else radioModel=FieldKitArt.Create(camera,"FieldRadio",.20f,new Vector3(.28f,-.16f,.42f),Quaternion.Euler(8,-8,0));
            if(period)
            {
                int vc=CastleInstrumentArt.Vertices(instrumentModels[0]),vl=CastleInstrumentArt.Vertices(instrumentModels[1]),vt=CastleInstrumentArt.Vertices(instrumentModels[2]),vr=CastleInstrumentArt.Vertices(radioModel);
                bool inBudget=vc>0&&vl>0&&vt>0&&vr>0&&vc<=CastleInstrumentArt.MaxVertices&&vl<=CastleInstrumentArt.MaxVertices&&vt<=CastleInstrumentArt.MaxVertices&&vr<=CastleInstrumentArt.MaxVertices;
                string line="HC_INSTRUMENTS map=castle compass="+vc+" trumpet="+vt+" period="+inBudget+" lantern="+vl+" radio="+vr+" (max "+CastleInstrumentArt.MaxVertices+")";
                if(inBudget)Debug.Log(line);else Debug.LogError(line);
            }
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
            bool lanternUp=LanternRaise>0&&Game.Case.Equipped;   // no lantern without the kit
            Torch.enabled=lanternUp||Game.Case.Equipped&&TorchOn&&!(Game.Scares&&Game.Scares.TorchFailed);Torch.intensity=.85f*Brightness*TorchScale;
            if(lanternGlow){lanternGlow.enabled=lanternUp;if(lanternUp){lanternGlow.intensity=LanternGlowIntensity*LanternRaise*Mathf.Max(.5f,Brightness);lanternGlow.range=LanternGlowRange;}}
            Ultraviolet.enabled=Game.Case.Equipped&&Tool==2&&!Hidden;
            hiddenVision.enabled=Hidden&&!Game.Modal;hiddenVision.intensity=.09f*Brightness;
            bool swordOut=SwordEquipped&&Game.Case.Equipped&&!Game.Modal&&!Hidden;
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
            // Owner rule (2026-10-14): scripted runs (playtest, verify, capture) never read the real mouse, keyboard or pad.
            bool live=TestInput==null&&!Game.ScriptedRun;
            if(live)
            {
                liveInput.Move=new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));liveInput.Use=Input.GetKeyDown(KeyCode.E);liveInput.UseHeld=Input.GetKey(KeyCode.E);
                liveInput.Fire=Input.GetMouseButtonDown(0);liveInput.FireHeld=Input.GetMouseButton(0);liveInput.Torch=Input.GetKeyDown(KeyCode.F);liveInput.Radio=Input.GetKeyDown(KeyCode.R);
                liveInput.Sprint=Input.GetKey(KeyCode.LeftShift);liveInput.Crouch=Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.C);liveInput.SelectTool=0;liveInput.Look=null;
                var pad=PadInput.Current;
                if(pad)pad.Apply(liveInput,Tool);
            }
            input=TestInput??(live?liveInput:idleInput);
            workingHand.Operate(input.Fire||input.FireHeld);radioHand.Operate(radioPose>0);
            if(live)
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
                Focus=FocusRay(View.transform.position,View.transform.forward,out _);
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
                if(footstep<=0){footstep=Running?.3f:Crouched?.8f:.51f;Game.Sound.Cue("step",transform.position,Game.Map==MapId.Castle?(Crouched?.06f:Running?.15f:.11f):.22f);Game.Creature.Hear(transform.position,Running?10:Crouched?1.8f:4.5f);}
            }
        }
        // The interaction ray (also used by CastleOnboarding's HC_HIDE_SPOTS audit, so the audit tests exactly what E does).
        public const float FocusRange=2.7f;
        public static Interactable FocusRay(Vector3 origin,Vector3 direction,out RaycastHit first)
        {
            if(!Physics.Raycast(origin,direction,out first,FocusRange))return OpeningRay(origin,direction,FocusRange);
            var focus=first.collider.GetComponent<Interactable>();
            // Deliberate Bellwether bug fix (2026-10-14): a hide's focus trigger sits in the same plane as its solid lower door
            // (89dac7e on main), so the ray could stop on the door and lose the cupboard. Accept an interactable whose collider the
            // same ray meets within 12 cm of the first hit.
            if(!focus)foreach(var h in Physics.RaycastAll(origin,direction,Mathf.Min(FocusRange,first.distance+.12f)))
            {var it=h.collider.GetComponent<Interactable>();if(it&&h.distance<=first.distance+.12f){focus=it;break;}}
            if(!focus)focus=OpeningRay(origin,direction,Mathf.Min(FocusRange,first.distance+.12f));
            return focus;
        }
        // fg-integ (r15 critic 8f: 8/8 doorproof closes needed close=direct): a castle leaf swung 155 deg lies flat on the wall and the ray
        // above no longer meets it. CastleWorld gives every leaf an "opening" door volume, a trigger on layer 2 (Ignore Raycast: sight lines,
        // the Count's queries, the nav bake and the swing probe never see it) filling that leaf's half of the doorway through the wall.
        // Only when nothing else is focused, the nearest such volume the ray meets before the first solid hit is the focus, so E on the
        // opening opens or closes the leaf from either side. Bellwether has no layer-2 interactables: its focus is unchanged.
        static readonly RaycastHit[] openingHits=new RaycastHit[8];
        static Interactable OpeningRay(Vector3 origin,Vector3 direction,float range)
        {
            int n=Physics.RaycastNonAlloc(origin,direction,openingHits,range,1<<2,QueryTriggerInteraction.Collide);
            Interactable best=null;float nearest=float.MaxValue;
            for(int i=0;i<n;i++)
            {
                var it=openingHits[i].collider?openingHits[i].collider.GetComponent<Interactable>():null;
                if(it&&it.Kind==InteractKind.Door&&it.Door&&openingHits[i].distance<nearest){best=it;nearest=openingHits[i].distance;}
            }
            return best;
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
            NeedleSpinning=Game.CastleDread&&Game.CastleDread.FalseCompass;
            float angle;
            if(NeedleSpinning)angle=Time.time*840+Mathf.Sin(Time.time*13)*40;
            else
            {
                var to=Vector3.ProjectOnPlane(Game.World.Induction-transform.position,Vector3.up);
                angle=to.sqrMagnitude<.01f?0:-Vector3.SignedAngle(transform.forward,to,Vector3.up)+90;
            }
            // fc-props: the period needle trembles like a real one near iron, harder as the reading (0..5, the first digit of the display) rises.
            if(compassParts!=null)
            {
                string shown=inductionReadout?inductionReadout.text:"";int level=shown.Length>0&&shown[0]>='0'&&shown[0]<='5'?shown[0]-'0':0;
                compassParts.SetLevel(level);
                if(!NeedleSpinning)angle+=(Mathf.Sin(Time.time*41f)*.6f+Mathf.Sin(Time.time*23.7f+1.3f)*.4f)*(.5f+.9f*level);
            }
            needle.localRotation=Quaternion.Euler(0,0,angle);
        }
        // Capture hook (-hc-capture-art instruments): CaptureMode disables the player's Update, so pose the held instrument by hand.
        public void CaptureHeld(int tool,int level)
        {
            bool radio=tool==4;   // fc-art2: tool 4 raises the field radio (the R-key pose) instead of an instrument
            if(!radio){Tool=tool;priorTool=tool;}
            equipPose=1;radioPose=radio?1:0;
            if(sword)sword.gameObject.SetActive(false);
            heldTool.localPosition=new Vector3(.28f,-.16f,.42f);heldTool.localRotation=Quaternion.Euler(8,-8,0);
            radioModel.localPosition=new Vector3(.28f,-.16f,.42f);
            heldTool.gameObject.SetActive(!radio);radioModel.gameObject.SetActive(radio);torchModel.gameObject.SetActive(true);leftHand.gameObject.SetActive(true);
            for(int i=0;i<instrumentModels.Length;i++)instrumentModels[i].gameObject.SetActive(!radio&&i==tool-1);
            if(compassParts!=null){compassParts.SetLevel(level);if(needle)needle.localRotation=Quaternion.Euler(0,0,100);}
            Debug.Log("HC_CAPTURE_HELD tool="+tool+" asset="+HeldAssetName+" verts="+HeldVertexCount+" level="+level);
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
            if(!Game.Modal&&Game.Case.HuntActive&&MicLevel>Game.MicThreshold&&Time.time-micHeardAt>.5f)
            {micHeardAt=Time.time;MicHeard++;Game.Creature.Hear(transform.position,9f);Debug.Log("HC_MIC heard rms="+MicLevel.ToString("F3"));}
        }
        void StopMicrophone()
        {
            micTried=false;MicLevel=0;if(micClip==null)return;
            Microphone.End(null);micClip=null;Debug.Log("HC_MIC stop");
        }
        void OnDestroy()=>StopMicrophone();
        void OnApplicationQuit()=>StopMicrophone();
        // Bellwether: interference for the whole Warning and Hunt. Castle (critic r10 #9): proximity, not phase: only within about
        // 12 m of the Count (or during a scare's own interference), in any phase.
        bool Interfered()
        {
            if(Game.Map!=MapId.Castle)return Game.Case.Phase==Phase.Warning||Game.Case.Phase==Phase.Hunt||Game.Scares.Interference;
            if(Game.Scares.Interference)return true;
            // Critic r10b N4: only while he hunts (a perched Count between hunts never suspends recording), within 12 m and in
            // line of sight from her eyes to his head (no radar through walls or floors).
            if(!Game.Case.HuntActive)return false;
            var creature=Game.Creature;var actor=creature?creature.Actor:null;
            if(!actor||!actor.gameObject.activeInHierarchy)return false;
            var head=creature.HeadPosition;var eye=View.transform.position;
            if(Vector3.Distance(head,eye)>=12)return false;
            return !Physics.Linecast(eye,head,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore)||Physics.Linecast(eye,head,out var hit,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore)&&hit.collider.transform.IsChildOf(actor.transform);
        }
        void UseEquipment(float dt)
        {
            if(SwordEquipped){Capture=0;evp=0;Game.World.Trace.gameObject.SetActive(false);Instrument="MOONSTONE SWORD";return;}
            UpdateNeedle();
            if(NeedleSpinning&&Tool==1)inductionReadout.text=InductionLevels[Game.Scares.FalseReading];
            bool interfered=Interfered();
            if(interfered)inductionReadout.text=InductionLevels[Game.Scares.FalseReading];
            if(!Game.Case.Equipped||Hidden){Game.World.Trace.gameObject.SetActive(false);return;}
            // Interference is a scare, not a silent prohibition on collecting a real clue.
            if(interfered)Instrument="INTERFERENCE · KEEP THE CLUE IN VIEW";
            scanAngle=Vector3.Angle(lastForward,View.transform.forward);lastForward=View.transform.forward;
            bool holding=input.FireHeld,valid=false;Evidence evidence=Evidence.Induction;
            float dist=Vector3.Distance(View.transform.position,Game.World.Induction);
            if(Tool==1)
            {
                bool near=AimAt(Game.World.Induction,5f,45);
                int reading=near?5:Mathf.Clamp(Mathf.CeilToInt((8-dist)*.55f),0,4);
                string display=InductionLevels[NeedleSpinning?Game.Scares.FalseReading:reading];if(inductionReadout.text!=display)inductionReadout.text=display;
                readerPulse-=dt;
                if(reading>0&&readerPulse<=0){readerPulse=1.35f-reading*.21f;Game.Sound.Cue("confirm",View.transform.position,.055f);}

                Instrument=near?Game.Book.ReaderNear:dist<7?Game.Book.ReaderClose:Game.Book.ReaderIdle;
                valid=near;
            }
            bool trace=Tool==2&&AimAt(Game.World.Print,5.5f,45);
            Game.World.Trace.gameObject.SetActive(trace||Game.Case.Evidence.Contains(Evidence.Handprint)&&Tool==2&&Vector3.Distance(View.transform.position,Game.World.Print)<5);
            if(Tool==2){Instrument=trace?Game.Book.TraceFound:Game.Book.TraceIdle;valid=trace;evidence=Evidence.Handprint;}
            if(Tool==3)
            {
                Instrument=PadInput.Active?Game.Book.AskPad:Game.Book.AskKey;
                if(input.Fire&&evp<=0){evp=1.5f;Game.Say(Game.Book.Question);Game.Sound.Cue("radio",transform.position);Game.Creature.Hear(transform.position,8);}
                if(evp>0)
                {
                    evp-=dt;Instrument=Game.Book.Listening;
                    if(evp<=0)
                    {
                        // Castle (critic r10b N5): the ear-trumpet answers in and out of a hunt alike.
                        if(Vector3.Distance(transform.position,Game.World.Voice)<6)
                        {
                            Game.Case.Record(Evidence.Voice);Game.Say(Game.Book.Reply);
                            // fd-voice: no voice at the evidence spot. The trumpet records a non-verbal texture in her hand; if the Count is in range he may say
                            // cc_bargain from his own head (once per run). Bellwether keeps its ghost-voice cue.
                            if(Game.Map==MapId.Castle)
                            {
                                bool said=Game.Creature&&Game.Creature.CountBargain();
                                var hand=View.transform;Game.Sound.Evp(hand.position+hand.forward*.4f-hand.up*.2f,"trumpet",said?"spoke":"none");
                                if(said)Debug.Log("HC_EVP from=head verbal=True id=cc_bargain");
                            }
                            else Game.Sound.Cue("voice",Game.World.Voice);
                        }
                        else Game.Say(Game.Book.NoReply);
                    }
                }
                Capture=0;return;
            }
            if(Game.Case.Evidence.Contains(evidence)){Capture=0;Instrument="RECORDED · "+Game.Book.EvidenceName(evidence);return;}
            if(valid)Capture+=dt/1.25f;else Capture=Mathf.Max(0,Capture-dt*.5f);
            if(Capture>=1){if(Game.Case.Record(evidence)){Game.Sound.Cue("evidence_filed",transform.position,.45f);Game.Notice("Recorded: "+Game.Book.EvidenceName(evidence)+" · "+Game.Case.Evidence.Count+"/3",4);}Capture=0;}
        }
        public void Hide(Interactable spot)
        {
            // fb-onboard (owner feedback items 6, 7: "everything you can hide in doesn't work"): E on a hide always hides or says why.
            // The castle never sits in Briefing any more (Game equips at spawn); this is the safety net, never a silent return.
            // Bellwether keeps its silent Briefing refusal unchanged (its van rack still starts the case).
            if(Game.Case.Phase==Phase.Briefing){if(Game.Map==MapId.Castle){Game.Notice("Not yet: the case has not started.");Debug.Log("HC_PLAYER_HIDE_REFUSED hide="+spot.name+" reason=briefing");}return;}
            if(Game.Map==MapId.Castle)
            {
                // Wave K: from Dread 60, one hide attempt in six is held from inside (FEAR_PLAN item 5; injected dice).
                int attempts=Game.Case.DoorAttempts;
                bool held=Game.Case.TryDoorHeld();
                // Wave L proof line: every eligible attempt (Dread >= 60) is counted, so "5 hides at >= 60 and no HC_DOOR_HELD" is checkable.
                if(Game.Case.DoorAttempts>attempts)Debug.Log("HC_DOOR_TRY hide="+spot.name+" held="+held+" attempt="+Game.Case.DoorAttempts+" dread="+Game.Case.Dread.ToString("F1"));
                if(held){Game.Notice("The door is held.");Game.Sound.Cue("door_rattle",spot.transform.position,.8f);Debug.Log("HC_DOOR_HELD hide="+spot.name+" dread="+Game.Case.Dread.ToString("F1")+" held="+Game.Case.DoorsHeld);return;}
            }
            Game.Creature.WitnessHide(spot.HidePoint);
            hiding=spot;Hidden=true;Teleport(spot.HidePoint);
            if(Game.Map==MapId.Castle)Debug.Log("HC_PLAYER_HID hide="+spot.name+" phase="+Game.Case.Phase+" t="+Game.Case.Elapsed.ToString("F0"));
            var outward=Vector3.ProjectOnPlane(spot.ExitPoint-spot.HidePoint,Vector3.up).normalized;
            hideYaw=Quaternion.LookRotation(outward).eulerAngles.y;transform.rotation=Quaternion.Euler(0,hideYaw,0);
            pitch=0;View.transform.localRotation=Quaternion.identity;View.transform.localPosition=new Vector3(0,1.65f,.18f);
            Crouched=false;Moving=false;Running=false;
        }
        void ExitHide(){Game.Creature.LeaveHide();Hidden=false;Teleport(hiding.ExitPoint);View.transform.localPosition=Vector3.up*1.64f;hiddenVision.enabled=false;hiding=null;}
        public void Teleport(Vector3 point){Controller.enabled=false;transform.position=point;Controller.enabled=true;vertical=0;}
    }
}
