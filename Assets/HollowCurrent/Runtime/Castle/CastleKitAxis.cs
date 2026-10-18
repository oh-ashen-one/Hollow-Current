using System;
using System.Collections.Generic;
using UnityEngine;

namespace HollowCurrent
{
    // fc-fbx (wave FC): axis audit and fix for the old castle kit.
    //
    // Every Resources/Castle FBX written by tools/castle_lib.py export_asset() without castle_gothic.turn_to_unity() (the architecture, props,
    // furniture and props_r12 manifests; not the round-13 hall kit, not the exterior) imports turned 180 degrees about Y: Blender (x, y, z) lands
    // on Unity (-x, z, -y) (handoffs/art-exterior2.md section 4, fb-doors), so an asset's authored front (+Y) faces -Z and +X lands on -X at
    // yaw 0, while CastleLayout's contract (CastleArt.Place) is "front = +Z at yaw 0". Each piece is judged here from its imported mesh bounds
    // (Kit: x/y/z ranges measured from the FBX vertices with the Unity mapping) and from where its placement puts it relative to the walls:
    //   Turn        placed by the contract and visibly wrong as imported (facing or sunk into a wall, seat away from its table, socket offsets
    //               mirrored): CastleArt.Place wraps the model in an empty "Castle / <name>" root (the contract frame, which callers' positions,
    //               yaws, scales and local offsets assume) and turns the model "FBX / <name>" 180 degrees inside it. Only when the model's bounds
    //               still match the old import: a re-export that already faces +Z (bounds match the contract) or an unknown mesh is left alone.
    //   AsImported  the current orientation is right or was tuned against the import (measured layouts, organic or free-standing props).
    //   Symmetric   identical (or without a readable front) under a 180 degree turn.
    //   Hinged      door leaves: CastleWorld.BuildDoor turns a leaf found on -X (fb-doors); this file thins the leaf and sizes its collider.
    //   Adaptive    the drawbridge deck and chains: CastleWorld.BuildGate turns them toward the moat.
    // Log(root) writes HC_KIT_AXIS per placed piece (aggregated per asset for Symmetric and small AsImported pieces) and HC_KIT_AXIS_SUMMARY
    // problems=N at prepare time; tools/check_castle_kit.py fails a log (or a baked scene) with problems > 0.
    public static class CastleKitAxis
    {
        public enum Policy { Turn, AsImported, Symmetric, Hinged, Adaptive }
        public enum Check { None, WallBacked, Layout }
        public readonly struct Entry
        {
            public readonly Policy Policy;
            public readonly Check Check;
            public readonly int ImportFront;                       // the visible front in the imported model: -1 = -Z (Blender +Y), +1 = +Z
            public readonly float X0, X1, Y0, Y1, Z0, Z1;          // imported mesh bounds (model space, metres)
            public readonly bool Aggregate;                        // one log line per asset instead of per instance
            public readonly string Why;
            public Entry(Policy policy, Check check, int importFront, float x0, float x1, float y0, float y1, float z0, float z1, bool aggregate, string why)
            { Policy = policy; Check = check; ImportFront = importFront; X0 = x0; X1 = x1; Y0 = y0; Y1 = y1; Z0 = z0; Z1 = z1; Aggregate = aggregate; Why = why; }
        }

