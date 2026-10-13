using System.Collections.Generic;
using UnityEngine;

namespace HollowCurrent
{
    // Data for the Haunted Castle: rooms, doors, windows, candelabra and the art-name contract.
    // Coordinates are castle-local (the root passed to CastleWorld.Build). Footprint x +-14, z 0..28,
    // the gate wall is z = 0 and the outside world lies at z < 0.
    public enum CastleDoorKind { Standard, Great, Service, Gate }

    public enum CastleRoom
    {
        Outside, Gatehouse, Courtyard, GreatHall, DiningHall, Kitchen, Chapel, Vestibule,
        Library, PortraitHall, Bedchamber, Balcony, Observatory, Crypt, Reliquary, ServiceStair, PortraitCloset
    }

    public readonly struct CastleRoomDef
    {
        public readonly CastleRoom Id;
        public readonly string Name;
        public readonly float Floor, Ceiling, X0, X1, Z0, Z1;
        public CastleRoomDef(CastleRoom id, string name, float floor, float ceiling, float x0, float x1, float z0, float z1)
        { Id = id; Name = name; Floor = floor; Ceiling = ceiling; X0 = x0; X1 = x1; Z0 = z0; Z1 = z1; }
        public bool Contains(Vector3 p) => p.x >= X0 && p.x <= X1 && p.z >= Z0 && p.z <= Z1 && p.y >= Floor - 0.3f && p.y <= Ceiling;
        public Vector3 Center => new Vector3((X0 + X1) * .5f, Floor + .1f, (Z0 + Z1) * .5f);
    }

    public readonly struct CastleDoorDef
    {
        public readonly string Name;
        public readonly bool RunsAlongX;   // true: wall runs along X at z = Line; false: wall runs along Z at x = Line
        public readonly float Line, Center, FloorY;
        public readonly CastleDoorKind Kind;
        public readonly CastleRoom From, To;
        public CastleDoorDef(string name, bool runsAlongX, float line, float center, float floorY, CastleDoorKind kind, CastleRoom from, CastleRoom to)
        { Name = name; RunsAlongX = runsAlongX; Line = line; Center = center; FloorY = floorY; Kind = kind; From = from; To = to; }
        public float Width => CastleLayout.DoorWidth(Kind);
        public float Height => CastleLayout.DoorHeight(Kind);
        public bool IsService => Kind == CastleDoorKind.Service;
        public Vector3 Position => RunsAlongX ? new Vector3(Center, FloorY, Line) : new Vector3(Line, FloorY, Center);
    }

    public readonly struct CastleWindowDef
    {
        public readonly bool RunsAlongX;
        public readonly float Line, Center, FloorY, Width, Sill, Top;
        public readonly int Inward;        // +1 / -1: direction of the room along the wall normal
        public readonly string Art;
        public readonly bool Light, Shadows;
        public CastleWindowDef(bool runsAlongX, float line, float center, float floorY, float width, float sill, float top, int inward, string art, bool light, bool shadows = false)
        { RunsAlongX = runsAlongX; Line = line; Center = center; FloorY = floorY; Width = width; Sill = sill; Top = top; Inward = inward; Art = art; Light = light; Shadows = shadows; }
    }

    public readonly struct CastleCandleDef
    {
        public readonly Vector3 Pos;       // base of the stand (floor or table top)
        public readonly float Intensity, Range, LightHeight;
        public readonly bool Art, Shadows;
        public CastleCandleDef(Vector3 pos, float intensity, float range = 5.5f, bool shadows = false, bool art = true, float lightHeight = 1.8f)
        { Pos = pos; Intensity = intensity; Range = range; Shadows = shadows; Art = art; LightHeight = lightHeight; }
    }

    public static class CastleLayout
    {
        // ---- constants -------------------------------------------------------------------------
        public const float FloorCrypt = -3.2f, FloorGround = 0f, FloorUpper = 3.2f, FloorTower = 6.4f;
        public const float HalfWidth = 14f, Depth = 28f;
        public const float HallCeiling = 9f;
        public const float StandardDoorW = 1.5f, StandardDoorH = 2.0f;
        public const float GreatDoorW = 3.0f, GreatDoorH = 4.5f;
        public const float ServiceDoorW = 0.9f, ServiceDoorH = 2.0f;
        public const string ServiceDoorPrefix = "Service door / ";
        public const string StandardDoorPrefix = "Door / ";
        public const string GreatDoorPrefix = "Great door / ";

        public static float DoorWidth(CastleDoorKind k) => k == CastleDoorKind.Great || k == CastleDoorKind.Gate ? GreatDoorW : k == CastleDoorKind.Service ? ServiceDoorW : StandardDoorW;
        public static float DoorHeight(CastleDoorKind k) => k == CastleDoorKind.Great || k == CastleDoorKind.Gate ? GreatDoorH : k == CastleDoorKind.Service ? ServiceDoorH : StandardDoorH;

