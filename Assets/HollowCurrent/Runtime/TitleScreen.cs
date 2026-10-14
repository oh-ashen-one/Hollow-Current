using System.Collections.Generic;
using UnityEngine;

namespace HollowCurrent
{
    // Drawing helpers for the title screen (piece T1): the full-bleed key art with a
    // slow Ken Burns push and crossfade between maps, a drifting fog layer, a
    // vignette and a left-hand gradient, per-glyph tracked type in the display serif,
    // and cropped map-card art. Interface.Title() lays the screen out and owns input;
    // this component only draws, and destroys every texture it created in OnDestroy.
    // Nothing here bounces or pops: fades and one slow zoom only.
    public sealed class TitleScreen : MonoBehaviour
    {
        // Design values (1600 x 900 design space; screen-space ones are fractions of the screen).
        public const float KenBurnsSeconds=40f,KenBurnsZoom=.06f,KenBurnsFade=4f,MapFadeSeconds=.6f;
        public const float FogAlpha=.12f,FogSpeed=4f,FogTile=512f,LeftShade=.78f,LeftWidth=.45f;
        const string DisplayFontPath="UI/Fonts/Cinzel-Medium";

        Font display,fallback;
        Texture2D fog,vignette,leftShade,cardShade;
        Texture2D outgoing,incoming;
        int toKey=int.MinValue;
        float clock0=-1,fadeStart=-99;
        readonly Dictionary<long,GUIStyle> styles=new Dictionary<long,GUIStyle>();
        readonly Dictionary<long,float> advances=new Dictionary<long,float>();

        // Crop of each map's backdrop shown on its card (x, y from the top, width, height; fractions).
        public static Rect CropFor(MapId map)=>map==MapId.Castle?new Rect(.36f,.12f,.64f,.64f):new Rect(.30f,.17f,.64f,.64f);

        // Idempotent. `serif` is the dynamic Georgia the rest of the interface uses; it stands in when the Cinzel file is missing.
        public void Prepare(Font serif)
        {
            if(fallback)return;
            fallback=serif;display=Resources.Load<Font>(DisplayFontPath);
            Debug.Log("HC_TITLE_FONT cinzel="+(display!=null)+" path="+DisplayFontPath); // critic r10 #42
            if(!display)display=serif;
        }
        public bool HasDisplayFont=>display&&display!=fallback;

        void OnDestroy()
        {
            if(fog)Destroy(fog);if(vignette)Destroy(vignette);if(leftShade)Destroy(leftShade);if(cardShade)Destroy(cardShade);
            fog=vignette=leftShade=cardShade=null;
        }

        // ---- Backdrop --------------------------------------------------------------------------------

        public void Backdrop(Material material,Texture2D art,int key)
        {
            if(Event.current.type!=EventType.Repaint)return;
            Build();
            float now=Time.unscaledTime;
            if(clock0<0){clock0=now;incoming=art;toKey=key;outgoing=null;}
            else if(key!=toKey){outgoing=incoming;incoming=art;toKey=key;fadeStart=now;}
            float t=now-clock0,f=Smooth((now-fadeStart)/MapFadeSeconds);
            if(f>=1)outgoing=null;
            var screen=new Rect(0,0,Screen.width,Screen.height);
            if(!incoming&&!outgoing)Flat(screen,new Color(.015f,.02f,.025f));
            if(outgoing)Layers(material,outgoing,screen,t,1);
            if(incoming)Layers(material,incoming,screen,t,outgoing?f:1);
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            Fog(material,screen,t,scale);
            Draw(material,vignette,screen,new Rect(0,0,1,1),Color.white);
            Draw(material,leftShade,new Rect(0,0,screen.width*LeftWidth,screen.height),new Rect(0,0,1,1),Color.white);
        }

        // Two overlapping layers so the 40 s zoom loops without a jump: a new layer starts at 1.00 and
        // fades in over the last four seconds of the old one.
        void Layers(Material material,Texture2D texture,Rect screen,float t,float alpha)
        {
            float cycle=KenBurnsSeconds-KenBurnsFade;
            int n=Mathf.FloorToInt(t/cycle);float age=t-n*cycle;
            if(n>0&&age<KenBurnsFade)Layer(material,texture,screen,age+cycle,alpha);
            Layer(material,texture,screen,age,n==0?alpha:alpha*Smooth(age/KenBurnsFade));
        }

