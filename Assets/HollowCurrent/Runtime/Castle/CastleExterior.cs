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
    // backdrop are render-only (the layout, nav and Points code never see them). Six new materials: windows (atlas), backdrop, two fog
    // layers, and two exterior stone instances (the same world-space stone as the veneer with a stronger ambient-fill term).
    public static class CastleExterior
    {
        // Draw order inside the group is irrelevant (all opaque except the backdrop and the fog, which are Transparent-queue).
        public static readonly string[] Pieces = { "ExtRock", "ExtWalls", "ExtTowers", "ExtGate", "ExtKeep", "ExtBackdrop" };

        // Mirror of tools/castle_exterior.py BACKDROP. DISPLAY colours: the project is Linear, and Material.SetColor on a non-HDR Color
        // property takes them as typed. Round 12 polish: the first values (.04 .055 .095) rendered at 10-15/255 in the captures.
        static readonly Color FarLow = new Color(.20f, .25f, .38f, 1f), FarHigh = new Color(.085f, .105f, .19f, 1f);
        static readonly Color NearLow = new Color(.10f, .13f, .21f, 1f), NearHigh = new Color(.04f, .05f, .095f, 1f);
        static readonly Color Haze = new Color(.20f, .26f, .40f, .40f);
        // Exterior stone: the veneer's own tints (CastleWorld.StoneFamily) with the shader's ambient-fill term raised, so the keep and gate read
        // as moonlit stone instead of black next to the warm-lit courtyard walls. _FillAmount multiplies albedo * (_CastleFillSky/Ground).
        const float StoneFill = 3.0f;

        static Material backdrop, windows, stoneLight, stoneDark, basalt, moat;

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
                Skin(placed.transform, name == "ExtBackdrop", name == "ExtRock");
                foreach (var r in placed.transform.GetComponentsInChildren<Renderer>()) { StripCollider(r); renderers++; }
                if (name == "ExtKeep") AxisCheck(root, group, placed.transform);
            }
            renderers += FogBands(root);
            Debug.Log("HC_EXTERIOR_KIT pieces=" + Pieces.Length + " renderers=" + renderers + " (render only, no colliders)");
            return renderers;
        }

        // The pieces are authored in castle-local coordinates (keep centre z +20.4). If the FBX axis conversion ever landed the mesh mirrored
        // in z (the CastleArt.ImportYaw question, never observed so far), turn the group 180 degrees like CastleWorld does for the
        // drawbridge leaf: that restores the layout position and leaves only a harmless x mirror.
        static void AxisCheck(Transform root, Transform group, Transform keep)
        {
            Bounds b = default; bool any = false;
            foreach (var r in keep.GetComponentsInChildren<Renderer>()) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            if (!any) return;
            var c = root.InverseTransformPoint(b.center);
            if (c.z < 0f) { group.localRotation = Quaternion.Euler(0f, 180f, 0f); Debug.LogWarning("HC_EXTERIOR_AXIS keep centre local z " + c.z.ToString("F1") + " (expected +20.4): group turned 180 degrees"); }
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
        static void Skin(Transform t, bool isBackdrop, bool isRock)
        {
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                var slots = r.sharedMaterials; bool changed = false;
                for (int i = 0; i < slots.Length; i++)
                {
                    Material m = null;
                    switch (BaseName(slots[i]))
                    {
                        case "Castle_Stone":
                        case "Castle_StoneWeathered": m = StoneLight(); break;
                        case "Castle_StoneDark": m = StoneDark(); break;
                        case "Castle_StoneMossy": m = StoneDark(); break;
                        case "Castle_Basalt": m = Basalt(); break;
                        case "Castle_ExtWindow": m = Windows(); break;
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

        static Material ExtStone(string surface, Color tint, string name)
        {
            var basis = CastleArt.Surface(surface, CastleArt.SurfaceKind.World, tint);
            if (!basis) return null;
            var m = new Material(basis) { name = name };
            if (m.HasProperty("_FillAmount")) m.SetFloat("_FillAmount", StoneFill);
            return m;
        }
        static Material StoneLight() { if (!stoneLight) stoneLight = ExtStone("Castle_StoneWeathered", new Color(.84f, .84f, .86f), "Castle exterior stone"); return stoneLight; }
        static Material StoneDark() { if (!stoneDark) stoneDark = ExtStone("Castle_StoneDark", new Color(1.9f, 1.9f, 1.85f), "Castle exterior stone dark"); return stoneDark; }

        // Wave L: the rock outcrop is dark basalt (Castle_Basalt_{albedo,normal,mask}.png from tools/castle_exterior_basalt.py, 0.3 tiles per metre,
        // CastleTriplanar, same ambient-fill boost as the exterior stone); falls back to the dark stone instance if the textures are missing.
        static Material Basalt()
        {
            if (!basalt) basalt = ExtStone("Castle_Basalt", new Color(1.55f, 1.55f, 1.6f), "Castle exterior basalt");
            return basalt ? basalt : StoneDark();
        }

        // Wave L: the moat. HollowCurrent/CastleWater (dark blood body, analytic ripples, fresnel reflection of the analytic night sky, both moons
        // and the castle mass) replaces the flat Castle_Blood liquid strip on the four moat boxes. Null if the shader is missing: the caller keeps
        // the old liquid. The crypt blood channels are untouched.
        public static Material Moat()
        {
            if (moat) return moat;
            var shader = Resources.Load<Shader>("Castle/Shaders/CastleWater");
            if (shader == null || !shader.isSupported) { Debug.LogWarning("[CastleExterior] CastleWater unavailable; the moat keeps the Castle_Blood liquid"); return null; }
            moat = new Material(shader) { name = "Castle exterior moat water" };
            // wave FD: the warm glints of the far-bank braziers and the causeway lamps (Approach places them at these points)
            for (int i = 0; i < Lamps.Length && i < 4; i++) moat.SetVector("_Lamp" + i, Lamps[i]);
            return moat;
        }

        // Lit windows (round 12 polish): one atlas (Castle_ExtWindows: leaded lancet, four arrow-slit variants, the dim lit gate tympanum) through
        // the unlit HDR CastleWindow shader. Texture core #d9933a to edge #5a2e0a, dark leading; _Intensity 1.5 keeps the hottest channel under 2.0.
        // Opaque, so it bakes as black emission like every castle material; the colour comes from the shader, not from the lightmap.
        static Material Windows()
        {
            if (windows) return windows;
            var shader = Resources.Load<Shader>("Castle/Shaders/CastleWindow");
            var atlas = Resources.Load<Texture2D>("Castle/Textures/Castle_ExtWindows");
            if (shader == null || !shader.isSupported || atlas == null)
            {
                Debug.LogWarning("[CastleExterior] CastleWindow shader or Castle_ExtWindows atlas unavailable; windows fall back to a dim flat amber");
                windows = new Material(Shader.Find("Standard")) { name = "Castle exterior windows (fallback)", color = new Color(.05f, .025f, .01f) };
                windows.EnableKeyword("_EMISSION"); windows.SetColor("_EmissionColor", new Color(.6f, .35f, .12f));
                return windows;
            }
            windows = new Material(shader) { name = "Castle exterior windows" };
            windows.SetTexture("_MainTex", atlas);
            windows.SetFloat("_Intensity", 1.5f);
            return windows;
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

        // ---- wave FD: the approach (r13 critic item 3: "a fog plane, a grey bridge and low-poly hills; no far bank, no reflection; 2/10") -------
        // The front moat is now a blood lake (z -0.45 .. -16, x +-24; CastleWorld.Exterior lays the water box and the shore banks) crossed by a timber
        // causeway on stone piers from the drawbridge leaf (z -6.45) to a far landing: a paved apron with two lit iron braziers (realtime point lights,
        // no shadows), a ruined gateway, a low broken wall, dead trees and shore rocks, with
        // the backdrop ridges rising behind the shore mist. The lake stops at x +-24 and z -16 so the backdrop ring (tools/castle_exterior.py LAYERS)
        // stands on land everywhere. Render-only dressing except the causeway deck and its rails (CastleWorld.Exterior owns those colliders).
        public const float LakeNear = -.45f, LakeFar = -16f, LakeHalfWidth = 24f, CausewayStart = -6.3f, CausewayHalf = 2.3f, WaterY = -1.6f;
        // glint sources for CastleWater (xyz castle-local, w intensity): the two far-bank braziers, the two lamps at the causeway's castle end.
        // fe-exterior (moat glint test): the castle-end lamps .3 -> .6, so their columns read beside the causeway in battlement_out and S4.
        public static readonly Vector4[] Lamps =
        {
            new Vector4(-3.0f, 1.55f, -23.0f, 1.8f), new Vector4(3.0f, 1.55f, -23.0f, 1.8f),
            new Vector4(-2.3f, 2.05f, -7.4f, .6f), new Vector4(2.3f, 2.05f, -7.4f, .6f),
        };
        static Material plank, iron, ground, ember;

        public static int Approach(Transform root)
        {
            var g = new GameObject("Exterior approach").transform; g.SetParent(root, false);
            int n = 0;
            var stone = StoneDark(); var rockMat = Basalt();
            // causeway deck: the planks are a thin render slab over CastleWorld's collision deck; stone piers in the water every 3.6 m
            float z0 = CausewayStart, z1 = LakeFar, len = z0 - z1;
            n += Block(g, "Causeway deck", new Vector3(0, -.09f, (z0 + z1) * .5f), new Vector3(2 * CausewayHalf - .2f, .18f, len), Plank());
            n += Block(g, "Causeway stringer W", new Vector3(-CausewayHalf + .08f, -.26f, (z0 + z1) * .5f), new Vector3(.22f, .36f, len), stone);
            n += Block(g, "Causeway stringer E", new Vector3(CausewayHalf - .08f, -.26f, (z0 + z1) * .5f), new Vector3(.22f, .36f, len), stone);
            for (float z = z0 - 1.6f; z > z1 + .8f; z -= 3.6f)
            {
                n += Block(g, "Causeway pier", new Vector3(0, -1.6f, z), new Vector3(2 * CausewayHalf + .5f, 2.8f, 1.1f), stone);
                n += Block(g, "Causeway pier cap", new Vector3(0, -.32f, z), new Vector3(2 * CausewayHalf + .7f, .14f, 1.3f), stone);
                foreach (int sx in new[] { -1, 1 })   // cutwaters, turned 45 degrees, on both sides of every pier
                {
                    var cw = Block(g, "Causeway cutwater", new Vector3(sx * (CausewayHalf + .55f), -1.75f, z), new Vector3(.8f, 2.3f, .8f), stone);
                    n += cw;
                    g.GetChild(g.childCount - 1).localRotation = Quaternion.Euler(0, 45, 0);
                }
            }
            // timber railing: posts every 1.8 m, top and mid rails; lamp posts at both ends (flames only; the glints are in CastleWater)
            foreach (int sx in new[] { -1, 1 })
            {
                float x = sx * (CausewayHalf - .1f);
                for (float z = z0 - .1f; z >= z1 - .01f; z -= 1.8f) n += Block(g, "Causeway post", new Vector3(x, .55f, z), new Vector3(.16f, 1.1f, .16f), Plank());
                n += Block(g, "Causeway rail", new Vector3(x, 1.02f, (z0 + z1) * .5f), new Vector3(.1f, .1f, len), Plank());
                n += Block(g, "Causeway mid rail", new Vector3(x, .55f, (z0 + z1) * .5f), new Vector3(.07f, .08f, len), Plank());
                n += LampPost(g, new Vector3(sx * 2.3f, 0, -7.4f));
                n += LampPost(g, new Vector3(sx * 2.3f, 0, LakeFar + .6f));
            }
            // far landing: paving, braziers, ruined gateway, broken wall
            // the braziers and the gateway stand behind the S4 eye (0, 2.5, -22): out of that frame, in gate_out_S6 and battlement_out
            n += Block(g, "Landing paving", new Vector3(0, -.03f, LakeFar - 4.4f), new Vector3(11f, .1f, 8.8f), stone);
            foreach (int sx in new[] { -1, 1 }) n += Brazier(g, new Vector3(sx * 3.0f, 0, -23.0f));
            const float gateZ = -23.9f;
            foreach (int sx in new[] { -1, 1 })
            {
                float x = sx * 4.5f, h = sx < 0 ? 4.6f : 3.6f;
                n += Block(g, "Ruined gate pier", new Vector3(x, h * .5f, gateZ), new Vector3(1.0f, h, 1.0f), stone);
                n += Block(g, "Ruined gate pier cap", new Vector3(x, h + .09f, gateZ), new Vector3(1.2f, .18f, 1.2f), stone);
                n += Block(g, "Ruined gate plinth", new Vector3(x, .22f, gateZ), new Vector3(1.3f, .44f, 1.3f), stone);
                // the low broken wall either side of the gateway, stepping down as it runs out
                for (int k = 0; k < 4; k++)
                {
                    float wx = sx * (5.8f + k * 1.6f), wh = 1.7f - k * .35f + (k % 2) * .2f;
                    n += Block(g, "Ruined wall", new Vector3(wx, wh * .5f, gateZ), new Vector3(1.55f, wh, .7f), stone);
                }
            }
            n += Block(g, "Ruined gate lintel stub", new Vector3(-3.4f, 4.82f, gateZ), new Vector3(2.4f, .45f, .85f), stone);
            g.GetChild(g.childCount - 1).localRotation = Quaternion.Euler(0, 0, -6f);
            // dead trees on the far bank and on the side banks by the castle (the reference's bare trees flank the view)
            var trees = new[] { new Vector4(-8.6f, -18.6f, 30f, 1.7f), new Vector4(10.2f, -19.2f, -60f, 2.0f), new Vector4(-15.5f, -18.0f, 110f, 2.3f), new Vector4(17f, -17.8f, 200f, 1.6f),
                                new Vector4(-23f, 2.2f, 75f, 2.6f), new Vector4(24.5f, 4.0f, -20f, 2.2f), new Vector4(-29f, -1.2f, 160f, 1.9f), new Vector4(30f, .6f, 15f, 2.4f) };
            foreach (var t in trees)
            {
                var placed = CastleArt.Place("DeadTree", g, new Vector3(t.x, 0, t.y), Quaternion.Euler(0, t.z, 0));
                if (placed.placeholder) { Release(placed.gameObject); continue; }
                placed.transform.localScale = Vector3.one * t.w;
                foreach (var r in placed.transform.GetComponentsInChildren<Renderer>()) { StripCollider(r); n++; }
            }
            // shore rocks: along the far bank edge and the side banks' lake edge, half sunk in the water
            var rnd = new System.Random(7717);
            var meshes = new Mesh[6];
            for (int i = 0; i < meshes.Length; i++) meshes[i] = RockMesh(31 + i * 17);
            for (float x = -LakeHalfWidth; x <= LakeHalfWidth; x += 1.7f)          // the far shore
            {
                if (Mathf.Abs(x) < CausewayHalf + .8f) continue;
                float z = LakeFar + (float)(rnd.NextDouble() - .5) * 1.4f, s = .6f + (float)rnd.NextDouble() * 1.4f;
                n += Rock(g, meshes[rnd.Next(meshes.Length)], new Vector3(x, WaterY + .05f + s * .1f, z), s, rnd, rockMat);
                if (rnd.NextDouble() < .45) n += Rock(g, meshes[rnd.Next(meshes.Length)], new Vector3(x + .7f, WaterY + .1f, z - 1.4f), s * 1.5f, rnd, rockMat);
            }
            foreach (int sx in new[] { -1, 1 })
            {
                for (float z = LakeFar + 1f; z <= LakeNear - .5f; z += 1.8f)        // the side shores of the lake
                {
                    float s = .7f + (float)rnd.NextDouble() * 1.5f;
                    n += Rock(g, meshes[rnd.Next(meshes.Length)], new Vector3(sx * (LakeHalfWidth + (float)(rnd.NextDouble() - .3) * 1.2f), WaterY + .1f + s * .12f, z), s, rnd, rockMat);
                }
                for (float x = 20.0f; x <= LakeHalfWidth; x += 1.8f)               // the castle-side shore between the side moat and the lake corner
                {
                    float s = .7f + (float)rnd.NextDouble() * 1.6f;
                    n += Rock(g, meshes[rnd.Next(meshes.Length)], new Vector3(sx * x, WaterY + .1f + s * .12f, LakeNear + (float)(rnd.NextDouble() - .3) * 1.2f), s, rnd, rockMat);
                }
            }
            // larger outcrops on the far bank behind the trees: broken ground instead of a flat plain at the foot of the ridge
            foreach (var o in new[] { new Vector4(-11.5f, -21.4f, 2.6f, 0), new Vector4(13.4f, -21.8f, 3.1f, 0), new Vector4(-20f, -20.4f, 3.6f, 0), new Vector4(21.5f, -20.8f, 2.8f, 0), new Vector4(-29f, -14.8f, 4.2f, 0), new Vector4(30f, -12.4f, 3.8f, 0) })
                n += Rock(g, meshes[rnd.Next(meshes.Length)], new Vector3(o.x, -.3f, o.y), o.z, rnd, rockMat);
            Debug.Log("HC_EXTERIOR_APPROACH renderers=" + n + " lake z[" + LakeFar + "," + LakeNear + "] causeway z[" + LakeFar + "," + CausewayStart + "] braziers=2 lamps=" + Lamps.Length);
            return n;
        }

        static int Block(Transform parent, string name, Vector3 centre, Vector3 size, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; Release(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false); go.transform.localPosition = centre; go.transform.localScale = size;
            if (m) go.GetComponent<Renderer>().sharedMaterial = m;
            return 1;
        }

        static int LampPost(Transform parent, Vector3 foot)
        {
            int n = 0;
            n += Block(parent, "Lamp post", foot + new Vector3(0, 1.05f, 0), new Vector3(.11f, 2.1f, .11f), Iron());
            n += Block(parent, "Lamp post foot", foot + new Vector3(0, .12f, 0), new Vector3(.28f, .24f, .28f), Iron());
            n += Block(parent, "Lamp cage top", foot + new Vector3(0, 2.32f, 0), new Vector3(.3f, .06f, .3f), Iron());
            n += Block(parent, "Lamp cage base", foot + new Vector3(0, 1.94f, 0), new Vector3(.26f, .05f, .26f), Iron());
            foreach (var c in new[] { new Vector2(-.12f, -.12f), new Vector2(.12f, -.12f), new Vector2(-.12f, .12f), new Vector2(.12f, .12f) })
                n += Block(parent, "Lamp cage bar", foot + new Vector3(c.x, 2.13f, c.y), new Vector3(.025f, .38f, .025f), Iron());
            SurrealProps.MakeCandleFlame(parent, foot + new Vector3(0, 2.02f, 0), .2f, false, "Lamp flame", .3f);
            return n + 1;
        }

        static int Brazier(Transform parent, Vector3 foot)
        {
            var b = new GameObject("Far bank brazier").transform; b.SetParent(parent, false); b.localPosition = foot;
            int n = 0;
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f;
                n += Block(b, "Brazier leg", Quaternion.Euler(0, a, 0) * new Vector3(0, .5f, .22f), new Vector3(.07f, 1.08f, .07f), Iron());
                b.GetChild(b.childCount - 1).localRotation = Quaternion.Euler(0, a, 0) * Quaternion.Euler(-13f, 0, 0);
            }
            var bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder); bowl.name = "Brazier bowl"; Release(bowl.GetComponent<Collider>());
            bowl.transform.SetParent(b, false); bowl.transform.localPosition = new Vector3(0, 1.08f, 0); bowl.transform.localScale = new Vector3(.82f, .1f, .82f);
            bowl.GetComponent<Renderer>().sharedMaterial = Iron(); n++;
            var coals = GameObject.CreatePrimitive(PrimitiveType.Cylinder); coals.name = "Brazier coals"; Release(coals.GetComponent<Collider>());
            coals.transform.SetParent(b, false); coals.transform.localPosition = new Vector3(0, 1.19f, 0); coals.transform.localScale = new Vector3(.66f, .03f, .66f);
            coals.GetComponent<Renderer>().sharedMaterial = Ember(); n++;
            SurrealProps.MakeCandleFlame(b, new Vector3(0, 1.2f, 0), .62f, false, "Brazier flame", .6f);
            SurrealProps.MakeCandleFlame(b, new Vector3(.16f, 1.2f, .06f), .42f, false, "Brazier flame", .4f);
            SurrealProps.MakeCandleFlame(b, new Vector3(-.14f, 1.2f, -.08f), .46f, false, "Brazier flame", .4f);
            var lg = new GameObject("Brazier light"); lg.transform.SetParent(b, false); lg.transform.localPosition = new Vector3(0, 1.65f, 0);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, .56f, .26f); l.range = 11f; l.intensity = 3.0f; l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.ForcePixel; l.bounceIntensity = 0f;
            return n + 3;
        }

        static int Rock(Transform parent, Mesh mesh, Vector3 pos, float size, System.Random rnd, Material m)
        {
            var go = new GameObject("Shore rock"); go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler((float)(rnd.NextDouble() - .5) * 14f, (float)rnd.NextDouble() * 360f, (float)(rnd.NextDouble() - .5) * 14f);
            go.transform.localScale = new Vector3(size * (.9f + (float)rnd.NextDouble() * .5f), size * (.55f + (float)rnd.NextDouble() * .45f), size * (.9f + (float)rnd.NextDouble() * .5f));
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
            return 1;
        }

        // A faceted boulder: a 7 x 10 lat-long sphere displaced by seeded low-frequency noise, flattened underneath, unshared vertices (flat facets).
        static Mesh RockMesh(int seed)
        {
            const int lat = 7, lon = 10;
            var rnd = new System.Random(seed);
            var ph = new float[9]; for (int i = 0; i < ph.Length; i++) ph[i] = (float)rnd.NextDouble() * 6.283f;
            Vector3 P(int i, int j)
            {
                float th = Mathf.PI * i / lat, fi = 2 * Mathf.PI * (j % lon) / lon;
                var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(fi), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(fi));
                float r = 1f + .22f * Mathf.Sin(d.x * 2.1f + ph[0]) * Mathf.Sin(d.z * 1.7f + ph[1]) + .16f * Mathf.Sin(d.y * 3.3f + d.x * 1.3f + ph[2])
                          + .08f * Mathf.Sin(d.z * 5.1f + ph[3]) * Mathf.Cos(d.x * 4.4f + ph[4]);
                var p = d * r;
                if (p.y < -.25f) p.y = -.25f + (p.y + .25f) * .25f;   // a flat-ish bed
                return p;
            }
            var verts = new List<Vector3>(); var tris = new List<int>();
            for (int i = 0; i < lat; i++)
                for (int j = 0; j < lon; j++)
                {
                    var a = P(i, j); var b = P(i, j + 1); var c = P(i + 1, j); var d = P(i + 1, j + 1);
                    int k = verts.Count;
                    if (i > 0) { verts.Add(a); verts.Add(b); verts.Add(c); tris.Add(k); tris.Add(k + 1); tris.Add(k + 2); k += 3; }
                    if (i < lat - 1) { verts.Add(b); verts.Add(d); verts.Add(c); tris.Add(k); tris.Add(k + 1); tris.Add(k + 2); }
                }
            var mesh = new Mesh { name = "Shore rock " + seed };
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        static Material Plank()
        {
            if (plank) return plank;
            var basis = CastleArt.Surface("Castle_OakPlank", CastleArt.SurfaceKind.World, new Color(1.25f, 1.15f, 1.05f));
            plank = basis ? new Material(basis) { name = "Castle causeway planks" } : null;
            if (plank && plank.HasProperty("_Tile")) plank.SetFloat("_Tile", .83f);   // 0.3 m boards like the drawbridge leaf
            return plank;
        }
        static Material Iron() { if (!iron) iron = CastleArt.Surface("Castle_Iron", CastleArt.SurfaceKind.World, Color.white); return iron; }
        static Material Ember()
        {
            if (ember) return ember;
            ember = new Material(Shader.Find("Standard")) { name = "Castle brazier coals", color = new Color(.25f, .07f, .03f) };
            ember.EnableKeyword("_EMISSION"); ember.SetColor("_EmissionColor", new Color(2.2f, .6f, .14f));
            return ember;
        }
        // The banks: dark mossy stone in world space (the Castle_FlagStone "earth" read as a paved plain), the same ambient-fill boost as the
        // exterior stone so the moonlit ground is not black. CastleWorld.Exterior assigns it to the bank boxes.
        public static Material Ground()
        {
            if (ground) return ground;
            ground = ExtStone("Castle_StoneMossy", new Color(.62f, .66f, .60f), "Castle exterior bank ground");
            if (ground && ground.HasProperty("_Tile")) ground.SetFloat("_Tile", .22f);
            return ground;
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
            var high = FogMaterial(shader, noise, "Castle exterior fog high", .20f, .11f, new Vector4(-.005f, .006f, 0, 0));
            var lakeVeil = FogMaterial(shader, noise, "Castle exterior lake veil", .10f, .09f, new Vector4(.004f, -.003f, 0, 0));
            lakeVeil.SetColor("_Color", new Color(.20f, .17f, .21f, 1f));   // a dark, faintly warm veil: it must not grey the blood
            var quad = Quad();
            // centre x, centre z, size x, size z. Wave FD (r13 critic: "the moat is a grey fog plane"): the side and back moats keep two thin
            // layers instead of six, and the front blood lake gets ONE low, sparse layer, so the red water and its reflections read through it.
            var bands = new[] { new Vector4(-17.2f, 14f, 9f, 36f), new Vector4(17.2f, 14f, 9f, 36f), new Vector4(0, 31.4f, 46f, 8f) };
            int n = 0;
            foreach (var b in bands)
                for (int i = 0; i < 2; i++)
                    n += FogQuad(group, quad, i == 0 ? low : high, b, WaterY + (i == 0 ? .18f : .75f), n);
            n += FogQuad(group, quad, lakeVeil, new Vector4(0, -8.2f, 50f, 17f), WaterY + .12f, n);
            // far-shore and flank mist (round 12 polish, wave FD): the mist now sits on the far shore behind the causeway landing and on the
            // flanks, not over the lake, so the shore line and the brazier light read as a lit bank with mist rising behind it.
            var far = new[] { new Vector4(0, -26.5f, 96f, 7f), new Vector4(-44f, 14f, 40f, 70f), new Vector4(44f, 14f, 40f, 70f), new Vector4(0, 62f, 110f, 56f) };
            foreach (var b in far)
            {
                n += FogQuad(group, quad, low, b, .35f, n);
                n += FogQuad(group, quad, high, b, 1.1f, n);
            }
            return n;
        }

        static int FogQuad(Transform group, Mesh quad, Material m, Vector4 b, float y, int n)
        {
            var go = new GameObject("Fog layer " + n);
            go.transform.SetParent(group, false);
            go.transform.localPosition = new Vector3(b.x, y, b.y);        // y relative to the castle root; the moat water is at WaterY
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(b.z, b.w, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return 1;
        }

        static Material FogMaterial(Shader shader, Texture2D noise, string name, float alpha, float tile, Vector4 scroll)
        {
            var m = new Material(shader) { name = name };
            m.SetTexture("_MainTex", noise);
            m.SetColor("_Color", new Color(.27f, .32f, .43f, 1f));   // wave FD: was (.34,.42,.58), a bright grey-blue sheet over everything
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
