using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace HollowCurrent
{
    // Wave FE (fe-exterior, r13 open item 3: S4 exterior frame time mean 16.7 ms, p99 23.1 ms against p99 <= 16.7).
    // Sorts every castle renderer and light into the exterior pieces wave FD added (water, towers/kit, rock, approach: causeway, lamps,
    // landing, braziers, dead trees, shore rocks; banks; exterior fog) and the interior, and prints their cost at prepare time
    // (HC_EXTERIOR_COST: renderers, draw calls before batching, triangles, realtime lights and the forward-add passes those lights cost).
    // CaptureMode's -hc-exterior-profile uses the same groups to switch one piece off at a time in the player and measure the frame.
    // Castle-only file (HouseLighting.CastleOnly), so editing it re-bakes the castle only.
    public static class CastleExteriorCost
    {
        // The castle shell (outer wall faces, castle-local): what is inside it is "interior" unless it belongs to a named exterior piece.
        public static readonly Vector3 ShellMin = new Vector3(-14.45f, -6f, -0.45f), ShellMax = new Vector3(14.45f, 40f, 28.45f);

        public static string Group(Renderer r, Transform root)
        {
            for (var t = r.transform; t && t != root; t = t.parent)
            {
                string n = t.name;
                if (n == "Exterior ground fog") return "ext_fog";
                if (n.StartsWith("Castle / Ext", StringComparison.Ordinal)) return "kit_" + n.Substring("Castle / Ext".Length).ToLowerInvariant();
                if (n == "Far bank brazier") return "approach_braziers";
                if (n == "Castle / DeadTree") return "approach_trees";
                if (t.parent && t.parent.name == "Exterior approach")
                {
                    if (n == "Shore rock") return "approach_rocks";
                    if (n.StartsWith("Causeway", StringComparison.Ordinal)) return "approach_causeway";
                    if (n.StartsWith("Lamp", StringComparison.Ordinal)) return "approach_lamps";
                    return "approach_landing";   // paving, ruined gateway and wall
                }
                if (n.StartsWith("Blood lake", StringComparison.Ordinal) || n.StartsWith("Blood moat", StringComparison.Ordinal)) return "water";
                if (n == "Far bank" || n.StartsWith("Bank ", StringComparison.Ordinal) || n.StartsWith("Moat bed", StringComparison.Ordinal)) return "banks";
            }
            var c = root ? root.InverseTransformPoint(r.bounds.center) : r.bounds.center;
            bool inside = c.x >= ShellMin.x && c.x <= ShellMax.x && c.y >= ShellMin.y && c.y <= ShellMax.y && c.z >= ShellMin.z && c.z <= ShellMax.z;
            return inside ? "interior" : "other";
        }

        public static string LightGroup(Light l)
        {
            if (!l) return null;
            string n = l.name;
            if (n == "Facade moon fill" || n == "Keep moon fill" || n == "Bridge moon fill") return "lights_outside_fills";
            if (n == "Brazier light") return "lights_braziers";
            if (l.type == LightType.Directional) return "lights_directional";
            return "lights_interior";
        }

        // Realtime = not baked into the lightmaps. Light.lightmapBakeType is editor-only; bakingOutput is the runtime record of the bake
        // (prepare time, before the bake, reads Realtime for every candle and brazier).
        // The window spots are always baked by HouseLighting (they read Realtime at prepare time, before the bake).
        public static bool Realtime(Light l) => l && l.enabled && l.name != "Window moonlight" && !(l.bakingOutput.isBaked && l.bakingOutput.lightmapBakeType == LightmapBakeType.Baked);

        public static Dictionary<string, List<Renderer>> Classify(Transform root)
        {
            var map = new Dictionary<string, List<Renderer>>();
            if (!root) return map;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!r || !r.enabled) continue;
                string g = Group(r, root);
                if (!map.TryGetValue(g, out var list)) map[g] = list = new List<Renderer>();
                list.Add(r);
            }
            return map;
        }

        static int Triangles(Renderer r)
        {
            Mesh m = null;
            if (r is SkinnedMeshRenderer s) m = s.sharedMesh;
            else { var f = r.GetComponent<MeshFilter>(); if (f) m = f.sharedMesh; }
            if (!m) return 0;
            long n = 0;
            for (int i = 0; i < m.subMeshCount; i++) n += m.GetIndexCount(i) / 3;
            return (int)n;
        }

        // One line per group plus a total. draws = renderer x material slots (before static batching); rt_lights = realtime lights inside the
        // group; lit_passes = forward-add passes the realtime point/spot lights cost on the group's renderers (every light whose range sphere
        // touches the renderer bounds; ForcePixel lights always, Auto lights up to the 4 pixel lights the Ultra tier allows), shadowed = of those
        // lights, how many cast realtime shadows (a point light's shadow is six extra caster passes).
        public static string Log(Transform root, string stage)
        {
            var groups = Classify(root);
            var lights = root ? root.GetComponentsInChildren<Light>(true).Where(l => Realtime(l) && l.type != LightType.Directional).ToArray() : new Light[0];
            var sb = new StringBuilder();
            int tr = 0, td = 0, tl = 0, tp = 0; long tt = 0;
            foreach (var kv in groups.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                int draws = 0, passes = 0; long tris = 0;
                foreach (var r in kv.Value)
                {
                    draws += Mathf.Max(1, r.sharedMaterials.Length);
                    tris += Triangles(r);
                    int auto = 0, forced = 0; var b = r.bounds;
                    foreach (var l in lights)
                    {
                        if (b.SqrDistance(l.transform.position) > l.range * l.range) continue;
                        if (l.renderMode == LightRenderMode.ForcePixel) forced++; else if (l.renderMode != LightRenderMode.ForceVertex) auto++;
                    }
                    passes += forced + Mathf.Min(auto, Mathf.Max(0, 4 - forced));
                }
                int own = lights.Count(l => Group(l, root) == kv.Key);
                int shadowed = lights.Count(l => l.shadows != LightShadows.None && Group(l, root) == kv.Key);
                string line = "HC_EXTERIOR_COST stage=" + stage + " group=" + kv.Key + " renderers=" + kv.Value.Count + " draws=" + draws + " tris=" + tris + " rt_lights=" + own + " shadowed_lights=" + shadowed + " lit_passes=" + passes;
                Debug.Log(line); sb.AppendLine(line);
                tr += kv.Value.Count; td += draws; tt += tris; tl += own; tp += passes;
            }
            int sh = lights.Count(l => l.shadows != LightShadows.None);
            string total = "HC_EXTERIOR_COST stage=" + stage + " total renderers=" + tr + " draws=" + td + " tris=" + tt + " rt_lights=" + lights.Length + " shadowed_lights=" + sh + " lit_passes=" + tp
                + " lights_by_kind=[" + string.Join(" ", lights.GroupBy(LightGroup).OrderBy(g => g.Key).Select(g => g.Key + "=" + g.Count())) + "]";
            Debug.Log(total); sb.AppendLine(total);
            return sb.ToString();
        }

        // A light belongs to the group of the renderer-free position it stands at (braziers, lamps, candles, fills).
        static string Group(Light l, Transform root)
        {
            for (var t = l.transform; t && t != root; t = t.parent)
            {
                if (t.name == "Far bank brazier") return "approach_braziers";
                if (t.name == "Exterior approach") return "approach_landing";
            }
            var c = root ? root.InverseTransformPoint(l.transform.position) : l.transform.position;
            bool inside = c.x >= ShellMin.x && c.x <= ShellMax.x && c.y >= ShellMin.y && c.y <= ShellMax.y && c.z >= ShellMin.z && c.z <= ShellMax.z;
            return inside ? "interior" : "other";
        }

