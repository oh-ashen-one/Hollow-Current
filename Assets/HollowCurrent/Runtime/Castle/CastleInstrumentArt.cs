using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HollowCurrent
{
    // fc-props (wave FC, r12 critic: "the instruments are modern props under gothic names"): period-looking held instruments for the CASTLE map only.
    // Bellwether keeps the Tripo field-kit meshes (FieldKitArt, NativeVerification requires them); the castle builds these instead:
    //   tool 1  Hollow Compass         brass-and-glass compass case, parchment dial, a red-tipped needle that trembles with the reading
    //                                  (replaces the induction reader's LCD; the five ember pips on the dial are the 0..5 reading)
    //   tool 2  Grave-ash lantern      brass cage lantern around a violet glass chimney and a glowing core (replaces the UV lamp)
    //   tool 3  Vellum ear-trumpet     flared brass horn under a vellum wrap with leather bindings (replaces the EVP recorder)
    //   radio   Resonance box          fc-art2: brass-banded walnut box with a grille, a tuning pip and a brass horn (replaces the field radio, R key)
    // Everything is procedural: meshes are generated here (units: metres, +z away from the player, +y up, +x right, origin = the mount point
    // of Investigator.heldTool) and the three textures (dial, vellum) are painted at runtime from a hash, so there are no new binary
    // assets. Hand transform, interaction and reading behaviour are Investigator's and are unchanged. Vertex budget: MaxVertices per instrument.
    public static class CastleInstrumentArt
    {
        public const int MaxVertices = 3000;
        public static readonly string[] Names = { "Hollow Compass", "Grave-ash Lantern", "Vellum Ear-Trumpet" };

        // Handle on the parts of the compass that move: the needle and the five reading pips.
        public sealed class CompassParts
        {
            public Transform Needle;
            public Renderer Pips;
            public Material PipDim, PipLit;
            readonly Material[] slots = new Material[5];
            int level = -1;
            public void SetLevel(int n)
            {
                n = Mathf.Clamp(n, 0, 5);
                if (n == level || !Pips) return;
                level = n;
                for (int i = 0; i < 5; i++) slots[i] = i < n ? PipLit : PipDim;
                Pips.sharedMaterials = slots;
            }
        }

        // index 0 compass, 1 lantern, 2 trumpet (the order of Investigator's instrumentModels). parts is only set for the compass.
        public static Transform Create(int index, Transform parent, out CompassParts parts)
        {
            parts = null;
            Transform root;
            switch (index)
            {
                case 0: root = BuildCompass(parent, out parts); break;
                case 1: root = BuildLantern(parent); break;
                default: root = BuildTrumpet(parent); break;
            }
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                r.gameObject.layer = 30; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            }
            root.gameObject.layer = 30;
            return root;
        }

        public static int Vertices(Transform item) => FieldKitArt.VertexCount(item);

        // ------------------------------------------------------------------------------------------------------------------------------
        // Hollow Compass
        // ------------------------------------------------------------------------------------------------------------------------------
        public const float DialLevel = .68f;   // fc-art2: unlit dial brightness (aged parchment x this; fc-light2 had .46, which still rendered pale grey)
        static Transform BuildCompass(Transform parent, out CompassParts parts)
        {
            var root = NewRoot(parent, "Field kit art / Hollow Compass (period)");
            var visual = new GameObject("Compass visual").transform; visual.SetParent(root, false); visual.localPosition = new Vector3(0, .012f, 0);
            var brass = Brass("Compass brass");
            // fc-light2 (director: "the compass glass is a mirror ... hides the dial and needle"): the held items sit on the equipment layer, which
            // the lantern does not light (Investigator.Torch excludes layer 30), so the dial is drawn unlit at a fixed level.
            // fc-art2 (director: "the dial reads pale grey, not parchment"): fc-light2's captures (fc3/r3 compass_idle) still show a flat neutral
            // grey disc (about 106,105,104), a grey-blue needle and salmon (veiled) ember pips, so a bluish-white veil lies over the whole face and
            // the painted parchment never reaches the screen. The only layer between the camera and the dial was the glass dome (a second CastleSprite
            // submesh with a glare texture), the prime suspect, so the dome is gone (not proven: no engine run was possible in this piece, see the
            // fc-art2 handoff and the HC_COMPASS_DIAL log): the glass is now a thin opaque rim band just inside the case lip (same vertex count,
            // no blending over the face). The dial is aged ivory with foxing, ink and numerals are dark umber, and the needle is unlit dark
            // oxblood with an umber tail so it reads whatever the lighting.
            var dial = Sprite("Compass dial", DialTexture(), new Color(DialLevel, DialLevel * .97f, DialLevel * .93f, 1));
            var rim = Glow("Compass glass rim", new Color(.105f, .115f, .125f));

            // case, bezel and back: one lathe about the z axis (r, z); front (toward the player) is -z. Counter-clockwise outline, outside on the right.
            var body = CompassBodyGeometry();
            Part(visual, "Compass case", body.ToMesh("Compass case"), brass, dial, rim);
            Debug.Log("HC_COMPASS_DIAL dial_shader=" + dial.shader.name + " rim_shader=" + rim.shader.name + " dial_level=" + DialLevel.ToString("F2") + " glass_dome=False (fc-art2: translucent veil removed)");


            // the five reading pips on the dial's lower arc: dark when unlit, glowing ember when the reading reaches them
            var pipMesh = CompassPipGeometry();
            parts = new CompassParts { PipDim = Lit("Compass pip dark", new Color(.13f, .07f, .05f), 0, .3f), PipLit = Glow("Compass pip ember", new Color(1.35f, .48f, .09f)) };
            var pipGo = Part(visual, "Compass reading pips", pipMesh.ToMesh("Compass pips"), parts.PipDim, parts.PipDim, parts.PipDim, parts.PipDim, parts.PipDim);
            parts.Pips = pipGo.GetComponent<MeshRenderer>();
            parts.SetLevel(0);

            // the needle: a flat kite over the dial, oxblood toward +x (the way UpdateNeedle aims it), umber tail; rotated about z by Investigator
            var needleMesh = CompassNeedleGeometry();
            // fc-art2: dark umber needle, unlit (CastleEmissive outputs its colour as is, linear): oxblood tip toward the reading, umber tail
            var needle = Part(visual, "Hollow compass needle", needleMesh.ToMesh("Compass needle"), Glow("Compass needle oxblood", new Color(.10f, .016f, .010f)), Glow("Compass needle umber", new Color(.034f, .018f, .010f)));
            needle.transform.localPosition = new Vector3(0, 0, -.0085f);
            parts.Needle = needle.transform;
            return root;
        }

        // ------------------------------------------------------------------------------------------------------------------------------
        // Grave-ash lantern
        // ------------------------------------------------------------------------------------------------------------------------------
        // fc-light2: the castle's light source in the left hand (it replaces the modern flashlight mesh there; the Investigator's torch light is
        // unchanged): the same brass cage with warm amber glass and a candle-coloured core instead of the grave-ash violet.
        public static Transform CreateHandLantern(Transform parent)
        {
            var root = BuildLantern(parent, true);
            root.name = "Field kit art / Hand lantern (period)";
            foreach (var r in root.GetComponentsInChildren<Renderer>()) { r.gameObject.layer = 30; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }
            root.gameObject.layer = 30;
            return root;
        }
        static Transform BuildLantern(Transform parent, bool warm = false)
        {
            var root = NewRoot(parent, "Field kit art / Grave-ash Lantern (period)");
            var visual = new GameObject("Lantern visual").transform; visual.SetParent(root, false); visual.localPosition = new Vector3(.030f, .008f, 0);
            var brass = Brass("Lantern brass");
            var iron = Lit("Lantern iron", new Color(.11f, .095f, .085f), .3f, .4f);

            var metal = LanternFrameGeometry();
            Part(visual, "Lantern frame", metal.ToMesh("Lantern frame"), brass, iron);

            var light = LanternLightGeometry();
            if (warm) Part(visual, "Lantern glass and core", light.ToMesh("Lantern glass"), Sprite("Lantern amber glass", null, new Color(1f, .60f, .24f, .22f)), Glow("Lantern flame core", new Color(1.15f, .60f, .20f)));
            else Part(visual, "Lantern glass and core", light.ToMesh("Lantern glass"), Sprite("Lantern violet glass", null, new Color(.50f, .20f, 1f, .46f)), Glow("Lantern core", new Color(.80f, .50f, 1.25f)));
            return root;
        }

        // ------------------------------------------------------------------------------------------------------------------------------
        // Vellum ear-trumpet
        // ------------------------------------------------------------------------------------------------------------------------------
        static Transform BuildTrumpet(Transform parent)
        {
            var root = NewRoot(parent, "Field kit art / Vellum Ear-Trumpet (period)");
            var visual = new GameObject("Trumpet visual").transform; visual.SetParent(root, false);
            var m = TrumpetGeometry();
            Part(visual, "Ear-trumpet horn", m.ToMesh("Ear-trumpet"), Brass("Trumpet brass"), Lit("Trumpet vellum", Color.white, 0, .12f, VellumTexture()),
                Lit("Trumpet leather", new Color(.20f, .11f, .07f), 0, .25f), Lit("Trumpet bell interior", new Color(.035f, .02f, .015f), 0, .1f));
            return root;
        }


        // ------------------------------------------------------------------------------------------------------------------------------
        // Resonance box (the castle's field radio)
        // ------------------------------------------------------------------------------------------------------------------------------
        // fc-art2: pressing R in the castle still raises "the radio" (Game.RadioHint: hints, or static during interference or a hunt; the dispatch voice is
        // gone, the call is text plus the "radio" cue), and it was still the modern Tripo radio mesh. In the castle it is now a small brass-banded walnut
        // "resonance box" with a grille, a tuning pip and a brass horn, in the same hand as the compass, lantern and trumpet. Bellwether keeps FieldKitArt's
        // FieldRadio (NativeVerification requires its name and vertex count there); Investigator picks this builder only when Game.Map == Castle.
        public static Transform CreateRadio(Transform parent)
        {
            var root = BuildRadio(parent);
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                r.gameObject.layer = 30; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            }
            root.gameObject.layer = 30;
            return root;
        }
        static Transform BuildRadio(Transform parent)
        {
            var root = NewRoot(parent, "Field kit art / Resonance Box (period)");
            var visual = new GameObject("Resonance box visual").transform; visual.SetParent(root, false);
            var m = RadioGeometry();
            Part(visual, "Resonance box", m.ToMesh("Resonance box"), Lit("Radio walnut", Color.white, 0, .30f, WalnutTexture()), Brass("Radio brass"),
                Lit("Radio dark panel", new Color(.07f, .045f, .03f), 0, .2f), Glow("Radio tuning pip", new Color(1.30f, .52f, .12f)));
            return root;
        }
        internal static Mb RadioGeometry()
        {
            var fwd = Vector3.forward;
            var m = new Mb(4);   // 0 walnut, 1 brass, 2 dark panel / grille, 3 glowing tuning pip
            // body: x -.065..+.055, y -.041..+.031, z -.031..+.031 (the glove grips its lower right corner, like the compass case)
            m.Box(0, new Vector3(-.005f, -.005f, 0), new Vector3(.120f, .072f, .062f));
            m.Box(2, new Vector3(-.005f, .0325f, 0), new Vector3(.104f, .004f, .046f));                 // inset lid panel
            foreach (float x in new[] { -.047f, .040f })                                                 // brass straps round the box
                m.Box(1, new Vector3(x, -.005f, 0), new Vector3(.007f, .0745f, .0645f));
            // front (toward the player, -z): a brass-rimmed round grille and a tuning pip
            const float frontZ = -.031f;
            var grille = new Vector3(-.012f, -.005f, frontZ);
            m.Lathe(2, grille, fwd, new (float, float)[] { (0, -.0006f), (.0215f, -.0006f) }, 20);
            m.Lathe(1, grille, fwd, Circle(.0235f, -.0010f, .0032f, 6), 18);
            for (int i = -2; i <= 2; i++)                                                                // five brass slats across the grille
                m.Box(1, new Vector3(-.012f, -.005f + i * .0072f, frontZ - .0010f), new Vector3(.040f - Mathf.Abs(i) * .0065f, .0016f, .0016f));
            m.Lathe(3, new Vector3(.034f, -.020f, frontZ), fwd, new (float, float)[] { (0, -.0012f), (.0042f, -.0012f) }, 10);
            m.Lathe(1, new Vector3(.034f, -.020f, frontZ), fwd, Circle(.0048f, -.0008f, .0016f, 6), 10);
            // a brass knob on the lid, right of centre
            m.Lathe(1, new Vector3(.028f, .0345f, .004f), Vector3.up, new (float, float)[] { (0, 0), (.0085f, 0), (.0085f, .006f), (.0055f, .0105f), (0, .0105f) }, 14);
            // the horn: from the lid's left end, rising and bending away from the player; flared bell with a dark mouth
            Vector3 p0 = new Vector3(-.040f, .032f, -.004f), p1 = new Vector3(-.046f, .052f, .030f), p2 = new Vector3(-.052f, .060f, .098f);
            Func<float, Vector3> path = t => (1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * p1 + t * t * p2;
            Func<float, float> radius = t => .0070f + .0075f * t + .0300f * t * t * t * t * t;
            m.Sweep(1, path, radius, 0, .94f, 12, 20, true, false, 1, 1);
            m.Sweep(1, path, t => radius(t) + .0026f, .94f, 1f, 2, 20, true, true, 2, 2);                // flared lip, the end cap is the dark mouth
            return m;
        }
        // a dark walnut with long fibre streaks along x
        static Texture2D WalnutTexture()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "HC period walnut (runtime)", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float grain = Noise(x * .035f, y * .55f), fine = Noise(x * .12f + 9, y * 1.7f);
                    float v = .62f + .55f * grain * grain + .12f * fine + .05f * (Hash(x, y) - .5f);
                    px[y * n + x] = new Color(.36f * v, .20f * v, .10f * v, 1);
                }
            tex.SetPixels(px); tex.Apply(true, true); return tex;
        }

        // ---- geometry recipes (no engine objects, so tools can dump and check them) ----
        internal static Mb CompassBodyGeometry()
        {
            var fwd = Vector3.forward;
            var body = new Mb(3);
            body.Lathe(0, Vector3.zero, fwd, new (float, float)[]
            {
                (0, -.004f), (.060f, -.004f), (.060f, -.014f), (.0645f, -.014f), (.0645f, -.0175f), (.0695f, -.0175f), (.0725f, -.0145f),
                (.0752f, -.010f), (.0752f, .012f), (.0725f, .0195f), (.052f, .0245f), (.020f, .0265f), (0, .0268f),
            }, 28);
            body.Lathe(1, Vector3.zero, fwd, new (float, float)[] { (0, -.0052f), (.060f, -.0052f) }, 28, true, .060f);   // the dial disc, planar uv
            body.Lathe(2, Vector3.zero, fwd, new (float, float)[]                                                         // fc-art2: the glass is a thin rim band inside the lip (was a dome over the whole face; same 5 profile points)
            {
                (.0595f, -.0152f), (.0608f, -.0158f), (.0620f, -.0160f), (.0632f, -.0158f), (.0645f, -.0153f),
            }, 28);
            // hub over the needle pivot
            body.Lathe(0, Vector3.zero, fwd, new (float, float)[] { (0, -.0133f), (.0020f, -.0132f), (.0055f, -.0120f), (.0075f, -.0098f), (.0075f, -.0085f), (0, -.0085f) }, 12);
            // suspension collar and ring at the top (the ring stands in the XY plane)
            body.Lathe(0, Vector3.zero, Vector3.up, new (float, float)[] { (0, .070f), (.0075f, .070f), (.0085f, .0745f), (.0060f, .0800f), (0, .0800f) }, 12);
            body.Lathe(0, new Vector3(0, .0885f, 0), fwd, Circle(.0125f, 0, .0030f, 6), 18);
            return body;
        }
        internal static Mb CompassPipGeometry()
        {
            var fwd = Vector3.forward;
            var pipMesh = new Mb(5);
            for (int i = 0; i < 5; i++)
            {
                float a = (-135f + 22.5f * i) * Mathf.Deg2Rad;
                pipMesh.Lathe(i, new Vector3(Mathf.Cos(a) * .043f, Mathf.Sin(a) * .043f, 0), fwd, new (float, float)[] { (0, -.0068f), (.0044f, -.0068f) }, 12);
            }
            return pipMesh;
        }
        internal static Mb CompassNeedleGeometry()
        {
            var needleMesh = new Mb(2);
            var apex = new Vector3(-.002f, 0, -.0030f); var tip = new Vector3(.052f, 0, 0); var tail = new Vector3(-.040f, 0, 0);
            var sideA = new Vector3(-.006f, .0046f, 0); var sideB = new Vector3(-.006f, -.0046f, 0);
            var toViewer = new Vector3(0, 0, -1);
            needleMesh.TriFacing(0, apex, tip, sideA, toViewer); needleMesh.TriFacing(0, apex, sideB, tip, toViewer);
            needleMesh.TriFacing(1, apex, sideA, tail, toViewer); needleMesh.TriFacing(1, apex, tail, sideB, toViewer);
            return needleMesh;
        }
        internal static Mb LanternFrameGeometry()
        {
            var up = Vector3.up;
            var metal = new Mb(2);
            metal.Lathe(0, Vector3.zero, up, new (float, float)[]                                  // foot and stand, bottom first
            {
                (0, -.095f), (.032f, -.095f), (.0355f, -.0925f), (.0355f, -.0885f), (.030f, -.085f), (.0235f, -.0785f), (.021f, -.075f), (0, -.075f),
            }, 20);
            metal.Lathe(0, Vector3.zero, up, new (float, float)[]                                  // cap, ending in a finial
            {
                (0, .050f), (.0365f, .050f), (.0365f, .0555f), (.0315f, .0575f), (.026f, .066f), (.015f, .078f), (.0085f, .0845f), (.0085f, .088f), (0, .088f),
            }, 20);
            metal.Lathe(0, Vector3.zero, up, Circle(.0305f, -.012f, .0025f, 6), 16);               // the waist band round the cage
            metal.Lathe(0, new Vector3(0, .0985f, 0), Vector3.forward, Circle(.0105f, 0, .0025f, 8), 16);   // the bail ring above the cap
            for (int i = 0; i < 6; i++)                                                            // cage bars
            {
                float a = i * 60f * Mathf.Deg2Rad; var xz = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .031f;
                metal.Sweep(1, t => xz + new Vector3(0, Mathf.Lerp(-.082f, .052f, t), 0), t => .0014f, 0, 1, 2, 5, true, true, 1, 1);
            }
            return metal;
        }
        internal static Mb LanternLightGeometry()
        {
            var up = Vector3.up;
            var light = new Mb(2);
            light.Lathe(0, Vector3.zero, up, new (float, float)[] { (.0235f, -.0755f), (.0265f, -.060f), (.0275f, -.02f), (.0275f, .03f), (.0245f, .052f) }, 20);   // chimney wall
            light.Lathe(1, Vector3.zero, up, new (float, float)[] { (0, -.07f), (.0075f, -.07f), (.0095f, -.05f), (.0095f, .02f), (.0055f, .04f), (0, .046f) }, 12);  // glowing core
            return light;
        }
        internal static Mb TrumpetGeometry()
        {
            // the horn: earpiece near the player at the lower right (where the glove grips), bell far and high, a gentle bow between
            Vector3 p0 = new Vector3(.050f, -.070f, -.090f), p1 = new Vector3(.030f, -.040f, 0), p2 = new Vector3(-.025f, .040f, .095f);
            Func<float, Vector3> path = t => (1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * p1 + t * t * p2;
            Func<float, float> radius = t => .0075f + .0105f * t + .0345f * t * t * t * t * t;
            var m = new Mb(4);   // 0 brass, 1 vellum, 2 leather, 3 dark bell interior
            const int segs = 22;
            const float wrap0 = .20f, wrap1 = .78f;
            m.Sweep(0, path, radius, 0, wrap0, 4, segs, true, true, 0, 0);                                                   // brass earpiece
            m.Sweep(1, path, t => radius(t) + .0014f, wrap0, wrap1, 12, segs, true, true, 1, 1, 3f);                          // the vellum wrap
            m.Sweep(0, path, radius, wrap1, .97f, 6, segs, true, true, 0, 0);                                               // brass bell
            m.Sweep(0, path, t => radius(t) + .0030f, .97f, 1f, 2, segs, true, true, 0, 3);                                 // flared lip; the cap is the dark inside
            foreach (float tb in new[] { wrap0, .49f, wrap1 })                                                            // leather bindings
                m.Sweep(2, path, t => radius(t) + .0028f, tb - .014f, tb + .014f, 2, segs, true, true, 2, 2);
            return m;
        }

        // ------------------------------------------------------------------------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------------------------------------------------------------------------
        static Transform NewRoot(Transform parent, string name)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = Vector3.zero; t.localRotation = Quaternion.identity; return t;
        }
        static GameObject Part(Transform parent, string name, Mesh mesh, params Material[] mats)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterials = mats;
            return go;
        }
        // a closed circle outline (counter-clockwise in (r, h), starting at the bottom) for a torus-like lathe
        static (float, float)[] Circle(float cr, float ch, float rho, int steps)
        {
            var pts = new (float, float)[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float a = (-90f + 360f * i / steps) * Mathf.Deg2Rad;
                pts[i] = (cr + rho * Mathf.Cos(a), ch + rho * Mathf.Sin(a));
            }
            return pts;
        }
        static Shader Std => Shader.Find("Standard");
        static Material Lit(string name, Color color, float metallic, float smooth, Texture tex = null)
        {
            var sh = Std; if (!sh) sh = Shader.Find("Sprites/Default");
            var m = new Material(sh) { name = "HC period instrument / " + name, color = color };
            if (tex) m.mainTexture = tex;
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            return m;
        }
        static Material Brass(string name) => Lit(name, new Color(.66f, .48f, .20f), .38f, .55f);
        // Unlit HDR colour through the castle's always-included emissive shader (the Standard _EMISSION keyword is not safe in a player build, see CastleArt.FlameShaderOk).
        static Material Glow(string name, Color hdr)
        {
            var sh = Resources.Load<Shader>("Castle/Shaders/CastleEmissive"); if (!sh) sh = Shader.Find("HollowCurrent/CastleEmissive");
            if (!sh) return Lit(name, hdr, 0, .1f);
            return new Material(sh) { name = "HC period instrument / " + name, color = hdr };
        }
        static Material Sprite(string name, Texture tex, Color tint)
        {
            var m = SurrealProps.Unlit("HC period instrument / " + name, tex, tint); return m;
        }

        // ---- procedural textures (painted at runtime, nothing on disk) ----
        static float Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263; h = (h ^ (h >> 13)) * 1274126177; h ^= h >> 16;
                return (h & 0xFFFF) / 65535f;
            }
        }
        static float Noise(float x, float y)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y); float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            return Mathf.Lerp(Mathf.Lerp(Hash(xi, yi), Hash(xi + 1, yi), fx), Mathf.Lerp(Hash(xi, yi + 1), Hash(xi + 1, yi + 1), fx), fy);
        }
        // fc-art2: aged ivory parchment (warm, edge-stained, with foxing) with an umber inked ring, 24 ticks (long at the quarters), the numerals
        // 3 / 6 / 9 and an oxblood mark at 12 o'clock (the needle's "ahead", at the top of the face). Colours are the texture's; DialLevel only dims the unlit material.
        static readonly int[][] DigitRows =
        {
            new[] { 7, 1, 7, 1, 7 },   // 3  (111 / 001 / 111 / 001 / 111)
            new[] { 7, 4, 7, 5, 7 },   // 6  (111 / 100 / 111 / 101 / 111)
            new[] { 7, 5, 7, 1, 7 },   // 9  (111 / 101 / 111 / 001 / 111)
        };
        // 1 where the numeral `digit` (index into DigitRows) is inked at texture-space (fx, fy) in [-1,1], for a glyph box centred on (cx, cy)
        static float NumeralInk(int digit, float fx, float fy, float cx, float cy)
        {
            const float boxW = .115f, boxH = .185f;
            float u = (fx - cx) / boxW + .5f, v = (fy - cy) / boxH + .5f;
            if (u < 0 || u >= 1 || v < 0 || v >= 1) return 0;
            int col = Mathf.Min(2, (int)(u * 3)), row = Mathf.Min(4, (int)((1 - v) * 5));
            return ((DigitRows[digit][row] >> (2 - col)) & 1) == 1 ? 1 : 0;
        }
        static Texture2D DialTexture()
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "HC period dial (runtime)", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var ink = new Color(.16f, .095f, .055f); var red = new Color(.40f, .065f, .045f);   // dark umber ink, oxblood mark
            var foxTint = new Color(.70f, .54f, .36f);                                           // foxing: rust-brown blotches over the ivory
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float tx = (x + .5f) / n * 2 - 1, ty = (y + .5f) / n * 2 - 1, r = Mathf.Sqrt(tx * tx + ty * ty);
                    // The planar dial uv (Mb.Lathe, axis +z) runs u toward mesh -x and v toward mesh -y, i.e. the texture lands turned 180 degrees on the
                    // face (fc3/r3 showed the 12 o'clock mark at the bottom). All features are laid out in the face's own frame (mx right, my up).
                    float fx = -tx, fy = -ty;
                    float stain = .91f + .10f * Noise(x * .09f, y * .09f) + .04f * (Hash(x, y) - .5f);
                    var c = new Color(.88f, .79f, .57f) * stain * (1 - .34f * Mathf.SmoothStep(.55f, 1.0f, r));   // warm ivory, darker toward the edge
                    float blot = Mathf.SmoothStep(.78f, .94f, Noise(x * .27f + 17f, y * .27f + 3f));              // foxing patches
                    float speck = Hash(x / 2 + 41, y / 2 + 7) > .988f ? 1 : 0;                                      // pinprick foxing
                    c = Color.Lerp(c, c * foxTint, Mathf.Clamp01(blot * .6f + speck * .6f));
                    float ang = Mathf.Atan2(fy, fx) * Mathf.Rad2Deg;
                    float near15 = Mathf.Abs(Mathf.DeltaAngle(ang, Mathf.Round(ang / 15f) * 15f));
                    float q = Mathf.Round(ang / 15f) * 15f;
                    bool quarter = Mathf.Abs(Mathf.Repeat(q, 90f)) < .01f || Mathf.Abs(Mathf.Repeat(q, 90f) - 90f) < .01f;
                    bool eighth = !quarter && Mathf.Abs(Mathf.Repeat(q, 45f)) < .01f;
                    float arc = near15 * Mathf.Deg2Rad * r;   // distance across a tick
                    bool tickHit = r > (quarter || eighth ? .78f : .82f) && r < .90f && arc < (quarter ? .034f : eighth ? .022f : .012f);   // ticks stop short of the numerals
                    bool ring = Mathf.Abs(r - .94f) < .022f || Mathf.Abs(r - .57f) < .010f;
                    bool mark = r > .46f && r < .60f && Mathf.Abs(Mathf.DeltaAngle(ang, 90)) < 14f * (1 - (r - .46f) / .14f) + 1f;
                    bool numeral = NumeralInk(0, fx, fy, .68f, 0) > 0 || NumeralInk(1, fx, fy, 0, -.68f) > 0 || NumeralInk(2, fx, fy, -.68f, 0) > 0;
                    if (mark) c = red; else if (tickHit || ring || numeral) c = Color.Lerp(c, ink, .92f);
                    c.a = 1; px[y * n + x] = c;
                }
            tex.SetPixels(px); tex.Apply(true, true); return tex;
        }
        // a pale vellum with fibre streaks along the horn and a few water stains
        static Texture2D VellumTexture()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "HC period vellum (runtime)", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float fibre = Noise(x * .6f, y * .08f) * .10f, stain = Noise(x * .11f + 5, y * .11f + 9) * .20f;
                    float v = .80f + fibre - stain * .45f + .04f * (Hash(x, y) - .5f);
                    px[y * n + x] = new Color(.90f * v, .80f * v, .60f * v, 1);
                }
            tex.SetPixels(px); tex.Apply(true, true); return tex;
        }
        // ------------------------------------------------------------------------------------------------------------------------------
        // a tiny mesh builder: lathes, sweeps and flat triangles, several submeshes. Triangle winding is derived from the vertex normals, so a
        // primitive is never inside out as long as its normals point outward.
        // ------------------------------------------------------------------------------------------------------------------------------
        internal sealed class Mb
        {
            readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
            readonly List<Vector2> uv = new List<Vector2>();
            readonly List<int>[] tris;
            public Mb(int subs) { tris = new List<int>[subs]; for (int i = 0; i < subs; i++) tris[i] = new List<int>(); }

            int Vert(Vector3 p, Vector3 nrm, Vector2 u) { v.Add(p); n.Add(nrm); uv.Add(u); return v.Count - 1; }
            void Tri(int sub, int a, int b, int c)
            {
                var cr = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
                if (cr.sqrMagnitude < 1e-14f) return;                       // degenerate (a lathe ring on the axis)
                if (Vector3.Dot(cr, n[a] + n[b] + n[c]) < 0) { int t = b; b = c; c = t; }
                tris[sub].Add(a); tris[sub].Add(b); tris[sub].Add(c);
            }

            // flat quad a-b-c-d (any corner order around the quad) facing "toward": own 4 vertices, uv (0,0) (1,0) (1,1) (0,1)
            public void Quad(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 toward)
            {
                var nrm = Vector3.Cross(b - a, c - a).normalized;
                if (Vector3.Dot(nrm, toward) < 0) nrm = -nrm;
                int ia = Vert(a, nrm, new Vector2(0, 0)), ib = Vert(b, nrm, new Vector2(1, 0)), ic = Vert(c, nrm, new Vector2(1, 1)), id = Vert(d, nrm, new Vector2(0, 1));
                Tri(sub, ia, ib, ic); Tri(sub, ia, ic, id);
            }
            // axis-aligned hard-edged box (24 vertices)
            public void Box(int sub, Vector3 centre, Vector3 size)
            {
                var h = size * .5f; float x = h.x, y = h.y, z = h.z;
                Vector3 P(float sx, float sy, float sz) => centre + new Vector3(sx * x, sy * y, sz * z);
                Quad(sub, P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1), Vector3.right);
                Quad(sub, P(-1, -1, -1), P(-1, 1, -1), P(-1, 1, 1), P(-1, -1, 1), Vector3.left);
                Quad(sub, P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1), P(-1, 1, 1), Vector3.up);
                Quad(sub, P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1), Vector3.down);
                Quad(sub, P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), Vector3.forward);
                Quad(sub, P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1), Vector3.back);
            }

            // flat triangle facing "toward" (a hard-edged facet)
            public void TriFacing(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 toward)
            {
                var nrm = Vector3.Cross(b - a, c - a).normalized;
                if (Vector3.Dot(nrm, toward) < 0) { var t = b; b = c; c = t; nrm = -nrm; }
                int ia = Vert(a, nrm, new Vector2(0, 0)), ib = Vert(b, nrm, new Vector2(1, 0)), ic = Vert(c, nrm, new Vector2(0, 1));
                tris[sub].Add(ia); tris[sub].Add(ib); tris[sub].Add(ic);
            }

            // Surface of revolution. prof = (radius, height along axis) points, counter-clockwise around the solid (radius to the right, height up),
            // so the outside is on the right of the walking direction. Corners sharper than ~55 degrees keep a hard edge (the vertex is doubled).
            public void Lathe(int sub, Vector3 origin, Vector3 axis, (float r, float h)[] prof, int segs, bool planarUv = false, float uvRadius = 1)
            {
                axis.Normalize();
                var u = Vector3.Cross(axis, Mathf.Abs(axis.y) > .9f ? Vector3.right : Vector3.up).normalized; var w = Vector3.Cross(axis, u);
                int m = prof.Length - 1; if (m < 1) return;
                var sn = new Vector2[m];   // (radial, axial) outward normal per profile segment
                for (int k = 0; k < m; k++)
                {
                    var t = new Vector2(prof[k + 1].r - prof[k].r, prof[k + 1].h - prof[k].h).normalized;
                    sn[k] = new Vector2(t.y, -t.x);
                }
                var startRing = new int[m]; var endRing = new int[m];
                float total = 0; var cum = new float[prof.Length];
                for (int k = 0; k < m; k++) { total += Vector2.Distance(new Vector2(prof[k].r, prof[k].h), new Vector2(prof[k + 1].r, prof[k + 1].h)); cum[k + 1] = total; }
                for (int i = 0; i <= m; i++)
                {
                    if (i == 0) { startRing[0] = AddRing(sn[0], i); }
                    else if (i == m) { endRing[m - 1] = AddRing(sn[m - 1], i); }
                    else if (Vector2.Dot(sn[i - 1], sn[i]) > .57f)   // cos 55 degrees: smooth
                    {
                        int r = AddRing((sn[i - 1] + sn[i]).normalized, i); endRing[i - 1] = r; startRing[i] = r;
                    }
                    else { endRing[i - 1] = AddRing(sn[i - 1], i); startRing[i] = AddRing(sn[i], i); }
                }
                for (int k = 0; k < m; k++)
                {
                    if (prof[k].r < 1e-6f && prof[k + 1].r < 1e-6f) continue;
                    for (int s = 0; s < segs; s++)
                    {
                        int a0 = startRing[k] + s, a1 = startRing[k] + s + 1, b0 = endRing[k] + s, b1 = endRing[k] + s + 1;
                        Tri(sub, a0, a1, b1); Tri(sub, a0, b1, b0);
                    }
                }
                int AddRing(Vector2 nrm, int pi)
                {
                    int first = v.Count;
                    for (int s = 0; s <= segs; s++)
                    {
                        float a = 2 * Mathf.PI * s / segs; var radial = Mathf.Cos(a) * u + Mathf.Sin(a) * w;
                        var p = origin + axis * prof[pi].h + radial * prof[pi].r;
                        var nn = (radial * nrm.x + axis * nrm.y).normalized;
                        Vector2 uvc;
                        if (planarUv) { var d = p - origin; uvc = new Vector2(.5f + Vector3.Dot(d, u) / (2 * uvRadius), .5f + Vector3.Dot(d, w) / (2 * uvRadius)); }
                        else uvc = new Vector2((float)s / segs, total > 0 ? cum[pi] / total : 0);
                        Vert(p, nn, uvc);
                    }
                    return first;
                }
            }

            // A tube along pos(t), t in [t0, t1], radius rad(t), rings x segs; flat end caps (capSub material) when asked. The ring frame is
            // built from the world +x axis, which is never parallel to the horn or to the cage bars.
            public void Sweep(int sub, Func<float, Vector3> pos, Func<float, float> rad, float t0, float t1, int rings, int segs, bool capStart, bool capEnd, int capSubStart, int capSubEnd, float vRepeat = 1)
            {
                const float d = .002f;
                var ringStart = new int[rings]; Vector3 tanA = Vector3.forward, tanB = Vector3.forward, pA = Vector3.zero, pB = Vector3.zero, uA = Vector3.right, uB = Vector3.right, wA = Vector3.up, wB = Vector3.up; float rA = 0, rB = 0;
                for (int i = 0; i < rings; i++)
                {
                    float t = Mathf.Lerp(t0, t1, rings == 1 ? 0 : (float)i / (rings - 1));
                    var p = pos(t); var tan = (pos(t + d) - pos(t - d)).normalized;
                    var uu = (Vector3.right - tan * Vector3.Dot(Vector3.right, tan)).normalized; var ww = Vector3.Cross(tan, uu);
                    float r = rad(t); float drds = (rad(t + d) - rad(t - d)) / Mathf.Max(1e-5f, (pos(t + d) - pos(t - d)).magnitude);
                    ringStart[i] = v.Count;
                    for (int s = 0; s <= segs; s++)
                    {
                        float a = 2 * Mathf.PI * s / segs; var radial = Mathf.Cos(a) * uu + Mathf.Sin(a) * ww;
                        Vert(p + radial * r, (radial - tan * drds).normalized, new Vector2((float)s / segs, t * vRepeat));
                    }
                    if (i == 0) { tanA = tan; pA = p; uA = uu; wA = ww; rA = r; }
                    if (i == rings - 1) { tanB = tan; pB = p; uB = uu; wB = ww; rB = r; }
                }
                for (int i = 0; i < rings - 1; i++)
                    for (int s = 0; s < segs; s++)
                    {
                        int a0 = ringStart[i] + s, a1 = ringStart[i] + s + 1, b0 = ringStart[i + 1] + s, b1 = ringStart[i + 1] + s + 1;
                        Tri(sub, a0, a1, b1); Tri(sub, a0, b1, b0);
                    }
                if (capStart) Cap(capSubStart, pA, -tanA, uA, wA, rA, segs);
                if (capEnd) Cap(capSubEnd, pB, tanB, uB, wB, rB, segs);
            }
            void Cap(int sub, Vector3 centre, Vector3 outward, Vector3 u, Vector3 w, float r, int segs)
            {
                int c = Vert(centre, outward, new Vector2(.5f, .5f)); int first = v.Count;
                for (int s = 0; s <= segs; s++)
                {
                    float a = 2 * Mathf.PI * s / segs; var radial = Mathf.Cos(a) * u + Mathf.Sin(a) * w;
                    Vert(centre + radial * r, outward, new Vector2(.5f + .5f * Mathf.Cos(a), .5f + .5f * Mathf.Sin(a)));
                }
                for (int s = 0; s < segs; s++) Tri(sub, c, first + s, first + s + 1);
            }

            public int VertexCount => v.Count;
            // Wavefront OBJ of every submesh as its own group (checks and previews; the game never calls this).
            public string ToObj()
            {
                var sb = new System.Text.StringBuilder();
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                foreach (var p in v) sb.Append("v ").Append(p.x.ToString("F5", ci)).Append(' ').Append(p.y.ToString("F5", ci)).Append(' ').Append(p.z.ToString("F5", ci)).Append('\n');
                foreach (var q in n) sb.Append("vn ").Append(q.x.ToString("F4", ci)).Append(' ').Append(q.y.ToString("F4", ci)).Append(' ').Append(q.z.ToString("F4", ci)).Append('\n');
                for (int g = 0; g < tris.Length; g++)
                {
                    sb.Append("g sub").Append(g).Append('\n');
                    for (int i = 0; i < tris[g].Count; i += 3)
                        sb.Append("f ").Append(tris[g][i] + 1).Append("//").Append(tris[g][i] + 1).Append(' ').Append(tris[g][i + 1] + 1).Append("//").Append(tris[g][i + 1] + 1).Append(' ').Append(tris[g][i + 2] + 1).Append("//").Append(tris[g][i + 2] + 1).Append('\n');
                }
                return sb.ToString();
            }
            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = "HC period instrument / " + name };
                mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv);
                mesh.subMeshCount = tris.Length;
                for (int i = 0; i < tris.Length; i++) mesh.SetTriangles(tris[i], i);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
