using UnityEngine;

namespace HollowCurrent
{
    // A flickering candle flame: one camera-facing billboard plus an optional
    // point light. The billboard pivots at its base so a Floater can lean the
    // whole flame toward Count Claude. Nothing here allocates per frame.
    public sealed class CandleFlame : MonoBehaviour
    {
        [Header("Billboard")]
        [Tooltip("Flame billboard (bottom-pivot quad). Optional; the light still flickers without it.")]
        public Transform billboard;
        [Tooltip("Flame height in metres.")]
        public float size=.11f;
        [Tooltip("Flame width as a fraction of its height.")]
        public float widthRatio=.5f;
        [Tooltip("Beyond this distance from the viewer the billboard stops turning toward the camera.")]
        public float billboardRange=40;

        [Tooltip("Optional additive halo billboard (centre pivot); it turns to the viewer and breathes with the flicker.")]
        public Transform glow;
        [Tooltip("Halo diameter in metres.")]
        public float glowSize=.5f;

        [Header("Flicker")]
        [Tooltip("Noise samples per second. Higher is more nervous.")]
        public float flickerSpeed=6;
        [Tooltip("Height/intensity swing as a fraction (0.25 = plus or minus 25 percent).")]
        [Range(0,.8f)] public float flickerAmount=.25f;

        [Header("Light (optional)")]
        [Tooltip("Point light to flicker. Leave empty for a flame without light; light only a few flames per candelabra.")]
        public Light flameLight;
        [Tooltip("Light intensity at full strength.")]
        public float lightIntensity=1.2f;
        [Tooltip("Distance from the viewer where the light starts to fade.")]
        public float lightFadeStart=10;
        [Tooltip("Distance from the viewer where the light is switched off.")]
        public float lightFadeEnd=16;

        float seed;
        bool ready;
        void Start()
        {
            var p=transform.position;
            seed=Mathf.Abs(Mathf.Sin(p.x*12.9898f+p.y*37.719f+p.z*78.233f))*100f;
            ready=true;
        }
        void Update()
        {
            if(!ready)return;
            float t=Time.time*flickerSpeed+seed;
            float flick=Mathf.PerlinNoise(t,.37f)*.7f+Mathf.PerlinNoise(t*1.9f,4.1f)*.3f; // roughly 0.2..0.8
            float level=1+(flick-.5f)*2*flickerAmount;
            bool haveViewer=SurrealProps.TryGetViewer(out var viewer);
            if(billboard)
            {
                float height=size*level;
                billboard.localScale=new Vector3(size*widthRatio*(2-level),height,1);
                var to=billboard.position-viewer;
                if(haveViewer&&to.sqrMagnitude<billboardRange*billboardRange&&to.sqrMagnitude>1e-4f)
                    billboard.rotation=Quaternion.LookRotation(to,transform.up);
            }
            if(glow)
            {
                glow.localScale=new Vector3(glowSize*(.9f+.2f*level),glowSize*(.9f+.2f*level),1);
                var to=glow.position-viewer;
                if(haveViewer&&to.sqrMagnitude<billboardRange*billboardRange&&to.sqrMagnitude>1e-4f)glow.rotation=Quaternion.LookRotation(to,Vector3.up);
            }
            if(flameLight)
            {
                float fade=1;
                if(haveViewer)
                {
                    float d=Vector3.Distance(viewer,flameLight.transform.position);
                    fade=d>=lightFadeEnd?0:d<=lightFadeStart?1:1-(d-lightFadeStart)/Mathf.Max(.01f,lightFadeEnd-lightFadeStart);
                }
                bool on=fade>0;
                if(flameLight.enabled!=on)flameLight.enabled=on;
                if(on)flameLight.intensity=lightIntensity*level*fade;
            }
        }
    }
}
