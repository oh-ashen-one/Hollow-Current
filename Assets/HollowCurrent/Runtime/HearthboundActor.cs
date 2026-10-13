using UnityEngine;
namespace HollowCurrent
{
    public sealed class HearthboundActor : MonoBehaviour
    {
        public Animator Animator;
        public Transform Head,LeftFoot;
        public int BoneCount;
        public int AnimatedFrames {get;private set;}
        public readonly int[] ModeFrames=new int[4];
        Quaternion previousFoot;
        int mode;
        public void Pose(int value,float speed=0,bool unscaled=false)
        {
            mode=value;Animator.updateMode=unscaled?AnimatorUpdateMode.UnscaledTime:AnimatorUpdateMode.Normal;
            Animator.SetInteger("Mode",value);Animator.SetFloat("LocomotionRate",Mathf.Clamp(speed/2.05f,.35f,2.5f));
        }
        void LateUpdate()
        {
            if(!Animator||!LeftFoot)return;
            ModeFrames[mode]++;
            // Measure hierarchical skeletal motion relative to the actor, including
            // movement inherited from thigh/shin bones, excluding navigation yaw.
            var relative=Quaternion.Inverse(transform.rotation)*LeftFoot.rotation;
            if(Quaternion.Angle(previousFoot,relative)>.01f)AnimatedFrames++;
            previousFoot=relative;
        }
    }
}
