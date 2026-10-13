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

        // ---- the flame is built in the flame shader (HollowCurrent/CastleFlame) ----
        // One quad per flame (core and halo in one sprite). The vertex stage builds it in the view plane of the rendering camera around the
        // wick (the object's origin), with the lean as a rigid rotation about the view axis: no CPU facing, no separate halo object.
        // FlameVertexWorld mirrors the shader line for line, for the self-tests. (vx, vy) = mesh vertex (x -0.9..0.9, y -0.4..1.4, y = 0 the wick).
        public const float MaxLean=45f;
        public static Vector3 FlameVertexWorld(Vector3 pivot,Vector3 cameraPosition,float sx,float sy,float vx,float vy,Vector3 leanDirection,float sideHint,float degrees)
        {
            var view=(pivot-cameraPosition).normalized;
            var axis=Vector3.up-view*view.y;
            axis=axis.sqrMagnitude>1e-4f?axis.normalized:SurrealProps.ViewerUp;
            var right=Vector3.Cross(axis,view).normalized;
            if(degrees>.01f)
            {
                float side=Vector3.Dot(leanDirection,right);
                float sign=Mathf.Abs(side)>.05f?Mathf.Sign(side):(sideHint>=0?1f:-1f);
                float a=Mathf.Min(degrees,MaxLean)*Mathf.Deg2Rad;
                var axisLean=axis*Mathf.Cos(a)+right*(sign*Mathf.Sin(a));
                var rightLean=right*Mathf.Cos(a)-axis*(sign*Mathf.Sin(a));
                axis=axisLean;right=rightLean;
            }
            return pivot+right*(vx*sx)+axis*(vy*sy);
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
                var f=pick[i];var b=f.billboard;
                var pivot=b.position;float sx=b.lossyScale.x,sy=Mathf.Max(.01f,b.lossyScale.y);var dir=f.lastLeanDir;float hint=f.seed>50?1f:-1f;var cp=cam.transform.position;
                // wick = vertex (0,0): with the lean applied against with none (the pivot of the rotation, so they must be identical)
                var wickLean=FlameVertexWorld(pivot,cp,sx,sy,0,0,dir,hint,f.leanNow);var wickPlain=FlameVertexWorld(pivot,cp,sx,sy,0,0,dir,hint,0);
                Vector2 p0=cam.WorldToScreenPoint(wickPlain),p0After=cam.WorldToScreenPoint(wickLean);
                Vector2 tip=(Vector2)cam.WorldToScreenPoint(FlameVertexWorld(pivot,cp,sx,sy,0,1,dir,hint,f.leanNow))-p0;
                Vector2 candle=(Vector2)cam.WorldToScreenPoint(FlameVertexWorld(pivot,cp,sx,sy,0,1,dir,hint,0))-p0;
                float amount=-1;Vector4 sent=Vector4.zero;string shader="<no renderer>";
                if(f.flameRenderer){f.flameRenderer.GetPropertyBlock(block);amount=block.GetFloat(LeanAmountId);sent=block.GetVector(LeanDirId);var m=f.flameRenderer.sharedMaterial;shader=m&&m.shader?m.shader.name+(m.shader.isSupported?"":" (NOT SUPPORTED)"):"<none>";}
                Debug.Log("HC_LEAN_CHECK "+tag+" candle "+(i+1)+" at "+dist[i].ToString("F1")+" m: wick screen position before lean ("+p0.x.ToString("F1")+","+p0.y.ToString("F1")+") after ("+p0After.x.ToString("F1")+","+p0After.y.ToString("F1")+") moved "+Vector2.Distance(p0,p0After).ToString("F2")+" px; rendered flame tip is tilted "+Mathf.Abs(Vector2.SignedAngle(candle,tip)).ToString("F0")+" deg on screen from its candle axis, tip distance from wick "+tip.magnitude.ToString("F0")+" px (unleaned "+candle.magnitude.ToString("F0")+" px); shader "+shader+", property block _LeanAmount="+amount.ToString("F1")+" _LeanDir=("+sent.x.ToString("F2")+","+sent.y.ToString("F2")+","+sent.z.ToString("F2")+")");
            }
            if(tag.StartsWith("S5",System.StringComparison.Ordinal))LogLedgeCandles(cam);
            LogRigidReport(tag,5);
        }

        // HC_FLAME_RIGID: for the `count` flames nearest the camera (leaning or not): is the core enabled, how far is the halo centre from the
        // core centre (one quad: the sprite's halo is centred 0.45 flame heights up, the teardrop 0.5, so the gap is 0.05 x height), and how
        // far has the flame object drifted from where it was seated on its wick at Start (local position), including the billboard against it.
        // The brief's limit is 0.05 m for the halo-core gap.
        public static void LogRigidReport(string tag,int count)
        {
            var cam=Camera.main;if(!cam&&SurrealProps.Viewer)cam=SurrealProps.Viewer.GetComponent<Camera>();
            if(!cam)return;
            var ordered=new System.Collections.Generic.List<CandleFlame>();
            foreach(var f in All)if(f&&f.billboard)ordered.Add(f);
            var cp=cam.transform.position;
            ordered.Sort((p,q)=>Vector3.Distance(cp,p.billboard.position).CompareTo(Vector3.Distance(cp,q.billboard.position)));
            bool all=true;int n=Mathf.Min(count,ordered.Count);
            for(int i=0;i<n;i++)
            {
                var f=ordered[i];var b=f.billboard;float sx=b.lossyScale.x,sy=Mathf.Max(.01f,b.lossyScale.y);var dir=f.lastLeanDir;float hint=f.seed>50?1f:-1f;
                var core=FlameVertexWorld(b.position,cp,sx,sy,0,.5f,dir,hint,f.leanNow);var halo=FlameVertexWorld(b.position,cp,sx,sy,0,.45f,dir,hint,f.leanNow);
                float gap=Vector3.Distance(core,halo),drift=Vector3.Distance(f.transform.localPosition,f.seatedLocal),billboardOffset=Vector3.Distance(b.position,f.transform.position);
                bool enabled=f.flameRenderer&&f.flameRenderer.enabled&&f.gameObject.activeInHierarchy&&f.flameRenderer.sharedMaterial&&f.flameRenderer.sharedMaterial.shader&&f.flameRenderer.sharedMaterial.shader.isSupported;
                bool ok=enabled&&gap<=.05f&&drift<.01f&&billboardOffset<.01f;all&=ok;
                Debug.Log("HC_FLAME_RIGID "+tag+" flame "+(i+1)+" at "+Vector3.Distance(cp,b.position).ToString("F1")+" m: core enabled="+enabled+", halo centre to core centre "+gap.ToString("F3")+" m (limit 0.05), wick drift "+drift.ToString("F3")+" m, quad offset from its wick "+billboardOffset.ToString("F3")+" m, flame height "+sy.ToString("F2")+" m, lean "+f.leanNow.ToString("F0")+" deg "+(ok?"PASS":"FAIL"));
            }
            Debug.Log("HC_FLAME_RIGID "+tag+" summary: "+n+" nearest flames "+(all?"PASS":"FAIL")+" ("+All.Count+" flames in the scene)");
        }
        // HC_S5_CANDLES: how many ledge candelabras (Floater.neverHide) exist, are still enabled after the capture's hide-for-shot
        // pass, and have a flame inside the camera frame.
        static void LogLedgeCandles(Camera cam)
        {
            int total=0,visible=0,inFrame=0;
            foreach(var fl in Floater.All)
            {
                if(!fl||!fl.neverHide)continue;
                total++;
                var rend=fl.GetComponentInChildren<Renderer>();
                if(rend&&rend.enabled)visible++;
                foreach(var fc in fl.GetComponentsInChildren<CandleFlame>())
                {
                    var vp=cam.WorldToViewportPoint(fc.transform.position);
                    if(vp.z>0&&vp.x>0&&vp.x<1&&vp.y>0&&vp.y<1){inFrame++;break;}
                }
            }
            Debug.Log("HC_S5_CANDLES "+total+" (enabled "+visible+", with a flame in frame "+inFrame+")");
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
            // A prepared scene baked before round 9 holds the old base-pivot quad plus a separate halo object: use the one-quad flame mesh
            // and switch the halo object off, so the saved scene cannot bring the detached halo back.
            if(billboard)
            {
                var filter=billboard.GetComponent<MeshFilter>();
                if(filter&&filter.sharedMesh!=SurrealProps.SharedFlameQuad)filter.sharedMesh=SurrealProps.SharedFlameQuad;
            }
            if(glow){glow.gameObject.SetActive(false);glow=null;}
            if(!reported)
            {
                reported=true;
                Debug.Log("HC_FLAME_CHECK scene material: "+before+" | runtime material: "+Describe(flameMaterial)+(flameRenderer?" | queue="+flameRenderer.sharedMaterial.renderQueue:"")+" | lean shader property _LeanAmount "+(flameMaterial&&flameMaterial.HasProperty(LeanAmountId)?"present":"MISSING (old shader?)")+" | one-quad flame mesh "+(billboard&&billboard.GetComponent<MeshFilter>()&&billboard.GetComponent<MeshFilter>().sharedMesh==SurrealProps.SharedFlameQuad?"in use":"NOT in use"));
            }
        }
        Vector3 seatedLocal;
        void Start()
        {
            UseRuntimeMaterials();
            seatedLocal=transform.localPosition;
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
                billboard.localScale=new Vector3(size*(2-level),height,1);   // the quad spans 1.8 flame heights across: x scale = height (about)
                var to=billboard.position-viewer;
                if(haveViewer&&to.sqrMagnitude<billboardRange*billboardRange&&to.sqrMagnitude>1e-4f)
                    billboard.rotation=Quaternion.LookRotation(to,Vector3.up);   // faces the viewer; the lean is done by the shader
                // lean target: the Count (degrees follow the floater's bend), easing back upright (about 1 s) when there is none
                float want=0;
                if(floater&&floater.flameTarget&&floater.Bend01>.01f)
                {
                    want=Mathf.Min(MaxLean,floater.maxBendDegrees*floater.Bend01);
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
