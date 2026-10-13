using System.Collections.Generic;
using UnityEngine;

namespace HollowCurrent
{
    // Weightless candelabras and books: a slow vertical bob, a tiny drift and a
    // lazy tilt. Child flame transforms lean toward flameTarget (Count Claude)
    // when it is within bendRange, so the candles give him away before he is
    // seen. The hunter sets every target at once with Floater.SetFlameTarget.
    public sealed class Floater : MonoBehaviour
    {
        // Every enabled floater. Read-only for callers; do not Add/Remove.
        public static readonly List<Floater> All=new List<Floater>();
        static Transform globalTarget;
        public static Transform GlobalFlameTarget=>globalTarget;
        // One call for the hunter: sets flameTarget on every floater, including
        // ones enabled later. Pass null to let the flames relax.
        public static void SetFlameTarget(Transform target)
        {
            globalTarget=target;
            for(int i=0;i<All.Count;i++)if(All[i])All[i].flameTarget=target;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics(){All.Clear();globalTarget=null;}

        [Header("Float")]
        [Tooltip("Vertical bob half-height in metres.")] public float bobAmplitude=.12f;
        [Tooltip("Bob cycles per second.")] public float bobHertz=.22f;
        [Tooltip("Horizontal drift radius in metres.")] public float driftRadius=.05f;
        [Tooltip("Drift cycles per second.")] public float driftHertz=.07f;
        [Tooltip("Slow spin about the vertical axis, degrees per second.")] public float yawDegreesPerSecond=2.5f;
        [Tooltip("Tilt wobble in degrees (books like this larger, candelabras smaller).")] public float tiltDegrees=2.5f;
        [Tooltip("Phase offset in cycles. Negative derives one from the start position, so floaters never bob in unison.")]
        public float phase=-1;

        [Header("Candle tell")]
        [Tooltip("Flames lean toward this transform. Set for all floaters with Floater.SetFlameTarget.")]
        public Transform flameTarget;
        [Tooltip("Flame transforms (usually CandleFlame objects) that lean. Their local rotation at start is the rest pose.")]
        public Transform[] flames;
        [Tooltip("Target distance in metres at which flames start to bend. Bend grows as the target closes in.")]
        public float bendRange=12;
        [Tooltip("Lean at full bend, degrees from vertical.")]
        [Range(0,80)] public float maxBendDegrees=38;
        [Tooltip("Bend per second while the target approaches (fraction of full lean).")] public float bendSpeed=2.5f;
        [Tooltip("Bend per second while the flames relax (1 = about one second from full lean to upright).")] public float relaxSpeed=1f;

        // 0 = upright, 1 = fully leaning. Readable for audio or debug.
        public float Bend01 {get;private set;}
        public bool IsBending=>Bend01>.01f;

        Vector3 basePosition;
        Quaternion baseRotation;
        Quaternion[] flameRest;
        bool ready,flamesDirty,haveLastTarget;
        Vector3 lastTargetPosition;
        void OnEnable()
        {
            if(!All.Contains(this))All.Add(this);
            if(!flameTarget&&globalTarget)flameTarget=globalTarget;
        }
        void OnDisable(){All.Remove(this);}
        void Start(){Rebase();}
        // Capture the current pose as the rest pose. Call after moving a floater by code.
        public void Rebase()
        {
            basePosition=transform.localPosition;baseRotation=transform.localRotation;
            if(phase<0)
            {
                var p=transform.position;
                phase=Mathf.Repeat(Mathf.Abs(Mathf.Sin(p.x*12.9898f+p.y*37.719f+p.z*78.233f))*43758.5453f,1);
            }
            int n=flames==null?0:flames.Length;
            if(flameRest==null||flameRest.Length!=n)flameRest=new Quaternion[n];
            for(int i=0;i<n;i++)if(flames[i])flameRest[i]=flames[i].localRotation;
            ready=true;
        }
        void Update()
        {
            if(!ready)return;
            float t=Time.time;
            float cycle=(t+phase*100f);
            float bob=Mathf.Sin(cycle*bobHertz*Mathf.PI*2)*bobAmplitude;
            float dx=Mathf.Sin(cycle*driftHertz*Mathf.PI*2+1.3f)*driftRadius;
            float dz=Mathf.Cos(cycle*driftHertz*Mathf.PI*2*.83f)*driftRadius;
            transform.localPosition=basePosition+new Vector3(dx,bob,dz);
            float yaw=cycle*yawDegreesPerSecond+phase*360f;
            transform.localRotation=baseRotation*Quaternion.Euler(Mathf.Sin(cycle*.31f)*tiltDegrees,yaw,Mathf.Cos(cycle*.23f)*tiltDegrees);
            UpdateFlames(Time.deltaTime);
        }
        void UpdateFlames(float dt)
        {
            if(flames==null||flames.Length==0)return;
            float want=0;
            bool hasTarget=flameTarget; // false for null and for a destroyed target
            // Remember where the target was: flames that are still leaning when it is cleared ease back up while
            // still pointing at that spot, so there is never a dereference of a missing target.
            if(hasTarget){lastTargetPosition=flameTarget.position;haveLastTarget=true;}
            if(hasTarget&&bendRange>.01f)
            {
                float d=Vector3.Distance(flameTarget.position,transform.position);
                float k=1-Mathf.Clamp01(d/bendRange);
                want=k*k*(3-2*k); // smoothstep: nothing at range, firm lean up close
            }
            Bend01=want>Bend01?Mathf.MoveTowards(Bend01,want,bendSpeed*dt):Mathf.MoveTowards(Bend01,want,relaxSpeed*dt);
            if(Bend01<=0)
            {
                if(flamesDirty)
                {
                    for(int i=0;i<flames.Length;i++)if(flames[i]&&i<flameRest.Length)flames[i].localRotation=flameRest[i];
                    flamesDirty=false;
                }
                return;
            }
            if(!haveLastTarget)Bend01=0;
            flamesDirty=true;
            float degrees=maxBendDegrees*Bend01;
            for(int i=0;i<flames.Length;i++)
            {
                var flame=flames[i];if(!flame||i>=flameRest.Length)continue;
                var parent=flame.parent;
                var rest=parent?parent.rotation*flameRest[i]:flameRest[i];
                var to=lastTargetPosition-flame.position;to.y=0;
                if(to.sqrMagnitude<1e-4f){flame.rotation=rest;continue;}
                // Rotating up about cross(up, direction) tips the flame toward the direction.
                flame.rotation=Quaternion.AngleAxis(degrees,Vector3.Cross(Vector3.up,to.normalized))*rest;
            }
        }
    }
}
