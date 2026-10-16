using UnityEngine;
namespace HollowCurrent
{
    public sealed class Door : MonoBehaviour
    {
        public Quaternion Closed;
        public bool Open;
        // Swing-clearance probe in hinge space (centre and half extents). Defaults fit
        // Bellwether's 1.6 x 2.2 m leaves; castle doors are sized from their leaf.
        public Vector3 ProbeCenter=new Vector3(.82f,1.1f,0),ProbeExtents=new Vector3(.74f,1.02f,.055f);
        // Fit the probe to a leaf of width x height whose hinge is at local x=0, floor y=0.
        // Re-measure now and return the widest clear swing in degrees (placement tools).
        public float MeasureSwing(){Measure();return Mathf.Abs(openAngle);}
        public void FitProbe(float width,float height)
        {
            ProbeCenter=new Vector3((width+.04f)*.5f,height*.5f,0);ProbeExtents=new Vector3((width-.12f)*.5f,height*.5f-.08f,.055f);measured=false;
        }
        float openAngle=-110;
        readonly Collider[] contacts=new Collider[32];
        bool measured;
        float positive,negative;
        public float OpenAngle=>openAngle;
        public bool HasClearOpenPose=>measured&&ClearAt(openAngle);
        void Measure()
        {
            Physics.SyncTransforms();positive=ClearArc(1);negative=ClearArc(-1);measured=true;
            openAngle=negative>=positive?-negative:positive;
        }
        float ClearArc(int sign)
        {
            float clear=0;
            for(float angle=5;angle<=175;angle+=5)
            {
                if(!ClearAt(sign*angle))break;
                clear=angle;
            }
            return clear;
        }
        // Diagnostics (editor validators): the first collider blocking the leaf at an angle.
        public string BlockerAt(float angle)
        {
            var rotation=(transform.parent?transform.parent.rotation:Quaternion.identity)*Closed*Quaternion.Euler(0,angle,0);
            int count=Physics.OverlapBoxNonAlloc(transform.position+rotation*ProbeCenter,ProbeExtents,contacts,rotation,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(contacts[i]&&!contacts[i].transform.IsChildOf(transform))return contacts[i].name+" ("+(contacts[i].transform.parent?contacts[i].transform.parent.name:"")+")";
            return "none";
        }
        bool ClearAt(float angle)
        {
            var rotation=(transform.parent?transform.parent.rotation:Quaternion.identity)*Closed*Quaternion.Euler(0,angle,0);
            var center=transform.position+rotation*ProbeCenter;
            int count=Physics.OverlapBoxNonAlloc(center,ProbeExtents,contacts,rotation,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore);
            if(count==contacts.Length)return false;
            for(int i=0;i<count;i++)if(contacts[i]&&!contacts[i].transform.IsChildOf(transform))return false;
            return true;
        }
        // ---- swing direction ---------------------------------------------------------------------------------------------
        // The leaf extends +X from the hinge; a negative angle swings it toward +Z, a positive angle toward -Z. Open away from the player
        // whenever that side has at least MinAwayClear degrees of clear arc, else take the widest clear arc (fb-doors, owner item 4).
        public const float MinAwayClear=85;
        // Chooses openAngle for an opening that starts now. Returns the side the player stands on (local z sign) for the log.
        float ChooseSwing(Game game)
        {
            Measure();
            float side=0;
            if(game&&game.Player)
            {
                var closed=(transform.parent?transform.parent.rotation:Quaternion.identity)*Closed;
                var local=Quaternion.Inverse(closed)*(game.Player.transform.position-transform.position);
                side=local.z;
                if(local.z<0&&negative>=MinAwayClear)openAngle=-negative;
                else if(local.z>=0&&positive>=MinAwayClear)openAngle=positive;
            }
            if(NativeVerification.Requested)Debug.Log("HC_DOOR_STATE "+name+" pivot="+transform.position+" positive="+positive+" negative="+negative+" playerSide="+(side<0?"-z":"+z")+" selected="+openAngle);
            return side;
        }
        public void Toggle()
        {
            if(!measured)Measure();
            var game=FindFirstObjectByType<Game>();
            if(!Open)ChooseSwing(game);
            Open=!Open;lastOpen=Open;BeginMotion(game);
            if(game&&game.Sound)game.Sound.Cue(Open?"door":"latch",transform.position+Vector3.up);
        }

        // ---- never push the player (fb-doors, owner item 5) --------------------------------------------------------------
        // A leaf that sweeps through the CharacterController makes the controller depenetrate, so the door shoved the player. While the leaf
        // moves, for SettleSeconds after it stops, and for as long as the controller still overlaps a leaf collider, the controller ignores the
        // door's colliders; they come back the moment the player stands clear (a closed door still stops you, an open one is a solid leaf).
        // The Count and the hunter navigation never use these colliders as a body, and Door.Open / Toggle behave exactly as before.
        public const float SettleSeconds=.5f;
        Collider[] leafColliders;
        CharacterController ignored;
        bool want,applied,lastOpen;
        float settleUntil;
        public bool IgnoringPlayer=>want;
        void BeginMotion(Game game)
        {
            settleUntil=Time.time+SettleSeconds;want=true;Apply(game);
        }
        // Idempotent and repeated every frame while wanted: the controller's collider is switched off and on by Investigator.Teleport (hiding),
        // and an ignored pair may not survive that, so the pair is asserted again rather than trusted.
        void Apply(Game game)
        {
            var cc=game&&game.Player?game.Player.Controller:null;
            if(!cc)return;
            if(ignored!=cc){if(ignored&&applied)Set(ignored,false);ignored=cc;applied=false;}
            if(!cc.enabled||!cc.gameObject.activeInHierarchy)return;
            if(want){Set(cc,true);applied=true;}
            else if(applied){Set(cc,false);applied=false;}
        }
        void Set(CharacterController cc,bool ignore)
        {
            if(leafColliders==null)leafColliders=GetComponentsInChildren<Collider>(true);
            foreach(var c in leafColliders)if(c&&c.enabled&&!c.isTrigger&&c!=cc)Physics.IgnoreCollision(c,cc,ignore);
        }
        bool PlayerOverlapsLeaf()
        {
            if(!ignored||leafColliders==null)return false;
            var body=ignored.bounds;body.Expand(.06f);
            foreach(var c in leafColliders)if(c&&!c.isTrigger&&c.enabled&&c.bounds.Intersects(body))return true;
            return false;
        }
        void OnDisable(){if(ignored&&applied)Set(ignored,false);want=false;applied=false;}
        void Update()
        {
            if(!measured)Measure();
            var game=Owner;
            // Something other than Toggle (the hunter walking through) set Open: pick the swing the same way and treat it as motion.
            if(Open!=lastOpen){lastOpen=Open;if(Open)ChooseSwing(game);BeginMotion(game);}
            var target=Closed*Quaternion.Euler(0,Open?openAngle:0,0);
            transform.localRotation=Quaternion.RotateTowards(transform.localRotation,target,Time.deltaTime*150);
            bool moving=Quaternion.Angle(transform.localRotation,target)>.05f;
            if(moving){settleUntil=Time.time+SettleSeconds;want=true;}
            else if(want&&Time.time>=settleUntil&&!PlayerOverlapsLeaf())want=false;
            if(want||applied)Apply(game);
        }
        Game cachedGame;
        Game Owner{get{if(!cachedGame)cachedGame=FindFirstObjectByType<Game>();return cachedGame;}}
    }
}
