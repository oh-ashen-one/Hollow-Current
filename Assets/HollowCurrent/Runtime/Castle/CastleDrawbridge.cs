using UnityEngine;

namespace HollowCurrent
{
    // Moved out of CastleWorld.cs by piece 0: a MonoBehaviour saved in a prepared
    // scene needs its own script file named after the class.
    // Raises and lowers the drawbridge leaf about the hinge at the gate. Starts raised (the gate is shut).
    public sealed class CastleDrawbridge : MonoBehaviour
    {
        public const float RaisedAngle = 90f;
        public Transform Hinge;
        public bool Lowered;
        public float Angle = RaisedAngle;
        public float DegreesPerSecond = 30f;
        public bool Moving => !Mathf.Approximately(Angle, Lowered ? 0f : RaisedAngle);
        public void Lower() { Lowered = true; }
        public void Raise() { Lowered = false; }
        public void Snap(bool lowered) { Lowered = lowered; Angle = lowered ? 0f : RaisedAngle; Apply(); }
        void Apply() { (Hinge ? Hinge : transform).localRotation = Quaternion.Euler(Angle, 0, 0); }
        void Update()
        {
            if (!Moving) return;
            Angle = Mathf.MoveTowards(Angle, Lowered ? 0f : RaisedAngle, DegreesPerSecond * Time.deltaTime);
            Apply();
        }
    }
}