        // ---- art contract with piece 2 (hc/art) -------------------------------------------------
        // CastleArt.Place(name, parent, localPos, rot): origin at the BASE CENTRE of the object, front faces +Z
        // at yaw 0. Nominal sizes are in ArtSizes (x = width, y = height, z = depth) and are what the layout reserves.
        public static class ArtNames
        {
            // gate and exterior
            public const string GateArch = "GateArch";               // frames the 3.0 x 4.5 gate opening, centred on the wall line
            public const string Drawbridge = "Drawbridge";           // origin = hinge line on the top surface, extends toward -Z (lowered pose)
            public const string Winch = "Winch";
            public const string LanternCart = "LanternCart";
            public const string Buttress = "Buttress";
            public const string Battlement = "Battlement";
            // openings
            public const string DoorFrame = "DoorFrame";             // around a 1.5 x 2.0 opening, centred on the wall
            public const string GreatDoorFrame = "GreatDoorFrame";   // around a 3.0 x 4.5 opening
            public const string ServiceDoorFrame = "ServiceDoorFrame"; // around a 0.9 x 2.0 opening
            public const string DoorLeaf = "DoorLeaf";               // origin at hinge end centre of the leaf: leaf extends +X from the hinge
            public const string GreatDoorLeaf = "GreatDoorLeaf";     // one half (1.5 x 4.5) of the double door
            public const string ServiceDoorLeaf = "ServiceDoorLeaf";
            public const string PortraitDoorLeaf = "PortraitDoorLeaf"; // 0.9 x 2.0 leaf painted as a portrait, hinge at origin
            public const string LancetWindow = "LancetWindow";       // base at the sill
            public const string TallLancetWindow = "TallLancetWindow";
            public const string ClerestoryWindow = "ClerestoryWindow";
            public const string ObservatoryWindow = "ObservatoryWindow";
            public const string RoseWindow = "RoseWindow";           // base at the opening bottom
            // structure
            public const string ColumnGreat = "ColumnGreat";         // great hall column, floor to ceiling
            public const string Column = "Column";                   // 3.0 m column (crypt)
            public const string CryptRib = "CryptRib";               // vaulting rib spanning the crypt between a column pair
            public const string RafterBeam = "RafterBeam";
            public const string Balustrade = "Balustrade";           // 2.0 m rail section
            public const string Fountain = "Fountain";
            public const string DeadTree = "DeadTree";
            public const string Candelabra = "Candelabra";
            // furniture and set-dressing
            public const string Pew = "Pew";
            public const string Altar = "Altar";
            public const string ChapelBell = "ChapelBell";
            public const string LongTable = "LongTable";
            public const string DiningTable = "DiningTable";
            public const string HighBackChair = "HighBackChair";
            public const string Throne = "Throne";
            public const string Fireplace = "Fireplace";
            public const string Sideboard = "Sideboard";
            public const string KitchenHearth = "KitchenHearth";
            public const string ButcherBlock = "ButcherBlock";
            public const string Cauldron = "Cauldron";
            public const string Bookshelf = "Bookshelf";
            public const string ReadingTable = "ReadingTable";
            public const string Lectern = "Lectern";
            public const string Portrait = "Portrait";
            public const string CanopyBed = "CanopyBed";
            public const string Orrery = "Orrery";
            public const string Telescope = "Telescope";
            public const string Sarcophagus = "Sarcophagus";
            public const string CryptBridge = "CryptBridge";
            public const string ReliquaryBox = "ReliquaryBox";
            public const string Pedestal = "Pedestal";
            // surreal dressing and crypt/chapel extras (exist under Resources/Castle; origin = base centre, front +Z)
            public const string GiantSkeleton = "GiantSkeleton";     // 12 m along its own Z, head +Z, ribs reach the floor
            public const string FloatingBook = "FloatingBook";       // open book, pages up
            public const string Banner = "Banner";                   // oxblood hanging banner, origin floor centre
            public const string StoneFont = "StoneFont";
            public const string Wall2m = "Wall_2m", Wall4m = "Wall_4m";   // 0.5 m thick stone wall modules with plinth and cornice, 3.2 m tall, symmetric front/back
            public const string PointedArch = "PointedArch";             // free-standing arch on two piers, 3.9 x 5.1 m, 3.0 m clear span
            public const string VaultRib = "VaultRib";                   // pointed rib, springs at x +-2, rise 2.84, origin on the spring line
            public const string FloorTile = "FloorTile";                 // 2 x 2 m flagstone, origin centre of the top face
            public const string DrawbridgeChains = "DrawbridgeChains"; // static pair of chains from the lowered leaf's far corners up to the gate
            public const string CoffinNiche = "CoffinNiche";         // origin at the front face; recess runs back 1.2 m, the coffin projects 1.15 m
        }

