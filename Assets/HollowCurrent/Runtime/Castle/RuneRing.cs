using UnityEngine;

namespace HollowCurrent
{
    // A ring of rune quads lying on the floor between two thin circles. Each rune
    // glows as the watcher (the player) walks near it, and Ignite() lights the
    // whole ring in a sweep for the fight. Glow is quantised to a few shared
    // materials per ring, so a ring costs a handful of batches and no per-frame
    // allocations. The ring lies in the XZ plane of this transform.
    public sealed class RuneRing : MonoBehaviour
    {
        [Header("Layout")]
        [Tooltip("Ring radius in metres (centre line of the runes).")] public float radius=5.5f;
        [Tooltip("Number of runes around the ring.")] [Range(6,64)] public int runeCount=28;
        [Tooltip("Rune quad size in metres.")] public float runeSize=.42f;
        [Tooltip("Height above the floor, to avoid z-fighting.")] public float lift=.02f;
        [Tooltip("Draw the two thin circles the runes sit between.")] public bool drawCircles=true;
        [Tooltip("Colour of the glow.")] public Color glowColor=new Color(.45f,.7f,1f,1f);
        [Tooltip("Seed for the glyph pattern.")] public int seed=1;

        [Header("Glow")]
        [Tooltip("The player (or any transform). Leave empty to use the camera.")] public Transform watcher;
        [Tooltip("A rune starts to glow when the watcher is closer than this (metres, horizontal).")] public float watchRadius=3.5f;
        [Tooltip("Ignore the watcher when it is more than this far above or below the ring (another floor).")] public float maxHeightDifference=2.2f;
        [Tooltip("Faint glow of an untouched rune, 0..1.")] [Range(0,.5f)] public float idleGlow=.12f;
        [Tooltip("Glow gained per second while the watcher is close.")] public float riseSpeed=3;
        [Tooltip("Glow lost per second once the watcher has passed.")] public float fallSpeed=.5f;
        [Tooltip("Seconds for the ignition sweep to go once round the ring.")] public float igniteSweepSeconds=1.4f;

        public bool Ignited {get;private set;}
        // Strongest rune glow, 0..1 (for light or audio that follows the ring).
        public float Energy {get;private set;}

        const int Levels=12;
        Material[] levelMaterials;
        MeshRenderer[] runes,circles;
        Vector3[] runeLocal;
        float[] glow;
        int[] runeLevel;
        int circleLevel=-1;
        float igniteTime;
        bool ready,settled;

        void Start(){Rebuild();}
        void OnDestroy()
        {
            if(levelMaterials!=null)foreach(var m in levelMaterials)if(m)Destroy(m);
        }
        // Light the whole ring, sweeping round once. Stays lit until Extinguish().
        public void Ignite(){if(!Ignited){Ignited=true;igniteTime=Time.time;settled=false;}}
        public void Extinguish(){Ignited=false;settled=false;}

