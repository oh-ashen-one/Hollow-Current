using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HollowCurrent
{
    // Round 12 (hc/art-exterior): the castle's silhouette. Places the FBX pieces authored by tools/castle_exterior.py at the castle root
    // origin (they are modelled in castle-local coordinates), re-skins their stone slots with the same world-space surfaces the front
    // veneer uses, paints the lit windows with the unlit CastleEmissive shader and the far ridge ring with CastleBackdrop, and adds
    // the exterior ground-fog bands. Nothing here has a collider and nothing here is a gameplay point: roofs, towers, rock and
    // backdrop are render-only (the layout, nav and Points code never see them). New materials: glow, backdrop, two fog layers.
    public static class CastleExterior
    {
        // Draw order inside the group is irrelevant (all opaque except the backdrop and the fog, which are Transparent-queue).
        public static readonly string[] Pieces = { "ExtRock", "ExtWalls", "ExtTowers", "ExtGate", "ExtKeep", "ExtBackdrop" };

        // Mirror of tools/castle_exterior.py (BACKDROP in the proof render): far ridge, near ridge, mist band.
        static readonly Color FarLow = new Color(.040f, .055f, .095f, 1f), FarHigh = new Color(.016f, .024f, .046f, 1f);
        static readonly Color NearLow = new Color(.022f, .030f, .055f, 1f), NearHigh = new Color(.008f, .011f, .022f, 1f);
        static readonly Color Haze = new Color(.10f, .13f, .20f, .38f);

        static Material glow, backdrop;

        public static int Build(Transform root)
        {
            var group = new GameObject("Exterior kit").transform;
            group.SetParent(root, false);
            int renderers = 0;
            foreach (var name in Pieces)
            {
                var placed = CastleArt.Place(name, group, Vector3.zero, Quaternion.identity);
                if (placed.placeholder)
                {
                    // never leave a magenta cube at the castle origin: report loudly and drop it
                    Debug.LogError("HC_MISSING_ART castle exterior piece " + name + " (run tools/castle_exterior.py and let Unity import Resources/Castle/" + name + ".fbx)");
                    Release(placed.gameObject);
                    continue;
                }
                Skin(placed.transform, name == "ExtBackdrop");
                foreach (var r in placed.transform.GetComponentsInChildren<Renderer>()) { StripCollider(r); renderers++; }
                if (name == "ExtKeep") AxisCheck(root, group, placed.transform);
            }
            renderers += FogBands(root);
            Debug.Log("HC_EXTERIOR_KIT pieces=" + Pieces.Length + " renderers=" + renderers + " (render only, no colliders)");
            return renderers;
        }

        // The pieces are authored in castle-local coordinates (keep centre z +19.5). If the FBX axis conversion ever landed the mesh mirrored
        // in z (the CastleArt.ImportYaw question, never observed so far), turn the group 180 degrees like CastleWorld does for the
        // drawbridge leaf: that restores the layout position and leaves only a harmless x mirror.
        static void AxisCheck(Transform root, Transform group, Transform keep)
        {
            Bounds b = default; bool any = false;
            foreach (var r in keep.GetComponentsInChildren<Renderer>()) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            if (!any) return;
            var c = root.InverseTransformPoint(b.center);
            if (c.z < 0f) { group.localRotation = Quaternion.Euler(0f, 180f, 0f); Debug.LogWarning("HC_EXTERIOR_AXIS keep centre local z " + c.z.ToString("F1") + " (expected +19.5): group turned 180 degrees"); }
            else Debug.Log("HC_EXTERIOR_AXIS ok keep centre local " + c.ToString("F1"));
        }

        static void Release(UnityEngine.Object o) { if (Application.isPlaying) UnityEngine.Object.Destroy(o); else UnityEngine.Object.DestroyImmediate(o); }
        static void StripCollider(Renderer r) { var c = r.GetComponent<Collider>(); if (c) Release(c); }

        static string BaseName(Material m)
        {
            if (!m) return "";
            string n = m.name; int cut = n.IndexOf(" (Instance)", StringComparison.Ordinal);
            return cut >= 0 ? n.Substring(0, cut) : n;
        }

        // Stone slots take the exterior world-space family (equal-luminance tints, see CastleWorld.StoneFamily), so the kit matches the
        // veneered front wall; slate (Castle_BlackStone), iron and cloth keep the UV material CastleArt already assigned.
        static void Skin(Transform t, bool isBackdrop)
        {
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                var slots = r.sharedMaterials; bool changed = false;
                for (int i = 0; i < slots.Length; i++)
                {
                    Material m = null;
                    switch (BaseName(slots[i]))
                    {
                        case "Castle_Stone": m = CastleArt.Surface("Castle_StoneWeathered", CastleArt.SurfaceKind.World, new Color(.86f, .86f, .88f)); break;
                        case "Castle_StoneWeathered": m = CastleArt.Surface("Castle_StoneWeathered", CastleArt.SurfaceKind.World, new Color(.80f, .80f, .82f)); break;
                        case "Castle_StoneDark": m = CastleArt.Surface("Castle_StoneDark", CastleArt.SurfaceKind.World, new Color(1.9f, 1.9f, 1.85f)); break;
                        case "Castle_StoneMossy": m = CastleArt.Surface("Castle_StoneMossy", CastleArt.SurfaceKind.World, new Color(1.05f, 1.05f, 1.05f)); break;
                        case "Castle_ExtGlow": m = Glow(); break;
                        case "Castle_ExtBackdrop": m = Backdrop(); break;
                    }
                    if (m != null) { slots[i] = m; changed = true; }
                }
                if (changed) r.sharedMaterials = slots;
                if (isBackdrop)
                {
                    r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
                    r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                }
            }
        }

        // Lit windows: unlit warm colour (same shader as the candelabra flames). Opaque, so it bakes as black emission like every
        // castle material (HouseLighting sets EmissiveIsBlack); the colour comes from the shader, not from the lightmap.
        static Material Glow()
        {
            if (glow) return glow;
            var shader = Resources.Load<Shader>("Castle/Shaders/CastleEmissive");
            if (shader == null || !shader.isSupported)
            {
                var std = Shader.Find("Standard");
                glow = new Material(std) { name = "Castle exterior glow", color = new Color(.1f, .05f, .02f) };
                glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", new Color(1.25f, .78f, .30f));
                return glow;
            }
            glow = new Material(shader) { name = "Castle exterior glow" };
            glow.SetColor("_Color", new Color(1.25f, .78f, .30f, 1f));
            return glow;
        }

        // Far ridge ring: unlit, ignores fog, alpha-blended in the Transparent queue (so the lightmap bake skips it: HouseLighting excludes
        // renderQueue >= Transparent). UV.x = layer (0 far ridge, 1 near ridge, 2 mist band), UV.y = height fraction.
        static Material Backdrop()
        {
            if (backdrop) return backdrop;
            var shader = Resources.Load<Shader>("Castle/Shaders/CastleBackdrop");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[CastleExterior] CastleBackdrop shader unavailable; using a flat dark card");
                backdrop = new Material(Shader.Find("Standard")) { name = "Castle exterior backdrop (fallback)", color = new Color(.02f, .028f, .05f) };
                return backdrop;
            }
            backdrop = new Material(shader) { name = "Castle exterior backdrop" };
            backdrop.SetColor("_FarLow", FarLow); backdrop.SetColor("_FarHigh", FarHigh);
            backdrop.SetColor("_NearLow", NearLow); backdrop.SetColor("_NearHigh", NearHigh);
            backdrop.SetColor("_Haze", Haze);
            return backdrop;
        }

        // ---- exterior ground fog ------------------------------------------------------------------------------------------------------
        // Stacked noise planes (CastleFogLayer, the shader CastleAtmosphere uses indoors) lying over the moat strips and the bank edge, so the
        // rock apron and the tower feet dissolve into mist from every reachable point. Two materials (low band, high band) shared by all
        // quads. They fade out within 2.8 m of the camera, so the bridge and the courtyard are never veiled.
        static int FogBands(Transform parent)
        {
            var shader = Resources.Load<Shader>("Castle/Shaders/CastleFogLayer");
            var noise = Resources.Load<Texture2D>("Castle/Textures/Castle_FogNoise");
            if (shader == null || !shader.isSupported || noise == null) { Debug.LogWarning("[CastleExterior] CastleFogLayer unavailable; exterior fog bands skipped"); return 0; }
            var group = new GameObject("Exterior ground fog").transform; group.SetParent(parent, false);
            var low = FogMaterial(shader, noise, "Castle exterior fog low", .30f, .18f, new Vector4(.007f, .004f, 0, 0));
            var high = FogMaterial(shader, noise, "Castle exterior fog high", .21f, .11f, new Vector4(-.005f, .006f, 0, 0));
            var quad = Quad();
            // centre x, centre z, size x, size z
            var bands = new[] { new Vector4(0, -3.4f, 46f, 11f), new Vector4(-17.2f, 14f, 9f, 36f), new Vector4(17.2f, 14f, 9f, 36f), new Vector4(0, 31.4f, 46f, 8f) };
            int n = 0;
            foreach (var b in bands)
                for (int i = 0; i < 3; i++)
                    foreach (var layer in new[] { (low, .18f + .38f * i), (high, .55f + .55f * i) })
                    {
                        var go = new GameObject("Fog layer " + n);
                        go.transform.SetParent(group, false);
                        go.transform.localPosition = new Vector3(b.x, layer.Item2 - .6f, b.y);        // moat water is at y -0.6; layers climb from there
                        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                        go.transform.localScale = new Vector3(b.z, b.w, 1f);
                        go.AddComponent<MeshFilter>().sharedMesh = quad;
                        var r = go.AddComponent<MeshRenderer>();
                        r.sharedMaterial = layer.Item1; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
                        r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                        n++;
                    }
            return n;
        }

        static Material FogMaterial(Shader shader, Texture2D noise, string name, float alpha, float tile, Vector4 scroll)
        {
            var m = new Material(shader) { name = name };
            m.SetTexture("_MainTex", noise);
            m.SetColor("_Color", new Color(.34f, .42f, .58f, 1f));
            m.SetFloat("_Alpha", alpha); m.SetFloat("_Tile", tile);
            m.SetVector("_Scroll", scroll); m.SetVector("_Fade", new Vector4(.9f, 2.8f, 0f, 0f));
            return m;
        }

        // Unit quad in XY facing -Z (rotated 90 deg about X it lies flat facing up), uv 0..1 for the edge falloff.
        static Mesh Quad()
        {
            var mesh = new Mesh { name = "Exterior fog quad" };
            mesh.SetVertices(new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) });
            mesh.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
