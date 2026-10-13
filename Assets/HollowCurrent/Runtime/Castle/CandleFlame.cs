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
        static bool reported;
        Floater floater;
        // Degrees the billboard is rotated about the view axis away from the candle's on-screen axis (0 when not leaning).
        public float LeanDegrees{get;private set;}
        public static readonly System.Collections.Generic.List<CandleFlame> All=new System.Collections.Generic.List<CandleFlame>();
        static float nextReport;static int reportsLeft=3;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics(){All.Clear();reported=false;nextReport=0;reportsLeft=3;}
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable(){All.Remove(this);}

        // Self-test: logs, for the three flames nearest the viewer that are leaning, the on-screen tilt of the flame quad against the
        // candle's own axis, measured from the screen positions of the quad's base and tip (HC_LEAN_CHECK). Called automatically
        // (up to three times, 2 s apart, once a flame leans) and by capture code after a shot: CandleFlame.LogLeanReport("S5").
        public static void LogLeanReport(string tag)
        {
            var cam=Camera.main;if(!cam&&SurrealProps.Viewer)cam=SurrealProps.Viewer.GetComponent<Camera>();
            if(!cam){Debug.Log("HC_LEAN_CHECK "+tag+": no camera");return;}
            var pick=new CandleFlame[3];var dist=new float[3];int n=0;
            foreach(var f in All)
            {
                if(!f||!f.billboard||f.LeanDegrees<=0)continue;
                float d=Vector3.Distance(cam.transform.position,f.billboard.position);
                int slot=n<3?n++:-1;
                if(slot<0){int worst=0;for(int i=1;i<3;i++)if(dist[i]>dist[worst])worst=i;if(d<dist[worst])slot=worst;else continue;}
                pick[slot]=f;dist[slot]=d;
            }
            if(n==0){Debug.Log("HC_LEAN_CHECK "+tag+": no flame is leaning ("+All.Count+" flames, none with a target in range)");return;}
            for(int i=0;i<n;i++)
            {
                var b=pick[i].billboard;float h=Mathf.Max(.01f,b.lossyScale.y);
                Vector2 p0=cam.WorldToScreenPoint(b.position),quad=(Vector2)cam.WorldToScreenPoint(b.position+b.up*h)-p0,candle=(Vector2)cam.WorldToScreenPoint(b.position+Vector3.up*h)-p0;
                Debug.Log("HC_LEAN_CHECK "+tag+" candle "+(i+1)+" at "+dist[i].ToString("F1")+" m: flame quad is tilted "+Mathf.Abs(Vector2.SignedAngle(candle,quad)).ToString("F0")+" deg on screen from its candle axis (quad "+Vector2.SignedAngle(Vector2.up,quad).ToString("F0")+" deg, candle "+Vector2.SignedAngle(Vector2.up,candle).ToString("F0")+" deg from screen vertical; applied lean "+pick[i].LeanDegrees.ToString("F0")+" deg)");
            }
        }
        static void MaybeReport()
        {
            if(reportsLeft<=0||Time.unscaledTime<nextReport)return;
            foreach(var f in All)if(f&&f.LeanDegrees>=20)
            {
                nextReport=Time.unscaledTime+2f;reportsLeft--;LogLeanReport("auto");return;
            }
        }
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
                    // The lean is a ROTATION OF THE BILLBOARD ABOUT THE VIEW AXIS: from the candle's own axis as it appears on screen
                    // (world up flattened against the sight line) toward the side of the screen the Count is on, by the floater's bend
                    // (55 deg x smoothstep: about 33 at 6 m, 50 at 3 m). It is visible from any camera, however the Count is placed
                    // relative to the sight line (a 3D lean along the view axis, or toward a Count straight above, is not).
                    var up=transform.up;
                    LeanDegrees=0;
                    if(floater&&floater.flameTarget&&floater.Bend01>.01f)
                    {
                        var view=to.normalized;
                        var axis=Vector3.up-view*Vector3.Dot(Vector3.up,view);
                        axis=axis.sqrMagnitude<.01f?SurrealProps.ViewerUp:axis.normalized;
                        var right=Vector3.Cross(axis,view).normalized;   // Unity: right = up x forward
                        float degrees=floater.maxBendDegrees*floater.Bend01;
                        var toTarget=floater.flameTarget.position+Vector3.up*1.2f-billboard.position;
                        float side=Vector3.Dot(toTarget,right);
                        float sign=Mathf.Abs(side)>.05f?Mathf.Sign(side):(seed>50?1f:-1f);
                        float radians=degrees*Mathf.Deg2Rad;
                        up=axis*Mathf.Cos(radians)+right*(sign*Mathf.Sin(radians));
                        LeanDegrees=degrees;
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
            if(this==(All.Count>0?All[0]:null))MaybeReport();
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
