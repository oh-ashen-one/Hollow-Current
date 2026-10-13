using UnityEngine;
namespace HollowCurrent
{
    public sealed class Door : MonoBehaviour
    {
        public Quaternion Closed;
        public bool Open;
        // Swing-clearance probe in hinge space (centre and half extents). Defaults fit
        // Bellwether's 1.6 x 2.2 m leaves; castle doors are sized from their leaf.
        public Vector3 ProbeCenter=new Vector3(.82f,1.1f,0),ProbeExtents=new Vector3(.74f,1.02f,.055f);
        // Fit the probe to a leaf of width x height whose hinge is at local x=0, floor y=0.
        // Re-measure now and return the widest clear swing in degrees (placement tools).
        public float MeasureSwing(){Measure();return Mathf.Abs(openAngle);}
        public void FitProbe(float width,float height)
        {
            ProbeCenter=new Vector3((width+.04f)*.5f,height*.5f,0);ProbeExtents=new Vector3((width-.12f)*.5f,height*.5f-.08f,.055f);measured=false;
        }
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
        // Diagnostics (editor validators): the first collider blocking the leaf at an angle.
        public string BlockerAt(float angle)
        {
            var rotation=(transform.parent?transform.parent.rotation:Quaternion.identity)*Closed*Quaternion.Euler(0,angle,0);
            int count=Physics.OverlapBoxNonAlloc(transform.position+rotation*ProbeCenter,ProbeExtents,contacts,rotation,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(contacts[i]&&!contacts[i].transform.IsChildOf(transform))return contacts[i].name+" ("+(contacts[i].transform.parent?contacts[i].transform.parent.name:"")+")";
            return "none";
        }
        bool ClearAt(float angle)
        {
            var rotation=(transform.parent?transform.parent.rotation:Quaternion.identity)*Closed*Quaternion.Euler(0,angle,0);
            var center=transform.position+rotation*ProbeCenter;
            int count=Physics.OverlapBoxNonAlloc(center,ProbeExtents,contacts,rotation,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore);
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