        void Layer(Material material,Texture2D texture,Rect screen,float age,float alpha)
        {
            if(alpha<=.001f)return;
            float s=1+KenBurnsZoom*Mathf.Clamp01(age/KenBurnsSeconds);
            float cover=Mathf.Max(screen.width/texture.width,screen.height/texture.height);
            float w=texture.width*cover,h=texture.height*cover;
            var pivot=new Vector2(screen.width*.62f,screen.height*.5f);
            float x0=screen.center.x-w*.5f,y0=screen.center.y-h*.5f;
            var rect=new Rect(pivot.x+(x0-pivot.x)*s,pivot.y+(y0-pivot.y)*s,w*s,h*s);
            Draw(material,texture,rect,new Rect(0,0,1,1),new Color(1,1,1,alpha));
        }

        // Tiling noise scrolling at four design pixels a second over the lower (moat) half, faded in
        // by horizontal strips so there is no hard top edge.
        void Fog(Material material,Rect screen,float t,float scale)
        {
            float top=screen.height*.42f,tile=FogTile*scale;const int strips=16;float h=(screen.height-top)/strips;
            float u=-t*FogSpeed*scale/tile;
            for(int i=0;i<strips;i++)
            {
                float y=top+i*h,k=(i+.5f)/strips,ramp=Smooth(k/.6f);
                Draw(material,fog,new Rect(0,y,screen.width,h),new Rect(u,-(y+h)/tile,screen.width/tile,h/tile),new Color(1,1,1,FogAlpha*ramp));
            }
        }

        static float Smooth(float x){x=Mathf.Clamp01(x);return x*x*(3-2*x);}

        void Flat(Rect r,Color c){var old=GUI.color;GUI.color=c.linear;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}

        // Draws in screen pixels: Graphics.DrawTexture ignores the design-space matrix, so callers pass final rects.
        void Draw(Material material,Texture texture,Rect screen,Rect source,Color tint)
        {
            if(!texture||tint.a<=.001f)return;
            var matrix=GUI.matrix;GUI.matrix=Matrix4x4.identity;
            material.SetColor("_Tint",tint.linear);
            Graphics.DrawTexture(screen,texture,source,0,0,0,0,Color.white,material);
            GUI.matrix=matrix;
        }

        // ---- Map cards -------------------------------------------------------------------------------

        // Cropped art plus a bottom shade, drawn in design space (rect transformed by the current GUI matrix).
        public void CardArt(Material material,Rect design,Texture2D art,Rect crop,float brightness)
        {
            if(Event.current.type!=EventType.Repaint)return;
            Build();
            var matrix=GUI.matrix;var p=matrix.MultiplyPoint3x4(new Vector3(design.x,design.y,0));var size=matrix.MultiplyVector(new Vector3(design.width,design.height,0));
            var screen=new Rect(p.x,p.y,size.x,size.y);
            if(art)Draw(material,art,screen,new Rect(crop.x,1-crop.yMax,crop.width,crop.height),new Color(brightness,brightness,brightness,1));
            float shadeTop=.34f;
            Draw(material,cardShade,new Rect(screen.x,screen.y+screen.height*shadeTop,screen.width,screen.height*(1-shadeTop)),new Rect(0,0,1,1),Color.white);
        }

        // ---- Tracked type ----------------------------------------------------------------------------

        GUIStyle Style(bool useDisplay,int size,FontStyle fontStyle)
        {
            long key=((useDisplay?1L:0L)<<40)|((long)size<<8)|(long)fontStyle;
            if(!styles.TryGetValue(key,out var style))
            {
                style=new GUIStyle(GUI.skin.label){font=useDisplay?display:fallback,fontSize=size,fontStyle=fontStyle,padding=new RectOffset(),margin=new RectOffset(),wordWrap=false,clipping=TextClipping.Overflow,alignment=TextAnchor.UpperLeft};
                styles[key]=style;
            }
            return style;
        }

        float Advance(GUIStyle style,bool useDisplay,int size,FontStyle fontStyle,char c)
        {
            long key=((useDisplay?1L:0L)<<40)|((long)size<<24)|((long)fontStyle<<20)|c;
            if(!advances.TryGetValue(key,out float w)){w=style.CalcSize(new GUIContent(c.ToString())).x;advances[key]=w;}
            return w;
        }

        // Height of one text line box in design pixels (used to centre or baseline-align labels).
        public float LineBox(int size,bool useDisplay=true,FontStyle fontStyle=FontStyle.Normal)=>Style(useDisplay,size,fontStyle).lineHeight;

