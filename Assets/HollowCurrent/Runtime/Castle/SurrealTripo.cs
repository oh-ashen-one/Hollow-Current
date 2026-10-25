using System.Collections.Generic;
using UnityEngine;

namespace HollowCurrent
{
    // Owner 2026-10-19 ("replace all these floating candles, books and objects with Tripo 3D assets"): the floating and wall candles, the
    // floating candelabras and books, the hall chandelier, the wall sconces and the orbiting knives draw Tripo models (tools/tripo_castle_convert.py
    // -> Editor/TripoImport, output Castle -> Resources/Castle/<name>.prefab). Every call site keeps its procedural mesh as the fallback when
    // the prefab is missing, and keeps its Floater / flame logic: flames sit on the prefab's "Flame anchor <i>" children (wick tips).
    public static partial class SurrealProps
    {
        sealed class TripoPart { public Mesh mesh; public Material[] materials; public Vector3[] anchors; }
        static readonly Dictionary<string, TripoPart> tripoParts = new Dictionary<string, TripoPart>();
        internal static bool Tripo(string name, out Mesh mesh, out Material[] materials, out Vector3[] anchors)
        {
            if (!tripoParts.TryGetValue(name, out var part))
            {
                part = null;
                var prefab = Resources.Load<GameObject>("Castle/" + name);
                var filter = prefab ? prefab.GetComponentInChildren<MeshFilter>() : null;
                var renderer = filter ? filter.GetComponent<MeshRenderer>() : null;
                if (filter && filter.sharedMesh && renderer)
                {
                    var list = new List<Vector3>();
                    foreach (Transform t in prefab.transform)
                        if (t.name.StartsWith("Flame anchor", System.StringComparison.Ordinal)) list.Add(t.localPosition);
                    part = new TripoPart { mesh = filter.sharedMesh, materials = renderer.sharedMaterials, anchors = list.ToArray() };
                }
                tripoParts[name] = part;
            }
            mesh = part?.mesh; materials = part?.materials; anchors = part?.anchors;
            return part != null;
        }

        // The Tripo chandelier is the body only: its three chains and ceiling canopy stay procedural (they depend on ChandelierChain).
        static Mesh ChandelierChainsMesh
        {
            get
            {
                var b = new MeshBuilder(1);
                float R = ChandelierRadius * .82f, top = ChandelierChain;
                var apex = new Vector3(0, top, 0);
                for (int i = 0; i < 3; i++)
                {
                    float a = (i * 120 + 90) * Mathf.Deg2Rad;
                    var attach = new Vector3(Mathf.Cos(a) * R, .02f, Mathf.Sin(a) * R);
                    b.TubeSmooth(new[] { attach, Vector3.Lerp(attach, apex, .5f), apex }, new[] { .016f, .016f, .016f }, 6, 0);
                }
                b.Lathe(apex - new Vector3(0, .06f, 0), new[] { 0f, .09f, .06f, .05f, .0f }, new[] { 0f, .01f, .06f, .12f, .12f }, 8, 0);
                return b.Build("Tripo chandelier chains");
            }
        }
    }
}
