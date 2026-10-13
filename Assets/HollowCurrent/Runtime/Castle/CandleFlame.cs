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
        Renderer flameRenderer;
        float leanNow;
        // Degrees of lean currently handed to the flame shader (0 when upright).
        public float LeanDegrees=>leanNow;
        public static readonly System.Collections.Generic.List<CandleFlame> All=new System.Collections.Generic.List<CandleFlame>();
        static float nextReport;static int reportsLeft=3;
        static MaterialPropertyBlock block;
        static readonly int LeanDirId=Shader.PropertyToID("_LeanDir"),LeanAmountId=Shader.PropertyToID("_LeanAmount");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics(){All.Clear();reported=false;nextReport=0;reportsLeft=3;block=null;}
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable(){All.Remove(this);}

        // ---- the lean lives in the flame shader (HollowCurrent/CastleFlame) ----
        // The billboard only faces the viewer. Its material reads _LeanDir (world direction to the Count, w = side hint) and
        // _LeanAmount (degrees) from a per-renderer MaterialPropertyBlock, and the vertex stage shifts the quad's tip sideways along
        // the camera-right of the view plane by tan(amount) * height * (y along the quad). Nothing done to the transform afterwards
        // (facing code, batching, the saved scene) can cancel it. LeanTipOffset mirrors the shader exactly, for the self-test.
        public static Vector3 LeanTipOffset(Vector3 basePosition,Vector3 viewerPosition,Vector3 leanDirection,float sideHint,float degrees,float height)
        {
            var view=(basePosition-viewerPosition).normalized;
            var right=Vector3.Cross(Vector3.up,view);
            if(right.sqrMagnitude<1e-6f)right=SurrealProps.ViewerRight;
            right.Normalize();
            float side=Vector3.Dot(leanDirection,right);
            float sign=Mathf.Abs(side)>.05f?Mathf.Sign(side):(sideHint>=0?1f:-1f);
            return right*(sign*Mathf.Tan(Mathf.Min(degrees,80f)*Mathf.Deg2Rad)*height);
        }

        // Self-test (HC_LEAN_CHECK). For the three leaning flames nearest the camera it projects the flame's base and its tip, with
        // the shader's offset applied (the position the vertex stage actually puts the tip at), to screen space and reports the
        // angle between that tip direction and the candle's own axis, plus the property-block values read back from the renderer and
        // the shader name. Call after a shot: CandleFlame.LogLeanReport("S5"). Also runs by itself (3 times, 2 s apart) once a flame leans.
        public static void LogLeanReport(string tag)
        {
            var cam=Camera.main;if(!cam&&SurrealProps.Viewer)cam=SurrealProps.Viewer.GetComponent<Camera>();
            if(!cam){Debug.Log("HC_LEAN_CHECK "+tag+": no camera");return;}
            var pick=new CandleFlame[3];var dist=new float[3];int n=0;
            foreach(var f in All)
            {
                if(!f||!f.billboard||f.leanNow<=0)continue;
                float d=Vector3.Distance(cam.transform.position,f.billboard.position);
                int slot=n<3?n++:-1;
                if(slot<0){int worst=0;for(int i=1;i<3;i++)if(dist[i]>dist[worst])worst=i;if(d<dist[worst])slot=worst;else continue;}
                pick[slot]=f;dist[slot]=d;
            }
            if(n==0){Debug.Log("HC_LEAN_CHECK "+tag+": no flame is leaning ("+All.Count+" flames, none has a target in range)");return;}
            if(block==null)block=new MaterialPropertyBlock();
            for(int i=0;i<n;i++)
            {
                var f=pick[i];var b=f.billboard;float h=Mathf.Max(.01f,b.lossyScale.y);
                var baseWorld=b.position;var dir=f.lastLeanDir;
                var tipWorld=baseWorld+b.up*h+LeanTipOffset(baseWorld,cam.transform.position,dir,f.seed>50?1f:-1f,f.leanNow,h);
                Vector2 p0=cam.WorldToScreenPoint(baseWorld),tip=(Vector2)cam.WorldToScreenPoint(tipWorld)-p0,candle=(Vector2)cam.WorldToScreenPoint(baseWorld+Vector3.up*h)-p0;
                float amount=-1;Vector4 sent=Vector4.zero;string shader="<no renderer>";
                if(f.flameRenderer){f.flameRenderer.GetPropertyBlock(block);amount=block.GetFloat(LeanAmountId);sent=block.GetVector(LeanDirId);var m=f.flameRenderer.sharedMaterial;shader=m&&m.shader?m.shader.name+(m.shader.isSupported?"":" (NOT SUPPORTED)"):"<none>";}
                Debug.Log("HC_LEAN_CHECK "+tag+" candle "+(i+1)+" at "+dist[i].ToString("F1")+" m: rendered flame tip is tilted "+Mathf.Abs(Vector2.SignedAngle(candle,tip)).ToString("F0")+" deg on screen from its candle axis (tip "+Vector2.SignedAngle(Vector2.up,tip).ToString("F0")+" deg, candle "+Vector2.SignedAngle(Vector2.up,candle).ToString("F0")+" deg from screen vertical); shader "+shader+", property block _LeanAmount="+amount.ToString("F1")+" _LeanDir=("+sent.x.ToString("F2")+","+sent.y.ToString("F2")+","+sent.z.ToString("F2")+")");
            }
        }
        static void MaybeReport()
        {
            if(reportsLeft<=0||Time.unscaledTime<nextReport)return;
            foreach(var f in All)if(f&&f.leanNow>=20)
            {
                nextReport=Time.unscaledTime+2f;reportsLeft--;LogLeanReport("auto");return;
            }
        }
        Vector3 lastLeanDir=Vector3.up;

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
            flameRenderer=billboard?billboard.GetComponent<Renderer>():null;
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
                Debug.Log("HC_FLAME_CHECK scene material: "+before+" | runtime material: "+Describe(flameMaterial)+(flameRenderer?" | queue="+flameRenderer.sharedMaterial.renderQueue:"")+" | lean shader property _LeanAmount "+(flameMaterial&&flameMaterial.HasProperty(LeanAmountId)?"present":"MISSING (old shader?)"));
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
                    billboard.rotation=Quaternion.LookRotation(to,Vector3.up);   // faces the viewer; the lean is done by the shader
                // lean target: the Count (degrees follow the floater's bend), easing back upright (about 1 s) when there is none
                float want=0;
                if(floater&&floater.flameTarget&&floater.Bend01>.01f)
                {
                    want=floater.maxBendDegrees*floater.Bend01;
                    var toTarget=floater.flameTarget.position+Vector3.up*1.2f-billboard.position;
                    if(toTarget.sqrMagnitude>1e-4f)lastLeanDir=toTarget.normalized;
                }
                leanNow=Mathf.MoveTowards(leanNow,want,60f*Time.deltaTime);
                if(flameRenderer&&(leanNow>0||leaned))
                {
                    if(block==null)block=new MaterialPropertyBlock();
                    block.SetVector(LeanDirId,new Vector4(lastLeanDir.x,lastLeanDir.y,lastLeanDir.z,seed>50?1f:-1f));
                    block.SetFloat(LeanAmountId,leanNow);
                    flameRenderer.SetPropertyBlock(block);
                    leaned=leanNow>0;
                }
                // the halo rides on the flame's body, wherever the tip has been leaned to (half way up, half the tip shift)
                if(glow)
                {
                    float h=billboard.lossyScale.y;
                    var pos=billboard.position+billboard.up*(h*.45f);
                    if(leanNow>0&&haveViewer)pos+=LeanTipOffset(billboard.position,viewer,lastLeanDir,seed>50?1f:-1f,leanNow,h)*.45f;
                    glow.position=pos;
                }
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
        bool leaned;
    }
}