        // tools/check_castle_kit.py parses these rows (keep one entry per line).
        public static readonly Dictionary<string, Entry> Kit = new Dictionary<string, Entry>
        {
            { "Bookshelf", new Entry(Policy.Turn, Check.WallBacked, -1, -1.000f, 1.000f, 0f, 3.000f, -.250f, .250f, false, "open shelves (back panel on import +Z) faced the wall") },
            { "Fireplace", new Entry(Policy.Turn, Check.WallBacked, -1, -1.340f, 1.340f, 0f, 2.625f, -.600f, .450f, false, "firebox and mantel faced the wall, hearth 0.15 m inside it") },
            { "KitchenHearth", new Entry(Policy.Turn, Check.WallBacked, -1, -1.500f, 1.500f, 0f, 2.400f, -.520f, .520f, false, "cooking opening faced the wall") },
            { "Sideboard", new Entry(Policy.Turn, Check.WallBacked, -1, -1.300f, 1.300f, 0f, 1.100f, -.325f, .280f, false, "doors faced the wall, front 3 cm inside it") },
            { "CanopyBed", new Entry(Policy.Turn, Check.WallBacked, -1, -1.200f, 1.200f, 0f, 2.800f, -1.300f, 1.300f, false, "headboard stood in the room, foot at the wall") },
            { "TapestryPanel", new Entry(Policy.Turn, Check.WallBacked, -1, -.760f, .760f, .001f, 2.701f, -.107f, -.016f, false, "emblem and braid faced the wall, cloth hung behind its anchor") },
            { "Throne", new Entry(Policy.Turn, Check.WallBacked, -1, -.500f, .500f, 0f, 1.900f, -.450f, .450f, false, "seat faced the north wall, back to the hall") },
            { "ReliquaryBox", new Entry(Policy.Turn, Check.WallBacked, -1, -.500f, .500f, 0f, .980f, -.350f, .350f, false, "hasp face (+Y) faced the wall") },
            { "Winch", new Entry(Policy.Turn, Check.WallBacked, -1, -.625f, .500f, 0f, 1.590f, -.400f, .400f, false, "crank and ratchet faced the gate wall") },
            { "Banner", new Entry(Policy.Turn, Check.WallBacked, -1, -.695f, .695f, .101f, 3.085f, -.095f, .039f, false, "not placed: wall-mounted, would project into the wall") },
            { "StringCourse", new Entry(Policy.Turn, Check.WallBacked, -1, -2.000f, 2.000f, 0f, .240f, -.200f, 0f, false, "not placed: wall-mounted, would project into the wall") },
            { "Corbel", new Entry(Policy.Turn, Check.WallBacked, -1, -.200f, .200f, 0f, .580f, -.450f, 0f, false, "not placed: wall-mounted, would project into the wall") },
            { "GargoyleSpout", new Entry(Policy.Turn, Check.WallBacked, -1, -.250f, .250f, 0f, .620f, -1.150f, 0f, false, "not placed: wall-mounted, would project into the wall") },
            { "HighBackChair", new Entry(Policy.Turn, Check.Layout, -1, -.255f, .255f, 0f, 1.300f, -.250f, .255f, false, "seat faced away from the table") },
            { "Lectern", new Entry(Policy.Turn, Check.Layout, -1, -.340f, .340f, 0f, 1.126f, -.252f, .250f, false, "reading side faced the wall instead of the room") },
            { "Orrery", new Entry(Policy.Turn, Check.Layout, -1, -1.394f, 1.394f, 0f, 2.901f, -1.367f, 1.370f, false, "planet socket mirrored: World.OrrerySocket is in layout (contract) coordinates") },
            { "CoffinNiche", new Entry(Policy.Turn, Check.Layout, -1, -1.100f, 1.100f, 0f, 1.450f, -1.383f, 1.200f, false, "recess block stood in the crypt, coffin pointed out of the castle") },
            { "StairFlight", new Entry(Policy.Turn, Check.Layout, -1, -1.300f, 1.300f, 0f, 4.150f, -5.040f, 0f, false, "not placed: would climb toward -Z") },
            { "Portrait", new Entry(Policy.AsImported, Check.WallBacked, 1, -.450f, .450f, 0f, 1.300f, -.050f, .050f, false, "single-sided canvas faces import +Z, which the call sites point into the room") },
            { "GiantSkeleton", new Entry(Policy.AsImported, Check.None, -1, -1.543f, 1.469f, -.156f, 2.827f, -6.103f, 6.010f, false, "layout and bone collisions are measured on the import (head toward the doors)") },
            { "DeadTree", new Entry(Policy.AsImported, Check.None, -1, -.584f, 1.994f, -.044f, 4.631f, -.609f, 1.588f, false, "organic; turned crowns would enter the chapel wall (overlap 0.02 to 0.20)") },
            { "GateArch", new Entry(Policy.AsImported, Check.None, -1, -1.950f, 1.950f, 0f, 5.600f, -.500f, .500f, false, "corbels on import -Z face the approach over the moat") },
            { "Telescope", new Entry(Policy.AsImported, Check.None, -1, -.431f, .431f, -.012f, 2.341f, -.619f, 1.958f, false, "contract pose drives the tube through the north wall (22 percent in wall boxes)") },
            { "LanternCart", new Entry(Policy.AsImported, Check.None, -1, -.870f, .870f, -.025f, 1.300f, -.645f, .460f, false, "free-standing, no wall or target relation; approved shots") },
            { "ButcherBlock", new Entry(Policy.AsImported, Check.None, -1, -.700f, .700f, 0f, .940f, -.500f, .500f, false, "free-standing, no wall or target relation; approved shots") },
            { "ReadingTable", new Entry(Policy.AsImported, Check.None, -1, -1.000f, 1.000f, 0f, 1.300f, -.500f, .500f, false, "free-standing, no wall or target relation; approved shots") },
            { "Sarcophagus", new Entry(Policy.AsImported, Check.None, -1, -1.490f, 1.000f, 0f, 1.210f, -.590f, .590f, false, "free-standing, no wall or target relation; approved shots") },
            { "RelicTablet", new Entry(Policy.AsImported, Check.None, -1, -.440f, .440f, 0f, .123f, -1.020f, 1.020f, true, "pocket order only (renamed Relic tablet; approved S2)") },
            { "MoonstoneSword", new Entry(Policy.AsImported, Check.None, -1, -.141f, .141f, -.176f, .925f, -.036f, .036f, true, "pose tuned in code or placed from measured bounds") },
            { "RelicBlade", new Entry(Policy.AsImported, Check.None, -1, -.022f, .022f, 0f, .014f, -.500f, .440f, true, "pose tuned in code or placed from measured bounds") },
            { "RelicHilt", new Entry(Policy.AsImported, Check.None, -1, -.141f, .141f, .002f, .073f, -.143f, .116f, true, "pose tuned in code or placed from measured bounds") },
            { "RelicMoonstone", new Entry(Policy.AsImported, Check.None, -1, -.108f, .105f, 0f, .190f, -.089f, .100f, true, "pose tuned in code or placed from measured bounds") },
            { "RelicSocketBlade", new Entry(Policy.AsImported, Check.None, -1, -.110f, .110f, 0f, .312f, -.330f, .330f, true, "pose tuned in code or placed from measured bounds") },
            { "RelicSocketHilt", new Entry(Policy.AsImported, Check.None, -1, -.220f, .220f, 0f, .312f, -.250f, .250f, true, "pose tuned in code or placed from measured bounds") },
            { "RelicSocketMoon", new Entry(Policy.AsImported, Check.None, -1, -.200f, .200f, 0f, .312f, -.200f, .200f, true, "pose tuned in code or placed from measured bounds") },
            { "FloatingBook", new Entry(Policy.AsImported, Check.None, -1, -.209f, .209f, -.022f, .128f, -.146f, .207f, true, "near-symmetric books with tuned rotations") },
            { "FloatingBookSmall", new Entry(Policy.AsImported, Check.None, -1, -.151f, .151f, -.016f, .096f, -.109f, .149f, true, "near-symmetric books with tuned rotations") },
            { "FloatingBookLarge", new Entry(Policy.AsImported, Check.None, -1, -.276f, .276f, -.029f, .175f, -.193f, .273f, true, "near-symmetric books with tuned rotations") },
            { "BookClosed", new Entry(Policy.AsImported, Check.None, -1, -.100f, .120f, -.003f, .038f, -.070f, .070f, true, "near-symmetric books with tuned rotations") },
            { "BookClosedSmall", new Entry(Policy.AsImported, Check.None, -1, -.072f, .087f, -.002f, .027f, -.050f, .050f, true, "near-symmetric books with tuned rotations") },
            { "BookClosedLarge", new Entry(Policy.AsImported, Check.None, -1, -.157f, .180f, -.004f, .057f, -.105f, .105f, true, "near-symmetric books with tuned rotations") },
            { "Tome", new Entry(Policy.AsImported, Check.None, -1, -.110f, .150f, -.003f, .078f, -.150f, .150f, true, "near-symmetric books with tuned rotations") },
            { "TomeSmall", new Entry(Policy.AsImported, Check.None, -1, -.079f, .108f, -.002f, .056f, -.108f, .108f, true, "near-symmetric books with tuned rotations") },
            { "TomeLarge", new Entry(Policy.AsImported, Check.None, -1, -.154f, .201f, -.004f, .105f, -.201f, .201f, true, "near-symmetric books with tuned rotations") },
            { "HandRoot0", new Entry(Policy.AsImported, Check.None, -1, -.651f, .590f, -1.408f, .015f, -.189f, .169f, true, "organic, random rotations") },
            { "HandRoot1", new Entry(Policy.AsImported, Check.None, -1, -.410f, .439f, -1.560f, .012f, -.294f, .270f, true, "organic, random rotations") },
            { "HandRoot2", new Entry(Policy.AsImported, Check.None, -1, -.428f, .627f, -1.985f, .011f, -.218f, .203f, true, "organic, random rotations") },
            { "HandRoot3", new Entry(Policy.AsImported, Check.None, -1, -.319f, .377f, -2.132f, .009f, -.318f, .212f, true, "organic, random rotations") },
            { "HandRoot4", new Entry(Policy.AsImported, Check.None, -1, -.348f, .698f, -2.529f, .014f, -.236f, .233f, true, "organic, random rotations") },
            { "HandRoot5", new Entry(Policy.AsImported, Check.None, -1, -.527f, .399f, -1.791f, .014f, -.327f, .245f, true, "organic, random rotations") },
            { "RootCrown", new Entry(Policy.AsImported, Check.None, -1, -.362f, .309f, -.004f, 1.009f, -.261f, .266f, true, "organic, random rotations") },
            { "DoorLeaf", new Entry(Policy.Hinged, Check.None, -1, -1.500f, 0f, 0f, 2.000f, -.097f, .097f, false, "CastleWorld.BuildDoor turns a leaf found on -X (fb-doors)") },
            { "GreatDoorLeaf", new Entry(Policy.Hinged, Check.None, -1, -1.500f, 0f, 0f, 4.500f, -.120f, .120f, false, "CastleWorld.BuildDoor turns a leaf found on -X (fb-doors)") },
            { "ServiceDoorLeaf", new Entry(Policy.Hinged, Check.None, -1, -.900f, 0f, 0f, 2.000f, -.092f, .092f, false, "CastleWorld.BuildDoor turns a leaf found on -X (fb-doors)") },
            { "PortraitDoorLeaf", new Entry(Policy.Hinged, Check.None, -1, -.900f, 0f, 0f, 2.000f, -.102f, .102f, false, "CastleWorld.BuildDoor turns a leaf found on -X (fb-doors)") },
            { "Drawbridge", new Entry(Policy.Adaptive, Check.None, -1, -1.620f, 1.620f, -.260f, .010f, 0f, 6.000f, false, "CastleWorld.BuildGate turns the deck and chains toward the moat") },
            { "DrawbridgeChains", new Entry(Policy.Adaptive, Check.None, -1, -1.575f, 1.577f, -.016f, 4.360f, .368f, 5.909f, false, "CastleWorld.BuildGate turns the deck and chains toward the moat") },
            { "Wall_2m", new Entry(Policy.Symmetric, Check.None, -1, -1.000f, 1.000f, 0f, 3.200f, -.330f, .330f, true, "identical under a 180 degree turn") },
            { "Wall_4m", new Entry(Policy.Symmetric, Check.None, -1, -2.000f, 2.000f, 0f, 3.200f, -.330f, .330f, true, "identical under a 180 degree turn") },
            { "FloorTile", new Entry(Policy.Symmetric, Check.None, -1, -1.000f, 1.000f, -.250f, .010f, -1.000f, 1.000f, true, "identical under a 180 degree turn") },
            { "Battlement", new Entry(Policy.Symmetric, Check.None, -1, -2.000f, 2.000f, 0f, .975f, -.440f, .440f, true, "identical under a 180 degree turn") },
            { "Balustrade", new Entry(Policy.Symmetric, Check.None, -1, -1.000f, 1.000f, 0f, 1.120f, -.100f, .100f, true, "identical under a 180 degree turn") },
            { "Buttress", new Entry(Policy.Symmetric, Check.None, -1, -.450f, .450f, 0f, 6.400f, -.600f, .600f, true, "identical under a 180 degree turn") },
            { "DoorFrame", new Entry(Policy.Symmetric, Check.None, -1, -.980f, .980f, 0f, 2.600f, -.420f, .420f, true, "identical under a 180 degree turn") },
            { "GreatDoorFrame", new Entry(Policy.Symmetric, Check.None, -1, -1.830f, 1.830f, 0f, 5.200f, -.470f, .470f, true, "identical under a 180 degree turn") },
            { "ServiceDoorFrame", new Entry(Policy.Symmetric, Check.None, -1, -.630f, .630f, 0f, 2.300f, -.420f, .420f, true, "identical under a 180 degree turn") },
            { "RoseWindow", new Entry(Policy.Symmetric, Check.None, -1, -1.500f, 1.500f, 0f, 3.000f, -.250f, .250f, true, "identical under a 180 degree turn") },
            { "Column", new Entry(Policy.Symmetric, Check.None, -1, -.350f, .350f, 0f, 3.000f, -.350f, .350f, true, "identical under a 180 degree turn") },
            { "ColumnGreat", new Entry(Policy.Symmetric, Check.None, -1, -.450f, .450f, 0f, 8.800f, -.450f, .450f, true, "identical under a 180 degree turn") },
            { "CryptRib", new Entry(Policy.Symmetric, Check.None, -1, -4.200f, 4.200f, 0f, .600f, -.300f, .300f, true, "identical under a 180 degree turn") },
            { "VaultRib", new Entry(Policy.Symmetric, Check.None, -1, -2.300f, 2.300f, 0f, 2.837f, -.200f, .200f, true, "identical under a 180 degree turn") },
            { "RafterBeam", new Entry(Policy.Symmetric, Check.None, -1, -5.750f, 5.750f, -.030f, .530f, -.280f, .280f, true, "identical under a 180 degree turn") },
            { "CryptBridge", new Entry(Policy.Symmetric, Check.None, -1, -.700f, .700f, 0f, .310f, -.600f, .600f, true, "identical under a 180 degree turn") },
            { "ChapelBell", new Entry(Policy.Symmetric, Check.None, -1, -.630f, .630f, -.009f, 1.915f, -.430f, .430f, true, "identical under a 180 degree turn") },
            { "DiningTable", new Entry(Policy.Symmetric, Check.None, -1, -2.800f, 2.800f, 0f, 1.250f, -.750f, .750f, true, "identical under a 180 degree turn") },
            { "StoneFont", new Entry(Policy.Symmetric, Check.None, -1, -.520f, .520f, 0f, 1.080f, -.520f, .520f, true, "identical under a 180 degree turn") },
            { "Pedestal", new Entry(Policy.Symmetric, Check.None, -1, -.300f, .300f, 0f, .990f, -.300f, .300f, true, "identical under a 180 degree turn") },
            { "Cauldron", new Entry(Policy.Symmetric, Check.None, -1, -.520f, .520f, -.010f, 1.015f, -.495f, .495f, true, "three-legged, no front: a turn changes nothing that reads") },
            { "Fountain", new Entry(Policy.Symmetric, Check.None, -1, -1.280f, 1.280f, 0f, 1.780f, -1.217f, 1.217f, true, "round basin, no front: a turn changes nothing that reads") },
        };

