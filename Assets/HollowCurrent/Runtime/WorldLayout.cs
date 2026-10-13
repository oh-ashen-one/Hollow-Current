using UnityEngine;
namespace HollowCurrent
{
    // Serialized source of the shipped map. Runtime owns case state; the scene
    // owns geometry and materials, so the house can be inspected and lit in-editor.
    public sealed class WorldLayout : MonoBehaviour
    {
        // Missing in scenes saved before maps existed: defaults to Bellwether.
        public MapId Map;
        public Transform Trace;
        public Vector3 Anchor,Induction,Voice,Print;
        public Light[] Lamps,WindowLights;
        public Transform[] Dolls;
        public Door[] Doors;
        public Vector3[] SearchPoints;
        public string[] MaterialNames;
        public Material[] Materials;
    }
}
