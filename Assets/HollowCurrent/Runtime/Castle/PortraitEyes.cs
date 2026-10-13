using UnityEngine;

namespace HollowCurrent
{
    // Pupil transforms on a portrait that slide toward a target, clamped to a
    // small angle so the effect stays subtle. The portrait faces along this
    // transform's +Z (outward from the wall); pupils are children that sit on
    // the painted eyes. Leave target empty to follow the camera.
    public sealed class PortraitEyes : MonoBehaviour
    {
        [Tooltip("What the eyes follow, usually the player. Empty = the camera (SurrealProps.Viewer or Camera.main).")]
        public Transform target;
        [Tooltip("Pupil transforms (children of this portrait). Their local position at start is the centred pose.")]
        public Transform[] pupils;
        [Tooltip("Largest pupil travel from centre, in metres (about 1/3 of an eye white).")]
        public float maxOffset=.014f;
        [Tooltip("Gaze angle (degrees, horizontal and vertical) at which the pupils reach maxOffset. Beyond it they stay put.")]
        [Range(5,60)] public float maxAngleDegrees=22;
        [Tooltip("Eyes only follow the target inside this distance (metres); beyond it they drift back to centre.")]
        public float trackRange=9;
        [Tooltip("How quickly the pupils follow, per second. Low values feel sluggish and watchful.")]
        public float followSpeed=4;

        Vector3[] rest;
        Vector2 offset;
        bool ready;
        void Start(){Rebase();}
        // Capture the pupils' current local positions as the centred pose.
        public void Rebase()
        {
            int n=pupils==null?0:pupils.Length;
            rest=new Vector3[n];
            for(int i=0;i<n;i++)if(pupils[i])rest[i]=pupils[i].localPosition;
            offset=Vector2.zero;ready=true;
        }
        void Update()
        {
            if(!ready||pupils==null||pupils.Length==0)return;
            Vector3 tp;bool have;
            if(target){tp=target.position;have=true;}else have=SurrealProps.TryGetViewer(out tp);
            var desired=Vector2.zero;
            if(have)
            {
                var local=transform.InverseTransformPoint(tp);
                if(local.z>.05f&&local.sqrMagnitude<trackRange*trackRange)
                {
                    float yaw=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
                    float pitch=Mathf.Atan2(local.y,local.z)*Mathf.Rad2Deg;
                    desired.x=Mathf.Clamp(yaw/maxAngleDegrees,-1,1)*maxOffset;
                    desired.y=Mathf.Clamp(pitch/maxAngleDegrees,-1,1)*maxOffset;
                }
            }
            offset=Vector2.Lerp(offset,desired,1-Mathf.Exp(-followSpeed*Time.deltaTime));
            if((offset-desired).sqrMagnitude<1e-10f&&desired==Vector2.zero&&offset==Vector2.zero)return;
            for(int i=0;i<pupils.Length;i++)if(pupils[i])pupils[i].localPosition=rest[i]+new Vector3(offset.x,offset.y,0);
        }
    }
}
