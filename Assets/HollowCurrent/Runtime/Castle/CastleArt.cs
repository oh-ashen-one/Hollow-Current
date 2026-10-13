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
        // Asset names == FBX file names under Resources/Castle. The first block is exactly CastleLayout.ArtNames (hc/layout,
        // commit 136b002); the second block is extra art the layout does not place yet (surreal dressing and loose kit).
        public static class Names
        {
            // layout contract (origin = base centre, front = +Z at yaw 0, sizes in docs/haunted-castle/art/manifest_*.json)
            public const string GateArch = "GateArch", Drawbridge = "Drawbridge", Winch = "Winch", LanternCart = "LanternCart";
            public const string Buttress = "Buttress", Battlement = "Battlement";
            public const string DoorFrame = "DoorFrame", GreatDoorFrame = "GreatDoorFrame", ServiceDoorFrame = "ServiceDoorFrame";
            public const string DoorLeaf = "DoorLeaf", GreatDoorLeaf = "GreatDoorLeaf", ServiceDoorLeaf = "ServiceDoorLeaf", PortraitDoorLeaf = "PortraitDoorLeaf";
            public const string LancetWindow = "LancetWindow", TallLancetWindow = "TallLancetWindow", ClerestoryWindow = "ClerestoryWindow";
            public const string ObservatoryWindow = "ObservatoryWindow", RoseWindow = "RoseWindow";
            public const string ColumnGreat = "ColumnGreat", Column = "Column", CryptRib = "CryptRib", RafterBeam = "RafterBeam";
            public const string Balustrade = "Balustrade", Fountain = "Fountain", DeadTree = "DeadTree", Candelabra = "Candelabra";
            public const string Pew = "Pew", Altar = "Altar", ChapelBell = "ChapelBell", LongTable = "LongTable", DiningTable = "DiningTable";
            public const string HighBackChair = "HighBackChair", Throne = "Throne", Fireplace = "Fireplace", Sideboard = "Sideboard";
            public const string KitchenHearth = "KitchenHearth", ButcherBlock = "ButcherBlock", Cauldron = "Cauldron", Bookshelf = "Bookshelf";
            public const string ReadingTable = "ReadingTable", Lectern = "Lectern", Portrait = "Portrait", CanopyBed = "CanopyBed";
            public const string Orrery = "Orrery", Telescope = "Telescope", Sarcophagus = "Sarcophagus", CryptBridge = "CryptBridge";
            public const string ReliquaryBox = "ReliquaryBox", Pedestal = "Pedestal";
            // extras
            public const string Wall2m = "Wall_2m", Wall4m = "Wall_4m", Wall2mWindow = "Wall_2m_Window", Wall4mWindow = "Wall_4m_Window";
            public const string PointedArch = "PointedArch", VaultRib = "VaultRib", FloorTile = "FloorTile", StairFlight = "StairFlight";
            public const string GreatDoor = "GreatDoor", ServiceDoor = "ServiceDoor", Door = "Door", BanquetTable = "BanquetTable";
            public const string StoneFont = "StoneFont", CoffinNiche = "CoffinNiche", StoneBridge = "StoneBridge";
            public const string GiantSkeleton = "GiantSkeleton", FloatingBook = "FloatingBook", Banner = "Banner", DrawbridgeChains = "DrawbridgeChains";
        }

        public static readonly string[] All =
        {
            Names.GateArch, Names.Drawbridge, Names.Winch, Names.LanternCart, Names.Buttress, Names.Battlement, Names.DoorFrame,
            Names.GreatDoorFrame, Names.ServiceDoorFrame, Names.DoorLeaf, Names.GreatDoorLeaf, Names.ServiceDoorLeaf, Names.PortraitDoorLeaf,
            Names.LancetWindow, Names.TallLancetWindow, Names.ClerestoryWindow, Names.ObservatoryWindow, Names.RoseWindow, Names.ColumnGreat,
            Names.Column, Names.CryptRib, Names.RafterBeam, Names.Balustrade, Names.Fountain, Names.DeadTree, Names.Candelabra, Names.Pew,
            Names.Altar, Names.ChapelBell, Names.LongTable, Names.DiningTable, Names.HighBackChair, Names.Throne, Names.Fireplace,
            Names.Sideboard, Names.KitchenHearth, Names.ButcherBlock, Names.Cauldron, Names.Bookshelf, Names.ReadingTable, Names.Lectern,
            Names.Portrait, Names.CanopyBed, Names.Orrery, Names.Telescope, Names.Sarcophagus, Names.CryptBridge, Names.ReliquaryBox,
            Names.Pedestal, Names.Wall2m, Names.Wall4m, Names.Wall2mWindow, Names.Wall4mWindow, Names.PointedArch, Names.VaultRib,
            Names.FloorTile, Names.StairFlight, Names.GreatDoor, Names.ServiceDoor, Names.Door, Names.BanquetTable, Names.StoneFont,
            Names.CoffinNiche, Names.StoneBridge, Names.GiantSkeleton, Names.FloatingBook, Names.Banner, Names.DrawbridgeChains
        };

        // The Blender FBX -> Unity axis conversion could not be verified without launching Unity. Authoring assumes the standard
        // result (model front = +Z, +X stays +X). If the first in-engine check shows assets facing -Z (door leaves swinging the
        // wrong way, drawbridge extending +Z), set this to 180 once; every Place call applies it about the asset's own origin.
        public const float ImportYaw = 0f;

        // Return value of Place: converts implicitly to Transform (what CastleLayout/CastleWorld use) and to GameObject.
        public readonly struct Placed
        {
            public readonly GameObject gameObject;
            public readonly bool placeholder;
            public Placed(GameObject go, bool isPlaceholder) { gameObject = go; placeholder = isPlaceholder; }
            public Transform transform => gameObject != null ? gameObject.transform : null;
            public static implicit operator Transform(Placed p) => p.transform;
            public static implicit operator GameObject(Placed p) => p.gameObject;
            public static implicit operator bool(Placed p) => p.gameObject != null;
        }

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
        public static Placed Place(string name, Transform parent, Vector3 pos, Quaternion rot)
        {
            GameObject item = null;
            bool missing = false;
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
            if (item == null) { item = Placeholder(name); missing = true; }
            var t = item.transform;
            t.SetParent(parent, false);
            t.localPosition = pos; t.localRotation = rot * Quaternion.Euler(0f, missing ? 0f : ImportYaw, 0f); t.localScale = Vector3.one;
            return new Placed(item, missing);
        }

        public static Placed Place(string name, Transform parent, Vector3 pos, float yawDegrees)
            => Place(name, parent, pos, Quaternion.Euler(0f, yawDegrees, 0f));

        // Place + a MeshCollider on every mesh part (walls, floors, stairs). Doors/leaves are left collider-free.
        public static Placed PlaceSolid(string name, Transform parent, Vector3 pos, Quaternion rot)
        {
            var placed = Place(name, parent, pos, rot);
            var go = placed.gameObject;
            foreach (var f in go.GetComponentsInChildren<MeshFilter>())
                if (f.sharedMesh != null && f.GetComponent<Collider>() == null && !f.name.StartsWith("Leaf") && !f.name.StartsWith("Flame"))
                    f.gameObject.AddComponent<MeshCollider>();
            return placed;
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
