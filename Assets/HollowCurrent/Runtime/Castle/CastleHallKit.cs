using System.Collections.Generic;
using UnityEngine;

namespace HollowCurrent
{
    // Round 13 (hc/hall-l): places the great hall's gothic architecture authored by tools/castle_hall_kit.py (clustered columns, the ribbed
    // vault with its corbels, the plinth and string course along the walls). Pure visuals: no colliders (the hall keeps its square pier
    // colliders and cube walls). Called from CastleWorld.Pier / HallArcade / HallDressing; CastleWorld keeps its old code as the placeholder
    // fallback (CastleArt returns a magenta cube when an FBX is missing, which `placed.placeholder` reports).
    public static class CastleHallKit
    {
        public const string Column = "HallColumn", Corbel = "HallCorbel", Plinth = "HallPlinth", StringCourse = "HallStringCourse", Vault = "HallVault";
        // Hall geometry shared with the generator (tools/castle_hall_kit.py): inner wall faces including the 0.125 m veneer, spring heights.
        public const float WallX = 5.575f, WallSouth = 14.425f, WallNorth = 27.425f, CorbelNose = 5.30f, StringCourseY = 5.5f;

        // Architecture is batching-static (merged draw calls, GI): children too, the FBX parts sit below the root. Editor only: the prepared
        // scene stores the flags.
        public static void MarkStatic(Transform t)
        {
#if UNITY_EDITOR
            if (!t) return;
            foreach (var tr in t.GetComponentsInChildren<Transform>(true)) tr.gameObject.isStatic = true;
#endif
        }

