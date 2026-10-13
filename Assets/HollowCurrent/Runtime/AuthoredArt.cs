using System;
using UnityEngine;
namespace HollowCurrent
{
    public static class AuthoredArt
    {
        public static Transform Place(Transform parent,string name,Vector3 position,Quaternion rotation,Vector3 scale)
        {
            var prefab=Resources.Load<GameObject>("Architecture/"+name);
            if(!prefab&&MissingArt.Placeholders)
            {var stand=MissingArt.Placeholder(parent,name,Vector3.one*.5f);stand.localPosition=position;stand.localRotation=rotation;stand.localScale=scale;return stand;}
            if(!prefab)throw new InvalidOperationException("Required original architectural asset missing: "+name);
            var item=UnityEngine.Object.Instantiate(prefab,parent,false).transform;
            item.name="Authored / "+name;item.localPosition=position;item.localRotation=rotation;item.localScale=scale;
            foreach(var r in item.GetComponentsInChildren<Renderer>())
                if(r.name=="Shade"||r.name=="Glass")r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return item;
        }
        public static Transform Place(Transform parent,string name,Vector3 position,float yaw=0)
            =>Place(parent,name,position,Quaternion.Euler(0,yaw,0),Vector3.one);
    }
}