        static readonly Dictionary<string, Vector3> sizes = new Dictionary<string, Vector3>
        {
            { ArtNames.GateArch, new Vector3(3.8f, 5.6f, 1.0f) }, { ArtNames.Drawbridge, new Vector3(3.2f, .25f, 6.0f) },
            { ArtNames.Winch, new Vector3(1.0f, 1.6f, .8f) }, { ArtNames.LanternCart, new Vector3(1.8f, 1.3f, 1.0f) },
            { ArtNames.Buttress, new Vector3(.9f, 6.4f, 1.2f) }, { ArtNames.Battlement, new Vector3(4.0f, 1.0f, .8f) },
            { ArtNames.DoorFrame, new Vector3(1.9f, 2.6f, .8f) }, { ArtNames.GreatDoorFrame, new Vector3(3.6f, 5.2f, .9f) },
            { ArtNames.ServiceDoorFrame, new Vector3(1.2f, 2.3f, .8f) },
            { ArtNames.DoorLeaf, new Vector3(1.5f, 2.0f, .09f) }, { ArtNames.GreatDoorLeaf, new Vector3(1.5f, 4.5f, .12f) },
            { ArtNames.ServiceDoorLeaf, new Vector3(.9f, 2.0f, .08f) }, { ArtNames.PortraitDoorLeaf, new Vector3(.9f, 2.0f, .1f) },
            { ArtNames.LancetWindow, new Vector3(1.2f, 1.6f, .35f) }, { ArtNames.TallLancetWindow, new Vector3(1.4f, 3.5f, .35f) },
            { ArtNames.ClerestoryWindow, new Vector3(1.2f, 1.8f, .35f) }, { ArtNames.ObservatoryWindow, new Vector3(1.6f, 2.7f, .35f) },
            { ArtNames.RoseWindow, new Vector3(3.0f, 3.0f, .5f) },
            { ArtNames.ColumnGreat, new Vector3(.9f, 8.8f, .9f) }, { ArtNames.Column, new Vector3(.7f, 3.0f, .7f) },
            { ArtNames.CryptRib, new Vector3(8.4f, .6f, .6f) }, { ArtNames.RafterBeam, new Vector3(11.4f, .5f, .5f) },
            { ArtNames.Balustrade, new Vector3(2.0f, 1.1f, .2f) }, { ArtNames.Fountain, new Vector3(2.6f, 1.8f, 2.6f) },
            { ArtNames.DeadTree, new Vector3(2.4f, 4.5f, 2.4f) }, { ArtNames.Candelabra, new Vector3(.5f, 1.7f, .5f) },
            { ArtNames.Pew, new Vector3(2.4f, 1.0f, .7f) }, { ArtNames.Altar, new Vector3(2.4f, 1.1f, 1.1f) },
            { ArtNames.ChapelBell, new Vector3(1.2f, 2.0f, .9f) }, { ArtNames.LongTable, new Vector3(6.6f, .95f, 1.4f) },
            { ArtNames.DiningTable, new Vector3(5.6f, .9f, 1.6f) }, { ArtNames.HighBackChair, new Vector3(.6f, 1.3f, .6f) },
            { ArtNames.Throne, new Vector3(1.0f, 1.9f, .9f) }, { ArtNames.Fireplace, new Vector3(2.6f, 2.6f, .9f) },
            { ArtNames.Sideboard, new Vector3(2.6f, 1.1f, .6f) }, { ArtNames.KitchenHearth, new Vector3(2.8f, 2.4f, 1.0f) },
            { ArtNames.ButcherBlock, new Vector3(1.5f, .9f, 1.0f) }, { ArtNames.Cauldron, new Vector3(1.0f, .9f, 1.0f) },
            { ArtNames.Bookshelf, new Vector3(2.0f, 3.0f, .5f) }, { ArtNames.ReadingTable, new Vector3(2.0f, .85f, 1.0f) },
            { ArtNames.Lectern, new Vector3(.7f, 1.2f, .6f) }, { ArtNames.Portrait, new Vector3(.9f, 1.3f, .1f) },
            { ArtNames.CanopyBed, new Vector3(2.4f, 2.8f, 2.6f) }, { ArtNames.Orrery, new Vector3(3.0f, 2.8f, 3.0f) },
            { ArtNames.Telescope, new Vector3(1.0f, 2.2f, 2.4f) }, { ArtNames.Sarcophagus, new Vector3(2.2f, 1.2f, 1.0f) },
            { ArtNames.CryptBridge, new Vector3(1.4f, .3f, 1.2f) }, { ArtNames.ReliquaryBox, new Vector3(1.0f, 1.0f, .7f) },
            { ArtNames.Pedestal, new Vector3(.6f, 1.0f, .6f) },
            { ArtNames.GiantSkeleton, new Vector3(3.4f, 3.4f, 12.0f) }, { ArtNames.FloatingBook, new Vector3(.4f, .1f, .29f) },
            { ArtNames.Banner, new Vector3(1.39f, 3.1f, .09f) }, { ArtNames.StoneFont, new Vector3(1.04f, 1.08f, 1.04f) },
            { ArtNames.CoffinNiche, new Vector3(2.2f, 1.45f, 2.6f) },
        };
        public static IReadOnlyDictionary<string, Vector3> ArtSizes => sizes;
        public static Vector3 ArtSize(string name) => sizes.TryGetValue(name, out var s) ? s : Vector3.one;

