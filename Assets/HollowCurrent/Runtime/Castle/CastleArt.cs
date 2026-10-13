using System;
using System.Collections.Generic;
using UnityEngine;

namespace HollowCurrent
{
    // Loads Haunted Castle meshes from Resources/Castle (FBX produced by tools/castle_architecture.py and
    // tools/castle_props.py). No dependency on Game/Investigation. Never throws: a missing asset becomes a
    // magenta cube placeholder and a "[CastleArt] missing <name>" warning.
    public static class CastleArt
    {
        // Asset names == FBX file names under Resources/Castle. Piece 1 (CastleLayout) should reference these.
        // (hc/layout had no ArtNames block when piece 2 was written; these names are defined here.)
        public static class Names
        {
            public const string Wall2m = "Wall_2m", Wall4m = "Wall_4m";
            public const string Wall2mWindow = "Wall_2m_Window", Wall4mWindow = "Wall_4m_Window";
            public const string Column = "Column", PointedArch = "PointedArch", VaultRib = "VaultRib", RoseWindow = "RoseWindow";
            public const string FloorTile = "FloorTile", StairFlight = "StairFlight", BalconyRailing = "BalconyRailing";
            public const string GreatDoor = "GreatDoor", ServiceDoor = "ServiceDoor", Door = "Door";
            public const string Pew = "Pew", BanquetTable = "BanquetTable", Altar = "Altar", StoneFont = "StoneFont";
            public const string Sarcophagus = "Sarcophagus", CoffinNiche = "CoffinNiche", StoneBridge = "StoneBridge";
            public const string GiantSkeleton = "GiantSkeleton", Candelabra = "Candelabra", FloatingBook = "FloatingBook";
            public const string PortraitFrame = "PortraitFrame", Bookshelf = "Bookshelf", Orrery = "Orrery";
            public const string DeadFountain = "DeadFountain", Drawbridge = "Drawbridge", DeadTree = "DeadTree", Banner = "Banner";
        }

        public static readonly string[] All =
        {
            Names.Wall2m, Names.Wall4m, Names.Wall2mWindow, Names.Wall4mWindow, Names.Column, Names.PointedArch, Names.VaultRib,
            Names.RoseWindow, Names.FloorTile, Names.StairFlight, Names.BalconyRailing, Names.GreatDoor, Names.ServiceDoor, Names.Door,
            Names.Pew, Names.BanquetTable, Names.Altar, Names.StoneFont, Names.Sarcophagus, Names.CoffinNiche, Names.StoneBridge,
            Names.GiantSkeleton, Names.Candelabra, Names.FloatingBook, Names.PortraitFrame, Names.Bookshelf, Names.Orrery,
            Names.DeadFountain, Names.Drawbridge, Names.DeadTree, Names.Banner
        };

        // Moon directions baked into SkyCastle (azimuth from +Z towards +X). Use for the directional moonlight rig.
        public static readonly Vector3 PaleMoonDirection = Direction(-34f, 27f);
        public static readonly Vector3 RedMoonDirection = Direction(42f, 24f);
        public static Vector3 Direction(float azimuthDeg, float elevationDeg)
        {
            float a = azimuthDeg * Mathf.Deg2Rad, e = elevationDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Cos(a) * Mathf.Cos(e));
        }

        struct Spec { public Color color; public float smooth, metal; public bool emissive; public Spec(Color c, float s, float m, bool e = false) { color = c; smooth = s; metal = m; emissive = e; } }
        // Fallback values (identical to tools/castle_common.py MATERIALS) if Resources/Castle/Materials/<name> is missing.
        static readonly Dictionary<string, Spec> Specs = new Dictionary<string, Spec>
        {
            { "Castle_Stone", new Spec(new Color(.34f, .35f, .37f), .14f, 0f) },
            { "Castle_BlackStone", new Spec(new Color(.075f, .075f, .09f), .55f, 0f) },
            { "Castle_Oak", new Spec(new Color(.17f, .105f, .06f), .22f, 0f) },
            { "Castle_Brass", new Spec(new Color(.40f, .31f, .14f), .42f, .8f) },
            { "Castle_Oxblood", new Spec(new Color(.30f, .045f, .055f), .08f, 0f) },
            { "Castle_Silver", new Spec(new Color(.56f, .57f, .60f), .38f, .85f) },
            { "Castle_Iron", new Spec(new Color(.13f, .13f, .15f), .30f, .85f) },
            { "Castle_Bone", new Spec(new Color(.62f, .58f, .48f), .20f, 0f) },
            { "Castle_Paper", new Spec(new Color(.62f, .55f, .40f), .05f, 0f) },
            { "Castle_Canvas", new Spec(new Color(.20f, .17f, .14f), .05f, 0f) },
            { "Castle_Flame", new Spec(new Color(1f, .62f, .22f), 0f, 0f, true) },
        };

        static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        static readonly HashSet<string> Warned = new HashSet<string>();
        static Material placeholder;

