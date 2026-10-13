using UnityEngine;

namespace HollowCurrent
{
    // STUB: replaced by hc/art
    // Placeholder loader: returns a magenta primitive of the layout's nominal size for every name so the
    // castle is buildable and visibly unfinished. Piece 2 replaces this file with the Resources/Castle loader
    // (same signature). Origin convention: base centre, front faces +Z at yaw 0.
    public static class CastleArt
    {
        static Material placeholder;

        public static Transform Place(string name, Transform parent, Vector3 pos, Quaternion rot)
        {
            var size = CastleLayout.ArtSize(name);
            var holder = new GameObject("Castle / " + name + " (stub)");
            holder.transform.SetParent(parent, false);
            holder.transform.localPosition = pos;
            holder.transform.localRotation = rot;
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Placeholder";
            var collider = cube.GetComponent<Collider>();
            if (Application.isPlaying) Object.Destroy(collider); else Object.DestroyImmediate(collider);
            cube.transform.SetParent(holder.transform, false);
            cube.transform.localPosition = new Vector3(0, size.y * .5f, 0);
            cube.transform.localScale = size;
            if (!placeholder)
            {
                var shader = Shader.Find("Standard");
                placeholder = new Material(shader) { name = "HC / castle art placeholder", color = Color.magenta };
            }
            cube.GetComponent<Renderer>().sharedMaterial = placeholder;
            return holder.transform;
        }
    }
}