        public const string Prefix = "Castle / ", ModelPrefix = "FBX / ";
        const float BoundsTolerance = .03f;
        enum Match { Import, Contract, Unknown }

        // Imported mesh bounds of a model in its own root frame (MeshFilter bounds corners; parts keep their FBX transforms).
        static bool ModelBounds(Transform model, out Bounds b)
        {
            b = default; bool any = false;
            foreach (var f in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!f.sharedMesh) continue;
                var mb = f.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = model.InverseTransformPoint(f.transform.TransformPoint(c));
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            return any;
        }
        static bool Near(float a, float b) => Mathf.Abs(a - b) <= BoundsTolerance;
        static Match Classify(Transform model, Entry e, out Bounds b)
        {
            if (!ModelBounds(model, out b)) return Match.Unknown;
            if (Near(b.min.x, e.X0) && Near(b.max.x, e.X1) && Near(b.min.z, e.Z0) && Near(b.max.z, e.Z1)) return Match.Import;
            if (Near(b.min.x, -e.X1) && Near(b.max.x, -e.X0) && Near(b.min.z, -e.Z1) && Near(b.max.z, -e.Z0)) return Match.Contract;
            return Match.Unknown;
        }

        // CastleArt.Place hook, right after Instantiate: returns the object Place positions. For Policy.Turn assets whose bounds match the old
        // import it is a new empty root "Castle / <name>" holding the model, renamed "FBX / <name>", turned 180 degrees about its own origin
        // (exactly the old placement turned about the asset origin). Everything else is returned unchanged.
        public static GameObject ImportFix(string name, GameObject model)
        {
            if (!model || !Kit.TryGetValue(name, out var e) || e.Policy != Policy.Turn) return model;
            var match = Classify(model.transform, e, out var b);
            if (match != Match.Import)
            {
                Debug.LogWarning("HC_KIT_AXIS_IMPORT " + name + " left as imported: model bounds " + b.min.ToString("F2") + ".." + b.max.ToString("F2") +
                                 (match == Match.Contract ? " already face +Z (re-exported with the axis fix)" : " match neither the old import nor the contract (unknown export)"));
                return model;
            }
            var root = new GameObject(Prefix + name);
            model.name = ModelPrefix + name;
            var t = model.transform;
            t.SetParent(root.transform, false);
            t.localPosition = Vector3.zero; t.localRotation = Quaternion.Euler(0f, 180f, 0f); t.localScale = Vector3.one;
            return root;
        }

