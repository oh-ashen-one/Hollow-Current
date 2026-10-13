using System;
using UnityEngine;

namespace HollowCurrent
{
    // A cheap flock of too-large bats circling a point: 5-8 camera-facing
    // billboards that flap by narrowing and widening. Startle() sends them
    // flying outward for a few seconds; they then settle back into the circle.
    // Bats move in world space around this transform, so keep its scale at 1.
    public sealed class GiantBat : MonoBehaviour
    {
        [Tooltip("Bat billboard transforms (children, quads with the bat material). Built by SurrealProps.MakeGiantBat.")]
        public Transform[] bats;
        [Tooltip("Circle radius in metres.")] public float radius=6;
        [Tooltip("Each bat's radius varies by up to this fraction.")] [Range(0,.6f)] public float radiusJitter=.25f;
        [Tooltip("Height above this transform in metres.")] public float height=3.5f;
        [Tooltip("Each bat's height varies by up to this many metres, and bobs by it.")] public float heightVariation=1.2f;
        [Tooltip("Circling speed in degrees per second.")] public float degreesPerSecond=22;
        [Tooltip("Wingspan in metres. Real bats are far smaller, which is the point.")] public float wingspan=1.6f;
        [Tooltip("Wing beats per second.")] public float flapHertz=2.2f;
        [Header("Startle")]
        [Tooltip("Seconds the flock stays scattered after Startle().")] public float startleSeconds=4;
        [Tooltip("How far they fly outward when scattered, in metres.")] public float scatterDistance=9;
        [Tooltip("If above 0, the flock startles by itself when the watcher comes this close (horizontal, metres).")] public float startleRadius;
        [Tooltip("Used with startleRadius.")] public Transform watcher;
        [Tooltip("Distance from the viewer beyond which the flock freezes to save time.")] public float updateRange=80;
        [Tooltip("Seed for the per-bat variation.")] public int seed=5;

        // Raised by Startle() so audio can play a screech.
        public event Action Startled;
        public bool IsScattered=>startleTimer>0||scatter>.05f;

        float[] angle,flap,radiusScale,heightOffset,bobPhase,scatterOut,scatterUp,scatterPhase;
        float startleTimer,scatter;
        bool ready;
        void Start(){Rebuild();}
        // Re-read the bat list and re-roll the per-bat variation.
        public void Rebuild()
        {
            int n=bats==null?0:bats.Length;
            angle=new float[n];flap=new float[n];radiusScale=new float[n];heightOffset=new float[n];bobPhase=new float[n];
            scatterOut=new float[n];scatterUp=new float[n];scatterPhase=new float[n];
            var random=new System.Random(seed);
            for(int i=0;i<n;i++)
            {
                angle[i]=(i*360f/Mathf.Max(1,n)+((float)random.NextDouble()*2-1)*20)*Mathf.Deg2Rad;
                flap[i]=(float)random.NextDouble();
                radiusScale[i]=1+((float)random.NextDouble()*2-1)*radiusJitter;
                heightOffset[i]=((float)random.NextDouble()*2-1)*heightVariation;
                bobPhase[i]=(float)random.NextDouble()*Mathf.PI*2;
                scatterOut[i]=.6f+.4f*(float)random.NextDouble();
                scatterUp[i]=((float)random.NextDouble()*2-.4f)*2.5f;
                scatterPhase[i]=(float)random.NextDouble()*Mathf.PI*2;
            }
            ready=true;
        }
        // Scatter the flock for startleSeconds, then let it re-form.
        public void Startle()
        {
            startleTimer=startleSeconds;
            Startled?.Invoke();
        }
        void Update()
        {
            if(!ready||bats==null)return;
            bool haveViewer=SurrealProps.TryGetViewer(out var viewer);
            var centre=transform.position;
            if(haveViewer&&(viewer-centre).sqrMagnitude>updateRange*updateRange)return;
            float dt=Time.deltaTime,time=Time.time;
            if(startleRadius>0&&watcher&&startleTimer<=0&&scatter<.05f)
            {
                var d=watcher.position-centre;d.y=0;
                if(d.sqrMagnitude<startleRadius*startleRadius)Startle();
            }
            startleTimer=Mathf.Max(0,startleTimer-dt);
            float target=startleTimer>0?1:0;
            scatter=Mathf.MoveTowards(scatter,target,(target>scatter?4f:.4f)*dt);
            float blend=scatter*scatter*(3-2*scatter);
            float speedMultiplier=1+scatter*1.5f,flapMultiplier=1+scatter;
            for(int i=0;i<bats.Length;i++)
            {
                var bat=bats[i];if(!bat)continue;
                angle[i]+=degreesPerSecond*speedMultiplier*Mathf.Deg2Rad*dt;
                flap[i]=Mathf.Repeat(flap[i]+flapHertz*flapMultiplier*dt,1);
                float s=Mathf.Sin(angle[i]),c=Mathf.Cos(angle[i]);
                float r=radius*radiusScale[i];
                float y=height+heightOffset[i]+Mathf.Sin(time*.6f+bobPhase[i])*heightVariation*.3f;
                var orbit=centre+new Vector3(s*r,y,c*r);
                var position=orbit;
                if(blend>0)
                {
                    float away=r+scatterDistance*scatterOut[i];
                    var scattered=centre+new Vector3(s*away+Mathf.Sin(time*3+scatterPhase[i])*1.2f,y+scatterUp[i],c*away+Mathf.Cos(time*2.6f+scatterPhase[i])*1.2f);
                    position=Vector3.Lerp(orbit,scattered,blend);
                }
                bat.position=position;
                float beat=Mathf.Abs(Mathf.Cos(flap[i]*Mathf.PI*2));
                bat.localScale=new Vector3(wingspan*(.4f+.6f*beat),wingspan*.5f*(.92f+.08f*beat),1);
                if(haveViewer)
                {
                    var to=position-viewer;
                    if(to.sqrMagnitude>1e-4f)bat.rotation=Quaternion.LookRotation(to,Vector3.up)*Quaternion.Euler(0,0,Mathf.Sin(angle[i]*2)*14);
                }
            }
        }
    }
}
