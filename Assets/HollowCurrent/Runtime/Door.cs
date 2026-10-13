using UnityEngine;
namespace HollowCurrent
{
    public sealed class Door : MonoBehaviour
    {
        public Quaternion Closed;
        public bool Open;
        float openAngle=-110;
        readonly Collider[] contacts=new Collider[32];
        bool measured;
        float positive,negative;
        public float OpenAngle=>openAngle;
        public bool HasClearOpenPose=>measured&&ClearAt(openAngle);
        void Measure()
        {
            Physics.SyncTransforms();positive=ClearArc(1);negative=ClearArc(-1);measured=true;
            openAngle=negative>=positive?-negative:positive;
        }
        float ClearArc(int sign)
        {
            float clear=0;
            for(float angle=5;angle<=175;angle+=5)
            {
                if(!ClearAt(sign*angle))break;
                clear=angle;
            }
            return clear;
        }
        bool ClearAt(float angle)
        {
            var rotation=(transform.parent?transform.parent.rotation:Quaternion.identity)*Closed*Quaternion.Euler(0,angle,0);
            var center=transform.position+rotation*new Vector3(.82f,1.1f,0);
            int count=Physics.OverlapBoxNonAlloc(center,new Vector3(.74f,1.02f,.055f),contacts,rotation,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore);
            if(count==contacts.Length)return false;
            for(int i=0;i<count;i++)if(contacts[i]&&!contacts[i].transform.IsChildOf(transform))return false;
            return true;
        }
        public void Toggle()
        {
            if(!measured)Measure();
            var game=FindFirstObjectByType<Game>();
            if(!Open)
            {
                Measure();openAngle=negative>=positive?-negative:positive;
                if(transform.position.x<0&&game&&game.Player)
                {
                    var local=Quaternion.Inverse(transform.rotation)*(game.Player.transform.position-transform.position);
                    if(local.z<0&&negative>=85)openAngle=-negative;else if(local.z>=0&&positive>=85)openAngle=positive;
                }
                if(NativeVerification.Requested)Debug.Log("HC_DOOR_STATE "+name+" pivot="+transform.position+" positive="+positive+" negative="+negative+" selected="+openAngle);
            }
            Open=!Open;if(game&&game.Sound)game.Sound.Cue(Open?"door":"latch",transform.position+Vector3.up);
        }
        void Update()
        {
            if(!measured)Measure();
            transform.localRotation=Quaternion.RotateTowards(transform.localRotation,Closed*Quaternion.Euler(0,Open?openAngle:0,0),Time.deltaTime*150);
        }
    }
}
