using UnityEngine;

namespace HollowCurrent
{
    // STUB: replaced by hc/layout (piece 1 owns this file).
    // Keep the public surface exactly: World.BuildCastle, WorldLayout and the
    // castle validator compile against Points and Build below.
    public static class CastleWorld
    {
        // Contract points, world space. Floors y = -3.2 / 0 / 3.2 / 6.4.
        // Footprint x +-14, z 0-28. Serialized into the prepared scene (WorldLayout).
        [System.Serializable]
        public struct Points
        {
            public Vector3 Spawn;               // player start, inside Gatehouse
            public Bounds Gatehouse;            // safe zone (the "van"): x/z extent is used
            public Vector3 Anchor;              // chapel bell (the music box's role)
            public Vector3 Compass;             // Hollow Compass hot spot (induction)
            public Vector3 Voice;               // ear-trumpet whisper spot (EVP)
            public Vector3 Print;               // claw-scores, a point on a wall face (UV print)
            public Vector3 HunterSpawn;         // hunt start, on a walkable floor
            public Vector3[] PerchPoints;       // balconies, ledges, gallery
            public Vector3 AltarPosition;       // chapel altar (forge)
            public Vector3[] RelicPositions;    // [0] silver blade, [1] hallowed hilt, [2] moonstone
            public Bounds DrawbridgeTrigger;    // escape volume; the finish interactable sits at its centre
        }

        // Builds every castle object under root and returns the contract points.
        public static void Build(Transform root,out Points points)
        {
            var stone=new Material(Shader.Find("Standard")){name="Castle stub / stone",color=new Color(.30f,.29f,.31f)};
            void Block(string name,Vector3 center,Vector3 size)
            {
                var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="Castle stub / "+name;g.transform.SetParent(root,false);
                g.transform.position=center;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=stone;
            }
            // Flat 30 x 30 m courtyard floor and four 4.5 m walls; no upper floors.
            Block("floor",new Vector3(0,-.25f,14),new Vector3(30,.5f,30));
            Block("north wall",new Vector3(0,2.25f,29.25f),new Vector3(30.5f,4.5f,.5f));
            Block("south wall",new Vector3(0,2.25f,-1.25f),new Vector3(30.5f,4.5f,.5f));
            Block("west wall",new Vector3(-15.25f,2.25f,14),new Vector3(.5f,4.5f,30.5f));
            Block("east wall",new Vector3(15.25f,2.25f,14),new Vector3(.5f,4.5f,30.5f));
            foreach(var p in new[]{new Vector3(-6,3.6f,10),new Vector3(6,3.6f,20)})
            {
                var g=new GameObject("Castle stub / torch light");g.transform.SetParent(root,false);g.transform.position=p;
                var l=g.AddComponent<Light>();l.type=LightType.Point;l.range=9;l.intensity=.9f;l.color=new Color(1,.62f,.36f);
            }
            points=new Points
            {
                Spawn=new Vector3(0,.1f,1.5f),
                Gatehouse=new Bounds(new Vector3(0,1.5f,2.25f),new Vector3(6,3,4.5f)),
                Anchor=new Vector3(8,1.2f,22),
                Compass=new Vector3(-8,1.1f,12),
                Voice=new Vector3(6,1.2f,8),
                Print=new Vector3(-14.97f,1.6f,20),
                HunterSpawn=new Vector3(10,.1f,26),
                PerchPoints=new[]{new Vector3(-10,3.2f,26),new Vector3(10,3.2f,26),new Vector3(0,3.2f,27.5f),new Vector3(-12,3.2f,6)},
                AltarPosition=new Vector3(0,0,25),
                RelicPositions=new[]{new Vector3(-12,.5f,26),new Vector3(12,.5f,26),new Vector3(0,.5f,14)},
                DrawbridgeTrigger=new Bounds(new Vector3(0,1,.25f),new Vector3(3,2.5f,1)),
            };
        }
    }
}