        // Width a tracked string would occupy.
        public float Measure(string text,int size,float tracking,bool useDisplay=true,FontStyle fontStyle=FontStyle.Normal)
        {
            var style=Style(useDisplay,size,fontStyle);float w=0;
            for(int i=0;i<text.Length;i++)w+=Advance(style,useDisplay,size,fontStyle,text[i])+(i<text.Length-1?tracking:0);
            return w;
        }

        // Draws one glyph at a time with extra spacing (like Interface.Track, at any size). Returns the width.
        public float Tracked(string text,float x,float y,int size,float tracking,Color color,bool useDisplay=true,FontStyle fontStyle=FontStyle.Normal)
        {
            var style=Style(useDisplay,size,fontStyle);float start=x;
            var old=style.normal.textColor;style.normal.textColor=color;
            for(int i=0;i<text.Length;i++)
            {
                float w=Advance(style,useDisplay,size,fontStyle,text[i]);
                if(text[i]!=' '&&Event.current.type==EventType.Repaint)GUI.Label(new Rect(x,y,w+size,style.lineHeight+4),text[i].ToString(),style);
                x+=w+tracking;
            }
            style.normal.textColor=old;
            return x-start-tracking;
        }

        // ---- Procedural textures (all destroyed in OnDestroy) ------------------------------------------

        void Build()
        {
            if(fog)return;
            fog=NoiseTexture(256);
            vignette=Make(128,128,(u,v)=>{float dx=u*2-1,dy=v*2-1,d=Mathf.Sqrt(dx*dx+dy*dy);return new Color(0,0,0,.66f*Smooth((d-.52f)/.88f));},TextureWrapMode.Clamp);
            leftShade=Make(256,2,(u,v)=>new Color(0,0,0,LeftShade*(1-u)),TextureWrapMode.Clamp);
            cardShade=Make(2,64,(u,v)=>new Color(0,0,0,.88f*Smooth(1-v)),TextureWrapMode.Clamp);
        }

        // v runs from the bottom (0) to the top (1) of the texture; cardShade is darkest at its bottom row.
        static Texture2D Make(int w,int h,System.Func<float,float,Color> pixel,TextureWrapMode wrap)
        {
            var texture=new Texture2D(w,h,TextureFormat.RGBA32,false){wrapMode=wrap,filterMode=FilterMode.Bilinear,hideFlags=HideFlags.HideAndDontSave};
            var pixels=new Color32[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)pixels[y*w+x]=pixel((x+.5f)/w,(y+.5f)/h);
            texture.SetPixels32(pixels);texture.Apply(false,true);return texture;
        }

        static Texture2D NoiseTexture(int size)
        {
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear,hideFlags=HideFlags.HideAndDontSave};
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=(float)x/size,v=(float)y/size,n=0,amp=.5f,sum=0;
                for(int octave=0,period=3;octave<5;octave++,period*=2){n+=amp*Lattice(u*period,v*period,period,octave*17+3);sum+=amp;amp*=.5f;}
                n/=sum;float density=Smooth((n-.30f)/.5f);
                pixels[y*size+x]=new Color32(190,200,212,(byte)Mathf.RoundToInt(255*density));
            }
            texture.SetPixels32(pixels);texture.Apply(false,true);return texture;
        }

        // Smooth value noise that wraps every `period` cells, so the texture tiles.
        static float Lattice(float x,float y,int period,int seed)
        {
            int x0=Mathf.FloorToInt(x),y0=Mathf.FloorToInt(y);float fx=Smooth(x-x0),fy=Smooth(y-y0);
            float a=Hash(Wrap(x0,period),Wrap(y0,period),seed),b=Hash(Wrap(x0+1,period),Wrap(y0,period),seed);
            float c=Hash(Wrap(x0,period),Wrap(y0+1,period),seed),d=Hash(Wrap(x0+1,period),Wrap(y0+1,period),seed);
            return Mathf.Lerp(Mathf.Lerp(a,b,fx),Mathf.Lerp(c,d,fx),fy);
        }
        static int Wrap(int i,int period)=>((i%period)+period)%period;
        static float Hash(int x,int y,int seed)
        {
            unchecked{uint h=(uint)(x*374761393+y*668265263+seed*144665);h=(h^(h>>13))*1274126177u;h^=h>>16;return (h&0xffff)/65535f;}
        }
    }
}
