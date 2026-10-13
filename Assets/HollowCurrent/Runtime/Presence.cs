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
            Game=game;Profile=HunterProfile.For(game.Map);count=Profile as CountClaudeProfile;
            var prefab=Resources.Load<GameObject>(Profile.ModelResource);
            if(!prefab&&Profile.AllowPlaceholderModel){silhouette=PlaceholderModel();actor=silhouette.GetComponent<HearthboundActor>();Debug.LogWarning("HC_HUNTER_PLACEHOLDER missing "+Profile.ModelResource);}
            else
            {
                if(!prefab)throw new System.InvalidOperationException("Required original animated apparition is missing");
                silhouette=Object.Instantiate(prefab,transform,false).transform;actor=silhouette.GetComponent<HearthboundActor>();
                if(!actor||actor.BoneCount<Profile.MinimumBones)throw new System.InvalidOperationException("Apparition is not a verified skinned rig");
            }
            agent=gameObject.AddComponent<NavMeshAgent>();agent.radius=Profile.AgentRadius;agent.height=Profile.AgentHeight;agent.speed=Profile.Speed;agent.angularSpeed=Profile.AngularSpeed;agent.acceleration=Profile.Acceleration;agent.stoppingDistance=Profile.StoppingDistance;
            if(count!=null)CountRimLight();
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
            if(Game.Modal||Game.Case.Phase==Phase.Hunt||Game.Case.Phase==Phase.Warning||Game.Case.Terminal||presentationRemaining>0&&!supersede)return false;
            fakeRush=false;staged=false;agent.enabled=false;transform.position=point;transform.rotation=Quaternion.Euler(0,yaw,0);
            silhouette.gameObject.SetActive(true);actor.Pose(Profile.ModeSearch);presentationRemaining=seconds;Appearances++;Game.Sound.Cue("apparition",point+Vector3.up,.85f);return true;
        }
        // End a staged appearance immediately (capture tooling, scripted beats).
        public void Dismiss(){presentationRemaining=0;fakeRush=false;staged=false;choreo=false;RestoreHiddenFloaters();if(silhouette)silhouette.localRotation=Quaternion.identity;if(silhouette)silhouette.gameObject.SetActive(false);}
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
            if(Game==null||Game.Case.Phase!=Phase.Hunt)return;
            float vertical=Mathf.Abs(source.y-transform.position.y);
            if(vertical>Profile.VerticalGap)radius*=Profile.VerticalHearingScale;
            if(Vector3.Distance(source,transform.position)<=radius){Memory.Observe(source.x,source.y,source.z);if(NativeVerification.Requested&&!NativeVerification.ProfileOnly)Debug.Log("HC_HEARD "+source+" "+DiagnosticState);}
        }
        public void WitnessHide(Vector3 spot)
        {
            if(Game.Case.Phase==Phase.Hunt&&(count!=null?CountSees():Sees())) {witnessedHide=true;knownHide=spot;Memory.Observe(spot.x,spot.y,spot.z);}
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
        void Update()
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
        enum Flight { Perched, Travel, Chase, Land, Stagger, Retreat, Dead }
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
            return root;
        }

        internal void CountStagger()
        {
            if(flight==Flight.Dead)return;
            staggerTimer=Fight.StaggerSeconds;retreatTimer=0;flight=Flight.Stagger;contact=0;route.Clear();actor.Pose(Profile.ModeStagger);
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
            staged=false;agent.enabled=false;route.Clear();holdTimer=0;
            if(flight==Flight.Dead){silhouette.gameObject.SetActive(true);actor.Pose(Profile.ModeDeath);return;}
            if(phase==Phase.Dead)
            {
                // Wing-wrap at a fixed stand-off: the Grab clip reaches GrabReach ahead of him, so the camera stays
                // CameraClearance outside the mesh while the presentation keeps animating (unscaled time).
                capturePresentation=true;presentationRemaining=Profile.CapturePresentationSeconds;
                var player=Game.Player.transform.position;var towards=player-transform.position;towards.y=0;
                if(towards.sqrMagnitude<.01f)towards=transform.forward;towards.Normalize();
                transform.SetPositionAndRotation(player-towards*count.GrabStandOff,Quaternion.LookRotation(towards));
                silhouette.gameObject.SetActive(true);actor.Pose(count.ModeGrab,0,true);CanSeePlayer=false;Memory.Clear();return;
            }
            if(phase==Phase.Hunt){CountBeginHunt();return;}
            silhouette.gameObject.SetActive(false);CanSeePlayer=false;Memory.Clear();witnessedHide=false;flight=Flight.Perched;
        }
        void CountBeginHunt()
        {
            var perches=Game.World.PerchPoints;var player=Game.Player.transform.position;
            Vector3 desired=huntOrigin??(Profile.HuntSpawn??Game.World.HunterSpawn);huntOrigin=null;
            Memory.Clear();witnessedHide=false;think=0;contact=0;tell=0;staggerTimer=retreatTimer=0;speedNow=0;searchedAt=new Vector3(9999,9999,9999);
            silhouette.gameObject.SetActive(true);
            // Between hunts he is seen perched: the hunt starts on the perch nearest the hunt origin that is not on top of you.
            int best=-1;float bestScore=float.MaxValue;
            for(int i=0;i<perches.Count;i++)
            {
                float score=Vector3.Distance(perches[i],desired)+(Vector3.Distance(perches[i],player)<Profile.HuntSpawnMinPlayerDistance+2?1000:0);
                if(score<bestScore){bestScore=score;best=i;}
            }
            if(best>=0)SetPerch(best,Random.Range(count.HuntPerchMinSeconds,count.HuntPerchMaxSeconds));
            else
            {
                if(GroundUnder(desired,out var ground))transform.position=ground;else transform.position=desired;
                BeginLand();
            }
            Debug.Log("HC_HUNT_SPAWN "+DiagnosticState);
        }
        void SetPerch(int i,float seconds)
        {
            Ledge(i,out var feet,out perchYaw);transform.position=feet;
            perchIndex=i;pendingPerch=-1;flight=Flight.Perched;perchTimer=seconds;tell=0;route.Clear();speedNow=0;actor.Pose(Profile.ModeIdle);
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
                pendingPerch=candidate;travelToPerch=true;flight=retreat?Flight.Retreat:Flight.Travel;return true;
            }
            BeginLand();return false;
        }
        void BeginSearch(Vector3 point)
        {
            searchedAt=point;searchSerial++;
            if(!PlanRoute(point,false)){BeginLand();return;}
            travelToPerch=false;pendingPerch=-1;flight=Flight.Travel;
        }
        void BeginChase(){flight=Flight.Chase;tell=0;holdTimer=0;replan=0;route.Clear();Debug.Log("HC_COUNT_DIVE "+DiagnosticState);}
        void BeginLand(){flight=Flight.Land;landTimer=count.SearchLandSeconds;route.Clear();scanClock=0;}

        // Returns true when the Count handled the frame (the shared non-hunt code still runs presentations).
        bool CountFrame()
        {
            if(choreo){CaptureChoreography(Mathf.Min(Time.deltaTime,1f/30));return true;}
            if(staged)return true;
            float dt=Time.deltaTime;
            if(flight==Flight.Dead)
            {
                if(GroundUnder(transform.position,out var ground)&&ground.y<transform.position.y)transform.position=Vector3.MoveTowards(transform.position,ground,3*dt);
                actor.Pose(Profile.ModeDeath);return true;
            }
            if(Game.Modal)return true;
            if(Game.Case.Phase!=Phase.Hunt)return false;
            CountHunt(dt);
            if(CaptureMode.Requested)KeepCameraClear();
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
            Memory.Tick(dt);CanSeePlayer=!retreating&&CountSees();
            if(CanSeePlayer){var seen=Game.Player.transform.position;Memory.Observe(seen.x,seen.y,seen.z);}
            if(!retreating&&Game.Player.Hidden&&Game.Player.TorchOn)Hear(Game.Player.transform.position,Profile.HiddenTorchHearRadius);
            breath-=dt;
            if(breath<=0){breath=Profile.BreathInterval;Game.Sound.Cue("breath",transform.position+Vector3.up,Profile.BreathVolume);}
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
                    if(CanSeePlayer){tell+=dt;if(tell>=count.PerchTellSeconds)BeginChase();break;}
                    tell=Mathf.Max(0,tell-dt);
                    if(freshMemory){BeginSearch(memory);break;}
                    perchTimer-=dt;
                    if(perchTimer<=0&&Game.World.PerchPoints.Count>1)GoToPerch((perchIndex+1)%Game.World.PerchPoints.Count,false);
                    break;
                }
                case Flight.Travel:
                case Flight.Retreat:
                {
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
            }
        }
        void CountChase(float dt,Vector3 memory)
        {
            var player=Game.Player;Vector3 goal;
            if(witnessedHide)goal=knownHide;
            else if(CanSeePlayer)goal=player.transform.position;
            else if(Memory.Knows)goal=memory;
            else{BeginLand();return;}
            foreach(var spot in Game.World.HidingPlaces)
                if(spot.Kind==InteractKind.Hide&&Vector3.Distance(goal,spot.HidePoint)<.35f){goal=spot.ExitPoint;break;}
            var flat=player.transform.position-transform.position;float vertical=Mathf.Abs(flat.y);flat.y=0;float distance=flat.magnitude;
            // Reached a remembered spot without seeing you: land there and prowl (Mode 2), then take a perch.
            var toGoal=goal-transform.position;toGoal.y=0;
            if(!CanSeePlayer&&!witnessedHide&&toGoal.magnitude<1){searchedAt=goal;BeginLand();return;}
            // Critic capture: dive to the hold distance, hover there in the Dive pose, then feint back to a perch.
            if(CaptureMode.Requested&&distance<=count.CaptureHoldDistance)
            {
                if(flat.sqrMagnitude>.0001f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(flat),count.TurnDegreesPerSecond*dt);
                speedNow=0;actor.Pose(Profile.ModeReach);holdTimer+=dt;
                if(holdTimer>=count.CaptureHoldSeconds){holdTimer=0;tell=0;Memory.Clear();searchedAt=player.transform.position;GoToPerch(Mathf.Max(0,NearestPerch(transform.position)),false);Debug.Log("HC_COUNT_FEINT "+DiagnosticState);}
                return;
            }
            replan-=dt;
            if(replan<=0||Vector3.Distance(goal,lastChaseGoal)>1||routeAt>=route.Count){replan=Profile.ThinkInterval;lastChaseGoal=goal;PlanRoute(goal,false);}
            // A cupboard can be searched from its clear entrance only (same rule as Split Grin).
            bool knowsThisHide=player.Hidden&&((witnessedHide&&Vector3.Distance(knownHide,player.transform.position)<.35f)||
                (Memory.Knows&&Vector3.Distance(memory,player.transform.position)<.35f));
            bool atHideEntrance=knowsThisHide&&Vector3.Distance(new Vector3(transform.position.x,player.HideExit.y,transform.position.z),player.HideExit)<Profile.HideEntranceDistance&&
                !Physics.Linecast(transform.position+Vector3.up,player.HideExit+Vector3.up);
            bool clearReach=!CaptureMode.Requested&&distance<Profile.GrabDistance&&vertical<1.6f&&!player.Hidden&&
                !Physics.Linecast(transform.position+Vector3.up,player.transform.position+Vector3.up,FlightMask,QueryTriggerInteraction.Ignore);
            bool touching=(clearReach&&(CanSeePlayer||contact>0))||atHideEntrance;
            contact=touching?contact+dt:0;
            if(touching)
            {
                if(flat.sqrMagnitude>.0001f)transform.rotation=Quaternion.LookRotation(flat);
                actor.Pose(count.ModeGrab);
                if(contact>=Profile.GrabSeconds&&Game.Case.PhaseTime>Profile.GrabGraceSeconds)Game.Case.Die();
                return;
            }
            Fly(dt,Profile.ModeReach);
        }
        void Fly(float dt,int mode)
        {
            if(routeAt<route.Count)
            {
                var target=route[routeAt];var to=target-transform.position;float dist=to.magnitude;
                speedNow=Mathf.MoveTowards(speedNow,CruiseSpeed(to),Profile.Acceleration*dt);
                float step=speedNow*dt;
                if(step>=dist){transform.position=target;routeAt++;}
                else transform.position+=to/dist*step;
                var flat=new Vector3(to.x,0,to.z);
                if(flat.sqrMagnitude>.0004f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(flat),count.TurnDegreesPerSecond*dt);
                foreach(var door in Game.World.Doors)if(Vector3.Distance(door.transform.position,transform.position)<Profile.DoorOpenDistance)door.Open=true;
            }
            // Wing-beat rate follows speed but stays slow enough to read as a giant (LocomotionRate = speed / 2.05).
            actor.Pose(mode,1.2f+speedNow*.35f);
        }
        float CruiseSpeed(Vector3 to)
        {
            var p=transform.position;
            if(NearServiceDoor(p)||routeAt<route.Count&&NearServiceDoor(route[routeAt]))return count.LandedWalkSpeed*SpeedScale;
            var forward=new Vector3(to.x,0,to.z);var side=forward.sqrMagnitude>.0001f?Vector3.Cross(Vector3.up,forward.normalized):transform.right;
            var chest=p+Vector3.up*1.1f;
            float width=Reach(chest,side,6)+Reach(chest,-side,6);
            bool straight=to.magnitude>=count.StraightLookahead||routeAt>=route.Count-1;
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
        bool PlanRoute(Vector3 goal,bool goalAir)
        {
            route.Clear();routeAt=0;routeGoal=goal;var start=transform.position;
            Vector3 end=goal;
            if(!goalAir){if(GroundUnder(goal,out var g))goal=g;end=goal+Vector3.up*HoverAt(goal,flight==Flight.Chase);}
            if(Clear(start,end)){route.Add(end);return true;}
            // Round 2: from a rail perch the downward sweep clipped the coping under his own feet, so he walked the
            // NavMesh route back across the balcony deck, out of sight. Lift forward off the ledge first.
            var launch=start+transform.forward*.6f+Vector3.up*.5f;
            if(Clear(start,launch)&&Clear(launch,end)){route.Add(launch);route.Add(end);return true;}
            if(path==null)path=new NavMeshPath();
            if(!GroundUnder(start,out var g0)||!GroundUnder(goal,out var g1)||!NavMesh.CalculatePath(g0,g1,NavMesh.AllAreas,path)||path.status==NavMeshPathStatus.PathInvalid||path.corners.Length==0)
                return false;
            var corners=path.corners;
            for(int i=0;i<corners.Length;i++)route.Add(corners[i]+Vector3.up*HoverAt(corners[i],false));
            route.Add(end);
            // String-pull: skip leading waypoints he can already fly past in a straight line.
            for(int guard=0;guard<4&&route.Count>1&&Clear(start,route[1]);guard++)route.RemoveAt(0);
            return true;
        }
        float HoverAt(Vector3 ground,bool chasing)
        {
            if(NearServiceDoor(ground))return 0;
            float ceiling=Physics.Raycast(ground+Vector3.up*.2f,Vector3.up,out var hit,8,FlightMask,QueryTriggerInteraction.Ignore)?hit.distance+.2f:8;
            float max=count.HoverMax*Mathf.Max(.3f,1-Tier*count.HoverTierScale);
            float hover=Mathf.Clamp(ceiling-count.BodyClearance,0,max);
            if(chasing)hover=Mathf.Min(hover,count.HoverNearPlayer);
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
            if(Physics.Raycast(p+Vector3.up*.3f,Vector3.down,out var hit,12,FlightMask,QueryTriggerInteraction.Ignore))probe=hit.point;
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
            if(delta.magnitude>Profile.SightRange||Vector3.Angle(look,delta)>Profile.SightAngle)return false;
            if(Physics.Raycast(eye,delta.normalized,out var hit,delta.magnitude,FlightMask,QueryTriggerInteraction.Ignore))return hit.collider.GetComponentInParent<Investigator>()!=null;
            return true;
        }
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
        public const float CapturePerchEnd=3.5f,CaptureGlideEnd=6.5f,CaptureDiveEnd=10.6f,CaptureFeintEnd=13.6f,CaptureDiveArrive=1.5f,CaptureLungeStart=9.2f,CaptureLungeArrive=9.8f;
        static readonly string[] CapturePhases={"Perch","Glide","Dive","Grab","Feint"};
        bool choreo;
        float choreoClock;
        int choreoPhase=-1;
        Vector3 cPerch,cFacing,cGlideEnd,cHold,cLunge,cCamForward,cLook;
        float cLean;
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
            // Round 5: the whole approach stays above the 1.1 m ground-fog planes (r4 H07/H08 waist cut on the descent).
            cGlideEnd=ground+cCamForward*5.5f+Vector3.up*count.CaptureGlideEndHeight;
            // Round 4: hold high enough that his lowest point clears the 0.08-1.1 m ground-fog planes (the r3 "bust cut").
            cHold=ground+cCamForward*hold+Vector3.up*count.CaptureHoldHeight;
            cLunge=ground+cCamForward*count.CaptureLungeDistance+Vector3.up*count.CaptureHoldHeight;
            presentationRemaining=0;capturePresentation=false;fakeRush=false;staged=false;agent.enabled=false;route.Clear();contact=0;
            CanSeePlayer=false;Memory.Clear();perchIndex=i;flight=Flight.Perched;
            transform.SetPositionAndRotation(cPerch,Quaternion.Euler(0,yaw,0));silhouette.gameObject.SetActive(true);actor.Pose(Profile.ModeIdle);
            cLook=cPerch+Vector3.up;cLean=0;silhouette.localRotation=Quaternion.identity;choreo=true;choreoClock=0;choreoPhase=-1;
            Debug.Log("HC_COUNT_CAPTURE begin perch="+i+" feet="+cPerch+" glideEnd="+cGlideEnd+" hold="+cHold+" holdDistance="+hold.ToString("F2")+" eye="+player.View.transform.position);
            return true;
        }
        public void EndCaptureChoreography(){if(!choreo)return;choreo=false;silhouette.localRotation=Quaternion.identity;Debug.Log("HC_COUNT_CAPTURE end clock="+choreoClock.ToString("F2"));}
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
                if(k<1){phase=2;return Vector3.Lerp(cGlideEnd,cHold,k*k);}
                // Climax: front-facing Grab (Mode 6) at the hold, face to the camera, then a short lunge in so the claws
                // and wing tips frame the lens.
                phase=3;mode=count.ModeGrab;float l=Mathf.SmoothStep(0,1,(t-CaptureLungeStart)/(CaptureLungeArrive-CaptureLungeStart));
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
            if(phase==1||phase==2){var travel=heading.normalized;heading=Vector3.Slerp(travel,toCamera.normalized,.6f);}
            float speed=(ahead-position).magnitude/.05f;
            transform.position=position;
            if(heading.sqrMagnitude>.0001f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(heading),(phase==0?120:phase==4?150:540)*dt);
            actor.Pose(mode,1.2f+speed*.35f);
            // Lean the whole figure back while gliding/diving toward the camera so the chest, not the soles, faces it.
            float lean=phase==1?count.CaptureGlideLean:phase==2?count.CaptureDiveLean:phase==4?count.CaptureGlideLean*.5f:0;
            cLean=Mathf.Lerp(cLean,lean,1-Mathf.Exp(-4*dt));silhouette.localRotation=Quaternion.Euler(cLean,0,0);
            if(phase!=choreoPhase)
            {
                choreoPhase=phase;
                Debug.Log("HC_COUNT_CAPTURE phase="+CapturePhases[phase]+" clock="+choreoClock.ToString("F2")+" mode="+mode+" pos="+position+" distance="+Vector3.Distance(position+Vector3.up,eye).ToString("F2"));
            }
            // The investigator's view follows him from the investigator's own eye height (the body stays put).
            // Close in, aim between the bounds centre and his head so the face stays in frame.
            var bounds=actor.PoseBounds(0);var aim=phase==3&&actor.Head?Vector3.Lerp(bounds.center,actor.Head.position,.55f):bounds.center;
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
        // A cold, shadowless rim light that rides behind his shoulders so wings and coat read against dark stone
        // (r2 hunt: "an unlit black smear"). Castle only; Split Grin never gets it.
        void CountRimLight()
        {
            var rim=new GameObject("Count rim light").AddComponent<Light>();rim.transform.SetParent(silhouette,false);
            rim.transform.localPosition=new Vector3(0,2.3f,-1.1f);rim.type=LightType.Point;rim.color=new Color(.62f,.72f,1f);
            rim.range=count.RimLightRange;rim.intensity=count.RimLightIntensity;rim.shadows=LightShadows.None;rim.renderMode=LightRenderMode.ForcePixel;
        }
    }
}