        // (Re)create the rune and circle objects from the current fields.
        public void Rebuild()
        {
            for(int i=transform.childCount-1;i>=0;i--)
            {
                var child=transform.GetChild(i);
                if(child.name=="Rune"||child.name=="Rune circle")Destroy(child.gameObject);
            }
            if(levelMaterials!=null)foreach(var m in levelMaterials)if(m)Destroy(m);
            levelMaterials=new Material[Levels];
            for(int i=0;i<Levels;i++)
            {
                float k=i/(float)(Levels-1);
                var tint=new Color(glowColor.r*(.4f+.6f*k),glowColor.g*(.4f+.6f*k),glowColor.b*(.4f+.6f*k),k);
                levelMaterials[i]=SurrealProps.Unlit("Rune glow "+i,SurrealProps.RuneAtlas,tint);
            }
            runes=new MeshRenderer[runeCount];runeLocal=new Vector3[runeCount];glow=new float[runeCount];runeLevel=new int[runeCount];
            var random=new System.Random(seed);
            for(int i=0;i<runeCount;i++)
            {
                float a=i*Mathf.PI*2/runeCount;
                var radial=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                runeLocal[i]=radial*radius+Vector3.up*lift;
                // Flat on the floor, glyph "up" pointing outward.
                var r=SurrealProps.NewRenderer("Rune",transform,runeLocal[i],Quaternion.LookRotation(Vector3.down,radial),
                    SurrealProps.RuneGlyphMesh(random.Next(SurrealProps.RuneGlyphCount)),new[]{levelMaterials[0]},false);
                r.transform.localScale=new Vector3(runeSize,runeSize,1);
                runes[i]=r;glow[i]=idleGlow;runeLevel[i]=0;
            }
            if(drawCircles)
            {
                circles=new MeshRenderer[2];
                float gap=runeSize*.72f,width=Mathf.Max(.02f,runeSize*.07f);
                circles[0]=NewCircle(radius+gap,width);circles[1]=NewCircle(Mathf.Max(.1f,radius-gap),width);
            }
            else circles=null;
            circleLevel=-1;ready=true;settled=false;
        }
        MeshRenderer NewCircle(float circleRadius,float width)
        {
            const int segments=72;
            var vertices=new Vector3[segments*2];var uv=new Vector2[segments*2];var normals=new Vector3[segments*2];
            var solid=SurrealProps.RuneSolidUV;
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments;
                var d=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                vertices[i*2]=d*(circleRadius+width*.5f);vertices[i*2+1]=d*(circleRadius-width*.5f);
                uv[i*2]=uv[i*2+1]=solid;normals[i*2]=normals[i*2+1]=Vector3.up;
            }
            var triangles=new int[segments*6];
            for(int i=0;i<segments;i++)
            {
                int o0=i*2,i0=i*2+1,o1=((i+1)%segments)*2,i1=o1+1;
                triangles[i*6]=o0;triangles[i*6+1]=o1;triangles[i*6+2]=i0;
                triangles[i*6+3]=i0;triangles[i*6+4]=o1;triangles[i*6+5]=i1;
            }
            // Wind so the visible side faces up.
            if(Vector3.Cross(vertices[triangles[1]]-vertices[triangles[0]],vertices[triangles[2]]-vertices[triangles[0]]).y<0)
                for(int i=0;i<segments*6;i+=3){int swap=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=swap;}
            var mesh=new Mesh{name="Rune circle",vertices=vertices,uv=uv,normals=normals,triangles=triangles};
            mesh.RecalculateBounds();
            var renderer=SurrealProps.NewRenderer("Rune circle",transform,Vector3.up*lift,Quaternion.identity,mesh,new[]{levelMaterials[0]},false);
            return renderer;
        }

        void Update()
        {
            if(!ready)return;
            Vector3 w;bool have;
            if(watcher){w=watcher.position;have=true;}else have=SurrealProps.TryGetViewer(out w);
            var centre=transform.position;
            bool far=!have||(new Vector2(w.x-centre.x,w.z-centre.z).magnitude>radius+runeSize+watchRadius)||Mathf.Abs(w.y-centre.y)>maxHeightDifference+1.8f;
            if(far&&!Ignited&&settled)return;
            float dt=Time.deltaTime;
            float since=Time.time-igniteTime;
            float best=0;bool atRest=true;
            for(int i=0;i<runeCount;i++)
            {
                float target=idleGlow;
                if(Ignited)
                {
                    if(since>=i*igniteSweepSeconds/Mathf.Max(1,runeCount))target=.82f+.18f*Mathf.Sin(Time.time*2.2f+i*.55f);
                }
                else if(have&&Mathf.Abs(w.y-centre.y)<=maxHeightDifference+1.8f)
                {
                    var p=transform.TransformPoint(runeLocal[i]);
                    float d=Mathf.Sqrt((p.x-w.x)*(p.x-w.x)+(p.z-w.z)*(p.z-w.z));
                    float k=1-Mathf.Clamp01(d/Mathf.Max(.01f,watchRadius));
                    target=Mathf.Max(idleGlow,k*k*(3-2*k));
                }
                float current=glow[i];
                glow[i]=target>current?Mathf.MoveTowards(current,target,(Ignited?riseSpeed*2:riseSpeed)*dt):Mathf.MoveTowards(current,target,fallSpeed*dt);
                if(Mathf.Abs(glow[i]-idleGlow)>.004f)atRest=false;
                if(glow[i]>best)best=glow[i];
                int level=Mathf.Clamp(Mathf.RoundToInt(glow[i]*(Levels-1)),0,Levels-1);
                if(level!=runeLevel[i]){runeLevel[i]=level;runes[i].sharedMaterial=levelMaterials[level];}
            }
            Energy=best;
            if(circles!=null)
            {
                int level=Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(idleGlow*.8f,best*(Ignited?1:.9f))*(Levels-1)),0,Levels-1);
                if(level!=circleLevel){circleLevel=level;foreach(var c in circles)c.sharedMaterial=levelMaterials[level];}
            }
            settled=atRest&&!Ignited;
        }
    }
}