        static void NoShadows(Transform t)
        {
            foreach (var r in t.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
        }

        // One cluster column: a bundle of four shafts with base, ring bands at the arcade imposts and a bell capital under an octagonal abacus,
        // top at y 5.85 (the springing of the vault). `foot` is the floor centre.
        public static Transform PlaceColumn(Transform root, Vector3 foot)
        {
            var placed = CastleArt.Place(Column, root, foot, Quaternion.identity);
            if (placed.placeholder) { Object.DestroyImmediate(placed.gameObject); return null; }
            foreach (var c in placed.gameObject.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            placed.gameObject.name = "Hall column";
            MarkStatic(placed.transform);
            return placed.transform;
        }

        // The ribbed vault: webbing and ribs over the nave (x +-3.9), four rib springers on the end walls. The vault is lit by light probes, the
        // realtime lights and a faint cold emission (the same recipe as the old cove ceiling, VaultCeiling marks it movable so the bake leaves it
        // alone): a baked ceiling this high in a dark hall comes out black. Returns false if the art is missing.
        public static bool BuildVault(Transform root)
        {
            var placed = CastleArt.Place(Vault, root, Vector3.zero, Quaternion.identity);
            if (placed.placeholder) { Object.DestroyImmediate(placed.gameObject); return false; }
            var v = placed.transform;
            v.name = "Hall vault ribs";
            foreach (var c in v.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            v.gameObject.AddComponent<VaultCeiling>();
            var stone = CastleArt.Surface("Castle_DressedStone", CastleArt.SurfaceKind.Prop, new Color(.80f, .82f, .92f));
            if (stone)
            {
                var m = new Material(stone) { name = "HC castle / hall vault stone" };
                if (m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(.030f, .040f, .070f)); }
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                foreach (var r in v.GetComponentsInChildren<Renderer>(true))
                {
                    var slots = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < slots.Length; i++) slots[i] = m;
                    r.sharedMaterials = slots;
                }
            }
            NoShadows(v);
            // rib springers where the end-wall ribs have no column: south wall faces +z (yaw 0), north wall faces -z (yaw 180)
            foreach (int side in new[] { -1, 1 })
            {
                SpringCorbel(v, new Vector3(side * 3.9f, CorbelNose, WallSouth), 0);
                SpringCorbel(v, new Vector3(side * 3.9f, CorbelNose, WallNorth), 180);
            }
            return true;
        }

        static void SpringCorbel(Transform parent, Vector3 pos, float yaw)
        {
            var placed = CastleArt.Place(Corbel, parent, pos, Quaternion.Euler(0, yaw, 0));
            if (placed.placeholder) { Object.DestroyImmediate(placed.gameObject); return; }
            foreach (var c in placed.gameObject.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            placed.gameObject.name = "Hall rib springer";
            NoShadows(placed.transform);
        }

        // Moulded plinth (0.44 high) and a string course (y 5.5, below the clerestory sills) round the hall walls, with gaps for doors, the dais,
        // the banners and the armoire. Modules are 4 m long, stretched to fit each run.
        public static void BuildTrim(Transform root)
        {
            var trim = new GameObject("Hall trim").transform;
            trim.SetParent(root, false);
            // run along z on the side walls, along x on the end walls. Each run: (wall coordinate, yaw, along range, skip intervals)
            var skipEast = new[] { (15.0f, 17.0f), (20.3f, 21.7f) };
            var skipWest = new[] { (15.0f, 17.0f), (24.3f, 25.7f) };
            Run(trim, Plinth, 0f, new Vector3(-WallX, 0, 0), 90, true, WallSouth, WallNorth, skipWest);
            Run(trim, Plinth, 0f, new Vector3(WallX, 0, 0), -90, true, WallSouth, WallNorth, skipEast);
            Run(trim, Plinth, 0f, new Vector3(0, 0, WallSouth), 0, false, -WallX, WallX, new[] { (-1.9f, 1.9f) });
            Run(trim, Plinth, 0f, new Vector3(0, 0, WallNorth), 180, false, -WallX, WallX, new[] { (-1.2f, 1.2f) });
            // string course: the banners hang at z 18.8-20.2 and 22.6-24.0 on the side walls and x +-1.6..3.2 on the north wall
            // wave FF: and at the blind gallery lancets (z 20.85-22.35, sill 5.1), which the course at 5.5 would cross
            var blind = (CastleLayout.BlindLancetZ - CastleLayout.BlindLancetWidth * .5f - .1f, CastleLayout.BlindLancetZ + CastleLayout.BlindLancetWidth * .5f + .1f);
            var bannersSide = new[] { (15.0f, 17.0f), (18.7f, 20.3f), blind, (22.5f, 24.1f), (24.3f, 25.7f) };
            Run(trim, StringCourse, StringCourseY, new Vector3(-WallX, 0, 0), 90, true, WallSouth, WallNorth, bannersSide);
            Run(trim, StringCourse, StringCourseY, new Vector3(WallX, 0, 0), -90, true, WallSouth, WallNorth, new[] { (15.0f, 17.0f), (18.7f, 20.3f), blind, (22.5f, 24.1f) });
            Run(trim, StringCourse, StringCourseY, new Vector3(0, 0, WallSouth), 0, false, -WallX, WallX, new[] { (-2.4f, 2.4f) });
            // wave FD: the north run also stops at the gallery lancets (x +-4.35..5.35, sill 3.6), which it would otherwise cross at 5.5
            Run(trim, StringCourse, StringCourseY, new Vector3(0, 0, WallNorth), 180, false, -WallX, WallX, new[] { (-5.6f, -4.25f), (-3.3f, 3.3f), (4.25f, 5.6f) });
            MarkStatic(trim);
        }

        static void Run(Transform parent, string art, float y, Vector3 wallPoint, float yaw, bool alongZ, float a0, float a1, (float, float)[] skips)
        {
            var cuts = new List<(float, float)>();
            float cursor = a0;
            var ordered = new List<(float, float)>(skips); ordered.Sort((p, q) => p.Item1.CompareTo(q.Item1));
            foreach (var s in ordered)
            {
                if (s.Item1 > cursor + .3f) cuts.Add((cursor, Mathf.Min(s.Item1, a1)));
                cursor = Mathf.Max(cursor, s.Item2);
            }
            if (a1 > cursor + .3f) cuts.Add((cursor, a1));
            foreach (var (c0, c1) in cuts)
            {
                float len = c1 - c0; int n = Mathf.Max(1, Mathf.CeilToInt(len / 4f)); float seg = len / n;
                for (int i = 0; i < n; i++)
                {
                    float mid = c0 + (i + .5f) * seg;
                    var pos = alongZ ? new Vector3(wallPoint.x, y, mid) : new Vector3(mid, y, wallPoint.z);
                    var placed = CastleArt.Place(art, parent, pos, Quaternion.Euler(0, yaw, 0));
                    if (placed.placeholder) { Object.DestroyImmediate(placed.gameObject); return; }
                    foreach (var c in placed.gameObject.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                    placed.transform.localScale = new Vector3(seg / 4f, 1, 1);   // local x is the length of the module
                }
            }
        }
    }
}