        // The turned model under a placed root, or null when the root is the imported model itself.
        static Transform TurnedModel(Transform placed, string art)
        {
            var model = placed.Find(ModelPrefix + art);
            return model && Mathf.Abs(Mathf.DeltaAngle(model.localEulerAngles.y, 180f)) < 1f ? model : null;
        }

        // ---- door leaves (item 1) ----------------------------------------------------------------------------------------------
        // tools/castle_architecture.py DOOR_LEAF_SPECS: the oak slab (t) of each leaf; straps stand 1.5 cm and the ring plates and rings 3-6 cm
        // proud of it on both faces, so the imported leaf is 0.18-0.24 m thick overall while the collision panel was 0.09 m. A real oak leaf is
        // 0.08-0.12 m (great door) and 0.05-0.07 m (service door). The leaf art is scaled along its thickness axis so its slab is Slab, and the
        // panel gets exactly that thickness: the collider and the visible oak agree (the player stops on the planks, never inside them), the
        // hardware stands a few centimetres proud like real ironwork, and the slab stays inside the swing probe (Door.ProbeExtents.z 0.055).
        public readonly struct LeafSpec
        {
            public readonly float Authored, Overall, Slab;
            public LeafSpec(float authored, float overall, float slab) { Authored = authored; Overall = overall; Slab = slab; }
        }
        public static readonly Dictionary<string, LeafSpec> Leaves = new Dictionary<string, LeafSpec>
        {
            { "GreatDoorLeaf", new LeafSpec(.12f, .240f, .10f) },
            { "DoorLeaf", new LeafSpec(.09f, .194f, .07f) },
            { "ServiceDoorLeaf", new LeafSpec(.08f, .184f, .06f) },
            { "PortraitDoorLeaf", new LeafSpec(.10f, .204f, .07f) },
        };
        public const float LegacyPanel = .09f;

