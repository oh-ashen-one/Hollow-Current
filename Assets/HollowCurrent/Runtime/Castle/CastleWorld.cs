using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Art = HollowCurrent.CastleLayout.ArtNames;

namespace HollowCurrent
{
    // The Haunted Castle, built imperatively like World.Build (primitives for structure, CastleArt for dressing).
    // Everything is created under `root` in castle-local coordinates; Points are returned in world space.
    // Assumes `root` has unit scale. Floors y = -3.2 / 0 / 3.2 / 6.4, footprint x +-14, z 0..28, gate wall at z = 0.
    public sealed class CastleWorld
    {
        public struct Points
        {
            public Vector3 Spawn;                 // player start, inside the gatehouse by the lantern cart
            public float SpawnYaw;
            public Bounds GatehouseSafeBounds;    // safe interior of the gatehouse (the van analogue)
            public Vector3 Anchor;                // chapel bell (music-box analogue); an InteractKind.Anchor interactable sits here
            public Vector3 Compass;               // hollow-compass reading spot (induction analogue), dining hall
            public Vector3 Voice;                 // ear-trumpet recording spot (EVP analogue), portrait hall
            public Vector3 Print;                 // latent claw-score wall position, bedchamber west wall (faces +x)
            public Vector3 HunterSpawn;           // the Count rests on the crypt island
            public Vector3[] PerchPoints;         // feet positions: balcony W/E, rose-window ledge, library gallery, observatory, hall rafter
            public Vector3 AltarPosition;         // top centre of the chapel altar (forge)
            public Vector3[] RelicPositions;      // 0 silver blade (reliquary), 1 hallowed hilt (orrery), 2 moonstone (portrait closet)
            public Bounds DrawbridgeTrigger;      // entering this on the far bank after crossing = escape
            // extras
            public Vector3 DrawbridgeWinch;       // InteractKind.Van interactable (lower the bridge)
            public CastleDrawbridge Drawbridge;
            public Vector3 RuneRingCenter;        // clear circle in the great hall
            public float RuneRingRadius;
            public Transform RuneRingAnchor, KnifeOrbitAnchor, AltarAnchor;
            public Transform StairLoop;           // trigger volume on the service stair
            public Vector3 StairLoopReturn;       // where the loop drops you: the crypt foot of the stair
            public Light Moon;
            public List<Light> Lamps, WindowLights;
            public List<Door> Doors, ServiceDoors;
            public List<Vector3> SearchPoints;
            public List<Interactable> HidingPlaces;
            public Dictionary<string, Material> Materials;
            public NavMeshData NavData;
            public NavMeshDataInstance NavInstance;
            public Transform Root;
        }

        public static void Build(Transform root, out Points points)
        {
            var world = new CastleWorld(root);
            world.Construct();
            points = world.Collect();
        }

        public static void Dispose(ref Points p)
        {
            if (p.NavInstance.valid) p.NavInstance.Remove();
            if (p.NavData != null) Release(p.NavData);
            if (p.Materials != null) foreach (var m in p.Materials.Values) if (m) Release(m);
            p.NavData = null;
        }
        static void Release(Object item) { if (Application.isPlaying) Object.Destroy(item); else Object.DestroyImmediate(item); }

        // ---- state -------------------------------------------------------------------------------
        readonly Transform root;
        readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        readonly List<Light> lamps = new List<Light>(), windowLights = new List<Light>();
        readonly List<Door> doors = new List<Door>(), serviceDoors = new List<Door>();
        readonly List<Vector3> search = new List<Vector3>();
        readonly List<Interactable> hides = new List<Interactable>();
        CastleDrawbridge bridge;
        Transform stairLoop, runeAnchor, knifeAnchor, altarAnchor;
        Light moon;
        NavMeshData navData;
        NavMeshDataInstance navInstance;

        // hole footprints of the stair flights, filled by Stairs()
        readonly struct Area
        {
            public readonly float X0, X1, Z0, Z1;
            public Area(float x0, float x1, float z0, float z1) { X0 = x0; X1 = x1; Z0 = z0; Z1 = z1; }
            public bool Contains(float x, float z) => x > X0 && x < X1 && z > Z0 && z < Z1;
        }
        Area holeA, holeB, hole1, hole2;

        // ---- fixed point values --------------------------------------------------------------------
        static readonly Vector3 SpawnLocal = new Vector3(0, .1f, 3.2f);
        static readonly Vector3 AnchorLocal = new Vector3(12.4f, 1.0f, 11.2f);
        static readonly Vector3 CompassLocal = new Vector3(-10f, 1.1f, 16f);
        static readonly Vector3 VoiceLocal = new Vector3(9.0f, 4.4f, 16.5f);
        static readonly Vector3 PrintLocal = new Vector3(-13.52f, 4.65f, 2.0f);
        static readonly Vector3 HunterLocal = new Vector3(0, -3.2f, 21.4f);
        static readonly Vector3 AltarLocal = new Vector3(13.0f, 1.45f, 6.5f);
        static readonly Vector3 WinchLocal = new Vector3(-2.6f, 1.0f, 1.4f);
        static readonly Vector3 RuneLocal = new Vector3(0, .02f, 18.5f);   // pulled 1 m toward the entrance so the ring's far edge clears the table (z 22.05)
        static readonly Vector3 LoopReturnLocal = new Vector3(-7.0f, -3.1f, 25.4f);
        static readonly Vector3[] PerchLocal =
        {
            new Vector3(-4.4f, 3.2f, 21f), new Vector3(4.4f, 3.2f, 21f), new Vector3(0, 5.45f, 27.0f),
            new Vector3(-10f, 5.6f, 16f), new Vector3(11.4f, 8.6f, 26.4f), new Vector3(0, 8.1f, 19.5f),
        };
        static readonly Vector3[] RelicLocal = { new Vector3(13f, -2.55f, 21f), new Vector3(10.0f, 8.1f, 24.0f), new Vector3(12.3f, 4.3f, 14.4f) };

        CastleWorld(Transform root) { this.root = root; }
        Vector3 W(Vector3 v) => root.TransformPoint(v);

        Points Collect()
        {
            var p = new Points
            {
                Spawn = W(SpawnLocal), SpawnYaw = root.eulerAngles.y,
                GatehouseSafeBounds = new Bounds(W(new Vector3(0, 2.5f, 3f)), new Vector3(11.4f, 5f, 5.4f)),
                Anchor = W(AnchorLocal), Compass = W(CompassLocal), Voice = W(VoiceLocal), Print = W(PrintLocal),
                HunterSpawn = W(HunterLocal), AltarPosition = W(AltarLocal),
                DrawbridgeTrigger = new Bounds(W(new Vector3(0, 1.2f, -6.1f)), new Vector3(4f, 2.4f, 1.4f)),
                DrawbridgeWinch = W(WinchLocal), Drawbridge = bridge, RuneRingCenter = W(RuneLocal), RuneRingRadius = 3f,
                RuneRingAnchor = runeAnchor, KnifeOrbitAnchor = knifeAnchor, AltarAnchor = altarAnchor,
                StairLoop = stairLoop, StairLoopReturn = W(LoopReturnLocal), Moon = moon,
                Lamps = lamps, WindowLights = windowLights, Doors = doors, ServiceDoors = serviceDoors,
                SearchPoints = search, HidingPlaces = hides, Materials = mats,
                NavData = navData, NavInstance = navInstance, Root = root,
            };
            p.PerchPoints = new Vector3[PerchLocal.Length];
            for (int i = 0; i < PerchLocal.Length; i++) p.PerchPoints[i] = W(PerchLocal[i]);
            p.RelicPositions = new Vector3[RelicLocal.Length];
            for (int i = 0; i < RelicLocal.Length; i++) p.RelicPositions[i] = W(RelicLocal[i]);
            return p;
        }

        // ---- construction order ---------------------------------------------------------------------
        void Construct()
        {
            Palette();
            Exterior();
            Staircases();   // must precede Floors: the flights define the slab openings
            Floors();
            Walls();
            foreach (var d in CastleLayout.Doors) BuildDoor(d);
            foreach (var w in CastleLayout.Windows) BuildWindow(w);
            Moons();
            Keep();
            Balcony();
            Gatehouse();
            Courtyard();
            GreatHall();
            DiningHall();
            Kitchen();
            Chapel();
            Vestibule();
            Library();
            PortraitHall();
            Bedchamber();
            Observatory();
            Crypt();
            Reliquary();
            ServiceStair();
            Roofline();
            foreach (var c in CastleLayout.Candles) Candle(c);
            Moonlight();
            SearchPoints();
            BuildNavigation();
            Debug.Log("HC_ARCH_KIT wall and floor tiles placed=" + tileCount + " (stone variants, art-backed veneer; colliders unchanged)");
        }

        // ---- materials and primitives -----------------------------------------------------------------
        void Palette()
        {
            Mat("stone", new Color(.27f, .28f, .32f)); Mat("wall", new Color(.23f, .23f, .27f)); Mat("darkstone", new Color(.14f, .14f, .17f));
            Mat("cobble", new Color(.19f, .2f, .22f)); Mat("wood", new Color(.2f, .11f, .06f)); Mat("darkwood", new Color(.1f, .06f, .04f));
            Mat("carpet", new Color(.28f, .05f, .07f)); Mat("brass", new Color(.47f, .33f, .13f)); Mat("paper", new Color(.77f, .73f, .6f));
            Mat("metal", new Color(.18f, .21f, .2f)); Mat("black", new Color(.025f, .03f, .029f)); Mat("earth", new Color(.055f, .06f, .05f));
            Mat("cloth", new Color(.34f, .08f, .1f)); Mat("glass", new Color(.075f, .14f, .2f));
            Mat("blood", new Color(.45f, .03f, .04f), new Color(.38f, .02f, .03f));
            Mat("bloodpool", new Color(.36f, .02f, .03f), new Color(.45f, .02f, .035f));
            Mat("ember", new Color(.35f, .1f, .04f), new Color(1.8f, .5f, .12f));
            Mat("silver", new Color(.62f, .64f, .68f)); Mat("socket", new Color(.012f, .012f, .018f));
        }
        void Mat(string id, Color c, Color emission = default(Color))
        {
            var shader = Shader.Find("Standard");
            var m = new Material(shader) { name = "HC castle / " + id, color = c };
            m.SetFloat("_Glossiness", id == "metal" || id == "brass" ? .35f : id == "silver" ? .75f : id == "blood" || id == "bloodpool" ? .55f : .12f);
            if (id == "bloodpool") m.SetFloat("_Metallic", .2f);
            if (emission.maxColorComponent > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission); }
            mats[id] = m;
        }

