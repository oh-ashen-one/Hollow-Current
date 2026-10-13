using System;
using UnityEngine;
namespace HollowCurrent
{
    public static class DetailArt
    {
        public static readonly string[] Required={"CookingRange","BathroomVanity","Toilet","BasementWorkbench","VanFieldConsole","BedsideCabinet","ToyChest","BasementStorage","PorchFrame"};
        public static void Apply(World w)
        {
            Place(w.Root,"CookingRange",new Vector3(7.35f,0,13.5f),new Vector3(.70f,1.15f,.72f),0);
            Hull(w,"Cooker",new Vector3(7.35f,.46f,13.5f),new Vector3(.7f,.92f,.72f));
            Hide(w,"Washbasin","Mirror","Toilet");
            Place(w.Root,"BathroomVanity",new Vector3(3.3f,3.2f,13.55f),new Vector3(.95f,1.8f,.55f),0);
            Hull(w,"Vanity base",new Vector3(3.3f,3.63f,13.55f),new Vector3(.95f,.86f,.55f));
            Place(w.Root,"Toilet",new Vector3(7.48f,3.2f,9.7f),new Vector3(.43f,.82f,.72f),90);
            Hull(w,"Toilet pedestal",new Vector3(7.48f,3.57f,9.7f),new Vector3(.72f,.74f,.43f));
            foreach(var r in w.Root.GetComponentsInChildren<Renderer>())
                if(r.name.StartsWith("Table ")&&r.transform.position.y< -1&&Mathf.Abs(r.transform.position.x+5.6f)<1.5f&&Mathf.Abs(r.transform.position.z-8.8f)<.8f)r.enabled=false;
            Place(w.Root,"BasementWorkbench",new Vector3(-5.6f,-3.2f,8.8f),new Vector3(2.4f,1.65f,.85f),0);
            Hull(w,"Workbench backing",new Vector3(-5.6f,-1.96f,9.2f),new Vector3(2.4f,.82f,.07f));
            foreach(var r in w.Root.GetComponentsInChildren<Renderer>())
                if(r.transform.position.z< -8&&(r.name.StartsWith("Table ")||r.name=="Case terminal"||r.name=="Terminal screen"||r.name.StartsWith("Radio ")))r.enabled=false;
            Place(w.Root,"VanFieldConsole",new Vector3(1.35f,0,-11.6f),new Vector3(2.2f,1.65f,.65f),90);
            var terminal=w.Root.Find("Case terminal");terminal.position=new Vector3(1.27f,1.16f,-10.85f);terminal.localScale=new Vector3(.14f,.45f,.58f);
            var brief=w.Root.Find("Van briefing");brief.position=new Vector3(1.23f,.925f,-11.7f);brief.localScale=new Vector3(.36f,.006f,.42f);
            foreach(var sign in w.Root.GetComponentsInChildren<TextMesh>())
                if(sign.transform.position.z> -13&&sign.transform.position.z< -11&&sign.transform.position.x>.9f)
                {
                    sign.text="BELLWETHER\nCASE 014\nLINK SECURE";sign.anchor=TextAnchor.MiddleCenter;sign.alignment=TextAlignment.Center;sign.characterSize=.007f;
                    sign.transform.position=new Vector3(1.195f,1.19f,-10.82f);
                }
            Bedside(w,new Vector3(-7,3.2f,2.3f));
            Bedside(w,new Vector3(-7.4f,3.2f,9.6f));
            Hide(w,"Toy chest","Packed family box","Porch awning","Porch post");
            Place(w.Root,"ToyChest",new Vector3(-5.7f,3.2f,5.9f),new Vector3(1.3f,.64f,.65f),0);
            Hull(w,"Toy chest",new Vector3(-5.7f,3.5f,5.9f),new Vector3(1.3f,.6f,.65f));
            w.Root.Find("Child's drawing").position=new Vector3(-5.7f,3.805f,5.9f);
            var drawing=w.Root.Find("Child's drawing");drawing.localScale=new Vector3(.4f,.006f,.30f);
            CluePage(w,drawing,"ChildDrawing");
            var diary=w.Root.Find("Basement diary");diary.position=new Vector3(-5.02f,-2.28f,8.66f);diary.localScale=new Vector3(.35f,.006f,.27f);
            CluePage(w,diary,"FurnaceDiary");
            foreach(float z in new[]{2.1f,4.1f})
            {
                Place(w.Root,"BasementStorage",new Vector3(-6.7f,-3.2f,z),new Vector3(1.1f,1,.65f),0);
                Hull(w,"Stored family belongings",new Vector3(-6.7f,-2.73f,z),new Vector3(1.1f,.94f,.65f));
            }
            // Source-mesh measurement: 2.7m overall height left only 1.785m
            // below the beam. 3.3m gives 2.18m clearance over the entry path.
            Place(w.Root,"PorchFrame",new Vector3(0,0,-.9f),new Vector3(3.6f,3.3f,1.8f),0);
            foreach(int side in new[]{-1,1})Hull(w,"Turned porch post",new Vector3(side*1.5f,1.1f,-1.485f),new Vector3(.2f,2.2f,.16f));
            Hull(w,"Porch canopy",new Vector3(0,2.24f,-.9f),new Vector3(3.25f,.12f,1.8f));
        }
        static void CluePage(World w,Transform item,string textureName)
        {
            item.GetComponent<Renderer>().enabled=false;
            var page=GameObject.CreatePrimitive(PrimitiveType.Quad);page.name="Original clue art / "+textureName;page.transform.SetParent(w.Root,false);
            page.transform.position=item.position+Vector3.up*(item.localScale.y*.5f+.001f);page.transform.rotation=Quaternion.Euler(90,0,0);page.transform.localScale=new Vector3(item.localScale.x,item.localScale.z,1);
            page.GetComponent<Collider>().enabled=false;
            var texture=Resources.Load<Texture2D>("Surfaces/"+textureName);
            if(!texture&&MissingArt.Placeholders){MissingArt.Report("texture",textureName);texture=MissingArt.MagentaTexture;}
            if(!texture)throw new InvalidOperationException("Original clue texture missing: "+textureName);
            var material=new Material(Shader.Find("Standard")){name="Original clue paper / "+textureName,mainTexture=texture};material.SetFloat("_Glossiness",.03f);
            page.GetComponent<Renderer>().sharedMaterial=material;page.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        static void Bedside(World w,Vector3 floor)
        {
            // Overall height includes the clock above the measured .57m top.
            Place(w.Root,"BedsideCabinet",floor,new Vector3(.52f,.72f,.43f),0);
            Hull(w,"Bedside cabinet",floor+Vector3.up*.285f,new Vector3(.52f,.57f,.43f));
            var top=floor+new Vector3(-.12f,.57f,0);
            var shade=AuthoredArt.Place(w.Root,"TableLamp",top,Quaternion.identity,Vector3.one*.65f);
            var source=new GameObject("Bedroom reading lamp");source.transform.SetParent(w.Root,false);source.transform.position=top+Vector3.up*.30f;
            var light=source.AddComponent<Light>();light.type=LightType.Point;light.range=3.2f;light.intensity=.45f;light.color=new Color(1,.76f,.48f);light.shadowBias=.02f;light.shadowNormalBias=.12f;
            w.Lamps.Add(light);shade.gameObject.AddComponent<PracticalFixture>().Source=light;
        }
        public static Transform Place(Transform parent,string name,Vector3 floor,Vector3 size,float yaw)
        {
            var prefab=Resources.Load<GameObject>("Furniture/"+name);
            if(!prefab&&MissingArt.Placeholders)
            {var stand=MissingArt.Placeholder(parent,name,size);stand.position=floor;stand.rotation=Quaternion.Euler(0,yaw,0);return stand;}
            if(!prefab)throw new InvalidOperationException("Required original detail model missing: "+name);
            var root=new GameObject("Original detail / "+name).transform;root.SetParent(parent,false);root.position=floor;root.rotation=Quaternion.Euler(0,yaw,0);
            var model=UnityEngine.Object.Instantiate(prefab,root,false).transform;var filters=model.GetComponentsInChildren<MeshFilter>();
            if(filters.Length==0)throw new InvalidOperationException("Detail model has no geometry: "+name);
            var bounds=filters[0].sharedMesh.bounds;for(int i=1;i<filters.Length;i++)bounds.Encapsulate(filters[i].sharedMesh.bounds);
            var scale=new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z);
            model.localScale=scale;model.localPosition=Vector3.Scale(new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z),scale);
            return root;
        }
        static void Hull(World w,string name,Vector3 p,Vector3 size)
        {var g=w.Box(name+" collision",p,size,"wood");g.GetComponent<Renderer>().enabled=false;}
        static void Hide(World w,params string[] names)
        {
            foreach(var t in w.Root.GetComponentsInChildren<Transform>())foreach(string name in names)
                if(t.name==name){var r=t.GetComponent<Renderer>();if(r)r.enabled=false;var c=t.GetComponent<Collider>();if(c)c.enabled=false;}
        }
    }
}
