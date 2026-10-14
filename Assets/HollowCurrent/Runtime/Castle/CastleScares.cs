using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HollowCurrent
{
    // Castle scares (fear plan wave F, piece F3): consequence-free dread beats, peripheral sightings, false tells and the
    // poltergeist throw. Every effect is a plain call on this MonoBehaviour. Nothing here reads Investigation rules beyond
    // Game.Case.Phase (no beat plays during a warning, hunt or recovery). The integrator adds the component in Game for the castle,
    // calls Initialize(this), and decides when each call happens (see HANDOFF.md).
    // Every beat logs "HC_SCARE_CASTLE <beat>". Audio goes through Game.Sound.Cue, which is silent for a missing castle clip.
    public sealed class CastleScares : MonoBehaviour
    {
        // The integrator's compass needle spins while this is true (set for 3 s by FalseCompassReading).
        public bool FalseCompass;
        public int Beats {get;private set;}
        public string LastBeat {get;private set;}
        public int Thrown {get;private set;}

        const int OcclusionMask=~((1<<2)|(1<<30)); // same occlusion mask as ScareDirector
        Game game;
        float falseCompassTimer;
        Transform bell;
        Quaternion bellRest;
        bool bellSwinging,silhouetteBusy;
        readonly HashSet<CandleFlame> guttering=new HashSet<CandleFlame>();
        static Material silhouetteMaterial;
        static Texture2D wingTexture,figureTexture;

        public void Initialize(Game owner){game=owner;}

        bool Ready=>game&&game.Player&&game.Player.View&&game.World!=null&&game.World.Root;
        Transform Root=>game.World.Root;
        Transform Eye=>game.Player.View.transform;
        // Beats stay out of the phases ScareDirector.Dread and Trigger also skip, and out of menus, hiding and notes.
        bool Calm
        {
            get
            {
                if(!Ready||game.Modal||game.Player.Hidden)return false;
                var phase=game.Case.Phase;
                return phase!=Phase.Warning&&phase!=Phase.Hunt&&phase!=Phase.Recovery;
            }
        }
        void Log(string beat){LastBeat=beat;Beats++;Debug.Log("HC_SCARE_CASTLE "+beat);}

        void Update()
        {
            if(falseCompassTimer>0){falseCompassTimer-=Time.deltaTime;if(falseCompassTimer<=0)FalseCompass=false;}
            if(shape)TickShape();
            else if(shapeArmed)TryPlaceShape();
        }

        // ===================================================================================================================
        // Item 15: dread beats (no sting, no card)
        // ===================================================================================================================
        // Picks one castle dread beat; one roll in six is plain silence. A beat that cannot happen here (no book pile, no knife, no bell
        // or candles near the player) falls through to the next, and the last resort is silence. Returns false only when not Calm.
        public bool Beat()
        {
            if(!Calm)return false;
            int roll=Random.Range(0,6);
            if(roll<5)
                for(int i=0;i<5;i++)
                    if(TryBeat((roll+i)%5))return true;
            Log("silence");return true;
        }
        bool TryBeat(int index)
        {
            switch(index)
            {
                case 0:return BookDrop();
                case 1:return KnifeStick();
                case 2:return BatPass();
                case 3:return BellToll();
                default:return CandleGutter();
            }
        }

        // ---- a book drops from the nearest floating book pile ------------------------------------------------------------------
        public bool BookDrop()
        {
            if(!Ready)return false;
            var player=game.Player.transform.position;
            Transform book=null;Floater owner=null;Transform pile=null;float best=14f*14f;
            foreach(var floater in Floater.All)
            {
                if(!floater)continue;
                bool isPile=floater.name.StartsWith("Floating book stack");
                bool isBook=floater.name.StartsWith("Castle / FloatingBook");
                if(!isPile&&!isBook)continue;
                if(isPile&&floater.transform.childCount==0)continue;
                var p=floater.transform.position;
                if(Mathf.Abs(p.y-player.y)>5)continue;
                var flat=p-player;flat.y=0;
                if(flat.sqrMagnitude>=best)continue;
                best=flat.sqrMagnitude;owner=floater;pile=isPile?floater.transform:null;book=isPile?null:floater.transform;
            }
            if(!owner)return false;
            if(pile)book=pile.GetChild(pile.childCount-1); // the top book of the pile
            else owner.enabled=false;                       // a lone floating book simply stops floating
            DropBook(book);
            Log("book_drop");return true;
        }
        void DropBook(Transform book)
        {
            book.SetParent(Root,true);
            var size=CastleLayout.ArtSize(CastleLayout.ArtNames.FloatingBook);
            var collider=book.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,size.y*.5f,0);collider.size=size;
            Physics.IgnoreCollision(collider,game.Player.Controller);
            var body=book.gameObject.AddComponent<Rigidbody>();body.mass=.6f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.interpolation=RigidbodyInterpolation.Interpolate;
            body.linearVelocity=Vector3.down*.4f;body.AddTorque(new Vector3(Random.Range(-2f,2f),Random.Range(-3f,3f),Random.Range(-2f,2f)),ForceMode.VelocityChange);
            var cue=book.gameObject.AddComponent<CueOnImpact>();cue.Sound=game.Sound;cue.Key="book_drop";cue.Volume=.9f;
        }

        // ---- one knife leaves the kitchen orbit and sticks in the block ----------------------------------------------------------
        KnifeOrbit orbit;
        public bool KnifeStick()
        {
            if(!Ready)return false;
            if(!orbit)orbit=FindFirstObjectByType<KnifeOrbit>();
            if(!orbit||orbit.ActiveKnives==0)return false;
            var player=game.Player.transform.position;var centre=orbit.transform.position;
            var flat=centre-player;flat.y=0;
            if(flat.magnitude>18||Mathf.Abs(centre.y-player.y)>4)return false;
            var knife=orbit.ReleaseKnife(player);
            if(!knife)return false;
            // The block top is 0.58 m below the orbit centre; the blade stands in it at about 63 degrees, tip down.
            var local=new Vector3(Random.Range(-.1f,.5f),-.58f+.23f,Random.Range(-.3f,.3f));
            var target=orbit.transform.TransformPoint(local);
            var heading=Random.insideUnitCircle.normalized;
            var direction=(Vector3.down+new Vector3(heading.x,0,heading.y)*.5f).normalized;
            StartCoroutine(KnifeFlight(knife,target,Quaternion.LookRotation(direction,Vector3.up)));
            Log("knife_stick");return true;
        }
        IEnumerator KnifeFlight(Transform knife,Vector3 target,Quaternion stuck)
        {
            var from=knife.position;var fromRotation=knife.rotation;
            const float flight=.5f;
            for(float t=0;t<flight;t+=Time.deltaTime)
            {
                if(!knife)yield break;
                float k=t/flight;k*=k; // accelerates into the block
                knife.position=Vector3.Lerp(from,target,k);knife.rotation=Quaternion.Slerp(fromRotation,stuck,Mathf.Clamp01(t/(flight*.6f)));
                yield return null;
            }
            if(!knife)yield break;
            knife.position=target;knife.rotation=stuck;
            game.Sound.Cue("knife_stick",target,.9f);
            for(float t=0;t<.9f;t+=Time.deltaTime)
            {
                if(!knife)yield break;
                knife.rotation=stuck*Quaternion.Euler(Mathf.Sin(t*55)*7f*(1-t/.9f),0,0); // the blade quivers and settles
                yield return null;
            }
            if(knife)
            {
                knife.rotation=stuck;
                var marker=knife.gameObject.AddComponent<Throwable>(); // a stuck kitchen knife can be thrown later
                if(!marker.BoundsValid)Destroy(marker);
            }
        }

        // ---- a giant bat crosses the torch beam three metres ahead -----------------------------------------------------------------
        public bool BatPass()
        {
            if(!Ready)return false;
            var eye=Eye;var forward=Vector3.ProjectOnPlane(eye.forward,Vector3.up);
            if(forward.sqrMagnitude<.05f)forward=game.Player.transform.forward;
            forward.Normalize();
            var right=Vector3.Cross(Vector3.up,forward);
            float distance=3f;
            var centre=eye.position+forward*distance+Vector3.up*.05f;
            if(Physics.Linecast(eye.position,centre,OcclusionMask,QueryTriggerInteraction.Ignore)){distance=2f;centre=eye.position+forward*distance+Vector3.up*.05f;}
            if(Physics.Linecast(eye.position,centre,OcclusionMask,QueryTriggerInteraction.Ignore))return false;
            float side=Random.value<.5f?-1:1;
            var start=centre-right*3.4f*side;var end=centre+right*3.4f*side;
            if(Physics.Linecast(start,end,OcclusionMask,QueryTriggerInteraction.Ignore))return false;
            var bat=new GameObject("Castle bat pass");
            bat.transform.position=start;
            bat.AddComponent<MeshFilter>().sharedMesh=SurrealProps.CentreQuad;
            var renderer=bat.AddComponent<MeshRenderer>();renderer.sharedMaterial=SurrealProps.BatMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            var crossing=bat.AddComponent<CrossingBat>();crossing.Begin(eye,start,end,1.15f,1.5f);
            game.Sound.Cue("bat_pass",centre,.9f);
            Log("bat_pass");return true;
        }

        // ---- the chapel bell tolls once by itself (a false tell: this is not the summons) ---------------------------------------
        public bool BellToll()
        {
            if(!Ready)return false;
            Vector3 anchor;
            var spots=game.World.Castle;
            if(spots!=null)anchor=spots.Anchor;
            else{FindBell();if(!bell)return false;anchor=bell.position;}
            var delta=anchor-game.Player.transform.position;
            if(delta.magnitude>34)return false;
            game.Sound.Cue("bell_single",anchor+Vector3.up*1.2f,1f);
            FindBell();
            if(bell&&!bellSwinging)StartCoroutine(SwingBell());
            Log("bell_single");return true; // deliberately does not call CastleRun.RingBell: ringing it is the summons
        }
        void FindBell()
        {
            if(bell)return;
            foreach(var t in Root.GetComponentsInChildren<Transform>(true))if(t.name=="Castle / ChapelBell"){bell=t;bellRest=t.localRotation;break;}
        }
        IEnumerator SwingBell()
        {
            bellSwinging=true;
            for(float t=0;t<2.6f;t+=Time.deltaTime)
            {
                if(!bell)break;
                bell.localRotation=bellRest*Quaternion.Euler(Mathf.Sin(t*6.5f)*2.4f*Mathf.Exp(-t*1.3f),0,0);
                yield return null;
            }
            if(bell)bell.localRotation=bellRest;
            bellSwinging=false;
        }

        // ---- a candle cluster near the player gutters ----------------------------------------------------------------------------
        public bool CandleGutter()
        {
            if(!Ready)return false;
            var player=game.Player.transform.position;
            CandleFlame seed=null;float best=7f*7f;
            foreach(var flame in CandleFlame.All)
            {
                if(!flame||!flame.isActiveAndEnabled||guttering.Contains(flame))continue;
                var d=flame.transform.position-player;
                if(Mathf.Abs(d.y)>3.5f)continue;
                d.y=0;
                if(d.sqrMagnitude<best){best=d.sqrMagnitude;seed=flame;}
            }
            if(!seed)return false;
            var cluster=new List<CandleFlame>();
            foreach(var flame in CandleFlame.All)
            {
                if(cluster.Count>=14)break;
                if(flame&&flame.isActiveAndEnabled&&!guttering.Contains(flame)&&(flame.transform.position-seed.transform.position).sqrMagnitude<2.5f*2.5f)cluster.Add(flame);
            }
            foreach(var flame in cluster)guttering.Add(flame);
            StartCoroutine(Gutter(cluster));
            Log("candle_gutter");return true;
        }
        IEnumerator Gutter(List<CandleFlame> cluster)
        {
            var rest=new Vector3[cluster.Count];
            for(int i=0;i<rest.Length;i++)rest[i]=cluster[i]?cluster[i].transform.localScale:Vector3.one;
            const float shrink=.25f,hold=2f,regrow=.6f;
            for(float t=0;t<shrink+hold+regrow;t+=Time.deltaTime)
            {
                float k=t<shrink?Mathf.Lerp(1,.2f,t/shrink):t<shrink+hold?.2f:Mathf.Lerp(.2f,1,(t-shrink-hold)/regrow);
                for(int i=0;i<rest.Length;i++)if(cluster[i])cluster[i].transform.localScale=rest[i]*k;
                yield return null;
            }
            for(int i=0;i<rest.Length;i++)
            {
                if(cluster[i])cluster[i].transform.localScale=rest[i];
                guttering.Remove(cluster[i]);
            }
        }

        // ===================================================================================================================
        // Item 8: poltergeist
        // ===================================================================================================================
        // Throws every Throwable within 6 m of the player that is in view (65 degrees, clear line) at once, away from the player in a
        // fan. Returns how many left their place. The integrator calls this one apparition in six instead of ScareDirector.ThrowNearbyProp.
        public int Poltergeist()
        {
            if(!Ready||game.Modal)return 0;
            var eye=Eye;var player=game.Player.transform.position;
            var candidates=new List<Throwable>(Throwable.All);
            int thrown=0;Vector3 sum=Vector3.zero;
            foreach(var item in candidates)
            {
                if(!item||item.Thrown||item.GetComponent<Rigidbody>())continue;
                var centre=item.WorldCenter;var delta=centre-eye.position;
                if(delta.magnitude>6||Vector3.Angle(eye.forward,delta)>65)continue;
                if(Physics.Linecast(eye.position,centre,OcclusionMask,QueryTriggerInteraction.Ignore))continue;
                var away=centre-player;away.y=0;
                away=away.sqrMagnitude<.01f?Random.onUnitSphere:away.normalized;
                away=Quaternion.Euler(0,Random.Range(-70f,70f),0)*away;away.y=0;
                var velocity=away.normalized*Random.Range(2.2f,4.6f)+Vector3.up*Random.Range(1.8f,3.2f);
                if(item.Launch(game,velocity)){thrown++;sum+=centre;}
            }
            if(thrown>0)
            {
                Thrown+=thrown;
                game.Sound.Cue("knock",sum/thrown,.8f);
                Log("poltergeist "+thrown);
            }
            return thrown;
        }

        // ===================================================================================================================
        // Item 9: visual beats with no consequence
        // ===================================================================================================================
        // A flat dark winged silhouette glides across the outside of a window for 1.8 s, chosen from the windows the player sees in the
        // periphery (25 to 70 degrees off the view centre, clear line, within 18 m). False when no window qualifies.
        public bool WindowSilhouette()
        {
            if(!Calm||silhouetteBusy)return false;
            var eye=Eye;var root=Root;
            var options=new List<CastleWindowDef>();
            foreach(var w in CastleLayout.Windows)
            {
                WindowPose(w,out var centre,out var inward);
                var delta=centre-eye.position;
                if(delta.magnitude>18||delta.magnitude<2.5f)continue;
                if(Vector3.Dot(eye.position-centre,inward)<.2f)continue; // the player must be on the room side
                float angle=Vector3.Angle(eye.forward,delta);
                if(angle<25||angle>70)continue;
                if(Physics.Linecast(eye.position,centre+inward*.25f,OcclusionMask,QueryTriggerInteraction.Ignore))continue;
                options.Add(w);
            }
            if(options.Count==0)return false;
            var pick=options[Random.Range(0,options.Count)];
            WindowPose(pick,out var windowCentre,out var windowInward);
            var tangent=root.TransformDirection(pick.RunsAlongX?Vector3.right:Vector3.forward);
            float flip=Random.value<.5f?-1:1;
            float span=pick.Width*.5f+.9f;
            var outside=windowCentre-windowInward*.9f;
            var bat=new GameObject("Window silhouette");
            bat.AddComponent<MeshFilter>().sharedMesh=SurrealProps.CentreQuad;
            var renderer=bat.AddComponent<MeshRenderer>();renderer.sharedMaterial=SilhouetteMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            var crossing=bat.AddComponent<CrossingBat>();
            crossing.flap=false;crossing.wingspan=Mathf.Clamp(pick.Width*1.3f,1.1f,2.2f);
            crossing.Begin(eye,outside-tangent*span*flip,outside+tangent*span*flip,1.8f,crossing.wingspan);
            crossing.OnDone=()=>silhouetteBusy=false;
            silhouetteBusy=true;
            Log("window_silhouette");return true;
        }
        void WindowPose(CastleWindowDef w,out Vector3 centre,out Vector3 inward)
        {
            float cy=w.FloorY+(w.Sill+w.Top)*.5f;
            var local=w.RunsAlongX?new Vector3(w.Center,cy,w.Line):new Vector3(w.Line,cy,w.Center);
            var normal=w.RunsAlongX?new Vector3(0,0,w.Inward):new Vector3(w.Inward,0,0);
            centre=Root.TransformPoint(local);inward=Root.TransformDirection(normal);
        }

        // A dark upright shape stands behind the great-hall gallery balustrade. It shows only while the camera is not pointed within 20
        // degrees of it, and is gone for good the first time the player looks straight at it (or after 40 s, or when the player leaves the
        // hall floor). The call arms it; it appears as soon as a gallery spot sits 28 to 65 degrees off the view centre.
        bool shapeArmed;GameObject shape;float shapeAge,shapeArmedTime,shapeFade;Vector3 shapeChest;Material shapeMaterial;
        public bool BalustradeShape()
        {
            if(!Calm||shape||shapeArmed||!OnHallFloor())return false;
            shapeArmed=true;shapeArmedTime=0;
            return true;
        }
        bool OnHallFloor()
        {
            var p=Root.InverseTransformPoint(game.Player.transform.position);
            return Mathf.Abs(p.x)<3.2f&&p.z>14.3f&&p.z<27.3f&&p.y>-.3f&&p.y<1f;
        }
        void TryPlaceShape()
        {
            shapeArmedTime+=Time.deltaTime;
            if(shapeArmedTime>25||!Calm){shapeArmed=false;return;}
            if(!OnHallFloor())return;
            var eye=Eye;
            Vector3 chosen=default;bool found=false;float bestScore=-1;
            foreach(int side in new[]{-1,1})
                for(float z=15.2f;z<=25.6f;z+=1.6f)
                {
                    var feet=Root.TransformPoint(new Vector3(side*3.45f,3.2f,z));
                    var chest=feet+Vector3.up*1.35f;
                    var delta=chest-eye.position;
                    float angle=Vector3.Angle(eye.forward,delta);
                    if(delta.magnitude<4||delta.magnitude>17||angle<28||angle>65)continue;
                    if(Physics.Linecast(eye.position,feet+Vector3.up*1.9f,OcclusionMask,QueryTriggerInteraction.Ignore))continue;
                    float score=delta.magnitude+Random.value*3;
                    if(score>bestScore){bestScore=score;chosen=feet;found=true;}
                }
            if(!found)return;
            shapeArmed=false;shapeAge=0;shapeFade=0;
            shape=new GameObject("Gallery shape");
            shape.transform.position=chosen+Vector3.up*1.05f;
            shapeChest=chosen+Vector3.up*1.35f;
            shape.AddComponent<MeshFilter>().sharedMesh=SurrealProps.CentreQuad;
            var renderer=shape.AddComponent<MeshRenderer>();
            shapeMaterial=SurrealProps.Unlit("Gallery shape",FigureTexture,new Color(0,0,0,0));
            renderer.sharedMaterial=shapeMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            shape.transform.localScale=new Vector3(.8f,2.1f,1);
            Log("balustrade_shape");
        }
        void TickShape()
        {
            if(!Ready){KillShape(false);return;}
            shapeAge+=Time.deltaTime;
            var eye=Eye;
            if(shapeAge>40||!OnHallFloor()||!Calm){KillShape(false);return;}
            var delta=shapeChest-eye.position;
            float angle=Vector3.Angle(eye.forward,delta);
            if(angle<20){KillShape(true);return;}
            // faces the viewer about the vertical axis, so it reads as a flat upright figure
            var flat=Vector3.ProjectOnPlane(delta,Vector3.up);
            if(flat.sqrMagnitude>1e-4f)shape.transform.rotation=Quaternion.LookRotation(flat,Vector3.up);
            shapeFade=Mathf.MoveTowards(shapeFade,1,Time.deltaTime/.7f);
            shapeMaterial.color=new Color(0,0,0,shapeFade*.92f);
        }
        void KillShape(bool lookedAt)
        {
            if(shape)Destroy(shape);
            if(shapeMaterial)Destroy(shapeMaterial);
            shape=null;shapeMaterial=null;shapeArmed=false;
            if(lookedAt)Debug.Log("HC_SCARE_CASTLE balustrade_shape_gone");
        }

        // ===================================================================================================================
        // Item 5: false tells
        // ===================================================================================================================
        // Every candle flame leans toward an empty doorway 6 to 10 m from the player for 4 s, then relaxes.
        public bool FalseCandleTell()
        {
            if(!Ready||game.Modal)return false;
            var player=game.Player.transform.position;
            var options=new List<Vector3>();
            foreach(var door in CastleLayout.Doors)
            {
                if(door.Kind==CastleDoorKind.Gate)continue;
                var point=Root.TransformPoint(door.Position+Vector3.up*1f);
                var flat=point-player;
                if(Mathf.Abs(flat.y)>2.2f)continue;
                flat.y=0;
                float d=flat.magnitude;
                if(d<6||d>10)continue;
                if(game.Creature&&game.Creature.isActiveAndEnabled&&Vector3.Distance(game.Creature.transform.position,point)<5)continue; // the doorway must be empty
                options.Add(point);
            }
            if(options.Count==0)return false;
            Floater.FalseTarget(options[Random.Range(0,options.Count)],4f);
            Log("false_candle_tell");return true;
        }
        // Lets the integrator's compass needle spin at nothing for 3 s.
        public void FalseCompassReading()
        {
            FalseCompass=true;falseCompassTimer=3f;
            Log("false_compass");
        }

        // ---- generated textures ----------------------------------------------------------------------------------------------------
        static Material SilhouetteMaterial
        {
            get
            {
                if(!silhouetteMaterial)silhouetteMaterial=SurrealProps.Unlit("Window silhouette",WingTexture,new Color(0,0,0,1));
                return silhouetteMaterial;
            }
        }
        static float Smooth(float a,float b,float x){float k=Mathf.Clamp01((x-a)/(b-a));return k*k*(3-2*k);}
        // A black bat seen from the front: a small body, ears, two scalloped wings rising to hooked tips. Only the alpha matters.
        static Texture2D WingTexture
        {
            get
            {
                if(wingTexture)return wingTexture;
                wingTexture=SurrealProps.MakeTexture("Window silhouette wings",128,64,(u,v)=>
                {
                    float x=Mathf.Abs(u-.5f)*2; // 0 at the body, 1 at the wing tip
                    float alpha=0;
                    float body=Mathf.Clamp01(1-(x*x/(.07f*.07f)+(v-.5f)*(v-.5f)/(.2f*.2f)));
                    float ears=(v>.68f&&v<.86f)?Mathf.Clamp01(1-Mathf.Abs(x-.045f)/Mathf.Max(.001f,.035f*(.86f-v)/.18f)):0;
                    alpha=Mathf.Max(body>0?1:0,ears>0?1:0);
                    if(x>.05f)
                    {
                        float top=.55f+.35f*Mathf.Pow(x,1.5f);
                        float tip=Mathf.Pow(x,4);
                        float bottom=Mathf.Lerp(.5f-.22f*(1-x)-.09f*Mathf.Abs(Mathf.Sin(x*3*Mathf.PI))*(1-tip),top,tip);
                        float coverage=Smooth(0,.04f,v-bottom)*Smooth(0,.04f,top-v);
                        alpha=Mathf.Max(alpha,coverage);
                    }
                    return new Color32(0,0,0,(byte)Mathf.RoundToInt(alpha*255));
                });
                return wingTexture;
            }
        }
        // A tall cloaked figure with a small head: only the alpha matters.
        static Texture2D FigureTexture
        {
            get
            {
                if(figureTexture)return figureTexture;
                figureTexture=SurrealProps.MakeTexture("Gallery shape figure",64,128,(u,v)=>
                {
                    float x=Mathf.Abs(u-.5f)*2; // 0 at the centre line, 1 at the texture edge
                    float head=Mathf.Clamp01(1-(x*x/(.2f*.2f)+(v-.9f)*(v-.9f)/(.07f*.07f)));
                    float neck=(v>.78f&&v<=.84f)?Smooth(.16f,.1f,x):0;
                    float shoulders=Smooth(.8f,.62f,v); // 0 near the head, 1 below the shoulders
                    float half=Mathf.Lerp(.1f,.62f,shoulders)*(1-.18f*Smooth(.3f,0,v));
                    float body=v<.8f?Smooth(half+.04f,half-.04f,x):0;
                    float alpha=Mathf.Max(head>0?1:0,Mathf.Max(neck,body));
                    return new Color32(0,0,0,(byte)Mathf.RoundToInt(alpha*255));
                });
                return figureTexture;
            }
        }
        void OnDestroy()
        {
            Floater.ClearFalseTarget();
            if(shape)Destroy(shape);
            if(shapeMaterial)Destroy(shapeMaterial);
        }
    }

    // First hard impact plays a cue (a book hitting the floor). Runtime only; never saved into a scene.
    public sealed class CueOnImpact : MonoBehaviour
    {
        public Soundscape Sound;public string Key;public float Volume=.8f;bool done;
        void OnCollisionEnter(Collision collision)
        {
            if(done||collision.relativeVelocity.magnitude<.8f)return;
            done=true;if(Sound)Sound.Cue(Key,transform.position,Volume);
        }
    }

    // A camera-facing flat creature that travels from `start` to `end` and destroys itself. With flap it narrows and widens like a
    // bat in flight (the GiantBat formula). Runtime only; never saved into a scene.
    public sealed class CrossingBat : MonoBehaviour
    {
        public bool flap=true;public float wingspan=1.5f;public System.Action OnDone;
        Transform viewer;Vector3 start,end;float duration,elapsed;
        public void Begin(Transform eye,Vector3 from,Vector3 to,float seconds,float span)
        {
            viewer=eye;start=from;end=to;duration=Mathf.Max(.2f,seconds);wingspan=span;elapsed=0;
            transform.position=from;
        }
        void Update()
        {
            elapsed+=Time.deltaTime;
            float k=Mathf.Clamp01(elapsed/duration);
            var position=Vector3.Lerp(start,end,k);
            position.y+=Mathf.Sin(k*Mathf.PI*2.4f)*.14f+Mathf.Sin(k*Mathf.PI)*.18f;
            transform.position=position;
            float beat=flap?Mathf.Abs(Mathf.Cos(elapsed*2.4f*Mathf.PI*2)):1;
            transform.localScale=new Vector3(wingspan*(.4f+.6f*beat),wingspan*.5f*(.92f+.08f*beat),1);
            if(viewer)
            {
                var to=position-viewer.position;
                if(to.sqrMagnitude>1e-4f)transform.rotation=Quaternion.LookRotation(to,Vector3.up)*Quaternion.Euler(0,0,Mathf.Sin(elapsed*6)*10f);
            }
            if(k>=1)Done();
        }
        void Done(){var done=OnDone;OnDone=null;if(done!=null)done();Destroy(gameObject);}
        void OnDestroy(){var done=OnDone;OnDone=null;if(done!=null)done();}
    }
}
