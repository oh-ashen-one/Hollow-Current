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
