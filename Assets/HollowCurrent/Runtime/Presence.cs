using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace HollowCurrent
{
    public sealed class Presence : MonoBehaviour
    {
        public Game Game;
        public readonly ObservationMemory Memory=new ObservationMemory();
        NavMeshAgent agent;
        Transform silhouette;
        float think,breath;
        int search;
        bool witnessedHide;
        Vector3 knownHide;
        HearthboundActor actor;
        float presentationRemaining;
        bool capturePresentation,fakeRush;
        Vector3? huntOrigin;
        Vector3 rushTarget;
        float contact;
        public HearthboundActor Actor=>actor;
        // Tuning for this map's hunter (Split Grin in Bellwether, Count Claude in the castle).
        public HunterProfile Profile {get;private set;}
        public float PresentationRemaining=>presentationRemaining;
        public string DiagnosticState=>"position="+transform.position+" player="+Game.Player.transform.position+" enabled="+agent.enabled+" nav="+agent.isOnNavMesh+" path="+(agent.enabled&&agent.isOnNavMesh?agent.pathStatus.ToString():"none")+" destination="+(agent.enabled&&agent.isOnNavMesh?agent.destination.ToString():"none")+" seen="+CanSeePlayer+" memory="+Memory.Knows+(count!=null?" flight="+flight+" perch="+perchIndex+" mode="+(actor?actor.Mode:-1):"");

        public int Appearances {get;private set;}
        public bool CanSeePlayer {get;private set;}
        public void Initialize(Game game)
        {
            Game=game;Profile=HunterProfile.For(game.Map);count=Profile as CountClaudeProfile;count?.ApplyReceipt(); // castle: grab reaches from CountClaudeModes.json (hc/grabs2)
            var prefab=Resources.Load<GameObject>(Profile.ModelResource);
            if(!prefab&&Profile.AllowPlaceholderModel){silhouette=PlaceholderModel();actor=silhouette.GetComponent<HearthboundActor>();Debug.LogWarning("HC_HUNTER_PLACEHOLDER missing "+Profile.ModelResource);}
            else
            {
                if(!prefab)throw new System.InvalidOperationException("Required original animated apparition is missing");
                silhouette=Object.Instantiate(prefab,transform,false).transform;actor=silhouette.GetComponent<HearthboundActor>();
                if(!actor||actor.BoneCount<Profile.MinimumBones)throw new System.InvalidOperationException("Apparition is not a verified skinned rig");
            }
            agent=gameObject.AddComponent<NavMeshAgent>();agent.radius=Profile.AgentRadius;agent.height=Profile.AgentHeight;agent.speed=Profile.Speed;agent.angularSpeed=Profile.AngularSpeed;agent.acceleration=Profile.Acceleration;agent.stoppingDistance=Profile.StoppingDistance;
            if(count!=null){CountRimLight();CountGoreMaterials();CacheRig();KeepSkinBoundsLive();}
            silhouette.gameObject.SetActive(false);agent.enabled=false;
        }
        // Magenta stand-in (no rig, no animator controller) so a map runs before its hunter art lands.
        Transform PlaceholderModel()
        {
            var root=new GameObject(Profile.Name+" / placeholder").transform;root.SetParent(transform,false);
            var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.name="Placeholder body";body.transform.SetParent(root,false);
            body.transform.localPosition=Vector3.up*1;body.transform.localScale=new Vector3(.6f,1,.6f);Object.Destroy(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial=MissingArt.Magenta;
            var head=new GameObject("Head").transform;head.SetParent(root,false);head.localPosition=Vector3.up*1.7f;
            var stand=root.gameObject.AddComponent<HearthboundActor>();stand.Animator=root.gameObject.AddComponent<Animator>();stand.Head=head;
            return root;
        }
        // Fight hooks: Animator modes 4 Stagger / 5 Death. They do nothing until a
        // profile overrides OnStagger / OnDeath (piece 4, CountClaudeProfile).
        public void Stagger()=>Profile.OnStagger(this);
        public void Defeat()=>Profile.OnDeath(this);
        public bool ManifestAt(Vector3 point,float yaw,float seconds=3.5f,bool supersede=false)
        {
            if(Game.Modal||Game.Case.Phase==Phase.Hunt||Game.Case.Phase==Phase.Warning||Game.Case.Terminal||presentationRemaining>0&&!supersede||count!=null&&huntOn)return false;
            fakeRush=false;staged=false;agent.enabled=false;transform.position=point;transform.rotation=Quaternion.Euler(0,yaw,0);
            silhouette.gameObject.SetActive(true);actor.Pose(Profile.ModeSearch);presentationRemaining=seconds;Appearances++;Game.Sound.Cue("apparition",point+Vector3.up,.85f);return true;
        }
        // End a staged appearance immediately (capture tooling, scripted beats).
        public void Dismiss(){presentationRemaining=0;fakeRush=false;staged=false;choreo=false;RestoreHiddenFloaters();if(silhouette){silhouette.localRotation=Quaternion.identity;if(count!=null)silhouette.localPosition=Vector3.zero;silhouette.gameObject.SetActive(false);}}
        public void FacePlayer()
        {
            var delta=Game.Player.transform.position-transform.position;delta.y=0;
            if(delta.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(delta);
        }
        public void BeginFakeRush(){fakeRush=true;rushTarget=Game.Player.transform.position;actor.Pose(Profile.ModeMove,Profile.FakeRushSpeed);}
        public void SetHuntOrigin(Vector3 point){huntOrigin=point;}
        public void PhaseChanged(Phase phase)
        {
            presentationRemaining=0;capturePresentation=false;fakeRush=false;contact=0;
            if(count!=null){CountPhaseChanged(phase);return;}
            if(phase==Phase.Dead)
            {
                agent.enabled=false;capturePresentation=true;presentationRemaining=Profile.CapturePresentationSeconds;
                var towards=Game.Player.transform.position-transform.position;towards.y=0;
                if(towards.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(towards);
                silhouette.gameObject.SetActive(true);actor.Pose(Profile.ModeReach,0,true);CanSeePlayer=false;Memory.Clear();return;
            }
            if(phase==Phase.Hunt)
            {
                var spawn=Profile.HuntSpawn??Game.World.HunterSpawn;
                var desired=huntOrigin??spawn;huntOrigin=null;
                if(Vector3.Distance(desired,Game.Player.transform.position)<Profile.HuntSpawnMinPlayerDistance)desired=spawn;
                // Spawn is fixed in the source room, never selected from hidden player state.
                if(NavMesh.SamplePosition(desired,out var hit,Profile.HuntSpawnSampleRadius,NavMesh.AllAreas))transform.position=hit.position;
                else {Game.Say("The investigation could not continue. Restart the case from the pause menu.");Debug.LogError("HC_NAV_UNAVAILABLE");return;}
                agent.enabled=true;agent.isStopped=false;silhouette.gameObject.SetActive(true);Debug.Log("HC_HUNT_SPAWN "+DiagnosticState);actor.Pose(Profile.ModeMove,Profile.Speed);Memory.Clear();witnessedHide=false;think=0;search=0;
            }
            else{agent.enabled=false;silhouette.gameObject.SetActive(false);CanSeePlayer=false;Memory.Clear();witnessedHide=false;}
        }
        public void Hear(Vector3 source,float radius)
        {
            if(Game==null||!(count!=null?huntOn:Game.Case.Phase==Phase.Hunt))return;
            float vertical=Mathf.Abs(source.y-transform.position.y);
            if(vertical>Profile.VerticalGap)radius*=Profile.VerticalHearingScale;
            if(Vector3.Distance(source,transform.position)<=radius)
            {
                Memory.Observe(source.x,source.y,source.z);if(NativeVerification.Requested&&!NativeVerification.ProfileOnly)Debug.Log("HC_HEARD "+source+" "+DiagnosticState);
                if(count!=null)Remember(source,false);   // r10 #29: a heard position is a last known place too
            }
        }
        public void WitnessHide(Vector3 spot)
        {
            if((count!=null?huntOn&&CountSees():Game.Case.Phase==Phase.Hunt&&Sees())) {witnessedHide=true;knownHide=spot;Memory.Observe(spot.x,spot.y,spot.z);}
            else return;
            // Castle (r10 #16/#28): he saw her go in. Whatever he is doing, he will come and open it (CountHunt starts it).
            if(count!=null){Remember(spot,true);pendingWitnessed=true;Debug.Log("HC_COUNT_WITNESSED hide at "+spot+" "+DiagnosticState);}
        }
        public void LeaveHide()
        {
            // Keep the observed location in short-term memory, but knowledge of
            // one cupboard must not reveal the player's next hiding place.
            witnessedHide=false;
        }
        bool Sees()
        {
            var p=Game.Player;
            if(p.Hidden&&!p.TorchOn)return false;
            Vector3 eye=transform.position+Vector3.up*Profile.EyeHeight,delta=p.View.transform.position-eye;
            if(delta.magnitude>Profile.SightRange||Vector3.Angle(transform.forward,delta)>Profile.SightAngle)return false;
            if(Physics.Raycast(eye,delta.normalized,out var hit,delta.magnitude))return hit.collider.GetComponent<Investigator>()!=null;
            return true;
        }
        // fd-grab: the castle's grab presentation clock runs after the frame's logic (a commit made this frame is already seen).
        void Update(){UpdateCore();if(count!=null)PresentationClock();}
        void UpdateCore()
        {
            if(Game==null)return;
            if(capturePresentation){presentationRemaining=Mathf.Max(0,presentationRemaining-Time.unscaledDeltaTime);return;}
            if(count!=null&&CountFrame())return;
            if(Game.Modal)return;
            if(Game.Case.Phase!=Phase.Hunt)
            {
                if(Game.Case.Phase==Phase.Containment&&Game.Case.SealProgress>.02f)
                {
                    float progress=Game.Case.SealProgress;agent.enabled=false;
                    transform.position=new Vector3(Game.World.Anchor.x,Game.World.AnchorFloor,Game.World.Anchor.z+Mathf.Lerp(2,1.05f,progress));
                    transform.rotation=Quaternion.Euler(0,180,0);silhouette.gameObject.SetActive(true);actor.Pose(progress>Profile.ContainmentReachProgress?Profile.ModeReach:Profile.ModeSearch);return;
                }
                if(presentationRemaining>0)
                {
                    presentationRemaining-=Time.deltaTime;
                    if(fakeRush)
                    {
                        var delta=rushTarget-transform.position;delta.y=0;float step=Mathf.Min(Time.deltaTime*Profile.FakeRushSpeed,Mathf.Max(0,delta.magnitude-Profile.FakeRushStop));
                        if(delta.magnitude<Profile.FakeRushArrive||Physics.CapsuleCast(transform.position+Vector3.up*.4f,transform.position+Vector3.up*1.65f,.25f,delta.normalized,out _,step,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore))presentationRemaining=0;
                        else transform.position+=delta.normalized*step;
                    }
                    if(presentationRemaining<=0){silhouette.gameObject.SetActive(false);fakeRush=false;}
                }
                else silhouette.gameObject.SetActive(false);
                return;
            }
            if(!agent.enabled||!agent.isOnNavMesh)return;
            float dt=Time.deltaTime;Memory.Tick(dt);CanSeePlayer=Sees();
            if(CanSeePlayer){var p=Game.Player.transform.position;Memory.Observe(p.x,p.y,p.z);}
            if(Game.Player.Hidden&&Game.Player.TorchOn)Hear(Game.Player.transform.position,Profile.HiddenTorchHearRadius);
            think-=dt;
            if(think<=0)
            {
                think=Profile.ThinkInterval;
                Vector3 target;
                if(witnessedHide)target=knownHide;
                else if(Memory.Knows)target=new Vector3(Memory.X,Memory.Y,Memory.Z);
                else
                {
                    var route=Profile.Search==SearchRoute.PerchPoints&&Game.World.PerchPoints.Count>0?Game.World.PerchPoints:Game.World.SearchPoints;
                    if(!agent.hasPath||agent.remainingDistance<Profile.SearchArriveDistance)search=(search+1)%route.Count;
                    target=route[search%route.Count];
                }
                // A remembered sound inside a cupboard leads to that cupboard's
                // entrance. Its solid collision shell is not a walkable target.
                foreach(var spot in Game.World.HidingPlaces)
                    if(spot.Kind==InteractKind.Hide&&Vector3.Distance(target,spot.HidePoint)<.35f){target=spot.ExitPoint;break;}
                if(NavMesh.SamplePosition(target,out var hit,Profile.TargetSampleRadius,NavMesh.AllAreas))agent.SetDestination(hit.position);
                foreach(var door in Game.World.Doors)if(Vector3.Distance(door.transform.position,transform.position)<Profile.DoorOpenDistance)door.Open=true;
            }
            breath-=dt;
            if(breath<=0){breath=Profile.BreathInterval;Game.Sound.Cue("breath",transform.position,Profile.BreathVolume);}
            float distance=Vector3.Distance(transform.position,Game.Player.transform.position);
            // A cupboard can be searched from its clear entrance, never through
            // the adjacent room wall merely because an old sound is remembered.
            bool knowsThisHide=Game.Player.Hidden&&((witnessedHide&&Vector3.Distance(knownHide,Game.Player.transform.position)<.35f)||
                (Memory.Knows&&Vector3.Distance(new Vector3(Memory.X,Memory.Y,Memory.Z),Game.Player.transform.position)<.35f));
            bool atHideEntrance=knowsThisHide&&Vector3.Distance(transform.position,Game.Player.HideExit)<Profile.HideEntranceDistance&&
                !Physics.Linecast(transform.position+Vector3.up,Game.Player.HideExit+Vector3.up);
            // Once a visible grab begins, overlapping eye positions must not break
            // the FOV test and reset it. Distance/occlusion still allow escape.
            bool clearReach=distance<Profile.GrabDistance&&!Game.Player.Hidden&&!Physics.Linecast(transform.position+Vector3.up,Game.Player.transform.position+Vector3.up,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore);
            bool touching=(clearReach&&(CanSeePlayer||contact>0))||atHideEntrance;
            agent.isStopped=touching;
            contact=touching?contact+dt:0;
            if(touching)actor.Pose(Profile.ModeReach);
            if(contact>=Profile.GrabSeconds&&Game.Case.PhaseTime>Profile.GrabGraceSeconds)Game.Case.Die();
            if(Game.Case.Terminal)return;
            if(!touching)actor.Pose(agent.velocity.sqrMagnitude>Profile.MovingVelocitySqr?Profile.ModeMove:Profile.ModeSearch,agent.velocity.magnitude);
            if(Physics.Raycast(transform.position+Vector3.up*.3f,Vector3.down,out var floor,1,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore))
                silhouette.localPosition=Vector3.up*Mathf.Clamp(floor.point.y-transform.position.y,-.3f,.1f);
        }
        // ---- Count Claude (Haunted Castle) ---------------------------------------------------------------------
        // Everything below runs only when Profile is CountClaudeProfile (count != null); Split Grin's NavMeshAgent
        // path above is untouched. The Count flies kinematically: straight swoops where a sphere cast is clear,
        // otherwise NavMesh corners lifted to a hover height under the ceiling. docs/haunted-castle/DESIGN.md.
        // Away: between hunts, not placed (hidden until a perch is free and he is unobserved). NearMiss: item 10.
        enum Flight { Perched, Travel, Chase, Land, Stagger, Retreat, Dead, Away, NearMiss }
        const int FlightMask=~((1<<2)|(1<<30));
        CountClaudeProfile count;
        Flight flight=Flight.Perched;
        bool staged,travelToPerch;
        int perchIndex=-1,pendingPerch=-1,searchSerial;
        float perchTimer,tell,landTimer,holdTimer,staggerTimer,retreatTimer,speedNow,perchYaw,replan,scanClock;
        Vector3 routeGoal,searchedAt=new Vector3(9999,9999,9999),lastChaseGoal;
        readonly List<Vector3> route=new List<Vector3>();
        int routeAt;
        NavMeshPath path;
        Fight CountFight=>Game.Castle?Game.Castle.Fight:null;
        float Tier=>CountFight!=null&&CountFight.Active?CountFight.Tier:0;
        float SpeedScale=>CountFight!=null&&CountFight.Active?CountFight.SpeedMultiplier:1;

        /// <summary>Capture hook (S5): crouch the Count on perch <paramref name="perchIndex"/> (its balustrade edge when
        /// one is in front of it), facing the open room, in Mode 0 Perch. Holds until Dismiss, ManifestAt or a phase change.</summary>
        public bool StageForCapture(int perchIndex)=>StageForCapture(perchIndex,false);
        /// <param name="clearViewLine">CaptureFraming then hides Floater props within 2 m of its eye-to-Count line
        /// (r3 S5: a floating book covered his face); Dismiss restores them.</param>
        public bool StageForCapture(int perchIndex,bool clearViewLine)
        {
            clearCaptureView=clearViewLine;
            if(count==null||Game.World.PerchPoints.Count==0)return false;
            int i=Mathf.Clamp(perchIndex,0,Game.World.PerchPoints.Count-1);
            presentationRemaining=0;capturePresentation=false;fakeRush=false;agent.enabled=false;route.Clear();
            Ledge(i,out var feet,out var yaw);transform.SetPositionAndRotation(feet,Quaternion.Euler(0,yaw,0));
            staged=true;flight=Flight.Perched;this.perchIndex=i;perchYaw=yaw;silhouette.gameObject.SetActive(true);actor.Pose(Profile.ModeIdle);
            Debug.Log("HC_COUNT_STAGED perch="+i+" point="+Game.World.PerchPoints[i]+" feet="+feet+" yaw="+yaw.ToString("F0"));return true;
        }
        /// <summary>Capture helper: a camera eye/target that frames the posed Count so his skeleton bounds fill
        /// <paramref name="fill"/> of the vertical field of view, from <paramref name="below"/> degrees under his facing.</summary>
        public bool CaptureFraming(float fill,float below,out Vector3 eye,out Vector3 target)
        {
            eye=target=transform.position;if(!actor||!silhouette.gameObject.activeInHierarchy)return false;
            var b=actor.PoseBounds(0);float half=Game.Player.View.fieldOfView*.5f*Mathf.Deg2Rad;
            float d=Mathf.Max(b.size.y,b.size.x*.5625f)/(Mathf.Clamp(fill,.1f,.95f)*2*Mathf.Tan(half));
            var dir=(transform.forward*Mathf.Cos(below*Mathf.Deg2Rad)-Vector3.up*Mathf.Sin(below*Mathf.Deg2Rad)).normalized;
            if(Physics.SphereCast(b.center,.25f,dir,out var hit,d,FlightMask,QueryTriggerInteraction.Ignore))d=Mathf.Max(1.2f,hit.distance-.3f);
            eye=b.center+dir*d;target=b.center;
            if(clearCaptureView)HideFloatersOnViewLine(eye,target);
            return true;
        }
        bool clearCaptureView;
        /// <summary>S5 (round 5): crouch him on the rose-window ledge perch (PerchPoints[2], CastleWorld order) with his claws
        /// on the ledge's front edge, facing down the hall. Then call RoseLedgeFraming after the pose settles (~1.5 s).</summary>
        public bool StageOnRoseLedge(bool clearViewLine=true)
        {
            if(count==null||Game.World.PerchPoints.Count<3)return false;
            if(!StageForCapture(2,clearViewLine))return false;
            // The ledge has no balustrade: find its front edge by stepping forward until the floor under him drops away.
            var feet=transform.position;var dir=transform.forward;dir.y=0;dir.Normalize();var edge=feet;
            for(int k=1;k<=24;k++)
            {
                var probe=feet+dir*(k*.05f)+Vector3.up*.3f;
                if(!Physics.Raycast(probe,Vector3.down,.45f,FlightMask,QueryTriggerInteraction.Ignore))break;
                edge=feet+dir*(k*.05f);
            }
            transform.position=edge-dir*.30f;   // Perch clip: wrists 0.32 m ahead of the ankles, over the edge
            Debug.Log("HC_COUNT_STAGED rose ledge feet="+transform.position+" edge="+edge);
            return true;
        }
        /// <summary>Camera for the rose-ledge S5: on the line from CastleLayout.RoseWindowEye through his posed bounds (the
        /// rose-window moon sprites are placed on rays from that eye), at the distance that makes his bounds
        /// <paramref name="fill"/> of the frame height, raised until both moons sit about <paramref name="moonsAboveDeg"/>
        /// degrees above his head. Logs HC_S5_FRAMING with eye, pitch and the moon angles.</summary>
        public bool RoseLedgeFraming(float fill,out Vector3 eye,out Vector3 target,float moonsAboveDeg=9f)
        {
            eye=target=transform.position;if(!actor||!silhouette.gameObject.activeInHierarchy)return false;
            var b=actor.PoseBounds(0);var head=actor.Head?actor.Head.position:b.center+Vector3.up*b.extents.y*.5f;
            float half=Game.Player.View.fieldOfView*.5f*Mathf.Deg2Rad;
            float d=b.size.y/(Mathf.Clamp(fill,.1f,.95f)*2*Mathf.Tan(half));
            var line=(b.center-CastleLayout.RoseWindowEye).normalized;
            var moons=new List<Vector3>();
            foreach(var t in Game.World.Root.GetComponentsInChildren<Transform>(true))if(t.name=="Pale moon"||t.name=="Red moon")moons.Add(t.position);
            float rise=.35f*d;
            if(moons.Count>0)
            {
                var m=Vector3.zero;foreach(var p in moons)m+=p;m/=moons.Count;
                float best=float.MaxValue;
                for(float r=-.2f*d;r<=.9f*d;r+=.01f*d)
                {
                    var c=b.center-line*d+Vector3.up*r;
                    float above=Elevation(m-c)-Elevation(head-c);
                    float err=Mathf.Abs(above-moonsAboveDeg);if(err<best){best=err;rise=r;}
                }
            }
            eye=b.center-line*d+Vector3.up*rise;target=b.center;
            if(clearCaptureView){HideFloatersOnViewLine(eye,target);foreach(var p in moons)HideFloatersOnViewLine(eye,p,1.2f);}
            var v=target-eye;
            Debug.Log("HC_S5_FRAMING eye="+eye+" target="+target+" pitch="+Elevation(v).ToString("F1")+" distance="+v.magnitude.ToString("F2")+" moons="+moons.Count+" rise="+rise.ToString("F2"));
            return true;
        }
        static float Elevation(Vector3 v)=>Mathf.Atan2(v.y,new Vector2(v.x,v.z).magnitude)*Mathf.Rad2Deg;
        readonly List<GameObject> hiddenFloaters=new List<GameObject>();
        /// <summary>Capture only: deactivate every Floater whose renderers come within <paramref name="radius"/> of the
        /// segment eye -> target (plus 1 m past the target). Returns how many were hidden; RestoreHiddenFloaters undoes it.</summary>
        public int HideFloatersOnViewLine(Vector3 eye,Vector3 target,float radius=2f)
        {
            int hidden=0;var end=target+(target-eye).normalized;
            foreach(var floater in Floater.All.ToArray())
            {
                if(!floater||!floater.gameObject.activeInHierarchy)continue;
                var renderers=floater.GetComponentsInChildren<Renderer>();if(renderers.Length==0)continue;
                var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
                var closest=ClosestOnSegment(eye,end,b.center);
                if(Vector3.Distance(b.ClosestPoint(closest),closest)<radius){floater.gameObject.SetActive(false);hiddenFloaters.Add(floater.gameObject);hidden++;}
            }
            Debug.Log("HC_CAPTURE_FLOATERS_HIDDEN "+hidden+" eye="+eye+" target="+target);
            return hidden;
        }
        public void RestoreHiddenFloaters(){foreach(var g in hiddenFloaters)if(g)g.SetActive(true);hiddenFloaters.Clear();clearCaptureView=false;}
        static Vector3 ClosestOnSegment(Vector3 a,Vector3 b,Vector3 p){var ab=b-a;float t=Mathf.Clamp01(Vector3.Dot(p-a,ab)/Mathf.Max(ab.sqrMagnitude,1e-6f));return a+ab*t;}
        /// <summary>Capture helper: a neutral 1.8 m mannequin (the investigator's standing height) for scale shots.</summary>
        public static GameObject ScaleReference(Vector3 feet,float yaw,float height=1.8f)
        {
            var root=new GameObject("Scale reference "+height.ToString("F2")+" m");root.transform.SetPositionAndRotation(feet,Quaternion.Euler(0,yaw,0));
            var shader=Shader.Find("Standard");var material=shader?new Material(shader){color=new Color(.62f,.62f,.66f)}:MissingArt.Magenta;
            float head=.23f*height/1.8f,torso=height-head;
            void Part(PrimitiveType type,Vector3 at,Vector3 size)
            {
                var part=GameObject.CreatePrimitive(type);part.transform.SetParent(root.transform,false);part.transform.localPosition=at;part.transform.localScale=size;
                Object.Destroy(part.GetComponent<Collider>());part.GetComponent<Renderer>().sharedMaterial=material;
            }
            Part(PrimitiveType.Capsule,Vector3.up*torso*.5f,new Vector3(.44f,torso*.5f,.3f));
            Part(PrimitiveType.Sphere,Vector3.up*(height-head*.5f),Vector3.one*head);
            // Blind critic r10 V6: the debug figure never appears in captures or playtests (it stays for editor checks).
            if(Game.ScriptedRun){root.SetActive(false);Debug.Log("HC_SCALE_REFERENCE hidden (scripted run)");}
            return root;
        }

        internal void CountStagger()
        {
            if(flight==Flight.Dead)return;
            CloseDive("sword stagger");staggerTimer=Fight.StaggerSeconds;retreatTimer=0;flight=Flight.Stagger;contact=0;chaseClosest=99;route.Clear();actor.Pose(Profile.ModeStagger);
            // r11 (camera inside his wing on sword hits): recoil away from her, and keep his joints out of the lens for a while.
            committedUntil=-1;grabMode=-1;
            if(grabCam)EndGrabCam("stagger");
            var away=transform.position-Game.Player.View.transform.position;away.y=0;if(away.sqrMagnitude<.01f)away=-Game.Player.transform.forward;
            recoilDir=away.normalized;recoilLeft=count.SwordRecoilDistance;clearUntil=Time.time+Fight.StaggerSeconds+count.SwordClearSeconds;
            Debug.Log("HC_COUNT_STAGGER "+DiagnosticState);
        }
        internal void CountDefeat()
        {
            flight=Flight.Dead;staged=false;contact=0;route.Clear();CanSeePlayer=false;Memory.Clear();
            silhouette.gameObject.SetActive(true);actor.Pose(Profile.ModeDeath);Debug.Log("HC_COUNT_DEATH "+DiagnosticState);
        }
        void CountPhaseChanged(Phase phase)
        {
            if(choreo)return;   // the scripted capture choreography ignores case phases until it is ended
            staged=false;agent.enabled=false;holdTimer=0;
            if(flight==Flight.Dead){silhouette.gameObject.SetActive(true);actor.Pose(Profile.ModeDeath);return;}
            if(phase==Phase.Dead)
            {
                // Grab presentation at a fixed stand-off: the clip reaches StandOffFor(mode) - CameraClearance ahead of him, so the
                // camera stays CameraClearance outside the mesh while it keeps animating (unscaled time). Mode 6 wing-wrap unless
                // the hunt chose GrabDrop (8) or GrabCrawl (9) and the controller has them (item 17).
                huntOn=false;route.Clear();SetRim(count.RimLightScale);
                int mode=grabMode>=0?grabMode:ChooseGrab(false,out _,out _);
                capturePresentation=true;presentationRemaining=Profile.CapturePresentationSeconds;
                var player=Game.Player.transform.position;var towards=player-transform.position;towards.y=0;
                if(towards.sqrMagnitude<.01f)towards=transform.forward;towards.Normalize();
                if(grabMode<0)mode=CommitGrab(false,mode);   // no grab under way (e.g. a forced death): choose with the headroom rule
                // A hide grab is already standing in the torn door opening at its stand-off; anything else is placed now.
                // r12: a committed grab is already at its stand-off; only a death with no grab under way is placed (no pop).
                // fd-grab: a committed grab keeps the facing it was committed with (re-aiming swung his skull at the kill).
                if(!grabHide&&grabMode<0)transform.SetPositionAndRotation(player-towards*count.StandOffFor(mode),Quaternion.LookRotation(towards));
                silhouette.gameObject.SetActive(true);actor.Pose(mode,0,true);CanSeePlayer=false;Memory.Clear();
                // count17: the GrabDrop clip carries its own fall (receipt top 4.72 m), so no extra lift here; CommitGrab already
                // chose GrabCrawl where the ceiling is too low for it.
                if(grabMode<0&&!grabCam)grabMode=mode;   // fd-grab: a forced death (no grab under way) records the mode it plays
                BeginGrabCam("dive",grabHide);grabCamT=Mathf.Min(grabCamT,count.GrabCamTurnSeconds);grabCamLog=Mathf.Min(grabCamLog,1);grabKill=true;
                Debug.Log("HC_COUNT_GRAB_PRESENTATION mode="+mode+" standOff="+count.StandOffFor(mode).ToString("F2")+" "+DiagnosticState);return;
            }
            if(grabCam)EndGrabCam("phase "+phase);
            RestoreHideDoor();
            SyncHunt();
        }
        // The castle hunts while F1's Investigation.HuntActive holds (Phase Hunt, or a hostile Return), not on Phase alone:
        // called on every phase change and every frame, so a hunt that starts or ends without a phase change is seen too.
        void SyncHunt()
        {
            bool active=Game.Case.HuntActive&&!Game.Case.Terminal;
            if(active==huntOn)return;
            huntOn=active;
            if(active)CountBeginHunt();else CountEndHunt();
        }
        bool HostileReturn=>huntOn&&Game.Case.Phase==Phase.Return;
        void CountBeginHunt()
        {
            var perches=Game.World.PerchPoints;var player=Game.Player.transform.position;bool hostile=HostileReturn;
            Vector3 desired=hostile?DrawbridgeSide():huntOrigin??(Profile.HuntSpawn??Game.World.HunterSpawn);huntOrigin=null;
            // Item 9: a hunt that starts while he is perched between hunts starts on that perch (no visible jump).
            bool keepPerch=!hostile&&flight==Flight.Perched&&perchIndex>=0&&silhouette.gameObject.activeSelf;
            Memory.Clear();witnessedHide=false;think=0;contact=0;tell=0;staggerTimer=retreatTimer=0;speedNow=0;searchedAt=new Vector3(9999,9999,9999);
            // Quiet start (r10 rerun: the wing loop jumped +0.6 at take-off in both sampled no-tell hunts): a no-tell or hostile-return
            // hunt flies its first leg silent and its second at most soft, and plays no one-shot for 8 s unless he has seen her.
            quietHunt=Game.Case.NoTellHunt||HostileReturn;quietReason=HostileReturn?"return":"notell";quietLeg=0;quietSeen=false;quietHeldLogged=false;
            gateStage=0;huntClock=0;missCooldownUntil=-1;plannedMode=-1;huntVariantAbandons=0;huntSideOwed=false;seenThisHunt=false;grabSurvivedThisHunt=false;huntLines=0;guaranteeNext=Random.Range(count.AttackGuaranteeMin,count.AttackGuaranteeMax);chaseClosest=99;nearMissRolled=false;nearMissStage=0;pendingWitnessed=false;swooping=false;diveLastFired=false;grabMode=-1;dropHeight=0;sawPlayer=false;route.Clear();
            silhouette.gameObject.SetActive(true);SetRim(count.RimLightScale);
            PrimeClipCurves();
            int best=keepPerch?perchIndex:-1;
            if(best<0)
            {
                // Hunt origin (the bell) or, in a hostile return, the drawbridge side: the nearest perch that is not on top of you.
                float bestScore=float.MaxValue;
                for(int i=0;i<perches.Count;i++)
                {
                    float score=Vector3.Distance(perches[i],desired)+(Vector3.Distance(perches[i],player)<Profile.HuntSpawnMinPlayerDistance+2?1000:0);
                    if(score<bestScore){bestScore=score;best=i;}
                }
            }
            if(hostile&&BeginReturnSwoop()){}
            else if(best>=0)SetPerch(best,hostile?count.ReturnPerchSeconds:Random.Range(count.HuntStartPerchMinSeconds,count.HuntStartPerchMaxSeconds));
            else
            {
                if(GroundUnder(desired,out var ground))transform.position=ground;else transform.position=desired;
                BeginLand();
            }
            // Round 10 (playtest: he never left the perch): the bell / the bargain tells him roughly where you are. He leaves the
            // perch for that spot within 2 s; it is never the exact cupboard, so hiding still works unless he saw you hide.
            attackAttempted=false;guaranteeClock=0;aiLogClock=0;returnHearClock=count.ReturnHearSeconds;
            // Item 14: after a survived hunt his first glide goes to the last place he saw, heard or watched you hide (r10 #29:
            // also in a hostile return, after the courtyard swoop).
            if(swooping){}
            else if(MemoryFresh("hunt start"))GoLearned(player);
            else HearPlayerRoughly();
            Debug.Log("HC_HUNT_SPAWN hostileReturn="+hostile+" noTell="+Game.Case.NoTellHunt+" keptPerch="+keepPerch+" "+DiagnosticState);
        }
        void GoLearned(Vector3 player)
        {
            var at=lastSeen.Value;Memory.Observe(at.x,at.y,at.z);searchedAt=new Vector3(9999,9999,9999);
            Debug.Log("HC_COUNT_LEARNED first glide to last known "+at+" (player now "+player+", "+Vector3.Distance(at,player).ToString("F1")+" m away) hostileReturn="+HostileReturn+" "+DiagnosticState);
            // r10 blind P1: no beat (and no audio) at hunt start; the learned glide is log-only.
        }
        // r10 #34: the hostile return is announced. Dispatch speaks (integrator: the CaseBook card on Beat "return-swoop") and he
        // swoops visibly down across the courtyard and out over the gate wall, harmless, before he hunts the bridge.
        bool swooping;
        bool BeginReturnSwoop()
        {
            // The announcement does not depend on the flight: the card beat fires even if the swoop is skipped.
            RaiseBeat("return-swoop"); // Game shows the card on the beat; no voice (owner item 13: a speaker over the castle ruins the scare)
            var root=Game.World.Root;var pts=count.ReturnSwoopLocal;
            if(!root||pts==null||pts.Length<2){Debug.Log("HC_COUNT_RETURN_SWOOP skipped reason=no root or swoop points");return false;}
            var start=root.TransformPoint(pts[0]);
            if(Physics.CheckSphere(start+Vector3.up*.8f,count.FlightClearRadius+.2f,FlightMask,QueryTriggerInteraction.Ignore)){Debug.Log("HC_COUNT_RETURN_SWOOP skipped reason=start "+start+" is inside geometry");return false;}
            // r10b N9: a blocked leg is re-routed through the nearest clear detour (raised, then to either side); if there is none the
            // swoop is skipped rather than flown through a wall.
            var legs=new List<Vector3>();var prev=start;var log=new System.Text.StringBuilder();
            for(int i=1;i<pts.Length;i++)
            {
                var target=root.TransformPoint(pts[i]);
                if(Clear(prev,target)){legs.Add(target);prev=target;log.Append(" leg"+i+"=clear");continue;}
                if(!SwoopDetour(prev,target,out var via)){Debug.Log("HC_COUNT_RETURN_SWOOP skipped reason=leg "+i+" "+prev+"->"+target+" blocked, no clear detour"+log);return false;}
                legs.Add(via);legs.Add(target);prev=target;log.Append(" leg"+i+"=detour via "+via);
            }
            route.Clear();route.AddRange(legs);routeAt=0;routeGoal=legs[legs.Count-1];routeAir=true;transform.position=start;
            var first=route[0]-transform.position;first.y=0;if(first.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(first);
            swooping=true;flight=Flight.Travel;travelToPerch=false;pendingPerch=-1;perchIndex=-1;speedNow=count.GlideTightSpeed;PickLegLoudness("swoop");
            Debug.Log("HC_COUNT_RETURN_SWOOP begin from="+start+" to="+routeGoal+log+" "+DiagnosticState);
            return true;
        }
        // Nearest clear point between a and b: the midpoint raised 1-4 m, then pushed 1.5-4.5 m to either side (raised a little).
        bool SwoopDetour(Vector3 a,Vector3 b,out Vector3 via)
        {
            var mid=(a+b)*.5f;var along=b-a;along.y=0;var side=along.sqrMagnitude>.01f?Vector3.Cross(Vector3.up,along.normalized):Vector3.right;
            var candidates=new List<Vector3>();
            for(float up=1;up<=4;up+=1)candidates.Add(mid+Vector3.up*up);
            for(float off=1.5f;off<=4.5f;off+=1.5f)foreach(int sign in new[]{1,-1})candidates.Add(mid+side*(sign*off)+Vector3.up*1.5f);
            foreach(var c in candidates)
            {
                if(Physics.CheckSphere(c+Vector3.up*.8f,count.FlightClearRadius,FlightMask,QueryTriggerInteraction.Ignore))continue;
                if(Clear(a,c)&&Clear(c,b)){via=c;return true;}
            }
            via=mid;return false;
        }
        void EndReturnSwoop()
        {
            swooping=false;var player=Game.Player.transform.position;
            Debug.Log("HC_COUNT_RETURN_SWOOP end at="+transform.position+" "+DiagnosticState);
            if(MemoryFresh("return swoop end"))GoLearned(player);else HearPlayerRoughly();
            BeginSearch(new Vector3(Memory.X,Memory.Y,Memory.Z));
        }
        void CountEndHunt()
        {
            if(actor)Debug.Log("HC_COUNT_MODES frames=["+string.Join(",",System.Array.ConvertAll(actor.ModeFrames,f=>f.ToString()))+"] (Animator Mode 0-9 frames so far)");
            CloseDive("the hunt ended");
            swooping=false;pendingWitnessed=false;CanSeePlayer=false;Memory.Clear();witnessedHide=false;contact=0;tell=0;holdTimer=0;nearMissStage=0;staggerTimer=retreatTimer=0;unobserved=0;
            Debug.Log("HC_COUNT_HUNT_END lastSeen="+(lastSeen.HasValue?lastSeen.Value.ToString():"none")+" "+DiagnosticState);
            if(flight==Flight.Dead)return;
            SetRim(count.RimLightScale);
            if(!silhouette.gameObject.activeSelf){flight=Flight.Away;return;}
            if(flight==Flight.Perched&&perchIndex>=0&&PerchFarEnough(perchIndex)){perchTimer=Random.Range(count.PerchMinSeconds,count.PerchMaxSeconds);return;}
            // Fly off (visibly) to a perch that keeps the distance rules; if none is reachable he prowls where he is until unobserved.
            int i=ChoosePerch(-1,false);
            if(i<0||!GoToPerch(i,false)){route.Clear();flight=Flight.Land;}
            Debug.Log("HC_COUNT_PERCH leaving hunt to="+i+" flight="+flight);
        }
        bool attackAttempted;
        float guaranteeClock,aiLogClock,guaranteeNext=10;
        // Seed his memory with a point near the investigator: 1.5-3 m off in the open, 3-5 m off when hidden.
        void HearPlayerRoughly()
        {
            var player=Game.Player.transform.position;bool hidden=Game.Player.Hidden;
            float min=hidden?count.HeardOffsetHiddenMin:count.HeardOffsetMin,max=hidden?count.HeardOffsetHiddenMax:count.HeardOffsetMax;
            Vector3 guess=player;bool found=false;
            // r10 #20: the NavMesh snap can pull a guess back toward her (0.3 m seen while hidden, under the 0.35 m knowsThisHide
            // threshold). A snapped guess closer than the minimum offset is rejected and another bearing is tried.
            for(int attempt=0;attempt<8&&!found;attempt++)
            {
                float r=Random.Range(min,max),a=Random.Range(0f,Mathf.PI*2);var raw=player+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);
                if(!NavMesh.SamplePosition(raw,out var nav,2.5f,NavMesh.AllAreas)){Debug.Log("HC_COUNT_HEARD rejected "+raw+" reason=no NavMesh within 2.5 m");continue;}
                if(Mathf.Abs(nav.position.y-player.y)>=1.6f){Debug.Log("HC_COUNT_HEARD rejected "+nav.position+" reason=other floor");continue;}
                float off=new Vector2(nav.position.x-player.x,nav.position.z-player.z).magnitude;
                if(off<min){Debug.Log("HC_COUNT_HEARD rejected "+nav.position+" reason=snapped to "+off.ToString("F2")+" m < "+min.ToString("F1"));continue;}
                guess=nav.position;found=true;
            }
            if(!found)
            {
                // Open floor only: a point near her on the mesh. Hidden: an unsnapped point at the minimum offset (never near the hide).
                float a=Random.Range(0f,Mathf.PI*2);var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                if(!hidden&&NavMesh.SamplePosition(player,out var nav,1.5f,NavMesh.AllAreas))guess=nav.position+dir*.8f;
                else guess=player+dir*min;
                Debug.Log("HC_COUNT_HEARD fallback reason=no snapped guess in 8 tries hidden="+hidden);
            }
            Memory.Observe(guess.x,guess.y,guess.z);searchedAt=new Vector3(9999,9999,9999);
            Debug.Log("HC_COUNT_HEARD guess="+guess+" offset="+Vector3.Distance(guess,player).ToString("F1")+" hidden="+hidden+" "+DiagnosticState);
        }
        void SetPerch(int i,float seconds)
        {
            Ledge(i,out var feet,out perchYaw);transform.position=feet;
            perchIndex=i;pendingPerch=-1;flight=Flight.Perched;perchTimer=seconds;tell=0;route.Clear();speedNow=0;reperchRolled=false;actor.Pose(Profile.ModeIdle);
        }
        // Perch point -> where his feet go (on a balustrade in front of it, if any) and the most open facing.
        void Ledge(int i,out Vector3 feet,out float yaw)
        {
            var p=Game.World.PerchPoints[i];var eye=p+Vector3.up*count.FlightEyeHeight;float best=-1;var dir=Vector3.forward;
            for(int k=0;k<16;k++)
            {
                var d=Quaternion.Euler(0,k*22.5f,0)*Vector3.forward;
                float reach=Physics.Raycast(eye,d,out var h,30,FlightMask,QueryTriggerInteraction.Ignore)?h.distance:30;
                if(reach>best+.01f){best=reach;dir=d;}
            }
            yaw=Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg;feet=p;
            if(Physics.Raycast(p+Vector3.up*.6f,dir,out var rail,count.LedgeProbe,FlightMask,QueryTriggerInteraction.Ignore))
            {
                var over=rail.point+dir*.06f;
                if(Physics.Raycast(new Vector3(over.x,p.y+1.8f,over.z),Vector3.down,out var top,1.8f,FlightMask,QueryTriggerInteraction.Ignore)&&top.point.y-p.y>.4f&&top.point.y-p.y<1.6f)
                {
                    // Crouch on the rail top with the claws (0.29 m ahead of the ankles in the Perch clip) on its front edge.
                    feet=new Vector3(over.x,top.point.y,over.z);
                    var outside=new Vector3(over.x,top.point.y-.03f,over.z)+dir*1.2f;
                    if(Physics.Raycast(outside,-dir,out var front,1.2f,FlightMask,QueryTriggerInteraction.Ignore)&&Vector3.Dot(front.point-over,dir)<.6f)
                        feet=new Vector3(front.point.x-dir.x*.30f,top.point.y,front.point.z-dir.z*.30f);
                }
            }
        }
        int NearestPerch(Vector3 from,int exclude=-1)
        {
            int best=-1;float bestDistance=float.MaxValue;var perches=Game.World.PerchPoints;
            for(int i=0;i<perches.Count;i++){if(i==exclude)continue;float d=Vector3.Distance(perches[i],from);if(d<bestDistance){bestDistance=d;best=i;}}
            return best;
        }
        bool GoToPerch(int i,bool retreat)
        {
            var perches=Game.World.PerchPoints;if(perches.Count==0){BeginLand();return false;}
            for(int attempt=0;attempt<perches.Count;attempt++)
            {
                int candidate=(i+attempt)%perches.Count;Ledge(candidate,out var feet,out _);
                if(!PlanRoute(feet,true))continue;
                pendingPerch=candidate;travelToPerch=true;flight=retreat?Flight.Retreat:Flight.Travel;PickLegLoudness(retreat?"retreat":"perch");return true;
            }
            Debug.Log("HC_COUNT_ROUTE abort=perch reason=no perch reachable "+DiagnosticState);BeginLand();return false;
        }
        void BeginSearch(Vector3 point)
        {
            searchedAt=point;searchSerial++;
            if(!PlanRoute(point,false)){Debug.Log("HC_COUNT_ROUTE abort=search reason=no route to "+point+" "+DiagnosticState);BeginLand();return;}
            Debug.Log("HC_COUNT_SEARCH to="+point+" waypoints="+route.Count+" "+DiagnosticState);
            travelToPerch=false;pendingPerch=-1;flight=Flight.Travel;PickLegLoudness("search");
        }
        int chaseRouteFails;
        float chaseT0,chaseD0;bool diveCloseLogged;
        bool diveHigh;float grabReach=1.05f;
        // The grab a hide gets: GrabDrop where the ceiling at its stand-off clears the clip's fall, else GrabCrawl (else Mode 6).
        int HideGrabMode(Vector3 feet,Vector3 outward)
        {
            if(count.ModeGrabCrawl>=0&&HasModeState(count.ModeGrabCrawl,"GrabCrawl"))return count.ModeGrabCrawl;   // count-l3
            bool drop=count.ModeGrabDrop>=0&&HasModeState(count.ModeGrabDrop,"GrabDrop");
            if(drop)
            {
                var spot=feet+outward*count.StandOffFor(count.ModeGrabDrop);
                float ceiling=Physics.Raycast(spot+Vector3.up*.2f,Vector3.up,out var hit,12,FlightMask,QueryTriggerInteraction.Ignore)?hit.point.y-feet.y:12;
                if(ceiling>=count.GrabDropClipTop+.1f)return count.ModeGrabDrop;
            }
            return count.ModeGrabCrawl>=0&&HasModeState(count.ModeGrabCrawl,"GrabCrawl")?count.ModeGrabCrawl:count.ModeGrab;
        }
        // r12 (7 of 9 grabs were the same low crawl): where the ceiling over her clears the GrabDrop clip, about half the dives come
        // in high and end in the drop; the rest come in low.
        // fd-grab (r13: side snatch and drop-spin 0 of 12, drops 20 % where the ceiling allows): the dive's style follows the planned
        // grab (PlanVariant): a planned drop or drop-spin comes in high, everything else low.
        void PickDiveStyle()
        {
            var feet=Game.Player.transform.position;
            float ceiling=Physics.Raycast(feet+Vector3.up*.2f,Vector3.up,out var hit,12,FlightMask,QueryTriggerInteraction.Ignore)?hit.point.y-feet.y:12;
            // fe-count: PlanVariant only plans a drop or drop-spin where the ring has the clip's headroom (DropRoomRing), so the plan decides.
            float ringCeiling=ceiling;bool room=count.ModeGrabDrop>=0&&HasModeState(count.ModeGrabDrop,"GrabDrop");
            if(room)room=DropRoomRing(feet,count.GrabDropClipTop,out ringCeiling,out _);
            diveHigh=room&&(plannedMode==count.ModeGrabDrop||plannedMode==count.ModeGrabDropSpin);
            Debug.Log("HC_COUNT_DIVE style="+(diveHigh?"high":"low")+" reason="+(diveHigh?"plan "+PlanName(plannedMode):room?"plan "+PlanName(plannedMode)+" (low)":"no headroom (ceiling "+ceiling.ToString("F2")+", ring "+ringCeiling.ToString("F2")+")")+" ceiling="+ceiling.ToString("F2"));
        }
        // ---- count-l2: the F5 wave L grabs (GrabSideL 10, GrabSideR 11, GrabDropSpin 12) --------------------------------
        // F5 could not confirm left/right under Unity's axis mapping: the first side grab checks where its lead claw is on screen
        // at 0.1 s and swaps the mapping for the rest of the run if it came from the wrong side (HC_COUNT_GRAB side_check=).
        static bool sideSwapped,sideChecked;
        bool IsSide(int mode)=>mode>=0&&(mode==count.ModeGrabSideL||mode==count.ModeGrabSideR);
        int LeftSideMode=>sideSwapped?count.ModeGrabSideR:count.ModeGrabSideL;
        int RightSideMode=>sideSwapped?count.ModeGrabSideL:count.ModeGrabSideR;
        // His bearing from her view direction, degrees, + = on her right.
        float BearingFromHerView()
        {
            var p=Game.Player;var f=p.View.transform.forward;f.y=0;var to=transform.position-p.transform.position;to.y=0;
            if(f.sqrMagnitude<1e-4f||to.sqrMagnitude<1e-4f)return 0;return Vector3.SignedAngle(f,to,Vector3.up);
        }
        float CeilingOver(Vector3 at,float floorY)=>Physics.Raycast(at+Vector3.up*.2f,Vector3.up,out var hit,12,FlightMask,QueryTriggerInteraction.Ignore)?hit.point.y-floorY:12;
        // The grab this dive will end in if it commits now (hide entrances are chosen separately).
        int PredictGrab(out string why)
        {
            var p=Game.Player;var feet=p.transform.position;float bearing=BearingFromHerView(),ab=Mathf.Abs(bearing);
            var toHim=transform.position-feet;toHim.y=0;toHim=toHim.sqrMagnitude>1e-4f?toHim.normalized:-p.transform.forward;
            // fe2-count: an overhead drop-spin (cone) falls on the side chosen when it was committed to, not on his (undefined) bearing.
            if(spinConeDir!=Vector3.zero&&plannedMode==count.ModeGrabDropSpin)
            {toHim=spinConeDir;var vf=p.View.transform.forward;vf.y=0;if(vf.sqrMagnitude>1e-4f){bearing=Vector3.SignedAngle(vf,toHim,Vector3.up);ab=Mathf.Abs(bearing);}}
            if(diveHigh)
            {
                bool spin=count.ModeGrabDropSpin>=0&&HasModeState(count.ModeGrabDropSpin,"GrabDropSpin");
                // fd-grab: the drop-spin is planned (it flew to a point beside / behind her); the camera turns the shortest way.
                if(spin&&plannedMode==count.ModeGrabDropSpin&&ab>=count.SpinMinBearing&&CeilingOver(feet+toHim*count.StandOffFor(count.ModeGrabDropSpin),feet.y)>=count.GrabDropSpinClipTop+.1f)
                {why="planned drop-spin, she faces away (bearing "+bearing.ToString("F0")+")";return count.ModeGrabDropSpin;}
                if(count.ModeGrabDrop>=0&&HasModeState(count.ModeGrabDrop,"GrabDrop")){why="high dive, drop (bearing "+bearing.ToString("F0")+(plannedMode==count.ModeGrabDropSpin?", spin bearing under "+count.SpinMinBearing.ToString("F0"):"")+")";return count.ModeGrabDrop;}
            }
            if(p.Crouched||MustCrawl(transform.position)||OnFoot){int c=count.ModeGrabCrawl>=0&&HasModeState(count.ModeGrabCrawl,"GrabCrawl")?count.ModeGrabCrawl:ChooseGrab(false,out _,out _);why=p.Crouched?"she is crouched":"low door / on foot";return c;}
            // fe3-grab: a planned side snatch is judged on its planned side, in the frame it was planned in (the frame CommitGrab places
            // the clip in): SideGrabRules.CommitOk. FE2 took the side from the sign of his bearing from her view and gated that bearing at
            // 35-110 deg, so a left plan in the -15 deg frame (start at 39 deg of view bearing) reached across her front entered the 0.7 m
            // commit radius under 35 deg and fell to the crawl (r3 natural9 "at the start point, came in low (1.5 m)").
            sideGateWhy="";
            if(IsSide(plannedMode)&&planSide!=0)
            {
                int side=planSide<0?LeftSideMode:RightSideMode;
                var ff=SideFrameForward(planSide);float fb=toHim.sqrMagnitude>1e-4f?Vector3.SignedAngle(ff,toHim,Vector3.up):0;
                if(side<0||!HasModeState(side,side==count.ModeGrabSideL?"GrabSideL":"GrabSideR"))sideGateWhy="no clip";
                else if(!SideGrabRules.CommitOk(planSide,fb,count.SideMinBearing,count.SideMaxBearing,out sideGateWhy)){}
                else if(!SideClearFrame(ff,planSide))sideGateWhy="flank line blocked";
                else{why="planned side snatch from her "+(planSide<0?"left":"right")+" (frame bearing "+fb.ToString("F0")+", view bearing "+bearing.ToString("F0")+(sideSwapped?", mapping swapped":"")+")";return side;}
            }
            int plain=ChooseGrab(false,out why,out _);
            if(sideGateWhy.Length>0)why="side gate: "+sideGateWhy+"; "+why;
            return plain;
        }
        // The flank the side clip enters from must be open: her eye to the stand-off point and that point to the clip's start.
        // fe2-count: in the planned frame (her view turned by planFrameYaw toward his side).
        bool SideClearFrame(Vector3 f,int sign)
        {
            var p=Game.Player;var eye=p.View.transform.position;f.y=0;if(f.sqrMagnitude<1e-4f)return false;f.Normalize();
            // fe3-grab: each side's own stand-off, and his own colliders / hers never block (Blocked ignores them: at the commit he is at
            // the start point the second line ends on).
            var right=Vector3.Cross(Vector3.up,f);var origin=p.transform.position+f*count.StandOffFor(sign<0?LeftSideMode:RightSideMode);
            var start=origin+right*sign*count.SideStartLateral;
            var chest=Vector3.up*1.2f;
            return !Blocked(eye,origin+chest)&&!Blocked(origin+chest,start+chest);
        }
        float eyeHoldSince=-1;string sideGateWhy="";
        float seatHoldT,seatHoldLast,seatPredictGap=-1;
        void BeginChase(){if(huntClock<missCooldownUntil)return;seatHoldT=0;seatHoldLast=0;seatPredictGap=-1;riseHoldT=0;riseHoldLast=0;dropSpotLogged=false;adaptLogged=false;CloseDive("a new dive began");diveOpen=true;diveResolved=false;chaseExit="";eyeHoldSince=-1;hideGrabLocked=false;PlanVariant();PickDiveStyle();chaseT0=huntClock;chaseD0=Vector3.Distance(transform.position,Game.Player.transform.position);diveCloseLogged=false;coneLogged=false;curveLogged=false;chaseRouteFails=0;flight=Flight.Chase;dropHeight=0;diveLastFired=false;diveEndsIn=float.PositiveInfinity;tell=0;holdTimer=0;replan=0;route.Clear();attackAttempted=true;nearMissStage=0;nearNoGrab=0;pathHoldLogged=false;groundLogged=false;ringAge=99;Debug.Log("HC_COUNT_DIVE "+DiagnosticState);Voice("cc_hunt_run");}

        // ---- fd-grab: grab variety, ring points, path clearance, the readable miss ----------------------------------------
        // A deck across runs (PlayerPrefs hc.castle.grabdeck, counts of committed open-floor grabs by mode): each dive plans the eligible
        // grab furthest below its target share (drop 8, drop-spin 12, side 10/11, else the crawl), with a little jitter so it is no cycle.
        int plannedMode=-1,planSide;bool planLocked;Vector3 planPoint;
        const string DeckKey="hc.castle.grabdeck";
        // fe3-grab: the deck is a GrabDeck (Core/GrabPlanRules.cs): commits by mode plus planned-not-committed streaks; a streak of
        // StarveAfter rebalances that mode, and the saved state is normalised so it cannot shut a mode (FE2: five left commits carried
        // into r3, so the right side was tried first on every side plan).
        static GrabDeck deckState;
        static GrabDeck DeckState{get{if(deckState==null)deckState=GrabDeck.Parse(PlayerPrefs.GetString(DeckKey,""),CountClaudeProfile.ModeCount);return deckState;}}
        static int[] Deck()=>DeckState.Commits;
        void SaveDeck()
        {
            var d=DeckState;
            d.Normalize(new[]{new[]{count.ModeGrabSideL,count.ModeGrabSideR}},
                new[]{new[]{count.ModeGrabDrop},new[]{count.ModeGrabDropSpin},new[]{count.ModeGrabSideL,count.ModeGrabSideR},new[]{count.ModeGrabCrawl}},
                new[]{count.DropTargetShare,count.SpinTargetShare,count.SideTargetShare,Mathf.Max(.15f,1-count.DropTargetShare-count.SpinTargetShare-count.SideTargetShare)});
            PlayerPrefs.SetString(DeckKey,d.Serialize());   // saved on quit (no disk write at the commit)
        }
        static string DeckText(){var d=DeckState;var c=d.Commits;var s=d.Streak;return "[8:"+c[8]+" 9:"+c[9]+" 10:"+c[10]+" 11:"+c[11]+" 12:"+c[12]+" streak 10:"+s[10]+" 11:"+s[11]+" 12:"+s[12]+"]";}
        // fe3-grab: a variant plan that ended without its commit counts toward that mode's streak (DropPlan, DiveMiss, CloseDive).
        void PlanNotCommitted(int mode)
        {
            if(mode<0||!(IsSide(mode)||mode==count.ModeGrabDropSpin))return;
            int pair=mode==count.ModeGrabSideL?count.ModeGrabSideR:mode==count.ModeGrabSideR?count.ModeGrabSideL:-1;
            string note=DeckState.PlannedNotCommitted(mode,pair);
            if(note!=null){SaveDeck();Debug.Log("HC_GRAB_DECK rebalance "+note+" deck="+DeckText());}
        }
        string PlanName(int m)=>m<0?"none":m==count.ModeGrabDrop?"drop":m==count.ModeGrabDropSpin?"drop-spin":IsSide(m)?"side-"+(planSide<0?"left":"right"):m==count.ModeGrabCrawl?"crawl":"mode "+m;
        void PlanVariant()
        {
            plannedMode=-1;planSide=0;planLocked=false;flankL=flankR="";flankFirst=0;
            if(witnessedHide||count==null)return;
            var p=Game.Player;var feet=p.transform.position;float ceiling=CeilingOver(feet,feet.y);var d=Deck();
            // fe-count (r13 FD: openHeadroom=0 in every run, drops 0 of 6 open-floor grabs): the drop clips fall at his stand-off ring, not
            // over her head. Under the hall gallery her own ceiling is 2.92 m while the ring a step away is open to the vault, so the old
            // test (her feet) ruled the drop and the drop-spin out of most hall dives. Headroom is now judged on the ring (DropRoomRing).
            bool dropStates=count.ModeGrabDrop>=0&&HasModeState(count.ModeGrabDrop,"GrabDrop");
            float ringCeiling=ceiling;string roomAt="no clip";
            bool dropRoom=dropStates&&DropRoomRing(feet,count.GrabDropClipTop,out ringCeiling,out roomAt);
            // fe-count: every candidate is feasibility-checked before it is planned (an open start point he can fly to directly), so a
            // plan never leads him on a route detour (r13 FD huntcam: a drop-spin point behind the gallery, 20 waypoints, 12 m away).
            string dropWhy="",spinWhy="",sideWhy="";
            // (The drop is the plain dive to a live ring point, as before; only the variants fly to a fixed start point and need DirectTo.)
            bool dropOk=dropRoom&&OpenRing(feet,count.StandOffFor(count.ModeGrabDrop),count.GrabDropClipTop,out _,out dropWhy);
            if(dropStates&&!dropRoom)dropWhy="no headroom on the ring (best "+ringCeiling.ToString("F2")+")";
            // A watched approach cannot end beside or behind her: she turns with him (huntcam, death and kill stare at his head).
            float bearing0=BearingFromHerView();bool facing=ObservedByPlayer()&&Mathf.Abs(bearing0)<count.SideMinBearing;
            bool spinStates=count.ModeGrabDropSpin>=0&&HasModeState(count.ModeGrabDropSpin,"GrabDropSpin");
            int spinSign=0;string spinHow="",sideHow="";float spinBearing=0,sideYaw=0;
            if(!spinStates)spinWhy="no clip";
            else if(!dropRoom)spinWhy="no headroom";
            else if(facing)spinWhy="she faces him";
            else
            {
                // fe2-count: the nearer side first; per side the first open bearing of SpinPlanBearings; reached directly or by a short bend.
                int near=bearing0<0?-1:1;var whys=new List<string>();
                foreach(int sg in new[]{near,-near})
                {
                    if(!SpinPlanPoint(sg,out var sp,out float sb,out string sw)){whys.Add((sg<0?"L ":"R ")+sw);continue;}
                    if(Reachable(sp,true,out string rw,out spinHow)){spinSign=sg;spinBearing=sb;break;}
                    whys.Add((sg<0?"L ":"R ")+rw);
                }
                if(spinSign==0)spinWhy=string.Join("; ",whys);
            }
            bool spinOk=spinSign!=0;
            bool sideStates=count.ModeGrabSideL>=0&&count.ModeGrabSideR>=0&&HasModeState(count.ModeGrabSideL,"GrabSideL")&&HasModeState(count.ModeGrabSideR,"GrabSideR");
            int sideSign=0;
            if(!sideStates)sideWhy="no clip";
            else if(p.Crouched||ceiling<count.CrawlCeiling||MustCrawl(feet))sideWhy="low ceiling or crouched";
            else if(facing)sideWhy="she faces him";
            else
            {
                // fe3-grab: both sides are evaluated on every side plan (each frame yaw: flank open, then its start point reachable) and
                // logged (HC_GRAB_FLANK); the side tried first is the one committed less often, a tie goes to the side he is on
                // (SideGrabRules.FirstSide: mirror-symmetric). FE2 tried the less-used side first by the deck alone and logged only the
                // chosen side, so a shut left flank never showed its reason.
                int first=SideGrabRules.FirstSide(d[LeftSideMode],d[RightSideMode],bearing0,d[count.ModeGrabSideL]+d[count.ModeGrabSideR]);flankFirst=first;
                var hows=new Dictionary<int,string>();
                var plan=SideGrabRules.Choose((sg,y)=>
                {
                    if(!SideFrameOpen(sg,y,out var pt,out string fw))return fw;
                    if(!Reachable(pt,false,out string rw,out string how))return rw;
                    hows[sg*1000+Mathf.RoundToInt(y)]=how;return null;
                },count.SideFrameYaws,first);
                sideSign=plan.Sign;
                if(sideSign!=0){sideYaw=plan.Yaw;hows.TryGetValue(sideSign*1000+Mathf.RoundToInt(sideYaw),out sideHow);}
                else sideWhy="L "+plan.LeftWhy+"; R "+plan.RightWhy;
                flankL=plan.LeftOk?"feasible=True yaw="+plan.LeftYaw.ToString("F0")+" reason=open":"feasible=False reason="+plan.LeftWhy.Replace(' ','_');
                flankR=plan.RightOk?"feasible=True yaw="+plan.RightYaw.ToString("F0")+" reason=open":"feasible=False reason="+plan.RightWhy.Replace(' ','_');
            }
            if(sideWhy.Length>0&&flankL.Length==0){flankL=flankR="feasible=False reason="+sideWhy.Replace(' ','_');}
            bool sideOk=sideSign!=0;
            // fe2-count: after VariantForceAfterAbandons abandoned variant plans in this hunt, the simplest feasible variant is taken.
            bool force=huntVariantAbandons>=count.VariantForceAfterAbandons&&(spinOk&&dropRoom||sideOk);
            float n=1;foreach(int m in new[]{8,9,10,11,12})n+=d[m];
            float dropT=count.DropTargetShare,spinT=count.SpinTargetShare,sideT=count.SideTargetShare,crawlT=Mathf.Max(.15f,1-dropT-spinT-sideT);
            float best=float.MinValue;string scores="";
            void Consider(int mode,bool ok,float target,int have,string name,string why)
            {
                if(!ok){scores+=name+"=n/a("+why+") ";return;}
                float score=target*n-have+Random.value*count.PlanJitter;scores+=name+"="+score.ToString("F2")+" ";
                if(score>best){best=score;plannedMode=mode;}
            }
            Consider(count.ModeGrabDrop,dropOk,dropT,d[8],"drop",dropWhy);
            Consider(count.ModeGrabDropSpin,spinOk,spinT,d[12],"spin",spinWhy);
            Consider(sideSign<0?LeftSideMode:RightSideMode,sideOk,sideT,d[10]+d[11],"side",sideWhy);
            // ff-count (r14: mode 9 was 10 of 17 grabs): the crawl is planned only while its deck share stays at or under CrawlMaxShare, unless
            // nothing else is feasible here (no headroom, no flank: the hall gallery).
            float crawlShare=0;bool crawlOk=count.ModeGrabCrawl>=0&&GrabMixRules.CrawlAllowed(d,count.ModeGrabCrawl,new[]{8,9,10,11,12},count.CrawlMaxShare,dropOk||spinOk||sideOk,out crawlShare);
            Consider(count.ModeGrabCrawl,crawlOk,crawlT,d[9],"crawl",count.ModeGrabCrawl<0?"no clip":"crawl share "+crawlShare.ToString("F2")+" over "+count.CrawlMaxShare.ToString("F2"));
            string forced="";
            if(force&&!(plannedMode==count.ModeGrabDropSpin||IsSide(plannedMode)))
            {
                plannedMode=spinOk&&dropRoom?count.ModeGrabDropSpin:sideSign<0?LeftSideMode:RightSideMode;
                forced=" forced=True reason=abandoned "+huntVariantAbandons+" variant plans this hunt";
            }
            // ff-count (r14: both side plans were dropped and no side followed): a side plan dropped earlier in this hunt is owed; this dive plans
            // the side if one is feasible.
            if(huntSideOwed&&sideOk&&!IsSide(plannedMode)){plannedMode=sideSign<0?LeftSideMode:RightSideMode;forced=" forced=True reason=side owed (a side plan was dropped this hunt)";}
            if(IsSide(plannedMode)){planSide=sideSign;planFrameYaw=sideYaw;planHow=sideHow;}
            if(plannedMode==count.ModeGrabDropSpin){planSide=spinSign;planSpinBearing=spinBearing;planHow=spinHow;}
            spinConeDir=Vector3.zero;
            Debug.Log("HC_GRAB_VARIANT plan="+PlanName(plannedMode)+" mode="+plannedMode+" scores=["+scores.Trim()+"] ceiling="+ceiling.ToString("F2")+" ring="+ringCeiling.ToString("F2")+" roomAt="+roomAt+" bearing="+bearing0.ToString("F0")+" facing="+facing+
                (IsSide(plannedMode)?" frameYaw="+planFrameYaw.ToString("F0")+" approach="+planHow:plannedMode==count.ModeGrabDropSpin?" spinBearing="+planSpinBearing.ToString("F0")+" approach="+planHow:"")+forced+" deck="+DeckText());
            // fe3-grab: every side plan logs both flanks (stage=plan), then the chosen side (or none).
            {
                string chosen=IsSide(plannedMode)?(planSide<0?"L":"R"):"none";
                Debug.Log("HC_GRAB_FLANK side=L stage=plan "+flankL+" bearing="+bearing0.ToString("F0")+" chosen="+chosen+" first="+(flankFirst<0?"L":flankFirst>0?"R":"none"));
                Debug.Log("HC_GRAB_FLANK side=R stage=plan "+flankR+" bearing="+bearing0.ToString("F0")+" chosen="+chosen+" first="+(flankFirst<0?"L":flankFirst>0?"R":"none"));
            }
        }
        string flankL="",flankR="";int flankFirst;
        // fe2-count: abandoned variant plans this hunt (dropped mid-dive or a dive that ended without its variant); the plan's frame turn
        // (side), start bearing (spin) and approach (direct | curved n | near n) as planned; the overhead drop-spin's direction.
        bool coneLogged,curveLogged,huntSideOwed;int huntVariantAbandons;float planFrameYaw,planSpinBearing;string planHow="";Vector3 spinConeDir;
        // fe2-count: can he get to a variant's start point? Directly (DirectTo), by a short bend (a NavMesh route of at most
        // VariantRouteMaxWaypoints legs no longer than VariantRouteMaxRatio x the straight line + 1 m), or, within VariantNearRadius of him,
        // by any route up to VariantNearRouteMax m when its last sample (the start point at his hover height) sees her eye. Only called
        // while planning a new dive (BeginChase clears the route afterwards).
        bool Reachable(Vector3 ground,bool high,out string why,out string how)
        {
            how="direct";if(DirectTo(ground,high,out why))return true;
            float ceiling=CeilingOver(ground,ground.y);
            float hover=high?Mathf.Clamp(ceiling-count.BodyClearance,0,count.HighDiveHover):Mathf.Min(count.HoverNearPlayer,Mathf.Max(0,ceiling-count.BodyClearance));
            var end=ground+Vector3.up*hover;float straight=Vector3.Distance(transform.position,end);
            if(!PlanRoute(ground,false)){route.Clear();why="no route to its start point";how="";return false;}
            float len=RouteLength(transform.position);int legs=route.Count;route.Clear();routeAt=0;
            if(legs<=count.VariantRouteMaxWaypoints&&len<=straight*count.VariantRouteMaxRatio+1){how="curved "+legs;why="";return true;}
            var eye=Game.Player.View.transform.position;
            if(straight<=count.VariantNearRadius&&len<=count.VariantNearRouteMax&&!Blocked(end+Vector3.up*1.2f,eye)){how="near "+legs;why="";return true;}
            why="detour "+legs+" legs "+len.ToString("F1")+" m (straight "+straight.ToString("F1")+")";how="";return false;
        }
        float RouteLength(Vector3 from){float len=0;var a=from;for(int i=routeAt;i<route.Count;i++){len+=Vector3.Distance(a,route[i]);a=route[i];}return len;}
        // The current route to a variant's start point is acceptable (the same rule as Reachable, from where he is now).
        bool RouteAcceptable(out string why)
        {
            int legs=route.Count-routeAt;float len=RouteLength(transform.position);float straight=route.Count>0?Vector3.Distance(transform.position,route[route.Count-1]):0;
            why=legs+" legs "+len.ToString("F1")+" m (straight "+straight.ToString("F1")+")";
            if(legs<=2)return true;
            if(legs<=count.VariantRouteMaxWaypoints&&len<=straight*count.VariantRouteMaxRatio+1)return true;
            return straight<=count.VariantNearRadius&&len<=count.VariantNearRouteMax;
        }
        // fe-count: is there headroom for a drop clip (top over her floor) where it would fall: over her, or at a floored stand-off point
        // on the ring around her (his bearing first). best = the highest ceiling seen; at = where (her / ring <deg> / none).
        bool DropRoomRing(Vector3 feet,float top,out float best,out string at)
        {
            best=CeilingOver(feet,feet.y);at="her";
            if(best>=top+.1f)return true;
            var away=transform.position-feet;away.y=0;
            if(away.sqrMagnitude<1e-4f){away=-Game.Player.View.transform.forward;away.y=0;}
            if(away.sqrMagnitude<1e-4f)away=Vector3.forward;away.Normalize();
            float so=count.StandOffFor(count.ModeGrabDrop);
            for(int i=0;i<=count.RingSteps*2;i++)
            {
                int k=(i+1)/2*(i%2==1?1:-1);var spot=feet+Quaternion.Euler(0,k*count.RingStepDegrees,0)*away*so;
                if(!FloorAt(spot,feet.y))continue;
                float c=CeilingOver(spot,feet.y);if(c>best)best=c;
                if(c>=top+.1f){at="ring "+(k*count.RingStepDegrees).ToString("F0");return true;}
            }
            at="none";return false;
        }
        // fe-count: the open ring bearing nearest his own (SpotOpen at standOff with the clip's top), as a direction from her; false if none.
        bool OpenRing(Vector3 feet,float standOff,float top,out Vector3 dir,out string why)
        {
            var away=transform.position-feet;away.y=0;
            if(away.sqrMagnitude<1e-4f){away=-Game.Player.View.transform.forward;away.y=0;}
            if(away.sqrMagnitude<1e-4f)away=Vector3.forward;away.Normalize();
            why="";
            for(int i=0;i<=count.RingSteps*2;i++)
            {
                int k=(i+1)/2*(i%2==1?1:-1);dir=Quaternion.Euler(0,k*count.RingStepDegrees,0)*away;
                if(SpotOpen(feet,feet+dir*standOff,top,out var b)){why="";return true;}
                if(i==0)why=b;
            }
            dir=away;why="no open ring bearing ("+why+")";return false;
        }
        // fe-count: can he fly from where he is straight to a point over this ground spot (high = the high dive's hover), directly or after
        // lifting off his ledge (PlanRoute's first two tries)? A plan that needs the NavMesh detour is not feasible.
        bool DirectTo(Vector3 ground,bool high,out string why)
        {
            float ceiling=CeilingOver(ground,ground.y);
            float hover=high?Mathf.Clamp(ceiling-count.BodyClearance,0,count.HighDiveHover):Mathf.Min(count.HoverNearPlayer,Mathf.Max(0,ceiling-count.BodyClearance));
            var start=transform.position;var end=ground+Vector3.up*hover;var launch=start+transform.forward*.6f+Vector3.up*.5f;
            if(Clear(start,end)||Clear(start,launch)&&Clear(launch,end)){why="";return true;}
            why="no direct flight to its start point";return false;
        }
        // fe-count: a variant whose geometry fails mid-dive falls to the next feasible grab for this dive's style, never to nothing: the other
        // side's snatch (if its flank is open and he can fly there directly), else the drop for a high dive with ring headroom, else the
        // plain grab (PredictGrab: the crawl). Logged with next=.
        void DropPlan(string why)
        {
            if(plannedMode<0||!(IsSide(plannedMode)||plannedMode==count.ModeGrabDropSpin))return;
            string from=PlanName(plannedMode);int next=-1;
            var feet=Game.Player.transform.position;
            if(!diveResolved){huntVariantAbandons++;PlanNotCommitted(plannedMode);}   // fe2-count: a plan already committed this dive is not an abandon
            if(IsSide(plannedMode)&&!why.StartsWith("not committed",System.StringComparison.Ordinal)&&!why.StartsWith("detour",System.StringComparison.Ordinal)&&SidePlanPoint(-planSide,out var other,out float oy,out _)&&DirectTo(other,false,out _))
            {planSide=-planSide;planFrameYaw=oy;next=planSide<0?LeftSideMode:RightSideMode;planLocked=false;}
            else if(diveHigh&&count.ModeGrabDrop>=0&&HasModeState(count.ModeGrabDrop,"GrabDrop")&&DropRoomRing(feet,count.GrabDropClipTop,out _,out _))next=count.ModeGrabDrop;
            if(IsSide(plannedMode)&&!IsSide(next))huntSideOwed=true;   // ff-count: the next dive of this hunt plans a side if it can
            plannedMode=next;if(!IsSide(next))planLocked=false;
            Debug.Log("HC_GRAB_VARIANT dropped="+from+" reason="+why+" next="+(next<0?"plain":PlanName(next))+" t="+(huntClock-chaseT0).ToString("F1")+" abandonsThisHunt="+huntVariantAbandons);
        }
        bool VariantPlanned=>plannedMode>=0&&(IsSide(plannedMode)||plannedMode==count.ModeGrabDropSpin);
        // The side clip's start: its origin at the stand-off straight ahead of her view, he starts SideStartLateral to that side.
        // fe2-count: the frame (her view) may turn SideFrameYaws toward his side, so a flank blocked straight ahead can open a little round.
        bool SidePlanPoint(int sign,out Vector3 point)=>SidePlanPoint(sign,out point,out _,out _);
        bool SidePlanPoint(int sign,out Vector3 point,out float yaw,out string why)
        {
            point=Game.Player.transform.position;yaw=0;why="no view direction";string first=null;
            foreach(float y in count.SideFrameYaws)
            {
                if(SideFrameOpen(sign,y,out var pt,out string w)){point=pt;yaw=y;why="";return true;}
                if(first==null)first=w;
            }
            why=first??why;return false;
        }
        // fe3-grab: one frame yaw of one side: the flank lines open, floor and room for his body at the flank start.
        bool SideFrameOpen(int sign,float y,out Vector3 point,out string why)
        {
            var p=Game.Player;var feet=p.transform.position;var f0=p.View.transform.forward;f0.y=0;point=feet;
            if(f0.sqrMagnitude<1e-4f){why="no view direction";return false;}f0.Normalize();
            var f=Quaternion.Euler(0,y*sign,0)*f0;var pt=SideStart(feet,f,sign);
            string w=!SideClearFrame(f,sign)?"flank line blocked":!FloorAt(pt,feet.y)?"no floor at the flank start":!BodyFits(pt)?"no room at the flank start":null;
            if(w!=null){why="flank closed ("+w+" at yaw "+(y*sign).ToString("F0")+")";return false;}
            point=pt;why="";return true;
        }
        // ff-count: re-frame a planned side snatch from where he is (GrabMixRules.AdaptFrame): the frame turned so his bearing in it is the
        // clip's start bearing on the side he is on, at most SideAdaptMaxTurn from her view, with that frame's flank lines open. Sets the plan
        // (side, mode, frame yaw, start point = where he is) and returns true; false with the reason otherwise.
        bool AdaptSideFrame(out string why)
        {
            var p=Game.Player;var feet=p.transform.position;var f0=p.View.transform.forward;f0.y=0;
            if(f0.sqrMagnitude<1e-4f){why="no view direction";return false;}f0.Normalize();
            float vb=BearingFromHerView();
            if(Mathf.Abs(vb)<count.SideMinBearing*.5f){why="she faces him (bearing "+vb.ToString("F0")+")";return false;}
            int sg=vb<0?-1:1;var st=SideStart(feet,f0,sg)-feet;st.y=0;float sb=Mathf.Abs(Vector3.SignedAngle(f0,st,Vector3.up));
            if(!GrabMixRules.AdaptFrame(vb,sb,count.SideAdaptMaxTurn,out int sign,out float yaw,out why))return false;
            int mode=sign<0?LeftSideMode:RightSideMode;
            if(mode<0||!HasModeState(mode,mode==count.ModeGrabSideL?"GrabSideL":"GrabSideR")){why="no clip";return false;}
            var f=Quaternion.Euler(0,yaw,0)*f0;
            if(!SideClearFrame(f,sign)){why="flank line blocked in the re-framed view (turn "+yaw.ToString("F0")+")";return false;}
            planSide=sign;plannedMode=mode;planFrameYaw=yaw*sign;planPoint=new Vector3(transform.position.x,feet.y,transform.position.z);planLocked=true;
            return true;
        }
        // The side frame for a plan: her view turned by the planned frame yaw toward the planned side.
        Vector3 SideFrameForward(int sign)
        {
            var f=Game.Player.View.transform.forward;f.y=0;f=f.sqrMagnitude>1e-4f?f.normalized:Game.Player.transform.forward;
            return Quaternion.Euler(0,planFrameYaw*sign,0)*f;
        }
        Vector3 SideStart(Vector3 feet,Vector3 f,int sign)
        {
            var right=Vector3.Cross(Vector3.up,f);
            int mode=sign<0?LeftSideMode:RightSideMode;
            var point=feet+f*count.StandOffFor(mode)+right*sign*count.SideStartLateral;point.y=feet.y;
            if(sideStartLocal.TryGetValue(mode,out var local)&&local.x*-sign>0)   // his +x is her left (-sign side)
            {
                var origin=feet+f*count.StandOffFor(mode);var start=origin+Quaternion.LookRotation(-f)*new Vector3(local.x,0,local.z);
                point=new Vector3(start.x,feet.y,start.z);
            }
            return point;
        }
        // The drop-spin lands beside / behind her (SpinPlanBearing off her view, on the side he is on), where the ceiling clears its fall.
        // fe2-count: the first open bearing of SpinPlanBearings on that side.
        bool SpinPlanPoint(int sign,out Vector3 point)=>SpinPlanPoint(sign,out point,out _,out _);
        bool SpinPlanPoint(int sign,out Vector3 point,out float bearing,out string why)
        {
            var p=Game.Player;var feet=p.transform.position;var f=p.View.transform.forward;f.y=0;point=feet;bearing=0;why="no view direction";
            if(f.sqrMagnitude<1e-4f)return false;f.Normalize();string first=null;
            foreach(float b in count.SpinPlanBearings)
            {
                var dir=Quaternion.Euler(0,sign*b,0)*f;var pt=feet+dir*count.StandOffFor(count.ModeGrabDropSpin);pt.y=feet.y;
                if(SpotOpen(feet,pt,count.GrabDropSpinClipTop,out var blocker)){point=pt;bearing=b;why="";return true;}
                if(first==null)first=blocker+" at "+b.ToString("F0");
            }
            why="no open start point ("+first+")";return false;
        }
        bool FloorAt(Vector3 spot,float floorY)
        {
            if(!Physics.Raycast(spot+Vector3.up*1.9f,Vector3.down,out var hit,2.4f,FlightMask,QueryTriggerInteraction.Ignore))return false;
            // fe2-count (r13 FE natural1 at the forge altar: she stood 0.27 m up a step, every ring, flank and spin point read "no floor at her
            // level"): a step of up to FloorStep between her feet and his stand-off counts as her level.
            return Mathf.Abs(hit.point.y-floorY)<count.FloorStep;
        }
        bool IgnoreHit(Collider c)=>!c||c.transform.IsChildOf(transform)||c.GetComponentInParent<Investigator>();
        bool BodyFits(Vector3 spot)
        {
            foreach(var c in Physics.OverlapCapsule(spot+Vector3.up*.65f,spot+Vector3.up*1.45f,.3f,FlightMask,QueryTriggerInteraction.Ignore))if(!IgnoreHit(c))return false;
            return true;
        }
        // Nothing between his stand-off and her: the grab clip's body sweep (0.35-1.75 m over her floor, 0.3 m radius) and her eye line
        // to his skull. r13 death: GrabCrawl went through the giant skeleton's ribs (11 collision boxes, tools/castle_skeleton_footprint.py).
        bool GrabPathClear(Vector3 feet,Vector3 at,out string blocker)
        {
            blocker=null;var a=new Vector3(at.x,feet.y,at.z);var d=feet-a;d.y=0;float len=d.magnitude;if(len<.3f)return true;d/=len;
            foreach(var h in Physics.CapsuleCastAll(a+Vector3.up*.65f,a+Vector3.up*1.45f,.3f,d,Mathf.Max(0,len-.55f),FlightMask,QueryTriggerInteraction.Ignore))
                if(!IgnoreHit(h.collider)){blocker=h.collider.name;return false;}
            var eye=Game.Player.View.transform.position;
            if(Physics.Linecast(eye,a+Vector3.up*1.5f,out var hit,FlightMask,QueryTriggerInteraction.Ignore)&&!IgnoreHit(hit.collider)){blocker="eye line, "+hit.collider.name;return false;}
            return true;
        }
        bool SpotOpen(Vector3 feet,Vector3 spot,float top,out string blocker)
        {
            if(!FloorAt(spot,feet.y)){blocker="no floor at her level";return false;}
            if(!BodyFits(spot)){blocker="no room for his body";return false;}
            if(top>0&&CeilingOver(spot,feet.y)<top+.1f){blocker="ceiling under the clip's fall";return false;}
            return GrabPathClear(feet,spot,out blocker);
        }
        // The ring bearing nearest his own whose stand-off point is open (SpotOpen); his own bearing if none is.
        Vector3 ringDir;float ringAge=99;string ringWhy="";
        Vector3 RingDirection(Vector3 feet,Vector3 away,float standOff,float top)
        {
            for(int i=0;i<=count.RingSteps*2;i++)
            {
                int k=(i+1)/2*(i%2==1?1:-1);float ang=k*count.RingStepDegrees;
                var dir=Quaternion.Euler(0,ang,0)*away;
                if(!SpotOpen(feet,feet+dir*standOff,top,out var b)){if(i==0)ringWhy=b;continue;}
                ringWhy=i==0?"own bearing open":"turned "+ang.ToString("F0")+" deg (own bearing: "+ringWhy+")";
                return dir;
            }
            ringWhy="no open bearing ("+ringWhy+")";return away;
        }
        // r13 huntcam: the high dive ended over the skeleton's collision boxes (their top read as ground), 4.4 m over her, and hovered
        // there for the rest of the hunt. Near her without a commit for DiveStallSeconds: he pulls up and leaves, a readable miss.
        float nearNoGrab,pathHoldSince=-1,missCooldownUntil=-1;bool pathHoldLogged,groundLogged;string gateWhy="";
        void DiveMiss(string why,float distance,float vertical)
        {
            if(VariantPlanned){huntVariantAbandons++;PlanNotCommitted(plannedMode);}
            Debug.Log("HC_COUNT_DIVE miss=True reason="+why+" distance="+distance.ToString("F2")+" vertical="+vertical.ToString("F2")+" plan="+PlanName(plannedMode)+" "+DiagnosticState);
            nearNoGrab=0;pathHoldSince=-1;plannedMode=-1;missCooldownUntil=huntClock+2.5f;diveResolved=true;   // he is seen leaving before any new dive
            searchedAt=Game.Player.transform.position;tell=0;
            GoToPerch(Mathf.Max(0,NearestPerch(transform.position)),false);
        }
        // fe-count: every dive ends in a commit, a readable miss (DiveMiss) or this line: a dive that ended any other way (lost sight, no
        // route, the hunt ended, a sword stagger, the run quit mid-dive) logs HC_COUNT_DIVE miss=True reason=abandoned: <why>.
        bool diveOpen,diveResolved;string chaseExit="";
        void CloseDive(string why)
        {
            if(!diveOpen)return;diveOpen=false;if(diveResolved)return;diveResolved=true;if(VariantPlanned){huntVariantAbandons++;PlanNotCommitted(plannedMode);}
            var p=Game&&Game.Player?Game.Player.transform.position:transform.position;var flat=p-transform.position;float vertical=Mathf.Abs(flat.y);flat.y=0;
            Debug.Log("HC_COUNT_DIVE miss=True reason=abandoned: "+why+(chaseExit.Length>0?" ("+chaseExit+")":"")+" distance="+flat.magnitude.ToString("F2")+" vertical="+vertical.ToString("F2")+" plan="+PlanName(plannedMode)+" t="+(huntClock-chaseT0).ToString("F1")+" "+DiagnosticState);
        }
        void OnApplicationQuit(){if(count!=null)CloseDive("the run quit mid-dive");}
        /// <summary>Playtest driver: close a dive still open when the run ends (logs its miss=True line before the grab summary).</summary>
        public void CloseOpenDive(string why){if(count!=null)CloseDive(why);}
        void BeginLand(){flight=Flight.Land;landTimer=count.SearchLandSeconds;route.Clear();scanClock=0;}

        // Returns true when the Count handled the frame (the shared non-hunt code still runs presentations).
        bool CountFrame()
        {
            // Measured flight speed for the wing loop (a teleport reads as 0, not a burst).
            if(Time.deltaTime>0)
            {
                var vel=(transform.position-lastFramePosition)/Time.deltaTime;float v=vel.magnitude;
                flightSpeed=v>20?0:Mathf.Lerp(flightSpeed,v,1-Mathf.Exp(-8*Time.deltaTime));
                if(v<=20)flightVelocity=Vector3.Lerp(flightVelocity,vel,1-Mathf.Exp(-8*Time.deltaTime));
            }
            lastFramePosition=transform.position;
            if(choreo){SetRim(1);CaptureChoreography(Mathf.Min(Time.deltaTime,1f/30));return true;}
            if(staged){SetRim(1);return true;}
            float dt=Time.deltaTime;
            if(flight==Flight.Dead)
            {
                if(GroundUnder(transform.position,out var ground)&&ground.y<transform.position.y)transform.position=Vector3.MoveTowards(transform.position,ground,3*dt);
                actor.Pose(Profile.ModeDeath);return true;
            }
            if(Game.Modal)return true;
            SyncHunt();
            if(huntOn)
            {
                CountHunt(dt);
                if(CaptureMode.Requested)KeepCameraClear();
                return true;
            }
            // Between hunts. The shared code keeps the containment reach, scripted apparitions and (in captures / when the
            // perch AI is off) the hidden silhouette; afterwards he is unplaced and re-perches unobserved.
            bool shared=!BetweenAllowed||presentationRemaining>0||Game.Case.Phase==Phase.Containment&&Game.Case.SealProgress>.02f;
            if(shared){gateStage=0;if(flight!=Flight.Away){flight=Flight.Away;perchIndex=-1;route.Clear();}SetRim(count.RimLightScale);return false;}
            CountBetween(dt);
            return true;
        }
        void CountHunt(float dt)
        {
            var fight=CountFight;bool staggered,retreating;
            if(fight!=null&&fight.Active){staggered=fight.Staggered;retreating=fight.Retreating;staggerTimer=retreatTimer=0;}
            else
            {
                if(staggerTimer>0){staggerTimer-=dt;if(staggerTimer<=0)retreatTimer=count.StaggerRetreatSeconds;}
                else if(retreatTimer>0)retreatTimer-=dt;
                staggered=staggerTimer>0;retreating=retreatTimer>0;
            }
            if(staggered)
            {
                // Hit by the sword: recoil on the ground (the Stagger clip is planted), nothing else.
                flight=Flight.Stagger;contact=0;CanSeePlayer=false;route.Clear();
                if(GroundUnder(transform.position,out var ground))transform.position=Vector3.MoveTowards(transform.position,ground,4*dt);
                actor.Pose(Profile.ModeStagger);return;
            }
            if(flight==Flight.Stagger)
            {
                // Recovered: retreat to the nearest perch for the retreat window, then come back lower and angrier.
                if(!GoToPerch(Mathf.Max(0,NearestPerch(transform.position)),true))flight=Flight.Land;
                Debug.Log("HC_COUNT_RETREAT "+DiagnosticState);
            }
            huntClock+=dt;
            // r12: in the dive only (Chase, Hunt), 2.5-9 m from her, the shoulder rim brightens so he reads against the window light.
            float toHer=Vector3.Distance(transform.position,Game.Player.transform.position);
            // fd-grab: the rim holds through a grab (no lighting change across the commit).
            if(!grabCam)SetRim(flight==Flight.Chase&&toHer>=count.DiveRimNear&&toHer<=count.DiveRimFar?count.DiveRimScale:count.RimLightScale);
            Memory.Tick(dt);CanSeePlayer=!retreating&&CountSees();
            if(CanSeePlayer){var seen=Game.Player.transform.position;Memory.Observe(seen.x,seen.y,seen.z);Remember(seen,true);}
            // Item 13: "found" when he regains sight of you within 6 m.
            if(CanSeePlayer&&!sawPlayer&&Vector3.Distance(transform.position,Game.Player.transform.position)<count.FoundVoiceDistance)Voice("cc_hunt_found");
            sawPlayer=CanSeePlayer;
            // Integrator request: +DreadSeen (15) the first time in a hunt she sees him (in her view, within SightRange).
            if(!seenThisHunt&&ObservedByPlayer()&&Vector3.Distance(Game.Player.View.transform.position,transform.position+Vector3.up*1.2f)<Profile.SightRange)
            {
                seenThisHunt=true;Game.Case.AddDread(Game.Case.Rules.DreadSeen);
                Debug.Log("HC_COUNT_DREAD seen +"+Game.Case.Rules.DreadSeen+" dread="+Game.Case.Dread.ToString("F1")+" "+DiagnosticState);
            }
            // A dive that got within reach of a grab and ended without a kill is a grab survived (+DreadGrabSurvived, 20).
            if(flight==Flight.Chase)chaseClosest=Mathf.Min(chaseClosest,Vector3.Distance(transform.position,Game.Player.transform.position));
            else if(chaseClosest<99)
            {
                if(!diveCloseLogged){diveCloseLogged=true;Debug.Log("HC_COUNT_DIVE close_s=none from="+chaseD0.ToString("F2")+" closest="+chaseClosest.ToString("F2")+" reason=dive ended before grab range");}
                if(chaseClosest<count.GrabSurvivedDistance)GrabSurvived("dive missed, closest "+chaseClosest.ToString("F2")+" m");
                chaseClosest=99;
            }
            else chaseClosest=99;
            bool hostile=HostileReturn;
            if(hostile&&!retreating&&!swooping&&flight!=Flight.Chase){returnHearClock-=dt;if(returnHearClock<=0){returnHearClock=count.ReturnHearSeconds;HearPlayerRoughly();}}
            // r10 #16/#28: he saw her go into a hide (from a perch, in flight or in a dive): he comes and opens it, from any state.
            if(pendingWitnessed&&!retreating&&!swooping&&flight!=Flight.Stagger&&flight!=Flight.NearMiss)
            {
                pendingWitnessed=false;
                // r10b N2: in a dive he keeps diving: CountChase goes for knownHide's entrance and grabs there (~0.3 s), the
                // telegraphed fly/land/crawl/pause sequence is for hides he saw from a perch, in flight or on foot.
                if(!Game.Player.Hidden||!witnessedHide)Debug.Log("HC_COUNT_WITNESSED abort=left reason=she is no longer in that hide "+DiagnosticState);
                else if(flight==Flight.Chase)Debug.Log("HC_COUNT_WITNESSED chase=True reason=already diving, entrance grab "+DiagnosticState);
                else BeginHideVisit(true);
            }
            // Item 10: once per hunt, hidden (torch off) and unwitnessed, roll for the cupboard near-miss.
            // r12 (within-match learning): the hide he last found her in or near is checked first: no attempt roll, sooner.
            bool inLearned=Game.Player.Hidden&&learnedHide&&HideSpotAt(Game.Player.HideExit)==learnedHide;
            if(!nearMissRolled&&!hostile&&!retreating&&flight!=Flight.Chase&&flight!=Flight.NearMiss&&Game.Player.Hidden&&!Game.Player.TorchOn&&!witnessedHide&&huntClock>=(inLearned?count.LearnedHideAfterSeconds:count.NearMissAfterSeconds))
            {
                // r11: more near-misses where it matters: he lost sight of her near the hide she is now in (0.33 -> 0.5).
                bool lostNear=lastSeen.HasValue&&lastSeenSighted&&Game.Case.Elapsed-lastSeenAt<count.NearMissLostSeconds&&Vector3.Distance(lastSeen.Value,Game.Player.transform.position)<count.NearMissLostRadius;
                float chance=inLearned?1:lostNear?count.NearMissChanceLost:count.NearMissChance;
                // fe2-count (r13 FE siege_held 17/18 survived: siege1 failed six 0.33 attempts in a row and never came to her hide): at
                // Dread >= BagDread, after NearMissAttemptPity failed attempts in a row he checks her hide on this hunt.
                bool pity=!inLearned&&Game.Case.Dread>=count.BagDread&&nearMissAttemptFails>=count.NearMissAttemptPity;
                if(pity)chance=1;
                nearMissRolled=true;float roll=Random.value;bool go=roll<chance;
                nearMissAttemptFails=go?0:nearMissAttemptFails+1;
                Debug.Log("HC_COUNT_NEARMISS attempt roll="+roll.ToString("F2")+" chance="+chance.ToString("F2")+" reason="+(inLearned?"learned hide "+learnedHide.name:pity?"pity after "+count.NearMissAttemptPity+" failed attempts":lostNear?"lost her near the hide "+Vector3.Distance(lastSeen.Value,Game.Player.transform.position).ToString("F1")+" m, "+(Game.Case.Elapsed-lastSeenAt).ToString("F0")+" s ago":"base")+" go="+go+" openP="+openDice.ChanceNow.ToString("F2")+" "+DiagnosticState);
                if(go)BeginHideVisit(false);
            }
            if(!retreating&&Game.Player.Hidden&&Game.Player.TorchOn)Hear(Game.Player.transform.position,Profile.HiddenTorchHearRadius);
            breath-=dt;if(CanSeePlayer)quietSeen=true;
            if(breath<=0)
            {
                if(QuietWindow){if(!quietHeldLogged){quietHeldLogged=true;Debug.Log("HC_COUNT_ONESHOT held=breath t="+huntClock.ToString("F1")+" reason="+quietReason+" (first 8 s, not seen)");}}
                else{breath=Profile.BreathInterval;Game.Sound.Cue("breath",transform.position+Vector3.up,Profile.BreathVolume);}
            }
            var memory=new Vector3(Memory.X,Memory.Y,Memory.Z);
            bool freshMemory=!retreating&&Memory.Knows&&Vector3.Distance(memory,searchedAt)>2;
            switch(flight)
            {
                case Flight.Perched:
                {
                    var face=CanSeePlayer?Mathf.Atan2(Game.Player.transform.position.x-transform.position.x,Game.Player.transform.position.z-transform.position.z)*Mathf.Rad2Deg:perchYaw;
                    transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.Euler(0,face,0),90*dt);
                    actor.Pose(Profile.ModeIdle);
                    if(retreating)break;
                    perchTimer-=dt;
                    // Hostile return: no perch tell, he dives as soon as he sees you.
                    if(CanSeePlayer){tell+=dt;if(hostile||tell>=count.PerchTellSeconds)BeginChase();break;}
                    tell=Mathf.Max(0,tell-dt);
                    if(perchTimer>0)break;
                    if(freshMemory){BeginSearch(memory);break;}
                    if(Game.World.PerchPoints.Count>1)GoToPerch((perchIndex+1)%Game.World.PerchPoints.Count,false);
                    else BeginLand();
                    break;
                }
                case Flight.Travel:
                case Flight.Retreat:
                {
                    if(swooping)
                    {
                        // The return swoop: seen, not hunting. No dive, no search until it ends.
                        Fly(dt,Profile.ModeMove);
                        if(routeAt>=route.Count)EndReturnSwoop();
                        break;
                    }
                    if(!retreating&&CanSeePlayer){BeginChase();break;}
                    if(!retreating&&(travelToPerch?freshMemory:freshMemory&&Vector3.Distance(memory,routeGoal)>2)){BeginSearch(memory);break;}
                    Fly(dt,Profile.ModeMove);
                    if(routeAt>=route.Count)
                    {
                        if(travelToPerch&&pendingPerch>=0)SetPerch(pendingPerch,Random.Range(count.HuntPerchMinSeconds,count.HuntPerchMaxSeconds));
                        else BeginLand();
                    }
                    break;
                }
                case Flight.Land:
                {
                    // Lands where it heard / last saw you and prowls (Mode 2), head scanning, then takes a perch.
                    if(GroundUnder(transform.position,out var ground))transform.position=Vector3.MoveTowards(transform.position,ground,2.5f*dt);
                    scanClock+=dt;transform.rotation*=Quaternion.Euler(0,Mathf.Sin(scanClock*1.4f)*40*dt,0);
                    actor.Pose(Profile.ModeSearch);
                    if(retreating){GoToPerch(Mathf.Max(0,NearestPerch(transform.position)),true);break;}
                    if(CanSeePlayer){BeginChase();break;}
                    if(freshMemory){BeginSearch(memory);break;}
                    landTimer-=dt;
                    if(landTimer<=0)GoToPerch(Mathf.Max(0,NearestPerch(transform.position)),false);
                    break;
                }
                case Flight.Chase:
                    CountChase(dt,memory);break;
                case Flight.NearMiss:
                    NearMissTick(dt);break;
                default:   // Away (hunt began while he was unplaced): land where he is and prowl
                    BeginLand();break;
            }
            if(diveOpen&&flight!=Flight.Chase)CloseDive("the dive ended, now "+flight);
            // At least one attack attempt per hunt: until he has dived, every GuaranteeSeconds he hears you again and searches.
            guaranteeClock+=dt;
            if(!retreating&&!swooping&&!attackAttempted&&guaranteeClock>=guaranteeNext&&flight!=Flight.Chase&&flight!=Flight.NearMiss)
            {
                guaranteeClock=0;guaranteeNext=Random.Range(count.AttackGuaranteeMin,count.AttackGuaranteeMax);HearPlayerRoughly();BeginSearch(new Vector3(Memory.X,Memory.Y,Memory.Z));
            }
            aiLogClock-=dt;
            if(aiLogClock<=0){aiLogClock=1;LogAI(retreating);}
        }
        // One line per second during a hunt: what he is doing and why he is (not) attacking.
        void LogAI(bool retreating)
        {
            var p=Game.Player;var eye=transform.position+Vector3.up*(flight==Flight.Perched?count.PerchEyeHeight:count.FlightEyeHeight);
            var delta=p.View.transform.position-eye;float distance=delta.magnitude;
            var look=flight==Flight.Perched?Quaternion.AngleAxis(count.PerchLookDown,transform.right)*transform.forward:transform.forward;
            float angle=Vector3.Angle(look,delta);string blocker=SightBlocker(eye,delta);
            string why=flight==Flight.Chase?(CaptureMode.Requested?"chasing (capture hold)":"chasing")
                :retreating?"retreating after a hit"
                :p.Hidden&&!p.TorchOn?"player hidden, torch off"
                :distance>CountSightRange?"out of sight range "+CountSightRange.ToString("F0")+" m"
                :angle>Profile.SightAngle?"outside the sight cone"
                :blocker!=null?"line of sight blocked by "+blocker
                :flight==Flight.Perched&&tell<count.PerchTellSeconds?"sees you, tell "+tell.ToString("F1")+"s"
                :"sees you";
            Debug.Log("HC_COUNT_AI t="+Game.Case.PhaseTime.ToString("F1")+" state="+flight+" pos="+transform.position+" player="+p.transform.position+
                " los="+(blocker==null)+" seen="+CanSeePlayer+" dist="+distance.ToString("F1")+" angle="+angle.ToString("F0")+" hidden="+p.Hidden+" torch="+p.TorchOn+
                " memory="+(Memory.Knows?new Vector3(Memory.X,Memory.Y,Memory.Z).ToString():"none")+" route="+routeAt+"/"+route.Count+" attacked="+attackAttempted+" why="+why);
        }
        // Null when the line from his eye to the investigator's view is clear (the first 0.5 m is skipped: the rail or ledge
        // he crouches on must not hide the hall below him).
        string SightBlocker(Vector3 eye,Vector3 delta)
        {
            float length=delta.magnitude;if(length<.6f)return null;var dir=delta/length;var start=eye+dir*.5f;
            if(Physics.Raycast(start,dir,out var hit,length-.5f,FlightMask,QueryTriggerInteraction.Ignore)&&!hit.collider.GetComponentInParent<Investigator>())return hit.collider.name;
            return null;
        }
        void CountChase(float dt,Vector3 memory)
        {
            var player=Game.Player;Vector3 goal;var feet0=player.transform.position;
            float herDistance=new Vector2(transform.position.x-feet0.x,transform.position.z-feet0.z).magnitude;
            // fd-grab: a planned side snatch or drop-spin flies to its own start point; it is followed live until he is within
            // PlanLockDistance, then fixed (a player who keeps turning to face him gets the plain grab instead of being circled).
            if(VariantPlanned&&!witnessedHide&&CanSeePlayer)
            {
                if(huntClock-chaseT0>count.VariantGiveUpSeconds+count.VariantCurveSeconds+chaseD0/Mathf.Max(1,count.DiveSpeed))DropPlan("not committed in time");
                else if(!planLocked)
                {
                    // fe2-count: the live point keeps its frame turn (side) / start bearing (spin) up to date.
                    bool side=IsSide(plannedMode);float pv;
                    string liveWhy;bool ok=side?SidePlanPoint(planSide,out var pp,out pv,out liveWhy):SpinPlanPoint(planSide,out pp,out pv,out liveWhy);
                    // fe3-grab: a side that flips here takes the other side's mode too (FE2 kept the old mode number: r3 natural9 logged
                    // plan=side-right mode=11, then dropped=side-left), and the flip is logged.
                    if(!ok)
                    {
                        bool other=side?SidePlanPoint(-planSide,out pp,out pv,out _):SpinPlanPoint(-planSide,out pp,out pv,out _);
                        if(other)
                        {
                            if(side)Debug.Log("HC_GRAB_FLANK side="+(planSide<0?"L":"R")+" stage=live feasible=False reason="+liveWhy.Replace(' ','_')+" next="+(planSide<0?"R":"L"));
                            planSide=-planSide;if(side)plannedMode=planSide<0?LeftSideMode:RightSideMode;
                        }
                        else{DropPlan("no open start point either side");pp=planPoint;}
                        ok=other;
                    }
                    if(ok){if(side)planFrameYaw=pv;else planSpinBearing=pv;}
                    if(ok){planPoint=pp;if(herDistance<count.PlanLockDistance){planLocked=true;Debug.Log("HC_GRAB_VARIANT locked="+PlanName(plannedMode)+" point="+planPoint+" bearing="+BearingFromHerView().ToString("F0"));}}
                }
            }
            // count-l4: open-floor dives fly to the ring at the chosen clip's stand-off (backing out if he is inside it), so the
            // commit happens at the stand-off with nothing to re-seat (huntcam1 committed a drop 0.51 m from her).
            int ringMode=PredictGrab(out _);float ring=IsSide(ringMode)?0:count.StandOffFor(ringMode);
            if(witnessedHide)goal=knownHide;
            else if(CanSeePlayer)
            {
                goal=player.transform.position;
                var away=transform.position-goal;away.y=0;
                if(VariantPlanned)goal=planPoint;
                else if(ring>0&&away.sqrMagnitude>.0001f)
                {
                    // fd-grab: the ring point on open floor with a clear sweep to her (no skeleton, pew or column in the clip's way).
                    ringAge+=dt;
                    if(ringAge>.3f){ringAge=0;float top=ringMode==count.ModeGrabDrop?count.GrabDropClipTop:ringMode==count.ModeGrabDropSpin?count.GrabDropSpinClipTop:0;ringDir=RingDirection(goal,away.normalized,ring,top);}
                    goal+=(ringDir.sqrMagnitude>.5f?ringDir:away.normalized)*ring;
                }
            }
            else if(Memory.Knows)goal=memory;
            else{BeginLand();return;}
            foreach(var spot in Game.World.HidingPlaces)
                if(spot.Kind==InteractKind.Hide&&Vector3.Distance(goal,spot.HidePoint)<.35f){goal=spot.ExitPoint;break;}
            // r10b: a watched hide that is not in HidingPlaces (any other Hide interactable) still leads to its own exit.
            if(witnessedHide&&player.Hidden&&Vector3.Distance(knownHide,player.transform.position)<.35f)goal=player.HideExit;
            // r12 (no pop at a hide kill): a known hide is approached to the stand-off point of the grab it will get (GrabDrop with
            // headroom, else GrabCrawl), so the commit does not move him.
            bool hideKnown=player.Hidden&&((witnessedHide&&Vector3.Distance(knownHide,player.transform.position)<.35f)||
                (Memory.Knows&&Vector3.Distance(memory,player.transform.position)<.35f));
            Vector3 hideStand=Vector3.zero;
            if(hideKnown)
            {
                var outward=player.HideExit-player.transform.position;outward.y=0;
                if(outward.sqrMagnitude>.01f){outward.Normalize();hideStand=player.transform.position+outward*count.StandOffFor(HideGrabMode(player.transform.position,outward));hideStand.y=player.HideExit.y;goal=hideStand;}
                else hideKnown=false;
            }
            var flat=player.transform.position-transform.position;float vertical=Mathf.Abs(flat.y);flat.y=0;float distance=flat.magnitude;
            // r10 #14: seconds until he reaches grab range at his current speed (Game's torch failure keys on the last second).
            // r12 (kill pop): the grab commits at the chosen clip's stand-off, so nothing moves him when she dies.
            int wantMode=PredictGrab(out var wantWhy);
            // Side snatches commit while he is still out at her flank (the clip brings him in from SideStartLateral); the rest
            // commit at their clip's stand-off. fd-grab: a planned side commits at its start point.
            float toPlan=VariantPlanned?new Vector2(transform.position.x-planPoint.x,transform.position.z-planPoint.z).magnitude:0;
            // fe2-count (r13 FE fear: four drawbridge drop-spin dives abandoned 0.2-0.4 m over her head): a planned drop-spin with him already
            // above her (origin SpinConeMinHeight over her floor, within the spin's stand-off + SpinConeSlack) commits from there: the stand-off
            // on his side of her, or on the planned side when he is overhead, if that spot is open for the spin's fall.
            if(plannedMode==count.ModeGrabDropSpin&&plannedMode>=0&&!witnessedHide&&CanSeePlayer&&toPlan>=count.VariantCommitTolerance&&spinConeDir==Vector3.zero)
            {
                var off=transform.position-feet0;float over=off.y;off.y=0;
                if(over>=count.SpinConeMinHeight&&off.magnitude<=count.StandOffFor(count.ModeGrabDropSpin)+count.SpinConeSlack)
                {
                    var dir=off.magnitude>=.8f?off.normalized:new Vector3(planPoint.x-feet0.x,0,planPoint.z-feet0.z).normalized;
                    var vf=player.View.transform.forward;vf.y=0;float b=vf.sqrMagnitude>1e-4f?Mathf.Abs(Vector3.SignedAngle(vf,dir,Vector3.up)):0;
                    var spot=feet0+dir*count.StandOffFor(count.ModeGrabDropSpin);spot.y=feet0.y;
                    string cb="she faces his side";
                    if(b>=count.SpinMinBearing&&SpotOpen(feet0,spot,count.GrabDropSpinClipTop,out cb))
                    {
                        spinConeDir=dir;planPoint=spot;planLocked=true;toPlan=0;
                        Debug.Log("HC_GRAB_VARIANT cone=True plan=drop-spin over="+over.ToString("F2")+" off="+off.magnitude.ToString("F2")+" bearing="+b.ToString("F0")+" dir="+(off.magnitude>=.8f?"his":"planned")+" point="+spot);
                    }
                    else if(!coneLogged){coneLogged=true;Debug.Log("HC_GRAB_VARIANT cone=False over="+over.ToString("F2")+" off="+off.magnitude.ToString("F2")+" bearing="+b.ToString("F0")+" reason="+cb);}
                }
            }
            if(spinConeDir!=Vector3.zero&&plannedMode==count.ModeGrabDropSpin)toPlan=0;
            grabReach=IsSide(wantMode)?(VariantPlanned?new Vector2(planPoint.x-feet0.x,planPoint.z-feet0.z).magnitude:count.SideCommitDistance):count.StandOffFor(wantMode);
            // At the planned point but the variant no longer fits (she turned to face him): the plain grab from here.
            // ff-count (r14: both side plans reached their start point and fell to the crawl, "came in low"): a side snatch is re-framed from
            // where he is (AdaptSideFrame: the frame turned so his bearing in it is the clip's start bearing, at most SideAdaptMaxTurn from her
            // view); if that fails too the plan is unlocked and follows her view again (it can still time out: "not committed in time").
            if(VariantPlanned&&planLocked&&toPlan<count.VariantCommitTolerance&&wantMode!=plannedMode&&IsSide(plannedMode)&&!IsSide(wantMode))
            {
                string was=sideGateWhy.Length>0?sideGateWhy:wantWhy;
                if(AdaptSideFrame(out string adaptWhy))
                {
                    wantMode=PredictGrab(out wantWhy);grabReach=IsSide(wantMode)?new Vector2(planPoint.x-feet0.x,planPoint.z-feet0.z).magnitude:count.StandOffFor(wantMode);toPlan=0;
                    Debug.Log("HC_GRAB_FLANK side="+(planSide<0?"L":"R")+" stage=adapt feasible="+IsSide(wantMode)+" frameYaw="+(planFrameYaw*planSide).ToString("F0")+" was="+was.Replace(' ','_')+" now="+wantWhy.Replace(' ','_'));
                }
                else if(!adaptLogged)
                {
                    adaptLogged=true;planLocked=false;
                    Debug.Log("HC_GRAB_FLANK side="+(planSide<0?"L":"R")+" stage=commit feasible=False reason="+was.Replace(' ','_')+" adapt="+adaptWhy.Replace(' ','_')+" bearing="+BearingFromHerView().ToString("F0")+" toPlan="+toPlan.ToString("F2")+" next=relock");
                }
                else planLocked=false;
            }
            if(VariantPlanned&&planLocked&&toPlan<count.VariantCommitTolerance&&wantMode!=plannedMode&&!(IsSide(plannedMode)&&IsSide(wantMode))&&!IsSide(plannedMode))
            {
                DropPlan("at the start point, "+wantWhy);wantMode=PredictGrab(out wantWhy);grabReach=count.StandOffFor(wantMode);toPlan=0;
            }
            diveEndsIn=Mathf.Max(0,distance-grabReach)/Mathf.Max(speedNow,1f);
            if(!diveLastFired&&diveEndsIn<=count.DiveLastSecond&&CanSeePlayer)
            {
                diveLastFired=true;Debug.Log("HC_COUNT_DIVE_LAST eta="+diveEndsIn.ToString("F2")+" distance="+distance.ToString("F2")+" speed="+speedNow.ToString("F1")+" "+DiagnosticState);
                DiveLastSecond?.Invoke();
            }
            // Item 17: how far above you he has been lately (decaying), so a grab after a drop from the vault reads as GrabDrop.
            dropHeight=Mathf.Max(dropHeight-count.GrabDropHeightDecay*dt,transform.position.y-player.transform.position.y);
            if(!diveCloseLogged&&distance<=grabReach)
            {
                diveCloseLogged=true;
                Debug.Log("HC_COUNT_DIVE close_s="+(huntClock-chaseT0).ToString("F2")+" from="+chaseD0.ToString("F2")+" to="+distance.ToString("F2")+" speed="+speedNow.ToString("F1")+" "+DiagnosticState);
            }
            // Reached a remembered spot without seeing you: land there and prowl (Mode 2), then take a perch.
            var toGoal=goal-transform.position;toGoal.y=0;
            if(!CanSeePlayer&&!witnessedHide&&toGoal.magnitude<1){chaseExit="lost sight, reached her last known spot";searchedAt=goal;BeginLand();return;}
            // Critic capture: dive to the hold distance, hover there in the Dive pose, then feint back to a perch.
            if(CaptureMode.Requested&&distance<=count.CaptureHoldDistance)
            {
                if(flat.sqrMagnitude>.0001f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(flat),count.TurnDegreesPerSecond*dt);
                speedNow=0;actor.Pose(Profile.ModeReach);holdTimer+=dt;
                if(holdTimer>=count.CaptureHoldSeconds){holdTimer=0;tell=0;Memory.Clear();searchedAt=player.transform.position;chaseExit="capture feint";GoToPerch(Mathf.Max(0,NearestPerch(transform.position)),false);Debug.Log("HC_COUNT_FEINT "+DiagnosticState);}
                return;
            }
            replan-=dt;
            if(replan<=0||Vector3.Distance(goal,lastChaseGoal)>1||routeAt>=route.Count)
            {
                replan=Profile.ThinkInterval;lastChaseGoal=goal;
                // A dive with no route at all (three plans in a row, about 1 s) lands and prowls instead of hovering in place.
                if(PlanRoute(goal,false))
                {
                    chaseRouteFails=0;
                    // fe-count (r13 FD huntcam): a variant's start point that needs a NavMesh detour is dropped for the next feasible grab.
                    // fe2-count: a short bend is accepted (RouteAcceptable: the same rule the plan was made with).
                    if(VariantPlanned&&!witnessedHide&&route.Count>2&&spinConeDir==Vector3.zero)
                    {
                        if(!RouteAcceptable(out var rw)){DropPlan("detour: "+rw+" to the start point");replan=0;}
                        else if(!curveLogged){curveLogged=true;Debug.Log("HC_GRAB_VARIANT curved=True plan="+PlanName(plannedMode)+" route="+rw);}
                    }
                }
                else if(++chaseRouteFails>=3){chaseRouteFails=0;Debug.Log("HC_COUNT_ROUTE abort=dive reason=no route to "+goal+" "+DiagnosticState);chaseExit="no route to "+goal;searchedAt=goal;BeginLand();return;}
            }
            // A cupboard can be searched from its clear entrance only (same rule as Split Grin).
            bool knowsThisHide=player.Hidden&&((witnessedHide&&Vector3.Distance(knownHide,player.transform.position)<.35f)||
                (Memory.Knows&&Vector3.Distance(memory,player.transform.position)<.35f));
            bool atHideEntrance=knowsThisHide&&(hideKnown
                    ?Vector3.Distance(new Vector3(transform.position.x,hideStand.y,transform.position.z),hideStand)<count.HideStandTolerance
                    :Vector3.Distance(new Vector3(transform.position.x,player.HideExit.y,transform.position.z),player.HideExit)<Profile.HideEntranceDistance)&&
                !Physics.Linecast(transform.position+Vector3.up,player.HideExit+Vector3.up);
            // fd-grab (r13 huntcam: 4.4 m over her, never committed): a high dive may commit up to the drop clip's own height over her
            // (MatchClipStart eases a higher root down); a planned variant commits at its start point only.
            float verticalLimit=diveHigh?count.HighDiveHover+1.6f:1.6f;
            bool inReach=distance<grabReach+count.RingTolerance;
            bool losClear=!Physics.Linecast(transform.position+Vector3.up,player.transform.position+Vector3.up,FlightMask,QueryTriggerInteraction.Ignore);
            bool clearReach=!CaptureMode.Requested&&inReach&&vertical<verticalLimit&&!player.Hidden&&losClear&&(!VariantPlanned||toPlan<count.VariantCommitTolerance);
            gateWhy=!inReach?"out of reach "+distance.ToString("F2")+" > "+(grabReach+count.RingTolerance).ToString("F2"):vertical>=verticalLimit?"vertical "+vertical.ToString("F2")+" >= "+verticalLimit.ToString("F2"):
                player.Hidden?"she is hidden":!losClear?"no line of sight":VariantPlanned&&toPlan>=count.VariantCommitTolerance?"not at the "+PlanName(plannedMode)+" start point ("+toPlan.ToString("F2")+" m)":"";
            // count-l4: flare upright over the last FlareDistance before the ring (the grab clips start upright: no head jump).
            flare=Mathf.Clamp01((distance-grabReach)/Mathf.Max(.1f,count.FlareDistance));
            // count17: a committed hide grab stays committed after CommitGrab moves him to the stand-off outside the door.
            // count-l4: any committed grab is irrevocable until its clip has played (natural1 re-committed a drop-spin 12 times
            // because placing him exactly at the stand-off read as out of reach on the next frame). Only the sword recoil
            // (CountStagger) or the kill end it.
            bool committed=grabMode>=0&&Time.time<committedUntil;
            // fd-grab (r13 death: GrabCrawl through the giant skeleton): an open-floor commit waits while the clip's path to her is
            // blocked; he keeps flying to the open ring point. Side and spin start points were checked when planned.
            if(clearReach&&!committed&&grabMode<0&&!IsSide(wantMode)&&wantMode!=count.ModeGrabDropSpin&&!GrabPathClear(player.transform.position,transform.position,out var pathBlocker))
            {
                if(pathHoldSince<0)pathHoldSince=Time.time;
                if(!pathHoldLogged){pathHoldLogged=true;Debug.Log("HC_GRAB_PATH held=True mode="+wantMode+" blocker="+pathBlocker+" ring="+ringWhy+" "+DiagnosticState);}
                clearReach=false;gateWhy="path blocked by "+pathBlocker;
            }
            else if(clearReach)pathHoldSince=-1;
            // ff-count (r14: mode 8 started 0.22-0.50 s into its clip, the fall cut; 3 planned drops fell to the crawl with 12 m of headroom):
            // a drop commits only where its fall clears the ceiling (his own bearing's stand-off; he keeps flying to the open ring point), and
            // only with his skull at least the clip's skull at DropStartMaxT over her floor: lower, he climbs first (the rise hold, at most
            // DropRiseMaxSeconds, never into a ceiling). A rise that cannot reach it commits from where it got to, or, under the clip's landed
            // skull, ends the dive as a readable miss (never the crawl).
            if(clearReach&&!committed&&grabMode<0&&wantMode==count.ModeGrabDrop&&wantMode>=0&&!atHideEntrance&&actor&&actor.Head&&!player.Hidden)
            {
                var feetD=player.transform.position;var outD=transform.position-feetD;outD.y=0;
                if(outD.sqrMagnitude<.01f){outD=-player.View.transform.forward;outD.y=0;}
                outD=outD.sqrMagnitude>1e-4f?outD.normalized:Vector3.forward;
                float spotCeil=CeilingOver(feetD+outD*count.StandOffFor(wantMode),feetD.y);
                if(spotCeil<count.GrabDropClipTop+.1f)
                {
                    clearReach=false;gateWhy="drop spot under a "+spotCeil.ToString("F2")+" m ceiling (flying to the open ring point)";
                    if(!dropSpotLogged){dropSpotLogged=true;Debug.Log("HC_GRAB_DROP held=True reason=spot_ceiling ceiling="+spotCeil.ToString("F2")+" needs="+(count.GrabDropClipTop+.1f).ToString("F2")+" ring="+ringWhy+" "+DiagnosticState);}
                }
                else if(clipSkull.TryGetValue(count.ModeGrabDrop,out var dropCurve))
                {
                    float need=GrabMixRules.DropStartSkull(dropCurve,ClipSampleStep,count.DropStartMaxT)+.02f,skullNow=actor.Head.position.y-feetD.y;
                    var rise=GrabMixRules.RiseDecision(skullNow,need,riseHoldT,count.DropRiseMaxSeconds);
                    if(rise==GrabMixRules.Rise.Hold)
                    {
                        float climb=Mathf.Min(count.DropRiseSpeed*Mathf.Max(dt,1e-4f),need-skullNow+.03f);
                        bool headClear=!Physics.SphereCast(actor.Head.position,.25f,Vector3.up,out var rh,climb+.4f,FlightMask,QueryTriggerInteraction.Ignore)||IgnoreHit(rh.collider);
                        if(headClear)
                        {
                            if(riseHoldT<=0)Debug.Log("HC_GRAB_DROP rise=begin skull="+skullNow.ToString("F2")+" need="+need.ToString("F2")+" "+DiagnosticState);
                            riseHoldT+=Mathf.Max(dt,1e-4f);transform.position+=Vector3.up*climb;speedNow=Mathf.Max(speedNow*.9f,1f);
                            gateWhy="rising to drop ("+skullNow.ToString("F2")+" < "+need.ToString("F2")+")";lastDiveSkull=actor.Head.position;
                            if(flat.sqrMagnitude>.0001f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(flat),count.TurnDegreesPerSecond*dt);
                            return;
                        }
                        Debug.Log("HC_GRAB_DROP rise=blocked skull="+skullNow.ToString("F2")+" need="+need.ToString("F2")+" blocker="+(rh.collider?rh.collider.name:"none"));
                        rise=GrabMixRules.Rise.Timeout;
                    }
                    if(riseHoldT>0||rise==GrabMixRules.Rise.Timeout)Debug.Log("HC_GRAB_DROP rise=end held="+riseHoldT.ToString("F2")+" skull="+skullNow.ToString("F2")+" need="+need.ToString("F2")+" result="+(rise==GrabMixRules.Rise.Commit?"commit":"timeout"));
                    riseHoldLast=riseHoldT;riseHoldT=0;
                    if(rise==GrabMixRules.Rise.Timeout&&skullNow<dropCurve[dropCurve.Length-1]+.05f){DiveMiss("drop rise blocked (skull "+skullNow.ToString("F2")+" m, the clip lands at "+dropCurve[dropCurve.Length-1].ToString("F2")+")",distance,vertical);return;}
                }
            }
            // fe-count (r13 FD natural2 Return drop: first frame visible=False lineClear=False): an open-floor commit also waits, up to
            // EyeHoldSeconds, while her eye line to his skull is blocked, so the commit frame shows him (the seat keeps the skull where it is).
            if(clearReach&&!committed&&grabMode<0&&actor&&actor.Head)
            {
                var eyeNow=player.View.transform.position;
                bool eyeClear=!Physics.Linecast(eyeNow,actor.Head.position,out var eb,FlightMask,QueryTriggerInteraction.Ignore)||IgnoreHit(eb.collider);
                if(eyeClear)eyeHoldSince=-1;
                else
                {
                    if(eyeHoldSince<0){eyeHoldSince=Time.time;Debug.Log("HC_GRAB_PATH held=True reason=eye_line blocker="+eb.collider.name+" mode="+wantMode+" "+DiagnosticState);}
                    if(Time.time-eyeHoldSince<count.EyeHoldSeconds){clearReach=false;gateWhy="eye line to his skull blocked by "+eb.collider.name;}
                    else Debug.Log("HC_GRAB_PATH held=False reason=eye_line_timeout blocker="+eb.collider.name+" (the first frame relocates the seat)");
                }
            }
            bool touching=committed||(clearReach&&(CanSeePlayer||contact>0))||atHideEntrance||hideGrabLocked&&player.Hidden;
            if(!committed&&touching&&grabMode<0)
            {
                // count-l4: no re-commit within RecommitBlock of a release, and never with his skull out of the clip's reach.
                string blocked=null;
                if(Time.time<recommitBlockedUntil)blocked="released "+(Time.time-(recommitBlockedUntil-count.RecommitBlock)).ToString("F1")+" s ago";
                else if(actor.Head&&!atHideEntrance)
                {
                    var sk=actor.Head.position-player.View.transform.position;sk.y=0;
                    float limit=(IsSide(wantMode)?grabReach:count.StandOffFor(wantMode))+count.RingTolerance+.6f;
                    if(sk.magnitude>limit)blocked="skull "+sk.magnitude.ToString("F2")+" m out, limit "+limit.ToString("F2");
                }
                if(blocked!=null){touching=false;gateWhy="recommit blocked: "+blocked;if(Time.time>=recommitLogAt){recommitLogAt=Time.time+1;Debug.Log("HC_COUNT_GRAB recommit_blocked=True reason="+blocked);}}
            }
            // fe3-grab (r13 FE2: side and spin seat gaps 0.55-1.10 m slid over 0.35-0.55 s; r2 m10 2.91 m): fe-count's seat rule now covers
            // modes 10/11/12. The clip's first skull (PredictVariantSkull) must be within SeatMaxGap of his skull now; over it the commit is
            // held while he flies onto it (at most VariantSeatHoldSeconds, never through geometry), then the grab commits and GrabCamFrame
            // hard-cuts frame 0 if the measured gap is still over SeatMaxGap (HC_GRAB_CUT). No slide over SeatMaxGap either way.
            if(touching&&!committed&&grabMode<0&&!atHideEntrance&&wantMode>=0&&(IsSide(wantMode)||wantMode==count.ModeGrabDropSpin)&&actor.Head&&PredictVariantSkull(wantMode,out var seatSkull))
            {
                float gap=Vector3.Distance(actor.Head.position,seatSkull);
                var seat=SideGrabRules.SeatDecision(gap,seatHoldT,count.SeatMaxGap,count.VariantSeatHoldSeconds);
                if(seat==SideGrabRules.Seat.Hold)
                {
                    var move=seatSkull-actor.Head.position;var target=transform.position+move;
                    if(Clear(transform.position,target))
                    {
                        if(seatHoldT<=0)Debug.Log("HC_GRAB_SEAT_HOLD begin mode="+wantMode+" gap="+gap.ToString("F2")+" offset="+move.ToString("F2")+" "+DiagnosticState);
                        seatHoldT+=Mathf.Max(dt,1e-4f);transform.position+=Vector3.ClampMagnitude(move,Mathf.Max(count.DiveSpeed,count.VariantSeatHoldSpeed)*dt);
                        gateWhy="seat gap "+gap.ToString("F2")+" > "+count.SeatMaxGap.ToString("F2");lastDiveSkull=actor.Head.position;
                        return;
                    }
                    Debug.Log("HC_GRAB_SEAT_HOLD blocked mode="+wantMode+" gap="+gap.ToString("F2")+" (no clear flight onto the clip start; frame 0 will cut)");
                    seat=SideGrabRules.Seat.Cut;
                }
                if(seatHoldT>0||seat==SideGrabRules.Seat.Cut)Debug.Log("HC_GRAB_SEAT_HOLD end mode="+wantMode+" gap="+gap.ToString("F2")+" held="+seatHoldT.ToString("F2")+" result="+(seat==SideGrabRules.Seat.Commit?"commit":"cut"));
                seatHoldLast=seatHoldT;seatPredictGap=gap;seatHoldT=0;
            }
            if(!touching&&actor.Head)lastDiveSkull=actor.Head.position;
            if(!touching&&grabMode>=0&&committedUntil>0&&Time.time>=committedUntil)
            {Debug.Log("HC_COUNT_GRAB released=clip_end mode="+grabMode);contact=0;grabMode=-1;plannedMode=-1;spinConeDir=Vector3.zero;committedUntil=-1;recommitBlockedUntil=Time.time+count.RecommitBlock;if(grabCam&&!capturePresentation)EndGrabCam("clip ended");}
            // Integrator (r10 rerun, hostile return): a walking player flickered in and out of GrabDistance every frame, so contact
            // reset and he chose a grab 20 times on the bridge without one kill. Contact now decays instead of resetting.
            contact=touching?contact+dt:Mathf.Max(0,contact-dt*.5f);
            if(touching)
            {
                nearNoGrab=0;
                // fd-grab: aimed once, at the commit; never re-aimed while the clip plays (the seat offset would swing his skull).
                if(!committed&&grabMode<0&&flat.sqrMagnitude>.0001f)transform.rotation=Quaternion.LookRotation(flat);
                if(grabMode<0||contact<=dt)
                {
                    string why;float height=transform.position.y-player.transform.position.y;
                    if(atHideEntrance)grabMode=ChooseGrab(true,out why,out height);else{grabMode=wantMode;why=wantWhy;}
                    Debug.Log("HC_COUNT_GRAB mode="+grabMode+" height="+height.ToString("F2")+" reason="+why+" dropHeight="+dropHeight.ToString("F2")+" hideEntrance="+atHideEntrance+" witnessed="+witnessedHide+" "+DiagnosticState);
                    if(atHideEntrance)OpenHideDoor(player.HideExit);
                    grabMode=CommitGrab(atHideEntrance,grabMode);
                    if(diveOpen&&!diveResolved){diveResolved=true;Debug.Log("HC_COUNT_DIVE end=commit mode="+grabMode+" planned="+PlanName(plannedMode)+" t="+(huntClock-chaseT0).ToString("F1"));}
                    committedUntil=Time.time+(count.ClipSecondsFor(grabMode)-clipStartT)/Mathf.Max(.5f,grabClipSpeed)+.25f;   // ff-count: the crawl's speed
                    // count18: a grab at a hide entrance is a hide open even when the dive delivered it (path label for the proof).
                    BeginGrabCam(atHideEntrance?"hideopen":"dive",atHideEntrance);
                }
                actor.Pose(grabMode);
                // huntClock, not PhaseTime: a hostile-return hunt (F1) runs inside Phase Return.
                if(contact>=Profile.GrabSeconds&&huntClock>Profile.GrabGraceSeconds)Game.Case.Die();
                if(Game.Case.Terminal)return;
                // The clip has played and she is still alive (the driver's invulnerability): let go, and no new grab for a while.
                if(grabMode>=0&&Time.time>=committedUntil&&!clearReach&&!atHideEntrance){Debug.Log("HC_COUNT_GRAB released=clip_end mode="+grabMode);contact=0;grabMode=-1;plannedMode=-1;spinConeDir=Vector3.zero;committedUntil=-1;recommitBlockedUntil=Time.time+count.RecommitBlock;if(grabCam)EndGrabCam("clip ended");}
                return;
            }
            if(grabMode>=0&&!committed&&contact<=0){grabMode=-1;committedUntil=-1;recommitBlockedUntil=Time.time+count.RecommitBlock;if(grabCam&&!capturePresentation)EndGrabCam("released");}
            // fe-count (r13 FD huntcam: planned drop-spin, he flew 12 m away along the gallery, no commit and no miss): a dive that has turned
            // into a flight away from her (out of her sight, or chasing a variant's start point) ends as a readable miss.
            if(!witnessedHide&&grabMode<0&&huntClock-chaseT0>1f&&distance>chaseD0+count.DiveAwayMargin+(VariantPlanned?count.VariantAwayExtra:0)&&(!CanSeePlayer||VariantPlanned))
            {DiveMiss("moving away ("+distance.ToString("F1")+" m from her, dive began at "+chaseD0.ToString("F1")+" m)",distance,vertical);return;}
            // fd-grab: close to her (or at his goal next to her) without a commit for DiveStallSeconds: a miss, never a hover.
            if(!player.Hidden&&!witnessedHide&&grabMode<0&&toGoal.magnitude<.75f&&distance<grabReach+3f)
            {
                nearNoGrab+=dt;
                if(nearNoGrab>=count.DiveStallSeconds){DiveMiss(gateWhy.Length>0?gateWhy:"no commit",distance,vertical);return;}
            }
            else nearNoGrab=0;
            // r10b: never hover at a watched hide forever (an entrance he cannot reach or a hide with no clear front): after 3 s at
            // the goal without a grab he gives up on it, logged, and prowls.
            var atGoal=goal-transform.position;atGoal.y=0;
            if(witnessedHide&&atGoal.magnitude<1)
            {
                hideStuck+=dt;
                if(hideStuck>3){Debug.Log("HC_COUNT_WITNESSED abort=stuck reason=no entrance grab within 3 s at "+goal+" "+DiagnosticState);hideStuck=0;witnessedHide=false;Memory.Clear();searchedAt=goal;BeginLand();return;}
            }
            else hideStuck=0;
            Fly(dt,Profile.ModeReach);
        }
        float hideStuck,committedUntil=-1,recommitBlockedUntil=-1,recommitLogAt,flare=1;Vector3? lastDiveSkull;
        float riseHoldT,riseHoldLast;bool dropSpotLogged,adaptLogged;   // ff-count: the drop's rise hold and spot gate; the side re-frame log
        void Fly(float dt,int mode)
        {
            if(routeAt<route.Count)
            {
                var target=route[routeAt];var to=target-transform.position;float dist=to.magnitude;
                speedNow=Mathf.MoveTowards(speedNow,CruiseSpeed(to),(flight==Flight.Chase?count.DiveAcceleration:Profile.Acceleration)*dt);
                float step=speedNow*dt;
                var next=step>=dist?target:transform.position+to/dist*step;
                // r10 blind (wallCrossings=2, both closing service doors): the step he is about to take must not pass through
                // anything. A door he waits for (opened, up to 2 s), anything else is re-routed, else he stops there.
                if(LegBlocked(transform.position,next,out var hit)){OnLegBlocked(hit,dt);}
                else
                {
                    blockedFor=0;
                    if(step>=dist){transform.position=target;routeAt++;}
                    else transform.position=next;
                }
                var flat=new Vector3(to.x,0,to.z);
                if(flat.sqrMagnitude>.0004f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(flat),count.TurnDegreesPerSecond*dt);
                foreach(var door in Game.World.Doors)if(Vector3.Distance(door.transform.position,transform.position)<Profile.DoorOpenDistance)door.Open=true;
            }
            // Under a low lintel or at a service door he drops to the stooped crawl (Mode 7) on the floor.
            if(routeAt<route.Count)CheckStall(dt);
            if(OnFoot||MustCrawl(transform.position)){actor.Pose(count.ModeCrawl,speedNow*.85f);return;}
            // Wing-beat rate follows speed but stays slow enough to read as a giant (LocomotionRate = speed / 2.05).
            actor.Pose(mode,1.2f+speedNow*.35f);
        }
        float blockedFor;int blockedLogs;
        // The same segment test as the playtest driver's wall-crossing count (0.35 m above his origin), excluding himself and her.
        bool LegBlocked(Vector3 from,Vector3 to,out RaycastHit hit)
        {
            hit=default;if((to-from).sqrMagnitude<1e-6f)return false;
            if(!Physics.Linecast(from+Vector3.up*.35f,to+Vector3.up*.35f,out hit,FlightMask,QueryTriggerInteraction.Ignore))return false;
            var t=hit.collider.transform;
            return !t.IsChildOf(transform)&&!hit.collider.GetComponentInParent<Investigator>();
        }
        // r11 (hostile Return hung mid-air on wall tops ~3 s): a door is opened and waited for up to DoorWaitSeconds; anything else,
        // or a longer door wait, re-routes at once; if the new route is blocked too he lands and crawls the NavMesh (GoOnFoot).
        void OnLegBlocked(RaycastHit hit,float dt)
        {
            blockedFor+=dt;speedNow*=.5f;
            var door=hit.collider.GetComponentInParent<Door>();
            string fix;
            if(door&&blockedFor<count.DoorWaitSeconds){door.Open=true;fix="wait";}
            else if(PlanRoute(routeGoal,routeAir)&&!NextStepBlocked())fix="replan";
            else if(GoOnFoot())fix="crawl";
            else{fix="land";BeginLand();}
            if(fix!="wait")blockedFor=0;
            if(blockedLogs<60&&(fix!="wait"||blockedFor<=dt*1.01f)){blockedLogs++;Debug.Log("HC_COUNT_LEG blocked="+(door?"door":"wall")+" fix="+fix+" hit="+hit.collider.name+" at="+hit.point+" state="+flight);}
        }
        bool NextStepBlocked()=>routeAt<route.Count&&LegBlocked(transform.position,Vector3.MoveTowards(transform.position,route[routeAt],.3f),out _);
        // Land and crawl: drop to the ground under him and follow the NavMesh to the same goal at hover 0 (Mode 7) for OnFootSeconds.
        float onFootUntil=-1;
        bool OnFoot=>Time.time<onFootUntil;
        bool GoOnFoot()
        {
            if(!GroundUnder(transform.position,out var ground)||LegBlocked(transform.position,ground,out _))return false;
            onFootUntil=Time.time+count.OnFootSeconds;
            var goal=routeGoal;bool air=routeAir;
            if(!PlanRoute(goal,false)){onFootUntil=-1;routeAir=air;return false;}
            route.Insert(0,ground);routeAt=0;
            return true;
        }
        // Zero progress on an airborne leg for StallSeconds counts as blocked (no hovering against a wall).
        float stallClock;Vector3 stallFrom;
        void CheckStall(float dt)
        {
            bool airborne=!OnFoot&&GroundUnder(transform.position,out var g)&&transform.position.y-g.y>.5f;
            if(!airborne||routeAt>=route.Count){stallClock=0;stallFrom=transform.position;return;}
            stallClock+=dt;
            if(stallClock<count.StallSeconds)return;
            float moved=Vector3.Distance(transform.position,stallFrom);stallClock=0;stallFrom=transform.position;
            if(moved>=count.StallMinProgress)return;
            string fix=PlanRoute(routeGoal,routeAir)&&!NextStepBlocked()?"replan":GoOnFoot()?"crawl":"land";
            if(fix=="land")BeginLand();
            Debug.Log("HC_COUNT_LEG stall="+count.StallSeconds.ToString("F1")+" moved="+moved.ToString("F2")+" fix="+fix+" state="+flight+" pos="+transform.position);
        }
        bool MustCrawl(Vector3 p)
        {
            if(NearServiceDoor(p))return true;
            // Clear height over the floor under him (not over his flight position), so 3 m rooms stay flyable.
            var floor=Physics.Raycast(p+Vector3.up*.3f,Vector3.down,out var down,6,FlightMask,QueryTriggerInteraction.Ignore)?down.point:p;
            return Physics.Raycast(floor+Vector3.up*.2f,Vector3.up,out var up,count.CrawlCeiling,FlightMask,QueryTriggerInteraction.Ignore)&&up.distance+.2f<count.CrawlCeiling;
        }
        float CruiseSpeed(Vector3 to)
        {
            var p=transform.position;
            if(OnFoot||NearServiceDoor(p)||routeAt<route.Count&&NearServiceDoor(route[routeAt])||MustCrawl(p))return count.LandedWalkSpeed*SpeedScale;
            var forward=new Vector3(to.x,0,to.z);var side=forward.sqrMagnitude>.0001f?Vector3.Cross(Vector3.up,forward.normalized):transform.right;
            var chest=p+Vector3.up*1.1f;
            float width=Reach(chest,side,6)+Reach(chest,-side,6);
            bool straight=to.magnitude>=count.StraightLookahead||routeAt>=route.Count-1;
            // Wave I: a dive closes at DiveSpeed (>= the 4.6 m/s design dive) wherever he can fly, tight rooms included.
            if(flight==Flight.Chase)return count.DiveSpeed*SpeedScale;
            // Item 4: in a hostile return he flies at straight-glide speed everywhere he can fly.
            if(HostileReturn)return count.GlideStraightSpeed*SpeedScale;
            return count.GlideSpeedFor(width,straight)*SpeedScale;
        }
        static float Reach(Vector3 from,Vector3 direction,float max)=>Physics.Raycast(from,direction,out var hit,max,FlightMask,QueryTriggerInteraction.Ignore)?hit.distance:max;
        bool NearServiceDoor(Vector3 p)
        {
            foreach(var door in Game.World.Doors)
            {
                if(!CastleLayout.IsServiceDoor(door))continue;
                var q=door.transform.position;
                if(Mathf.Abs(q.y-p.y)<1.8f&&new Vector2(q.x-p.x,q.z-p.z).magnitude<count.ServiceDoorLandRadius)return true;
            }
            return false;
        }
        // Route to goal (feet position). goalAir: the goal is itself a flight point (a perch); otherwise it is on the
        // ground and gets a hover height (low on the last leg of a chase). False when neither a straight flight nor a
        // NavMesh path exists.
        bool routeAir;
        bool PlanRoute(Vector3 goal,bool goalAir)
        {
            route.Clear();routeAt=0;routeGoal=goal;routeAir=goalAir;var start=transform.position;
            Vector3 end=goal;
            if(!goalAir)
            {
                if(GroundUnder(goal,out var g))goal=g;
                // fd-grab (r13 huntcam): next to her, a dive's ground is her floor, never the top of furniture or the skeleton's boxes.
                if(flight==Flight.Chase&&Game.Player)
                {
                    var her=Game.Player.transform.position;
                    if(goal.y>her.y+.3f&&goal.y-her.y<2.4f&&new Vector2(goal.x-her.x,goal.z-her.z).magnitude<5f){if(!groundLogged){groundLogged=true;Debug.Log("HC_COUNT_ROUTE ground=her_floor was="+goal.y.ToString("F2")+" floor="+her.y.ToString("F2"));}goal.y=her.y;}
                }
                end=goal+Vector3.up*HoverAt(goal,flight==Flight.Chase);
            }
            if(Clear(start,end)){route.Add(end);return true;}
            // Round 2: from a rail perch the downward sweep clipped the coping under his own feet, so he walked the
            // NavMesh route back across the balcony deck, out of sight. Lift forward off the ledge first.
            var launch=start+transform.forward*.6f+Vector3.up*.5f;
            if(Clear(start,launch)&&Clear(launch,end)){route.Add(launch);route.Add(end);return true;}
            if(path==null)path=new NavMeshPath();
            if(!GroundUnder(start,out var g0)||!GroundUnder(goal,out var g1))return false;
            bool found=NavMesh.CalculatePath(g0,g1,NavMesh.AllAreas,path)&&path.status!=NavMeshPathStatus.PathInvalid&&path.corners.Length>0;
            // Item 4: a goal off the NavMesh (the lowered drawbridge is not baked into it) is reached via the nearest mesh point;
            // the final leg to `end` is appended below.
            if(!found&&NavMesh.SamplePosition(g1,out var nearGoal,6,NavMesh.AllAreas))
                found=NavMesh.CalculatePath(g0,nearGoal.position,NavMesh.AllAreas,path)&&path.status!=NavMeshPathStatus.PathInvalid&&path.corners.Length>0;
            if(!found)return false;
            var corners=path.corners;
            for(int i=0;i<corners.Length;i++)route.Add(corners[i]+Vector3.up*HoverAt(corners[i],false));
            route.Add(end);
            // String-pull: skip leading waypoints he can already fly past in a straight line.
            for(int guard=0;guard<4&&route.Count>1&&Clear(start,route[1]);guard++)route.RemoveAt(0);
            return true;
        }
        float HoverAt(Vector3 ground,bool chasing)
        {
            if(NearServiceDoor(ground)||OnFoot)return 0;
            float ceiling=Physics.Raycast(ground+Vector3.up*.2f,Vector3.up,out var hit,8,FlightMask,QueryTriggerInteraction.Ignore)?hit.distance+.2f:8;
            float max=count.HoverMax*Mathf.Max(.3f,1-Tier*count.HoverTierScale);
            float hover=Mathf.Clamp(ceiling-count.BodyClearance,0,max);
            // count-l4: a high dive hovers so his skull ends at the drop clip's peak skull height over her floor (the clip starts there).
            if(chasing&&diveHigh)
            {
                float peak=clipSkull.TryGetValue(count.ModeGrabDrop,out var curve)?Mathf.Max(curve):count.GrabDropPeakSkullDefault;
                float skull=actor&&actor.Head?actor.Head.position.y-transform.position.y:2.2f;
                hover=Mathf.Clamp(peak-skull,0,Mathf.Min(ceiling-count.BodyClearance,count.HighDiveHover));
            }
            else if(chasing)hover=Mathf.Min(hover,count.HoverNearPlayer);
            return hover;
        }
        bool Clear(Vector3 a,Vector3 b)
        {
            for(int k=0;k<2;k++)
            {
                float h=k==0?.45f:1.15f;var from=a+Vector3.up*h;var delta=b+Vector3.up*h-from;float length=delta.magnitude;
                if(length<.05f)continue;
                if(Physics.SphereCast(from,count.FlightClearRadius,delta/length,out var hit,Mathf.Max(0,length-.4f),FlightMask,QueryTriggerInteraction.Ignore)&&
                   !hit.collider.GetComponentInParent<Investigator>())return false;
            }
            return true;
        }
        bool GroundUnder(Vector3 p,out Vector3 ground)
        {
            var probe=p;
            if(Physics.Raycast(p+Vector3.up*.3f,Vector3.down,out var hit,12,FlightMask,QueryTriggerInteraction.Ignore))
            {
                probe=hit.point;
                // Item 4: the drawbridge deck is real ground for him (it is outside IsInside and not on the NavMesh).
                var bridge=Game.World.Castle!=null?Game.World.Castle.Drawbridge:null;
                if(bridge&&hit.collider.transform.IsChildOf(bridge.transform)){ground=probe;return true;}
            }
            if(NavMesh.SamplePosition(probe,out var nav,2.2f,NavMesh.AllAreas)||NavMesh.SamplePosition(p,out nav,6,NavMesh.AllAreas)){ground=nav.position;return true;}
            ground=probe;return false;
        }
        // Sight 14 m / 80 degrees with line of sight from his eye; perched, he looks down into the room.
        bool CountSees()
        {
            var p=Game.Player;
            if(p.Hidden&&!p.TorchOn)return false;
            bool perched=flight==Flight.Perched;
            Vector3 eye=transform.position+Vector3.up*(perched?count.PerchEyeHeight:count.FlightEyeHeight),delta=p.View.transform.position-eye;
            var look=perched?Quaternion.AngleAxis(count.PerchLookDown,transform.right)*transform.forward:transform.forward;
            // fe2-count (r13 FE fear: four drawbridge drop-spin dives "lost sight, reached her last known spot" 0.2-0.4 m over her head,
            // "outside the sight cone"): a diving Count within DiveSenseRadius of her eye senses her whatever way he faces.
            bool close=flight==Flight.Chase&&delta.magnitude<=count.DiveSenseRadius;
            if(delta.magnitude>CountSightRange||!close&&Vector3.Angle(look,delta)>Profile.SightAngle)return false;
            return SightBlocker(eye,delta)==null;
        }
        // r10 #35: the lantern is a beacon in the open (torch on, not hidden): SightRange x TorchSightScale.
        float CountSightRange=>Profile.SightRange*(Game.Player.TorchOn&&!Game.Player.Hidden?count.TorchSightScale:1);
        // Capture only: the camera (the investigator's view) must never be inside his posed silhouette.
        void KeepCameraClear()
        {
            var bounds=actor.PoseBounds(count.CameraClearance);var view=Game.Player.View.transform.position;
            if(!bounds.Contains(view))return;
            var away=view-bounds.center;away.y=0;if(away.sqrMagnitude<.0001f)away=-transform.forward;away.Normalize();
            var moved=view;for(int i=0;i<60&&bounds.Contains(moved);i++)moved+=away*.1f;
            Game.Player.transform.position+=moved-view;Physics.SyncTransforms();
            Debug.Log("HC_CAPTURE_CAMERA_PUSHED by "+(moved-view).magnitude.ToString("F2")+" "+DiagnosticState);
        }
        // ---- Scripted hunt choreography for the critic capture (round 3) -------------------------------------
        // Round 2's live hunt left the frame: the straight swoop's sphere cast clipped the rail he crouched on, so he
        // took the NavMesh route back across the balcony deck and out of view, and the capture camera was fixed.
        // This timeline is a pure function of CaptureClock (seconds, advanced by at most 1/30 s per frame so a slow
        // screenshot frame cannot skip a phase). Phases (Animator Mode): Perch 0 [0,3.5) on the perch, Glide 1
        // [3.5,6.5) off the rail and down toward the camera, Dive 3 [6.5,9.5) to the hold distance and holding,
        // Feint 1 [9.5,12.5) back to the perch; then it repeats. The investigator's view tracks his posed bounds at
        // the investigator's own eye height. Each phase change logs HC_COUNT_CAPTURE.
        public const float CapturePerchEnd=3.5f,CaptureGlideEnd=6.5f,CaptureDiveEnd=10.6f,CaptureFeintEnd=13.6f,CaptureDiveArrive=1.0f,CaptureLungeStart=9.2f,CaptureLungeArrive=9.8f;
        static readonly string[] CapturePhases={"Perch","Glide","Dive","Grab","Feint"};
        bool choreo;
        float choreoClock;
        int choreoPhase=-1;
        Vector3 cPerch,cFacing,cGlideEnd,cHold,cLunge,cCamForward,cLook;
        float cLean;int cGrabMode=6;
        public float CaptureClock=>choreoClock;
        public bool CaptureChoreographyActive=>choreo;
        public string CaptureNote {get;private set;}="";
        /// <summary>Start the deterministic capture choreography from perch <paramref name="perchIndex"/> (1 = east balcony).
        /// Call after placing the investigator (CaptureMode.Place) and after any warning mist has cleared; then shoot at
        /// CaptureClock = 1, 2, ... 10. Holds the Count out of the hunt AI until EndCaptureChoreography or Dismiss.</summary>
        public bool BeginCaptureChoreography(int perchIndex=1,float holdDistance=-1)
        {
            if(count==null||Game.World.PerchPoints.Count==0)return false;
            int i=Mathf.Clamp(perchIndex,0,Game.World.PerchPoints.Count-1);
            Ledge(i,out cPerch,out var yaw);cFacing=Quaternion.Euler(0,yaw,0)*Vector3.forward;
            var player=Game.Player;var ground=player.transform.position;
            cCamForward=player.View.transform.forward;cCamForward.y=0;
            if(cCamForward.sqrMagnitude<.01f)cCamForward=player.transform.forward;cCamForward.Normalize();
            float hold=holdDistance>0?holdDistance:count.CaptureHoldDistance;
            // Wave I (r10 blind: "a monster floats nearer, then poses"): the glide ends CaptureGlideEndDistance (7 m) out, the dive
            // closes in CaptureDiveArrive (1 s, >= 4.6 m/s) and the climax is GrabDrop (else GrabCrawl, else the old Mode 6 hold)
            // planted on her floor at its stand-off, so the clip's claws end CameraClearance short of the lens.
            silhouette.gameObject.SetActive(true);
            cGrabMode=count.ModeGrabDrop>=0&&HasModeState(count.ModeGrabDrop,"GrabDrop")?count.ModeGrabDrop:count.ModeGrabCrawl>=0&&HasModeState(count.ModeGrabCrawl,"GrabCrawl")?count.ModeGrabCrawl:count.ModeGrab;
            cGlideEnd=ground+cCamForward*count.CaptureGlideEndDistance+Vector3.up*count.CaptureGlideEndHeight;
            if(cGrabMode==count.ModeGrab)
            {
                // Round 4: hold high enough that his lowest point clears the 0.08-1.1 m ground-fog planes (the r3 "bust cut").
                cHold=ground+cCamForward*hold+Vector3.up*count.CaptureHoldHeight;
                cLunge=ground+cCamForward*count.CaptureLungeDistance+Vector3.up*count.CaptureHoldHeight;
            }
            else{cHold=ground+cCamForward*count.StandOffFor(cGrabMode);cLunge=cHold;}
            Debug.Log("HC_COUNT_CAPTURE grab mode="+cGrabMode+" standOff="+Vector3.Distance(new Vector3(cHold.x,ground.y,cHold.z),ground).ToString("F2")+" glideEndDistance="+count.CaptureGlideEndDistance.ToString("F1"));
            presentationRemaining=0;capturePresentation=false;fakeRush=false;staged=false;agent.enabled=false;route.Clear();contact=0;
            CanSeePlayer=false;Memory.Clear();perchIndex=i;flight=Flight.Perched;
            transform.SetPositionAndRotation(cPerch,Quaternion.Euler(0,yaw,0));silhouette.gameObject.SetActive(true);actor.Pose(Profile.ModeIdle);
            cLook=cPerch+Vector3.up;cLean=0;silhouette.localRotation=Quaternion.identity;choreo=true;choreoClock=0;choreoPhase=-1;
            Debug.Log("HC_COUNT_CAPTURE begin perch="+i+" feet="+cPerch+" glideEnd="+cGlideEnd+" hold="+cHold+" holdDistance="+hold.ToString("F2")+" eye="+player.View.transform.position);
            return true;
        }
        public void EndCaptureChoreography(){if(!choreo)return;choreo=false;silhouette.localRotation=Quaternion.identity;silhouette.localPosition=Vector3.zero;Debug.Log("HC_COUNT_CAPTURE end clock="+choreoClock.ToString("F2"));}
        static Vector3 Bezier(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float t){float u=1-t;return u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;}
        Vector3 ChoreoPosition(float t,out int phase,out int mode)
        {
            t%=CaptureFeintEnd;
            if(t<CapturePerchEnd){phase=0;mode=Profile.ModeIdle;return cPerch;}
            if(t<CaptureGlideEnd)
            {
                phase=1;mode=Profile.ModeMove;float u=Mathf.SmoothStep(0,1,(t-CapturePerchEnd)/(CaptureGlideEnd-CapturePerchEnd));
                // Out over the hall and straight on toward the camera (r4 H05 flew away, soles to camera).
                return Bezier(cPerch,cPerch+cFacing*2.2f+Vector3.up*.9f,Vector3.Lerp(cPerch,cGlideEnd,.65f)+Vector3.up*1.2f,cGlideEnd,u);
            }
            if(t<CaptureDiveEnd)
            {
                mode=Profile.ModeReach;float k=(t-CaptureGlideEnd)/CaptureDiveArrive;
                // Wave I: near-constant closing speed (was k*k from a standing start); slight ease-in over the first 15 %.
                if(k<1){phase=2;float e=k<.15f?k*k/.3f:k-.075f;return Vector3.Lerp(cGlideEnd,cHold,e/.925f);}
                phase=3;mode=cGrabMode;
                if(cGrabMode!=count.ModeGrab)return cHold;   // GrabDrop / GrabCrawl: the clip itself carries the fall or the lunge
                // Mode 6 fallback: front-facing wing-wrap at the hold, then a short lunge so the claws and wing tips frame the lens.
                float l=Mathf.SmoothStep(0,1,(t-CaptureLungeStart)/(CaptureLungeArrive-CaptureLungeStart));
                return Vector3.Lerp(cHold,cLunge,l)+Vector3.up*.05f*Mathf.Sin((t-CaptureGlideEnd-CaptureDiveArrive)*4);
            }
            phase=4;mode=Profile.ModeMove;float f=Mathf.SmoothStep(0,1,(t-CaptureDiveEnd)/(CaptureFeintEnd-CaptureDiveEnd));
            return Bezier(cLunge,cLunge+Vector3.up*1.4f+cCamForward*1.2f,cPerch+cFacing*2.2f+Vector3.up*1.2f,cPerch,f);
        }
        void CaptureChoreography(float dt)
        {
            choreoClock+=dt;
            var position=ChoreoPosition(choreoClock,out int phase,out int mode);
            var ahead=ChoreoPosition(choreoClock+.05f,out _,out _);
            var eye=Game.Player.View.transform.position;
            var toCamera=eye-position;toCamera.y=0;
            // The feint backs away still facing the camera, then turns (no snap to his back while he is close).
            bool facing=phase==0||phase==3||phase==4&&(choreoClock%CaptureFeintEnd)-CaptureDiveEnd<count.CaptureFeintFacingSeconds;
            var heading=facing?toCamera:ahead-position;heading.y=0;
            // Glide and dive keep his chest and face turned to the camera (blend of travel and camera direction).
            if(phase==1||phase==2){var travel=heading.normalized;heading=Vector3.Slerp(travel,toCamera.normalized,.8f);}
            float speed=(ahead-position).magnitude/.05f;
            transform.position=position;
            if(heading.sqrMagnitude>.0001f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(heading),(phase==0?120:phase==4?150:540)*dt);
            actor.Pose(mode,1.2f+speed*.35f);
            // Wave I (r10 blind: "hangs upright, arms down"): nose-down into the travel direction in Glide and Dive (he comes
            // down at the camera, so it sees his face and chest), upright at the perch and in the grab clip.
            bool backing=phase==4&&(choreoClock%CaptureFeintEnd)-CaptureDiveEnd<count.CaptureFeintFacingSeconds;
            float lean=phase==1?count.CaptureGlidePitch:phase==2?count.CaptureDivePitch:phase==4&&!backing?count.FlightPitchMin:0;
            cLean=Mathf.Lerp(cLean,lean,1-Mathf.Exp(-(phase==3?10:6)*dt));SetFlightPose(cLean);
            if(phase!=choreoPhase)
            {
                if(phase==3)Debug.Log("HC_COUNT_DIVE close_s="+CaptureDiveArrive.ToString("F2")+" from="+Vector3.Distance(new Vector3(cGlideEnd.x,eye.y,cGlideEnd.z),eye).ToString("F2")+" to="+Vector3.Distance(new Vector3(position.x,eye.y,position.z),eye).ToString("F2")+" capture=True grabMode="+mode);
                if(phase==1||phase==2)Debug.Log("HC_COUNT_POSE pitch="+lean.ToString("F0")+" capture=True phase="+CapturePhases[phase]);
                choreoPhase=phase;
                Debug.Log("HC_COUNT_CAPTURE phase="+CapturePhases[phase]+" clock="+choreoClock.ToString("F2")+" mode="+mode+" pos="+position+" distance="+Vector3.Distance(position+Vector3.up,eye).ToString("F2"));
            }
            // The investigator's view follows him from the investigator's own eye height (the body stays put).
            // Close in, aim between the bounds centre and his head so the face stays in frame.
            // Round 6: aim at the bounds centre at the climax too (r5 H10 cut his feet); the lunge stops at 2.4 m so he fits.
            var bounds=actor.PoseBounds(0);var aim=bounds.center;
            cLook=Vector3.Lerp(cLook,aim,1-Mathf.Exp(-10*dt));
            var player=Game.Player;var look=cLook-eye;var flat=new Vector2(look.x,look.z).magnitude;
            if(look.sqrMagnitude>.0001f)
            {
                player.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(look.x,look.z)*Mathf.Rad2Deg,0);
                player.View.transform.localRotation=Quaternion.Euler(-Mathf.Atan2(look.y,flat)*Mathf.Rad2Deg,0,0);
            }
            CaptureNote="phase="+CapturePhases[phase]+" mode="+mode+" clock="+choreoClock.ToString("F2")+" distance="+Vector3.Distance(bounds.center,eye).ToString("F2")+" span="+bounds.size.x.ToString("F2")+" height="+bounds.size.y.ToString("F2");
            KeepCameraClear();
        }
        // CastleArt.Gore keeps the projected albedo and adds wet blood and translucency on the wing membrane (submesh 1) and the
        // flayed flesh (submesh 2); the body/coat (submesh 0) keeps the imported material. Any failure keeps the imported material.
        void CountGoreMaterials()
        {
            if(!count.GoreMaterials)return;
            try
            {
                foreach(var skin in silhouette.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var mats=skin.sharedMaterials;if(mats==null||mats.Length==0)continue;
                    var gore=new Material[mats.Length];
                    // Round 8 (critic r7: 80% red-dominant px): no Skin gore on the body; submesh 1 wing membrane, 2 flayed flesh.
                    // Round 9: by material name (CountClaudeImport matches submeshes to parts by triangle count), never by index.
                    for(int i=0;i<mats.Length;i++)
                    {
                        string n=mats[i]?mats[i].name.ToLowerInvariant():"";
                        gore[i]=n.Contains("membrane")?CastleArt.Gore(CastleArt.GoreKind.Membrane,mats[i],count.GoreMembraneWet)??mats[i]
                               :n.Contains("flesh")?CastleArt.Gore(CastleArt.GoreKind.Flesh,mats[i],count.GoreFleshWet)??mats[i]
                               :count.GoreSkinOnBody?CastleArt.Gore(CastleArt.GoreKind.Skin,mats[i],count.GoreSkinWet)??mats[i]:mats[i];
                    }
                    skin.sharedMaterials=gore;
                }
                Debug.Log("HC_COUNT_GORE applied "+string.Join(",",System.Array.ConvertAll(silhouette.GetComponentsInChildren<SkinnedMeshRenderer>(true),r=>string.Join("/",System.Array.ConvertAll(r.sharedMaterials,m=>m?m.name:"null")))));
            }
            catch(System.Exception e){Debug.LogWarning("HC_COUNT_GORE skipped: "+e.Message);}
        }
        // A cold, shadowless rim light that rides behind his shoulders so wings and coat read against dark stone
        // (r2 hunt: "an unlit black smear"). Castle only; Split Grin never gets it.
        void CountRimLight()
        {
            var rim=rimLight=new GameObject("Count rim light").AddComponent<Light>();rim.transform.SetParent(silhouette,false);
            rim.transform.localPosition=new Vector3(0,2.3f,-1.1f);rim.type=LightType.Point;rim.color=new Color(.62f,.72f,1f);
            rim.range=count.RimLightRange;rim.intensity=count.RimLightIntensity;rim.shadows=LightShadows.None;rim.renderMode=LightRenderMode.ForcePixel;
        }
        // ---- Wave I: flight posture, dive arms, wing-tip clamp, mode binding (r10 blind critic) -----------------------------
        readonly List<Transform> rigJoints=new List<Transform>();
        sealed class HandVerts{public SkinnedMeshRenderer skin;public int side;public readonly List<int> indices=new List<int>();public Vector3[] mesh;public BoneWeight[] weights;public Matrix4x4[] bindposes;}
        readonly List<HandVerts> handVerts=new List<HandVerts>();
        // Vertices weighted mostly (>= 0.5) to a hand bone, per renderer and side (the mesh is readable: CountClaudeImport).
        void CacheHandVertices()
        {
            int total=0;
            foreach(var skin in silhouette.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh=skin.sharedMesh;if(!mesh||!mesh.isReadable)continue;var bones=skin.bones;
                int l=-1,r=-1;for(int i=0;i<bones.Length;i++){if(bones[i]&&bones[i].name=="LeftHand")l=i;else if(bones[i]&&bones[i].name=="RightHand")r=i;}
                if(l<0&&r<0)continue;
                var weights=mesh.boneWeights;var meshVerts=mesh.vertices;var binds=mesh.bindposes;
                var hl=new HandVerts{skin=skin,side=0,mesh=meshVerts,weights=weights,bindposes=binds};var hr=new HandVerts{skin=skin,side=1,mesh=meshVerts,weights=weights,bindposes=binds};
                for(int v=0;v<weights.Length;v++)
                {
                    var bw=weights[v];
                    float wl=(bw.boneIndex0==l?bw.weight0:0)+(bw.boneIndex1==l?bw.weight1:0)+(bw.boneIndex2==l?bw.weight2:0)+(bw.boneIndex3==l?bw.weight3:0);
                    float wr=(bw.boneIndex0==r?bw.weight0:0)+(bw.boneIndex1==r?bw.weight1:0)+(bw.boneIndex2==r?bw.weight2:0)+(bw.boneIndex3==r?bw.weight3:0);
                    if(l>=0&&wl>=.5f)hl.indices.Add(v);else if(r>=0&&wr>=.5f)hr.indices.Add(v);
                }
                if(hl.indices.Count>0)handVerts.Add(hl);if(hr.indices.Count>0)handVerts.Add(hr);total+=hl.indices.Count+hr.indices.Count;
            }
            Debug.Log("HC_COUNT_HANDS vertices="+total+" groups="+handVerts.Count);
        }
        readonly List<Transform> wingExt=new List<Transform>();readonly List<Vector3> wingExtRest=new List<Vector3>();
        Transform armL,armR,foreL,foreR,handL,handR;
        float pitchNow,poseLegTimer=-1;bool poseLogged,modesLogged;
        readonly HashSet<int> clampLogged=new HashSet<int>();
        // Bind-pose offsets, read before the Animator has ever evaluated (the silhouette is still inactive here).
        void CacheRig()
        {
            foreach(var t in silhouette.GetComponentsInChildren<Transform>(true))
            {
                if(t.name.StartsWith("WingFan",System.StringComparison.Ordinal)&&t.name.EndsWith("Ext",System.StringComparison.Ordinal)){wingExt.Add(t);wingExtRest.Add(t.localPosition);}
                else if(t.name=="LeftArm")armL=t;else if(t.name=="RightArm")armR=t;else if(t.name=="LeftForeArm")foreL=t;else if(t.name=="RightForeArm")foreR=t;
                else if(t.name=="LeftHand")handL=t;else if(t.name=="RightHand")handR=t;
            }
            rigJoints.AddRange(silhouette.GetComponentsInChildren<Transform>(true));
            CacheHandVertices();
            Debug.Log("HC_COUNT_RIG wingExt="+wingExt.Count+" arms="+(armL&&armR&&foreL&&foreR&&handL&&handR));
        }
        // Pitch the whole figure about a point FlightPitchPivot above his feet (nose-down = positive).
        void SetFlightPose(float pitch)
        {
            var rot=Quaternion.Euler(pitch,0,0);var pivot=Vector3.up*count.FlightPitchPivot;
            silhouette.localRotation=rot;silhouette.localPosition=pivot-rot*pivot;
        }
        void LateUpdate()
        {
            if(count==null||!silhouette||!silhouette.gameObject.activeInHierarchy||!actor)return;
            float dt=Time.deltaTime;
            if(!modesLogged&&actor.Animator&&actor.Animator.isActiveAndEnabled&&actor.Animator.runtimeAnimatorController)
            {
                modesLogged=true;string[] names={"Perch","Glide","Search","Dive","Stagger","Death","Grab","Crawl","GrabDrop","GrabCrawl","GrabSideL","GrabSideR","GrabDropSpin"};var b=new System.Text.StringBuilder();
                for(int i=0;i<names.Length;i++)b.Append(i).Append(':').Append(names[i]).Append('=').Append(HasModeState(i,names[i])?1:0).Append(' ');
                Debug.Log("HC_COUNT_MODES bound="+b.ToString().Trim()+" frames=["+string.Join(",",System.Array.ConvertAll(actor.ModeFrames,f=>f.ToString()))+"]");
            }
            if(!choreo)
            {
                // Glide and Dive lean into the travel direction: at least FlightPitchMin nose-down, more when descending.
                bool flying=!staged&&!capturePresentation&&(actor.Mode==Profile.ModeMove||actor.Mode==Profile.ModeReach)&&flightSpeed>count.FlightPitchMinSpeed&&gateStage!=2&&
                            (flight==Flight.Travel||flight==Flight.Retreat||flight==Flight.Chase||flight==Flight.NearMiss||gateStage>0);
                float target=0;
                if(flying)
                {
                    var v=flightVelocity;float horizontal=new Vector2(v.x,v.z).magnitude;
                    float descent=Mathf.Atan2(-v.y,Mathf.Max(horizontal,.01f))*Mathf.Rad2Deg;
                    target=Mathf.Clamp(count.FlightPitchMin+Mathf.Max(0,descent)*.6f,count.FlightPitchMin,count.FlightPitchMax);
                }
                // count-l4: in the last FlareDistance before a grab he flares upright (the grab clips start upright).
                bool flaring=flight==Flight.Chase&&flare<1;
                if(flaring)target*=flare;
                pitchNow=staged||capturePresentation?0:Mathf.Lerp(pitchNow,target,1-Mathf.Exp(-count.FlightPitchRate*(flaring?3:1)*dt));
                if(target>0&&!staged&&!capturePresentation&&!flaring)pitchNow=Mathf.Max(pitchNow,count.FlightPitchMin); // r11: floor 38 deg in every airborne leg
                SetFlightPose(pitchNow);
                // One HC_COUNT_POSE per leg, logged once the lean has settled (0.4 s after it starts).
                if(target>0){if(poseLegTimer<0)poseLegTimer=0;poseLegTimer+=dt;if(!poseLogged&&poseLegTimer>.4f){poseLogged=true;Debug.Log("HC_COUNT_POSE pitch="+pitchNow.ToString("F0")+" target="+target.ToString("F0")+" mode="+actor.Mode+" state="+flight+" speed="+flightSpeed.ToString("F1"));}}
                else{poseLegTimer=-1;poseLogged=false;}
                // Dive: the arms reach along the pitched body forward.
                if(actor.Mode==Profile.ModeReach&&target>0)ReachArms(count.DiveArmReach*Mathf.Clamp01(pitchNow/Mathf.Max(1,target)));
            }
            else if(choreoPhase==2)ReachArms(count.DiveArmReach);
            ClampWingTips();
            KeepJointsClear();
            GateLantern();
            if(grabCam)GrabCamFrame();
            // fd-grab: the skull as rendered this frame (the next commit is seated on it).
            if(actor.Head){lastSkull=actor.Head.position;lastSkullFrame=Time.frameCount;}
        }
        void ReachArms(float weight)
        {
            if(weight<=0||!armL||!armR||!foreL||!foreR||!handL||!handR)return;
            var forward=silhouette.forward;
            void Aim(Transform bone,Transform child)
            {
                var dir=child.position-bone.position;if(dir.sqrMagnitude<1e-6f)return;dir.Normalize();
                var want=Vector3.Slerp(dir,forward,weight);bone.rotation=Quaternion.FromToRotation(dir,want)*bone.rotation;
            }
            Aim(armL,foreL);Aim(armR,foreR);Aim(foreL,handL);Aim(foreR,handR);
        }
        // r10 H09/H10 wing-tip zig-zag: the Grab clip stretches the fan Ext bones to 2.2x (tools/count_prepare.py, grab(),
        // stretch=2.2). Any stretch past WingExtMaxStretch is clamped back along the same direction.
        void ClampWingTips()
        {
            float max=count.WingExtMaxStretch-1;if(max<0||count.WingExtMaxStretch<=0)return;
            float worst=0;
            for(int i=0;i<wingExt.Count;i++)
            {
                var t=wingExt[i];if(!t)continue;var rest=wingExtRest[i];var delta=t.localPosition-rest;float limit=rest.magnitude*max;
                float ratio=rest.sqrMagnitude>1e-8f?delta.magnitude/rest.magnitude:0;worst=Mathf.Max(worst,ratio);
                if(delta.magnitude>limit&&limit>0)t.localPosition=rest+delta.normalized*limit;
            }
            if(worst>max&&clampLogged.Add(actor.Mode))Debug.Log("HC_COUNT_WING_CLAMP mode="+actor.Mode+" stretch="+(1+worst).ToString("F2")+" clampedTo="+count.WingExtMaxStretch.ToString("F2"));
        }
        // ---- Wave K: grab camera, drop from above, door torn open, sword recoil (r11 blind playtest critic) ------------------
        bool grabCam,grabKill,grabHide,hideGrabLocked,grabCamSnap;float grabCamT,camYaw0,camPitch0;int grabCamLog;string grabCamKind="";
        /// <summary>True while a grab steers the view (integrator: Game's Dead-phase look-at may skip itself then).</summary>
        public bool GrabCamActive=>count!=null&&grabCam;
        void BeginGrabCam(string path,bool hide=false)
        {
            if(grabCam)return;
            doorClampLogged=false;grabCamSnap=false;firstCutWhy="";seatCut=false;seatCutGap=0;spinLogged=false;spinEndLogged=false;grabCam=true;grabKill=false;grabHide=hide;grabCamT=0;grabCamLog=0;grabCamKind=path;grabBeginRoot=transform.position;Debug.Log("HC_COUNT_GRABCAM begin path="+path+" hide="+hide+" root="+transform.position.ToString("F2")+" "+DiagnosticState);
            // fd-grab: per-grab presentation state (seat offset, first frame, distance and view-line records).
            presentT=0;seatSet=false;leashSet=false;seat0=seatApplied=Vector3.zero;camInit=false;sideCheckLogged=false;camDistLogged=false;minEyeSkull=float.MaxValue;minEyeSkullT=0;
            lineClear=lineSamples=0;lineBlockers.Clear();skullPushes=0;presentFrames=0;
            ShieldFromLantern(true);
            // fd-grab: every grab clip starts at once (no 0.06 s any-state crossfade from the dive or crawl pose), so the seat offset
            // measured in the commit frame covers the whole pose change and decays smoothly.
            if(grabMode>=0&&clipPlayedFrame!=Time.frameCount&&actor&&actor.Animator&&actor.Animator.runtimeAnimatorController&&grabMode<ModeStateNames.Length&&HasModeState(grabMode,ModeStateNames[grabMode]))
            {
                actor.Pose(grabMode);float n0=0;
                if(clipStartT>0){var clip=FindClip(ModeStateNames[grabMode]);if(clip&&clip.length>.01f)n0=Mathf.Clamp01(clipStartT/clip.length);}   // ff-count: a hide crawl's drawn start
                actor.Animator.Play(ModeStateNames[grabMode],0,n0);clipPlayedFrame=Time.frameCount;
            }
            NoteCommit(path,hide);
            // fd-grab (r13 natural3: silhouette at the commit, fully lit 0.15 s later): a torch that failed in the dive's last second stays
            // out for the whole grab (it used to come back mid-grab); a burning torch keeps burning (no failure starts in a grab).
            bool failed=Game.Scares&&Game.Scares.TorchFailed;
            // ff-count (r14: hide grabs path=nearmiss / hideopen averaged 5/255 luma, only his ember eyes showed): when he tears the hide open
            // her lantern swings up. From the commit frame (no pop between t 0.01 and 0.16) her hand lantern's beam is lit even if she had put
            // it out, with a warm glow at the lantern in her hand that spills over the hide's opening and him (Investigator.LanternRaise).
            if(hide){Game.Player.LanternGlowIntensity=count.HideLanternGlow;Game.Player.LanternGlowRange=count.HideLanternRange;Game.Player.LanternRaise=1;}
            else if(failed)Game.Scares.Blackout(count.ClipSecondsFor(grabMode)-clipStartT+.6f);
            Debug.Log("HC_GRAB_LIGHT torch="+(Game.Player.TorchOn?(failed?"held-out":"on"):"off")+" rim="+rimScale.ToString("F2")+" layer="+count.GrabLightLayer+" gate="+(lanternGated?"always":"pending")+" mode="+grabMode+
                " hideLantern="+(hide?"raised glow="+count.HideLanternGlow.ToString("F2")+" range="+count.HideLanternRange.ToString("F1")+(failed?" (over a failed torch)":""):"none"));
        }
        void EndGrabCam(string why)
        {
            rootBlend=false;if(!grabCam)return;
            LogCamDist("end "+why);
            grabCam=false;grabKill=false;grabHide=false;hideGrabLocked=false;ShieldFromLantern(false);crawlVarOn=false;grabClipSpeed=1;
            if(Game&&Game.Player)Game.Player.LanternRaise=0;   // ff-count: the hide grab's raised lantern goes back down
            // The seat offset and pushes stay where they are (no snap back); the presentation clock lets go of the Animator.
            seatApplied=Vector3.zero;if(actor&&actor.Animator)actor.Animator.speed=1;clockScaled=false;
            RestoreLineProps();
            Debug.Log("HC_COUNT_GRABCAM end path="+grabCamKind+" reason="+why);
        }
        // r12 (the purple grave-ash lantern recoloured him at the kill): he is on GrabLightLayer and the lantern does not light that layer.
        // fd-grab (r13: the layer switched at the commit, and the fight frames were still purple): the gate is permanent in the castle,
        // applied the first frame he is in the world, so nothing about his lighting changes at the commit.
        bool lanternGated;
        void ShieldFromLantern(bool on){GateLantern();}
        void GateLantern()
        {
            if(lanternGated||count==null||!silhouette)return;
            var uv=Game&&Game.Player?Game.Player.Ultraviolet:null;int layer=count.GrabLightLayer;
            if(!uv||layer<0||layer>=32)return;
            lanternGated=true;int n=0;
            foreach(var r in silhouette.GetComponentsInChildren<Renderer>(true)){r.gameObject.layer=layer;n++;}
            uv.cullingMask&=~(1<<layer);
            Debug.Log("HC_COUNT_GRAB light=lantern-excluded layer="+layer+" renderers="+n+" uvLight=True gate=always (no layer switch at the commit; the grave-ash lantern never recolours him)");
        }
        // ---- fd-grab: the presentation clock ----------------------------------------------------------------------------
        // While the grab camera runs, the Animator, the root easing and the camera advance at most GrabMaxFrameStep per rendered frame:
        // a slow frame (a screenshot, a hitch) slows the presentation instead of skipping it, so each frame continues the last
        // (r13: the 0.44-0.87 m continuity steps were clip velocity x 0.1-0.2 s screenshot frames). Normal frames are untouched.
        float presentDt,presentT;bool clockScaled;int presentFrames,clipPlayedFrame=-9;
        static readonly string[] ModeStateNames={"Perch","Glide","Search","Dive","Stagger","Death","Grab","Crawl","GrabDrop","GrabCrawl","GrabSideL","GrabSideR","GrabDropSpin"};
        /// <summary>Seconds the grab presentation advanced this frame (PlaytestDriver's continuity measure); 0 outside a grab.</summary>
        public float GrabFrameDt=>grabCam?presentDt:0;
        /// <summary>Seconds of grab presentation so far (presentation clock).</summary>
        public float GrabPresentT=>grabCam?presentT:0;
        public int GrabModeNow=>grabMode;
        void PresentationClock()
        {
            var anim=actor?actor.Animator:null;
            if(!grabCam){if(anim&&clockScaled){anim.speed=1;clockScaled=false;}presentDt=0;return;}
            bool unscaled=anim&&anim.updateMode==AnimatorUpdateMode.UnscaledTime;
            float step=unscaled||Time.timeScale>0?Mathf.Min(Time.unscaledDeltaTime,count.GrabMaxFrameStep):0;presentDt=step;
            if(!anim)return;
            float raw=unscaled?Time.unscaledDeltaTime:Time.deltaTime;
            anim.speed=(raw>1e-5f?Mathf.Clamp01(step/raw):1)*(crawlVarOn?grabClipSpeed:1);clockScaled=true;   // ff-count: the crawl's drawn speed
        }
        // ---- fd-grab: grab records for the integrator ---------------------------------------------------------------------
        // HC_GRAB_COMMIT (one per grab, with the mode actually played), HC_GRAB_MODE_HIST (this run, running), the variety deck.
        readonly int[] runModes=new int[CountClaudeProfile.ModeCount];int runGrabs,runHide,runReturn,runOpenHeadroom,runOpenHeadroomDrops;
        void NoteCommit(string path,bool hide)
        {
            int m=grabMode;var feet=Game.Player.transform.position;
            // fe-count (r13 FD: openHeadroom=0 in every run): "headroom" means a drop could have fallen here: over her, or at a floored
            // stand-off on the ring around her (his own bearing first). It used to read the ceiling over her head only, which under the hall
            // gallery is 2.92 m while the vault a step away is open, so no open-floor grab ever counted.
            bool ret=Game.Case.Phase==Phase.Return;bool headroom=DropRoomRing(feet,count.GrabDropClipTop,out float roomCeiling,out string roomAt);
            runGrabs++;if(m>=0&&m<runModes.Length)runModes[m]++;if(hide)runHide++;if(ret)runReturn++;
            if(!hide&&!ret&&headroom){runOpenHeadroom++;if(m==count.ModeGrabDrop||m==count.ModeGrabDropSpin)runOpenHeadroomDrops++;}
            if(!hide&&m>=0&&m<Deck().Length){DeckState.Committed(m);SaveDeck();}
            if(IsSide(m))huntSideOwed=false;
            Debug.Log("HC_GRAB_COMMIT mode="+m+" path="+path+" hide="+hide+" return="+ret+" headroom="+headroom+" headroomAt="+roomAt.Replace(' ','_')+" ceilingMax="+roomCeiling.ToString("F2")+" planned="+PlanName(plannedMode)+" clip_start_t="+clipStartT.ToString("F2")+" deck="+DeckText());
            var b=new System.Text.StringBuilder();for(int i=6;i<runModes.Length;i++)b.Append(" m").Append(i).Append('=').Append(runModes[i]);
            Debug.Log("HC_GRAB_MODE_HIST grabs="+runGrabs+b+" hide="+runHide+" return="+runReturn+" openHeadroom="+runOpenHeadroom+" openHeadroomDrops="+runOpenHeadroomDrops+
                " dropShareHeadroom="+(runOpenHeadroom>0?(100f*runOpenHeadroomDrops/runOpenHeadroom).ToString("F0")+"%":"n/a")+" deck="+DeckText());
        }
        // HC_GRAB_CAMDIST: per grab, the closest the lens came to his skull, and how many presentation frames had a clear eye-to-skull line.
        float minEyeSkull=float.MaxValue,minEyeSkullT;int lineClear,lineSamples,skullPushes;bool camDistLogged;readonly List<string> lineBlockers=new List<string>();
        void LogCamDist(string when)
        {
            if(camDistLogged||presentFrames==0)return;camDistLogged=true;
            Debug.Log("HC_GRAB_CAMDIST min="+(minEyeSkull<float.MaxValue?minEyeSkull.ToString("F2"):"n/a")+" at_t="+minEyeSkullT.ToString("F2")+" ok="+(minEyeSkull>=.6f)+" frames="+presentFrames+
                " lineClear="+lineClear+"/"+lineSamples+" blockers=["+string.Join(",",lineBlockers)+"] pushes="+skullPushes+" hiddenProps=["+string.Join(",",lineHiddenNames)+"] mode="+grabMode+" path="+grabCamKind+" when="+when);
        }
        // Small props on the line from her eye to his skull or claws (r13 siege_held: a floor candelabrum stood in front of him) are hidden
        // for the presentation and put back when it ends. Only things smaller than ViewPropMaxSize: candelabra and the floating props.
        readonly List<GameObject> lineHidden=new List<GameObject>();readonly List<string> lineHiddenNames=new List<string>();List<Transform> lineProps;
        void ClearLineProps(Vector3 eye,Vector3 head,Vector3 claws)
        {
            if(lineProps==null)
            {
                lineProps=new List<Transform>();
                if(Game.World!=null&&Game.World.Root)foreach(var t in Game.World.Root.GetComponentsInChildren<Transform>(true))if(t.name=="Candelabra")lineProps.Add(t);
                foreach(var f in Floater.All)if(f)lineProps.Add(f.transform);
                Debug.Log("HC_GRAB_LINE props="+lineProps.Count);
            }
            foreach(var t in lineProps)
            {
                if(!t||!t.gameObject.activeInHierarchy||lineHidden.Contains(t.gameObject)||(t.position-eye).sqrMagnitude>49)continue;
                var rs=t.GetComponentsInChildren<Renderer>();if(rs.Length==0)continue;
                var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
                if(bounds.size.x>count.ViewPropMaxSize||bounds.size.y>count.ViewPropMaxSize||bounds.size.z>count.ViewPropMaxSize)continue;
                bool onLine=false;
                foreach(var target in new[]{head,claws})
                {
                    var c=ClosestOnSegment(eye,target,bounds.center);
                    if(Vector3.Distance(bounds.ClosestPoint(c),c)<count.ViewLineRadius){onLine=true;break;}
                }
                if(!onLine)continue;
                // Renderers and colliders only: a candelabrum's own candle light stays on (no lighting change in the grab).
                foreach(var r in rs)if(r.enabled){r.enabled=false;lineHiddenRenderers.Add(r);}
                foreach(var c in t.GetComponentsInChildren<Collider>())if(c.enabled){c.enabled=false;lineHiddenColliders.Add(c);}
                lineHidden.Add(t.gameObject);lineHiddenNames.Add(t.name);
                Debug.Log("HC_GRAB_LINE hidden="+t.name+" at="+t.position+" size="+bounds.size+" reason=between her eye and his skull/claws");
            }
        }
        readonly List<Renderer> lineHiddenRenderers=new List<Renderer>();readonly List<Collider> lineHiddenColliders=new List<Collider>();
        void RestoreLineProps()
        {
            foreach(var r in lineHiddenRenderers)if(r)r.enabled=true;foreach(var c in lineHiddenColliders)if(c)c.enabled=true;
            lineHiddenRenderers.Clear();lineHiddenColliders.Clear();lineHidden.Clear();lineHiddenNames.Clear();
        }
        // The lens never closer than MinSkullDistance to his skull: his root is pushed back along her line of sight (the hide door axis
        // in a hide grab). r13 fear: 0.31-0.46 m, a dark cranium filling the frame.
        void KeepSkullOut(Vector3 eye)
        {
            if(!actor||!actor.Head)return;var r=actor.Head.position-eye;float min=count.MinSkullDistance;if(r.magnitude>=min)return;
            var away=grabHide?hideOutward:transform.position-eye;away.y=0;
            if(away.sqrMagnitude<1e-4f){away=Game.Player.View.transform.forward;away.y=0;}
            if(away.sqrMagnitude<1e-4f)return;away.Normalize();
            float b=Vector3.Dot(r,away),c=r.sqrMagnitude-min*min;float push=-b+Mathf.Sqrt(Mathf.Max(0,b*b-c));
            if(push<=0)return;transform.position+=away*push;skullPushes++;
            if(skullPushes==1)Debug.Log("HC_GRAB_CAMDIST push="+push.ToString("F2")+" was="+r.magnitude.ToString("F2")+" min="+min.ToString("F2")+" t="+presentT.ToString("F2")+" mode="+grabMode);
            // fg-polish: the lens-in-skull push is the one mover allowed past the root leash (logged once per grab).
            if(leashSet&&!leashPushLogged){var d=transform.position-leashRoot;d.y=0;if(d.magnitude>RootLeashMeters+.005f){leashPushLogged=true;Debug.Log("HC_GRAB_LEASH over=skull_push mode="+grabMode+" path="+grabCamKind+" travel="+d.magnitude.ToString("F2")+" t="+presentT.ToString("F2"));}}
        }
        Vector3 lastSkull;int lastSkullFrame=-9;
        Vector3 seat0,seatApplied;float seatSeconds;bool seatSet,camInit;
        // count17: commit a grab. GrabDrop needs headroom for its own fall (GrabDropClipTop over his floor); without it the grab is
        // GrabCrawl from the door. A hide grab stands him in the torn door opening at the clip's stand-off from her, facing in,
        // so his skull lands in the centre band once the view has turned and the nearer claw ends about GrabCamClawDistance away.
        float spinYawStart;
        // count-l3 (r13 f0a/f0b: every mode 8 grab rose about 3 m between the commit sample and 0.16 s): the drop clip starts with
        // him 4.7 m up over his origin, and the origin was left at the dive's hover height. At commit the clip's first frame is
        // sampled with the figure upright, and his origin is set so the clip's skull starts where his skull is now; the origin then
        // eases to her floor over the clip's fall (GrabRootBlendDrop) or the crawl's lunge (GrabRootBlendCrawl).
        bool rootBlend;float rootFromY,rootToY,rootBlendSeconds,rootT;
        // count-l4 (r13 f0c huntcam1: root=matched headNow=4.76 clipHead=2.28, then the skull rose 1.3 m): two faults.
        // (1) the clip was sampled on the silhouette root, which has no Animator ("Non-Legacy animations cannot be sampled ...
        // without an Animator"), so clipHead was the dive pose, not the clip; it is now sampled on the Animator's own GameObject.
        // (2) GrabDrop / GrabDropSpin open with an anticipation lift before the fall. The clip's skull height over his origin is
        // sampled every 1/60 s; playback starts at the first falling frame (after the peak) whose skull height matches his skull
        // over her floor, so the commit is continuous and the skull only ever falls (HC_COUNT_GRAB clip_start_t=). Higher than the
        // peak: start at the peak and ease the root down. GrabCrawl starts at its first frame (no lift), head-matched as before.
        float clipStartT;   // seconds into the grab clip where playback started (shifts the drop-spin camera window)
        static readonly Dictionary<int,float[]> clipSkull=new Dictionary<int,float[]>();
        const float ClipSampleStep=1f/60;
        static string StateFor(CountClaudeProfile c,int mode)=>mode==c.ModeGrabDrop?"GrabDrop":mode==c.ModeGrabCrawl?"GrabCrawl":mode==c.ModeGrabDropSpin?"GrabDropSpin":null;
        AnimationClip FindClip(string state)
        {
            var controller=actor&&actor.Animator?actor.Animator.runtimeAnimatorController:null;if(!controller||state==null)return null;
            AnimationClip best=null;
            foreach(var c in controller.animationClips)
            {
                if(!c||c.name.IndexOf(state,System.StringComparison.OrdinalIgnoreCase)<0)continue;
                if(state=="GrabDrop"&&c.name.IndexOf("Spin",System.StringComparison.OrdinalIgnoreCase)>=0)continue;
                best=c;break;
            }
            return best;
        }
        // Skull height over his origin through the clip (upright), sampled on the Animator's GameObject; cached per mode.
        float[] SkullCurve(int mode,AnimationClip clip)
        {
            if(clipSkull.TryGetValue(mode,out var curve))return curve;
            var target=actor.Animator.gameObject;int n=Mathf.Max(2,Mathf.CeilToInt(clip.length/ClipSampleStep)+1);curve=new float[n];
            var track=new Vector3[n];
            for(int i=0;i<n;i++){clip.SampleAnimation(target,Mathf.Min(clip.length,i*ClipSampleStep));curve[i]=actor.Head.position.y-transform.position.y;track[i]=transform.InverseTransformPoint(actor.Head.position);}
            clipSkull[mode]=curve;clipSkullLocal[mode]=track;
            int peak=0;for(int i=1;i<n;i++)if(curve[i]>curve[peak])peak=i;
            Debug.Log("HC_COUNT_CLIP_SKULL mode="+mode+" clip="+clip.name+" frames="+n+" start="+curve[0].ToString("F2")+" peak="+curve[peak].ToString("F2")+"@"+(peak*ClipSampleStep).ToString("F2")+"s end="+curve[n-1].ToString("F2"));
            return curve;
        }
        // Sample the drop clips' skull curves once, in Update, upright (the Animator overwrites the pose before this frame renders),
        // so the first high dive already hovers to the real clip peak.
        void PrimeClipCurves()
        {
            if(!actor||!actor.Animator||!actor.Head||!actor.Animator.isActiveAndEnabled)return;
            var rot=silhouette.localRotation;var pos=silhouette.localPosition;
            foreach(int m in new[]{count.ModeGrabDrop,count.ModeGrabDropSpin})
            {
                if(m<0||clipSkull.ContainsKey(m))continue;var clip=FindClip(StateFor(count,m));if(!clip)continue;
                silhouette.localRotation=Quaternion.identity;silhouette.localPosition=Vector3.zero;SkullCurve(m,clip);
            }
            // fd-grab: where each side clip's skull starts relative to his origin (his local frame). He faces her, so a start on his
            // right (+x) is on her left: that clip is the one for a Count on her left. This fixes the 10/11 mapping before the first
            // side grab (count-l2's on-screen check could only correct it after one had played from the wrong side), and gives the
            // side approach its exact start point.
            if(!sideMapped)
            {
                var target=actor.Animator.gameObject;
                foreach(int m in new[]{count.ModeGrabSideL,count.ModeGrabSideR})
                {
                    if(m<0||sideStartLocal.ContainsKey(m))continue;var clip=FindClip(m==count.ModeGrabSideL?"GrabSideL":"GrabSideR");if(!clip)continue;
                    silhouette.localRotation=Quaternion.identity;silhouette.localPosition=Vector3.zero;
                    // fe3-grab: the whole skull track (his local frame), for the seat gap and the +0.3 s framing.
                    int n=Mathf.Max(2,Mathf.CeilToInt(clip.length/ClipSampleStep)+1);var track=new Vector3[n];
                    for(int i=n-1;i>=0;i--){clip.SampleAnimation(target,Mathf.Min(clip.length,i*ClipSampleStep));track[i]=transform.InverseTransformPoint(actor.Head.position);}
                    clipSkullLocal[m]=track;sideStartLocal[m]=track[0];
                }
                if(sideStartLocal.TryGetValue(count.ModeGrabSideL,out var l)&&sideStartLocal.TryGetValue(count.ModeGrabSideR,out var r))
                {
                    sideMapped=true;bool swap=l.x<0&&r.x>0;
                    if(swap!=sideSwapped)sideSwapped=swap;
                    sideChecked=true;   // the clip data decides; the on-screen check only logs
                    Debug.Log("HC_GRAB_SIDE_MAP sideL_start="+l.ToString("F2")+" sideR_start="+r.ToString("F2")+" swapped="+sideSwapped+" (a start on his right is on her left)");
                }
            }
            silhouette.localRotation=rot;silhouette.localPosition=pos;
        }
        static bool sideMapped;static readonly Dictionary<int,Vector3> sideStartLocal=new Dictionary<int,Vector3>();
        // fe3-grab: each grab clip's skull through the clip in his local frame (ClipSampleStep), from the same sampling.
        static readonly Dictionary<int,Vector3[]> clipSkullLocal=new Dictionary<int,Vector3[]>();
        static Vector3 TrackAt(Vector3[] track,float t){if(track==null||track.Length==0)return Vector3.zero;float f=Mathf.Clamp(t/ClipSampleStep,0,track.Length-1);int i=Mathf.Min(track.Length-2,Mathf.FloorToInt(f));if(i<0)return track[0];return Vector3.Lerp(track[i],track[i+1],f-i);}
        int switchedTo=-1;
        // The first frame after the clip's skull peak whose skull is at or under want (the last frame if none).
        static int FallStart(float[] curve,float want)
        {
            int peak=0;for(int i=1;i<curve.Length;i++)if(curve[i]>curve[peak])peak=i;
            int start=peak;for(int i=peak;i<curve.Length;i++){start=i;if(curve[i]<=want)break;}
            return start;
        }
        // fe3-grab: where the side / drop-spin clip's first skull will be if he commits now: the placement CommitGrab makes (the side's
        // planned frame, the spin's outward stand-off and matched start frame) and the clip's sampled skull track. False when it cannot be
        // predicted (no track yet, or MatchClipStart would take the crawl).
        bool PredictVariantSkull(int mode,out Vector3 skull)
        {
            skull=Vector3.zero;var p=Game.Player;var feet=p.transform.position;
            if(mode<0||!actor||!actor.Head||!clipSkullLocal.TryGetValue(mode,out var track)||track.Length==0)return false;
            if(IsSide(mode))
            {
                var f=p.View.transform.forward;f.y=0;f=f.sqrMagnitude>1e-4f?f.normalized:p.transform.forward;
                var dir=IsSide(plannedMode)&&planSide!=0?SideFrameForward(planSide):f;
                var at=feet+dir*count.StandOffFor(mode);at.y=feet.y;
                skull=at+Quaternion.LookRotation(-dir)*track[0];return true;
            }
            if(mode!=count.ModeGrabDropSpin||!clipSkull.TryGetValue(mode,out var curve)||curve.Length==0)return false;
            var outward=transform.position-feet;outward.y=0;
            if(outward.sqrMagnitude<.01f){outward=-p.View.transform.forward;outward.y=0;}
            if(outward.sqrMagnitude<.01f)outward=Vector3.forward;outward.Normalize();
            if(spinConeDir!=Vector3.zero)outward=spinConeDir;
            float head=actor.Head.position.y,want=head-feet.y;
            if(want<curve[curve.Length-1]&&count.ModeGrabCrawl>=0&&HasModeState(count.ModeGrabCrawl,"GrabCrawl"))return false;
            int start=FallStart(curve,want);var local=track[Mathf.Min(start,track.Length-1)];
            var at2=feet+outward*count.StandOffFor(mode);at2.y=Mathf.Max(feet.y,head-curve[start]);
            skull=at2+Quaternion.LookRotation(-outward)*new Vector3(local.x,curve[start],local.z);return true;
        }
        void MatchClipStart(int mode,float floorY,float headBefore)
        {
            rootBlend=false;clipStartT=0;
            string state=StateFor(count,mode);var clip=FindClip(state);
            pitchNow=0;SetFlightPose(0);            // the grab clips are played upright
            if(!clip||!actor.Head){Debug.Log("HC_COUNT_GRAB root=floor reason=no "+state+" clip to sample");var p0=transform.position;p0.y=floorY;transform.position=p0;return;}
            var curve=SkullCurve(mode,clip);
            float want=headBefore-floorY;   // his skull over her floor right now
            // Lower than anywhere the drop's skull goes after its peak: a drop would have to jump up first. Take the crawl instead.
            if((mode==count.ModeGrabDrop||mode==count.ModeGrabDropSpin)&&want<curve[curve.Length-1]&&count.ModeGrabCrawl>=0&&HasModeState(count.ModeGrabCrawl,"GrabCrawl"))
            {
                Debug.Log("HC_COUNT_GRAB mode="+mode+"->9 reason=skull "+want.ToString("F2")+" m over her floor is below the drop's landed skull "+curve[curve.Length-1].ToString("F2"));
                switchedTo=count.ModeGrabCrawl;MatchClipStart(count.ModeGrabCrawl,floorY,headBefore);return;
            }
            int start=0;
            // ff-count (r14: every crawl replayed the same camera-relative frames): the crawl starts at its drawn offset (DrawCrawlVariant).
            if(mode==count.ModeGrabCrawl){DrawCrawlVariant("open");start=Mathf.Clamp(Mathf.RoundToInt(crawlVar.StartT/ClipSampleStep),0,curve.Length-1);}
            if(mode!=count.ModeGrabCrawl)
            {
                int peak=0;for(int i=1;i<curve.Length;i++)if(curve[i]>curve[peak])peak=i;
                start=peak;
                // fd-grab: the drop-spin starts at its matching falling frame too (it started at the peak: a lift when he was lower).
                if(mode==count.ModeGrabDrop||mode==count.ModeGrabDropSpin)start=FallStart(curve,want);
            }
            clipStartT=start*ClipSampleStep;
            float fromY=Mathf.Max(floorY,headBefore-curve[start]);   // never under her floor
            var at=transform.position;at.y=fromY;transform.position=at;
            actor.Pose(mode);actor.Animator.Play(state,0,Mathf.Clamp01(clipStartT/Mathf.Max(.01f,clip.length)));clipPlayedFrame=Time.frameCount;
            rootFromY=fromY;rootToY=floorY;rootBlendSeconds=mode==count.ModeGrabCrawl?count.GrabRootBlendCrawl:Mathf.Max(.15f,count.GrabRootBlendDrop-clipStartT);rootT=0;rootBlend=fromY-floorY>.01f;
            float skullAfter=fromY+curve[start];
            Debug.Log("HC_COUNT_GRAB clip_start_t="+clipStartT.ToString("F2")+" mode="+mode+" clip="+clip.name+" skullFrom="+(headBefore-floorY).ToString("F2")+" skullClip="+curve[start].ToString("F2"));
            Debug.Log("HC_COUNT_GRAB root=matched mode="+mode+" clip="+clip.name+" headNow="+headBefore.ToString("F2")+" clipHead="+curve[start].ToString("F2")+" originY="+fromY.ToString("F2")+" floor="+floorY.ToString("F2")+
                " skullDelta="+(skullAfter-headBefore).ToString("F2")+" fromLastDiveSample="+(lastDiveSkull.HasValue?Vector3.Distance(lastDiveSkull.Value,new Vector3(transform.position.x,skullAfter,transform.position.z)).ToString("F2"):"none")+
                " blend="+(rootBlend?rootBlendSeconds.ToString("F2")+"s":"none")+(mode==count.ModeGrabCrawl?"":" from=above"));
        }
        // fe3-grab: the side snatch's +0.3 s frame. The side camera has turned onto the clip's end point by then (SideTurnSeconds 0.25),
        // so the skull's place on screen is fixed by the clip: predicted from its skull track. If it would sit behind one of her held
        // items (the hand lantern is in her left hand, low left of the frame) or outside the central 60 %, the side camera's aim is turned
        // (sideAimBias, faded out by SideSeconds) to put it beside the item, toward the centre. The held items are on layer 30 with no
        // colliders, so they never block a flank line; this only keeps the skull visible. Logged HC_GRAB_SIDEFRAME.
        float sideAimBias;
        void SideHandFrame(int mode,Vector3 at,Vector3 dir)
        {
            sideAimBias=0;var cam=Game.Player.View;if(!cam||!clipSkullLocal.TryGetValue(mode,out var track))return;
            var eye=cam.transform.position;var aim=at+Vector3.up*count.SideSkullHeight;var to=aim-eye;if(to.sqrMagnitude<1e-4f)return;
            var rot=Quaternion.LookRotation(to);float t=.3f;
            var skull=at+Quaternion.LookRotation(-dir)*TrackAt(track,t);
            float hHalf=Camera.VerticalToHorizontalFieldOfView(cam.fieldOfView,cam.aspect)*.5f*Mathf.Deg2Rad,vHalf=cam.fieldOfView*.5f*Mathf.Deg2Rad;
            var l=Quaternion.Inverse(rot)*(skull-eye);if(l.z<=.05f){Debug.Log("HC_GRAB_SIDEFRAME mode="+mode+" t=0.30 behind=True bias=0");return;}
            float x=.5f+.5f*(l.x/l.z)/Mathf.Tan(hHalf),y=.5f+.5f*(l.y/l.z)/Mathf.Tan(vHalf);
            string covered="none";Rect hit=default;
            foreach(var r in cam.GetComponentsInChildren<Renderer>())
            {
                if(!r.enabled||!r.gameObject.activeInHierarchy||r.gameObject.layer!=30)continue;
                var b=r.bounds;float x0=1,x1=0,y0=1,y1=0;int n=0;
                for(int i=0;i<8;i++)
                {
                    var c=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    var v=cam.WorldToViewportPoint(c);if(v.z<=0)continue;n++;
                    x0=Mathf.Min(x0,v.x);x1=Mathf.Max(x1,v.x);y0=Mathf.Min(y0,v.y);y1=Mathf.Max(y1,v.y);
                }
                if(n==0||x<x0-.01f||x>x1+.01f||y<y0-.01f||y>y1+.01f)continue;
                var top=r.transform;while(top.parent&&top.parent!=cam.transform)top=top.parent;
                covered=top.name;hit=Rect.MinMaxRect(x0,y0,x1,y1);break;
            }
            float tx=x;
            if(covered!="none")tx=hit.center.x<.5f?hit.xMax+.03f:hit.xMin-.03f;
            tx=Mathf.Clamp(tx,.22f,.78f);
            if(Mathf.Abs(tx-x)>.005f)
            {
                float ax=Mathf.Atan((x-.5f)*2*Mathf.Tan(hHalf))*Mathf.Rad2Deg,axT=Mathf.Atan((tx-.5f)*2*Mathf.Tan(hHalf))*Mathf.Rad2Deg;
                sideAimBias=Mathf.Clamp(ax-axT,-15,15);
            }
            bool central=x>=.2f&&x<=.8f&&y>=.2f&&y<=.8f;
            Debug.Log("HC_GRAB_SIDEFRAME mode="+mode+" t=0.30 x="+x.ToString("F2")+" y="+y.ToString("F2")+" central="+central+" covered="+covered.Replace(' ','_')+" bias="+sideAimBias.ToString("F1")+" targetX="+tx.ToString("F2"));
        }
        // ff-count: the crawl's variation for this grab (GrabMixRules.DrawCrawl): clip start offset, playback speed, grab-camera aim offset.
        GrabMixRules.CrawlVariant crawlVar;bool crawlVarOn;float grabClipSpeed=1;static int crawlDraws;
        void DrawCrawlVariant(string path)
        {
            crawlVar=GrabMixRules.DrawCrawl(Random.value,Random.value,Random.value,Random.value,crawlDraws++);crawlVarOn=true;grabClipSpeed=crawlVar.Speed;
            Debug.Log("HC_GRAB_CRAWL_VARIANT path="+path+" start="+crawlVar.StartT.ToString("F2")+" speed="+crawlVar.Speed.ToString("F2")+" yaw="+crawlVar.YawOffset.ToString("F1")+" pitch="+crawlVar.PitchOffset.ToString("F1")+" n="+crawlDraws);
        }
        int CommitGrab(bool hide,int mode)
        {
            clipStartT=0;sideAimBias=0;   // fe3-grab: a side or hide grab kept the last drop's clip start (natural8 side commits logged 0.28)
            grabClipSpeed=1;crawlVarOn=false;   // ff-count: the crawl variant applies to this grab only when drawn
            var p=Game.Player;var feet=p.transform.position;var eye=p.View.transform.position;
            var outward=hide?p.HideExit-feet:transform.position-feet;outward.y=0;
            if(outward.sqrMagnitude<.01f){outward=-p.View.transform.forward;outward.y=0;}
            if(outward.sqrMagnitude<.01f)outward=Vector3.forward;outward.Normalize();
            if(!hide&&mode==count.ModeGrabDropSpin&&spinConeDir!=Vector3.zero)outward=spinConeDir;   // fe2-count: overhead drop-spin
            if(mode==count.ModeGrabDrop)
            {
                var spot=feet+outward*count.StandOffFor(mode);
                float ceiling=Physics.Raycast(spot+Vector3.up*.2f,Vector3.up,out var hit,12,FlightMask,QueryTriggerInteraction.Ignore)?hit.point.y-feet.y:12;
                if(ceiling<count.GrabDropClipTop+.1f)
                {
                    int crawl=count.ModeGrabCrawl>=0&&HasModeState(count.ModeGrabCrawl,"GrabCrawl")?count.ModeGrabCrawl:count.ModeGrab;
                    Debug.Log("HC_COUNT_GRAB from=door reason=headroom ceiling="+ceiling.ToString("F2")+" needs="+(count.GrabDropClipTop+.1f).ToString("F2")+" mode="+mode+"->"+crawl);
                    mode=crawl;
                }
                else Debug.Log("HC_COUNT_GRAB from=above dy="+(count.GrabDropClipTop-(eye.y-feet.y)).ToString("F2")+" ceiling="+ceiling.ToString("F2")+" (the GrabDrop clip falls from "+count.GrabDropClipTop.ToString("F2")+" m)");
            }
            float headBefore=actor.Head?actor.Head.position.y:transform.position.y+2.2f;
            if(!hide&&(mode==count.ModeGrabDrop||mode==count.ModeGrabCrawl)&&mode>=0){switchedTo=-1;MatchClipStart(mode,feet.y,headBefore);if(switchedTo>=0)mode=switchedTo;}
            if(!hide&&(IsSide(mode)||mode==count.ModeGrabDropSpin))
            {
                // Side snatch: his origin goes to the stand-off straight ahead of her view (the clip carries him in from her flank and
                // the camera stays put). Drop-spin: to the stand-off on her side of him, behind her, facing her.
                var f=p.View.transform.forward;f.y=0;f=f.sqrMagnitude>1e-4f?f.normalized:p.transform.forward;
                // fe2-count: a side snatch keeps the frame it was planned in (her view turned planFrameYaw toward his side).
                var dir=mode==count.ModeGrabDropSpin?outward:IsSide(plannedMode)&&planSide!=0?SideFrameForward(planSide):f;
                var at=feet+dir*count.StandOffFor(mode);at.y=feet.y;
                transform.SetPositionAndRotation(at,Quaternion.LookRotation(-dir));
                spinYawStart=Mathf.Atan2(f.x,f.z)*Mathf.Rad2Deg;
                Debug.Log("HC_COUNT_GRAB placed="+(mode==count.ModeGrabDropSpin?"spin":"side")+" at="+at+" standOff="+count.StandOffFor(mode).ToString("F2")+" bearing="+Vector3.SignedAngle(f,outward,Vector3.up).ToString("F0")+" mode="+mode);
                if(mode==count.ModeGrabDropSpin)MatchClipStart(mode,feet.y,headBefore);   // count-l4: start after the anticipation lift
                else SideHandFrame(mode,at,dir);
            }
            if(hide)
            {
                // count18: remember the door geometry; ClampToDoor keeps him on her floor, outside the door plane and within
                // DoorLateral of the door's axis for the whole presentation.
                hideFeet=feet;hideOutward=outward;hideDoorDistance=.43f;
                var found=HideSpotAt(p.HideExit);if(found&&found!=learnedHide){learnedHide=found;Debug.Log("HC_COUNT_LEARNED hide="+found.name+" reason=grabbed her in it");}
                foreach(var h in Game.World.HidingPlaces)
                    if(h&&h.Kind==InteractKind.Hide&&Vector3.Distance(h.ExitPoint,p.HideExit)<.6f){var d=h.transform.position-feet;d.y=0;hideDoorDistance=Mathf.Max(.1f,Vector3.Dot(d,outward));break;}
                var at=feet+outward*count.StandOffFor(mode);at.y=feet.y;
                transform.SetPositionAndRotation(at,Quaternion.LookRotation(-outward));hideGrabLocked=true;
                Debug.Log("HC_COUNT_GRAB placed=door at="+at+" standOff="+count.StandOffFor(mode).ToString("F2")+" mode="+mode);
                if(mode==count.ModeGrabCrawl){DrawCrawlVariant("hide");clipStartT=crawlVar.StartT;}   // BeginGrabCam plays the clip from clipStartT
            }
            return mode;
        }
        void GrabCamFrame()
        {
            var cam=Game.Player.View;if(!cam)return;
            // fd-grab: the presentation clock (at most GrabMaxFrameStep per frame) drives everything below.
            // (0 while the game is paused, or in a frame whose grab began after Presence's Update: nothing moves, the camera holds.)
            float dt=presentDt;grabCamT+=dt;presentT+=dt;presentFrames++;
            // Work on the seated root: take last frame's seat offset off, re-apply the decayed offset below.
            transform.position-=seatApplied;seatApplied=Vector3.zero;
            // Claws ~GrabCamClawDistance from the lens, in the kill presentation and in a hide grab (he is in the door opening).
            bool variant=IsSide(grabMode)||grabMode==count.ModeGrabDropSpin&&grabMode>=0;
            if((grabKill||grabHide)&&!variant&&!rootBlend)FrameClaws(cam,dt);   // count-l3: not while the root is still easing down
            if(grabHide)ClampToDoor();
            // count-l3: the root eases from the head-matched commit height to her floor while the clip plays (no teleport).
            if(rootBlend)
            {
                rootT+=dt;   // its own clock: the kill re-entry rewinds grabCamT for the log samples
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(rootT/Mathf.Max(.05f,rootBlendSeconds)));
                var at=transform.position;at.y=Mathf.Lerp(rootFromY,rootToY,k);transform.position=at;
                if(k>=1){rootBlend=false;Debug.Log("HC_COUNT_GRAB root=landed y="+at.y.ToString("F2")+" t="+rootT.ToString("F2"));}
            }
            var eye=cam.transform.position;
            // fd-grab (r13: 0 of 6 natural grabs continuous): the seat. In the commit frame his skull is put exactly where the last
            // rendered frame left it (whatever the clip's first pose, the hide stand-off or the side/spin placement moved), and that
            // offset decays to the seated root over SeatSeconds (longer for a long offset). A drop never rises: an offset that would lift
            // the skull lowers the root's easing start instead.
            if(!seatSet)
            {
                seatSet=true;seat0=Vector3.zero;seatSeconds=count.SeatSeconds;string note="none";
                if(actor&&actor.Head&&lastSkullFrame==Time.frameCount-1)
                {
                    seat0=lastSkull-actor.Head.position;
                    bool drop=grabMode==count.ModeGrabDrop||grabMode==count.ModeGrabDropSpin;
                    if(drop&&seat0.y<0)
                    {
                        if(rootBlend&&rootFromY+seat0.y>=rootToY){rootFromY+=seat0.y;var at=transform.position;at.y+=seat0.y;transform.position=at;note="lowered the root "+(-seat0.y).ToString("F2");seat0.y=0;}
                        else note="small rise "+(-seat0.y).ToString("F2");
                    }
                    // fe-count (r13 FD: seat gaps up to 0.90 m, natural1's Return grab slid ~1 m over t 0.01-0.16): an open-floor drop or
                    // crawl keeps the horizontal part of the gap beyond SeatMaxGap in its root (he plays the clip up to that much nearer where
                    // his skull already was) instead of sliding it over the seat time. Side, spin and hide grabs keep their placement.
                    bool variantSeat=IsSide(grabMode)||grabMode==count.ModeGrabDropSpin&&grabMode>=0;
                    var flatGap=new Vector3(seat0.x,0,seat0.z);
                    var seatPath=GrabSlideRules.Seat(grabHide,variantSeat,seat0.magnitude,flatGap.magnitude,count.SeatMaxGap);
                    if(seatPath==GrabSlideRules.SeatPath.Reseat)
                    {
                        var move=flatGap*(1-count.SeatMaxGap/flatGap.magnitude);transform.position+=move;seat0-=move;
                        note=(note=="none"?"":note+"; ")+"reseated "+move.magnitude.ToString("F2")+" (raw gap "+(seat0+move).magnitude.ToString("F2")+")";
                    }
                    // fe3-grab: side and spin grabs take the same rule. A gap still over SeatMaxGap after the commit hold (CountChase) is a
                    // hard cut on frame 0: no seat offset (he is in the clip's first pose at once) and the view cuts to his skull.
                    // fg-polish (FF fear: a hideopen crawl seated a 0.92 m gap and slid 0.91 m): hide grabs (nearmiss, hideopen) are placed
                    // in the torn door and cannot keep the gap in their root, so they take the cut too.
                    if(seatPath==GrabSlideRules.SeatPath.Cut)
                    {
                        float g=seat0.magnitude;seatCut=true;seatCutGap=g;seat0=Vector3.zero;
                        note=(note=="none"?"":note+"; ")+"cut on frame 0 (gap "+g.ToString("F2")+")";
                        Debug.Log("HC_GRAB_CUT mode="+grabMode+" path="+grabCamKind+" gap="+g.ToString("F2")+" held="+seatHoldLast.ToString("F2")+" predicted="+seatPredictGap.ToString("F2")+" reason=seat gap over "+count.SeatMaxGap.ToString("F2")+" (no slide)");
                    }
                    // fe-count: frame 0 must show him. If her eye line to the carried skull is blocked, the seat starts part (or all) of the
                    // way toward his clip pose, at the first fraction whose line is clear.
                    var eyeS=Game.Player.View.transform.position;
                    if(seat0.sqrMagnitude>1e-4f&&Blocked(eyeS,lastSkull))
                    {
                        foreach(float f in new[]{.25f,.5f,.75f,1f})
                            if(!Blocked(eyeS,lastSkull-seat0*f)){note=(note=="none"?"":note+"; ")+"relocated "+(seat0.magnitude*f).ToString("F2")+" to a clear line";seat0*=1-f;break;}
                    }
                    seatSeconds=Mathf.Clamp(seat0.magnitude/Mathf.Max(.5f,count.SeatMaxSpeed)*1.5f,count.SeatSeconds,count.SeatMaxSeconds);
                }
                else note="no previous frame";
                Debug.Log("HC_GRAB_SEAT gap="+seat0.magnitude.ToString("F2")+" offset="+seat0.ToString("F2")+" seconds="+seatSeconds.ToString("F2")+" mode="+grabMode+" path="+grabCamKind+" note="+note);
            }
            float ks=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(presentT/Mathf.Max(.05f,seatSeconds)));
            seatApplied=seat0*ks;transform.position+=seatApplied;
            RootLeash();
            KeepSkullOut(eye);
            // Turn the view to his skull (biased toward the claws) over GrabCamTurnSeconds, then hold it there.
            var head=actor.Head?actor.Head.position:transform.position+Vector3.up*2.2f;
            var claws=handL&&handR?(handL.position+handR.position)*.5f:head-Vector3.up*.6f;
            ClearLineProps(eye,head,claws);
            var aim=Vector3.Lerp(head,claws,count.GrabCamClawBias);
            // Side snatch: the camera settles on the clip's end point (he comes in from her flank). After the clip, as usual.
            if(IsSide(grabMode)&&presentT<SideSeconds)aim=transform.position-seatApplied+Vector3.up*count.SideSkullHeight;
            var to=aim-eye;
            if(to.sqrMagnitude>.0001f)
            {
                float yaw=Mathf.Atan2(to.x,to.z)*Mathf.Rad2Deg,pitch=-Mathf.Atan2(to.y,new Vector2(to.x,to.z).magnitude)*Mathf.Rad2Deg;
                if(crawlVarOn&&grabMode==count.ModeGrabCrawl&&grabMode>=0){yaw+=crawlVar.YawOffset;pitch+=crawlVar.PitchOffset;}   // ff-count: the crawl's framing varies
                if(IsSide(grabMode)&&sideAimBias!=0)yaw+=sideAimBias*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.6f,SideSeconds,presentT)));   // fe3-grab: SideHandFrame
                if(!camInit)
                {
                    camInit=true;var f=cam.transform.forward;camYaw0=Mathf.Atan2(f.x,f.z)*Mathf.Rad2Deg;camPitch0=-Mathf.Asin(Mathf.Clamp(f.y,-1,1))*Mathf.Rad2Deg;
                    // fd-grab (r13: no skull in 3 of 8 commit frames): the commit frame's view starts no further from his skull than
                    // FirstFrameMargin of the half field of view, so his skull is on screen at t=0.01; the turn continues from there.
                    var th=head-eye;float hy=Mathf.Atan2(th.x,th.z)*Mathf.Rad2Deg,hp=-Mathf.Atan2(th.y,new Vector2(th.x,th.z).magnitude)*Mathf.Rad2Deg;
                    float hHalf=Camera.VerticalToHorizontalFieldOfView(cam.fieldOfView,cam.aspect)*.5f,vHalf=cam.fieldOfView*.5f;
                    float y0=camYaw0,p0=camPitch0;
                    float dy=Mathf.DeltaAngle(hy,camYaw0),ly=hHalf*count.FirstFrameMargin;if(Mathf.Abs(dy)>ly)camYaw0=hy+Mathf.Sign(dy)*ly;
                    float dp=camPitch0-hp,lp=vHalf*count.FirstFrameMargin;if(Mathf.Abs(dp)>lp)camPitch0=Mathf.Clamp(hp+Mathf.Sign(dp)*lp,-85,85);
                    firstSnapDeg=Quaternion.Angle(Quaternion.Euler(p0,y0,0),Quaternion.Euler(camPitch0,camYaw0,0));
                    // fe-count (r13 FD: snapDeg up to 138 on Return drops from behind): a first frame that has to turn more than
                    // FirstFrameCutDegrees is a hard cut on frame 0 straight to the grab framing (logged cut=True), not a jump to the edge
                    // of the frame followed by a turn.
                    if(firstSnapDeg>count.FirstFrameCutDegrees){grabCamSnap=true;firstCutWhy="turn "+firstSnapDeg.ToString("F0")+" > "+count.FirstFrameCutDegrees.ToString("F0");}
                    // fe3-grab: a seat cut (side / spin) also cuts the view onto his skull on frame 0; the side turn / spin tracking continue
                    // from there.
                    else if(seatCut){camYaw0=hy;camPitch0=Mathf.Clamp(hp,-85,85);firstSnapDeg=Quaternion.Angle(Quaternion.Euler(p0,y0,0),Quaternion.Euler(camPitch0,camYaw0,0));firstCutWhy="seat gap "+seatCutGap.ToString("F2")+" > "+count.SeatMaxGap.ToString("F2");}
                }
                float k=Mathf.SmoothStep(0,1,presentT/Mathf.Max(.01f,IsSide(grabMode)?count.SideTurnSeconds:count.GrabCamTurnSeconds));
                // Drop-spin: her view holds near him while he falls, then spins to his face over the spin window (clip time; the clip may
                // start past its lift, clipStartT), at least 0.35 s long.
                bool spin=grabMode>=0&&grabMode==count.ModeGrabDropSpin;
                float clipT=presentT+clipStartT;
                if(spin){float s0=Mathf.Max(count.SpinStart,clipStartT),s1=Mathf.Max(count.SpinEnd,s0+.35f);k=Mathf.SmoothStep(0,1,Mathf.InverseLerp(s0,s1,clipT));}
                // fe2-count (r13 FE2 r1 fear: a drawbridge drop-spin left the frame at t 0.16-0.30, skull_px x -160 / -19, while her view held
                // still and the clip carried him sideways): through the hold and the spin her view follows his skull at SpinTrackMargin of the
                // half field of view, so it stays inside the central 60 % (the spin then turns the rest of the way).
                if(spin&&k<1&&!grabCamSnap)
                {
                    var th=head-eye;float hy=Mathf.Atan2(th.x,th.z)*Mathf.Rad2Deg,hp=-Mathf.Atan2(th.y,new Vector2(th.x,th.z).magnitude)*Mathf.Rad2Deg;
                    float hHalf=Camera.VerticalToHorizontalFieldOfView(cam.fieldOfView,cam.aspect)*.5f,vHalf=cam.fieldOfView*.5f;
                    float dy=Mathf.DeltaAngle(hy,camYaw0),ly=hHalf*count.SpinTrackMargin;if(Mathf.Abs(dy)>ly)camYaw0=hy+Mathf.Sign(dy)*ly;
                    float dp=camPitch0-hp,lp=vHalf*count.SpinTrackMargin;if(Mathf.Abs(dp)>lp)camPitch0=Mathf.Clamp(hp+Mathf.Sign(dp)*lp,-85,85);
                }
                if(spin&&!spinLogged&&clipT>=count.SpinStart){spinLogged=true;Debug.Log("HC_COUNT_GRABCAM spin=start t="+grabCamT.ToString("F2")+" yawOff="+Mathf.DeltaAngle(camYaw0,yaw).ToString("F0"));}
                if(grabCamSnap)k=1;
                cam.transform.rotation=Quaternion.Euler(Mathf.LerpAngle(camPitch0,pitch,k),Mathf.LerpAngle(camYaw0,yaw,k),0);
                if(presentFrames==1)
                {
                    // Proof: the skull must be inside the frame now; if the clamp could not do it (steep pitch), cut to him.
                    var v=cam.WorldToViewportPoint(head);bool inFrame=v.z>cam.nearClipPlane&&v.x>=0&&v.x<=1&&v.y>=0&&v.y<=1;
                    if(!inFrame){grabCamSnap=true;firstCutWhy="skull outside the clamped first frame";cam.transform.rotation=Quaternion.Euler(pitch,yaw,0);v=cam.WorldToViewportPoint(head);inFrame=v.z>cam.nearClipPlane&&v.x>=0&&v.x<=1&&v.y>=0&&v.y<=1;Debug.Log("HC_COUNT_GRABCAM snap=True reason=skull outside the clamped first frame");}
                    bool clear=!Physics.Linecast(eye,head,out var hb,FlightMask,QueryTriggerInteraction.Ignore)||IgnoreHit(hb.collider);
                    Debug.Log("HC_GRAB_FIRSTFRAME skull_px=("+(v.x*2560).ToString("F0")+","+((1-v.y)*1440).ToString("F0")+") visible="+(inFrame&&clear)+" inFrame="+inFrame+" lineClear="+clear+
                        " snapDeg="+firstSnapDeg.ToString("F0")+(grabCamSnap||seatCut?" cut=True cutWhy=\""+firstCutWhy+"\"":" cut=False")+" seatCut="+seatCut+" eyeToSkull="+Vector3.Distance(eye,head).ToString("F2")+" seatGap="+seat0.magnitude.ToString("F2")+" mode="+grabMode+" path="+grabCamKind);
                }
            }
            // Per-frame records: closest lens-to-skull distance and the eye-to-skull line.
            float es=Vector3.Distance(eye,head);if(es<minEyeSkull){minEyeSkull=es;minEyeSkullT=presentT;}
            lineSamples++;
            if(!Physics.Linecast(eye,head,out var lb,FlightMask,QueryTriggerInteraction.Ignore)||IgnoreHit(lb.collider))lineClear++;
            else if(lineBlockers.Count<4&&!lineBlockers.Contains(lb.collider.name))lineBlockers.Add(lb.collider.name);
            if(!camDistLogged&&presentT>=Mathf.Max(1.2f,count.ClipSecondsFor(grabMode)-clipStartT))LogCamDist("clip end");
            if(IsSide(grabMode)&&!sideCheckLogged&&presentT>=.1f){sideCheckLogged=true;SideCheck(cam);}
            if(grabMode==count.ModeGrabDropSpin&&grabMode>=0&&!spinEndLogged&&presentT+clipStartT>=count.SpinEnd+.3f){spinEndLogged=true;LogGrabCam(cam,head);}
            // Proof in the log: the commit frame (turn starts), the end of the turn (the grab frame), +0.3 s and +1.0 s.
            // count-l3: then every 0.15 s to 1.0 s, so the per-sample eyeToSkull step resolves the motion (no 0.3 -> 1.0 gap).
            if(grabCamLog<GrabCamSchedule.Length&&(grabCamLog==0||grabCamT>=(grabCamLog==1?count.GrabCamTurnSeconds:GrabCamSchedule[grabCamLog]))){grabCamLog++;LogGrabCam(cam,head);}
        }
        // fg-polish (FF fear: hideopen root_slide 0.91 = the 0.43 m seat decay plus FrameClaws easing him 0.5 m on toward the lens): on
        // every grab path his rendered root travels at most RootLeashMeters horizontally from where frame 0 drew it (the seat decay, the
        // claw framing, the door clamp and the reseat all share it); what is left over stays in the seated root. Only KeepSkullOut (the lens
        // inside his skull) may push past it, and that is logged. HC_GRAB_LEASH logs the first hold of a grab.
        float RootLeashMeters=>count.SeatMaxGap-.02f;
        Vector3 leashRoot;bool leashSet,leashLogged,leashPushLogged;float leashHeld;
        void RootLeash()
        {
            if(!leashSet){leashSet=true;leashLogged=false;leashPushLogged=false;leashHeld=0;leashRoot=transform.position;return;}
            var d=transform.position-leashRoot;float lim=RootLeashMeters,m=new Vector2(d.x,d.z).magnitude;
            if(GrabSlideRules.Leash(d.x,d.z,lim,out float bx,out float bz)<=0)return;
            var back=new Vector3(bx,0,bz);transform.position-=back;leashHeld+=back.magnitude;   // the seated root takes it (next frame subtracts seatApplied only)
            if(!leashLogged){leashLogged=true;Debug.Log("HC_GRAB_LEASH mode="+grabMode+" path="+grabCamKind+" t="+presentT.ToString("F2")+" wanted="+m.ToString("F2")+" held="+back.magnitude.ToString("F2")+" leash="+lim.ToString("F2")+" kill="+grabKill);}
        }
        /// <summary>Playtest driver (fg-polish): the root leash's total withheld travel this grab (0 = never reached).</summary>
        public float GrabLeashHeld=>count!=null?leashHeld:0;
        float firstSnapDeg;string firstCutWhy="";
        // fe3-grab: this grab's frame-0 seat cut (side / spin gap over SeatMaxGap); kept until the next grab (the driver reads it).
        bool seatCut;float seatCutGap;
        /// <summary>Playtest driver: the current (or last) grab began with a logged frame-0 seat cut (HC_GRAB_CUT).</summary>
        public bool GrabSeatCut=>count!=null&&seatCut;
        /// <summary>Playtest driver (ff-count): frame 0 of the current (or last) grab was a hard cut (a > 90 deg turn, a skull outside the frame, or a seat cut).</summary>
        public bool GrabFrameCut=>count!=null&&(grabCamSnap||seatCut);
        bool Blocked(Vector3 from,Vector3 to)=>Physics.Linecast(from,to,out var h,FlightMask,QueryTriggerInteraction.Ignore)&&!IgnoreHit(h.collider);
        // Kill presentation: ease him so the nearer claw sits GrabCamClawDistance from the lens; no joint inside CameraClearance.
        bool spinLogged,spinEndLogged;
        static readonly float[] GrabCamSchedule={0,.15f,.3f,.45f,.6f,.75f,.9f,1f};
        float SideSeconds=>1.2f;
        // At 0.1 s the lead claw (the hand furthest from the frame centre) must be on the side the clip came from; else swap 10/11.
        bool sideCheckLogged;
        void SideCheck(Camera cam)
        {
            if(!handL||!handR)return;
            // fd-grab: once the clip data has fixed the mapping (HC_GRAB_SIDE_MAP) this is a log only (the first-frame view now turns
            // toward him, so the lead claw's screen side is no longer a clean test).
            if(sideMapped){var la=cam.WorldToViewportPoint(handL.position);var lb=cam.WorldToViewportPoint(handR.position);Debug.Log("HC_COUNT_GRAB side_check=logged mode="+grabMode+" leadX="+(Mathf.Abs(la.x-.5f)>Mathf.Abs(lb.x-.5f)?la.x:lb.x).ToString("F2")+" swapped="+sideSwapped+" (mapping from HC_GRAB_SIDE_MAP)");return;}
            if(sideChecked)return;sideChecked=true;
            var a=cam.WorldToViewportPoint(handL.position);var b=cam.WorldToViewportPoint(handR.position);
            float lead=Mathf.Abs(a.x-.5f)>Mathf.Abs(b.x-.5f)?a.x:b.x;
            bool cameFromLeft=grabMode==LeftSideMode;bool onLeft=lead<.5f;
            if(cameFromLeft!=onLeft){sideSwapped=!sideSwapped;Debug.Log("HC_COUNT_GRAB side_check=reversed mode="+grabMode+" leadX="+lead.ToString("F2")+" expected="+(cameFromLeft?"left":"right")+" swapped="+sideSwapped+" (10/11 mapping swapped for the rest of the run)");}
            else Debug.Log("HC_COUNT_GRAB side_check=ok mode="+grabMode+" leadX="+lead.ToString("F2")+" side="+(onLeft?"left":"right")+" swapped="+sideSwapped);
        }
        void FrameClaws(Camera cam,float dt)
        {
            if(!handL||!handR)return;
            var eye=cam.transform.position;float near=Mathf.Min(Vector3.Distance(handL.position,eye),Vector3.Distance(handR.position,eye));
            var away=transform.position-eye;away.y=0;if(away.sqrMagnitude<.0001f)return;away.Normalize();
            float move=(near-count.GrabCamClawDistance)*Mathf.Clamp01(dt*6);
            move=Mathf.Clamp(move,-count.GrabCamMaxEase*dt,count.GrabCamMaxEase*dt);   // r12: eased, never a pop
            if(move>0)move=Mathf.Min(move,Mathf.Max(0,ClosestJoint(eye)-count.CameraClearance));   // never closer than the clearance
            if(move>0&&actor.Head)move=Mathf.Min(move,Mathf.Max(0,Vector3.Distance(actor.Head.position,eye)-count.MinSkullDistance));   // fd-grab
            transform.position-=away*move;
        }
        // count18 (r7b fear t=1.00: logged centred, not on screen): the skinned mesh's bounds came from the bind pose, so a
        // far-reaching grab pose was frustum-culled while it was in front of the lens. Bounds now follow the pose.
        void KeepSkinBoundsLive()
        {
            int n=0;foreach(var skin in silhouette.GetComponentsInChildren<SkinnedMeshRenderer>(true)){skin.updateWhenOffscreen=true;n++;}
            Debug.Log("HC_COUNT_SKIN_BOUNDS live renderers="+n);
        }
        Vector3 hideFeet,hideOutward;float hideDoorDistance=.43f;bool doorClampLogged;
        void ClampToDoor()
        {
            var local=transform.position-hideFeet;local.y=0;float along=Vector3.Dot(local,hideOutward);var lateral=local-hideOutward*along;
            float minAlong=hideDoorDistance+count.DoorOutside;bool clamped=false;
            if(along<minAlong){along=minAlong;clamped=true;}
            if(lateral.magnitude>count.DoorLateral){lateral=lateral.normalized*count.DoorLateral;clamped=true;}
            var at=hideFeet+hideOutward*along+lateral;at.y=hideFeet.y;   // feet on her floor, never below it
            if(Mathf.Abs(transform.position.y-at.y)>.001f)clamped=true;
            transform.position=at;
            if(clamped&&!doorClampLogged){doorClampLogged=true;Debug.Log("HC_COUNT_GRABCAM clamp=door along="+along.ToString("F2")+" lateral="+lateral.magnitude.ToString("F2")+" doorDistance="+hideDoorDistance.ToString("F2")+" floor="+hideFeet.y.ToString("F2"));}
        }
        // Visible = any of his renderers' bounds in the view frustum and a clear line from the lens to his skull or chest.
        string VisibleToCamera(Camera cam,Vector3 head)
        {
            var planes=GeometryUtility.CalculateFrustumPlanes(cam);bool inFrustum=false;
            foreach(var r in silhouette.GetComponentsInChildren<Renderer>())if(r.enabled&&r.gameObject.activeInHierarchy&&GeometryUtility.TestPlanesAABB(planes,r.bounds)){inFrustum=true;break;}
            if(!inFrustum)return "False reason=outside frustum";
            var eye=cam.transform.position;var chest=transform.position+Vector3.up*1.0f;
            foreach(var target in new[]{head,chest})
            {
                if(!Physics.Linecast(eye,target,out var hit,FlightMask,QueryTriggerInteraction.Ignore)||hit.collider.transform.IsChildOf(transform))return "True";
            }
            Physics.Linecast(eye,head,out var block,FlightMask,QueryTriggerInteraction.Ignore);
            return "False reason=occluded by "+(block.collider?block.collider.name:"?");
        }
        // r12 (claw_px=629 with no hand on screen): the claw measure is the projected width of each hand's own skinned vertices
        // (vertices weighted mostly to LeftHand/RightHand, baked from the current pose), keeping only points inside the frame and
        // with a clear line from the lens; the larger hand, in pixels of a 2560-wide frame.
        // count-l3 (claw_px_proj was 0 on every sample, "0in"): BakeMesh output was put through the renderer's localToWorldMatrix,
        // which with the import's carrier scale put every point far off screen. The hand vertices are now skinned by hand from the
        // live bones (sum of weight x bone.localToWorldMatrix x bindpose x vertex), which is exactly what the GPU draws and does not
        // depend on BakeMesh's scale convention. dBone= is the distance from those points' centroid to the hand bone (sanity: < 0.4 m).
        static Vector3 SkinVertex(HandVerts hv,Transform[] bones,int idx)
        {
            var w=hv.weights[idx];var v=hv.mesh[idx];var sum=Vector3.zero;float total=0;
            void Add(int bi,float wt){if(wt<=0||bi<0||bi>=bones.Length||!bones[bi])return;sum+=(bones[bi].localToWorldMatrix*hv.bindposes[bi]).MultiplyPoint3x4(v)*wt;total+=wt;}
            Add(w.boneIndex0,w.weight0);Add(w.boneIndex1,w.weight1);Add(w.boneIndex2,w.weight2);Add(w.boneIndex3,w.weight3);
            return total>0?sum/total:v;
        }
        float ProjectedClawPx(Camera cam,out string detail)
        {
            detail="";float best=0;if(handVerts.Count==0){detail="no hand vertices";return 0;}
            var eye=cam.transform.position;
            for(int side=0;side<2;side++)
            {
                float minX=float.MaxValue,maxX=float.MinValue;int sampled=0,inside=0,clear=0,tested=0;string firstReject=null;var centroid=Vector3.zero;
                Transform bone=side==0?handL:handR;
                foreach(var hv in handVerts)
                {
                    if(hv.side!=side||!hv.skin||hv.mesh==null)continue;
                    var bones=hv.skin.bones;int step=Mathf.Max(1,hv.indices.Count/64);
                    for(int i=0;i<hv.indices.Count;i+=step)
                    {
                        int idx=hv.indices[i];if(idx>=hv.mesh.Length)continue;
                        var w=SkinVertex(hv,bones,idx);sampled++;centroid+=w;var v=cam.WorldToViewportPoint(w);
                        if(v.z<cam.nearClipPlane){firstReject??="behind (z "+v.z.ToString("F2")+")";continue;}
                        if(v.x<0||v.x>1||v.y<0||v.y>1){firstReject??="outside ("+v.x.ToString("F2")+","+v.y.ToString("F2")+")";continue;}
                        inside++;
                        if(tested<16&&inside%4==1)
                        {
                            tested++;
                            if(Physics.Linecast(eye,w,out var hit,FlightMask,QueryTriggerInteraction.Ignore)&&!hit.collider.transform.IsChildOf(transform)&&!hit.collider.GetComponentInParent<Investigator>())
                            {firstReject??="occluded by "+hit.collider.name;continue;}
                            clear++;
                        }
                        minX=Mathf.Min(minX,v.x);maxX=Mathf.Max(maxX,v.x);
                    }
                }
                float px=inside>0&&(tested==0||clear>0)?(maxX-minX)*2560f:0;
                float dBone=sampled>0&&bone?Vector3.Distance(centroid/sampled,bone.position):-1;
                detail+=(side==0?"L":"R")+"="+px.ToString("F0")+"("+sampled+"pts,"+inside+"in,"+clear+"/"+tested+"clear,dBone="+dBone.ToString("F2")+(firstReject!=null?",first="+firstReject:"")+") ";
                best=Mathf.Max(best,px);
            }
            return best;
        }
        void LogGrabCam(Camera cam,Vector3 head)
        {
            var v=cam.WorldToViewportPoint(head);
            bool skullClear=!Physics.Linecast(cam.transform.position,head,out var sh,FlightMask,QueryTriggerInteraction.Ignore)||sh.collider.transform.IsChildOf(transform);
            bool central=v.z>cam.nearClipPlane&&v.x>=.2f&&v.x<=.8f&&v.y>=.2f&&v.y<=.8f&&skullClear;
            float claw=ProjectedClawPx(cam,out var hands);
            Debug.Log("HC_COUNT_GRABCAM t="+grabCamT.ToString("F2")+" path="+grabCamKind+" visible="+VisibleToCamera(cam,head)+" pos="+transform.position+" blend="+(grabCamT<count.GrabCamTurnSeconds&&!grabCamSnap?"turning":"done")+" kill="+grabKill+" mode="+actor.Mode+
                " skull_px=("+(v.x*2560).ToString("F0")+","+((1-v.y)*1440).ToString("F0")+") skull_in_central60="+central+" claw_px_proj="+claw.ToString("F0")+" claw_ok="+(claw>=200)+" hands=["+hands.Trim()+"]"+
                " eyeToSkull="+Vector3.Distance(cam.transform.position,head).ToString("F2")+" headAboveEye="+(head.y-cam.transform.position.y).ToString("F2")+
                // ff-count (r14 critic: the samples had no skull world position, so the 0.45-0.97 m root re-seats under a hard cut could not be
                // tested): his skull in world space, how far his root is from where it stood at the commit (begin line) and the seat offset still
                // applied, whether frame 0 was a cut, and his on-screen box (2560 x 1440 scale, like skull_px) for the hide-grab luma test.
                " skull=("+head.x.ToString("F2")+","+head.y.ToString("F2")+","+head.z.ToString("F2")+") rootReseat="+Vector3.Distance(transform.position,grabBeginRoot).ToString("F2")+" seatOffset="+seatApplied.magnitude.ToString("F2")+
                " frameCut="+(grabCamSnap||seatCut)+" countRect="+CountScreenRect(cam));
        }
        Vector3 grabBeginRoot;
        string CountScreenRect(Camera cam)
        {
            float x0=1,y0=1,x1=0,y1=0;bool any=false;
            foreach(var r in silhouette.GetComponentsInChildren<Renderer>())
            {
                if(!r.enabled||!r.gameObject.activeInHierarchy)continue;var b=r.bounds;
                for(int i=0;i<8;i++)
                {
                    var c=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    var v=cam.WorldToViewportPoint(c);if(v.z<=0)continue;any=true;
                    x0=Mathf.Min(x0,v.x);x1=Mathf.Max(x1,v.x);y0=Mathf.Min(y0,v.y);y1=Mathf.Max(y1,v.y);
                }
            }
            if(!any)return "none";
            x0=Mathf.Clamp01(x0);x1=Mathf.Clamp01(x1);y0=Mathf.Clamp01(y0);y1=Mathf.Clamp01(y1);
            return "("+(x0*2560).ToString("F0")+","+((1-y1)*1440).ToString("F0")+","+(x1*2560).ToString("F0")+","+((1-y0)*1440).ToString("F0")+")";
        }
        float ClosestJoint(Vector3 point)
        {
            float best=float.MaxValue;foreach(var t in rigJoints)if(t)best=Mathf.Min(best,Vector3.Distance(t.position,point));return best;
        }
        // Sword hits: recoil, then keep every joint (wing markers included) SwordClearRadius + 0.15 m from the lens.
        Vector3 recoilDir;float recoilLeft,clearUntil=-1;
        void KeepJointsClear()
        {
            if(choreo||staged||capturePresentation||grabCam)return;
            var eye=Game.Player.View.transform.position;
            if(recoilLeft>0)
            {
                float step=Mathf.Min(recoilLeft,count.SwordRecoilDistance/Mathf.Max(.01f,count.SwordRecoilSeconds)*Time.deltaTime);
                var next=transform.position+recoilDir*step;
                if(LegBlocked(transform.position,next,out _))recoilLeft=0;else{transform.position=next;recoilLeft-=step;}
            }
            if(Time.time>clearUntil)return;
            float need=count.SwordClearRadius+.15f,closest=ClosestJoint(eye);
            if(closest>=need)return;
            var away=transform.position-eye;away.y=0;if(away.sqrMagnitude<.0001f)away=-Game.Player.transform.forward;away.Normalize();
            transform.position+=away*(need-closest+.02f);
            Debug.Log("HC_COUNT_RECOIL closest="+closest.ToString("F2")+" pushed="+(need-closest+.02f).ToString("F2"));
        }
        // A hide grab tears the door off: its door, vent and louvre panels hide until the next phase (r11: no kill behind a door).
        readonly List<GameObject> tornDoor=new List<GameObject>();
        void OpenHideDoor(Vector3 exit)
        {
            Interactable spot=null;float best=.6f;
            foreach(var h in Game.World.HidingPlaces){if(h.Kind!=InteractKind.Hide)continue;float d=Vector3.Distance(h.ExitPoint,exit);if(d<best){best=d;spot=h;}}
            if(!spot||!Game.World.Root){Debug.Log("HC_COUNT_GRAB door=kept reason=no hide at the exit");return;}
            foreach(var t in Game.World.Root.GetComponentsInChildren<Transform>())
            {
                if(!t.name.StartsWith(spot.name+" / ",System.StringComparison.Ordinal))continue;
                string part=t.name.Substring(spot.name.Length+3);
                if(part.Contains("door")||part.Contains("vent")||part.Contains("louvre")){t.gameObject.SetActive(false);tornDoor.Add(t.gameObject);}
            }
            Debug.Log("HC_COUNT_GRAB door=torn hide="+spot.name+" panels="+tornDoor.Count);
        }
        void RestoreHideDoor(){foreach(var g in tornDoor)if(g)g.SetActive(true);tornDoor.Clear();}
        Light rimLight;
        float rimScale=1;
        // RimLightScale in hunts and between hunts alike (r10 #11), 1 for staged captures and the capture choreography.
        void SetRim(float scale)
        {
            if(!rimLight||Mathf.Approximately(scale,rimScale))return;
            rimScale=scale;rimLight.enabled=scale>0;rimLight.intensity=count.RimLightIntensity*scale;
            Debug.Log("HC_COUNT_RIM scale="+scale.ToString("F2")+" hunt="+huntOn);
        }

        // ---- Wave F (FEAR_PLAN items 4, 9, 10, 11, 13, 14, 16, 17), castle only ---------------------------------------
        bool huntOn;                       // Investigation.HuntActive as last applied (Phase Hunt or a hostile Return)
        float huntClock,returnHearClock,dropHeight;
        bool sawPlayer;
        int grabMode=-1;
        // Integrator (r11 critic, lost kill): a castle grab under way (contact building toward GrabSeconds). A win checked in the same
        // frame resolves this death first.
        public bool GrabInProgress=>count!=null&&huntOn&&contact>0&&grabMode>=0;
        /// <summary>Scripted runs only (PlaytestDriver): end a grab the driver's invulnerability blocked, so the run does not stall.</summary>
        public void ReleaseGrab(string why){if(count==null)return;contact=0;grabMode=-1;committedUntil=-1;recommitBlockedUntil=Time.time+count.RecommitBlock;if(grabCam)EndGrabCam(why);}
        Vector3? lastSeen;                 // item 14: survives between hunts, but ages out (r10b N8)
        float lastSeenAt;bool lastSeenSighted;
        // r10b N8: heard places go stale after MemoryHeardSeconds, seen ones (incl. a watched hide) after MemorySeenSeconds
        // (Long Night x MemoryLongNightScale). A stale memory is dropped and the hunt opens from the perch instead.
        void Remember(Vector3 at,bool sighted){lastSeen=at;lastSeenAt=Game.Case.Elapsed;lastSeenSighted=sighted;}
        bool MemoryFresh(string when)
        {
            if(!lastSeen.HasValue){Debug.Log("HC_COUNT_MEMORY stale=none when="+when);return false;}
            float age=Game.Case.Elapsed-lastSeenAt;
            float limit=(lastSeenSighted?count.MemorySeenSeconds:count.MemoryHeardSeconds)*(LongNight?count.MemoryLongNightScale:1);
            bool stale=age>limit;
            Debug.Log("HC_COUNT_MEMORY stale="+stale+" age="+age.ToString("F0")+" limit="+limit.ToString("F0")+" kind="+(lastSeenSighted?"seen":"heard")+" at="+lastSeen.Value+" when="+when+(stale?" reason=too old, opening from the perch":""));
            if(stale)lastSeen=null;
            return !stale;
        }
        /// <summary>True while he is diving at the investigator during a hunt (Game: torch fails for the dive).</summary>
        public bool Diving=>count!=null&&huntOn&&!choreo&&flight==Flight.Chase;
        /// <summary>r10 #14: while Diving, the estimated seconds until he is in grab range (distance over speed); otherwise
        /// +Infinity. DiveLastSecond fires once per dive when it drops to CountClaudeProfile.DiveLastSecond (1 s).</summary>
        public float DiveEndsIn=>Diving?diveEndsIn:float.PositiveInfinity;
        public event System.Action DiveLastSecond;
        float diveEndsIn=float.PositiveInfinity;bool diveLastFired;
        /// <summary>Wings actually beating: visible and flying (Glide or Dive pose) faster than 0.4 m/s, in hunts and between
        /// them alike. CastleRun drives the wing loop from this, never from HuntActive (r10 #6).</summary>
        // r10b N3: loudness of the current flight leg (0 silent glide, WingSoftVolume, 1), drawn the same way in hunts and between
        // them; a dive is always 1. CastleRun: wing loop volume = 0.6 x WingLoudness.
        float legLoudness=1;
        public float WingLoudness=>WingsBeating?(flight==Flight.Chase?1:legLoudness):0;
        bool quietHunt,quietSeen,quietHeldLogged;int quietLeg;string quietReason="";
        /// <summary>True in the first QuietSeconds of a no-tell or hostile-return hunt while he has not seen her: no one-shots.</summary>
        bool QuietWindow=>huntOn&&quietHunt&&huntClock<count.QuietStartSeconds&&!quietSeen&&!CanSeePlayer;
        void PickLegLoudness(string leg)
        {
            float r=Random.value;legLoudness=r<1f/3?0:r<2f/3?count.WingSoftVolume:1;
            if(huntOn&&quietHunt&&quietLeg<2)
            {
                quietLeg++;
                if(quietLeg==1){legLoudness=0;Debug.Log("HC_COUNT_LEG first=silent reason="+quietReason+" leg="+leg);}
                else{legLoudness=r<.5f?0:count.WingSoftVolume;Debug.Log("HC_COUNT_LEG second="+(legLoudness>0?"soft":"silent")+" reason="+quietReason+" leg="+leg);}
            }
            Debug.Log("HC_COUNT_WINGS leg="+leg+" loudness="+legLoudness.ToString("F1")+" hunt="+huntOn);
        }
        /// <summary>r10b N3: claw-step volume by distance only (ClawNearVolume at 2 m to ClawFarVolume at 16 m), same curve in
        /// hunts and between them. Integrator: Soundscape's claw cue uses this instead of hunting ? .8 : .05.</summary>
        public float ClawStepVolume=>count==null?.05f:Mathf.Lerp(count.ClawNearVolume,count.ClawFarVolume,Mathf.InverseLerp(2,16,Vector3.Distance(transform.position,Game.Player.transform.position)));
        public bool WingsBeating=>count!=null&&silhouette&&silhouette.gameObject.activeInHierarchy&&flightSpeed>.4f&&actor&&(actor.Mode==Profile.ModeMove||actor.Mode==Profile.ModeReach);
        float flightSpeed;Vector3 lastFramePosition,flightVelocity;
        /// <summary>Beats for logs and CastleRun audio: "near-miss", "hide-open", "gatehouse", "learned", "return-swoop" (integrator: show the hostile-return card on this one).</summary>
        public event System.Action<string> Beat;
        /// <summary>The cupboard door he is breathing at (item 10), for CastleRun's door rattle.</summary>
        public Vector3 NearMissDoor {get;private set;}
        public Vector3 HeadPosition=>actor&&actor.Head?actor.Head.position:transform.position+Vector3.up*2.2f;
        bool seenThisHunt;
        float chaseClosest=99;
        bool grabSurvivedThisHunt;
        void GrabSurvived(string why)
        {
            if(Game.Case.Terminal)return;
            // r10 blind P3 (Dread runaway): at most once per hunt.
            if(grabSurvivedThisHunt){Debug.Log("HC_COUNT_DREAD grab survived ("+why+") +0 reason=already counted this hunt");return;}
            grabSurvivedThisHunt=true;
            Game.Case.AddDread(Game.Case.Rules.DreadGrabSurvived);
            Debug.Log("HC_COUNT_DREAD grab survived ("+why+") +"+Game.Case.Rules.DreadGrabSurvived+" dread="+Game.Case.Dread.ToString("F1"));
        }
        void RaiseBeat(string id){Debug.Log("HC_COUNT_BEAT "+id+" "+DiagnosticState);Beat?.Invoke(id);}

        // Item 13 / owner feedback 13 (hc/fb-voice2): the Count's voice is diegetic only. A line is spoken from his actual head, only
        // while he is in the world (silhouette active), within VoiceMaxDistance (14 m) of her ears and, for the four hunt lines,
        // within VoiceCloseDistance (6 m); at most ONE line per hunt and, outside hunts, at most one per VoiceOffHuntGap (180 s).
        // Soundscape.Speak then plays it on a 3D rolloff (min 1.2, max 14, low-passed behind walls) and logs HC_COUNT_VOICE.
        // Nothing here ever speaks without a location (Soundscape drops such lines).
        int huntLines;float lastOffHuntLine=-999;
        void Voice(string id)=>CountSpeak(id);
        // fd-voice (r13 critic 5, owner rule: nothing is a speaker throughout the house): CountSpeak is the ONLY way a human-voice clip
        // plays in the castle, and it plays from his head (HeadPosition, from="head"). Not the ear-trumpet, not the evidence spot, not behind
        // the player. `asked` is for a line the player explicitly requested (the ear-trumpet reply, the sarcophagus reading): it waives the
        // off-hunt 180 s gap and the first-8-s hold, never the presence, distance, per-hunt or hunt-line-range rules.
        // ff-count (r14 critic: cc_hunt_found was 10 of 27 lines, 37 %): the hunt lines are pools; CountSpeak plays the variant played least
        // this run (GrabMixRules.PickVariant: ties to the dice, never the one just played). Every variant is a voice_cc_* clip from his head.
        static readonly Dictionary<string,string[]> VoicePools=new Dictionary<string,string[]>
        {
            {"cc_hunt_found",new[]{"cc_hunt_found","cc_hunt_found_b","cc_hunt_found_c"}},
            {"cc_hunt_smell",new[]{"cc_hunt_smell","cc_hunt_smell_b"}},
            {"cc_hunt_run",new[]{"cc_hunt_run","cc_hunt_run_b"}},
        };
        readonly Dictionary<string,int[]> voicePlayed=new Dictionary<string,int[]>();readonly Dictionary<string,int> voiceLast=new Dictionary<string,int>();
        // The pool a line id belongs to (its base id), or the id itself.
        static string VoiceBase(string id){foreach(var kv in VoicePools)if(System.Array.IndexOf(kv.Value,id)>=0)return kv.Key;return id;}
        string PickVoice(string id,out int index)
        {
            index=-1;if(!VoicePools.TryGetValue(id,out var pool))return id;
            if(!voicePlayed.TryGetValue(id,out var played)){played=new int[pool.Length];voicePlayed[id]=played;}
            index=GrabMixRules.PickVariant(played,voiceLast.TryGetValue(id,out int last)?last:-1,Random.value);
            return index>=0?pool[index]:id;
        }
        public string CountSpeakBlocker(string id,bool asked=false)
        {
            float now=Time.time;id=VoiceBase(id);
            var view=Game.Player.View.transform.position;var head=HeadPosition;float dist=Vector3.Distance(view,head);
            bool close=id=="cc_hunt_found"||id=="cc_hunt_smell"||id=="cc_hunt_run"||id=="cc_stay";
            if(!silhouette||!silhouette.gameObject.activeInHierarchy)return "not in the world";
            if(dist>count.VoiceMaxDistance)return "beyond "+count.VoiceMaxDistance.ToString("F0")+" m ("+dist.ToString("F1")+")";
            if(close&&dist>count.VoiceCloseDistance)return "beyond "+count.VoiceCloseDistance.ToString("F0")+" m for a hunt line ("+dist.ToString("F1")+")";
            if(huntOn&&huntLines>=1)return "one line per hunt";
            if(!asked&&!huntOn&&now-lastOffHuntLine<count.VoiceOffHuntGap)return "off-hunt gap "+(count.VoiceOffHuntGap-(now-lastOffHuntLine)).ToString("F0")+" s left";
            if(!asked&&huntOn&&huntClock<8&&!CanSeePlayer&&id!="cc_hunt_found"&&id!="cc_hunt_smell")return "first 8 s of a hunt, not seen";
            return null;
        }
        public bool CountSpeak(string id,bool asked=false)
        {
            string why=CountSpeakBlocker(id,asked);
            if(why!=null){Debug.Log("HC_VOICE_GATE held id="+id+" reason="+why+" t="+huntClock.ToString("F1"));return false;}
            string baseId=VoiceBase(id);string line=PickVoice(baseId,out int vi);
            if(!Game.Sound.Speak(line,HeadPosition,"head"))return false;
            if(vi>=0){voicePlayed[baseId][vi]++;voiceLast[baseId]=vi;id=line;}
            if(huntOn)huntLines++;else lastOffHuntLine=Time.time;
            Debug.Log("HC_VOICE_GATE spoke id="+id+" from=head asked="+asked+" huntLines="+huntLines+" t="+huntClock.ToString("F1")+" "+DiagnosticState);
            return true;
        }
        bool bargainSpoken;
        /// <summary>The ear-trumpet reply (fd-voice): at most once per run, and only if he is in range of her ears; otherwise the trumpet's non-verbal texture is all there is.</summary>
        public bool CountBargain()
        {
            if(bargainSpoken)return false;
            if(CountSpeakBlocker("cc_bargain",true)!=null)return false;
            if(!CountSpeak("cc_bargain",true))return false;
            bargainSpoken=true;return true;
        }

        // ---- Item 17: grab variants -----------------------------------------------------------------------------------
        // Wave I grab choice (r10 blind: the grab shown was Mode 6 hovering 3.9 m up). Height = his origin above her feet, the larger of
        // now and the recent decaying approach height. Hide entrance -> GrabDrop. She is crouched, or he comes in under
        // GrabCrawlBelow -> GrabCrawl. Mode 6 wing-wrap only within GrabWrapDistance at <= GrabWrapMaxHeight. Higher -> GrabDrop.
        // A missing clip falls through to the next choice, Mode 6 last.
        int ChooseGrab(bool hideEntrance,out string why,out float height)
        {
            var p=Game.Player;var feet=p.transform.position;height=Mathf.Max(dropHeight,transform.position.y-feet.y);
            var flat=feet-transform.position;flat.y=0;float distance=flat.magnitude;
            bool drop=count.ModeGrabDrop>=0&&HasModeState(count.ModeGrabDrop,"GrabDrop");
            bool crawl=count.ModeGrabCrawl>=0&&HasModeState(count.ModeGrabCrawl,"GrabCrawl");
            // count-l3: a hide is taken with GrabCrawl from the door (a drop there starts 4.7 m up over a crawling Count: the r13 pop).
            if(hideEntrance){if(crawl){why="hide entrance (crawl from the door)";return count.ModeGrabCrawl;}if(drop){why="hide entrance, GrabCrawl missing";return count.ModeGrabDrop;}}
            if(p.Crouched||height<count.GrabCrawlBelow)
            {
                if(crawl){why=p.Crouched?"she is crouched":"came in low ("+height.ToString("F1")+" m)";return count.ModeGrabCrawl;}
                if(drop&&height>=count.GrabCrawlBelow){why="crouched, GrabCrawl missing";return count.ModeGrabDrop;}
            }
            if(height<=count.GrabWrapMaxHeight&&distance<=count.GrabWrapDistance){why="wing-wrap in reach ("+distance.ToString("F2")+" m)";return count.ModeGrab;}
            if(drop){why="from above ("+height.ToString("F1")+" m)";return count.ModeGrabDrop;}
            if(crawl){why="GrabDrop missing";return count.ModeGrabCrawl;}
            why="no GrabDrop/GrabCrawl state, wing-wrap fallback";return count.ModeGrab;
        }
        readonly Dictionary<int,bool> modeStates=new Dictionary<int,bool>();
        // F5 adds Count_GrabDrop / Count_GrabCrawl (Animator Mode 8 / 9). Missing => false => the Mode 6 wing-wrap.
        bool HasModeState(int mode,string name)
        {
            if(modeStates.TryGetValue(mode,out var known))return known;
            var animator=actor?actor.Animator:null;var controller=animator?animator.runtimeAnimatorController:null;
            if(!controller)return false;
            bool found=false;
            foreach(var clip in controller.animationClips)if(clip&&clip.name.IndexOf(name,System.StringComparison.OrdinalIgnoreCase)>=0){found=true;break;}
            if(!found&&animator.isActiveAndEnabled)found=animator.HasState(0,Animator.StringToHash(name))||animator.HasState(0,Animator.StringToHash("Base Layer."+name));
            else if(!found)return false;   // inactive animator: decide later
            modeStates[mode]=found;Debug.Log("HC_COUNT_GRAB_STATE mode="+mode+" "+name+"="+found);
            return found;
        }

        // ---- Item 9: perch sightings between hunts --------------------------------------------------------------------
        float unobserved;
        bool reperchRolled;
        bool BetweenAllowed
        {
            get
            {
                var phase=Game.Case.Phase;
                return count.PerchBetweenHunts&&!CaptureMode.Requested&&Game.Started&&!Game.Case.Terminal&&Game.World.PerchPoints.Count>0&&
                       (phase==Phase.Survey||phase==Phase.Disturbance||phase==Phase.Warning||phase==Phase.Recovery||phase==Phase.Containment||phase==Phase.Return);
            }
        }
        float BetweenPerchSeconds()
        {
            var rules=Game.Case.Rules;
            float min=rules!=null&&rules.PerchMinSeconds>0?rules.PerchMinSeconds:count.PerchMinSeconds;
            float max=rules!=null&&rules.PerchMaxSeconds>0?rules.PerchMaxSeconds:count.PerchMaxSeconds;
            return Random.Range(min,Mathf.Max(min,max));
        }
        void CountBetween(float dt)
        {
            CanSeePlayer=false;contact=0;SetRim(count.RimLightScale);
            bool seen=ObservedByPlayer();unobserved=seen?0:unobserved+dt;
            if(gateStage>0){GatehouseTick(dt);return;}
            if(GatehouseDue(dt)&&unobserved>=count.UnobservedSeconds){BeginGatehouse();return;}
            switch(flight)
            {
                case Flight.Perched:
                {
                    transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.Euler(0,perchYaw,0),90*dt);
                    actor.Pose(Profile.ModeIdle);perchTimer-=dt;
                    if(ShowcaseTick(dt,seen))break;
                    // r10 #11: one perch move in five is a visible flight (even while she watches; nothing can kill), so seeing him
                    // move does not mean "hunt". The rest keep the SCP-173 rule: he moves only while nobody is looking.
                    if(perchTimer<=0&&!reperchRolled)
                    {
                        reperchRolled=true;float roll=Random.value;
                        if(roll<count.ObservedReperchChance)
                        {
                            int to=ChoosePerch(perchIndex,false);
                            if(to>=0&&GoToPerch(to,false)){Debug.Log("HC_COUNT_PERCH flight observed="+seen+" roll="+roll.ToString("F2")+" from="+perchIndex+" to="+to+" "+DiagnosticState);break;}
                            Debug.Log("HC_COUNT_PERCH flight rejected reason="+(to<0?"no perch keeps the distance rules":"no route to perch "+to));
                        }
                    }
                    if((perchTimer<=0||!PerchFarEnough(perchIndex))&&unobserved>=count.UnobservedSeconds)MoveUnseen();
                    break;
                }
                case Flight.Travel:
                case Flight.Retreat:
                    // r10b N3: a between-hunt flight is always flown to the end (no silent teleport mid-flight), so the wings
                    // fade with his real landing, after a hunt as much as before one.
                    Fly(dt,Profile.ModeMove);
                    if(routeAt>=route.Count)
                    {
                        if(pendingPerch>=0)
                        {
                            SetPerch(pendingPerch,BetweenPerchSeconds());Debug.Log("HC_COUNT_PERCH landed perch="+perchIndex+" "+DiagnosticState);
                            if(showcaseFlying){showcaseFlying=false;Debug.Log("HC_COUNT_PERCH showcase landed perch="+perchIndex+" seen="+ObservedByPlayer()+" done="+showcaseDone);}
                        }
                        else flight=Flight.Land;
                    }
                    break;
                case Flight.Land:
                    if(GroundUnder(transform.position,out var ground))transform.position=Vector3.MoveTowards(transform.position,ground,2.5f*dt);
                    actor.Pose(Profile.ModeSearch);
                    if(unobserved>=count.UnobservedSeconds)TeleportPerch(-1);
                    break;
                default:   // Away, or a hunt state left over: hidden (or held still) until he is unobserved and a perch is free
                    if(unobserved>=count.UnobservedSeconds)TeleportPerch(-1);
                    break;
            }
        }
        // ---- Wave FD: one observed relocation per run (r13 critic: observed=True was 0 in every run) ----------------------------------
        bool showcaseDone,showcaseFlying;float showcaseWait,showcaseCheck;int showcaseTries;
        bool ShowcaseArmed
        {
            get
            {
                var phase=Game.Case.Phase;
                return !showcaseDone&&(phase==Phase.Survey||phase==Phase.Disturbance||phase==Phase.Recovery)&&Game.Case.Elapsed>=count.ShowcaseAfterSeconds
                       &&!Game.Player.Hidden&&!Game.Modal&&perchIndex>=0;
            }
        }
        // Called every frame he sits on a perch between hunts. Returns true when it started a flight.
        bool ShowcaseTick(float dt,bool seen)
        {
            if(!ShowcaseArmed)return false;
            showcaseWait+=dt;showcaseCheck-=dt;
            bool timeout=showcaseWait>=count.ShowcaseWaitSeconds;
            if(showcaseCheck>0&&!timeout)return false;
            showcaseCheck=count.ShowcaseCheckSeconds;
            string view=null;int to=-1;
            if(seen){to=ChoosePerch(perchIndex,false);if(to>=0)view="departure";}
            if(view==null){to=VisibleArrivalPerch(perchIndex);if(to>=0)view="arrival";}
            if(view==null&&!timeout)return false;
            if(view==null)to=ChoosePerch(perchIndex,false);
            int from=perchIndex;showcaseTries++;
            if(to<0||!GoToPerch(to,false))
            {
                Debug.Log("HC_COUNT_PERCH showcase rejected reason="+(to<0?"no perch keeps the distance rules":"no route to perch "+to)+" view="+(view??"none")+" waited="+showcaseWait.ToString("F0"));
                if(timeout)showcaseWait=0;
                return false;
            }
            bool observed=view!=null;
            if(observed)showcaseDone=true;
            showcaseFlying=true;float waited=showcaseWait;showcaseWait=0;
            Debug.Log("HC_COUNT_PERCH flight observed="+observed+" showcase="+(observed?"True":"timeout")+" view="+(view??"none")+" waited="+waited.ToString("F0")+" tries="+showcaseTries+" from="+from+" to="+pendingPerch+" "+DiagnosticState);
            return true;
        }
        // A perch (not the current one, keeping the distance rules) whose head or chest point is inside the central viewport and in clear
        // line of sight from the camera: he would land in her view. Random among the visible ones; -1 when none.
        int VisibleArrivalPerch(int exclude)
        {
            var perches=Game.World.PerchPoints;int pick=-1,seenCount=0;
            for(int i=0;i<perches.Count;i++)
            {
                if(i==exclude||!PerchFarEnough(i))continue;
                var feet=PerchFeet(i);
                if(!PointInView(feet+Vector3.up*(count.PerchEyeHeight+.35f))&&!PointInView(feet+Vector3.up*.8f))continue;
                seenCount++;if(Random.Range(0,seenCount)==0)pick=i;
            }
            return pick;
        }
        bool PointInView(Vector3 p)
        {
            var cam=Game.Player.View;var v=cam.WorldToViewportPoint(p);
            if(v.z<=.5f||v.z>40||v.x<.08f||v.x>.92f||v.y<.08f||v.y>.92f)return false;
            return !Occluded(cam.transform.position,p);
        }
        // r10b N3: an unseen perch move is a real (audible) flight one time in three (AudibleReperchChance), otherwise the silent
        // move. Either way the destination is out of her sight when it is chosen.
        void MoveUnseen()
        {
            float roll=Random.value;
            if(roll<count.AudibleReperchChance)
            {
                int to=ChoosePerch(perchIndex,true);
                if(to>=0&&GoToPerch(to,false)){unobserved=0;Debug.Log("HC_COUNT_PERCH flight unseen=True audible=True roll="+roll.ToString("F2")+" from="+perchIndex+" to="+to+" wings="+legLoudness.ToString("F1")+" "+DiagnosticState);return;}
                Debug.Log("HC_COUNT_PERCH flight unseen rejected reason="+(to<0?"no hidden perch keeps the distance rules":"no route to perch "+to)+", moving silently");
            }
            TeleportPerch(perchIndex);
        }
        // Place him on a perch that is out of her sight. Called only while he is unobserved, so the move is never seen.
        void TeleportPerch(int exclude)
        {
            unobserved=0;int i=ChoosePerch(exclude,true);
            if(i<0)
            {
                if(flight==Flight.Perched&&perchIndex>=0&&PerchFarEnough(perchIndex)){perchTimer=1;return;}
                if(flight!=Flight.Away)Debug.Log("HC_COUNT_PERCH abort=hide reason=no perch free out of her sight "+DiagnosticState);
                flight=Flight.Away;perchIndex=-1;route.Clear();silhouette.gameObject.SetActive(false);return;
            }
            silhouette.gameObject.SetActive(true);route.Clear();SetPerch(i,BetweenPerchSeconds());
            var cam=Game.Player.View.transform;var head=HeadPosition;
            Debug.Log("HC_COUNT_PERCH moved perch="+i+" feet="+transform.position+" playerDist="+Vector3.Distance(transform.position,Game.Player.transform.position).ToString("F1")+
                " viewAngle="+Vector3.Angle(cam.forward,head-cam.position).ToString("F0")+" occluded="+Occluded(cam.position,head)+" watches="+PerchWatchFloor(i)+" favourite="+FavouriteFloor()+" phase="+Game.Case.Phase);
        }
        // Distance rules only (used to keep a perch he is already on): >= PerchMinPlayerDistance and not on her floor within 6 m.
        bool PerchFarEnough(int i)
        {
            if(i<0||i>=Game.World.PerchPoints.Count)return false;
            var feet=PerchFeet(i);var player=Game.Player.transform.position;float d=Vector3.Distance(feet,player);
            bool sameFloor=Mathf.Approximately(World.FloorBelow(feet.y+.1f),World.FloorBelow(player.y+.1f));
            return d>=count.PerchMinPlayerDistance&&!(sameFloor&&d<count.PerchSameFloorExclusion);
        }
        string PerchTooClose(int i)
        {
            var feet=PerchFeet(i);var player=Game.Player.transform.position;float d=Vector3.Distance(feet,player);
            return d<count.PerchMinPlayerDistance?"near "+d.ToString("F1")+"m":"same-floor";
        }
        // Outside her view cone (more than SightAngle off the camera forward) or occluded from the camera.
        bool PerchHidden(int i)
        {
            var cam=Game.Player.View.transform;var feet=PerchFeet(i);
            var head=feet+Vector3.up*(count.PerchEyeHeight+.35f);var chest=feet+Vector3.up*.8f;
            if(Vector3.Angle(cam.forward,head-cam.position)>Profile.SightAngle&&Vector3.Angle(cam.forward,chest-cam.position)>Profile.SightAngle)return true;
            return Occluded(cam.position,head)&&Occluded(cam.position,chest);
        }
        // Weighted random over allowed perches; item 14: a perch watching the floor where she spends most time weighs more.
        int ChoosePerch(int exclude,bool mustBeHidden)
        {
            var perches=Game.World.PerchPoints;int fav=FavouriteFloor();float share=FavouriteShare();
            var weights=new float[perches.Count];float total=0;var why=new System.Text.StringBuilder();
            for(int i=0;i<perches.Count;i++)
            {
                string reject=i==exclude?"current":!PerchFarEnough(i)?PerchTooClose(i):mustBeHidden&&!PerchHidden(i)?"in-view":null;
                if(reject!=null){why.Append(i).Append(":x(").Append(reject).Append(") ");continue;}
                float w=1+(fav>=0&&PerchWatchFloor(i)==fav?count.PerchFloorWeight*share:0);
                weights[i]=w;total+=w;why.Append(i).Append(':').Append(w.ToString("F1")).Append(' ');
            }
            if(total<=0)return -1;
            float pick=Random.value*total;int chosen=-1;
            for(int i=0;i<perches.Count;i++){if(weights[i]<=0)continue;chosen=i;pick-=weights[i];if(pick<=0)break;}
            Debug.Log("HC_COUNT_PERCH_CHOICE reason=x(current|near|same-floor|in-view) weights=["+why.ToString().Trim()+"] favouriteFloor="+fav+" share="+share.ToString("F2")+" chosen="+chosen);
            return chosen;
        }
        int FavouriteFloor()=>Game.Castle?Game.Castle.FavouriteFloor:-1;
        float FavouriteShare()=>Game.Castle?Game.Castle.FavouriteFloorShare:0;
        readonly Dictionary<int,Vector3> perchFeet=new Dictionary<int,Vector3>();
        readonly Dictionary<int,int> perchWatch=new Dictionary<int,int>();
        Vector3 PerchFeet(int i){if(!perchFeet.TryGetValue(i,out var feet)){Ledge(i,out feet,out _);perchFeet[i]=feet;}return feet;}
        // The floor a perch looks down on: the ground 2.5 m beyond its ledge in its open facing (a balcony watches the hall).
        int PerchWatchFloor(int i)
        {
            if(perchWatch.TryGetValue(i,out var floor))return floor;
            Ledge(i,out var feet,out var yaw);var probe=feet+Quaternion.Euler(0,yaw,0)*Vector3.forward*2.5f+Vector3.up*.5f;
            float y=Physics.Raycast(probe,Vector3.down,out var hit,12,FlightMask,QueryTriggerInteraction.Ignore)?hit.point.y:feet.y;
            floor=CastleRun.FloorIndex(y);perchWatch[i]=floor;return floor;
        }
        // The camera sees him: a head, chest or feet point inside the viewport and not occluded.
        bool ObservedByPlayer()
        {
            if(!silhouette.gameObject.activeInHierarchy)return false;
            var cam=Game.Player.View;var eye=cam.transform.position;
            var points=new[]{HeadPosition,transform.position+Vector3.up*1.2f,transform.position+Vector3.up*.3f};
            foreach(var p in points)
            {
                var v=cam.WorldToViewportPoint(p);
                if(v.z<=0||v.z>45||v.x<-.05f||v.x>1.05f||v.y<-.05f||v.y>1.05f)continue;
                if(!Occluded(eye,p))return true;
            }
            return false;
        }
        bool Occluded(Vector3 from,Vector3 to)
        {
            if(!Physics.Linecast(from,to,out var hit,FlightMask,QueryTriggerInteraction.Ignore))return false;
            return !hit.collider.transform.IsChildOf(transform)&&!hit.collider.GetComponentInParent<Investigator>();
        }

        // ---- Item 11: the gatehouse is not safe (once per run) ---------------------------------------------------------
        float safeClock,gateTimer;
        bool gatehouseDone,gateHover,gateGround;
        float gateSeen;
        int gateStage;
        Vector3 gateSpot,gateFrom;
        bool GatehouseDue(float dt)
        {
            if(gatehouseDone||!Game.Case.Survived){safeClock=0;return false;}
            safeClock=Game.Player.InVan?safeClock+dt:0;
            return safeClock>=count.GatehouseSafeSeconds;
        }
        void BeginGatehouse()
        {
            gatehouseDone=true;gateStage=1;gateTimer=0;gateSeen=0;route.Clear();legLoudness=1;
            gateSpot=VisibleGateSpot(out gateHover,out gateGround);var player=Game.Player.transform.position;
            // He drops in from above a battlement; at the gate arch or under a ceiling he swoops in from beyond it, away from her.
            var away=gateSpot-player;away.y=0;away=away.sqrMagnitude>.01f?away.normalized:Vector3.forward;
            gateFrom=gateHover||gateGround?gateSpot+away*2.5f+Vector3.up*(gateGround?1.4f:.6f):gateSpot+Vector3.up*3f;
            if(!Clear(gateFrom,gateSpot))gateFrom=Vector3.Lerp(gateSpot,gateFrom,.4f);
            transform.SetPositionAndRotation(gateFrom,Quaternion.LookRotation(-away));silhouette.gameObject.SetActive(true);
            Debug.Log("HC_COUNT_GATEHOUSE begin spot="+gateSpot+" from="+gateFrom+" hover="+gateHover+" safeSeconds="+safeClock.ToString("F1")+" "+DiagnosticState);
        }
        void GatehouseTick(float dt)
        {
            gateTimer+=dt;var player=Game.Player.transform.position;
            var face=player-transform.position;face.y=0;
            if(face.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(face),240*dt);
            if(gateStage==1)
            {
                float k=Mathf.SmoothStep(0,1,gateTimer/count.GatehouseArriveSeconds);
                transform.position=Vector3.Lerp(gateFrom,gateSpot,k);actor.Pose(Profile.ModeMove,2.5f);
                if(k>=1){gateStage=2;gateTimer=0;Voice("cc_stay");RaiseBeat("gatehouse");Debug.Log("HC_COUNT_GATEHOUSE landed "+DiagnosticState);}
            }
            else if(gateStage==2)
            {
                actor.Pose(gateHover?Profile.ModeMove:gateGround?Profile.ModeSearch:Profile.ModeIdle,1.2f);
                gateSeen=ObservedByPlayer()?gateSeen+dt:gateSeen;
                bool done=gateTimer>=count.GatehouseHoldSeconds&&(!Game.Player.InVan||gateSeen>=count.GatehouseSeenSeconds||gateTimer>=count.GatehouseMaxHoldSeconds);
                if(done){gateStage=3;gateTimer=0;Debug.Log("HC_COUNT_GATEHOUSE leaving "+DiagnosticState);}
            }
            else
            {
                float k=Mathf.SmoothStep(0,1,gateTimer/count.GatehouseArriveSeconds);
                transform.position=Vector3.Lerp(gateSpot,gateFrom,k);actor.Pose(Profile.ModeMove,2.5f);
                if(k>=1){gateStage=0;flight=Flight.Away;perchIndex=-1;silhouette.gameObject.SetActive(false);unobserved=0;Debug.Log("HC_COUNT_GATEHOUSE gone "+DiagnosticState);}
            }
        }
        // r11 (the visit was invisible from inside the gatehouse): candidates are points along the gate-wall top around F3's
        // battlement point, the gate arch (inside and outside) and the courtyard doorway; each is scored by how many of three body
        // points (0.4, 1.3, 2.2 m) her eye can see by line of sight. Best wins (battlement first on ties); none visible -> gate arch.
        Vector3 VisibleGateSpot(out bool hover,out bool ground)
        {
            hover=false;ground=false;var root=Game.World.Root;var eye=Game.Player.View.transform.position;
            var spots=Game.World.Castle;var list=new List<(Vector3 at,string kind)>();
            var right=root?root.right:Vector3.right;
            if(spots!=null&&spots.GatehouseBattlement!=Vector3.zero)
                foreach(float dx in new[]{0f,-1.5f,1.5f,-3f,3f,-4.5f,4.5f,-6f})list.Add((spots.GatehouseBattlement+right*dx,"battlement"));
            Vector3 arch=root?root.TransformPoint(new Vector3(0,0,0)):Vector3.zero;
            foreach(var t in root?root.GetComponentsInChildren<Transform>(true):new Transform[0])if(t.name.EndsWith("GateArch",System.StringComparison.Ordinal)){arch=t.position;break;}
            var inward=root?root.forward:Vector3.forward;
            list.Add((arch+inward*.8f,"arch-inside"));list.Add((arch+inward*.8f+right*1.2f,"arch-inside"));list.Add((arch+inward*.8f-right*1.2f,"arch-inside"));
            list.Add((arch-inward*1.4f,"arch-outside"));
            if(spots!=null){var g=spots.Gatehouse;list.Add((new Vector3(g.center.x,g.min.y,g.max.z)+inward*1.2f,"courtyard-door"));}
            int best=-1,bestSeen=-1;var log=new System.Text.StringBuilder();
            for(int i=0;i<list.Count;i++)
            {
                int seen=0;foreach(float h in new[]{.4f,1.3f,2.2f}){var p=list[i].at+Vector3.up*h;if(!Physics.Linecast(eye,p,FlightMask,QueryTriggerInteraction.Ignore))seen++;}
                log.Append(list[i].kind).Append(':').Append(seen).Append(' ');
                if(seen>bestSeen){bestSeen=seen;best=i;}
            }
            Vector3 spot;string kind;
            if(best>=0&&bestSeen>0){spot=list[best].at;kind=list[best].kind;}
            else{spot=arch+inward*.8f;kind="arch-inside (fallback, nothing visible)";}
            ground=!kind.StartsWith("battlement",System.StringComparison.Ordinal);
            if(ground&&GroundUnder(spot,out var floor))spot=floor;
            Debug.Log("HC_COUNT_GATEHOUSE visible="+Mathf.Max(0,bestSeen)+"/3 kind="+kind+" spot="+spot+" eye="+eye+" candidates=["+log.ToString().Trim()+"]");
            return spot;
        }
        // ---- Item 10 near-miss, and (wave G, r10 #16/#17/#28) the hide he opens --------------------------------------------
        // One sequence for both: fly to a landing point in front of the cupboard, crawl (Mode 7) to NearMissDoorDistance from
        // the door, hold, then either leave (near-miss) or open it: the grab at the hide entrance (HC_COUNT_GRAB hideEntrance=True).
        // Witnessed (he saw her go in, from any state): a WitnessedPauseSeconds pause, the door rattle, then the drop-grab.
        // Near-miss: breathes and rattles for NearMissHoldSeconds; at Dread >= NearMissOpenDread or in Long Night, one in four opens
        // with the crawl grab.
        bool nearMissRolled,pendingWitnessed,visitWitnessed,visitOpens;int nearMissAttemptFails;
        int nearMissStage;
        float nearMissHold,visitGrab;
        Vector3 nearMissCrawl,nearMissOutward,visitStand;
        bool LongNight=>Game.Case.Rules!=null&&Game.Case.Rules.Name=="Long Night";
        // r12: within-match learning; survives between hunts (a new Game after a death starts fresh).
        // Wave FD (r13 critic): the learned-hide open is NearMissDice (a per-visit roll with a soft pity floor), not a bag of three.
        Interactable learnedHide,bagHide;readonly NearMissDice openDice=new NearMissDice();
        Interactable HideSpotAt(Vector3 exit)
        {
            Interactable best=null;float d=.6f;
            foreach(var h in Game.World.HidingPlaces){if(!h||h.Kind!=InteractKind.Hide)continue;float e=Vector3.Distance(h.ExitPoint,exit);if(e<d){d=e;best=h;}}
            return best;
        }
        // Wave L (integrator): the base curve is F1's HuntRules.NearMissOpenChance (2 % at 0, 5 % at 40, 25 % at 80, 40 % at 100);
        // the survived-hunt and Long Night floors and the learned-hide multiplier are applied on top by the caller.
        float OpenChanceForDread(float dread)=>Game.Case.Rules.NearMissOpenChance(dread);
        /// <summary>False when the hide he is at has no door panel (CastleRun then skips the rattle).</summary>
        public bool NearMissHasDoor {get;private set;}=true;
        readonly Dictionary<Interactable,bool> hideDoors=new Dictionary<Interactable,bool>();
        bool HideHasDoor(Interactable spot)
        {
            if(hideDoors.TryGetValue(spot,out var has))return has;
            has=false;var root=Game.World.Root?Game.World.Root:spot.transform.root;
            foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==spot.name+" / lower door"||t.name.StartsWith(spot.name+" / ",System.StringComparison.Ordinal)&&t.name.EndsWith("door",System.StringComparison.Ordinal)){has=true;break;}
            hideDoors[spot]=has;return has;
        }
        void BeginHideVisit(bool witnessed)
        {
            string tag=witnessed?"HC_COUNT_WITNESSED":"HC_COUNT_NEARMISS";
            Interactable spot=null;float best=float.MaxValue;
            var exit=Game.Player.HideExit;var inside=witnessed?knownHide:Game.Player.transform.position;
            foreach(var h in Game.World.HidingPlaces)
            {
                if(h.Kind!=InteractKind.Hide)continue;
                float d=witnessed?Vector3.Distance(h.HidePoint,inside):Vector3.Distance(h.ExitPoint,exit);
                if(d<best){best=d;spot=h;}
            }
            if(!spot||best>.5f)
            {
                // No matching cupboard (a hide this code does not know): a witnessed hide still costs her, he dives at the place.
                Debug.Log(tag+" abort=spot reason=no hide within 0.5 m (best "+best.ToString("F2")+")"+(witnessed?", diving at the hide point":"")+" "+DiagnosticState);
                if(witnessed)BeginChase();
                return;
            }
            // r10b N2 (S3): a hide without a door (no "<name> / lower door" panel) gets no rattle; the sequence still runs.
            NearMissHasDoor=HideHasDoor(spot);
            if(!NearMissHasDoor)Debug.Log(tag+" door=none reason=hide "+spot.name+" has no door panel, no rattle");
            var outward=Vector3.ProjectOnPlane(spot.ExitPoint-spot.HidePoint,Vector3.up);if(outward.sqrMagnitude<.01f)outward=-spot.transform.forward;outward.Normalize();
            float floorY=GroundUnder(spot.ExitPoint,out var g)?g.y:spot.ExitPoint.y;
            var door=spot.transform.position;nearMissOutward=outward;
            NearMissDoor=new Vector3(door.x,floorY+1,door.z);
            nearMissCrawl=new Vector3(door.x,floorY,door.z)+outward*count.NearMissDoorDistance;
            // r10 #31: try the landing in front of the door, then closer, then the nearest NavMesh point to the door; log each miss.
            Vector3 landing=nearMissCrawl;bool routed=false;
            foreach(float back in new[]{count.NearMissCrawlDistance,1.4f,.7f,0f})
            {
                landing=nearMissCrawl+outward*back;
                if(PlanRoute(landing,false)){routed=true;break;}
                Debug.Log(tag+" landing rejected at "+landing+" reason=no route ("+back.ToString("F1")+" m out)");
            }
            if(!routed&&NavMesh.SamplePosition(nearMissCrawl,out var near,3,NavMesh.AllAreas))
            {
                landing=near.position;routed=PlanRoute(landing,false);
                if(!routed)Debug.Log(tag+" landing rejected at "+landing+" reason=no route (nearest NavMesh point)");
            }
            if(!routed)
            {
                // r10b N2: never a free cancel. Witnessed: he dives straight at the entrance (CountChase's entrance grab). Near-miss:
                // there is nothing he knows, so he prowls where he is.
                Debug.Log(tag+" abort=route reason=no landing reachable; "+(witnessed?"diving at the entrance instead":"prowling where he is")+" "+DiagnosticState);
                if(witnessed)BeginChase();else BeginLand();
                return;
            }
            visitWitnessed=witnessed;
            // Watched: always opens, at any Dread. Near-miss (r12): the open chance follows Dread from 40 (5 % at 40, 25 % at 80, 40 % at
            // 100), never under 1 in 10 once she has survived a hunt nor under 1 in 4 in Long Night; x2 (capped) at the hide he last
            // found her in or near (within-match learning).
            float dread=Game.Case.Dread;bool learned=!witnessed&&learnedHide&&spot==learnedHide;
            float curve=OpenChanceForDread(dread);
            float chance=witnessed?1:Mathf.Max(curve,Game.Case.Survived?count.NearMissOpenChanceLow:0,LongNight?count.NearMissOpenChance:0);
            if(learned)chance=Mathf.Min(count.LearnedHideOpenCap,chance*count.LearnedHideOpenMultiplier);
            bool eligible=witnessed||chance>0;
            float roll=witnessed?0:Random.value;
            visitOpens=witnessed||roll<chance;
            // count-l4 (siege at Dread 60: 5 learned-hide visits, 0 opens): at Dread >= BagDread the hide he keeps finding her in
            // opens on its own dice. Wave FD (r13 critic: the bag of three made the two visits after an open guaranteed rattles): a per-visit
            // roll, 0.24, 0.45 on the visit right after an open, a soft pity floor only after 4 closed visits (NearMissDice, long-run about
            // 1 in 3). The dice start fresh when he switches to another hide. Other hides and lower Dread keep the per-visit curve roll.
            string bagNote="";float p=witnessed?1:chance;
            if(!witnessed&&dread>=count.BagDread&&(learned||learnedHide==null||learnedHide==spot||bagHide==null))
            {
                if(bagHide!=spot){bagHide=spot;openDice.Reset();}
                int streak=openDice.ClosedStreak;bool after=openDice.LastOpened;float u=Random.value;
                visitOpens=openDice.Roll(u,out p);
                bagNote=" dice=learned streak="+streak+" afterOpen="+after;
                Debug.Log("HC_COUNT_NEARMISS openroll p="+p.ToString("F2")+" roll="+u.ToString("F2")+" open="+visitOpens+" streak="+streak+" afterOpen="+after+" visits="+openDice.Visits+" opens="+openDice.Opens+" hide="+spot.name+" dread="+dread.ToString("F0"));
            }
            // He has now found her in or near this hide: it is the one he checks first next time.
            if(learnedHide!=spot)Debug.Log("HC_COUNT_LEARNED hide="+spot.name+" reason="+(witnessed?"watched her go in":"found her in it"));
            learnedHide=spot;
            flight=Flight.NearMiss;nearMissStage=1;pendingPerch=-1;travelToPerch=false;Memory.Clear();attackAttempted=true;visitGrab=0;
            Debug.Log(tag+" begin hide="+spot.name+" door="+NearMissDoor+" crawlTo="+nearMissCrawl+" landing="+landing+
                (witnessed?" open=True p=1.00 reason=watched (any Dread)":" open="+visitOpens+" p="+p.ToString("F2")+bagNote+" openRoll="+roll.ToString("F2")+" chance="+chance.ToString("F2")+" curve="+curve.ToString("F2")+" learnedHide="+learned+" eligible="+eligible+(eligible?"":" reason=first hunt, below Dread 40")+" dread="+dread.ToString("F0")+" longNight="+LongNight)+" "+DiagnosticState);
        }
        void NearMissTick(float dt)
        {
            string tag=visitWitnessed?"HC_COUNT_WITNESSED":"HC_COUNT_NEARMISS";
            if(CanSeePlayer){Debug.Log(tag+" abort=seen reason=she is in sight, diving");nearMissStage=0;BeginChase();return;}
            if(!Game.Player.Hidden){Debug.Log(tag+" abort=left reason=she left the hide");nearMissStage=0;BeginLand();return;}
            if(nearMissStage==1)
            {
                Fly(dt,Profile.ModeMove);
                if(routeAt<route.Count)return;
                // Landed: crawl the last metres along the NavMesh, low to the floor.
                route.Clear();routeAt=0;
                if(GroundUnder(transform.position,out var g0)&&NavMesh.SamplePosition(nearMissCrawl,out var goal,1,NavMesh.AllAreas))
                {
                    if(path==null)path=new NavMeshPath();
                    if(NavMesh.CalculatePath(g0,goal.position,NavMesh.AllAreas,path)&&path.status!=NavMeshPathStatus.PathInvalid)route.AddRange(path.corners);
                }
                route.Add(nearMissCrawl);nearMissStage=2;speedNow=0;
                Debug.Log(tag+" landed crawl waypoints="+route.Count+" "+DiagnosticState);return;
            }
            if(nearMissStage==2)
            {
                if(routeAt<route.Count)
                {
                    var target=route[routeAt];var to=target-transform.position;float dist=to.magnitude;float step=count.NearMissCrawlSpeed*dt;
                    var next=step>=dist?target:transform.position+to/dist*step;
                    if(LegBlocked(transform.position,next,out var hit))
                    {
                        // Never crawl through a door or wall: a door is opened and waited for; after 2 s he holds where he is.
                        blockedFor+=dt;var door=hit.collider.GetComponentInParent<Door>();if(door)door.Open=true;
                        if(blockedFor<=dt*1.01f)Debug.Log("HC_COUNT_LEG blocked="+(door?"door":"wall")+" fix="+(door?"wait":"hold")+" hit="+hit.collider.name+" state=NearMiss crawl");
                        if(blockedFor>2f||!door){Debug.Log("HC_COUNT_LEG blocked="+(door?"door":"wall")+" fix=hold-here state=NearMiss crawl");route.Clear();routeAt=0;blockedFor=0;}
                        actor.Pose(count.ModeCrawl,0);return;
                    }
                    blockedFor=0;
                    if(step>=dist){transform.position=target;routeAt++;}else transform.position=next;
                    var flat=new Vector3(to.x,0,to.z);if(flat.sqrMagnitude>.0004f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(flat),count.TurnDegreesPerSecond*.5f*dt);
                    actor.Pose(count.ModeCrawl,count.NearMissCrawlSpeed*.85f);return;
                }
                nearMissStage=3;nearMissHold=visitWitnessed?count.WitnessedPauseSeconds:count.NearMissHoldSeconds;transform.rotation=Quaternion.LookRotation(-nearMissOutward);
                var d=new Vector3(NearMissDoor.x-transform.position.x,0,NearMissDoor.z-transform.position.z).magnitude;
                Debug.Log(tag+" hold doorDistance="+d.ToString("F2")+" seconds="+nearMissHold.ToString("F1")+" opens="+visitOpens+" "+DiagnosticState);
                // Near-miss: breath at the slats + rattle (CastleRun). Witnessed: a pause, then the rattle as he takes the door.
                if(!visitWitnessed)RaiseBeat("near-miss");
                return;
            }
            if(nearMissStage==3)
            {
                // Holding at the slats (Mode 7, barely moving).
                actor.Pose(count.ModeCrawl,0);float before=nearMissHold;nearMissHold-=dt;
                if(visitWitnessed&&before>.5f&&nearMissHold<=.5f)RaiseBeat("hide-open");   // the rattle half a second before the grab
                if(nearMissHold>0)return;
                if(visitOpens)
                {
                    // r12 (no pop): he rears back from the slats to the grab's stand-off (it ends where the grab clip needs him),
                    // then commits; CommitGrab then has nothing left to move.
                    var feet=Game.Player.transform.position;var outward=nearMissOutward;
                    visitStand=feet+outward*count.StandOffFor(HideGrabMode(feet,outward));visitStand.y=feet.y;
                    nearMissStage=5;Debug.Log(tag+" rear-back to="+visitStand+" "+DiagnosticState);return;
                }
                Debug.Log("HC_COUNT_NEARMISS open=False");
                LeaveHideVisit(true);return;
            }
            if(nearMissStage==5)
            {
                transform.position=Vector3.MoveTowards(transform.position,visitStand,count.RearBackSpeed*dt);actor.Pose(count.ModeCrawl,count.RearBackSpeed*.85f);
                if((transform.position-visitStand).sqrMagnitude>.0025f)return;
                {
                    nearMissStage=4;visitGrab=0;OpenHideDoor(Game.Player.HideExit);
                    // Wave I: at a hide entrance the grab is GrabDrop (it bursts the door down on her), witnessed or near-miss;
                    // count17: only with headroom for the clip's fall, else GrabCrawl from the door (CommitGrab).
                    grabMode=PreferGrab(count.ModeGrabCrawl,"GrabCrawl",out var why);   // count-l3: crawl from the door, no drop pop
                    grabMode=CommitGrab(true,grabMode);
                    BeginGrabCam(visitWitnessed?"hideopen":"nearmiss",true);
                    if(!visitWitnessed)Debug.Log("HC_COUNT_NEARMISS open=True");
                    Debug.Log("HC_COUNT_GRAB mode="+grabMode+" height="+(transform.position.y-Game.Player.transform.position.y).ToString("F2")+" reason="+why+" hideEntrance=True witnessed="+visitWitnessed+" torch="+Game.Player.TorchOn+" "+DiagnosticState);
                    return;
                }
            }
            // Stage 4: he opens it.
            var toDoor=-nearMissOutward;transform.rotation=Quaternion.LookRotation(toDoor);
            actor.Pose(grabMode);visitGrab+=dt;contact=visitGrab;
            if(visitGrab<Profile.GrabSeconds)return;
            if(huntClock>Profile.GrabGraceSeconds)Game.Case.Die();
            if(Game.Case.Terminal)return;
            Debug.Log(tag+" grab blocked reason="+(huntClock<=Profile.GrabGraceSeconds?"grace":"driver invulnerability")+" "+DiagnosticState);
            contact=0;grabMode=-1;EndGrabCam("blocked");LeaveHideVisit(false);
        }
        int PreferGrab(int mode,string state,out string why)
        {
            if(mode>=0&&HasModeState(mode,state)){why="hide opened ("+state+")";return mode;}
            int fallback=ChooseGrab(true,out var other,out _);why="hide opened, "+state+" missing; "+other;return fallback;
        }
        void LeaveHideVisit(bool survived)
        {
            nearMissStage=0;Memory.Clear();searchedAt=NearMissDoor;guaranteeClock=0;
            if(survived)GrabSurvived(visitWitnessed?"witnessed hide":"near-miss");
            int far=-1;float farthest=-1;
            for(int i=0;i<Game.World.PerchPoints.Count;i++){float d=Vector3.Distance(Game.World.PerchPoints[i],NearMissDoor);if(d>farthest){farthest=d;far=i;}}
            Debug.Log((visitWitnessed?"HC_COUNT_WITNESSED":"HC_COUNT_NEARMISS")+" leave to perch="+far+" "+DiagnosticState);
            if(far<0||!GoToPerch(far,false))BeginLand();
        }
        Vector3 DrawbridgeSide()
        {
            var spots=Game.World.Castle;if(spots==null)return Game.World.Spawn;
            return spots.Drawbridge?spots.DrawbridgeTrigger.center:spots.DrawbridgeWinch;
        }
    }
}
