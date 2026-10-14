using System.Collections.Generic;
using UnityEngine;

namespace HollowCurrent
{
    // Marks a small castle prop (goblet, candlestick, plate, book, kitchen knife) that a scare may throw. It adds no Rigidbody and no
    // collider until it is thrown, so it costs nothing in the scene and ScareDirector.ThrowNearbyProp accepts it (visual bounds
    // <= 0.4 m per axis, at least one MeshFilter). The integrator feeds every Throwable's transform to the director's prop list;
    // CastleScares.Poltergeist throws all of them near the player at once through Launch.
    public sealed class Throwable : MonoBehaviour
    {
        public const float MaxExtent=.4f;
        // Every enabled throwable. Read only for callers.
        public static readonly List<Throwable> All=new List<Throwable>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics(){All.Clear();}
        public bool Thrown {get;private set;}
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable(){All.Remove(this);}

        // Visual bounds in this transform's local space, the same measure ScareDirector applies. False when there is no mesh.
        public bool TryLocalBounds(out Bounds bounds)
        {
            bounds=new Bounds();bool first=true;
            foreach(var filter in GetComponentsInChildren<MeshFilter>())
            {
                if(!filter.sharedMesh)continue;
                var b=filter.sharedMesh.bounds;var matrix=transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                for(int corner=0;corner<8;corner++)
                {
                    var point=matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                    if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
                }
            }
            return !first;
        }
        public bool BoundsValid
        {
            get{return TryLocalBounds(out var b)&&b.size.x<=MaxExtent&&b.size.y<=MaxExtent&&b.size.z<=MaxExtent;}
        }
        public Vector3 WorldCenter
        {
            get
            {
                var renderers=GetComponentsInChildren<Renderer>();
                if(renderers.Length==0)return transform.position;
                var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
                return b.center;
            }
        }

        // True when any of its renderers was merged into a static batch. A batched renderer never moves with its transform and its
        // MeshFilter reports the whole combined batch mesh, so it cannot be thrown (NativeBuild exempts Throwables from batching).
        public bool StaticBatched
        {
            get{foreach(var r in GetComponentsInChildren<Renderer>())if(r.isPartOfStaticBatch)return true;return false;}
        }

        // Same occlusion mask as ScareDirector and CastleScares (ignore raycast layer 2 and layer 30).
        public const int SightMask=~((1<<2)|(1<<30));
        static readonly RaycastHit[] sightHits=new RaycastHit[32];
        // True when something solid stands between `from` and `to`. A BoxCollider that contains `to` is not an obstacle: it is the
        // collision proxy of the furniture the prop stands on. The castle's table proxies take the art's nominal height (LongTable
        // 0.95 m against a 0.775 m top, DiningTable 0.9 against 0.792), so a plain Linecast to a goblet ends inside the table box and
        // reports it blocked. `blocker` names the first real obstacle ("name@hit/length").
        public static bool SightBlocked(Vector3 from,Vector3 to,out string blocker)
        {
            blocker=null;var line=to-from;float length=line.magnitude;if(length<1e-4f)return false;
            int count=Physics.RaycastNonAlloc(from,line/length,sightHits,length,SightMask,QueryTriggerInteraction.Ignore);
            float nearest=float.MaxValue;
            for(int i=0;i<count;i++)
            {
                var c=sightHits[i].collider;if(!c||Encloses(c,to))continue;
                if(sightHits[i].distance<nearest){nearest=sightHits[i].distance;blocker=c.name+"@"+nearest.ToString("F2")+"/"+length.ToString("F2");}
            }
            return blocker!=null;
        }
        static bool Encloses(Collider c,Vector3 point)
        {
            var box=c as BoxCollider;if(!box)return false;
            var p=box.transform.InverseTransformPoint(point)-box.center;var half=box.size*.5f;
            return Mathf.Abs(p.x)<=half.x&&Mathf.Abs(p.y)<=half.y&&Mathf.Abs(p.z)<=half.z;
        }

        // Why a throw candidate was passed over: one "HC_THROW_SKIP reason=..." line per rejected candidate that is near enough to
        // matter (thrown props and props beyond 9 m are only counted), then one summary line when nothing was thrown.
        public static void LogSkip(string source,string reason,Transform prop,float distance,float angle,string detail,Dictionary<string,int> counts)
        {
            counts.TryGetValue(reason,out int n);counts[reason]=n+1;
            if(reason=="thrown"||distance>9)return;
            Debug.Log("HC_THROW_SKIP reason="+reason+" source="+source+" prop="+prop.name+" at="+prop.position.ToString("F2")+" d="+distance.ToString("F1")+" angle="+angle.ToString("F0")+(detail!=null?" detail="+detail:""));
        }
        public static void LogSkipSummary(string source,Dictionary<string,int> counts,int candidates)
        {
            var text=new System.Text.StringBuilder("HC_THROW_SKIP summary source="+source+" candidates="+candidates);
            foreach(var pair in counts)text.Append(' ').Append(pair.Key).Append('=').Append(pair.Value);
            Debug.Log(text.ToString());
        }

        // Gives the prop a collider and a Rigidbody and kicks it along `velocity` (m/s, world) with a random tumble. Returns false when it
        // was thrown already or its bounds break the 0.4 m rule.
        public bool Launch(Game game,Vector3 velocity)
        {
            if(Thrown||GetComponent<Rigidbody>())return false;
            if(!TryLocalBounds(out var bounds)||bounds.size.x>MaxExtent||bounds.size.y>MaxExtent||bounds.size.z>MaxExtent)
            {Debug.LogWarning("HC_THROWABLE_BOUNDS_INVALID "+name+" "+bounds);return false;}
            Thrown=true;
            var collider=gameObject.AddComponent<BoxCollider>();collider.center=bounds.center;collider.size=bounds.size;
            if(game&&game.Player&&game.Player.Controller)Physics.IgnoreCollision(collider,game.Player.Controller);
            var body=gameObject.AddComponent<Rigidbody>();body.mass=.25f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.interpolation=RigidbodyInterpolation.Interpolate;
            if(game){var impact=gameObject.AddComponent<ScareImpact>();impact.Sound=game.Sound;}
            body.AddForce(velocity,ForceMode.VelocityChange);
            body.AddTorque(new Vector3(Random.Range(3f,9f),Random.Range(6f,14f),Random.Range(2f,7f)),ForceMode.VelocityChange);
            return true;
        }
    }
}