        // pos/rot are local to parent (world space when parent is null). Returns the instantiated root.
        public static GameObject Place(string name, Transform parent, Vector3 pos, Quaternion rot)
        {
            GameObject item = null;
            try
            {
                var prefab = Load(name);
                if (prefab != null)
                {
                    item = UnityEngine.Object.Instantiate(prefab);
                    item.name = "Castle / " + name;
                    ApplyMaterials(item);
                }
            }
            catch (Exception e) { Warn(name, e.Message); item = null; }
            if (item == null) item = Placeholder(name);
            var t = item.transform;
            t.SetParent(parent, false);
            t.localPosition = pos; t.localRotation = rot; t.localScale = Vector3.one;
            return item;
        }

        public static GameObject Place(string name, Transform parent, Vector3 pos, float yawDegrees)
            => Place(name, parent, pos, Quaternion.Euler(0f, yawDegrees, 0f));

        // Place + a MeshCollider on every mesh part (walls, floors, stairs). Doors/leaves are left collider-free.
        public static GameObject PlaceSolid(string name, Transform parent, Vector3 pos, Quaternion rot)
        {
            var go = Place(name, parent, pos, rot);
            foreach (var f in go.GetComponentsInChildren<MeshFilter>())
                if (f.sharedMesh != null && f.GetComponent<Collider>() == null && !f.name.StartsWith("Leaf") && !f.name.StartsWith("Flame"))
                    f.gameObject.AddComponent<MeshCollider>();
            return go;
        }

        // Sets RenderSettings.skybox to Resources/Castle/SkyCastle (rebuilding from the six faces if the .mat shader
        // did not resolve). Returns false and warns if the sky is missing.
        public static bool ApplySkybox()
        {
            try
            {
                var mat = Resources.Load<Material>("Castle/SkyCastle");
                if (mat == null || mat.shader == null || mat.shader.name != "Skybox/6 Sided")
                {
                    var shader = Shader.Find("Skybox/6 Sided");
                    var front = Resources.Load<Texture2D>("Castle/Sky/Sky_Front");
                    if (shader == null || front == null) { Warn("SkyCastle", "skybox shader or faces unavailable"); return false; }
                    mat = new Material(shader) { name = "SkyCastle (runtime)" };
                    foreach (var pair in new[] { ("_FrontTex", "Front"), ("_BackTex", "Back"), ("_LeftTex", "Left"), ("_RightTex", "Right"), ("_UpTex", "Up"), ("_DownTex", "Down") })
                        mat.SetTexture(pair.Item1, Resources.Load<Texture2D>("Castle/Sky/Sky_" + pair.Item2));
                }
                RenderSettings.skybox = mat;
                return true;
            }
            catch (Exception e) { Warn("SkyCastle", e.Message); return false; }
        }

        static GameObject Load(string name)
        {
            if (Prefabs.TryGetValue(name, out var cached) && cached != null) return cached;
            var prefab = Resources.Load<GameObject>("Castle/" + name);
            if (prefab == null) { Warn(name, null); return null; }
            Prefabs[name] = prefab;
            return prefab;
        }

        static void Warn(string name, string detail)
        {
            if (!Warned.Add(name)) return;
            Debug.LogWarning("[CastleArt] missing " + name + (string.IsNullOrEmpty(detail) ? "" : " (" + detail + ")"));
        }

        static void ApplyMaterials(GameObject root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                var shared = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < shared.Length; i++)
                {
                    string n = shared[i] != null ? shared[i].name : "";
                    int cut = n.IndexOf(" (Instance)", StringComparison.Ordinal); if (cut >= 0) n = n.Substring(0, cut);
                    if (!n.StartsWith("Castle_", StringComparison.Ordinal)) continue;
                    var m = GetMaterial(n);
                    if (m != null) { shared[i] = m; changed = true; }
                }
                if (changed) r.sharedMaterials = shared;
                if (r.name.StartsWith("Flame", StringComparison.Ordinal)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        static Material GetMaterial(string name)
        {
            if (Materials.TryGetValue(name, out var m) && m != null) return m;
            m = Resources.Load<Material>("Castle/Materials/" + name);
            if (m == null && Specs.TryGetValue(name, out var s))
            {
                var shader = Shader.Find("Standard");
                if (shader == null) return null;
                m = new Material(shader) { name = name };
                m.color = s.color; m.SetFloat("_Glossiness", s.smooth); m.SetFloat("_Metallic", s.metal);
                if (s.emissive) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(2.4f, 1.2f, .35f)); }
            }
            if (m != null) Materials[name] = m;
            return m;
        }

        static GameObject Placeholder(string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Castle MISSING / " + name;
            var col = go.GetComponent<Collider>(); if (col != null) UnityEngine.Object.Destroy(col);
            try
            {
                if (placeholder == null)
                {
                    var shader = Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
                    if (shader != null) placeholder = new Material(shader) { name = "CastleArt placeholder", color = Color.magenta };
                }
                if (placeholder != null) go.GetComponent<Renderer>().sharedMaterial = placeholder;
            }
            catch (Exception) { }
            return go;
        }
    }
}
