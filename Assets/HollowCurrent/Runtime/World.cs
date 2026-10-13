using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace HollowCurrent
{
    public sealed class World
    {
        public readonly List<Light> Lamps = new List<Light>();
        public readonly List<Light> WindowLights = new List<Light>();
        bool ownsMaterials=true;
        public readonly List<Transform> Dolls = new List<Transform>();
        public readonly List<Vector3> SearchPoints = new List<Vector3>();
        public readonly List<Door> Doors = new List<Door>();
        public readonly List<Interactable> HidingPlaces=new List<Interactable>();
        public readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        public Transform Root;
        public Transform Trace;
        public Vector3 Anchor = new Vector3(5.8f, -2.2f, 11.5f);
        public Vector3 Induction = new Vector3(-5.8f, 1.1f, 10.5f);
        public Vector3 Voice = new Vector3(5.8f, -1.9f, 11.5f);
        public Vector3 Print = new Vector3(-7.84f, 4.65f, 4.6f); // Clear wall beyond the curtain folds.
        public NavMeshData NavData;
        NavMeshDataInstance navInstance;
        // Which map this world holds. Every map-specific coordinate that Game,
        // Investigator and Presence read lives on this instance.
        public MapId Map {get;private set;}=MapId.Bellwether;
        public static readonly Vector3 HouseSpawn = new Vector3(0, .1f, -11.4f);
        public Vector3 Spawn = HouseSpawn;
        // Where a hunt begins when no scare chose an origin (Bellwether: basement source room).
        public Vector3 HunterSpawn = new Vector3(5,-3.05f,9.5f);
        // Floor the anchor stands on; the containment pose stands the hunter there.
        public float AnchorFloor = -3.2f;
        public readonly List<Vector3> PerchPoints = new List<Vector3>();
        // Castle contract points (valid when Map==Castle): altar, relics, drawbridge.
        // A serializable copy of CastleWorld.Points (whose lists/nav fields Unity
        // cannot serialize), stored in the prepared scene's WorldLayout.
        [System.Serializable]
        public sealed class CastleSpots
        {
            public Vector3 Spawn,Anchor,Compass,Voice,Print,HunterSpawn,AltarPosition,DrawbridgeWinch,RuneRingCenter,StairLoopReturn;
            public float SpawnYaw,RuneRingRadius;
            public Bounds Gatehouse,DrawbridgeTrigger;
            public Vector3[] PerchPoints,RelicPositions;
            public CastleDrawbridge Drawbridge;
            public Transform StairLoop,RuneRingAnchor,KnifeOrbitAnchor,AltarAnchor;
            public Door[] ServiceDoors;
            public static CastleSpots From(CastleWorld.Points p)=>new CastleSpots{Spawn=p.Spawn,SpawnYaw=p.SpawnYaw,Gatehouse=p.GatehouseSafeBounds,Anchor=p.Anchor,Compass=p.Compass,Voice=p.Voice,Print=p.Print,
                HunterSpawn=p.HunterSpawn,PerchPoints=p.PerchPoints,AltarPosition=p.AltarPosition,RelicPositions=p.RelicPositions,DrawbridgeTrigger=p.DrawbridgeTrigger,DrawbridgeWinch=p.DrawbridgeWinch,
                Drawbridge=p.Drawbridge,RuneRingCenter=p.RuneRingCenter,RuneRingRadius=p.RuneRingRadius,RuneRingAnchor=p.RuneRingAnchor,KnifeOrbitAnchor=p.KnifeOrbitAnchor,AltarAnchor=p.AltarAnchor,
                StairLoop=p.StairLoop,StairLoopReturn=p.StairLoopReturn,ServiceDoors=p.ServiceDoors!=null?p.ServiceDoors.ToArray():new Door[0]};
        }
        public CastleSpots Castle;
        // Plan-view rectangles with strict bounds; y is ignored, exactly as the
        // original Investigator.Inside / InVan tests.
        public struct Area
        {
            public float MinX,MaxX,MinZ,MaxZ;
            public Area(float minX,float maxX,float minZ,float maxZ){MinX=minX;MaxX=maxX;MinZ=minZ;MaxZ=maxZ;}
            public bool Contains(Vector3 p)=>p.z>MinZ&&Mathf.Abs(p.x-(MinX+MaxX)*.5f)<(MaxX-MinX)*.5f&&p.z<MaxZ;
        }
        public Area InteriorArea = new Area(-8.1f,8.1f,0,14.3f);
        public Area SafeArea = new Area(-1.9f,1.9f,-14.1f,-8.2f);
        public Bounds NavBounds = new Bounds(new Vector3(0,1,2),new Vector3(42,18,42));
        public bool IsInside(Vector3 p)=>InteriorArea.Contains(p)&&(Map==MapId.Bellwether||!SafeArea.Contains(p));
        public bool IsSafe(Vector3 p)=>SafeArea.Contains(p);
        public static readonly float[] CastleFloors={-3.2f,0,3.2f,6.4f};
        public static float FloorBelow(float y)
        {
            float floor=CastleFloors[0];foreach(float f in CastleFloors)if(f<=y+.05f)floor=f;return floor;
        }
        // Where a checkpoint restores the investigator.
        public Vector3 CheckpointPosition(Vector3 player,bool inside)
        {
            if(Map!=MapId.Bellwether)return Spawn;
            float floor=player.y< -1?-3.2f:player.y>2?3.2f:0;
            return inside?new Vector3(0,floor+.1f,10.5f):Spawn;
        }
        // Footstep surface for Soundscape ("wood", "tile", "carpet", "gravel").
        public string StepSurface(Vector3 position)
        {
            if(Map!=MapId.Bellwether)return !IsInside(position)&&!IsSafe(position)?"gravel":"tile";
            bool van=position.z< -8.2f&&position.z> -14.1f&&Mathf.Abs(position.x)<1.9f;
            bool rug=position.y>-.4f&&position.y<.5f&&Mathf.Abs(position.x+5.4f)<1.8f&&Mathf.Abs(position.z-4.4f)<1.8f;
            return van||rug?"carpet":position.z<0?"gravel":position.y>2?"carpet":position.y< -1||position.x>3?"tile":"wood";
        }

        public void LoadOrBuild()=>LoadOrBuild(MapId.Bellwether);
        // A saved authored layout in the open scene wins; it records its own map.
        public void LoadOrBuild(MapId map)
        {
            var saved=Object.FindFirstObjectByType<WorldLayout>();
            if(!saved){Build(map);return;}
            if(saved.Map!=map)Debug.LogWarning("HC_MAP_MISMATCH scene layout is "+saved.Map+", requested "+map);
            Map=saved.Map;MissingArt.Placeholders=Map!=MapId.Bellwether;
            ownsMaterials=false;Root=saved.transform;Trace=saved.Trace;
            if(Map==MapId.Castle)ApplyCastlePoints(saved.Castle);
            Anchor=saved.Anchor;Induction=saved.Induction;Voice=saved.Voice;Print=saved.Print;
            foreach(var item in Root.GetComponentsInChildren<Interactable>())if(item.Kind==InteractKind.Hide)HidingPlaces.Add(item);
            Lamps.AddRange(saved.Lamps);WindowLights.AddRange(saved.WindowLights);Dolls.AddRange(saved.Dolls);Doors.AddRange(saved.Doors);SearchPoints.AddRange(saved.SearchPoints);
            for(int i=0;i<saved.MaterialNames.Length;i++)Materials[saved.MaterialNames[i]]=saved.Materials[i];
        }
        public void Build(MapId map)
        {
            MissingArt.Placeholders=map!=MapId.Bellwether;
            if(map==MapId.Castle)BuildCastle();else Build();
        }
        void ApplyCastlePoints(CastleSpots p)
        {
            if(p==null)throw new System.InvalidOperationException("Castle layout has no saved contract points; re-run PrepareCastle");
            Castle=p;Spawn=p.Spawn;Anchor=p.Anchor;Induction=p.Compass;Voice=p.Voice;Print=p.Print;HunterSpawn=p.HunterSpawn;AnchorFloor=FloorBelow(p.Anchor.y);
            PerchPoints.Clear();if(p.PerchPoints!=null)PerchPoints.AddRange(p.PerchPoints);
            var g=p.Gatehouse;SafeArea=new Area(g.min.x,g.max.x,g.min.z,g.max.z);
            InteriorArea=new Area(-14.5f,14.5f,-.5f,28.5f);
            NavBounds=new Bounds(new Vector3(0,2,12),new Vector3(48,22,56));
        }
        // The castle: CastleWorld (piece 1) builds the geometry and returns the
        // contract points and lists; World adopts them, replaces CastleWorld's own
        // NavMesh with World.BuildNavigation's, and adds any case interactable
        // CastleWorld did not provide.
        void BuildCastle()
        {
            Map=MapId.Castle;
            Root=new GameObject("Haunted Castle / authored layout").transform;
            CastleWorld.Build(Root,out var points);
            if(points.NavInstance.valid)points.NavInstance.Remove();
            if(points.NavData!=null)Release(points.NavData);
            if(points.Materials!=null)foreach(var kv in points.Materials)Materials[kv.Key]=kv.Value;
            foreach(var id in new[]{"paper","wood","metal","brass","black"})if(!Materials.ContainsKey(id))Mat(id,new Color(.3f,.27f,.22f));
            ApplyCastlePoints(CastleSpots.From(points));
            if(points.Doors!=null)Doors.AddRange(points.Doors);else foreach(var door in Root.GetComponentsInChildren<Door>(true))Doors.Add(door);
            if(points.HidingPlaces!=null)HidingPlaces.AddRange(points.HidingPlaces);else foreach(var item in Root.GetComponentsInChildren<Interactable>(true))if(item.Kind==InteractKind.Hide)HidingPlaces.Add(item);
            if(points.Lamps!=null)Lamps.AddRange(points.Lamps);
            if(points.WindowLights!=null)WindowLights.AddRange(points.WindowLights);
            if(points.SearchPoints!=null&&points.SearchPoints.Count>0)SearchPoints.AddRange(points.SearchPoints);
            else foreach(var point in CastleRoutePoints())SearchPoints.Add(point+Vector3.up*.1f);
            Physics.SyncTransforms();
            CastleCaseItems();
            CastleTrace();
        }
        // Floor-level copies of the castle's points of interest: the hunter's
        // patrol route and the castle validator's reachability targets.
        public List<Vector3> CastleRoutePoints()
        {
            var list=new List<Vector3>();if(Map!=MapId.Castle||Castle==null)return list;
            void Add(Vector3 p){list.Add(new Vector3(p.x,FloorBelow(p.y),p.z));}
            Add(Castle.Compass);Add(Castle.Voice);Add(Castle.Anchor);Add(Castle.AltarPosition);Add(Castle.HunterSpawn);Add(Castle.DrawbridgeTrigger.center);
            if(Castle.RelicPositions!=null)foreach(var p in Castle.RelicPositions)Add(p);
            return list;
        }
        Interactable Find(InteractKind kind,string label=null)
        {
            foreach(var item in Root.GetComponentsInChildren<Interactable>(true))if(item.Kind==kind&&(label==null||item.Label==label))return item;
            return null;
        }
        // First candidate around centre whose box is clear of geometry.
        Vector3 FreeSpot(Vector3 center,Vector3 size,params Vector3[] offsets)
        {
            foreach(var offset in offsets)
            {
                var p=center+offset;
                if(!Physics.CheckBox(p,size*.5f,Quaternion.identity,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore))return p;
            }
            return center+offsets[0];
        }
        // Fallback case objects, created only when CastleWorld did not place its own
        // Interactable of that kind (piece 1 may author the real ones).
        void CastleCaseItems()
        {
            var book=CaseBook.For(MapId.Castle);
            var around=new[]{new Vector3(1.6f,0,0),new Vector3(-1.6f,0,0),new Vector3(0,0,1.6f),new Vector3(0,0,-1.6f),new Vector3(2.4f,0,1.2f),new Vector3(-2.4f,0,1.2f)};
            if(!Find(InteractKind.Kit))Interact("Fallback / lantern cart",FreeSpot(Spawn+new Vector3(0,.45f,0),new Vector3(.9f,.9f,.6f),new Vector3(1.4f,0,1.2f),new Vector3(-1.4f,0,1.2f),new Vector3(0,0,1.6f)),new Vector3(.9f,.9f,.6f),"wood",InteractKind.Kit,book.KitLabel,"");
            if(!Find(InteractKind.Van))Interact("Fallback / drawbridge winch",Castle.DrawbridgeTrigger.center,new Vector3(.6f,1.2f,.6f),"metal",InteractKind.Van,book.FinishLabel,"");
            if(!Find(InteractKind.Anchor))Interact("Fallback / chapel bell",Anchor,new Vector3(.6f,.7f,.6f),"brass",InteractKind.Anchor,book.AnchorLabel,"");
            if(!Find(InteractKind.Breaker))Interact("Fallback / chapel brazier",FreeSpot(Castle.AltarPosition+new Vector3(0,.8f,0),new Vector3(.6f,1,.6f),around),new Vector3(.6f,1,.6f),"black",InteractKind.Breaker,book.BreakerLabel,"");
            if(!Find(InteractKind.Note,book.ArrivalNoteTitle))Interact("Fallback / arrival note",FreeSpot(Spawn+new Vector3(0,.85f,0),new Vector3(.42f,.04f,.3f),new Vector3(-1.4f,0,1.2f),new Vector3(1.4f,0,-.6f),new Vector3(0,0,1)),new Vector3(.42f,.04f,.3f),"paper",InteractKind.Note,book.ArrivalNoteTitle,book.ArrivalNoteBody);
            if(!Find(InteractKind.Altar))
            {var altar=Interact("Forge altar",Castle.AltarPosition+new Vector3(0,.15f,0),new Vector3(.9f,.3f,1.6f),"black",InteractKind.Altar,book.AltarLabel,"");altar.GetComponent<Renderer>().enabled=false;altar.GetComponent<Collider>().isTrigger=true;}
            if(Castle.RelicPositions!=null)for(int i=0;i<Castle.RelicPositions.Length&&i<book.RelicNames.Length;i++)
            {
                bool exists=false;foreach(var item in Root.GetComponentsInChildren<Interactable>(true))if(item.Kind==InteractKind.Relic&&item.RelicIndex==i)exists=true;
                if(exists)continue;
                var relic=Interact("Relic / "+book.RelicNames[i],Castle.RelicPositions[i]+Vector3.up*.08f,new Vector3(.3f,.16f,.3f),"brass",InteractKind.Relic,book.RelicNames[i],"");relic.RelicIndex=i;
            }
            var anchors=new[]{Castle.Compass,Castle.Voice,Castle.AltarPosition};
            for(int i=0;i<book.NoteTitles.Length;i++)
            {
                if(Find(InteractKind.Note,book.NoteTitles[i]))continue;
                var near=anchors[i%anchors.Length];var floor=new Vector3(near.x,FloorBelow(near.y)+.85f,near.z);
                Interact("Fallback / "+book.NoteTitles[i],FreeSpot(floor,new Vector3(.4f,.04f,.3f),around),new Vector3(.4f,.04f,.3f),"paper",InteractKind.Note,book.NoteTitles[i],book.NoteBodies[i]);
            }
        }
        // The grave-ash lantern trace (UV print role): a quad on the wall nearest Print.
        void CastleTrace()
        {
            var normal=Vector3.forward;float best=float.MaxValue;
            foreach(var direction in new[]{Vector3.left,Vector3.right,Vector3.forward,Vector3.back})
                if(Physics.Raycast(Print+direction*-.3f,direction,out var hit,.9f,~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore)&&hit.distance<best){best=hit.distance;normal=hit.normal;}
            var trace=GameObject.CreatePrimitive(PrimitiveType.Quad);trace.name="Persistent claw-scores";trace.transform.SetParent(Root,false);
            trace.transform.position=Print;trace.transform.rotation=Quaternion.LookRotation(-normal);trace.transform.localScale=new Vector3(.45f,.4f,1);
            trace.GetComponent<Collider>().enabled=false;
            var texture=Resources.Load<Texture2D>("Castle/ClawScores");if(!texture)texture=Resources.Load<Texture2D>("Surfaces/UVHandprint");
            var material=new Material(Resources.Load<Shader>("UVTrace")){name="Castle latent claw-scores",mainTexture=texture};Materials["uvtrace"]=material;
            var renderer=trace.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Trace=trace.transform;Trace.gameObject.SetActive(false);
        }
        // Adds the serialized snapshot that a prepared scene loads back at runtime.
        public WorldLayout Snapshot()
        {
            var snapshot=Root.gameObject.AddComponent<WorldLayout>();snapshot.Map=Map;snapshot.Castle=Castle;
            snapshot.Trace=Trace;snapshot.Anchor=Anchor;snapshot.Induction=Induction;snapshot.Voice=Voice;snapshot.Print=Print;
            snapshot.Lamps=Lamps.ToArray();snapshot.WindowLights=WindowLights.ToArray();snapshot.Dolls=Dolls.ToArray();snapshot.Doors=Doors.ToArray();snapshot.SearchPoints=SearchPoints.ToArray();
            snapshot.MaterialNames=new List<string>(Materials.Keys).ToArray();snapshot.Materials=new List<Material>(Materials.Values).ToArray();
            return snapshot;
        }
        public void Build()
        {
            Map=MapId.Bellwether;Spawn=HouseSpawn;
            Root = new GameObject("Bellwether House / authored layout").transform;
            Palette();
            Box("Front garden", new Vector3(0,-.38f,-16.2f), new Vector3(65,.5f,32.4f), "earth");
            Box("Rear garden", new Vector3(0,-.38f,23.2f), new Vector3(65,.5f,18.4f), "earth");
            foreach(int side in new[]{-1,1})Box("Side garden",new Vector3(side*20.25f,-.38f,7),new Vector3(24.5f,.5f,14),"earth");
            Box("Garden path", new Vector3(0,-.09f,-4.4f), new Vector3(2.2f,.14f,9), "stone");
            // Basement, ground and upper slabs; stair voids are physically open.
            Floor(-3.2f, false, false, "stone"); Floor(0, true, false, "wood"); Floor(3.2f, false, true, "carpet");
            Box("Attic ceiling", new Vector3(0,6.35f,7), new Vector3(16,.16f,14), "cream");
            for(int level=-1; level<=1; level++)
            {
                float y=level*3.2f;
                Shell(y,level==-1?"stone":"cream",level==0);
                Corridor(y,level);
                Lamp(new Vector3(0,y+2.65f,3.8f),level==-1?.55f:.8f);
                Lamp(new Vector3(0,y+2.65f,10.5f),.75f);
                SearchPoints.Add(new Vector3(0,y+.1f,3.5f)); SearchPoints.Add(new Vector3(0,y+.1f,10.5f));
                SearchPoints.Add(new Vector3(-5,y+.1f,10.5f)); SearchPoints.Add(new Vector3(5,y+.1f,10.5f));
            }
            Stairs(2.8f,0); Stairs(-2.8f,-3.2f);
            // Separate left rooms with a connecting doorway, and separate upper right bath.
            // Keep the basement stair's top landing clear before dividing the rooms.
            SplitWall(-4.8f,0,8.15f,6.4f,"sage"); SplitWall(-4.8f,3.2f,7,6.4f,"sage");
            SplitWall(5.3f,3.2f,8.3f,5.4f,"cream");
            Living(); Dining(); Kitchen(); Upstairs(); Basement(); Exterior(); Van();
            Interact("Foyer letter",new Vector3(.85f,.85f,1.15f),new Vector3(.42f,.04f,.3f),"paper",InteractKind.Note,"Arrival note",
                "14 BELLWETHER LANE\n\nThe Harrows left in a hurry. They reported footsteps above an empty dining room and a child's toy playing below the furnace.\n\nScout the wardrobe, linen cupboard and basement locker. She can follow you back to the van. Never hide while it is watching you.");
            Box("Console table",new Vector3(.9f,.72f,1.2f),new Vector3(.65f,.12f,.5f),"wood");
            Hide("Hall coat cupboard",new Vector3(.2f,0,13.05f),0);
            Hide("Upstairs linen cupboard",new Vector3(.2f,3.2f,13.05f),0);
            Hide("Basement steel locker",new Vector3(-6.8f,-3.2f,12.8f),0);
            DoorAt("Front door",new Vector3(-.8f,0,.02f),0);
            var music=Interact("Music box anchor",Anchor,new Vector3(.55f,.3f,.38f),"wood",InteractKind.Anchor,"The music box","");
            music.GetComponent<Renderer>().enabled=false;AuthoredArt.Place(Root,"MusicBox",Anchor);
            Interact("Fuse cabinet",new Vector3(7.77f,-1.7f,7.9f),new Vector3(.2f,.7f,.6f),"metal",InteractKind.Breaker,"Basement breaker","");
            Interact("Basement diary",new Vector3(-5.6f,-2.27f,8.8f),new Vector3(.35f,.04f,.25f),"paper",InteractKind.Note,"Furnace diary",
                "OCTOBER 17\n\nIt repeats the little things. A chair pulled out. A door left open. The house kept warm.\n\nI moved the music box upstairs. By morning it was back beside the furnace. The doll stayed where I left it.\n\nThe same evening, again and again: the fourth chair, the warm room, that little song. It is the routine she cannot leave.");
            var trace=GameObject.CreatePrimitive(PrimitiveType.Quad);trace.name="Persistent ultraviolet handprint";trace.transform.SetParent(Root,false);
            trace.transform.position=Print;trace.transform.rotation=Quaternion.Euler(0,-90,0);trace.transform.localScale=new Vector3(.27f,.28f,1);
            trace.GetComponent<Collider>().enabled=false;
            var traceMaterial=new Material(Resources.Load<Shader>("UVTrace")){name="Original latent UV print",mainTexture=Resources.Load<Texture2D>("Surfaces/UVHandprint")};Materials["uvtrace"]=traceMaterial;
            var traceRenderer=trace.GetComponent<Renderer>();traceRenderer.sharedMaterial=traceMaterial;traceRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Trace=trace.transform;
            Trace.gameObject.SetActive(false);
            FurnitureArt.Apply(this);DetailArt.Apply(this);GroundProps();
            // Switch plates at the remaining room entrances. Their targets are
            // resolved after every practical lamp exists in the authored layout.
            AuthoredArt.Place(Root,"Switch",new Vector3(1.32f,1.25f,9.2f),90);
            AuthoredArt.Place(Root,"Switch",new Vector3(1.32f,4.45f,10),90);
            AuthoredArt.Place(Root,"Switch",new Vector3(-1.32f,-1.95f,11.8f),-90);
            WireSwitches();AudioScene();
        }
        void GroundProps()
        {
            var all=Root.GetComponentsInChildren<Renderer>();
            foreach(var item in Root.GetComponentsInChildren<Transform>())
            {
                if(item.name!="Authored / Mug")continue;
                var own=item.GetComponentsInChildren<Renderer>();if(own.Length==0)continue;
                var bounds=own[0].bounds;foreach(var r in own)bounds.Encapsulate(r.bounds);
                float top=float.NegativeInfinity;
                foreach(var r in all)
                {
                    if(!r.enabled||r.transform.IsChildOf(item))continue;
                    var b=r.bounds;
                    if(b.max.y>bounds.min.y+.10f||b.max.y<bounds.min.y-.35f)continue;
                    if(item.position.x<b.min.x||item.position.x>b.max.x||item.position.z<b.min.z||item.position.z>b.max.z)continue;
                    if(b.size.x<.2f||b.size.z<.2f)continue;
                    top=Mathf.Max(top,b.max.y);
                }
                if(!float.IsNegativeInfinity(top))item.position+=Vector3.up*(top-bounds.min.y+.001f);
            }
        }
        void Palette()
        {
            Mat("cream",new Color(.64f,.60f,.49f)); Mat("sage",new Color(.26f,.34f,.27f)); Mat("wood",new Color(.23f,.13f,.075f));
            Mat("carpet",new Color(.24f,.22f,.19f)); Mat("stone",new Color(.28f,.29f,.27f)); Mat("earth",new Color(.055f,.07f,.06f));
            Materials["earth"].shader=Resources.Load<Shader>("GardenGround");Materials["earth"].color=new Color(.8f,.82f,.75f);
            Materials["earth"].SetTexture("_GrassTex",Resources.Load<Texture2D>("Surfaces/GardenGround"));Materials["earth"].SetTexture("_RoadTex",Resources.Load<Texture2D>("Surfaces/DrivewayGround"));
            Mat("metal",new Color(.18f,.21f,.20f)); Mat("black",new Color(.025f,.03f,.029f)); Mat("paper",new Color(.77f,.73f,.60f));
            Mat("cloth",new Color(.38f,.35f,.27f)); Mat("porcelain",new Color(.68f,.65f,.55f)); Mat("brass",new Color(.47f,.33f,.13f));
            Mat("familyphoto",Color.white);Materials["familyphoto"].mainTexture=Resources.Load<Texture2D>("Surfaces/FamilyPhoto");
            Mat("rug",Color.white);Materials["rug"].mainTexture=Resources.Load<Texture2D>("Surfaces/FamilyRug");
            Mat("floorwood",Color.white);Materials["floorwood"].mainTexture=Resources.Load<Texture2D>("Surfaces/OakFloor");
            Mat("red",new Color(.3f,.07f,.065f)); Mat("glass",new Color(.075f,.14f,.20f));
            Mat("uv",new Color(.5f,.15f,.9f),new Color(.5f,.1f,1)); Mat("glow",new Color(.7f,.64f,.4f),new Color(.5f,.42f,.2f));
        }
        void Mat(string id,Color c,Color emission=default(Color))
        {
            bool surface=id=="cream"||id=="sage"||id=="carpet"||id=="stone"||id=="wood"||id=="cloth";
            var shader=surface?Resources.Load<Shader>("HouseSurface"):Shader.Find("Standard");
            var m=new Material(shader){name="HC / "+id,color=c};
            if(surface)m.SetFloat("_Pattern",id=="carpet"||id=="cloth"?1:id=="stone"?2:id=="wood"?3:0);
            m.SetFloat("_Glossiness",id=="metal"?.35f:.12f);
            if(emission.maxColorComponent>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",emission);}
            Materials[id]=m;
        }
        public GameObject Box(string name,Vector3 p,Vector3 s,string mat,bool collision=true)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(Root); g.transform.position=p;g.transform.localScale=s;
            g.GetComponent<Renderer>().sharedMaterial=Materials[mat];
            if(mat=="floorwood")
            {
                var mesh=g.GetComponent<MeshFilter>().mesh;var vertices=mesh.vertices;var uv=new Vector2[vertices.Length];
                for(int j=0;j<vertices.Length;j++){var point=g.transform.TransformPoint(vertices[j]);uv[j]=new Vector2(point.x*.5f,point.z*.5f);}
                mesh.uv=uv;
            }
            if(!collision) g.GetComponent<Collider>().enabled=false;
            return g;
        }
        public GameObject Sphere(string name,Vector3 p,Vector3 s,string mat)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(Root);g.transform.position=p;g.transform.localScale=s;
            g.GetComponent<Renderer>().sharedMaterial=Materials[mat];g.GetComponent<Collider>().enabled=false;return g;
        }
        void Floor(float y,bool leftHole,bool rightHole,string mat)
        {
            float[] xs={-8,-4.15f,-1.45f,1.45f,4.15f,8};
            for(int i=0;i<xs.Length-1;i++)
            {
                bool hole=(leftHole&&i==1)||(rightHole&&i==3);
                if(hole){Slab(xs[i],xs[i+1],0,.9f,y,mat);Slab(xs[i],xs[i+1],7.15f,14,y,mat);}
                else Slab(xs[i],xs[i+1],0,14,y,mat);
            }
        }
        void Slab(float x0,float x1,float z0,float z1,float y,string mat)
        {
            Box("Floor / "+mat,new Vector3((x0+x1)/2,y-.1f,(z0+z1)/2),new Vector3(x1-x0,.2f,z1-z0),mat=="wood"?"floorwood":mat);
            Box("Painted ceiling underside",new Vector3((x0+x1)/2,y-.206f,(z0+z1)/2),new Vector3(x1-x0,.012f,z1-z0),"cream",false);
        }
        void Shell(float y,string mat,bool entrance)
        {
            // Real openings, with a closed glass collision plane; no decorative
            // window is painted on an unbroken opaque wall.
            float low=y<0?2.1f:.95f,high=y<0?2.75f:2.3f;
            foreach(int side in new[]{-1,1})
            {
                float x=side*8;
                float[] ends={0,2.1f,3.9f,10.1f,11.9f,14};
                for(int i=0;i<ends.Length;i+=2)
                    Box("Exterior wall pier",new Vector3(x,y+1.5f,(ends[i]+ends[i+1])*.5f),new Vector3(.26f,3,ends[i+1]-ends[i]),mat);
                foreach(float z in new[]{3f,11f})
                {
                    Box("Window apron wall",new Vector3(x,y+low*.5f,z),new Vector3(.26f,low,1.8f),mat);
                    Box("Window head wall",new Vector3(x,y+(high+3)*.5f,z),new Vector3(.26f,3-high,1.8f),mat);
                    var pane=Box("Closed window collision",new Vector3(x,y+(low+high)*.5f,z),new Vector3(.04f,high-low,1.8f),"glass");pane.GetComponent<Renderer>().enabled=false;
                    var size=new Vector3(1,(high-low)/1.3f,1);
                    AuthoredArt.Place(Root,"SashWindow",new Vector3(x,y+(low+high)*.5f,z),Quaternion.Euler(0,side*90,0),size);
                    if(y>=0)AuthoredArt.Place(Root,"CurtainPair",new Vector3(x,y+1.65f,z),side*90);
                    if(y>=0)AuthoredArt.Place(Root,"Radiator",new Vector3(side*7.73f,y+.43f,z),side*90);
                    // Light enters from the opening; it never ignores room walls.
                    var moon=new GameObject("Window moonlight");moon.transform.SetParent(Root,false);moon.transform.position=new Vector3(side*8.1f,y+high-.05f,z);
                    moon.transform.rotation=Quaternion.LookRotation(new Vector3(-side,-.23f,.16f));
                    var light=moon.AddComponent<Light>();light.type=LightType.Spot;light.spotAngle=100;light.range=6;light.intensity=y<0?.17f:.36f;light.color=new Color(.43f,.55f,.72f);light.shadows=LightShadows.Soft;light.shadowStrength=.8f;light.shadowBias=.02f;WindowLights.Add(light);
                }
                Box("Skirting",new Vector3(side*7.83f,y+.07f,7),new Vector3(.065f,.14f,14),"paper",false);
                Box("Skirting cap",new Vector3(side*7.82f,y+.15f,7),new Vector3(.08f,.025f,14),"paper",false);
                Box("Ceiling cornice",new Vector3(side*7.83f,y+2.93f,7),new Vector3(.09f,.11f,14),"paper",false);
                foreach(float z in new[]{5.4f,12.6f})AuthoredArt.Place(Root,"Socket",new Vector3(side*7.85f,y+.32f,z),side*90);
            }
            Box("Rear wall",new Vector3(0,y+1.5f,14),new Vector3(16,3,.26f),mat);
            if(y>=0)FrontFacade(y,mat,entrance);
            else Box("Front wall",new Vector3(0,y+1.5f,0),new Vector3(16,3,.26f),mat);
            foreach(float z in new[]{.17f,13.83f})
            {
                if(z<1&&y>=0)continue;
                Box("End skirting",new Vector3(0,y+.08f,z),new Vector3(15.7f,.16f,.065f),"paper",false);
                Box("End cornice",new Vector3(0,y+2.93f,z),new Vector3(15.7f,.11f,.095f),"paper",false);
            }
        }
        void FrontFacade(float y,string mat,bool entrance)
        {
            // The approach should read as a lived-in house. These are actual
            // wall openings, using the same recessed sash/curtain set as indoors.
            foreach(int side in new[]{-1,1})
            {
                float x=side*5.2f;
                Box("Front outer pier",new Vector3(side*7.05f,y+1.5f,0),new Vector3(1.9f,3,.26f),mat);
                Box("Front inner pier",new Vector3(side*2.6f,y+1.5f,0),new Vector3(3.4f,3,.26f),mat);
                Box("Front window apron",new Vector3(x,y+.475f,0),new Vector3(1.8f,.95f,.26f),mat);
                Box("Front window head",new Vector3(x,y+2.65f,0),new Vector3(1.8f,.7f,.26f),mat);
                var pane=Box("Front closed window collision",new Vector3(x,y+1.625f,0),new Vector3(1.8f,1.35f,.04f),"glass");pane.GetComponent<Renderer>().enabled=false;
                AuthoredArt.Place(Root,"SashWindow",new Vector3(x,y+1.625f,0),Quaternion.Euler(0,180,0),new Vector3(1,1.35f/1.3f,1));
                AuthoredArt.Place(Root,"CurtainPair",new Vector3(x,y+1.65f,0),180);
                var moon=new GameObject("Front window moonlight");moon.transform.SetParent(Root,false);moon.transform.position=new Vector3(x,y+2.25f,-.1f);
                moon.transform.rotation=Quaternion.LookRotation(new Vector3(-side*.1f,-.23f,1));
                var light=moon.AddComponent<Light>();light.type=LightType.Spot;light.spotAngle=100;light.range=6;light.intensity=.3f;light.color=new Color(.43f,.55f,.72f);light.shadows=LightShadows.Soft;light.shadowStrength=.8f;light.shadowBias=.02f;WindowLights.Add(light);
                Box("Front room skirting",new Vector3(side*4.45f,y+.08f,.17f),new Vector3(7.1f,.16f,.065f),"paper",false);
                Box("Front room cornice",new Vector3(side*4.45f,y+2.93f,.17f),new Vector3(7.1f,.11f,.095f),"paper",false);
            }
            if(entrance)Box("Door lintel",new Vector3(0,y+2.7f,0),new Vector3(1.8f,.6f,.26f),mat);
            else
            {
                Box("Front center pier",new Vector3(0,y+1.5f,0),new Vector3(1.8f,3,.26f),mat);
                Box("Front center skirting",new Vector3(0,y+.08f,.17f),new Vector3(1.8f,.16f,.065f),"paper",false);
                Box("Front center cornice",new Vector3(0,y+2.93f,.17f),new Vector3(1.8f,.11f,.095f),"paper",false);
            }
        }
        void Corridor(float y,int level)
        {
            foreach(float x in new[]{-1.45f,1.45f})
            {
                // Stair sides remain open at both landings; left ground stair arrives at z7.2.
                bool stair=(level==0)||(level==-1&&x<0)||(level==1&&x>0);
                if(!stair) WallSegmentsX(x,y,new[]{0f,2.6f,4.4f,9.6f,11.4f,14f});
                else WallSegmentsX(x,y,new[]{8.1f,9.6f,11.4f,14f});
            }
        }
        void WallSegmentsX(float x,float y,float[] z)
        {
            for(int i=0;i+1<z.Length;i+=2)Box("Hall partition",new Vector3(x,y+1.5f,(z[i]+z[i+1])/2),new Vector3(.15f,3,z[i+1]-z[i]),"sage");
        }
        void SplitWall(float x,float y,float z,float width,string mat)
        {
            Box("Partition",new Vector3(x-width*.25f-.45f,y+1.5f,z),new Vector3(width*.5f-.9f,3,.14f),mat);
            Box("Partition",new Vector3(x+width*.25f+.45f,y+1.5f,z),new Vector3(width*.5f-.9f,3,.14f),mat);
            Box("Interior lintel",new Vector3(x,y+2.65f,z),new Vector3(1.8f,.7f,.14f),mat);
            DoorAt("Interior door",new Vector3(x-.8f,y,z),0);
        }
        void Stairs(float x,float y)
        {
            const int n=18;
            for(int i=0;i<n;i++)
            {
                float h=(i+1)*3.2f/n;
                Box("Stair tread "+i,new Vector3(x,y+h/2,1+(i+.5f)*6.15f/n),new Vector3(2.4f,h,6.15f/n+.008f),"wood");
                Box("Stair nosing",new Vector3(x,y+h+.005f,1+i*6.15f/n),new Vector3(2.4f,.018f,.035f),"brass",false);
            }
            // Balustrade along outer edge; central corridor remains a clear passage.
            float outer=x>0?x+1.22f:x-1.22f;
            for(int i=0;i<=18;i++)AuthoredArt.Place(Root,"Baluster",new Vector3(outer,y+i*3.2f/18,1+i*6.15f/18));
            var rail=Box("Handrail",new Vector3(outer,y+2.65f,4.08f),new Vector3(.1f,.1f,6.94f),"wood");rail.transform.rotation=Quaternion.Euler(-27.5f,0,0);
            // Protect the upper-floor void from side entry; the z7.15 landing stays open.
            foreach(int side in new[]{-1,1})
            {
                var guard=Box("Upper stair guard",new Vector3(x+side*1.43f,y+3.72f,3.9f),new Vector3(.08f,1.04f,5.9f),"wood");guard.GetComponent<Renderer>().enabled=false;
                Box("Landing handrail",new Vector3(x+side*1.43f,y+4.22f,3.9f),new Vector3(.11f,.09f,5.9f),"wood",false);
                for(int i=0;i<19;i++)AuthoredArt.Place(Root,"Baluster",new Vector3(x+side*1.43f,y+3.2f,1.05f+i*.32f));
            }
            var end=Box("Upper stair end guard",new Vector3(x,y+3.72f,.92f),new Vector3(2.96f,1.04f,.08f),"wood");end.GetComponent<Renderer>().enabled=false;
            Box("Landing end handrail",new Vector3(x,y+4.22f,.92f),new Vector3(2.96f,.09f,.11f),"wood",false);
            for(int i=0;i<9;i++)AuthoredArt.Place(Root,"Baluster",new Vector3(x-1.2f+i*.3f,y+3.2f,.92f));
        }
        void Living()
        {
            Box("Living room rug",new Vector3(-5.4f,.015f,4.4f),new Vector3(3.6f,.025f,3.6f),"rug",false);
            Box("Sofa seat",new Vector3(-6.6f,.42f,4),new Vector3(1.1f,.7f,2.7f),"cloth");Box("Sofa back",new Vector3(-7.2f,.9f,4),new Vector3(.25f,1.1f,2.7f),"sage");
            for(int i=-1;i<=1;i+=2)Box("Sofa arm",new Vector3(-6.6f,.75f,4+i*1.35f),new Vector3(1.2f,.4f,.2f),"sage");
            Table(new Vector3(-5.225f,0,4.6f),new Vector2(.6f,1.5f),.47f);
            foreach(var r in Root.GetComponentsInChildren<Renderer>())if(r.name.StartsWith("Table ")&&Vector3.Distance(r.transform.position,new Vector3(-5.2f,.3f,4.6f))<1.3f)r.enabled=false;
            AuthoredArt.Place(Root,"CoffeeTable",new Vector3(-5.225f,0,4.6f),Quaternion.identity,new Vector3(.5f,1,1.5f/1.7f));Mug(new Vector3(-5.15f,.54f,4.4f));
            Box("TV stand",new Vector3(-4.75f,.43f,2.6f),new Vector3(.8f,.86f,1.2f),"wood");
            AuthoredArt.Place(Root,"Television",new Vector3(-4.75f,.88f,2.6f),125);
            Plant(new Vector3(-7.25f,0,1.55f),1);
            Bookshelf(new Vector3(-5.8f,0,1),0);Lamp(new Vector3(-6,2.55f,4.5f),1.1f);
            Doll(new Vector3(-6.5f,.56f,4.6f));
            SideLamp(new Vector3(-6.65f,0,6.35f));
        }
        void SideLamp(Vector3 floor)
        {
            Table(floor,new Vector2(.65f,.65f),.64f);
            var shade=AuthoredArt.Place(Root,"TableLamp",floor+Vector3.up*.70f);
            var g=new GameObject("Warm table lamp");g.transform.SetParent(Root,false);g.transform.position=floor+Vector3.up*1.16f;
            var l=g.AddComponent<Light>();l.type=LightType.Point;l.range=3.9f;l.intensity=.85f;l.color=new Color(1,.76f,.48f);l.shadowBias=.02f;l.shadowNormalBias=.12f;
            Lamps.Add(l);shade.gameObject.AddComponent<PracticalFixture>().Source=l;
        }
        void Dining()
        {
            Table(new Vector3(-5.3f,0,10.4f),new Vector2(2.1f,2.8f),.8f);
            for(int i=-1;i<=1;i+=2){Chair(new Vector3(-5.3f+i*1.45f,0,10.4f));Mug(new Vector3(-5.3f+i*.65f,.89f,10.3f));}
            Box("Dining sideboard",new Vector3(-5.6f,.55f,13.3f),new Vector3(3.1f,1.1f,.6f),"wood");
            Lamp(new Vector3(-5.3f,2.55f,10.4f),1.2f);
            Interact("Family photograph",new Vector3(-5.65f,1.3f,13.1f),new Vector3(.45f,.36f,.05f),"paper",InteractKind.Note,"Family photograph",
                "A FAMILY OF FOUR\n\nThe mother's face has been rubbed away while the other three remain clear. Under the frame: 'Don't put her chair away. She still comes home.'\n\nThe empty place is laid for supper. The chair faces the doorway, as if someone is expected.");
            Frame(new Vector3(-7.83f,1.9f,11.6f));
            var family=Root.Find("Family photograph");family.GetComponent<Renderer>().enabled=false;
            family.position=new Vector3(-5.65f,1.34f,13.06f);family.localScale=new Vector3(.6f,.48f,.06f);
            Photograph(family.position,0,.95f);
        }
        void Kitchen()
        {
            for(int i=0;i<4;i++)
            {
                Box("Kitchen cabinet",new Vector3(4.2f+i*.8f,.48f,13.52f),new Vector3(.77f,.9f,.72f),"sage");
                Box("Countertop",new Vector3(4.2f+i*.8f,.95f,13.52f),new Vector3(.8f,.065f,.81f),"paper");
                Box("Cabinet pull",new Vector3(4.2f+i*.8f,.73f,12.63f),new Vector3(.22f,.04f,.04f),"brass",false);
                Box("Upper cabinet",new Vector3(4.2f+i*.8f,2.12f,13.78f),new Vector3(.77f,.8f,.38f),"cream");
            }
            Box("Sink",new Vector3(5,.995f,13.1f),new Vector3(.65f,.02f,.7f),"metal",false);
            Box("Refrigerator",new Vector3(7.1f,1.04f,9.1f),new Vector3(1.15f,2.08f,1.05f),"cream");
            for(int i=0;i<4;i++)Box("Fridge drawing",new Vector3(6.97f+(i%2)*.25f,.9f+(i/2)*.4f,8.568f),new Vector3(.18f,.27f,.015f),i==2?"red":"paper",false);
            foreach(var r in Root.GetComponentsInChildren<Renderer>())
                if(r.name=="Kitchen cabinet"||r.name=="Countertop"||r.name=="Cabinet pull"||r.name=="Upper cabinet"||r.name=="Sink")r.enabled=false;
            for(int i=0;i<4;i++)
            {
                AuthoredArt.Place(Root,"BaseCabinet",new Vector3(4.2f+i*.8f,0,13.52f),Quaternion.identity,Vector3.one);
                AuthoredArt.Place(Root,"WallCabinet",new Vector3(4.2f+i*.8f,2.12f,13.78f));
            }
            AuthoredArt.Place(Root,"KitchenSink",new Vector3(5,1.005f,13.5f));
            Plant(new Vector3(6.5f,1.005f,13.65f),.45f);
            Mug(new Vector3(4.4f,1.005f,13.45f));Lamp(new Vector3(5,2.55f,10.7f),1);
            Table(new Vector3(6,0,4.7f),new Vector2(2.1f,1.1f),.9f);Box("Laundry basket",new Vector3(6.7f,.3f,2),new Vector3(.7f,.6f,.65f),"cloth");
            Box("Utility towel",new Vector3(6,.99f,4.7f),new Vector3(.7f,.06f,.45f),"paper",false);
        }
        void Upstairs()
        {
            Bed(new Vector3(-5.8f,3.2f,3.2f),false);Bed(new Vector3(-5.8f,3.2f,10.5f),true);
            Bookshelf(new Vector3(-3.1f,3.2f,1),0);Doll(new Vector3(-3.1f,4.5f,1));Doll(new Vector3(-7.3f,3.2f,5.7f));
            Box("Toy chest",new Vector3(-5.7f,3.5f,5.9f),new Vector3(1.3f,.6f,.65f),"sage");
            Interact("Child's drawing",new Vector3(-5.7f,3.83f,5.9f),new Vector3(.4f,.025f,.3f),"paper",InteractKind.Note,"Child's drawing",
                "A HOUSE WITH HANDS\n\nA hand is traced beside the bedroom window. Beneath it: 'She finds me when I leave the light on.'\n\nIn the margin: 'When the little song ends, she goes quiet. Dad keeps winding it back. Please let it finish.'");
            Lamp(new Vector3(-5.2f,5.9f,3.8f),.7f);Lamp(new Vector3(-5.2f,5.9f,10.6f),.65f);
            Box("Bath",new Vector3(6.6f,3.52f,12.5f),new Vector3(1.2f,.65f,2.3f),"porcelain");
            Box("Bath water",new Vector3(6.6f,3.86f,12.5f),new Vector3(.9f,.02f,1.9f),"glass",false);
            Box("Washbasin",new Vector3(5.8f,4.1f,9),new Vector3(1.4f,.18f,.65f),"porcelain");
            Box("Mirror",new Vector3(5.8f,4.8f,8.48f),new Vector3(1.25f,1,.03f),"glass",false);
            Sphere("Toilet",new Vector3(7,3.65f,9.7f),new Vector3(.65f,.7f,.9f),"porcelain");
            Lamp(new Vector3(5.7f,5.85f,11),.65f);Box("Guest wardrobe",new Vector3(6.7f,4.3f,3.2f),new Vector3(1.5f,2.2f,.8f),"wood");
        }
        void Basement()
        {
            for(int i=0;i<2;i++)
            {
                Box("Laundry machine",new Vector3(6.8f,-2.65f,2+i*1.4f),new Vector3(1.1f,1.1f,1.15f),"cream");
                Sphere("Washer drum",new Vector3(6.22f,-2.6f,2+i*1.4f),new Vector3(.05f,.65f,.65f),"black");
            }
            Box("Furnace",new Vector3(6.7f,-2.05f,13),new Vector3(1.4f,2.3f,1),"metal");
            Box("Furnace duct",new Vector3(6.7f,-.55f,11),new Vector3(.65f,.55f,5.6f),"metal");
            Table(new Vector3(5.8f,-3.2f,11.5f),new Vector2(1.8f,.9f),.8f);
            Table(new Vector3(-5.6f,-3.2f,8.8f),new Vector2(2.5f,1.2f),.85f);
            for(int i=0;i<7;i++)Box("Packed family box",new Vector3(-6.8f+(i%2)*.8f,-2.92f+(i/4)*.55f,2+(i%4)*.8f),new Vector3(.65f,.55f,.65f),"cloth");
            Bookshelf(new Vector3(-5.2f,-3.2f,13.3f),0);Lamp(new Vector3(5,-.6f,9.5f),.7f);Lamp(new Vector3(-5,-.6f,9.2f),.5f);
            Doll(new Vector3(-5.6f,-2.29f,8.8f));
        }
        void Exterior()
        {
            Box("Porch awning",new Vector3(0,2.6f,-1),new Vector3(3.6f,.2f,2.2f),"wood");
            for(int i=-1;i<=1;i+=2)Box("Porch post",new Vector3(i*1.6f,1.25f,-1.7f),new Vector3(.14f,2.5f,.14f),"cream");
            var roof=Box("West roof pitch",new Vector3(-4.1f,7.35f,7),new Vector3(8.5f,.22f,14.8f),"black");roof.transform.rotation=Quaternion.Euler(0,0,15);
            roof.GetComponent<Renderer>().enabled=false;
            roof=Box("East roof pitch",new Vector3(4.1f,7.35f,7),new Vector3(8.5f,.22f,14.8f),"black");roof.transform.rotation=Quaternion.Euler(0,0,-15);
            roof.GetComponent<Renderer>().enabled=false;
            AuthoredArt.Place(Root,"HouseRoof",new Vector3(0,6.35f,7));
            foreach(float z in new[]{-.17f,14.17f})Box("Exterior storey fascia",new Vector3(0,3.10f,z),new Vector3(16.30f,.20f,.08f),"paper",false);
            foreach(float x in new[]{-8.17f,8.17f})Box("Exterior side fascia",new Vector3(x,3.10f,7),new Vector3(.08f,.20f,14.30f),"paper",false);
            Lamp(new Vector3(1.25f,2.05f,-.25f),.55f,false);
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<13;i++)AuthoredArt.Place(Root,"FencePanel",new Vector3(side*11,-.13f,-5+i*1.6f),90);
                for(int i=0;i<=13;i++)AuthoredArt.Place(Root,"FencePost",new Vector3(side*11,-.13f,-5.8f+i*1.6f),90);
                var fence=Box("Garden fence collision",new Vector3(side*11,.50f,4.6f),new Vector3(.16f,1.26f,20.8f),"wood");fence.GetComponent<Renderer>().enabled=false;
            }
            for(int i=0;i<14;i++)
            {
                float x=(i%2==0?-1:1)*(13+i%4*2.2f),z=-4+i*2;
                var tree=new GameObject("Authored winter tree "+i).transform;tree.SetParent(Root,false);tree.position=new Vector3(x,-.13f,z);tree.rotation=Quaternion.Euler(0,i*137.5f,0);tree.localScale=Vector3.one*(.88f+(i%5)*.065f);
                string asset="WinterTree"+(1+i%3);
                var high=AuthoredArt.Place(tree,asset,Vector3.zero);var low=AuthoredArt.Place(tree,asset+"Low",Vector3.zero);
                var group=tree.gameObject.AddComponent<LODGroup>();group.SetLODs(new[]{new LOD(.085f,high.GetComponentsInChildren<Renderer>()),new LOD(.012f,low.GetComponentsInChildren<Renderer>())});group.RecalculateBounds();
                var trunk=Box("Tree trunk collision",new Vector3(x,1.07f,z),new Vector3(.55f,2.4f,.55f),"wood");trunk.GetComponent<Renderer>().enabled=false;
            }
        }
        void Van()
        {
            Box("Van deck",new Vector3(0,-.1f,-11),new Vector3(3.7f,.2f,5.6f),"metal");
            Box("Van roof",new Vector3(0,2.65f,-11),new Vector3(3.7f,.15f,5.6f),"cream");
            Box("Van left",new Vector3(-1.85f,1.3f,-11),new Vector3(.16f,2.6f,5.6f),"cream");
            Box("Van right",new Vector3(1.85f,1.3f,-11),new Vector3(.16f,2.6f,5.6f),"cream");
            Box("Van cab wall",new Vector3(0,1.3f,-13.8f),new Vector3(3.7f,2.6f,.16f),"metal");
            Table(new Vector3(1.28f,0,-11.6f),new Vector2(.7f,2.5f),.8f);
            var rack=Interact("Equipment rack",new Vector3(-1.4f,1,-10.3f),new Vector3(.5f,.65f,1.4f),"metal",InteractKind.Kit,"Field kit · torch, induction, UV, recorder, radio","");
            rack.GetComponent<Renderer>().enabled=false;
            Box("Field kit shelf",new Vector3(-1.42f,.74f,-10.3f),new Vector3(.48f,.045f,1.5f),"metal",false);
            Box("Field kit rack back",new Vector3(-1.66f,1.02f,-10.3f),new Vector3(.045f,.58f,1.5f),"metal",false);
            for(int i=0;i<FieldKitArt.Names.Length;i++)
                FieldKitArt.Create(Root,FieldKitArt.Names[i],.19f,new Vector3(-1.38f,.77f,-10.86f+i*.28f),Quaternion.Euler(0,-90,0),false);
            Interact("Case terminal",new Vector3(1.25f,1.2f,-12),new Vector3(.45f,.5f,.6f),"black",InteractKind.Van,"Case terminal","");
            Box("Terminal screen",new Vector3(1.018f,1.23f,-12),new Vector3(.012f,.37f,.48f),"metal",false);
            Sign("BELLWETHER\nFIELD OFFICE\n\nCASE 014\nLINK SECURE",new Vector3(1.008f,1.38f,-11.8f),90,.016f);
            Box("Radio console",new Vector3(1.25f,.94f,-10.5f),new Vector3(.42f,.21f,.4f),"metal",false);
            for(int i=0;i<6;i++)Box("Radio grille",new Vector3(1.031f,.9f+i*.023f,-10.56f),new Vector3(.008f,.008f,.22f),"black",false);
            Sphere("Radio dial",new Vector3(1.021f,.96f,-10.35f),new Vector3(.025f,.055f,.055f),"brass");
            Box("Pinned briefing board",new Vector3(-1.75f,1.65f,-12.3f),new Vector3(.06f,.9f,1.1f),"wood",false);
            for(int i=0;i<3;i++)Box("Pinned field record",new Vector3(-1.71f,1.65f,-12.62f+i*.32f),new Vector3(.01f,.6f,.25f),"paper",false);
            Sign("FIELD STATION\nBELLWETHER / 014",new Vector3(.95f,2.20f,-13.69f),180,.032f);
            for(int i=0;i<5;i++)Box("Van roof rib",new Vector3(0,2.54f,-13+i),new Vector3(3.55f,.08f,.045f),"metal",false);
            Box("Van floor mat",new Vector3(0,.012f,-11),new Vector3(1.4f,.015f,4.6f),"carpet",false);
            Interact("Van briefing",new Vector3(1.1f,.87f,-10.9f),new Vector3(.5f,.03f,.65f),"paper",InteractKind.Note,"Case briefing",
                "HOLLOW CURRENT / CASE 014\n\nBellwether House. The Harrow family is safely away. Identify the presence and seal its anchor.\n\n1 Induction: hold left mouse while aiming at a signal.\n2 Ultraviolet: light reveals persistent traces; hold to record.\n3 EVP recorder: press left mouse, then stay quiet for a reply.\nF torch · R radio · J journal · E interact · Shift sprint · Ctrl crouch.\n\nEscape always releases the mouse. Click Resume to return. No microphone is used.");
            Lamp(new Vector3(0,2.4f,-11.3f),1.5f,false);
            // Keep the proven interior colliders and equipment positions while
            // replacing their visible shell with the original authored vehicle.
            foreach(var r in Root.GetComponentsInChildren<Renderer>())
                if(r.name=="Van deck"||r.name=="Van roof"||r.name=="Van left"||r.name=="Van right"||r.name=="Van cab wall"||r.name=="Van roof rib"||r.name=="Van floor mat")r.enabled=false;
            AuthoredArt.Place(Root,"FieldVanBody",new Vector3(0,0,-11));
            foreach(int side in new[]{-1,1})
            {
                AuthoredArt.Place(Root,"FieldVanWheel",new Vector3(side*2.0f,.36f,-9.4f),Quaternion.Euler(0,0,-side*90),Vector3.one);
                AuthoredArt.Place(Root,"FieldVanWheel",new Vector3(side*1.90f,.36f,-14.85f),Quaternion.Euler(0,0,-side*90),Vector3.one);
                var door=Box("Parked van door collision",new Vector3(side*2.671f,1.29f,-7.802f),new Vector3(1.79f,2.54f,.09f),"cream");door.transform.rotation=Quaternion.Euler(0,-side*25,0);door.GetComponent<Renderer>().enabled=false;
            }
            var cab=Box("Driver cab collision",new Vector3(0,1.2f,-14.75f),new Vector3(3.55f,2.4f,1.90f),"metal");cab.GetComponent<Renderer>().enabled=false;
        }
        void Table(Vector3 p,Vector2 size,float h)
        {
            Box("Table top",p+Vector3.up*h,new Vector3(size.x,.1f,size.y),"wood");
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Box("Table leg",p+new Vector3(x*(size.x/2-.12f),h/2,z*(size.y/2-.12f)),new Vector3(.1f,h,.1f),"wood");
        }
        void Chair(Vector3 p){Box("Chair seat",p+Vector3.up*.48f,new Vector3(.65f,.12f,.65f),"wood");Box("Chair back",p+new Vector3(0,.87f,.3f),new Vector3(.65f,.8f,.08f),"wood");foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Box("Chair leg",p+new Vector3(x*.25f,.23f,z*.25f),new Vector3(.06f,.46f,.06f),"wood");}
        void Bed(Vector3 p,bool large){float w=large?2.2f:1.3f;Box("Bed frame",p+Vector3.up*.32f,new Vector3(w,.45f,2.6f),"wood");Box("Mattress",p+Vector3.up*.62f,new Vector3(w-.08f,.22f,2.5f),"paper");Box("Worn quilt",p+new Vector3(0,.76f,.25f),new Vector3(w,.1f,1.8f),"sage",false);Box("Pillow",p+new Vector3(0,.8f,-.92f),new Vector3(w*.7f,.18f,.45f),"cloth",false);}
        void Bookshelf(Vector3 p,float yaw)
        {
            for(int i=0;i<4;i++){Box("Shelf",p+new Vector3(0,.25f+i*.5f,0),new Vector3(1.8f,.07f,.4f),"wood");for(int j=0;j<6;j++)Box("Book",p+new Vector3(-.7f+j*.23f,.44f+i*.5f,0),new Vector3(.15f,.3f,.28f),j%3==0?"red":"cloth",false);}
            foreach(int i in new[]{-1,1})Box("Shelf side",p+new Vector3(i*.88f,1,0),new Vector3(.07f,2,.4f),"wood");
        }
        void Mug(Vector3 p)=>AuthoredArt.Place(Root,"Mug",p,35);
        void Plant(Vector3 floor,float scale)
        {
            AuthoredArt.Place(Root,"HousePlant",floor,Quaternion.identity,Vector3.one*scale);
            if(scale>.7f){var hull=Box("Plant pot collision",floor+Vector3.up*.13f,new Vector3(.3f,.26f,.3f),"earth");hull.GetComponent<Renderer>().enabled=false;}
        }
        void Photograph(Vector3 center,float yaw,float scale)
        {
            var frame=AuthoredArt.Place(Root,"PictureFrame",center,Quaternion.Euler(0,yaw,0)*Quaternion.Euler(0,0,90),Vector3.one*scale);
            var photo=GameObject.CreatePrimitive(PrimitiveType.Quad);photo.name="Harrow family photograph";photo.transform.SetParent(frame,false);
            photo.transform.localPosition=new Vector3(0,0,-.033f);photo.transform.localRotation=Quaternion.Euler(0,0,-90);photo.transform.localScale=new Vector3(.45f,.33f,1);
            photo.GetComponent<Collider>().enabled=false;photo.GetComponent<Renderer>().sharedMaterial=Materials["familyphoto"];
        }
        void Sign(string text,Vector3 position,float yaw,float size)
        {
            var go=new GameObject("Field signage");go.transform.SetParent(Root,false);go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,yaw,0);
            var label=go.AddComponent<TextMesh>();label.text=text;label.characterSize=size;label.fontSize=40;label.color=new Color(.67f,.82f,.7f);label.anchor=TextAnchor.UpperLeft;
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.font.RequestCharactersInTexture(text,label.fontSize);
            go.GetComponent<MeshRenderer>().sharedMaterial=new Material(Resources.Load<Shader>("WorldText")){name="Depth-tested field lettering",mainTexture=label.font.material.mainTexture};
            go.AddComponent<WorldLabel>();
        }
        void Frame(Vector3 p)=>Photograph(p,-90,1.25f);
        public Transform Doll(Vector3 p)
        {
            var root=new GameObject("Porcelain doll").transform;root.SetParent(Root);root.position=p;
            var parts=new List<GameObject>{Sphere("Doll head",p+Vector3.up*.4f,new Vector3(.23f,.27f,.23f),"porcelain"),Sphere("Doll body",p+Vector3.up*.15f,new Vector3(.23f,.33f,.18f),"red")};
            for(int s=-1;s<=1;s+=2){parts.Add(Sphere("Glass eye",p+new Vector3(s*.049f,.425f,-.104f),new Vector3(.032f,.038f,.025f),"black"));parts.Add(Sphere("Doll hand",p+new Vector3(s*.17f,.16f,0),new Vector3(.08f,.16f,.08f),"porcelain"));}
            foreach(var part in parts)part.transform.SetParent(root,true);Dolls.Add(root);return root;
        }
        void Lamp(Vector3 p,float intensity,bool affected=true)
        {
            var fixture=AuthoredArt.Place(Root,"CeilingLamp",p+Vector3.up*.17f);var g=new GameObject("Practical light");g.transform.SetParent(Root);g.transform.position=p-Vector3.up*.26f;
            var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.88f,.74f);l.range=5.5f;l.intensity=intensity;l.shadows=LightShadows.Soft;l.shadowBias=.025f;l.shadowNormalBias=.15f;l.shadowResolution=UnityEngine.Rendering.LightShadowResolution.Low;if(affected)Lamps.Add(l);fixture.gameObject.AddComponent<PracticalFixture>().Source=l;
        }
        public Interactable Interact(string name,Vector3 p,Vector3 size,string mat,InteractKind kind,string label,string body)
        {
            var g=Box(name,p,size,mat);var i=g.AddComponent<Interactable>();i.Kind=kind;i.Label=label;i.Body=body;return i;
        }
        void Hide(string name,Vector3 p,float yaw)
        {
            var rotation=Quaternion.Euler(0,yaw,0);
            GameObject Panel(string part,Vector3 offset,Vector3 size)
            {
                var g=Box(name+" / "+part,p+rotation*offset,size,"wood");g.transform.rotation=rotation;return g;
            }
            Panel("back",new Vector3(0,1,.35f),new Vector3(1.15f,2,.1f));
            foreach(int side in new[]{-1,1})Panel("side",new Vector3(side*.56f,1,0),new Vector3(.08f,2,.8f));
            Panel("top",new Vector3(0,2,0),new Vector3(1.15f,.08f,.8f));
            // A real slatted opening, not a transparent wall: the center eye
            // ray leaves through a 6 cm gap, while the door blocks most of view.
            Panel("lower door",new Vector3(0,.70f,-.43f),new Vector3(1.1f,1.40f,.08f));
            Panel("upper door",new Vector3(0,1.95f,-.43f),new Vector3(1.1f,.10f,.08f));
            foreach(int side in new[]{-1,1})Panel("vent stile",new Vector3(side*.445f,1.65f,-.43f),new Vector3(.21f,.50f,.08f));
            foreach(float y in new[]{1.43f,1.53f,1.61f,1.71f,1.81f,1.89f})
                Panel("vent louvre",new Vector3(0,y,-.43f),new Vector3(.68f,.025f,.055f));
            var h=Interact(name,p+rotation*new Vector3(0,1,-.43f),new Vector3(1.1f,2,.08f),"wood",InteractKind.Hide,name,"");
            h.transform.rotation=rotation;h.GetComponent<Renderer>().enabled=false;
            h.GetComponent<Collider>().isTrigger=true; // Focus target, never an opaque wall across the slits.
            HidingPlaces.Add(h);h.HidePoint=p;h.ExitPoint=p+rotation*new Vector3(0,0,-1.2f);
        }
        void DoorAt(string name,Vector3 p,float yaw)
        {
            // The pivot sits beyond the frame face, so a real 175-degree swing
            // parks beside the wall instead of intersecting its thickness.
            bool landingDoor=name=="Interior door"&&p.x>0;
            Vector3 leaf=p+Quaternion.Euler(0,yaw,0)*new Vector3(0,0,landingDoor?.30f:0);
            var root=new GameObject(name+" hinge");root.transform.SetParent(Root);root.transform.position=leaf;root.transform.rotation=Quaternion.Euler(0,yaw,0);
            if(landingDoor)foreach(float x in new[]{-.04f,1.64f})Box("Door jamb",p+new Vector3(x,1.1f,0),new Vector3(.08f,2.2f,.4f),"wood");
            var panel=Box(name,leaf+new Vector3(.8f,1.1f,0),new Vector3(1.6f,2.2f,.09f),"wood");panel.transform.SetParent(root.transform,true);
            panel.GetComponent<Renderer>().enabled=false;
            AuthoredArt.Place(root.transform,"PanelDoor",new Vector3(.8f,0,0));
            foreach(int sign in new[]{-1,1})
            {
                foreach(float x in new[]{-.065f,1.665f})Box("Door architrave",p+new Vector3(x,1.14f,(!landingDoor?sign*.11f:sign<0?-.15f:.20f)),new Vector3(.10f,2.28f,.07f),"paper",false);
                Box("Door architrave head",p+new Vector3(.8f,2.29f,(!landingDoor?sign*.11f:sign<0?-.15f:.20f)),new Vector3(1.83f,.11f,.07f),"paper",false);
            }
            AuthoredArt.Place(Root,"Switch",p+new Vector3(1.91f,1.25f,name=="Front door"?-.17f:-.11f));
            var i=panel.AddComponent<Interactable>();i.Kind=InteractKind.Door;i.Label=name;
            var d=root.AddComponent<Door>();d.Closed=root.transform.localRotation;i.Door=d;Doors.Add(d);
        }
        void WireSwitches()
        {
            foreach(var t in Root.GetComponentsInChildren<Transform>())
            {
                if(t.name!="Authored / Switch")continue;
                var collider=t.gameObject.AddComponent<BoxCollider>();collider.size=new Vector3(.10f,.15f,.035f);
                var item=t.gameObject.AddComponent<Interactable>();item.Kind=InteractKind.LightSwitch;item.Label="Light switch";
                Vector3 target=t.position;
                if(Mathf.Abs(t.position.x-1.32f)<.01f)target=new Vector3(5,t.position.y+1.3f,10.7f);
                if(t.position.y<0)target=new Vector3(-5,-.7f,9.2f);
                float distance=float.MaxValue;
                foreach(var lamp in Lamps)
                {float d=(lamp.transform.position-target).sqrMagnitude;if(d<distance){distance=d;item.Circuit=lamp;}}
            }
        }
        void AudioScene()
        {
            // All sound is synthesized locally by Soundscape; no microphone or network.
        }
        public void BuildNavigation()
        {
            var sources=new List<NavMeshBuildSource>();
            foreach(var c in Root.GetComponentsInChildren<BoxCollider>())
            {
                if(!c.enabled||c.GetComponentInParent<Door>()!=null)continue;
                // Castle trigger volumes (stair loop, drawbridge) are not obstacles.
                if(Map==MapId.Castle&&c.isTrigger)continue;
                sources.Add(new NavMeshBuildSource {shape=NavMeshBuildSourceShape.Box,transform=c.transform.localToWorldMatrix,size=c.size,area=0});
            }
            // Castle architecture may use mesh colliders (Blender/Higgsfield meshes).
            if(Map==MapId.Castle)
                foreach(var c in Root.GetComponentsInChildren<MeshCollider>())
                {
                    if(!c.enabled||c.isTrigger||!c.sharedMesh||c.GetComponentInParent<Door>()!=null)continue;
                    sources.Add(new NavMeshBuildSource {shape=NavMeshBuildSourceShape.Mesh,sourceObject=c.sharedMesh,transform=c.transform.localToWorldMatrix,area=0});
                }
            var settings=NavMesh.GetSettingsByID(0);settings.agentRadius=.28f;settings.agentHeight=1.8f;settings.agentClimb=.25f;settings.agentSlope=45;
            settings.overrideVoxelSize=true;settings.voxelSize=.1f;
            NavData=NavMeshBuilder.BuildNavMeshData(settings,sources,NavBounds,Vector3.zero,Quaternion.identity);
            if(NavData!=null)navInstance=NavMesh.AddNavMeshData(NavData);
        }
        public void Dispose()
        {
            if(navInstance.valid)navInstance.Remove();
            if(NavData!=null)Release(NavData);
            if(ownsMaterials)foreach(var material in Materials.Values)Release(material);
            Materials.Clear();
        }
        static void Release(Object item){if(Application.isPlaying)Object.Destroy(item);else Object.DestroyImmediate(item);}
    }
}