#if UNITY_EDITOR
        // ---- occlusion culling (castle bake only) ----------------------------------------------------------------------------------------
        // The castle had no occlusion data, so from outside (S4 stands 22 m out on the far landing and sees the whole castle, FOV 90) the player
        // drew every interior room, prop, candle and its forward-add light passes behind the solid outer walls. The castle bake now computes
        // Unity's occlusion data. Occluders are a whitelist of solid opaque shells only: CastleWorld's box primitives (walls, slabs, floors,
        // banks), the stone wall and floor tiles and the exterior kit (rock, curtain walls, towers, gate, keep). Everything else is cleared of
        // Occluder Static first: transparent and cut-out renderers (mist and fog planes, the floor sheen, flames, glass, the backdrop ring) and
        // meshes with gaps (balustrades, railings, grilles, shelves: a gap under SmallestHole would count as solid and hide what shows through
        // it). Everything static stays an occludee. Doors, leaves, the drawbridge and moving props are not static (NativeBuild.PersistLayoutAssets),
        // so an open gate or door never hides anything. Called from CastleLightTuning.Tune (the castle-only bake hook in HouseLighting.Bake),
        // after the prepare persisted the static flags; the bake saves the scene with the data.
        static bool SolidShell(Renderer r)
        {
            var f = r.GetComponent<MeshFilter>();
            if (f && f.sharedMesh && f.sharedMesh.name == "Cube") return true;
            for (var t = r.transform; t; t = t.parent)
            {
                string n = t.name;
                if (n == "Castle / ExtBackdrop") return false;
                if (n.StartsWith("Castle / Ext", StringComparison.Ordinal) || n == "Castle / Wall_2m" || n == "Castle / FloorTile") return true;
                if (n.StartsWith("Castle / ", StringComparison.Ordinal) || n.StartsWith("FBX / ", StringComparison.Ordinal)) return false;
            }
            return false;
        }
        public const float SmallestOccluder = 3f, SmallestHole = .25f, BackfaceThreshold = 100f;
        public static void BakeOcclusion(Transform root)
        {
            if (!root) return;
            int occluders = 0, cleared = 0, occludees = 0;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var go = r.gameObject;
                var flags = UnityEditor.GameObjectUtility.GetStaticEditorFlags(go);
                if ((flags & UnityEditor.StaticEditorFlags.OccludeeStatic) != 0) occludees++;
                if ((flags & UnityEditor.StaticEditorFlags.OccluderStatic) == 0) continue;
                bool seeThrough = !r.enabled || !(r is MeshRenderer) || r.sharedMaterials.Any(m => !m || m.renderQueue >= 2450 || m.shader == null
                                  || m.shader.name.Contains("Fog") || m.shader.name.Contains("Sheen") || m.shader.name.Contains("Flame") || m.shader.name.Contains("Sprite") || m.shader.name.Contains("Decal"));
                var size = r.bounds.size;
                bool thin = Mathf.Max(size.x, Mathf.Max(size.y, size.z)) < 1f || !SolidShell(r);   // small or gapped dressing never occludes
                if (seeThrough || thin) { UnityEditor.GameObjectUtility.SetStaticEditorFlags(go, flags & ~UnityEditor.StaticEditorFlags.OccluderStatic); cleared++; }
                else occluders++;
            }
            UnityEditor.StaticOcclusionCulling.smallestOccluder = SmallestOccluder;
            UnityEditor.StaticOcclusionCulling.smallestHole = SmallestHole;
            UnityEditor.StaticOcclusionCulling.backfaceThreshold = BackfaceThreshold;
            var started = DateTime.UtcNow;
            bool ok = UnityEditor.StaticOcclusionCulling.Compute();
            Debug.Log("HC_OCCLUSION baked=" + ok + " occluders=" + occluders + " cleared=" + cleared + " occludees=" + occludees + " smallestOccluder=" + SmallestOccluder
                + " smallestHole=" + SmallestHole + " umbraBytes=" + UnityEditor.StaticOcclusionCulling.umbraDataSize + " seconds=" + (DateTime.UtcNow - started).TotalSeconds.ToString("F1"));
            if (!ok) throw new Exception("HC_OCCLUSION castle occlusion bake failed");
        }
#endif
    }
}