        // ---- rooms ------------------------------------------------------------------------------
        // Order matters for RoomAt: Balcony before GreatHall.
        public static readonly CastleRoomDef[] Rooms =
        {
            new CastleRoomDef(CastleRoom.Gatehouse, "Gatehouse", 0, 5f, -6, 6, 0, 6),
            new CastleRoomDef(CastleRoom.Courtyard, "Courtyard", 0, 12f, -6, 6, 6, 14),
            new CastleRoomDef(CastleRoom.Balcony, "Balcony", 3.2f, 9f, -5.7f, 5.7f, 14.3f, 27.7f),
            new CastleRoomDef(CastleRoom.GreatHall, "Great hall", 0, 9f, -6, 6, 14, 28),
            new CastleRoomDef(CastleRoom.Kitchen, "Kitchen", 0, 3f, -14, -6, 0, 10),
            new CastleRoomDef(CastleRoom.DiningHall, "Dining hall", 0, 3f, -14, -6, 10, 22),
            new CastleRoomDef(CastleRoom.Chapel, "Chapel", 0, 6.2f, 6, 14, 0, 13),
            new CastleRoomDef(CastleRoom.Vestibule, "Vestibule", 0, 3f, 6, 14, 13, 28),
            new CastleRoomDef(CastleRoom.Bedchamber, "Count's bedchamber", 3.2f, 6.2f, -14, -6, 0, 10),
            new CastleRoomDef(CastleRoom.Library, "Library gallery", 3.2f, 6.2f, -14, -6, 10, 22),
            new CastleRoomDef(CastleRoom.PortraitCloset, "Portrait closet", 3.2f, 6.2f, 11, 13.55f, 13.3f, 15.5f),
            new CastleRoomDef(CastleRoom.PortraitHall, "Portrait hall", 3.2f, 6.2f, 6, 14, 13, 28),
            new CastleRoomDef(CastleRoom.Observatory, "Observatory", 6.4f, 11.4f, 6, 14, 19.4f, 28),
            new CastleRoomDef(CastleRoom.Crypt, "Crypt", -3.2f, -.2f, -6, 6, 14, 28),
            new CastleRoomDef(CastleRoom.Reliquary, "Reliquary", -3.2f, -.2f, 6, 14, 16, 26),
            new CastleRoomDef(CastleRoom.ServiceStair, "Service stair", -3.2f, 6.2f, -14, -6, 22, 28),
        };

        public static CastleRoom RoomAt(Vector3 p)
        {
            foreach (var r in Rooms) if (r.Contains(p)) return r.Id;
            return CastleRoom.Outside;
        }
        public static CastleRoomDef Room(CastleRoom id) { foreach (var r in Rooms) if (r.Id == id) return r; return default; }

        // ---- doors ------------------------------------------------------------------------------
        public static readonly CastleDoorDef[] Doors =
        {
            new CastleDoorDef("Gate", true, 0, 0, 0, CastleDoorKind.Gate, CastleRoom.Outside, CastleRoom.Gatehouse),
            new CastleDoorDef("Gatehouse great doors", true, 6, 0, 0, CastleDoorKind.Great, CastleRoom.Gatehouse, CastleRoom.Courtyard),
            new CastleDoorDef("Gatehouse kitchen passage", false, -6, 3, 0, CastleDoorKind.Service, CastleRoom.Gatehouse, CastleRoom.Kitchen),
            new CastleDoorDef("Gatehouse guard door", false, 6, 3, 0, CastleDoorKind.Standard, CastleRoom.Gatehouse, CastleRoom.Chapel),
            new CastleDoorDef("Kitchen courtyard door", false, -6, 8, 0, CastleDoorKind.Standard, CastleRoom.Kitchen, CastleRoom.Courtyard),
            new CastleDoorDef("Dining courtyard door", false, -6, 12, 0, CastleDoorKind.Standard, CastleRoom.DiningHall, CastleRoom.Courtyard),
            new CastleDoorDef("Chapel great doors", false, 6, 9, 0, CastleDoorKind.Great, CastleRoom.Chapel, CastleRoom.Courtyard),
            new CastleDoorDef("Hall great doors", true, 14, 0, 0, CastleDoorKind.Great, CastleRoom.Courtyard, CastleRoom.GreatHall),
            new CastleDoorDef("Kitchen dining door", true, 10, -10, 0, CastleDoorKind.Standard, CastleRoom.Kitchen, CastleRoom.DiningHall),
            new CastleDoorDef("Dining hall door", false, -6, 16, 0, CastleDoorKind.Standard, CastleRoom.DiningHall, CastleRoom.GreatHall),
            new CastleDoorDef("Dining stair door", true, 22, -8, 0, CastleDoorKind.Service, CastleRoom.DiningHall, CastleRoom.ServiceStair),
            new CastleDoorDef("Hall stair door", false, -6, 25, 0, CastleDoorKind.Service, CastleRoom.GreatHall, CastleRoom.ServiceStair),
            new CastleDoorDef("Chapel vestibule door", true, 13, 10, 0, CastleDoorKind.Standard, CastleRoom.Chapel, CastleRoom.Vestibule),
            new CastleDoorDef("Vestibule hall door", false, 6, 16, 0, CastleDoorKind.Standard, CastleRoom.Vestibule, CastleRoom.GreatHall),
            new CastleDoorDef("Bedchamber library door", true, 10, -10, 3.2f, CastleDoorKind.Standard, CastleRoom.Bedchamber, CastleRoom.Library),
            new CastleDoorDef("Library balcony door", false, -6, 16, 3.2f, CastleDoorKind.Standard, CastleRoom.Library, CastleRoom.Balcony),
            new CastleDoorDef("Library stair door", true, 22, -8, 3.2f, CastleDoorKind.Service, CastleRoom.Library, CastleRoom.ServiceStair),
            new CastleDoorDef("Portrait balcony door", false, 6, 16, 3.2f, CastleDoorKind.Standard, CastleRoom.PortraitHall, CastleRoom.Balcony),
            new CastleDoorDef("Portrait door", false, 11, 14.4f, 3.2f, CastleDoorKind.Service, CastleRoom.PortraitHall, CastleRoom.PortraitCloset),
            new CastleDoorDef("Crypt stair door", false, -6, 25, -3.2f, CastleDoorKind.Service, CastleRoom.Crypt, CastleRoom.ServiceStair),
            new CastleDoorDef("Crypt reliquary door", false, 6, 21, -3.2f, CastleDoorKind.Standard, CastleRoom.Crypt, CastleRoom.Reliquary),
        };

