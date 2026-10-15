using System;
using System.Collections.Generic;
using UnityEngine;

namespace HollowCurrent
{
    public enum FloaterKind { Candelabra, ClosedBook, OpenBook }

    // Code-only factories for the castle's surreal props, so the layout can place
    // them without prefabs. Meshes, textures and materials are built once and
    // shared, so every floater (and every rune, knife and bat) reuses the same
    // assets and batches or instances. No Game/Investigation/Presence references.
    public static partial class SurrealProps
    {
        // ---- viewer (the camera the billboards and light fades follow) ----
        // Piece 0 may assign the player camera; otherwise Camera.main is used.
        public static Transform Viewer;
        // Optional shader for glowing/billboard materials, e.g. an additive one
        // from Resources. Defaults to Sprites/Default (alpha blended, unlit).
        public static Shader GlowShader;
        static int viewerFrame=-1;
        static Vector3 viewerPosition,viewerRight=Vector3.right,viewerUp=Vector3.up;
        static bool viewerFound;
        // Screen-right of the viewer (valid after TryGetViewer this frame).
        public static Vector3 ViewerRight=>viewerRight;
        public static Vector3 ViewerUp=>viewerUp;
        public static bool TryGetViewer(out Vector3 position)
        {
            if(viewerFrame!=Time.frameCount)
            {
                viewerFrame=Time.frameCount;
                var t=Viewer;
                if(!t){var cam=Camera.main;t=cam?cam.transform:null;}
                viewerFound=t;viewerPosition=t?t.position:Vector3.zero;viewerRight=t?t.right:Vector3.right;viewerUp=t?t.up:Vector3.up;
            }
            position=viewerPosition;return viewerFound;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Viewer=null;GlowShader=null;spriteShader=flameShader=null;viewerFrame=-1;viewerFound=false;
            ResetAssets();
        }
        static partial void ResetAssets();

        // ---- materials ----
        // The sprite shaders live in Resources/Castle/Shaders so they are always in a player build. Sprites/Default is not in
        // Always Included Shaders: Shader.Find returned null there and the Standard fallback drew every sprite as an opaque
        // white card (the round-2 flames, moons and the chapel window).
        static Shader spriteShader,flameShader;
        static Shader FindCastleShader(string name)
        {
            var s=Resources.Load<Shader>("Castle/Shaders/"+name);
            if(!s)s=Shader.Find("HollowCurrent/"+name);
            return s;
        }
        static Shader SpriteShader=>spriteShader?spriteShader:spriteShader=FindCastleShader("CastleSprite");
        static Shader FlameShader=>flameShader?flameShader:flameShader=FindCastleShader("CastleFlame");
        public static Material Unlit(string name,Texture texture,Color tint)
        {
            var shader=GlowShader?GlowShader:SpriteShader;
            if(!shader)shader=Shader.Find("Sprites/Default");
            if(!shader)shader=Shader.Find("Standard");
            var material=new Material(shader){name=name,mainTexture=texture,color=tint};
            material.enableInstancing=true;
            return material;
        }
        // Additive (premultiplied) variant for flames and glows.
        public static Material UnlitAdditive(string name,Texture texture,Color tint)
        {
            var shader=FlameShader;
            if(!shader)return Unlit(name,texture,tint);
            var material=new Material(shader){name=name,mainTexture=texture,color=tint};
            material.enableInstancing=true;
            return material;
        }
        public static Material Lit(string name,Color color,float metallic,float smoothness)
        {
            var material=new Material(Shader.Find("Standard")){name=name,color=color};
            material.SetFloat("_Metallic",metallic);material.SetFloat("_Glossiness",smoothness);
            material.enableInstancing=true;
            return material;
        }

