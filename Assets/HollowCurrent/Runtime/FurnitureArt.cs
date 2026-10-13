using System.Collections.Generic;
using UnityEngine;

namespace HollowCurrent
{
    // Authored visual replacements leave the tested simple collision hulls intact.
    public static class FurnitureArt
    {
        static readonly Dictionary<string,GameObject> Cache=new Dictionary<string,GameObject>();
        public static void Apply(World w)
        {
            Replace(w,"Sofa",new Vector3(-6.6f,0,4),new Vector3(2.85f,1.12f,1.2f),-90,"Sofa seat","Sofa back","Sofa arm");
            Replace(w,"Bathtub",new Vector3(6.6f,3.2f,12.5f),new Vector3(2.3f,.85f,1.2f),90,"Bath","Bath water");
            Replace(w,"Wardrobe",new Vector3(6.7f,3.2f,3.2f),new Vector3(1.5f,2.2f,.8f),90,"Guest wardrobe");
            Replace(w,"Sideboard",new Vector3(-5.6f,0,13.3f),new Vector3(3.1f,1.1f,.6f),0,"Dining sideboard");
            Replace(w,"Refrigerator",new Vector3(7.1f,0,9.1f),new Vector3(1.15f,2.08f,1.05f),90,"Refrigerator","Fridge drawing");
            for(int i=0;i<2;i++)
            {
                Vector3 p=new Vector3(6.8f,-3.2f,2+i*1.4f);
                if(Place(w.Root,"Washer",p,new Vector3(1.15f,1.1f,1.1f),90))HideNear(w,p,1.2f,"Laundry machine","Washer drum");
            }
            Bed(w,new Vector3(-5.8f,3.2f,3.2f),"ChildBed",new Vector3(1.3f,1.25f,2.6f));
            Bed(w,new Vector3(-5.8f,3.2f,10.5f),"ParentsBed",new Vector3(2.2f,1.3f,2.6f));
            Vector3 dining=new Vector3(-5.3f,0,10.4f);
            if(Place(w.Root,"DiningTable",dining,new Vector3(2.8f,.85f,1.65f),90))HideNear(w,dining,2.1f,"Table top","Table leg");
            foreach(int side in new[]{-1,1})
            {
                Vector3 p=dining+Vector3.right*(side*1.45f);
                if(Place(w.Root,"DiningChair",p,new Vector3(.65f,1.27f,.68f),side*90))HideNear(w,p,.75f,"Chair seat","Chair back","Chair leg");
            }
            foreach(var shelf in new[]{new Vector3(-5.8f,0,1),new Vector3(-3.1f,3.2f,1),new Vector3(-5.2f,-3.2f,13.3f)})
            {
                if(Place(w.Root,"Bookcase",shelf,new Vector3(1.8f,2.05f,.4f),shelf.z<2?180:0))HideNear(w,shelf,1.1f,"Shelf","Book","Shelf side");
            }
            Replace(w,"Furnace",new Vector3(6.7f,-3.2f,13),new Vector3(1.4f,2.3f,1),0,"Furnace");
            foreach(var doll in w.Dolls)
            {
                var old=doll.GetComponentsInChildren<Renderer>();
                if(Place(doll,"Doll",doll.position,new Vector3(.3f,.55f,.22f),0))foreach(var r in old)r.enabled=false;
            }
        }
        static void Bed(World w,Vector3 p,string asset,Vector3 size)
        {
            if(Place(w.Root,asset,p,size,180))HideNear(w,p,1.6f,"Bed frame","Mattress","Worn quilt","Pillow");
        }
        static void Replace(World w,string asset,Vector3 p,Vector3 size,float yaw,params string[] old)
        {
            if(!Place(w.Root,asset,p,size,yaw))return;
            foreach(var r in w.Root.GetComponentsInChildren<Renderer>())foreach(string n in old)if(r.name==n)r.enabled=false;
        }
        static void HideNear(World w,Vector3 p,float radius,params string[] names)
        {
            foreach(var r in w.Root.GetComponentsInChildren<Renderer>())
            {
                var d=r.transform.position-p;d.y=0;
                if(d.magnitude>radius||Mathf.Abs(r.transform.position.y-p.y)>2.2f)continue;
                foreach(string name in names)if(r.name==name)r.enabled=false;
            }
        }
        static bool Place(Transform parent,string name,Vector3 floor,Vector3 size,float yaw)
        {
            if(!Cache.TryGetValue(name,out var prefab)){prefab=Resources.Load<GameObject>("Furniture/"+name);Cache[name]=prefab;}
            if(!prefab){Debug.LogWarning("HC_FURNITURE_MISSING "+name);return false;}
            var root=new GameObject("Original crafted "+name).transform;root.SetParent(parent,false);root.position=floor;root.rotation=Quaternion.Euler(0,yaw,0);
            var instance=Object.Instantiate(prefab,root,false);var renderers=instance.GetComponentsInChildren<Renderer>();
            // Compute bounds before rotating or scaling so authored dimensions are deterministic.
            var bounds=new Bounds();bool first=true;
            foreach(var filter in instance.GetComponentsInChildren<MeshFilter>())
            {
                var b=filter.sharedMesh.bounds;var matrix=instance.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                for(int corner=0;corner<8;corner++)
                {
                    var point=matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                    if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
                }
            }
            var extent=bounds.size;
            instance.transform.localScale=new Vector3(size.x/Mathf.Max(.001f,extent.x),size.y/Mathf.Max(.001f,extent.y),size.z/Mathf.Max(.001f,extent.z));
            var offset=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z);
            instance.transform.localPosition=Vector3.Scale(offset,instance.transform.localScale);
            foreach(var r in renderers){r.receiveShadows=true;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;}
            return true;
        }
    }
}