        // The hunter AI reads this: a service door is 0.9 m wide and he must land and walk through it.
        public static bool IsServiceDoor(Door d) => d && d.name.StartsWith(ServiceDoorPrefix);
        public static string DoorPrefix(CastleDoorKind k) => k == CastleDoorKind.Service ? ServiceDoorPrefix : k == CastleDoorKind.Great ? GreatDoorPrefix : StandardDoorPrefix;

        // ---- windows ----------------------------------------------------------------------------
        // Exterior walls plus courtyard-facing walls. Inward is the direction of the room along the wall normal.
        public static readonly CastleWindowDef[] Windows =
        {
            // west exterior (x = -14, room to +x)
            new CastleWindowDef(false, -14, 5, 0, 1.2f, 1.0f, 2.6f, 1, ArtNames.LancetWindow, true),
            new CastleWindowDef(false, -14, 13.5f, 0, 1.2f, 1.0f, 2.6f, 1, ArtNames.LancetWindow, true),
            new CastleWindowDef(false, -14, 18.5f, 0, 1.2f, 1.0f, 2.6f, 1, ArtNames.LancetWindow, false),
            new CastleWindowDef(false, -14, 5, 3.2f, 1.2f, 1.0f, 2.6f, 1, ArtNames.LancetWindow, true),
            new CastleWindowDef(false, -14, 13.5f, 3.2f, 1.2f, 1.0f, 2.6f, 1, ArtNames.LancetWindow, true),
            new CastleWindowDef(false, -14, 18.5f, 3.2f, 1.2f, 1.0f, 2.6f, 1, ArtNames.LancetWindow, false),
            // south exterior (z = 0, room to +z)
            new CastleWindowDef(true, 0, -10, 0, 1.2f, 1.0f, 2.6f, 1, ArtNames.LancetWindow, false),
            new CastleWindowDef(true, 0, -10, 3.2f, 1.2f, 1.0f, 2.6f, 1, ArtNames.LancetWindow, false),
            new CastleWindowDef(true, 0, 8.5f, 0, 1.4f, 1.4f, 4.9f, 1, ArtNames.TallLancetWindow, true),
            new CastleWindowDef(true, 0, 11.5f, 0, 1.4f, 1.4f, 4.9f, 1, ArtNames.TallLancetWindow, false),
            // east exterior (x = 14, room to -x)
            new CastleWindowDef(false, 14, 3.5f, 0, 1.4f, 1.4f, 4.9f, -1, ArtNames.TallLancetWindow, true),
            new CastleWindowDef(false, 14, 9.5f, 0, 1.4f, 1.4f, 4.9f, -1, ArtNames.TallLancetWindow, true),
            new CastleWindowDef(false, 14, 18, 3.2f, 1.2f, 1.0f, 2.6f, -1, ArtNames.LancetWindow, true),
            new CastleWindowDef(false, 14, 21, 6.4f, 1.6f, .9f, 3.6f, -1, ArtNames.ObservatoryWindow, true),
            new CastleWindowDef(false, 14, 25, 6.4f, 1.6f, .9f, 3.6f, -1, ArtNames.ObservatoryWindow, false),
            // north exterior (z = 28, room to -z)
            new CastleWindowDef(true, 28, -4.4f, 0, 1.4f, 1.8f, 5.4f, -1, ArtNames.TallLancetWindow, true),
            new CastleWindowDef(true, 28, 4.4f, 0, 1.4f, 1.8f, 5.4f, -1, ArtNames.TallLancetWindow, true),
            new CastleWindowDef(true, 28, 0, 0, 3.0f, 5.6f, 8.6f, -1, ArtNames.RoseWindow, true, true),
            new CastleWindowDef(true, 28, 9, 6.4f, 1.4f, .9f, 3.6f, -1, ArtNames.ObservatoryWindow, true),
            new CastleWindowDef(true, 28, 12, 6.4f, 1.4f, .9f, 3.6f, -1, ArtNames.ObservatoryWindow, false),
            // courtyard-facing
            new CastleWindowDef(false, -6, 8, 3.2f, 1.2f, 1.0f, 2.6f, -1, ArtNames.LancetWindow, true),
            new CastleWindowDef(false, -6, 12, 3.2f, 1.2f, 1.0f, 2.6f, -1, ArtNames.LancetWindow, false),
            new CastleWindowDef(false, 6, 12, 0, 1.2f, 1.5f, 5.0f, 1, ArtNames.TallLancetWindow, false),
            // great hall clerestory (above the wing roofs, looking outside)
            new CastleWindowDef(false, -6, 17.5f, 0, 1.2f, 6.8f, 8.6f, 1, ArtNames.ClerestoryWindow, false),
            new CastleWindowDef(false, -6, 21.5f, 0, 1.2f, 6.8f, 8.6f, 1, ArtNames.ClerestoryWindow, true, true),
            new CastleWindowDef(false, -6, 25, 0, 1.2f, 6.8f, 8.6f, 1, ArtNames.ClerestoryWindow, false),
            new CastleWindowDef(false, 6, 17.5f, 0, 1.2f, 6.8f, 8.6f, -1, ArtNames.ClerestoryWindow, false),
            new CastleWindowDef(false, 6, 21.5f, 0, 1.2f, 6.8f, 8.6f, -1, ArtNames.ClerestoryWindow, true, true),
            new CastleWindowDef(false, 6, 25, 0, 1.2f, 6.8f, 8.6f, -1, ArtNames.ClerestoryWindow, false),
        };


