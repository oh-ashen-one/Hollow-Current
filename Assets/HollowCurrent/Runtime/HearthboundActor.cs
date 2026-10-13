using UnityEngine;
namespace HollowCurrent
{
    public sealed class HearthboundActor : MonoBehaviour
    {
        public Animator Animator;
        public Transform Head,LeftFoot;
        public int BoneCount;
        public int AnimatedFrames {get;private set;}
        // Modes 0-3 (Split Grin), 4 Stagger, 5 Death and 6 Grab (Count Claude).
        public readonly int[] ModeFrames=new int[7];
        public int Mode=>mode;
        Quaternion previousFoot;
        int mode;
        Transform[] joints;
        public void Pose(int value,float speed=0,bool unscaled=false)
        {
            mode=value;
            // Placeholder actors have an Animator without a controller.
            if(!Animator||!Animator.runtimeAnimatorController)return;
            Animator.updateMode=unscaled?AnimatorUpdateMode.UnscaledTime:AnimatorUpdateMode.Normal;
            Animator.SetInteger("Mode",value);Animator.SetFloat("LocomotionRate",Mathf.Clamp(speed/2.05f,.35f,2.5f));
        }
        // World bounds of the posed skeleton (every joint, including the Count's wing-fan end markers), grown by
        // margin plus 0.15 m for skin thickness. Cheap enough per frame; used for camera clearance and framing.
        public Bounds PoseBounds(float margin)
        {
            if(joints==null||joints.Length==0)joints=(Animator?Animator.transform:transform).GetComponentsInChildren<Transform>();
            var b=new Bounds(joints[0].position,Vector3.zero);
            foreach(var t in joints)if(t)b.Encapsulate(t.position);
            b.Expand(2*(margin+.15f));return b;
        }
        void LateUpdate()
        {
            if(!Animator||!LeftFoot)return;
            if(mode>=0&&mode<ModeFrames.Length)ModeFrames[mode]++;
            // Measure hierarchical skeletal motion relative to the actor, including
            // movement inherited from thigh/shin bones, excluding navigation yaw.
            var relative=Quaternion.Inverse(transform.rotation)*LeftFoot.rotation;
            if(Quaternion.Angle(previousFoot,relative)>.01f)AnimatedFrames++;
            previousFoot=relative;
        }
    }
}
