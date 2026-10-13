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
        public string DiagnosticState=>"position="+transform.position+" player="+Game.Player.transform.position+" enabled="+agent.enabled+" nav="+agent.isOnNavMesh+" path="+(agent.enabled&&agent.isOnNavMesh?agent.pathStatus.ToString():"none")+" destination="+(agent.enabled&&agent.isOnNavMesh?agent.destination.ToString():"none")+" seen="+CanSeePlayer+" memory="+Memory.Knows;

        public int Appearances {get;private set;}
        public bool CanSeePlayer {get;private set;}
        public void Initialize(Game game)
        {
            Game=game;Profile=HunterProfile.For(game.Map);
            var prefab=Resources.Load<GameObject>(Profile.ModelResource);
            if(!prefab&&Profile.AllowPlaceholderModel){silhouette=PlaceholderModel();actor=silhouette.GetComponent<HearthboundActor>();Debug.LogWarning("HC_HUNTER_PLACEHOLDER missing "+Profile.ModelResource);}
            else
            {
                if(!prefab)throw new System.InvalidOperationException("Required original animated apparition is missing");
                silhouette=Object.Instantiate(prefab,transform,false).transform;actor=silhouette.GetComponent<HearthboundActor>();
                if(!actor||actor.BoneCount<Profile.MinimumBones)throw new System.InvalidOperationException("Apparition is not a verified skinned rig");
            }
            agent=gameObject.AddComponent<NavMeshAgent>();agent.radius=Profile.AgentRadius;agent.height=Profile.AgentHeight;agent.speed=Profile.Speed;agent.angularSpeed=Profile.AngularSpeed;agent.acceleration=Profile.Acceleration;agent.stoppingDistance=Profile.StoppingDistance;
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
            fakeRush=false;agent.enabled=false;transform.position=point;transform.rotation=Quaternion.Euler(0,yaw,0);
            silhouette.gameObject.SetActive(true);actor.Pose(Profile.ModeSearch);presentationRemaining=seconds;Appearances++;Game.Sound.Cue("apparition",point+Vector3.up,.85f);return true;
        }
        // End a staged appearance immediately (capture tooling, scripted beats).
        public void Dismiss(){presentationRemaining=0;fakeRush=false;if(silhouette)silhouette.gameObject.SetActive(false);}
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
            if(Game.Case.Phase==Phase.Hunt&&Sees()) {witnessedHide=true;knownHide=spot;Memory.Observe(spot.x,spot.y,spot.z);}
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
    }
}
