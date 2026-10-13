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
        static bool reported,leanReported;
        Floater floater;
        static string Describe(Material m)
        {
            if(!m)return "<no material>";
            var t=m.mainTexture;
            return "shader="+(m.shader?m.shader.name:"<none>")+" tex="+(t?t.name+" "+t.width+"x"+t.height+" "+(t is Texture2D t2?t2.format.ToString()+" readable="+t2.isReadable:t.GetType().Name):"<null>");
        }
        // Flames built by CastleWorld are saved into the prepared scene; the player must not depend on what the scene serialized
        // (procedural textures arrived blank white there, which drew every flame as an opaque card). Re-assign the shared runtime
        // materials, built from the baked PNG sprites, and log once what the scene had and what is used now.
        void UseRuntimeMaterials()
        {
            var flameRenderer=billboard?billboard.GetComponent<Renderer>():null;
            string before=flameRenderer?Describe(flameRenderer.sharedMaterial):"<no billboard>";
            var flameMaterial=SurrealProps.SharedFlameMaterial;
            if(flameRenderer&&flameMaterial)flameRenderer.sharedMaterial=flameMaterial;
            if(glow)
            {
                var glowRenderer=glow.GetComponent<Renderer>();
                var glowMaterial=SurrealProps.SharedGlowMaterial;
                if(glowRenderer&&glowMaterial)glowRenderer.sharedMaterial=glowMaterial;
            }
            if(!reported)
            {
                reported=true;
                Debug.Log("HC_FLAME_CHECK scene material: "+before+" | runtime material: "+Describe(flameMaterial)+(flameRenderer?" | queue="+flameRenderer.sharedMaterial.renderQueue:""));
            }
        }
        void Start()
        {
            UseRuntimeMaterials();
            floater=GetComponentInParent<Floater>();
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
                {
                    // Lean toward the Count ON SCREEN. A 3D lean toward him is invisible when the camera looks along it (the S5
                    // camera stands next to the candles and looks at him), so tilt the billboard's up toward his screen position:
                    // the tip always points at him in the image. Degrees follow the floater's bend (about 33 at 6 m).
                    var up=transform.up;
                    if(floater&&floater.flameTarget&&floater.Bend01>.01f)
                    {
                        var view=to.normalized;
                        var toTarget=floater.flameTarget.position+Vector3.up*1.2f-billboard.position;
                        // Lean horizontally toward him, as seen on screen. The Count usually perches above the candles, so
                        // "toward him on screen" would be straight up (no visible lean): use the horizontal part of the offset,
                        // and when that runs along the sight line (foreshortened), lean toward the side of the screen he is on.
                        var horizontal=new Vector3(toTarget.x,0,toTarget.z);
                        var across=horizontal-view*Vector3.Dot(horizontal,view);
                        Vector3 lean;
                        if(horizontal.sqrMagnitude>.04f&&across.sqrMagnitude>.25f*horizontal.sqrMagnitude)lean=across.normalized;
                        else{var right=SurrealProps.ViewerRight;lean=Vector3.Dot(toTarget,right)<0?-right:right;}
                        float degrees=floater.maxBendDegrees*floater.Bend01;
                        up=Vector3.RotateTowards(Vector3.up,lean,degrees*Mathf.Deg2Rad,0);
                        if(!leanReported&&degrees>=20)
                        {
                            leanReported=true;
                            Debug.Log("HC_LEAN_CHECK flame leans "+degrees.ToString("F0")+" deg off its candle toward the target at "+toTarget.magnitude.ToString("F1")+" m (floater bend "+floater.Bend01.ToString("F2")+", screen lean "+Vector3.Angle(Vector3.up,up).ToString("F0")+" deg)");
                        }
                    }
                    billboard.rotation=Quaternion.LookRotation(to,up);
                }
                // the halo rides on the flame's body, wherever the flame leans
                if(glow)glow.position=billboard.position+billboard.up*(billboard.lossyScale.y*.45f);
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