        // ---- dressing data (round 2) ---------------------------------------------------------------
        // Great hall, round 6 (owner: the skeleton in the middle of the entrance looked stupid and uncentred): the centre axis from the
        // great doors (x = 0) is clean: rune ring, banquet table, throne and rose window all sit on x = 0. The giant skeleton lies
        // along the west colonnade against the wall, in the 1.35 m strip between the wall relief (x -5.57) and the columns' west
        // faces (x -4.35): its head (+Y in the art imports as local -Z, so yaw 0 points it at the entrance) at z about 17.7,
        // tail into the back wall. Squeezed to 0.30 x laterally (ribs +-0.62 m), 0.82 x in height (2.8 m, under the 3.0 m gallery
        // slab) and 0.9 x in length (10.8 m). Its bounds never reach x -3.
        public static readonly Vector3 SkeletonPos = new Vector3(-5.0f, 0.05f, 22.4f);
        public const float SkeletonYaw = 0f;
        public static readonly Vector3 SkeletonScaleVec = new Vector3(.30f, .82f, .90f);   // local x lateral, y height, z length
        public const float SkeletonMaxX = -3.0f;
        public static readonly Vector3 TablePos = new Vector3(0f, 0, 24.65f);
        public const float TableYaw = 90f;         // length axis along the hall axis (z)
        public const float TableLengthScale = .66f, TableWidthScale = 1f;

        // Camera lines the dressing must stay clear of (round 4): S5 looks from the hall floor up at the Count's perch,
        // S6 stands in the gatehouse. Nothing that floats may sit within 2 m of the eye-to-subject segment.
        public static readonly Vector3 S5Eye = new Vector3(-0.41f, 3.62f, 19.37f), S5Subject = new Vector3(-3.5f, 4.4f, 20.7f);
        public static readonly Vector3 S6Eye = new Vector3(-0.30f, 1.15f, 1.00f), S6Subject = new Vector3(-0.9f, 1.2f, 4.4f);
        public static bool NearCaptureLine(Vector3 p, float radius)
        {
            return SegmentDistance(p, S5Eye, S5Subject) < radius || SegmentDistance(p, S6Eye, S6Subject) < radius;
        }
        static float SegmentDistance(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = b - a; float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
            return Vector3.Distance(p, a + ab * t);
        }

        // Floating candelabra (centre of the stand) and books; hall-local. Under the galleries (|x| > 3, y < 3.2) nothing hangs.
        public static readonly Vector3[] HallCandelabras =
        {
            new Vector3(-2.3f, 2.7f, 17.2f), new Vector3(2.4f, 3.0f, 17.8f), new Vector3(-1.2f, 4.8f, 16.5f), new Vector3(1.4f, 5.4f, 18.9f),
            new Vector3(-1.8f, 3.3f, 15.8f), new Vector3(2.5f, 4.4f, 22.1f), new Vector3(-0.4f, 6.0f, 20.5f), new Vector3(-4.8f, 6.2f, 17.5f),
            new Vector3(4.6f, 5.4f, 16.8f), new Vector3(-3.9f, 5.8f, 23.4f), new Vector3(3.9f, 5.6f, 24.2f), new Vector3(0.3f, 3.5f, 23.1f),
            new Vector3(2.0f, 6.4f, 24.0f), new Vector3(-2.1f, 6.2f, 25.0f),
        };
        // Books sit east of the S5 line (hall floor to the Count's perch) and above 2.7 m; none within 2 m of an S5/S6 camera line.
        public static readonly Vector3[] HallBooks =
        {
            new Vector3(2.0f, 3.3f, 17.5f), new Vector3(3.0f, 4.3f, 19.2f), new Vector3(1.2f, 5.2f, 21.0f), new Vector3(2.6f, 3.6f, 21.5f),
            new Vector3(-2.2f, 3.1f, 16.8f), new Vector3(0.3f, 4.6f, 17.6f), new Vector3(-0.5f, 5.6f, 22.4f), new Vector3(3.2f, 5.2f, 23.0f),
            new Vector3(1.8f, 2.8f, 19.5f), new Vector3(-2.6f, 5.6f, 24.0f),
        };

