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
        static Vector3 viewerPosition;
        static bool viewerFound;
        public static bool TryGetViewer(out Vector3 position)
        {
            if(viewerFrame!=Time.frameCount)
            {
                viewerFrame=Time.frameCount;
                var t=Viewer;
                if(!t){var cam=Camera.main;t=cam?cam.transform:null;}
                viewerFound=t;viewerPosition=t?t.position:Vector3.zero;
            }
            position=viewerPosition;return viewerFound;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Viewer=null;GlowShader=null;viewerFrame=-1;viewerFound=false;
            ResetAssets();
        }
        static partial void ResetAssets();

        // ---- materials ----
        public static Material Unlit(string name,Texture texture,Color tint)
        {
            var shader=GlowShader?GlowShader:Shader.Find("Sprites/Default");
            if(!shader)shader=Shader.Find("Standard");
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
            texture.SetPixels32(pixels);texture.Apply(true,true);
            return texture;
        }
        static float Smooth(float edge0,float edge1,float x){float k=Mathf.Clamp01((x-edge0)/(edge1-edge0));return k*k*(3-2*k);}

        // ---- candle flame ----
        static Texture2D flameTexture;
        static Material flameMaterial;
        static Texture2D FlameTexture=>flameTexture?flameTexture:flameTexture=MakeTexture("Surreal flame",32,64,(u,v)=>
        {
            float width=.5f*Mathf.Pow(Mathf.Sin(Mathf.PI*Mathf.Pow(v,.6f)),.8f)*(1-.25f*v);
            float d=Mathf.Abs(u-.5f);
            float edge=width<.002f?0:Mathf.Clamp01((width-d)/.09f);
            float core=Mathf.Clamp01(1-d/Mathf.Max(width,.001f))*(1-v*.6f);
            var c=Color.Lerp(new Color(1,.42f,.08f),new Color(1,.95f,.7f),Mathf.Pow(core,1.5f));
            c.a=edge*(.55f+.45f*core);
            return c;
        });
        static Material FlameMaterial=>flameMaterial?flameMaterial:flameMaterial=Unlit("Surreal flame",FlameTexture,Color.white);

        public static CandleFlame MakeCandleFlame(Transform parent,Vector3 localPosition,float height=.11f,bool withLight=false,string name="Candle flame")
        {
            var t=NewChild(name,parent,localPosition,Quaternion.identity);
            var flame=t.gameObject.AddComponent<CandleFlame>();
            flame.size=height;
            var renderer=NewRenderer("Billboard",t,Vector3.zero,Quaternion.identity,BaseQuad,new[]{FlameMaterial},false);
            renderer.transform.localScale=new Vector3(height*flame.widthRatio,height,1);
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
                        var flame=MakeCandleFlame(t,CandlePositions[i],.11f,i<lightedFlames,"Flame "+i);
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
            ring.radius=radius;ring.runeCount=runeCount;ring.watcher=watcher;
            ring.runeSize=Mathf.Clamp(radius*.075f,.25f,.6f);
            return ring;
        }
    }
}
