using System;
using UnityEngine;

namespace HollowCurrent
{
    // The service stair that sometimes returns you to the room you left. A
    // trigger volume: while Armed, the first CharacterController to enter is
    // moved to `destination` (position keeps its offset inside the volume, facing
    // is turned by the angle between the two transforms), Fired is raised, and
    // Armed drops to false so it happens once per investigation. Whoever owns the
    // investigation sets Armed=true when a run starts and listens to Fired.
    // Needs a trigger Collider on the same object (added automatically if missing).
    public sealed class StairLoop : MonoBehaviour
    {
        [Tooltip("The loop only fires while this is true. Starts false so nothing teleports until the game arms it. Cleared when fired.")]
        public bool Armed;
        [Tooltip("Where the player reappears. Its position and yaw define the exit; place it in the room the player left.")]
        public Transform destination;
        [Tooltip("Optional: only this transform (or its children) can trigger the loop. Empty = any CharacterController.")]
        public Transform player;

        // Raised after the player has been moved. LastYawDelta and LastPlayer describe the move.
        public event Action Fired;
        public bool HasFired {get;private set;}
        // Degrees the player's facing was turned by the last teleport (read it in a Fired handler to rotate a separate camera pitch/yaw).
        public float LastYawDelta {get;private set;}
        public Transform LastPlayer {get;private set;}

        public void Arm(){Armed=true;}
        public void Disarm(){Armed=false;}
        // Re-arm for a new investigation.
        public void ResetForNewRun(){HasFired=false;Armed=true;}

        void Start()
        {
            var c=GetComponent<Collider>();
            if(!c){var box=gameObject.AddComponent<BoxCollider>();box.size=new Vector3(1.2f,2,1.2f);c=box;}
            c.isTrigger=true;
        }
        void OnTriggerEnter(Collider other){TryFire(other);}
        // Covers arming while the player is already standing in the volume.
        void OnTriggerStay(Collider other){if(Armed)TryFire(other);}
        void TryFire(Collider other)
        {
            if(!Armed||!destination)return;
            if(!other.TryGetComponent<CharacterController>(out var controller))return;
            var root=controller.transform;
            if(player&&root!=player&&!root.IsChildOf(player))return;
            Armed=false;HasFired=true;
            float yaw=Mathf.DeltaAngle(transform.eulerAngles.y,destination.eulerAngles.y);
            var offset=Quaternion.Inverse(transform.rotation)*(root.position-transform.position);
            var position=destination.position+destination.rotation*offset;
            var rotation=Quaternion.Euler(0,yaw,0)*root.rotation;
            // A CharacterController overwrites teleports unless it is off while it moves.
            controller.enabled=false;
            root.SetPositionAndRotation(position,rotation);
            controller.enabled=true;
            Physics.SyncTransforms();
            LastYawDelta=yaw;LastPlayer=root;
            Fired?.Invoke();
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color=Armed?new Color(1,.3f,.2f,.8f):new Color(.4f,.4f,.4f,.8f);
            var c=GetComponent<BoxCollider>();
            Gizmos.matrix=transform.localToWorldMatrix;
            if(c)Gizmos.DrawWireCube(c.center,c.size);
            Gizmos.matrix=Matrix4x4.identity;
            if(destination){Gizmos.DrawLine(transform.position,destination.position);Gizmos.DrawWireSphere(destination.position,.3f);}
        }
    }
}