        public readonly struct CastleBannerDef
        {
            public readonly Vector3 Pos; public readonly float Yaw;   // Pos = bottom centre of the cloth
            public CastleBannerDef(float x, float y, float z, float yaw) { Pos = new Vector3(x, y, z); Yaw = yaw; }
        }
        // Hung on the wall faces between the clerestory windows (west/east) and flanking the rose window (north).
        public static readonly CastleBannerDef[] HallBanners =
        {
            new CastleBannerDef(-5.45f, 3.9f, 19.5f, 90), new CastleBannerDef(-5.45f, 3.9f, 23.3f, 90),
            new CastleBannerDef(5.45f, 3.9f, 19.5f, -90), new CastleBannerDef(5.45f, 3.9f, 23.3f, -90),
            new CastleBannerDef(-2.4f, 3.9f, 27.35f, 180), new CastleBannerDef(2.4f, 3.9f, 27.35f, 180),
        };

        // Chapel: knocked-over pews. Roll is about the pew's own length axis; the top leans toward +x for positive roll.
        public readonly struct CastlePewDef
        {
            public readonly float X, Z, Yaw, Roll;
            public CastlePewDef(float x, float z, float yaw, float roll) { X = x; Z = z; Yaw = yaw; Roll = roll; }
        }
        public static readonly CastlePewDef[] ChapelFallenPews =
        {
            new CastlePewDef(11.0f, 1.7f, 90, 78), new CastlePewDef(8.4f, 4.2f, 84, -76),
            new CastlePewDef(9.7f, 11.3f, 100, -70), new CastlePewDef(11.0f, 8.8f, 76, -62),
        };
        public static bool IsFallenPew(float x, float z) { foreach (var p in ChapelFallenPews) if (Mathf.Abs(p.X - x) < .01f && Mathf.Abs(p.Z - z) < .01f) return true; return false; }
        public static readonly Vector3 FontPos = new Vector3(7.55f, 0, 6.95f);
        public static readonly Vector3 BrazierPos = new Vector3(12.6f, 0, 11.2f);

        // Crypt: coffin niches cut into the walls. Inward is the room side along the wall normal.
        public const float NicheWidth = 1.1f, NicheSill = .35f, NicheTop = 1.15f;
        public readonly struct CastleNicheDef
        {
            public readonly bool RunsAlongX;
            public readonly float Line, Center, FloorY, Thickness;
            public readonly int Inward;
            public CastleNicheDef(bool runsAlongX, float line, float center, int inward, float thickness, float floorY = FloorCrypt)
            { RunsAlongX = runsAlongX; Line = line; Center = center; Inward = inward; Thickness = thickness; FloorY = floorY; }
            // The niche block sits behind the wall; its front face is flush with the wall's outer face, the coffin comes through the cut.
            public Vector3 Origin => RunsAlongX ? new Vector3(Center, FloorY, Line - Inward * Thickness * .5f) : new Vector3(Line - Inward * Thickness * .5f, FloorY, Center);
            public float Yaw => RunsAlongX ? (Inward > 0 ? 0 : 180) : (Inward > 0 ? 90 : -90);
        }
        public static readonly CastleNicheDef[] Niches =
        {
            // west wall x = -6 (room to +x): clear of the stair door at z 25 and the stair room beyond z 22
            new CastleNicheDef(false, -6, 15.4f, 1, .6f), new CastleNicheDef(false, -6, 17.8f, 1, .6f), new CastleNicheDef(false, -6, 20.2f, 1, .6f),
            // south wall z = 14 (room to +z)
            new CastleNicheDef(true, 14, -4.6f, 1, .6f), new CastleNicheDef(true, 14, -2.3f, 1, .6f), new CastleNicheDef(true, 14, 0f, 1, .6f),
            new CastleNicheDef(true, 14, 2.3f, 1, .6f), new CastleNicheDef(true, 14, 4.6f, 1, .6f),
            // north wall z = 28 (room to -z), the thick exterior wall
            new CastleNicheDef(true, 28, -4.6f, -1, .9f), new CastleNicheDef(true, 28, -2.3f, -1, .9f), new CastleNicheDef(true, 28, 0f, -1, .9f),
            new CastleNicheDef(true, 28, 2.3f, -1, .9f), new CastleNicheDef(true, 28, 4.6f, -1, .9f),
        };

