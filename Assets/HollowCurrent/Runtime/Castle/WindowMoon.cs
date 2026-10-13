using UnityEngine;

namespace HollowCurrent
{
    // A moon sprite painted into a window opening. It only exists for viewers inside the castle footprint: from outside the
    // real skybox moons are the only moons (otherwise the S4 view would show four).
    public sealed class WindowMoon : MonoBehaviour
    {
        public Renderer disc;
        public Transform castleRoot;
        public float margin=.2f;
        bool shown=true;
        void Update()
        {
            if(!disc||!castleRoot)return;
            if(!SurrealProps.TryGetViewer(out var viewer))return;
            var p=castleRoot.InverseTransformPoint(viewer);
            bool inside=p.x>-CastleLayout.HalfWidth-margin&&p.x<CastleLayout.HalfWidth+margin&&p.z>-margin&&p.z<CastleLayout.Depth+margin;
            if(inside!=shown){shown=inside;disc.enabled=inside;}
        }
    }
}