        // Scales the leaf art (a child of the hinge, turned 0 or 180 degrees about Y) along the hinge's Z and returns the thickness the
        // collision panel must have. A leaf whose overall thickness does not match the authored FBX (re-exported) is left as it is.
        public static float FitDoorLeaf(Transform leaf, Transform hinge, string art, string label)
        {
            if (!leaf || !hinge) return LegacyPanel;
            float z0 = float.PositiveInfinity, z1 = float.NegativeInfinity;
            foreach (var f in leaf.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!f.sharedMesh) continue;
                var mb = f.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    float z = hinge.InverseTransformPoint(f.transform.TransformPoint(c)).z;
                    z0 = Mathf.Min(z0, z); z1 = Mathf.Max(z1, z);
                }
            }
            float overall = z1 - z0, slab = LegacyPanel, scale = 1f;
            bool known = Leaves.TryGetValue(art, out var spec) && Mathf.Abs(overall - spec.Overall) <= .012f;
            if (known)
            {
                scale = spec.Slab / spec.Authored; slab = spec.Slab;
                leaf.localScale = new Vector3(1f, 1f, scale);
            }
            Debug.Log("HC_DOOR_LEAF_THICKNESS name=" + label + " art=" + art + " meshZ=" + overall.ToString("F3") + " slabAuthored=" + (known ? spec.Authored.ToString("F3") : "?") +
                      " slab=" + slab.ToString("F3") + " scaleZ=" + scale.ToString("F3") + " overall=" + (overall * scale).ToString("F3") + " collider=" + slab.ToString("F3") + " fitted=" + known);
            return slab;
        }

        // The portrait door's canvas is a single-sided quad whose visible side is its local +Z, but the FBX puts it on the -Z face of the slab
        // (castle_architecture.door_leaf: the canvas at y = t/2 + 0.012 with a -Y winding), so it faced into its own leaf and showed from
        // neither side. Turn it 180 degrees about its own centre when it faces the slab: the painting then faces out from the face it sits on
        // (the portrait hall once BuildDoor has turned the leaf), not mirrored.
        public static void FaceCanvasOut(Transform leaf, Transform hinge, string label)
        {
            if (!leaf || !hinge) return;
            foreach (var r in leaf.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name != "Canvas") continue;
                var c = r.transform;
                var centre = hinge.InverseTransformPoint(c.position); var facing = hinge.InverseTransformDirection(c.forward);
                bool flipped = centre.z * facing.z < 0;
                if (flipped) c.localRotation = c.localRotation * Quaternion.Euler(0f, 180f, 0f);
                Debug.Log("HC_PORTRAIT_CANVAS name=" + label + " side=" + (centre.z >= 0 ? "+z" : "-z") + " was=" + (facing.z >= 0 ? "+z" : "-z") +
                          " flipped=" + flipped + " now=" + (hinge.InverseTransformDirection(c.forward).z >= 0 ? "+z" : "-z") + " world=" + c.forward.ToString("F2"));
            }
        }

        // ---- prepare-time audit -------------------------------------------------------------------------------------------------
        static List<Bounds> WallBoxes(Transform root)
        {
            var walls = new List<Bounds>();
            foreach (var c in root.GetComponentsInChildren<BoxCollider>(true))
            {
                if (c.name != "Wall" || c.isTrigger || !c.enabled || c.GetComponentInParent<Door>()) continue;
                var t = c.transform; bool any = false; var b = new Bounds();
                for (int i = 0; i < 8; i++)
                {
                    var p = t.TransformPoint(c.center + Vector3.Scale(c.size * .5f, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
                walls.Add(b);
            }
            return walls;
        }
        static float Ray(Vector3 o, Vector3 d, List<Bounds> walls, float max = 30f)
        {
            float best = max;
            foreach (var w in walls)
            {
                float t0 = 0, t1 = best; bool hit = true;
                for (int i = 0; i < 3 && hit; i++)
                {
                    if (Mathf.Abs(d[i]) < 1e-6f) { if (o[i] < w.min[i] || o[i] > w.max[i]) hit = false; continue; }
                    float a = (w.min[i] - o[i]) / d[i], b = (w.max[i] - o[i]) / d[i];
                    if (a > b) { var s = a; a = b; b = s; }
                    t0 = Mathf.Max(t0, a); t1 = Mathf.Min(t1, b);
                    if (t0 > t1) hit = false;
                }
                if (hit) best = Mathf.Min(best, t0);
            }
            return best;
        }
        static float Overlap(Bounds b, List<Bounds> walls)
        {
            float volume = Mathf.Max(1e-6f, b.size.x * b.size.y * b.size.z), inside = 0;
            foreach (var w in walls)
            {
                float dx = Mathf.Min(b.max.x, w.max.x) - Mathf.Max(b.min.x, w.min.x), dy = Mathf.Min(b.max.y, w.max.y) - Mathf.Max(b.min.y, w.min.y), dz = Mathf.Min(b.max.z, w.max.z) - Mathf.Max(b.min.z, w.min.z);
                if (dx > 0 && dy > 0 && dz > 0) inside += dx * dy * dz;
            }
            return Mathf.Clamp01(inside / volume);
        }
        static bool RendererBounds(Transform t, out Bounds b)
        {
            b = default; bool any = false;
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return any;
        }
        static Vector3 MeshCentre(Transform art, Transform space)
        {
            var sum = Vector3.zero; int n = 0;
            foreach (var f in art.GetComponentsInChildren<MeshFilter>(true))
                if (f.sharedMesh) { sum += space.InverseTransformPoint(f.transform.TransformPoint(f.sharedMesh.bounds.center)); n++; }
            return n > 0 ? sum / n : Vector3.zero;
        }
        static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude > 1e-8f ? v.normalized : Vector3.forward; }
        static string V(Vector3 v) => "(" + v.x.ToString("F2") + "," + v.y.ToString("F2") + "," + v.z.ToString("F2") + ")";

        // Back-to-wall test for a piece whose visible front is `front`: the gap between its back face and the nearest "Wall" box behind its
        // origin (three heights, so a window opening behind it does not hide the wall), the clear room in front of its front face, and the
        // share of its bounds inside wall boxes. Yaws are multiples of 90 degrees, so the world AABB is the piece's box.
        static void WallSides(Vector3 origin, Bounds b, Vector3 front, List<Bounds> walls, out float backGap, out float frontClear, out float overlap)
        {
            float fExt = 0, bExt = 0;
            for (int i = 0; i < 8; i++)
            {
                var c = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, 0, (i & 4) == 0 ? b.min.z : b.max.z);
                float d = Vector3.Dot(c - new Vector3(origin.x, 0, origin.z), front);
                if (i == 0) { fExt = d; bExt = -d; } else { fExt = Mathf.Max(fExt, d); bExt = Mathf.Max(bExt, -d); }
            }
            float back = float.PositiveInfinity, ahead = float.PositiveInfinity;
            foreach (float k in new[] { .15f, .5f, .85f })
            {
                var p = new Vector3(origin.x, Mathf.Lerp(b.min.y, b.max.y, k), origin.z);
                back = Mathf.Min(back, Ray(p, -front, walls)); ahead = Mathf.Min(ahead, Ray(p, front, walls));
            }
            backGap = back - bExt; frontClear = ahead - fExt; overlap = Overlap(b, walls);
        }

        public const float MaxBackGap = .6f, MinFrontClear = .3f, MaxWallOverlap = .05f;

        // One HC_KIT_AXIS line per placed old-kit piece (per asset for aggregated entries) and HC_KIT_AXIS_SUMMARY. Per-instance lines are
        // written in the editor (prepare) and under -hc-verify; a player only logs failures and the summary.
        public static void Log(Transform root)
        {
            if (!root) return;
            bool verbose = Application.isEditor || NativeVerification.Requested;
            var walls = WallBoxes(root);
            var index = new Dictionary<string, int>(); var aggregated = new SortedDictionary<string, int>();
            int n = 0, turned = 0, wallBacked = 0, problems = 0, aggregateCount = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith(Prefix, StringComparison.Ordinal)) continue;
                string art = t.name.Substring(Prefix.Length);
                if (!Kit.TryGetValue(art, out var e)) continue;
                if (e.Aggregate) { aggregated[art] = (aggregated.TryGetValue(art, out var c) ? c : 0) + 1; aggregateCount++; continue; }
                int k = index[art] = (index.TryGetValue(art, out var i) ? i : 0) + 1;
                var model = TurnedModel(t, art); bool isTurned = model;
                RendererBounds(t, out var b);
                bool ok = true; string expected, extra = "";
                var front = Vector3.zero;
                switch (e.Policy)
                {
                    case Policy.Hinged:
                    {
                        var c = t.parent ? MeshCentre(t, t.parent) : Vector3.zero;
                        ok = t.parent && c.x > 0; expected = "leaf_extends_+X_from_hinge"; extra = " hingeCentre=" + V(c);
                        break;
                    }
                    case Policy.Adaptive:
                    {
                        if (art == "Drawbridge") { var c = t.parent ? MeshCentre(t, t.parent) : Vector3.zero; ok = t.parent && c.z < 0; expected = "deck_extends_-Z_from_hinge"; extra = " hingeCentre=" + V(c); }
                        else { var c = MeshCentre(t, root); ok = c.z < -.45f; expected = "chains_over_the_moat"; extra = " centre=" + V(c); }
                        break;
                    }
                    default:
                    {
                        // visible front: the model's import front, unless the placed model is a re-export that already faces +Z
                        var src = isTurned ? model : t;
                        int sign = e.ImportFront;
                        if (!isTurned && Classify(t, e, out _) == Match.Contract) sign = 1;
                        front = Flat(src.TransformDirection(new Vector3(0, 0, sign)));
                        var layout = Flat(t.forward);
                        bool faces = Vector3.Dot(front, layout) > .7f;
                        if (e.Policy == Policy.Turn) { ok = faces; expected = e.Check == Check.WallBacked ? "front_to_room_back_to_wall" : "front_along_layout_yaw"; }
                        else expected = e.Check == Check.WallBacked ? "front_to_room_back_to_wall_as_imported" : e.Policy == Policy.Symmetric ? "symmetric_under_180" : "as_imported";
                        if (e.Check == Check.WallBacked)
                        {
                            WallSides(t.position, b, front, walls, out float backGap, out float frontClear, out float overlap);
                            ok &= backGap <= MaxBackGap && frontClear >= MinFrontClear && overlap <= MaxWallOverlap;
                            extra = " backGap=" + backGap.ToString("F2") + " frontClear=" + frontClear.ToString("F2") + " wallOverlap=" + overlap.ToString("F2");
                            wallBacked++;
                        }
                        extra = " facesLayout=" + faces + extra;
                        break;
                    }
                }
                n++; if (isTurned) turned++; if (!ok) problems++;
                if (verbose || !ok)
                    Debug.Log("HC_KIT_AXIS name=" + art + "#" + k + " art=" + art + " policy=" + e.Policy + " check=" + e.Check + " turned=" + isTurned + " pos=" + V(t.position) +
                              (e.Policy == Policy.Hinged || e.Policy == Policy.Adaptive ? "" : " front=" + V(front)) + " bounds=" + V(b.center) + "/" + V(b.size) + extra +
                              " expected=" + expected + " ok=" + ok);
            }
            if (verbose)
                foreach (var kv in aggregated)
                    Debug.Log("HC_KIT_AXIS name=" + kv.Key + " art=" + kv.Key + " policy=" + Kit[kv.Key].Policy + " instances=" + kv.Value + " expected=" +
                              (Kit[kv.Key].Policy == Policy.Symmetric ? "symmetric_under_180" : "as_imported") + " ok=True");
            Debug.Log("HC_KIT_AXIS_SUMMARY instances=" + n + " aggregated=" + aggregateCount + " turned=" + turned + " wallBacked=" + wallBacked + " problems=" + problems);
        }
    }
}