        // ---- shared quads (pivot at base or at centre; visible side faces -Z like Unity's Quad) ----
        static Mesh baseQuad,centreQuad;
        public static Mesh BaseQuad=>baseQuad?baseQuad:baseQuad=Quad("Surreal base quad",0);
        public static Mesh CentreQuad=>centreQuad?centreQuad:centreQuad=Quad("Surreal centre quad",.5f);
        static Mesh Quad(string name,float pivotY)
        {
            var mesh=new Mesh{name=name};
            mesh.vertices=new[]{new Vector3(-.5f,-pivotY,0),new Vector3(.5f,-pivotY,0),new Vector3(-.5f,1-pivotY,0),new Vector3(.5f,1-pivotY,0)};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)};
            mesh.normals=new[]{Vector3.back,Vector3.back,Vector3.back,Vector3.back};
            mesh.triangles=new[]{0,2,1,2,3,1};
            mesh.RecalculateBounds();
            return mesh;
        }

        // ---- helpers ----
        internal static Transform NewChild(string name,Transform parent,Vector3 localPosition,Quaternion localRotation)
        {
            var go=new GameObject(name);
            if(parent)go.transform.SetParent(parent,false);
            go.transform.localPosition=localPosition;go.transform.localRotation=localRotation;
            return go.transform;
        }
        internal static MeshRenderer NewRenderer(string name,Transform parent,Vector3 localPosition,Quaternion localRotation,Mesh mesh,Material[] materials,bool castShadows)
        {
            var t=NewChild(name,parent,localPosition,localRotation);
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=t.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials=materials;
            renderer.shadowCastingMode=castShadows?UnityEngine.Rendering.ShadowCastingMode.On:UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows=castShadows;
            return renderer;
        }
        internal static Texture2D MakeTexture(string name,int width,int height,Func<float,float,Color32> shade)
        {
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,true){name=name,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            var pixels=new Color32[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)pixels[y*width+x]=shade((x+.5f)/width,(y+.5f)/height);
            // Stays readable: a non-readable procedural texture could not be serialized into the prepared scene (CastleWorld's
            // output is saved with it) and arrived in the player as a blank white texture.
            texture.SetPixels32(pixels);texture.Apply(true,false);
            return texture;
        }
        // Sprite textures baked by tools/castle_surreal_textures.py (saved by reference, so they survive scene serialization).
        // The procedural versions remain as fallbacks only.
        internal static Texture2D LoadSprite(string file)
        {
            try{return Resources.Load<Texture2D>("Castle/Textures/"+file);}
            catch(Exception){return null;}
        }
        static float Smooth(float edge0,float edge1,float x){float k=Mathf.Clamp01((x-edge0)/(edge1-edge0));return k*k*(3-2*k);}

        // ---- candle flame ----
        static Texture2D flameTexture;
        static Material flameMaterial;
        static Texture2D FlameTexture=>flameTexture?flameTexture:flameTexture=LoadSprite("Surreal_Flame")??MakeTexture("Surreal flame",32,64,(u,v)=>
        {
            float width=.5f*Mathf.Pow(Mathf.Sin(Mathf.PI*Mathf.Pow(v,.6f)),.8f)*(1-.25f*v);
            float d=Mathf.Abs(u-.5f);
            float edge=width<.002f?0:Mathf.Clamp01((width-d)/.09f);
            float core=Mathf.Clamp01(1-d/Mathf.Max(width,.001f))*(1-v*.6f);
            var c=Color.Lerp(new Color(1,.42f,.08f),new Color(1,.95f,.7f),Mathf.Pow(core,1.5f));
            c.a=edge*(.55f+.45f*core);
            return c;
        });
        static Material FlameMaterial=>flameMaterial?flameMaterial:flameMaterial=UnlitAdditive("Surreal flame",FlameTexture,Color.white);
        static Texture2D glowTexture;
        static Material glowMaterial;
        static Texture2D GlowTexture=>glowTexture?glowTexture:glowTexture=LoadSprite("Surreal_FlameGlow")??MakeTexture("Surreal flame glow",32,32,(u,v)=>
        {
            float r=Mathf.Sqrt((u-.5f)*(u-.5f)+(v-.5f)*(v-.5f))*2;
            float a=Mathf.Pow(Mathf.Clamp01(1-r),2.2f)*.55f;
            return new Color(1,.55f,.2f,a);
        });
        static Material GlowMaterial=>glowMaterial?glowMaterial:glowMaterial=UnlitAdditive("Surreal flame glow",GlowTexture,Color.white);
        // Runtime-built shared materials. Props saved into the prepared scene re-assign these in Start (see CandleFlame), so
        // what is drawn never depends on what the scene serialized.
        // Round 9: one quad per flame holds core AND halo (Surreal_FlameHalo: teardrop plus a soft halo centred 0.45 flame heights up) and is
        // built in the CastleFlame vertex stage around the wick, so the two can never separate. The mesh spans x -0.9..0.9, y -0.4..1.4
        // flame heights (y = 0 is the wick); the object's x/y scale is the flame height.
        static Mesh flameQuad;
        static Texture2D flameHaloTexture;
        static Material flameHaloMaterial;
        public static Mesh FlameQuad
        {
            get
            {
                if(flameQuad)return flameQuad;
                var mesh=new Mesh{name="Surreal flame quad"};
                mesh.vertices=new[]{new Vector3(-.9f,-.4f,0),new Vector3(.9f,-.4f,0),new Vector3(-.9f,1.4f,0),new Vector3(.9f,1.4f,0)};
                mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)};
                mesh.normals=new[]{Vector3.back,Vector3.back,Vector3.back,Vector3.back};
                mesh.triangles=new[]{0,2,1,2,3,1};
                mesh.bounds=new Bounds(new Vector3(0,.5f,0),new Vector3(2.2f,2.2f,2.2f));   // the vertex stage turns the quad: keep culling generous
                return flameQuad=mesh;
            }
        }
        static Texture2D FlameHaloTexture=>flameHaloTexture?flameHaloTexture:flameHaloTexture=LoadSprite("Surreal_FlameHalo")??FlameTexture;
        static Material FlameHaloMaterial=>flameHaloMaterial?flameHaloMaterial:flameHaloMaterial=UnlitAdditive("Surreal flame",FlameHaloTexture,Color.white);
        internal static Material SharedFlameMaterial=>FlameHaloMaterial;
        internal static Mesh SharedFlameQuad=>FlameQuad;

        // Round 13: the standard flame height of a candle: 6 cm (a little over real, so it still reads at 6 m; 0.11 m quad with its halo).
        public const float FlameHeight=.06f;

        // glowRadius > 0 adds a soft additive halo (a billboard, not a light) around the flame.
        public static CandleFlame MakeCandleFlame(Transform parent,Vector3 localPosition,float height=.11f,bool withLight=false,string name="Candle flame",float glowRadius=0)
        {
            var t=NewChild(name,parent,localPosition,Quaternion.identity);
            var flame=t.gameObject.AddComponent<CandleFlame>();
            flame.size=height;
            // glowRadius is kept for callers but no longer creates a second quad: the halo is part of the flame sprite
            var renderer=NewRenderer("Billboard",t,Vector3.zero,Quaternion.identity,FlameQuad,new[]{FlameHaloMaterial},false);
            renderer.transform.localScale=new Vector3(height,height,1);
            flame.billboard=renderer.transform;
            if(withLight)
            {
                var lightObject=NewChild("Flame light",t,Vector3.up*height*.7f,Quaternion.identity).gameObject;
                var light=lightObject.AddComponent<Light>();
                light.type=LightType.Point;light.color=new Color(1,.62f,.3f);light.range=4.5f;light.intensity=flame.lightIntensity;
                light.shadows=LightShadows.None;
                flame.flameLight=light;
            }
            return flame;
        }

        // ---- floaters ----
        // MakeFloater builds a floating candelabra or book under `parent` at
        // `localPosition` (world position if parent is null). `seed` varies the
        // phase and, for books, nothing else, so two calls with different seeds
        // never bob together. Lit candles: lightedFlames of the five candles get
        // a real point light (default 1); the rest are billboards only.
        public static Floater MakeFloater(Transform parent,Vector3 localPosition,FloaterKind kind=FloaterKind.Candelabra,int seed=0,int lightedFlames=1)
        {
            var t=NewChild(kind==FloaterKind.Candelabra?"Floating candelabra":"Floating book",parent,localPosition,Quaternion.Euler(0,(seed*57)%360,0));
            var floater=t.gameObject.AddComponent<Floater>();
            floater.phase=Mathf.Repeat(seed*.6180339f,1);
            switch(kind)
            {
                case FloaterKind.Candelabra:
                {
                    NewRenderer("Candelabra",t,Vector3.zero,Quaternion.identity,CandelabraMesh,CandelabraMaterials,true);
                    var flames=new Transform[CandlePositions.Length];
                    for(int i=0;i<flames.Length;i++)
                    {
                        var flame=MakeCandleFlame(t,CandlePositions[i],FlameHeight,i<lightedFlames,"Flame "+i,.12f);   // round 13: 6 cm flame sprite, the size agreed with hc/props-l
                        flames[i]=flame.transform;
                    }
                    floater.flames=flames;
                    floater.tiltDegrees=1.5f;floater.bobAmplitude=.1f;
                    break;
                }
                default:
                    NewRenderer("Book",t,Vector3.zero,Quaternion.identity,kind==FloaterKind.OpenBook?OpenBookMesh:ClosedBookMesh,BookMaterials,true);
                    floater.tiltDegrees=7;floater.bobAmplitude=.16f;floater.yawDegreesPerSecond=6;floater.bobHertz=.17f;
                    break;
            }
            return floater;
        }

        // ---- rune ring ----
        // A floor ring of glowing runes centred at `localPosition` (world if parent
        // is null). The ring lies in the XZ plane. watcher may be null (camera).
        public static RuneRing MakeRuneRing(Transform parent,Vector3 localPosition,float radius=5.5f,int runeCount=28,Transform watcher=null)
        {
            var t=NewChild("Rune ring",parent,localPosition,Quaternion.identity);
            var ring=t.gameObject.AddComponent<RuneRing>();
            // A ring of 3 m radius carries 24 glyphs: more than that crowds the quads into a line.
            if(radius<=4f)runeCount=Mathf.Min(runeCount,24);
            ring.radius=radius;ring.runeCount=runeCount;ring.watcher=watcher;
            ring.runeSize=Mathf.Clamp(radius*.17f,.25f,.6f);
            return ring;
        }

        // ---- portrait eyes ----
        // A framed portrait whose painted eyes follow `target` (null = camera). The
        // portrait faces along the returned transform's +Z (out of the wall); place
        // its position at the picture centre. Size is the canvas in metres.
        public static PortraitEyes MakePortraitEyes(Transform parent,Vector3 localPosition,Quaternion localRotation,float width=.9f,float height=1.3f,Transform target=null)
        {
            var t=NewChild("Watching portrait",parent,localPosition,localRotation);
            var eyes=t.gameObject.AddComponent<PortraitEyes>();
            var frame=NewRenderer("Frame",t,Vector3.zero,Quaternion.identity,FrameMesh,new[]{FrameMaterial},true);
            frame.transform.localScale=new Vector3(width,height,1);
            var back=Quaternion.Euler(0,180,0); // quads show their front on -Z; turn it to face +Z
            var canvas=NewRenderer("Canvas",t,Vector3.zero,back,CentreQuad,new[]{PortraitMaterial},false);
            canvas.transform.localScale=new Vector3(width,height,1);
            var pupils=new Transform[2];
            float size=width*.045f;
            for(int i=0;i<2;i++)
            {
                float side=i==0?-1:1;
                var p=NewRenderer("Pupil",t,new Vector3(side*PortraitEyeU*width,(PortraitEyeV-.5f)*height,.004f),back,CentreQuad,new[]{PupilMaterial},false);
                p.transform.localScale=new Vector3(size,size,1);
                pupils[i]=p.transform;
            }
            eyes.pupils=pupils;eyes.target=target;
            eyes.maxOffset=width*.018f;
            return eyes;
        }

        // Pupils only, for a portrait that is already framed and painted (the Castle Portrait.fbx: front +Z, canvas centre `canvasCentre`
        // in its local space, canvas width x height metres). Same pupil layout and travel as MakePortraitEyes.
        public static PortraitEyes AttachPupils(Transform portrait,Vector3 canvasCentre,float width,float height,Transform target=null)
        {
            var eyes=portrait.gameObject.AddComponent<PortraitEyes>();
            var back=Quaternion.Euler(0,180,0);
            var pupils=new Transform[2];
            float size=width*.045f;
            for(int i=0;i<2;i++)
            {
                float side=i==0?-1:1;
                var p=NewRenderer("Pupil",portrait,canvasCentre+new Vector3(side*PortraitEyeU*width,(PortraitEyeV-.5f)*height,.004f),back,CentreQuad,new[]{PupilMaterial},false);
                p.transform.localScale=new Vector3(size,size,1);
                pupils[i]=p.transform;
            }
            eyes.pupils=pupils;eyes.target=target;
            eyes.maxOffset=width*.018f;
            return eyes;
        }

        // ---- stair loop ----
        // A trigger volume of `size` metres centred on localPosition (its floor
        // sits size.y/2 below). Armed is false until the game arms it. It lives on
        // layer 2 (Ignore Raycast) so interaction rays never hit it.
        public static StairLoop MakeStairLoop(Transform parent,Vector3 localPosition,Vector3 size,Transform destination,bool armed=false,Quaternion? localRotation=null)
        {
            var t=NewChild("Stair loop",parent,localPosition,localRotation??Quaternion.identity);
            t.gameObject.layer=2;
            var box=t.gameObject.AddComponent<BoxCollider>();box.size=size;box.isTrigger=true;
            var loop=t.gameObject.AddComponent<StairLoop>();
            loop.destination=destination;loop.Armed=armed;
            return loop;
        }

        // ---- orbiting knives ----
        // `knives` steel knives circling localPosition (the middle of the orbit
        // plane). knifeScale 1 makes ~0.33 m blades; 1.5 reads as "too big".
        public static KnifeOrbit MakeKnifeOrbit(Transform parent,Vector3 localPosition,int knives=7,float radius=.9f,float knifeScale=1.4f)
        {
            var t=NewChild("Knife orbit",parent,localPosition,Quaternion.identity);
            var orbit=t.gameObject.AddComponent<KnifeOrbit>();
            orbit.radius=radius;
            var list=new Transform[knives];
            for(int i=0;i<knives;i++)
            {
                var r=NewRenderer("Knife "+i,t,Vector3.zero,Quaternion.identity,KnifeMesh,KnifeMaterials,true);
                r.transform.localScale=Vector3.one*knifeScale;
                list[i]=r.transform;
            }
            orbit.knives=list;
            return orbit;
        }

        // ---- giant bats ----
        // A flock of `bats` (clamped to 5-8) circling `localPosition` at `height`
        // metres above it. Keep the parent chain at scale 1.
        public static GiantBat MakeGiantBat(Transform parent,Vector3 localPosition,int bats=6,float radius=6,float height=3.5f,float wingspan=1.6f)
        {
            bats=Mathf.Clamp(bats,5,8);
            var t=NewChild("Giant bats",parent,localPosition,Quaternion.identity);
            var flock=t.gameObject.AddComponent<GiantBat>();
            flock.radius=radius;flock.height=height;flock.wingspan=wingspan;
            var list=new Transform[bats];
            for(int i=0;i<bats;i++)
            {
                var r=NewRenderer("Bat "+i,t,Vector3.zero,Quaternion.identity,CentreQuad,new[]{BatMaterial},false);
                r.transform.localScale=new Vector3(wingspan,wingspan*.5f,1);
                list[i]=r.transform;
            }
            flock.bats=list;
            return flock;
        }

        // ---- floating candle ----
        // A long wax taper hanging in the air (instanced: one mesh, two shared materials). Only a `lit` candle gets a
        // flame billboard; none gets a point light.
        public static Floater MakeFloatingCandle(Transform parent,Vector3 localPosition,int seed,bool lit,float scale=1,bool hero=false)
        {
            var t=NewChild(lit?"Floating candle (lit)":"Floating candle",parent,localPosition,Quaternion.Euler(0,(seed*57)%360,0));
            t.localScale=Vector3.one*scale;
            var floater=t.gameObject.AddComponent<Floater>();
            floater.phase=Mathf.Repeat(seed*.6180339f,1);
            floater.bobAmplitude=.06f+.05f*Mathf.Repeat(seed*.381966f,1);
            floater.bobHertz=.1f+.08f*Mathf.Repeat(seed*.754877f,1);
            floater.driftRadius=.05f;floater.tiltDegrees=2f;floater.yawDegreesPerSecond=1.5f;
            NewRenderer("Candle",t,Vector3.zero,Quaternion.identity,CandleMesh,CandleMaterials,false);
            if(lit)
            {
                // ~0.11 x 0.22 m teardrop plus a soft warm halo; the hero candle burns bigger and brighter. No point lights.
                var flame=MakeCandleFlame(t,Vector3.up*.23f,hero?.34f:.22f,false,"Flame",hero?.55f:.24f);
                floater.flames=new[]{flame.transform};
            }
            return floater;
        }

        static void Drop(GameObject item){if(Application.isPlaying)UnityEngine.Object.Destroy(item);else UnityEngine.Object.DestroyImmediate(item);}

        // ---- hand-roots ----
        // A root strand hanging from the ceiling: `localPosition` is the top of the hanging part, a crown cone of
        // `crownLength` metres reaches up to the ceiling. The strand sways slowly about the top.
        public static Floater MakeHandRoot(Transform parent,Vector3 localPosition,int seed,float crownLength)
        {
            var t=NewChild("Hand-root",parent,localPosition,Quaternion.Euler(0,(seed*97)%360,0));
            var floater=t.gameObject.AddComponent<Floater>();
            floater.phase=Mathf.Repeat(seed*.6180339f,1);
            floater.bobAmplitude=0;floater.driftRadius=0;floater.yawDegreesPerSecond=0;
            floater.tiltDegrees=2.4f;floater.bobHertz=.08f;
            // round 12: modelled strand with three hands (Resources/Castle/HandRoot0..5.fbx, tools/castle_props_r12.py) and a modelled
            // collar (RootCrown.fbx, 1 m long, scaled to the ceiling distance). The procedural boxes-and-tubes mesh is the fallback.
            int variant=((seed%HandRootVariants)+HandRootVariants)%HandRootVariants;
            var art=CastleArt.Place("HandRoot"+variant,t,Vector3.zero,Quaternion.identity);
            if(art.placeholder)
            {
                Drop(art.gameObject);
                NewRenderer("Roots",t,Vector3.zero,Quaternion.identity,HandRootMesh(seed),HandRootMaterials,false);
                var crown=NewRenderer("Crown",t,Vector3.zero,Quaternion.identity,CrownMesh,RootCrownMaterials,false);
                crown.transform.localScale=new Vector3(1,Mathf.Max(.05f,crownLength),1);
            }
            else
            {
                foreach(var r in art.gameObject.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;}
                var collar=CastleArt.Place("RootCrown",t,Vector3.zero,Quaternion.identity);
                if(collar.placeholder)Drop(collar.gameObject);
                else
                {
                    collar.transform.localScale=new Vector3(1,Mathf.Max(.05f,crownLength),1);
                    foreach(var r in collar.gameObject.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;}
                }
            }
            return floater;
        }

        // ---- moon in a window ----
        // A moon sprite at `localPosition` (castle-local), turned to face `viewerLocal`. The disc is `radius` metres.
        public static Transform MakeMoon(Transform parent,Vector3 localPosition,Vector3 viewerLocal,float radius,bool red)
        {
            var t=NewChild(red?"Red moon":"Pale moon",parent,localPosition,Quaternion.LookRotation(localPosition-viewerLocal));
            var r=NewRenderer("Disc",t,Vector3.zero,Quaternion.identity,CentreQuad,new[]{red?RedMoonMaterial:PaleMoonMaterial},false);
            float size=radius/.78f*2;
            r.transform.localScale=new Vector3(size,size,1);
            var window=t.gameObject.AddComponent<WindowMoon>();window.disc=r;window.red=red;window.castleRoot=parent;
            return t;
        }

        // ---- painted-over sun window ----
        // A pointed-arch window quad `width` x `height` centred on localPosition; it faces `facing` (the room side).
        public static Transform MakeSunWindow(Transform parent,Vector3 localPosition,Vector3 facing,float width,float height)
        {
            var t=NewChild("Painted-over sun window",parent,localPosition,Quaternion.LookRotation(-facing));
            var r=NewRenderer("Glass",t,Vector3.zero,Quaternion.identity,CentreQuad,new[]{SunWindowMaterial},false);
            r.transform.localScale=new Vector3(width,height,1);
            return t;
        }

        // ---- glowing runes ----
        // One violet rune quad lying flat against a surface whose outward normal is `normal` (up = world up).
        public static Transform MakeGlowRune(Transform parent,Vector3 localPosition,Vector3 normal,float size,int glyph)
        {
            var r=NewRenderer("Glow rune",parent,localPosition,Quaternion.LookRotation(-normal,Vector3.up),RuneGlyphMesh(glyph),new[]{VioletRuneMaterial},false);
            r.transform.localScale=new Vector3(size,size,1);
            return r.transform;
        }

        // ---- chandelier ----
        // The great hall's hanging iron chandelier: `localPosition` is the hub, `chain` metres of chain reach the ceiling above.
        // 12 candles, each with a billboard flame and halo (no point light), a slow sway and a Floater so the flames lean like the rest.
        public static Floater MakeChandelier(Transform parent,Vector3 localPosition,int seed,float chain)
        {
            ChandelierChain=chain;chandelierMesh=null;   // the mesh bakes the chain length: rebuild for this call
            var t=NewChild("Chandelier",parent,localPosition,Quaternion.Euler(0,(seed*37)%360,0));
            var floater=t.gameObject.AddComponent<Floater>();
            floater.phase=Mathf.Repeat(seed*.6180339f,1);
            floater.bobAmplitude=.04f;floater.bobHertz=.08f;floater.driftRadius=.03f;floater.tiltDegrees=1.2f;floater.yawDegreesPerSecond=1.5f;
            NewRenderer("Chandelier body",t,Vector3.zero,Quaternion.identity,ChandelierMesh,ChandelierMaterials,true);
            var flames=new Transform[ChandelierCandles];
            for(int i=0;i<ChandelierCandles;i++)flames[i]=MakeCandleFlame(t,ChandelierFlameBase(i),FlameHeight,false,"Flame "+i,.2f).transform;
            floater.flames=flames;
            return floater;
        }
    }
}
