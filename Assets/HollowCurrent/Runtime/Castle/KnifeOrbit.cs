using UnityEngine;

namespace HollowCurrent
{
    // Knives circling a point (the kitchen block). Each child knife keeps its own
    // phase, speed, radius, bob and tilt, so the ring looks loose rather than
    // mechanical. Blades point along their travel (local +Z is the tip). The orbit
    // is around this transform, in its XZ plane.
    public sealed class KnifeOrbit : MonoBehaviour
    {
        [Tooltip("Knife transforms. Empty = every direct child.")]
        public Transform[] knives;
        [Tooltip("Orbit radius in metres.")] public float radius=.9f;
        [Tooltip("Each knife's radius varies by up to this fraction.")] [Range(0,.5f)] public float radiusJitter=.15f;
        [Tooltip("Orbit speed in degrees per second (slow is eerier).")] public float degreesPerSecond=14;
        [Tooltip("Each knife's speed varies by up to this fraction.")] [Range(0,.6f)] public float speedJitter=.2f;
        [Tooltip("Each knife starts up to this many degrees away from an even spacing.")] public float phaseJitterDegrees=18;
        [Tooltip("Vertical bob half-height in metres.")] public float bobAmplitude=.06f;
        [Tooltip("Bob cycles per second.")] public float bobHertz=.3f;
        [Tooltip("Tilt of the whole orbit plane in degrees.")] public float planeTiltDegrees=8;
        [Tooltip("Per-knife pitch and roll, up to this many degrees.")] public float knifeTiltDegrees=22;
        [Tooltip("Reverse the orbit direction.")] public bool counterClockwise;
        [Tooltip("Seed for the per-knife variation.")] public int seed=3;

        float[] angle,radiusScale,speedScale,bobPhase,pitch,roll;
        bool ready;
        void Start(){Rebuild();}
        // Re-read the knife list and re-roll the per-knife variation.
        public void Rebuild()
        {
            if(knives==null||knives.Length==0)
            {
                knives=new Transform[transform.childCount];
                for(int i=0;i<knives.Length;i++)knives[i]=transform.GetChild(i);
            }
            int n=knives.Length;
            angle=new float[n];radiusScale=new float[n];speedScale=new float[n];bobPhase=new float[n];pitch=new float[n];roll=new float[n];
            var random=new System.Random(seed);
            for(int i=0;i<n;i++)
            {
                angle[i]=(i*360f/Mathf.Max(1,n)+((float)random.NextDouble()*2-1)*phaseJitterDegrees)*Mathf.Deg2Rad;
                radiusScale[i]=1+((float)random.NextDouble()*2-1)*radiusJitter;
                speedScale[i]=1+((float)random.NextDouble()*2-1)*speedJitter;
                bobPhase[i]=(float)random.NextDouble()*Mathf.PI*2;
                pitch[i]=((float)random.NextDouble()*2-1)*knifeTiltDegrees;
                roll[i]=((float)random.NextDouble()*2-1)*knifeTiltDegrees;
            }
            ready=true;
        }
        void Update()
        {
            if(!ready)return;
            float dt=Time.deltaTime,time=Time.time;
            float direction=counterClockwise?-1:1;
            var plane=Quaternion.Euler(planeTiltDegrees,0,planeTiltDegrees*.4f);
            for(int i=0;i<knives.Length;i++)
            {
                var knife=knives[i];if(!knife)continue;
                angle[i]+=direction*degreesPerSecond*speedScale[i]*Mathf.Deg2Rad*dt;
                float s=Mathf.Sin(angle[i]),c=Mathf.Cos(angle[i]);
                var position=new Vector3(s,Mathf.Sin(time*bobHertz*Mathf.PI*2+bobPhase[i])*bobAmplitude/Mathf.Max(.01f,radius*radiusScale[i]),c)*(radius*radiusScale[i]);
                var tangent=new Vector3(c,0,-s)*direction;
                knife.localPosition=plane*position;
                knife.localRotation=plane*Quaternion.LookRotation(tangent,Vector3.up)*Quaternion.Euler(pitch[i],0,roll[i]);
            }
        }
    }
}