        GameObject Box(string name, Vector3 center, Vector3 size, string mat, bool collide = true, bool render = true, float yaw = 0)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name; g.transform.SetParent(root, false);
            g.transform.localPosition = center; g.transform.localRotation = Quaternion.Euler(0, yaw, 0); g.transform.localScale = size;
            var r = g.GetComponent<Renderer>();
            if (render) r.sharedMaterial = mats[mat]; else r.enabled = false;
            g.GetComponent<Collider>().enabled = collide;
            return g;
        }
        // Castle_Blood liquid surface (scrolling normals, deep red). The Standard fallback stays red if the shader is unavailable.
        void Liquid(GameObject g)
        {
            var m = CastleArt.Surface("Castle_Blood", CastleArt.SurfaceKind.Liquid, Color.white);
            if (m) g.GetComponent<Renderer>().sharedMaterial = m;
        }
        GameObject Cyl(string name, Vector3 center, float diameter, float height, string mat)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = name; g.transform.SetParent(root, false);
            g.transform.localPosition = center; g.transform.localScale = new Vector3(diameter, height * .5f, diameter);
            Release(g.GetComponent<Collider>());
            g.GetComponent<Renderer>().sharedMaterial = mats[mat];
            g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }
        // Collision-only box (no mesh): used under CastleArt props so nav and physics never depend on art colliders.
        GameObject Proxy(string name, Vector3 center, Vector3 size, float yaw = 0, bool trigger = false)
        {
            var g = new GameObject(name);
            g.transform.SetParent(root, false); g.transform.localPosition = center; g.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var c = g.AddComponent<BoxCollider>(); c.size = size; c.isTrigger = trigger;
            return g;
        }
        Transform ArtAt(string name, Vector3 pos, float yaw = 0) => CastleArt.Place(name, root, pos, Quaternion.Euler(0, yaw, 0));
        Transform Prop(string name, Vector3 pos, float yaw = 0, bool solid = true)
        {
            var t = ArtAt(name, pos, yaw);
            if (solid)
            {
                var s = CastleLayout.ArtSize(name);
                Proxy(name + " collision", pos + Quaternion.Euler(0, yaw, 0) * new Vector3(0, s.y * .5f, 0), s, yaw);
            }
            return t;
        }
        Interactable Interact(string name, Vector3 p, Vector3 size, string mat, InteractKind kind, string label, string body, bool visible = true)
        {
            var g = Box(name, p, size, mat, true, visible);
            var i = g.AddComponent<Interactable>(); i.Kind = kind; i.Label = label; i.Body = body; return i;
        }
        void Note(string name, Vector3 p, string label, string body) => Interact(name, p, new Vector3(.42f, .035f, .3f), "paper", InteractKind.Note, label, body);

        // Wall-hugging cupboard with a slatted door (same construction as World.Hide). `face` is the point on the
        // wall face at floor level; yaw picks the front direction: 0 faces -z, 90 faces -x, 180 faces +z, -90 faces +x.
        void Hide(string name, Vector3 face, float yaw)
        {
            var rot = Quaternion.Euler(0, yaw, 0);
            var p = face + rot * Vector3.back * .40f;
            void Panel(string part, Vector3 offset, Vector3 size) => Box(name + " / " + part, p + rot * offset, size, "darkwood", true, true, yaw);
            Panel("back", new Vector3(0, 1, .35f), new Vector3(1.15f, 2, .1f));
            foreach (int side in new[] { -1, 1 }) Panel("side", new Vector3(side * .56f, 1, 0), new Vector3(.08f, 2, .8f));
            Panel("top", new Vector3(0, 2, 0), new Vector3(1.15f, .08f, .8f));
            Panel("lower door", new Vector3(0, .70f, -.43f), new Vector3(1.1f, 1.40f, .08f));
            Panel("upper door", new Vector3(0, 1.95f, -.43f), new Vector3(1.1f, .10f, .08f));
            foreach (int side in new[] { -1, 1 }) Panel("vent stile", new Vector3(side * .445f, 1.65f, -.43f), new Vector3(.21f, .50f, .08f));
            foreach (float y in new[] { 1.43f, 1.53f, 1.61f, 1.71f, 1.81f, 1.89f }) Panel("vent louvre", new Vector3(0, y, -.43f), new Vector3(.68f, .025f, .055f));
            var h = Interact(name, p + rot * new Vector3(0, 1, -.43f), new Vector3(1.1f, 2, .08f), "darkwood", InteractKind.Hide, name, "", false);
            h.transform.localRotation = rot;
            h.GetComponent<Collider>().isTrigger = true;
            h.HidePoint = W(p); h.ExitPoint = W(p + rot * new Vector3(0, 0, -1.2f));
            hides.Add(h);
        }

        // ---- slabs, walls, stairs -----------------------------------------------------------------------
        void Slab(float x0, float x1, float z0, float z1, float top, string mat, params Area[] holes)
        {
            var xs = new List<float> { x0, x1 }; var zs = new List<float> { z0, z1 };
            foreach (var h in holes)
            {
                if (h.X0 > x0 && h.X0 < x1) xs.Add(h.X0); if (h.X1 > x0 && h.X1 < x1) xs.Add(h.X1);
                if (h.Z0 > z0 && h.Z0 < z1) zs.Add(h.Z0); if (h.Z1 > z0 && h.Z1 < z1) zs.Add(h.Z1);
            }
            xs.Sort(); zs.Sort();
            for (int i = 0; i < xs.Count - 1; i++)
                for (int j = 0; j < zs.Count - 1; j++)
                {
                    float dx = xs[i + 1] - xs[i], dz = zs[j + 1] - zs[j];
                    if (dx < .01f || dz < .01f) continue;
                    float cx = (xs[i] + xs[i + 1]) * .5f, cz = (zs[j] + zs[j + 1]) * .5f;
                    bool cut = false; foreach (var h in holes) if (h.Contains(cx, cz)) cut = true;
                    if (cut) continue;
                    Box("Slab / " + mat, new Vector3(cx, top - .1f, cz), new Vector3(dx, .2f, dz), mat);
                }
        }

        struct Op { public float A0, A1, Y0, Y1; }

        // A wall segment with every door and window of the layout table cut into it.
        void Wall(bool alongX, float line, float a, float b, float yb, float yt, float t, string mat = "wall")
        {
            var ops = new List<Op>();
            foreach (var d in CastleLayout.Doors)
                if (d.RunsAlongX == alongX && Mathf.Abs(d.Line - line) < .01f && d.Center > a && d.Center < b && d.FloorY >= yb - .01f && d.FloorY < yt - .01f)
                    ops.Add(new Op { A0 = d.Center - d.Width * .5f, A1 = d.Center + d.Width * .5f, Y0 = d.FloorY, Y1 = d.FloorY + d.Height });
            foreach (var w in CastleLayout.Windows)
                if (w.RunsAlongX == alongX && Mathf.Abs(w.Line - line) < .01f && w.Center > a && w.Center < b && w.FloorY + w.Sill >= yb - .01f && w.FloorY + w.Sill < yt)
                    ops.Add(new Op { A0 = w.Center - w.Width * .5f, A1 = w.Center + w.Width * .5f, Y0 = w.FloorY + w.Sill, Y1 = w.FloorY + w.Top });
            foreach (var n in CastleLayout.Niches)
                if (n.RunsAlongX == alongX && Mathf.Abs(n.Line - line) < .01f && n.Center > a && n.Center < b && n.FloorY >= yb - .01f && n.FloorY < yt - .01f)
                    ops.Add(new Op { A0 = n.Center - CastleLayout.NicheWidth * .5f, A1 = n.Center + CastleLayout.NicheWidth * .5f, Y0 = n.FloorY + CastleLayout.NicheSill, Y1 = n.FloorY + CastleLayout.NicheTop });
            var cuts = new List<float> { a, b };
            foreach (var o in ops) { cuts.Add(o.A0); cuts.Add(o.A1); }
            cuts.Sort();
            for (int i = 0; i < cuts.Count - 1; i++)
            {
                float xa = cuts[i], xb = cuts[i + 1];
                if (xb - xa < .005f) continue;
                float mid = (xa + xb) * .5f;
                var covering = ops.FindAll(o => o.A0 < mid && o.A1 > mid);
                covering.Sort((p, q) => p.Y0.CompareTo(q.Y0));
                float cursor = yb;
                foreach (var o in covering)
                {
                    if (o.Y0 > cursor + .005f) WallSeg(alongX, line, xa, xb, cursor, o.Y0, t, mat);
                    cursor = Mathf.Max(cursor, o.Y1);
                }
                if (yt > cursor + .005f) WallSeg(alongX, line, xa, xb, cursor, yt, t, mat);
            }
        }
        void WallSeg(bool alongX, float line, float a0, float a1, float y0, float y1, float t, string mat)
        {
            float along = (a0 + a1) * .5f, len = a1 - a0, cy = (y0 + y1) * .5f, h = y1 - y0;
            if (alongX) Box("Wall", new Vector3(along, cy, line), new Vector3(len, h, t), mat);
            else Box("Wall", new Vector3(line, cy, along), new Vector3(t, h, len), mat);
            foreach (var r in VeneerRules)
                if (r.AlongX == alongX && Mathf.Abs(r.Line - line) < .01f) FaceTiles(r, alongX, line, a0, a1, y0, y1, t);
        }

        // ---- architectural veneer (round 5) --------------------------------------------------------------------------
        // The room shells stay cube colliders (nav, doors and Points are unchanged). Their visible faces in the rooms the capture
        // cameras see are dressed with the real wall art: 2 m / 4 m Wall modules (plinth, cornice, 3.2 m bands) squeezed to 0.125 m
        // so they read as coursed relief without moving any collision, with the stone varied per module.
        readonly struct VeneerRule
        {
            public readonly bool AlongX; public readonly float Line, A0, A1, Y0, Y1; public readonly int Side;
            public VeneerRule(bool alongX, float line, float a0, float a1, float y0, float y1, int side) { AlongX = alongX; Line = line; A0 = a0; A1 = a1; Y0 = y0; Y1 = y1; Side = side; }
        }
        static readonly VeneerRule[] VeneerRules =
        {
            // great hall (room side of the spine walls and of the front and back walls)
            new VeneerRule(false, -6, 14, 28, 0, 9.2f, 1), new VeneerRule(false, 6, 14, 28, 0, 9.2f, -1),
            new VeneerRule(true, 14, -6, 6, 0, 9.2f, 1), new VeneerRule(true, 28, -6, 6, 0, 9.2f, -1),
            // chapel
            new VeneerRule(false, 6, 0, 13, 0, 6.4f, 1), new VeneerRule(false, 14, 0, 13, 0, 6.4f, -1),
            new VeneerRule(true, 0, 6, 14, 0, 6.4f, 1), new VeneerRule(true, 13, 6, 14, 0, 6.4f, -1),
            // crypt
            new VeneerRule(false, -6, 14, 28, -3.2f, -.2f, 1), new VeneerRule(false, 6, 14, 28, -3.2f, -.2f, -1),
            new VeneerRule(true, 14, -6, 6, -3.2f, -.2f, 1), new VeneerRule(true, 28, -6, 6, -3.2f, -.2f, -1),
            // gatehouse (S6): the inner faces of its four walls
            new VeneerRule(false, -6, 0, 6, 0, 7.5f, 1), new VeneerRule(false, 6, 0, 6, 0, 7.5f, -1),
            new VeneerRule(true, 0, -6, 6, 0, 7.5f, 1), new VeneerRule(true, 6, -6, 6, 0, 7.5f, -1),
            // exterior front, outer face (seen from the far bank)
            new VeneerRule(true, 0, -14.45f, 14.45f, -.3f, 7.5f, -1),
        };
        // One stone family, per-module seeded pick (round 6): Weathered / Dark / Mossy with tints that equalise their mean luminance
        // (about 0.38) so neighbouring modules differ in texture and hue, not in brightness. Lower bands lean Mossy/Dark, upper
        // bands Weathered. No black stone and no flag tint: those read as untextured panels in the r5 captures.
        static readonly (string material, Color tint)[] StoneFamily =
        {
            ("Castle_StoneWeathered", new Color(.80f, .80f, .82f)), ("Castle_StoneDark", new Color(1.9f, 1.9f, 1.85f)), ("Castle_StoneMossy", new Color(1.05f, 1.05f, 1.05f)),
        };
        // Cracks and stains dominate; the black-outlined chip decal read as holes when repeated, so it appears at most once per room.
        static readonly string[] DecalNames = { "Castle_DecalCracksA", "Castle_DecalCracksB", "Castle_DecalStain", "Castle_DecalCracksA", "Castle_DecalCracksB", "Castle_DecalStain", "Castle_SootBase", "Castle_DecalChips" };
        readonly HashSet<int> chipRooms = new HashSet<int>();
        Material StoneVariant(int pick)
        {
            var v = StoneFamily[((pick % 3) + 3) % 3];
            return CastleArt.Surface(v.material, CastleArt.SurfaceKind.World, v.tint) ?? CastleArt.Surface("Castle_Stone", CastleArt.SurfaceKind.World, new Color(.8f, .8f, .84f));
        }
        static int PickFamily(System.Random rnd, bool lowerBand)
        {
            double p = rnd.NextDouble();
            return lowerBand ? (p < .38 ? 2 : p < .75 ? 1 : 0) : (p < .6 ? 0 : p < .8 ? 1 : 2);
        }
        int tileCount;
        static void Paint(Transform tile, Material m)
        {
            if (!m) return;
            foreach (var r in tile.GetComponentsInChildren<Renderer>())
            {
                var slots = new Material[r.sharedMaterials.Length]; for (int i = 0; i < slots.Length; i++) slots[i] = m;
                r.sharedMaterials = slots; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
        void FaceTiles(VeneerRule r, bool alongX, float line, float a0, float a1, float y0, float y1, float t)
        {
            float s0 = Mathf.Max(a0, r.A0), s1 = Mathf.Min(a1, r.A1), v0 = Mathf.Max(y0, r.Y0), v1 = Mathf.Min(y1, r.Y1);
            float len = s1 - s0, h = v1 - v0;
            if (len < .8f || h < 1.0f) return;
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 2f)); float w = len / n;      // 2 m modules, stretched 0.7x to 1.5x to fit
            int bands = Mathf.Max(1, Mathf.RoundToInt(h / 3.2f)); float bh = h / bands;
            float centre = line + r.Side * (t * .5f + .0625f);   // module body is 0.125 m thick after the squeeze; its back face sits on the cube face
            var rnd = new System.Random(Mathf.RoundToInt(s0 * 37) * 7919 + Mathf.RoundToInt(line * 13) * 104729 + Mathf.RoundToInt(v0 * 11) * 1299709 + r.Side);
            for (int i = 0; i < n; i++)
                for (int j = 0; j < bands; j++)
                {
                    float along = s0 + (i + .5f) * w, y = v0 + j * bh;
                    var pos = alongX ? new Vector3(along, y, centre) : new Vector3(centre, y, along);
                    var tile = ArtAt(Art.Wall2m, pos, alongX ? 0 : 90);
                    tile.localScale = new Vector3(w / 2f, bh / 3.2f, .25f);
                    Paint(tile, StoneVariant(PickFamily(rnd, y < v0 + 1.5f && v0 < 1f)));
                    MakeStatic(tile);
                    tileCount++;
                    if (rnd.NextDouble() < .30) WallDecal(rnd, RoomOf(r), alongX, line, r.Side, t, along, y, w, bh);
                }
        }

        static void MakeStatic(Transform t)
        {
#if UNITY_EDITOR
            // the prepared scene is built in the editor: mark the module static (batching, GI) so the player merges equal-material modules
            t.gameObject.isStatic = true;
#endif
        }

        // Crack / chip / stain / soot decal on a module, 1.5 cm off the relief face (the decal shader carries its own depth bias).
        static int RoomOf(VeneerRule r)
        {
            for (int i = 0; i < VeneerRules.Length; i++)
            {
                var q = VeneerRules[i];
                if (q.AlongX == r.AlongX && q.Line == r.Line && q.Side == r.Side && q.A0 == r.A0 && q.A1 == r.A1 && q.Y0 == r.Y0) return i / 4;   // hall, chapel, crypt, gatehouse, exterior
            }
            return 99;
        }
        void WallDecal(System.Random rnd, int room, bool alongX, float line, int side, float t, float along, float y, float w, float bh)
        {
            string name = DecalNames[rnd.Next(DecalNames.Length)];
            if (name == "Castle_DecalChips" && !chipRooms.Add(room)) name = "Castle_DecalCracksB";
            if (name == "Castle_SootBase" && y > 1f) name = "Castle_DecalStain";   // soot only at the foot of a wall
            var m = CastleArt.Decal(name);
            if (!m) return;
            bool soot = name == "Castle_SootBase";
            float sw = soot ? Mathf.Max(1.2f, w) : (name == "Castle_DecalChips" ? 1.5f : 2f), sh = soot ? 1.2f : (name == "Castle_DecalStain" ? 2f : (name == "Castle_DecalChips" ? 1.5f : 2f));
            float cy = soot ? y + .6f : y + bh * (.35f + .3f * (float)rnd.NextDouble());
            float ca = along + (float)(rnd.NextDouble() - .5) * Mathf.Max(0f, w - sw * .5f);
            float off = line + side * (t * .5f + .125f + .045f);
            var pos = alongX ? new Vector3(ca, cy, off) : new Vector3(off, cy, ca);
            var normal = alongX ? new Vector3(0, 0, side) : new Vector3(side, 0, 0);
            var r = SurrealProps.NewRenderer("Wall decal " + name, root, pos, Quaternion.LookRotation(-normal), SurrealProps.CentreQuad, new[] { m }, false);
            r.transform.localScale = new Vector3(sw, sh, 1);
        }

        // 2 m flagstones over a cube floor (collision unchanged). `skip` returns true for tiles that must stay open.
        void FloorTiles(float x0, float x1, float z0, float z1, float y, System.Func<float, float, bool> skip)
        {
            int k = 0;
            for (float cx = x0 + 1; cx < x1 - .01f; cx += 2)
                for (float cz = z0 + 1; cz < z1 - .01f; cz += 2, k++)
                {
                    if (skip != null && skip(cx, cz)) continue;
                    var tile = ArtAt(Art.FloorTile, new Vector3(cx, y, cz), 90 * (k % 4));
                    var v = CastleArt.Surface("Castle_FlagStone", CastleArt.SurfaceKind.World, Color.Lerp(new Color(.66f, .66f, .72f), new Color(.92f, .9f, .94f), Mathf.Repeat(k * .6180339f, 1)));
                    Paint(tile, v);
                    MakeStatic(tile);
                    tileCount++;
                }
        }

        // dir: 0 +x, 1 -x, 2 +z, 3 -z. `from` is the coordinate of the foot along the run; steps are solid blocks
        // from baseY. Returns the floor opening above the flight (every step except the last, which is flush).
        Area Stair(string name, int dir, float from, float lateral, float width, float baseY, int steps = 16)
        {
            const float rise = .2f, run = .3f;
            for (int i = 0; i < steps; i++)
            {
                float h = (i + 1) * rise, cy = baseY + h * .5f;
                float s = dir == 0 || dir == 2 ? from + (i + .5f) * run : from - (i + .5f) * run;
                if (dir < 2) Box(name + " step", new Vector3(s, cy, lateral), new Vector3(run, h, width), "stone");
                else Box(name + " step", new Vector3(lateral, cy, s), new Vector3(width, h, run), "stone");
            }
            float reach = (steps - 1) * run;
            switch (dir)
            {
                case 0: return new Area(from, from + reach, lateral - width * .5f, lateral + width * .5f);
                case 1: return new Area(from - reach, from, lateral - width * .5f, lateral + width * .5f);
                case 2: return new Area(lateral - width * .5f, lateral + width * .5f, from, from + reach);
                default: return new Area(lateral - width * .5f, lateral + width * .5f, from - reach, from);
            }
        }

        void Staircases()
        {
            holeA = Stair("Service stair A", 1, -7.2f, 24.1f, 1.2f, -3.2f);   // crypt -> ground, climbs toward -x
            holeB = Stair("Service stair B", 0, -12.9f, 26.8f, 1.2f, 0f);      // ground -> upper, climbs toward +x
            hole1 = Stair("Main stair 1", 2, 19.6f, 7.2f, 1.8f, 0f);           // ground -> upper, climbs toward +z
            hole2 = Stair("Main stair 2", 3, 26.8f, 12.8f, 1.4f, 3.2f);        // upper -> tower, climbs toward -z
            // guard rails on the open sides of each hole
            Rail(holeA.X0, holeA.Z1, holeA.X1, holeA.Z1, 0); Rail(holeA.X0, holeA.Z0, holeA.X1, holeA.Z0, 0); Rail(holeA.X1, holeA.Z0, holeA.X1, holeA.Z1, 0);
            Rail(holeB.X0, holeB.Z0, holeB.X1, holeB.Z0, 3.2f); Rail(holeB.X0, holeB.Z0, holeB.X0, holeB.Z1, 3.2f);
            Rail(hole1.X1, hole1.Z0, hole1.X1, hole1.Z1, 3.2f); Rail(hole1.X0, hole1.Z0, hole1.X1, hole1.Z0, 3.2f);
            Rail(hole2.X0, hole2.Z0, hole2.X0, hole2.Z1 + 0.75f, 6.4f); Rail(hole2.X0, hole2.Z1, hole2.X1, hole2.Z1, 6.4f);
        }

        void Rail(float x0, float z0, float x1, float z1, float y, bool art = false)
        {
            var a = new Vector3(x0, y, z0); var b = new Vector3(x1, y, z1);
            var d = b - a; float len = d.magnitude; if (len < .05f) return;
            var mid = (a + b) * .5f; bool alongX = Mathf.Abs(d.x) > Mathf.Abs(d.z);
            var size = alongX ? new Vector3(len, 1.05f, .12f) : new Vector3(.12f, 1.05f, len);
            if (art)
            {
                Proxy("Rail collision", mid + Vector3.up * .525f, size);
                int n = Mathf.Max(1, Mathf.CeilToInt(len / 2f));
                for (int i = 0; i < n; i++) ArtAt(Art.Balustrade, a + d * ((i + .5f) / n), alongX ? 0 : 90);
            }
            else Box("Rail", mid + Vector3.up * .525f, size, "darkwood");
        }

        void Floors()
        {
            // L0 (top y = 0)
            Slab(-6, 6, 0, 6, 0, "stone"); Slab(-6, 6, 6, 14, 0, "cobble"); Slab(-6, 6, 14, 28, 0, "stone");
            Slab(-14, -6, 0, 28, 0, "wood", holeA); Slab(6, 14, 0, 28, 0, "stone");
            Slab(-1.5f, 1.5f, -.45f, .45f, 0, "stone"); // gate threshold
            // L-1 (top y = -3.2); the channel around the sarcophagus island is left open
            var ring = new[] { new Area(-3, 3, 19.5f, 20.7f), new Area(-3, 3, 24.3f, 25.5f), new Area(-3, -1.8f, 20.7f, 24.3f), new Area(1.8f, 3, 20.7f, 24.3f) };
            Slab(-6, 6, 14, 28, -3.2f, "darkstone", ring); Slab(6, 14, 16, 26, -3.2f, "darkstone"); Slab(-14, -6, 22, 28, -3.2f, "darkstone");
            // L1 (top y = 3.2)
            Slab(-14, -6, 0, 28, 3.2f, "wood", holeB); Slab(6, 14, 13, 28, 3.2f, "carpet", hole1);
            // ceilings and roofs
            Slab(-6, 6, 0, 6, 5.2f, "wall");                  // gatehouse ceiling top (underside 5.0)
            Slab(-6, 6, 14, 28, 9.2f, "wall");                // great hall roof (underside 9.0)
            Slab(-14, -6, 0, 28, 6.4f, "wall");               // west wing roof (underside 6.2)
            Slab(6, 14, 0, 28, 6.4f, "wall", hole2);          // east wing roof / observatory floor
            Slab(6, 14, 19.4f, 28, 11.6f, "wall");            // observatory roof (underside 11.4)
        }

        void Balcony()
        {
            Slab(-5.7f, -3.0f, 14.3f, 27.7f, 3.2f, "darkwood"); Slab(3.0f, 5.7f, 14.3f, 27.7f, 3.2f, "darkwood"); Slab(-3.0f, 3.0f, 26.0f, 27.7f, 3.2f, "darkwood");
            Rail(-3.0f, 14.3f, -3.0f, 26.0f, 3.2f, true); Rail(3.0f, 14.3f, 3.0f, 26.0f, 3.2f, true); Rail(-3.0f, 26.0f, 3.0f, 26.0f, 3.2f, true);
            // rose-window ledge: a stone shelf at the sill, perch for the Count
            Box("Rose window ledge", new Vector3(0, 5.3f, 27.35f), new Vector3(4.0f, .3f, .9f), "stone");
            // hall rafter
            ArtAt(Art.RafterBeam, new Vector3(0, 7.6f, 19.5f), 0);
        }

        void Walls()
        {
            const float E = .9f, I = .6f;
            // exterior shell, padded at the corners
            Wall(true, 0, -14.45f, -6, -3.2f, 6.4f, E, "stone"); Wall(true, 0, -6, 6, -3.2f, 7.5f, E, "stone"); Wall(true, 0, 6, 14.45f, -3.2f, 6.4f, E, "stone");
            Wall(true, 28, -14.45f, -6, -3.2f, 6.4f, E, "stone"); Wall(true, 28, -6, 6, -3.2f, 9.2f, E, "stone"); Wall(true, 28, 6, 14.45f, -3.2f, 11.6f, E, "stone");
            Wall(false, -14, -.45f, 28.45f, -3.2f, 6.4f, E, "stone");
            Wall(false, 14, -.45f, 19.4f, -3.2f, 6.4f, E, "stone"); Wall(false, 14, 19.4f, 28.45f, -3.2f, 11.6f, E, "stone");
            // spine walls
            Wall(false, -6, 0, 14, 0, 6.4f, I); Wall(false, -6, 14, 28, 0, 9.2f, I);
            Wall(false, 6, 0, 14, 0, 6.4f, I); Wall(false, 6, 14, 28, 0, 9.2f, I);
            Wall(true, 6, -6, 6, 0, 7.5f, I); Wall(true, 14, -6, 6, 0, 9.2f, I);
            // wing partitions
            Wall(true, 10, -14, -6, 0, 6.4f, I); Wall(true, 22, -14, -6, -3.2f, 6.4f, I);
            Wall(true, 13, 6, 14, 0, 6.4f, I); Wall(true, 19.4f, 6, 14, 6.4f, 11.6f, I);
            // portrait closet (secret room behind the portrait door)
            Wall(false, 11, 13.3f, 15.5f, 3.2f, 6.2f, .4f); Wall(true, 15.5f, 11, 13.55f, 3.2f, 6.2f, .4f);
            // crypt and reliquary
            Wall(false, -6, 14, 28, -3.2f, -.2f, I); Wall(false, 6, 14, 28, -3.2f, -.2f, I); Wall(true, 14, -6, 6, -3.2f, -.2f, I);
            Wall(true, 16, 6, 14, -3.2f, -.2f, I); Wall(true, 26, 6, 14, -3.2f, -.2f, I);
        }

        // ---- openings -----------------------------------------------------------------------------------
        void BuildDoor(CastleDoorDef d)
        {
            if (d.Kind == CastleDoorKind.Gate) { BuildGate(); return; }
            bool portrait = d.Name == "Portrait door";
            if (!portrait)
                ArtAt(d.Kind == CastleDoorKind.Great ? Art.GreatDoorFrame : d.Kind == CastleDoorKind.Service ? Art.ServiceDoorFrame : Art.DoorFrame, d.Position, d.RunsAlongX ? 0 : 90);
            string leafArt = portrait ? Art.PortraitDoorLeaf : d.Kind == CastleDoorKind.Great ? Art.GreatDoorLeaf : d.Kind == CastleDoorKind.Service ? Art.ServiceDoorLeaf : Art.DoorLeaf;
            int leaves = d.Kind == CastleDoorKind.Great ? 2 : 1;
            float lw = d.Width / leaves, h = d.Height;
            for (int i = 0; i < leaves; i++)
            {
                bool forward = i == 0;
                float hingeAlong = leaves == 2 ? (forward ? d.Center - d.Width * .5f : d.Center + d.Width * .5f) : d.Center - d.Width * .5f;
                float yaw = d.RunsAlongX ? (forward ? 0 : 180) : (forward ? -90 : 90);
                var hp = d.RunsAlongX ? new Vector3(hingeAlong, d.FloorY, d.Line) : new Vector3(d.Line, d.FloorY, hingeAlong);
                string label = CastleLayout.DoorPrefix(d.Kind) + d.Name + (leaves == 2 ? (forward ? " (left)" : " (right)") : "");
                var hinge = new GameObject(label);
                hinge.transform.SetParent(root, false); hinge.transform.localPosition = hp; hinge.transform.localRotation = Quaternion.Euler(0, yaw, 0);
                var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                panel.name = d.Name; panel.transform.SetParent(hinge.transform, false);
                panel.transform.localPosition = new Vector3(lw * .5f, h * .5f, 0); panel.transform.localScale = new Vector3(lw, h, .09f);
                panel.GetComponent<Renderer>().enabled = false;      // CastleArt supplies the visible leaf
                CastleArt.Place(leafArt, hinge.transform, Vector3.zero, Quaternion.identity);
                var it = panel.AddComponent<Interactable>(); it.Kind = InteractKind.Door; it.Label = d.Name;
                var door = hinge.AddComponent<Door>(); door.Closed = hinge.transform.localRotation; it.Door = door;
                doors.Add(door); if (d.IsService) serviceDoors.Add(door);
            }
        }

        void BuildGate()
        {
            ArtAt(Art.GateArch, new Vector3(0, 0, 0), 0);
            var hinge = new GameObject("Drawbridge hinge");
            hinge.transform.SetParent(root, false); hinge.transform.localPosition = new Vector3(0, 0, -.45f);
            hinge.transform.localRotation = Quaternion.Euler(CastleDrawbridge.RaisedAngle, 0, 0);
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = "Drawbridge leaf"; leaf.transform.SetParent(hinge.transform, false);
            leaf.transform.localPosition = new Vector3(0, -.125f, -3f); leaf.transform.localScale = new Vector3(3.2f, .25f, 6f);
            leaf.GetComponent<Renderer>().enabled = false;               // CastleArt supplies the visible leaf
            // The leaf must extend toward -Z (over the moat). If the FBX axis conversion put the mesh on the +Z side (into the
            // gatehouse, which is why no bridge showed from the far bank), turn it about the hinge. The chains follow the same fix.
            var leafArt = CastleArt.Place(Art.Drawbridge, hinge.transform, Vector3.zero, Quaternion.identity);
            float artYaw = 0;
            if (ArtLocalCentre(leafArt, hinge.transform).z > 0) { artYaw = 180; leafArt.transform.localRotation = Quaternion.Euler(0, 180, 0); }
            // planks: look6 re-exported the leaf with Castle_OakPlank (4 boards per metre, UV mapped); leave that material alone (round 6
            // replaced it with brightened Castle_Oak by mistake, which is why no planks showed). Only an old plain-oak leaf gets the
            // world-space plank surface (0.25 m boards at 1 tile per metre).
            var plank = CastleArt.Surface("Castle_OakPlank", CastleArt.SurfaceKind.World, Color.white);
            if (plank) foreach (var pr in leafArt.transform.GetComponentsInChildren<Renderer>())
            {
                var slots = pr.sharedMaterials; bool any = false;
                for (int pi = 0; pi < slots.Length; pi++) if (slots[pi] && slots[pi].name.Equals("Castle_Oak", System.StringComparison.Ordinal)) { slots[pi] = plank; any = true; }
                if (any) pr.sharedMaterials = slots;
            }
            var chains = CastleArt.Place(Art.DrawbridgeChains, root, new Vector3(0, 0, -.45f), Quaternion.Euler(0, artYaw, 0));
            chains.gameObject.SetActive(false);
            var chainGate = chains.gameObject;
            foreach (int side in new[] { -1, 1 })
            {
                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = "Drawbridge parapet"; rail.transform.SetParent(hinge.transform, false);
                rail.transform.localPosition = new Vector3(side * 1.5f, .45f, -3f); rail.transform.localScale = new Vector3(.2f, .9f, 6f);
                rail.GetComponent<Renderer>().sharedMaterial = mats["stone"];
            }
            bridge = hinge.AddComponent<CastleDrawbridge>(); bridge.Hinge = hinge.transform;
            var vis = hinge.AddComponent<ChainVisibility>(); vis.bridge = bridge; vis.chains = chainGate;
        }

        // Centre of an art object's meshes in `space`'s local coordinates (exact for rotated parents; no AABB).
        static Vector3 ArtLocalCentre(Transform art, Transform space)
        {
            var sum = Vector3.zero; int n = 0;
            foreach (var f in art.GetComponentsInChildren<MeshFilter>())
                if (f.sharedMesh != null) { sum += space.InverseTransformPoint(f.transform.TransformPoint(f.sharedMesh.bounds.center)); n++; }
            return n > 0 ? sum / n : Vector3.zero;
        }

        void BuildWindow(CastleWindowDef w)
        {
            float h = w.Top - w.Sill, cy = w.FloorY + (w.Sill + w.Top) * .5f;
            var center = w.RunsAlongX ? new Vector3(w.Center, cy, w.Line) : new Vector3(w.Line, cy, w.Center);
            var n = w.RunsAlongX ? new Vector3(0, 0, w.Inward) : new Vector3(w.Inward, 0, 0);
            Proxy("Closed window collision", center, w.RunsAlongX ? new Vector3(w.Width, h, .04f) : new Vector3(.04f, h, w.Width));
            float yaw = w.RunsAlongX ? (w.Inward > 0 ? 0 : 180) : (w.Inward > 0 ? 90 : -90);
            ArtAt(w.Art, new Vector3(center.x, w.FloorY + w.Sill, center.z), yaw);
            if (!w.Light) return;
            var tangent = w.RunsAlongX ? Vector3.right : Vector3.forward;
            var g = new GameObject("Window moonlight");
            g.transform.SetParent(root, false);
            g.transform.localPosition = new Vector3(center.x, w.FloorY + w.Top - .1f, center.z) - n * .6f;
            g.transform.localRotation = Quaternion.LookRotation(n + Vector3.down * .23f + tangent * .16f);
            var l = g.AddComponent<Light>();
            l.type = LightType.Spot; l.spotAngle = 100; l.range = w.Shadows ? 9f : 7f;
            l.intensity = w.Art == Art.RoseWindow ? .5f : w.FloorY < 0 ? .17f : .36f;
            l.color = new Color(.43f, .55f, .72f);
            l.shadows = w.Shadows ? LightShadows.Soft : LightShadows.None; l.shadowStrength = .8f; l.shadowBias = .02f;
            windowLights.Add(l);
        }

        // ---- lights -------------------------------------------------------------------------------------
        void Candle(CastleCandleDef c)
        {
            var g = new GameObject("Candelabra");
            g.transform.SetParent(root, false); g.transform.localPosition = c.Pos;
            if (c.Art) UnifyFlames(CastleArt.Place(Art.Candelabra, g.transform, Vector3.zero, Quaternion.identity), g.transform);
            var lg = new GameObject("Candle light");
            lg.transform.SetParent(g.transform, false); lg.transform.localPosition = Vector3.up * c.LightHeight;
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1, .88f, .74f); l.range = c.Range; l.intensity = c.Intensity;
            l.shadows = c.Shadows ? LightShadows.Soft : LightShadows.None; l.shadowBias = .025f; l.shadowNormalBias = .15f;
            l.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Low;
            lamps.Add(l);
        }

        // The art candelabra carries mesh flames in Castle_Flame, whose emission the castle bake forces to black (every material in
        // the layout is set EmissiveIsBlack so the breaker can switch lights off): standing candelabras burned black. Replace
        // each mesh flame with the same CandleFlame billboard the floating candles use (warm additive sprite plus halo) and give
        // the stand a motionless Floater so the flames lean toward the Count like every other flame.
        void UnifyFlames(Transform art, Transform stand)
        {
            var spots = new List<Vector3>();
            foreach (var r in art.GetComponentsInChildren<Renderer>())
                if (r.name.StartsWith("Flame", System.StringComparison.Ordinal))
                {
                    var b = r.bounds; spots.Add(stand.InverseTransformPoint(new Vector3(b.center.x, b.min.y, b.center.z)));
                    r.enabled = false;
                }
            if (spots.Count == 0) return;
            var flames = new Transform[spots.Count];
            for (int i = 0; i < spots.Count; i++) flames[i] = SurrealProps.MakeCandleFlame(stand, spots[i], .17f, false, "Flame " + i, .14f).transform;
            var f = stand.gameObject.AddComponent<Floater>();
            f.phase = 0; f.bobAmplitude = 0; f.driftRadius = 0; f.yawDegreesPerSecond = 0; f.tiltDegrees = 0; f.flames = flames;
        }

        void Moonlight()
        {
            var g = new GameObject("Castle moonlight");
            g.transform.SetParent(root, false); g.transform.localRotation = Quaternion.Euler(50, -35, 0);
            moon = g.AddComponent<Light>();
            moon.type = LightType.Directional; moon.color = new Color(.36f, .51f, .75f); moon.intensity = .22f;
            moon.shadows = LightShadows.Soft; moon.shadowStrength = .85f;
            var second = new GameObject("Castle second moon");
            second.transform.SetParent(root, false); second.transform.localRotation = Quaternion.Euler(35, 120, 0);
            var l2 = second.AddComponent<Light>();
            l2.type = LightType.Directional; l2.color = new Color(.62f, .32f, .46f); l2.intensity = .06f; l2.shadows = LightShadows.None;
        }

        // ---- keep towers ----
        // Round towers with conical slate spires on the roofs, so the skyline from the far bank has at least five spires above the wall.
        void Keep()
        {
            // From the S4 eye (0, 2, -6) pitched 18 degrees the gatehouse wall top (y 7.5 at z 0) hides everything below an
            // elevation of 42 degrees at x = 0. Spires must clear that line, and none may stand on the red/pale moon rays
            // (azimuth +42 / -34, elevation 42 / 55), so there is no tower on the east wing.
            Tower("Gate turret W", new Vector3(-4.6f, 5.2f, 4.6f), 1.0f, 3.5f, 7.0f);
            Tower("Gate turret E", new Vector3(4.6f, 5.2f, 4.6f), 1.0f, 3.5f, 7.0f);
            Tower("Wing tower W", new Vector3(-10.5f, 6.4f, 2.6f), 1.3f, 5.0f, 7.0f);
            Tower("Keep", new Vector3(0, 9.2f, 22.5f), 2.4f, 7.0f, 16.0f);
            Tower("Rear tower W", new Vector3(-10f, 6.4f, 24f), 1.4f, 5.0f, 12.0f);
            Tower("Observatory spire", new Vector3(10.5f, 11.6f, 23.5f), 1.3f, 3.0f, 11.0f);
        }
        void Tower(string name, Vector3 baseCentre, float radius, float shaft, float spire)
        {
            Cyl(name + " shaft", baseCentre + new Vector3(0, shaft * .5f, 0), radius * 2f, shaft, "stone");
            Cyl(name + " corbel", baseCentre + new Vector3(0, shaft, 0), radius * 2.35f, .35f, "darkstone");
            var b = new SurrealProps.MeshBuilder(1);
            b.Prism(Vector3.zero, radius * 1.3f, .03f, spire, 10, 0);
            var g = new GameObject(name + " spire");
            g.transform.SetParent(root, false); g.transform.localPosition = baseCentre + new Vector3(0, shaft + .17f, 0);
            g.AddComponent<MeshFilter>().sharedMesh = b.Build(name + " spire");
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mats["black"]; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ---- exterior -----------------------------------------------------------------------------------
        void Exterior()
        {
            // blood moat around the walls, banks beyond, bed below
            Liquid(Box("Blood moat front", new Vector3(0, -.6f, -3.025f), new Vector3(39f, .1f, 5.15f), "blood", false));
            Liquid(Box("Blood moat back", new Vector3(0, -.6f, 31.025f), new Vector3(39f, .1f, 5.15f), "blood", false));
            Liquid(Box("Blood moat west", new Vector3(-16.975f, -.6f, 14f), new Vector3(5.05f, .1f, 28.9f), "blood", false));
            Liquid(Box("Blood moat east", new Vector3(16.975f, -.6f, 14f), new Vector3(5.05f, .1f, 28.9f), "blood", false));
            // bed ring only outside the footprint, so it never intrudes on the crypt
            Box("Moat bed front", new Vector3(0, -2.5f, -3.025f), new Vector3(39f, 1f, 5.15f), "earth");
            Box("Moat bed back", new Vector3(0, -2.5f, 31.025f), new Vector3(39f, 1f, 5.15f), "earth");
            Box("Moat bed west", new Vector3(-16.975f, -2.5f, 14f), new Vector3(5.05f, 1f, 28.9f), "earth");
            Box("Moat bed east", new Vector3(16.975f, -2.5f, 14f), new Vector3(5.05f, 1f, 28.9f), "earth");
            Box("Far bank", new Vector3(0, -1f, -33f), new Vector3(120f, 2f, 54.8f), "earth");
            Box("Bank west", new Vector3(-40f, -1f, 14f), new Vector3(41f, 2f, 39.2f), "earth", false);
            Box("Bank east", new Vector3(40f, -1f, 14f), new Vector3(41f, 2f, 39.2f), "earth", false);
            Box("Bank rear", new Vector3(0, -1f, 63f), new Vector3(120f, 2f, 54.8f), "earth", false);
        }

        void Roofline()
        {
            // battlements along the roof edges (decor only, roofs are not reachable)
            for (float z = 2f; z < 28f; z += 4f) ArtAt(Art.Battlement, new Vector3(-13.6f, 6.4f, z), 90);
            for (float z = 2f; z < 19f; z += 4f) ArtAt(Art.Battlement, new Vector3(13.6f, 6.4f, z), 90);
            for (float x = -12f; x <= -6f; x += 4f) { ArtAt(Art.Battlement, new Vector3(x, 6.4f, .4f), 0); ArtAt(Art.Battlement, new Vector3(x, 6.4f, 27.6f), 0); }
            for (float x = 8f; x <= 12f; x += 4f) ArtAt(Art.Battlement, new Vector3(x, 6.4f, .4f), 0);
            for (float x = -4f; x <= 4f; x += 4f) ArtAt(Art.Battlement, new Vector3(x, 9.2f, 27.6f), 0);
            for (float x = -4f; x <= 4f; x += 4f) ArtAt(Art.Battlement, new Vector3(x, 5.2f, .4f), 0);
            for (float x = 8f; x <= 12f; x += 4f) ArtAt(Art.Battlement, new Vector3(x, 11.6f, 27.6f), 0);
            foreach (float z in new[] { 21f, 26f }) ArtAt(Art.Battlement, new Vector3(13.6f, 11.6f, z), 90);
            // buttresses on the long exterior faces
            foreach (float z in new[] { 6f, 12f, 17f, 23f }) { ArtAt(Art.Buttress, new Vector3(-14.9f, -3.2f, z), 90); ArtAt(Art.Buttress, new Vector3(14.9f, -3.2f, z), -90); }
            ArtAt(Art.Buttress, new Vector3(-8f, -3.2f, -.9f), 180); ArtAt(Art.Buttress, new Vector3(8f, -3.2f, -.9f), 180);
        }

        // ---- rooms ---------------------------------------------------------------------------------------
        void Gatehouse()
        {
            Prop(Art.LanternCart, new Vector3(3.6f, 0, 3.0f), 0);
            Interact("Equipment rack", new Vector3(4.9f, .8f, 1.5f), new Vector3(.5f, 1.0f, 1.3f), "metal", InteractKind.Kit,
                "Field kit · hollow compass, grave-ash lantern, vellum ear-trumpet", "");
            Prop(Art.Winch, new Vector3(WinchLocal.x, 0, WinchLocal.z), 0, false);
            Interact("Drawbridge winch", WinchLocal, new Vector3(.9f, 1.6f, .7f), "metal", InteractKind.Van, "Drawbridge winch", "", false);
            Box("Gatehouse table", new Vector3(-1.4f, .4f, 5.3f), new Vector3(1.4f, .8f, .7f), "wood");
            Note("Gate keeper's letter", new Vector3(-1.4f, .82f, 5.3f), "Gate keeper's letter",
                "THE MARGRAVE'S GATE\n\nThe last steward bolted the great doors behind him and swore the night would not end for any of us. He was right. The sun has not come over the wall since.\n\nThe drawbridge winch is behind you. Crossing is the only way out, and the bridge will not lower for anyone who has not read the Blood Ledger and survived the hunt.\n\nSomething sleeps in the crypt. Do not let it find you.");
        }

        void Courtyard()
        {
            Prop(Art.Fountain, new Vector3(0, 0, 10f), 0);
            Prop(Art.DeadTree, new Vector3(-4.2f, 0, 12.4f), 20);
            Prop(Art.DeadTree, new Vector3(4.2f, 0, 7.6f), -50);
        }

        void GreatHall()
        {
            foreach (int side in new[] { -1, 1 })
                foreach (float z in new[] { 15.6f, 18.6f, 21.6f, 24.4f })
                    Pier(new Vector3(side * 3.9f, 0, z));
            // The banquet table lies on the hall axis (x = 0), its throne at the far end, chairs down both sides.
            var table = ArtAt(Art.LongTable, CastleLayout.TablePos, CastleLayout.TableYaw);
            table.localScale = new Vector3(CastleLayout.TableLengthScale, 1, CastleLayout.TableWidthScale);
            var tableSize = CastleLayout.ArtSize(Art.LongTable);
            Proxy("LongTable collision", CastleLayout.TablePos + new Vector3(0, tableSize.y * .5f, 0), new Vector3(tableSize.x * CastleLayout.TableLengthScale, tableSize.y, tableSize.z * CastleLayout.TableWidthScale), CastleLayout.TableYaw);
            Prop(Art.Throne, new Vector3(0f, 0, 27.05f), 180);
            foreach (float z in new[] { 23.3f, 24.65f, 26.0f })
            {
                Prop(Art.HighBackChair, new Vector3(1.15f, 0, z), -90, false);
                Prop(Art.HighBackChair, new Vector3(-1.15f, 0, z), 90, false);
            }
            runeAnchor = new GameObject("RuneRing anchor").transform; runeAnchor.SetParent(root, false); runeAnchor.localPosition = RuneLocal;
            Hide("Hall armoire", new Vector3(5.7f, 0, 21.0f), 90);
            HallDressing();
        }

        // Giant skeleton, banners, floating candelabras and books. Candelabras share one mesh and carry billboard flames only;
        // the hall keeps exactly the point lights CastleLayout.Candles gives it.
        void HallDressing()
        {
            FloorTiles(-6, 6, 14, 28, 0, null);
            HallArcade();
            VaultCeiling();
            var skeleton = ArtAt(Art.GiantSkeleton, CastleLayout.SkeletonPos, CastleLayout.SkeletonYaw);
            skeleton.localScale = CastleLayout.SkeletonScaleVec;
            CheckHallAxis(skeleton);
            foreach (var b in CastleLayout.HallBanners) ArtAt(CastleArt.Names.TapestryPanel, b.Pos, b.Yaw);
            // focal piece on the hall axis: the iron chandelier hangs over the table (hub y 3.9, 4.7 m of chain to the vault) behind
            // the S5 camera (eye z 25.3), with the throne at the far end of the table
            SurrealProps.MakeChandelier(root, new Vector3(0f, 3.9f, 24.0f), 7, 4.7f);
            int seed = 100;
            foreach (var p in CastleLayout.HallCandelabras)
            {
                if (CastleLayout.NearCaptureLine(p, 1.2f)) continue;   // keep the S5/S6 camera lines clear
                var f = SurrealProps.MakeFloater(root, p, FloaterKind.Candelabra, seed++, 0);
                f.transform.localScale = Vector3.one * 1.6f;
            }
            for (int i = 0; i < CastleLayout.HallBooks.Length; i++)
            {
                if (CastleLayout.NearCaptureLine(CastleLayout.HallBooks[i], 1.2f)) continue;
                var book = ArtAt(Art.FloatingBook, CastleLayout.HallBooks[i], 0);
                book.localScale = Vector3.one * 2.2f;
                // pages tipped up toward the entrance so the open book reads from the aisle
                book.localRotation = Quaternion.Euler(-(52 + (i * 7) % 20), (i % 2 == 0 ? 1 : -1) * (14 + i * 9), (i % 3 - 1) * 12);
                var f = book.gameObject.AddComponent<Floater>();
                f.phase = Mathf.Repeat((i + 1) * .6180339f, 1);
                f.bobAmplitude = .16f; f.bobHertz = .13f + .02f * (i % 3); f.tiltDegrees = 6; f.yawDegreesPerSecond = 5;
            }
        }

        // Pointed arches between the colonnade columns (their piers sit inside the column footprints) and cross ribs springing
        // from the columns at 6 m across the hall. Pure visuals: no colliders.
        void HallArcade()
        {
            int k = 0;
            foreach (int side in new[] { -1, 1 })
                foreach (float zc in new[] { 17.1f, 20.1f, 23.0f })
                {
                    var arch = ArtAt(Art.PointedArch, new Vector3(side * 3.9f, 0, zc), 90);
                    arch.localScale = new Vector3(Mathf.Approximately(zc, 23.0f) ? .93f : 1f, 1, 1);
                    Paint(arch, StoneVariant(0)); MakeStatic(arch); k++;
                }
            foreach (float z in new[] { 15.6f, 18.6f, 21.6f, 24.4f })
            {
                var rib = ArtAt(Art.VaultRib, new Vector3(0, 6.0f, z), 0);
                rib.localScale = new Vector3(1.95f, 1, 1);   // 4 m span -> 7.8 m, apex 8.84 under the 9.0 roof
                Paint(rib, StoneVariant(0)); MakeStatic(rib); k++;
            }
        }

        // A lit, slightly vaulted stone ceiling under the flat roof slab (which rendered as a black void): an elliptic cove from
        // y 8.4 at the side walls to 8.95 on the axis. A VaultCeiling component keeps it out of static GI (lit by probes, the
        // realtime lights and a faint cold emission so it can never fall to pure black).
        void VaultCeiling()
        {
            const int cols = 16, rows = 5; const float x0 = -5.7f, x1 = 5.7f, z0 = 14.3f, z1 = 27.55f;
            var verts = new Vector3[(cols + 1) * (rows + 1)]; var uv = new Vector2[verts.Length];
            for (int j = 0; j <= rows; j++)
                for (int i = 0; i <= cols; i++)
                {
                    float x = Mathf.Lerp(x0, x1, i / (float)cols), z = Mathf.Lerp(z0, z1, j / (float)rows);
                    float y = 8.4f + .55f * Mathf.Sqrt(Mathf.Max(0, 1 - (x / 5.7f) * (x / 5.7f)));
                    verts[j * (cols + 1) + i] = new Vector3(x, y, z); uv[j * (cols + 1) + i] = new Vector2(x * .5f, z * .5f);
                }
            var tris = new List<int>();
            for (int j = 0; j < rows; j++)
                for (int i = 0; i < cols; i++)
                {
                    int a = j * (cols + 1) + i, b = a + 1, c = a + cols + 1, d = c + 1;
                    tris.Add(a); tris.Add(b); tris.Add(c); tris.Add(b); tris.Add(d); tris.Add(c);   // faces down
                }
            var mesh = new Mesh { name = "Hall vault" };
            mesh.vertices = verts; mesh.uv = uv; mesh.triangles = tris.ToArray(); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var g = new GameObject("Hall vault"); g.transform.SetParent(root, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = g.AddComponent<MeshRenderer>(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var m = new Material(Shader.Find("Standard")) { name = "HC castle / vault", color = new Color(.30f, .32f, .38f) };
            var tex = Resources.Load<Texture2D>("Castle/Textures/Castle_Stone_albedo");
            if (tex) { m.mainTexture = tex; m.mainTextureScale = Vector2.one; }
            m.SetFloat("_Glossiness", .1f); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(.035f, .045f, .075f));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            mr.sharedMaterial = m;
            mats["vault"] = m;
            g.AddComponent<VaultCeiling>();
        }

        // Numeric checks for the owner's layout rule: the table and the rune ring sit on the hall axis (x = 0, within 0.05 m) and the
        // skeleton stays on the west side (its bounds never cross x = -3). Logged as HC_HALL_AXIS; a violation also logs an error.
        void CheckHallAxis(Transform skeleton)
        {
            float tableX = CastleLayout.TablePos.x, ringX = RuneLocal.x;
            // analytic bounds of the art (Blender x +-2.06 lateral) after the squeeze, and the measured renderer bounds
            float analyticMax = CastleLayout.SkeletonPos.x + 2.06f * CastleLayout.SkeletonScaleVec.x;
            float measuredMax = float.NegativeInfinity; bool any = false;
            foreach (var r in skeleton.GetComponentsInChildren<Renderer>())
            {
                var b = r.bounds; var local = root.InverseTransformPoint(b.max);
                measuredMax = Mathf.Max(measuredMax, local.x); any = true;
            }
            bool ok = Mathf.Abs(tableX) <= .05f && Mathf.Abs(ringX) <= .05f && analyticMax <= CastleLayout.SkeletonMaxX && (!any || measuredMax <= CastleLayout.SkeletonMaxX);
            string line = "HC_HALL_AXIS table centre x=" + tableX.ToString("F3") + " rune ring centre x=" + ringX.ToString("F3") + " skeleton max x analytic=" + analyticMax.ToString("F2") + " measured=" + (any ? measuredMax.ToString("F2") : "n/a") + " (limit " + CastleLayout.SkeletonMaxX.ToString("F1") + ") " + (ok ? "PASS" : "FAIL");
            if (ok) Debug.Log(line); else Debug.LogError(line);
        }

        // Colonnade pier built from the wall modules (round 6): the round ColumnGreat art stood inside the square arch piers and poked
        // through them. The column visual is gone; the collider box of the old column stays. Four module faces form a coursed square
        // pier of the old footprint (0.9 m, 8.8 m); the arch piers and cross ribs spring from it.
        void Pier(Vector3 foot)
        {
            const float size = .92f, height = 8.8f;   // 1 cm proud of the arch piers (0.9 m footprint), so their coplanar faces cannot show through
            Proxy("ColumnGreat collision", foot + new Vector3(0, height * .5f, 0), new Vector3(.9f, height, .9f));
            int bands = Mathf.RoundToInt(height / 3.2f); float bh = height / bands;
            for (int face = 0; face < 4; face++)
            {
                bool alongX = face < 2; int sign = face % 2 == 0 ? 1 : -1;
                // the two z-facing panels run the full width; the x-facing ones stop 0.125 m short at each end, so no two modules overlap
                float width = alongX ? size : size - .25f;
                for (int j = 0; j < bands; j++)
                {
                    float off = size * .5f - .0625f;
                    var pos = foot + (alongX ? new Vector3(0, j * bh, sign * off) : new Vector3(sign * off, j * bh, 0));
                    var tile = ArtAt(Art.Wall2m, pos, alongX ? 0 : 90);
                    tile.localScale = new Vector3(width / 2f, bh / 3.2f, .25f);
                    Paint(tile, StoneVariant(0));    // one material for the whole pier and its arches: one continuous world-space course grid
                    MakeStatic(tile);
                    tileCount++;
                }
            }
        }

        // The two moons are painted into the openings of the rose window and the chapel windows as distant sprites, placed
        // on the rays from the capture eyes through each opening (the skybox moons sit at azimuths no narrow window frames).
        void Moons()
        {
            // Rose window: both moons are painted into the opening itself, just outside the wall, so they sit inside the glass from
            // every viewpoint in the hall (S1 from the doors, the round-6 S5 from the rose-window ledge) instead of on one eye's ray.
            // WindowMoon hides them from outside, where the real sky moons are the only moons.
            foreach (int k in new[] { 0, 1 })
            {
                var at = new Vector3(k == 0 ? -.72f : .72f, k == 0 ? 7.25f : 6.95f, 28.85f);
                SurrealProps.MakeMoon(root, at, at + new Vector3(0, -.3f, -8f), .5f, k == 1);
            }
            // Chapel windows: no sprites (round 4). The real sky shows through them, and the only window pane in the chapel is
            // CastleAtmosphere's painted-over sun window.
        }

        void DiningHall()
        {
            Prop(Art.DiningTable, new Vector3(-10f, 0, 16f), 90);
            foreach (float z in new[] { 14.2f, 16f, 17.8f })
            {
                Prop(Art.HighBackChair, new Vector3(-11.4f, 0, z), 90, false);
                Prop(Art.HighBackChair, new Vector3(-8.6f, 0, z), -90, false);
            }
            Prop(Art.Fireplace, new Vector3(-13.1f, 0, 16f), 90);
            Prop(Art.Sideboard, new Vector3(-11f, 0, 21.4f), 180);
            Hide("Dining armoire", new Vector3(-12.5f, 0, 10.3f), 180);
        }

        void Kitchen()
        {
            Prop(Art.KitchenHearth, new Vector3(-13.0f, 0, 5.0f), 90);
            Prop(Art.ButcherBlock, new Vector3(-9.5f, 0, 5.0f), 0);
            knifeAnchor = new GameObject("KnifeOrbit anchor").transform; knifeAnchor.SetParent(root, false); knifeAnchor.localPosition = new Vector3(-9.5f, 1.4f, 5.0f);
            Prop(Art.Cauldron, new Vector3(-12.4f, 0, 1.6f), 0);
            Box("Prep table", new Vector3(-8.5f, .45f, 7.5f), new Vector3(2.2f, .9f, 1.0f), "wood");
            Hide("Kitchen larder", new Vector3(-7.5f, 0, .45f), 180);
            Note("Kitchen tally", new Vector3(-8.5f, .92f, 7.5f), "Kitchen tally",
                "Tally cut into the table:\n\nMonday, six places set.\nTuesday, six places set.\nWednesday, five.\nThursday, four, and the knives would not stay in the block.\nFriday, one place, and it was not set by us.");
        }

        void Chapel()
        {
            Box("Altar step low", new Vector3(12.575f, .08f, 6.5f), new Vector3(1.95f, .16f, 5.0f), "stone");
            Box("Altar step high", new Vector3(12.875f, .24f, 6.5f), new Vector3(1.35f, .16f, 4.0f), "stone");
            Prop(Art.Altar, new Vector3(13.0f, .32f, 6.5f), -90);
            altarAnchor = new GameObject("Forge altar anchor").transform; altarAnchor.SetParent(root, false); altarAnchor.localPosition = AltarLocal;
            foreach (float x in new[] { 8.4f, 9.7f, 11.0f })
                foreach (float z in new[] { 1.7f, 4.2f, 8.8f, 11.3f })
                    if (!CastleLayout.IsFallenPew(x, z)) Prop(Art.Pew, new Vector3(x, 0, z), 90);
            foreach (var pew in CastleLayout.ChapelFallenPews) FallenPew(pew);
            Prop(Art.ChapelBell, new Vector3(AnchorLocal.x, 0, AnchorLocal.z), -90, false);
            Interact("Chapel bell", AnchorLocal, new Vector3(.6f, 1.2f, .6f), "brass", InteractKind.Anchor, "The chapel bell", "", false);
            Hide("Confessional", new Vector3(7.3f, 0, 12.7f), 0);
            Note("Chapel inscription", new Vector3(12.75f, 1.44f, 5.7f), "Chapel inscription",
                "Cut into the altar slab:\n\nWHAT WAS BARGAINED FOR NIGHT IS PAID IN BLOOD. WHAT WAS HIDDEN FROM HIM IS HIDDEN IN THREE: THE BLADE UNDER THE CRYPT, THE HILT AMONG THE STARS, THE STONE BEHIND A FACE.\n\nBring them here and the altar remembers its trade.");
            FloorTiles(6, 14, 0, 14, 0, null);
            AltarInlay();
            ChapelFont();
            ChapelBrazier();
            ChapelCandles();
        }

        // The three relic sockets (rounds 6-7): the altar top is seen edge-on from the nave (eye 0.2 m above it, 6 m away), so the
        // sockets sit on a thin slab tilted 8 degrees toward the nave (back edge up): the plane then faces the S2 eye at about 10
        // degrees instead of 2, five times the visible area. Each socket is a dark inset in a silver rim raised 4 cm, laid along the
        // altar's length (world z): a blade slot, a cross for the hilt, a round seat for the moonstone.
        void AltarInlay()
        {
            var inlay = new GameObject("Altar inlay slab").transform;
            inlay.SetParent(root, false); inlay.localPosition = new Vector3(13.0f, 1.42f, 6.5f); inlay.localRotation = Quaternion.Euler(0, 0, 8f);
            GameObject Part(string name, PrimitiveType type, Vector3 local, Vector3 scale, string mat)
            {
                var g = GameObject.CreatePrimitive(type);
                g.name = name; g.transform.SetParent(inlay, false); g.transform.localPosition = local; g.transform.localScale = scale;
                Release(g.GetComponent<Collider>());
                g.GetComponent<Renderer>().sharedMaterial = mats[mat];
                return g;
            }
            Part("Slab", PrimitiveType.Cube, new Vector3(0, .025f, 0), new Vector3(.95f, .05f, 2.1f), "black");
            // slab top at .05: each socket is a silver block standing 4 cm proud (top .09) with a dark inset face on top (.092, smaller footprint), so a silver edge frames it
            const float rimTop = .09f, insetTop = .092f, bottom = .04f;
            void Socket(string name, Vector3 centre, Vector3 rimSize, Vector3 insetSize, bool round = false)
            {
                var type = round ? PrimitiveType.Cylinder : PrimitiveType.Cube;
                float rimH = rimTop - bottom, insetH = insetTop - bottom;
                Part(name + " rim", type, centre + new Vector3(0, (rimTop + bottom) * .5f, 0), round ? new Vector3(rimSize.x, rimH * .5f, rimSize.x) : new Vector3(rimSize.x, rimH, rimSize.z), "silver");
                Part(name + " socket", type, centre + new Vector3(0, (insetTop + bottom) * .5f + .001f, 0), round ? new Vector3(insetSize.x, insetH * .5f, insetSize.x) : new Vector3(insetSize.x, insetH, insetSize.z), "socket");
            }
            Socket("Blade", new Vector3(0, 0, -.65f), new Vector3(.2f, 0, .66f), new Vector3(.12f, 0, .56f));
            Socket("Hilt bar", new Vector3(0, 0, 0), new Vector3(.17f, 0, .5f), new Vector3(.09f, 0, .42f));
            Socket("Hilt guard", new Vector3(0, 0, 0), new Vector3(.42f, 0, .15f), new Vector3(.34f, 0, .09f));
            Socket("Moonstone", new Vector3(0, 0, .65f), new Vector3(.38f, 0, 0), new Vector3(.3f, 0, 0), true);
            // carved front: cracks and a damp stain (no chip decal: it reads as holes) 2 cm off the altar front
            foreach (var d in new[] { ("Castle_DecalCracksA", 5.8f, 1.5f), ("Castle_DecalStain", 7.0f, 1.5f) })
            {
                var m = CastleArt.Decal(d.Item1); if (!m) continue;
                var r = SurrealProps.NewRenderer("Altar decal", root, new Vector3(12.43f, .86f, d.Item2), Quaternion.LookRotation(Vector3.right), SurrealProps.CentreQuad, new[] { m }, false);
                r.transform.localScale = new Vector3(d.Item3, .9f, 1);
            }
        }

        // A pew knocked onto its side: rolled about its own length axis, lifted so the low edge rests on the floor.
        void FallenPew(CastleLayout.CastlePewDef d)
        {
            var yawRot = Quaternion.Euler(0, d.Yaw, 0);
            float lift = .36f * Mathf.Sin(Mathf.Abs(d.Roll) * Mathf.Deg2Rad) + .02f;
            CastleArt.Place(Art.Pew, root, new Vector3(d.X, lift, d.Z), yawRot * Quaternion.AngleAxis(d.Roll, Vector3.right));
            float side = Mathf.Sign(d.Roll);
            Proxy("Fallen pew collision", new Vector3(d.X, 0, d.Z) + yawRot * new Vector3(0, .4f, side * .45f), new Vector3(2.4f, .8f, 1.1f), d.Yaw);
        }

        // The stone font in the aisle, its basin brimming with a glossy red liquid.
        void ChapelFont()
        {
            Prop(Art.StoneFont, CastleLayout.FontPos, 0);
            Cyl("Font liquid", CastleLayout.FontPos + new Vector3(0, 1.012f, 0), .78f, .012f, "bloodpool");
        }

        // The breaker analogue: a lit brazier. (World adds an unlit black box as a stand-in when no Breaker exists.)
        void ChapelBrazier()
        {
            var p = CastleLayout.BrazierPos;
            Cyl("Brazier foot", p + new Vector3(0, .04f, 0), .52f, .08f, "metal");
            Cyl("Brazier stem", p + new Vector3(0, .46f, 0), .12f, .84f, "metal");
            Cyl("Brazier bowl", p + new Vector3(0, .92f, 0), .72f, .14f, "metal");
            Cyl("Brazier coals", p + new Vector3(0, .995f, 0), .62f, .02f, "ember");
            SurrealProps.MakeCandleFlame(root, p + new Vector3(0, 1.0f, 0), .5f, false, "Brazier flame", .8f);
            Interact("Chapel brazier", p + new Vector3(0, .5f, 0), new Vector3(.7f, 1.0f, .7f), "metal", InteractKind.Breaker, "Chapel brazier", "", false);
        }

        // 56 candles hang in the chapel, ALL burning (billboard flame plus halo, no point light; the point-light count is unchanged).
        // 40 sit inside the S2 view cone (eye (7,1.64,6.5) looking +x) so the frame holds well over 30 flames; the hero candle above
        // the altar burns brightest.
        void ChapelCandles()
        {
            var rnd = new System.Random(2026);
            var placed = new List<Vector3> { new Vector3(12.2f, 2.4f, 5.0f) };
            SurrealProps.MakeFloatingCandle(root, placed[0], 0, true, 1.5f, true);
            int guard = 0;
            while (placed.Count < 56 && guard++ < 8000)
            {
                bool inCone = placed.Count < 41;
                float x = inCone ? Mathf.Lerp(8.6f, 13.2f, (float)rnd.NextDouble()) : Mathf.Lerp(7.4f, 13.2f, (float)rnd.NextDouble());
                float half = (x - 7f) * 1.0f;   // the S2 cone is about +-52 degrees wide: stay inside +-45
                float z = inCone ? Mathf.Clamp(6.5f + ((float)rnd.NextDouble() * 2 - 1) * half, .9f, 12.1f) : Mathf.Lerp(.9f, 12.1f, (float)rnd.NextDouble());
                float top = inCone ? Mathf.Min(5.2f, 1.64f + .65f * (x - 7f)) : 5.0f;
                var p = new Vector3(x, Mathf.Lerp(1.5f, Mathf.Max(1.9f, top), (float)rnd.NextDouble()), z);
                if (p.x > 11.3f && Mathf.Abs(p.z - 6.5f) < 1.5f && p.y < 2.2f) continue;                                    // the altar block
                if (Mathf.Abs(p.x - CastleLayout.FontPos.x) < .8f && Mathf.Abs(p.z - CastleLayout.FontPos.z) < .8f && p.y < 2.2f) continue;
                if (Mathf.Abs(p.x - CastleLayout.BrazierPos.x) < .8f && Mathf.Abs(p.z - CastleLayout.BrazierPos.z) < .8f && p.y < 2.2f) continue;
                bool crowded = false;
                foreach (var q in placed) if ((q - p).sqrMagnitude < .4f) { crowded = true; break; }
                if (crowded) continue;
                SurrealProps.MakeFloatingCandle(root, p, placed.Count, true, .9f + .4f * (float)rnd.NextDouble());
                placed.Add(p);
            }
        }

        void Vestibule()
        {
            Hide("Vestibule press", new Vector3(13.55f, 0, 16.0f), 90);
        }

        void Library()
        {
            foreach (float z in new[] { 11.5f, 16.0f, 20.6f }) Prop(Art.Bookshelf, new Vector3(-13.3f, 3.2f, z), 90);
            Prop(Art.Bookshelf, new Vector3(-10.5f, 3.2f, 21.4f), 180);
            Prop(Art.Bookshelf, new Vector3(-12.3f, 3.2f, 10.55f), 0);
            Prop(Art.Bookshelf, new Vector3(-7.9f, 3.2f, 10.55f), 0);
            Prop(Art.ReadingTable, new Vector3(-10.5f, 3.2f, 16.0f), 90);
            Note("Steward's last entry", new Vector3(-10.5f, 4.07f, 16.0f), "Steward's last entry",
                "BLOOD LEDGER, LAST PAGE IN THE STEWARD'S HAND\n\nI have hidden the three of them where he will not look, because he does not look up or down, only across. The blade I put under the stone where the dead sleep. The hilt I gave to the stars; they have kept a socket empty for it. The stone I hid behind a face that he never could abide.\n\nIf you read this and still have your own blood, find the altar.");
            Hide("Library cabinet", new Vector3(-6.3f, 3.2f, 19.8f), 90);
        }

        void PortraitHall()
        {
            foreach (float x in new[] { 7.6f, 9.6f }) Prop(Art.Portrait, new Vector3(x, 3.2f + 1.0f, 13.35f), 0, false);
            foreach (float z in new[] { 14.3f, 18.2f }) Prop(Art.Portrait, new Vector3(6.35f, 3.2f + 1.0f, z), 90, false);
            foreach (float z in new[] { 16.4f, 20.2f }) Prop(Art.Portrait, new Vector3(13.5f, 3.2f + 1.0f, z), -90, false);
            Prop(Art.Pedestal, new Vector3(12.3f, 3.2f, 14.4f), 0);
        }

        void Bedchamber()
        {
            Prop(Art.CanopyBed, new Vector3(-12.1f, 3.2f, 7.5f), 90);
            Prop(Art.Lectern, new Vector3(-8.4f, 3.2f, 6.5f), -90);
            Note("Blood Ledger page", new Vector3(-8.4f, 4.4f, 6.5f), "Blood Ledger page",
                "BLOOD LEDGER, THE MARGRAVE'S OWN HAND\n\nFirst night: the cellarer. Second night: the cook and both her girls. The bargain asked for the household one name at a time, and I wrote each name myself because he asked me to.\n\nThe candles lean when he is near. They lean toward the thing that wants us.");
            Hide("Count's wardrobe", new Vector3(-7.7f, 3.2f, .45f), 180);
        }

        void Observatory()
        {
            Prop(Art.Orrery, new Vector3(9.0f, 6.4f, 24.0f), 0);
            Prop(Art.Telescope, new Vector3(11.4f, 6.4f, 27.0f), 180);
        }

        void Crypt()
        {
            // Blood channel 1.2 m wide: a 0.3 m deep trench (floor -3.2, bed -3.5) holding glossy blood 0.1 m below the floor,
            // fenced by low curbs and crossed by a single bridge.
            Box("Channel bed", new Vector3(0, -3.65f, 22.5f), new Vector3(6.4f, .3f, 6.4f), "darkstone");
            Liquid(Box("Blood channel north", new Vector3(0, -3.31f, 24.9f), new Vector3(6.0f, .02f, 1.2f), "bloodpool", false));
            Liquid(Box("Blood channel south", new Vector3(0, -3.31f, 20.1f), new Vector3(6.0f, .02f, 1.2f), "bloodpool", false));
            Liquid(Box("Blood channel west", new Vector3(-2.4f, -3.31f, 22.5f), new Vector3(1.2f, .02f, 3.6f), "bloodpool", false));
            Liquid(Box("Blood channel east", new Vector3(2.4f, -3.31f, 22.5f), new Vector3(1.2f, .02f, 3.6f), "bloodpool", false));
            // trench walls between the slab underside (-3.4) and the bed (-3.5)
            const float lw = .04f, lh = .1f, ly = -3.45f;
            Box("Trench wall W", new Vector3(-3.0f, ly, 22.5f), new Vector3(lw, lh, 6.0f), "darkstone", false); Box("Trench wall E", new Vector3(3.0f, ly, 22.5f), new Vector3(lw, lh, 6.0f), "darkstone", false);
            Box("Trench wall S", new Vector3(0, ly, 19.5f), new Vector3(6.0f, lh, lw), "darkstone", false); Box("Trench wall N", new Vector3(0, ly, 25.5f), new Vector3(6.0f, lh, lw), "darkstone", false);
            Box("Island wall W", new Vector3(-1.8f, ly, 22.5f), new Vector3(lw, lh, 3.6f), "darkstone", false); Box("Island wall E", new Vector3(1.8f, ly, 22.5f), new Vector3(lw, lh, 3.6f), "darkstone", false);
            Box("Island wall S", new Vector3(0, ly, 20.7f), new Vector3(3.6f, lh, lw), "darkstone", false); Box("Island wall N", new Vector3(0, ly, 24.3f), new Vector3(3.6f, lh, lw), "darkstone", false);
            const float ch = .45f, ct = .2f, cy = -3.2f + ch * .5f;
            Box("Outer curb west", new Vector3(-3.1f, cy, 22.5f), new Vector3(ct, ch, 6.4f), "darkstone");
            Box("Outer curb east", new Vector3(3.1f, cy, 22.5f), new Vector3(ct, ch, 6.4f), "darkstone");
            Box("Outer curb north", new Vector3(0, cy, 25.6f), new Vector3(6.4f, ch, ct), "darkstone");
            Box("Outer curb south west", new Vector3(-1.95f, cy, 19.4f), new Vector3(2.5f, ch, ct), "darkstone");
            Box("Outer curb south east", new Vector3(1.95f, cy, 19.4f), new Vector3(2.5f, ch, ct), "darkstone");
            Box("Inner curb west", new Vector3(-1.7f, cy, 22.5f), new Vector3(ct, ch, 3.6f), "darkstone");
            Box("Inner curb east", new Vector3(1.7f, cy, 22.5f), new Vector3(ct, ch, 3.6f), "darkstone");
            Box("Inner curb north", new Vector3(0, cy, 24.2f), new Vector3(3.6f, ch, ct), "darkstone");
            Box("Inner curb south west", new Vector3(-1.25f, cy, 20.8f), new Vector3(1.1f, ch, ct), "darkstone");
            Box("Inner curb south east", new Vector3(1.25f, cy, 20.8f), new Vector3(1.1f, ch, ct), "darkstone");
            Box("Bridge deck", new Vector3(0, -3.3f, 20.1f), new Vector3(1.4f, .2f, 1.2f), "darkstone", true, false);
            CryptBridgeAndPosts();
            Prop(Art.Sarcophagus, new Vector3(0, -3.2f, 22.5f), 0);
            SarcophagusRunes(new Vector3(0, -3.2f, 22.5f));
            foreach (float z in new[] { 16.5f, 26.5f })
            {
                foreach (int side in new[] { -1, 1 }) Prop(Art.Column, new Vector3(side * 4.2f, -3.2f, z), 0);
                ArtAt(Art.CryptRib, new Vector3(0, -3.2f + 3.0f - .6f, z), 0);
            }
            Hide("Ossuary niche", new Vector3(5.7f, -3.2f, 17.6f), 90);
            FloorTiles(-6, 6, 14, 28, -3.2f, (cx, cz) => cx + 1 > -3.1f && cx - 1 < 3.1f && cz + 1 > 19.4f && cz - 1 < 25.6f);
            CoffinNiches();
            HandRoots();
        }

        // The crypt bridge sat 0.6 m off its collider (art at z 19.5, deck collider at 20.1) and 0.2 m above it; it now covers the deck,
        // with candle posts (lit billboard flames, no lights) on the four curb ends beside it so the crossing reads from afar.
        void CryptBridgeAndPosts()
        {
            ArtAt(Art.CryptBridge, new Vector3(0, -3.5f, 20.1f), 0);
            foreach (float x in new[] { -.85f, .85f })
                foreach (float z in new[] { 19.4f, 20.8f })
                    CandlePost(new Vector3(x, -3.2f + .45f, z));
        }
        void CandlePost(Vector3 top)
        {
            Cyl("Candle stub", top + new Vector3(0, .1f, 0), .07f, .2f, "paper");
            SurrealProps.MakeCandleFlame(root, top + new Vector3(0, .2f, 0), .2f, false, "Post flame", .18f);
        }

        // Coffin niches lining the west, south and north walls. The niche block sits behind the wall (Wall() cuts the
        // opening); the drawn-out coffin projects through the cut and stays solid for collision.
        void CoffinNiches()
        {
            foreach (var n in CastleLayout.Niches)
            {
                ArtAt(Art.CoffinNiche, n.Origin, n.Yaw);
                float protrusion = 1.15f - n.Thickness;   // beyond the wall's inner face
                float centre = n.Thickness * .5f + protrusion * .5f;
                // distance from the wall line to the proxy centre: half the wall + half the protrusion
                var c = n.RunsAlongX ? new Vector3(n.Center, n.FloorY + .4f, n.Line + n.Inward * centre) : new Vector3(n.Line + n.Inward * centre, n.FloorY + .4f, n.Center);
                Proxy("Coffin niche collision", c, n.RunsAlongX ? new Vector3(1.2f, .8f, protrusion) : new Vector3(protrusion, .8f, 1.2f), 0);
                // a lit candle on the coffin lid so the niche reads from across the crypt
                var lid = n.RunsAlongX ? new Vector3(n.Center, n.FloorY + .79f, n.Line + n.Inward * (n.Thickness * .5f + protrusion * .8f)) : new Vector3(n.Line + n.Inward * (n.Thickness * .5f + protrusion * .8f), n.FloorY + .79f, n.Center);
                CandlePost(lid - new Vector3(0, .1f, 0));
            }
        }

        // Violet glowing runes carved around the sarcophagus trough (emissive unlit quads pulsing as one).
        void SarcophagusRunes(Vector3 baseCentre)
        {
            var holder = new GameObject("Sarcophagus runes");
            holder.transform.SetParent(root, false); holder.transform.localPosition = baseCentre;
            holder.AddComponent<RunePulse>();
            int glyph = 0;
            foreach (float y in new[] { .36f, .66f })
            {
                foreach (int side in new[] { -1, 1 })
                {
                    foreach (float x in new[] { -.75f, -.45f, -.15f, .15f, .45f, .75f })
                        SurrealProps.MakeGlowRune(holder.transform, new Vector3(x, y, side * .508f), new Vector3(0, 0, side), .15f, glyph++);
                    foreach (float z in new[] { -.24f, 0f, .24f })
                        SurrealProps.MakeGlowRune(holder.transform, new Vector3(side * 1.008f, y, z), new Vector3(side, 0, 0), .15f, glyph++);
                }
            }
        }

        // 30 root strands hanging from the vault over the sarcophagus and the channel, each ending in pale hands. The
        // strand tops sit 2.6 to 3.0 m above the crypt floor, a crown cone reaches the ceiling, and the hands dangle
        // between roughly 0.4 and 2.3 m above the floor.
        void HandRoots()
        {
            var rnd = new System.Random(77);
            const float floorY = -3.2f, ceilingY = -.2f;
            for (int i = 0; i < 30; i++)
            {
                Vector3 p; bool overCoffin = false;
                if (i < 10) { p = new Vector3((float)(rnd.NextDouble() * 2.2 - 1.1), 0, 21.9f + (float)rnd.NextDouble() * 1.2f); overCoffin = true; }
                else if (i < 20) { p = new Vector3((float)(rnd.NextDouble() * 5.4 - 2.7), 0, 19.6f + (float)rnd.NextDouble() * 5.8f); overCoffin = Mathf.Abs(p.x) < 1.35f && p.z > 21.6f && p.z < 23.4f; }
                else { p = new Vector3((float)(rnd.NextDouble() * 5.0 - 2.5), 0, 16.0f + (float)rnd.NextDouble() * 3.2f); }
                float height = 2.6f + .4f * (float)rnd.NextDouble();           // metres above the floor
                if (overCoffin) height = Mathf.Max(height, 2.7f);
                float anchor = floorY + height;
                float maxLength = overCoffin ? anchor + 1.55f : anchor - floorY - .8f - .4f; // hand tips clear the sarcophagus / the floor
                int variant = rnd.Next(SurrealProps.HandRootVariants);
                for (int k = 0; k < SurrealProps.HandRootVariants && SurrealProps.HandRootLength(variant) > maxLength; k++) variant = (variant + 1) % SurrealProps.HandRootVariants;
                p.y = anchor;
                SurrealProps.MakeHandRoot(root, p, variant + SurrealProps.HandRootVariants * i, ceilingY + .06f - anchor);
            }
        }

        void Reliquary()
        {
            Prop(Art.ReliquaryBox, new Vector3(13.0f, -3.2f, 21.0f), -90);
        }

        void ServiceStair()
        {
            stairLoop = Proxy("StairLoop", new Vector3(-9.45f, -1.4f, 24.1f), new Vector3(4.4f, 3.0f, 1.2f), 0, true).transform;
        }

        // ---- search points and navigation ------------------------------------------------------------------
        void SearchPoints()
        {
            // hand-picked floor spots clear of furniture, two or more per room
            var pts = new[]
            {
                new Vector3(0, .1f, 7.5f), new Vector3(-3.5f, .1f, 10f), new Vector3(3.5f, .1f, 10f),
                new Vector3(0, .1f, 17.5f), new Vector3(0, .1f, 22.5f), new Vector3(-5f, .1f, 19.5f),
                new Vector3(-11.8f, .1f, 12.5f), new Vector3(-7.2f, .1f, 19f), new Vector3(-8.5f, .1f, 2.5f), new Vector3(-11f, .1f, 8.5f),
                new Vector3(9.5f, .1f, 6.5f), new Vector3(7.2f, .1f, 6.5f), new Vector3(10f, .1f, 20.5f), new Vector3(9f, .1f, 15f),
                new Vector3(-8.5f, 3.3f, 13.5f), new Vector3(-8.5f, 3.3f, 19f), new Vector3(-8.5f, 3.3f, 4f), new Vector3(-10f, 3.3f, 1.5f),
                new Vector3(9.5f, 3.3f, 17f), new Vector3(10.5f, 3.3f, 21.5f), new Vector3(-4.4f, 3.3f, 21f), new Vector3(4.4f, 3.3f, 21f),
                new Vector3(8f, 6.5f, 21.2f), new Vector3(6.9f, 6.5f, 23.5f),
                new Vector3(0, -3.1f, 16.5f), new Vector3(-4.5f, -3.1f, 23f), new Vector3(4.5f, -3.1f, 23f), new Vector3(0, -3.1f, 27f), new Vector3(9.5f, -3.1f, 21f),
                new Vector3(-9.5f, .1f, 25.4f), new Vector3(-9.5f, -3.1f, 25.4f), new Vector3(-9.5f, 3.3f, 25.4f),
            };
            foreach (var v in pts) search.Add(W(v));
        }

        // Same approach as World.BuildNavigation: bake from enabled solid BoxColliders at runtime. Doors are left
        // open to the bake (Door hierarchies are skipped), triggers are ignored, the raised drawbridge blocks the gate.
        void BuildNavigation()
        {
            var sources = new List<NavMeshBuildSource>();
            foreach (var c in root.GetComponentsInChildren<BoxCollider>())
            {
                if (!c.enabled || c.isTrigger || c.GetComponentInParent<Door>() != null) continue;
                sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, transform = c.transform.localToWorldMatrix, size = c.size, area = 0 });
            }
            var settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = .28f; settings.agentHeight = 1.8f; settings.agentClimb = .25f; settings.agentSlope = 45;
            settings.overrideVoxelSize = true; settings.voxelSize = .1f;
            navData = NavMeshBuilder.BuildNavMeshData(settings, sources, new Bounds(W(new Vector3(0, 4.5f, 14f)), new Vector3(36f, 17f, 38f)), Vector3.zero, Quaternion.identity);
            if (navData != null) navInstance = NavMesh.AddNavMeshData(navData);
        }
    }
}
