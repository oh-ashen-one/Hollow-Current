using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace HollowCurrent
{
    // Sparse appearances punctuate long stretches of unresolved tension;
    // pursuit still uses Presence's sight/sound memory and the investigation rules.
    public sealed class ScareDirector : MonoBehaviour
    {
        Game game;
        float next=24,activity,blackout,age;
        readonly ScareRhythm rhythm=new ScareRhythm();
        Phase previousPhase;
        bool basement;
        readonly List<Transform> props=new List<Transform>();
        ParticleSystem mist;
        Material mistMaterial;
        public int Events {get;private set;}
        public int BasementFlashes {get;private set;}
        public int ThrownProps {get;private set;}
        public Transform LastThrownProp {get;private set;}
        // Castle only: the CastleScares beat set (F3), added by Game. Null in Bellwether.
        public CastleScares Castle=>game?game.CastleDread:null;
        bool IsCastle=>game.Map==MapId.Castle;
        public bool Interference=>activity>0;
        public bool TorchFailed=>blackout>0;
        public float LightMultiplier=>activity>0?(Mathf.Sin(age*37)+Mathf.Sin(age*71)>.15f?.08f:1):1;
        public int FalseReading=>Mathf.Abs(Mathf.FloorToInt(age*19)*17)%6;
        public void Initialize(Game owner)
        {
            game=owner;
            foreach(var t in game.World.Root.GetComponentsInChildren<Transform>())if(t.name=="Authored / Mug")props.Add(t);
            // Castle goblets, candlesticks, books and plates carry F3's Throwable marker.
            if(game.Map==MapId.Castle){foreach(var t in game.World.Root.GetComponentsInChildren<Throwable>())props.Add(t.transform);Debug.Log("HC_THROWABLES director="+props.Count);}
            var fog=new GameObject("Local manifestation mist");fog.transform.SetParent(transform,false);
            mist=fog.AddComponent<ParticleSystem>();mist.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=mist.main;main.playOnAwake=false;main.loop=false;main.duration=2;main.startLifetime=2.8f;main.startSpeed=.15f;main.startSize=1.6f;main.startColor=new Color(.55f,.65f,.68f,.12f);main.maxParticles=32;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=mist.emission;emission.rateOverTime=0;
            var shape=mist.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.8f;
            var color=mist.colorOverLifetime;color.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.7f,.2f),new GradientAlphaKey(0,1)});color.color=gradient;
            mistMaterial=new Material(Resources.Load<Shader>("ManifestMist"));mist.GetComponent<ParticleSystemRenderer>().sharedMaterial=mistMaterial;
        }
        public void Blackout(float seconds){blackout=Mathf.Max(blackout,seconds);}
        public void RetryGrace(){next=25;rhythm.Rest(55);basement=game.Player.transform.position.y< -1;}
        public void Pulse(Vector3 point,float seconds=5)
        {
            activity=Mathf.Max(activity,seconds);blackout=Mathf.Max(blackout,1.8f);
            mist.transform.position=point+Vector3.up*.7f;mist.Play();mist.Emit(22);
            game.Sound.Cue("radio",game.Player.View.transform.position,.5f);
            game.Sound.Cue("breath_presence",point+Vector3.up*1.5f,.65f);
        }
        void Update()
        {
            if(!game||game.Modal)return;
            float dt=Time.deltaTime;age+=dt;activity=Mathf.Max(0,activity-dt);blackout=Mathf.Max(0,blackout-dt);
            if(!game.Case.Equipped||game.Case.Phase==Phase.Briefing||game.Case.Phase==Phase.Return)return;
            rhythm.Tick(dt);
            if(previousPhase!=game.Case.Phase)
            {
                previousPhase=game.Case.Phase;
                if(previousPhase==Phase.Warning||previousPhase==Phase.Hunt)rhythm.Rest(90);
                if(previousPhase==Phase.Recovery){rhythm.Rest(65);next=30;}
            }
            if(game.Player.Hidden||game.Case.Phase==Phase.Hunt||game.Case.Phase==Phase.Warning||game.Case.Phase==Phase.Recovery||game.Case.Phase==Phase.Containment)return;
            // Reserve the approach to a natural hunt for dread, not a competing
            // loud apparition. The nine-second hunt warning remains intact.
            bool approachingHunt=game.Case.Phase==Phase.Disturbance&&game.Case.Evidence.Count>=2&&game.Case.PhaseTime>=55;
            if(!basement&&game.Player.transform.position.y< -2.5f&&!approachingHunt&&rhythm.CanSurprise)
            {
                if(Trigger(true)){basement=true;BasementFlashes++;}
                return;
            }
            next-=dt;if(next>0)return;
            if(!approachingHunt&&rhythm.AppearanceDue)Trigger();else Dread();
            next=Random.Range(22f,38f);
        }
        public void Dread()
        {
            if(game.Modal||game.Player.Hidden||game.Case.Phase==Phase.Warning||game.Case.Phase==Phase.Hunt||game.Case.Phase==Phase.Recovery)return;
            if(IsCastle&&CastleBeat())return;
            var point=game.Player.transform.position-game.Player.transform.forward*3+game.Player.transform.right*2;
            // These hints end without a sting, rush, blackout or explanatory card.
            switch(Random.Range(0,6))
            {
                case 0:game.Sound.Cue("step",point,.15f);break;
                case 1:game.Sound.Cue("knock",point,.12f);break;
                case 2:game.Sound.Cue("radio",game.Player.transform.position,.09f);break;
                case 3:activity=Mathf.Max(activity,.65f);break;
                case 4:game.Sound.Cue("breath_presence",point+Vector3.up,.10f);break;
                // The last beat is deliberately unresolved silence.
            }
        }
        // Castle dread beats: lies above the Lies threshold (false candle tell, false compass), otherwise the
        // castle's own beats (Beat picks a book drop, knife, bat, bell; the window silhouette; the balustrade shape).
        bool CastleBeat()
        {
            if(!Castle)return false;
            // Lies (Dread >= 70): one candle tell in three points at an empty doorway; the compass lies while raised.
            if(game.Case.Lies)
            {
                if(Random.value<1f/3&&Castle.FalseCandleTell())return true;
                if(game.Player.Tool==1&&game.Case.Equipped&&Random.value<.5f){Castle.FalseCompassReading();return true;}
            }
            // Peripheral beats need the player's view and return false cheaply when nothing qualifies.
            float r=Random.value;
            if(r<.2f&&Castle.WindowSilhouette())return true;
            if(r<.35f&&Castle.BalustradeShape())return true;
            Castle.Beat();return true; // Beat logs its own HC_SCARE_CASTLE line
        }
        public bool Trigger(bool firstBasement=false)
        {
            if(game.Modal||game.Player.Hidden||game.Case.Phase==Phase.Hunt||game.Case.Phase==Phase.Warning||game.Case.Phase==Phase.Recovery||game.Case.Phase==Phase.Containment||game.Case.Phase==Phase.Return||!rhythm.CanSurprise)return false;
            if(!firstBasement&&!rhythm.AppearanceDue)return false;
            if(game.Case.Phase==Phase.Disturbance&&game.Case.Evidence.Count>=2&&game.Case.PhaseTime>=55)return false;
            if(!VisiblePoint(out var point)){if(!firstBasement)Dread();return false;}
            bool rush=!firstBasement&&Events%3==2;
            if(!game.Creature.ManifestAt(point,0,firstBasement?.32f:rush?1.6f:.55f,true))return false;
            game.Creature.FacePlayer();if(rush)game.Creature.BeginFakeRush();
            Pulse(point,firstBasement?3:5);Events++;rhythm.RecordAppearance(Random.value);
            next=Random.Range(28f,40f);
            if(IsCastle)
            {
                FearLink.AddDread(game.Case,game.Case.Rules.DreadApparition);
                // An apparition that lands close can become a hunt with a short tell (FEAR_PLAN item 3).
                float near=Vector3.Distance(point,game.Player.transform.position);
                if(near<4){bool escalated=FearLink.TryEscalate(game.Case);Debug.Log("HC_ESCALATE near="+near.ToString("F1")+" escalated="+escalated);}
            }
            if(!firstBasement&&Events%2==0)
            {
                // Castle: one throw in six is the poltergeist (everything on the table at once).
                int thrown=IsCastle&&Castle&&Random.value<1f/6?Castle.Poltergeist():0;
                if(thrown>0){ThrownProps+=thrown;FearLink.AddDread(game.Case,game.Case.Rules.DreadThrow);Debug.Log("HC_THROW poltergeist="+thrown);}
                else ThrowNearbyProp();
            }
            Debug.Log("HC_SCARE visible="+Events+" basement="+firstBasement+" rush="+rush);return true;
        }
        bool VisiblePoint(out Vector3 point)
        {
            var view=game.Player.View.transform;var forward=Vector3.ProjectOnPlane(view.forward,Vector3.up).normalized;
            if(forward.sqrMagnitude<.1f)forward=game.Player.transform.forward;
            // Try both sides of the actual camera view, then the centre. Sample
            // on this floor and require clear torso/head lines and body clearance.
            foreach(float angle in new[]{24f,-24f,0f,38f,-38f})foreach(float distance in new[]{4.8f,3.4f,6f})
            {
                var desired=game.Player.transform.position+Quaternion.Euler(0,angle,0)*forward*distance;
                if(!NavMesh.SamplePosition(desired,out var hit,1,NavMesh.AllAreas)||Mathf.Abs(hit.position.y-game.Player.transform.position.y)>.6f)continue;
                var candidate=hit.position;var eye=candidate+Vector3.up*1.65f;
                if(Vector3.Distance(candidate,game.Player.transform.position)<2.8f||Vector3.Angle(view.forward,eye-view.position)>44)continue;
                if(Physics.Linecast(view.position,eye,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore)||Physics.Linecast(view.position,candidate+Vector3.up*.8f,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore))continue;
                if(Physics.CheckCapsule(candidate+Vector3.up*.4f,candidate+Vector3.up*1.7f,.28f,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore))continue;
                point=candidate;return true;
            }
            point=Vector3.zero;return false;
        }
        public bool ThrowNearbyProp()
        {
            foreach(var prop in props)
            {
                if(!prop||prop.GetComponent<Rigidbody>())continue;
                var renderers=prop.GetComponentsInChildren<Renderer>();if(renderers.Length==0)continue;
                var visualBounds=renderers[0].bounds;foreach(var r in renderers)visualBounds.Encapsulate(r.bounds);
                var visibleCenter=visualBounds.center;
                var delta=visibleCenter-game.Player.View.transform.position;
                if(delta.magnitude>6||Vector3.Angle(game.Player.View.transform.forward,delta)>65||Physics.Linecast(game.Player.View.transform.position,visibleCenter,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore))continue;
                var filters=prop.GetComponentsInChildren<MeshFilter>();if(filters.Length==0)continue;
                var bounds=new Bounds();bool first=true;
                foreach(var filter in filters)
                {
                    var b=filter.sharedMesh.bounds;var matrix=prop.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                    for(int corner=0;corner<8;corner++)
                    {
                        var point=matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                        if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
                    }
                }
                float limit=prop.GetComponent<Throwable>()?.9f:.4f; // castle candlesticks stand taller than a mug
                if(bounds.size.x>limit||bounds.size.y>limit||bounds.size.z>limit){Debug.LogError("HC_THROWABLE_BOUNDS_INVALID "+bounds);continue;}
                var collider=prop.gameObject.AddComponent<BoxCollider>();collider.center=bounds.center;collider.size=bounds.size;Physics.IgnoreCollision(collider,game.Player.Controller);
                var body=prop.gameObject.AddComponent<Rigidbody>();body.mass=.25f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.interpolation=RigidbodyInterpolation.Interpolate;
                var impact=prop.gameObject.AddComponent<ScareImpact>();impact.Sound=game.Sound;
                var lateral=Vector3.Cross(Vector3.up,delta.normalized);body.AddForce((lateral*5+Vector3.up*2.3f),ForceMode.VelocityChange);body.AddTorque(new Vector3(7,13,5),ForceMode.VelocityChange);
                game.Sound.Cue("knock",prop.position,.6f);ThrownProps++;LastThrownProp=prop;
                if(IsCastle)FearLink.AddDread(game.Case,game.Case.Rules.DreadThrow);
                Debug.Log("HC_THROW prop="+prop.name+" at="+prop.position+" distance="+delta.magnitude.ToString("F1"));return true;
            }
            return false;
        }
        void OnDestroy(){if(mistMaterial)Destroy(mistMaterial);}
    }
    public sealed class ScareImpact : MonoBehaviour
    {
        public Soundscape Sound;int hits;
        void OnCollisionEnter(Collision collision)
        {if(hits<3&&collision.relativeVelocity.magnitude>1){hits++;Sound.Cue("knock",transform.position,.75f);}}
    }
}
