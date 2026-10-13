using System.Collections.Generic;
using UnityEngine;

namespace HollowCurrent
{
    // Tags a set piece (altar sockets, crypt vault arches, niches, keep, plank deck, dais and chandelier) with the capture shot that
    // is meant to show it, so capture mode can log how much of the frame it fills: HC_SETPIECE <shot> <name> <h>% of frame height.
    // Bounds come from the renderers under this object, or from an explicit box (local to this transform) when `explicitSize` is set.
    // A MonoBehaviour in a castle scene keeps its object out of static GI, so tag only small groups.
    public sealed class SetPiece : MonoBehaviour
    {
        public string label,shot;
        public Vector3 explicitCentre,explicitSize;
        public static readonly List<SetPiece> All=new List<SetPiece>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics(){All.Clear();}
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable(){All.Remove(this);}

        bool WorldBounds(out Bounds bounds)
        {
            bounds=default;bool any=false;
            if(explicitSize.sqrMagnitude>0)
            {
                var c=transform.TransformPoint(explicitCentre);bounds=new Bounds(c,Vector3.zero);
                for(int i=0;i<8;i++)bounds.Encapsulate(transform.TransformPoint(explicitCentre+Vector3.Scale(explicitSize*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
                return true;
            }
            foreach(var r in GetComponentsInChildren<Renderer>())
            {
                if(!r.enabled)continue;
                if(!any){bounds=r.bounds;any=true;}else bounds.Encapsulate(r.bounds);
            }
            return any;
        }

        // Logs, for every set piece tagged with `shot`, the share of the frame height its bounds project to from `cam`
        // (eight corners, clipped to the frame; the numbers ignore occlusion). Call after the shot.
        public static void Report(Camera cam,string shot)
        {
            if(!cam)return;
            int n=0;
            foreach(var s in All)
            {
                if(!s||s.shot!=shot)continue;
                n++;
                if(!s.WorldBounds(out var b)){Debug.Log("HC_SETPIECE "+shot+" "+s.label+" no renderer bounds");continue;}
                float lo=float.MaxValue,hi=float.MinValue;bool behind=false;
                for(int i=0;i<8;i++)
                {
                    var corner=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    var vp=cam.WorldToViewportPoint(corner);
                    if(vp.z<=0){behind=true;continue;}
                    lo=Mathf.Min(lo,vp.y);hi=Mathf.Max(hi,vp.y);
                }
                if(lo>hi){Debug.Log("HC_SETPIECE "+shot+" "+s.label+" 0.0% (behind the camera)");continue;}
                lo=Mathf.Clamp01(lo);hi=Mathf.Clamp01(hi);
                var centreVp=cam.WorldToViewportPoint(b.center);
                Debug.Log("HC_SETPIECE "+shot+" "+s.label+" "+((hi-lo)*100f).ToString("F1")+"% of frame height (viewport y "+lo.ToString("F2")+".."+hi.ToString("F2")+", centre x "+centreVp.x.ToString("F2")+(behind?", part behind camera":"")+")");
            }
            if(n==0)Debug.Log("HC_SETPIECE "+shot+" no set pieces tagged for this shot");
        }
    }
}