        // Moons painted into the openings of the rose window and the chapel windows, seen from the capture positions.
        public static readonly Vector3 RoseWindowEye = new Vector3(0, 1.64f, 14.7f);
        public static readonly Vector3 ChapelEye = new Vector3(7.0f, 1.64f, 6.5f);

        // ---- candelabra -------------------------------------------------------------------------
        // Warm point lights as in the house (intensity .5 - 1.2, range 5.5); only a few cast shadows.
        public static readonly CastleCandleDef[] Candles =
        {
            // gatehouse
            new CastleCandleDef(new Vector3(-4.8f, 0, 4.6f), .8f), new CastleCandleDef(new Vector3(4.8f, 0, 4.8f), .8f),
            // courtyard (dim)
            new CastleCandleDef(new Vector3(-4.6f, 0, 7.6f), .6f), new CastleCandleDef(new Vector3(4.6f, 0, 12.4f), .6f),
            // great hall
            new CastleCandleDef(new Vector3(-3.4f, 0, 20.1f), 1.0f, 7f, true), new CastleCandleDef(new Vector3(3.4f, 0, 20.1f), 1.0f, 7f, true),
            new CastleCandleDef(new Vector3(-3.4f, 0, 16.9f), .9f, 7f), new CastleCandleDef(new Vector3(3.4f, 0, 16.9f), .9f, 7f),
            new CastleCandleDef(new Vector3(0f, .95f, 23.6f), .8f), new CastleCandleDef(new Vector3(0f, .95f, 25.7f), .8f),
            // balcony
            new CastleCandleDef(new Vector3(-4.4f, 3.2f, 16.0f), .6f), new CastleCandleDef(new Vector3(4.4f, 3.2f, 24.0f), .6f),
            // dining hall
            new CastleCandleDef(new Vector3(-10f, .9f, 14.0f), .9f), new CastleCandleDef(new Vector3(-10f, .9f, 18.0f), .9f),
            // kitchen
            new CastleCandleDef(new Vector3(-12.4f, 0, 8.6f), .8f), new CastleCandleDef(new Vector3(-12.2f, .2f, 5.0f), 1.1f, 5.5f, false, false, 1.0f),
            // chapel
            new CastleCandleDef(new Vector3(12.7f, .32f, 4.8f), .9f, 6.5f), new CastleCandleDef(new Vector3(12.7f, .32f, 8.2f), .9f, 6.5f),
            new CastleCandleDef(new Vector3(8.0f, 0, 1.2f), .6f),
            // vestibule
            new CastleCandleDef(new Vector3(12.5f, 0, 15.5f), .8f), new CastleCandleDef(new Vector3(12.5f, 0, 26.5f), .6f),
            // library
            new CastleCandleDef(new Vector3(-10.5f, 4.05f, 16.8f), .8f), new CastleCandleDef(new Vector3(-8.0f, 3.2f, 20.6f), .6f), new CastleCandleDef(new Vector3(-12.6f, 3.2f, 11.2f), .6f),
            // bedchamber
            new CastleCandleDef(new Vector3(-8.0f, 3.2f, 2.0f), .7f), new CastleCandleDef(new Vector3(-10.2f, 3.2f, 8.8f), .7f),
            // portrait hall
            new CastleCandleDef(new Vector3(9.8f, 3.2f, 17.0f), .7f), new CastleCandleDef(new Vector3(9.8f, 3.2f, 21.0f), .7f), new CastleCandleDef(new Vector3(13.1f, 3.2f, 13.9f), .5f, 3.5f),
            // observatory
            new CastleCandleDef(new Vector3(7.3f, 6.4f, 21.0f), .7f), new CastleCandleDef(new Vector3(8.0f, 6.4f, 27.0f), .6f),
            // crypt (dimmer, as the house basement)
            new CastleCandleDef(new Vector3(-4.5f, -3.2f, 19.0f), .55f), new CastleCandleDef(new Vector3(4.6f, -3.2f, 20.0f), .55f),
            new CastleCandleDef(new Vector3(-2.4f, -3.2f, 15.2f), .5f), new CastleCandleDef(new Vector3(2.4f, -3.2f, 15.2f), .5f), new CastleCandleDef(new Vector3(0, -3.2f, 26.8f), .5f),
            // reliquary
            new CastleCandleDef(new Vector3(7.2f, -3.2f, 17.0f), .5f), new CastleCandleDef(new Vector3(7.2f, -3.2f, 25.0f), .5f),
            new CastleCandleDef(new Vector3(13.0f, -3.2f, 18.8f), .55f), new CastleCandleDef(new Vector3(13.0f, -3.2f, 23.2f), .55f),
            // service stair
            new CastleCandleDef(new Vector3(-7.2f, -3.2f, 25.6f), .45f), new CastleCandleDef(new Vector3(-7.2f, 0, 25.4f), .5f), new CastleCandleDef(new Vector3(-7.0f, 3.2f, 24.5f), .5f),
        };
    }
}
