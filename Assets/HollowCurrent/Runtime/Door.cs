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
        // Wave FD (castle only; r13 critic saw the Hall great doors swing toward a hall-side viewer). Castle hinges sit on the jamb corner, so
        // the clearance probe's near-hinge corner meets the jamb at about 55-60 degrees on one side (tools/check_castle_doors.py --swing: 55 / 155
        // for the Hall, Dining hall and Vestibule hall leaves). The old rule then fell back to the widest arc, i.e. toward a player on that side.
        // Now the castle opens away whenever that arc is at least MinAwayPartial AND still leaves MinPassWidth of the opening free
        // (leaf width x (1 - cos arc): a 1.5 m leaf at 55 degrees leaves 0.64 m for a 0.54 m player); otherwise the widest arc, as before.
        public const float MinAwayPartial=45,MinPassWidth=.62f;
        public float LeafWidth=>ProbeCenter.x*2-.04f;
        bool Passable(float arc)=>arc>=MinAwayPartial&&LeafWidth*(1-Mathf.Cos(arc*Mathf.Deg2Rad))>=MinPassWidth;
        // Diagnostics of the last opening (doorproof, capture): the opener's side in the closed hinge frame (-1 / +1, 0 = nobody) and
        // whether the chosen angle swings the leaf away from that side.
        public int LastOpenerSide{get;private set;}
        public bool LastSwingAway{get;private set;}
        public string LastOpener{get;private set;}="none";
        // The opener's side of the closed leaf: local z in the closed hinge frame.
        public float SideOf(Vector3 point)
        {
            var closed=(transform.parent?transform.parent.rotation:Quaternion.identity)*Closed;
            return (Quaternion.Inverse(closed)*(point-transform.position)).z;
        }
        // Chooses openAngle for an opening that starts now, away from the player. Returns the side the player stands on (local z) for the log.
        float ChooseSwing(Game game)=>ChooseSwing(game,game&&game.Player?game.Player.transform.position:(Vector3?)null,"player");
        // The same rule from any viewpoint (fd-rules: the capture camera's position, so stills use the real swing logic).
        float ChooseSwing(Game game,Vector3? opener,string who)
        {
            Measure();
            float side=0;bool castle=game&&game.Map==MapId.Castle;
            if(opener.HasValue)
            {
                side=SideOf(opener.Value);
                float away=side<0?negative:positive;
                if(away>=MinAwayClear||castle&&Passable(away))openAngle=side<0?-away:away;
            }
            LastOpenerSide=!opener.HasValue?0:side<0?-1:1;LastOpener=who;
            // A negative angle swings the leaf toward +Z: away from an opener on the -Z side.
            LastSwingAway=opener.HasValue&&(side<0)==(openAngle<0);
            if(NativeVerification.Requested||castle)
                Debug.Log("HC_DOOR_STATE name=\""+name+"\" side="+(side<0?"-z":"+z")+" swing="+(!opener.HasValue?"none":LastSwingAway?"away":"toward")+" angle="+openAngle.ToString("F0")+
                    " by="+who+" positive="+positive+" negative="+negative+" pivot="+transform.position);
            return side;
        }
        // by: "E" from the player's interaction (Interactable.Use), "direct" from code (drivers, editor tools). fg-integ: a castle close logs
        // HC_DOOR_STATE ... state=closed closed_by=<by> (doorproof counts closed_by=E).
        public void Toggle(string by="direct")
        {
            if(!measured)Measure();
            var game=FindFirstObjectByType<Game>();
            if(!Open)ChooseSwing(game);
            else if(game&&game.Map==MapId.Castle)Debug.Log("HC_DOOR_STATE name=\""+name+"\" state=closed closed_by="+by+" angle="+openAngle.ToString("F0")+" leafRot="+transform.localEulerAngles.y.ToString("F0"));
            Open=!Open;lastOpen=Open;BeginMotion(game);
            if(game&&game.Sound)game.Sound.Cue(Open?"door":"latch",transform.position+Vector3.up);
        }
        // Open (if closed) choosing the swing from this viewpoint instead of the player's (capture stills). No sound.
        public void OpenFrom(Vector3 viewer,string who)
        {
            if(!measured)Measure();
            if(Open)return;
            var game=Owner;ChooseSwing(game,viewer,who);
            Open=true;lastOpen=true;BeginMotion(game);
        }
        // Where the leaf actually is now (its probe centre in the closed hinge frame): -1 / +1 side, for the doorproof frame check.
        public int LeafSideNow()
        {
            var center=transform.position+transform.rotation*ProbeCenter;
            float z=SideOf(center);return Mathf.Abs(z)<.05f?0:z<0?-1:1;
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
            if(Open!=lastOpen){lastOpen=Open;if(Open)ChooseSwing(game,game&&game.Player?game.Player.transform.position:(Vector3?)null,"auto");BeginMotion(game);}
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
