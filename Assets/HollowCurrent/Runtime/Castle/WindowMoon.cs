using UnityEngine;

namespace HollowCurrent
{
    // A moon sprite painted into a window opening. It only exists for viewers inside the castle footprint: from outside the
    // real skybox moons are the only moons (otherwise the S4 view would show four).
    // Wave FF (hc/ff-art, r14 critic: "a moon visible in at least one lancet"): with fixedDirection the disc behaves like a distant moon: every
    // frame it sits where the ray from the viewer along `direction` (castle-local) crosses the plane z = planeZ just outside the north wall, so
    // each viewer sees it in exactly one direction, through whichever opening lines up. From the S1 eye the two moons stand in the north
    // lancets, from the S5 rose-ledge eye in the rose window, beside the Count (the round-6 look). The disc keeps its 0.5 m radius on that
    // plane, so it grows as the viewer walks up to the wall, as the old fixed sprites did. keepMaterial: the component only hides a renderer
    // from outside (CastleWorld.NorthSkyCard).
    public sealed class WindowMoon : MonoBehaviour
    {
        public Renderer disc;
        public bool red;
        public Transform castleRoot;
        public float margin=.2f;
        public bool fixedDirection;
        public Vector3 direction=Vector3.forward;
        public float planeZ=28.85f;
        public bool keepMaterial;
        bool shown=true;
        void Start(){if(keepMaterial)return;var m=SurrealProps.MoonMaterial(red);if(disc&&m)disc.sharedMaterial=m;}
        void Update()
        {
            if(!disc||!castleRoot)return;
            if(!SurrealProps.TryGetViewer(out var viewer))return;
            var p=castleRoot.InverseTransformPoint(viewer);
            bool inside=p.x>-CastleLayout.HalfWidth-margin&&p.x<CastleLayout.HalfWidth+margin&&p.z>-margin&&p.z<CastleLayout.Depth+margin;
            if(inside&&fixedDirection)
            {
                float s=direction.z>1e-4f?(planeZ-p.z)/direction.z:-1f;
                if(s<=.05f)inside=false;
                else{transform.localPosition=p+direction*s;transform.localRotation=Quaternion.LookRotation(direction);}
            }
            if(inside!=shown){shown=inside;disc.enabled=inside;}
        }
    }
}
